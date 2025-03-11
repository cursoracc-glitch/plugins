using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Timed Permissions", "LaserHydra", "1.4.0", ResourceId = 1926)]
    [Description("Allows you to grant permissions or groups for a specific time")]
    internal class TimedPermissions : CovalencePlugin
    {
        [PluginReference] Plugin XMenu;
        private const string AdminPermission = "timedpermissions.use";
        private const string AdvancedAdminPermission = "timedpermissions.advanced";

        private static TimedPermissions _instance;
        private static List<Player> _players = new List<Player>();

        private Configuration _config;
        
        #region Hooks & Loading

        private void Loaded()
        {
            _instance = this;

            MigrateData();

            LoadData(ref _players);

            if (_players == null)
            {
                _players = new List<Player>();
                SaveData(_players);
            }
        }

        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion

        public Dictionary<string, string> NicePerms = new Dictionary<string, string>()
        {
            ["blueprintmanager.all"] = "Это название я сам настроил"
        };

        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            NicePerms = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("NicePerms");
            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "Привилегии", "RenderPermissions", null);
                    TimerInitialize.Destroy();
                }
            });
        }

        private void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("NicePerms", NicePerms);
        }

        private void RenderPermissions(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#0000007f"),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -460",
                            OffsetMax = "630 -10"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Title",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#1d71ff>Список ваших привилегий</color>",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 24,
                                Font = "robotocondensed-bold.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.9",
                                AnchorMax = "0.975 0.975",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });
            Player player = Player.Get(userID.ToString());
            if (player != null)
            {
                int j = 1;
                for (int i = 0; i < player.Groups.Count; i++)
                {
                    if (!player.Groups.ElementAt(i).Expired)
                    { 
                        Container.Add(new CuiElement
                        {
                            Parent = MenuContent + ".Info",
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"{j}. <color=#1d71ff>" + (NicePerms.ContainsKey(player.Groups.ElementAt(i).Value) ? NicePerms[player.Groups.ElementAt(i).Value] : player.Groups.ElementAt(i).Value) + "</color> до " + player.Groups.ElementAt(i).ExpireDate.ToString("dd/MM/yyyy HH:mm"),
                                    Align = TextAnchor.MiddleLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-bold.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"30 {-50 - j * 25}",
                                    OffsetMax = $"550 {-25 - j * 25}"
                                }
                            }
                        });
                        j++;
                    }
                }

                for (int i = 0; i < player.Permissions.Count; i++)
                {
                    if (!player.Permissions.ElementAt(i).Expired)
                    {
                        Container.Add(new CuiElement
                        {
                            Parent = MenuContent + ".Info",
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"{j}. <color=#90BD47>" + (NicePerms.ContainsKey(player.Permissions.ElementAt(i).Value) ? NicePerms[player.Permissions.ElementAt(i).Value] : player.Permissions.ElementAt(i).Value) + "</color> до " + player.Permissions.ElementAt(i).ExpireDate.ToString("dd/MM/yyyy HH:mm"),
                                    Align = TextAnchor.MiddleLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-bold.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"30 {-50 - j * 25}",
                                    OffsetMax = $"550 {-25 - j * 25}"
                                }
                            }
                        });
                        j++;
                    }
                }
            }
        }

        private static string HexToRustFormat(string hex)
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

        private void OnNewSave(string filename)
        {
            LoadConfig(); // Ensure config is loaded at this point

            if (_config.WipeDataOnNewSave)
            {
                string backupFileName;
                ResetAllAccess(out backupFileName);

                PrintWarning($"New save file detected: all groups and permissions revoked and data cleared. Backup created at {backupFileName}");
            }
        }

        private void MigrateData()
        {
            List<JObject> data = new List<JObject>();
            LoadData(ref data);

            if (data == null)
                return;

            foreach (JObject playerData in data)
            {
                if (playerData["permissions"] != null)
                {
                    JArray permissions = (JArray) playerData["permissions"];
                    
                    foreach (JObject obj in permissions)
                    {
                        if (obj["permission"] != null)
                        {
                            obj["Value"] = obj["permission"]; 
                            obj.Remove("permission");
                        }

                        if (obj["_expireDate"] != null)
                        {
                            string expireDate = obj["_expireDate"].Value<string>();
                            
                            int[] date = (from val in expireDate.Split('/') select Convert.ToInt32(val)).ToArray(); 
                            obj["ExpireDate"] = new DateTime(date[4], date[3], date[2], date[1], date[0], 0);

                            obj.Remove("_expireDate");
                        }
                    }
                    
                    playerData["Permissions"] = permissions;
                    playerData.Remove("permissions");
                }

                if (playerData["groups"] != null)
                {
                    JArray permissions = (JArray)playerData["groups"];
                    
                    foreach (JObject obj in permissions)
                    {
                        if (obj["group"] != null)
                        {
                            obj["Value"] = obj["group"];
                            obj.Remove("group");
                        }

                        if (obj["_expireDate"] != null)
                        {
                            string expireDate = obj["_expireDate"].Value<string>();

                            int[] date = (from val in expireDate.Split('/') select Convert.ToInt32(val)).ToArray();
                            obj["ExpireDate"] = new DateTime(date[4], date[3], date[2], date[1], date[0], 0);

                            obj.Remove("_expireDate"); 
                        }
                    }

                    playerData["Groups"] = permissions;
                    playerData.Remove("groups");
                }

                if (playerData["steamID"] != null)
                {
                    playerData["Id"] = playerData["steamID"];
                    playerData.Remove("steamID");
                }

                if (playerData["name"] != null)
                {
                    playerData["Name"] = playerData["name"];
                    playerData.Remove("name");
                }
            }

            SaveData(data);
        }

        #endregion

        #region Commands

        [Command("pinfo")]
        private void CmdPlayerInfo(IPlayer player, string cmd, string[] args)
        {
            IPlayer target;

            if (args.Length == 0 || !player.HasPermission(AdminPermission))
                target = player;
            else
                target = FindPlayer(args[0], player);

            if (target == null)
                return;

            var pl = Player.Get(target.Id);

            if (pl == null)
                player.Reply(GetMessage("Player Has No Info", player.Id));
            else
            {
                string msg = GetMessage("Player Info", player.Id);

                msg = msg.Replace("{player}", $"{pl.Name} ({pl.Id})");
                msg = msg.Replace("{groups}", string.Join(", ", (from g in pl.Groups select $"{g.Value} until {g.ExpireDate.ToLongDateString() + " " + g.ExpireDate.ToShortTimeString()} UTC").ToArray()));
                msg = msg.Replace("{permissions}", string.Join(", ", (from p in pl.Permissions select $"{p.Value} until {p.ExpireDate.ToLongDateString() + " " + p.ExpireDate.ToShortTimeString()} UTC").ToArray()));

                player.Reply(msg);
            }
        }

        [Command("revokeperm"), Permission(AdminPermission)]
        private void CmdRevokePerm(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 2)
            {
                player.Reply($"Syntax: {(player.LastCommand == CommandType.Console ? string.Empty : "/")}revokeperm <player|steamid> <permission>");
                return;
            }

            IPlayer target = FindPlayer(args[0], player);

            if (target == null)
                return;

            Player pl = Player.Get(target.Id);
            
            if (pl == null || !pl.Permissions.Any(p => p.Value == args[1].ToLower()))
            {
                player.Reply(GetMessage("User Doesn't Have Permission", player.Id).Replace("{target}", target.Name).Replace("{permission}", args[1].ToLower()));
                return;
            }

            pl.RemovePermission(args[1].ToLower());
        }

        [Command("grantperm"), Permission(AdminPermission)]
        private void CmdGrantPerm(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 3)
            {
                player.Reply($"Syntax: {(player.LastCommand == CommandType.Console ? string.Empty : "/")}grantperm <player|steamid> <permission> <time Ex: 1d12h30m>");
                return;
            }

            IPlayer target = FindPlayer(args[0], player);
            TimeSpan duration;

            if (target == null)
                return;

            if (!TryParseTimeSpan(args[2], out duration))
            {
                player.Reply(GetMessage("Invalid Time Format", player.Id));
                return;
            }

            Player.GetOrCreate(target).AddPermission(args[1].ToLower(), DateTime.UtcNow + duration);
        }

        [Command("removegroup"), Permission(AdminPermission)]
        private void CmdRemoveGroup(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 2)
            {
                player.Reply($"Syntax: {(player.LastCommand == CommandType.Console ? string.Empty : "/")}removegroup <player|steamid> <group>");
                return;
            }

            IPlayer target = FindPlayer(args[0], player);

            if (target == null)
                return;

            Player pl = Player.Get(target.Id);

            if (pl == null || !pl.Groups.Any(p => p.Value == args[1].ToLower()))
            {
                player.Reply(GetMessage("User Isn't In Group", player.Id).Replace("{target}", target.Name).Replace("{group}", args[1].ToLower()));
                return;
            }

            pl.RemoveGroup(args[1].ToLower());
        }

        [Command("addgroup"), Permission(AdminPermission)]
        private void CmdAddGroup(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 3)
            {
                player.Reply($"Syntax: {(player.LastCommand == CommandType.Console ? string.Empty : "/")}addgroup <player|steamid> <group> <time Ex: 1d12h30m>");
                return;
            }

            IPlayer target = FindPlayer(args[0], player);
            TimeSpan duration;

            if (target == null)
                return;

            if (!TryParseTimeSpan(args[2], out duration))
            {
                player.Reply(GetMessage("Invalid Time Format", player.Id));
                return;
            }

            Player.GetOrCreate(target).AddGroup(args[1], DateTime.UtcNow + duration);
        }

        [Command("timedpermissions_resetaccess"), Permission(AdvancedAdminPermission)]
        private void CmdResetAccess(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 1 || !args[0].Equals("yes", StringComparison.InvariantCultureIgnoreCase))
            {
                player.Reply("Syntax: timedpermissions_resetaccess [yes]");
                player.Reply("This command will reset all access data and create a backup. Please confirm by calling the command with 'yes' as parameter");

                return;
            }

            string backupFileName;
            ResetAllAccess(out backupFileName);

            player.Reply($"All groups and permissions revoked and data cleared. Backup created at {backupFileName}");
        }

        [Command("timedpermissions_ensureaccess"), Permission(AdvancedAdminPermission)]
        private void CmdEnsureAccess(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 1 || !args[0].Equals("yes", StringComparison.InvariantCultureIgnoreCase))
            {
                player.Reply("Syntax: timedpermissions_ensureaccess [yes]");
                player.Reply("This command will ensure every player has their permissions and groups assigned. Please confirm by calling the command with 'yes' as parameter");

                return;
            }

            foreach (Player playerInformation in _players)
                playerInformation.EnsureAllAccess();

            player.Reply("All players were ensured to have their permissions and groups assigned.");
        }

        #endregion

        #region Helper Methods

        private void ResetAllAccess(out string backupFileName)
        {
            backupFileName = $"{nameof(TimedPermissions)}_Backups/{DateTime.UtcNow.Date:yyyy-MM-dd}_{DateTime.UtcNow:T}";
            SaveData(_players, backupFileName); // create backup of current data

            foreach (Player playerInformation in _players)
                playerInformation.RemoveAllAccess();

            _players = new List<Player>();
            SaveData(_players);
        }

        #region Time Helper

        private bool TryParseTimeSpan(string source, out TimeSpan date)
        {
            int minutes = 0;
            int hours = 0;
            int days = 0;

            Match m = new Regex(@"(\d+?)m", RegexOptions.IgnoreCase).Match(source);
            Match h = new Regex(@"(\d+?)h", RegexOptions.IgnoreCase).Match(source);
            Match d = new Regex(@"(\d+?)d", RegexOptions.IgnoreCase).Match(source);

            if (m.Success)
                minutes = Convert.ToInt32(m.Groups[1].ToString());

            if (h.Success)
                hours = Convert.ToInt32(h.Groups[1].ToString());

            if (d.Success)
                days = Convert.ToInt32(d.Groups[1].ToString());

            source = source.Replace(minutes + "m", string.Empty);
            source = source.Replace(hours + "h", string.Empty);
            source = source.Replace(days + "d", string.Empty);

            if (!string.IsNullOrEmpty(source) || (!m.Success && !h.Success && !d.Success))
            {
                date = default(TimeSpan);
                return false;
            }

            date = new TimeSpan(days, hours, minutes, 0);
            return true;
        }

        #endregion

        #region Finding Helper

        private IPlayer FindPlayer(string nameOrId, IPlayer player)
        {
            if (IsConvertibleTo<ulong>(nameOrId) && nameOrId.StartsWith("7656119") && nameOrId.Length == 17)
            {
                IPlayer result = players.All.ToList().Find(p => p.Id == nameOrId);

                if (result == null)
                    player.Reply($"Could not find player with ID '{nameOrId}'");

                return result;
            }

            List<IPlayer> foundPlayers = new List<IPlayer>();

            foreach (IPlayer current in players.Connected)
            {
                if (string.Equals(current.Name, nameOrId, StringComparison.CurrentCultureIgnoreCase))
                    return current;

                if (current.Name.ToLower().Contains(nameOrId.ToLower()))
                    foundPlayers.Add(current);
            }

            switch (foundPlayers.Count)
            {
                case 0:
                    player.Reply($"Could not find player with name '{nameOrId}'");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    string[] names = (from current in foundPlayers select current.Name).ToArray();
                    player.Reply("Multiple matching players found: \n" + string.Join(", ", names));
                    break;
            }

            return null;
        }

        #endregion

        #region Conversion Helper

        private static bool IsConvertibleTo<T>(object s)
        {
            try
            {
                var parsed = (T)Convert.ChangeType(s, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Data Helper

        private static void LoadData<T>(ref T data, string filename = null) =>
            data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename ?? nameof(TimedPermissions));

        private static void SaveData<T>(T data, string filename = null) =>
            Core.Interface.Oxide.DataFileSystem.WriteObject(filename ?? nameof(TimedPermissions), data);

        #endregion

        #region Message Wrapper

        public static string GetMessage(string key, string id) => _instance.lang.GetMessage(key, _instance, id);

        #endregion

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Invalid Time Format", "Invalid Time Format: Ex: 1d12h30m | d = days, h = hours, m = minutes"},
                {"Player Has No Info", "There is no info about this player."},
                {"Player Info", $"Info about <color=#C4FF00>{{player}}</color>:{Environment.NewLine}<color=#C4FF00>Groups</color>: {{groups}}{Environment.NewLine}<color=#C4FF00>Permissions</color>: {{permissions}}"},
                {"User Doesn't Have Permission", "{target} does not have permission '{permission}'."},
                {"User Isn't In Group", "{target} isn't in group '{group}'."},
            }, this);
        }

        #endregion

        #region Configuration

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("Wipe Data on New Save (Limited to Certain Games)")]
            public bool WipeDataOnNewSave { get; private set; } = false;
        }

        #endregion

        #region Data Structures

        // TODO: do general refactoring & improvements
        private class Player
        {
            public readonly List<TimedAccessValue> Permissions = new List<TimedAccessValue>();
            public readonly List<TimedAccessValue> Groups = new List<TimedAccessValue>();
            public string Name = "unknown";
            public string Id = "0";

            internal static Player Get(string steamId) => _players.Find(p => p.Id == steamId);

            internal static Player GetOrCreate(IPlayer player)
            {
                Player pl = Get(player.Id);

                if (pl == null)
                {
                    pl = new Player(player);

                    _players.Add(pl);
                    SaveData(_players);
                }

                return pl;
            }

            public TimedAccessValue GetTimedPermission(string permission) => Permissions.Find(p => p.Value == permission);

            public TimedAccessValue GetTimedGroup(string group) => Groups.Find(g => g.Value == group);

            public void AddPermission(string permission, DateTime expireDate)
            {
                TimedAccessValue existingPermission = GetTimedPermission(permission);

                if (existingPermission != null)
                {
                    existingPermission.ExpireDate += expireDate - DateTime.UtcNow;

                    _instance.Puts($"----> {Name} ({Id}) - Permission Extended: {permission} to {existingPermission.ExpireDate - DateTime.UtcNow}" + Environment.NewLine);
                }
                else
                {
                    Permissions.Add(new TimedAccessValue(permission, expireDate));
                    _instance.permission.GrantUserPermission(Id, permission, null);

                    _instance.Puts($"----> {Name} ({Id}) - Permission Granted: {permission} for {expireDate - DateTime.UtcNow}" + Environment.NewLine);
                }

                SaveData(_players);
            }

            internal void AddGroup(string group, DateTime expireDate)
            {
                TimedAccessValue existingGroup = GetTimedGroup(group);

                if (existingGroup != null)
                {
                    existingGroup.ExpireDate += expireDate - DateTime.UtcNow;

                    _instance.Puts($"----> {Name} ({Id}) - Group Time Extended: {group} to {existingGroup.ExpireDate - DateTime.UtcNow}" + Environment.NewLine);
                }
                else
                {
                    Groups.Add(new TimedAccessValue(group, expireDate));
                    _instance.permission.AddUserGroup(Id, group);

                    _instance.Puts($"----> {Name} ({Id}) - Added to Group: {group} for {expireDate - DateTime.UtcNow}" + Environment.NewLine);
                }

                SaveData(_players);
            }

            internal void RemovePermission(string permission)
            {
                Permissions.Remove(GetTimedPermission(permission));
                _instance.permission.RevokeUserPermission(Id, permission);

                _instance.Puts($"----> {Name} ({Id}) - Permission Expired: {permission}" + Environment.NewLine);

                if (Groups.Count == 0 && Permissions.Count == 0)
                    _players.Remove(this);

                SaveData(_players);
            }

            internal void RemoveGroup(string group)
            {
                Groups.Remove(GetTimedGroup(group));
                _instance.permission.RemoveUserGroup(Id, group);

                _instance.Puts($"----> {Name} ({Id}) - Group Expired: {group}" + Environment.NewLine);

                if (Groups.Count == 0 && Permissions.Count == 0)
                    _players.Remove(this);

                SaveData(_players);
            }

            public void RemoveAllAccess()
            {
                foreach (TimedAccessValue group in Groups)
                    _instance.permission.RemoveUserGroup(Id, group.Value);

                Groups.Clear();

                foreach (TimedAccessValue permission in Permissions)
                    _instance.permission.RevokeUserPermission(Id, permission.Value);

                Permissions.Clear();
            }

            public void EnsureAllAccess()
            {
                foreach (TimedAccessValue group in Groups)
                    _instance.permission.AddUserGroup(Id, group.Value);

                foreach (TimedAccessValue permission in Permissions)
                    _instance.permission.GrantUserPermission(Id, permission.Value, null);
            }

            private void Update()
            {
                foreach (TimedAccessValue perm in Permissions.ToList())
                    if (perm.Expired)
                        RemovePermission(perm.Value);

                foreach (TimedAccessValue group in Groups.ToList())
                    if (group.Expired)
                        RemoveGroup(group.Value);
            }

            public override int GetHashCode() => Id.GetHashCode();

            private Player(IPlayer player)
            {
                Id = player.Id;
                Name = player.Name;

                _instance.timer.Repeat(60, 0, Update);
            }

            public Player()
            {
                _instance.timer.Repeat(60, 0, Update);
            }
        }

        // TODO: do general refactoring & improvements
        private class TimedAccessValue
        {
            public string Value = string.Empty;
            public DateTime ExpireDate;

            internal bool Expired => DateTime.Compare(DateTime.UtcNow, ExpireDate) > 0;

            public override int GetHashCode() => Value.GetHashCode();

            internal TimedAccessValue(string value, DateTime expireDate)
            {
                Value = value;
                ExpireDate = expireDate;
            }

            public TimedAccessValue()
            {
            }
        }

        #endregion
    }
}