using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TeleportGUI", "A0001", "0.0.0")]

    class TeleportGUI : RustPlugin
    {
        #region Fields

        private const string permUse = "teleportgui.use";
        private const string permCancel = "teleportgui.tpcancel";
        private const string permBack = "teleportgui.tpback";
        private const string permHere = "teleportgui.tphere";
        private const string permSleepers = "teleportgui.sleepers";

        private static TeleportGUI Instance;

        Dictionary<BasePlayer, bool> GUIOpen = new Dictionary<BasePlayer, bool>();
        Dictionary<BasePlayer, Vector3> LastTeleport = new Dictionary<BasePlayer, Vector3>();
        List<GameObject> GameObjects = new List<GameObject>();

        bool DebuggingMode = false;

        [PluginReference] Plugin Economics, ServerRewards, ZoneManager;

        #endregion

        #region Classes 

        class GUIManager
        {
            public static Dictionary<BasePlayer, GUIManager> Players = new Dictionary<BasePlayer, GUIManager>();

            public int Page = 1;
            public bool TPHere = false;
            public bool Sleepers = false;

            public static GUIManager Get(BasePlayer player)
            {
                if (Players.ContainsKey(player)) return Players[player];
                Players.Add(player, new GUIManager());
                return Players[player];
            }
        }

        class TeleportRequest : MonoBehaviour
        {
            GameObject GameObject;
            int Time;
            public BasePlayer From;
            public BasePlayer To;

            public static void Create(BasePlayer From, BasePlayer To, int TimeoutTime)
            {
                TeleportRequest tr = new TeleportRequest();
                tr.GameObject = new GameObject();
                tr = tr.GameObject.AddComponent<TeleportRequest>();
                tr.Time = TimeoutTime;
                tr.From = From;
                tr.To = To;
                Instance.GameObjects.Add(tr.GameObject);
            }

            void Start()
            {
                Instance.SendReply(From, Instance.GetMessage("RequestSent").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("RequestRecieved").Replace("{0}", From.displayName));
                PendingRequest pr = To.gameObject.AddComponent<PendingRequest>();
                pr.From = From;
                pr.TeleportRequest = this;
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void TimerTick()
            {
                if (Time == 0) RequestTimeOut();
                Time--;
            }

            public void RequestAccepted()
            {
                int timeUntilTeleport = Instance.GetLowest(Instance.GetConfig<Dictionary<string, object>>("TimeUntilTeleport"), From, Instance.GetConfig<int>("DefaultTimeUntilTeleport"));
                Instance.SendReply(From, Instance.GetMessage("RequestToAccepted").Replace("{0}", To.displayName).Replace("{1}", timeUntilTeleport.ToString()));
                Instance.SendReply(To, Instance.GetMessage("RequestFromAccepted").Replace("{0}", From.displayName).Replace("{1}", timeUntilTeleport.ToString()));

                Teleporter teleporter = From.gameObject.AddComponent<Teleporter>();
                teleporter.Create(From, To, timeUntilTeleport);
                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null) GameObject.Destroy(pr);
                CancelInvoke();
                GameObject.Destroy(this.gameObject);
            }

            public void RequestDeclined()
            {
                if (From != null)
                {
                    Instance.SendReply(From, Instance.GetMessage("RequestToDenied").Replace("{0}", To.displayName));
                    if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("UseEconomicsPlugin"))
                        Instance.RefundPlayerEconomics(From);
                    if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("UseServerRewardsPlugin"))
                        Instance.RefundServerRewards(From);
                }
                if (To != null)
                {
                    Instance.SendReply(To, Instance.GetMessage("RequestFromDenied").Replace("{0}", From.displayName));
                }
                PendingRequest pr = To?.gameObject?.GetComponent<PendingRequest>();
                if (pr != null) GameObject.Destroy(pr);
                GameObject.Destroy(this.gameObject);
            }

            public void RequestCancelled()
            {
                if (From != null)
                {
                    Instance.SendReply(From, Instance.GetMessage("TeleportRequestToCancelled").Replace("{0}", To.displayName));

                    if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("UseEconomicsPlugin"))
                        Instance.RefundPlayerEconomics(From);

                    if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("UseServerRewardsPlugin"))
                        Instance.RefundServerRewards(From);
                }

                if (To != null)
                {
                    Instance.SendReply(To, Instance.GetMessage("TeleportRequestFromCancelled").Replace("{0}", From.displayName));
                }

                PendingRequest pr = To?.gameObject?.GetComponent<PendingRequest>();
                if (pr != null) GameObject.Destroy(pr);

                GameObject.Destroy(this.gameObject);
            }

            void RequestTimeOut()
            {
                if (From != null)
                {
                    Instance.SendReply(From, Instance.GetMessage("RequestToTimedOut").Replace("{0}", To.displayName));
                    if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("UseEconomicsPlugin"))
                        Instance.RefundPlayerEconomics(From);
                    if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("UseServerRewardsPlugin"))
                        Instance.RefundServerRewards(From);
                }
                if (To != null)
                {
                    Instance.SendReply(To, Instance.GetMessage("RequestFromTimedOut").Replace("{0}", From.displayName));
                }
                PendingRequest pr = To?.gameObject?.GetComponent<PendingRequest>();
                if (pr != null) GameObject.Destroy(pr);
                GameObject.Destroy(this.gameObject);
            }

            void CancelRequest()
            {
                Instance.SendReply(From, Instance.GetMessage("BlockTPTakeDamage"));

                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("UseEconomicsPlugin"))
                    Instance.RefundPlayerEconomics(From);

                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("UseServerRewardsPlugin"))
                    Instance.RefundServerRewards(From);

                PendingRequest pr = To.gameObject.GetComponent<PendingRequest>();
                if (pr != null) GameObject.Destroy(pr);

                GameObject.Destroy(this.gameObject);
            }

            void OnDestroy()
            {
                CancelInvoke();
                Instance.GameObjects.Remove(GameObject);
            }
        }

        class PendingRequest : MonoBehaviour
        {
            public BasePlayer From;
            public TeleportRequest TeleportRequest;
        }

        class Teleporter : MonoBehaviour
        {
            GameObject GameObject;
            int TimeUntilTeleport;
            BasePlayer From;
            BasePlayer To;

            public void Create(BasePlayer from, BasePlayer to, int timeUntilTeleport)
            {
                this.GameObject = new GameObject();
                this.TimeUntilTeleport = timeUntilTeleport;
                this.From = from;
                this.To = to;
                Instance.GameObjects.Add(GameObject);
            }

            void Start() => InvokeRepeating("TimerTick", 0, 1.0f);

            void TimerTick()
            {
                if (TimeUntilTeleport == 0) Teleport();
                TimeUntilTeleport--;
            }

            void Teleport()
            {
                Vector3 currentPos = From.transform.position;
                Instance.RecordLastTP(From, currentPos);

                Instance.Teleport(From, To);

                Instance.SendReply(From, Instance.GetMessage("YouTeleportedTo").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("TeleportedToYou").Replace("{0}", From.displayName));

                int cooldown = Instance.GetLowest(Instance.GetConfig<Dictionary<string, object>>("Cooldowns"), From, Instance.GetConfig<int>("DefaultCooldown"));
                Instance.storedData.Cooldowns.Add(From.userID, cooldown);

                if (Instance.GetConfig<int>("DefaultDailyLimit") != -1)
                {
                    int usesRemaining = Instance.IncrementUses(From);
                    Instance.SendReply(From, Instance.GetMessage("TeleportsRemaining").Replace("{0}", usesRemaining.ToString()));
                }

                GameObject.Destroy(this.gameObject.GetComponent<Teleporter>());
            }

            public void CancelTeleport()
            {
                Instance.SendReply(From, Instance.GetMessage("TeleportToCancelled").Replace("{0}", To.displayName));
                Instance.SendReply(To, Instance.GetMessage("TeleportFromCancelled").Replace("{0}", From.displayName));
                
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("UseEconomicsPlugin"))
                    Instance.RefundPlayerEconomics(From);

                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("UseServerRewardsPlugin"))
                    Instance.RefundServerRewards(From);

                GameObject.Destroy(this.gameObject.GetComponent<Teleporter>());
            }

            void StartSleeping(BasePlayer player)
            {
                if (player.IsSleeping())
                    return;
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                if (!BasePlayer.sleepingPlayerList.Contains(player))
                    BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
            }

            void OnDestory()
            {
                CancelInvoke();
                Instance.GameObjects.Remove(GameObject);
            }
        }

        class CooldownManager : MonoBehaviour
        {
            GameObject GameObject;

            public static void Create()
            {
                CooldownManager cm = new CooldownManager();
                cm.GameObject = new GameObject();
                cm.GameObject.AddComponent<CooldownManager>();
                Instance.GameObjects.Add(cm.GameObject);
            }

            void Start()
            {
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void TimerTick()
            {
                if (Instance?.storedData?.Cooldowns == null) return;
                Dictionary<ulong, int> dict = new Dictionary<ulong, int>(Instance.storedData.Cooldowns);
                foreach (KeyValuePair<ulong, int> kvp in dict)
                {
                    Instance.storedData.Cooldowns[kvp.Key]--;
                    if (kvp.Value == 0)
                        Instance.storedData.Cooldowns.Remove(kvp.Key);
                }
            }

            void OnDestroy()
            {
                Instance?.SaveData();
                CancelInvoke();
                Instance.GameObjects.Remove(GameObject);
            }
        }

        class StoredData
        {
            public Dictionary<ulong, int> Cooldowns = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> UsesToday = new Dictionary<ulong, int>();
        }
        StoredData storedData;

        #endregion

        #region Oxide Hooks

        void Init()
        {
            //Debugging mode should be enabled?
            if (ConVar.Server.hostname == "PsychoTea's Testing Server")
            {
                DebuggingMode = true;
                Puts("Debugging mode enabled.");
            }

            //Register permissions
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permCancel, this);
            permission.RegisterPermission(permBack, this);
            permission.RegisterPermission(permHere, this);
            permission.RegisterPermission(permSleepers, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("Cooldowns").Keys)
                permission.RegisterPermission(perm, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "У вас нет разрешения на использование этой команды." },
                { "TeleportTitle", "Телепорт" },
                { "RequestSent", "Запрос телепорта отправлен к {0}." },
                { "RequestRecieved", "Запрос телепорта от {0}. Откройте графический интерфейс (/tp) чтобы принять телепорт." },
                { "RequestToTimedOut", "Истекло время ожидания ответа {0} на ваш запрос телепорта." },
                { "RequestFromTimedOut", "Истекло время ответа на запрос телепорт от {0}." },
                { "HasPendingRequest", "У {0} уже есть ожидающий запрос телепорта." },
                { "RequestFrom", "Запрос от {0}" },
                { "RequestToAccepted", "Ваш запрос на телепорт был принят {0}. Телепортация через {1} секунд." },
                { "RequestFromAccepted", "Запрос телепорта от {0} принят. Телепортация через {1} секунд." },
                { "RequestToDenied", "Ваш запрос на телепорт к {0} отклонён." },
                { "RequestFromDenied", "Запрос телепорта от {0} отменён." },
                { "YouTeleportedTo", "Ты телепортировался к {0}." },
                { "TeleportedToYou", "{0} телепортироваться к вам." },
                { "OnCooldown", "Ваш телепорт откатится через {0} секунд." },
                { "NoPendingRequests", "У вас нет запросов на телепорт." },
                { "SyntaxTPR", "Неверная команда! Пиши: /tpr {name}" },
                { "PlayerNotFound", "Игрок \"{0}\" не найден." },
                { "PlayerIDNotFound", "Игрок с таким ID {0} не найден." },
                { "MultiplePlayersFound", "Найдено несколько игроков с именем {0}." },
                { "CantTeleportToSelf", "Ты не можешь телепортироваться сам к себе!" },
                { "PlayerIsBuildBlocked", "Вы не можете использовать телепорт во время блокировки строительства!" },
                { "TargetIsBuildBlocked", "Человек, к которому вы пытаетесь телепортироваться, заблокирован." },
                { "LocationIsBuildBlocked", "Здание заблокировано в том месте, куда вы пытаетесь телепортироваться." },
                { "PlayerIsBleeding", "Вы не можете использовать телепорт во время кровотечения." },
                { "CantAffordEconomics", "Вы не можете себе этого позволить! Цена: ${0}" },
                { "EconomicsYouSpent", "Вы потратили ${0} на этот телепорт." },
                { "EconomicsRefunded", "Вам были возвращены ${0}." },
                { "CantAffordServerRewards", "Вы не можете себе этого позволить! Цена: {0}RP" },
                { "ServerRewardsYouSpent", "Вы потратили {0}RP на этот телепорт." },
                { "ServerRewardsRefunded", "Вам были возвращены {0}RP." },
                { "BlockTPCrafting", "Вы не можете использовать телепорт во время крафта." },
                { "MaxTeleportsReached", "На сегодня ваши телепорты закончились." },
                { "TeleportsRemaining", "{0} телепортов, осталось на сегодня." },
                { "TeleportRequestFromCancelled", "Запрос телепортации от {0} отменен." },
                { "TeleportRequestToCancelled", "Телепорт на {0} отменен." },
                { "TeleportRequestCancelled", "Запрос на телепорт отменен." },
                { "TeleportToCancelled", "Телепорт в {0} отменен." },
                { "TeleportFromCancelled", "Телепорт к {0} отменен." },
                { "NoBackLocation", "У вас нет предыдущего места для возврата." },
                { "TeleportedBack", "Телепортация обратно в ваше предыдущее местоположение." },
                { "SummonedToYou", "{0} был вызван к вам." },
                { "SummonedTo", "Вы были вызваны {0}." },
                { "SyntaxTPHere", "Неизвесная команда! Пиши: /tphere {name}" },
                { "TPPos-InvalidSyntax", "Неизвесная команда! Пиши: /tp {x} {y} {z}" },
                { "TPToPos", "Телепортация в {x}, {y}, {z}" },
                { "NothingToCancel", "У вас нет телепортов для отмены." },
                { "CantTeleportFromZone", "Вы не можете телепортироваться из этой зоны." },
                { "CantTeleportToZone", "Вы не можете телепортироваться в эту зону." },
                { "CantTPWhilstWounded", "Вы не можете телепортироваться, пока ранены." }
            }, this, "en");

            ReadData();

            foreach (string cmdAlias in GetConfig<List<object>>("TPCommandAliases"))
                cmd.AddChatCommand(cmdAlias, this, "tpCommand");

            timer.Once(TimeUntilMidnight(), () => ResetDailyUses());

            if (DebuggingMode)
            {
                foreach (var check in BasePlayer.activePlayerList)
                    ShowTeleportUI(check);
            }
        }

        void OnServerInitialized()
        {
            Instance = this;

            CooldownManager.Create();

            if (Economics == null && GetConfig<bool>("UseEconomicsPlugin"))
            {
                Debug.LogError("[TeleportGUI] Error! Economics is enabled in the config but is not installed! Please install Economics or disable 'UseEconomicsPlugin' in the config!");
            }

            if (ServerRewards == null && GetConfig<bool>("UseServerRewardsPlugin"))
            {
                Debug.LogError("[TeleportGUI] Error! ServerRewards is enabled in the config but is not installed! Please install ServerRewards or disable 'UseServerRewardsPlugin' in the config!");
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is BasePlayer)) return;
            BasePlayer player = entity as BasePlayer;
            if (player == null) return;

            if (!HasComponent<Teleporter>(player)) return;
            if (!GetConfig<bool>("CancelTeleportOnDamage")) return;

            var teleporter = player.gameObject.GetComponent<Teleporter>();
            teleporter.CancelTeleport();
        }

        void Unload()
        {
            foreach (GameObject go in GameObjects)
            {
                if (go == null) continue;
                if (HasComponent<Teleporter>(go) ||
                    HasComponent<PendingRequest>(go) ||
                    HasComponent<CooldownManager>(go))
                    GameObject.Destroy(go);
            }
            GameObjects.Clear();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CloseUI(player);

            SaveData();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            Config["PrefixEnabled"] = true;
            Config["PrefixText"] = "<color=orange>TP: </color>";
            Config["DefaultTimeUntilTeleport"] = 25;
            Config["TimeUntilTeleport"] = new Dictionary<string, int>()
            {
                { "teleportgui.vip", 20 },
                { "teleportgui.elite", 15 },
                { "teleportgui.god", 5 },
                { "teleportgui.none", 0 }
            };
            Config["TPCommandAliases"] = new List<string>() { "teleport" };
            Config["RequestTimeoutTime"] = 30;
            Config["DefaultCooldown"] = 180;
            Config["Cooldowns"] = new Dictionary<string, int>()
            {
                { "teleportgui.vip", 60 },
                { "teleportgui.elite", 30 },
                { "teleportgui.god", 15 },
                { "teleportgui.none", 0 }
            };
            Config["DefaultDailyLimit"] = 3;
            Config["DailyLimit"] = new Dictionary<string, int>()
            {
                { "teleportgui.vip", 5 },
                { "teleportgui.elite", 8 },
                { "teleportgui.god", 15 },
                { "teleportgui.none", 9999 }
            };
            Config["AdminTPSilent"] = false;
            Config["AdminTPEnabled"] = false;
            Config["AllowTeleportWhilstBleeding"] = false;
            Config["AllowTeleportToBuildBlockedPlayer"] = false;
            Config["AllowTeleportIntoBuildBlock"] = false;
            Config["AllowTeleportFromBuildBlock"] = false;
            Config["UseEconomicsPlugin"] = false;
            Config["EconomicsPrice"] = 100;
            Config["UseServerRewardsPlugin"] = false;
            Config["ServerRewardsPrice"] = 10;
            Config["BlockTPCrafting"] = true;
            Config["AllowSpecialCharacters"] = false;
            Config["CancelTeleportOnDamage"] = true;
            Config["CanTeleportIntoZone"] = true;
            Config["CanTeleportFromZone"] = true;
        }

        #endregion

        #region Commands

        [ChatCommand("tp")]
        void TPCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (player.IsAdmin && args.Length > 0)
            {
                if (args.Length < 3)
                {
                    SendReply(player, GetMessage("TPPos-InvalidSyntax"));
                    return;
                }

                float x, y, z;
                if (!float.TryParse(args[0], out x) || !float.TryParse(args[1], out y) || !float.TryParse(args[2], out z))
                {
                    SendReply(player, GetMessage("TPPos-InvalidSyntax"));
                    return;
                }

                Teleport(player, new Vector3(x, y, z));
                SendReply(player, GetMessage("TPToPos")
                                    .Replace("{x}", x.ToString("N1"))
                                    .Replace("{y}", y.ToString("N1"))
                                    .Replace("{z}", z.ToString("N1")));
                return;
            }

            ShowTeleportUI(player);
        }

        [ChatCommand("tpr")]
        void TPRCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("SyntaxTPR"));
                return;
            }

            string name = args[0];
            for (int i = 1; i < args.Length; i++)
                name += " " + args[i];

            List<BasePlayer> matches = FindByNameMulti(name);
            if (matches.Count() == 0)
            {
                SendReply(player, GetMessage("PlayerNotFound").Replace("{0}", name));
                return;
            }
            else if (matches.Count() > 1)
            {
                SendReply(player, GetMessage("MultiplePlayersFound").Replace("{0}", name));
                return;
            }
            BasePlayer targetPlayer = matches.First();

            if (targetPlayer == player && !DebuggingMode)
            {
                SendReply(player, GetMessage("CantTeleportToSelf"));
                return;
            }

            TPR(player, targetPlayer);
            return;
        }

        [ChatCommand("tpa")]
        void TPACommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>()?.TeleportRequest;

            if (tr == null)
            {
                SendReply(player, GetMessage("NoPendingRequests"));
                return;
            }

            tr.RequestAccepted();
            CloseUI(player);
        }

        [ChatCommand("tpd")]
        void TPDCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>()?.TeleportRequest;

            if (tr == null)
            {
                SendReply(player, GetMessage("NoPendingRequests"));
                return;
            }

            tr.RequestDeclined();
            CloseUI(player);
        }

        [ChatCommand("tpc")]
        void TPCCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permCancel))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TPC(player);
        }

        [ChatCommand("tpb")]
        void TPBCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permBack))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TPB(player);
        }

        [ChatCommand("tphere")]
        void TPHereCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permHere))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("SyntaxTPHere"));
                return;
            }

            string targetName = string.Join(" ", args);
            
            List<BasePlayer> matches = FindByNameMulti(targetName);
            if (matches.Count() == 0)
            {
                SendReply(player, GetMessage("PlayerNotFound").Replace("{0}", targetName));
                return;
            }
            if (matches.Count > 1)
            {
                SendReply(player, GetMessage("MultiplePlayersFound").Replace("{0}", targetName));
                return;
            }
            BasePlayer targetPlayer = matches.First();

            TPHere(player, targetPlayer.userID);
        }

        [ConsoleCommand("tpgui")]
        void TPGuiCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();

            string[] args = arg.Args ?? new string[] { };

            #region Check Perm
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }
            #endregion

            #region Open GUI
            if (args.Length == 0)
            {
                ShowTeleportUI(player);
                return;
            }

            if (args[0] == "True") //Because apparently bind b tpgui runs "tpgui True" -.-
            {
                ShowTeleportUI(player);
                return;
            }
            #endregion

            #region Close
            if (args[0] == "close")
            {
                CloseUI(player);
                return;
            }
            #endregion

            #region To
            if (args[0] == "to")
            {
                if (args.Length < 2) return;

                CloseUI(player);

                string userIDString = args[1];
                ulong userID;
                if (!ulong.TryParse(userIDString, out userID)) return;

                var guiSettings = GUIManager.Get(player);

                if (guiSettings.Sleepers)
                {
                    var target = BasePlayer.sleepingPlayerList.FirstOrDefault(x => x.userID == userID);
                    if (target == null)
                    {
                        SendReply(player, GetMessage("PlayerIDNotFound").Replace("{0}", userIDString));
                        return;
                    }

                    SendReply(player, GetMessage("YouTeleportedTo").Replace("{0}", target.displayName));
                    RecordLastTP(player, player.transform.position);
                    Teleport(player, target);
                    return;
                }

                BasePlayer targetPlayer = BasePlayer.FindByID(userID);

                if (targetPlayer == null)
                {
                    SendReply(player, GetMessage("PlayerIDNotFound").Replace("{0}", userIDString));
                    return;
                }

                TPR(player, targetPlayer);
                return;
            }
            #endregion

            #region Accept
            if (args[0] == "accept")
            {
                TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>()?.TeleportRequest;
                if (tr == null)
                {
                    SendReply(player, GetMessage("NoPendingRequests"));
                    return;
                }

                tr.RequestAccepted();
                CloseUI(player);
                return;
            }
            #endregion

            #region Decline
            if (args[0] == "decline")
            {
                TeleportRequest tr = player.gameObject.GetComponent<PendingRequest>()?.TeleportRequest;
                if (tr == null)
                {
                    SendReply(player, GetMessage("NoPendingRequests"));
                    return;
                }

                tr.RequestDeclined();
                CloseUI(player);
                return;
            }
            #endregion

            #region Back
            if (args[0] == "back")
            {
                if (!HasPerm(player, permBack))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                TPB(player);
                CloseUI(player);
                return;
            }
            #endregion

            #region Cancel
            if (args[0] == "cancel")
            {
                if (!HasPerm(player, permCancel))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                TPC(player);
                CloseUI(player);
                return;
            }
            #endregion

            #region Here TP
            if (args[0] == "heretp")
            {
                if (!HasPerm(player, permHere))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                if (args.Length < 2) return;

                string userIDString = args[1];
                ulong userID;
                if (!ulong.TryParse(userIDString, out userID)) return;

                TPHere(player, userID);
                return;
            }
            #endregion

            #region Set
            if (args[0] == "set")
            {
                if (args.Length < 3) return;

                switch (args[1])
                {
                    case "page":
                        int page;
                        if (!Int32.TryParse(args[2], out page)) return;
                        GUIManager.Get(player).Page = page;
                        UIChooser(player);
                        break;

                    case "tphere":
                        if (!HasPerm(player, permHere))
                        {
                            SendReply(player, GetMessage("NoPermission"));
                            return;
                        }
                        bool here = args[2] == bool.TrueString;
                        GUIManager.Get(player).TPHere = here;
                        UIChooser(player);
                        break;

                    case "sleepers":
                        if (!HasPerm(player, permSleepers))
                        {
                            SendReply(player, GetMessage("NoPermission"));
                            return;
                        }
                        bool sleepers = args[2] == bool.TrueString;
                        GUIManager.Get(player).Sleepers = sleepers;
                        UIChooser(player);
                        break;
                }
                return;
            }
            #endregion
        }

        [ConsoleCommand("resetdatafile")]
        void ResetAllCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;

            if (!DebuggingMode)
            {
                Debug.LogError("[TeleportGUI] Вы не можете использовать эту команду. Предупреждение: это непроверенно и небезопасно. Пожалуйста, не пропустите это предупреждение.");
                return;
            }

            storedData.Cooldowns.Clear();
            storedData.UsesToday.Clear();
            SaveData();
            Puts("Cleared data file and saved.");
        }

        #endregion

        #region GUIs

        void ShowTeleportUI(BasePlayer player)
        {
            if (!GUIOpen.ContainsKey(player))
                GUIOpen.Add(player, false);
            if (GUIOpen[player])
            {
                CloseUI(player);
                GUIOpen[player] = false;
                return;
            }
            GUIOpen[player] = true;

            UIChooser(player);
        }

        void UIChooser(BasePlayer player)
        {
            var guiSettings = GUIManager.Get(player);

            if (BasePlayer.activePlayerList.Count > 25 || guiSettings.Sleepers)
            {
                BigUI(player);
                return;
            }

            SmallUI(player);
        }

        void CloseUI(BasePlayer player)
        {
            if (!GUIOpen.ContainsKey(player))
                GUIOpen.Add(player, false);
            GUIOpen[player] = false;
            CuiHelper.DestroyUi(player, "smallTeleportGUI");
            CuiHelper.DestroyUi(player, "bigTeleportGUI");
        }

        void SmallUI(BasePlayer player)
        {
            var GUIElement = new CuiElementContainer();

            var guiSettings = GUIManager.Get(player);

            List<BasePlayer> players = new List<BasePlayer>(!guiSettings.Sleepers ? BasePlayer.allPlayerList : BasePlayer.sleepingPlayerList);

            if (!DebuggingMode && players.Contains(player))
            {
                players.Remove(player);
            }

            //if (debuggingMode) players.AddRange(SpareNames());

            players = players.OrderBy(x => x.displayName).ToList();

            #region Whole Panel

            var smallTeleportGUI = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0.3 0.3", //Left Bottom
                    AnchorMax = "0.7 0.75" // Right Top
                },
                CursorEnabled = true
            }, "Hud", "smallTeleportGUI");

            #endregion

            #region Title Bar

            var titleBar = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9", //Left Bottom
                    AnchorMax = "0.998 0.999" // Right Top
                }
            }, smallTeleportGUI);

            #region Pending Request Buttons

            bool pendingRequest = (player.gameObject.GetComponent<PendingRequest>() != null);
            string requestFrom = player.gameObject.GetComponent<PendingRequest>()?.From.displayName;

            if (pendingRequest)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.12 0.96"
                    },
                    Button =
                    {
                        Command = "tpgui accept",
                        Color = "0 1 0 1",
                    },
                    Text =
                    {
                        Text = "Accept",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.122 0",
                        AnchorMax = "0.24 0.96"
                    },
                    Button =
                    {
                        Command = "tpgui decline",
                        Color = "1 0 0 1",
                    },
                    Text =
                    {
                        Text = "Decline",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("RequestFrom", this).Replace("{0}", requestFrom),
                        FontSize = 16,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.26 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Title

            if (!pendingRequest)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("TeleportTitle", this),
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Sleepers
            if (HasPerm(player, permSleepers))
            {
                var sleepers = GUIManager.Get(player).Sleepers;
                string colour = (sleepers) ? "1 0.2 0.2 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.61 0",
                        AnchorMax = "0.71 0.97"
                    },
                    Text =
                    {
                        Text = "Sleepers",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set sleepers {!sleepers}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPHere
            if (HasPerm(player, permHere))
            {
                var tpHere = GUIManager.Get(player).TPHere;
                string colour = (tpHere) ? "0 1 0 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.71 0",
                        AnchorMax = "0.78 0.97"
                    },
                    Text =
                    {
                        Text = "Here",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set tphere {!tpHere}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPBack
            if (HasPerm(player, permBack))
            {
                var colour = (LastTeleport.ContainsKey(player)) ? "0.15 0.15 1 1" : "0.5 0.5 0.5 1";
                var command = (LastTeleport.ContainsKey(player)) ? "tpgui back" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.78 0",
                        AnchorMax = "0.85 0.97"
                    },
                    Text =
                    {
                        Text = "Back",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPCancel
            if (HasPerm(player, permCancel))
            {
                var colour = (HasPendingTeleport(player)) ? "1 0.5 0 1" : "0.5 0.5 0.5 1";
                var command = (HasPendingTeleport(player)) ? "tpgui cancel" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.85 0",
                        AnchorMax = "0.934 0.97"
                    },
                    Text =
                    {
                        Text = "Cancel",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region Close Button

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.935 0",
                    AnchorMax = "0.998 0.97"
                },
                Button =
                {
                    Command = "tpgui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Player List

            var playerList = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.9"
                }
            }, smallTeleportGUI);

            const float columnWidth = 0.2f;
            const float rowWidth = 0.2f;

            int playerCount = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (players.Count <= playerCount) continue;

                    BasePlayer target = players[playerCount];

                    var panel = GUIElement.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = (columnWidth * j).ToString() + " " + (1f - (rowWidth * i) - rowWidth).ToString(),
                            AnchorMax = ((columnWidth * j) + columnWidth).ToString() + " " + (1f - (rowWidth * i)).ToString()
                        },
                        Image =
                        {
                            Color = "0 0 0 0"
                        }
                    }, playerList);
                    
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = CleanText(target.displayName),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Color = "1 1 1 1",
                            Font = "robotocondensed-regular.ttf"
                        },
                        Button =
                        {
                            Command = $"tpgui to {target.userID}",
                            Color = "0 0 0 0"
                        }
                    }, panel);

                    if (GUIManager.Get(player).TPHere)
                    {
                        GUIElement.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.7 0.005",
                                AnchorMax = "0.97 0.2"
                            },
                            Text =
                            {
                                Text = "Here",
                                Color = "1 1 1 1",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 12
                            },
                            Button =
                            {
                                Command = $"tpgui heretp {target.userID}",
                                Color = "0 1 0 0.5"
                            }
                        }, panel);
                    }

                    playerCount++;
                }
            }

            #endregion

            #region Empty List

            if (players.Count == 0)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "Looks like you're a lone survivor!",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1",
                        AnchorMax = "1 1"
                    }
                }, playerList);
            }

            #endregion

            CuiHelper.DestroyUi(player, "smallTeleportGUI");
            CuiHelper.DestroyUi(player, "bigTeleportGUI");
            CuiHelper.AddUi(player, GUIElement);
        }

        void BigUI(BasePlayer player)
        {
            var GUIElement = new CuiElementContainer();

            var guiSettings = GUIManager.Get(player);

            List<BasePlayer> players = new List<BasePlayer>(!guiSettings.Sleepers ? BasePlayer.allPlayerList : BasePlayer.sleepingPlayerList);

            if (!DebuggingMode && players.Contains(player))
            {
                players.Remove(player);
            }

            //if (debuggingMode) players.AddRange(SpareNames());

            players = players.OrderBy(x => x.displayName).ToList();

            int maxPages = CalculatePages(players.Count);

            #region Whole Panel

            var bigTeleportGUI = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0.2 0.125", //Left Bottom
                    AnchorMax = "0.8 0.9" // Right Top
                },
                CursorEnabled = true
            }, "Hud", "bigTeleportGUI");

            #endregion

            #region Title Bar

            var titleBar = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.935", //Left Bottom
                    AnchorMax = "0.997 1.0" // Right Top
                }
            }, bigTeleportGUI);

            #region Pending Request Buttons

            bool pendingRequest = (player.gameObject.GetComponent<PendingRequest>() != null);
            string requestFrom = player.gameObject.GetComponent<PendingRequest>()?.From.displayName;

            if (pendingRequest)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.01",
                        AnchorMax = "0.16 0.98"
                    },
                    Button =
                    {
                        Command = "tpgui accept",
                        Color = "0 1 0 1",
                    },
                    Text =
                    {
                        Text = "Accept",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.1615 0.01",
                        AnchorMax = "0.32 0.98"
                    },
                    Button =
                    {
                        Command = "tpgui decline",
                        Color = "1 0 0 1",
                    },
                    Text =
                    {
                        Text = "Decline",
                        FontSize = 18,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    }
                }, titleBar);

                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("RequestFrom", this).Replace("{0}", requestFrom),
                        FontSize = 18,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.34 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Title

            if (!pendingRequest)
            {
                string pageNum = (maxPages > 1) ? $" - {GUIManager.Get(player).Page}" : "";
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("TeleportTitle", this) + pageNum,
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, titleBar);
            }

            #endregion

            #region Sleepers
            if (HasPerm(player, permSleepers))
            {
                var sleepers = GUIManager.Get(player).Sleepers;
                string colour = (sleepers) ? "1 0.2 0.2 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.62 0",
                        AnchorMax = "0.70 0.97"
                    },
                    Text =
                    {
                        Text = "Sleepers",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set sleepers {!sleepers}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPHere
            if (HasPerm(player, permHere))
            {
                var tpHere = GUIManager.Get(player).TPHere;
                string colour = (tpHere) ? "0 1 0 1" : "0.5 0.5 0.5 1";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.70 0",
                        AnchorMax = "0.78 0.97"
                    },
                    Text =
                    {
                        Text = "Here",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = $"tpgui set tphere {!tpHere}",
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPBack
            if (HasPerm(player, permBack))
            {
                var colour = (LastTeleport.ContainsKey(player)) ? "0.15 0.15 1 1" : "0.5 0.5 0.5 1";
                var command = (LastTeleport.ContainsKey(player)) ? "tpgui back" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.78 0",
                        AnchorMax = "0.86 0.97"
                    },
                    Text =
                    {
                        Text = "Back",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region TPCancel
            if (HasPerm(player, permCancel))
            {
                var colour = (HasPendingTeleport(player)) ? "1 0.5 0 1" : "0.5 0.5 0.5 1";
                var command = (HasPendingTeleport(player)) ? "tpgui cancel" : "";
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.86 0",
                        AnchorMax = "0.9385 0.98"
                    },
                    Text =
                    {
                        Text = "Cancel",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16
                    },
                    Button =
                    {
                        Command = command,
                        Color = colour
                    }
                }, titleBar);
            }
            #endregion

            #region Close Button

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.94 0.01",
                    AnchorMax = "1.0 0.98"
                },
                Button =
                {
                    Command = "tpgui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Player List

            var playerList = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.9"
                }
            }, bigTeleportGUI);

            var page = GUIManager.Get(player).Page;
            int playerCount = (page * 100) - 100;
            for (int j = 0; j < 20; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (players.Count <= playerCount) continue;

                    BasePlayer target = players[playerCount];

                    var panel = GUIElement.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = (0.2f * i).ToString() + " " + (1f - (0.05f * j) - 0.05f).ToString(),
                            AnchorMax = ((0.2f * i) + 0.2f).ToString() + " " + (1f - (0.05f * j)).ToString()
                        },
                        Image =
                        {
                            Color = "0 1 0 0"
                        }
                    }, playerList);
                    
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = CleanText(target.displayName),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Color = "1 1 1 1"
                        },
                        Button =
                        {
                            Command = $"tpgui to {target.userID}",
                            Color = "0 0 0 0"
                        }
                    }, panel);

                    if (GUIManager.Get(player).TPHere)
                    {
                        GUIElement.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.85 0",
                                AnchorMax = "0.98 0.5"
                            },
                            Text =
                            {
                                Text = "Here",
                                FontSize = 8,
                                Align = TextAnchor.MiddleCenter,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Command = $"tpgui heretp {target.userID}",
                                Color = "0 1 0 1"
                            }
                        }, panel);
                    }

                    playerCount++;
                }
            }

            #endregion

            #region Empty List

            if (players.Count == 0)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "Looks like you're a lone survivor!",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1",
                        AnchorMax = "1 1"
                    }
                }, playerList);
            }

            #endregion

            #region Page Buttons

            if (page < maxPages)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1.025 0.575",
                        AnchorMax = "1.1 0.675"
                    },
                    Text =
                    {
                        Text = "Up",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Command = $"tpgui set page {(page + 1).ToString()}",
                        Color = "0 0 0 0.75"
                    }
                }, bigTeleportGUI);
            }

            if (page > 1)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1.025 0.45",
                        AnchorMax = "1.1 0.55"
                    },
                    Text =
                    {
                        Text = "Down",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Command = $"tpgui set page {(page - 1).ToString()}",
                        Color = "0 0 0 0.75"
                    }
                }, bigTeleportGUI);
            }

            #endregion

            CuiHelper.DestroyUi(player, "smallTeleportGUI");
            CuiHelper.DestroyUi(player, "bigTeleportGUI");
            CuiHelper.AddUi(player, GUIElement);
        }

        #endregion

        #region TP Functions

        void TPR(BasePlayer player, BasePlayer targetPlayer)
        {
            if (player.IsAdmin && GetConfig<bool>("AdminTPEnabled"))
            {
                if (!GetConfig<bool>("AdminTPSilent"))
                    SendReply(player, GetMessage("TeleportedToYou").Replace("{0}", player.displayName));
                SendReply(player, GetMessage("YouTeleportedTo").Replace("{0}", targetPlayer.displayName));

                Vector3 currentPos = player.transform.position;
                RecordLastTP(player, currentPos);

                Teleport(player, targetPlayer);
                return;
            }

            if (storedData.Cooldowns.ContainsKey(player.userID))
            {
                SendReply(player, GetMessage("OnCooldown").Replace("{0}", storedData.Cooldowns[player.userID].ToString()));
                return;
            }

            if (player.IsWounded())
            {
                SendReply(player, GetMessage("CantTPWhilstWounded"));
                return;
            }

            if (!GetConfig<bool>("AllowTeleportWhilstBleeding"))
            {
                if (player.metabolism.bleeding.value > 0f)
                {
                    SendReply(player, GetMessage("PlayerIsBleeding"));
                    return;
                }
            }

            if (!GetConfig<bool>("AllowTeleportFromBuildBlock"))
            {
                if (!player.CanBuild())
                {
                    SendReply(player, GetMessage("PlayerIsBuildBlocked"));
                    return;
                }
            }

            if (!GetConfig<bool>("AllowTeleportToBuildBlockedPlayer"))
            {
                if (!targetPlayer.CanBuild())
                {
                    SendReply(player, GetMessage("TargetIsBuildBlocked"));
                    return;
                }
            }

            if (!GetConfig<bool>("AllowTeleportIntoBuildBlock"))
            {
                if (IsBuildingBlocked(player, targetPlayer.transform.position))
                {
                    SendReply(player, GetMessage("LocationIsBuildBlocked"));
                    return;
                }
            }

            if (IsCrafting(player))
            {
                SendReply(player, GetMessage("BlockTPCrafting"));
                return;
            }

            if (HasComponent<PendingRequest>(targetPlayer))
            {
                SendReply(player, GetMessage("HasPendingRequest").Replace("{0}", targetPlayer.displayName));
                return;
            }

            if (GetConfig<bool>("UseEconomicsPlugin"))
            {
                if (EconomicsInstalled())
                {
                    if (!CanAffordEconomics(player))
                    {
                        SendReply(player, GetMessage("CantAffordEconomics").Replace("{0}", GetConfig<double>("EconomicsPrice").ToString()));
                        return;
                    }
                    SendReply(player, GetMessage("EconomicsYouSpent").Replace("{0}", GetConfig<double>("EconomicsPrice").ToString()));
                }
            }

            if (GetConfig<bool>("UseServerRewardsPlugin"))
            {
                if (ServerRewardsInstalled())
                {
                    if (!CanAffordServerRewards(player))
                    {
                        SendReply(player, GetMessage("CantAffordServerRewards").Replace("{0}", GetConfig<double>("ServerRewardsPrice").ToString()));
                        return;
                    }
                    SendReply(player, GetMessage("ServerRewardsYouSpent").Replace("{0}", GetConfig<double>("ServerRewardsPrice").ToString()));
                }
            }

            if (GetConfig<int>("DefaultDailyLimit") != -1)
            {
                int maxTeleports = GetHighest(GetConfig<Dictionary<string, object>>("DailyLimit"), player, GetConfig<int>("DefaultDailyLimit"));
                if (!storedData.UsesToday.ContainsKey(player.userID))
                    storedData.UsesToday.Add(player.userID, 0);
                if (storedData.UsesToday[player.userID] >= maxTeleports)
                {
                    SendReply(player, GetMessage("MaxTeleportsReached"));
                    return;
                }
            }

            string canTeleport = Interface.Oxide.CallHook("CanTeleport", player) as string;
            if (canTeleport != null)
            {
                SendReply(player, canTeleport);
                return;
            }

            if (ZoneManager != null)
            {
                if (!GetConfig<bool>("CanTeleportFromZone"))
                {
                    var call = ZoneManager.Call("EntityHasFlag", player, "notp");
                    if (call is bool && (bool)call)
                    {
                        SendReply(player, GetMessage("CantTeleportFromZone"));
                        return;
                    }
                }

                if (!GetConfig<bool>("CanTeleportIntoZone"))
                {
                    var call = ZoneManager.Call("EntityHasFlag", targetPlayer, "notp");
                    if (call is bool && (bool)call)
                    {
                        SendReply(player, GetMessage("CantTeleportToZone"));
                        return;
                    }
                }
            }

            TeleportRequest.Create(player, targetPlayer, GetConfig<int>("RequestTimeoutTime"));
        }

        void TPC(BasePlayer player)
        {
            if (HasComponent<Teleporter>(player))
            {
                var teleporter = player.GetComponent<Teleporter>();
                teleporter.CancelTeleport();
                return;
            }

            var call = Interface.Oxide.CallHook("CancelAllTeleports", player);
            if (call is string)
            {
                SendReply(player, call as string);
                return;
            }

            SendReply(player, GetMessage("NothingToCancel"));
            return;
        }

        void TPB(BasePlayer player)
        {
            if (!LastTeleport.ContainsKey(player))
            {
                SendReply(player, GetMessage("NoBackLocation"));
                return;
            }

            Teleport(player, LastTeleport[player]);
            SendReply(player, GetMessage("TeleportedBack"));
        }

        void TPHere(BasePlayer player, ulong targetID)
        {
            if (!HasPerm(player, permHere))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            BasePlayer target = BasePlayer.FindByID(targetID);

            if (target == null)
            {
                SendReply(player, GetMessage("PlayerIDNotFound").Replace("{0}", targetID.ToString()));
                return;
            }

            if (!DebuggingMode && player == target)
            {
                SendReply(player, GetMessage("CantTeleportToSelf"));
                return;
            }

            Teleport(target, player);
            SendReply(player, GetMessage("SummonedToYou").Replace("{0}", target.displayName));
            SendReply(target, GetMessage("SummonedTo").Replace("{0}", player.displayName));
        }

        void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);

        void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping()) return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        #endregion

        #region Functions

        void RecordLastTP(BasePlayer player, Vector3 oldPos)
        {
            if (!LastTeleport.ContainsKey(player))
                LastTeleport.Add(player, Vector3.zero);
            LastTeleport[player] = oldPos;
        }

        bool HasPendingTeleport(BasePlayer player) => HasComponent<PendingRequest>(player) || HasComponent<Teleporter>(player);

        int IncrementUses(BasePlayer player)
        {
            if (!storedData.UsesToday.ContainsKey(player.userID))
                storedData.UsesToday.Add(player.userID, 0);
            storedData.UsesToday[player.userID]++;
            SaveData();
            int maxTeleports = Instance.GetHighest(Instance.GetConfig<Dictionary<string, object>>("DailyLimit"), player, Instance.GetConfig<int>("DefaultDailyLimit"));
            int usesRemaining = maxTeleports - storedData.UsesToday[player.userID];
            return usesRemaining;
        }

        int GetLowest(Dictionary<string, object> objDict, BasePlayer player, int lowest = Int32.MaxValue)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (var kvp in objDict)
                dict.Add(kvp.Key, Int32.Parse(kvp.Value.ToString()));
            foreach (var kvp in dict)
                if (kvp.Value < lowest)
                    if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                        lowest = kvp.Value;
            return lowest;
        }

        int GetHighest(Dictionary<string, object> objDict, BasePlayer player, int highest = Int32.MinValue)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (var kvp in objDict)
                dict.Add(kvp.Key, Int32.Parse(kvp.Value.ToString()));
            foreach (var kvp in dict)
                if (kvp.Value > highest)
                    if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                        highest = kvp.Value;
            return highest;
        }

        bool IsBuildingBlocked(BasePlayer player, Vector3 pos)
        {
            var colliders = Pool.GetList<Collider>();
            Vis.Colliders(pos, 0.1f, colliders, LayerMask.GetMask("Trigger"));
            var cupboard = colliders.Select(x => x.GetComponentInParent<BuildingPrivlidge>()).Where(x => x != null).FirstOrDefault();
            Pool.FreeList(ref colliders);
            if (cupboard == null) return false;
            return player.userID != cupboard.OwnerID && !cupboard.IsAuthed(player);
        }

        bool IsCrafting(BasePlayer player) => (player.inventory.crafting.queue.Count() > 0);

        int CalculatePages(int value) => (int)Math.Ceiling(value / 100d);

        BasePlayer FindByName(string name, bool sleepers = false)
        {
            List<BasePlayer> players = new List<BasePlayer>();
            if (sleepers) players = new List<BasePlayer>(BasePlayer.sleepingPlayerList);
            else players = new List<BasePlayer>(BasePlayer.activePlayerList);
            return players.Where(x => x.displayName.ToLower().Replace(" ", "")
                                       .Contains(name.ToLower().Replace(" ", "")))
                                       .FirstOrDefault();
        }

        List<BasePlayer> FindByNameMulti(string name, bool sleepers = false)
        {
            List<BasePlayer> players = new List<BasePlayer>();
            if (sleepers) players = new List<BasePlayer>(BasePlayer.sleepingPlayerList);
            else players = new List<BasePlayer>(BasePlayer.activePlayerList);
            return players.Where(x => x.displayName.ToLower().Replace(" ", "")
                                       .Contains(name.ToLower().Replace(" ", "")))
                                       .ToList();
        }

        #region Economics/ServerRewards

        bool EconomicsInstalled() => Economics != null;

        bool ServerRewardsInstalled() => ServerRewards != null;

        bool CanAffordEconomics(BasePlayer player)
        {
            double price = GetConfig<double>("EconomicsPrice");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);

            if (playerMoney - price >= 0)
            {
                Economics?.Call("Set", player.userID, playerMoney - price);
                return true;
            }
            return false;
        }

        bool CanAffordServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("ServerRewardsPrice");
            int currentPoints;
            var call = ServerRewards?.Call("CheckPoints", player.userID);
            if (call == null) currentPoints = 0;
            else currentPoints = (int)call;

            if (currentPoints - price >= 0)
            {
                ServerRewards.Call("TakePoints", player.userID, price);
                return true;
            }
            return false;
        }

        void RefundPlayerEconomics(BasePlayer player)
        {
            double price = GetConfig<double>("EconomicsPrice");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);
            Economics?.Call("Set", player.userID, playerMoney + price);
            SendReply(player, GetMessage("EconomicsRefunded").Replace("{0}", price.ToString()));
        }

        void RefundServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("ServerRewardsPrice");
            ServerRewards.Call("AddPoints", player.userID, price);
            SendReply(player, GetMessage("ServerRewardsRefunded").Replace("{0}", price.ToString()));
        }

        #endregion

        void ResetDailyUses()
        {
            storedData.UsesToday.Clear();
            SaveData();
            timer.Once(TimeUntilMidnight(), () => ResetDailyUses());
        }

        #endregion

        #region Helpers

        bool HasPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permUse) || player.IsAdmin);
        bool HasPerm(BasePlayer player, string perm) => (permission.UserHasPermission(player.UserIDString, perm) || player.IsAdmin);

        string CleanText(string text) => GetConfig<bool>("AllowSpecialCharacters") ? text : new Regex(@"[^A-Za-z0-9\/:*?<>|!@#$%^&()\[\] ]+").Replace(text, " ");

        int TimeUntilMidnight() => ((59 - DateTime.Now.Second) + ((59 - DateTime.Now.Minute) * 60) + ((23 - DateTime.Now.Hour) * 3600));

        T GetConfig<T>(string key)
        {
            if (Config[key] == null)
            {
                Debug.LogError($"[TeleportGUI] Tried to grab the key \"{key}\" from the config. Either add it manually or delete the config and allow it to regenerate.");
                return default(T);
            }
            return (T)Convert.ChangeType(Config[key], typeof(T));
        }

        string GetMessage(string key) => (GetConfig<bool>("PrefixEnabled") ? GetConfig<string>("PrefixText") : "") + lang.GetMessage(key, this);

        bool HasComponent<T>(GameObject go) => (go.GetComponent<T>() != null);
        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(this.Title, storedData);
        void ReadData() => storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(this.Title);
        #endregion

        #region Spare Names (For Testing)

        List<BasePlayer> SpareNames() => nameList.Select(x => new BasePlayer() { displayName = x, userID = 1 }).ToList();

        static string names = @"AaronLongjohnsonson John Thomas George James Henry Charles Joseph Frederick Robert Alfred Edward Arthur Richard Samuel Walter David Harry Albert Edwin Francis Frank Benjamin Herbert Daniel Tom Isaac Fred Peter Ernest Michael Stephen Patrick Matthew Edmund Frederic Alexander Philip Mark Evan Andrew Abraham Hugh Christopher Sidney Lewis Jonathan Jesse Ralph Joshua Sam Martin Owen Josiah Jacob Reuben Joe Leonard Edgar Eli Enoch Job Oliver Anthony Amos Horace Elijah Timothy Cornelius Moses Jeremiah Sydney Louis Nicholas Aaron Percy Ebenezer Willie Luke Dennis Jabez Levi Augustus Adam Nathaniel Harold Allen Griffith Bernard Rowland Ben Ellis Rees Archibald Ambrose Lawrence Morgan Noah Simon Ephraim Caleb Elias Reginald Roger Isaiah Phillip Jonas Nathan Clement Solomon Morris Charley Emanuel Gilbert Paul Hubert Maurice Simeon Abel Wilfred Dan Emmanuel Jim Ezra Squire Theodore Seth Horatio Wright Theophilus Vincent Wilson Alan Stanley Sampson Miles Wallace Israel Smith Humphrey Hiram Howard Cecil Felix Allan Oswald Silas Austin Nelson Douglas Hedley Enos Eugene Percival Spencer Edmond Septimus Robinson Luther Joel Adolphus Cuthbert Donald Bartholomew Elisha Uriah Laurence Johnson Lionel Clarence Llewellyn Oscar Norman Dick Charlie Godfrey Herman Colin Harvey Walker Denis Claude Zachariah Hezekiah Roland Llewelyn Harrison Julius Duncan Victor Jasper Jackson Lancelot Giles Jenkin Hartley Gerald Valentine Clifford Thompson Charlie Godfrey Herman Colin Harvey Walker Denis Claude Zachariah Hezekiah Roland Llewelyn Harrison Julius Duncan Victor Jasper Jackson Lancelot Giles Jenkin Hartley Gerald Valentine Clifford Thompson";

        string[] nameList = names.Split(' ');

        #endregion
    }
}