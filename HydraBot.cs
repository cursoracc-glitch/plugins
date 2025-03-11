using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("HydraBot", "Deversive", "0.0.1", ResourceId = 1510)]
    [Description("Авто-оправка оповещений в чат для сервера - Hydra Rust")]
    internal class HydraBot : CovalencePlugin
    {
        private Configuration _config;
        private int _previousAdvert = -1;
        private ulong ImageID = 76561199039326412;

        #region Хук

        private void Loaded()
        {
            LoadConfig();
            
            Puts($"{Title} show ads every is {_config.AdvertInterval} minutes.");
            timer.Every(_config.AdvertInterval * 60, BroadcastNextAdvert);
        }

        #endregion

        #region Конфиг

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
            [JsonProperty("Сообщение")]
            public List<string> Messages { get; private set; }

            [JsonProperty("Интервал сообщений( в минутах)")]
            public float AdvertInterval { get; private set; }  = 10;

            [JsonProperty("Оповещать в консоль? (true/false)")]
            public bool BroadcastToConsole { get; private set; } = true;

            [JsonProperty("Выбрать сообщение в случайном порядке (true/false)")]
            public bool ChooseMessageAtRandom { get; private set; } = false;

            public static Configuration CreateDefault()
            {
                return new Configuration
                {
                    Messages = new List<string>
                    {
                        "ДРУЗЬЯ НАПОМИНАЕМ ВАМ ЧТО СЕЙЧАС СЕРВЕР В ТЕСТОВОМ РЕЖИИМЕ",
                        "Хочешь узнать о сервере побольше?\nПерейди в группу <color=#9198bf>ВК</color> VK.COM/STORM.RUST.",
                        "Не забывай у нас есть бесплатная рулетка в магазине \nМагазин <color=#8e6874>STORMRUST.RU</color>",
                        "Не забудь подключить оповщение о рейде \n написав команду - <color=#8e6874>/raid</color>",
                        "Увидел читера или нарушителей?\nОтправляй жалобу, написав <color=#8e6874>/REPORT</color>",
                        "ДРУЗЬЯ НАПОМИНАЕМ ВАМ ЧТО СЕЙЧАС СЕРВЕР В ТЕСТОВОМ РЕЖИИМЕ",
                        "Хочешь увеличить рейты на своём персонаже? \n купи привилегию в магазине - <color=#8e6874>STORMRUST.RU</color>",
                        "Каждые 24 часа у нас активируется \n <color=#8e6874>Двойной фарм</color>",
                        "Хочешь иметь приемущество над игроками? \n купи привилегию в магазине - <color=#8e6874>STORMRUST.RU</color>",
                        "Не забудь подключить оповщение о рейде \n написав команду - <color=#8e6874>/raid</color>",
                        "У нас имеется свой дискорд сервер, где вы можете общаться \n <color=#8e6874>discord.gg/vzCD3ZBC3C</color>",
                        "ДРУЗЬЯ НАПОМИНАЕМ ВАМ ЧТО СЕЙЧАС СЕРВЕР В ТЕСТОВОМ РЕЖИИМЕ"
                    }
                };
            }
        }

        #endregion

        #region Методы помощи

        void OnServerInitialized()
        {
        }

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
            if (!_config.ChooseMessageAtRandom)
                return (_previousAdvert + 1) % _config.Messages.Count;

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
    }
}