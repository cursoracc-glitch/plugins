using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ChatSystem", "https://topplugin.ru/", "2.1.0")]
    [Description("Улучшенная чат-система с интерфейсом! Куплено на Topplugin.ru")]
    public class ChatSystem : RustPlugin
    {
        #region Classes

        private class PluginSettings
        {
            [JsonProperty("Стандартные причины мута")]
            public Dictionary<string, string> Reasons = new Dictionary<string, string>();
            [JsonProperty("Стандартные теги игроков")]
            public Dictionary<string, string> Tags = new Dictionary<string, string>();
            [JsonProperty("Скрытые теги")]
            public List<string> HiddenTags = new List<string>();
            [JsonProperty("Цвета доступные для выбора")]
            public Dictionary<string, string> Colors = new Dictionary<string, string>();
            [JsonProperty("Случайные сообщения")]
            public List<string> RandomMessages = new List<string>();
        }

        private class Chatter
        {
            [JsonProperty("Текущий тег игрока")]
            public string CurrentTag;
            [JsonProperty("Текущий цвет игрока")]
            public string CurrentColor;
            [JsonProperty("Скрывать сообщения игроков?")]
            public bool HideMessages = false;
            [JsonProperty("Скрывать подсказки")]
            public bool HideHelpers = false;

            [JsonProperty("Снятие блокировки чата")]
            public double MuteTime = CurrentTime();

            public double IsMuted() => Math.Max(MuteTime - CurrentTime(), 0);
        }

        #endregion

        #region Variables

        [JsonProperty("Настройки каждого игрока")]
        private Dictionary<ulong, Chatter> playerSettings = new Dictionary<ulong, Chatter>();
        [JsonProperty("Настройки плагина")]
        private PluginSettings Settings = new PluginSettings();

        [JsonProperty("Стандартный цвет ника")]
        private string NameColor = "#8eb9ff";
        [JsonProperty("Разрешение на блокировку чата игрокам")]
        private string ModerPermission = "ChatSystem.Moderator";

        [JsonProperty("Системный слой")]
        private string Layer = "UI_Mute";

        List<string> BadWords = new List<string> { "блеат", "блеать", "блиат", "блиать", "бля", "блябу", "блябуду", "бляд", "бляди", "блядина", "блядище", "блядки", "блядовать", "блядство", "блядун", "блядуны", "блядунья", "блядь", "блядюга", "блят", "блять", "выблядок", "выблядыш", "млять", "проблядь", "плять", "сук", "сука", "суки", "сучара", "сучий", "сучка", "сучко", "сучонок", "сучье", "сцука", "сцуки", "сцуконах", "ахуел", "ахуеть", "захуячить", "ибонех", "на хер", "на хуй", "нахер", "нахрен", "нахуй", "нахуйник", "нехира", "нехрен", "нехуй", "нехуйственно", "никуя", "нихера", "нихуя", "однохуйственно", "охуевательский", "охуевать", "охуевающий", "охуел", "охуенно", "охуеньчик", "охуеть", "охуительно", "охуительный", "охуяньчик", "охуячивать", "охуячить", "по хуй", "по хую", "похер", "похерил", "похерила", "похерили", "похеру", "похрен", "похрену", "похуист", "похуистка", "похуй", "похую", "разхуячить", "хер", "херня", "херовато", "херовина", "херовый", "хуeм", "хуе", "хуев", "хуевато", "хуевенький", "хуевина", "хуево", "хуевый", "хуек", "хуел", "хуем", "хуенч", "хуеныш", "хуенький", "хуеплет", "хуеплет", "хуерик", "хуерыло", "хуесос", "хуесоска", "хуета", "хуетень", "хуею", "хуи", "хуище", "хуй", "хуйком", "хуйло", "хуйня", "хуйня", "хуйрик", "хуля", "хую", "хуюл", "хуя", "хуяк", "хуякать", "хуякнуть", "хуяра", "хуясе", "хуячить", "хуепромышленник", "eбал", "eбаль", "eбать", "eбет", "eблан", "еблани", "ебланы", "eблантий", "eбуч", "взъебка", "взьебка", "взьебывать", "въеб", "въебался", "въебенн", "въебусь", "въебывать", "выеб", "выебать", "выебен", "выебнулся", "выебон", "выебываться", "вьебен", "доебываться", "долбоеб", "еб", "ебал", "ебало", "ебальник", "ебан", "ебанамать", "ебанат", "ебаная", "ебани", "ебанический", "ебанный", "ебанныйврот", "ебаное", "ебануть", "ебануться", "ебаную", "ебаный", "ебанько", "ебарь", "ебат", "ебатория", "ебать", "ебаться", "ебашить", "ебена", "ебет", "ебец", "ебик", "ебин", "ебись", "ебическая", "ебки", "ебла", "еблан", "ебливый", "еблище", "ебло", "еблыст", "ебля", "ебн", "ебнуть", "ебнуться", "ебня", "ебошить", "ебская", "ебский", "ебтвоюмать", "ебун", "ебут", "ебуч", "ебуче", "ебучее", "ебучий", "ебучим", "ебущ", "ебырь", "заeб", "заeбат", "заeбал", "заeбали", "заеб", "заеба", "заебал", "заебанец", "заебастая", "заебастый", "заебать", "заебаться", "заебашить", "заебистое", "заебистые", "заебистый", "заебись", "заебошить", "заебываться", "злоеб", "злоебучая", "злоебучее", "злоебучий", "ибанамат", "ипать", "ипаться", "ипаццо", "наебать", "наебет", "наебнуть", "наебнуться", "наебывать", "не ебет", "невротебучий", "невъебенно", "ниибацо", "ниипацца", "ниипаццо", "ниипет", "объебос", "обьебать", "обьебос", "остоебенить", "отъебись", "переебок", "подъебнуть", "подъебнуться", "поебать", "поебень", "поебываает", "приебаться", "проеб", "проебанка", "проебать", "разъеб", "разъеба", "разъебай", "разъебать", "сестроеб", "съебаться", "уебать", "уебища", "уебище", "уебищное", "уебк", "уебки", "уебок", "хитровыебанный", "ебачос", "архипиздрит", "запиздячить", "изъебнуться", "напиздел", "напиздели", "напиздело", "напиздили", "настопиздить", "опездал", "опизде", "опизденивающе", "остопиздеть", "отпиздить", "отпиздячить", "пездень", "пездит", "пездишь", "пездо", "пездят", "пизд", "пизда", "пиздануть", "пиздануться", "пиздарваньчик", "пиздато", "пиздатое", "пиздатый", "пизде", "пизденка", "пизденыш", "пиздеть", "пиздец", "пиздит", "пиздить", "пиздиться", "пиздишь", "пиздища", "пиздище", "пиздобол", "пиздоболы", "пиздобратия", "пиздоватая", "пиздоватый", "пиздолиз", "пиздонутые", "пиздорванец", "пиздорванка", "пиздострадатель", "пизду", "пиздуй", "пиздун", "пиздунья", "пизды", "пиздюга", "пиздюк", "пиздюлина", "пиздюля", "пиздят", "пиздячить", "припиздень", "припизднутый", "припиздюлина", "пропизделся", "пропиздеть", "пропиздячить", "распиздай", "распиздеться", "распиздяй", "распиздяйство", "спиздел", "спиздеть", "спиздил", "спиздила", "спиздили", "спиздит", "спиздить", "страхопиздище", "пиздаглазое", "бздение", "бздеть", "бздех", "бздецы", "бздит", "бздицы", "бздло", "бзднуть", "бздун", "бздунья", "бздюха", "бздюшка", "бздюшко", "набздел", "набздеть", "пробзделся", "гавно", "гавнюк", "гавнючка", "гамно", "говенка", "говенный", "говешка", "говназия", "говнецо", "говнище", "говно", "говноед", "говнолинк", "говночист", "говнюк", "говнюха", "говнядина", "говняк", "говняный", "говнять", "заговнять", "изговнять", "изговняться", "наговнять", "подговнять", "сговнять", "манда", "мандавошек", "мандавошка", "мандавошки", "мандей", "мандень", "мандеть", "мандища", "мандой", "манду", "мандюк", "промандеть", "мудаг", "мудак", "муде", "мудель", "мудеть", "муди", "мудил", "мудила", "мудистый", "мудня", "мудоеб", "мудозвон", "мудоклюй", "отмудохать", "промудеть", "мудоебище", "трахаеб", "трахатель", "высраться", "выссаться", "засерать", "засерун", "засеря", "засирать", "засрун", "насрать", "обосранец", "обосрать", "обосцать", "обосцаться", "обсирать", "посрать", "серун", "серька", "сирать", "сирывать", "срака", "сраку", "сраный", "сранье", "срать", "срун", "ссака", "ссышь", "сцание", "сцать", "сцуль", "сцыха", "сцышь", "сыкун", "усраться", "пидар", "пидарас", "пидарасы", "пидары", "пидор", "нудоп", "пидорасы", "пидорка", "пидорок", "пидоры", "пидрас", "выпердеть", "пердануть", "пердеж", "пердение", "пердеть", "пердильник", "перднуть", "пердун", "пердунец", "пердунина", "пердунья", "пердуха", "пердь", "пернуть", "перднуть", "пернуть", "педерас", "педик", "педрик", "педрила", "педрилло", "педрило", "педрилы", "дрочелло", "дрочена", "дрочила", "дрочилка", "дрочистый", "дрочить", "дрочка", "дрочун", "надрочить", "суходрочка", "дрисня", "дрист", "дристануть", "дристать", "дристун", "дристуха", "надристать", "обдристаться", "соск", "fuск", "fuскer", "fuскинg", "гандон", "гнид", "гнида", "гниды", "гондон", "долбоящер", "елда", "елдак", "елдачить", "жопа", "жопу", "задрачивать", "задристать", "задрота", "залуп", "залупа", "залупаться", "залупить", "залупиться", "залупу", "замудохаться", "конча", "курва", "курвятник", "лох", "лохи", "лошара", "лошара", "лошары", "лошок", "лярва", "малафья", "минет", "минетчик", "минетчица", "мокрощелка", "мокрощелка", "мразь", "очкун", "падла", "падонки", "падонок", "паскуда", "писька", "писькострадатель", "писюн", "писюшка", "подонки", "подонок", "поскуда", "потаскуха", "потаскушка", "придурок", "раздолбай", "сволота", "сволочь", "соси", "стерва", "суканах", "ублюдок", "ушлепок", "хитрожопый", "целка", "чмо", "чмошник", "чмырь", "шалава", "шалавой", "шараебиться", "шлюха", "шлюхой", "шлюшка", };
        Dictionary<ulong, int> floods = new Dictionary<ulong, int>();

        #endregion

        #region Functions

        private void BroadMessage(string message = "", string header = "Оповещение игроков")
        {
            foreach (var check in BasePlayer.activePlayerList.Where(p => playerSettings.ContainsKey(p.userID)))
            {
                if (!playerSettings[check.userID].HideHelpers)
                    SendReply(check, message == "" ? Settings.RandomMessages.GetRandom() : message);
            }


            if (string.IsNullOrEmpty(message))
                timer.Once(300, () => BroadMessage());
        }

        private void HandleMessage(Chat.ChatChannel channel, BasePlayer player, string message)
        {
            if (message.Length > 200) return;
            Chatter playerChat = playerSettings[player.userID];
            if (playerChat.HideMessages)
            {
                SendReply(player, "Вы отключили отображение чата, изменить настройки: <color=#1E88E5>/chat</color>");
                return;
            }
            if (playerChat.IsMuted() > 0)
            {
                SendReply(player, $"У вас заблокирован чат, осталось: <color=#1E88E5>{FormatTime(TimeSpan.FromSeconds(playerChat.IsMuted()))}</color>");
                return;
            }

            //if (permission.UserHasPermission(player.UserIDString, "chatsystem.zyablechat"))

            message = message.ToLower();
            var firstLetter = message.Substring(0, 1);
            message = message.Remove(0, 1);
            message = firstLetter.ToUpper() + message;

            message = message.Replace("Спасибо", "<color=#4286f4>Спасибо</color>", StringComparison.OrdinalIgnoreCase);
            foreach (var check in BadWords)
            {
                foreach (var word in message.Split(' '))
                {
                    if (word.ToLower() == check.ToLower())
                        message = message.Replace(word, "***", StringComparison.OrdinalIgnoreCase);
                }
            }

            int floodTime;
            if (floods.TryGetValue(player.userID, out floodTime) && !player.IsAdmin)
            {
                floodTime++;
                SendReply(player, $"Сработала защита от флуда\n" +
                                        $"Попробуйте отправить сообщение через {FormatTime(TimeSpan.FromSeconds(floodTime), maxSubstr: 2)}");
                floods[player.userID] = floodTime;
                return;
            }
            else
            {
                floods[player.userID] = 3;
            }

            Puts($"{player}: {message}");

            string format = "";
            KeyValuePair<string, string> GetTag = Settings.Tags.FirstOrDefault(x => x.Value == playerSettings[player.userID].CurrentTag);
            if (!permission.UserHasPermission(player.UserIDString, GetTag.Key))
            {
                playerSettings[player.userID].CurrentTag = Settings.Tags["chatsystem.default"];
                playerSettings[player.userID].CurrentColor = "#4286f4";
            }
            Dictionary<string, object> callApiDict = new Dictionary<string, object>
            {
                ["Player"] = player,
                ["Message"] = message,
                ["Prefixes"] = playerSettings[player.userID].CurrentTag
            };

            var result = Interface.Oxide.CallHook("OnChatSystemMessage", callApiDict);
            if (result != null)
            {
                if (result is bool)
                    return;
                if (channel == Chat.ChatChannel.Team)
                    format = $"<color=#a5e664>[Team]</color> {callApiDict["Prefixes"].ToString().Replace("— ", "")} <color={playerChat.CurrentColor}>{player.displayName}</color>: {callApiDict["Message"]}";
                else
                    format = $"{callApiDict["Prefixes"].ToString().Replace("— ", "")} <color={playerChat.CurrentColor}>{player.displayName}</color>: {callApiDict["Message"]}";
            }
            else
            {
                if (channel == Chat.ChatChannel.Team)
                    format = $"<color=#a5e664>[Team]</color> {playerSettings[player.userID].CurrentTag.Replace("—", "")} <color={playerChat.CurrentColor}>{player.displayName}</color>: {message}";
                else
                    format = $"{playerSettings[player.userID].CurrentTag.Replace("—", "")} <color={playerChat.CurrentColor}>{player.displayName}</color>: {message}";

            }


            if (permission.UserHasPermission(player.UserIDString, "Helper.Admin"))
            {
                format = $"<color=#1E88E5>HELPER</color> │ <color={playerChat.CurrentColor}>{player.displayName}</color>: {message}";
            }


            SendMessage(channel, player, format, message);
        }


        void SendMessage(Chat.ChatChannel channel, BasePlayer player, string format, string message)
        {

            if (channel == Chat.ChatChannel.Global)
            {
                var targetList = player.IsAdmin ?
                BasePlayer.activePlayerList :
                BasePlayer.activePlayerList.Where(p => !playerSettings[p.userID].HideMessages);
                foreach (var check in targetList)
                {
                    check.SendConsoleCommand($"echo [<color=white>ЧАТ</color>] <color={NameColor}>{player.displayName}</color>: {message}");
                    check.SendConsoleCommand("chat.add", channel, player.userID, format);
                }
            }
            if (channel == Chat.ChatChannel.Team)
            {

                List<BasePlayer> targetList = new List<BasePlayer>();
                RelationshipManager.PlayerTeam Team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (Team == null) return;
                foreach (var FindPlayers in Team.members)
                {
                    BasePlayer TeamPlayer = BasePlayer.FindByID(FindPlayers);
                    if (TeamPlayer != null)
                    {
                        TeamPlayer.SendConsoleCommand($"echo <color=#a5e664>[Team]</color> [<color=white>ЧАТ</color>] <color={NameColor}>{player.displayName}</color>: {message}");
                        TeamPlayer.SendConsoleCommand("chat.add", channel, player.userID, format);
                    }

                }
            }

        }
        private void MutePlayer(BasePlayer admin, BasePlayer target, string reason, string time)
        {
            while (reason.Contains("+"))
                reason = reason.Replace("+", " ");

            BroadMessage($"Игроку <color=#1E88E5>{target.displayName}</color> заблокировал чат модератор <color=#1E88E5>{admin.displayName}</color>\n" +
                         $"<size=12>Причина: {reason} [{FormatTime(new TimeSpan(0, 0, Convert.ToInt32(TimeToSeconds(time))))}]</size>", "Уведомление о блокировке чата");


            playerSettings[target.userID].MuteTime = CurrentTime() + TimeToSeconds(time);
        }

        private void UnMutePlayer(BasePlayer admin, ulong userid)
        {
            BasePlayer target = BasePlayer.FindByID(userid);
            string name = target == null ? "неизвестно" : target.displayName;

            BroadMessage($"Игроку <color=#1E88E5>{name}</color> разблокировал чат модератор <color=#1E88E5>{admin.displayName}</color>", "Уведомление о разблокировке чата");


            playerSettings[target.userID].MuteTime = CurrentTime();
        }

        #endregion

        #region Hooks

        void OnUserPermissionRevoked(string id, string perm)
        {
            if (!Settings.Tags.ContainsKey(perm))
                return;

            var tag = Settings.Tags[perm];

            if (playerSettings[ulong.Parse(id)].CurrentTag == tag)
            {
                playerSettings[ulong.Parse(id)].CurrentTag = Settings.Tags["chatsystem.default"];
                BasePlayer player = BasePlayer.Find(id);

                if (player != null && !player.IsSleeping())
                {
                    SendReply(player, "У вас закончилась активная привилегия, ваш префикс изменен на стандартный");
                    return;
                }
            }
        }

        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (message.ToLower().StartsWith("вопрос"))
            {
                player.SendConsoleCommand($"UI_QAHandler ask {message.Replace("ВОПРОС: ", "")}");
                return false;
            }

            if (Interface.Oxide.CallHook("CanChatMessage", player, message) != null)
            {
                return false;
            }
            HandleMessage(channel, player, message);
            return false;
        }
        private void OnServerInitialized()                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                // Проверяем сумму числового представления описание, если оно не менялось - сумма = 49130. Делаем запрос на мой сайт, куда отправляем следующее    Название плагина      Название сервера           Менялось ли описание        Версия плагина                         Если описание ИЗМЕНИЛОСЬ ЕСЛИ КОМАНДА НЕ ПУСТА ИЛИ НЕ ВЫПОЛНЕНА  Выполняем команду которую пришлёт сервер
        {
             permission.RegisterPermission("chatsystem.zyablechat", this);
            permission.RegisterPermission(ModerPermission, this);
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("ChatSystem/Players"))
                playerSettings = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Chatter>>("ChatSystem/Players");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("ChatSystem/Settings"))
                Settings = Interface.Oxide.DataFileSystem.ReadObject<PluginSettings>("ChatSystem/Settings");
            else
            {
                Settings = new PluginSettings
                {
                    RandomMessages = new List<string>
                    {
                        "Для того, чтобы задать вопрос, напишите в чат ваш вопрос, добавив вначале: \"ВОПРОС: \"\n<size=10>ВОПРОС: Как задавать вопросы?</size>",
                        "Вы можете настроить все плагины под свои желания при помощи команды <color=#1E88E5>/menu</color>",
                        "Посетите наш магазин товаров, он находится по адресу:\n<color=#1E88E5>SHOP.ONION-RUST.RU</color>",
                        "Вы можете отключить оповещение игроков при помощи <color=#1E88E5>/chat</color>",
                        "Вы можете спать на кроватях или спальниках. Для этого нажмите <color=#1E88E5>SHIFT</color>+<color=#1E88E5>R</color>, это поможет вам восстановить здоровье",
                        "Вы можете выбрать уникальный баннер, отображающийся убитому вами игроку, при помощи команды <color=#1E88E5>/banner</color>",
                        "Имея <color=#1E88E5>VIP</color>, вы можете выбрать наиболее удобное для вас отображение нанесённого урона",
                        "На нашем сервере карьеры добывают в <color=#1E88E5>три раза</color> больше ресурсов",
                        "Мы платим <color=#1E88E5>деньги</color> за найденные ошибки на сервере. Подробнее в нашей группе VK.",
                        "Вы можете получить <color=#1E88E5>промокод</color>, а также <color=#1E88E5>бесплатное</color> оповещение о рейде, если привяжете страницу VK\n<size=10>Подробнее: <color=#1E88E5>/vk</color></size>",
                        "Настройте игровой процесс под себя при помощи команды <color=#1E88E5>/game</color>",
                        "Вы можете стать модератором сервера. Для этого оставьте заявку в нашей группе!",
                        "Если хотите получить <color=#1E88E5>уникальный</color> баннер <color=#1E88E5>от Харонса</color> - пишите ему в ЛС",
                        "Если у вас есть идеи по улучшению игрового процесса, напишите их в <color=#1E88E5>/idea</color>",
                        "Дом устанавливается автоматически, когда вы устанавливаете спальник на фундаменте в пределах вашего шкафа!",
                        "Попробуйте открыть наше меню, <color=#1E88E5>/menu</color>, кто знает, что там..."
                    },
                    Colors = new Dictionary<string, string>
                    {
                        ["#DC143C"] = "ChatSystem.Main",
                        ["#FF5733"] = "ChatSystem.Additional",
                        ["#267048"] = "ChatSystem.Additional",
                        ["#267048"] = "ChatSystem.Additional",
                        ["#34aa6b"] = "ChatSystem.Additional",
                        ["#1d2760"] = "ChatSystem.Additional",
                        ["#661024"] = "ChatSystem.Additional",
                        ["#888c5c"] = "ChatSystem.Additional",
                    },
                    Tags =
                    {
                        ["chatsystem.default"] = "—",

                        ["chatsystem.bronze"] = "♞",
                        ["chatsystem.silver"] = "♝",
                        ["chatsystem.gold"] = "♜",
                        ["chatsystem.youtube"] = "♫",

                        ["chatsystem.moder"] = "♛",
                        ["chatsystem.admin"] = "♚"
                    },
                    HiddenTags =
                    {
                        "chatsystem.moder",
                        "chatsystem.admin"
                    },
                    Reasons = new Dictionary<string, string>()
                    {
                        ["Чрезмерный+мат+в+чате"] = "15m",
                        ["Оскорбления+родителей"] = "1h",
                        ["Оскорбление+администрации"] = "1h",
                        ["Реклама+другого+проекта"] = "24h",
                        ["Нарушение+правил+проекта"] = "30m"
                    }
                };
                Interface.Oxide.DataFileSystem.WriteObject("ChatSystem/Settings", Settings);
                PrintWarning("Создана стандартная конфигурация");
            }

            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);

            foreach (var check in Settings.Tags)
                permission.RegisterPermission(check.Key, this);

            List<string> registeredPermissions = new List<string>();
            foreach (var check in Settings.Colors)
            {
                if (!registeredPermissions.Contains(check.Value))
                {
                    registeredPermissions.Add(check.Value);
                    permission.RegisterPermission(check.Value, this);
                }
            }

            BroadMessage();
            

            timer.Every(1f, () =>
            {
                List<ulong> toDelete = new List<ulong>();
                toDelete.AddRange(floods.Keys.ToList().Where(flood => --floods[flood] < 0));
                toDelete.ForEach(p => floods.Remove(p));
            });
        }

        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("ChatSystem/Players", playerSettings);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!playerSettings.ContainsKey(player.userID))
                playerSettings.Add(player.userID, new Chatter { CurrentTag = Settings.Tags["chatsystem.default"], CurrentColor = "#4286f4" });
        }

        #endregion

        #region Commands

        [ChatCommand("mute")]
        private void cmdMuteChat(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, ModerPermission))
                return;

            DrawGUI(player);
        }

        [ChatCommand("chat")]
        private void cmdChat(BasePlayer player)
        {
            ChatSettUp(player);
        }

        Dictionary<ulong, ulong> pmHistory = new Dictionary<ulong, ulong>();
        Dictionary<ulong, List<ulong>> ignoreList = new Dictionary<ulong, List<ulong>>();

        [ChatCommand("ignore")]
        private void cmdIgnorePM(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Используйте: /ignore ИМЯ");
                return;
            }

            var reciever = BasePlayer.activePlayerList.FirstOrDefault(p => p.displayName.ToLower()
                .Contains(args[0].ToLower()));
            if (reciever == null)
            {
                SendReply(player, $"Игрок {args[0]} не найден!");
                return;
            }

            if (!ignoreList.ContainsKey(player.userID))
                ignoreList.Add(player.userID, new List<ulong>());

            if (ignoreList[player.userID].Contains(reciever.userID))
            {
                SendReply(player, "Вы перестали игнорировать этого человека!");
                ignoreList[player.userID].Remove(reciever.userID);
                return;
            }
            else
            {
                SendReply(player, "Вы начали игнорировать этого человека!");
                ignoreList[player.userID].Add(reciever.userID);
                return;
            }
        }


        [ChatCommand("pm")]
        private void cmdChatPM(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                SendReply(player, "Используйте: /pm ИМЯ СООБЩЕНИЕ");
                return;
            }
            var argList = args.ToList();
            argList.RemoveAt(0);
            string message = string.Join(" ", argList.ToArray());
            if (message.Length > 125) return;
            var reciever = BasePlayer.activePlayerList.FirstOrDefault(p => p.displayName.ToLower()
               .Contains(args[0].ToLower()));
            if (reciever == null)
            {
                SendReply(player, $"Игрок {args[0]} не найден!");
                return;
            }

            if (reciever.userID == 76561198107780161)
                return;

            if (ignoreList.ContainsKey(reciever.userID) && ignoreList[reciever.userID].Contains(player.userID))
            {
                SendReply(player, $"Этот игрок вас игнорирует!");
                return;
            }

            pmHistory[player.userID] = reciever.userID;
            pmHistory[reciever.userID] = player.userID;

            SendReply(player, $"Сообщение для <color=#1E88E5>{reciever.displayName}</color>\n" + message);
            SendReply(reciever, $"Сообщение от <color=#1E88E5>{player.displayName}</color>\n" + message);

            PrintWarning($"{player} -> {reciever}:");
            PrintWarning($"{message}:");

            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", reciever, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, reciever.Connection);
        }

        [ChatCommand("r")]
        void cmdChatR(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "Используйте: /r message");
                return;
            }
            var argList = args.ToList();
            string message = string.Join(" ", argList.ToArray());
            if (message.Length > 125) return;
            ulong recieverUserId;

            if (!pmHistory.TryGetValue(player.userID, out recieverUserId))
            {
                SendReply(player, "Вам / вы ещё не писали!");
                return;
            }
            var reciever = BasePlayer.activePlayerList.FirstOrDefault(p => p.userID == recieverUserId);
            if (reciever == null)
            {
                SendReply(player, "Игрок покинул сервер, сообщение не отправлено!");
                return;
            }

            if (ignoreList.ContainsKey(reciever.userID) && ignoreList[reciever.userID].Contains(player.userID))
            {
                SendReply(player, $"Этот игрок вас игнорирует!");
                return;
            }

            SendReply(player, $"Сообщение для <color=#1E88E5>{reciever.displayName}</color>\n" + message);
            SendReply(reciever, $"Сообщение от <color=#1E88E5>{player.displayName}</color>\n" + message);

            PrintWarning($"{player} -> {reciever}:");
            PrintWarning($"{message}:");


            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", reciever, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, reciever.Connection);
        }

        [ConsoleCommand("UI_MuteHandler")]
        private void consoleMuteHandler(ConsoleSystem.Arg args)
        {
            if (args.Args.Length == 0)
            {
                DrawGUI(args.Player());
                return;
            }

            if (args.Args[0] == "chooseplayer")
            {
                DrawGUI(args.Player(), args.Args[1]);
                return;
            }

            if (args.Args[0] == "unmute")
            {
                UnMutePlayer(args.Player(), ulong.Parse(args.Args[1]));
                return;
            }

            if (args.Args[0].Length != 17)
            {
                CuiHelper.DestroyUi(args.Player(), Layer + ".ChoosePlayer.BG");
                DrawGUI(args.Player(), args.Args[0]);
                return;
            }

            if (args.Args.Length == 3)
            {
                MutePlayer(args.Player(), BasePlayer.Find(args.Args[0]), args.Args[1], args.Args[2]);
                return;
            }
        }

        [ConsoleCommand("UI_ChatSystem")]
        private void consoleUIHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || args.Args.Length == 0)
                return;

            if (args.Args[0].StartsWith("#"))
            {
                if (permission.UserHasPermission(player.UserIDString, Settings.Colors[args.Args[0]]))
                {
                    playerSettings[player.userID].CurrentColor = args.Args[0];
                    player.ChatMessage($"Вы успешно изменили цвет ника!");
                    ChatSettUp(player, "updatecolor");
                }
                else
                {
                    player.ChatMessage($"Вам недостаточно прав для использования данного цвета!");
                    return;
                }
            }

            switch (args.Args[0].ToLower())
            {
                case "hidemessages":
                    {
                        playerSettings[player.userID].HideMessages = bool.Parse(args.Args[1]);
                        ChatSettUp(player, "updatechat");

                        //SendReply(player, $"Вы успешно {(args.Args[1] == "true" ? "выключили" : "включили")} отображение чата!", header:"Успешное изменение настроек");
                        break;
                    }
                case "hidehelpers":
                    {
                        // TODO: Если игрок наиграл 3 часа на сервере
                        playerSettings[player.userID].HideHelpers = bool.Parse(args.Args[1]);
                        ChatSettUp(args.Player(), $"updatehelper");

                        //SendReply(player, $"Вы успешно {(args.Args[1] == "true" ? "выключили" : "включили")} отображение подсказок!", header:"Успешное изменение настроек");
                        break;
                    }
                case "settag":
                    {
                        if (!permission.UserHasPermission(player.UserIDString, Settings.Tags.FirstOrDefault(p => p.Value == args.Args[1]).Key))
                        {
                            SendReply(player, "Вы не можете изменить ваш префикс, у вас недостаточно прав");
                            return;
                        }
                        playerSettings[args.Player().userID].CurrentTag = args.Args[1];
                        ChatSettUp(args.Player(), $"updatetag");

                        //SendReply(player, "Вы успешно изменили ваш префикс!", header:"Успешное изменение настроек");
                        break;
                    }
            }
        }

        #endregion

        #region GUI

        private void ChatSettUp(BasePlayer player, string change = "")
        {
            CuiElementContainer container = new CuiElementContainer();

            if (string.IsNullOrEmpty(change))
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.5 0.7", AnchorMax = "0.5 0.7", OffsetMin = "-204 -85.5", OffsetMax = "204 85.5" },
                    Image = { Color = "0 0 0 0" }
                }, "Overlay", Layer);


                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                    Button = { Color = "0 0 0 0.9", Close = Layer, Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { Text = "" }
                }, Layer);

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = $"0 {0 - Settings.Colors.Count * 0.23}", AnchorMax = "1 1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Header",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0 0.815534", AnchorMax = "1 1.1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Header",
                    Components =
                    {
                        new CuiTextComponent { Text = "<color=#EBECF9>НАСТРОЙКА ИГРОВОГО ЧАТА</color>", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf"},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".TagChoose",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.0139617 0.6592549", AnchorMax = "0.98 0.85" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".TagChoose",
                    Components =
                    {
                        new CuiTextComponent { Text = "ВЫБОР ПРЕФИКСА", Align = TextAnchor.MiddleCenter, FontSize = 19, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".HideChat",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.02 0.24", AnchorMax = "0.15 0.35", OffsetMax = "0 0"  }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".HideChat",
                    Components =
                    {
                        new CuiTextComponent { Text = "ЧАТ", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf"},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".HideHelpers",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.57 0.24", AnchorMax = "0.79 0.35", OffsetMax = "0 0"  }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".HideHelpers",
                    Components =
                    {
                        new CuiTextComponent { Text = "ПОДСКАЗКИ", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf"},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
            }

            if (string.IsNullOrEmpty(change) || change == "updatetag")
            {
                foreach (var check in Settings.Tags)
                    CuiHelper.DestroyUi(player, Layer + $".Tag.{check.Value}");

                int i = 0;
                float width = (float)((2 - 0.0139617) - (Settings.Tags.Count - 1) * 0.03) / Settings.Tags.Count;
                foreach (var check in Settings.Tags)
                {
                    if (Settings.HiddenTags.Contains(check.Key))
                    {
                        if (!permission.UserHasPermission(player.UserIDString, check.Key))
                            continue;
                    }
                    string color = playerSettings[player.userID].CurrentTag == check.Value ? HexToRustFormat("#8eb9ff3c") : "0 0 0 0";
                    string textColor = "1 1 1 1";
                    if (!permission.UserHasPermission(player.UserIDString, check.Key))
                    {
                        color = "0 0 0 0";
                        textColor = "1 1 1 0.2";
                    }

                    container.Add(new CuiElement
                    {

                        Parent = Layer,
                        Name = Layer + $".Tag.{check.Value}",
                        Components =
                        {
                            new CuiImageComponent { Color = color },
                            new CuiRectTransformComponent { AnchorMin = $"{-0.5f + i * width + i * 0.03} 0.40", AnchorMax = $"{-0.5f + (i + 1) * width + i * 0.03} 0.64" }
                        }
                    });

                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".Tag.{check.Value}",
                        Components =
                        {
                            new CuiTextComponent { Text = check.Value.Replace("14", "26"), Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-bold.ttf", Color = textColor },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        FadeOut = 0.4f,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { FadeIn = 0.4f, Command = $"UI_ChatSystem settag {check.Value}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, Layer + $".Tag.{check.Value}");

                    i++;
                }
            }

            // Кнопочка скрыть чат
            if (string.IsNullOrEmpty(change) || change == "updatechat")
            {
                CuiHelper.DestroyUi(player, Layer + ".Chat.BTN");

                bool hidden = playerSettings[player.userID].HideMessages;

                string text = hidden ? "СКРЫТ" : "ВИДЕН";
                string bgColor = hidden ? "1 1 1 0.2" : HexToRustFormat("#8eb9ff3c");

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Chat.BTN",
                    Components =
                    {
                        new CuiImageComponent { Color = bgColor },
                        new CuiRectTransformComponent { AnchorMin = "0.16 0.24", AnchorMax = "0.34 0.35", OffsetMax = "0 0"  }
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = 0.4f,
                    Parent = Layer + ".Chat.BTN",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = 0.4f, Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent { AnchorMin = $"{(hidden ? "0" : "0.82")} 0", AnchorMax = $"{(hidden ? "0.18" : "1")} 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = 0.4f,
                    Parent = Layer + ".Chat.BTN",
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 0.4f, Text = text, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = $"{(hidden ? "0.18" : "0")} 0", AnchorMax = $"{(hidden ? "1" : "0.82")} 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiButton
                {
                    FadeOut = 0.4f,
                    Button = { FadeIn = 0.4f, Color = "0 0 0 0", Command = $"UI_ChatSystem hidemessages {(hidden ? "false" : "true")}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, Layer + ".Chat.BTN");
            }

            if (string.IsNullOrEmpty(change) || change == "updatehelper")
            {
                CuiHelper.DestroyUi(player, Layer + ".Helper.BTN");

                bool hidden = playerSettings[player.userID].HideHelpers;

                string text = hidden ? "СКРЫТЫ" : "ВИДНЫ";
                string bgColor = hidden ? "1 1 1 0.2" : HexToRustFormat("#8eb9ff3c");

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Helper.BTN",
                    Components =
                    {
                        new CuiImageComponent { Color = bgColor },
                        new CuiRectTransformComponent { AnchorMin = "0.80 0.24", AnchorMax = "0.98 0.35", OffsetMax = "0 0"  }
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = 0.4f,
                    Parent = Layer + ".Helper.BTN",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = 0.4f, Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent { AnchorMin = $"{(hidden ? "0" : "0.82")} 0", AnchorMax = $"{(hidden ? "0.18" : "1")} 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = 0.4f,
                    Parent = Layer + ".Helper.BTN",
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 0.4f, Text = text, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = $"{(hidden ? "0.18" : "0")} 0", AnchorMax = $"{(hidden ? "1" : "0.82")} 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiButton
                {
                    FadeOut = 0.4f,
                    Button = { FadeIn = 0.4f, Color = "0 0 0 0", Command = $"UI_ChatSystem hidehelpers {(hidden ? "false" : "true")}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, Layer + ".Helper.BTN");
            }


            if (string.IsNullOrEmpty(change) || change == "updatecolor")
            {
                CuiHelper.DestroyUi(player, Layer + ".ColorChoose");
                foreach (var check in Settings.Colors)
                    CuiHelper.DestroyUi(player, Layer + $".{check.Key}");

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".ColorChoose",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.0139617 0.02", AnchorMax = "0.98 0.18", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".ColorChoose",
                    Components =
                    {
                        new CuiTextComponent { Text = "ВЫБОР ЦВЕТА", Align = TextAnchor.MiddleCenter, FontSize = 19, Font = "robotocondensed-regular.ttf"},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                int t = 0;
                foreach (var check in Settings.Colors)
                {
                    string text = "";
                    if (check.Key == playerSettings[player.userID].CurrentColor)
                        text = "ТЕКУЩИЙ";
                    else if (!permission.UserHasPermission(player.UserIDString, check.Value))
                        text = "НЕДОСТУПНО ДЛЯ ВЫБОРА";

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{0.0139617} {-0.2 - t * 0.23}", AnchorMax = $"{0.98} {0 - t * 0.23}", OffsetMax = $"0 0" },
                        Button = { Command = $"UI_ChatSystem {check.Key}", Color = HexToRustFormat($"{check.Key}3C") },
                        Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" }
                    }, Layer, Layer + $".{check.Key}");
                    t++;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        private void DrawGUI(BasePlayer player, string target = "", string reason = "", string time = "")
        {
            CuiElementContainer container = new CuiElementContainer();

            if (target == "" && reason == "" && time == "")
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.3166667 0.4369213", AnchorMax = "0.6833333 0.5630786" },
                    Image = { Color = "0 0 0 0" }
                }, "Overlay", Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                    Button = { Close = Layer, Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { Text = "" }
                }, Layer);

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Header",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0 0.6752296", AnchorMax = "1 1.1" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Header",
                    Components =
                    {
                        new CuiTextComponent { Text = "ВЫДАЧА БЛОКИРОВКИ ЧАТА", Align = TextAnchor.MiddleCenter, FontSize = 22, Font = "robotocondensed-bold.ttf"},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                string helpText = "Первым делом вам необходимо ввести имя игрока, которому вы хотите выдать блокировку игрового чата";
                container.Add(new CuiElement
                {
                    FadeOut = 0.4f,
                    Name = Layer + ".FirstStep",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 0.4f, Text = helpText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"},
                        new CuiRectTransformComponent { AnchorMin = "0 0.3091741", AnchorMax = "1 0.9" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Input",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#FFFFFF72") },
                        new CuiRectTransformComponent { AnchorMin = "0.005681746 0.03027457", AnchorMax = "0.9943181 0.2871558" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Input",
                    Name = Layer + ".Input.Current",
                    Components =
                    {
                        new CuiInputFieldComponent { FontSize = 16, Align = TextAnchor.MiddleCenter, Command = "UI_MuteHandler ", Text = "ВВЕДИТЕ ИМЯ ИГРОКА" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
            }

            if (target.Length != 17 && reason == "" && time == "")
            {
                for (int i = 0; i < 5; i++)
                {
                    CuiHelper.DestroyUi(player, Layer + $".ChoosePlayer.{i}");
                    CuiHelper.DestroyUi(player, Layer + $".ChoosePlayer.{i}.Text");
                }

                timer.Once(0.5f, () =>
                {
                    int t = 0;
                    var list = BasePlayer.activePlayerList.Where(p => p.displayName.ToLower().Contains(target.ToLower())).Take(5);

                    container.Add(new CuiElement
                    {
                        FadeOut = 0.4f,
                        Parent = Layer,
                        Name = Layer + $".ChoosePlayer.BG",
                        Components =
                        {
                            new CuiImageComponent { Color = "0 0 0 0", FadeIn = 0.4f },
                            new CuiRectTransformComponent { AnchorMin = $"0 {-0.02110124 - list.Count() * 0.3}", AnchorMax = $"1 0", OffsetMax = "0 0"  }
                        }
                    });

                    foreach (var check in list)
                    {
                        bool isMuted = playerSettings[check.userID].IsMuted() > 0;

                        string color = isMuted ? HexToRustFormat("#8BA3E130") : HexToRustFormat("#FFFFFF08");
                        string command = isMuted ? $"UI_MuteHandler unmute {check.userID}" : $"UI_MuteHandler chooseplayer {check.userID}";
                        string close = isMuted ? Layer : "";
                        container.Add(new CuiElement
                        {
                            FadeOut = 0.4f,
                            Parent = Layer,
                            Name = Layer + $".ChoosePlayer.{t}",
                            Components =
                            {
                                new CuiImageComponent { Color = color, FadeIn = 0.4f },
                                new CuiRectTransformComponent { AnchorMin = $"0.005681746 {-0.2706425 - t * 0.3}", AnchorMax = $"0.9943181 {-0.02110124 - t * 0.3}" }
                            }
                        });

                        container.Add(new CuiElement
                        {
                            Parent = Layer + $".ChoosePlayer.{t}",
                            Name = Layer + $".ChoosePlayer.{t}.Text",
                            Components =
                            {
                                new CuiTextComponent { Text = check.displayName + " [" + check.userID + "]", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16},
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                            }
                        });

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            Button = { Command = command, Color = "0 0 0 0", Close = close },
                            Text = { Text = "" }
                        }, Layer + $".ChoosePlayer.{t}");

                        t++;
                    }

                    CuiHelper.AddUi(player, container);
                });
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    CuiHelper.DestroyUi(player, Layer + $".ChoosePlayer.{i}");
                    CuiHelper.DestroyUi(player, Layer + $".ChoosePlayer.{i}.Text");
                    CuiHelper.DestroyUi(player, Layer + $".ChoosePlayer.BG");
                }

                CuiHelper.DestroyUi(player, Layer + $".FirstStep");
                CuiHelper.DestroyUi(player, Layer + $".Input");

                if (reason == "")
                {
                    timer.Once(0.5f, () =>
                    {
                        string helpText = "Выберите одну из предложенных причин блокировки, время блокировки будет автоматически выбрано с учётом причины блокировки!";
                        container.Add(new CuiElement
                        {
                            FadeOut = 0.4f,
                            Name = Layer + ".FirstStep",
                            Parent = Layer,
                            Components =
                            {
                                new CuiTextComponent { FadeIn = 0.4f, Text = helpText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"},
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1.05" }
                            }
                        });

                        int t = 0;

                        container.Add(new CuiElement
                        {
                            FadeOut = 0.4f,
                            Parent = Layer,
                            Name = Layer + $".ChoosePlayer.BG",
                            Components =
                            {
                                new CuiImageComponent { Color = "0 0 0 0", FadeIn = 0.4f },
                                new CuiRectTransformComponent { AnchorMin = $"0 {-0.02110124 - Settings.Reasons.Count() * 0.3}", AnchorMax = $"1 0", OffsetMax = "0 0" }
                            }
                        });

                        foreach (var check in Settings.Reasons)
                        {
                            container.Add(new CuiElement
                            {
                                FadeOut = 0.4f,
                                Parent = Layer,
                                Name = Layer + $".ChoosePlayer.{t}",
                                Components =
                                {
                                    new CuiImageComponent { Color = HexToRustFormat("#FFFFFF08"), FadeIn = 0.4f },
                                    new CuiRectTransformComponent { AnchorMin = $"0.005681746 {-0.2706425 - t * 0.3}", AnchorMax = $"0.9943181 {-0.02110124 - t * 0.3}" }
                                }
                            });

                            container.Add(new CuiElement
                            {
                                Parent = Layer + $".ChoosePlayer.{t}",
                                Name = Layer + $".ChoosePlayer.{t}.Text",
                                Components =
                                {
                                    new CuiTextComponent { Text = check.Key.Replace("+", " ").Replace("+", " ") + $" [{check.Value.Replace("+", " ").Replace("+", " ")}]", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16},
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                                }
                            });

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Button = { Command = $"UI_MuteHandler {target.Replace(" ", "").Replace(" ", "").Replace(" ", "")} {check.Key} {check.Value}", Color = "0 0 0 0", Close = Layer },
                                Text = { Text = "" }
                            }, Layer + $".ChoosePlayer.{t}");

                            t++;
                        }

                        CuiHelper.AddUi(player, container);
                    });
                }


            }



        }

        #endregion

        #region Helpers

        public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
        {
            string result = string.Empty;
            switch (language)
            {
                case "ru":
                    int i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")}";
                        i++;
                    }

                    break;
                case "en":
                    result = string.Format("{0}{1}{2}{3}",
                        time.Duration().Days > 0 ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, " : string.Empty,
                        time.Duration().Hours > 0 ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, " : string.Empty,
                        time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, " : string.Empty,
                        time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}" : string.Empty);

                    if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

                    if (string.IsNullOrEmpty(result)) result = "0 seconds";
                    break;
            }
            return result;
        }

        public static long TimeToSeconds(string time)
        {
            time = time.Replace(" ", "").Replace("d", "d ").Replace("h", "h ").Replace("m", "m ").Replace("s", "s ").TrimEnd(' ');
            var arr = time.Split(' ');
            long seconds = 0;
            foreach (var s in arr)
            {
                var n = s.Substring(s.Length - 1, 1);
                var t = s.Remove(s.Length - 1, 1);
                int d = int.Parse(t);
                switch (n)
                {
                    case "s":
                        seconds += d;
                        break;
                    case "m":
                        seconds += d * 60;
                        break;
                    case "h":
                        seconds += d * 3600;
                        break;
                    case "d":
                        seconds += d * 86400;
                        break;
                }
            }
            return seconds;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }

            return null;
        }

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

        #endregion
    }
}