namespace Oxide.Plugins
{
    [Info("AuthLimits", "Hougan", "0.0.1")]
    [Description("Ограничение на авторизация в шкафах/замках/турелях")]
    public class AuthLimits : RustPlugin
    {
        #region Variables

        private int MaxAuthorize = 5;
        private int DeAuthNumber = 1;

        #endregion

        #region Hooks

        private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege.authorizedPlayers.Count >= MaxAuthorize)
            {
                var DeAuthId = privilege.authorizedPlayers[DeAuthNumber - 1];
                BasePlayer DeAuthorizated = BasePlayer.FindByID(DeAuthId.userid);

                SendReply(player, $"Вы превысили лимит авторизаций в одном шкафу, поэтому <color=#e34747>последний авторизовавшийся игрок</color> был выписан!");
                privilege.authorizedPlayers.RemoveAt(DeAuthNumber - 1);
                PrintWarning($"Игрок {player.displayName} [{player.userID}] превысил лимит авторизации в шкафу!");
            }
        }

        private object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
            if (turret.authorizedPlayers.Count >= MaxAuthorize)
            {
                SendReply(player, "Вы <color=#e34747>не можете</color> превысить лимит авторизации в турели!");
                PrintWarning($"Игрок {player.displayName} [{player.userID}] пытался превысить лимит авторизации в туреле!");
                return false;
            }
            return null;
        }

        private object OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
        {
            if (codeLock.whitelistPlayers.Count >= MaxAuthorize)
            {
                SendReply(player, "Вы <color=#e34747>не можете</color> превысить лимит авторизаций в замке!");
                PrintWarning($"Игрок {player.displayName} [{player.userID}] пытался превысить лимит авторизации в замке!");
                return false;
            }
            return null;
        }

        #endregion
    }
}