using System.Collections;
using System.Collections.Generic;
using Facepunch;
using ProtoBuf;
using Network;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
using HarmonyLib;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NoSkins", "Whispers88", "1.0.1")]
    [Description("Allows you to disable other players worn skins")]
    public class NoSkins : RustPlugin
    {
        //public static NoSkins _noSkins;
        private static HashSet<ulong> _HideSkinsPlayers = new HashSet<ulong>();
        private const string PermAllow = "noskins.allow";
        private const string PermHideSkins = "noskins.on";

        #region Config
        private Configuration config;
        class Configuration
        {
            [JsonProperty("Command Aliases")]
            public string[] noskinCMD = new[] { "noskin", "noskins", "ns" };

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
                var configDict = config.ToDictionary();
                Dictionary<string, object> defaultconObjects = new Dictionary<string, object>();
                foreach (var obj in Config)
                {
                    defaultconObjects.Add(obj.Key, obj.Value);
                }
                if (configDict.Count != defaultconObjects.Count)
                {
                    Puts("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
                else
                {
                    foreach (var key in configDict.Keys)
                    {
                        if (defaultconObjects.ContainsKey(key))
                            continue;

                        Puts("Configuration appears to be outdated; updating and saving");
                        SaveConfig();
                        break;

                    }
                }
            }
            catch
            {
                Puts($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Config

        #region Initialization
        private void Init()
        {
            //_noSkins = this;
            _cachedContainers = new Dictionary<ulong, ProtoBuf.ItemContainer>();
            _HideSkinsPlayers = new HashSet<ulong>();

            permission.RegisterPermission(PermAllow, this);
            permission.RegisterPermission(PermHideSkins, this);

            AddCovalenceCommand(config.noskinCMD, nameof(NoSkinCMD));

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (_HideSkinsPlayers.Contains(player.userID))
                    permission.GrantUserPermission(player.UserIDString, PermHideSkins, this);
            }
            _HideSkinsPlayers = null;
            _cachedContainers = null;
            //_noSkins = null;
        }

        #endregion Initialization

        #region Commands
        private void NoSkinCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = (BasePlayer)iplayer.Object;
            if (player == null) return;

            if (!HasPerm(player.UserIDString, PermAllow))
            {
                Message(player, "NoPerms");
                return;
            }

            if (_HideSkinsPlayers.Contains(player.userID))
            {
                _HideSkinsPlayers.Remove(player.userID);
                //permission.RevokeUserPermission(player.UserIDString, PermHideSkins);

                UpdateCurrentConnections(player, false);
                Message(player, "SkinsEnabled");
                return;
            }

            //permission.GrantUserPermission(player.UserIDString, PermHideSkins, this);
            _HideSkinsPlayers.Add(player.userID);

            UpdateCurrentConnections(player, true);
            Message(player, "SkinsDisabled");
        }
        #endregion Commands

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["SkinsDisabled"] = "Skins: <color=orange> Disabled </color>",
                ["SkinsEnabled"] = "Skins: <color=orange> Enabled </color>",
                ["NoPerms"] = "You do not have permission to do this"
            }, this);
        }

        private string GetLang(string langKey, string playerId) => lang.GetMessage(langKey, this, playerId);

        private void Message(BasePlayer player, string langKey)
        {
            if (player.IsConnected) player.ChatMessage(GetLang(langKey, player.UserIDString));
        }

        #endregion Localization

        #region Hooks
        private void OnPlayerConnected(BasePlayer player)
        {
            if (HasPerm(player.UserIDString, PermHideSkins) && HasPerm(player.UserIDString, PermAllow))
            {
                ServerMgr.Instance.Invoke(() =>
                {
                    _HideSkinsPlayers.Add(player.userID);
                    UpdateCurrentConnections(player, true);
                }, 3f);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (_HideSkinsPlayers.Contains(player.userID))
                permission.GrantUserPermission(player.UserIDString, PermHideSkins, this);
            else
                permission.RevokeUserPermission(player.UserIDString, PermHideSkins);

            _HideSkinsPlayers.Remove(player.userID);
        }

        private object? OnInventoryNetworkUpdate(PlayerInventory instance, ItemContainer container, UpdateItemContainer updateItemContainer, PlayerInventory.Type type, PlayerInventory.NetworkInventoryMode mode)
        {
            if (instance._baseEntity?.IsNpc ?? true)
                return null;

            if (type != PlayerInventory.Type.Wear)
            {
                return null;
            }
            if (mode == PlayerInventory.NetworkInventoryMode.LocalPlayer)
            {
                return null;
            }
            if (mode == PlayerInventory.NetworkInventoryMode.EveryoneButLocal)
            {
                SendUpdates(instance, updateItemContainer, true);
                return false;
            }
            if (mode == PlayerInventory.NetworkInventoryMode.Everyone)
            {
                SendUpdates(instance, updateItemContainer, false);
                return false;
            }
            return null;
        }
        #endregion Hooks

        #region Methods
        private void UpdateCurrentConnections(BasePlayer player, bool disableSkins)
        {
            var subscribers = player.net.@group.subscribers;
            foreach (var connection in subscribers)
            {
                if (connection?.player is BasePlayer current)
                {
                    if (current.userID == player.userID)
                        continue;

                    CreateAndSendUpdateItemContainer(player, current, current.inventory.containerWear, PlayerInventory.Type.Wear, disableSkins);
                }
            }
        }

        private static Dictionary<ulong, ProtoBuf.ItemContainer> _cachedContainers = new Dictionary<ulong, ProtoBuf.ItemContainer>();

        private static void CreateAndSendUpdateItemContainer(BasePlayer player, BasePlayer current, ItemContainer container, PlayerInventory.Type type, bool disableSkins)
        {
            if (container == null)
                return;

            using (var updateItemContainer = Pool.Get<UpdateItemContainer>())
            {
                container.dirty = false;

                updateItemContainer.type = (int)type;
                updateItemContainer.container = Pool.Get<List<ProtoBuf.ItemContainer>>();

                if (!_cachedContainers.TryGetValue(current.userID, out ProtoBuf.ItemContainer? savedContainer) || savedContainer?.contents == null || savedContainer.contents.Count != current.inventory.containerWear.itemList.Count)
                {
                    savedContainer = container.Save(true);
                    _cachedContainers[current.userID] = savedContainer;
                }
                if (disableSkins && savedContainer.contents != null)
                {
                    foreach (var item in savedContainer.contents)
                    {
                        item.skinid = 0;
                    }
                }
                updateItemContainer.container.Add(savedContainer);

                current.ClientRPC<UpdateItemContainer>(RpcTarget.Player("UpdatedItemContainer", player), updateItemContainer);
            }
        }

        private void SendUpdates(PlayerInventory playerInventory, UpdateItemContainer updateItemContainer, bool updateLocal)
        {
            BasePlayer basePlayer = playerInventory._baseEntity;
            if (basePlayer?.net?.group == null)
                return;

            _cachedContainers[basePlayer.userID] = updateItemContainer.container[0];

            var subscribers = basePlayer.net.group.subscribers;

            List<Connection> connectionsSkinned = Pool.Get<List<Connection>>();
            List<Connection> connectionsNoSkin = Pool.Get<List<Connection>>();

            foreach (var connection in subscribers)
            {
                if (connection?.player is BasePlayer current)
                {
                    if (updateLocal && current.userID == basePlayer.userID)
                    {
                        continue;
                    }

                    if (_HideSkinsPlayers.Contains(current.userID))
                    {
                        connectionsNoSkin.Add(connection);
                    }
                    else
                    {
                        connectionsSkinned.Add(connection);
                    }
                }
            }

            if (connectionsSkinned.Count > 0)
            {
                basePlayer.ClientRPC<UpdateItemContainer>(RpcTarget.Players("UpdatedItemContainer", connectionsSkinned), updateItemContainer);
            }

            if (connectionsNoSkin.Count > 0)
            {
                foreach (var con in updateItemContainer.container)
                {
                    foreach (var item in con.contents)
                    {
                        item.skinid = 0;
                    }
                }
                basePlayer.ClientRPC<UpdateItemContainer>(RpcTarget.Players("UpdatedItemContainer", connectionsNoSkin), updateItemContainer);
            }

            Pool.FreeUnmanaged(ref connectionsSkinned);
            Pool.FreeUnmanaged(ref connectionsNoSkin);
        }

        #endregion Methods

        #region Harmony
        [HarmonyPatch(typeof(PlayerCorpse), "Save"), AutoPatch]
        private static class PlayerCorpse_Save_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(PlayerCorpse __instance, BaseNetworkable.SaveInfo info)
            {
                if (info.forConnection == null)
                    return;

                if (!_HideSkinsPlayers.Contains(info.forConnection.userid))
                    return;

                if (info.msg.storageBox == null)
                    return;

                foreach (var item in info.msg.storageBox.contents.contents)
                {
                    item.skinid = 0;
                }
            }
        }

        [HarmonyPatch(typeof(BaseNetworkable), "SendNetworkGroupChange"), AutoPatch] // when a player respawns
        private static class BaseNetworkable_SendNetworkGroupChange_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(BaseNetworkable __instance)
            {
                if (__instance.net.connection == null)
                    return;

                if (__instance.net.connection.player is not BasePlayer basePlayer)
                    return;

                bool hidingSkins = _HideSkinsPlayers.Contains(__instance.net.connection.userid);

                foreach (var subscriber in basePlayer.net.group.subscribers)
                {
                    if (subscriber.userid == __instance.net.connection.userid)
                        continue;

                    if (subscriber.player is not BasePlayer subPlayer || subPlayer.IsNpc)
                        continue;

                    bool hidingSkins2 = _HideSkinsPlayers.Contains(subscriber.userid);

                    if (hidingSkins2)
                    {
                        CreateAndSendUpdateItemContainer(subPlayer, basePlayer, basePlayer.inventory.containerWear, PlayerInventory.Type.Wear, true);
                    }

                    if (hidingSkins)
                    {
                        CreateAndSendUpdateItemContainer(basePlayer, subPlayer, subPlayer.inventory.containerWear, PlayerInventory.Type.Wear, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Networkable), "OnGroupTransition"), AutoPatch] //moving between grids
        private static class BaseNetworkable_OnGroupTransition_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Networkable __instance, Network.Visibility.Group oldGroup)
            {
                if (__instance.connection == null)
                    return;

                if (__instance.connection.player is not BasePlayer basePlayer)
                    return;

                bool hidingSkins = _HideSkinsPlayers.Contains(__instance.connection.userid);

                foreach (var subscriber in basePlayer.net.group.subscribers)
                {
                    if (subscriber.userid == __instance.connection.userid)
                        continue;

                    if (subscriber.player is not BasePlayer subPlayer || subPlayer.IsNpc)
                        continue;

                    bool hidingSkins2 = _HideSkinsPlayers.Contains(subscriber.userid);

                    if (hidingSkins2)
                    {
                        CreateAndSendUpdateItemContainer(subPlayer, basePlayer, basePlayer.inventory.containerWear, PlayerInventory.Type.Wear, true);
                    }

                    if (hidingSkins)
                    {
                        CreateAndSendUpdateItemContainer(basePlayer, subPlayer, subPlayer.inventory.containerWear, PlayerInventory.Type.Wear, true);
                    }
                }
            }
        }
        #endregion Harmony

        #region Helpers

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private void AddLocalizedCommand(string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages)
                {
                    if (!message.Key.Equals(command)) continue;

                    if (string.IsNullOrEmpty(message.Value)) continue;

                    AddCovalenceCommand(message.Value, command);
                }
            }
        }
        #endregion Helpers
    }
}