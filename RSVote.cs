using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustServer.gg Vote", "NightHawk@Codefling", "1.2.2")]
    [Description("Voting reward plugin for RustServers.gg")]
    public class RSVote: RustPlugin
    {
        #region Vars

        private Dictionary<ulong, DateTime> cooldown = new Dictionary<ulong, DateTime>();

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            foreach (var command in config.commands)
            {
                cmd.AddChatCommand(command, this, cmdChat);
            }
            
            timer.Every(Core.Random.Range(300, 500), () => { cooldown.Clear(); });
            Puts($"Loaded server with id '{config.serverId}'");
        }

        #endregion

        #region Commands

        private void cmdChat(BasePlayer player, string command, string[] args)
        {
            if (cooldown.ContainsKey(player.userID) == true)
            {
                var nextUse = cooldown[player.userID];
                if (nextUse > DateTime.UtcNow)
                {
                    player.ChatMessage("Cooldown...");
                    return;
                }

                cooldown[player.userID] = DateTime.UtcNow.AddSeconds(5);
            }
            else
            {
                cooldown.Add(player.userID, DateTime.UtcNow.AddSeconds(5));
            }

            player.ChatMessage("Trying to claim reward...");
            var url = CheckUrl(player.UserIDString);
            Execute(url, "status", player);
        }

        #endregion

        #region Core

        private string CheckUrl(string steamId)
        {
            return BuildUrl("status", steamId);
        }

        private string ClaimUrl(string steamId)
        {
            return BuildUrl("claim", steamId);
        }

        private string BuildUrl(string action, string userId)
        {
            var url = ConfigData.baseUrl;
            url += $"action={action}";
            url += $"&key={config.apiKey}";
            url += $"&server={config.serverId}";
            url += $"&steamid={userId}";
            return url;
        }

        private void Execute(string url, string action, BasePlayer player)
        {
            webrequest.Enqueue(url, null, (i, s) => { HandleResponse(url, i, s, action, player); }, this);
        }

        private void HandleResponse(string url, int code, string response, string action, BasePlayer player)
        {
            var rInt = 0;
            if (int.TryParse(response, out rInt) == false)
            {
                PrintWarning($"Failed to parse response from '{response}'");
                return;
            }

            if (action == "status")
            {
                switch (rInt)
                {
                    case 0:
                        player.ChatMessage($"Vote at the servers page to claim reward!\n" +
                                           $"https://rustservers.gg/server/{config.serverId}");
                        break;

                    case 1:
                        GiveRandomReward(player);
                        break;

                    case 2:
                        player.ChatMessage("You already claimed your reward(s)");
                        break;
                }
            }

            if (action == "claim")
            {
                switch (rInt)
                {
                    case 0:
                        // Ignore
                        break;

                    case 1:
                        // Ignore
                        break;

                    case 2:
                        // Ignore
                        break;
                }
            }
        }

        private void GiveRandomReward(BasePlayer player)
        {
            IReward reward;
            if (Core.Random.Range(0, 101) > 50)
            {
                reward = config.rewardCommands.GetRandom();
            }
            else
            {
                reward = config.rewardItems.GetRandom();
            }

            var userId = player.UserIDString;
            var result = reward.GiveTo(userId);
            if (result == false)
            {
                PrintWarning($"Failed to give reward to '{userId}' ({reward.Info()})");
                player.ChatMessage("Failed to claim reward, please contact the server owner!");
                return;
            }

            Puts($"Player {userId} received reward for voting");
            player.ChatMessage($"You received {reward.Info()} for voting!");
            var url = ClaimUrl(userId);
            Execute(url, "claim", player);
        }

        #endregion

        #region Config

        private class ConfigData
        {
            [JsonProperty("API Key")] 
            public string apiKey = string.Empty;

            [JsonProperty("Server Id")] 
            public string serverId = string.Empty;

            [JsonProperty("Chat Commands")] 
            public string[] commands =
            {
                "claim",
                "vote",
                "votes",
            };

            [JsonProperty("Reward items")] 
            public RewardItem[] rewardItems = new[]
            {
                new RewardItem
                {
                    shortname = "wood",
                    amountMin = 1000,
                    amountMax = 2000,
                },
                new RewardItem
                {
                    shortname = "stones",
                    amountMin = 1000,
                    amountMax = 2000,
                },
                new RewardItem
                {
                    shortname = "metal.refined",
                    amountMin = 1000,
                    amountMax = 2000,
                },
            };

            [JsonProperty("Reward Commands Help")] 
            public string commandHelp = "use {steamid} to add user steam id\n" +
                                        "Examples:\n" +
                                        "sr add {steamid} 10 - to add ServerRewards points\n" +
                                        "deposit {steamid} 10 - to add Economics points\n";

            [JsonProperty("Reward Commands")] 
            public RewardCommand[] rewardCommands = new[]
            {
                new RewardCommand(),
                new RewardCommand(),
                new RewardCommand()
            };

            [JsonIgnore] 
            public const string baseUrl = "https://rustservers.gg/vote-api.php?";
        }

        private static ConfigData config = new ConfigData();

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
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Rewards

        private interface IReward
        {
            bool GiveTo(string userId);

            string Info();
        }

        private class RewardItem : IReward
        {
            [JsonProperty("Item Shortname")] public string shortname;
            [JsonProperty("Description")] public string description;

            [JsonProperty("Item Amount Min")] public int amountMin;

            [JsonProperty("Item Amount Max")] public int amountMax;

            [JsonProperty("Item Skin id")] public ulong skinId;

            public Item GetItem()
            {
                var def = ItemManager.FindItemDefinition(shortname);
                if (def == null)
                {
                    return null;
                }

                var amount = amountMax > amountMin
                    ? Core.Random.Range(amountMin, amountMax)
                    : Core.Random.Range(amountMax, amountMin);

                var item = ItemManager.Create(def, amount);
                if (item == null)
                {
                    return null;
                }

                item.skin = skinId;
                return item;
            }

            public bool GiveTo(string userId)
            {
                var player = BasePlayer.Find(userId);
                if (player == null)
                {
                    return false;
                }

                var item = GetItem();
                if (item == null)
                {
                    return false;
                }

                player.GiveItem(item);
                return true;
            }

            public string Info()
            {
                return $"Item: {description}";
            }
        }

        private class RewardCommand : IReward
        {
            [JsonProperty("Command")] public string command = "example {steamid} 1d";
            [JsonProperty("Description")] public string description = "example Description (will appear on Message)";
            [JsonProperty("Type")] public string type = "example type of Reward (Kit, Permission etc.)";

            public bool GiveTo(string userId)
            {
                var cmd = command;
                cmd = cmd.Replace("{steamid}", userId, StringComparison.OrdinalIgnoreCase);
                cmd = cmd.Replace("{steam}", userId, StringComparison.OrdinalIgnoreCase);
                cmd = cmd.Replace("{id}", userId, StringComparison.OrdinalIgnoreCase);
                ConsoleSystem.Run(ConsoleSystem.Option.Server, cmd);
                return true;
            }

            public string Info()
            {
                return $"{type}: {description}";
            }
        }

        #endregion
    }
}