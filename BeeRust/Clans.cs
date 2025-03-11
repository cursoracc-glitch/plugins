using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Transactions;
using Facepunch.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Clans", "OxideBro - RustPlugin.ru || Edit by King. ( Boloto Clans )", "1.0.61", ResourceId = 14)]
    public class Clans : RustPlugin
    {

        bool Changed;
        bool Initialized;
        internal static Clans cc = null;
        bool newSaveDetected = false;
        List<ulong> manuallyEnabledBy = new List<ulong>();
        HashSet<ulong> bypass = new HashSet<ulong>();
        Dictionary<string, DateTime> notificationTimes = new Dictionary<string, DateTime>();
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;
        Library lib;
        public Dictionary<string, Clan> clans = new Dictionary<string, Clan>();
        public Dictionary<string, string> clansSearch = new Dictionary<string, string>();
        List<string> purgedClans = new List<string>();
        Dictionary<string, string> originalNames = new Dictionary<string, string>();
        Dictionary<string, List<string>> pendingPlayerInvites = new Dictionary<string, List<string>>();
        Regex tagReExt;
        Dictionary<string, Clan> clanCache = new Dictionary<string, Clan>();
        List<object> filterDefaults()
        {
            var dp = new List<object>();
            dp.Add("admin");
            dp.Add("mod");
            dp.Add("owner");
            return dp;
        }

        private void SaveConf()
        {
            if (Author != r("BkvqrOeb - EhfgCyhtva.eh"))
                Author = r("Cyhtva nhgube BkvqrOeb - EhfgCyhtva.eh");

        }
        private static string r(string i)
        {
            return !string.IsNullOrEmpty(i) ? new string(i.Select(x => x >= 'a' && x <= 'z' ? (char)((x - 'a' + 13) % 26 + 'a') : x >= 'A' && x <= 'Z' ? (char)((x - 'A' + 13) % 26 + 'A') : x).ToArray()) : i;
        }

        Dictionary<string, object> rewardsDefaults()
        {
            var dp = new Dictionary<string, object>()
            {
                {"wood", 1 },
                { "stones", 1 },
                { "metal.ore", 1 },
                { "sulfur.ore", 1},
                { "hq.metal.ore", 1 },
                {"fat.animal", 1},
                {"cloth", 1},
                {"leather", 1},
                {"scrap", 1},
                {"gears", 1},
                {"techparts", 1},
                {"metalpipe", 1},
            };
            return dp;
        }

        private void RewardClans()
        {
            string SecretKey = "";
            if (string.IsNullOrEmpty(SecretKey))
            {
                PrintWarning("У вас не указан секретный ключ магазина!");
                return;
            }
            string ShopId = "";
            if (string.IsNullOrEmpty(ShopId))
            {
                PrintWarning("У вас не указан айди магазина!");
                return;
            }
            var sortedData = from pair in clans orderby pair.Value.ClanPoints descending select pair;
            int pos = 1;
            foreach (KeyValuePair<string, Clan> key in sortedData)
            {
                if (ClanBonus.ContainsKey(pos))
                {
                    foreach (var member in key.Value.members)
                    {
                        ulong playerID = Convert.ToUInt64(member.Key);
                        string request = $"http://gamestores.ru/api?shop_id={ShopId}&secret={SecretKey}&action=moneys&type=plus&steam_id={playerID}&amount={ClanBonus[pos]}&mess=Баланс за топ клана";
                        webrequest.Enqueue(request, null, (code, response) =>
                        {
                            if (code != 200 || response == null || code != 100) 
                                return;
                        }, this, Core.Libraries.RequestMethod.GET);

                        var player = BasePlayer.FindByID(playerID);
                        if (player == null)
                        {
                            Puts($"Успешно выдано {ClanBonus[pos]} рублей на баланс магазина игроку {playerID}");
                        }
                        else
                        {
                            Puts($"Успешно выдано {ClanBonus[pos]} рублей на баланс магазина игроку {player.displayName}/{playerID}");
                        }
                    }
                }
                pos++;
            }
        }

        Dictionary<int, float> ClanBonus = new Dictionary<int, float>()
        {
            [1] = 100,
            [2] = 45,
            [3] = 15,
        };

        Dictionary<string, int> NeedLimit = new Dictionary<string, int>()
        {
            ["wood"] = 1000000,
            ["stones"] = 1000000,
            ["metal.ore"] = 1000000,
            ["sulfur.ore"] = 1000000,
            ["hq.metal.ore"] = 250000,
            ["cloth"] = 100000,
            ["leather"] = 100000,
            ["fat.animal"] = 100000,
            ["loot-barrel"] = 100000,
        };

        Dictionary<string, object> rewardsTranslateDefault()
        {
            var dp = new Dictionary<string, object>()
            {
                {"wood", "ДЕРЕВО"},
                { "stones", "КАМЕНЬ" },
                { "metal.ore", "МЕТАЛ" },
                { "sulfur.ore", "СЕРА"},
                { "hq.metal.ore", "МВК" },
                {"fat.animal", "ЖИР"},
                {"cloth", "ТКАНЬ"},
                {"leather", "КОЖА"},
                {"scrap", "СКРАП"},
                {"gears", "ШЕСТЕРНИ"},
                {"techparts", "МИКРОСХЕМЫ"},
                {"metalpipe", "ТРУБЫ"},
            };
            return dp;
        }
        int ValidTimeFarm; 
        public int limitMembers;
        int limitModerators;
        public int limitAlliances;
        int tagLengthMin;
        int tagLengthMax;
        int inviteValidDays;
        int friendlyFireNotifyTimeout;
        string allowedSpecialChars;
        public bool enableFFOPtion;
        bool enableAllyFFOPtion;
        bool enableWordFilter;
        bool enableClanTagging;
        public bool enableClanAllies;
        bool forceAllyFFNoDeactivate;
        bool forceClanFFNoDeactivate;
        bool enableWhoIsOnlineMsg;
        bool enableComesOnlineMsg;
        int authLevelRename;
        int authLevelDelete;
        int authLevelInvite;
        int authLevelKick;
        int authLevelPromoteDemote;
        int authLevelClanInfo;
        bool purgeOldClans;
        int notUpdatedSinceDays;
        bool listPurgedClans;
        bool wipeClansOnNewSave;
        bool useProtostorageClandata;
        string consoleName;
        string broadcastPrefix;
        string broadcastPrefixAlly;
        string broadcastPrefixColor;
        string broadcastPrefixFormat;
        string broadcastMessageColor;
        string colorCmdUsage;
        string colorTextMsg;
        string colorClanNamesOverview;
        string colorClanFFOff;
        string colorClanFFOn;
        string pluginPrefix;
        string pluginPrefixColor;
        string pluginPrefixREBORNColor;
        bool pluginPrefixREBORNShow;
        string pluginPrefixFormat;
        string clanServerColor;
        string clanOwnerColor;
        string clanCouncilColor;
        string clanModeratorColor;
        string clanMemberColor;
        bool setHomeOwner = false;
        bool setHomeModerator = false;
        bool setHomeMember = false;
        string chatCommandClan;
        string chatCommandFF;
        string chatCommandAllyChat;
        string chatCommandClanChat;
        string chatCommandClanInfo;
        string subCommandClanHelp;
        string subCommandClanAlly;
        bool usePermGroups;
        string permGroupPrefix;
        bool usePermToCreateClan;
        string permissionToCreateClan;
        bool usePermToJoinClan;
        string permissionToJoinClan;
        bool addClanMembersAsIOFriends;
        string clanTagColorBetterChat;
        int clanTagSizeBetterChat;
        string clanTagOpening;
        string clanTagClosing;
        bool clanChatDenyOnMuted;
        List<string> activeRadarUsers = new List<string>();
        Dictionary<string, List<BasePlayer>> clanRadarMemberobjects = new Dictionary<string, List<BasePlayer>>();
        static Vector3 sleeperHeight = new Vector3(0f, 1.0f, 0f);
        static Vector3 playerHeight = new Vector3(0f, 1.8f, 0f);
        bool enableAtLogin;
        private bool forceNametagsOnTagging;
        public static bool useRelationshipManager;
        private bool teamUiWasDisabled;
        private bool disableManageFunctions;
        private bool allowButtonLeave;
        private bool allowButtonKick;
        private bool allowDirectInvite;
        private bool allowPromoteLeader;

        List<object> wordFilter = new List<object>();

        Dictionary<string, object> RewardGather = new Dictionary<string, object>();
        Dictionary<string, object> RewardTranslate = new Dictionary<string, object>();

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }


        int PointsOfDeath = 1;
        int PointsOfKilled = 1;
        int PointsOfKilledHeli = 1;
        int PointsOfKilledBradleyAPC = 1;
        int PointsOfSuicide = 1;
        int PointsOfBarrel = 1;
        int PointFarm = 1;
        int PointsOfGatherMetalOre = 1;
        int PointsOfGatherStone = 1;
        int PointsOfGatherWood = 1;
        int PointsOfGatherHQM = 0;

        void LoadVariables()
        {
            wordFilter = (List<object>)GetConfig("Фильтр", "Слова", filterDefaults());
            RewardGather = (Dictionary<string, object>)GetConfig("ТОП", "Список предметов и очков за их добычу [Shortname: количество очков]", rewardsDefaults());

            RewardTranslate = (Dictionary<string, object>)GetConfig("ТОП", "Перевод итемов на русский [Shortname: Русский перевод]", rewardsTranslateDefault());

            PointsOfKilledBradleyAPC = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за Танк", 1));
            PointsOfDeath = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков убирает за смерть", 1));
            PointsOfKilled = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за убийство игрока", 1));
            PointsOfKilledHeli = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за сбитие вертолёта", 1));
            PointsOfSuicide = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков забираем за суицид", 1));
            PointFarm = Convert.ToInt32(GetConfig("ТОП", "Скок очков за фарм?", 1));
            PointsOfGatherMetalOre = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу металической руды (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherStone = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу камня (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherWood = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу дерева (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherHQM = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу МВК (за разбитый камень - то есть за последний удар)", 0));
            PointsOfBarrel = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за разрушение бочки (за разбитый камень - то есть за последний удар)", 1));

            ValidTimeFarm = Convert.ToInt32(GetConfig("Время", "После сколки часов нельзя фармить очки", 1));
            limitMembers = Convert.ToInt32(GetConfig("Лимиты", "Лимит участников клана", 8));
            limitModerators = Convert.ToInt32(GetConfig("Лимиты", "Лимит модераторов", 2));
            limitAlliances = Convert.ToInt32(GetConfig("Лимиты", "Лимит альянса", 2));
            tagLengthMin = Convert.ToInt32(GetConfig("Лимиты", "Лимит размера тега клана от", 2));
            tagLengthMax = Convert.ToInt32(GetConfig("Лимиты", "Лимит размера тега клана до", 10));
            inviteValidDays = Convert.ToInt32(GetConfig("Лимиты", "Длительность активного приглашения в днях", 1));
            friendlyFireNotifyTimeout = Convert.ToInt32(GetConfig("Лимиты", "Таймаут FF", 5));
            allowedSpecialChars = Convert.ToString(GetConfig("Лимиты", "Разрешенные специальные символы в тег клана", "!²³"));
            enableFFOPtion = Convert.ToBoolean(GetConfig("Настройки", "Включить FF длля кланов", true));
            enableAllyFFOPtion = Convert.ToBoolean(GetConfig("Настройки", "Включить FF для альянса", true));
            forceAllyFFNoDeactivate = Convert.ToBoolean(GetConfig("Настройки", "Запретить отключать FF для альянса", true));
            forceClanFFNoDeactivate = Convert.ToBoolean(GetConfig("Настройки", "Запретить отключать FF для клана", false));
            enableWordFilter = Convert.ToBoolean(GetConfig("Настройки", "Включить фльтр слов", true));
            enableClanTagging = Convert.ToBoolean(GetConfig("Настройки", "Включить клан ТЭГ", true));
            enableClanAllies = Convert.ToBoolean(GetConfig("Настройки", "Включить альянсы", false));
            enableWhoIsOnlineMsg = Convert.ToBoolean(GetConfig("Настройки", "Включить сообщение об онлайне клана", true));
            enableComesOnlineMsg = Convert.ToBoolean(GetConfig("Настройки", "Включить сообщение об входе игрока сокланам", true));
            useProtostorageClandata = Convert.ToBoolean(GetConfig("Storage", "Использовать Proto хранилище данных клана (Дата)", false));
            authLevelRename = Convert.ToInt32(GetConfig("Настройки привилегий", "Authentication Level Rename (Не трогать)", 1));
            authLevelDelete = Convert.ToInt32(GetConfig("Настройки привилегий", "Authentication Level Delete (Не трогать)", 2));
            authLevelInvite = Convert.ToInt32(GetConfig("Настройки привилегий", "Authentication Level Invite (Не трогать)", 1));
            authLevelKick = Convert.ToInt32(GetConfig("Настройки привилегий", "Authentication Level Kick (Не трогать)", 2));
            authLevelPromoteDemote = Convert.ToInt32(GetConfig("Настройки привилегий", "Authentication Level Promote Demote (Не трогать)", 1));
            authLevelClanInfo = Convert.ToInt32(GetConfig("Настройки привилегий", "Authentication Level Clan Info (Не трогать)", 0));
            usePermGroups = Convert.ToBoolean(GetConfig("Настройки привилегий", "Использовать разрешения для групп?", false));
            permGroupPrefix = Convert.ToString(GetConfig("Настройки привилегий", "Префикс привилегий для групп", "clan_"));
            usePermToCreateClan = Convert.ToBoolean(GetConfig("Настройки привилегий", "Использовать привилегию для создания клана?", false));
            permissionToCreateClan = Convert.ToString(GetConfig("Настройки привилегий", "Привилегия для создания клана", "clans.cancreate"));
            usePermToJoinClan = Convert.ToBoolean(GetConfig("Permission", "Использовать привилегию для возможности вступления в клан?", false));
            permissionToJoinClan = Convert.ToString(GetConfig("Permission", "Привилегия на возможность вступления в клан", "clans.canjoin"));
            purgeOldClans = Convert.ToBoolean(GetConfig("Очистка", "Удаление старых кланов", false));
            notUpdatedSinceDays = Convert.ToInt32(GetConfig("Очистка", "Дни с каких клан не обновлялся на удаление", 14));
            listPurgedClans = Convert.ToBoolean(GetConfig("Очистка", "Включить список очищенных кланов", false));
            wipeClansOnNewSave = Convert.ToBoolean(GetConfig("Очистка", "Удалить кланы при вайпе?", false));
            consoleName = Convert.ToString(GetConfig("Оформление", "Консольное имя", "ServerOwner"));
            broadcastPrefix = Convert.ToString(GetConfig("Оформление", "Префикс", "(CLAN)"));
            broadcastPrefixAlly = Convert.ToString(GetConfig("Оформление", "Префикс альянса", "(ALLY)"));
            broadcastPrefixColor = Convert.ToString(GetConfig("Оформление", "Цвет префикса", "#a1ff46"));
            broadcastPrefixFormat = Convert.ToString(GetConfig("Оформление", "Формат вывода сообщения", "<color={0}>{1}</color> "));
            broadcastMessageColor = Convert.ToString(GetConfig("Оформление", "Цвет вывода сообщения", "#e0e0e0"));
            colorCmdUsage = Convert.ToString(GetConfig("Оформление", "Цвет CMD", "#ffd479"));
            colorTextMsg = Convert.ToString(GetConfig("Оформление", "Цвет сообщения", "#e0e0e0"));
            colorClanNamesOverview = Convert.ToString(GetConfig("Оформление", "Цвет имена клана ", "#b2eece"));
            colorClanFFOff = Convert.ToString(GetConfig("Оформление", "Цвет сообщения об отключении FF", "#00DF00"));
            colorClanFFOn = Convert.ToString(GetConfig("Оформление", "Цвет сообщения об включении FF", "#DF0005"));
            pluginPrefix = Convert.ToString(GetConfig("Оформление", "Префикс", "CLANS"));
            pluginPrefixColor = Convert.ToString(GetConfig("Оформление", "Цвет префикса в сообщении", "#FBA300"));
            pluginPrefixREBORNColor = Convert.ToString(GetConfig("Оформление", "Цвет префикса в сообщении", "#ce422b"));
            pluginPrefixREBORNShow = Convert.ToBoolean(GetConfig("Оформление", "Включить префикс?", true));
            pluginPrefixFormat = Convert.ToString(GetConfig("Оформление", "Формат префикса REBORN в сообщении", "<color={0}>{1}</color>: "));
            clanServerColor = Convert.ToString(GetConfig("Оформление", "Цвет клана сервера в сообщении", "#ff3333"));
            clanOwnerColor = Convert.ToString(GetConfig("Оформление", "Цвец владельца клана в сообщении", "#a1ff46"));
            clanCouncilColor = Convert.ToString(GetConfig("Оформление", "Цвет команд помощи в сообщении", "#b573ff"));
            clanModeratorColor = Convert.ToString(GetConfig("Оформление", "Цвет модераторов клана в сообщении", "#74c6ff"));
            clanMemberColor = Convert.ToString(GetConfig("Оформление", "Цвет онлайна клана в сообщении", "#fcf5cb"));
            clanTagColorBetterChat = Convert.ToString(GetConfig("BetterChat", "Цвет тега кланов в чате", "#aaff55"));
            clanTagSizeBetterChat = Convert.ToInt32(GetConfig("BetterChat", "Размер тега кланов в чате", 15));
            clanTagOpening = Convert.ToString(GetConfig("BetterChat", "Скобка начало для тега", "["));
            clanTagClosing = Convert.ToString(GetConfig("BetterChat", "Скобка конец для тега", "]"));
            clanChatDenyOnMuted = Convert.ToBoolean(GetConfig("BetterChat", "Использовать полный мут для клана", false));
            chatCommandClan = Convert.ToString(GetConfig("Команды", "Открытие меню клана", "clan"));
            chatCommandFF = Convert.ToString(GetConfig("Команды", "Настройка FF для клана", "cff"));
            chatCommandAllyChat = Convert.ToString(GetConfig("Команды", "Отправка сообщения альянсу", "a"));
            chatCommandClanChat = Convert.ToString(GetConfig("Команды", "Отправка сообщения клану", "c"));
            chatCommandClanInfo = Convert.ToString(GetConfig("Команды", "Чатовая команда инфо клана для администраторов", "cinfo"));
            subCommandClanHelp = Convert.ToString(GetConfig("Команды", "Дополнительная подкоманда для вывода информации помощи", "help"));
            subCommandClanAlly = Convert.ToString(GetConfig("Команды", "Дополнительная подкоманда для вывода информации об альянсе", "ally"));
            addClanMembersAsIOFriends = Convert.ToBoolean(GetConfig("RustIO", "Добавить членов клана в качестве IO Friends", true));

            forceNametagsOnTagging = Convert.ToBoolean(GetConfig("Настройки", "forceNametagsOnTagging", false));

            useRelationshipManager = Convert.ToBoolean(GetConfig("Внутриигровая система друзей", "Использовать внутриигровую систему друзей", false));
            disableManageFunctions = Convert.ToBoolean(GetConfig("Внутриигровая система друзей", "Отключить управление тимой игрокам (Выход, инвайт и прочее)", false));
            allowButtonLeave = Convert.ToBoolean(GetConfig("Внутриигровая система друзей", "Разрешить выходить из тимы", true));
            allowButtonKick = Convert.ToBoolean(GetConfig("Внутриигровая система друзей", "Разрешить удалять из тимы", true));
            allowDirectInvite = Convert.ToBoolean(GetConfig("Внутриигровая система друзей", "Разрешить приглашение в тиму", true));
            allowPromoteLeader = Convert.ToBoolean(GetConfig("Внутриигровая система друзей", "Разрешить продвижение лидера тимы", true));
            if (!Changed) return;

            SaveConf();
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                    { "nopermtocreate", "You got no rights to create a clan." },
                {
                    "nopermtojoin", "You got no rights to join a clan."
                }
                , {
                    "nopermtojoinbyinvite", "The player {0} has no rights to join a clan."
                }
                , {
                    "claninvite", "You have been invited to join the clan: [{0}] '{1}'\nTo join, type: <color={2}>/clan join {0}</color>"
                }
                , {
                    "comeonline", "{0} has come online!"
                }
                , {
                    "goneoffline", "{0} has gone offline!"
                }
                , {
                    "friendlyfire", "{0} is a clan member and cannot be hurt.\nTo toggle clan friendlyfire type: <color=#FF6c6c>/clan ff</color>"
                }
                , {
                    "allyfriendlyfire", "{0} is an ally member and cannot be hurt."
                }
                , {
                    "notmember", "You are currently not a member of a clan."
                }
                , {
                    "youareownerof", "You are the owner of:"
                }
                , {
                    "youaremodof", "You are a moderator of:"
                }
                , {
                    "youarecouncilof", "You are a council of:"
                }
                , {
                    "youarememberof", "You are a member of:"
                }
                , {
                    "claninfo", " [{0}] {1}"
                }
                , {
                    "memberon", "Members online: "
                }
                , {
                    "overviewnamecolor", "<color={0}>{1}</color>"
                }
                , {
                    "memberoff", "Members offline: "
                }
                , {
                    "notmoderator", "You need to be a moderator of your clan to use this command."
                }
                , {
                    "pendinvites", "Pending invites: "
                }
                , {
                    "bannedwords", "The clan tag contains banned words."
                }
                , {
                    "viewthehelp", "To view more commands, type: <color={0}>/{1} helpies</color>"
                }
                , {
                    "usagecreate", "Usage - <color={0}>/clan create \"TAG\" \"Description\"</color>"
                }
                , {
                    "hintlength", "Clan tags must be {0} to {1} characters long"
                }
                , {
                    "hintchars", "Clan tags must contain only 'a-z' 'A-Z' '0-9' '{0}'"
                }
                , {
                    "providedesc", "Please provide a short description of your clan."
                }
                , {
                    "tagblocked", "There is already a clan with this tag."
                }
                , {
                    "nownewowner", "You are now the owner of the clan [{0}] \"{1}\""
                }
                , {
                    "inviteplayers", "To invite new members, type: <color={0}>/clan invite <name></color>"
                }
                , {
                    "usageinvite", "Usage - <color={0}>/clan invite <name></color>"
                }
                , {
                    "nosuchplayer", "No such player or player name not unique: {0}"
                }
                , {
                    "alreadymember", "This player is already a member of your clan: {0}"
                }
                , {
                    "alreadyinvited", "This player has already been invited to your clan: {0}"
                }
                , {
                    "alreadyinclan", "This player is already in a clan: {0}"
                }
                , {
                    "invitebroadcast", "{0} invited {1} to the clan."
                }
                , {
                    "usagewithdraw", "Usage: <color={0}>/clan withdraw <name></color>"
                }
                , {
                    "notinvited", "This player has not been invited to your clan: {0}"
                }
                , {
                    "canceledinvite", "{0} canceled the invitation of {1}."
                }
                , {
                    "usagejoin", "Usage: <color={0}>/clan join \"TAG\"</color>"
                }
                , {
                    "youalreadymember", "You are already a member of a clan."
                }
                , {
                    "younotinvited", "You have not been invited to join this clan."
                }
                , {
                    "reachedmaximum", "This clan has already reached the maximum number of members."
                }
                , {
                    "broadcastformat", "<color={0}>{1}</color>: {2}"
                }
                , {
                    "allybroadcastformat", "[{0}] <color=#FF6c6c>{2}</color>: {3}"
                }
                , {
                    "clanrenamed", "{0} renamed your clan to: [{1}]."
                }
                , {
                    "yourenamed", "You have renamed the clan [{0}] to [{1}]"
                }
                , {
                    "clandeleted", "{0} deleted your clan."
                }
                , {
                    "youdeleted", "You have deleted the clan [{0}]"
                }
                , {
                    "noclanfound", "There is no clan with that tag [{0}]"
                }
                , {
                    "renamerightsowner", "You need to be a server owner to rename clans."
                }
                , {
                    "usagerename", "Usage: <color={0}>/clan rename OLDTAG NEWTAG</color>"
                }
                , {
                    "deleterightsowner", "You need to be a server owner to delete clans."
                }
                , {
                    "usagedelete", "Usage: <color={0}>/clan delete TAG</color>"
                }
                , {
                    "clandisbanded", "Your current clan has been disbanded forever."
                }
                , {
                    "needclanowner", "You need to be the owner of your clan to use this command."
                }
                , {
                    "needclanownercouncil", "You need to be the owner or a council to use this command."
                }
                , {
                    "usagedisband", "Usage: <color={0}>/clan disband forever</color>"
                }
                , {
                    "usagepromote", "Usage: <color={0}>/clan promote <name></color>"
                }
                , {
                    "playerjoined", "{0} has joined the clan!"
                }
                , {
                    "waskicked", "{0} kicked {1} from the clan."
                }
                , {
                    "modownercannotkicked", "The player {0} is an owner or moderator and cannot be kicked."
                }
                , {
                    "notmembercannotkicked", "The player {0} is not a member of your clan."
                }
                , {
                    "usageff", "Usage: <color={0}>/clan ff</color> toggles your current FriendlyFire status."
                }
                , {
                    "usagekick", "Usage: <color={0}>/clan kick <name></color>"
                }
                , {
                    "playerleft", "{0} has left the clan."
                }
                , {
                    "youleft", "You have left your current clan."
                }
                , {
                    "usageleave", "Usage: <color={0}>/clan leave</color>"
                }
                , {
                    "notaclanmember", "The player {0} is not a member of your clan."
                }
                , {
                    "alreadyowner", "The player {0} is already the owner of your clan."
                }
                , {
                    "alreadyamod", "The player {0} is already a moderator of your clan."
                }
                , {
                    "alreadyacouncil", "The player {0} is already a council of your clan."
                }
                , {
                    "alreadyacouncilset", "The position of the council is already awarded."
                }
                , {
                    "maximummods", "This clan has already reached the maximum number of moderators."
                }
                , {
                    "playerpromoted", "{0} promoted {1} to moderator."
                }
                , {
                    "playerpromotedcouncil", "{0} promoted {1} to council."
                }
                , {
                    "playerpromotedowner", "{0} promoted {1} to new owner."
                }
                , {
                    "usagedemote", "Usage: <color={0}>/clan demote <name></color>"
                }
                , {
                    "notamoderator", "The player {0} is not a moderator of your clan."
                }
                , {
                    "notpromoted", "The player {0} is not a moderator or council of your clan."
                }
                , {
                    "playerdemoted", "{0} demoted {1} to a member."
                }
                , {
                    "councildemoted", "{0} demoted {1} to a moderator."
                }
                , {
                    "noactiveally", "Your clan has no current alliances."
                }
                , {
                    "yourffstatus", "Your FriendlyFire:"
                }
                , {
                    "yourclanallies", "Your Clan allies:"
                }
                , {
                    "allyinvites", "Ally invites:"
                }
                , {
                    "allypending", "Ally requests:"
                }
                , {
                    "allyReqHelp", "Offer an alliance to another clan"
                }
                , {
                    "allyAccHelp", "Accept an alliance from another clan"
                }
                , {
                    "allyDecHelp", "Decline an alliance from another clan"
                }
                , {
                    "allyCanHelp", "Cancel an alliance with another clan"
                }
                , {
                    "reqAlliance", "[{0}] has requested a clan alliance"
                }
                , {
                    "invitePending", "You already have a pending alliance invite for [{0}]"
                }
                , {
                    "clanNoExist", "The clan [{0}] does not exist"
                }
                , {
                    "alreadyAllies", "You are already allied with"
                }
                , {
                    "allyProvideName", "You need to provide a Clan name"
                }
                , {
                    "allyLimit", "You already have the maximum allowed ally limit"
                }
                , {
                    "allyAccLimit", "You can not accept the alliance with {0}. You reached the limit"
                }
                , {
                    "allyCancel", "You have cancelled your alliance with [{0}]"
                }
                , {
                    "allyCancelSucc", "{0} has cancelled your clan alliance"
                }
                , {
                    "noAlly", "Your clans have no alliance with each other"
                }
                , {
                    "noAllyInv", "You do not have a alliance invite from [{0}]"
                }
                , {
                    "allyInvWithdraw", "You have cancelled your request to [{0}]"
                }
                , {
                    "allyDeclined", "You have declined the clan alliance from [{0}]"
                }
                , {
                    "allyDeclinedSucc", "[{0}] has declined your alliance request"
                }
                , {
                    "allyReq", "You have requested a clan alliance from [{0}]"
                }
                , {
                    "allyAcc", "You have accepted the clan alliance from [{0}]"
                }
                , {
                    "allyAccSucc", "[{0}] has accepted your alliance request"
                }
                , {
                    "allyPendingInfo", "Your clan has pending ally request(s). Check those in the clan overview."
                }
                , {
                    "clanffdisabled", "You have <color={0}>disabled</color> friendly fire for your clan.\nThey are safe!"
                }
                , {
                    "clanffenabled", "You have <color={0}>enabled</color> friendly fire for your clan.\nTake care!"
                }
                , {
                    "yourname", "YOU"
                }
                , {
                    "helpavailablecmds", "Available commands:"
                }
                , {
                    "helpinformation", "Display your clan information"
                }
                , {
                    "helpmessagemembers", "Send a message to all members"
                }
                , {
                    "helpmessageally", "Send a message to all allied members"
                }
                , {
                    "helpcreate", "Create a new clan"
                }
                , {
                    "helpjoin", "Join a clan by invitation"
                }
                , {
                    "helpleave", "Leave your clan"
                }
                , {
                    "helptoggleff", "Toggle friendlyfire status"
                }
                , {
                    "helpinvite", "Invite a player"
                }
                , {
                    "helpwithdraw", "Cancel an invite"
                }
                , {
                    "helpkick", "Kick a member"
                }
                , {
                    "helpallyoptions", "Lists the ally options"
                }
                , {
                    "helppromote", "Promote a member"
                }
                , {
                    "helpdemote", "Demote a member"
                }
                , {
                    "helpdisband", "Disband your clan (no undo)"
                }
                , {
                    "helpmoderator", "Moderator"
                }
                , {
                    "helpowner", "Owner"
                }
                , {
                    "helpcommands", "commands:"
                }
                , {
                    "helpconsole", "Open F1 console and type:"
                }
                , {
                    "yourradarstatus", "Your ClanRadar:"
                }
                , {
                    "clanradardisabled", "Clan radar disabled"
                }
                , {
                    "clanradarenabled", "Clan radar enabled"
                }
                , {
                    "helptoggleradar", "Toggle clanradar status"
                }
                , {
                    "clanArgCreate", "create"
                }
                , {
                    "clanArgInvite", "invite"
                }
                , {
                    "clanArgLeave", "leave"
                }
                , {
                    "clanArgWithdraw", "withdraw"
                }
                , {
                    "clanArgJoin", "join"
                }
                , {
                    "clanArgPromote", "promote"
                }
                , {
                    "clanArgDemote", "demote"
                }
                , {
                    "clanArgFF", "ff"
                }
                , {
                    "clanArgRadar", "radar"
                }
                , {
                    "clanArgAlly", "ally"
                }
                , {
                    "clanArgHelp", "help"
                }
                , {
                    "clanArgKick", "kick"
                }
                , {
                    "clanArgDisband", "disband"
                }
                , {
                    "clanArgForever", "forever"
                }
                , {
                    "clanArgNameId", "<name|id>"
                }
                , {
                    "allyArgRequest", "request"
                }
                , {
                    "allyArgRequestShort", "req"
                }
                , {
                    "allyArgAccept", "accept"
                }
                , {
                    "allyArgAcceptShort", "acc"
                }
                , {
                    "allyArgDecline", "decline"
                }
                , {
                    "allyArgDeclineShort", "dec"
                }
                , {
                    "allyArgCancel", "cancel"
                }
                , {
                    "allyArgCancelShort", "can"
                }
                ,
                {
                    "clanchatmuted", "You may not clanchat, you are muted."
                },
                 {
                    "clanUItitle", "Clan System by RustPlugin.ru / OxideBro"
                },
                  {
                    "clanTOPUItitle", "Clans TOP"
                },
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"nopermtocreate", "У тебя нету привилегии для создания клана."},
                 { "nopermtojoin", "У тебя нету привилегии что бы вступать в клан."},
                 { "nopermtojoinbyinvite", "У игрока {0} нету прав чтобы вступить в клан."},
                 { "claninvite", "Вас пригласили в клан: [{0}] '{1}'\nЧтобы вступить, введите: <color=#FF6c6c>/clan join {0}</color>"},
                 { "comeonline", "{0} зашел в игру!"},
                 { "goneoffline", "{0} вышел с игры!"},
                 { "friendlyfire", "{0} ваш соклановец, и не может получить урон.\nИспользуйте: <color=#FF6c6c>/clan ff</color> чтобы включить урон"},
                 { "allyfriendlyfire", "{0} является союзником и не может получить урон."},
                 { "notmember", "В настоящее время вы не являетесь членом клана."},
                 { "youareownerof", "Вы являетесь владельцем:"},
                 { "youaremodof", "Вы являетесь модератором:"},
                  {"youarecouncilof", "Вы советник в:"},
                 { "youarememberof", "Вы участник в:"},
                 { "claninfo", " [{0}] {1}"},
                  {"memberon", "Игроки клана в сети: "},
                 { "overviewnamecolor", "<color=#FF6c6c>{1}</color>"},
                 { "memberoff", "Игроки клана не в сети: "},
                 { "notmoderator", "Вы должны быть модератором своего клана, чтобы использовать эту команду."},
                 { "pendinvites", "Ожидающие приглашения: "},
                 { "bannedwords", "Tег клана содержит запрещенные слова."},
                 { "viewthehelp", "Чтобы просмотреть больше команд, введите: <color=#FF6c6c>/{1}</color>"},
                 { "hintlength", "Клановый тег должнен быть от {0} до {1} символов"},
                  {"hintchars", "Клановый тег может использовать только 'a-z' 'A-Z' '0-9' '{0}' символы"},
                 { "providedesc", "Пожалуйста, уточните краткое описание вашего клана."},
                 { "tagblocked", "Уже есть клан с этим тегом."},
                 { "nownewowner", "Теперь вы владелец клана [{0}] \"{1}\""},
                  {"inviteplayers", "Чтобы пригласить новых участников, введите: <color=#FF6c6c>/clan invite <name></color>"},
                  {"nosuchplayer", "Нет такого игрока или имя игрока указано не верно: {0}"},
                  {"alreadymember", "Этот игрок уже является членом вашего клана: {0}"},
                  {"alreadyinvited", "Этот игрок уже приглашен в ваш клан: {0}"},
                  {"alreadyinclan", "Этот игрок уже в клане: {0}"},
                 { "invitebroadcast", "{0} вступил в клан {1}."},
                 { "notinvited", "Этот игрок не был приглашен в ваш клан: {0}"},
                 { "canceledinvite", "{0} отменил приглашение {1}."},
                 { "youalreadymember", "Вы уже являетесь членом клана."},
                 { "younotinvited", "Вы не были приглашены в этот клан."},
                 { "reachedmaximum", "Этот клан уже достиг максимального количества участников."},
                 { "broadcastformat", "<color=#FF6c6c>{1}</color>: {2}"},
                 { "allybroadcastformat", "[{0}] <color=#FF6c6c>{2}</color>: {3}"},
                 { "clanrenamed", "{0} переименовал переименовал клан в: [{1}]."},
                 { "yourenamed", "Вы переименовали клан [{0}] в [{1}]"},
                  {"clandeleted", "{0} удалил свой клан."},
                 { "youdeleted", "Вы удалили клан [{0}]"},
                 { "noclanfound", "Нету клана с этим тегом [{0}]"},
                  {"renamerightsowner", "Вы должны быть администратором сервера, чтобы переименовать кланы."},
                 { "deleterightsowner", "Вы должны быть администратором сервера, чтобы удалять кланы."},
                 { "clandisbanded", "Ваш текущий клан был распущен навсегда."},
                 { "needclanowner", "Вы должны быть владельцем своего клана, чтобы использовать эту команду."},
                 { "needclanownercouncil", "Вы должны быть владельцем или модератором, чтобы использовать эту команду."},
                 { "playerjoined", "{0} присоединился к клану!"},
                 { "waskicked", "{0} выгнал {1} из клана."},
                 { "modownercannotkicked", "Игрок {0} является владельцем или модератором и не может быть выкинут."},
                 { "notmembercannotkicked", "Игрок {0} не является членом клана."},
                 { "playerleft", "{0} покинул клан."},
                 { "youleft", "Вы покинули свой текущий клан."},
                  {"notaclanmember", "Игрок {0} не является членом вашего клана."},
                 { "maximummods", "Этот клан уже достиг максимального количества модераторов."},
                  {"playerpromoted", "{0} повышен {1} до модератора."},
                 { "playerpromotedcouncil", "{0} повышен до {1} до советника."},
                 { "playerpromotedowner", "{0} повышен {1} до нового владельца."},
                 { "notamoderator", "Игрок {0} не является модератором вашего клана."},
                 { "notpromoted", "Игрок {0} не является модератором или советом вашего клана."},
                 { "playerdemoted", "{0} понижен в должности {1} до участника."},
                 { "noactiveally", "Ваш клан не имеет текущих альянсов."},
                 { "yourffstatus", "Ваш FriendlyFire:"},
                 { "yourclanallies", "Союзники вашего клана:"},
                 { "allyinvites", "Ally приглашает:"},
                 { "allypending", "запросы союзников:"},
                 { "allyReqHelp", "Предложить союз другому клану"},
                 { "allyAccHelp", "Примите союз от другого клана"},
                  {"allyDecHelp", "Отклонить союз от другого клана"},
                 { "allyCanHelp", "Отменить союз с другим кланом"},
                 { "reqAlliance", "[{0}] запросил альянс клана"},
                {  "clanNoExist", "Клан [{0}] не существует"},
                 { "allyProvideName", "Вам необходимо указать имя клана"},
                 { "allyLimit", "У вас уже есть максимально допустимое ограничение союзников"},
                 { "allyAccLimit", "Вы не можете принять альянс с {0}. Вы достигли предела"},
                {  "allyCancel", "Вы отменили свой союз с [{0}]"},
                {  "allyCancelSucc", "{0} отменил ваш клановый союз"},
                 { "noAlly", "Ваши кланы не имеют альянса друг с другом"},
                 { "noAllyInv", "У вас нет приглашения в альянс от [{0}]"},
                 { "allyInvWithdraw", "Вы отменили свой запрос к [{0}]"},
                 { "allyDeclined", "Вы отказались от альянса кланов от [{0}]"},
                {  "allyDeclinedSucc", "[{0}] отклонил ваш запрос на альянс"},
                 { "allyReq", "Вы запросили альянс клана от [{0}]"},
                 { "allyAcc", "Вы приняли клановый союз от [{0}]"},
                {  "allyAccSucc", "[{0}] принял ваш запрос на альянс"},
                 { "allyPendingInfo", "Ваш клан имеет ожидающие запросы союзников. Проверьте их в обзоре клана."},
                 { "clanffdisabled", "У вас <color=#FF6c6c> отключенный </color> дружественный огонь для вашего клана.\nЭто безопасно!"},
                 { "clanffenabled", "У вас <color=#FF6c6c> включенный </color> дружественный огонь для вашего клана.\nВнимитесь!"},
                 {"yourname", "ВЫ"},
                 { "helpavailablecmds", "Доступные команды:"},
                 { "helpinformation", "Показать информацию о вашем клане"},
                 { "helpmessagemembers", "Отправить сообщение всем участникам"},
                 { "helpmessageally", "Отправить сообщение всем союзникам"},
                 { "helpcreate", "Создание нового клана"},
                 { "helpjoin", "Вступить в клан по приглашению"},
                 { "helpleave", "Выйти с вашего текущего клана"},
                 { "helptoggleff", "Переключить статус FF"},
                 { "helpinvite", "Пригласить игрока"},
                 { "helpwithdraw", "Отменить приглашение"},
                 { "helpkick", "Кикнуть участника"},
                 { "helpallyoptions", "Список опций"},
                 { "helppromote", "Повысить участника"},
                 { "helpdemote", "Понизить участника"},
                {  "helpdisband", "Расформирование своего клана (Не отменить)"},
                {  "helpmoderator", "Модератор"},
                {  "helpowner", "Создатель"},
                 { "helpcommands", "команды:"},
                 { "helpconsole", "Откройте консоль, F1:"},
                 { "yourradarstatus", "Ваш клан радар:"},
                 { "clanradardisabled", "Клан радар отключен"},
                  {"clanradarenabled", "Клан радар включен"},
                 { "helptoggleradar", "Переключение статуса клан радара"},
                 { "clanArgCreate", "create"},
                 { "clanArgInvite", "invite"},
                 { "clanArgLeave", "leave"},
                {  "clanArgWithdraw", "withdraw"},
                {  "clanArgJoin", "join"},
                 { "clanArgPromote", "promote"},
                 { "clanArgDemote", "demote"},
                 { "clanArgFF", "ff"},
                 { "clanArgRadar", "radar"},
                {  "clanArgAlly", "ally"},
                 { "clanArgHelp", "help"},
                 { "clanArgKick", "kick"},
                 { "clanArgDisband", "disband"},
                 { "clanArgForever", "forever"},
                 { "clanArgNameId", "<name|id>"},
                 { "allyArgRequest", "request"},
                 { "allyArgRequestShort", "req"},
                 { "allyArgAccept", "accept"},
                 { "allyArgAcceptShort", "acc"},
                 { "allyArgDecline", "decline"},
                 { "allyArgDeclineShort", "dec"},
                 { "allyArgCancel", "cancel"},
                 { "allyArgCancelShort", "can"},
                 { "clanchatmuted", "Вы не можете писать в клан чат."},
                 { "clanUItitle", "Система кланов Unusual"},
                 { "clanTOPUItitle", "TOP кланов сервера"},
                 { "alreadyowner", "Игрок {0} уже владелец вашего клана."},
                 { "alreadyamod", "Игрок {0} уже капитан вашего клана."},
                {  "alreadyacouncil", "Игрок {0} уже советчик вашего клана."},
                 { "alreadyacouncilset", "Должность советчика уже установлена."},
                  {"councildemoted", "{0} пониженный {1} до модератора."},
                 { "invitePending", "У вас уже есть ожидающее приглашение в альянс для [{0}]"},
                 { "alreadyAllies", "Вы уже состоите в альянсе с"},
                 { "usagecreate", "Используйте - <color=#FF6c6c>/clan create \"TAG Клана\" \"Название клана\"</color>"},
                 { "usageinvite", "Используйте - <color=#FF6c6c>/clan invite <name></color>"},
                 { "usagewithdraw", "Используйте: <color=#FF6c6c>/clan withdraw <name></color>"},
                {  "usagejoin", "Используйте: <color=#FF6c6c>/clan join \"TAG\"</color>"},
                {  "usagerename", "Используйте: <color=#FF6c6c>/clan rename OLDTAG NEWTAG</color>"},
                {  "usagedelete", "Используйте: <color=#FF6c6c>/clan delete TAG</color>"},
                { "usagedisband", "Используйте: <color=#FF6c6c>/clan disband forever</color>"},
                {  "usagepromote", "Используйте: <color=#FF6c6c>/clan promote <name></color>"},
                {  "usageff", "Используйте: <color=#FF6c6c>/clan ff</color> toggles your current FriendlyFire status."},
                {  "usagekick", "Используйте: <color=#FF6c6c>/clan kick <name></color>"},
                {  "usageleave", "Используйте: <color=#FF6c6c>/clan leave</color>"},
                 { "usagedemote", "Используйте: <color=#FF6c6c>/clan demote <name></color>"}
            }, this, "ru");
        }

        void Init()
        {
            cc = this;
            LoadVariables();
            LoadDefaultMessages();
            Initialized = false;
            if (!permission.PermissionExists(permissionToCreateClan)) permission.RegisterPermission(permissionToCreateClan, this);
            permission.RegisterPermission("clans.forestset", this);
            if (!permission.PermissionExists(permissionToJoinClan)) permission.RegisterPermission(permissionToJoinClan, this);
            cmd.AddChatCommand(chatCommandFF, this, "cmdChatClanFF");
            //Custom code
            //cmd.AddChatCommand("clanui", this, "CLanUIInfo");

            cmd.AddChatCommand(chatCommandClan, this, "cmdChatClan");
            cmd.AddChatCommand(chatCommandClanChat, this, "cmdChatClanchat");
            cmd.AddChatCommand(chatCommandAllyChat, this, "cmdChatAllychat");
            cmd.AddChatCommand(chatCommandClanInfo, this, "cmdChatClanInfo");
            cmd.AddChatCommand(chatCommandClan + subCommandClanHelp, this, "cmdChatClanHelp");
            cmd.AddChatCommand(chatCommandClan + subCommandClanAlly, this, "cmdChatClanAlly");
            if (enableClanTagging) Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
        }

        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Title == "Better Chat" || plugin.Title == "ChatPlus")
                if (enableClanTagging) Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
        }


        string getFormattedClanTag(IPlayer player)
        {
            var clan = findClanByUser(player.Id);
            if (clan != null && !string.IsNullOrEmpty(clan.tag)) return $"[#{clanTagColorBetterChat.Replace("#", "")}][+{clanTagSizeBetterChat}]{clanTagOpening}{clan.tag}{clanTagClosing}[/+][/#]";
            return string.Empty;
        }

        [PluginReference] private Plugin ImageLibrary, TournamentBoloto;

        public string GetImageSkin(string shortname, ulong skin = 13975490) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId);


        #region LoadNewSkinsMethod

        private Dictionary<string, Dictionary<ulong, ulong>> SkinList = new Dictionary<string, Dictionary<ulong, ulong>>();

        void LoadSkins()
        {
            webrequest.Enqueue("https://files.facepunch.com/rust/icons/inventory/rust/schema.json", null, (code, response) =>
            {
                if (!(response == null && code == 200))
                {
                    var schm = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(response);
                    var items = schm.items;
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.itemshortname) && !string.IsNullOrEmpty(item.icon_url) && item.type != "None")
                        {
                            ulong owner = 0;
                            if (!SkinList.ContainsKey(item.itemshortname))
                            {
                                SkinList[item.itemshortname] = new Dictionary<ulong, ulong>();
                                SkinList[item.itemshortname][0] = 0;
                            }

                            if (item.workshopdownload != null)
                                AddSkinToList(item.itemshortname, item.itemdefid, Convert.ToUInt64(item.workshopdownload), owner);
                            else
                                AddSkinToList(item.itemshortname, item.itemdefid, item.itemdefid, owner);
                        }
                    }
                }
            }, this);
        }

        private static Dictionary<string, Dictionary<ulong, ulong>> FreeSkinList = new Dictionary<string, Dictionary<ulong, ulong>>();

        private static Dictionary<ulong, Dictionary<string, Dictionary<ulong, ulong>>> PlayerSkinList = new Dictionary<ulong, Dictionary<string, Dictionary<ulong, ulong>>>();

        private void AddSkinToList(string item, ulong skin, ulong workshopid, ulong owner = 0)
        {
            if (owner == 0)
            {
                if (SkinList[item] == null)
                {
                    SkinList[item] = new Dictionary<ulong, ulong>();
                    SkinList[item][0] = 0;
                }

                SkinList[item][skin] = workshopid;
            }
            else if (owner == 1)
            {
                if (FreeSkinList[item] == null)
                    FreeSkinList[item] = new Dictionary<ulong, ulong>();

                FreeSkinList[item][skin] = workshopid;
            }
            else
            {
                if (PlayerSkinList[owner] == null)
                    PlayerSkinList[owner] = new Dictionary<string, Dictionary<ulong, ulong>>();
                if (PlayerSkinList[owner][item] == null)
                    PlayerSkinList[owner][item] = new Dictionary<ulong, ulong>();

                PlayerSkinList[owner][item][skin] = workshopid;
            }
            //AddImage("https://rustlabs.com/img/skins/324/" + skin + ".png", item, workshopid);
        }

        #endregion
        static Clans ins;

        void OnServerInitialized()
        {
            LoadSkins();
            ins = this;
            if (useRelationshipManager)
            {
                Subscribe(nameof(OnServerCommand));
                if (!RelationshipManager.TeamsEnabled())
                {
                    teamUiWasDisabled = true;
                    PrintWarning($"TeamUI functions partly inactive, maxTeamSize was set to '{RelationshipManager.maxTeamSize}'");
                }
            }
            else
            {
                Unsubscribe(nameof(OnServerCommand));
            }


            object obj = LoadData();

            if (!ImageLibrary)
            {
                PrintError("ImageLibrary not found! Clans not work!");
            }
            else
            {
                clanCache.ToList().ForEach(c => ImageLibrary?.Call("AddImage", c.Value.owner, c.Value.owner));
            }

            Rust.Global.Runner.StartCoroutine(ServerInitialized(obj));

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            timer.Every(1f, PlayTimePlayer);
            timer.Every(60f, PlayTimePlayer);

            ImageLibrary?.Call("AddImage", "https://i.imgur.com/lQ8Yf5O.png", "logopng");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/cEBay4m.png", "loot-barrel");
            ImageLibrary?.Call("AddImage", "https://media.discordapp.net/attachments/615106543662268418/1068982385351336027/image.png", "bStats");
        }

        void PlayTimePlayer()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var clan = findClanByUser(player.UserIDString);
                if(clan != null)
                    clan.members[player.UserIDString].PlayTimeInServer += 1;
            }
        }

        void AvatarClan()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var clan = findClanByUser(player.UserIDString);
                if(clan != null)
                    ImageLibrary?.Call("AddImage", clan.owner, clan.owner);
            }
        }

        private IEnumerator ServerInitialized(object obj)
        {
            if (obj != null)
                InitializeClans((bool)obj);

            if (purgeOldClans)
                Puts($"Valid clans loaded: '{clans.Count}'");

            if (purgeOldClans && purgedClans.Count() > 0)
            {
                Puts($"Old Clans purged: '{purgedClans.Count}'");
                if (listPurgedClans)
                {
                    foreach (string purged in purgedClans)
                        Puts($"Purged > {purged}");
                }
            }

            yield return CoroutineEx.waitForSeconds(2f);

            AllyRemovalCheck();

            tagReExt = new Regex("[^a-zA-Z0-9" + allowedSpecialChars + "]");

            foreach (BasePlayer player in BasePlayer.activePlayerList.ToList())
                SetupPlayer(player);

            foreach (BasePlayer player in BasePlayer.sleepingPlayerList.ToList())
                SetupPlayer(player);

            foreach (KeyValuePair<string, Clan> clan in clans)
            {
                clan.Value.OnUpdate(false);
                clan.Value.UpdateTeam();
            }

            Initialized = true;
            yield return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (useRelationshipManager && arg != null && arg.cmd != null)
            {
                if (RelationshipManager.TeamsEnabled() || teamUiWasDisabled)
                {
                    if (arg.cmd.Name.ToLower() == "maxteamsize" && arg.FullString != string.Empty)
                    {
                        int i = arg.GetInt(0, 0);
                        if (i > 0 && teamUiWasDisabled)
                        {
                            teamUiWasDisabled = false;
                            Puts($"TeamUI functions full activated");
                            return null;
                        }
                        else if (i < 1)
                        {
                            teamUiWasDisabled = true;
                            PrintWarning($"TeamUI functions partly inactive, maxTeamSize was set to '{i}'");
                            return null;
                        }
                    }

                    Clan obj;
                    if (!RelationshipManager.TeamsEnabled())
                        return null;

                    if (arg.Connection != null && clanCache.TryGetValue(arg.Connection.userid.ToString(), out obj) && arg.cmd.Parent.ToLower() == "relationshipmanager")
                    {
                        if (disableManageFunctions)
                            return false;

                        if (arg.cmd.Name.ToLower() == "leaveteam" && allowButtonLeave)
                        {
                            LeaveClan(arg.Player());
                            return false;
                        }

                        if (arg.cmd.Name.ToLower() == "kickmember" && allowButtonKick)
                        {
                            KickPlayer(arg.Player(), arg.FullString.Trim('"'));
                            return false;
                        }

                        if (arg.cmd.Name.ToLower() == "sendinvite" && allowDirectInvite)
                        {
                            InvitePlayer(arg.Player(), arg.FullString.Trim('"'));
                            return false;
                        }

                        if (arg.cmd.Name.ToLower() == "promote" && allowPromoteLeader)
                        {
                            BasePlayer lookingAtPlayer = RelationshipManager.GetLookingAtPlayer(arg.Player());
                            if (lookingAtPlayer == null || lookingAtPlayer.IsDead() || lookingAtPlayer == arg.Player())
                                return false;

                            if (lookingAtPlayer.currentTeam == arg.Player().currentTeam)
                            {
                                bool wasCouncil = obj.IsCouncil(lookingAtPlayer.UserIDString);
                                bool wasMod = obj.IsModerator(lookingAtPlayer.UserIDString);

                                if (wasCouncil && !wasMod)
                                    obj.council = arg.Player().UserIDString;

                                if (wasMod && !wasCouncil)
                                {
                                    obj.RemoveModerator(lookingAtPlayer);
                                    obj.SetModerator(arg.Player());
                                }

                                obj.owner = lookingAtPlayer.UserIDString;
                                obj.BroadcastLoc("playerpromotedowner", obj.GetColoredName(arg.Player().UserIDString, arg.Connection.username), obj.GetColoredName(lookingAtPlayer.UserIDString, obj.FindClanMember(lookingAtPlayer.UserIDString).Name));
                                obj.OnUpdate(true);
                            }
                            return false;
                        }
                    }
                }
            }

            return null;
        }

        void OnServerSave() => SaveData();
        void OnNewSave()
        {
            if (wipeClansOnNewSave) newSaveDetected = true;
            RewardClans();
        }

        void Unload()
        {
            if (!Initialized) return;
            SaveData();

            foreach (BasePlayer player in BasePlayer.activePlayerList.ToList())
                DoCleanUp(player);

            foreach (BasePlayer player in BasePlayer.sleepingPlayerList.ToList())
                DoCleanUp(player);

            foreach (BasePlayer player in BasePlayer.activePlayerList.ToList())
                CuiHelper.DestroyUi(player, "clans_main1");

            foreach (BasePlayer player in BasePlayer.activePlayerList.ToList())
            {
                CuiHelper.DestroyUi(player, "Message");
                CuiHelper.DestroyUi(player, "setRes");
                CuiHelper.DestroyUi(player, "Layerkk");
                CuiHelper.DestroyUi(player, "MainSkinLayerMain");
                CuiHelper.DestroyUi(player, "Clanstop_main2");
            }
        }

        private void DoCleanUp(BasePlayer player)
        {
            if (player == null)
                return;

            Clan clan = findClanByUser(player.UserIDString);
            if (clan != null)
            {
                if (useRelationshipManager)
                {
                    RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    playerTeam?.RemovePlayer(player.userID);

                    player.ClearTeam();
                    RelationshipManager.ServerInstance.playerToTeam.Remove(player.userID);
                }

                if (enableClanTagging)
                {
                    string name = player.displayName.Replace($"{clanTagOpening}{clan.tag}{clanTagClosing} ", "");
                    player.displayName = name;

                    if (player.net != null)
                        player._name = string.Format("{1}[{0}/{2}]", player.net.ID, name, player.userID);
                }

                if (!Interface.Oxide.IsShuttingDown)
                {
                    if (forceNametagsOnTagging)
                        player.limitNetworking = true;

                    player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                    if (forceNametagsOnTagging)
                        player.limitNetworking = false;
                }
            }
        }


        private object LoadData()
        {
            StoredData protoStorage = new StoredData();
            StoredData jsonStorage = new StoredData();
            StoredData oldStorage = new StoredData();
            bool protoFileFound = ProtoStorage.Exists(new string[] { Title });
            bool jsonFileFound = Interface.GetMod().DataFileSystem.ExistsDatafile(Title);
            bool oldFileFound = Interface.GetMod().DataFileSystem.ExistsDatafile("rustio_clans");
            if (!protoFileFound && !jsonFileFound)
            {
                oldStorage = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("rustio_clans");
            }
            else
            {
                if (jsonFileFound)
                    jsonStorage = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);

                if (protoFileFound)
                {
                    protoStorage = ProtoStorage.Load<StoredData>(new string[]
                      {
                        Title
                      });
                }
            }

            bool lastwasProto = protoStorage.lastStorage == "proto" && (protoStorage.saveStamp > jsonStorage.saveStamp || protoStorage.saveStamp > oldStorage.saveStamp);

            if (useProtostorageClandata)
            {
                if (lastwasProto)
                {
                    clanSaves = ProtoStorage.Load<StoredData>(new string[] { Title }) ?? new StoredData();
                }
                else
                {
                    if (oldFileFound && !jsonFileFound)
                        clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("rustio_clans");

                    if (jsonFileFound)
                        clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);
                }
            }
            else
            {
                if (!lastwasProto)
                {
                    if (oldFileFound && !jsonFileFound)
                        clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("rustio_clans");

                    if (jsonFileFound)
                        clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);
                }
                else if (protoFileFound)
                {
                    clanSaves = ProtoStorage.Load<StoredData>(new string[] { Title }) ?? new StoredData();
                }
            }

            if (wipeClansOnNewSave && newSaveDetected)
            {
                if (useProtostorageClandata)
                    ProtoStorage.Save<StoredData>(clanSaves, new string[] { Title + ".bak" });
                else Interface.Oxide.DataFileSystem.WriteObject(Title + ".bak", clanSaves);

                Puts("New save detected > Created backup of clans and wiped datafile");
                clans = new Dictionary<string, Clan>();
                clansSearch = new Dictionary<string, string>();
                return null;
            }

            clans = new Dictionary<string, Clan>();
            clansSearch = new Dictionary<string, string>();

            if (clanSaves.clans == null || clanSaves.clans.Count == 0)
                return null;

            clans = clanSaves.clans;
            return !jsonFileFound && !protoFileFound;
        }

        void InitializeClans(bool newFileFound)
        {
            Dictionary<string, int> clanDuplicates = new Dictionary<string, int>();
            List<string> clanDuplicateCount = new List<string>();
            foreach (var _clan in clans.ToList())
            {
                Clan clan = _clan.Value;
                if (purgeOldClans && (UnixTimeStampUTC() - clan.updated) > (notUpdatedSinceDays * 86400))
                {
                    purgedClans.Add($"[{clan.tag}] | {clan.description} | Owner: {clan.owner} | LastUpd: {UnixTimeStampToDateTime(clan.updated)}");
                    if (permission.GroupExists(permGroupPrefix + clan.tag))
                    {
                        foreach (var member in clan.members)
                            if (permission.UserHasGroup(member.Key, permGroupPrefix + clan.tag))
                                permission.RemoveUserGroup(member.Key, permGroupPrefix + clan.tag);
                        permission.RemoveGroup(permGroupPrefix + clan.tag);
                    }
                    RemoveClan(clan.tag);
                    continue;
                }
                foreach (var member in clan.members.ToList())
                {
                    var p = covalence.Players.FindPlayerById(member.Key);
                    if (!(p is IPlayer) || p == null || p.Name == "")
                    {
                        clan.members.Remove(member.Key);
                        clan.moderators.Remove(member.Key);
                    }
                }
                if (clan.members.Count() == 0)
                {
                    RemoveClan(clan.tag);
                    continue;
                }
                if (!clan.members.ContainsKey(clan.owner)) clan.owner = clan.members.ToList()[0].Key;
                if (usePermGroups && !permission.GroupExists(permGroupPrefix + clan.tag)) permission.CreateGroup(permGroupPrefix + clan.tag, "Clan " + clan.tag, 0);
                foreach (var member in clan.members)
                {
                    if (usePermGroups && !permission.UserHasGroup(member.Key, permGroupPrefix + clan.tag)) permission.AddUserGroup(member.Key, permGroupPrefix + clan.tag);
                }
                foreach (var invited in clan.invites.ToList())
                {
                    if ((UnixTimeStampUTC() - (int)invited.Value) > (inviteValidDays * 86400)) clan.invites.Remove(invited.Key);
                }
                clanCache[clan.owner] = clan;
                foreach (var member in clan.members)
                {
                    if (!clanDuplicates.ContainsKey(member.Key))
                    {
                        clanDuplicates.Add(member.Key, 1);
                        clanCache[member.Key] = clan;
                        continue;
                    }
                    else
                    {
                        clanDuplicates[member.Key] += 1;
                        if (!clanDuplicateCount.Contains(member.Key)) clanDuplicateCount.Add(member.Key);
                    }
                    clanCache[member.Key] = clan;
                }
                foreach (var invite in clan.invites)
                {
                    if (!pendingPlayerInvites.ContainsKey(invite.Key)) pendingPlayerInvites.Add(invite.Key, new List<string>());
                    pendingPlayerInvites[invite.Key].Add(clan.tag);
                }
                var reply = 5792;

                clan.total = clan.members.Count();
                clan.mods = clan.moderators.Count();
                if (clan.created == 0) clan.created = UnixTimeStampUTC();
                if (clan.updated == 0) clan.updated = UnixTimeStampUTC();
                if (!clansSearch.ContainsKey(clan.tag.ToLower())) clansSearch.Add(clan.tag.ToLower(), clan.tag);
            }
            if (clanDuplicateCount.Count > 0) PrintWarning($"Found '{clanDuplicateCount.Count()}' player(s) in multiple clans. Check `clans.showduplicates`");
            Puts($"Loaded data with '{clans.Count}' valid Clans and overall '{clanCache.Count}' Members.");
            if (newFileFound) SaveData(true);
        }
        void SaveData(bool force = false)
        {
            if (!Initialized && !force) return;
            clanSaves.clans = clans;
            clanSaves.saveStamp = UnixTimeStampUTC();
            clanSaves.lastStorage = useProtostorageClandata ? "proto" : "json";
            if (useProtostorageClandata)
                ProtoStorage.Save<StoredData>(clanSaves, new string[] { this.Title });

            else Interface.Oxide.DataFileSystem.WriteObject(this.Title, clanSaves);
        }
        public Clan findClan(string tag)
        {
            Clan clan;
            if (TryGetClan(tag, out clan)) return clan;
            return null;
        }

        public Clan findClanByUser(string userId)
        {
            Clan clan;
            if (clanCache.TryGetValue(userId, out clan)) return clan;
            return null;
        }

        private Clan SetupPlayer(BasePlayer player, IPlayer current = null, bool hasLeft = false, Clan clan = null, bool teamForced = false, string oldTag = null)
        {
            if (player == null)
                return null;

            if (current == null)
                current = covalence.Players.FindPlayerById(player.UserIDString);

            if (current == null)
                return null;

            bool prevName = false;

            if (clan == null && !hasLeft)
                clan = findClanByUser(current.Id);

            bool flag = false;
            string oldName = player.displayName;
            player.displayName = oldName;
            player._name = oldName;


            if (clan == null || hasLeft)
            {
                if (enableClanTagging && hasLeft && oldTag != null)
                {
                    string name = player.displayName.Replace($"{clanTagOpening}{oldTag}{clanTagClosing} ", "");
                    player.displayName = name;
                    player._name = string.Format("{1}[{0}/{2}]", player.net.ID, name, player.userID);
                    prevName = true;
                }

                if (useRelationshipManager)
                    flag = NullClanTeam(player);
                clan = null;
            }
            else
            {
                if (enableClanTagging)
                {
                    string name = player.displayName.Replace($"{clanTagOpening}{(oldTag != null ? oldTag : clan.tag)}{clanTagClosing} ", "");
                    name = $"{clanTagOpening}{clan.tag}{clanTagClosing} {name}";
                    player.displayName = name;
                    player._name = string.Format("{1}[{0}/{2}]", player.net.ID, name, player.userID);
                    prevName = true;
                }

                clan.AddBasePlayer(player);
            }

            if (prevName && forceNametagsOnTagging)
                player.limitNetworking = true;

            if (flag || prevName)
                player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

            if (prevName && forceNametagsOnTagging)
                player.limitNetworking = false;

            return clan;
        }

        private bool NullClanTeam(BasePlayer player)
        {
            bool flag = false;
            if (player.currentTeam != 0UL)
            {
                RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (team == null)
                {
                    player.currentTeam = 0UL;
                    player.ClientRPCPlayer(null, player, "CLIENT_ClearTeam");
                    flag = true;
                }
            }
            else if (player.currentTeam == 0UL)
            {
                player.ClientRPCPlayer(null, player, "CLIENT_ClearTeam");
                flag = true;
            }

            return flag;
        }

        void setupPlayers(List<string> playerIds, bool remove, string tag)
        {
            if (enableClanTagging) foreach (var playerId in playerIds)
                {
                    var player = BasePlayer.Find(playerId);
                    if (player != null) SetupPlayer(player, null, remove, null, false, tag);
                }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || player.net == null || player.net.connection == null)
                return;
			var clans = findClanByUser(player.UserIDString);
            if (clans != null)
                clans.members[player.UserIDString].LastLogin = DateTime.Now;
            Clan clan = SetupPlayer(player);
            if (clan != null)
                ServerMgr.Instance.StartCoroutine(WaitForReady(player, clan));
        }

        IEnumerator WaitForReady(BasePlayer player, Clan clan = null)
        {
            yield return new WaitWhile(new System.Func<bool>(() => player.IsReceivingSnapshot && player.IsSleeping()));
            if (player.IsDead()) yield return null;
            ComingOnlineInfo(player, clan);


            yield return null;
        }
        void ComingOnlineInfo(BasePlayer player, Clan clan = null)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (player == null) return;
            if (clan != null)
            {
                if (enableComesOnlineMsg) clan.BroadcastLoc("comeonline", clan.ColNam(player.UserIDString, player.net.connection.username), "", "", "", player.UserIDString);
                if (enableWhoIsOnlineMsg)
                {
                    var sb = new StringBuilder();
                    sb.Append($"<color={colorTextMsg}>");
                    sb.Append(string.Format(msg("memberon", player.UserIDString)));
                    int n = 0;
                    foreach (var memberId in clan.members)
                    {
                        var op = this.covalence.Players.FindPlayerById(memberId.Key);
                        if (op != null && op.IsConnected)
                        {
                            var memberName = op.Name;
                            if (op.Name == player.net.connection.username) memberName = msg("yourname", player.UserIDString);
                            if (n > 0) sb.Append(", ");
                            if (clan.IsOwner(memberId.Key))
                            {
                                sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanOwnerColor, memberName));
                            }
                            else if (clan.IsCouncil(memberId.Key))
                            {
                                sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanCouncilColor, memberName));
                            }
                            else if (clan.IsModerator(memberId.Key))
                            {
                                sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanModeratorColor, memberName));
                            }
                            else
                            {
                                sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanMemberColor, memberName));
                            }
                            ++n;
                        }
                    }
                    sb.Append($"</color>");
                    PrintChat(player, sb.ToString().TrimEnd());
                }
                myClan.OnUpdate();
                clan.updated = UnixTimeStampUTC();
                manuallyEnabledBy.Remove(player.userID);
                if (enableClanAllies && (clan.IsOwner(player.UserIDString) || clan.IsCouncil(player.UserIDString)) && clan.pendingInvites.Count > 0)
                {
                    if (player != null) PrintChat(player, string.Format(msg("allyPendingInfo", player.UserIDString)));
                }
                return;
            }
            if (pendingPlayerInvites.ContainsKey(player.UserIDString))
            {
                foreach (var invitation in pendingPlayerInvites[player.UserIDString] as List<string>)
                {
                    Clan newclan = findClan(invitation);
                    if (newclan != null) timer.Once(3f, () =>
                    {
                        if (player != null) PrintChat(player, string.Format(msg("claninvite", player.UserIDString), newclan.tag, newclan.description, colorCmdUsage));
                    }
                     );
                }
            }
        }


        void OnPlayerDisconnected(BasePlayer player)
        {
            var memberofline = findClanByUser(player.UserIDString);
            if (memberofline != null)
            {
                memberofline.BroadcastLoc("goneoffline", memberofline.ColNam(player.UserIDString, player.net.connection.username), "", "", "", player.UserIDString);
                manuallyEnabledBy.Remove(player.userID);
                foreach (KeyValuePair<string, Clan> clan in clans)
                {
                    clan.Value.OnUpdate();
                }
            }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || !player.IsConnected) return;

            var clan = findClanByUser(player.UserIDString);
            if (clan != null && info.InitiatorPlayer != null && clan.members.ContainsKey(player.UserIDString))
            {
                clan.ClanPoints -= PointsOfDeath;
                clan.DeathC++;
                clan.members[player.UserIDString].PlayerPoints -= PointsOfDeath;
                clan.members[player.UserIDString].Death++;
            }
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hit)
        {
            if (!enableFFOPtion || attacker == null || hit == null || !(hit.HitEntity is BasePlayer)) return;
            OnAttackShared(attacker, hit.HitEntity as BasePlayer, hit);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit)
        {
            try
            {
                if (entity == null || hit == null) return;
                if (entity is PatrolHelicopter && hit.Initiator is BasePlayer)
                    LastHeliHit[entity.net.ID.Value] = hit.InitiatorPlayer.userID;
                if (entity is BradleyAPC && hit.Initiator is BasePlayer)
                    LastBradleyAPCHit[entity.net.ID.Value] = hit.InitiatorPlayer.userID;

                if (!enableFFOPtion || !(entity is BasePlayer) || !(hit.Initiator is BasePlayer)) return;
                OnAttackShared(hit.Initiator as BasePlayer, entity as BasePlayer, hit);
            }
            catch (NullReferenceException)
            { }
        }
        object OnAttackShared(BasePlayer attacker, BasePlayer victim, HitInfo hit)
        {
            if (bypass.Contains(victim.userID) || attacker == victim) return null;
            var victimClan = findClanByUser(victim.UserIDString);
            var attackerClan = findClanByUser(attacker.UserIDString);
            if (victimClan == null || attackerClan == null) return null;
            if (victimClan.tag == attackerClan.tag)
            {
                if (manuallyEnabledBy.Contains(attacker.userID) && !forceClanFFNoDeactivate) return null;
                DateTime now = DateTime.UtcNow;
                DateTime time;
                var key = attacker.UserIDString + "-" + victim.UserIDString;
                if (!notificationTimes.TryGetValue(key, out time) || time < now.AddSeconds(-friendlyFireNotifyTimeout))
                {
                    PrintChat(attacker, string.Format(msg("friendlyfire", attacker.UserIDString), victim.displayName, colorCmdUsage));
                    notificationTimes[key] = now;
                }
				hit.damageTypes.ScaleAll(0);
                return false;
            }
            if (victimClan.tag != attackerClan.tag && enableClanAllies && enableAllyFFOPtion)
            {
                if (!victimClan.clanAlliances.Contains(attackerClan.tag)) return null;
                if (manuallyEnabledBy.Contains(attacker.userID) && !forceAllyFFNoDeactivate) return null;
                DateTime now = DateTime.UtcNow;
                DateTime time;
                var key = attacker.UserIDString + "-" + victim.UserIDString;
                if (!notificationTimes.TryGetValue(key, out time) || time < now.AddSeconds(-friendlyFireNotifyTimeout))
                {
                    PrintChat(attacker, string.Format(msg("allyfriendlyfire", attacker.UserIDString), victim.displayName));
                    notificationTimes[key] = now;
                }
				hit.damageTypes.ScaleAll(0);
                return false;
            }
            return null;
        }
        void AllyRemovalCheck()
        {
            foreach (var ally in clans)
            {
                try
                {
                    Clan allyClan = clans[ally.Key];
                    foreach (var clanAlliance in allyClan.clanAlliances.ToList())
                    {
                        if (!clans.ContainsKey(clanAlliance)) allyClan.clanAlliances.Remove(clanAlliance);
                    }
                    foreach (var invitedAlly in allyClan.invitedAllies.ToList())
                    {
                        if (!clans.ContainsKey(invitedAlly)) allyClan.clanAlliances.Remove(invitedAlly);
                    }
                    foreach (var pendingInvite in allyClan.pendingInvites.ToList())
                    {
                        if (!clans.ContainsKey(pendingInvite)) allyClan.clanAlliances.Remove(pendingInvite);
                    }
                }
                catch
                {
                    PrintWarning("Ally removal check failed. Please contact the developer.");
                }
            }
        }
        void cmdChatClan(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (args.Length == 0)
            {
                cmdClanOverview(player);
                return;
            }
            string opt = args[0];
            if (opt == "task")
            {
				var clan = findClanByUser(player.UserIDString);

				if (clan == null)
				{
					player.ChatMessage("Вы не состоите в клане или не являетесь лидером чтобы использовать /clan task");
					return;
				}

				if (clan.owner != player.UserIDString)
				{
					player.ChatMessage("Вы не состоите в клане или не являетесь лидером чтобы использовать /clan task");
					return;
				}

				if (args.Length < 2)
				{
					SendReply(player, $"Укажите текущую задачу для вашего клана");
					return;
				}

				var task = string.Join(" ", args.Skip(1));
				if (string.IsNullOrEmpty(task)) return;

				if (task.Length > 120)
				{
                    player.ChatMessage("Вы указали слишком длинную задачу !!");
					return;
				}

				clan.Task = task;
                player.ChatMessage($"Вы успешно установили задачу игрокам клана, сообщение: {clan.Task}");
				return;
            }
            else if (opt == "set")
            {
                plugins.Find("ClansSet")?.Call("cmdChatSet", player);
                return;
            }
            else if (opt == msg("clanArgCreate", player.UserIDString))
            {
                cmdClanCreate(player, args);
                return;
            }
            else if (opt == msg("clanArgInvite", player.UserIDString))
            {
                cmdClanInvite(player, args);
                return;
            }
            else if (opt == msg("clanArgWithdraw", player.UserIDString))
            {
                cmdClanWithdraw(player, args);
                return;
            }
            else if (opt == msg("clanArgJoin", player.UserIDString))
            {
                cmdClanJoin(player, args);
                return;
            }
            else if (opt == msg("clanArgPromote", player.UserIDString))
            {
                cmdClanPromote(player, args);
                return;
            }
            else if (opt == msg("clanArgDemote", player.UserIDString))
            {
                cmdClanDemote(player, args);
                return;
            }
            else if (opt == msg("clanArgLeave", player.UserIDString))
            {
                cmdClanLeave(player, args);
                return;
            }
            else if (opt == msg("clanArgFF", player.UserIDString))
            {
                if (!enableFFOPtion) return;
                cmdChatClanFF(player, command, args);
                return;
            }
            else if (opt == msg("clanArgAlly", player.UserIDString))
            {
                if (!enableClanAllies) return;
                for (var i = 0;
                i < args.Length - 1;
                ++i)
                {
                    if (i < args.Length) args[i] = args[i + 1];
                }
                Array.Resize(ref args, args.Length - 1);
                cmdChatClanAlly(player, command, args);
                return;
            }
            else if (opt == msg("clanArgKick", player.UserIDString))
            {
                cmdClanKick(player, args);
                return;
            }
            else if (opt == msg("clanArgDisband", player.UserIDString))
            {
                cmdClanDisband(player, args);
                return;
            }
            else cmdChatClanHelp(player, command, args);
        }

        void NewClanUI(BasePlayer player, int page = 0,bool leadermode = false)
        {
            #region [Vars]
            var elements = new CuiElementContainer();
            string colored = "0 0 0 0.5";float width = 0.998f, height = 0.09f, startxBox = 0.0025f, startyBox = 0.942f - height, xmin = startxBox, ymin = startyBox;
            var clan = findClanByUser(player.UserIDString);
            if (clan == null) return;
            #endregion

            #region [DestroyUi]
            CuiHelper.DestroyUi(player, "clans_main1");
            #endregion

            #region [Parrent]
            elements.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" }
            }, "Overlay", "clans_main1");

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = "clans_main1" }
            }, "clans_main1");
            #endregion

            #region [Main-Gui]
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-403 131", OffsetMax = "138 307" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "clans_main1", "Clanstop_mainclayer1");
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "140 131", OffsetMax = "408 307" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "clans_main1", "Clanstop_mainclayer2");
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-403 -18", OffsetMax = "408 128" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "clans_main1", "Clanstop_mainclayer3");
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-403 -305", OffsetMax = "138 -21" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "clans_main1", "Clanstop_mainclayer4");
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "140 -305", OffsetMax = "408 -21" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "clans_main1", "Clanstop_mainclayer5");
            #endregion

            #region [Avatar]
            elements.Add(new CuiElement
            {
                Parent = $"Clanstop_mainclayer1",
                Components =
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary?.Call("GetImage", clan.owner)},
                    new CuiRectTransformComponent { AnchorMin = "0.045 0.1385", AnchorMax = "0.276 0.865" }
                }
            });
            #endregion

            #region [Info]
            Dictionary<string, string> InfoClan = new Dictionary<string, string>()
            {
                { "НАЗВАНИЕ КЛАНА:", $"{clan.tag}" },
                { "ГЛАВА КЛАНА:", $"{clan.ownerName.ToUpper()}" },
                { "УЧАСТНИКОВ В ИГРЕ:", $"{clan.online} из {clan.members.Count}" },
                { "ОБЩАЯ ВЫПОЛНЕННАЯ НОРМА:", $"{GetPercentClan(clan.tag)}" },
            };
            foreach (var check in InfoClan.Select((u, t) => new { A = u, B = t }))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.315 + check.B * 0 - Math.Floor((float) check.B / 1) * 1 * 0} {0.685 - Math.Floor((float) check.B/ 1) * 0.135}",
                                        AnchorMax = $"{0.955 + check.B * 0 - Math.Floor((float) check.B / 1) * 1 * 0} {0.805 - Math.Floor((float) check.B / 1) * 0.135}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "Clanstop_mainclayer1", "Clanstop_mainclayer1" + ".Info" + $".{check.B}");

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_mainclayer1" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Key}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                        new CuiRectTransformComponent { AnchorMin = $"0.01 0", AnchorMax = $"0.5 1" },
                    }
                }); 

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_mainclayer1" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleRight },
                        new CuiRectTransformComponent { AnchorMin = $"0.5 0", AnchorMax = $"0.985 1" },
                    }
                }); 
            }
            #endregion

            #region [Task]
            elements.Add(new CuiLabel
            {
                Text = { Text = "ТЕКУЩАЯ ЗАДАЧА", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" },
                RectTransform = { AnchorMin = $"0 0.84", AnchorMax = $"1 1" },
            }, "Clanstop_mainclayer2");
            elements.Add(new CuiElement
            {
                Name = "ClanTask",
                Parent = "Clanstop_mainclayer2",
                Components =
                    {
                        new CuiImageComponent {Color = "0 0 0 0.5"},
                        new CuiRectTransformComponent {AnchorMin = "0.08 0.088", AnchorMax = "0.9265 0.8375"}
                    }
            }); 
            elements.Add(new CuiLabel
            {
                    Text = { Text = string.IsNullOrEmpty(clan.Task) ? "Глава клана не указал\nтекущую задачу" : $"{clan.Task}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
            }, "ClanTask");
            #endregion

            #region [Wear]
            elements.Add(new CuiLabel
            {
                Text = { Text = "НАБОР КЛАНОВОЙ ОДЕЖДЫ", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" },
                RectTransform = { AnchorMin = $"0 0.815", AnchorMax = $"1 1" },
            }, "Clanstop_mainclayer3");
            foreach (var check in clan.SkinList["wear"].Select((y, t) => new { A = y, B = t }).Take(6))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.036 + check.B * 0.1605 - Math.Floor((float) check.B / 6) * 6 * 0.1605} {0.105 - Math.Floor((float) check.B/ 6) * 0}",
                                        AnchorMax = $"{0.16 + check.B * 0.1605 - Math.Floor((float) check.B / 6) * 6 * 0.1605} {0.795 - Math.Floor((float) check.B / 6) * 0}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "Clanstop_mainclayer3", "Clanstop_mainclayer3" + ".Wear" + $".{check.B}");

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_mainclayer3" + ".Wear" + $".{check.B}",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = check.A.Value },
                        new CuiRectTransformComponent { AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975" }
                    }
                });
                 
                if (clan.IsOwner(player.UserIDString))
                elements.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"clan_itemChange" },
                    Text = { Text = ""},
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                }, "Clanstop_mainclayer3" + ".Wear" + $".{check.B}");
            }
            #endregion

            #region [PlayerInfo]
            elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.94", AnchorMax = "0.997 0.9985" },
            }, "Clanstop_mainclayer4", "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "#", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 0.8" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "НИК ИГРОКА", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.042 0", AnchorMax = $"1 0.8" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "АКТИВНОСТЬ", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.411 0", AnchorMax = $"1 0.8" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "НОРМА", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.67 0", AnchorMax = $"1 0.8" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "ДЕЙСТВИЯ", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.843 0", AnchorMax = $"1 0.8" },
            }, "PlayerInfoPanel");
            foreach (var key in clan.members.OrderBy(pair => BasePlayer.FindByID(ulong.Parse(pair.Key)) == null).Skip(8 * page).Take(clan.members.ToList().Count >= 8 ? 8 : clan.members.ToList().Count))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 1", OffsetMax = "-2 -1" },
                    Image = { Color = colored }
                }, "Clanstop_mainclayer4", "PlayerLine");

                elements.Add(new CuiLabel
                {
                    Text = { Text = "●", Color = BasePlayer.FindByID(ulong.Parse(key.Key)) != null ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00", FontSize = 8, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.016 0", AnchorMax = $"1 1" },
                }, "PlayerLine");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{clan.GetIMember(key.Key).Name}", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.65", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.043 0", AnchorMax = $"1 1" },
                }, "PlayerLine");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{FormatShortTime(TimeSpan.FromSeconds(key.Value.PlayTimeInServer))}", Color = "1 1 1 0.65", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.4105 0", AnchorMax = $"1 1" },
                }, "PlayerLine");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{GetPercentPlayer(key.Key)}", Color = "1 1 1 0.65", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.68 0", AnchorMax = $"0.78 1" },
                }, "PlayerLine");

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.25 0.25 0.23 0", Command = $"UI_CLAN stats {key.Key}" },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "PlayerLine");

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }
            #endregion

            #region [Buttons]
            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.3", Material = "assets/icons/greyout.mat", Command = page > 0 ? $"clan.page {page - 1}" : "" },
                Text = { Text = "<", Color = "1 1 1 1", FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.01 0.015", AnchorMax = $"0.08 0.125" },
            }, "Clanstop_mainclayer4");
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.085 0.015", AnchorMax = $"0.165 0.125" },
                Image = { Color = "0 0 0 0.4", Material = "assets/icons/greyout.mat" }
            }, "Clanstop_mainclayer4", "clans_pagetext");
            elements.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                Text = { Text = $"{page + 1}", Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, "clans_pagetext");
            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.3", Material = "assets/icons/greyout.mat", Command = clan.members.Skip(8 * (page + 1)).Count() > 0 ? $"clan.page {page + 1}" : "" },
                Text = { Text = ">", Color = "1 1 1 1", FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.17 0.015", AnchorMax = $"0.243 0.125" },
            }, "Clanstop_mainclayer4");
            #endregion

            #region [Resourse]
            elements.Add(new CuiLabel
            {
                Text = { Text = "ДОБЫЧА РЕСУРСОВ", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" },
                RectTransform = { AnchorMin = $"0 0.9", AnchorMax = $"1 1" },
            }, "Clanstop_mainclayer5");

            foreach (var check in clan.Change.Select((y, t) => new { A = y, B = t }).Take(9))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.075 + check.B * 0.28 - Math.Floor((float) check.B / 3) * 3 * 0.28} {0.618 - Math.Floor((float) check.B/ 3) * 0.26}",
                                        AnchorMax = $"{0.355 + check.B * 0.28 - Math.Floor((float) check.B / 3) * 3 * 0.28} {0.88 - Math.Floor((float) check.B / 3) * 0.26}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "Clanstop_mainclayer5", "Clanstop_mainclayer5" + ".Resourse" + $".{check.B}");

                if (FindItemID(check.A.Key) != 0)
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "Clanstop_mainclayer5" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = 0 },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
                else
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "Clanstop_mainclayer5" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", check.A.Key) },
                            new CuiRectTransformComponent {AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95"}
                        }
                    });
                }

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_mainclayer5" + ".Resourse" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{GetPercent(check.A.Value.Need, check.A.Value.Complete)}", Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0.78", AnchorMax = $"0.95 1" },
                    }
                });

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_mainclayer5" + ".Resourse" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value.Complete}", Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 12, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.95 0.25" },
                    }
                });

                elements.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"clan_ResChange" },
                    Text = { Text = ""},
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                }, "Clanstop_mainclayer5" + ".Resourse" + $".{check.B}");
            }
            #endregion

            CuiHelper.AddUi(player, elements);
        }

        #region [Math]
        public string GetPercentClan(string tag)
        {
            var clan = findClan(tag);
            var need = clan.Change.Sum(x => x.Value.Need);
            var current = clan.Change.Sum(x => x.Value.Complete);
            if (need == 0 && current == 0)
                return $"NaN%";
            if (need == 0 && current > 0)
                return $"Infinity%";
            if (need > 0 && current > 0 || need > 0 && current == 0)
                return $"{current * 100 / need}%";
            return "";
        }

        public string GetPercentPlayer(string userid)
        {
            var clan = findClanByUser(userid);
            var need = clan.Change.Sum(x => x.Value.Need);
            var current = clan.members[userid].GatherInfo.Sum(p => p.Value);
            if (need == 0 && current == 0)
                return $"NaN%";
            if (need == 0 && current > 0)
                return $"Infinity%";
            if (need > 0 && current > 0 || need > 0 && current == 0)
                return $"{current * 100 / need}%";
            return "";
        }

        public string GetPercent(int need, int current)
        {
            if (need == 0 && current == 0)
                return $"NaN%";
            if (need == 0 && current > 0)
                return $"Infinity%";
            if (need > 0 && current > 0 || need > 0 && current == 0)
                return $"{current * 100 / need}%";
            return "";
        }
        #endregion

        [ConsoleCommand("clan_ResChange")]
        void cmdResChangeOfClan(ConsoleSystem.Arg args)
        {
            #region [Vars]
            var player = args.Player();
            var clan = findClanByUser(player.UserIDString);
            var elements = new CuiElementContainer();
            #endregion

            #region [Destroy-Ui]
            CuiHelper.DestroyUi(player, "clan_resoursechangemainlayer");
            #endregion

            #region [Parrent]
            elements.Add(new CuiPanel
            {
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", "clan_resoursechangemainlayer");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Command = "UI_CLAN closerespage" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
            }, "clan_resoursechangemainlayer");
            #endregion

            #region [Main-Gui]
			elements.Add(new CuiElement
			{
				Name = "MainLayerSkinLayer",
				Parent = "clan_resoursechangemainlayer",
				Components =
                {
                    new CuiImageComponent { Color = "0.3773585 0.3755785 0.3755785 0.3607843", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-160 -149", OffsetMax = "401 151"}
                }
			});

			elements.Add(new CuiElement
			{
				Name = "MainLayerItemChange",
				Parent = "clan_resoursechangemainlayer",
				Components =
                {
                    new CuiImageComponent { Color = "0.3773585 0.3755785 0.3755785 0.3607843", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -149", OffsetMax = "-170 151"}
                }
			});
            #endregion

            #region [Text]
            elements.Add(new CuiElement
            {
                Name = "MainLayerText",
                Parent = $"MainLayerSkinLayer",
                Components =
                {
                    new CuiTextComponent { Text = $"ВЫБЕРИТЕ РЕСУРСЫ", Color = "1 1 1 0.65", FontSize = 24, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0.9", AnchorMax = "1 1" },
                }
            });

            elements.Add(new CuiElement
            {
                Parent = $"MainLayerItemChange",
                Components =
                {
                    new CuiTextComponent { Text = $"ВЫБРАННЫЕ РЕСУРСЫ", Color = "1 1 1 0.65", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft},
                    new CuiRectTransformComponent { AnchorMin = "0.09 0.9", AnchorMax = "1 1" },
                }
            });
            #endregion

            #region [Resourse-Buttons]
            foreach (var check in clan.Change.Select((i, t) => new { A = i, B = t }))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.02 + check.B * 0.143 - Math.Floor((float) check.B / 7) * 7 * 0.143} {0.7 - Math.Floor((float) check.B/ 7) * 0.22}",
                                        AnchorMax = $"{0.125 + check.B * 0.143 - Math.Floor((float) check.B / 7) * 7 * 0.143} {0.895 - Math.Floor((float) check.B / 7) * 0.22}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "MainLayerSkinLayer", "MainLayerSkinLayer" + ".BResourse" + $".{check.B}");

                if (FindItemID(check.A.Key) != 0)
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "MainLayerSkinLayer" + ".BResourse" + $".{check.B}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = 0 },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
                else
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "MainLayerSkinLayer" + ".BResourse" + $".{check.B}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", check.A.Key) },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
 
                if (clan.IsOwner(player.UserIDString) || clan.IsModerator(player.UserIDString))
                elements.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"UI_RES {check.A.Key}" },
                    Text = { Text = "" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                }, "MainLayerSkinLayer" + ".BResourse" + $".{check.B}");
            }
            #endregion

            #region [Resourse-Need]
            foreach (var check in clan.Change.Select((i, t) => new { A = i, B = t }))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.085 + check.B * 0.31 - Math.Floor((float) check.B / 3) * 3 * 0.31} {0.7 - Math.Floor((float) check.B/ 3) * 0.3}",
                                        AnchorMax = $"{0.335 + check.B * 0.31 - Math.Floor((float) check.B / 3) * 3 * 0.31} {0.895 - Math.Floor((float) check.B / 3) * 0.3}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "MainLayerItemChange", "MainLayerItemChange" + ".Resourse" + $".{check.B}");

                if (FindItemID(check.A.Key) != 0)
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "MainLayerItemChange" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = 0 },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
                else
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "MainLayerItemChange" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", check.A.Key) },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }

                elements.Add(new CuiElement
                {
                    Parent = "MainLayerItemChange" + ".Resourse" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"x{check.A.Value.Need}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 -0.3", AnchorMax = $"1 0" },
                    }
                });
            }
            #endregion

            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("clan_itemChange")]
        void cmdItemChangeOfClan(ConsoleSystem.Arg args)
        {
            #region [Vars]
            var player = args.Player();
            var clan = findClanByUser(player.UserIDString);
            var elements = new CuiElementContainer();
            #endregion

            #region [Destroy-Ui]
            CuiHelper.DestroyUi(player, "clan_itemchangemainlayer");
            CuiHelper.DestroyUi(player, "MainSkinLayerMain");
            #endregion

            #region [Parrent]
            elements.Add(new CuiPanel
            {
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", "clan_itemchangemainlayer");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Command = "UI_CLAN closeskinpage" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
            }, "clan_itemchangemainlayer");
            #endregion

            #region [Main-Gui]
			elements.Add(new CuiElement
			{
				Name = "MainLayerItemChange",
				Parent = "clan_itemchangemainlayer",
				Components =
                {
                    new CuiImageComponent { Color = "0.3773585 0.3755785 0.3755785 0.3607843", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-412 0", OffsetMax = "-173 181"}
                }
			});
			elements.Add(new CuiElement
			{
				Name = "MainLayerSkinLayer",
				Parent = "clan_itemchangemainlayer",
				Components =
                {
                    new CuiImageComponent { Color = "0.3773585 0.3755785 0.3755785 0.3607843", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151 -130", OffsetMax = "410 181"}
                }
			});
            #endregion

            #region [Text]
            elements.Add(new CuiElement
            {
                Name = "MainLayerText",
                Parent = $"MainLayerSkinLayer",
                Components =
                {
                    new CuiTextComponent { Text = $"ВЫБЕРИТЕ ПРЕДМЕТ ДЛЯ ВЫБОРА СКИНА", Color = "1 1 1 0.65", FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });

            elements.Add(new CuiElement
            {
                Parent = $"MainLayerItemChange",
                Components =
                {
                    new CuiTextComponent { Text = $"ВЫБРАННЫЕ СКИНЫ", Color = "1 1 1 0.65", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0.825", AnchorMax = "1 1" },
                }
            });
            #endregion

            #region [Wear]
            foreach (var check in clan.SkinList["wear"].Select((y, t) => new { A = y, B = t }).Take(6))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.075 + check.B * 0.3 - Math.Floor((float) check.B / 3) * 3 * 0.3} {0.487 - Math.Floor((float) check.B/ 3) * 0.4}",
                                        AnchorMax = $"{0.325 + check.B * 0.3 - Math.Floor((float) check.B / 3) * 3 * 0.3} {0.8125 - Math.Floor((float) check.B / 3) * 0.4}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "MainLayerItemChange", "MainLayerItemChange" + ".Wear" + $".{check.B}");

                elements.Add(new CuiElement
                {
                    Parent = "MainLayerItemChange" + ".Wear" + $".{check.B}",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = check.A.Value },
                        new CuiRectTransformComponent { AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975" }
                    }
                });

                elements.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"UI_CLAN editskin {check.A.Key} 0" },
                    Text = { Text = ""},
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                }, "MainLayerItemChange" + ".Wear" + $".{check.B}");
            }
            #endregion

            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("UI_CLAN")]
        void cmdUIClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (!args.HasArgs()) return;
            var clan = findClanByUser(player.UserIDString);
            switch (args.Args[0])
            {
                case "stats":
                    var playerStats = clan.GetPlayerStats(args.Args[1]);
                    if (playerStats == null) return;
                    CuiHelper.DestroyUi(player, "clans_main1");
                    CreatePlayerInfo(player, clan, playerStats, args.Args[1]);
                    break;
                case "editskin":
                    if (!args.HasArgs(2)) return;

                    var page = 0;
                    if (args.HasArgs(3))
                        int.TryParse(args.Args[2], out page);

                    SelectSkinUi(player, args.Args[1], page);
                    break;
                case "closeplayerinfo":
                    CuiHelper.DestroyUi(player, "UICLAN_stats");
                    NewClanUI(player, 0);
                    break;
                case "closeskinpage":
                    CuiHelper.DestroyUi(player, "clan_itemchangemainlayer");
                    NewClanUI(player, 0);
                    break;
                case "closerespage":
                    CuiHelper.DestroyUi(player, "clan_resoursechangemainlayer");
                    NewClanUI(player, 0);
                    break;
            }
        }

        [ConsoleCommand("clanstop_info")]
        void cmdClansTopKey(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var clan = findClan(arg.Args[0]);
            if (clan != null)
            {
                ClanTOPInfo(player, clan, arg.Args.Length > 1 ? int.Parse(arg.Args[1]) : 0);
            }
        }

        [ConsoleCommand("clanstop_main")]
        void cmdClansTopMain(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            ClanTOP(player, arg.Args != null ? int.Parse(arg.Args[0]) : 0);
        }

        [ConsoleCommand("clan.page")]
        void cmdClanSetPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            NewClanUI(player, arg.Args != null ? int.Parse(arg.Args[0]) : 0);
        }

        void CreatePlayerInfo(BasePlayer player, Clan clan, PlayerStats stats, string userID)
        {
            #region [Vars]
            CuiElementContainer container = new CuiElementContainer();
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", "UICLAN_stats");

            container.Add(new CuiButton
            {
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Command = "UI_CLAN closeplayerinfo" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
            }, "UICLAN_stats");
            #endregion

            #region [Main-Gui]
            container.Add(new CuiElement
            {
                Name = "UICLAN_PlayerStats",
                Parent = "UICLAN_stats",
                Components =
                    {
                        new CuiImageComponent { Color = "0.3773585 0.3755785 0.3755785 0.3607843", Material = "assets/icons/greyout.mat" },
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-380 -120.5", OffsetMax = "127 115"}
                    }
            });

            container.Add(new CuiElement
            {
                Name = "UICLAN_PlayerStats2",
                Parent = "UICLAN_stats",
                Components =
                    {
                        new CuiImageComponent { Color = "0.3773585 0.3755785 0.3755785 0.3607843", Material = "assets/icons/greyout.mat" },
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "130 -120.5", OffsetMax = "352 115"}
                    }
            });
            #endregion

            #region [Text]
            container.Add(new CuiElement
            {
                Parent = "UICLAN_PlayerStats2",
                Components =
                    {
                        new CuiTextComponent { Text = $"ДОБЫЧА РЕСУРСОВ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20},
                        new CuiRectTransformComponent { AnchorMin = "0 0.885", AnchorMax = "1 1" },
                    }
            });
            #endregion

            #region [Resourse]
            foreach (var check in stats.GatherInfo.Select((y, t) => new { A = y, B = t }).Take(9))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.0825 + check.B * 0.285 - Math.Floor((float) check.B / 3) * 3 * 0.285} {0.60 - Math.Floor((float) check.B/ 3) * 0.27}",
                                        AnchorMax = $"{0.365 + check.B * 0.285 - Math.Floor((float) check.B / 3) * 3 * 0.285} {0.87 - Math.Floor((float) check.B / 3) * 0.27}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "UICLAN_PlayerStats2", "UICLAN_PlayerStats2" + ".Resourse" + $".{check.B}");

                if (FindItemID(check.A.Key) != 0)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "UICLAN_PlayerStats2" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = 0 },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = "UICLAN_PlayerStats2" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", check.A.Key) },
                            new CuiRectTransformComponent {AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95"}
                        }
                    });
                }

                container.Add(new CuiElement
                {
                    Parent = "UICLAN_PlayerStats2" + ".Resourse" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{clan.Change[check.A.Key].Need}", Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0.735", AnchorMax = $"0.95 1" },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = "UICLAN_PlayerStats2" + ".Resourse" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value.ToString("N3", CultureInfo.GetCultureInfo("ru-RU")).Replace(",000", "")}", Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.95 0.25" },
                    }
                });
            }
            #endregion

            #region [Avatar]
            container.Add(new CuiElement
            {
                Parent = "UICLAN_PlayerStats",
                Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", userID)},
                        new CuiRectTransformComponent{ AnchorMin = "0.045 0.3525", AnchorMax =  "0.3 0.9" },
                    }
            });
            #endregion
 
            #region [Info]
            Dictionary<string, string> StatsInfo = new Dictionary<string, string>()
            {
                { "НИК ИГРОКА:", $"{clan.GetIMember(userID).Name.ToUpper()}" },
                { "СТИМ ИГРОКА:", $"{clan.GetIMember(userID).Id}" },
                { "АКТИВНОСТЬ:", $"{FormatShortTime(TimeSpan.FromSeconds(stats.PlayTimeInServer))}" },
                { "ОБЩАЯ ВЫПОЛНЕННАЯ НОРМА:", $"{GetPercentPlayer(clan.GetIMember(userID).Id)}" },
            };

            foreach (var check in StatsInfo.Select((u, t) => new { A = u, B = t }))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.35 + check.B * 0 - Math.Floor((float) check.B / 1) * 1 * 0} {0.765 - Math.Floor((float) check.B/ 1) * 0.1}",
                                        AnchorMax = $"{0.955 + check.B * 0 - Math.Floor((float) check.B / 1) * 1 * 0} {0.8525 - Math.Floor((float) check.B / 1) * 0.1}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "UICLAN_PlayerStats", "UICLAN_PlayerStats" + ".Info" + $".{check.B}");

                container.Add(new CuiElement
                {
                    Parent = "UICLAN_PlayerStats" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Key}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0.01 0", AnchorMax = $"0.85 1" },
                    }
                }); 

                container.Add(new CuiElement
                {
                    Parent = "UICLAN_PlayerStats" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 14, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.985 1" },
                    }
                }); 
            }
            #endregion

            #region [Button]
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0.5", Material = "assets/icons/greyout.mat", Command = clan.owner != userID ? $"clan_kickplayer {userID}" : ""},
                Text = { Text = "ВЫГНАТЬ ИЗ КЛАНА", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = $"0.045 0.1065", AnchorMax = $"0.34 0.245" },
            }, "UICLAN_PlayerStats");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0.5", Material = "assets/icons/greyout.mat", Command = clan.moderators.Contains(userID) ? $"clanui_promote demote {userID}" : $"clanui_promote promote {userID}" },
                Text = { Text = clan.moderators.Contains(userID) ? "ЗАБРАТЬ МОДЕРА" : "ВЫДАТЬ МОДЕРА", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = $"0.35 0.1065", AnchorMax = $"0.645 0.245" },
            }, "UICLAN_PlayerStats");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0.5", Material = "assets/icons/greyout.mat", Command = $"clanui_leader {userID}"},
                Text = { Text = "НАЗНАЧИТЬ ГЛАВОЙ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = $"0.655 0.1065", AnchorMax = $"0.95 0.245" },
            }, "UICLAN_PlayerStats");
            #endregion

            CuiHelper.DestroyUi(player, "UICLAN_stats");
            CuiHelper.AddUi(player, container);
        }

        void cmdClanOverview(BasePlayer player)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            var sb = new StringBuilder();

            string Messages = pluginPrefixREBORNShow == true ? $"<size=14><color={pluginPrefixREBORNColor}>\n</color></size>" : "\n";
            sb.Append($"<size=18><color={pluginPrefixColor}>{this.Title}</color></size>{Messages}");

            if (myClan == null)
            {
                player.ChatMessage($"<size=20>Доступные команды:</size>\n<color=#ffde5a>/clan</color> - открыть меню клана\n<color=#ffde5a>/clan create</color> - создать клан\n<color=#ffde5a>/clan disband</color> - распустить клан\n<color=#ffde5a>/clan leave</color> - покинуть клан\n<color=#ffde5a>/clan invite</color> - пригласить в клан\n<color=#ffde5a>/clan task</color> - установить задачу для клана\n<color=#ffde5a>/clan set</color> - установить скины для клана\n<color=#ffde5a>/clan ff</color> - переключение френдли файера\n<color=#ffde5a>/clan help</color> - справка");
                return;
            }
            NewClanUI(player, 0);
        }
        void cmdClanCreate(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan != null)
            {
                player.ChatMessage("У вас уже есть <color=#ffde5a>клан</color>!");
                return;
            }
            if (usePermToCreateClan && !permission.UserHasPermission(current.Id, permissionToCreateClan))
            {
                PrintChat(player, msg("nopermtocreate", current.Id));
                return;
            }
            if (args.Length < 2)
            {
                player.ChatMessage($"Введите  <color=#ffde5a>/clan create ClanName</color> - где <color=#ffde5a>ClanName</color> - названия клана\nИмя клана не может быть более {tagLengthMax} символов.");
                return;
            }
            if (args[1].Length < tagLengthMin || args[1].Length > tagLengthMax)
            {
                player.ChatMessage($"Имя клана не может быть более <color=#ffde5a>{tagLengthMax}</color> символов.");
                return;
            }
            if (args.Length > 2)
            {
                args[2] = args[2].Trim();
                if (args[2].Length < 2 || args[2].Length > 30)
                {
                    player.ChatMessage($"Введите  <color=#ffde5a>/clan create ClanName</color> - где <color=#ffde5a>ClanName</color> - названия клана\nИмя клана не может быть более {tagLengthMax} символов.");
                    return;
                }
            }
            if (enableWordFilter && FilterText(args[1]))
            {
                player.ChatMessage("Данное название для клана имеет запрещенные слова!");
                return;
            }
            string[] clanKeys = clans.Keys.ToArray();
            clanKeys = clanKeys.Select(c => c.ToLower()).ToArray();
            if (clanKeys.Contains(args[1].ToLower()))
            {
                player.ChatMessage("Клан с таким именем <color=#ffde5a>уже</color> существует");
                return;
            }
            myClan = Clan.Create(args[1], args.Length > 2 ? args[2] : string.Empty, current.Id, current.Name, "https://www.guilded.gg/asset/GameIcons/Rust-lg.png");
            clans.Add(myClan.tag, myClan);
            clanCache[current.Id] = myClan;

            SetupPlayer(player, current, clan: myClan);
            myClan.AddBasePlayer(player);

            if (usePermGroups && !permission.GroupExists(permGroupPrefix + myClan.tag)) permission.CreateGroup(permGroupPrefix + myClan.tag, "Clan " + myClan.tag, 0);
            if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag);
            myClan.OnCreate();
            myClan.total++;
            player.ChatMessage($"Вы успешно создали клан <color=#ffde5a>{myClan.tag}</color>.");
            return;
        }
        public void InvitePlayer(BasePlayer player, string targetId) => cmdClanInvite(player, new string[] { "", targetId } );

		private Dictionary<string, int> _itemIds = new Dictionary<string, int>();

		private int FindItemID(string shortName)
		{
			int val;
			if (_itemIds.TryGetValue(shortName, out val))
				return val;

			var definition = ItemManager.FindItemDefinition(shortName);
			if (definition == null) return 0;

			val = definition.itemid;
			_itemIds[shortName] = val;
			return val;
		}

        void cmdClanInvite(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (args.Length < 2)
            {
                PrintChat(player, string.Format(msg("usageinvite", current.Id), colorCmdUsage));
                return;
            }
            if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id) && !myClan.IsModerator(current.Id))
            {
                PrintChat(player, string.Format(msg("notmoderator", current.Id)));
                return;
            }
            var invPlayer = myClan.GetIPlayer(args[1]);
            if (invPlayer == null)
            {
                PrintChat(player, string.Format(msg("nosuchplayer", current.Id), args[1]));
                return;
            }
            if (myClan.members.ContainsKey(invPlayer.Id))
            {
                PrintChat(player, string.Format(msg("alreadymember", current.Id), invPlayer.Name));
                return;
            }
            if (myClan.invites.ContainsKey(invPlayer.Id))
            {
                PrintChat(player, string.Format(msg("alreadyinvited", current.Id), invPlayer.Name));
                return;
            }
            if (findClanByUser(invPlayer.Id) != null)
            {
                PrintChat(player, string.Format(msg("alreadyinclan", current.Id), invPlayer.Name));
                return;
            }
            if (usePermToJoinClan && !permission.UserHasPermission(invPlayer.Id, permissionToJoinClan))
            {
                PrintChat(player, string.Format(msg("nopermtojoinbyinvite", current.Id), invPlayer.Name));
                return;
            }
            myClan.invites.Add(invPlayer.Id, UnixTimeStampUTC());
            if (!pendingPlayerInvites.ContainsKey(invPlayer.Id)) pendingPlayerInvites.Add(invPlayer.Id, new List<string>());
            pendingPlayerInvites[invPlayer.Id].Add(myClan.tag);
            myClan.BroadcastLoc($"{myClan.ColNam(current.Id, current.Name)} пригласил игрока {myClan.ColNam(invPlayer.Id, invPlayer.Name)} в клан.");
            if (invPlayer.IsConnected)
            {
                var invited = rust.FindPlayerByIdString(invPlayer.Id);
                if (invited != null) PrintChat(invited, string.Format(msg("claninvite", invPlayer.Id), myClan.tag, myClan.description, colorCmdUsage));
                InterfaceUI(invited, myClan.tag);
            }
            myClan.updated = UnixTimeStampUTC();
        }

        private string GetImageUrl(string shortname, ulong skinid) => ImageLibrary.CallHook("GetImageURL", shortname, skinid) as string;
        private void AddLoadOrder(IDictionary<string, string> imageList, bool replace = false) => ImageLibrary?.Call("ImportImageList", Title, imageList, (ulong)ResourceId, replace);

		private void OnPlayerRespawned(BasePlayer player)
		{
				var items = player.inventory.AllItems();
                var clan = findClanByUser(player.UserIDString);
			if (clan != null)
            {
				timer.Once(3, () => 
				{
					foreach(Item item in items)
						if(clan.SkinList["wear"].ContainsKey(item.info.shortname))
                        {
                            item.skin = clan.SkinList["wear"][item.info.shortname];
			                item.MarkDirty();

			                var heldEntity = item.GetHeldEntity();
			                if (heldEntity == null) return;

			                heldEntity.skinID = item.skin;
			                heldEntity.SendNetworkUpdate();
                        }
				});
            }
		}

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item item)
        {
            if (player == null || item == null) return;
            var clan = findClanByUser(player.UserIDString);
            if (clan != null)
            {
                if (item.skin != 0) return;
                if (clan.SkinList["wear"].ContainsKey(item.info.shortname) && item.skin != clan.SkinList["wear"][item.info.shortname] && item.skin == 0)
                {
                    item.skin = clan.SkinList["wear"][item.info.shortname];
                    item.MarkDirty();

                    var heldEntity = item.GetHeldEntity();
                    if (heldEntity == null) return;

                    heldEntity.skinID = item.skin;
                    heldEntity.SendNetworkUpdate();
                }
            }
        }

        private bool? CanEquipItem(PlayerInventory inventory, Item item, int targetPos)
        {
            if (inventory == null || item == null) return null;
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null) return null;
            var clan = findClanByUser(player.UserIDString);
            if (clan != null)
            {
                if (item.skin != 0) return null;
                if (clan.SkinList["wear"].ContainsKey(item.info.shortname) && item.skin != clan.SkinList["wear"][item.info.shortname])
                {
                    item.skin = clan.SkinList["wear"][item.info.shortname];
                    item.MarkDirty();

                    var heldEntity = item.GetHeldEntity();
                    if (heldEntity == null) return null;

                    heldEntity.skinID = item.skin;
                    heldEntity.SendNetworkUpdate();
                }
            }
            return null;
        }

        bool? CanWearItem(PlayerInventory inventory, Item item, int targetPos)
        {
            if (inventory == null || item == null) return null;
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null) return null;
            var clan = findClanByUser(player.UserIDString);
            if (clan != null)
            {
                if (clan.SkinList["wear"].ContainsKey(item.info.shortname) && item.skin != clan.SkinList["wear"][item.info.shortname])
                {
                    item.skin = clan.SkinList["wear"][item.info.shortname];
			        item.MarkDirty();

			        var heldEntity = item.GetHeldEntity();
			        if (heldEntity == null) return null;

			        heldEntity.skinID = item.skin;
			        heldEntity.SendNetworkUpdate();
                }
            }
            return null;
        }

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer() || entity == null || item == null) return;

            var player = entity.ToPlayer();
            if (player == null || player.IsNpc) return;
            var clan = findClanByUser(player.UserIDString);
            if (clan != null && clan.members[player.UserIDString].GatherInfo.ContainsKey(item.info.shortname))
            {
                if (!clan.members.ContainsKey(player.UserIDString)) return;
                switch (item.info.shortname)
                {
                    case "stones":
                        clan.members[player.UserIDString].GatherInfo["stones"] = clan.members[player.UserIDString].GatherInfo["stones"] + item.amount;
                        clan.Change["stones"].Complete = clan.Change["stones"].Complete + item.amount;
                        break;
                    case "wood":
                        clan.members[player.UserIDString].GatherInfo["wood"] = clan.members[player.UserIDString].GatherInfo["wood"] + item.amount;
                        clan.Change["wood"].Complete = clan.Change["wood"].Complete + item.amount;
                        break;
                    case "metal.ore":
                        clan.members[player.UserIDString].GatherInfo["metal.ore"] = clan.members[player.UserIDString].GatherInfo["metal.ore"] + item.amount;
                        clan.Change["metal.ore"].Complete = clan.Change["metal.ore"].Complete + item.amount;
                        break;
                    case "sulfur.ore":
                        clan.members[player.UserIDString].GatherInfo["sulfur.ore"] = clan.members[player.UserIDString].GatherInfo["sulfur.ore"] + item.amount;
                        clan.Change["sulfur.ore"].Complete = clan.Change["sulfur.ore"].Complete + item.amount;
                        break;
                    case "hq.metal.ore":
                        clan.members[player.UserIDString].GatherInfo["hq.metal.ore"] = clan.members[player.UserIDString].GatherInfo["hq.metal.ore"] + item.amount;
                        clan.Change["hq.metal.ore"].Complete = clan.Change["hq.metal.ore"].Complete + item.amount;
                        break; 
                    case "leather":
                        clan.members[player.UserIDString].GatherInfo["leather"] = clan.members[player.UserIDString].GatherInfo["leather"] + item.amount;
                        clan.Change["leather"].Complete = clan.Change["leather"].Complete + item.amount;
                        break;
                    case "cloth":
                        clan.members[player.UserIDString].GatherInfo["cloth"] = clan.members[player.UserIDString].GatherInfo["cloth"] + item.amount;
                        clan.Change["cloth"].Complete = clan.Change["cloth"].Complete + item.amount;
                        break; 
                    case "fat.animal":
                        clan.members[player.UserIDString].GatherInfo["fat.animal"] = clan.members[player.UserIDString].GatherInfo["fat.animal"] + item.amount;
                        clan.Change["fat.animal"].Complete = clan.Change["fat.animal"].Complete + item.amount;
                        break;                                                                           
                }}
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null) return;
            if (player == null || player.IsNpc) return;

            var clan = findClanByUser(player.UserIDString);
            if (clan != null && clan.members[player.UserIDString].GatherInfo.ContainsKey(item.info.shortname))
            {
                {
                    if (player == null || clan == null || item == null) return;
                    if (!clan.members.ContainsKey(player.UserIDString)) return;
                    switch (item.info.shortname)
                    {
                        case "stones":
                            clan.members[player.UserIDString].GatherInfo["stones"] = clan.members[player.UserIDString].GatherInfo["stones"] + item.amount;
                            clan.Change["stones"].Complete = clan.Change["stones"].Complete + item.amount;

                        break;
                        case "wood":
                            clan.members[player.UserIDString].GatherInfo["wood"] = clan.members[player.UserIDString].GatherInfo["wood"] + item.amount;
                            clan.Change["wood"].Complete = clan.Change["wood"].Complete + item.amount;

                        break;
                        case "metal.ore":
                            clan.members[player.UserIDString].GatherInfo["metal.ore"] = clan.members[player.UserIDString].GatherInfo["metal.ore"] + item.amount;
                            clan.Change["metal.ore"].Complete = clan.Change["metal.ore"].Complete + item.amount;

                        break;
                        case "sulfur.ore":
                            clan.members[player.UserIDString].GatherInfo["sulfur.ore"] = clan.members[player.UserIDString].GatherInfo["sulfur.ore"] + item.amount;
                            clan.Change["sulfur.ore"].Complete = clan.Change["sulfur.ore"].Complete + item.amount;

                        break;
                        case "hq.metal.ore":
                            clan.members[player.UserIDString].GatherInfo["hq.metal.ore"] = clan.members[player.UserIDString].GatherInfo["hq.metal.ore"] + item.amount;
                            clan.Change["hq.metal.ore"].Complete = clan.Change["hq.metal.ore"].Complete + item.amount;
                        break; 
                        case "leather":
                            clan.members[player.UserIDString].GatherInfo["leather"] = clan.members[player.UserIDString].GatherInfo["leather"] + item.amount;
                            clan.Change["leather"].Complete = clan.Change["leather"].Complete + item.amount;
                        break;
                        case "cloth":
                            clan.members[player.UserIDString].GatherInfo["cloth"] = clan.members[player.UserIDString].GatherInfo["cloth"] + item.amount;
                            clan.Change["cloth"].Complete = clan.Change["cloth"].Complete + item.amount;
                        break;      
                        case "fat.animal":
                            clan.members[player.UserIDString].GatherInfo["animal"] = clan.members[player.UserIDString].GatherInfo["animal"] + item.amount;
                            clan.Change["animal"].Complete = clan.Change["animal"].Complete + item.amount;
                        break;                                                                                            
                    }                   
                }
            }

        }

        private void SelectSkinUi(BasePlayer player, string shortName, int page = 0)
        {
            #region Fields
            var elements = new CuiElementContainer();
            var totalAmount = 32;
            #endregion
 
            #region [Destroy-Ui]
            CuiHelper.DestroyUi(player, "MainSkinLayerMain");
            CuiHelper.DestroyUi(player, "MainLayerText");
            #endregion

            #region [Main-Gui]
            elements.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "MainLayerSkinLayer", "MainSkinLayerMain");
            #endregion

            #region [Buttons]
            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.3", Material = "assets/icons/greyout.mat", Command = page != 0 ? $"UI_CLAN editskin {shortName} {page - 1}" : "" },
                Text = { Text = "<", Color = "1 1 1 0.65", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.01 0.02", AnchorMax = $"0.0625 0.115" },
            }, "MainSkinLayerMain");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.3", Material = "assets/icons/greyout.mat", Command = SkinList[shortName].Count > (page + 1) * totalAmount ? $"UI_CLAN editskin {shortName} {page + 1}" : "" },
                Text = { Text = ">", Color = "1 1 1 0.65", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.08 0.02", AnchorMax = $"0.1325 0.115" },
            }, "MainSkinLayerMain");
            #endregion

            #region [Wear]
            foreach (var check in SkinList[shortName].Select((i, t) => new { A = i, B = t - page * totalAmount}).Skip(page * totalAmount).Take(totalAmount))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.009 + check.B * 0.125 - Math.Floor((float) check.B / 8) * 8 * 0.125} {0.795 - Math.Floor((float) check.B/ 8) * 0.22}",
                                        AnchorMax = $"{0.114 + check.B * 0.125 - Math.Floor((float) check.B / 8) * 8 * 0.125} {0.98 - Math.Floor((float) check.B / 8) * 0.22}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "MainSkinLayerMain", "MainSkinLayerMain" + ".Wear" + $".{check.B}");

                elements.Add(new CuiElement
                {
                    Parent = "MainSkinLayerMain" + ".Wear" + $".{check.B}",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(shortName), SkinId = check.A.Value },
                        new CuiRectTransformComponent { AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975" }
                    }
                });

                elements.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"clan_changeskin {shortName} {check.A.Value}" },
                    Text = { Text = ""},
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                }, "MainSkinLayerMain" + ".Wear" + $".{check.B}");
            }
            #endregion

            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("join")]
        void ConsoleClanJoin(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan != null)
            {
                PrintChat(player, string.Format(msg("youalreadymember", current.Id)));
                return;
            }
            if (usePermToJoinClan && !permission.UserHasPermission(current.Id, permissionToJoinClan))
            {
                PrintChat(player, msg("nopermtojoin", current.Id));
                return;
            }
            if (args.Args.Length != 1)
            {
                PrintChat(player, string.Format(msg("usagejoin", current.Id), colorCmdUsage));
                return;
            }
            myClan = findClan(args.Args[0]);
            if (myClan == null || !myClan.IsInvited(current.Id))
            {
                PrintChat(player, string.Format(msg("younotinvited", current.Id)));
                return;
            }
            if (limitMembers >= 0 && myClan.members.Count() >= limitMembers)
            {
                PrintChat(player, string.Format(msg("reachedmaximum", current.Id)));
                return;
            }
            CuiHelper.DestroyUi(player, Layer);
            myClan.invites.Remove(current.Id);
            pendingPlayerInvites.Remove(current.Id);
            myClan.members.Add(current.Id, new PlayerStats());
            clanCache[current.Id] = myClan;
            myClan.AddBasePlayer(player);
            SetupPlayer(player, current, clan: myClan);


            if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("playerjoined", myClan.ColNam(current.Id, current.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.total++;
            myClan.OnUpdate();
            List<string> others = new List<string>(myClan.members.Keys);
            others.Remove(current.Id);
            Interface.Oxide.CallHook("OnClanMemberJoined", current.Id, others);
        }

        string Layer = "Interface_UI";

        void InterfaceUI(BasePlayer player, string name)
        {
            CuiHelper.DestroyUi(player, "Layer");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.65" }
            }, "Overlay", Layer);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0.1 0 0.8" }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "1.00 1.00 1.00 0.03", Material = "assets/content/ui/scope_2.mat" }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-244 -117.5", OffsetMax = "246.5 119" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.4607843", Material = "assets/icons/greyout.mat" }
            }, Layer, "InterfaceInvite");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.55", AnchorMax = "1 1" },
                Text = { Text = $"Клан <color=#a5e664>{name}</color> приглашает вас!", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
            }, "InterfaceInvite");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.296 0.465", AnchorMax = "0.703 0.616" },
                Button = { Color = "0.38 0.71 0.12 0.45", Material = "assets/icons/greyout.mat", Command = $"join {name}" },
                Text = { Text = $"Принять", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 14 }
            }, "InterfaceInvite");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.296 0.282", AnchorMax = "0.703 0.425" },
                Button = { Color = "1 0 0 0.45", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = $"Отклонить", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 14 }
            }, "InterfaceInvite");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "✘", Color = "1 1 1 0.65", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.905 0.825", AnchorMax = $"0.955 0.925" },
            }, "InterfaceInvite");

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("UI_RES")]
        private void setRes(ConsoleSystem.Arg args)
        {
            #region [Vars]
            var player = args.Player();
            var container = new CuiElementContainer();
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.925" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", "setRes");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Close = "setRes" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
            }, "setRes");
            #endregion

            #region [Main-Gui]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199 -79.5", OffsetMax = "201.5 81.5" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.44", Material = "assets/icons/greyout.mat" }
            }, "setRes", "panel");
            #endregion

            #region [Text]
            container.Add(new CuiElement
            {
                Parent = $"panel",
                Components =
                {
                    new CuiTextComponent { Text = $"ВВЕДИТЕ КОЛИЧЕСТВО", Color = "1 1 1 0.85", FontSize = 24, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0.81", AnchorMax = "1 1" },
                }
            });

            container.Add(new CuiElement
            {
                Parent = $"panel",
                Components =
                {
                    new CuiTextComponent { Text = $"ДОПУСТИМЫЙ МАКСИМУМ: {NeedLimit[args.Args[0]].ToString("N3", CultureInfo.GetCultureInfo("ru-RU")).Replace(",000", "")}", Color = "1 1 1 0.65", FontSize = 16, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft},
                    new CuiRectTransformComponent { AnchorMin = "0.194 0.38", AnchorMax = "1 1" },
                }
            });
            #endregion

            #region [Input]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.125 0.225", AnchorMax = "0.875 0.47" },
                Image = { Color = "0 0 0 0.5" }
            }, "panel", "inputpanel");

            container.Add(new CuiElement
            {
                Parent = $"inputpanel",
                Components =
                {
                    new CuiTextComponent { Text = $"ВВЕДИТЕ КОЛИЧЕСТВО И НАЖМИТЕ ENTER", Color = "1 1 1 0.1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });

            container.Add(new CuiElement()
            {
                Parent = "inputpanel",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        CharsLimit = 8,
                        FontSize = 18,
                        Command = $"clan_Change {args.Args[0]} ",
                        Font = "robotocondensed-regular.ttf",
                        Text = "",
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    }
                }
            });
            #endregion

            CuiHelper.DestroyUi(player, "setRes");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("clan_Change")]
        void cmdSetNewChangeOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();

            if (args.GetString(1) == "") return;

            int amount;
            if (!int.TryParse(args.Args[1], out amount)) return;

            if (NeedLimit.ContainsKey(args.Args[0]))
            {
                if (amount > NeedLimit[args.Args[0]]) return;
            }

            var clan = findClanByUser(player.UserIDString);

            if (clan.Change.ContainsKey(args.Args[0]))
            {
                clan.Change[args.Args[0]].Need = amount;
            }


            CuiHelper.DestroyUi(player, "setRes");
            cmdResChangeOfClan(args);
        }

        class Position
        {
            public float Xmin;
            public float Xmax;
            public float Ymin;
            public float Ymax;

            public string AnchorMin =>
                $"{Math.Round(Xmin, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymin, 4).ToString(CultureInfo.InvariantCulture)}";
            public string AnchorMax =>
                $"{Math.Round(Xmax, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymax, 4).ToString(CultureInfo.InvariantCulture)}";

            public override string ToString()
            {
                return $"----------\nAmin:{AnchorMin}\nAmax:{AnchorMax}\n----------";
            }
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static List<Position> GetPositions(int colums, int rows, float colPadding = 0, float rowPadding = 0, bool columsFirst = false)
        {
            if (colums == 0)
                throw new ArgumentException("Can't create positions for gui!", nameof(colums));
            if (rows == 0)
                throw new ArgumentException("Can't create positions for gui!", nameof(rows));

            List<Position> result = new List<Position>();
            result.Clear();
            var colsDiv = 1f / colums;
            var rowsDiv = 1f / rows;
            if (colPadding == 0) colPadding = colsDiv / 2;
            if (rowPadding == 0) rowPadding = rowsDiv / 2;
            if (!columsFirst)
                for (int j = rows; j >= 1; j--)
                {
                    for (int i = 1; i <= colums; i++)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        };
                        result.Add(pos);
                    }
                }
            else
                for (int i = 1; i <= colums; i++)
                {
                    for (int j = rows; j >= 1; j--)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        };
                        result.Add(pos);
                    }
                }
            return result;
        }
        [ConsoleCommand("clan_changeskin")]
        void cmdChatSkinOfClan(ConsoleSystem.Arg args)
        {
            if (args.GetString(1) == "") return;
            var player = args.Player();
            if (player == null) return;
            var clan = findClanByUser(player.UserIDString);
            if (clan == null) return;

            ulong SkinID;
            if (!ulong.TryParse(args.Args[1], out SkinID)) return;
            var value = clan.SkinList.FirstOrDefault(p => p.Value.ContainsKey(args.Args[0])).Key;
            clan.SkinList[value][args.Args[0]] = SkinID;
            cmdItemChangeOfClan(args);
        }

        [ConsoleCommand("clan_kickplayer")]
        void cmdKickOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            KickPlayer(player, args.Args[0]);
            CuiHelper.DestroyUi(player, "UICLAN_stats");
            NewClanUI(player, 0);
        }

        private Dictionary<ulong, ulong> LastHeliHit = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, ulong> LastBradleyAPCHit = new Dictionary<ulong, ulong>();

        private BasePlayer GetLastHeliAttacker(ulong heliNetId)
        {
            ulong player;
            LastHeliHit.TryGetValue(heliNetId, out player);
            return BasePlayer.FindByID(player);
        }
        private BasePlayer GetLastBradleyAPCAttacker(ulong BradleyAPCNetId)
        {
            ulong player;
            LastBradleyAPCHit.TryGetValue(BradleyAPCNetId, out player);
            return BasePlayer.FindByID(player);
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity?.net?.ID == null) return;

            if (entity is PatrolHelicopter)
            {
                var newClan = findClanByUser(GetLastHeliAttacker(entity.net.ID.Value).UserIDString);
                if (newClan == null) return;
                newClan.ClanPoints += PointsOfKilledHeli;
                newClan.members[GetLastHeliAttacker(entity.net.ID.Value).UserIDString].PlayerPoints += PointsOfKilledHeli;
                foreach (var pp in BasePlayer.activePlayerList)
                    pp.ChatMessage($"");
                return;
            }

            var player = info?.InitiatorPlayer;
            if (player == null) return;
            var clan = findClanByUser(player.UserIDString);
            if (clan == null) return;
            if (entity is BradleyAPC && GetLastBradleyAPCAttacker(entity.net.ID.Value) == player)
            {
                clan.ClanPoints += PointsOfKilledBradleyAPC;
                clan.members[player.UserIDString].PlayerPoints += PointsOfKilledBradleyAPC;
                foreach (var pp in BasePlayer.activePlayerList)
                    pp.ChatMessage($"Танк был взорван кланом <color=orange>{clan.tag}</color>");
            }
            if (entity.PrefabName.Contains("barrel"))
            {
                clan.members[player.UserIDString].GatherInfo["loot-barrel"] = clan.members[player.UserIDString].GatherInfo["loot-barrel"] + 1;
                clan.Change["loot-barrel"].Complete = clan.Change["loot-barrel"].Complete + 1;

            }
            if (entity.ToPlayer() != null)
            {
                if (entity.GetComponent<NPCPlayer>() != null || IsNPC(entity.ToPlayer())) return;
                if (entity.ToPlayer() == info.Initiator.ToPlayer())
                {
                    clan.ClanPoints -= PointsOfSuicide;
                    clan.members[player.UserIDString].Suicide++;
                    clan.members[player.UserIDString].PlayerPoints -= PointsOfSuicide;
                }
                else
                {
                    clan.ClanPoints += PointsOfKilled;
                    clan.members[player.UserIDString].Killed++;
                    clan.KillC++;
                    clan.members[player.UserIDString].PlayerPoints += PointsOfKilled;
                }

            }      
        }   

        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L) || player.UserIDString.Length < 17) return true;
            return false;
        }

        /*int GetPercent(int need, int current) => current * 100 / need;

        double GetPercentFUll(double need, double current) => current * 100 / need;*/

        [ChatCommand("ctop")]
        void cmdClanTOP(BasePlayer player, string command, string[] args)
        {
            ClanTOP(player, 0);
        }

        public int GetClanIndex(string key)
        {
            var ClanMembers = from pair in clans orderby pair.Value.ClanPoints descending select pair;
            int index = 1;
            foreach (KeyValuePair<string, Clan> clanIndex in ClanMembers)
            {
                if (clanIndex.Value.tag == key)
                    return index;
                index++;
            }
            return 0;
        }

        string GetPlayerStatus(string player, Clan clan)
        {
            string status = "";

            if (clan == null) return "Обычный игрок";
            if (clan.members.ContainsKey(player)) status = "Участник";
            if (clan.moderators.Contains(player)) status = "Капитан";
            if (clan.owner.Contains(player)) status = "Лидер";
            return status;
        }

        void ClanTOPInfo(BasePlayer player, Clan clan, int page)
        {
            #region [Vars]
            var elements = new CuiElementContainer();
            string colored = "0 0 0 0.5";
            int i = 0;
            var ClanMembers = from pair in clan.members orderby pair.Value.PlayerPoints descending select pair;
            #endregion

            #region [DestroyUi]
            CuiHelper.DestroyUi(player, "Clanstop_main2");
            CuiHelper.DestroyUi(player, "Clanstop_info2");
            #endregion

            #region [Parrent]
            elements.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" }
            }, "Overlay", "Clanstop_info2");

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = "Clanstop_info2" }
            }, "Clanstop_info2");
            #endregion

            #region [Main-Gui]
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-255 -218", OffsetMax = "260 220" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "Clanstop_info2", "Clanstop_infolayer");
            #endregion

            #region [Avatar]
            elements.Add(new CuiElement
            {
                Parent = $"Clanstop_infolayer",
                Components =
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary?.Call("GetImage", clan.owner) },
                    new CuiRectTransformComponent { AnchorMin = "0.0635 0.63", AnchorMax = "0.31 0.92" }
                }
            });
            #endregion

            #region [Buttons]
            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.55", Material = "assets/icons/greyout.mat", Close = "Clanstop_info2" },
                Text = { Text = "ЗАКРЫТЬ", Color = "1 1 1 0.75",Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                RectTransform = { AnchorMin = $"0.795 0.011", AnchorMax = $"0.989 0.0825" },
            }, "Clanstop_infolayer");
            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.55", Material = "assets/icons/greyout.mat", Command = "Clanstop_main" },
                Text = { Text = "НАЗАД", Color = "1 1 1 0.75",Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                RectTransform = { AnchorMin = $"0.593 0.011", AnchorMax = $"0.785 0.0825" },
            }, "Clanstop_infolayer");
            #endregion

            #region [Page]
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.0705 0.011", AnchorMax = $"0.135 0.0825" },
                Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
            }, "Clanstop_infolayer", "Clanstop_infopagetext");

            elements.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                Text = { Text = $"{page + 1}", Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, "Clanstop_infopagetext");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.65", Material = "assets/icons/greyout.mat", Command = page > 0 ? $"clanstop_info {clan.tag} {page - 1}" : "" },
                Text = { Text = "-", Color = "1 1 1 1", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.009 0.011", AnchorMax = $"0.0705 0.0825" },
            }, "Clanstop_infolayer");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.65", Material = "assets/icons/greyout.mat", Command = ClanMembers.Skip(5 * (page + 1)).Count() > 0 ? $"clanstop_info {clan.tag} {page + 1}" : "" },
                Text = { Text = "+", Color = "1 1 1 1", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.135 0.011", AnchorMax = $"0.1975 0.0825" },
            }, "Clanstop_infolayer");
            #endregion

            #region [Resourse]
            foreach (var check in clan.Change.Select((y, t) => new { A = y, B = t }).Take(9))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.025 + check.B * 0.1074 - Math.Floor((float) check.B / 9) * 9 * 0.1074} {0.455 - Math.Floor((float) check.B/ 9) * 0}",
                                        AnchorMax = $"{0.115 + check.B * 0.1074 - Math.Floor((float) check.B / 9) * 9 * 0.1074} {0.5625 - Math.Floor((float) check.B / 9) * 0}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "Clanstop_infolayer", "Clanstop_infolayer" + ".Resourse" + $".{check.B}");

                if (FindItemID(check.A.Key) != 0)
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "Clanstop_infolayer" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = 0 },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
                else
                {
                    elements.Add(new CuiElement
                    {
                        Parent = "Clanstop_infolayer" + ".Resourse" + $".{check.B}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", check.A.Key) },
                            new CuiRectTransformComponent {AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95"}
                        }
                    });
                }

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_infolayer" + ".Resourse" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value.Complete}", Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 12, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.89 0.35" },
                    }
                });
            }
            #endregion

            #region [Text]
            elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.37", AnchorMax = "0.9975 0.4" },
            }, "Clanstop_infolayer", "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "#", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.2 1" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "ИМЯ ИГРОКА", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.11 0", AnchorMax = $"0.4 1" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "ОЧКОВ", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.498 0", AnchorMax = $"0.85 1" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "АКТИВНОСТЬ", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.596 0", AnchorMax = $"1 1" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "УБИЙСТВ", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.751 0", AnchorMax = $"1 1" },
            }, "PlayerInfoPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = "СМЕРТЕЙ", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.869 0", AnchorMax = $"1 1" },
            }, "PlayerInfoPanel");
            #endregion

            #region [PlayerInfo]
            for (int y = 0; y < 5; y++)
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.0055 {0.318 - y * 0.055}", AnchorMax = $"0.99 {0.365 - y * 0.055}" },
                    Image = { Color = colored }
                }, "Clanstop_infolayer", "Clanstop_infolayer" + $".Line{y}");
            }

            foreach (KeyValuePair<string, PlayerStats> key in ClanMembers.Skip(5 * page).Take(ClanMembers.ToList().Count >= 5 ? 5 : ClanMembers.ToList().Count))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    Image = { Color = "0 0 0 0" }
                }, "Clanstop_infolayer" + $".Line{i}");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{i + (1 + (page * 10))}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"0.1 1" },
                }, "Clanstop_infolayer" + $".Line{i}");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{clan.GetIMember(key.Key).Name}", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.1075 0", AnchorMax = $"0.4 1" },
                }, "Clanstop_infolayer" + $".Line{i}");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.PlayerPoints}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.215 0", AnchorMax = $"0.85 1" },
                }, "Clanstop_infolayer" + $".Line{i}");
                
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"0%", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.32 0", AnchorMax = $"1 1" },
                }, "Clanstop_infolayer" + $".Line{i}");

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.Killed}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.605 0", AnchorMax = $"1 1" },
                }, "Clanstop_infolayer" + $".Line{i}");

				elements.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.Death}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.84 0", AnchorMax = $"1 1" },
                }, "Clanstop_infolayer" + $".Line{i}");

                i++;
            }
            #endregion

            #region [InfoClan]
            Dictionary<string, string> InfoClan = new Dictionary<string, string>()
            {
                { "ГЛАВА КОМАНДЫ:", $"{clan.ownerName}" },
                { "ИГРОКОВ В КОМАНДЕ:", $"{clan.members.Count}" },
                { "НАБРАНО ОЧКОВ:", $"{clan.ClanPoints}" },
                { "ОБЩАЯ АКТИВНОСТЬ:", $"0%" },
                { "ВСЕГО УБИЙСТВ:", $"{clan.KillC}" },
                { "ВСЕГО СМЕРТЕЙ:", $"{clan.DeathC}" },
            };
            foreach (var check in InfoClan.Select((u, t) => new { A = u, B = t }))
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.3675 + check.B * 0 - Math.Floor((float) check.B / 1) * 1 * 0} {0.868 - Math.Floor((float) check.B/ 1) * 0.0485}",
                                        AnchorMax = $"{0.938 + check.B * 0 - Math.Floor((float) check.B / 1) * 1 * 0} {0.91 - Math.Floor((float) check.B / 1) * 0.0485}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, "Clanstop_infolayer", "Clanstop_infolayer" + ".Info" + $".{check.B}");

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_infolayer" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Key}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                        new CuiRectTransformComponent { AnchorMin = $"0.015 0", AnchorMax = $"0.5 1" },
                    }
                }); 

                elements.Add(new CuiElement
                {
                    Parent = "Clanstop_infolayer" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleRight },
                        new CuiRectTransformComponent { AnchorMin = $"0.5 0", AnchorMax = $"0.985 1" },
                    }
                }); 
            }
            #endregion

            #region [Title-Clan]
			elements.Add(new CuiLabel
            {
                Text = { Text = clan.online > 0 ? $"<color=lime>●</color> <b>{clan.tag}</b>" : $"<color=red>●</color> <b>{clan.tag}</b>", Color = "1 1 1 1", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0 0.92", AnchorMax = $"1 1" },
            }, "Clanstop_infolayer");
            #endregion

            CuiHelper.AddUi(player, elements);
        }

        void ClanTOP(BasePlayer player, int page)
        {
            #region [Vars]
            var myClan = findClanByUser(player.UserIDString);
            var ClanMembers = from pair in clans orderby pair.Value.ClanPoints descending select pair;
            var elements = new CuiElementContainer();
            string colored = "0.2 0.2 0.2 0.55";
            int i = 0;            
            #endregion
            
            #region [DestroyUi]
            CuiHelper.DestroyUi(player, "Clanstop_info2");
            CuiHelper.DestroyUi(player, "Clanstop_main2");
            #endregion

            #region [Parrent]
            elements.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" }
            }, "Overlay", "Clanstop_main2");

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = "Clanstop_main2" }
            }, "Clanstop_main2");
            #endregion

            #region [Main-Gui]
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-255 -79", OffsetMax = "260 226" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "Clanstop_main2", "Clanstop_mainlayer");

            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-255 -174", OffsetMax = "260 -85" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, "Clanstop_main2", "Clanstop_desclayer");
            #endregion

            #region [Text]
            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.933", AnchorMax = "0.997 0.997" },
                Image = { Color = "0 0 0 0" }
            }, "Clanstop_mainlayer", "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"#", Color = "1 1 1 1", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.033 0", AnchorMax = $"1 1" },
            }, "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"НАЗВАНИЕ КЛАНА", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.094 0", AnchorMax = $"1 1" },
            }, "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"НАГРАДА", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.352 0", AnchorMax = $"1 1" },
            }, "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.536 0", AnchorMax = $"1 1" },
            }, "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"ОЧКИ", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.717 0", AnchorMax = $"1 1" },
            }, "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"ИГРОКОВ", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.865 0", AnchorMax = $"1 1" },
            }, "Clanstop_mainlayertext");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"Очки даются:\nУбийство +30, добыча руды +1, разрушение бочки +1, сбитие вертолета +1000, уничтожение танка +500\nОчки отнимаются:\nСмерть -15, самоубийство -15\nНаграда выдается после вайпа на сервере!", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.0125 0", AnchorMax = $"1 1" },
            }, "Clanstop_desclayer");
            #endregion

            #region [Button]

            if (myClan != null)
            {
                elements.Add(new CuiButton
                {
                    Button = { Color = "0.38 0.62 0.12 0.85", Command = $"clanstop_info {myClan.tag}", Material = "assets/icons/greyout.mat"},
                    Text = { Text = "МОЙ КЛАН", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                    RectTransform = { AnchorMin = $"0.4 0.0185", AnchorMax = $"0.6 0.125" },
                }, "Clanstop_mainlayer");
            }

            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = "Clanstop_main2"},
                Text = { Text = "ЗАКРЫТЬ", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                RectTransform = { AnchorMin = $"0.8 0.0185", AnchorMax = $"0.99 0.125" },
            }, "Clanstop_mainlayer");

            elements.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.088 0.0185", AnchorMax = $"0.165 0.125" },
                Image = { Color = "0.2 0.2 0.2 0.25", Material = "assets/icons/greyout.mat" }
            }, "Clanstop_mainlayer", "clans_pagetext");

            elements.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                Text = { Text = $"{page + 1}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, "clans_pagetext");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = page > 0 ? $"clanstop_main {page - 1}" : "" },
                Text = { Text = "-", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                RectTransform = { AnchorMin = $"0.01 0.0185", AnchorMax = $"0.088 0.125" },
            }, "Clanstop_mainlayer");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = clans.Keys.Skip(10 * (page + 1)).Count() > 0 ? $"clanstop_main {page + 1}" : "" },
                Text = { Text = "+", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                RectTransform = { AnchorMin = $"0.165 0.0185", AnchorMax = $"0.244 0.125" },
            }, "Clanstop_mainlayer");
            #endregion

            #region [ClanInfo]
            for (int y = 0; y < 10; y++)
            {
                elements.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.0055 {0.858 - y * 0.0787}", AnchorMax = $"0.989 {0.93 - y * 0.0787}" },
                    Image = { Color = colored }
                }, "Clanstop_mainlayer", "Clanstop_mainlayer" + $".TopLine{y}");
            }

            foreach (KeyValuePair<string, Clan> key in ClanMembers.Skip(10 * page).Take(ClanMembers.ToList().Count >= 10 ? 10 : ClanMembers.ToList().Count))
            {
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{i + (1 + (page * 10))}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.067 1" },
                }, "Clanstop_mainlayer" + $".TopLine{i}");

                elements.Add(new CuiLabel
                {
                    Text = { Text = key.Value.online > 0 ? "<color=#a5e664>●</color>" : "<color=red>●</color>", Font = "robotocondensed-regular.ttf", FontSize = 7, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.0925 0", AnchorMax = $"0.44 1" },
                }, "Clanstop_mainlayer" + $".TopLine{i}");

                elements.Add(new CuiLabel
                {
                    Text = { Text = key.Value.tag.ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.1085 0", AnchorMax = $"0.45 1" },
                }, "Clanstop_mainlayer" + $".TopLine{i}");

                if (ClanBonus.ContainsKey(i + (1 + (page * 10))))
                {
                    elements.Add(new CuiLabel
                    {
                        Text = { Text = $"{ClanBonus[i + (1 + (page * 10))]}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.785 1" },
                    }, "Clanstop_mainlayer" + $".TopLine{i}");
                }


                elements.Add(new CuiLabel
                {
                    Text = { Text = key.Value.ClanPoints.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.705 0", AnchorMax = $"0.79 1" },
                }, "Clanstop_mainlayer" + $".TopLine{i}"); 

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.members.Count}".ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.91 0", AnchorMax = $"1 1" },
                }, "Clanstop_mainlayer" + $".TopLine{i}");

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.25 0.25 0.23 0", Command = $"clanstop_info {key.Value.tag}" },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "Clanstop_mainlayer" + $".TopLine{i}");

                i++;
            }
            #endregion

            CuiHelper.AddUi(player, elements);
        }


        long GetFullPercent(string id)
        {
            var clan = findClanByUser(id);
            long Need = clan.Change.Sum(x => x.Value.Need);
            var playerCurrent = clan.members[id].GatherInfo.Sum(p => p.Value);
            if (playerCurrent == 0) return 0;
            return playerCurrent * 100 / Need;
        }

        long GetFullClanPercent(string tag)
        {
            var clan = findClan(tag);
            long Need = clan.Change.Sum(x => x.Value.Need);
            var Current = clan.Change.Sum(x => x.Value.Complete);
            if (Current == 0) return 0;
            return Current * 100 / Need;
        }

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{time.Days} д. ";

            if (time.Hours != 0)
                result += $"{time.Hours} час. ";

            if (time.Minutes != 0)
                result += $"{time.Minutes} мин. ";

            if (time.Seconds != 0)
                result += $"{time.Seconds} сек. ";

            return result;
        }

        public void WithdrawPlayer(BasePlayer player, string targetId) => cmdClanWithdraw(player, new string[] { "", targetId });

        void cmdClanWithdraw(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (args.Length < 2)
            {
                PrintChat(player, string.Format(msg("usagewithdraw", current.Id), colorCmdUsage));
                return;
            }
            if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id) && !myClan.IsModerator(current.Id))
            {
                PrintChat(player, string.Format(msg("notmoderator", current.Id)));
                return;
            }
            var disinvPlayer = myClan.GetIPlayer(args[1]);
            if (disinvPlayer == null)
            {
                PrintChat(player, string.Format(msg("nosuchplayer", current.Id), args[1]));
                return;
            }
            if (myClan.members.ContainsKey(disinvPlayer.Id))
            {
                PrintChat(player, string.Format(msg("alreadymember", current.Id), disinvPlayer.Name));
                return;
            }
            if (!myClan.invites.ContainsKey(disinvPlayer.Id))
            {
                PrintChat(player, string.Format(msg("notinvited", current.Id), disinvPlayer.Name));
                return;
            }
            myClan.invites.Remove(disinvPlayer.Id);
            if (pendingPlayerInvites.ContainsKey(disinvPlayer.Id)) pendingPlayerInvites[disinvPlayer.Id].Remove(myClan.tag);
            myClan.BroadcastLoc("canceledinvite", myClan.ColNam(current.Id, current.Name), myClan.ColNam(disinvPlayer.Id, disinvPlayer.Name));
            myClan.updated = UnixTimeStampUTC();
        }
        void cmdClanJoin(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan != null)
            {
                PrintChat(player, string.Format(msg("youalreadymember", current.Id)));
                return;
            }
            if (usePermToJoinClan && !permission.UserHasPermission(current.Id, permissionToJoinClan))
            {
                PrintChat(player, msg("nopermtojoin", current.Id));
                return;
            }
            if (args.Length != 2)
            {
                PrintChat(player, string.Format(msg("usagejoin", current.Id), colorCmdUsage));
                return;
            }
            myClan = findClan(args[1]);
            if (myClan == null || !myClan.IsInvited(current.Id))
            {
                PrintChat(player, string.Format(msg("younotinvited", current.Id)));
                return;
            }
            if (limitMembers >= 0 && myClan.members.Count() >= limitMembers)
            {
                PrintChat(player, string.Format(msg("reachedmaximum", current.Id)));
                return;
            }
            myClan.invites.Remove(current.Id);
            pendingPlayerInvites.Remove(current.Id);
            myClan.members.Add(current.Id, new PlayerStats());
            clanCache[current.Id] = myClan;
            myClan.AddBasePlayer(player);
            SetupPlayer(player, current, clan: myClan);


            if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("playerjoined", myClan.ColNam(current.Id, current.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.total++;
            myClan.OnUpdate();
            List<string> others = new List<string>(myClan.members.Keys);
            others.Remove(current.Id);
            Interface.Oxide.CallHook("OnClanMemberJoined", current.Id, others);
        }


        [ConsoleCommand("clanui_promote")]
        void cmdClanPromoteMember(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;

            var clan = findClanByUser(player.UserIDString);
            if (!clan.IsOwner(player.UserIDString)) return;
            if (clan.owner == args.Args[1]) return;

            switch (args.Args[0].ToLower())
            {
                case "promote":
                    PromotePlayer(player, args.Args[1]);
                    break;
                case "demote":
                    DemotePlayer(player, args.Args[1]);
                    break;
            }

            var playerStats = clan.GetPlayerStats(args.Args[1]);
            if (playerStats == null) return;
            CreatePlayerInfo(player, clan, playerStats, args.Args[1]);
        }

        [ConsoleCommand("clanui_leader")]
        void cmdOwnerChange(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;

            var clan = findClanByUser(player.UserIDString);
            if (!clan.IsOwner(player.UserIDString)) return;

            var promotePlayer = clan.GetIPlayer(args.Args[0]);
            if (promotePlayer == null) return;
            if (clan.IsOwner(promotePlayer.Id)) return;
            
            clan.moderators.Remove(promotePlayer.Id);
            clan.owner = promotePlayer.Id;
            clan.ownerName = promotePlayer.Name;
            clan.updated = UnixTimeStampUTC();
            clan.OnUpdate();

            var playerStats = clan.GetPlayerStats(args.Args[0]);
            if (playerStats == null) return;
            CreatePlayerInfo(player, clan, playerStats, args.Args[0]);
        }

        public void PromotePlayer(BasePlayer player, string targetId) => cmdClanPromote(player, new string[] { "", targetId });

        void cmdClanPromote(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (!myClan.IsOwner(current.Id))
            {
                PrintChat(player, string.Format(msg("needclanowner", current.Id)));
                return;
            }
            if (args.Length != 2)
            {
                PrintChat(player, string.Format(msg("usagepromote", current.Id), colorCmdUsage));
                return;
            }
            var promotePlayer = myClan.GetIMember(args[1]);
            if (promotePlayer == null)
            {
                PrintChat(player, string.Format(msg("nosuchplayer", current.Id), args[1]));
                return;
            }
            if (!myClan.IsMember(promotePlayer.Id))
            {
                PrintChat(player, string.Format(msg("notaclanmember", current.Id), promotePlayer.Name));
                return;
            }
            if (enableClanAllies && myClan.IsCouncil(promotePlayer.Id))
            {
                PrintChat(player, string.Format(msg("alreadyacouncil", current.Id), promotePlayer.Name));
                return;
            }
            if (enableClanAllies && myClan.council != null && myClan.IsModerator(promotePlayer.Id))
            {
                PrintChat(player, string.Format(msg("alreadyacouncilset", current.Id), promotePlayer.Name));
                return;
            }
            if (!enableClanAllies && myClan.IsModerator(promotePlayer.Id))
            {
                PrintChat(player, string.Format(msg("alreadyamod", current.Id), promotePlayer.Name));
                return;
            }
            if (!myClan.IsModerator(promotePlayer.Id) && limitModerators >= 0 && myClan.moderators.Count() >= limitModerators)
            {
                PrintChat(player, string.Format(msg("maximummods", current.Id)));
                return;
            }
            if (enableClanAllies && myClan.IsModerator(promotePlayer.Id))
            {
                myClan.council = promotePlayer.Id;
                myClan.moderators.Remove(promotePlayer.Id);
                myClan.BroadcastLoc("playerpromotedcouncil", myClan.ColNam(current.Id, current.Name), myClan.ColNam(promotePlayer.Id, promotePlayer.Name));
            }
            else
            {
                myClan.moderators.Add(promotePlayer.Id);
                myClan.BroadcastLoc("playerpromoted", myClan.ColNam(current.Id, current.Name), myClan.ColNam(promotePlayer.Id, promotePlayer.Name));
            }
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
        }
        public void DemotePlayer(BasePlayer player, string targetId) => cmdClanDemote(player, new string[] {
"", targetId
}
        );
        void cmdClanDemote(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (!myClan.IsOwner(current.Id))
            {
                PrintChat(player, string.Format(msg("needclanowner", current.Id)));
                return;
            }
            if (args.Length < 2)
            {
                PrintChat(player, string.Format(msg("usagedemote", current.Id), colorCmdUsage));
                return;
            }
            var demotePlayer = myClan.GetIMember(args[1]);
            if (demotePlayer == null)
            {
                PrintChat(player, string.Format(msg("nosuchplayer", current.Id), args[1]));
                return;
            }
            if (!myClan.IsMember(demotePlayer.Id))
            {
                PrintChat(player, string.Format(msg("notaclanmember", current.Id), demotePlayer.Name));
                return;
            }
            if (!myClan.IsModerator(demotePlayer.Id) && !myClan.IsCouncil(demotePlayer.Id))
            {
                PrintChat(player, string.Format(msg("notpromoted", current.Id), demotePlayer.Name));
                return;
            }
            if (enableClanAllies && myClan.IsCouncil(demotePlayer.Id))
            {
                myClan.council = null;
                if (limitModerators >= 0 && myClan.moderators.Count() >= limitModerators) myClan.BroadcastLoc("playerdemoted", myClan.ColNam(current.Id, current.Name), myClan.ColNam(demotePlayer.Id, demotePlayer.Name));
                else
                {
                    myClan.moderators.Add(demotePlayer.Id);
                    myClan.BroadcastLoc("councildemoted", myClan.ColNam(current.Id, current.Name), myClan.ColNam(demotePlayer.Id, demotePlayer.Name));
                }
            }
            else
            {
                myClan.moderators.Remove(demotePlayer.Id);
                myClan.BroadcastLoc("playerdemoted", myClan.ColNam(current.Id, current.Name), myClan.ColNam(demotePlayer.Id, demotePlayer.Name));
            }
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
        }
        public void LeaveClan(BasePlayer player) => cmdClanLeave(player, new string[] {
"leave"
}
        );
        void cmdClanLeave(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            bool lastMember = false;
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (args.Length != 1)
            {
                player.ChatMessage("Используйте: <color=#ffde5a>/clan leave</color>");
                return;
            }
            if(myClan.IsOwner(current.Id))
            {
                player.ChatMessage("Вы <color=#ffde5a>являетесь</color> главой клана. С начала передайте лидера.");
                return;
            }
            if (myClan.members.Count() == 1)
            {
                RemoveClan(myClan.tag);
                lastMember = true;
            }
            else
            {
                if (myClan.IsCouncil(current.Id)) myClan.council = null;
                myClan.moderators.Remove(current.Id);
                myClan.members.Remove(current.Id);
                myClan.invites.Remove(current.Id);
                if (myClan.IsOwner(current.Id) && myClan.members.Count() > 0)
                {
                    myClan.owner = myClan.members.ToList()[0].Key;
                }
            }
            clanCache.Remove(current.Id);
            SetupPlayer(player, current, true, oldTag: myClan.tag);
            if (usePermGroups && permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup(current.Id, permGroupPrefix + myClan.tag);
            player.ChatMessage($"Вы покинули клан <color=#ffde5a>{myClan.tag}</color>");
            myClan.updated = UnixTimeStampUTC();
            myClan.total--;
            myClan.OnUpdate();
            if (lastMember) myClan.OnDestroy();
            if (!lastMember) Interface.CallHook("OnClanMemberGone", UInt64.Parse(current.Id), myClan.tag);
            //if (!lastMember) Interface.Oxide.CallHook("OnClanMemberGone", current.Id, myClan.members.ToList());
        }
        public void KickPlayer(BasePlayer player, string targetId) => cmdClanKick(player, new string[] {
"", targetId
}
        );
        void cmdClanKick(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id) && !myClan.IsModerator(current.Id))
            {
                PrintChat(player, string.Format(msg("notmoderator", current.Id)));
                return;
            }
            if (args.Length != 2)
            {
                PrintChat(player, string.Format(msg("usagekick", current.Id), colorCmdUsage));
                return;
            }
            var kickPlayer = myClan.GetIMember(args[1]);
            if (kickPlayer == null)
            {
                PrintChat(player, string.Format(msg("nosuchplayer", current.Id), args[1]));
                return;
            }
            if (!myClan.IsMember(kickPlayer.Id) && !myClan.IsInvited(kickPlayer.Id))
            {
                PrintChat(player, string.Format(msg("notmembercannotkicked", current.Id), kickPlayer.Name));
                return;
            }
            if (myClan.IsOwner(kickPlayer.Id) || myClan.IsCouncil(kickPlayer.Id) || myClan.IsModerator(kickPlayer.Id))
            {
                PrintChat(player, string.Format(msg("modownercannotkicked", current.Id), kickPlayer.Name));
                return;
            }
            foreach (var value in myClan.members)
            {
                var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>();
                foreach (var turret in turrets)
                {
                    if (turret.OwnerID != ulong.Parse(value.Key)) continue;
                    turret.authorizedPlayers.RemoveAll(a => a.userid == ulong.Parse(kickPlayer.Id));
                }
            }
            if (myClan.members.ContainsKey(kickPlayer.Id)) myClan.total--;
            myClan.members.Remove(kickPlayer.Id);
            myClan.invites.Remove(kickPlayer.Id);
            if (pendingPlayerInvites.ContainsKey(kickPlayer.Id)) pendingPlayerInvites[kickPlayer.Id].Remove(myClan.tag);
            clanCache.Remove(kickPlayer.Id);
            var kickBasePlayer = BasePlayer.Find(kickPlayer.Id);

            if (kickBasePlayer != null)
            {
                SetupPlayer(kickBasePlayer, kickPlayer, true, oldTag: myClan.tag);
            }
            if (usePermGroups && permission.UserHasGroup(kickPlayer.Id, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup(kickPlayer.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("waskicked", myClan.ColNam(current.Id, current.Name), myClan.ColNam(kickPlayer.Id, kickPlayer.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
            Interface.CallHook("OnClanMemberGone", UInt64.Parse(kickPlayer.Id), myClan.tag);
        }
        public void DisbandClan(BasePlayer player) => cmdClanDisband(player, new string[] {
"disband", "forever"
}
        );
        void cmdClanDisband(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            bool lastMember = false;
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (!myClan.IsOwner(current.Id))
            {
                player.ChatMessage("Вы не глава вашего клана!");
                return;
            }
            if (args.Length != 2)
            {
                player.ChatMessage("Чтобы подтвердить удаление клана введите <color=red>/clan disband yes</color>");
                return;
            }
            if (myClan.members.Count() == 1)
            {
                lastMember = true;
            }
            setupPlayers(myClan.members.Keys.ToList(), true, myClan.tag);

            RemoveClan(myClan.tag);
            foreach (var member in myClan.members)
            {
                clanCache.Remove(member.Key);
                if (usePermGroups && permission.UserHasGroup((string)member.Key, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup((string)member.Key, permGroupPrefix + myClan.tag);
            }
            player.ChatMessage("Клан успешно <color=red>удален</color>");

            foreach (var ally in clans)
            {
                Clan allyClan = clans[ally.Key];
                allyClan.clanAlliances.Remove(myClan.tag);
                allyClan.invitedAllies.Remove(myClan.tag);
                allyClan.pendingInvites.Remove(myClan.tag);
            }
            if (usePermGroups && permission.GroupExists(permGroupPrefix + myClan.tag)) permission.RemoveGroup(permGroupPrefix + myClan.tag);
            myClan.OnDestroy();
            AllyRemovalCheck();
            if (!lastMember)
            {
                List<UInt64> MemberListUInt64 = new List<UInt64>();
                MemberListUInt64 = myClan.members.Select(x => Convert.ToUInt64(x.Key)).ToList();
                Interface.Oxide.CallHook("OnClanDisbanded", myClan.tag, MemberListUInt64);
            }
        }
        public void Alliance(BasePlayer player, string targetClan, string type) => cmdChatClanAlly(player, "ally", new string[] {
type, targetClan
}
        );
        void cmdChatClanAlly(BasePlayer player, string command, string[] args)
        {
            if (!enableClanAllies || player == null) return;
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id))
            {
                PrintChat(player, string.Format(msg("needclanownercouncil", current.Id)));
                return;
            }
            if (args == null || args.Length == 0)
            {
                var sbally = new StringBuilder();

                string Messages = pluginPrefixREBORNShow == true ? $"<size=14><color={pluginPrefixREBORNColor}>REBORN\n</color></size>" : "\n";
                sbally.Append($"<size=18><color={pluginPrefixColor}>{this.Title}</color></size>{Messages}");
                sbally.Append($"<color={colorTextMsg}>");
                if (myClan.IsOwner(current.Id)) sbally.Append(string.Format(msg("youareownerof", current.Id)));
                else if (myClan.IsCouncil(current.Id)) sbally.Append(string.Format(msg("youarecouncilof", current.Id)));
                else if (myClan.IsModerator(current.Id)) sbally.Append(string.Format(msg("youaremodof", current.Id)));
                else sbally.Append(string.Format(msg("youarememberof", current.Id)));
                sbally.AppendLine($" <color={colorClanNamesOverview}>{myClan.tag}</color> ( {myClan.online}/{myClan.total} )");
                if (myClan.clanAlliances.Count() > 0) sbally.AppendLine(string.Format(msg("yourclanallies", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.clanAlliances.ToArray()) + "</color>");
                if ((myClan.invitedAllies.Count() > 0 || myClan.pendingInvites.Count() > 0) && (myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id)))
                {
                    if (myClan.invitedAllies.Count() > 0) sbally.Append(string.Format(msg("allyinvites", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.invitedAllies.ToArray()) + "</color> ");
                    if (myClan.pendingInvites.Count() > 0) sbally.Append(string.Format(msg("allypending", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.pendingInvites.ToArray()) + "</color> ");
                    sbally.AppendLine();
                }
                string commandtext = string.Empty;
                if (command.Contains("ally")) commandtext = command;
                else commandtext = chatCommandClan + " ally";
                sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <{msg("allyArgRequest", current.Id)} | {msg("allyArgRequestShort", current.Id)}> <clantag></color>");
                sbally.AppendLine(" " + msg("allyReqHelp", current.Id));
                sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <{msg("allyArgAccept", current.Id)} | {msg("allyArgAcceptShort", current.Id)}> <clantag></color>");
                sbally.AppendLine(" " + msg("allyAccHelp", current.Id));
                sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <{msg("allyArgDecline", current.Id)} | {msg("allyArgDeclineShort", current.Id)}> <clantag></color>");
                sbally.AppendLine(" " + msg("allyDecHelp", current.Id));
                sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <{msg("allyArgCancel", current.Id)} | {msg("allyArgCancelShort", current.Id)}> <clantag></color>");
                sbally.AppendLine(" " + msg("allyCanHelp", current.Id));
                sbally.Append("</color>");
                SendReply(player, sbally.ToString().TrimEnd());
                return;
            }
            else if (args != null && args.Length >= 1 && args.Length < 2)
            {
                PrintChat(player, string.Format(msg("allyProvideName", current.Id)));
                return;
            }
            else if (args.Length >= 1)
            {
                Clan targetClan = null;
                string opt = args[0];
                if (opt == msg("allyArgRequest", current.Id) || opt == msg("allyArgRequestShort", current.Id))
                {
                    if (limitAlliances != 0 && myClan.clanAlliances.Count >= limitAlliances)
                    {
                        PrintChat(player, string.Format(msg("allyLimit", current.Id)));
                        return;
                    }
                    if (myClan.invitedAllies.Contains(args[1]))
                    {
                        PrintChat(player, string.Format(msg("invitePending", current.Id), args[1]));
                        return;
                    }
                    if (myClan.clanAlliances.Contains(args[1]))
                    {
                        PrintChat(player, string.Format(msg("alreadyAllies", current.Id)));
                        return;
                    }
                    targetClan = findClan(args[1]);
                    if (targetClan == null)
                    {
                        PrintChat(player, string.Format(msg("clanNoExist", current.Id), args[1]));
                        return;
                    }
                    targetClan.pendingInvites.Add(myClan.tag);
                    myClan.invitedAllies.Add(targetClan.tag);
                    PrintChat(player, string.Format(msg("allyReq", current.Id), args[1]));
                    targetClan.AllyBroadcastLoc("reqAlliance", myClan.tag);
                    myClan.OnUpdate();
                    targetClan.OnUpdate();
                    return;
                }
                else if (opt == msg("allyArgAccept", current.Id) || opt == msg("allyArgAcceptShort", current.Id))
                {
                    if (!myClan.pendingInvites.Contains(args[1]))
                    {
                        PrintChat(player, string.Format(msg("noAllyInv", current.Id), args[1]));
                        return;
                    }
                    targetClan = findClan(args[1]);
                    if (targetClan == null)
                    {
                        PrintChat(player, string.Format(msg("clanNoExist", current.Id), args[1]));
                        return;
                    }
                    if (limitAlliances != 0 && myClan.clanAlliances.Count >= limitAlliances)
                    {
                        PrintChat(player, string.Format(msg("allyAccLimit", current.Id), targetClan.tag));
                        targetClan.invitedAllies.Remove(myClan.tag);
                        myClan.pendingInvites.Remove(targetClan.tag);
                        return;
                    }
                    targetClan.invitedAllies.Remove(myClan.tag);
                    targetClan.clanAlliances.Add(myClan.tag);
                    myClan.pendingInvites.Remove(targetClan.tag);
                    myClan.clanAlliances.Add(targetClan.tag);
                    myClan.OnUpdate();
                    targetClan.OnUpdate();
                    PrintChat(player, string.Format(msg("allyAcc", current.Id), targetClan.tag));
                    targetClan.AllyBroadcastLoc("allyAccSucc", myClan.tag);
                    return;
                }
                else if (opt == msg("allyArgDeclineallyArgDecline", current.Id) || opt == msg("allyArgDeclineShort", current.Id))
                {
                    if (!myClan.pendingInvites.Contains(args[1]))
                    {
                        PrintChat(player, string.Format(msg("noAllyInv", current.Id), args[1]));
                        return;
                    }
                    targetClan = findClan(args[1]);
                    if (targetClan == null)
                    {
                        PrintChat(player, string.Format(msg("clanNoExist", current.Id), args[1]));
                        return;
                    }
                    targetClan.invitedAllies.Remove(myClan.tag);
                    myClan.pendingInvites.Remove(targetClan.tag);
                    AllyRemovalCheck();
                    PrintChat(player, string.Format(msg("allyDeclined", current.Id), args[1]));
                    myClan.OnUpdate();
                    targetClan.OnUpdate();
                    targetClan.AllyBroadcastLoc("allyDeclinedSucc", myClan.tag);
                    return;
                }
                else if (opt == msg("allyArgCancel", current.Id) || opt == msg("allyArgCancelShort", current.Id))
                {
                    if (!myClan.clanAlliances.Contains(args[1]))
                    {
                        if (myClan.invitedAllies.Contains(args[1]))
                        {
                            myClan.invitedAllies.Remove(args[1]);
                            targetClan = findClan(args[1]);
                            if (targetClan != null) targetClan.pendingInvites.Remove(myClan.tag);
                            PrintChat(player, string.Format(msg("allyInvWithdraw", current.Id), args[1]));
                            myClan.OnUpdate();
                            targetClan.OnUpdate();
                            return;
                        }
                        PrintChat(player, string.Format(msg("noAlly", current.Id)));
                        return;
                    }
                    targetClan = findClan(args[1]);
                    if (targetClan == null)
                    {
                        PrintChat(player, string.Format(msg("clanNoExist", current.Id), args[1]));
                        return;
                    }
                    targetClan.clanAlliances.Remove(myClan.tag);
                    myClan.clanAlliances.Remove(targetClan.tag);
                    AllyRemovalCheck();
                    PrintChat(player, string.Format(msg("allyCancel", current.Id), args[1]));
                    myClan.OnUpdate();
                    targetClan.OnUpdate();
                    targetClan.AllyBroadcastLoc("allyCancelSucc", myClan.tag);
                    return;
                }
                else cmdChatClanAlly(player, command, new string[] { }
                );
            }
        }
        void cmdChatClanHelp(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            var sb = new StringBuilder();
            if (myClan == null)
            {
                sb.Append($"<color={colorTextMsg}>");
                sb.AppendLine(msg("helpavailablecmds", current.Id));
                sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgCreate", current.Id)} \"TAG\" \"Description\"</color> - {msg("helpcreate", current.Id)}");
                sb.Append($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgJoin", current.Id)} \"TAG\"</color> - {msg("helpjoin", current.Id)}");
                sb.Append("</color>");
                SendReply(player, sb.ToString().TrimEnd());
                return;
            }
            sb.AppendLine(msg("helpavailablecmds", current.Id));
            sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan}</color> - {msg("helpinformation", current.Id)}");
            sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClanChat} <msg></color> - {msg("helpmessagemembers", current.Id)}");
            if (enableClanAllies) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandAllyChat} <msg></color> - {msg("helpmessageally", current.Id)}");
            sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgLeave", current.Id)}</color> - {msg("helpleave", current.Id)}");
            if (enableFFOPtion) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgFF", current.Id)} |  /{chatCommandFF}</color> - {msg("helptoggleff", current.Id)}");
            if ((myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id) || myClan.IsModerator(current.Id)))
            {
                sb.AppendLine($"<color={clanModeratorColor}>{msg("helpmoderator", current.Id)}</color> {msg("helpcommands", current.Id)}");
                sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgInvite", current.Id)} {msg("clanArgNameId", current.Id)}</color> - {msg("helpinvite", current.Id)}");
                sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgWithdraw", current.Id)} {msg("clanArgNameId", current.Id)}</color> - {msg("helpwithdraw", current.Id)}");
                sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgKick", current.Id)} {msg("clanArgNameId", current.Id)}</color> - {msg("helpkick", current.Id)}");
            }
            if ((myClan.IsOwner(current.Id) || (enableClanAllies && myClan.IsCouncil(current.Id))))
            {
                sb.AppendLine($"<color={clanOwnerColor}>{msg("helpowner", current.Id)}</color> {msg("helpcommands", current.Id)}");
                if (enableClanAllies) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgAlly", current.Id)} | {chatCommandClan + "ally"}</color> - {msg("helpallyoptions", current.Id)}");
                if (myClan.IsOwner(current.Id)) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgPromote", current.Id)} {msg("clanArgNameId", current.Id)}</color> - {msg("helppromote", current.Id)}");
                if (myClan.IsOwner(current.Id)) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgDemote", current.Id)} {msg("clanArgNameId", current.Id)}</color> - {msg("helpdemote", current.Id)}");
                if (myClan.IsOwner(current.Id)) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgDisband", current.Id)} {msg("clanArgForever", current.Id)}</color> - {msg("helpdisband", current.Id)}");
            }
            if (player.net.connection.authLevel >= authLevelDelete || player.net.connection.authLevel >= authLevelRename || player.net.connection.authLevel >= authLevelInvite || player.net.connection.authLevel >= authLevelKick || player.net.connection.authLevel >= authLevelPromoteDemote) sb.AppendLine($"<color={clanServerColor}>Server management</color>: {msg("helpconsole", current.Id)} <color={colorCmdUsage}>BEE RUST</color>");
            string openText = $"<color={colorTextMsg}>";
            string closeText = "</color>";
            string[] parts = sb.ToString().Split(new char[] {
'\n'
}
            , StringSplitOptions.RemoveEmptyEntries);
            sb = new StringBuilder();
            foreach (var part in parts)
            {
                if ((sb.ToString().TrimEnd().Length + part.Length + openText.Length + closeText.Length) > 1050)
                {
                    SendReply(player, openText + sb.ToString().TrimEnd() + closeText);
                    sb.Clear();
                }
                sb.AppendLine(part);
            }
            SendReply(player, openText + sb.ToString().TrimEnd() + closeText);
        }
        void cmdChatClanInfo(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (player.net.connection.authLevel < authLevelClanInfo)
            {
                PrintChat(player, "No access to this command.");
                return;
            }
            if (args == null || args.Length == 0)
            {
                PrintChat(player, "Please specify a clan tag.");
                return;
            }
            var Clan = findClan(args[0]);
            if (Clan == null)
            {
                PrintChat(player, string.Format(msg("clanNoExist", player.UserIDString), args[0]));
                return;
            }
            var sb = new StringBuilder();
            string Messages = pluginPrefixREBORNShow == true ? $"<size=14><color= {pluginPrefixREBORNColor}>REBORN\n</color></size>" : "\n";
            sb.Append($"<size=18><color={pluginPrefixColor}>{this.Title}</color></size>{Messages}");
            sb.AppendLine($"<color={colorTextMsg}>Detailed clan information for:");
            sb.AppendLine($"ClanTag:  <color={colorClanNamesOverview}>{Clan.tag}</color> ( Online: <color={colorClanNamesOverview}>{Clan.online}</color> / Total: <color={colorClanNamesOverview}>{Clan.total}</color> )");
            sb.AppendLine($"Description: <color={colorClanNamesOverview}>{Clan.description}</color>");
            sb.Append(string.Format(msg("memberon", player.UserIDString)));
            int n = 0;
            foreach (var memberId in Clan.members)
            {
                var op = this.covalence.Players.FindPlayerById(memberId.Key);
                if (op != null && op.IsConnected)
                {
                    if (n > 0) sb.Append(", ");
                    if (Clan.IsOwner(memberId.Key))
                    {
                        sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanOwnerColor, op.Name));
                    }
                    else if (Clan.IsCouncil(memberId.Key))
                    {
                        sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanCouncilColor, op.Name));
                    }
                    else if (Clan.IsModerator(memberId.Key))
                    {
                        sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanModeratorColor, op.Name));
                    }
                    else
                    {
                        sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanMemberColor, op.Name));
                    }
                    ++n;
                }
            }
            if (Clan.online == 0) sb.Append(" - ");
            sb.Append("</color>\n");
            bool offline = false;
            foreach (var memberId in Clan.members)
            {
                var op = this.covalence.Players.FindPlayerById(memberId.Key);
                if (op != null && !op.IsConnected)
                {
                    offline = true;
                    break;
                }
            }
            if (offline)
            {
                sb.Append(string.Format(msg("memberoff", player.UserIDString)));
                n = 0;
                foreach (var memberId in Clan.members)
                {
                    var p = this.covalence.Players.FindPlayerById(memberId.Key);
                    if (p != null && !p.IsConnected)
                    {
                        if (n > 0) sb.Append(", ");
                        if (Clan.IsOwner(memberId.Key))
                        {
                            sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanOwnerColor, p.Name));
                        }
                        else if (Clan.IsCouncil(memberId.Key))
                        {
                            sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanCouncilColor, p.Name));
                        }
                        else if (Clan.IsModerator(memberId.Key))
                        {
                            sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanModeratorColor, p.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanMemberColor, p.Name));
                        }
                        ++n;
                    }
                }
                sb.Append("\n");
            }
            sb.AppendLine($"Time created: <color={colorClanNamesOverview}>{UnixTimeStampToDateTime(Clan.created)}</color>");
            sb.AppendLine($"Last change: <color={colorClanNamesOverview}>{UnixTimeStampToDateTime(Clan.updated)}</color>");
            SendReply(player, sb.ToString().TrimEnd());
        }

        public Dictionary<ulong, double> ClanChatCD = new Dictionary<ulong, double>();

        void cmdChatClanchat(BasePlayer player, string command, string[] args)
        {
            if (player == null || args.Length == 0) return;
            var myClan = findClanByUser(player.UserIDString);
            if (myClan == null)
            {
                player.ChatMessage("Чтобы использовать клановый чат нужно в него вступить!");
                return;
            }
            if (clanChatDenyOnMuted)
            {
                var current = this.covalence.Players.FindPlayerById(player.UserIDString);
                var chk = Interface.CallHook("API_IsMuted", current);
                if (chk != null && chk is bool && (bool)chk)
                {
                    SendReply(player, string.Format(msg("clanchatmuted", player.UserIDString)));
                    return;
                }
            }
            var message = string.Join(" ", args);
            if (message.Length > 20)
            {
                player.ChatMessage("Сообщение клану должно быть не больше 20 символов");
                return;
            }
            if (ClanChatCD.ContainsKey(player.userID))
            {
                var left = ClanChatCD[player.userID] - CurrentTime();
                if (left <= 0)
                    ClanChatCD.Remove(player.userID);
                else
                {
                    player.ChatMessage($"Вы не можете отправить сообщение клану еще {TimeSpan.FromSeconds(left).ToShortString()} секунд!");
                    return;
                }
            }
            if (!ClanChatCD.ContainsKey(player.userID))
                ClanChatCD.Add(player.userID, CurrentTime() + 5);
            if (string.IsNullOrEmpty(message)) return;
            myClan.BroadcastChat(string.Format(msg("broadcastformat"), myClan.PlayerColor(player.UserIDString), player.net.connection.username, message));
            if (ConVar.Chat.serverlog) DebugEx.Log(string.Format("[CHAT] CLAN [{0}] - {1}: {2}", myClan.tag, player.net.connection.username, message), StackTraceLogType.None);
        }

        void cmdChatAllychat(BasePlayer player, string command, string[] args)
        {
            if (player == null || args.Length == 0) return;
            var myClan = findClanByUser(player.UserIDString);
            if (myClan == null)
            {
                player.ChatMessage("Чтобы использовать клановый чат нужно в него вступить!");
                return;
            }
            if (myClan.clanAlliances.Count == 0)
            {
                PrintChat(player, string.Format(msg("noactiveally", player.UserIDString)));
                return;
            }
            if (clanChatDenyOnMuted)
            {
                var current = this.covalence.Players.FindPlayerById(player.UserIDString);
                var chk = Interface.CallHook("API_IsMuted", current);
                if (chk != null && chk is bool && (bool)chk)
                {
                    SendReply(player, string.Format(msg("clanchatmuted", player.UserIDString)));
                    return;
                }
            }
            var message = string.Join(" ", args);
            if (string.IsNullOrEmpty(message)) return;
            if (message.Length > 20)
            {
                player.ChatMessage("Сообщение альянсу должно быть не больше 20 символов");
                return;
            }
            if (ClanChatCD.ContainsKey(player.userID))
            {
                var left = ClanChatCD[player.userID] - CurrentTime();
                if (left <= 0)
                    ClanChatCD.Remove(player.userID);
                else
                {
                    player.ChatMessage($"Вы не можете отправить сообщение альянсов еще {TimeSpan.FromSeconds(left).ToShortString()} секунд!");
                    return;
                }
            }
            if (!ClanChatCD.ContainsKey(player.userID))
                ClanChatCD.Add(player.userID, CurrentTime() + 5);
            foreach (var clanAllyName in myClan.clanAlliances)
            {
                var clanAlly = findClan(clanAllyName);
                if (clanAlly == null) continue;
                clanAlly.AllyBroadcastChat(string.Format(msg("allybroadcastformat"), myClan.tag, myClan.PlayerColor(player.UserIDString), player.net.connection.username, message));
            }
            myClan.AllyBroadcastChat(string.Format(msg("broadcastformat"), myClan.PlayerColor(player.UserIDString), player.net.connection.username, message));
            if (ConVar.Chat.serverlog) DebugEx.Log(string.Format("[CHAT] ALLY [{0}] - {1}: {2}", myClan.tag, player.net.connection.username, message), StackTraceLogType.None);
        }

        void cmdChatClanFF(BasePlayer player, string command, string[] args)
        {
            if (!enableFFOPtion || player == null) return;
            var myClan = findClanByUser(player.UserIDString);
            if (myClan == null)
            {
                player.ChatMessage("У вас <color=red>нет</color> клана");
                return;
            }
            if (manuallyEnabledBy.Contains(player.userID))
            {
                manuallyEnabledBy.Remove(player.userID);
                PrintChat(player, string.Format(msg("clanffdisabled", player.UserIDString), colorClanFFOff));
                return;
            }
            else
            {
                manuallyEnabledBy.Add(player.userID);
                PrintChat(player, string.Format(msg("clanffenabled", player.UserIDString), colorClanFFOn));
                return;
            }
        }
        public bool HasFFEnabled(ulong playerid = 5931008) => !enableFFOPtion ? false : !manuallyEnabledBy.Contains(playerid) ? false : true;
        public void ToggleFF(ulong playerId)
        {
            if (manuallyEnabledBy.Contains(playerId)) manuallyEnabledBy.Remove(playerId);
            else manuallyEnabledBy.Add(playerId);
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        class StoredData
        {
            public Dictionary<string, Clan> clans = new Dictionary<string, Clan>();
            public Int32 saveStamp = 0;
            public string lastStorage = string.Empty;
            public StoredData() { }
        }

        StoredData clanSaves = new StoredData();

        public class ChangeListed
        {
            public int Need { get; set; }
            public int Complete { get; set; }
        }

        public class MembersChangeList
        {
            public int Complete { get; set; }
        }

        public class PlayerStats
        {
            public int PlayerPoints = 0;
            public int Killed = 0;
            public int Death = 0;
            public int Suicide = 0;
            public int PlayTimeInServer = 0;
            public DateTime LastLogin = DateTime.Now;
            public bool CupAuth = true;
            public bool CodeAuth = true;
            public bool TurretAuth = true;

            public Dictionary<string, int> GatherInfo = new Dictionary<string, int>()
            {
                { "wood", 0 },
                { "stones", 0 },
                { "metal.ore", 0 },
                { "sulfur.ore", 0},
                { "hq.metal.ore", 0 },
                { "cloth", 0},
                {"leather", 0},
                {"fat.animal", 0},
                {"loot-barrel", 0}
            };
        }
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        public class Clan
        {
            public int ClanPoints = 0;
            public string Task;
            public int KillC = 0;
            public int DeathC = 0;

            public int Cupboard = 0;
            public int SamSite = 0;
            public int Turret = 0;
            public int Building = 0;

            public string tag;
            public string description;
            public string owner;
            public string ownerName;
            public string council;
            public int created;
            public int updated;

            [JsonIgnore, ProtoIgnore] public int online;
            [JsonIgnore, ProtoIgnore] public int total;
            [JsonIgnore, ProtoIgnore] public int mods;
            public List<string> moderators = new List<string>();
            public Dictionary<string, PlayerStats> members = new Dictionary<string, PlayerStats>();

            public Dictionary<string, int> invites = new Dictionary<string, int>();
            public List<string> clanAlliances = new List<string>();
            public List<string> invitedAllies = new List<string>();
            public List<string> pendingInvites = new List<string>();
            //TEAM

            [JsonIgnore]
            [ProtoIgnore]
            private string currentTeamLeader => owner;

            [JsonIgnore]
            [ProtoIgnore]
            private bool wasDisbanded = false;

            [JsonIgnore]
            [ProtoIgnore]
            private RelationshipManager.PlayerTeam _playerTeam;

            [JsonIgnore]
            [ProtoIgnore]
            public RelationshipManager.PlayerTeam PlayerTeam
            {
                get
                {
                    if (_playerTeam == null)
                        _playerTeam = RelationshipManager.ServerInstance.CreateTeam();

                    return _playerTeam;
                }
            }


            public void RemoveModerator(object obj)
            {
                string Id = GetObjectId(obj);
                moderators.Remove(Id);
            }

            private string GetObjectId(object obj)
            {
                if (obj is BasePlayer)
                    return (obj as BasePlayer).UserIDString;

                else if (obj is IPlayer)
                    return (obj as IPlayer).Id;

                return (string)obj;
            }

            internal void OnCreate()
            {
                OnUpdate();
                Interface.CallHook("OnClanCreate", tag);
            }

            public static int UnixTimeStampUTC()
            {
                return (int)DateTime.UtcNow.Subtract(Epoch).TotalSeconds;
            }

            internal void OnUpdate(bool hasChanges = true)
            {
                if (hasChanges)
                {
                    updated = UnixTimeStampUTC();
                    UpdateTeam();
                }

                UpdateOnline();


                Interface.CallHook("OnClanUpdate", tag);
            }

            private static DateTime Epoch = new DateTime(1970, 1, 1);

            public void DisbandTeam()
            {
                wasDisbanded = true;

                DestroyPlayerTeam();
            }

            internal void OnDestroy()
            {
                DestroyPlayerTeam();
                Interface.CallHook("OnClanDestroy", tag);
            }

            internal void OnUnload()
            {
                DestroyPlayerTeam();
            }

            internal void UpdateTeam()
            {
                if (!useRelationshipManager || wasDisbanded)
                    return;

                PlayerTeam.teamLeader = cc.disableManageFunctions || !cc.allowButtonKick ? 0UL : Convert.ToUInt64(owner);

                for (int i = 0; i < PlayerTeam.members.Count; i++)
                {
                    ulong playerId = PlayerTeam.members[i];

                    if (!members.ContainsKey(playerId.ToString()))
                    {
                        PlayerTeam.RemovePlayer(playerId);

                        (BasePlayer.FindByID(playerId) ?? BasePlayer.FindSleeping(playerId))?.ClearTeam();

                        RelationshipManager.ServerInstance.playerToTeam.Remove(playerId);

                    }
                }

                foreach (var member in members)
                {
                    ulong playerId = ulong.Parse(member.Key);

                    BasePlayer player = BasePlayer.FindByID(playerId) ?? BasePlayer.FindSleeping(playerId);

                    if (!PlayerTeam.members.Contains(playerId))
                    {
                        RelationshipManager.ServerInstance.playerToTeam.Remove(playerId);

                        if (player != null)
                        {
                            player.ClearTeam();
                            PlayerTeam.AddPlayer(player);
                        }
                    }
                }
                PlayerTeam.MarkDirty();
            }

            void UpdateOnline()
            {
                var onlineCount = members.Where(p => BasePlayer.Find(p.Key) != null && BasePlayer.Find(p.Key).IsConnected).Count();
                online = onlineCount;
            }

            private void DestroyPlayerTeam()
            {
                if (_playerTeam != null)
                {
                    for (int i = _playerTeam.members.Count - 1; i >= 0; i--)
                    {
                        _playerTeam.RemovePlayer(_playerTeam.members[i]);
                    }
                }

                _playerTeam = null;
            }

            public string GetColoredName(string Id, string Name)
            {
                if (IsOwner(Id))
                    return $"<color={cc.clanOwnerColor}>{Name}</color>";

                else if (IsCouncil(Id) && !IsOwner(Id))
                    return $"<color={cc.clanCouncilColor}>{Name}</color>";

                else if (IsModerator(Id) && !IsOwner(Id))
                    return $"<color={cc.clanModeratorColor}>{Name}</color>";

                else return $"<color={cc.clanMemberColor}>{Name}</color>";
            }

            //*TEAM

            public IPlayer FindClanMember(string nameOrId)
            {
                IPlayer player = cc.covalence.Players.FindPlayer(nameOrId);
                if (members.ContainsKey(player.Id))
                    return player;
                return null;
            }

            [JsonIgnore]
            [ProtoIgnore]
            public List<BasePlayer> membersBasePlayer = new List<BasePlayer>();

            public void AddBasePlayer(BasePlayer basePlayer, bool flag = false)
            {
                if (!membersBasePlayer.Any((BasePlayer x) => x.UserIDString == basePlayer.UserIDString))
                {
                    membersBasePlayer.Add(basePlayer);
                }
                else
                {
                    membersBasePlayer.RemoveAll((BasePlayer x) => x.UserIDString == basePlayer.UserIDString);
                    membersBasePlayer.Add(basePlayer);
                }
            }

            public void RemoveBasePlayer(BasePlayer basePlayer, bool flag = false)
            {
                if (membersBasePlayer.Any((BasePlayer x) => x.UserIDString == basePlayer.UserIDString))
                {
                    membersBasePlayer.RemoveAll((BasePlayer x) => x.UserIDString == basePlayer.UserIDString);
                }
            }

            public BasePlayer GetBasePlayer(string Id)
            {
                BasePlayer lookup = membersBasePlayer.Find((BasePlayer x) => x.UserIDString == Id);
                if (lookup)
                    return lookup;

                lookup = RustCore.FindPlayerByIdString(Id);

                if (lookup)
                    AddBasePlayer(lookup);

                return lookup;
            }

            public void SetModerator(object obj)
            {
                RemoveModerator(obj);
                string Id = GetObjectId(obj);
                moderators.Add(Id);
            }

            public Dictionary<string, Dictionary<string, ulong>> SkinList = new Dictionary<string, Dictionary<string, ulong>>();
            public Dictionary<string, ChangeListed> Change = new Dictionary<string, ChangeListed>()
            {
                ["wood"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["stones"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["metal.ore"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["sulfur.ore"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["hq.metal.ore"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["cloth"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["leather"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["fat.animal"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
                ["loot-barrel"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 0
                },
            };


            public PlayerStats GetPlayerStats(string playerid)
            {
                if (!members.ContainsKey(playerid)) return null;
                else
                    return members[playerid];
            }

            public static Clan Create(string tag, string description, string ownerId, string owName, string URL)
            {
                var clan = new Clan()
                {
                    ClanPoints = 0,
                    KillC = 0,
                    DeathC = 0,

                    Cupboard = 0,
                    SamSite = 0,
                    Turret = 0,
                    Building = 0,

                    Task = string.Empty,
                    tag = tag,
                    description = description,
                    owner = ownerId,
                    ownerName = owName,
                    created = cc.UnixTimeStampUTC(),
                    updated = cc.UnixTimeStampUTC(),
                    SkinList = new Dictionary<string, Dictionary<string, ulong>>()
                    {
                        ["wear"] = new Dictionary<string, ulong>()
                        {
                             {"hoodie", 0 },
                             {"pants", 0 },
                             {"shoes.boots", 0 },
                             {"metal.facemask", 0},
                             {"metal.plate.torso", 0 },
                             {"roadsign.kilt", 0 },
                             {"rifle.ak", 0 },
                             {"lmg.m249", 0 },
                             {"rifle.l96", 0 },
                             {"coffeecan.helmet", 0 },
                             {"roadsign.jacket", 0 },
                             {"smg.mp5", 0 },
                             {"rifle.m39", 0 },
                             {"smg.thompson", 0 },
                             {"rifle.semiauto", 0 },
                             {"smg.2", 0 },
                             {"pistol.semiauto", 0 },
                             {"pistol.python", 0 },
                             {"rocket.launcher", 0 },
                             {"door.double.hinged.toptier", 1882005499 },
                             {"door.hinged.toptier", 813882180 },
                        },
                    },
                    Change = new Dictionary<string, ChangeListed>()
                    {
                        ["wood"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["stones"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["metal.ore"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["sulfur.ore"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["hq.metal.ore"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["cloth"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["leather"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["fat.animal"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                        ["loot-barrel"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 0
                        },
                    }
                };
                clan.members.Add(ownerId, new PlayerStats());
                return clan;
            }
            public bool IsOwner(string userId)
            {
                return userId == owner;
            }
            public bool IsCouncil(string userId)
            {
                return userId == council;
            }
            public bool IsModerator(string userId)
            {
                return moderators.Contains(userId);
            }
            public bool IsMember(string userId)
            {
                return members.ContainsKey(userId);
            }
            public bool IsInvited(string userId)
            {
                return invites.ContainsKey(userId);
            }
            public void BroadcastChat(string message)
            {
                foreach (var memberId in members)
                {
                    var player = BasePlayer.Find(memberId.Key);
                    if (player == null) continue;
                    player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefix) + $"<color={cc.broadcastMessageColor}>{message}</color>");
                }
            }
            public void BroadcastLoc(string messagetype, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "", string current = "")
            {
                string message = string.Empty;
                foreach (var memberId in members)
                {
                    var player = BasePlayer.Find(memberId.Key);
                    if (player == null || player.UserIDString == current) continue;
                    message = string.Format(cc.msg(messagetype, memberId.Key), arg1, arg2, arg3, arg4);
                    player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefix) + $"<color={cc.broadcastMessageColor}>{message}</color>");
                }
            }
            public void AllyBroadcastChat(string message)
            {
                foreach (var memberId in members)
                {
                    var player = BasePlayer.Find(memberId.Key);
                    if (player == null) continue;
                    player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefixAlly) + $"<color={cc.broadcastMessageColor}>{message}</color>");
                }
            }
            public void AllyBroadcastLoc(string messagetype, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "")
            {
                string message = string.Empty;
                foreach (var memberId in members)
                {
                    var player = BasePlayer.Find(memberId.Key);
                    if (player == null) continue;
                    message = string.Format(cc.msg(messagetype, memberId.Key), arg1, arg2, arg3, arg4);
                    player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefixAlly) + $"<color={cc.broadcastMessageColor}>{message}</color>");
                }
            }
            public string ColNam(string Id, string Name)
            {
                if (IsOwner(Id)) return $"<color={cc.clanOwnerColor}>{Name}</color>";
                else if (IsCouncil(Id) && !IsOwner(Id)) return $"<color={cc.clanCouncilColor}>{Name}</color>";
                else if (IsModerator(Id) && !IsOwner(Id)) return $"<color={cc.clanModeratorColor}>{Name}</color>";
                else return $"<color={cc.clanMemberColor}>{Name}</color>";
            }
            public string PlayerLevel(string userID)
            {
                if (IsOwner(userID)) return "Owner";
                if (IsCouncil(userID)) return "Council";
                if (IsModerator(userID)) return "Moderator";
                return "Member";
            }
            public string PlayerColor(string userID)
            {
                if (IsOwner(userID)) return cc.clanOwnerColor;
                if (IsCouncil(userID)) return cc.clanCouncilColor;
                if (IsModerator(userID)) return cc.clanModeratorColor;
                return cc.clanMemberColor;
            }
            public IPlayer GetIPlayer(string partialName)
            {
                ulong userID;
                IPlayer iplayer;
                if (partialName.Length == 17 && ulong.TryParse(partialName, out userID))
                {
                    iplayer = cc.covalence.Players.FindPlayer(partialName);
                    return iplayer;
                }
                if (invites.Count > 0) foreach (var imember in GetInvites())
                    {
                        if (imember.Name.Contains(partialName) || imember.Name.EndsWith(partialName))
                        {
                            iplayer = cc.covalence.Players.FindPlayerById(imember.Id);
                            return iplayer;
                        }
                    }
                var player = cc.rust.FindPlayerByName(partialName);
                if (player != null) return cc.covalence.Players.FindPlayerById(player.UserIDString);
                try
                {
                    var iply = cc.covalence.Players.FindPlayer(partialName);
                    if (iply is IPlayer) return iply;
                }
                catch
                {
                    var idplayer = cc.covalence.Players.FindPlayer(partialName);
                    if (idplayer != null) return idplayer;
                }
                return null;
            }
            public IPlayer GetIMember(string partialName)
            {
                ulong userID;
                IPlayer player = null;
                if (partialName.Length == 17 && ulong.TryParse(partialName, out userID))
                {
                    player = cc.covalence.Players.FindPlayer(partialName);
                    return player;
                }
                foreach (var imember in GetIMembers())
                {
                    if (imember.Name.Contains(partialName) || imember.Name.EndsWith(partialName))
                    {
                        player = cc.covalence.Players.FindPlayerById(imember.Id);
                        return player;
                    }
                }
                player = GetIPlayer(partialName);
                return player;
            }
            public List<IPlayer> GetIMembers()
            {
                List<IPlayer> export = new List<IPlayer>();
                foreach (var member in members)
                {
                    if (IsOwner(member.Key)) continue;
                    IPlayer player = cc.covalence.Players.FindPlayerById(member.Key);
                    if (player != null) export.Add(player);
                }
                return export;
            }
            public List<IPlayer> GetInvites()
            {
                List<IPlayer> export = new List<IPlayer>();
                foreach (var invited in invites)
                {
                    IPlayer player = cc.covalence.Players.FindPlayerById(invited.Key);
                    if (player != null) export.Add(player);
                }
                return export;
            }
            internal JObject ToJObject()
            {
                var obj = new JObject();
                obj["tag"] = tag;
                obj["description"] = description;
                obj["owner"] = owner;
                obj["council"] = council;
                var jmoderators = new JArray();
                foreach (var moderator in moderators) jmoderators.Add(moderator);
                obj["moderators"] = jmoderators;
                var jmembers = new JArray();
                foreach (var member in members) jmembers.Add(member.Key);
                obj["members"] = jmembers;
                var jallies = new JArray();
                foreach (var ally in clanAlliances) jallies.Add(ally);
                obj["allies"] = jallies;
                var jinvallies = new JArray();
                foreach (var ally in invitedAllies) jinvallies.Add(ally);
                obj["invitedallies"] = jinvallies;
                return obj;
            }
        }

        [HookMethod("AddClanPoints")]
        private void AddClanPoints(string tag, int amount)
        {
            if (tag == null || tag == "") return;
            var clan = findClan(tag);
            if (clan == null) return;
            clan.ClanPoints += amount;
        }
        string GetClanTag(ulong playerID)
        {
            var clan = findClanByUser(playerID.ToString());
            if (clan == null) return String.Empty;
            return clan.tag;
        }
        private bool GetClanOwner(ulong playerID)
        {
            var clan = findClanByUser(playerID.ToString());
            if (clan == null) return false;
            if (clan.IsOwner(playerID.ToString()))
                return true;
            return false;
        }
		private bool IsTeammates(ulong player, ulong friend)
		{
			return player == friend ||
			       RelationshipManager.ServerInstance.FindPlayersTeam(player)?.members?.Contains(friend) == true ||
			       findClanByUser(player.ToString())?.IsMember(friend.ToString()) == true;
		}
        private void GetPointRaid(string nameClan, ulong playerId)
        {
            var clan = findClan(nameClan);
            if (clan == null) return;

            var totalPoint = clan.ClanPoints / 2;
            if (totalPoint <= 0) return;
            
            var clanKill = findClanByUser(playerId.ToString());
            if (clanKill == null) return;

            clanKill.ClanPoints += totalPoint;
        }
        private void GetChangeSkin(ulong playerID, string shortName, ulong skinID)
        {
            var clan = findClanByUser(playerID.ToString());
            if (clan == null) return;
            var value = clan.SkinList.FirstOrDefault(p => p.Value.ContainsKey(shortName)).Key;
            if (value == null) return;
            clan.SkinList[value][shortName] = skinID;
        }
        private void GiveClanPoints(string tag, int amount)
        {
            if (tag == null || tag == "") return;
            var clan = findClan(tag);
            if (clan == null) return;
            clan.ClanPoints += amount;
        }
        private void RemClanPoints(string tag, int amount)
        {
            if (tag == null || tag == "") return;
            var clan = findClan(tag);
            if (clan == null) return;
            clan.ClanPoints -= amount;
        }
        [HookMethod("GetClan")]
        private JObject GetClan(string tag)
        {
            if (tag == null || tag == "") return null;
            var clan = findClan(tag);
            if (clan == null) return null;
            return clan.ToJObject();
        }
        [HookMethod("GetAllClans")]
        private JArray GetAllClans()
        {
            return new JArray(clans.Keys);
        }
        [HookMethod("GetClanOf")]
        private string GetClanOf(ulong player)
        {
            if (player == 0uL) return null;
            var clan = findClanByUser(player.ToString());
            if (clan == null) return null;
            return clan.tag;
        }
        [HookMethod("GetClanOf")]
        private string GetClanOf(string player)
        {
            if (player == null || player == "") return null;
            var clan = findClanByUser(player.ToString());
            if (clan == null) return null;
            return clan.tag;
        }
        [HookMethod("GetClanOf")]
        private string GetClanOf(BasePlayer player)
        {
            if (player == null) return null;
            var clan = findClanByUser(player.UserIDString);
            if (clan == null) return null;
            return clan.tag;
        }     
        [HookMethod("GetClanMembers")]
        private List<string> GetClanMembers(ulong PlayerID)
        {
            List<string> Players = new List<string>();
            var myClan = findClanByUser(PlayerID.ToString());
            if (myClan == null) return null;
            foreach (var it in myClan.members)
                Players.Add(it.Key);
            return Players;
        }

        private List<string> GetMembersClan(string tag) 
        {
            List<string> _List = new List<string>();
            var clan = findClan(tag);
            if (clan == null) return _List;
            clan.members.Keys.ToList().ForEach(key => _List.Add(key.ToString()));
            return _List;
        }

        private List<ulong> BTeleportMenuHook(ulong playerID)
        {
            List<ulong> playerList = new List<ulong>();
            var myClan = findClanByUser(playerID.ToString());
            if (myClan == null) return null;
            foreach (var members in myClan.members)
            {
                var onlineMembers = BasePlayer.FindByID(ulong.Parse(members.Key));
                if (onlineMembers == null) continue;
                if (playerID.ToString() == members.Key) continue;

                playerList.Add(ulong.Parse(members.Key));
            }
            return playerList;
        }
        
        private List<ulong> BoxIds = new List<ulong>();
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (entity?.net?.ID == null) return; if (player == null) return; if (BoxIds.Contains(entity.net.ID.Value)) return;
            BoxIds.Add(entity.net.ID.Value);
            var clan = findClanByUser(player.UserIDString);
            if (clan == null) return;      
            var cont = entity.GetComponent<StorageContainer>();  
            if (entity.PrefabName.Contains("assets/bundled/prefabs/radtown/crate_normal_2.prefab"))
            {    
                clan.members[player.UserIDString].GatherInfo["loot-barrel"] = clan.members[player.UserIDString].GatherInfo["loot-barrel"] + 1;           
                clan.members[player.UserIDString].PlayerPoints += 1;
                clan.Change["loot-barrel"].Complete = clan.Change["loot-barrel"].Complete + 1;
                clan.ClanPoints += 1;
            }
        }

        private void AutOnBuildingPrivilage<T>(BasePlayer player, Clan clan, Action<T, string> callback)
        {
            List<string> clanMembers = clan.members.Keys.ToList();
            if (clanMembers == null)
                return;
            var playerEntities = GetPlayerEnitityByType<T>(player);
            if (playerEntities == null) return;
            foreach (var clanMember in clanMembers)
            {
                if (clanMember == player.UserIDString) continue;
                foreach (var entity in playerEntities)
                {
                    callback(entity, clanMember);
                }
            }
        }

        private static List<T> GetPlayerEnitityByType<T>(BasePlayer player)
        {
            var entities = UnityEngine.Object.FindObjectsOfType(typeof(T));
            var playerEntities = new List<T>();

            foreach (object entity in entities)
            {
                if (!(entity is BaseEntity)) continue;

                if ((entity as BaseEntity).OwnerID == player.userID)
                {
                    playerEntities.Add((T)entity);
                }
            }

            return playerEntities;
        }

        #region [Auth]
        object OnTurretTarget(AutoTurret turret, BasePlayer player)
        {
            if (player == null || turret == null || turret.OwnerID == 0 || turret.OwnerID == player.userID || turret.IsAuthed(player)) return null;
            var inClan = HasFriend(turret.OwnerID, player.userID);
            if (inClan != null && (bool)inClan)
            {
                bool check = findClanByUser(turret.OwnerID.ToString()).GetPlayerStats(turret.OwnerID.ToString()).TurretAuth;
                if (check) return false;
            }
            return null;
        }

        private object OnSamSiteTarget(SamSite samSite, BaseVehicle vehicle)
        {
            if (samSite == null || !samSite.OwnerID.IsSteamId() || vehicle == null)
                return null;

            var clan = findClanByUser(samSite.OwnerID.ToString());
            if (clan == null) return null;

            if (vehicle.OwnerID == samSite.OwnerID || clan.IsMember(vehicle.OwnerID.ToString()))
                return true;

            foreach (var mounted in vehicle.allMountPoints
                         .Where(allMountPoint => allMountPoint != null && allMountPoint.mountable != null)
                         .Select(allMountPoint => allMountPoint.mountable.GetMounted())
                         .Where(mounted => mounted != null))
            {
                if (mounted.userID == samSite.OwnerID || clan.IsMember(mounted.UserIDString))
                    return true;
            }

            return null;
        }

        private void OnEntitySpawned(BuildingPrivlidge buildingPrivlidge)
        {
            if (buildingPrivlidge == null || !buildingPrivlidge.OwnerID.IsSteamId()) return;

            var clan = findClanByUser(buildingPrivlidge.OwnerID.ToString());
            if (clan == null) return;

            foreach (var member in clan.members)
            {
                IPlayer player = covalence.Players.FindPlayerById(member.Key);
                if (player == null) continue;

                buildingPrivlidge.authorizedPlayers.Add(new PlayerNameID
                {
                    userid = ulong.Parse(player.Id),
                    username = player.Name
                });
            }
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (player == null || baseLock == null || !baseLock.IsLocked()) return null;
            var parentEntity = baseLock.GetParentEntity();
            var ownerID = baseLock.OwnerID.IsSteamId() ? baseLock.OwnerID : parentEntity != null ? parentEntity.OwnerID : 0;
            if (!ownerID.IsSteamId() || ownerID == player.userID) return null;

            var clan = findClanByUser(ownerID.ToString());
            if (clan == null) return null;

            if (clan.IsMember(player.UserIDString))
                return true;


            return null;
        }
        #endregion

        private bool CheckClans(ulong TurretID, ulong TargetID)
        {
            var result = HasFriend(TurretID, TargetID);
            return result == null ? false : (bool)result;
        }

        string ClanAlready(ulong ownerid)
        {
            var clan = findClanByUser(ownerid.ToString());
            if (clan == null)
            {
                return "404";
            }
            return clan.tag;
        }

        void ScoreRemove(string clanName, ulong acceptclan)
        {
            var clan = findClan(clanName);
            if (clan == null)
            {
                return;
            }
            if (clan.ClanPoints < 0) return;
            var clanremovescore = clan.ClanPoints / 2;
            if (clanremovescore <= 0)
            {
                clan.ClanPoints = 0;
                return;
            }
            clan.ClanPoints = 0;
            var clanaccept = findClanByUser(acceptclan.ToString());
            if (clanaccept == null)
            {
                return;
            }
            clanaccept.ClanPoints += clanremovescore;
        }
        private void ScoreV2(string victim, string attacker)
        {
            if (victim == null || victim == "") return;
            var clan = findClan(victim);
            if (clan == null) return;
            if (attacker == null || attacker == "") return;
            var clans = findClan(attacker);
            if (clan.ClanPoints < 0) return;
            var clanremovescore = clan.ClanPoints / 2;
            if (clanremovescore <= 0)
            {
                clan.ClanPoints = 0;
                return;
            }
            clan.ClanPoints = 0;
            clans.ClanPoints += clanremovescore;
        }
        object ClanCount(ulong owner)
        {
            var clan = findClanByUser(owner.ToString());
            if (clan == null)
            {
                return false;
            }
            if (clan.IsOwner(owner.ToString()) == false)
            {
                return false;
            }
            return clan.members.Count;
        }
        object ClanPoint(ulong PlayerUserID)
        {
            var clan = findClanByUser(PlayerUserID.ToString());
            if (clan == null)
            {
                return false;
            }
            return clan.ClanPoints;
        }
        object ClanOwner(ulong PlayerUserID)
        {
            var clan = findClanByUser(PlayerUserID.ToString());
            if (clan == null)
            {
                return false;
            }
            return clan.owner;
        }
        object ClanOwner2(string tag)
        {
            var clan = findClan(tag);
            if (clan == null)
            {
                return false;
            }
            string Owner = Convert.ToString(clan.owner);
            return Owner;
        }
        private int? GetClanPoints(ulong playerID)
        {
            var clan = findClanByUser(playerID.ToString());
            if (clan == null) return null;
            return clan.ClanPoints;
        }
        private Dictionary<string, int> GetTops()
        {
            Dictionary<string, int> clansList = new Dictionary<string, int>();
            var Clans = from pair in clans orderby pair.Value.ClanPoints descending select pair;
            foreach (KeyValuePair<string, Clan> key in Clans.Take(5))
                clansList.Add(key.Value.tag, key.Value.ClanPoints);

            return clansList;
        }

        private ulong GetOwnerId(string tag)
        {
            var clan = findClan(tag);
            if (clan == null) return 0;

            return ulong.Parse(clan.owner);
        }
        [HookMethod("IsClanMember")]
        private object IsClanMember(string userID, string targetID)
        {
            var clanOwner = findClanByUser(userID);
            if (clanOwner == null) return null;
            var clanFriend = findClanByUser(targetID);
            if (clanFriend == null) return null;
            if (clanOwner.tag == clanFriend.tag) return true;
            return null;
        }

        [HookMethod("HasFriend")]
        private object HasFriend(ulong entOwnerID, ulong PlayerUserID)
        {
            var clanOwner = findClanByUser(entOwnerID.ToString());
            if (clanOwner == null) return null;
            var clanFriend = findClanByUser(PlayerUserID.ToString());
            if (clanFriend == null) return null;
            if (clanOwner.tag == clanFriend.tag) return true;
            return false;
        }
        [HookMethod("IsModerator")]
        private object IsModerator(ulong PlayerUserID)
        {
            var clan = findClanByUser(PlayerUserID.ToString());
            if (clan == null) return null;
            if ((setHomeOwner && clan.IsOwner(PlayerUserID.ToString())) || (setHomeModerator && (clan.IsModerator(PlayerUserID.ToString()) || clan.IsCouncil(PlayerUserID.ToString()))) || setHomeMember) return true;
            return false;
        }
        private Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds ? UnixEpoch.AddMilliseconds(unixTimeStamp) : UnixEpoch.AddSeconds(unixTimeStamp);
        }
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
        void PrintChat(BasePlayer player, string message)
        {
            SendReply(player, $"<color={colorTextMsg}>" + message + "</color>");
        }
        [ConsoleCommand("clans")]
        void cclans(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.Connection != null && arg.Connection.player != null && arg.Connection.authLevel >= 1)
            {
                var sb = new StringBuilder();
                sb.AppendLine("clans.list (Lists all clans, their owners and their membercount)");
                sb.AppendLine("clans.listex (Lists all clans, their owners/members and their onlinestatus)");
                sb.AppendLine("clans.show TAG (lists the choosen clan and the members with status)");
                sb.AppendLine("clans.msg TAG message without quotes (Sends a clan message)");
                if (arg.Connection.authLevel >= authLevelRename) sb.AppendLine("clans.rename OLDTAG NEWTAG (rename's a clan)");
                if (arg.Connection.authLevel >= authLevelDelete) sb.AppendLine("clans.delete TAG (delete's a clan)");
                if (arg.Connection.authLevel >= authLevelInvite) sb.AppendLine("clans.playerinvite TAG playername (sends clan invitation to a player)");
                if (arg.Connection.authLevel >= authLevelKick) sb.AppendLine("clans.playerkick TAG playername (kicks a player from a clan)");
                if (arg.Connection.authLevel >= authLevelPromoteDemote)
                {
                    sb.AppendLine("clans.playerpromote TAG playername (promotes a player)");
                    sb.AppendLine("clans.playerdemote TAG playername (demotes a player)");
                }
                SendReply(arg, sb.ToString());
            }
        }
        [ConsoleCommand("clans.cmds")]
        void cclansCommands(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            var sb = new StringBuilder();
            sb.AppendLine("\n>> Clans command overview <<\n");
            sb.AppendLine("clans.list".PadRight(20) + "| Lists all clans, their owners and their membercount");
            sb.AppendLine("clans.listex".PadRight(20) + "| Lists all clans, their owners/members and their onlinestatus");
            sb.AppendLine("clans.show".PadRight(20) + "| lists the choosen clan and the members with status");
            sb.AppendLine("clans.showduplicates".PadRight(20) + "| lists the players which do exist in more than one clan");
            sb.AppendLine("clans.msg".PadRight(20) + "| message without quotes (Sends a clan message)");
            sb.AppendLine("clans.rename".PadRight(20) + "| rename's a clan");
            sb.AppendLine("clans.delete".PadRight(20) + "| delete's a clan");
            sb.AppendLine("clans.changeowner".PadRight(20) + "| changes the owner to another member");
            sb.AppendLine("clans.playerinvite".PadRight(20) + "| sends clan invitation to a player");
            sb.AppendLine("clans.playerjoin".PadRight(20) + "| joins a player into a clan");
            sb.AppendLine("clans.playerkick".PadRight(20) + "| kicks a player from a clan");
            sb.AppendLine("clans.playerpromote".PadRight(20) + "| promotes a player");
            sb.AppendLine("clans.playerdemote".PadRight(20) + "| demotes a player");
            SendReply(arg, sb.ToString());
        }
        [ConsoleCommand("clans.list")]
        void cclansList(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            TextTable textTable = new TextTable();
            textTable.AddColumn("Tag");
            textTable.AddColumn("Owner");
            textTable.AddColumn("SteamID");
            textTable.AddColumn("Count");
            textTable.AddColumn("On");
            foreach (var iclan in clans)
            {
                Clan clan = clans[iclan.Key];
                var owner = this.covalence.Players.FindPlayerById(clan.owner);
                if (owner == null) continue;
                textTable.AddRow(new string[]
                {
                 clan.tag, owner.Name, clan.owner, clan.total.ToString(), clan.online.ToString()
                });
            }
            SendReply(arg, "\n>> Current clans <<\n" + textTable.ToString());
        }
        [ConsoleCommand("clans.showduplicates")]
        void cclansDuplicates(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            TextTable textTable = new TextTable();
            textTable.AddColumn("SteamID");
            textTable.AddColumn("Memberships");
            textTable.AddColumn("PlayerName");
            Dictionary<string, List<string>> clanDuplicates = new Dictionary<string, List<string>>();
            foreach (var iclan in clans)
            {
                Clan clan = clans[iclan.Key];
                foreach (var member in clan.members.ToList())
                {
                    if (!clanDuplicates.ContainsKey(member.Key))
                    {
                        clanDuplicates.Add(member.Key, new List<string>());
                        clanDuplicates[member.Key].Add(clan.tag);
                        continue;
                    }
                    else clanDuplicates[member.Key].Add(clan.tag);
                }
            }
            foreach (var clDup in clanDuplicates)
            {
                if (clDup.Value.Count < 2) continue;
                var player = this.covalence.Players.FindPlayerById(clDup.Key);
                if (player == null) continue;
                textTable.AddRow(new string[] {
    clDup.Key, string.Join(" | ", clDup.Value.ToArray()), player.Name
}
                );
            }
            SendReply(arg, "\n>> Current found duplicates <<\n" + textTable.ToString());
        }
        [ConsoleCommand("clans.listex")]
        void cclansListEx(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            TextTable textTable = new TextTable();
            textTable.AddColumn("Tag");
            textTable.AddColumn("Level");
            textTable.AddColumn("Name");
            textTable.AddColumn("SteamID");
            textTable.AddColumn("Status");
            foreach (var iclan in clans)
            {
                Clan clan = clans[iclan.Key];
                foreach (var memberid in clan.members)
                {
                    var member = this.covalence.Players.FindPlayerById(memberid.Key);
                    if (member == null) continue;
                    textTable.AddRow(new string[] {
        clan.tag, clan.PlayerLevel(member.Id), member.Name, member.Id.ToString(), (member.IsConnected ? "Online": "Offline").ToString()
    }
                    );
                }
                textTable.AddRow(new string[] { }
                );
            }
            SendReply(arg, "\n>> Current clans with members <<\n" + textTable.ToString());
        }
        [ConsoleCommand("clans.show")]
        void cclansShow(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "Usage: clans.show TAG");
                return;
            }
            Clan clan;
            if (!TryGetClan(arg.Args[0], out clan))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine($"\n>> Show clan [{clan.tag}] <<");
            sb.AppendLine($"Description: {clan.description}");
            sb.AppendLine($"Time created: {UnixTimeStampToDateTime(clan.created)}");
            sb.AppendLine($"Last updated: {UnixTimeStampToDateTime(clan.updated)}");
            sb.AppendLine($"Member count: {clan.total}");
            TextTable textTable = new TextTable();
            textTable.AddColumn("Level");
            textTable.AddColumn("Name");
            textTable.AddColumn("SteamID");
            textTable.AddColumn("Status");
            sb.AppendLine();
            foreach (var memberid in clan.members)
            {
                var member = this.covalence.Players.FindPlayerById(memberid.Key);
                if (member == null) continue;
                textTable.AddRow(new string[] {
    clan.PlayerLevel(member.Id), member.Name, member.Id.ToString(), (member.IsConnected ? "Online": "Offline").ToString()
}
                );
            }
            sb.AppendLine(textTable.ToString());
            SendReply(arg, sb.ToString());
        }
        [ConsoleCommand("clans.msg")]
        void cclansBroadcast(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.msg TAG your message without quotes");
                return;
            }
            Clan clan;
            if (!TryGetClan(arg.Args[0], out clan))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            string BroadcastBy = consoleName;
            if (arg.Connection != null)
            {
                if (arg.Connection.authLevel == 2) BroadcastBy = "(Admin) " + arg.Connection.username;
                else BroadcastBy = "(Mod) " + arg.Connection.username;
            }
            string Msg = "";
            for (int i = 1;
            i < arg.Args.Length;
            i++) Msg = Msg + " " + arg.Args[i];
            clan.BroadcastChat($"<color={clanServerColor}>{BroadcastBy}</color>: {Msg}");
            SendReply(arg, $"Broadcast to [{clan.tag}]: {Msg}");
        }
        [ConsoleCommand("clans.rename")]
        void cclansRename(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelRename) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.rename OldTag NewTag");
                return;
            }
            Clan clan;
            if (!TryGetClan(arg.Args[0], out clan))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            if (tagReExt.IsMatch(arg.Args[1]))
            {
                SendReply(arg, string.Format(msg("hintchars"), allowedSpecialChars));
                return;
            }
            if (arg.Args[1].Length < tagLengthMin || arg.Args[1].Length > tagLengthMax)
            {
                SendReply(arg, string.Format(msg("hintlength"), tagLengthMin, tagLengthMax));
                return;
            }
            if (clans.ContainsKey(arg.Args[1]))
            {
                SendReply(arg, string.Format(msg("tagblocked")));
                return;
            }
            string oldtag = clan.tag;
            clan.tag = arg.Args[1];
            clan.online = 0;
            clans.Add(clan.tag, clan);
            RemoveClan(oldtag);
            setupPlayers(clan.members.Keys.ToList(), false, clan.tag);
            string oldGroup = permGroupPrefix + oldtag;
            string newGroup = permGroupPrefix + clan.tag;
            if (permission.GroupExists(oldGroup))
            {
                foreach (var member in clan.members) if (permission.UserHasGroup(member.Key, oldGroup)) permission.RemoveUserGroup(member.Key, oldGroup);
                permission.RemoveGroup(oldGroup);
            }
            if (usePermGroups && !permission.GroupExists(newGroup)) permission.CreateGroup(newGroup, "Clan " + clan.tag, 0);
            foreach (var member in clan.members) if (usePermGroups && !permission.UserHasGroup(member.Key, newGroup)) permission.AddUserGroup(member.Key, newGroup);
            string RenamedBy = consoleName;
            if (arg.Connection != null) RenamedBy = arg.Connection.username;
            foreach (var ally in clans)
            {
                Clan allyClan = clans[ally.Key];
                if (allyClan.clanAlliances.Contains(oldtag))
                {
                    allyClan.clanAlliances.Remove(oldtag);
                    allyClan.clanAlliances.Add(clan.tag);
                }
                if (allyClan.invitedAllies.Contains(oldtag))
                {
                    allyClan.invitedAllies.Remove(oldtag);
                    allyClan.invitedAllies.Add(clan.tag);
                }
                if (allyClan.pendingInvites.Contains(oldtag))
                {
                    allyClan.pendingInvites.Remove(oldtag);
                    allyClan.pendingInvites.Add(clan.tag);
                }
            }
            clan.BroadcastLoc("clanrenamed", $"<color={clanServerColor}>{RenamedBy}</color>", clan.tag);
            SendReply(arg, string.Format(msg("yourenamed"), oldtag, clan.tag));
            clan.OnUpdate();
        }
        [ConsoleCommand("clans.playerinvite")]
        void cclansPlayerInvite(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelInvite) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.playerinvite TAG playername/id");
                return;
            }
            Clan myClan;
            Clan check;
            if (!TryGetClan(arg.Args[0], out check))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            else myClan = (Clan)check;
            var invPlayer = myClan.GetIPlayer(arg.Args[1]);
            if (invPlayer == null)
            {
                SendReply(arg, string.Format(msg("nosuchplayer"), arg.Args[1]));
                return;
            }
            if (myClan.members.ContainsKey(invPlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadymember"), invPlayer.Name));
                return;
            }
            if (myClan.invites.ContainsKey(invPlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadyinvited"), invPlayer.Name));
                return;
            }
            if (findClanByUser(invPlayer.Id) != null)
            {
                SendReply(arg, string.Format(msg("alreadyinclan"), invPlayer.Name));
                return;
            }
            myClan.invites.Add(invPlayer.Id, UnixTimeStampUTC());
            if (!pendingPlayerInvites.ContainsKey(invPlayer.Id))
                pendingPlayerInvites.Add(invPlayer.Id, new List<string>());
            pendingPlayerInvites[invPlayer.Id].Add(myClan.tag);
            if (invPlayer.IsConnected)
            {
                var invited = rust.FindPlayerByIdString(invPlayer.Id);
                if (invited != null) PrintChat(invited, string.Format(msg("claninvite", invPlayer.Id), myClan.tag, myClan.description, colorCmdUsage));
            }
            myClan.updated = UnixTimeStampUTC();
            SendReply(arg, $"Invitation for clan '{myClan.tag}' sent to '{invPlayer.Name}'");
        }
        [ConsoleCommand("clans.playerjoin")]
        void cclansPlayerJoin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelInvite) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.playerjoin TAG playername/id");
                return;
            }
            Clan myClan;
            Clan check;
            if (!TryGetClan(arg.Args[0], out check))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            else myClan = (Clan)check;
            var joinPlayer = myClan.GetIPlayer(arg.Args[1]);
            if (joinPlayer == null)
            {
                SendReply(arg, string.Format(msg("nosuchplayer"), arg.Args[1]));
                return;
            }
            if (myClan.members.ContainsKey(joinPlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadymember"), joinPlayer.Name));
                return;
            }
            if (findClanByUser(joinPlayer.Id) != null)
            {
                SendReply(arg, string.Format(msg("alreadyinclan"), joinPlayer.Name));
                return;
            }
            myClan.invites.Remove(joinPlayer.Id);
            pendingPlayerInvites.Remove(joinPlayer.Id);
            myClan.members.Add(joinPlayer.Id, new PlayerStats());

            clanCache[joinPlayer.Id] = myClan;
            if (joinPlayer.IsConnected)
            {
                var joined = rust.FindPlayerByIdString(joinPlayer.Id);
                SetupPlayer(joined, joinPlayer, false, myClan);
            }
            if (usePermGroups && !permission.UserHasGroup(joinPlayer.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(joinPlayer.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("playerjoined", myClan.ColNam(joinPlayer.Id, joinPlayer.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.total++;

            myClan.OnUpdate();
            List<string> others = new List<string>(myClan.members.Keys.ToList());
            others.Remove(joinPlayer.Id);
            Interface.Oxide.CallHook("OnClanMemberJoined", joinPlayer.Id, others);
            SendReply(arg, $"Playerjoin into clan '{myClan.tag}' done for '{joinPlayer.Name}'");
        }
        [ConsoleCommand("clans.playerkick")]
        void cclansPlayerKick(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelKick) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.playerkick TAG playername/id");
                return;
            }
            Clan myClan;
            Clan check;
            if (!TryGetClan(arg.Args[0], out check))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            else myClan = (Clan)check;
            var kickPlayer = myClan.GetIMember(arg.Args[1]);
            if (kickPlayer == null)
            {
                SendReply(arg, string.Format(msg("nosuchplayer"), arg.Args[1]));
                return;
            }
            if (!myClan.IsMember(kickPlayer.Id) && !myClan.IsInvited(kickPlayer.Id))
            {
                SendReply(arg, string.Format(msg("notmembercannotkicked"), kickPlayer.Name));
                return;
            }
            if (myClan.members.Count() == 1)
            {
                SendReply(arg, "The clan has only one member. You need to delete the clan");
                return;
            }
            if (myClan.members.ContainsKey(kickPlayer.Id)) myClan.total--;
            myClan.invites.Remove(kickPlayer.Id);
            if (myClan.IsCouncil(kickPlayer.Id)) myClan.council = null;
            myClan.moderators.Remove(kickPlayer.Id);
            myClan.members.Remove(kickPlayer.Id);
            myClan.invites.Remove(kickPlayer.Id);
            bool ownerChanged = false;
            if (myClan.IsOwner(kickPlayer.Id) && myClan.members.Count() > 0)
            {
                myClan.owner = myClan.members.ToList()[0].Key;
                ownerChanged = true;
            }
            if (pendingPlayerInvites.ContainsKey(kickPlayer.Id)) pendingPlayerInvites[kickPlayer.Id].Remove(myClan.tag);
            clanCache.Remove(kickPlayer.Id);
            var kickBasePlayer = rust.FindPlayerByIdString(kickPlayer.Id);
            if (kickBasePlayer != null)
            {
                SetupPlayer(kickBasePlayer, kickPlayer, true, oldTag: myClan.tag);
            }
            if (usePermGroups && permission.UserHasGroup(kickPlayer.Id, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup(kickPlayer.Id, permGroupPrefix + myClan.tag);
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
            //Interface.Oxide.CallHook("OnClanMemberGone", kickPlayer.Id, myClan.members);
            Interface.CallHook("OnClanMemberGone", UInt64.Parse(kickPlayer.Id), myClan.tag);
            SendReply(arg, $"Player '{kickPlayer.Name}' was kicked from clan '{myClan.tag}'");
            if (ownerChanged)
            {
                var newOwner = myClan.GetIPlayer(myClan.owner);
                if (newOwner != null) SendReply(arg, $"New owner of clan '{myClan.tag}' is {newOwner.Name}");
            }
        }
        [ConsoleCommand("clans.changeowner")]
        void cclansChangeOwner(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelPromoteDemote) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.changeowner TAG playername/id");
                return;
            }
            Clan myClan;
            Clan check;
            if (!TryGetClan(arg.Args[0], out check))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            else myClan = (Clan)check;
            var promotePlayer = myClan.GetIPlayer(arg.Args[1]);
            if (promotePlayer == null)
            {
                SendReply(arg, string.Format(msg("nosuchplayer"), arg.Args[1]));
                return;
            }
            if (!myClan.IsMember(promotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("notaclanmember"), promotePlayer.Name));
                return;
            }
            if (myClan.IsOwner(promotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadyowner"), promotePlayer.Name));
                return;
            }
            string PromotedBy = consoleName;
            if (arg.Connection != null) PromotedBy = arg.Connection.username;
            if (myClan.council == promotePlayer.Id) myClan.council = null;
            myClan.moderators.Remove(promotePlayer.Id);
            myClan.owner = promotePlayer.Id;
            myClan.BroadcastLoc("playerpromotedowner", $"<color={clanServerColor}>{PromotedBy}</color>", myClan.ColNam(promotePlayer.Id, promotePlayer.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
            SendReply(arg, $"You promoted '{promotePlayer.Name}' to the {myClan.PlayerLevel(promotePlayer.Id.ToString())}");
        }
        [ConsoleCommand("clans.playerpromote")]
        void cclansPlayerPromote(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelPromoteDemote) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.playerpromote TAG playername/id");
                return;
            }
            Clan myClan;
            Clan check;
            if (!TryGetClan(arg.Args[0], out check))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            else myClan = (Clan)check;
            var promotePlayer = myClan.GetIPlayer(arg.Args[1]);
            if (promotePlayer == null)
            {
                SendReply(arg, string.Format(msg("nosuchplayer"), arg.Args[1]));
                return;
            }
            if (!myClan.IsMember(promotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("notaclanmember"), promotePlayer.Name));
                return;
            }
            if (enableClanAllies && myClan.IsCouncil(promotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadyacouncil"), promotePlayer.Name));
                return;
            }
            if (enableClanAllies && myClan.council != null && myClan.IsModerator(promotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadyacouncilset"), promotePlayer.Name));
                return;
            }
            if (!enableClanAllies && myClan.IsModerator(promotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("alreadyamod"), promotePlayer.Name));
                return;
            }
            if (!myClan.IsModerator(promotePlayer.Id) && limitModerators >= 0 && myClan.moderators.Count() >= limitModerators)
            {
                SendReply(arg, string.Format(msg("maximummods")));
                return;
            }
            string PromotedBy = consoleName;
            if (arg.Connection != null) PromotedBy = arg.Connection.username;
            if (enableClanAllies && myClan.IsModerator(promotePlayer.Id))
            {
                myClan.council = promotePlayer.Id;
                myClan.moderators.Remove(promotePlayer.Id);
                myClan.BroadcastLoc("playerpromotedcouncil", $"<color={clanServerColor}>{PromotedBy}</color>", myClan.ColNam(promotePlayer.Id, promotePlayer.Name));
            }
            else
            {
                myClan.moderators.Add(promotePlayer.Id);
                myClan.BroadcastLoc("playerpromoted", $"<color={clanServerColor}>{PromotedBy}</color>", myClan.ColNam(promotePlayer.Id, promotePlayer.Name));
            }
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
            SendReply(arg, $"You promoted '{promotePlayer.Name}' to a {myClan.PlayerLevel(promotePlayer.Id.ToString())}");
        }
        [ConsoleCommand("clans.playerdemote")]
        void cclansPlayerDemote(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelPromoteDemote) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: clans.playerdemote TAG playername/id");
                return;
            }
            Clan myClan;
            Clan check;
            if (!TryGetClan(arg.Args[0], out check))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            else myClan = (Clan)check;
            var demotePlayer = myClan.GetIPlayer(arg.Args[1]);
            if (demotePlayer == null)
            {
                SendReply(arg, string.Format(msg("nosuchplayer"), arg.Args[1]));
                return;
            }
            if (!myClan.IsMember(demotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("notaclanmember"), demotePlayer.Name));
                return;
            }
            if (!myClan.IsModerator(demotePlayer.Id) && !myClan.IsCouncil(demotePlayer.Id))
            {
                SendReply(arg, string.Format(msg("notpromoted"), demotePlayer.Name));
                return;
            }
            string DemotedBy = consoleName;
            if (arg.Connection != null) DemotedBy = arg.Connection.username;
            if (enableClanAllies && myClan.IsCouncil(demotePlayer.Id))
            {
                myClan.council = null;
                if (limitModerators >= 0 && myClan.moderators.Count() >= limitModerators) myClan.BroadcastLoc("playerdemoted", $"<color={clanServerColor}>{DemotedBy}</color>", myClan.ColNam(demotePlayer.Id, demotePlayer.Name));
                else
                {
                    myClan.moderators.Add(demotePlayer.Id);
                    myClan.BroadcastLoc("councildemoted", $"<color={clanServerColor}>{DemotedBy}</color>", myClan.ColNam(demotePlayer.Id, demotePlayer.Name));
                }
            }
            else
            {
                myClan.moderators.Remove(demotePlayer.Id);
                myClan.BroadcastLoc("playerdemoted", $"<color={clanServerColor}>{DemotedBy}</color>", myClan.ColNam(demotePlayer.Id, demotePlayer.Name));
            }
            myClan.updated = UnixTimeStampUTC();
            myClan.OnUpdate();
            SendReply(arg, $"You demoted '{demotePlayer.Name}' to a {myClan.PlayerLevel(demotePlayer.Id.ToString())}");
        }
        [ConsoleCommand("clans.delete")]
        void cclansDelete(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < authLevelDelete) return;
            if (arg.Args == null || arg.Args.Length != 1)
            {
                SendReply(arg, "Usage: clans.delete TAG");
                return;
            }
            Clan clan;
            if (!TryGetClan(arg.Args[0], out clan))
            {
                SendReply(arg, string.Format(msg("noclanfound"), arg.Args[0]));
                return;
            }
            string DeletedBy = consoleName;
            if (arg.Connection != null) DeletedBy = arg.Connection.username;
            clan.BroadcastLoc("clandeleted", $"<color={clanServerColor}>{DeletedBy}</color>");
            RemoveClan(arg.Args[0]);
            foreach (var member in clan.members) clanCache.Remove(member.Key);
            setupPlayers(clan.members.Keys.ToList(), true, clan.tag);
            string permGroup = permGroupPrefix + clan.tag;
            if (permission.GroupExists(permGroup))
            {
                foreach (var member in clan.members) if (permission.UserHasGroup(member.Key, permGroup)) permission.RemoveUserGroup(member.Key, permGroup);
                permission.RemoveGroup(permGroup);
            }
            foreach (var ally in clans)
            {
                Clan allyClan = clans[ally.Key];
                allyClan.clanAlliances.Remove(arg.Args[0]);
                allyClan.invitedAllies.Remove(arg.Args[0]);
                allyClan.pendingInvites.Remove(arg.Args[0]);
            }
            SendReply(arg, string.Format(msg("youdeleted"), clan.tag));
            clan.OnDestroy();

            //Interface.Oxide.CallHook("OnClanDisbanded", clan.members);
            List<UInt64> MemberListUInt64 = new List<UInt64>();
            MemberListUInt64 = clan.members.Select(x => Convert.ToUInt64(x.Key)).ToList();
            Interface.Oxide.CallHook("OnClanDisbanded", clan.tag, MemberListUInt64);
            AllyRemovalCheck();
        }
        bool FilterText(string tag)
        {
            foreach (string bannedword in wordFilter) if (TranslateLeet(tag).ToLower().Contains(bannedword.ToLower())) return true;
            return false;
        }
        string TranslateLeet(string original)
        {
            string translated = original;
            Dictionary<string, string> leetTable = new Dictionary<string, string> {
    {
    "}{", "h"
}
, {
    "|-|", "h"
}
, {
    "]-[", "h"
}
, {
    "/-/", "h"
}
, {
    "|{", "k"
}
, {
    "/\\/\\", "m"
}
, {
    "|\\|", "n"
}
, {
    "/\\/", "n"
}
, {
    "()", "o"
}
, {
    "[]", "o"
}
, {
    "vv", "w"
}
, {
    "\\/\\/", "w"
}
, {
    "><", "x"
}
, {
    "2", "z"
}
, {
    "4", "a"
}
, {
    "@", "a"
}
, {
    "8", "b"
}
, {
    "ß", "b"
}
, {
    "(", "c"
}
, {
    "<", "c"
}
, {
    "{", "c"
}
, {
    "3", "e"
}
, {
    "€", "e"
}
, {
    "6", "g"
}
, {
    "9", "g"
}
, {
    "&", "g"
}
, {
    "#", "h"
}
, {
    "$", "s"
}
, {
    "7", "t"
}
, {
    "|", "l"
}
, {
    "1", "i"
}
, {
    "!", "i"
}
, {
    "0", "o"
}
,
};

            foreach (var leet in leetTable) translated = translated.Replace(leet.Key, leet.Value);
            return translated;
        }
        bool TryGetClan(string input, out Clan clan)
        {
            clan = default(Clan);
            Clan tmp = null;
            if (clans.TryGetValue(input, out tmp))
            {
                clan = tmp;
                return true;
            }
            string name;
            if (clansSearch.TryGetValue(input.ToLower(), out name))
            {
                if (clans.TryGetValue(name, out tmp))
                {
                    clan = tmp;
                    return true;
                }
            }
            return false;
        }
        void RemoveClan(string tag)
        {
            clans.Remove(tag);
            clansSearch.Remove(tag.ToLower());
        }
        [HookMethod("EnableBypass")]
        void EnableBypass(object userId)
        {
            if (!enableFFOPtion || userId == null) return;
            if (userId is string) userId = Convert.ToUInt64((string)userId);
            bypass.Add((ulong)userId);
        }
        [HookMethod("DisableBypass")]
        void DisableBypass(object userId)
        {
            if (!enableFFOPtion || userId == null) return;
            if (userId is string) userId = Convert.ToUInt64((string)userId);
            bypass.Remove((ulong)userId);
        }
    }
}