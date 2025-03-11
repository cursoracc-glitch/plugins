using Newtonsoft.Json;
using System;
using Oxide.Core.Plugins;
		   		 		  						  	   		  		 			  	  			  						  		  
namespace Oxide.Plugins
{
    [Info("XStashLogs", "Sempai#3239", "1.0.201")]
    class XStashLogs : RustPlugin
	{
        protected override void SaveConfig() => Config.WriteObject(config);
	
				
				
		private void OnServerInitialized()
		{		
			PrintWarning("\n-----------------------------\n" +
			"     Author - Sempai\n" +
			"     VK - vk.com/rustnastroika\n" +
			"     Discord - Sempai#3239\n" +
			"     Config - v.916\n" +
			"-----------------------------");
		}
		
		object CanSeeStash(BasePlayer player, StashContainer stash)
		{
			if (player == null || stash == null) return null;
			
			DateTime time = DateTime.Now;
			
			if (stash.OwnerID == player.userID)
			{
				string oss = $"```[{time}] | Игрок: {player.displayName} | {player.userID} - выкопал свой тайник. Координаты: {stash.transform.position}```";
				
				if (config.See.ConsoleSeeStashOwner) PrintWarning(oss);
				if (config.See.LogSeeStashOwner) LogToFile($"StashLogsSee", oss, this);
				
				if (config.See.DiscordSeeStashOwner)
				{
				    if (DiscordMessages) DiscordMessages?.Call("API_SendTextMessage", config.Discord.WebHook, oss);
				        else PrintWarning(error);
				}
			}
			else if (player.currentTeam != 0 && player.Team.members.Contains(stash.OwnerID))
			{
				string fss = $"```[{time}] | Игрок: {player.displayName} | {player.userID} - выкопал тайник друга. Тайник игрока: ({stash.OwnerID}). Координаты: {stash.transform.position}```";
				
				if (config.See.ConsoleSeeStashFriend) PrintWarning(fss);
				if (config.See.LogSeeStashFriend) LogToFile($"StashLogsSee", fss, this);
				
				if (config.See.DiscordSeeStashFriend)
				{
				    if (DiscordMessages) DiscordMessages?.Call("API_SendTextMessage", config.Discord.WebHook, fss);
				        else PrintWarning(error);
				}
			}
			else
			{
				string ss = $"```[{time}] | Игрок: {player.displayName} | {player.userID} - выкопал чужой тайник. Тайник игрока: ({stash.OwnerID}). Координаты: {stash.transform.position}```";
				
				if (config.See.ConsoleSeeStash) PrintWarning(ss);
				if (config.See.LogSeeStash) LogToFile($"StashLogsSee", ss, this);
				
				if (config.See.DiscordSeeStash)
				{
				    if (DiscordMessages) DiscordMessages?.Call("API_SendTextMessage", config.Discord.WebHook, ss);
				        else PrintWarning(error);
				}
			}				
			
			return null;
		}
		   		 		  						  	   		  		 			  	  			  						  		  
        private class StashConfig
        {		
			[JsonProperty("Настройка сообщений/логов закопанных тайников")]
            public SeeSetting See;						
			
			public static StashConfig GetNewConfiguration()
            {
                return new StashConfig
                {
					Discord = new DiscordSetting
					{
						WebHook = "$WEBHOOK-916"
						
					},
					Hide = new HideSetting
					{
						ConsoleHideStashOwner = true,
						ConsoleHideStashFriend = true,
						ConsoleHideStash = true,						
						DiscordHideStashOwner = false,
						DiscordHideStashFriend = false,
						DiscordHideStash = false,
						LogHideStashOwner = true,
						LogHideStashFriend = true,
						LogHideStash = true
					},
					See = new SeeSetting
					{
						ConsoleSeeStashOwner = true,
						ConsoleSeeStashFriend = true,
						ConsoleSeeStash = true,						
						DiscordSeeStashOwner = false,
						DiscordSeeStashFriend = false,
						DiscordSeeStash = false,
						LogSeeStashOwner = true,
						LogSeeStashFriend = true,
						LogSeeStash = true
					}
				};
			}
			internal class DiscordSetting
            {
                [JsonProperty("WebHook канала для сообщений")] public string WebHook;	
            }	
             
			internal class SeeSetting
			{
				[JsonProperty("Включить сообщения в консоль закопанных своих тайников")] public bool ConsoleSeeStashOwner;				
				[JsonProperty("Включить сообщения в консоль закопанных тайников друзей")] public bool ConsoleSeeStashFriend;				
				[JsonProperty("Включить сообщения в консоль закопанных чужих тайников")] public bool ConsoleSeeStash;					
				[JsonProperty("Включить сообщения в Discord закопанных своих тайников")] public bool DiscordSeeStashOwner;				
				[JsonProperty("Включить сообщения в Discord закопанных тайников друзей")] public bool DiscordSeeStashFriend;				
				[JsonProperty("Включить сообщения в Discord закопанных чужих тайников")] public bool DiscordSeeStash;				
				[JsonProperty("Включить логирование закопанных своих тайников")] public bool LogSeeStashOwner;				
				[JsonProperty("Включить логирование закопанных тайников друзей")] public bool LogSeeStashFriend;				
				[JsonProperty("Включить логирование закопанных чужих тайников")] public bool LogSeeStash;
			}
			[JsonProperty("Настройка сообщений/логов выкопанных тайников")]
            public HideSetting Hide;			
		   		 		  						  	   		  		 			  	  			  						  		  
            internal class HideSetting
			{
				[JsonProperty("Включить сообщения в консоль выкопанных своих тайников")] public bool ConsoleHideStashOwner;				
				[JsonProperty("Включить сообщения в консоль выкопанных тайников друзей")] public bool ConsoleHideStashFriend;				
				[JsonProperty("Включить сообщения в консоль выкопанных чужих тайников")] public bool ConsoleHideStash;								
				[JsonProperty("Включить сообщения в Discord выкопанных своих тайников")] public bool DiscordHideStashOwner;				
				[JsonProperty("Включить сообщения в Discord выкопанных тайников друзей")] public bool DiscordHideStashFriend;				
				[JsonProperty("Включить сообщения в Discord выкопанных чужих тайников")] public bool DiscordHideStash;				
				[JsonProperty("Включить логирование выкопанных своих тайников")] public bool LogHideStashOwner;				
				[JsonProperty("Включить логирование выкопанных тайников друзей")] public bool LogHideStashFriend;				
				[JsonProperty("Включить логирование выкопанных чужих тайников")] public bool LogHideStash;
			}			
			
			[JsonProperty("Настройка Discord")]
            public DiscordSetting Discord;			
        }
				
		[PluginReference] private Plugin DiscordMessages;

		protected override void LoadConfig() 
        {
            base.LoadConfig(); 
			 
			try
			{
				config = Config.ReadObject<StashConfig>();
			}
			catch  
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			 
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = StashConfig.GetNewConfiguration();
		
				
				
		private StashConfig config;
		
				
				
		public string error = "Ошибка отправки логов в Discord. Плагин - DiscordMessages не установлен!";
		
		object CanHideStash(BasePlayer player, StashContainer stash)
		{
			if (player == null || stash == null) return null;
			
			DateTime time = DateTime.Now;
			
			if (stash.OwnerID == player.userID)
			{
				string osh = $"```[{time}] | Игрок: {player.displayName} | {player.userID} - закопал свой тайник. Координаты: {stash.transform.position}```";
				
				if (config.Hide.ConsoleHideStashOwner) PrintWarning(osh);
				if (config.Hide.LogHideStashOwner) LogToFile($"StashLogsHide", osh, this);
				
				if (config.Hide.DiscordHideStashOwner)
				{
				    if (DiscordMessages) DiscordMessages?.Call("API_SendTextMessage", config.Discord.WebHook, osh); 
				        else PrintWarning(error);
				}
			}
			else if (player.currentTeam != 0 && player.Team.members.Contains(stash.OwnerID))
			{
				string fsh = $"```[{time}] | Игрок: {player.displayName} | {player.userID} - закопал тайник друга. Тайник игрока: ({stash.OwnerID}). Координаты: {stash.transform.position}```";
				
				if (config.Hide.ConsoleHideStashFriend) PrintWarning(fsh);
				if (config.Hide.LogHideStashFriend) LogToFile($"StashLogsHide", fsh, this);
				
				if (config.Hide.DiscordHideStashFriend)
				{
				    if (DiscordMessages) DiscordMessages?.Call("API_SendTextMessage", config.Discord.WebHook, fsh);
				        else PrintWarning(error);
				}
			}
			else
			{
				string sh = $"```[{time}] | Игрок: {player.displayName} | {player.userID} - закопал чужой тайник. Тайник игрока: ({stash.OwnerID}). Координаты: {stash.transform.position}```";
				
				if (config.Hide.ConsoleHideStash) PrintWarning(sh);
				if (config.Hide.LogHideStash) LogToFile($"StashLogsHide", sh, this);
				
				if (config.Hide.DiscordHideStash)
				{
				    if (DiscordMessages) DiscordMessages?.Call("API_SendTextMessage", config.Discord.WebHook, sh);
				        else PrintWarning(error);
				}
			}				
			
			return null;
		}		
		
			}
}
