using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Linq;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("Admin Time", "Rustic", "1.1.0")]
    [Description("Allows admins to use /day, /night, and /now to change their local time in game.")]

    internal class AdminTime : RustPlugin
    {
        #region General

        const string permAllowTimeChange = "AdminTime.use";

        void Init()
        {
            permission.RegisterPermission(permAllowTimeChange, this);
        }
        
        #endregion

        #region Config
        
        // Config Creation
		private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Day Value")]
            public int DayValue = 12;

            [JsonProperty(PropertyName = "Night Value")]
            public int NightValue = 1;
            
            public string ToJson() => JsonConvert.SerializeObject(this);
            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    throw new JsonException();
                }
                if (!configData.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration File invalid or outdated. Updated.");
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted. \n{ex}");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating Config File");
            configData = new ConfigData();
        }

        #endregion

        #region Commands

        [ChatCommand("day")]
        private void DayCommand(BasePlayer player)
        {
            ChangeTime(player, configData.DayValue);
        }

        [ChatCommand("night")]
        private void NightCommand(BasePlayer player)
        {
            ChangeTime(player, configData.NightValue);
        }

        [ChatCommand("now")]
        private void NowCommand(BasePlayer player)
        {
            ChangeTime(player, -1);
        }

        [ChatCommand("time")]
        private void TimeCommand(BasePlayer player, string command, string[] args)
        {
            int timevalue = Convert.ToInt32(args[0]);

            if (player.IsAdmin == false && !permission.UserHasPermission(player.UserIDString, permAllowTimeChange))
            {
                SendReply(player, "Permission Denied.");
            } 

            if (player.IsAdmin == true || permission.UserHasPermission(player.UserIDString, permAllowTimeChange))
            {
                if (timevalue > 23 | timevalue < 0)
                {
                    SendReply(player, "Invalid Time, please use 1-23");
                } else {
                    ChangeTime(player, timevalue);
                }
            }
        }

        void ChangeTime(BasePlayer player, int timevalue)
        {
            if (player.IsAdmin == false && !permission.UserHasPermission(player.UserIDString, permAllowTimeChange))
            {
                SendReply(player, "Permission Denied.");
            } 

            if (player.IsAdmin == true || permission.UserHasPermission(player.UserIDString, permAllowTimeChange))
            {
                if (player.IsAdmin == true) { player.SendConsoleCommand("admintime", timevalue); }

                if (player.IsAdmin == false && permission.UserHasPermission(player.UserIDString, permAllowTimeChange)) 
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true); 
                    player.SendNetworkUpdateImmediate();
                    player.SendConsoleCommand("admintime", timevalue); 
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false); 
                    player.SendNetworkUpdateImmediate();
                }
            }
        }

        #endregion
    }
}