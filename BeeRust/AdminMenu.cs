using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Ext.Chaos;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json.Converters;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Chaos.Data;
using Oxide.Ext.Chaos.UIFramework;
using UnityEngine.UI;

using Chaos = Oxide.Ext.Chaos;
using Color = Oxide.Ext.Chaos.UIFramework.Color;
using GridLayoutGroup = Oxide.Ext.Chaos.UIFramework.GridLayoutGroup;
using HorizontalLayoutGroup = Oxide.Ext.Chaos.UIFramework.HorizontalLayoutGroup;
using VerticalLayoutGroup = Oxide.Ext.Chaos.UIFramework.VerticalLayoutGroup;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("AdminMenu", "123", "2.0.10")]
    class AdminMenu : ChaosPlugin
    {
	    private Datafile<RecentPlayers> m_RecentPlayers;

	    private CommandCallbackHandler m_CallbackHandler;
        
	    private readonly Hash<ulong, UIUser> m_UIUsers = new Hash<ulong, UIUser>();
	    private readonly List<KeyValuePair<string, bool>> m_Permissions = new List<KeyValuePair<string, bool>>();
	    
	    private readonly string[] m_IgnoreItems = new string[] { "ammo.snowballgun", "blueprintbase", "rhib", "spraycandecal", "vehicle.chassis", "vehicle.module", "water", "water.salt" };

        [Chaos.Permission] private const string USE_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PERM_PERMISSION = "adminmenu.userpopassssss";
        [Chaos.Permission] private const string GROUP_PERMISSION = "adminmenu.userpopassssss";
        [Chaos.Permission] private const string CONVAR_PERMISSION = "adminmenu.userpopass";

        [Chaos.Permission] private const string GIVE_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string GIVE_SELF_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_PERMISSION = "adminmenu.userpopass";

        [Chaos.Permission] private const string PLAYER_KICKBAN_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_MUTE_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_BLUERPRINTS_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_HURT_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_HEAL_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_KILL_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_STRIP_PERMISSION = "adminmenu.userpopass";
        [Chaos.Permission] private const string PLAYER_TELEPORT_PERMISSION = "adminmenu.userpopass";

        #region Oxide Hooks
        private void Init()
	    {
		    m_MenuTypes = (MenuType[])Enum.GetValues(typeof(MenuType));
		    m_PermissionSubTypes = (int[])Enum.GetValues(typeof(PermissionSubType));
		    m_GroupSubTypes = (int[])Enum.GetValues(typeof(GroupSubType));
		    m_CommandSubTypes = (int[]) Enum.GetValues(typeof(CommandSubType));
		    
		    m_RecentPlayers = new Datafile<RecentPlayers>("AdminMenu/recent_players");
		    m_RecentPlayers.Data.PurgeCollection();

		    m_CallbackHandler = new CommandCallbackHandler(this);

		    SetupPlayerActions();
		    
		    cmd.AddChatCommand("penisslonika", this, ((player, command, args) =>
		    {
			    if (!permission.UserHasPermission(player.UserIDString, USE_PERMISSION))
			    {
				    player.LocalizedMessage(this, "Error.NoPermission");
				    return;
			    }
			    
			    BaseContainer root = BaseContainer.Create(ADMINMENU_MOUSE, Layer.Hud, Anchor.Center, Offset.Default)
				    .NeedsCursor()
				    .NeedsKeyboard();
			    
			    ChaosUI.Show(player, root);
			    CreateAdminMenu(player);
		    }));
	    }

	    private void OnServerInitialized()
	    {
		    List<string> commandPermissions = Facepunch.Pool.GetList<string>();
		    
		    commandPermissions.AddRange(Configuration.ChatCommands.Select(x => x.RequiredPermission));
		    commandPermissions.AddRange(Configuration.ConsoleCommands.Select(x => x.RequiredPermission));
		    Configuration.PlayerInfoCommands.ForEach(customCommand => commandPermissions.AddRange(customCommand.Commands.Select(x => x.RequiredPermission)));

		    foreach (string perm in commandPermissions)
		    {
			    if (!string.IsNullOrEmpty(perm) && perm.StartsWith("adminmenu.", StringComparison.OrdinalIgnoreCase))
				    permission.RegisterPermission(perm, this);
		    }
		    
		    Facepunch.Pool.FreeList(ref commandPermissions);
		    
		    if (ImageLibrary.IsLoaded)
		    {
			    ImageLibrary.AddImage("https://chaoscode.io/oxide/Images/magnifyingglass.png", "adminmenu.search", 0UL, () =>
			    {
				    m_MagnifyImage = ImageLibrary.GetImage("adminmenu.search", 0UL);
			    });
		    }

		    m_ItemDefinitionsPerCategory = new Hash<ItemCategory, List<ItemDefinition>>();
		    foreach (ItemDefinition itemDefinition in ItemManager.itemList)
		    {
			    if (m_IgnoreItems.Contains(itemDefinition.shortname))
				    continue;
			    
			    List<ItemDefinition> list;
			    if (!m_ItemDefinitionsPerCategory.TryGetValue(itemDefinition.category, out list))
				    list = m_ItemDefinitionsPerCategory[itemDefinition.category] = new List<ItemDefinition>();
			    
			    list.Add(itemDefinition);

			    m_AllItemDefinitions.Add(itemDefinition);
		    }

		    foreach (KeyValuePair<ItemCategory, List<ItemDefinition>> kvp in m_ItemDefinitionsPerCategory)
			    kvp.Value.Sort(((a, b) => a.displayName.english.CompareTo(b.displayName.english)));
	    }
	    
	    private void OnPermissionRegistered(string name, Plugin owner) => UpdatePermissionList();

	    private void OnPluginUnloaded(Plugin plugin) => UpdatePermissionList();

	    private void OnPlayerConnected(BasePlayer player) => m_RecentPlayers.Data.OnPlayerConnected(player);
	    
	    private void OnPlayerDisconnected(BasePlayer player)
	    {
		    m_RecentPlayers.Data.OnPlayerDisconnected(player);
		    
		    ChaosUI.Destroy(player, ADMINMENU_MOUSE);
		    ChaosUI.Destroy(player, ADMINMENU_UI);
		    ChaosUI.Destroy(player, ADMINMENU_UI_POPUP);

		    m_UIUsers.Remove(player.userID);
	    }

	    private void OnServerSave() => m_RecentPlayers.Save();

	    private void Unload()
	    {
		    foreach (BasePlayer player in BasePlayer.activePlayerList)
			    OnPlayerDisconnected(player);
	    }
	    #endregion
	    
	    #region Functions
	    private void UpdatePermissionList()
	    {
		    m_Permissions.Clear();

		    List<string> permissions = Facepunch.Pool.GetList<string>();
		    List<Plugin> plugin = Facepunch.Pool.GetList<Plugin>();
		    
		    permissions.AddRange(permission.GetPermissions());
		    permissions.RemoveAll(x => x.ToLower().StartsWith("oxide."));
		    permissions.Sort();
		    
		    plugin.AddRange(plugins.PluginManager.GetPlugins());
		    
		    string lastName = string.Empty;
		    foreach (string perm in permissions)
		    {
			    string name;
			    if (perm.Contains("."))
			    {
				    string permStart = perm.Substring(0, perm.IndexOf("."));
				    name = plugin.Find(x => x?.Name?.ToLower() == permStart)?.Title ?? permStart;
			    }
			    else name = perm;
			    
			    if (lastName != name)
			    {
				    m_Permissions.Add(new KeyValuePair<string, bool>(name, false));
				    lastName = name;
			    }

			    m_Permissions.Add(new KeyValuePair<string, bool>(perm, true));
		    }
		    
		    Facepunch.Pool.FreeList(ref permissions);
		    Facepunch.Pool.FreeList(ref plugin);
	    }

	    private bool HasPermissionForMenuType(BasePlayer player, MenuType menuType)
	    {
		    switch (menuType)
		    {
			    case MenuType.Commands:
				    return true;
			    case MenuType.Permissions:
				    return player.HasPermission(PERM_PERMISSION);
			    case MenuType.Groups:
				    return player.HasPermission(GROUP_PERMISSION);
			    case MenuType.Convars:
				    return player.HasPermission(CONVAR_PERMISSION);
			    case MenuType.Give:
				    return player.HasPermission(GIVE_PERMISSION);
		    }

		    return false;
	    }

	    private bool HasPermissionForSubMenu(BasePlayer player, MenuType menuType, int subMenuIndex)
	    {
		    if (menuType == MenuType.Commands)
		    {
			    if (subMenuIndex == (int) CommandSubType.PlayerInfo)
				    return player.HasPermission(PLAYER_PERMISSION);
		    }

		    return true;
	    }

	    private bool UserHasPermissionNoGroup(string playerId, string perm)
	    {
		    if (string.IsNullOrEmpty(perm))
			    return false;
		    
		    Core.Libraries.UserData userData = permission.GetUserData(playerId);
		    
		    return userData != null && userData.Perms.Contains(perm, StringComparer.OrdinalIgnoreCase);
	    }

	    private bool UsersGroupsHavePermission(string playerId, string perm)
	    {
		    if (string.IsNullOrEmpty(perm))
			    return false;
		    
		    Core.Libraries.UserData userData = permission.GetUserData(playerId);
		    
		    return userData != null && permission.GroupsHavePermission(userData.Groups, perm);
	    }
	    #endregion
	    
	    #region Types
	    protected enum MenuType { Commands, Permissions, Groups, Convars, Give }

	    protected enum PermissionSubType { Player, Group }
	    
	    [JsonConverter(typeof(StringEnumConverter))]
	    protected enum CommandSubType { Chat, Console, PlayerInfo }
	    
	    protected enum GroupSubType { List, Create, UserGroups, GroupUsers }
	    #endregion
	    
	    #region Localization
	    protected override void PopulatePhrases()
	    {
		    m_Messages = new Dictionary<string, string>
		    {
			    ["Button.Exit"] = "Exit",
			    ["Button.Give"] = "Give",
			    ["Button.Cancel"] = "Cancel",
			    ["Button.Confirm"] = "Confirm",
			    ["Button.Create"] = "Create",
			    ["Button.Delete"] = "Delete",
			    ["Button.Clone"] = "Clone",
			    ["Button.Remove"] = "Remove",
			    
			    ["Label.Amount"] = "Amount",
			    ["Label.SkinID"] = "Skin ID",
			    ["Label.InheritedPermission"] = "Inherited from group",
			    ["Label.DirectPermission"] = "Has direct permission",
			    ["Label.TogglePermission"] = "Toggle permissions for : {0}",
			    ["Label.ToggleGroup"] = "Toggle groups for : {0}",
			    ["Label.SelectPlayer"] = "Select a player",
			    ["Label.SelectPlayer1"] = "Select player for first argument",
			    ["Label.SelectPlayer2"] = "Select player for second argument",
			    ["Label.SelectGroup"] = "Select a usergroup",
			    ["Label.Reason"] = "Reason",
			    ["Label.Kick"] = "Do you want to kick {0}?",
			    ["Label.Ban"] = "Do you want to ban {0}?",
			    ["Label.CreateUsergroup"] = "Create Usergroup",
			    ["Label.CloneUsergroup"] = "Clone Usergroup from {0}",
			    ["Label.Name"] = "Name",
			    ["Label.Title"] = "Title (optional)",
			    ["Label.Rank"] = "Rank (optional)",
			    ["Label.CopyUsers"] = "Copy Users",
			    ["Label.DeleteConfirm"] = "Are you sure you want to delete {0}?",
			    ["Label.ViewGroups"] = "Viewing Oxide user groups",
			    ["Label.GiveToPlayer"] = "Select a item to give to {0}",
			    ["Label.ViewGroupUsers"] = "Viewing users in group {0}",
			    ["Label.OfflinePlayers"] = "Offline Players",
			    ["Label.OnlinePlayers"] = "Online Players",
			    
			    ["Notification.RunCommand"] = "You have run the command : {0}",
			    ["Notification.Give.Success"] = "You have given {0} {1} x {2}",

			    ["PlayerInfo.Info"] = "Player Information",
			    ["PlayerInfo.Actions"] = "Actions",
			    ["PlayerInfo.CustomActions"] = "Custom Actions",
			    ["PlayerInfo.Name"] = "Name : {0}",
		        ["PlayerInfo.ID"] = "ID : {0}",
		        ["PlayerInfo.Auth"] = "Auth Level : {0}",
		        ["PlayerInfo.Status"] = "Status : {0}",
		        ["PlayerInfo.Position"] = "World Position : {0}",
		        ["PlayerInfo.Grid"] = "Grid Location : {0}",
		        ["PlayerInfo.Health"] = "Health : {0}",
		        ["PlayerInfo.Calories"] = "Calories : {0}",
		        ["PlayerInfo.Hydration"] = "Hydration : {0}",
		        ["PlayerInfo.Temperature"] = "Temperature : {0}",
		        ["PlayerInfo.Comfort"] = "Comfort : {0}",
		        ["PlayerInfo.Wetness"] = "Wetness : {0}",
		        ["PlayerInfo.Bleeding"] = "Bleeding : {0}",
		        ["PlayerInfo.Radiation"] = "Radiation : {0}",
		        ["PlayerInfo.Clan"] = "Clan : {0}",
		        ["PlayerInfo.Playtime"] = "Playtime : {0}",
		        ["PlayerInfo.AFKTime"] = "AFK Time : {0}",
		        ["PlayerInfo.IdleTime"] = "Idle Time : {0}",
		        ["PlayerInfo.ServerRewards"] = "RP : {0}",
		        ["PlayerInfo.Economics"] = "Economics : {0}",
		        ["Action.Kick"] = "Kick",
				["Action.Ban"] = "Ban",
				["Action.StripInventory"] = "Strip Inventory",
				["Action.ResetMetabolism"] = "Reset Metabolism",
				["Action.GiveBlueprints"] = "Unlock Blueprints",
				["Action.RevokeBlueprints"] = "Revoke Blueprints",
				["Action.Mute"] = "Mute Chat",
				["Action.Unmute"] = "Unmute Chat",
				["Action.Hurt25"] = "Hurt 25%",
				["Action.Hurt50"] = "Hurt 50%",
				["Action.Hurt75"] = "Hurt 75%",
				["Action.Kill"] = "Kill",
				["Action.Heal25"] = "Heal 25%",
				["Action.Heal50"] = "Heal 50%",
				["Action.Heal75"] = "Heal 75%",
				["Action.Heal100"] = "Heal 100%",
				["Action.TeleportSelfTo"] = "Teleport Self To",
				["Action.TeleportToSelf"] = "Teleport To Self",
				["Action.ViewPermissions"] = "View Permissions",
				["Action.TeleportAuthedItem"] = "Teleport Authed Item",
				["Action.TeleportOwnedItem"] = "Teleport Owned Item",

				["Action.StripInventory.Success"] = "{0}'s inventory was stripped",
				["Action.ResetMetabolism.Success"] = "{0}'s metabolism was reset",
				["Action.GiveBlueprints.Success"] = "Unlocked all blueprints for {0}",
				["Action.RevokeBlueprints.Success"] = "Revoked all blueprints for {0}",
				["Action.Mute.Success"] = "{0} is now chat muted",
				["Action.Unmute.Success"] = "{0} chat mute has been lifted",
				["Action.Hurt25.Success"] = "{0}'s health has been reduced by 25%",
				["Action.Hurt50.Success"] = "{0}'s health has been reduced by 50%",
				["Action.Hurt75.Success"] = "{0}'s health has been reduced by 75%",
				["Action.Kill.Success"] = "You have killed {0}",
				["Action.Heal25.Success"] = "{0}'s health has been restored 25%",
				["Action.Heal50.Success"] = "{0}'s health has been restored 50%",
				["Action.Heal75.Success"] = "{0}'s health has been restored 75%",
				["Action.Heal100.Success"] = "{0}'s health has been restored 100%",
				["Action.TeleportSelfTo.Success"] = "Teleported to {0}",
				["Action.TeleportToSelf.Success"] = "Teleported {0} to you",
				["Action.TeleportAuthedItem.Success"] = "Teleported to {0} at {1}",
				["Action.TeleportOwnedItem.Success"] = "Teleported to {0} at {1}",

				["Action.StripInventory.Failed"] = "Failed to strip {0}'s inventory. They may be dead or not on the server",
				["Action.ResetMetabolism.Failed"] = "Failed to reset {0}'s metabolism. They may be dead or not on the server",
				["Action.GiveBlueprints.Failed"] = "Failed to unlock all blueprints for {0}. They may be dead or not on the server",
				["Action.RevokeBlueprints.Failed"] = "Failed to revoked all blueprints for {0}. They may be dead or not on the server",
				["Action.Mute.Failed"] = "Failed to mute chat for {0}. They may be dead or not on the server",
				["Action.Unmute.Failed"] = "Failed to unmute chat for {0}. They may be dead or not on the server",
				["Action.Hurt25.Failed"] = "Failed to reduce {0}'s health. They may be dead or not on the server",
				["Action.Hurt50.Failed"] = "Failed to reduce {0}'s health. They may be dead or not on the server",
				["Action.Hurt75.Failed"] = "Failed to reduce {0}'s health. They may be dead or not on the serverFailed to reduce {0}'s health. They may be dead or not on the server",
				["Action.Kill.Failed"] = "Failed to kill {0}. They may be dead or not on the server",
				["Action.Heal25.Failed"] = "Failed to heal {0}. They may be dead or not on the server",
				["Action.Heal50.Failed"] = "Failed to heal {0}. They may be dead or not on the server",
				["Action.Heal75.Failed"] = "Failed to heal {0}. They may be dead or not on the server",
				["Action.Heal100.Failed"] = "Failed to heal {0}. They may be dead or not on the server",
				["Action.TeleportSelfTo.Failed"] = "Failed to teleport to {0}. They may be dead or not on the server",
				["Action.TeleportToSelf.Failed"] = "Failed to teleport {0} to you. They may be dead or not on the server",
				["Action.TeleportAuthedItem.Failed"] = "Failed to teleport to authed item. The target player may be dead or not on the server",
				["Action.TeleportOwnedItem.Failed"] = "Failed to teleport to owned item. The target player may be dead or not on the server",
				
				["Action.TeleportAuthedItem.Failed.Entities"] = "No entities found for player",
				["Action.TeleportOwnedItem.Failed.Entities"] = "No entities found for player",
				
				["Error.NoPermission"] = "хуя от куда коману узнал?? (там еще пермсы поменяны)"
		    };

		    MenuType[] menuTypes = (MenuType[])Enum.GetValues(typeof(MenuType));
		    for (int i = 0; i < menuTypes.Length; i++)
		    {
			    MenuType menuType = menuTypes[i];
			    m_Messages[$"Category.{menuType}"] = menuType.ToString();
		    }

		    PermissionSubType[] permissionTypes = (PermissionSubType[])Enum.GetValues(typeof(PermissionSubType));
		    for (int i = 0; i < permissionTypes.Length; i++)
		    {
			    PermissionSubType index = permissionTypes[i];
			    m_Messages[$"Permissions.{(int)index}"] = index.ToString();
		    }
		    
		    CommandSubType[] commandTypes = (CommandSubType[])Enum.GetValues(typeof(CommandSubType));
		    for (int i = 0; i < commandTypes.Length; i++)
		    {
			    CommandSubType index = commandTypes[i];
			    m_Messages[$"Commands.{(int)index}"] = index.ToString();
		    }
		    m_Messages[$"Commands.{(int)CommandSubType.PlayerInfo}"] = "Player Info";

		    GroupSubType[] groupTypes = (GroupSubType[])Enum.GetValues(typeof(GroupSubType));
		    for (int i = 0; i < groupTypes.Length; i++)
		    {
			    GroupSubType index = groupTypes[i];
			    m_Messages[$"Groups.{(int)index}"] = index.ToString();
		    }
		    m_Messages[$"Groups.{(int)GroupSubType.UserGroups}"] = "User Groups";
		    m_Messages[$"Groups.{(int)GroupSubType.GroupUsers}"] = "Group Users";
		    
		    ItemCategory[] itemCategories = (ItemCategory[])Enum.GetValues(typeof(ItemCategory));
		    for (int i = 0; i < itemCategories.Length; i++)
		    {
			    ItemCategory index = itemCategories[i];
			    m_Messages[$"Give.{(int)index}"] = index.ToString();
		    }
	    }
	    #endregion
	    
        #region UI
        private string[] m_CharacterFilter = new string[] { "~", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        private string m_MagnifyImage;
        
        private MenuType[] m_MenuTypes;
        private int[] m_PermissionSubTypes;
        private int[] m_GroupSubTypes;
        private int[] m_CommandSubTypes;
        private int[] m_ItemCategoryTypes = new int[] {(int) ItemCategory.Weapon, (int) ItemCategory.Construction, (int)ItemCategory.Items, (int)ItemCategory.Resources, (int)ItemCategory.Attire, (int)ItemCategory.Tool, (int)ItemCategory.Medical, (int)ItemCategory.Food, (int)ItemCategory.Ammunition, (int)ItemCategory.Traps, (int)ItemCategory.Misc, (int)ItemCategory.Component, (int)ItemCategory.Electrical, (int)ItemCategory.Fun};

        private readonly List<ItemDefinition> m_AllItemDefinitions = new List<ItemDefinition>();
        private Hash<ItemCategory, List<ItemDefinition>> m_ItemDefinitionsPerCategory;
        
        private const string ADMINMENU_MOUSE = "adminmenu.ui.mouse";
        private const string ADMINMENU_UI = "adminmenu.ui";
        private const string ADMINMENU_UI_POPUP = "adminmenu.ui.popup";
        
        #region Outline Components
        private OutlineComponent m_OutlineGreen = new OutlineComponent(new Color(0.7695657f, 1f, 0f, 1f));
        private OutlineComponent m_OutlineBlue = new OutlineComponent(new Color(0f, 0.5590005f, 1f, 1f));
        private OutlineComponent m_OutlineRed = new OutlineComponent(new Color(0.8078431f, 0.2588235f, 0.1686275f, 1f));
        private OutlineComponent m_OutlineWhite = new OutlineComponent(new Color(0.1647059f, 0.1803922f, 0.1921569f, 1f));
		#endregion
		
        #region Styles
        private Style m_BackgroundStyle = new Style
        {
	        ImageColor = new Color(0.08235294f, 0.08235294f, 0.08235294f, 0.9490196f),
	        Sprite = Sprites.Background_Rounded,
	        Material = Materials.BackgroundBlur,
	        ImageType = Image.Type.Tiled
        };
        
        private Style m_ButtonStyle = new Style
        {
	        ImageColor = new Color(0.1647059f, 0.1803922f, 0.1921569f, 1f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled,
	        Alignment = TextAnchor.MiddleCenter
        };
        
        private Style m_DisabledButtonStyle = new Style
        {
	        ImageColor = new Color(0.1647059f, 0.1803922f, 0.1921569f, 0.8f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled,
	        FontColor = new Color(1f, 1f, 1f, 0.2f),
	        Alignment = TextAnchor.MiddleCenter
        };

        private Style m_PermissionStyle = new Style
        {
	        ImageColor = new Color(0.1647059f, 0.1803922f, 0.1921569f, 1f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled,
	        FontSize = 12,
	        Alignment = TextAnchor.MiddleCenter
        };
        
        private Style m_PermissionHeaderStyle = new Style
        {
	        ImageColor = new Color(0.8117647f, 0.8117647f, 0.8117647f, 0.8f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled,
	        FontColor = new Color(0.1647059f, 0.1803922f, 0.1921569f, 1f),
	        FontSize = 14,
	        Alignment = TextAnchor.MiddleCenter,
        };

        private Style m_ConvarStyle = new Style
        {
	        ImageColor = new Color(1f, 1f, 1f, 0.172549f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled
        };

        private Style m_ConvarDescriptionStyle = new Style
        {
	        FontColor = new Color(0.745283f, 0.745283f, 0.745283f, 1f),
	        FontSize = 10,
	        Alignment = TextAnchor.LowerLeft
        };
        
        private Style m_PanelStyle = new Style
        {
	        ImageColor = new Color(1f, 1f, 1f, 0.1647059f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled
        };
        
        private Style m_ToggleLabelStyle = new Style
        {
	        FontSize = 40,
	        Alignment = TextAnchor.MiddleCenter,
	        WrapMode = VerticalWrapMode.Overflow,
	        FontColor = new Color(0.7695657f, 1f, 0f, 1f)
        };
        
        private Style m_GroupDeleteButton = new Style
        {
	        ImageColor = new Color(0.8078431f, 0.2588235f, 0.1686275f, 0.5254902f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled
        };
						
        private Style m_GroupCloneButton = new Style
        {
	        ImageColor = new Color(0.7695657f, 1f, 0f, 0.4196078f),
	        Sprite = Sprites.Background_Rounded,
	        ImageType = Image.Type.Tiled
        };
        #endregion
        
        #region Layout Groups
        private HorizontalLayoutGroup m_CategoryLayout = new HorizontalLayoutGroup()
        {
	        Area = new Area(-535f, -15f, 535f, 15f),
	        Spacing = new Spacing(5f, 0f),
	        Padding = new Padding(0f, 0f, 0f, 0f),
	        Corner = Corner.Centered,
	        FixedSize = new Vector2(100, 20),
	        FixedCount = new Vector2Int(5, 0)
        };
        
        private HorizontalLayoutGroup m_SubLayoutGroup = new HorizontalLayoutGroup()
        {
	        Area = new Area(-535f, -12.5f, 535f, 12.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(0f, 0f, 0f, 0f),
	        Corner = Corner.Centered,
	        FixedSize = new Vector2(71.5f, 20),
        };

        private readonly GridLayoutGroup m_ListLayout = new GridLayoutGroup(5, 15, Axis.Vertical)
        {
	        Area = new Area(-522.5f, -257.5f, 522.5f, 257.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };

        private readonly GridLayoutGroup m_ConvarLayout = new GridLayoutGroup(3, 15, Axis.Vertical)
        {
	        Area = new Area(-522.5f, -257.5f, 522.5f, 257.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };
        
        private GridLayoutGroup m_CommandLayoutGroup = new GridLayoutGroup(5, 13, Axis.Horizontal)
        {
	        Area = new Area(-535f, -272.5f, 535f, 272.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };

        private GridLayoutGroup m_GiveLayoutGroup = new GridLayoutGroup(Axis.Horizontal)
        {
	        Area = new Area(-522.5f, -257.5f, 522.5f, 257.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
	        FixedSize = new Vector2(125, 97),
	        FixedCount = new Vector2Int(8, 5),
        };
        
        private VerticalLayoutGroup m_CharacterFilterLayout = new VerticalLayoutGroup
        {
	        Area = new Area(-10f, -257.5f, 10f, 257.5f),
	        Spacing = new Spacing(0f, 3f),
	        Padding = new Padding(2f, 2f, 2f, 2f),
	        Corner = Corner.TopLeft,
	        FixedSize = new Vector2(16, 16),
	        FixedCount = new Vector2Int(1, 27)
        };
        
        private VerticalLayoutGroup m_PlayerInfoLayout = new VerticalLayoutGroup(24)
        {
	        Area = new Area(-100f, -257.5f, 100f, 257.5f),
	        Spacing = new Spacing(0f, 0f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };

        private GridLayoutGroup m_GroupViewLayout = new GridLayoutGroup(4, 15, Axis.Horizontal)
        {
	        Area = new Area(-522.5f, -257.5f, 522.5f, 257.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };

        private VerticalLayoutGroup m_PluginActionsLayout = new VerticalLayoutGroup(19)
        {
	        Area = new Area(-287.5f, -257.5f, 287.5f, 257.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };

        private VerticalLayoutGroup m_CustomActionsLayout = new VerticalLayoutGroup(19)
        {
	        Area = new Area(-142.5f, -257.5f, 142.5f, 257.5f),
	        Spacing = new Spacing(0f, 5f),
	        Padding = new Padding(5f, 5f, 5f, 5f),
	        Corner = Corner.TopLeft,
        };

        private HorizontalLayoutGroup m_InternalPluginActionsLayout = new HorizontalLayoutGroup(6)
        {
	        Area = new Area(-427.5f, -10.92105f, 427.5f, 10.92105f),
	        Spacing = new Spacing(5f, 0f),
	        Padding = new Padding(0f, 0f, 0f, 0f),
	        Corner = Corner.TopLeft,
        };

        private HorizontalLayoutGroup m_InternalCustomActionsLayout = new HorizontalLayoutGroup(2)
        {
	        Area = new Area(-137.5f, -10.92105f, 137.5f, 10.92105f),
	        Spacing = new Spacing(5f, 0f),
	        Padding = new Padding(0f, 0f, 0f, 0f),
	        Corner = Corner.TopLeft,
        };
		#endregion

		#region UI User
		private class UIUser
        {
	        public readonly BasePlayer Player;
	        
	        public MenuType MenuIndex = MenuType.Commands;
	        public int SubMenuIndex = 0;
		    
	        public string SearchFilter = string.Empty;
	        public string CharacterFilter = "~";
	        public int Page = 0;

	        public bool ShowOnlinePlayers = true;
	        public bool ShowOfflinePlayers = false;
	        
	        public string PermissionTarget = string.Empty;
	        public string PermissionTargetName = string.Empty;
	        
	        public ConfigData.CommandEntry CommandEntry = null;
	        public bool RequireTarget1;
	        public bool RequireTarget2;
	        public IPlayer CommandTarget1;
	        public IPlayer CommandTarget2;

	        public string GroupName = string.Empty;
	        public string GroupTitle = string.Empty;
	        public int GroupRank = 0;
	        public bool CopyUsers = false;

	        public int GiveAmount = 1;
	        public ulong SkinID = 0UL;
	        
	        public string KickBanReason = string.Empty;

	        public UIUser(BasePlayer player)
	        {
		        this.Player = player;
	        }

	        public void Reset()
	        {
		        SearchFilter = string.Empty;
		        CharacterFilter = "~";
		        Page = 0;
		        PermissionTarget = string.Empty;
		        PermissionTargetName = string.Empty;
		        KickBanReason = string.Empty;
		        ClearGroup();
		        ClearCommand();
	        }

	        public void ClearGroup()
	        {
		        GroupName = string.Empty;
		        GroupTitle = string.Empty;
		        GroupRank = 0;
		        CopyUsers = false;
	        }

	        public void ClearCommand()
	        {
		        CommandEntry = null;
		        RequireTarget1 = false;
		        RequireTarget2 = false;
		        CommandTarget1 = null;
		        CommandTarget2 = null;
		        GiveAmount = 1;
		        SkinID = 0UL;
	        }
        }
        #endregion
        
        private void CreateAdminMenu(BasePlayer player)
        {
	        UIUser uiUser;
	        if (!m_UIUsers.TryGetValue(player.userID, out uiUser))
		        uiUser = m_UIUsers[player.userID] = new UIUser(player);

	        if (uiUser.MenuIndex == MenuType.Groups && uiUser.SubMenuIndex == (int) GroupSubType.Create)
	        {
		        uiUser.SubMenuIndex = 0;
		        CreateGroupCreateOverlay(uiUser);
		        return;
	        }

	        BaseContainer root = ImageContainer.Create(ADMINMENU_UI, Layer.Overall, Anchor.Center, new Offset(-540f, -310f, 540f, 310f))
		        .WithStyle(m_BackgroundStyle)
		        .WithChildren(mainContainer =>
		        {
			        CreateTitleBar(uiUser, mainContainer);

			        CreateSubMenu(uiUser, mainContainer);

			        BaseContainer subContainer = BaseContainer.Create(mainContainer, Anchor.FullStretch, new Offset(5, 5, -5, -70));
			        switch (uiUser.MenuIndex)
			        {
				        case MenuType.Commands:
					        CommandSubType commandSubType = (CommandSubType) uiUser.SubMenuIndex;

					        if (commandSubType <= CommandSubType.Console)
						        CreateCommandMenu(uiUser, subContainer);
					        else if (commandSubType == CommandSubType.PlayerInfo)
						        CreatePlayerMenu(uiUser, subContainer);
					        break;

				        case MenuType.Permissions:
					        CreatePermissionsMenu(uiUser, subContainer);
					        break;

				        case MenuType.Groups:
					        GroupSubType groupSubType = (GroupSubType) uiUser.SubMenuIndex;

					        if (groupSubType == GroupSubType.List)
						        CreateGroupMenu(uiUser, subContainer);
					        else if (groupSubType == GroupSubType.GroupUsers)
						        CreateGroupUsersMenu(uiUser, subContainer);
					        else if (groupSubType == GroupSubType.UserGroups)
						        CreateUserGroupsMenu(uiUser, subContainer);
					        break;

				        case MenuType.Convars:
					        CreateConvarMenu(uiUser, subContainer);
					        break;

				        case MenuType.Give:
					        CreateGiveMenu(uiUser, subContainer);
					        break;
			        }
		        });
		        //.NeedsCursor()
		        //.NeedsKeyboard();
	        
	        ChaosUI.Destroy(player, ADMINMENU_UI);
	        ChaosUI.Show(player, root);
        }

        #region Menus
        private void CreateTitleBar(UIUser uiUser, BaseContainer parent)
        {
	        ImageContainer.Create(parent, Anchor.TopStretch, new Offset(5f, -35f, -5f, -5f))
		        .WithStyle(m_PanelStyle)
		        .WithChildren(titleBar =>
		        {
			        TextContainer.Create(titleBar, Anchor.CenterLeft, new Offset(10f, -15f, 205f, 15f))
				        .WithSize(18)
				        .WithText($"{Title} v{Version}")
				        .WithAlignment(TextAnchor.MiddleLeft)
				        .WithOutline(m_OutlineWhite);

			        // Category Buttons
			        BaseContainer.Create(titleBar, Anchor.FullStretch, Offset.zero)
				        .WithLayoutGroup(m_CategoryLayout, m_MenuTypes, 0, (int i, MenuType menuType, BaseContainer buttons, Anchor anchor, Offset offset) =>
				        {
					        if (!HasPermissionForMenuType(uiUser.Player, menuType))
						        return;

					        BaseContainer menuButton = ImageContainer.Create(buttons, anchor, offset)
						        .WithStyle(m_ButtonStyle)
						        .WithChildren(template =>
						        {
							        TextContainer.Create(template, Anchor.FullStretch, Offset.zero)
								        .WithText(GetString($"Category.{menuType}", uiUser.Player))
								        .WithAlignment(TextAnchor.MiddleCenter);

							        ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
								        .WithColor(Color.Clear)
								        .WithCallback(m_CallbackHandler, arg =>
								        {
									        uiUser.MenuIndex = menuType;
									        uiUser.SubMenuIndex = 0;
									        uiUser.Reset();
									        
									        CreateAdminMenu(uiUser.Player);
								        }, $"{uiUser.Player.UserIDString}.{(int)menuType}");

						        });

					        if (menuType == uiUser.MenuIndex)
						        menuButton.WithOutline(m_OutlineGreen);
				        });

			        // Exit Button
			        ImageContainer.Create(titleBar, Anchor.CenterRight, new Offset(-55f, -10f, -5f, 10f))
				        .WithStyle(m_ButtonStyle)
				        .WithOutline(m_OutlineRed)
				        .WithChildren(exit =>
				        {
					        TextContainer.Create(exit, Anchor.FullStretch, Offset.zero)
						        .WithText(GetString("Button.Exit", uiUser.Player))
						        .WithAlignment(TextAnchor.MiddleCenter);

					        ButtonContainer.Create(exit, Anchor.FullStretch, Offset.zero)
						        .WithColor(Color.Clear)
						        .WithCallback(m_CallbackHandler, arg =>
						        {
							        ChaosUI.Destroy(uiUser.Player, ADMINMENU_MOUSE);
							        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
							        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI_POPUP);
							        m_UIUsers.Remove(uiUser.Player.userID);
						        }, $"{uiUser.Player.UserIDString}.exit");

				        });
		        });
        }

        private void CreateSubMenu(UIUser uiUser, BaseContainer parent)
        {
	        int[] subTypes = uiUser.MenuIndex == MenuType.Commands ? m_CommandSubTypes : 
							 uiUser.MenuIndex == MenuType.Permissions ? m_PermissionSubTypes : 
					         uiUser.MenuIndex == MenuType.Groups ? m_GroupSubTypes :
					         uiUser.MenuIndex == MenuType.Give && (uiUser.CommandTarget1 != null || uiUser.Player.HasPermission(GIVE_SELF_PERMISSION)) ? m_ItemCategoryTypes : 
					         Array.Empty<int>();

	        m_SubLayoutGroup.FixedCount = new Vector2Int(subTypes.Length, 0);
	        
	        ImageContainer.Create(parent, Anchor.Center, new Offset(-535f, 245f, 535f, 270f))
		        .WithStyle(m_PanelStyle)
		        .WithLayoutGroup(m_SubLayoutGroup, subTypes, 0, (int i, int t, BaseContainer subMenu, Anchor anchor, Offset offset) =>
		        {
			        if (!HasPermissionForSubMenu(uiUser.Player, uiUser.MenuIndex, t))
				        return;
			        
			        BaseContainer subMenuButton = ImageContainer.Create(subMenu, anchor, offset)
				        .WithStyle(m_ButtonStyle)
				        .WithChildren(commands =>
				        {
					        TextContainer.Create(commands, Anchor.FullStretch, Offset.zero)
						        .WithText(GetString($"{uiUser.MenuIndex}.{t}", uiUser.Player))
						        .WithSize(12)
						        .WithAlignment(TextAnchor.MiddleCenter);

					        ButtonContainer.Create(commands, Anchor.FullStretch, Offset.zero)
						        .WithColor(Color.Clear)
						        .WithCallback(m_CallbackHandler, arg =>
						        {
							        if (uiUser.MenuIndex != MenuType.Give)
										uiUser.Reset();
							        else
							        {
								        uiUser.SearchFilter = string.Empty;
								        uiUser.CharacterFilter = m_CharacterFilter[0];
								        uiUser.Page = 0;
							        }
							        
							        uiUser.SubMenuIndex = i;
							        CreateAdminMenu(uiUser.Player);
						        }, $"{uiUser.Player.UserIDString}.{(int)uiUser.MenuIndex}.{i}");

				        });

			        if (i == uiUser.SubMenuIndex)
				        subMenuButton.WithOutline(m_OutlineGreen);
		        });
        }

        private BaseContainer CreateSelectionHeader(UIUser uiUser, BaseContainer parent, string label, bool pageUp, bool pageDown, bool showPlayerToggles)
        {
	        // Header Bar
	        return ImageContainer.Create(parent, Anchor.TopStretch, new Offset(0f, -25f, 0f, 0f))
		        .WithStyle(m_PanelStyle)
		        .WithChildren(header =>
		        {

			        // Previous Page
			        ImageContainer.Create(header, Anchor.CenterLeft, new Offset(5f, -10f, 35f, 10f))
				        .WithStyle(pageDown ? m_ButtonStyle : m_DisabledButtonStyle)
				        .WithChildren(backButton =>
				        {
					        TextContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
						        .WithText("<<<")
						        .WithStyle(pageDown ? m_ButtonStyle : m_DisabledButtonStyle);

					        if (pageDown)
					        {
						        ButtonContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        uiUser.Page--;
								        CreateAdminMenu(uiUser.Player);
							        }, $"{uiUser.Player.UserIDString}.back");
					        }
				        });



			        // Next Page
			        ImageContainer.Create(header, Anchor.CenterRight, new Offset(-35f, -10f, -5f, 10f))
				        .WithStyle(pageUp ? m_ButtonStyle : m_DisabledButtonStyle)
				        .WithChildren(nextButton =>
				        {
					        TextContainer.Create(nextButton, Anchor.FullStretch, Offset.zero)
						        .WithText(">>>")
						        .WithStyle(pageUp ? m_ButtonStyle : m_DisabledButtonStyle);

					        if (pageUp)
					        {
						        ButtonContainer.Create(nextButton, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        uiUser.Page++;
								        CreateAdminMenu(uiUser.Player);
							        }, $"{uiUser.Player.UserIDString}.next");
					        }
				        });


			        // Search Input
			        ImageContainer.Create(header, Anchor.CenterRight, new Offset(-240f, -10f, -40f, 10f))
				        .WithStyle(m_ButtonStyle)
				        .WithChildren(searchInput =>
				        {
					        InputFieldContainer.Create(searchInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
						        .WithText(uiUser.SearchFilter)
						        .WithAlignment(TextAnchor.MiddleLeft)
						        .WithCallback(m_CallbackHandler, arg =>
						        {
							        uiUser.SearchFilter = arg.Args.Length > 1 ? string.Join(" ", arg.Args.Skip(1)) : string.Empty;
							        uiUser.Page = 0;
							        CreateAdminMenu(uiUser.Player);
						        }, $"{uiUser.Player.UserIDString}.searchinput");
				        });

			        if (!string.IsNullOrEmpty(m_MagnifyImage))
			        {
				        RawImageContainer.Create(header, Anchor.Center, new Offset(275f, -10f, 295f, 10f))
					        .WithPNG(m_MagnifyImage);
			        }

			        // Label
			        TextContainer.Create(header, Anchor.Center, new Offset(-200f, -12.5f, 200f, 12.5f))
				        .WithText(label)
				        .WithAlignment(TextAnchor.MiddleCenter);

			        if (showPlayerToggles)
			        {
				        // Online player toggle
				        ImageContainer.Create(header, Anchor.CenterLeft, new Offset(40f, -10f, 60f, 10f))
					        .WithStyle(m_ButtonStyle)
					        .WithChildren(toggle =>
					        {
						        if (uiUser.ShowOnlinePlayers)
						        {
							        TextContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
								        .WithText("•")
								        .WithStyle(m_ToggleLabelStyle);
						        }

						        ButtonContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        uiUser.ShowOnlinePlayers = !uiUser.ShowOnlinePlayers;
								        CreateAdminMenu(uiUser.Player);
							        });

						        TextContainer.Create(toggle, Anchor.CenterLeft, new Offset(25f, -10f, 125f, 10f))
							        .WithText(GetString("Label.OnlinePlayers", uiUser.Player))
							        .WithAlignment(TextAnchor.MiddleLeft);
					        });

				        // Offline player toggle
				        ImageContainer.Create(header, Anchor.CenterLeft, new Offset(155f, -10f, 175f, 10f))
					        .WithStyle(m_ButtonStyle)
					        .WithChildren(toggle =>
					        {
						        if (uiUser.ShowOfflinePlayers)
						        {
							        TextContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
								        .WithText("•")
								        .WithStyle(m_ToggleLabelStyle);
						        }

						        ButtonContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        uiUser.ShowOfflinePlayers = !uiUser.ShowOfflinePlayers;
								        CreateAdminMenu(uiUser.Player);
							        });

						        TextContainer.Create(toggle, Anchor.CenterLeft, new Offset(25f, -10f, 125f, 10f))
							        .WithText(GetString("Label.OfflinePlayers", uiUser.Player))
							        .WithAlignment(TextAnchor.MiddleLeft);

					        });
			        }
		        });
        }

        #endregion

        #region Commands
        private void CreateCommandMenu(UIUser uiUser, BaseContainer parent)
        {
	        if (uiUser.CommandEntry != null)
	        {
		        if (uiUser.RequireTarget1 && uiUser.CommandTarget1 == null)
		        {
			        List<IPlayer> dst = Facepunch.Pool.GetList<IPlayer>();

			        GetApplicablePlayers(uiUser, dst);
	        
			        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectPlayer1", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

			        CreateCharacterFilter(uiUser, parent);
			        
			        LayoutSelectionGrid(uiUser, parent,  dst, (s) => s.Name.StripTags(), iPlayer =>
			        {
				        uiUser.CommandTarget1 = iPlayer;
				        
				        if (!uiUser.RequireTarget2)
					        RunCommand(uiUser, uiUser.CommandEntry, uiUser.SubMenuIndex == 0);
			        });
	        
			        Facepunch.Pool.FreeList(ref dst);
			        return;
		        }
		        
		        if (uiUser.RequireTarget2 && uiUser.CommandTarget2 == null)
		        {
			        List<IPlayer> dst = Facepunch.Pool.GetList<IPlayer>();

			        GetApplicablePlayers(uiUser, dst);
	        
			        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectPlayer2", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

			        CreateCharacterFilter(uiUser, parent);
			        
			        LayoutSelectionGrid(uiUser, parent,  dst, (s) => s.Name.StripTags(), iPlayer =>
			        {
				        uiUser.CommandTarget2 = iPlayer;
				        RunCommand(uiUser, uiUser.CommandEntry, uiUser.SubMenuIndex == 0);
			        });
	        
			        Facepunch.Pool.FreeList(ref dst);
			        return;
		        }
	        }
	        else
	        {
		        List<ConfigData.CommandEntry> commands = Facepunch.Pool.GetList<ConfigData.CommandEntry>();
		        commands.AddRange(uiUser.SubMenuIndex == 0 ? Configuration.ChatCommands : Configuration.ConsoleCommands);

		        for (int i = commands.Count - 1; i >= 0; i--)
		        {
			        ConfigData.CommandEntry command = commands[i];
			        if (!string.IsNullOrEmpty(command.RequiredPermission) && !uiUser.Player.HasPermission(command.RequiredPermission))
				        commands.RemoveAt(i);
		        }

		        ImageContainer.Create(parent, Anchor.FullStretch, Offset.zero)
			        .WithStyle(m_PanelStyle)
			        .WithLayoutGroup(m_CommandLayoutGroup, commands, 0, (int i, ConfigData.CommandEntry t, BaseContainer commandList, Anchor anchor, Offset offset) =>
			        {
				        ImageContainer.Create(commandList, anchor, offset)
					        .WithStyle(m_ButtonStyle)
					        .WithChildren(commandTemplate =>
					        {
						        TextContainer.Create(commandTemplate, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
							        .WithText(t.Name)
							        .WithAlignment(TextAnchor.UpperCenter);

						        TextContainer.Create(commandTemplate, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
							        .WithSize(10)
							        .WithText(t.Description)
							        .WithAlignment(TextAnchor.LowerCenter);

						        ButtonContainer.Create(commandTemplate, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        uiUser.CommandEntry = t;
								        uiUser.RequireTarget1 = t.Command.Contains("{target1_name}") || t.Command.Contains("{target1_id}");
								        uiUser.RequireTarget2 = t.Command.Contains("{target2_name}") || t.Command.Contains("{target2_id}");

								        if (uiUser.RequireTarget1 || uiUser.RequireTarget2)
									        CreateAdminMenu(uiUser.Player);
								        else RunCommand(uiUser, t, uiUser.SubMenuIndex == 0);

							        }, $"{uiUser.Player.UserIDString}.command.{i}");

					        });
			        });
	        }
        }
       
        private void CreateGiveMenu(UIUser uiUser, BaseContainer parent)
        {
	        RESTART:
	        if (uiUser.CommandTarget1 == null)
	        {
		        if (uiUser.Player.HasPermission(GIVE_SELF_PERMISSION))
		        {
			        uiUser.CommandTarget1 = uiUser.Player.IPlayer;
			        goto RESTART;
		        }
		        
		        List<IPlayer> dst = Facepunch.Pool.GetList<IPlayer>();

		        GetApplicablePlayers(uiUser, dst);
		        
		        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectPlayer", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

		        CreateCharacterFilter(uiUser, parent);
		        
		        LayoutSelectionGrid(uiUser, parent,  dst, (s) => s.Name.StripTags(), iPlayer =>
		        {
			        uiUser.CommandTarget1 = iPlayer;
			        CreateAdminMenu(uiUser.Player);
		        });
		        
		        Facepunch.Pool.FreeList(ref dst);
	        }
	        else
	        {
		        List<ItemDefinition> dst = Facepunch.Pool.GetList<ItemDefinition>();

		        if (uiUser.CharacterFilter != m_CharacterFilter[0] || !string.IsNullOrEmpty(uiUser.SearchFilter))
		        {
			        List<ItemDefinition> src = Facepunch.Pool.GetList<ItemDefinition>();

			        src.AddRange(m_AllItemDefinitions);
			        
			        FilterList(src, dst, uiUser, (s, itemDefinition) => StartsWithValidator(s, itemDefinition.displayName.english), (s, itemDefinition) => ContainsValidator(s, itemDefinition.displayName.english));
			        
			        Facepunch.Pool.FreeList(ref src);
		        }
		        else dst.AddRange(m_ItemDefinitionsPerCategory[(ItemCategory)m_ItemCategoryTypes[uiUser.SubMenuIndex]]);
		        
		        CreateSelectionHeader(uiUser, parent, FormatString("Label.GiveToPlayer", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()), m_GiveLayoutGroup.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);

		        CreateCharacterFilter(uiUser, parent);
		        
		        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
			        .WithStyle(m_PanelStyle)
			        .WithLayoutGroup(m_GiveLayoutGroup, dst, uiUser.Page, (int i, ItemDefinition t, BaseContainer layout, Anchor anchor, Offset offset) =>
			        {
				        ImageContainer.Create(layout, anchor, offset)
					        .WithStyle(m_PanelStyle)
					        .WithChildren(template =>
					        {
						        ImageContainer.Create(template, Anchor.TopCenter, new Offset(-37.5f, -75f, 37.5f, 0f))
							        .WithIcon(t.itemid);

						        TextContainer.Create(template, Anchor.BottomStretch, new Offset(0f, 0f, 0f, 25f))
							        .WithSize(10)
							        .WithText(t.displayName.english)
							        .WithAlignment(TextAnchor.MiddleCenter);

						        ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        BasePlayer target = uiUser.CommandTarget1.Object as BasePlayer;
								        if (!target)
								        {
									        CreatePopupMessage(uiUser, "The selected user is not valid at this time. They may be dead or disconnected");
									        return;
								        }
										        
								        CreateGiveOverlay(uiUser, target, t);
							        }, $"{uiUser.Player.UserIDString}.{t.shortname}" );

						        Action<int> quickGiveAction = new Action<int>((int amount) =>
						        {
							        BasePlayer target = uiUser.CommandTarget1.Object as BasePlayer;
							        if (!target)
							        {
								        CreatePopupMessage(uiUser, "The selected user is not valid at this time. They may be dead or disconnected");
								        return;
							        }

							        target.GiveItem(ItemManager.Create(t, amount), BaseEntity.GiveItemReason.PickedUp);
							        CreatePopupMessage(uiUser, FormatString("Notification.Give.Success", uiUser.Player, target.displayName, amount, t.displayName.english));
						        });
						        
						        ImageContainer.Create(template, Anchor.TopRight, new Offset(-23f, -18f, -3f, -3f))
							        .WithStyle(m_ButtonStyle)
							        .WithChildren(giveOne =>
							        {
								        TextContainer.Create(giveOne, Anchor.FullStretch, Offset.zero)
									        .WithSize(8)
									        .WithText("1")
									        .WithAlignment(TextAnchor.MiddleCenter)
									        .WithWrapMode(VerticalWrapMode.Overflow);

								        ButtonContainer.Create(giveOne, Anchor.FullStretch, Offset.zero)
									        .WithColor(Color.Clear)
									        .WithCallback(m_CallbackHandler, arg => quickGiveAction(1), $"{uiUser.Player.UserIDString}.quick.{t.shortname}.1");
							        });

						        ImageContainer.Create(template, Anchor.TopRight, new Offset(-23f, -35f, -3f, -20f))
							        .WithStyle(m_ButtonStyle)
							        .WithChildren(giveOneHundred =>
							        {
								        TextContainer.Create(giveOneHundred, Anchor.FullStretch, Offset.zero)
									        .WithSize(8)
									        .WithText("100")
									        .WithAlignment(TextAnchor.MiddleCenter)
									        .WithWrapMode(VerticalWrapMode.Overflow);

								        ButtonContainer.Create(giveOneHundred, Anchor.FullStretch, Offset.zero)
									        .WithColor(Color.Clear)
									        .WithCallback(m_CallbackHandler, arg => quickGiveAction(100), $"{uiUser.Player.UserIDString}.quick.{t.shortname}.100");
							        });

						        ImageContainer.Create(template, Anchor.TopRight, new Offset(-23f, -52f, -3f, -37f))
							        .WithStyle(m_ButtonStyle)
							        .WithChildren(giveOneThousand =>
							        {
								        TextContainer.Create(giveOneThousand, Anchor.FullStretch, Offset.zero)
									        .WithSize(8)
									        .WithText("1000")
									        .WithAlignment(TextAnchor.MiddleCenter)
									        .WithWrapMode(VerticalWrapMode.Overflow);

								        ButtonContainer.Create(giveOneThousand, Anchor.FullStretch, Offset.zero)
									        .WithColor(Color.Clear)
									        .WithCallback(m_CallbackHandler, arg => quickGiveAction(1000), $"{uiUser.Player.UserIDString}.quick.{t.shortname}.1000");

							        });
					        });
			        });
		        
		        Facepunch.Pool.FreeList(ref dst);
	        }
        }

        private void CreateGiveOverlay(UIUser uiUser, BasePlayer target, ItemDefinition itemDefinition)
        {
	        BaseContainer baseContainer = ButtonContainer.Create(ADMINMENU_UI, Layer.Overall, Anchor.FullStretch, Offset.zero)
				.WithStyle(m_BackgroundStyle)
				.WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.give.cancel")
				.WithChildren(givePopup =>
				{
					ImageContainer.Create(givePopup, Anchor.Center, new Offset(-100f, 107.5f, 100f, 127.5f))
						.WithStyle(m_PanelStyle)
						.WithChildren(infoBar =>
						{
							TextContainer.Create(infoBar, Anchor.FullStretch, Offset.zero)
								.WithText(itemDefinition.displayName.english)
								.WithAlignment(TextAnchor.MiddleCenter);
						});
					
					ImageContainer.Create(givePopup, Anchor.Center, new Offset(-100f, -102.5f, 100f, 102.5f))
						.WithStyle(m_PanelStyle)
						.WithChildren(givePanel =>
						{
							// Item Icon
							ImageContainer.Create(givePanel, Anchor.TopCenter, new Offset(-64f, -128f, 64f, 0f))
								.WithIcon(itemDefinition.itemid);

							// Amount Input
							TextContainer.Create(givePanel, Anchor.BottomStretch, new Offset(4.999969f, 55f, -145f, 75f))
								.WithText(GetString("Label.Amount", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(givePanel, Anchor.BottomStretch, new Offset(60f, 55f, -4.999985f, 75f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(amountInput =>
								{
									InputFieldContainer.Create(amountInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.GiveAmount.ToString())
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GiveAmount = arg.GetInt(1);
											CreateGiveOverlay(uiUser, target, itemDefinition);
										});
								});
							
							// Skin Input
							TextContainer.Create(givePanel, Anchor.BottomStretch, new Offset(4.999969f, 30f, -145f, 50f))
								.WithText(GetString("Label.SkinID", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);
							
							ImageContainer.Create(givePanel, Anchor.BottomStretch, new Offset(60f, 30f, -4.999985f, 50f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(skinId =>
								{
									InputFieldContainer.Create(skinId, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.SkinID.ToString())
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.SkinID = arg.GetUInt64(1);
											CreateGiveOverlay(uiUser, target, itemDefinition);
										});
								});
							
							// Buttons
							ImageContainer.Create(givePanel, Anchor.BottomLeft, new Offset(5f, 5f, 95f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineGreen)
								.WithChildren(giveButton =>
								{
									TextContainer.Create(giveButton, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Give", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(giveButton, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg =>
										{
											if (uiUser.GiveAmount == 0)
												return;

											target.GiveItem(ItemManager.Create(itemDefinition, uiUser.GiveAmount, uiUser.SkinID), BaseEntity.GiveItemReason.PickedUp);
											
											CreateAdminMenu(uiUser.Player);
											CreatePopupMessage(uiUser, FormatString("Notification.Give.Success", uiUser.Player, target.displayName, uiUser.GiveAmount, itemDefinition.displayName.english));
										}, $"{uiUser.Player.UserIDString}.give");

								});

							ImageContainer.Create(givePanel, Anchor.BottomRight, new Offset(-95f, 5f, -5f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineRed)
								.WithChildren(cancel =>
								{
									TextContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Cancel", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.give.cancel");

								});
						});
				});
				//.NeedsCursor()
				//.NeedsKeyboard();
	        
	        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
	        ChaosUI.Show(uiUser.Player, baseContainer);
        }

        private void CreatePlayerMenu(UIUser uiUser, BaseContainer parent)
        {
	        if (uiUser.CommandTarget1 == null)
	        {
		        List<IPlayer> dst = Facepunch.Pool.GetList<IPlayer>();

		        GetApplicablePlayers(uiUser, dst);
		        
		        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectPlayer", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

		        CreateCharacterFilter(uiUser, parent);
		        
		        LayoutSelectionGrid(uiUser, parent,  dst, (s) => s.Name.StripTags(), iPlayer =>
		        {
			        uiUser.CommandTarget1 = iPlayer;
			        CreateAdminMenu(uiUser.Player);
		        });
		        
		        Facepunch.Pool.FreeList(ref dst);
	        }
	        else
	        {
		        // Headers
		        BaseContainer.Create(parent, Anchor.TopStretch, new Offset(0f, -25f, 0f, 0f))
			        .WithChildren(headers =>
			        {
				        ImageContainer.Create(headers, Anchor.LeftStretch, new Offset(0f, 0f, 200f, 0f))
					        .WithStyle(m_PanelStyle)
					        .WithChildren(statsheader =>
					        {
						        TextContainer.Create(statsheader, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
							        .WithText(GetString("PlayerInfo.Info", uiUser.Player))
							        .WithAlignment(TextAnchor.MiddleLeft);
					        });

				        ImageContainer.Create(headers, Anchor.LeftStretch, new Offset(205f, 0f, 1070f, 0f))
					        .WithStyle(m_PanelStyle)
					        .WithChildren(actionsHeader =>
					        {
						        TextContainer.Create(actionsHeader, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
							        .WithText(GetString("PlayerInfo.Actions", uiUser.Player))
							        .WithAlignment(TextAnchor.MiddleLeft);
					        });
			        });


		        // Player Information
		        ImageContainer.Create(parent, Anchor.LeftStretch, new Offset(0f, 0f, 200f, -30f))
			        .WithStyle(m_PanelStyle)
			        .WithLayoutGroup(m_PlayerInfoLayout, m_PlayerInfo, 0, (int i, PlayerInfo t, BaseContainer stats, Anchor anchor, Offset offset) =>
			        {
				        if (string.IsNullOrEmpty(t.Name))
					        return;

				        TextContainer.Create(stats, anchor, offset)
					        .WithText(FormatString(t.Name, uiUser.Player, t.Result(uiUser.CommandTarget1)))
					        .WithAlignment(TextAnchor.MiddleLeft)
					        .WithSize(12);
			        });

		        // Plugin Actions
		        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(205f, 0f, 0f, -30f))
			        .WithStyle(m_PanelStyle)
			        .WithLayoutGroup(m_PluginActionsLayout, m_PluginActions, 0, (int i, List<PluginAction> list, BaseContainer actions, Anchor anchor, Offset offset) =>
			        {
				        if (list == null)
					        return;

				        BaseContainer.Create(actions, anchor, offset)
					        .WithLayoutGroup(m_InternalPluginActionsLayout, list, 0, (int ii, PluginAction t, BaseContainer innerGrid, Anchor anchor2, Offset offset2) =>
					        {
						        if (string.IsNullOrEmpty(t.Name) || !t.IsViewable())
							        return;
						        
						        if ((Configuration.UsePlayerAdminPermissions || t.ForcePermissionCheck) && !t.HasPermission(uiUser))
							        return;

						        ImageContainer.Create(innerGrid, anchor2, offset2)
							        .WithStyle(m_ButtonStyle)
							        .WithChildren(template =>
							        {
								        TextContainer.Create(template, Anchor.FullStretch, Offset.zero)
									        .WithText(GetString(t.Name, uiUser.Player))
									        .WithAlignment(TextAnchor.MiddleCenter)
									        .WithSize(12);

								        ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
									        .WithColor(Color.Clear)
									        .WithCallback(m_CallbackHandler, arg => t.OnClick(uiUser), $"{uiUser.Player.UserIDString}.pluginaction.{t.Name}");
							        });
					        });
			        });
	        }
        }

        private void CreateKickBanOverlay(UIUser uiUser, IPlayer target, bool isKick)
        {
	        BaseContainer baseContainer = ButtonContainer.Create(ADMINMENU_UI, Layer.Overall, Anchor.FullStretch, Offset.zero)
				.WithStyle(m_BackgroundStyle)
				.WithChildren(kickBanPopup =>
				{
					ImageContainer.Create(kickBanPopup, Anchor.Center, new Offset(-175f, 32.5f, 175f, 52.5f))
						.WithStyle(m_PanelStyle)
						.WithChildren(infoBar =>
						{
							TextContainer.Create(infoBar, Anchor.FullStretch, Offset.zero)
								.WithText(FormatString(isKick ? "Label.Kick" : "Label.Ban", uiUser.Player, target.Name.StripTags()))
								.WithAlignment(TextAnchor.MiddleCenter);

						});

					ImageContainer.Create(kickBanPopup, Anchor.Center, new Offset(-175f, -27.5f, 175f, 27.5f))
						.WithStyle(m_PanelStyle)
						.WithChildren(titleBar =>
						{
							//Reason Input
							TextContainer.Create(titleBar, Anchor.BottomStretch, new Offset(4.999969f, 30f, -145f, 50f))
								.WithText(GetString("Label.Reason", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);
							
							ImageContainer.Create(titleBar, Anchor.BottomStretch, new Offset(60f, 30f, -4.999985f, 50f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(reasonInput =>
								{
									InputFieldContainer.Create(reasonInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(isKick ? "Kicked by Administrator" : "Banned by Administrator")
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.KickBanReason = arg.Args.Length > 1 ? string.Join(" ", arg.Args.Skip(1)) : (isKick ? "Kicked by Administrator" : "Banned by Administrator");
											CreateKickBanOverlay(uiUser, target, isKick);
										}, $"{uiUser.Player.UserIDString}.kickban.reason");
								});
							
							// Buttons
							ImageContainer.Create(titleBar, Anchor.BottomLeft, new Offset(5f, 5f, 95f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineGreen)
								.WithChildren(confirm =>
								{
									TextContainer.Create(confirm, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Confirm", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(confirm, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg =>
										{
											if (isKick)
											{
												string reason = string.IsNullOrEmpty(uiUser.KickBanReason) ? "Kicked by Administrator" : uiUser.KickBanReason;
												
												ConVar.Chat.Broadcast($"Kicked {target.Name} ({reason})", "SERVER", "#eee", (ulong)0);
												
												BasePlayer targetPlayer = target.Object as BasePlayer;
												if (targetPlayer)
													Network.Net.sv.Kick(targetPlayer.net.connection, reason);
											}
											else
											{
												string reason = string.IsNullOrEmpty(uiUser.KickBanReason) ? "Banned by Administrator" : uiUser.KickBanReason;

												ConVar.Chat.Broadcast($"Banned {target.Name} ({reason})", "SERVER", "#eee", (ulong)0);
												target.Ban(reason);
											}
											CreateAdminMenu(uiUser.Player);
										}, $"{uiUser.Player.UserIDString}.kickban.confirm");

								});

							ImageContainer.Create(titleBar, Anchor.BottomRight, new Offset(-95f, 5f, -5f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineRed)
								.WithChildren(cancel =>
								{
									TextContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Cancel", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.kickban.cancel");
								});
						});
				});
				//.NeedsCursor()
				//.NeedsKeyboard();
	        
	        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
	        ChaosUI.Show(uiUser.Player, baseContainer);
        }
        
        #region Player Info Functions
	    private struct PluginAction
	    {
		    public readonly string Name;
		    public readonly bool ForcePermissionCheck;
		    public readonly Func<bool> IsViewable;
		    public readonly Func<UIUser, bool> HasPermission;
		    public readonly Action<UIUser> OnClick;

		    public PluginAction(string name, Func<UIUser, bool> hasPermission, Action<UIUser> onClick, bool forcePermissionCheck = false)
		    {
			    this.Name = name;
			    this.IsViewable = () => true;
			    this.HasPermission = hasPermission;
			    this.OnClick = onClick;
			    this.ForcePermissionCheck = forcePermissionCheck;
		    }
		    
		    public PluginAction(string name, Func<bool> isViewable, Func<UIUser, bool> hasPermission, Action<UIUser> onClick, bool forcePermissionCheck = false)
		    {
			    this.Name = name;
			    this.IsViewable = isViewable;
			    this.HasPermission = hasPermission;
			    this.OnClick = onClick;
			    this.ForcePermissionCheck = forcePermissionCheck;
		    }
	    }

	    private struct PlayerInfo
	    {
		    public readonly string Name;
		    public readonly Func<IPlayer, string> Result;

		    public PlayerInfo(string name, Func<IPlayer, string> result)
		    {
			    this.Name = name;
			    this.Result = result;
		    }
	    }
        
	    private List<List<PluginAction>> m_PluginActions;
	    
        private readonly List<PlayerInfo> m_PlayerInfo = new List<PlayerInfo>()
        {
	        new PlayerInfo("PlayerInfo.Name", (player => player.Name.StripTags())),
	        new PlayerInfo("PlayerInfo.ID", (player => player.Id)),
	        new PlayerInfo("PlayerInfo.Auth", (player =>
	        {
		        ulong userId = ulong.Parse(player.Id);
		        return (DeveloperList.Contains(userId) ? "Developer" : (ServerUsers.Get(userId)?.group ?? ServerUsers.UserGroup.None).ToString());
	        })),
	        new PlayerInfo("PlayerInfo.Status", (player => player.IsConnected ? "Online" : "Offline")),
	        new PlayerInfo("PlayerInfo.IdleTime", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? FormatTime(basePlayer.IdleTime) : string.Empty;
	        })),
	        new PlayerInfo(string.Empty, null),
	        new PlayerInfo("PlayerInfo.Position", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? basePlayer.ServerPosition.ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Grid", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? PhoneController.PositionToGridCoord(basePlayer.ServerPosition) : string.Empty;
	        })),
	        new PlayerInfo(string.Empty, null),
	        new PlayerInfo("PlayerInfo.Health", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.health, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Calories", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.calories.value, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Hydration", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.hydration.value, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Temperature", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.temperature.value, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Comfort", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.comfort.value, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Wetness", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.wetness.value, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Bleeding", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.bleeding.value, 2).ToString() : string.Empty;
	        })),
	        new PlayerInfo("PlayerInfo.Radiation", (player =>
	        {
		        BasePlayer basePlayer = (player.Object as BasePlayer);
		        return basePlayer ? Math.Round(basePlayer.metabolism.radiation_level.value, 2).ToString() : string.Empty;
	        })),
        };

        private void SetupPlayerActions()
        {
	        m_PluginActions = new List<List<PluginAction>>()
	        {
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.Kick", (s) => s.Player.HasPermission(PLAYER_KICKBAN_PERMISSION), uiUser => CreateKickBanOverlay(uiUser, uiUser.CommandTarget1, true)),
			        new PluginAction("Action.Ban", (s) => s.Player.HasPermission(PLAYER_KICKBAN_PERMISSION), (uiUser) => CreateKickBanOverlay(uiUser, uiUser.CommandTarget1, false))
		        },
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.StripInventory", (s) => s.Player.HasPermission(PLAYER_STRIP_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.inventory.Strip();
					        CreatePopupMessage(uiUser, FormatString("Action.StripInventory.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.StripInventory.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.ResetMetabolism", (s) => s.Player.HasPermission(PLAYER_HEAL_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.metabolism.bleeding.value = 0;
					        player.metabolism.calories.value = player.metabolism.calories.max;
					        player.metabolism.hydration.value = player.metabolism.hydration.max;
					        player.metabolism.radiation_level.value = 0;
					        player.metabolism.radiation_poison.value = 0;
					        player.metabolism.poison.value = 0;
					        player.metabolism.wetness.value = 0;

					        player.metabolism.SendChangesToClient();
					        CreatePopupMessage(uiUser, FormatString("Action.ResetMetabolism.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        CreateAdminMenu(uiUser.Player);
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.ResetMetabolism.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
		        },
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.GiveBlueprints", (s) => s.Player.HasPermission(PLAYER_BLUERPRINTS_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        ProtoBuf.PersistantPlayer persistantPlayerInfo = player.PersistantPlayerInfo;
					        foreach (ItemBlueprint itemBlueprint in ItemManager.bpList)
					        {
						        if (!itemBlueprint.userCraftable || itemBlueprint.defaultBlueprint || persistantPlayerInfo.unlockedItems.Contains(itemBlueprint.targetItem.itemid))
						        {
							        continue;
						        }

						        persistantPlayerInfo.unlockedItems.Add(itemBlueprint.targetItem.itemid);
					        }

					        player.PersistantPlayerInfo = persistantPlayerInfo;
					        player.SendNetworkUpdateImmediate(false);
					        player.ClientRPCPlayer<int>(null, player, "UnlockedBlueprint", 0);
					        CreatePopupMessage(uiUser, FormatString("Action.GiveBlueprints.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.GiveBlueprints.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.RevokeBlueprints", (s) => s.Player.HasPermission(PLAYER_BLUERPRINTS_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.blueprints.Reset();
					        CreatePopupMessage(uiUser, FormatString("Action.RevokeBlueprints.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.RevokeBlueprints.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        default(PluginAction),
			        new PluginAction("Action.ViewPermissions", (s) => s.Player.HasPermission(PERM_PERMISSION), uiUser =>
			        {
				        uiUser.MenuIndex = MenuType.Permissions;
				        uiUser.SubMenuIndex = (int) PermissionSubType.Player;
				        uiUser.PermissionTarget = uiUser.CommandTarget1.Id;
				        uiUser.PermissionTargetName = uiUser.CommandTarget1.Name.StripTags();
				        uiUser.CommandTarget1 = null;
				        CreateAdminMenu(uiUser.Player);
			        }, true),
		        },
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.Mute", (s) => s.Player.HasPermission(PLAYER_MUTE_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
					        CreatePopupMessage(uiUser, FormatString("Action.Mute.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Mute.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Unmute", (s) => s.Player.HasPermission(PLAYER_MUTE_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
					        CreatePopupMessage(uiUser, FormatString("Action.Unmute.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Unmute.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
		        },
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.Hurt25", (s) => s.Player.HasPermission(PLAYER_HURT_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.Hurt(player.health * 0.25f);
					        CreatePopupMessage(uiUser, FormatString("Action.Hurt25.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Hurt25.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Hurt50", (s) => s.Player.HasPermission(PLAYER_HURT_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.Hurt(player.health * 0.5f);
					        CreatePopupMessage(uiUser, FormatString("Action.Hurt50.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Hurt50.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Hurt75", (s) => s.Player.HasPermission(PLAYER_HURT_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.Hurt(player.health * 0.75f);
					        CreatePopupMessage(uiUser, FormatString("Action.Hurt75.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Hurt75.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Kill", (s) => s.Player.HasPermission(PLAYER_KILL_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.Die(new HitInfo(player, player, Rust.DamageType.Stab, 1000));
					        CreatePopupMessage(uiUser, FormatString("Action.Kill.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Kill.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        })
		        },
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.Heal25", (s) => s.Player.HasPermission(PLAYER_HEAL_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        if (player.IsWounded())
						        player.StopWounded();

					        player.Heal(player.MaxHealth() * 0.25f);
					        CreatePopupMessage(uiUser, FormatString("Action.Heal25.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Heal25.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Heal50", (s) => s.Player.HasPermission(PLAYER_HEAL_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        if (player.IsWounded())
						        player.StopWounded();

					        player.Heal(player.MaxHealth() * 0.5f);
					        CreatePopupMessage(uiUser, FormatString("Action.Heal50.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Heal50.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Heal75", (s) => s.Player.HasPermission(PLAYER_HEAL_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        if (player.IsWounded())
						        player.StopWounded();

					        player.Heal(player.MaxHealth() * 0.75f);
					        CreatePopupMessage(uiUser, FormatString("Action.Heal75.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Heal75.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.Heal100", (s) => s.Player.HasPermission(PLAYER_HEAL_PERMISSION), (uiUser) =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        if (player.IsWounded())
						        player.StopWounded();

					        player.Heal(player.MaxHealth());
					        CreatePopupMessage(uiUser, FormatString("Action.Heal100.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.Heal100.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        })
		        },
		        new List<PluginAction>()
		        {
			        new PluginAction("Action.TeleportSelfTo", (s) => s.Player.HasPermission(PLAYER_TELEPORT_PERMISSION), uiUser =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        uiUser.Player.Teleport(player.transform.position);
					        CreatePopupMessage(uiUser, FormatString("Action.TeleportSelfTo.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.TeleportSelfTo.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.TeleportToSelf", (s) => s.Player.HasPermission(PLAYER_TELEPORT_PERMISSION), uiUser =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        player.Teleport(uiUser.Player.transform.position);
					        CreatePopupMessage(uiUser, FormatString("Action.TeleportToSelf.Success", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.TeleportToSelf.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.TeleportAuthedItem", (s) => s.Player.HasPermission(PLAYER_TELEPORT_PERMISSION), uiUser =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        BaseEntity[] entities = BaseEntity.Util.FindTargetsAuthedTo(player.userID, string.Empty);
					        if (entities.Length > 0)
					        {
						        int random = UnityEngine.Random.Range(0, (int) entities.Length);

						        player.Teleport(entities[random].transform.position);
						        CreatePopupMessage(uiUser, FormatString("Action.TeleportAuthedItem.Success", uiUser.Player, entities[random].ShortPrefabName, entities[random].transform.position));
					        }
					        else CreatePopupMessage(uiUser, FormatString("Action.TeleportAuthedItem.Failed.Entities", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));

					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.TeleportAuthedItem.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
			        new PluginAction("Action.TeleportOwnedItem", (s) => s.Player.HasPermission(PLAYER_TELEPORT_PERMISSION), uiUser =>
			        {
				        BasePlayer player = (uiUser.CommandTarget1.Object as BasePlayer);
				        if (player)
				        {
					        BaseEntity[] entities = BaseEntity.Util.FindTargetsOwnedBy(player.userID, string.Empty);
					        if (entities.Length > 0)
					        {
						        int random = UnityEngine.Random.Range(0, (int) entities.Length);

						        player.Teleport(entities[random].transform.position);
						        CreatePopupMessage(uiUser, FormatString("Action.TeleportOwnedItem.Success", uiUser.Player, entities[random].ShortPrefabName, entities[random].transform.position));
					        }
					        else CreatePopupMessage(uiUser, FormatString("Action.TeleportOwnedItem.Failed.Entities", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));

					        return;
				        }

				        CreatePopupMessage(uiUser, FormatString("Action.TeleportOwnedItem.Failed", uiUser.Player, uiUser.CommandTarget1.Name.StripTags()));
			        }),
		        },
		        null
	        };

	        foreach (ConfigData.CustomCommands playerInfoCommand in Configuration.PlayerInfoCommands)
	        {
		        if (playerInfoCommand?.Commands?.Count > 0)
		        {
			        List<PluginAction> customActions = new List<PluginAction>();

			        foreach (ConfigData.CustomCommands.PlayerInfoCommandEntry customCommand in playerInfoCommand.Commands)
			        {
				        customActions.Add(new PluginAction(customCommand.Name, () =>
				        {
					        if (!string.IsNullOrEmpty(customCommand.RequiredPlugin) && !plugins.Exists(customCommand.RequiredPlugin))
						        return false;

					        return true;
				        }, (user =>
				        {
					        if (!string.IsNullOrEmpty(customCommand.RequiredPermission) && !user.Player.HasPermission(customCommand.RequiredPermission))
						        return false;

					        return true;
				        }), user => RunCommand(user, customCommand, customCommand.SubType == CommandSubType.Chat)));
			        }

			        m_PluginActions.Add(customActions);
		        }
	        }

	        m_PlayerInfo.Add(new PlayerInfo(string.Empty, null));
	        m_PlayerInfo.Add(new PlayerInfo("PlayerInfo.Clan", (player =>
	        {
		        if (Clans.IsLoaded)
		        {
			        BasePlayer basePlayer = (player.Object as BasePlayer);
			        if (basePlayer)
				        return Clans.GetClanOf(basePlayer) ?? "None";
		        }

		        return string.Empty;
	        })));
	        m_PlayerInfo.Add(new PlayerInfo("PlayerInfo.Playtime", (player =>
	        {
		        if (PlaytimeTracker.IsLoaded)
		        {
			        BasePlayer basePlayer = (player.Object as BasePlayer);
			        if (basePlayer)
			        {
				        object obj = PlaytimeTracker.GetPlayTime(basePlayer.UserIDString);
				        return FormatTime(obj == null ? 0 : (double) obj);
			        }
		        }

		        return string.Empty;
	        })));
	        m_PlayerInfo.Add(new PlayerInfo("PlayerInfo.AFKTime", (player =>
	        {
		        if (PlaytimeTracker.IsLoaded)
		        {
			        BasePlayer basePlayer = (player.Object as BasePlayer);
			        if (basePlayer)
			        {
				        object obj = PlaytimeTracker.GetAFKTime(basePlayer.UserIDString);
				        return FormatTime(obj == null ? 0 : (double) obj);
			        }
		        }

		        return string.Empty;
	        })));
	        m_PlayerInfo.Add(new PlayerInfo("PlayerInfo.ServerRewards", (player =>
	        {
		        if (ServerRewards.IsLoaded)
		        {
			        BasePlayer basePlayer = (player.Object as BasePlayer);
			        if (basePlayer)
			        {
				        object obj = ServerRewards.CheckPoints(basePlayer.UserIDString);
				        return obj == null ? "0" : obj.ToString();
			        }
		        }

		        return string.Empty;
	        })));
	        m_PlayerInfo.Add(new PlayerInfo("PlayerInfo.Economics", (player =>
	        {
		        if (Economics.IsLoaded)
		        {
			        BasePlayer basePlayer = (player.Object as BasePlayer);
			        if (basePlayer)
			        {
				        return Math.Round(Economics.Balance(basePlayer.userID), 2).ToString();
			        }
		        }

		        return string.Empty;
	        })));
        }

        #endregion

        private void RunCommand(UIUser uiUser, ConfigData.CommandEntry commandEntry, bool chat)
        {
	        string command = commandEntry.Command.Replace("{target1_name}", $"\"{uiUser.CommandTarget1?.Name}\"")
										         .Replace("{target1_id}", uiUser.CommandTarget1?.Id)
										         .Replace("{target2_name}", $"\"{uiUser.CommandTarget2?.Name}\"")
										         .Replace("{target2_id}", uiUser.CommandTarget2?.Id);
	        
	        if (chat)
		        rust.RunClientCommand(uiUser.Player, "chat.say", command);
	        else rust.RunServerCommand(command);

	        uiUser.ClearCommand();
	        
	        CreatePopupMessage(uiUser, FormatString("Notification.RunCommand", uiUser.Player, command.Replace("\"", string.Empty)));
	        
	        if (commandEntry.CloseOnRun)
	        {
		        ChaosUI.Destroy(uiUser.Player, ADMINMENU_MOUSE);
		        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
		        m_UIUsers.Remove(uiUser.Player.userID);
	        }
	        else CreateAdminMenu(uiUser.Player);
        }
        #endregion
        
        #region Usergroups

        private void CreateGroupMenu(UIUser uiUser, BaseContainer parent)
        {
	        List<string> src = Facepunch.Pool.GetList<string>();
	        List<string> dst = Facepunch.Pool.GetList<string>();

	        src.AddRange(permission.GetGroups());

	        FilterList(src, dst, uiUser, ((s, pair) => StartsWithValidator(s, pair)), (s, pair) => ContainsValidator(s, pair));

	        CreateCharacterFilter(uiUser, parent);

	        CreateSelectionHeader(uiUser, parent, FormatString("Label.ViewGroups", uiUser.Player, uiUser.PermissionTargetName.StripTags()), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);

	        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
		        .WithStyle(m_PanelStyle)
		        .WithLayoutGroup(m_GroupViewLayout, dst, uiUser.Page, (int i, string t, BaseContainer layout, Anchor anchor, Offset offset) =>
		        {
			        ImageContainer.Create(layout, anchor, offset)
				        .WithStyle(m_ButtonStyle)
				        .WithChildren(template =>
				        {
					        TextContainer.Create(template, Anchor.FullStretch, Offset.zero)
						        .WithText(t)
						        .WithAlignment(TextAnchor.MiddleCenter);

					        ImageContainer.Create(template, Anchor.CenterRight, new Offset(-45f, -10f, -5f, 10f))
						        .WithStyle(m_GroupDeleteButton)
						        .WithChildren(button =>
						        {
							        TextContainer.Create(button, Anchor.FullStretch, Offset.zero)
								        .WithSize(10)
								        .WithText(GetString("Button.Delete", uiUser.Player))
								        .WithAlignment(TextAnchor.MiddleCenter);

							        ButtonContainer.Create(button, Anchor.FullStretch, Offset.zero)
								        .WithColor(Color.Clear)
								        .WithCallback(m_CallbackHandler, arg => CreateDeleteGroupOverlay(uiUser, t), $"{uiUser.Player.UserIDString}.delete.{i}");
						        });

					        ImageContainer.Create(template, Anchor.CenterLeft, new Offset(5f, -10f, 45f, 10f))
						        .WithStyle(m_GroupCloneButton)
						        .WithChildren(button =>
						        {
							        TextContainer.Create(button, Anchor.FullStretch, Offset.zero)
								        .WithSize(10)
								        .WithText(GetString("Button.Clone", uiUser.Player))
								        .WithAlignment(TextAnchor.MiddleCenter);

							        ButtonContainer.Create(button, Anchor.FullStretch, Offset.zero)
								        .WithColor(Color.Clear)
								        .WithCallback(m_CallbackHandler, arg => CreateCloneGroupOverlay(uiUser, t), $"{uiUser.Player.UserIDString}.clone.{i}");
						        });
				        });
		        });

	        Facepunch.Pool.FreeList(ref src);
	        Facepunch.Pool.FreeList(ref dst);
        }
        
        private void CreateGroupUsersMenu(UIUser uiUser, BaseContainer parent)
        {
	        if (string.IsNullOrEmpty(uiUser.PermissionTarget))
	        {
		        List<string> dst = Facepunch.Pool.GetList<string>();

		        GetApplicableGroups(uiUser, dst);
		        
		        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectGroup", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);

		        CreateCharacterFilter(uiUser, parent);
		        
		        LayoutSelectionGrid(uiUser, parent,  dst, (s) => s, s =>
		        {
			        uiUser.PermissionTarget = s;
			        CreateAdminMenu(uiUser.Player);
		        });
		        
		        Facepunch.Pool.FreeList(ref dst);
	        }
	        else
	        {
		        List<string> src = Facepunch.Pool.GetList<string>();
		        List<string> dst = Facepunch.Pool.GetList<string>();

		        src.AddRange(permission.GetUsersInGroup(uiUser.PermissionTarget));

		        FilterList(src, dst, uiUser, ((s, pair) => StartsWithValidator(s, pair)), (s, pair) => ContainsValidator(s, pair));

		        CreateCharacterFilter(uiUser, parent);

		        CreateSelectionHeader(uiUser, parent, FormatString("Label.ViewGroupUsers", uiUser.Player, uiUser.PermissionTarget), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);

		        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
			        .WithStyle(m_PanelStyle)
			        .WithLayoutGroup(m_GroupViewLayout, dst, uiUser.Page, (int i, string t, BaseContainer layout, Anchor anchor, Offset offset) =>
			        {
				        ImageContainer.Create(layout, anchor, offset)
					        .WithStyle(m_ButtonStyle)
					        .WithChildren(template =>
					        {
						        TextContainer.Create(template, Anchor.FullStretch, Offset.zero)
							        .WithText(t.Substring(18).TrimStart('(').TrimEnd(')'))
							        .WithAlignment(TextAnchor.MiddleCenter);

						        ImageContainer.Create(template, Anchor.CenterRight, new Offset(-45f, -10f, -5f, 10f))
							        .WithStyle(m_GroupDeleteButton)
							        .WithChildren(button =>
							        {
								        TextContainer.Create(button, Anchor.FullStretch, Offset.zero)
									        .WithSize(10)
									        .WithText(GetString("Button.Remove", uiUser.Player))
									        .WithAlignment(TextAnchor.MiddleCenter);

								        ButtonContainer.Create(button, Anchor.FullStretch, Offset.zero)
									        .WithColor(Color.Clear)
									        .WithCallback(m_CallbackHandler, arg =>
									        {
										        string id = t.Split(' ')?[0];
										        if (!string.IsNullOrEmpty(id))
										        {
											        permission.RemoveUserGroup(id, uiUser.PermissionTarget);
											        CreateAdminMenu(uiUser.Player);
										        }
									        }, $"{uiUser.Player.UserIDString}.removegroup.{i}");
							        });
					        });
			        });

		        Facepunch.Pool.FreeList(ref src);
		        Facepunch.Pool.FreeList(ref dst);
	        }
        }

        private void CreateUserGroupsMenu(UIUser uiUser, BaseContainer parent)
        {
	        CreateCharacterFilter(uiUser, parent);

	        if (string.IsNullOrEmpty(uiUser.PermissionTarget))
	        {
		        List<IPlayer> dst = Facepunch.Pool.GetList<IPlayer>();

		        GetApplicablePlayers(uiUser, dst);

		        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectPlayer", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

		        LayoutSelectionGrid(uiUser, parent, dst, (s) => s.Name.StripTags(), player =>
		        {
			        uiUser.CharacterFilter = m_CharacterFilter[0];
			        uiUser.SearchFilter = string.Empty;
			        uiUser.PermissionTarget = player.Id;
			        uiUser.PermissionTargetName = player.Name;
		        });

		        Facepunch.Pool.FreeList(ref dst);
	        }
	        else
	        {
		        List<string> dst = Facepunch.Pool.GetList<string>();
		        GetApplicableGroups(uiUser, dst);

		        CreateSelectionHeader(uiUser, parent, FormatString("Label.ToggleGroup", uiUser.Player, uiUser.PermissionTargetName.StripTags()), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);

		        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
			        .WithStyle(m_PanelStyle)
			        .WithLayoutGroup(m_ListLayout, dst, uiUser.Page, (int i, string t, BaseContainer permissionLayout, Anchor anchor, Offset offset) =>
			        {
				        bool isInGroup = permission.UserHasGroup(uiUser.PermissionTarget, t);

				        BaseContainer permissionEntry = ImageContainer.Create(permissionLayout, anchor, offset)
					        .WithStyle(m_PermissionStyle)
					        .WithChildren(template =>
					        {
						        TextContainer.Create(template, Anchor.FullStretch, new Offset(5, 0, -5, 0))
							        .WithText(t)
							        .WithAlignment(TextAnchor.MiddleCenter);

						        ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        if (isInGroup)
									        permission.RemoveUserGroup(uiUser.PermissionTarget, t);
								        else permission.AddUserGroup(uiUser.PermissionTarget, t);

								        CreateAdminMenu(uiUser.Player);
							        }, $"{uiUser.Player.UserIDString}.group.{i}");
					        });

				        if (isInGroup)
					        permissionEntry.WithOutline(m_OutlineGreen);
			        });

		        Facepunch.Pool.FreeList(ref dst);
	        }
        }

        private void CreateGroupCreateOverlay(UIUser uiUser)
        {
	        BaseContainer baseContainer = ButtonContainer.Create(ADMINMENU_UI, Layer.Overall, Anchor.FullStretch, Offset.zero)
		        .WithStyle(m_BackgroundStyle)
		        .WithCallback(m_CallbackHandler, arg =>
		        {
			        uiUser.SubMenuIndex = 0;
			        CreateAdminMenu(uiUser.Player);
		        }, $"{uiUser.Player.UserIDString}.cancel")
		        .WithChildren(createGroupPopup =>
				{
					ImageContainer.Create(createGroupPopup, Anchor.Center, new Offset(-175f, 60f, 175f, 80f))
						.WithStyle(m_PanelStyle)
						.WithChildren(title =>
						{
							TextContainer.Create(title, Anchor.FullStretch, Offset.zero)
								.WithText(GetString("Label.CreateUsergroup", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleCenter);

						});

					ImageContainer.Create(createGroupPopup, Anchor.Center, new Offset(-175f, -55f, 175f, 55f))
						.WithStyle(m_PanelStyle)
						.WithChildren(inputs =>
						{
							TextContainer.Create(inputs, Anchor.TopStretch, new Offset(5f, -25f, -145f, -5f))
								.WithText(GetString("Label.Name", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -25f, -4.999996f, -5f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(nameInput =>
								{
									InputFieldContainer.Create(nameInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.GroupName)
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GroupName = arg.GetString(1);
										}, $"{uiUser.Player.UserIDString}.name.input");
								});

							TextContainer.Create(inputs, Anchor.TopStretch, new Offset(5f, -50f, -145f, -30f))
								.WithText(GetString("Label.Title", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -50f, -4.999996f, -30f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(titleInput =>
								{
									InputFieldContainer.Create(titleInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.GroupTitle)
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GroupTitle = arg.GetString(1);
										}, $"{uiUser.Player.UserIDString}.title.input");
								});

							TextContainer.Create(inputs, Anchor.TopStretch, new Offset(5f, -75f, -145f, -55f))
								.WithText(GetString("Label.Rank", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -75f, -4.999969f, -55f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(rankInput =>
								{
									InputFieldContainer.Create(rankInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.GroupRank.ToString())
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GroupRank = arg.GetInt(1);
											CreateGroupCreateOverlay(uiUser);
										}, $"{uiUser.Player.UserIDString}.rank.input");
								});

							ImageContainer.Create(inputs, Anchor.BottomLeft, new Offset(5f, 5f, 95f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineGreen)
								.WithChildren(create =>
								{
									TextContainer.Create(create, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Create", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(create, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg =>
										{
											if (string.IsNullOrEmpty(uiUser.GroupName))
											{
												CreatePopupMessage(uiUser, "You must enter a group name");
												return;
											}

											if (permission.GroupExists(uiUser.GroupName))
											{
												CreatePopupMessage(uiUser, "A group with that name already exists");
												return;
											}
											
											permission.CreateGroup(uiUser.GroupName, uiUser.GroupTitle, uiUser.GroupRank);
											
											uiUser.ClearGroup();
											uiUser.SubMenuIndex = 0;
											CreateAdminMenu(uiUser.Player);
											CreatePopupMessage(uiUser, "Group created");
										},$"{uiUser.Player.UserIDString}.create");

								});

							ImageContainer.Create(inputs, Anchor.BottomRight, new Offset(-95f, 5f, -5f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineRed)
								.WithChildren(cancel =>
								{
									TextContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Cancel", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.SubMenuIndex = 0;
											CreateAdminMenu(uiUser.Player);
										},$"{uiUser.Player.UserIDString}.cancel");

								});
						});
				});
		        //.NeedsCursor()
		        //.NeedsKeyboard();
	        
	        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
	        ChaosUI.Show(uiUser.Player, baseContainer);
        }

        private void CreateCloneGroupOverlay(UIUser uiUser, string usergroup)
        {
	        BaseContainer baseContainer = ButtonContainer.Create(ADMINMENU_UI, Layer.Overall, Anchor.FullStretch, Offset.zero)
				.WithStyle(m_BackgroundStyle)
				.WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.cancel")
				.WithChildren(cloneGroupPopup =>
				{
					ImageContainer.Create(cloneGroupPopup, Anchor.Center, new Offset(-175f, 72.5f, 175f, 92.5f))
						.WithStyle(m_PanelStyle)
						.WithChildren(title =>
						{
							TextContainer.Create(title, Anchor.FullStretch, Offset.zero)
								.WithText(FormatString("Label.CloneUsergroup", uiUser.Player, usergroup))
								.WithAlignment(TextAnchor.MiddleCenter);

						});

					ImageContainer.Create(cloneGroupPopup, Anchor.Center, new Offset(-175f, -67.5f, 175f, 67.5f))
						.WithStyle(m_PanelStyle)
						.WithChildren(inputs =>
						{
							TextContainer.Create(inputs, Anchor.TopStretch, new Offset(5f, -25f, -145f, -5f))
								.WithText(GetString("Label.Name", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -25f, -4.999996f, -5f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(nameInput =>
								{
									InputFieldContainer.Create(nameInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.GroupName)
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GroupName = arg.GetString(1);
										}, $"{uiUser.Player.UserIDString}.name.input");
								});

							TextContainer.Create(inputs, Anchor.TopStretch, new Offset(5f, -50f, -145f, -30f))
								.WithText(GetString("Label.Title", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -50f, -4.999996f, -30f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(titleInput =>
								{
									InputFieldContainer.Create(titleInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithText(uiUser.GroupTitle)
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GroupTitle = arg.GetString(1);
										}, $"{uiUser.Player.UserIDString}.title.input");
								});

							TextContainer.Create(inputs, Anchor.TopStretch, new Offset(5f, -75f, -145f, -55f))
								.WithText(GetString("Label.Rank", uiUser.Player))
								.WithAlignment(TextAnchor.MiddleLeft);

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -75f, -4.999969f, -55f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(rankInput =>
								{
									InputFieldContainer.Create(rankInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
										.WithAlignment(TextAnchor.MiddleLeft)
										.WithText(uiUser.GroupRank.ToString())
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.GroupRank = arg.GetInt(1);
											CreateCloneGroupOverlay(uiUser, usergroup);
										}, $"{uiUser.Player.UserIDString}.rank.input");

								});

							ImageContainer.Create(inputs, Anchor.TopStretch, new Offset(120f, -100f, -210f, -80f))
								.WithStyle(m_ButtonStyle)
								.WithChildren(toggle =>
								{
									if (uiUser.CopyUsers)
									{
										TextContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
											.WithStyle(m_ToggleLabelStyle)
											.WithText("•");
									}

									ButtonContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg =>
										{
											uiUser.CopyUsers = !uiUser.CopyUsers;
											CreateCloneGroupOverlay(uiUser, usergroup);
										},$"{uiUser.Player.UserIDString}.rank.input");

									TextContainer.Create(toggle, Anchor.CenterLeft, new Offset(-115f, -10f, -15f, 10f))
										.WithText(GetString("Label.CopyUsers", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleLeft);

								});

							ImageContainer.Create(inputs, Anchor.BottomLeft, new Offset(5f, 5f, 95f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineGreen)
								.WithChildren(create =>
								{
									TextContainer.Create(create, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Create", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(create, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg =>
										{
											if (string.IsNullOrEmpty(uiUser.GroupName))
											{
												CreatePopupMessage(uiUser, "You must enter a group name");
												return;
											}

											if (permission.GroupExists(uiUser.GroupName))
											{
												CreatePopupMessage(uiUser, "A group with that name already exists");
												return;
											}

											if (permission.CreateGroup(uiUser.GroupName, uiUser.GroupTitle, uiUser.GroupRank))
											{
												string[] perms = permission.GetGroupPermissions(usergroup);
												
												for (int i = 0; i < perms.Length; i++)
													permission.GrantGroupPermission(uiUser.GroupName, perms[i], null);

												if (uiUser.CopyUsers)
												{
													string[] users = permission.GetUsersInGroup(usergroup);
													for (int i = 0; i < users.Length; i++)
													{
														string userId = users[i].Split(' ')?[0];
														if (!string.IsNullOrEmpty(userId))
															userId.AddToGroup(uiUser.GroupName);
													}
												}
											}
											
											uiUser.ClearGroup();
											uiUser.SubMenuIndex = 0;
											CreateAdminMenu(uiUser.Player);
											CreatePopupMessage(uiUser, "Group cloned successfully");
										},$"{uiUser.Player.UserIDString}.create");

								});

							ImageContainer.Create(inputs, Anchor.BottomRight, new Offset(-95f, 5f, -5f, 25f))
								.WithStyle(m_ButtonStyle)
								.WithOutline(m_OutlineRed)
								.WithChildren(cancel =>
								{
									TextContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithText(GetString("Button.Cancel", uiUser.Player))
										.WithAlignment(TextAnchor.MiddleCenter);

									ButtonContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
										.WithColor(Color.Clear)
										.WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.cancel");
								});
						});
				});
				//.NeedsCursor()
				//.NeedsKeyboard();
	        
	        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
	        ChaosUI.Show(uiUser.Player, baseContainer);
        }

        private void CreateDeleteGroupOverlay(UIUser uiUser, string usergroup)
        {
	        BaseContainer baseContainer = ButtonContainer.Create(ADMINMENU_UI, Layer.Overall, Anchor.FullStretch, Offset.zero)
		        .WithStyle(m_BackgroundStyle)
		        .WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.cancel")
		        .WithChildren(deleteGroupPopup =>
		        {
			        ImageContainer.Create(deleteGroupPopup, Anchor.Center, new Offset(-150f, 20f, 150f, 40f))
				        .WithStyle(m_PanelStyle)
				        .WithChildren(title =>
				        {
					        TextContainer.Create(title, Anchor.FullStretch, Offset.zero)
						        .WithText(FormatString("Label.DeleteConfirm", uiUser.Player, usergroup))
						        .WithAlignment(TextAnchor.MiddleCenter);
				        });

			        ImageContainer.Create(deleteGroupPopup, Anchor.Center, new Offset(-150f, -15f, 150f, 15f))
				        .WithStyle(m_PanelStyle)
				        .WithChildren(inputs =>
				        {
					        ImageContainer.Create(inputs, Anchor.BottomLeft, new Offset(5f, 5f, 135f, 25f))
						        .WithStyle(m_ButtonStyle)
						        .WithOutline(m_OutlineGreen)
						        .WithChildren(confirm =>
						        {
							        TextContainer.Create(confirm, Anchor.FullStretch, Offset.zero)
								        .WithText(GetString("Button.Confirm", uiUser.Player))
								        .WithAlignment(TextAnchor.MiddleCenter);

							        ButtonContainer.Create(confirm, Anchor.FullStretch, Offset.zero)
								        .WithColor(Color.Clear)
								        .WithCallback(m_CallbackHandler, arg =>
								        {
									        permission.RemoveGroup(usergroup);
									        CreateAdminMenu(uiUser.Player);
								        }, $"{uiUser.Player.UserIDString}.delete");

						        });

					        ImageContainer.Create(inputs, Anchor.BottomRight, new Offset(-135f, 5f, -5f, 25f))
						        .WithStyle(m_ButtonStyle)
						        .WithOutline(m_OutlineRed)
						        .WithChildren(cancel =>
						        {
							        TextContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
								        .WithText(GetString("Button.Cancel", uiUser.Player))
								        .WithAlignment(TextAnchor.MiddleCenter);

							        ButtonContainer.Create(cancel, Anchor.FullStretch, Offset.zero)
								        .WithColor(Color.Clear)
								        .WithCallback(m_CallbackHandler, arg => CreateAdminMenu(uiUser.Player), $"{uiUser.Player.UserIDString}.cancel");

						        });
				        });
		        });
		        //.NeedsCursor()
		        //.NeedsKeyboard();
	        
	        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI);
	        ChaosUI.Show(uiUser.Player, baseContainer);
        }

        #endregion
        
        #region Convars
        private void CreateConvarMenu(UIUser uiUser, BaseContainer parent)
        {
	        List<ConsoleSystem.Command> src = Facepunch.Pool.GetList<ConsoleSystem.Command>();
	        List<ConsoleSystem.Command> dst = Facepunch.Pool.GetList<ConsoleSystem.Command>();

	        src.AddRange(ConsoleGen.All.Where(x => x.ServerAdmin && x.Variable));
	        
	        FilterList(src, dst, uiUser, (s, command) => StartsWithValidator(s, command.FullName), (s, command) => ContainsValidator(s, command.FullName));

	        CreateCharacterFilter(uiUser, parent);
			        
	        CreateSelectionHeader(uiUser, parent, string.Empty, m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);
	        
	        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
		        .WithStyle(m_PanelStyle)
		        .WithLayoutGroup(m_ConvarLayout, dst, uiUser.Page, (int i, ConsoleSystem.Command t, BaseContainer layout, Anchor anchor, Offset offset) =>
		        {
			        ImageContainer.Create(layout, anchor, offset)
				        .WithStyle(m_ConvarStyle)
				        .WithChildren(template =>
				        {
					        TextContainer.Create(template, Anchor.FullStretch, new Offset(5f, 13f, -130f, -1f))
						        .WithSize(12)
						        .WithText(t.FullName);

					        if (!string.IsNullOrEmpty(t.Description))
					        {
						        TextContainer.Create(template, Anchor.FullStretch, new Offset(5f, 1f, -130f, -15f))
							        .WithStyle(m_ConvarDescriptionStyle)
							        .WithText(t.Description);
					        }

					        ImageContainer.Create(template, Anchor.CenterRight, new Offset(-125f, -10f, -5f, 10f))
						        .WithStyle(m_ButtonStyle)
						        .WithChildren(input =>
						        {
							        InputFieldContainer.Create(input, Anchor.FullStretch, Offset.zero)
								        .WithSize(12)
								        .WithText(t.String)
								        .WithAlignment(TextAnchor.MiddleCenter)
								        .WithCallback(m_CallbackHandler, arg =>
								        {
									        ConsoleSystem.Run(ConsoleSystem.Option.Server, t.FullName, arg.GetString(1));
									        CreateAdminMenu(uiUser.Player);
								        }, $"{uiUser.Player.UserIDString}.convar.{i}");
						        });
				        });
		        });
	        
	        Facepunch.Pool.FreeList(ref src);
	        Facepunch.Pool.FreeList(ref dst);
        }
        #endregion

		#region Filters
        private void CreateCharacterFilter(UIUser uiUser, BaseContainer parent)
        {
	        ImageContainer.Create(parent, Anchor.LeftStretch, new Offset(0f, 0f, 20f, -30f))
		        .WithStyle(m_PanelStyle)
		        .WithLayoutGroup(m_CharacterFilterLayout, m_CharacterFilter, 0, (int i, string t, BaseContainer filterList, Anchor anchor, Offset offset) =>
		        {
			        BaseContainer filterButton = ImageContainer.Create(filterList, anchor, offset)
				        .WithStyle(m_ButtonStyle)
				        .WithChildren(characterTemplate =>
				        {
					        TextContainer.Create(characterTemplate, Anchor.FullStretch, Offset.zero)
						        .WithSize(12)
						        .WithText(t)
						        .WithAlignment(TextAnchor.MiddleCenter);

					        if (t != uiUser.CharacterFilter)
					        {
						        ButtonContainer.Create(characterTemplate, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        uiUser.CharacterFilter = t;
								        uiUser.Page = 0;
								        CreateAdminMenu(uiUser.Player);
							        }, $"{uiUser.Player.UserIDString}.filter.{i}");
					        }
				        });

			        if (t == uiUser.CharacterFilter)
				        filterButton.WithOutline(m_OutlineGreen);
		        });
        }
        
        private void FilterList<T>(List<T> src, List<T> dst, UIUser uiUser, Func<string, T, bool> startsWith, Func<string, T, bool> contains)
        {
	        bool useCharacterFilter = !string.IsNullOrEmpty(uiUser.CharacterFilter) && uiUser.CharacterFilter != m_CharacterFilter[0];
	        bool useSearchFilter = !string.IsNullOrEmpty(uiUser.SearchFilter);
				        
	        if (!useCharacterFilter && !useSearchFilter)
		        dst.AddRange(src);
	        else
	        {
		        for (int i = 0; i < src.Count; i++)
		        {
			        T t = src[i];

			        if (useSearchFilter && useCharacterFilter)
			        {
				        if (startsWith(uiUser.CharacterFilter, t) && contains(uiUser.SearchFilter, t))
					        dst.Add(t);

				        continue;
			        }

			        if (useCharacterFilter)
			        {
				        if (startsWith(uiUser.CharacterFilter, t))
					        dst.Add(t);
				        
				        continue;
			        }
						        
			        if (useSearchFilter && contains(uiUser.SearchFilter, t))
				        dst.Add(t);
		        }
	        }
        }

        private bool StartsWithValidator(string character, string phrase) => phrase.StartsWith(character, StringComparison.OrdinalIgnoreCase);
        
        private bool ContainsValidator(string character, string phrase) => phrase.Contains(character, CompareOptions.OrdinalIgnoreCase);
        
        private void GetApplicablePlayers(UIUser uiUser, List<IPlayer> dst)
        {
	        List<IPlayer> src = Facepunch.Pool.GetList<IPlayer>();

	        if (uiUser.ShowOnlinePlayers)
		        src.AddRange(covalence.Players.Connected);

	        if (uiUser.ShowOfflinePlayers)
		        m_RecentPlayers.Data.GetRecentPlayers(covalence.Players.All, ref src);

	        FilterList(src, dst, uiUser, (s, player) => StartsWithValidator(s, player.Name.StripTags()), (s, player) => ContainsValidator(s, player.Name.StripTags()));

	        Facepunch.Pool.FreeList(ref src);
        }

        private void GetApplicableGroups(UIUser uiUser, List<string> dst)
        {
	        List<string> src = Facepunch.Pool.GetList<string>();

	        src.AddRange(permission.GetGroups());

	        FilterList(src, dst, uiUser, (s, group) => StartsWithValidator(s, group), (s, group) => ContainsValidator(s, group));

	        Facepunch.Pool.FreeList(ref src);
        }
        #endregion
        
        #region Permission Toggling

        private void CreatePermissionsMenu(UIUser uiUser, BaseContainer parent)
        {
	        CreateCharacterFilter(uiUser, parent);

	        if (string.IsNullOrEmpty(uiUser.PermissionTarget))
	        {
		        if (uiUser.SubMenuIndex == 0)
		        {
			        List<IPlayer> dst = Facepunch.Pool.GetList<IPlayer>();

			        GetApplicablePlayers(uiUser, dst);

			        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectPlayer", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

			        LayoutSelectionGrid(uiUser, parent, dst, (s) => s.Name.StripTags(), player =>
			        {
				        uiUser.CharacterFilter = m_CharacterFilter[0];
				        uiUser.SearchFilter = string.Empty;
				        uiUser.PermissionTarget = player.Id;
				        uiUser.PermissionTargetName = player.Name;
			        });

			        Facepunch.Pool.FreeList(ref dst);
		        }
		        else
		        {
			        List<string> dst = Facepunch.Pool.GetList<string>();

			        GetApplicableGroups(uiUser, dst);

			        CreateSelectionHeader(uiUser, parent, GetString("Label.SelectGroup", uiUser.Player), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, true);

			        LayoutSelectionGrid(uiUser, parent, dst, (s) => s, s =>
			        {
				        uiUser.PermissionTarget = uiUser.PermissionTargetName = s;
				        uiUser.CharacterFilter = m_CharacterFilter[0];
				        uiUser.SearchFilter = string.Empty;
			        });

			        Facepunch.Pool.FreeList(ref dst);
		        }
	        }
	        else
	        {
		        List<KeyValuePair<string, bool>> dst = Facepunch.Pool.GetList<KeyValuePair<string, bool>>();

		        if (uiUser.CharacterFilter != m_CharacterFilter[0] || !string.IsNullOrEmpty(uiUser.SearchFilter))
		        {
			        List<KeyValuePair<string, bool>> src = Facepunch.Pool.GetList<KeyValuePair<string, bool>>();
			        
			        for (int i = 0; i < m_Permissions.Count; i++)
			        {
				        KeyValuePair<string, bool> kvp = m_Permissions[i];
				        if (kvp.Value)
							src.Add(kvp);
			        }
			        
			        FilterList(src, dst, uiUser, ((s, pair) => StartsWithValidator(s, pair.Key)), (s, pair) => ContainsValidator(s, pair.Key));
			        Facepunch.Pool.FreeList(ref src);

		        }
		        else dst.AddRange(m_Permissions);

		        BaseContainer header = CreateSelectionHeader(uiUser, parent, FormatString("Label.TogglePermission", uiUser.Player, uiUser.PermissionTargetName.StripTags()), m_ListLayout.HasNextPage(uiUser.Page, dst.Count), uiUser.Page > 0, false);

		        if (uiUser.SubMenuIndex == 0)
		        {
			        ImageContainer.Create(header, Anchor.CenterLeft, new Offset(40f, -5f, 50f, 5f))
				        .WithColor(m_OutlineGreen.Color)
				        .WithSprite(Sprites.Background_Rounded)
				        .WithImageType(Image.Type.Tiled)
				        .WithChildren(permissionColorHas =>
				        {
					        TextContainer.Create(permissionColorHas, Anchor.CenterRight, new Offset(5f, -10f, 155f, 10f))
						        .WithSize(12)
						        .WithText(GetString("Label.DirectPermission", uiUser.Player))
						        .WithAlignment(TextAnchor.MiddleLeft);

				        });

			        ImageContainer.Create(header, Anchor.CenterLeft, new Offset(165f, -5f, 175f, 5f))
				        .WithColor(m_OutlineBlue.Color)
				        .WithSprite(Sprites.Background_Rounded)
				        .WithImageType(Image.Type.Tiled)
				        .WithChildren(permissionColorInherit =>
				        {
					        TextContainer.Create(permissionColorInherit, Anchor.CenterRight, new Offset(5f, -10f, 155f, 10f))
						        .WithSize(12)
						        .WithText(GetString("Label.InheritedPermission", uiUser.Player))
						        .WithAlignment(TextAnchor.MiddleLeft);

				        });

		        }
		        LayoutPermissionGrid(uiUser, parent, dst);

		        Facepunch.Pool.FreeList(ref dst);
	        }
        }

        private void LayoutPermissionGrid(UIUser uiUser, BaseContainer parent, List<KeyValuePair<string, bool>> list)
        {
	        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
		        .WithStyle(m_PanelStyle)
		        .WithLayoutGroup(m_ListLayout, list, uiUser.Page, (int i, KeyValuePair<string, bool> t, BaseContainer permissionLayout, Anchor anchor, Offset offset) =>
		        {
			        bool isUserPermission = uiUser.SubMenuIndex == 0;
			        bool isGroupPermission = uiUser.SubMenuIndex == 1;

			        bool hasPermission = (isUserPermission && UserHasPermissionNoGroup(uiUser.PermissionTarget, t.Key)) || (isGroupPermission && permission.GroupHasPermission(uiUser.PermissionTarget, t.Key));
			        bool usersGroupHasPermission = isUserPermission && UsersGroupsHavePermission(uiUser.PermissionTarget, t.Key);
			        
			        BaseContainer permissionEntry = ImageContainer.Create(permissionLayout, anchor, offset + (t.Value ? new Offset(5f, 0f, -5f, 0f) : Offset.zero))
				        .WithStyle(t.Value ? m_PermissionStyle : m_PermissionHeaderStyle)
				        .WithChildren(template =>
				        {
					        if (t.Key.Contains("."))
					        {
						        int index = t.Key.IndexOf(".");
						       
						        TextContainer.Create(template, Anchor.FullStretch, new Offset(5, 1, -5, -1))
							        .WithText(t.Key.Substring(0, index))
							        .WithStyle(t.Value ? m_PermissionStyle : m_PermissionHeaderStyle)
							        .WithAlignment(TextAnchor.UpperCenter);

						        TextContainer.Create(template, Anchor.FullStretch, new Offset(5, 1, -5, -1))
							        .WithText(t.Key.Substring(index + 1))
							        .WithStyle(t.Value ? m_PermissionStyle : m_PermissionHeaderStyle)
							        .WithAlignment(TextAnchor.LowerCenter);
					        }
					        else
					        {
						        TextContainer.Create(template, Anchor.FullStretch, new Offset(5, 0, -5, 0))
							        .WithText(t.Key)
							        .WithStyle(t.Value ? m_PermissionStyle : m_PermissionHeaderStyle);
					        }
				        

							if (isUserPermission || isGroupPermission)
					        {
						        ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
							        .WithColor(Color.Clear)
							        .WithCallback(m_CallbackHandler, arg =>
							        {
								        if (isUserPermission)
								        {
									        if (hasPermission)
										        permission.RevokeUserPermission(uiUser.PermissionTarget, t.Key);
									        else permission.GrantUserPermission(uiUser.PermissionTarget, t.Key, null);
								        }

								        if (isGroupPermission)
								        {
									        if (hasPermission)
										        permission.RevokeGroupPermission(uiUser.PermissionTarget, t.Key);
									        else permission.GrantGroupPermission(uiUser.PermissionTarget, t.Key, null);
								        }

								        CreateAdminMenu(uiUser.Player);
							        }, $"{uiUser.Player.UserIDString}.permission.{i}");
					        }
				        });

			        if (!t.Value)
				        permissionEntry.WithOutline(m_OutlineWhite);
			        else
			        {
				        if (hasPermission)
					        permissionEntry.WithOutline(m_OutlineGreen);

				        if (!hasPermission && usersGroupHasPermission)
					        permissionEntry.WithOutline(m_OutlineBlue);
			        }
		        });
        }
        #endregion
        
        #region Selection Grid
        private void LayoutSelectionGrid<T>(UIUser uiUser, BaseContainer parent, List<T> list, Func<T, string> asString,Action<T> callback)
        {
	        ImageContainer.Create(parent, Anchor.FullStretch, new Offset(25f, 0f, 0f, -30f))
		        .WithStyle(m_PanelStyle)
		        .WithLayoutGroup(m_ListLayout, list, uiUser.Page, (int i, T t, BaseContainer layout, Anchor anchor, Offset offset) =>
		        {
			        ImageContainer.Create(layout, anchor, offset)
				        .WithStyle(m_ButtonStyle)
				        .WithChildren(header =>
				        {
					        TextContainer.Create(header, Anchor.FullStretch, Offset.zero)
						        .WithText(asString(t))
						        .WithAlignment(TextAnchor.MiddleCenter);

					        ButtonContainer.Create(header, Anchor.FullStretch, Offset.zero)
						        .WithColor(Color.Clear)
						        .WithCallback(m_CallbackHandler, arg =>
						        {
							        callback.Invoke(t);
							        CreateAdminMenu(uiUser.Player);
						        }, $"{uiUser.Player.UserIDString}.select.{i}");
				        });
		        });
        }
        #endregion
        
        #region Popup Message

        private Hash<ulong, Timer> m_PopupTimers = new Hash<ulong, Timer>();

        private void CreatePopupMessage(UIUser uiUser, string message)
        {
	        BaseContainer baseContainer = ImageContainer.Create(ADMINMENU_UI_POPUP, Layer.Overall, Anchor.Center, new Offset(-540f, -345f, 540f, -315f))
		        .WithStyle(m_BackgroundStyle)
		        .WithChildren(popup =>
		        {
			        ImageContainer.Create(popup, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
				        .WithStyle(m_PanelStyle)
				        .WithChildren(titleBar =>
				        {
					        TextContainer.Create(titleBar, Anchor.FullStretch, Offset.zero)
						        .WithText(message)
						        .WithAlignment(TextAnchor.MiddleCenter);

				        });
		        });
			
	        ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI_POPUP);
	        ChaosUI.Show(uiUser.Player, baseContainer);

	        Timer t;
	        if (m_PopupTimers.TryGetValue(uiUser.Player.userID, out t))
		        t?.Destroy();

	        m_PopupTimers[uiUser.Player.userID] = timer.Once(5f, () => ChaosUI.Destroy(uiUser.Player, ADMINMENU_UI_POPUP));
        }
        #endregion
        #endregion
        
        #region Configuration
        private ConfigData Configuration => ConfigurationData as ConfigData;
        

        protected override ConfigurationFile OnLoadConfig(ref ConfigurationFile configurationFile) => configurationFile = new ConfigurationFile<ConfigData>(Config);
        

        protected override void OnConfigurationUpdated(VersionNumber oldVersion)
        {
	        ConfigData baseConfigData = GenerateDefaultConfiguration<ConfigData>();
	        
	        if (oldVersion < new VersionNumber(2, 0, 0))
		        ConfigurationData = baseConfigData;
        }
        
        protected class ConfigData : BaseConfigData
        {
            [JsonProperty(PropertyName = "Chat Command List")]
            public List<CommandEntry> ChatCommands { get; set; }

            [JsonProperty(PropertyName = "Console Command List")]
            public List<CommandEntry> ConsoleCommands { get; set; }

            [JsonProperty(PropertyName = "Player Info Custom Commands")]
            public List<CustomCommands> PlayerInfoCommands { get; set; }

            [JsonProperty(PropertyName = "Use different permissions for each section of the player administration tab")]
            public bool UsePlayerAdminPermissions { get; set; }
            
            public class CommandEntry
            {
                public string Name { get; set; }
                
                public string Command { get; set; }
                
                public string Description { get; set; }
                
                public bool CloseOnRun { get; set; }
                
                public string RequiredPermission { get; set; } = string.Empty;
            }
            
            public class CustomCommands
            {
                public string Name { get; set; }

                public List<PlayerInfoCommandEntry> Commands { get; set; }
                
                public class PlayerInfoCommandEntry : CommandEntry
                {            
                    public string RequiredPlugin { get; set; }

                    [JsonProperty(PropertyName = "Command Type ( Chat, Console )")]
                    public CommandSubType SubType { get; set; }            
                }
            }
        }
        
        protected override T GenerateDefaultConfiguration<T>()
        {
            return new ConfigData
            {
                ChatCommands = new List<ConfigData.CommandEntry>
                {
	                new ConfigData.CommandEntry
	                {
		                Name = "These are examples",
		                Command = "/example",
		                Description = "To show how to create your own"
	                },
                    new ConfigData.CommandEntry
                    {
                        Name = "TP to 0 0 0",
                        Command = "/tp 0 0 0",
                        Description = "Teleport self to 0 0 0"
                    },
                    new ConfigData.CommandEntry
                    {
                        Name = "TP to player",
                        Command = "/tp {target1_name}",
                        Description = "Teleport self to player"
                    },
                    new ConfigData.CommandEntry
                    {
                        Name = "TP P2P",
                        Command = "/tp {target1_name} {target2_name}",
                        Description = "Teleport player to player"
                    },
                    new ConfigData.CommandEntry
                    {
                        Name = "God",
                        Command = "/god",
                        Description = "Toggle god mode"
                    }
                },
                ConsoleCommands = new List<ConfigData.CommandEntry>
                {
	                new ConfigData.CommandEntry
	                {
		                Name = "These are examples",
		                Command = "example",
		                Description = "To show how to create your own"
	                },
                    new ConfigData.CommandEntry
                    {
                        Name = "Set time to 9",
                        Command = "env.time 9",
                        Description = "Set the time to 9am"
                    },
                    new ConfigData.CommandEntry
                    {
                        Name = "Set to to 22",
                        Command = "env.time 22",
                        Description = "Set the time to 10pm"
                    },
                    new ConfigData.CommandEntry
                    {
                        Name = "TP P2P",
                        Command = "teleport.topos {target1_name} {target2_name}",
                        Description = "Teleport player to player"
                    },
                    new ConfigData.CommandEntry
                    {
                        Name = "Call random strike",
                        Command = "airstrike strike random",
                        Description = "Call a random Airstrike"
                    }
                },
                PlayerInfoCommands = new List<ConfigData.CustomCommands>
                {
                    new ConfigData.CustomCommands
                    {
                        Name = "Backpacks",
                        Commands = new List<ConfigData.CustomCommands.PlayerInfoCommandEntry>
                        {
                            new ConfigData.CustomCommands.PlayerInfoCommandEntry
                            {
                                RequiredPlugin = "Backpacks",
                                RequiredPermission = "backpacks.admin",
                                Name = "View Backpack",
                                CloseOnRun = true,
                                Command = "/viewbackpack {target1_id}",
                                SubType = CommandSubType.Chat
                            }
                        }
                    },
                    new ConfigData.CustomCommands
                    {
                        Name = "InventoryViewer",
                        Commands = new List<ConfigData.CustomCommands.PlayerInfoCommandEntry>
                        {
                            new ConfigData.CustomCommands.PlayerInfoCommandEntry
                            {
                                RequiredPlugin = "InventoryViewer",
                                RequiredPermission = "inventoryviewer.allowed",
                                Name = "View Inventory",
                                CloseOnRun = true,
                                Command = "/viewinv {target1_id}",
                                SubType = CommandSubType.Chat
                            }
                        }
                    },
                    new ConfigData.CustomCommands
                    {
                        Name = "Freeze",
                        Commands = new List<ConfigData.CustomCommands.PlayerInfoCommandEntry>
                        {
                            new ConfigData.CustomCommands.PlayerInfoCommandEntry
                            {
                                RequiredPlugin = "Freeze",
                                RequiredPermission = "freeze.use",
                                Name = "Freeze",
                                CloseOnRun = false,
                                Command = "/freeze {target1_id}",
                                SubType = CommandSubType.Chat
                            },
                            new ConfigData.CustomCommands.PlayerInfoCommandEntry
                            {
                                RequiredPlugin = "Freeze",
                                RequiredPermission = "freeze.use",
                                Name = "Unfreeze",
                                CloseOnRun = false,
                                Command = "/unfreeze {target1_id}",
                                SubType = CommandSubType.Chat
                            }
                        }
                    }
                },
                UsePlayerAdminPermissions = false,
            } as T;
        }
        #endregion
        
        #region Data

        private static DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0);
        
        private static double CurrentTimeStamp() =>  DateTime.UtcNow.Subtract(EPOCH).TotalSeconds;  

        private class RecentPlayers
        {
	        [JsonProperty]
	        private Hash<string, double> m_RecentPlayers = new Hash<string, double>();

	        public void OnPlayerConnected(BasePlayer player)
	        {
		        m_RecentPlayers.Remove(player.UserIDString);
	        }
	        
	        public void OnPlayerDisconnected(BasePlayer player)
	        {
		        m_RecentPlayers[player.UserIDString] = CurrentTimeStamp();
	        }

	        public void GetRecentPlayers(IEnumerable<IPlayer> allPlayers, ref List<IPlayer> list)
	        {
		        foreach (IPlayer player in allPlayers)
		        {
			        if (m_RecentPlayers.ContainsKey(player.Id))
				        list.Add(player);
		        }
	        }

	        public void PurgeCollection()
	        {
		        double currentTime = CurrentTimeStamp();
		        
		        for (int i = m_RecentPlayers.Count - 1; i >= 0; i--)
		        {
			        KeyValuePair<string, double> kvp = m_RecentPlayers.ElementAt(i);
			        if (currentTime - kvp.Value > 604800)
				        m_RecentPlayers.Remove(kvp);
		        }
	        }
        }
        #endregion
    }
}