using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GiveLock", "Mercury", "1.0.0")]
    [Description("Supported Discord - Mercury#5212")]
    public class GiveLock : RustPlugin
    {

        private object OnServerCommand(ConsoleSystem.Arg arg)
         {
             BasePlayer player = arg.Player();
		   		 		  						  	   		  	   		  						  		  		   		 
             if (player == null || arg.cmd.FullName == "chat.say") return null;
             if (permission.UserHasPermission(player.UserIDString, PermissionImmunitete))
                 return null; 
             
             if (arg.cmd.Name.Contains("give")) return false;

             return null;
         }

        private void OnServerInitialized() => permission.RegisterPermission(PermissionImmunitete, this);
        private const String PermissionImmunitete = "givelock.ignore";
    }
}