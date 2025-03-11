using Oxide.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fix", "playermodel", "1.0.0")]
    public class Fix : RustPlugin
    {
        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (type == AntiHackType.FlyHack)
            {
                if (amount > 500f)
                {
                    Server.Command($"banid {player.UserIDString}");
                }
            }
            return null;
        }
    }
}