using System.Collections.Generic;
using Newtonsoft.Json; 

namespace Oxide.Plugins 
{ 
    [Info("XChatMessage", "Sempai#3239", "1.0.0")] 
    class XChatMessage : RustPlugin 
    { 
	    public int _message;
		
		#region Configuration  
   
        private ChatMessageConfig config; 
 
        private class ChatMessageConfig
        {		  
		    internal class GeneralSetting
			{
				[JsonProperty("Интервал сообщений в чат")] public float TimeMessage;
				[JsonProperty("SteamID профиля для кастомной аватарки")] public ulong SteamID;
			}	 						
			
			[JsonProperty("Общие настройки")]
            public GeneralSetting Setting;			
			[JsonProperty("Ключи сообщений для ленгов")]
            public List<string> Messages;	          	
			
			public static ChatMessageConfig GetNewConfiguration()
            {
                return new ChatMessageConfig  
                {
					Setting = new GeneralSetting      
					{
						TimeMessage = 300.0f,
						SteamID = 0
					},
					Messages = new List<string>
					{
						"MESSAGE_1",
						"MESSAGE_2",
						"MESSAGE_3"
					}   
				};
			}
        }
  
        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<ChatMessageConfig>(); 
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = ChatMessageConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			
			InitializeLang();
			Broadcast();
		}
		
		private void Broadcast()       
		{
			int count = config.Messages.Count;
			
			if(count == 0)
			{
				PrintWarning("У вас нет сообщений!!!");
				return;
			}
			
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			    Player.Reply(player, lang.GetMessage(config.Messages[_message], this, player.UserIDString), config.Setting.SteamID); 
			
			_message++;
			if(_message >= count--)
				_message = 0;
			
			timer.Once(config.Setting.TimeMessage, Broadcast); 
		}
		
		#endregion
		
		#region Lang
 
        private void InitializeLang()
        {
			Dictionary<string, string> llang = new Dictionary<string, string>();		
				
			foreach(var message in config.Messages)
			    llang.Add(message, "MESSAGE MESSAGE MESSAGE");			
			 
            lang.RegisterMessages(llang, this);
            lang.RegisterMessages(llang, this, "ru");
        }

        #endregion
	}
}