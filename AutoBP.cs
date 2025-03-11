using System.Collections.Generic;
using System.Linq;

using System;
using UnityEngine;
using Network;

namespace Oxide.Plugins
{
    [Info("AutoBP", "Ryamkk", "1.0.3")]
    public class AutoBP : RustPlugin
    {
        void Loaded()
        {
            permission.RegisterPermission("autobp.use", this);
        }

        private void OnServerInitialized()
        {
            timer.Every(30f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList.ToList())
                {
                    if(permission.UserHasPermission(player.UserIDString, "autobp.use")) 
                        player.blueprints.UnlockAll();
                }
            });
        }
		

        private List<string> BPes = new List<string>
        {
            "syringe.medical",
            "pistol.revolver",
            "ammo.pistol",
        };

        private void OnPlayerInit(BasePlayer player)
        {
            if(permission.UserHasPermission(player.UserIDString, "autobp.use")) 
                player.blueprints.UnlockAll();
            
            foreach (var check in BPes)
            {
                var def = ItemManager.FindItemDefinition(check);
                if (!player.blueprints.IsUnlocked(def))
                    player.blueprints.Unlock(def); 
            }
        }
    }
}