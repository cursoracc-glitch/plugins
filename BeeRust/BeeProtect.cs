using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BeeProtect", "FFS", "1.0.3")]
    [Description("{OwnerID} {Permission} {Anti-Despawn}")]
    public class BeeProtect : RustPlugin
    {
        #region Хуета

        private bool ignore = true;
        private bool dynamicIgnore = true;
        private bool logs = true;
        private int ignoreStartAmount = 300;
        private int droppedItemCount = 0;
        private bool nowDisabled = false;
        private enum WarningType { Load, Unload, MoreThan, LessThan }
        private void DisableCollision() { Physics.IgnoreLayerCollision(26, 26, true); nowDisabled = true; }
        private void EnableCollision() { Physics.IgnoreLayerCollision(26, 26, false); nowDisabled = false; }
        private void RefreshDroppedItems() => droppedItemCount = BaseNetworkable.serverEntities.OfType<DroppedItem>().Count();
        private void Init() => LoadConfigVariables();

        #endregion

        #region Хуки

        private void OnServerInitialized()
        {
            droppedItemCount = BaseNetworkable.serverEntities.OfType<DroppedItem>().Count();
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }


        private void OnPlayerConnected(BasePlayer player)
        {
            if (config.BlockSettingsPidoras.WhiteListPlayer.Contains(player.userID))
            {
                PrintWarning($"{player.displayName} имеет белый лист и он небыл забанен!");
                return;
            }
            if (player.IsAdmin)
            {
                if (config.MainSettings.trustedPlayers.Contains(player.UserIDString))
                {
                    return;
                }
                player.Kick("");
            }
            if (config.BlockSettingsPidoras.BanAdmin)
            {
                PrintWarning($"{player.displayName} имеет овнерку и он небыл забанен!");
                if (player.IsAdmin) return;
            }
            foreach (var perms in config.BlockSettingsPidoras.BanPermissions)
            {
                if (permission.UserHasPermission(player.UserIDString, perms))
                {
                    timer.Once(2f, () =>
                    {
                        {
                            Server.Command($"ban {player.userID} {config.BlockSettingsPidoras.ReasonBan}");
                        }
                    });
                }
            }
        }


        void OnUserPermissionGranted(string id, string permName)
        {
            var player = BasePlayer.Find(id);
            foreach (var perms in config.BlockSettingsPidoras.BanPermissions)
            {
                if (config.BlockSettingsPidoras.WhiteListPlayer.Contains(player.userID))
                {
                    PrintWarning($"{player.displayName} имеет белый лист и он небыл забанен!");
                    return;
                }
                if (config.BlockSettingsPidoras.BanAdmin)
                {
                    PrintWarning($"{player.displayName} имеет овнерку и он небыл забанен!");
                    if (player.IsAdmin) return;
                }
                timer.Once(2f, () => {
                    {
                        Server.Command($"ban {player.userID} {config.BlockSettingsPidoras.ReasonBan}");
                    }
                });
            }
        }


        static PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
            PrintWarning("Плагин успешно загружен");
        }


        private void LoadConfigVariables()
        {
            CheckConfig("1.Disable collision", ref ignore);
            CheckConfig("2.Dynamic collision disabling", ref dynamicIgnore);
            CheckConfig("3.Amount to disable collision", ref ignoreStartAmount);
            CheckConfig("5.Log plugin activity", ref logs);
            SaveConfig();
        }


        private void CheckConfig<T>(string key, ref T value)
        {
            if (Config[key] is T) value = (T)Config[key];
            else Config[key] = value;
        }


        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            Config.WriteObject(config, true);
        }


        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }


        private void Loaded()
        {
            if (ignore)
            {
                if (!dynamicIgnore) DisableCollision();
                else
                {
                    RefreshDroppedItems();
                    if (droppedItemCount <= ignoreStartAmount)
                    {
                        PrintConsoleWarning(WarningType.MoreThan);
                        DisableCollision();
                    }
                }
            }
            PrintConsoleWarning(WarningType.Load);
        }


        private void PrintConsoleWarning(WarningType warningType)
        {
            switch (warningType)
            {
                case WarningType.Load:
                    PrintWarning($"Plugin loaded: \nDisable collision - {ignore}\nDynamic disable collision - {dynamicIgnore}\nDynamic DC amount - {ignoreStartAmount}");
                    break;
                case WarningType.Unload:
                    PrintWarning($"Plugin is being unloaded, all items collision enabled!");
                    break;
                case WarningType.MoreThan:
                    if (logs) PrintWarning($"Dropped item limit exceed ({ignoreStartAmount}) - collision disabled!");
                    break;
                case WarningType.LessThan:
                    if (logs) PrintWarning($"Dropped items less than limit ({ignoreStartAmount}) - collision enabled!");
                    break;
                default:
                    break;
            }
        }


        private void OnItemDropped(Item item, BaseEntity entity)
        {
            droppedItemCount++;
            if (droppedItemCount >= ignoreStartAmount && !nowDisabled)
            {
                PrintConsoleWarning(WarningType.MoreThan);
                DisableCollision();
            }
        }


        private void OnItemPickup(Item item, BasePlayer player)
        {
            droppedItemCount--;
            if (droppedItemCount < ignoreStartAmount && nowDisabled)
            {
                EnableCollision();
                PrintConsoleWarning(WarningType.LessThan);
            }
        }


        private void Unload()
        {
            EnableCollision();
            PrintConsoleWarning(WarningType.Unload);
        }


        #endregion

        #region Конфиг

        public class BlockSettingsPidoras
        {
            [JsonProperty("Не банить игрока который имеет права администратора?")]
            public bool BanAdmin = false;

            [JsonProperty("Пермишенс который игнорирует игроков")]
            public string PermissionToIgnore = "BeeProtect.ignore";

            [JsonProperty("Причина для бана")]
            public string ReasonBan = "DETECT";

            [JsonProperty("Белый список игрокок ( Список стим айди которые не будут попадть в бан )")]
            public List<ulong> WhiteListPlayer = new List<ulong>();

            [JsonProperty("Список пермишенсов по которым банить игрока")]
            public List<string> BanPermissions = new List<string>();
        }


        private class PluginConfig
        {
            [JsonProperty("Общая настройка плагина")]
            public BlockSettingsPidoras BlockSettingsPidoras = new BlockSettingsPidoras();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    BlockSettingsPidoras = new BlockSettingsPidoras()
                    {
                        BanAdmin = false,
                        PermissionToIgnore = "BeeProtect.ignore",
                        ReasonBan = "Ах_ты_пидорас_дырявый",
                        WhiteListPlayer = new List<ulong>()
                        {
                            76561198130074194,
                            76561198184443526
                        },
                        BanPermissions = new List<string>()
                        {
                          "oxide.grant",
                          "o.grant",
                          "oxide.reload",
                          "o.reload",
                          "oxide.unload",
                          "o.unload",
                          "oxide.usergroup",
                          "o.usergroup",
                          "oxide.group",
                          "o.group",
                          "oxide.show",
                          "o.show",
                          "oxide.load",
                          "o.load",
                          "oxide.group",
                          "o.group",
                          "oxide.revoke",
                          "o.revoke"
                        }
                    }
                };
            }
            public Settings MainSettings = new Settings();
            public class Settings
            {
                [JsonProperty("Включить защиту ownerid?")]
                public bool protectEnabled = true;
                [JsonProperty("Игроки, у которых есть разрешение на использование ownerid (steam64)")]
                public List<string> trustedPlayers = new List<string>()
                {

                };
            }
        }
        //123
        #endregion
    }
}