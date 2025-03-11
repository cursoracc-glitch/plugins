using System.Collections.Generic;
using Oxide.Game.Rust.Libraries;
using Oxide.Core;
using System;
using Rust;
using Newtonsoft.Json;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("EasyAdmin", "Fartus", "1.0.1")]
    [Description("Игроки могут включать и выключать режим администратора")]
    class EasyAdmin : RustPlugin
    {
        #region Vars
        private PluginConfig config;
        #endregion

        #region Config
        private class GrandRevokeList
        {
            [JsonProperty("Список групп, которые нужно выдать")]
            public List<string> GrantGroup;
            [JsonProperty("Список групп, которые нужно забрать")]
            public List<string> RevokeGroup;
            [JsonProperty("Список привилегий, которые нужно выдать")]
            public List<string> GrantPerms;
            [JsonProperty("Список привилегий, которые нужно забрать")]
            public List<string> RevokePerms;
        }
        private class PluginConfig
        {
            [JsonProperty("Формат сообщений в чате")]
            public string ChatFormat;
            [JsonProperty("Команда переключения режима администратора")]
            public string ChatCommand;
            [JsonProperty("Привилегия, требующаяся для активации команды")]
            public string Permission;
            [JsonProperty("Действия при ВКЛЮЧЕНИИ режима администратора")]
            public GrandRevokeList Enable;
            [JsonProperty("Действия при ВЫКЛЮЧЕНИИ режима администратора")]
            public GrandRevokeList Disable;
            [JsonProperty("Добавлять и удалять игрока при смене режима администратора в users.cfg")]
            public bool SaveToConfig;
            [JsonProperty("Логировать использование команды")]
            public bool Log;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    ChatFormat = "<color=#42eef4>[EasyAdmin]</color> <color=#AAAAAA>{0}</color>",
                    ChatCommand = "/admin",
                    Permission = "easyadmin.use",
                    SaveToConfig = true,
                    Log = true,
                    Enable = new GrandRevokeList()
                    {
                        GrantGroup = new List<string>()
                        {
                            "admin"
                        },
                        RevokeGroup = new List<string>()
                        {
                            "default"
                        },
                        GrantPerms = new List<string>()
                        {
                            "vanish.allowed",
                            "removeaaa.*",
                            "betterchat.mute"
                        },
                        RevokePerms = new List<string>()
                        {
                            "buildinglimit.limit"
                        }
                    },
                    Disable = new GrandRevokeList()
                    {
                        GrantGroup = new List<string>()
                        {
                            "default"
                        },
                        RevokeGroup = new List<string>()
                        {
                            "admin"
                        },
                        GrantPerms = new List<string>()
                        {
                            "buildinglimit.limit"
                        },
                        RevokePerms = new List<string>()
                        {
                            "vanish.allowed",
                            "removeaaa.*",
                            "betterchat.mute"
                        }
                    }
                };
            }
        }
        #endregion

        #region Config loading
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации...");
            Config.WriteObject(PluginConfig.DefaultConfig(), true);
        }
        private void InitConfig()
        {
            try
            {
                config = Config.ReadObject<PluginConfig>();
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to load config file (is the config file corrupt?) ({ex.Message})");
            }
        }
        #endregion

        #region Data
        private Dictionary<string, bool> ActiveAdmins = new Dictionary<string, bool>();
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, ActiveAdmins);
        }
        void LoadData()
        {
            try
            {
                ActiveAdmins = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, bool>>(Title);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load active admins data file (is the file corrupt?) ({ex.Message})");
                ActiveAdmins = new Dictionary<string, bool>();
            }
        }
        #endregion

        #region Initialization and quiting
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["No Permission"] = "You don't have permission to use this command.",
                ["Admin On"] = "Admin mode turned <color=green>ON</color>",
                ["Admin Off"] = "Admin mode turned <color=red>OFF</color>",
                ["Fly"] = "Can not disable admin mode while flying"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["No Permission"] = "Недостаточно прав на выполнение данной команды.",
                ["Admin On"] = "Режим Администратора <color=green>ВКЛЮЧЕН</color>",
                ["Admin Off"] = "Режим Администратора <color=red>ВЫКЛЮЧЕН</color>",
                ["Fly"] = "Невозможно отключить режим администратора в полёте"
            }, this, "ru");
        }
        void Loaded()
        {
            InitConfig();
            LoadData();
            LoadMessages();
            permission.RegisterPermission(config.Permission, this);
            var command = Interface.Oxide.GetLibrary<Command>();
            command.AddChatCommand(config.ChatCommand.Replace("/", string.Empty), this, AdminCC);
        }
        void Unload()
        {
            OnServerSave();
        }
        void OnServerSave()
        {
            SaveData();
        }
        #endregion

        #region Commands
        void AdminCC(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                Reply(player, "No Permission");
                if (ActiveAdmins.ContainsKey(player.UserIDString))
                {
                    ActiveAdmins.Remove(player.UserIDString);
                    SaveData();
                }
                return;
            }

            if (!ActiveAdmins.ContainsKey(player.UserIDString))
            {
                ActiveAdmins[player.UserIDString] = false;
            }
            ActiveAdmins[player.UserIDString] = !ActiveAdmins[player.UserIDString];
            SaveData();

            if (ActiveAdmins[player.UserIDString])
            {
                Reply(player, "Admin On");
                AdminOn(player);
                return;

            }
            else
            {
                if (player.IsFlying)
                {
                    Reply(player, "Fly");
                    ActiveAdmins[player.UserIDString] = true;
                    return;
                }
                Reply(player, "Admin Off");
                AdminOff(player);
                return;
            }
        }
        #endregion

        #region Admin mode switching
        private void AdminOn(BasePlayer player)
        {
            if (config.Log)
                Puts($"Player \"{player.displayName}\" has turn ON admin mode");
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.net.connection.authLevel = 2;
            if (config.SaveToConfig)
            {
                Server.Command($"ownerid {player.UserIDString} \"{player.displayName}\" {Title}");
                Server.Command("writecfg");
            }
            foreach(var group in config.Enable.GrantGroup)
            {
                if (!permission.GroupExists(group))
                {
                    PrintWarning($"Group \"{group}\" doesn't exist and wasn't added to player \"{player.displayName}\"");
                    continue;
                }
                permission.AddUserGroup(player.UserIDString, group);
            }
            foreach(var group in config.Enable.RevokeGroup)
            {
                if (!permission.GroupExists(group))
                {
                    PrintWarning($"Group \"{group}\" doesn't exist and wasn't removed from player \"{player.displayName}\"");
                    continue;
                }
                permission.RemoveUserGroup(player.UserIDString, group);
            }
            foreach(var perm in config.Enable.GrantPerms)
            {
                if (!permission.PermissionExists(perm))
                {
                    PrintWarning($"Permission \"{perm}\" doesn't exist and wasn't added to player \"{player.displayName}\"");
                    continue;
                }
                permission.GrantUserPermission(player.UserIDString, perm, null);
            }
            foreach(var perm in config.Enable.RevokePerms)
            {
                if (!permission.PermissionExists(perm))
                {
                    PrintWarning($"Permission \"{perm}\" doesn't exist and wasn't removed from player \"{player.displayName}\"");
                    continue;
                }
                permission.RevokeUserPermission(player.UserIDString, perm);
            }
        }
        private void AdminOff(BasePlayer player)
        {
            if (config.Log)
                Puts($"Player \"{player.displayName}\" has turn OFF admin mode");
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
            player.net.connection.authLevel = 0;
            if (config.SaveToConfig)
            {
                Server.Command($"removeowner {player.UserIDString}");
                Server.Command("writecfg");
            }
            foreach (var group in config.Disable.GrantGroup)
            {
                if (!permission.GroupExists(group))
                {
                    PrintWarning($"Group \"{group}\" doesn't exist and wasn't added to player \"{player.displayName}\"");
                    continue;
                }
                permission.AddUserGroup(player.UserIDString, group);
            }
            foreach (var group in config.Disable.RevokeGroup)
            {
                if (!permission.GroupExists(group))
                {
                    PrintWarning($"Group \"{group}\" doesn't exist and wasn't removed from player \"{player.displayName}\"");
                    continue;
                }
                permission.RemoveUserGroup(player.UserIDString, group);
            }
            foreach (var perm in config.Disable.GrantPerms)
            {
                if (!permission.PermissionExists(perm))
                {
                    PrintWarning($"Permission \"{perm}\" doesn't exist and wasn't added to player \"{player.displayName}\"");
                    continue;
                }
                permission.GrantUserPermission(player.UserIDString, perm, null);
            }
            foreach (var perm in config.Disable.RevokePerms)
            {
                if (!permission.PermissionExists(perm))
                {
                    PrintWarning($"Permission \"{perm}\" doesn't exist and wasn't removed from player \"{player.displayName}\"");
                    continue;
                }
                permission.RevokeUserPermission(player.UserIDString, perm);
            }
        }
        #endregion

        #region Oxide Hooks
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!ActiveAdmins.ContainsKey(player.UserIDString)) return;
            if (ActiveAdmins[player.UserIDString])
            {
                if (player.net.connection.authLevel != 2)
                {
                    AdminOn(player);
                }
            }
            else
            {
                if (player.net.connection.authLevel == 2)
                {
                    AdminOff(player);
                }
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if(!permission.UserHasPermission(player.UserIDString,config.Permission))
                AdminOff(player);
        }

        #endregion

        #region Helpers        
        private void Reply(BasePlayer player, string LangKey, params object[] args)
        {
            SendReply(player, string.Format(config.ChatFormat, GetMsg(LangKey, player)), args);
        }
        private string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        private bool HasPerm(BasePlayer player) => permission.UserHasPermission(player.UserIDString, config.Permission);
        #endregion
    }
}