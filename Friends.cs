    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Globalization;
    using Oxide.Core;
    using Newtonsoft.Json;
    using Oxide.Core.Plugins;
    using Oxide.Game.Rust.Cui;
    using ProtoBuf;
    using UnityEngine;

    namespace Oxide.Plugins
    {
        /* Based on version 1.0.5 by Nimant */
        [Info("Friends", "Nimant, Ryamkk", "1.1.0")]
        class Friends : RustPlugin
        {
            [PluginReference] private Plugin ImageLibrary;
            
            public string Sprite = "assets/content/ui/ui.background.tile.psd";
            public string Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
            
            public string Title = "ПОИСК ТИМЕЙТА";
            public string Description = "Выберите игрока из списка либо воспользуйтесь быстрым поиском";
            
            private static Dictionary<ulong, List<ulong>> FriendsData = new Dictionary<ulong, List<ulong>>();
            private static Dictionary<ulong, List<ulong>> FriendsWipeData = new Dictionary<ulong, List<ulong>>();
            private static Dictionary<ulong, PlayerInfo>  PlayerTempData = new Dictionary<ulong, PlayerInfo>();
            private static Dictionary<ulong, List<ulong>> FriendsWaitAccept = new Dictionary<ulong, List<ulong>>();
            private static Dictionary<ulong, List<PlayerEntry>> friends = new Dictionary<ulong, List<PlayerEntry>>();
            
            private class PlayerEntry
            {
                public string name;
                public ulong id;
            }

            public string Layer = "Friend.Menu";
            public string LayerSearch = "Friend.Search";
            public class ButtonEntry
            {
                public string Name;
                public string Sprite;
                public string Command;
                public string Color;
            }
            
            private class PlayerInfo
            {
                public Dictionary<ulong, long> RemoveFriends = new Dictionary<ulong, long>();
                public int CountAddFriends;
                public long LastRemoveFriends;
                
                public bool TurrentAuthorization = false;
                public bool AttackFriend = false;
                public bool CodeAuthorization = false;
            }

            private void Init()
            {
                LoadVariables();
                LoadData();
                LoadWipeData();
                LoadTmpData();          
                LoadDefaultMessages();
            }
            
            private void Unload()
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, LayerSearch);
                    CuiHelper.DestroyUi(player, Layer);
                }
                
                SaveWipeData();
            }
            
            private void OnServerSave() => SaveWipeData();
            
            private void OnNewSave()
            {                               
                PlayerTempData.Clear();
                SaveTmpData();
                
                foreach(var pair in FriendsWipeData)
                {
                    if (FriendsData.ContainsKey(pair.Key)) FriendsData[pair.Key] = pair.Value;
                    else FriendsData.Add(pair.Key, pair.Value);
                }
                SaveData();
                
                FriendsWipeData.Clear();
                SaveWipeData();
            }
            
            private void OnPlayerConnected(BasePlayer player) => LoadFriendList(player.userID);     
            
            private void OnServerInitialized()
            {
                if (5 > 0) timer.Every(5, () => { friends.Clear(); });
                
                foreach (var player in BasePlayer.activePlayerList)  
                    OnPlayerConnected(player);                                    
            }
            
            private object OnTurretTarget(AutoTurret turret, BaseCombatEntity targ)
            {
                if (!ShareCodeLocks || !(targ is BasePlayer) || turret.OwnerID <= 0) return null;
                var player = (BasePlayer) targ;
                if (turret.IsAuthed(player) || !HasFriend(turret.OwnerID, player.userID)) return null;
                turret.authorizedPlayers.Add(new PlayerNameID
                {
                    userid = player.userID,
                    username = player.displayName
                });
                return false;
            }
            
            public Dictionary<BasePlayer, int> CooldownList = new Dictionary<BasePlayer, int>();
            
            private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            
            private bool FriendsSD(BasePlayer player, ulong friend)
            {
                return RelationshipManager.Instance.FindTeam(player.userID)?.members?.Contains(friend) == true;
            }
            
            void OnEntityTakeDamage(BaseCombatEntity vic, HitInfo info)
            {
                try
                {
                    if (vic != null && vic is BasePlayer && info?.Initiator != null && info.Initiator is BasePlayer && vic != info.Initiator)
                    {
                        BasePlayer vitim = vic as BasePlayer;
                        if (vitim == null) return;
                        BasePlayer iniciator = info.Initiator as BasePlayer;
                        if (iniciator == null) return;
                        if (HasFriend(iniciator.userID, vitim.userID))
                        {
                            if (IsFriend(iniciator.userID, vitim.userID))
                            {
                                info.damageTypes?.ScaleAll(0f);
                                iniciator.ChatMessage(string.Format($"Игрок {0} Ваш друг, вы неможете его ранить.", vitim.displayName));
                            }
                        }
                    }
                }
                catch (NullReferenceException)
                {
                }
            }

            private bool ShareCodeLocks = true;
            private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
            {
                if (!ShareCodeLocks || !(@lock is CodeLock) || @lock.GetParentEntity().OwnerID <= 0) return null;
                if (HasFriend(@lock.GetParentEntity().OwnerID, player.userID))
                {
                    if (IsFriend(@lock.GetParentEntity().OwnerID, player.userID))
                    {
                        var codeLock = @lock as CodeLock;
                        var whitelistPlayers = (List<ulong>)codeLock.whitelistPlayers;
                        if (!whitelistPlayers.Contains(player.userID)) whitelistPlayers.Add(player.userID);
                    }
                    return null;
                }
                return null;
            }

            private readonly object True = true;

            private object OnSamSiteTarget(SamSite samSite, BaseCombatEntity target)
            {
                var mountPoints = (target as BaseVehicle)?.mountPoints;
                if (!IsOccupied(target, mountPoints))
                    return True;

                if (samSite.staticRespawn)
                    return null;

                var cupboard = samSite.GetBuildingPrivilege(samSite.WorldSpaceBounds());
                if ((object)cupboard == null)
                    return null;

                if (mountPoints != null)
                {
                    foreach (var mountPoint in mountPoints)
                    {
                        var player = mountPoint.mountable.GetMounted();
                        if ((object)player != null && IsAuthed(cupboard, player.userID))
                            return True;
                    }
                }

                foreach (var child in target.children)
                {
                    var player = child as BasePlayer;
                    if ((object)player != null)
                    {
                        if (IsAuthed(cupboard, player.userID))
                            return True;
                    }
                }

                return null;
            }

            private static bool IsOccupied(BaseCombatEntity entity, List<BaseVehicle.MountPointInfo> mountPoints)
            {
                if (mountPoints != null)
                {
                    foreach (var mountPoint in mountPoints)
                    {
                        var player = mountPoint.mountable.GetMounted();
                        if ((object)player != null)
                            return true;
                    }
                }

                foreach (var child in entity.children)
                {
                    if (child is BasePlayer)
                        return true;
                }

                return false;
            }

            private static bool IsAuthed(BuildingPrivlidge cupboard, ulong userId)
            {
                foreach (var entry in cupboard.authorizedPlayers)
                {
                    if (entry.userid == userId)
                        return true;
                }

                return false;
            }
            
            private void FriendUI(BasePlayer player)
            {
                CuiElementContainer container = new CuiElementContainer();
                List<ButtonEntry> buttons = new List<ButtonEntry>();
                
                var friends = GetFriends(player);
                
                if (friends.Count < 3)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = "Поиск друзей",
                        Command = "friend.cmd search",
                        Sprite = "assets/icons/examine.png",
                        Color = "#EA2E5D"
                    });
                }
                
                if (friends.Count > 0)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = "Удалить всех друзей",
                        Command = "friend.cmd removeall",
                        Sprite = "assets/icons/clear_list.png",
                        Color = "#EA2E5D"
                    });
                }

                if (friends.Count > 0)
                {
                    foreach (var friend in GetFriends(player.userID))
                    {
                        var covFriend = covalence.Players.FindPlayerById(friend.ToString()); 
                        if (covFriend == null) continue;
                        
                        buttons.Add(new ButtonEntry
                        {
                            Name = $"{covFriend.Name}\nНажми чтобы удалить",
                            Command = $"friend.cmd remove {covFriend.Id}",
                            Sprite = "assets/icons/friends_servers.png",
                            Color = "#EA2E5D"
                        });
                    }
                }

                foreach(var pair in FriendsWaitAccept.Where(x=> x.Value.Contains(player.userID)).ToDictionary(x=> x.Key, x=> x.Value))
                {
                    if (pair.Key == player.userID)
                    {
                        buttons.Add(new ButtonEntry
                        {
                            Name = "Добавить в друзья",
                            Command = "friend.cmd accept",
                            Sprite = "assets/icons/vote_up.png",
                            Color = "#EA2E5D"
                        });

                        buttons.Add(new ButtonEntry
                        {
                            Name = "Отклонить запрос",
                            Command = "friend.cmd deny",
                            Sprite = "assets/icons/vote_down.png",
                            Color = "#EA2E5D"
                        });
                    }

                    if (pair.Key != player.userID)
                    {
                        buttons.Add(new ButtonEntry
                        {
                            Name = "Добавить в друзья",
                            Command = "friend.cmd accept",
                            Sprite = "assets/icons/vote_up.png",
                            Color = "#EA2E5D"
                        });

                        buttons.Add(new ButtonEntry
                        {
                            Name = "Отклонить запрос",
                            Command = "friend.cmd deny",
                            Sprite = "assets/icons/vote_down.png",
                            Color = "#EA2E5D"
                        });
                    }
                }

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5" },
                }, "Hud", Layer);

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiButtonComponent { Close = Layer, Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1000 -1000", OffsetMax = "1000 1000" }
                    }
                });

                for (var i = 0; i < buttons.Count; i++) 
                {
                    var button = buttons[i];
                    
                    var r = buttons.Count * 10 + 25;
                    var c = (double) buttons.Count / 2;
                    var pos = i / c * Math.PI;    
                    var x = r * Math.Sin(pos);
                    var y = r * Math.Cos(pos);

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + $".{i}",
                        Components =
                        {
                            new CuiImageComponent { Sprite = "assets/icons/circle_gradient.png", Color = HexToCuiColor(button.Color) },
                            new CuiRectTransformComponent { AnchorMin = $"{x - 35} {y - 35}", AnchorMax = $"{x + 35} {y + 35}" },
                        },
                    });
                    
                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".{i}",
                        Components =
                        {
                            new CuiImageComponent { Sprite = button.Sprite, Color = HexToCuiColor("#FFFFFF3F") },
                            new CuiRectTransformComponent { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.8" },
                        },
                    });
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = button.Name, FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFFCA"), Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0" }
                    }, Layer + $".{i}"); 
                    
                    container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = $"{button.Command}", Close = Layer },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { Text = "" }
                    }, Layer + $".{i}");
                }

                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.AddUi(player, container);
            }

            private void FriendSearchUI(BasePlayer player, int page = 0, bool reopen = false)
            {
                CuiElementContainer container = new CuiElementContainer();

                if (!reopen)
                {
                    CuiHelper.DestroyUi(player, LayerSearch);
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image = { Color = HexToCuiColor("#2E2E2E7D"), Sprite = Sprite, Material = Material },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-420 -270", OffsetMax = "420 270" },
                    }, "Hud", LayerSearch);
                }

                container.Add(new CuiElement
                {
                    Parent = LayerSearch,
                    Components =
                    {
                        new CuiButtonComponent { Close = LayerSearch, Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1000 -1000", OffsetMax = "1000 1000" }
                    }
                });
                
                CuiHelper.DestroyUi(player, LayerSearch + ".Title");
                container.Add(new CuiPanel
                { 
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0.5 0.95", AnchorMax = "0.5 0.95", OffsetMin = "-420 -13", OffsetMax = "420 23" },
                    Image = { Color = "0 0 0 0" }
                }, LayerSearch, LayerSearch + ".Title");
                
                container.Add(new CuiElement
                {
                    Parent = LayerSearch + ".Title",
                    Components =
                    {
                        new CuiTextComponent { Text = Title, Color = "1 1 1 1", Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = LayerSearch + ".Title",
                    Components =
                    {
                        new CuiTextComponent { Text = Description, Color = "1 1 1 1", FontSize = 12, Align = TextAnchor.LowerCenter, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                    }
                });

                CuiHelper.DestroyUi(player, LayerSearch + ".BG");
                container.Add(new CuiPanel()
                { 
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -260", OffsetMax = "400 230" },
                    Image = { Color = "0 0 0 0" }
                }, LayerSearch, LayerSearch + ".BG");
                
                container.Add(new CuiPanel
                { 
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0.5 0.96", AnchorMax = "0.5 0.96", OffsetMin = "-300 -12", OffsetMax = "300 15" },
                    Image = { Color = "0 0 0 0.5", Sprite = Sprite, Material = Material }
                }, LayerSearch + ".BG", LayerSearch + ".Input");
                
                container.Add(new CuiElement
                {
                    Parent = LayerSearch + ".Input",
                    Components =
                    {
                        new CuiInputFieldComponent { Text = "", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Command = "friend.cmd add " },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                    }
                });

                int i = 0;
                int Number = 24;
                
                foreach (var check in BasePlayer.activePlayerList.Skip(page * Number).Take(Number))
                {
                    var list = BasePlayer.activePlayerList.Skip(page * Number).Take(Number).Count();
                    
                    float DistanceBetweenA = 0.006f; 
                    float DistanceBetweenB = -0.007f;     
                    float ToRaiseORLower = 0.71f;   
                    float AmountUIonLine = 0.05f; 
                    float StretchUP = 0.205f; 
                    float StretchSide = 0.14f;     
                    float MoveUIinLeft = 0.0050f; 
                    
                    float[] pos = SquarePos(i, list, AmountUIonLine, ToRaiseORLower, StretchSide, StretchUP, DistanceBetweenA, DistanceBetweenB, MoveUIinLeft);

                    if (!reopen) CuiHelper.DestroyUi(player, LayerSearch + $".Avatar.{check.displayName}");
                    
                    container.Add(new CuiElement
                    {
                        Parent = LayerSearch + ".BG",
                        Name = LayerSearch + $".Avatar.{check.displayName}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary?.Call("GetImage", check.UserIDString) },
                            new CuiRectTransformComponent { AnchorMin = $"{pos[0]} {pos[1]}", AnchorMax = $"{pos[2]} {pos[3]}" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0.3", Command = $"friend.cmd add {check.displayName}" },
                        Text = { Text = $"{check.displayName}", FontSize = 16, Align = TextAnchor.LowerLeft, Color = HexToCuiColor("#FFFFFFFF"), Font = "robotocondensed-bold.ttf" },
                    }, LayerSearch + $".Avatar.{check.displayName}");
                    i++;
                }

                string leftCommand = $"friend.cmd page {page - 1}";
                string rightCommand = $"friend.cmd page {page + 1}";
                bool leftActive = page > 0;
                bool rightActive = (page + 1) * Number < BasePlayer.activePlayerList.Count;

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.49 0.04", AnchorMax = "0.49 0.04", OffsetMin = "-100 -15", OffsetMax = "2 15" },
                    Text = { Text = "←", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 },
                    Button = { Command = leftActive ? leftCommand : "", Color = leftActive ? "0.294 0.38 0.168 1" : "0.294 0.38 0.168 0.3", Sprite = Sprite, Material = Material }
                }, LayerSearch + ".BG");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.51 0.04", AnchorMax = "0.51 0.04", OffsetMin = "-2 -15", OffsetMax = "100 15" },
                    Text = { Text = "→", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 },
                    Button = { Command = rightActive ? rightCommand : "", Color = rightActive ? "0.294 0.38 0.168 1" : "0.294 0.38 0.168 0.3", Sprite = Sprite, Material = Material }
                }, LayerSearch + ".BG");
                
                CuiHelper.AddUi(player, container);
            }

            [ConsoleCommand("friend.cmd")]
            private void FriendsCMD(ConsoleSystem.Arg args)
            {
                var player = args.Player();
                if (!player || !args.HasArgs(1)) return;
                
                switch (args.Args[0].ToLower())
                {
                    case "page":
                    {
                        int page = 0;
                        
                        if (!args.HasArgs(2) || !int.TryParse(args.Args[1], out page)) return;
                        if (page < 0) return;
                        
                        FriendSearchUI(player, page, true);
                        break;
                    }
                    
                    case "add":
                    {
                        if (!args.HasArgs(2)) return;
                        CuiHelper.DestroyUi(player, LayerSearch);
                        player.Command($"chat.say \"/friend add {args.Args[1]}\"");
                        break;
                    }
                    
                    case "remove":
                    {
                        if (!args.HasArgs(2)) return;
                        player.Command($"chat.say \"/friend remove {args.Args[1]}\"");
                        break;
                    }


                    case "removeall":
                    {
                        if (!FriendsWipeData.ContainsKey(player.userID) || FriendsWipeData[player.userID].Count == 0)
                        {
                            GetMsg(player, "YOU.NOT.FRIEND");
                            return;
                        }

                        if (!PlayerTempData.ContainsKey(player.userID))
                            PlayerTempData.Add(player.userID, new PlayerInfo());

                        foreach (var friendID in FriendsWipeData[player.userID].ToList())
                        {
                            FriendsWipeData[player.userID].Remove(friendID);

                            if (FriendsWipeData.ContainsKey(friendID))
                                FriendsWipeData[friendID].Remove(player.userID);
                            else
                            {
                                LoadFriendList(friendID);
                                if (FriendsWipeData.ContainsKey(friendID))
                                    FriendsWipeData[friendID].Remove(player.userID);
                            }

                            var friend = BasePlayer.FindByID(friendID);

                            GetMsg(friend, "FRIEND.REMOVED.YOU", new List<object>() {player.displayName});

                            if (!PlayerTempData[player.userID].RemoveFriends.ContainsKey(friendID))
                                PlayerTempData[player.userID].RemoveFriends.Add(friendID, ToEpochTime(DateTime.Now));
                            else
                                PlayerTempData[player.userID].RemoveFriends[friendID] = ToEpochTime(DateTime.Now);

                            if (!PlayerTempData.ContainsKey(friendID))
                                PlayerTempData.Add(friendID, new PlayerInfo());

                            if (!PlayerTempData[friendID].RemoveFriends.ContainsKey(player.userID))
                                PlayerTempData[friendID].RemoveFriends.Add(player.userID, ToEpochTime(DateTime.Now));
                            else
                                PlayerTempData[friendID].RemoveFriends[player.userID] = ToEpochTime(DateTime.Now);

                            PlayerTempData[friendID].LastRemoveFriends = ToEpochTime(DateTime.Now);

                            Interface.Oxide.CallHook("OnFriendRemoved", player.userID.ToString(), friendID.ToString());
                            Interface.Oxide.CallHook("OnFriendRemoved", friendID.ToString(), player.userID.ToString());

                            CallSomeOvhHooks(player.userID);
                            CallSomeOvhHooks(friendID);
                        }

                        PlayerTempData[player.userID].LastRemoveFriends = ToEpochTime(DateTime.Now);
                        GetMsg(player, "YOU.REMOVED.FRIENDS");

                        SaveTmpData();
                        return;
                    }

                    case "accept":
                    {
                        foreach (var pair in FriendsWaitAccept.Where(x => x.Value.Contains(player.userID))
                            .ToDictionary(x => x.Key, x => x.Value))
                        {
                            if (!FriendsWipeData.ContainsKey(pair.Key))
                                FriendsWipeData.Add(pair.Key, new List<ulong>());

                            FriendsWipeData[pair.Key].Add(player.userID);

                            if (!FriendsWipeData.ContainsKey(player.userID))
                                FriendsWipeData.Add(player.userID, new List<ulong>());

                            FriendsWipeData[player.userID].Add(pair.Key);

                            (FriendsWaitAccept[pair.Key]).Remove(player.userID);

                            var friend = BasePlayer.FindByID(pair.Key);
                            var friendName = FindPlayerName(pair.Key);

                            GetMsg(player, "YOU.ADDED.PLAYER", new List<object>() {friendName});
                            GetMsg(friend, "PLAYER.ADDED.YOU", new List<object>() {player.displayName});

                            if (!PlayerTempData.ContainsKey(player.userID))
                                PlayerTempData.Add(player.userID, new PlayerInfo());

                            PlayerTempData[player.userID].CountAddFriends++;

                            if (!PlayerTempData.ContainsKey(pair.Key))
                                PlayerTempData.Add(pair.Key, new PlayerInfo());

                            PlayerTempData[pair.Key].CountAddFriends++;

                            /*GetMsg(player, "PLAYER.ADDED.YOU.LIMIT",
                                new List<object>()
                                    {PlayerTempData[player.userID].CountAddFriends, configData.LimitToAddFrinds});
                            GetMsg(friend, "PLAYER.ADDED.YOU.LIMIT",
                                new List<object>()*/
                                    /*{PlayerTempData[pair.Key].CountAddFriends, configData.LimitToAddFrinds});*/

                            Interface.Oxide.CallHook("OnFriendAdded", player.userID.ToString(), pair.Key.ToString());
                            Interface.Oxide.CallHook("OnFriendAdded", pair.Key.ToString(), player.userID.ToString());

                            CallSomeOvhHooks(player.userID);
                            CallSomeOvhHooks(pair.Key);

                            SaveTmpData();
                            return;
                        }

                        GetMsg(player, "YOU.NO.REQUEST");
                        return;
                    }

                    case "deny":
                    {
                        foreach(var pair in FriendsWaitAccept.Where(x=> x.Value.Contains(player.userID)).ToDictionary(x=> x.Key, x=> x.Value))
                        {                                                                               
                            (FriendsWaitAccept[pair.Key]).Remove(player.userID);    

                            var friend = BasePlayer.FindByID(pair.Key);                 
                        
                            GetMsg(player, "YOU.REFUSE.REQUEST");                   
                            GetMsg(friend, "PLAYER.REFUSE.REQUEST", new List<object>() { player.displayName }); 
                                                    
                            return;
                        }
                    
                        GetMsg(player, "YOU.NO.REQUEST");
                        return;  
                    }
                    
                    case "search":
                    {
                        FriendSearchUI(player, 0);
                        break;
                    }
                }
            }

            [ChatCommand("friend")]
            private void ChatFriend(BasePlayer player, string command, string[] args)
            {           
                if (player == null) return;                         
                
                if (args == null || args.Length < 1)
                {
                    FriendUI(player);
                    GetMsg(player, "CMD.FRIEND.HELP");
                    return;
                }
                
                if (args[0].ToLower() == "list")
                {               
                    if (!FriendsWipeData.ContainsKey(player.userID) || FriendsWipeData[player.userID].Count == 0)
                    {
                        GetMsg(player, "YOU.NOT.FRIEND");
                        return;
                    }   
                    
                    string friendList = "";
                    int count = 0;
                    var players = BasePlayer.activePlayerList.ToList();
                    
                    foreach(var friend in FriendsWipeData[player.userID].Where(x=> players.Exists(y=> y != null && y.userID == x)))
                    {
                        if (count >= 15)
                        {
                            friendList += " и другие...";
                            break;
                        }
                        friendList += " <color=#aae9f2>*</color> " + /*"<color=#90EE90>"+*/FindPlayerName(friend, true)/*+"</color>"*/ + "\n";
                        count++;
                    }
                        
                    if (count < 15)
                    {
                        foreach(var friend in FriendsWipeData[player.userID].Where(x=> !players.Exists(y=> y != null && y.userID == x)))
                        {
                            if (count >= 15)
                            {
                                friendList += " и другие...";
                                break;
                            }
                            friendList += " <color=#aae9f2>*</color> " + /*"<color=#FFA07A>"+*/FindPlayerName(friend, true)/*+"</color>\n"*/ + "\n";
                            count++;
                        }
                    }
                    
                    GetMsg(player, "FRIEND.LIST", new List<object>() { friendList.Trim('\n') });
                    return;
                }
                
                if (args[0].ToLower() == "add")
                {
                    if (args.Length < 2)
                    {                   
                        GetMsg(player, "FRIEND.ADD.HELP");
                        return;
                    }
                                    
                    if (FriendsWipeData.ContainsKey(player.userID) && FriendsWipeData[player.userID].Count >= configData.MaxFriends)
                    {
                        GetMsg(player, "YOUR.FRIENDS.LIMIT", new List<object>() { configData.MaxFriends });
                        return;
                    }   
                                    
                    string nameOrId = "";
                    for(int ii=1;ii<args.Length;ii++)
                        nameOrId += args[ii] + " ";
                    nameOrId = nameOrId.Trim(' ');
                                    
                    var friend = FindOnlinePlayer(player, nameOrId);
                    if (friend == null) return;                             
                    
                    if (friend.userID == player.userID)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.YOURSELF");                   
                        return;
                    }
                    
                    if (FriendsWipeData.ContainsKey(player.userID) && FriendsWipeData[player.userID].Contains(friend.userID))
                    {
                        GetMsg(player, "PLAYER.ALREADY.FRIENDS", new List<object>() { friend.displayName });
                        return;
                    } 
                    
                    if (FriendsWaitAccept.ContainsKey(player.userID) && FriendsWaitAccept[player.userID].Contains(friend.userID))
                    {
                        GetMsg(player, "YOUR.ACTIVE.REQUEST", new List<object>() { friend.displayName });
                        return;
                    }
                    
                    if (FriendsWaitAccept.ContainsKey(friend.userID) && FriendsWaitAccept[friend.userID].Contains(player.userID))
                    {
                        GetMsg(player, "PLAYER.SEND.YOU.REQUEST", new List<object>() { friend.displayName });
                        return;
                    }                               
                    
                    foreach(var pair in FriendsWaitAccept.Where(x=> x.Value.Contains(friend.userID)))
                    {
                        GetMsg(player, "PLAYER.ACTIVE.REQUEST", new List<object>() { friend.displayName, FindPlayerName(pair.Key) });
                        return;
                    }   
                    
                    if (FriendsWipeData.ContainsKey(friend.userID) && FriendsWipeData[friend.userID].Count >= configData.MaxFriends)
                    {
                        GetMsg(player, "PLAYER.FRIENDS.LIMIT", new List<object>() { friend.displayName, configData.MaxFriends });
                        return;
                    }
                                    
                    if (PlayerTempData.ContainsKey(player.userID) && (ToEpochTime(DateTime.Now) - PlayerTempData[player.userID].LastRemoveFriends) <= configData.BlockTimeToAddAgain * 60)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.YOU.COOLDOWN", new List<object>() { GetTime(configData.BlockTimeToAddAgain * 60 - (ToEpochTime(DateTime.Now) - PlayerTempData[player.userID].LastRemoveFriends)) });
                        return;
                    }
                    
                    if (PlayerTempData.ContainsKey(friend.userID) && (ToEpochTime(DateTime.Now) - PlayerTempData[friend.userID].LastRemoveFriends) <= configData.BlockTimeToAddAgain * 60)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.TARGET.COOLDOWN", new List<object>() { friend.displayName, GetTime(configData.BlockTimeToAddAgain * 60 - (ToEpochTime(DateTime.Now) - PlayerTempData[friend.userID].LastRemoveFriends)) });
                        return;
                    }
                    
                    if (PlayerTempData.ContainsKey(player.userID) && PlayerTempData[player.userID].RemoveFriends.ContainsKey(friend.userID) && (ToEpochTime(DateTime.Now) - PlayerTempData[player.userID].RemoveFriends[friend.userID]) <= configData.BlockTimeToAddFriendAgain * 60)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.YOU.COOLDOWN.OLD.FRIENDS", new List<object>() { friend.displayName, GetTime(configData.BlockTimeToAddFriendAgain * 60 - (ToEpochTime(DateTime.Now) - PlayerTempData[player.userID].RemoveFriends[friend.userID])) });
                        return;
                    }
                    
                    if (PlayerTempData.ContainsKey(friend.userID) && PlayerTempData[friend.userID].RemoveFriends.ContainsKey(player.userID) && (ToEpochTime(DateTime.Now) - PlayerTempData[friend.userID].RemoveFriends[player.userID]) <= configData.BlockTimeToAddFriendAgain * 60)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.TARGET.COOLDOWN.OLD.FRIENDS", new List<object>() { friend.displayName, GetTime(configData.BlockTimeToAddFriendAgain * 60 - (ToEpochTime(DateTime.Now) - PlayerTempData[friend.userID].RemoveFriends[player.userID])) });
                        return;
                    }
                    
/*                    if (PlayerTempData.ContainsKey(player.userID) && PlayerTempData[player.userID].CountAddFriends >= configData.LimitToAddFrinds)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.YOU.LIMIT.ADD.FRIENDS");
                        return;
                    }*/
                    
                    if (PlayerTempData.ContainsKey(friend.userID) && PlayerTempData[friend.userID].CountAddFriends >= configData.LimitToAddFrinds)
                    {
                        GetMsg(player, "CANT.SEND.REQUEST.TARGET.LIMIT.ADD.FRIENDS", new List<object>() { friend.displayName } );
                        return;
                    }
                    
                    TryAddFrind(player, friend);                                                                                
                    return;
                }   
                
                if (args[0].ToLower() == "accept")
                {               
                    foreach(var pair in FriendsWaitAccept.Where(x=> x.Value.Contains(player.userID)).ToDictionary(x=> x.Key, x=> x.Value))
                    {
                        if (!FriendsWipeData.ContainsKey(pair.Key))
                            FriendsWipeData.Add(pair.Key, new List<ulong>());
                        
                        FriendsWipeData[pair.Key].Add(player.userID);
                        
                        if (!FriendsWipeData.ContainsKey(player.userID))
                            FriendsWipeData.Add(player.userID, new List<ulong>());
                        
                        FriendsWipeData[player.userID].Add(pair.Key);
                                            
                        (FriendsWaitAccept[pair.Key]).Remove(player.userID);                                        
                                            
                        var friend = BasePlayer.FindByID(pair.Key);
                        var friendName = FindPlayerName(pair.Key);
                        
                        GetMsg(player, "YOU.ADDED.PLAYER", new List<object>() { friendName } );                 
                        GetMsg(friend, "PLAYER.ADDED.YOU", new List<object>() { player.displayName });                                                                                                                          
                        
                        if (!PlayerTempData.ContainsKey(player.userID))
                            PlayerTempData.Add(player.userID, new PlayerInfo());
                        
                        PlayerTempData[player.userID].CountAddFriends++;
                        
                        if (!PlayerTempData.ContainsKey(pair.Key))
                            PlayerTempData.Add(pair.Key, new PlayerInfo());
                        
                        PlayerTempData[pair.Key].CountAddFriends++;
                        
                        /*GetMsg(player, "PLAYER.ADDED.YOU.LIMIT", new List<object>() { PlayerTempData[player.userID].CountAddFriends, configData.LimitToAddFrinds });
                        GetMsg(friend, "PLAYER.ADDED.YOU.LIMIT", new List<object>() { PlayerTempData[pair.Key].CountAddFriends, configData.LimitToAddFrinds });*/
                                
                        Interface.Oxide.CallHook("OnFriendAdded", player.userID.ToString(), pair.Key.ToString());
                        Interface.Oxide.CallHook("OnFriendAdded", pair.Key.ToString(), player.userID.ToString());                                       
                        
                        CallSomeOvhHooks(player.userID);
                        CallSomeOvhHooks(pair.Key);
                        
                        PlayerTempData[player.userID].TurrentAuthorization = true;
                        PlayerTempData[player.userID].CodeAuthorization = true;
                        PlayerTempData[player.userID].AttackFriend = true;
                        SaveTmpData();
                        return;
                    }
                    
                    GetMsg(player, "YOU.NO.REQUEST");
                    return;             
                }   
                
                if (args[0].ToLower() == "deny")
                {               
                    foreach(var pair in FriendsWaitAccept.Where(x=> x.Value.Contains(player.userID)).ToDictionary(x=> x.Key, x=> x.Value))
                    {                                                                               
                        (FriendsWaitAccept[pair.Key]).Remove(player.userID);    

                        var friend = BasePlayer.FindByID(pair.Key);                 
                        
                        GetMsg(player, "YOU.REFUSE.REQUEST");                   
                        GetMsg(friend, "PLAYER.REFUSE.REQUEST", new List<object>() { player.displayName }); 
                                                    
                        return;
                    }
                    
                    GetMsg(player, "YOU.NO.REQUEST");
                    return;             
                }   
                
                if (args[0].ToLower() == "remove")
                {
                    if (args.Length < 2)
                    {
                        GetMsg(player, "FRIEND.REMOVE.HELP");                   
                        return;
                    }                               
                                    
                    string nameOrId = "";
                    for(int ii=1;ii<args.Length;ii++)
                        nameOrId += args[ii] + " ";
                    nameOrId = nameOrId.Trim(' ');
                                    
                    var friendID = FindYourFriend(player, nameOrId);
                    if (friendID == 0) return;                                                              
                    
                    if (friendID == player.userID)
                    {
                        GetMsg(player, "CANT.SEND.REMOVE.YOURSELF");                    
                        return;
                    }
                    
                    if (!FriendsWipeData.ContainsKey(player.userID) || (FriendsWipeData.ContainsKey(player.userID) && !FriendsWipeData[player.userID].Contains(friendID)))
                    {
                        GetMsg(player, "PLAYER.NOTFOUND.FRIEND", new List<object>() { FindPlayerName(friendID) });
                        return;
                    } 
                    
                    if (FriendsWipeData.ContainsKey(player.userID))
                        FriendsWipeData[player.userID].Remove(friendID);
                    
                    if (FriendsWipeData.ContainsKey(friendID))
                        FriendsWipeData[friendID].Remove(player.userID);
                    else
                    {
                        LoadFriendList(friendID);
                        if (FriendsWipeData.ContainsKey(friendID))
                            FriendsWipeData[friendID].Remove(player.userID);
                    }
                    
                    var friend = BasePlayer.FindByID(friendID);
                    
                    GetMsg(player, "YOU.REMOVED.FRIEND", new List<object>() { FindPlayerName(friendID) } );             
                    GetMsg(friend, "FRIEND.REMOVED.YOU", new List<object>() { player.displayName });    
                    
                    if (!PlayerTempData.ContainsKey(player.userID))
                        PlayerTempData.Add(player.userID, new PlayerInfo());
                    
                    if (!PlayerTempData[player.userID].RemoveFriends.ContainsKey(friendID))
                        PlayerTempData[player.userID].RemoveFriends.Add(friendID, ToEpochTime(DateTime.Now));
                    else
                        PlayerTempData[player.userID].RemoveFriends[friendID] = ToEpochTime(DateTime.Now);
                    
                    PlayerTempData[player.userID].LastRemoveFriends = ToEpochTime(DateTime.Now);
                                    
                    if (!PlayerTempData.ContainsKey(friendID))
                        PlayerTempData.Add(friendID, new PlayerInfo());
                    
                    if (!PlayerTempData[friendID].RemoveFriends.ContainsKey(player.userID))
                        PlayerTempData[friendID].RemoveFriends.Add(player.userID, ToEpochTime(DateTime.Now));
                    else
                        PlayerTempData[friendID].RemoveFriends[player.userID] = ToEpochTime(DateTime.Now);
                    
                    PlayerTempData[friendID].LastRemoveFriends = ToEpochTime(DateTime.Now);
                    
                    Interface.Oxide.CallHook("OnFriendRemoved", player.userID.ToString(), friendID.ToString());
                    Interface.Oxide.CallHook("OnFriendRemoved", friendID.ToString(), player.userID.ToString());
                    
                    CallSomeOvhHooks(player.userID);
                    CallSomeOvhHooks(friendID);
                    
                    PlayerTempData[player.userID].TurrentAuthorization = false;
                    PlayerTempData[player.userID].CodeAuthorization = false;
                    PlayerTempData[player.userID].AttackFriend = false;
                    SaveTmpData();          
                    return;
                }   
                
                if (args[0].ToLower() == "removeall")
                {               
                    if (!FriendsWipeData.ContainsKey(player.userID) || FriendsWipeData[player.userID].Count == 0)
                    {
                        GetMsg(player, "YOU.NOT.FRIEND");
                        return;
                    } 
                    
                    if (!PlayerTempData.ContainsKey(player.userID))
                        PlayerTempData.Add(player.userID, new PlayerInfo());
                    
                    foreach(var friendID in FriendsWipeData[player.userID].ToList())
                    {                           
                        FriendsWipeData[player.userID].Remove(friendID);
                        
                        if (FriendsWipeData.ContainsKey(friendID))
                            FriendsWipeData[friendID].Remove(player.userID);
                        else
                        {
                            LoadFriendList(friendID);
                            if (FriendsWipeData.ContainsKey(friendID))
                                FriendsWipeData[friendID].Remove(player.userID);
                        }
                        
                        var friend = BasePlayer.FindByID(friendID);
                                            
                        GetMsg(friend, "FRIEND.REMOVED.YOU", new List<object>() { player.displayName });                                            
                        
                        if (!PlayerTempData[player.userID].RemoveFriends.ContainsKey(friendID))
                            PlayerTempData[player.userID].RemoveFriends.Add(friendID, ToEpochTime(DateTime.Now));
                        else
                            PlayerTempData[player.userID].RemoveFriends[friendID] = ToEpochTime(DateTime.Now);
                                                                                
                        if (!PlayerTempData.ContainsKey(friendID))
                            PlayerTempData.Add(friendID, new PlayerInfo());
                        
                        if (!PlayerTempData[friendID].RemoveFriends.ContainsKey(player.userID))
                            PlayerTempData[friendID].RemoveFriends.Add(player.userID, ToEpochTime(DateTime.Now));
                        else
                            PlayerTempData[friendID].RemoveFriends[player.userID] = ToEpochTime(DateTime.Now);                                      
                        
                        PlayerTempData[friendID].LastRemoveFriends = ToEpochTime(DateTime.Now);
                        
                        Interface.Oxide.CallHook("OnFriendRemoved", player.userID.ToString(), friendID.ToString());
                        Interface.Oxide.CallHook("OnFriendRemoved", friendID.ToString(), player.userID.ToString());
                        
                        CallSomeOvhHooks(player.userID);
                        CallSomeOvhHooks(friendID);
                    }
                    
                    PlayerTempData[player.userID].LastRemoveFriends = ToEpochTime(DateTime.Now);                
                    GetMsg(player, "YOU.REMOVED.FRIENDS");
                    
                    PlayerTempData[player.userID].TurrentAuthorization = false;
                    PlayerTempData[player.userID].CodeAuthorization = false;
                    PlayerTempData[player.userID].AttackFriend = false;
                    SaveTmpData();          
                    return;
                }
                
                GetMsg(player, "CMD.FRIEND.HELP");      
            }

            private void CallSomeOvhHooks(ulong userID)
            {
                var result = new List<BasePlayer>();
                            
                var players = BasePlayer.activePlayerList.ToList();
                            
                foreach(var friend2 in GetFriends(userID).Where(x=> players.Exists(y=> y != null && y.userID == x)))
                    result.Add(BasePlayer.FindByID(friend2));       
                        
                if (players.Exists(y=> y != null && y.userID == userID))                                                                                
                    Interface.Oxide.CallHook("OnActiveFriendsUpdate", BasePlayer.FindByID(userID), result);                     
                else                                                                                    
                    Interface.Oxide.CallHook("OnActiveFriendsUpdateUserId", userID, result);    
            }
            
            private void TryAddFrind(BasePlayer player, BasePlayer friend)
            {
                if (!FriendsWaitAccept.ContainsKey(player.userID)) FriendsWaitAccept.Add(player.userID, new List<ulong>());

                FriendsWaitAccept[player.userID].Add(friend.userID);
                GetMsg(player, "YOU.SEND.REQUEST", new List<object>() { friend.displayName } );
                GetMsg(friend, "PLAYER.SEND.REQUEST.YOU", new List<object>() { player.displayName } );
                
                var playerID = player.userID;
                var friendID = friend.userID;
                var friendName = friend.displayName;
                
                timer.Once(configData.TimeToAnswer, ()=>
                {
                    if (FriendsWaitAccept.ContainsKey(playerID) && FriendsWaitAccept[playerID].Contains(friendID))
                    {                               
                        (FriendsWaitAccept[playerID]).Remove(friendID);
                        GetMsg(player, "PLAYER.CANCELED.WAIT.REQUEST", new List<object>() { friendName } );
                        GetMsg(friend, "YOU.CANCELED.WAIT.REQUEST");
                    }   
                });                                 
            }
            
            private static void LoadFriendList(ulong userID)
            {
                if (!FriendsWipeData.ContainsKey(userID) && FriendsData.ContainsKey(userID))                        
                    FriendsWipeData.Add(userID, FriendsData[userID]);                                                           
            }

            private string GetPlayerName(ulong userID)
            {
                var data = permission.GetUserData(userID.ToString());                                                           
                return data.LastSeenNickname;
            }
            
            private string FindPlayerName(ulong userID, bool isFull = false)
            {            
                var player = BasePlayer.activePlayerList.FirstOrDefault(x=>x.userID == userID);
                
                if (player == null)
                {
                    player = BasePlayer.sleepingPlayerList.FirstOrDefault(x=>x.userID == userID);
                    if (player == null)
                    {
                        var name = GetPlayerName(userID);                   

                        if (name != "Unnamed")
                            return isFull ? (name + " (" + userID.ToString() + ")") : name;
                        else
                            return isFull ? ("Без имени" + " (" + userID.ToString() + ")") : "Без имени";
                    }   
                }                           

                return isFull ? (player.displayName + " (" + userID.ToString() + ")") : player.displayName;
            }       
            
            private BasePlayer FindOnlinePlayer(BasePlayer player, string nameOrID)
            {
                if (nameOrID.IsSteamId())
                {
                    var target = BasePlayer.FindByID((ulong)Convert.ToInt64(nameOrID));
                    if (target == null)
                    {                   
                        GetMsg(player, "PLAYER.NOTFOUND", new List<object>() { nameOrID });
                        return null;
                    }
                    
                    return target;
                }
                
                var targets = BasePlayer.activePlayerList.Where(x=> x.displayName == nameOrID).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return null;
                }
                
                targets = BasePlayer.activePlayerList.Where(x=> x.displayName.ToLower() == nameOrID.ToLower()).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return null;
                }   
                        
                targets = BasePlayer.activePlayerList.Where(x=> x.displayName.Contains(nameOrID)).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return null;
                }
                
                targets = BasePlayer.activePlayerList.Where(x=> x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return null;
                }
                
                GetMsg(player, "PLAYER.NOTFOUND", new List<object>() { nameOrID });
                return null;
            }
            
            private ulong FindYourFriend(BasePlayer player, string nameOrID)
            {
                if (!FriendsWipeData.ContainsKey(player.userID))
                {
                    GetMsg(player, "YOU.NOT.FRIEND");
                    return 0;
                }
                
                var friends = FriendsWipeData[player.userID];

                if (friends.Count() == 0)
                {
                    GetMsg(player, "YOU.NOT.FRIEND");
                    return 0;
                }
                
                if (nameOrID.IsSteamId())
                {
                    var targetID = (ulong)Convert.ToInt64(nameOrID);
                    if (!friends.Contains(targetID))
                    {                   
                        GetMsg(player, "PLAYER.NOTFOUND.FRIEND", new List<object>() { FindPlayerName(targetID) });
                        return 0;
                    }
                    
                    return targetID;
                }
                
                var targets = friends.Where(x=> FindPlayerName(x) == nameOrID).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return 0;
                }
                
                targets = friends.Where(x=> FindPlayerName(x).ToLower() == nameOrID.ToLower()).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return 0;
                }
                
                targets = friends.Where(x=> FindPlayerName(x).Contains(nameOrID)).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return 0;
                }
                
                targets = friends.Where(x=> FindPlayerName(x).ToLower().Contains(nameOrID.ToLower())).ToList();
                
                if (targets.Count() == 1)
                    return targets[0];
                
                if (targets.Count() > 1)
                {
                    GetMsg(player, "PLAYER.MULTIPLE.FOUND", new List<object>() { targets.Count() });
                    return 0;
                }
                
                GetMsg(player, "PLAYER.NOTFOUND.FRIEND.LIST", new List<object>() { nameOrID });
                return 0;
            }                               
            
            private static string GetTime(long time)
            {            
                TimeSpan elapsedTime = TimeSpan.FromSeconds(time);
                int hours = elapsedTime.Hours;
                int minutes = elapsedTime.Minutes;
                int seconds = elapsedTime.Seconds;
                int days = Mathf.FloorToInt((float)elapsedTime.TotalDays);
                string s = "";
                int cnt = 0;
                
                if (days > 0) { s += $"{GetStringCount(days, new List<string>() {"день","дня","дней"})} "; cnt+=1; }
                if (hours > 0) { s += (cnt == 1 ? "и " : "") + $@"{GetStringCount(hours, new List<string>() {"час","часа","часов"})} "; cnt+=2; }
                if (cnt == 1 || cnt == 3) return s.TrimEnd(' ');
                
                if (minutes > 0) { s += (cnt == 2 ? "и " : "") + $"{GetStringCount(minutes, new List<string>() {"минута","минуты","минут"})} "; cnt+=4; }           
                if (cnt == 2 || cnt == 6) return s.TrimEnd(' ');
                
                if (seconds > 0) s += (cnt == 4 ? "и " : "") + $"{GetStringCount(seconds, new List<string>() {"секунда","секунды","секунд"})} ";                                    
                if (string.IsNullOrEmpty(s)) return "несколько секунд";
                
                return s.TrimEnd(' ');
            }               
            
            private static string GetStringCount(long count, List<string> words)
            {   
                switch(count)
                {
                    case 11: 
                    case 12: 
                    case 13: 
                    case 14: return $"{count} {words[2]}";
                }
                
                var countString = count.ToString();         
                switch(countString[countString.Length-1])
                {
                    case '1': return $"{count} {words[0]}";
                    case '2': 
                    case '3': 
                    case '4': return $"{count} {words[1]}";             
                }
                
                return $"{count} {words[2]}";
            }               
            
            private long ToEpochTime(DateTime dateTime)
            {
                var date = dateTime.ToLocalTime();
                var ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0).Ticks;
                var ts = ticks / TimeSpan.TicksPerSecond;
                return ts;
            }
            
            private void GetMsg(BasePlayer player, string key, List<object> params_ = null)
            {
                var message = GetLangMessage(key, player.IPlayer.Id);
                if (params_ != null) for(int ii=0;ii<params_.Count;ii++) message = message.Replace("{"+ii+"}", Convert.ToString(params_[ii]));
                if (player != null) SendReply(player, message);                     
            }
            
            private void LoadDefaultMessages()
            {
                lang.RegisterMessages(new Dictionary<string, string>
                {                
                    { "CMD.FRIEND.HELP", "<size=14>Команды для <color=#EA2E5D>управления</color> друзьями:</size>\n<size=12>/friend add <color=#EA2E5D>[Имя игрока]</color> - Добавить в список друзей\n/friend remove <color=#EA2E5D>[Имя игрока]</color> - Удалить из списка друзей\n/friend removeall - Очистить список друзей\n/friend list - Показать список друзей</size>"},
                    { "FRIEND.REMOVE.HELP", "Используете /friend remove <color=#EA2E5D>[Имя игрока]</color> чтобы удалить игрока из списка друзей."},
                    { "FRIEND.ADD.HELP", "Используйте /friend add <color=#EA2E5D>[Имя игрока]</color> чтобы добавить игрока в список друзей"},
                    { "YOUR.ACTIVE.REQUEST", "Вы уже отправили игроку <color=#EA2E5D>{0}</color> предложение дружбы, дождитесь ответа."},
                    { "PLAYER.ACTIVE.REQUEST", "Игрок <color=#EA2E5D>{0}</color> имеет активное предложение дружбы от <color=#EA2E5D>{1}</color>, попробуйте позже."},
                    { "PLAYER.SEND.YOU.REQUEST", "<size=14>Игрок <color=#EA2E5D>{0}</color> уже отправил вам предложение дружбы.</size>\n<size=12>\nИспользуйте <color=#EA2E5D>/friend accept</color> чтобы принять предложение или <color=#EA2E5D>/friend deny</color> чтобы отменить предложение.</size>"},
                    { "PLAYER.ALREADY.FRIENDS", "Игрок <color=#EA2E5D>{0}</color> уже есть в списке друзей."},
                    { "CANT.SEND.REQUEST.YOURSELF", "Вы не можете отправить предложение дружбы самому себе."},
                    { "CANT.SEND.REMOVE.YOURSELF", "Вы не можете удалять самого себя."},
                    { "CANT.SEND.REQUEST.YOU.COOLDOWN", "Вы не можете отправлять предложения дружбы, вы недавно удалили из списка одного из друзей, подождите <color=#EA2E5D>{0}</color>."},
                    { "CANT.SEND.REQUEST.TARGET.COOLDOWN", "Вы не можете отправить предложение дружбы игроку <color=#EA2E5D>{0}</color>, так как он недавно удалил из списка одного из друзей, подождите <color=#EA2E5D>{1}</color>."},
                    { "CANT.SEND.REQUEST.YOU.COOLDOWN.OLD.FRIENDS", "Вы не можете отправить предложение дружбы игроку <color=#EA2E5D>{0}</color>, так как вы недавно удалили его из списка друзей, подождите <color=#EA2E5D>{1}</color>."},
                    { "CANT.SEND.REQUEST.TARGET.COOLDOWN.OLD.FRIENDS", "Вы не можете отправить предложение дружбы игроку <color=#EA2E5D>{0}</color>, так как он недавно удалил вас из списка друзей, подождите <color=#EA2E5D>{1}</color>."},           
                    { "CANT.SEND.REQUEST.YOU.LIMIT.ADD.FRIENDS", "Вы не можете отправлять предложения дружбы, вы исчерпали лимит на количество добавлений в друзья."},
                    { "CANT.SEND.REQUEST.TARGET.LIMIT.ADD.FRIENDS", "Вы не можете отправить предложение дружбы игроку <color=#EA2E5D>{0}</color>, он исчерпал лимит на количество добавлений в друзья."},
                    { "YOUR.FRIENDS.LIMIT", "Вы имеете максимальное количество друзей <color=#EA2E5D>{0}</color>."},
                    { "PLAYER.FRIENDS.LIMIT", "Игрок <color=#EA2E5D>{0}</color> имеет максимальное количество друзей <color=#EA2E5D>{1}</color>."},
                    { "YOU.NO.REQUEST", "У вас нет предложений дружбы."},
                    { "YOU.REFUSE.REQUEST", "Вы отказались от предложения дружбы."},
                    { "PLAYER.REFUSE.REQUEST", "Игрок <color=#EA2E5D>{0}</color> отказался от предложения дружбы."},
                    { "PLAYER.CANCELED.WAIT.REQUEST", "Игрок <color=#EA2E5D>{0}</color> не ответил на предложение дружбы."},
                    { "YOU.CANCELED.WAIT.REQUEST", "Вы не ответили на предложение дружбы."},
                    { "YOU.SEND.REQUEST", "Предложение дружбы для <color=#EA2E5D>{0}</color> успешно отправлено."},
                    { "PLAYER.SEND.REQUEST.YOU", "<size=14>Игрок <color=#EA2E5D>{0}</color> отправил вам предложение дружбы.</size>\n<size=12>Используйте <color=#EA2E5D>/friend accept</color> чтобы принять предложение или <color=#EA2E5D>/friend deny</color> чтобы отменить предложение.</size>"},
                    { "YOU.ADDED.PLAYER", "Игрок <color=#EA2E5D>{0}</color> добавлен в список друзей."},
                    { "PLAYER.ADDED.YOU", "Игрок <color=#EA2E5D>{0}</color> добавил вас в список друзей."},
                    { "PLAYER.NOTFOUND.FRIEND", "Игрок <color=#EA2E5D>{0}</color> не является вашим другом."},
                    { "YOU.REMOVED.FRIEND", "Игрок <color=#EA2E5D>{0}</color> удален из списка друзей."},
                    { "YOU.REMOVED.FRIENDS", "Вы очистили свой список друзей."},
                    { "FRIEND.REMOVED.YOU", "Игрок <color=#EA2E5D>{0}</color> удалил вас из списка друзей."},
                    { "FRIEND.LIST", "<size=14>Список друзей:</size>\n<size=12>{0}</size>"},
                    { "YOU.NOT.FRIEND", "У вас нет друзей."},
                    { "PLAYER.NOTFOUND", "Игрок <color=#EA2E5D>{0}</color> не найден, возможно он отключён."},
                    { "PLAYER.NOTFOUND.FRIEND.LIST", "Игрок <color=#EA2E5D>{0}</color> не найден в списке ваших друзей."},
                    { "PLAYER.MULTIPLE.FOUND", "Найдено <color=#EA2E5D>{0}</color> похожих игроков, уточните запрос или используйте steam id игрока."}  
                }, this, "ru");

                lang.RegisterMessages(new Dictionary<string, string>
                {                
                    { "CMD.FRIEND.HELP", "<size=14>Commands to <color=#EA2E5D>manage</color> friends:</size>\n<size=12>/friend add <color=#EA2E5D>[Player name]</color> - Add to friends list\n/friend remove <color=#EA2E5D>[Player name]</color> - Remove from friends list\n/friend removeall - Clear friends list\n/friend list - Show friends list</size>."},
                    { "FRIEND.REMOVE.HELP", "Use /friend remove <color=#EA2E5D>[Player Name]</color> to remove a player from your friends list."},
                    { "FRIEND.ADD.HELP", "Use /friend add <color=#EA2E5D>[Player Name]</color> to add a player to your friends list"},
                    { "YOUR.ACTIVE.REQUEST", "You have already sent player <color=#EA2E5D>{0}</color> a friendship offer, wait for a reply."},
                    { "PLAYER.ACTIVE.REQUEST", "Player <color=#EA2E5D>{0}</color> has an active friendship offer from <color=#EA2E5D>{1}</color>, try again later."},
                    { "PLAYER.SEND.YOU.REQUEST", "<size=14>A player <color=#EA2E5D>{0}</color> has already sent you a friendship offer.</size>\n<size=12>nUse <color=#EA2E5D>/friend accept</color> to accept the offer or <color=#EA2E5D>/friend deny</color> to decline the offer.</size>"},
                    { "PLAYER.ALREADY.FRIENDS", "Player <color=#EA2E5D>{0}</color> is already in your friends list."},
                    { "CANT.SEND.REQUEST.YOURSELF", "You can't send a friendship offer to yourself."},
                    { "CANT.SEND.REMOVE.YOURSELF", "You can't delete yourself."},
                    { "CANT.SEND.REQUEST.YOU.COOLDOWN", "You can't send friendship offers, you recently removed one of your friends from your list, wait <color=#EA2E5D>{0}</color>."},
                    { "CANT.SEND.REQUEST.TARGET.COOLDOWN", "You can't send a friendship offer to player <color=#EA2E5D>{0}</color> because he recently removed one of his friends from the list, wait <color=#EA2E5D>{1}</color>."},
                    { "CANT.SEND.REQUEST.YOU.COOLDOWN.OLD.FRIENDS", "You cannot send a friendship offer to player <color=#EA2E5D>{0}</color> because you recently removed him from your friends list, wait <color=#EA2E5D>{1}</color>."},
                    { "CANT.SEND.REQUEST.TARGET.COOLDOWN.OLD.FRIENDS", "You can't send a friendship offer to player <color=#EA2E5D>{0}</color> because he recently removed you from his friends list, wait <color=#EA2E5D>{1}</color>."},           
                    { "CANT.SEND.REQUEST.YOU.LIMIT.ADD.FRIENDS", "You can't send friendship offers, you've exhausted your friend add limit."},
                    { "CANT.SEND.REQUEST.TARGET.LIMIT.ADD.FRIENDS", "You cannot send a friendship offer to player <color=#EA2E5D>{0}</color>, he has reached the limit on the number of friendships he can add."},
                    { "YOUR.FRIENDS.LIMIT", "You have a maximum number of friends <color=#EA2E5D>{0}</color>."},
                    { "PLAYER.FRIENDS.LIMIT", "Player <color=#EA2E5D>{0}</color> has a maximum of <color=#EA2E5D>{1}</color> friends."},
                    { "YOU.NO.REQUEST", "You have no offers of friendship."},
                    { "YOU.REFUSE.REQUEST", "You turned down an offer of friendship."},
                    { "PLAYER.REFUSE.REQUEST", "Player <color=#EA2E5D>{0}</color> declined the offer of friendship."},
                    { "PLAYER.CANCELED.WAIT.REQUEST", "Player <color=#EA2E5D>{0}</color> has not responded to the offer of friendship."},
                    { "YOU.CANCELED.WAIT.REQUEST", "You haven't responded to the offer of friendship."},
                    { "YOU.SEND.REQUEST", "The friendship offer for <color=#EA2E5D>{0}</color> has been successfully sent."},
                    { "PLAYER.SEND.REQUEST.YOU", "<size=14>A player <color=#EA2E5D>{0}</color> has sent you a friendship offer.</size>\n<size=12>Use <color=#EA2E5D>/friend accept</color> to accept the offer or <color=#EA2E5D>/friend deny</color> to cancel the offer.</size>>"},
                    { "YOU.ADDED.PLAYER", "Player <color=#EA2E5D>{0}</color> has been added to your friends list."},
                    { "PLAYER.ADDED.YOU", "Player <color=#EA2E5D>{0}</color> has added you to his friends list."},
                    { "PLAYER.NOTFOUND.FRIEND", "Player <color=#EA2E5D>{0}</color> is not your friend."},
                    { "YOU.REMOVED.FRIEND", "Player <color=#EA2E5D>{0}</color> has been removed from his friends list."},
                    { "YOU.REMOVED.FRIENDS", "You've cleared your friends list."},
                    { "FRIEND.REMOVED.YOU", "Player <color=#EA2E5D>{0}</color> has removed you from his friends list."},
                    { "FRIEND.LIST", "<size=14>Friends list:</size>\n<size=12>{0}</size>"},
                    { "YOU.NOT.FRIEND", "You have no friends."},
                    { "PLAYER.NOTFOUND", "Player <color=#EA2E5D>{0}</color> is not found, he may be disabled."},
                    { "PLAYER.NOTFOUND.FRIEND.LIST", "Player <color=#EA2E5D>{0}</color> is not found in your friends list."},
                    { "PLAYER.MULTIPLE.FOUND", "Found <color=#EA2E5D>{0}</color> similar players, refine your query or use the player's SteamID64."}  
                }, this, "en");
            }

            private string GetLangMessage(string key, string steamID = null) => lang.GetMessage(key, this, steamID);

            private static ConfigData configData;
            
            private class ConfigData
            {
                [JsonProperty(PropertyName = "Максимальное количество друзей")]
                public int MaxFriends;
                [JsonProperty(PropertyName = "Время для ответа на предложение дружбы (в секундах)")]
                public int TimeToAnswer;
                [JsonProperty(PropertyName = "Блокировка добавления игроков в друзья после удаления (в минутах)")]
                public int BlockTimeToAddAgain;         
                [JsonProperty(PropertyName = "Блокировка добавления удаленного игрока в друзья после удаления (в минутах)")]
                public int BlockTimeToAddFriendAgain;           
                [JsonProperty(PropertyName = "Лимит на количество добавлений в друзья")]
                public int LimitToAddFrinds;                        
            }
            
            private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
            
            protected override void LoadDefaultConfig()
            {
                var config = new ConfigData
                {
                    MaxFriends = 15,
                    TimeToAnswer = 20,
                    BlockTimeToAddAgain = 5,
                    BlockTimeToAddFriendAgain = 10,
                    LimitToAddFrinds = 20
                };
                SaveConfig(config);
                timer.Once(0.3f, ()=>SaveConfig(config));
            }        
            
            private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);             
            
            
            private void LoadData() => FriendsData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, List<ulong>>>("Friends/FriendsMainData");
            private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("Friends/FriendsMainData", FriendsData);
            private void LoadTmpData() => PlayerTempData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("Friends/FriendsTempData");
            private void SaveTmpData() => Interface.GetMod().DataFileSystem.WriteObject("Friends/FriendsTempData", PlayerTempData);
            private void LoadWipeData() => FriendsWipeData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, List<ulong>>>("Friends/FriendsWipeData");
            private void SaveWipeData() => Interface.GetMod().DataFileSystem.WriteObject("Friends/FriendsWipeData", FriendsWipeData);

            private bool HasFriend(ulong playerId, ulong friendId) 
            {
                if (!FriendsWipeData.ContainsKey(playerId)) return false;
                return FriendsWipeData[playerId].Contains(friendId);
            }

            private bool HasFriendS(string playerS, string friendS)
            {
                if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;           
                if (!playerS.IsSteamId() || !friendS.IsSteamId()) return false;
                var playerId = Convert.ToUInt64(playerS);
                var friendId = Convert.ToUInt64(friendS);
                return HasFriend(playerId, friendId);           
            }                       

            private bool AreFriends(ulong playerId, ulong friendId)
            {
                if (!FriendsWipeData.ContainsKey(playerId)) return false;
                if (!FriendsWipeData.ContainsKey(friendId)) return false;
                return FriendsWipeData[playerId].Contains(friendId) && FriendsWipeData[friendId].Contains(playerId);
            }

            private bool AreFriendsS(string playerS, string friendS)
            {
                if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;           
                if (!playerS.IsSteamId() || !friendS.IsSteamId()) return false;
                var playerId = Convert.ToUInt64(playerS);
                var friendId = Convert.ToUInt64(friendS);
                return AreFriends(playerId, friendId);
            }

            private bool HadFriend(ulong playerId, ulong friendId) => HasFriend(playerId, friendId);
            private bool HadFriendS(string playerS, string friendS) => HasFriendS(playerS, friendS);
            private bool WereFriends(ulong playerId, ulong friendId) => AreFriends(playerId, friendId);
            private bool WereFriendsS(string playerS, string friendS) => AreFriendsS(playerS, friendS);
            private bool IsFriend(ulong playerId, ulong friendId) => HasFriend(playerId, friendId);
            private bool IsFriendS(string playerS, string friendS) => HasFriendS(playerS, friendS);
            private bool WasFriend(ulong playerId, ulong friendId) => AreFriends(playerId, friendId);
            private bool WasFriendS(string playerS, string friendS) => AreFriendsS(playerS, friendS);          
            
            private ulong[] GetFriends(ulong playerId) 
            {
                if (!FriendsWipeData.ContainsKey(playerId)) return null;
                return FriendsWipeData[playerId].ToArray();
            }               

            private string[] GetFriendsS(string playerS)
            {
                if (string.IsNullOrEmpty(playerS)) return null;
                if (!playerS.IsSteamId()) return null;
                
                var playerId = Convert.ToUInt64(playerS);
                if (!FriendsWipeData.ContainsKey(playerId)) return null;
                return FriendsWipeData[playerId].ToList().ConvertAll(f => f.ToString()).ToArray();
            }

            private string[] GetFriendList(ulong playerId)
            {
                if (!FriendsWipeData.ContainsKey(playerId)) return null;
                            
                var players = new List<string>();
                foreach (var friendID in FriendsWipeData[playerId]) players.Add(FindPlayerName(friendID));
                return players.ToArray();
            }

            private string[] GetFriendListS(string playerS)
            {
                if (string.IsNullOrEmpty(playerS)) return null;
                if (!playerS.IsSteamId()) return null;
                return GetFriendList(Convert.ToUInt64(playerS));
            }

            private ulong[] IsFriendOf(ulong playerId)
            {
                return FriendsWipeData.Where(x=> x.Value.Contains(playerId)).Select(x=> x.Key).ToArray();
            }

            private string[] IsFriendOfS(string playerS)
            {
                if (string.IsNullOrEmpty(playerS)) return null;
                if (!playerS.IsSteamId()) return null;
                
                var playerId = Convert.ToUInt64(playerS);
                var friends = IsFriendOf(playerId);
                return friends.ToList().ConvertAll(f => f.ToString()).ToArray();
            }

            // ApiIsFriend(ulong playerId, ulong targetId) return true / null - являются ли игроки друзьями
            private bool ApiIsFriend(ulong playerId, ulong targetId) => AreFriends(playerId, targetId);
            
            // ApiGetFriends(ulong playerId) return List<ulong> / null - получить список друзей     
            private List<ulong> ApiGetFriends(ulong playerId) 
            {
                var friends = GetFriends(playerId);
                if (friends != null) return friends.ToList();
                return null;
            }
            
            // ApiGetActiveFriends(BasePlayer player) return List<ulong> / null - список друзей онлайн
            private List<ulong> ApiGetActiveFriends(BasePlayer player)
            {
                var result = new List<ulong>();
                if (player != null)
                {
                    var players = BasePlayer.activePlayerList.ToList();
                    var friends = GetFriends(player.userID);                
                    if (friends != null) foreach(var friend in friends.Where(x=> players.Exists(y=> y != null && y.userID == x))) result.Add(friend);
                    else return null;
                }
                
                return result;
            }
            
            // ApiGetActiveFriendsUserId(ulong userId) return List<ulong> / null - список друзей онлайн
            private List<ulong> ApiGetActiveFriendsUserId(ulong userId)
            {
                var result = new List<ulong>();                                 
                var friends = GetFriends(userId);
                var players = BasePlayer.activePlayerList.ToList();
                
                if (friends != null) foreach(var friend in friends.Where(x=> players.Exists(y=> y != null && y.userID == x))) result.Add(friend);                   
                else return null;
                
                return result;
            }

            private List<PlayerEntry> GetFriends(BasePlayer player)
            {
                var playerID = player.userID;
                List<PlayerEntry> list = new List<PlayerEntry>();

                if (5 > 0) if (friends.TryGetValue(playerID, out list)) return list;

                list = new List<PlayerEntry>();
                if (GetFriends(playerID) != null)
                {
                    foreach (var value in GetFriends(playerID))
                    {
                        var data = permission.GetUserData(value.ToString());
                        var displayName = data.LastSeenNickname;
                        if (displayName == "Unnamed")
                        {
                            var target = BasePlayer.FindByID(value) ?? BasePlayer.FindSleeping(value);
                            if (target != null)
                            {
                                displayName = target.displayName;
                            }
                        }
                    
                        list.Add(new PlayerEntry
                        {
                            name = displayName,
                            id = value,
                        });
                    }
                }

                if (5 > 0) friends.Add(playerID, list);

                return list;
            }


            private static string HexToCuiColor(string hex)
            {
                if (string.IsNullOrEmpty(hex))
                {
                    hex = "#FFFFFFFF";
                }

                var str = hex.Trim('#');

                if (str.Length == 6)
                    str += "FF";

                if (str.Length != 8)
                {
                    throw new Exception(hex);
                    throw new InvalidOperationException("Cannot convert a wrong format.");
                }

                var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
                var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

                Color color = new Color32(r, g, b, a);

                return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
            }
            
            private float[] SquarePos(int number, double count, float AmountUIonLine, float ToRaiseORLower, float StretchSide, float StretchUP, float DistanceBetweenA, float DistanceBetweenB, float MoveUIinLeft)
            {
                float offsetY = 0; float offsetX = 0;
                
                Vector2 position = new Vector2(AmountUIonLine, ToRaiseORLower); 
                Vector2 dimensions = new Vector2(StretchSide, StretchUP);
                
                int colum = (int)Math.Floor((decimal)((1 - position.x * 2) / dimensions.x));
                int row = (int)Math.Floor((decimal)(number / colum));
                
                if(colum*(row + 1) > count) count = count - colum * row;
                if(count > colum) count = colum;
                
                position.x = (float)(1 - ((MoveUIinLeft + dimensions.x) * count)) / 2;
                
                offsetY = (DistanceBetweenB - dimensions.y) * row;
                offsetX = (DistanceBetweenA + dimensions.x) * (number - (row* colum));
                
                Vector2 offset = new Vector2(offsetX, offsetY);
                Vector2 posMin = position + offset;
                Vector2 posMax = posMin + dimensions;
                
                return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
            }
        }
    }
