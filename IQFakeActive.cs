using Oxide.Core.Plugins;
using Random = UnityEngine.Random;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("IQFakeActive", "Mercury", "2.5.31")]
    [Description("Simulate activity for a server with support for other plugins")]
    public class IQFakeActive : RustPlugin
    {

        private Single GetPercentWipeOnline(Int32 wipelocal)
        {
            if (!config.onlineController.wipeTimeOnlineOffset.ContainsKey(wipelocal)) return 1.0f;
            return 1.0f - (config.onlineController.wipeTimeOnlineOffset[wipelocal] / 100.0f);
        }

        private class InitializedStatus
        {
            public Boolean IsSuccess;
            public Boolean IsInit;
        }
        
        
        private readonly Regex _avatarRegex = new(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>", RegexOptions.Compiled);
        private readonly DateTime RealTime = DateTime.UtcNow.Date;
        private enum NetworkType
        {
            Connected,
            Disconnected
        }

                
        
        private Boolean IsFullInitialized = false;
        private class Configuration
        {
            [JsonProperty(LanguageEn ? "Display plugin actions in the console (true - yes/false - no), it is recommended to use it for analysis" : "Выводить ли в консоль действия плагина (true - да/false - нет), желательно использовать с целью анализа")]
            public Boolean useInformationConsole;
            [JsonProperty(LanguageEn ? "Online generation settings" : "Настройка генерации онлайна")]
            public OnlineController onlineController;
            [JsonProperty(LanguageEn ? "Settings for generating a fake player database" : "Настройка генерации базы фейковых игроков")]
            public PlayerFakeController playerFakeController;
            [JsonProperty(LanguageEn ? "Settings for generating a fake message database" : "Настройка генерации базы фейковых сообщений")]
            public MessageController messageFakeController;
            [JsonProperty(LanguageEn ? "Settings for connecting and disconnecting fake players" : "Настройка подключений и отключений фейковых игроков")]
            public NetworkController networkFakeController; 
            [JsonProperty(LanguageEn ? "Setting up the sending of fake kill logs in DeathMessage" : "Настройка отправки фейковых логов убийств в DeathMessage")]
            public DeathNoteController deathNoteController;
            
            internal class OnlineController
            {
                [JsonProperty(LanguageEn ? "Online offsets, a random offset from -N to +N will be applied for each generation" : "Отступы онлайна, случайно будет делаться отступ от -N до +N для каждой генерации")]
                public Int32 onlineOffset;
                [JsonProperty(LanguageEn ? "Ceiling of fake online in % ratio (will not exceed the online depending on the server slots for the specified %)" : "Потолок фейкового онлайна в % соотношении (не будет превышать онлайн в зависимости от слотов сервера для указанного %)")]
                public Int32 onlineMaxPercent;
                [JsonProperty(LanguageEn ? "Forced addition of online after the entire generation" : "Принудительное добавление онлайна после всей генерации")]
                public Int32 onlineMoreAmount;
                [JsonProperty(LanguageEn ? "Reduction of fake online in % ratio depending on the days since the wipe started (starts from 0)" : "Уменьшение фейкового онлайна в % соотношении в зависимости от пройденных дней вайпа (начинается с 0)")]
                public Dictionary<Int32, Int32> wipeTimeOnlineOffset = new Dictionary<Int32, Int32>();
            }

            internal class PlayerFakeController
            {
                [JsonProperty(LanguageEn ? "Set the type of player database generation: 0 - Cloud database (All data is taken from real servers, all SteamID are valid, and profiles are real), 1 - Local database (note that it will be generated based on the number of unique nicknames in the list, it is also advisable to specify lists of real Steam64ID for displaying avatars)" : "Выберите тип генерации базы игроков: 0 - Облачная база (Все данные взяты с настоящих серверов, все SteamID актуальны и профили настоящие), 1 - Локальная база (учтите, что она будет сгенерирована от количества уникальных ников указанных в списке, также желательно для отображения аватарок указать списки настоящих Steam64ID)")]
                public DatabaseType playersDbType;

                [JsonProperty(LanguageEn ? "List of unique nicknames for generating a local database (suitable for generation type - 1)" : "Список уникальных ников для генерации локальной базы (подходит для типа генерации - 1)")]
                public List<String> localDatabaseNickName = new List<String>();
                [JsonProperty(LanguageEn ? "List of unique Steam64ID for generating a local database (suitable for generation type - 1)" : "Список уникальных Steam64ID для генерации локальной базы (подходит для типа генерации - 1)")]
                public List<UInt64> localDatabaseSteamIds = new List<UInt64>();
            }

            internal class NetworkController
            {
                [JsonProperty(LanguageEn ? "Use connection/disconnected messages for fake players on the server (requires IQChat)" : "Использовать сообщения о подключение/отключениее на сервер фейковых игроков (требуется IQChat)")]
                public Boolean useNetwork;
                [JsonProperty(LanguageEn ? "List of countries for displaying connections of fake players (requires IQChat)" : "Список стран для отображения подключений фейковых игроков (требуется IQChat)")]
                public List<String> countryConnectedList = new List<String>();
                [JsonProperty(LanguageEn ? "List of reasons for displaying disconnections of fake players (requires IQChat)" : "Список причин для отображения отключений фейковых игроков (требуется IQChat)")]
                public List<String> reasonDisconnectedList = new List<String>();
                [JsonProperty(LanguageEn ? "Minimum time to send a connection message (A random interval between the minimum and maximum time is chosen)" : "Минимальное время для отправки сообщения о подключении (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 minTimeUpdateNetwork;
                [JsonProperty(LanguageEn ? "Maximum time to send a connection message (A random interval between the minimum and maximum time is chosen)" : "Максимальное время для отправки сообщения о подключении (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 maxTimeUpdateNetwork;
            }

            internal class MessageController
            {
                [JsonProperty(LanguageEn ? "Use sending fake personal messages to the player (true - yes/false - no) (requires IQChat)" : "Использовать отправку фейковых личных сообщений игроку (true - да/false - нет) (требуется IQChat)")]
                public Boolean usePmMessage;
                [JsonProperty(LanguageEn ? "Use sending fake chat messages (true - yes/false - no)" : "Использовать отправку фейковых сообщений в чат (true - да/false - нет)")]
                public Boolean useMessageChat;
                [JsonProperty(LanguageEn ? "Set the type of message database generation: 0 - Cloud database (All data is taken from real servers, all messages are real (in Russian)), 1 - Local database (fill in the list is required)" : "Выберите тип генерации базы сообщений: 0 - Облачная база (Все данные взяты с настоящих серверов, все сообщения являются настоящими (сообщения на русском языке)), 1 - Локальная база (требуется заполнить список)")]
                public DatabaseType messageDbType;
                [JsonProperty(LanguageEn ? "Local message database" : "Локальная база сообщений")]
                public List<String> localDatabaseMessages = new List<String>();
                [JsonProperty(LanguageEn ? "Local private message database" : "Локальная база личных сообщений")]
                public List<String> localDatabasePmMessage = new List<String>();
                [JsonProperty(LanguageEn ? "Minimum time to send a chat message (A random interval between the minimum and maximum time is chosen)" : "Минимальное время для отправки сообщения в чат (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 minTimeUpdateChat = new Int32();
                [JsonProperty(LanguageEn ? "Maximum time to send a chat message (A random interval between the minimum and maximum time is chosen)" : "Максимальное время для отправки сообщения в чат (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 maxTimeUpdateChat = new Int32();
                [JsonProperty(LanguageEn ? "Minimum time to send private messages (Requires IQChat) (A random interval between the minimum and maximum time is chosen)" : "Минимальное время для отправки сообщения в личные сообщения (Требуется IQChat) (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 minTimeUpdatePmChat = new Int32();
                [JsonProperty(LanguageEn ? "Maximum time to send private messages (Requires IQChat) (A random interval between the minimum and maximum time is chosen)" : "Максимальное время для отправки сообщения в личные сообщения (Требуется IQChat) (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 maxTimeUpdatePmChat = new Int32();
            }

            internal class DeathNoteController
            {
                [JsonProperty(LanguageEn ? "Use support for fake sending in DeathMessages" : "Использовать поддержку фейковой отправки в DeathMessage")]
                public Boolean useFakeDeathMessages;
                [JsonProperty(LanguageEn ? "The list of kill weapons for DeathMessages based on the time after the wipe" : "Список оружий убийства для DeathMessage по времени после вайпа")]
                public List<WeaponDeathNote> weaponDeathNote;
                [JsonProperty(LanguageEn ? "Utilize time adjustment between intervals depending on the online player count (the higher the online count, the more the specified interval will be reduced)" : "Использовать корректировку времени между интервалами в зависимости от онлайна (чем выше онлайн - тем больше заданный интервал будет урезаться)")]
                public Boolean useCorrectedInterval;
                [JsonProperty(LanguageEn ? "Interval limit, messages will be sent no more frequently than specified in this item (if time adjustment is enabled)" : "Предел интервала, отправление сообщений будет не чаще чем указанное в этом пункте (если включена корректировка времени)")]
                public Int32 limitCorrected;
                [JsonProperty(LanguageEn ? "Minimum time for sending death log in DeathMessage (Requires a plugin with support) (A random interval between minimum and maximum time is selected)" : "Минимальное время для отправки лога о смерти в DeathMessage (Должен быть плагин с поддержкой) (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 minTimeUpdateDeath = new Int32();
                [JsonProperty(LanguageEn ? "Maximum time for sending death log in DeathMessage (Requires a plugin with support) (A random interval between minimum and maximum time is selected)" : "Максимальное время для отправки лога о смерти в DeathMessage (Должен быть плагин с поддержкой) (Выбирается случайный промежуток между минимальным и максимальным временем)")]
                public Int32 maxTimeUpdateDeath = new Int32();
		   		 		  						  	   		   					  						  						  	   
                internal class WeaponDeathNote
                {
                    [JsonProperty(LanguageEn ? "Time elapsed since the wipe" : "Время которое прошло после вайпа")]
                    public Int32 secondWipeTime;
                    [JsonProperty(LanguageEn ? "List of weapons for displaying kills (shortname)" : "Список оружий для отображения убийства (shortname)")]
                    public List<String> weapon;
                }
            }
            
            public static Configuration GetNewConfiguration() 
            {
                return new Configuration
                {
                    useInformationConsole = false,
                    onlineController = new OnlineController()
                    {
                        onlineOffset = 3,
                        onlineMaxPercent = 20,
                        onlineMoreAmount = 5,
                        wipeTimeOnlineOffset = new Dictionary<Int32, Int32>()
                        {
                            [0] = 0,
                            [1] = 10,
                            [2] = 20,
                            [3] = 25,
                            [4] = 30,
                            [5] = 40,
                            [6] = 50,
                        }
                    },
                    deathNoteController = new DeathNoteController
                    {
                        useFakeDeathMessages = false,
                        weaponDeathNote = new List<DeathNoteController.WeaponDeathNote>()
                        {
                            new DeathNoteController.WeaponDeathNote()
                            {
                                secondWipeTime = 18000,
                                weapon = new List<String>()
                                {
                                    "bow.hunting",
                                    "bone.club",
                                    "salvaged.sword",
                                    "spear.wooden",
                                    "spear.stone",
                                    "pistol.nailgun",
                                    "shotgun.waterpipe",
                                }
                            },
                            new DeathNoteController.WeaponDeathNote()
                            {
                                secondWipeTime = 25200,
                                weapon = new List<String>()
                                {
                                    "pistol.m92",
                                    "shotgun.double",
                                    "pistol.revolver",
                                    "pistol.semiauto",
                                    "pistol.python",
                                }
                            },
                            new DeathNoteController.WeaponDeathNote()
                            {
                                secondWipeTime = 36000,
                                weapon = new List<String>()
                                {
                                    "smg.2",
                                    "smg.thompson",
                                    "rifle.semiauto",
                                    "smg.mp5",
                                }
                            },
                            new DeathNoteController.WeaponDeathNote()
                            {
                                secondWipeTime = 86400,
                                weapon = new List<String>()
                                {
                                    "rifle.bolt",
                                    "rifle.lr300",
                                    "rifle.ak",
                                }
                            },
                            new DeathNoteController.WeaponDeathNote()
                            {
                                secondWipeTime = 108000,
                                weapon = new List<String>()
                                {
                                    "rifle.l96",
                                    "rifle.m39",
                                    "multiplegrenadelauncher",
                                    "lmg.m249",
                                }
                            },
                        },
                        minTimeUpdateDeath = 60,
                        maxTimeUpdateDeath = 120,
                        useCorrectedInterval = false,
                        limitCorrected = 30,
                    },
                    playerFakeController = new PlayerFakeController()
                    {
                        playersDbType = DatabaseType.Cloud,
                        localDatabaseNickName = new List<String>()
                        {
                            "katzu","Dmitry","Pimp of the simps","Flacko","A-Train","PGI_ZeroBB","LudiMgnt","D3MON","Kyl4U","el tigre",
                            "Funovichok","Loxxy♥♥","Venom","BANDIT33RUS","unknown","vvvzl.","crater", "luxer",
                            "LucaCobraS","Bodya","CrowNStaR","Lixxon?","Zloi","JOAO MARCOS","h1zzy32.exe","Cr1sp","Game Checker YT","RedBabL", "buble",
                            ".. #YRS","Mode","vannna535","ЖЫРНС","J3n5","Krombopulos Michael","Le Z","ab1l1ty #BLAGORUST","Cna","thiefs.","叮当","Uchiha Sasuke","Uzbek",
                            "Baghool","oggyultra","HMJMF","mr. bombastik","fuwkitup^","Dusi","freekle3","Eqzelanz","黃小明","slaves 1","kidneystones",
                            "CT..NIK","Я отброс","Fishka","John Pork","wake","Ge_Sh","PEDRO","ジヅOzzyヅジ(Talant)","DARIYERLİ","grandma","88888888bibi",
                            "hânyangho","artemmatyukha2","Jumpair","HoodieWhip","wdawd1235412","ZarazA","Medved_Pluh","BlackFont","Alex","dendi","Tripp","timurkachernyshev",
                            "CelalCSK","N1cCione","Ling Ling","arlekin","Anti_Red","maximus1","skaicross","dikiy strapon","tendonin","Mwad3",
                            "FAIL Kaz0ku","Mixsar","begula2305","rizvan0150505","Pupirka_","KaYoZz","artemochka","AMATERASU","SeReneY","Protazer",
                            "MR__GLOOM_TM ✪","yunghell","重生26","n1se","iKnow","vdeschanel","Carmini","I'm Top on Dat","Abadji","Margerita","AMID",
                            "Ingush_001","Tokitō Muichirō?","GL KVESTIK1","CUMSHOT","tifeck","wawekqer"
                        },
                        localDatabaseSteamIds = new List<UInt64>()
                        {
                            76561199499115921, 76561199099926211, 76561198207721004, 76561199047092906, 76561198271128751, 76561199228471971, 76561199007962066, 
                            76561198868203169, 76561199222046444, 76561198951445576, 76561198264541170, 76561198049279651, 76561199065361509, 76561199049286411,
                            76561199350307660, 76561198149621274, 76561199019136069, 76561198166194715, 76561198867944664, 76561199344515835, 76561199138512597,
                            76561198271121379, 76561199225909449, 76561193232335826, 76561199455212819, 76561198097977241, 76561198370413800, 76561199559469371,
                            76561199484958613, 76561199248846017, 76561199180444128, 76561198994001632, 76561198969591407, 76561199131386981, 76561199520531978,
                            76561199130648454, 76561198877987683, 76561199506599580, 76561198802125279, 76561198046788189, 76561198081263959, 76561199023535765,
                            76561199040058426, 76561199031809287, 76561199026957616, 76561199007988524, 76561132328971722, 76561199479382516, 76561198124987208,
                            76561199379008287, 76561198362419170, 76561198411265357, 76561198135085249, 76561199139388698, 76561198132501705, 76561199523729588,
                            76561198885992908, 76561198401714507, 76561199067162628, 76561199285356144, 76561198020408410, 76561199389227618, 76561199213999334,
                            76561198386863230, 76561199195704084, 76561198251043950, 76561199090431567, 76561199227885579, 76561198322959103, 76561198072935695,
                            76561199559583351, 76561199523275996, 76561199514936217, 76561198359614832, 76561199100363543, 76561199188594271, 76561199429349448,
                            76561198124664616, 76561199278597129, 76561198969122505, 76561199173825189, 76561198373384073, 76561198296218154, 76561199061751104,
                            76561199350369014, 76561198874349944, 76561199519449486, 76561199496409070, 76561199473372304, 76561199434006899, 76561198871711789,
                            76561199522967671, 76561198969748511, 76561199171239990, 76561199239753048, 76561198836533674, 76561199289407095, 76561198420736762,
                            76561198989446920, 76561199126807222, 76561199481284332, 76561198303573658, 76561199242610615, 76561199243042817, 76561198168903862,
                            76561198990243500, 76561198355493515, 76561198998059878, 76561198135110211, 76561198058211512, 76561198276743061, 76561199022527413,
                            76561198175277109, 76561199163033413, 76561198951701258, 76561199235878329, 76561199273540400, 76561199260320816, 76561199323200600,
                            76561198393876803, 76561199487255537, 76561199415410689   
                        },
                    },
                    networkFakeController = new NetworkController()
                    {
                        minTimeUpdateNetwork = 60,
                        maxTimeUpdateNetwork = 250,
                        countryConnectedList = new List<String>()
                        {
                            "Russia",
                            "Belarus",
                            "Spain",
                            "Germany",
                            "Ukraine",
                            "United States",
                        },
                        reasonDisconnectedList = new List<String>()
                        {
                            "Disconnected",
                            "Time out",
                            "Unresponsive",
                        }
                    },
                    messageFakeController = new MessageController()
                    {
                        useMessageChat = true,
                        usePmMessage = false,
                        messageDbType = DatabaseType.Cloud,
                        minTimeUpdateChat = 30,
                        maxTimeUpdateChat = 60,
                        minTimeUpdatePmChat = 600,
                        maxTimeUpdatePmChat = 1200,
                        localDatabaseMessages = LanguageEn ?
                        new List<string>()
                        {
                            "hahaha", "I don't know anything myself", "F***", ".LSHE", "Danil doesn't want to go together?", "chf, jnjkj", "Well, crates and cargo", "Hello", "They will shoot you from the bolt",
                            "Alcoholic, don't kill", "Go", "C", "When was the wipe?", "Let's go", "Sucked", "Hi", "Okay", "Let's go", "I dodged, but you still hit me",
                            "I didn't pay off", "Ok", "It swims as if", "Glory and freedom, go team", "No paint", "He disagreed with himself but had a good laugh", "NORM", "Hey, get out of the house",
                            "What will it be?", "jktu", "Guys (sSd)HiTm@n put heads, report, throw", "NO", "Well, there was no voting for me",
                            "wsefwsef", "78", "omg)))) its your neighbor from another side I think",
                            "If you're going to buy, you don't have to, and I'll take it for free, even if I don't need it... iron logic.))",
                            "What's the problem?", "Go to hell, you f***ing cheater", "I'm waiting for you inside", "Give resources", "It will be soon", ".", "Give me /battle",
                            "When is the wipe", "friend ff", "PvE genius server", "6", "Let's go on autopilot", "Where are you?", "Selling cars", "Looking for a teammate DC Semka#6332 1675 hours",
                            "Hello", "haha", "Who will take a slave into the team?", "That's why they're selling so cheap", "How much", "YOU DOG",
                            "ash 10", "hvahvah", "Give me a green card please", "I'm already there", "Why 1.001? Because he doesn't want to go negative", "Let's go",
                            "ZADHFYADZHZLFAYLZLZH", "He's going to dip him", "Are you a girl?", "Who's fighting with Lukas", "Yes, let's go", "Okay", "Sorry for killing you during farming",
                            "To you", "I didn't doorcamp, I didn't camp in the offline, I didn't shoot", "I just asked", "Why is the entrance to the metro closed?", "How to get to training?", "Because I don't have one",
                            "Sell propane cylinders(", "Oops", "We're coming now", "You have a full team", "HE BUILT HIMSELF", "He's the luckiest being in the world", "FOR WHAT", "Yes",
                            "Should have asked right away", ")", "1 chip 100 scrap :)", "You didn't put a spring on it, genius", "like cargo does everytime to :)", "Selling scrap )",
                            "Guess", "No, I just built", "Here, there was an HQ door", "Sleeping bag", "Who's team 12 years old from 700 hours", "No", "PINGGGGGGG6332", "Man, God",
                            "Take it", "What's your teammate's name?", "I am too", "Sema", "What to do with duct tape?", "I'll buy 2 for 1000 scrap", "fxfxfxfx", "Yeah, fully geared", "I'll sell 19",
                            "I'm going to shoot a missile at you", "I don't have a home, so I'm stealing because you're a f***ing cheater", "And they started whining", "Missiles in my face",
                            "How much", "21,000 followers on Twitch", "Who's on the team", "Yes", "What", "There are no kits here", "NEAR THE HOUSE I WAS KILLED BY GUNSHOTS", "I'll still remove it",
                            "Warsxzaw, will you sell pineapple cola?", "Me?", "iyi", "You gave us hits in the head right away, genius", "hahah", "WHAT ARE YOU LOOKING AT, STUPID?", "I am",
                            "Do you have springs?", "How old are you?", "hahaha - mioder", "SHOOT MORE", "It's boring already, everything is there", "How?", "Dota genius software", "GBPLF RJGXT",
                            "l13 maybe", "Let's go, 2 ak47", "ff", "What", "If you come to shoot", "KORDI SAY", "Can someone send cards, green and blue", "+", "You'll see",
                        }
                        : new List<String>()
                        {
                            "хахаахах", "я сам ничего не знаю", "ЕБАТЬ", ".ЛШЕ", "данил нехоч вместе?", "chf,jnjkj", "ну и  карго и крейты", "ало", "тебя с болта будут шотать",
                            "алкаш неубивай", "пошёл", "ц", "когда вайп был?", "го", "сосал", "прив", "ну ладно", "давай иди", "я еще отклонился ну ты все равно попал", 
                            "я не окупился", "ок", "он плывет как бы", "слава и воля го тим", "не мазила", "сам не согласился а поржал", "НОРМ", "э мипо выйди из хаты",
                            "какой будет ?", "jktu", "пацаны  (sSd)HiTm@n  головы ставит репорт кидайте", "НЕ", "ну у меня не было голосвания", 
                            "wsefwsef", "78", "omg))))  its your neibor from another side i think", 
                            "если покупать то ненадо а в нахаляву заберу если даже ненадо.. железная логика.))",
                            "А в чём проблема?", "да пошел ты нахуй читак ебаный", "я жду тебя тут внутри", "ДАй ресов", "Скоро будет", ".", "кидай  мне /battel", 
                            "вайп когда", "friend ff", "гений сервер пве", "6", "го на автомате", "а ты где?", "прадаю машины", "ищу тиммейта дс Semka#3141 1675 часов", 
                            "О Б дарова", "хахах", "кто раба возмет в команду?", "вот и продают так дешево", "сколько", "ТЫ СОБАКА", 
                            "аш 10", "хвахвах", "киньте зеленую карту плиз", "я итак там", "почему 1.001 потому что он не хочет уйти в минус", "го", 
                            "ЗАДХФЫАДЗЩЗЛФАЫЛЩЗаыф", "гоинг дипнул его", "ты девочка?", "кто файт на луках", "да давай", "окей", "простите что убил на фарме", 
                            "тебе", "я не доркепмил,не кемпили в оффе не бахал", "просто просил", "а че вход в метро закрыт", "как зайти на тренеровку", "а то у меня нету", 
                            "продайте пропан балоны(", "ой", "ща мы придем", "у вас фул тим", "ОН ПОСТРОИЛСЯ", "он самое везучее СУЩЕСТВО во всём мире", "FOR WHAT", "да", 
                            "нада было сразу спросить", ")", "1 микросхема 100 скрапа :)", "ты не откинул спалку гений", "like cargo does everytime to :)", "selling scrap )",
                            "угадывай", "нет я только построился", "aca habia puerta hq la quite", "СПАЛКУ", "кто тим 12 лет от 700 часов", "Нет.", "ПИНГГГГГГГГГГГ", "чел боже",
                            "бери", "可以的，你队友叫啥", "я тоже", "Сема", "что делать с изолентой?", "куплю 2 за 1000 скраппа", "фхфхфххф", "ну 19 продам",
                            "ща нахуй на тебя млрс запущу", "у меня нет дома поэтому и пизжу что ты софтяра ебаная", "и они ныть начали чет", "ракеты мне в ебало",
                            "сколько", "21тыс фолоу на твиче", "кто тим", "да", "че", "тут нету китов", "ВОЗЛЕ ДОМА МЕНЯ УБИЛИ ПРИДЫ НА ВЫСТРЕЛЫ", "Я все же уберу ее", 
                            "Warsxzaw апелисьновую колу продашь ?", "я?", "iyi", "ты нам хиты в голову сразу дал гений", "hahah", "QUE MIRAS BOBO AL ALA PASSA", "Я", 
                            "есть шестрени?", "Тебе сколкьо лет ?", "ахаха - миодер", "РЕЩЕ СТРЕЛЯЙТЕ", "скучно уже, все есть", "Как?", "гений доты софт", "GBPLF RJGXT", 
                            "l13 maybe", "давай 2 калаша", "ff", "че", "если придешь бахать", "КОРДИ СКАЖИ", "Киньте кто-нибудь карточки,зеленую и голубую", "+", "Увидишь",
                        },
                        localDatabasePmMessage = new List<String>()
                        {
                            "pmMessageTemplate",
                            "pmMessageTemplate",
                        }
                    }
                };
            }
        }
        public class FakeDatabase
        {
            public OnlineDatabase onlineDatabase = new();
            public ChatPresetFakePlayer chatPresetFakePlayer = new();
            public List<FakePlayer> fakePlayerList = new();
            public List<FakePlayer> fakePlayerConnection = new();
            public List<FakePlayer> fakePlayerDisconnection = new();
            
            public List<FakePlayer> realAndFakePlayerList = new();
            public List<Messages> fakeMessageList = new();

            public class Messages
            {
                [JsonProperty("message")]
                public String message;
            }
            public class OnlineDatabase
            {
                public Int32 currentFakeOnline;
                public Int32 currentOnline;
            }
            public class FakePlayer
            {
                [JsonProperty("userId")] public String userId;
                [JsonProperty("displayName")] public String displayName;

                public Boolean isMuted;
            }

            public class ChatPresetFakePlayer
            {
                public String chatPrefix;
                public String chatColor;
                public String nickColor;
                public Int32 sizeMessage;
                public Int32 sizeNick;
            }
        }
        
        JObject GetOnlyListFakePlayers()
        {
            if (!IsActiveAPIPlayers()) return null;
            return JObject.FromObject(new { players = fakeDatabase.fakePlayerList });
        }

        Int32 GetOnline() => !IsActiveAPIPlayers() ? BasePlayer.activePlayerList.Count : fakeDatabase.onlineDatabase.currentOnline;
        
        private void CheckFullInitialized()
        {
            if(timerCheckInitialized is { Destroyed: false })
                timerCheckInitialized.Destroy();
            
            if (statusInitialized[TypeInitialize.Messages].IsSuccess &&
                statusInitialized[TypeInitialize.Online].IsSuccess && 
                statusInitialized[TypeInitialize.Players].IsSuccess &&
                statusInitialized[TypeInitialize.Networks].IsInit)
            {
                IsFullInitialized = true;
                Puts(LanguageEn ? "The plugin is fully initialized, and its functions are running" : "Плагин полностью инициализирован, функции запущены");
                
                timerRefreshDatabase = timer.Once(600, RefresgDataBase);

                if (IQChat && config.networkFakeController.useNetwork)
                    timerNetworking = timer.Once(Random.Range(config.networkFakeController.minTimeUpdateNetwork, config.networkFakeController.maxTimeUpdateNetwork), StartNetworking);

                if (config.messageFakeController.useMessageChat)
                    timerChatMessages = timer.Once(Random.Range(config.messageFakeController.minTimeUpdateChat, config.messageFakeController.maxTimeUpdateChat), MessageImmitation);

                if (config.messageFakeController.usePmMessage && IQChat)
                    timerChatPmMessages = timer.Once(Random.Range(config.messageFakeController.minTimeUpdatePmChat, config.messageFakeController.maxTimeUpdatePmChat), MessagePmImmitation);
		   		 		  						  	   		   					  						  						  	   
                if (config.deathNoteController.useFakeDeathMessages && (DeathMessages || DeathMessage))
                    timerDeathMessages = timer.Once(GetTimeUpdateDeathNote(), SendDeathNote);
                
                return;
            }

            timerCheckInitialized = timer.Once(5f, CheckFullInitialized);
        }
        
        private void ConnectPlayer()
        {
            if (fakeDatabase.fakePlayerConnection == null || fakeDatabase.fakePlayerConnection.Count == 0)
                return;

            Int32 randomIndex = Random.Range(0, fakeDatabase.fakePlayerConnection.Count);
            FakeDatabase.FakePlayer playerConnected = fakeDatabase.fakePlayerConnection[randomIndex];
            String country = GetCountryConnected();
            if (country == null) return;

            IQChat.Call("API_SEND_PLAYER_CONNECTED", playerConnected.displayName, country, playerConnected.userId);

            fakeDatabase.fakePlayerDisconnection.Add(playerConnected);
            fakeDatabase.fakePlayerConnection.RemoveAt(randomIndex);

            lastNetworkType = NetworkType.Connected;
            LogToConsole(LanguageEn ? $"Simulating player connection: {playerConnected.displayName}" : $"Имитация подключения игрока : {playerConnected.displayName}");
        }
		   		 		  						  	   		   					  						  						  	   
                
        
        
        [ConsoleCommand("iqfa.debug")]
        private void ConsoleCommandFakeActive(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if(player != null)
                if (!player.IsAdmin)
                    return;

            if (!IsFullInitialized)
            {
                PrintWarning(LanguageEn ? "The plugin has not yet been produced, the command is not yet available" : "Плагин еще не инициализировался, команда еще недоступна");
                return;
            }
            if (!Int32.TryParse(arg.Args[0], out Int32 countWipe)) 
            {
                PrintWarning(LanguageEn ? "An incorrect number of wipe days is specified" : "Некорректно указанное число дней вайпа");
                return;
            }

            String debugStroke = String.Empty;
            
            for (Int32 d = 0; d < countWipe; d++)
            {
                for (Int32 h = 0; h < 24; h++)
                {
                    for (Int32 m = 0; m < 60; m++)
                    {
                        Int32 Online = GenerateOnlineCount(d, h, m);
                        debugStroke += LanguageEn ? $"\n{d + 1} day. Time {h}:{m}. Fake online: {Online}. Displayed online with players: {Online + BasePlayer.activePlayerList.Count}" : $"\n{d + 1} день. Время {h}:{m}. Фейковый онлайн : {Online}. Отображаемый онлайн с учетом игроков : {Online + BasePlayer.activePlayerList.Count}";
                    }
                }
            }
            
            PrintWarning(LanguageEn ? $"The log with approximate online statistics for {countWipe} wipe days has been saved in the file /logs/IQFakeActive/iqfakeactive_example_statistics.txt" : $"Лог с примерной статистикой онлайна на {countWipe} дней вайпа сохранен в файле /logs/IQFakeActive/iqfakeactive_exapmle_statistics.txt");
            LogToFile("exapmle_statistics", debugStroke, this);
        }
                
                private void InitializeProccess()
        {
            if (!statusInitialized[TypeInitialize.ChatInformation].IsInit)
            {
                GeneratePlayerPreset();
                statusInitialized[TypeInitialize.ChatInformation].IsInit = true;
            }
            else if (!statusInitialized[TypeInitialize.Messages].IsInit)
            {
                GenerateMessages();
                statusInitialized[TypeInitialize.Messages].IsInit = true;
            }
            else if (!statusInitialized[TypeInitialize.Messages].IsSuccess)
                GenerateMessages();
            else if (!statusInitialized[TypeInitialize.Players].IsInit)
            {
                GeneratePlayers();
                statusInitialized[TypeInitialize.Players].IsInit = true;
            }
            else if (!statusInitialized[TypeInitialize.Players].IsSuccess)
                GeneratePlayers();
            else if (!statusInitialized[TypeInitialize.Online].IsInit)
            {
                GenerateOnline();
                statusInitialized[TypeInitialize.Online].IsInit = true;
                statusInitialized[TypeInitialize.Online].IsSuccess = true;
                Puts(LanguageEn ? $"Online initialized: number of fake players - {fakeDatabase.onlineDatabase.currentFakeOnline} : displayed online - {fakeDatabase.onlineDatabase.currentOnline}" : $"Онлайн инициализирован : количество фейковых игроков - {fakeDatabase.onlineDatabase.currentFakeOnline} : отображаемый онлайн - {fakeDatabase.onlineDatabase.currentOnline}");
            }
            else if (!statusInitialized[TypeInitialize.Avatars].IsInit && ImageLibrary)
            {
                Puts(LanguageEn ? "The process of generating fake avatars for ImageLibrary has started, and this may take some time" : "Запущен процесс генерации фейковых аватарок для ImageLibrary, это может занять некоторое время");
                if (routineAddedAvatars == null)
                    routineAddedAvatars = ServerMgr.Instance.StartCoroutine(GenerateAvatars());
                else
                {
                    ServerMgr.Instance.StopCoroutine(routineAddedAvatars);
                    routineAddedAvatars = ServerMgr.Instance.StartCoroutine(GenerateAvatars());
                }

                statusInitialized[TypeInitialize.Avatars].IsInit = true;
            }
            else if (!statusInitialized[TypeInitialize.Avatars].IsInit && !ImageLibrary)
            {
                statusInitialized[TypeInitialize.Avatars].IsInit = true;
                statusInitialized[TypeInitialize.Avatars].IsSuccess = true;
            }
            else if (!statusInitialized[TypeInitialize.Networks].IsInit && statusInitialized[TypeInitialize.Avatars].IsSuccess)
            {
                if (config.networkFakeController.useNetwork)
                    GenerateConnectionPlayer();
                
                statusInitialized[TypeInitialize.Networks].IsInit = true;
                
                if (initializationTimer != null)
                {
                    initializationTimer.Destroy();
                    initializationTimer = null;
                }
            }
        }
        
                
                private static Configuration config = new Configuration();
        
        
        private String GetCountryConnected() => config.networkFakeController.countryConnectedList != null && config.networkFakeController.countryConnectedList.Count != 0 ? config.networkFakeController.countryConnectedList[Random.Range(0, config.networkFakeController.countryConnectedList.Count)] : null;
		   		 		  						  	   		   					  						  						  	   
        private NetworkType lastNetworkType = NetworkType.Disconnected;

        private Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        Int32 GetOnlyFakeOnline() => !IsActiveAPIPlayers() ? BasePlayer.activePlayerList.Count : fakeDatabase.onlineDatabase.currentFakeOnline;

        
        
        private Boolean IsActiveAPIPlayers()
        {
            if (!IsFullInitialized) return false;
            return statusInitialized[TypeInitialize.Players].IsInit &&
                   statusInitialized[TypeInitialize.Players].IsSuccess;
        }
        private static Double CurrentTime => Facepunch.Math.Epoch.Current;

        
                
        private Int32 GetTimeUpdateDeathNote()
        {
            Configuration.DeathNoteController deathController = config.deathNoteController;
            Int32 interval = Random.Range(deathController.minTimeUpdateDeath, deathController.maxTimeUpdateDeath);

            if (!deathController.useCorrectedInterval) return interval;
            interval = (Int32)(interval * (1.0f - GetOnline() / ConVar.Server.maxplayers));

            if (interval < deathController.limitCorrected)
                interval = deathController.limitCorrected;

            return interval;
        }
        
                
        private void StartNetworking()
        {
            if (!IsFullInitialized) return;
            if (!statusInitialized[TypeInitialize.Networks].IsSuccess) return;
            if(timerNetworking is { Destroyed: false })
                timerNetworking.Destroy();

            if (!IQChat) return;
            
            if (lastNetworkType == NetworkType.Disconnected)
                ConnectPlayer();
            else DisconnectPlayer();

            timerNetworking = timer.Once(Random.Range(config.networkFakeController.minTimeUpdateNetwork, config.networkFakeController.maxTimeUpdateNetwork), StartNetworking);
        }
        private DateTime TimeCreatedSave;
        
        
                
        JObject GetDatabase()
        {
            if (!IsFullInitialized) return null;
            List<FakeDatabase.FakePlayer> realPlayers = GetFInPlayers();
            List<FakeDatabase.FakePlayer> realAndFake = new();
            realAndFake.AddRange(realPlayers);
            realAndFake.AddRange(fakeDatabase.fakePlayerList);

            fakeDatabase.realAndFakePlayerList = realAndFake;
            
            return JObject.FromObject(fakeDatabase);
        }

        private void WarningAndUnloadPlugin(String Message)
        {
            PrintWarning(Message);
            NextTick(() => { Interface.Oxide.UnloadPlugin(Name); });
        }
        public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);

        private void LogToConsole(String Messages)
        {
            if (!config.useInformationConsole) return;
            Puts(Messages);
        }
        private const Int32 maxAttemptsGetMessage = 5;
        private void GenerateOnline()
        {
            fakeDatabase.onlineDatabase.currentFakeOnline = GenerateOnlineCount();
            fakeDatabase.onlineDatabase.currentOnline = fakeDatabase.onlineDatabase.currentFakeOnline + BasePlayer.activePlayerList.Count;
        }
		   		 		  						  	   		   					  						  						  	   
        
        
        private Single GetPercentMaxFake => 1.0f - (config.onlineController.onlineMaxPercent / 100.0f);
        
        private Timer timerChatMessages;
        
        private void SendDeathNote()
        {
            if (!IsFullInitialized) return;
            
            if(timerDeathMessages is { Destroyed: false })
                timerDeathMessages.Destroy();

            if (!DeathMessages && !DeathMessage) return;

            String killerName = GetNameDeathNote();
            if (String.IsNullOrWhiteSpace(killerName)) return;

            String killedName = GetNameDeathNote();
            if (String.IsNullOrWhiteSpace(killedName)) return;
            if (killerName.Equals(killedName)) return;

            String weapon = GetWeaponDeathNote();
            if (String.IsNullOrWhiteSpace(weapon)) return;

            Single distanceKilled = Random.Range(30f, 300f);
            Boolean isHeadshot = Random.Range(0, 2) == 0;

            if (DeathMessages)
                DeathMessages.CallHook("CreateNote", killerName, killedName, distanceKilled, "", "", weapon, new string[] { }, isHeadshot, false, false);
            else if (DeathMessage)
                DeathMessage.Call("Send_Kill_Player_Note", "PlayerKillPlayer", killerName, weapon, killedName, $"{distanceKilled}");
            
            timerDeathMessages = timer.Once(GetTimeUpdateDeathNote(), SendDeathNote);
        }
        private Int32 WipeTime;
        
        
        
        private void GenerateConnectionPlayer()
        {
            if (!IQChat)
            {
                PrintWarning(LanguageEn ? "You don't have IQChat installed, functions for connecting and disconnecting fake players will be unavailable" : "У вас не установлен IQChat, функции подключений и отключений фейковых игроков будут недоступны");
                return;
            }
            if (fakeDatabase.fakePlayerList.Count < 80)
            {
                PrintWarning(LanguageEn ? "Not enough fake players generated for simulating connections and disconnections; you should have at least 80 players in the database" : "Недостаточно сгенерировано фейковых игроков для генерации подключений и отключений, у вас должно быть не меньше 80 игроков в базе");
                return;
            }
            
            Int32 conAmount = 0;
            for (Int32 con = 0; con < 30; con++)
            {
                if (fakeDatabase.fakePlayerList.Count < con) continue;
                fakeDatabase.fakePlayerConnection.Add(fakeDatabase.fakePlayerList[con]);
                fakeDatabase.fakePlayerList.RemoveAt(con);
                conAmount = con;
            }
            
            Int32 disconAmount = 0;
            for (Int32 con = 0; con < 30; con++)
            {
                if (fakeDatabase.fakePlayerList.Count < con) continue;
                fakeDatabase.fakePlayerDisconnection.Add(fakeDatabase.fakePlayerList[con]);
                fakeDatabase.fakePlayerList.RemoveAt(con);
                disconAmount = con;
            }
            
            statusInitialized[TypeInitialize.Networks].IsSuccess = true;
            Puts(LanguageEn ? "60 users were generated from the player database for connections and disconnections on the server" : "Из базы игроков было сгенерировано 60 пользователей для подключений и отключений с сервера");
        }
        private Timer timerRefreshDatabase;
        /// <summary>
        /// - Исправлена работа с API DeathMessages by Voodoo после его обновления
        /// </summary>
        
        
        [PluginReference] Plugin IQChat, ImageLibrary, DeathMessages, DeathMessage;
		   		 		  						  	   		   					  						  						  	   
        private Coroutine routineAddedAvatars = null;

        
        
        private void OnServerInitialized()
        {
            TimeCreatedSave = SaveRestore.SaveCreatedTime.Date;
            WipeTime = RealTime.Subtract(TimeCreatedSave).Days;
            
            CheckFullInitialized();
            initializationTimer = timer.Every(5f, InitializeProccess);
        }
        
        
        
        private void GenerateMessages()
        {
            if (config.messageFakeController.messageDbType == DatabaseType.Local)
            {
                fakeDatabase.fakeMessageList = config.messageFakeController.localDatabaseMessages.Select(message => new FakeDatabase.Messages { message = message }).ToList();
                
                statusInitialized[TypeInitialize.Messages].IsInit = true;
                statusInitialized[TypeInitialize.Messages].IsSuccess = true;
                Puts(LanguageEn ? $"Generating messages from the local database - successful, received: {fakeDatabase.fakeMessageList.Count} messages" : $"Генерация сообщений с локальной базы - успешно, было получено : {fakeDatabase.fakeMessageList.Count} сообщений");
                return;
            }

            try
            {
                webrequest.Enqueue(ApiMessages, null, (code, response) =>
                {
                    switch (code)
                    {
                        case 503:
                            WarningAndUnloadPlugin(LanguageEn ? "An update for the plugin is available. Please update the plugin version to allow database connection" : "Вышло обновление плагина, обновите версию плагина чтобы вы могли подключаться в базе-данных!");
                            return;
                        case 404:
                        case 500:
                        case 401:
                            WarningAndUnloadPlugin(LanguageEn ? "An error occurred in the database server. Please report this to the developer" : "Произошла ошибка на сервере базы-данных, сообщите разработчику об этом");
                            return;
                        case 429:
                            WarningAndUnloadPlugin(LanguageEn ? "The request limit to the cloud database has been exceeded! Your IP is blocked for 2 hours" : "Превышен лимит запросов к облачной базе данных! Ваш IP заблокирован на 2 часа");
                            return;
                        default:
                            if (response == null)
                            {
                                WarningAndUnloadPlugin(LanguageEn ? "An error occurred on the database server. The response came back empty (null). Please report this to the developer" : "Произошла ошибка на сервере базы-данных, ответ пришел пустым (null) сообщите разработчику об этом");
                                return;
                            }

                            fakeDatabase.fakeMessageList = JsonConvert.DeserializeObject<List<FakeDatabase.Messages>>(response);

                            if (fakeDatabase.fakeMessageList.Count == 0)
                            {
                                WarningAndUnloadPlugin(LanguageEn ? "No messages were received during generation! Report this issue to the developer" : "Во время генерации не было получено сообщений! Сообщите разработчику об этой проблеме");
                                return;
                            }
                            
                            if (!statusInitialized[TypeInitialize.Messages].IsSuccess)
                                statusInitialized[TypeInitialize.Messages].IsSuccess = true;

                            Puts(LanguageEn ? $"Generating messages from the cloud - successful, received: {fakeDatabase.fakeMessageList.Count} messages" : $"Генерация сообщений с облака - успешно, было получено : {fakeDatabase.fakeMessageList.Count} сообщений");
                            break;
                    }
                }, this, timeout: 5f);
            }
            catch (Exception ex)
            {
                PrintError(LanguageEn ? $"Unexpected error while generating the player database from the cloud, error:\n\n{ex.ToString()}" : $"Неожиданная ошибка генерации базы игроков с облака, ошибка\n\n{ex.ToString()}");
            }
        }
        private const String ApiPlayers = "https://iqsystem.skyplugins.ru/iqsystem/iqfakeactive/getplayers/v5rsBqzl7wCvFqb45b56bb45/350";

        private readonly Dictionary<String, Int32> averageOnlineDataPattern = new()
        {
            { "00:00", 44 }, { "00:10", 41 }, { "00:20", 33 }, { "00:30", 27 }, { "00:40", 26 }, { "00:50", 25 },
            { "01:00", 23 }, { "01:10", 18 }, { "01:20", 14 }, { "01:30", 11 }, { "01:40", 11 }, { "01:50", 12 },
            { "02:00", 12 }, { "02:10", 8 }, { "02:20", 8 }, { "02:30", 8 }, { "02:40", 9 }, { "02:50", 9 },
            { "03:00", 7 }, { "03:10", 7 }, { "03:20", 8 }, { "03:30", 6 }, { "03:40", 8 }, { "03:50", 8 },
            { "04:00", 5 }, { "04:10", 5 }, { "04:20", 5 }, { "04:30", 4 }, { "04:40", 3 }, { "04:50", 3 }, 
            { "05:00", 3 }, { "05:10", 3 }, { "05:20", 4 }, { "05:30", 5 }, { "05:40", 4 }, { "05:50", 4 },
            { "06:00", 4 }, { "06:10", 4 }, { "06:20", 4 }, { "06:30", 2 }, { "06:40", 3 }, { "06:50", 2 },
            { "07:00", 1 }, { "07:10", 3 }, { "07:20", 4 }, { "07:30", 4 }, { "07:40", 5 }, { "07:50", 6 },
            { "08:00", 5 }, { "08:10", 7 }, { "08:20", 6 }, { "08:30", 4 }, { "08:40", 10 }, { "08:50", 9 },
            { "09:00", 11 }, { "09:10", 10 }, { "09:20", 12 }, { "09:30", 13 }, { "09:40", 15 }, { "09:50", 17 },
            { "10:00", 21 }, { "10:10", 21 }, { "10:20", 21 }, { "10:30", 23 }, { "10:40", 24 }, { "10:50", 25 },
            { "11:00", 25 }, { "11:10", 24 }, { "11:20", 30 }, { "11:30", 31 }, { "11:40", 30 }, { "11:50", 32 },
            { "12:00", 35 }, { "12:10", 37 }, { "12:20", 40 }, { "12:30", 38 }, { "12:40", 38 }, { "12:50", 35 },
            { "13:00", 39 }, { "13:10", 41 }, { "13:20", 41 }, { "13:30", 44 }, { "13:40", 48 }, { "13:50", 50 }, 
            { "14:00", 51 }, { "14:10", 56 }, { "14:20", 59 }, { "14:30", 67 }, { "14:40", 65 }, { "14:50", 67 },
            { "15:00", 67 }, { "15:10", 74 }, { "15:20", 71 }, { "15:30", 71 }, { "15:40", 73 }, { "15:50", 73 },
            { "16:00", 73 }, { "16:10", 74 }, { "16:20", 78 }, { "16:30", 78 }, { "16:40", 80 }, { "16:50", 79 },
            { "17:00", 75 }, { "17:10", 78 }, { "17:20", 81 }, { "17:30", 84 }, { "17:40", 79 }, { "17:50", 69 },
            { "18:00", 72 }, { "18:10", 71 }, { "18:20", 75 }, { "18:30", 75 }, { "18:40", 72 }, { "18:50", 69 },
            { "19:00", 72 }, { "19:10", 66 }, { "19:20", 65 }, { "19:30", 71 }, { "19:40", 70 }, { "19:50", 72 },
            { "20:00", 70 }, { "20:10", 69 }, { "20:20", 67 }, { "20:30", 69 }, { "20:40", 64 }, { "20:50", 62 }, 
            { "21:00", 60 }, { "21:10", 62 }, { "21:20", 62 }, { "21:30", 63 }, { "21:40", 64 }, { "21:50", 59 },
            { "22:00", 61 }, { "22:10", 60 }, { "22:20", 59 }, { "22:30", 54 }, { "22:40", 53 }, { "22:50", 50 },
            { "23:00", 47 }, { "23:10", 46 }, { "23:20", 44 }, { "23:30", 45 }, { "23:40", 42 }, { "23:50", 43 }
        };
        
        private String GetFakeMessage()
        {
            if (!IsFullInitialized) return null;
            if (fakeDatabase.fakeMessageList == null || fakeDatabase.fakeMessageList.Count == 0) return null;
		   		 		  						  	   		   					  						  						  	   
            String message = null;

            for (Int32 i = 0; i < maxAttemptsGetMessage; i++)
            {
                Int32 randomIndex = Core.Random.Range(0, fakeDatabase.fakeMessageList.Count);
                message = fakeDatabase.fakeMessageList[randomIndex].message;

                if (!lastMessages.Contains(message))
                {
                    lastMessages.Enqueue(message);
                    if (lastMessages.Count > 5)
                        lastMessages.Dequeue(); 
                    break;
                }
            }
            
            return message;
        }
        private Double TimeUnblockWeaponDeath(Int32 BlockTime) => (SaveRestore.SaveCreatedTime.ToUniversalTime().Subtract(epoch).TotalSeconds + BlockTime) - CurrentTime;

        private Timer timerCheckInitialized;
        private Queue<String> lastMessages = new();
		   		 		  						  	   		   					  						  						  	   
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

        private enum TypeInitialize
        {
            Messages,
            Players,
            Online,
            Networks,
            ChatInformation,
            Avatars,
        }
        private Timer timerDeathMessages;
		   		 		  						  	   		   					  						  						  	   
        
        
        private void GeneratePlayerPreset()
        {
            if (IQChat)
            {
                if (IQChat.Version < new Oxide.Core.VersionNumber(2, 39, 17))
                {
                    WarningAndUnloadPlugin(LanguageEn ? "You have an outdated version of the IQChat plugin, the plugin cannot access the API, update the IQChat plugin to version 2.39.17 or higher" : "У вас устаревшая версия плагина IQChat, плагин не может получить доступ к API, обновите плагин IQChat до версии 2.39.17 или выше");
                    return;
                }

                fakeDatabase.chatPresetFakePlayer.chatColor = IQChat.Call<String>("API_GET_DEFAULT_MESSAGE_COLOR");
                if (String.IsNullOrEmpty(fakeDatabase.chatPresetFakePlayer.chatColor))
                    fakeDatabase.chatPresetFakePlayer.chatColor = "#ffffff";
                
                fakeDatabase.chatPresetFakePlayer.chatPrefix = IQChat.Call<String>("API_GET_DEFAULT_PREFIX");
                
                fakeDatabase.chatPresetFakePlayer.nickColor = IQChat.Call<String>("API_GET_DEFAULT_NICK_COLOR"); 
                if (String.IsNullOrEmpty(fakeDatabase.chatPresetFakePlayer.nickColor))
                    fakeDatabase.chatPresetFakePlayer.nickColor = "#55aafe";
                
                fakeDatabase.chatPresetFakePlayer.sizeMessage = IQChat.Call<Int32>("API_GET_DEFAULT_SIZE_MESSAGE");
                fakeDatabase.chatPresetFakePlayer.sizeNick = IQChat.Call<Int32>("API_GET_DEFAULT_SIZE_NICK");
            }
            else
            {
                fakeDatabase.chatPresetFakePlayer.chatColor = "#ffffff"; 
                fakeDatabase.chatPresetFakePlayer.chatPrefix = String.Empty;
                fakeDatabase.chatPresetFakePlayer.nickColor = "#1F6BA0"; 
                fakeDatabase.chatPresetFakePlayer.sizeMessage = 14; 
                fakeDatabase.chatPresetFakePlayer.sizeNick = 14; 
            }
            
            statusInitialized[TypeInitialize.ChatInformation].IsSuccess = true;
            Puts(LanguageEn ? "Chat presets for fake players have been created" : "Созданы пресеты в чате для фейковых игроков");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                if (config.deathNoteController == null)
                {
                    config.deathNoteController = new Configuration.DeathNoteController()
                    {
                        useFakeDeathMessages = false,
                        weaponDeathNote = new List<Configuration.DeathNoteController.WeaponDeathNote>()
                        {
                            new()
                            {
                                secondWipeTime = 18000,
                                weapon = new List<String>()
                                {
                                    "bow.hunting",
                                    "bone.club",
                                    "salvaged.sword",
                                    "spear.wooden",
                                    "spear.stone",
                                    "pistol.nailgun",
                                    "shotgun.waterpipe",
                                }
                            },
                            new()
                            {
                                secondWipeTime = 25200,
                                weapon = new List<String>()
                                {
                                    "pistol.m92",
                                    "shotgun.double",
                                    "pistol.revolver",
                                    "pistol.semiauto",
                                    "pistol.python",
                                }
                            },
                            new()
                            {
                                secondWipeTime = 36000,
                                weapon = new List<String>()
                                {
                                    "smg.2",
                                    "smg.thompson",
                                    "rifle.semiauto",
                                    "smg.mp5",
                                }
                            },
                            new()
                            {
                                secondWipeTime = 86400,
                                weapon = new List<String>()
                                {
                                    "rifle.bolt",
                                    "rifle.lr300",
                                    "rifle.ak",
                                }
                            },
                            new()
                            {
                                secondWipeTime = 108000,
                                weapon = new List<String>()
                                {
                                    "rifle.l96",
                                    "rifle.m39",
                                    "multiplegrenadelauncher",
                                    "lmg.m249",
                                }
                            },
                        },
                        minTimeUpdateDeath = 60,
                        maxTimeUpdateDeath = 120,
                        useCorrectedInterval = false,
                        limitCorrected = 30,
                    };
                }
            }
            catch
            {
                PrintWarning(LanguageEn ? $"Error reading #54327 configuration 'oxide/config/{Name}', creating a new configuration!!" : $"Ошибка чтения #54327 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        private enum DatabaseType
        {
            Cloud,
            Local,
        }

        
                
        private IEnumerator GenerateAvatars()
        {
            if (ImageLibrary is null)
                yield break;

            foreach (FakeDatabase.FakePlayer fakePlayer in fakeDatabase.fakePlayerList)
            {
                webrequest.Enqueue($"https://steamcommunity.com/profiles/{fakePlayer.userId}?xml=1", null,
                    (code, response) =>
                    {
                        if (code != 200 || response is null)
                            return;

                        String avatarUrl = _avatarRegex.Match(response).Groups[1].ToString();
                        if (!String.IsNullOrEmpty(avatarUrl) && !HasImage(fakePlayer.userId))
                            AddImage(avatarUrl, fakePlayer.userId);
                    }, this, timeout: 2f);
                
                yield return new WaitForSeconds(0.1f);
            }

            statusInitialized[TypeInitialize.Avatars].IsSuccess = true;
            Puts(LanguageEn ? "Generating fake avatars for ImageLibrary - successful" : "Генерация фейковых аватарок для ImageLibrary - успешно");
        }
        
                
        private Timer initializationTimer;
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private UInt64 GenerateUniqueSteamID(UInt64 randomAmount = 0)
        {
            UInt64 steamId = (UInt64)Random.Range(76561100000000000, 76561199999999999) + randomAmount;
            if (usedSteamIds.Contains(steamId))
                return GenerateUniqueSteamID((UInt64)Random.Range(1, 20000));

            usedSteamIds.Add(steamId);
            return steamId;
        }

        public FakeDatabase fakeDatabase = new();
        private Timer timerNetworking;
        
        
        
        Boolean MuteAction(String idOrName, Boolean isMuted)
        {
            if (!IsActiveAPIPlayers()) return false;
            FakeDatabase.FakePlayer fakePlayer = fakeDatabase.fakePlayerList.FirstOrDefault(x => x.userId.Equals(idOrName) || x.displayName.Equals(idOrName));
            if (fakePlayer == null) return false;
            fakePlayer.isMuted = isMuted;
            return true;
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private Timer timerChatPmMessages;
        
        private void MessagePmImmitation()
        {
            if (!IsFullInitialized) return;
            if (!statusInitialized[TypeInitialize.ChatInformation].IsSuccess || 
                !statusInitialized[TypeInitialize.Messages].IsSuccess || 
                !statusInitialized[TypeInitialize.Players].IsSuccess) return;

            if(timerChatPmMessages is { Destroyed: false })
                timerChatPmMessages.Destroy();

            if (!IQChat) return;
            
            FakeDatabase.FakePlayer fakePlayer = GetRandomFakePlayer();
            if (fakePlayer == null) return;

            BasePlayer realPlayer = BasePlayer.activePlayerList[Random.Range(0, BasePlayer.activePlayerList.Count)];
            if (realPlayer == null) return;

            String fakeMessage = GetPmFakeMessage(realPlayer);
            if (fakeMessage == null) return;

            IQChat.Call("API_SEND_PLAYER_PM", realPlayer, fakePlayer.displayName, fakePlayer.userId, fakeMessage);
            LogToConsole(LanguageEn ? $"Sent a fake private message to player {realPlayer.displayName} => [{fakeDatabase.chatPresetFakePlayer.chatPrefix}]{fakePlayer.displayName}: {fakeMessage}" : $"Отправлено фейковое сообщение в личные сообщения игроку {realPlayer.displayName} => [{fakeDatabase.chatPresetFakePlayer.chatPrefix}]{fakePlayer.displayName} : {fakeMessage}");
            timerChatPmMessages = timer.Once(Random.Range(config.messageFakeController.minTimeUpdatePmChat, config.messageFakeController.maxTimeUpdatePmChat), MessagePmImmitation);
        }
        
        private String GetPmFakeMessage(BasePlayer player)
        {
            if (!IsFullInitialized) return null;
            if (config.messageFakeController.localDatabasePmMessage == null || config.messageFakeController.localDatabasePmMessage.Count == 0) return null;

            String message = null;

            if (!lastPlayerPmMessage.ContainsKey(player))
            {
                message = config.messageFakeController.localDatabasePmMessage[Core.Random.Range(0, config.messageFakeController.localDatabasePmMessage.Count)];
                lastPlayerPmMessage.Add(player, message);
                return message;
            }
            
            for (Int32 i = 0; i < maxAttemptsGetMessage; i++)
            {
                Int32 randomIndex = Core.Random.Range(0, config.messageFakeController.localDatabasePmMessage.Count);
                message = config.messageFakeController.localDatabasePmMessage[randomIndex];
		   		 		  						  	   		   					  						  						  	   
                if (!lastPlayerPmMessage[player].Equals(message))
                {
                    lastPlayerPmMessage[player] = message;
                    break;
                }
            }

            lastPlayerPmMessage[player] = message;
            
            return message;
        }
        private String GetReasonDisconnected() => config.networkFakeController.reasonDisconnectedList != null && config.networkFakeController.reasonDisconnectedList.Count != 0 ? config.networkFakeController.reasonDisconnectedList[Random.Range(0, config.networkFakeController.reasonDisconnectedList.Count)] : null;

        private Dictionary<TypeInitialize, InitializedStatus> statusInitialized = new()
        {
            [TypeInitialize.Messages] = new InitializedStatus()
            {
                IsInit = false,
                IsSuccess = false,
            },
            [TypeInitialize.Players] = new InitializedStatus()
            {
                IsInit = false,
                IsSuccess = false,
            },
            [TypeInitialize.Online] = new InitializedStatus 
            {
                IsInit = false,
                IsSuccess = false,
            },
            [TypeInitialize.Networks] = new InitializedStatus 
            {
                IsInit = false,
                IsSuccess = false,
            },
            [TypeInitialize.ChatInformation] = new InitializedStatus 
            {
                IsInit = false,
                IsSuccess = false,
            },
            [TypeInitialize.Avatars] = new InitializedStatus 
            {
                IsInit = false,
                IsSuccess = false,
            },
        };
        
        
        
        Boolean IsReady() => IsFullInitialized;

        private void DisconnectPlayer()
        {
            if (fakeDatabase.fakePlayerConnection.Count == 0)
                return;

            Int32 randomIndexDisconnected = Random.Range(0, fakeDatabase.fakePlayerConnection.Count);
            FakeDatabase.FakePlayer playerDisconnected = fakeDatabase.fakePlayerConnection[randomIndexDisconnected];
            String reason = GetReasonDisconnected();
            if (reason == null) return;
            
            IQChat.Call("API_SEND_PLAYER_DISCONNECTED", playerDisconnected.displayName, reason, playerDisconnected.userId);

            fakeDatabase.fakePlayerConnection.Add(playerDisconnected);
            fakeDatabase.fakePlayerDisconnection.RemoveAt(randomIndexDisconnected);

            lastNetworkType = NetworkType.Disconnected;
            LogToConsole(LanguageEn ? $"Simulating player disconnection: {playerDisconnected.displayName}" : $"Имитация отключения игрока : {playerDisconnected.displayName}");
        }
        private List<FakeDatabase.FakePlayer> GetFInPlayers() => BasePlayer.activePlayerList
            .Select(player => new FakeDatabase.FakePlayer
            {
                userId = player.UserIDString,
                displayName = player.displayName
            }).ToList();
        private void GeneratePlayers()
        {
            Configuration.PlayerFakeController playerDbConfigure = config.playerFakeController;
            if (playerDbConfigure.playersDbType == DatabaseType.Local)
            {
                List<FakeDatabase.FakePlayer> localFakePlayers = new();
                if (playerDbConfigure.localDatabaseNickName == null || playerDbConfigure.localDatabaseNickName.Count == 0)
                {
                    WarningAndUnloadPlugin(LanguageEn ? "The local database does not contain any nicknames, generation is impossible" : "В локальной базе данных не указаны ники, генерация невозможна");
                    return;
                }
                
                List<String> distinctNickList = playerDbConfigure.localDatabaseNickName.Distinct().ToList();

                List<UInt64> distinctSteamIDsList = new();
                if(playerDbConfigure.localDatabaseSteamIds != null)
                    distinctSteamIDsList = playerDbConfigure.localDatabaseSteamIds.Distinct().ToList();
                
                for (Int32 player = 0; player < distinctNickList.Count; player++)
                {
                    UInt64 steamId;
                    String displayName = playerDbConfigure.localDatabaseNickName[player];

                    if (distinctSteamIDsList.Count != 0 && player < distinctSteamIDsList.Count)
                        steamId = distinctSteamIDsList[player];
                    else steamId = GenerateUniqueSteamID();

                    FakeDatabase.FakePlayer fakePlayer = new()
                    {
                        displayName = displayName,
                        userId = steamId.ToString(),
                    };

                    localFakePlayers.Add(fakePlayer);
                }

                fakeDatabase.fakePlayerList = localFakePlayers;
               
                statusInitialized[TypeInitialize.Players].IsInit = true;
                statusInitialized[TypeInitialize.Players].IsSuccess = true;
                Puts(LanguageEn ? $"Generation of players from the local database - successful, {fakeDatabase.fakePlayerList.Count} players were obtained" : $"Генерация игроков с локальной базы - успешно, было получено : {fakeDatabase.fakePlayerList.Count} игроков");

                return;
            }

            try
            {
                webrequest.Enqueue(ApiPlayers, null, (code, response) =>
                {
                    switch (code)
                    {
                        case 503:
                            WarningAndUnloadPlugin(LanguageEn ? "An update for the plugin is available. Please update the plugin version to allow database connection" : "Вышло обновление плагина, обновите версию плагина чтобы вы могли подключаться в базе-данных!");
                            return;
                        case 404:
                        case 500:
                        case 401:
                            WarningAndUnloadPlugin(LanguageEn ? "An error occurred in the database server. Please report this to the developer" : "Произошла ошибка на сервере базы-данных, сообщите разработчику об этом");
                            return;
                        case 429:
                            WarningAndUnloadPlugin(LanguageEn ? "The request limit to the cloud database has been exceeded! Your IP is blocked for 2 hours" : "Превышен лимит запросов к облачной базе данных! Ваш IP заблокирован на 2 часа");
                            return;
                        default:
                            if (response == null)
                            {
                                WarningAndUnloadPlugin(LanguageEn ? "An error occurred on the database server. The response came back empty (null). Please report this to the developer" : "Произошла ошибка на сервере базы-данных, ответ пришел пустым (null) сообщите разработчику об этом");
                                return;
                            }
                            
                            fakeDatabase.fakePlayerList = JsonConvert.DeserializeObject<List<FakeDatabase.FakePlayer>>(response);

                            if (!statusInitialized[TypeInitialize.Players].IsSuccess)
                                statusInitialized[TypeInitialize.Players].IsSuccess = true;
                            
                            Puts(LanguageEn ? $"Players generation from the cloud - successful, {fakeDatabase.fakePlayerList.Count} player templates were received" : $"Генерация игроков с облака - успешно, было получено : {fakeDatabase.fakePlayerList.Count} шаблонов игроков");
                            break;
                    }
                }, this, timeout:120f);
            }
            catch (Exception ex)
            {
                PrintError(LanguageEn ? $"Unexpected error while generating the player database from the cloud, error:\n\n{ex.ToString()}" : $"Неожиданная ошибка генерации базы игроков с облака, ошибка\n\n{ex.ToString()}");
            }
        }

        
        
        private void MessageImmitation()
        {
            if (!IsFullInitialized) return;
            if (!statusInitialized[TypeInitialize.ChatInformation].IsSuccess || 
                !statusInitialized[TypeInitialize.Messages].IsSuccess || 
                !statusInitialized[TypeInitialize.Players].IsSuccess) return;

            if(timerChatMessages is { Destroyed: false })
                timerChatMessages.Destroy();
            
            FakeDatabase.FakePlayer fakePlayer = GetRandomFakePlayer();
            if (fakePlayer == null) return;
            
            String fakeMessage = GetFakeMessage();
            if (fakeMessage == null) return;

            String fakeDisplayName = $"<size={fakeDatabase.chatPresetFakePlayer.sizeNick}><color={fakeDatabase.chatPresetFakePlayer.nickColor}>{fakePlayer.displayName}</color></size>";
            String fakeFormatPlayer = $"{fakeDatabase.chatPresetFakePlayer.chatPrefix} {fakeDisplayName}";
            String fakeMessageFormat = $"<size={fakeDatabase.chatPresetFakePlayer.sizeMessage}><color={fakeDatabase.chatPresetFakePlayer.chatColor}>{fakeMessage}</color></size>"; 
            
            LogToConsole(LanguageEn ? $"Message sent in chat: [{fakeDatabase.chatPresetFakePlayer.chatPrefix}]{fakeDisplayName}: {fakeMessage}" : $"Отправлено сообщение в чат : [{fakeDatabase.chatPresetFakePlayer.chatPrefix}]{fakeDisplayName} : {fakeMessage}");
		   		 		  						  	   		   					  						  						  	   
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (IQChat)
                    IQChat.CallHook("API_SEND_PLAYER", player, fakeFormatPlayer, fakeMessageFormat, $"{fakePlayer.userId}");
                else player.SendConsoleCommand("chat.add", 0, fakePlayer.userId, $"{fakeFormatPlayer}: {fakeMessageFormat}");
            }
            
            timerChatMessages = timer.Once(Random.Range(config.messageFakeController.minTimeUpdateChat, config.messageFakeController.maxTimeUpdateChat), MessageImmitation);
        }
        
                
        
        private void RefresgDataBase()
        {
            if(timerRefreshDatabase is { Destroyed: false })
                timerRefreshDatabase.Destroy();
            
            if (!IsFullInitialized) return;
            
            GenerateMessages();
            GenerateOnline();

            timerRefreshDatabase = timer.Once(600, RefresgDataBase);
        }

        
                
        JObject GetListPlayers()
        {
            if (!IsActiveAPIPlayers()) return null;

            List<FakeDatabase.FakePlayer> realPlayers = GetFInPlayers();
            List<FakeDatabase.FakePlayer> realAndFake = new();
            realAndFake.AddRange(realPlayers);
            realAndFake.AddRange(fakeDatabase.fakePlayerList);
		   		 		  						  	   		   					  						  						  	   
            fakeDatabase.realAndFakePlayerList = realAndFake;
            
            return JObject.FromObject(new { players = fakeDatabase.realAndFakePlayerList.Take(GetOnline()) });
        }

        private void Unload()
        {
            if (initializationTimer != null)
            {
                initializationTimer.Destroy();
                initializationTimer = null;
            }
            
            if (timerRefreshDatabase != null)
            {
                timerRefreshDatabase.Destroy();
                timerRefreshDatabase = null;
            }
            
            if (timerChatMessages != null)
            {
                timerChatMessages.Destroy();
                timerChatMessages = null;
            }

            if (timerChatPmMessages != null)
            {
                timerChatPmMessages.Destroy();
                timerChatPmMessages = null;
            }
            
            if (timerNetworking != null)
            {
                timerNetworking.Destroy();
                timerNetworking = null;
            }
            
            if (timerDeathMessages != null)
            {
                timerDeathMessages.Destroy();
                timerDeathMessages = null;
            }

            if (routineAddedAvatars != null)
            {
                ServerMgr.Instance.StopCoroutine(routineAddedAvatars);
                routineAddedAvatars = null;
            }
        }        

        private FakeDatabase.FakePlayer GetRandomFakePlayer()
        {
            if (!IsFullInitialized) return null;
            if (fakeDatabase.fakePlayerList == null || fakeDatabase.fakePlayerList.Count == 0) return null;

            for (Int32 i = 0; i < maxAttemptsGetRandomFakePlayer; i++)
            {
                Int32 indexRandom = Core.Random.Range(0, fakeDatabase.fakePlayerList.Count);
                FakeDatabase.FakePlayer randomPlayer = fakeDatabase.fakePlayerList[indexRandom];

                if (!randomPlayer.isMuted)
                    return randomPlayer;
            }

            return null;
        }
                                                          
        private readonly String ApiMessages = $"https://iqsystem.skyplugins.ru/iqsystem/iqfakeactive/getmessages/v5rsBqzl7wCvFqb45b56bb45/600/{LanguageEn}";

                private HashSet<UInt64> usedSteamIds = new HashSet<UInt64>();
        private Int32 GenerateOnlineCount(Int32 day = -1, Int32 hourse = -1, Int32 minute = -1)
        {
            if (config.onlineController.onlineMaxPercent is > 100 or < 0)
            {
                WarningAndUnloadPlugin(LanguageEn ? "You have indicated an online ceiling of more than 100% or less than 0%, indicate a range from 0 to 100" : "У вас указан потолок онлайна больше 100% или меньше 0%, укажите диапазон от 0 до 100");
                return 0;
            }
            Int32 playersCount = BasePlayer.activePlayerList.Count;
            Int32 maxSlots = ConVar.Server.maxplayers - playersCount;
            Int32 allGeneratedPlayers = fakeDatabase.fakePlayerList.Count;
            DateTime now = DateTime.Now;
            Int32 currentHour = hourse != -1 ? hourse : now.Hour;
            Int32 currentMinute = (Int32)(Math.Ceiling(minute != -1 ? minute : now.Minute / 10.0) * 10);
            
            if (currentMinute >= 60)
            {
                currentHour++; 
                currentMinute = 0;  
            }

            if (currentHour == 24)
                currentHour = 00;
            
            String key = $"{currentHour:D2}:{currentMinute:D2}";
            if (!averageOnlineDataPattern.TryGetValue(key, out Int32 dataOnline)) return 0;

            Int32 onlineOffset = Math.Abs(config.onlineController.onlineOffset);
            
            Int32 offsetOnline = Oxide.Core.Random.Range(-onlineOffset, onlineOffset);
            Int32 resultDataOnline = (dataOnline + offsetOnline) < offsetOnline || (dataOnline + offsetOnline) <= 0 ? dataOnline : (dataOnline + offsetOnline);
            Int32 desiredOnline = (Int32)((resultDataOnline * (maxSlots / 100.0f) * GetPercentWipeOnline(day != -1 ? day : WipeTime)) + config.onlineController.onlineMoreAmount);
            Int32 maxOnline = (Int32)(maxSlots * GetPercentMaxFake);

            if (maxOnline > allGeneratedPlayers)
                maxOnline = allGeneratedPlayers;

            return desiredOnline > maxOnline ? maxOnline : desiredOnline <= 0 ? 0 : desiredOnline;
        }
        Boolean IsFakeUser(String idOrName) => IsActiveAPIPlayers() && fakeDatabase.fakePlayerList.Any(x => x.userId.Equals(idOrName) || x.displayName.ToLower().Contains(idOrName.ToLower()));

        private String GetNameDeathNote() => fakeDatabase.fakePlayerList.Count > 0 ? fakeDatabase.fakePlayerList.GetRandom().displayName : String.Empty;
        private const Boolean LanguageEn = false;

        private String GetWeaponDeathNote()
        {
            if (config.deathNoteController.weaponDeathNote == null ||
                config.deathNoteController.weaponDeathNote.Count == 0) return String.Empty;

            List<Configuration.DeathNoteController.WeaponDeathNote> listWeapon = config.deathNoteController.weaponDeathNote.Where(x => TimeUnblockWeaponDeath(x.secondWipeTime) <= 0).ToList();
            return listWeapon.Count == 0 ? String.Empty : listWeapon.GetRandom().weapon.GetRandom();
        }

        String GetFakeName(String userId)
        {
            if (!IsActiveAPIPlayers()) return "initializePlugin";
            FakeDatabase.FakePlayer fakePlayer = fakeDatabase.fakePlayerList.FirstOrDefault(x => x.userId.Equals(userId));
            return fakePlayer == null ? "notFindedUser" : fakePlayer.displayName;
        }
        private const Int32 maxAttemptsGetRandomFakePlayer = 5;

        private Dictionary<BasePlayer, String> lastPlayerPmMessage = new(); 

                
            }
}
