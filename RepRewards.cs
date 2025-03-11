using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Rep Rewards", "TopPlugin.ru", 1.0)]
    [Description("Reward players for representing your server!")]
    public class RepRewards : RustPlugin {
        [PluginReference]
        private Plugin economics = null;
        [PluginReference]
        private Plugin serverRewards = null;

        private Hash<string, Timer> users;

        private void Loaded() {
            if (economics == null || serverRewards == null) {
                foreach (Plugin pl in plugins.GetAll()) {
                    if (economics != null && serverRewards != null) break;

                    if (!pl.IsLoaded) continue;
                    if (economics == null && pl.Name.Equals("Economics")) {
                        economics = pl;
                        Puts("Found previously loaded Economics plugin.");
                        continue;
                    }
                    if (serverRewards == null && pl.Name.Equals("ServerRewards")) {
                        serverRewards = pl;
                        Puts("Found previously loaded ServerRewards plugin.");
                        continue;
                    }
                }
            }
            
            LoadConfig();
            users = new Hash<string, Timer>();

            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                if (player.displayName.Contains(Config.Get<string>("RepKey"))) {
                    if (!users.ContainsKey(player.UserIDString)) {
                        Timer userTimer = timer.Every(Config.Get<float>("RewardIntervalMins") * 60f, () => {
                            RewardPlayer(player);
                        });
                        users.Add(player.UserIDString, userTimer);
                    }
                }
            }
        }

        private void OnPluginLoaded(Plugin plugin) {
            if (plugin.Name.Equals("Economics")) {
                economics = plugin;
                Puts("Found Economics plugin from subscribed event.");
            } else if (plugin.Name.Equals("ServerRewards")) {
                serverRewards = plugin;
                Puts("Found ServerRewards plugin from subscribed event.");
            }
        }

        private void OnPlayerConnected(BasePlayer player) {
            if (player.displayName.Contains(Config.Get<string>("RepKey"))) {
                if (!users.ContainsKey(player.UserIDString)) {
                    Timer userTimer = timer.Every(Config.Get<float>("RewardIntervalMins") * 60f, () => {
                        RewardPlayer(player);
                    });
                    users.Add(player.UserIDString, userTimer);
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason) {
            if (users.ContainsKey(player.UserIDString)) {
                users[player.UserIDString].Destroy();
                if (!users.Remove(player.UserIDString)) {
                    Puts("Couldn't remove player!");
                }
            }
        }

        private void RewardPlayer(BasePlayer player) {
            Dictionary<string, double> itemRewards = Config.Get<Dictionary<string, double>>("ItemRewards");
            foreach (KeyValuePair<string, double> item in itemRewards) {
                if (item.Key.Equals("money")) {
                    if (economics != null) {
                        economics.Call("Deposit", player.userID, item.Value);
                    } else {
                        Puts("Couldn't find Economy plugin. " + item.Key);
                    }
                } else if (item.Key.Equals("points")) {
                    if (serverRewards != null) {
                        serverRewards.Call("AddPoints", player.userID, (int)item.Value);
                        Puts("Gave user points");
                    } else {
                        Puts("Couldn't find ServerRewards plugin. " + item.Key);
                    }
                } else {
                    int itemID = ItemManager.FindItemDefinition(item.Key).itemid;
                    Item objectToGive = ItemManager.CreateByItemID(itemID, (int)item.Value);
                    if (objectToGive == null) {
                        Puts("Invalid item! " + item.Key);
                    }
                    player.GiveItem(objectToGive);
                }
            }
        }

        // Default Config
        protected override void LoadDefaultConfig() {
            Config.Clear();
            Config["RepKey"] = "[RR]";
            Config["RewardIntervalMins"] = 10f;
            Dictionary<string, double> itemRewards = new Dictionary<string, double>();
            itemRewards.Add("money", 100.0);
            itemRewards.Add("points", 100.0);
            itemRewards.Add("metal.fragments", 1000.0);
            Config["ItemRewards"] = itemRewards;
        }
    }
}
