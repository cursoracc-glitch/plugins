using UnityEngine;
using System;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using Rust;
using System.Text;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("XDStatistics", "DezLife", "1.9.33")]
    [Description("Multifunctional statistics for your server!")]
    class XDStatistics : RustPlugin
    {
        /// <summary>
        /// Теперь награды выдаются сразу после вайпа
        /// Правки в UI фаворитного оружия
        /// Теперь очки за добычу считаются за последний удар по камню. (ранее считался каждый удар)
        /// Добавлена команда для начислени/удаления очков
        /// Разделил файлы даты, для удобства (Перед загрузкой плагина нужно удалить дата файл)
        /// Добавлена возможность награждать игроков в категории 'Убийца нпс'
        /// Для PVE серверов добавлена функция в конфиги которую нужно активировать
        /// Исправления небольших проблем
        /// Теперь с разрешением 'XDStatistics.admin' игрок сможет смотреть скрытую статистику
        /// Теперь для магазина GAMESTORE - Можно указывать сообщения для истории в магазине
        /// Добавлена возможность импользовать свое изображения для заднего фона
        /// </summary>

         #region ReferencePlugins
        [PluginReference] Plugin ImageLibrary, Friends, Clans, Battles, Duel, Economics, IQEconomic, ServerRewards, GameStoresRUST, RustStore;
        private bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else if (RelationshipManager.ServerInstance.playerToTeam.ContainsKey(userID) && RelationshipManager.ServerInstance.playerToTeam[userID].members.Contains(targetID))
                return true;
            else
                return false;
        }

        private bool IsClans(string userID, string targetID)
        {
            if (Clans)
            {
                String TagUserID = (String)Clans?.Call("GetClanOf", userID);
                String TagTargetID = (String)Clans?.Call("GetClanOf", targetID);
                if (TagUserID == null && TagTargetID == null)
                    return false;
                return (bool)(TagUserID == TagTargetID);
            }
            else
                return false;
        }
        private bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel)
                return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else
                return false;
        }
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);

        #endregion

        #region Var
        private static XDStatistics _;
        private readonly string permAdmin = "XDStatistics.admin";
        private readonly string permReset = "XDStatistics.reset";
        private readonly string permAvailability = "XDStatistics.availability";
        private enum CatType
        {
            score,
            killer,
            time,
            farm,
            raid,
            killerNPC
        }
        private Dictionary<string, ItemDisplayName> ItemName = new Dictionary<string, ItemDisplayName>();
        private List<items> ItemList = new List<items>();
        public static StringBuilder sb;
        private Dictionary<uint, string> prefabID2Item = new Dictionary<uint, string>();
        private Dictionary<string, string> prefabNameItem = new Dictionary<string, string>()
        {
            ["40mm_grenade_he"] = "multiplegrenadelauncher",
            ["grenade.beancan.deployed"] = "grenade.beancan",
            ["grenade.f1.deployed"] = "grenade.f1",
            ["explosive.satchel.deployed"] = "explosive.satchel",
            ["explosive.timed.deployed"] = "explosive.timed",
            ["rocket_basic"] = "ammo.rocket.basic",
            ["rocket_hv"] = "ammo.rocket.hv",
            ["rocket_fire"] = "ammo.rocket.fire",
            ["survey_charge.deployed"] = "surveycharge"
        };
        private List<int> AlowedSeedId = new List<int>(){ 1548091822, 1771755747, 1112162468, 1367190888,858486327,-1962971928, -2086926071, -567909622, 1272194103, 854447607,1660145984, 1783512007, -858312878 };
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["STAT_TOP_FIVE_KILL"] = "<size=16>Top 5 <color=#4286f4>Killers</color>\n{0}</size>",
                ["STAT_TOP_FIVE_KILL_NPC"] = "<size=16>Top 5 <color=#4286f4>NPC killers</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FARM"] = "<size=16>Top 5 <color=#4286f4>Farmers</color>\n{0}</size>",
                ["STAT_TOP_FIVE_EXPLOSION"] = "<size=16>Top 5 <color=#4286f4>Explosions</color>\n{0}</size>",
                ["STAT_TOP_FIVE_TIMEPLAYED"] = "<size=16>Top 5 <color=#4286f4>Most Time Played</color>\n{0}</size>",
                ["STAT_TOP_FIVE_BUILDINGS"] = "<size=16>Top 5 <color=#4286f4>Builders</color>\n{0}</size>",
                ["STAT_TOP_FIVE_SCORE"] = "<size=16>Top 5 <color=#4286f4>Most points</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FERMER"] = "<size=16>Top 5 <color=#4286f4>Farmers</color>\n{0}</size>",
                ["STAT_USER_TOTAL_GATHERED"] = "Total Gathered:",
                ["STAT_USER_TOTAL_EXPLODED"] = "Total Explosions:",
                ["STAT_USER_TOTAL_GROWED"] = "Total Farmed:",
                ["STAT_UI_MY_STAT"] = "My Statistics",
                ["STAT_UI_TOP_TEN"] = "Top 10 Players",
                ["STAT_UI_SEARCH"] = "Search",
                ["STAT_UI_INFO"] = "Player Information {0}",
                ["STAT_UI_ACTIVITY"] = "Activity",
                ["STAT_UI_ACTIVITY_TODAY"] = "Today: {0}",
                ["STAT_UI_ACTIVITY_TOTAL"] = "All Time: {0}",
                ["STAT_UI_SETTINGS"] = "Settings",
                ["STAT_UI_PLACE_TOP"] = "Place On Top: {0}",
                ["STAT_UI_SCORE"] = "Score: {0}",
                ["STAT_UI_PVP"] = "PvP Statistics",
                ["STAT_UI_FAVORITE_WEAPON"] = "Favorite Weapon",
                ["STAT_UI_PVP_KILLS"] = "Kills",
                ["STAT_UI_PVP_KILLS_NPC"] = "Kills NPC",
                ["STAT_UI_PVP_DEATH"] = "Deaths",
                ["STAT_UI_PVP_KDR"] = "K/D",
                ["STAT_UI_FAVORITE_WEAPON_KILLS"] = "Kills: {0} \n Hits: {1}",
                ["STAT_UI_FAVORITE_WEAPON_NOT_DATA"] = "Data is still being calculated..",
                ["STAT_UI_OTHER_STAT"] = "Other Statistics",
                ["STAT_UI_HIDE_STAT"] = "Public Profile",
                ["STAT_UI_CONFIRM"] = "Are You Sure?",
                ["STAT_UI_CONFIRM_YES"] = "Yes",
                ["STAT_UI_CONFIRM_NO"] = "No",
                ["STAT_UI_RESET_STAT"] = "Reset Statistics",
                ["STAT_UI_CRATE_OPEN"] = "Crates Opened: {0}",
                ["STAT_UI_BARREL_KILL"] = "Barrels Destroyed: {0}",
                ["STAT_UI_ANIMAL_KILL"] = "Animal Kills: {0}",
                ["STAT_UI_HELI_KILL"] = "Helicopter Kills: {0}",
                ["STAT_UI_BRADLEY_KILL"] = "Bradley Kills: {0}",
                ["STAT_UI_NPC_KILL"] = "NPC Kills: {0}",
                ["STAT_UI_BTN_MORE"] = "Show More",
                ["STAT_UI_CATEGORY_GATHER"] = "Gather",
                ["STAT_UI_CATEGORY_EXPLOSED"] = "Explosions",
                ["STAT_UI_CATEGORY_PLANT"] = "Farming",
                ["STAT_UI_CATEGORY_TOP_KILLER"] = "Top 10 Killers",
                ["STAT_UI_CATEGORY_TOP_KILLER_ANIMALS"] = "Top 10 Animal Killers",
                ["STAT_UI_CATEGORY_TOP_NPCKILLER"] = "Top 10 NPC Killers",
                ["STAT_UI_CATEGORY_TOP_TIME"] = "Top 10 Most Time Played",
                ["STAT_UI_CATEGORY_TOP_GATHER"] = "Top 10 Gatherers",
                ["STAT_UI_CATEGORY_TOP_SCORE"] = "Top 10 Most Score",
                ["STAT_UI_CATEGORY_TOP_EXPLOSED"] = "Top 10 Explosions",
                ["STAT_PRINT_WIPE"] = "Wipe Detected. Data was successfully cleared!",
                ["STAT_CMD_1"] = "No Permission!!",
                ["STAT_CMD_2"] = "Usage: stat.ignore <add/remove> <Steam ID|Name>",
                ["STAT_CMD_3"] = "The specified playername could not be found. Please use their SteamID.",
                ["STAT_CMD_4"] = "Found several players with similar names: {0}",
                ["STAT_CMD_5"] = "Player not found!",
                ["STAT_CMD_6"] = "Player {0} is already ignored",
                ["STAT_CMD_7"] = "You have successfully added a player {0} to the ignore list",
                ["STAT_CMD_8"] = "The player {0} is not in the ignore list",
                ["STAT_CMD_9"] = "You have successfully removed the player {0} from the ignore list",
                ["STAT_CMD_10"] = "Player {0} successfully credited {1} score",
                ["STAT_CMD_11"] = "Player {0} successfully removed {1} score",
                ["STAT_ADMIN_HIDE_STAT"] = "You've been added to the ignore list. You will not have access to the statistics. If this is an error, Please contact the Administrator!",
                ["STAT_TOP_PLAYER_WIPE_SCORE"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>HIGHEST SCORE</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_TIME"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST TIME PLAYED</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_EXP"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST EXPLOSIONS</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_FARM"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST CROPS FARMED</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_KILL"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST KILLS</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_KILL_NPC"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>NPC Killer</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_VK_SCORE"] = "Топ {0} игрока по очкам\n {1}",
                ["STAT_TOP_VK_KILLER"] = "Топ {0} игрока по убийствам\n {1}",
                ["STAT_TOP_VK_TIME"] = "Топ {0} игрока по онлайну\n {1}",
                ["STAT_TOP_VK_FARM"] = "Топ {0} игрока по фарму\n {1}",
                ["STAT_TOP_VK_RAID"] = "Топ {0} игрока по рейдам\n {1}",
                ["STAT_TOP_VK_KILLER_NPC"] = "Топ {0} игрока по убийствам NPC\n {1}",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["STAT_TOP_FIVE_KILL"] = "<size=16>Топ 5 <color=#4286f4>киллеры</color>\n{0}</size>",
                ["STAT_TOP_FIVE_KILL_NPC"] = "<size=16>Топ 5 <color=#4286f4>убийцы NPC</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FARM"] = "<size=16>Топ 5 <color=#4286f4>фармеры</color>\n{0}</size>",
                ["STAT_TOP_FIVE_EXPLOSION"] = "<size=16>Топ 5 <color=#4286f4>рейдеры</color>\n{0}</size>",
                ["STAT_TOP_FIVE_TIMEPLAYED"] = "<size=16>Топ 5 <color=#4286f4>долгожителей</color>\n{0}</size>",
                ["STAT_TOP_FIVE_BUILDINGS"] = "<size=16>Топ 5 <color=#4286f4>строители</color>\n{0}</size>",
                ["STAT_TOP_FIVE_SCORE"] = "<size=16>Топ 5 по <color=#4286f4>очкам</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FERMER"] = "<size=16>Топ 5 <color=#4286f4>фермеров</color>\n{0}</size>",
                ["STAT_USER_TOTAL_GATHERED"] = "Всего добыто:",
                ["STAT_USER_TOTAL_EXPLODED"] = "Всего взорвано:",
                ["STAT_USER_TOTAL_GROWED"] = "Всего выращено:",
                ["STAT_UI_MY_STAT"] = "Моя Статистика",
                ["STAT_UI_TOP_TEN"] = "Топ 10 игроков",
                ["STAT_UI_SEARCH"] = "Поиск",
                ["STAT_UI_INFO"] = "Информация о профиле {0}",
                ["STAT_UI_ACTIVITY"] = "Активность",
                ["STAT_UI_ACTIVITY_TODAY"] = "Сегодня: {0}",
                ["STAT_UI_ACTIVITY_TOTAL"] = "За все время: {0}",
                ["STAT_UI_SETTINGS"] = "Настройки",
                ["STAT_UI_PLACE_TOP"] = "Место в топе: {0}",
                ["STAT_UI_SCORE"] = "SCORE: {0}",
                ["STAT_UI_PVP"] = "PVP статистика",
                ["STAT_UI_FAVORITE_WEAPON"] = "Фаворитное оружие",
                ["STAT_UI_PVP_KILLS"] = "Убийств",
                ["STAT_UI_PVP_KILLS_NPC"] = "Убийств NPC",
                ["STAT_UI_PVP_DEATH"] = "Смертей",
                ["STAT_UI_PVP_KDR"] = "K/D",
                ["STAT_UI_FAVORITE_WEAPON_KILLS"] = "Убийств: {0} \n Попаданий: {1}",
                ["STAT_UI_FAVORITE_WEAPON_NOT_DATA"] = "Данные еще собираются...",
                ["STAT_UI_OTHER_STAT"] = "Другая статистика",
                ["STAT_UI_HIDE_STAT"] = "Общедоступный профиль",
                ["STAT_UI_CONFIRM"] = "Вы уверены ?",
                ["STAT_UI_CONFIRM_YES"] = "Да",
                ["STAT_UI_CONFIRM_NO"] = "Нет",
                ["STAT_UI_RESET_STAT"] = "Обнулить статистику",
                ["STAT_UI_CRATE_OPEN"] = "Открыто ящиков: {0}",
                ["STAT_UI_BARREL_KILL"] = "Разбито бочек: {0}",
                ["STAT_UI_ANIMAL_KILL"] = "Убито животных: {0}",
                ["STAT_UI_HELI_KILL"] = "Сбито вертолетов: {0}",
                ["STAT_UI_BRADLEY_KILL"] = "Танков уничтожено: {0}",
                ["STAT_UI_NPC_KILL"] = "Убито NPC: {0}",
                ["STAT_UI_BTN_MORE"] = "Показать еще",
                ["STAT_UI_CATEGORY_GATHER"] = "Добыча",
                ["STAT_UI_CATEGORY_EXPLOSED"] = "Взрывчатка",
                ["STAT_UI_CATEGORY_PLANT"] = "Фермерство",
                ["STAT_UI_CATEGORY_TOP_KILLER"] = "Топ 10 киллеров",
                ["STAT_UI_CATEGORY_TOP_KILLER_ANIMALS"] = "Топ 10 убийц животных",
                ["STAT_UI_CATEGORY_TOP_NPCKILLER"] = "Топ 10 убийц npc",
                ["STAT_UI_CATEGORY_TOP_TIME"] = "Топ 10 по онлайну",
                ["STAT_UI_CATEGORY_TOP_GATHER"] = "Топ 10 фармил",
                ["STAT_UI_CATEGORY_TOP_SCORE"] = "Топ 10 по очкам",
                ["STAT_UI_CATEGORY_TOP_EXPLOSED"] = "Топ 10 рейдеров",
                ["STAT_PRINT_WIPE"] = "Произошел вайп. Данные успешно удалены!",
                ["STAT_CMD_1"] = "Недостаточно прав!",
                ["STAT_CMD_2"] = "Используйте: stat.ignore <add/remove> <Steam ID|Имя>",
                ["STAT_CMD_3"] = "Указанный игрок не найден. Для более точного поиска укажите его SteamID.",
                ["STAT_CMD_4"] = "Найдено несколько игроков с похожим именем: {0}",
                ["STAT_CMD_5"] = "Игрок не найден!",
                ["STAT_CMD_6"] = "Игрок {0} уже игнорируется",
                ["STAT_CMD_7"] = "Вы успешно добавили игрока {0} в игнор лист",
                ["STAT_CMD_8"] = "Игрока {0} нет в списке игнорируемых",
                ["STAT_CMD_9"] = "Вы успешно убрали игрока {0} из игнор листа",
                ["STAT_CMD_10"] = "Игроку {0} успешно зачислено {1} очков",
                ["STAT_CMD_11"] = "Игроку {0} успешно снято {1} очков",
                ["STAT_ADMIN_HIDE_STAT"] = "Вы добавлены в игнор лист. У вас нет доступа к статистики, если это ошибка, свяжитесь с администратором!",
                ["STAT_TOP_PLAYER_WIPE_SCORE"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>SCORE</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_TIME"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Долгожитель</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_EXP"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Рейдер</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_FARM"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Добытчик</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_KILL"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Киллер</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_KILL_NPC"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Убийца нпс</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_VK_SCORE"] = "Топ {0} игрока по очкам\n {1}",
                ["STAT_TOP_VK_KILLER"] = "Топ {0} игрока по убийствам\n {1}",
                ["STAT_TOP_VK_TIME"] = "Топ {0} игрока по онлайну\n {1}",
                ["STAT_TOP_VK_FARM"] = "Топ {0} игрока по фарму\n {1}",
                ["STAT_TOP_VK_RAID"] = "Топ {0} игрока по рейдам\n {1}",
                ["STAT_TOP_VK_KILLER_NPC"] = "Топ {0} игрока по убийствам NPC\n {1}",
            }, this, "ru");
        }

        #endregion

        #region Configuration
        private Configuration config;

        private class Configuration
        {
            public class SettingsInterface
            {
                [JsonProperty("Цвет плашки заднего фона в топ 10 за 1 место | Background color in the top 10 for 1st place")]
                public string ColorTop1 = "1 0.8431373 0 0.49";
                [JsonProperty("Цвет плашки заднего фона в топ 10 за 2 место | Background color in the top 10 for 2st place")]
                public string ColorTop2 = "0.7529412 0.7529412 0.7529412 0.49";
                [JsonProperty("Цвет плашки заднего фона в топ 10 за 3 место | Background color in the top 10 for 3st place")]
                public string ColorTop3 = "0.803922835 0.49803 0.1960784 0.49";
                [JsonProperty("Использовать свой задний фон ? (указанный снизу)| Use your own background ? (indicated at the bottom)")]
                public bool UsebackgroundImageUrl = false;
                [JsonProperty("Ссылка на свой задний фон (Если нужно) | Link to your background (If necessary)")]
                public string backgroundImageUrl = "";

            }
            public class Settings
            {
                [JsonProperty("Чат команда для открытия статистики | Chat command for opening statistics")]
                public string chatCommandOpenStat = "stat";
                [JsonProperty("Консольная команда для открытия статистики | Console command to open statistics")]
                public string consoleCommandOpenStat = "stat";
                [JsonProperty("Отправлять в чат сообщения с топ 5 игроками в разных категориях | Send chat messages with top 5 players in different categories")]
                public bool chatSendTop = true;
                [JsonProperty("Раз в сколько секунд будет отправлятся сообщение ? | Once in how many seconds will a message be sent ?")]
                public int chatSendTopTime = 600;
                [JsonProperty("Включить возможность сбросить свою статистику ? (требуется XDStatistics.reset) | Enable the ability to reset your stats ? (requires XDStatistics.reset)")]
                public bool dropStatUse = false;
                [JsonProperty("Включить возможность скрыть свою статистику от пользователей ? (требуется XDStatistics.availability) | Enable the ability to hide your statistics from users ? (requires XDStatistics.availability)")]
                public bool availabilityUse = true;
                [JsonProperty("очищать данные при вайпе | Clear data when wiped")]
                public bool wipeData = true;
                [JsonProperty("Раз во сколько минут будут сохранятся данные | Once in a rowman, the data will be saved.")]
                public int dataSaveTime = 30;
                [JsonProperty("Учитывать убийство npc для фаворитного оружия ? | Consider killing an NPC for a favorite weapon ?")]
                public bool npsDeathUse = false;
                [JsonProperty("У вас PVE сервер ?  | You have a PVE server?")]
                public bool pveServerMode = false;
            }
            public class SettingsScore
            {
                [JsonProperty("Очки за крафт | Points for crafting")]
                public float craftScore = 1;
                [JsonProperty("Очки за бочки | Points for barrels")]
                public float barrelScore = 1;
                [JsonProperty("Очки за установку строительных блоков | Points for installing building blocks")]
                public float BuildingScore = 1;
                [JsonProperty("Очки за использования взрывчатых предметов | Points for using explosive items")]
                public Dictionary<string, float> ExplosionScore = new Dictionary<string, float>();
                [JsonProperty("Очки за добычу ресурсов | Points for resource extraction")]
                public Dictionary<string, float> GatherScore = new Dictionary<string, float>();
                [JsonProperty("Очки за найденные скрап | Points for found scraps")]
                public float ScrapScore = 0.5f;
                [JsonProperty("Очки за сбор урожая (с плантации) | Points for harvesting (from the plantation)")]
                public float PlantScore = 0.2f;
                [JsonProperty("Очки за убийство животных | Points for killing animals")]
                public float AnimalScore = 1;
                [JsonProperty("Очки за сбитие вертолета | Points for shooting down a helicopter")]
                public float HeliScore = 5;
                [JsonProperty("Очки за взрыв танка | Points for tank explosion")]
                public float BradleyScore = 5;
                [JsonProperty("Очки за убийство нпс | Points for killing NPCs")]
                public float NpcScore = 5;
                [JsonProperty("Очки за убийство игроков | Points for killing players")]
                public float PlayerScore = 10;
                [JsonProperty("Очки за время (За кажду. минуту игры на сервере) | Points for time (for every minute of the game on the server)")]
                public float TimeScore = 0.2f;
                [JsonProperty("Сколько отнять очков за суицид ? | How many points to take away for suicide ?")]
                public float SuicideScore = 2;
                [JsonProperty("Сколько отнять очков за смерть ? | How many points to take away for death ?")]
                public float DeathScore = 1;
            }
            public class SettingsPrize
            {
                [JsonProperty("Использовать выдачу награды после вайпа ? | Use the award issue after the wipe ?")]
                public bool prizeUse = false;
                [JsonProperty("Награда в категории SCORE | Award in the SCORE category")]
                public List<Prize> prizeScore = new List<Prize>();
                [JsonProperty("Награда в категории Киллер | Award in the Killer category")]
                public List<Prize> prizeKiller = new List<Prize>();
                [JsonProperty("Награда в категории Фармила | Award in the gathering category")]
                public List<Prize> prizeFarm = new List<Prize>();
                [JsonProperty("Награда в категории рейдер | Reward in the Raider category")]
                public List<Prize> prizeRaid = new List<Prize>();
                [JsonProperty("Награда в категории Большой онлайн | Award in the Big Online category")]
                public List<Prize> prizeTime = new List<Prize>();
                [JsonProperty("Награда в категории Убийца НПС | Reward in the NPC Killer category")]
                public List<Prize> prizeNPCKiller = new List<Prize>();
                [JsonProperty("[RU][GameStores] ID магазина")]
                public string ShopID = "";
                [JsonProperty("[RU][GameStores] ID сервера")]
                public string ServerID = "";
                [JsonProperty("[RU][GameStores] Секретный ключ")]
                public string SecretKey = "";
                internal class Prize
                {
                    [JsonProperty("Использовать комманды в виде приза ? | Use command as a prize ?")]
                    public bool commandPrizeUse = true;
                    [JsonProperty("За какое место давать эту награду ? (от 1 до 3) если не нужно награждать эту категорию. удалите все награды | For which place to give this award ? (from 1 to 3) if you do not need to award this category. remove all rewards")]
                    public int top = 1;
                    [JsonProperty("[RU]Использовать магазин GameStore для выдачи награды")]
                    public bool gamestorePrizeUse = false;
                    [JsonProperty("[RU]Использовать магазин MoscowOVH для выдачи награды")]
                    public bool ovhPrizeUse = false;
                    [JsonProperty("Использовать [IQEconomic или Economics или ServerRewards] для выдачи награды | Use [IQEconomic or Economics or Server Rewards] to issue a reward")]
                    public bool economicPrizeUse = false;
                    [JsonProperty("Команды для приза | Command for the prize")]
                    public List<string> commandPrizeList = new List<string>();
                    [JsonProperty("[RU][GameStores] Сообщения для истории в магазине")]
                    public string balancePlusMess = "За топ 1!!!";
                    [JsonProperty("[RU][GameStores или MoscowOVH] Сколько начислять денег на баланс")]
                    public int balancePlus = 30;
                    [JsonProperty("[IQEconomic или Economics или ServerRewards] Сколько начислять денег на баланс | [IQEconomic or Economics or ServerRewards] How much money to add to the balance")]
                    public int balanceEconomicsPlus = 100;

                    public void GiftPrizePlayer(string player)
                    {
                        if (commandPrizeUse)
                        {
                            foreach (string cmd in commandPrizeList)
                                _.Server.Command(cmd.Replace("%STEAMID%", player));
                        }
                        if (gamestorePrizeUse && _?.GameStoresRUST)
                        {
                            _.webrequest.Enqueue(
                           $"https://gamestores.ru/api?shop_id={_.config.settingsPrize.ShopID}&secret={_.config.settingsPrize.SecretKey}&server={_.config.settingsPrize.ServerID}&action=moneys&type=plus&steam_id={player}&amount={balancePlus}&mess={balancePlusMess}",
                           "", (code, response) =>
                           {
                               switch (code)
                               {
                                   case 0:
                                       {
                                           _.PrintError("Api does not responded to a request");
                                           break;
                                       }
                                   case 200:
                                       {
                                           break;
                                       }
                                   case 404:
                                       {
                                           _.PrintError($"Please check your configuration! {code}");
                                           break;
                                       }
                               }
                           }, _);
                        }
                        if (ovhPrizeUse)
                        {
                            if (_?.RustStore)
                            {
                                _.RustStore.CallHook("APIChangeUserBalance", ulong.Parse(player), balancePlus, new Action<string>(result =>
                                {
                                    if (result == "SUCCESS")
                                        return;
                                    Interface.Oxide.LogDebug($"Баланс игрока {ulong.Parse(player)} не был изменен, ошибка: {result}");
                                }));
                            }
                        }
                        if (economicPrizeUse)
                        {
                            if (_?.Economics)
                            {
                                _.Economics.Call("Deposit", ulong.Parse(player), (double)balanceEconomicsPlus);
                            }
                            else if (_?.IQEconomic)
                            {
                                _.IQEconomic.Call("API_SET_BALANCE", ulong.Parse(player), balanceEconomicsPlus);
                            }
                            else if (_?.ServerRewards)
                            {
                                _.ServerRewards.Call("AddPoints", ulong.Parse(player), balanceEconomicsPlus);
                            }
                        }
                    }

                }
            }

            [JsonProperty("Основные настройки плагина | Basic plugin settings")]
            public Settings settings = new Settings();
            [JsonProperty("Настройка выдачи очков | Setting up the issuance of points")]
            public SettingsScore settingsScore = new SettingsScore();
            [JsonProperty("Настройка награды топ игрокам в каждой категории | Customize rewards for top 1 players in each category")]
            public SettingsPrize settingsPrize = new SettingsPrize();
            [JsonProperty("Настройки интерфейса | Interface Settings")]
            public SettingsInterface settingsInterface = new SettingsInterface();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    LoadDefaultConfig();
                ValidateConfig();
                SaveConfig();
            }
            catch (JsonException ex)
            {
                Debug.LogException(ex);
                LoadDefaultConfig();
            }
        }

        private void ValidateConfig()
        {
            if (config.settingsScore.GatherScore.Count == 0)
            {
                config.settingsScore.GatherScore = new Dictionary<string, float>
                {
                    ["wood"] = 0.3f,
                    ["stones"] = 0.6f,
                    ["metal.ore"] = 1,
                    ["sulfur.ore"] = 1.5f,
                    ["hq.metal.ore"] = 2,
                };
            }
            if (config.settingsScore.ExplosionScore.Count == 0)
            {
                config.settingsScore.ExplosionScore = new Dictionary<string, float>
                {
                    ["explosive.timed"] = 2,
                    ["explosive.satchel"] = 0.7f,
                    ["grenade.beancan"] = 0.3f,
                    ["grenade.f1"] = 0.1f,
                    ["ammo.rocket.basic"] = 1,
                    ["ammo.rocket.hv"] = 0.5f,
                    ["ammo.rocket.fire"] = 0.7f,
                    ["ammo.rifle.explosive"] = 0.02f,
                };
            }
            if (!config.settingsScore.ExplosionScore.ContainsKey("ammo.rifle.explosive"))
            {
                config.settingsScore.ExplosionScore.Add("ammo.rifle.explosive", 0.02f);
            }

            if (config.settingsPrize.prizeFarm.Count == 0)
            {
                config.settingsPrize.prizeFarm = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории фармер" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории фармер" },
                };
            }
            if (config.settingsPrize.prizeKiller.Count == 0)
            {
                config.settingsPrize.prizeKiller = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории киллер" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории киллер" },
                };
            }
            if (config.settingsPrize.prizeRaid.Count == 0)
            {
                config.settingsPrize.prizeRaid = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории рейдер" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории рейдер" },
                };
            }
            if (config.settingsPrize.prizeScore.Count == 0)
            {
                config.settingsPrize.prizeScore = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории больше всего очков" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории больше всего очков"  },
                };
            }
            if (config.settingsPrize.prizeTime.Count == 0)
            {
                config.settingsPrize.prizeTime = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории большой онлайн"  },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории большой онлайн" },
                };
            }
            if (config.settingsPrize.prizeNPCKiller.Count == 0)
            {
                config.settingsPrize.prizeNPCKiller = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории убийца NPC" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории убийца NPC" },
                };
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion

        #region Data
        private static Dictionary<ulong, PlayerInfo> Players = new Dictionary<ulong, PlayerInfo>();
        private static Dictionary<ulong, PlayerInfo> IgnoreReservedPlayer = new Dictionary<ulong, PlayerInfo>();
        private static Dictionary<ulong, List<PrizePlayer>> PrizePlayerData = new Dictionary<ulong, List<PrizePlayer>>();
        
        private class PrizePlayer
        {
            public string Name = string.Empty;
            public CatType catType;
            public int value;
            public int top;
        }

        private class PlayerInfo
        {
            public string Name = string.Empty;
            public bool HidedStatistics = false;

            public float Score = 0;
            public Harvesting harvesting = new Harvesting();
            public OtherStat otherStat = new OtherStat();
            public Explosion explosion = new Explosion();
            public Gather gather = new Gather();
            public Weapon weapon = new Weapon();
            public PVP pVP = new PVP();
            public PlayedTime playedTime = new PlayedTime();

            internal class PlayedTime
            {
                public string DayNumber = DateTime.Now.ToShortDateString();
                public int PlayedForWipe = 0;
                public int PlayedToday = 0;
            }
            internal class Harvesting
            {
                public Dictionary<string, int> HarvestingList = new Dictionary<string, int>();
                public int AllHarvesting = 0;
            }
            internal class OtherStat
            {
                public int CrateOpen = 0;
                public int BarrelDeath = 0;
                public int AllCraft = 0;
                public int BuildingCrate = 0;
                public int AnimalsKill = 0;
            }
            internal class Explosion
            {
                public Dictionary<string, int> ExplosionUsed = new Dictionary<string, int>()
                {
                   ["explosive.timed"] = 0,
                   ["explosive.satchel"] = 0,
                   ["grenade.beancan"] = 0,
                   ["grenade.f1"] = 0,
                   ["ammo.rocket.basic"] = 0,
                   ["ammo.rocket.hv"] = 0,
                   ["ammo.rocket.fire"] = 0,
                   ["ammo.rifle.explosive"] = 0
                };
                public int AllExplosionUsed = 0;
            }
            internal class Gather
            {
                public Dictionary<string, int> GatheredTotal = new Dictionary<string, int>()
                {
                    ["wood"] = 0,
                    ["stones"] = 0,
                    ["metal.ore"] = 0,
                    ["sulfur.ore"] = 0,
                    ["hq.metal.ore"] = 0,
                    ["scrap"] = 0,
                };
                public int AllGathered = 0; 
            }
            internal class Weapon
            {
                public Dictionary<string, WeaponInfo> WeaponUsed = new Dictionary<string, WeaponInfo>();
                internal class WeaponInfo
                {
                    public int Kills = 0;
                    public int Headshots = 0;
                    public int Shots = 0;
                }
            }
            internal class PVP
            {
                public int Kills = 0;
                public int KillsNpc = 0;
                public int Deaths = 0;
                public int Suicides = 0;
                public int Shots = 0;
                public int Headshots = 0;
                public int HeliKill = 0;
                public int BradleyKill = 0;
            }
            public static void AddPlayedTime(ulong id)
            {
                var player = Players.ContainsKey(id) ? Players[id] : null;
                if (player == null)
                    return;
                player.playedTime.PlayedToday++;
                player.playedTime.PlayedForWipe++;
                player.Score += _.config.settingsScore.TimeScore;

                if (player.playedTime.DayNumber != DateTime.Now.ToShortDateString())
                {
                    player.playedTime.PlayedToday = 0;
                    player.playedTime.DayNumber = DateTime.Now.ToShortDateString();
                }
            }
            public static void PlayerClearData(ulong id)
            {
                BasePlayer player = BasePlayer.FindByID(id);
                Players[id] = new PlayerInfo();
                Players[id].Name = player.displayName;
            }
            public static void ClearDataWipe()
            {
                if (IgnoreReservedPlayer != null)
                {
                    foreach (var item in IgnoreReservedPlayer)
                        _.NextTick(() => {
                            IgnoreReservedPlayer[item.Key] = new PlayerInfo();
                        });
                }
                if (Players != null)
                {
                    foreach (var item in Players)
                    {
                        if (item.Value.playedTime.PlayedForWipe <= 300)
                            _.NextTick(() => { Players.Remove(item.Key); });
                        else
                            _.NextTick(() => { Players[item.Key] = new PlayerInfo(); });
                    }
                }         
            }
            public static PlayerInfo Find(ulong id)
            {
                if (Players.ContainsKey(id))
                {
                    return Players[id];
                }

                if (IgnoreReservedPlayer.ContainsKey(id))
                {
                    return IgnoreReservedPlayer[id];
                }

                Players.Add(id, new PlayerInfo());

                if (Players.ContainsKey(id))
                {
                    return Players[id];
                }
                return null;
            }
        }

        private void LoadData() => Players = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("XDStatistics/StatsUser");
        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("XDStatistics/StatsUser", Players);
        private void LoadDataIgnoreList() => IgnoreReservedPlayer = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("XDStatistics/IgnorePlayers");
        private void SaveDataIgnoreList() => Interface.GetMod().DataFileSystem.WriteObject("XDStatistics/IgnorePlayers", IgnoreReservedPlayer);
        private void LoadDataPrize() => PrizePlayerData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, List<PrizePlayer>>>("XDStatistics/PlayersReward");
        private void SaveDataPrize() => Interface.GetMod().DataFileSystem.WriteObject("XDStatistics/PlayersReward", PrizePlayerData);
        #endregion

        #region StatHooks     
        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            PlayerInfo Player = PlayerInfo.Find(task.owner.userID);
            Player.otherStat.AllCraft += item.amount;
            Player.Score += config.settingsScore.craftScore;
        }

        private void OnEntityDeath(LootContainer entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;
            BasePlayer player = info.InitiatorPlayer;
            if (player == null)
                return;
            if (entity.ShortPrefabName.Contains("barrel"))
            {
                PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                Playerstat.otherStat.BarrelDeath++;
                Playerstat.Score += config.settingsScore.barrelScore;
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null)
                return;

            BaseEntity entity = go?.ToBaseEntity();
            if (entity == null)
                return;

            if (entity.PrefabName.Contains("building core"))
            {
                PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                Playerstat.otherStat.BuildingCrate++;
                Playerstat.Score += config.settingsScore.BuildingScore;
            }
        }
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (player == null || item == null)
                return;
            ExplosionProgressAdd(player, entity);
        }
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (projectile == null || player == null)
                return;
            ExplosionProgressAdd(player, null, projectile.primaryMagazine?.ammoType?.shortname);
        }
        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null)
                return;
            ExplosionProgressAdd(player, entity);
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null)
                return;
            NextTick(() => {
                ProgressAdd(player, item, true);
            });
        }
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null)
                return;
            NextTick(() => {
                ProgressAdd(player, item);
            });
        }
        private void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {       
            NextTick(() => {
                if (AlowedSeedId.Contains(item.info.itemid))
                {
                    PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                    if (!Playerstat.harvesting.HarvestingList.ContainsKey(item.info.shortname))
                    {
                        Playerstat.harvesting.HarvestingList.Add(item.info.shortname, item.amount);
                    }
                    else
                    {
                        Playerstat.harvesting.HarvestingList[item.info.shortname] += item.amount;
                    }
                    Playerstat.harvesting.AllHarvesting += item.amount;
                    Playerstat.Score += config.settingsScore.PlantScore;
                }
                else
                    ProgressAdd(player, item, true);
            });
        }
        private void OnContainerDropItems(ItemContainer container)
        {
            if (container == null)
                return;
            BaseEntity entity = container.entityOwner;
            if (entity == null)
                return;
            if (!entity.ShortPrefabName.Contains("barrel"))
                return;
            foreach (Item lootitem in container.itemList)
            {
                if (lootitem.info.shortname == "scrap")
                    lootitem.skin = 2352567;
            }
        }
        void OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || item.skin != 2352567)
                return;
            item.skin = 0;
            if (item.info.shortname == "scrap")
                ProgressAdd(player, item);
        }
        private static Dictionary<BasePlayer, List<UInt64>> LootersListCarte = new Dictionary<BasePlayer, List<UInt64>>();
        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (entity == null || player == null || entity.OwnerID.IsSteamId() || entity.net == null)
                return;
            if (!LootersListCarte.ContainsKey(player))
                LootersListCarte.Add(player, new List<UInt64> { });
            UInt64 netId = entity.net.ID;
            if (LootersListCarte[player].Contains(netId))
                return;
            PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
            Playerstat.otherStat.CrateOpen++;
            foreach (var item in entity.inventory.itemList)
            {
                if(item.info.shortname == "scrap")
                    ProgressAdd(player, item);
            }
            LootersListCarte[player].Add(netId);
        }
        Dictionary<uint, ulong> _heliattacker = new Dictionary<uint, ulong>();
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
            {
                BasePlayer player = info.InitiatorPlayer;
                if (player == null)
                    return;
                _heliattacker[entity.net.ID] = player.userID;
            }
        }
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null)
                    return;
                BasePlayer player = null;
                if (entity is BaseHelicopter)
                {
                    uint id = entity.net.ID;
                    if (!_heliattacker.ContainsKey(id))
                        return;
                    player = BasePlayer.FindByID(_heliattacker[id]);
                    if (player == null)
                        return;
                    PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                    Playerstat.pVP.HeliKill++;
                    Playerstat.Score += config.settingsScore.HeliScore;
                    _heliattacker.Remove(id);
                    return;
                }
                if (info.InitiatorPlayer != null)
                    player = info.InitiatorPlayer;
                if (player == null || !player.userID.IsSteamId())
                    return;
                PlayerInfo Playerstat2 = PlayerInfo.Find(player.userID);

                if (entity is NPCPlayer || entity is ScarecrowNPC || entity is Zombie)
                {
                    Playerstat2.pVP.KillsNpc++;
                    Playerstat2.Score += config.settingsScore.NpcScore;
                    if(config.settings.npsDeathUse)
                        WeaponProgressAdd(Playerstat2, info, true);
                    return;
                }

                if (entity is BaseAnimalNPC)
                {
                    Playerstat2.otherStat.AnimalsKill++;
                    Playerstat2.Score += config.settingsScore.AnimalScore;
                    return;
                }

                if (entity is BradleyAPC)
                {
                    Playerstat2.pVP.BradleyKill++;
                    Playerstat2.Score += config.settingsScore.BradleyScore;
                    return;
                }     
            }
            catch (NullReferenceException ex)
            {
            }
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (player != null)
            {
                if (hitInfo == null || !player.userID.IsSteamId())
                    return null;
                PlayerInfo PlayerstatVic = PlayerInfo.Find(player.userID);
                if (hitInfo.InitiatorPlayer != null)
                {
                    if (!hitInfo.InitiatorPlayer.userID.IsSteamId())
                        return null;
                    BasePlayer initiator = hitInfo.InitiatorPlayer;
                    if (IsFriends(initiator.userID, player.userID))
                        return null;
                    if (IsClans(initiator.UserIDString, player.UserIDString))
                        return null;
                    if (IsDuel(initiator.userID))
                        return null;
                    PlayerInfo PlayerstatInitiator = PlayerInfo.Find(initiator.userID);
                    if (hitInfo.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                    {
                        PlayerstatVic.pVP.Suicides++;
                        PlayerstatVic.pVP.Deaths++;
                        PlayerstatVic.Score -= config.settingsScore.SuicideScore;
                        return null;
                    }
                    WeaponProgressAdd(PlayerstatInitiator, hitInfo, true);
                    PlayerstatInitiator.pVP.Kills++;
                    PlayerstatInitiator.pVP.Shots++;
                    PlayerstatInitiator.pVP.Headshots += hitInfo.isHeadshot ? 1 : 0;
                    PlayerstatInitiator.Score += config.settingsScore.PlayerScore;
                    PlayerstatVic.pVP.Deaths++;
                    PlayerstatVic.Score -= config.settingsScore.DeathScore;
                }
                else
                {
                    if (hitInfo.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                        return null;
                    PlayerstatVic.pVP.Deaths++;
                    PlayerstatVic.Score -= config.settingsScore.DeathScore;
                }
            }
            return null;
        }
        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (hitinfo == null || attacker == null || hitinfo.HitEntity == null || !attacker.IsConnected)
                return;
            if (hitinfo.HitEntity is BaseNpc)
                return;
            var victim = hitinfo.HitEntity as BasePlayer;
            if (victim == null || !victim.userID.IsSteamId())
                return;
            if (victim == attacker)
                return;
            PlayerInfo PlayerstatInitiator = PlayerInfo.Find(attacker.userID);
            WeaponProgressAdd(PlayerstatInitiator, hitinfo);

            PlayerstatInitiator.pVP.Shots++;
            if (hitinfo.isHeadshot)
                PlayerstatInitiator.pVP.Headshots++;
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (AlowedSeedId.Contains(item.info.itemid))
            {
                NextTick(() =>
                {
                    PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                    if (!Playerstat.harvesting.HarvestingList.ContainsKey(item.info.shortname))
                    {
                        Playerstat.harvesting.HarvestingList.Add(item.info.shortname, item.amount);
                    }
                    else
                    {
                        Playerstat.harvesting.HarvestingList[item.info.shortname] += item.amount;
                    }
                    Playerstat.harvesting.AllHarvesting += item.amount;
                    Playerstat.Score += config.settingsScore.PlantScore;
                });
            }
        }

        #endregion

        #region BaseHooks
        private void Unload()
        {
            SaveData();
            SaveDataIgnoreList();
            SaveDataPrize();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, UI_INTERFACE);
            sb = null;
            _ = null;
        }
        private void Init()
        {
            _ = this;
            sb = new StringBuilder();
            LoadData();
            LoadDataIgnoreList();
            LoadDataPrize();
        }
        private void OnNewSave()
        {
            if (config.settingsPrize.prizeUse)
            {
                ParseTopUserForPrize();
            } 
            if (config.settings.wipeData)
            {
                PlayerInfo.ClearDataWipe();
                NextTick(() => {
                    SaveData();
                    SaveDataIgnoreList();
                });
                PrintWarning(GetLang("STAT_PRINT_WIPE"));
            }
        }
       
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!IgnoreReservedPlayer.ContainsKey(player.userID))
            {
                var dataPlayer = PlayerInfo.Find(player.userID);
                dataPlayer.Name = player.displayName;
                if (dataPlayer.playedTime.DayNumber != DateTime.Now.ToShortDateString())
                {
                    dataPlayer.playedTime.PlayedToday = 0;
                    dataPlayer.playedTime.DayNumber = DateTime.Now.ToShortDateString();
                }
            }
            if (PrizePlayerData.ContainsKey(player.userID))
                SendMsgRewardWipe(player);
            SteamAvatarAdd(player.UserIDString);
        }
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                NextTick(() => {
                    PrintError($"ERROR! Plugin ImageLibrary not found!");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            #region LoadItemList
            foreach (var itemDef in ItemManager.GetItemDefinitions())
            {
                Item newItem = ItemManager.CreateByName(itemDef.shortname, 1, 0);

                BaseEntity heldEntity = newItem.GetHeldEntity();
                if (heldEntity != null)
                {
                    prefabID2Item[heldEntity.prefabID] = itemDef.shortname;
                }

                var deployablePrefab = itemDef.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
                if (string.IsNullOrEmpty(deployablePrefab))
                {
                    continue;
                }

                var shortPrefabName = GameManager.server.FindPrefab(deployablePrefab)?.GetComponent<BaseEntity>()?.ShortPrefabName;
                if (!string.IsNullOrEmpty(shortPrefabName) && !prefabNameItem.ContainsKey(shortPrefabName))
                {
                    prefabNameItem.Add(shortPrefabName, itemDef.shortname);
                }
            }
            #endregion
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            timer.Once(60f, CheckInMinute);

            if (config.settings.chatSendTop)
                timer.Once(config.settings.chatSendTopTime, PlayersTopOnChat);

            /*foreach (var item in ItemManager.itemDictionary)
            {
                if (item.Value.category == ItemCategory.Weapon || item.Value.category == ItemCategory.Resources || item.Value.category == ItemCategory.Food || item.Value.category == ItemCategory.Ammunition || item.Value.category == ItemCategory.Tool)
                    if (HasImage(item.Value.shortname) == false)
                            AddImage($"https://rustlabs.com/img/items180/{item.Value.shortname}.png", item.Value.shortname);
            }*/
            if (config.settingsInterface.UsebackgroundImageUrl)
                AddImage(config.settingsInterface.backgroundImageUrl, "CustomBackgroundImage");
                
            AddImage("https://i.imgur.com/hrgfWyQ.png", "1");
            AddImage("https://i.imgur.com/f8f5JvP.png", "2");
            AddImage("https://i.imgur.com/uXkWpqc.png", "3");

            AddImage("https://i.imgur.com/k34NkdO.png", "4");
            AddImage("https://i.imgur.com/Zu5eCrs.png", "5");
            AddImage("https://i.imgur.com/1BHMfvg.png", "6");

            AddImage("https://i.imgur.com/8Zz7UgI.png", "7");
            AddImage("https://i.imgur.com/XqjkkZe.jpg", "10");
            AddImage("https://i.imgur.com/hLBbDvt.jpg", "11");

            AddDisplayName();
            cmd.AddChatCommand(config.settings.chatCommandOpenStat, this, nameof(MainMenuStat));
            cmd.AddConsoleCommand(config.settings.consoleCommandOpenStat, this, nameof(ConsoleCommandOpenMenu));

            #region PermReg
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permAvailability, this);
            permission.RegisterPermission(permReset, this);
            #endregion
            timer.Every(config.settings.dataSaveTime * 60, () => { SaveData(); SaveDataIgnoreList(); SaveDataPrize(); });
        }
        #endregion

        #region UI
        public const string UI_INTERFACE = "INTERFACE_STATS";
        public const string UI_MENU_BUTTON = "MENU_BUTTON";
        public const string UI_USER_STAT = "USER_STAT";
        public const string UI_USER_STAT_INFO = "USER_STAT_INFO";
        public const string UI_CLOSE_MENU = "CLOSE_MENU";
        public const string UI_SEARCH_USER = "SEARCH_USER";
        public const string UI_TOP_TEN_USER = "TOP_TEN_USER";

        private void CloseLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_USER_STAT);
            CuiHelper.DestroyUi(player, UI_SEARCH_USER);
            CuiHelper.DestroyUi(player, UI_TOP_TEN_USER);
        }

        #region MainPage
        private void MainMenuStat(BasePlayer player)
        {
            if (IgnoreReservedPlayer.ContainsKey(player.userID))
            {
                PrintToChat(player, GetLang("STAT_ADMIN_HIDE_STAT", player.UserIDString));
                return;
            }
            string background = GetImage("1");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", UI_INTERFACE);

            container.Add(new CuiElement
            {
                Name = "BACKGROUND",
                Parent = UI_INTERFACE,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = background },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiElement
            {
                Name = UI_CLOSE_MENU,
                Parent = "BACKGROUND",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("3") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-63.354 -36.798", OffsetMax = "-53.246 -27.942" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = UI_INTERFACE, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, UI_CLOSE_MENU);

            CuiHelper.DestroyUi(player, UI_INTERFACE);
            CuiHelper.AddUi(player, container);
            MenuButton(player);
        }
        private void MenuButton(BasePlayer player, int page = 0)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-132.238 -75.531", OffsetMax = "122.197 -48.564" }
            }, UI_INTERFACE, UI_MENU_BUTTON);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-0.001 -13.483", OffsetMax = "88.113 13.484" },
                Button = { Command = $"UI_HandlerStat Page_swap 0", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_MY_STAT", player.UserIDString), FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_MENU_BUTTON, "BUTTON_MY_STAT");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = page == 0 ? "0.2988604 0.6886792 0.120194 0.6431373" : "1 1 1 0.3" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-44.057 -13.483", OffsetMax = "44.057 -11.642" }
            }, "BUTTON_MY_STAT", "Panel_8193");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-39.106 -13.483", OffsetMax = "65.745 13.484" },
                Button = { Command = $"UI_HandlerStat Page_swap 1", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_TOP_TEN", player.UserIDString), FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_MENU_BUTTON, "BUTTON_TOPTEN_USER");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = page == 1 ? "0.2988604 0.6886792 0.120194 0.64313732" : "1 1 1 0.3" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-52.425 -13.484", OffsetMax = "52.425 -11.642" }
            }, "BUTTON_TOPTEN_USER", "Panel_8193");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-61.472 -13.483", OffsetMax = "0.216 13.484" }
            }, UI_MENU_BUTTON, "BUTTON_PAGE_SEARCH");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = page == 2 ? "0.2988604 0.6886792 0.120194 0.6431373" : "1 1 1 0.3" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30.844 -13.484", OffsetMax = "30.845 -11.642" }
            }, "BUTTON_PAGE_SEARCH");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30.844 -8.36", OffsetMax = "12.862 8.361" },
                Text = { Text = GetLang("STAT_UI_SEARCH", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "BUTTON_PAGE_SEARCH");

            container.Add(new CuiElement
            {
                Parent = "BUTTON_PAGE_SEARCH",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/examine.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "12.862 -6.5", OffsetMax = "25.862 6.5" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = $"UI_HandlerStat Page_swap 2", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "BUTTON_PAGE_SEARCH");

            CuiHelper.DestroyUi(player, UI_MENU_BUTTON);
            CuiHelper.AddUi(player, container);
            if (page == 0)
                UserStat(player);
            else if (page == 1)
                TopTen(player);
            else
                SearchPageUser(player);
        }
        #endregion

        #region SearchPage
        private void SearchPageUser(BasePlayer player, string target = "")
        {
            string SearchName = "";
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-378.454 -264.835", OffsetMax = "381.998 266.939" }
            }, UI_INTERFACE, UI_SEARCH_USER);

            container.Add(new CuiElement
            {
                Name = "SEARCH_LINE",
                Parent = UI_SEARCH_USER,
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.1" },
                    new CuiRectTransformComponent {AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-181.67 -31.1", OffsetMax = "149.27 -7.1" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "LOUPE_SEARCH_IMG",
                Parent = "SEARCH_LINE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/examine.png" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.87 -10", OffsetMax = "33.87 10" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "INPUT_SEARCH",
                Parent = "SEARCH_LINE",
                Components = {
                    new CuiInputFieldComponent { Text = SearchName, Command = $"UI_HandlerStat listplayer {SearchName}", Color = "1 1 1 1", FontSize = 10, Align = TextAnchor.MiddleLeft },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-119.86 -9.314", OffsetMax = "129.03 9.591" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-181.67 -1.95", OffsetMax = "149.27 212.35" }
            }, UI_SEARCH_USER, "LIST_USER_SEARCH");

            int y = 0, x = 0;
            foreach (var players in Players.Where(z => z.Value.Name.ToLower().Contains(target)))
            {
                string LockStatus = players.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = players.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {players.Key}";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {players.Key}";

                container.Add(new CuiElement
                {
                    Name = "USER_IN_SEARCH",
                    Parent = "LIST_USER_SEARCH",
                    Components = {
                        new CuiImageComponent { Color = "1 1 1 0.1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-164.971 + (x * 112.586)} {84.138 - (y * 26.281)}", OffsetMax = $"{-62.801 + (x * 112.586)} {105.623 - (y * 26.281)}" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "USER_HIDE_PROFILE",
                    Parent = "USER_IN_SEARCH",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45.68 -7.5", OffsetMax = "-30.68 7.5" }
                }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26.832 -10.743", OffsetMax = "48.365 10.743" },
                    Text = { Text = GetCorrectName(players.Value.Name, 14), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_IN_SEARCH");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = ""}
                }, "USER_IN_SEARCH");

                x++;
                if (x == 3)
                {
                    x = 0;
                    y++;
                    if (y == 8)
                        break;
                }
            }
            CloseLayer(player);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UserInfoPage
        private void UserStat(BasePlayer player, ulong target = 0)
        {
            PlayerInfo statInfo = PlayerInfo.Find(target == 0 ? player.userID : target);
            ulong userid = target == 0 ? player.userID : target;
            string color = BasePlayer.FindByID(userid) != null ? "0.55 0.78 0.24 1" : "0.8 0.28 0.2 1";
            int kills = config.settings.pveServerMode ? statInfo.pVP.KillsNpc : statInfo.pVP.Kills;
            string titleKills = config.settings.pveServerMode ? GetLang("STAT_UI_PVP_KILLS_NPC", player.UserIDString) : GetLang("STAT_UI_PVP_KILLS", player.UserIDString);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.254 -331.554", OffsetMax = "393.974 274.446" }
            }, UI_INTERFACE, UI_USER_STAT);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.254 -331.554", OffsetMax = "393.974 274.446" }
            }, UI_USER_STAT, UI_USER_STAT_INFO);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "14.908 -69.438", OffsetMax = "227.492 -53.162" },
                Text = { Text = GetLang("STAT_UI_INFO", player.UserIDString, statInfo.Name), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "INFO_USER_NICK");

            container.Add(new CuiElement
            {
                Name = "USER_AVATAR_LAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = color },
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15 -130", OffsetMax = "68 -77" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "AVATAR_ON_STEAM",
                Parent = "USER_AVATAR_LAYER",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage(target == 0 ? player.UserIDString : target.ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15.477 -179.883", OffsetMax = "87.323 -161.717" },
                Text = { Text = GetLang("STAT_UI_ACTIVITY", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "USER_ACTIVE");

            if (target == 0 && (config.settings.availabilityUse && (permission.UserHasPermission(player.UserIDString, permAvailability) ||  config.settings.dropStatUse && permission.UserHasPermission(player.UserIDString, permReset))))
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15.477 -290.083", OffsetMax = "87.323 -271.917" },
                    Text = { Text = GetLang("STAT_UI_SETTINGS", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, UI_USER_STAT_INFO, "USER_SETINGS");
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "14.8 -151.281", OffsetMax = "227.38 -134.319" },
                Text = { Text = GetLang("STAT_UI_PLACE_TOP", player.UserIDString, GetTopScore(target == 0 ? player.userID : target)), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, UI_USER_STAT_INFO, "TOP_IN_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "14.69 -201.181", OffsetMax = "227.27 -184.219" },
                Text = { Text = GetLang("STAT_UI_ACTIVITY_TODAY", player.UserIDString, TimeHelper.FormatTime(TimeSpan.FromMinutes(statInfo.playedTime.PlayedToday), 5, lang.GetLanguage(player.UserIDString))), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, UI_USER_STAT_INFO, "TODAY_ACTIVE_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-382.31 79.219", OffsetMax = "-169.73 96.181" },
                Text = { Text = GetLang("STAT_UI_ACTIVITY_TOTAL", player.UserIDString, TimeHelper.FormatTime(TimeSpan.FromMinutes(statInfo.playedTime.PlayedForWipe), 5, lang.GetLanguage(player.UserIDString))), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, UI_USER_STAT_INFO, "ALLTIME_ACTIVE_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-382.31 56.919", OffsetMax = "-169.73 73.881" },
                Text = { Text = GetLang("STAT_UI_SCORE", player.UserIDString, statInfo.Score.ToString("0.0")), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, UI_USER_STAT_INFO, "SCORE_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.5 -67.825", OffsetMax = "-130.043 -53.66" },
                Text = { Text = GetLang("STAT_UI_PVP", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "PVP_STAT_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-209.505 36.618", OffsetMax = "-13.975 50.782" },
                Text = { Text = GetLang("STAT_UI_FAVORITE_WEAPON", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "RIFLE_FAVORITE_USER");

            #region KillStat
            container.Add(new CuiElement
            {
                Name = "KILL_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.389 -123.243", OffsetMax = "-14.469 -78.4" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "19 -7.014", OffsetMax = "97 8.414" },
                Text = { Text = titleKills, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "KILL_STAT_PLAYER", "LABEL_KILL_AMOUNT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-98 -7.014", OffsetMax = "-16.845 8.414" },
                Text = { Text = kills.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8538514 0.8491456 0.88679242835 1" }
            }, "KILL_STAT_PLAYER", "LABEL_KILL_AMOUNTTWO");
            #endregion

            #region DeathStat
            container.Add(new CuiElement
            {
                Name = "KILLSHOT_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.389 -173.691", OffsetMax = "-14.469 -128.849" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "19 -7.014", OffsetMax = "97 8.414" },
                Text = { Text = GetLang("STAT_UI_PVP_DEATH", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "KILLSHOT_STAT_PLAYER", "LABEL_KILL_AMOUNT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-98 -7.014", OffsetMax = "-16.845 8.414" },
                Text = { Text = statInfo.pVP.Deaths.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, "KILLSHOT_STAT_PLAYER", "LABEL_KILL_AMOUNTTWO");
            #endregion

            #region KDRStat
            container.Add(new CuiElement
            {
                Name = "DEATCH_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.39 -224.721", OffsetMax = "-14.47 -179.879" }
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "19 -7.014", OffsetMax = "97 8.414" },
                Text = { Text = GetLang("STAT_UI_PVP_KDR", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "DEATCH_STAT_PLAYER", "LABEL_KILL_AMOUNT");
            float kdr = statInfo.pVP.Deaths == 0 ? kills : (float)Math.Round(((float)kills) / statInfo.pVP.Deaths, 2);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-98 -7.014", OffsetMax = "-16.845 8.414" },
                Text = { Text = kdr.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, "DEATCH_STAT_PLAYER", "LABEL_KILL_AMOUNTTWO");
            #endregion

            #region FavoriteWeapon
            container.Add(new CuiElement
            {
                Name = "FAVORITE_WEAPON_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-210.15 -324.607", OffsetMax = "-13.977 -278.569" }
                }
            });

            var weaponTop = statInfo.weapon.WeaponUsed.OrderByDescending(x => x.Value.Kills).Take(1).FirstOrDefault();
            if (weaponTop.Key != null)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "46.321 -16.5", OffsetMax = "176.509 16.5" },
                    Text = { Text = GetLang("STAT_UI_FAVORITE_WEAPON_KILLS", player.UserIDString, weaponTop.Value.Kills, weaponTop.Value.Shots), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "FAVORITE_WEAPON_STAT_PLAYER", "LABEL_KILL_AMOUNT");

                container.Add(new CuiElement
                {
                    Name = "WEAPON_IMG_USER",
                    Parent = "FAVORITE_WEAPON_STAT_PLAYER",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(weaponTop.Key) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
            }
            else
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = GetLang("STAT_UI_FAVORITE_WEAPON_NOT_DATA", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "FAVORITE_WEAPON_STAT_PLAYER", "LABEL_KILL_AMOUNT");
            }
            #endregion

            #region OtherStat

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-209.498 -63.383", OffsetMax = "-79.669 -49.219" },
                Text = { Text = GetLang("STAT_UI_OTHER_STAT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "OTHER_STAT_LABEL");

            #endregion

            CloseLayer(player);
            CuiHelper.DestroyUi(player, "USER_STAT");
            CuiHelper.AddUi(player, container);
            CategoryStatUser(player, target);
            OtherStatUser(player, target);
            if (target == 0)
            {
                if (config.settings.dropStatUse && permission.UserHasPermission(player.UserIDString, permReset))
                    ButtonDropStat(player, statInfo);
                if (config.settings.availabilityUse && permission.UserHasPermission(player.UserIDString, permAvailability))
                    ButtonHideStat(player, statInfo);
            }
        }
        private void ButtonHideStat(BasePlayer player, PlayerInfo info)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.865 -8.619", OffsetMax = "160.34 6.226" }
            }, UI_USER_STAT_INFO, "BUTTON_HIDE_STAT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-120.779 -7.423", OffsetMax = "21.525 9.815" },
                Text = { Text = GetLang("STAT_UI_HIDE_STAT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, "BUTTON_HIDE_STAT", "LABEL_HIDE_USER");


            if (!info.HidedStatistics)
            {
                container.Add(new CuiElement
                {
                    Name = "CHECK_BOX_HIDE",
                    Parent = "BUTTON_HIDE_STAT",
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("5")},
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.381 -6.404", OffsetMax = "14.381 6.596" }
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "CHECK_BOX_HIDE",
                    Parent = "BUTTON_HIDE_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("4")},
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.381 -6.404", OffsetMax = "14.381 6.596" }
                }
                });
            }
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "UI_HandlerStat hidestat", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "BUTTON_HIDE_STAT");

            CuiHelper.DestroyUi(player, "BUTTON_HIDE_STAT");
            CuiHelper.AddUi(player, container);
        }

        private void DialogConfirmationDropStat(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "CONFIRMATIONS",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.05" },
                    new CuiRectTransformComponent {AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.64 -104.387", OffsetMax = "160.12 -45.246" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-73.24 -30", OffsetMax = "73.24 -0.43" },
                Text = { Text = GetLang("STAT_UI_CONFIRM", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "CONFIRMATIONS");

            container.Add(new CuiElement
            {
                Name = "CONFIRMATIONS_YES",
                Parent = "CONFIRMATIONS",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.05"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "11.28 6.7", OffsetMax = "46.28 23.7" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22.5 -11", OffsetMax = "22.5 11" },
                Button = { Command = "UI_HandlerStat confirm_yes", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CONFIRM_YES", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "CONFIRMATIONS_YES", "LABEL_YES");

            container.Add(new CuiElement
            {
                Name = "CONFIRMATIONS_NO",
                Parent = "CONFIRMATIONS",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.05" },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-46.8 6.7", OffsetMax = "-11.8 23.7" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22.5 -11", OffsetMax = "22.5 11" },
                Button = { Close = "CONFIRMATIONS", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CONFIRM_NO", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "CONFIRMATIONS_NO", "LABEL_NO");

            CuiHelper.DestroyUi(player, "CONFIRMATIONS");
            CuiHelper.AddUi(player, container);
        }

        private void ButtonDropStat(BasePlayer player, PlayerInfo info)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.75 -29.559", OffsetMax = "160.23 -15.727" }
            }, UI_USER_STAT_INFO, "BUTTON_REFRESH_STAT");

            container.Add(new CuiElement
            {
                Name = "USER_REFRESH_STAT",
                Parent = "BUTTON_REFRESH_STAT",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/clear_list.png" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.74 -6.5", OffsetMax = "14.74 6.5" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-120.55 -6.916", OffsetMax = "21.75 8.202" },
                Text = { Text = GetLang("STAT_UI_RESET_STAT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" }
            }, "BUTTON_REFRESH_STAT", "LABEL_REFRESH_USER");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "UI_HandlerStat confirm", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "BUTTON_REFRESH_STAT");

            CuiHelper.DestroyUi(player, "BUTTON_REFRESH_STAT");
            CuiHelper.AddUi(player, container);
        }

        private void OtherStatUser(BasePlayer player, ulong target = 0, int statType = 0)
        {
            var container = new CuiElementContainer();
            PlayerInfo statInfo = PlayerInfo.Find(target == 0 ? player.userID : target);
            if (statType == 0)
            {
                #region CrateStat
                container.Add(new CuiElement
                {
                    Name = "CRATE_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 181.391", OffsetMax = "-13.974 227.429" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.014", OffsetMax = "176.509 8.414" },
                    Text = { Text = GetLang("STAT_UI_CRATE_OPEN", player.UserIDString, statInfo.otherStat.CrateOpen), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "CRATE_STAT");

                container.Add(new CuiElement
                {
                    Parent = "CRATE_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("cratecostume") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
                #endregion

                #region BarrelStat
                container.Add(new CuiElement
                {
                    Name = "BARREL_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 129.171", OffsetMax = "-13.974 175.209" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.014", OffsetMax = "176.509 8.414" },
                    Text = { Text = GetLang("STAT_UI_BARREL_KILL", player.UserIDString, statInfo.otherStat.BarrelDeath), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "BARREL_STAT");

                container.Add(new CuiElement
                {
                    Parent = "BARREL_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage("barrelcostume") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
                #endregion

                #region AnimalKillStat
                container.Add(new CuiElement
                {
                    Name = "ANIMALKILL_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 76.951", OffsetMax = "-13.974 122.989" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.014", OffsetMax = "176.509 8.414" },
                    Text = { Text = GetLang("STAT_UI_ANIMAL_KILL", player.UserIDString, statInfo.otherStat.AnimalsKill), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "ANIMALKILL_STAT");

                container.Add(new CuiElement
                {
                    Parent = "ANIMALKILL_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("skull.wolf") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
                #endregion
            }
            else
            {
                #region HeliStat
                container.Add(new CuiElement
                {
                    Name = "Heli_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 181.391", OffsetMax = "-13.974 227.429" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.014", OffsetMax = "176.509 8.414" },
                    Text = { Text = GetLang("STAT_UI_HELI_KILL", player.UserIDString, statInfo.pVP.HeliKill), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "Heli_STAT");

                container.Add(new CuiElement
                {
                    Parent = "Heli_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("10") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
                #endregion

                #region BradleyStat
                container.Add(new CuiElement
                {
                    Name = "BRADLEY_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 129.171", OffsetMax = "-13.974 175.209" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.014", OffsetMax = "176.509 8.414" },
                    Text = { Text = GetLang("STAT_UI_BRADLEY_KILL", player.UserIDString, statInfo.pVP.BradleyKill), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "BRADLEY_STAT");

                container.Add(new CuiElement
                {
                    Parent = "BRADLEY_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage("11") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
                #endregion

                #region NpcKillStat
                container.Add(new CuiElement
                {
                    Name = "NPCKILL_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 76.951", OffsetMax = "-13.974 122.989" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.014", OffsetMax = "176.509 8.414" },
                    Text = { Text = GetLang("STAT_UI_NPC_KILL", player.UserIDString, statInfo.pVP.KillsNpc), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "NPCKILL_STAT");

                container.Add(new CuiElement
                {
                    Name = "NPC_IMG_USER",
                    Parent = "NPCKILL_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("scientistsuit_heavy") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
                #endregion
            }

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0.1", Command = $"UI_HandlerStat ShowMoreStat {target} {statType}" },
                Text = { Text = GetLang("STAT_UI_BTN_MORE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0.8538514 0.8491456 0.8867924 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "186.97 -254.682", OffsetMax = "383.15 -233.6" }
            }, UI_USER_STAT_INFO, "SHOW_MORE_STAT");

            CuiHelper.DestroyUi(player, "SHOW_MORE_STAT");
            CuiHelper.DestroyUi(player, "CRATE_STAT");
            CuiHelper.DestroyUi(player, "BARREL_STAT");
            CuiHelper.DestroyUi(player, "NPCKILL_STAT");
            CuiHelper.DestroyUi(player, "Heli_STAT");
            CuiHelper.DestroyUi(player, "BRADLEY_STAT");
            CuiHelper.DestroyUi(player, "ANIMALKILL_STAT");
            CuiHelper.AddUi(player, container);
        }

        #region CategoryAndStat
       
        private void CategoryStatUser(BasePlayer player, ulong target = 0, int cat = 0)
        {
            var container = new CuiElementContainer();
            PlayerInfo statInfo = PlayerInfo.Find(target == 0 ? player.userID : target);
            var list = GetCategory(statInfo, cat);
            #region line
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5803922 0.572549 0.6117647 0.4313726" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"151.69 {181.046 - ((list.Count - 1) * 50.729)}", OffsetMax = $"153.21 225.49" }
            }, UI_USER_STAT_INFO, "STAT_LINE");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5803922 0.572549 0.6117647 1" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-0.761 {-23.343 - ((list.Count - 1) * 30.729)}", OffsetMax = "0.761 0.17" }
            }, "STAT_LINE", "STAT_LINE_CHILD");
            #endregion

            #region USER STAT BUTTON CATEGORY
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-146.988 -71.4", OffsetMax = "146.388 -53.16" }
            }, UI_USER_STAT_INFO, "MENU_USER_STAT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0.312 -9.12", OffsetMax = "84.704 9.121" },
                Button = { Command = $"UI_HandlerStat changeCategory {target} 0", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_GATHER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, }
            }, "MENU_USER_STAT", "Panel_5655");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = cat == 0 ? "0.2988604 0.6886792 0.120194 0.6431373" : "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-42.347 0", OffsetMax = "42.196 1.871" }
            }, "Panel_5655", "Panel_8052");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-61.983 -9.121", OffsetMax = "31.232 9.12" },
                Button = { Command = $"UI_HandlerStat changeCategory {target} 1", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_EXPLOSED", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, }
            }, "MENU_USER_STAT", "Panel_56551");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = cat == 1 ? "0.2988604 0.6886792 0.120194 0.6431373" : "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-46.608 0", OffsetMax = "46.608 1.871" }
            }, "Panel_56551", "Panel_8052");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-115.459 -9.12", OffsetMax = "0.001 9.121" },
                Button = { Command = $"UI_HandlerStat changeCategory {target} 2", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_PLANT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, }
            }, "MENU_USER_STAT", "Panel_56553");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = cat == 2 ? "0.2988604 0.6886792 0.120194 0.6431373" : "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-57.73 0", OffsetMax = "57.73 1.871" }
            }, "Panel_56553", "Panel_8052");
            #endregion

            #region USER STAT CATEGORY
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-146.991 -124.121", OffsetMax = "146.389 226.031" }
            }, "USER_STAT_INFO", "MAIN_LIST_STAT_USER");

            int y = 0;
            string userLang = lang.GetLanguage(player.UserIDString);
            foreach (var item in list)
            {
                float fade = 0.15f * y;
                string itemName = string.Empty;
                if (ItemName.ContainsKey(item.Key))
                {
                    itemName = userLang == "ru" ? ItemName[item.Key].ru : ItemName[item.Key].en;
                }

                container.Add(new CuiElement
                {
                    Name = "STAT_USER_LINE",
                    Parent = "MAIN_LIST_STAT_USER",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 0.1", FadeIn = fade },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-146.118 {-44.977 - (y * 50.729)}", OffsetMax = $"146.692 {-0.765 - (y * 50.729)}" }
                }
                });
                string name = cat == 0 ? item.Value.ToString("0,0", CultureInfo.InvariantCulture) : item.Value.ToString();

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "26.03 -10.534", OffsetMax = "126.03 12.574" },
                    Text = { Text = name, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8538514 0.8491456 0.8867924 1", FadeIn = fade }
                }, "STAT_USER_LINE", "STAT_USER_AMOUNT");

                if (item.Key == "all")
                {
                    string langGet = cat == 0 ? "STAT_USER_TOTAL_GATHERED" : cat == 1 ? "STAT_USER_TOTAL_EXPLODED" : "STAT_USER_TOTAL_GROWED";

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-128 -10.534", OffsetMax = "50 12.574" },
                        Text = { Text = GetLang(langGet, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", FadeIn = fade }
                    }, "STAT_USER_LINE", "ALL_TOTAL");
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "IMAGE_ITEM",
                        Parent = "STAT_USER_LINE",
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage(item.Key), FadeIn = fade },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-128 -17.5", OffsetMax = "-93 17.5" }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-73.17 -10.534", OffsetMax = "50 12.574" },
                        Text = { Text = itemName, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8538514 0.8491456 0.8867924 1" , FadeIn = fade}
                    }, "STAT_USER_LINE");
                }
                y++;
            }

            #endregion

            CuiHelper.DestroyUi(player, "MENU_USER_STAT");
            CuiHelper.DestroyUi(player, "MAIN_LIST_STAT_USER");
            CuiHelper.DestroyUi(player, "STAT_LINE");
            CuiHelper.AddUi(player, container);
        }
        #endregion
        #endregion

        #region TopTenUserStatPage
        private void TopTen(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -360.001", OffsetMax = "640 266.939" }
            }, UI_INTERFACE, UI_TOP_TEN_USER);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-599.3 -237.224", OffsetMax = "589.334 271.441" }
            }, UI_TOP_TEN_USER, "TOP_10_TABLE");

            if (config.settings.pveServerMode)
            {
                #region KillerNPC
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.2 -20.655", OffsetMax = "176.343 -3.545" },
                    Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_KILLER_ANIMALS", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "TOP_10_TABLE");

                container.Add(new CuiElement
                {
                    Name = "TOP_TABLE_0",
                    Parent = "TOP_10_TABLE",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.575 -441.751", OffsetMax = "177.214 -26.861" }
                }
                });
                var AnimalKillerList = Players.OrderByDescending(x => x.Value.otherStat.AnimalsKill).Take(10);

                int y = 0;

                foreach (var item in AnimalKillerList)
                {

                    string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                    string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                    if (permission.UserHasPermission(player.UserIDString, permAdmin))
                        Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                    string color = y == 0 ? config.settingsInterface.ColorTop1 : y == 1 ? config.settingsInterface.ColorTop2 : y == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = color },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (y * 42.863)}", OffsetMax = $"83.82 {207.448 - (y * 42.863)}" }
                    }, "TOP_TABLE_0", "USER_INFO");
                    container.Add(new CuiElement
                    {
                        Name = "USER_STAT_HIDE",
                        Parent = "USER_INFO",
                        Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                        Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                    }, "USER_INFO");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                        Text = { Text = item.Value.otherStat.AnimalsKill.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                    }, "USER_INFO");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Command = Command, Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, "USER_INFO");
                    y++;
                }
                #endregion
            }
            else
            {
                #region Killer

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.2 -20.655", OffsetMax = "176.343 -3.545" },
                    Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_KILLER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "TOP_10_TABLE");

                container.Add(new CuiElement
                {
                    Name = "TOP_TABLE_0",
                    Parent = "TOP_10_TABLE",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.575 -441.751", OffsetMax = "177.214 -26.861" }
                }
                });
                var KillerList = Players.OrderByDescending(x => x.Value.pVP.Kills).Take(10);

                int y = 0;

                foreach (var item in KillerList)
                {

                    string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                    string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                    if (permission.UserHasPermission(player.UserIDString, permAdmin))
                        Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                    string color = y == 0 ? config.settingsInterface.ColorTop1 : y == 1 ? config.settingsInterface.ColorTop2 : y == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = color },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (y * 42.863)}", OffsetMax = $"83.82 {207.448 - (y * 42.863)}" }
                    }, "TOP_TABLE_0", "USER_INFO");
                    container.Add(new CuiElement
                    {
                        Name = "USER_STAT_HIDE",
                        Parent = "USER_INFO",
                        Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                        Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                    }, "USER_INFO");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                        Text = { Text = item.Value.pVP.Kills.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                    }, "USER_INFO");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Command = Command, Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, "USER_INFO");
                    y++;
                }
                #endregion
            }

            #region Npc_Killer
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "209.129 -20.655", OffsetMax = "376.271 -3.545" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_NPCKILLER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_1",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "210.281 -441.751", OffsetMax = "377.919 -26.861" }
                }
            });

            var KillerNpcList = Players.OrderByDescending(x => x.Value.pVP.KillsNpc).Take(10);

            int i = 0;
            foreach (var item in KillerNpcList)
            {
                
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = i == 0 ? config.settingsInterface.ColorTop1 : i == 1 ? config.settingsInterface.ColorTop2 : i == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (i * 42.863)}", OffsetMax = $"83.82 {207.448 - (i * 42.863)}" }
                }, "TOP_TABLE_1", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                    Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_INFO");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                    Text = { Text = item.Value.pVP.KillsNpc.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                i++;
            }
            #endregion

            #region Time

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "409.229 -20.655", OffsetMax = "576.371 -3.545" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_TIME", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_2",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "409.231 -441.751", OffsetMax = "576.869 -26.861" }
                }
            });
            var TimeList = Players.OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(10);

            int c = 0;
            foreach (var item in TimeList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = c == 0 ? config.settingsInterface.ColorTop1 : c == 1 ? config.settingsInterface.ColorTop2 : c == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (c * 42.863)}", OffsetMax = $"83.82 {207.448 - (c * 42.863)}" }
                }, "TOP_TABLE_2", "USER_INFO");
                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                    Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_INFO");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                    Text = { Text = TimeHelper.FormatTime(TimeSpan.FromMinutes(item.Value.playedTime.PlayedForWipe), 5, lang.GetLanguage(player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                c++;
            }
            #endregion

            #region Farm
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-580.571 -20.655", OffsetMax = "-413.429 -3.545" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_GATHER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_3",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-580.099 -441.751", OffsetMax = "-412.461 -26.861" }
                }
            });

            var FarmList = Players.OrderByDescending(x => x.Value.gather.AllGathered).Take(10);
            int f = 0;

            foreach (var item in FarmList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = f == 0 ? config.settingsInterface.ColorTop1 : f == 1 ? config.settingsInterface.ColorTop2 : f == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (f * 42.863)}", OffsetMax = $"83.82 {207.448 - (f * 42.863)}" }
                }, "TOP_TABLE_3", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                    Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_INFO");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                    Text = { Text = item.Value.gather.AllGathered.ToString("0,0", CultureInfo.InvariantCulture), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                f++;
            }

            #endregion

            #region Explosion

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-381.151 -20.655", OffsetMax = "-214.009 -3.545" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_EXPLOSED", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");


            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_5",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-380.679 -441.751", OffsetMax = "-213.041 -26.861"  }
                }
            });
            

            var ExpList = Players.OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(10);
            int z = 0;
            foreach (var item in ExpList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = z == 0 ? config.settingsInterface.ColorTop1 : z == 1 ? config.settingsInterface.ColorTop2 : z == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (z * 42.863)}", OffsetMax = $"83.82 {207.448 - (z * 42.863)}" }
                }, "TOP_TABLE_5", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                        new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                    Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_INFO");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                    Text = { Text = item.Value.explosion.AllExplosionUsed.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                z++;
            }
            #endregion

            #region Score
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-180.871 -20.655", OffsetMax = "-13.729 -3.545" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_SCORE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_4",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 0.15" },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-180.419 -441.751", OffsetMax = "-12.781 -26.861"}
                }
            });

            var ScoreList = Players.OrderByDescending(x => x.Value.Score).Take(10);
            int s = 0;

            foreach (var item in ScoreList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = s == 0 ? config.settingsInterface.ColorTop1 : s == 1 ? config.settingsInterface.ColorTop2 : s == 2 ? config.settingsInterface.ColorTop3 : "0 0 0 0";
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (s * 42.863)}", OffsetMax = $"83.82 {207.448 - (s * 42.863)}" }
                }, "TOP_TABLE_4", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                        new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                    }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.822 -6.724", OffsetMax = "7.522 6.724" },
                    Text = { Text = GetCorrectName(item.Value.Name, 17), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_INFO");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.724", OffsetMax = "76.934 6.724" },
                    Text = { Text = item.Value.Score.ToString("0.00"), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                s++;
            }
            #endregion

            CloseLayer(player);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region OtherMetods
        private void SendMsgRewardWipe(BasePlayer player)
        {
            var playerGrant = PrizePlayerData[player.userID];

            foreach (var item in playerGrant)
            {
                switch (item.catType)
                {
                    case CatType.score:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_SCORE", player.UserIDString, item.top));
                        break;
                    case CatType.killer:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_KILL", player.UserIDString, item.top));
                        break;
                    case CatType.time:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_TIME", player.UserIDString, item.top));
                        break;
                    case CatType.farm:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_FARM", player.UserIDString, item.top));
                        break;
                    case CatType.raid:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_EXP", player.UserIDString, item.top));
                        break;
                    case CatType.killerNPC:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_KILL_NPC", player.UserIDString, item.top));
                        break;
                }
            }
            NextTick(() => { PrizePlayerData.Remove(player.userID); SaveDataPrize(); });
        }
        private void ParseTopUserForPrize()
        {
            PrizePlayerData.Clear();

            List<CatType> catTypes = new List<CatType> { CatType.score, CatType.killer, CatType.time, CatType.farm,  CatType.raid, };
            foreach (var cat in catTypes)
            {
                switch (cat)    
                {
                    case CatType.score:
                        {
                            int countReward = config.settingsPrize.prizeScore.Count;
                            if (countReward != 0)
                            {
                                var score = Players.OrderByDescending(x => x.Value.Score).Take(countReward);
                                int top = 1;
                                foreach (var item in score)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = (int)item.Value.Score, top = top });
                                    config.settingsPrize.prizeScore.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                }
                            }
                            break;
                        }
                    case CatType.killer:
                        {
                            int countReward = config.settingsPrize.prizeKiller.Count;
                            if (countReward != 0)
                            {
                                var killer = Players.OrderByDescending(x => x.Value.pVP.Kills).Take(countReward);
                                int top = 1;
                                foreach (var item in killer)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = (int)item.Value.pVP.Kills, top = top });
                                    config.settingsPrize.prizeKiller.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                }
                            }
                            break;
                        }
                    case CatType.time:
                        {
                            int countReward = config.settingsPrize.prizeTime.Count;
                            if (countReward != 0)
                            {
                                var time = Players.OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(countReward);
                                int top = 1;
                                foreach (var item in time)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = (int)item.Value.playedTime.PlayedForWipe, top = top });
                                    config.settingsPrize.prizeTime.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                }
                            }
                            break;
                        }
                    case CatType.farm:
                        {
                            int countReward = config.settingsPrize.prizeFarm.Count;
                            if (countReward != 0)
                            {
                                var farm = Players.OrderByDescending(x => x.Value.gather.AllGathered).Take(countReward);
                                int top = 1;
                                foreach (var item in farm)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.gather.AllGathered, top = top });
                                    config.settingsPrize.prizeFarm.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                }
                            }
                            break;
                        }
                    case CatType.raid:
                        {
                            int countReward = config.settingsPrize.prizeRaid.Count;
                            if (countReward != 0)
                            {
                                var raid = Players.OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(countReward);
                                int top = 1;
                                foreach (var item in raid)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.explosion.AllExplosionUsed, top = top });
                                    config.settingsPrize.prizeRaid.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                }
                            }
                            break;
                        }
                    case CatType.killerNPC:
                        {
                            int countReward = config.settingsPrize.prizeNPCKiller.Count;
                            if (countReward != 0)
                            {
                                var killerNpc = Players.OrderByDescending(x => x.Value.pVP.KillsNpc).Take(countReward);
                                int top = 1;
                                foreach (var item in killerNpc)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.pVP.KillsNpc, top = top });
                                    config.settingsPrize.prizeNPCKiller.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                }
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            SaveDataPrize();

        }
        private void PlayersTopOnChat()
        {
            int i = Core.Random.Range(0, 7);
            string playerstat = string.Empty;
            string langmsg = string.Empty;
            int top = 1;

            switch (i)
            {
                case 0:
                    {
                        if (config.settings.pveServerMode)
                        {
                            var playerList = Players.OrderByDescending(x => x.Value.pVP.KillsNpc).Take(5);
                            foreach (var playerInfo in playerList)
                            {
                                playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.pVP.KillsNpc}\n";
                                top++;
                            }
                            langmsg = "STAT_TOP_FIVE_KILL_NPC";
                        }
                        else
                        {
                            var playerList = Players.OrderByDescending(x => x.Value.pVP.Kills).Take(5);
                            foreach (var playerInfo in playerList)
                            {
                                playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.pVP.Kills}\n";
                                top++;
                            }
                            langmsg = "STAT_TOP_FIVE_KILL";
                        }                   
                        break;
                    }
                case 1:
                    {
                        var playerList = Players.OrderByDescending(x => x.Value.gather.AllGathered).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.gather.AllGathered.ToString("0,0", CultureInfo.InvariantCulture)}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_FARM";
                        break;
                    }
                case 2:
                    {
                        var playerList = Players.OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.explosion.AllExplosionUsed}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_EXPLOSION";
                        break;
                    }
                case 3:
                    {
                        var playerList = Players.OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {TimeHelper.FormatTime(TimeSpan.FromMinutes(playerInfo.Value.playedTime.PlayedForWipe), 5, lang.GetServerLanguage())}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_TIMEPLAYED";
                        break;
                    }
                case 4:
                    {
                        var playerList = Players.OrderByDescending(x => x.Value.otherStat.BuildingCrate).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.otherStat.BuildingCrate}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_BUILDINGS";
                        break;
                    }
                case 5:
                    {
                        var playerList = Players.OrderByDescending(x => x.Value.Score).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.Score.ToString("0.0")}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_SCORE";
                        break;
                    }
                case 6:
                    {
                        var playerList = Players.OrderByDescending(x => x.Value.harvesting.AllHarvesting).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.harvesting.AllHarvesting}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_FERMER";
                        break;
                    }
            }

            foreach (BasePlayer item in BasePlayer.activePlayerList)
            {
                item.ChatMessage(GetLang(langmsg, item.UserIDString, playerstat));
            }

            timer.Once(config.settings.chatSendTopTime, PlayersTopOnChat);
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb?.Clear();
            if (args != null)
            {
                sb?.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb?.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion

        #region ApiLoadData
        #region Classes
        public class items
        {
            public String shortName;
            public String ENdisplayName;
            public String RUdisplayName;
        }
        private class ItemDisplayName
        {
            public String ru;
            public String en;
        }
        #endregion
        private void AddDisplayName()
        {
            webrequest.Enqueue($"http://api.skyplugins.ru/api/getitemlist", "", (code, response) =>
            {
                if (code == 200)
                {
                    ItemList = JsonConvert.DeserializeObject<List<items>>(response);
                    for (int i = 0; i < ItemList.Count; i++)
                    {
                        items items = ItemList[i];
                        ItemName.Add(items.shortName, new ItemDisplayName { ru = items.RUdisplayName, en = items.ENdisplayName });
                    }
                }
            }, this);
        }
    
        void SteamAvatarAdd(string userid)
        {
            if (ImageLibrary == null)
                return;
            if (HasImage(userid))
                return;
            string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=3F2959BD838BF8FB544B9A767F873457&" + "steamids=" + userid;
            webrequest.Enqueue(url, null, (code, response) =>
            {
                if (code == 200)
                {
                    string Avatar = (string)JObject.Parse(response)["response"]?["players"]?[0]?["avatarfull"];
                    AddImage(Avatar, userid);
                }
            }, this);
        }
        #endregion

        #region Help
        private Dictionary<string, int> GetCategory(PlayerInfo statInfo, int cat)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            switch (cat)
            {
                case 0:
                    {
                        foreach (var item in statInfo.gather.GatheredTotal.Take(8))
                            result.Add(item.Key, item.Value);

                        result.Add("all", statInfo.gather.AllGathered);
                        return result;
                    }
                case 1:
                    {
                        foreach (var item in statInfo.explosion.ExplosionUsed.Take(8))
                            result.Add(item.Key, item.Value);

                        result.Add("all", statInfo.explosion.AllExplosionUsed);
                        return result;
                    }
                case 2:
                    {
                        foreach (var item in statInfo.harvesting.HarvestingList.OrderByDescending(x => x.Value).Take(9))
                            result.Add(item.Key, item.Value);

                        result.Add("all", statInfo.harvesting.AllHarvesting);
                        return result;
                    }
                default:
                    break;
            }
            return null;
        }
        private static List<KeyValuePair<ulong, PlayerInfo>> FindPlayers(string name)
        {
            var players = new List<KeyValuePair<ulong, PlayerInfo>>();

            var playersData = Players.Concat(IgnoreReservedPlayer);
            foreach (var activePlayer in playersData)
            {
                if (activePlayer.Value.Name.ToLower() == name)
                    players.Add(activePlayer);
                else if (activePlayer.Value.Name.ToLower().Contains(name))
                    players.Add(activePlayer);
            }

            return players;
        }
        private void CheckInMinute()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null || !player.userID.IsSteamId() || IgnoreReservedPlayer.ContainsKey(player.userID))
                    continue;
                PlayerInfo.AddPlayedTime(player.userID);
            }

            timer.Once(60f, CheckInMinute);
        }
        public static class TimeHelper
        {
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

                            result += $"{Format(time.Days, "д", "д", "д")}";
                            i++;
                        }
                        if (time.Hours != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Hours, "ч", "ч", "ч")}";
                            i++;
                        }
                        if (time.Minutes != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "м", "м", "м")}";
                            i++;
                        }
                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && i < maxSubstr)
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "с", "с", "с")}";
                                i++;
                            }
                        }
                        if (string.IsNullOrEmpty(result))
                            result = "0 секунд";
                        break;
                    default:
                        result = string.Format("{0}{1}{2}{3}",
                            time.Duration().Days > 0
                                ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Hours > 0
                                ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Minutes > 0
                                ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Seconds > 0
                                ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}"
                                : string.Empty);

                        if (result.EndsWith(", "))
                            result = result.Substring(0, result.Length - 2);

                        if (string.IsNullOrEmpty(result))
                            result = "0 seconds";
                        break;
                }
                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }
        }
        Int32 GetTopScore(ulong userid)
        {
            Int32 Top = 1;
            var RaitingNumber = Players.OrderByDescending(x => x.Value.Score);

            foreach (var Data in RaitingNumber)
            {
                if (Data.Key == userid)
                    break;
                Top++;
            }
            return Top;
        }
        private string GetCorrectName(string name, int length)
        {
            string res = name;
            if (name.Length > length)
                res = name.Substring(0, length);
            return res;
        }
        private void ExplosionProgressAdd(BasePlayer player, BaseEntity entity, string shortname = "")
        {
            string WeaponName = string.IsNullOrWhiteSpace(shortname) == true ? string.Empty : shortname;
            if (entity != null)
            {
                if (prefabID2Item.TryGetValue(entity.prefabID, out WeaponName) == false)
                {
                    prefabNameItem.TryGetValue(entity.ShortPrefabName, out WeaponName);
                }
            } 

            if (!string.IsNullOrEmpty(WeaponName))
            {
                PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                if (Playerstat.explosion.ExplosionUsed.ContainsKey(WeaponName))
                {
                    Playerstat.explosion.ExplosionUsed[WeaponName]++;
                    Playerstat.explosion.AllExplosionUsed++;
                    Playerstat.Score += config.settingsScore.ExplosionScore[WeaponName];
                }
            }
        }
        private void WeaponProgressAdd(PlayerInfo player, HitInfo hitinfo, bool kill = false)
        {
            if (hitinfo == null || hitinfo.WeaponPrefab == null)
                return;
            string WeaponName = string.Empty;
            if (prefabID2Item.TryGetValue(hitinfo.WeaponPrefab.prefabID, out WeaponName) == false)
            {
                prefabNameItem.TryGetValue(hitinfo.WeaponPrefab.ShortPrefabName, out WeaponName);
            }
            if (!string.IsNullOrEmpty(WeaponName))
            {
                if (!player.weapon.WeaponUsed.ContainsKey(WeaponName))
                {
                    player.weapon.WeaponUsed.Add(WeaponName, new PlayerInfo.Weapon.WeaponInfo { Kills = kill ? 1 : 0, Headshots = hitinfo.isHeadshot ? 1 : 0 });
                }
                else
                {
                    var weapon = player.weapon.WeaponUsed[WeaponName];
                    weapon.Headshots += hitinfo.isHeadshot ? 1 : 0;
                    weapon.Shots++;
                    if (kill)
                        weapon.Kills++;
                }
            }
        }
        private void ProgressAdd(BasePlayer player, Item item, bool scoreGive = false)
        {
            PlayerInfo Playerstat = PlayerInfo.Find(player.userID);

            switch (item.info.shortname)
            {
                case "wood":
                    {
                        Playerstat.gather.GatheredTotal[item.info.shortname] += item.amount;
                        Playerstat.gather.AllGathered += item.amount;
                        if(scoreGive)
                            Playerstat.Score += config.settingsScore.GatherScore[item.info.shortname];
                        break;
                    }
                case "stones":
                    {
                        Playerstat.gather.GatheredTotal[item.info.shortname] += item.amount;
                        Playerstat.gather.AllGathered += item.amount;
                        if (scoreGive)
                            Playerstat.Score += config.settingsScore.GatherScore[item.info.shortname];
                        break;
                    }
                case "metal.ore":
                    {
                        Playerstat.gather.GatheredTotal[item.info.shortname] += item.amount;
                        Playerstat.gather.AllGathered += item.amount;
                        if (scoreGive)
                            Playerstat.Score += config.settingsScore.GatherScore[item.info.shortname];
                        break;
                    }
                case "sulfur.ore":
                    {
                        Playerstat.gather.GatheredTotal[item.info.shortname] += item.amount;
                        Playerstat.gather.AllGathered += item.amount;
                        if (scoreGive)
                            Playerstat.Score += config.settingsScore.GatherScore[item.info.shortname];
                        break;
                    }
                case "hq.metal.ore":
                    {
                        Playerstat.gather.GatheredTotal[item.info.shortname] += item.amount;
                        Playerstat.gather.AllGathered += item.amount;
                        if (scoreGive)
                            Playerstat.Score += config.settingsScore.GatherScore[item.info.shortname];
                        break;
                    }
                case "scrap":
                    {
                        Playerstat.gather.GatheredTotal[item.info.shortname] += item.amount;
                        Playerstat.gather.AllGathered += item.amount;
                        Playerstat.Score += config.settingsScore.ScrapScore;
                        break;
                    }
            }
        }
        #endregion

        #region Command
        private void ConsoleCommandOpenMenu(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (IgnoreReservedPlayer.ContainsKey(arg.Player().userID))
            {
                PrintToChat(arg.Player(), GetLang("STAT_ADMIN_HIDE_STAT", arg.Player().UserIDString));
                return;
            }
            MainMenuStat(arg.Player());
        }
        [ConsoleCommand("stat.topuser")]
        private void CmdTestVkPlayer(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            var players = arg.Player();
            if (players != null && !permission.UserHasPermission(players.UserIDString, permAdmin))
            {
                PrintToConsole(players, GetLang("STAT_CMD_1", players.UserIDString));
                return;
            }
            List<CatType> catTypes = new List<CatType> { CatType.score, CatType.killer, CatType.time, CatType.farm, CatType.raid, };
            string txt = string.Empty;
            foreach (var cat in catTypes)
            {
                switch (cat)
                {
                    case CatType.score:
                        {
                            int countReward = config.settingsPrize.prizeScore.Count;
                            if (countReward != 0)
                            {
                                var score = Players.OrderByDescending(x => x.Value.Score).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in score)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.Score}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_SCORE", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.killer:
                        {
                            int countReward = config.settingsPrize.prizeKiller.Count;
                            if (countReward != 0)
                            {
                                var killer = Players.OrderByDescending(x => x.Value.pVP.Kills).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in killer)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.pVP.Kills}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_KILLER", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.time:
                        {
                            int countReward = config.settingsPrize.prizeTime.Count;
                            if (countReward != 0)
                            {
                                var time = Players.OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in time)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {TimeHelper.FormatTime(TimeSpan.FromMinutes(item.Value.playedTime.PlayedForWipe), 5)}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_TIME", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.farm:
                        {
                            int countReward = config.settingsPrize.prizeFarm.Count;
                            if (countReward != 0)
                            {
                                var farm = Players.OrderByDescending(x => x.Value.gather.AllGathered).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in farm)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.gather.AllGathered.ToString("0,0", CultureInfo.InvariantCulture)}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_FARM", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.raid:
                        {
                            int countReward = config.settingsPrize.prizeRaid.Count;
                            if (countReward != 0)
                            {
                                var raid = Players.OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in raid)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.explosion.AllExplosionUsed}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_RAID", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.killerNPC:
                        {
                            int countReward = config.settingsPrize.prizeNPCKiller.Count;
                            if (countReward != 0)
                            {
                                var killerNpc = Players.OrderByDescending(x => x.Value.pVP.KillsNpc).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in killerNpc)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.pVP.KillsNpc}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_KILLER_NPC", null, countReward, infoUser);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            PrintWarning(txt);
        }
        [ConsoleCommand("stat.score")]
        void ShopMoneyGive(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            var player = arg.Player();
            if (player != null && !player.IsAdmin)
            {
                PrintToConsole(player, GetLang("STAT_CMD_1", player.UserIDString));
                return;
            }

            switch (arg.Args[0])
            {
                case "give":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int score = Convert.ToInt32(arg.Args[2]);
                        PlayerInfo Player = PlayerInfo.Find(userID);
                        Player.Score += score;
                        Puts(GetLang("STAT_CMD_10", null, userID, score));
                        break;
                    }
                case "remove":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int score = Convert.ToInt32(arg.Args[2]);
                        PlayerInfo Player = PlayerInfo.Find(userID);
                        Player.Score -= score;
                        Puts(GetLang("STAT_CMD_11", null, userID, score));
                        break;
                    }
            }
        }
        [ConsoleCommand("stat.ignore")]
        private void CmdIgnorePlayer(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, permAdmin))
            {
                PrintToConsole(player, GetLang("STAT_CMD_1", player.UserIDString));
                return;
            }

            if (arg == null || arg.Args == null || arg.Args.Count() == 0)
            {
                arg.ReplyWith(GetLang("STAT_CMD_2"));
                return;
            }

            ulong steamid = 0;
            if (!UInt64.TryParse(arg.Args[1], out steamid))
            {
                var players = FindPlayers(arg.Args[1].ToLower());

                if (players.Count == 0)
                {
                    arg.ReplyWith(GetLang("STAT_CMD_3"));
                    return;
                }

                if (players.Count > 1)
                {
                    var PlayersMore = "";
                    foreach (var plr in players)
                        PlayersMore = PlayersMore + "\n" + plr.Value.Name + " - " + plr.Key;

                    arg.ReplyWith(GetLang("STAT_CMD_4", null, PlayersMore));
                    return;
                }

                steamid = players[0].Key;
            }

            string name = "";
            for (int ii = 1; ii < arg.Args.Count(); ii++)
                name += arg.Args[ii] + " ";

            name = name.Trim(' ');

            if (string.IsNullOrEmpty(name))
            {
                arg.ReplyWith(GetLang("STAT_CMD_2"));
                return;
            }

            if (!Players.ContainsKey(steamid) && !IgnoreReservedPlayer.ContainsKey(steamid))
            {
                arg.ReplyWith(GetLang("STAT_CMD_5"));
                return;
            }

            PlayerInfo playerInfo = Players.ContainsKey(steamid) ? Players[steamid] : IgnoreReservedPlayer[steamid];

            switch (arg.Args[0])
            {
                case "add":
                case "a":
                    {
                        if (IgnoreReservedPlayer.ContainsKey(steamid))
                        {
                            arg.ReplyWith(GetLang("STAT_CMD_6", null, playerInfo.Name));
                            break;
                        }
                        IgnoreReservedPlayer.Add(steamid, playerInfo);
                        Players.Remove(steamid);
                        arg.ReplyWith(GetLang("STAT_CMD_7", null, playerInfo.Name));
                        break;
                    }
                case "r":
                case "remove":
                    {
                        if (!IgnoreReservedPlayer.ContainsKey(steamid))
                        {
                            arg.ReplyWith(GetLang("STAT_CMD_8", null, playerInfo.Name));
                            break;
                        }
                        PlayerInfo info = IgnoreReservedPlayer[steamid];
                        Players.Add(steamid, info);
                        IgnoreReservedPlayer.Remove(steamid);
                        arg.ReplyWith(GetLang("STAT_CMD_9", null, playerInfo.Name));
                        break;
                    }
                default:
                    break;
            }
        }

        [ConsoleCommand("UI_HandlerStat")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            PlayerInfo playerInfo = PlayerInfo.Find(player.userID);
            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0])
                {
                    case "hidestat":
                        {
                            if (config.settings.availabilityUse && !permission.UserHasPermission(player.UserIDString, permAvailability))
                                return;
                            if (playerInfo.HidedStatistics)
                            {
                                playerInfo.HidedStatistics = false;
                            }
                            else
                                playerInfo.HidedStatistics = true;
                            ButtonHideStat(player, playerInfo);
                            break;
                        }
                    case "confirm":
                        {
                            DialogConfirmationDropStat(player);
                            break;
                        }
                    case "confirm_yes":
                        {
                            if (config.settings.dropStatUse && !permission.UserHasPermission(player.UserIDString, permReset))
                                return;
                            PlayerInfo.PlayerClearData(player.userID);
                            UserStat(player);
                            break;
                        }
                    case "changeCategory":
                        {
                            ulong target = ulong.Parse(args.Args[1]);
                            int cat = int.Parse(args.Args[2]);
                            CategoryStatUser(player, target, cat);
                            break;
                        }
                    case "ShowMoreStat":
                        {
                            ulong target = ulong.Parse(args.Args[1]);
                            int cat = int.Parse(args.Args[2]);
                            if (cat == 0)
                                OtherStatUser(player, target, 1);
                            else
                                OtherStatUser(player, target, 0);
                            break;
                        }
                    case "listplayer":
                        {
                            if (args.Args.Length > 1)
                            {
                                string Seaecher = args.Args[1].ToLower();
                                SearchPageUser(player, Seaecher);
                            }
                            else
                                SearchPageUser(player);
                            break;
                        }
                    case "GoStatPlayers":
                        {
                            ulong id = ulong.Parse(args.Args[1]);
                            UserStat(player, id);
                            break;
                        }
                    case "Page_swap":
                        {
                            int cat = int.Parse(args.Args[1]);
                            MenuButton(player, cat);
                            break;
                        }
                }
            }
        }
        #endregion
    }
}
      