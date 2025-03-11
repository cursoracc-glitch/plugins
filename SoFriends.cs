﻿﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
 using Facepunch.Extend;
  using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
  using ProtoBuf;
  using UnityEngine;
 using Pool = Facepunch.Pool;

 namespace Oxide.Plugins
{
    [Info("SoFriends", "https://topplugin.ru/", "1.1.2")]
    public class SoFriends : RustPlugin
    {
        #region [DATA&CONFIG]

        private Dictionary<ulong, FriendData> friendData = new Dictionary<ulong, FriendData>();
        private Dictionary<ulong, ulong> playerAccept = new Dictionary<ulong, ulong>();
        private static Configs cfg { get; set; }

        private class FriendData
        {
            [JsonProperty("Ник")] public string Name;

            [JsonProperty("Список друзей")]
            public Dictionary<ulong, FriendAcces> friendList = new Dictionary<ulong, FriendAcces>();

            public class FriendAcces
            {
                [JsonProperty("Ник")] public string name;
                [JsonProperty("Урон по человеку")] public bool Damage;

                [JsonProperty("Авторизациия в турелях")]
                public bool Turret;

                [JsonProperty("Авторизациия в дверях")]
                public bool Door;

                [JsonProperty("Авторизациия в пво")] public bool Sam;
                
                [JsonProperty("Авторизациия в шкафу")] public bool bp;
            }
        }

        private class Configs
        {
            [JsonProperty("Включить настройку авто авторизации турелей?")]
            public bool Turret;

            [JsonProperty("Включить настройку урона по своим?")]
            public bool Damage;

            [JsonProperty("Включить настройку авто авторизации в дверях?")]
            public bool Door;

            [JsonProperty("Включить настройку авто авторизации в пво?")]
            public bool Sam;
            
            [JsonProperty("Включить настройку авто авторизации в шкафу?")]
            public bool build;
            
            [JsonProperty("Сколько максимум людей может быть в друзьях?")]
            public int MaxFriends;

            [JsonProperty("Урон по человеку(По стандрату у игрока включена?)")]
            public bool SDamage;

            [JsonProperty("Авторизациия в турелях(По стандрату у игрока включена?)")]
            public bool STurret;

            [JsonProperty("Авторизациия в дверях(По стандрату у игрока включена?)")]
            public bool SDoor;

            [JsonProperty("Авторизациия в пво(По стандрату у игрока включена?)")]
            public bool SSam;
            
            [JsonProperty("Авторизациия в шкафу(По стандрату у игрока включена?)")]
            public bool bp;
            
            [JsonProperty("Время ожидания  ответа на запроса в секнудах")]
            public int otvet;

            [JsonProperty("Вообще включать пво настройку?")]
            public bool SSamOn; 

            public static Configs GetNewConf()
            {
                var newconfig = new Configs();
                newconfig.Damage = true;
                newconfig.Door = true;
                newconfig.build = true;
                newconfig.Turret = true;
                newconfig.Sam = true;
                newconfig.MaxFriends = 5;
                newconfig.SDamage = false;
                newconfig.SDoor = true;
                newconfig.STurret = true;
                newconfig.SSam = true;
                newconfig.SSamOn = true;
                newconfig.otvet = 10;
                return newconfig;
            }
        }

        protected override void LoadDefaultConfig() => cfg = Configs.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(cfg);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<Configs>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultMessages()
         {
             var ru = new Dictionary<string, string>();
             foreach (var rus in new Dictionary<string, string>()
             {
                 ["SYNTAX"] = "/fmenu - Открыть меню друзей\n/f(riend) add - Добавить в друзья\n/f(riend) remove - Удалить из друзей\n/f(riend) list - Список друзей\n/f(riend) team - Пригласить в тиму всех друзей онлайн\n/f(riend) set - Настройка друзей по отдельности\n/f(riend) setall - Настройка друзей всех сразу",
                 ["NPLAYER"] = "Игрок не найден!",
                 ["CANTADDME"] = "Нельзя добавить себя в друзья!",
                 ["ONFRIENDS"] = "Игрок уже у вас в друзьях!",
                 ["MAXFRIENDSPLAYERS"] = "У игрока максимальное кол-во друзей!",
                 ["MAXFRIENDYOU"] = "У вас максимальное кол-во друзей!",
                 ["HAVEINVITE"] = "Игрок уже имеет запрос в друзья!",
                 ["SENDADD"] = "Вы отправили запрос, ждем ответа!",
                 ["YOUHAVEINVITE"] = "Вам пришел запрос в друзья напишите /f(riend) accept",
                 ["TIMELEFT"] = "Вы не ответили на запрос!",
                 ["HETIMELEFT"] = "Вам не ответили на запрос!",
                 ["DONTHAVE"] = "У вас нет запросов!",
                 ["ADDFRIEND"] = "Успешное добавление в друзья!",
                 ["DENYADD"] = "Отклонение запроса в друзья!",
                 ["PLAYERDHAVE"] = "У тебя нету такого игрока в друзьях!",
                 ["REMOVEFRIEND"] = "Успешное удаление из друзей!",
                 ["LIST"] = "Список пуст!",
                 ["LIST2"] = "Список друзей",
                 ["SYNTAXSET"] = "/f(riend) set damage [Name] - Урон по человеку\n/f(riend) set door [NAME] - Авторизация в дверях для человека\n/f(riend) set turret [NAME] - Авторизация в турелях для человека\n/f(riend) set sam [NAME] - Авторизация в пво для человека",
                 ["SETOFF"] = "Настройка отключена",
                 ["DAMAGEOFF"] = "Урон по игроку {0} выключен!",
                 ["DAMAGEON"] = "Урон по игроку {0} включен!",
                 ["AUTHDOORON"] = "Авторизация в дверях для {0} включена!",
                 ["AUTHDOOROFF"] = "Авторизация в дверях для {0} выключена!",
                 ["AUTHTURRETON"] = "Авторизация в терелях для {0} включена!",
                 ["AUTHTURRETOFF"] = "Авторизация в терелях для {0} выключена!",
                 ["AUTHBUILDON"] = "Авторизация в шкафу для {0} включена!",
                 ["AUTHBUILDOFF"] = "Авторизация в шкафу для {0} выключена!",
                 ["AUTHSAMON"] = "Авторизация в ПВО для {0} включена!",
                 ["AUTHSAMOFF"] = "Авторизация в ПВО для {0} выключена!",
                 ["SYNTAXSETALL"] = "/f(riend) setall damage 0/1 - Урон по всех друзей\n/f(riend) setall door 0/1 - Авторизация в дверях для всех друзей\n/f(riend) setall turret 0/1 - Авторизация в турелях для всех друзей\n/f(riend) setall sam 0/1 - Авторизация в пво для всех друзей",
                 ["DAMAGEOFFALL"] = "Урон по всем друзьям выключен!",
                 ["DAMAGEONALL"] = "Урон по всем друзьям включен!",
                 ["AUTHDOORONALL"] = "Авторизация в дверях для всех друзей включена!",
                 ["AUTHDOOROFFALL"] = "Авторизация в дверях для всех друзей выключена!",
                 ["AUTHBUILDONALL"] = "Авторизация в шкафу для всех друзей включена!",
                 ["AUTHBUILDOFFALL"] = "Авторизация в шкафу для всех друзей выключена!",
                 ["AUTHTURRETONALL"] = "Авторизация в терелях для всех друзей включена!",
                 ["AUTHTURRETOFFALL"] = "Авторизация в терелях для всех друзей выключена!",
                 ["AUTHSAMONALL"] = "Авторизация в ПВО для всех друзей включена!",
                 ["AUTHSAMOFFALL"] = "Авторизация в ПВО для всех друзей выключена!",
                 ["SENDINVITETEAM"] = "Приглашение отправлено: ",
                 ["SENDINVITE"] = "Вам пришло приглашение в команду от",
                 ["DAMAGE"] = "Нельзя аттаковать {0} это ваш друг!",
             }) ru.Add(rus.Key, rus.Value);
             lang.RegisterMessages(ru, this, "ru");
             lang.RegisterMessages(ru, this, "en");
         }
        #endregion

        #region [Func]

        private string PlugName = "<color=red>[SOFRIEND]</color> ";

        [ChatCommand("f")]
        private void FriendCmd(BasePlayer player, string command, string[] arg)
        {
            ulong ss;
            FriendData player1;
            FriendData targetPlayer;
            if (!friendData.TryGetValue(player.userID, out player1)) return;
            if (arg.Length < 1)
            {
                SendReply(player,
                    $"<size=22>{PlugName}</size>\n{lang.GetMessage("SYNTAX", this, player.UserIDString)}");
                return;
            }

            switch (arg[0])
            {
                case "add":
                    if (arg.Length < 2)
                    {
                        SendReply(player, $"{PlugName}/f(riend) add [NAME or SteamID]");
                        return;
                    }

                    var argLists = arg.ToList();
                    argLists.RemoveRange(0, 1);
                    var name = string.Join(" ", argLists.ToArray()).ToLower();
                    var target = BasePlayer.Find(name);
                    if (target == null || !friendData.TryGetValue(target.userID, out targetPlayer))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("NPLAYER", this, player.UserIDString)}");
                        return;
                    }
                    
                    if (target.userID == player.userID)
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("CANTADDME", this, player.UserIDString)}");
                        return;
                    }
                    
                    if (player1.friendList.Count >= cfg.MaxFriends)
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("MAXFRIENDYOU", this, player.UserIDString)}");
                        return;
                    }
                     
                    if (player1.friendList.ContainsKey(target.userID))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("ONFRIENDS", this, player.UserIDString)}");
                        return;
                    }
                    
                    if (targetPlayer.friendList.Count >= cfg.MaxFriends)
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("MAXFRIENDSPLAYERS", this, player.UserIDString)}");
                        return;
                    }
                    
                    if (playerAccept.ContainsKey(target.userID))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("HAVEINVITE", this, player.UserIDString)}");
                        return;
                    }

                    playerAccept.Add(target.userID, player.userID);
                    SendReply(player, $"{PlugName}{lang.GetMessage("SENDADD", this, player.UserIDString)}");
                    SendReply(target, $"{PlugName}{lang.GetMessage("YOUHAVEINVITE", this, target.UserIDString)}");
                    InivteStart(player, target);
                    ss = target.userID;
                    timer.Once(cfg.otvet, () =>
                    {
                        if (!playerAccept.ContainsKey(target.userID) || !playerAccept.ContainsValue(player.userID)) return;
                        if (target != null)
                        {
                            CuiHelper.DestroyUi(target, LayerInvite);
                            SendReply(target, $"{PlugName}{lang.GetMessage("TIMELEFT", this, target.UserIDString)}");
                        }
                        
                        SendReply(player, $"{PlugName}{lang.GetMessage("HETIMELEFT", this, player.UserIDString)}");
                        playerAccept.Remove(ss);
                    });
                    break;
                case "accept":

                    if (!playerAccept.TryGetValue(player.userID, out ss))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("DONTHAVE", this, player.UserIDString)}");
                        return;
                    }

                    if (!friendData.TryGetValue(ss, out targetPlayer))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("NPLAYER", this, player.UserIDString)}");
                        return;
                    }

                    if (player1.friendList.Count >= cfg.MaxFriends)
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("MAXFRIENDYOU", this, player.UserIDString)}");
                        return;
                    }

                    if (targetPlayer.friendList.Count >= cfg.MaxFriends)
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("MAXFRIENDSPLAYERS", this, player.UserIDString)}!");
                        return;
                    }

                    target = BasePlayer.FindByID(ss);
                    player1.friendList.Add(target.userID,
                        new FriendData.FriendAcces()
                        {
                            name = target.displayName, Damage = cfg.SDamage, Door = cfg.SDoor, Turret = cfg.STurret,
                            Sam = cfg.SSam, bp = cfg.bp
                        });
                    targetPlayer.friendList.Add(player.userID,
                        new FriendData.FriendAcces()
                        {
                            name = player.displayName, Damage = cfg.SDamage, Door = cfg.SDoor, Turret = cfg.STurret,
                            Sam = cfg.SSam, bp = cfg.bp
                        });
                    SendReply(player, $"{PlugName}{lang.GetMessage("ADDFRIEND", this, player.UserIDString)}");
                    playerAccept.Remove(player.userID);
                    SendReply(target, $"{PlugName}{lang.GetMessage("ADDFRIEND", this, target.UserIDString)}");
                    if(cfg.bp) AuthBuild(target, player.userID);
                    CuiHelper.DestroyUi(player, LayerInvite);
                    break;
                case "deny":
                    if (!playerAccept.TryGetValue(player.userID, out ss))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("DONTHAVE", this, player.UserIDString)}");
                        return;
                    }

                    if (!friendData.TryGetValue(ss, out targetPlayer))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("NPLAYER", this, player.UserIDString)}");
                        return;
                    }

                    target = BasePlayer.FindByID(ss);
                    playerAccept.Remove(player.userID);
                    SendReply(player, $"{PlugName}{lang.GetMessage("DENYADD", this, player.UserIDString)}");
                    SendReply(target, $"{PlugName}{lang.GetMessage("DENYADD", this, target.UserIDString)}");
                    CuiHelper.DestroyUi(player, LayerInvite);
                    break;
                case "remove":
                    if (arg.Length < 2)
                    {
                        SendReply(player, $"{PlugName}/f(riend) remove [NAME or SteamID]");
                        return;
                    }

                    argLists = arg.ToList();
                    argLists.RemoveRange(0, 1);
                    name = string.Join(" ", argLists.ToArray()).ToLower();
                    ulong tt;
                    if (ulong.TryParse(arg[1], out tt)) { }else tt = player1.friendList.FirstOrDefault(p => p.Value.name.ToLower().Contains(name)).Key;

                    if (!player1.friendList.ContainsKey(tt))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("PLAYERDHAVE", this, player.UserIDString)}");
                        return;
                    }

                    if (!friendData.TryGetValue(tt, out targetPlayer))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("NPLAYER", this, player.UserIDString)}");
                        return;
                    }

                    player1.friendList.Remove(tt);
                    targetPlayer.friendList.Remove(player.userID);
                    SendReply(player, $"{PlugName}{lang.GetMessage("REMOVEFRIEND", this, player.UserIDString)}");
                    target = tt.IsSteamId() ? BasePlayer.FindByID(tt) : BasePlayer.Find(arg[1].ToLower());
                    if (target != null)
                        SendReply(target, $"{PlugName}{lang.GetMessage("REMOVEFRIEND", this, player.UserIDString)}");
                    break;
                case "list":
                    if (player1.friendList.Count < 1)
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("LIST", this, player.UserIDString)}");
                        return;
                    }
                    
                    var argList = player1.friendList;
                    var friendlist = $"{PlugName}{lang.GetMessage("LIST2", this, player.UserIDString)}\n";
                    foreach (var keyValuePair in argList)
                        friendlist += keyValuePair.Value.name + $"({keyValuePair.Key})\n";
                    SendReply(player, friendlist);
                    break;
                case "set":
                    if (arg.Length < 3)
                    {
                        SendReply(player, $"<size=22>{PlugName}</size>\n{lang.GetMessage("SYNTAXSET", this, player.UserIDString)}");
                        return;
                    }

                    argLists = arg.ToList();
                    argLists.RemoveRange(0, 2);
                    name = string.Join(" ", argLists.ToArray()).ToLower();
                    FriendData.FriendAcces access;
                    if (ulong.TryParse(arg[2], out ss)) {}else ss = player1.friendList.FirstOrDefault(p => p.Value.name.ToLower().Contains(name)).Key;

                    if (!player1.friendList.TryGetValue(ss, out access))
                    {
                        SendReply(player, $"{PlugName}{lang.GetMessage("NPLAYER", this, player.UserIDString)}");
                        return;
                    }

                    switch (arg[1])
                    {
                        case "damage":
                            if (!cfg.Damage)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }

                            if (access.Damage)
                            {
                                SendReply(player, $"{PlugName}{String.Format(lang.GetMessage("DAMAGEOFF", this, player.UserIDString), access.name)}");
                                access.Damage = false;
                            }
                            else
                            {
                                SendReply(player, $"{PlugName}{String.Format(lang.GetMessage("DAMAGEON", this, player.UserIDString), access.name)}");
                                access.Damage = true;
                            }

                            break;
                        case "build":
                            if (!cfg.build)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }

                            if (access.bp)
                            {
                                SendReply(player,
                                    $"{PlugName}{String.Format(lang.GetMessage("AUTHBUILDOFF", this, player.UserIDString), access.name)}");
                                access.bp = false;
                            }
                            else
                            {
                                SendReply(player, $"{PlugName}{String.Format(lang.GetMessage("AUTHBUILDON", this, player.UserIDString), access.name)}");
                                access.bp = true;
                                AuthBuild(player, ss);
                            }

                            break;  
                        case "door":
                            if (!cfg.Door)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }

                            if (access.Door)
                            {
                                SendReply(player,
                                    $"{PlugName}{String.Format(lang.GetMessage("AUTHDOOROFF", this, player.UserIDString), access.name)}");
                                access.Door = false;
                            }
                            else
                            {
                                SendReply(player, $"{PlugName}{String.Format(lang.GetMessage("AUTHDOORON", this, player.UserIDString), access.name)}");
                                access.Door = true;
                            }

                            break;
                        case "turret":
                            if (!cfg.Turret)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }

                            if (access.Turret)
                            {
                                SendReply(player,
                                    $"{PlugName}{String.Format(lang.GetMessage("AUTHTURRETOFF", this, player.UserIDString), access.name)}");
                                access.Turret = false;
                            }
                            else
                            {
                                SendReply(player,
                                    $"{PlugName}{String.Format(lang.GetMessage("AUTHTURRETON", this, player.UserIDString), access.name)}");
                                access.Turret = true;
                            }

                            break;
                        case "sam":
                            if (!cfg.SSamOn) return;
                            if (!cfg.Sam)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }

                            if (access.Sam)
                            {
                                SendReply(player, $"{PlugName}{String.Format(lang.GetMessage("AUTHSAMOFF", this, player.UserIDString), access.name)}");
                                access.Sam = false;
                            }
                            else
                            {
                                SendReply(player, $"{PlugName}{String.Format(lang.GetMessage("AUTHSAMON", this, player.UserIDString), access.name)}");
                                access.Sam = true;
                            }

                            break;
                    }

                    break;
                case "setall":
                    if (arg.Length < 3)
                    {
                        SendReply(player,
                            $"<size=22>{PlugName}</size>\n{lang.GetMessage("SYNTAXSETALL", this, player.UserIDString)}");
                        return;
                    }

                    switch (arg[1])
                    {
                        case "door":
                            if (!cfg.Door)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }
                            if (arg[2] == "1")
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Door = true;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHDOORONALL", this, player.UserIDString)}");
                            }
                            else
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Door = false;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHDOOROFFALL", this, player.UserIDString)}");
                            }

                            break;
                        
                        case "damage":
                            if (!cfg.Damage)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }
                            if (arg[2] == "1")
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Damage = true;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("DAMAGEON", this, player.UserIDString)}");
                            }
                            else
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Damage = false;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("DAMAGEOFF", this, player.UserIDString)}");
                            }

                            break;
                        case "build":
                            if (!cfg.Turret)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }
                            if (arg[2] == "1")
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Turret = true;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHBUILDONALL", this, player.UserIDString)}");
                            }
                            else
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Turret = false;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHBUILDOFFALL", this, player.UserIDString)}");
                            }

                            break;
                        case "turret":
                            if (!cfg.Turret)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }
                            if (arg[2] == "1")
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Turret = true;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHTURRETONALL", this, player.UserIDString)}");
                            }
                            else
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Turret = false;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHTURRETOFFALL", this, player.UserIDString)}");
                            }

                            break;
                        case "sam":
                            if (!cfg.SSamOn) return;
                            if (!cfg.Sam)
                            {
                                SendReply(player, $"{PlugName}{lang.GetMessage("SETOFF", this, player.UserIDString)}");
                                return;
                            }
                            if (arg[2] == "1")
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Sam = true;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHSAMONALL", this, player.UserIDString)}");
                            }
                            else
                            {
                                foreach (var friends in player1.friendList)
                                {
                                    friends.Value.Sam = false;
                                }

                                SendReply(player, $"{PlugName}{lang.GetMessage("AUTHSAMOFFALL", this, player.UserIDString)}");
                            }

                            break;
                    }

                    break;
                case "team":
                    var team = player.Team;
                    if (team == null || player.currentTeam == 0)
                    {
                        team = RelationshipManager.ServerInstance.CreateTeam();
                        team.AddPlayer(player);
                    }

                    var text = $"{PlugName}{lang.GetMessage("SENDINVITETEAM", this, player.UserIDString)}";
                    foreach (var ts in player1.friendList)
                    {
                        target = BasePlayer.Find(ts.Key.ToString());
                        if (target != null)
                        {
                            if (target.currentTeam == 0)
                            {
                                team.SendInvite(target);
                                text += $"{target.displayName}[{target.userID}] ";
                                SendReply(target,
                                    $"{PlugName}{lang.GetMessage("SENDINVITE", this, player.UserIDString)} {player.displayName}[{player.userID}]");
                            }
                        }
                    }

                    SendReply(player, text);
                    break;
            }
        }

        [ConsoleCommand("friendui2")]
        private void FriendConsole(ConsoleSystem.Arg arg)
        {
            if(arg.Args == null || arg.Args.Length < 1) return;
            FriendCmd(arg.Player(), "friend", arg.Args);
            if (arg.Args[0] == "set")
            {
                SettingInit(arg.Player(), ulong.Parse(arg.Args[2]));
            }
            if (arg.Args[0] == "remove")
            {
                StartUi(arg.Player());
            }
        }

        [ChatCommand("friend")]
        private void FriendCmd2(BasePlayer player, string command, string[] arg) => FriendCmd(player, command, arg);

        #endregion

        #region [Hooks]

        private void OnEntitySpawned(BuildingPrivlidge entity)
        {
            FriendData fData;
            if(!friendData.TryGetValue(entity.OwnerID, out fData)) return;
            foreach (var ids in fData.friendList.Where(p => p.Value.bp == true))
            {
                entity.authorizedPlayers.Add(new PlayerNameID()
                {
                    ShouldPool = true,
                    userid = ids.Key, 
                    username = ids.Value.name
                });
            }
        }
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            FriendData player1;
            var targetplayer = entity as BasePlayer;
            var attackerplayer = info.Initiator as BasePlayer;
            if (attackerplayer == null || targetplayer == null) return null;
            if (!friendData.TryGetValue(attackerplayer.userID, out player1)) return null;
            FriendData.FriendAcces ss;
            if (!player1.friendList.TryGetValue(targetplayer.userID, out ss)) return null;
            if (ss.Damage) return null;
            SendReply(attackerplayer, string.Format(lang.GetMessage("DAMAGE",this, attackerplayer.UserIDString),targetplayer.displayName ));
            return false;
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (entity == null || turret == null) return null;
            FriendData targetPlayer;
            var targetplayer = entity as BasePlayer;
            if (targetplayer == null) return null;
            if (!friendData.TryGetValue(turret.OwnerID, out targetPlayer)) return null;
            FriendData.FriendAcces ss;
            var owner = turret.authorizedPlayers.Exists(p => p.userid == turret.OwnerID);
            if (!owner) return null;
            if (!targetPlayer.friendList.TryGetValue(targetplayer.userID, out ss)) return null;
            if (!ss.Turret) return null;
            return false;
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (player == null || baseLock == null) return null;
            FriendData targetPlayer2;
            if (!friendData.TryGetValue(baseLock.OwnerID, out targetPlayer2)) return null;
            FriendData.FriendAcces ss;
            if (!targetPlayer2.friendList.TryGetValue(player.userID, out ss)) return null;
            if (!ss.Door) return null;
            return true;
        }

        private object OnSamSiteTarget(SamSite entity, BaseCombatEntity target)
        {
            if (!cfg.SSamOn) return null;
            if (entity == null || target == null) return null;
            FriendData targetPlayer;
            var targetpcopter = target as MiniCopter;
            var targetpcopterBig = target as ScrapTransportHelicopter;
            if (targetpcopter != null || targetpcopterBig != null)
            {
                var build = entity.GetBuildingPrivilege();
                if (build == null) return null;
                if (!build.authorizedPlayers.Exists(p => p.userid == entity.OwnerID)) return null;
                BasePlayer targePlayer = null;
                if(targetpcopter != null)targePlayer = targetpcopter.mountPoints[0].mountable._mounted;
                if(targetpcopterBig != null)targePlayer = targetpcopterBig.mountPoints[0].mountable._mounted;
                if (targePlayer == null) return null;
                if (entity.OwnerID == targePlayer.userID) return false;
                if (!friendData.TryGetValue(entity.OwnerID, out targetPlayer)) return null;
                FriendData.FriendAcces ss;
                if (!targetPlayer.friendList.TryGetValue(targePlayer.userID, out ss)) return null;
                if (!ss.Sam) return null; 
            }
            else
            {
                return null;
            }
            return false;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!friendData.ContainsKey(player.userID))
                friendData.Add(player.userID, new FriendData() {Name = player.displayName, friendList = { }});
        }

        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("SoFriends/FriendData"))
            {
                friendData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, FriendData>>("SoFriends/FriendData");
            }
            else
            {
                friendData = new Dictionary<ulong, FriendData>();
            }
            foreach (var basePlayer in BasePlayer.activePlayerList)
                OnPlayerConnected(basePlayer);
        }

        private void Unload() 
        {
            Interface.Oxide.DataFileSystem.WriteObject("SoFriends/FriendData", friendData);
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, LayerInvite);
                CuiHelper.DestroyUi(basePlayer, Layer);
            }
        }

        #endregion

        #region [UI]

        private static string Layer = "UISoFriends";
        private string LayerInvite = "UISoFriendsInv";
        private string Hud = "Hud";
        private string Overlay = "Overlay";
        private CuiPanel Fon = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
            CursorEnabled = true,
            Image = {Color = "0 0 0 0.87", Material = "assets/content/ui/uibackgroundblur.mat"}
        };

        private CuiPanel MainFon = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1920 -1080", OffsetMax = "1920 1080"},
            CursorEnabled = true,
            Image = {Color = "0 0 0 0"}
        };

        private CuiPanel MainPanel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.3333333 0.3333333", AnchorMax = "0.6664931 0.67"},
            Image = {Color = "0 0 0 0"}
        };
        
        private CuiElement ButtonList = new CuiElement()
        {
            Parent = Layer + "Panel",
            Components =
            {
                new CuiButtonComponent(){Color = "0.40 0.40 0.40 0", Command = "chat.say /fmenu", Sprite = "assets/content/ui/ui.background.tile.psd", Material = "assets/content/ui/uibackgroundblur.mat"},
                new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                new CuiRectTransformComponent(){AnchorMin = "0.223554 0.70132", AnchorMax = "0.4835852 0.7489916"}
            }
        };
        private CuiElement ButtonListText = new CuiElement()
        {
            Parent = Layer + "Panel",
            Components =
            {
                new CuiTextComponent(){Text = "СПИСОК ДРУЗЕЙ", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"},
                new CuiRectTransformComponent(){AnchorMin = "0.223554 0.70132", AnchorMax = "0.4835852 0.7489916"}
            }
        };
        private CuiButton SettingText = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.4283481 0.7700769", AnchorMax = "0.5560187 0.8122478"},
            Button = {Color = "0 0 0 0"},
            Text =
            {
                Text = "СИСТЕМА ДРУЗЕЙ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 20
            }
        };

        private CuiElement AddFriendsText = new CuiElement()
        {
            Parent = Layer + "Panel",
            Components =
            {
                new CuiTextComponent(){Text = "ДОБАВИТЬ ДРУГА", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"},
                new CuiRectTransformComponent(){AnchorMin = "0.4986922 0.70132", AnchorMax = "0.7587094 0.7489916"}
            }
        };
        private CuiElement AddFriends = new CuiElement()
        {
            Parent = Layer + "Panel",
            Components =
            {
                new CuiButtonComponent(){Color = "0.40 0.40 0.40 0", Command = "friendui addfriend", Sprite = "assets/content/ui/ui.background.tile.psd", Material = "assets/content/ui/uibackgroundblur.mat"},
                new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                new CuiRectTransformComponent(){AnchorMin = "0.4986922 0.70132", AnchorMax = "0.7587094 0.7489916"}
            }
        };

        private CuiButton CloseKrest = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.7310873 0.7719141", AnchorMax = "0.7571682 0.8168337"},
            Button =
            {
                Color = HexToRustFormat("#E10394"), Close = Layer, Sprite = "assets/icons/vote_down.png",
            },
            Text = {Text = "", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
        };
        private CuiButton ClosePanel = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.7310873 0.7719141", AnchorMax = "0.7571682 0.8168337"},
            Button =
            {
                Color = HexToRustFormat("#E10394"), Close = Layer, Sprite = "assets/icons/circle_open.png",
            },
            Text = {Text = "", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
        };

        private CuiPanel Invite = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-200 110", OffsetMax = "180 130"},
            Image = {Color = HexToRustFormat("#1b1b1bCC")}
        };

        private CuiButton DenyButton = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.552987 -0.9999999", AnchorMax = "0.9988059 -0.05645192"},
            Button = {Color = HexToRustFormat("#656565CC"), Command = "friendui2 deny"},
            Text = {Text = "ОТКЛОНИТЬ", Color = HexToRustFormat("#DEDEDEFF"), Align = TextAnchor.MiddleCenter}
        };

        private CuiButton AcceptButton = new CuiButton()
        {
            RectTransform = {AnchorMin = "-0.0014289 -0.9999999", AnchorMax = "0.4443915 -0.05645192"},
            Button = {Color = HexToRustFormat("#E10394"), Command = "friendui2 accept"},
            Text = {Text = "ПРИНЯТЬ", Color = HexToRustFormat("#DEDEDEFF"), Align = TextAnchor.MiddleCenter}
        };

        private CuiPanel StaticSetPanel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.5424429 0.2456912", AnchorMax = "0.686816 0.6563989"},
            Image =
            {
                Color = "0 0 0 0"
            }
        };

        CuiElement page0 = new CuiElement()
        {
            Parent = Layer + "Panel",
            Name = Layer + "Page",
            Components =
            {
                new CuiImageComponent() {Color = "0 0 0 0"},
                new CuiRectTransformComponent() {AnchorMin = "0.292861 0.2585258", AnchorMax = "0.3850963 0.2970295"}
            }
        };

        CuiElement page1 = new CuiElement()
        {
            Parent = Layer + "Page",
            Components =
            {
                new CuiImageComponent()
                {
                    Color = "0 0 0 0",
                    Material = "assets/content/ui/uibackgroundblur.mat"
                },
                new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                new CuiRectTransformComponent()
                    {AnchorMin = "0.005649818 0.05172569", AnchorMax = "0.2768343 0.9137955"}
            }
        };

        CuiElement page2 = new CuiElement()
        {
            Parent = Layer + "Page",
            Components =
            {
                new CuiImageComponent()
                {
                    Color = "0 0 0 0",
                    Material = "assets/content/ui/uibackgroundblur.mat"
                },
                new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                new CuiRectTransformComponent() {AnchorMin = "0.7118673 0.05172569", AnchorMax = "0.9830517 0.9137955"}
            }
        };
        CuiElement page3 = new CuiElement()
        {
            Parent = Layer + "Page",
            Components =
            {
                new CuiImageComponent()
                {
                    Color = HexToRustFormat("#00fff7cc"),
                    Material = "assets/content/ui/uibackgroundblur.mat"
                },
                new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                new CuiRectTransformComponent() {AnchorMin = "0.3672329 0.05172569", AnchorMax = "0.6384174 0.9137955"}
            }
        };
        private void InivteStart(BasePlayer inviter, BasePlayer target)
        {
            CuiHelper.DestroyUi(target, LayerInvite);
            var cont = new CuiElementContainer();
            cont.Add(Invite, Hud, LayerInvite);
            cont.Add(new CuiElement()
            {
                Parent = LayerInvite,
                Components = 
                {
                    new CuiTextComponent()
                    {
                        Text = $"У ВАС ЗАЯВКА В ДРУЗЬЯ ОТ <color=#00fff7>{inviter.displayName}</color>", Color = HexToRustFormat("#DEDEDEFF"),
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "1 1"},
                }
            });
            cont.Add(AcceptButton, LayerInvite);
            cont.Add(DenyButton, LayerInvite);
            CuiHelper.AddUi(target, cont);
        }

        [ChatCommand("fmenu")]
        private void StartUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var cont = new CuiElementContainer();
            cont.Add(Fon, Hud, Layer);
            cont.Add(MainFon, Layer, Layer + "off");
            cont.Add(MainPanel, Layer + "off", Layer + "Panel");
            cont.Add(ButtonListText);
            cont.Add(AddFriendsText);
            cont.Add(ButtonList);
            cont.Add(AddFriends);
            cont.Add(ClosePanel, Layer + "Panel");
            cont.Add(CloseKrest, Layer + "Panel");
            cont.Add(SettingText, Layer + "Panel");
            CuiHelper.AddUi(player, cont);
            FriendsInit(player, 1);
        }

        [ConsoleCommand("friendui")]
        private void FriendUI(ConsoleSystem.Arg arg)
        {
            var targetPlayer = arg?.Player();
            if (targetPlayer == null) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                StartUi(arg.Player());
                return;
            }

            switch (arg.Args[0])
            {
                case "page":
                    if (arg.Args[1].ToInt() < 1) return;
                    FriendsInit(targetPlayer, arg.Args[1].ToInt());
                    break;
                case "page2":
                    if (arg.Args[1].ToInt() < 1) return;
                    AddFriendList(targetPlayer, arg.Args[1].ToInt());
                    break;
                case "setting":
                    SettingInit(targetPlayer, ulong.Parse(arg.Args[1]));
                    break;
                case "addfriend":
                    AddFriendList(targetPlayer, 1);
                    break;
            }
        }

        private void AddFriendList(BasePlayer player, int page)
        {
            FriendData target;
            if (!friendData.TryGetValue(player.userID, out target)) return;
            CuiHelper.DestroyUi(player, Layer + "Set");
            CuiHelper.DestroyUi(player, Layer + "Page");
            CuiHelper.DestroyUi(player, Layer + "ToPanel");
            var cont = new CuiElementContainer();
            foreach (var friends in target.friendList.Select((i, t) => new {A = i, B = t}))
                CuiHelper.DestroyUi(player, Layer + "Panel" + friends.B);
            foreach (var friends in BasePlayer.activePlayerList.Where(p => p.userID != player.userID))
                CuiHelper.DestroyUi(player, Layer + "Players" + friends.userID);
            cont.Add(page0);
            cont.Add(page1);
            cont.Add(page2);
            cont.Add(page3);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.005649818 0.05172569", AnchorMax = "0.2768343 0.9137955"},
                Text = {Text = "«", Align = TextAnchor.MiddleCenter, FontSize = 14, Color = HexToRustFormat("#00fff7")},
                Button = {Color = "0 0 0 0", Command = $"friendui page2 {page - 1}"}
            }, Layer + "Page");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Page",
                Components =
                {
                    new CuiTextComponent() {Text = $"{page}", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent() {AnchorMin = "0.3672329 0.05172569", AnchorMax = "0.6384174 0.9137955"}
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.7118673 0.05172569", AnchorMax = "0.9830517 0.9137955"},
                Text = {Text = "»", Align = TextAnchor.MiddleCenter, FontSize = 14, Color = HexToRustFormat("#00fff7")},
                Button = {Color = "0 0 0 0", Command = $"friendui page2 {page + 1}"}
            }, Layer + "Page");
            foreach (var players in BasePlayer.activePlayerList.Where(p => p.userID != player.userID).Where(d => !target.friendList.ContainsKey(d.userID)).Select((i, t) => new {A = i, B = t - (page - 1) * 14}).Skip((page - 1) * 14).Take(14))
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Panel",
                    Name = Layer + "Players" + players.A.userID,
                    Components =
                    {
                        new CuiImageComponent()
                            {Color = "0.1 0.512312 0.213345 0", Material = "assets/content/ui/uibackgroundblur.mat"},
                        new CuiOutlineComponent() {Distance = "1 1", Color = "0.64 0.64 0.64 1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin =
                                $"{0.2944241 + players.B * 0.25 - Math.Floor((double) players.B / 2) * 2 * 0.25} {0.6123946  - Math.Floor((double) players.B / 2) * 0.05}",
                            AnchorMax =
                                $"{0.4533611 + players.B * 0.25 - Math.Floor((double) players.B / 2) * 2 * 0.25} {0.6508984 - Math.Floor((double) players.B / 2) * 0.05}"

                        }
                    }
                });
                cont.Add(new CuiButton
                { 
                    RectTransform =
                    {
                        AnchorMin = $"0 0",
                        AnchorMax = $"1 1"
                    },
                    Button =
                    {
                        Color = "0 0 0 0", Command = $"friendui2 add {players.A.userID}"
                    },
                    Text = {Text = $"{players.A.displayName}", Align = TextAnchor.MiddleCenter}
                }, Layer + "Players" + players.A.userID);
            }

            CuiHelper.AddUi(player, cont);
        }

        private void SettingInit(BasePlayer player, ulong steamdIdTarget)
        {
            FriendData.FriendAcces access;
            FriendData target;
            if (!friendData.TryGetValue(player.userID, out target)) return;
            if (!target.friendList.TryGetValue(steamdIdTarget, out access)) return;
            CuiHelper.DestroyUi(player, Layer + "Set");
            var cont = new CuiElementContainer();
            cont.Add(StaticSetPanel, Layer + "Panel", Layer + "Set");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Set",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                    },
                    new CuiOutlineComponent() {Color = HexToRustFormat("#E10394"), Distance = "1 1"},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.007406145 0.01562492", AnchorMax = "0.9855621 0.1088883"},
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.007406145 0.01562492", AnchorMax = "0.9855621 0.1088883"},
                Button =
                {
                    Color = "0 0.00 0.00 0", Command = $"friendui2 remove {steamdIdTarget}",
                },
                Text =
                {
                    Text = "УДАЛИТЬ ИЗ ДРУЗЕЙ", Align = TextAnchor.MiddleCenter, FontSize = 12,
                    Font = "robotocondensed-regular.ttf"
                }
            }, Layer + "Set");
            if (access.Damage)
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.9040182", AnchorMax = "0.4766316 1.001982"}
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.9040182", AnchorMax = "0.4766316 1.001982"},
                    Button = {Color = "0 0 0 0", Command = $"friendui2 set damage {steamdIdTarget}"},
                    Text = {Text = "УРОН", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }
            else
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = "0.40 0.40 0.40 1.00", Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.9040182", AnchorMax = "0.4766316 1.001982"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.9040182", AnchorMax = "0.4766316 1.001982"},
                    Button = {Color = "0 0 0 0", Command = $"friendui2 set damage {steamdIdTarget}"},
                    Text = {Text = "УРОН", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }

            if (access.Door)
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.7611616", AnchorMax = "0.4766316 0.8591254"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.7611616", AnchorMax = "0.4766316 0.8591254"},
                    Button = {Color = "0 0 0 0", Command = $"friendui2 set door {steamdIdTarget}"},
                    Text = {Text = "ДВЕРИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }
            else
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = "0.40 0.40 0.40 1.00", Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.7611616", AnchorMax = "0.4766316 0.8591254"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.7611616", AnchorMax = "0.4766316 0.8591254"},
                    Button = {Color = "0 0 0 0", Command = $"friendui2 set door {steamdIdTarget}"},
                    Text = {Text = "ДВЕРИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }

            if (access.Turret)
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.6250014", AnchorMax = "0.4766316 0.7229652"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.6250014", AnchorMax = "0.4766316 0.7229652"},
                    Button = {Color = "0 0 0 0", Command = $"friendui2 set turret {steamdIdTarget}"},
                    Text = {Text = "ТУРЕЛИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }
            else 
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = "0.40 0.40 0.40 1.00", Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.6250014", AnchorMax = "0.4766316 0.7229652"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.6250014", AnchorMax = "0.4766316 0.7229652"},
                    Button = {Color = "0 0 0 0", Command = $"friendui2 set turret {steamdIdTarget}"},
                    Text = {Text = "ТУРЕЛИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            } 
            if (access.bp)
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.4843769", AnchorMax = "0.4766316 0.5823407"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.4843769", AnchorMax = "0.4766316 0.5823407"},
                    Button =
                    {
                        Color = "0 0 0 0", Command = $"friendui2 set build {steamdIdTarget}"
                    },
                    Text = {Text = "ШКАФ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }
            else
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Set",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiOutlineComponent() {Color = "0.40 0.40 0.40 1.00", Distance = "1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.009811074 0.4843769", AnchorMax = "0.4766316 0.5823407"},
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.009811074 0.4843769", AnchorMax = "0.4766316 0.5823407"},
                    Button =
                    {
                        Color = "0 0 0 0", Command = $"friendui2 set build {steamdIdTarget}"
                    },
                    Text = {Text = "ШКАФ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                }, Layer + "Set");
            }
            if (cfg.SSamOn)
            {
                if (access.Sam)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Set",
                        Components =
                        {
                            new CuiImageComponent()
                            {
                                Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiOutlineComponent() {Color = HexToRustFormat("#00fff7"), Distance = "1 1"},
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.009811074 0.3437524", AnchorMax = "0.4766316 0.4417162"},
                        }
                    });
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = "0.009811074 0.3437524", AnchorMax = "0.4766316 0.4417162"},
                        Button =
                        {
                            Color = "0 0 0 0", Command = $"friendui2 set sam {steamdIdTarget}"
                        },
                        Text = {Text = "ПВО", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                    }, Layer + "Set");
                }
                else
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Set",
                        Components =
                        {
                            new CuiImageComponent()
                            {
                                Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiOutlineComponent() {Color = "0.40 0.40 0.40 1.00", Distance = "1 1"},
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.009811074 0.3437524", AnchorMax = "0.4766316 0.4417162"},
                        }
                    });
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = "0.009811074 0.3437524", AnchorMax = "0.4766316 0.4417162"},
                        Button =
                        {
                            Color = "0 0 0 0", Command = $"friendui2 set sam {steamdIdTarget}"
                        },
                        Text = {Text = "ПВО", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"}
                    }, Layer + "Set");
                }
            }

            CuiHelper.AddUi(player, cont);
        }

        private void AuthBuild(BasePlayer player, ulong friendId)
        {
            var friend = friendData[player.userID].friendList[friendId];
            foreach (BaseNetworkable entity in BaseNetworkable.serverEntities)
            {
                var bp = entity.GetComponent<BuildingPrivlidge>();
                if(bp == null) continue;
                if (bp.authorizedPlayers.Exists(p => p.userid == player.userID))
                {
                    if(bp.authorizedPlayers.Exists(p => p.userid == friendId)) continue;
                    bp.authorizedPlayers.Add(new PlayerNameID()
                    {
                        userid = friendId,
                        username = friend.name,
                        ShouldPool = true
                    });
                    bp.SendNetworkUpdate();
                }
            }
        }
        private void FriendsInit(BasePlayer player, int page)
        {
            CuiHelper.DestroyUi(player, Layer + "Page");
            var cont = new CuiElementContainer();
            FriendData targetPlayer;
            if (!friendData.TryGetValue(player.userID, out targetPlayer)) return;
            cont.Add(page0);
            cont.Add(page1);
            cont.Add(page2); 
            cont.Add(page3);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.005649818 0.05172569", AnchorMax = "0.2768343 0.9137955"},
                Text = {Text = "«", Align = TextAnchor.MiddleCenter, FontSize = 14, Color = HexToRustFormat("#00fff7")},
                Button = {Color = "0 0 0 0", Command = $"friendui page {page - 1}"}
            }, Layer + "Page");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Page",
                Components =
                {
                    new CuiTextComponent() {Text = $"{page}", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent() {AnchorMin = "0.3672329 0.05172569", AnchorMax = "0.6384174 0.9137955"}
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.7118673 0.05172569", AnchorMax = "0.9830517 0.9137955"},
                Text = {Text = "»", Align = TextAnchor.MiddleCenter, FontSize = 14, Color = HexToRustFormat("#00fff7")},
                Button = {Color = "0 0 0 0", Command = $"friendui page {page + 1}"}
            }, Layer + "Page");
            foreach (var friends in targetPlayer.friendList.Select((i, t) => new {A = i, B = t}))
                CuiHelper.DestroyUi(player, Layer + "Panel" + friends.B);
            foreach (var friends in targetPlayer.friendList.Select((i, t) => new {A = i, B = t - (page - 1) * 6})
                .Skip((page - 1) * 6).Take(6))
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Panel",
                    Name = Layer + "Panel" + friends.B,
                    Components =
                    {
                        new CuiImageComponent()
                            {Color = "0.1 0.512312 0.213345 0", Material = "assets/content/ui/uibackgroundblur.mat"},
                        new CuiOutlineComponent() {Distance = "1 1", Color = "0.64 0.64 0.64 1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.2944241 {0.6123946 - Math.Floor((double) friends.B) * 0.05}",
                            AnchorMax = $"0.4533611 {0.6508984 - Math.Floor((double) friends.B) * 0.05}",
                        }
                    }
                });
                var friend = BasePlayer.FindByID(friends.A.Key);
                if (friend == null)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Panel" + friends.B,
                        Components =
                        {
                            new CuiImageComponent(){Sprite = "assets/icons/circle_closed_toedge.png", Color = "0.64 0.64 0.64 1"},
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.06557404 0.3333297", AnchorMax = "0.1016398 0.6190448"}
                        }
                    });
                }
                else
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Panel" + friends.B,
                        Components =
                        {
                            new CuiImageComponent(){Sprite = "assets/icons/circle_closed_toedge.png", Color = HexToRustFormat("#00fff7")},
                            new CuiRectTransformComponent() {AnchorMin = "0.06557404 0.3333297", AnchorMax = "0.1016398 0.6190448"}
                        }
                    });
                }
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Panel" + friends.B,
                    Components =
                    {
                        new CuiTextComponent() {Text = $"{friends.A.Value.name}", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf",},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.1572777 0.1764694", AnchorMax = "0.9672127 0.8627409"}
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "1.049078 -0.04762325", AnchorMax = "1.203278 1.047622"},
                    Button =
                    {
                        Color = "1 1 1 1", Sprite = "assets/icons/circle_open.png",
                        Command = $""
                    },
                    Text = {Text = string.Empty}
                }, Layer + "Panel" + friends.B, Layer + "Tool" + friends.B );
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.2345498 0.1956543", AnchorMax = "0.7661109 0.7608694"},
                    Button =
                    {
                        Color = "1 1 1 1", Sprite = "assets/icons/tools.png",
                        Command = $"friendui setting {friends.A.Key}"
                    },
                    Text = {Text = string.Empty}
                }, Layer + "Tool" + friends.B);
            }

            CuiHelper.AddUi(player, cont);
        }

        #endregion

        #region [Help]

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion 
        #region API

        private bool HasFriend(ulong playerId, ulong friendId)
        {
            return friendData[playerId].friendList.ContainsKey(friendId);
        }
        private bool AreFriends(ulong playerId, ulong friendId)
        {
            return friendData[playerId].friendList.ContainsKey(friendId) && friendData[friendId].friendList.ContainsKey(playerId);;
        }
        private bool AddFriend(ulong playerId, ulong friendId)
        {
            if (friendData[playerId].friendList.ContainsKey(friendId)) return false;
            return friendData[playerId].friendList.TryAdd(friendId, new FriendData.FriendAcces()
            {
                name = BasePlayer.FindByID(friendId) ? BasePlayer.FindByID(friendId).displayName : "НЕИЗВЕСТНЫЙ",
                Damage = cfg.SDamage, Door = cfg.SDoor, Turret = cfg.STurret,
                Sam = cfg.SSam
            });
        }
        private bool RemoveFriend(ulong playerId, ulong friendId)
        {
            if (!friendData[playerId].friendList.ContainsKey(friendId)) return false;
            return friendData[playerId].friendList.Remove(friendId);
        }
        private bool IsFriend(ulong playerId, ulong friendId)
        {
            return friendData[playerId].friendList.ContainsKey(friendId);
        }
        private int GetMaxFriends()
        {
            return cfg.MaxFriends;
        }
        private ulong[] GetFriends(ulong playerId)
        {
            FriendData playerData;
            if (!friendData.TryGetValue(playerId, out playerData)) return new ulong[0];
            var test = Pool.GetList<ulong>();
            foreach (var friendId in playerData.friendList)
            {
                test.Add(friendId.Key);
            }
            return test.ToArray();
        }

        private ulong[] GetFriendList(string playerId)
        {
            FriendData playerData;
            if (!friendData.TryGetValue(ulong.Parse(playerId), out playerData)) return new ulong[0];
            List<ulong> players = new List<ulong>();
            foreach (var friendId in playerData.friendList)
            {
                players.Add(friendId.Key);
            }
            return players.ToArray();
        }

        private ulong[] GetFriendList(ulong playerId)
        {
            return GetFriendList(playerId.ToString()).ToArray();
        }

        private ulong[] IsFriendOf(ulong playerId)
        {
            FriendData friend;
            return friendData.TryGetValue(playerId, out friend) ? friend.friendList.Keys.ToArray() : new ulong[0];
        }
        #endregion
    }
}
