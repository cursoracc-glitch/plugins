using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("AdminProtect", "Ryamkk", "1.0.0")]
    public class AdminProtect : RustPlugin
    {
        public Configuration config;

        public class Configuration
        {
            [JsonProperty("Список SteamID админов которым разрешено заходить на сервер")]
            public List<ulong> AdminProtectList = new List<ulong>(){ };
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintError("Конфигурационный файл повреждён, проверьте правильность ведённых данных!");
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new Configuration(), true);
        }

        void OnPlayerInit(BasePlayer player)
        {
            if(player.Connection.authLevel > 1)
            {
                if (!config.AdminProtectList.Contains(player.userID))
                {
                    SendVKLogs($"Игрок {player.displayName} ({player.userID}) попытался зайти с уровнем авторизации {player.Connection.authLevel} и был кикнут!");
                    player.Kick("Если вы реально являетесь администратором отпишите в вк @zaharkotov!");
                }
            }
        }

        [ConsoleCommand("admin.protect")]
        void CmdACIgnore(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            if (arg.Args == null) return;
            
            string steamidSTR = arg.Args[0];
            ulong steamid = 0;
            
            if (steamidSTR.Length == 17 && ulong.TryParse(steamidSTR, out steamid))
            {
                if (!config.AdminProtectList.Contains(steamid))
                {
                    config.AdminProtectList.Add(steamid);
                    Puts($"[AdminProtect]: Player [{steamidSTR}] added to AdminList");
                    SaveConfig();
                }
                else
                {
                    config.AdminProtectList.Remove(steamid);
                    Puts($"[AdminProtect]: Player [{steamidSTR}] removed from AdminList");
                    SaveConfig();
                }
                Config.WriteObject(config, true);
            }
            else
            {
                Puts($"[AdminProtect]: You write steamid [{steamidSTR}] is not correct!");
            }
        }
        
        private string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }
        
        #region VK
        private void SendVKLogs(string msg, params object[] args)
            {
            int RandomID = UnityEngine.Random.Range(0, 9999);
            int id = 11;
            string token = "vk1.a.gRWKIa_kC-IGL5pFkb_iIY7xYY8h6yiUzniSxDRBOw9191_p0KPUethDtwHf3eKPftMHgRXF9Upiv94udeDTzFIpkkLKR7jCFB8LEg19-PlNRTEeSRjc6QkCzdx8_LMIf5P_O7L3PxsuR_4xT62iW_-L9R3EDEgl_qmRv7h3wIyWfM1GK7PwO9d1k6tvR7TI";
            while (msg.Contains("#"))
                msg = msg.Replace("#", "%23");
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={id}&random_id={RandomID}&message={msg}&access_token={token}&v=5.92", null, (code, response) => { }, this);
        }

        #endregion
    }
}