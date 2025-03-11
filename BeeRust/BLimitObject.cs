using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using Oxide.Core;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Linq;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("BLimitObject", "King", "1.0.0")]
    public class BLimitObject : RustPlugin
    {
        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class ObjectSettings
        {
			[JsonProperty("Минимальное количество обьектов от которых будет действовать множитель")]
            public int minbuildingBlocks;

			[JsonProperty("Максимальное количество обьектов до которых будет действовать множитель")]
            public int maxbuildingBlocks;

			[JsonProperty("Коэфицент удвоения стоимости")]
            public float Factor;
        }

        public class Settings
        {
            [JsonProperty("Использовать увелечения потребления ресурсов зависимых от обьектов")]
            public bool useObjectSettings;

            [JsonProperty("Параметры увелечения")]
            public List<ObjectSettings> _ObjectSettings = new List<ObjectSettings>();

            [JsonProperty("Использовать ограничения обьектов на шкаф")]
            public bool usebuildingBlocks;

            [JsonProperty("Максимальное количество объектов, которые можно установить в шкафу")]
            public int buildingBlocks;
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public Settings _Settings;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _Settings = new Settings()
                    {
                        useObjectSettings = false,
                        _ObjectSettings = new List<ObjectSettings>()
                        {
                            new ObjectSettings()
                            {
                                minbuildingBlocks = 1500,
                                maxbuildingBlocks = 2500,
                                Factor = 1.5f,
                            },
                            new ObjectSettings()
                            {
                                minbuildingBlocks = 2500,
                                maxbuildingBlocks = 3500,
                                Factor = 2f,
                            },
                        },
                        usebuildingBlocks = false,
                        buildingBlocks = 4500,
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [Rust-Api]
        private bool CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
			var dict = new Dictionary<int, int>();

			foreach (var itemAmount in block.blockDefinition.GetGrade(grade, block.skinID).CostToBuild())
			{
			    int amount;
				if (!dict.TryGetValue(itemAmount.itemid, out amount))
					amount = player.inventory.GetAmount(itemAmount.itemid);
                var Factor = itemAmount.amount + (int)GetMultiply(player, Convert.ToInt32(itemAmount.amount));
				if (amount < Factor)
					return false;

				dict[itemAmount.itemid] = amount - Mathf.RoundToInt(itemAmount.amount);
			}

            return true;
        }

        private object OnPayForUpgrade(BasePlayer player, BuildingBlock block, ConstructionGrade gradeTarget)
        {
            var collect = new List<Item>();

            foreach (var item in gradeTarget.CostToBuild())
            {
                var Amount = (int)GetMultiply(player, Convert.ToInt32(item.amount));
                player.inventory.Take(collect, item.itemid, Amount);
                player.Command("note.inv " + item.itemid + " " + (float) ((int)Amount * -1.0));
            }

            foreach (var obj in collect)
                obj.Remove();

            return null;
        }

        private object CanBuild(Planner builder, Construction prefab, Construction.Target target)
        {
            var prefabName = prefab.fullName ?? "";
            if (prefabName == "") return null;
            var player = builder.GetOwnerPlayer();
            if (player == null) return null;
            var cupboard = player.GetBuildingPrivilege();
            if (cupboard == null) return null;
            return CheckStatus(player, cupboard);
        }
        #endregion

        #region [Limit]
        private object CheckStatus(BasePlayer player, BuildingPrivlidge build)
        {
            int buildingBlocks = build.GetBuilding().buildingBlocks.Count;

            if (buildingBlocks >= config._Settings.buildingBlocks)
            {
                if (player.SecondsSinceAttacked > 5)
                {
                    player.ChatMessage($"Вы превысили лимит установки объектов в 1 шкафу! Максимальное количество объектов, которое можно установить в 1 шкафу - {config._Settings.buildingBlocks}");
                    player.lastAttackedTime = UnityEngine.Time.time;
                }
                return false;
            }
            else
            {
                if (player.SecondsSinceAttacked > 5)
                {
                    player.ChatMessage($"В данном шкафу можно еще поставить <color=#a5e664>{config._Settings.buildingBlocks - buildingBlocks}</color> объектов!");
                    player.lastAttackedTime = UnityEngine.Time.time;
                }
            }

            return null;
        }
        #endregion

        #region [Func]
        private int CheckObject(BasePlayer player)
        {
            var cupboard = player.GetBuildingPrivilege();
            if (cupboard == null) return 0;
            var entity = cupboard.GetBuilding().buildingBlocks.Where(i => i as BuildingBlock).ToList();
            return entity.Count;
        }

        private double GetMultiply(BasePlayer player, int costToBuild)
        {
            int buildingBlocks = CheckObject(player);

            var find = config._Settings._ObjectSettings.FirstOrDefault(obj => buildingBlocks >= obj.minbuildingBlocks && buildingBlocks <= obj.maxbuildingBlocks);
            if (find == null) return 0;

            return (costToBuild * find.Factor) - costToBuild;
        }
        #endregion
    }
}