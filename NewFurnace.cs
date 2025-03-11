using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NewFurnace", "CASHR", "1.0.0")]
    public class NewFurnace : RustPlugin
    {
        [JsonProperty("Список ящиков в которых его спавнить")]
        public List<string> ListContainers = new List<string>()
                {
                    { "crate_basic" },
                    { "crate_normal" },
                    { "crate_normal_2" }
                };
        #region Конфиг
        private class Configuration
        {
            [JsonProperty("Шанс нахождения печки в ящике")]
            public int Chance = 10;
            [JsonProperty("Скорость плавки")]
            public double Speed = 3f;
            [JsonProperty("Название печи")]
            public string DisplayName = "Улучшенная печь";
            [JsonProperty("Описание печи")]
            public string Desk = "Плавит ресурсы в 2 раза быстрее!";
            [JsonProperty("СкинИД предмета")]
            public ulong SkinID = 10;  
            public static Configuration GetNewConf()
            {
                return new Configuration();
            }

        }
        protected override void LoadDefaultConfig() => _config = Configuration.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(_config);
        private Configuration _config = new Configuration();
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        #endregion

        #region OxideHooks
        object OnOvenToggle(BaseOven oven, BasePlayer player)
        {

            if (oven.HasFlag(BaseEntity.Flags.On)) return null;
            if (oven.skinID == _config.SkinID)
            {              
                double ovenMultiplier = _config.Speed;
                if (ovenMultiplier > 10f) ovenMultiplier = 10f;
                if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
                StartCooking(oven, oven.GetComponent<BaseEntity>(), ovenMultiplier);
                return false;
            }
            else
            {
                double ovenMultiplier = 1f;
                if (ovenMultiplier > 10f) ovenMultiplier = 10f;
                if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
                StartCooking(oven, oven.GetComponent<BaseEntity>(), ovenMultiplier);
                return false;
            }
        }
        float CookingTemperature(BaseOven.TemperatureType temperature)
        {
            switch (temperature)
            {
                case BaseOven.TemperatureType.Warming:
                    return 50f;
                case BaseOven.TemperatureType.Cooking:
                    return 200f;
                case BaseOven.TemperatureType.Smelting:
                    return 1000f;
                case BaseOven.TemperatureType.Fractioning:
                    return 1500f;
                default:
                    return 15f;
            }
        }
       
        private void OnServerInitialized()
        {
            LoadConfig();
        }
        void OnLootSpawn(LootContainer container)
        {
            if (container.ShortPrefabName == "stocking_large_deployed" ||
                container.ShortPrefabName == "stocking_small_deployed") return;
            if (ListContainers.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(1, 100) <= _config.Chance)
                {
                    if (container.inventory.itemList.Count == container.inventory.capacity)
                    {
                        container.inventory.capacity++;
                    }
                    Item i = ItemManager.CreateByName("furnace", 1, _config.SkinID);
                    i.name = $"<color=#️249c00>{_config.DisplayName}</color>" + $"<size=10>{_config.Desk}</size>";
                    i.MoveToContainer(container.inventory);
                }
            }
        }

        #endregion

        #region Function

        public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
                message = string.Format(message, args);
            player.SendConsoleCommand("chat.add 0", new object[2]
            {
                76561198090669418,
                string.Format("<size=16><color={2}>{0}</color>:</size>\n{1}", "Виртуальный помощник:", message, "#00bfff")
            });
        }
        void StartCooking(BaseOven oven, BaseEntity entity, double ovenMultiplier)
        {
            if (FindBurnable(oven) == null)
                return;
            oven.inventory.temperature = CookingTemperature(oven.temperature);
            oven.UpdateAttachmentTemperature();
            InvokeHandler.CancelInvoke(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook));
            InvokeHandler.InvokeRepeating(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook), (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));
            entity.SetFlag(BaseEntity.Flags.On, true, false);
        }
        Item FindBurnable(BaseOven oven)
        {
            if (oven.inventory == null)
                return null;
            foreach (Item current in oven.inventory.itemList)
            {
                ItemModBurnable component = current.info.GetComponent<ItemModBurnable>();
                if (component && (oven.fuelType == null || current.info == oven.fuelType))
                    return current;
            }
            return null;
        }
        private static string HexToRGB(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        public Item FindItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            Item item = null;

            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null && player.inventory.FindItemID(itemID).amount >= amount)
                    return player.inventory.FindItemID(itemID);
            }
            else
            {

                List<Item> items = new List<Item>();

                items.AddRange(player.inventory.FindItemIDs(itemID));

                foreach (var findItem in items)
                {
                    if (findItem.skin == skinID && findItem.amount >= amount)
                    {
                        return findItem;
                    }
                }
            }

            return item;
        }
        public bool HaveItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null &&
                    player.inventory.FindItemID(itemID).amount >= amount) return true;
                return false;
            }
            else
            {

                List<Item> items = new List<Item>();

                items.AddRange(player.inventory.FindItemIDs(itemID));

                foreach (var item in items)
                {
                    if (item.skin == skinID && item.amount >= amount)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

    }
}