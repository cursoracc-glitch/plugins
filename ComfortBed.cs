using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ComfortBed", "Mercury", "0.0.2")]
    [Description("Самый лучший пионер Mercury")]
    class ComfortBed : RustPlugin
    {
        [PluginReference] Plugin IQChat;

        #region Vars
        string ShortnameBed = "bed";
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("SkinID для кровати")]
            public ulong SkinID;
            [JsonProperty("DisplayName для кровати")]
            public string DisplayName;
            [JsonProperty("Ломать кровать при возрождении(Сделать ее одноразовой)")]
            public bool KillBed;
            [JsonProperty("Настройки пользователя при возрождении")]
            public MetabolismUser metabolismUser = new MetabolismUser();

            internal class MetabolismUser
            {
                [JsonProperty("Кол-во ХП при возраждении на кровати")]
                public int Health;
                [JsonProperty("Кол-во ЖАЖДЫ при возраждении на кровати")]
                public int Water;
                [JsonProperty("Кол-во СЫТНОСТИ при возраждении на кровати")]
                public int Hungry;
            }
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    SkinID = 1957274032,
                    DisplayName = "Комфортная шконка",
                    KillBed = false,
                    metabolismUser = new MetabolismUser
                    {
                        Health = 100,
                        Hungry = 500,
                        Water = 500
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #1325" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Metods
        void CreateItem(BasePlayer player)
        {
            Item item = ItemManager.CreateByName(ShortnameBed, 1, config.SkinID);
            item.name = config.DisplayName;
            player.GiveItem(item);
        }

        #endregion

        #region Hooks
        private object OnPlayerRespawn(BasePlayer player, SleepingBag bag)
        {
            if(bag.skinID == config.SkinID)
                if(bag.OwnerID == player.userID)
                {
                    var MetabolismConfig = config.metabolismUser;
                    string MessageChat = config.KillBed ? "BED_KILL" : "BED_USED";
                    NextTick(() =>
                    {
                        player.health = MetabolismConfig.Health;
                        player.metabolism.calories.value = MetabolismConfig.Hungry;
                        player.metabolism.hydration.value = MetabolismConfig.Water;
                    });

                    if(config.KillBed)
                        bag.Kill();
                    SendChat(lang.GetMessage(MessageChat, this, player.UserIDString), player);
                }
            return null;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            SleepingBag bed = entity.GetComponent<SleepingBag>();
            if (bed == null) return;
            if (bed.skinID == config.SkinID)
                bed.niceName = config.DisplayName;
        }
        #endregion

        #region Command

        [ConsoleCommand("cb")]
        void ComfortBedCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(arg.Args[0]));
            if (player == null) return;
            CreateItem(player);
            SendChat($"Вы успешно получили {config.DisplayName}", player);
            Puts("Игроку успешно выдана кровать!");
        }

        #endregion

        #region Helps
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning("Языковой файл загружается...");
            Dictionary<string, string> Lang = new Dictionary<string, string>
            {
                ["BED_USED"] = "Вы успешно появились на комфортной шконке",
                ["BED_KILL"] = "Ваша комфортная шконка разрушена",
            };

            lang.RegisterMessages(Lang, this);
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion
    }
}
