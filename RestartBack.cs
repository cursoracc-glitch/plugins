using System;
using System.Collections.Generic;
using System.Linq;
using Apex;
using Newtonsoft.Json;
using Oxide.Core;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("RestartBack", "Hougan", "0.0.1")]
    public class RestartBack : RustPlugin
    {
        #region Classes

        private class Configuration
        {
            [JsonProperty("Оповещение о включение сервера (%STEAMID%, %NAME%, %LENGTH%)")]
            public string BackMessage = $"Привет, %NAME%.\n\nТы был на сервере перед рестартом, в общем, сервер снова доступен!\n" +
                    $"Он был выключен %TIME% сек, быстро, правда?\n" +
                    $"\n" +
                    $"IP: 127.0.0.1:12000";

            [JsonProperty("Отправлять сообщение после вайпа")]
            public bool AfterWipe = false;
            [JsonProperty("Также уведомлять вышедших %КОЛ-ВО% секунд назад")]
            public int DisconnectBefore = 600; 
        }

        private class RestartInfo
        {
            public double DestroyTime = 0;
            public Dictionary<ulong, string> UsersInfo = new Dictionary<ulong, string>();
        }
       
        #endregion

        #region Variables

        private Configuration Settings;
        private RestartInfo CurrentInfo;
        
        #endregion
        
        #region Initialization 
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => Settings = new Configuration();
        protected override void SaveConfig()        => Config.WriteObject(Settings); 

        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name))
                CurrentInfo = Interface.Oxide.DataFileSystem.ReadObject<RestartInfo>(Name);

            ProcessRestart();
        }

        private void Unload()
        {
            if (ServerMgr.Instance.Restarting)
            {
                CurrentInfo = new RestartInfo();
                
                CurrentInfo.DestroyTime = CurrentTime();
                CurrentInfo.UsersInfo.AddRange(BasePlayer.activePlayerList.ToDictionary(p => p.userID, p => p.displayName));
                CurrentInfo.UsersInfo.AddRange(BasePlayer.sleepingPlayerList.ToDictionary(p => p.userID, p => p.displayName));
                
                Interface.Oxide.DataFileSystem.WriteObject(Name, CurrentInfo);
            }
        }

        #endregion

        #region Functions

        private void ProcessRestart()
        {
            if (CurrentInfo == null) return;
            if (!Settings.AfterWipe && DateTime.Now.Subtract(SaveRestore.SaveCreatedTime).TotalMilliseconds < 30)
            {
                Interface.Oxide.DataFileSystem.WriteObject(Name, CurrentInfo);
                return;
            }

            var disconnectedPlayers = CurrentInfo.UsersInfo; 

            var VKBot = plugins.Find("VKBot");
            if (VKBot != null) return;
            
            string elapsedTime = Math.Ceiling(CurrentTime() -CurrentInfo.DestroyTime).ToString("F0");
            foreach (var check in disconnectedPlayers)
            {
                var userId = (string) VKBot.Call("GetUserVKId", check.Key); 
                if (userId.IsNullOrEmpty()) continue;
                
                VKBot?.Call("SendVkMessage", userId, Settings.BackMessage.Replace("%NANE%", check.Value).Replace("%STEAMID%", check.Key.ToString()).Replace("%LENGTH%", elapsedTime)); 
            }
        }

        #endregion

        #region Utils

        private double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        #endregion
    }
}