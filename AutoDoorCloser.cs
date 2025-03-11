using System;
using Oxide.Core;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("AutoDoorCloser", "Ryamkk", "1.0.5", ResourceId = 1924)]
 
    class AutoDoorCloser : RustPlugin
    {
	    [PluginReference]
        Plugin NoEscape;
		
		#region Variables
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("AutoDoorCloser");
        List<string> playerPrefs = new List<string>();
		bool CancelOnKill = false;
		#endregion
		
		#region Variables Configuration
		private string Permission; // Названия привилегии для использования команды.
		private bool PermissionSupport; // Разрешить использования функционала плагина только тем игрокам у которых есть привилегия.
		private int DelayDoor; // Задержка перед автоматическим закрытием дверей (в секундах).
		private bool UseRaidBlocker; //Разрешить/Запретить использования автоматического закрытия дверей при РеидБлоке.
		#endregion
		
		#region Configuration
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "A. Задержка перед автоматическим закрытием дверей (в секундах).", out DelayDoor, 10);
            GetVariable(Config, "B. (Разрешить/Запретить) использования автоматического закрытия дверей при РеидБлоке.", out UseRaidBlocker, false);
			GetVariable(Config, "C. (Разрешить/Запретить) использования функционала плагина только тем игрокам у которых есть привилегия.", out PermissionSupport, false);
			GetVariable(Config, "D. Названия привилегии для использования команды.", out Permission, "autodoorcloser.access");	
            SaveConfig();
        }
        #endregion

		#region OxideCore
		void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || !door.IsOpen() || door.OwnerID == 0 || door.LookupPrefab().name.Contains("shutter")) return;
            if (PermissionSupport && !IsAllowed(player.UserIDString, Permission)) return;
			if (IsRaidBlocked(player.UserIDString))
			{
                SendReply(player, GetMessage("RAID.BLOCKED", player));
                return;				
			}

            if (playerPrefs.Contains(player.UserIDString)) return;

            timer.Once(DelayDoor, () =>
            {
                if (!door || !door.IsOpen()) return;
                if (CancelOnKill && player.IsDead()) return;

                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
            });
        }
		#endregion
		
		#region Oxide
		void OnServerInitialized() => LoadDefaultConfig();
        void Init()
        {
            LoadDefaultMessages();
            permission.RegisterPermission(Permission, this);
            playerPrefs = dataFile.ReadObject<List<string>>();
        }
		
		bool IsRaidBlocked(string targetId) => UseRaidBlocker && (bool)(NoEscape?.Call("IsRaidBlockedS", targetId) ?? false);
		#endregion

		#region ChatCommand
        [ChatCommand("ad")]
        void AutoDoorCommand(BasePlayer player, string command, string[] args)
        {
            if(PermissionSupport)
			{
			    if (!IsAllowed(player.UserIDString, Permission))
                {
                    SendReply(player, GetMessage("NO.ACCESS", player, command));
                    return;
                }
			}
			
			if (IsRaidBlocked(player.UserIDString))
			{
                SendReply(player, GetMessage("RAID.BLOCKED", player));
                return;				
			}

            if (args.Length == 0)
            {
                SendReply(player, GetMessage("CMD.AD.HELP", player, playerPrefs.Contains(player.UserIDString) ? "<color=#8e6874>Выключен</color>" : "<color=#8e6874>Включено</color>"));
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "on":
                        if (!playerPrefs.Contains(player.UserIDString))
                        {
							SendReply(player, GetMessage("ALREADY.ON", player));
                            return;
                        }

                        playerPrefs.Remove(player.UserIDString);
						SendReply(player, GetMessage("AUTO.CLOSE.ON", player));
                        break;
                    case "off":
                        if (playerPrefs.Contains(player.UserIDString))
                        {
							SendReply(player, GetMessage("ALREADY.OFF", player));
                            return;
                        }
                        playerPrefs.Add(player.UserIDString);
						SendReply(player, GetMessage("AUTO.CLOSE.OFF", player));
                        break;
                }

                dataFile.WriteObject(playerPrefs);
            }
        }
		#endregion
		
	    #region Localization
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>() 
            {
                ["NO.ACCESS"] = "У вас нет <color=#8e6874>доступа</color> к этой команде!",
				
				["RAID.BLOCKED"] = "<size=14>Был активирован <color=#8e6874>Рейд-Блок</color></size>\n<size=12>-автоматическое закрытие дверей недоступно!</size>",
				
				["ALREADY.ON"] = "Автоматическое закрытие <color=#8e6874>дверей</color> уже включено", 
                ["AUTO.CLOSE.ON"] = "Вы <color=#8e6874>включили</color> автоматическое закрытие дверей",
				
				["ALREADY.OFF"] = "Автоматическое закрытия <color=#8e6874>дверей</color> уже выключено",
                ["AUTO.CLOSE.OFF"] = "Вы <color=#8e6874>выключили</color> автоматическое закрытие дверей",
				
                ["CMD.AD.HELP"] = "<size=14>Автоматическое <color=#8e6874>закрытие</color> дверей: {0}</size>\n<size=12><color=#8e6874>/ad on</color> - Включить автоматическое закрытие дверей.</size>\n<size=12><color=#8e6874>/ad off</color> - Выключить автоматическое закрытие дверей.</size>",
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>() 
            {
                ["NO.ACCESS"] = "You do not have <color=#8e6874>access</color> to this command!",
				
				["RAID.BLOCKED"] = "<size=14>The <color=#8e6874>Raid-Block</color></size>\n<size=12>Automatic door closing is unavailable!</size>",
				
				["ALREADY.ON"] = "Automatic closing of <color=#8e6874>doors</color> is already enabled", 
                ["AUTO.CLOSE.ON"] = "You have <color=#8e6874>enabled</color> automatic door closing",
				
				["ALREADY.OFF"] = "Automatic closing of <color=#8e6874>doors</color> is already turned off",
                ["AUTO.CLOSE.OFF"] = "You <color=#8e6874>disabled</color> the automatic door closure",
				
                ["CMD.AD.HELP"] = "<size=14>Automatic <color=#8e6874>closing</color> doors: {0}</size>\n<size=12><color=#8e6874>/ad on</color> - Enable automatic door closing.</size>\n<size=12><color=#8e6874>/ad off</color> - Turn off automatic door closing.</size>",
            }, this, "en");
        }
		#endregion
		
		#region Helpers
        string GetMessage(string key, BasePlayer player, params string[] args) => String.Format(lang.GetMessage(key, this, player.UserIDString), args);
		bool IsAllowed(string id, string perm) => permission.UserHasPermission(id, perm);
		T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
		#endregion
    }
}
