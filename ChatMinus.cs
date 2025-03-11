using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ChatMinus", "Хомячок", "2.1")]
    [Description("Отправка и просмотр ЛС, тикетов админам и модераторам")]
    public class ChatMinus : RustPlugin
    {
        #region Virables
        
        private List<BasePlayer> _activeGui = new List<BasePlayer>();
        private List<BasePlayer> _activeFind = new List<BasePlayer>();
        //private Dictionary<string, Dictionary<BasePlayer, BasePlayer>> _activeCuiAll = new Dictionary<string, Dictionary<BasePlayer, BasePlayer>>
        //{
        //   { "_activeGuiModer",  new Dictionary<BasePlayer, BasePlayer>()},
        //    { "_activeGuiAdmin", new Dictionary<BasePlayer, BasePlayer>()},
        //    { "_activeGuiPrivate", new Dictionary<BasePlayer, BasePlayer>()},
        //    { "_activeMess", new Dictionary<BasePlayer, BasePlayer>()}
        //};
        private Dictionary<BasePlayer, BasePlayer> _activeGuiModer = new Dictionary<BasePlayer, BasePlayer>();
        private Dictionary<BasePlayer, BasePlayer> _activeGuiAdmin = new Dictionary<BasePlayer, BasePlayer>();
        private Dictionary<BasePlayer, BasePlayer> _activeGuiPrivate = new Dictionary<BasePlayer, BasePlayer>();
        private Dictionary<BasePlayer, BasePlayer> _activeMess = new Dictionary<BasePlayer, BasePlayer>();
        private Dictionary<BasePlayer, string> _string = new Dictionary<BasePlayer, string>();

        private const string PermModer = "chatminus.moder";
        private const string PermAdmin = "chatminus.admin";
        private const string UiElement = "MainUi";
        private const string UiInput = "UiInput";
        private const string UiInputFind = "UiInputFind";
        private const string UiMess = "UiMess";
        private const string UiPlayers = "UiPlayers";
        private const string UiNotiсe = "UiRemove";
        private const string Sound = "assets/prefabs/npc/scientist/sound/chatter.prefab";
        
        #endregion

        #region Commands

        [ChatCommand("cm")]
        private void CmdChatGui(BasePlayer player, string command, string[] args)
        {
            if (_activeGui.Contains(player))
            {
                DestroyGuiAll(player);
                if (_activeGuiModer.ContainsKey(player)) _activeGuiModer.Remove(player);
                if (_activeGuiAdmin.ContainsKey(player)) _activeGuiAdmin.Remove(player);
                if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
                _activeGui.Remove(player);
            }
            else
            {
                _activeGui.Add(player);
                CreateGui(player, player);
                if (!_activeMess.ContainsKey(player)) return;
                var target = _activeMess[player];
                player.SendConsoleCommand($"cm.openinputmenu {target.userID} 3");
                //InputGui(player, target.userID, 3);
                //MessageList(player, target, 3);
                //if (!_activeGuiPrivate.ContainsKey(player))
                //    _activeGuiPrivate.Add(player, target);
                //if (!_string.ContainsKey(player)) return;
                //_string.Remove(player);
            }
        }
        
        [ConsoleCommand("cm.change")]
        private void CmdChatMinusChange(ConsoleSystem.Arg arg)
        {
/*            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            
            var hum = Convert.ToUInt64(arg.Args[0]); 
            var target = GetPlayer(hum);

            DestroyGuiNoOne(player);
            var num = _data.PlayersSetting[player.userID].StatusMenu;
            if (num == 0)
            {
                _data.PlayersSetting[player.userID].StatusMenu = 1;
                SaveData();
                CreateGui(player, target);
            }
            else
            {
                _data.PlayersSetting[player.userID].StatusMenu = 0;
                SaveData();
                CreateGui(player, target);
                if (player != target) MessageList(player, target, 3);
                if (_activeGuiAdmin.ContainsKey(player)) MessageList(player, target, 1);
                if (_activeGuiModer.ContainsKey(player)) MessageList(player, target, 2);
            }*/
        }
        
        [ConsoleCommand("cm.opendopmenu")]
        private void CmdChatMinusMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            switch (arg.Args[0])
            {
                case "admin":
                    if (AllowAdmin(player))
                    {
                        player.Command($"cm.filtercharsadmincharf {int.Parse(arg.Args[1])} ~");
                        if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
                        if (_activeGuiModer.ContainsKey(player)) _activeGuiModer.Remove(player);
                    }
                    else
                    {
                        if (_activeFind.Contains(player))
                        {
                            _activeFind.Remove(player);
                            DestroyGuiNoOne(player);
                        }
                        InputGui(player, player.userID, 1);
                        MessageList(player, player, 1);
                        if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
                        if (_activeGuiModer.ContainsKey(player)) _activeGuiModer.Remove(player);
                        if (!_activeGuiAdmin.ContainsKey(player))_activeGuiAdmin.Add(player, player);
                    }
                    return;
                case "moder":
                    if (AllowModer(player))
                    {
                        player.Command($"cm.filtercharsmodercharf {int.Parse(arg.Args[1])} ~");
                        if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
                        if (_activeGuiAdmin.ContainsKey(player)) _activeGuiAdmin.Remove(player);
                    }
                    else
                    {
                        if (_activeFind.Contains(player))
                        {
                            _activeFind.Remove(player);
                            DestroyGuiNoOne(player);
                        }
                        InputGui(player, player.userID, 2);
                        MessageList(player, player, 2);
                        if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
                        if (_activeGuiAdmin.ContainsKey(player)) _activeGuiAdmin.Remove(player);
                        if (!_activeGuiModer.ContainsKey(player)) _activeGuiModer.Add(player, player);
                    }
                    return;
                case "players":
                    DestroyGui(player, UiInput);
                    player.Command($"cm.filtercharsplayerscharf {int.Parse(arg.Args[1])} ~");
                    if (_activeGuiModer.ContainsKey(player)) _activeGuiModer.Remove(player);
                    if (_activeGuiAdmin.ContainsKey(player)) _activeGuiAdmin.Remove(player);
                    return;
            }
        }
        
        [ConsoleCommand("cm.openinputmenu")]
        private void CmdAnswer(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            
            var hum = Convert.ToUInt64(arg.Args[0]); 
            var target = GetPlayer(hum);
            
            if (_activeFind.Contains(player))_activeFind.Remove(player);

            DestroyGuiNoOne(player);
            CreateGui(player, target);

            switch (arg.Args[1])
            {
                case "1":
                    InputGui(player, target.userID, 1);
                    MessageList(player, target, 1);
                    if (!_activeGuiAdmin.ContainsKey(player)) 
                        _activeGuiAdmin.Add(player, target);
                    if (!_string.ContainsKey(player)) return;
                    _string.Remove(player);
                    return;
                case "2":
                    InputGui(player, target.userID, 2);
                    MessageList(player, target, 2);
                    if (!(_activeGuiModer).ContainsKey(player)) 
                        _activeGuiModer.Add(player, target);
                    if (!_string.ContainsKey(player)) return;
                    _string.Remove(player);
                    return;
                case "3":
                    InputGui(player, target.userID, 3);
                    MessageList(player, target, 3);
                    if (!_activeGuiPrivate.ContainsKey(player)) 
                        _activeGuiPrivate.Add(player, target);
                    if (!_activeMess.ContainsKey(player))
                        _activeMess.Add(player, target);
                    _activeMess[player] = target;
                    if (!_string.ContainsKey(player)) return;
                    _string.Remove(player);
                    return;
            }
            
            _activeFind.Remove(player);
        }
        
        [ConsoleCommand("cm.filtercharsadmininput")]
        private void CmdFilterCharsAdmin(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!AllowAdmin(player)) return;

            var playerList = new Dictionary<string, ulong>();

            var argList = arg.Args.ToList();
            argList.RemoveAt(1);
            var message = string.Join(" ", argList.ToArray());

            if (!_string.ContainsKey(player)) _string.Add(player, "");
            _string[player] = message;
            if (_string[player] == "") return;
            var name = arg.Args[1];
            foreach (var target in _data.Players)
            {
                if (target.Value == player.userID) continue;
                if (target.Key.StartsWith(name.ToUpper()) || target.Key.StartsWith(name.ToLower()))
                {
                    playerList.Add(target.Key, target.Value);
                }
            }
            DestroyGui(player, UiPlayers);
            DestroyGui(player, UiMess);
            var numList = int.Parse(arg.Args[2]);
            ShowPlayers(player, playerList, "input", 1, numList);
            _activeFind.Add(player);
            if (!_string.ContainsKey(player)) return;
            _string.Remove(player);
        }

        [ConsoleCommand("cm.filtercharsadmincharf")]
        private void CmdFilterCharsAdminCh(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!AllowAdmin(player)) return;

            var playerList = new Dictionary<string, ulong>();

            var numList = int.Parse(arg.Args[0]);
            var charF = arg.Args[1];
            if (charF == "~")
            {
                foreach (var target in BasePlayer.activePlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (target == player) continue;
                    if (AllowAdmin(target)) continue;
                    if (!_data.PlayerMess.ContainsKey(target.userID)) continue;
                    if (_data.PlayerMess[target.userID].CountAdminMess > 0)
                        playerList.Add(target.displayName, target.userID);
                }
                DestroyGui(player, UiPlayers);
                DestroyGui(player, UiMess);
                ShowPlayers(player, playerList, "charf", 1, numList, charF);
                _activeFind.Add(player);
            }
            else
            {
                foreach (var target in BasePlayer.activePlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (target != player && !AllowAdmin(target))
                    {
                        if (target.displayName.StartsWith(charF.ToUpper()) ||
                            target.displayName.StartsWith(charF.ToLower()))
                            if (!playerList.ContainsKey(target.displayName))
                                playerList.Add(target.displayName, target.userID);
                    }
                }

                foreach (var target in BasePlayer.sleepingPlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (!AllowAdmin(target))
                    {
                        if (target.displayName.StartsWith(charF.ToUpper()) ||
                            target.displayName.StartsWith(charF.ToLower()))
                            if (!playerList.ContainsKey(target.displayName))
                                playerList.Add(target.displayName, target.userID);
                    }
                }

                DestroyGui(player, UiPlayers);
                DestroyGui(player, UiMess);
                ShowPlayers(player, playerList, "charf", 1, numList, charF);
                _activeFind.Add(player);
            }

            if (!_string.ContainsKey(player)) return;
            _activeFind.Add(player);
            _string.Remove(player);
        }

        [ConsoleCommand("cm.filtercharsmoderinput")]
        private void CmdFilterCharsModer(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!AllowModer(player)) return;
            var playerList = new Dictionary<string, ulong>();

            var argList = arg.Args.ToList();
            argList.RemoveAt(1);
            var message = string.Join(" ", argList.ToArray());

            if (!_string.ContainsKey(player)) _string.Add(player, "");
            _string[player] = message;
            if (_string[player] == "") return;
            var name = arg.Args[1];
            foreach (var target in _data.Players)
            {
                if (target.Value == player.userID) continue;
                if (target.Key.StartsWith(name.ToUpper()) || target.Key.StartsWith(name.ToLower()))
                {
                    playerList.Add(target.Key, target.Value);
                }
            }
            DestroyGui(player, UiPlayers);
            DestroyGui(player, UiMess);
            var numList = int.Parse(arg.Args[2]);
            ShowPlayers(player,playerList, "input", 2, numList);
            _activeFind.Add(player);
            if (!_string.ContainsKey(player)) return;
            _string.Remove(player);
        }

        [ConsoleCommand("cm.filtercharsmodercharf")]
        private void CmdFilterCharsModerCh(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!AllowModer(player)) return;
            var playerList = new Dictionary<string, ulong>();

            var numList = int.Parse(arg.Args[0]);
            var charF = arg.Args[1];
            if (charF == "~")
            {
                foreach (var target in BasePlayer.activePlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (target == player) continue;
                    if (AllowModer(target)) continue;
                    if (!_data.PlayerMess.ContainsKey(target.userID)) continue;
                    if (_data.PlayerMess[target.userID].CountModerMess > 0)
                        playerList.Add(target.displayName, target.userID);
                }
                DestroyGui(player, UiPlayers);
                DestroyGui(player, UiMess);
                ShowPlayers(player, playerList, "charf", 2, numList, charF);
                _activeFind.Add(player);
            }
            else
            {
                foreach (var target in BasePlayer.activePlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (target != player && !AllowModer(target) && !AllowAdmin(target))
                    {
                        if (target.displayName.StartsWith(charF.ToUpper()) ||
                            target.displayName.StartsWith(charF.ToLower()))
                                if (!playerList.ContainsKey(target.displayName))
                                    playerList.Add(target.displayName, target.userID);
                    }
                }

                foreach (var target in BasePlayer.sleepingPlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (!AllowModer(target) && !AllowAdmin(target))
                    {
                        if (target.displayName.StartsWith(charF.ToUpper()) ||
                            target.displayName.StartsWith(charF.ToLower()))
                            if (target != player)
                                if (!playerList.ContainsKey(target.displayName))
                                    playerList.Add(target.displayName, target.userID);
                    }
                }

                DestroyGui(player, UiPlayers);
                DestroyGui(player, UiMess);
                ShowPlayers(player, playerList, "charf", 2, numList, charF);
                _activeFind.Add(player);
            }

            if (!_string.ContainsKey(player)) return;
            _activeFind.Add(player);
            _string.Remove(player);
        }

        [ConsoleCommand("cm.filtercharsplayersinput")]
        private void CmdFilterCharsPlayers(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            
            var playerList = new Dictionary<string, ulong>();
            var argList = arg.Args.ToList();
            argList.RemoveAt(1);
            var message = string.Join(" ", argList.ToArray());

            if (!_string.ContainsKey(player)) _string.Add(player, "");
            _string[player] = message;
            if (_string[player] == "") return;
            var name = arg.Args[1];
            if (_data.Players == null) return;
            foreach (var target in _data.Players)
            {
                if (target.Value == player.userID) continue;
                if (target.Key.StartsWith(name.ToUpper()) || target.Key.StartsWith(name.ToLower()))
                {
                    playerList.Add(target.Key, target.Value);
                }
            }
            DestroyGui(player, UiPlayers);
            DestroyGui(player, UiMess);
            var numList = int.Parse(arg.Args[2]);
            ShowPlayers(player, playerList, "input", 3, numList);
            _activeFind.Add(player);
            if (!_string.ContainsKey(player)) return;
            _string.Remove(player);
        }

        [ConsoleCommand("cm.filtercharsplayerscharf")]
        private void CmdFilterCharsPlayersCh(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            var playerList = new Dictionary<string, ulong>();

            var numList1 = int.Parse(arg.Args[0]);
            var charF = arg.Args[1];
            if (charF == "~")
            {
                foreach (var target in BasePlayer.activePlayerList)
                {
                    if (target != player)
                        playerList.Add(target.displayName, target.userID);
                }
                //foreach (var target in BasePlayer.sleepingPlayerList)
                //{
                //    if (IsNpc(target)) continue;
                //    if (target != player)
                //        if (!playerList.ContainsKey(target.displayName))
                //            playerList.Add(target.displayName, target.userID);
                //}
                DestroyGui(player, UiPlayers);
                DestroyGui(player, UiMess);

                ShowPlayers(player, playerList, "charf", 3, numList1, charF);
                _activeFind.Add(player);
            }
            else
            {
                foreach (var target in _data.Players)
                {
                    if (target.Value != player.userID)
                    {
                        if (target.Key.StartsWith(charF.ToUpper()) ||
                            target.Key.StartsWith(charF.ToLower()))
                            if (!playerList.ContainsKey(target.Key))
                                playerList.Add(target.Key, target.Value);
                    }
                }

                foreach (var target in BasePlayer.sleepingPlayerList)
                {
                    if (IsNpc(target)) continue;
                    if (target.displayName.StartsWith(charF.ToUpper()) ||
                        target.displayName.StartsWith(charF.ToLower()))
                        if(!playerList.ContainsKey(target.displayName))
                            playerList.Add(target.displayName, target.userID);
                }

                DestroyGui(player, UiPlayers);
                DestroyGui(player, UiMess);
                ShowPlayers(player, playerList, $"charf", 3, numList1, charF);
                _activeFind.Add(player);
            }

            if (!_string.ContainsKey(player)) return;
            _activeFind.Add(player);
            _string.Remove(player);
        }

        [ConsoleCommand("cm.sendchatminus")]
        private void CmdChatMinusSendMessage(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            BasePlayer target;
            
            if (!_string.ContainsKey(player)) _string.Add(player, "");

            switch (arg.Args[0])
            {
                case "input":
                    var argList = arg.Args.ToList();
                    argList.RemoveAt(0);
                    var message = string.Join(" ", argList.ToArray());
            
                    if (!_string.ContainsKey(player)) _string.Add(player, "");
                    _string[player] = message;
                    return;
                case "playermessage": // игрок игроку
                    target = GetPlayer(Convert.ToUInt64(arg.Args[1]));
                    if (!CheckMessage(_string[player]))
                    {
                        Msg(player, lang.GetMessage("MSG_ERROR", this, player.UserIDString));
                        return;
                    }
                    _data.PlayerMess[player.userID].PrivateMess[target.userID].Add(DateTime.Now, ($"<b><color=#FFFFFF>  {player.displayName}</color></b> <size=10>({DateTime.Now})</size>\n" + _string[player]));
                    SaveData();
                    DestroyGuiNoOne(player);
                    InputGui(player, target.userID, 3);
                    MessageList(player, target, 3);
                    if (_config.EnableSound) Effect.server.Run(_config.Sound, target, 0, Vector3.zero, Vector3.zero);
                    //Puts($"Игрок {player.displayName} для {target.displayName}: {_string[player]}");
                    SendUpdateMessages(player, target, 1);
                    return;
                case "adminmessage": // игрок админу
                    if (!CheckMessage(_string[player])) 
                    {
                        Msg(player, lang.GetMessage("MSG_ERROR", this, player.UserIDString));
                        return;
                    }
                    _data.PlayerMess[player.userID].AdminMess.Add(DateTime.Now, ($"<b><color=#ffffff>  {player.displayName}</color></b> <size=10>({DateTime.Now})</size>\n" + _string[player]));
					_data.PlayerMess[player.userID].CountAdminMess += 1;
                    SaveData();
                    DestroyGuiNoOne(player);
                    InputGui(player, player.userID, 1);
                    MessageList(player, player, 1);
                    //Puts($"Игрок {player.displayName} АДМИНУ: {_string[player]}");
                    SendUpdateMessages(player, player, 2);
                    return;
                case "adminanswer": // админ игроку
                    target = GetPlayer(Convert.ToUInt64(arg.Args[1]));
                    if (!CheckMessage(_string[player])) 
                    {
                        Msg(player, lang.GetMessage("MSG_ERROR", this, player.UserIDString));
                        return;
                    }
                    _data.PlayerMess[target.userID].AdminMess.Add(DateTime.Now, ($"<b><color=#ffffff>ADMIN</color></b> <size=10>({DateTime.Now})</size>\n" + _string[player]));
					_data.PlayerMess[target.userID].CountAdminMess += 1;
                    SaveData();
                    DestroyGuiNoOne(player);
                    InputGui(player, target.userID, 1);
                    MessageList(player, target, 1);
                    if (_config.EnableSound) Effect.server.Run(_config.Sound, target, 0, Vector3.zero, Vector3.zero);
                    //Puts($"АДМИН {player.displayName} ИГРОКУ {target.displayName}: {_string[player]}");
                    SendUpdateMessages(player, target, 3);
                    return;
                case "modermessage": // игрок модеру
                    if (!CheckMessage(_string[player])) 
                    {
                        Msg(player, lang.GetMessage("MSG_ERROR", this, player.UserIDString));
                        return;
                    }
                    _data.PlayerMess[player.userID].ModerMess.Add(DateTime.Now, ($"<b><color=#ffffff>  {player.displayName}</color></b> <size=10>({DateTime.Now})</size>\n" + _string[player]));
					_data.PlayerMess[player.userID].CountModerMess += 1;
                    SaveData();
                    DestroyGuiNoOne(player);
                    InputGui(player, player.userID, 2);
                    MessageList(player, player, 2);
                    //Puts($"Игрок {player.displayName} МОДЕРУ: {_string[player]}");
                    SendUpdateMessages(player, player, 4);
                    return;
                case "moderanswer": // модер игроку
                    target = GetPlayer(Convert.ToUInt64(arg.Args[1]));
                    if (!CheckMessage(_string[player])) 
                    {
                        Msg(player, lang.GetMessage("MSG_ERROR", this, player.UserIDString));
                        return;
                    }
                    _data.PlayerMess[target.userID].ModerMess.Add(DateTime.Now, ($"<b><color=#ffffff>MODERATOR</color></b> <size=10>({DateTime.Now})</size>\n" + _string[player]));
					_data.PlayerMess[target.userID].CountModerMess += 1;
                    SaveData();
                    DestroyGuiNoOne(player);
                    InputGui(player, target.userID, 2);
                    MessageList(player, target, 2);
                    if (_config.EnableSound) Effect.server.Run(_config.Sound, target, 0, Vector3.zero, Vector3.zero);
                    //Puts($"МОДЕР {player.displayName} ИГРОКУ {target.displayName}: {_string[player]}");
                    SendUpdateMessages(player, target, 5);
                    return;
                case "removeamdmin": // админ удаляет
                    target = GetPlayer(Convert.ToUInt64(arg.Args[1]));
                    if (_data.PlayerMess[target.userID].AdminMess.Count > 0) _data.PlayerMess[target.userID].AdminMess.Clear();
					_data.PlayerMess[target.userID].CountAdminMess = 0;
                    SaveData();
                    player.Command("cm.sendchatminus close");
                    target.Command("cm.sendchatminus close");
                    Msg(player, lang.GetMessage("TICKET_DELETE", this, player.UserIDString));
                    Msg(target, lang.GetMessage("TICKET_DELETE", this, target.UserIDString));
                    return;
                case "removemoder": // модер удаляет
                    target = GetPlayer(Convert.ToUInt64(arg.Args[1]));
                    if (_data.PlayerMess[target.userID].ModerMess.Count > 0) _data.PlayerMess[target.userID].ModerMess.Clear();
					_data.PlayerMess[target.userID].CountModerMess = 0;
                    SaveData();
                    player.Command("cm.sendchatminus close");
                    target.Command("cm.sendchatminus close");
                    Msg(player, lang.GetMessage("TICKET_DELETE", this, player.UserIDString));
                    Msg(target, lang.GetMessage("TICKET_DELETE", this, target.UserIDString));
                    return;
                case "removeplayeramdmin": // игрок удаляет сообщения для админов
                    if (_data.PlayerMess[player.userID].AdminMess.Count > 1) _data.PlayerMess[player.userID].AdminMess.Clear();
					_data.PlayerMess[player.userID].CountAdminMess = 0;
                    SaveData();
                    player.Command("cm.sendchatminus close");
                    Msg(player, lang.GetMessage("TICKET_DELETE", this, player.UserIDString));
                    SendUpdateMessages(player, player, 6);
                    return;
                case "removeplayermoder": // игрок удаляет сообщения для модеров
                    if (_data.PlayerMess[player.userID].ModerMess.Count > 1) _data.PlayerMess[player.userID].ModerMess.Clear();
					_data.PlayerMess[player.userID].CountModerMess = 0;
                    SaveData();
                    player.Command("cm.sendchatminus close");
                    Msg(player, lang.GetMessage("TICKET_DELETE", this, player.UserIDString));
                    SendUpdateMessages(player, player, 7);
                    return;
                case "close":
                    DestroyGuiAll(player);
                    if (_activeGuiModer.ContainsKey(player)) _activeGuiModer.Remove(player);
                    if (_activeGuiAdmin.ContainsKey(player)) _activeGuiAdmin.Remove(player);
                    if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
                    if (_activeFind.Contains(player))_activeFind.Remove(player);
                    _activeGui.Remove(player);
                    return;
            }
        }

        #endregion

        #region Cui

        private static void CreateGui(BasePlayer player, BasePlayer target)
        {
            //var num = _data.PlayersSetting[player.userID].StatusMenu;
            var startOne = "0.825 0.3";
            var lastOne = "0.9975 0.9";
/*            var  name = ">";
            switch (num)
            {
                case 1:
                    startOne = "0.9975 0.3";
                    lastOne = "1.17 0.9";
                    name = "<b><</b>";
                    break;
                case 0:
                    startOne = "0.825 0.3";
                    lastOne = "0.9975 0.9";
                    name = "<b>></b>";
                    break;
            }*/

            var container = Ui.Container(UiElement, Ui.Color("#000000", 0f), startOne, lastOne); 
            Ui.Panel(ref container, UiElement, Ui.Color("#808080", 0.3f), "0 0.074", "1 0.924");
            
            Ui.Panel(ref container, UiElement, Ui.Color("#808080", 0.5f), "0 0.931", "1 1");
            Ui.Label(ref container, UiElement, $"<b>{target.displayName}</b>", Ui.Color("#FFFFFF", 1f), 16, "0 0.931", "1 1", 0);
            
            Ui.Panel(ref container, UiElement, Ui.Color("#808080", 0f), "0 0", "1 0.069");
            Ui.Button(ref container, UiElement, Ui.Color("#B67A7F", 0.8f), "<b>ADMIN</b>", "1 1 1 0.8", 14, "0 0", "0.32 0.065", $"cm.opendopmenu admin 0");
            Ui.Button(ref container, UiElement, Ui.Color("#81b67a", 0.8f), "<b>MODER</b>", "1 1 1 0.8", 14, "0.34 0", "0.66 0.065", $"cm.opendopmenu moder 0");
            Ui.Button(ref container, UiElement, Ui.Color("#808080", 0.6f), "<b>PLAYERS</b>", "1 1 1 0.8", 14, "0.68 0", "1 0.065", $"cm.opendopmenu players 0");
            
            //Ui.Button(ref container, UiElement, Ui.Color("#808080", 0.6f), name, "1 1 1 0.8", 16, "-0.07 0.46", "0 0.54", $"cm.change {target.userID}");

            CuiHelper.DestroyUi(player, UiElement);
            CuiHelper.AddUi(player, container);
        }
        
        private void InputGui(BasePlayer player, ulong targetUlong, int num)
        {
            var container = Ui.Container(UiInput, Ui.Color("#808080", 0f), "0.0128 0.025", "0.3077 0.124");
            
            Ui.Panel(ref container, UiInput, Ui.Color("#ff7f50", 0.7f), "0 0.57", "1 0.94", 0.5f);
            Ui.Input(ref container, UiInput, Ui.Color("#ffffff", 1f), "input ", 18, $"cm.sendchatminus ", "0.02 0.57", "0.98 0.94");
            
            Ui.Panel(ref container, UiInput, Ui.Color("#808080", 0.4f), "0.51 0.14", "0.75 0.51");
            Ui.Button(ref container, UiInput, Ui.Color("#FF6347", 0.7f), "<b>CLOSE</b>", Ui.Color("#FFFFFF", 1f), 14, "0.51 0.14", "0.75 0.51", $"cm.sendchatminus close");
            Ui.Panel(ref container, UiInput, Ui.Color("#808080", 0.4f), "0.76 0.14", "1 0.51");

            switch (num)
            {
                    case 1:
                        if (AllowAdmin(player))
                        {
                            Ui.Panel(ref container, UiInput, Ui.Color("#808080", 0.4f), "0.26 0.14", "0.5 0.51");
                            Ui.Button(ref container, UiInput, Ui.Color("#808080", 0.7f), "<b>REMOVE</b>", Ui.Color("#FFFFFF", 1f), 14, "0.26 0.14", "0.5 0.51", $"cm.sendchatminus removeamdmin {targetUlong}");
                            Ui.Button(ref container, UiInput, Ui.Color("#9acd32", 0.7f), "<b>SEND</b>", Ui.Color("#FFFFFF", 1f), 14, "0.76 0.14", "1 0.51", $"cm.sendchatminus adminanswer {targetUlong}");
                        }
                        else
                        {
                            Ui.Panel(ref container, UiInput, Ui.Color("#808080", 0.4f), "0.26 0.14", "0.5 0.51");
                            Ui.Button(ref container, UiInput, Ui.Color("#808080", 0.7f), "<b>REMOVE</b>", Ui.Color("#FFFFFF", 1f), 14, "0.26 0.14", "0.5 0.51", $"cm.sendchatminus removeplayeramdmin");
                            Ui.Button(ref container, UiInput, Ui.Color("#9acd32", 0.7f), "<b>SEND</b>", Ui.Color("#FFFFFF", 1f), 14, "0.76 0.14", "1 0.51", $"cm.sendchatminus adminmessage");
                        }
                        
                        CuiHelper.DestroyUi(player, UiInput);
                        CuiHelper.AddUi(player, container);
                        return;
                    case 2:
                        if (AllowModer(player))
                        {
                            Ui.Panel(ref container, UiInput, Ui.Color("#808080", 0.4f), "0.26 0.14", "0.5 0.51");
                            Ui.Button(ref container, UiInput, Ui.Color("#808080", 0.7f), "<b>REMOVE</b>", Ui.Color("#FFFFFF", 1f), 14, "0.26 0.14", "0.5 0.51", $"cm.sendchatminus removemoder {targetUlong}");
                            Ui.Button(ref container, UiInput, Ui.Color("#9acd32", 0.7f), "<b>SEND</b>", Ui.Color("#FFFFFF", 1f), 14, "0.76 0.14", "1 0.51", $"cm.sendchatminus moderanswer {targetUlong}");
                        }
                        else
                        {
                            Ui.Panel(ref container, UiInput, Ui.Color("#808080", 0.4f), "0.26 0.14", "0.5 0.51");
                            Ui.Button(ref container, UiInput, Ui.Color("#808080", 0.7f), "<b>REMOVE</b>", Ui.Color("#FFFFFF", 1f), 14, "0.26 0.14", "0.5 0.51", $"cm.sendchatminus removeplayermoder");
                            Ui.Button(ref container, UiInput, Ui.Color("#9acd32", 0.7f), "<b>SEND</b>", Ui.Color("#FFFFFF", 1f), 14, "0.76 0.14", "1 0.51", $"cm.sendchatminus modermessage");
                        }
                        
                        CuiHelper.DestroyUi(player, UiInput);
                        CuiHelper.AddUi(player, container);
                        return;
                    case 3:
                        Ui.Button(ref container, UiInput, Ui.Color("#9acd32", 0.7f), "<b>SEND</b>", Ui.Color("#FFFFFF", 1f), 14, "0.76 0.14", "1 0.51", $"cm.sendchatminus playermessage {targetUlong}");
                        
                        CuiHelper.DestroyUi(player, UiInput);
                        CuiHelper.AddUi(player, container);
                        return;
            }
        }
        
        private static void FilterCharsInputMenu(BasePlayer player, int num)
        {
            var container = Ui.Container(UiInputFind, Ui.Color("#808080", 0f), "0.825 0.25", "0.9975 0.29");

            switch (num)
            {
                case 1:
                    Ui.Panel(ref container, UiInputFind, Ui.Color("#808080", 0.6f), "0 0", "0.8 1");
                    Ui.Input(ref container, UiInputFind, Ui.Color("#ffffff", 1f), "0 ", 18, $"cm.filtercharsadmincharf ", "0 0", "0.8 1");
                    Ui.Panel(ref container, UiInputFind, Ui.Color("#808080", 0.6f), "0.82 0", "1 1");
                    Ui.Label(ref container, UiInputFind, $"<b>FIND</b>", Ui.Color("#FFFFFF", 1f), 16, "0.82 0", "1 1");
                    CuiHelper.DestroyUi(player, UiInputFind);
                    CuiHelper.AddUi(player, container);
                    return;
                case 2:
                    Ui.Panel(ref container, UiInputFind, Ui.Color("#808080", 0.6f), "0 0", "0.8 1");
                    Ui.Input(ref container, UiInputFind, Ui.Color("#ffffff", 1f), "0 ", 18, $"cm.filtercharsmodercharf ", "0 0", "0.8 1");
                    Ui.Panel(ref container, UiInputFind, Ui.Color("#808080", 0.6f), "0.82 0", "1 1");
                    Ui.Label(ref container, UiInputFind, $"<b>FIND</b>", Ui.Color("#FFFFFF", 1f), 16, "0.82 0", "1 1");
                    CuiHelper.DestroyUi(player, UiInputFind);
                    CuiHelper.AddUi(player, container);
                    return;
                case 3:
                    Ui.Panel(ref container, UiInputFind, Ui.Color("#808080", 0.6f), "0 0", "0.8 1");
                    Ui.Input(ref container, UiInputFind, Ui.Color("#ffffff", 1f), "0 ", 18, $"cm.filtercharsplayerscharf ", "0 0", "0.8 1");
                    Ui.Panel(ref container, UiInputFind, Ui.Color("#808080", 0.6f), "0.82 0", "1 1");
                    Ui.Label(ref container, UiInputFind, $"<b>FIND</b>", Ui.Color("#FFFFFF", 1f), 16, "0.82 0", "1 1");
                    CuiHelper.DestroyUi(player, UiInputFind);
                    CuiHelper.AddUi(player, container);
                    return;
            }
        }
        
        private void MessageList(BasePlayer player, BasePlayer target, int num)
        {
            BasePlayer human;

            if (AllowAdmin(player) && num != 2 && num != 3) human = target;
            else if (AllowModer(player) && num != 1 && num != 3) human = target;
            else human = player;

            Dictionary<DateTime, string> top;
            var message = new List<string>();
            
            switch (num)
            {
                case 1:
                    CheckData(human.userID, human.userID);
                    top = _data.PlayerMess[human.userID].AdminMess.OrderByDescending(pair => pair.Key).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                    foreach (var msg in top)
                    {
                        message.Add(msg.Value);
                    }
                    ShowMessage(player, message);
                    break;
                case 2:
                    CheckData(human.userID, human.userID);
                    top = _data.PlayerMess[human.userID].ModerMess.OrderByDescending(pair => pair.Key).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                    foreach (var msg in top)
                    {
                        message.Add(msg.Value);
                    }
                    ShowMessage(player, message);
                    break;
                case 3:
                    CheckData(human.userID, target.userID);
                     top = _data.PlayerMess[human.userID].PrivateMess[target.userID].OrderByDescending(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                     var top1 = _data.PlayerMess[target.userID].PrivateMess[human.userID].OrderByDescending(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                    var top3 = new Dictionary<DateTime, string>();
                    foreach (var tops in top) 
                    {top3.Add(tops.Key, tops.Value);}
                    foreach (var tops in top1) 
                    {top3.Add(tops.Key, tops.Value);}
                    var top4 = top3.OrderByDescending(pair => pair.Key).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                    foreach (var msg in top4)
                    {
                        message.Add(msg.Value);
                    }
                    ShowMessage(player, message);
                    break;
            }
        }

        private static void ShowMessage(BasePlayer player, IEnumerable<string> messageList)
        {
            var container = Ui.Container(UiMess, "1 1 1 0", "0.8278 0.3495", "0.9955 0.85");
            var y1 = 0f;
            var y2 = 0.1915f;
            var y3 = 0.154f;
            foreach (var t in messageList)
            {
                var color = Ui.Color(t.Contains(player.displayName) ? "#9acd32" : "#42aaff", 0.6f);
                if (t == "") color = Ui.Color("#808080", 0.6f);
                if (t.Contains("ADMIN") || t.Contains("MODER")) color = Ui.Color("#a333ff", 0.6f);
                
                Ui.Panel(ref container, UiMess, Ui.Color("#808080", 0.3f), $"0 {y1}", $"1 {y2}");
                Ui.Panel(ref container, UiMess, color, $"0 {y1}", $"1 {y2}");
                Ui.Panel(ref container, UiMess, Ui.Color("#808080", 0.8f), $"0 {y3}", $"1 {y2}");
                Ui.Label(ref container, UiMess, t, Ui.Color("#FFFFFF", 1f), 12, $"0.01 {y1}", $"0.99 {y2}", 0, TextAnchor.UpperLeft);
                y1 += 0.201f;
                y2 += 0.201f;
                y3 += 0.201f;
            }

            CuiHelper.DestroyUi(player, UiMess);
            CuiHelper.AddUi(player, container);
        }
        
        private void ShowPlayers(BasePlayer player, Dictionary<string, ulong> list, string chars,  int num, int numList, string charF = "")
        {
            FilterCharsInputMenu(player, num);
            var container = Ui.Container(UiPlayers, Ui.Color("#808080", 0.6f), "0.2 0.3", "0.8 0.9");
            //var playerListSort = list.OrderByDescending(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            var playerListSort = list.OrderByDescending(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value).Skip(numList * 60).Take(60);

            var color = Ui.Color("#FFFFFF", 0.3f);

            var count = 0;
            var x1 = 0.014f;
            var y1 = 0.02f;
            var x2 = 0.17f;
            var y2 = 0.105f;
            foreach (var pl in playerListSort)
            {
                if (_data.PlayerMess.ContainsKey(player.userID) && _data.PlayerMess.ContainsKey(pl.Value) && _data.PlayerMess[player.userID].PrivateMess.ContainsKey(pl.Value) && _data.PlayerMess[pl.Value].PrivateMess.ContainsKey(player.userID))
                {
                    var date1 = _data.PlayerMess[player.userID].PrivateMess[pl.Value].OrderByDescending(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value).Take(1);
                    var date2 = _data.PlayerMess[pl.Value].PrivateMess[player.userID].OrderByDescending(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value).Take(1);

                    var dada1 = DateTime.Now;
                    var dada2 = DateTime.Now;
                    var text1 = "";
                    var text2 = "";
                    foreach (var r in date1)
                    {
                        foreach (var t in date2)
                        {
                            dada2 = t.Key;
                            text1 = t.Value;
                        }
                        dada1 = r.Key;
                        text2 = r.Value;
                    }
                    color = dada1 > dada2 ? text1 != "" && text2 != "" ? Ui.Color("#9acd32", 0.3f): Ui.Color("#FFFFFF", 0.3f) : text1 != "" && text2 != "" ? Ui.Color("#42aaff", 0.3f): Ui.Color("#FFFFFF", 0.3f);
                }
                var playerId = pl.Key;
                Ui.Button(ref container, UiPlayers, color, playerId, "1 1 1 1", 18, $"{x1} {y1}", $"{x2} {y2}", $"cm.openinputmenu {pl.Value} {num}");
                color = Ui.Color("#FFFFFF", 0.3f);
                ++count;

                x1 += 0.163f;
                x2 += 0.163f;
                if (count % 6 == 0)
                {
                    y1 += 0.0968f;
                    y2 += 0.0968f;
                    x1 = 0.014f;
                    x2 = 0.17f;
                }

                if (count >= 60)
                    break;
            }

            var command = num == 1 ? $"cm.filtercharsadmin{chars}" : num == 2 ? $"cm.filtercharsmoder{chars}" : $"cm.filtercharsplayers{chars}";
            var numDown = numList == 0 ? 0 : numList - 1;
            var cmdUp = (numList + 1) * 60 > playerListSort.Count() ? "" : $"{command} {numList + 1} {charF}";
            var cmdDown = numList <= 0 ? "" : $"{command} {numDown} {charF}";
            Ui.Button(ref container, UiPlayers, Ui.Color("#808080", 0.6f), $"{numList + 2}", "1 1 1 1", 16, "0.9 -0.08", "1 -0.015", $"{cmdUp}");
            Ui.Button(ref container, UiPlayers, Ui.Color("#808080", 0.6f), $"{numDown +1}", "1 1 1 1", 16, "0 -0.08", "0.1 -0.015", $"{cmdDown}");

            CuiHelper.DestroyUi(player, UiMess);
            CuiHelper.AddUi(player, container);
        }

        #endregion
        
        #region DestroyGui
        
        private static void DestroyGui(BasePlayer player, string container)
        {
            CuiHelper.DestroyUi(player, container);
        }
        
        private static void DestroyGuiNoOne(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UiPlayers);
            CuiHelper.DestroyUi(player, UiMess);
            CuiHelper.DestroyUi(player, UiInputFind);
        }
        
        private static void DestroyGuiAll(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UiElement);
            CuiHelper.DestroyUi(player, UiInput);
            CuiHelper.DestroyUi(player, UiInputFind);
            CuiHelper.DestroyUi(player, UiMess);
            CuiHelper.DestroyUi(player, UiPlayers);
            CuiHelper.DestroyUi(player, UiNotiсe);
        }
        
        #endregion

        #region Func

        private static bool IsNpc(BasePlayer player) //исключение НПС
        {
            if (player == null) return false;
            if (player is NPCPlayer)
                return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }
        
        private bool AllowModer(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, PermModer);
        }
        
        private bool AllowAdmin(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, PermAdmin);
        }

        private void Msg(BasePlayer player, string msg, params object[] args)
        {
            SendReply(player, $"{_config.Prefix}: " + string.Format(GetMsg(msg, player), args));
        }

        private string GetMsg(string key) => lang.GetMessage(key, this);
        private string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        
        private static BasePlayer GetPlayer(ulong userId)
        {
            var player = BasePlayer.FindByID(userId);
            if (player)
                return player;
 
            player = BasePlayer.FindSleeping(userId);
            if (player)
                return player;
 
            player = BasePlayer.Find(userId.ToString());
            return player ? player : null;
        }

        private void CheckData(ulong playerId, ulong targetId)
        {
            if (playerId == targetId)
            {
                if (!_data.PlayerMess.ContainsKey(playerId))
                 {
                     _data.PlayerMess.Add(playerId, new ChatMess{AdminMess = new Dictionary<DateTime, string>(), ModerMess = new Dictionary<DateTime, string>()});
                     _data.PlayerMess[playerId].AdminMess.Add(DateTime.Now, "");
                     _data.PlayerMess[playerId].ModerMess.Add(DateTime.Now, "");
                     _data.PlayerMess[playerId].CountAdminMess = 0;
                     _data.PlayerMess[playerId].CountModerMess = 0;
                 }
//                if (!_data.PlayerMess.ContainsKey(targetId))
//                {
//                    _data.PlayerMess.Add(targetId, new ChatMess{AdminMess = new Dictionary<DateTime, string>(), ModerMess = new Dictionary<DateTime, string>()});
//                    _data.PlayerMess[targetId].AdminMess.Add(DateTime.Now, "");
//                    _data.PlayerMess[targetId].ModerMess.Add(DateTime.Now, "");
//                    _data.PlayerMess[targetId].CountAdminMess = 0;
//                    _data.PlayerMess[targetId].CountModerMess = 0;
//                }
             }
             else
            {
                 if (!_data.PlayerMess.ContainsKey(playerId))
                 {
                     _data.PlayerMess.Add(playerId, new ChatMess{PrivateMess = new Dictionary<ulong, Dictionary<DateTime, string>>()});
                 }
                 if (!_data.PlayerMess[playerId].PrivateMess.ContainsKey(targetId))
                 {
                     _data.PlayerMess[playerId].PrivateMess.Add(targetId, new Dictionary<DateTime, string>());
                     _data.PlayerMess[playerId].PrivateMess[targetId].Add(DateTime.Now, "");
                     SaveData();
                 }
                 
                 if (!_data.PlayerMess.ContainsKey(targetId))
                 {
                     _data.PlayerMess.Add(targetId, new ChatMess{PrivateMess = new Dictionary<ulong, Dictionary<DateTime, string>>()});
                 }
                 if (!_data.PlayerMess[targetId].PrivateMess.ContainsKey(playerId))
                 {
                     _data.PlayerMess[targetId].PrivateMess.Add(playerId, new Dictionary<DateTime, string>());
                     _data.PlayerMess[targetId].PrivateMess[playerId].Add(DateTime.Now.ToUniversalTime(), "");
                     SaveData();
                 }
            }
        }

        private static bool CheckMessage(string message)
        {
            return !message.Contains("<") && !message.Contains("\\n") && message != "";
        }

        private void SendUpdateMessages(BasePlayer player, BasePlayer target,  int num)
        {
            switch (num)
            {
                    case 1:
                        foreach (var human in _activeGuiPrivate)
                        {
                            if (human.Key == target && human.Value == player)
                            {
                                DestroyGuiNoOne(target);
                                MessageList(target, player, 3);
                                return;
                            }
                        }
                        Msg(target, lang.GetMessage("PM_FROM", this, target.UserIDString), player.displayName);
                        return;
                    case 2:
                        foreach (var human in _activeGuiAdmin)
                        {
                            if (human.Value == player && human.Key != player)
                            {
                                DestroyGuiNoOne(human.Key);
                                MessageList(human.Key, target, 1);
                            }
                        }

                        foreach (var admins in BasePlayer.activePlayerList)
                        {
                            if (AllowAdmin(admins))
                            {
                                Msg(admins, lang.GetMessage("FOR_ADMIN", this, admins.UserIDString), player.displayName);
                                //Msg(admins, $"Игрок <color=#81b67a>{player.displayName}</color> написал новое обращение <color=#81b67a>АДМИНАМ</color>, используйте <color=#81b67a>/cm</color>, чтобы ответить");
                            }
                        }
                        return;
                    case 3:
                        foreach (var human in _activeGuiAdmin)
                        {
                            if (human.Key != target) continue;
                            DestroyGuiNoOne(target);
                            MessageList(target, target, 1);
                            return;
                        }
                        Msg(target, lang.GetMessage("FROM_ADMIN", this, target.UserIDString));
                        //Msg(target, $"Администратор ответил на ваше обращение, используйте <color=#81b67a>/cm</color>, чтобы ответить");
                        return;
                    case 4:
                        foreach (var human in _activeGuiModer)
                        {
                            if (human.Value == player && human.Key != player)
                            {
                                DestroyGuiNoOne(human.Key);
                                MessageList(human.Key, target, 2);
                            }
                        }
                        foreach (var moders in BasePlayer.activePlayerList)
                        {
                            if (AllowModer(moders)) Msg(moders, lang.GetMessage("FOR_MODER", this, moders.UserIDString), player.displayName);
                            //Msg(moders, $"Игрок <color=#81b67a>{player.displayName}</color> написал новое обращение <color=#81b67a>МОДЕРАТОРАМ</color>, используйте <color=#81b67a>/cm</color>, чтобы ответить");
                        }
                        return;
                    case 5:
                        foreach (var human in _activeGuiModer)
                        {
                            if (human.Key != target) continue;
                            DestroyGuiNoOne(target);
                            MessageList(target, target, 2);
                            return;
                        }
                        Msg(target, lang.GetMessage("FROM_MODER", this, target.UserIDString));
                        //Msg(target, $"Модератор ответил на ваше обращение, используйте <color=#81b67a>/cm</color>, чтобы ответить");
                    return;
                case 6:
                    foreach (var human in _activeGuiAdmin)
                    {
                        if (human.Value != target || human.Key == target) continue;
                        DestroyGuiNoOne(human.Key);
                        //Msg(human.Key, "Игрок <color=#81b67a>удалил</color> тикет");
                        Msg(human.Key, lang.GetMessage("TICKET_DELETE", this, human.Key.UserIDString));
                    }
                    return;
                case 7:
                    foreach (var human in _activeGuiModer)
                    {
                        if (human.Value != target || human.Key == target) continue;
                        DestroyGuiNoOne(human.Key);
                        //Msg(human.Key, "Игрок <color=#81b67a>удалил</color> тикет");
                        Msg(human.Key, lang.GetMessage("TICKET_DELETE", this, human.Key.UserIDString));
                    }
                    return;
                case 8:
                    foreach (var human in _activeGuiPrivate)
                    {
                        if (human.Key != target || human.Value != player) continue;
                        DestroyGuiNoOne(target);
                        CreateGui(target, player);
                        MessageList(target, player, 3);
                    }
                    return;
            }
        }
        
        #endregion
        
        #region Oxide
        
        private void Init()
        {
            permission.RegisterPermission(PermAdmin, this);
            permission.RegisterPermission(PermModer, this);
        }
        
        private void OnServerInitialized()
        {
            LoadData();
            LoadConfig();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (IsNpc(player)) continue;
                if (!_data.Players.ContainsValue(player.userID))
                    _data.Players.Add(player.displayName, player.userID);
                SaveData();
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player == null) continue;
                if (IsNpc(player)) continue;
                if (!_data.Players.ContainsValue(player.userID))
                    _data.Players.Add(player.displayName, player.userID);
                SaveData();
            }
        }
        
        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyGuiAll(player);
            if (_activeGui.Contains(player)) _activeGui.Remove(player);
            if (_activeGuiModer.ContainsKey(player)) _activeGuiModer.Remove(player);
            if (_activeGuiAdmin.ContainsKey(player)) _activeGuiAdmin.Remove(player);
            if (_activeGuiPrivate.ContainsKey(player)) _activeGuiPrivate.Remove(player);
            if (_activeFind.Contains(player))_activeFind.Remove(player);
        }
        
        private void OnPlayerInit(BasePlayer player)
        {
            if (IsNpc(player)) return;
            if (!_data.Players.ContainsValue(player.userID)) _data.Players.Add(player.displayName, player.userID);
            SaveData();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyGuiAll(player);
            }
        }

        #endregion

        #region API

        [HookMethod("SendPrivateMessage")]
        private void SendPrivateMessage(ulong playerId, ulong targetId, string msg)
        {
            var player = GetPlayer(playerId);
            var target = GetPlayer(targetId);
            CheckData(player.userID, target.userID);
            if (!CheckMessage(msg))
            {
                Msg(player, "Вы не можете отправлять <color=#81b67a>пустые</color> сообщения, или сообщения, содержащие знаки <color=#81b67a>'<'</color> и <color=#81b67a>'\\n'</color>");
                return;
            }
            _data.PlayerMess[player.userID].PrivateMess[target.userID].Add(DateTime.Now, ($"<b><color=#FFFFFF>  {player.displayName}</color></b> <size=10>({DateTime.Now})</size>\n" + msg));
            SaveData();
            foreach (var human in _activeGuiPrivate)
            {
                if (human.Key != target || human.Value != player) continue;
                DestroyGuiNoOne(target);
                CreateGui(target, player);
                MessageList(target, player, 3);
            }
            foreach (var human in _activeGuiPrivate)
            {
                if (human.Key != player || human.Value != target) continue;
                DestroyGuiNoOne(player);
                CreateGui(player, target);
                MessageList(player, target, 3);
            }
        }

        #endregion
        
        #region Data
        
        private PlayerMessage _data = new PlayerMessage();

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _data);

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PlayerMessage>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PlayerMessage();
        }

        #region PlayerMessage

        private class PlayerMessage
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            public Dictionary<ulong, ChatMess> PlayerMess = new Dictionary<ulong, ChatMess>();
            public Dictionary<string, ulong> Players = new Dictionary<string, ulong>();
            //public Dictionary<ulong, SettingEntry> PlayersSetting = new Dictionary<ulong, SettingEntry>();
        }
        
        private class ChatMess
        {
            public int CountAdminMess;
            public int CountModerMess;
            public Dictionary<DateTime, string> ModerMess = new Dictionary<DateTime, string>();
            public Dictionary<DateTime, string> AdminMess = new Dictionary<DateTime, string>();
            public Dictionary<ulong, Dictionary<DateTime, string>> PrivateMess = new Dictionary<ulong, Dictionary<DateTime, string>>();
        }
        
//        private class SettingEntry
//        {
//            public int StatusMenu = 0;
//            public int Sound = 0;
//        }
        
        #endregion
        
        #endregion

        #region CUI Helper

        private class Ui
        {
            public static CuiElementContainer Container(string panelName, string color, string aMin, string aMax, float fadein = 0f, bool useCursor = false, string parent = "Overlay")
            {
                var newElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color, FadeIn = fadein},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return newElement;
            }

            public static void Panel(ref CuiElementContainer container, string panel, string color, string aMin,
                string aMax, float fadein = 0f, bool cursor = false)
            {
                container.Add(new CuiPanel
                    {
                        Image = {Color = color, FadeIn = fadein/*, Sprite = "assets/content/ui/Noise.psd"*/},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor
                    },
                    panel);
            }

            public static void Label(ref CuiElementContainer container, string panel, string text, string color,
                int size, string aMin, string aMax, float fadein = 0f, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                    {
                        Text =
                        {
                            FontSize = size,
                            Align = align,
                            Text = text,
                            Color = color,
                            Font = "robotocondensed-regular.ttf",
                            FadeIn = fadein
                        },
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax}
                    }, 
                    panel);
            }

            public static void Button(ref CuiElementContainer container, string panel, string color, string text, string color1, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                    {
                        Button = {Color = color, Command = command, FadeIn = 0f},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        Text =
                        {
                            Text = text,
                            FontSize = size,
                            Align = align,
                            Color = color1,
                            Font = "robotocondensed-regular.ttf"
                        }
                    },
                    panel);
            }

            public static void Input(ref CuiElementContainer container, string panel, string color, string text,
                int size, string command, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 128,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text,
                            Font = "robotocondensed-bold.ttf"
                        },
                        new CuiOutlineComponent
                        {
                            Color = Ui.Color("#000000", 1f),
                            Distance = "0 0.1",
                            UseGraphicAlpha = false
                        },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax}
                    },
                });
            }
            
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double) red / 255} {(double) green / 255} {(double) blue / 255} {alpha}";
            }
        }
        #endregion
        
        #region Config
        private PluginConfig _config;
        private class PluginConfig
        {
            [JsonProperty("Префикс уведомлений")]
            public string Prefix { get; set; }
            
            [JsonProperty("Включить звуковое оповещение")]
            public bool EnableSound { get; set; }
            
            [JsonProperty("Звук оповещения")]
            public string Sound { get; set; }
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Хрум...!");
            _config = new PluginConfig()
            {
                Prefix = "<color=#81b67a>HAPPY RUST</color>",
                EnableSound = true,
                Sound = "assets/prefabs/npc/scientist/sound/chatter.prefab"
            };
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion
        
        #region Localisation
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MSG_ERROR"] = "Вы не можете отправлять <color=#81b67a>пустые</color> сообщения, или сообщения, содержащие знаки <color=#81b67a>'<'</color> и <color=#81b67a>'\\n'</color>",
                ["TICKET_DELETE"] = "Тикет <color=#81b67a>удален</color> !",
                ["PM_FROM"] = "Игрок <color=#81b67a>{0}</color> прислал вам личное сообщение, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FOR_ADMIN"] = "Игрок <color=#81b67a>{0}</color> написал новое обращение <color=#81b67a>АДМИНАМ</color>, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FROM_ADMIN"] = "Администратор ответил на ваше обращение, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FOR_MODER"] = "Игрок <color=#81b67a>{0}</color> написал новое обращение <color=#81b67a>МОДЕРАТОРАМ</color>, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FROM_MODER"] = "Модератор ответил на ваше обращение, используйте <color=#81b67a>/cm</color>, чтобы ответить"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MSG_ERROR"] = "Вы не можете отправлять <color=#81b67a>пустые</color> сообщения, или сообщения, содержащие знаки <color=#81b67a>'<'</color> и <color=#81b67a>'\\n'</color>",
                ["TICKET_DELETE"] = "Тикет <color=#81b67a>удален</color> !",
                ["PM_FROM"] = "Игрок <color=#81b67a>{0}</color> прислал вам личное сообщение, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FOR_ADMIN"] = "Игрок <color=#81b67a>{0}</color> написал новое обращение <color=#81b67a>АДМИНАМ</color>, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FROM_ADMIN"] = "Администратор ответил на ваше обращение, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FOR_MODER"] = "Игрок <color=#81b67a>{0}</color> написал новое обращение <color=#81b67a>МОДЕРАТОРАМ</color>, используйте <color=#81b67a>/cm</color>, чтобы ответить",
                ["FROM_MODER"] = "Модератор ответил на ваше обращение, используйте <color=#81b67a>/cm</color>, чтобы ответить"
            }, this, "ru");
        }
        #endregion
    }
}