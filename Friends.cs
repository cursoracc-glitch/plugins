using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Newtonsoft.Json;
using System.Text;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Friends", "walkinrey", "1.1.1f")]
    public class Friends : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        private Dictionary<ulong, string> _lastInput = new Dictionary<ulong, string>();
        private Dictionary<ulong, int> _viewingNow = new Dictionary<ulong, int>();
        private Dictionary<ulong, ulong> _pendingRequests = new Dictionary<ulong, ulong>();

        private Dictionary<ulong, float> _requestCooldown = new Dictionary<ulong, float>();

        private const string _middleAnchor = "0.5 0.5";

        #region Конфиг

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty("Кнопки быстрого доступа, слева от кнопки удалить (макс. 3)")]
            public List<FriendButton> friendButtons = new List<FriendButton>();

            [JsonProperty("Настройка задержек")]
            public CooldownSetup cooldowns = new CooldownSetup();

            [JsonProperty("Настройка интерфейса")]
            public GUISetup gui = new GUISetup();

            [JsonProperty("Настройки логирования")]
            public LogOptions logs = new LogOptions();

            [JsonProperty("Время на принятие заявки в друзья")]
            public float addTime = 30f;

            [JsonProperty("Максимальное кол-во друзей")]
            public int maxFriends = 35;

            public class LogOptions
            {
                [JsonProperty("Включить логирование в файл?")]
                public bool enableLogs = false;

                [JsonProperty("Логировать о добавлении в друзья?")]
                public bool logAddFriends = true;

                [JsonProperty("Логировать о удалении из друзей?")]
                public bool logRemoveFriends = true;

                [JsonProperty("Логировать о игнорировании запроса в друзья?")]
                public bool logIgnoreRequest = true;

                [JsonProperty("Логировать о добавлении шкафа в дата-файл игрока?")]
                public bool logCupboardAdd = true;

                [JsonProperty("Логировать о добавлении турели в дата-файл игрока?")]
                public bool logTurretsAdd = true;
            }

            public class FriendButton
            {
                [JsonProperty("Отображаемое название")]
                public string name = "Телепорт";

                [JsonProperty("Исполняемая чатовая команда")]
                public string command = "tpr %FRIEND_ID% %FRIEND_NAME%";
            }

            public class CooldownSetup
            {
                [JsonProperty("Стандартная задержка на добавление в друзья")]
                public float cooldown = 60f;

                [JsonProperty("Задержка на добавление в друзья по пермишону")]
                public Dictionary<string, float> permissionCooldown = new Dictionary<string, float>();
            }

            public class GUISetup
            {
                [JsonProperty("Цвет задней панели (при использовании картинки используется для указания ее цвета)")]
                public string bgColor = "#242424";

                [JsonProperty("Цвет основной панели")]
                public string panelMainColor = "#333232";

                [JsonProperty("Цвет второстепенных панелей")]
                public string panelSecondColor = "#424242";

                [JsonProperty("Цвет кнопок перелистывания, панели информации о друзьях и опций")]
                public string panelThirdColor = "#525252";

                [JsonProperty("Цвет кнопок включить, отправить заявку в друзья")]
                public string greenButtonsColor = "#27ae60";

                [JsonProperty("Цвет кнопок выключить, удалить, закрыть")]
                public string redButtonsColor = "#e74c3c";

                [JsonProperty("Цвет дополнительных кнопок")]
                public string additionalButtonsColor = "#7f8c8d";

                [JsonProperty("Положение панели запроса в друзья")]
                public CuiRectTransformComponent panelRequest = new CuiRectTransformComponent();

                [JsonProperty("Картинка заднего фона")]
                public BackgroundImage image = new BackgroundImage();

                public class BackgroundImage
                {
                    [JsonProperty("Использовать картинку для заднего фона?")]
                    public bool enableImage = false;

                    [JsonProperty("Ссылка на картинку")]
                    public string imageUrl = "";
                }
            }

            public static Configuration GetDefault()
            {
                Configuration configuration = new Configuration();

                configuration.friendButtons = new List<FriendButton>
                {
                    new FriendButton(),

                    new FriendButton
                    {
                        name = "Трейд",
                        command = "trade %FRIEND_ID% %FRIEND_NAME%"
                    }
                };

                configuration.gui.panelRequest = new CuiRectTransformComponent
                {
                    AnchorMin = "0.75 0.07",
                    AnchorMax = "0.75 0.07",

                    OffsetMin = "-120 -31",
                    OffsetMax = "100 25"
                };

                configuration.cooldowns.permissionCooldown.Add("friends.vip", 30f);
                configuration.cooldowns.permissionCooldown.Add("friends.premium", 10f);

                return configuration;
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.GetDefault();

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        #endregion

        #region Дата

        private DataFile _data;

        private class DataFile
        {
            public Dictionary<ulong, PlayerData> playersData = new Dictionary<ulong, PlayerData>();

            public class PlayerData
            {
                public List<FriendInfo> friends = new List<FriendInfo>();

                public List<uint> entitiesCupboard = new List<uint>();
                public List<uint> entitiesTurret = new List<uint>();

                public bool enableAuthorizeLocks, enableAuthorizeCupboards, enableAuthorizeTurrets, enableFF;

                public class FriendInfo
                {
                    public ulong id;
                    public string name;
                }
            }

            public PlayerData GetPlayerData(ulong userID)
            {
                PlayerData data;

                if (playersData.ContainsKey(userID)) data = playersData[userID];
                else
                {
                    data = new DataFile.PlayerData();
                    SetPlayerData(userID, data);
                }

                return data;
            }

            public void SetPlayerData(ulong userID, PlayerData data)
            {
                if (playersData.ContainsKey(userID)) playersData.Remove(userID);

                playersData.Add(userID, data);
            }
        }

        protected void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<DataFile>("Friends");
            }
            catch
            {
                _data = new DataFile();
            }

            NextTick(() => SaveData());
        }

        protected void SaveData(bool needLog = false)
        {
            Interface.Oxide.DataFileSystem.WriteObject("Friends", _data);

            if (needLog) Puts("Дата-файл сохранен.");
        }

        #endregion

        #region Локализация

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Error_EnterName"] = "Введите никнейм игрока!",
                ["Error_NoPlayerFounded"] = "Не удалось найти игрока с таким никнеймом, возможно он не в сети!",
                ["Error_PlayerAlreadyFriend"] = "Игрок {0} уже у вас в друзьях!",
                ["Error_MaxFriends"] = "Вы достигли максимального кол-ва друзей!",
                ["Error_CooldownTime"] = "Подождите {0} секунд, прежде чем снова отправить заявку!",
                ["Error_HasIncoming"] = "У игрока уже есть другая входящая заявка в друзья!",
                ["Error_CantDelete"] = "Не удалось удалить друга!",
                ["Error_CantAccept"] = "Не удалось принять заявку в друзья!",
                ["Error_NoIncoming"] = "У вас нет входящих заявок в друзья!",
                ["Success_RequestSended"] = "Заявка успешно отправлена игроку {0}",
                ["Success_FriendDeleted"] = "Игрок {0} успешно удален из списка друзей!",
                ["Success_RequestDeclined"] = "Вы отклонили заявку в друзья",
                ["Success_RequestDeclined_Player"] = "Вы отклонили заявку в друзья от игрока {0}",
                ["Notice_FriendDeleted"] = "Игрок {0} удалил вас из списка друзей!",
                ["Notice_FriendClaimed_Target"] = "Игрок {0} теперь у вас в друзьях!",
                ["Notice_FriendClaimed_Player"] = "Теперь {0} является вашим другом!",
                ["Notice_RequestDeclined"] = "Игрок {0} отклонил вашу заявку в друзья",
                ["Notice_RequestOut_Target"] = "Время на принятие заявки истекло!",
                ["Notice_RequestOut_Sender"] = "Игрок {0} не принял вашу заявку вовремя!",
                ["Info_FF"] = "Огонь по друзьям: {0}",
                ["Info_Locks"] = "Авторизация в замках: {0}",
                ["Info_Turrets"] = "Авторизация в турелях: {0}",
                ["Info_Cupboard"] = "Авторизация в шкафах: {0}",
                ["Status_Enabled"] = "Включено",
                ["Status_Disabled"] = "Отключено",
                ["GUI_Header_Text"] = "HuntFriends",
                ["GUI_Settings"] = "Настройки",
                ["GUI_FriendList"] = "Список друзей",
                ["GUI_FriendAdd"] = "Добавить в друзья",
                ["GUI_Auth_Locks"] = "Авторизация в замках",
                ["GUI_Auth_Cupboard"] = "Авторизация в шкафах",
                ["GUI_Auth_Turret"] = "Авторизация в турелях",
                ["GUI_FF"] = "Урон по друзьям",
                ["GUI_FriendAdd_Nickname"] = "Введите никнейм",
                ["GUI_FriendAdd_SendRequest"] = "Отправить заявку в друзья",
                ["GUI_Disable_Text"] = "Выкл.",
                ["GUI_Enable_Text"] = "Вкл.",
                ["GUI_Request_Info"] = "Заявка в друзья от {0}",
                ["GUI_Request_Accept"] = "Принять",
                ["GUI_Request_Decline"] = "Отклонить",
                ["GUI_Card_Button_Remove"] = "Удалить"
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Error_EnterName"] = "Enter player nickname!",
                ["Error_NoPlayerFounded"] = "Couldn't find a player with that nickname, maybe he's offline!",
                ["Error_PlayerAlreadyFriend"] = "Player {0} is already your friend!",
                ["Error_MaxFriends"] = "You have reached the maximum number of friends!",
                ["Error_CooldownTime"] = "Wait {0} seconds before send your friend request again!",
                ["Error_HasIncoming"] = "The player already has another incoming friend request!",
                ["Error_CantDelete"] = "Failed to delete friend!",
                ["Error_CantAccept"] = "Failed to accept friend request!",
                ["Error_NoIncoming"] = "You have no incoming friend requests!",
                ["Success_RequestSended"] = "The request has been successfully sent to player {0}",
                ["Success_FriendDeleted"] = "Player {0} has been successfully removed from your friends list!",
                ["Success_RequestDeclined"] = "You declined a friend request",
                ["Success_RequestDeclined_Player"] = "You declined a friend request from player {0}",
                ["Notice_FriendDeleted"] = "Player {0} has removed you from the friends list!",
                ["Notice_FriendClaimed_Target"] = "Player {0} is now your friend!",
                ["Notice_FriendClaimed_Player"] = "Now {0} is your friend!",
                ["Notice_RequestDeclined"] = "Player {0} has rejected your friend request",
                ["Notice_RequestOut_Target"] = "The time for accepting friend request has expired!",
                ["Notice_RequestOut_Sender"] = "Player {0} did not accept your friend request on time!",
                ["Info_FF"] = "Friendly Fire: {0}",
                ["Info_Locks"] = "Locks auth: {0}",
                ["Info_Turrets"] = "Turrets auth: {0}",
                ["Info_Cupboard"] = "Cupboards auth: {0}",
                ["Status_Enabled"] = "Enabled",
                ["Status_Disabled"] = "Disabled",
                ["GUI_Header_Text"] = "HuntRUST",
                ["GUI_Settings"] = "Settings",
                ["GUI_FriendList"] = "Friends List",
                ["GUI_FriendAdd"] = "Add as Friend",
                ["GUI_Auth_Locks"] = "Locks auth",
                ["GUI_Auth_Cupboard"] = "Cupboards auth",
                ["GUI_Auth_Turret"] = "Turrets auth",
                ["GUI_FF"] = "Friendly Fire",
                ["GUI_FriendAdd_Nickname"] = "Enter nickname",
                ["GUI_FriendAdd_SendRequest"] = "Send friend request",
                ["GUI_Disable_Text"] = "Off",
                ["GUI_Enable_Text"] = "On",
                ["GUI_Request_Info"] = "Friend request from {0}",
                ["GUI_Request_Accept"] = "Accept",
                ["GUI_Request_Decline"] = "Decline",
                ["GUI_Card_Button_Remove"] = "Remove"
            }, this, "en");
        }

        private string GetMessage(string key, BasePlayer player) => lang.GetMessage(key, this, player.UserIDString);

        #endregion

        #region Методы

        [ChatCommand("friends")]
        private void chatCommand(BasePlayer player, string command, string[] args)
        {
            if (args == null || args?.Length == 0)
            {
                OpenMenu(player);
            }
            else
            {
                if (args[0] == "add")
                {
                    if (args.Length != 1)
                    {
                        var nickname = args.ToList();
                        nickname.Remove("add");

                        var data = _data.GetPlayerData(player.userID);
                        var target = (BasePlayer)null;

                        foreach (var playerFind in BasePlayer.activePlayerList)
                        {
                            if (playerFind.displayName.Contains(string.Join(" ", nickname.ToArray())))
                            {
                                target = playerFind;
                                break;
                            }
                        }

                        if (target == null) target = BasePlayer.Find(string.Join(" ", nickname.ToArray()));

                        if (target == null)
                        {
                            player.ChatMessage(GetMessage("Error_NoPlayerFounded", player));

                            return;
                        }

                        if (data.friends.Count + 2 > _config.maxFriends)
                        {
                            player.ChatMessage(GetMessage("Error_MaxFriends", player));
                        }

                        if (_requestCooldown.ContainsKey(player.userID))
                        {
                            if (_requestCooldown[player.userID] > UnityEngine.Time.realtimeSinceStartup)
                            {
                                player.ChatMessage(string.Format(GetMessage("Error_CooldownTime", player), Mathf.RoundToInt(_requestCooldown[player.userID] - UnityEngine.Time.realtimeSinceStartup)));

                                return;
                            }
                            else _requestCooldown.Remove(player.userID);
                        }

                        var comp = target.GetComponent<PlayerPendingRequest>();

                        if (comp != null)
                        {
                            player.ChatMessage(GetMessage("Error_HasIncoming", player));

                            return;
                        }

                        comp = target.gameObject.AddComponent<PlayerPendingRequest>();

                        comp.from = player.userID;
                        comp.to = target.userID;

                        comp.cooldownTime = _config.addTime;

                        RenderRequest(target, player);

                        foreach (var perm in _config.cooldowns.permissionCooldown.Keys)
                        {
                            if (permission.UserHasPermission(player.UserIDString, perm))
                            {
                                _requestCooldown.Add(player.userID, UnityEngine.Time.realtimeSinceStartup + _config.cooldowns.permissionCooldown[perm]);

                                return;
                            }
                        }

                        _requestCooldown.Add(player.userID, UnityEngine.Time.realtimeSinceStartup + _config.cooldowns.cooldown);

                        player.ChatMessage(string.Format(GetMessage("Success_RequestSended", player), target.displayName));
                    }
                    else
                    {
                        player.ChatMessage(GetMessage("Error_EnterName", player));

                        return;
                    }
                }

                if (args[0] == "remove")
                {
                    if (args.Length != 1)
                    {
                        var target = BasePlayer.Find(args[1]);


                        if (target == null)
                        {
                            player.ChatMessage(GetMessage("Error_CantDelete", player));

                            return;
                        }
                        else
                        {
                            var data = _data.GetPlayerData(player.userID);

                            if (data.friends.Where(x => x.id == target.userID)?.Count() != 0)
                            {
                                data.friends.RemoveAll(x => x.id == target.userID);
                            }

                            _data.SetPlayerData(player.userID, data);

                            player.ChatMessage(string.Format(GetMessage("Success_FriendDeleted", player), target.displayName));

                            if (target.IsConnected) target.ChatMessage(string.Format(GetMessage("Notice_FriendDeleted", player), target.displayName));

                            var targetData = _data.GetPlayerData(target.userID);

                            targetData.friends.RemoveAll(x => x.id == player.userID);

                            _data.SetPlayerData(target.userID, targetData);

                            if (_config.logs.enableLogs)
                            {
                                if (_config.logs.logRemoveFriends)
                                {
                                    LogToFile("Log", $"Игрок {player} удалил из друзей игрока {target} | Друзей: {data.friends.Count}", this);
                                }
                            }

                            DeAuthFriend(player.userID, target.userID);
                            API_OnFriendRemoved(player.userID, target.userID);

                            SaveData();
                        }
                    }
                    else
                    {
                        player.ChatMessage(GetMessage("Error_EnterName", player));

                        return;
                    }
                }

                if (args[0] == "accept")
                {
                    if (_pendingRequests.ContainsKey(player.userID))
                    {
                        string targetID = _pendingRequests[player.userID].ToString();

                        var target = BasePlayer.Find(targetID);


                        if (target == null)
                        {
                            CuiHelper.DestroyUi(player, "Friends_Accept");

                            player.ChatMessage(GetMessage("Error_CantAccept", player));

                            _pendingRequests.Remove(player.userID);
                        }
                        else
                        {
                            CuiHelper.DestroyUi(player, "Friends_Accept");

                            var data = _data.GetPlayerData(player.userID);

                            if (data.friends.Where(x => x.id == target.userID)?.Count() == 0)
                            {
                                data.friends.Add(new DataFile.PlayerData.FriendInfo
                                {
                                    id = target.userID,
                                    name = target.displayName
                                });
                            }

                            var data2 = _data.GetPlayerData(target.userID);

                            if (data2.friends.Where(x => x.id == player.userID)?.Count() == 0)
                            {
                                data2.friends.Add(new DataFile.PlayerData.FriendInfo
                                {
                                    id = player.userID,
                                    name = player.displayName
                                });
                            }

                            _data.SetPlayerData(player.userID, data);
                            _data.SetPlayerData(target.userID, data2);

                            if (player.IsConnected) player.ChatMessage(string.Format(GetMessage("Notice_FriendClaimed_Player", player), target.displayName));
                            if (target.IsConnected) target.ChatMessage(string.Format(GetMessage("Notice_FriendClaimed_Target", target), player.displayName));

                            _pendingRequests.Remove(target.userID);

                            if (target.GetComponent<PlayerPendingRequest>() != null)
                                UnityEngine.Object.Destroy(target.GetComponent<PlayerPendingRequest>());

                            if (_config.logs.enableLogs)
                            {
                                if (_config.logs.logAddFriends)
                                {
                                    LogToFile("Log", $"Игрок {player} добавил в друзья игрока {target} | Друзей: {data.friends.Count}", this);
                                }
                            }

                            AuthFriend(target.userID, player.userID);
                            API_OnFriendAdded(player.userID, target.userID);

                            SaveData();
                        }
                    }
                    else
                    {
                        player.ChatMessage(GetMessage("Error_NoIncoming", player));

                        return;
                    }
                }

                var playerData = _data.GetPlayerData(player.userID);

                if (args[0] == "ff")
                {
                    playerData.enableFF = !playerData.enableFF;

                    _data.SetPlayerData(player.userID, playerData);

                    player.ChatMessage(string.Format(GetMessage("Info_FF", player), ConvertBool(playerData.enableFF, player).ToLower()));
                }

                if (args[0] == "locks")
                {
                    playerData.enableAuthorizeLocks = !playerData.enableAuthorizeLocks;

                    _data.SetPlayerData(player.userID, playerData);

                    player.ChatMessage(string.Format(GetMessage("Info_Locks", player), ConvertBool(playerData.enableAuthorizeLocks, player).ToLower()));

                    return;
                }

                if (args[0] == "turrets")
                {
                    playerData.enableAuthorizeTurrets = !playerData.enableAuthorizeTurrets;

                    _data.SetPlayerData(player.userID, playerData);

                    player.ChatMessage(string.Format(GetMessage("Info_Turrets", player), ConvertBool(playerData.enableAuthorizeTurrets, player).ToLower()));

                    return;
                }

                if (args[0] == "cupboard")
                {
                    playerData.enableAuthorizeCupboards = !playerData.enableAuthorizeCupboards;

                    _data.SetPlayerData(player.userID, playerData);

                    player.ChatMessage(string.Format(GetMessage("Info_Cupboard", player), ConvertBool(playerData.enableAuthorizeCupboards, player).ToLower()));

                    return;
                }
            }
        }

        [ConsoleCommand("friends")]
        private void ConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
            {
                var player = arg.Player();

                if (arg.HasArgs())
                {
                    if (arg.Args[0] == "close")
                    {
                        CuiHelper.DestroyUi(player, "Friends_Background");
                    }

                    if (arg.Args[0] == "command")
                    {
                        if (arg.HasArgs(2))
                        {
                            string args = "";

                            if (arg.HasArgs(3))
                            {
                                for (int i = 3; i < arg.Args.Length; i++)
                                {
                                    args = args + $"\"{arg.Args[i]}\"";
                                }
                            }

                            rust.RunClientCommand(player, $"chat.say", new string[] { $"/{arg.Args[1]} {args}" });
                        }
                    }

                    if (arg.Args[0] == "deauthorize")
                    {
                        if (arg.HasArgs(2))
                        {
                            var playerData = _data.GetPlayerData(player.userID);

                            if (arg.Args[1] == "locks")
                            {
                                if (playerData.enableAuthorizeLocks) playerData.enableAuthorizeLocks = false;
                                else return;
                            }

                            if (arg.Args[1] == "turrets")
                            {
                                if (playerData.enableAuthorizeTurrets) playerData.enableAuthorizeTurrets = false;
                                else return;
                            }

                            if (arg.Args[1] == "cupboard")
                            {
                                if (playerData.enableAuthorizeCupboards) playerData.enableAuthorizeCupboards = false;
                                else return;
                            }

                            _data.SetPlayerData(player.userID, playerData);
                            RenderText(player, arg.Args[1] == "locks", arg.Args[1] == "turrets", false, arg.Args[1] == "cupboard");
                        }
                    }

                    if (arg.Args[0] == "authorize")
                    {
                        if (arg.HasArgs(2))
                        {
                            var playerData = _data.GetPlayerData(player.userID);

                            if (arg.Args[1] == "locks")
                            {
                                if (!playerData.enableAuthorizeLocks) playerData.enableAuthorizeLocks = true;
                                else return;
                            }

                            if (arg.Args[1] == "turrets")
                            {
                                if (!playerData.enableAuthorizeTurrets) playerData.enableAuthorizeTurrets = true;
                                else return;
                            }

                            if (arg.Args[1] == "cupboard")
                            {
                                if (!playerData.enableAuthorizeCupboards) playerData.enableAuthorizeCupboards = true;
                                else return;
                            }

                            _data.SetPlayerData(player.userID, playerData);
                            RenderText(player, arg.Args[1] == "locks", arg.Args[1] == "turrets", false, arg.Args[1] == "cupboard");
                        }
                    }

                    if (arg.Args[0] == "ff")
                    {
                        if (arg.HasArgs(2))
                        {
                            var playerData = _data.GetPlayerData(player.userID);

                            if (arg.Args[1] == "enable")
                            {
                                if (!playerData.enableFF) playerData.enableFF = true;
                                else return;
                            }

                            if (arg.Args[1] == "disable")
                            {
                                if (playerData.enableFF) playerData.enableFF = false;
                                else return;
                            }

                            _data.SetPlayerData(player.userID, playerData);
                            RenderText(player, false, false, true, false);
                        }
                    }

                    if (arg.Args[0] == "insert")
                    {
                        if (arg.HasArgs(2))
                        {
                            if (_lastInput.ContainsKey(player.userID)) _lastInput.Remove(player.userID);

                            _lastInput.Add(player.userID, arg.Args[1]);
                        }
                    }

                    if (arg.Args[0] == "add")
                    {
                        if (_lastInput.ContainsKey(player.userID))
                        {
                            var nickname = _lastInput[player.userID];

                            var target = (BasePlayer)null;
                            var data = _data.GetPlayerData(player.userID);

                            foreach (var playerFind in BasePlayer.activePlayerList)
                            {
                                if (playerFind.displayName.Contains(nickname))
                                {
                                    target = playerFind;
                                    break;
                                }
                            }

                            if (target == null) target = BasePlayer.Find(nickname);

                            if (target == null)
                            {
                                RenderNotice(player, GetMessage("Error_NoPlayerFounded", player));
                            }
                            else
                            {
                                if (data.friends.Where(x => x.id == target.userID)?.Count() != 0)
                                {
                                    RenderNotice(player, string.Format(GetMessage("Error_PlayerAlreadyFriend", player), target.displayName));
                                }
                                else
                                {
                                    if (data.friends.Count + 2 > _config.maxFriends)
                                    {
                                        RenderNotice(player, GetMessage("Error_MaxFriends", player));
                                    }
                                    else
                                    {
                                        if (_requestCooldown.ContainsKey(player.userID))
                                        {
                                            if (_requestCooldown[player.userID] > UnityEngine.Time.realtimeSinceStartup)
                                            {
                                                RenderNotice(player, string.Format(GetMessage("Error_CooldownTime", player), Mathf.RoundToInt(_requestCooldown[player.userID] - UnityEngine.Time.realtimeSinceStartup)));

                                                return;
                                            }
                                            else _requestCooldown.Remove(player.userID);
                                        }

                                        var comp = target.GetComponent<PlayerPendingRequest>();

                                        if (comp != null)
                                        {
                                            RenderNotice(player, GetMessage("Error_HasIncoming", player));
                                            return;
                                        }

                                        comp = target.gameObject.AddComponent<PlayerPendingRequest>();

                                        comp.from = player.userID;
                                        comp.to = target.userID;

                                        comp.cooldownTime = _config.addTime;

                                        RenderNotice(player, string.Format(GetMessage("Success_RequestSended", player), target.displayName));
                                        RenderRequest(target, player);

                                        foreach (var perm in _config.cooldowns.permissionCooldown.Keys)
                                        {
                                            if (permission.UserHasPermission(player.UserIDString, perm))
                                            {
                                                _requestCooldown.Add(player.userID, UnityEngine.Time.realtimeSinceStartup + _config.cooldowns.permissionCooldown[perm]);

                                                return;
                                            }
                                        }

                                        _requestCooldown.Add(player.userID, UnityEngine.Time.realtimeSinceStartup + _config.cooldowns.cooldown);
                                    }
                                }
                            }
                        }
                    }

                    if (arg.Args[0] == "remove")
                    {
                        if (arg.HasArgs(2))
                        {
                            var target = BasePlayer.Find(arg.Args[1]);


                            if (target == null)
                            {
                                RenderNotice(player, GetMessage("Error_CantDelete", player));
                            }
                            else
                            {
                                var data = _data.GetPlayerData(player.userID);

                                if (data.friends.Where(x => x.id == target.userID)?.Count() != 0)
                                {
                                    data.friends.RemoveAll(x => x.id == target.userID);
                                }

                                _data.SetPlayerData(player.userID, data);

                                OpenMenu(player, 1, false);
                                RenderNotice(player, string.Format(GetMessage("Success_FriendDeleted", player), target.displayName));

                                if (target.IsConnected) target.ChatMessage(string.Format(GetMessage("Notice_FriendDeleted", target), player.displayName));

                                var targetData = _data.GetPlayerData(target.userID);

                                targetData.friends.RemoveAll(x => x.id == player.userID);

                                _data.SetPlayerData(target.userID, targetData);

                                if (_config.logs.enableLogs)
                                {
                                    if (_config.logs.logRemoveFriends)
                                    {
                                        LogToFile("Log", $"Игрок {player} удалил из друзей игрока {target} | Друзей: {data.friends.Count}", this);
                                    }
                                }

                                DeAuthFriend(player.userID, target.userID);
                                API_OnFriendRemoved(player.userID, target.userID);

                                SaveData();
                            }
                        }
                    }

                    if (arg.Args[0] == "accept")
                    {
                        if (_pendingRequests.ContainsKey(player.userID))
                        {
                            string targetID = _pendingRequests[player.userID].ToString();

                            var target = BasePlayer.Find(targetID);


                            if (target == null)
                            {
                                CuiHelper.DestroyUi(player, "Friends_Accept");

                                player.ChatMessage(GetMessage("Error_CantAccept", player));

                                _pendingRequests.Remove(player.userID);
                            }
                            else
                            {
                                CuiHelper.DestroyUi(player, "Friends_Accept");

                                var data = _data.GetPlayerData(player.userID);

                                if (data.friends.Where(x => x.id == target.userID)?.Count() == 0)
                                {
                                    data.friends.Add(new DataFile.PlayerData.FriendInfo
                                    {
                                        id = target.userID,
                                        name = target.displayName
                                    });
                                }

                                var data2 = _data.GetPlayerData(target.userID);

                                if (data2.friends.Where(x => x.id == player.userID)?.Count() == 0)
                                {
                                    data2.friends.Add(new DataFile.PlayerData.FriendInfo
                                    {
                                        id = player.userID,
                                        name = player.displayName
                                    });
                                }

                                _data.SetPlayerData(player.userID, data);
                                _data.SetPlayerData(target.userID, data2);

                                if (player.IsConnected) player.ChatMessage(string.Format(GetMessage("Notice_FriendClaimed_Player", player), target.displayName));
                                if (target.IsConnected) target.ChatMessage(string.Format(GetMessage("Notice_FriendClaimed_Target", target), player.displayName));

                                _pendingRequests.Remove(target.userID);

                                if (target.GetComponent<PlayerPendingRequest>() != null)
                                    UnityEngine.Object.Destroy(target.GetComponent<PlayerPendingRequest>());

                                if (_config.logs.enableLogs)
                                {
                                    if (_config.logs.logAddFriends)
                                    {
                                        LogToFile("Log", $"Игрок {player} добавил в друзья игрока {target} | Друзей: {data.friends.Count}", this);
                                    }
                                }

                                AuthFriend(target.userID, player.userID);
                                API_OnFriendAdded(player.userID, target.userID);

                                SaveData();
                            }
                        }
                    }

                    if (arg.Args[0] == "noaccept")
                    {
                        if (_pendingRequests.ContainsKey(player.userID))
                        {
                            CuiHelper.DestroyUi(player, "Friends_Accept");

                            ulong targetID = _pendingRequests[player.userID];

                            var target = BasePlayer.Find(targetID.ToString());


                            if (target != null) target.ChatMessage(string.Format(GetMessage("Notice_RequestDeclined", target), player.displayName));

                            _pendingRequests.Remove(player.userID);

                            if (target.GetComponent<PlayerPendingRequest>() != null)
                                UnityEngine.Object.Destroy(target.GetComponent<PlayerPendingRequest>());

                            if (target != null) player.ChatMessage(string.Format(GetMessage("Success_RequestDeclined_Player", player), target.displayName));
                            else player.ChatMessage(GetMessage("Success_RequestDeclined", player));
                        }
                    }

                    if (arg.Args[0] == "page+")
                    {
                        if (!_viewingNow.ContainsKey(player.userID)) return;
                        else
                        {
                            int page = _viewingNow[player.userID];
                            int allFriends = _data.GetPlayerData(player.userID).friends.Count;

                            if (10 * page < allFriends)
                            {
                                _viewingNow.Remove(player.userID);
                                _viewingNow.Add(player.userID, page + 1);

                                RenderFriends(player, null, page + 1);
                            }
                            else return;
                        }
                    }

                    if (arg.Args[0] == "page-")
                    {
                        if (!_viewingNow.ContainsKey(player.userID)) return;
                        else
                        {
                            int page = _viewingNow[player.userID];

                            if (page <= 1) return;
                            else
                            {
                                _viewingNow.Remove(player.userID);
                                _viewingNow.Add(player.userID, page - 1);

                                RenderFriends(player, null, page - 1);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region GUI

        private string ConvertBool(bool value, BasePlayer player)
        {
            if (value) return GetMessage("Status_Enabled", player);
            else return GetMessage("Status_Disabled", player);
        }

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);

            var sb = new StringBuilder();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }

        private void OpenMenu(BasePlayer player, int page = 1, bool recreateBackground = true)
        {
            var data = _data.GetPlayerData(player.userID);

            CuiElementContainer container = new CuiElementContainer();

            if (recreateBackground)
            {
                if (_config.gui.image.enableImage)
                {
                    if (ImageLibrary != null)
                    {
                        container.Add(new CuiElement
                        {
                            Name = "Friends_Background",
                            Parent = "Overlay",

                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = ImageLibrary.Call<string>("GetImage", "Friends_Background"),
                                    Color = HexToRustFormat(_config.gui.bgColor)
                                },

                                new CuiNeedsCursorComponent(),

                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1"
                                },
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Name = "Friends_Background",
                            Parent = "Overlay",

                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Url = _config.gui.image.imageUrl,
                                    Color = HexToRustFormat(_config.gui.bgColor)
                                },

                                new CuiNeedsCursorComponent(),

                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1"
                                },
                            }
                        });
                    }
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "Friends_Background",
                        Parent = "Overlay",

                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat(_config.gui.bgColor)
                            },

                            new CuiNeedsCursorComponent(),

                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            },
                        }
                    });
                }
            }

            container.Add(new CuiElement
            {
                Name = "Friends_Main",
                Parent = "Friends_Background",

                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Panel",
                Parent = "Friends_Main",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelMainColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-400 -250",
                        OffsetMax = "400 250"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Header",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelSecondColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-395 205",
                        OffsetMax = "395 245"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Header_Text",
                Parent = "Main_Header",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Header_Text", player),
                        FontSize = 23,
                        Align = TextAnchor.MiddleLeft
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-385 -20",
                        OffsetMax = "0 20"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Header_Button",
                Parent = "Main_Header",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends close",
                        Color = "0.7372549 0.3803922 0.3803922 1"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "355 -15",
                        OffsetMax = "390 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Header_Button_Text",
                Parent = "Header_Button",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "X",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Friends",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelSecondColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-395 -245",
                        OffsetMax = "180 175"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Options",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelSecondColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "185 -65",
                        OffsetMax = "395 175"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Text_Locks",
                Parent = "Main_Options",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Auth_Locks", player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -30",
                        OffsetMax = "105 0"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Panel_Locks",
                Parent = "Main_Options",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -60",
                        OffsetMax = "105 -30"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Locks_Status_Text",
                Parent = "Option_Panel_Locks",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = ConvertBool(data.enableAuthorizeLocks, player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 -15",
                        OffsetMax = "-10 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Locks_Button_Disable",
                Parent = "Option_Panel_Locks",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends deauthorize locks",
                        Color = HexToRustFormat(_config.gui.redButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "50 -10",
                        OffsetMax = "100 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Locks_Text_Disable",
                Parent = "Locks_Button_Disable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Disable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Locks_Button_Enable",
                Parent = "Option_Panel_Locks",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends authorize locks",
                        Color = HexToRustFormat(_config.gui.greenButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "0 -10",
                        OffsetMax = "45 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Locks_Text_Enable",
                Parent = "Locks_Button_Enable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Enable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Text_Cupboard",
                Parent = "Main_Options",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Auth_Cupboard", player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -90",
                        OffsetMax = "105 -60"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Panel_Cupboard",
                Parent = "Main_Options",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -120",
                        OffsetMax = "105 -90"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Cupboard_Status_Text",
                Parent = "Option_Panel_Cupboard",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = ConvertBool(data.enableAuthorizeCupboards, player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 -15",
                        OffsetMax = "-10 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Cupboard_Button_Disable",
                Parent = "Option_Panel_Cupboard",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends deauthorize cupboard",
                        Color = HexToRustFormat(_config.gui.redButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "50 -10",
                        OffsetMax = "100 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Cupboard_Text_Disable",
                Parent = "Cupboard_Button_Disable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Disable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Cupboard_Button_Enable",
                Parent = "Option_Panel_Cupboard",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends authorize cupboard",
                        Color = HexToRustFormat(_config.gui.greenButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "0 -10",
                        OffsetMax = "45 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Cupboard_Text_Enable",
                Parent = "Cupboard_Button_Enable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Enable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Text_Turrets",
                Parent = "Main_Options",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Auth_Turret", player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -150",
                        OffsetMax = "105 -120"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Panel_Turrets",
                Parent = "Main_Options",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -180",
                        OffsetMax = "105 -150"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Turrets_Status_Text",
                Parent = "Option_Panel_Turrets",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = ConvertBool(data.enableAuthorizeTurrets, player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 -15",
                        OffsetMax = "-10 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Turrets_Button_Disable",
                Parent = "Option_Panel_Turrets",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends deauthorize turrets",
                        Color = HexToRustFormat(_config.gui.redButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "50 -10",
                        OffsetMax = "100 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Turrets_Disable_Text",
                Parent = "Turrets_Button_Disable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Disable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Turrets_Button_Enable",
                Parent = "Option_Panel_Turrets",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends authorize turrets",
                        Color = HexToRustFormat(_config.gui.greenButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "0 -10",
                        OffsetMax = "45 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Turrets_Text_Enable",
                Parent = "Turrets_Button_Enable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Enable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Text_FF",
                Parent = "Main_Options",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_FF", player),
                        Align = TextAnchor.MiddleCenter,
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -210",
                        OffsetMax = "105 -180"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Option_Panel_FF",
                Parent = "Main_Options",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",

                        OffsetMin = "-105 -240",
                        OffsetMax = "105 -210"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FF_Status_Text",
                Parent = "Option_Panel_FF",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = ConvertBool(data.enableFF, player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 -15",
                        OffsetMax = "-10 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FF_Button_Disable",
                Parent = "Option_Panel_FF",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends ff disable",
                        Color = HexToRustFormat(_config.gui.redButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "50 -10",
                        OffsetMax = "100 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FF_Text_Disable",
                Parent = "FF_Button_Disable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Disable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FF_Button_Enable",
                Parent = "Option_Panel_FF",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends ff enable",
                        Color = HexToRustFormat(_config.gui.greenButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "0 -10",
                        OffsetMax = "45 10"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FF_Text_Enable",
                Parent = "FF_Button_Enable",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Enable_Text", player),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_FriendsAdd",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelSecondColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "185 -205",
                        OffsetMax = "395 -105"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FriendsAdd_Nickname_Text",
                Parent = "Main_FriendsAdd",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_FriendAdd_Nickname", player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 20",
                        OffsetMax = "100 45"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FriendsAdd_InputField_Background",
                Parent = "Main_FriendsAdd",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 -10",
                        OffsetMax = "100 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FriendsAdd_InputField",
                Parent = "FriendsAdd_InputField_Background",

                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Command = "friends insert",
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FriendsAdd_Button",
                Parent = "Main_FriendsAdd",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends add",
                        Color = HexToRustFormat(_config.gui.greenButtonsColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-100 -40",
                        OffsetMax = "100 -15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "FriendsAdd_Button_Text",
                Parent = "FriendsAdd_Button",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_FriendAdd_SendRequest", player),
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_PagesPanel",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "185 -245",
                        OffsetMax = "395 -210"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Pages_ButtonLeft",
                Parent = "Main_PagesPanel",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends page-",
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.49 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Pages_ButtonLeft_Text",
                Parent = "Pages_ButtonLeft",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "<<<",
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Pages_ButtonRight",
                Parent = "Main_PagesPanel",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "friends page+",
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.51 0",
                        AnchorMax = "0.999 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Pages_ButtonRight_Text",
                Parent = "Pages_ButtonRight",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = ">>>",
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Text_Options",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Settings", player),
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "190 175",
                        OffsetMax = "390 205"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Text_FriendsList",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_FriendList", player),
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-395 175",
                        OffsetMax = "180 205"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Main_Text_AddFriend",
                Parent = "Main_Panel",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_FriendAdd", player),
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "185 -100",
                        OffsetMax = "395 -70"
                    }
                }
            });

            RenderFriends(player, container, page);
        }

        private void RenderText(BasePlayer player, bool locks = false, bool turrets = false, bool ff = false, bool cupboards = false)
        {
            var data = _data.GetPlayerData(player.userID);

            if (locks)
            {
                CuiHelper.DestroyUi(player, "Locks_Status_Text");

                CuiHelper.AddUi(player, new List<CuiElement>
                {
                    new CuiElement
                    {
                        Name = "Locks_Status_Text",
                        Parent = "Option_Panel_Locks",

                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = ConvertBool(data.enableAuthorizeLocks, player),
                                Align = TextAnchor.MiddleCenter
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = _middleAnchor,
                                AnchorMax = _middleAnchor,

                                OffsetMin = "-100 -15",
                                OffsetMax = "-10 15"
                            }
                        }
                    }
                });
            }

            if (turrets)
            {
                CuiHelper.DestroyUi(player, "Turrets_Status_Text");

                CuiHelper.AddUi(player, new List<CuiElement>
                {
                    new CuiElement
                    {
                        Name = "Turrets_Status_Text",
                        Parent = "Option_Panel_Turrets",

                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = ConvertBool(data.enableAuthorizeTurrets, player),
                                Align = TextAnchor.MiddleCenter
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = _middleAnchor,
                                AnchorMax = _middleAnchor,

                                OffsetMin = "-100 -15",
                                OffsetMax = "-10 15"
                            }
                        }
                    }
                });
            }

            if (ff)
            {
                CuiHelper.DestroyUi(player, "FF_Status_Text");

                CuiHelper.AddUi(player, new List<CuiElement>
                {
                    new CuiElement
                    {
                        Name = "FF_Status_Text",
                        Parent = "Option_Panel_FF",

                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = ConvertBool(data.enableFF, player),
                                Align = TextAnchor.MiddleCenter
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = _middleAnchor,
                                AnchorMax = _middleAnchor,

                                OffsetMin = "-100 -15",
                                OffsetMax = "-10 15"
                            }
                        }
                    }
                });
            }

            if (cupboards)
            {
                CuiHelper.DestroyUi(player, "Cupboard_Status_Text");

                CuiHelper.AddUi(player, new List<CuiElement>
                {
                    new CuiElement
                    {
                        Name = "Cupboard_Status_Text",
                        Parent = "Option_Panel_Cupboard",

                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = ConvertBool(data.enableAuthorizeCupboards, player),
                                Align = TextAnchor.MiddleCenter
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = _middleAnchor,
                                AnchorMax = _middleAnchor,

                                OffsetMin = "-100 -15",
                                OffsetMax = "-10 15"
                            }
                        }
                    }
                });
            }
        }

        private void RenderFriends(BasePlayer player, CuiElementContainer mainGUI = null, int page = 1)
        {
            CuiHelper.DestroyUi(player, "Main_Friends");

            CuiHelper.AddUi(player, new List<CuiElement>
            {
                new CuiElement
                {
                    Name = "Main_Friends",
                    Parent = "Main_Panel",

                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(_config.gui.panelSecondColor)
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _middleAnchor,
                            AnchorMax = _middleAnchor,

                            OffsetMin = "-395 -245",
                            OffsetMax = "180 175"
                        }
                    }
                }
            });

            List<CuiElement> elements = new List<CuiElement>();

            var data = _data.GetPlayerData(player.userID);

            if (mainGUI != null)
            {
                foreach (var element in mainGUI)
                {
                    elements.Add(element);
                }
            }

            if (data.friends.Count != 0)
            {
                int currentIndex = -1;

                for (int i = 0; i < 10; i++)
                {
                    if (data.friends.Count <= i + (10 * (page - 1))) break;

                    var friendInfo = data.friends[i + (10 * (page - 1))];

                    currentIndex++;

                    var card = GetCard(player, $"-280 -{32.5 + (40 * currentIndex)}", $"280 -{12.5 + (40 * currentIndex)}", (currentIndex + 1 + (10 * (page - 1))).ToString(), friendInfo.name, friendInfo.id);

                    foreach (var element in card)
                    {
                        elements.Add(element);
                    }
                }
            }

            if (_viewingNow.ContainsKey(player.userID)) _viewingNow.Remove(player.userID);
            _viewingNow.Add(player.userID, page);

            CuiHelper.AddUi(player, elements);
        }

        private void RenderRequest(BasePlayer to, BasePlayer from)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "Friends_Accept",
                Parent = "Overlay",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.3114566 0.3114566 0.3114566 1"
                    },

                    _config.gui.panelRequest
                }
            });

            container.Add(new CuiElement
            {
                Name = "MainAccept_Header",
                Parent = "Friends_Accept",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.1943554 0.1943554 0.1943554 1"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.6",
                        AnchorMax = "0.5 0.6",

                        OffsetMin = "-110 -5",
                        OffsetMax = "110 23"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "MainAccept_Header_Text",
                Parent = "MainAccept_Header",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = string.Format(GetMessage("GUI_Request_Info", to), from.displayName),
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "MainAccept_Button_Accept",
                Parent = "Friends_Accept",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = $"friends accept",
                        Color = "0.2990798 0.4561759 0.2835915 1"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",

                        OffsetMin = "-110 -28",
                        OffsetMax = "0 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "MainAccept_Button_AcceptText",
                Parent = "MainAccept_Button_Accept",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Request_Accept", to),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "MainAccept_Button_NoAccept",
                Parent = "Friends_Accept",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = $"friends noaccept",
                        Color = "0.4956286 0.314086 0.314086 1"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",

                        OffsetMin = "0 -28",
                        OffsetMax = "110 1"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "MainAccept_Button_NoAcceptText",
                Parent = "MainAccept_Button_NoAccept",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Request_Decline", to),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            CuiHelper.AddUi(to, container);

            if (_pendingRequests.ContainsKey(to.userID)) _pendingRequests.Remove(to.userID);
            _pendingRequests.Add(to.userID, from.userID);
        }

        private void RenderNotice(BasePlayer player, string notice)
        {
            CuiHelper.DestroyUi(player, "Header_Notice_Text");

            CuiHelper.AddUi(player, new List<CuiElement>
            {
                new CuiElement
                {
                    Name = "Header_Notice_Text",
                    Parent = "Main_Header",

                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = notice,
                            Align = TextAnchor.MiddleRight
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _middleAnchor,
                            AnchorMax = _middleAnchor,

                            OffsetMin = "-245 -15",
                            OffsetMax = "345 15"
                        }
                    }
                }
            });
        }

        private CuiElementContainer GetCard(BasePlayer viewer, string offsetMin, string offsetMax, string number, string friendName, ulong friendID)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "Friend_Card",
                Parent = "Main_Friends",

                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(_config.gui.panelThirdColor)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.97",
                        AnchorMax = "0.5 1",

                        OffsetMin = offsetMin,
                        OffsetMax = offsetMax
                    }
                }
            });

            string circleColor = "1 1 1 1";

            if (BasePlayer.FindByID(friendID) != null) circleColor = "0.4078431 0.7607843 0.4588235 1";

            container.Add(new CuiElement
            {
                Name = "Card_OnlineIcon",
                Parent = "Friend_Card",

                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/circle_closed.png",
                        Color = circleColor
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-275 -12",
                        OffsetMax = "-250 12"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Card_Nickname",
                Parent = "Friend_Card",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"{number}. {friendName}",
                        Align = TextAnchor.MiddleLeft
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "-243 -12",
                        OffsetMax = "0 12"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Card_Button_Remove",
                Parent = "Friend_Card",

                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = HexToRustFormat(_config.gui.redButtonsColor),
                        Command = $"friends remove {friendID}"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = _middleAnchor,
                        AnchorMax = _middleAnchor,

                        OffsetMin = "210 -12",
                        OffsetMax = "275 12"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Button_Remove_Text",
                Parent = "Card_Button_Remove",

                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("GUI_Card_Button_Remove", viewer),
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            if (_config.friendButtons.Count != 0)
            {
                var first = _config.friendButtons[0];

                container.Add(new CuiElement
                {
                    Name = "Card_Button1",
                    Parent = "Friend_Card",

                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = HexToRustFormat(_config.gui.additionalButtonsColor),
                            Command = $"friends command {first.command.Replace("%FRIEND_ID%", friendID.ToString()).Replace("%FRIEND_NAME%", friendName)}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _middleAnchor,
                            AnchorMax = _middleAnchor,

                            OffsetMin = "125 -12",
                            OffsetMax = "200 12"
                        }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Card_Button1_Text",
                    Parent = "Card_Button1",

                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = first.name,
                            Align = TextAnchor.MiddleCenter
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });

                if (_config.friendButtons.Count >= 2)
                {
                    var second = _config.friendButtons[1];

                    container.Add(new CuiElement
                    {
                        Name = "Card_Button2",
                        Parent = "Friend_Card",

                        Components =
                        {
                            new CuiButtonComponent
                            {
                                Color = HexToRustFormat(_config.gui.additionalButtonsColor),
                                Command = $"friends command {second.command.Replace("%FRIEND_ID%", friendID.ToString()).Replace("%FRIEND_NAME%", friendName)}"
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = _middleAnchor,
                                AnchorMax = _middleAnchor,

                                OffsetMin = "40 -12",
                                OffsetMax = "115 12"
                            }
                        }
                    });

                    container.Add(new CuiElement
                    {
                        Name = "Card_Button2_Text",
                        Parent = "Card_Button2",

                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = second.name,
                                Align = TextAnchor.MiddleCenter
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });

                    if (_config.friendButtons.Count >= 3)
                    {
                        var third = _config.friendButtons[2];

                        container.Add(new CuiElement
                        {
                            Name = "Card_Button3",
                            Parent = "Friend_Card",

                            Components =
                            {
                                new CuiButtonComponent
                                {
                                    Command = $"friends command {third.command.Replace("%FRIEND_ID%", friendID.ToString()).Replace("%FRIEND_NAME%", friendName)}",
                                    Color = HexToRustFormat(_config.gui.additionalButtonsColor)
                                },

                                new CuiRectTransformComponent
                                {
                                    AnchorMin = _middleAnchor,
                                    AnchorMax = _middleAnchor,

                                    OffsetMin = "-45 -12",
                                    OffsetMax = "30 12"
                                }
                            }
                        });

                        container.Add(new CuiElement
                        {
                            Name = "Card_Button3_Text",
                            Parent = "Card_Button3",

                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = third.name,
                                    Align = TextAnchor.MiddleCenter
                                },

                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                }
            }

            return container;
        }

        #endregion

        #region Хуки

        private void OnNewSave()
        {
            foreach (var info in _data.playersData.Values)
            {
                info.entitiesTurret = new List<uint>();
                info.entitiesCupboard = new List<uint>();
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var baseEnt = go.GetComponent<BaseNetworkable>();

            if (baseEnt == null || plan?.GetOwnerPlayer() == null) return;

            if (!(baseEnt is BuildingPrivlidge) && !(baseEnt is AutoTurret)) return;

            var player = plan.GetOwnerPlayer();
            var data = _data.GetPlayerData(player.userID);

            if (baseEnt is BuildingPrivlidge)
            {
                var cupboard = baseEnt as BuildingPrivlidge;

                if (!data.entitiesCupboard.Contains(cupboard.net.ID))
                {
                    data.entitiesCupboard.Add(cupboard.net.ID);

                    _data.SetPlayerData(player.userID, data);

                    if (_config.logs.enableLogs)
                    {
                        if (_config.logs.logCupboardAdd)
                        {
                            LogToFile("Log", $"Игрок {player} установил шкаф (NET_ID: {baseEnt.net.ID}) | Шкафов: {data.entitiesCupboard.Count}", this);
                        }
                    }

                    foreach (var friend in data.friends)
                    {
                        BasePlayer friendPlayer = BasePlayer.FindByID(friend.id);


                        if (friendPlayer == null) continue;

                        if (Interface.CallHook("OnCupboardAuthorize", cupboard, friendPlayer) != null)
                        {
                            PrintWarning($"{friendPlayer} не был добавлен в шкаф {cupboard}, другой плагин отклонил авторизацию.");

                            continue;
                        }

                        cupboard.AddChild(friendPlayer);
                        cupboard.SendNetworkUpdateImmediate();
                    }
                }
            }

            if (baseEnt is AutoTurret)
            {
                var turret = baseEnt as AutoTurret;

                if (!data.entitiesTurret.Contains(turret.net.ID))
                {
                    data.entitiesTurret.Add(turret.net.ID);

                    _data.SetPlayerData(player.userID, data);

                    if (_config.logs.enableLogs)
                    {
                        if (_config.logs.logTurretsAdd)
                        {
                            LogToFile("Log", $"Игрок {player} установил турель (NET_ID: {baseEnt.net.ID}) | Шкафов: {data.entitiesTurret.Count}", this);
                        }
                    }

                    foreach (var friend in data.friends)
                    {
                        BasePlayer friendPlayer = BasePlayer.FindByID(friend.id);


                        if (friendPlayer == null) continue;

                        if (Interface.CallHook("OnTurretAuthorize", turret, friendPlayer) != null)
                        {
                            PrintWarning($"{friendPlayer} не был авторизован в турели {turret}, другой плагин отклонил авторизацию.");

                            continue;
                        }

                        turret.authorizedPlayers.RemoveAll(x => x.userid == friendPlayer.userID);
                        turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID()
                        {
                            userid = friendPlayer.userID,
                            username = friendPlayer.displayName
                        });

                        turret.SendNetworkUpdate();
                    }
                }
            }
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (player == null || baseLock == null) return null;

            if (baseLock.OwnerID != 0)
            {
                var data = _data.GetPlayerData(baseLock.OwnerID);

                if (IsFriend(player.userID, baseLock.OwnerID) && data.enableAuthorizeLocks)
                {
                    if (baseLock is CodeLock)
                    {
                        var codeLock = baseLock as CodeLock;

                        if (codeLock.effectUnlocked.isValid)
                        {
                            Effect.server.Run(codeLock.effectUnlocked.resourcePath, baseLock.transform.position);
                        }
                    }

                    return true;
                }
            }

            return null;
        }

        private void Loaded()
        {
            LoadData();

            foreach (var perm in _config.cooldowns.permissionCooldown.Keys)
            {
                permission.RegisterPermission(perm, this);
            }

            if (ImageLibrary != null && _config.gui.image.enableImage) ImageLibrary.Call("AddImage", _config.gui.image.imageUrl, "Friends_Background");
        }

        private void Unload()
        {
            BasePlayer.activePlayerList.ToList().ForEach(x => CuiHelper.DestroyUi(x, "Friends_Background"));
            _pendingRequests.ToList().ForEach(x => OnRequestIgnored(x.Key, x.Value));

            SaveData(true);
        }

        private void AuthFriend(ulong player, ulong friend)
        {
            var playerData = _data.GetPlayerData(player);

            var friendPlayer = BasePlayer.FindByID(friend);


            if (friendPlayer == null) return;

            if (playerData.enableAuthorizeCupboards)
            {
                foreach (var entID in playerData.entitiesCupboard)
                {
                    var ent = BaseNetworkable.serverEntities.Find(entID);

                    if (ent != null)
                    {
                        var cupboard = ent as BuildingPrivlidge;

                        if (Interface.CallHook("OnCupboardAuthorize", cupboard, friendPlayer) != null)
                        {
                            PrintWarning($"{friendPlayer} не был добавлен в шкаф {cupboard}, другой плагин отклонил авторизацию.");

                            continue;
                        }

                        cupboard.AddChild(friendPlayer);
                        cupboard.SendNetworkUpdateImmediate();
                    }
                }
            }

            if (playerData.enableAuthorizeTurrets)
            {
                foreach (var entID in playerData.entitiesTurret)
                {
                    var ent = BaseNetworkable.serverEntities.Find(entID);

                    if (ent != null)
                    {
                        var turret = ent as AutoTurret;

                        if (Interface.CallHook("OnTurretAuthorize", turret, friendPlayer) != null)
                        {
                            PrintWarning($"{friendPlayer} не был авторизован в турели {turret}, другой плагин отклонил авторизацию.");

                            continue;
                        }

                        turret.authorizedPlayers.RemoveAll(x => x.userid == friendPlayer.userID);
                        turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID()
                        {
                            userid = friendPlayer.userID,
                            username = friendPlayer.displayName
                        });

                        turret.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

        private void DeAuthFriend(ulong player, ulong friend)
        {
            var playerData = _data.GetPlayerData(player);

            foreach (var entID in playerData.entitiesCupboard)
            {
                var ent = BaseNetworkable.serverEntities.Find(entID);

                if (ent != null)
                {
                    var cupboard = ent as BuildingPrivlidge;

                    cupboard.authorizedPlayers.RemoveAll(x => x.userid == friend);

                    cupboard.SendNetworkUpdate();
                }
            }

            foreach (var entID in playerData.entitiesTurret)
            {
                var ent = BaseNetworkable.serverEntities.Find(entID);

                if (ent != null)
                {
                    var turret = ent as AutoTurret;

                    turret.authorizedPlayers.RemoveAll(x => x.userid == friend);

                    turret.SendNetworkUpdate();
                }
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info?.InitiatorPlayer == null || entity == null) return null;

            if (entity is BasePlayer && info.InitiatorPlayer.userID.IsSteamId() && !info.InitiatorPlayer.IsNpc)
            {
                var target = entity.ToPlayer();
                var data = _data.GetPlayerData(info.InitiatorPlayer.userID);

                if (!data.enableFF)
                {
                    if (IsFriend(target.userID, info.InitiatorPlayer.userID))
                    {
                        return false;
                    }
                }
            }

            return null;
        }

        private void OnRequestIgnored(ulong from, ulong to)
        {
            BasePlayer target = BasePlayer.FindByID(to);
            BasePlayer sender = BasePlayer.FindByID(from);

            if (target == null || sender == null) return;

            if (_pendingRequests.ContainsKey(from)) _pendingRequests.Remove(from);
            else return;

            if (AreFriends(target.userID, sender.userID)) return;

            target.ChatMessage(GetMessage("Notice_RequestOut_Target", target));
            sender.ChatMessage(string.Format(GetMessage("Notice_RequestOut_Sender", sender), target.displayName));

            CuiHelper.DestroyUi(target, "Friends_Accept");

            if (_config.logs.enableLogs)
            {
                if (_config.logs.logIgnoreRequest)
                {
                    LogToFile("Log", $"Игрок {target} проигнорировал запрос в друзья от игрока {sender}", this);
                }
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            var found = _data.playersData.Where(x => x.Value.friends.Where(y => y.id == player.userID && y.name != player.displayName)?.Count() != 0);

            if (found?.Count() != 0)
            {
                var foundList = found.ToList();

                foreach (var obj in foundList)
                {
                    obj.Value.friends.RemoveAll(x => x.id == player.userID);
                    obj.Value.friends.Add(new DataFile.PlayerData.FriendInfo
                    {
                        id = player.userID,
                        name = player.displayName
                    });

                    _data.SetPlayerData(obj.Key, obj.Value);
                }
            }
        }

        #endregion

        #region API

        private void API_OnFriendAdded(ulong player, ulong friend) => Interface.Oxide.CallHook("OnFriendAdded", player, friend);

        private void API_OnFriendRemoved(ulong player, ulong friend) => Interface.Oxide.CallHook("OnFriendRemoved", player, friend);

        private bool HasFriend(ulong player, ulong friend) => _data.GetPlayerData(player).friends.Where(x => x.id == friend)?.Count() != 0;
        private bool HasFriend(string player, string friend)
        {
            ulong playerID, friendID;

            if (ulong.TryParse(player, out playerID) && ulong.TryParse(friend, out friendID))
            {
                return HasFriend(playerID, friendID);
            }

            return false;
        }

        private bool AreFriends(ulong player, ulong friend) => HasFriend(player, friend);
        private bool AreFriends(string player, string friend)
        {
            ulong playerID, friendID;

            if (ulong.TryParse(player, out playerID) && ulong.TryParse(friend, out friendID))
            {
                return HasFriend(playerID, friendID);
            }

            return false;
        }

        private bool IsFriend(ulong player, ulong friend) => HasFriend(player, friend);
        private bool IsFriend(string player, string friend)
        {
            ulong playerID, friendID;

            if (ulong.TryParse(player, out playerID) && ulong.TryParse(friend, out friendID))
            {
                return HasFriend(playerID, friendID);
            }

            return false;
        }

        private ulong[] GetFriends(ulong player)
        {
            List<ulong> friends = new List<ulong>();

            foreach (var friend in _data.GetPlayerData(player).friends)
            {
                friends.Add(friend.id);
            }

            return friends.ToArray();
        }

        private string[] GetFriends(string player)
        {
            ulong playerID;

            if (ulong.TryParse(player, out playerID))
            {
                List<string> friends = new List<string>();

                foreach (var friend in _data.GetPlayerData(playerID).friends)
                {
                    friends.Add(friend.id.ToString());
                }

                return friends.ToArray();
            }

            return new string[] { };
        }

        private ulong[] GetFriendList(ulong player) => GetFriends(player);
        private string[] GetFriendList(string player) => GetFriends(player);

        private int GetMaxFriends() => _config.maxFriends;

        #endregion

        #region Вспомогательный класс

        private class PlayerPendingRequest : MonoBehaviour
        {
            public ulong from, to;
            public float cooldownTime;

            private void Update()
            {
                cooldownTime -= Time.deltaTime;

                if (cooldownTime <= 0)
                {
                    Interface.Oxide.CallHook("OnRequestIgnored", from, to);
                    UnityEngine.Object.Destroy(this);
                }
            }
        }

        #endregion
    }
}