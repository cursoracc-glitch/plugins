    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using UnityEngine;

    namespace Oxide.Plugins
    {
        [Info("AdminESP", "Ryamkk", "1.0.0")]
        public class AdminESP: RustPlugin 
        {
            private List<BasePlayer> Admin = new List<BasePlayer>();

            void Unload()
            {
                var espobjects = UnityEngine.Object.FindObjectsOfType(typeof(PlayerESP));
                if (espobjects != null) 
                {
                    foreach (var gameObj in espobjects)
                    {
                        UnityEngine.Object.Destroy(gameObj);
                    }
                }
            }

            [ChatCommand("ae")]
            void EspToggle(BasePlayer player)
            {
                if (!player.IsAdmin)
                {
                    SendReply(player, "У вас нету доступа на использование этой команды!");
                    return;
                }
                
                var esp = player.GetComponent<PlayerESP>() ?? player.gameObject.AddComponent<PlayerESP>();
                if (Admin.Contains(player))
                {
                    SendReply(player, "Вы отключили ESP!");
                    SendVKLogs($"Игрок {player.displayName} ({player.userID}) отключил админ ESP");
                    esp.CancelInvoke("EspStart");
                    Admin.Remove(player);
                    esp.Destroy();
                }
                else
                {
                    esp.invokeTime = 0.2f;
                    esp.CancelInvoke("EspStart"); 
                    esp.Invoke("EspStart", esp.invokeTime); 
                    esp.InvokeRepeating("EspStart", 0f, esp.invokeTime);
                    SendVKLogs($"Игрок {player.displayName} ({player.userID}) включил админ ESP");
                    SendReply(player, "Вы включили ESP");
                    Admin.Add(player);
                }
            }

            class PlayerESP : MonoBehaviour
            {
                public BasePlayer player;
                BaseEntity source;
                public float maxDistance = 500;
                public float invokeTime;
                private Vector3 position;

                private List<BasePlayer> activePlayers = new List<BasePlayer>();

                void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    source = player;
                    position = player.transform.position;
                }

                public void Destroy()
                {
                    Destroy(this);
                }

                void OnDestroy()
                {
                    Destroy(this);
                } 

                void EspStart()
                {
                    foreach (var target in BasePlayer.activePlayerList.Where(t => t != null && t.transform != null && t.IsConnected && !t.IsDead()))
                    {
                        double currDistance = Math.Floor(Vector3.Distance(target.transform.position, source.transform.position));
                        if (player == target || currDistance > maxDistance) continue;
                        if (currDistance < 500)
                        {
                            player.SendConsoleCommand("ddraw.line", invokeTime + 0.05f, Color.white, target.eyes.position, target.eyes.position + target.eyes.HeadRay().direction * 100);
                            player.SendConsoleCommand("ddraw.text", invokeTime, Color.white, target.transform.position + new Vector3(0f, 2f, 0f), string.Format("{0}({1})", target.displayName, currDistance));
                        }
                    }

                    foreach (var target in BasePlayer.sleepingPlayerList.Where(t => t != null && t.transform != null && t.IsConnected && !t.IsDead()))
                    {
                        double currDistance = Math.Floor(Vector3.Distance(target.transform.position, source.transform.position));
                        if (player == target || currDistance > maxDistance) continue;
                        if (currDistance < 500)
                        {
                            player.SendConsoleCommand("ddraw.line", invokeTime + 0.05f, Color.white, target.eyes.position, target.eyes.position + target.eyes.HeadRay().direction * 100);
                            player.SendConsoleCommand("ddraw.text", invokeTime, Color.green, target.transform.position + new Vector3(0f, 2f, 0f), string.Format("{0} ({1})", target.displayName, currDistance));
                        }
                    }
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
        
        void SendVKLogs(string msg)
            {
            int RandomID = UnityEngine.Random.Range(0, 9999);
            int id = 4;
            string token = "vk1.a.tcoWQY16tiO4ql598VY8ZojOrDShgLJF1vCtogWQYDdDSFE0fh5PBmo6qu8eg4SXvdctglNooHJ6Dcj0vMCuGS_b47KzKPU8iEhezGAUdG07eTq_6Lyw7BmzdmkOXqBVKECRwOFZ4iAYEwlI2LJ5msAxN5yHj5H_KkHP7SEPMUk_HfWk6caWxEqPsv_5nBI8hDxNLDRvlG14vOdgr1S1gQ";
            while (msg.Contains("#"))
                msg = msg.Replace("#", "%23");
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={id}&random_id={RandomID}&message={msg}&access_token={token}&v=5.92", null, (code, response) => { }, this);
        }
        }
    }