using ConVar;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("StopDamageMan", "Sempai#3239", "0.0.2")]
    class StopDamageMan : RustPlugin
    {
        #region Varibles
        public List<ulong> PlayerStopDamage = new List<ulong>();
        #endregion

        #region Hooks

        void OnServerInitialized()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("stopdamageman.use", this);
            PlayerStopDamage = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>("StopDamageMan/PlayerList");
        }

        void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("StopDamageMan/PlayerList", PlayerStopDamage);
        }

        void OnServerSave()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("StopDamageMan/PlayerList", PlayerStopDamage);
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            BasePlayer damager = hitInfo.InitiatorPlayer;
            if (entity == null || hitInfo == null || damager == null) return;
            if(PlayerStopDamage.Contains(damager.userID))
            {
                hitInfo.damageTypes.ScaleAll(0);
            }   
        }

        #endregion

        #region Commands

        [ChatCommand("sdm_add")]
        void AddPlayerNoDamageList(BasePlayer player, string command, string[] args)
        {
            if(!player.IsAdmin || !permission.UserHasPermission(player.UserIDString, "stopdamageman.use")) return; 

            var targetPlayer = FindPlayerByPartialName(args[0]);
            if (targetPlayer == null)
            {

                SendChat(lang.GetMessage("NOT_FOUND", this), player);
                return;
            }

            if (!PlayerStopDamage.Contains(targetPlayer.userID))
            {
                PlayerStopDamage.Add(targetPlayer.userID);
                SendChat(String.Format(lang.GetMessage("PLAYER_ACCEPT", this), targetPlayer.displayName), player);
            }
            else { SendChat(String.Format(lang.GetMessage("PLAYER_NDM", this), targetPlayer.displayName), player); }
            
        }

        [ChatCommand("sdm_remove")]
        void RemovePlayerNoDamageList(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin || !permission.UserHasPermission(player.UserIDString, "stopdamageman.use")) return;

            var targetPlayer = FindPlayerByPartialName(args[0]);
            if (targetPlayer == null)
            {
                SendChat(lang.GetMessage("NOT_FOUND", this), player);
                return;
            }

            if (PlayerStopDamage.Contains(targetPlayer.userID))
            {
                PlayerStopDamage.Remove(targetPlayer.userID);
                SendChat(String.Format(lang.GetMessage("PLAYER_DICLINE", this), targetPlayer.displayName), player);
            }
            else { SendChat(String.Format(lang.GetMessage("PLAYER_YDM", this), targetPlayer.displayName), player); }

        }

        #endregion

        #region HelpMetods

        private BasePlayer FindPlayerByPartialName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.net == null || p.net.connection == null)
                    continue;

                if (p.displayName == name)
                {
                    if (player != null)
                        return null;
                    player = p;
                }
            }

            if (player != null)
                return player;
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.net == null || p.net.connection == null)
                    continue;

                if (p.displayName.ToLower().IndexOf(name) >= 0)
                {
                    if (player != null)
                        return null;
                    player = p;
                }
            }

            return player;
        }

        [PluginReference] Plugin IQChat;
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning("Alert#3432 : Языковой файл загружается...");
            timer.In(2.5f, () => {
                Dictionary<string, string> Lang = new Dictionary<string, string>
                {
                    ["NOT_FOUND"] = "Введите конкретное значение или ник",
                    ["PLAYER_ACCEPT"] = "Игроку {0} успешно отключен урон",
                    ["PLAYER_DICLINE"] = "Игроку {0} успешно включен урон",
                    ["PLAYER_NDM"] = "У игрока {0} уже отключен урон",
                    ["PLAYER_YDM"] = "У игрока {0} уже включен урон",
                };
                lang.RegisterMessages(Lang, this, "en");
                PrintWarning("Языковой файл успешно загружен");
            });
        }
        #endregion
    }
}
