using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Rust;
using System.Linq;
using System.Globalization;
namespace Oxide.Plugins
{
    [Info("EvoCop", "Urust", "0.0.01", ResourceId = 0)]
    class EvoCop : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Spawns, Craft;

        private static EvoCop ins;
        const string copPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";

        private bool initialized;

        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("evocop.craft", this);
            lang.RegisterMessages(Messages, this);
        }

        private void OnServerInitialized()
        {
            ins = this;
            initialized = true;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!initialized || player == null) return;

        }

        #endregion

        

        #region API
        private BaseEntity SpawnAtLocation(Vector3 position, Quaternion rotation = default(Quaternion), bool enableSaving = false, bool isExternallyManaged = false, bool repairEnabled = true, bool disableFuel = false, bool disableSecurity = false, bool disableCollision = false)
        {
            BaseEntity entity = GameManager.server.CreateEntity(copPrefab, position + Vector3.up, rotation);

            entity.Spawn();

            return entity;
        }

        #endregion

        

        #region Commands
        [ChatCommand("cop")]
        void CmdSpawnCarUser(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "evocop.craft"))
            {
                SendReply(player, "У тебя нету прав использовать эту команду!");
                return;
            }

            if (configData.options.copCraft)
            {
                foreach (var ingredient in bp.ingredients)
                {
                    var playeram = player.inventory.GetAmount(ingredient.itemDef.itemid);
                    if (playeram >= ingredient.amount) continue;
                    var replyPlayer = bp.ingredients.Select(x =>
                        string.Format(msg(player.inventory.GetAmount(x.itemDef.itemid) >= x.amount
                                ? "EnoughtIngridient"
                                : "NotEnoughtIngridient"
                            , player.UserIDString), x.itemDef.displayName.translated, player.inventory.GetAmount(x.itemDef.itemid),
                            x.amount)).ToArray();
                    SendReply(player, Messages["Noingridient"], string.Join("\n", replyPlayer));
                    return;
                }
                List<Item> items = new List<Item>();
                ItemCrafter itemCrafter = player.inventory.crafting;

                foreach (var ingridient in bp.ingredients)
                {
                    var amount = (int)ingridient.amount;
                    foreach (var container in itemCrafter.containers)
                    {
                        amount -= container.Take(items, ingridient.itemid, amount);
                        if (amount > 0)
                            continue;
                        break;
                    }
                }
            }

            Vector3 position = player.transform.position + (player.transform.forward * 3);
            RaycastHit hit;
            if (Physics.SphereCast(player.eyes.position, 0.1f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 20f))
                position = hit.point;
                        SpawnAtLocation(position, new Quaternion());
        }

        #endregion


        #region Config
        private ConfigData configData;
        private ItemBlueprint bp;
        private ItemDefinition def;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Options")]
            public Options options { get; set; }
            public class Options
            {
                [JsonProperty(PropertyName = "Включить крафт minicopter за ресурсы?")]
                public bool copCraft { get; set; }
                [JsonProperty(PropertyName = "Ресурсы для крафта minicopter (ShortName - Amount)")]
                public Dictionary<string, int> CraftItems = new Dictionary<string, int>();
            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }
            public int Amount = 1;
            protected override void LoadConfig()
            {
                base.LoadConfig();
                configData = Config.ReadObject<ConfigData>();

                if (configData.Version < Version)
                    UpdateConfigValues();

                Config.WriteObject(configData, true);

                var ingridients = new List<ItemAmount>();
                var defs = ItemManager.GetItemDefinitions();

                foreach (var item in configData.options.CraftItems)
                {
                    def = defs.FirstOrDefault(x =>
                        x.displayName.english == item.Key || x.shortname == item.Key || x.itemid.ToString() == item.Key);
                    if (!def)
                    {
                        PrintWarning(Messages["Nodef"], item.Key);
                        continue;
                    }
                    ingridients.Add(new ItemAmount(def, item.Value));
                }

                def = ItemManager.FindItemDefinition("workbench3");
                if (!def)
                {
                    PrintError("Unable to find the quarry defenition! The plugin can't work at all.\nPlease contact the developer");
                    Interface.Oxide.UnloadPlugin(Title);
                }
                bp = def.Blueprint;
                if (bp != null)
                {
                    var reply = 70;
                    if (reply == 0) { }
                    bp = def.gameObject.AddComponent<ItemBlueprint>();
                    bp.ingredients = ingridients;
                    bp.defaultBlueprint = false;
                    bp.userCraftable = true;
                    bp.isResearchable = false;
                    bp.amountToCreate = Amount;
                    bp.scrapRequired = 750;
                    bp.blueprintStackSize = 1;
                }
            }
        protected override void LoadDefaultConfig() => configData = GetBaseConfig();
        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                options = new ConfigData.Options
                {
                    copCraft = true,
                    CraftItems = new Dictionary<string, int>()
                    {
                        ["scrap"] = 1000,
                        ["metal.refined"] = 100,
                        ["gears"] = 25,
                        ["sheetmetal"] = 25,
                        ["metalspring"] = 15
                    }

                },

                Version = Version
            };
        }
        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Localization
        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Nodef"] = "Не найдено определение предмета {0}. Он не будет добавлен к цене крафта.",
            ["nopermission"] = "<color=#D3D3D3>У вас нет разрешения на управление MiniCopter</color>",
            ["EnoughtIngridient"] = "{0} - <color=#53f442>{1}</color>/{2}",
            ["NotEnoughtIngridient"] = "{0} - <color=#f44141>{1}</color>/{2}",
            ["Noingridient"] = "Не хватает ресурсов для крафта:\n{0}"

        };
        #endregion
    }
}

