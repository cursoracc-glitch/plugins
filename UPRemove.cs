using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
[Info("UpRemove", "Mevent", "1.3")]
public class UPRemove : RustPlugin
{
    #region Fields

    [PluginReference] private Plugin ImageLibrary, NoEscape, Clans, Friends, Notify, UINotify;
    

    private const string Layer = "UI.UPRemove";
    private const string LayerUpdate = "UI.UPRemove1";

    private static UPRemove _instance;

    private enum Types
    {
        None = -1,
        Remove = 5,
        Wood = 1,
        Stone = 2,
        Metal = 3,
        TopTier = 4
    }

    private const string PermAll = "UPRemove.all";

    private const string modepermission = "UPRemove.use";

    private const string PermFree = "UPRemove.free";

    #endregion

    #region Config

    private static Configuration _config;

    private class Configuration
    {
        [JsonProperty(PropertyName = "Remove Commands")]
        public readonly string[] RemoveCommands = {"remove"};

        [JsonProperty(PropertyName = "Upgrade Commands")]
        public readonly string[] UpgradeCommands = {"up", "building.upgrade"};

        [JsonProperty(PropertyName = "Work with Notify?")]
        public readonly bool UseNotify = true;

        [JsonProperty(PropertyName = "Setting Modes", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public readonly List<Mode> Modes = new List<Mode>
        {
            new Mode
            {
                Type = Types.Remove,
                Icon = "assets/icons/clear.png",
                Permission = string.Empty
            },
            new Mode
            {
                Type = Types.Wood,
                Icon = "assets/icons/level_wood.png",
                Permission = string.Empty
            },
            new Mode
            {
                Type = Types.Stone,
                Icon = "assets/icons/level_stone.png",
                Permission = string.Empty
            },
            new Mode
            {
                Type = Types.Metal,
                Icon = "assets/icons/level_metal.png",
                Permission = string.Empty
            },
            new Mode
            {
                Type = Types.TopTier,
                Icon = "assets/icons/level_top.png",
                Permission = string.Empty
            }
        };

        [JsonProperty(PropertyName = "Upgrade Settings")]
        public readonly UpgradeSettings Upgrade = new UpgradeSettings
        {
            ActionTime = 30,
            Cooldown = 0,
            VipCooldown = new Dictionary<string, int>
            {
                ["buildtool.vip"] = 0,
                ["buildtool.premium"] = 0
            },
            AfterWipe = 0,
            VipAfterWipe = new Dictionary<string, int>
            {
                ["buildtool.vip"] = 0,
                ["buildtool.premium"] = 0
            }
        };

        [JsonProperty(PropertyName = "Remove Settings")]
        public readonly RemoveSettings Remove = new RemoveSettings
        {
            ActionTime = 30,
            Cooldown = 0,
            VipCooldown = new Dictionary<string, int>
            {
                ["buildtool.vip"] = 0,
                ["buildtool.premium"] = 0
            },
            AfterWipe = 0,
            VipAfterWipe = new Dictionary<string, int>
            {
                ["buildtool.vip"] = 0,
                ["buildtool.premium"] = 0
            },
            Condition = new ConditionSettings
            {
                Default = true,
                Percent = false,
                PercentValue = 0
            },
            ReturnItem = true,
            ReturnPercent = 100,
            BlockedList = new List<string>
            {
                "shortname 1",
                "shortname 2",
                "shortname 3"
            }
        };

        [JsonProperty(PropertyName = "Block Settings")]
        public readonly BlockSettings Block = new BlockSettings
        {
            UseNoEscape = true,
            UseClans = true,
            UseFriends = true,
            UseCupboard = true
        };

        [JsonProperty(PropertyName = "Additional Slot Settings")]
        public readonly AdditionalSlot AdditionalSlot = new AdditionalSlot
        {
            Enabled = true
        };

        [JsonProperty(PropertyName = "UI Settings")]
        public readonly InterfaceSettings UI = new InterfaceSettings
        {
            Color1 = new IColor("#4B68FF"),
            Color2 = new IColor("#2C2C2C"),
            Color3 = new IColor("#B64040"),
            OffsetY = 0,
            OffsetX = 0
        };
    }

    private class AdditionalSlot
    {
        [JsonProperty(PropertyName = "Enabled")]
        public bool Enabled;

/*        public static void Get(BasePlayer player)
        {
            var item = ItemManager.CreateByName("hammer");
            if (item == null) return;

            if (player.inventory.containerBelt.capacity < 7)
                player.inventory.containerBelt.capacity++;

            item.LockUnlock(true);
            item.MoveToContainer(player.inventory.containerBelt, 6);
        }*/

/*        public static void Remove(BasePlayer player)
        {
            var item = player.inventory.containerBelt.GetSlot(6);
            if (item == null || item.info.shortname != "hammer") return;

            item.RemoveFromContainer();
            item.Remove();

            ItemManager.DoRemoves();

            player.inventory.containerBelt.capacity--;
        }*/
    }

    private class InterfaceSettings
    {
        [JsonProperty(PropertyName = "Color 1")]
        public IColor Color1;

        [JsonProperty(PropertyName = "Color 2")]
        public IColor Color2;

        [JsonProperty(PropertyName = "Color 3")]
        public IColor Color3;

        [JsonProperty(PropertyName = "Offset Y")]
        public float OffsetY;

        [JsonProperty(PropertyName = "Offset X")]
        public float OffsetX;
    }

    private class IColor
    {
        [JsonProperty(PropertyName = "HEX")] public string Hex;

        [JsonProperty(PropertyName = "Opacity (0 - 100)")]
        public readonly float Alpha;

        [JsonProperty] private string _color;

        [JsonIgnore]
        public string Get
        {
            get
            {
                if (string.IsNullOrEmpty(_color))
                    _color = GetColor();

                return _color;
            }
        }

        private string GetColor()
        {
            if (string.IsNullOrEmpty(Hex)) Hex = "#FFFFFF";

            var str = Hex.Trim('#');
            if (str.Length != 6) throw new Exception(Hex);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

            return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
        }

        public IColor()
        {
        }

        public IColor(string hex, float alpha = 100)
        {
            Hex = hex;
            Alpha = alpha;
        }
    }

    private class ConditionSettings
    {
        [JsonProperty(PropertyName = "Default (from game)")]
        public bool Default;

        [JsonProperty(PropertyName = "Use percent?")]
        public bool Percent;

        [JsonProperty(PropertyName = "Percent (value)")]
        public float PercentValue;
    }

    private class BlockSettings
    {
        [JsonProperty(PropertyName = "Work with NoEscape?")]
        public bool UseNoEscape;

        [JsonProperty(PropertyName = "Work with Clans? (clan members will be able to delete/upgrade)")]
        public bool UseClans;

        [JsonProperty(PropertyName = "Work with Friends? (friends will be able to delete/upgrade)")]
        public bool UseFriends;

        [JsonProperty(PropertyName = "Can those authorized in the cupboard delete/upgrade?")]
        public bool UseCupboard;

        [JsonProperty(PropertyName = "Is an upgrade/remove cupbaord required?")]
        public bool NeedCupboard;
    }

    private abstract class TotalSettings
    {
        [JsonProperty(PropertyName = "Time of action")]
        public int ActionTime;

        [JsonProperty(PropertyName = "Cooldown (default | 0 - disable)")]
        public int Cooldown;

        [JsonProperty(PropertyName = "Cooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, int> VipCooldown;

        [JsonProperty(PropertyName = "Block After Wipe (default | 0 - disable)")]
        public int AfterWipe;

        [JsonProperty(PropertyName = "Block After Wipe", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, int> VipAfterWipe;

        public int GetCooldown(BasePlayer player)
        {
            return (from check in VipCooldown
                where player.IPlayer.HasPermission(check.Key)
                select check.Value).Prepend(Cooldown).Min();
        }

        public int GetWipeCooldown(BasePlayer player)
        {
            return (from check in VipAfterWipe
                where player.IPlayer.HasPermission(check.Key)
                select check.Value).Prepend(AfterWipe).Min();
        }
    }

    private class UpgradeSettings : TotalSettings
    {
    }

    private class RemoveSettings : TotalSettings
    {
        [JsonProperty(PropertyName = "Blocked items to remove (prefab)",
            ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> BlockedList;

        [JsonProperty(PropertyName = "Return Item")]
        public bool ReturnItem;

        [JsonProperty(PropertyName = "Returnable Item Percentage")]
        public float ReturnPercent;

        [JsonProperty(PropertyName = "Can friends remove? (Friends)")]
        public bool CanFriends;

        [JsonProperty(PropertyName = "Can clanmates remove? (Clans)")]
        public bool CanClan;

        [JsonProperty(PropertyName = "Can teammates remove?")]
        public bool CanTeams;

        [JsonProperty(PropertyName = "Require a cupboard")]
        public bool RequireCupboard;

        [JsonProperty(PropertyName = "Remove by cupboard? (those who are authorized in the cupboard can remove)")]
        public bool RemoveByCupboard;

        [JsonProperty(PropertyName = "Condition Settings")]
        public ConditionSettings Condition;
    }

    private class Mode
    {
        [JsonProperty(PropertyName = "Icon (assets/url)")]
        public string Icon;

        [JsonProperty(PropertyName = "Type (Remove/Wood/Stone/Metal/TopTier)")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Types Type;

        [JsonProperty(PropertyName = "Permission (ex: UPRemove.1)")]
        public string Permission;
    }

    protected override void LoadConfig()
    {
        base.LoadConfig();
        try
        {
            _config = Config.ReadObject<Configuration>();
            if (_config == null) throw new Exception();
            SaveConfig();
        }
        catch (Exception ex)
        {
            PrintError("Your configuration file contains an error. Using default configuration values.");
            LoadDefaultConfig();
            Debug.LogException(ex);
        }
    }

    protected override void SaveConfig()
    {
        Config.WriteObject(_config);
    }

    protected override void LoadDefaultConfig()
    {
        _config = new Configuration();
    }

    #endregion


    Dictionary<string, string> UPRemoveImages = new Dictionary<string, string>()
    {
        {"UPRemove.Wood",    "https://imgur.com/79AHR7v.png"},
        {"UPRemove.Stone",   "https://imgur.com/hZrZ1wP.png"},
        {"UPRemove.Metal",   "https://imgur.com/9M9sdqg.png"},
        {"UPRemove.TopTier", "https://imgur.com/Cu50ZmH.png"},
        {"UPRemove.Remove",  "https://imgur.com/IRJwxtZ.png"}
    };
    
    Dictionary<BuildingGrade.Enum, string> UPRemoveString = new Dictionary<BuildingGrade.Enum, string>()
    {
        {BuildingGrade.Enum.Wood,     "ДЕРЕВО"},
        {BuildingGrade.Enum.Stone,    "КАМЕНЬ"},
        {BuildingGrade.Enum.Metal,    "МЕТАЛЛ"},
        {BuildingGrade.Enum.TopTier,  "МВК"}
    };

    #region Data

    private PluginData _data;

    private void SaveData()
    {
        Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
    }

    private void LoadData()
    {
        try
        {
            _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
        }
        catch (Exception e)
        {
            PrintError(e.ToString());
        }

        if (_data == null) _data = new PluginData();
    }

    private class PluginData
    {
        [JsonProperty(PropertyName = "Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public readonly Dictionary<ulong, PlayerData> Players = new Dictionary<ulong, PlayerData>();
    }

    private class PlayerData
    {
        [JsonProperty(PropertyName = "Last Upgrade")]
        public DateTime LastUpgrade = new DateTime(1970, 1, 1, 0, 0, 0);

        [JsonProperty(PropertyName = "Last Remove")]
        public DateTime LastRemove = new DateTime(1970, 1, 1, 0, 0, 0);

        public int LeftTime(bool remove, int cooldown)
        {
            var time = remove
                ? LastRemove
                : LastUpgrade;
            return (int) time.AddSeconds(cooldown).Subtract(DateTime.UtcNow).TotalSeconds;
        }

        public bool HasCooldown(bool remove, int cooldown)
        {
            var time = remove
                ? LastRemove
                : LastUpgrade;

            return DateTime.UtcNow.Subtract(time).TotalSeconds < cooldown;
        }

        public static bool HasWipeCooldown(int cooldown)
        {
            return DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime.ToUniversalTime()).TotalSeconds < cooldown;
        }

        public static int WipeLeftTime(int cooldown)
        {
            return (int) SaveRestore.SaveCreatedTime.ToUniversalTime().AddSeconds(cooldown)
                .Subtract(DateTime.UtcNow)
                .TotalSeconds;
        }
    }

    private PlayerData GetPlayerData(ulong userId)
    {
        PlayerData playerData;
        if (!_data.Players.TryGetValue(userId, out playerData))
            _data.Players.Add(userId, playerData = new PlayerData());

        return playerData;
    }

    #endregion

    #region Hooks

    private void Init()
    {
        _instance = this;

        LoadData();

        RegisterPermissions();

        AddCovalenceCommand(_config.UpgradeCommands, nameof(CmdUpgrade));

        AddCovalenceCommand(_config.RemoveCommands, nameof(CmdRemove));

/*        if (!_config.AdditionalSlot.Enabled)
        {
            Unsubscribe(nameof(OnPlayerConnected));
            Unsubscribe(nameof(OnPlayerDisconnected));
            Unsubscribe(nameof(OnPlayerDeath));
            Unsubscribe(nameof(OnActiveItemChanged));
        }*/
    }

    private void OnServerInitialized()
    {
        LoadImages();

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        
        InitFileManager();
        CommunityEntity.ServerInstance.StartCoroutine(StoreImages());
    }

    private void Unload()
    {
        foreach (var player in BasePlayer.activePlayerList)
        {
            CuiHelper.DestroyUi(player, Layer);

          
                OnPlayerDisconnected(player, string.Empty);
        }

        Array.ForEach(_components.Values.ToArray(), build =>
        {
            if (build != null)
                build.Kill();
        });

        SaveData();

        _config = null;
        _instance = null;
    }

    private void OnPlayerConnected(BasePlayer player)
    {
        if (player == null || player.IsNpc) return;

        //AdditionalSlot.Get(player);
    }

    private void OnPlayerDisconnected(BasePlayer player, string reason)
    {
        if (player == null || player.IsNpc) return;

        //AdditionalSlot.Remove(player);
    }

    private void OnPlayerDeath(BasePlayer player, HitInfo info)
    {
        if (player == null || player.IsNpc) return;

        //AdditionalSlot.Remove(player);
    }

    private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
    {
        if (player == null || player.IsNpc) return;

        if (oldItem != null && oldItem.position == 6)
        {
            var build = GetBuild(player);
            if (build != null && build.activeByItem)
                build.Kill();
            return;
        }

        if (newItem != null && newItem.position == 6)
        {
            if (GetBuild(player) != null) return;

            AddOrGetBuild(player, true).GoNext();
        }
    }

    private object OnHammerHit(BasePlayer player, HitInfo info)
    {
        if (player == null || info == null) return null;

        var entity = info.HitEntity as BaseCombatEntity;
        if (entity == null || entity.OwnerID == 0) return null;

        var build = GetBuild(player);
        if (build == null) return null;

        var mode = build.GetMode();
        if (mode == null) return null;

        if (!player.CanBuild())
        {
            SendNotify(player, BuildingBlocked, 1);
            return true;
        }

        if (_config.Block.UseNoEscape && NoEscape != null && NoEscape.IsLoaded && IsRaidBlocked(player))
        {
            SendNotify(player, mode.Type == Types.Remove ? RemoveRaidBlocked : UpgradeRaidBlocked, 1);
            return true;
        }

        var cupboard = entity.GetBuildingPrivilege();
        if (cupboard == null && _config.Block.NeedCupboard)
        {
            SendNotify(player, CupboardRequired, 1);
            return true;
        }

        if (entity.OwnerID != player.userID) //NOT OWNER
        {
            var any =
                _config.Block.UseFriends && Friends != null && Friends.IsLoaded &&
                IsFriends(player.OwnerID, entity.OwnerID) ||
                _config.Block.UseClans && Clans != null && Clans.IsLoaded &&
                IsClanMember(player.OwnerID, entity.OwnerID) ||
                _config.Block.UseCupboard && (cupboard == null || cupboard.IsAuthed(player));

            if (!any)
            {
                SendNotify(player, mode.Type == Types.Remove ? CantRemove : CantUpgrade, 1);
                return true;
            }
        }

        if (mode.Type == Types.Remove)
        {
            if (_config.Remove.BlockedList.Contains(entity.name))
            {
                SendNotify(player, mode.Type == Types.Remove ? CantRemove : CantUpgrade, 1);
                return true;
            }
        }
        else
        {
            var block = entity as BuildingBlock;
            if (block != null && (int) block.grade >= (int) mode.Type) return true;
        }

        build.DoIt(entity);
        return true;
    }

    private void OnEntityBuilt(Planner plan, GameObject go)
    {
        var player = plan.GetOwnerPlayer();
        if (player == null) return;

        var block = go.ToBaseEntity() as BuildingBlock;
        if (block == null) return;

        var build = GetBuild(player);
        if (build == null) return;

        var mode = build.GetMode();
        if (mode == null || mode.Type == Types.Remove) return;

        build.DoIt(block);
    }

    #endregion

    #region Commands

    private void CmdRemove(IPlayer cov, string command, string[] args)
    {
        var player = cov.Object as BasePlayer;
        if (player == null) return;

        var mode = _config.Modes.Find(x => x.Type == Types.Remove);

        if (args.Length > 0 && args[0] == "all")
        {
            var cupboard = player.GetBuildingPrivilege();
            if (cupboard == null)
            {
                SendNotify(player, NoCupboard, 1);
                return;
            }

            var data = GetPlayerData(player.userID);

            var cooldown = _config.Remove.GetCooldown(player);
            if (cooldown > 0 && data.HasCooldown(false, cooldown))
            {
                SendNotify(player, RemoveCanThrough, 1,
                    data.LeftTime(false, cooldown));
                return;
            }

            var blockWipe = _config.Remove.GetWipeCooldown(player);
            if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
            {
                SendNotify(player, RemoveCanThrough, 1,
                    PlayerData.WipeLeftTime(blockWipe));
                return;
            }

            var entities = BaseNetworkable.serverEntities
                .OfType<BaseCombatEntity>()
                .Where(x => !(x is BasePlayer) && x.GetBuildingPrivilege() == cupboard)
                .ToList();
            if (entities.Count == 0 || entities.Any(x => !CanRemove(player, x)))
                return;

            Global.Runner.StartCoroutine(StartRemove(player, entities));

            SendNotify(player, SuccessfullyUpgrade, 0);
            return;
        }

        AddOrGetBuild(player).Init(mode);
    }

    private void CmdUpgrade(IPlayer cov, string command, string[] args)
    {
        var player = cov.Object as BasePlayer;
        if (player == null) return;

        if (args.Length == 0)
        {
            AddOrGetBuild(player).GoNext();
            return;
        }

        switch (args[0])
        {
            case "all":
            {
                if (!cov.HasPermission(PermAll))
                {
                    SendNotify(player, NoPermission, 1);
                    return;
                }

                Types upgradeType;
                if (args.Length < 2 || ParseType(args[1], out upgradeType) == Types.None)
                {
                    cov.Reply($"Error syntax! Use: /{command} {args[0]} [wood/stone/metal/toptier]");
                    return;
                }

                var cupboard = player.GetBuildingPrivilege();
                if (cupboard == null)
                {
                    SendNotify(player, NoCupboard, 1);
                    return;
                }

                if (!player.CanBuild())
                {
                    SendNotify(player, BuildingBlocked, 1);
                    return;
                }

                if (_config.Block.UseNoEscape && NoEscape != null && NoEscape.IsLoaded && IsRaidBlocked(player))
                {
                    SendNotify(player, UpgradeRaidBlocked, 1);
                    return;
                }

                var data = GetPlayerData(player.userID);

                var cooldown = _config.Upgrade.GetCooldown(player);
                if (cooldown > 0 && data.HasCooldown(false, cooldown))
                {
                    SendNotify(player, UpgradeCanThrough, 1,
                        data.LeftTime(false, cooldown));
                    return;
                }

                var blockWipe = _config.Upgrade.GetWipeCooldown(player);
                if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
                {
                    SendNotify(player, UpgradeCanThrough, 1,
                        PlayerData.WipeLeftTime(blockWipe));
                    return;
                }

                var grade = GetEnum(upgradeType);

                var buildingBlocks = BaseNetworkable.serverEntities
                    .OfType<BuildingBlock>()
                    .Where(x =>
                        x.GetBuildingPrivilege() == cupboard &&
                        x.grade <= grade &&
                        x.CanChangeToGrade(grade, player))
                    .ToList();
                if (buildingBlocks.Count == 0) return;

                if (!cov.HasPermission(PermFree))
                {
                    if (!CanAffordUpgrade(buildingBlocks, grade, player))
                    {
                        SendNotify(player, NotEnoughResources, 1);
                        return;
                    }

                    PayForUpgrade(buildingBlocks, grade, player);
                }

                Global.Runner.StartCoroutine(StartUpgrade(player, buildingBlocks, grade));

                SendNotify(player, SuccessfullyUpgrade, 0);
                break;
            }
            default:
            {
                Types type;
                if (ParseType(args[0], out type) != Types.None)
                {
                    var modes = GetPlayerModes(player);
                    if (modes == null) return;

                    var mode = modes.Find(x => x.Type == type);

                    var build = AddOrGetBuild(player);
                    build.Init(mode);
                }
                else
                {
                    AddOrGetBuild(player).GoNext();
                }

                break;
            }
        }
    }

    [ConsoleCommand("UI_Builder")]
    private void CmdConsoleBuilding(ConsoleSystem.Arg arg)
    {
        var player = arg.Player();
        if (player == null || !arg.HasArgs()) return;

        switch (arg.Args[0])
        {
            case "mode":
            {
                int index;
                if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out index)) return;

                var mode = GetPlayerModes(player)[index];
                if (mode == null) return;

                AddOrGetBuild(player)?.Init(mode);
                break;
            }

            case "close":
            {
                GetBuild(player)?.Kill();
                break;
            }
        }
    }

    #endregion

    #region Component

    private readonly Dictionary<BasePlayer, BuildComponent> _components =
        new Dictionary<BasePlayer, BuildComponent>();

    private BuildComponent GetBuild(BasePlayer player)
    {
        BuildComponent build;
        return _components.TryGetValue(player, out build) ? build : null;
    }

    private BuildComponent AddOrGetBuild(BasePlayer player, bool item = false)
    {
        BuildComponent build;
        if (_components.TryGetValue(player, out build))
            return build;

        build = player.gameObject.AddComponent<BuildComponent>();
        build.activeByItem = item;
        return build;
    }

    private class BuildComponent : FacepunchBehaviour
    {
        #region Fields

        private BasePlayer _player;

        private Mode _mode;

        private float _startTime;

        private readonly CuiElementContainer _container = new CuiElementContainer();

        private bool _started = true;

        private float _cooldown;

        public bool activeByItem;

        #endregion

        #region Init

        private void Awake()
        {
            _player = GetComponent<BasePlayer>();

            _instance._components[_player] = this;

            enabled = false;
        }

        public void Init(Mode mode)
        {
            if (mode == null)
                mode = GetPlayerModes(_player).FirstOrDefault();

            _mode = mode;

            _startTime = Time.time;

            _cooldown = GetCooldown();

            MainUi();
            //DDrawUI();

            enabled = true;

            _started = true;
        }

        #endregion

        #region Interface

        /*public void DDrawUI()
        {               
            _container.Clear();

            _container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.272397 0.009259259", AnchorMax = "0.3348944 0.1203704" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);
            
            _container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = _instance.UPRemoveImages[$"building.upgrade.{_mode.Type}"] },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.DestroyUi(_player, Layer);
            CuiHelper.AddUi(_player, _container);
        }*/

        public void MainUi()
        {
            _container.Clear();

            _container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.00718534 0.387963", AnchorMax = "0.1427083 0.4375001" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);
            
            _container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Panel",
                Components =
                {
                    new CuiRawImageComponent { Png = _instance.UPRemoveImages[$"UPRemove.{_mode.Type}"] },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.DestroyUi(_player, Layer);
            CuiHelper.AddUi(_player, _container);
        }

        private void UpdateUi()
        {
            _container.Clear();

            _container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.07083333 0.4027778", AnchorMax = "0.1843693 0.4314461" },
                Image = { Color = "0 0 0 0" }
            },  "Overlay", LayerUpdate);

            var TextUI = _mode.Type == Types.Remove ? $"<size=12>{GetLeftTime()}СЕК</size>" : $"<size=12>{GetLeftTime()}СЕК</size>";
            
            _container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"{TextUI}", Align = TextAnchor.LowerCenter, Font = "robotocondensed-bold.ttf" }
            }, LayerUpdate);

            CuiHelper.DestroyUi(_player, LayerUpdate);
            CuiHelper.AddUi(_player, _container);
        }

        #endregion

        #region Update

        private void FixedUpdate()
        {
            if (!_started) return;

            var timeLeft = Time.time - _startTime;
            if (timeLeft > _cooldown)
            {
                Kill();
                return;
            }

            UpdateUi();
        }

        #endregion

        #region Main

        public void DoIt(BaseCombatEntity entity)
        {
            if (entity == null) return;

            switch (_mode.Type)
            {
                case Types.Remove:
                {
                    if (!CanRemove(_player, entity))
                        return;

                    var data = _instance.GetPlayerData(_player.userID);

                    var cooldown = _config.Remove.GetCooldown(_player);
                    if (cooldown > 0 && data.HasCooldown(true, cooldown))
                    {
                        _instance.SendNotify(_player, RemoveCanThrough, 1,
                            data.LeftTime(true, cooldown));
                        return;
                    }

                    var blockWipe = _config.Remove.GetWipeCooldown(_player);
                    if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
                    {
                        _instance.SendNotify(_player, RemoveCanThrough, 1,
                            PlayerData.WipeLeftTime(blockWipe));
                        return;
                    }

                    RemoveEntity(_player, entity);

                    data.LastRemove = DateTime.UtcNow;
                    break;
                }
                default:
                {
                    var block = entity as BuildingBlock;
                    if (block == null) return;

                    var data = _instance.GetPlayerData(_player.userID);

                    var cooldown = _config.Upgrade.GetCooldown(_player);
                    if (cooldown > 0 && data.HasCooldown(false, cooldown))
                    {
                        _instance.SendNotify(_player, UpgradeCanThrough, 1,
                            data.LeftTime(false, cooldown));
                        return;
                    }

                    var blockWipe = _config.Upgrade.GetWipeCooldown(_player);
                    if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
                    {
                        _instance.SendNotify(_player, UpgradeCanThrough, 1,
                            PlayerData.WipeLeftTime(blockWipe));
                        return;
                    }

                    var enumGrade = GetEnum(_mode.Type);

                    var grade = block.GetGrade(enumGrade);
                    if (grade == null || !block.CanChangeToGrade(enumGrade, _player) ||
                        Interface.CallHook("OnStructureUpgrade", block, _player, enumGrade) != null ||
                        block.SecondsSinceAttacked < 30.0)
                        return;

                    if (!_player.IPlayer.HasPermission(PermFree))
                    {
                        if (!block.CanAffordUpgrade(enumGrade, _player))
                        {
                            _instance.SendNotify(_player, NotEnoughResources, 0);
                            return;
                        }

                        block.PayForUpgrade(grade, _player);
                    }

                    UpgradeBuildingBlock(block, enumGrade);

                    Effect.server.Run(
                        "assets/bundled/prefabs/fx/build/promote_" + enumGrade.ToString().ToLower() + ".prefab",
                        block,
                        0U, Vector3.zero, Vector3.zero);

                    data.LastUpgrade = DateTime.UtcNow;
                    break;
                }
            }

            _startTime = Time.time;
        }

        #endregion

        #region Utils

        private int GetLeftTime()
        {
            return Mathf.RoundToInt(_startTime + _cooldown - Time.time);
        }

        public void GoNext()
        {
            var modes = GetPlayerModes(_player);
            if (modes == null) return;

            if (_mode == null)
            {
                _mode = modes.FindAll(x => x.Type != Types.Remove).FirstOrDefault();
                Init(_mode);
                return;
            }

            var i = 0;
            for (; i < modes.Count; i++)
            {
                var mode = modes[i];

                if (mode == _mode)
                    break;
            }

            i++;

            var nextMode = modes.Count <= i ? modes[0] : modes[i];

            _mode = nextMode;

            Init(nextMode);
        }

        public Mode GetMode()
        {
            return _mode;
        }

        private float GetCooldown()
        {
            switch (_mode.Type)
            {
                case Types.Remove:
                    return _config.Remove.ActionTime;
                default:
                    return _config.Upgrade.ActionTime;
            }
        }

        #endregion

        #region Destroy

        private void OnDestroy()
        {
            CancelInvoke();

            CuiHelper.DestroyUi(_player, Layer);
            CuiHelper.DestroyUi(_player, LayerUpdate);

            _instance?._components.Remove(_player);

            Destroy(this);
        }

        public void Kill()
        {
            enabled = false;

            _started = false;

            DestroyImmediate(this);
        }

        #endregion
    }

    #endregion

    #region Utils

    private void RegisterPermissions()
    {
        permission.RegisterPermission(PermAll, this);

        permission.RegisterPermission(PermFree, this);

        _config.Modes.ForEach(mode =>
        {
            if (!string.IsNullOrEmpty(modepermission) && !permission.PermissionExists(modepermission))
                permission.RegisterPermission(modepermission, this);
        });
    }

    private void LoadImages()
    {
        if (ImageLibrary == null || !ImageLibrary.IsLoaded)
        {
            PrintError("IMAGE LIBRARY IS NOT INSTALLED!");
        }
        else
        {
            var imagesList = new Dictionary<string, string>();

            _config.Modes.FindAll(mode => !mode.Icon.Contains("assets/icon")).ForEach(mode =>
            {
                if (!string.IsNullOrEmpty(mode.Icon) && !imagesList.ContainsKey(mode.Icon))
                    imagesList.Add(mode.Icon, mode.Icon);
            });

            ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
        }
    }

    private static string FixNames(string name)
    {
        switch (name)
        {
            case "wall.external.high.wood": return "wall.external.high";
            case "electric.windmill.small": return "generator.wind.scrap";
            case "graveyardfence": return "wall.graveyard.fence";
            case "coffinstorage": return "coffin.storage";
        }

        return name;
    }

    private static List<Mode> GetPlayerModes(BasePlayer player)
    {
        return _config.Modes.FindAll(x =>
            string.IsNullOrEmpty(x.Permission) || player.IPlayer.HasPermission(x.Permission));
    }

    private bool IsRaidBlocked(BasePlayer player)
    {
        return Convert.ToBoolean(NoEscape?.Call("IsRaidBlocked", player));
    }

    private bool IsClanMember(ulong playerID, ulong targetID)
    {
        return Convert.ToBoolean(Clans?.Call("HasFriend", playerID, targetID));
    }

    private bool IsFriends(ulong playerID, ulong friendId)
    {
        return Convert.ToBoolean(Friends?.Call("HasFriend", playerID, friendId));
    }

    private static BuildingGrade.Enum GetEnum(Types type)
    {
        switch (type)
        {
            case Types.Wood:
                return BuildingGrade.Enum.Wood;
            case Types.Stone:
                return BuildingGrade.Enum.Stone;
            case Types.Metal:
                return BuildingGrade.Enum.Metal;
            case Types.TopTier:
                return BuildingGrade.Enum.TopTier;
            default:
                return BuildingGrade.Enum.None;
        }
    }

    private static void RemoveEntity(BasePlayer player, BaseCombatEntity entity)
    {
        if (_config.Remove.ReturnItem)
            GiveRefund(entity, player);

        entity.Kill();
    }

    private static bool CanRemove(BasePlayer player, BaseEntity entity)
    {
        if (entity.OwnerID == 0)
        {
            _instance.SendNotify(player, CantRemove, 1);
            return false;
        }

        var storageContainer = entity.GetComponent<StorageContainer>();
        if (storageContainer != null && storageContainer.inventory.itemList.Count > 1)
        {
            _instance.SendNotify(player, CRStorageNotEmpty, 1);
            return false;
        }

        var combat = entity.GetComponent<BaseCombatEntity>();
        if (combat != null && combat.SecondsSinceAttacked < 30f)
        {
            _instance.SendNotify(player, CRDamaged, 1);
            return false;
        }

        if (Interface.Call("CanRemove", player, entity) != null)
        {
            _instance.SendNotify(player, CRBeBlocked, 1);
            return false;
        }

        if (_config.Block.NeedCupboard && entity.GetBuildingPrivilege() == null)
        {
            _instance.SendNotify(player, CRBuildingBlock, 1);
            return false;
        }

        if (_config.Block.UseNoEscape && _instance.NoEscape != null && _instance.NoEscape.IsLoaded &&
            _instance.IsRaidBlocked(player))
        {
            _instance.SendNotify(player, RemoveRaidBlocked, 1);
            return false;
        }

        if (player.userID != entity.OwnerID)
        {
            if (_config.Remove.RemoveByCupboard)
                return true;

            if (_config.Remove.CanClan && _instance.IsClanMember(player.userID, entity.OwnerID)) return true;

            if (_config.Remove.CanFriends && _instance.IsFriends(player.userID, entity.OwnerID)) return true;

            _instance.SendNotify(player, CRNotAccess, 1);
            return false;
        }

        return true;
    }

    private static void GiveRefund(BaseCombatEntity entity, BasePlayer player)
    {
        var shortPrefabName = entity.ShortPrefabName;
        shortPrefabName = Regex.Replace(shortPrefabName, "\\.deployed|_deployed", "");
        shortPrefabName = FixNames(shortPrefabName);

        var item = ItemManager.CreateByName(shortPrefabName);
        if (item != null)
        {
            HandleCondition(ref item, player, entity);

            player.inventory.GiveItem(item);
            return;
        }

        entity.BuildCost()?.ForEach(value =>
        {
            var amount = _config.Remove.ReturnPercent < 100
                ? Convert.ToInt32(value.amount * (_config.Remove.ReturnPercent / 100f))
                : Convert.ToInt32(value.amount);

            var x = ItemManager.Create(value.itemDef, amount);
            if (x == null) return;

            HandleCondition(ref x, player, entity);

            player.GiveItem(x);
        });
    }

    private static void HandleCondition(ref Item item, BasePlayer player, BaseCombatEntity entity)
    {
        if (_config.Remove.Condition.Default)
        {
            if (entity.pickup.setConditionFromHealth && item.hasCondition)
                item.conditionNormalized = Mathf.Clamp01(entity.healthFraction - entity.pickup.subtractCondition);
            //entity.OnPickedUpPreItemMove(item, player);
        }

        if (_config.Remove.Condition.Percent)
            item.LoseCondition(item.maxCondition * (_config.Remove.Condition.PercentValue / 100f));
    }

    private static void UpgradeBuildingBlock(BuildingBlock block, BuildingGrade.Enum @enum)
    {
        if (block == null || block.IsDestroyed) return;

        block.SetGrade(@enum);
        block.SetHealthToMax();
        block.StartBeingRotatable();
        block.SendNetworkUpdate();
        block.UpdateSkin();
        block.ResetUpkeepTime();
        block.UpdateSurroundingEntities();
        BuildingManager.server.GetBuilding(block.buildingID)?.Dirty();
    }

    private bool CanAffordUpgrade(List<BuildingBlock> blocks, BuildingGrade.Enum @enum, BasePlayer player)
    {
        return blocks.All(block => block.GetGrade(@enum).costToBuild.All(itemAmount =>
            player.inventory.GetAmount(itemAmount.itemid) >= itemAmount.amount));
    }

    private static void PayForUpgrade(List<BuildingBlock> blocks, BuildingGrade.Enum @enum, BasePlayer player)
    {
        var collect = new List<Item>();

        blocks.ForEach(block => block.GetGrade(@enum).costToBuild.ForEach(itemAmount =>
        {
            player.inventory.Take(collect, itemAmount.itemid, (int) itemAmount.amount);
            player.Command("note.inv " + itemAmount.itemid + " " + (float) ((int) itemAmount.amount * -1.0));
        }));

        foreach (var obj in collect)
            obj.Remove();
    }

    private IEnumerator StartUpgrade(BasePlayer player, List<BuildingBlock> blocks, BuildingGrade.Enum @enum)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null || block.IsDestroyed) continue;

            UpgradeBuildingBlock(block, @enum);

            if (i % 10 == 0) yield return CoroutineEx.waitForFixedUpdate;
        }
    }

    private IEnumerator StartRemove(BasePlayer player, List<BaseCombatEntity> entities)
    {
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity == null || entity.IsDestroyed) continue;

            RemoveEntity(player, entity);

            if (i % 10 == 0)
                yield return CoroutineEx.waitForFixedUpdate;
        }
    }

    private static Types ParseType(string arg, out Types type)
    {
        Types upgradeType;
        if (Enum.TryParse(arg, true, out upgradeType))
        {
            type = upgradeType;
            return type;
        }

        int value;
        if (int.TryParse(arg, out value) && value > 0 && value < 6)
        {
            type = (Types) value;
            return type;
        }

        type = Types.None;
        return type;
    }

    #endregion

    #region Lang

    private const string
        CRNotAccess = "CRNotAccess",
        CRBuildingBlock = "CRBuildingBlock",
        CRBeBlocked = "CRBeBlocked",
        CRStorageNotEmpty = "CRStorageNotEmpty",
        CRDamaged = "CRDamaged",
        SuccessfullyRemove = "SuccessfullyRemove",
        CloseMenu = "CloseMenu",
        UpgradeTitle = "UpgradeTitle",
        RemoveTitle = "RemoveTitle",
        UpgradeCanThrough = "UpgradeCanThrough",
        RemoveCanThrough = "RemoveCanThrough",
        NoPermission = "NoPermission",
        SuccessfullyUpgrade = "SuccessfullyUpgrade",
        NoCupboard = "NoCupboard",
        CupboardRequired = "CupboardRequired",
        RemoveRaidBlocked = "RemoveRaidBlocked",
        UpgradeRaidBlocked = "UpgradeRaidBlocked",
        BuildingBlocked = "BuildingBlocked",
        CantUpgrade = "CantUpgrade",
        CantRemove = "CantRemove",
        NotEnoughResources = "NotEnoughResources";

    protected override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            [NotEnoughResources] = "Not enough resources to upgrade!",
            [CantRemove] = "You can remove this entity.",
            [CantUpgrade] = "You cannot upgrade this entity.",
            [BuildingBlocked] = "You are building blocked",
            [UpgradeRaidBlocked] = "You cannot upgrade buildings <color=#81B67A>during a raid!</color>!",
            [RemoveRaidBlocked] = "You cannot upgrade or remove <color=#81B67A>during a raid!</color>!",
            [CupboardRequired] = "A Cupboard is required!",
            [NoCupboard] = "No cupboard found!",
            [SuccessfullyUpgrade] = "You have successfully upgraded a building",
            [NoPermission] = "Нету такого типа улучшение!",
            [UpgradeCanThrough] = "You can upgrade the building in: {0}s",
            [RemoveCanThrough] = "You can remove the building in: {0}s",
            [RemoveTitle] = "Remove in <color=white>{0}s</color>",
            [UpgradeTitle] = "Upgrade to {0} <color=white>{1}s</color>",
            [CloseMenu] = "✕",
            [SuccessfullyRemove] = "You have successfully removed a building",
            [CRDamaged] = "Can't remove: Server has disabled damaged objects from being removed.",
            [CRStorageNotEmpty] = "Can't remove: The entity storage is not empty.",
            [CRBeBlocked] = "Can't remove: An external plugin blocked the usage.",
            [CRBuildingBlock] = "Can't remove: Missing cupboard",
            [CRNotAccess] = "Can't remove: You don't have any rights to remove this.",
            ["Wood"] = "wood",
            ["Stone"] = "stone",
            ["Metal"] = "metal",
            ["TopTier"] = "HQM"
        }, this);
    }

    private string Msg(string key, string userid = null, params object[] obj)
    {
        return string.Format(lang.GetMessage(key, this, userid), obj);
    }

    private string Msg(BasePlayer player, string key, params object[] obj)
    {
        return string.Format(lang.GetMessage(key, this, player.UserIDString), obj);
    }

    private void Reply(BasePlayer player, string key, params object[] obj)
    {
        player.ChatMessage(Msg(player, key, obj));
    }

    private void SendNotify(BasePlayer player, string key, int type, params object[] obj)
    {
        if (_config.UseNotify && (Notify != null || UINotify != null))
            Interface.Oxide.CallHook("SendNotify", player, type, Msg(player, key, obj));
        else
            Reply(player, key, obj);
    }

    #endregion
    
    bool loaded = false;
    
    IEnumerator StoreImages()
    {
        foreach (var img in UPRemoveImages)
        {
            yield return m_FileManager.LoadFile( img.Key, img.Value );
        }
        
        var keys = UPRemoveImages.Keys.ToList();
        foreach (string t in keys)
        {
            UPRemoveImages[t] = m_FileManager.GetPng( t );
        }
        PrintWarning($"Картинки загружены: {string.Join(", ", UPRemoveImages.Values.ToArray())}");
        loaded = true;
    }
    
    private GameObject FileManagerObject;
    private FileManager m_FileManager;
    
    void InitFileManager()
    {
        FileManagerObject = new GameObject( "MAP_FileManagerObject" );
        m_FileManager = FileManagerObject.AddComponent<FileManager>();
    }
    class FileManager : MonoBehaviour
    {
        int loaded = 0;
        int needed = 0;

        public bool IsFinished => needed == loaded;
        const ulong MaxActiveLoads = 10;
        Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

        DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile( "UPRemove" );

        private class FileInfo
        {
            public string Url;
            public string Png;
        }

        public void SaveData()
        {
            dataFile.WriteObject( files );
        }

        public string GetPng( string name ) => files[ name ].Png;

        private void Awake()
        {
            files = dataFile.ReadObject<Dictionary<string, FileInfo>>() ?? new Dictionary<string, FileInfo>();
        }

        public IEnumerator LoadFile( string name, string url, int size = -1 )
        {
            if (files.ContainsKey( name ) && files[ name ].Url == url && !string.IsNullOrEmpty( files[ name ].Png )) yield break;
            files[ name ] = new FileInfo() { Url = url };
            needed++;
            yield return StartCoroutine( LoadImageCoroutine( name, url, size ) );
        }

        IEnumerator LoadImageCoroutine( string name, string url, int size = -1 )
        {
            using (WWW www = new WWW( url ))
            {
                yield return www;
                using (MemoryStream stream = new MemoryStream())
                {
                    if (string.IsNullOrEmpty( www.error ))
                    {
                        stream.Position = 0;
                        stream.SetLength( 0 );
                        var bytes = size == -1 ? www.bytes : Resize( www.bytes, size );
                        stream.Write( bytes, 0, bytes.Length );

                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId ).ToString();
                        files[ name ].Png = crc32;
                    }
                }
            }
            loaded++;
        }

        static byte[] Resize( byte[] bytes, int size )
        {
            Image img = (Bitmap) ( new ImageConverter().ConvertFrom( bytes ) );
            Bitmap cutPiece = new Bitmap( size, size );
            System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage( cutPiece );
            graphic.DrawImage( img, new Rectangle( 0, 0, size, size ), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel );
            graphic.Dispose();
            MemoryStream ms = new MemoryStream();
            cutPiece.Save( ms, ImageFormat.Jpeg );
            return ms.ToArray();
        }
    }
}
}