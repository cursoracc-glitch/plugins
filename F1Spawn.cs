using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("F1Spawn", "Colon Blow", "1.0.3")]
    [Description("Allows use of F1 Item List Spawn")]
    class F1Spawn : CovalencePlugin
    {

        #region Load

        const string permBL1 = "f1spawn.blacklist1";
        const string permBL2 = "f1spawn.blacklist2";
        const string permAL1 = "f1spawn.allowlist1";
        const string permAL2 = "f1spawn.allowlist2";
        const string permALL = "f1spawn.allowall";

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permBL1, this);
            permission.RegisterPermission(permBL2, this);
            permission.RegisterPermission(permAL1, this);
            permission.RegisterPermission(permAL2, this);
            permission.RegisterPermission(permALL, this);
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Bypass checks if Admin ? ")] public bool AdminBypass { get; set; }
            [JsonProperty(PropertyName = "Bypass checks if Moderator ? ")] public bool ModBypass { get; set; }
            [JsonProperty(PropertyName = "Blacklist 1 Items : ")] public List<string> BlackListedItems1 { get; set; }
            [JsonProperty(PropertyName = "Blacklist 2 Items : ")] public List<string> BlackListedItems2 { get; set; }
            [JsonProperty(PropertyName = "Allowed list 1 Items : ")] public List<string> AllowListItems1 { get; set; }
            [JsonProperty(PropertyName = "Allowed list 2 Items : ")] public List<string> AllowListItems2 { get; set; }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                AdminBypass = true,
                ModBypass = true,
                BlackListedItems1 = new List<string>()
                    {
                        "Satchel Charge",
                        "Timed Explosive Charge"
                    },
                BlackListedItems2 = new List<string>()
                    {
                        "Beancan Grenade",
                        "F1 Grenade"
                    },
                AllowListItems1 = new List<string>()
                    {
                        "Hammer",
                        "Building Plan"
                    },
                AllowListItems2 = new List<string>()
                    {
                        "Wood",
                        "Stones"
                    }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Give Command and Hook

        [Command("inventory.giveid")]
        void GiveIdCommand(IPlayer player, string command, string[] args)
        {
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            string command = arg.cmd.Name;
            if (command.Equals("giveid") || command.Equals("givearm"))
            {
                BasePlayer player = arg.Player();
                if (!player) return null;
                if (isAllowed(player, permALL) || isAllowed(player, permAL1) || isAllowed(player, permAL2) || isAllowed(player, permBL1) || isAllowed(player, permBL2) || player.net?.connection?.authLevel > 0)
                {
                    Item item = ItemManager.CreateByItemID(arg.GetInt(0), 1, 0);
                    if (item == null) return false;
                    var allowspawn = false;
                    if ((player.IsAdmin || player.IsDeveloper || player.net?.connection?.authLevel >= 2) && config.AdminBypass) allowspawn = true;
                    else if (player.net?.connection?.authLevel == 1 && config.ModBypass) allowspawn = true;
                    else if (isAllowed(player, permALL)) allowspawn = true;
                    else if (isAllowed(player, permAL1) && ((config.AllowListItems1.Contains(item.info.displayName.english) || config.AllowListItems1.Contains(item.info.shortname)))) allowspawn = true;
                    else if (isAllowed(player, permAL2) && ((config.AllowListItems2.Contains(item.info.displayName.english) || config.AllowListItems2.Contains(item.info.shortname)))) allowspawn = true;
                    else if (isAllowed(player, permBL1) && (!(config.BlackListedItems1.Contains(item.info.displayName.english) || config.BlackListedItems1.Contains(item.info.shortname)))) allowspawn = true;
                    else if (isAllowed(player, permBL2) && (!(config.BlackListedItems2.Contains(item.info.displayName.english) || config.BlackListedItems2.Contains(item.info.shortname)))) allowspawn = true;
                    else return false;

                    if (allowspawn)
                    {
                        item.amount = arg.GetInt(1, 1);
                        if (!player.inventory.GiveItem(item, null))
                        {
                            item.Remove(0f);
                            return false;
                        }
                        player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                        //Debug.Log(string.Concat(new object[] { "[F1Spawn] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                        return false;
                    }
                    else return false;
                }
            }
            return null;
        }

        #endregion
    }
}