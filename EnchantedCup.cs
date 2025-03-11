using System;
using System.Collections.Generic;
using Facepunch;
using UnityEngine;
using System.Reflection;
using System.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("EnchantedCup", "Vlad-00003", "1.0.2")]
    [Description("Allow user with a permission use command, that would prevent building in the current cup zone.")]
    //Author info:
    //E-mail: Vlad-00003@mail.ru
    //Vk: vk.com/vlad_00003

    class EnchantedCup : RustPlugin
    {
        #region Vars
        [PluginReference]
        Plugin NoEscape, ServerRewards, Economics;
        private PluginConfig config;
        private List<uint> Upgraded;
        private Dictionary<ItemDefinition, int> ItemsPayment = new Dictionary<ItemDefinition, int>();
        private Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);
        private class Constants
        {
            public static string TopTierFx = "assets/bundled/prefabs/fx/build/promote_toptier.prefab";
        }
        private string PanelName = "EnchantedCup.GUI";
        #endregion

        #region Config setup
        private class Price
        {
            [JsonProperty("Список предметов(короткое имя,полное имя на английском или ID)")]
            public Dictionary<string, int> Items;
            [JsonProperty("Монеты (Оставьте 0 если не используете плагин Economics)")]
            public int coins;
            [JsonProperty("Очки наград (Оставьте 0 если не используете плагин ServerRewards)")]
            public int RP;
        }
        private class GUI
        {
            [JsonProperty("Панель. Максимальный отступ")]
            public string Amax;
            [JsonProperty("Панель. Минимальный отступ")]
            public string Amin;
            [JsonProperty("Панель. Цвет")]
            public string Color;
            [JsonProperty("Текст. Максимальный отступ")]
            public string TextAmax;
            [JsonProperty("Текст. Минимальный отступ")]
            public string TextAmin;
            [JsonProperty("Текст. Цвет текста")]
            public string TextColor;
            [JsonProperty("Текст. Размер текста")]
            public int TextSize;
            [JsonProperty("Время автоматического скрывания панели")]
            public float Hide;
        }
        private class PluginConfig
        {
            [JsonProperty("Привилегия для использования команд")]
            public string Permission;
            [JsonProperty("Чат-команда для улучшения шкафа")]
            public string UpCommand;
            [JsonProperty("Чат-команда для снятия улучшения с шкафа")]
            public string DownCommand;
            //[JsonProperty("Chat format")]
            //public string ChatFormat;
            [JsonProperty("Разрешить строительство лестниц в зоне действия улучшенного шкафа")]
            public bool LadderPlacment;
            [JsonProperty("Цена улучшения")]
            public Price price;
            [JsonProperty("Процент возвращаемых ресурсов при снятии уличшения")]
            public double Refund;
            [JsonProperty("Радиус проверки на наличие шкафов")]
            public float CupRadius;
            [JsonProperty("Формат сообщения при НЕДОСТАТОЧНОМ количестве ресурсов")]
            public string NotEnoughtFormat;
            [JsonProperty("Формат сообщения при ДОСТАТОЧНОМ количестве ресурсов")]
            public string EnoughtFormat;
            [JsonProperty("Максимальное расстояние до шкафа при использовании команд")]
            public float Radius;
            [JsonProperty("Настройки графики")]
            public GUI gui;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Permission = "enchantedcup.use",
                    UpCommand = "/cupup",
                    DownCommand = "/cupdown",
                    //ChatFormat = "<color=#42f4c5>[EnchantedCup]</color> {0}",
                    LadderPlacment = false,
                    Radius = 2f,
                    EnoughtFormat = "{0}: <color=#009900>{1}</color>/{2}",
                    NotEnoughtFormat = "{0}: <color=#990000>{1}</color>/{2}",
                    gui = new GUI()
                    {
                        Amax = "0.64 0.29",
                        Amin = "0.344 0.12",
                        Color = "0.1 0.1 0.1 0.5",
                        TextAmax = "1 1",
                        TextAmin = "0 0",
                        TextColor = "0.443 0.867 0.941 1.0",
                        TextSize = 13,
                        Hide = 5f
                    },
                    price = new Price()
                    {
                        Items = new Dictionary<string, int>()
                        {
                            ["wood"] = 2000,
                            ["metal.fragments"] = 1000,
                            ["High Quality Metal"] = 20
                        },
                        RP = 0,
                        coins = 0
                    },
                    Refund = 50.0d,
                    CupRadius = 1.9f
                };
            }
        }
        #endregion

        #region Config Initialization
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            permission.RegisterPermission(config.Permission, this);
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Building blocked"] = "Building is blocked.",
                ["Upgrade"] = "Upgrade successfull! Now this cupboard will block twigs placment!",
                ["Downgrade"] = "Downgrade successfull! This cupboard no longer would block placment of twigs.",
                ["Can't pay"] = "You dont have enougth resources to upgrade this cupboard.\nPrice for the upgrade is:{0}",
                ["Coins"] = "Coins",
                ["RP"] = "Reward Points",
                ["No cup"] = "No tool cupboard found in front of you. Try to get closer.",
                ["No permission"] = "You don't have rights to do it",
                ["Not upgraded"] = "This cup isn't upgraded!",
                ["Already upgraded"] = "This tool cupboard already upgraded!",
                ["Not owned"] = "You doesn't own this tool cupboard!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Building blocked"] = "Строительство заблокировано.",
                ["Upgrade"] = "Улучшение завершено! Теперь в радиусе этого шкафа нельзя строиться в соломе!",
                ["Downgrade"] = "Улучшение снято! В зоне действия данного шкафа снова можно строить соломенные строения",
                ["Can't pay"] = "Недостаточно реурсов для улучшения данного шкафа.\nЦена улучшения:{0}",
                ["Coins"] = "Монеты",
                ["RP"] = "Очки Наград",
                ["No cup"] = "Шкаф с инструментами не найден! Попробуйте подойти по-ближе.",
                ["No permission"] = "Недостаточно прав на выполнение команды",
                ["Not upgraded"] = "Этот шкаф не является улучшенным!",
                ["Already upgraded"] = "Этот шкаф уже является улучшенным!",
                ["Not owned"] = "Этот шкаф не принадлежит вам!"
            }, this, "ru");
        }
        #endregion

        #region Data
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, Upgraded);
        }
        void LoadData()
        {
            try
            {
                Upgraded = Interface.Oxide.DataFileSystem.ReadObject<List<uint>>(Title);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load cupboard data file (is the file corrupt?) ({ex.Message})");
                Upgraded = new List<uint>();
            }
        }
        #endregion

        #region Init and quiting
        void Loaded()
        {
            LoadData();
            cmd.AddChatCommand(config.UpCommand.Replace("/", string.Empty), this, UpgradeCommand);
            cmd.AddChatCommand(config.DownCommand.Replace("/", string.Empty), this, DowngradeCommand);
        }
        void Unload()
        {
            SaveData();
            foreach(var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, PanelName);
            }
        }
        void OnServerInitialized()
        {
            foreach (var pay in config.price.Items)
            {
                ItemDefinition def = ItemManager.GetItemDefinitions().Where(p => p.shortname == pay.Key || p.displayName.english == pay.Key ||
                p.itemid.ToString() == pay.Key).FirstOrDefault();
                if (def == null)
                {
                    PrintWarning($"Failed to find item \"{pay.Key}\"! Check your config!");
                    continue;
                }
                if (ItemsPayment.ContainsKey(def))
                {
                    ItemsPayment[def] += pay.Value;
                    continue;
                }
                ItemsPayment.Add(def, pay.Value);
            }
            if(config.Refund > 100 || config.Refund < 0)
            {
                PrintWarning("Refund must be set in percentage - (0-100). Can't be hier or lower. set to 50%");
                config.Refund = 50d;
            }
            config.Refund = config.Refund / 100;
        }
        #endregion

        #region Chat Commands
        private void UpgradeCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanDo(player))
            {
                CreateGUI(player, "No permission");
                return;
            }
            var cupboard = GetCup(player, config.Radius);
            if (!cupboard || !(cupboard is BuildingPrivlidge))
            {
                CreateGUI(player, "No cup");
                return;
            }
            if(cupboard.OwnerID != player.userID)
            {
                CreateGUI(player, "Not owned");
                return;
            }
            if (Upgraded.Contains(cupboard.net.ID))
            {
                CreateGUI(player, "Already upgraded");
                return;
            }
            string price;
            if (!CanPay(player, out price))
            {
                CreateGUI(player, "Can't pay", price);
                return;
            }
            if (!Pay(player))
            {
                PrintWarning("Payment system crushed! Check if Economics or ServerRewards available!");
            }
            Upgraded.Add(cupboard.net.ID);
            Effect.server.Run(Constants.TopTierFx, cupboard, 0, Vector3.zero, Vector3.zero);
            CreateGUI(player, "Upgrade");
        }
        private void DowngradeCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanDo(player))
            {
                CreateGUI(player, "No permission");
                return;
            }
            var cupboard = GetCup(player, config.Radius);
            if (!cupboard || !(cupboard is BuildingPrivlidge))
            {
                CreateGUI(player, "No cup");
                return;
            }
            if (cupboard.OwnerID != player.userID)
            {
                CreateGUI(player, "Not owned");
                return;
            }
            if (!Upgraded.Contains(cupboard.net.ID))
            {
                CreateGUI(player, "Not upgraded");
                return;
            }
            if (!Refund(player))
            {
                PrintWarning("Refund system crushed! Check if Economics or ServerRewards available!");
            }
            Upgraded.Remove(cupboard.net.ID);
            Effect.server.Run(Constants.TopTierFx, cupboard, 0, Vector3.zero, Vector3.zero);
            CreateGUI(player, "Downgrade");
        }
        #endregion

        #region Oxide Hooks
        void OnEntityKill(BaseNetworkable entity)
        {
			if(entity?.net?.ID == null) return;
            if (Upgraded.Contains(entity.net.ID))
                Upgraded.Remove(entity.net.ID);
        }
        void OnServerSave()
        {
            SaveData();
        }
        #endregion

        #region Payment and refunding
        private bool Refund(BasePlayer player)
        {
            bool done = true;
            foreach(var kvp in ItemsPayment)
            {
                int amount = (int)(kvp.Value * config.Refund);
                amount = amount > 1 ? amount : 1;
                Item i = ItemManager.Create(kvp.Key, amount);
                player.GiveItem(i);
            }
            if(config.price.coins != 0)
            {
                int coins = (int)(config.price.coins * config.Refund);
                coins = coins > 1 ? coins : 1;
                Economics?.CallHook("Deposit", player.userID, coins);
                if (Economics == null)
                    done = false;
            }
            if(config.price.RP != 0)
            {
                int rp = (int)(config.price.RP * config.Refund);
                rp = rp > 1 ? rp : 1;
                var reward = ServerRewards?.CallHook("AddPoints", player.userID, rp);
                if (reward == null || !(bool)reward)
                    done = false;
            }
            return done;
        }
        private bool Pay(BasePlayer player)
        {
            bool done = true;
            List<Item> Taken = new List<global::Item>();
            foreach(var item in ItemsPayment)
            {
                player.inventory.Take(Taken, item.Key.itemid, item.Value);
                player.Command("note.inv", item.Key.itemid, -item.Value);
            }
            if(config.price.coins != 0)
            {
                var econ = Economics?.CallHook("Withdraw", player.userID, config.price.coins);
                if(econ == null || !(bool)econ)
                    done = false;
            }
            if(config.price.RP != 0)
            {
                var reward = ServerRewards?.CallHook("TakePoints", player.userID, config.price.RP);
                if(reward == null || !(bool)reward)
                    done = false;
            }
            foreach(var item in Taken)
            {
                item.Remove(1f);
            }
            return done;
        }
        private bool CanPay(BasePlayer player, out string price)
        {
            price = string.Empty;
            bool Can = true;
            foreach(var item in ItemsPayment)
            {
                int InvCount = player.inventory.GetAmount(item.Key.itemid);
                if (InvCount < item.Value)
                {
                    price += "\n" +  string.Format(config.NotEnoughtFormat, item.Key.displayName.english, InvCount, item.Value);
                    Can = false;
                    continue;
                }
                price += "\n" + string.Format(config.EnoughtFormat, item.Key.displayName.english, InvCount, item.Value);
            }
            if(config.price.coins != 0)
            {
                var money = Economics?.CallHook("GetPlayerMoney", player.userID);
                if (money == null || (double)money < config.price.coins)
                {
                    price += "\n" + string.Format(config.NotEnoughtFormat, GetMsg("Coins", player), money, config.price.coins);
                    Can = false;
                }else
                {
                    price += "\n" + string.Format(config.EnoughtFormat, GetMsg("Coins", player), money, config.price.coins);
                }
            }
            if(config.price.RP != 0)
            {
                var rewards = ServerRewards?.CallHook("CheckPoints", player.userID);
                if (rewards == null || (int)rewards < config.price.RP)
                {
                    price += "\n" + string.Format(config.NotEnoughtFormat, GetMsg("RP", player), rewards, config.price.RP);
                    Can = false;
                }else
                {
                    price += "\n" + string.Format(config.EnoughtFormat, GetMsg("RP", player), rewards, config.price.RP);
                }
            }
            return Can;
        }
        #endregion

        #region Main
        object CanBuild(Planner plan, Construction prefab)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            if (!player) return null;
            var result = NoEscape?.Call("CanDo", "build", player);
            if (result is string)
            {
                return null;
            }
            object Block = BuildingBlocked(plan, prefab);
            if (Block != null)
            {
                CreateGUI(player,"Building blocked");
                return false;
            }
            return null;
        }
        public object BuildingBlocked(Planner plan, Construction prefab)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            if (!player) return null;
            if (config.LadderPlacment && prefab.fullName.Contains("ladder.wooden")) return null;

            var pos = player.ServerPosition;
            var targetLocation = pos + (player.eyes.BodyForward() * 4f);
            var privilage = player.GetBuildingPrivilege(new OBB(targetLocation, new Quaternion(0, 0, 0, 0),
                new Bounds(Vector3.zero, Vector3.zero)));
            if (privilage && !privilage.IsAuthed(player) && Upgraded.Contains(privilage.net.ID))
                return true;
            return null;
        }
        #endregion

        #region GUI
        private void CreateGUI(BasePlayer player, string Langkey, params object[] args)
        {
            CuiHelper.DestroyUi(player, PanelName);
            string text = string.Format(GetMsg(Langkey, player), args);
            var elements = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = config.gui.Color
                        },
                        RectTransform =
                        {
                            AnchorMin = config.gui.Amin,
                            AnchorMax = config.gui.Amax
                        },
                        CursorEnabled = false
                    },
                    new CuiElement().Parent = "Overlay", PanelName
                }
            };
            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = text,
                    FontSize = config.gui.TextSize,
                    Align = TextAnchor.MiddleCenter,
                    Color = config.gui.TextColor
                },
                RectTransform =
                {
                    AnchorMin = config.gui.TextAmin,
                    AnchorMax = config.gui.TextAmax
                }
            }, PanelName);
            CuiHelper.AddUi(player, elements);
            timer.Once(config.gui.Hide, () => { CuiHelper.DestroyUi(player, PanelName); });
        }
        #endregion

        #region Helpers
        private BuildingPrivlidge GetCup(BasePlayer player, float radius)
        {
            RaycastHit RayHit;
            bool flag = Physics.Raycast(player.eyes.HeadRay(), out RayHit, radius);
            var cup = flag ? RayHit.GetEntity() : null;
            if (cup == null || !(cup is BuildingPrivlidge))
                return null;
            return (cup as BuildingPrivlidge);
        }
        private bool CanDo(BasePlayer player) => permission.UserHasPermission(player.UserIDString, config.Permission);
        //private void Reply(BasePlayer player, string langkey, params object[] args)
        //{
        //    SendReply(player, string.Format(config.ChatFormat, GetMsg(langkey, player)), args);
        //}
        private string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        private bool HasPerm(BasePlayer player) => permission.UserHasPermission(player.UserIDString, config.Permission);
        #endregion
    }
}
