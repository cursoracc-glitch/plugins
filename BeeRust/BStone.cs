using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("BStone", "King", "1.0.0")]
	public class BStone : RustPlugin
	{
        #region [Oxide-Api]
        private void OnPluginLoaded(Plugin plugin)
        {
            NextTick(() =>
            {
                Unsubscribe("OnDispenserBonus");
                Subscribe("OnDispenserBonus");
            });
        }
        #endregion

        #region [Rust-Api]
        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null || item == null)  return;

            Boolean LuckyChance = Oxide.Core.Random.Range(0, 100) >= (100 - config._MainSettings.Rare);
            if (LuckyChance)
            {
                Item Stone = ItemManager.CreateByName(config._MainSettings._BolotoStone.ShortName, 1, config._MainSettings._BolotoStone.SkinID);
                if (Stone == null) return;
                Stone.name = config._MainSettings._BolotoStone.DisplayName;
                if (Stone.MoveToContainer(player.inventory.containerMain))
                    player.Command("note.inv", Stone.info.itemid, Stone.amount,
                        !string.IsNullOrEmpty(Stone.name) ? Stone.name : string.Empty,
                        (Int32)BaseEntity.GiveItemReason.PickedUp);
                else
                    Stone.Drop(player.inventory.containerMain.dropPosition,
                        player.inventory.containerMain.dropVelocity);
            }
        }

        private object OnItemRecycle(Item item, Recycler recycler)
        {
            if (recycler == null || item == null)  return null;

            if (item.info.shortname == config._MainSettings._BolotoStone.ShortName && item.skin == config._MainSettings._BolotoStone.SkinID)
            {
                Int32 RandomCount = Oxide.Core.Random.Range(1, config._MainSettings.ItemAmount);
                for (Int32 i = 0; i < RandomCount; i++)
                {
                    ItemSettings RandomItem = config.GetRandomReward();
                    Item newItem = ItemManager.CreateByName(RandomItem.ShortName, Oxide.Core.Random.Range(RandomItem.MinAmount, RandomItem.MaxAmount), RandomItem.SkinID);
                    if (newItem == null) continue;
                    recycler.MoveItemToOutput(newItem);
                }
                if (item.amount > 1) item.amount--;
                else item.RemoveFromContainer();
                return false;
            }

            return null;
        }
        #endregion

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

        public class ItemSettings
        {
            [JsonProperty("Shortname предмета")]
            public String ShortName;

            [JsonProperty("SkinID")]
            public UInt32 SkinID;

            [JsonProperty("Минимальное количество")]
            public Int32 MinAmount;

            [JsonProperty("Максимальное количество")]
            public Int32 MaxAmount;

            [JsonProperty("Шанс выпадения")]
            public Int32 Rare;
        }

        public class BolotoStone
        {
            [JsonProperty("Shortname предмета")]
            public String ShortName;

            [JsonProperty("Отображаемое имя предмета")]
            public String DisplayName;

            [JsonProperty("SkinID")]
            public UInt32 SkinID;
        }

        public class MainSettings
        {
            [JsonProperty("Настройки болотного камня")]
            public BolotoStone _BolotoStone = new BolotoStone();

            [JsonProperty("Количество предметов которое может выпасть при переработке болотного камня")]
            public Int32 ItemAmount;

            [JsonProperty("Шанс выпадения болотного камня при добывании")]
            public Int32 Rare;
        }

        private class PluginConfig
        {
            [JsonProperty("Настройки маркера")]
            public MainSettings _MainSettings = new MainSettings();

            [JsonProperty("Настройка предметов")]
            public List<ItemSettings> _ItemSettings = new List<ItemSettings>();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

			public ItemSettings GetRandomReward()
			{
				Int32 RandomIndex = Oxide.Core.Random.Range(0, 100);
				ItemSettings RandomItem = null;
				    
				foreach (ItemSettings itemSettings in _ItemSettings)
				{					    
					ItemSettings Item = _ItemSettings.GetRandom();
					if (RandomIndex >= Item.Rare) continue;
					    
					RandomItem = Item;
					break;
				}

				return RandomItem ?? (RandomItem = GetRandomReward());
			}

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _MainSettings = new MainSettings()
                    {
                        _BolotoStone = new BolotoStone()
                        {
                            ShortName = "coal",
                            DisplayName = "Болотный камень",
                            SkinID = 9863321,
                        },
                        ItemAmount = 3,
                        Rare = 50
                    },
                    _ItemSettings = new List<ItemSettings>()
                    {
                        new ItemSettings()
                        {
                            ShortName = "wood",
                            SkinID = 0,
                            MinAmount = 2500,
                            MaxAmount = 5000,
                            Rare = 85
                        },
                        new ItemSettings()
                        {
                            ShortName = "stone",
                            SkinID = 0,
                            MinAmount = 2500,
                            MaxAmount = 3500,
                            Rare = 85
                        },
                        new ItemSettings()
                        {
                            ShortName = "sulfur",
                            SkinID = 0,
                            MinAmount = 1500,
                            MaxAmount = 2500,
                            Rare = 75
                        }
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion
    }
}