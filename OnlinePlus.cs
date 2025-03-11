using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("OnlinePlus", "Sempai#3239", "1.0.1")]
    [Description("OnlinePlus")]
    class OnlinePlus : RustPlugin
    {
        #region Variables
        private const string Permfromuse = "perm.fromuse";
        private const string DeniedEffectPrefab = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        private string Moderperm = "perm.moder";
        #endregion

        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(Permfromuse, this);
            permission.RegisterPermission(Moderperm, this);
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noPerm"] = "You don't have access to this command.",
                ["players"] = "Players list: ({0}): \n{1}",
                ["PlayerAlone"] = "You are alone on server! Your name: {0}, SteamID {1}",
                ["onlineInfo"] = "Online: {0} player, {1} moderators, {2} admins.",
            }, this);
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noPerm"] = "Недостаточно прав.",
                ["PlayerAlone"] = "Вы одни на сервере! Ваш ник: {0}, SteamID {1}",
                ["players"] = "Список Игроков ({0}):\n{1}",
                ["onlineInfo"] = "Сейчас на сервере {0} игроков, {1} модераторов, {2} администраторов.",
            }, this, "ru");
        }

        #endregion

        #region Commands
        
        [ChatCommand("online")]
        void Chat_Online(BasePlayer player)
        {
            int admins = BasePlayer.activePlayerList.Count(p => p.IsAdmin);
            int playerAmount = BasePlayer.activePlayerList.Count;
            int moderationAmount = BasePlayer.activePlayerList.Count(p => HasPermission(p, Moderperm) && p.IsAdmin == false);
            
            if (HasPermission(player, Permfromuse) == false)
            {
                InChat(player, "noPerm");
                Effect.server.Run(DeniedEffectPrefab, player.GetNetworkPosition());
                return;
            }
            
            InChat(player, "onlineInfo", playerAmount, moderationAmount, admins);
        }
        
        [ChatCommand("players")]
        void Chat_Players(BasePlayer player)
        {
            if (HasPermission(player, Permfromuse) == false)
            {
                InChat(player, "noPerm");
                Effect.server.Run(DeniedEffectPrefab, player.GetNetworkPosition());
                return;
            }
            
            int playersAmount = BasePlayer.activePlayerList.Count;
            if (playersAmount == 1)
            {
                InChat(player, "PlayerAlone", player.displayName, player.UserIDString);
                return;
            }

            string formattedList = "";
            
            for (var i = BasePlayer.activePlayerList.Count - 1; i >= 0 ; i--)
            {
                var playerTarget = BasePlayer.activePlayerList[i];
                if(playerTarget == null || playerTarget.IsConnected == false)
                    continue;
                
                formattedList += $"{playerTarget.displayName} ({playerTarget.UserIDString})\n";
            }
            
            InChat(player, "players", playersAmount, formattedList);
        }

        #endregion

        #region Utils

        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm) || player.IsAdmin;

        private string GetLocal(string messageKey, string userId) => lang.GetMessage(messageKey, this, userId);
        
        private void InChat(BasePlayer player, string msgId, params object[] args)
        {
            PrintToChat(player, string.Format(GetLocal(msgId, player.UserIDString), args));
        }

        #endregion
    }
}