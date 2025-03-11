using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using ConVar;
using Facepunch.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = UnityEngine.Random;
namespace Oxide.Plugins
{
    [Info("TextPromo", "TopPlugin.ru", "1.0.0")]
    public class TextPromo : RustPlugin
    {
        public class ConfigData
        {
            [JsonProperty("Адресс магазина")] public string nameserver;
            [JsonProperty("Первый промо")] public string promo1;
            [JsonProperty("Второй промо")] public string promo2;
            [JsonProperty("Сколько игроков получит промо")] public int count;
            public static ConfigData GetNewConf()
            {
                ConfigData newConfig = new ConfigData();
                newConfig.nameserver = "RUST PINK";
                newConfig.promo1 = "wipe (25RUB)";
                newConfig.promo2 = "MAGAZIN - rustpink.ru ";
                newConfig.count = 50;
                return newConfig;
            }
        }
        protected override void LoadDefaultConfig() => cfg = ConfigData.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(cfg);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        private ConfigData cfg { get; set;}
        public List<TextPromos> TextPromo2 = new List<TextPromos>();

        [ChatCommand("givepromonote")]
        void cmdgive(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                GivePromo(player);
            }
        }
        public class TextPromos
        {
            [JsonProperty("Ник")] public string Name { get; set; }
            [JsonProperty("СтимАйди")] public ulong SteamID { get; set; }
            [JsonProperty("Выдал")] public bool Vidal { get; set; }
        }
        void OnPlayerInit(BasePlayer player)
        {
            TextPromos data = TextPromo2.Find(x => x.SteamID == player.userID);
            if((player is NPCPlayer)) return;
            if (TextPromo2.Count >= cfg.count)
            {
                return;
            }
            if (player.IsAdmin)
            {
                return;
            }
            if (data == null)
            {
                data = new TextPromos()
                {
                    Name = player.displayName,
                    SteamID = player.userID,
                    Vidal = false,
                };
                TextPromo2.Add(data);
                return;
            }

            if (data.Vidal)
            {
                return;
            }
            if (data != null)
            {
                GivePromo(player);
                SendReply(player, "Вы зашли на вайп сервера, вы получили промо-код в записке.");
                data.Vidal = true;
                SaveData();
            }
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            OnPlayerInit(player);
        }
        void GivePromo(BasePlayer player)
        {
            Item it = ItemManager.CreateByName("note", 1);
            it.name = "PROMO";
            it.skin = 1923090978;
            it.text = $"{cfg.nameserver}\n\nPROMO:\n{cfg.promo1}\n{cfg.promo2}\n\n{cfg.nameserver}";
            if (!player.inventory.GiveItem(it))
            {
                it.Drop(player.inventory.containerMain.dropPosition,
                    player.inventory.containerMain.dropVelocity,
                    new Quaternion());
            }
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TextPromoData", TextPromo2);
        }
        void OnServerInitialized()
        {
            TextPromo2 = Interface.Oxide.DataFileSystem.ReadObject<List<TextPromos>>("TextPromoData");
        }
    }
}