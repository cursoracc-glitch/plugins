using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Activities;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Commands;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Logging;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DiscordRewards", "k1lly0u", "0.2.2")]
    [Description("Reward players with items, kits and commands for being a member of your Discord")]
    class DiscordRewards : RustPlugin
    {
        #region Fields        
        [PluginReference]
        private Plugin ImageLibrary, Kits;

        [DiscordClient]
        private DiscordClient Client;

        private DiscordGuild Guild;

        private DiscordRole NitroRole;

        private DiscordChannel ValidationChannel;

        public static DiscordRewards Instance { get; private set; }


        private bool isInitialized = false;

        private bool needsWipe = false;

        private int statusIndex = 0;

        private enum RewardType { Kit, Item, Command }

        #endregion

        #region Oxide Hooks
        private void Loaded() 
        {
            Instance = this;
            LoadData();
        }

        protected override void LoadDefaultMessages() => lang.RegisterMessages(Messages, this);

        private void OnServerInitialized()
        {            
            LoadImages();

            if (needsWipe)
            {
                if (Configuration.Token.WipeReset)
                    WipeData();
                else if (Configuration.Token.WipeResetRewards)
                    WipeRewardCooldowns();
            }            
        }

        private void OnServerSave() => SaveData();

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null)
                return;

            ValidateUser(player);
            UpdateStatus();
        }

        private void OnPlayerDisconnected(BasePlayer player) => UpdateStatus();

        private void OnNewSave(string filename) => needsWipe = true;

        private void Unload()
        {            
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, UI_MENU);

            if (Client != null)
                Client.Disconnect();

            Instance = null;
        }
        #endregion

        #region Discord Hooks        
        private void OnDiscordClientCreated()
        {
            if (string.IsNullOrEmpty(Configuration.Settings.APIKey))
            {
                PrintError("No API token set in config... Unable to continue!");
                return;
            }

            if (string.IsNullOrEmpty(Configuration.Settings.BotID))
            {
                PrintError("No bot client ID set in config... Unable to continue!");
                return;
            }

            Puts("Establishing connection to your Discord server...");

            DiscordSettings settings = new DiscordSettings();
            settings.ApiToken = Configuration.Settings.APIKey;
            settings.LogLevel = Configuration.Settings.LogLevel;
            settings.Intents = GatewayIntents.Guilds | GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers;

            Client.Connect(settings);
        }
                
        private void OnDiscordGuildCreated(DiscordGuild guild)
        {
            if (guild == null)
            {
                PrintError("Failed to connect to guild. Unable to continue...");
                return;
            }

            Guild = guild;

            Puts($"Connection to {Guild.Name} established! DiscordRewards is now active");

            NitroRole = Guild.GetBoosterRole();

            if (!string.IsNullOrEmpty(Configuration.Token.ValidationChannel))
                ValidationChannel = Guild.GetChannel(Configuration.Token.ValidationChannel);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                ValidateUser(player);

            UpdateStatus();

            isInitialized = true;
        }

        private void OnDiscordGuildMemberRemoved(GuildMember member, DiscordGuild guild)
        {
            ulong steamId;
            if (userData.FindByID(member.Id, out steamId))
            {     
                int code;
                if (userData.HasPendingToken(steamId, out code))
                    userData.InvalidateToken(code);

                UserData.User user = userData.GetUser(steamId);
                if (user != null)
                    RevokeRewards(steamId, user);
            }
        }

        private void OnDiscordDirectMessageCreated(DiscordMessage message, DiscordChannel channel)
        {
            if (message == null || message.Author.Bot == true)
                return;

            int code;
            if (int.TryParse(message.Content, out code) && AttemptTokenValidation(message.Author, code))
                return;

            message.Author.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, Message("Discord.InvalidToken")));
        }

        private void OnDiscordGuildMessageCreated(DiscordMessage message, DiscordChannel channel, DiscordGuild guild)
        {
            if (message == null || message.Author.Bot == true)
                return;

            if (ValidationChannel == null || channel != ValidationChannel)
                return;

            int code;
            if (int.TryParse(message.Content, out code) && AttemptTokenValidation(message.Author, code))
            {
                message.DeleteMessage(Client);
                return;
            }

            message.Author.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, Message("Discord.InvalidToken")));
            message.DeleteMessage(Client);
        }
        #endregion

        #region Token Validation        
        private bool AttemptTokenValidation(DiscordUser discordUser, int code)
        {
            UserData.DiscordToken token;
            if (userData.IsValidToken(code, out token))
            {
                BasePlayer player = FindPlayer(token.playerId);

                if (token.expireTime < CurrentTime())
                {
                    discordUser.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, Message("Discord.TokenExpired", player?.userID ?? 0UL)));
                    userData.InvalidateToken(code);
                    return true;
                }

                if (player == null)
                {
                    discordUser.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, string.Format(Message("Discord.FailedToFindPlayer", player?.userID ?? 0UL), token.playerId)));
                    return true;
                }

                if (!player.IsConnected)
                {
                    discordUser.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, Message("Discord.NotOnServer", player?.userID ?? 0UL)));
                    return true;
                }

                if (player.IsDead())
                {
                    discordUser.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, Message("Discord.UserIsDead", player?.userID ?? 0UL)));
                    return true;
                }

                userData.InvalidateToken(code);

                UserData.User user = userData.GetUser(token.playerId) ?? userData.AddNewUser(token.playerId, discordUser.Id);

                user.SetExpiryDate(Configuration.Token.RevalidationInterval);

                string response = Message("Discord.ValidatedToken", player?.userID ?? 0UL);

                if (Configuration.Token.RequireRevalidation)
                    response += $" {string.Format(Message("Discord.TokenExpires", player?.userID ?? 0UL), FormatTime(Configuration.Token.RevalidationInterval))}";

                if (Configuration.UISettings.Enabled)
                    response += $" {Message("Discord.OpenStore", player?.userID ?? 0UL)}";

                discordUser.CreateDirectMessageChannel(Client, (DiscordChannel dmChannel) => dmChannel.CreateMessage(Client, response));


                if (player != null)
                {
                    player.ChatMessage(response);
                    IssueAlternativeRewards(player);
                }

                SaveData();
                UpdateStatus();
                return true;
            }

            return false;
        }

        private BasePlayer FindPlayer(ulong userId)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.userID.Equals(userId))
                    return player;
            }

            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
            {
                if (player.userID.Equals(userId))
                    return player;
            }

            return null;
        }
        #endregion

        #region Status
        private void UpdateStatus()
        {            
            statusIndex += 1;

            if (statusIndex >= (Configuration.Settings.StatusMessages?.Length ?? 0))
                statusIndex = 0;

            if (Client?.Bot != null) 
            {
                if (Configuration.Settings.StatusMessages != null && Configuration.Settings.StatusMessages.Length > 0)
                {
                    string message = Configuration.Settings.StatusMessages[statusIndex];

                    if (!string.IsNullOrEmpty(message))
                    {
                        string str = message.Replace("{playersMin}", BasePlayer.activePlayerList.Count.ToString())
                            .Replace("{playersMax}", ConVar.Server.maxplayers.ToString())
                            .Replace("{rewardPlayers}", (!Configuration.Token.RequireRevalidation ? userData.users.Count.ToString() : userData.users.Where(x => CurrentTime() < x.Value.expireTime).Count().ToString()));

                        Client.Bot.UpdateStatus(new UpdatePresenceCommand()
                        {
                            Activities = new List<DiscordActivity>
                            {
                                new DiscordActivity()
                                {
                                    Name = str,
                                    Type = ActivityType.Game
                                }
                            }
                        });
                    }
                }
            }

            timer.In(Mathf.Clamp(Configuration.Settings.StatusCycle, 60, int.MaxValue), UpdateStatus);
        }
        #endregion

        #region Helpers
        private GuildMember FindMember(Snowflake id)
        {
            foreach (GuildMember guildMember in Guild.Members.Values)
            {
                if (guildMember.Id.Equals(id))
                    return guildMember;
            }

            return null;
        }
                
        public DiscordRole GetRoleByID(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (DiscordRole role in Guild.Roles.Values)
                {
                    if (role.Id.ToString().Equals(id, StringComparison.OrdinalIgnoreCase))
                        return role;
                }
            }

            return null;
        }

        private int GenerateToken()
        {
            int token = UnityEngine.Random.Range(100000, 999999);
            if (userData.tokenToUser.ContainsKey(token))
                return GenerateToken();
            return token;
        }

        private static double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private T ParseType<T>(string type) => (T)Enum.Parse(typeof(T), type, true);

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            int days = dateDifference.Days;
            int hours = dateDifference.Hours;
            int mins = dateDifference.Minutes;
            int secs = dateDifference.Seconds;

            if (days > 0)
                return string.Format("{0:00}d:{1:00}h:{2:00}m:{3:00}s", days, hours, mins, secs);
            else if (hours > 0)
                return string.Format("{0:00}h:{1:00}m:{2:00}s", hours, mins, secs);
            else if (mins > 0)
                return string.Format("{0:00}m:{1:00}s", mins, secs);
            else return string.Format("{0}s", secs);
        }
        #endregion

        #region Groups and Permission Helpers
        private void AddToGroup(string userId, string groupId) => permission.AddUserGroup(userId, groupId);

        private void RemoveFromGroup(string userId, string groupId) => permission.RemoveUserGroup(userId, groupId);

        private bool GroupExists(string groupId) => permission.GroupExists(groupId);

        private bool HasGroup(string userId, string groupId) => permission.UserHasGroup(userId, groupId);
               
        private void GrantPermission(string userId, string perm) => permission.GrantUserPermission(userId, perm, null);

        private void RevokePermission(string userId, string perm) => permission.RevokeUserPermission(userId, perm);

        private bool HasPermission(string userId, string perm) => permission.UserHasPermission(userId, perm);

        private bool PermissionExists(string userId) => permission.PermissionExists(userId);
        #endregion

        #region User Validation
        private void ValidateUser(BasePlayer player)
        {
            if (player == null)
                return;

            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(3, () => ValidateUser(player));
                return;
            }

            UserData.User user;
            if (!userData.users.TryGetValue(player.userID, out user))
                return;

            if (CurrentTime() > user.expireTime)
            {
                if (Configuration.Token.RequireRevalidation)
                {
                    if (Configuration.Token.AutoRevalidation)
                    {
                        if (FindMember(user.Id) != null)
                        {
                            user.SetExpiryDate(Configuration.Token.RevalidationInterval);
                            IssueAlternativeRewards(player);
                            SendReply(player, Message("Message.AutoValidated", player.userID));
                            return;
                        }
                    }

                    SendReply(player, Message("Message.ValidationExpired", player.userID));
                    RevokeRewards(player.userID, user);
                    return;
                }
            }
        }
        #endregion

        #region Rewards
        private void IssueAlternativeRewards(BasePlayer player)
        {
            UserData.User user;
            if (!userData.users.TryGetValue(player.userID, out user))
                return;

            for (int i = 0; i < Configuration.Rewards.Groups.Length; i++)
            {
                string group = Configuration.Rewards.Groups[i];
                if (GroupExists(group) && !HasGroup(player.UserIDString, group))
                {
                    AddToGroup(player.UserIDString, group);
                    user.groups.Add(group);
                }
            }

            for (int i = 0; i < Configuration.Rewards.Permissions.Length; i++)
            {
                string perm = Configuration.Rewards.Permissions[i];
                if (PermissionExists(perm) && !HasPermission(player.UserIDString, perm))
                {
                    GrantPermission(player.UserIDString, perm);
                    user.permissions.Add(perm);
                }
            }

            for (int i = 0; i < Configuration.Rewards.Commands.Length; i++)
            {
                string cmd = Configuration.Rewards.Commands[i];
                rust.RunServerCommand(cmd.Replace("$player.id", player.UserIDString)
                    .Replace("$player.name", player.displayName)
                    .Replace("$player.x", player.transform.position.x.ToString())
                    .Replace("$player.y", player.transform.position.y.ToString())
                    .Replace("$player.z", player.transform.position.z.ToString())
                    );
            }

            if (Configuration.Rewards.Roles.Length > 0)
                ApplyUserRoles(user, Configuration.Rewards.Roles, true);

            if (Configuration.Rewards.RevokeRoles.Length > 0)
                RevokeUserRoles(user, Configuration.Rewards.RevokeRoles, false);

            Guild.GetGuildMember(Client, user.Id, (GuildMember guildMember) =>
            {
                bool wasNitroBooster = user.isNitroBooster;

                user.isNitroBooster = NitroRole != null && guildMember.HasRole(NitroRole);

                if (user.isNitroBooster)
                    IssueNitroRewards(player, user);
                else if (wasNitroBooster)
                    RevokeNitroRewards(player.userID, user);
            });
        }

        private void ApplyUserRoles(UserData.User user, IEnumerable<string> roles, bool storeChanges)
        {
            Guild.GetGuildMember(Client, user.Id, (GuildMember guildMember) =>
            {
                bool hasAllRoles = true;
                foreach (string roleName in roles)
                {
                    DiscordRole discordRole = Guild.GetRole(roleName);
                    if (discordRole == null)
                        discordRole = GetRoleByID(roleName);

                    if (discordRole != null)
                    {
                        if (guildMember.HasRole(discordRole))
                        {
                            if (storeChanges)                            
                                user.roles.Add(discordRole.Id);                            
                            continue;
                        }

                        hasAllRoles = false;
                        Guild.AddGuildMemberRole(Client, guildMember.User, discordRole);
                    }
                }

                if (!hasAllRoles)
                    timer.In(5f, () => ApplyUserRoles(user, roles, storeChanges));
            });
        }

        private void RevokeUserRoles(UserData.User user, IEnumerable<string> roles, bool storeChanges)
        {
            Guild.GetGuildMember(Client, user.Id, (GuildMember guildMember) =>
            {
                bool allRolesRemoved = true;
                foreach (string roleName in roles)
                {
                    DiscordRole discordRole = Guild.GetRole(roleName);
                    if (discordRole == null)
                        discordRole = GetRoleByID(roleName);

                    if (discordRole != null)
                    {
                        if (!guildMember.HasRole(discordRole))
                        {
                            if (storeChanges)
                                user.roles.Remove(discordRole.Id);
                            continue;
                        }

                        allRolesRemoved = false;
                        Guild.RemoveGuildMemberRole(Client, guildMember.User, discordRole);
                    }
                }

                if (!allRolesRemoved)
                    timer.In(5f, () => RevokeUserRoles(user, roles, storeChanges));
            });
        }

        private void IssueNitroRewards(BasePlayer player, UserData.User user)
        {
            for (int i = 0; i < Configuration.Rewards.NitroGroups.Length; i++)
            {
                string group = Configuration.Rewards.NitroGroups[i];
                if (GroupExists(group) && !HasGroup(player.UserIDString, group))
                {
                    AddToGroup(player.UserIDString, group);
                    user.nitroGroups.Add(group);
                }
            }

            for (int i = 0; i < Configuration.Rewards.NitroPermissions.Length; i++)
            {
                string perm = Configuration.Rewards.NitroPermissions[i];
                if (PermissionExists(perm) && !HasPermission(player.UserIDString, perm))
                {
                    GrantPermission(player.UserIDString, perm);
                    user.nitroPermissions.Add(perm);
                }
            }

            for (int i = 0; i < Configuration.Rewards.NitroCommands.Length; i++)
            {
                string cmd = Configuration.Rewards.NitroCommands[i];
                rust.RunServerCommand(cmd.Replace("$player.id", player.UserIDString)
                                         .Replace("$player.name", player.displayName)
                                         .Replace("$player.x", player.transform.position.x.ToString())
                                         .Replace("$player.y", player.transform.position.y.ToString())
                                         .Replace("$player.z", player.transform.position.z.ToString()));
            }
        }

        private void RevokeRewards(ulong playerId, UserData.User user)
        {
            foreach (string group in user.groups)
                RemoveFromGroup(playerId.ToString(), group);
            user.groups.Clear();

            foreach (string perm in user.permissions)
                RevokePermission(playerId.ToString(), perm);
            user.permissions.Clear();

            if (user.roles.Count > 0)
                RevokeUserRoles(user, new List<string>(user.roles), true);

            if (Configuration.Rewards.RevokeRoles.Length > 0)
                ApplyUserRoles(user, Configuration.Rewards.RevokeRoles, false);

            if (user.isNitroBooster)
                RevokeNitroRewards(playerId, user);
        }

        private void RevokeNitroRewards(ulong playerId, UserData.User user)
        {
            foreach (string group in user.nitroGroups)
                RemoveFromGroup(playerId.ToString(), group);
            user.nitroGroups.Clear();

            foreach (string perm in user.nitroPermissions)
                RevokePermission(playerId.ToString(), perm);
            user.nitroPermissions.Clear();
        }
        #endregion

        #region UI
        private const string UI_MENU = "discordstore_ui";

        private class UI
        {
            public static CuiElementContainer Container(string panel, UI4 dimensions, string color = "0 0 0 0.9")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = true
                        },
                        new CuiElement().Parent = "Hud",
                        panel.ToString()
                    }
                };
                return container;
            }

            public static void Panel(CuiElementContainer container, string panel, string color, UI4 dimensions)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);
            }

            public static void Label(CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);
            }

            public static void Button(CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f, },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align }

                },
                panel);
            }

            public static void Image(CuiElementContainer container, string panel, string png, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        public struct UI4
        {
            public float xMin, yMin, xMax, yMax;

            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            public string GetMin() => $"{xMin} {yMin}";

            public string GetMax() => $"{xMax} {yMax}";

            private static UI4 _full;

            public static UI4 Full
            {
                get
                {
                    if (_full.Equals(default(UI4)))
                        _full = new UI4(0, 0, 1, 1);
                    return _full;
                }
            }
        }
        #endregion

        #region UI Creation
        private void OpenStore(BasePlayer player)
        {
            if (rewardData.items.Count > 0)
                LoadStoreUI(player, RewardType.Item, 0);
            else if (rewardData.kits.Count > 0)
                LoadStoreUI(player, RewardType.Kit, 0);
            else if (rewardData.commands.Count > 0)
                LoadStoreUI(player, RewardType.Command, 0);
            else SendReply(player, Message("Error.NoItems", player.userID));
        }
       
        private void LoadStoreUI(BasePlayer player, RewardType rewardType, int page)
        {
            UserData.User user;
            if (!userData.users.TryGetValue(player.userID, out user))
                return;

            CuiElementContainer container = UI.Container(UI_MENU, new UI4(0.3f, 0.35f, 0.7f, 0.65f));

            UI.Panel(container, UI_MENU, Configuration.UISettings.Panel.Color, new UI4(0.0075f, 0.88f, 0.9925f, 0.98f));

            UI.Label(container, UI_MENU, Message("UI.Title", player.userID), 18, new UI4(0.015f, 0.88f, 0.99f, 0.98f), TextAnchor.MiddleLeft);

            UI.Button(container, UI_MENU, Configuration.UISettings.Close.Color, "<b>×</b>", 18, new UI4(0.95f, 0.88f, 0.992f, 0.98f), "drui.exit");

            AddCategoryButtons(container, rewardType, player.userID);

            switch (rewardType)
            {
                case RewardType.Kit:
                    PopulateItems<RewardData.RewardKit>(rewardData.kits, container, page * 4, player.userID, user.isNitroBooster);
                    break;
                case RewardType.Item:
                    PopulateItems<RewardData.RewardItem>(rewardData.items, container, page * 4, player.userID, user.isNitroBooster);
                    break;
                case RewardType.Command:
                    PopulateItems<RewardData.RewardCommand>(rewardData.commands, container, page * 4, player.userID, user.isNitroBooster);
                    break;
            }            

            AddPagination(container, rewardType, page);

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.AddUi(player, container);
        }

        private void AddCategoryButtons(CuiElementContainer container, RewardType selected, ulong playerId)
        {
            int i = 0;
            UI4 position = TypeAlignment.Get(i);

            if (rewardData.kits.Count > 0)
            {
                UI.Button(container, UI_MENU, selected == RewardType.Kit ? Configuration.UISettings.Selected.Color : Configuration.UISettings.Deselected.Color, 
                    Message("UI.Kits", playerId), 12, new UI4(position.xMin, position.yMin, position.xMax, position.yMax), selected == RewardType.Kit ? "" : "drui.changepage kit 0");                
                i++;
                position = TypeAlignment.Get(i);
            }
            if (rewardData.items.Count > 0)
            {                
                UI.Button(container, UI_MENU, selected == RewardType.Item ? Configuration.UISettings.Selected.Color : Configuration.UISettings.Deselected.Color, 
                    Message("UI.Items", playerId), 12, new UI4(position.xMin, position.yMin, position.xMax, position.yMax), selected == RewardType.Item ? "" : "drui.changepage item 0");
                i++;
                position = TypeAlignment.Get(i);
            }
            if (rewardData.commands.Count > 0)
            {
                UI.Button(container, UI_MENU, selected == RewardType.Command ? Configuration.UISettings.Selected.Color : Configuration.UISettings.Deselected.Color, 
                    Message("UI.Commands", playerId), 12, new UI4(position.xMin, position.yMin, position.xMax, position.yMax), selected == RewardType.Command ? "" : "drui.changepage command 0");                
            }            
        }

        private void PopulateItems<T>(List<T> list, CuiElementContainer container, int index, ulong playerId, bool isNitroBooster) where T : RewardData.BaseReward
        {
            int count = 0;

            for (int i = index; i < Mathf.Min(list.Count, index + 4); i++)
            {               
                RewardData.BaseReward reward = list[i];
               
                UI4 position = RewardAlignment.Get(count);

                reward.AddUIEntry(container, position, i, playerId, isNitroBooster);
                count++;
            }
        }

        private void AddPagination(CuiElementContainer container, RewardType rewardType, int page)
        {
            int max = rewardType == RewardType.Command ? rewardData.commands.Count : rewardType == RewardType.Item ? rewardData.items.Count : rewardData.kits.Count;

            if (page > 0)
                UI.Button(container, UI_MENU, Configuration.UISettings.Close.Color, "< < <", 12, new UI4(0.01f, 0.01f, 0.2f, 0.08f), $"drui.changepage {rewardType} {page - 1}");
            if ((page * 3) + 3 < max)
                UI.Button(container, UI_MENU, Configuration.UISettings.Close.Color, "> > >", 12, new UI4(0.8f, 0.01f, 0.99f, 0.08f), $"drui.changepage {rewardType} {page + 1}");
        }
        #endregion

        #region UI Grid Helper
        private readonly HoriztonalAlignment RewardAlignment = new HoriztonalAlignment(4, 0.0075f, 0.01f, 0.76f, 0.66f, 0.06f);

        private readonly HoriztonalAlignment TypeAlignment = new HoriztonalAlignment(5, 0.0075f, 0.01f, 0.86f, 0.08f, 0f);

        private class HoriztonalAlignment
        {
            private int Columns { get; set; }
            private float XBorder { get; set; }
            private float XSpacing { get; set; }
            private float YOffset { get; set; }
            private float Height { get; set; }
            private float YSpacing { get; set; }
            private float ReservedSpace { get; set; }
            private float Width { get; set; }

            internal HoriztonalAlignment(int columns, float xBorder, float xSpacing, float yOffset, float height, float ySpacing)
            {
                Columns = columns;
                XBorder = xBorder;
                XSpacing = xSpacing;
                YOffset = yOffset;
                Height = height;
                YSpacing = ySpacing;

                ReservedSpace = (xBorder * 2f) + (XSpacing * (columns - 1));
                Width = (1f - ReservedSpace) / columns;
            }

            internal UI4 Get(int index)
            {
                int rowNumber = index == 0 ? 0 : Mathf.FloorToInt(index / Columns);
                int columnNumber = index - (rowNumber * Columns);
                
                float offsetX = XBorder + (Width * columnNumber) + (XSpacing * columnNumber);

                float offsetY = (YOffset - (rowNumber * Height) - (YSpacing * rowNumber));

                return new UI4(offsetX, offsetY - Height, offsetX + Width, offsetY);
            }
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("drui.changepage")]
        private void ccmdChangePage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            LoadStoreUI(player, ParseType<RewardType>(arg.GetString(0)), arg.GetInt(1));
        }

        [ConsoleCommand("drui.exit")]
        private void ccmdExit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, UI_MENU);
        }

        [ConsoleCommand("drui.claim")]
        private void ccmdClaim(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            UserData.User user;
            if (!userData.users.TryGetValue(player.userID, out user))
                return;

            int id = arg.GetInt(1);

            RewardType rewardType = ParseType<RewardType>(arg.GetString(0));

            double remaining;

            if (Configuration.Cooldown.Enabled && user.HasCooldown(out remaining))
            {
                SendReply(player, string.Format(Message("Message.OnCooldownGlobal", player.userID), FormatTime(remaining)));
                return;
            }
            if (user.HasCooldown(rewardType, id, out remaining))
            {
                SendReply(player, string.Format(Message("Message.OnCooldown", player.userID), FormatTime(remaining)));
                return;
            }

            RewardData.BaseReward reward;

            if (rewardType == RewardType.Command)
                reward = rewardData.commands[id];
            else if (rewardType == RewardType.Item)
                reward = rewardData.items[id];
            else reward = rewardData.kits[id];

            if (Configuration.Cooldown.Enabled)
                user.AddCooldown(Configuration.Cooldown.Time);
            else user.AddCooldown(rewardType, id, reward.Cooldown);

            CuiHelper.DestroyUi(player, UI_MENU);

            reward.GiveReward(player);
            SendReply(player, Message("Message.RewardGiven", player.userID));
        }
        #endregion

        #region Chat Commands 
        [ChatCommand("discord")]
        private void cmdDiscord(BasePlayer player, string command, string[] args)
        {
            if (!isInitialized)
                return;

            UserData.User user = userData.GetUser(player.userID);
            if (user == null || (Configuration.Token.RequireRevalidation && CurrentTime() > user.expireTime))
            {
                if (args.Length == 0)
                {
                    SendReply(player, Message("Help.Token", player.userID));
                    if (ValidationChannel != null)
                        SendReply(player, string.Format(Message("Help.BotOrChannel", player.userID), Client.Bot.BotUser.Username, ValidationChannel.Name));
                    else SendReply(player, string.Format(Message("Help.BotOnly", player.userID), Client.Bot.BotUser.Username));

                    return;
                }

                int code;
                if (userData.HasPendingToken(player.userID, out code))
                {
                    SendReply(player, string.Format(Message("Error.PendingToken", player.userID), code, Client.Bot.BotUser.Username));
                    return;
                }

                if (args[0].ToLower() == "token")
                {
                    code = GenerateToken();

                    userData.AddToken(code, player.userID, Configuration.Token.TokenLife);

                    SendReply(player, string.Format(Message("Message.Token", player.userID), code));
                }                
                return;
            }

            if (Configuration.UISettings.Enabled)
                OpenStore(player);
            else SendReply(player, Message("Message.AlreadyRegistered"));
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("discord.admin")]
        private void ccmdDiscordAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel != 2)
                return;
            
            string[] args = arg.Args;
            if (args == null || args.Length == 0)
            {
                SendReply(arg, $"{Title}  v{Version}");
                SendReply(arg, "discord.admin purge - Clear out all expired user data");
                SendReply(arg, "discord.admin wipe - Revoke rewards from all players and invalidate their tokens");
                SendReply(arg, "discord.admin revoke <player ID> - Revoke all rewards from the target player and invalidate their token");
                SendReply(arg, "discord.admin validatepermissions - Purges user data and validates user permissions to reinstate any that are missing");
                return;
            }

            switch (args[0].ToLower())
            {
                case "purge":
                    {
                        if (!Configuration.Token.RequireRevalidation)
                        {
                            SendReply(arg, "You can not purge the data file because you have Require Validation set to false in your config");
                            return;
                        }

                        double currentTime = CurrentTime();
                        int count = 0;

                        for (int i = userData.users.Count - 1; i >= 0; i--)
                        {
                            KeyValuePair<ulong, UserData.User> kvp = userData.users.ElementAt(i);
                            if (currentTime > kvp.Value.expireTime || string.IsNullOrEmpty(kvp.Value.discordId))
                            {
                                RevokeRewards(kvp.Key, kvp.Value);
                                userData.users.Remove(kvp.Key);
                                count++;
                            }
                        }
                        SaveData();
                        UpdateStatus();

                        SendReply(arg, $"Revoked rewards and purged {count} users with expired tokens from the data file");
                    }
                    return;

                case "wipe":                    
                    WipeData();
                    SendReply(arg, "Revoked all user rewards and wiped user data");
                    return;

                case "revoke":
                    if (args.Length == 2)
                    {
                        ulong playerId;
                        if (!ulong.TryParse(args[1], out playerId))
                        {
                            SendReply(arg, "Invalid Steam ID entered");
                            return;
                        }

                        UserData.User user;
                        if (!userData.users.TryGetValue(playerId, out user))
                        {
                            SendReply(arg, "The specified user does not have any data saved");
                            return;
                        }

                        RevokeRewards(playerId, user);
                        userData.users.Remove(playerId);
                        SaveData();
                        UpdateStatus();

                        SendReply(arg, $"Successfully revoked rewards for user: {playerId}");
                    }
                    else SendReply(arg, "You must enter a players Steam ID");
                    return;

                case "validatepermissions":
                    {
                        int purgeCount = 0;
                        int reinstateCount = 0;

                        if (Configuration.Token.RequireRevalidation)
                        {
                            double currentTime = CurrentTime();
                            for (int i = userData.users.Count - 1; i >= 0; i--)
                            {
                                KeyValuePair<ulong, UserData.User> kvp = userData.users.ElementAt(i);
                                if (currentTime > kvp.Value.expireTime)
                                {                                    
                                    RevokeRewards(kvp.Key, kvp.Value);
                                    userData.users.Remove(kvp.Key);
                                    purgeCount++;
                                }
                            }
                        }
                        foreach (KeyValuePair<ulong, UserData.User> kvp in userData.users)
                        {
                            foreach(string perm in kvp.Value.permissions)
                            {
                                if (!HasPermission(kvp.Key.ToString(), perm))
                                {
                                    GrantPermission(kvp.Key.ToString(), perm);
                                    reinstateCount++;
                                }
                            }
                        }
                        SendReply(arg, $"Purged {purgeCount} inactive users and reinstated missing permissions for {reinstateCount} users");
                    }                    
                    return;

                default:
                    break;
            }
        }

        [ConsoleCommand("discord.rewards")]
        private void ccmdDiscordRewards(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel != 2)
                return;            

            string[] args = arg.Args;
            if (args == null || args.Length == 0)
            {
                SendReply(arg, $"{Title}  v{Version}");
                SendReply(arg, "--- List Rewards ---");
                SendReply(arg, "discord.rewards list <items | kits | commands> - Display a list of rewards for the specified category, which information on each item");
                SendReply(arg, "--- Add Rewards ---");
                SendReply(arg, "discord.rewards add item <shortname> <skinId> <amount> <cooldown> <opt:bp> - Add a new reward item to the store (add \"bp\" to add the item as a blueprint)");
                SendReply(arg, "discord.rewards add kit <name> <kitname> <cooldown> - Add a new reward kit to the store");
                SendReply(arg, "discord.rewards add command <name> <command> <cooldown> - Add a new reward command to the store");
                SendReply(arg, "--- Editing Rewards ---");
                SendReply(arg, "discord.rewards edit item <ID> <name | amount | cooldown> \"edit value\" - Edit the specified field of the item with ID number <ID>");
                SendReply(arg, "discord.rewards edit kit <ID> <name | description | icon | cooldown> \"edit value\" - Edit the specified field of the kit with ID number <ID>");
                SendReply(arg, "discord.rewards edit command <ID> <name | amount | description | icon | add | remove | cooldown> \"edit value\" - Edit the specified field of the kit with ID number <ID>");
                SendReply(arg, "Icon field : The icon field can either be a URL, or a image saved to disk under the folder \"oxide/data/DiscordRewards/Images/\"");
                SendReply(arg, "Command add/remove field: Here you add additional commands or remove existing commands. Be sure to type the command inside quotation marks");
                SendReply(arg, "--- Removing Rewards ---");
                SendReply(arg, "discord.rewards remove item <ID #> - Removes the item with the specified ID number");
                SendReply(arg, "discord.rewards remove kit <ID #> - Removes the kit with the specified ID number");
                SendReply(arg, "discord.rewards remove command <ID #> - Removes the command with the specified ID number");
                SendReply(arg, "--- Important Note ---");
                SendReply(arg, "Removing rewards may change each rewards ID number. Be sure to list your rewards before removing them");
                SendReply(arg, "To set a reward for Nitro Boosters only add the word 'nitro' to the end of the command when adding the reward!");
                return;
            }

            bool isNitro = arg.Args.Last().ToLower() == "nitro";

            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    #region Lists
                    case "list":
                        if (args.Length >= 2)
                        {
                            int i = 0;
                            switch (args[1].ToLower())
                            {
                                case "items":
                                    foreach (var entry in rewardData.items)
                                    {
                                        SendReply(arg, string.Format("Item ID: {0} || Shortname: {1} ||  Amount: {2} || Skin ID: {3} ||Is Blueprint {4} || Cooldown : {5}", i, entry.Shortname, entry.Amount, entry.SkinID, entry.IsBP, entry.Cooldown));
                                        i++;
                                    }
                                    return;

                                case "kits":
                                    i = 0;
                                    foreach (var entry in rewardData.kits)
                                    {
                                        SendReply(arg, string.Format("Kit ID: {0} || Name: {1} || Description: {2} || Cooldown : {3}", i, entry.Kit, entry.Description, entry.Cooldown));
                                        i++;
                                    }
                                    return;

                                case "commands":
                                    i = 0;
                                    foreach (var entry in rewardData.commands)
                                    {
                                        SendReply(arg, string.Format("Command ID: {0} || Name: {1} || Description: {2} || Commands: {3} || Cooldown : {4}", i, entry.Name, entry.Description, entry.Commands.ToSentence(), entry.Cooldown));
                                        i++;
                                    }
                                    return;

                                default:
                                    return;
                            }
                        }
                        return;
                    #endregion
                    #region Additions
                    case "add":
                        if (args.Length >= 2)
                        {
                            switch (args[1].ToLower())
                            {
                                case "item":
                                    if (args.Length >= 6)
                                    {
                                        string shortname = args[2];

                                        ulong skinId;
                                        if (!ulong.TryParse(args[3], out skinId))
                                        {
                                            SendReply(arg, "You must enter a number for the skin ID. If you dont wish to select any skin use 0");
                                            return;
                                        }

                                        int amount;
                                        if (!int.TryParse(args[4], out amount))
                                        {
                                            SendReply(arg, "You must enter an amount of this item");
                                            return;
                                        }

                                        int cooldown = 0;
                                        if (!int.TryParse(args[5], out cooldown))
                                        {
                                            SendReply(arg, "You must enter a cooldown for this item");
                                            return;
                                        }

                                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(shortname);
                                        if (itemDefinition != null)
                                        {
                                          
                                            RewardData.RewardItem newItem = new RewardData.RewardItem
                                            {
                                                Amount = amount,
                                                Name = itemDefinition.displayName.translated,
                                                SkinID = skinId,
                                                Shortname = shortname,
                                                Cooldown = cooldown,
                                                IsBP = (args.Length >= 7 && args[6].ToLower() == "bp"),
                                                Nitro = isNitro
                                            };
                                           
                                            rewardData.items.Add(newItem);
                                            SendReply(arg, $"You have added {itemDefinition.displayName.english} to DiscordRewards");
                                            SaveRewards();
                                        }
                                        else SendReply(arg, "Invalid item selected!");
                                    }
                                    else SendReply(arg, "discord.rewards add item <shortname> <skinId> <amount> <cooldown> <opt:bp>");
                                    return;

                                case "kit":
                                    if (args.Length >= 5)
                                    {                                      
                                        int cooldown = 0;
                                        if (!int.TryParse(args[4], out cooldown))
                                        {
                                            SendReply(arg, "You must enter a cooldown for this kit");
                                            return;
                                        }

                                        object isKit = Kits?.Call("isKit", new object[] { args[3] });
                                        if (isKit is bool && (bool)isKit)
                                        {                                            
                                            rewardData.kits.Add(new RewardData.RewardKit { Name = args[2], Kit = args[3], Description = "", Cooldown = cooldown,
                                                Nitro = isNitro
                                            });
                                            SendReply(arg, $"You have added {args[3]} to DiscordRewards");
                                            SaveRewards();
                                        }
                                        else SendReply(arg, "Invalid kit selected");
                                    }
                                    else SendReply(arg, "discord.rewards add kit <Name> <kitname> <cooldown>");
                                    return;

                                case "command":
                                    if (args.Length >= 5)
                                    {                                      
                                        int cooldown = 0;
                                        if (!int.TryParse(args[4], out cooldown))
                                        {
                                            SendReply(arg, "You must enter a cooldown for this kit");
                                            return;
                                        }

                                        rewardData.commands.Add(new RewardData.RewardCommand { Name = args[2], Commands = new List<string>{ args[3] }, Description = "", Cooldown = cooldown,
                                            Nitro = isNitro
                                        });
                                        SendReply(arg, $"You have added a new command group to DiscordRewards");
                                        SaveRewards();
                                    }
                                    else SendReply(arg, "discord.rewards add command <name> <command> <cooldown>");
                                    return;
                            }
                        }

                        return;
                    #endregion
                    #region Removal
                    case "remove":
                        if (args.Length == 3)
                        {
                            int id = 0;
                            if (!int.TryParse(args[2], out id) || id < 0)
                            {
                                SendReply(arg, "You must enter a valid ID number");
                                return;
                            }                            

                            switch (args[1].ToLower())
                            {                                
                                case "kit":
                                    if (id < rewardData.kits.Count)
                                    {
                                        rewardData.kits.RemoveAt(id);
                                        SendReply(arg, $"Successfully removed kit with ID: {id}");
                                        SaveRewards();
                                    }
                                    else SendReply(arg, Message("noKitRem"), "");
                                    return;
                                case "item":
                                    if (id < rewardData.items.Count)
                                    {
                                        rewardData.items.RemoveAt(id);
                                        SendReply(arg, $"Successfully removed item with ID: {id}");
                                        SaveRewards();
                                    }
                                    else SendReply(arg, Message("noItemRem"), "");
                                    return;
                                case "command":
                                    if (id < rewardData.commands.Count)
                                    {
                                        rewardData.commands.RemoveAt(id);
                                        SendReply(arg, $"Successfully removed command with ID: {id}");
                                        SaveRewards();
                                    }
                                    else SendReply(arg, Message("noCommandRem"), "");
                                    return;
                            }
                        }
                        return;
                    #endregion
                    #region Editing
                    case "edit":
                        if (args.Length >= 3)
                        {
                            int id = 0;
                            if (!int.TryParse(args[2], out id) || id < 0)
                            {
                                SendReply(arg, "You must enter a valid ID number");
                                return;
                            }

                            switch (args[1].ToLower())
                            {
                                case "kit":
                                    if (id < rewardData.kits.Count)
                                    {
                                        if (args.Length >= 5)
                                        {
                                            switch (args[3].ToLower())
                                            {                                              
                                                case "description":
                                                    rewardData.kits.ElementAt(id).Description = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Kit {0} description set to {1}", args[2], args[4]));
                                                    return;
                                                case "name":
                                                    rewardData.kits.ElementAt(id).Name = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Kit {0} name set to {1}", args[2], args[4]));
                                                    return;
                                                case "icon":
                                                    rewardData.kits.ElementAt(id).Icon = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Kit {0} icon set to {1}", args[2], args[4]));
                                                    return;
                                                case "cooldown":
                                                    int cooldown = 0;
                                                    if (int.TryParse(args[4], out cooldown))
                                                    {
                                                        rewardData.kits.ElementAt(id).Cooldown = cooldown;
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Kit {0} cooldown set to {1} seconds", args[2], args[4]));
                                                    }
                                                    else SendReply(arg, "You must enter a cooldown number");
                                                    return;
                                                default:
                                                    SendReply(arg, "discord.rewards edit kit <ID> <description|name|icon|cooldown> \"info here\"");
                                                    return; ;
                                            }
                                        }
                                        else SendReply(arg, "discord.rewards edit kit <ID> <description|name|icon|cooldown> \"info here\"");
                                    }
                                    else SendReply(arg, "Invalid ID number selected");
                                    return;
                                case "item":
                                    if (id < rewardData.items.Count)
                                    {
                                        if (args.Length >= 5)
                                        {
                                            switch (args[3].ToLower())
                                            {
                                                case "amount":
                                                    int amount = 0;
                                                    if (int.TryParse(args[4], out amount))
                                                    {
                                                        rewardData.items.ElementAt(id).Amount = amount;
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Item {0} amount set to {1}", args[2], amount));
                                                    }
                                                    else SendReply(arg, "Invalid amount entered");
                                                    return;
                                                case "skinid":
                                                    ulong skinId = 0;
                                                    if (ulong.TryParse(args[4], out skinId))
                                                    {
                                                        rewardData.items.ElementAt(id).SkinID = skinId;
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Item {0} skin set to {1}", args[2], skinId));
                                                    }
                                                    else SendReply(arg, "Invalid skin ID entered");
                                                    return;
                                                case "isbp":
                                                    bool isBp;
                                                    if (bool.TryParse(args[4], out isBp))
                                                    {
                                                        rewardData.items.ElementAt(id).IsBP = isBp;
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Item {0} blueprint set to {1}", args[2], isBp));
                                                    }
                                                    else SendReply(arg, "You must enter true or false");
                                                    return;
                                                case "icon":
                                                    rewardData.items.ElementAt(id).Icon = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Item {0} icon set to {1}", args[2], args[4]));
                                                    return;
                                                case "cooldown":
                                                    int cooldown = 0;
                                                    if (int.TryParse(args[4], out cooldown))
                                                    {
                                                        rewardData.items.ElementAt(id).Cooldown = cooldown;
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Item {0} cooldown set to {1} seconds", args[2], args[4]));
                                                    }
                                                    else SendReply(arg, "You must enter a cooldown number");
                                                    return;
                                                default:
                                                    SendReply(arg, "discord.rewards edit item <ID> <amount|skinid|isbp|icon|cooldown> \"info here\"");
                                                    return;
                                            }
                                        }
                                        else SendReply(arg, "discord.rewards edit item <ID> <amount|skinid|isbp|icon|cooldown> \"info here\"");
                                    }
                                    else SendReply(arg, "Invalid ID number selected");
                                    return;
                                case "command":
                                    if (id < rewardData.commands.Count)
                                    {
                                        if (args.Length >= 5)
                                        {
                                            switch (args[3].ToLower())
                                            {                                              
                                                case "description":
                                                    rewardData.commands.ElementAt(id).Description = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Command {0} description set to {1}", args[2], args[4]));
                                                    return;
                                                case "name":
                                                    rewardData.commands.ElementAt(id).Name = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Command {0} name set to {1}", args[2], args[4]));
                                                    return;
                                                case "icon":
                                                    rewardData.commands.ElementAt(id).Icon = args[4];
                                                    SaveRewards();
                                                    SendReply(arg, string.Format("Command {0} icon set to {1}", args[2], args[4]));
                                                    return;
                                                case "add":
                                                    if (!rewardData.commands.ElementAt(id).Commands.Contains(args[4]))
                                                    {
                                                        rewardData.commands.ElementAt(id).Commands.Add(args[4]);
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Added command \"{1}\" to Reward Command {0}", args[2], args[4]));
                                                    }
                                                    else SendReply(arg, string.Format("The command \"0\" is already registered to this reward command", args[4]));
                                                    return;
                                                case "remove":
                                                    if (rewardData.commands.ElementAt(id).Commands.Contains(args[4]))
                                                    {
                                                        rewardData.commands.ElementAt(id).Commands.Remove(args[4]);
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Removed command \"{1}\" to Command {0}", args[2], args[4]));
                                                    }
                                                    else SendReply(arg, string.Format("The command \"{0}\" is not registered to this reward command", args[4]));
                                                    return;
                                                case "cooldown":
                                                    int cooldown = 0;
                                                    if (int.TryParse(args[4], out cooldown))
                                                    {
                                                        rewardData.commands.ElementAt(id).Cooldown = cooldown;
                                                        SaveRewards();
                                                        SendReply(arg, string.Format("Command {0} cooldown set to {1} seconds", args[2], args[4]));
                                                    }
                                                    else SendReply(arg, "You must enter a cooldown number");
                                                    return;
                                                default:
                                                    SendReply(arg, "discord.rewards edit command <ID> <description|name|icon|add|remove|cooldown> \"info here\"");
                                                    return;
                                            }
                                        }
                                        else SendReply(arg, "discord.rewards edit command <ID> <description|name|icon|add|remove|cooldown> \"info here\"");
                                    }
                                    else SendReply(arg, "Invalid ID number selected");
                                    return;
                            }
                        }
                        return;
                        #endregion
                }
            }
        }
        #endregion

        #region Images
        private void LoadImages()
        {
            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();

            string dataDir = $"file://{Interface.Oxide.DataDirectory}{Path.DirectorySeparatorChar}DiscordRewards{Path.DirectorySeparatorChar}Images{Path.DirectorySeparatorChar}";
            foreach (RewardData.RewardItem item in rewardData.items.Where(x => !string.IsNullOrEmpty(x.Icon)))
            {
                if (newLoadOrder.ContainsKey(item.Icon))
                    continue;
                string url = item.Icon;
                if (!url.StartsWith("http") && !url.StartsWith("www"))
                    url = $"{dataDir}{item.Icon}.png";
                newLoadOrder.Add(item.Icon, url);
            }
            foreach (RewardData.RewardKit kit in rewardData.kits)
            {
                if (!string.IsNullOrEmpty(kit.Icon))
                {
                    if (newLoadOrder.ContainsKey(kit.Icon))
                        continue;
                    string url = kit.Icon;
                    if (!url.StartsWith("http") && !url.StartsWith("www"))
                        url = $"{dataDir}{kit.Icon}.png";
                    newLoadOrder.Add(kit.Icon, url);
                }
            }
            foreach (RewardData.RewardCommand command in rewardData.commands)
            {
                if (!string.IsNullOrEmpty(command.Icon))
                {
                    if (newLoadOrder.ContainsKey(command.Icon))
                        continue;
                    string url = command.Icon;
                    if (!url.StartsWith("http") && !url.StartsWith("www"))
                        url = $"{dataDir}{command.Icon}.png";
                    newLoadOrder.Add(command.Icon, url);
                }
            }
            if (newLoadOrder.Count > 0)
                ImageLibrary.Call("ImportImageList", Title, newLoadOrder);

            ImageLibrary.Call("LoadImageList", Title, rewardData.items.Where(y => string.IsNullOrEmpty(y.Icon)).Select(x => new KeyValuePair<string, ulong>(x.Shortname, x.SkinID)).ToList(), null);
        }

        private string GetImage(string fileName, ulong skin = 0)
        {
            string imageId = (string)ImageLibrary.Call("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        #endregion

        #region API
        private string SteamToDiscordID(ulong playerId)
        {
            UserData.User discordUser;
            if (userData.users.TryGetValue(playerId, out discordUser))
                return discordUser.discordId;

            return string.Empty;
        }

        private string DiscordToSteamID(string discordId)
        {
            ulong playerId;
            if (userData.FindByID(discordId, out playerId))
                return playerId.ToString();
          
            return string.Empty;
        }
        #endregion

        #region Config        
        private ConfigData Configuration;
        private class ConfigData
        {
            public DiscordSettings Settings { get; set; }

            [JsonProperty(PropertyName = "Alternative Rewards")]
            public AlternativeRewards Rewards { get; set; }

            [JsonProperty(PropertyName = "Validation Tokens")]
            public Validation Token { get; set; }

            [JsonProperty(PropertyName = "Global Cooldown")]
            public GlobalCooldown Cooldown { get; set; }           

            [JsonProperty(PropertyName = "UI Options")]
            public UIOptions UISettings { get; set; }

            public class DiscordSettings
            {
                [JsonProperty(PropertyName = "Bot Token")]
                public string APIKey { get; set; }

                [JsonProperty(PropertyName = "Bot Client ID")]
                public string BotID { get; set; }

                [JsonProperty(PropertyName = "Bot Status Messages")]
                public string[] StatusMessages { get; set; }

                [JsonProperty(PropertyName = "Bot Status Cycle Time (seconds)")]
                public int StatusCycle { get; set; }

                [JsonConverter(typeof(StringEnumConverter))]
                [JsonProperty(PropertyName = "Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
                public DiscordLogLevel LogLevel { get; set; }
            }

            public class UIOptions
            {
                [JsonProperty(PropertyName = "Enable Reward Menu")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Selected Button Color")]
                public UIColor Selected { get; set; }

                [JsonProperty(PropertyName = "Deselected Button Color")]
                public UIColor Deselected { get; set; }

                [JsonProperty(PropertyName = "Close Button Color")]
                public UIColor Close { get; set; }

                [JsonProperty(PropertyName = "Claim Button Color")]
                public UIColor Claim { get; set; }

                [JsonProperty(PropertyName = "Nitro Color")]
                public UIColor Nitro { get; set; }

                [JsonProperty(PropertyName = "Background Color")]
                public UIColor Background { get; set; }

                [JsonProperty(PropertyName = "Panel Color")]
                public UIColor Panel { get; set; }

                public class UIColor
                {
                    public string Hex { get; set; }
                    public float Alpha { get; set; }

                    [JsonIgnore]
                    private string _color;

                    [JsonIgnore]
                    public string Color
                    {
                        get
                        {
                            if (string.IsNullOrEmpty(_color))
                                _color = UI.Color(Hex, Alpha);
                            return _color;
                        }
                    }
                }
            }

            public class GlobalCooldown
            {
                [JsonProperty(PropertyName = "Use Global Cooldown")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Global Cooldown Time (seconds)")]
                public int Time { get; set; }
            }

            public class Validation
            {
                [JsonProperty(PropertyName = "Token Lifetime (seconds)")]
                public int TokenLife { get; set; }

                [JsonProperty(PropertyName = "Require Re-validation")]
                public bool RequireRevalidation { get; set; }

                [JsonProperty(PropertyName = "Automatically try and re-validate users when their token has expired")]
                public bool AutoRevalidation { get; set; }

                [JsonProperty(PropertyName = "Revalidation Interval (seconds)")]
                public int RevalidationInterval { get; set; }

                [JsonProperty(PropertyName = "Revoke rewards and wipe token data on map wipe")]
                public bool WipeReset { get; set; }

                [JsonProperty(PropertyName = "Reset reward cooldowns on map wipe")]
                public bool WipeResetRewards { get; set; }

                [JsonProperty(PropertyName = "Validation channel")]
                public string ValidationChannel { get; set; }
            }

            public class AlternativeRewards
            {
                [JsonProperty(PropertyName = "Add user to user groups")]
                public string[] Groups { get; set; }

                [JsonProperty(PropertyName = "Commands to run on successful validation")]
                public string[] Commands { get; set; }

                [JsonProperty(PropertyName = "Permissions to grant on successful validation")]
                public string[] Permissions { get; set; }

                [JsonProperty(PropertyName = "Discord roles to grant on successful validation")]
                public string[] Roles { get; set; }

                [JsonProperty(PropertyName = "Discord roles to revoke on successful validation")]
                public string[] RevokeRoles { get; set; }

                [JsonProperty(PropertyName = "[Nitro Boosters] Add user to user groups")]
                public string[] NitroGroups { get; set; }

                [JsonProperty(PropertyName = "[Nitro Boosters] Commands to run on successful validation")]
                public string[] NitroCommands { get; set; }

                [JsonProperty(PropertyName = "[Nitro Boosters] Permissions to grant on successful validation")]
                public string[] NitroPermissions { get; set; }

            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Configuration = Config.ReadObject<ConfigData>();

            if (Configuration.Version < Version)
                UpdateConfigValues();

            VerifyConfigContents();

            Config.WriteObject(Configuration, true);
        }

        protected override void LoadDefaultConfig() => Configuration = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Cooldown = new ConfigData.GlobalCooldown
                {
                    Enabled = false,
                    Time = 84600
                },
                Settings = new ConfigData.DiscordSettings
                {
                    APIKey = "",
                    BotID = "",
                    LogLevel = DiscordLogLevel.Info,
                    StatusMessages = new string[0],
                    StatusCycle = 120,
                },
                Token = new ConfigData.Validation
                {
                    RevalidationInterval = 84600,
                    TokenLife = 3600,
                    AutoRevalidation = true,
                    RequireRevalidation = true,
                    WipeReset = false,
                    ValidationChannel = string.Empty,
                    WipeResetRewards = false
                },
                Rewards = new ConfigData.AlternativeRewards
                {
                    Commands = new string[0],
                    Groups = new string[0],
                    Permissions = new string[0],
                    RevokeRoles = new string[0],
                    Roles = new string[0],
                    NitroCommands = new string[0],
                    NitroGroups = new string[0],
                    NitroPermissions = new string[0]
                },
                UISettings = new ConfigData.UIOptions
                {
                    Enabled = true,
                    Selected = new ConfigData.UIOptions.UIColor { Hex = "#6a8b38", Alpha = 1f },
                    Deselected = new ConfigData.UIOptions.UIColor { Hex = "#007acc", Alpha = 1f },
                    Close = new ConfigData.UIOptions.UIColor { Hex = "#d85540", Alpha = 1f },
                    Claim = new ConfigData.UIOptions.UIColor { Hex = "#d08822", Alpha = 1f },
                    Nitro = new ConfigData.UIOptions.UIColor { Hex = "#dc16f5", Alpha = 1f },
                    Background = new ConfigData.UIOptions.UIColor { Hex = "#2b2b2b", Alpha = 1f },
                    Panel = new ConfigData.UIOptions.UIColor { Hex = "#232323", Alpha = 1f },
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(Configuration, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();
           
            if (Configuration.Version < new VersionNumber(0, 1, 2))
            {
                Configuration.UISettings.Enabled = true;
                Configuration.Token.RequireRevalidation = true;
                Configuration.Rewards = baseConfig.Rewards;
            }

            if (Configuration.Version < new VersionNumber(0, 1, 4))
                Configuration.Rewards.Roles = new string[0];

            if (Configuration.Version < new VersionNumber(0, 1, 10))
            {
                Configuration.Settings.StatusMessages = new string[0];
                Configuration.Settings.StatusCycle = 120;
            }

            if (Configuration.Version < new VersionNumber(0, 1, 12))
                Configuration.Token.WipeReset = false;

            if (Configuration.Version < new VersionNumber(0, 1, 19))
                Configuration.Rewards.RevokeRoles = new string[0];

            if (Configuration.Version < new VersionNumber(0, 1, 20))
            {
                Configuration.Rewards.NitroCommands = new string[0];
                Configuration.Rewards.NitroGroups = new string[0];
                Configuration.Rewards.NitroPermissions = new string[0];
            }

            if (Configuration.Version < new VersionNumber(0, 2, 0))
            {
                Configuration.Settings.LogLevel = DiscordLogLevel.Info;
                Configuration.Token.ValidationChannel = string.Empty;

                Configuration.UISettings.Background = baseConfig.UISettings.Background;
                Configuration.UISettings.Deselected = baseConfig.UISettings.Deselected;
                Configuration.UISettings.Nitro = baseConfig.UISettings.Nitro;
                Configuration.UISettings.Selected = baseConfig.UISettings.Selected;
                Configuration.UISettings.Panel = baseConfig.UISettings.Panel;
            }

            Configuration.Version = Version;
            PrintWarning("Config update completed!");
        }

        private void VerifyConfigContents()
        {
            if (Configuration.Rewards.Commands == null)
                Configuration.Rewards.Commands = new string[0];

            if (Configuration.Rewards.Groups == null)
                Configuration.Rewards.Groups = new string[0];

            if (Configuration.Rewards.Permissions == null)
                Configuration.Rewards.Permissions = new string[0];

            if (Configuration.Rewards.Roles == null)
                Configuration.Rewards.Roles = new string[0];

            if (Configuration.Rewards.RevokeRoles == null)
                Configuration.Rewards.RevokeRoles = new string[0];

            if (Configuration.Rewards.NitroCommands == null)
                Configuration.Rewards.NitroCommands = new string[0];

            if (Configuration.Rewards.NitroGroups == null)
                Configuration.Rewards.NitroGroups = new string[0];

            if (Configuration.Rewards.NitroPermissions == null)
                Configuration.Rewards.NitroPermissions = new string[0];
        }
        #endregion

        #region Data Management
        private UserData userData;
        private RewardData rewardData;

        private DynamicConfigFile userdata, rewarddata;

        private void SaveData() => userdata.WriteObject(userData);

        private void SaveRewards() => rewarddata.WriteObject(rewardData);

        private void WipeData()
        {
            foreach (KeyValuePair<ulong, UserData.User> kvp in userData.users)
                RevokeRewards(kvp.Key, kvp.Value);

            userData.users.Clear();
            SaveData();
            UpdateStatus();
        }

        private void WipeRewardCooldowns()
        {
            foreach (KeyValuePair<ulong, UserData.User> kvp in userData.users)
                kvp.Value.WipeCooldowns();

            SaveData();
        }

        private void LoadData()
        {
            userdata = Interface.Oxide.DataFileSystem.GetFile("DiscordRewards/userdata");
            rewarddata = Interface.Oxide.DataFileSystem.GetFile("DiscordRewards/rewarddata");

            userData = userdata.ReadObject<UserData>();
            if (userData == null)
                userData = new UserData();

            rewardData = rewarddata.ReadObject<RewardData>();
            if (rewardData == null)
                rewardData = new RewardData();
        }

        private class UserData
        {
            public Dictionary<ulong, User> users = new Dictionary<ulong, User>();
            public Hash<int, DiscordToken> tokenToUser = new Hash<int, DiscordToken>();

            public User AddNewUser(ulong playerId, string discordId)
            {
                User userData = new User(discordId);
                users.Add(playerId, userData);
                return userData;
            }

            public User GetUser(ulong userId)
            {
                User user;
                if (users.TryGetValue(userId, out user))
                    return user;
                return null;
            }

            public bool FindByID(string discordId, out ulong userId)
            {
                foreach(KeyValuePair<ulong, User> user in users)
                {
                    if (user.Value.discordId.Equals(discordId))
                    {
                        userId = user.Key;
                        return true;
                    }
                }

                userId = 0UL;
                return false;
            }

            public void AddToken(int code, ulong playerId, int duration)
            {
                tokenToUser.Add(code, new DiscordToken(playerId, duration));
            }

            public bool HasPendingToken(ulong playerId, out int code)
            {
                code = -1;
                foreach(KeyValuePair<int, DiscordToken> kvp in tokenToUser)                
                {
                    if (kvp.Value.expireTime < CurrentTime())
                    {
                        tokenToUser.Remove(kvp.Key);
                        return false;
                    }
                    code = kvp.Key;
                    return true;
                }
                return false;
            }

            public bool IsValidToken(int code, out DiscordToken token)
            {
                return tokenToUser.TryGetValue(code, out token);
            }
            
            public void InvalidateToken(int token) => tokenToUser.Remove(token);

            public class DiscordToken
            {
                public ulong playerId;
                public double expireTime;

                public DiscordToken(ulong playerId, int duration)
                {
                    this.playerId = playerId;
                    this.expireTime = CurrentTime() + duration;
                }
            }

            public class User
            {                
                public string discordId;

                public double expireTime;
                public double globalTime;

                public bool isNitroBooster = false;

                public HashSet<string> groups = new HashSet<string>();
                public HashSet<string> permissions = new HashSet<string>();
                public HashSet<string> roles = new HashSet<string>();

                public HashSet<string> nitroGroups = new HashSet<string>();
                public HashSet<string> nitroPermissions = new HashSet<string>();

                [JsonIgnore]
                private Snowflake _id;

                [JsonIgnore]
                public Snowflake Id
                {
                    get
                    {
                        if (_id.Equals(default(Snowflake)))
                        {
                            _id = new Snowflake(ulong.Parse(discordId));
                        }
                        return _id;
                    }
                    set
                    {
                        discordId = value.Id.ToString();
                        _id = value;
                    }
                }

                public User(string discordId)
                {
                    this.discordId = discordId;
                }

                public void SetExpiryDate(int duration)
                {
                    this.expireTime = CurrentTime() + duration;
                }

                public Dictionary<RewardType, Dictionary<int, double>> items = new Dictionary<RewardType, Dictionary<int, double>>
                {
                    [RewardType.Command] = new Dictionary<int, double>(),
                    [RewardType.Item] = new Dictionary<int, double>(),
                    [RewardType.Kit] = new Dictionary<int, double>()
                };

                public void AddCooldown(RewardType type, int id, int time)
                {
                    if (!items[type].ContainsKey(id))
                        items[type].Add(id, time + CurrentTime());
                    else items[type][id] = time + CurrentTime();
                }

                public void AddCooldown(int time)
                {
                    globalTime = CurrentTime() + time;
                }

                public bool HasCooldown(RewardType type, int id, out double remaining)
                {
                    remaining = 0;
                    double time;
                    if (items[type].TryGetValue(id, out time))
                    {
                        double currentTime = CurrentTime();
                        if (time > currentTime)
                        {
                            remaining = time - currentTime;
                            return true;
                        }
                    }
                    return false;
                }

                public bool HasCooldown(out double remaining)
                {
                    remaining = globalTime - CurrentTime();
                    return remaining > 0;
                }

                public void WipeCooldowns()
                {
                    items = new Dictionary<RewardType, Dictionary<int, double>>
                    {
                        [RewardType.Command] = new Dictionary<int, double>(),
                        [RewardType.Item] = new Dictionary<int, double>(),
                        [RewardType.Kit] = new Dictionary<int, double>()
                    };
                }
            }
        }

        private class RewardData
        {
            public List<RewardItem> items = new List<RewardItem>();
            public List<RewardKit> kits = new List<RewardKit>();
            public List<RewardCommand> commands = new List<RewardCommand>();

            public class RewardItem : BaseReward
            {
                public string Shortname { get; set; }
                public int Amount { get; set; }
                public ulong SkinID { get; set; }
                public bool IsBP { get; set; }

                internal override string RewardType => "item";

                public override void GiveReward(BasePlayer player)
                {
                    Item item = null;
                    if (IsBP)
                    {
                        item = ItemManager.CreateByItemID(-996920608, Amount, SkinID);
                        item.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == Shortname)?.itemid ?? 0;
                    }
                    else item = ItemManager.CreateByName(Shortname, Amount, SkinID);
                    player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                }

                internal override void CreateIconImage(CuiElementContainer container, UI4 position)
                {
                    string icon = string.IsNullOrEmpty(Icon) ? Instance.GetImage(Shortname, SkinID) : Instance.GetImage(Icon, 0);
                    if (!string.IsNullOrEmpty(icon))                    
                        UI.Image(container, UI_MENU, icon, new UI4(position.xMin + 0.015f, position.yMin + 0.16f, position.xMax - 0.015f, position.yMax));
                }

                internal override void CreateNameLabel(CuiElementContainer container, UI4 position)
                {
                    UI.Label(container, UI_MENU, $"{Name}{(IsBP ? " (BP)" : "")}{(Amount > 1 ? $" x{Amount}" : "")}", 10, new UI4(position.xMin, position.yMin + 0.08f, position.xMax, position.yMin + 0.16f));
                }

                internal override void CreateDescriptionLabel(CuiElementContainer container, UI4 position) { }
            }

            public class RewardCommand : BaseReward
            {
                public List<string> Commands { get; set; }

                internal override string RewardType => "command";

                public override void GiveReward(BasePlayer player)
                {
                    foreach (string cmd in Commands)
                        Interface.Oxide.GetLibrary<Game.Rust.Libraries.Rust>().RunServerCommand(cmd.Replace("$player.id", player.UserIDString).Replace("$player.name", player.displayName).Replace("$player.x", player.transform.position.x.ToString()).Replace("$player.y", player.transform.position.y.ToString()).Replace("$player.z", player.transform.position.z.ToString()));
                }
            }

            public class RewardKit : BaseReward
            {               
                public string Kit { get; set; }

                internal override string RewardType => "kit";

                public override void GiveReward(BasePlayer player)
                {
                    Instance.Kits?.Call("GiveKit", player, Kit);
                }
            }

            public class BaseReward
            {
                public string Name { get; set; }
                public string Description { get; set; }
                public int Cooldown { get; set; }
                public string Icon { get; set; }
                public bool Nitro { get; set; }

                [JsonIgnore]
                internal virtual string RewardType { get; }

                public virtual void GiveReward(BasePlayer player) { }

                internal virtual void AddUIEntry(CuiElementContainer container, UI4 position, int listIndex, ulong playerId, bool isNitroBooster)
                {
                    UI.Panel(container, UI_MENU, Instance.Configuration.UISettings.Panel.Color, position);

                    CreateIconImage(container, position);

                    CreateNameLabel(container, position);

                    CreateDescriptionLabel(container, position);

                    UI.Label(container, UI_MENU, Name, 14, new UI4(position.xMin, position.yMin + 0.04f, position.xMax, position.yMin + 0.09f));

                    UI.Button(container, UI_MENU, Nitro ? Instance.Configuration.UISettings.Nitro.Color : Instance.Configuration.UISettings.Claim.Color,
                                            Nitro && !isNitroBooster ? Instance.Message("UI.NitroOnly", playerId) : Instance.Message("UI.Claim", playerId),
                                            12, new UI4(position.xMin + 0.005f, position.yMin + 0.01f, position.xMax - 0.005f, position.yMin + 0.08f),
                                            Nitro && !isNitroBooster ? string.Empty : $"drui.claim {RewardType} {listIndex}");
                }

                internal virtual void CreateIconImage(CuiElementContainer container, UI4 position)
                {
                    if (!string.IsNullOrEmpty(Icon))
                    {
                        string itemIcon = Instance.GetImage(Icon, 0);
                        if (!string.IsNullOrEmpty(itemIcon))
                            UI.Image(container, UI_MENU, itemIcon, new UI4(position.xMin + 0.015f, position.yMin + 0.16f, position.xMax - 0.015f, position.yMax));
                    }
                }

                internal virtual void CreateNameLabel(CuiElementContainer container, UI4 position)
                {
                    if (!string.IsNullOrEmpty(Name))
                        UI.Label(container, UI_MENU, Name, 10, new UI4(position.xMin, position.yMin + 0.08f, position.xMax, position.yMin + 0.16f));
                }

                internal virtual void CreateDescriptionLabel(CuiElementContainer container, UI4 position)
                {
                    if (!string.IsNullOrEmpty(Description))
                        UI.Label(container, UI_MENU, Description, 10, new UI4(position.xMin + 0.02f, position.yMin + 0.16f, position.xMax - 0.02f, position.yMax - 0.04f), TextAnchor.UpperCenter);
                }
            }
        }
        #endregion

        #region Localization
        private string Message(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
       
        private Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Token"] = "Type <color=#ce422b>/discord token</color> to get a unique 6 digit token.",
            ["Help.BotOnly"] = "When you have your unique token, DM the token to our bot (<color=#ce422b>{0}</color>) on Discord to verify your account",
            ["Help.BotOrChannel"] = "When you have your unique token, either DM the token to our bot (<color=#ce422b>{0}</color>) on Discord or post the token in the <color=#ce422b>#{1}</color> channel to verify your account",
            
            ["Message.Token"] = "Your unique token is <color=#ce422b>{0}</color>",
            ["Message.RewardGiven"] = "<color=#ce422b>Thanks for being a part of our community!</color> You have received your reward",
            ["Message.OnCooldown"] = "You have cooldown on this reward for another <color=#ce422b>{0}</color>",
            ["Message.OnCooldownGlobal"] = "You have cooldown for another <color=#ce422b>{0}</color>",
            ["Message.ValidationExpired"] = "Your Discord validation token has expired! Type <color=#ce422b>/discord</color> to re-validate",
            ["Message.AutoValidated"] = "Your Discord validation token has expired, however we can see you are still in our Discord so you have been automatically re-validated!",
            ["Message.AlreadyRegistered"] = "You are already a member of the Discord group",
            
            ["Error.NoItems"] = "The Discord Reward store currently has no items...",
            ["Error.PendingToken"] = "<color=#ce422b>You already have a token pending validation.</color> DM your unique token (<color=#ce422b>{0}</color>) to our bot (<color=#ce422b>{1}</color>) to continue!",

            ["Discord.TokenExpires"] = "This token will expire in {0}.",
            ["Discord.ValidatedToken"] = "Your token has been validated!",
            ["Discord.InvalidToken"] = "The token you entered is invalid. Please copy the 6 digit token you recieved from ingame chat",
            ["Discord.TokenExpired"] = "The token you entered has expired. Please request a new token via the /discord command ingame",
            ["Discord.FailedToFindPlayer"] = "Failed to find a online player with the Steam ID {0}. Unable to complete validation",
            ["Discord.NotOnServer"] = "You must be online in the game server to complete validation",
            ["Discord.UserIsDead"] = "You are currently dead. Some rewards issued on validation may not work whilst you are dead. Try again when you are alive",
            ["Discord.OpenStore"] = "Type /discord in game to open the reward selection menu",

            ["UI.Title"] = "Discord Rewards",
            ["UI.Claim"] = "Claim",
            ["UI.NitroOnly"] = "Nitro Boosters Only",
            ["UI.Kits"] = "Kits",
            ["UI.Items"] = "Items",
            ["UI.Commands"] = "Commands",
        };
        #endregion
    }
}
