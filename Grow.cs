using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Grow", "Doldak", "1.2.2")]
    [Description(
        "Allows players to grow plants instantly using ServerRewards, Economics, or Scrap as currency"
    )]
    class Grow : RustPlugin
    {
        [PluginReference]
        Plugin ServerRewards,
            Economics;
        #region Fields
        const string _growUse = "grow.use";
        #endregion

        #region Vars
        private ConfigData config;
        #endregion

        #region Config

        private class ConfigData
        {
            [JsonProperty("currency active?")]
            public bool SRA;

            [JsonProperty("if currency true = ServerRewards")]
            public bool UseServerRewards;

            [JsonProperty("if currency true = Economics")]
            public bool UseEconomics;

            [JsonProperty("if currency true = Scrap")]
            public bool UseScrap;

            [JsonProperty("cost")]
            public int cost;
        }

        private ConfigData GenerateConfig()
        {
            return new ConfigData
            {
                cost = 50,
                SRA = true,
                UseServerRewards = true,
                UseEconomics = false,
                UseScrap = false
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError(
                    "Configuration file is corrupt! Check your config file at https://jsonlint.com/"
                );
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GenerateConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    ["growd"] = "Grooow it Baby...",
                    ["costl_sr"] = "You take {0} RP",
                    ["costl_ec"] = "You take {0} Economics",
                    ["costl_scrap"] = "You take {0} Scrap",
                    ["nomoney"] = "You do not have enough {0} for that.",
                },
                this
            );

            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    ["growd"] = "Grooow it Baby...",
                    ["costl_sr"] = "Dir wurden {0} RP Abgezogen",
                    ["costl_ec"] = "Dir wurden {0} Economics Abgezogen",
                    ["costl_scrap"] = "Dir wurden {0} Scrap Abgezogen",
                    ["nomoney"] = "Du hast nicht genug {0}!",
                },
                this,
                "de"
            );
        }

        #region Oxide
        void OnServerInitialized()
        {
            string growd = lang.GetMessage("growd", this);
            string kosten = lang.GetMessage("costl", this);
            permission.RegisterPermission(_growUse, this);
        }

        [ChatCommand("grow")]
        private void growcomand(BasePlayer player)
        {
            string growd = lang.GetMessage("growd", this);

            if (!permission.UserHasPermission(player.UserIDString, _growUse))
            {
                return;
            }

            if (config.SRA)
            {
                if (!CheckBalance(player, config.cost))
                {
                    player.ChatMessage(Lang("nomoney", player.UserIDString, GetCurrencyName()));
                    return;
                }
                BalanceTake(player, config.cost);
                GrowAll(player);
                player.ChatMessage(growd);
                player.ChatMessage(
                    $"{String.Format(lang.GetMessage(GetCurrencyCostMessageKey(), this), config.cost)}"
                );
            }
            else
            {
                GrowAll(player);
                player.ChatMessage(growd);
            }
        }

        private string GetCurrencyName()
        {
            if (config.UseServerRewards)
                return "RP";
            if (config.UseEconomics)
                return "Economics";
            if (config.UseScrap)
                return "Scrap";
            return "";
        }

        private string GetCurrencyCostMessageKey()
        {
            if (config.UseServerRewards)
                return "costl_sr";
            if (config.UseEconomics)
                return "costl_ec";
            if (config.UseScrap)
                return "costl_scrap";
            return "";
        }

        #endregion
        #region Helpers

        public void GrowAll(BasePlayer player)
        {
            List<GrowableEntity> list = Facepunch.Pool.GetList<GrowableEntity>();
            Vis.Entities<GrowableEntity>(player.ServerPosition, 6f, list);
            foreach (GrowableEntity growableEntity in list)
            {
                if (growableEntity.isServer)
                    growableEntity.ChangeState(growableEntity.currentStage.nextState, false);
            }
            Facepunch.Pool.FreeList<GrowableEntity>(ref list);
        }

        bool CheckBalance(BasePlayer player, int cost)
        {
            if (
                config.UseServerRewards
                && ServerRewards?.Call<int>("CheckPoints", player.userID) >= cost
            )
            {
                return true;
            }

            if (
                config.UseEconomics
                && Economics?.Call<double>("Balance", player.userID) >= (double)cost
            )
            {
                return true;
            }

            if (
                config.UseScrap
                && player.inventory.GetAmount(ItemManager.FindItemDefinition("scrap").itemid)
                    >= cost
            )
            {
                return true;
            }

            return false;
        }

        public void BalanceTake(BasePlayer player, int cost)
        {
            if (config.UseServerRewards)
            {
                ServerRewards?.Call<object>("TakePoints", player.userID, cost, null);
            }

            if (config.UseEconomics)
            {
                Economics?.Call<object>("Withdraw", player.userID, (double)cost);
            }

            if (config.UseScrap)
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition("scrap").itemid, cost);
            }
        }

        string Lang(string key, string id = null, params object[] args) =>
            string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}
