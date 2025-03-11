using System.Collections.Generic;
using Network;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Custom Icon", "collect_vood", "1.0.4")]
    [Description("Set a customizable icon for all non user messages")]

    class CustomIcon : CovalencePlugin
    {
        #region Config
        
        private Configuration _configuration;
        
        private class Configuration
        {
            [JsonProperty(PropertyName = "Steam Avatar User ID")]
            public ulong SteamAvatarUserID = 0;
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            _configuration = new Configuration();
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _configuration = Config.ReadObject<Configuration>();
            SaveConfig();
        }
        
        protected override void SaveConfig() => Config.WriteObject(_configuration);
        
        #endregion

        #region Hooks 
        
        private void OnBroadcastCommand(string command, object[] args)
        {
            TryApplySteamAvatarUserID(command, args);
        }
        
        private void OnSendCommand(Connection cn, string command, object[] args)
        {
            TryApplySteamAvatarUserID(command, args);
        }
        
        private void OnSendCommand(List<Connection> cn, string command, object[] args)
        {
            TryApplySteamAvatarUserID(command, args);
        }

        #endregion

        #region Helpers

        private void TryApplySteamAvatarUserID(string command, object[] args)
        {
            if (args == null || _configuration == null) 
                return;
            
            if (args.Length < 2 || (command != "chat.add" && command != "chat.add2")) 
                return;

            ulong providedID;
            if (ulong.TryParse(args[1].ToString(), out providedID) && providedID == 0)
                args[1] = _configuration.SteamAvatarUserID;
        }

        #endregion
    }
}