using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Instant Smelt", "Orange", "2.0.5")]
    [Description("Smelt resources as soon as they are mined")]
    public class InstantSmelt : RustPlugin
    {
        #region Vars

        private const string permUse = "instantsmelt.use";
        private const string charcoalItemName = "charcoal";
        private const string woodItemName = "wood";

        #endregion
        
        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
            cmd.AddChatCommand(config.command, this, nameof(cmdToggleChat));
            LoadData();
        }

        private void Unload()
        {
            SaveData();
        }

        private object OnCollectiblePickup(Item item, BasePlayer player)
        {
            return OnGather(player, item, false, true);
        }
        
        private object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            return OnGather(player, item);
        }
        
        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            return OnGather(player, item, true);
        }

        #endregion

        #region Commands

        private void cmdToggleChat(BasePlayer player)
        {
            if (HasPermission(player) == false)
            {
                Message(player, "Permission");
                return;
            }

            var key = string.Empty;
            
            if (data.Contains(player.userID))
            {
                key = "Enabled";
                data.Remove(player.userID);
            }
            else
            {
                key = "Disabled";
                data.Add(player.userID);
            }
            
            Message(player, key);
        }

        #endregion

        #region Core

        private object OnGather(BasePlayer player, Item item, bool bonus = false, bool pickup = false)
        {
            var perm = HasPermission(player);
            if (perm == false)
            {
                return null;
            }

            if (data.Contains(player.userID))
            {
                return null;
            }

            var shortname = item.info.shortname;
            if (config.blackList.Contains(shortname))
            {
                return null;
            }

            var newItem = (Item) null;
            
            if (shortname == woodItemName)
            {
                newItem = ItemManager.CreateByName(charcoalItemName, item.amount);
            }
            else
            {
                var cookable = item.info.GetComponent<ItemModCookable>();
                if (cookable == null) {return null;}
                newItem = ItemManager.Create(cookable.becomeOnCooked, item.amount);
            }
            
            NextTick(() =>
            {
                newItem.amount = item.amount;
                item.GetHeldEntity()?.Kill();
                item.DoRemove();

                if (bonus == false)
                {
                    player.GiveItem(newItem, BaseEntity.GiveItemReason.ResourceHarvested);
                }
            });
                
            return pickup ? null : newItem;
        }

        private bool HasPermission(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, permUse);
        }

        #endregion
        
        #region Configuration 1.1.0

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Command")]
            public string command;

            [JsonProperty(PropertyName = "A. Blacklist")]
            public List<string> blackList;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                command = "ismelt",
                blackList = new List<string>
                {
                    "shortname here",
                    "another shortname"
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
        
        #region Data 1.0.0

        private const string filename = "Temp/InstantSmelt/Playes";
        private List<ulong> data = new List<ulong>();

        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>(filename);
            }
            catch (Exception e)
            {
                PrintWarning(e.Message);
            }

            SaveData();
            timer.Every(Core.Random.Range(500, 700f), SaveData);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(filename, data);
        }

        #endregion
        
        #region Localization 1.1.1
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Permission", "You don't have permission to use that!"},
                {"Enabled", "You enabled instant smelt!"},
                {"Disabled", "You disabled instant smelt!"},
            }, this);
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null)
            {
                return;
            }

            var message = GetMessage(messageKey, player.UserIDString, args);
            player.ChatMessage(message);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args)
        {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        #endregion
    }
}