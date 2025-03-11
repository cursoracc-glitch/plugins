using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("Smelt", "Rosty", "0.0.1")]
    class Smelt : RustPlugin
    {
        #region Config
        private PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Кулдавн")]
            public float cooldown;

            [JsonProperty("Чат команда")]
            public string command;

            [JsonProperty("Переплавлять дерево?")]
            public bool wood;

            [JsonProperty("Сообщения")]
            public Dictionary<string, string> messages;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    cooldown = 300f,
                    command = "smelt",
                    wood = true,
                    messages = new Dictionary<string, string>
                    {
                        {"M.COOLDOWN", "<color=#8888ff>[PsixRust]</color>  Переплавка инвентаря находится на перезарядке подождите:<color=#ff8888> {0} </color> сек."},
                        {"M.PERM", "<color=#8888ff>[PsixRust]</color> У вас нет разрешения на использование <color=#ff8888>smelt</color>." },
                        {"M.INV", "<color=#8888ff>[PsixRust]</color> Твой инвентарь переплавлен <color=#ff8888>successfully</color>." }
                    }
                };
            }
        }
        #endregion

        #region Initialize
        const string perminsta = "justsmelt.instant";
        const string permcommand = "justsmelt.command";
        static Dictionary<ulong, float> cooldowns = new Dictionary<ulong, float>();
        static ItemDefinition coal;

        private void OnServerInitialized()
        {
            permission.RegisterPermission(perminsta, this);
            permission.RegisterPermission(permcommand, this);
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand(config.command, this, "SmeltCmd");
            if (config.wood) coal = ItemManager.FindItemDefinition(-1938052175);
        }
        #endregion

    

        #region Command
        private void SmeltCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permcommand))
            {
                SendReply(player, config.messages["M.PERM"]);
                return;
            }

            float cooldown;
            if (cooldowns.TryGetValue(player.userID, out cooldown) && cooldown > UnityEngine.Time.realtimeSinceStartup)
            {
                SendReply(player, config.messages["M.COOLDOWN"], cooldowns[player.userID] - (int)UnityEngine.Time.realtimeSinceStartup);
                return;
            }

            foreach (Item item in player.inventory.AllItems()) SmeltIt(player, item);

            SendReply(player, config.messages["M.INV"]);
            cooldowns[player.userID] = (int)UnityEngine.Time.realtimeSinceStartup + config.cooldown;
        }
        #endregion

       #region Gathering
        /* private void OnItemAddedToContainer(ItemContainer container, Item item)
         {
             var player = container.GetOwnerPlayer();
             if (player == null || !permission.UserHasPermission(player.UserIDString, perminsta)) return;
             SmeltIt(player, item);
         }*/

        private object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();
            if (player == null || !permission.UserHasPermission(player.UserIDString, perminsta)) return null;
            return SmeltIt(player, item);
        }

        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (!permission.UserHasPermission(player.UserIDString, perminsta)) return null;
            return SmeltIt(player, item);
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perminsta)) return;
            SmeltIt(player, item);
        }
        #endregion

        #region Smelt
        private object SmeltIt(BaseEntity player, Item item)
        {
            if (item.info.itemid == -151838493 && config.wood) return GiveToPlayer(player, item, coal);
            else
            {
                ItemModCookable cookable = item.info.GetComponent<ItemModCookable>();
                if (cookable == null) return null;
                return GiveToPlayer(player, item, cookable.becomeOnCooked);
            }
        }

        private object GiveToPlayer(BaseEntity player, Item item, ItemDefinition def)
        {
            Item newItem = ItemManager.Create(def, item.amount);
            if (newItem == null) return null;
            item.Remove(0.0f);
            player.GiveItem(newItem, BaseEntity.GiveItemReason.ResourceHarvested);
            return true;
        }
        #endregion
    }
}