using Facepunch.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
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
    [Info("Clans", "C cайта на букву T", "1.2.2", ResourceId = 14)]
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
        bool enableClanRadar;
        string colorClanRadarOff;
        string colorClanRadarOn;
        float refreshTime;
        string nameColor;
        string sleeperNameColor;
        string distanceColor;
        static float minDistance;
        static float maxNamedistance;
        static float maxSleeperDistance;
        bool showSleepers;
        bool extendOnAllyMembers;
        bool enableAtLogin;
        int radarTextSize;
        string permissionClanRadarUse;
        bool usePermissionClanRadar;
        string chatCommandRadar;
        List<object> wordFilter = new List<object>();
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
        int PointsOfSuicide = 1;
        int PointsOfGatherSulfur = 1;
        int PointsOfGatherMetalOre = 1;
        int PointsOfGatherStone = 1;
        int PointsOfGatherWood = 1;
        int PointsOfGatherHQM = 1;
        int PointsOfBarrel = 1;


        void LoadVariables()
        {
            wordFilter = (List<object>)GetConfig("Фильтр", "Слова", filterDefaults());

            PointsOfDeath = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков убирает за смерть", 1));
            PointsOfKilled = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за убийство игрока", 1));
            PointsOfKilledHeli = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за сбитие вертолёта", 1));
            PointsOfSuicide = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков забираем за суицид", 1));
            PointsOfGatherSulfur = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу серы (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherMetalOre = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу металической руды (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherStone = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу камня (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherWood = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу дерева (за разбитый камень - то есть за последний удар)", 1));
            PointsOfGatherHQM = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за добычу МВК (за разбитый камень - то есть за последний удар)", 1));
            PointsOfBarrel = Convert.ToInt32(GetConfig("ТОП", "Очки: Сколько очков даем за разрушение бочки (за разбитый камень - то есть за последний удар)", 1));

            limitMembers = Convert.ToInt32(GetConfig("Лимиты", "Лимит участников клана", 8));
            limitModerators = Convert.ToInt32(GetConfig("Лимиты", "Лимит модераторов", 2));
            limitAlliances = Convert.ToInt32(GetConfig("Лимиты", "Лимит альянса", 2));
            tagLengthMin = Convert.ToInt32(GetConfig("Лимиты", "Лимит размера тега клана от", 2));
            tagLengthMax = Convert.ToInt32(GetConfig("Лимиты", "Лимит размера тега клана до", 6));
            inviteValidDays = Convert.ToInt32(GetConfig("Лимиты", "Длительность жизни приглашения", 1));
            friendlyFireNotifyTimeout = Convert.ToInt32(GetConfig("Лимиты", "Таймаус сообщения об FF", 5));
            allowedSpecialChars = Convert.ToString(GetConfig("Лимиты", "Разрешены специальные символы", "!²³"));
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
            colorClanFFOff = Convert.ToString(GetConfig("Оформление", "Цвет сообщения об отключении FF", "lime"));
            colorClanFFOn = Convert.ToString(GetConfig("Оформление", "Цвет сообщения об включении FF", "red"));
            pluginPrefix = Convert.ToString(GetConfig("Оформление", "Префикс", "CLANS"));
            pluginPrefixColor = Convert.ToString(GetConfig("Оформление", "Цвет префикса в сообщении", "orange"));
            pluginPrefixREBORNColor = Convert.ToString(GetConfig("Оформление", "Цвет префикса REBORN в сообщении", "#ce422b"));
            pluginPrefixREBORNShow = Convert.ToBoolean(GetConfig("Оформление", "Включить префикс Reborn?", true));
            pluginPrefixFormat = Convert.ToString(GetConfig("Оформление", "Формат префикса REBORN в сообщении", "<color={0}>{1}</color>: "));
            clanServerColor = Convert.ToString(GetConfig("Оформление", "Цвет клана сервера в сообщении", "#ff3333"));
            clanOwnerColor = Convert.ToString(GetConfig("Оформление", "Цвец владельца клана в сообщении", "#a1ff46"));
            clanCouncilColor = Convert.ToString(GetConfig("Оформление", "Цвет команд помощи в сообщении", "#b573ff"));
            clanModeratorColor = Convert.ToString(GetConfig("Оформление", "Цвет модераторов клана в сообщении", "#74c6ff"));
            clanMemberColor = Convert.ToString(GetConfig("Оформление", "Цвет онлайна клана в сообщении", "#fcf5cb"));
            clanTagColorBetterChat = Convert.ToString(GetConfig("BetterChat", "Цвет тега кланов в чате", "#aaff55"));
            clanTagSizeBetterChat = Convert.ToInt32(GetConfig("BetterChat", "Размер тега клано в чате", 15));
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
            enableClanRadar = Convert.ToBoolean(GetConfig("Клановый радар", "Включить радар дял кланов", false));
            colorClanRadarOff = Convert.ToString(GetConfig("Клановый радар", "Цвет радара когда он отключен", "silver"));
            colorClanRadarOn = Convert.ToString(GetConfig("Клановый радар", "Цвет радара когда он включен", "lime"));
            refreshTime = Convert.ToSingle(GetConfig("Клановый радар", "Время обновления", 3.0));
            nameColor = Convert.ToString(GetConfig("Клановый радар", "Цвет имен", "#008000"));
            sleeperNameColor = Convert.ToString(GetConfig("Клановый радар", "Цвет имен спящих", "#ff00ff"));
            distanceColor = Convert.ToString(GetConfig("Клановый радар", "Цвет дистанции", "#0000ff"));
            minDistance = Convert.ToSingle(GetConfig("Клановый радар", "Минимальная дистанция", 10.0));
            maxNamedistance = Convert.ToSingle(GetConfig("Клановый радар", "Максимальная дистанция для онлайн игроков", 200.0));
            maxSleeperDistance = Convert.ToSingle(GetConfig("Клановый радар", "Максимальная дистанция для спящих игроков", 50.0));
            showSleepers = Convert.ToBoolean(GetConfig("Клановый радар", "Показывать слиперов?", false));
            radarTextSize = Convert.ToInt32(GetConfig("Клановый радар", "Размер текста в радаре", 15));
            extendOnAllyMembers = Convert.ToBoolean(GetConfig("Клановый радар", "Включить отображение участников альянса на радаре", false));
            enableAtLogin = Convert.ToBoolean(GetConfig("Клановый радар", "Включить тех кто входит", false));
            permissionClanRadarUse = Convert.ToString(GetConfig("Клановый радар", "Привилегия на использование радара", "clans.radaruse"));
            usePermissionClanRadar = Convert.ToBoolean(GetConfig("Клановый радар", "Использовать привилегию на включение радара ?", false));
            chatCommandRadar = Convert.ToString(GetConfig("Клановый радар", "Чатовая команда открытия радара", "crd"));
            if (!Changed) return;
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
                { "nopermtojoin", "You got no rights to join a clan." }, 
				{ "nopermtojoinbyinvite", "The player {0} has no rights to join a clan." }, 
				{ "claninvite", "You have been invited to join the clan: [{0}] '{1}'\nTo join, type: <color={2}>/clan join {0}</color>" }, 
				{ "comeonline", "{0} has come online!" }, 
				{ "goneoffline", "{0} has gone offline!" }, 
				{ "friendlyfire", "{0} is a clan member and cannot be hurt.\nTo toggle clan friendlyfire type: <color={1}>/clan ff</color>" },
                { "allyfriendlyfire", "{0} is an ally member and cannot be hurt." }, 
				{ "notmember", "You are currently not a member of a clan." }, 
				{ "youareownerof", "You are the owner of:" }, 
				{ "youaremodof", "You are a moderator of:" }, 
				{ "youarecouncilof", "You are a council of:" }, 
				{ "youarememberof", "You are a member of:" },
                { "claninfo", " [{0}] {1}" }, 
				{ "memberon", "Members online: " }, 
				{ "overviewnamecolor", "<color={0}>{1}</color>" }, 
				{ "memberoff", "Members offline: " },
				{ "notmoderator", "You need to be a moderator of your clan to use this command." },
                { "pendinvites", "Pending invites: " }, 
				{ "bannedwords", "The clan tag contains banned words." }, 
				{ "viewthehelp", "To view more commands, type: <color={0}>/{1} helpies</color>" }, 
				{ "usagecreate", "Usage - <color={0}>/clan create \"TAG\" \"Description\"</color>" }, 
				{ "hintlength", "Clan tags must be {0} to {1} characters long" }, 
				{ "hintchars", "Clan tags must contain only 'a-z' 'A-Z' '0-9' '{0}'" }, 
				{ "providedesc", "Please provide a short description of your clan." }, 
				{ "tagblocked", "There is already a clan with this tag." }, 
				{ "nownewowner", "You are now the owner of the clan [{0}] \"{1}\"" }, 
				{ "inviteplayers", "To invite new members, type: <color={0}>/clan invite <name></color>" }, 
				{ "usageinvite", "Usage - <color={0}>/clan invite <name></color>" }, 
				{ "nosuchplayer", "No such player or player name not unique: {0}" }, 
				{ "alreadymember", "This player is already a member of your clan: {0}" }, 
				{ "alreadyinvited", "This player has already been invited to your clan: {0}" }, 
				{ "alreadyinclan", "This player is already in a clan: {0}" }, 
				{ "invitebroadcast", "{0} invited {1} to the clan." }, 
				{ "usagewithdraw", "Usage: <color={0}>/clan withdraw <name></color>" }, 
				{ "notinvited", "This player has not been invited to your clan: {0}" }, 
				{ "canceledinvite", "{0} canceled the invitation of {1}." }, 
				{ "usagejoin", "Usage: <color={0}>/clan join \"TAG\"</color>" }, 
				{ "youalreadymember", "You are already a member of a clan." }, 
				{ "younotinvited", "You have not been invited to join this clan." }, 
				{ "reachedmaximum", "This clan has already reached the maximum number of members." }, 
				{ "broadcastformat", "<color={0}>{1}</color>: {2}" }, 
				{ "allybroadcastformat", "[{0}] <color={1}>{2}</color>: {3}" }, 
				{ "clanrenamed", "{0} renamed your clan to: [{1}]." } , 
				{ "yourenamed", "You have renamed the clan [{0}] to [{1}]" }, 
				{ "clandeleted", "{0} deleted your clan." }, 
				{ "youdeleted", "You have deleted the clan [{0}]" }, 
				{ "noclanfound", "There is no clan with that tag [{0}]" }, 
				{ "renamerightsowner", "You need to be a server owner to rename clans." }, 
				{ "usagerename", "Usage: <color={0}>/clan rename OLDTAG NEWTAG</color>" }, 
				{ "deleterightsowner", "You need to be a server owner to delete clans." }, 
				{ "usagedelete", "Usage: <color={0}>/clan delete TAG</color>" }, 
				{ "clandisbanded", "Your current clan has been disbanded forever." }, 
				{ "needclanowner", "You need to be the owner of your clan to use this command." }, 
				{ "needclanownercouncil", "You need to be the owner or a council to use this command." }, 
				{ "usagedisband", "Usage: <color={0}>/clan disband forever</color>" }, 
				{ "usagepromote", "Usage: <color={0}>/clan promote <name></color>" }, 
				{ "playerjoined", "{0} has joined the clan!" }, 
				{ "waskicked", "{0} kicked {1} from the clan." }, 
				{ "modownercannotkicked", "The player {0} is an owner or moderator and cannot be kicked." }, 
				{ "notmembercannotkicked", "The player {0} is not a member of your clan." }, 
				{ "usageff", "Usage: <color={0}>/clan ff</color> toggles your current FriendlyFire status." }, 
				{ "usagekick", "Usage: <color={0}>/clan kick <name></color>" }, 
				{
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
                    "clanUItitle", "Clan System by TopPlugin / Oxide"
                },
                  {
                    "clanTOPUItitle", "Clans TOP"
                },
            }
            , this, "en");
			lang.RegisterMessages(new Dictionary<string, string> {
                {"nopermtocreate", "Ты не имеешь права создавать клан."},
                {"nopermtojoin", "Ты не имеешь права вступать в клан."}
                ,{"nopermtojoinbyinvite", "Игрок {0} не имеет права вступать в клан."}
                ,{"claninvite", "Вас пригласили вступить в клан: [{0}] '{1}'\nДля вступления введите: <color={2}>/clan join {0}</color>"}
                ,{"comeonline", "{0} зашёл в онлайн!"}
                ,{"goneoffline", "{0} ушел в офлайн!"}
                ,{"friendlyfire", "{0} является членом клана и не может быть поранен вами.\nЧтобы переключить режимы дружественного огня клана введите: <color={1}>/clan ff</color>"}
                ,{"allyfriendlyfire", "{0} является членом союзника и не может быть ранен."}
                ,{"notmember", "В настоящее время вы не являетесь членом клана."}
                ,{"youareownerof", "Вы являетесь владельцем:"}
                ,{"youaremodof", "Вы являетесь модератором:"}
                ,{"youarecouncilof", "Ты состоишь в:"}
                ,{"youarememberof", "Вы состоите в клане:"}
                ,{"claninfo", " [{0}] {1}"}
                ,{"memberon", "Участники онлайн: "}
                ,{"overviewnamecolor", "<color={0}>{1}</color>"}
                ,{"memberoff", "Участники в офлайн: "}
                ,{"notmoderator", "Вы должны быть модератором своего клана, чтобы использовать эту команду."}
                ,{"pendinvites", "Ожидающие приглашения: "}
                ,{"bannedwords", "Тег клана содержит запрещенные слова."}
                ,{"viewthehelp", "Чтобы просмотреть дополнительные команды, введите: <color={0}>/{1} helpies</color>"}
                ,{"usagecreate", "Использование - <color={0}>/clan create \"Название\" \"Описание\"</color>"}
                ,{"hintlength", "Теги клана должны быть длиной от {0} до {1} символов"}
                ,{"hintchars", "Клановые теги должны содержать только 'a-z' 'A-Z' '0-9' '{0}'"}
                ,{"providedesc", "Пожалуйста, дайте краткое описание вашего клана."}
                ,{"tagblocked", "Уже есть клан с этим тегом."}
                ,{"nownewowner", "Вы являетесь владельцем клана [{0}] \"{1}\""}
                ,{"inviteplayers", "Чтобы пригласить новых участников, введите: <color={0}>/clan invite <name></color>"}
                ,{"usageinvite", "Использование - <color={0}>/clan invite <name></color>"}
                ,{"nosuchplayer", "Нет такого игрока или имя игрока не уникально: {0}"}
                ,{"alreadymember", "Этот игрок уже является членом вашего клана: {0}"}
                ,{"alreadyinvited", "Этот игрок уже приглашен в ваш клан: {0}"}
                ,{"alreadyinclan", "Этот игрок уже состоит в клане: {0}"}
                ,{"invitebroadcast", "{0} приглашен {1} в клан."}
                ,{"usagewithdraw", "Использование: <color={0}>/clan withdraw <name></color>"}
                ,{"notinvited", "Этот игрок не был приглашен в ваш клан: {0}"}
                ,{"canceledinvite", "{0} отменил приглашение {1}."}
                ,{"usagejoin", "Использование: <color={0}>/clan join \"Название\"</color>"}
                ,{"youalreadymember", "Ты уже состоишь в клане."}
                ,{"younotinvited", "Вас не приглашали присоединиться к этому клану."}
                ,{"reachedmaximum", "Этот клан уже достиг максимального количества членов."}
                ,{"broadcastformat", "<color={0}>{1}</color>: {2}"}
                ,{"allybroadcastformat", "[{0}] <color={1}>{2}</color>: {3}"}
                ,{"clanrenamed", "{0} переименовал свой клан в: [{1}]."}
                ,{"yourenamed", "Вы переименовали клан с [{0}] в [{1}]"}
                ,{"clandeleted", "{0} удалил свой клан."}
                ,{"youdeleted", "Вы удалили клан [{0}]"}
                ,{"noclanfound", "Нет клана с такой меткой [{0}]"}
                ,{"renamerightsowner", "Вы должны быть владельцем сервера, чтобы переименовать кланы."}
                ,{"usagerename", "Использование: <color={0}>/clan rename OLDTAG NEWTAG</color>"}
                ,{"deleterightsowner", "Вы должны быть владельцем сервера, чтобы удалить клан."}
                ,{"usagedelete", "Использование: <color={0}>/clan delete Название</color>"}
                ,{"clandisbanded", "Ваш нынешний клан был распущен навсегда."}
                ,{"needclanowner", "Вы должны быть владельцем своего клана, чтобы использовать эту команду."}
                ,{"needclanownercouncil", "Вы должны быть владельцем или советом, чтобы использовать эту команду."}
                ,{"usagedisband", "Использование: <color={0}>/clan disband forever</color>"}
                ,{"usagepromote", "Использование: <color={0}>/clan promote <name></color>"}
                ,{"playerjoined", "{0} присоединился к клану!"}
                ,{"waskicked", "{0} выгнали {1} из клана."}
                ,{"modownercannotkicked", "Игрок {0} является владельцем или модератором и не может быть выгнан."}
                ,{"notmembercannotkicked", "Игрок {0} не является членом вашего клана."}
                ,{"usageff", "Использование: <color={0}>/clan ff</color> переключает ваш текущий статус урон по своим."}
                ,{"usagekick", "Использование: <color={0}>/clan kick <name></color>"}
                ,{"playerleft", "{0} покинул клан."}
                ,{"youleft", "Вы покинули клан."}
                ,{"usageleave", "Использование: <color={0}>/clan leave</color>"}
                ,{"notaclanmember", "Игрок {0} не является членом вашего клана."}
                ,{"alreadyowner", "Игрок {0} уже является владельцем вашего клана."}
                ,{"alreadyamod", "Игрок {0} уже является модератором вашего клана."}
                ,{"alreadyacouncil", "Игрок {0} уже является членом совета вашего клана."}
                ,{"alreadyacouncilset", "Должность председателя совета уже присуждена."}
                ,{"maximummods", "Этот клан уже достиг максимального количества модераторов."}
                ,{"playerpromoted", "{0} повышен {1} до модератора."}
                ,{"playerpromotedcouncil", "{0} выдвинут {1} в совет."}
                ,{"playerpromotedowner", "{0} повышен {1} до нового владельца."}
                ,{"usagedemote", "Использование: <color={0}>/clan demote <name></color>"}
                ,{"notamoderator", "Игрок {0} не является модератором вашего клана."}
                ,{"notpromoted", "Игрок {0} не является модератором или советом вашего клана."}
                ,{"playerdemoted", "{0} понижен в должности {1} до члена."}
                ,{"councildemoted", "{0} понижен в должности {1} до модератора."}
                ,{"noactiveally", "У вашего клана нет нынешних альянсов."}
                ,{"yourffstatus", "Огонь по своим:"}
                ,{"yourclanallies", "Ваши союзники по Клану:"}
                ,{"allyinvites", "Приглашения в союз:"}
                ,{"allypending", "Запросы союзников:"}
                ,{"allyReqHelp", "Предложите союз другому клану"}
                ,{"allyAccHelp", "Принимаем в альянс с другим кланом"}
                ,{"allyDecHelp", "Отказаться от союза с другим кланом"}
                ,{"allyCanHelp", "Отменить союз с другим кланом"}
                , {
                    "reqAlliance", "[{0}] запросил клановый союз"
                }
                , {
                    "invitePending", "У вас уже есть ожидающее приглашение на альянс [{0}]"
                }
                , {
                    "clanNoExist", "Клан [{0}] не существует"
                }
                , {
                    "alreadyAllies", "Вы уже вступили в союз с"
                }
                , {
                    "allyProvideName", "Вам нужно указать название клана"
                }
                , {
                    "allyLimit", "У вас уже есть максимально допустимый лимит союзников"
                }
                , {
                    "allyAccLimit", "Вы не можете принять союз с {0}. Вы достигли предела"
                }
                , {
                    "allyCancel", "Вы отменили свой союз с [{0}]"
                }
                , {
                    "allyCancelSucc", "{0} отменил ваш клановый союз"
                }
                , {
                    "noAlly", "Ваши кланы не имеют союза друг с другом"
                }
                , {
                    "noAllyInv", "У вас нет приглашения на альянс от [{0}]"
                }
                , {
                    "allyInvWithdraw", "Вы отменили свой запрос на [{0}]"
                }
                , {
                    "allyDeclined", "Вы отказались от кланового союза с [{0}]"
                }
                , {
                    "allyDeclinedSucc", "[{0}] отклонил ваш запрос на союз"
                }
                , {
                    "allyReq", "Вы запросили клановый союз у [{0}]"
                }
                , {
                    "allyAcc", "Вы приняли клановый союз от [{0}]"
                }
                , {
                    "allyAccSucc", "[{0}] принял ваш запрос на союз"
                }
                , {
                    "allyPendingInfo", "У вашего клана есть ожидающий запрос(ы) союзника (ов). Проверьте их в обзоре клана."
                }
                , {
                    "clanffdisabled", "У тебя есть <color={0}>disabled</color> дружественный огонь для вашего клана.\nВы в безопасности!"
                }
                , {
                    "clanffenabled", "У тебя есть <color={0}>enabled</color> дружественный огонь для вашего клана.\nБудь осторожен!"
                }
                , {
                    "yourname", "ВЫ"
                }
                , {
                    "helpavailablecmds", "Доступные команды:"
                }
                , {
                    "helpinformation", "Отображение информации о вашем клане"
                }
                , {
                    "helpmessagemembers", "Отправьте сообщение всем участникам"
                }
                , {
                    "helpmessageally", "Отправьте сообщение всем членам альянса"
                }
                , {
                    "helpcreate", "Создайте новый клан"
                }
                , {
                    "helpjoin", "Вступайте в клан по приглашению"
                }
                , {
                    "helpleave", "Оставь свой клан"
                }
                , {
                    "helptoggleff", "Переключение статуса friendlyfire"
                }
                , {
                    "helpinvite", "Пригласите игрока"
                }
                , {
                    "helpwithdraw", "Отменить приглашение"
                }
                , {
                    "helpkick", "Удар ногой по члену"
                }
                , {
                    "helpallyoptions", "Перечисляет параметры союзника"
                }
                , {
                    "helppromote", "Продвигать члена клуба"
                }
                , {
                    "helpdemote", "Понизить в должности члена"
                }
                , {
                    "helpdisband", "Расформируйте свой клан (без отмены)"
                }
                , {
                    "helpmoderator", "Модератор"
                }
                , {
                    "helpowner", "Владелец"
                }
                , {
                    "helpcommands", "команды:"
                }
                , {
                    "helpconsole", "Откройте консоль F1 и введите:"
                }
                , {
                    "yourradarstatus", "Твой КланРадар:"
                }
                , {
                    "clanradardisabled", "Клановый радар отключен"
                }
                , {
                    "clanradarenabled", "Клановый радар включен"
                }
                , {
                    "helptoggleradar", "Переключение статуса радара клана"
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
                    "allyArgAccept", "acc"
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
                    "clanchatmuted", "Вы не можете клацать, вы приглушены."
                },
                 {
                    "clanUItitle", "Клановая система TopPlugin / Oxide"
                },
                  {
                    "clanTOPUItitle", "ТОП СТАТИСТИКА КЛАНОВ"
                },
            }
            , this, "ru");
		}

        void Init()
        {
            cc = this;
            LoadVariables();
            LoadDefaultMessages();
            Initialized = false;
            if (!permission.PermissionExists(permissionClanRadarUse)) permission.RegisterPermission(permissionClanRadarUse, this);
            if (!permission.PermissionExists(permissionToCreateClan)) permission.RegisterPermission(permissionToCreateClan, this);
            if (!permission.PermissionExists(permissionToJoinClan)) permission.RegisterPermission(permissionToJoinClan, this);
            cmd.AddChatCommand(chatCommandFF, this, "cmdChatClanFF");
            //Custom code
            //cmd.AddChatCommand("clanui", this, "CLanUIInfo");

            cmd.AddChatCommand(chatCommandClan, this, "cmdChatClan");
            cmd.AddChatCommand(chatCommandClanChat, this, "cmdChatClanchat");
            cmd.AddChatCommand(chatCommandAllyChat, this, "cmdChatAllychat");
            cmd.AddChatCommand(chatCommandClanInfo, this, "cmdChatClanInfo");
            cmd.AddChatCommand(chatCommandRadar, this, "cmdChatClanRadar");
            cmd.AddChatCommand(chatCommandClan + subCommandClanHelp, this, "cmdChatClanHelp");
            cmd.AddChatCommand(chatCommandClan + subCommandClanAlly, this, "cmdChatClanAlly");
            if (enableClanTagging) Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
        }
        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Title != "Better Chat") return;
            if (enableClanTagging) Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
        }


        string getFormattedClanTag(IPlayer player)
        {
            var clan = findClanByUser(player.Id);
            if (clan != null && !string.IsNullOrEmpty(clan.tag)) return $"[#{clanTagColorBetterChat.Replace("#", "")}][+{clanTagSizeBetterChat}]{clanTagOpening}{clan.tag}{clanTagClosing}[/+][/#]";
            return string.Empty;
        }
        //Custom code
        [PluginReference] private Plugin ImageLibrary;
        public string GetImageSkin(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public List<ulong> GetImageSkins(string shortname) => ImageLibrary.Call("GetImageList", shortname) as List<ulong>;

        static Clans ins;

        void OnServerInitialized()
        {
            ins = this;
            if (!ImageLibrary)
            {
                PrintError("ImageLibrary not found! Clans not work!");
                Interface.Oxide.UnloadPlugin("Clans");
                return;
            }
            else
            {
                clanCache.ToList().ForEach(c => ImageLibrary?.Call("AddImage", c.Value.ClanAvatar, c.Value.ClanAvatar));
                ImageLibrary?.Call("AddImage", "https://i.imgur.com/d9HBO4C.png", "loot-barrel");
            }
            LoadData();
            if (enableClanRadar)
            {
                clanRadarMemberobjects = new Dictionary<string, List<BasePlayer>>();
                foreach (var clan in clans) clanRadarMemberobjects.Add(clan.Key, new List<BasePlayer>());
            }
            if (purgeOldClans) Puts($"Valid clans loaded: '{clans.Count}'");
            if (purgeOldClans && purgedClans.Count() > 0)
            {
                Puts($"Old Clans purged: '{purgedClans.Count}'");
                if (listPurgedClans)
                {
                    foreach (var purged in purgedClans) Puts($"Purged > {purged}");
                }
            }
            AllyRemovalCheck();
            tagReExt = new Regex("[^a-zA-Z0-9" + allowedSpecialChars + "]");
            foreach (var player in BasePlayer.activePlayerList) setupPlayer(player);
            foreach (var player in BasePlayer.sleepingPlayerList) setupPlayer(player);

            Initialized = true;
        }
        void OnServerSave() => SaveData();
        void OnNewSave()
        {
            if (wipeClansOnNewSave) newSaveDetected = true;
        }

        void Unload()
        {
            if (!Initialized) return;
            SaveData();
            foreach (var pair in originalNames)
            {
                var player = rust.FindPlayerByIdString(pair.Key);
                if (player != null && player.displayName != pair.Value)
                {
                    player.displayName = pair.Value;
                    if (player.net != null) player._name = string.Format("{1}[{0}/{2}]", player.net.ID, pair.Value, player.userID);
                    player.SendNetworkUpdate();
                }
            }
            if (enableClanRadar)
            {
                var objects = GameObject.FindObjectsOfType(typeof(ClanRadar));
                if (objects != null) foreach (var gameObj in objects) GameObject.Destroy(gameObj);
            }
        }

        void LoadData()
        {
            StoredData protoStorage = new StoredData();
            StoredData jsonStorage = new StoredData();
            StoredData oldStorage = new StoredData();
            bool protoFileFound = ProtoStorage.Exists(new string[] {
                this.Title
            }
            );
            bool jsonFileFound = Interface.GetMod().DataFileSystem.ExistsDatafile(this.Title);
            bool oldFileFound = Interface.GetMod().DataFileSystem.ExistsDatafile("rustio_clans");
            if (!protoFileFound && !jsonFileFound) oldStorage = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("rustio_clans");
            else
            {
                if (jsonFileFound) jsonStorage = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
                if (protoFileFound) protoStorage = ProtoStorage.Load<StoredData>(new string[] {
                    this.Title
                }
                );
            }
            bool lastwasProto = (protoStorage.lastStorage == "proto" && (protoStorage.saveStamp > jsonStorage.saveStamp || protoStorage.saveStamp > oldStorage.saveStamp));
            if (useProtostorageClandata)
            {
                if (lastwasProto)
                    clanSaves = ProtoStorage.Load<StoredData>(new string[] { this.Title }) ?? new StoredData();
                else
                {
                    if (oldFileFound && !jsonFileFound) clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("rustio_clans");
                    if (jsonFileFound) clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
                }
            }
            else
            {
                if (!lastwasProto)
                {
                    if (oldFileFound && !jsonFileFound) clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("rustio_clans");
                    if (jsonFileFound) clanSaves = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
                }
                else if (protoFileFound) clanSaves = ProtoStorage.Load<StoredData>(new string[] {
                    this.Title
                }
                ) ?? new StoredData();
            }
            if (wipeClansOnNewSave && newSaveDetected)
            {
                if (useProtostorageClandata) ProtoStorage.Save<StoredData>(clanSaves, new string[] {
                    this.Title+".bak"}
                );
                else Interface.Oxide.DataFileSystem.WriteObject(this.Title + ".bak", clanSaves);
                Puts("New save detected > Created backup of clans and wiped datafile.");
                clans = new Dictionary<string, Clan>();
                clansSearch = new Dictionary<string, string>();
                return;
            }
            clans = new Dictionary<string, Clan>();
            clansSearch = new Dictionary<string, string>();
            if (clanSaves.clans == null || clanSaves.clans.Count == 0) return;
            Puts("Loading clans data");
            clans = clanSaves.clans;
            InitializeClans(!jsonFileFound && !protoFileFound);
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
                        foreach (var member in clan.members) if (permission.UserHasGroup(member.Key, permGroupPrefix + clan.tag)) permission.RemoveUserGroup(member.Key, permGroupPrefix + clan.tag);
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

        void setupPlayer(BasePlayer player, string cName = "", string cId = "")
        {
            if (player == null || player.UserIDString == "" || player.displayName == "") return;
            if (player.GetInfoInt("noRadarAdmin", 0) != 0)
            {
                if (player.GetInfoInt("noRadarAdmin", 0) == 1) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
            }
            if (cName == "" || cId == "")
            {
                var current = this.covalence.Players.FindPlayerById(player.UserIDString);
                if (current == null) return;
                cName = current.Name;
                cId = current.Id;
            }
            if (!originalNames.ContainsKey(cId)) originalNames.Add(cId, cName);
            else originalNames[cId] = cName;
            var prevName = player.displayName;
            var clan = findClanByUser(cId);
            if (clan == null)
            {
                if (enableClanTagging)
                {
                    player.displayName = cName;
                    if (player.IsConnected) player._name = string.Format("{1}[{0}/{2}]", player.net.ID, cName, player.userID);
                }
            }
            else
            {
                if (enableClanRadar)
                {
                    if (!clanRadarMemberobjects.ContainsKey(clan.tag)) clanRadarMemberobjects.Add(clan.tag, new List<BasePlayer>());
                    if (!clanRadarMemberobjects[clan.tag].Contains(player)) clanRadarMemberobjects[clan.tag].Add(player);
                }
                if (enableClanTagging)
                {
                    player.displayName = $"[{clan.tag}] {cName}";
                }
                if (player.IsConnected)
                {
                    if (enableClanTagging) player._name = string.Format("{1}[{0}/{2}]", player.net.ID, $"[{clan.tag}] {cName}", player.userID);
                    clan.online++;
                }
                if (enableClanRadar && enableAtLogin && player.IsConnected)
                {
                    ClanRadar radar = player.transform.GetOrAddComponent<ClanRadar>();
                    radar.DoStart();
                }
            }
            if (enableClanTagging && player.displayName != prevName) player.SendNetworkUpdate();
        }
        void setupPlayers(List<string> playerIds)
        {
            if (enableClanTagging) foreach (var playerId in playerIds)
                {
                    var player = rust.FindPlayerByIdString(playerId);
                    if (player != null) setupPlayer(player);
                }
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || player.net == null || player.net.connection == null) return;
            player.displayName = player.net.connection.username;
            setupPlayer(player, player.net.connection.username, player.UserIDString);
            var clan = findClanByUser(player.UserIDString);
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
            var clan = findClanByUser(player.UserIDString);
            if (clan != null)
            {
                clan.BroadcastLoc("goneoffline", clan.ColNam(player.UserIDString, player.net.connection.username), "", "", "", player.UserIDString);
                clan.online--;
                manuallyEnabledBy.Remove(player.userID);
                if (enableClanRadar) activeRadarUsers.Remove(player.UserIDString);
            }
        }
        void OnDie(BasePlayer player)
        {
            if (player == null || !enableClanRadar) return;
            string tag = GetClanOf(player);
            if (tag != null && clanRadarMemberobjects.ContainsKey(tag)) clanRadarMemberobjects[tag].Remove(player);
        }
        void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null || !enableClanRadar) return;
            string tag = GetClanOf(player);
            if (tag == null) return;
            if (!clanRadarMemberobjects.ContainsKey(tag)) clanRadarMemberobjects.Add(tag, new List<BasePlayer>());
            if (!clanRadarMemberobjects[tag].Contains(player)) clanRadarMemberobjects[tag].Add(player);
            if (activeRadarUsers.Contains(player.UserIDString))
            {
                ClanRadar radar = player.transform.GetOrAddComponent<ClanRadar>();
                radar.DoStart();
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
                if (entity is BaseHelicopter && hit.Initiator is BasePlayer)
                    LastHeliHit[entity.net.ID] = hit.InitiatorPlayer.userID;

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
                hit.damageTypes = new DamageTypeList();
                hit.DidHit = false;
                hit.HitEntity = null;
                hit.Initiator = null;
                hit.DoHitEffects = false;
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
                hit.damageTypes = new DamageTypeList();
                hit.DidHit = false;
                hit.HitEntity = null;
                hit.Initiator = null;
                hit.DoHitEffects = false;
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
            if (opt == msg("clanArgCreate", player.UserIDString))
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
            else if (opt == msg("clanArgRadar", player.UserIDString))
            {
                if (!enableClanRadar || (usePermissionClanRadar && !permission.UserHasPermission(player.UserIDString, permissionClanRadarUse))) return;
                cmdChatClanRadar(player, command, args);
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
        void cmdClanOverview(BasePlayer player)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            var sb = new StringBuilder();

            string Messages = pluginPrefixREBORNShow == true ? $"<size=14><color={pluginPrefixREBORNColor}>REBORN\n</color></size>" : "\n";
            sb.Append($"<size=18><color={pluginPrefixColor}>{this.Title}</color></size>{Messages}");

            if (myClan == null)
            {
                sb.AppendLine(string.Format(msg("notmember", current.Id)));
                sb.AppendLine(string.Format(msg("viewthehelp", current.Id), colorCmdUsage, $"{chatCommandClan + "help"} | /{chatCommandClan}"));
                SendReply(player, $"<color={colorTextMsg}>{sb.ToString().TrimEnd()}</color>");
                return;
            }
            ClanUI(player);
            if (myClan.IsOwner(current.Id)) sb.Append(string.Format(msg("youareownerof", current.Id)));
            else if (myClan.IsCouncil(current.Id)) sb.Append(string.Format(msg("youarecouncilof", current.Id)));
            else if (myClan.IsModerator(current.Id)) sb.Append(string.Format(msg("youaremodof", current.Id)));
            else sb.Append(string.Format(msg("youarememberof", current.Id)));
            sb.AppendLine($" <color={colorClanNamesOverview}>{myClan.tag}</color> ( {myClan.online}/{myClan.total} )");
            sb.Append(string.Format(msg("memberon", current.Id)));
            int n = 0;
            foreach (var memberId in myClan.members)
            {
                var op = this.covalence.Players.FindPlayerById(memberId.Key);
                if (op != null && op.IsConnected)
                {
                    var memberName = op.Name;
                    if (op.Name == current.Name) memberName = msg("yourname", current.Id);
                    if (n > 0) sb.Append(", ");
                    var memberOn = string.Empty;
                    if (myClan.IsOwner(memberId.Key))
                    {
                        memberOn = string.Format(msg("overviewnamecolor", current.Id), clanOwnerColor, memberName);
                    }
                    else if (myClan.IsCouncil(memberId.Key))
                    {
                        memberOn = string.Format(msg("overviewnamecolor", current.Id), clanCouncilColor, memberName);
                    }
                    else if (myClan.IsModerator(memberId.Key))
                    {
                        memberOn = string.Format(msg("overviewnamecolor", current.Id), clanModeratorColor, memberName);
                    }
                    else
                    {
                        memberOn = string.Format(msg("overviewnamecolor", current.Id), clanMemberColor, memberName);
                    }
                    ++n;
                    sb.Append(memberOn);
                }
            }
            if (n > 0) sb.AppendLine();
            bool offline = false;
            foreach (var memberId in myClan.members)
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
                sb.Append(string.Format(msg("memberoff", current.Id)));
                n = 0;
                foreach (var memberId in myClan.members)
                {
                    var p = this.covalence.Players.FindPlayerById(memberId.Key);
                    var memberOff = string.Empty;
                    if (p != null && !p.IsConnected)
                    {
                        if (n > 0) sb.Append(", ");
                        if (myClan.IsOwner(memberId.Key))
                        {
                            memberOff = string.Format(msg("overviewnamecolor", current.Id), clanOwnerColor, p.Name);
                        }
                        else if (myClan.IsCouncil(memberId.Key))
                        {
                            memberOff = string.Format(msg("overviewnamecolor", current.Id), clanCouncilColor, p.Name);
                        }
                        else if (myClan.IsModerator(memberId.Key))
                        {
                            memberOff = string.Format(msg("overviewnamecolor", current.Id), clanModeratorColor, p.Name);
                        }
                        else
                        {
                            memberOff = string.Format(msg("overviewnamecolor", current.Id), clanMemberColor, p.Name);
                        }
                        ++n;
                        sb.Append(memberOff);
                    }
                }
                if (n > 0) sb.AppendLine();
            }
            if ((myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id) || myClan.IsModerator(current.Id)) && myClan.invites.Count() > 0)
            {
                sb.Append(string.Format(msg("pendinvites", current.Id)));
                int m = 0;
                foreach (var inviteId in myClan.invites)
                {
                    var p = this.covalence.Players.FindPlayerById(inviteId.Key);
                    if (p != null)
                    {
                        var invitedPlayer = string.Empty;
                        if (m > 0) sb.Append(", ");
                        invitedPlayer = string.Format(msg("overviewnamecolor", current.Id), clanMemberColor, p.Name);
                        ++m;
                        sb.Append(invitedPlayer);
                    }
                }
                if (m > 0) sb.AppendLine();
            }
            if (enableClanAllies && myClan.clanAlliances.Count() > 0) sb.AppendLine(string.Format(msg("yourclanallies", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.clanAlliances.ToArray()) + "</color>");
            if (enableClanAllies && (myClan.invitedAllies.Count() > 0 || myClan.pendingInvites.Count() > 0) && (myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id)))
            {
                if (myClan.invitedAllies.Count() > 0) sb.AppendLine(string.Format(msg("allyinvites", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.invitedAllies.ToArray()) + "</color> ");
                if (myClan.pendingInvites.Count() > 0) sb.AppendLine(string.Format(msg("allypending", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.pendingInvites.ToArray()) + "</color> ");
                if (myClan.pendingInvites.Count() == 0 && myClan.invitedAllies.Count() == 0) sb.AppendLine();
            }
            if (enableFFOPtion) sb.AppendLine(string.Format(msg("yourffstatus", current.Id)) + " " + (manuallyEnabledBy.Contains(player.userID) ? $"<color={colorClanFFOn}>ON</color>" : $"<color={colorClanFFOff}>OFF</color>") + $" ( <color={colorCmdUsage}>/{chatCommandFF}</color> )");
            if ((enableClanRadar && !usePermissionClanRadar) || enableClanRadar && usePermissionClanRadar && permission.UserHasPermission(current.Id, permissionClanRadarUse)) sb.AppendLine(string.Format(msg("yourradarstatus", current.Id)) + " " + (player.GetComponent<ClanRadar>() ? $"<color={colorClanRadarOn}>ON</color>" : $"<color={colorClanRadarOff}>OFF</color>") + $" ( <color={colorCmdUsage}>/{chatCommandRadar}</color> )");
            sb.AppendLine(string.Format(msg("viewthehelp", current.Id), colorCmdUsage, $"{string.Concat(chatCommandClan, subCommandClanHelp)} | /{chatCommandClan}"));
            string openText = $"<color={colorTextMsg}>";
            string closeText = "</color>";
            string[] parts = sb.ToString().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
        void cmdClanCreate(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan != null)
            {
                PrintChat(player, string.Format(msg("youalreadymember", current.Id)));
                return;
            }
            if (usePermToCreateClan && !permission.UserHasPermission(current.Id, permissionToCreateClan))
            {
                PrintChat(player, msg("nopermtocreate", current.Id));
                return;
            }
            if (args.Length < 2)
            {
                PrintChat(player, string.Format(msg("usagecreate", current.Id), colorCmdUsage));
                return;
            }
            if (tagReExt.IsMatch(args[1]))
            {
                PrintChat(player, string.Format(msg("hintchars", current.Id), allowedSpecialChars));
                return;
            }
            if (args[1].Length < tagLengthMin || args[1].Length > tagLengthMax)
            {
                PrintChat(player, string.Format(msg("hintlength", current.Id), tagLengthMin, tagLengthMax));
                return;
            }
            if (args.Length > 2)
            {
                args[2] = args[2].Trim();
                if (args[2].Length < 2 || args[2].Length > 30)
                {
                    PrintChat(player, string.Format(msg("providedesc", current.Id)));
                    return;
                }
            }
            if (enableWordFilter && FilterText(args[1]))
            {
                PrintChat(player, string.Format(msg("bannedwords", current.Id)));
                return;
            }
            string[] clanKeys = clans.Keys.ToArray();
            clanKeys = clanKeys.Select(c => c.ToLower()).ToArray();
            if (clanKeys.Contains(args[1].ToLower()))
            {
                PrintChat(player, string.Format(msg("tagblocked", current.Id)));
                return;
            }
            //Custom code
            myClan = Clan.Create(args[1], args.Length > 2 ? args[2] : string.Empty, current.Id, current.Name, "https://i.imgur.com/mNjQeB7.png");
            clans.Add(myClan.tag, myClan);
            clanCache[current.Id] = myClan;
            setupPlayer(player, current.Name, current.Id);
            if (usePermGroups && !permission.GroupExists(permGroupPrefix + myClan.tag)) permission.CreateGroup(permGroupPrefix + myClan.tag, "Clan " + myClan.tag, 0);
            if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag);
            myClan.onCreate();
            myClan.total++;
            PrintChat(player, string.Format(msg("nownewowner", current.Id), myClan.tag, myClan.description) + "\n" + string.Format(msg("inviteplayers", current.Id), colorCmdUsage));
            return;
        }
        public void InvitePlayer(BasePlayer player, string targetId) => cmdClanInvite(player, new string[] {
            "", targetId
        }
        );
        void cmdClanInvite(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                PrintChat(player, string.Format(msg("notmember", current.Id)));
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
            myClan.BroadcastLoc("invitebroadcast", myClan.ColNam(current.Id, current.Name), myClan.ColNam(invPlayer.Id, invPlayer.Name));
            if (invPlayer.IsConnected)
            {
                var invited = rust.FindPlayerByIdString(invPlayer.Id);
                if (invited != null) PrintChat(invited, string.Format(msg("claninvite", invPlayer.Id), myClan.tag, myClan.description, colorCmdUsage));
            }
            myClan.updated = UnixTimeStampUTC();
        }



        string ButtonListed = "[{\"name\":\"clans_player{id}\",\"parent\":\"clans_main7\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.001 {amin}\",\"anchormax\":\"0.998 {amax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"clans_player{id}\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"clans_player{id}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        [ConsoleCommand("clanui.page")]
        void cmdConsoleClanUI(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null || args == null) return;
            CLanUIInfo(player, null, args.Args);
        }

        private string GetImageUrl(string shortname, ulong skinid) =>
           ImageLibrary.CallHook("GetImageURL", shortname, skinid) as string;

        private void AddLoadOrder(IDictionary<string, string> imageList, bool replace = false) =>
           ImageLibrary?.Call("ImportImageList", Title, imageList, (ulong)ResourceId, replace);

        private string GetImage(string shortname, ulong skinid) =>
           ImageLibrary.CallHook("GetImage", $"{shortname} {skinid}") as string;

        void CLanUIInfo(BasePlayer player, string command, string[] args)
        {
            var sb = new StringBuilder();
            if (findClanByUser(player.UserIDString) == null)
            {
                sb.AppendLine(string.Format(msg("notmember", player.UserIDString)));
                sb.AppendLine(string.Format(msg("viewthehelp", player.UserIDString), colorCmdUsage, $"{chatCommandClan + "help"} | /{chatCommandClan}"));
                SendReply(player, $"<color={colorTextMsg}>{sb.ToString().TrimEnd()}</color>");
                return;
            }
            else
            {
                int page = 0;
                bool Enabled = false;
                if (args.Length > 0)
                    page = int.Parse(args[0]);
                if (args.Length > 1)
                    Enabled = true;
                ClanUI(player, page, Enabled);
            }
        }

        bool? CanWearItem(PlayerInventory inventory, Item item, int targetPos)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            var clan = findClanByUser(player.UserIDString);
            if (clan != null)
                if (clan.SkinList.ContainsKey(item.info.shortname))
                    item.skin = clan.SkinList[item.info.shortname];
            return null;
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;
            var player = entity.ToPlayer();
            var clan = findClanByUser(player.UserIDString);
            if (clan != null && clan.members[player.UserIDString].GatherInfo.ContainsKey(item.info.shortname))
                if (clan.Change.ContainsKey(item.info.shortname) && clan.Change[item.info.shortname].Complete < clan.Change[item.info.shortname].Need)
                {
                    clan.Change[item.info.shortname].Complete = clan.Change[item.info.shortname].Complete + (uint)item.amount;
                    clan.members[player.UserIDString].GatherInfo[item.info.shortname] = clan.members[player.UserIDString].GatherInfo[item.info.shortname] + item.amount;
                    if (clan.Change[item.info.shortname].Complete > clan.Change[item.info.shortname].Need)
                        clan.Change[item.info.shortname].Complete = clan.Change[item.info.shortname].Need;
                }
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null) return;
            var clan = findClanByUser(player.UserIDString);
            if (clan != null && clan.members[player.UserIDString].GatherInfo.ContainsKey(item.info.shortname))
            {
                if (clan.Change.ContainsKey(item.info.shortname) && clan.Change[item.info.shortname].Complete < clan.Change[item.info.shortname].Need)
                {
                    clan.Change[item.info.shortname].Complete = clan.Change[item.info.shortname].Complete + (uint)item.amount;
                    clan.members[player.UserIDString].GatherInfo[item.info.shortname] = clan.members[player.UserIDString].GatherInfo[item.info.shortname] + item.amount;
                    if (clan.Change[item.info.shortname].Complete > clan.Change[item.info.shortname].Need)
                        clan.Change[item.info.shortname].Complete = clan.Change[item.info.shortname].Need;
                }
                switch (item.info.shortname)
                {
                    case "stones":
                        clan.members[player.UserIDString].GatherStone += item.amount;
                        clan.members[player.UserIDString].PlayerPoints += PointsOfGatherStone;
                        clan.ClanPoints += PointsOfGatherStone;
                        break;
                    case "wood":
                        clan.members[player.UserIDString].GatherWood += item.amount;
                        clan.members[player.UserIDString].PlayerPoints += PointsOfGatherWood;
                        clan.ClanPoints += PointsOfGatherWood;
                        break;
                    case "metal.ore":
                        clan.members[player.UserIDString].GatherMetal += item.amount;
                        clan.members[player.UserIDString].PlayerPoints += PointsOfGatherMetalOre;
                        clan.ClanPoints += PointsOfGatherMetalOre;
                        break;
                    case "sulfur.ore":
                        clan.members[player.UserIDString].GatherSulfur += item.amount;
                        clan.members[player.UserIDString].PlayerPoints += PointsOfGatherSulfur;
                        clan.ClanPoints += PointsOfGatherSulfur;
                        break;
                    case "hq.metal.ore":
                        clan.members[player.UserIDString].GatherHQM += item.amount;
                        clan.members[player.UserIDString].PlayerPoints += PointsOfGatherHQM;
                        clan.ClanPoints += PointsOfGatherHQM;
                        break;
                }
            }

        }

        [ConsoleCommand("clans_getskinIds")]
        void cmdClansGetSkinList(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var SkinList = GetImageSkins(args.Args[0]);
            var elements = new CuiElementContainer();

            elements.Add(new CuiElement
            {
                Name = "clans_skinlist",
                Parent = "clans_main2",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0 0 0 0.85",Sprite = "assets/content/ui/ui.background.tile.psd",Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
            });

            elements.Add(new CuiElement
            {
                Name = "clans_main200",
                Parent = "clans_main2",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0 0 0 0.85", Sprite = "assets/content/ui/ui.background.tile.psd"},
                        new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "1 1.1"}
                    }
            });

            elements.Add(new CuiElement
            {
                Parent = "clans_main200",
                Components =
                    {
                        new CuiTextComponent { Text = "<size=26>CHOOSE THE SKIN YOU NEED BY JUST CLICKING ON IT</size>", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
            });

            var poses = GetPositions(7, 5, 0.01f, 0.01f);
            if (SkinList.Count > 35) SkinList.Take(35);
            var count = SkinList.Count < 35 ? SkinList.Count : 35;
            for (int i = 0; i < count; i++)
            {
                elements.Add(new CuiElement
                {
                    Name = "clans_skinlist" + SkinList[i],
                    Parent = "clans_skinlist",
                    Components =
                    {
                        new CuiRawImageComponent {FadeIn = 0.5f, Color = "0.3294118 0.3294118 0.3294118 0.5", Sprite = "assets/content/ui/ui.background.tile.psd"},
                        new CuiRectTransformComponent {AnchorMin = poses[i].AnchorMin, AnchorMax = poses[i].AnchorMax}
                    }
                });

                elements.Add(new CuiElement
                {
                    Parent = "clans_skinlist" + SkinList[i],
                    Components =
                    {
                        new CuiRawImageComponent {FadeIn = 0.5f , Png = GetImageSkin(args.Args[0], SkinList[i])},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                elements.Add(new CuiButton
                {
                    Button = { Color = "0.13 0.44 0.48 0", Command = $"clan_changeskin {args.Args[0]} {SkinList[i]}" },
                    Text = { Text = "", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "clans_skinlist" + SkinList[i]);
            }

            elements.Add(new CuiElement
            {
                Name = "clans_skinlist_input",
                Parent = "clans_skinlist",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0 0 0 0.85", Sprite = "assets/content/ui/ui.background.tile.psd",Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        new CuiRectTransformComponent {AnchorMin = "0 -0.1", AnchorMax = "1 0"}
                    }
            });


            elements.Add(new CuiElement
            {
                Parent = "clans_skinlist_input",
                Components =
                    {
                        new CuiTextComponent { Text = "<size=24>YOU CAN ALSO ENTER YOUR SKIN ID HERE</size>", Color = "1 0.9294118 0.8666667 0.01", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
            });

            elements.Add(new CuiElement()
            {
                Parent = "clans_skinlist_input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        CharsLimit = 64,
                        FontSize = 26,
                        Command = $"clan_changeskin {args.Args[0]} ",
                        Font = "robotocondensed-bold.ttf",
                        Text = "",
                        Color = "1 0.9294118 0.8666667 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    }
                }
            });


            CuiHelper.AddUi(player, elements);

        }

        [ConsoleCommand("clan_setChange")]
        void cmdSetChangeOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, "clan_setChange");

            var clan = findClanByUser(player.UserIDString);
            var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "clan_setChange",
                Parent = "clans_main2",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0 0 0 0.85",Sprite = "assets/content/ui/ui.background.tile.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
            });
            elements.Add(new CuiElement
            {
                Name = "clan_setChange1",
                Parent = "clan_setChange",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0 0 0 0",Sprite = "assets/content/ui/ui.background.tile.psd"},
                        new CuiRectTransformComponent {AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.99"}
                    }
            });
            var poses = GetPositions(1, clan.Change.Count, 0.01f, 0.01f);

            for (int i = 0; i < clan.Change.Count; i++)
            {
                elements.Add(new CuiElement
                {
                    Name = $"clans_setChange_main{i}",
                    Parent = "clan_setChange1",
                    Components =
                    {
                        new CuiRawImageComponent {Color = "0.3294118 0.3294118 0.3294118 0.1"},
                        new CuiRectTransformComponent {AnchorMin = poses[i].AnchorMin, AnchorMax = poses[i].AnchorMax}
                    }
                });

                elements.Add(new CuiElement
                {
                    Parent = $"clans_setChange_main{i}",
                    Components =
                    {
                        new CuiRawImageComponent {Color = "1 1 1 1", Png = (string)ImageLibrary.Call("GetImage", clan.Change.ToList()[i].Key)},
                        new CuiRectTransformComponent {AnchorMin = "0.05 0", AnchorMax = "0.2 1"}
                    }
                });
                elements.Add(new CuiElement
                {
                    Parent = $"clans_setChange_main{i}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"Установлено:\n{clan.Change.ToList()[i].Value.Need.ToString("N3", CultureInfo.GetCultureInfo("ru-RU")).Replace(",000", "")}".ToUpper(), Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0.3 0", AnchorMax = "0.6 1" },
                    }
                });

                elements.Add(new CuiElement
                {
                    Name = $"clans_setChange_main1{i}",
                    Parent = $"clans_setChange_main{i}",
                    Components =
                    {
                        new CuiRawImageComponent {Color = "0.3294118 0.3294118 0.3294118 0.5"},
                        new CuiRectTransformComponent { AnchorMin = "0.65 0.15", AnchorMax = "0.9 0.85"}
                    }
                });

                elements.Add(new CuiElement()
                {
                    Parent = $"clans_setChange_main1{i}",
                    Components =
                    {
                        new CuiInputFieldComponent {Align = TextAnchor.MiddleCenter,CharsLimit = 7,FontSize = 15,
                            Command = $"clan_Change {clan.Change.ToList()[i].Key} ",Font = "robotocondensed-bold.ttf",Text = "", Color = "1 0.9294118 0.8666667 1"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });


                elements.Add(new CuiButton
                {
                    Button = { Color = "0.25 0.25 0.23 0.9", Command = $"" },
                    Text = { Text = "OK", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                    RectTransform = { AnchorMin = $"0.9 0.15", AnchorMax = $"0.997 0.85" },
                }, $"clans_setChange_main{i}");
            }

            elements.Add(new CuiButton
            {
                Button = { Color = "0.13 0.44 0.48 0.7", Command = $"clanui.page 0" },
                Text = { Text = "ЗАВЕРШИТЬ", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                RectTransform = { AnchorMin = $"0.4 0.03", AnchorMax = $"0.6 0.09" },
            }, $"clan_setChange");
            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("clan_Change")]
        void cmdSetNewChangeOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();

            if (args.GetString(1) == "") return;

            int amount;
            if (!int.TryParse(args.Args[1], out amount)) return;

            var clan = findClanByUser(player.UserIDString);
            if (clan.Change.ContainsKey(args.Args[0]))
            {
                clan.Change[args.Args[0]].Need = (uint)amount;
            }
            cmdSetChangeOfClan(args);
        }


        [ConsoleCommand("clan_setAvatar")]
        void cmdSetAvatarOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, "clans_setAvatar");

            var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "clans_setAvatar",
                Parent = "clans_main2",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0 0 0 0.85",Sprite = "assets/content/ui/ui.background.tile.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
            });

            elements.Add(new CuiElement
            {
                Name = "clans_setAvatar_input",
                Parent = "clans_setAvatar",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0.3294118 0.3294118 0.3294118 0.5"},
                        new CuiRectTransformComponent {AnchorMin = "0.05 0.45", AnchorMax = "0.8 0.55"}
                    }
            });

            elements.Add(new CuiElement
            {
                Parent = "clans_setAvatar_input",
                Components =
                    {
                        new CuiTextComponent { Text = "<size=24>СКОПИРУЙТЕ СЮДА ССЫЛКУ И НАЖМИТЕ СОХРАНИТЬ</size>", Color = "1 0.9294118 0.8666667 0.05", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
            });

            elements.Add(new CuiButton
            {
                Button = { Color = "0.13 0.44 0.48 0.7", Command = $"clanui.page 0" },
                Text = { Text = "ЗАКРЫТЬ", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                RectTransform = { AnchorMin = $"0.3 0.38", AnchorMax = $"0.7 0.44" },
            }, $"clans_setAvatar");

            elements.Add(new CuiElement()
            {
                Parent = "clans_setAvatar_input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        CharsLimit = 80,
                        FontSize = 26,
                        Command = $"clan_changeAvatar ",
                        Font = "robotocondensed-bold.ttf",
                        Text = "",
                        Color = "1 0.9294118 0.8666667 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    }
                }
            });

            elements.Add(new CuiButton
            {
                Button = { Color = "0.25 0.25 0.23 0.9", Command = "" },
                Text = { Text = "СОХРАНИТЬ", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                RectTransform = { AnchorMin = $"1 0", AnchorMax = $"1.2 0.993" },
            }, "clans_setAvatar_input");

            CuiHelper.AddUi(player, elements);
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

            //if (string.IsNullOrEmpty(GetImageUrl(args.Args[0], SkinID))) return;

            //ImageLibrary?.Call("AddImage", GetImageUrl(args.Args[0], SkinID), args.Args[0], SkinID);
            clan.SkinList[args.Args[0]] = SkinID;
            ClanUI(player, 0, true);
        }

        [ConsoleCommand("clan_kickplayer")]
        void cmdKickOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            KickPlayer(player, args.Args[0]);
            ClanUI(player, 0);
        }

        [ConsoleCommand("clan_changeAvatar")]
        void cmdChangeAvatarOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (args.GetString(0) == "" || !args.Args[0].Contains("http") || !args.Args[0].Contains(".png") && !args.Args[0].Contains(".jpg")) return;

            var clan = findClanByUser(player.UserIDString);
            clan.ClanAvatar = args.Args[0];
            ImageLibrary.Call("AddImage", clan.ClanAvatar, clan.ClanAvatar);
            ClanUI(player, 0);
        }

        private Dictionary<uint, ulong> LastHeliHit = new Dictionary<uint, ulong>();

        private BasePlayer GetLastHeliAttacker(uint heliNetId)
        {
            ulong player;
            LastHeliHit.TryGetValue(heliNetId, out player);
            return BasePlayer.FindByID(player);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity?.net?.ID == null) return;
            var player = info?.InitiatorPlayer;
            if (player == null) return;
            var clan = findClanByUser(player.UserIDString);
            if (clan == null) return;
            if (entity.PrefabName.Contains("barrel"))
            {
                if (clan.Change["loot-barrel"].Complete < clan.Change["loot-barrel"].Need && clan.Change["loot-barrel"].Need > 0)
                {
                    clan.Change["loot-barrel"].Complete++;
                    clan.members[player.UserIDString].GatherInfo["loot-barrel"]++;
                }
                clan.members[player.UserIDString].KilledBarrel++;
                clan.members[player.UserIDString].PlayerPoints += PointsOfBarrel;
                clan.ClanPoints += PointsOfBarrel;
            }

            if (entity is BaseHelicopter && GetLastHeliAttacker(entity.net.ID) == player)
            {
                clan.members[player.UserIDString].KilledHeli++;
                clan.ClanPoints += PointsOfKilledHeli;
                clan.members[player.UserIDString].PlayerPoints += PointsOfKilledHeli;
            }

            if (entity.ToPlayer() != null && entity as BasePlayer)
            {
                if (entity.GetComponent<NPCPlayer>() != null) return;
                if (IsNPC(entity.ToPlayer())) return;
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
                    clan.members[player.UserIDString].PlayerPoints += PointsOfKilled;
                }
                if (findClanByUser(entity.ToPlayer().UserIDString) != null)
                {
                    findClanByUser(entity.ToPlayer().UserIDString).ClanPoints -= PointsOfDeath;
                    findClanByUser(entity.ToPlayer().UserIDString).members[entity.ToPlayer().UserIDString].PlayerPoints -= PointsOfDeath;
                    findClanByUser(entity.ToPlayer().UserIDString).members[entity.ToPlayer().UserIDString].Death++;
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

        [PluginReference]
        private Plugin ItemNameLocalizator;

        private bool IsInlReady => (bool?)ItemNameLocalizator?.CallHook("IsReady") == true;
        public bool IsReady() => (bool)ImageLibrary?.Call("IsReady");


        private string GetItemName(string shortname, object player)
        {
            if (!IsInlReady)
                return shortname;
            if (shortname.ToLower() == "loot-barrel") return "бочка";
            string name = ItemNameLocalizator?.CallHook("GetItemName", shortname, player) as string;
            return name ?? shortname;
        }
		
        int GetPercent(int need, int current){
			if (need==0) return 0;
			return current * 100 / need;
		}

        double GetPercentFUll(double need, double current){
			if (need==0) return 0;
			return current * 100 / need;
		}



        string ButtonListedClan = "[{\"name\":\"clans_player{id}\",\"parent\":\"Clanstop_main8\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.001 {amin}\",\"anchormax\":\"0.998 {amax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"clans_player{id}\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"clans_player{id}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        string TOP = "[{\"name\":\"Clanstop_main2\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0.6980392\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main3\",\"parent\":\"Clanstop_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0.1647059\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2 0.1546297\",\"anchormax\":\"0.8 0.8935185\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main4\",\"parent\":\"Clanstop_main3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.9385965\",\"anchormax\":\"0.9953553 0.9955974\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main5\",\"parent\":\"Clanstop_main4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>{title}</b>\",\"fontSize\":18,\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main6\",\"parent\":\"Clanstop_main5\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"Clanstop_main2\",\"color\":\"0.5254902 0.282353 0.2313726 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.935175 0\",\"anchormax\":\"0.9986773 0.94\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main7\",\"parent\":\"Clanstop_main6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>X</b>\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main8\",\"parent\":\"Clanstop_main3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.005012453\",\"anchormax\":\"0.9953553 0.8734336\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main9\",\"parent\":\"Clanstop_main3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548413 0.8809524\",\"anchormax\":\"0.9953553 0.9310777\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_main10\",\"parent\":\"Clanstop_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"A PLACE\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.03405698 0\",\"anchormax\":\"0.1355833 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main11\",\"parent\":\"Clanstop_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"NAME\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1329573 0\",\"anchormax\":\"0.6983538 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main12\",\"parent\":\"Clanstop_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"GLASSES\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7298629 0\",\"anchormax\":\"0.8270121 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main12\",\"parent\":\"Clanstop_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"PLAYERS\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8585205 0\",\"anchormax\":\"0.9985565 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";

        string InfoTOP = "[{\"name\":\"Clanstop_info2\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0.6980392\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info3\",\"parent\":\"Clanstop_info2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0.1647059\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2 0.1546297\",\"anchormax\":\"0.8 0.8935185\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info4\",\"parent\":\"Clanstop_info3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.9385965\",\"anchormax\":\"0.9953553 0.9955974\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info5\",\"parent\":\"Clanstop_info4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>{clanname}</b> PLACE IN THE RANKING: {RANK}\",\"fontSize\":18,\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info6\",\"parent\":\"Clanstop_info5\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"Clanstop_info2\",\"color\":\"0.5254902 0.282353 0.2313726 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.935175 0\",\"anchormax\":\"0.9986773 0.94\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info7\",\"parent\":\"Clanstop_info6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>X</b>\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info8\",\"parent\":\"Clanstop_info5\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{return}\",\"color\":\"0.2175973 0.2175973 0.2175973 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0004332103 0\",\"anchormax\":\"0.1224549 0.93\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info9\",\"parent\":\"Clanstop_info8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>НАЗАД</b>\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info10\",\"parent\":\"Clanstop_info3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.005012453\",\"anchormax\":\"0.9953553 0.7481203\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info11\",\"parent\":\"Clanstop_info10\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.9173692\",\"anchormax\":\"1 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main10\",\"parent\":\"Clanstop_info11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Name Players</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.2528636 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main10\",\"parent\":\"Clanstop_info11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>СТАТУС</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2449865 0\",\"anchormax\":\"0.4594171 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main10\",\"parent\":\"Clanstop_info11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>GLASSES</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4576665 0\",\"anchormax\":\"0.6055799 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main10\",\"parent\":\"Clanstop_info11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Norm в %</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6073302 0\",\"anchormax\":\"0.7771242 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_main10\",\"parent\":\"Clanstop_info11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>УБИЙСТВ/СМЕРТЕЙ</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7841259 0\",\"anchormax\":\"0.9985565 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_info12\",\"parent\":\"Clanstop_info10\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.01011801\",\"anchormax\":\"1 0.9123102\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_info13\",\"parent\":\"Clanstop_info3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548413 0.755639\",\"anchormax\":\"0.9953553 0.9310777\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Clanstop_info14\",\"parent\":\"Clanstop_info13\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6318366 0.1428573\",\"anchormax\":\"0.9670526 0.8928576\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_info15\",\"parent\":\"Clanstop_info14\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>ВСЕГО ОЧКОВ: {Points}\n\nЛИДЕР КЛАНА: {lname}</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_info16\",\"parent\":\"Clanstop_info13\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2187297 0.1428573\",\"anchormax\":\"0.5644442 0.8928576\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_info17\",\"parent\":\"Clanstop_info16\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Clan players: {ccount}</b>\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Clanstop_info18\",\"parent\":\"Clanstop_info13\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.03318176 0.07857105\",\"anchormax\":\"0.1382089 0.9357135\",\"offsetmax\":\"0 0\"}]}]";

        string InfoTOPButton = "[{\"name\":\"clans_player{id}\",\"parent\":\"Clanstop_info12\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.001 {amin}\",\"anchormax\":\"0.998 {amax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"clans_player{id}\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"clans_player{id}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

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
            CuiHelper.DestroyUi(player, "Clanstop_main2");
            CuiHelper.DestroyUi(player, "Clanstop_info2");

            CuiHelper.AddUi(player, InfoTOP.Replace("{clanname}", "КЛАН: " + clan.tag.ToUpper() + " | ")
                .Replace("{RANK}", GetClanIndex(clan.tag).ToString())
                 .Replace("{ccount}", clan.members.Count.ToString())
                  .Replace("{ocount}", clan.online.ToString())
                   .Replace("{Points}", clan.ClanPoints.ToString())
                   .Replace("{lname}", clan.ownerName.ToUpper())
                   .Replace("{avatar}", (string)ImageLibrary.Call("GetImage", clan.ClanAvatar))
                    .Replace("{return}", "clanstop_main")
                );
            var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Parent = $"Clanstop_info13",
                Components =
                    {
                        new CuiRawImageComponent {Color = "1 1 1 0.5", Png = (string)ImageLibrary?.Call("GetImage", clan.ClanAvatar)},
                        new CuiRectTransformComponent {AnchorMin = "0.03318176 0.07857105", AnchorMax = "0.1382089 0.9357135"}
                    }
            });
            string colored = "0.25 0.25 0.23 0.5";
            double Amin = 0.92;
            double Amax = 0.995;
            int i = 1+(page*10);
            var ClanMembers = from pair in clan.members orderby pair.Value.PlayerPoints descending select pair;
            foreach (KeyValuePair<string, PlayerStats> key in ClanMembers.Skip(11 * page).Take(ClanMembers.ToList().Count >= 10 ? 10 : ClanMembers.ToList().Count))
            {

                CuiHelper.AddUi(player, InfoTOPButton.Replace("{amin}", Amin.ToString()).Replace("{amax}", Amax.ToString()).Replace("{color}", colored).Replace("{id}", key.Key));
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{i}", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.05 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = covalence.Players.FindPlayerById(key.Key) != null ? covalence.Players.FindPlayerById(key.Key).Name : "Имя не указано", Font = "robotocondensed-bold.ttf", FontSize = 14, Color = "1 0.9294118 0.8666667 1", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.2528636 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = GetPlayerStatus(key.Key, clan), Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.2449865 0", AnchorMax = $"0.4594171 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = key.Value.PlayerPoints.ToString(), Color = "1 0.9294118 0.8666667 1", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.4576665 0", AnchorMax = $"0.6055799 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{GetFullPercent(key.Key)}%", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.6073302 0", AnchorMax = $"0.7771242 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.Killed}/{key.Value.Death}", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.7841259 0", AnchorMax = $"0.9985565 1" },
                }, "clans_player" + key.Key);
                i++;
				
                Amin = Amin - 0.085;
                Amax = Amax - 0.085;

            }
            elements.Add(new CuiButton
            {
                Button = { Color = "0.25 0.25 0.23 0.9" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0.01 0", AnchorMax = $"0.1 0.048" },
            }, "Clanstop_info12", "clans_page");
            elements.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.3921569 0.372549 0.7", Command = page > 0 ? $"clanstop_info {clan.tag} {page - 1}" : "" },
                Text = { Text = "<", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.33 0.997" },
            }, "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.3294118 0.3294118 0.3294118 0" },
                Text = { Text = $"{page + 1}", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.33 0", AnchorMax = $"0.66 0.997" },
            }, "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.3921569 0.372549 0.7", Command = ClanMembers.Skip(11 * (page + 1)).Count() > 0 ? $"clanstop_info {page + 1}" : "" },
                Text = { Text = ">", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.66 0", AnchorMax = $"0.997 0.997" },
            }, "clans_page");
            CuiHelper.AddUi(player, elements);

        }

        void ClanTOP(BasePlayer player, int page)
        {			
            CuiHelper.DestroyUi(player, "Clanstop_info2");
            CuiHelper.DestroyUi(player, "Clanstop_main2");
            CuiHelper.AddUi(player, TOP.Replace("{title}", msg("clanTOPUItitle", player.UserIDString).ToUpper()));
            string colored = "0.25 0.25 0.23 0.5";
            var elements = new CuiElementContainer();
            double Amin = 0.93;
            double Amax = 0.995;
            int i = 1;
            var ClanMembers = from pair in clans orderby pair.Value.ClanPoints descending select pair;

            foreach (KeyValuePair<string, Clan> key in ClanMembers.Skip(10 * page).Take(ClanMembers.ToList().Count >= 10 ? 10 : ClanMembers.ToList().Count))
            {
                CuiHelper.AddUi(player, ButtonListedClan.Replace("{amin}", Amin.ToString()).Replace("{amax}", Amax.ToString()).Replace("{color}", colored).Replace("{id}", key.Key));
                elements.Add(new CuiElement
                {
                    Parent = $"clans_player{key.Key}",
                    Components =
                    {
                        new CuiRawImageComponent {Color = "1 1 1 0.5", Png = (string)ImageLibrary?.Call("GetImage", key.Value.ClanAvatar)},
                        new CuiRectTransformComponent {AnchorMin = "0.01 0.1", AnchorMax = "0.04 0.9"}
                    }
                });

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{i}", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.08 0", AnchorMax = $"0.1355833 1" },
                }, "clans_player" + key.Key);

                elements.Add(new CuiLabel
                {
                    Text = { Text = key.Value.tag.ToUpper(), Font = "robotocondensed-bold.ttf", FontSize = 14, Color = "1 0.9294118 0.8666667 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.1329573 0", AnchorMax = $"0.6983538 1" },
                }, "clans_player" + key.Key);

                elements.Add(new CuiLabel
                {
                    Text = { Text = key.Value.ClanPoints.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 14, Color = "1 0.9294118 0.8666667 1", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.7298629 0", AnchorMax = $"0.8270121 1" },
                }, "clans_player" + key.Key);

                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.online}/{key.Value.members.Count}".ToUpper(), Font = "robotocondensed-bold.ttf", FontSize = 14, Color = "1 0.9294118 0.8666667 1", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.8585205 0", AnchorMax = $"0.9985565 1" },
                }, "clans_player" + key.Key);

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.25 0.25 0.23 0", Command = $"clanstop_info {key.Value.tag}" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "clans_player" + key.Key);
                i++;
                Amin = Amin - 0.073;
                Amax = Amax - 0.073;
            }
            elements.Add(new CuiButton
            {
                Button = { Color = "0.25 0.25 0.23 0.9" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0.01 0.003", AnchorMax = $"0.1 0.048" },
            }, "Clanstop_main8", "clans_page");
            elements.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.3921569 0.372549 0.7", Command = page > 0 ? $"clanstop_main {page - 1}" : "" },
                Text = { Text = "<", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.33 0.997" },
            }, "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.3294118 0.3294118 0.3294118 0" },
                Text = { Text = $"{page + 1}", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.33 0", AnchorMax = $"0.66 0.997" },
            }, "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.3921569 0.372549 0.7", Command = clans.Keys.Skip(10 * (page + 1)).Count() > 0 ? $"clanstop_main {page + 1}" : "" },
                Text = { Text = ">", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.66 0", AnchorMax = $"0.997 0.997" },
            }, "clans_page");
            CuiHelper.AddUi(player, elements);
        }

        void ClanUI(BasePlayer player, int page = 0, bool changeSkin = false, bool changeResource = false)
        {
            //if (!IsReady())
            //{
            //    timer.Once(2f, () => ClanUI(player, page, changeSkin, changeResource));
            //    return;
            //}
            CuiHelper.DestroyUi(player, "clans_main1");
            string Main = "[{\"name\":\"clans_main1\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"0 0 0 0.7\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main2\",\"parent\":\"clans_main1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0.1647059\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.17 0.1546297\",\"anchormax\":\"0.83 0.8935185\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main3\",\"parent\":\"clans_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.943609\",\"anchormax\":\"0.9953553 0.9955974\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main4\",\"parent\":\"clans_main3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>{title}</b>\",\"fontSize\":18,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main5\",\"parent\":\"clans_main4\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"clans_main1\",\"color\":\"0.5254902 0.282353 0.2313726 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.935175 0\",\"anchormax\":\"0.9986773 0.94\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main6\",\"parent\":\"clans_main5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>X</b>\",\"fontSize\":18,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main10\",\"parent\":\"clans_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.6918138\",\"anchormax\":\"0.6431664 0.9398496\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanLogo\",\"parent\":\"clans_main10\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05315594 0.02741549\",\"anchormax\":\"0.2838709 0.9721818\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanInfo\",\"parent\":\"clans_main10\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2745098 0.2745098 0.2745098 0.3948693\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3418818 0.02741549\",\"anchormax\":\"0.9542304 0.9754673\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName\",\"parent\":\"ClanInfo\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7517123\",\"anchormax\":\"1 0.9931507\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName1\",\"parent\":\"ClanName\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"NAME CLANS: \",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0221675 0\",\"anchormax\":\"0.556291 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clanTitle\",\"parent\":\"ClanName\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>{Title}</b>\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleRight\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5141813 0\",\"anchormax\":\"0.9929016 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName2\",\"parent\":\"ClanInfo\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.5034246\",\"anchormax\":\"1 0.744863\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName3\",\"parent\":\"ClanName2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Head of the clan:\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0221675 0\",\"anchormax\":\"0.4698553 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clanTitle\",\"parent\":\"ClanName2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{LeaderName}\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleRight\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5141813 0\",\"anchormax\":\"0.9929016 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName3\",\"parent\":\"ClanInfo\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.255137\",\"anchormax\":\"1 0.4965754\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName4\",\"parent\":\"ClanName3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Clan players:\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8705882 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0221675 0\",\"anchormax\":\"0.5718051 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clanTitle\",\"parent\":\"ClanName3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{O/P}\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleRight\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5141813 0\",\"anchormax\":\"0.9929016 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName3\",\"parent\":\"ClanInfo\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.372549 0.3843137\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.006849289\",\"anchormax\":\"1 0.2482877\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ClanName4\",\"parent\":\"ClanName3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Place in the top:\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8705882 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0221675 0\",\"anchormax\":\"0.6493754 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clanTitle\",\"parent\":\"ClanName3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{per}\",\"fontSize\":16,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleRight\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5141813 0\",\"anchormax\":\"0.9929016 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main11\",\"parent\":\"clans_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6467149 0.6918139\",\"anchormax\":\"0.9980168 0.9398496\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main112\",\"parent\":\"clans_main11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Task list</b>\",\"fontSize\":15,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7554934\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main113\",\"parent\":\"clans_main11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>{attach}</b>\",\"fontSize\":15,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.02572244\",\"anchormax\":\"1 0.7554934\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_main9\",\"parent\":\"clans_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548518 0.4335839\",\"anchormax\":\"0.9971297 0.6859453\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main91\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Clan clothing</b>\",\"fontSize\":15,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"clans_item0\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.06081926 0.04293609\",\"anchormax\":\"0.1799577 0.7877806\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_item1\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1871024 0.04293609\",\"anchormax\":\"0.3062414 0.7877806\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_item2\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3133879 0.04293609\",\"anchormax\":\"0.4325227 0.7877806\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_item3\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4396606 0.04293609\",\"anchormax\":\"0.558799 0.7877806\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_item4\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5659431 0.04293609\",\"anchormax\":\"0.6850816 0.7877806\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_item5\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6922278 0.04293609\",\"anchormax\":\"0.8113656 0.7877806\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_item6\",\"parent\":\"clans_main9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3921569 0.3686275 0.5568628\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.81852 0.04293605\",\"anchormax\":\"0.9376571 0.7877805\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main7\",\"parent\":\"clans_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.003548417 0.006466299\",\"anchormax\":\"0.6431664 0.4285714\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main71\",\"parent\":\"clans_main7\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.911024\",\"anchormax\":\"1 0.9998377\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main722\",\"parent\":\"clans_main71\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"*\",\"fontSize\":15,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.06366677 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main723\",\"parent\":\"clans_main71\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Name Players\",\"fontSize\":13,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.06366677 0\",\"anchormax\":\"0.4043106 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main724\",\"parent\":\"clans_main71\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Activity\",\"fontSize\":13,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4056678 0\",\"anchormax\":\"0.6526684 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main725\",\"parent\":\"clans_main71\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Norm\",\"fontSize\":13,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6526684 0\",\"anchormax\":\"0.75 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main726\",\"parent\":\"clans_main71\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"actions\",\"fontSize\":13,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main8\",\"parent\":\"clans_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3294118 0.3294118 0.3294118 0.7058824\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6467149 0.006466303\",\"anchormax\":\"0.9980168 0.4285714\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"clans_main81\",\"parent\":\"clans_main8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Mining</b>\",\"fontSize\":15,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 0.9294118 0.8666667 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8873403\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";
            var Messages = "";

            var clan = findClanByUser(player.UserIDString);
            var need = clan.Change.Select(p => $"{GetItemName(p.Key, player)} - {p.Value.Need.ToString("N3", CultureInfo.GetCultureInfo("ru-RU")).Replace(",000", "")}").ToList();

            if (need.Count > 0) Messages = string.Join(", ", need);		
			
            CuiHelper.AddUi(player,
                Main
                .Replace("{Title}", clan.tag.ToUpper())
                .Replace("{LeaderName}", clan.ownerName.ToUpper())
                .Replace("{O/P}", $"{clan.online}/{clan.members.Count}".ToUpper())
                .Replace("{per}", GetClanIndex(clan.tag).ToString())
                .Replace("{attach}", Messages.ToUpper())
                 .Replace("{title}", msg("clanUItitle", player.UserIDString).ToUpper())
                );


            string colored = "0.25 0.25 0.23 0.5";
            var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "ClanLogo",
                Parent = $"clans_main10",
                Components =
                    {
                        new CuiRawImageComponent { Png = (string)ImageLibrary?.Call("GetImage", clan.ClanAvatar)},
                        new CuiRectTransformComponent { AnchorMin = "0.05315594 0.02741549", AnchorMax = "0.2838709 0.9721818" }
                    }
            });
            double Amin = 0.8;
            double Amax = 0.9;
			
            foreach (var key in clan.members.OrderBy(pair => BasePlayer.FindByID(ulong.Parse(pair.Key)) == null).Skip(8 * page).Take(clan.members.Count >= 8 ? 8 : clan.members.Count))
            {
                var playerKey = BasePlayer.FindByID(ulong.Parse(key.Key));
                CuiHelper.AddUi(player, ButtonListed.Replace("{amin}", Amin.ToString()).Replace("{amax}", Amax.ToString()).Replace("{color}", colored).Replace("{id}", key.Key));
                elements.Add(new CuiLabel
                {
                    Text = { Text = "●", Color = playerKey != null ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.06366677 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = covalence.Players.FindPlayerById(key.Key) != null ? covalence.Players.FindPlayerById(key.Key).Name : "Имя не указано", Font = "robotocondensed-bold.ttf", FontSize = 14, Color = "1 0.9294118 0.8666667 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.06366677 0", AnchorMax = $"0.4043106 1" },
                }, "clans_player" + key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = playerKey != null ? FormatShortTime(TimeSpan.FromSeconds(playerKey.TimeAlive())) : "0", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.4056678 0", AnchorMax = $"0.6526684 1" },
                }, "clans_player" + key.Key);

                var persent = GetFullPercent(key.Key) > 100 ? 100 : GetFullPercent(key.Key);
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{persent}%", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.6526684 0", AnchorMax = $"0.814169 1" },
                }, "clans_player" + key.Key);
                Amin = Amin - 0.1;
                Amax = Amax - 0.1;
                if (clan.owner == player.UserIDString)
                {
                    if (key.Key != player.UserIDString)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "0.25 0.25 0.23 0.9", Command = $"clan_kickplayer {key.Key}" },
                            Text = { Text = "ВЫГНАТЬ", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                            RectTransform = { AnchorMin = $"0.91 0", AnchorMax = $"0.997 0.955" },
                        }, "clans_player" + key.Key);

                        elements.Add(new CuiButton
                        {
                            Button = { Color = "0.25 0.25 0.23 0.9", Command = clan.moderators.Contains(key.Key) ? $"clanui_promote demote {key.Key}" : $"clanui_promote promote {key.Key}" },
                            Text = { Text = clan.moderators.Contains(key.Key) ? "УБРАТЬ МОДЕРАТОРА" : "НАЗНАЧИТЬ МОДЕРАТОРОМ", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                            RectTransform = { AnchorMin = $"0.75 0", AnchorMax = $"0.905 0.955" },
                        }, "clans_player" + key.Key);
                    }
                }
                else if (clan.moderators.Contains(player.UserIDString))
                {
                    elements.Add(new CuiButton
                    {
                        Button = { Color = "0.25 0.25 0.23 0.9", Command = $"clan_kickplayer {key.Key}" },
                        Text = { Text = "ВЫГНАТЬ", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf", Color = "1 0.9294118 0.8666667 1" },
                        RectTransform = { AnchorMin = $"0.91 0", AnchorMax = $"0.997 0.955" },
                    }, "clans_player" + key.Key);
                }
                else
                    CuiHelper.DestroyUi(player, "clans_main726");
            }

            elements.Add(new CuiButton
            {
                Button = { Color = "0.25 0.25 0.23 0.9" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = $"0.01 0.005", AnchorMax = $"0.15 0.085" },
            }, "clans_main7", "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.3921569 0.372549 0.7", Command = page > 0 ? $"clanui.page {page - 1}" : "" },
                Text = { Text = "<", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.33 0.997" },
            }, "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.3294118 0.3294118 0.3294118 0" },
                Text = { Text = $"{page + 1}", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.33 0", AnchorMax = $"0.66 0.997" },
            }, "clans_page");

            elements.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.3921569 0.372549 0.7", Command = clan.members.Skip(8 * (page + 1)).Count() > 0 ? $"clanui.page {page + 1}" : "" },
                Text = { Text = ">", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.66 0", AnchorMax = $"0.997 0.997" },
            }, "clans_page");

            for (int i = 0; i < clan.SkinList.Count; i++)
            {
                elements.Add(new CuiElement
                {
                    Parent = "clans_item" + i,
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImageSkin(clan.SkinList.ToList()[i].Key, clan.SkinList.ToList()[i].Value) },
                        new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                    }
                });

                if (changeSkin)
                    elements.Add(new CuiButton
                    {
                        Button = { Color = "0.13 0.44 0.48 0", Command = changeSkin ? $"clans_getskinIds {clan.SkinList.ToList()[i].Key}" : $"" },
                        Text = { Text = "", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    }, "clans_item" + i);
            }
            var poses = GetPositions(3, 3, 0.08f, 0.03f);

            elements.Add(new CuiElement
            {
                Name = $"clans_main88",
                Parent = "clans_main8",
                Components =
                    {
                        new CuiRawImageComponent {Color = "0.3294118 0.3294118 0.3294118 0"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 0.9"}
                    }
            });
			
            for (int i = 0; i < clan.Change.Count; i++)
            {
                elements.Add(new CuiElement
                {
                    Name = $"clans_main88{clan.Change.ToList()[i].Key}",
                    Parent = $"clans_main88",
                    Components =
                    {
                        new CuiRawImageComponent {Color = "0.3294118 0.3294118 0.3294118 0.7"},
                        new CuiRectTransformComponent {AnchorMin = poses[i].AnchorMin, AnchorMax = poses[i].AnchorMax}
                    }
                });

                var Anchor = GetPercentFUll(clan.Change.ToList()[i].Value.Need, clan.Change.ToList()[i].Value.Complete) / 100 > 1 ?
                    1.0 :
                    GetPercentFUll(clan.Change.ToList()[i].Value.Need, clan.Change.ToList()[i].Value.Complete) / 100;

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.13 0.44 0.48 1", Command = "" },
                    Text = { Text = $"", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 {Anchor}" },
                }, $"clans_main88{clan.Change.ToList()[i].Key}");

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.13 0.44 0.48 0", Command = "" },
                    Text = { Text = $"{GetPercent((int)clan.Change.ToList()[i].Value.Need, (int)clan.Change.ToList()[i].Value.Complete)}%", Color = "1 0.9294118 0.8666667 1", FontSize = 14, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0.6 0.7", AnchorMax = "1 1" },
                }, $"clans_main88{clan.Change.ToList()[i].Key}");


                elements.Add(new CuiElement
                {
                    Parent = $"clans_main88{clan.Change.ToList()[i].Key}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", clan.Change.ToList()[i].Key)},
                        new CuiRectTransformComponent { AnchorMin = "0.2 0.1", AnchorMax = "0.8 0.9" }
                    }
                });
            }

            if (clan.owner == player.UserIDString)
            {
                elements.Add(new CuiButton
                {
                    Button = { Color = "0.13 0.44 0.48 0.7", Command = changeSkin ? $"clanui.page {page}" : $"clanui.page {page} true" },
                    Text = { Text = changeSkin ? "Save" : "Tune", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.9 0", AnchorMax = $"0.997 0.95" },
                }, "clans_main91");

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.13 0.44 0.48 0.7", Command = $"clan_setChange" },
                    Text = { Text = "Tune", Color = "1 0.9294118 0.8666667 1", FontSize = 13, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.75 0.2", AnchorMax = $"0.993 0.93" },
                }, "clans_main112");

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.13 0.44 0.48 0.5", Command = $"clan_setAvatar" },
                    Text = { Text = "Edit", Color = "1 0.9294118 0.8666667 1", FontSize = 12, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.55 0.85", AnchorMax = $"1 1" },
                }, "ClanLogo");
            }

            if (changeSkin)
            {
                elements.Add(new CuiElement
                {
                    Name = "clans_main100",
                    Parent = "clans_main2",
                    Components =
                    {
                        new CuiImageComponent {Color = "0 0 0 0.85"},
                        new CuiRectTransformComponent {AnchorMin = "0 0.6917294", AnchorMax = "1 0.7619048"}
                    }
                });

                elements.Add(new CuiElement
                {
                    Parent = "clans_main100",
                    Components =
                    {
                        new CuiTextComponent { Text = "<size=26>SKIN CONTROL PANEL</size>", Color = "1 0.9294118 0.8666667 1", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
                });

                elements.Add(new CuiButton
                {
                    Button = { Color = "0.4039216 0.3921569 0.372549 0.9", Command = "", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "clans_main10");
                elements.Add(new CuiButton
                {
                    Button = { Color = "0.4039216 0.3921569 0.372549 0.9", Command = "", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "clans_main11");
                elements.Add(new CuiButton
                {
                    Button = { Color = "0.4039216 0.3921569 0.372549 0.9", Command = "", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "clans_main7");
                elements.Add(new CuiButton
                {
                    Button = { Color = "0.4039216 0.3921569 0.372549 0.9", Command = "", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "", Color = "1 0.9294118 0.8666667 1", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, "clans_main8");

            }
            CuiHelper.AddUi(player, elements);
        }

        long GetFullPercent(string id)
        {
            var clan = findClanByUser(id);
            long Need = clan.Change.Sum(x => x.Value.Need);
            var playerCurrent = clan.members[id].GatherInfo.Sum(p => p.Value);
            if (playerCurrent == 0) return 0;
			if (Need==0) return 0;
            return playerCurrent * 100 / Need;
        }

        long GetFullClanPercent(string tag)
        {
            var clan = findClan(tag);
            long Need = clan.Change.Sum(x => x.Value.Need);
            var Current = clan.Change.Sum(x => x.Value.Complete);
            if (Current == 0) return 0;
			if (Need==0) return 0;
            return Current * 100 / Need;
        }

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{time.Days} д. ";

            if (time.Hours != 0)
                result += $"{time.Hours} ч. ";

            if (time.Minutes != 0)
                result += $"{time.Minutes} м. ";

            if (time.Seconds != 0)
                result += $"{time.Seconds} с. ";

            return result;
        }

        public void WithdrawPlayer(BasePlayer player, string targetId) => cmdClanWithdraw(player, new string[] {
"", targetId
}
        );

        void cmdClanWithdraw(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                PrintChat(player, string.Format(msg("notmember", current.Id)));
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
            setupPlayer(player, current.Name, current.Id);
            if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("playerjoined", myClan.ColNam(current.Id, current.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.total++;
            myClan.onUpdate();
            List<string> others = new List<string>(myClan.members.Keys);
            others.Remove(current.Id);
            Interface.Oxide.CallHook("OnClanMemberJoined", current.Id, others);
        }


        [ConsoleCommand("clanui_promote")]
        void cmdClanPromoteMember(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            switch (args.Args[0].ToLower())
            {
                case "promote":
                    PromotePlayer(player, args.Args[1]);
                    break;
                case "demote":
                    DemotePlayer(player, args.Args[1]);
                    break;
            }
            ClanUI(player, 0);

        }

        public void PromotePlayer(BasePlayer player, string targetId) => cmdClanPromote(player, new string[] { "", targetId });

        void cmdClanPromote(BasePlayer player, string[] args)
        {
            var current = this.covalence.Players.FindPlayerById(player.UserIDString);
            var myClan = findClanByUser(current.Id);
            if (myClan == null)
            {
                PrintChat(player, string.Format(msg("notmember", current.Id)));
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
            myClan.onUpdate();
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
                PrintChat(player, string.Format(msg("notmember", current.Id)));
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
            myClan.onUpdate();
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
                PrintChat(player, string.Format(msg("notmember", current.Id)));
                return;
            }
            if (args.Length != 1)
            {
                PrintChat(player, string.Format(msg("usageleave", current.Id), colorCmdUsage));
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
            setupPlayer(player, current.Name, current.Id);
            if (usePermGroups && permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup(current.Id, permGroupPrefix + myClan.tag);
            RemoveRadar(player, myClan.tag, true);
            PrintChat(player, string.Format(msg("youleft", current.Id)));
            myClan.BroadcastLoc("playerleft", myClan.ColNam(current.Id, current.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.total--;
            myClan.onUpdate();
            if (lastMember) myClan.onDestroy();
            if (!lastMember) Interface.Oxide.CallHook("OnClanMemberGone", current.Id, myClan.members.ToList());
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
                PrintChat(player, string.Format(msg("notmember", current.Id)));
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
            var kickBasePlayer = rust.FindPlayerByIdString(kickPlayer.Id);
            if (kickBasePlayer != null)
            {
                setupPlayer(kickBasePlayer, kickPlayer.Name, kickPlayer.Id);
                RemoveRadar(kickBasePlayer, myClan.tag, true);
            }
            if (usePermGroups && permission.UserHasGroup(kickPlayer.Id, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup(kickPlayer.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("waskicked", myClan.ColNam(current.Id, current.Name), myClan.ColNam(kickPlayer.Id, kickPlayer.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.onUpdate();
            Interface.Oxide.CallHook("OnClanMemberGone", kickPlayer.Id, myClan.members);
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
                PrintChat(player, string.Format(msg("notmember", current.Id)));
                return;
            }
            if (!myClan.IsOwner(current.Id))
            {
                PrintChat(player, string.Format(msg("needclanowner", current.Id)));
                return;
            }
            if (args.Length != 2)
            {
                PrintChat(player, string.Format(msg("usagedisband", current.Id), colorCmdUsage));
                return;
            }
            if (myClan.members.Count() == 1)
            {
                lastMember = true;
            }
            RemoveRadarGroup(myClan.members.Keys.ToList(), myClan.tag, true);
            RemoveClan(myClan.tag);
            foreach (var member in myClan.members)
            {
                clanCache.Remove(member.Key);
                if (usePermGroups && permission.UserHasGroup((string)member.Key, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup((string)member.Key, permGroupPrefix + myClan.tag);
            }
            myClan.BroadcastLoc("clandisbanded");
            setupPlayers(myClan.members.Keys.ToList());
            foreach (var ally in clans)
            {
                Clan allyClan = clans[ally.Key];
                allyClan.clanAlliances.Remove(myClan.tag);
                allyClan.invitedAllies.Remove(myClan.tag);
                allyClan.pendingInvites.Remove(myClan.tag);
            }
            if (usePermGroups && permission.GroupExists(permGroupPrefix + myClan.tag)) permission.RemoveGroup(permGroupPrefix + myClan.tag);
            myClan.onDestroy();
            AllyRemovalCheck();
            if (!lastMember) Interface.Oxide.CallHook("OnClanDisbanded", myClan.members);
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
                PrintChat(player, string.Format(msg("notmember", current.Id)));
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
                    myClan.onUpdate();
                    targetClan.onUpdate();
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
                    myClan.onUpdate();
                    targetClan.onUpdate();
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
                    myClan.onUpdate();
                    targetClan.onUpdate();
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
                            myClan.onUpdate();
                            targetClan.onUpdate();
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
                    myClan.onUpdate();
                    targetClan.onUpdate();
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
            if ((enableClanRadar && !usePermissionClanRadar) || enableClanRadar && usePermissionClanRadar && permission.UserHasPermission(current.Id, permissionClanRadarUse)) sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} {msg("clanArgRadar", current.Id)} | /{chatCommandRadar}</color> - {msg("helptoggleradar", current.Id)}");
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
            if (player.net.connection.authLevel >= authLevelDelete || player.net.connection.authLevel >= authLevelRename || player.net.connection.authLevel >= authLevelInvite || player.net.connection.authLevel >= authLevelKick || player.net.connection.authLevel >= authLevelPromoteDemote) sb.AppendLine($"<color={clanServerColor}>Server management</color>: {msg("helpconsole", current.Id)} <color={colorCmdUsage}>clans</color>");
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
        void cmdChatClanchat(BasePlayer player, string command, string[] args)
        {
            if (player == null || args.Length == 0) return;
            var myClan = findClanByUser(player.UserIDString);
            if (myClan == null)
            {
                SendReply(player, string.Format(msg("notmember", player.UserIDString)));
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
            myClan.BroadcastChat(string.Format(msg("broadcastformat"), myClan.PlayerColor(player.UserIDString), player.net.connection.username, message));
            if (ConVar.Chat.serverlog) DebugEx.Log(string.Format("[CHAT] CLAN [{0}] - {1}: {2}", myClan.tag, player.net.connection.username, message), StackTraceLogType.None);
        }
        void cmdChatAllychat(BasePlayer player, string command, string[] args)
        {
            if (player == null || args.Length == 0) return;
            var myClan = findClanByUser(player.UserIDString);
            if (myClan == null)
            {
                PrintChat(player, string.Format(msg("notmember", player.UserIDString)));
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
                PrintChat(player, string.Format(msg("notmember", player.UserIDString)));
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
        public bool HasFFEnabled(ulong playerId) => !enableFFOPtion ? false : !manuallyEnabledBy.Contains(playerId) ? false : true;
        public void ToggleFF(ulong playerId)
        {
            if (manuallyEnabledBy.Contains(playerId)) manuallyEnabledBy.Remove(playerId);
            else manuallyEnabledBy.Add(playerId);
        }
        void cmdChatClanRadar(BasePlayer player, string command, string[] args)
        {
            if (!enableClanRadar || player == null || (usePermissionClanRadar && !permission.UserHasPermission(player.UserIDString, permissionClanRadarUse))) return;
            var myClan = findClanByUser(player.UserIDString);
            if (myClan == null)
            {
                PrintChat(player, string.Format(msg("notmember", player.UserIDString)));
                return;
            }
            if (player.GetComponent<ClanRadar>())
            {
                GameObject.Destroy(player.GetComponent<ClanRadar>());
                PrintChat(player, string.Format(msg("clanradardisabled", player.UserIDString)));
                activeRadarUsers.Remove(player.UserIDString);
                return;
            }
            ClanRadar radar = player.transform.GetOrAddComponent<ClanRadar>();
            radar.DoStart();
            PrintChat(player, string.Format(msg("clanradarenabled", player.UserIDString)));
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
            public uint Need { get; set; }
            public uint Complete { get; set; }
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
            public int KilledHeli = 0;
            public int GatherStone = 0;
            public int GatherSulfur = 0;
            public int GatherMetal = 0;
            public int GatherHQM = 0;
            public int GatherWood = 0;
            public int KilledBarrel = 0;
            public Dictionary<string, int> GatherInfo = new Dictionary<string, int>()
            {
                 {"wood", 0},
                         {"metal.ore", 0 },
                         {"stones", 0 },
                         {"sulfur.ore", 0 },
                         {"fat.animal", 0 },
                         {"cloth", 0 },
                         {"leather", 0 },
                         {"hq.metal.ore", 0 },
                         {"loot-barrel", 0 },


            };
        }


        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        public class Clan
        {
            public int ClanPoints = 0;
            public string tag;
            public string description;
            public string owner;
            public string ownerName;
            public string ClanAvatar;
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
            //Custom code
            public Dictionary<string, ulong> SkinList = new Dictionary<string, ulong>();
            public Dictionary<string, ChangeListed> Change = new Dictionary<string, ChangeListed>()
            {
                ["wood"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 1000
                },
                ["metal.ore"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 1000
                },
                ["stones"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 1000
                },
                ["sulfur.ore"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 1000
                },
                ["fat.animal"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 300
                },
                ["cloth"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 300
                },
                ["leather"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 100
                },
                ["hq.metal.ore"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 500
                },
                ["loot-barrel"] = new ChangeListed()
                {
                    Complete = 0,
                    Need = 20
                },
            };

            public static Clan Create(string tag, string description, string ownerId, string owName, string URL)
            {
                var clan = new Clan()
                {
                    ClanPoints = 0,
                    tag = tag,
                    description = description,
                    owner = ownerId,
                    ClanAvatar = URL,
                    ownerName = owName,
                    created = cc.UnixTimeStampUTC(),
                    updated = cc.UnixTimeStampUTC(),
                    SkinList = new Dictionary<string, ulong>()
                    {
                         {"metal.facemask", 0},
                         {"metal.plate.torso", 0 },
                         {"burlap.gloves", 0 },
                         {"hoodie", 0 },
                         {"pants", 0 },
                         {"roadsign.kilt", 0 },
                         {"shoes.boots", 0 },
                    },
                    Change = new Dictionary<string, ChangeListed>()
                    {
                        ["wood"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 1000
                        },
                        ["metal.ore"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 1000
                        },
                        ["stones"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 1000
                        },
                        ["sulfur.ore"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 1000
                        },
                        ["fat.animal"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 300
                        },
                        ["cloth"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 300
                        },
                        ["leather"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 100
                        },
                        ["hq.metal.ore"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 500
                        },
                        ["loot-barrel"] = new ChangeListed()
                        {
                            Complete = 0,
                            Need = 20
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
            internal void onCreate() => Interface.CallHook("OnClanCreate", tag);
            internal void onUpdate() => Interface.CallHook("OnClanUpdate", tag);
            internal void onDestroy() => Interface.CallHook("OnClanDestroy", tag);
        }
        sealed class ClanRadar : FacepunchBehaviour
        {
            BasePlayer player;
            Clan clan;
            bool noAdmin;
            void Awake()
            {
                player = GetComponent<BasePlayer>();
                player.SetInfo("noRadarAdmin", !player.IsAdmin ? "1" : "2");
                noAdmin = !player.IsAdmin;
                clan = cc.findClanByUser(player.UserIDString);
            }
            public void DoStart()
            {
                CancelInvoke(DoRadar);
                if (!cc.activeRadarUsers.Contains(player.UserIDString)) cc.activeRadarUsers.Add(player.UserIDString);
                InvokeRepeating(DoRadar, 1f, cc.refreshTime);
            }
            void SetPlayerFlag(BasePlayer.PlayerFlags f, bool b)
            {
                if (b)
                {
                    if (player.HasPlayerFlag(f)) return;
                    player.playerFlags |= f;
                }
                else
                {
                    if (!player.HasPlayerFlag(f)) return;
                    player.playerFlags &= ~f;
                }
                player.SendNetworkUpdateImmediate(false);
            }
            void DoRadar()
            {
                if (player != null && (player.IsSleeping() || player.IsSpectating() || player.IsReceivingSnapshot)) return;
                if (player == null || !player.IsConnected || player.IsDead())
                {
                    DoDestroy();
                    return;
                }
                if (noAdmin) SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                try
                {
                    foreach (BasePlayer targetplayer in cc.clanRadarMemberobjects[clan.tag].Where(p => p != null && p != player && !p.IsDead()).ToList())
                    {
                        float targetDist = Vector3.Distance(targetplayer.transform.position, player.transform.position);
                        bool inFullRange = targetDist < maxNamedistance && targetDist > minDistance;
                        bool inSleepRange = targetDist < maxSleeperDistance && targetDist > minDistance;
                        if (targetplayer.IsConnected && !targetplayer.IsSleeping() && inFullRange) player.SendConsoleCommand("ddraw.text", cc.refreshTime, UnityEngine.Color.grey, targetplayer.transform.position + playerHeight, $"<size={cc.radarTextSize}><color={cc.nameColor}>{targetplayer.displayName}</color> | <color={cc.distanceColor}>{Math.Floor(targetDist)}m</color></size>");
                        else if (cc.showSleepers && targetplayer.IsSleeping() && inSleepRange) player.SendConsoleCommand("ddraw.text", cc.refreshTime, UnityEngine.Color.grey, targetplayer.transform.position + sleeperHeight, $"<size={cc.radarTextSize}><color={cc.sleeperNameColor}>{targetplayer.displayName}</color> | <color={cc.distanceColor}>{Math.Floor(targetDist)}m</color></size>");
                    }
                    if (cc.enableClanAllies && cc.extendOnAllyMembers && clan.clanAlliances.Count > 0) foreach (var allyClan in clan.clanAlliances) foreach (BasePlayer targetplayer in cc.clanRadarMemberobjects[allyClan.ToString()].Where(p => p != null && p.IsConnected && !p.IsSleeping() && !p.IsDead()).ToList())
                            {
                                float targetDist = Vector3.Distance(targetplayer.transform.position, player.transform.position);
                                bool inFullRange = targetDist < maxNamedistance && targetDist > minDistance;
                                if (Vector3.Distance(targetplayer.transform.position, player.transform.position) < maxNamedistance && Vector3.Distance(targetplayer.transform.position, player.transform.position) > minDistance) player.SendConsoleCommand("ddraw.text", cc.refreshTime, UnityEngine.Color.grey, targetplayer.transform.position + playerHeight, $"<size={cc.radarTextSize}><color={cc.nameColor}>{targetplayer.displayName}</color> | <color={cc.distanceColor}>{Math.Floor(Vector3.Distance(targetplayer.transform.position, player.transform.position))}m</color></size>");
                            }
                }
                finally
                {
                    if (noAdmin) SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                }
            }
            void DoDestroy()
            {
                CancelInvoke(DoRadar);
                Destroy(this);
            }
            void OnDestroy()
            {
                if (noAdmin) SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                CancelInvoke(DoRadar);
            }
        }
        void RemoveRadar(BasePlayer player, string tag, bool leftOrKicked = false)
        {
            if (player != null && enableClanRadar)
            {
                GameObject.Destroy(player.GetComponent<ClanRadar>());
                activeRadarUsers.Remove(player.UserIDString);
                if (leftOrKicked && clanRadarMemberobjects.ContainsKey(tag)) clanRadarMemberobjects[tag].Remove(player);
            }
        }
        void RemoveRadarGroup(List<string> playerIds, string tag, bool isDisband = false)
        {
            if (!enableClanRadar) return;
            if (isDisband) clanRadarMemberobjects.Remove(tag);
            foreach (var playerId in playerIds)
            {
                BasePlayer player = rust.FindPlayerByIdString(playerId);
                if (player != null) RemoveRadar(player, tag, isDisband);
            }
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
        //object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        //{
        //    if (!(@lock is CodeLock) || @lock.GetParentEntity().OwnerID <= 0) return null;
        //    if (@lock.GetParentEntity().OwnerID == player.userID) return null;
        //    bool check = (bool)HasFriend(@lock.GetParentEntity().OwnerID, player.userID);
        //    if (check == true)
        //    {
        //        return true;
        //    }
        //    return null;
        //}
        //private object OnTurretTarget(AutoTurret turret, BaseCombatEntity targ)
        //{
        //    if (!(targ is BasePlayer) || turret.OwnerID <= 0) return null;
        //    var player = (BasePlayer)targ;
        //    if (turret.IsAuthed(player)) return null;
        //    bool check = (bool)HasFriend(turret.OwnerID, player.userID);
        //    if (check == true)
        //    {
        //        turret.authorizedPlayers.Add(new PlayerNameID
        //        {
        //            userid = player.userID,
        //            username = player.displayName
        //        });
        //        turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        //        return false;
        //    }
        //    return null;
        //}
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
		
		 private bool IsClanMember(string playerId, string otherId)
        {
            Clan playerClan = findClanByUser(playerId);
            if (playerClan == null) return false;

            Clan otherClan = findClanByUser(otherId);
            if (otherClan == null) return false;

            if (playerClan.tag != otherClan.tag)
                return false;

            return true;
        }
		
		private bool IsClanMember(ulong playerId, ulong otherId)
		{
		return IsClanMember(playerId.ToString(),otherId.ToString());
		}
		
		 private bool IsMemberOrAlly(string playerId, string otherId)
        {
            Clan playerClan = findClanByUser(playerId);
            if (playerClan == null) return false;

            Clan otherClan = findClanByUser(otherId);
            if (otherClan == null) return false;

            if (playerClan.tag == otherClan.tag  || playerClan.clanAlliances.Contains(otherClan.tag))
                return true;

            return false;
        }
		
        private bool IsAllyPlayer(string playerId, string otherId)
        {
            Clan playerClan = findClanByUser(playerId);
            if (playerClan == null) return false;

            Clan otherClan = findClanByUser(otherId);
            if (otherClan == null) return false;

            if (playerClan.clanAlliances.Contains(otherClan.tag))
                return true;

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
            SendReply(player, string.Format(pluginPrefixFormat, pluginPrefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + message + "</color>");
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
                textTable.AddRow(new string[] {
    clan.tag, owner.Name, clan.owner, clan.total.ToString(), clan.online.ToString()
}
                );
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
            setupPlayers(clan.members.Keys.ToList());
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
            clan.onUpdate();
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
            if (!pendingPlayerInvites.ContainsKey(invPlayer.Id)) pendingPlayerInvites.Add(invPlayer.Id, new List<string>());
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
                if (joined != null) setupPlayer(joined, joinPlayer.Name, joinPlayer.Id);
            }
            if (usePermGroups && !permission.UserHasGroup(joinPlayer.Id, permGroupPrefix + myClan.tag)) permission.AddUserGroup(joinPlayer.Id, permGroupPrefix + myClan.tag);
            myClan.BroadcastLoc("playerjoined", myClan.ColNam(joinPlayer.Id, joinPlayer.Name));
            myClan.updated = UnixTimeStampUTC();
            myClan.total++;

            myClan.onUpdate();
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
                setupPlayer(kickBasePlayer, kickPlayer.Name, kickPlayer.Id);
                RemoveRadar(kickBasePlayer, myClan.tag, true);
            }
            if (usePermGroups && permission.UserHasGroup(kickPlayer.Id, permGroupPrefix + myClan.tag)) permission.RemoveUserGroup(kickPlayer.Id, permGroupPrefix + myClan.tag);
            myClan.updated = UnixTimeStampUTC();
            myClan.onUpdate();
            Interface.Oxide.CallHook("OnClanMemberGone", kickPlayer.Id, myClan.members);
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
            myClan.onUpdate();
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
            myClan.onUpdate();
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
            myClan.onUpdate();
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
            RemoveRadarGroup(clan.members.Keys.ToList(), clan.tag, true);
            if (arg.Connection != null) DeletedBy = arg.Connection.username;
            clan.BroadcastLoc("clandeleted", $"<color={clanServerColor}>{DeletedBy}</color>");
            RemoveClan(arg.Args[0]);
            foreach (var member in clan.members) clanCache.Remove(member.Key);
            setupPlayers(clan.members.Keys.ToList());
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
            clan.onDestroy();

            Interface.Oxide.CallHook("OnClanDisbanded", clan.members);
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
