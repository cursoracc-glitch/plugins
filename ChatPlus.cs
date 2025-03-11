//#define RUST
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Text.RegularExpressions;
using ConVar;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ChatPlus", "RustPlugin.ru - Developed by Vlad-00003", "1.4.0", ResourceId = 82)]
    public class ChatPlus : CovalencePlugin
    {
        /*  covalence.FormatText(msg);
         *  Formatter.ToPlaintext(msg);
         *  Original autor: Unknown
         *  Editor: Vlad-00003
         *  Editor info:
         *    E-mail: Vlad-00003@mail.ru
         *    Vk: vk.com/vlad_00003
         * v1.1.0
         *   Добавлены команды:
         *     chatplus.message {steamid/ник} привилегия
         *     chatplus.name {steamid/ник} привилегия
         *     chatplus.prefix {steamid/ник} привилегия
         *       Данные команды лишь УСТАНАВЛИВАЮТ префикс\цвета игроку. Для использования необходимо выдать игроку 
         *       привилегии. Без привилегий команды работать НЕ будут.
         *       Для использования из консоли игрока требуется привилегия на присваивание префикса и цветов (chatplus.assign)
         *   Добавлена возможноть выдавать мут\использовать команды присваивания цветов\префика игрокам, не находящимся на сервере.
         *   Добавлена комманда mutelist - выводит список текущик игроков, у которых заблокирован чат.
         *   При отключении чата игроку сохраняется не только время, а так же то, кто заблокировал игроку чат и причина,
         *   что полностью отражается как в чате и заблокированного, так и при команде mutelist
         *   Отныне при использовании команды mute причина и время опциональны - можно заблокировать человека навсегда, но с причиной.
         *   Примеры:
         *     "mute vlad" - блокировать игроку чат на всегда. Причина - Not specified
         *     "mute vlad 1d" - блокировать чат игроку на 1 день. Причина - Not specified
         *     "mute vlad Просто потому что я так решил." - блокировать чат игроку навсегда. Причина - "Просто потому что я так решил."
         *     "mute vlad 1d Это весомоя причина" - блокировать чат игроку на 1 день. Причина - "Это весомоя причина"
         *   Разделены привилегии на использование команд mute/unmute на две разные привилегии - chatplus.mute и chatplus.unmute соответственно.
         *   Небольшая чистка и оптимизация кода.
         * V1.1.1
         *   Стандартная причина мута (если не указана) выведена в файл конфигурации
         * v1.1.2
         *   Добавлен API - private void API_RegisterThirdPartyTitle(Plugin plugin, Func<IPlayer, string> titleGetter)
         * V1.1.3
         *   Удалена ненужная проверка
         * V1.2.0
         *   Плагин переведён на тип Covalence - Теперь он может работать не только в Rust-е. Требуется проверка на других играх.
         *     Так же теперь плагин поддерживает стандартизированное форматирование ([#HEXCOLOR][/#] [+SIZE][/+])
         *   Исправлена ошибка при удалении игрока из чёрного списка - теперь сообщение о том, что игрока удалили появляется в нужное время.
         *   Если игрок указал неверный параметр в команде /chat, то ему будет выведена справка. Раньше не выводилось ничего.
         *   Если у вас возникнут ошибки при обновлении до данной версии вы можете сделать следующие - очистить BlackList у каждого игрока 
         *     в файле данных. Или просто удалите ChatPlus_Players.json из папки oxide/data.
         *   Для полной совместимости с Clans Reborn вам нужно поправить функцию OnPluginLoaded в плагине Clans:
         *   void OnPluginLoaded(Plugin plugin)
         *   {
         *       if (plugin.Title != "Better Chat" || plugin.Title != "ChatPlus") return;
         *       if (enableClanTagging) Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
         *   }
         * V1.2.1
         *   Добавлено лоигрование режимов администратора и модератора чата.
         * V1.2.2
         *   Исправлена ошибка во флаге, Rust заменено на RUST. Из-за этого даже в игре Rust использовались мультиобразные вызовы,
         *   что приводило к отсутствию звуков личных сообщений и отсутствию иконок игроков в чате
         *   Так же исправлена возможность отправлять ЛС самому себе если отображаемое имя на сервере игры RUST отличается от 
         *   фактического имени в базе
         *   Добавлен ResourceId
         * V1.3.0
         *   Добавлена возможность использовать PM через консоль и отвечать через r. 
         *     Теперь вы можете отправлять личные сообщения игрокам из консоли, а они могут вам отвечать!
         *   Исправлена ошибка в языковых файлах. Команд присваивания префикса\цветов имён\сообщений в чате
         *   Полностью переписаны языковые файл (Изменён принцип форматирования на стандартизированное представление):
         *     <color=#HEX></color> заменено на [#HEX][/#] и <size=N></size> заменено на [+N][/+]
         *   Удалите ваши старые языковые файлы и перезапустите плагин. 
         *   Данное действие ОБЯЗАТЕЛЬНО!
         *   Добавлены новые строки файла конфигурации(пересоздавать ничего не нужно - 
         *     они автоматически будут добавлены в конфиг при первом запуске обновлённой версии):
         *      M. Имя консоли при отправке личных сообщений и отключении чата из консоли
         *      N. Формат отправки сообщений из консоли командой say
         *  Имя консоли это то имя, которые будет выводиться при блокировке чата из консоли сервера.
         *  Так же теперь изменению подверглась команда global.say, которую, помимо консоли, могут вызывать администраторы из консоли игры:
         *    Теперь вы можете настроить формат вывода сообщений в чат при использовании данной команды!
         *    {0} - Это имя консоли, из пераметра выше. {1} - само сообщение.
         * V1.3.1
         *   Добавлена возможность отображение аватарок игрока при отправке личного сообщения(ТОЛЬКО Rust!)
         * V1.3.2
         *   Переделана система логгирования, теперь так же ведётся лог личных сообщений(что крайне мешает когда ты переписываешься через консоль
         *     я подумаю как это сделать красивее).
         *   Оптимизация функций, отвечающих за отображение иконок в личных переписках.
         *   Проверка на консоль сервера вынесена отдельно
         * V1.3.3
         *   Теперь в списках имён\префиксов\цветов сообщений отображается default, дабы игрок мог вернуть цвет к стандарту без команды /chat reset
         * v1.3.4
         *   Исправление v1.3.3 - теперь default видят только те, у кого на данный момент выбран не стандартный цвет\префикс
         * v1.3.5
         *   Добавлено верное форматирование в функцию BroadcastChat - вывод сообщений в чат о мутах. Теперь она так же поддерживает стандартизированное форматирование.
         * v1.3.6
         *   Теперь верно получается языковой файл справки о команде /chat
		 * v1.3.7
		 *   Попытка исправить NRE в SendChat при вызове SendConsoleCommand
         * v1.3.71
         *   Убрана устаревшая функция Reply, принимающая как аргумент BasePlayer, упрощён доступ к данным пользователя в команде /chat
         * v1.3.8
         *   Добавлена возможность скрывать имя администратора, который заблокировал доступ к чату.
         * v1.3.9
         *   Добавлен API IsPlayerMuted(object ID) - вернёт true если игроку недоступен чат
         *   Добавлен хук OnChatPlusMessage(Dictionary<string,object> dict), доступные параметры:
         *     ["Player"] - Iplayer - игрок, написавший в чат
         *     ["Message"] - string - его сообщение в чат
         *     ["CensoredMessage"] - string - версия его сообщения с цензурой
         *     ["Prefixes"] - string - все префиксы игрока, разделённые одним пробелом.
         * v1.3.10
         *   Исправил ошибку в хуке
         * v1.3.11
         *   Добавлена новая строка в языковой файл - сообщение о том, что игрока нет в чёрном списке при использовании /chat ignore remove <name>
         *   Исправлена ошибка, по которой сообщение в чат могло отправляться игроку, которого нет на сервере, что приводило к сбоям в работе плагина.
         *   Проверка IsServerConsole по ID игрока заменена на поле IsServer класса IPlayer.
         * v1.4.0
         *   Убрана возможность форматирования текста игроками(перед форматированием сообщение очищается от тэегов)
         *   Поправлена цензура чата. Теперь если исключение находиться на нулевой позиции оно так же будет учтено.
         *   Исправлена ошибка из-за которой время блокировки чата не сохранялось при перезагрузках.
         *      Всем администраторам - пересмотрите список тех, кому блокировали чат. Возможно часть из этих блокировок уже давно должна была закончится =)
         */

        #region Global vars
        private ChatConfig config;
        private static ChatPlus m_Instance;
        private static Dictionary<string, MuteData> mutes = new Dictionary<string, MuteData>();
        private Dictionary<string, PlayerData> PlayersData = new Dictionary<string, PlayerData>();
        private PlayerData defaultData = new PlayerData();
        private bool GlobalMute = false;
        private bool HasExpired;
        private IPlayer ConsoleIPly;

        DynamicConfigFile mutes_File = Interface.Oxide.DataFileSystem.GetFile("ChatPlus_Mutes");
        DynamicConfigFile players_File = Interface.Oxide.DataFileSystem.GetFile("ChatPlus_Players");

        public static Dictionary<Plugin, Func<IPlayer, string>> ThirdPartyTitles = new Dictionary<Plugin, Func<IPlayer, string>>();
        #endregion

        #region Initialization and quiting
        void OnServerInitialized()
        {
            m_Instance = this;
            LoadMessages();
            LoadConfig();
            LoadData();
            config.RegisterPerms();
            timer.Repeat(10, 0, () =>
            {
                List<string> expired = mutes.Where(m => m.Value.Expired).Select(m => m.Key).ToList();
                foreach (string userID in expired)
                {
                    IPlayer player = players.FindPlayerById(userID.ToString());
                    if (player == null) continue;
                    mutes.Remove(userID);
                    BroadcastChat("MUTE.EXPIRED", player.Name);
                    Log(LogType.Mute, string.Format(GetMsg("MUTE.EXPIRED"), player.Name));
                    if (!HasExpired)
                        HasExpired = true;
                }
                if (HasExpired)
                {
                    SaveMutes();
                    HasExpired = false;
                }
            });
        }

        void Unload()
        {
            OnServerSave();
        }
        void OnServerSave()
        {
            SaveMutes();
            players_File.WriteObject(PlayersData.Where(p => !p.Value.Equals(defaultData)).ToDictionary(p => p.Key, p => p.Value));
        }
        private void SaveMutes()
        {
            mutes_File.WriteObject(mutes);
        }
        void LoadData()
        {
            mutes = mutes_File.ReadObject<Dictionary<string, MuteData>>();
            PlayersData = players_File.ReadObject<Dictionary<string, PlayerData>>();
        }
        #endregion

        #region Chat handling
        object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            IPlayer sender = player.IPlayer;
            if (GlobalMute)
            {
                if (!CanMuteAll(sender))
                {
                    Reply(sender, "MUTE.ALL.DISABLED");
                    return false;
                }
            }
            
            Puts($"[{channel}] {player.displayName}[{player.userID}]: {message}");
            var pData = GetPlayerData(sender);
            if (pData.IsAdmin == true)
            {
                Log(LogType.Chat, $"[ADMIN MODE] {sender.Name}({sender.Id}): {message}");
                bool outer;
                var cens = CensorBadWords(message, out outer);
                cens = string.Format(config.adminPrivilages.AdminFormat, cens);
                message = string.Format(config.adminPrivilages.AdminFormat, message);
                SendChat(player, "", channel, message, cens);
                return false;
            }
            if (pData.IsModer == true)
            {
                Log(LogType.Chat, $"[MODERATOR MODE] {sender.Name}({sender.Id}): {message}");
                bool outer;
                var cens = CensorBadWords(message, out outer);
                cens = string.Format(config.adminPrivilages.ModerFormat, cens);
                message = string.Format(config.adminPrivilages.ModerFormat, message);
                SendChat(player, "", channel, message, cens);
                return false;
            }
            if (SpamCheck(sender, message) != null) return true;
            if (MuteCheck(sender) != null) return true;

            if (config.CapsBlock)
            {
                message = RemoveCaps(message);
            }
            message = RemoveTags(message);
            bool mute;
            var censorMessage = CensorBadWords(message, out mute);
            if (config.BadWordsBlock & mute)
            {
                Mute(sender.Id, sender.Name, new TimeSpan(0, 0, config.MuteBadWordsDefault), config.MuteReasonAutoMute, "ChatPlus");
            }
            var prefix = config.Get(config.prefixes, pData.Prefix).Format;
            var nameColor = config.Get(config.names, pData.NameColor).Format;
            var messageColor = config.Get(config.messages, pData.MessageColor).Format;

            var name = string.Format(nameColor, sender.Name);
            message = string.Format(messageColor, message);
            censorMessage = string.Format(messageColor, censorMessage);
            foreach (var thirdPartyTitle in ThirdPartyTitles)
            {
                try
                {
                    string title = thirdPartyTitle.Value(sender);

                    if (!string.IsNullOrEmpty(title))
                    {
                        prefix = title + " " + prefix;
                    }
                }
                catch (Exception ex)
                {
                    PrintError($"Error when trying to get third-party title from plugin '{thirdPartyTitle.Key.Title}'{Environment.NewLine}{ex}");
                }
            }
            Dictionary<string,object> hookDictionary = new Dictionary<string, object>()
            {
                ["Player"] = sender,
                ["Message"] = message,
                ["CensoredMessage"] = censorMessage,
                ["Prefixes"] = prefix
            };
            foreach (var plugin in plugins.GetAll())
            {
                object result = plugin.CallHook("OnChatPlusMessage", hookDictionary);
                if (result is Dictionary<string, object>)
                {
                    try
                    {
                        var dict = (Dictionary<string, object>)result;
                        sender = dict["Player"] as IPlayer;
                        message = dict["Message"] as string;
                        censorMessage = dict["CensoredMessage"] as string;
                        prefix = dict["Prefixes"] as string;
                    }
                    catch (Exception ex)
                    {
                        PrintWarning($"Plugin '{plugin.Title}({plugin.Version})' failed to modify the ChatPlus message data. Error:\n{ex.Message}");
                    }
                }
                else if(result != null)
                    return null;
            }
            if (prefix.Length > 0)
                name = $"{prefix} {name}";
            SendChat(player, name, channel, message, censorMessage, sender.Id);
            Log(LogType.Chat, $"{name}[{sender.Id}]: {message}");
            return false;
        }
        private string RemoveTags(string message)
        {
            List<string> forbiddenTags = new List<string>{
                "</color>",
                "</size>",
                "<b>",
                "</b>",
                "<i>",
                "</i>"
            };
            message = Regex.Replace(message, "(<color=.+?>)", string.Empty, RegexOptions.IgnoreCase);
            message = Regex.Replace(message, "(<size=.+?>)", string.Empty, RegexOptions.IgnoreCase);
            foreach (string tag in forbiddenTags)
                message = Regex.Replace(message, tag, string.Empty, RegexOptions.IgnoreCase);
            return Formatter.ToPlaintext(message);
        }
        string RemoveCaps(string message)
        {
            var ss = message.Split(' ');
            for (int j = 0; j < ss.Length; j++)
                for (int i = 1; i < ss[j].Length; i++)
                {
                    var sym = ss[j][i];
                    if (char.IsLower(sym)) continue;
                    ss[j] = ss[j].Remove(i, 1);
                    ss[j] = ss[j].Insert(i, char.ToLower(sym).ToString());
                }
            return string.Join(" ", ss);
        }

        bool? MuteCheck(IPlayer sender)
        {
            if (MuteData.IsMuted(sender.Id))
            {
                MuteData md = mutes[sender.Id];
                Reply(sender, "YOU.MUTED", md.Initiator, md.Reason, md.Remain);
                return true;
            }
            return null;
        }
        
        bool? SpamCheck(IPlayer sender, string message)
        {
            if (message.Length > 500)
            {
                sender.Kick(GetMsg("CHAT.TOOMUCH", sender));
                return false;
            }
            if (message.Length > 100)
            {
                Reply(sender, "CHAT.SPAM");
                return false;
            }
            return null;
        }
        public string CensorBadWords(string input, out bool found)
        {
            found = false;
            string temp = input.ToLower();
            foreach (var swear in config.badWords)
            {
                var firstIndex = temp.IndexOf(swear.Key);
                if (firstIndex >= 0 && swear.Value.All(exception => temp.IndexOf(exception) <= 0))
                    while (firstIndex < input.Length && input[firstIndex] != ' ')
                    {
                        input = input.Remove(firstIndex, 1);
                        input = input.Insert(firstIndex, "*");
                        firstIndex++;
                        found = true;
                    }
            }
            return input;
        }
        #endregion

        #region Logging
        enum LogType
        {
            Chat,
            Mute,
            PM,
            ServerConsole
        }
        private void Log(LogType type, string msg)
        {
            switch (type)
            {
                case LogType.Chat:
                    Interface.Oxide.RootLogger.Write(Oxide.Core.Logging.LogType.Info, $"[CHAT] {Formatter.ToPlaintext(msg)}");
                    return;
                case LogType.Mute:
                    Interface.Oxide.RootLogger.Write(Oxide.Core.Logging.LogType.Info, $"[MUTE] {Formatter.ToPlaintext(msg)}");
                    return;
                case LogType.PM:
                    Interface.Oxide.RootLogger.Write(Oxide.Core.Logging.LogType.Info, $"[PM] {Formatter.ToPlaintext(msg)}");
                    return;
                case LogType.ServerConsole:
                    Interface.Oxide.RootLogger.Write(Oxide.Core.Logging.LogType.Info, $"[ServerConsole] {Formatter.ToPlaintext(msg)}");
                    return;
            }
        }
        #endregion

        #region Broadcasting
        void SendChat(BasePlayer players, string name, Chat.ChatChannel channel, string message, string censorMessage = "", string userId = "0")
        {
            name = covalence.FormatText(name);
            message = covalence.FormatText(message);
            censorMessage = covalence.FormatText(censorMessage);
            if (string.IsNullOrEmpty(censorMessage)) censorMessage = message;
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (channel == Chat.ChatChannel.Global)
                {
                    string msg = GetPlayerData(player).Censor ? censorMessage : message;
                    player?.SendConsoleCommand("chat.add", channel, userId,
                        string.IsNullOrEmpty(name) ? $"{msg}" : $"{name}: {msg}");
                }
            }

            if (channel == Chat.ChatChannel.Team)
            {
                RelationshipManager.PlayerTeam team = BasePlayer.Find(userId).Team;
                if (team == null || team.members.Count == 0)
                {
                    return;
                }

                foreach (ulong userId2 in team.members)
                {
                    string msg = GetPlayerData(players).Censor ? censorMessage : message;
                    BasePlayer basePlayer = RelationshipManager.FindByID(userId2);
                    basePlayer.SendConsoleCommand("chat.add", (int) channel, players.userID,
                        string.IsNullOrEmpty(name) ? $"{msg}" : $"{name}: {msg}");
                }
            }
        }
        void BroadcastChat(string langKey, params object[] args)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.ChatMessage(covalence.FormatText(string.Format(GetMsg(langKey, player), args)));
            }
        }
        private void Reply(IPlayer player, string langkey, params object[] args)
        {
            string message = string.Format(GetMsg(langkey, player.Id), args);
            message = player.IsServer ? Formatter.ToPlaintext(message) : covalence.FormatText(message);
            if(player.IsServer | player.IsConnected)
                player.Reply(message);
        }
        #endregion

        #region Sub functions
        void Mute(string userID, string name, TimeSpan? time = null, string reason = null, string sender = null)
        {
            if (string.IsNullOrEmpty(sender)) sender = "Not specified";
            if (string.IsNullOrEmpty(reason)) reason = config.MuteReason;
            string log;
            if (!time.HasValue)
            {
                mutes[userID] = new MuteData(sender, reason);
                BroadcastChat("USER.MUTED.REASON", sender, name, reason, "Unlimited");
                log = string.Format(GetMsg("USER.MUTED.LOG"), sender, name, "Unlimited", reason);
            }
            else
            {
                mutes[userID] = new MuteData(sender, reason, time.Value);
                BroadcastChat("USER.MUTED.REASON", sender, name, reason, MuteData.TimeToString(time.Value));
                log = string.Format(GetMsg("USER.MUTED.LOG"), sender, name, MuteData.TimeToString(time.Value), reason);
            }
            Log(LogType.Mute, log);
            SaveMutes();
        }
        private Dictionary<string, string> GetPlayers(string NameOrID)
        {
            var pl = players.FindPlayers(NameOrID).ToList();
            return pl.Select(p => new KeyValuePair<string, string>(p.Id, p.Name)).ToDictionary(x => x.Key, x => x.Value);
        }
        private void SendChatHelp(IPlayer player)
        {
            string msg = GetMsg("CMD.CHAT.HELP", player.Id);
            if (CanMuteAll(player)) msg += GetMsg("CMD.MUTE.ALL.HELP", player.Id);
            if (IsAdmin(player)) msg += GetMsg("CMD.CHAT.HELP.PERMISSION.ADMIN", player.Id);
            if (IsModerator(player)) msg += GetMsg("CMD.CHAT.HELP.PERMISSION.MODERATOR", player.Id);
            msg = player.IsServer ? Formatter.ToPlaintext(msg) : covalence.FormatText(msg);
            player.Message(msg);
        }
        #endregion

        #region Flags
        bool IsModerator(IPlayer player) => PermissionService.HasPermission(player.Id, config.adminPrivilages.ModerPermiss) || player.IsAdmin;//player.net.connection.authLevel >= 1;
        bool IsAdmin(IPlayer player) => PermissionService.HasPermission(player.Id, config.adminPrivilages.AdminPermiss) || player.IsAdmin;//player.net.connection.authLevel >= 2;
        bool CanMute(IPlayer player) => PermissionService.HasPermission(player.Id, config.adminPrivilages.MutePermiss) || player.IsAdmin;//player.net.connection.authLevel >= 1;
        bool CanMuteAll(IPlayer player) => PermissionService.HasPermission(player.Id, config.adminPrivilages.MuteAllPermiss) || player.IsAdmin;//player.net.connection.authLevel >= 2;
        bool CanAssign(IPlayer player) => PermissionService.HasPermission(player.Id, config.adminPrivilages.AssignPermiss) || player.IsAdmin;//player.net.connection.authLevel >= 2;
        bool CanUnMute(IPlayer player) => PermissionService.HasPermission(player.Id, config.adminPrivilages.UnMutePermiss) || player.IsAdmin;//player.net.connection.authLevel >= 1;
        #endregion

        #region Commands
#if RUST
        object OnServerCommand(ConsoleSystem.Arg arg, Chat.ChatChannel channel)
        {
            if (arg?.cmd?.FullName != null && arg?.cmd?.FullName == "global.say")
            {
                if (!arg.HasArgs())
                    return false;
                string message = string.Join(" ", arg.Args);

                bool outer;
                var cens = CensorBadWords(message, out outer);
                cens = string.Format(config.ConsoleFormat, config.ConsoleName, cens);
                message = string.Format(config.ConsoleFormat, config.ConsoleName, message);
                Log(LogType.ServerConsole, message);
                SendChat(arg.Player(), "", channel, message, cens);
                return true;
            }
            else return null;
        }
#endif

        [Command("chatplus.prefix")]
        private void cmdConsolePrefix(IPlayer player, string cmd, string[] Args)
        {
            if (!CanAssign(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            if (Args == null || Args.Length < 2)
            {
                Reply(player, "CMD.HELP.PREFIX");
                return;
            }
            var recivers = GetPlayers(Args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.FOUND", Args[0]);
                return;
            }
            if (recivers.Count > 1)
            {
                Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var target = recivers.First();
            var pdata = GetPlayerData(target.Key);
            if (!PermissionService.HasPermission(target.Key, Args[1]))
            {
                Reply(player, "NO.PERMISSION.PLAYER");
                return;
            }
            pdata.Prefix = Args[1];
            Reply(player, "PREFIX.CHANGED", Args[1]);
        }
        [Command("chatplus.name")]
        private void cmdConsoleName(IPlayer player, string cmd, string[] Args)
        {
            if (!CanAssign(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            if (Args == null || Args.Length < 2)
            {
                Reply(player, "CMD.HELP.NAME");
                return;
            }
            var recivers = GetPlayers(Args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.FOUND", Args[0]);
                return;
            }
            if (recivers.Count > 1)
            {
                Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var target = recivers.First();
            var pdata = GetPlayerData(target.Key);
            if (!PermissionService.HasPermission(target.Key, Args[1]))
            {
                Reply(player, "NO.PERMISSION.PLAYER");
                return;
            }
            pdata.NameColor = Args[1];
            Reply(player, "NAME.COLOR.CHANGED", Args[1]);
        }
        [Command("chatplus.message")]
        private void cmdConsoleMessage(IPlayer player, string cmd, string[] Args)
        {
            if (!CanAssign(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            if (Args == null || Args.Length < 2)
            {
                Reply(player, "CMD.HELP.MESSAGE");
                return;
            }
            var recivers = GetPlayers(Args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.FOUND", Args[0]);
                return;
            }
            if (recivers.Count > 1)
            {
                Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var target = recivers.First();
            var pdata = GetPlayerData(target.Key);
            if (!PermissionService.HasPermission(target.Key, Args[1]))
            {
                Reply(player, "NO.PERMISSIO.PLAYERN");
                return;
            }
            pdata.MessageColor = Args[1];
            Reply(player, "MESSAGE.COLOR.CHANGED", Args[1]);
        }
        [Command("muteall")]
        private void cmdChatMuteAll(IPlayer player, string cmd, string[] Args)
        {
            if (!CanMuteAll(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            if (GlobalMute)
            {
                BroadcastChat("MUTE.ALL.ENABLED");
                GlobalMute = false;
                return;
            }
            BroadcastChat("MUTE.ALL.DISABLED");
            GlobalMute = true;
            return;
        }
        [Command("mutelist")]
        void cmdChatMuteList(IPlayer player, string cmd, string[] Args)
        {
            if (!CanMute(player) || !CanUnMute(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            string msg;
            if (mutes.Count == 0)
                msg = GetMsg("MUTE.LIST.NOONE");
            else
            {
                msg = GetMsg("MUTE.LIST.HEAD");
                foreach (var mute in mutes)
                {
                    msg += "\n" + string.Format(GetMsg("MUTE.LIST.BODY", player.Id), mute.Value.Initiator, $"{players.FindPlayerById(mute.Key.ToString()).Name} ({mute.Key})", mute.Value.Reason, mute.Value.Remain);
                }
            }
            player.Reply(msg);
        }
        [Command("mute")]
        void cmdChatMute(IPlayer player, string cmd, string[] args)
        {
            if (!CanMute(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            if (args.Length < 1)
            {
                Reply(player, "CMD.MUTE.HELP");
                return;
            }
            var recivers = GetPlayers(args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.FOUND", args[0]);
                return;
            }
            if (recivers.Count > 1)
            {
                Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var mutePlayer = recivers.First();
            if (MuteData.IsMuted(mutePlayer.Key))
            {
                Reply(player, "USER.ALREADY.MUTED", mutePlayer.Value, mutes[mutePlayer.Key].Reason, mutes[mutePlayer.Key].Remain);
                return;
            }
            TimeSpan time;
            string sender = player.IsServer ? config.ConsoleName : player.Name;
            if (config.adminPrivilages.HideAdmins.HasValue && config.adminPrivilages.HideAdmins.Value && !player.IsServer)
                sender = config.adminPrivilages.AdminReplace;
            if (args.Length == 1)
            {
                Mute(mutePlayer.Key, mutePlayer.Value, null, null, sender);
                return;
            }
            if (MuteData.StringToTime(args[1], out time))
            {
                Mute(mutePlayer.Key, mutePlayer.Value, time, string.Join(" ", args.Skip(2).ToArray()), sender);
                return;
            }
            Mute(mutePlayer.Key, mutePlayer.Value, null, string.Join(" ", args.Skip(1).ToArray()), sender);
        }
        [Command("unmute")]
        void cmdChatUnMute(IPlayer player, string cmd, string[] args)
        {
            if (!CanUnMute(player))
            {
                Reply(player, "NO.ACCESS");
                return;
            }
            if (args.Length == 0)
            {
                Reply(player, "CMD.UNMUTE.HELP");
                return;
            }
            var recivers = GetPlayers(args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.FOUND", args[0]);
                return;
            }
            recivers = recivers.Where(p => mutes.ContainsKey(p.Key)).ToDictionary(x => x.Key, x => x.Value);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.MUTED");
                return;
            }
            if (recivers.Count > 1)
            {
                Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var mutePlayer = recivers.First();
            mutes.Remove(mutePlayer.Key);
            BroadcastChat("USER.UNMUTED", player.Name, mutePlayer.Value);
            string log = GetMsg("USER.UNMUTED.LOG");
            log = string.Format(log, player.Name, mutePlayer.Value);
            Log(LogType.Mute, log);
            SaveMutes();
        }
        [Command("chat")]
        void cmdChat(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatHelp(player);
                return;
            }
            var playerData = GetPlayerData(player);
            switch (args[0])
            {
                case "reset":
                    PlayersData[player.Id] = new PlayerData();
                    Reply(player, "RESET.SUCCESSFULL");
                    return;
                case "admin":
                    if (!IsAdmin(player))
                    {
                        Reply(player, "NO.ACCESS");
                        return;
                    }
                    if (playerData.IsAdmin)
                    {
                        playerData.IsAdmin = false;
                        Reply(player, "ADMIN.DISABLE");
                        return;
                    }
                    playerData.IsAdmin = true;
                    Reply(player, "ADMIN.ENABLE");
                    return;
                case "moder":
                    if (!IsModerator(player))
                    {
                        Reply(player, "NO.ACCESS");
                        return;
                    }
                    if (playerData.IsModer)
                    {
                        playerData.IsModer = false;
                        Reply(player, "MODERATOR.DISABLE");
                        return;
                    }
                    playerData.IsModer = true;
                    Reply(player, "MODERATOR.ENABLE");
                    return;
                case "censor":
                    if (playerData.Censor)
                    {
                        playerData.Censor = false;
                        Reply(player, "CENSOR.DISABLED");
                        return;
                    }
                    playerData.Censor = true;
                    Reply(player, "CENSOR.ENABLED");
                    return;
                case "ignore":
                    if (args.Length == 2 && args[1] == "list")
                    {
                        if (playerData.BlackList.Count == 0)
                        {
                            Reply(player, "IGNORE.LIST.IS.EMPTY");
                            return;
                        }
                        Reply(player, "IGNORE.LIST", string.Join(", ", playerData.BlackList.Select(p => GetPlayerData(p).Name).ToArray()));
                        return;
                    }
                    if (args.Length < 3 || (args[1] != "add" && args[1] != "remove"))
                    {
                        Reply(player, "CMD.CHAT.IGNORE.HELP");
                        return;
                    }
                    bool mode = args[1] == "add" ? true : false;
                    var reply = 777;
                    var recivers = players.FindPlayers(args[2]);
                    if (recivers == null || !recivers.Any())
                    {
                        Reply(player, "PLAYER.NOT.FOUND", args[2]);
                        return;
                    }
                    if (recivers.Count() > 1)
                    {
                        Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", recivers.Select(p => $"{p.Name} ({p.Id})").ToArray()));
                        return;
                    }
                    var ignorePlayer = recivers.First();
                    if (mode)
                    {
                        if (!playerData.BlackList.Contains(ignorePlayer.Id))
                        {
                            playerData.BlackList.Add(ignorePlayer.Id);
                            Reply(player, "USER.ADD.IGNORE.LIST", ignorePlayer.Name);
                            if (ignorePlayer.IsConnected)
                                Reply(ignorePlayer, "YOU.ADD.IGNORE.LIST", player.Name);
                            return;
                        }
                        Reply(player, "USER.IS.IGNORE.LIST", ignorePlayer.Name);
                        return;
                    }
                    else
                    {
                        if (playerData.BlackList.Contains(ignorePlayer.Id))
                        {
                            playerData.BlackList.Remove(ignorePlayer.Id);
                            Reply(player, "USER.REMOVE.IGNORE.LIST", ignorePlayer.Name);
                            Reply(ignorePlayer, "YOU.REMOVE.IGNORE.LIST", player.Name);
                            return;
                        }
                        Reply(player, "NOT.ON.IGNORE.LIST",ignorePlayer.Name);
                        return;
                    }
                case "sound":
                    if (args.Length == 1 || (args[1] != "on" && args[1] != "off"))
                    {
                        Reply(player, "CMD.CHAT.SOUND.HELP");
                        return;
                    }
                    bool pmSound = args[1] == "on";
                    playerData.PMSound = pmSound;
                    if (pmSound)
                    {
                        Reply(player, "SOUND.ENABLED");
                        return;
                    }
                    else
                    {
                        Reply(player, "SOUND.DISABLED");
                        return;
                    }
                case "prefix":
                    var aviablePrefixes = config.prefixes
                        .Where(p => PermissionService.HasPermission(player.Id, p.Perm) && playerData.Prefix != p.Perm
                        || p.Perm == "chatplus.default" && playerData.Prefix != p.Perm).ToList();
                    if (aviablePrefixes.Count == 0)
                    {
                        Reply(player, "NO.AVAILABLE.PREFIXS");
                        return;
                    }
                    if (args.Length == 1)
                    {
                        Reply(player, "AVAILABLE.COLORS.PREFIX",
                            string.Join(", ", aviablePrefixes.Select(p => $"{p.Arg}({covalence.FormatText(p.Format)})").ToArray()));
                        return;
                    }
                    var selectedPrefix = aviablePrefixes.FirstOrDefault(p => p.Arg == args[1]);
                    if (selectedPrefix == null)
                    {
                        Reply(player, "PREFIX.NOT.FOUND", args[1]);
                        return;
                    }
                    playerData.Prefix = selectedPrefix.Perm;
                    Reply(player, "PREFIX.CHANGED", selectedPrefix.Arg);
                    return;
                case "name":
                    var aviableNameColors = config.names
                        .Where(p => PermissionService.HasPermission(player.Id, p.Perm) && playerData.NameColor != p.Perm
                        || p.Perm == "chatplus.default" && playerData.NameColor != p.Perm).ToList();
                    if (aviableNameColors.Count == 0)
                    {
                        Reply(player, "NO.AVAILABLE.COLORS");
                        return;
                    }
                    if (args.Length == 1)
                    {
                        Reply(player, "AVAILABLE.COLORS.NAME",
                            string.Join(", ", aviableNameColors.Select(p => covalence.FormatText(string.Format(p.Format, p.Arg))).ToArray()));
                        return;
                    }
                    var selectedNameColor = aviableNameColors.FirstOrDefault(p => p.Arg == args[1]);
                    if (selectedNameColor == null)
                    {
                        Reply(player, "COLOR.NOT.FOUND", args[1]);
                        return;
                    }
                    playerData.NameColor = selectedNameColor.Perm;
                    Reply(player, "NAME.COLOR.CHANGED", selectedNameColor.Arg);
                    return;
                case "message":
                    var aviableMessageColors = config.messages
                        .Where(p => PermissionService.HasPermission(player.Id, p.Perm) && playerData.MessageColor != p.Perm
                        || p.Perm == "chatplus.default" && playerData.MessageColor != p.Perm).ToList();
                    if (aviableMessageColors.Count == 0)
                    {
                        Reply(player, "NO.AVAILABLE.COLORS");
                        return;
                    }
                    if (args.Length == 1)
                    {
                        Reply(player, "AVAILABLE.COLORS.MESSAGE",
                            string.Join(", ", aviableMessageColors.Select(p => covalence.FormatText(string.Format(p.Format, p.Arg))).ToArray()));
                        return;
                    }
                    var selectedMessageColor = aviableMessageColors.FirstOrDefault(p => p.Arg == args[1]);
                    if (selectedMessageColor == null)
                    {
                        Reply(player, "COLOR.NOT.FOUND", args[1]);
                        return;
                    }
                    playerData.MessageColor = selectedMessageColor.Perm;
                    Reply(player, "MESSAGE.COLOR.CHANGED", selectedMessageColor.Arg);
                    return;
                default:
                    SendChatHelp(player);
                    return;
            }
        }
        #endregion

        #region Private messaging
#if RUST
        private bool PmAvatar() => config.PMAva.HasValue && config.PMAva.Value;
        private void RustChat(IPlayer player, string langkey, string avatar, params object[] args)
        {
            var msg = string.Format(GetMsg(langkey, player.Id), args);
            player.Command("chat.add 0", PmAvatar() ? avatar : "0", covalence.FormatText(msg));
        }
#endif
        Dictionary<string, string> pmHistory = new Dictionary<string, string>();
        [Command("pm")]
        void cmdChatPM(IPlayer player, string cmd, string[] args)
        {
            if (args.Length < 2)
            {
                Reply(player, "CMD.PM.HELP");
                return;
            }
            var argList = args.ToList();
            argList.RemoveAt(0);
            string message = string.Join(" ", argList.ToArray());
            var receivers = players.Connected.Where(p => p.Name.ToLower()
            .Contains(args[0].ToLower()) || p.Id == args[0]).ToList();
            if (receivers == null || receivers.Count == 0)
            {
                Reply(player, "PLAYER.NOT.FOUND", args[0]);
                return;
            }
            if (receivers.Count > 1)
            {
                Reply(player, "MULTIPLE.PLAYERS.FOUND", string.Join("\n", receivers.Select(p => $"{p.Name} ({p.Id})").ToArray()));
                return;
            }
            IPlayer receiver = receivers[0];
            if (receiver.Id == player.Id)
            {
                Reply(player, "PM.SELF");
                return;
            }
            if (GetPlayerData(receiver).BlackList.Contains(player.Id))
            {
                Reply(player, "PM.YOU.ARE.BLACK.LIST", receiver.Name);
                return;
            }
            pmHistory[player.Id] = receiver.Id;
            pmHistory[receiver.Id] = player.Id;
#if RUST
            string msg;

            if (player.IsServer)
            {
                ConsoleIPly = player;
                Reply(player, "PM.SENDER.FORMAT", receiver.Name, message);
                RustChat(receiver, "PM.RECEIVER.FORMAT", "0", config.ConsoleName, message);
            }
            else
            {
                RustChat(player, "PM.SENDER.FORMAT", player.Id, receiver.Name, message);
                RustChat(receiver, "PM.RECEIVER.FORMAT", player.Id, player.Name, message);
            }

            var bply = receiver.Object as BasePlayer;
            if (GetPlayerData(receiver).PMSound)
            {
                Effect.server.Run(strName: config.PrivateSoundMessagePath, ent: bply, boneID: 0, posLocal: Vector3.zero, normLocal: Vector3.zero);
            }
#else
            Reply(player, "PM.SENDER.FORMAT", receiver.Name, message);
            if (player.IsServer)
            {
                ConsoleIPly = player;
                Reply(receiver, "PM.RECEIVER.FORMAT", config.ConsoleName, message);
            }
            else
                Reply(receiver, "PM.RECEIVER.FORMAT", player.Name, message);
#endif
            Log(LogType.PM, string.Format(GetMsg("PM.LOG"), $"{player.Name}({player.Id})", $"{receiver.Name}({receiver.Id})", message));
        }
        [Command("r")]
        void cmdChatR(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                Reply(player, "CMD.R.HELP");
                return;
            }
            string message = string.Join(" ", args);

            string recieverUserId;
            if (!pmHistory.TryGetValue(player.Id, out recieverUserId))
            {
                Reply(player, "PM.NO.MESSAGES");
                return;
            }
            if (recieverUserId== "server_console")
            {
#if RUST
                RustChat(player, "PM.SENDER.FORMAT", player.Id, config.ConsoleName, message);
#else
                Reply(player, "PM.SENDER.FORMAT", config.ConsoleName, message);
#endif
                Reply(ConsoleIPly, "PM.RECEIVER.FORMAT", player.Name, message);
                Log(LogType.PM, string.Format(GetMsg("PM.LOG"), $"{player.Name}({player.Id})", $"Server console", message));
                return;
            }

            var receiver = players.Connected.FirstOrDefault(p => p.Id == recieverUserId);
            if (receiver == null)
            {
                Reply(player, "PM.PLAYER.LEAVE");
                return;
            }
            if (GetPlayerData(receiver).BlackList.Contains(player.Id))
            {
                Reply(player, "PM.YOU.ARE.BLACK.LIST", receiver.Name);
                return;
            }
#if RUST
            if (player.IsServer)
            {
                Reply(player, "PM.SENDER.FORMAT", receiver.Name, message);
                RustChat(receiver, "PM.RECEIVER.FORMAT", "0", config.ConsoleName, message);
            }
            else
            {
                RustChat(player, "PM.SENDER.FORMAT", player.Id, receiver.Name, message);
                RustChat(receiver, "PM.RECEIVER.FORMAT", player.Id, player.Name, message);
            }
            var bply = receiver.Object as BasePlayer;
            if (GetPlayerData(receiver).PMSound)
            {
                Effect.server.Run(strName: config.PrivateSoundMessagePath, ent: bply, boneID: 0, posLocal: Vector3.zero, normLocal: Vector3.zero);
            }
#else
            Reply(player, "PM.SENDER.FORMAT", receiver.Name, message);
            Reply(receiver, "PM.RECEIVER.FORMAT", player.IsServer ? config.ConsoleName : player.Name, message);
#endif
            Log(LogType.PM, string.Format(GetMsg("PM.LOG"), $"{player.Name}({player.Id})", $"{receiver.Name}({receiver.Id})", message));
        }
        #endregion

        #region Data
        private class MuteData
        {
            public readonly string Initiator;
            public readonly string Reason;
            public readonly DateTime ExpireDate;
            [JsonIgnore]
            private bool Timed => ExpireDate != DateTime.MinValue;
            [JsonIgnore]
            public bool Expired => Timed && ExpireDate < DateTime.UtcNow;
            [JsonIgnore]
            public string Remain => Timed ? TimeToString(ExpireDate - DateTime.UtcNow) : "Unlimited";
            public static bool IsMuted(string userID) => mutes.ContainsKey(userID);
            public MuteData(string Initiator, string Reason, TimeSpan? Until = null)
            {
                this.ExpireDate = Until.HasValue ? DateTime.UtcNow + Until.Value : DateTime.MinValue;
                this.Reason = Reason;
                this.Initiator = Initiator;
            }
            [JsonConstructor]
            public MuteData(string Initiator, string Reason, DateTime ExpireDate)
            {
                Interface.Oxide.RootLogger.Write(Core.Logging.LogType.Chat,$"Mutedata initilizere called with: {Initiator} | {Reason} | {ExpireDate}");
                this.ExpireDate = ExpireDate;
                this.Reason = Reason;
                this.Initiator = Initiator;
            }

            public static string TimeToString(TimeSpan elapsedTime)
            {
                int hours = elapsedTime.Hours;
                int minutes = elapsedTime.Minutes;
                int seconds = elapsedTime.Seconds;
                int days = elapsedTime.Days;
                string s = "";
                if (days > 0) s += $"{days} дн. ";
                if (hours > 0) s += $"{hours} ч. ";
                if (minutes > 0) s += $"{minutes} мин. ";
                if (seconds > 0) s += $"{seconds} сек.";
                else s = s.TrimEnd(' ');
                return s;
            }
            public static bool StringToTime(string source, out TimeSpan time)
            {
                int seconds = 0, minutes = 0, hours = 0, days = 0;
                Match s = new Regex(@"(\d+?)s", RegexOptions.IgnoreCase).Match(source);
                Match m = new Regex(@"(\d+?)m", RegexOptions.IgnoreCase).Match(source);
                Match h = new Regex(@"(\d+?)h", RegexOptions.IgnoreCase).Match(source);
                Match d = new Regex(@"(\d+?)d", RegexOptions.IgnoreCase).Match(source);
                if (s.Success)
                    seconds = Convert.ToInt32(s.Groups[1].ToString());
                if (m.Success)
                    minutes = Convert.ToInt32(m.Groups[1].ToString());
                if (h.Success)
                    hours = Convert.ToInt32(h.Groups[1].ToString());
                if (d.Success)
                    days = Convert.ToInt32(d.Groups[1].ToString());
                source = source.Replace(seconds + "s", string.Empty);
                source = source.Replace(minutes + "m", string.Empty);
                source = source.Replace(hours + "h", string.Empty);
                source = source.Replace(days + "d", string.Empty);
                if (!string.IsNullOrEmpty(source) || (!s.Success && !m.Success && !h.Success && !d.Success))
                {
                    time = TimeSpan.Zero;
                    return false;
                }
                time = new TimeSpan(days, hours, minutes, seconds);
                return true;
            }
        }
        public class PlayerData
        {
            public string Prefix = "chatplus.default";
            public string NameColor = "chatplus.default";
            public string MessageColor = "chatplus.default";
            public bool Censor = true;
            public bool PMSound = true;
            public string Name = "";
            public bool IsAdmin = false;
            public bool IsModer = false;
            public List<string> BlackList = new List<string>();
        }

        PlayerData GetPlayerData(IPlayer player)
        {
            var data = GetPlayerData(player.Id);
            data.Name = player.Name;
            return data;
        }
        PlayerData GetPlayerData(BasePlayer player)
        {
            var data = GetPlayerData(player.UserIDString);
            data.Name = player.displayName;
            return data;
        }
        PlayerData GetPlayerData(string userId)
        {
            PlayerData config;
            if (PlayersData.TryGetValue(userId, out config))
            {
                if (config.Prefix != "chatplus.default" && !PermissionService.HasPermission(userId, config.Prefix)) config.Prefix = "chatplus.default";
                if (config.NameColor != "chatplus.default" && !PermissionService.HasPermission(userId, config.NameColor)) config.NameColor = "chatplus.default";
                if (config.MessageColor != "chatplus.default" && !PermissionService.HasPermission(userId, config.MessageColor)) config.MessageColor = "chatplus.default";
                return config;
            }
            config = new PlayerData();
            PlayersData[userId] = config;
            return config;
        }
        #endregion

        #region Localization
        string GetMsg(string key, object ID = null, params object[] args) => lang.GetMessage(key, this, ID == null ? null : ID.ToString());
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["CMD.CHAT.HELP"] = "Available commands:\n[#00FF00]/chat censor[/#] - switching chat censor\n[#00FF00]/chat prefix[/#] - switching your chat prefix\n[#00FF00]/chat name[/#] - switching your chat name color\n[#00FF00]/chat message[/#] - switching your chat message color\n[#00FF00]/chat ignore[/#] [#42f4ee]add/remove/list[/#] - managing black list\n[#00FF00]/chat sound[/#] [#42f4ee]on/off[/#] - switching private messages sounds\n[#00FF00]/chat reset[/#] - set your chat/message color and prefix to default",
                ["CMD.CHAT.HELP.PERMISSION.ADMIN"] = "\n[#00FF00]/chat admin[/#] - administrator mode",
                ["CMD.CHAT.HELP.PERMISSION.MODERATOR"] = "\n[#00FF00]/chat moder[/#] - moderator mode",
                ["CMD.MUTE.ALL.HELP"] = "\n[#00FF00]/muteall[/#] - Toggle global chat mute",
                ["NO.AVAILABLE.PREFIXS"] = "You have no prefixes available",
                ["PREFIX.NOT.FOUND"] = "Prefix \"{0}\" not found",
                ["PREFIX.CHANGED"] = "Prefix changed to {0}",
                ["AVAILABLE.COLORS.NAME"] = "Available name colors:\n{0}",
                ["AVAILABLE.COLORS.PREFIX"] = "Available prefixes:\n{0}",
                ["AVAILABLE.COLORS.MESSAGE"] = "Available chat colors:\n{0}",
                ["NO.AVAILABLE.COLORS"] = "You have no colors available",
                ["COLOR.NOT.FOUND"] = "Color \"{0}\" not found",
                ["NAME.COLOR.CHANGED"] = "Name color changed to {0}",
                ["MESSAGE.COLOR.CHANGED"] = "Message color changed to {0}",
                ["CMD.CHAT.SOUND.HELP"] = "Use [#00FF00]/chat sound[/#] [#008000]on[/#] or [#00FF00]/chat sound[/#] [#FF4500]off[/#] to toggle sound of income PM",
                ["SOUND.ENABLED"] = "You have turn income PM sounds [#008000]ON[/#]",
                ["SOUND.DISABLED"] = "You have turn income PM sounds [#FF4500]OFF[/#]",
                ["CMD.MUTE.HELP"] = "Use [#00FF00]/mute[/#] <\"player name\"> [time] [reason] to mute player",
                ["USER.ALREADY.MUTED"] = "Player \"{0}\" Already muted.\nReason: {1}\nTimeleft: {2}",
                ["USER.MUTED.REASON"] = "{0} has mute player \"{1}\"\nReason: {2}\nTimeleft: {3}",
                ["USER.MUTED.LOG"] = "\"{0}\" muted player \"{1}\" for {2}\nReason: \"{3}\"",
                ["CMD.UNMUTE.HELP"] = "Use [#00FF00]/unmute[/#] \"player name\" to unmute player",
                ["USER.UNMUTED"] = "{0} remove mute from player \"{1}\"",
                ["USER.UNMUTED.LOG"] = "\"{0}\" unmuted player \"{1}\"",
                ["YOU.MUTED"] = "You are muted, and may not chat!\nInitiator: {0}\nReason: {1}\nTimeleft: {2}",
                ["MUTE.ALL.ENABLED"] = "Global chat [#62ff29]ENABLED[/#]",
                ["MUTE.ALL.DISABLED"] = "Global chat [#ff552a]DISABLED[/#]",
                ["CMD.CHAT.IGNORE.HELP"] = "Commands list:\n[#00FF00]/chat ignore add[/#] \"player name\" - add player to your blacklist\n[#00FF00]/chat ignore remove[/#] \"player name\" - remove player from your blacklist\n[#00FF00]/chat ignore list[/#] - show your blacklist",
                ["USER.IS.IGNORE.LIST"] = "Player \"{0}\" already blacklisted",
                ["NOT.ON.IGNORE.LIST"] = "Player \"{0}\" isn't blacklisted",
                ["USER.ADD.IGNORE.LIST"] = "You successfully add player \"{0}\" to your blacklist",
                ["YOU.ADD.IGNORE.LIST"] = "Player \"{0}\" added you to his/her blacklist",
                ["IGNORE.LIST.IS.EMPTY"] = "Blacklist is empty",
                ["USER.REMOVE.IGNORE.LIST"] = "You have removed player \"{0}\" from your blacklist",
                ["YOU.REMOVE.IGNORE.LIST"] = "Player \"{0}\" removed you from his/her blacklist",
                ["IGNORE.LIST"] = "BLACKLIST:\n",
                ["CMD.PM.HELP"] = "Use [#00FF00]/pm[/#] \"player name\" \"message\" to send PM to another player",
                ["PM.SENDER.FORMAT"] = "[#e664a5]PM for {0}[/#]: {1}",
                ["PM.RECEIVER.FORMAT"] = "[#e664a5]PM from {0}[/#]: {1}",
                ["PM.NO.MESSAGES"] = "You havn't recive PM yet.",
                ["PM.PLAYER.LEAVE"] = "Player with who you were chating has left the server.",
                ["PM.YOU.ARE.BLACK.LIST"] = "You can't send PM to the player \"{0}\", you are in his/her blacklist",
                ["CMD.R.HELP"] = "Use [#00FF00]/r[/#] \"message\" to reply to the lates PM",
                ["CENSOR.ENABLED"] = "You have [#62ff29]ENABLE[/#] chat censority",
                ["CENSOR.DISABLED"] = "You have [#ff552a]DISABLE[/#] chat censority",
                ["NO.ACCESS"] = "You have no access to this command",
                ["PLAYER.NOT.FOUND"] = "PLayer \"{0}\" not found",
                ["MULTIPLE.PLAYERS.FOUND"] = "Multiply players found:\n{0}",
                ["PM.SELF"] = "You can't send messages to self",
                ["MODERATOR.ENABLE"] = "Moderator mode has been [#62ff29]ENABLED[/#]",
                ["MODERATOR.DISABLE"] = "Moderator mode has been [#ff552a]DISABLED[/#]",
                ["ADMIN.ENABLE"] = "Administrator mode has been [#62ff29]ENABLED[/#]",
                ["ADMIN.DISABLE"] = "Administrator mode has been [#ff552a]DISABLED[/#]",
                ["PLAYER.NOT.MUTED"] = "Player is not muted",
                ["RESET.SUCCESSFULL"] = "Your chat setting was resetted to default",
                ["CHAT.SPAM"] = "Your message is to loong!",
                ["CHAT.TOOMUCH"] = "Too long message. > 500",
                ["CMD.HELP.PREFIX"] = "Incorrect syntax! chatplus.prefix steamid/nick privilege",
                ["CMD.HELP.NAME"] = "Incorrect syntax! chatplus.name steamid/nick privilege",
                ["CMD.HELP.MESSAGE"] = "Incorrect syntax! chatplus.message steamid/nick privilege",
                ["NO.PERMISSION.PLAYER"] = "Player doesn't have requiered permission!",
                ["NO.PERMISSION.YOU"] = "You don't have requiered permission!",
                ["MUTE.EXPIRED"] = "Player {0} is no longer muted.",
                ["MUTE.LIST.HEAD"] = "Mutelist:",
                ["MUTE.LIST.BODY"] = "--------------------\nInitiator: {0}\nPlayer: {1}\nReason: {2}\nTimeleft: {3}\n--------------------",
                ["MUTE.LIST.NOONE"] = "No one is currently muted",
                ["PM.LOG"] = "PM from \"{0}\" to \"{1}\": {2}"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["CMD.CHAT.HELP"] = "Доступные команды:\n[#00FF00]/chat censor[/#] - цензура в чате\n[#00FF00]/chat prefix[/#] - доступные префиксы\n[#00FF00]/chat name[/#] - доступные цвета имени\n[#00FF00]/chat message[/#] - доступные цвета сообщений\n[#00FF00]/chat ignore[/#] [#42f4ee]add/remove/list[/#] - управление чёрным списком\n[#00FF00]/chat sound[/#] [#42f4ee]on/off[/#] - звук при получении ЛС\n[#00FF00]/chat reset[/#] - Сбрасывает ваши настройки чата на стандартные.",
                ["CMD.CHAT.HELP.PERMISSION.ADMIN"] = "\n[#00FF00]/chat admin[/#] - режим администратора",
                ["CMD.CHAT.HELP.PERMISSION.MODERATOR"] = "\n[#00FF00]/chat moder[/#] - режим модератора",
                ["CMD.MUTE.ALL.HELP"] = "\n[#00FF00]/muteall[/#] - Блокировка/разблокировка общего чата",
                ["NO.AVAILABLE.PREFIXS"] = "У вас нет доступных префиксов",
                ["PREFIX.NOT.FOUND"] = "Префикс с названием \"{0}\" не найден",
                ["PREFIX.CHANGED"] = "Префикс изменен на {0}",
                ["AVAILABLE.COLORS.NAME"] = "Доступные цвета имени:\n{0}",
                ["AVAILABLE.COLORS.PREFIX"] = "Доступные префиксы:\n{0}",
                ["AVAILABLE.COLORS.MESSAGE"] = "Доступные цвета сообщений:\n{0}",
                ["NO.AVAILABLE.COLORS"] = "У вас нет доступных цветов",
                ["COLOR.NOT.FOUND"] = "Цвет с названием \"{0}\" не найден",
                ["NAME.COLOR.CHANGED"] = "Цвет имени успешно изменен на {0}",
                ["MESSAGE.COLOR.CHANGED"] = "Цвет сообщений успешно изменен на {0}",
                ["CMD.CHAT.SOUND.HELP"] = "Используйте [#00FF00]/chat sound[/#] [#008000]on[/#] или [#FF4500]off[/#] чтобы включить или выключить звуковое оповещение при получении ЛС",
                ["SOUND.ENABLED"] = "Вы включили звуковое оповещение при получении ЛС",
                ["SOUND.DISABLED"] = "Вы выключили звуковое оповещение при получении ЛС",
                ["CMD.MUTE.HELP"] = "Используйте [#00FF00]/mute[/#] <\"имя игрока\"> [длительность] [причина] чтобы заблокировать чат игроку",
                ["USER.ALREADY.MUTED"] = "У игрока \"{0}\" уже отключён чат.\nПричина: {1}\nОсталось времени: {2}",
                ["USER.MUTED.REASON"] = "{0} заблокировал чат игроку \"{1}\"\nПричина: {2}\nВремя блокировки: {3}",
                ["USER.MUTED.LOG"] = "\"{0}\" заблокировал чат игроку \"{1}\" на {2}\nПричина: \"{3}\"",
                ["CMD.UNMUTE.HELP"] = "Используйте [#00FF00]/unmute[/#] \"имя игрока\" чтобы разблокировать чат игроку",
                ["USER.UNMUTED"] = "{0} снял мут с игрока \"{1}\"",
                ["USER.UNMUTED.LOG"] = "\"{0}\" разблокировал чат \"{1}\"",
                ["YOU.MUTED"] = "Ваш чат заблокирован!\nЗаблокировавший: {0}\nПричина: {1}\nОсталось времени: {2}",
                ["MUTE.ALL.ENABLED"] = "Общий чат [#62ff29]РАЗБЛОКИРОВАН[/#]",
                ["MUTE.ALL.DISABLED"] = "Общий чат [#ff552a]ЗАБЛОКИРОВАН[/#]",
                ["CMD.CHAT.IGNORE.HELP"] = "Список команд:\n[#00FF00]/chat ignore add[/#] \"имя игрока\" - добавить в черный список\n[#00FF00]/chat ignore remove[/#] \"имя игрока\" - удалить из черного списка\n[#00FF00]/chat ignore list[/#] - показать черный список",
                ["USER.IS.IGNORE.LIST"] = "Игрок \"{0}\" уже находится в черном списке",
                ["USER.ADD.IGNORE.LIST"] = "Вы добавили игрока \"{0}\" в черный список",
                ["YOU.ADD.IGNORE.LIST"] = "Игрок \"{0}\" добавил вас в черный список",
                ["NOT.ON.IGNORE.LIST"] = "Игрок \"{0}\" не находится в чёрном списке",
                ["IGNORE.LIST.IS.EMPTY"] = "Черный список пуст",
                ["USER.REMOVE.IGNORE.LIST"] = "Вы удалили игрока \"{0}\" из черного списка",
                ["YOU.REMOVE.IGNORE.LIST"] = "Игрок \"{0}\" удалил вас из черного списка",
                ["IGNORE.LIST"] = "Чёрный список:\n",
                ["CMD.PM.HELP"] = "Используйте [#00FF00]/pm[/#] \"имя игрока\" \"сообщение\" чтобы отправить ЛС игроку",
                ["PM.SENDER.FORMAT"] = "[#e664a5]ЛС для {0}[/#]: {1}",
                ["PM.RECEIVER.FORMAT"] = "[#e664a5]ЛС от {0}[/#]: {1}",
                ["PM.NO.MESSAGES"] = "Вы не получали личных сообщений",
                ["PM.PLAYER.LEAVE"] = "Игрок с которым вы переписывались вышел с сервера",
                ["PM.YOU.ARE.BLACK.LIST"] = "Вы не можете отправить ЛС игроку \"{0}\", он добавил вас в черный список",
                ["CMD.R.HELP"] = "Используйте [#00FF00]/r[/#] \"сообщение\" чтобы ответить но последнее ЛС",
                ["CENSOR.ENABLED"] = "Вы [#62ff29]ВКЛЮЧИЛИ[/#] цензуру в чате",
                ["CENSOR.DISABLED"] = "Вы [#ff552a]ВЫКЛЮЧИЛИ[/#] цензуру в чате",
                ["NO.ACCESS"] = "У вас нет доступа к этой команде",
                ["PLAYER.NOT.FOUND"] = "Игрок \"{0}\" не найден",
                ["MULTIPLE.PLAYERS.FOUND"] = "Найдено несколько игроков с похожим именем:\n{0}",
                ["PM.SELF"] = "Вы не можете отправлять сообщения самому себе",
                ["MODERATOR.ENABLE"] = "Режим модератора [#62ff29]ВКЛЮЧЕН[/#]",
                ["MODERATOR.DISABLE"] = "Режим модератора [#ff552a]ВЫКЛЮЧЕН[/#]",
                ["ADMIN.ENABLE"] = "Режим администратора [#62ff29]ВКЛЮЧЕН[/#]",
                ["ADMIN.DISABLE"] = "Режим администратора [#ff552a]ВЫКЛЮЧЕН[/#]",
                ["PLAYER.NOT.MUTED"] = "У игрока не отключен чат",
                ["RESET.SUCCESSFULL"] = "Вы сбросили свои настройки чата на стандартные",
                ["CHAT.SPAM"] = "Ваше сообщение слишком длинное!",
                ["CHAT.TOOMUCH"] = "Слишком длинное сообщение. Больше 500 символов.",
                ["CMD.HELP.PREFIX"] = "Неверный синтаксис! chatplus.prefix steamid/ник привилегия",
                ["CMD.HELP.NAME"] = "Неверный синтаксис! chatplus.name steamid/ник привилегия",
                ["CMD.HELP.MESSAGE"] = "Неверный синтаксис! chatplus.message steamid/ник привилегия",
                ["NO.PERMISSION.PLAYER"] = "У игрока нет необходимой привилегии!",
                ["NO.PERMISSION.YOU"] = "У вас нет необходимой привилегии!",
                ["MUTE.EXPIRED"] = "Игроку \"{0}\" вновь доступен чат.",
                ["MUTE.LIST.HEAD"] = "Список игроков с отключённым чатом:",
                ["MUTE.LIST.BODY"] = "--------------------\nИнициатор: {0}\nИгрок: {1}\nПричина: {2}\nСрок: {3}\n--------------------",
                ["MUTE.LIST.NOONE"] = "Чат на данный момент доступен всем игрокам.",
                ["PM.LOG"] = "ЛС от \"{0}\" для \"{1}\": {2}"
            }, this, "ru");
        }
        #endregion

        #region Config Initialization
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = new ChatConfig()
            {
                CapsBlock = true,
                BadWordsBlock = false,
                MuteReasonAutoMute = "Нецензурная лексика",
                MuteReason = "Не указана",
                MuteBadWordsDefault = 300,
                PrivateSoundMessage = true,
                PrivateSoundMessagePath = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
                adminPrivilages = new AdminPrivilages
                {
                    AdminPermiss = "chatplus.adminmode",
                    ModerPermiss = "chatplus.moderatormode",
                    MutePermiss = "chatplus.mute",
                    UnMutePermiss = "chatplus.unmute",
                    MuteAllPermiss = "chatplus.muteall",
                    AssignPermiss = "chatplus.assign",
                    AdminFormat = "[#a5e664]Администратор[/#]: {0}",
                    ModerFormat = "[#a5e664]Модератор[/#]: {0}",
                    HideAdmins = false,
                    AdminReplace = "Модератор чата"
                },
                prefixes = new List<ChatPrivilege>()
                    {
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.default",
                            Arg = "default",
                            Format = "",
                        },
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.vip",
                            Arg = "vip",
                            Format = "[#9370DB][VIP][/#]",
                        },
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.premium",
                            Arg = "premium",
                            Format = "[#00FF7F][Премиум][/#]",
                        }
                    },
                names = new List<ChatPrivilege>()
                    {
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.default",
                            Arg = "default",
                            Format = "[#ffffff]{0}[/#]",
                        },
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.hotpink",
                            Arg = "hotpink",
                            Format = "[#FF69B4]{0}[/#]",
                        },
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.tomato",
                            Arg = "tomato",
                            Format = "[#FF6347]{0}[/#]",
                        }
                    },
                messages = new List<ChatPrivilege>()
                    {
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.default",
                            Arg = "default",
                            Format = "[#ffffff]{0}[/#]",
                        },
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.blue",
                            Arg = "blue",
                            Format = "[#64a5e6]{0}[/#]",
                        },
                        new ChatPrivilege()
                        {
                            Perm = "chatplus.gold",
                            Arg = "gold",
                            Format = "[#DAA520]{0}[/#]",
                        }
                    },
                badWords = new Dictionary<string, List<string>>()
                    {
                        { "ебля", new List<string>() },
                        { "сука", new List<string>() },
                        { "пидор", new List<string>() },
                    },
                ConsoleName = "Server Console",
                ConsoleFormat = "[+16][#00ff00]{0}[/#]: {1}[/+]",
                PMAva = true
            };
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            {
                config = Config.ReadObject<ChatConfig>();
                if (!config.names.Any(c => c.Perm == "chatplus.default"))
                {
                    PrintWarning("Не удаляйте привилегию chatplus.default! Стандартное имя восстановлено. Без него плагин работать не будет.");
                    config.names.Add(new ChatPrivilege()
                    {
                        Arg = "default",
                        Perm = "chatplus.default",
                        Format = "<color=#ffffff>{0}</color>"
                    });
                    Config.WriteObject(config);
                }
                if (!config.messages.Any(c => c.Perm == "chatplus.default"))
                {
                    PrintWarning("Не удаляйте привилегию chatplus.default! Стандартный вид сообщений восстановлен. Без него плагин работать не будет.");
                    config.messages.Add(new ChatPrivilege()
                    {
                        Arg = "default",
                        Perm = "chatplus.default",
                        Format = "<color=#ffffff>{0}</color>"
                    });
                    Config.WriteObject(config);
                }
                if (!config.prefixes.Any(c => c.Perm == "chatplus.default"))
                {
                    PrintWarning("Не удаляйте привилегию chatplus.default! Стандартный перфикс восстановлен. Без него плагин работать не будет.");
                    config.prefixes.Add(new ChatPrivilege()
                    {
                        Arg = "default",
                        Perm = "chatplus.default",
                        Format = ""
                    });
                    Config.WriteObject(config);
                }
                //Для совместимости с прошлыми версиями
                bool changed = false;
                if (config.ConsoleName == null)
                {
                    PrintWarning("В файл конфигурации добавлен новый параметр - \"M. Имя консоли при отправке личных сообщений и отключении чата из консоли\"");
                    config.ConsoleName = "Server Console";
                    changed = true;
                }
                if (config.ConsoleFormat == null)
                {
                    PrintWarning("В файл конфигурации добавлен новый параметр - \"N. Формат отправки сообщений из консоли командой say\"");
                    config.ConsoleFormat = "[+16][#00ff00]{0}[/#]: {1}[/+]";
                    changed = true;
                }
                if (!config.PMAva.HasValue)
                {
                    PrintWarning("В файл конфигурации добавлен новый параметр - \"O. Отображать ли аватарки игроков при получении ЛС(только RUST)\"");
                    config.PMAva = true;
                    changed = true;
                }
                if (!config.adminPrivilages.HideAdmins.HasValue)
                {
                    PrintWarning("В файл конфигурации добавлен новый параметр - \"H - Скрывать имена администраторов при блокировке чата\"");
                    config.adminPrivilages.HideAdmins = false;
                    changed = true;
                }
                if(config.adminPrivilages.AdminReplace == null)
                {
                    PrintWarning("В файл конфигурации добавлен новый параметр - \"H - Замена имени администратора при блокировке чата(если включено)\"");
                    config.adminPrivilages.AdminReplace = "Модератор чата";
                    changed = true;
                }
                if (changed)
                    SaveConfig();
            }
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Configuration
        public class ChatPrivilege
        {
            [JsonProperty("Привилегия")]
            public string Perm;
            [JsonProperty("Аргумент")]
            public string Arg;
            [JsonProperty("Формат")]
            public string Format;
        }
        public class AdminPrivilages
        {
            [JsonProperty("Привилегия для включения режима администратора")]
            public string AdminPermiss;
            [JsonProperty("Привилегия для включения режима модератора")]
            public string ModerPermiss;
            [JsonProperty("Привилегия для использования команды /mute")]
            public string MutePermiss;
            [JsonProperty("Привилегия для использования команды /unmute")]
            public string UnMutePermiss;
            [JsonProperty("Формат чата режима администратор")]
            public string AdminFormat;
            [JsonProperty("Формат чата режима модератор")]
            public string ModerFormat;
            [JsonProperty("Привилегия для полного отключения чата")]
            public string MuteAllPermiss;
            [JsonProperty("Привилегия для использования консольных команд на присваивание префикса и цветов")]
            public string AssignPermiss;
            [JsonProperty("Скрывать имена администраторов при блокировке чата")]
            public bool? HideAdmins;
            [JsonProperty("Замена имени администратора при блокировке чата(если включено)")]
            public string AdminReplace;
        }
        public class ChatConfig
        {
            [JsonProperty("A. Автоматически блокировать чат за нецензурную лексику")]
            public bool BadWordsBlock;
            [JsonProperty("B. Длительность блокировки чата за нецензурную лексику(в секундах)")]
            public int MuteBadWordsDefault;
            [JsonProperty("C. Причина мута при автоматической блокировке чата за нецензурную лексику")]
            public string MuteReasonAutoMute;
            [JsonProperty("D. Стандартная причина мута")]
            public string MuteReason;
            [JsonProperty("E. Воспроизводить звук при получении личного сообщения")]
            public bool PrivateSoundMessage;
            [JsonProperty("F. Полный путь к звуковому файлу")]
            public string PrivateSoundMessagePath;
            [JsonProperty("G. Выключить заглавные буквы в чате")]
            public bool CapsBlock;
            [JsonProperty("H. Настройки привелегий администраторов")]
            public AdminPrivilages adminPrivilages;
            [JsonProperty("I. Цвет имен")]
            public List<ChatPrivilege> names;
            [JsonProperty("J. Префиксы")]
            public List<ChatPrivilege> prefixes;
            [JsonProperty("K. Цвет сообщений")]
            public List<ChatPrivilege> messages;
            [JsonProperty("L. Список начальных букв нецензурных слов или слова целиком | список исключений")]
            public Dictionary<string, List<string>> badWords;
            [JsonProperty("M. Имя консоли при отправке личных сообщений и отключении чата из консоли")]
            public string ConsoleName;
            [JsonProperty("N. Формат отправки сообщений из консоли командой say")]
            public string ConsoleFormat;
            [JsonProperty("O. Отображать ли аватарки игроков при получении ЛС(только RUST)")]
            public bool? PMAva;
            public void RegisterPerms()
            {
                PermissionService.RegisterPermissions(prefixes.Select(p => p.Perm).ToList());
                PermissionService.RegisterPermissions(names.Select(p => p.Perm).ToList());
                PermissionService.RegisterPermissions(messages.Select(p => p.Perm).ToList());
                PermissionService.RegisterPermissions(new List<string>()
                {
                    adminPrivilages.AdminPermiss,
                    adminPrivilages.ModerPermiss,
                    adminPrivilages.MutePermiss,
                    adminPrivilages.MuteAllPermiss,
                    adminPrivilages.AssignPermiss,
                    adminPrivilages.UnMutePermiss
                });
            }

            public ChatPrivilege Get(List<ChatPrivilege> list, string perm)
            {
                return list.FirstOrDefault(p => p.Perm == perm);
            }
        }
        #endregion

        #region API
        bool IsPlayerMuted(object id) => MuteData.IsMuted(id.ToString());
        void OnPluginUnloaded(Plugin plugin)
        {
            if (ThirdPartyTitles.ContainsKey(plugin))
                ThirdPartyTitles.Remove(plugin);
        }
        private void API_RegisterThirdPartyTitle(Plugin plugin, Func<IPlayer, string> titleGetter) => ThirdPartyTitles[plugin] = titleGetter;
        #endregion

        #region PermissionService

        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(string uid, string permissionName)
            {
                return !string.IsNullOrEmpty(permissionName) && permission.UserHasPermission(uid, permissionName);
            }

            public static void RegisterPermissions(List<string> permissions)
            {
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, m_Instance);
                }
            }
            public static bool UserIDVaild(string userID)
            {
                return permission.UserIdValid(userID);
            }
        }

        #endregion
    }
}
///////////////////////////////////////////////
