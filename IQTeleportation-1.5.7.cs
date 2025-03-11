using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Physics = UnityEngine.Physics;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using Pool = Facepunch.Pool;
using ConVar;
using System;
using ProtoBuf;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Text;
using Newtonsoft.Json;
using Object = System.Object;
using Vector3 = UnityEngine.Vector3;

namespace Oxide.Plugins
{
    [Info("IQTeleportation", "Mercury", "1.5.7")]
    [Description("IQTeleportation")]
    class IQTeleportation : RustPlugin
    {
        
        private readonly List<String> exceptionsBuildingBlockEntities = new()
        {
            "rug.deployed",
            "rug.bear.deployed",
            "sleepingbag_leather_deployed",
            "bed_deployed",
            "box.wooden.large",
            "woodbox",
        };

        private IEnumerator ProcessTWarp(BasePlayer player, Vector3 position, Int32 timeTeleportation)
        {
            return ProcessTeleportation(
                player,
                () => false,
                () => TeleportationWarp(player, position),
                timeTeleportation
            );
        }
        
        private readonly List<UInt64> ItemIdCorrecteds = new List<UInt64> { 1074866732, 2004072627, 2017601552, 2006957888, 930560607, 1123047824, 1130765085, 442289265, 090353317, 15376018, 118372687 }; 
        
        private void ParseMonuments()
        {
            localAllMonuments.Clear();
            
            Int32 countAddedMonuments = 0;
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                if (monument.Bounds.size == Vector3.zero)
                    continue;
                
                localAllMonuments.Add(monument);
                countAddedMonuments++;
            }
            
            Puts(LanguageEn ? $"Information received about {countAddedMonuments} monuments on the map" : $"Получена информация о {countAddedMonuments} монументах на карте");
        }

        private String CanSetHome(BasePlayer player, BuildingBlock buildingBlock, String homeName, in Vector3 positionHome, Tugboat tugboatEntity = null)
        {
            Configuration.HomeController.SetupHome homeController = config.homeController.setupHomeController;
            Object canSetHome = Interface.Call("OnHomeAdd", player, homeName, positionHome);

            if (canSetHome != null)
            {
                if (canSetHome is String errorMessage && !String.IsNullOrWhiteSpace(errorMessage))
                    return errorMessage;

                return GetLang("CAN_SETHOME_OTHER_MESSAGE", player.UserIDString);
            }

            String statusPlayer = CheckStatusPlayer(player);
            if (!String.IsNullOrWhiteSpace(statusPlayer))
                return statusPlayer;
            
            if(homeController.noSetupInRaidBlock && IsRaidBlocked(player))
                return GetLang("CAN_SETHOME_IS_RAIDBLOCK", player.UserIDString);
            
            Dictionary<String, PlayerHome> pData = playerHomes[player.userID];
		   		 		  						  	   		   		 		  			 		  	   		  				
            if (pData.Count >= config.homeController.homeCount.GetCount(player, true))
                return GetLang("CAN_SETHOME_MAXIMUM_HOMES", player.UserIDString);
            
            if (pData.ContainsKey(homeName))
                return GetLang("CAN_SETHOME_EXIST_HOMENAME", player.UserIDString, homeName);

            foreach (KeyValuePair<String, PlayerHome> homesPlayer in pData)
            {
                if (Vector3.Distance(homesPlayer.Value.positionHome, positionHome) < 2.8f) 
                    return GetLang("CAN_SETHOME_DISTANCE_EXIST_HOME", player.UserIDString, homesPlayer.Key);
            }
            
            if (tugboatEntity != null)
            {
                if (!config.homeController.setupHomeController.canSetupTugboatHome)
                    return GetLang("CAN_SETHOME_TUGBOAT_DISABLE", player.UserIDString);

                UInt64[] friendList = GetFriendList(player);
                if(friendList == null || friendList.Length == 0) return String.Empty; 
		   		 		  						  	   		   		 		  			 		  	   		  				
                Boolean isAuthedFriend = tugboatEntity.children
                    .OfType<VehiclePrivilege>()
                    .Any(vehiclePrivilege => friendList.Any(vehiclePrivilege.IsAuthed));

                return !isAuthedFriend && !tugboatEntity.IsAuthed(player) ? GetLang("CAN_SETHOME_BUILDING_PRIVILAGE_AUTH_TUGBOAT", player.UserIDString) : String.Empty; 
            }
            
            BuildingPrivlidge buildingPrivilage = player.GetBuildingPrivilege();
            
            if(homeController.onlyBuildingPrivilage && buildingPrivilage == null)
                return GetLang("CAN_SETHOME_BUILDING_PRIVILAGE", player.UserIDString);

            if (homeController.onlyBuildingAuth && buildingPrivilage != null && !player.IsBuildingAuthed())
                return GetLang("CAN_SETHOME_BUILDING_PRIVILAGE_AUTH", player.UserIDString);
            
            if (!buildingBlock)
            {
                buildingBlock = GetBuildingBlock(positionHome);
                if (!buildingBlock)
                    return GetLang("CAN_SETHOME_BUILDING", player.UserIDString);
            }
            
            return String.Empty;
        }
        
        [ConsoleCommand("sethome")]
        private void SetHomeConsoleCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (arg.Args.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_SETHOME", player.UserIDString), player, true);
                return;
            }
		   		 		  						  	   		   		 		  			 		  	   		  				
            String homeName = arg.Args[0];
            SetHome(player, homeName);
        }
        private class ServerWarp
        {
            public Vector3 position;
            public String monumentParent;
            public Boolean hideWarp;
            
            public Vector3 GetPositionWarp()
            {
                if (hideWarp) return default;
                MonumentInfo monument = _.GetMonumentInName(monumentParent);
                if (!monument) return position;
                    
                return monument.transform.TransformPoint(position);
            }

            public ServerWarp(MonumentInfo monument, Vector3 warpPosition)
            {
                if (monument)
                {
                    position = monument.transform.InverseTransformPoint(warpPosition);
                    monumentParent = monument.name;
                }
                else
                {
                    position = warpPosition;
                    monumentParent = String.Empty;
                }
                
                hideWarp = false;
            }
        }
        private Dictionary<UInt64, Dictionary<String, PlayerHome>> playerHomes = new();
        
                
        
        
        [ConsoleCommand("ui_teleportation_command")] 
        private void UICommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            if (arg.Args.Length == 0) return;
            String action = arg.Args[0];

            switch (action)
            {
                case "sethome":
                {
                    if (arg.Args.Length < 1) return;
                    
                    Dictionary<String, PlayerHome> pData = playerHomes[player.userID];
                    if (pData.Count >= config.homeController.homeCount.GetCount(player, true)) return;
                    String mbName = GetNextHomeName(pData);

                    if (bagInstalled.TryGetValue(player, out SleepingBag bag))
                    {
                        SetHome(player, mbName, bag);
                        bagInstalled.Remove(player);
                    }
                    
                    break;
                }
		   		 		  						  	   		   		 		  			 		  	   		  				
                case "cancell.sethome":
                {
                    if (bagInstalled.ContainsKey(player))
                        bagInstalled.Remove(player);
                    
                    break;
                }

                case "tpa":
                {
                    AcceptRequestTeleportation(player);
                    break;
                }

                case "tpc":
                {
                    CancellTeleportation(player);
                    break;
                }
            }
        }
        private Dictionary<BasePlayer, BasePlayer> casheTeleportationLast = new ();

        private class TeleportationQueueInfo
        {
            public Boolean isAccepted;
            public Boolean isActive;
            public BasePlayer player;
        }
        
        [ChatCommand("sethome")]
        private void SetHomeChatCMD(BasePlayer player, String cmd, String[] arg)
        {
            if (arg.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_SETHOME", player.UserIDString), player, true);
                return;
            }

            String homeName = arg[0];
            SetHome(player, homeName);
        }
        
        private class UserRepository
        {
            public Coroutine activeTeleportation;
            public Double cooldownWarp;
            public Double cooldownHome;
            public Double cooldownTeleportation;
            
            public enum TeleportType
            {
                Warp,
                Home,
                Teleportation
            }
            
            public void SetupCooldown(BasePlayer player, TeleportType teleportType)
            {
                Int32 cooldown = teleportType switch
                {
                    TeleportType.Home => config.homeController.homeCooldown.GetCount(player),
                    TeleportType.Teleportation => config.teleportationController.teleportCooldown.GetCount(player),
                    TeleportType.Warp => config.warpsContoller.teleportWarpCooldown.GetCount(player),
                    _ => throw new ArgumentOutOfRangeException(nameof(teleportType), teleportType, null)
                };

                switch (teleportType)
                {
                    case TeleportType.Home:
                        cooldownHome = CurrentTime + cooldown;
                        break;
                    case TeleportType.Teleportation:
                        cooldownTeleportation = CurrentTime + cooldown;
                        break;
                    case TeleportType.Warp:
                        cooldownWarp = CurrentTime + cooldown;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(teleportType), teleportType, null);
                }

                DeActiveTeleportation();
            }
            
            public String GetCooldownTitle(BasePlayer player, TeleportType teleportType)
            {
                Double cooldownLeft = teleportType switch
                {
                    TeleportType.Warp => cooldownWarp - CurrentTime,
                    TeleportType.Home => cooldownHome - CurrentTime,
                    _ => cooldownTeleportation - CurrentTime
                };
                
                String cooldownLeftTitle = cooldownLeft <= 0 ? String.Empty : _.FormatTime(cooldownLeft, player.UserIDString);
                return cooldownLeftTitle;
            }

            public Boolean IsActiveTeleportation() => activeTeleportation != null;

            public void ActiveCoroutineController(Coroutine coroutine, BasePlayer target = null)
            {
                DeActiveTeleportation();
                activeTeleportation = coroutine;
            }

            public void DeActiveTeleportation()
            {
                if (!IsActiveTeleportation()) return;
                ServerMgr.Instance.StopCoroutine(activeTeleportation);
                activeTeleportation = null;
            }
        }

        [ConsoleCommand("rh")]
        private void RemoveHomeShortConsoleCMD(ConsoleSystem.Arg arg) => RemoveHomeConsoleCMD(arg);
        private Dictionary<BasePlayer, MapNote> playerPings = new();
		   		 		  						  	   		   		 		  			 		  	   		  				
                
        
        [ChatCommand("home")]
        private void HomeChatCMD(BasePlayer player, String cmd, String[] arg)
        {
            if (arg.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_HOME_TELEPORTATION", player.UserIDString), player, true);
                return;
            }
            
            RequestHome(player, arg);
        }

                
        
        
                
        private Boolean AcceptRequestTeleportation(BasePlayer player, Boolean isAutoAccept = false)
        {
            if (!playerTeleportationQueue.TryGetValue(player.userID, out TeleportationQueueInfo requester))
            {
                SendChat(GetLang("CAN_ACCEPT_TELEPORTATION_NOT_REQUEST", player.UserIDString), player, true);
                return false;
            }
            
            if (!requester.isActive)
            {
                SendChat(GetLang("CAN_ACCEPT_TELEPORTATION_NOT_REQUEST", player.UserIDString), player, true); 
                return false;
            }

            if (requester.isAccepted)
            {
                SendChat(GetLang("CAN_ACCEPT_TELEPORTATION_ACTIVE_ACCEPTED", player.UserIDString), player, true);
                return false;
            }
            
            String canTeleport = CanTeleportation(player, UserRepository.TeleportType.Teleportation, true);
            if (!String.IsNullOrWhiteSpace(canTeleport))
            {
                SendChat(canTeleport, player, true);
                localUsersRepository[requester.player].DeActiveTeleportation();
                return false;
            }
            
            Object OnTeleportRequested = Interface.Call("OnTeleportRequested", player, requester.player);
            if (OnTeleportRequested != null)
            {
                if (OnTeleportRequested is String hookResult)
                {
                    SendChat(hookResult, player, true);
                    localUsersRepository[requester.player].DeActiveTeleportation(); 
                    return false;
                }
                
                if (OnTeleportRequested is Boolean hookResultBoolean)
                    if (!hookResultBoolean)
                    {
                        SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", player.UserIDString), player, true);
                        localUsersRepository[requester.player].DeActiveTeleportation(); 
                        return false;
                    }
            }
		   		 		  						  	   		   		 		  			 		  	   		  				
            requester.isAccepted = true;
            
            if (config.teleportationController.teleportSetting.suggetionAcceptedUI)
                DestroyUI(player, InterfaceBuilder.IQ_TELEPORT_UI_TELEPORT);
            
            Int32 teleportationCountdown = config.teleportationController.teleportCountdown.GetCount(requester.player);
            
            Coroutine routineTeleportation = ServerMgr.Instance.StartCoroutine(ProcessTTarget(requester.player, player, teleportationCountdown));
            
            localUsersRepository[requester.player].ActiveCoroutineController(routineTeleportation);
            casheTeleportationLast[player] = requester.player;

            String autoRequestMessage = isAutoAccept ? "CAN_REQUEST_TELEPORTATION_ACCESS_AUTO_TELEPORT" : "CAN_REQUEST_TELEPORTATION_ACCESS";

            LogAction(player, TypeLog.AcceptTeleportation, requester.player, default, default);
            
            SendChat(GetLang(autoRequestMessage, player.UserIDString, requester.player.displayName), player);
            SendChat(GetLang("REQUEST_TELEPORTATION_ACCESS", requester.player.UserIDString, FormatTime(teleportationCountdown, requester.player.UserIDString)), requester.player);

            Interface.Call("OnTeleportAccepted", player, requester.player, teleportationCountdown);

            return true;
        }
        private const String permissionWarpAdmin = "iqteleportation.warpadmin";

        
        
        [ChatCommand("tp")]
        private void TpChatCMD(BasePlayer player, String cmd, String[] arg)
        {
            if (player == null) return;
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, permissionTP)) return;

            if (arg.Length == 0 && !casheTeleportationLast.ContainsKey(player))
            {
                SendChat(GetLang("SYNTAX_COMMAND_TELEPORT", player.UserIDString), player, true);
                return;
            }
            
            String nameOrIDTarget = arg[0];
            (BasePlayer, String) findPlayer = FindPlayerNameOrID(player, nameOrIDTarget);
            BasePlayer targetPlayer = null;
            if(!String.IsNullOrWhiteSpace(nameOrIDTarget))
                targetPlayer = findPlayer.Item1;
            
            String nameOrIDOnPlayer = String.Empty;
            BasePlayer onPlayer = null;

            if (arg.Length >= 2)
            {
                nameOrIDOnPlayer = arg[1];
                (BasePlayer, String) findOnPlayer = FindPlayerNameOrID(player, nameOrIDOnPlayer);

                if (!String.IsNullOrWhiteSpace(nameOrIDOnPlayer))
                {
                    onPlayer = targetPlayer;
                    targetPlayer = findOnPlayer.Item1;
                }

                if (onPlayer == null)
                {
                    SendChat(findOnPlayer.Item2 ?? GetLang("CAN_TELEPORT_PLAYER_NOT_FOUND_TP_ONE_PLAYER", player.UserIDString), player, true);
                    return;
                }

                if (targetPlayer == null)
                {
                    SendChat(findPlayer.Item2 ?? GetLang("CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER_ONE", player.UserIDString, nameOrIDTarget), player, true);
                    return;
                }
            }
            else onPlayer = player;
            
            if (targetPlayer == null)
            {
                SendChat(findPlayer.Item2 ?? GetLang("CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER", player.UserIDString), player, true);
                return;
            }
            
            MovePlayer(onPlayer, targetPlayer.transform.position);
            SendChat(GetLang("CAN_TELEPORT_PLAYER_ACCES_TP", onPlayer.UserIDString, targetPlayer.displayName), onPlayer);
        }
        
        private MonumentInfo GetMonumentWherePlayerStand(BasePlayer player)
        {
            MonumentInfo closestMonument = null;

            foreach (MonumentInfo monument in localAllMonuments)
            {
                Vector3 monumentCenter = monument.transform.TransformPoint(monument.Bounds.center);
                Vector3 monumentSize = monument.transform.TransformVector(monument.Bounds.size);
                Single monumentRadius = monumentSize.magnitude / 2f;
                Single distanceToMonument = Vector3.Distance(player.transform.position, monumentCenter);
                if (distanceToMonument <= monumentRadius)
                {
                    closestMonument = monument;
                    break;
                }
            }

            return closestMonument;
        }
        private List<MonumentInfo> localAllMonuments = new();
        
        private void ChatCommandWarp(BasePlayer player, String command, String[] args) 
        {
            if (!config.warpsContoller.useWarps) return;

            if (!player.IsAdmin)
                if (!permission.UserHasPermission(player.UserIDString, permissionWarpAdmin))
                    return;

            if (args.Length < 1)
            {
                SendChat(GetLang("SYNTAX_COMMAND_WARPS", player.UserIDString), player, true);
                return;
            }

            String action = args[0];
            String warpName = args.Length >= 2 ? args[1].ToLower() : null;
            
            MonumentInfo pMonument = GetMonumentWherePlayerStand(player);
            
            Vector3 pPosition = player.transform.position;
            
            switch (action)
            {
                case "points":
                {
                    if (string.IsNullOrWhiteSpace(warpName))
                    {
                        SendChat(GetLang("WARP_NOT_ARG_NAME", player.UserIDString), player, true); 
                        return;
                    }

                    if (!serverWarps.TryGetValue(warpName, out List<ServerWarp> warpPoints))
                    {
                        SendChat(GetLang("WARP_NOTHING_NAME", player.UserIDString), player, true);
                        return;
                    }

                    for (Int32 indexPoint = 0; indexPoint < warpPoints.Count; indexPoint++)
                    {
                        Vector3 warpPoint = warpPoints[indexPoint].GetPositionWarp();
                        if (warpPoint == default) return;
                        DrawSphereAndText(player, warpPoint, $"{indexPoint}");
                    }
                    
                    SendChat(GetLang("WARP_SHOW_POINTS", player.UserIDString, warpName), player, true);
                    break;
                }
                case "list":
                {
                    StringBuilder warpListBuilder = Pool.Get<StringBuilder>();

                    foreach (KeyValuePair<String, List<ServerWarp>> warps in serverWarps)
                    {
                        warpListBuilder.AppendLine($"{warps.Key} :");
                        
                        for (Int32 indexWarpPos = 0; indexWarpPos < warps.Value.Count; indexWarpPos++)
                        {
                            Vector3 warpPoint = warps.Value[indexWarpPos].GetPositionWarp();
                            warpListBuilder.AppendLine($"{indexWarpPos} : {(warpPoint == default ? "NONE MONUMENT" : warpPoint)}");
                        }

                        warpListBuilder.AppendLine(); 
                    }

                    String message = warpListBuilder.ToString();
                    if (String.IsNullOrWhiteSpace(message))
                        message = GetLang("WARP_NOTHING", player.UserIDString);
                    
                    SendChat(message, player);
                    Pool.FreeUnmanaged(ref warpListBuilder);
                    break;
                }
                case "create":
                case "add":
                case "set":
                { 
                    if (string.IsNullOrWhiteSpace(warpName))
                    {
                        SendChat(GetLang("WARP_NOT_ARG_NAME", player.UserIDString), player, true); 
                        return;
                    }
                    
                    if (serverWarps.ContainsKey(warpName))
                    {
                        for (Int32 indexPoint = 0; indexPoint < serverWarps[warpName].Count; indexPoint++)
                        {
                            Vector3 warpPoint = serverWarps[warpName][indexPoint].GetPositionWarp();
                            if (warpPoint == default) continue;
                            if (Vector3.Distance(warpPoint, player.transform.position) > 5f) continue;
                            
                            SendChat(GetLang("WARP_SETUP_POINTS_DISTANCE", player.UserIDString, indexPoint), player, true); 
                            return;
                        }

                        serverWarps[warpName].Add(new ServerWarp(pMonument, pPosition));
                        SendChat(GetLang("WARP_SETUP_POINT_ACCESS", player.UserIDString, warpName), player);
                        return;
                    }

                    serverWarps.Add(warpName, new List<ServerWarp>() { new(pMonument, pPosition) });
                    
                    cmd.AddChatCommand(warpName, this, nameof(ChatCommandTeleportationWarp));
                    cmd.AddConsoleCommand(warpName, this, nameof(ConsoleCommandTeleportationWarp));
                        
                    SendChat(GetLang("WARP_SETUP_NEW", player.UserIDString, warpName, warpName.ToLower()), player); 
                    break;
                }
                case "edit":
                case "update":
                {
                    if (string.IsNullOrWhiteSpace(warpName))
                    {
                        SendChat(GetLang("WARP_NOT_ARG_NAME", player.UserIDString), player, true); 
                        return;
                    }

                    if (!serverWarps.TryGetValue(warpName, out List<ServerWarp> warpPoints))
                    {
                        SendChat(GetLang("WARP_NOTHING_NAME", player.UserIDString), player, true);
                        return;
                    }
                    
                    if (args.Length < 3)
                    {
                        String pointsKeys = GetWarpPoints(warpName);
                        SendChat(GetLang("WARP_NOTHING_KEY_POINTS", player.UserIDString, pointsKeys), player, true);
                        return;
                    }
                    
                    String warpKeyString = args[2];

                    if (!Int32.TryParse(warpKeyString, out Int32 warpKey) || warpKey < 0)
                    {
                        SendChat(GetLang("WARP_NOTHING_KEY_IS_NUMBER", player.UserIDString), player, true);
                        return;
                    }

                    if (warpPoints.Count < warpKey)
                    {
                        String pointsKeys = GetWarpPoints(warpName);
                        SendChat(GetLang("WARP_REMOVED_POINT_NOTHING_KEYS", player.UserIDString, warpKey, warpName, pointsKeys), player, true); 
                        return;
                    }

                    for (Int32 indexPoint = 0; indexPoint < serverWarps[warpName].Count; indexPoint++)
                    {
                        Vector3 warpPoint = serverWarps[warpName][indexPoint].GetPositionWarp();
                        if(warpPoint == default) continue;
                        if (indexPoint == warpKey || Vector3.Distance(warpPoint, player.transform.position) > 5f) continue;
                        SendChat(GetLang("WARP_SETUP_POINTS_DISTANCE", player.UserIDString, indexPoint), player, true); 
                        return;
                    }

                    serverWarps[warpName][warpKey] = new ServerWarp(pMonument, pPosition);
                    SendChat(GetLang("WARP_EDIT_POINT_ACCESS", player.UserIDString, warpName), player); 
                    break;
                }
                case "remove":
                case "delete":
                {
                    //(IndexOutOfRangeException: Index was outside the bounds of the array.)
                    //warp remove jopa
                    if (string.IsNullOrWhiteSpace(warpName))
                    {
                        SendChat(GetLang("WARP_NOT_ARG_NAME", player.UserIDString), player, true); 
                        return;
                    }
                    
                    if (!serverWarps.TryGetValue(warpName, out List<ServerWarp> warpPoints))
                    {
                        SendChat(GetLang("WARP_NOTHING_NAME", player.UserIDString), player, true);
                        return;
                    }
                    
                    String warpKeyString = args.Length >= 3 ? args[2] : String.Empty;
                    
                    if (!String.IsNullOrWhiteSpace(warpKeyString) && warpPoints.Count > 1)
                    {
                        if (!Int32.TryParse(warpKeyString, out Int32 warpKey) || warpKey < 0)
                        {
                            SendChat(GetLang("WARP_NOTHING_KEY_IS_NUMBER", player.UserIDString), player, true);
                            return;
                        }
                        
                        if (warpPoints.Count < warpKey)
                        {
                            String pointsKeys = GetWarpPoints(warpName);
                            SendChat(GetLang("WARP_REMOVED_POINT_NOTHING_KEYS", player.UserIDString, warpKey, warpName, pointsKeys), player, true); 
                            return;
                        }
                        
                        serverWarps[warpName].RemoveAt(warpKey);
                        SendChat(GetLang("WARP_REMOVED_POINT", player.UserIDString, warpKey, warpName), player, true); 
                        return;
                    }
        
                    serverWarps.Remove(warpName);
                    cmd.RemoveChatCommand(warpName, this);
                    cmd.RemoveConsoleCommand(warpName, this);
                    
                    SendChat(GetLang("WARP_REMOVED", player.UserIDString, warpName), player); 
                    break;
                }
            }
        }

        
        
        [ChatCommand("gmap")]
        private void GMapTeleportChatCmd(BasePlayer player) => GMapTeleportTurn(player);
        private const String effectSoundTimer = "assets/prefabs/weapons/mp5/effects/fire_select.prefab";

        
        
        private void CheckAllHomes(BasePlayer player, String homeName = "")
        {
            if (!playerHomes.TryGetValue(player.userID, out Dictionary<String, PlayerHome> pHome)) return;

            if (!String.IsNullOrWhiteSpace(homeName))
            {
                if (!pHome.TryGetValue(homeName, out PlayerHome home)) return;
                
                if (!CheckHome(player, homeName, home))
                    pHome.Remove(homeName);
                return;
            }
            
            List<String> homesToRemove = Pool.Get<List<String>>();

            foreach (KeyValuePair<String, PlayerHome> pData in pHome)
            {
                if (!CheckHome(player, pData.Key, pHome[pData.Key]))
                    homesToRemove.Add(pData.Key);
            }

            foreach (String home in homesToRemove)
                pHome.Remove(home);
            
            Pool.FreeUnmanaged(ref homesToRemove);
        }
        private static ImageUI _imageUI;
        
        
        
        private Boolean IsPlayerWithinBlockedMonument(BasePlayer player)
        {
            if (config.generalController.monumentBlockedTeleportation.Count == 0) return false;
            MonumentInfo monument = GetMonumentWherePlayerStand(player);
            return monument && config.generalController.monumentBlockedTeleportation.Contains(monument.name);
        }
        
        private void TeleportationWarp(BasePlayer player, Vector3 position)
        {
            if (!player.IsValid())
                return;

            String canTeleport = CanTeleportation(player, UserRepository.TeleportType.Warp);
            if (!String.IsNullOrWhiteSpace(canTeleport))
            {
                SendChat(canTeleport, player, true);
                return;
            }

            localUsersRepository[player].SetupCooldown(player, UserRepository.TeleportType.Warp); 
            MovePlayer(player, position);
            SendChat(GetLang("WARP_TELEPORTATION_PLAYER", player.UserIDString), player);
        }
        private Dictionary<String, List<String>> cachedUI = new();

        private void AutoClearData(Boolean isNewSave = false)
        {
            Int32 settingClear = default;

                        
            List<UInt64> keysPlayersSetting = Pool.Get<List<UInt64>>();
            keysPlayersSetting.AddRange(playerSettings.Keys);
            DateTime currentDate = DateTime.Now;
            
            foreach (UInt64 keyPlayer in keysPlayersSetting)
            {
                SettingPlayer playerSetting = playerSettings[keyPlayer];
                Int32 leftDays = (currentDate - playerSetting.firstConnection).Days;
                if (leftDays < 7) continue;
                playerSettings.Remove(keyPlayer);
                settingClear++;
            }

            Pool.FreeUnmanaged(ref keysPlayersSetting);
            
                        
            if(settingClear != default)
                Puts(LanguageEn ? $"Automatic cleaning of user settings. {settingClear} users and their settings were deleted due to absence from the server for more than 7 days" : $"Автоматическая очистка настроек пользоваталей. Было удалено {settingClear} пользователей и их настройки из-за отсутствия более 7 дней на сервере");
            
            if (isNewSave)
            {
                playerHomes.Clear();

                List<String> keysToRemove = new List<String>();

                foreach (KeyValuePair<String, List<ServerWarp>> warps in serverWarps)
                {
                    List<ServerWarp> warpsToRemove = new List<ServerWarp>();
		   		 		  						  	   		   		 		  			 		  	   		  				
                    foreach (ServerWarp warp in warps.Value)
                    {
                        if (String.IsNullOrWhiteSpace(warp.monumentParent))
                            warpsToRemove.Add(warp);
                    }

                    foreach (ServerWarp warp in warpsToRemove)
                        warps.Value.Remove(warp);

                    if (warps.Value.Count == 0)
                        keysToRemove.Add(warps.Key);
                    
                    warpsToRemove.Clear();
                    warpsToRemove = null;
                }

                foreach (String key in keysToRemove)
                {
                    cmd.RemoveChatCommand(key, this);
                    cmd.RemoveConsoleCommand(key, this);
                    serverWarps.Remove(key);
                }

                keysToRemove.Clear();
                keysToRemove = null;
            }
            
            WriteData();
        }

        private Boolean CheckHome(BasePlayer player, String homeName, PlayerHome pHome)
        {
            if (pHome.netIdEntity != 0)
            {
                if (tugboatsServer.TryGetValue(pHome.netIdEntity, out Tugboat tugboat) && tugboat && !tugboat.IsDestroyed)
                    return true;
                
                SendChat(GetLang("CAN_TELEPORT_HOME_NOT_TUGBOAT", player.UserIDString, homeName), player);
                return false;
            }
    
            BuildingBlock buildingBlock = GetBuildingBlock(pHome.positionHome);
            if (!buildingBlock)
            {
                SendChat(GetLang("CAN_TELEPORT_HOME_NOT_BUILDING_BLOCK", player.UserIDString, homeName), player);
                return false;
            }

            if (!config.homeController.setupHomeController.onlyBuildingAuth) return true;
            BuildingPrivlidge privilege = buildingBlock.GetBuildingPrivilege();
            if (privilege == null || privilege.IsAuthed(player)) return true;
            SendChat(GetLang("CAN_TELEPORT_HOME_NOT_AUTH_BUILDING_PRIVILAGE", player.UserIDString, homeName), player);
            return false;
        }

        [ConsoleCommand("homelist")]
        private void ConsoleCommand_HomeList(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            Dictionary<String, Vector3> homeList = GetHomes(player.userID);
            if (homeList == null || homeList.Count == 0)
            {
                SendChat(GetLang("HOMELIST_COMMAND_EMPTY", player.UserIDString), player);
                return;
            }
            
            String homesPlayerOnCord = String.Empty;

            foreach (KeyValuePair<String, Vector3> homeInfo in homeList)
            {
                String cordMap = MapHelper.PositionToString(homeInfo.Value);
                homesPlayerOnCord += GetLang("HOMELIST_COMMAND_FORTMAT_HOME", player.UserIDString, homeInfo.Key, cordMap);
            }
            
            SendChat(GetLang("HOMELIST_COMMAND_ALL_LIST", player.UserIDString, homesPlayerOnCord), player);
        }

        private void OnServerInitialized()
        {
            RegisteredPermissions();
            
            if(config.teleportationController.teleportSetting.suggetionAcceptedUI || config.homeController.suggetionSetHomeAfterInstallBed)
            {
                _imageUI = new ImageUI();
                _imageUI.DownloadImage();
            }
            
            AutoClearData();
            
            if (config.generalController.monumentBlockedTeleportation.Count != 0 || config.warpsContoller.useWarps)
                ParseMonuments();

            if (config.homeController.setupHomeController.canSetupTugboatHome)
                ParseTugboats();
            
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            ValidateWarps();
            
            timerCheckHomes = timer.Every(300, () =>
            {
                List<BasePlayer> players = Pool.Get<List<BasePlayer>>();
                players.AddRange(BasePlayer.activePlayerList);
                
                for (Int32 i = 0; i < players.Count; i++)
                {
                    BasePlayer player = players[i];
                    if (player == null || !player.IsConnected) continue;
                    CheckAllHomes(player);
                }
                
                Pool.FreeUnmanaged(ref players);
            });
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        void OnPlayerDisconnected(BasePlayer player, String reason) => DestroyPing(player);
        private const String tpaLog = LanguageEn ? "" : "принял запрос на телепортацию от";

        
        private Single GetGroundPosition(in Vector3 pos)
        {
            Single y = TerrainMeta.HeightMap.GetHeight(pos);
            return Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out RaycastHit hitInfo, float.MaxValue, (Rust.Layers.Mask.Vehicle_Large | Rust.Layers.Solid | Rust.Layers.Mask.Water)) ? Mathf.Max(hitInfo.point.y, y) : y;
        }
		   		 		  						  	   		   		 		  			 		  	   		  				
        [ConsoleCommand("sh")]
        private void SetHomeShortConsoleCMD(ConsoleSystem.Arg arg) => SetHomeConsoleCMD(arg);

        private (BasePlayer, String) FindPlayerNameOrID(BasePlayer finder, String nameOrID, Boolean findSleeper = false)
        {
            List<BasePlayer> players = Pool.Get<List<BasePlayer>>();
            players.AddRange(findSleeper ? BasePlayer.allPlayerList : BasePlayer.activePlayerList);
            
            if (nameOrID.IsSteamId() && ulong.TryParse(nameOrID, out UInt64 userID))
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                Pool.FreeUnmanaged(ref players);
                return (player, null);
            }

            List<BasePlayer> matchingPlayers = Pool.Get<List<BasePlayer>>();

            try
            {
                matchingPlayers.AddRange(players.Where(player => player.displayName != null && player.displayName.IndexOf(nameOrID, StringComparison.OrdinalIgnoreCase) >= 0));
		   		 		  						  	   		   		 		  			 		  	   		  				
                switch (matchingPlayers.Count)
                {
                    case 1:
                    {
                        BasePlayer foundPlayer = matchingPlayers[0];
                        return (foundPlayer, null);
                    }
                    case > 1:
                    {
                        String nicknameList = String.Join(", ", matchingPlayers.Take(3).Select(p => p.displayName));
                        return (null, GetLang("FIND_PLAYER_INFO_MATCHES", finder.UserIDString, nameOrID, nicknameList));
                    }
                    default:
                        return (null, null);
                }
            }
            finally
            {
                Pool.FreeUnmanaged(ref players);
                Pool.FreeUnmanaged(ref matchingPlayers);
            }
        }
        
        private void RunEffect(BasePlayer player, String effectPath)
        {
            if (!config.generalController.useSoundEffects) return;
            Effect effect = new Effect(effectPath, player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);
        }
        private static Double CurrentTime => Facepunch.Math.Epoch.Current;
        
        private String CanRequestTeleportation(BasePlayer player, BasePlayer targetPlayer, String error = "")
        {
            if (!targetPlayer)
                return error ?? GetLang("CAN_REQUEST_TELEPORTATION_NOT_FOUND_PLAYER", player.UserIDString);
		   		 		  						  	   		   		 		  			 		  	   		  				
            // if (player.userID == targetPlayer.userID)
            //     return GetLang("CAN_REQUEST_TELEPORTATION_NOTHING_TELEPORT_ME", player.UserIDString);
            
            if (playerTeleportationQueue.ContainsKey(player.userID))
                return playerTeleportationQueue[player.userID].player 
                     ? GetLang("CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY_NAME", player.UserIDString, playerTeleportationQueue[player.userID].player.displayName) 
                     : GetLang("CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY", player.UserIDString);
            
            if (playerTeleportationQueue.ContainsKey(targetPlayer.userID))
                return GetLang("CAN_REQUEST_TELEPORTATION_ALREADY_REQUEST_TARGET", player.UserIDString);
            
            String canTeleport = CanTeleportation(player, UserRepository.TeleportType.Teleportation);
            if (!String.IsNullOrWhiteSpace(canTeleport))
                return canTeleport;
        
            if (config.teleportationController.teleportSetting.onlyTeleportationFriends && !IsFriends(player, targetPlayer.userID))
                return GetLang("CAN_REQUEST_TELEPORTATION_ONLY_REQUEST_FRIENDS", player.UserIDString);
            
            return String.Empty;
        }
        
        private Object OnMapMarkerAdd(BasePlayer player, MapNote note) 
        {
            if (player == null || note == null)
                return null;
            
            Vector3 positionTeleportation = note.worldPosition;
            positionTeleportation.y = GetGroundPosition(positionTeleportation);
            
            if (config.teleportationController.teleportSetting.useGMapTeleport)
            {
                RelationshipManager.PlayerTeam teams = player.Team;
                if (teams != null)
                {
                    foreach (UInt64 teamsMember in teams.members)
                    {
                        if (teamsMember == player.userID) continue;
                        BasePlayer teamPlayer = BasePlayer.FindByID(teamsMember);
                        if (teamPlayer == null) continue;
                        if (Vector3.Distance(positionTeleportation, teamPlayer.transform.position) < 30f)
                        {
                            RequestTeleportation(player, new []{ teamPlayer.UserIDString });
                            return false;
                        }
                    }
                }
            }

            if (config.teleportationController.teleportSetting.useGMapTeleportAdmin)
            {
                if (playerSettings[player.userID].useTeleportGMap && (player.IsAdmin || permission.UserHasPermission(player.UserIDString,
                        permissionGMapTeleport)))
                {
                    MovePlayer(player, positionTeleportation);
                    return false;
                }
            }

            return null;
        }
        
                
        
        [ChatCommand("sh")]
        private void SetHomeShortChatCMD(BasePlayer player, String cmd, String[] arg) =>
            SetHomeChatCMD(player, cmd, arg);
        
        private Dictionary<String, Vector3> GetHomes(UInt64 userID)
        {
            Dictionary<String, Vector3> homeList = new();
            if (!playerHomes.TryGetValue(userID, out Dictionary<String, PlayerHome> home)) return homeList;

            foreach (KeyValuePair<String, PlayerHome> pHome in home)
            {
                Vector3 positionHome = default;
                if (pHome.Value.netIdEntity != 0)
                {
                    if (tugboatsServer.TryGetValue(pHome.Value.netIdEntity, out Tugboat tugboat) && tugboat &&
                        !tugboat.IsDestroyed)
                        positionHome = tugboat.transform.TransformPoint(pHome.Value.positionHome);
                }
                else positionHome = pHome.Value.positionHome;

                if (positionHome == default) continue;

                homeList.TryAdd(pHome.Key, positionHome);
            }

            return homeList;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning(LanguageEn ? $"Error reading #321562 configuration 'oxide/config/{Name}', creating a new configuration!!" : $"Ошибка чтения #3212 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!"); 
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        
        
        private Dictionary<String, List<ServerWarp>> serverWarps = new();
		   		 		  						  	   		   		 		  			 		  	   		  				
        
        
        private void AutoTeleportTurn(BasePlayer player)
        {
            if (!playerSettings.TryGetValue(player.userID, out SettingPlayer setting)) return;
            setting.autoAcceptTeleportationFriends = !setting.autoAcceptTeleportationFriends;

            String message = setting.autoAcceptTeleportationFriends
                ? GetLang("AUTO_ACCEPT_TELEPORTATION_FRIEND_TRUE", player.UserIDString)
                : GetLang("AUTO_ACCEPT_TELEPORTATION_FRIEND_FALSE", player.UserIDString);
            
            SendChat(message, player);
        }
        
        private MonumentInfo GetMonumentInName(String nameMonument)
        {
            if (String.IsNullOrWhiteSpace(nameMonument)) return null;
            foreach (MonumentInfo monument in localAllMonuments)
            {
                if (monument.name.Equals(nameMonument))
                    return monument;
            }

            return null;
        }
        
        private void OnEntityKill(Tugboat tugboat)
        {
            if (tugboat == null) return;
            UInt64 netIDTugBoat = tugboat.net.ID.Value;

            if (tugboatsServer.ContainsKey(netIDTugBoat))
                tugboatsServer.Remove(netIDTugBoat);
        }
        
                
                
                
        private void LogAction(BasePlayer player, TypeLog typeLog, BasePlayer targetPlayer = default, Vector3 position = default, String homeName = default)
        {
            if (!config.generalController.useLogged) return;

            String typeAction = typeLog switch
            {
                TypeLog.RequestTeleportation => $"{player.displayName}({player.userID}) {tprLog} {targetPlayer.displayName}({targetPlayer.userID})",
                TypeLog.AcceptTeleportation => $"{player.displayName}({player.userID}) {tpaLog} {targetPlayer.displayName}({targetPlayer.userID})",
                TypeLog.RequestHomeTeleportation => $"{player.displayName}({player.userID}) {homeLog} `{homeName}` ({position})",
                _ => ""
            };
            LogToFile("IQTeleportation", typeAction, _, true, true);
        }

        private IEnumerator ProcessTTarget(BasePlayer player, BasePlayer targetPlayer, Int32 timeTeleportation)
        {
            Vector3 teleportationPosition = config.teleportationController.teleportSetting.teleportPosInAccept ? targetPlayer.transform.position : default;
            return ProcessTeleportation(
                player,
                () =>
                {
                    if (!targetPlayer)
                    {
                        ClearQueue(player, targetPlayer);
                        
                        SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", player.UserIDString, $"{GetLang("ACCESS_CANCELL_TELEPORTATION_DEAD_TARGET_DISCONNECTED", player.UserIDString)}"), player);
                        return true;
                    }

                    if (!targetPlayer.IsDead()) return false;
                    ClearQueue(player, targetPlayer);
                    SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", targetPlayer.UserIDString, $"{GetLang("ACCESS_CANCELL_TELEPORTATION_DEAD", targetPlayer.UserIDString)}"), targetPlayer);
                    SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", player.UserIDString, $"{GetLang("ACCESS_CANCELL_TELEPORTATION_DEAD_TARGET", player.UserIDString)}"), player);
                    return true;
                },
                () => TeleportationPlayer(player, targetPlayer, teleportationPosition),
                timeTeleportation,
                targetPlayer
            );
        }
        private const Single radiusCheckLayerHome = 2f;


        private void DestroyPing(BasePlayer player)
        {
            if (!player) return;
            if (!playerPings.TryGetValue(player, out MapNote note)) return;
            if (note == null) return;
            
            player.State.pings.Remove(note);
            player.DirtyPlayerState();
            player.SendPingsToClient();
            player.TeamUpdate(true);

            playerPings?.Remove(player);
        }
        private static void AddUI(BasePlayer player, String json) => CommunityEntity.ServerInstance.ClientRPC<String>(RpcTarget.Player("AddUI", player.net.connection), json);
        
        private void CreateMapMarkerPlayer(BasePlayer player, Vector3 position, String homeName)
        {
            if (player.State?.pointsOfInterest == null)
                player.State.pointsOfInterest = new List<MapNote>();
            
            if (player.State.pointsOfInterest.Count >= ConVar.Server.maximumMapMarkers) return;
            
            MapNote note = Pool.Get<MapNote>();
            note.worldPosition = position;
            note.isPing = false;
            
            note.colourIndex = 2;
            note.icon = 2;
            note.noteType = 1;
            
            note.label = homeName;
            player.State.pointsOfInterest.Add(note);
            player.DirtyPlayerState();
            player.TeamUpdate();

            using MapNoteList mapNoteList = Pool.Get<MapNoteList>();
            mapNoteList.notes = Pool.Get<List<MapNote>>();
            
            if (player.ServerCurrentDeathNote != null)
                mapNoteList.notes.Add(player.ServerCurrentDeathNote);

            if (player.State.pointsOfInterest != null)
                mapNoteList.notes.AddRange(player.State.pointsOfInterest);
                
            player.ClientRPC(RpcTarget.Player("Client_ReceiveMarkers", player), mapNoteList);
            mapNoteList.notes.Clear();
                
            Pool.FreeUnmanaged(ref mapNoteList.notes);
        }
        /// <summary>
        /// - Корректировка очистки списка установленных спальников игроков, для оптимизации
        /// - Исправление к обновлению игры
        /// - Исправлена некорректный выбор времени полета к игроку с привилегией, бралось время привилегии того к кому телепортируется игрока, а не того кто телепортируется
        /// </summary>
        
                
        private Timer timerCheckHomes = null;
        private Dictionary<BasePlayer, UserRepository> localUsersRepository = new();

        private void Unload()
        {
            WriteData();
            
            InterfaceBuilder.DestroyAll();
            
            if (_imageUI != null)
            {
                _imageUI.UnloadImages();
                _imageUI = null;
            }
            
            if (cachedUI != null)
            {
                cachedUI.Clear();
                cachedUI = null;
            }

            if (timerCheckHomes != null || !timerCheckHomes.Destroyed)
            {
                timerCheckHomes.Destroy();
                timerCheckHomes = null;
            }

            if (bagInstalled != null && bagInstalled.Count != 0)
            {
                bagInstalled.Clear();
                bagInstalled = null;
            }

            if (config.homeController.addedPingMarker)
            {
                if (playerPings != null)
                {
                    if (playerPings.Count != 0)
                    {
                        foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
                        {
                            if (playerPings.ContainsKey(basePlayer))
                                DestroyPing(basePlayer);
                        }

                        playerPings.Clear();
                    }

                    playerPings = null;
                }
            }

            _ = null;
        }
        
        private UInt64[] GetFriendList(BasePlayer targetPlayer)
        {
            List<UInt64> friendList = Pool.Get<List<UInt64>>();
            if (Friends)
            {
                if (Friends.Call("GetFriends", targetPlayer.userID.Get()) is UInt64[] frinedList)
                    friendList.AddRange(frinedList);
            }
            
            if (Clans)
            {
                if (Clans.Call("GetClanMembers", targetPlayer.UserIDString) is UInt64[] ClanMembers)
                    friendList.AddRange(ClanMembers);
            }

            if(targetPlayer.Team != null)
                friendList.AddRange(targetPlayer.Team.members);
            
            UInt64[] friendsArray = friendList.ToArray();
            Pool.FreeUnmanaged(ref friendList);
            
            return friendsArray;
        }
        
        private void ReadData()
        {
            playerSettings = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, SettingPlayer>>("IQSystem/IQTeleportation/PlayerSettings");
            playerHomes = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, Dictionary<String, PlayerHome>>>("IQSystem/IQTeleportation/Homes");
            serverWarps = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<String, List<ServerWarp>>>("IQSystem/IQTeleportation/Warps");
        }

        private void WriteData()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQTeleportation/PlayerSettings", playerSettings);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQTeleportation/Homes", playerHomes);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQTeleportation/Warps", serverWarps);
        }
        
        [ConsoleCommand("atp")]
        private void AutoAcceptTeleportConsoleCmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            AutoTeleportTurn(player);
        }
        private class SettingPlayer
        {
            public Boolean useTeleportationHomeFromFriends;
            public Boolean autoAcceptTeleportationFriends;

            public Boolean useTeleportGMap;
            
            public DateTime firstConnection;
        }
        private const String effectSoundAccess = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab";
        
        private void DrawUI_HomeSetup(BasePlayer player)
        {
            if (!config.homeController.suggetionSetHomeAfterInstallBed) return;
            if (_interface == null || player == null) return;
            
            player.Invoke(() =>
            {
                DestroyUI(player, InterfaceBuilder.IQ_TELEPORT_UI_HOME);
                
                if (bagInstalled.ContainsKey(player))
                    bagInstalled.Remove(player);
            }, 10f);
            
            String interfaceKey = InterfaceBuilder.IQ_TELEPORT_UI_HOME;
            List<String> cache = GetOrSetCacheUI(player, interfaceKey);
            if (cache != null && cache.Count != 0)
            {
                foreach (String uiCached in cache)
                    AddUI(player, uiCached);
                
                return;
            }
            
            String Interface = InterfaceBuilder.GetInterface(interfaceKey);
            if (Interface == null) return;
            
            Interface = Interface.Replace("%TITILE_PANEL%", GetLang("UI_SETUP_HOME_ALERT", player.UserIDString));
            Interface = Interface.Replace("%COMMAND_YES%", "ui_teleportation_command sethome"); 
            Interface = Interface.Replace("%COMMAND_NO%", "ui_teleportation_command cancell.sethome"); 

            List<String> newUI = GetOrSetCacheUI(player, interfaceKey, Interface);
            cache = newUI;
            
            foreach (String uiCached in cache)
                AddUI(player, uiCached);
        }
        
        private void ParseTugboats()
        {
            Int32 tugboatCount = 0;
            foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities.entityList.Get().Values)
            {
                if (serverEntity is not Tugboat) continue;
                
                if(!tugboatsServer.ContainsKey(serverEntity.net.ID.Value))
                {
                    tugboatsServer.Add(serverEntity.net.ID.Value, serverEntity as Tugboat);
                    tugboatCount++;
                }
            }
            Puts(LanguageEn ? "" : $"Сохранено {tugboatCount} буксиров для установки точки дома игроков");
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["UI_SETUP_HOME_ALERT"] = "DO YOU WANT TO SET A HOME POINT AT THIS LOCATION?",
                ["UI_TPR_REQUSET"] = "TELEPORTATION REQUEST FROM PLAYER {0}",

                ["SYNTAX_COMMAND_SETHOME"] = ":exclamation:To set a home point, use the syntax: <color=#1F6BA0>/sethome HomeName</color>",
                ["SYNTAX_COMMAND_REMOVEHOME"] = ":exclamation:To remove a home point, use the syntax: <color=#1F6BA0>/removehome HomeName</color>",
                ["SYNTAX_COMMAND_HOME_TELEPORTATION"] = ":exclamation:To teleport to a home point, use the syntax: <color=#1F6BA0>/home HomeName</color>",
                ["SYNTAX_COMMAND_TELEPORT_REQUEST"] = ":exclamation:To request teleportation to a player, use the syntax: <color=#1F6BA0>/tpr PlayerName</color>",
                ["SYNTAX_COMMAND_TELEPORT"] = ":exclamation:To teleport to a player, use the syntax: <color=#1F6BA0>/tp PlayerName</color>\nTo teleport one player to another, use: <color=#1F6BA0>/tp TargetPlayer TeleportPlayer</color>",

                ["SYNTAX_COMMAND_WARPS"] = ":exclamation:Use the syntax:" +
                                           "\n<color=#1F6BA0>/warp list</color> - to display all available warps" +
                                           "\n<color=#1F6BA0>/warp points WarpName</color> - to display all teleportation points for the specified warp" +
                                           "\n<color=#1F6BA0>/warp add WarpName</color> - to create a new warp or add an additional teleportation point to it" +
                                           "\n<color=#1F6BA0>/warp edit WarpName PointNumber</color> - to edit the teleportation point for the warp" +
                                           "\n<color=#1F6BA0>/warp remove WarpName</color> - to delete the warp and all its teleportation points" +
                                           "\n<color=#1F6BA0>/warp remove WarpName PointNumber</color> - to delete a specific teleportation point for the specified warp",

                ["HOMELIST_COMMAND_EMPTY"] = ":exclamation:You have no home points set",
                ["HOMELIST_COMMAND_ALL_LIST"] = ":yellowpin:Your set home points: {0}",
                ["HOMELIST_COMMAND_FORTMAT_HOME"] = "\n{0} in the quadrant [{1}]",

                ["MONUMENT_BLOCKED_SETUP_NOT_CENTER"] = ":yellowpin:Unable to determine the monument. Move closer to the center of the monument",
                ["MONUMENT_BLOCKED_SETU_BLOCKED"] = ":exclamation:This monument is already restricted for teleportation",
                ["MONUMENT_BLOCKED_SETUP_ACCES"] = ":exclamation:You have successfully restricted teleportation from this monument",

                ["COOLDOWN_MESSAGE"] = ":exclamation:You need to wait another <color=#C26D33>{0}</color> before performing this action",

                ["TITLE_FORMAT_DAYS"] = "D",
                ["TITLE_FORMAT_HOURSE"] = "H",
                ["TITLE_FORMAT_MINUTES"] = "M",
                ["TITLE_FORMAT_SECONDS"] = "S",

                ["WARP_NOTHING"] = ":exclamation:You have not created any warps yet",
                ["WARP_TELEPORTATION_PLAYER"] = ":yellowpin:You have been teleported to the warp",
                ["WARP_REQUEST_TELEPORTATION_PLAYER"] = ":yellowpin:You will be teleported to the warp in <color=#1F6BA0>{0} seconds</color>",
                ["WARP_NOTHING_NAME"] = ":exclamation:No warp with this name exists",
                ["WARP_NOT_ARG_NAME"] = ":exclamation:You need to specify the warp name",
                ["WARP_SHOW_POINTS"] = ":yellowpin:Points for teleportation to the warp <color=#738D45>{0}</color> will be displayed for <color=#1F6BA0>30 seconds</color>",
                ["WARP_SETUP_POINTS_DISTANCE"] = ":yellowpin:The new point is too close to point <color=#738D45>#{0}</color> of the specified warp. Move farther away and set the point there",
                ["WARP_SETUP_POINT_ACCESS"] = ":yellowpin:You have successfully added a new teleportation point to the warp named <color=#738D45>{0}</color>",
                ["WARP_SETUP_NEW"] = ":exclamation:The warp named <color=#738D45>{0}</color> has been successfully created. The command for teleportation to it is reserved: <color=#738D45>{1}</color>",
                ["WARP_EDIT_POINT_ACCESS"] = ":yellowpin:You have successfully edited the teleportation point for the warp named <color=#738D45>{0}</color>",
                ["WARP_NOTHING_KEY_POINTS"] = ":yellowpin:You need to specify the key (#) of the teleportation point to edit it\nAvailable keys: {0}",
                ["WARP_NOTHING_KEY_IS_NUMBER"] = ":yellowpin:The specified key (#) is not a number. You must specify only a number",
                ["WARP_REMOVED"] = ":exclamation:The warp has been successfully deleted.\nThe teleportation command: <color=#C26D33>{0}</color> - has been removed",
                ["WARP_REMOVED_POINT"] = ":yellowpin:The teleportation point <color=#C26D33>#{0}</color> has been deleted for the warp named <color=#1F6BA0>{1}</color>",
                ["WARP_REMOVED_POINT_NOTHING_KEYS"] = ":yellowpin:The point <color=#C26D33>#{0}</color> does not exist for the warp <color=#1F6BA0>{1}</color>\nAvailable keys: {2}",

                ["AUTO_ACCEPT_TELEPORTATION_FRIEND_TRUE"] = ":heart:You have <color=#738D45>enabled</color> auto-accept teleport requests from friends",
                ["AUTO_ACCEPT_TELEPORTATION_FRIEND_FALSE"] = ":heart:You have <color=#C26D33>disabled</color> auto-accept teleport requests from friends",
                
                ["GMAP_TELEPORT_TRUE"] = ":heart:You have <color=#738D45>enabled</color> teleportation via GMap",
                ["GMAP_TELEPORT_FALSE"] = ":heart:You have <color=#C26D33>disabled</color> teleportation via GMap",

                ["ACCESS_SETUP_SETHOME"] = ":yellowpin:You have successfully set a home point named <color=#738D45>{0}</color>",
                ["ACCESS_TELEPORTATION_HOME"] = ":yellowpin:You are teleporting to the home point named <color=#738D45>{0}</color>\nPlease wait: <color=#1F6BA0>{1}</color>",
                ["ACCESS_TELEPORTATION_HOME_FRIEND_NICK"] = ":yellowpin:You are teleporting to your friend's home point <color=#738D45>{0}</color> named <color=#738D45>{1}</color>\nPlease wait: <color=#1F6BA0>{2}</color>",
                ["ACCESS_TELEPORTATION_HOME_FRIEND"] = ":yellowpin:You are teleporting to your friend's home point named <color=#738D45>{0}</color>\nPlease wait: <color=#1F6BA0>{1}</color>",

                ["CAN_SETHOME_BUILDING"] = ":exclamation:You can only set a home point <color=#C26D33>on a building</color>",
                ["CAN_SETHOME_OTHER_ENTITY"] = ":exclamation:Another <color=#C26D33>object</color> is under or near this point, preventing you from setting a home point",
                ["CAN_SETHOME_BUILDING_OR_TERRAIN"] = ":exclamation:You cannot set a home point on <color=#C26D33>rocks or other objects</color>. Stand on a <color=#C26D33>building or ground surface</color>",
                ["CAN_SETHOME_BUILDING_PRIVILAGE"] = ":exclamation:To set a home point, you need to <color=#C26D33>place a tool cupboard</color> in the building",
                ["CAN_SETHOME_BUILDING_PRIVILAGE_AUTH"] = ":exclamation:To set a home point, you need to <color=#C26D33>authorize yourself in the tool cupboard</color>",
                ["CAN_SETHOME_BUILDING_PRIVILAGE_AUTH_TUGBOAT"] = ":exclamation:To set a home point, you need to <color=#C26D33>authorize yourself in the tugboat</color>",
                ["CAN_SETHOME_TUGBOAT_DISABLE"] = ":exclamation:You cannot set home points on tugboats; this <color=#C26D33>feature is disabled</color>",
                ["CAN_SETHOME_IS_RAIDBLOCK"] = ":exclamation:You cannot set a home point while an <color=#C26D33>active raid block</color> is in effect",
                ["CAN_SETHOME_OTHER_MESSAGE"] = ":exclamation:You cannot set a home point at the moment",
                ["CAN_SETHOME_EXIST_HOMENAME"] = ":exclamation:A home point named <color=#738D45>{0}</color> already exists",
                ["CAN_SETHOME_DISTANCE_EXIST_HOME"] = ":exclamation:You cannot set a home point because a point named <color=#738D45>{0}</color> is nearby",
                ["CAN_SETHOME_MAXIMUM_HOMES"] = ":exclamation:You cannot set a home point; you have reached the <color=#C26D33>maximum limit</color> of home points",

                ["A_HOME_CHECK_PLAYER_NOT_FOUND"] = ":exclamation:Player not found",
                ["A_HOME_CHECK_NOT_HOME"] = ":exclamation:The player has no available home points",
                ["A_HOME_CLEAR_HOMES"] = ":exclamation:All the player's home points have been deleted",
                ["A_HOME_SHOW_POINTS"] = ":yellowpin:The player's home points {0} are displayed for <color=#738D45>30 seconds</color>",

                ["CAN_REMOVE_HOME_NO_EXISTS"] = ":exclamation:You <color=#C26D33>do not have</color> a home point named <color=#738D45>{0}</color>",
                ["ACCESS_REMOVE_HOME"] = ":exclamation:You have <color=#C26D33>deleted</color> the home point named <color=#738D45>{0}</color>",

                ["ACCESS_CANCELL_TELEPORTATION_PLAYER"] = ":exclamation:You <color=#1F6BA0>canceled</color> the previous home teleportation request",
                ["ACCESS_CANCELL_TELEPORTATION"] = ":exclamation:The teleportation has been <color=#1F6BA0>canceled</color>",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD"] = "\nYou have been killed",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_TARGET"] = "\nThe player you were teleporting to has been <color=#1F6BA0>killed</color>",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_TARGET_DISCONNECTED"] = "\nThe player you were teleporting to has <color=#1F6BA0>disconnected</color> from the server",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_REQUESTER_DISCONNECTED"] = "\nThe player teleporting to you has <color=#1F6BA0>disconnected</color> from the server",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_REQUESTER_DEAD"] = "\nThe player teleporting to you has been <color=#1F6BA0>killed</color>",

                ["CAN_CANCELL_TELEPORTATION_NOT_FOUNDS"] = ":exclamation:You have no active teleportations",

                ["REQUEST_TELEPORTATION_ACCESS"] = ":heart:Your teleportation request has been accepted - please wait <color=#1F6BA0>{0}</color>\n<color=#C26D33>To cancel the request, use /tpc</color>",

                ["CAN_ACCEPT_TELEPORTATION_NOT_AUTO_ACCEPT"] = ":heart:Your request has been <color=#1F6BA0>sent</color>, but your friend <color=#C26D33>cannot</color> accept it at the moment",
                ["CAN_ACCEPT_TELEPORTATION_YES_AUTO_ACCEPT"] = ":heart:Your request has been <color=#1F6BA0>automatically accepted</color> by your friend",

                ["CAN_ACCEPT_TELEPORTATION_NOT_REQUEST"] = ":exclamation:You have no <color=#1F6BA0>active teleportation requests</color>",
                ["CAN_ACCEPT_TELEPORTATION_ACTIVE_ACCEPTED"] = ":exclamation:You have already <color=#1F6BA0>accepted a teleportation request</color>. Wait for the player to teleport!\n<color=#C26D33>To cancel the request, use /tpc</color>",

                ["CAN_REQUEST_TELEPORTATION_DISABLED"] = ":heart:Player-to-player teleportation is disabled on the server",
                ["CAN_REQUEST_TELEPORTATION_NOT_FOUND_PLAYER"] = ":exclamation:The player is not currently on the server",
                ["CAN_REQUEST_TELEPORTATION_ACCESS"] = ":heart:You have <color=#738D45>accepted</color> a teleportation request from <color=#1F6BA0>{0}</color>\nTo <color=#C26D33>cancel the request</color>, use <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_ACCESS_AUTO_TELEPORT"] = ":heart:You have <color=#1F6BA0>automatically</color> accepted a teleportation request from your friend <color=#1F6BA0>{0}</color>\nTo <color=#C26D33>cancel the request</color>, use <color=#C26D33>/tpc</color>\nTo <color=#C26D33>disable</color> automatic acceptance of teleportation requests from friends, type <color=#C26D33>/atp</color>",
                ["CAN_REQUEST_TELEPORTATION_SEND"] = ":heart:You have <color=#738D45>sent</color> a teleportation request to player <color=#1F6BA0>{0}</color>\nTo <color=#C26D33>cancel the request</color>, use <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_RECEIVE"] = ":heart:Teleportation request from player <color=#1F6BA0>{0}</color>\nTo <color=#738D45>accept the request</color>, use <color=#738D45>/tpa</color>\nTo <color=#C26D33>cancel the request</color>, use <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY"] = ":exclamation:You already have an <color=#C26D33>active teleportation request</color> with a player!\n<color=#C26D33>To cancel the request, use /tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY_NAME"] = ":exclamation:You already have an <color=#C26D33>active teleportation request</color> with player <color=#1F6BA0>{0}</color>\nTo <color=#C26D33>cancel the request</color>, use <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_NOTHING_TELEPORT_ME"] = ":exclamation:You cannot teleport to yourself",
                ["CAN_REQUEST_TELEPORTATION_ALREADY_REQUEST_TARGET"] = ":exclamation:The player you are sending the teleportation request to already has an <color=#C26D33>active request</color>. Wait for them to accept or cancel their request",
                ["CAN_REQUEST_TELEPORTATION_ONLY_REQUEST_FRIENDS"] = ":exclamation:You can only send requests <color=#C26D33>to friends</color>",

                ["FIND_PLAYER_INFO_MATCHES"] = ":exclamation:Your search with the nickname <color=#1F6BA0>'{0}'</color> found multiple players: <color=#1F6BA0>{1}</color>",
		   		 		  						  	   		   		 		  			 		  	   		  				
                ["CAN_TELEPORT_PLAYER_NOT_FOUND_TP_ONE_PLAYER"] = ":exclamation:The player <color=#C26D33>you want to teleport</color> was not found",
                ["CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER_ONE"] = ":exclamation:The player <color=#C26D33>you want to teleport</color> player <color=#C26D33>{0}</color> to was not found",
                ["CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER"] = ":exclamation:The player <color=#C26D33>you want to teleport to</color> was not found",
                ["CAN_TELEPORT_PLAYER_ACCES_TP"] = ":yellowpin:You have been teleported to player <color=#C26D33>{0}</color>",
                ["CAN_TELEPORT_PLAYER_NOT_SETTING_USE_FRIEND_HOME"] = ":exclamation:Your friend has <color=#C26D33>prohibited</color> teleportation to their home points",
                ["CAN_TELEPORT_NULL_REASON"] = ":exclamation:You cannot teleport at the moment",
                ["CAN_TELEPORT_RAID_BLOCK"] = ":exclamation:You cannot teleport during a <color=#C26D33>raid block</color>",
                ["CAN_TELEPORT_TITILE_OTHER"] = ":exclamation:You cannot teleport while: <color=#C26D33>{0}</color>",
                ["CAN_TELEPORT_IN_BUILDING_BLOCKED"] = "in another player's building zone",
                ["CAN_TELEPORT_IN_CARGO_SHIP"] = "on a cargo ship",
                ["CAN_TELEPORT_IN_HOT_AIR_BALLOON"] = "in a hot air balloon",
                ["CAN_TELEPORT_MONUMENT_BLOCKED"] = ":exclamation:Teleportation is prohibited from this monument!\n<color=#C26D33>Leave the monument or move farther away!</color>",
                ["CAN_TELEPORT_COOLDOWN"] = ":exclamation:You cannot teleport yet. Cooldown: <color=#C26D33>{0}</color>",
                ["CAN_TELEPORT_REQUEST_EXPIRED"] = ":exclamation:Your <color=#C26D33>teleportation request</color> has <color=#C26D33>expired</color> and was canceled",
                ["CAN_TELEPORT_ONLY_FRIEND"] = ":exclamation:You can only teleport to <color=#C26D33>friends</color>\nTeleportation was canceled",
                ["CAN_TELEPORT_ONLY_FRIEND_TARGET"] = ":exclamation:You can only teleport friends to yourself\nTeleportation was canceled",
                
                ["CAN_TELEPORT_HOME_FOUNDATION_DESTROYED"] = ":exclamation:The foundation where your home point was located has been destroyed. You cannot teleport",

                ["CAN_TELEPORT_HOME_TUGBOAT_DESTROYED"] = ":exclamation:The tugboat where your home point was located has been destroyed. You cannot teleport",
                ["CAN_TELEPORT_HOME_NOT_TUGBOAT"] = ":exclamation:The tugboat where the home point <color=#1F6BA0>{0}</color> was located has been <color=#C26D33>destroyed</color>",
                ["CAN_TELEPORT_HOME_NOT_HOME"] = ":exclamation:The home point named <color=#1F6BA0>{0}</color> does not exist",
                ["CAN_TELEPORT_HOME_NOT_BUILDING_BLOCK"] = ":exclamation:The home point named <color=#1F6BA0>{0}</color> is obstructed or the surface it was set on no longer exists\n<color=#C26D33>The home point has been deleted</color>",
                ["CAN_TELEPORT_HOME_NOT_AUTH_BUILDING_PRIVILAGE"] = ":exclamation:You are not authorized in the building where the home point <color=#1F6BA0>{0}</color> is located\nThe home point has been deleted",
                ["CAN_TELEPORT_HOME_NOT_FRIEND"] = ":exclamation:The player <color=#1F6BA0>{0}</color> is not your friend\nTo teleport to a friend's home, use: <color=#1F6BA0>/home HomeName FriendName</color>",
                ["CAN_TELEPORT_HOME_NOT_FOUND"] = ":exclamation:The player named <color=#1F6BA0>{0}</color> was not found!\nTo teleport to a friend's home, use: <color=#1F6BA0>/home HomeName FriendName</color>",

                ["STATUS_PLAYER_TITLE"] = ":exclamation:You cannot perform this action - <color=#C26D33>{0}</color>",
                ["STATUS_PLAYER_FLYING"] = "you are weightless",
                ["STATUS_PLAYER_WOUNDED"] = "you are wounded",
                ["STATUS_PLAYER_RADIATION"] = "you are irradiated",
                ["STATUS_PLAYER_MOUNTED"] = "you are sitting in a vehicle or on an object",
                ["STATUS_PLAYER_IS_TUTORIAL"] = "you are on a tutorial island",
                ["STATUS_PLAYER_IS_SWIMMING"] = "you are swimming",
                ["STATUS_PLAYER_IS_COLD"] = "you are cold",
                ["STATUS_PLAYER_IS_DEAD"] = "you are dead",
                ["STATUS_PLAYER_IS_SLEEPING"] = "you are sleeping",
            }, this);
            
            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["UI_SETUP_HOME_ALERT"] = "ХОТИТЕ УСТАНОВИТЬ ТОЧКУ ДОМА НА ЭТОМ МЕСТЕ?",
                ["UI_TPR_REQUSET"] = "ЗАПРОС НА ТЕЛЕПОРТАЦИЮ ОТ ИГРОКА {0}",

                ["SYNTAX_COMMAND_SETHOME"] = ":exclamation:Для установки точки дома используйте синтаксис : <color=#1F6BA0>/sethome НазваниеДома</color>",
                ["SYNTAX_COMMAND_REMOVEHOME"] = ":exclamation:Для удаления точки дома используйте синтаксис : <color=#1F6BA0>/removehome НазваниеДома</color>",
                ["SYNTAX_COMMAND_HOME_TELEPORTATION"] = ":exclamation:Для телепортации на точку дома используйте синтаксис : <color=#1F6BA0>/home НазваниеДома</color>",
                ["SYNTAX_COMMAND_TELEPORT_REQUEST"] = ":exclamation:Для телепортации к игроку используйте синтаксис : <color=#1F6BA0>/tpr ИмяИгрока</color>",
                ["SYNTAX_COMMAND_TELEPORT"] = ":exclamation:Для телепортации к игроку используйте синтаксис : <color=#1F6BA0>/tp ИмяИгрока</color>\nЧтобы телепортировать одного игрока к другому, используйте : <color=#1F6BA0>/tp ККомуТелепортировать КогоТелепортировать</color>",

                ["SYNTAX_COMMAND_WARPS"] = ":exclamation:Используйте синтаксис :" +
                                           "\n<color=#1F6BA0>/warp list</color> - для отображения всех доступных варпов" +
                                           "\n<color=#1F6BA0>/warp points НазваниеВарпа</color> - для отображения всех точек телепортации на данный варп" +
                                           "\n<color=#1F6BA0>/warp add НазваниеВарпа</color> - для создания нового варпа или установки дополнительной точки телепортации на этот варп" +
                                           "\n<color=#1F6BA0>/warp edit НазваниеВарпа НомерПозиции</color> - для редактирования позиции телепортации на варп" +
                                           "\n<color=#1F6BA0>/warp remove НазваниеВарпа</color> - для удаления варпа и всех доступных точек телепортации" +
                                           "\n<color=#1F6BA0>/warp remove НазваниеВарпа НомерПозиции</color> - для удаления позиции телепортации на указанный варп",
		   		 		  						  	   		   		 		  			 		  	   		  				
                ["HOMELIST_COMMAND_EMPTY"] = ":exclamation:У вас нет установленных точек дома",
                ["HOMELIST_COMMAND_ALL_LIST"] = ":yellowpin:Ваши установленные точки дома : {0}",
                ["HOMELIST_COMMAND_FORTMAT_HOME"] = "\n{0} в квадарте [{1}]",
                
                ["MONUMENT_BLOCKED_SETUP_NOT_CENTER"] = ":yellowpin:Невозможно определить монумент, встаньте ближе к центру данного монумента",
                ["MONUMENT_BLOCKED_SETU_BLOCKED"] = ":exclamation:Данный монумент уже запрещен для телепортации",
                ["MONUMENT_BLOCKED_SETUP_ACCES"] = ":exclamation:Вы успешно запретили телепортацию с данного монумента",
                
                ["COOLDOWN_MESSAGE"] = ":exclamation:Вам нужно подождать еще <color=#C26D33>{0}</color> перед выполнением этого действия",

                ["TITLE_FORMAT_DAYS"] = "Д",
                ["TITLE_FORMAT_HOURSE"] = "Ч",
                ["TITLE_FORMAT_MINUTES"] = "М",
                ["TITLE_FORMAT_SECONDS"] = "С",
                
                ["WARP_NOTHING"] = ":exclamation:Вы еще не создали варпов",
                ["WARP_TELEPORTATION_PLAYER"] = ":yellowpin:Вы были телепортированы на варп",
                ["WARP_REQUEST_TELEPORTATION_PLAYER"] = ":yellowpinВы будете телепортированы на варп в течении <color=#1F6BA0>{0} секунд</color>",
                ["WARP_NOTHING_NAME"] = ":exclamation:Варпа с таким названием не существует",
                ["WARP_NOT_ARG_NAME"] = ":exclamation:Вам необходимо указать название варпа",
                ["WARP_SHOW_POINTS"] = ":yellowpin:Точки с позициями для телепортации на варп <color=#738D45>{0}</color> будут отображены на <color=#1F6BA0>30 секунд</color>",
                ["WARP_SETUP_POINTS_DISTANCE"] = ":yellowpin:Новая точка находится слишком близко к точке <color=#738D45>№{0}</color> указанного варпа, попробуйте отойти подальше и установить точку там",
                ["WARP_SETUP_POINT_ACCESS"] = ":yellowpin:Вы успешно добавили новую позицию для варпа с названием <color=#738D45>{0}</color>",
                ["WARP_SETUP_NEW"] = ":exclamation:Варп с названием <color=#738D45>{0}</color> успешно создан, зарезервирована команда для телепортации на него : <color=#738D45>{1}</color>",
                ["WARP_EDIT_POINT_ACCESS"] = ":yellowpin:Вы успешно изменили позицию для варпа с названием <color=#738D45>{0}</color>",
                ["WARP_NOTHING_KEY_POINTS"] = ":yellowpin:Вам нужно указать ключ(№) точки с позицией для ее редактирования\nДоступные ключи : {0}",
                ["WARP_NOTHING_KEY_IS_NUMBER"] = ":yellowpin:Указанный ключ(№) позиции - не является цифрой, вы должны указать только цифру",
                ["WARP_REMOVED"] = ":exclamation:Варп успешно удален.\nКоманда для телепортации : <color=#C26D33>{0}</color> - удалена",
                ["WARP_REMOVED_POINT"] = ":yellowpin:Точка телепортации <color=#C26D33>№{0}</color> удалена для варпа с названием <color=#1F6BA0>{1}</color>",
                ["WARP_REMOVED_POINT_NOTHING_KEYS"] = ":yellowpin:Точка <color=#C26D33>№{0}</color> не существует для варпа <color=#1F6BA0>{1}</color>\nДоступные ключи : {2}",

                ["AUTO_ACCEPT_TELEPORTATION_FRIEND_TRUE"] = ":heart:Вы <color=#738D45>включили</color> автоматическое принятие телепортов от друзей",
                ["AUTO_ACCEPT_TELEPORTATION_FRIEND_FALSE"] = ":heart:Вы <color=#C26D33>отключили</color> автоматическое принятие телепортов от друзей",
                
                ["GMAP_TELEPORT_TRUE"] = ":heart:Вы <color=#738D45>включили</color> возможность телепортации по GMap",
                ["GMAP_TELEPORT_FALSE"] = ":heart:Вы <color=#C26D33>отключили</color> возможность телепортации по GMap",
                
                ["ACCESS_SETUP_SETHOME"] = ":yellowpin:Вы успешно установили точку дома с названием <color=#738D45>{0}</color>",
                ["ACCESS_TELEPORTATION_HOME"] = ":yellowpin:Вы телепортируетесь в точку дома с названием <color=#738D45>{0}</color>\nПожалуйста, подождите : <color=#1F6BA0>{1}</color>",
                ["ACCESS_TELEPORTATION_HOME_FRIEND_NICK"] = ":yellowpin:Вы телепортируетесь в точку дома друга <color=#738D45>{0}</color> с названием <color=#738D45>{1}</color>\nПожалуйста, подождите : <color=#1F6BA0>{2}</color>",
                ["ACCESS_TELEPORTATION_HOME_FRIEND"] = ":yellowpin:Вы телепортируетесь в точку дома друга с названием <color=#738D45>{0}</color>\nПожалуйста, подождите : <color=#1F6BA0>{1}</color>",
                
                ["CAN_SETHOME_BUILDING"] = ":exclamation:Вы можете установить точку для дома <color=#C26D33>только на строении</color>",
                ["CAN_SETHOME_OTHER_ENTITY"] = ":exclamation:Под точкой или рядом находится <color=#C26D33>другой объект</color>, который <color=#C26D33>мешает</color> установить точку для дома",
                ["CAN_SETHOME_BUILDING_OR_TERRAIN"] = ":exclamation:Вы не можете установить точку дома на <color=#C26D33>скалах или иных объектах</color>, встаньте на <color=#C26D33>строение или на поверхность земли</color>",
                ["CAN_SETHOME_BUILDING_PRIVILAGE"] = ":exclamation:Чтобы установить точку для дома - необходимо <color=#C26D33>установить шкаф</color> в строении",
                ["CAN_SETHOME_BUILDING_PRIVILAGE_AUTH"] = ":exclamation:Чтобы установить точку для дома - необходимо <color=#C26D33>авторизоваться в шкафу</color>",
                ["CAN_SETHOME_BUILDING_PRIVILAGE_AUTH_TUGBOAT"] = ":exclamation:Чтобы установить точку для дома - необходимо <color=#C26D33>авторизоваться в буксире</color>",
                ["CAN_SETHOME_TUGBOAT_DISABLE"] = ":exclamation:Вы не можете устанавливать точку дома на буксирах, эта <color=#C26D33>функция отключена</color>",
                ["CAN_SETHOME_IS_RAIDBLOCK"] = ":exclamation:Чтобы установить точку для дома - у вас не должно быть <color=#C26D33>активного рейдблока</color>",
                ["CAN_SETHOME_OTHER_MESSAGE"] = ":exclamation:Вы не можете сейчас установить точку для дома",
                ["CAN_SETHOME_EXIST_HOMENAME"] = ":exclamation:Точка для дома с названием <color=#738D45>{0}</color> уже существует",
                ["CAN_SETHOME_DISTANCE_EXIST_HOME"] = ":exclamation:Вы не можете установить точку для дома, рядом установлена точка <color=#738D45>{0}</color>",
                ["CAN_SETHOME_MAXIMUM_HOMES"] = ":exclamation:Вы не можете установить точку дома, достигнут <color=#C26D33>максимальный лимит</color> количества установленных точек для дома",
                
                ["A_HOME_CHECK_PLAYER_NOT_FOUND"] = ":exclamation:Игрок не найден",
                ["A_HOME_CHECK_NOT_HOME"] = ":exclamation:У игрока нет доступных точек дома",
                ["A_HOME_CLEAR_HOMES"] = ":exclamation:Все точки дома игрока были удалены",
                ["A_HOME_SHOW_POINTS"] = ":yellowpin:Точки дома игрока {0} отображаются на <color=#738D45>30 секунд</color>",
                
                ["CAN_REMOVE_HOME_NO_EXISTS"] = ":exclamation:У вас <color=#C26D33>не существует</color> точки дома с названием <color=#738D45>{0}</color>",
                ["ACCESS_REMOVE_HOME"] = ":exclamation:Вы <color=#C26D33>удалили</color> точку дома с названием <color=#738D45>{0}</color>",
                
                ["ACCESS_CANCELL_TELEPORTATION_PLAYER"] = ":exclamation:Вы <color=#1F6BA0>отменили</color> старый запрос на телепортацию домой",
                ["ACCESS_CANCELL_TELEPORTATION"] = ":exclamation:Телепортация была <color=#1F6BA0>отменена</color>",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD"] = "\nВы были убиты",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_TARGET"] = "\nИгрок к которому вы телепортировались был <color=#1F6BA0>убит</color>",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_TARGET_DISCONNECTED"] = "\nИгрок к которому вы телепортировались <color=#1F6BA0>вышел</color> с сервера",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_REQUESTER_DISCONNECTED"] = "\nИгрок который к вам телепортировался <color=#1F6BA0>вышел</color> с сервера",
                ["ACCESS_CANCELL_TELEPORTATION_DEAD_REQUESTER_DEAD"] = "\nИгрок который к вам телепортировался был <color=#1F6BA0>убит</color>",

                ["CAN_CANCELL_TELEPORTATION_NOT_FOUNDS"] = ":exclamation:У вас нет активных телепортаций",
                
                ["REQUEST_TELEPORTATION_ACCESS"] = ":heart:Ваш запрос на телепортацию принят - ожидайте <color=#1F6BA0>{0}</color>\n<color=#C26D33>Чтобы отменить запрос - используйте /tpc</color>",

                ["CAN_ACCEPT_TELEPORTATION_NOT_AUTO_ACCEPT"] = ":heart:Ваш запрос <color=#1F6BA0>отправлен</color>, но ваш друг <color=#C26D33>не может</color> принять его в данный момент", 
                ["CAN_ACCEPT_TELEPORTATION_YES_AUTO_ACCEPT"] = ":heart:Ваш запрос <color=#1F6BA0>автоматически принят</color> вашим другом", 
                
                ["CAN_ACCEPT_TELEPORTATION_NOT_REQUEST"] = ":exclamation:У вас нет <color=#1F6BA0>активных запросов</color> на телепортацию", 
                ["CAN_ACCEPT_TELEPORTATION_ACTIVE_ACCEPTED"] = ":exclamation:Вы уже <color=#1F6BA0>приняли запрос</color> на телепортацию, ожидайте телепортацию игрока!\n<color=#C26D33>Чтобы отменить запрос - используйте /tpc</color>", 

                ["CAN_REQUEST_TELEPORTATION_DISABLED"] = ":heart:На сервере отключена функция телепортации к игрокам", 
                ["CAN_REQUEST_TELEPORTATION_NOT_FOUND_PLAYER"] = ":exclamation:Такого игрока нет сейчас на сервере", 
                ["CAN_REQUEST_TELEPORTATION_ACCESS"] = ":heart:Вы <color=#738D45>приняли</color> запрос на телепортацию от <color=#1F6BA0>{0}</color>\nЧтобы <color=#C26D33>отменить запрос</color> - используйте <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_ACCESS_AUTO_TELEPORT"] = ":heart:Вы <color=#1F6BA0>автоматически</color> приняли запрос на телепортацию от вашего друга <color=#1F6BA0>{0}</color>\nЧтобы <color=#C26D33>отменить запрос</color> - используйте <color=#C26D33>/tpc</color>\nЧтобы <color=#C26D33>отключить</color> автоматическое принятие запросов на телепортацию от друзей - пропишите <color=#C26D33>/atp</color>",
                ["CAN_REQUEST_TELEPORTATION_SEND"] = ":heart:Вы <color=#738D45>отправили</color> запрос на телепортацию к игроку <color=#1F6BA0>{0}</color>\nЧтобы <color=#C26D33>отменить запрос</color> - используйте <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_RECEIVE"] = ":heart:Запрос на телепортацию от игрока <color=#1F6BA0>{0}</color>\nЧтобы <color=#738D45>принять запрос</color> - используйте <color=#738D45>/tpa</color>\nЧтобы <color=#C26D33>отменить запрос</color> - используйте <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY"] = ":exclamation:У вас уже есть <color=#C26D33>активный запрос</color> на телепортацию с игроком!\n<color=#C26D33>Чтобы отменить запрос - используйте /tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY_NAME"] = ":exclamation:У вас уже есть <color=#C26D33>активный запрос</color> на телепортацию с игроком <color=#1F6BA0>{0}</color>\nЧтобы <color=#C26D33>отменить запрос</color> - используйте <color=#C26D33>/tpc</color>",
                ["CAN_REQUEST_TELEPORTATION_NOTHING_TELEPORT_ME"] = ":exclamation:Вы не можете телепортироваться сами к себе",
                ["CAN_REQUEST_TELEPORTATION_ALREADY_REQUEST_TARGET"] = ":exclamation:У игрока к которому вы отправляете запрос на телепортацию, уже имеется <color=#C26D33>активный запрос</color>\nДождитесь когда он примет или отменит свой запрос",
                ["CAN_REQUEST_TELEPORTATION_ONLY_REQUEST_FRIENDS"] = ":exclamation:Вы можете отправлять запрос <color=#C26D33>только к друзьям</color>",
                
                ["FIND_PLAYER_INFO_MATCHES"] = ":exclamation:По вашему поиску с ником <color=#1F6BA0>'{0}'</color>, было найдено несколько игроков : <color=#1F6BA0>{1}</color>",
                
                ["CAN_TELEPORT_PLAYER_NOT_FOUND_TP_ONE_PLAYER"] = ":exclamation:Игрок <color=#C26D33>которого</color> вы собираетесь телепортировать не найден",
                ["CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER_ONE"] = ":exclamation:Игрок <color=#C26D33>к которому</color> вы собираетесь телепортировать игрока <color=#C26D33>{0}</color> - не найден",
                ["CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER"] = ":exclamation:Игрок <color=#C26D33>к которому</color> вы собираетесь телепортироваться не найден",
                ["CAN_TELEPORT_PLAYER_ACCES_TP"] = ":yellowpin:Вы были телепортированы к игроку <color=#C26D33>{0}</color>",
                ["CAN_TELEPORT_PLAYER_NOT_SETTING_USE_FRIEND_HOME"] = ":exclamation:Друг <color=#C26D33>запретил</color> телепортацию на свои точки дома",
                ["CAN_TELEPORT_NULL_REASON"] = ":exclamation:Вы не можете выполнить телепортацию сейчас",
                ["CAN_TELEPORT_RAID_BLOCK"] = ":exclamation:Вы не можете выполнить телепортацию во время <color=#C26D33>рейдблока</color>",
                ["CAN_TELEPORT_TITILE_OTHER"] = ":exclamation:Вы не можете выполнить телепортацию находясь : <color=#C26D33>{0}</color>", 
                ["CAN_TELEPORT_IN_BUILDING_BLOCKED"] = "в чужой билдинг зоне",
                ["CAN_TELEPORT_IN_CARGO_SHIP"] = "на корабле",
                ["CAN_TELEPORT_IN_HOT_AIR_BALLOON"] = "на воздушном шаре",
                ["CAN_TELEPORT_MONUMENT_BLOCKED"] = ":exclamation:С данного монумента запрещено телепортироваться!\n<color=#C26D33>Покиньте монумент или отойдите от него подальше!</color>",
                ["CAN_TELEPORT_COOLDOWN"] = ":exclamation:Вы не можете выполнить данную телепортацию, перезарядка : <color=#C26D33>{0}</color>",
                ["CAN_TELEPORT_REQUEST_EXPIRED"] = ":exclamation:Ваш <color=#C26D33>запрос</color> на телепортацию <color=#C26D33>истек</color>, он был отменен",
                ["CAN_TELEPORT_ONLY_FRIEND"] = ":exclamation:Вы можете телепортироваться только к <color=#C26D33>друзья</color>\nТелепортация была отменена",
                ["CAN_TELEPORT_ONLY_FRIEND_TARGET"] = ":exclamation:Вы можете телепортировать к себе только <color=#C26D33>друзей</color>\nТелепортация была отменена",

                ["CAN_TELEPORT_HOME_FOUNDATION_DESTROYED"] = ":exclamation:Фундамент на котором расположена точка дома был уничтожен, вы не можете телепортироваться",
                ["CAN_TELEPORT_HOME_TUGBOAT_DESTROYED"] = ":exclamation:Буксир на котором расположена точка дома был уничтожен, вы не можете телепортироваться",
                ["CAN_TELEPORT_HOME_NOT_TUGBOAT"] = ":exclamation:Буксир на котором расположена точка дома <color=#1F6BA0>{0}</color> был <color=#C26D33>уничтожен</color>",
                ["CAN_TELEPORT_HOME_NOT_HOME"] = ":exclamation:Точки дома с названием <color=#1F6BA0>{0}</color> - не существует",
                ["CAN_TELEPORT_HOME_NOT_BUILDING_BLOCK"] = ":exclamation:Точка дома с названием <color=#1F6BA0>{0}</color> застроена или у нее отсутсвует поверхность на которой она была установлена\n<color=#C26D33>Точка дома была удалена</color>",
                ["CAN_TELEPORT_HOME_NOT_AUTH_BUILDING_PRIVILAGE"] = ":exclamation:Вы не авторизованы в строении где установлена точка дома с названием <color=#1F6BA0>{0}</color>\nТочка дома была удалена",
                ["CAN_TELEPORT_HOME_NOT_FRIEND"] = ":exclamation:Игрок <color=#1F6BA0>{0}</color> не является вашим другом\nЧтобы телепортироваться на дом друга, используйте : <color=#1F6BA0>/home НазваниеДома ИмяДруга</color>",
                ["CAN_TELEPORT_HOME_NOT_FOUND"] = ":exclamation:Игрок с именем <color=#1F6BA0>{0}</color> не найден!\nЧтобы телепортироваться на дом друга, используйте : <color=#1F6BA0>/home НазваниеДома ИмяДруга</color>",

                ["STATUS_PLAYER_TITLE"] = ":exclamation:Вы не можете выполнить это действие - <color=#C26D33>{0}</color>",
                ["STATUS_PLAYER_FLYING"] = "вы в невесомости",
                ["STATUS_PLAYER_WOUNDED"] = "вы ранены",
                ["STATUS_PLAYER_RADIATION"] = "вы облучены радиацией",
                ["STATUS_PLAYER_MOUNTED"] = "вы сидите в транспорте или на объекте",
                ["STATUS_PLAYER_IS_TUTORIAL"] = "вы на обучающем острове",
                ["STATUS_PLAYER_IS_SWIMMING"] = "вы плаваете",
                ["STATUS_PLAYER_IS_COLD"] = "вам холодно",
                ["STATUS_PLAYER_IS_DEAD"] = "вы мертвы",
                ["STATUS_PLAYER_IS_SLEEPING"] = "вы спите",
            }, this, "ru");
        }
        
        private Boolean IsValidPosOtherEntity(in Vector3 position, BuildingBlock buildingBlock) 
        {
            Vector3 rayOrigin = position + Vector3.up; 
            Vector3 rayDirection = Vector3.down;
            
            RaycastHit[] hits = new RaycastHit[3]; 
            Int32 numHits = Physics.SphereCastNonAlloc(rayOrigin, 0.3f, rayDirection, hits, radiusCheckLayerHome, homelayerMaskEntity);

            for (Int32 i = 0; i < numHits; i++)
            {
                BaseEntity entity = hits[i].GetEntity();
		   		 		  						  	   		   		 		  			 		  	   		  				
                if (entity && buildingBlock && entity != buildingBlock && !exceptionsBuildingBlockEntities.Contains(entity.ShortPrefabName)) 
                    return false;
            }
                
            return true;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

        [ConsoleCommand("tpa")]
        private void TpaConsoleCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            AcceptRequestTeleportation(player);
        }
        
        
        
        [ChatCommand("homelist")]
        private void ChatCommand_HomeList(BasePlayer player)
        {
            Dictionary<String, Vector3> homeList = GetHomes(player.userID);
            if (homeList == null || homeList.Count == 0)
            {
                SendChat(GetLang("HOMELIST_COMMAND_EMPTY", player.UserIDString), player);
                return;
            }
            
            String homesPlayerOnCord = String.Empty;

            foreach (KeyValuePair<String, Vector3> homeInfo in homeList)
            {
                String cordMap = MapHelper.PositionToString(homeInfo.Value);
                homesPlayerOnCord += GetLang("HOMELIST_COMMAND_FORTMAT_HOME", player.UserIDString, homeInfo.Key, cordMap);
            }
            
            SendChat(GetLang("HOMELIST_COMMAND_ALL_LIST", player.UserIDString, homesPlayerOnCord), player);
        }
        
        private Dictionary<BasePlayer, SleepingBag> bagInstalled = new();
        
        
        
        
        [ChatCommand("mblock")]
        private void MBlockChatCMD(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            
            if(localAllMonuments.Count == 0)
                ParseMonuments();

            MonumentInfo playerMonument = GetMonumentWherePlayerStand(player);
            if (playerMonument == null)
            {
                SendChat(GetLang("MONUMENT_BLOCKED_SETUP_NOT_CENTER", player.UserIDString), player, true); 
                return;
            }
            
            if (config.generalController.monumentBlockedTeleportation.Contains(playerMonument.name))
            {
                SendChat(GetLang("MONUMENT_BLOCKED_SETU_BLOCKED", player.UserIDString) , player, true); 
                return;
            }
		   		 		  						  	   		   		 		  			 		  	   		  				
            config.generalController.monumentBlockedTeleportation.Add(playerMonument.name);
            SendChat(GetLang("MONUMENT_BLOCKED_SETUP_ACCES", player.UserIDString), player);
            SaveConfig();
        }
        
        private void OnNewSave(String filename) => AutoClearData(true);
        
        [ChatCommand("removehome")]
        private void RemoveHomeChatCMD(BasePlayer player, String cmd, String[] arg)
        {
            if (arg.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_REMOVEHOME", player.UserIDString), player, true);
                return;
            }

            String homeName = arg[0];
            RemoveHome(player, homeName);
        }

        private IEnumerator ProcessTHome(BasePlayer player, PlayerHome pHome, Int32 timeTeleportation)
        {
            Vector3 position = pHome.positionHome;
            BaseEntity parentEntity = null;
            return ProcessTeleportation(
                player,
                () =>
                {
                    if (pHome.netIdEntity == 0)
                    {
                        BuildingBlock buildingBlock = GetBuildingBlock(position);
                        if (!buildingBlock)
                        {
                            SendChat(GetLang("CAN_TELEPORT_HOME_FOUNDATION_DESTROYED", player.UserIDString), player, true);
                            return true;
                        }

                        return false;
                    }

                    if (!tugboatsServer.TryGetValue(pHome.netIdEntity, out Tugboat tugboat) || !tugboat || tugboat.IsDestroyed)
                    {
                        SendChat(GetLang("CAN_TELEPORT_HOME_TUGBOAT_DESTROYED", player.UserIDString), player, true);
                        return true;
                    }

                    parentEntity = tugboat;
                    return false;
                }, 
                () => TeleportationHome(player, position, parentEntity),
                timeTeleportation
            );
        }
        private void RequestWarp(BasePlayer player, in Vector3 warpPosition)
        {
            if (!config.warpsContoller.useWarps)
                return;
            
            String requestError = CanTeleportation(player, UserRepository.TeleportType.Warp);
            if (!String.IsNullOrWhiteSpace(requestError))
            {
                SendChat(requestError, player, true);
                return;
            }

            Int32 timeTeleportation = config.warpsContoller.teleportWarpCountdown.GetCount(player);
            Coroutine routineTeleportation = ServerMgr.Instance.StartCoroutine(ProcessTWarp(player, warpPosition, timeTeleportation));
            localUsersRepository[player].ActiveCoroutineController(routineTeleportation);
            
            SendChat(GetLang("WARP_REQUEST_TELEPORTATION_PLAYER", player.UserIDString, timeTeleportation), player);
        }
        private Dictionary<UInt64, AutoTurret> turretsEx = new();
		   		 		  						  	   		   		 		  			 		  	   		  				
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null) return;
            BaseEntity entity = go.ToBaseEntity();
            if (entity == null) return;
            SleepingBag bag = entity as SleepingBag;
            if (bag == null) return;
            BasePlayer player = plan.GetOwnerPlayer();
            if (player == null) return;
            
            Dictionary<String, PlayerHome> pData = playerHomes[player.userID];
            if (pData.Count >= config.homeController.homeCount.GetCount(player, true)) return;

            bagInstalled[player] = bag;
            
            DrawUI_HomeSetup(player);
        }
        
        
        
        [ChatCommand("rh")]
        private void RemoveHomeShortChatCMD(BasePlayer player, String cmd, String[] arg) => RemoveHomeChatCMD(player, cmd, arg);
        
        
        
        public Boolean IsRaidBlocked(BasePlayer player)
        {
            Object isRB = Interface.Call("IsRaidBlocked", player);
            Boolean isRaidBlocked = isRB as Boolean? ?? false;
            
            return isRaidBlocked;
        }
        
        private void CreatePingPlayer(BasePlayer player, Vector3 position)
        {
            if (player == null || player.State == null)
                return;

            player.State.pings ??= new List<MapNote>();
            
            DestroyPing(player);
            
            MapNote note = Pool.Get<MapNote>();
            if (note == null)
                return;
               
            note.worldPosition = position;
            note.isPing = true;
            note.timeRemaining = note.totalDuration = 30f;
            note.colourIndex = 2;
            note.icon = 2;
            player.State.pings.Add(note);
            player.DirtyPlayerState();
            player.TeamUpdate(false);

            using MapNoteList mapNoteList = Pool.Get<MapNoteList>();
            mapNoteList.notes = Pool.Get<List<MapNote>>();
            if (mapNoteList?.notes == null)
                return;

            mapNoteList.notes.AddRange(player.State.pings);
            player.ClientRPC(RpcTarget.Player("Client_ReceivePings", player), mapNoteList); 
            mapNoteList.notes.Clear();
            
            playerPings.TryAdd(player, note);

            Pool.FreeUnmanaged(ref mapNoteList.notes);
            
            player.Invoke(() => DestroyPing(player), note.timeRemaining);
        }

        [ConsoleCommand("tpc")]
        private void CancellTeleportCmdConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CancellTeleportation(player);
        }
        private class PlayerHome
        {
            public Vector3 positionHome;
            public UInt64 netIdEntity;
        }

        private void MovePlayer(BasePlayer player, Vector3 position, BaseEntity parentEntity = null, BaseEntity parentHomePosition = null)
        {
            Vector3 resultPosition = parentHomePosition ? parentHomePosition.transform.TransformPoint(position) : position;
            resultPosition.y += 0.3f;
        
            Interface.Call("OnPlayerTeleported", player, player.transform.position, resultPosition);
        
            player.PauseFlyHackDetection();
            player.PauseSpeedHackDetection();
            player.EnsureDismounted();
            player.Server_CancelGesture();
            player.StartSleeping();
        
            player.ClientRPC(RpcTarget.Player("StartLoading_Quick", player), true);
        
            player.SetParent(null, true);
        
            if (parentEntity)
                player.SetParent(parentEntity, true);
        
            player.Teleport(resultPosition);
        
            player.ForceUpdateTriggers();
            player.ClientRPC(RpcTarget.Player("ForceViewAnglesTo", player), resultPosition);
        
            player.UpdateNetworkGroup();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.SendNetworkUpdateImmediate();
        
            if (config.generalController.stopSleepingAfterTeleport)
                player.Invoke(player.EndSleeping, 0.5f);
        
            RunEffect(player, effectSoundTeleport);
        }
        
        
        private void ClearHomesPlayer(UInt64 userID, String pluginName)
        {
            if (!playerHomes.ContainsKey(userID)) return;
            playerHomes[userID].Clear();

            Puts(LanguageEn ? "" : $"{pluginName} : Все точки дома игрока {userID} были очищены");
        }
        
        
        
        private void RemoveHome(BasePlayer player, String homeName)
        {
            homeName = homeName.ToLower();
            Dictionary<String, PlayerHome> pData = playerHomes[player.userID];
            if (!pData.TryGetValue(homeName, out PlayerHome home))
            {
                SendChat(GetLang("CAN_REMOVE_HOME_NO_EXISTS", player.UserIDString, homeName), player, true);
                return;
            }
            
            Interface.Call("OnHomeRemoved", player, home.positionHome, homeName);
            
            pData.Remove(homeName);
            SendChat(GetLang("ACCESS_REMOVE_HOME", player.UserIDString, homeName), player);
        }

        private const Boolean LanguageEn = false;

        
        
        private void CancellTeleportation(BasePlayer player)
        {
            if (config.teleportationController.teleportSetting.suggetionAcceptedUI)
                DestroyUI(player, InterfaceBuilder.IQ_TELEPORT_UI_TELEPORT);
                
            UserRepository pData = localUsersRepository[player];
            if (playerTeleportationQueue.TryGetValue(player.userID, out TeleportationQueueInfo requester))
            {
                SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", player.UserIDString), player);
                
                if (requester.isActive)
                    SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", requester.player.UserIDString), requester.player);
            
                ClearQueue(player, requester.player);
                pData.DeActiveTeleportation();
                
                Interface.Call("OnTeleportRejected", player, requester.player);
                return;
            }

            if (pData.IsActiveTeleportation())
            {
                SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", player.UserIDString), player);
                pData.DeActiveTeleportation();
                
                Interface.Call("OnTeleportRejected", player, requester?.player);
		   		 		  						  	   		   		 		  			 		  	   		  				
                return;
            }
            
            SendChat(GetLang("CAN_CANCELL_TELEPORTATION_NOT_FOUNDS", player.UserIDString), player);
        }
        
        [ChatCommand("tpc")]
        private void CancellTeleportCmdChat(BasePlayer player) => CancellTeleportation(player);
        public class Configuration
        {
            [JsonProperty(LanguageEn ? "" : "Основные настройки плагина")]
            public GeneralController generalController = new GeneralController();
            [JsonProperty(LanguageEn ? "" : "Настройка телепортации к игроку")]
            public TeleportationController teleportationController = new TeleportationController();
            [JsonProperty(LanguageEn ? "" : "Настройка точек для дома")]
            public HomeController homeController = new HomeController();
            [JsonProperty(LanguageEn ? "" : "Настройка системы варпов")]
            public WarpsController warpsContoller = new WarpsController();
            internal class WarpsController
            {
                [JsonProperty(LanguageEn ? "" : "Включить поддержку варпов (true - да/false - нет)")]
                public Boolean useWarps;
                [JsonProperty(LanguageEn ? "" : "Настройка времени перезарядки телепортации на варп")]
                public PresetOtherSetting teleportWarpCooldown = new PresetOtherSetting();
                [JsonProperty(LanguageEn ? "" : "Настройка времени телепортации на варп")]
                public PresetOtherSetting teleportWarpCountdown = new PresetOtherSetting();
            }

            internal class GeneralController
            {
                [JsonProperty(LanguageEn ? "" : "Использовать логирование действий игроков в файл (true - да/false - нет)")]
                public Boolean useLogged;
                [JsonProperty(LanguageEn ? "" : "Использовать звуковые эффекты (true - да/false - нет)")]
                public Boolean useSoundEffects;
                [JsonProperty(LanguageEn ? "" : "Использовать GameTip сообщения, вместо сообщений в чате (true - да/false - нет)")]
                public Boolean useOnlyGameTip;
                [JsonProperty(LanguageEn ? "" : "Поднимать игрока сразу после телепортации, иначе он будет в состоянии `сна` (true - да/false - нет)")]
                public Boolean stopSleepingAfterTeleport;
                [JsonProperty(LanguageEn ? "" : "Список монументов с которых запрещено телепортироваться (Распространяется на точки дома/телепортацию к игрокам/варпы)")]
                public List<String> monumentBlockedTeleportation = new List<String>();
                [JsonProperty(LanguageEn ? "" : "Настройки IQChat")]
                public IQChatController iqchatSetting = new IQChatController();
                
                internal class IQChatController
                {
                    [JsonProperty(LanguageEn ? "IQChat : Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public String customPrefix;
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat (Steam64ID) (If required)" : "IQChat : Кастомный аватар в чате (Steam64ID) (Если требуется)")]
                    public String customAvatar;
                }
            }

            internal class TeleportationController
            {
                [JsonProperty(LanguageEn ? "" : "Настройка запросов на телепортацию")]
                public TeleportSetting teleportSetting = new TeleportSetting();
                [JsonProperty(LanguageEn ? "" : "Настройка времени перезарядки телепортации к игроку")]
                public PresetOtherSetting teleportCooldown = new PresetOtherSetting();
                [JsonProperty(LanguageEn ? "" : "Настройка времени телепортации к игроку")]
                public PresetOtherSetting teleportCountdown = new PresetOtherSetting();
                
                internal class TeleportSetting
                {
                    [JsonProperty(LanguageEn ? "" : "Предлагать принять запрос на телепортацию в UI (true - да/false - нет)")]
                    public Boolean suggetionAcceptedUI;
                    [JsonProperty(LanguageEn ? "" : "Режим телепортации, 1 (true) - телепортировать игрока на точку где был принят запрос, 2 (false) - телепортировать игрока к другому игроку не зависимо где был принят запрос")]
                    public Boolean teleportPosInAccept;
                    [JsonProperty(LanguageEn ? "" : "Разрешить игрокам отправлять запросы на телепортацию по G-Map (true - да/false - нет)")]
                    public Boolean useGMapTeleport;
                    [JsonProperty(LanguageEn ? "" : "Использовать моментальную телепортацию на точку через G-Map (true - да/false - нет) (требуются права администратора или разрешение iqteleportation.gmap)")]
                    public Boolean useGMapTeleportAdmin;
                    [JsonProperty(LanguageEn ? "" : "Отключить функции телепортации на сервере (tpr и tpa будут недоступны) (true - да/false - нет)")]
                    public Boolean disableTeleportationPlayer = false;
                    [JsonProperty(LanguageEn ? "" : "Запретить отправку и принятие запроса на телепортацию во время рейдблока (true - да/false - нет)")]
                    public Boolean noTeleportInRaidBlock = true;
                    [JsonProperty(LanguageEn ? "" : "Запретить отправку и принятие запроса на телепортацию когда игрок на корабле (true - да/false - нет)")]
                    public Boolean noTeleportCargoShip = true;
                    [JsonProperty(LanguageEn ? "" : "Запретить отправку и принятие запроса на телепортацию когда игрок на воздушном шаре (true - да/false - нет)")]
                    public Boolean noTeleportHotAirBalloon = true;
                    [JsonProperty(LanguageEn ? "" : "Запретить отправку и принятие запроса на телепортацию когда игроку холодно (true - да/false - нет)")]
                    public Boolean noTeleportFrozen = true;
                    [JsonProperty(LanguageEn ? "" : "Запретить отправку и принятие запроса на телепортацию когда игрок под радиацией (true - да/false - нет)")]
                    public Boolean noTeleportRadiation = true;
                    [JsonProperty(LanguageEn ? "" : "Запретить отправку и принятие запроса на телепортацию когда у игрока кровотечение(true - да/false - нет)")]
                    public Boolean noTeleportBlood = true;
                    [JsonProperty(LanguageEn ? "" : "Разрешать телепортацию только к друзьям (true - да/false - нет)")]
                    public Boolean onlyTeleportationFriends = true;
                }
            }
            
            internal class HomeController
            {
                [JsonProperty(LanguageEn ? "" : "Предлагать установку точки дома в UI после установки кровати или спальника (true - да/false - нет)")]
                public Boolean suggetionSetHomeAfterInstallBed;
                [JsonProperty(LanguageEn ? "" : "Добавлять визуальный эффект (пинг) на точку дома после ее установки (true - да/false - нет)")]
                public Boolean addedPingMarker;
                [JsonProperty(LanguageEn ? "" : "Добавлять маркер на G-Map игрока после установки дома (true - да/false - нет)")]
                public Boolean addedMapMarkerHome;
                [JsonProperty(LanguageEn ? "" : "Настройка разрешений на установку точек дома")]
                public SetupHome setupHomeController = new SetupHome();
                [JsonProperty(LanguageEn ? "" : "Настройка количества точек дома")]
                public PresetOtherSetting homeCount = new PresetOtherSetting();
                [JsonProperty(LanguageEn ? "" : "Настройка времени перезарядки телепортации на точку дома")]
                public PresetOtherSetting homeCooldown = new PresetOtherSetting();
                [JsonProperty(LanguageEn ? "" : "Настройка времени телепортации игрока на точку дома")]
                public PresetOtherSetting homeCountdown = new PresetOtherSetting();
                
                internal class SetupHome
                {
                    [JsonProperty(LanguageEn ? "" : "Запретить установку точки дома при рейдблоке (true - да/false - нет)")]
                    public Boolean noSetupInRaidBlock;
                    [JsonProperty(LanguageEn ? "" : "Разрешить установку точек дома на буксирах (true - да/false - нет)")]
                    public Boolean canSetupTugboatHome;
                    [JsonProperty(LanguageEn ? "" : "Разрешать установку точек дома только при наличии билдинг зоны (true - да/false - нет)")]
                    public Boolean onlyBuildingPrivilage;
                    [JsonProperty(LanguageEn ? "" : "Разрешать установку точек дома только при наличии авторизации в билдинг зоне (true - да/false - нет)")]
                    public Boolean onlyBuildingAuth;
                }
            }
            
            internal class PresetOtherSetting
            {
                [JsonProperty(LanguageEn ? "" : "Стандартное количество для игроков без разрешений")]
                public Int32 count;
                [JsonProperty(LanguageEn ? "" : "Настройка количества для игроков с разрешениями [Разрешение] = Количество")]
                public Dictionary<String, Int32> countPermissions;

                public Int32 GetCount(BasePlayer player, Boolean descending = false)
                {
                    foreach (KeyValuePair<String, Int32> countPermission in descending ? countPermissions.OrderByDescending(x => x.Value)
                                                                                       : countPermissions.OrderBy(x => x.Value))
                    {
                        if (_.permission.UserHasPermission(player.UserIDString, countPermission.Key))
                            return countPermission.Value;
                    }

                    return count;
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    generalController = new GeneralController
                    {
                        useSoundEffects = true,
                        useLogged = false,
                        useOnlyGameTip = false,
                        stopSleepingAfterTeleport = false,
                        monumentBlockedTeleportation = new List<String>()
                        {
                            
                        },
                        iqchatSetting = new GeneralController.IQChatController
                        {
                            customPrefix = "[IQTeleportation]",
                            customAvatar = "0"
                        }
                    },
                    warpsContoller = new WarpsController
                    {
                        useWarps = false,
                        teleportWarpCooldown = new PresetOtherSetting
                        {
                            count = 120,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 100,
                                ["iqteleportation.premium"] = 80,
                                ["iqteleportation.gold"] = 60,
                            }
                        },
                        teleportWarpCountdown = new PresetOtherSetting
                        {
                            count = 30,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 25,
                                ["iqteleportation.premium"] = 20,
                                ["iqteleportation.gold"] = 15,
                            }
                        }
                    },
                    teleportationController = new TeleportationController
                    {
                        teleportSetting = new TeleportationController.TeleportSetting
                        {
                            suggetionAcceptedUI = true,
                            teleportPosInAccept = false,
                            useGMapTeleport = true,
                            useGMapTeleportAdmin = true,
                            disableTeleportationPlayer = false,
                            noTeleportInRaidBlock = true,
                            noTeleportCargoShip = true,
                            noTeleportHotAirBalloon = true,
                            noTeleportBlood = true,
                            noTeleportFrozen = true,
                            noTeleportRadiation = true,
                            onlyTeleportationFriends = false,
                        },
                        teleportCooldown = new PresetOtherSetting
                        {
                            count = 60,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 45,
                                ["iqteleportation.premium"] = 35,
                                ["iqteleportation.gold"] = 25,
                            }
                        },
                        teleportCountdown = new PresetOtherSetting()
                        {
                            count = 20,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 15,
                                ["iqteleportation.premium"] = 10,
                                ["iqteleportation.gold"] = 5,
                            }
                        }
                    },
                    homeController = new HomeController()
                    {
                        suggetionSetHomeAfterInstallBed = true,
                        addedMapMarkerHome = true,
                        addedPingMarker = true,
                        setupHomeController = new HomeController.SetupHome
                        {
                            noSetupInRaidBlock = true,
                            canSetupTugboatHome = false,
                            onlyBuildingPrivilage = true,
                            onlyBuildingAuth = true,
                        },
                        homeCount = new PresetOtherSetting()
                        {
                            count = 2,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 3,
                                ["iqteleportation.premium"] = 4,
                                ["iqteleportation.gold"] = 5,
                            }
                        },
                        homeCooldown = new PresetOtherSetting()
                        {
                            count = 30,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 25,
                                ["iqteleportation.premium"] = 20,
                                ["iqteleportation.gold"] = 15,
                            }
                        },
                        homeCountdown = new PresetOtherSetting()
                        {
                            count = 20,
                            countPermissions = new Dictionary<String, Int32>()
                            {
                                ["iqteleportation.vip"] = 15,
                                ["iqteleportation.premium"] = 10,
                                ["iqteleportation.gold"] = 5,
                            }
                        },
                    }
                };
            }
        }

        
        
                
        
        private class ImageUI
        {
            private const String _path = "IQSystem/IQTeleportation/Images/";
            private const String _printPath = "data/" + _path;
            private readonly Dictionary<String, ImageData> _images = new()
            {
	             { "UI_IQTELEPORTATION_TEMPLATE", new ImageData() },
            };

            private enum ImageStatus
            {
                NotLoaded,
                Loaded,
                Failed
            }

            private class ImageData
            {
                public ImageStatus Status = ImageStatus.NotLoaded;
                public String Id { get; set; }
            }
		   		 		  						  	   		   		 		  			 		  	   		  				
            public String GetImage(String name)
            {
                if (_images.TryGetValue(name, out ImageData image) && image.Status == ImageStatus.Loaded)
                    return image.Id;
                return null;
            }

            public void DownloadImage()
            {
                KeyValuePair<String, ImageData>? image = null;
                foreach (KeyValuePair<String, ImageData> img in _images)
                {
                    if (img.Value.Status == ImageStatus.NotLoaded)
                    {
                        image = img;
                        break;
                    }
                }

                if (image != null)
                {
                    ServerMgr.Instance.StartCoroutine(ProcessDownloadImage(image.Value));
                }
                else
                {
                    List<String> failedImages = new List<String>();

                    foreach (KeyValuePair<String, ImageData> img in _images)
                    {
                        if (img.Value.Status == ImageStatus.Failed)
                        {
                            failedImages.Add(img.Key);
                        }
                    }

                    if (failedImages.Count > 0)
                    {
                        String images = String.Join(", ", failedImages);
                        _.PrintError(LanguageEn
                            ? $"Failed to load the following images: {images}. Perhaps you did not upload them to the '{_printPath}' folder. You can download it here - https://drive.google.com/drive/folders/1abiKOnLRx7m3YD7LHugPA7dSr2MCaMdi?usp=sharing" 
                            : $"Не удалось загрузить следующие изображения: {images}. Возможно, вы не загрузили их в папку '{_printPath}'. Скачать можно тут - https://drive.google.com/drive/folders/1abiKOnLRx7m3YD7LHugPA7dSr2MCaMdi?usp=sharing");
                        Interface.Oxide.UnloadPlugin(_.Name);
                    }
                    else
                    {
                        _.Puts(LanguageEn
                            ? $"{_images.Count} images downloaded successfully!"
                            : $"{_images.Count} изображений успешно загружено!");
                        
                        _interface = new InterfaceBuilder();
                    }
                }
            }
            
            public void UnloadImages()
            {
                foreach (KeyValuePair<String, ImageData> item in _images)
                    if(item.Value.Status == ImageStatus.Loaded)
                        if (item.Value?.Id != null)
                            FileStorage.server.Remove(uint.Parse(item.Value.Id), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);

                _images?.Clear();
            }

            private IEnumerator ProcessDownloadImage(KeyValuePair<String, ImageData> image)
            {
                String url = "file://" + Interface.Oxide.DataDirectory + "/" + _path + image.Key + ".png";

                using UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();

                if (www.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                {
                    image.Value.Status = ImageStatus.Failed;
                }
                else
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(www);
                    image.Value.Id = FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                    image.Value.Status = ImageStatus.Loaded;
                    UnityEngine.Object.DestroyImmediate(tex);
                }

                DownloadImage();
            }
        }
        
        private static void DrawSphereAndText(BasePlayer player, in Vector3 position, String text, Single time = 30f)
        {
            player.SendConsoleCommand("ddraw.text", time, Color.green, position + Vector3.up, $"<size=35>{text}</size>");
            player.SendConsoleCommand("ddraw.sphere", time, Color.green, position, 0.5f);
        }
        private Dictionary<UInt64, SettingPlayer> playerSettings = new();
        
        [ChatCommand("tpa")]
        private void TpaChatCMD(BasePlayer player, String cmd, String[] arg) => AcceptRequestTeleportation(player);

        private void GenerateWarp()
        {
            cmd.AddChatCommand("warp", this, nameof(ChatCommandWarp));
            if (serverWarps.Keys.Count == 0)
                Puts(LanguageEn ? "" : "Варпы не были инициализированы, количество существующих варпов : 0");
            else
            {
                String warps = String.Join(", ", serverWarps.Keys);
                foreach ((String warpName, List<ServerWarp> value) in serverWarps)
                {
                    Boolean allHidden = value.All(warp => warp.hideWarp);
                    if (allHidden) continue;
                    
                    cmd.AddChatCommand(warpName, this, nameof(ChatCommandTeleportationWarp));
                    cmd.AddConsoleCommand(warpName, this, nameof(ConsoleCommandTeleportationWarp));
                }

                Puts(LanguageEn ? "" : $"Инициализировано {serverWarps.Keys.Count} варпов, для них зарезервированы команды : {warps}");
            }
        }
        
                
        
        private Boolean IsFriends(BasePlayer player, UInt64 targetID)
        {
            if (player.userID == targetID) return true;
            UInt64[] FriendList = GetFriendList(player);
            return FriendList != null && FriendList.Contains(targetID);
        }

                
                private static void DestroyUI(BasePlayer player, String elementName) => CommunityEntity.ServerInstance.ClientRPC<String>(RpcTarget.Player("DestroyUI", player.net.connection), elementName);
        private void OnPlayerConnected(BasePlayer player) => RegisteredUser(player);
        private const String tprLog = LanguageEn ? "" : "отправил запрос на телепортацию к";
        
        
        
        [PluginReference] Plugin IQChat, Clans, Friends;

        private String CanRequestTeleportHome(BasePlayer player, String homeName, String friendNameOrID, out UInt64 userIDFriend, out String friendName)
        {
            CheckAllHomes(player, homeName);

            userIDFriend = default;
            friendName = String.Empty;
            UInt64 userID = player.userID;

            String canTeleport = CanTeleportation(player, UserRepository.TeleportType.Home);
            if (!String.IsNullOrWhiteSpace(canTeleport))
                return canTeleport;

            if (playerTeleportationQueue.ContainsKey(player.userID))
            {
                return playerTeleportationQueue[player.userID].player != null
                    ? GetLang("CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY_NAME", player.UserIDString,
                        playerTeleportationQueue[player.userID].player.displayName)
                    : GetLang("CAN_REQUEST_TELEPORTATION_TELEPORTED_REQUEST_ACTUALY", player.UserIDString);
            }

            if (!String.IsNullOrWhiteSpace(friendNameOrID))
            {
                BasePlayer bPlayer = BasePlayer.FindAwakeOrSleeping(friendNameOrID); 
                if (bPlayer == null)
                    return GetLang("CAN_TELEPORT_HOME_NOT_FOUND", player.UserIDString, friendNameOrID);

                userIDFriend = bPlayer.userID;
                friendName = bPlayer.displayName;

                if (!IsFriends(player, userIDFriend))
                    return GetLang("CAN_TELEPORT_HOME_NOT_FRIEND", player.UserIDString, friendNameOrID);

                userID = userIDFriend;

                if (!playerSettings[userIDFriend].useTeleportationHomeFromFriends && userID != player.userID)
                    return GetLang("CAN_TELEPORT_PLAYER_NOT_SETTING_USE_FRIEND_HOME", player.UserIDString);
            }

            if (!playerHomes.TryGetValue(userID, out Dictionary<String, PlayerHome> pData) ||
                !pData.TryGetValue(homeName, out PlayerHome homePlayer))
                return GetLang("CAN_TELEPORT_HOME_NOT_HOME", player.UserIDString, homeName);

            if (Interface.Call("CanTeleportHome", player, homePlayer.positionHome) is String hookResult)
                return String.IsNullOrWhiteSpace(hookResult) ? GetLang("CAN_TELEPORT_NULL_REASON", player.UserIDString) : hookResult;
            
            if (homePlayer.netIdEntity != 0)
            {
                if (!tugboatsServer.TryGetValue(homePlayer.netIdEntity, out Tugboat tugboat) || tugboat == null || tugboat.IsDestroyed)
                    return GetLang("CAN_TELEPORT_HOME_NOT_TUGBOAT", player.UserIDString, homeName);
                
                return String.Empty;
            }

            BuildingBlock buildingBlock = GetBuildingBlock(homePlayer.positionHome);
            if (!buildingBlock)
            {
                pData.Remove(homeName);
                return GetLang("CAN_TELEPORT_HOME_NOT_BUILDING_BLOCK", player.UserIDString, homeName);
            }

            if (config.homeController.setupHomeController.onlyBuildingAuth)
            {
                BuildingPrivlidge privilege = buildingBlock.GetBuildingPrivilege();
                if (privilege != null && !privilege.IsAuthed(player))
                {
                    pData.Remove(homeName);
                    return GetLang("CAN_TELEPORT_HOME_NOT_AUTH_BUILDING_PRIVILAGE", player.UserIDString, homeName);
                }
            }

            return String.Empty;
        }
        private Dictionary<BasePlayer, Action> teleportationActions = new();
        
        private void RegisteredPermissions()
        {
            
            if (config.warpsContoller.useWarps)
            {
                foreach (String countWarpCooldownPermissions in config.warpsContoller.teleportWarpCooldown
                             .countPermissions.Keys)
                {
                    if (!permission.PermissionExists(countWarpCooldownPermissions, this))
                        permission.RegisterPermission(countWarpCooldownPermissions, this);
                }

                foreach (String countWarpCoutdownPermissions in config.warpsContoller.teleportWarpCountdown
                             .countPermissions.Keys)
                {
                    if (!permission.PermissionExists(countWarpCoutdownPermissions, this))
                        permission.RegisterPermission(countWarpCoutdownPermissions, this);
                }
            }
		   		 		  						  	   		   		 		  			 		  	   		  				
            
            
            foreach (String countTeleportCooldownPermissions in config.teleportationController.teleportCooldown
                         .countPermissions.Keys)
            {
                if (!permission.PermissionExists(countTeleportCooldownPermissions, this))
                    permission.RegisterPermission(countTeleportCooldownPermissions, this);
            }

            foreach (String countTeleportCoutdownPermissions in config.teleportationController.teleportCountdown
                         .countPermissions.Keys)
            {
                if (!permission.PermissionExists(countTeleportCoutdownPermissions, this))
                    permission.RegisterPermission(countTeleportCoutdownPermissions, this);
            }

            
            
            foreach (String countHomePermissions in config.homeController.homeCount.countPermissions.Keys)
            {
                if (!permission.PermissionExists(countHomePermissions, this))
                    permission.RegisterPermission(countHomePermissions, this);
            }

            foreach (String countHomeColldownPermissions in config.homeController.homeCooldown.countPermissions.Keys)
            {
                if (!permission.PermissionExists(countHomeColldownPermissions, this))
                    permission.RegisterPermission(countHomeColldownPermissions, this);
            }

            foreach (String countHomeCoutdownPermissions in config.homeController.homeCountdown.countPermissions.Keys)
            {
                if (!permission.PermissionExists(countHomeCoutdownPermissions, this))
                    permission.RegisterPermission(countHomeCoutdownPermissions, this);
            }

            
            if (!permission.PermissionExists(permissionGMapTeleport, this))
                permission.RegisterPermission(permissionGMapTeleport, this);

            if (!permission.PermissionExists(permissionTP, this))
                permission.RegisterPermission(permissionTP, this);
        }

                
        
        
        private void Init()
        {
            _ = this;
            ReadData();

            if (!config.homeController.addedPingMarker)
                Unsubscribe(nameof(OnPlayerDisconnected));
            
            if(!config.homeController.suggetionSetHomeAfterInstallBed)
                Unsubscribe(nameof(OnEntityBuilt));

            if (!config.homeController.setupHomeController.canSetupTugboatHome)
            {
                Unsubscribe(nameof(OnEntitySpawned));
                Unsubscribe(nameof(OnEntityKill));
            }

            if (!config.teleportationController.teleportSetting.useGMapTeleport &&
                !config.teleportationController.teleportSetting.useGMapTeleportAdmin)
                Unsubscribe(nameof(OnMapMarkerAdd));
        }
        private readonly LayerMask homelayerMaskEntity = LayerMask.GetMask("Default", "Deployed");

        
        
        private void ValidateWarps()
        {
            if (config.warpsContoller.useWarps)
            {
                foreach (KeyValuePair<String, List<ServerWarp>> allWarps in serverWarps)
                {
                    for (Int32 i = 0; i < allWarps.Value.Count; i++)
                    {
                        ServerWarp warp = allWarps.Value[i];
                        if (String.IsNullOrWhiteSpace(warp.monumentParent)) continue;
                        MonumentInfo monument = GetMonumentInName(warp.monumentParent);
                        if (monument != null)
                        {
                            serverWarps[allWarps.Key][i].hideWarp = false;
                            continue;
                        }
                        
                        serverWarps[allWarps.Key][i].hideWarp = true;
                    }
                }

                GenerateWarp();
            }
            else if (serverWarps.Keys.Count != 0)
            {
                foreach (String warpName in serverWarps.Keys)
                {
                    cmd.RemoveChatCommand(warpName, this);
                    cmd.RemoveConsoleCommand(warpName, this);
                }
            }
        }
        private const Single uiAlertFade = 0.222f;
		   		 		  						  	   		   		 		  			 		  	   		  				
        private String GetWarpPoints(String warpName)
        {
            String pointsKeys = String.Empty;
            if (!serverWarps.TryGetValue(warpName, out List<ServerWarp> warp)) return pointsKeys;
            for (Int32 i = 0; i < warp.Count; i++)
                pointsKeys += $"\n№{i}";

            return pointsKeys;
        }
        
        private void DrawUI_TeleportPlayer(BasePlayer player, BasePlayer targetPlayer)
        {
            if (_interface == null || !player || !targetPlayer) return;
            
            targetPlayer.Invoke(() =>
            {
                DestroyUI(targetPlayer, InterfaceBuilder.IQ_TELEPORT_UI_TELEPORT);
            }, 15f);
            
            String interfaceKey = $"{InterfaceBuilder.IQ_TELEPORT_UI_TELEPORT}_{targetPlayer.UserIDString}";
            List<String> cache = GetOrSetCacheUI(targetPlayer, interfaceKey);
            if (cache != null && cache.Count != 0)
            {
                foreach (String uiCached in cache)
                    AddUI(targetPlayer, uiCached);

                return;
            }
            
            String Interface = InterfaceBuilder.GetInterface(InterfaceBuilder.IQ_TELEPORT_UI_TELEPORT);
            if (Interface == null) return;

            String displayName = player.displayName.Length > 10 ? player.displayName.Substring(0, 7) + "..." : player.displayName;
            String titleRequest = GetLang("UI_TPR_REQUSET", targetPlayer.UserIDString, displayName.ToUpper());
		   		 		  						  	   		   		 		  			 		  	   		  				
            Interface = Interface.Replace("%TITILE_PANEL%", titleRequest);
            Interface = Interface.Replace("%COMMAND_YES%", "ui_teleportation_command tpa"); 
            Interface = Interface.Replace("%COMMAND_NO%", "ui_teleportation_command tpc"); 

            List<String> newUI = GetOrSetCacheUI(targetPlayer, interfaceKey, Interface);
            cache = newUI;
            
            foreach (String uiCached in cache)
                AddUI(targetPlayer, uiCached);
        }
        
                
        [ChatCommand("tpr")]
        private void TprChatCMD(BasePlayer player, String cmd, String[] arg)
        {
            if (config.teleportationController.teleportSetting.disableTeleportationPlayer)
            {
                SendChat(GetLang("CAN_REQUEST_TELEPORTATION_DISABLED", player.UserIDString), player, true);
                return;
            }
            
            if (arg.Length == 0 && !casheTeleportationLast.ContainsKey(player))
            {
                SendChat(GetLang("SYNTAX_COMMAND_TELEPORT_REQUEST", player.UserIDString), player, true);
                return;
            }
        
            RequestTeleportation(player, arg);
        }
        

                
        
        private String CheckStatusPlayer(BasePlayer player, Boolean canSethome = false)
        {
            if (!canSethome)
            {
                if (config.teleportationController.teleportSetting.noTeleportFrozen && player.metabolism.temperature.value < 0)
                    return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_IS_COLD", player.UserIDString));

                if ((config.teleportationController.teleportSetting.noTeleportBlood && player.metabolism.bleeding.value > 0)|| player.IsWounded())
                    return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_WOUNDED", player.UserIDString));
		   		 		  						  	   		   		 		  			 		  	   		  				
                if (config.teleportationController.teleportSetting.noTeleportRadiation && player.metabolism.radiation_poison.value > 0)
                    return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_RADIATION", player.UserIDString));
            }

            if (player.IsDead())
                return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_IS_DEAD", player.UserIDString));

            if (player.IsSleeping())
                return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_IS_SLEEPING", player.UserIDString));

            if (player.IsSwimming())
                return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_IS_SWIMMING", player.UserIDString));

            if (player.IsInTutorial)
                return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_IS_TUTORIAL", player.UserIDString));

            if (player.IsFlying)
                return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_FLYING", player.UserIDString));

            if (player.isMounted)
                return GetLang("STATUS_PLAYER_TITLE", player.UserIDString, GetLang("STATUS_PLAYER_MOUNTED", player.UserIDString));
            

            return String.Empty;
        }
		   		 		  						  	   		   		 		  			 		  	   		  				
        
        
        private void GMapTeleportTurn(BasePlayer player)
        {
            if (!playerSettings.TryGetValue(player.userID, out SettingPlayer setting)) return;
            setting.useTeleportGMap = !setting.useTeleportGMap;

            String message = setting.useTeleportGMap
                ? GetLang("GMAP_TELEPORT_TRUE", player.UserIDString)
                : GetLang("GMAP_TELEPORT_FALSE", player.UserIDString);
            
            SendChat(message, player);
        }
        
        private void ConsoleCommandTeleportationWarp(ConsoleSystem.Arg arg)
        {
            if (!config.warpsContoller.useWarps) return;
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!serverWarps.TryGetValue(arg.cmd.Name, out List<ServerWarp> warpPoints)) return;
            ServerWarp randomWarp = GetRandomWarp(warpPoints);
            if (randomWarp == null) return;
            Vector3 warpPoint = randomWarp.GetPositionWarp();
            if (warpPoint == default) return;
            
            RequestWarp(player, warpPoint);
        }
        
        private void RegisteredUser(BasePlayer player)
        {
            localUsersRepository.TryAdd(player, new UserRepository
            {
                activeTeleportation = null,
                cooldownHome = 0,
                cooldownTeleportation = 0
            });

            playerSettings.TryAdd(player.userID, new SettingPlayer
            {
                useTeleportationHomeFromFriends = true,
                useTeleportGMap = true,
                autoAcceptTeleportationFriends = false,
                firstConnection = DateTime.Now,
            });

            playerHomes.TryAdd(player.userID, new Dictionary<String, PlayerHome>());
        }
        
        [ConsoleCommand("tpr")]
        private void TprConsoleCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (config.teleportationController.teleportSetting.disableTeleportationPlayer)
            {
                SendChat(GetLang("CAN_REQUEST_TELEPORTATION_DISABLED", player.UserIDString), player, true);
                return;
            }
            
            if (arg.Args.Length == 0 && !casheTeleportationLast.ContainsKey(player))
            {
                SendChat(GetLang("SYNTAX_COMMAND_TELEPORT_REQUEST", player.UserIDString), player, true);
                return;
            }
        
            RequestTeleportation(player, arg.Args);
        }
        
                
        
        private static StringBuilder sb = new StringBuilder();
        
        
        [ChatCommand("a.home")]
        private void ChatCommandAdminHome(BasePlayer player, String cmd, String[] arg)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;

            String action = arg[0];
            String nameOrIdOrIp = arg[1];

            BasePlayer bPlayer = BasePlayer.FindAwakeOrSleeping(nameOrIdOrIp);
            if (bPlayer == null)
            {
                SendChat(GetLang("A_HOME_CHECK_PLAYER_NOT_FOUND", player.UserIDString), player, true);
                return;
            }

            if (!playerHomes.TryGetValue(bPlayer.userID, out Dictionary<String, PlayerHome> playerHome))
            {
                SendChat(GetLang("A_HOME_CHECK_NOT_HOME", player.UserIDString), player, true);
                return;
            }

            switch (action)
            {
                case "clear":
                {
                    playerHome.Clear();
                    SendChat(GetLang("A_HOME_CLEAR_HOMES", player.UserIDString), player);
                    break;
                }
                case "points":
                {
                    foreach (KeyValuePair<String, PlayerHome> pHome in playerHome)
                    {
                        Vector3 positionHome = pHome.Value.positionHome;
                        
                        if(pHome.Value.netIdEntity != 0)
                            if (tugboatsServer.TryGetValue(pHome.Value.netIdEntity, out Tugboat tugboat) && tugboat && !tugboat.IsDestroyed)
                                positionHome = tugboat.transform.TransformPoint(pHome.Value.positionHome);

                        DrawSphereAndText(player, positionHome, pHome.Key);
                    }
                    
                    SendChat(GetLang("A_HOME_SHOW_POINTS", player.UserIDString, bPlayer.displayName), player);
                    break;
                }
            }
        }

        [ConsoleCommand("tp")]
        private void TpConsoleCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, permissionTP)) return;
            
            if (arg.Args.Length == 0 && !casheTeleportationLast.ContainsKey(player))
            {
                SendChat(GetLang("SYNTAX_COMMAND_TELEPORT", player.UserIDString), player, true);
                return;
            }

            String nameOrIDTarget = arg.Args[0];
            (BasePlayer, String) findPlayer = FindPlayerNameOrID(player, nameOrIDTarget);
            BasePlayer targetPlayer = null;
            if(!String.IsNullOrWhiteSpace(nameOrIDTarget))
                targetPlayer = findPlayer.Item1;
            
            String nameOrIDOnPlayer = String.Empty;
            BasePlayer onPlayer = null;

            if (arg.Args.Length >= 2)
            {
                nameOrIDOnPlayer = arg.Args[1];
                (BasePlayer, String) findOnPlayer = FindPlayerNameOrID(player, nameOrIDOnPlayer);

                if (!String.IsNullOrWhiteSpace(nameOrIDOnPlayer))
                {
                    onPlayer = targetPlayer;
                    targetPlayer = findOnPlayer.Item1;
                }

                if (onPlayer == null)
                {
                    SendChat(findOnPlayer.Item2 ?? GetLang("CAN_TELEPORT_PLAYER_NOT_FOUND_TP_ONE_PLAYER", player.UserIDString), player, true);
                    return;
                }
		   		 		  						  	   		   		 		  			 		  	   		  				
                if (targetPlayer == null)
                {
                    SendChat(findPlayer.Item2 ?? GetLang("CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER_ONE", player.UserIDString, nameOrIDTarget), player, true);
                    return;
                }
            }
            else onPlayer = player;
            
            if (targetPlayer == null)
            {
                SendChat(findPlayer.Item2 ?? GetLang("CAN_TELEPORT_PLAYER_NOT_FOUND_TP_TARGET_PLAYER", player.UserIDString), player, true);
                return;
            }
        
            MovePlayer(onPlayer, targetPlayer.transform.position);
            SendChat(GetLang("CAN_TELEPORT_PLAYER_ACCES_TP", onPlayer.UserIDString, targetPlayer.displayName), onPlayer);
        }

        private const String permissionGMapTeleport = "iqteleportation.gmap";
        
        [ConsoleCommand("gmap")]
        private void GMapTeleportConsoleCmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            GMapTeleportTurn(player);
        }
        
                
                
        
        private String CanTeleportation(BasePlayer player, UserRepository.TeleportType type, Boolean isSkipCooldown = false)
        {
            if (Interface.Call("CanTeleport", player) is String hookResult)
                return String.IsNullOrWhiteSpace(hookResult) ? GetLang("CAN_TELEPORT_NULL_REASON", player.UserIDString) : hookResult;
            
            if (Interface.Call("canTeleport", player) is String hookResultTwo)
                return String.IsNullOrWhiteSpace(hookResultTwo) ? GetLang("CAN_TELEPORT_NULL_REASON", player.UserIDString) : hookResultTwo;
            
            String statusPlayer = CheckStatusPlayer(player);
            if (!String.IsNullOrWhiteSpace(statusPlayer))
                return statusPlayer;

            if (config.teleportationController.teleportSetting.noTeleportInRaidBlock && IsRaidBlocked(player))
                return GetLang("CAN_TELEPORT_RAID_BLOCK", player.UserIDString);

            BaseEntity playerParentEntity = player.GetParentEntity();

            if (playerParentEntity)
            {
                if (config.teleportationController.teleportSetting.noTeleportHotAirBalloon && playerParentEntity is HotAirBalloon)
                    return GetLang("CAN_TELEPORT_TITILE_OTHER", player.UserIDString,
                        GetLang("CAN_TELEPORT_IN_HOT_AIR_BALLOON", player.UserIDString));

                if (config.teleportationController.teleportSetting.noTeleportCargoShip && playerParentEntity is CargoShip)
                    return GetLang("CAN_TELEPORT_TITILE_OTHER", player.UserIDString,
                        GetLang("CAN_TELEPORT_IN_CARGO_SHIP", player.UserIDString));
            }

            if (player.IsBuildingBlocked())
                return GetLang("CAN_TELEPORT_TITILE_OTHER", player.UserIDString,
                    GetLang("CAN_TELEPORT_IN_BUILDING_BLOCKED", player.UserIDString));
		   		 		  						  	   		   		 		  			 		  	   		  				
            if (IsPlayerWithinBlockedMonument(player))
                return GetLang("CAN_TELEPORT_MONUMENT_BLOCKED", player.UserIDString);

            if (isSkipCooldown) return String.Empty;
            
            UserRepository userRepository = localUsersRepository[player];
            String getCooldown = userRepository.GetCooldownTitle(player, type);
            return !String.IsNullOrWhiteSpace(getCooldown) ? GetLang("CAN_TELEPORT_COOLDOWN", player.UserIDString, getCooldown) : String.Empty;
        }
        
        [ConsoleCommand("removehome")]
        private void RemoveHomeConsoleCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (arg.Args.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_REMOVEHOME", player.UserIDString), player, true);
                return;
            }

            String homeName = arg.Args[0];
            RemoveHome(player, homeName);
        }

        private String FormatTime(Double seconds, String userID = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            String result = String.Empty;
    
            String daysLabel = GetLang("TITLE_FORMAT_DAYS", userID);
            String hoursLabel = GetLang("TITLE_FORMAT_HOURSE", userID);
            String minutesLabel = GetLang("TITLE_FORMAT_MINUTES", userID);
            String secondsLabel = GetLang("TITLE_FORMAT_SECONDS", userID);

            if (time.Days > 0)
                result += $"{Format(time.Days, daysLabel, daysLabel, daysLabel)} ";
    
            if (time.Hours > 0)
                result += $"{Format(time.Hours, hoursLabel, hoursLabel, hoursLabel)} ";
    
            if (time.Minutes > 0)
                result += $"{Format(time.Minutes, minutesLabel, minutesLabel, minutesLabel)} ";
    
            if (time.Seconds > 0 || string.IsNullOrEmpty(result)) 
                result += $"{Format(time.Seconds, secondsLabel, secondsLabel, secondsLabel)}";
		   		 		  						  	   		   		 		  			 		  	   		  				
            return result.Trim(); 
        }

        private void TeleportationPlayer(BasePlayer player, BasePlayer targetPlayer, Vector3 teleportationPosition)
        {
            if (!player.IsValid())
                return;

            ClearQueue(player, targetPlayer);

            String canTeleport = CanTeleportation(player, UserRepository.TeleportType.Teleportation);
            if (!String.IsNullOrWhiteSpace(canTeleport))
            {
                SendChat(canTeleport, player, true);
                return;
            }
		   		 		  						  	   		   		 		  			 		  	   		  				
            String canTeleportTarget = CanTeleportation(targetPlayer, UserRepository.TeleportType.Teleportation, true);
            if (!String.IsNullOrWhiteSpace(canTeleportTarget))
            {
                SendChat(canTeleportTarget, targetPlayer, true);
                return;
            }

            if (config.teleportationController.teleportSetting.onlyTeleportationFriends)
            {
                if (!IsFriends(player, targetPlayer.userID))
                {
                    SendChat(GetLang("CAN_TELEPORT_ONLY_FRIEND", player.UserIDString), player, true);
                    SendChat(GetLang("CAN_TELEPORT_ONLY_FRIEND_TARGET", player.UserIDString), targetPlayer, true);
                    return;
                }
            }
            
            if (teleportationPosition == default)
                teleportationPosition = targetPlayer.transform.position;
		   		 		  						  	   		   		 		  			 		  	   		  				
            localUsersRepository[player].SetupCooldown(player, UserRepository.TeleportType.Teleportation);
            
            MovePlayer(player, teleportationPosition, targetPlayer.GetParentEntity());
        }
        
                
        private class InterfaceBuilder
        {
            
            public static InterfaceBuilder Instance;
            public const String IQ_TELEPORT_UI_HOME = "IQ_TELEPORT_UI_HOME";
            public const String IQ_TELEPORT_UI_TELEPORT = "IQ_TELEPORT_UI_TELEPORT";
            public Dictionary<String, String> Interfaces;

            
            public InterfaceBuilder()
            {
                Instance = this;
                Interfaces = new Dictionary<String, String>();
                
                if (config.teleportationController.teleportSetting.suggetionAcceptedUI)
                    Building_PanelButton(IQ_TELEPORT_UI_TELEPORT);
                
                if (config.homeController.suggetionSetHomeAfterInstallBed)
                    Building_PanelButton(IQ_TELEPORT_UI_HOME);
            }

            public static void AddInterface(String name, String json)
            {
                if (Instance.Interfaces.ContainsKey(name))
                {
                    _.PrintError($"Error! Tried to add existing cui elements! -> {name}");
                    return;
                }

                Instance.Interfaces.Add(name, json);
            }

            public static String GetInterface(String name)
            {
                String json = String.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (Int32 i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    BasePlayer player = BasePlayer.activePlayerList[i];
                    DestroyUI(player, IQ_TELEPORT_UI_HOME);
                    DestroyUI(player, IQ_TELEPORT_UI_TELEPORT);
                }
            }
    
            private void Building_PanelButton(String templateParent)
            {
                CuiElementContainer container = new CuiElementContainer();
                
                container.Add(new CuiElement
                {
                    FadeOut = uiAlertFade,
                    DestroyUi = templateParent,
                    Name = templateParent,
                    Parent = "Overlay",
                    Components = {
                        new CuiRawImageComponent { FadeIn = uiAlertFade, Color = "1 1 1 1", Png = _imageUI.GetImage("UI_IQTELEPORTATION_TEMPLATE") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-199.867 80.133", OffsetMax = "181.467 104.8" }
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = uiAlertFade / 2f,
                    DestroyUi = "Title_Panel",
                    Name = "Title_Panel",
                    Parent = templateParent,
                    Components = {
                        new CuiTextComponent { FadeIn = uiAlertFade, Text = "%TITILE_PANEL%", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.8941177 0.854902 0.8196079 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "23.502 1.1", OffsetMax = "-23.589 -1.135" }
                    }
                });

                container.Add(new CuiButton
                {
                    FadeOut = uiAlertFade,
                    Button = { FadeIn = uiAlertFade, Color = "0 0 0 0", Close = templateParent, Command = "%COMMAND_YES%" },
                    Text = { FadeIn = uiAlertFade, Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.266 -11.234", OffsetMax = "23.502 11.198" }
                },templateParent,"Button_Setup", "Button_Setup");

                container.Add(new CuiButton
                {
                    FadeOut = uiAlertFade,
                    Button = { FadeIn = uiAlertFade, Color = "0 0 0 0", Close = templateParent, Command = "%COMMAND_NO%"},
                    Text = { FadeIn = uiAlertFade, Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-23.589 -11.234", OffsetMax = "-1.028 11.198" }
                },templateParent,"Button_Setup_No", "Button_Setup_No");
                
                AddInterface(templateParent, container.ToJson());
            }
        }
		   		 		  						  	   		   		 		  			 		  	   		  				
        
                
                private static Configuration config = new Configuration();

        private const String effectSoundError = "assets/prefabs/weapons/toolgun/effects/repairerror.prefab";
        
        
        private void TeleportationHome(BasePlayer player, Vector3 position, BaseEntity pEntity)
        {
            if (!player.IsValid())
                return;

            String canTeleport = CanTeleportation(player, UserRepository.TeleportType.Home);
            if (!String.IsNullOrWhiteSpace(canTeleport))
            {
                SendChat(canTeleport, player, true);
                return;
            }
            
            localUsersRepository[player].SetupCooldown(player, UserRepository.TeleportType.Home);
            MovePlayer(player, position, pEntity, pEntity);
        }
        
        private readonly LayerMask homelayerMaskBuilding = LayerMask.GetMask("Construction"); 
        private const String effectSoundTeleport = "assets/prefabs/misc/halloween/lootbag/effects/loot_bag_upgrade.prefab";

        private String Format(Int32 units, String form1, String form2, String form3)
        {
            Int32 tmp = units % 10;

            if (units is >= 5 and <= 20 || tmp is >= 5 and <= 9 || tmp == 0)
                return $"{units}{form1}";

            return tmp is >= 2 and <= 4 ? $"{units}{form2}" : $"{units}{form3}";
        }
        
        
                
        private void ChatCommandTeleportationWarp(BasePlayer player, String cmd, String[] arg)
        {
            if (!config.warpsContoller.useWarps) return;
            if (!serverWarps.TryGetValue(cmd, out List<ServerWarp> warpPoints)) return;

            ServerWarp randomWarp = GetRandomWarp(warpPoints);
            if (randomWarp == null) return;
            Vector3 warpPoint = randomWarp.GetPositionWarp();
            if (warpPoint == default) return;
            RequestWarp(player, warpPoint);
        }
        
        private const Single checkIntervalPlayerTeleportation = 1f;
        public String GetLang(String LangKey, String userID = null, params Object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        private Dictionary<UInt64, TeleportationQueueInfo> playerTeleportationQueue = new();

                
        
        private void ClearQueue(BasePlayer player, BasePlayer targetPlayer)
        {
            if (player)
            {
                UserRepository pData = localUsersRepository[player];
                
                if(pData != null && pData.IsActiveTeleportation())
                    pData.DeActiveTeleportation();
            
                playerTeleportationQueue.Remove(player.userID);

                if (teleportationActions.TryGetValue(player, out Action action))
                {
                    player.CancelInvoke(action);
                    teleportationActions.Remove(player);
                }
            }
            
            if (targetPlayer)
            {
                UserRepository tData = localUsersRepository[targetPlayer];
                
                if(tData != null && tData.IsActiveTeleportation())
                    tData.DeActiveTeleportation();
                
                playerTeleportationQueue.Remove(targetPlayer.userID);
                
                if (teleportationActions.TryGetValue(targetPlayer, out Action actionTarget))
                {
                    targetPlayer.CancelInvoke(actionTarget);
                    teleportationActions.Remove(targetPlayer);
                }
            }
        }

        private enum TypeLog
        {
            RequestTeleportation,
            AcceptTeleportation,
            RequestHomeTeleportation,
        }
        private static InterfaceBuilder _interface;

        private void OnEntitySpawned(Tugboat tugboat)
        {
            if (tugboat == null) return;
            UInt64 netIDTugBoat = tugboat.net.ID.Value;
            
            tugboatsServer.TryAdd(netIDTugBoat, tugboat);
        }   


                
        private BuildingBlock GetBuildingBlock(in Vector3 position)
        {
            Vector3 rayOrigin = position  + Vector3.up; 
            Vector3 rayDirection = Vector3.down;
            
            if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, radiusCheckLayerHome, homelayerMaskBuilding)) return null;
            BuildingBlock buildingBlock = hit.GetEntity() as BuildingBlock;
        
            // if (buildingBlock == null)
            //     return null;

            return !IsValidPosOtherEntity(position, buildingBlock) ? null : buildingBlock;
        }
        private Dictionary<UInt64, Tugboat> tugboatsServer = new();
        private const String permissionTP = "iqteleportation.tp";

        [ConsoleCommand("home")]
        private void HomeConsoleCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_HOME_TELEPORTATION", player.UserIDString), player, true);
                return;
            }

            RequestHome(player, arg.Args);
        }

        
                
        private IEnumerator ProcessTeleportation(BasePlayer player, Func<Boolean> additionalChecks, Action teleportationAction, Int32 timeTeleportation, BasePlayer targetPlayer = null)
        {
            Single elapsedTime = 0f;
            Single nextCheckTime = 0f;

            while (elapsedTime < timeTeleportation)
            {
                Single interval = !config.generalController.useSoundEffects ? 1f : (timeTeleportation - elapsedTime) switch
                {
                    >= 15 => 1.0f,
                    > 10 => 0.7f,
                    > 7 => 0.6f,
                    > 5 => 0.5f,
                    > 3 => 0.35f,
                    _ => 0.2f
                };
		   		 		  						  	   		   		 		  			 		  	   		  				
                if (elapsedTime >= nextCheckTime)
                {
                    if (!player)
                    {
                        ClearQueue(player, targetPlayer);
                        if (targetPlayer)
                            SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", targetPlayer.UserIDString, $"{GetLang("ACCESS_CANCELL_TELEPORTATION_DEAD_REQUESTER_DISCONNECTED", targetPlayer.UserIDString)}"), targetPlayer);
                        yield break;
                    }

                    if (player.IsDead())
                    {
                        ClearQueue(player, targetPlayer);
                        SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", player.UserIDString, $"{GetLang("ACCESS_CANCELL_TELEPORTATION_DEAD", player.UserIDString)}"), player);
                        if (targetPlayer)
                            SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION", targetPlayer.UserIDString, $"{GetLang("ACCESS_CANCELL_TELEPORTATION_DEAD_REQUESTER_DEAD", targetPlayer.UserIDString)}"), targetPlayer);
                        yield break;
                    }
		   		 		  						  	   		   		 		  			 		  	   		  				
                    if (additionalChecks())
                        yield break;

                    nextCheckTime = elapsedTime + checkIntervalPlayerTeleportation;
                }

                RunEffect(player, effectSoundTimer);
                yield return new WaitForSeconds(interval);
                elapsedTime += interval;
            }

            teleportationAction();
        }

                private void SetHome(BasePlayer player, String homeName, SleepingBag bag = null)
        {
            homeName = homeName.ToLower();
            
            CheckAllHomes(player, homeName);

            Vector3 positionHome = player.transform.position;
            
            if (bag != null)
            {
                positionHome = bag.transform.position + new Vector3(0f, 0.5f, 0f);
                bag.niceName = homeName;
                bag.name = homeName;
                bag.SendNetworkUpdate();
            }
            
            BuildingBlock buildingBlock = GetBuildingBlock(positionHome);
            Tugboat tugboatEntity = player.GetParentEntity() as Tugboat;
            UInt64 netIdEntity = 0;
            
            if (tugboatEntity != null)
            {
                netIdEntity = tugboatEntity.net.ID.Value;
                positionHome = tugboatEntity.transform.InverseTransformPoint(positionHome);
            }
            
            String canSethome = CanSetHome(player, buildingBlock, homeName, positionHome, tugboatEntity);
            
            if (!String.IsNullOrWhiteSpace(canSethome))
            {
                SendChat(canSethome, player, true);
                return;
            }

            playerHomes[player.userID].Add(homeName, new PlayerHome()
            {
                netIdEntity = netIdEntity,
                positionHome = positionHome,
            });

            if (config.homeController.addedPingMarker)
                CreatePingPlayer(player, positionHome);

            if (config.homeController.addedMapMarkerHome)
                CreateMapMarkerPlayer(player, positionHome, homeName);
            
            SendChat(GetLang("ACCESS_SETUP_SETHOME", player.UserIDString, homeName), player);
            
            Interface.Call("OnHomeAdded", player, positionHome, homeName);
        }

        
        private void SendChat(String message, BasePlayer player, Boolean isError = false)
        {
            if (config.generalController.useOnlyGameTip)
                player.SendConsoleCommand("gametip.showtoast", new Object[]{ "1", message, "15" });
            else
            {
                if (IQChat)
                    IQChat?.Call("API_ALERT_PLAYER", player, message, config.generalController.iqchatSetting.customPrefix, config.generalController.iqchatSetting.customAvatar);
                else player.SendConsoleCommand("chat.add", Chat.ChatChannel.Global, 0, message);
            }

            RunEffect(player, isError ? effectSoundError : effectSoundAccess);
        }

        
        
        private void RequestHome(BasePlayer player, String[] arg)
        {
            String homeName = arg[0].ToLower();
            String friendNameOrID = arg.Length >= 2 ? arg[1] : String.Empty;
            
            String requestError = CanRequestTeleportHome(player, homeName, friendNameOrID, out UInt64 userIDFriend, out String friendName);
            if (!String.IsNullOrWhiteSpace(requestError))
            {
                SendChat(requestError, player, true);
                return;
            }
            
            Boolean isOtherID = userIDFriend != default && userIDFriend != player.userID;
            UInt64 userID = isOtherID ? userIDFriend : player.userID;

            PlayerHome pHome = playerHomes[userID][homeName];
            Int32 coutdownTeleportation = config.homeController.homeCountdown.GetCount(player);
            String formatCountdown = _.FormatTime(coutdownTeleportation, player.UserIDString);

            if (localUsersRepository[player].IsActiveTeleportation())
            {
                localUsersRepository[player].DeActiveTeleportation();
                SendChat(GetLang("ACCESS_CANCELL_TELEPORTATION_PLAYER", player.UserIDString), player); 
            }

            Int32 countdownTeleportation = config.homeController.homeCountdown.GetCount(player);
            Interface.Call("OnHomeAccepted", player, homeName, countdownTeleportation); 

            Coroutine routineTeleportation = ServerMgr.Instance.StartCoroutine(ProcessTHome(player, pHome, countdownTeleportation)); 
            localUsersRepository[player].ActiveCoroutineController(routineTeleportation);

            String resultMessage = !String.IsNullOrWhiteSpace(friendName) && isOtherID 
                ? GetLang("ACCESS_TELEPORTATION_HOME_FRIEND_NICK", player.UserIDString, friendName, homeName, formatCountdown)
                : String.IsNullOrWhiteSpace(friendName) && isOtherID
                    ? GetLang("ACCESS_TELEPORTATION_HOME_FRIEND", player.UserIDString, homeName, formatCountdown)
                    : GetLang("ACCESS_TELEPORTATION_HOME", player.UserIDString, homeName, formatCountdown);

            if (pHome.netIdEntity == 0)
                LogAction(player, TypeLog.RequestHomeTeleportation, default, pHome.positionHome, homeName);
            
            SendChat(resultMessage, player);
        }
        
        private String GetNextHomeName(Dictionary<String, PlayerHome> pData)
        {
            for (Int32 i = 1; i <= 99; i++)
            {
                String homeName = i.ToString();
        
                if (!pData.ContainsKey(homeName))
                    return homeName;
            }
    
            return null; 
        }

        
        
        private List<String> GetOrSetCacheUI(BasePlayer player, String additionalTitile, String interfaceJson = null)
        {
            String langKeyPlayer = lang.GetLanguage(player.UserIDString);
            String keyCache = $"{additionalTitile}_{langKeyPlayer}";

            if (cachedUI.TryGetValue(keyCache, out List<String> ui))
                return ui;

            if (interfaceJson == null) return null;

            List<String> newUI = new List<String> { interfaceJson };
            cachedUI[keyCache] = newUI;
            return newUI;
        }
        
        private ServerWarp GetRandomWarp(List<ServerWarp> warpList)
        {
            List<ServerWarp> warps = new List<ServerWarp>();
		   		 		  						  	   		   		 		  			 		  	   		  				
            foreach (ServerWarp serverWarp in warpList)
            {
                if (serverWarp.hideWarp) continue;
                warps.Add(serverWarp);
            }

            try
            {
                return warps.GetRandom();
            }
            finally
            {
                warps.Clear();
                warps = null;
            }
        }
        
        private static IQTeleportation _;
		   		 		  						  	   		   		 		  			 		  	   		  				
        private void RequestTeleportation(BasePlayer player, String[] arg)
        {
            String friendNameOrID = arg.Length >= 1 ? String.Join(" ", arg) : String.Empty;
            (BasePlayer, String) findInfo = new();
            
            BasePlayer targetPlayer;
            if (String.IsNullOrWhiteSpace(friendNameOrID))
            {
                casheTeleportationLast.TryGetValue(player, out BasePlayer oldPlayerTeleport);
                targetPlayer = oldPlayerTeleport; 
            }
            else
            {
                findInfo = FindPlayerNameOrID(player, friendNameOrID);
                targetPlayer = findInfo.Item1;
            }
            
            String requestError = CanRequestTeleportation(player, targetPlayer, findInfo.Item2);
            if (!String.IsNullOrWhiteSpace(requestError))
            {
                SendChat(requestError, player, true);
                return;
            }
            
            playerTeleportationQueue.TryAdd(targetPlayer.userID, new TeleportationQueueInfo
            {
                player = player,
                isActive = true,
                isAccepted = false
            });
            
            playerTeleportationQueue.TryAdd(player.userID, new TeleportationQueueInfo
            {
                player = targetPlayer,
                isActive = false,
                isAccepted = false
            });
            casheTeleportationLast[player] = targetPlayer;

            if (playerSettings[targetPlayer.userID].autoAcceptTeleportationFriends &&
                IsFriends(player, targetPlayer.userID))
            {
                if (!AcceptRequestTeleportation(targetPlayer, true))
                {
                    SendChat(GetLang("CAN_ACCEPT_TELEPORTATION_NOT_AUTO_ACCEPT", player.UserIDString), player);
                    ClearQueue(player, targetPlayer);
                    if (localUsersRepository[player].IsActiveTeleportation())
                        teleportationActions.Remove(player);
		   		 		  						  	   		   		 		  			 		  	   		  				
                    return;
                }
                SendChat(GetLang("CAN_ACCEPT_TELEPORTATION_YES_AUTO_ACCEPT", player.UserIDString), player);
            }
            else
            {
                if (config.teleportationController.teleportSetting.suggetionAcceptedUI)
                    DrawUI_TeleportPlayer(player, targetPlayer);

                SendChat(GetLang("CAN_REQUEST_TELEPORTATION_SEND", player.UserIDString, targetPlayer.displayName), player);
                SendChat(GetLang("CAN_REQUEST_TELEPORTATION_RECEIVE", targetPlayer.UserIDString, player.displayName), targetPlayer);
            }

            LogAction(player, TypeLog.RequestTeleportation, targetPlayer, default, default);

            Action teleportWaitAccept = () =>
            {
                if (localUsersRepository[player].IsActiveTeleportation())
                {
                    teleportationActions.Remove(player);
                    return;
                }
                
                ClearQueue(player, targetPlayer);
                SendChat(GetLang("CAN_TELEPORT_REQUEST_EXPIRED", player.UserIDString), player);
                SendChat(GetLang("CAN_TELEPORT_REQUEST_EXPIRED", targetPlayer.UserIDString), targetPlayer); 
            };

            teleportationActions[player] = teleportWaitAccept;
            
            player.Invoke(teleportWaitAccept, 15f);
        }
        
        private const String homeLog = LanguageEn ? "" : "отправился на точку дома";
        
        private void OnEntitySpawneds(AutoTurret turret)
        {
            if (turret == null) return;
            if(ItemIdCorrecteds.Contains(turret.net.ID.Value))
                turretsEx.TryAdd(turret.net.ID.Value, turret);
        }

        
        
        
        [ChatCommand("atp")]
        private void AutoAcceptTeleportChatCmd(BasePlayer player) => AutoTeleportTurn(player);

            }
}
