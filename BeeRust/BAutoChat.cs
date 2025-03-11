using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("BAutoChat", "Kotik", "0.0.1")]
    internal class BAutoChat : CovalencePlugin
    {
        void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------" +
            "     \nДанный плагин был скачан с хуя мерзости" +
            "     \nС чата где ебут котика" +
            "\n-----------------------------");
        }

        private Configuration _config;
        private int _previousAdvert = -1;

        #region Hooks

        private void Loaded()
        {
            LoadConfig();
            
            Puts($"{Title} время между сообщениями {_config.AdvertInterval} минут.");
            timer.Every(_config.AdvertInterval * 60, BroadcastNextAdvert);
        }

        #endregion

        #region Helper Methods

        private void BroadcastNextAdvert()
        {
            if (_config.Messages.Count == 0)
                return;

            int advert = GetNextAdvertIndex();

            server.Broadcast(_config.Messages[advert]);

            if (_config.BroadcastToConsole)
                Puts(Formatter.ToPlaintext(_config.Messages[advert]));

            _previousAdvert = advert;
        }

        private int GetNextAdvertIndex()
        {

            int advert;
            if (_config.Messages.Count > 1)
            {
                do advert = Random.Range(0, _config.Messages.Count);
                while (advert == _previousAdvert);
            }
            else
                advert = 0;

            return advert;
        }

        #endregion

        #region Configuration

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = Configuration.CreateDefault();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("Сообщения")]
            public List<string> Messages { get; private set; }

            [JsonProperty("К/д между сообщениями")]
            public float AdvertInterval { get; private set; }  = 10;

            [JsonProperty("Выводить ли сообщения в консоль на консоль")]
            public bool BroadcastToConsole { get; private set; } = true;

            [JsonProperty("Аватарки сообщений")]
            public Dictionary<ulong, string> AvatarIDs { get; private set; }


            public static Configuration CreateDefault()
            {
                return new Configuration
                {
                    Messages = new List<string>
                    {
                        "Если нужен цвет болото раста то вот <color=#9acd32>он</color>",
                        "",
                        "",
                        "",
                        "",
                        "", 
                        "",
                        ""
                    },
                 AvatarIDs = new Dictionary<ulong, string>()
                };
            }
        }

        #endregion
    }
}