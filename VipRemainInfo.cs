using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("VipRemainInfo", "Nimant", "1.0.5", ResourceId = 0)]
    class VipRemainInfo : RustPlugin
    {            		
				
		#region Variables 		
		
		[PluginReference]
		private Plugin Grant;

		private Dictionary<ulong, bool>	ShowedVipInfo = new Dictionary<ulong, bool>();				
		
		#endregion
		
		#region Hooks
		
		private void Init() 
		{
			LoadConfigVariables();
			LoadDefaultMessages();
			
			if (configData.ShowDelay < 0f)
			{
				Unsubscribe(nameof(OnPlayerConnected));
				Unsubscribe(nameof(OnPlayerSleepEnded));
			}
		}
		
		private void OnServerInitialized()
		{
			if (Grant == null)			
				PrintWarning("Не найден плагин 'Grant'. Работа плагина невозможна !");						
		}
				
		private void OnPlayerConnected(BasePlayer player)
        {
			if (player == null) return;
			
            if (!ShowedVipInfo.ContainsKey(player.userID)) 
				ShowedVipInfo.Add(player.userID, false);
			else
				ShowedVipInfo[player.userID] = false;
		}	
		
		private void OnPlayerSleepEnded(BasePlayer player)
        {					
			if (player == null) return;
			
            if (!ShowedVipInfo.ContainsKey(player.userID)) return;
			
			if (!ShowedVipInfo[player.userID])
				timer.Once(configData.ShowDelay, () => ShowRemainVipInfo(player));
        }
		
		#endregion
		
		#region Commands
		
		[ChatCommand("vipinfo")]
        private void cmdVipInfo(BasePlayer player, string command, string[] args)
        {
			if (player == null) return;
			ShowRemainVipInfo(player, false);
		}
		
		#endregion
		
		#region Main
		
		private void ShowRemainVipInfo(BasePlayer player, bool isSilent = true)
		{
			if (Grant == null) return;
			
			var result = "";
			var showHead = false;
			var color = "";
			
			var groupInfo = (Dictionary<string, int>)(Grant.CallHook("GetGroups", player.userID));
			if (groupInfo != null)
			{
				foreach(var info in groupInfo)
				{					
					if (!showHead)
					{						
						result += GetLangMessage("VRI.PASS_TIME") + "\n";
						showHead = true;
					}
					
					var name = info.Key;
					if (configData.GroupsNewName.ContainsKey(info.Key)) 
						name = configData.GroupsNewName[info.Key];						
					
					if (configData.GroupsColor.TryGetValue(info.Key, out color))						
						result += string.Format(GetLangMessage("VRI.PRIV_TEXT"), color, name, GetTime(info.Value)) + "\n";
					else
						if (configData.GroupsColor.TryGetValue("default", out color))							
							result += string.Format(GetLangMessage("VRI.PRIV_TEXT"), color, name, GetTime(info.Value)) + "\n";
						else
							result += string.Format(GetLangMessage("VRI.PRIV_TEXT"), "white", name, GetTime(info.Value)) + "\n";							
				}								
			}
			
			var privsInfo = (Dictionary<string, int>)(Grant.CallHook("GetPermissions", player.userID));
				
			if (privsInfo != null)
			{
				foreach(var info in privsInfo)
				{					
					if (!showHead)
					{						
						result += GetLangMessage("VRI.PASS_TIME") + "\n";
						showHead = true;
					}	
					
					var name = info.Key;
					if (configData.GroupsNewName.ContainsKey(info.Key))					
						name = configData.GroupsNewName[info.Key];						
					
					if (configData.GroupsColor.TryGetValue(info.Key, out color))						
						result += string.Format(GetLangMessage("VRI.PRIV_TEXT"), color, name, GetTime(info.Value)) + "\n";
					else
						if (configData.GroupsColor.TryGetValue("default", out color))							
							result += string.Format(GetLangMessage("VRI.PRIV_TEXT"), color, name, GetTime(info.Value)) + "\n";
						else
							result += string.Format(GetLangMessage("VRI.PRIV_TEXT"), "white", name, GetTime(info.Value)) + "\n";							
				}
			}
			
			if (!string.IsNullOrEmpty(result))
				SendReply(player, result.TrimEnd('\n'));
			else
				if (!isSilent)
					SendReply(player, GetLangMessage("VRI.NO_PRIVS"));
			
			if (isSilent)
				ShowedVipInfo[player.userID] = true;
		}		
		
		private string GetTime(int rawSeconds)
		{							
			int days    = (int)Math.Truncate((((decimal)rawSeconds/60)/60)/24);
			int hours   = (int)Math.Truncate((((decimal)rawSeconds-days*24*60*60)/60)/60);
			int minutes = (int)Math.Truncate((((decimal)rawSeconds-days*24*60*60)/60)%60);			
			
			string time = "";
		
			if (days!=0)
				time += $"{days}д ";			
			if (hours!=0)
				time += $"{hours}ч ";			
			if (minutes!=0)
				time += $"{minutes}м ";			
			
			if (string.IsNullOrEmpty(time))
				time = "несколько секунд";
			
			return time;
		}
		
		#endregion
		
		#region Lang
		
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> 
            {                
				{"VRI.PASS_TIME", "Остаток времени у ваших привилегий:"},
				{"VRI.PRIV_TEXT", "Привилегия <color={0}>{1}</color> - осталось {2}"},
				{"VRI.NO_PRIVS", "У вас нет активных привилегий"}
            }, this);
        }

        private string GetLangMessage(string key, string steamID = null) => lang.GetMessage(key, this, steamID);
		
		#endregion
		
		#region Config   		
		
		private static ConfigData configData;
		
        private class ConfigData
        {
			[JsonProperty(PropertyName = "Цвета вип групп и отдельных привилегий")]
            public Dictionary<string, string> GroupsColor;		
			[JsonProperty(PropertyName = "Названия вип групп и отдельных привилегий")]
			public Dictionary<string, string> GroupsNewName;
			[JsonProperty(PropertyName = "Задержка на показ остатка времени у групп и отдельных привилегий при заходе на сервер (-1 = отключить показ)")]
			public float ShowDelay;
        }				                        
		
        private void LoadDefaultConfig()
        {
			configData = new ConfigData
            {
				GroupsColor = new Dictionary<string, string>()
				{
					{"vip", "#0FED02"},	
					{"prime", "#A202ED"},
					{"kits.bigboss", "#FF170A"},
					{"default", "#FFA500"}
				},
				GroupsNewName = new Dictionary<string, string>()
				{
					{"vip", "VIP"},	
					{"prime", "PRIME"},
					{"kits.bigboss", "Кит BigBoss"}					
				},
				ShowDelay = 2f
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }
        
		private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        
		private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion

    }	
	
}