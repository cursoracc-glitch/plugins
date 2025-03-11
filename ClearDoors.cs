using System;
using UnityEngine;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Удаление дверей с РТ", "poof.", "1.0.0")]
    class ClearDoors : RustPlugin
    {
		private void clearallchest() 
		{ 

		var ents = UnityEngine.Object.FindObjectsOfType<BaseEntity>(); 
		foreach(var ent in ents) 
		{ 
		if(ent.PrefabName.Contains("door.hinged.industrial_a") || ent.PrefabName.Contains("door.hinged.garage_a") || ent.PrefabName.Contains("door.hinged.bunker.door") || ent.PrefabName.Contains("door.hinged.security") || ent.PrefabName.Contains("door.hinged.vent.prefab")) 
			{ 
			ent.Kill(); 
			} 
		} 
		}
 
		void OnServerInitialized() 
		{
		clearallchest();
        PrintWarning("Плагин разработан специально для сервера SnyxCloud [Автор плагина: poof.]");
        PrintWarning("Группа разработчика: vk.com/poof.rust");
		}

        #region Helpers

        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T) Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        #endregion
    }
}
