using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Weapon Drop", "Fujikura", "1.2.0")]
	[Description("Prevents dropping of active weapon when players start to die")]
    class NoWeaponDrop : CovalencePlugin
    {
        [PluginReference]
		Plugin RestoreUponDeath;
		
		private const string permissionName = "noweapondrop.active";

		private bool Changed = false;
		private bool usePermission;
		
		private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
		
		void LoadVariables()
        {
			usePermission = Convert.ToBoolean(GetConfig("Settings", "Use permissions", false));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }
		
		protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }
		
		void Init()
		{
			LoadVariables();
			permission.RegisterPermission(permissionName, this);
		}
		
		object CanDropActiveItem(BasePlayer player)
		{
			if (player.IsNpc || (usePermission && !permission.UserHasPermission(player.UserIDString, permissionName)))
				return null;
			return false;
		}
	}
}
