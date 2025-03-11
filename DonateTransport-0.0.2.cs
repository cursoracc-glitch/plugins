using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DonateTransport", "MaltrzD", "0.0.2")]
    class DonateTransport : RustPlugin
    {
        private ConfigData _config;

        #region [ OXIDE HOOKS ]
        private void Loaded() => ReadConfig();
        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            if(go.TryGetComponent(out BaseEntity entity))
            {
                Transport transport = GetTransportBySkinId(entity.skinID);
                if(transport != null)
                {
                    if(transport.CanPlaceOnlyCosntruction)
                    {
                        RaycastHit Hit;
                        if (Physics.Raycast(entity.transform.position, Vector3.down, out Hit, LayerMask.GetMask(new string[] { "Construction" })))
                        {
                            var rhEntity = Hit.GetEntity();
                            if (rhEntity != null)
                            {
                                BaseEntity entityToSpawn = GameManager.server.CreateEntity(transport.TransportPrefab, entity.transform.position, entity.transform.rotation);
                                entityToSpawn.Spawn();

                                entity.Kill();
                            }
                        }
                    }
                    else
                    {
                        Vector3 targetPos = entity.transform.position;

                        if (transport.CordPlus != 0f)
                        {
                            Vector3 entityPos = entity.transform.position;

                            Vector3 forwardDirection = -entity.transform.forward;

                            Vector3 offset = forwardDirection * transport.CordPlus;

                            targetPos = entityPos + offset;
                        }


                        BaseEntity entityToSpawn = GameManager.server.CreateEntity(
                            transport.TransportPrefab,
                            targetPos,
                            entity.transform.rotation);


                        entityToSpawn.Spawn();

                        entity.Kill();
                    }
                }
            }
        }
        #endregion

        #region [ MAIN ]
        [ConsoleCommand("dt.give")]
        private void GiveTransport_ConsoleCommand(ConsoleSystem.Arg arg)
        {
            if(arg.IsAdmin == false)
            {
                return;
            }

            ulong userID = 0;
            string transportName = string.Empty;

            try
            {
                userID = System.Convert.ToUInt64(arg.Args[0]);
                transportName = arg.Args[1];
            }
            catch
            {
                Debug.LogWarning
                    (
                    "Один из аргументов указан неверно!\n" +
                    "Синтаксис:\n" +
                    "dt.give <steamid> <transport name>"
                    );

                return;
            }

            BasePlayer player = BasePlayer.FindByID(userID);
            if(player == null)
            {
                Debug.LogWarning("Игрок с указанным SteamID не найден!");
                return;
            }

            Transport requiredTransport = GetTransportByName(transportName);
            if(requiredTransport == null)
            {
                Debug.LogWarning("Транспорт с указанным именем успешно создан!");
                return;
            }

            GiveTransport(player, requiredTransport);
        }
        private void GiveTransport(BasePlayer player, Transport transport)
        {
            Item itemToGive = ItemManager.CreateByName(transport.BuildItemShortName, 1, transport.SkinId);
            itemToGive.name = transport.DisplayName;

            player.GiveItem(itemToGive);
        }
        #endregion

        #region [ EXT ]
        private Transport GetTransportByName(string name) =>
            _config.Transports
            .Where(x => x.TransportName == name)
            .FirstOrDefault();
        private Transport GetTransportBySkinId(ulong skinId) =>
            _config.Transports
            .Where(x => x.SkinId == skinId)
            .FirstOrDefault();

        #endregion

        #region [ CONFIG ]
        class ConfigData
        {
            [JsonProperty("Настройка транспорта")]
            public List<Transport> Transports = new List<Transport>()
            {
                new Transport()
                {
                    BuildItemShortName = "box.wooden",
                    TransportName = "minicopter",
                    DisplayName = "Миникоптер",
                    TransportPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
                    CanPlaceOnlyCosntruction = false,
                    SkinId = 1231230
                },
                new Transport()
                {
                    BuildItemShortName = "box.wooden",
                    TransportName = "attackhelicopter",
                    DisplayName = "Аттак хеликоптер",
                    TransportPrefab = "assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab",
                    CanPlaceOnlyCosntruction = false,
                    SkinId = 1231231
                },
                new Transport()
                {
                    BuildItemShortName = "box.wooden",
                    TransportName = "scraphelicopter",
                    DisplayName = "Грузовой вертолет",
                    TransportPrefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
                    CanPlaceOnlyCosntruction = false,
                    SkinId = 1231232,
                    CordPlus = 5
                },
                new Transport()
                {
                    BuildItemShortName = "box.wooden",
                    TransportName = "sedantest",
                    DisplayName = "Седан",
                    TransportPrefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab",
                    CanPlaceOnlyCosntruction = false,
                    SkinId = 1231233
                },
                new Transport()
                {
                    BuildItemShortName = "research.table",
                    TransportName = "recycler",
                    DisplayName = "Переработчик",
                    TransportPrefab = "assets/bundled/prefabs/static/recycler_static.prefab",
                    CanPlaceOnlyCosntruction = true,
                    SkinId = 1231234
                },
            };
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData();
            SaveConfig(config);
        }
        void SaveConfig(object config)
        {
            Config.WriteObject(config, true);
        }
        void ReadConfig()
        {
            base.Config.Settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            _config = Config.ReadObject<ConfigData>();
            SaveConfig(_config);
        }
        #endregion
        public class Transport
        {
            [JsonProperty("Шортнейм предмета который ставить")]
            public string BuildItemShortName;

            [JsonProperty("корд плюс")]
            public float CordPlus;

            [JsonProperty("Название транспорта (для выдачи)")]
            public string TransportName;

            [JsonProperty("Отображаемое имя айтема")]
            public string DisplayName;

            [JsonProperty("Путь до префаба транспорта")]
            public string TransportPrefab;

            [JsonProperty("Ставится только на конструкцию?")]
            public bool CanPlaceOnlyCosntruction;

            [JsonProperty("Скин ид")]
            public ulong SkinId;
        }
    }
}