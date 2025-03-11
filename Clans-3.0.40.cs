using CompanionServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

using VersionNumber = Oxide.Core.VersionNumber;

namespace Oxide.Plugins
{
    [Info("Clans", "k1lly0u", "3.0.40")]
    class Clans : RustPlugin
    {
        #region Fields
        [PluginReference] private Plugin DiscordClans;

        internal StoredData storedData;

        private bool wipeData;
        private bool isInitialized = false;
        private Coroutine initClansRoutine;

        private Regex tagFilter;
        private Regex hexFilter;

        private int[] customTagMinValue;
        private int[] customTagMaxValue;

        private HashSet<ulong> friendlyFireDisabled = new HashSet<ulong>();

        public static Clans Instance { get; private set; }

        private static DateTime Epoch = new DateTime(1970, 1, 1);
        private static double MaxUnixSeconds = (DateTime.MaxValue - Epoch).TotalSeconds;

        private const string COLORED_LABEL = "<color={0}>{1}</color>";

        private enum MessageType { Create, Invite, InviteReject, InviteWithdrawn, Join, Leave, Kick, Promote, Demote, Disband, AllianceInvite, AllianceInviteReject, AllianceInviteWithdrawn, AllianceAccept, AllianceWithdrawn, TeamChat, ClanChat, AllyChat }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            Instance = this;

            permission.RegisterPermission(configData.Permissions.PermissionCreate, this);
            permission.RegisterPermission(configData.Permissions.PermissionJoin, this);
            permission.RegisterPermission(configData.Permissions.PermissionLeave, this);
            permission.RegisterPermission(configData.Permissions.PermissionDisband, this);
            permission.RegisterPermission(configData.Permissions.PermissionKick, this);
            permission.RegisterPermission(configData.Permissions.ClanInfoPermission, this);

            lang.RegisterMessages(Messages, this);

            cmd.AddChatCommand(configData.Commands.FFCommand, this, cmdClanFF);
            cmd.AddChatCommand(configData.Commands.AFFCommand, this, cmdAllyFF);
            cmd.AddChatCommand(configData.Commands.ClanCommand, this, cmdChatClan);
            cmd.AddChatCommand(configData.Commands.AllyChatCommand, this, cmdAllianceChat);
            cmd.AddChatCommand(configData.Commands.ClanChatCommand, this, cmdClanChat);
            cmd.AddChatCommand(configData.Commands.ClanInfoCommand, this, cmdChatClanInfo);
            cmd.AddChatCommand(configData.Commands.ClanHelpCommand, this, cmdChatClanHelp);
            cmd.AddChatCommand(configData.Commands.ClanAllyCommand, this, cmdChatClanAlly);

            _tags.Add(new KeyValuePair<string, string>(configData.Tags.TagOpen, configData.Tags.TagClose));

            tagFilter = new Regex($"[^a-zA-Z0-9{configData.Tags.AllowedCharacters}]");
            hexFilter = new Regex("^([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");

            SetMinMaxColorRange();

            if (!configData.Tags.EnabledBC)
                Unsubscribe(nameof(OnPluginLoaded));

            if (!configData.Clans.MemberFF && !configData.Clans.Alliance.AllyFF)
                Unsubscribe(nameof(OnEntityTakeDamage));

            if (configData.Clans.Teams.Enabled)
                RelationshipManager.maxTeamSize = configData.Clans.MemberLimit;

            LoadData();
        }

        private void OnServerInitialized()
        {
            initClansRoutine = ServerMgr.Instance.StartCoroutine(InitializeClans());
        }

        private void OnNewSave(string str) => wipeData = configData.Purge.WipeOnNewSave;

        private void OnServerSave() => SaveData();

        private void OnPluginLoaded(Plugin plugin)
        {
            if (configData.Tags.EnabledBC && plugin?.Title == "Better Chat")
                Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<Oxide.Core.Libraries.Covalence.IPlayer, string>(BetterChat_FormattedClanTag));
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.currentTeam != 0UL)
            {
                if (configData.Clans.Teams.Enabled)
                {
                    player.ClearTeam();
                    RelationshipManager.ServerInstance.playerToTeam.Remove(player.userID);
                }
                else
                {
                    RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    if (playerTeam == null || !playerTeam.members.Contains(player.userID))
                    {
                        player.ClearTeam();
                        RelationshipManager.ServerInstance.playerToTeam.Remove(player.userID);
                    }
                }
            }

            Clan clan = storedData?.FindClanByID(player.userID);
            if (clan != null)
            {
                clan.OnPlayerConnected(player);
            }
            else
            {
                List<string> invites;
                if (storedData.playerInvites.TryGetValue(player.userID, out invites))
                {
                    player.ChatMessage(string.Format(msg("Notification.PendingInvites", player.UserIDString), invites.ToSentence(), configData.Commands.ClanCommand));
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player) => storedData?.FindClanByID(player.userID)?.OnPlayerDisconnected(player);

        private object OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
        {
            if (channel != ConVar.Chat.ChatChannel.Team)
                return null;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
                return null;

            if (configData.Options.DenyOnMuted)
            {
                object success = Interface.CallHook("API_IsMuted", player.IPlayer);
                if ((success is bool && (bool)success) || player.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute))
                {
                    player.ChatMessage(msg("Chat.IsMuted", player.UserIDString));
                    return false;
                }
            }

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} : {message}", (int)MessageType.TeamChat);

            Interface.CallHook("OnClanChat", player, message, clan.Tag);
            return null;
        }

        private object OnTeamCreate(BasePlayer player)
        {
            if (!configData.Clans.Teams.Enabled)
                return null;

            player.ChatMessage(msg("Notification.Create.NoNativeCreate", player.UserIDString));
            return true;
        }

        private object OnTeamKick(RelationshipManager.PlayerTeam playerTeam, BasePlayer player, ulong targetId)
        {
            if (!configData.Clans.Teams.Enabled || string.IsNullOrEmpty(playerTeam.teamName))
                return null;

            if (!configData.Clans.Teams.AllowKick)
                return false;

            KickPlayer(player, targetId);
            return true;
        }

        private object OnTeamInvite(BasePlayer player, BasePlayer other)
        {
            if (!configData.Clans.Teams.Enabled)
                return null;

            if (!configData.Clans.Teams.AllowInvite)
                return false;

            if (other.IsNpc || other != null && !other.userID.IsSteamId())
                return false;

            InvitePlayer(player, other);
            return true;
        }

        private object OnTeamAcceptInvite(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
        {
            if (!configData.Clans.Teams.Enabled || string.IsNullOrEmpty(playerTeam.teamName))
                return null;

            JoinClan(player, playerTeam.teamName);
            return true;
        }

        private object OnTeamRejectInvite(BasePlayer player, RelationshipManager.PlayerTeam playerTeam)
        {
            if (!configData.Clans.Teams.Enabled || string.IsNullOrEmpty(playerTeam.teamName))
                return null;

            RejectInvite(player, playerTeam.teamName);
            return true;
        }

        private object OnTeamLeave(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
        {
            if (!configData.Clans.Teams.Enabled || string.IsNullOrEmpty(playerTeam.teamName))
                return null;

            if (!configData.Clans.Teams.AllowLeave)
                return false;

            LeaveClan(player);
            return true;
        }

        private object OnTeamPromote(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
        {
            if (!configData.Clans.Teams.Enabled || string.IsNullOrEmpty(playerTeam.teamName))
                return null;

            if (!configData.Clans.Teams.AllowPromote)
                return false;

            PromotePlayer(BasePlayer.FindByID(playerTeam.teamLeader), player.userID);
            return true;
        }

        private object OnTeamDisband(RelationshipManager.PlayerTeam playerTeam)
        {
            if (!configData.Clans.Teams.Enabled || string.IsNullOrEmpty(playerTeam.teamName))
                return null;

            Clan clan = storedData.FindClan(playerTeam.teamName);
            if (clan != null)
                clan.DisbandClan();

            return true;
        }

        private object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (!player || !info?.InitiatorPlayer)
                return null;

            if (player == info.InitiatorPlayer)
                return null;

            Clan victimClan = storedData.FindClanByID(player.userID);
            if (victimClan == null)
                return null;

            Clan attackerClan = storedData.FindClanByID(info.InitiatorPlayer.userID);
            if (attackerClan == null)
                return null;

            Clan.Member member = storedData.FindMemberByID(info.InitiatorPlayer.userID);
            if (member == null)
                return null;

            if (friendlyFireDisabled.Contains(player.userID))
                return null;

            if (victimClan.Tag.Equals(attackerClan.Tag) && configData.Clans.MemberFF && member.MemberFFEnabled)
            {
                member.OnClanMemberHit(string.Format(COLORED_LABEL, victimClan.GetRoleColor(player.userID), player.displayName));
                return true;
            }

            if (victimClan.IsAlliedClan(attackerClan.Tag) && configData.Clans.Alliance.Enabled && configData.Clans.Alliance.AllyFF && member.AllyFFEnabled)
            {
                member.OnAllyMemberHit(string.Format(COLORED_LABEL, victimClan.GetRoleColor(player.userID), player.displayName));
                return true;
            }

            return null;
        }

        private void Unload()
        {
            if (isInitialized)
            {
                SaveData();

                foreach (Clan clan in storedData.clans.Values)
                    clan.OnUnload();
            }
            else
            {
                if (initClansRoutine != null)
                    ServerMgr.Instance.StopCoroutine(initClansRoutine);
            }

            configData = null;
            Instance = null;
        }
        #endregion

        #region Functions
        private IEnumerator InitializeClans()
        {
            Puts("Initializing Clans...");

            if (configData.Clans.Teams.Enabled)
            {
                RelationshipManager.ServerInstance.playerToTeam.Clear();

                foreach (KeyValuePair<ulong, RelationshipManager.PlayerTeam> kvp in RelationshipManager.ServerInstance.teams)
                {
                    RelationshipManager.PlayerTeam playerTeam = kvp.Value;
                    ClearTeam(ref playerTeam);
                }

                RelationshipManager.ServerInstance.teams.Clear();
                RelationshipManager.ServerInstance.lastTeamIndex = 1;
            }

            if (wipeData)
            {
                storedData.clans.Clear();
                storedData.playerInvites.Clear();
                SaveData();
            }
            else
            {
                List<string> purgedClans = Facepunch.Pool.Get<List<string>>();

                foreach (KeyValuePair<string, Clan> kvp in storedData.clans)
                {
                    Clan clan = kvp.Value;

                    if (clan.ClanMembers.Count == 0 || (configData.Purge.Enabled && UnixTimeStampUTC() - clan.LastOnlineTime > (configData.Purge.OlderThanDays * 86400)))
                    {
                        purgedClans.Add(kvp.Key);
                        continue;
                    }

                    clan.Description = StripHTMLTags(clan.Description);

                    if (configData.Clans.Alliance.Enabled)
                    {
                        for (int i = clan.AllianceInvites.Count - 1; i >= 0; i--)
                        {
                            KeyValuePair<string, double> allianceInvite = clan.AllianceInvites.ElementAt(i);

                            if (!storedData.clans.ContainsKey(allianceInvite.Key) || (UnixTimeStampUTC() - allianceInvite.Value > configData.Clans.Invites.AllianceInviteExpireTime))
                                clan.AllianceInvites.Remove(allianceInvite.Key);
                        }

                        for (int i = clan.Alliances.Count - 1; i >= 0; i--)
                        {
                            string allyTag = clan.Alliances.ElementAt(i);

                            if (!storedData.clans.ContainsKey(allyTag))
                                clan.Alliances.Remove(allyTag);
                        }
                    }

                    for (int i = clan.MemberInvites.Count - 1; i >= 0; i--)
                    {
                        KeyValuePair<ulong, Clan.MemberInvite> memberInvite = clan.MemberInvites.ElementAt(i);

                        if (UnixTimeStampUTC() - memberInvite.Value.ExpiryTime > configData.Clans.Invites.MemberInviteExpireTime)
                            clan.MemberInvites.Remove(memberInvite.Key);
                    }

                    foreach (KeyValuePair<ulong, Clan.Member> member in clan.ClanMembers)
                        storedData.RegisterPlayer(member.Key, clan.Tag);

                    if (configData.Permissions.PermissionGroups)
                        permission.CreateGroup(configData.Permissions.PermissionGroupPrefix + clan.Tag, "Clan " + clan.Tag, 0);

                    yield return null;
                }

                if (purgedClans.Count > 0)
                {
                    Puts($"Purging {purgedClans.Count} expired or invalid clans");

                    StringBuilder str = new StringBuilder();

                    for (int i = 0; i < purgedClans.Count; i++)
                    {
                        string tag = purgedClans[i];
                        Clan clan = storedData.clans[tag];
                        if (clan == null)
                            continue;

                        permission.RemoveGroup(configData.Permissions.PermissionGroupPrefix + clan.Tag);

                        str.Append($"{(i > 0 ? "\n" : "")}Purged - [{tag}] | {clan.Description} | Owner: {clan.OwnerID} | Last Online: {UnixTimeStampToDateTime(clan.LastOnlineTime)}");

                        storedData.clans.Remove(tag);
                    }

                    if (configData.Purge.ListPurgedClans)
                    {
                        Puts(str.ToString());

                        if (configData.Options.LogChanges)
                            LogToFile(Title, str.ToString(), this);
                    }
                }

                Puts($"Loaded {storedData.clans.Count} clans!");

                Facepunch.Pool.FreeUnmanaged(ref purgedClans);
            }

            if (configData.Tags.EnabledBC)
                Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<Oxide.Core.Libraries.Covalence.IPlayer, string>(BetterChat_FormattedClanTag));

            isInitialized = true;
            initClansRoutine = null;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }
        #endregion

        #region Clan Tag Colors
        private void SetMinMaxColorRange()
        {
            customTagMinValue = new int[3];
            customTagMaxValue = new int[3];

            if (configData.Tags.CustomTagColorMin.StartsWith("#"))
                configData.Tags.CustomTagColorMin = configData.Tags.CustomTagColorMin.Substring(1);

            if (configData.Tags.CustomTagColorMax.StartsWith("#"))
                configData.Tags.CustomTagColorMax = configData.Tags.CustomTagColorMax.Substring(1);

            customTagMinValue[0] = int.Parse(configData.Tags.CustomTagColorMin.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            customTagMinValue[1] = int.Parse(configData.Tags.CustomTagColorMin.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            customTagMinValue[2] = int.Parse(configData.Tags.CustomTagColorMin.Substring(3, 2), NumberStyles.AllowHexSpecifier);

            customTagMaxValue[0] = int.Parse(configData.Tags.CustomTagColorMax.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            customTagMaxValue[1] = int.Parse(configData.Tags.CustomTagColorMax.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            customTagMaxValue[2] = int.Parse(configData.Tags.CustomTagColorMax.Substring(3, 2), NumberStyles.AllowHexSpecifier);
        }

        private bool TagColorIsBlocked(string color)
        {
            if (color.StartsWith("#"))
                color = color.Substring(1);

            if (configData.Tags.BlockedTagColors.Contains(color, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private bool TagColorWithinRange(string color)
        {
            if (color.StartsWith("#"))
                color = color.Substring(1);

            int red = int.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            return red >= customTagMinValue[0] && red <= customTagMaxValue[0] &&
                   green >= customTagMinValue[1] && green <= customTagMaxValue[1] &&
                   blue >= customTagMinValue[2] && blue <= customTagMaxValue[2];
        }


        #endregion

        #region Helpers
        private string BetterChat_FormattedClanTag(Oxide.Core.Libraries.Covalence.IPlayer player)
        {
            Clan clan = storedData.FindClanByID(player.Id);
            if (clan == null)
                return string.Empty;

            string tagColor = string.IsNullOrEmpty(clan.TagColor) || !configData.Tags.CustomColors ? configData.Tags.TagColor : clan.TagColor;

            if (configData.Tags.EnabledGroupColors && (string.IsNullOrEmpty(clan.TagColor) || !configData.Tags.CustomColors))
            {
                foreach (KeyValuePair<string, string> kvp in configData.Tags.GroupTagColors)
                {
                    if (player.BelongsToGroup(kvp.Key))
                    {
                        tagColor = kvp.Value;
                        break;
                    }
                }
            }

            return $"[#{tagColor.Replace("#", "")}][+{configData.Tags.TagSize}]{configData.Tags.TagOpen}{clan.Tag}{configData.Tags.TagClose}[/+][/#]";
        }

        private static int UnixTimeStampUTC() => (int)DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds
                ? Epoch.AddMilliseconds(unixTimeStamp)
                : Epoch.AddSeconds(unixTimeStamp);
        }

        private bool ContainsBlockedWord(string tag)
        {
            for (int i = 0; i < configData.Tags.BlockedWords.Length; i++)
            {
                if (TranslateLeet(tag).ToLower().Contains(configData.Tags.BlockedWords[i].ToLower()))
                    return true;
            }
            return false;
        }

        private string TranslateLeet(string original)
        {
            string translated = original;

            foreach (KeyValuePair<string, string> leet in leetTable)
                translated = translated.Replace(leet.Key, leet.Value);
            return translated;
        }

        private bool ClanTagExists(string tag)
        {
            ICollection<string> collection = storedData.clans.Keys;
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection.ElementAt(i).Equals(tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            int days = dateDifference.Days;
            int hours = dateDifference.Hours + (days * 24);

            int mins = dateDifference.Minutes;
            int secs = dateDifference.Seconds;

            if (hours > 0)
                return $"{hours:00}h:{mins:00}m:{secs:00}s";
            if (mins > 0)
                return $"{mins:00}m:{secs:00}s";
            return $"{secs:00}s";
        }

        private BasePlayer FindPlayer(string partialNameOrID) => BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.displayName.Equals(partialNameOrID, StringComparison.OrdinalIgnoreCase)) ??
                                                                 BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.displayName.Contains(partialNameOrID, CompareOptions.OrdinalIgnoreCase)) ??
                                                                 BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.UserIDString == partialNameOrID);

        private static void RemoveFromTeam(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
        {
            playerTeam.members.Remove(player.userID);
            RelationshipManager.ServerInstance.playerToTeam.Remove(player.userID);

            player.ClearTeam();
        }

        private static void RemoveFromTeam(RelationshipManager.PlayerTeam playerTeam, ulong playerId)
        {
            playerTeam.members.Remove(playerId);
            RelationshipManager.ServerInstance.playerToTeam.Remove(playerId);

            RelationshipManager.FindByID(playerId)?.ClearTeam();
        }

        private static void ClearTeam(ref RelationshipManager.PlayerTeam playerTeam)
        {
            playerTeam.invites.Clear();
            playerTeam.members.Clear();
            playerTeam.onlineMemberConnections.Clear();
            playerTeam.teamID = 0UL;
            playerTeam.teamLeader = 0UL;
            playerTeam.teamName = string.Empty;

            Facepunch.Pool.Free(ref playerTeam);
        }

        private static string RemoveTags(string str)
        {
            foreach (KeyValuePair<string, string> kvp in _tags)
            {
                if (str.StartsWith(kvp.Key) && str.Contains(kvp.Value) && str.Length > str.IndexOf(kvp.Value))
                {
                    str = str.Substring(str.IndexOf(kvp.Value)).Trim();
                }
            }
            return str;
        }

        private static List<KeyValuePair<string, string>> _tags = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("[", "]"),
            new KeyValuePair<string, string>("{", "}"),
            new KeyValuePair<string, string>("(", ")"),
            new KeyValuePair<string, string>("<", ">"),
        };

        public static string StripHTMLTags(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
        #endregion

        #region Clan Management        
        internal void CreateClan(BasePlayer player, string tag, string description)
        {
            if (!player)
                return;

            if (storedData.FindClanByID(player.userID) != null)
            {
                player.ChatMessage(msg("Notification.Create.InExistingClan", player.UserIDString));
                return;
            }

            if (configData.Permissions.UsePermissionCreate && !permission.UserHasPermission(player.UserIDString, configData.Permissions.PermissionCreate))
            {
                player.ChatMessage(msg("Notification.Create.NoPermission", player.UserIDString));
                return;
            }

            List<ulong> list;
            if (configData.Tags.ReservedClanTags.TryGetValue(tag, out list))
            {
                if (!list.Contains(player.userID))
                {
                    player.ChatMessage(string.Format(msg("Notification.Create.TagReserved1", player.UserIDString), tag));
                    return;
                }
            }

            if (tag.Length < configData.Tags.TagLength.Minimum || tag.Length > configData.Tags.TagLength.Maximum)
            {
                player.ChatMessage(string.Format(msg("Notification.Create.InvalidTagLength", player.UserIDString), configData.Tags.TagLength.Minimum, configData.Tags.TagLength.Maximum));
                return;
            }

            if (tagFilter.IsMatch(tag) || ContainsBlockedWord(tag))
            {
                player.ChatMessage(msg("Notification.Create.InvalidCharacters", player.UserIDString));
                return;
            }

            if (ClanTagExists(tag))
            {
                player.ChatMessage(msg("Notification.Create.ClanExists", player.UserIDString));
                return;
            }

            if (configData.Clans.Teams.Enabled && player.currentTeam != 0UL)
            {
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (playerTeam != null)
                    RemoveFromTeam(playerTeam, player);

                RelationshipManager.ServerInstance.playerToTeam.Remove(player.userID);
                player.ClearTeam();
            }

            storedData.clans[tag] = new Clan(player, tag, description);
            storedData.RegisterPlayer(player.userID, tag);

            player.ChatMessage(string.Format(msg("Notification.Create.Success", player.UserIDString), tag));

            Interface.CallHook("OnClanCreate", tag);

            if (configData.Options.LogChanges)
                LogToFile(Title, $"{player.displayName} created the clan [{tag}]", this);

            DiscordClans?.CallHook("LogMessage", $"{player.displayName} has created the clan {tag}", (int)MessageType.Create);
        }

        internal bool InvitePlayer(BasePlayer inviter, ulong targetId)
        {
            BasePlayer invitee = (covalence.Players.FindPlayerById(targetId.ToString())?.Object as BasePlayer);
            if (!invitee)
            {
                inviter.ChatMessage(string.Format(msg("Notification.Generic.UnableToFindPlayer", inviter.UserIDString), targetId));
                return false;
            }

            return InvitePlayer(inviter, invitee);
        }

        internal bool InvitePlayer(BasePlayer inviter, BasePlayer invitee)
        {
            if (!inviter || !invitee)
                return false;

            if (invitee.IsNpc || !invitee.userID.IsSteamId())
                return false;

            Clan clan = storedData.FindClanByID(inviter.userID);
            if (clan == null)
            {
                inviter.ChatMessage(msg("Notification.Generic.NoClan", inviter.UserIDString));
                return false;
            }

            Clan other = storedData.FindClanByID(invitee.userID);
            if (other != null)
            {
                inviter.ChatMessage(string.Format(msg("Notification.Invite.InClan", inviter.UserIDString), invitee.displayName));
                return false;
            }

            if (configData.Permissions.UsePermissionJoin && !permission.UserHasPermission(invitee.UserIDString, configData.Permissions.PermissionJoin))
            {
                inviter.ChatMessage(msg("Notification.Invite.NoPermission", inviter.UserIDString));
                return false;
            }

            return clan.InvitePlayer(inviter, invitee);
        }

        internal bool WithdrawInvite(BasePlayer player, string partialNameOrID)
        {
            if (!player)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            if (!clan.IsOwner(player.userID) && !clan.IsModerator(player.userID) && !clan.IsCouncil(player.userID))
            {
                player.ChatMessage(msg("Notification.WithdrawInvite.NoPermissions", player.UserIDString));
                return false;
            }

            ulong targetId;
            if (!ulong.TryParse(partialNameOrID, out targetId))
                targetId = 0UL;

            foreach (KeyValuePair<ulong, Clan.MemberInvite> invite in clan.MemberInvites)
            {
                if ((targetId != 0UL && targetId.Equals(invite.Key)) || invite.Value.DisplayName.Contains(partialNameOrID))
                {
                    storedData.RevokePlayerInvite(targetId, clan.Tag);

                    clan.MemberInvites.Remove(invite.Key);
                    clan.LocalizedBroadcast("Notification.WithdrawInvite.Success", player.displayName, invite.Value.DisplayName);

                    DiscordClans?.CallHook("LogMessage", $"{player.displayName} has withdrawn a clan invite for {invite.Value.DisplayName}", (int)MessageType.InviteWithdrawn);
                    return true;
                }
            }

            if (configData.Clans.Teams.Enabled)
                player.ClearPendingInvite();

            player.ChatMessage(string.Format(msg("Notification.WithdrawInvite.UnableToFind", player.UserIDString), partialNameOrID));
            return false;
        }

        internal bool RejectInvite(BasePlayer player, string tag)
        {
            if (!player)
                return false;

            Clan clan = storedData.FindClan(tag);
            if (clan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), tag));
                return false;
            }

            // Update tag from found clan for case insensitive selection
            tag = clan.Tag;

            if (!clan.MemberInvites.ContainsKey(player.userID))
            {
                player.ChatMessage(string.Format(msg("Notification.RejectInvite.InvalidInvite", player.UserIDString), tag));
                return false;
            }

            clan.MemberInvites.Remove(player.userID);

            storedData.OnInviteRejected(player.userID, tag);

            if (configData.Clans.Teams.Enabled)
                clan.PlayerTeam.RejectInvite(player);

            clan.LocalizedBroadcast("Notification.RejectInvite.Message", player.displayName);
            player.ChatMessage(string.Format(msg("Notification.RejectInvite.PlayerMessage", player.UserIDString), tag));

            if (configData.Options.LogChanges)
                Instance.LogToFile(Instance.Title, $"{player.displayName} rejected their invite to [{tag}]", Instance);

            return true;
        }

        internal bool JoinClan(BasePlayer player, string tag)
        {
            if (!player || string.IsNullOrEmpty(tag))
                return false;

            if (configData.Permissions.UsePermissionJoin && !permission.UserHasPermission(player.UserIDString, configData.Permissions.PermissionJoin))
            {
                player.ChatMessage(msg("Notification.Join.NoPermission", player.UserIDString));
                return false;
            }

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan != null)
            {
                player.ChatMessage(msg("Notification.Join.InExistingClan", player.UserIDString));
                return false;
            }

            clan = storedData.FindClan(tag);
            if (clan == null)
                return false;

            return clan.JoinClan(player);
        }

        internal bool LeaveClan(BasePlayer player)
        {
            if (!player)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            if (configData.Permissions.UsePermissionLeave && !permission.UserHasPermission(player.UserIDString, configData.Permissions.PermissionLeave))
            {
                player.ChatMessage(msg("Notification.Leave.NoPermission", player.UserIDString));
                return false;
            }

            return clan.LeaveClan(player);
        }

        internal bool KickPlayer(BasePlayer player, ulong playerId)
        {
            if (!player)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            if (configData.Permissions.UsePermissionKick && !permission.UserHasPermission(player.UserIDString, configData.Permissions.PermissionKick))
            {
                player.ChatMessage(msg("Notification.Kick.NoPermission", player.UserIDString));
                return false;
            }

            return clan.KickMember(player, playerId);
        }

        internal bool PromotePlayer(BasePlayer promoter, ulong targetId)
        {
            if (!promoter)
                return false;

            Clan clan = storedData.FindClanByID(promoter.userID);
            if (clan == null)
            {
                promoter.ChatMessage(msg("Notification.Generic.NoClan", promoter.UserIDString));
                return false;
            }

            Clan other = storedData.FindClanByID(targetId);
            if (other == null || !clan.Tag.Equals(other.Tag))
            {
                string displayName = covalence.Players.FindPlayer(targetId.ToString())?.Name ?? targetId.ToString();

                promoter.ChatMessage(string.Format(msg("Notification.Promotion.TargetNoClan", promoter.UserIDString), displayName));
                return false;
            }

            return clan.PromotePlayer(promoter, targetId);
        }

        internal bool DemotePlayer(BasePlayer demoter, ulong targetId)
        {
            if (!demoter)
                return false;

            Clan clan = storedData.FindClanByID(demoter.userID);
            if (clan == null)
            {
                demoter.ChatMessage(msg("Notification.Generic.NoClan", demoter.UserIDString));
                return false;
            }

            Clan other = storedData.FindClanByID(targetId);
            if (other == null || !clan.Tag.Equals(other.Tag))
            {
                string displayName = covalence.Players.FindPlayer(targetId.ToString())?.Name ?? targetId.ToString();

                demoter.ChatMessage(string.Format(msg("Notification.Promotion.TargetNoClan", demoter.UserIDString), displayName));
                return false;
            }

            return clan.DemotePlayer(demoter, targetId);
        }

        internal bool DisbandClan(BasePlayer player)
        {
            Clan clan = storedData.FindClanByID(player.userID);

            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            if (!clan.IsOwner(player.userID))
            {
                player.ChatMessage(msg("Notification.Disband.NotOwner", player.UserIDString));
                return false;
            }

            if (configData.Permissions.UsePermissionDisband && !permission.UserHasPermission(player.UserIDString, configData.Permissions.PermissionDisband))
            {
                player.ChatMessage(msg("Notification.Disband.NoPermission", player.UserIDString));
                return false;
            }

            string tag = clan.Tag;

            clan.LocalizedBroadcast("Notification.Disband.Message", Array.Empty<object>());
            clan.DisbandClan();

            player.ChatMessage(string.Format(msg("Notification.Disband.Success", player.UserIDString), tag));

            return true;
        }
        #endregion

        #region Alliance Management
        internal bool OfferAlliance(BasePlayer player, string tag)
        {
            if (!configData.Clans.Alliance.Enabled)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            Clan alliedClan = storedData.FindClan(tag);
            if (alliedClan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), tag));
                return false;
            }

            // Update tag from found clan for case insensitive selection
            tag = alliedClan.Tag;

            if (!clan.IsOwner(player.userID) && !clan.IsCouncil(player.userID))
            {
                player.ChatMessage(msg("Notification.Alliance.NoPermissions", player.UserIDString));
                return false;
            }

            if (clan.AllianceInvites.ContainsKey(tag) && (UnixTimeStampUTC() - clan.AllianceInvites[tag] < configData.Clans.Invites.AllianceInviteExpireTime))
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.PendingInvite", player.UserIDString), tag));
                return false;
            }

            if (clan.AllianceInviteCount >= configData.Clans.Invites.AllianceInviteLimit)
            {
                player.ChatMessage(msg("Notification.Alliance.MaximumInvites", player.UserIDString));
                return false;
            }

            if (clan.AllianceCount >= configData.Clans.Alliance.AllianceLimit)
            {
                player.ChatMessage(msg("Notification.Alliance.MaximumAlliances", player.UserIDString));
                return false;
            }

            if (configData.Clans.Alliance.CountAllianceMembers)
            {
                int count = clan.CountMembersAndAlliances();

                if (count + alliedClan.MemberCount >= configData.Clans.MemberLimit)
                {
                    player.ChatMessage(msg("Notification.Alliance.AtLimitAlliedMembersSelf", player.UserIDString));
                    return false;
                }

                count = alliedClan.CountMembersAndAlliances();

                if (count + clan.MemberCount >= configData.Clans.MemberLimit)
                {
                    player.ChatMessage(string.Format(msg("Notification.Alliance.AtLimitAlliedMembersTarget", player.UserIDString), alliedClan.Tag));
                    return false;
                }
            }

            clan.AllianceInvites[tag] = UnixTimeStampUTC();
            alliedClan.IncomingAlliances.Add(clan.Tag);

            player.ChatMessage(string.Format(msg("Notification.Alliance.InviteSent", player.UserIDString), tag, FormatTime(configData.Clans.Invites.AllianceInviteExpireTime)));

            alliedClan.LocalizedBroadcast("Notification.Alliance.InviteReceived", clan.Tag, FormatTime(configData.Clans.Invites.AllianceInviteExpireTime), configData.Commands.ClanAllyCommand);

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} has offered an alliance to [{tag}]", (int)MessageType.AllianceInvite);
            return true;
        }

        internal bool WithdrawAlliance(BasePlayer player, string tag)
        {
            if (!configData.Clans.Alliance.Enabled)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            Clan alliedClan = storedData.FindClan(tag);
            if (alliedClan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), tag));
                return false;
            }

            // Update tag from found clan for case insensitive selection
            tag = alliedClan.Tag;

            if (!clan.IsOwner(player.userID) && !clan.IsCouncil(player.userID))
            {
                player.ChatMessage(msg("Notification.Alliance.NoPermissions", player.UserIDString));
                return false;
            }

            if (!clan.AllianceInvites.ContainsKey(tag))
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.NoActiveInvite", player.UserIDString), tag));
                return false;
            }

            clan.AllianceInvites.Remove(tag);
            alliedClan.IncomingAlliances.Remove(clan.Tag);

            clan.LocalizedBroadcast("Notification.Alliance.WithdrawnClan", player.displayName, tag);
            alliedClan.LocalizedBroadcast("Notification.Alliance.WithdrawnTarget", clan.Tag);

            clan.MarkDirty();

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} has withdrawn their alliance offer to [{tag}]", (int)MessageType.AllianceInviteWithdrawn);
            return true;
        }

        internal bool AcceptAlliance(BasePlayer player, string tag)
        {
            if (!configData.Clans.Alliance.Enabled)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            Clan alliedClan = storedData.FindClan(tag);
            if (alliedClan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), tag));
                return false;
            }

            // Update tag from found clan for case insensitive selection
            tag = alliedClan.Tag;

            if (!clan.IsOwner(player.userID) && !clan.IsCouncil(player.userID))
            {
                player.ChatMessage(msg("Notification.Alliance.NoPermissions", player.UserIDString));
                return false;
            }

            bool noActiveInvite = !alliedClan.AllianceInvites.ContainsKey(clan.Tag);

            if ((UnixTimeStampUTC() - alliedClan.AllianceInvites[clan.Tag] > configData.Clans.Invites.AllianceInviteExpireTime))
            {
                alliedClan.AllianceInvites.Remove(clan.Tag);
                noActiveInvite = true;
            }

            if (noActiveInvite)
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.NoActiveInviteFrom", player.UserIDString), tag));
                return false;
            }

            if (alliedClan.AllianceCount >= configData.Clans.Alliance.AllianceLimit)
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.AtLimitTarget", player.UserIDString), tag));
                return false;
            }

            if (clan.AllianceCount >= configData.Clans.Alliance.AllianceLimit)
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.AtLimitSelf", player.UserIDString), tag));
                return false;
            }

            if (configData.Clans.Alliance.CountAllianceMembers)
            {
                int count = clan.CountMembersAndAlliances();

                if (count + alliedClan.MemberCount >= configData.Clans.MemberLimit)
                {
                    player.ChatMessage(msg("Notification.Alliance.AtLimitAlliedMembersSelf", player.UserIDString));
                    return false;
                }

                count = alliedClan.CountMembersAndAlliances();

                if (count + clan.MemberCount >= configData.Clans.MemberLimit)
                {
                    player.ChatMessage(string.Format(msg("Notification.Alliance.AtLimitAlliedMembersTarget", player.UserIDString), alliedClan.Tag));
                    return false;
                }
            }

            clan.Alliances.Add(tag);
            clan.IncomingAlliances.Remove(tag);

            alliedClan.Alliances.Add(clan.Tag);
            alliedClan.AllianceInvites.Remove(clan.Tag);

            clan.MarkDirty();
            alliedClan.MarkDirty();

            clan.LocalizedBroadcast("Notification.Alliance.Formed", clan.Tag, alliedClan.Tag);
            alliedClan.LocalizedBroadcast("Notification.Alliance.Formed", clan.Tag, alliedClan.Tag);

            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);
            Interface.Oxide.CallHook("OnClanUpdate", alliedClan.Tag);

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} has accepted a alliance offer from [{tag}]", (int)MessageType.AllianceAccept);

            return true;
        }

        internal bool RejectAlliance(BasePlayer player, string tag)
        {
            if (!configData.Clans.Alliance.Enabled)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            Clan alliedClan = storedData.FindClan(tag);
            if (alliedClan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), tag));
                return false;
            }

            // Update tag from found clan for case insensitive selection
            tag = alliedClan.Tag;

            if (!clan.IsOwner(player.userID) && !clan.IsCouncil(player.userID))
            {
                player.ChatMessage(msg("Notification.Alliance.NoPermissions", player.UserIDString));
                return false;
            }

            if (!alliedClan.AllianceInvites.ContainsKey(clan.Tag) || (UnixTimeStampUTC() - alliedClan.AllianceInvites[clan.Tag] > configData.Clans.Invites.AllianceInviteExpireTime))
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.NoActiveInvite", player.UserIDString), tag));
                return false;
            }

            clan.IncomingAlliances.Remove(tag);

            alliedClan.AllianceInvites.Remove(clan.Tag);
            alliedClan.MarkDirty();

            clan.LocalizedBroadcast("Notification.Alliance.Rejected", clan.Tag, alliedClan.Tag);
            alliedClan.LocalizedBroadcast("Notification.Alliance.Rejected", clan.Tag, alliedClan.Tag);

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} has rejected a alliance offer from [{tag}]", (int)MessageType.AllianceInviteReject);
            return true;
        }

        internal bool RevokeAlliance(BasePlayer player, string tag)
        {
            if (!configData.Clans.Alliance.Enabled)
                return false;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                return false;
            }

            Clan alliedClan = storedData.FindClan(tag);
            if (alliedClan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), tag));
                return false;
            }

            // Update tag from found clan for case insensitive selection
            tag = alliedClan.Tag;

            if (!clan.IsOwner(player.userID) && !clan.IsCouncil(player.userID))
            {
                player.ChatMessage(msg("Notification.Alliance.NoPermissions", player.UserIDString));
                return false;
            }

            if (!clan.Alliances.Contains(alliedClan.Tag))
            {
                player.ChatMessage(string.Format(msg("Notification.Alliance.NoActiveAlliance", player.UserIDString), alliedClan.Tag));
                return false;
            }

            alliedClan.Alliances.Remove(clan.Tag);
            clan.Alliances.Remove(alliedClan.Tag);

            alliedClan.MarkDirty();
            clan.MarkDirty();

            clan.LocalizedBroadcast("Notification.Alliance.Revoked", clan.Tag, alliedClan.Tag);
            alliedClan.LocalizedBroadcast("Notification.Alliance.Revoked", clan.Tag, alliedClan.Tag);

            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);
            Interface.Oxide.CallHook("OnClanUpdate", alliedClan.Tag);

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} has withdrawn their alliance to [{tag}]", (int)MessageType.AllianceWithdrawn);
            return true;
        }
        #endregion

        #region Chat
        private void ClanChat(BasePlayer player, string message)
        {
            if (!player)
                return;

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
                return;

            if (configData.Options.DenyOnMuted)
            {
                object success = Interface.CallHook("API_IsMuted", player.IPlayer);
                if ((success is bool && (bool)success) || player.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute))
                {
                    player.ChatMessage(msg("Chat.IsMuted", player.UserIDString));
                    return;
                }
            }

            message = message.Replace("\n", "").Replace("\r", "").Trim();

            if (message.Length > 128)
                message = message.Substring(0, 128);

            if (message.Length <= 0)
                return;

            string str = string.Format(msg("Chat.Alliance.Format"), clan.Tag, clan.GetRoleColor(player.userID), player.net.connection.username, message);

            clan.Broadcast(string.Format(msg("Chat.Clan.Prefix"), str));

            if (ConVar.Chat.serverlog)
            {
                ServerConsole.PrintColoured(ConsoleColor.White, "[CLAN] ", ConsoleColor.DarkYellow, player.displayName + ": ", ConsoleColor.DarkGreen, message);
                DebugEx.Log($"[CLAN CHAT] {str}");
            }

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} : {message}", (int)MessageType.ClanChat);

            Interface.CallHook("OnClanChat", player, message, clan.Tag);
        }

        private void AllianceChat(BasePlayer player, string message)
        {
            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
                return;

            if (configData.Options.DenyOnMuted)
            {
                object success = Interface.CallHook("API_IsMuted", player.IPlayer);
                if ((success is bool && (bool)success) || player.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute))
                {
                    player.ChatMessage(msg("Chat.IsMuted", player.UserIDString));
                    return;
                }
            }

            message = message.Replace("\n", "").Replace("\r", "").Trim();

            if (message.Length > 128)
                message = message.Substring(0, 128);

            if (message.Length <= 0)
                return;

            string str = string.Format(msg("Chat.Alliance.Format"), clan.Tag, clan.GetRoleColor(player.userID), player.net.connection.username, message);

            clan.Broadcast(string.Format(msg("Chat.Alliance.Prefix"), str));

            for (int i = 0; i < clan.AllianceCount; i++)
            {
                Clan alliedClan = storedData.FindClan(clan.Alliances.ElementAt(i));
                if (alliedClan != null)
                {
                    alliedClan.Broadcast(string.Format(msg("Chat.Alliance.Prefix"), str));
                }
            }

            if (ConVar.Chat.serverlog)
            {
                ServerConsole.PrintColoured(ConsoleColor.White, "[ALLY] ", ConsoleColor.DarkYellow, player.displayName + ": ", ConsoleColor.DarkGreen, message);
                DebugEx.Log($"[ALLY CHAT] {str}");
            }

            Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} : {message}", (int)MessageType.AllyChat);

            Interface.CallHook("OnAllianceChat", player, message, clan.Tag);
        }
        #endregion

        #region Chat Commands
        private void cmdAllianceChat(BasePlayer player, string command, string[] args)
        {
            if (!configData.Clans.Alliance.Enabled || args.Length == 0)
                return;

            AllianceChat(player, string.Join(" ", args));
        }

        private void cmdClanChat(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
                return;

            ClanChat(player, string.Join(" ", args));
        }

        private void cmdClanFF(BasePlayer player, string command, string[] args)
        {
            if (!configData.Clans.MemberFF)
                return;

            Clan.Member member = storedData.FindMemberByID(player.userID);
            if (member == null)
                return;

            if (configData.Clans.OwnerFF && member.Role >= Clan.Member.MemberRole.Moderator)
            {
                player.ChatMessage(msg("Notification.FF.ToggleNotOwner", player.UserIDString));
                return;
            }

            member.MemberFFEnabled = !member.MemberFFEnabled;

            if (configData.Clans.OwnerFF)
            {
                Clan clan = storedData.FindClanByID(player.userID);

                foreach (KeyValuePair<ulong, Clan.Member> kvp in clan.ClanMembers)
                {
                    if (kvp.Key.Equals(player.userID))
                        continue;

                    kvp.Value.MemberFFEnabled = member.MemberFFEnabled;

                    BasePlayer memberPlayer = kvp.Value.Player;

                    if (memberPlayer != null && memberPlayer.IsConnected)
                    {
                        memberPlayer.ChatMessage(string.Format(msg("Notification.FF.OwnerToggle", memberPlayer.UserIDString),
                            string.Format(COLORED_LABEL, clan.GetRoleColor(member.Role), player.displayName),
                            !kvp.Value.MemberFFEnabled ? msg("Notification.FF.MemberEnabled", memberPlayer.UserIDString) : msg("Notification.FF.MemberDisabled", memberPlayer.UserIDString)));
                    }
                }
            }

            player.ChatMessage(!member.MemberFFEnabled ? msg("Notification.FF.MemberEnabled", player.UserIDString) : msg("Notification.FF.MemberDisabled", player.UserIDString));
        }

        private void cmdAllyFF(BasePlayer player, string command, string[] args)
        {
            if (!configData.Clans.Alliance.AllyFF || !configData.Clans.Alliance.Enabled)
                return;

            Clan.Member member = storedData.FindMemberByID(player.userID);
            if (member == null)
                return;

            if (configData.Clans.Alliance.OwnerFF && member.Role >= Clan.Member.MemberRole.Moderator)
            {
                player.ChatMessage(msg("Notification.FF.ToggleNotOwner", player.UserIDString));
                return;
            }

            member.AllyFFEnabled = !member.AllyFFEnabled;

            if (configData.Clans.Alliance.OwnerFF)
            {
                Clan clan = storedData.FindClanByID(player.userID);

                foreach (KeyValuePair<ulong, Clan.Member> kvp in clan.ClanMembers)
                {
                    if (kvp.Key.Equals(player.userID))
                        continue;

                    kvp.Value.AllyFFEnabled = member.AllyFFEnabled;

                    BasePlayer memberPlayer = kvp.Value.Player;

                    if (memberPlayer != null && memberPlayer.IsConnected)
                    {
                        memberPlayer.ChatMessage(string.Format(msg("Notification.FF.OwnerAllyToggle", memberPlayer.UserIDString),
                            string.Format(COLORED_LABEL, clan.GetRoleColor(member.Role), player.displayName),
                            !kvp.Value.MemberFFEnabled ? msg("Notification.FF.MemberEnabled", memberPlayer.UserIDString) : msg("Notification.FF.MemberDisabled", memberPlayer.UserIDString)));
                    }
                }
            }

            player.ChatMessage(!member.AllyFFEnabled ? msg("Notification.FF.AllyEnabled", player.UserIDString) : msg("Notification.FF.AllyDisabled", player.UserIDString));
        }

        private void cmdChatClanInfo(BasePlayer player, string command, string[] args)
        {
            if (configData.Permissions.UsePermissionClanInfo && !permission.UserHasPermission(player.UserIDString, configData.Permissions.ClanInfoPermission))
            {
                player.ChatMessage(msg("Notification.Generic.NoPermissions", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage(msg("Notification.Generic.SpecifyClanTag", player.UserIDString));
                return;
            }

            Clan clan = storedData.FindClan(args[0]);
            if (clan == null)
            {
                player.ChatMessage(string.Format(msg("Notification.Generic.InvalidClan", player.UserIDString), args[0]));
                return;
            }

            clan.PrintClanInfo(player);
        }

        private void cmdChatClanHelp(BasePlayer player, string command, string[] args)
        {
            StringBuilder sb = new StringBuilder();

            Clan clan = storedData.FindClanByID(player.userID);
            if (clan == null)
            {
                sb.Append(msg("Notification.ClanInfo.Title", player.UserIDString));
                sb.Append(string.Format(msg("Notification.ClanHelp.NoClan", player.UserIDString), configData.Commands.ClanCommand));
                player.ChatMessage(sb.ToString());
                return;
            }

            sb.Append(msg("Notification.ClanInfo.Title", player.UserIDString));
            sb.Append(string.Format(msg("Notification.ClanHelp.Basic2", player.UserIDString), configData.Commands.ClanCommand, configData.Commands.ClanChatCommand));

            if (configData.Clans.MemberFF)
                sb.Append(string.Format(msg("Notification.ClanHelp.MFF", player.UserIDString), configData.Commands.FFCommand));

            if (configData.Clans.Alliance.Enabled && configData.Clans.Alliance.AllyFF)
                sb.Append(string.Format(msg("Notification.ClanHelp.AFF", player.UserIDString), configData.Commands.AFFCommand));


            if (clan.IsModerator(player.userID) || clan.IsCouncil(player.userID) || clan.OwnerID.Equals(player.userID))
            {
                if (configData.Clans.Alliance.Enabled && (clan.IsCouncil(player.userID) || clan.OwnerID.Equals(player.userID)))
                    sb.Append(string.Format(msg("Notification.ClanHelp.Alliance", player.UserIDString), configData.Commands.ClanAllyCommand));

                sb.Append(string.Format(msg("Notification.ClanHelp.Moderator", player.UserIDString), configData.Commands.ClanCommand));
            }

            if (clan.OwnerID.Equals(player.userID))
            {
                sb.Append(string.Format(msg("Notification.ClanHelp.Owner", player.UserIDString), configData.Commands.ClanCommand));

                if (configData.Tags.CustomColors)
                    sb.Append(string.Format(msg("Notification.ClanHelp.TagColor", player.UserIDString), configData.Commands.ClanCommand));
            }

            player.ChatMessage(sb.ToString());

        }

        private void cmdChatClanAlly(BasePlayer player, string command, string[] args)
        {
            if (!configData.Clans.Alliance.Enabled)
                return;

            if (args.Length < 2)
            {
                player.ChatMessage(string.Format(msg("Notification.ClanHelp.Alliance", player.UserIDString), configData.Commands.ClanAllyCommand));
                return;
            }

            string tag = args[1];

            switch (args[0].ToLower())
            {
                case "invite":
                    OfferAlliance(player, tag);
                    return;
                case "withdraw":
                    WithdrawAlliance(player, tag);
                    return;
                case "accept":
                    AcceptAlliance(player, tag);
                    return;
                case "reject":
                    RejectAlliance(player, tag);
                    return;
                case "revoke":
                    RevokeAlliance(player, tag);
                    return;
                default:
                    player.ChatMessage(string.Format(msg("Notification.ClanHelp.Alliance", player.UserIDString), configData.Commands.ClanAllyCommand));
                    return;
            }
        }

        private void cmdChatClan(BasePlayer player, string command, string[] args)
        {
            Clan clan = storedData.FindClanByID(player.userID);

            if (args.Length == 0)
            {
                StringBuilder sb = new StringBuilder();
                if (clan == null)
                {
                    sb.Append(msg("Notification.ClanInfo.Title", player.UserIDString));
                    sb.Append(msg("Notification.Clan.NotInAClan", player.UserIDString));
                    sb.Append(string.Format(msg("Notification.Clan.Help", player.UserIDString), configData.Commands.ClanHelpCommand));
                    player.ChatMessage(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(msg("Notification.ClanInfo.Title", player.UserIDString));
                    sb.Append(string.Format(msg((clan.IsOwner(player.userID) ? "Notification.Clan.OwnerOf" : clan.IsCouncil(player.userID) ? "Notification.Clan.CouncilOf" : clan.IsModerator(player.userID) ? "Notification.Clan.ModeratorOf" : "Notification.Clan.MemberOf"), player.UserIDString), clan.Tag, clan.OnlineCount, clan.MemberCount));
                    sb.Append(string.Format(msg("Notification.Clan.MembersOnline", player.UserIDString), clan.GetMembersOnline()));

                    if (configData.Clans.MemberFF)
                    {
                        bool isOn = clan.ClanMembers[player.userID].MemberFFEnabled;

                        sb.Append(string.Format(msg("Notification.Clan.MFF", player.UserIDString), isOn ? msg("Notification.FF.IsEnabled") : msg("Notification.FF.IsDisabled"), configData.Commands.FFCommand));
                    }

                    if (configData.Clans.Alliance.Enabled && configData.Clans.Alliance.AllyFF)
                    {
                        bool isOn = clan.ClanMembers[player.userID].AllyFFEnabled;

                        sb.Append(string.Format(msg("Notification.Clan.AFF", player.UserIDString), isOn ? msg("Notification.FF.IsEnabled") : msg("Notification.FF.IsDisabled"), configData.Commands.AFFCommand));
                    }

                    sb.Append(string.Format(msg("Notification.Clan.Help", player.UserIDString), configData.Commands.ClanHelpCommand));
                    player.ChatMessage(sb.ToString());
                    sb.Clear();
                }
                return;
            }

            string tag = clan?.Tag ?? string.Empty;

            switch (args[0].ToLower())
            {
                case "create":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.CreateSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    CreateClan(player, args[1], args.Length > 2 ? string.Join(" ", args.Skip(2)) : string.Empty);
                    return;

                case "leave":
                    LeaveClan(player);
                    return;

                case "invite":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.InviteSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    BasePlayer invitee = FindPlayer(args[1]);
                    if (!invitee)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Generic.UnableToFindPlayer", player.UserIDString), args[1]));
                        return;
                    }

                    if (invitee == player)
                    {
                        player.ChatMessage(msg("Notification.Generic.CommandSelf", player.UserIDString));
                        return;
                    }

                    InvitePlayer(player, invitee);
                    return;

                case "withdraw":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.WithdrawSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    WithdrawInvite(player, args[1]);
                    return;

                case "accept":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.AcceptSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    JoinClan(player, args[1]);
                    return;

                case "reject":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.RejectSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    RejectInvite(player, args[1]);
                    return;

                case "kick":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.KickSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    if (clan == null)
                    {
                        player.ChatMessage(msg("Notification.Clan.NotInAClan", player.UserIDString));
                        return;
                    }

                    ulong target = clan.FindPlayer(args[1]);
                    if (target == 0UL)
                    {
                        player.ChatMessage(msg("Notification.Kick.NoPlayerFound", player.UserIDString));
                        return;
                    }

                    if (target == player.userID)
                    {
                        player.ChatMessage(msg("Notification.Generic.CommandSelf", player.UserIDString));
                        return;
                    }

                    KickPlayer(player, target);
                    return;

                case "promote":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.PromoteSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    if (clan == null)
                    {
                        player.ChatMessage(msg("Notification.Clan.NotInAClan", player.UserIDString));
                        return;
                    }

                    ulong promotee = clan.FindPlayer(args[1]);
                    if (promotee == 0UL)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Generic.UnableToFindPlayer", player.UserIDString), args[1]));
                        return;
                    }

                    if (promotee == player.userID)
                    {
                        player.ChatMessage(msg("Notification.Generic.CommandSelf", player.UserIDString));
                        return;
                    }

                    PromotePlayer(player, promotee);
                    return;

                case "demote":
                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.DemoteSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    if (clan == null)
                    {
                        player.ChatMessage(msg("Notification.Clan.NotInAClan", player.UserIDString));
                        return;
                    }

                    ulong demotee = clan.FindPlayer(args[1]);
                    if (demotee == 0UL)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Generic.UnableToFindPlayer", player.UserIDString), args[1]));
                        return;
                    }

                    if (demotee == player.userID)
                    {
                        player.ChatMessage(msg("Notification.Generic.CommandSelf", player.UserIDString));
                        return;
                    }

                    DemotePlayer(player, demotee);
                    return;

                case "disband":
                    if (args.Length < 2 || !args[1].Equals("forever", StringComparison.OrdinalIgnoreCase))
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.DisbandSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    if (clan == null)
                    {
                        player.ChatMessage(msg("Notification.Generic.NoClan", player.UserIDString));
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        player.ChatMessage(msg("Notification.Disband.NotOwner", player.UserIDString));
                        return;
                    }

                    if (configData.Permissions.UsePermissionDisband && !permission.UserHasPermission(player.UserIDString, configData.Permissions.PermissionDisband))
                    {
                        player.ChatMessage(msg("Notification.Disband.NoPermission", player.UserIDString));
                        return;
                    }

                    clan.LocalizedBroadcast("Notification.Disband.Message", Array.Empty<object>());
                    clan.DisbandClan();

                    player.ChatMessage(string.Format(msg("Notification.Disband.Success", player.UserIDString), tag));
                    return;

                case "tagcolor":
                    if (!configData.Tags.CustomColors)
                    {
                        player.ChatMessage(msg("Notification.Clan.TagColorDisabled", player.UserIDString));
                        return;
                    }

                    if (args.Length < 2)
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.TagColorSyntax", player.UserIDString), configData.Commands.ClanCommand));
                        return;
                    }

                    if (clan == null)
                    {
                        player.ChatMessage(msg("Notification.Clan.NotInAClan", player.UserIDString));
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        player.ChatMessage(msg("Notification.Disband.NotOwner", player.UserIDString));
                        return;
                    }

                    string hexColor = args[1].ToUpper();

                    if (hexColor.Equals("RESET"))
                    {
                        clan.TagColor = string.Empty;
                        player.ChatMessage(msg("Notification.Clan.TagColorReset", player.UserIDString));
                        return;
                    }

                    if (hexColor.Length < 6 || hexColor.Length > 6 || !hexFilter.IsMatch(hexColor))
                    {
                        player.ChatMessage(msg("Notification.Clan.TagColorFormat", player.UserIDString));
                        return;
                    }

                    if (TagColorIsBlocked(hexColor))
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.TagColorBlocked", player.UserIDString), hexColor));
                        return;
                    }

                    if (!TagColorWithinRange(hexColor))
                    {
                        player.ChatMessage(string.Format(msg("Notification.Clan.TagColorOutOfRange", player.UserIDString), hexColor, configData.Tags.CustomTagColorMin, configData.Tags.CustomTagColorMax));
                        return;
                    }

                    clan.TagColor = hexColor;
                    player.ChatMessage(string.Format(msg("Notification.Clan.TagColorSet", player.UserIDString), clan.TagColor));
                    return;

                default:
                    player.ChatMessage(string.Format(msg("Notification.Clan.Help", player.UserIDString), configData.Commands.ClanHelpCommand));
                    return;
            }
        }

        #endregion

        #region API
        private void DisableBypass(ulong userId) => friendlyFireDisabled.Add(userId);

        private void EnableBypass(ulong userId) => friendlyFireDisabled.Remove(userId);

        private JObject GetClan(string tag)
        {
            if (!string.IsNullOrEmpty(tag))
                return storedData.FindClan(tag)?.ToJObject();

            return null;
        }

        private JArray GetAllClans() => new JArray(storedData.clans.Keys);


        private string GetClanOf(ulong playerId) => storedData.FindClanByID(playerId)?.Tag;

        private string GetClanOf(BasePlayer player) => GetClanOf(player ? (ulong)player.userID : 0UL);

        private string GetClanOf(string playerId) => GetClanOf(ulong.Parse(playerId));


        private List<string> GetClanMembers(ulong playerId) => storedData.FindClanByID(playerId)?.ClanMembers.Keys.Select(x => x.ToString()).ToList() ?? new List<string>();

        private List<string> GetClanMembers(string playerId) => GetClanMembers(ulong.Parse(playerId));


        private object HasFriend(ulong ownerId, ulong playerId)
        {
            Clan clanOwner = storedData.FindClanByID(ownerId);
            if (clanOwner == null)
                return null;

            Clan clanFriend = storedData.FindClanByID(playerId);
            if (clanFriend == null)
                return null;

            return clanOwner.Tag.Equals(clanFriend.Tag);
        }

        private object HasFriend(string ownerId, string playerId) => HasFriend(ulong.Parse(ownerId), ulong.Parse(playerId));


        private bool IsClanMember(ulong playerId, ulong otherId)
        {
            Clan clanPlayer = storedData.FindClanByID(playerId);
            if (clanPlayer == null)
                return false;

            Clan clanOther = storedData.FindClanByID(otherId);
            if (clanOther == null)
                return false;

            return clanPlayer.Tag.Equals(clanOther.Tag);
        }

        private bool IsClanMember(string playerId, string otherId) => IsClanMember(ulong.Parse(playerId), ulong.Parse(otherId));


        private bool IsMemberOrAlly(ulong playerId, ulong otherId)
        {
            Clan playerClan = storedData.FindClanByID(playerId);
            if (playerClan == null)
                return false;

            Clan otherClan = storedData.FindClanByID(otherId);
            if (otherClan == null)
                return false;

            if ((playerClan.Tag.Equals(otherClan.Tag)) || playerClan.Alliances.Contains(otherClan.Tag))
                return true;

            return false;
        }

        private bool IsMemberOrAlly(string playerId, string otherId) => IsMemberOrAlly(ulong.Parse(playerId), ulong.Parse(otherId));


        private bool IsAllyPlayer(ulong playerId, ulong otherId)
        {
            Clan playerClan = storedData.FindClanByID(playerId);
            if (playerClan == null)
                return false;

            Clan otherClan = storedData.FindClanByID(otherId);
            if (otherClan == null)
                return false;

            if (playerClan.Alliances.Contains(otherClan.Tag))
                return true;

            return false;
        }

        private bool IsAllyPlayer(string playerId, string otherId) => IsAllyPlayer(ulong.Parse(playerId), ulong.Parse(otherId));


        private List<string> GetClanAlliances(ulong playerId)
        {
            Clan clan = storedData.FindClanByID(playerId);
            if (clan == null)
                return new List<string>();

            return new List<string>(clan.Alliances);
        }

        private List<string> GetClanAlliances(string playerId) => GetClanAlliances(ulong.Parse(playerId));


        [HookMethod("ToggleFF")]
        public void ToggleFF(ulong playerId)
        {
            Clan.Member member = storedData.FindMemberByID(playerId);
            if (member == null)
                return;

            member.MemberFFEnabled = !member.MemberFFEnabled;
        }

        [HookMethod("HasFFEnabled")]
        public bool HasFFEnabled(ulong playerId)
        {
            Clan.Member member = storedData.FindMemberByID(playerId);
            if (member == null)
                return false;

            return member.MemberFFEnabled;
        }
        #endregion

        #region Clan
        [Serializable, ProtoContract]
        public class Clan
        {
            [ProtoMember(1), JsonProperty]
            public string Tag { get; set; }

            [ProtoMember(2), JsonProperty]
            public string Description { get; set; }

            [ProtoMember(3), JsonProperty]
            public ulong OwnerID { get; set; }

            [ProtoMember(4), JsonProperty]
            public double CreationTime { get; set; }

            [ProtoMember(5), JsonProperty]
            public double LastOnlineTime { get; set; }

            [ProtoMember(6), JsonProperty]
            public Hash<ulong, Member> ClanMembers { get; set; } = new Hash<ulong, Member>();

            [ProtoMember(7), JsonProperty]
            public Hash<ulong, MemberInvite> MemberInvites { get; set; } = new Hash<ulong, MemberInvite>();

            [ProtoMember(8), JsonProperty]
            public HashSet<string> Alliances { get; set; } = new HashSet<string>();

            [ProtoMember(9), JsonProperty]
            public Hash<string, double> AllianceInvites { get; set; } = new Hash<string, double>();

            [ProtoMember(10), JsonProperty]
            public HashSet<string> IncomingAlliances { get; set; } = new HashSet<string>();

            [ProtoMember(11), JsonProperty]
            public string TagColor { get; set; } = string.Empty;

            [ProtoMember(12), JsonProperty]
            public int MemberInviteCooldownTime { get; set; } = 0;

            [JsonIgnore, ProtoIgnore]
            internal int OnlineCount { get; private set; }

            [JsonIgnore, ProtoIgnore]
            internal ulong CouncilID
            {
                get
                {
                    foreach (KeyValuePair<ulong, Member> kvp in ClanMembers)
                    {
                        if (kvp.Value.Role == Member.MemberRole.Council)
                            return kvp.Key;
                    }
                    return 0UL;
                }
            }

            [JsonIgnore, ProtoIgnore]
            internal int ModeratorCount => ClanMembers.Where(x => x.Value.Role == Member.MemberRole.Moderator).Count();

            [JsonIgnore, ProtoIgnore]
            internal int MemberCount => ClanMembers.Count;

            [JsonIgnore, ProtoIgnore]
            internal int MemberInviteCount => MemberInvites.Count;

            [JsonIgnore, ProtoIgnore]
            internal int AllianceCount => Alliances.Count;

            [JsonIgnore, ProtoIgnore]
            internal int AllianceInviteCount => AllianceInvites.Count;

            [JsonIgnore, ProtoIgnore]
            private RelationshipManager.PlayerTeam _playerTeam;

            [JsonIgnore, ProtoIgnore]
            internal RelationshipManager.PlayerTeam PlayerTeam
            {
                get
                {
                    if (!configData.Clans.Teams.Enabled)
                        return null;

                    if (_playerTeam == null)
                    {
                        _playerTeam = Facepunch.Pool.Get<RelationshipManager.PlayerTeam>();
                        _playerTeam.teamID = FindRandomTeamID;
                        _playerTeam.teamStartTime = Time.realtimeSinceStartup;
                        _playerTeam.invites.Clear();
                        _playerTeam.members.Clear();
                        _playerTeam.onlineMemberConnections.Clear();

                        RelationshipManager.ServerInstance.teams.Add(_playerTeam.teamID, _playerTeam);

                        foreach (ulong playerId in ClanMembers.Keys)
                        {
                            BasePlayer player = RelationshipManager.FindByID(playerId);
                            if (player)
                            {
                                if (player.currentTeam != 0UL && player.currentTeam != _playerTeam.teamID)
                                {
                                    RelationshipManager.PlayerTeam oldTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                                    if (oldTeam != null)
                                    {
                                        oldTeam.members.Remove(player.userID);
                                        player.ClearTeam();
                                    }
                                }

                                player.currentTeam = _playerTeam.teamID;
                                player.SendNetworkUpdate();
                                player.TeamUpdate();
                            }

                            _playerTeam.members.Add(playerId);

                            RelationshipManager.ServerInstance.playerToTeam.Remove(playerId);
                            RelationshipManager.ServerInstance.playerToTeam.Add(playerId, _playerTeam);
                        }

                        _playerTeam.teamName = Tag;
                        _playerTeam.SetTeamLeader(OwnerID);

                        _playerTeam.MarkDirty();
                    }
                    return _playerTeam;
                }
            }

            public int CountMembersAndAlliances()
            {
                int count = MemberCount;

                foreach (string allianceTag in Alliances)
                {
                    Clan alliedClan = Instance.storedData.FindClan(allianceTag);
                    if (alliedClan != null)
                        count += alliedClan.MemberCount;
                }

                return count;
            }

            private static ulong FindRandomTeamID
            {
                get
                {
                START_AGAIN:
                    ulong teamId = (ulong)UnityEngine.Random.Range(10000, long.MaxValue);
                    if (RelationshipManager.ServerInstance.teams.ContainsKey(teamId))
                        goto START_AGAIN;

                    return teamId;
                }
            }

            public Clan() { }

            public Clan(BasePlayer player, string tag, string description)
            {
                this.Tag = tag;
                this.Description = StripHTMLTags(description);
                CreationTime = LastOnlineTime = UnixTimeStampUTC();
                OwnerID = player.userID;
                ClanMembers.Add(player.userID, new Member(Member.MemberRole.Owner, this));
                OnPlayerConnected(player);
            }

            #region Connection
            internal void OnPlayerConnected(BasePlayer player)
            {
                if (!player)
                    return;

                Member member;
                if (ClanMembers.TryGetValue(player.userID, out member))
                {
                    member.DisplayName = RemoveTags(player.displayName);
                    member.Player = player;

                    if (configData.Tags.Enabled)
                    {
                        string newDisplayname = $"{configData.Tags.TagOpen}{Tag}{configData.Tags.TagClose} {player.displayName}";
                        player.displayName = newDisplayname;
                        player._name = string.Format("{1}[{0}/{2}]", player.net.ID, newDisplayname, player.userID);
                    }

                    if (configData.Clans.Teams.Enabled)
                    {
                        if (player.currentTeam != 0UL && player.currentTeam != PlayerTeam.teamID)
                        {
                            RelationshipManager.PlayerTeam oldTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                            if (oldTeam != null)
                            {
                                oldTeam.members.Remove(player.userID);
                                player.ClearTeam();
                            }
                        }

                        player.currentTeam = PlayerTeam.teamID;

                        if (!PlayerTeam.members.Contains(player.userID))
                            PlayerTeam.members.Add(player.userID);

                        RelationshipManager.ServerInstance.playerToTeam.Remove(player.userID);
                        RelationshipManager.ServerInstance.playerToTeam.Add(player.userID, PlayerTeam);

                        player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        player.TeamUpdate();

                        if (member.Role == Member.MemberRole.Owner)
                            PlayerTeam.teamLeader = player.userID;

                        PlayerTeam.MarkDirty();
                    }

                    player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                    if (configData.Permissions.PermissionGroups)
                        Instance.permission.AddUserGroup(player.UserIDString, configData.Permissions.PermissionGroupPrefix + Tag);

                    LastOnlineTime = UnixTimeStampUTC();
                    OnlineCount++;
                }

                MarkDirty();
            }

            internal void OnPlayerDisconnected(BasePlayer player)
            {
                if (!player)
                    return;

                Member member;
                if (ClanMembers.TryGetValue(player.userID, out member))
                {
                    if (configData.Tags.Enabled)
                    {
                        player.displayName = member.DisplayName;
                        player._name = string.Format("{1}[{0}/{2}]", player.net.ID, member.DisplayName, player.userID);

                        player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }

                    if (configData.Permissions.PermissionGroups)
                        Instance.permission.RemoveUserGroup(player.UserIDString, configData.Permissions.PermissionGroupPrefix + Tag);

                    member.Player = null;

                    LastOnlineTime = UnixTimeStampUTC();
                    OnlineCount--;
                }

                MarkDirty();
            }
            #endregion

            #region Clan Management
            internal bool InvitePlayer(BasePlayer inviter, BasePlayer invitee)
            {
                if (!IsOwner(inviter.userID) && !IsCouncil(inviter.userID) && !IsModerator(inviter.userID))
                {
                    inviter.ChatMessage(msg("Notification.Invite.NoPermissions", inviter.UserIDString));
                    return false;
                }

                if (ClanMembers.ContainsKey(invitee.userID))
                {
                    inviter.ChatMessage(string.Format(msg("Notification.Invite.IsMember", inviter.UserIDString), invitee.displayName));
                    return false;
                }

                if (MemberInvites.ContainsKey(invitee.userID))
                {
                    inviter.ChatMessage(string.Format(msg("Notification.Invite.HasPending", inviter.UserIDString), invitee.displayName));
                    return false;
                }

                if (MemberCount >= configData.Clans.MemberLimit)
                {
                    inviter.ChatMessage(msg("Notification.Generic.ClanFull", inviter.UserIDString));
                    return false;
                }

                if (configData.Clans.Alliance.Enabled && configData.Clans.Alliance.CountAllianceMembers)
                {
                    int count = CountMembersAndAlliances();

                    if (count >= configData.Clans.MemberLimit)
                    {
                        inviter.ChatMessage(msg("Notification.Generic.ClanFull", inviter.UserIDString));
                        return false;
                    }
                }

                if (MemberInviteCount >= configData.Clans.Invites.MemberInviteLimit)
                {
                    inviter.ChatMessage(msg("Notification.Invite.InviteLimit", inviter.UserIDString));
                    return false;
                }

                int cooldownTimeRemain = MemberInviteCooldownTime - UnixTimeStampUTC();
                if (cooldownTimeRemain > 0)
                {
                    inviter.ChatMessage(string.Format(msg("Notification.Invite.InviteCooldown", inviter.UserIDString), FormatTime(cooldownTimeRemain)));
                    return false;
                }

                MemberInvites[invitee.userID] = new MemberInvite(invitee);

                Instance.storedData.AddPlayerInvite(invitee.userID, Tag);

                if (configData.Clans.Teams.Enabled)
                    PlayerTeam.SendInvite(invitee);

                invitee.ChatMessage(string.Format(msg("Notification.Invite.SuccesTarget", invitee.UserIDString), Tag, Description, configData.Commands.ClanCommand));
                LocalizedBroadcast("Notification.Invite.SuccessClan", inviter.displayName, invitee.displayName);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"{inviter.displayName} invited {invitee.displayName} to [{Tag}]", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"{inviter.displayName} invited {invitee.displayName} to [{Tag}]", (int)MessageType.Invite);
                return true;
            }

            internal bool JoinClan(BasePlayer player)
            {
                if (!MemberInvites.ContainsKey(player.userID))
                    return false;

                if ((UnixTimeStampUTC() - MemberInvites[player.userID].ExpiryTime > configData.Clans.Invites.MemberInviteExpireTime))
                {
                    MemberInvites.Remove(player.userID);
                    player.ChatMessage(string.Format(msg("Notification.Join.ExpiredInvite", player.UserIDString), Tag));
                    return false;
                }

                if (MemberCount >= configData.Clans.MemberLimit)
                {
                    player.ChatMessage(msg("Notification.Generic.ClanFull", player.UserIDString));
                    return false;
                }

                if (configData.Clans.Alliance.Enabled && configData.Clans.Alliance.CountAllianceMembers)
                {
                    int count = CountMembersAndAlliances();

                    if (count >= configData.Clans.MemberLimit)
                    {
                        player.ChatMessage(msg("Notification.Generic.ClanFull", player.UserIDString));
                        return false;
                    }
                }

                Instance.storedData.OnInviteAccepted(player.userID, Tag);

                MemberInvites.Remove(player.userID);
                List<ulong> currentMembers = ClanMembers.Keys.ToList();

                ClanMembers.Add(player.userID, new Member(Member.MemberRole.Member, this));

                Instance.storedData.RegisterPlayer(player.userID, Tag);

                OnPlayerConnected(player);

                LocalizedBroadcast("Notification.Join.Message", player.displayName);

                Interface.Oxide.CallHook("OnClanMemberJoined", (ulong)player.userID, Tag);
                Interface.Oxide.CallHook("OnClanMemberJoined", (ulong)player.userID, currentMembers);

                Interface.Oxide.CallHook("OnClanUpdate", Tag);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"{player.displayName} joined [{Tag}]", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} joined [{Tag}]", (int)MessageType.Join);
                return true;
            }

            internal bool LeaveClan(BasePlayer player)
            {
                if (!ClanMembers.ContainsKey(player.userID))
                    return false;

                if (configData.Clans.Teams.Enabled && _playerTeam != null)
                    RemoveFromTeam(_playerTeam, player);

                OnPlayerDisconnected(player);

                ClanMembers.Remove(player.userID);
                Instance.storedData.UnregisterPlayer(player.userID);

                player.ChatMessage(string.Format(msg("Notification.Leave.PlayerMessage", player.UserIDString), Tag));
                LocalizedBroadcast("Notification.Leave.Message", player.displayName);

                MarkDirty();

                if (ClanMembers.Count == 0)
                {
                    Interface.Oxide.CallHook("OnClanMemberGone", (ulong)player.userID, Tag);
                    Interface.Oxide.CallHook("OnClanMemberGone", (ulong)player.userID, ClanMembers.Keys.ToList());

                    if (configData.Options.LogChanges)
                        Instance.LogToFile(Instance.Title, $"{player.displayName} has left [{Tag}]", Instance);

                    DisbandClan();
                    return true;
                }

                if (OwnerID == player.userID)
                {
                    ulong councilId = CouncilID;
                    if (councilId != 0UL)
                    {
                        OwnerID = councilId;
                        ClanMembers[councilId].Role = Member.MemberRole.Owner;
                    }
                    else
                    {
                        foreach (KeyValuePair<ulong, Member> kvp in ClanMembers.OrderBy(x => x.Value.Role))
                        {
                            OwnerID = kvp.Key;
                            kvp.Value.Role = Member.MemberRole.Owner;
                            break;
                        }
                    }

                    if (configData.Clans.Teams.Enabled)
                        PlayerTeam.SetTeamLeader(OwnerID);

                    LocalizedBroadcast("Notification.Leave.NewOwner", ClanMembers[OwnerID].DisplayName);
                }

                MemberInviteCooldownTime = UnixTimeStampUTC() + configData.Clans.Invites.InviteCooldownAfterMemberLeave;

                Interface.Oxide.CallHook("OnClanMemberGone", (ulong)player.userID, ClanMembers.Keys.ToList());
                Interface.Oxide.CallHook("OnClanMemberGone", (ulong)player.userID, Tag);
                Interface.Oxide.CallHook("OnClanUpdate", Tag);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"{player.displayName} has left [{Tag}]", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"{player.displayName} has left [{Tag}]", (int)MessageType.Leave);

                return true;
            }

            internal bool KickMember(BasePlayer player, ulong targetId)
            {
                if (!ClanMembers.ContainsKey(targetId))
                {
                    player.ChatMessage(msg("Notification.Kick.NotClanmember", player.UserIDString));
                    return false;
                }

                if (IsOwner(targetId))
                {
                    player.ChatMessage(msg("Notification.Kick.IsOwner", player.UserIDString));
                    return false;
                }

                if (!IsOwner(player.userID) && !IsModerator(player.userID) && !IsCouncil(player.userID))
                {
                    player.ChatMessage(msg("Notification.Kick.NoPermissions", player.UserIDString));
                    return false;
                }

                if ((IsOwner(targetId) || IsModerator(targetId) || IsCouncil(targetId)) && OwnerID != player.userID)
                {
                    player.ChatMessage(msg("Notification.Kick.NotEnoughRank", player.UserIDString));
                    return false;
                }

                Member member = ClanMembers[targetId];

                if (configData.Clans.Teams.Enabled && _playerTeam != null)
                    RemoveFromTeam(_playerTeam, targetId);

                if (member.IsConnected && member.Player != null)
                {
                    member.Player.ChatMessage(string.Format(msg("Notification.Kick.PlayerMessage", member.Player.UserIDString), player.displayName));

                    OnPlayerDisconnected(member.Player);
                }

                MemberInviteCooldownTime = UnixTimeStampUTC() + configData.Clans.Invites.InviteCooldownAfterMemberLeave;

                ClanMembers.Remove(targetId);
                Instance.storedData.UnregisterPlayer(targetId);

                LocalizedBroadcast("Notification.Kick.Message", player.displayName, member.DisplayName);

                Interface.Oxide.CallHook("OnClanMemberGone", targetId, ClanMembers.Keys.ToList());
                Interface.Oxide.CallHook("OnClanMemberGone", targetId, Tag);
                Interface.Oxide.CallHook("OnClanUpdate", Tag);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"{member.DisplayName} was kicked from [{Tag}] by {player.displayName}", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"{member.DisplayName} was kicked from [{Tag}] by {player.displayName}", (int)MessageType.Kick);
                return true;
            }

            internal bool PromotePlayer(BasePlayer promoter, ulong targetId)
            {
                if (!IsOwner(promoter.userID))
                {
                    promoter.ChatMessage(msg("Notification.Promotion.NoPermissions", promoter.UserIDString));
                    return false;
                }

                if (IsOwner(targetId))
                {
                    promoter.ChatMessage(msg("Notification.Promotion.IsOwner", promoter.UserIDString));
                    return false;
                }

                if (configData.Clans.Alliance.Enabled)
                {
                    if (IsCouncil(targetId))
                    {
                        promoter.ChatMessage(msg("Notification.Promotion.IsCouncil", promoter.UserIDString));
                        return false;
                    }

                    if (IsModerator(targetId) && HasCouncil())
                    {
                        promoter.ChatMessage(msg("Notification.Promotion.CouncilTaken", promoter.UserIDString));
                        return false;
                    }
                }
                else
                {
                    if (IsModerator(targetId))
                    {
                        promoter.ChatMessage(msg("Notification.Promotion.IsModerator", promoter.UserIDString));
                        return false;
                    }
                }

                if (IsMember(targetId) && ModeratorCount >= configData.Clans.ModeratorLimit)
                {
                    promoter.ChatMessage(msg("Notification.Promotion.ModeratorLimit", promoter.UserIDString));
                    return false;
                }

                Member member = ClanMembers[targetId];
                member.Role = (Member.MemberRole)((int)member.Role - 1);

                MarkDirty();

                LocalizedBroadcast("Notification.Promotion.Message", member.DisplayName, string.Format(COLORED_LABEL, GetRoleColor(member.Role), member.Role), string.Format(COLORED_LABEL, GetRoleColor(promoter.userID), promoter.displayName));
                Interface.Oxide.CallHook("OnClanUpdate", Tag);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"{member.DisplayName} was promototed to {member.Role} by {promoter.displayName}", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"{member.DisplayName} was promototed to {member.Role} by {promoter.displayName}", (int)MessageType.Promote);
                return true;
            }

            internal bool DemotePlayer(BasePlayer demoter, ulong targetId)
            {
                if (!IsOwner(demoter.userID))
                {
                    demoter.ChatMessage(msg("Notification.Demotion.NoPermissions", demoter.UserIDString));
                    return false;
                }

                Member member = ClanMembers[targetId];
                if (IsMember(targetId))
                {
                    demoter.ChatMessage(string.Format(msg("Notification.Demotion.IsMember", demoter.UserIDString), member.DisplayName));
                    return false;
                }

                member.Role = member.Role == Member.MemberRole.Council ? (ModeratorCount >= configData.Clans.ModeratorLimit ? Member.MemberRole.Member : Member.MemberRole.Moderator) : (Member.MemberRole)((int)member.Role + 1);

                MarkDirty();

                LocalizedBroadcast("Notification.Demotion.Message", member.DisplayName, string.Format(COLORED_LABEL, GetRoleColor(member.Role), member.Role), string.Format(COLORED_LABEL, GetRoleColor(demoter.userID), demoter.displayName));

                Interface.Oxide.CallHook("OnClanUpdate", Tag);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"{member.DisplayName} was demoted to {member.Role} by {demoter.displayName}", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"{member.DisplayName} was demoted to {member.Role} by {demoter.displayName}", (int)MessageType.Demote);
                return true;
            }

            internal void DisbandClan()
            {
                List<ulong> clanMembers = ClanMembers.Keys.ToList();

                OnUnload();

                Instance.storedData.clans.Remove(Tag);

                foreach (KeyValuePair<string, Clan> kvp in Instance.storedData.clans)
                    kvp.Value.OnClanDisbanded(Tag);

                if (configData.Options.LogChanges)
                    Instance.LogToFile(Instance.Title, $"The clan [{Tag}] was disbanded", Instance);

                Instance.DiscordClans?.CallHook("LogMessage", $"The clan [{Tag}] was disbanded", (int)MessageType.Disband);

                Interface.CallHook("OnClanDisbanded", clanMembers);
                Interface.CallHook("OnClanDisbanded", Tag);
            }

            internal void OnClanDisbanded(string tag)
            {
                Alliances.Remove(tag);
                AllianceInvites.Remove(tag);
                IncomingAlliances.Remove(tag);
            }

            internal void OnUnload()
            {
                foreach (KeyValuePair<ulong, Member> kvp in ClanMembers)
                {
                    Instance.storedData.UnregisterPlayer(kvp.Key);

                    if (configData.Clans.Teams.Enabled && _playerTeam != null)
                    {
                        _playerTeam.members.Remove(kvp.Key);
                        RelationshipManager.ServerInstance.playerToTeam.Remove(kvp.Key);

                        BasePlayer basePlayer = (kvp.Value.Player ? kvp.Value.Player : RelationshipManager.FindByID(kvp.Key));
                        if (basePlayer != null)
                        {
                            basePlayer.ClearTeam();
                            basePlayer.BroadcastAppTeamRemoval();
                        }
                    }

                    if (kvp.Value.Player != null)
                        OnPlayerDisconnected(kvp.Value.Player);
                }

                if (_playerTeam != null)
                {
                    RelationshipManager.ServerInstance.teams.Remove(_playerTeam.teamID);
                    ClearTeam(ref _playerTeam);
                }
            }

            internal bool IsAlliedClan(string otherClan) => Alliances.Contains(otherClan);

            internal void MarkDirty()
            {
                cachedClanInfo = string.Empty;
                membersOnline = string.Empty;
                serializedClanObject = null;
            }
            #endregion

            #region Clan Chat
            internal void Broadcast(string message)
            {
                foreach (Member member in ClanMembers.Values)
                {
                    if (member?.Player)
                        member.Player.ChatMessage(message);
                }
            }

            internal void LocalizedBroadcast(string key, params object[] args)
            {
                foreach (Member member in ClanMembers.Values)
                {
                    if (member?.Player)
                        member.Player.ChatMessage(args?.Length > 0 ? string.Format(msg(key, member.Player.UserIDString), args) : msg(key, member.Player.UserIDString));
                }
            }
            #endregion

            #region Clan Info
            [JsonIgnore, ProtoIgnore]
            private string cachedClanInfo = string.Empty;

            [JsonIgnore, ProtoIgnore]
            private string membersOnline = string.Empty;

            internal void PrintClanInfo(BasePlayer player)
            {
                if (string.IsNullOrEmpty(cachedClanInfo))
                {
                    StringBuilder str = new StringBuilder();
                    str.Append(msg("Notification.ClanInfo.Title"));
                    str.Append(string.Format(msg("Notification.ClanInfo.Tag"), Tag));

                    if (!string.IsNullOrEmpty(Description))
                        str.Append(string.Format(msg("Notification.ClanInfo.Description"), Description));

                    if (configData.Commands.ClanInfoOptions.MemberCount)
                        str.Append(string.Format(msg("Notification.ClanInfo.TotalMembers"), MemberCount));

                    if (configData.Commands.ClanInfoOptions.Players)
                    {
                        List<string> online = Facepunch.Pool.Get<List<string>>();
                        List<string> offline = Facepunch.Pool.Get<List<string>>();

                        foreach (KeyValuePair<ulong, Member> kvp in ClanMembers)
                        {
                            string member = string.Format(COLORED_LABEL, GetRoleColor(kvp.Key), kvp.Value.DisplayName);

                            if (kvp.Value.IsConnected)
                                online.Add(member);
                            else offline.Add(member);
                        }

                        if (online.Count > 0)
                            str.Append(string.Format(msg("Notification.ClanInfo.Online"), online.ToSentence()));

                        if (offline.Count > 0)
                            str.Append(string.Format(msg("Notification.ClanInfo.Offline"), offline.ToSentence()));

                        Facepunch.Pool.FreeUnmanaged(ref online);
                        Facepunch.Pool.FreeUnmanaged(ref offline);
                    }
                    else
                    {
                        str.Append(string.Format(msg("Notification.ClanInfo.Members"), ClanMembers.Select(x => string.Format(COLORED_LABEL, GetRoleColor(x.Key), x.Value.DisplayName)).ToSentence()));
                    }


                    str.Append(string.Format(msg("Notification.ClanInfo.Established"), UnixTimeStampToDateTime(CreationTime)));

                    if (configData.Commands.ClanInfoOptions.LastOnline)
                        str.Append(string.Format(msg("Notification.ClanInfo.LastOnline"), UnixTimeStampToDateTime(LastOnlineTime)));

                    if (configData.Clans.Alliance.Enabled)
                    {
                        if (configData.Commands.ClanInfoOptions.AllianceCount)
                            str.Append(string.Format(msg("Notification.ClanInfo.TotalAlliances"), AllianceCount));

                        if (configData.Commands.ClanInfoOptions.Alliances)
                            str.Append(string.Format(msg("Notification.ClanInfo.Alliances"), Alliances.Count > 0 ? Alliances.ToSentence() : msg("Notification.ClanInfo.Alliances.None")));
                    }

                    cachedClanInfo = str.ToString();
                }

                player.ChatMessage(cachedClanInfo);
            }

            internal string GetMembersOnline()
            {
                if (string.IsNullOrEmpty(membersOnline))
                {
                    List<string> list = Facepunch.Pool.Get<List<string>>();

                    foreach (KeyValuePair<ulong, Member> kvp in ClanMembers)
                    {
                        if (kvp.Value.IsConnected)
                        {
                            string member = string.Format(COLORED_LABEL, GetRoleColor(kvp.Key), kvp.Value.DisplayName);
                            list.Add(member);
                        }
                    }

                    membersOnline = list.ToSentence();

                    Facepunch.Pool.FreeUnmanaged(ref list);
                }
                return membersOnline;
            }
            #endregion

            #region Roles
            internal bool IsOwner(ulong playerId) => ClanMembers[playerId].Role == Member.MemberRole.Owner;

            internal bool IsCouncil(ulong playerId) => ClanMembers[playerId].Role == Member.MemberRole.Council;

            internal bool IsModerator(ulong playerId) => ClanMembers[playerId].Role == Member.MemberRole.Moderator;

            internal bool IsMember(ulong playerId) => ClanMembers[playerId].Role == Member.MemberRole.Member;

            internal Member GetOwner() => ClanMembers[OwnerID];

            internal bool HasCouncil()
            {
                foreach (Member member in ClanMembers.Values)
                {
                    if (member.Role == Member.MemberRole.Council)
                        return true;
                }
                return false;
            }

            internal string GetRoleColor(ulong userID) => GetRoleColor(ClanMembers[userID].Role);

            internal string GetRoleColor(Member.MemberRole role)
            {
                if (role == Member.MemberRole.Owner)
                    return configData.Colors.Owner;

                if (role == Member.MemberRole.Council)
                    return configData.Colors.Council;

                if (role == Member.MemberRole.Moderator)
                    return configData.Colors.Moderator;

                return configData.Colors.Member;
            }
            #endregion

            [Serializable, ProtoContract]
            public class Member
            {
                [JsonIgnore, ProtoIgnore]
                public BasePlayer Player { get; set; }

                [ProtoMember(1)]
                public string DisplayName { get; set; } = string.Empty;

                [ProtoMember(2)]
                public MemberRole Role { get; set; }

                [ProtoMember(3)]
                public bool MemberFFEnabled { get; set; }

                [ProtoMember(4)]
                public bool AllyFFEnabled { get; set; }

                [JsonIgnore, ProtoIgnore]
                internal bool IsConnected => Player && Player.IsConnected;

                [JsonIgnore, ProtoIgnore]
                internal float lastFFAttackTime;

                [JsonIgnore, ProtoIgnore]
                internal float lastAFFAttackTime;

                public Member() { }

                public Member(MemberRole role, Clan clan)
                {
                    this.Role = role;

                    if (role == MemberRole.Owner)
                    {
                        MemberFFEnabled = configData.Clans.DefaultEnableFF;
                        AllyFFEnabled = configData.Clans.Alliance.DefaultEnableFF;
                    }
                    else
                    {
                        MemberFFEnabled = configData.Clans.OwnerFF ? clan.GetOwner().MemberFFEnabled : configData.Clans.DefaultEnableFF;
                        AllyFFEnabled = configData.Clans.Alliance.OwnerFF ? clan.GetOwner().AllyFFEnabled : configData.Clans.Alliance.DefaultEnableFF;
                    }
                }

                public Member(MemberRole role, bool memberFFEnabled, bool allyFFEnabled)
                {
                    this.Role = role;

                    MemberFFEnabled = memberFFEnabled;
                    AllyFFEnabled = allyFFEnabled;
                }

                public void OnClanMemberHit(string victimName)
                {
                    if (Time.time - lastFFAttackTime > 3f)
                    {
                        Player.ChatMessage(string.Format(msg("Notification.FF.OnHitClanMember", Player.UserIDString), victimName, configData.Commands.FFCommand));
                        lastFFAttackTime = Time.time;
                    }
                }

                public void OnAllyMemberHit(string victimName)
                {
                    if (Time.time - lastAFFAttackTime > 3f)
                    {
                        Player.ChatMessage(string.Format(msg("Notification.FF.OnHitAllyMember", Player.UserIDString), victimName, configData.Commands.AFFCommand));
                        lastAFFAttackTime = Time.time;
                    }
                }

                public enum MemberRole { Owner, Council, Moderator, Member }
            }

            [Serializable, ProtoContract]
            public class MemberInvite
            {
                [ProtoMember(1)]
                public string DisplayName { get; set; }

                [ProtoMember(2)]
                public double ExpiryTime { get; set; }

                public MemberInvite() { }

                public MemberInvite(BasePlayer player)
                {
                    DisplayName = player.displayName;
                    ExpiryTime = UnixTimeStampUTC();
                }
            }

            [JsonIgnore, ProtoIgnore]
            private JObject serializedClanObject;

            internal JObject ToJObject()
            {
                if (serializedClanObject != null)
                    return serializedClanObject;

                serializedClanObject = new JObject();
                serializedClanObject["tag"] = Tag;
                serializedClanObject["description"] = Description;
                serializedClanObject["owner"] = OwnerID;
                serializedClanObject["council"] = CouncilID;

                JArray jmoderators = new JArray();
                JArray jmembers = new JArray();

                foreach (KeyValuePair<ulong, Member> kvp in ClanMembers)
                {
                    if (kvp.Value.Role == Member.MemberRole.Moderator)
                        jmoderators.Add(kvp.Key);

                    jmembers.Add(kvp.Key);
                }

                serializedClanObject["moderators"] = jmoderators;
                serializedClanObject["members"] = jmembers;

                JArray jallies = new JArray();

                foreach (string ally in Alliances)
                    jallies.Add(ally);

                serializedClanObject["allies"] = jallies;

                JArray jinvallies = new JArray();

                foreach (KeyValuePair<string, double> ally in AllianceInvites)
                    jinvallies.Add(ally.Key);

                serializedClanObject["invitedallies"] = jinvallies;

                return serializedClanObject;
            }

            internal ulong FindPlayer(string partialNameOrID)
            {
                foreach (KeyValuePair<ulong, Member> kvp in ClanMembers)
                {
                    if (kvp.Key.Equals(partialNameOrID))
                        return kvp.Key;

                    if (kvp.Value.DisplayName.Contains(partialNameOrID, CompareOptions.OrdinalIgnoreCase))
                        return kvp.Key;
                }

                return 0UL;
            }
        }
        #endregion

        #region Console Commands        
        [ConsoleCommand("clans")]
        private void ccmdClans(ConsoleSystem.Arg arg)
        {
            bool isRcon = arg.Connection == null;
            if (isRcon || (arg.Connection?.player != null && arg.Connection?.authLevel > 0))
            {
                if (arg.Args == null || arg.Args.Length == 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("> Clans command overview <");
                    sb.AppendLine("clans list (Lists all clans, their owners and their member-count)");
                    sb.AppendLine("clans listex (Lists all clans, their owners/members and their on-line status)");
                    sb.AppendLine("clans show \'tag|partialNameOrId\' (lists the chosen clan (or clan by user) and the members with status)");
                    sb.AppendLine("clans msg \'tag\' \'message without quotes\' (Sends a clan message)");

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Create)
                        sb.AppendLine("clans create \'tag(case-sensitive)\' \'steam-id(owner)\' \'desc(optional)\'");

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Reserve)
                        sb.AppendLine("clans reserve \'tag(case-sensitive)\' \'steam-id(optional)\'. If not steam ID is supplied, and that clan tag is already in use, it will reserve the tag for all members of that clan");

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Rename)
                        sb.AppendLine("clans rename \'old tag\' \'new tag\' (renames a clan | case-sensitive)");

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Disband)
                        sb.AppendLine("clans disband \'tag\' (disbands a clan)");

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Invite)
                    {
                        sb.AppendLine("clans invite \'tag\' \'partialNameOrId\' (sends clan invitation to a player)");
                        sb.AppendLine("clans join \'tag\' \'partialNameOrId\' (joins a player into a clan)");
                    }

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Kick)
                        sb.AppendLine("clans kick \'tag\' \'partialNameOrId\' (kicks a member from a clan | deletes invite)");

                    if (isRcon || arg.Connection.authLevel >= configData.Commands.Auth.Promote)
                    {
                        sb.AppendLine("clans owner \'tag\' \'partialNameOrId\' (sets a new owner)");
                        sb.AppendLine("clans promote \'tag\' \'partialNameOrId\' (promotes a member)");
                        sb.AppendLine("clans demote \'tag\' \'partialNameOrId\' (demotes a member)");
                    }

                    SendReply(arg, sb.ToString());
                    return;
                }

                int authLevel = isRcon ? 2 : (int)arg.Connection.authLevel;
                switch (arg.Args[0].ToLower())
                {
                    case "list":
                        {
                            TextTable textTable = new TextTable();
                            textTable.AddColumn("Tag");
                            textTable.AddColumn("Owner");
                            textTable.AddColumn("SteamID");
                            textTable.AddColumn("Count");
                            textTable.AddColumn("On");

                            foreach (Clan clan in storedData.clans.Values)
                            {
                                string ownerName = clan.ClanMembers.FirstOrDefault(x => x.Value.Role == Clan.Member.MemberRole.Owner).Value?.DisplayName ?? string.Empty;
                                textTable.AddRow(new string[] { clan.Tag, ownerName, clan.OwnerID.ToString(), clan.MemberCount.ToString(), clan.OnlineCount.ToString() });
                            }

                            SendReply(arg, "\n>> Current clans <<\n" + textTable);
                        }
                        return;
                    case "listex":
                        {
                            TextTable textTable = new TextTable();
                            textTable.AddColumn("Tag");
                            textTable.AddColumn("Role");
                            textTable.AddColumn("Name");
                            textTable.AddColumn("SteamID");
                            textTable.AddColumn("Status");
                            foreach (Clan clan in storedData.clans.Values)
                            {
                                foreach (KeyValuePair<ulong, Clan.Member> kvp in clan.ClanMembers)
                                {
                                    textTable.AddRow(new string[] { clan.Tag, kvp.Value.Role.ToString(), kvp.Value.DisplayName, kvp.Key.ToString(), kvp.Value.Player != null ? "Online" : "Offline" });
                                }

                                textTable.AddRow(new string[] { });
                            }

                            SendReply(arg, "\n>> Current clans with members <<\n" + textTable);
                        }
                        return;
                    case "show":
                        {
                            if (arg.Args.Length < 2)
                            {
                                SendReply(arg, "Usage: clans show \'tag|partialNameOrId\'");
                                return;
                            }

                            Clan clan = storedData.FindClan(arg.Args[1]);
                            if (clan == null)
                            {
                                Oxide.Core.Libraries.Covalence.IPlayer iPlayer = covalence.Players.FindPlayer(arg.Args[1]);
                                if (iPlayer != null)
                                {
                                    clan = storedData.FindClanByID(ulong.Parse(iPlayer.Id));
                                }
                            }

                            if (clan == null)
                            {
                                SendReply(arg, $"No clan or player found with: {arg.Args[1]}");
                                return;
                            }

                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine($"\n>> Show clan [{clan.Tag}] <<");
                            sb.AppendLine($"Description: {clan.Description}");
                            sb.AppendLine($"Time created: {UnixTimeStampToDateTime(clan.CreationTime)}");
                            sb.AppendLine($"Last online: {UnixTimeStampToDateTime(clan.LastOnlineTime)}");
                            sb.AppendLine($"Member count: {clan.MemberCount}");

                            TextTable textTable = new TextTable();
                            textTable.AddColumn("Role");
                            textTable.AddColumn("Name");
                            textTable.AddColumn("SteamID");
                            textTable.AddColumn("Status");
                            sb.AppendLine();
                            foreach (KeyValuePair<ulong, Clan.Member> kvp in clan.ClanMembers)
                            {
                                textTable.AddRow(new string[] { clan.Tag, kvp.Value.Role.ToString(), kvp.Value.DisplayName, kvp.Key.ToString(), kvp.Value.Player != null ? "Online" : "Offline" });
                            }

                            sb.AppendLine(textTable.ToString());
                            SendReply(arg, sb.ToString());
                            SendReply(arg, $"Allied Clans: {clan.Alliances.ToSentence()}");
                        }
                        return;
                    case "msg":
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans.msg \'tag\' \'your message without quotes\'");
                                return;
                            }

                            Clan clan = storedData.FindClan(arg.Args[1]);
                            if (clan == null)
                            {
                                SendReply(arg, $"Unable to find a clan with the tag: {arg.Args[1]}");
                                return;
                            }

                            string message = string.Join(" ", arg.Args.Skip(2));

                            clan.LocalizedBroadcast("Admin.BroadcastToClan", message);
                            SendReply(arg, $"Broadcast to [{clan.Tag}]: {message}");
                        }
                        return;
                    case "create":
                        if (authLevel >= configData.Commands.Auth.Create)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans create \'tag(case-sensitive)\' \'steamid(owner)\' \'desc(optional)\'");
                                return;
                            }

                            string tag = arg.Args[1];

                            if (tag.Length < configData.Tags.TagLength.Minimum || tag.Length > configData.Tags.TagLength.Maximum)
                            {
                                SendReply(arg, $"Invalid tag length, it must be between {configData.Tags.TagLength.Minimum} and {configData.Tags.TagLength.Maximum} characters long");
                                return;
                            }

                            if (tagFilter.IsMatch(tag) || ContainsBlockedWord(tag))
                            {
                                SendReply(arg, "Invalid characters or blocked words detected in tag");
                                return;
                            }

                            if (ClanTagExists(tag))
                            {
                                SendReply(arg, "A clan with that tag already exists");
                                return;
                            }

                            Core.Libraries.Covalence.IPlayer owner = covalence.Players.FindPlayerById(arg.Args[2]);
                            if (owner == null)
                            {
                                SendReply(arg, "No player found with that ID");
                                return;
                            }

                            BasePlayer ownerPlayer = owner.Object as BasePlayer;
                            ulong ownerID = ulong.Parse(owner.Id);

                            if (storedData.FindClanByID(ownerID) != null)
                            {
                                SendReply(arg, "The specified owner is already in a clan");
                                return;
                            }

                            string description = arg.Args.Length > 3 ? string.Join(" ", arg.Args.Skip(3)) : string.Empty;

                            storedData.clans[tag] = ownerPlayer != null ? new Clan(ownerPlayer, tag, description) :
                                new Clan() { Tag = tag, Description = StripHTMLTags(description), OwnerID = ownerID, CreationTime = UnixTimeStampUTC(), LastOnlineTime = UnixTimeStampUTC(), ClanMembers = new Hash<ulong, Clan.Member>() { [ownerID] = new Clan.Member() { Role = Clan.Member.MemberRole.Owner } } };

                            storedData.RegisterPlayer(ownerID, tag);

                            if (ownerPlayer != null)
                            {
                                ownerPlayer.ChatMessage(string.Format(msg("Notification.Create.Success", ownerPlayer.UserIDString), tag));
                                OnPlayerConnected(ownerPlayer);
                            }

                            Interface.CallHook("OnClanCreate", tag);

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) created the clan [{tag}] for {owner.Name}", this);

                            SendReply(arg, $"You created the clan {tag} and set {owner.Name} as the owner");
                        }
                        return;
                    case "reserve":
                        if (authLevel >= configData.Commands.Auth.Create)
                        {
                            if (arg.Args.Length < 2)
                            {
                                SendReply(arg, "Usage: clans reserve \'tag(case-sensitive)\' \'steamid(optional)\'");
                                return;
                            }

                            string tag = arg.Args[1];
                            ulong steamid = arg.GetUInt64(2);

                            if (tag.Length < configData.Tags.TagLength.Minimum || tag.Length > configData.Tags.TagLength.Maximum)
                            {
                                SendReply(arg, $"Invalid tag length, it must be between {configData.Tags.TagLength.Minimum} and {configData.Tags.TagLength.Maximum} characters long");
                                return;
                            }

                            if (tagFilter.IsMatch(tag) || ContainsBlockedWord(tag))
                            {
                                SendReply(arg, "Invalid characters or blocked words detected in tag");
                                return;
                            }

                            if (steamid != 0UL)
                            {
                                configData.Tags.ReservedClanTags[tag] = new List<ulong> { steamid };
                                SendReply(arg, $"You have reserved the clan tag '{tag}' to Steam ID {steamid}");
                            }
                            else
                            {
                                Clan clan = storedData.FindClan(tag);
                                if (clan == null)
                                {
                                    SendReply(arg, $"No clan found with the tag : {tag}");
                                    return;
                                }

                                configData.Tags.ReservedClanTags[tag] = new List<ulong>(clan.ClanMembers.Keys);
                                SendReply(arg, $"You have reserved the clan tag '{tag}' to Steam IDs : {clan.ClanMembers.Keys.ToSentence()}");
                            }

                            SaveConfig();
                        }
                        return;
                    case "rename":
                        if (authLevel >= configData.Commands.Auth.Rename)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans rename \'oldtag(case-sensitive)\' \'newtag(case-sensitive)\'");
                                return;
                            }

                            string oldTag = arg.Args[1];
                            Clan clan = storedData.FindClan(oldTag);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            string newTag = arg.Args[2];

                            if (newTag.Length < configData.Tags.TagLength.Minimum || newTag.Length > configData.Tags.TagLength.Maximum)
                            {
                                SendReply(arg, $"Invalid tag length, it must be between {configData.Tags.TagLength.Minimum} and {configData.Tags.TagLength.Maximum} characters long");
                                return;
                            }

                            if (ClanTagExists(newTag))
                            {
                                SendReply(arg, "A clan with that tag already exists");
                                return;
                            }

                            clan.Tag = newTag;
                            storedData.clans[newTag] = clan;
                            storedData.clans.Remove(oldTag);

                            foreach (Clan otherClan in storedData.clans.Values)
                            {
                                if (otherClan == clan)
                                    continue;

                                if (otherClan.Alliances.Contains(oldTag))
                                {
                                    otherClan.Alliances.Remove(oldTag);
                                    otherClan.Alliances.Add(newTag);
                                    otherClan.MarkDirty();
                                }

                                if (otherClan.AllianceInvites.ContainsKey(oldTag))
                                {
                                    double time = otherClan.AllianceInvites[oldTag];
                                    otherClan.AllianceInvites.Remove(oldTag);
                                    otherClan.AllianceInvites.Add(newTag, time);
                                }
                            }

                            if (configData.Permissions.PermissionGroups)
                            {
                                permission.RemoveGroup(configData.Permissions.PermissionGroupPrefix + oldTag);
                                permission.CreateGroup(configData.Permissions.PermissionGroupPrefix + newTag, "Clan " + newTag, 0);
                            }

                            foreach (KeyValuePair<ulong, Clan.Member> kvp in clan.ClanMembers)
                            {
                                storedData.RegisterPlayer(kvp.Key, newTag);
                                if (kvp.Value.Player != null)
                                    OnPlayerConnected(kvp.Value.Player);
                            }

                            storedData.OnClanRenamed(oldTag, newTag);

                            clan.LocalizedBroadcast("Admin.Rename", newTag);

                            clan.MarkDirty();

                            SendReply(arg, $"You have changed the tag for clan {oldTag} to {newTag}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) renamed clan tag {oldTag} to {newTag}", this);
                        }
                        return;
                    case "disband":
                        if (authLevel >= configData.Commands.Auth.Disband)
                        {
                            if (arg.Args.Length < 2)
                            {
                                SendReply(arg, "Usage: clans disband \'tag(case-sensitive)\'");
                                return;
                            }

                            Clan clan = storedData.FindClan(arg.Args[1]);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            clan.LocalizedBroadcast("Admin.Disband");
                            clan.DisbandClan();

                            SendReply(arg, $"You have disbanded the clan {arg.Args[1]}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) disbanded the clan {arg.Args[1]}", this);
                        }
                        return;
                    case "invite":
                        if (authLevel >= configData.Commands.Auth.Invite)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans invite \'tag(case-sensitive)\' \'partialNameOrId\'");
                                return;
                            }

                            Clan clan = storedData.FindClan(arg.Args[1]);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            BasePlayer player = FindPlayer(arg.Args[2]);
                            if (!player)
                            {
                                SendReply(arg, "Unable to find a player with the specified name or ID");
                                return;
                            }

                            if (storedData.FindClanByID(player.userID) != null)
                            {
                                SendReply(arg, "The specified player is already a member of a clan");
                                return;
                            }

                            if (clan.ClanMembers.ContainsKey(player.userID))
                            {
                                SendReply(arg, "The specified player is already a member of that clan");
                                return;
                            }

                            if (clan.MemberInvites.ContainsKey(player.userID))
                            {
                                SendReply(arg, "The specified player already has a invitation to join that clan");
                                return;
                            }

                            if (clan.MemberCount >= configData.Clans.MemberLimit)
                            {
                                SendReply(arg, "The specified clan is already at capacity");
                                return;
                            }

                            if (clan.MemberInviteCount >= configData.Clans.Invites.MemberInviteLimit)
                            {
                                SendReply(arg, "The specified clan already has the maximum amount of invitations");
                                return;
                            }

                            clan.MemberInvites[player.userID] = new Clan.MemberInvite(player);

                            if (configData.Clans.Teams.Enabled)
                                clan.PlayerTeam.SendInvite(player);

                            player.ChatMessage(string.Format(msg("Notification.Invite.SuccesTarget", player.UserIDString), clan.Tag, clan.Description, configData.Commands.ClanCommand));
                            clan.LocalizedBroadcast("Admin.Invite", player.displayName);

                            SendReply(arg, $"You have invited {player.displayName} to clan {clan.Tag}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) invited {player.displayName} to the clan {arg.Args[1]}", this);
                        }
                        return;
                    case "join":
                        if (authLevel >= configData.Commands.Auth.Invite)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans join \'tag(case-sensitive)\' \'partialNameOrId\'");
                                return;
                            }

                            string tag = arg.Args[1];

                            Clan clan = storedData.FindClan(tag);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            BasePlayer player = FindPlayer(arg.Args[2]);
                            if (!player)
                            {
                                SendReply(arg, "Unable to find a player with the specified name or ID");
                                return;
                            }

                            Clan otherClan = storedData.FindClanByID(player.userID);
                            if (otherClan != null)
                            {
                                SendReply(arg, "The specified player is already in a clan");
                                return;
                            }

                            if (!clan.MemberInvites.ContainsKey(player.userID))
                            {
                                SendReply(arg, "The specified player does not have a invite to that clan");
                                return;
                            }

                            if ((UnixTimeStampUTC() - clan.MemberInvites[player.userID].ExpiryTime > configData.Clans.Invites.AllianceInviteExpireTime))
                            {
                                clan.MemberInvites.Remove(player.userID);
                                SendReply(arg, "The specified players clan invite has expired");
                                return;
                            }

                            if (clan.MemberCount >= configData.Clans.MemberLimit)
                            {
                                SendReply(arg, "The specified clan is already at member capacity");
                                return;
                            }

                            clan.MemberInvites.Remove(player.userID);

                            List<ulong> currentMembers = clan.ClanMembers.Select(x => x.Key).ToList();

                            clan.ClanMembers.Add(player.userID, new Clan.Member(Clan.Member.MemberRole.Member, clan));

                            Instance.storedData.RegisterPlayer(player.userID, clan.Tag);

                            OnPlayerConnected(player);

                            player.ChatMessage(string.Format(msg("Admin.Join", player.UserIDString), clan.Tag));
                            clan.LocalizedBroadcast("Notification.Join.Message", player.displayName);

                            clan.MarkDirty();

                            Interface.Oxide.CallHook("OnClanMemberJoined", (ulong)player.userID, clan.Tag);
                            Interface.Oxide.CallHook("OnClanMemberJoined", (ulong)player.userID, currentMembers);

                            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);

                            SendReply(arg, $"You have force joined {player.displayName} to clan {clan.Tag}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) forced {player.displayName} to join the clan {arg.Args[1]}", this);
                        }
                        return;
                    case "kick":
                        if (authLevel >= configData.Commands.Auth.Kick)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans kick \'tag(case-sensitive)\' \'partialNameOrId\'");
                                return;
                            }

                            string tag = arg.Args[1];
                            Clan clan = storedData.FindClan(tag);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            BasePlayer player = FindPlayer(arg.Args[2]);
                            if (!player)
                            {
                                SendReply(arg, "Unable to find a player with the specified name or ID");
                                return;
                            }

                            if (!clan.ClanMembers.ContainsKey(player.userID))
                            {
                                SendReply(arg, "The specified user is not in that clan");
                                return;
                            }

                            if (configData.Clans.Teams.Enabled && clan.PlayerTeam != null)
                                RemoveFromTeam(clan.PlayerTeam, player);

                            OnPlayerDisconnected(player);
                            clan.ClanMembers.Remove(player.userID);
                            storedData.UnregisterPlayer(player.userID);

                            player.ChatMessage(string.Format(msg("Admin.Kick", player.UserIDString), clan.Tag));
                            clan.LocalizedBroadcast("Admin.Kick.Broadcast", player.displayName);

                            clan.MarkDirty();

                            Interface.Oxide.CallHook("OnClanMemberGone", (ulong)player.userID, clan.Tag);
                            Interface.Oxide.CallHook("OnClanMemberGone", (ulong)player.userID, clan.ClanMembers.Select(x => x.Key).ToList());

                            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);

                            SendReply(arg, $"You have kicked {player.displayName} from clan {clan.Tag}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) kicked {player.displayName} from the clan {clan.Tag}", this);
                        }
                        return;
                    case "owner":
                        if (authLevel >= configData.Commands.Auth.Promote)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans owner \'tag(case-sensitive)\' \'partialNameOrId\'");
                                return;
                            }

                            string tag = arg.Args[1];

                            Clan clan = storedData.FindClan(tag);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            BasePlayer player = FindPlayer(arg.Args[2]);
                            if (!player)
                            {
                                SendReply(arg, "Unable to find a player with the specified name or ID");
                                return;
                            }

                            if (!clan.ClanMembers.ContainsKey(player.userID))
                            {
                                SendReply(arg, "The specified player is not a member of that clan");
                                return;
                            }

                            if (clan.IsOwner(player.userID))
                            {
                                SendReply(arg, "The specified player is already the clan owner");
                                return;
                            }

                            Clan.Member currentOwner = clan.ClanMembers[clan.OwnerID];
                            currentOwner.Role = Clan.Member.MemberRole.Member;

                            Clan.Member member = clan.ClanMembers[player.userID];
                            member.Role = Clan.Member.MemberRole.Owner;
                            clan.OwnerID = player.userID;

                            if (configData.Clans.Teams.Enabled)
                                clan.PlayerTeam?.SetTeamLeader(player.userID);

                            clan.LocalizedBroadcast("Admin.SetOwner", string.Format(COLORED_LABEL, clan.GetRoleColor(Clan.Member.MemberRole.Owner), player.displayName));

                            clan.MarkDirty();

                            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);

                            SendReply(arg, $"You have set {player.displayName} as owner of the clan {clan.Tag}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) set {player.displayName} as the owner of the clan {clan.Tag}", this);
                        }
                        return;
                    case "promote":
                        if (authLevel >= configData.Commands.Auth.Promote)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans promote \'tag(case-sensitive)\' \'partialNameOrId\'");
                                return;
                            }

                            string tag = arg.Args[1];

                            Clan clan = storedData.FindClan(tag);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            BasePlayer player = FindPlayer(arg.Args[2]);
                            if (!player)
                            {
                                SendReply(arg, "Unable to find a player with the specified name or ID");
                                return;
                            }

                            if (!clan.ClanMembers.ContainsKey(player.userID))
                            {
                                SendReply(arg, "The specified player is not a member of that clan");
                                return;
                            }

                            if (clan.IsOwner(player.userID))
                            {
                                SendReply(arg, "The specified player is already the clan owner");
                                return;
                            }

                            if (configData.Clans.Alliance.Enabled)
                            {
                                if (clan.IsCouncil(player.userID))
                                {
                                    SendReply(arg, "The specified player is already the highest rank they can be");
                                    return;
                                }

                                if (clan.IsModerator(player.userID) && clan.HasCouncil())
                                {
                                    SendReply(arg, "The specified player is already the highest rank they can be");
                                    return;
                                }
                            }
                            else
                            {
                                if (clan.IsModerator(player.userID))
                                {
                                    SendReply(arg, "The specified player is already the highest rank they can be");
                                    return;
                                }
                            }

                            if (clan.IsMember(player.userID) && clan.ModeratorCount >= configData.Clans.ModeratorLimit)
                            {
                                SendReply(arg, "The specified player is already the highest rank they can be");
                                return;
                            }

                            Clan.Member member = clan.ClanMembers[player.userID];
                            member.Role = (Clan.Member.MemberRole)((int)member.Role - 1);

                            clan.MarkDirty();

                            clan.LocalizedBroadcast("Admin.Promote", player.displayName, string.Format(COLORED_LABEL, clan.GetRoleColor(member.Role), member.Role));
                            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);

                            SendReply(arg, $"You have promoted {player.displayName} to rank {member.Role} in clan {clan.Tag}");

                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) promoted {player.displayName} to the rank of {member.Role} in the clan {clan.Tag}", this);
                        }
                        return;
                    case "demote":
                        if (authLevel >= configData.Commands.Auth.Promote)
                        {
                            if (arg.Args.Length < 3)
                            {
                                SendReply(arg, "Usage: clans demote \'tag(case-sensitive)\' \'partialNameOrId\'");
                                return;
                            }

                            string tag = arg.Args[1];

                            Clan clan = storedData.FindClan(tag);
                            if (clan == null)
                            {
                                SendReply(arg, "No clan found with the specified tag");
                                return;
                            }

                            BasePlayer player = FindPlayer(arg.Args[2]);
                            if (!player)
                            {
                                SendReply(arg, "Unable to find a player with the specified name or ID");
                                return;
                            }

                            if (clan.IsOwner(player.userID))
                            {
                                SendReply(arg, "You can not demote the clan owner");
                                return;
                            }

                            if (clan.IsMember(player.userID))
                            {
                                SendReply(arg, "The specified player is already at the lowest rank");
                                return;
                            }

                            Clan.Member member = clan.ClanMembers[player.userID];
                            member.Role = member.Role == Clan.Member.MemberRole.Council ? (clan.ModeratorCount >= configData.Clans.ModeratorLimit ? Clan.Member.MemberRole.Member : Clan.Member.MemberRole.Moderator) : (Clan.Member.MemberRole)((int)member.Role + 1);

                            clan.MarkDirty();

                            clan.LocalizedBroadcast("Admin.Demote", player.displayName, string.Format(COLORED_LABEL, clan.GetRoleColor(member.Role), member.Role));

                            SendReply(arg, $"You have demoted {player.displayName} to rank {member.Role} in clan {clan.Tag}");

                            Interface.Oxide.CallHook("OnClanUpdate", clan.Tag);
                            if (configData.Options.LogChanges)
                                LogToFile(Title, $"Server ({(isRcon ? "ADMIN" : (arg.Connection.player as BasePlayer).displayName)}) demoted {player.displayName} to the rank of {member.Role} in the clan {clan.Tag}", this);
                        }
                        return;
                    default:
                        SendReply(arg, "Invalid syntax! Type \"clans\" in console to see available commands");
                        return;
                }
            }
        }
        #endregion

        #region Config        
        internal static ConfigData configData;

        internal class ConfigData
        {
            [JsonProperty(PropertyName = "Clan Options")]
            public ClanOptions Clans { get; set; }

            [JsonProperty(PropertyName = "Command Options")]
            public CommandOptions Commands { get; set; }

            [JsonProperty(PropertyName = "Role Colors")]
            public ColorOptions Colors { get; set; }

            [JsonProperty(PropertyName = "Clan Tag Options")]
            public TagOptions Tags { get; set; }

            [JsonProperty(PropertyName = "Permission Options")]
            public PermissionOptions Permissions { get; set; }

            [JsonProperty(PropertyName = "Purge Options")]
            public PurgeOptions Purge { get; set; }

            [JsonProperty(PropertyName = "Settings")]
            public OtherOptions Options { get; set; }

            public class ClanOptions
            {
                [JsonProperty(PropertyName = "Member limit")]
                public int MemberLimit { get; set; }

                [JsonProperty(PropertyName = "Moderator limit")]
                public int ModeratorLimit { get; set; }

                [JsonProperty(PropertyName = "Allow friendly fire toggle (clan members)")]
                public bool MemberFF { get; set; }

                [JsonProperty(PropertyName = "Enable friendly fire by default (clan members)")]
                public bool DefaultEnableFF { get; set; }

                [JsonProperty(PropertyName = "Only allow clan owner and council to toggle friendly fire (clan members)")]
                public bool OwnerFF { get; set; }

                [JsonProperty(PropertyName = "Alliance Options")]
                public AllianceOptions Alliance { get; set; }

                [JsonProperty(PropertyName = "Invite Options")]
                public InviteOptions Invites { get; set; }

                [JsonProperty(PropertyName = "Rust Team Options")]
                public TeamOptions Teams { get; set; }

                public class AllianceOptions
                {
                    [JsonProperty(PropertyName = "Enable clan alliances")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Alliance limit")]
                    public int AllianceLimit { get; set; }

                    [JsonProperty(PropertyName = "Count alliance members as clan members")]
                    public bool CountAllianceMembers { get; set; }

                    [JsonProperty(PropertyName = "Allow friendly fire toggle (allied clans)")]
                    public bool AllyFF { get; set; }

                    [JsonProperty(PropertyName = "Enable friendly fire by default (allied clans)")]
                    public bool DefaultEnableFF { get; set; }

                    [JsonProperty(PropertyName = "Only allow clan owner and council to toggle friendly fire (allied clans)")]
                    public bool OwnerFF { get; set; }
                }

                public class InviteOptions
                {
                    [JsonProperty(PropertyName = "Maximum allowed member invites at any given time")]
                    public int MemberInviteLimit { get; set; }

                    [JsonProperty(PropertyName = "Member invite expiry time (seconds)")]
                    public int MemberInviteExpireTime { get; set; }

                    [JsonProperty(PropertyName = "Maximum allowed alliance invites at any given time")]
                    public int AllianceInviteLimit { get; set; }

                    [JsonProperty(PropertyName = "Alliance invite expiry time (seconds)")]
                    public int AllianceInviteExpireTime { get; set; }

                    [JsonProperty(PropertyName = "Member invite cooldown time after a member has left the clan (seconds)")]
                    public int InviteCooldownAfterMemberLeave { get; set; }
                }

                public class TeamOptions
                {
                    [JsonProperty(PropertyName = "Automatically create and manage Rust team's for each clan")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Allow players to leave their clan by using Rust's leave team button")]
                    public bool AllowLeave { get; set; }

                    [JsonProperty(PropertyName = "Allow players to kick members from their clan using Rust's kick member button")]
                    public bool AllowKick { get; set; }

                    [JsonProperty(PropertyName = "Allow players to invite other players to their clan via Rust's team invite system")]
                    public bool AllowInvite { get; set; }

                    [JsonProperty(PropertyName = "Allow players to promote other clan members via Rust's team promote button")]
                    public bool AllowPromote { get; set; }
                }
            }

            public class ColorOptions
            {
                [JsonProperty(PropertyName = "Clan owner color (hex)")]
                public string Owner { get; set; }

                [JsonProperty(PropertyName = "Clan council color (hex)")]
                public string Council { get; set; }

                [JsonProperty(PropertyName = "Clan moderator color (hex)")]
                public string Moderator { get; set; }

                [JsonProperty(PropertyName = "Clan member color (hex)")]
                public string Member { get; set; }

                [JsonProperty(PropertyName = "General text color (hex)")]
                public string TextColor { get; set; }
            }

            public class TagOptions
            {
                [JsonProperty(PropertyName = "Enable clan tags (Display Name)")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Enable clan tags (BetterChat)")]
                public bool EnabledBC { get; set; }

                [JsonProperty(PropertyName = "Tag opening character")]
                public string TagOpen { get; set; }

                [JsonProperty(PropertyName = "Tag closing character")]
                public string TagClose { get; set; }

                [JsonProperty(PropertyName = "Tag color (hex) (BetterChat)")]
                public string TagColor { get; set; }

                [JsonProperty(PropertyName = "Allow clan leaders to set custom tag colors (BetterChat only)")]
                public bool CustomColors { get; set; }

                [JsonProperty(PropertyName = "Custom tag color minimum value (hex)")]
                public string CustomTagColorMin { get; set; }

                [JsonProperty(PropertyName = "Custom tag color maximum value (hex)")]
                public string CustomTagColorMax { get; set; }

                [JsonProperty(PropertyName = "Blacklisted tag colors (hex without the # at the start)")]
                public string[] BlockedTagColors { get; set; }

                [JsonProperty(PropertyName = "Tag size (BetterChat)")]
                public int TagSize { get; set; }

                [JsonProperty(PropertyName = "Tag character limits")]
                public Range TagLength { get; set; }

                [JsonProperty(PropertyName = "Special characters allowed in tags")]
                public string AllowedCharacters { get; set; }

                [JsonProperty(PropertyName = "Words/characters not allowed in tags")]
                public string[] BlockedWords { get; set; }

                [JsonProperty(PropertyName = "Enable Oxide group clan tag colors (BetterChat)")]
                public bool EnabledGroupColors { get; set; }

                [JsonProperty(PropertyName = "Default tag colors per Oxide group (BetterChat)")]
                public Hash<string, string> GroupTagColors { get; set; }

                [JsonProperty(PropertyName = "Reserved clan tags (Tag, List of SteamIDs)")]
                public Hash<string, List<ulong>> ReservedClanTags { get; set; }
            }

            public class CommandOptions
            {
                [JsonProperty(PropertyName = "Ally chat command")]
                public string AllyChatCommand { get; set; }

                [JsonProperty(PropertyName = "Clan chat command")]
                public string ClanChatCommand { get; set; }

                [JsonProperty(PropertyName = "Clan command")]
                public string ClanCommand { get; set; }

                [JsonProperty(PropertyName = "Clan info command")]
                public string ClanInfoCommand { get; set; }

                [JsonProperty(PropertyName = "Ally friendly fire command")]
                public string AFFCommand { get; set; }

                [JsonProperty(PropertyName = "Friendly fire command")]
                public string FFCommand { get; set; }

                [JsonProperty(PropertyName = "Clan ally command")]
                public string ClanAllyCommand { get; set; }

                [JsonProperty(PropertyName = "Clan help command")]
                public string ClanHelpCommand { get; set; }

                [JsonProperty(PropertyName = "Required auth-levels to use admin console command")]
                public AdminAuth Auth { get; set; }

                [JsonProperty(PropertyName = "Clan info options")]
                public ClanInfo ClanInfoOptions { get; set; }

                public class ClanInfo
                {
                    [JsonProperty(PropertyName = "Show online/offline players")]
                    public bool Players { get; set; }

                    [JsonProperty(PropertyName = "Show clan alliances")]
                    public bool Alliances { get; set; }

                    [JsonProperty(PropertyName = "Show last online time")]
                    public bool LastOnline { get; set; }

                    [JsonProperty(PropertyName = "Show member count")]
                    public bool MemberCount { get; set; }

                    [JsonProperty(PropertyName = "Show alliance count")]
                    public bool AllianceCount { get; set; }
                }

                public class AdminAuth
                {
                    [JsonProperty(PropertyName = "Create clan")]
                    public int Create { get; set; }

                    [JsonProperty(PropertyName = "Rename clan")]
                    public int Rename { get; set; }

                    [JsonProperty(PropertyName = "Disband clan")]
                    public int Disband { get; set; }

                    [JsonProperty(PropertyName = "Invite member to clan")]
                    public int Invite { get; set; }

                    [JsonProperty(PropertyName = "Kick member from clan")]
                    public int Kick { get; set; }

                    [JsonProperty(PropertyName = "Promote/Demote member in clan")]
                    public int Promote { get; set; }

                    [JsonProperty(PropertyName = "Reserve clan tag for member or group")]
                    public int Reserve { get; set; }
                }
            }

            public class PermissionOptions
            {
                [JsonProperty(PropertyName = "Use permission for clan info command")]
                public bool UsePermissionClanInfo { get; set; }

                [JsonProperty(PropertyName = "Clan info command permission")]
                public string ClanInfoPermission { get; set; }

                [JsonProperty(PropertyName = "Use permission groups")]
                public bool PermissionGroups { get; set; }

                [JsonProperty(PropertyName = "Permission group prefix")]
                public string PermissionGroupPrefix { get; set; }

                [JsonProperty(PropertyName = "Use permission to create a clan")]
                public bool UsePermissionCreate { get; set; }

                [JsonProperty(PropertyName = "Clan creation permission")]
                public string PermissionCreate { get; set; }

                [JsonProperty(PropertyName = "Use permission to join a clan")]
                public bool UsePermissionJoin { get; set; }

                [JsonProperty(PropertyName = "Clan join permission")]
                public string PermissionJoin { get; set; }

                [JsonProperty(PropertyName = "Use permission to leave a clan")]
                public bool UsePermissionLeave { get; set; }

                [JsonProperty(PropertyName = "Clan leave permission")]
                public string PermissionLeave { get; set; }

                [JsonProperty(PropertyName = "Use permission to disband a clan")]
                public bool UsePermissionDisband { get; set; }

                [JsonProperty(PropertyName = "Clan disband permission")]
                public string PermissionDisband { get; set; }

                [JsonProperty(PropertyName = "Use permission to kick a clan member")]
                public bool UsePermissionKick { get; set; }

                [JsonProperty(PropertyName = "Clan kick permission")]
                public string PermissionKick { get; set; }
            }

            public class PurgeOptions
            {
                [JsonProperty(PropertyName = "Enable clan purging")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Purge clans that havent been online for x amount of day")]
                public int OlderThanDays { get; set; }

                [JsonProperty(PropertyName = "List purged clans in console when purging")]
                public bool ListPurgedClans { get; set; }

                [JsonProperty(PropertyName = "Wipe clans on new map save")]
                public bool WipeOnNewSave { get; set; }
            }

            public class OtherOptions
            {
                [JsonProperty(PropertyName = "Block clan/ally chat when muted")]
                public bool DenyOnMuted { get; set; }

                [JsonProperty(PropertyName = "Log clan and member changes")]
                public bool LogChanges { get; set; }

                [JsonProperty(PropertyName = "Use ProtoBuf data storage")]
                public bool UseProtoStorage { get; set; }
            }

            public class Range
            {
                public int Minimum { get; set; }
                public int Maximum { get; set; }

                public Range() { }

                public Range(int minimum, int maximum)
                {
                    this.Minimum = minimum;
                    this.Maximum = maximum;
                }
            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Clans = new ConfigData.ClanOptions
                {
                    Alliance = new ConfigData.ClanOptions.AllianceOptions
                    {
                        AllianceLimit = 2,
                        AllyFF = true,
                        DefaultEnableFF = false,
                        OwnerFF = false,
                        Enabled = true
                    },
                    Invites = new ConfigData.ClanOptions.InviteOptions
                    {
                        AllianceInviteExpireTime = 86400,
                        AllianceInviteLimit = 2,
                        MemberInviteExpireTime = 86400,
                        MemberInviteLimit = 8,
                        InviteCooldownAfterMemberLeave = 0
                    },
                    MemberFF = true,
                    DefaultEnableFF = false,
                    OwnerFF = false,
                    MemberLimit = 8,
                    ModeratorLimit = 2,
                    Teams = new ConfigData.ClanOptions.TeamOptions
                    {
                        AllowInvite = true,
                        AllowKick = true,
                        AllowLeave = true,
                        AllowPromote = true,
                        Enabled = true
                    }
                },
                Colors = new ConfigData.ColorOptions
                {
                    Council = "#b573ff",
                    Member = "#fcf5cb",
                    Moderator = "#74c6ff",
                    Owner = "#a1ff46",
                    TextColor = "#e0e0e0"
                },
                Commands = new ConfigData.CommandOptions
                {
                    AllyChatCommand = "a",
                    ClanChatCommand = "c",
                    ClanAllyCommand = "clanally",
                    ClanCommand = "clan",
                    ClanHelpCommand = "clanhelp",
                    ClanInfoCommand = "cinfo",
                    FFCommand = "cff",
                    AFFCommand = "aff",
                    Auth = new ConfigData.CommandOptions.AdminAuth
                    {
                        Create = 2,
                        Disband = 2,
                        Invite = 1,
                        Kick = 2,
                        Promote = 1,
                        Rename = 1,
                        Reserve = 2
                    },
                    ClanInfoOptions = new ConfigData.CommandOptions.ClanInfo
                    {
                        Alliances = true,
                        LastOnline = true,
                        Players = true,
                        AllianceCount = false,
                        MemberCount = false
                    }
                },
                Options = new ConfigData.OtherOptions
                {
                    LogChanges = false,
                    UseProtoStorage = false,
                    DenyOnMuted = false,
                },
                Permissions = new ConfigData.PermissionOptions
                {
                    UsePermissionClanInfo = false,
                    ClanInfoPermission = "clans.claninfo",
                    PermissionCreate = "clans.cancreate",
                    PermissionDisband = "clans.candisband",
                    PermissionGroupPrefix = "clan_",
                    PermissionGroups = false,
                    PermissionJoin = "clans.canjoin",
                    PermissionLeave = "clans.canleave",
                    PermissionKick = "clans.cankick",
                    UsePermissionDisband = false,
                    UsePermissionLeave = false,
                    UsePermissionCreate = false,
                    UsePermissionJoin = false,
                    UsePermissionKick = false,
                },
                Purge = new ConfigData.PurgeOptions
                {
                    Enabled = true,
                    ListPurgedClans = true,
                    OlderThanDays = 14,
                    WipeOnNewSave = false
                },
                Tags = new ConfigData.TagOptions
                {
                    AllowedCharacters = "!²³",
                    BlockedWords = new string[] { "admin", "mod", "owner" },
                    CustomColors = false,
                    Enabled = true,
                    TagClose = "]",
                    TagColor = "#aaff55",
                    TagLength = new ConfigData.Range(2, 5),
                    TagOpen = "[",
                    TagSize = 15,
                    CustomTagColorMin = "000000",
                    CustomTagColorMax = "FFFFFF",
                    BlockedTagColors = Array.Empty<string>(),
                    EnabledBC = false,
                    EnabledGroupColors = false,
                    GroupTagColors = new Hash<string, string>
                    {
                        ["admin"] = "#aaff55",
                        ["default"] = "#aaff55"
                    },
                    ReservedClanTags = new Hash<string, List<ulong>>
                    {
                        ["example1"] = new List<ulong>
                        {
                            76560000000000000,
                            76560000000000001
                        },
                        ["example2"] = new List<ulong>
                        {
                            76560000000000000,
                            76560000000000001
                        }
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(3, 0, 0))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(3, 0, 1))
                configData.Tags.TagColor = baseConfig.Tags.TagColor;

            if (configData.Version < new VersionNumber(3, 0, 13))
            {
                configData.Tags.CustomTagColorMin = baseConfig.Tags.CustomTagColorMin;
                configData.Tags.CustomTagColorMax = baseConfig.Tags.CustomTagColorMax;
            }

            if (configData.Version < new VersionNumber(3, 0, 14))
            {
                configData.Permissions.UsePermissionLeave = baseConfig.Permissions.UsePermissionLeave;
                configData.Permissions.UsePermissionDisband = baseConfig.Permissions.UsePermissionDisband;
                configData.Permissions.PermissionDisband = baseConfig.Permissions.PermissionDisband;
                configData.Permissions.PermissionLeave = baseConfig.Permissions.PermissionLeave;
            }

            if (configData.Version < new VersionNumber(3, 0, 15))
            {
                configData.Permissions.UsePermissionKick = baseConfig.Permissions.UsePermissionKick;
                configData.Permissions.PermissionKick = baseConfig.Permissions.PermissionKick;
            }

            if (configData.Version < new VersionNumber(3, 0, 23))
            {
                configData.Tags.Enabled = configData.Tags.EnabledBC = true;
                configData.Commands.ClanInfoOptions = baseConfig.Commands.ClanInfoOptions;
                configData.Tags.BlockedTagColors = Array.Empty<string>();
            }

            if (configData.Version < new VersionNumber(3, 0, 25))
            {
                if (configData.Tags.BlockedTagColors == null)
                    configData.Tags.BlockedTagColors = Array.Empty<string>();
            }

            if (configData.Version < new VersionNumber(3, 0, 28))
            {
                configData.Tags.EnabledGroupColors = false;
                configData.Tags.GroupTagColors = baseConfig.Tags.GroupTagColors;
            }

            if (configData.Version < new VersionNumber(3, 0, 30))
            {
                configData.Tags.ReservedClanTags = baseConfig.Tags.ReservedClanTags;
                configData.Commands.Auth.Reserve = baseConfig.Commands.Auth.Reserve;
            }

            if (configData.Version < new VersionNumber(3, 0, 32))
                configData.Permissions.ClanInfoPermission = baseConfig.Permissions.ClanInfoPermission;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion

        #region Data Management
        private void SaveData()
        {
            storedData.timeSaved = UnixTimeStampUTC();

            if (configData.Options.UseProtoStorage)
                ProtoStorage.Save<StoredData>(storedData, Title);
            else Interface.Oxide.DataFileSystem.WriteObject(Title, storedData);
        }

        private void LoadData()
        {
            try
            {
                StoredData protoStorage = ProtoStorage.Exists(Title) ? ProtoStorage.Load<StoredData>(new string[] { Title }) : null;
                StoredData jsonStorage = Interface.GetMod().DataFileSystem.ExistsDatafile(Title) ? Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title) : null;

                if (protoStorage == null && jsonStorage == null)
                {
                    Puts("No data file found! Creating new data file");
                    storedData = new StoredData();
                }
                else
                {
                    if (protoStorage == null && jsonStorage != null)
                        storedData = jsonStorage;
                    else if (protoStorage != null && jsonStorage == null)
                        storedData = protoStorage;
                    else
                    {
                        if (protoStorage.timeSaved > jsonStorage.timeSaved)
                        {
                            storedData = protoStorage;
                            Puts("Multiple data files found! ProtoBuf storage time stamp is newer than JSON storage. Loading ProtoBuf data file");
                        }
                        else
                        {
                            storedData = jsonStorage;
                            Puts("Multiple data files found! JSON storage time stamp is newer than ProtoBuf storage. Loading JSON data file");
                        }
                    }
                }

                if (wipeData)
                {
                    Puts("Backing up data file for data wipe...");
                    if (configData.Options.UseProtoStorage)
                    {
                        ProtoStorage.Save<StoredData>(storedData, Title + ".bak");
                        Puts($"Saved data file backup as {Title}.bak.data");
                    }
                    else
                    {
                        Interface.Oxide.DataFileSystem.WriteObject<StoredData>(Title + ".bak", storedData);
                        Puts($"Saved data file backup as {Title}.bak.json");
                    }

                    storedData.clans.Clear();
                }
            }
            catch { }

            if (storedData?.clans == null)
                storedData = new StoredData();
        }

        [ConsoleCommand("clans.convertdata")]
        private void ConvertFromOldData(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "clans.convertdata <filename> - Load an old Clans data file and convert it to the new data structure\nThe file name is the name of the file without the extension, it must be a old .json or .proto file, and it must be in your /oxide/data/ folder");
                return;
            }

            string filename = arg.Args[0];

            if (!Interface.GetMod().DataFileSystem.ExistsDatafile(filename) && !ProtoStorage.Exists(new string[] { filename }))
            {
                SendReply(arg, "Unable to find a valid data file with that name");
                return;
            }

            OldDataStructure oldDataStructure = Interface.GetMod().DataFileSystem.ReadObject<OldDataStructure>(filename) ?? ProtoStorage.Load<OldDataStructure>(new string[] { filename });
            if (oldDataStructure == null)
            {
                SendReply(arg, $"Failed to deserialize old data file with the name {filename}");
                return;
            }

            ConvertFromOldData(arg, oldDataStructure);
        }

        private void ConvertFromOldData(ConsoleSystem.Arg arg, OldDataStructure oldData)
        {
            if (oldData.clans.Count == 0)
            {
                SendReply(arg, "No clans exist in the specified data file");
                return;
            }

            SendReply(arg, $"Converting {oldData.clans.Count} old clans to new data structure...");

            storedData = new StoredData();

            foreach (KeyValuePair<string, OldClan> kvp in oldData.clans)
            {
                OldClan oldClan = kvp.Value;

                if (string.IsNullOrEmpty(oldClan.tag) || string.IsNullOrEmpty(oldClan.owner))
                    continue;

                Clan clan = new Clan();
                clan.Tag = oldClan.tag;
                clan.Description = StripHTMLTags(oldClan.description) ?? string.Empty;
                clan.OwnerID = ulong.Parse(oldClan.owner);

                clan.CreationTime = clan.LastOnlineTime = UnixTimeStampUTC();

                oldClan.clanAlliances.ForEach((string x) => clan.Alliances.Add(x));

                oldClan.invitedAllies.ForEach((string x) => clan.AllianceInvites.Add(x, UnixTimeStampUTC()));

                foreach (KeyValuePair<string, int> invite in oldClan.invites)
                    clan.MemberInvites.Add(ulong.Parse(invite.Key), new Clan.MemberInvite() { DisplayName = string.Empty, ExpiryTime = invite.Value });

                oldClan.members.ForEach((string x) =>
                    clan.ClanMembers.Add(ulong.Parse(x),
                    new Clan.Member(!string.IsNullOrEmpty(oldClan.owner) && oldClan.owner.Equals(x) ? Clan.Member.MemberRole.Owner :
                                    !string.IsNullOrEmpty(oldClan.council) && oldClan.council.Equals(x) ? Clan.Member.MemberRole.Council :
                                    oldClan.moderators.Contains(x) ? Clan.Member.MemberRole.Moderator : Clan.Member.MemberRole.Member,
                                    configData.Clans.DefaultEnableFF, configData.Clans.Alliance.DefaultEnableFF)));

                storedData.clans[kvp.Key] = clan;
            }

            SaveData();

            SendReply(arg, $"Successfully converted {storedData.clans.Count} old clans to new data structure");

            if (initClansRoutine != null)
                ServerMgr.Instance.StopCoroutine(initClansRoutine);

            isInitialized = false;
            initClansRoutine = ServerMgr.Instance.StartCoroutine(InitializeClans());
        }

        [Serializable, ProtoContract]
        internal class StoredData
        {
            [ProtoMember(1)]
            public Hash<string, Clan> clans = new Hash<string, Clan>(StringComparer.OrdinalIgnoreCase);

            [ProtoMember(2)]
            public int timeSaved;

            [ProtoMember(3)]
            public Hash<ulong, List<string>> playerInvites = new Hash<ulong, List<string>>();

            [JsonIgnore, ProtoIgnore]
            private Hash<ulong, string> playerLookup = new Hash<ulong, string>();

            [JsonIgnore, ProtoIgnore]
            private Hash<string, string> clanLookup = new Hash<string, string>(StringComparer.OrdinalIgnoreCase);

            internal Clan FindClan(string tag)
            {
                if (clans.TryGetValue(tag, out Clan clan))
                    return clan;

                return null;
            }

            internal Clan FindClanByID(ulong playerId)
            {
                if (!playerLookup.TryGetValue(playerId, out string tag))
                    return null;

                return FindClan(tag);
            }

            internal Clan FindClanByID(string playerId) => FindClanByID(ulong.Parse(playerId));

            internal Clan.Member FindMemberByID(ulong playerId)
            {
                Clan.Member member = null;
                FindClanByID(playerId)?.ClanMembers.TryGetValue(playerId, out member);
                return member;
            }

            internal void RegisterPlayer(ulong playerId, string tag) => playerLookup[playerId] = tag;

            internal void UnregisterPlayer(ulong playerId) => playerLookup.Remove(playerId);

            internal void AddPlayerInvite(ulong target, string tag)
            {
                List<string> invites;
                if (!playerInvites.TryGetValue(target, out invites))
                    invites = playerInvites[target] = new List<string>();

                if (!invites.Contains(tag))
                    invites.Add(tag);
            }

            internal void RevokePlayerInvite(ulong target, string tag)
            {
                List<string> invites;
                if (!playerInvites.TryGetValue(target, out invites))
                    return;

                invites.Remove(tag);

                if (invites.Count == 0)
                    playerInvites.Remove(target);
            }

            internal void OnInviteAccepted(ulong target, string tag)
            {
                List<string> invites;
                if (!playerInvites.TryGetValue(target, out invites))
                    return;

                for (int i = invites.Count - 1; i >= 0; i--)
                {
                    string t = invites[i];

                    if (!t.Equals(tag))
                        FindClan(t)?.MemberInvites.Remove(target);

                    invites.RemoveAt(i);
                }

                if (invites.Count == 0)
                    playerInvites.Remove(target);
            }

            internal void OnInviteRejected(ulong target, string tag)
            {
                List<string> invites;
                if (!playerInvites.TryGetValue(target, out invites))
                    return;

                invites.Remove(tag);

                if (invites.Count == 0)
                    playerInvites.Remove(target);
            }

            internal void OnClanRenamed(string oldTag, string newTag)
            {
                foreach (KeyValuePair<ulong, List<string>> kvp in playerInvites)
                {
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        if (kvp.Value[i].Equals(oldTag))
                            kvp.Value[i] = newTag;
                    }
                }
            }
        }

        private class OldDataStructure
        {
            public Dictionary<string, OldClan> clans = new Dictionary<string, OldClan>();
            public int saveStamp = 0;
            public string lastStorage = string.Empty;
        }

        public class OldClan
        {
            public string tag;
            public string description;
            public string owner;
            public string council;
            public int created;
            public int updated;

            public List<string> moderators = new List<string>();
            public List<string> members = new List<string>();
            public Dictionary<string, int> invites = new Dictionary<string, int>();
            public List<string> clanAlliances = new List<string>();
            public List<string> invitedAllies = new List<string>();
            public List<string> pendingInvites = new List<string>();
        }
        #endregion

        #region Localization
        private static string msg(string key, string playerId = null) => string.Format(COLORED_LABEL, configData.Colors.TextColor, Instance.lang.GetMessage(key, Instance, playerId));

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Notification.ClanInfo.Title"] = "<size=18><color=#ffa500>Clans</color></size><size=14><color=#ce422b> REBORN</color></size>",
            ["Notification.ClanInfo.Tag"] = "\nClanTag: <color=#b2eece>{0}</color>",
            ["Notification.ClanInfo.Description"] = "\nDescription: <color=#b2eece>{0}</color>",
            ["Notification.ClanInfo.Online"] = "\nMembers Online: {0}",
            ["Notification.ClanInfo.Offline"] = "\nMembers Offline: {0}",
            ["Notification.ClanInfo.Members"] = "\nMembers: {0}",
            ["Notification.ClanInfo.Established"] = "\nEstablished: <color=#b2eece>{0}</color>",
            ["Notification.ClanInfo.LastOnline"] = "\nLast Online: <color=#b2eece>{0}</color>",
            ["Notification.ClanInfo.Alliances"] = "\nAlliances: <color=#b2eece>{0}</color>",
            ["Notification.ClanInfo.Alliances.None"] = "None",
            ["Notification.ClanInfo.TotalMembers"] = "\nMembers: <color=#b2eece>{0}</color>",
            ["Notification.ClanInfo.TotalAlliances"] = "\nAlliances: <color=#b2eece>{0}</color>",

            ["Notification.Create.InExistingClan"] = "You are already a member of a clan",
            ["Notification.Create.NoPermission"] = "You do not have permission to create a clan",
            ["Notification.Create.TagReserved1"] = "The tag {0} is reserved for another player",
            ["Notification.Create.InvalidTagLength"] = "The tag you have chosen is invalid. It must be between {0} and {1} characters long",
            ["Notification.Create.InvalidCharacters"] = "The tag you have chosen contains words/characters that are not allowed to be used",
            ["Notification.Create.ClanExists"] = "A clan with that tag already exists",
            ["Notification.Create.NoNativeCreate"] = "You must create a clan using /clan create <tag> <description>",
            ["Notification.Create.Success"] = "You have formed the clan <color=#aaff55>[{0}]</color>",

            ["Notification.Kick.IsOwner"] = "You can not kick the clan owner",
            ["Notification.Kick.NoPermissions"] = "You do not have sufficient permission to kick clan members",
            ["Notification.Kick.NotClanmember"] = "The target is not a member of your clan",
            ["Notification.Kick.Self"] = "You can not kick yourself",
            ["Notification.Kick.NotEnoughRank"] = "Only the clan owner can kick another ranking member",
            ["Notification.Kick.NoPlayerFound"] = "Unable to find a player with the specified name of ID",
            ["Notification.Kick.Message"] = "{0} kicked {1} from the clan!",
            ["Notification.Kick.PlayerMessage"] = "{0} kicked you from the clan!",
            ["Notification.Kick.NoPermission"] = "You do not have permission to kick clan members",

            ["Notification.Leave.Message"] = "{0} has left the clan!",
            ["Notification.Leave.PlayerMessage"] = "You have left the clan <color=#aaff55>[{0}]</color>!",
            ["Notification.Leave.NewOwner"] = "{0} is now the clan leader!",
            ["Notification.Leave.NoPermission"] = "You do not have permission to leave this clan",

            ["Notification.Join.NoPermission"] = "You do not have permission to join a clan",
            ["Notification.Join.ExpiredInvite"] = "Your invite to {0} has expired!",
            ["Notification.Join.InExistingClan"] = "You are already a member of another clan",
            ["Notification.Join.Message"] = "{0} has joined the clan!",

            ["Notification.Invite.NoPermissions"] = "You do not have sufficient permissions to invite other players",
            ["Notification.Invite.InviteLimit"] = "You already have the maximum number of invites allowed",
            ["Notification.Invite.InviteCooldown"] = "You must wait another {0} before you can invite new members to your clan",
            ["Notification.Invite.HasPending"] = "{0} all ready has a pending clan invite",
            ["Notification.Invite.IsMember"] = "{0} is already a clan member",
            ["Notification.Invite.InClan"] = "{0} is already a member of another clan",
            ["Notification.Invite.NoPermission"] = "{0} does not have the required permission to join a clan",
            ["Notification.Invite.SuccesTarget"] = "You have been invited to join the clan: <color=#aaff55>[{0}]</color> '{1}'\nTo join, type: <color=#ffd479>/{2} accept {0}</color>",
            ["Notification.Invite.SuccessClan"] = "{0} has invited {1} to join the clan",
            ["Notification.PendingInvites"] = "You have pending clan invites from: {0}\nYou can join a clan type: <color=#ffd479>/{1} accept <tag></color>",

            ["Notification.WithdrawInvite.NoPermissions"] = "You do not have sufficient permissions to withdraw member invites",
            ["Notification.WithdrawInvite.UnableToFind"] = "Unable to find a invite for the player with {0}",
            ["Notification.WithdrawInvite.Success"] = "{0} revoked the member invitation for {1}",

            ["Notification.RejectInvite.InvalidInvite"] = "You do not have a invite to join <color=#aaff55>[{0}]</color>",
            ["Notification.RejectInvite.Message"] = "{0} has rejected their invition to join your clan",
            ["Notification.RejectInvite.PlayerMessage"] = "You have rejected the invitation to join <color=#aaff55>[{0}]</color>",

            ["Notification.Promotion.NoPermissions"] = "You do not have sufficient permissions to promote other players",
            ["Notification.Promotion.TargetNoClan"] = "{0} is not a member of your clan",
            ["Notification.Promotion.IsOwner"] = "You can not promote the clan leader",
            ["Notification.Promotion.IsCouncil"] = "You can not promote higher than the rank of council",
            ["Notification.Promotion.CouncilTaken"] = "The rank of council has already been awarded",
            ["Notification.Promotion.ModeratorLimit"] = "You already have the maximum amount of moderators",
            ["Notification.Promotion.IsModerator"] = "You can not promote higher than the rank of moderator",
            ["Notification.Promotion.Message"] = "{0} was promoted to rank of {1} by {2}",

            ["Notification.Demotion.NoPermissions"] = "You do not have sufficient permissions to demote other players",
            ["Notification.Demotion.IsOwner"] = "You can not demote the clan leader",
            ["Notification.Demotion.IsMember"] = "{0} is already at the lowest rank",
            ["Notification.Demotion.Message"] = "{0} was demoted to rank of {1} by {2}",

            ["Notification.Alliance.NoPermissions"] = "You do not have sufficient permissions to manage alliances",
            ["Notification.Alliance.PendingInvite"] = "<color=#aaff55>[{0}]</color> already has a pending alliance invite",
            ["Notification.Alliance.MaximumInvites"] = "You already have the maximum amount of alliance invites allowed",
            ["Notification.Alliance.MaximumAlliances"] = "You already have the maximum amount of alliances formed",
            ["Notification.Alliance.InviteSent"] = "You have sent a clan alliance invitation to <color=#aaff55>[{0}]</color>\nThe invitation will expire in: {1}",
            ["Notification.Alliance.InviteReceived"] = "You have received a clan alliance invitation from <color=#aaff55>[{0}]</color>\nTo accept, type: <color=#ffd479>/{2} accept {0}</color>\nThe invitation will expire in: {1}",
            ["Notification.Alliance.NoActiveInvite"] = "You do not have an active alliance invitation for <color=#aaff55>[{0}]</color>",
            ["Notification.Alliance.NoActiveInviteFrom"] = "You do not have an active alliance invitation from <color=#aaff55>[{0}]</color>",
            ["Notification.Alliance.WithdrawnClan"] = "{0} has withdrawn an alliance invitation to <color=#aaff55>[{1}]</color>",
            ["Notification.Alliance.WithdrawnTarget"] = "<color=#aaff55>[{0}]</color> has withdrawn their alliance invitation",
            ["Notification.Alliance.AtLimitTarget"] = "<color=#aaff55>[{0}]</color> currently has the maximum amount of alliances allowed",
            ["Notification.Alliance.AtLimitSelf"] = "Your clan currently has the maximum amount of alliances allowed",
            ["Notification.Alliance.AtLimitAlliedMembersTarget"] = "<color=#aaff55>[{0}]</color> clan member count plus alliance member count plus your clans member count is greater than allowed",
            ["Notification.Alliance.AtLimitAlliedMembersSelf"] = "Your clan member count plus alliance member count plus this clans member count is greater than allowed",
            ["Notification.Alliance.Formed"] = "<color=#aaff55>[{0}]</color> has formed an alliance with <color=#aaff55>[{1}]</color>",
            ["Notification.Alliance.Rejected"] = "<color=#aaff55>[{0}]</color> has rejected calls to form an alliance with <color=#aaff55>[{1}]</color>",
            ["Notification.Alliance.Revoked"] = "<color=#aaff55>[{0}]</color> has revoked their alliance with <color=#aaff55>[{1}]</color>",
            ["Notification.Alliance.NoActiveAlliance"] = "You do not currently have an alliance with <color=#aaff55>[{0}]</color>",

            ["Notification.FF.MemberEnabled"] = "Clanmates <color=#ffd479>will</color> take damage from friendly fire",
            ["Notification.FF.MemberDisabled"] = "Clanmates <color=#ffd479>won't</color> take damage from friendly fire",
            ["Notification.FF.AllyEnabled"] = "Allies <color=#ffd479>will</color> take damage from friendly fire",
            ["Notification.FF.AllyDisabled"] = "Allies <color=#ffd479>won't</color> take damage from friendly fire",
            ["Notification.FF.IsEnabled"] = "<color=#aaff55>Enabled</color>",
            ["Notification.FF.IsDisabled"] = "<color=#ce422b>Disabled</color>",
            ["Notification.FF.OnHitClanMember"] = "{0} is a clan member and can not be hurt.\nTo toggle clan friendly fire type: <color=#ffd479>/{1}</color>",
            ["Notification.FF.OnHitAllyMember"] = "{0} is a member of an allied clan and can not be hurt.\nTo toggle ally friendly fire type: <color=#ffd479>/{1}</color>",
            ["Notification.FF.ToggleNotOwner"] = "Only the clan Owner or Council can toggle friendly fire",
            ["Notification.FF.OwnerToggle"] = "{0} has toggled friendly fire.\n{1}",
            ["Notification.FF.OwnerAllyToggle"] = "{0} has toggled allied friendly fire.\n{1}",

            ["Notification.ClanHelp.NoClan"] = "\nAvailable Commands:\n<color=#ffd479>/{0} create <tag> \"description\"</color> - Create a new clan\n<color=#ffd479>/{0} accept <tag></color> - Join a clan by invitation\n<color=#ffd479>/{0} reject <tag></color> - Reject a clan invitation",
            ["Notification.ClanHelp.Basic2"] = "\nAvailable Commands:\n<color=#ffd479>/{0}</color> - Display your clan information\n<color=#ffd479>/{1} <message></color> - Send a message via clan chat\n<color=#ffd479>/{0} leave</color> - Leave your current clan",
            ["Notification.ClanHelp.MFF"] = "\n<color=#ffd479>/{0}</color> - Toggle friendly fire against other clan mates",
            ["Notification.ClanHelp.AFF"] = "\n<color=#ffd479>/{0}</color> - Toggle friendly fire against allied clan members",
            ["Notification.ClanHelp.Alliance"] = "\n\n<color=#45b6fe><size=14>Alliance Commands:</size></color>\n<color=#ffd479>/{0} invite <tag></color> - Invite a clan to become allies\n<color=#ffd479>/{0} withdraw <tag></color> - Withdraw an alliance invitation\n<color=#ffd479>/{0} accept <tag></color> - Accept an alliance invitation\n<color=#ffd479>/{0} reject <tag></color> - Reject an alliance invitation\n<color=#ffd479>/{0} revoke <tag></color> - Revoke an alliance",
            ["Notification.ClanHelp.Moderator"] = "\n\n<color=#b573ff><size=14>Moderator Commands:</size></color>\n<color=#ffd479>/{0} invite <name or ID></color> - Invite a player to your clan\n<color=#ffd479>/{0} withdraw <name or ID></color> - Revoke a invitation\n<color=#ffd479>/{0} kick <name or ID></color> - Kick a member from your clan",
            ["Notification.ClanHelp.Owner"] = "\n\n<color=#a1ff46><size=14>Owner Commands:</size></color>\n<color=#ffd479>/{0} promote <name or ID></color> - Promote a clan member\n<color=#ffd479>/{0} demote <name or ID></color> - Demote a clan member\n<color=#ffd479>/{0} disband forever</color> - Disband your clan",

            ["Notification.ClanHelp.TagColor"] = "\n<color=#ffd479>/{0} tagcolor <hex></color> - Sets a custom clan tag color\n<color=#ffd479>/{0} tagcolor reset</color> - Restores the default clan tag color",

            ["Notification.Clan.NotInAClan"] = "\nYou are currently not a member of a clan",
            ["Notification.Clan.Help"] = "\nTo see available commands type: <color=#ffd479>/{0}</color>",
            ["Notification.Clan.OwnerOf"] = "\nYou are the owner of: <color=#aaff55>{0}</color> ({1}/{2})",
            ["Notification.Clan.CouncilOf"] = "\nYou are the council of: <color=#aaff55>{0}</color> ({1}/{2})",
            ["Notification.Clan.ModeratorOf"] = "\nYou are a moderator of: <color=#aaff55>{0}</color> ({1}/{2})",
            ["Notification.Clan.MemberOf"] = "\nYou are a member of: <color=#aaff55>{0}</color> ({1}/{2})",
            ["Notification.Clan.MembersOnline"] = "\nMembers Online: {0}",
            ["Notification.Clan.MFF"] = "\nClan FF Status: {0} (<color=#ffd479>/{1}</color>)",
            ["Notification.Clan.AFF"] = "\nAlly FF Status: {0} (<color=#ffd479>/{1}</color>)",

            ["Notification.Clan.CreateSyntax"] = "<color=#ffd479>/{0} create <tag> \"description\"</color> - Create a new clan",
            ["Notification.Clan.InviteSyntax"] = "<color=#ffd479>/{0} invite <partialNameOrID></color> - Invite a player to your clan",
            ["Notification.Clan.WithdrawSyntax"] = "<color=#ffd479>/{0} withdraw <partialNameOrID></color> - Revoke a member invitation",
            ["Notification.Clan.AcceptSyntax"] = "<color=#ffd479>/{0} accept <tag></color> - Join a clan by invitation",
            ["Notification.Clan.RejectSyntax"] = "<color=#ffd479>/{0} reject <tag></color> - Reject a clan invitation",
            ["Notification.Clan.PromoteSyntax"] = "<color=#ffd479>/{0} promote <partialNameOrID></color> - Promote a clanFreb member to the next rank",
            ["Notification.Clan.DemoteSyntax"] = "<color=#ffd479>/{0} demote <partialNameOrID></color> - Demote a clan member to the next lowest rank",
            ["Notification.Clan.DisbandSyntax"] = "<color=#ffd479>/{0} disband forever</color> - Disband your clan (this can not be undone)",
            ["Notification.Clan.KickSyntax"] = "<color=#ffd479>/{0} kick <partialNameOrID></color> - Kick a member from your clan",

            ["Notification.Clan.TagColorSyntax"] = "<color=#ffd479>/{0} tagcolor <hex (XXXXXX)></color> - Set a custom clan tag color",
            ["Notification.Clan.TagColorFormat"] = "<color=#ffd479>The hex string must be 6 characters long, and be a valid hex color</color>",
            ["Notification.Clan.TagColorReset"] = "<color=#ffd479>You have reset your clan's tag color</color>",
            ["Notification.Clan.TagColorSet"] = "<color=#ffd479>You have set your clan's tag color to</color> <color=#{0}>{0}</color>",
            ["Notification.Clan.TagColorDisabled"] = "<color=#ffd479>Custom tag colors are disabled on this server</color>",
            ["Notification.Clan.TagColorBlocked"] = "<color=#ffd479>The color <color=#{0}>{0}</color> is blacklisted for use by clans.",
            ["Notification.Clan.TagColorOutOfRange"] = "<color=#ffd479>The color <color=#{0}>{0}</color> is out of the allowed color range. You must pick a color between <color=#{1}>{1}</color> and <color=#{2}>{2}</color></color>",

            ["Notification.Disband.NotOwner"] = "You must be the clan owner to use this command",
            ["Notification.Disband.Success"] = "You have disbanded the clan <color=#aaff55>[{0}]</color>",
            ["Notification.Disband.Message"] = "The clan has been disbanded",
            ["Notification.Disband.NoPermission"] = "You do not have permission to disband this clan",

            ["Notification.Generic.ClanFull"] = "The clan is already at maximum capacity",
            ["Notification.Generic.NoClan"] = "You are not a member of a clan",
            ["Notification.Generic.InvalidClan"] = "The clan <color=#aaff55>[{0}]</color> does not exist!",
            ["Notification.Generic.NoPermissions"] = "You have insufficient permission to use that command",
            ["Notification.Generic.SpecifyClanTag"] = "Please specify a clan tag",
            ["Notification.Generic.UnableToFindPlayer"] = "Unable to find a player with the name or ID {0}",
            ["Notification.Generic.CommandSelf"] = "You can not use this command on yourself",

            ["Chat.IsMuted"] = "You are currently muted",
            ["Chat.Alliance.Prefix"] = "<color=#a1ff46>[ALLY CHAT]</color>: {0}",
            ["Chat.Clan.Prefix"] = "<color=#a1ff46>[CLAN CHAT]</color>: {0}",
            ["Chat.Alliance.Format"] = "[{0}] <color={1}>{2}</color>: {3}",

            ["Admin.BroadcastToClan"] = "<color=#ff3333>[ADMIN]</color>: {0}",
            ["Admin.Rename"] = "An administrator changed your clan tag to <color=#aaff55>[{0}]</color>",
            ["Admin.Disband"] = "An administrator has disbanded your clan",
            ["Admin.Invite"] = "An administrator has invited {0} to join your clan",
            ["Admin.Join"] = "An administrator has forced you to join <color=#aaff55>[{0}]</color>",
            ["Admin.Kick"] = "An administrator has kicked you from <color=#aaff55>[{0}]</color>",
            ["Admin.SetOwner"] = "An administrator has set {0} as the clan leader",
            ["Admin.Promote"] = "An administrator has promoted {0} to the rank of {1}",
            ["Admin.Demote"] = "An administrator has demoted {0} to the rank of {1}",
        };

        private Dictionary<string, string> leetTable = new Dictionary<string, string>
        {
            { "}{", "h" },
            { "|-|", "h" },
            { "]-[", "h" },
            { "/-/", "h" },
            { "|{", "k" },
            { "/\\/\\", "m" },
            { "|\\|", "n" },
            { "/\\/", "n" },
            { "()", "o" },
            { "[]", "o" },
            { "vv", "w" },
            { "\\/\\/", "w" },
            { "><", "x" },
            { "2", "z" },
            { "4", "a" },
            { "@", "a" },
            { "8", "b" },
            { "ß", "b" },
            { "(", "c" },
            { "<", "c" },
            { "{", "c" },
            { "3", "e" },
            { "€", "e" },
            { "6", "g" },
            { "9", "g" },
            { "&", "g" },
            { "#", "h" },
            { "$", "s" },
            { "7", "t" },
            { "|", "l" },
            { "1", "i" },
            { "!", "i" },
            { "0", "o" },
        };
        #endregion

        /*[ChatCommand(("fakeclan"))]
        private void cmdFakeClan(BasePlayer player, string command, string[] args)
        {
            Clan myClan = storedData.FindClanByID(player.userID);
            for (int i = 0; i < 20; i++)
            {
                ulong ownerid = (ulong)UnityEngine.Random.Range(76560000000000000, 7656999999999999);
                string tag = $"{UnityEngine.Random.Range(100, 999)}";
                Clan clan = new Clan
                {
                    Tag = tag,
                    Description = string.Empty,
                    CreationTime = UnixTimeStampUTC(),
                    LastOnlineTime = UnixTimeStampUTC(),
                    OwnerID = ownerid
                };
                
                clan.ClanMembers.Add(ownerid, new Clan.Member(Clan.Member.MemberRole.Owner, clan));
                clan.AllianceInvites.Add("420", UnixTimeStampUTC());

                myClan.IncomingAlliances.Add(tag);

                storedData.clans[tag] = clan;
            }

            for (int i = 0; i < 20; i++)
            {
                ulong ownerid = (ulong)UnityEngine.Random.Range(76560000000000000, 7656999999999999);
                myClan.MemberInvites[ownerid] = new Clan.MemberInvite()
                {
                    DisplayName = $"Dummy Player {i}",
                    ExpiryTime = UnixTimeStampUTC()
                };
            }
            
            for (int i = 0; i < 20; i++)
            {
                ulong ownerid = (ulong)UnityEngine.Random.Range(76560000000000000, 7656999999999999);
                myClan.Alliances.Add(ownerid.ToString());
            }
            
            for (int i = 0; i < 20; i++)
            {
                ulong ownerid = (ulong)UnityEngine.Random.Range(76560000000000000, 7656999999999999);
                myClan.AllianceInvites.Add(ownerid.ToString(), UnixTimeStampUTC());
            }
            
            for (int i = 0; i < 20; i++)
            {
                ulong ownerid = (ulong)UnityEngine.Random.Range(76560000000000000, 7656999999999999);
                myClan.IncomingAlliances.Add(ownerid.ToString());
            }
            SaveData();
        }*/
    }
}
