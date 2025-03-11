using System.Collections.Generic;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("SmallDecay", "Sempai#3239", "0.0.1")]
    [Description("Гниение Mercury")]
    class SmallDecay : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat;
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Через сколько времени снимать ХП смоллтешу(секунды)")]
            public int DecayTime;
            [JsonProperty("Сколько ХП снимать соллтешу(У тайника - 150 ХП)")]
            public int DecayDamage;
            [JsonProperty("Сообщение игроку")]
            public string MessagePlayer;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                   DecayTime = 10,
                   DecayDamage = 1,
                   MessagePlayer = "Ваш тайник будет гнить каждые 3462 секунд",
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
                PrintWarning("Ошибка #175" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #138");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        public List<StashContainer> SmollData = new List<StashContainer>();

        #region Hooks
        void OnEntitySpawned(BaseNetworkable entity)
        {
            var Stash = entity?.GetComponent<StashContainer>();
            if (Stash == null) return;
            DecayWrite(Stash);
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            var Stash = entity?.GetComponent<StashContainer>();
            if (Stash == null) return;
            SmollData.Remove(Stash);
        }
        private void OnServerInitialized()
        {
            SmollData = UnityEngine.Object.FindObjectsOfType<StashContainer>().ToList();
            PrintWarning($"Найдено {SmollData.Count} тайников. Начинается гниение");
            DecayStart();
        }
        #endregion

        void DecayStart()
        {
            timer.Every(config.DecayTime, () =>
            {
                if (SmollData.Count == 0) return;
                for(int i = 0; i < SmollData.Count; i++)
                {
                    var Stash = SmollData[i];
                    Stash.health -= config.DecayDamage;
                    if (Stash.health <= 0)
                    {
                        SmollData.Remove(Stash);
                        Stash.Kill();
                    }
                }
            });
        }

        void DecayWrite(StashContainer stash)
        {
            if (stash == null) return;
            var player = BasePlayer.FindByID(stash.OwnerID);
            if(player == null)
            {
                PrintError("R");
                return;
            }
            SmollData.Add(stash);
            SendChat(player, config.MessagePlayer);
        }
    }
}
