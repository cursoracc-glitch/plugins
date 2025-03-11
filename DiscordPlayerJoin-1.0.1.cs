using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("DiscordPlayerJoin", "AKACHATGPTPASTEROK", "1.0.1")]
    [Description("Sends a message to Discord when a player joins the server.")]
    public class DiscordPlayerJoin : RustPlugin
    {
        [PluginReference]
        Plugin Discord;

        private string webhookUrl = "https://discord.com/api/webhooks/1334675549276340285/C8u0nv9n9nLZmKjT6QX-EuR-Ku7_oO-ybDL24HFyDaeSgKm8GpA5NvYo53F5_6HnWDCI"; // Замените на ваш вебхук
        private const string TimeFormat = "dd.MM.yyyy HH:mm:ss";

        void Init()
        {
            Puts("DiscordPlayerJoin has been initialized.");
        }

        void OnPlayerConnected(BasePlayer player)
        {
            string steamId = player.UserIDString;
            string playerName = player.displayName;
            string ipAddress = player.Connection.ipaddress;
            string steamProfile = $"https://steamcommunity.com/profiles/{player.UserIDString.Substring(0,17)}";
            string currentTime = DateTime.Now.ToString(TimeFormat);

            string message = $"🎮 **Новый игрок присоединился к серверу!**\n" +
                           $"👤 Имя: `{playerName}`\n" +
                           $"🕒 Время захода: `{currentTime}`\n" +
                           $"🆔 Steam ID: `{steamId}`\n" +
                           $"🌐 IP Адрес: `{ipAddress}`\n" +
                           $"🔗 Steam Профиль: {steamProfile}";

            SendDiscordMessage(message);
        }

        void SendDiscordMessage(string message)
        {
            var payload = new
            {
                content = message,
                username = "Rust Server Monitor",
                avatar_url = "https://i.imgur.com/RxNEzZC.png"
            };

            var json = JsonConvert.SerializeObject(payload);
            
            webrequest.Enqueue(webhookUrl, json, (code, response) =>
            {
                if (code != 204 && code != 200)
                {
                    Puts($"Error sending message to Discord. Code: {code}, Response: {response}");
                }
            }, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
        }
    }
}