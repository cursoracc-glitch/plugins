using Rust;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using ConVar;
using Oxide.Core.Libraries.Covalence;
using System;
using Object = System.Object;
using Oxide.Core.Libraries;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Core.Database;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("IQBanSystem", "rustmods.ru", "1.12.19")]
    [Description("IQBanSystem")]
    public class IQBanSystem : RustPlugin
    { 
		   		 		  						  	   		  		 			  	   		  		  		   		 

                
        
        private String SQL_Query_CreatedDatabase()
        {
            return $"CREATE TABLE IF NOT EXISTS `{config.mysqlConnected.dbTableName}`(" +
                               "`id` INT(11) NOT NULL AUTO_INCREMENT," +
                               "`steamid` VARCHAR(17) NOT NULL," +
                               "`ipAdress` VARCHAR(30) NOT NULL," +
                               "`permanent` VARCHAR(1) NOT NULL," +
                               "`timeUnbanned` VARCHAR(70) NOT NULL," +
                               "`reason` VARCHAR(70) NOT NULL," +
                               "`serverName` VARCHAR(70) NOT NULL," +
                               "`serverAdress` VARCHAR(70) NOT NULL," +
                               "`owner` VARCHAR(70) NOT NULL," +
                               "`nameHistory` TEXT NOT NULL," +
                               "`ipHistory` TEXT NOT NULL," +
                               "`steamIdHistory` TEXT NOT NULL," +
                               " PRIMARY KEY(`id`))" + 
                               " CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci;";
        }
        
        object OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, UInt64 target)
        {
            LeaveTeamPlayer(target);
            return null;
        }
        [ChatCommand("ban")]
        private void ChatBanCommand(BasePlayer ownerCommand, String cmd, String[] arg) => BanCommand(ownerCommand, arg);
        /// <summary>
        /// - Корректировка работы с хуком : OnUserConnected
        /// 
        /// </summary>
        
        
        private static IQBanSystem _;

        private class TeamLocalMemory
        {
            public Double firstJoin;
            public Double lastRemoved;
        }
        
        void OnTeamCreated(BasePlayer player, RelationshipManager.PlayerTeam team) => TeamAdded(team);

        public class Footer
        {
            public String text { get; set; }
            public String icon_url { get; set; }
            public String proxy_icon_url { get; set; }
            public Footer(String text, String icon_url, String proxy_icon_url)
            {
                this.text = text;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        
        private class ProcessCheckVPN
        {
            public Int32 detectedVpn;
            public Boolean isCompleteProxyCheck;
            public Boolean isCompleteVpnApi;
            public Boolean isCompleteIPHub;
        }
        
                
        
        
        private List<String> ipListGods = new List<String>(); 
        private Timer timerCheckVpn;

        
                
        private Dictionary<Vector3, String> GetAuthCupboardPlayer(UInt64 playerID)
        {
            Dictionary<Vector3, String> resultData = new Dictionary<Vector3, String>();
            List<BaseNetworkable> listEntity = Facepunch.Pool.Get<List<BaseNetworkable>>();
            listEntity.AddRange(BaseNetworkable.serverEntities.entityList.Get().Values.Where(x => x.ShortPrefabName.Contains("cupboard") && x is BuildingPrivlidge));
                
            foreach (BaseNetworkable cupboards in listEntity)
            {
                BuildingPrivlidge cupboard = cupboards as BuildingPrivlidge;
                if (cupboard == null) continue;
                if (cupboard.IsAuthed(playerID) || cupboard.OwnerID == playerID)
                    resultData.Add(cupboard.transform.position, MapHelper.PositionToString(cupboard.transform.position));
            }
            
            Facepunch.Pool.FreeUnmanaged(ref listEntity);
            return resultData;
        }
        
        private void OnServerInitialized()
        {
            permission.RegisterPermission(permissionsBan,this);
            permission.RegisterPermission(permissionsUnBan,this);
            permission.RegisterPermission(permissionsKick,this);
            permission.RegisterPermission(permissionsIgnoreVPN,this);
            permission.RegisterPermission(permissionIgnoreBan,this);

            Configuration.BannedSetting.DestroyedAfterBannedSetting configDestroyed = config.bannedSetting.destroyedAfterBannedSetting;
            Boolean isMarkerUse = configDestroyed.mapMarkerDestroyedObjectSetting.useMapMarkerDestroyedObjectPlayer && config.generalSetting.additionalSetting.useAlertAllPlayers && configDestroyed.useAlertGridHomes &&
                                  configDestroyed.useDestroyObjects;

            if (isMarkerUse)
                timerUpdateMarkers = timer.Every(60f, MarkerUpdate);
            
            useIpHub = !String.IsNullOrWhiteSpace(config.vpnSetting.iPHubToken);
            useVpnApi = !String.IsNullOrWhiteSpace(config.vpnSetting.vpnApiIoToken);
            useProxyCheck = !String.IsNullOrWhiteSpace(config.vpnSetting.proxyCheckIo);
            maxDetectedVpn = (config.vpnSetting.countDetectedToKickInVPN <= 0 ? 1 : config.vpnSetting.countDetectedToKickInVPN);
            
            if(RustApp)
                if (RustApp.Version < new VersionNumber(1, 9, 2))
                {
                    PrintWarning(LanguageEn ? "" : "У вас установлен RustApp версии ниже 1.9.2 - плагины не смогут взаимодействовать! Обновите плагин RustApp..");
                }
        }
        private const String VendingPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";

        private Boolean TryParseTimeSpan(String source, out Double timeSpan)
        {
            Int32 seconds = 0, minutes = 0, hours = 0, days = 0;

            MatchCollection matches = Regex.Matches(source, @"(\d+)([smhd])", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                Int32 value = int.Parse(match.Groups[1].Value);
                Char unit = match.Groups[2].Value.ToLower()[0];
		   		 		  						  	   		  		 			  	   		  		  		   		 
                switch (unit)
                {
                    case 's':
                        seconds += value;
                        break;
                    case 'm':
                        minutes += value;
                        break;
                    case 'h':
                        hours += value;
                        break;
                    case 'd':
                        days += value;
                        break;
                }
            }

            source = Regex.Replace(source, @"(\d+)[smhd]", String.Empty, RegexOptions.IgnoreCase);

            if (!String.IsNullOrEmpty(source) || (seconds == 0 && minutes == 0 && hours == 0 && days == 0))
            {
                timeSpan = 0;
                return false;
            }

            timeSpan = new TimeSpan(days, hours, minutes, seconds).TotalSeconds;
            return true;
        }
        private const String permissionsUnBan = "iqbansystem.unban";
        private Dictionary<UInt64, Dictionary<UInt64, TeamLocalMemory>> teamLocalMemory = new();
        
        
        
                
        private void PullOutTeamsInfo(BasePlayer player)
        {
            RelationshipManager.PlayerTeam team = player.Team;
            if (team == null) return;
            //PrintToChat("Pull Out Teams");
            TeamAdded(team);
        }
        
        private Boolean IsIpAdress(String mbIP) => Regex.IsMatch(mbIP, patternIP);
        
        
        private void KickCommand(BasePlayer ownerCommand, String[] arg)
        {
            if (arg == null)
            {
                SendInfo(ownerCommand, LanguageEn ? "Use syntax: kick SteamID/IP/Name* Reason\nItems marked with * are mandatory" : "Используйте синтаксис : kick SteamID/IP/Name* Причина\nПункты помеченные `*` - это обязательные пункты");
                return;
            }
            
            if (ownerCommand != null)
                if (!permission.UserHasPermission(ownerCommand.UserIDString, permissionsKick))
                    return;
		   		 		  						  	   		  		 			  	   		  		  		   		 
            if (arg.Length < 1)
            {
                SendInfo(ownerCommand, LanguageEn ? "Use syntax: kick SteamID/IP/Name* Reason\nItems marked with * are mandatory" : "Используйте синтаксис : kick SteamID/IP/Name* Причина\nПункты помеченные `*` - это обязательные пункты");
                return;
            }

            String userParams = arg[0];
            IPlayer iPlayer = GetIPlayer(userParams);
            if (iPlayer == null)
            {
                SendInfo(ownerCommand, LanguageEn ? "Couldn't find a player" : "Не удалось найти игрока");
                return;
            }
            
            String reasonArg = String.Empty;
            if (arg.Length >= 2)
            {
                if (!String.IsNullOrWhiteSpace(arg[1]))
                    reasonArg = arg[1];
            }
            
            KickUser(iPlayer, reasonArg, ownerCommand);
        }
        private Connection sqlConnection = null;

        /// <summary>
        /// Returned unbanTime. -1 = permanent
        /// </summary>
        /// <param name="idOrIP"></param>
        /// <returns></returns>
        private Double GetUnbanTime(String idOrIP)
        {
            TypeAction typeAction = TypeAction.BannedSteamID;
            if (IsIpAdress(idOrIP))
                typeAction = TypeAction.BannedIP;
            
            PlayerInfo pInfo = PlayerInfo.Get(idOrIP, typeAction);
            if (pInfo == null) return 0;
            if (pInfo.permanent) return -1;
            
            return pInfo.GetUnbanTime;
        }
        
        
        
                
        private void BanUserOrIp(String idPlayer, String displayName, String ipAdress, String reason, Double banTime, TypeAction type, 
            BasePlayer ownerAction = null, Boolean skipTeam = false)  
        {
            if (permission.UserHasPermission(idPlayer, permissionIgnoreBan)) return;

            Boolean isBannedIP = type == TypeAction.BannedIP;
            String valueImported = isBannedIP ? ipAdress : idPlayer;

            PlayerInfo PlayerBanned = PlayerInfo.Get(valueImported, type);
            if (PlayerBanned != null && PlayerBanned.IsBanned()) return;

                        Configuration.BannedSetting.DestroyedAfterBannedSetting configDestroyed = config.bannedSetting.destroyedAfterBannedSetting;

            String ownerBanned = ownerAction == null ? "Console" : $"{ownerAction.displayName}({ownerAction.userID})";
            Boolean isPermanent = banTime <= 0;

            String banReason = String.IsNullOrWhiteSpace(reason) ? "Banned" : reason;
            String kickReason = isPermanent ? GetLang("BANNED_DEFAULT_REASON_PERMANENT", idPlayer) :
                GetLang("BANNED_DEFAULT_REASON", idPlayer, FormatTime(banTime, idPlayer));

            if (!String.IsNullOrWhiteSpace(reason))
            {
                kickReason = isPermanent ? GetLang("BANNED_ALERT_PERMANENT", idPlayer, reason) :
                    GetLang("BANNED_ALERT_TIME", idPlayer, reason, FormatTime(banTime, idPlayer));
            }
            
                        
                        
            String formatTime = DateTime.Now.ToString("HH:mm dd:MM:yyyy");
            List<PlayerInfo.History> nameHistory = String.IsNullOrWhiteSpace(displayName) ? new List<PlayerInfo.History>() : new List<PlayerInfo.History> { new() { value = displayName, time = formatTime } };
            List<PlayerInfo.History> ipHistory = String.IsNullOrWhiteSpace(ipAdress) ? new List<PlayerInfo.History>() : new List<PlayerInfo.History> { new() { value = ipAdress, time = formatTime } };
            List<PlayerInfo.History> steamIdHistory = String.IsNullOrWhiteSpace(idPlayer) ? new List<PlayerInfo.History>() : new List<PlayerInfo.History> { new() { value = idPlayer, time = formatTime } };

            PlayerInfo.Import(valueImported, new PlayerInfo
            {
                permanent = isPermanent,
                timeUnbanned = banTime + CurrentTime,
                reason = banReason,
                nameHistory = nameHistory,
                ipHistory = ipHistory,
                steamIdHistory = steamIdHistory,
                serverName = config.generalSetting.serverName, 
                serverAdress = config.generalSetting.serverAdress,
                owner = ownerBanned,
            }, type);

            IPlayer bannedPlayer = GetIPlayer(valueImported);
            UInt64 userID = 0;
            if(bannedPlayer != null)
                userID = UInt64.Parse(bannedPlayer.Id);
            else if(valueImported.IsSteamId())
                userID = UInt64.Parse(valueImported);
            Boolean isValidID = userID.IsSteamId();
            
            if (IsOpenMySQL())
                InserOrUpdateDatabase(valueImported, type, bannedPlayer);

            if (isBannedIP) 
                Interface.CallHook("OnBannedPlayerIP", ipAdress, reason, banTime, ownerAction);
            else Interface.CallHook("OnBannedPlayerID", userID, reason, banTime, ownerAction);

            
            List<String> posInGridDestroyed = Facepunch.Pool.Get<List<String>>();
            List<IPlayer> teamsBanneds = Facepunch.Pool.Get<List<IPlayer>>();
            
            if(bannedPlayer != null)
                if (config.bannedSetting.teamBannedSetting.useBlockTeam && !skipTeam && isValidID)
                    teamsBanneds = BannedTeams(userID, ownerAction);

            String playerBannedName = type == TypeAction.BannedIP ? ipAdress : !String.IsNullOrWhiteSpace(displayName) ? displayName : "Unknown";
            String playerBannedNameAlert = type == TypeAction.BannedIP ? ipAdress : !String.IsNullOrWhiteSpace(displayName) ? $"{displayName}({(String.IsNullOrWhiteSpace(idPlayer) ? "Unknown" : idPlayer)})" : "Unknown";

            
            if (config.bannedSetting.useKilledPlayer && bannedPlayer != null && isValidID)
            {
                BasePlayer bPlayer = BasePlayer.FindAwakeOrSleepingByID(userID);
                if (bPlayer != null) 
                    bPlayer.Hurt(1000f, DamageType.Suicide);
            }
                        
                        
            if (configDestroyed.useDestroyObjects)
            {
                if (bannedPlayer != null)
                    posInGridDestroyed = DestroyObjectBanned(bannedPlayer, playerBannedName); 
            }

            
                        
            if (config.generalSetting.additionalSetting.useAlertAllPlayers)
            {
                foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
                {
                    String banTimeFormat = isPermanent
                        ? GetLang("ALERT_ALL_TITILE_PERMANENT", basePlayer.UserIDString)
                        : FormatTime(banTime, basePlayer.UserIDString);

                    String reasonAlert = GetLang(banReason, basePlayer.UserIDString);
                    String ownerBannedAlert = ownerAction == null ? GetLang("ALERT_ALL_TITILE_ADMIN_BANNED", basePlayer.UserIDString) : ownerAction.displayName;
                    String playerNameAlert = GetLang(playerBannedName, basePlayer.UserIDString);
                    
                    String messagePlayers = GetLang("ALERT_ALL_PLAYER_BANNED", basePlayer.UserIDString, playerNameAlert, reasonAlert, banTimeFormat, ownerBannedAlert);

                    if (config.bannedSetting.teamBannedSetting.useBlockTeam && config.generalSetting.additionalSetting.useAlertBlockTeam && teamsBanneds != null && teamsBanneds.Count != 0)
                        messagePlayers += GetLang("ALERT_ALL_BANNED_TEAMS", basePlayer.UserIDString, String.Join(", ", teamsBanneds.Select(player => player.Name)));

                    if (configDestroyed.useDestroyObjects && configDestroyed.useAlertGridHomes && posInGridDestroyed != null && posInGridDestroyed.Count != 0)
                    {
                        String alertMessage = configDestroyed.typeDestroyed switch
                        {
                            TypeDestroy.AllObjectsAndDropInStorage => "ALERT_ALL_ADDITIONAL_DESTROY",
                            TypeDestroy.OnlyLocks => "ALERT_ALL_ADDITIONAL_DESTROY_ONLY_CODE_LOCK",
                            TypeDestroy.AllObjects => "ALERT_ALL_ADDITIONAL_DESTROY_CLEAR_STORAGE",
                            TypeDestroy.OnlyStorageAndDropInStorage => "ALERT_ALL_ADDITIONAL_DESTROY_ONLY_STORAGE",
                            _ => "ALERT_ALL_ADDITIONAL_DESTROY_ONLY_STORAGE_CLEAR"
                        };
                        
                        messagePlayers += GetLang(alertMessage, basePlayer.UserIDString,
                            String.Join(", ", posInGridDestroyed));
                        
                        if (configDestroyed.mapMarkerDestroyedObjectSetting.useMapMarkerDestroyedObjectPlayer)
                            if (TryParseTimeSpan(configDestroyed.mapMarkerDestroyedObjectSetting.markerLifeTime, out Double markerLife)) 
                                messagePlayers += GetLang("ALERT_ALL_USE_MARKER", basePlayer.UserIDString, FormatTime(markerLife, idPlayer));
                    }
                    
                    SendChat(messagePlayers, basePlayer);

                    if (skipTeam) continue;
                    String soundPath = config.generalSetting.additionalSetting.effectAlertAllPlayers;
                    if (soundPath != null && !String.IsNullOrWhiteSpace(soundPath))
                        RunEffect(basePlayer, soundPath);
                }
            }
            
                        
            if (bannedPlayer is { IsConnected: true }) 
                bannedPlayer.Kick(banReason);
            
            if(isValidID && isPermanent)
                SetTirifyBan(userID.ToString(), reason);
            
            String messageAlert = LanguageEn
                ? $"Player {playerBannedNameAlert} has been banned.\nBan Time: {(isPermanent ? "permanent" : FormatTime(banTime, null))}.\nReason: {banReason}.\nIssued by: {ownerBanned}"
                : $"Игрок {playerBannedNameAlert} заблокирован.\nВремя блокировки : {(isPermanent ? "навсегда" : FormatTime(banTime, null))}.\nПричина : {banReason}.\nВыдал блокировку : {ownerBanned}";
          
            Puts(messageAlert);

            String webhookBanned = config.generalSetting.discordSetting.alertBanned.webHookBanned;
            if (!String.IsNullOrWhiteSpace(webhookBanned))
            {
                List<Fields> fieldsBanned = new List<Fields>
                {
                    new Fields(type == TypeAction.BannedIP ? "IP" : LanguageEn ? "Player" : "Игрок", playerBannedNameAlert, true),
                    new Fields(LanguageEn ? "Issued by" : "Выдал блокировку", ownerBanned, true),
                    new Fields("", "", false),
                    new Fields(LanguageEn ? "Ban Time" : "Время блокировки", (isPermanent ? (LanguageEn ? "permanent" : "навсегда") : FormatTime(banTime, null)), true),
                    new Fields(LanguageEn ? "Reason" : "Причина", banReason, true),
                    new Fields("", "", false),
                };

                if (config.bannedSetting.teamBannedSetting.useBlockTeam && config.generalSetting.additionalSetting.useAlertBlockTeam && teamsBanneds != null && teamsBanneds.Count != 0)
                {
                    fieldsBanned.Add(new Fields(LanguageEn ? "Banneds teams" : "Заблокированные тиммейты", String.Join(", ", teamsBanneds.Select(player => player.Name)), false));
                    fieldsBanned.Add(new Fields("", "", false));
                }    
                
                if(posInGridDestroyed != null && posInGridDestroyed.Count != 0)
                {
                    fieldsBanned.Add(new Fields(LanguageEn ? "Destroyed buildings of the player in squares" : "Разрушенные строения игрока в квадратах", String.Join(", ", posInGridDestroyed), false));
                    fieldsBanned.Add(new Fields("", "", false));
                }

                if (config.generalSetting.discordSetting.alertBanned.cupboardAuthInfo)
                {
                    if (isValidID)
                    {
                        Dictionary<Vector3, String> infoCupboards = GetAuthCupboardPlayer(userID);
                        fieldsBanned.Add(new Fields(LanguageEn ? "Houses where the player is logged in" : "Дома в которых авторизован игрок", infoCupboards.Count == 0 ? (LanguageEn ? "Empty" : "Пусто") : String.Join("\n", infoCupboards.Select(kv => $"{kv.Value} - {kv.Key}")), false));
                        fieldsBanned.Add(new Fields("", "", false));
                    }
                }
                
                if (config.generalSetting.discordSetting.alertBanned.teamsAlert.saveInfoTeams)
                {
                    if (isValidID)
                    {
                        String timePlayeingInConfig = config.generalSetting.discordSetting.alertBanned.teamsAlert.formatTimePlaying;
                        if (TryParseTimeSpan(timePlayeingInConfig, out Double timePlaying))
                        {
                            Dictionary<UInt64, Double> teamMemory = GetTeamsBannedPlayer(userID);
                            
                                String resultTeams = teamMemory == null || teamMemory.Count == 0
                                    ? (LanguageEn ? "Empty" : "Пусто")
                                    : String.Join("\n",
                                        teamMemory.Where(memory => memory.Value > timePlaying && memory.Key != userID)
                                            .Select(memory =>
                                            {
                                                IPlayer iPlayer = GetIPlayer($"{memory.Key}");
                                                return iPlayer != null ? $"{iPlayer.Name} - {memory.Key}" : $"Unknown - {memory.Key}";
                                            }));
                                
                                resultTeams = String.IsNullOrWhiteSpace(resultTeams)
                                    ? (LanguageEn ? "Empty" : "Пусто")
                                    : resultTeams;

                                fieldsBanned.Add(new Fields(LanguageEn ? $"Players who have been blocked for more than {GetFormatTimeDiscord(timePlayeingInConfig)}" : $"Игроки с которым играл заблокированный более {GetFormatTimeDiscord(timePlayeingInConfig)}", resultTeams, false));
                                fieldsBanned.Add(new Fields("", "", false));
                        }
                    }
                }

                fieldsBanned.Add(new Fields(LanguageEn ? "History nick" : "История ников", nameHistory == null || nameHistory.Count == 0? (LanguageEn ? "Empty" : "Пусто") : String.Join("\n", nameHistory.Select(ph => $"{ph.time} - {ph.value}")), false));
                fieldsBanned.Add(new Fields(LanguageEn ? "History Steam64ID" : "История Steam64ID", steamIdHistory == null || steamIdHistory.Count == 0 ? (LanguageEn ? "Empty" : "Пусто") : String.Join("\n", steamIdHistory.Select(ph => $"{ph.time} - {ph.value}")), false));
                fieldsBanned.Add(new Fields(LanguageEn ? "History IPs" : "История IP", ipHistory == null || ipHistory.Count == 0 ? (LanguageEn ? "Empty" : "Пусто") : String.Join("\n", ipHistory.Select(ph => $"{ph.time} - {ph.value}")), false));
                fieldsBanned.Add(new Fields("", "", false));
                
                SendDiscord(fieldsBanned, TypeAlertDiscord.Banned, webhookBanned);
            }
            
            Facepunch.Pool.FreeUnmanaged(ref posInGridDestroyed);
            Facepunch.Pool.FreeUnmanaged(ref teamsBanneds);

            if (IQTeleportation && isValidID)
                IQTeleportation.CallHook("ClearHomesPlayer", userID, "IQBanSystem");
        }
        
        private const String permissionsBan = "iqbansystem.ban";
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQBanSystem/ListGodIps", ipListGods);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQBanSystem/TeamsMemory", teamLocalMemory);
        }
        private static Double CurrentTime => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        
        private IPlayer GetIPlayer(String userParams)
        {
            if (UInt64.TryParse(userParams, out UInt64 userID))
            {
                BasePlayer basePlayerByID = BasePlayer.FindByID(userID);
                if (basePlayerByID)
                    return basePlayerByID.IPlayer;

                BasePlayer sleepingPlayer = BasePlayer.sleepingPlayerList.FirstOrDefault(p => p.userID == userID);
                if (sleepingPlayer)
                    return sleepingPlayer.IPlayer;
		   		 		  						  	   		  		 			  	   		  		  		   		 
                IPlayer iPlayer = covalence.Players.FindPlayerById(userParams);
                if (iPlayer != null)
                    return iPlayer;
            }

            if (IsIpAdress(userParams))
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (player.net?.connection?.ipaddress != null && player.net.connection.ipaddress.StartsWith(userParams))
                        return player.IPlayer;
                }

                foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
                {
                    if (player.net?.connection?.ipaddress != null && player.net.connection.ipaddress.StartsWith(userParams))
                        return player.IPlayer;
                }
            }
		   		 		  						  	   		  		 			  	   		  		  		   		 
            BasePlayer activePlayer = BasePlayer.activePlayerList.FirstOrDefault(p => String.Equals(p.displayName, userParams, StringComparison.OrdinalIgnoreCase));
            if (activePlayer != null)
                return activePlayer.IPlayer;

            BasePlayer sleepingPlayerByName = BasePlayer.sleepingPlayerList.FirstOrDefault(p => String.Equals(p.displayName, userParams, StringComparison.OrdinalIgnoreCase));
            if (sleepingPlayerByName != null)
                return sleepingPlayerByName.IPlayer;

            List<BasePlayer> matchingPlayers = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.displayName.IndexOf(userParams, StringComparison.OrdinalIgnoreCase) >= 0)
                    matchingPlayers.Add(player);
            }

            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.IndexOf(userParams, StringComparison.OrdinalIgnoreCase) >= 0)
                    matchingPlayers.Add(player);
            }

            switch (matchingPlayers.Count)
            {
                case 1: return matchingPlayers[0].IPlayer;
                case > 1:
                {
                    String playerNames = "";
                    for (Int32 i = 0; i < matchingPlayers.Count; i++)
                    {
                        playerNames += matchingPlayers[i].displayName;
                        if (i < matchingPlayers.Count - 1)
                            playerNames += ", ";
                    }

                    Puts(LanguageEn ? $"Specify the nickname or use the ID.\nPossible players with a similar nickname : {playerNames}" : $"Уточните ник или используйте ID.\nВозможные игроки с похожим ником : {playerNames}");
                    return null;
                }
            }

            IPlayer covalencePlayer = covalence.Players.FindPlayer(userParams);
            return covalencePlayer ?? null;
        }
        
        private class PlayerInfo : SplitDatafile<PlayerInfo>
        {
                        
            private const String BaseFolder = "IQSystem" + "/" + "IQBanSystem" + "/";
            private const String UserFolder = "UserBanned" + "/";
            private const String IpFolder = "IPBanned" + "/";

            private static String GetFolder(TypeAction typeBanned) =>
                typeBanned == TypeAction.BannedIP ? BaseFolder + IpFolder : BaseFolder + UserFolder;
            public static PlayerInfo Save(String idOrIp, TypeAction type) => Save(type, GetFolder(type), idOrIp);
            public static void Import(String idOrIp, PlayerInfo data, TypeAction type) => Import(type, GetFolder(type), idOrIp, data);
            public static void Remove(String idOrIp, TypeAction type) => Remove(type, GetFolder(type), idOrIp);
            public static PlayerInfo Get(String idOrIp, TypeAction type) => Get(type, GetFolder(type), idOrIp);
            public static PlayerInfo Load(String idOrIp, TypeAction type) => Load(type, GetFolder(type), idOrIp);
            public static PlayerInfo Clear(String idOrIp, TypeAction type) => ClearAndSave(type, GetFolder(type), idOrIp);
            public static PlayerInfo GetOrLoad(String idOrIp, TypeAction type) => GetOrLoad(type, GetFolder(type), idOrIp);
            public static PlayerInfo GetOrCreate(String idOrIp, TypeAction type) => GetOrCreate(type, GetFolder(type), idOrIp);
            public static String[] GetFiles(TypeAction type) => GetFiles(GetFolder(type));
            
            
            public Boolean permanent = false;
            public Double timeUnbanned;
            public String reason;
            public String serverName;
            public String serverAdress;
            public String owner;

            public List<History> nameHistory = new();
            public List<History> ipHistory = new();
            public List<History> steamIdHistory = new();

            internal class History
            {
                public String value;
                public String time;
            }
            
            [JsonIgnore] public Double GetUnbanTime => timeUnbanned - CurrentTime;

            public Boolean IsBanned()
            {
                if (permanent) return true;
                return GetUnbanTime > 0;
            }
            public void UpdateHistory(TypeHistoryInfo typeInfo, String value) 
            {
                List<History> historyList = GetHistoryList(typeInfo);
                if (IsExistsValueHistory(historyList, value)) return;

                historyList.Insert(0, new History
                {
                    time = DateTime.Now.ToString("HH:mm dd:MM:yyyy"),
                    value = value,
                });
            }

            private List<History> GetHistoryList(TypeHistoryInfo typeInfo)
            {
                return typeInfo switch
                {
                    TypeHistoryInfo.Name => nameHistory,
                    TypeHistoryInfo.IP => ipHistory,
                    TypeHistoryInfo.SteamID => steamIdHistory,
                    _ => throw new ArgumentOutOfRangeException(nameof(typeInfo), typeInfo, null),
                };
            }

            private Boolean IsExistsValueHistory(List<History> histories, String value)
            {
                foreach (History history in histories)
                {
                    if (!history.value.Equals(value)) continue;
                    history.time = DateTime.Now.ToString("HH:mm dd:MM:yyyy");
                    return true;
                }

                return false;
            }
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
                PrintWarning(LanguageEn
                    ? $"Error reading #54327 configuration 'oxide/config/{Name}', creating a new configuration!!"
                    : $"Ошибка чтения #54327 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

                
                
        private abstract class SplitDatafile<T> where T : SplitDatafile<T>, new()
        {
            public static Dictionary<String, T> _players = new();
            public static Dictionary<String, T> _ipAdresses = new();
            public static Dictionary<String, T> _savedItems = new();

            private static Dictionary<String, T> GetRepository(TypeAction type) =>
                type switch
                {
                    TypeAction.BannedIP => _ipAdresses,
                    TypeAction.BannedSteamID => _players,
                    _ => _savedItems
                };
            
            protected static void Import(TypeAction type, String baseFolder, String userId, T data)
            {
                Dictionary<String, T> repository = GetRepository(type);
                repository[userId] = data;

                Save(type, baseFolder, userId);
            }

            protected static String[] GetFiles(String baseFolder)
            {
                try
                {
                    Int32 json = ".json".Length;
                    String[] paths = Interface.Oxide.DataFileSystem.GetFiles(baseFolder, "*.json");
                    for (Int32 i = 0; i < paths.Length; i++)
                    {
                        String path = paths[i];
                        Int32 separatorIndex = path.LastIndexOf("/", StringComparison.Ordinal);
                        paths[i] = path.Substring(separatorIndex + 1, path.Length - separatorIndex - 1 - json);
                    }

                    return paths;
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }

            protected static T Save(TypeAction type, string baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                T data;
                if (!repository.TryGetValue(userId, out data))
                    return null;

                Interface.Oxide.DataFileSystem.WriteObject(baseFolder + userId, data);
                return data;
            }
            
            protected static void Remove(TypeAction type, string baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                if (!repository.ContainsKey(userId))
                    return;

                repository.Remove(userId);
                Interface.Oxide.DataFileSystem.DeleteDataFile(baseFolder + userId);
            }

            protected static T Get(TypeAction type, string baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                T data;
                if (repository.TryGetValue(userId, out data))
                    return data;

                return null;
            }

            protected static T Load(TypeAction type, String baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                T data = null;

                try
                {
                    data = Interface.Oxide.DataFileSystem.ReadObject<T>(baseFolder + userId);
                }
                catch (Exception e)
                {
                    Interface.Oxide.LogError(e.ToString());
                }

                return repository[userId] = data;
            }

            protected static T GetOrLoad(TypeAction type, String baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                T data;
                if (repository.TryGetValue(userId, out data))
                    return data;


                return Load(type, baseFolder, userId);
            }

            protected static T GetOrCreate(TypeAction type, String baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                return GetOrLoad(type, baseFolder, userId) ?? (repository[userId] = new T());
            }

            protected static T ClearAndSave(TypeAction type, String baseFolder, String userId)
            {
                Dictionary<String, T> repository = GetRepository(type);

                T data;
                if (repository.TryGetValue(userId, out data))
                {
                    data = new T();
		   		 		  						  	   		  		 			  	   		  		  		   		 
                    Interface.Oxide.DataFileSystem.WriteObject(baseFolder + userId, data);
                    return data;
                }
		   		 		  						  	   		  		 			  	   		  		  		   		 
                return null;
            }
        }
        
        private void BanIp(String IpAdress, IPlayer iPlayer = null, String reason = default, Double banTime = 0, BasePlayer ownerCommand = null, Boolean skipTeam = false)
        {
            String displayName = iPlayer?.Name ?? String.Empty;
            String idPlayer = iPlayer?.Id ?? String.Empty;

            BanUserOrIp(idPlayer, displayName, IpAdress, reason, banTime, TypeAction.BannedIP, ownerCommand, skipTeam);
        }
        
        object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            LeaveTeamPlayer(player.userID);
            return null;
        }
        
                
                
        private static Configuration config = new Configuration();

        private const String patternIP = @"^\b(?:\d{1,3}\.){3}\d{1,3}\b$";

        
        
                
        
        private void BanCommand(BasePlayer ownerCommand, String[] arg)
        {
            if (arg == null)
            {
                SendInfo(ownerCommand, LanguageEn ? "Use the syntax: ban SteamID/IP/Name* Time(1s/1m/1h/1d) Reason\nItems marked with `*` are mandatory" : "Используйте синтаксис : ban SteamID/IP/Name* Время(1s/1m/1h/1d) Причина\nПункты помеченные `*` - это обязательные пункты");
                return;
            }
            
            if (ownerCommand != null)
                if (!permission.UserHasPermission(ownerCommand.UserIDString, permissionsBan))
                    return;

            if (arg.Length < 1)
            {
                SendInfo(ownerCommand, LanguageEn ? "Use the syntax: ban SteamID/IP/Name* Time(1s/1m/1h/1d) Reason\nItems marked with `*` are mandatory" : "Используйте синтаксис : ban SteamID/IP/Name* Время(1s/1m/1h/1d) Причина\nПункты помеченные `*` - это обязательные пункты");
                return;
            }

            String userParams = arg[0];
            UInt64 targetUserID = 0;
            IPlayer iPlayer = GetIPlayer(userParams);

            Boolean isIp = IsIpAdress(userParams);

            if (!isIp)
            {
                if (!UInt64.TryParse(userParams, out targetUserID))
                {
                    if (iPlayer == null)
                    {
                        SendInfo(ownerCommand, LanguageEn ? "Couldn't find a player" : "Не удалось найти игрока");
                        return;
                    }
                    targetUserID = UInt64.Parse(iPlayer.Id);
                }
            }
            
            Double banTime = 0;
            String reasonArg = String.Empty;

            if (arg.Length >= 2)
            {
                String potentialTime = arg[1];
		   		 		  						  	   		  		 			  	   		  		  		   		 
                if (!String.IsNullOrWhiteSpace(potentialTime) && TryParseTimeSpan(potentialTime, out banTime))
                    reasonArg = arg.Length > 2 ? String.Join(" ", arg.Skip(2)) : null;
                else reasonArg = String.Join(" ", arg.Skip(1));
            }

            if (isIp) 
                BanIp(userParams, iPlayer, reasonArg, banTime, ownerCommand);
            else BanUser(targetUserID, iPlayer, reasonArg, banTime, ownerCommand);
        }

        private void HandlerSteam(IPlayer player, Int32 code, String response)
        {
            if (code != 200 || string.IsNullOrEmpty(response))
            {
                ResponseError("Steam", $"{code}");
                return;
            }
            
            Dictionary<String, Object> jsonresponse;
            try { jsonresponse = JsonConvert.DeserializeObject<Dictionary<String, Object>>(response); }
            catch (JsonReaderException e)
            {
                ResponseError("Steam", e.Message);
                return;
            }
		   		 		  						  	   		  		 			  	   		  		  		   		 
            if (!jsonresponse.TryGetValue("response", out Object value))
            {
                ResponseError("Steam", "Response == null");
                return;
            }
            
            JToken playerData = ((JObject)value)["players"]?[0];
            if (playerData == null)
            {
                ResponseError("Steam", "playerData == null");
                return;
            }
            
            Int64? accountCreationDate = (Int64?)playerData["timecreated"];
            webrequest.Enqueue(
                $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={config.steamSetting.steamApiKey}&steamid={player.Id}&include_played_free_games=1",
                String.Empty,
                (gameCode, gameResponse) => 
                    HandleGameTimes(player, accountCreationDate, gameCode, gameResponse), this, timeout: 10f);
        }

        private void MarkerUpdate()
        {
            for (Int32 i = 0; i < mapMarkers.Count; i++)
            {
                MarkerRepository marker = mapMarkers[i];
                TimeSpan timeSpan = DateTime.Now - marker.timeSetMarker;
                Double totalSeconds = timeSpan.TotalSeconds;

                if (!TryParseTimeSpan(config.bannedSetting.destroyedAfterBannedSetting.mapMarkerDestroyedObjectSetting.markerLifeTime, out Double markerUpdate))
                {
                    PrintError(LanguageEn ? "Incorrect marker update time format. The marker has been deleted." : "Некорректный формат времени обновления маркера. Маркер был удален.");
                    
                    if(!marker.vending.IsDestroyed)
                        marker.vending.Kill();
                    
                    if(!marker.marker.IsDestroyed)
                        marker.marker.Kill();
                    
                    mapMarkers.Remove(marker);
                } 

                if(totalSeconds < markerUpdate) continue;
                
                if(!marker.vending.IsDestroyed)
                    marker.vending.Kill();
                    
                if(!marker.marker.IsDestroyed)
                    marker.marker.Kill();

                mapMarkers.Remove(marker);
            }
        }

        public String FormatTime(Double Second, String UserID = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(Second);
            String Result = String.Empty;
            String Days = GetLang("TITLE_FORMAT_DAYS", UserID);
            String Hourse = GetLang("TITLE_FORMAT_HOURSE", UserID);
            String Minutes = GetLang("TITLE_FORMAT_MINUTES", UserID);
            String Seconds = GetLang("TITLE_FORMAT_SECONDS", UserID);

            if (time.Seconds != 0)
                Result = $"{Format(time.Seconds, Seconds, Seconds, Seconds)}";

            if (time.Minutes != 0)
                Result = $"{Format(time.Minutes, Minutes, Minutes, Minutes)}";

            if (time.Hours != 0)
                Result = $"{Format(time.Hours, Hourse, Hourse, Hourse)}";

            if (time.Days != 0)
                Result = $"{Format(time.Days, Days, Days, Days)}";

            return Result;
        }
        
        
                void HandlerProxyCheckIo(Int32 code, String response, String adress)
        {
            ProcessCheckVPN processChecked = ipListChecked[adress];
            if (code != 200 || string.IsNullOrEmpty(response))
            {
                ResponseError("ProxyCheckIO", $"{code}");
                processChecked.isCompleteProxyCheck = true;
                return;
            }

            JObject jsonResponse;
            try { jsonResponse = JObject.Parse(response); }
            catch (JsonReaderException e)
            {
                ResponseError("ProxyCheckIO", e.Message);
                processChecked.isCompleteProxyCheck = true;
                return;
            }

            String status = jsonResponse["status"]?.ToString();
            if (!status.Equals("ok"))
            {
                String message = jsonResponse["message"]?.ToString();
                ResponseError("ProxyCheckIO", $"{status} : {message}");
                processChecked.isCompleteProxyCheck = true;
                return;
            }

            foreach (JProperty ipInfoProperty in jsonResponse.Properties())
            {
                if (ipInfoProperty.Name.Equals("status", StringComparison.OrdinalIgnoreCase))
                    continue;

                JToken ipInfo = ipInfoProperty.Value;
		   		 		  						  	   		  		 			  	   		  		  		   		 
                if (ipInfo["proxy"] != null && ipInfo["type"] != null)
                {
                    String proxy = ipInfo["proxy"].ToString();
                    String type = ipInfo["type"].ToString();

                    if (proxy.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                        type.Equals("VPN", StringComparison.OrdinalIgnoreCase))
                    {
                        //PrintError("DETECT ");
                        processChecked.detectedVpn++;
                    }
                }
            }
            
            processChecked.isCompleteProxyCheck = true;
        }
		   		 		  						  	   		  		 			  	   		  		  		   		 
        
        
        /// <summary>
        /// Returned IsBanned status
        /// </summary>
        /// <param name="idOrIP"></param>
        /// <returns></returns>
        private Boolean IsBanned(String idOrIP)
        {
            TypeAction typeAction = TypeAction.BannedSteamID;
            if (IsIpAdress(idOrIP))
                typeAction = TypeAction.BannedIP;
            
            PlayerInfo pInfo = PlayerInfo.Get(idOrIP, typeAction);
            return pInfo != null && pInfo.IsBanned();
        }
        
        private String SQL_Query_SelectedDatabase() => $"SELECT * FROM {config.mysqlConnected.dbTableName}";

        public class Fields
        {
            public String name { get; set; }
            public String value { get; set; }
            public bool inline { get; set; }
            public Fields(String name, String value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }
   
        
        private void SaveDataFiles(TypeAction type)
        {
            foreach (KeyValuePair<String, PlayerInfo> player in PlayerInfo._players)
                PlayerInfo.Save(player.Key, type);
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["BANNED_ALERT_TIME"] = "You are banned for the reason {0}. Unban in: {1}",
                ["BANNED_ALERT_PERMANENT"] = "You are permanently banned for the reason {0}",
                ["BANNED_DEFAULT_REASON_PERMANENT"] = "You are permanently banned from the server",
                ["BANNED_DEFAULT_REASON"] = "You are banned from the server. Unban in: {0}",
                ["KICKED_VPN_REASON"] = "You were kicked from the server for using a VPN",
                ["KICKED_DEFAULT_REASON"] = "You were kicked from the server",
                ["KICKED_STEAM_ID_INCORRECTED"] = "Incorrect SteamID",
                ["KICKED_STEAM_NEW_ACCOUNT"] = "Your account is too new to play on our server",
                ["KICKED_STEAM_NO_GAME_TIME"] = "You don't have enough playtime in RUST to play on this server",
                ["BANNED_DEFAULT_REASON_GAME_WITH_CHEATER"] = "Playing with a cheater",

                ["ALERT_ALL_PLAYER_KICKED"] = "Player <color=#CD412B>{0}</color> was kicked from the server.\nReason: <color=#CD412B>{1}</color>\nKick issued by: <color=#1F6BA0>{2}</color>",
                ["ALERT_ALL_PLAYER_BANNED"] = "Player <color=#CD412B>{0}</color> was banned from the server.\nReason: <color=#CD412B>{1}</color>\nBan duration: <color=#CD412B>{2}</color>\nBan issued by: <color=#1F6BA0>{3}</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_ONLY_STORAGE_CLEAR"] = "\n\nAll this player's storage boxes were destroyed, and all items were deleted in the areas: <color=#C26D33>{0}</color>\nThe building was preserved, and you can claim it\n<color=#738D45>*Items will disappear over time!</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_ONLY_STORAGE"] = "\n\nAll this player's storage boxes were destroyed, and all items were dropped on the ground in the areas: <color=#C26D33>{0}</color>\nYou can raid the building and claim the items\n<color=#738D45>*Items will disappear over time!</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_CLEAR_STORAGE"] = "\n\nThis player's buildings were destroyed, and all items were deleted in the areas: <color=#C26D33>{0}</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY"] = "\n\nThis player's buildings were destroyed, and all items were dropped on the ground in the areas: <color=#C26D33>{0}</color>\n<color=#738D45>*Items will disappear over time!</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_ONLY_CODE_LOCK"] = "\n\nAll this player's locks were destroyed in houses in the areas: <color=#C26D33>{0}</color>\n<color=#738D45>You are free to claim the buildings and loot</color>",
                ["ALERT_ALL_USE_MARKER"] = "\n\nAll house locations have been marked on the G-map!\nThey will be removed in: <color=#CD412B>{0}</color>",
                ["ALERT_ALL_BANNED_TEAMS"] = "\nBanned teammates: <color=#CD412B>{0}</color>",
                ["ALERT_ALL_TITILE_PERMANENT"] = "permanently",
                ["ALERT_ALL_TITILE_ADMIN_BANNED"] = "administrator",

                ["TITLE_FORMAT_DAYS"] = "D",
                ["TITLE_FORMAT_HOURSE"] = "H",
                ["TITLE_FORMAT_MINUTES"] = "M",
                ["TITLE_FORMAT_SECONDS"] = "S",
            }, this);

            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["BANNED_ALERT_TIME"] = "Вы заблокированы по причине {0}. Разблокировка через : {1}",
                ["BANNED_ALERT_PERMANENT"] = "Вы заблокированы навсегда по причине {0}",
                ["BANNED_DEFAULT_REASON_PERMANENT"] = "Вы навсегда заблокированы на сервере",
                ["BANNED_DEFAULT_REASON"] = "Вы заблокированы на сервере. Разблокировка через : {0}",
                ["KICKED_VPN_REASON"] = "Вы были кикнуты с сервера за использование VPN",
                ["KICKED_DEFAULT_REASON"] = "Вы были кикнуты с сервера",
                ["KICKED_STEAM_ID_INCORRECTED"] = "Некорректный SteamID",
                ["KICKED_STEAM_NEW_ACCOUNT"] = "Ваш аккаунт слишком новый для игры на нашем сервере",
                ["KICKED_STEAM_NO_GAME_TIME"] = "Вы слишком мало времени провели в RUST для игры на сервере",
                ["BANNED_DEFAULT_REASON_GAME_WITH_CHEATER"] = "игра с нарушителем",

                ["ALERT_ALL_PLAYER_KICKED"] = "Игрок <color=#CD412B>{0}</color> был кикнут с сервере.\nПричина : <color=#CD412B>{1}</color>\nВыдал кик : <color=#1F6BA0>{2}</color>",
                ["ALERT_ALL_PLAYER_BANNED"] = "Игрок <color=#CD412B>{0}</color> был заблокирован на сервере.\nПричина : <color=#CD412B>{1}</color>\nВремя блокировки : <color=#CD412B>{2}</color>\nВыдал блокировку : <color=#1F6BA0>{3}</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_ONLY_STORAGE_CLEAR"] = "\n\nВсе сундуки данного игрока были разрушены и все предметы удалены на квадратах : <color=#C26D33>{0}</color>\nСтроение было сохранено, вы можете завладеть им\n<color=#738D45>*Предметы исчезнут через время!</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_ONLY_STORAGE"] = "\n\nВсе сундуки данного игрока были разрушены и все предметы были выброшены на землю на квадратах : <color=#C26D33>{0}</color>\nВы можете пробраться в строение и забрать их\n<color=#738D45>*Предметы исчезнут через время!</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_CLEAR_STORAGE"] = "\n\nСтроения данного игрока были разрушены и все предметы удалены на квадратах : <color=#C26D33>{0}</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY"] = "\n\nСтроения данного игрока были разрушены и все предметы были выброшены на землю в квадратах : <color=#C26D33>{0}</color>\n<color=#738D45>*Предметы исчезнут через время!</color>",
                ["ALERT_ALL_ADDITIONAL_DESTROY_ONLY_CODE_LOCK"] = "\n\nВсе замки данного игрока были разрушены в домах на квадратах : <color=#C26D33>{0}</color>\n<color=#738D45>Вы вправе забрать строения и лут</color>",
                ["ALERT_ALL_USE_MARKER"] = "\n\nВсе точки домов были отмечены на G-карте!\nОни будут удалены через : <color=#CD412B>{0}</color>",
                ["ALERT_ALL_BANNED_TEAMS"] = "\nЗаблокированные тиммейты : <color=#CD412B>{0}</color>",
                ["ALERT_ALL_TITILE_PERMANENT"] = "навсегда",
                ["ALERT_ALL_TITILE_ADMIN_BANNED"] = "администратор",

                ["TITLE_FORMAT_DAYS"] = "Д",
                ["TITLE_FORMAT_HOURSE"] = "Ч",
                ["TITLE_FORMAT_MINUTES"] = "М",
                ["TITLE_FORMAT_SECONDS"] = "С",

            }, this, "ru");
        }
        
        private String Format(Int32 units, String form1, String form2, String form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units}{form1}";
		   		 		  						  	   		  		 			  	   		  		  		   		 
            if (tmp >= 2 && tmp <= 4)
                return $"{units}{form2}";

            return $"{units}{form3}";
        }
        
        private enum TypeAction
        {
            BannedIP,
            BannedSteamID,
        }

        private String SQL_Query_UpdateUser(TypeAction typeBanned)
        {
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append($"UPDATE {config.mysqlConnected.dbTableName} SET ");
            
            if (typeBanned == TypeAction.BannedSteamID)
            {
                queryBuilder.Append("`steamid` = @0,");
                queryBuilder.Append("`ipAdress` = @1,");
            }
            else queryBuilder.Append("`ipAdress` = @1,");
            queryBuilder.Append("`permanent` = @2,");
            queryBuilder.Append("`timeUnbanned` = @3,");
            queryBuilder.Append("`reason` = @4,");
            queryBuilder.Append("`serverName` = @5,");
            queryBuilder.Append("`serverAdress` = @6,");
            queryBuilder.Append("`owner` = @7,");
            queryBuilder.Append("`nameHistory` = @8,");
            queryBuilder.Append("`ipHistory` = @9,");
            queryBuilder.Append("`steamIdHistory` = @10");

            if (queryBuilder[queryBuilder.Length - 1] == ',')
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
		   		 		  						  	   		  		 			  	   		  		  		   		 
            queryBuilder.Append(typeBanned == TypeAction.BannedSteamID ? " WHERE `steamid` = @0" : " WHERE `ipAdress` = @1");
		   		 		  						  	   		  		 			  	   		  		  		   		 
            return queryBuilder.ToString();
        }

        private void Unload()
        {
            if (_ == null) return;
            
            if (IsOpenMySQL())
                sqlLibrary.CloseDb(sqlConnection);
            else
            {
                SaveDataFiles(TypeAction.BannedIP);
                SaveDataFiles(TypeAction.BannedSteamID);
            }
            
            if (config.vpnSetting.useSaveGodIps)
                WriteData();
            
            if (config.bannedSetting.destroyedAfterBannedSetting.mapMarkerDestroyedObjectSetting.useMapMarkerDestroyedObjectPlayer)
            {
                if (timerUpdateMarkers is { Destroyed: false })
                {
                    timerUpdateMarkers.Destroy();
                    timerUpdateMarkers = null;
                }
                
                foreach (MarkerRepository markerRepository in mapMarkers)
                {
                    if(!markerRepository.vending.IsDestroyed)
                        markerRepository.vending.Kill();
                    
                    if(!markerRepository.marker.IsDestroyed)
                        markerRepository.marker.Kill();
                }
            }
            
            _ = null;
        }

        
                
                
        private void IsAvailabilityConnection(IPlayer player)
        {
            if (permission.UserHasPermission(player.Id, permissionIgnoreBan)) return;
            
            String ipAddress = player.Address;
		   		 		  						  	   		  		 			  	   		  		  		   		 
            if (config.mysqlConnected.useMySQL)
            {
                GetUserData(player, ipAddress);
                return;
            }
            
            String userID = player.Id;
		   		 		  						  	   		  		 			  	   		  		  		   		 
            PlayerInfo playerDataSteam = PlayerInfo.Get(userID, TypeAction.BannedSteamID);
            PlayerInfo playerDataIps = PlayerInfo.Get(ipAddress, TypeAction.BannedIP);
            
            if (playerDataSteam == null && playerDataIps == null)
                return;
            
            ProcessBanData(player, playerDataSteam, TypeAction.BannedSteamID, userID, ipAddress);
            ProcessBanData(player, playerDataIps, TypeAction.BannedIP, userID, ipAddress);
        }

        
        
        private void GetUserData(IPlayer player, String ipAddress)
        {
            if (sqlConnection == null) return;
    
            String sqlQueryGetUserData = SQL_Query_GetUserData();
            Sql selectCommand = Oxide.Core.Database.Sql.Builder.Append(sqlQueryGetUserData, player.Id, ipAddress);
    
            sqlLibrary.Query(selectCommand, sqlConnection, list =>
            {
                if (list.Count > 0)
                {
                    foreach (Dictionary<String, Object> entry in list)
                    {
                        String valueImported = String.Empty;
                        TypeAction typeBanned;
                        
                        if (UInt64.TryParse((String)entry["steamid"], out UInt64 steamIdParse))
                        {
                            valueImported = player.Id;
                            typeBanned = TypeAction.BannedSteamID;
                        }
                        else
                        {
                            valueImported = (String)entry["ipAdress"];
                            typeBanned = TypeAction.BannedIP;
                        }
                        
                        if (!Int32.TryParse((String)entry["permanent"], out Int32 permanent))
                        {
                            PrintError(LanguageEn ? $"Error retrieving permanent information for player {valueImported} ({typeBanned})" : $"Ошибка получения информации permanent для игрока {valueImported}({typeBanned})");
                            return;
                        }
                        Boolean isPemanent = permanent != 0;

                        if (!Double.TryParse((String)entry["timeUnbanned"], out Double timeUnbanned))
                        {
                            PrintError(LanguageEn ? $"Error retrieving timeUnbanned information for player {{valueImported}} ({{typeBanned}})" : $"Ошибка получения информации timeUnbanned для игрока {valueImported}({typeBanned}))");
                            return;
                        }

                        if (!isPemanent && timeUnbanned - CurrentTime <= 0)
                            continue;
                        
                        String serverAdress = (String)entry["serverAdress"];
                        String reason = (String)entry["reason"];
                        String serverName = (String)entry["serverName"];
                        
                        String owner = (String)entry["owner"];
                        List<PlayerInfo.History> nameHistory = new ();
                        List<PlayerInfo.History> ipHistory = new ();
                        List<PlayerInfo.History> steamIdHistory = new ();
                        
                        String jsonNameHistory = (String)entry["nameHistory"];
                        String jsonipHistory = (String)entry["ipHistory"];
                        String jsonsteamIdHistory = (String)entry["steamIdHistory"];
                        
                        nameHistory = JsonConvert.DeserializeObject<List<PlayerInfo.History>>(jsonNameHistory);
                        ipHistory = JsonConvert.DeserializeObject<List<PlayerInfo.History>>(jsonipHistory);
                        steamIdHistory = JsonConvert.DeserializeObject<List<PlayerInfo.History>>(jsonsteamIdHistory);
                        
                        PlayerInfo.Import(valueImported, new PlayerInfo
                        {
                            permanent = isPemanent,
                            timeUnbanned = timeUnbanned,
                            reason = reason,
                            nameHistory = nameHistory,
                            ipHistory = ipHistory,
                            steamIdHistory = steamIdHistory,
                            serverName = serverName, 
                            serverAdress = serverAdress,
                            owner = owner,
                        }, typeBanned);
                        
                        PlayerInfo playerDataSteam = PlayerInfo.Get(player.Id, TypeAction.BannedSteamID);
                        PlayerInfo playerDataIps = PlayerInfo.Get(ipAddress, TypeAction.BannedIP);
            
                        if (playerDataSteam == null && playerDataIps == null)
                            return;
            
                        ProcessBanData(player, playerDataSteam, TypeAction.BannedSteamID, player.Id, ipAddress);
                        ProcessBanData(player, playerDataIps, TypeAction.BannedIP, player.Id, ipAddress);
                    }
                }
            });
        }
        
        private enum TypeHistoryInfo
        {
            Name,
            IP,
            SteamID
        }

        [ConsoleCommand("kick")] 
        private void ConsoleKickCommand(ConsoleSystem.Arg arg) => KickCommand(arg.Player(), arg.Args);

        private String GetFormatTimeDiscord(String input)
        {
            String result = Regex.Replace(input, @"(\d+)s", m => $"{m.Groups[1].Value} {(LanguageEn ? "seconds" : "секунды")}");
            result = Regex.Replace(result, @"(\d+)m", m => $"{m.Groups[1].Value} {(LanguageEn ? "minuts" : "минуты")}");
            result = Regex.Replace(result, @"(\d+)h", m => $"{m.Groups[1].Value} {(LanguageEn ? "hours" : "часов")}");
            result = Regex.Replace(result, @"(\d+)d", m => $"{m.Groups[1].Value} {(LanguageEn ? "days" : "дней")}");
		   		 		  						  	   		  		 			  	   		  		  		   		 
            return result;
        }
        
        private String BuildConnectionString(String host, Int32 port, String database, String user, String password)
        {
            return String.Format(
                "Server={0};Port={1};Database={2};User={3};Password={4};Pooling=false;default command timeout=120;Allow Zero Datetime=true;CharSet=utf8mb4;",
                host, port, database, user, password
            );
        }
		   		 		  						  	   		  		 			  	   		  		  		   		 
                
        private void Init()
        {
            _ = this;
            
            if (config.mysqlConnected.useMySQL)
                SQL_OpenConnection();
            else
            {
                LoadDataFiles(TypeAction.BannedIP);
                LoadDataFiles(TypeAction.BannedSteamID);
                
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    OnPlayerConnected(player);
            }
            
            if(config.vpnSetting.useSaveGodIps)
                ReadData();

            if (!config.generalSetting.discordSetting.alertBanned.teamsAlert.saveInfoTeams &&
                !config.generalSetting.additionalSetting.useAlertBlockTeam && !config.bannedSetting.teamBannedSetting.useBlockTeam)
            {
                Unsubscribe(nameof(OnTeamCreated));
                Unsubscribe(nameof(OnTeamDisbanded));
                Unsubscribe(nameof(OnTeamKick));
                Unsubscribe(nameof(OnTeamLeave));
                Unsubscribe(nameof(OnTeamAcceptInvite));
            }
        }

        private void Request(String url, String payload, Action<Int32> callback = null)
        {
            Dictionary<String, String> header = new Dictionary<String, String>();
            header.Add("Content-Type", "application/json");
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                Single seconds = Single.Parse(Math.Ceiling((Double)(Int32)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex) { }

            }, this, RequestMethod.POST, header, timeout: 10f);
        }

        private List<Int32> appIds = new() { 252490, 480 };

        private String SQL_Query_DeletedDatabase(TypeAction typeUnBanned) => $"DELETE FROM `{config.mysqlConnected.dbTableName}` WHERE " + 
                                                                             $"{(typeUnBanned == TypeAction.BannedSteamID ? "`steamid` = @0" : "`ipAdress` = @0")}";

        public String GetLang(String LangKey, String userID = null, params Object[] args)
        {
            sb.Clear();
            if (args == null) return lang.GetMessage(LangKey, this, userID);
            sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
            return sb.ToString();
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (config.generalSetting.discordSetting.alertBanned.teamsAlert.saveInfoTeams ||
                config.generalSetting.additionalSetting.useAlertBlockTeam || config.bannedSetting.teamBannedSetting.useBlockTeam)
                PullOutTeamsInfo(player);

            Configuration.BannedSetting.DestroyedAfterBannedSetting configDestroyed = config.bannedSetting.destroyedAfterBannedSetting;
            Boolean isMarkerUse = configDestroyed.mapMarkerDestroyedObjectSetting.useMapMarkerDestroyedObjectPlayer && config.generalSetting.additionalSetting.useAlertAllPlayers && configDestroyed.useAlertGridHomes && configDestroyed.useDestroyObjects;

            if (isMarkerUse)
            {
                foreach (MarkerRepository markerRepository in mapMarkers)
                {
                    if(!markerRepository.vending.IsDestroyed)
                        markerRepository.vending.SendNetworkUpdate();
                    
                    if(!markerRepository.marker.IsDestroyed)
                        markerRepository.marker.SendNetworkUpdate();
                }
            }
        }

        
        
        private void SetTirifyBan(String steamIdString, String reason)
        {
            if (!TirifyGamePluginRust) return;
            if (!config.bannedSetting.tirifyBannedReplace) return;

            TirifyGamePluginRust.Call("SetTirifyBan", steamIdString, reason);
            Puts(LanguageEn ? $"Additional blocking was detected by the player's hardware {steamIdString} (Tirify)" : $"Выдана дополнительная блокировка по железу игроку {steamIdString} (Tirify)");
        }
        private String SQL_Query_GetUserData() => $"SELECT `steamid`, `ipAdress`, `permanent`, `timeUnbanned`, `reason`, `serverName`, `serverAdress`, `owner`, `nameHistory`, `ipHistory`, `steamIdHistory` FROM {config.mysqlConnected.dbTableName} WHERE `steamid` = @0 OR `ipAdress` = @1";
        
        [ConsoleCommand("ban")] 
        private void ConsoleBanCommand(ConsoleSystem.Arg arg) => BanCommand(arg.Player(), arg.Args);

        void OnUserConnected(IPlayer player)
        {
             if (useVpnApi || useProxyCheck)
                 CheckPlayerUsedVPN(player);

             if (!String.IsNullOrWhiteSpace(config.steamSetting.steamApiKey))
                 CheckSteamController(player);

             IsAvailabilityConnection(player);
        }
        
                
        
        private static StringBuilder sb = new StringBuilder();
        
                
        
        
                
        private void SendInfo(BasePlayer player, String message)
        {
            if (player != null)
                SendChat(message, player);
            else Puts(message);
        }

                
                private void ProcessStorageContainer(BaseEntity entityPlayer, Action<StorageContainer> action)
        {
            if (entityPlayer is not StorageContainer) return;
            StorageContainer storage = entityPlayer as StorageContainer;
            if (storage.inventory != null && storage.inventory.itemList.Count != 0)
                action(storage);
        }
		   		 		  						  	   		  		 			  	   		  		  		   		 
        
                void HandlerVpnApiIo(Int32 code, String response, String adress)
        {
            ProcessCheckVPN processChecked = ipListChecked[adress];
            if (code != 200 || string.IsNullOrEmpty(response))
            {
                ResponseError("VPNApiIO", $"{code}");
                processChecked.isCompleteVpnApi = true;
                return;
            }
            
            JObject jsonResponse;
            try { jsonResponse = JObject.Parse(response); }
            catch (JsonReaderException e)
            {
                ResponseError("VPNApiIO", e.Message);
                processChecked.isCompleteVpnApi = true;
                return;
            }

            String message = jsonResponse["message"]?.ToString();
            if (!string.IsNullOrEmpty(message))
            {
                ResponseError("VPNApiIO", message);
                processChecked.isCompleteVpnApi = true;
                return;
            }

            JToken security = jsonResponse["security"];
            if (security == null)
            {
                ResponseError("VPNApiIO", "Security information is missing in the response.");
                processChecked.isCompleteVpnApi = true;
                return;
            }

            Boolean isVpn = security["vpn"]?.ToObject<Boolean>() ?? false;
            Boolean isProxy = security["proxy"]?.ToObject<Boolean>() ?? false;

            if (isVpn || isProxy)
            {
                //PrintError("DETECT ");
                processChecked.detectedVpn++;
            }            
            processChecked.isCompleteVpnApi = true;
        }

        
        
        // [ConsoleCommand("db.steam")]
        // private void DebugSteam(ConsoleSystem.Arg arg)
        // {
        //     String steamID = "76561198434075094";//arg.Args[0];
        //     webrequest.Enqueue(
        //         $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={config.steamSetting.steamApiKey}&steamid={steamID}&include_played_free_games=1",
        //         String.Empty,
        //         (gameCode, gameResponse) =>
        //         {
        //             
        //             Dictionary<Int32, Int32> gameTimes = new();
        //             Int32 allTimeGames = 0;
        //     
        //             Dictionary<String, Object> jsonresponse;
        //             try { jsonresponse = JsonConvert.DeserializeObject<Dictionary<String, Object>>(gameResponse); }
        //             catch (JsonReaderException e)
        //             {
        //                 ResponseError("Steam", e.Message);
        //                 return;
        //             }
        //             
        //             if (jsonresponse.TryGetValue("response", out Object value))
        //             {
        //                 JToken games = ((JObject)value)["games"];
        //                 if (games != null)
        //                 {
        //                     foreach (JToken game in games)
        //                     {
        //                         Int32 appId = (Int32)game["appid"];
        //                         Int32 playtime = (Int32)game["playtime_forever"];
        //                 
        //                         if (appIds.Contains(appId))
        //                             gameTimes[appId] = playtime;
        //
        //                         allTimeGames += playtime;
        //                     }
        //                 }
        //             }
        //
        //             Boolean isClosedProfile = allTimeGames == 0;
        //             
        //             if (isClosedProfile || gameTimes.Count == 0)
        //                 return;
        //     
        //             Int32 timeToGamingAll = 0;
        //             foreach (Int32 appId in appIds)
        //                 if (gameTimes.TryGetValue(appId, out Int32 time))
        //                     timeToGamingAll += time;
        //     
        //             timeToGamingAll *= 3600;
        //             TryParseTimeSpan(config.steamSetting.minPlayGameTime, out Double minPlayGameTimes);
        //             PrintWarning(timeToGamingAll.ToString() + " | " + minPlayGameTimes.ToString());
        //             
        //         }, this, timeout: 10f);
        //     
        //     //76561198434075094
        // }
        
        private void CheckSteamController(IPlayer player)
        {
            if (String.IsNullOrWhiteSpace(config.steamSetting.steamApiKey)) return;

            webrequest.Enqueue($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={config.steamSetting.steamApiKey}&steamids={player.Id}", String.Empty,
                (code, response) => HandlerSteam(player, code, response), this, timeout: 10f);
        }
        
        private void TeamAdded(RelationshipManager.PlayerTeam team)
        {
            NextTick(() =>
            {
                for (Int32 i = 0; i < team.members.Count; i++)
                for (Int32 j = i + 1; j < team.members.Count; j++)
                {
                    UInt64 player1 = team.members[i];
                    UInt64 player2 = team.members[j];

                    if (!teamLocalMemory.ContainsKey(player1))
                        teamLocalMemory[player1] = new Dictionary<UInt64, TeamLocalMemory>();

                    if (!teamLocalMemory.ContainsKey(player2))
                        teamLocalMemory[player2] = new Dictionary<UInt64, TeamLocalMemory>();

                    if (!teamLocalMemory[player1].ContainsKey(player2))
                        teamLocalMemory[player1][player2] = new TeamLocalMemory
                        {
                            firstJoin = CurrentTime,
                            lastRemoved = 0,
                        };

                    if (!teamLocalMemory[player2].ContainsKey(player1))
                        teamLocalMemory[player2][player1] = new TeamLocalMemory
                        {
                            firstJoin = CurrentTime,
                            lastRemoved = 0,
                        };
                }
            });
        }

        
        private void SendChat(String message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, message, config.generalSetting.iqchatSetting.customPrefix, config.generalSetting.iqchatSetting.customAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, message);
        }
        [ChatCommand("unban")] 
        private void ChatUnBanCommand(BasePlayer ownerCommand, String cmd, String[] arg) => UnBanCommand(ownerCommand, arg);
        
        object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            TeamAdded(team);
            return null;
        }

        
        
        
        private Boolean IsOpenMySQL() => config.mysqlConnected.useMySQL && sqlConnection != null;

        [ConsoleCommand("unban")] 
        private void ConsoleUnBanCommand(ConsoleSystem.Arg arg) => UnBanCommand(arg.Player(), arg.Args);
        
        [ChatCommand("kick")] 
        private void ChatKickCommand(BasePlayer ownerCommand, String cmd, String[] arg) => KickCommand(ownerCommand, arg);

        private void SQL_GetData()
        {
            Sql sql = Sql.Builder.Append(SQL_Query_CreatedDatabase());
            sqlLibrary.Insert(sql, sqlConnection);
            sql = Sql.Builder.Append(SQL_Query_SelectedDatabase());
            sqlLibrary.Query(sql, sqlConnection, list =>
            {
                if (list.Count > 0)
                    foreach (Dictionary<String, Object> entry in list)
                    {
                        String valueImported = String.Empty;
                        TypeAction typeBanned;
                        
                        if (UInt64.TryParse((String)entry["steamid"], out UInt64 steamID))
                        {
                            valueImported = steamID.ToString();
                            typeBanned = TypeAction.BannedSteamID;
                        }
                        else
                        {
                            valueImported = (String)entry["ipAdress"];
                            typeBanned = TypeAction.BannedIP;
                        }
                        
                        if (!Int32.TryParse((String)entry["permanent"], out Int32 permanent))
                        {
                            PrintError(LanguageEn ? $"Error retrieving permanent information for player {valueImported} ({typeBanned})" : $"Ошибка получения информации permanent для игрока {valueImported}({typeBanned})");
                            return;
                        }
                        Boolean isPemanent = permanent != 0;

                        if (!Double.TryParse((String)entry["timeUnbanned"], out Double timeUnbanned))
                        {
                            PrintError(LanguageEn ? $"Error retrieving timeUnbanned information for player {valueImported} ({typeBanned})" : $"Ошибка получения информации timeUnbanned для игрока {valueImported}({typeBanned}))");
                            return;
                        }

                        String reason = (String)entry["reason"];
                        String serverName = (String)entry["serverName"];
                        String serverAdress = (String)entry["serverAdress"];
                        
                        String owner = (String)entry["owner"];
                        List<PlayerInfo.History> nameHistory = new ();
                        List<PlayerInfo.History> ipHistory = new ();
                        List<PlayerInfo.History> steamIdHistory = new ();
                        
                        String jsonNameHistory = (String)entry["nameHistory"];
                        String jsonipHistory = (String)entry["ipHistory"];
                        String jsonsteamIdHistory = (String)entry["steamIdHistory"];
                        
                        nameHistory = JsonConvert.DeserializeObject<List<PlayerInfo.History>>(jsonNameHistory);
                        ipHistory = JsonConvert.DeserializeObject<List<PlayerInfo.History>>(jsonipHistory);
                        steamIdHistory = JsonConvert.DeserializeObject<List<PlayerInfo.History>>(jsonsteamIdHistory);
                        
                        PlayerInfo.Import(valueImported, new PlayerInfo
                        {
                            permanent = isPemanent,
                            timeUnbanned = timeUnbanned,
                            reason = reason,
                            nameHistory = nameHistory,
                            ipHistory = ipHistory,
                            steamIdHistory = steamIdHistory,
                            serverName = serverName, 
                            serverAdress = serverAdress,
                            owner = owner,
                        }, typeBanned);
                    }
                
                Puts(LanguageEn ? $"MySQL database initialized, {list.Count} users retrieved" : $"Инициализирована база данных MySQL, получено {list.Count} пользователей");
                
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    OnPlayerConnected(player);
            });
        }

        private class Configuration
        {
            [JsonProperty(LanguageEn ? "Server data configuration" : "Настройка данных сервера")]
            public GeneralSetting generalSetting = new GeneralSetting();
            [JsonProperty(LanguageEn ? "Setting up MySQL connection" : "Настройка подключения MySQL")]
            public MySQLSetting mysqlConnected = new MySQLSetting();
            [JsonProperty(LanguageEn ? "Setting up interaction with Steam" : "Настройка взаимодействия со Steam")]
            public SteamSetting steamSetting = new SteamSetting();
            [JsonProperty(LanguageEn ? "Setting up VPN protection" : "Настройка защиты от VPN")]
            public VPNSetting vpnSetting = new VPNSetting();
            [JsonProperty(LanguageEn ? "Setting up the blocking system" : "Настройка системы блокировки")]
            public BannedSetting bannedSetting = new BannedSetting();    
            
            internal class GeneralSetting
            {
                [JsonProperty(LanguageEn ? "Server name" : "Название сервера")] 
                public String serverName;
                [JsonProperty(LanguageEn ? "IP:PORT server" : "IP:Port сервера")] 
                public String serverAdress;
                [JsonProperty(LanguageEn ? "Additional configuration" : "Дополнительная настройка")]
                public AdditionalSetting additionalSetting = new AdditionalSetting();    
                [JsonProperty(LanguageEn ? "Setting IQChat" : "Настройка IQChat")]
                public IQChatSetting iqchatSetting = new IQChatSetting();  
                [JsonProperty(LanguageEn ? "Setting Discord" : "Настройка Discord")]
                public DiscordSetting discordSetting = new DiscordSetting();  
                
                internal class AdditionalSetting
                {
                    [JsonProperty(LanguageEn ? "Sound effect for all players to notify when a player is blocked ('Notify all players' should be enabled) (leave it blank - if you don't need it)" : "Звуковой эффект для всех игроков для уведомления о блокировке игрока (должно быть включено 'Уведомлять всех игроков') (оставьте пустым - если вам не нужно это)")]
                    public String effectAlertAllPlayers;
                    [JsonProperty(LanguageEn ? "Notify all players when a player is blocked" : "Уведомлять всех игроков о блокировке игрока")]
                    public Boolean useAlertAllPlayers;
                    [JsonProperty(LanguageEn ? "Notify all players about player's kick" : "Уведомлять всех игроков о кике игрока")]
                    public Boolean useAlertKickPlayer;
                    [JsonProperty(LanguageEn ? "Add information about blocked teammates to the blocking notification" : "Добавлять в уведомление о блокировке - информацию о заблокированных тиммейтах")]
                    public Boolean useAlertBlockTeam; 
                }
                
                internal class IQChatSetting
                {
                    [JsonProperty(LanguageEn ? "IQChat: Chat Prefix" : "IQChat : Префикс в чате")]
                    public String customPrefix; 
                    [JsonProperty(LanguageEn ? "IQChat: Chat Avatar (Use Steam64ID)" : "IQChat : Аватарка в чате (Используйте Steam64ID)")]
                    public String customAvatar;
                }

                internal class DiscordSetting
                {
                    [JsonProperty(LanguageEn ? "Discord notification setting for player ban" : "Настройка уведомления в Discord о блокировке игрока")]
                    public AlertBanned alertBanned = new AlertBanned();
    
                    internal class AlertBanned
                    {
                        [JsonProperty(LanguageEn ? "Webhooks: For banned notifications" : "Webhooks : Для уведомлений о блокировке")]
                        public String webHookBanned;
                        [JsonProperty(LanguageEn ? "Include home information of the banned player in the notification" : "Добавлять в уведомление дома где прописан заблокированный игрок")]
                        public Boolean cupboardAuthInfo;
                        [JsonProperty(LanguageEn ? "Additional notification setting for players who played with the banned player" : "Настройка дополнительного уведомления о тех с кем играл заблокированный")]
                        public TeamsAlert teamsAlert = new TeamsAlert();
        
                        internal class TeamsAlert
                        {
                            [JsonProperty(LanguageEn ? "Include players who played with the banned player in the notification" : "Добавлять в уведомление игроков с которыми играл забаненный")]
                            public Boolean saveInfoTeams;
                            [JsonProperty(LanguageEn ? "Time spent together with the banned player to be included in the notification (Format: 1s/1m/1h/1d)" : "Сколько времени должны провести вместе забаненный с игроком для его отображения в уведомлении (В формате 1s/1m/1h/1d)")]
                            public String formatTimePlaying;
                        }
                    }
                    
                    [JsonProperty(LanguageEn ? "Webhooks : For unbanned notifications" : "Webhooks : Для уведомлений о разблокировке")]
                    public String webHookUnBanned;
                    [JsonProperty(LanguageEn ? "Webhooks : For kicked notifications" : "Webhooks : Для уведомлений о киках")]
                    public String webHookKicked;
                    [JsonProperty(LanguageEn ? "Webhooks : For notifications about a blocked player's login attempt" : "Webhooks : Для уведомлений о попытке входа заблокированного игрока")]
                    public String webHookConnectedBanned;
                    [JsonProperty(LanguageEn ? "Link to the image in Discord" : "Ссылка на изображение в Discord")]
                    public String imageLink;
                }
            }
            internal class SteamSetting
            {
                [JsonProperty(LanguageEn ? "Please provide the Steam API Key for the operation of these functions (Obtain it here - https://steamcommunity.com/dev/apikey)" : "Укажите SteamApiKey для работы данных функций (Взять тут - https://steamcommunity.com/dev/apikey)")]
                public String steamApiKey;
                [JsonProperty(LanguageEn ? "The minimum amount of time since Steam account registration for logging into the server. Format: 1m/1h/1d (leave empty if you don't need this function)" : "Минимальное количество времени с регистрации аккаунта Steam для входа на сервер. Формат 1m/1h/1d (оставьте пустым - если вам не нужна эта функция)")]
                public String minSteamRegisterTime; 
                [JsonProperty(LanguageEn ? "The minimum amount of playtime in RUST required for logging into the server. Format: 1m/1h/1d (leave empty if you don't need this function)" : "Минимальное количество отыгранного времени в RUST для входа на сервер. Формат 1m/1h/1d (оставьте пустым - если вам не нужна эта функция)")]
                public String minPlayGameTime; 
            }
            internal class MySQLSetting
            {
                [JsonProperty(LanguageEn ? "Use MySQL database (true - yes/false - no)" : "Использовать базу-данных MySQL (true - да/false - нет)")]
                public Boolean useMySQL;
                [JsonProperty(LanguageEn ? "Host (IP-Address)" : "Хост (IP-Address)")]
                public String dbIP;
                [JsonProperty(LanguageEn ? "Port (default 3306)" : "Порт (стандартно 3306)")]
                public String dbPort;
                [JsonProperty(LanguageEn ? "Database name" : "Имя базы данных")]
                public String dbName;
                [JsonProperty(LanguageEn ? "Username" : "Имя пользователя")]
                public String dbUser;
                [JsonProperty(LanguageEn ? "Password" : "Пароль")]
                public String dbPassword;
                [JsonProperty(LanguageEn ? "Table name" : "Название таблицы")]
                public String dbTableName;
            }
            
            internal class VPNSetting
            {
                [JsonProperty(LanguageEn ? "Saving 'Good IPs' to avoid reusing requests on the player" : "Сохранять `Хорошие IP`, чтобы не тратить запросы повторно на игрока")]
                public Boolean useSaveGodIps;
                [JsonProperty(LanguageEn ? "The number of detects for kicking a player for VPN (if you use more than 1 service for VPN checking)" : "Количество детектов для кика игрока за VPN (Если вы испольузете более 1 сервиса на проверку VPN)")]
                public Int32 countDetectedToKickInVPN;
                [JsonProperty(LanguageEn ? "Token https://iphub.info/" : "Токен от https://iphub.info/")]
                public String iPHubToken;
                [JsonProperty(LanguageEn ? "Token https://proxycheck.io/" : "Токен от https://proxycheck.io/")]
                public String proxyCheckIo;
                [JsonProperty(LanguageEn ? "Token https://vpnapi.io/" : "Токен от https://vpnapi.io/")]
                public String vpnApiIoToken;
                [JsonProperty(LanguageEn ? "IP Whitelist for ignoring VPN" : "Белый список IP для игнорирования VPN")]
                public List<String> vpnWhiteList = new List<String>();
            }
            
            internal class BannedSetting
            {
                [JsonProperty(LanguageEn ? "TirifyGamePluginRust : Block the player by hardware using Tirify if the lock is issued forever (true - yes/false - no)" : "TirifyGamePluginRust : Блокировать игрока по железу с помощью Tirify если блокировка выдана навсегда (true - да/false - нет)")]
                public Boolean tirifyBannedReplace;
                [JsonProperty(LanguageEn ? "Kill player after being banned on the server" : "Убивать игрока после блокировки на сервере")]
                public Boolean useKilledPlayer;
                [JsonProperty(LanguageEn ? "Setting for banning teammates for playing with the offender" : "Настройка блокировки тиммейтов за игру с нарушителем")]
                public TeamBannedSetting teamBannedSetting = new TeamBannedSetting();
                [JsonProperty(LanguageEn ? "Setting for destroying player's objects after being banned" : "Настройка уничтожения объектов игрока после блокировки")]
                public DestroyedAfterBannedSetting destroyedAfterBannedSetting = new DestroyedAfterBannedSetting();
                
                internal class TeamBannedSetting
                {
                    [JsonProperty(LanguageEn ? "Specify the time played with the offender for issuing a ban. Format 1s/1m/1h/1d" : "Укажите время игры с нарушителем для выдачи блокировки. Формат 1s/1m/1h/1d")]
                    public String timeDeteckGamingTeamMemory;
                    [JsonProperty(LanguageEn ? "Ban teammates of the banned player" : "Блокировать тиммейтов заблокированного игрока")]
                    public Boolean useBlockTeam;
                    [JsonProperty(LanguageEn ? "Specify the ban time for teammates for playing with the offender. Format 1s/1m/1h/1d (Leave empty for issuing a permanent ban)" : "Укажите время блокировки тиммейтов за игру с нарушителем. Формат 1s/1m/1h/1d (Оставьте пустым - для выдачи блокировки навсегда)")]
                    public String timeBlockTeam; 
                }

                internal class DestroyedAfterBannedSetting
                {
                    [JsonProperty(LanguageEn ? "Use the function to delete player's objects after being banned" : "Использовать функцию удаления объектов игрока после блокировки")]
                    public Boolean useDestroyObjects;
                    [JsonProperty(LanguageEn ? "Type of object destruction: 0 - All objects, 1 - All objects and dropping items from containers on the ground, 2 - Only locks, 3 - Only boxes, 4 - Only boxes with dropping items from them" : "Тип уничтожения объектов : 0 - Все объекты, 1 - Все объекты и выбрасывание предметов из контейнеров на пол, 2 - Только замки, 3 - Только ящики, 4 - Только ящики с выбрасыванием предметов из них")]
                    public TypeDestroy typeDestroyed;
                    [JsonProperty(LanguageEn ? "Notify in chat about squares of destroyed player's objects" : "Уведомлять в чате о квадратах разрушенных объектах игрока")]
                    public Boolean useAlertGridHomes;
                    [JsonProperty(LanguageEn ? "Setting for displaying markers on the G-Map" : "Настройка отображения маркеров на G-Map")]
                    public MapMarkerDestroyedObject mapMarkerDestroyedObjectSetting = new();
                    
                    internal class MapMarkerDestroyedObject
                    {
                        [JsonProperty(LanguageEn ? "Display points with destroyed objects on the map" : "Отображать на карте точки с разрушенными объектами")]
                        public Boolean useMapMarkerDestroyedObjectPlayer;
                        [JsonProperty(LanguageEn ? "Main marker color" : "Основной цвет маркера")]
                        public String mainColorMarker;
                        [JsonProperty(LanguageEn ? "Outline marker color" : "Цвет обводки маркера")]
                        public String additionalColorMarker;
                        [JsonProperty(LanguageEn ? "Marker radius on the map" : "Радиус маркера на карте")]
                        public Single radiusMarker;
                        [JsonProperty(LanguageEn ? "How long the marker will be displayed. Format 1s/1m/1h/1d" : "Сколько будет отображаться маркер. Формат 1s/1m/1h/1d")]
                        public String markerLifeTime;
                    }
                }
            }
            
	        public static Configuration GetNewConfiguration()
	        {
		        return new Configuration
                {
                    generalSetting = new GeneralSetting
                    {
                        serverName = "MY SERVER",
                        serverAdress = "127.0.0.1",
                        additionalSetting = new GeneralSetting.AdditionalSetting
                        {
                            effectAlertAllPlayers = "assets/bundled/prefabs/fx/item_unlock.prefab",
                            useAlertAllPlayers = true,
                            useAlertKickPlayer = false,
                            useAlertBlockTeam = false,
                        },
                        iqchatSetting = new GeneralSetting.IQChatSetting
                        {
                            customPrefix = "<color=#1F6BA0>[IQBanSystem]</color>",
                            customAvatar = "0"
                        },
                        discordSetting = new GeneralSetting.DiscordSetting
                        {
                            alertBanned = new GeneralSetting.DiscordSetting.AlertBanned
                            {
                                webHookBanned = String.Empty,
                                cupboardAuthInfo = false,
                                teamsAlert = new GeneralSetting.DiscordSetting.AlertBanned.TeamsAlert
                                {
                                    saveInfoTeams = false,
                                    formatTimePlaying = "12h",
                                }
                            },
                            webHookUnBanned = String.Empty,
                            webHookKicked = String.Empty,
                            webHookConnectedBanned = String.Empty,
                            imageLink = "https://i.postimg.cc/MGLfp4dR/IQBan-System.png",
                        },
                    },
                    mysqlConnected = new MySQLSetting
                    {
                        useMySQL = false,
                        dbIP = "",
                        dbPort = "3306",
                        dbName = "",
                        dbUser = "",
                        dbPassword = "",
                        dbTableName = "IQBanSystem_Db"
                    },
                    steamSetting = new SteamSetting
                    {
                        steamApiKey = "",
                        minSteamRegisterTime = "14d",
                        minPlayGameTime = "3d",
                    },
                    vpnSetting = new VPNSetting
                    {
                        useSaveGodIps = true,
                        countDetectedToKickInVPN = 1,
                        iPHubToken = "",
                        proxyCheckIo = "",
                        vpnApiIoToken = "",
                        vpnWhiteList = new List<String>
                        {
                            "127.0.0.1"
                        }
                    },
                    bannedSetting = new BannedSetting
                    {
                        tirifyBannedReplace = false,
                        useKilledPlayer = true,
                        teamBannedSetting = new BannedSetting.TeamBannedSetting
                        {
                            useBlockTeam = false,
                            timeDeteckGamingTeamMemory = "3h",
                            timeBlockTeam = "7d"
                        },
                        destroyedAfterBannedSetting = new BannedSetting.DestroyedAfterBannedSetting
                        {
                            useDestroyObjects = false,
                            typeDestroyed = TypeDestroy.AllObjects,
                            useAlertGridHomes = false,
                            mapMarkerDestroyedObjectSetting = new BannedSetting.DestroyedAfterBannedSetting.MapMarkerDestroyedObject
                            {
                                useMapMarkerDestroyedObjectPlayer = false,
                                mainColorMarker = "#CD412B",
                                additionalColorMarker = "#1E2020",
                                radiusMarker = 0.25f,
                                markerLifeTime = "10m",
                            }
                        },
                    }
                };
	        }
        }

        private Boolean useIpHub = false;
        
        private List<String> DestroyObjectBanned(IPlayer bannedPlayer, String bannedDisplayName)
        {
            Vector3 lastObjectPos = Vector3.zero;
            List<String> homeCords = new ();
            List<BaseEntity> entitesPlayer = Facepunch.Pool.Get<List<BaseEntity>>();
            Configuration.BannedSetting.DestroyedAfterBannedSetting configDestroyed = config.bannedSetting.destroyedAfterBannedSetting;

            Func<BaseNetworkable, Boolean> baseFilter = e => e != null && e is BaseEntity && (e as BaseEntity).OwnerID == UInt64.Parse(bannedPlayer.Id);
            
            Func<BaseNetworkable, Boolean> filter = baseFilter;
            baseFilter = configDestroyed.typeDestroyed switch
            {
                TypeDestroy.OnlyLocks => e => filter(e) && e is BaseLock,
                TypeDestroy.OnlyStorageAndDropInStorage or TypeDestroy.OnlyStorage => e => filter(e) && e is StorageContainer,
                _ => baseFilter
            };

            Boolean isBuildingAccess = false;
            
            foreach (BaseEntity entityPlayer in BaseNetworkable.serverEntities.entityList.Get().Values.Where(baseFilter).OrderByDescending(x => x is StorageContainer))
            {
                if (entityPlayer.IsDestroyed) continue;
                if (!isBuildingAccess)
                {
                    BuildingPrivlidge buildingPrivilage = entityPlayer.GetBuildingPrivilege();
                    if (buildingPrivilage == null) continue;
                    isBuildingAccess = true;
                }

                switch (configDestroyed.typeDestroyed)
                {
                    case TypeDestroy.AllObjectsAndDropInStorage or TypeDestroy.OnlyStorageAndDropInStorage:
                        ProcessStorageContainer(entityPlayer, storage => { storage.DropItems(); });
                        break;
                    case TypeDestroy.AllObjects or TypeDestroy.OnlyStorage:
                        ProcessStorageContainer(entityPlayer, storage => { storage.inventory.Clear(); });
                        break;
                }
                
                if (config.generalSetting.additionalSetting.useAlertAllPlayers && configDestroyed.useAlertGridHomes)
                {
                    if (lastObjectPos != Vector3.zero)
                        if (Vector3.Distance(lastObjectPos, entityPlayer.transform.position) >= 100)
                        {
                            String cordHomeOnMap = MapHelper.PositionToString(entityPlayer.transform.position);

                            if (!homeCords.Contains(cordHomeOnMap))
                            {
                                homeCords.Add(cordHomeOnMap);
                                CreateMapMarker(entityPlayer.transform.position, bannedDisplayName);
                            }
                                        
                            lastObjectPos = entityPlayer.transform.position;
                            continue;
                        }
            
                    lastObjectPos = entityPlayer.transform.position;
                                
                    if (homeCords.Count == 0)
                    {
                        homeCords.Add(MapHelper.PositionToString(entityPlayer.transform.position));
                        CreateMapMarker(entityPlayer.transform.position, bannedDisplayName);
                    }
                }
                
                entityPlayer.Kill();
            }
            
            Facepunch.Pool.FreeUnmanaged(ref entitesPlayer);
            
            return homeCords; 
        }
		   		 		  						  	   		  		 			  	   		  		  		   		 
                
        
        [PluginReference] private Plugin IQChat, RustApp, IQTeleportation, TirifyGamePluginRust;

        
        
        private void KickUser(IPlayer iTarget, String reason, BasePlayer ownerCommand)
        {
            if (iTarget == null)
            {
                SendInfo(ownerCommand, LanguageEn ? "Couldn't find a player" : "Не удалось найти игрока");
                return;
            }
            
            if (!iTarget.IsConnected)
            {
                SendInfo(ownerCommand, LanguageEn ? "The player has not yet connected to the server" : "Игрок еще не подключился к серверу");
                return;
            }
            
            String resultReason = String.IsNullOrWhiteSpace(reason) ? GetLang("KICKED_DEFAULT_REASON", iTarget.Id) : reason;
            String ownerKicked = ownerCommand == null ? "Console" : $"{ownerCommand.displayName}({ownerCommand.userID})";

            if (!String.IsNullOrWhiteSpace(config.generalSetting.discordSetting.webHookKicked))
            {
                List<Fields> fieldsKick = new List<Fields>
                {
                    new Fields(LanguageEn ? "Issued by kick" : "Выдал кик", ownerKicked, false),
                    new Fields("", "", false),
                    new Fields(LanguageEn ? "Player" : "Игрок", $"{iTarget.Name}({iTarget.Id})", false),
                    new Fields(LanguageEn ? "Reason" : "Причина", resultReason, false),
                };

                SendDiscord(fieldsKick, TypeAlertDiscord.Kicked, config.generalSetting.discordSetting.webHookKicked);
            }
            
            Interface.CallHook("OnKickPlayer", iTarget.Id, resultReason, ownerCommand);

            iTarget.Kick(resultReason);
            
            SendInfo(ownerCommand, LanguageEn ? $"You have successfully kicked player {iTarget.Name}" : $"Вы успешно кикнули игрока {iTarget.Name}");
            if (ownerCommand != null)
                Puts(LanguageEn ? $"Player {iTarget.Name}({iTarget.Id}) was kicked for: {resultReason}.\nKicked by: {ownerKicked}" : $"Игрок {iTarget.Name}({iTarget.Id}) был кикнут по причине : {resultReason}.\nВыдал кик : {ownerKicked}");
            
            if(config.generalSetting.additionalSetting.useAlertKickPlayer)
                foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
                    SendChat(GetLang("ALERT_ALL_PLAYER_KICKED", basePlayer.UserIDString, iTarget.Name, resultReason, ownerKicked), basePlayer);
        }
        
        private const String GenericPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        
        
        public class FancyMessage
        {
            public String content { get; set; }
            public Boolean tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public String title { get; set; }
                public Int32 color { get; set; }
                public List<Fields> fields { get; set; }
                public Footer footer { get; set; }
                public Authors author { get; set; }

                public Embeds(String title, Int32 color, List<Fields> fields, Authors author, Footer footer)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                    this.author = author;
                    this.footer = footer;

                }
            }

            public FancyMessage(String content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public String toJSON() => JsonConvert.SerializeObject(this);
        }
        private void CreateMapMarker(Vector3 position, String namePlayer)
        {
            Configuration.BannedSetting.DestroyedAfterBannedSetting configDestroyed = config.bannedSetting.destroyedAfterBannedSetting;
            if (!configDestroyed.mapMarkerDestroyedObjectSetting.useMapMarkerDestroyedObjectPlayer) return;
            VendingMachineMapMarker vending = GameManager.server.CreateEntity(VendingPrefab, position, Quaternion.identity, true) as VendingMachineMapMarker;
            vending.markerShopName = namePlayer;
            vending.enableSaving = false;
            vending.EnableGlobalBroadcast(true);
            vending.Spawn();
                
            MapMarkerGenericRadius genericMarker = GameManager.server.CreateEntity(GenericPrefab, new Vector3(), Quaternion.identity, true) as MapMarkerGenericRadius;
            ColorUtility.TryParseHtmlString(configDestroyed.mapMarkerDestroyedObjectSetting.mainColorMarker, out Color color1);
            ColorUtility.TryParseHtmlString(configDestroyed.mapMarkerDestroyedObjectSetting.additionalColorMarker, out Color color2);
            genericMarker.color1 = color1;
            genericMarker.color2 = color2;
            genericMarker.radius = configDestroyed.mapMarkerDestroyedObjectSetting.radiusMarker;
            genericMarker.alpha = 1f;
            genericMarker.SetParent(vending);
            genericMarker.Spawn();
            genericMarker.SendUpdate(); 
            genericMarker.EnableGlobalBroadcast(true);
        
            MarkerRepository markerInfo = new MarkerRepository() { vending = vending, marker = genericMarker, timeSetMarker = DateTime.Now };
            mapMarkers.Add(markerInfo);
        }
        
        private void RunEffect(BasePlayer player, String effectPath)
        {
            Effect effect = new Effect(effectPath, player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);
        }

        
        private String SQL_Query_InsertUser()
        {
            return $"INSERT INTO {config.mysqlConnected.dbTableName} " +
                          "(`steamid`, `ipAdress`, `permanent`, `timeUnbanned`, `reason`, `serverName`, `serverAdress`, `owner`, `nameHistory`, `ipHistory`, `steamIdHistory`) " +
                          "VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)";
        }
		   		 		  						  	   		  		 			  	   		  		  		   		 
        
        
        private void UnBanCommand(BasePlayer ownerCommand, String[] arg)
        {
            if (arg == null)
            {
                SendInfo(ownerCommand, LanguageEn ? "Use syntax: unban SteamID/IP/Name* Time(1s/1m/1h/1d)\nItems marked with * are mandatory" : "Используйте синтаксис : unban SteamID/IP/Name* Время(1s/1m/1h/1d) \nПункты помеченные `*` - это обязательные пункты");
                return;
            }
            
            if (ownerCommand != null)
                if (!permission.UserHasPermission(ownerCommand.UserIDString, permissionsUnBan))
                    return;

            if (arg.Length < 1)
            {
                SendInfo(ownerCommand, LanguageEn ? "Use syntax: unban SteamID/IP/Name* Time(1s/1m/1h/1d)\nItems marked with * are mandatory" : "Используйте синтаксис : unban SteamID/IP/Name* Время(1s/1m/1h/1d) \nПункты помеченные `*` - это обязательные пункты");
                return;
            }
            
            String userParams = arg[0];
            UInt64 targetUserID = 0;
            IPlayer iPlayer = GetIPlayer(userParams);
		   		 		  						  	   		  		 			  	   		  		  		   		 
            Boolean isIp = IsIpAdress(userParams);

            if (!isIp)
            {
                if (!UInt64.TryParse(userParams, out targetUserID))
                {
                    if (iPlayer == null)
                    {
                        SendInfo(ownerCommand, LanguageEn ? "Couldn't find a player" : "Не удалось найти игрока");
                        return;
                    }
                    targetUserID = UInt64.Parse(iPlayer.Id);
                }
            }
            
            Double unBanTime = 0;

            if (arg.Length >= 2)
            {
                String timeFormat = arg[1];
                if (!String.IsNullOrWhiteSpace(timeFormat))
                {
                    if (!TryParseTimeSpan(timeFormat, out unBanTime))
                    {
                        SendInfo(ownerCommand, LanguageEn ? "Incorrect time format specified, use 1s/1m/1h/1d" : "Неверно указан формат времени, используйте 1s/1m/1h/1d");
                        return;
                    }
                }
            }

            TypeAction typeUnbanned = isIp ? TypeAction.BannedIP : TypeAction.BannedSteamID;
            String valueInfo = isIp ? userParams : targetUserID.ToString();
            PlayerInfo infoUser = PlayerInfo.Get(valueInfo, typeUnbanned);
            
            if (infoUser == null)
            {
                SendInfo(ownerCommand, LanguageEn ? "This player is not banned" : "Данный игрок не имеет блокировки");
                return;
            }
            
            UnBanUserOrIp(valueInfo, infoUser, typeUnbanned, unBanTime, ownerCommand);
        }
        
                
                
        private List<IPlayer> BannedTeams(UInt64 playerID, BasePlayer ownerCommand)
        {
            List<IPlayer> PlayersFromTeam = new List<IPlayer>();
            
            if (config.bannedSetting.teamBannedSetting.useBlockTeam)
            {
                if (!TryParseTimeSpan(config.bannedSetting.teamBannedSetting.timeDeteckGamingTeamMemory, out Double banGaming))
                    return PlayersFromTeam;
                
                Dictionary<UInt64, Double> teamMemory = GetTeamsBannedPlayer(playerID);
                if(teamMemory == null || teamMemory.Count == 0) return PlayersFromTeam;

                TryParseTimeSpan(config.bannedSetting.teamBannedSetting.timeBlockTeam, out Double banTeamTime);
                
                foreach (KeyValuePair<UInt64, Double> memory in teamMemory)
                {
                    if(memory.Value < banGaming) continue;
                    if(memory.Key == playerID) continue;
                    IPlayer iPlayer = GetIPlayer($"{memory.Key}");
                    
                    BanUser(memory.Key, iPlayer,"BANNED_DEFAULT_REASON_GAME_WITH_CHEATER", banTeamTime, ownerCommand, true);
                    
                    if (!PlayersFromTeam.Contains(iPlayer))
                        PlayersFromTeam.Add(iPlayer);
                }
            }
            // else
            // {
            //  RelationshipManager.PlayerTeam teams = player.Team;

            //     if(teams != null)
            //         foreach (UInt64 teamsMember in teams.members)
            //         {
            //             if (teamsMember == playerID) continue;
            //             TryParseTimeSpan(config.bannedSetting.teamBannedSetting.timeBlockTeam, out Double banTimeTeam);
            //             BanUser(teamsMember, reason: "BANNED_DEFAULT_REASON_GAME_WITH_CHEATER", banTime: banTimeTeam, skipTeam: true, ownerCommand: ownerCommand);
            //             IPlayer iPlayer = GetIPlayer($"{teamsMember}");
            //
            //             if (!PlayersFromTeam.Contains(iPlayer))
            //                 PlayersFromTeam.Add(iPlayer);
            //         }
            // }

            return PlayersFromTeam;
        }
        void ReadData()
        {
            ipListGods = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<String>>("IQSystem/IQBanSystem/ListGodIps");
            teamLocalMemory = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, Dictionary<UInt64, TeamLocalMemory>>>("IQSystem/IQBanSystem/TeamsMemory");
            Puts(LanguageEn ? $"Database initialized with {ipListGods.Count} VPN-verified IPs" : $"Инициализирована база данных {ipListGods.Count} проверенных на VPN IP");
        }
        
        
        private void SQL_OpenConnection()
        {
            if (String.IsNullOrWhiteSpace(config.mysqlConnected.dbIP) || String.IsNullOrWhiteSpace(config.mysqlConnected.dbPassword) ||
                String.IsNullOrWhiteSpace(config.mysqlConnected.dbPort) || String.IsNullOrWhiteSpace(config.mysqlConnected.dbName) ||
                String.IsNullOrWhiteSpace(config.mysqlConnected.dbUser))
            {
                config.mysqlConnected.useMySQL = false;
                PrintWarning(LanguageEn ? "Incorrect MySQL data entered, MySQL usage has been disabled. Please check the configuration for correct data" : "Некорректно введены данные для MySQL, использование MySQL было отключено. Проверьте конфигурацию на корректность данных");
                Init();
                return;
            }
            
            sqlConnection = 
                sqlLibrary.OpenDb(
                BuildConnectionString(
                    config.mysqlConnected.dbIP, 
                    Convert.ToInt32(config.mysqlConnected.dbPort), 
                    config.mysqlConnected.dbName, 
                    config.mysqlConnected.dbUser, 
                    config.mysqlConnected.dbPassword
                ), 
                this);
            
            if (sqlConnection == null) return;

            SQL_GetData();
        }
        
        
        void HandlerIPHub(Int32 code, String response, String adress)
        {
            ProcessCheckVPN processChecked = ipListChecked[adress];
            if (code != 200 || string.IsNullOrEmpty(response))
            {
                ResponseError("IPHub", $"{code}");
                processChecked.isCompleteIPHub = true;
                return;
            }

            Dictionary<String, Object> jsonresponse;
            try { jsonresponse = JsonConvert.DeserializeObject<Dictionary<String, Object>>(response); }
            catch (JsonReaderException e)
            {
                ResponseError("IPHub", e.Message);
                processChecked.isCompleteIPHub = true;
                return;
            }

            if (jsonresponse["block"] == null)
            {
                processChecked.isCompleteIPHub = true;
                return;
            }
            String playerVpn = (jsonresponse["block"].ToString());

            if (playerVpn == "1")
            {
                //PrintError("DETECT ");
                processChecked.detectedVpn++;
            }
            
            processChecked.isCompleteIPHub = true;
        }


        
        
        
        // private void RustApp_BanDeleted(String steamID)
        // {
        //     if (String.IsNullOrWhiteSpace(steamID)) return;
        //     PlayerInfo infoUser = PlayerInfo.Get(steamID, TypeAction.BannedSteamID);
        //     if (infoUser == null)
        //     {
        //         PrintWarning(LanguageEn ? $"We received an unlock request from RusApp for the ID '{steamID}', there is no such user in the database" : $"Получили запрос на разблокировку от RusApp для ID '{steamID}', такого пользователя нет в базе данных");
        //         return;
        //     }
        //     
        //     UnBanUserOrIp(steamID, infoUser, TypeAction.BannedSteamID);
        // }


        private void RustApp_OnPaidAnnounceBan(String steamID, List<String> targets, String reason)
        {
            if (!UInt64.TryParse(steamID, out UInt64 userID)) return;
            IPlayer iPlayer = GetIPlayer(steamID);
            BanUser(userID, iPlayer, reason: reason);
        }

        private List<MarkerRepository> mapMarkers = new List<MarkerRepository>();

        private void ProcessBanData(IPlayer player, PlayerInfo playerData, TypeAction typeAction, String userID, String ipAddress)
        {
            if (playerData == null)
                return;

            playerData.UpdateHistory(TypeHistoryInfo.Name, player.Name);
            playerData.UpdateHistory(TypeHistoryInfo.IP, ipAddress);
            playerData.UpdateHistory(TypeHistoryInfo.SteamID, userID);

            if (playerData.IsBanned())
            {
                String kickReason = GetKickReason(playerData, userID);
                player.Kick(kickReason);
                
                if (!String.IsNullOrWhiteSpace(config.generalSetting.discordSetting.webHookConnectedBanned))
                {
                    List<Fields> fieldsKick = new List<Fields>
                    {
                        new Fields(LanguageEn ? "Player" : "Игрок", $"{player.Name}({player.Id})", false),
                        new Fields("IP", ipAddress, false),
                        new Fields(LanguageEn ? "Banned reason" : "Причина блокировки", playerData.reason, false),
                        new Fields(LanguageEn ? "Unban time" : "Время разблокировки", playerData.permanent ? (LanguageEn ? "never" : "никогда") : FormatTime(playerData.GetUnbanTime), false),
                    };
                    
                    SendDiscord(fieldsKick, TypeAlertDiscord.ConnectionBanned, config.generalSetting.discordSetting.webHookConnectedBanned);
                }
            }
            else
            {
                PlayerInfo.Remove(userID, typeAction);
            }
        }
        
        private void SetTirifyUnBan(String steamIdString, String reason = "")
        {
            if (!TirifyGamePluginRust) return;
            if (!config.bannedSetting.tirifyBannedReplace) return;
		   		 		  						  	   		  		 			  	   		  		  		   		 
            TirifyGamePluginRust.Call("SetTirifyUnBan", steamIdString, reason);
            Puts(LanguageEn ? $"The player's hardware has been unblocked {steamIdString} (Tirify)" : $"Выдана разблокировка по железу игроку {steamIdString} (Tirify)");
        }
        
        private void OnServerShutdown() => Unload();
        
        private Dictionary<UInt64, Double> GetTeamsBannedPlayer(UInt64 bannedPlayer)
        {
            Dictionary<UInt64, Double> playerTeamMemory = new Dictionary<UInt64, Double>();
            
            if (teamLocalMemory.TryGetValue(bannedPlayer, out Dictionary<UInt64, TeamLocalMemory> teamsPlayers))
            {
                foreach (KeyValuePair<UInt64, TeamLocalMemory> teammate in teamsPlayers)
                {
                    Double duration = (teammate.Value.lastRemoved == 0 ? CurrentTime : teammate.Value.lastRemoved) - teammate.Value.firstJoin;
                    playerTeamMemory.Add(teammate.Key, duration);
                   // PrintToChat(teammate.Key.ToString() + " " + teammate.Value.firstJoin + " " + teammate.Value.lastRemoved);
                }
            }
            //PrintToChat(playerTeamMemory.Count.ToString());
            return playerTeamMemory;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        private Boolean useVpnApi = false;
		   		 		  						  	   		  		 			  	   		  		  		   		 

        private void ResponseError(String nameService, String message) => PrintWarning(LanguageEn ? $"[{nameService}] The service is temporarily unavailable or your key is incorrect, it returned a negative code, the request was not sent.\nInformation (Code or messages from the service): {message}" : $"[{nameService}] Сервис временно недоступен или ваш ключ некорректен, он вернул отрицательный код, запрос не был отправлен.\nИнформация (Код или сообщения от сервиса) : : {message}");
        
                
                
                
        
        public class MarkerRepository
        {
            public VendingMachineMapMarker vending;
            public MapMarkerGenericRadius marker;
            public DateTime timeSetMarker;
        }
        private const String permissionsKick = "iqbansystem.kick";
        
        private void HandleGameTimes(IPlayer player, Int64? accountCreationDate, Int32 code, String response)
        {
            if (code != 200 || string.IsNullOrEmpty(response))
            {
                ResponseError("Steam", $"{code}");
                return;
            }
            
            Dictionary<String, Object> jsonresponse;
            try { jsonresponse = JsonConvert.DeserializeObject<Dictionary<String, Object>>(response); }
            catch (JsonReaderException e)
            {
                ResponseError("Steam", e.Message);
                return;
            }

            Dictionary<Int32, Int32> gameTimes = new();
            Int32 allTimeGames = 0;
            
            if (jsonresponse.TryGetValue("response", out Object value))
            {
                JToken games = ((JObject)value)["games"];
                if (games != null)
                {
                    foreach (JToken game in games)
                    {
                        Int32 appId = (Int32)game["appid"];
                        Int32 playtime = (Int32)game["playtime_forever"];
                        
                        if (appIds.Contains(appId))
                            gameTimes[appId] = playtime;

                        allTimeGames += playtime;
                    }
                }
            }

            Boolean isClosedProfile = allTimeGames == 0;

            if (!String.IsNullOrWhiteSpace(config.steamSetting.minSteamRegisterTime))
                if (TryParseTimeSpan(config.steamSetting.minSteamRegisterTime, out Double minRegisterSteamTime))
                    if (accountCreationDate < minRegisterSteamTime)
                        KickUser(player, GetLang("KICKED_STEAM_NEW_ACCOUNT", player.Id), null);
            
            if (isClosedProfile || gameTimes.Count == 0)
                return;
            
            Int64 timeToGamingAll = 0;
            foreach (Int32 appId in appIds)
                if (gameTimes.TryGetValue(appId, out Int32 time))
                    timeToGamingAll += time;
            
            timeToGamingAll *= 60;

            if (!String.IsNullOrWhiteSpace(config.steamSetting.minPlayGameTime))
                if (TryParseTimeSpan(config.steamSetting.minPlayGameTime, out Double minPlayGameTimes))
                    if (timeToGamingAll < minPlayGameTimes) 
                        KickUser(player, GetLang("KICKED_STEAM_NO_GAME_TIME", player.Id), null);
        }


                
                
        private void CheckPlayerUsedVPN(IPlayer player)
        {
            if (player == null) return;
            if (!player.IsConnected) return;
            
            if (permission.UserHasPermission(player.Id, permissionsIgnoreVPN)) return;
            
            String adress = player.Address;
            
            PlayerInfo playerDataSteam = PlayerInfo.Get(player.Id, TypeAction.BannedSteamID);
            if (playerDataSteam != null && playerDataSteam.IsBanned()) return;
            
            PlayerInfo playerDataIP = PlayerInfo.Get(adress, TypeAction.BannedIP);
            if (playerDataIP != null && playerDataIP.IsBanned()) return;

            if (config.vpnSetting.vpnWhiteList.Contains(adress)) return;
            if (ipListGods.Contains(adress)) return;
            if (ipListChecked.TryGetValue(adress, out ProcessCheckVPN value))
            {
                if (value == null) return;
                if ((useIpHub && !value.isCompleteIPHub) || (useVpnApi && !value.isCompleteVpnApi) ||
                    (useProxyCheck && !value.isCompleteProxyCheck))
                {
                    if (timerCheckVpn == null || timerCheckVpn.Destroyed)
                        timerCheckVpn = timer.Once(5f, () => { CheckPlayerUsedVPN(player); });
                    return;
                }
                
                if (value.detectedVpn >= maxDetectedVpn)
                { 
                    KickUser(player, GetLang("KICKED_VPN_REASON", player.Id), null);
                    ipListChecked.Remove(adress);
                    
                    if (ipListChecked.Count == 0)
                    {
                        if (timerCheckVpn is { Destroyed: false })
                        {
                            timerCheckVpn.Destroy();
                            timerCheckVpn = null;
                        }
                    }
                    return;
                }

                ipListGods.Add(adress);
                ipListChecked.Remove(adress);

                if (ipListChecked.Count == 0)
                {
                    if (timerCheckVpn is { Destroyed: false })
                    {
                        timerCheckVpn.Destroy();
                        timerCheckVpn = null;
                    }
                }
                return;
            }
            
            ipListChecked.Add(adress, new ProcessCheckVPN
            {
                isCompleteIPHub = !useIpHub,
                isCompleteVpnApi = !useVpnApi,
                isCompleteProxyCheck = !useProxyCheck,
                detectedVpn = 0,
            });
            
            if (useIpHub)
            {
                webrequest.Enqueue($"http://v2.api.iphub.info/ip/{adress}", String.Empty,
                    (code, response) => HandlerIPHub(code, response, adress), this,
                    RequestMethod.GET,
                    new Dictionary<String, String> { ["X-Key"] = config.vpnSetting.iPHubToken }, timeout: 10f);
            }

            if (useVpnApi)
            {
                webrequest.Enqueue($"https://vpnapi.io/api/{adress}?key={config.vpnSetting.vpnApiIoToken}", String.Empty,
                    (code, response) => HandlerVpnApiIo(code, response, adress), this, timeout: 10f);
            }

            if (useProxyCheck)
            {
                webrequest.Enqueue($"https://proxycheck.io/v2/{adress}?key={config.vpnSetting.proxyCheckIo}&risk=1&vpn=1",
                    String.Empty,
                    (code, response) => HandlerProxyCheckIo(code, response, adress),
                    this, timeout: 10f);
            }

            if (timerCheckVpn == null || timerCheckVpn.Destroyed)
                timerCheckVpn = timer.Once(5f, () =>
                {
                    CheckPlayerUsedVPN(player);
                });
        }

                
        
        
        
        private void UnBanUserOrIp(String valueInfo, PlayerInfo infoUser, TypeAction type, Double unBanTime = 0, BasePlayer ownerPlayer = null)
        {
            if (type == TypeAction.BannedSteamID)
            {
                Object canUnBanPlayer = Interface.Oxide.CallHook("CanUnBanPlayer",UInt64.Parse(valueInfo), unBanTime, ownerPlayer); 
                if (canUnBanPlayer != null)
                {
                    switch (canUnBanPlayer)
                    {
                        case String player when !string.IsNullOrWhiteSpace(player):
                            Puts($"{canUnBanPlayer}");
                            return;
                        case Boolean value when !value:
                            return;
                    }
                }
                
                SetTirifyUnBan(valueInfo, String.Empty);
            }

            String ownerCommand = ownerPlayer == null ? "Console" : $"{ownerPlayer.displayName}({ownerPlayer.UserIDString})";
            String ownerID = ownerPlayer != null ? ownerPlayer.UserIDString : null;
            
            if (unBanTime != 0 && (infoUser.GetUnbanTime > 0 || infoUser.permanent))
            {
                if (infoUser.permanent)
                {
                    infoUser.permanent = false;
                    infoUser.timeUnbanned = unBanTime + CurrentTime;
                        
                    PlayerInfo.Save(valueInfo, type);
                    InserOrUpdateDatabase(valueInfo, type);
                    
                    Interface.CallHook(type == TypeAction.BannedSteamID ? "OnChangePermanentBannedID" : "OnChangePermanentBannedIP", valueInfo, unBanTime, ownerPlayer); 
                    
                    SendInfo(ownerPlayer, LanguageEn ? $"The permanent ban has been changed to a temporary ban, unlocking in: {FormatTime(unBanTime, ownerID)}" : $"Бан навсегда был заменен на временный бан, разблокировка через : {FormatTime(unBanTime, ownerID)}"); 
                    
                    if (!String.IsNullOrWhiteSpace(config.generalSetting.discordSetting.webHookUnBanned))
                    {
                        List<Fields> fieldsUnban = new List<Fields>();

                        fieldsUnban.Add(new Fields(LanguageEn ? "Replaced permanent ban with a temporary ban" : "Заменил блокировку навсегда на временную", ownerCommand, true));
                        fieldsUnban.Add(new Fields(type == TypeAction.BannedIP ? "IP" : LanguageEn ? "Player" : "Игрок", valueInfo, false));
                        fieldsUnban.Add(new Fields(LanguageEn ? "Time left until unban" : "Осталось до разблокировки", FormatTime(infoUser.GetUnbanTime, null), false));
                        fieldsUnban.Add(new Fields("", "", false));

                        SendDiscord(fieldsUnban, TypeAlertDiscord.Unbanned, config.generalSetting.discordSetting.webHookUnBanned);
                    }
                    return;
                }
                
                if (unBanTime < infoUser.GetUnbanTime)
                {
                    infoUser.timeUnbanned -= unBanTime;
                    
                    PlayerInfo.Save(valueInfo, type);
                    InserOrUpdateDatabase(valueInfo, type);
                    
                    Interface.CallHook(type == TypeAction.BannedSteamID ? "OnUpdateTimeBannedID" : "OnUpdateTimeBannedIP", valueInfo, infoUser.timeUnbanned - CurrentTime, ownerPlayer); 

                    SendInfo(ownerPlayer, LanguageEn ? $"You've reduced the player's ban time to {FormatTime(unBanTime, ownerID)}. Unlock in: {FormatTime(infoUser.GetUnbanTime, ownerID)}" : $"Вы снизили время блокировки игрока на {FormatTime(unBanTime, ownerID)}. Разблокировка через : {FormatTime(infoUser.GetUnbanTime, ownerID)}");
                    
                    if (!String.IsNullOrWhiteSpace(config.generalSetting.discordSetting.webHookUnBanned))
                    {
                        List<Fields> fieldsUnban = new List<Fields>();

                        fieldsUnban.Add(new Fields(LanguageEn ? "Reduced ban time" : "Снизил время блокировки", ownerCommand, true));
                        fieldsUnban.Add(new Fields(type == TypeAction.BannedIP ? "IP" : LanguageEn ? "Player" : "Игрок", valueInfo, false));
                        fieldsUnban.Add(new Fields(LanguageEn ? "Reduced by" : "Снижено на", FormatTime(unBanTime, null), false));
                        fieldsUnban.Add(new Fields(LanguageEn ? "Time left until unban" : "Осталось до разблокировки", FormatTime(infoUser.GetUnbanTime, null), false));
                        fieldsUnban.Add(new Fields("", "", false));

                        SendDiscord(fieldsUnban, TypeAlertDiscord.Unbanned, config.generalSetting.discordSetting.webHookUnBanned);
                    }
                    return;
                }
            }
            
            PlayerInfo.Remove(valueInfo, type);

            if (IsOpenMySQL())
                DeleteDatabase(valueInfo, type);

            Interface.CallHook(type == TypeAction.BannedSteamID ? "OnUnbannedID" : "OnUnbannedIP", valueInfo, ownerPlayer); 
		   		 		  						  	   		  		 			  	   		  		  		   		 
            SendInfo(ownerPlayer, LanguageEn ? $"Player {valueInfo} has been unbanned. Unban issued by {ownerCommand}" : $"Игрок {valueInfo} был разблокирован. Выдал разблокировку {ownerCommand}");
            if (ownerPlayer != null)
                SendInfo(null, LanguageEn ? $"Player {valueInfo} has been unbanned. Unban issued by {ownerCommand}" : $"Игрок {valueInfo} был разблокирован. Выдал разблокировку {ownerCommand}");
            
            if (!String.IsNullOrWhiteSpace(config.generalSetting.discordSetting.webHookUnBanned))
            {
                List<Fields> fieldsUnban = new List<Fields>();

                fieldsUnban.Add(new Fields(LanguageEn ? "Removed ban" : "Снял блокировку", ownerCommand, true));
                fieldsUnban.Add(new Fields(type == TypeAction.BannedIP ? "IP" : LanguageEn ? "Player" : "Игрок", valueInfo, false));
                fieldsUnban.Add(new Fields("", "", false));
		   		 		  						  	   		  		 			  	   		  		  		   		 
                SendDiscord(fieldsUnban, TypeAlertDiscord.Unbanned, config.generalSetting.discordSetting.webHookUnBanned);
            }
        }

        private void LeaveTeamPlayer(UInt64 targetPlayer)
        {
            foreach (KeyValuePair<UInt64, Dictionary<UInt64, TeamLocalMemory>> memoryTeams in teamLocalMemory)
            {
                foreach (KeyValuePair<UInt64,TeamLocalMemory> localMemory in memoryTeams.Value)
                {
                    if ((localMemory.Key != targetPlayer && memoryTeams.Key != targetPlayer)) continue;
                    localMemory.Value.lastRemoved = CurrentTime;
                }
            }
        }

        private enum TypeAlertDiscord
        {
            Banned,
            Unbanned,
            Kicked,
            ConnectionBanned
        }
        
        private enum TypeDestroy
        {
            AllObjects,
            AllObjectsAndDropInStorage,
            OnlyLocks,
            OnlyStorage,
            OnlyStorageAndDropInStorage,
        }
        
        private readonly Core.MySql.Libraries.MySql sqlLibrary = Interface.Oxide.GetLibrary<Core.MySql.Libraries.MySql>();

        
        private void SendDiscord(List<Fields> fields, TypeAlertDiscord typeAlertDiscord, String webHooks)
        {
            if (String.IsNullOrWhiteSpace(webHooks)) return;
            
            String nameAction = typeAlertDiscord switch
            {
                TypeAlertDiscord.Banned => LanguageEn ? "Banned function" : "Функции блокировки",
                TypeAlertDiscord.Unbanned => LanguageEn ? "Unbanned function" : "Функции разблокировки",
                TypeAlertDiscord.Kicked => LanguageEn ? "Player Kicked" : "Игрок кикнут",
                TypeAlertDiscord.ConnectionBanned => LanguageEn ? "A blocked player's login attempt" : "Попытка входа заблокированного игрока",
                _ => "ERROR ALERT EMBED"
            };
            
            Int32 colorEmbed = typeAlertDiscord switch
            {
                TypeAlertDiscord.Banned => 16734296,
                TypeAlertDiscord.Unbanned => 16752984,
                TypeAlertDiscord.Kicked => 12741939,
                TypeAlertDiscord.ConnectionBanned => 5832569,
                _ => 2558568
            };
            
            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, colorEmbed, fields, new Authors(nameAction, null, config.generalSetting.discordSetting.imageLink, null), 
                new Footer($"{(LanguageEn ? "Server" : "Сервер")} {config.generalSetting.serverName} - {config.generalSetting.serverAdress}", "", "")) });

            Request(webHooks, newMessage.toJSON());
        }

        private String GetKickReason(PlayerInfo playerData, string userID)
        {
            if (!string.IsNullOrWhiteSpace(playerData.reason))
            {
                return playerData.permanent ? 
                    GetLang("BANNED_ALERT_PERMANENT", userID, playerData.reason) :
                    GetLang("BANNED_ALERT_TIME", userID, playerData.reason, FormatTime(playerData.GetUnbanTime, userID));
            }
            else
            {
                return playerData.permanent ? 
                    GetLang("BANNED_DEFAULT_REASON_PERMANENT", userID) :
                    GetLang("BANNED_DEFAULT_REASON", userID, FormatTime(playerData.GetUnbanTime, userID));
            }
        }
        private const String permissionsIgnoreVPN = "iqbansystem.ignorevpncheck";

        
        
        private void DeleteDatabase(String idOrIP, TypeAction typeUnBanned)
        {
            if (sqlConnection == null) return;
            String sqlQueryDelete = SQL_Query_DeletedDatabase(typeUnBanned);
            Sql deleteCommand = Oxide.Core.Database.Sql.Builder.Append(sqlQueryDelete, idOrIP);
            sqlLibrary.Delete(deleteCommand, sqlConnection, rowsAffected =>
            {
                Puts(LanguageEn ? $"User removed from the database: {idOrIP}" : $"В базе данных удален пользователь : {idOrIP}");
            });
        }
        private Int32 maxDetectedVpn = 1;
		   		 		  						  	   		  		 			  	   		  		  		   		 
        private void LoadDataFiles(TypeAction type)
        {
            String[] players = PlayerInfo.GetFiles(type);
            foreach (String player in players)
                PlayerInfo.Load(player, type);

            String Message = type == TypeAction.BannedSteamID
                ? (LanguageEn ? $"Database initialized with {players.Length} banned players" : $"Инициализирована база данных {players.Length} забаненных игроков")
                : (LanguageEn ? $"Database initialized with {players.Length} banned IPs" : $"Инициализирована база данных {players.Length} забаненных IP-адресов");
            
            Puts(Message);
        }

        private void BanUser(UInt64 userID, IPlayer iPlayer = null, String reason = default, Double banTime = 0, BasePlayer ownerCommand = null, Boolean skipTeam = false)
        {
            if (!userID.IsSteamId()) return;

            Object canBanPlayer = Interface.Oxide.CallHook("CanBanPlayer", userID, reason, banTime, ownerCommand); 
            if (canBanPlayer != null)
            {
                switch (canBanPlayer)
                {
                    case String player when !string.IsNullOrWhiteSpace(player):
                        Puts($"{canBanPlayer}");
                        return;
                    case Boolean value when !value:
                        return;
                }
            }
            
            String idPlayer = userID.ToString();
            
            BanUserOrIp(idPlayer, iPlayer?.Name, iPlayer?.Address, reason, banTime, TypeAction.BannedSteamID, ownerCommand, skipTeam);
        }
        private const Boolean LanguageEn = false;
        private readonly Dictionary<String, ProcessCheckVPN> ipListChecked = new Dictionary<String, ProcessCheckVPN>();
        private const String permissionIgnoreBan = "iqbansystem.ignoreban";
        private Boolean useProxyCheck = false;
        
        void OnTeamDisbanded(RelationshipManager.PlayerTeam team)
        {
            foreach (UInt64 teamMember in team.members)
                LeaveTeamPlayer(teamMember);
        }
		   		 		  						  	   		  		 			  	   		  		  		   		 
        private Timer timerUpdateMarkers = null;

        public class Authors
        {
            public String name { get; set; }
            public String url { get; set; }
            public String icon_url { get; set; }
            public String proxy_icon_url { get; set; }
            public Authors(String name, String url, String icon_url, String proxy_icon_url)
            {
                this.name = name;
                this.url = url;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        
        
        private void InserOrUpdateDatabase(String idOrIP, TypeAction typeBanned, IPlayer mbPlayer = null)
        {
            if (sqlConnection == null) return;

            PlayerInfo playerInfo = PlayerInfo.Get(idOrIP, typeBanned);
            if (playerInfo == null) return;
            
            String ipAddress = typeBanned == TypeAction.BannedIP ? idOrIP : mbPlayer?.Address ?? "None";
            String userID = typeBanned == TypeAction.BannedSteamID ? idOrIP : mbPlayer?.Id ?? "None";
            
            String jsonNameHistory = JsonConvert.SerializeObject(playerInfo.nameHistory, Formatting.None);
            String jsonIpHistory = JsonConvert.SerializeObject(playerInfo.ipHistory, Formatting.None);
            String jsonSteamHistory = JsonConvert.SerializeObject(playerInfo.steamIdHistory, Formatting.None);
            
            String sqlQueryUpdate = SQL_Query_UpdateUser(typeBanned);
            Sql updateCommand = Oxide.Core.Database.Sql.Builder.Append(sqlQueryUpdate, userID, ipAddress, 
                playerInfo.permanent, playerInfo.timeUnbanned, playerInfo.reason, playerInfo.serverName, playerInfo.serverAdress, playerInfo.owner, jsonNameHistory, jsonIpHistory, jsonSteamHistory);
        
            sqlLibrary.Update(updateCommand, sqlConnection, rowsAffected =>
            {
                if (rowsAffected <= 0)
                {
                    String sqlQueryInsert = SQL_Query_InsertUser();
                    Sql insertCommand = Oxide.Core.Database.Sql.Builder.Append(sqlQueryInsert, userID, ipAddress, 
                        playerInfo.permanent, playerInfo.timeUnbanned, playerInfo.reason, playerInfo.serverName, playerInfo.serverAdress, playerInfo.owner, jsonNameHistory, jsonIpHistory, jsonSteamHistory);
                    
                    sqlLibrary.Insert(insertCommand, sqlConnection, rowsAffecteds =>
                    {
                        Puts(LanguageEn ? $"New user added to the database: {userID}/{ipAddress}" : $"В базу данных добавлен новый пользователь : {userID}/{ipAddress}");
                    });
                }
                else Puts(LanguageEn ? $"User data updated in the database: {userID}/{ipAddress}" : $"В базе данных обновлены данные о пользователе : {userID}/{ipAddress}");
            });
        }
        
            }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    

