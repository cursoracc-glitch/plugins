using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Facepunch;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("EventManager", "senyaa & M&B-Studios", "2.2.2")]
    class EventManager : RustPlugin
    {
        #region Constants
        const string USE_PERMISSION = "eventmanager.use";

        const string SELECTED_CUI_NAME = "EventManager_EventSelected";
        const string EDITENTRY_CUI_NAME = "EventManager_EditEntry";
        const string SELECT_CUI_NAME = "EventManager_SelectUI";

        const string SELECT_CCMD = "EM_SELECT";
        const string SET_ENTRY_CCMD = "EM_SETENTRY";
        const string SAVE_ENTRY_CCMD = "EM_SAVEENTRY";
        const string EDIT_ENTRY_CCMD = "EM_EDITENTRY";
        const string SHOW_PAGE_CCMD = "EM_SHOWPAGE";
        const string DELETE_ENTRY_CCMD = "EM_DELETEENTRY";
        const string NEW_ENTRY_CCMD = "EM_NEWENTRY";
        #endregion

        #region Fields
        static EventManager Instance;
        EventController controller;

        readonly Dictionary<int, string> DayList = new Dictionary<int, string>()
        {
            [1] = "Monday",
            [2] = "Tuesday",
            [3] = "Wednesday",
            [4] = "Thursday",
            [5] = "Friday",
            [6] = "Saturday",
            [0] = "Sunday",
        };
        #endregion

        [ConsoleCommand("em_spawn")]
        private void cmdSpawn(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!arg.Player().IsAdmin)
                    return;
            
            if (arg.Args.IsNullOrEmpty())
                return;

            var prefabname = String.Join(" ", arg.Args);


            var baseEntity = GameManager.server.CreateEntity(prefabname);
            if (baseEntity)
            {
                if (baseEntity.TryGetComponent<CH47HelicopterAIController>(out var ch47))
                {
                    ch47.TriggeredEventSpawn();
                }
                if (baseEntity.TryGetComponent<CargoShip>(out var cargo))
                {
                    cargo.TriggeredEventSpawn();
                }
                baseEntity.Spawn();
                
            }
        }

        #region Event List
        static readonly EventSettings RandomStartEv = new EventSettings()
        {
            displayName = "RANDOM START"
        };//

        #region Data

        private void SaveData()
        {
            var eventlistb = EventList.ToDictionary(p => $"{p.Key.Name}|{p.Key.Color}|{Guid.NewGuid().ToString()}", p => p.Value);
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/events", eventlistb);
        }

        private void TryCreateNewDataFile()
        {
            if (EventList.IsNullOrEmpty())
            {
                EventList = new Dictionary<AuthorName, List<EventSettings>>()
                {
                    [new AuthorName()
                    {
                        Name = "Facepunch",
                        Color = "1 1 1 1"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Helicopter",
                            Command = "em_spawn assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab",
                        },
                        new EventSettings()
                        {
                            displayName = "Cargoship",
                            Command = "em_spawn assets/content/vehicles/boats/cargoship/cargoshiptest.prefab",
                        },
                        new EventSettings()
                        {
                            displayName = "Airdrop",
                            Command = "em_spawn assets/prefabs/misc/supply drop/supply_drop.prefab",
                        },
                        new EventSettings()
                        {
                            displayName = "Chinook 47",
                            Command = "em_spawn assets/prefabs/npc/ch47/ch47scientists.entity.prefab",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "KpucTaJl",
                        Color = "0.733 0.122 0.004 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Air Event",
                            Command = "airstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Water Event",
                            Command = "waterstart ",
                        },
                        new EventSettings()
                        {
                            displayName = "Arctic Base Event",
                            Command = "abstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Satellite Dish Event",
                            Command = "satdishstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Junkyard Event",
                            Command = "jstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Power Plant Event",
                            Command = "ppstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Harbor Event",
                            Command = "harborstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Squid Game",
                            Command = "rlglstart",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "KpucTaJl",
                        Color = "0.733 0.122 0.004 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "GasStation Event",
                            Command = "gsstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Defendable-Bases Start",
                            Command = "warstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Defendable-Bases Stop",
                            Command = "warstop",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Adem",
                        Color = "0.424 0.000 0.988 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Sputnik",
                            Command = "sputnikstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Armored Train",
                            Command = "atrainstart",
                        },
                        new EventSettings()
                        {
                            displayName = "Space",
                            Command = "spacestart",
                        },
                        new EventSettings()
                        {
                            displayName = "Convoy",
                            Command = "convoystart",
                        },
                        new EventSettings()
                        {
                            displayName = "Shipwreck",
                            Command = "shipwreckstart ",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Mercury",
                        Color = "1.000 0.573 0.000 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "IQSphereEvent",
                            Command = "iqsp start",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "DezLife",
                        Color = "0.659 1.000 0.000 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Cobalt Laboratory",
                            Command = "cl start",
                        },
                        new EventSettings()
                        {
                            displayName = "XDChinookEvent",
                            Command = "chinook call",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Cashr",
                        Color = "0.000 0.224 0.863 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Rocket Event",
                            Command = "rocket ||",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Nikedemos",
                        Color = "1.000 0.000 0.600 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "CargoTrainEvent",
                            Command = "trainevent_now",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Ridamees",
                        Color = "0.071 0.478 0.059 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Stone Eventt",
                            Command = "StoneStart",
                        },
                        new EventSettings()
                        {
                            displayName = "Metal Event",
                            Command = "MetalStart",
                        },
                        new EventSettings()
                        {
                            displayName = "Sulfur Event",
                            Command = "SulfurStart",
                        },
                        new EventSettings()
                        {
                            displayName = "RaidRush Event",
                            Command = "raidrush.start",
                        },
                        new EventSettings()
                        {
                            displayName = "BarrelBash Event",
                            Command = "StartBarrelBash",
                        },
                        new EventSettings()
                        {
                            displayName = "BoxBattle Event",
                            Command = "StartBoxBattle",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Fruster",
                        Color = "0.455 0.106 0.059 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "CargoPlaneEvent",
                            Command = "callcargoplane",
                        },
                        new EventSettings()
                        {
                            displayName = "AirfieldEvent",
                            Command = "afestart",
                        },
                        new EventSettings()
                        {
                            displayName = "HelipadEvent",
                            Command = "hpestart",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Bazz3l",
                        Color = "0.075 0.682 0.925 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "GuardedCrate Easy",
                            Command = "gcrate start Easy",
                        },
                        new EventSettings()
                        {
                            displayName = "GuardedCrate Medium",
                            Command = "gcrate start Medium",
                        },
                        new EventSettings()
                        {
                            displayName = "GuardedCrate Hard",
                            Command = "gcrate start Hard",
                        },
                        new EventSettings()
                        {
                            displayName = "GuardedCrate Elite",
                            Command = "gcrate start Elite",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Nivex",
                        Color = "0.949 0.651 0.996 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Dangerous Treasures",
                            Command = "dtevent",
                        },
                        new EventSettings()
                        {
                            displayName = "RaidableBases Easy",
                            Command = "rbevent easy",
                        },
                        new EventSettings()
                        {
                            displayName = "RaidableBases Medium",
                            Command = "rbevent easy",
                        },
                        new EventSettings()
                        {
                            displayName = "RaidableBases Hard",
                            Command = "rbevent easy",
                        },
                        new EventSettings()
                        {
                            displayName = "RaidableBases Nightmare",
                            Command = "rbevent easy",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Dana",
                        Color = "0.698 0.024 0.678 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Heli Regular",
                            Command = "heli.call Regular",
                        },
                        new EventSettings()
                        {
                            displayName = "Heli Millitary",
                            Command = "heli.call Millitary",
                        },
                        new EventSettings()
                        {
                            displayName = "Heli Elite",
                            Command = "heli.call Elite",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Razor",
                        Color = "0.243 0.204 0.690 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Jet Event",
                            Command = "jet event 2 2 10",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "ThePitereq",
                        Color = "0.851 0.612 0.208 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Meteor-Event-Kill",
                            Command = "ms kill",
                        },
                        new EventSettings()
                        {
                            displayName = "Meteor-Event-Start-Amount-10",
                            Command = "ms run 10",
                        },
                        new EventSettings()
                        {
                            displayName = "Meteor-Event-Start-Amount-20",
                            Command = "ms run 20",
                        },
                        new EventSettings()
                        {
                            displayName = "Meteor-Event-Start-Amount-30",
                            Command = "ms run 30",
                        },
                        new EventSettings()
                        {
                            displayName = "Meteor-Event-Start-Amount-40",
                            Command = "ms run 40",
                        },
                        new EventSettings()
                        {
                            displayName = "Meteor-Event-Start-Amount-50",
                            Command = "ms run 50",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = " k1lly0u",
                        Color = "0.455 0.106 0.159 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "PilotEjec",
                            Command = "pe call",
                        },
                        new EventSettings()
                        {
                            displayName = "HeliRefuel",
                            Command = "hr call",
                        },
                    },
                    [new AuthorName()
                    {
                        Name = "Wrecks",
                        Color = "0.561 0.498 0.718 1.00"
                    }] = new List<EventSettings>()
                    {
                        new EventSettings()
                        {
                            displayName = "Bot-Purge-Event",
                            Command = "Purge",
                        },
                        new EventSettings()
                        {
                            displayName = "Eradication Event",
                            Command = "Erad",
                        },
                    }
                };
                SaveData();
            }
        }
        
        void LoadData()
        {
            var EventListb = Interface.Oxide?.DataFileSystem?.ReadObject<Dictionary<string, List<EventSettings>>>($"{Title}/events")
                        ?? new Dictionary<string, List<EventSettings>>();

            EventList = EventListb.ToDictionary(p => new AuthorName(p.Key.Split('|')[0], p.Key.Split('|')[1]),
                p => p.Value);
            
            
            TryCreateNewDataFile();
        }

        #endregion
        
        static Dictionary<AuthorName, List<EventSettings>> EventList = new Dictionary<AuthorName, List<EventSettings>>();
        static EventSettings GetEventByName(string name)
        {
            if (name == "RANDOM START")
                return RandomStartEv;

            foreach (var events in EventList.Values)
            {
                foreach (var setting in events)
                {
                    if (setting.displayName == name)
                    {
                        return setting;
                    }
                }
            }
            return null;
        }
        #endregion

        #region Configuration
        static Configuration config;
        class Configuration
        {
            [JsonProperty(PropertyName = "Time Entries", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, EventSettingsEntry> Entries = new Dictionary<string, EventSettingsEntry>();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new Exception();
                }

                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
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

        #region Types
        internal class AuthorName
        {
            public AuthorName(){}
            public AuthorName(string name, string color)
            {
                this.Name = name;
                this.Color = color;
            }
            public string Name;
            public string Color;
        }
        internal class EventSettings
        {
            [JsonProperty(PropertyName = "Event name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string displayName = "Air Event";

            [JsonProperty(PropertyName = "The command to launch the event", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Command = "airevent start";

            [JsonProperty(PropertyName = "Color", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Color = "0.1 0.1 0.1 0.95";
        }

        internal class EventSettingsEntry
        {
            public string GUID = "";
            public string Name = "";
            public bool StaticTime = true;
            public int Hour = 12;
            public int Minute = 30;
            public int MinPlayers = 0;
            public DateTime LastRun;
            public Dictionary<int, bool> DaysActive = new Dictionary<int, bool>()
            {
                [1] = false,
                [2] = false,
                [3] = false,
                [4] = false,
                [5] = false,
                [6] = false,
                [0] = false,
            };

            public List<string> RandomStartEvents = new List<string>();
            public string PrevStartedEv = "";

            public EventSettingsEntry(string guid, string name)
            {
                GUID = guid;
                Name = name;
            }
            public static string NewEntry(string name)
            {
                var guid = Guid.NewGuid().ToString();
                config.Entries.Add(guid, new EventSettingsEntry(guid, name));
                return guid;
            }
        }

        
        class EventController : FacepunchBehaviour
        {
            private void Awake()
            {
                InvokeRepeating(CheckEvents, 5, 5);
            }

            private List<string> ExecutedEvents = new();
            private void CheckEvents()
            {
                var date = DateTime.Now;
                var day = Convert.ToInt32(date.DayOfWeek);
                var hour = date.Hour;
                var minute = date.Minute;

                foreach (var entry in config.Entries.Values)
                {
                    if (!entry.StaticTime)
                    {
                        continue;
                    }

                    if (!entry.DaysActive[day])
                    {
                        continue;
                    }

                    if (entry.Hour == hour && entry.Minute == minute)
                    {
                        if (entry.LastRun.Minute == minute && entry.LastRun.Hour == hour && entry.LastRun.DayOfWeek == date.DayOfWeek)
                            continue;
                        
                        entry.LastRun = date;
                        if (BasePlayer.activePlayerList.Count < entry.MinPlayers)
                            continue;

                        if (entry.Name == "RANDOM START" && entry.RandomStartEvents.Count > 0)
                        {
                            // var events = new List<string>();
                            // events.AddRange(entry.RandomStartEvents.Where(x => x != entry.PrevStartedEv));
                            // events.Shuffle((uint)UnityEngine.Random.Range(1, 1000000000));
                            //
                            // var evToRun = events.GetRandom();
                            // if(entry.RandomStartEvents.Count > 1)
                            // {
                            //     while (evToRun == entry.PrevStartedEv)
                            //         evToRun = events.GetRandom();
                            // } 
                            var validEvents = entry.RandomStartEvents.Where(e => !ExecutedEvents.Contains(e)).ToArray();
                            if (validEvents.IsNullOrEmpty())
                            {
                                if (ExecutedEvents.IsNullOrEmpty())
                                {
                                    Instance.PrintError("Not founded events to start!");
                                    return;
                                }
                                ExecutedEvents.Clear();
                                CheckEvents();
                                return;
                            }
                            int index = new System.Random().Next(validEvents.Length);
                            var evToRun = validEvents[index];
                            ExecutedEvents.Add(evToRun);
                            entry.PrevStartedEv = evToRun;
                            Instance.rust.RunServerCommand(GetEventByName(evToRun).Command);
                            Instance.CallStartedHook(GetEventByName(evToRun).displayName);
                            continue;
                        }
                        Instance.rust.RunServerCommand(GetEventByName(entry.Name).Command);
                        Instance.CallStartedHook(GetEventByName(entry.Name).displayName);
                    }
                }

                foreach (var entry in config.Entries.Values)
                {
                    if (entry.StaticTime)
                    {
                        continue;
                    }

                    if (!entry.DaysActive[day])
                    {
                        continue;
                    }

                    if ((date - entry.LastRun).TotalMinutes > ((entry.Hour * 60) + entry.Minute))
                    {
                        if (entry.LastRun.Minute == minute && entry.LastRun.Hour == hour && entry.LastRun.DayOfWeek == date.DayOfWeek)
                            continue;
                        
                        entry.LastRun = date;
                        if (BasePlayer.activePlayerList.Count < entry.MinPlayers)
                            continue;

                        if (entry.Name == "RANDOM START" && entry.RandomStartEvents.Count > 0)
                        {
                            var validEvents = entry.RandomStartEvents.Where(e => !ExecutedEvents.Contains(e)).ToArray();
                            if (validEvents.IsNullOrEmpty())
                            {
                                if (ExecutedEvents.IsNullOrEmpty())
                                {
                                    Instance.PrintError("Not founded events to start!");
                                    return;
                                }
                                ExecutedEvents.Clear();
                                CheckEvents();
                                return;
                            }
                            
                            int index = new System.Random().Next(validEvents.Length);
                            var evToRun = validEvents[index];
                            ExecutedEvents.Add(evToRun);
                            
                            // var evToRun = entry.RandomStartEvents.GetRandom();
                            // while(evToRun == entry.PrevStartedEv && entry.RandomStartEvents.Count > 1)
                            //     evToRun = entry.RandomStartEvents.GetRandom();
                            entry.PrevStartedEv = evToRun;
                            Instance.rust.RunServerCommand(GetEventByName(evToRun).Command);
                            Instance.CallStartedHook(GetEventByName(evToRun).displayName);
                            continue;
                        }

                        Instance.rust.RunServerCommand(GetEventByName(entry.Name).Command);
                        Instance.CallStartedHook(GetEventByName(entry.Name).displayName);
                    }
                }
            }
        }
        #endregion

        #region Oxide Hooks
        void Init()
        {
            Instance = this;
        }

        void OnServerInitialized()
        {
            LoadData();

            permission.RegisterPermission(USE_PERMISSION, this);
            timer.Once(3f, () =>
            {
                controller = ServerMgr.Instance.gameObject.AddComponent<EventController>();
            });
        }

        private void OnServerSave() => SaveData();
        
        void Unload()
        {
            SaveData();
            SaveConfig();
            UnityEngine.Object.DestroyImmediate(controller);
            Instance = null;
        }

        
        private Vector3 GetRandomSpawnPosCh47(){return TerrainMeta.RandomPointOffshore();}
        #endregion

        #region UI

        internal class NewEventData
        {
            public string Name;
            public string Command;
            public AuthorName Author;

            public EventSettings ToEntry()
            {
                var entry = new EventSettings
                {
                    displayName = Name,
                    Command = Command,
                    Color = "0.1 0.1 0.1 0.95"
                };
                return entry;
            }
        }
        private Dictionary<ulong, NewEventData> _creatingEvents = new Dictionary<ulong, NewEventData>();
        private const string Layer = "ui.EventManager.bg";

        [ConsoleCommand("em_createconfirm")]
        private void cmdCreateConfirm(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (!arg.Player().IPlayer.HasPermission(USE_PERMISSION))
                return;
            
            if (!_creatingEvents.ContainsKey(arg.Player().userID))
                return;
            var e = _creatingEvents[arg.Player().userID];
            
            
            EventList[e.Author].Add(e.ToEntry());

            _creatingEvents.Remove(arg.Player().userID);
            SaveData();
            CuiHelper.DestroyUi(arg.Player(), Layer);
        }

        [ConsoleCommand("em_cancelcreate")]
        private void cmdCancelCreate(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (!arg.Player().IPlayer.HasPermission(USE_PERMISSION))
                return;
            
            _creatingEvents.Remove(arg.Player().userID);
            CuiHelper.DestroyUi(arg.Player(), Layer);
        }
        [ConsoleCommand("em_setname")]
        private void cmdSetName(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (!arg.Player().IPlayer.HasPermission(USE_PERMISSION))
                return;
            
            if (!_creatingEvents.TryGetValue(arg.Player().userID, out var ev))
                return;

            ev.Name = string.Join(" ", arg.Args);
        }
        [ConsoleCommand("em_setcommand")]
        private void cmdSetCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (!arg.Player().IPlayer.HasPermission(USE_PERMISSION))
                return;
            
            if (!_creatingEvents.TryGetValue(arg.Player().userID, out var ev))
                return;

            ev.Command = string.Join(" ", arg.Args);
        }
        [ConsoleCommand("em_setauthor")]
        private void cmdSetAuthor(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (!arg.Player().IPlayer.HasPermission(USE_PERMISSION))
                return;
            
            if (!_creatingEvents.TryGetValue(arg.Player().userID, out var ev))
                return;

            ev.Author = EventList.Select(x => x.Key).FirstOrDefault(x => x.Name.Contains(arg.Args[0]))!;
            
            CuiHelper.DestroyUi(arg.Player(), Layer + ".eventcreator.div");
            UI_UpdateCreator(arg.Player());
        }

        [ConsoleCommand("em_drawcreators")]
        private void cmdDrawCreators(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            
            UI_DrawCreators(arg.Player());
        }

        private void UI_UpdateCreator(BasePlayer player)
        {
            var container = new CuiElementContainer();
            
            if (!_creatingEvents.TryGetValue(player.userID, out var ev))
                return;
            
            
            container.Add(new CuiButton
            {
                Button = { Color = "0.8078431 0.7803922 0.7411765 0.6588235", Command = "em_drawcreators"},
                Text =
                {
                    Text = ev.Author?.Name ?? "Choose event creator...", Font = "robotocondensed-bold.ttf", FontSize = 21,
                    Align = TextAnchor.MiddleCenter, Color = ev.Author?.Color ?? "0.1 0.1 0.1 0.95"//
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-62.243 95.069",
                    OffsetMax = "172.248 130.901"
                }
            }, Layer, Layer + ".eventcreator");
            CuiHelper.DestroyUi(player, Layer + ".eventcreator");
            CuiHelper.AddUi(player, container);
            
        }

        private void UI_DrawCreators(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.8078431 0.7803922 0.7411765 0" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-62.242 58.184",
                    OffsetMax = "172.248 94.016"
                }
            }, Layer, Layer + ".eventcreator.div");
            float minx = -117.245f;
            float maxx = 117.245f;
            float miny = -17.91578f;
            float maxy = 5.91522f;
            int i = 0;
            foreach (var x in EventList.Where(x => x.Value.Count < 8).Select(x => x.Key))
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.8078431 0.7803922 0.7411765 0.6588235", Command = $"em_setauthor {x.Name}" },
                    Text =
                    {
                        Text = x.Name, Font = "robotocondensed-regular.ttf", FontSize = 14,
                        Align = TextAnchor.MiddleCenter, Color = x.Color
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}",
                        OffsetMax = $"{maxx} {maxy}"
                    }
                }, Layer + ".eventcreator.div", $"creator.{i}");

                i++;
                miny -= 25.04f;
                maxy -= 25.04f;
            }

            CuiHelper.DestroyUi(player, Layer + ".eventcreator.div");
            CuiHelper.AddUi(player, container);
        }
        
        private void UI_DrawCreateNewEvent(BasePlayer player)
        {
            _creatingEvents.Remove(player.userID);
            _creatingEvents.Add(player.userID, new NewEventData());
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0.317 0.317 0.317 0.7490196" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-353.394 -207.674",
                    OffsetMax = "353.394 207.674"
                }
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Name = Layer + ".label",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "NEW EVENT", Font = "robotocondensed-bold.ttf", FontSize = 23,
                        Align = TextAnchor.MiddleLeft, Color = "0.6470588 0.6235294 0.6 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-344 167.364",
                        OffsetMax = "-188.991 200.636"
                    }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.8078431 0.7803922 0.7411765 0.6588235" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-344 95.069", OffsetMax = "-93.687 130.9"
                }
            }, Layer, Layer + ".name.bg");

            container.Add(new CuiElement
            {
                Name = Layer + ".name.bg" + ".name.input",
                Parent = Layer + ".name.bg",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Color = "1 1 1 1", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        CharsLimit = 0, IsPassword = false, Text = "Enter event name...", Command = "em_setname "
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-125.155 -17.916",
                        OffsetMax = "125.155 17.916"
                    }
                }
            });

            
            
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.8078431 0.7803922 0.7411765 0.6588235" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-344 44.584",
                    OffsetMax = "-93.687 80.416"
                }
            }, Layer, Layer + ".command.bg");

            container.Add(new CuiElement
            {
                Name = Layer + ".command.bg" + ".input",
                Parent = Layer + ".command.bg",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Color = "1 1 1 1", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        CharsLimit = 0, IsPassword = false, Text = "Enter command...", Command = "em_setcommand "
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-125.155 -17.916",
                        OffsetMax = "125.155 17.916"
                    }
                }
            });

            

            

            container.Add(new CuiButton
            {
                Button = { Color = "0.3882353 0.3960785 0.3098039 1", Command = "em_createconfirm"},
                Text =
                {
                    Text = "CREATE", Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter,
                    Color = "0.8078431 0.8431373 0.5490196 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "237.359 -198.4",
                    OffsetMax = "346.2 -165.34"
                }
            }, Layer, Layer + ".create.btn");

            container.Add(new CuiButton
            {
                Button = { Color = "0.4078432 0.3372549 0.3019608 1", Command = "em_cancelcreate" },
                Text =
                {
                    Text = "CANCEL", Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter,
                    Color = "0.8156863 0.5568628 0.4745098 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "121.179 -198.4",
                    OffsetMax = "230.021 -165.34"
                }
            }, Layer, Layer + ".delete.btn");
            
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            
            UI_UpdateCreator(player);
        }

        [ConsoleCommand("em.closecreatingevent")]
        private void cmdCloseCreatingEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;

            
            CuiHelper.DestroyUi(arg.Player(), Layer);
            _creatingEvents.Remove(arg.Player().userID);
        }
        private void ShowMainUI(BasePlayer player, int page = 1, string title = "EVENT MANAGER", bool showRandom = true, string command = SELECT_CCMD)
        {
            var parsedCommand = string.Join(" ", command.Split(':'));
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = SELECT_CUI_NAME,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.7254902",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiNeedsCursorComponent(),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-485.471 -287.535",
                        OffsetMax = "485.463 287.535"
                    }
               }
            });

            container.Add(new CuiElement
            {
                Name = SELECT_CUI_NAME + "_Bar",
                Parent = SELECT_CUI_NAME,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "1 0.5",
                        OffsetMin = "15 226",
                        OffsetMax = "-15 230"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECT_CUI_NAME + "_Title",
                Parent = SELECT_CUI_NAME,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = title,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 24,
                        Align = TextAnchor.MiddleLeft,
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "1 0.5",
                        OffsetMin = "15 237.2",
                        OffsetMax = "15 284.54"
                    }
                }
            });

            if (showRandom)
            {
                container.Add(new CuiElement
                {
                    Name = SELECT_CUI_NAME + "_RandomStart",
                    Parent = SELECT_CUI_NAME,
                    Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.1 0.1 0.1 0.95",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        FadeIn = 0.1f,
                        Command = $"{SELECT_CCMD} RANDOM START",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",
                        OffsetMin = "-75 -50",
                        OffsetMax = "75 -10"
                    }
                }
                });

                container.Add(new CuiElement
                {
                    Name = SELECT_CUI_NAME + "_RandomStart_Text",
                    Parent = SELECT_CUI_NAME + "_RandomStart",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = "Random Start",
                        FontSize = 20,
                        FadeIn = 0.2f,
                        Font = "robotocondensed-regular.ttf",
                        Align = TextAnchor.MiddleCenter
                    }
                }
                });

            }

            container.Add(new CuiElement
            {
                Name = SELECT_CUI_NAME + "_CloseBtn",
                Parent = SELECT_CUI_NAME,
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.5849056 0.2615521 0.2615521 1",
                        Close = SELECT_CUI_NAME
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = "-40 -40",
                        OffsetMax = "-15 -15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECT_CUI_NAME + "_CloseBtn_Text",
                Parent = SELECT_CUI_NAME + "_CloseBtn",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "X",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
            });

            var authors = EventList.Keys.ToList();

            int startIndex = (page - 1) * 6;
            int displayCount = Mathf.Min(EventList.Keys.Count - (page - 1) * 6, 6);

            for (var i = startIndex; i < startIndex + displayCount; i++)
            {
                var name = SELECT_CUI_NAME + "_author_" + i;
                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = SELECT_CUI_NAME,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = authors[i].Name,
                            FadeIn = (i % 6 + 1) * 0.2f,
                            FontSize = 24,
                            Font = "robotocondensed-regular.ttf",
                            Color = authors[i].Color,
                            Align = TextAnchor.UpperCenter
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0.85",
                            OffsetMin = $"{i % 6 * 160} 0",
                            OffsetMax = $"{(i % 6 + 1) * 160} 0",
                        }
                    }
                });

                for (var j = 0; j < EventList[authors[i]].Count; j++)
                {
                    var eventName = name + "_event" + j;
                    var ev = EventList[authors[i]][j];

                    container.Add(new CuiElement
                    {
                        Name = eventName,
                        Parent = name,
                        Components =
                        {
                            new CuiButtonComponent
                            {
                                Color = ev.Color,
                                Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                                FadeIn = (i % 6 + 1) * (j + 1) * 0.1f,
                                Command = $"{parsedCommand} {ev.displayName}",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.08 1",
                                AnchorMax = "0.93 1",
                                OffsetMin = $"0 -{((j + 1) * 55) + 20}",
                                OffsetMax = $"0 -{(j * 55) + 35}"
                            }
                        }
                    });

                    container.Add(new CuiButton()
                    {
                        Button = { Color = "0.46 0.18 0.04 1.00", Command = $"em.delete {i} {j} {page}", Sprite = "assets/icons/close.png"},
                        Text = { Text = ""},
                        RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-5 -5", OffsetMax = "5 5"}
                    }, eventName, eventName + ".delete");

                    container.Add(new CuiElement
                    {
                        Name = eventName + "_Text",
                        Parent = eventName,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = ev.displayName,
                                FontSize = 16,
                                FadeIn = (i % 6 + 1) * (j + 1) * 0.1f,
                                Font = "robotocondensed-regular.ttf",
                                Align = TextAnchor.MiddleCenter
                            }
                        }
                    });
                }

                if (i != EventList.Keys.Count - 1 && i % 6 != 5)
                {
                    container.Add(new CuiElement
                    {
                        Name = name + "_sep",
                        Parent = name,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 1",
                                FadeIn = (i % 6 + 1) * 0.3f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.99 0.05",
                                AnchorMax = "1 0.98",
                            }
                        }
                    });
                }
            }

            if (authors.Count > page * 6)
            {
                container.Add(new CuiElement
                {
                    Name = SELECT_CUI_NAME + "_NextPageBtn",
                    Parent = SELECT_CUI_NAME,
                    Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{SHOW_PAGE_CCMD} {page + 1} {(showRandom ? '1' : '0')} {command.Trim()} {title}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-40 15",
                        OffsetMax = "-15 40"
                    }
                }
                });

                container.Add(new CuiElement
                {
                    Name = SELECT_CUI_NAME + "_NextPageBtn_Text",
                    Parent = SELECT_CUI_NAME + "_NextPageBtn",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = ">",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
                });
            }

            if (page > 1)
            {
                container.Add(new CuiElement
                {
                    Name = SELECT_CUI_NAME + "_PrevPageBtn",
                    Parent = SELECT_CUI_NAME,
                    Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{SHOW_PAGE_CCMD} {page - 1} {(showRandom ? '1' : '0')} {command} {title}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = "15 15",
                        OffsetMax = "40 40"
                    }
                }
                });

                container.Add(new CuiElement
                {
                    Name = SELECT_CUI_NAME + "_PrevPageBtn_Text",
                    Parent = SELECT_CUI_NAME + "_PrevPageBtn",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = "<",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
                });

            }


            CuiHelper.DestroyUi(player, SELECT_CUI_NAME);
            CuiHelper.AddUi(player, container);
        }

        private void ShowSelectUI(BasePlayer player, EventSettings settings, int page = 1)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = SELECTED_CUI_NAME,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.3254902",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiNeedsCursorComponent(),
                    new CuiNeedsKeyboardComponent(),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECTED_CUI_NAME + "_Panel",
                Parent = SELECTED_CUI_NAME,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.9254902",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-350 -300",
                        OffsetMax = "350 300"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "EventSelected_Title",
                Parent = SELECTED_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "1 1",
                        OffsetMin = "15 -66",
                        OffsetMax = "15 0"
                    },
                    new CuiTextComponent
                    {
                        Text = settings.displayName.ToUpper(),
                        Color = "0.9 0.9 0.9 1",
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 25,
                    }
                }
            });


            container.Add(new CuiElement()
            {
                Name = SELECTED_CUI_NAME + "_Panel_CloseBtn",
                Parent = SELECTED_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.5849056 0.2615521 0.2615521 1",
                        Close = SELECTED_CUI_NAME,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = "-40 -40",
                        OffsetMax = "-15 -15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECTED_CUI_NAME + "_Panel_CloseBtn_Text",
                Parent = SELECTED_CUI_NAME + "_Panel_CloseBtn",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "X",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECTED_CUI_NAME + "_Panel_AddBtn",
                Parent = SELECTED_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = NEW_ENTRY_CCMD + " " + settings.displayName
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-100 15",
                        OffsetMax = "-15 45",
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECTED_CUI_NAME + "_Panel_AddBtn_Text",
                Parent = SELECTED_CUI_NAME + "_Panel_AddBtn",
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = "1 1 1 1",
                        FontSize = 20,
                        Text = "+ ADD",
                        Font = "robotocondensed-bold.ttf",
                        Align = TextAnchor.MiddleCenter,
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = SELECTED_CUI_NAME + "_Panel_Line",
                Parent = SELECTED_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "1 1",
                        OffsetMin = "0 -66",
                        OffsetMax = "0 -62"
                    }
                }
            });

            var entries = new List<EventSettingsEntry>();
            foreach (var entry in config.Entries.Values)
            {
                if (entry.Name == settings.displayName)
                {
                    entries.Add(entry);
                }
            }

            int startIndex = (page - 1) * 7;
            int displayCount = Mathf.Min(entries.Count - (page - 1) * 7, 7);

            for (var i = startIndex; i < startIndex + displayCount; i++)
            {
                var text = "";
                if (entries[i].StaticTime)
                {
                    text += entries[i].Hour + ":" + entries[i].Minute + " - ";

                    var days = new List<string>();
                    for (var j = 0; j < 7; j++)
                    {
                        if (entries[i].DaysActive[j])
                        {
                            days.Add(DayList[j]);
                        }
                    }
                    if (days.Count != 7)
                    {
                        text += days.ToSentence();
                    }
                    else
                    {
                        text += "Everyday";
                    }
                }
                else
                {
                    text += "Every ";
                    if (entries[i].Hour > 0)
                    {
                        text += entries[i].Hour + " hour";
                        if (entries[i].Hour > 1)
                        {
                            text += "s";
                        }
                    }

                    if (entries[i].Hour > 0 && entries[i].Minute > 0)
                    {
                        text += " and ";
                    }

                    if (entries[i].Minute > 0)
                    {
                        text += entries[i].Minute + " minute";
                        if (entries[i].Minute > 1)
                        {
                            text += "s";
                        }
                    }
                    text += " - ";
                    var days = new List<string>();
                    for (var j = 0; j < 7; j++)
                    {
                        if (entries[i].DaysActive[j])
                        {
                            days.Add(DayList[j]);
                        }
                    }

                    if (days.Count != 7)
                    {
                        text += days.ToSentence();
                    }
                    else
                    {
                        text += "Everyday";
                    }
                }

                if (entries[i].MinPlayers != 0)
                {
                    text += ", if the player count is above " + entries[i].MinPlayers.ToString();
                }


                var entryName = SELECTED_CUI_NAME + "_Panel_Entry" + i;
                container.Add(new CuiElement
                {
                    Name = entryName,
                    Parent = SELECTED_CUI_NAME + "_Panel",
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = i % 2 == 0 ? "0.3 0.3 0.3 0.98" : "0.2 0.2 0.2 0.98",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Command = EDIT_ENTRY_CCMD + " " + entries[i].GUID
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMax = $"-5 -{((i % 7 + 1) * 60) + 16}",
                            OffsetMin = $"5 -{((i % 7 + 2) * 60) + 8}",
                        }
                    }
                });

                container.Add(new CuiElement()
                {
                    Name = entryName + "_text",
                    Parent = entryName,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = text,
                            Color = "1 1 1 1",
                            FontSize = 18,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "10 0",
                            OffsetMax = "-10 0"
                        }
                    }
                });
            }


            if (entries.Count > page * 7)
            {
                container.Add(new CuiElement
                {
                    Name = SELECTED_CUI_NAME + "_Panel" + "_NextPageBtn",
                    Parent = SELECTED_CUI_NAME + "_Panel",
                    Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{SELECT_CCMD} {page + 1}|{settings.displayName}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-40 65",
                        OffsetMax = "-15 90"
                    }
                }
                });

                container.Add(new CuiElement
                {
                    Name = SELECTED_CUI_NAME + "_Panel" + "_NextPageBtn_Text",
                    Parent = SELECTED_CUI_NAME + "_Panel" + "_NextPageBtn",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = ">",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
                });
            }

            if (page > 1)
            {
                container.Add(new CuiElement
                {
                    Name = SELECTED_CUI_NAME + "_Panel" + "_PrevPageBtn",
                    Parent = SELECTED_CUI_NAME + "_Panel",
                    Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{SELECT_CCMD} {page - 1}|{settings.displayName}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = "15 65",
                        OffsetMax = "40 90"
                    }
                }
                });

                container.Add(new CuiElement
                {
                    Name = SELECTED_CUI_NAME + "_Panel" + "_PrevPageBtn_Text",
                    Parent = SELECTED_CUI_NAME + "_Panel" + "_PrevPageBtn",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = "<",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
                });

            }

            CuiHelper.DestroyUi(player, SELECTED_CUI_NAME);
            CuiHelper.AddUi(player, container);
        }

        void EditEntryUI(BasePlayer player, EventSettingsEntry entry, int page = 1)
        {
            var elements = new CuiElementContainer();

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.8"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel",
                Parent = EDITENTRY_CUI_NAME,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.1 0.1 0.1 0.9",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = entry.Name != "RANDOM START" ? "-300 -200" : "-300 -300",
                        OffsetMax = entry.Name != "RANDOM START" ? "300 200" : "300 300",
                    }
                }
            });


            elements.Add(new CuiElement()
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_CloseBtn",
                Parent = EDITENTRY_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.5849056 0.2615521 0.2615521 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = DELETE_ENTRY_CCMD + " " + entry.GUID
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-270 15",
                        OffsetMax = "-150 50"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_CloseBtn_Text",
                Parent = EDITENTRY_CUI_NAME + "_Panel_CloseBtn",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "DELETE",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 17,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }
            });

            elements.Add(new CuiElement()
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_SaveBtn",
                Parent = EDITENTRY_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = SAVE_ENTRY_CCMD + " " + entry.GUID,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-135 15",
                        OffsetMax = "-15 50"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_SaveBtn_Text",
                Parent = EDITENTRY_CUI_NAME + "_Panel_SaveBtn",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "SAVE",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 17,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_Title",
                Parent = EDITENTRY_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "1 1",
                        OffsetMin = "30 -66",
                        OffsetMax = "30 0"
                    },
                    new CuiTextComponent
                    {
                        Text = "EDIT START TIME - " + entry.Name,
                        Color = "0.9 0.9 0.9 1",
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 25,
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_StaticTimeChkBx",
                Parent = EDITENTRY_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "1 1 1 1",
                        Command = SET_ENTRY_CCMD + " " + entry.GUID + " static"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "36 -96",
                        OffsetMax = "58 -74",
                    }
                }
            });

            if (entry.StaticTime)
            {
                elements.Add(new CuiElement
                {
                    Name = EDITENTRY_CUI_NAME + "_Panel_StaticTimeChkBxTicked",
                    Parent = EDITENTRY_CUI_NAME + "_Panel_StaticTimeChkBx",
                    Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                    },
                    new CuiRectTransformComponent
                    {
                        OffsetMin = "3 3",
                        OffsetMax = "-3 -3",
                    }
                }
                });

            }

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_StaticTimeTitle",
                Parent = EDITENTRY_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "STATIC TIME?",
                        FontSize = 20,
                        Color = "0.9 0.9 0.9 1",
                        Align = TextAnchor.UpperCenter
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "52 -105",
                        OffsetMax = "220 -75"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_Time",
                Parent = EDITENTRY_CUI_NAME + "_Panel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.1 0.1 0.1 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "30 -235",
                        OffsetMax = "564 -115",
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_Time_Title",
                Parent = EDITENTRY_CUI_NAME + "_Panel_Time",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = entry.StaticTime ? "TIME" : "EVERY",
                        Align = TextAnchor.UpperLeft,
                        Color = "0.9 0.9 0.9 1",
                        FontSize = 22
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_TimeHour",
                Parent = EDITENTRY_CUI_NAME + "_Panel_Time",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.8 0.8 0.8 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-264 -25",
                        OffsetMax = "-214 25"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_TimeHourField",
                Parent = EDITENTRY_CUI_NAME + "_Panel_TimeHour",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        NeedsKeyboard = true,
                        Autofocus = true,
                        LineType = UnityEngine.UI.InputField.LineType.SingleLine,
                        CharsLimit = 2,
                        IsPassword = false,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 30,
                        Color = "0 0 0 1",
                        Text = entry.Hour.ToString(),
                        Command = SET_ENTRY_CCMD + " " + entry.GUID + " hour "
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_TimeColon",
                Parent = EDITENTRY_CUI_NAME + "_Panel_Time",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = ":",
                        FontSize = 40,
                        Align = TextAnchor.MiddleCenter,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-213 -25",
                        OffsetMax = "-195 25"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_TimeMinute",
                Parent = EDITENTRY_CUI_NAME + "_Panel_Time",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.8 0.8 0.8 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-195 -25",
                        OffsetMax = "-145 25"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_TimeMinuteField",
                Parent = EDITENTRY_CUI_NAME + "_Panel_TimeMinute",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        NeedsKeyboard = true,
                        Autofocus = true,
                        LineType = UnityEngine.UI.InputField.LineType.SingleLine,
                        CharsLimit = 2,
                        IsPassword = false,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 30,
                        Color = "0 0 0 1",
                        Text = entry.Minute.ToString(),
                        Command = SET_ENTRY_CCMD + " " + entry.GUID + " minute "
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_MinPlayers_Title",
                Parent = EDITENTRY_CUI_NAME + "_Panel_Time",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "MIN PLAYER COUNT",
                        Align = TextAnchor.UpperLeft,
                        Color = "0.9 0.9 0.9 1",
                        FontSize = 22
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.6 0.7",
                        AnchorMax = "1 1",
                    }
                }
            });


            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_MinPlayers",
                Parent = EDITENTRY_CUI_NAME + "_Panel_Time",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.8 0.8 0.8 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "54 -25",
                        OffsetMax = "154 25"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = EDITENTRY_CUI_NAME + "_Panel_MinPlayersField",
                Parent = EDITENTRY_CUI_NAME + "_Panel_MinPlayers",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        NeedsKeyboard = true,
                        Autofocus = true,
                        LineType = UnityEngine.UI.InputField.LineType.SingleLine,
                        CharsLimit = 4,
                        IsPassword = false,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 30,
                        Color = "0 0 0 1",
                        Text = entry.MinPlayers.ToString(),
                        Command = SET_ENTRY_CCMD + " " + entry.GUID + " minplayers "
                    }
                }
            });

            for (var i = 0; i < DayList.Count; i++)
            {
                var boxName = EDITENTRY_CUI_NAME + "_PanelChkBxDay" + i;

                elements.Add(new CuiElement
                {
                    Name = boxName,
                    Parent = EDITENTRY_CUI_NAME + "_Panel",
                    Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "1 1 1 1",
                        Command = SET_ENTRY_CCMD + " " + entry.GUID + " day "  + i
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"{(i % 4 * 140) + 35} {(i >= 4 ? -265 : -315)}",
                        OffsetMax = $"{(((i % 4) + 1) * 140) - 80} {(i >= 4 ? -240 : -290)}",
                    }
                }
                });

                if (entry.DaysActive[i])
                {
                    elements.Add(new CuiElement
                    {
                        Name = boxName + "Ticked",
                        Parent = boxName,
                        Components =
                        {
                        new CuiImageComponent
                        {
                            Color = "0.35 0.58 0.04 1",
                        },
                        new CuiRectTransformComponent
                        {
                            OffsetMin = "3 3",
                            OffsetMax = "-3 -3",
                        }
                        }
                    });
                }

                elements.Add(new CuiElement
                {
                    Name = boxName + "_Text",
                    Parent = EDITENTRY_CUI_NAME + "_Panel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = DayList[i].ToUpper(),
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 17,
                            Color = "0.9 0.9 0.9 1",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"{(i % 4 * 140) + 70} {(i >= 4 ? -265 : -315)}",
                            OffsetMax = $"{((i % 4) + 1) * 155} {(i >= 4 ? -240 : -290)}",
                        }
                    }
                });
            }

            if (entry.Name == "RANDOM START")
            {
                elements.Add(new CuiElement
                {
                    Name = EDITENTRY_CUI_NAME + "_Panel_AddEventsBtn",
                    Parent = EDITENTRY_CUI_NAME + "_Panel",
                    Components =
                    {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{SHOW_PAGE_CCMD} 1 0 {SET_ENTRY_CCMD + ":" + entry.GUID + ":add"} SELECT EVENT FROM LIST",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "35 -380",
                        OffsetMax = "200 -340"
                    }
                    }
                });

                elements.Add(new CuiElement
                {
                    Name = EDITENTRY_CUI_NAME + "_Panel_AddEventsBtn_Text",
                    Parent = EDITENTRY_CUI_NAME + "_Panel_AddEventsBtn",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = "ADD EVENT",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }
                });


                if (entry.RandomStartEvents.Count > page * 12)
                {
                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_NextPageBtn",
                        Parent = EDITENTRY_CUI_NAME + "_Panel",
                        Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{EDIT_ENTRY_CCMD} {entry.GUID} {page + 1}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-40 60",
                        OffsetMax = "-15 85"
                    }
                }
                    });

                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_NextPageBtn_Text",
                        Parent = EDITENTRY_CUI_NAME + "_Panel_NextPageBtn",
                        Components =
                {
                    new CuiTextComponent
                    {
                        Text = ">",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
                    });
                }

                if (page > 1)
                {
                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_PrevPageBtn",
                        Parent = EDITENTRY_CUI_NAME + "_Panel",
                        Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0.35 0.58 0.04 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        Command = $"{EDIT_ENTRY_CCMD} {entry.GUID} {page - 1}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = "15 60",
                        OffsetMax = "40 85"
                    }
                }
                    });

                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_PrevPageBtn_Text",
                        Parent = EDITENTRY_CUI_NAME + "_Panel_PrevPageBtn",
                        Components =
                {
                    new CuiTextComponent
                    {
                        Text = "<",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 21,
                        Align = TextAnchor.MiddleCenter,
                    }
                }
                    });

                }
                int startIndex = (page - 1) * 12;
                int displayCount = Mathf.Min(entry.RandomStartEvents.Count - (page - 1) * 12, 12);


                for (int i = startIndex; i < startIndex + displayCount; i++)
                {
                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_RandomStartEvText" + i,
                        Parent = EDITENTRY_CUI_NAME + "_Panel",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Color = "0.7 0.7 0.7 1",
                                Align = TextAnchor.MiddleLeft,
                                FadeIn = 0.1f * (i % 12 + 1),
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 19,
                                Text = entry.RandomStartEvents[i],
                                VerticalOverflow = VerticalWrapMode.Truncate
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"{i % 12 / 4 * 190 + 20} {-405 - (i % 4 + 1) * 25 - (i % 4 * 5)}",
                                OffsetMax = $"{i % 12 / 4 * 190 + 160 + 20} {-380 - i % 4 * 25 - (i % 4 * 5)}"
                            }
                        }
                    });

                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_RandomStartEvDelBtn" + i,
                        Parent = EDITENTRY_CUI_NAME + "_Panel",
                        Components =
                        {
                            new CuiButtonComponent
                            {
                                Color = "0.5849056 0.2615521 0.2615521 1",
                                Command = SET_ENTRY_CCMD + " " + entry.GUID + " del " + i
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"{i % 12 / 4 * 190 + 160 + 20} {-395 - (i % 4 + 1) * 20 - (i % 4 * 10)}",
                                OffsetMax = $"{i % 12 / 4 * 190 + 160 + 20 + 20} {-395 - i % 4 * 20 - (i % 4 * 10)}"
                            }
                        }
                    });
                    elements.Add(new CuiElement
                    {
                        Name = EDITENTRY_CUI_NAME + "_Panel_RandomStartEvDelBtn" + i + "_Text",
                        Parent = EDITENTRY_CUI_NAME + "_Panel_RandomStartEvDelBtn" + i,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "X",
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 17,
                                Align = TextAnchor.MiddleCenter,
                            }
                        }
                    });
                }
            }

            CuiHelper.DestroyUi(player, EDITENTRY_CUI_NAME);
            CuiHelper.AddUi(player, elements);
        }
        #endregion

        #region Commands

        [ConsoleCommand("em.delete")]
        private void cmdDeleteEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || arg.Args.IsNullOrEmpty())
                return;
            
            if (arg.Args.Length < 2)
                return;
            // EventList[authors[i]][j]
            if (!int.TryParse(arg.Args[0], out var authorIndex))
                return;
            if (!int.TryParse(arg.Args[1], out var eventIndex))
                return;
            if (!int.TryParse(arg.Args[2], out var page))
                return;

            EventList.ElementAtOrDefault(authorIndex).Value.RemoveAt(eventIndex);
            ShowMainUI(arg.Player(), page);
            SaveData();
        }
        [ConsoleCommand("newevent")]
        private void ccmdNewEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                cmdNewEvent(arg.Player());
        }

        [ChatCommand("newevent")]
        private void cmdNewEvent(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }
            
            UI_DrawCreateNewEvent(player);
        }
        
        [ChatCommand("em")]
        private void cmdChatem(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }
            ShowMainUI(player);
        }


        [ConsoleCommand(SHOW_PAGE_CCMD)]
        void showPageCCmd(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }

            var title = "EVENT MANAGER";
            var showRandom = true;
            var command = SELECT_CCMD;

            if (arg.Args.Length > 1)
                showRandom = arg.Args[1] == "1" ? true : false;

            if (arg.Args.Length > 2)
                command = arg.Args[2];

            if (arg.Args.Length > 3)
                title = string.Join(" ", arg.Args.Skip(3).ToArray());

            ShowMainUI(player, Convert.ToInt32(arg.Args[0]), title, showRandom, command);
        }

        [ConsoleCommand(EDIT_ENTRY_CCMD)]
        void ccmdEMEdit(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }

            var page = 1;
            if (arg.Args.Length > 1)
                page = Convert.ToInt32(arg.Args[1]);

            EditEntryUI(player, config.Entries[arg.Args[0]], page);
        }

        [ConsoleCommand(NEW_ENTRY_CCMD)]
        void ccmdEMNew(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            // if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            // {
            //     player.ChatMessage("You don't have permission for use this command.");
            //     return;
            // }

            var guid = EventSettingsEntry.NewEntry(string.Join(" ", arg.Args));
            EditEntryUI(player, config.Entries[guid]);
        }

        [ConsoleCommand(DELETE_ENTRY_CCMD)]
        void ccmdDeleteEntry(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }

            var ev = GetEventByName(config.Entries[arg.Args[0]].Name);

            config.Entries.Remove(arg.Args[0]);
            CuiHelper.DestroyUi(player, EDITENTRY_CUI_NAME);
            SaveConfig();
            ShowSelectUI(player, ev);
        }

        [ConsoleCommand(SAVE_ENTRY_CCMD)]
        void ccmdSaveEntry(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }

            CuiHelper.DestroyUi(player, EDITENTRY_CUI_NAME);
            SaveConfig();

            ShowSelectUI(player, GetEventByName(config.Entries[arg.Args[0]].Name));
        }

        [ConsoleCommand(SET_ENTRY_CCMD)]
        void ccmdEditEntry(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }

            try
            {
                var entry = config.Entries[arg.Args[0]];

                switch (arg.Args[1].ToLower())
                {
                    case "static":
                        entry.StaticTime = !entry.StaticTime;
                        break;
                    case "day":
                        var day = Convert.ToInt32(arg.Args[2]);
                        entry.DaysActive[day] = !entry.DaysActive[day];
                        break;
                    case "hour":
                        var hour = Mathf.Clamp(Convert.ToInt32(arg.Args[2]), 0, 60);
                        entry.Hour = hour;
                        return;
                    case "minute":
                        var minute = Mathf.Clamp(Convert.ToInt32(arg.Args[2]), 0, 60);
                        entry.Minute = minute;
                        return;
                    case "minplayers":
                        var minplayers = Mathf.Clamp(Convert.ToInt32(arg.Args[2]), 0, 10000);
                        entry.MinPlayers = minplayers;
                        return;
                    case "del":
                        var delIndex = Convert.ToInt32(arg.Args[2]);
                        entry.RandomStartEvents.RemoveAt(delIndex);
                        break;
                    case "add":
                        var addName = string.Join(" ", arg.Args.Skip(2));
                        entry.RandomStartEvents.Add(addName);
                        CuiHelper.DestroyUi(player, SELECT_CUI_NAME);
                        break;
                }

                EditEntryUI(player, entry);
            }
            catch (Exception)
            {
                return;
            }
        }

        [ConsoleCommand(SELECT_CCMD)]
        private void cmdConsoleUI_EM_SELECT(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
            {
                player.ChatMessage("You don't have permission for use this command.");
                return;
            }

            var eventName = string.Join(" ", arg.Args);
            var page = 1;

            if(eventName.Contains("|"))
            {
                page = Convert.ToInt32(eventName.Substring(0, eventName.IndexOf('|')));
                eventName = eventName.Substring(eventName.IndexOf('|') + 1);
            }

            ShowSelectUI(player, GetEventByName(eventName), page);
        }
        #endregion

        private void CallStartedHook(string eventname)
        {
            Interface.Oxide.CallHook("OnEventStarted", eventname);
        }
    }
}