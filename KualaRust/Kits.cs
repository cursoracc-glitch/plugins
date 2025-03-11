using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Kits", "RustPlugin.ru", "1.1.21")]
    class Kits : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;
        static Kits ins;
        private PluginConfig config;
        private List<Kit> kitsList;
        private Dictionary<ulong, Dictionary<string, KitData>> PlayersData;
        private Dictionary<BasePlayer, List<Kit>> OpenGUI = new Dictionary<BasePlayer, List<Kit>>();
        public List<BasePlayer> AdminSetting = new List<BasePlayer>();
        private class RarityColor
        {
            [JsonProperty("Шанс выпадения предмета данной редкости")] public int Chance;
            [JsonProperty("Цвет этой редкости в интерфейсе")] public string Color;
            public RarityColor(int chance, string color)
            {
                Chance = chance;
                Color = color;
            }
        }
        class PluginConfig
        {
            [JsonProperty("Кастомные автокиты по привилегии (Привилегию устанавливаете в настройке кита) | Custom autokit, install privilege in the configuration of the kit")] public List<string> CustomAutoKits;
            [JsonProperty("Префикс чата | Chat Prefix")]
            public string DefaultPrefix
            {
                get;
                set;
            }
            [JsonProperty("Настройка цвета предмета по шансу")] public List<RarityColor> RaritiesColor = new List<RarityColor>();
            [JsonProperty("Версия конфигурации | Configuration Version")] public VersionNumber PluginVersion = new VersionNumber();
            public static PluginConfig CreateDefault()
            {
                return new PluginConfig
                {
                    CustomAutoKits = new List<string>() {
                        "autokit1", "autokit2"
                    }
                    ,
                    DefaultPrefix = "[Kit]",
                    PluginVersion = new VersionNumber(),
                    RaritiesColor = new List<RarityColor> {
                        new RarityColor(40, "1.00 1.00 1.00 0.3"), new RarityColor(30, "0.68 0.87 1.00 0.3"), new RarityColor(20, "0.77 0.65 1.00 0.3"), new RarityColor(10, "1.00 0.68 0.17 0.3"),
                    }
                    ,
                }
                ;
            }
            [JsonIgnore]
            [JsonProperty("Server Initialized")] public bool Init;

        }
        public class Kit
        {
            public string Name
            {
                get;
                set;
            }
            public string DisplayName
            {
                get;
                set;
            }
            public int Amount
            {
                get;
                set;
            }
            public double Cooldown
            {
                get;
                set;
            }
            public bool Hide
            {
                get;
                set;
            }
            public string Permission
            {
                get;
                set;
            }
            public string Color
            {
                get;
                set;
            }
            public List<KitItem> Items
            {
                get;
                set;
            }
        }
        public class KitItem
        {
            public string ShortName
            {
                get;
                set;
            }
            public int Amount
            {
                get;
                set;
            }
            public int Blueprint
            {
                get;
                set;
            }
            public ulong SkinID
            {
                get;
                set;
            }
            public string Container
            {
                get;
                set;
            }
            public float Condition
            {
                get;
                set;
            }
            public int Change
            {
                get;
                set;
            }

            public bool EnableCommand { get; set; }
            [JsonProperty("Command (Player identifier %STEAMID%)")]
            public string Command { get; set; }
            public string CustomImage { get; set; }
            public Weapon Weapon
            {
                get;
                set;
            }
            public List<ItemContent> Content
            {
                get;
                set;
            }
        }
        public class Weapon
        {
            public string ammoType
            {
                get;
                set;
            }
            public int ammoAmount
            {
                get;
                set;
            }
        }
        public class ItemContent
        {
            public string ShortName
            {
                get;
                set;
            }
            public float Condition
            {
                get;
                set;
            }
            public int Amount
            {
                get;
                set;
            }
        }
        public class KitData
        {
            public int Amount
            {
                get;
                set;
            }
            public double Cooldown
            {
                get;
                set;
            }
        }
        public class Position
        {
            public string AnchorMin
            {
                get;
                set;
            }
            public string AnchorMax
            {
                get;
                set;
            }
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            Config.Clear();
            Config.WriteObject(PluginConfig.CreateDefault(), true);
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            if (config.PluginVersion < Version) UpdateConfigValues();
            Config.WriteObject(config, true);
        }
        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.CreateDefault();
            if (config.PluginVersion < Version)
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }
        void OnPlayerRespawned(BasePlayer player)
        {
            foreach (var kits in config.CustomAutoKits)
            {
                if (kitsList.Exists(x => x.Name == kits))
                {
                    var kit1 = kitsList.First(x => x.Name.ToLower() == kits.ToLower());
                    if (permission.UserHasPermission(player.UserIDString, kit1.Permission))
                    {
                        player.inventory.Strip();
                        GiveItems(player, kit1);
                        return;
                    }
                }
            }
            if (kitsList.Exists(x => x.Name.ToLower() == "autokit"))
            {
                player.inventory.Strip();
                var kit = kitsList.First(x => x.Name.ToLower() == "autokit");
                GiveItems(player, kit);
            }
        }
        private void SaveKits()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits/KitsList", kitsList);
        }
        private void SaveData()
        {
            if (PlayersData != null) Interface.Oxide.DataFileSystem.WriteObject("Kits/PlayersData", PlayersData);
        }
        void OnServerSave()
        {
            SaveData();
            SaveKits();
        }
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kit Was Removed"] = "{Prefix}: Kit {kitname} was removed",
                ["Kit Doesn't Exist"] = "{Prefix}: This kit doesn't exist",
                ["Not Found Player"] = "{Prefix}: Player not found",
                ["To Many Player"] = "{Prefix}: Found multipy players",
                ["Permission Denied"] = "{Prefix}: Access denied",
                ["Limite Denied"] = "{Prefix}: Useage limite reached",
                ["Cooldown Denied"] = "{Prefix}: You will be able to use this kit after {time}",
                ["Reset"] = "{Prefix}Kits data wiped",
                ["Kit Already Exist"] = "{Prefix}Kit with the same name already exist",
                ["Kit Created"] = "{Prefix}You have created a new kit - {name}",
                ["Kit Extradited"] = "{Prefix}You have claimed kit - {kitname}",
                ["Kit Cloned"] = "{Prefix}You inventory was copyed to the kit",
                ["UI Amount"] = "<b>Timeleft: {amount}</b>",
                ["UI COOLDOWN"] = "Cooldown: {cooldown}",
                ["UI EXIT"] = "<b>EXIT</b>",
                ["UI READ"] = "<b>READ MORE</b>",
                ["UI NOLIMIT"] = "<b>no limit</b>",
                ["UI LIMIT"] = "<b>Limit: {limit}</b>",
                ["UI NOGIVE"] = "<b>NOT AVAILABLE</b>",
                ["UI GIVE"] = "YOU CAN TAKE",
                ["Help"] = "/kit name|add|clone|remove|list|reset",
                ["Help Add"] = "/kit add <kitname>",
                ["Help Clone"] = "/kit clone <kitname>",
                ["Help Remove"] = "/kit remove <kitname>",
                ["Help Give"] = "/kit give <kitname> <playerName|steamID>",
                ["Give Succes"] = "You have successfully given the player {0} a set {1}",
                ["No Space"] = "Can't redeem kit. Not enought space",
                ["UI Item Info"] = "If you see a percentage on an item, it means that with the indicated probability you can get this one.",
                ["UI Admin ON"] = "DISPLAY ALL KITS",
                ["UI Admin OFF"] = "HIDE ALL KITS",
                ["UI No Available"] = "There are no available ktis for you.",
            }
            , this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kit Was Removed"] = "{Prefix}{kitname} был удалён",
                ["Kit Doesn't Exist"] = "{Prefix}Этого комплекта не существует",
                ["Not Found Player"] = "{Prefix}Игрок не найден",
                ["To Many Player"] = "{Prefix}Найдено несколько игроков",
                ["Permission Denied"] = "{Prefix}У вас нет полномочий использовать этот комплект",
                ["Limite Denied"] = "{Prefix}Вы уже использовали этот комплект максимальное количество раз",
                ["Cooldown Denied"] = "{Prefix}Вы сможете использовать этот комплект через {time}",
                ["Reset"] = "{Prefix}Вы обнулили все данные о использовании комплектов игроков",
                ["Kit Already Exist"] = "{Prefix}Этот набор уже существует",
                ["Kit Created"] = "{Prefix}Вы создали новый набор - {name}",
                ["Kit Extradited"] = "{Prefix}Вы получили комплект {kitname}",
                ["Kit Cloned"] = "{Prefix}Предметы были скопированы из инвентаря в набор",
                ["UI Amount"] = "<b>Осталось: {amount}</b>",
                ["UI READ"] = "ПОДРОБНЕЕ",
                ["UI EXIT"] = "<b>ВЫХОД</b>",
                ["UI NOLIMIT"] = "<b>неогр.</b>",
                ["UI LIMIT"] = "<b>Лимит: {limit}</b>",
                ["UI COOLDOWN"] = "ЗАДЕРЖКА: {cooldown}",
                ["UI NOGIVE"] = "<b>НЕ ДОСТУПНО</b>",
                ["UI GIVE"] = "МОЖНО БРАТЬ",
                ["Help"] = "/kit name|add|clone|remove|list|reset",
                ["Help Add"] = "/kit add <kitname>",
                ["Help Clone"] = "/kit clone <kitname>",
                ["Help Remove"] = "/kit remove <kitname>",
                ["Help Give"] = "/kit give <kitname> <playerName|steamID>",
                ["Give Succes"] = "Вы успешно выдали игрок {0} набор {1}",
                ["No Space"] = "Невозможно получить набор - недостаточно места в инвентаре",
                ["UI Item Info"] = "Если вы видете на предмете процент, это значит что с указанной вероятностью вы сможете получить его.",
                ["UI Admin ON"] = "ОТОБРАЗИТЬ ВСЕ НАБОРЫ",
                ["UI Admin OFF"] = "СПРЯТАТЬ ВСЕ НАБОРЫ",
                ["UI No Available"] = "Для Вас нету доступных наборов.",
            }
            , this, "ru");
        }
        private void Loaded()
        {
            config = Config.ReadObject<PluginConfig>();
            LoadData();
            LoadMessages();
        }
        void LoadData()
        {
            try
            {
                kitsList = Interface.Oxide.DataFileSystem.ReadObject<List<Kit>>("Kits/KitsList");
                PlayersData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("Kits/PlayersData");
            }
            catch
            {
                kitsList = new List<Kit>();
                PlayersData = new Dictionary<ulong, Dictionary<string, KitData>>();
            }
            CheckKits();
        }
        private void Unload()
        {
            SaveData();
            BasePlayer.activePlayerList.ToList().ForEach(DestroyUI);
        }
        public bool AddImage(string url, string name, ulong skin) => (bool)ImageLibrary?.Call("AddImage", url, name, skin);
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        private void OnServerInitialized()
        {
            config.Init = true;
            ins = this;
            foreach (var kit in kitsList)
            {
                if (!permission.PermissionExists(kit.Permission)) permission.RegisterPermission(kit.Permission, this);
            }
            ImageLibrary.Call("AddImage", $"https://i.imgur.com/8iuRY3u.png", $"mailbox_1812087291");
            kitsList.ForEach(kit =>
               kit.Items.ForEach(item =>
               {
                   if (!string.IsNullOrEmpty(item.CustomImage))
                       ImageLibrary.Call("AddImage", item.CustomImage, item.CustomImage);
               })
           );
            timer.Repeat(1, 0, RefreshCooldownKitsUI);
        }
        void CheckKits()
        {
            kitsList.ForEach(kit =>
            {
                if (kit.Color == null) kit.Color = "0.55 0.68 0.31 0.6";
                kit.Items.ForEach(item =>
                {
                    if (item.Change <= 0) item.Change = 100;
                }
                );
            }
            );
            SaveKits();
        }
        private void OnPlayerDisconnected(BasePlayer player)
        {
            OpenGUI.Remove(player);
        }
        [ConsoleCommand("kit")]
        private void CommandConsoleKit(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
            {
                if (arg.Args[0].ToLower() == "give")
                {
                    var target = BasePlayer.Find(arg.Args[1]);
                    var kitname = arg.Args[2];
                    if (target != null) GiveKit(target, kitname, 0, true);
                    return;
                }
            }
            var player = arg.Player();
            var page = int.Parse(arg.Args[1]);
            if (!arg.HasArgs()) return;
            var value = arg.Args[0].ToLower();
            if (value == "ui")
            {
                CuiHelper.DestroyUi(player, $"ui.kits.{arg.Args[2]}.info");
                TriggerUI(player, page);
                return;
            }
            if (!OpenGUI.ContainsKey(player)) return;
            if (!OpenGUI[player].Contains(kitsList.First(kits => kits.Name.ToLower() == value.ToLower()))) return;
            GiveKit(player, value, page);
            var container = new CuiElementContainer();
            var kit = kitsList.First(x => x.Name.ToLower() == value.ToLower());
            var playerData = GetPlayerData(value, player.userID);
            if (kit.Amount > 0)
            {
                if (playerData.Amount >= kit.Amount)
                {
                    InitilizeKitsUI(player, 0);
                    CuiHelper.AddUi(player, container);
                    return;
                }
            }
            if (kit.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (playerData.Cooldown > currentTime)
                {
                    DestroyUI(player);
                    TriggerUI(player, page);
                }
            }
            CuiHelper.AddUi(player, container);
            return;
        }
        [ChatCommand("kit")]
        private void CommandChatKit(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (args.Length == 0)
            {
                TriggerUI(player, 0);
                return;
            }
            if (!player.IsAdmin)
            {
                GiveKit(player, args[0].ToLower(), 0);
                return;
            }
            switch (args[0].ToLower())
            {
                case "help":
                    SendReply(player, GetMsg("Help", player));
                    return;
                case "add":
                    if (args.Length < 2) SendReply(player, GetMsg("Help Add", player));
                    else KitCommandAdd(player, args[1].ToLower());
                    return;
                case "clone":
                    if (args.Length < 2) SendReply(player, GetMsg("Help Clone", player));
                    else KitCommandClone(player, args[1].ToLower());
                    return;
                case "remove":
                    if (args.Length < 2) SendReply(player, GetMsg("Help Remove", player));
                    else KitCommandRemove(player, args[1].ToLower());
                    return;
                case "list":
                    KitCommandList(player);
                    return;
                case "reset":
                    KitCommandReset(player);
                    return;
                case "give":
                    if (args.Length < 3)
                    {
                        SendReply(player, GetMsg("Help Give", player));
                    }
                    else
                    {
                        var foundPlayer = FindPlayer(player, args[1].ToLower());
                        if (foundPlayer == null) return;
                        SendReply(player, GetMsg("Give Succes", player), foundPlayer.displayName, args[2]);
                        KitCommandGive(player, foundPlayer, args[2].ToLower());
                    }
                    return;
                default:
                    GiveKit(player, args[0].ToLower(), 0);
                    return;
            }
        }
        private bool GiveKit(BasePlayer player, string kitname, int page = -1, bool admin = false)
        {
            if (string.IsNullOrEmpty(kitname)) return false;
            if (Interface.Oxide.CallHook("canRedeemKit", player) != null && page > -1) return false;
            if (!kitsList.Exists(x => x.Name.ToLower() == kitname.ToLower()))
            {
                SendReply(player, GetMsg("Kit Doesn't Exist", player));
                return false;
            }
            var kit = kitsList.First(x => x.Name.ToLower() == kitname.ToLower());
            if (!string.IsNullOrEmpty(kit.Permission) && !permission.UserHasPermission(player.UserIDString, kit.Permission) && !admin && page > -1)
            {
                SendReply(player, GetMsg("Permission Denied", player));
                return false;
            }
            var playerData = GetPlayerData(kitname, player.userID);
            if (kit.Amount > 0 && playerData.Amount >= kit.Amount && !admin && page > -1)
            {
                SendReply(player, GetMsg("Limite Denied", player));
                return false;
            }
            if (kit.Cooldown > 0 && !admin && page > -1)
            {
                var currentTime = GetCurrentTime();
                if (playerData.Cooldown > currentTime)
                {
                    SendReply(player, GetMsg("Cooldown Denied", player).Replace("{time}", TimeExtensions.FormatTime(TimeSpan.FromSeconds(playerData.Cooldown - currentTime))));
                    return false;
                }
            }
            int beltcount = kit.Items.Where(i => i.Container == "belt").Count();
            int wearcount = kit.Items.Where(i => i.Container == "wear").Count();
            int maincount = kit.Items.Where(i => i.Container == "main").Count();
            int totalcount = beltcount + wearcount + maincount;
            if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < beltcount || (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < wearcount || (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < maincount) if (totalcount > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
                {
                    player.ChatMessage(GetMsg("No Space", player));
                    return false;
                }
            GiveItems(player, kit);
            if (page > -1)
            {
                if (kit.Amount > 0)
                {
                    playerData.Amount += 1;
                }
                if (kit.Cooldown > 0) playerData.Cooldown = GetCurrentTime() + kit.Cooldown;
                EffectNetwork.Send(new Effect("assets/prefabs/misc/xmas/presents/effects/unwrap.prefab", player, 0, Vector3.up, Vector3.zero)
                {
                    scale = UnityEngine.Random.Range(0f, 1f)
                }
                );
                SendReply(player, GetMsg("Kit Extradited", player).Replace("{kitname}", kit.DisplayName));
                DestroyUI(player);
                TriggerUI(player, page);
            }
            return true;
        }
        private void KitCommandAdd(BasePlayer player, string kitname)
        {
            if (kitsList.Exists(x => x.Name == kitname))
            {
                SendReply(player, GetMsg("Kit Already Exist", player));
                return;
            }
            kitsList.Add(new Kit
            {
                Name = kitname,
                DisplayName = kitname,
                Cooldown = 600,
                Hide = true,
                Permission = "kits.default",
                Amount = 0,
                Color = "0.55 0.68 0.31 0.6",
                Items = GetPlayerItems(player)
            }
            );
            permission.RegisterPermission($"kits.default", this);
            SendReply(player, GetMsg("Kit Created", player).Replace("{name}", kitname));
            SaveKits();
            SaveData();
        }
        private void KitCommandClone(BasePlayer player, string kitname)
        {
            if (!kitsList.Exists(x => x.Name == kitname))
            {
                SendReply(player, GetMsg("Kit Doesn't Exist", player));
                return;
            }
            kitsList.First(x => x.Name.ToLower() == kitname.ToLower()).Items = GetPlayerItems(player);
            SendReply(player, GetMsg("Kit Cloned", player).Replace("{name}", kitname));
            SaveKits();
        }
        private void KitCommandRemove(BasePlayer player, string kitname)
        {
            if (kitsList.RemoveAll(x => x.Name == kitname) <= 0)
            {
                SendReply(player, GetMsg("Kit Doesn't Exist", player));
                return;
            }
            SendReply(player, GetMsg("Kit Was Removed", player).Replace("{kitname}", kitname));
            SaveKits();
        }
        private void KitCommandList(BasePlayer player)
        {
            foreach (var kit in kitsList) SendReply(player, $"{kit.Name} - {kit.DisplayName}");
        }
        private void KitCommandReset(BasePlayer player)
        {
            PlayersData.Clear();
            SendReply(player, GetMsg("Reset", player));
        }
        private void KitCommandGive(BasePlayer player, BasePlayer foundPlayer, string kitname)
        {
            var reply = 521;
            if (reply == 0) { }
            if (!kitsList.Exists(x => x.Name == reply.ToString())) { }
            if (!kitsList.Exists(x => x.Name == kitname))
            {
                SendReply(player, GetMsg("Kit Doesn't Exist", player));
                return;
            }
            GiveItems(foundPlayer, kitsList.First(x => x.Name.ToLower() == kitname.ToLower()));
        }
        private void GiveItems(BasePlayer player, Kit kit)
        {
            foreach (var kitem in kit.Items)
            {
                if (kitem.EnableCommand && !string.IsNullOrEmpty(kitem.Command))
                {
                    Server.Command(kitem.Command.Replace("%STEAMID%", player.UserIDString));
                    continue;
                }
                GiveItem(player, BuildItem(kitem.ShortName, kitem.Amount, kitem.SkinID, kitem.Condition, kitem.Blueprint, kitem.Weapon, kitem.Content), kitem.Change, kitem.Container == "belt" ? player.inventory.containerBelt : kitem.Container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
            }
        }
        private void GiveItem(BasePlayer player, Item item, int percent, ItemContainer cont = null)
        {
            if (item == null) return;
            var inv = player.inventory;
            if (UnityEngine.Random.Range(1, 100) < percent)
            {
                var moved = item.MoveToContainer(cont) || item.MoveToContainer(inv.containerMain);
                if (!moved)
                {
                    if (cont == inv.containerBelt) moved = item.MoveToContainer(inv.containerWear);
                    if (cont == inv.containerWear) moved = item.MoveToContainer(inv.containerBelt);
                }
                if (!moved) item.Drop(player.GetCenter(), player.GetDropVelocity());
            }
        }
        private Item BuildItem(string ShortName, int Amount, ulong SkinID, float Condition, int blueprintTarget, Weapon weapon, List<ItemContent> Content)
        {
            Item item = ItemManager.CreateByName(ShortName, Amount > 1 ? Amount : 1, SkinID);
            item.condition = Condition;
            if (blueprintTarget != 0) item.blueprintTarget = blueprintTarget;
            if (weapon != null)
            {
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.ammoAmount;
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType = ItemManager.FindItemDefinition(weapon.ammoType);
            }
            if (Content != null)
            {
                foreach (var cont in Content)
                {
                    Item new_cont = ItemManager.CreateByName(cont.ShortName, cont.Amount);
                    new_cont.condition = cont.Condition;
                    new_cont.MoveToContainer(item.contents);
                }
            }
            return item;
        }
        [ConsoleCommand("kits.page")]
        void cmdKitsPage(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var page = int.Parse(args.Args[0]);
            InitilizeKitsUI(player, page);
        }
        private void TriggerUI(BasePlayer player, int page)
        {
            if (OpenGUI.ContainsKey(player)) DestroyUI(player);
            else InitilizeUI(player, page);
        }
        private void InitilizeUI(BasePlayer player, int page)
        {
            CuiHelper.DestroyUi(player, $"ui.kits.info");
            var kits = GetKitsForPlayer(player).ToList();
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {
                    Color="0.19 0.19 0.18 0.6", /*Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" */
				}
                ,
                RectTransform = {
                    AnchorMin="0 0", AnchorMax="1 1"
                }
                ,
                CursorEnabled = true
            }
            , "Overlay", "ui.kits");
            CuiHelper.AddUi(player, container);
            InitilizeKitsUI(player, page);
        }
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"ui.kits.info");
            if (!OpenGUI.ContainsKey(player)) return;
            foreach (var kitname in OpenGUI[player])
            {
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.time");
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.mask");
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.button");
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.amount");
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}");
            }
            CuiHelper.DestroyUi(player, "ui.kits");
            OpenGUI.Remove(player);
        }
        private void RefreshCooldownKitsUI()
        {
            var currentTime = GetCurrentTime();
            List<Kit> toRemove = new List<Kit>();
            foreach (var playerGUIData in OpenGUI)
            {
                if (!PlayersData.ContainsKey(playerGUIData.Key.userID)) continue;
                var playerKitsData = PlayersData[playerGUIData.Key.userID];
                foreach (var kitname in playerGUIData.Value)
                {
                    var playerKitData = playerKitsData[kitname.Name];
                    if (playerKitData.Cooldown > 0)
                    {
                        if (playerKitData.Cooldown > currentTime)
                        {
                            var container = new CuiElementContainer();
                            InitilizeCooldown(ref container, playerGUIData.Key, kitname, 0);
                            CuiHelper.AddUi(playerGUIData.Key, container);
                        }
                    }
                }
                toRemove.ForEach(p => OpenGUI[playerGUIData.Key].Remove(p));
            }
        }
        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds(time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            if (hours > 0) return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
            else return string.Format("{0:00}:{1:00}", mins, secs);
        }
        private void InitilizeKitsUI(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, $"ui.kits1");
            OpenGUI[player] = new List<Kit>();
            var currentTime = GetCurrentTime();
            var kits = GetKitsForPlayer(player).Skip(5 * page).Take(5).ToList();
            int i = 0;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {
                    Color="1 1 1 0"
                }
                ,
                RectTransform = {
                    AnchorMin=$"0 0", AnchorMax=$"1 1"
                }
            }
            , "ui.kits", $"ui.kits1");
            container.Add(new CuiElement
            {
                Parent = "ui.kits1",
                Components = {
                    new CuiButtonComponent {
                        Color="0.16 0.15 0.31 0", Command=$"kit ui {page} 1"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="-100 -100", AnchorMax="100 100", OffsetMax="0 0"
                    }
                    ,
                }
                ,
            }
            );
            var pos = 0.5f - (GetKitsForPlayer(player).Skip(5 * page).Take(5).Count() * 0.175f + (GetKitsForPlayer(player).Skip(5 * page).Take(5).Count() - 1) * 0.005f) / 2;
            if (kits.Count > 0) foreach (var kit in kits)
                {
                    CuiHelper.DestroyUi(player, $"ui.kits.{kit.Name}");
                    OpenGUI[player].Add(kit);
                    var playerData = GetPlayerData(kit.Name, player.userID);
                    container.Add(new CuiPanel
                    {
                        Image = {
                        Color="0.43 0.43 0.42 0"
                    }
                        ,
                        RectTransform = {
                        AnchorMin=$"{pos} 0.43", AnchorMax=$"{pos + 0.175f} 0.57"
                    }
                    }
                    , "ui.kits1", $"ui.kits.main.{kit.Name}");
                    container.Add(new CuiPanel
                    {
                        Image = {
                        Color="1 1 1 0.25"
                    }
                        ,
                        RectTransform = {
                        AnchorMin=$"0 0.32", AnchorMax=$"1 0.99"
                    }
                    }
                    , $"ui.kits.main.{kit.Name}", $"ui.kits.{kit.Name}");
                    pos += 0.175f + 0.005f;
                    InitilizeCooldown(ref container, player, kit, page);
                    container.Add(new CuiButton
                    {
                        Button = {
                        Color="0.75 0.75 0.75 0.5", Command=$"kit.drawkitinfo {kit.Name} {page}", /*Material = "assets/content/ui/ui.background.transparent.radial.psd",*/
					}
                        ,
                        RectTransform = {
                        AnchorMin="0 0.01", AnchorMax="1 0.28"
                    }
                        ,
                        Text = {
                        Text="", Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter, FontSize=18
                    }
                    }
                    , $"ui.kits.main.{kit.Name}", $"ui.kits.{kit.Name}.button");
                    container.Add(new CuiElement
                    {
                        Parent = $"ui.kits.{kit.Name}.button",
                        Components = {
                        new CuiTextComponent {
                            FadeIn=0.1f, Color="1 1 1 1", Text=GetMsg("UI READ", player).ToUpper(), FontSize=18, Font="robotocondensed-bold.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                        ,
                    }
                    );
                    i++;
                }
            else container.Add(new CuiElement
            {
                Parent = "ui.kits1",
                Components = {
                    new CuiTextComponent {
                        FadeIn=1f, Color="1 1 1 0.5", Text=GetMsg("UI No Available", player).ToUpper(), FontSize=35, Font="robotocondensed-bold.ttf", Align=TextAnchor.MiddleCenter,
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0 0.4", AnchorMax=$"1 0.6"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 0.3", Distance="-0.5 0.5"
                    }
                }
                ,
            }
            );
            if (player.IsAdmin)
            {
                container.Add(new CuiPanel
                {
                    Image = {
                        Color="1 1 1 0.01", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    ,
                    RectTransform = {
                        AnchorMin="0.8 0.37", AnchorMax="0.95 0.41"
                    }
                    ,
                }
                , $"ui.kits1", "kits.adminSettings");
                container.Add(new CuiPanel
                {
                    Image = {
                        Color=AdminSetting.Contains(player) ? "1 1 1 0.7": "1 1 1 0.1", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    ,
                    RectTransform = {
                        AnchorMin="0.01 0.2", AnchorMax="0.1 0.8"
                    }
                    ,
                }
                , "kits.adminSettings");
                container.Add(new CuiElement
                {
                    Parent = "kits.adminSettings",
                    Components = {
                        new CuiTextComponent {
                            Color="1 1 1 1", Text=AdminSetting.Contains(player) ? GetMsg("UI Admin OFF", player): GetMsg("UI Admin ON", player), FontSize=12, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0.1 0", AnchorMax=$"1 1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
                container.Add(new CuiButton
                {
                    Button = {
                        Color="1 1 1 0", Command=$"UIkits_adminSettings {page}"
                    }
                    ,
                    RectTransform = {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                    ,
                    Text = {
                        Text="", Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter, Color="1 1 1 0"
                    }
                }
                , "kits.adminSettings");
            }
            container.Add(new CuiButton
            {
                Button = {
                    Color="1 1 1 0", Command=GetKitsForPlayer(player).Skip(5 * (page + 1)).Count() > 0 ? $"kits.page {page + 1}": $""
                }
                ,
                RectTransform = {
                    AnchorMin="0.95 0.4", AnchorMax="1 0.6"
                }
                ,
                Text = {
                    Text=">", Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter, FontSize=80, Color=GetKitsForPlayer(player).Skip(5 * (page + 1)).Count() > 0 ? "1 1 1 1": "1 1 1 0.3"
                }
            }
            , $"ui.kits1", $"ui.kits1.buttonNext");
            container.Add(new CuiButton
            {
                Button = {
                    Color="1 1 1 0", Command=page > 0 ? $"kits.page {page - 1}": ""
                }
                ,
                RectTransform = {
                    AnchorMin="0 0.4", AnchorMax="0.05 0.6"
                }
                ,
                Text = {
                    Text="<", Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter, FontSize=80, Color=page > 0 ? "1 1 1 1": "1 1 1 0.3"
                }
            }
            , $"ui.kits1", $"ui.kits1.buttonNext");
            CuiHelper.AddUi(player, container);
        }
        private void InitilizeCooldown(ref CuiElementContainer container, BasePlayer player, Kit kit, int page)
        {
            CuiHelper.DestroyUi(player, $"ui.kits.{kit.Name}.kitmain");
            var playerData = GetPlayerData(kit.Name, player.userID);
            container.Add(new CuiPanel
            {
                Image = {
                    Color="0.75 0.75 0.75 0.4", /*Material= "assets/content/ui/ui.background.tile.psd", */
				}
                ,
                RectTransform = {
                    AnchorMin=$"0 0", AnchorMax=$"1 1"
                }
            }
            , $"ui.kits.{kit.Name}", $"ui.kits.{kit.Name}.kitmain");
            if (kit.Cooldown > 0 && playerData.Cooldown - 1 < GetCurrentTime() || kit.Cooldown == 0)
            {
                container.Add(new CuiPanel
                {
                    Image = {
                        Color=kit.Color
                    }
                    ,
                    RectTransform = {
                        AnchorMin=$"0 0", AnchorMax=$"0.994 0.97"
                    }
                }
                , $"ui.kits.{kit.Name}.kitmain");
                container.Add(new CuiButton
                {
                    Button = {
                        Color="0.75 0.75 0.75 0", Command=$"kit {kit.Name}"
                    }
                    ,
                    RectTransform = {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                    ,
                    Text = {
                        Text="", Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter, FontSize=18
                    }
                }
                , $"ui.kits.{kit.Name}", $"ui.kits.{kit.Name}.kitmain");
                container.Add(new CuiElement
                {
                    Parent = $"ui.kits.{kit.Name}.kitmain",
                    Components = {
                        new CuiTextComponent {
                            Color="1 1 1 1", Text=$"<b>{kit.DisplayName.ToUpper()}</b>", FontSize=22, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
                container.Add(new CuiElement
                {
                    Parent = $"ui.kits.{kit.Name}.kitmain",
                    Components = {
                        new CuiTextComponent {
                            Color="0.85 0.85 0.85 1.00", Text=GetMsg("UI GIVE", player), FontSize=14, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 0.3"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
                if (kit.Amount > 0)
                {
                    var amount = kit.Amount - playerData.Amount;
                    container.Add(new CuiElement
                    {
                        Parent = $"ui.kits.{kit.Name}.kitmain",
                        Components = {
                            new CuiTextComponent {
                                Color="0.85 0.85 0.85 1.00", Text=GetMsg("UI Amount", player).Replace("{amount}", amount.ToString()), FontSize=11, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleRight,
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"0 0.75", AnchorMax=$"0.95 0.95"
                            }
                            , new CuiOutlineComponent {
                                Color="0 0 0 0.3", Distance="-0.5 0.5"
                            }
                        }
                        ,
                    }
                    );
                }
                container.Add(new CuiButton
                {
                    Button = {
                        Color="0.75 0.75 0.75 0", Command=playerData.Cooldown - 1 < GetCurrentTime() ? $"kit {kit.Name} {page}": ""
                    }
                    ,
                    RectTransform = {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                    ,
                    Text = {
                        Text="", Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter, FontSize=18
                    }
                }
                , $"ui.kits.{kit.Name}");
            }
            else
            {
                var time = TimeSpan.FromSeconds(playerData.Cooldown - GetCurrentTime());
                var AnchorType = kit.Cooldown / (float)time.TotalSeconds - 0.03f;
                var max = 1 - ((time.TotalSeconds + (float)kit.Cooldown / 60) / kit.Cooldown);
                container.Add(new CuiPanel
                {
                    Image = {
                        Color=kit.Color
                    }
                    ,
                    RectTransform = {
                        AnchorMin=$"0 0", AnchorMax=$"{max} 0.97"
                    }
                }
                , $"ui.kits.{kit.Name}.kitmain", $"ui.kits.{kit.Name}.time");
                container.Add(new CuiElement
                {
                    Parent = $"ui.kits.{kit.Name}.kitmain",
                    Components = {
                        new CuiTextComponent {
                            Color="1 1 1 1", Text=$"<b>{kit.DisplayName.ToUpper()}</b>", FontSize=22, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
                container.Add(new CuiElement
                {
                    Parent = $"ui.kits.{kit.Name}.kitmain",
                    Components = {
                        new CuiTextComponent {
                            Color="0.85 0.85 0.85 1.00", Text=TimeExtensions.FormatShortTime(time), FontSize=14, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 0.3"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
            }
        }
        [ConsoleCommand("kit.drawkitinfo")]
        void cmdDrawKitInfo(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null || player.Connection == null) return;
            var kit = kitsList.First(kits => kits.Name.ToLower() == args.Args[0].ToLower());
            if (kit == null) return;
            DrawKitInfo(player, kit, int.Parse(args.Args[1]));
        }
        void DrawKitInfo(BasePlayer player, Kit kit, int page)
        {
            DestroyUI(player);
            CuiHelper.DestroyUi(player, $"ui.kits.info");


            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {
                    Color="0.19 0.19 0.18 0.6"/*, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" */
				}
                ,
                RectTransform = {
                    AnchorMin="0 0", AnchorMax="1 1"
                }
                ,
                CursorEnabled = true
            }
            , "Overlay", $"ui.kits.info");
            container.Add(new CuiElement
            {
                Parent = $"ui.kits.info",
                Components = {
                    new CuiTextComponent {
                        Color="1 1 1 1", Text=$"<b>{kit.DisplayName.ToUpper()}</b>", FontSize=30, Align=TextAnchor.MiddleCenter,
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0 0.8", AnchorMax=$"1 1"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 0.3", Distance="-0.5 0.5"
                    }
                }
                ,
            }
            );
            var pos = 0.5f - ((kit.Items.Count > 10 ? 10 : kit.Items.Count) * 0.09f + ((kit.Items.Count > 10 ? 10 : kit.Items.Count) - 1) * 0.005f) / 2;
            var newline1 = 0.43;
            var newline2 = 0.57;
            if (kit.Items.Count > 10)
            {
                newline1 = 0.58;
                newline2 = 0.72;
            }
            container.Add(new CuiElement
            {
                Parent = $"ui.kits.info",
                Components = {
                    new CuiTextComponent {
                        Color="0.85 0.85 0.85 1.00", Text=GetMsg("UI Item Info", player), FontSize=14, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0 0", AnchorMax=$"1 0.3"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 0.3", Distance="-0.5 0.5"
                    }
                }
                ,
            }
            );
            container.Add(new CuiElement
            {
                Parent = $"ui.kits.info",
                Components = {
                    new CuiButtonComponent {
                        Color="0.16 0.15 0.31 0", Command=$"kit ui {page} {kit.Name}"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="-100 -100", AnchorMax="100 100", OffsetMax="0 0"
                    }
                    ,
                }
                ,
            }
            );
            foreach (var item in kit.Items.OrderBy(p => p.Change < p.Change).Select((i, t) => new
            {
                A = i,
                B = t
            }
            ).Take(30))
            {
                var element = config.RaritiesColor.Find(p => p.Chance == ChangeSelect(item.A.Change));

                if (element == null)
                {
                    PrintError($"Ошибка загрузки цвета шанса у кита {kit.Name} {item.A.ShortName} Шанс {item.A.Change} {element.Color}");
                    continue;
                }
                container.Add(new CuiButton
                {
                    RectTransform = {
                        AnchorMin=$"{pos} {newline1}", AnchorMax=$"{pos + 0.09f} {newline2}", OffsetMax="0 0"
                    }
                    ,
                    Button = {
                        Color=element.Color
                    }
                    ,
                    Text = {
                        Text=""
                    }
                }
                , $"ui.kits.info", $"ui.kits.info" + $".{item.B}");
                pos += 0.09f + 0.005f;
                container.Add(new CuiElement
                {
                    Parent = $"ui.kits.info" + $".{item.B}",
                    Components = {
                        new CuiRawImageComponent {
                                                       Png =!string.IsNullOrEmpty(item.A.CustomImage) ? GetImage(item.A.CustomImage) :  item.A.ShortName == "mailbox"? GetImage("mailbox_1812087291") : GetImage(item.A.ShortName, item.A.SkinID),

                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0.1 0", AnchorMax="0.9 0.97", OffsetMax="0 0"
                        }
                        ,
                    }
                    ,
                }
                );
                container.Add(new CuiElement
                {
                    Parent = $"ui.kits.info" + $".{item.B}",
                    Components = {
                        new CuiTextComponent {
                            Color="0.85 0.85 0.85 1.00", Text=$"<b>x{item.A.Amount}</b>", FontSize=12, Align=TextAnchor.MiddleRight,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0.4 0", AnchorMax=$"0.95 0.2"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
                if (item.A.Change < 99) container.Add(new CuiElement
                {
                    Parent = $"ui.kits.info" + $".{item.B}",
                    Components = {
                        new CuiTextComponent {
                            Color="0.85 0.85 0.85 0.7", Text=$"<b>{item.A.Change}%</b>", FontSize=45, Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.3", Distance="-0.5 0.5"
                        }
                    }
                    ,
                }
                );
                if (item.B == 9)
                {
                    newline1 = 0.43;
                    newline2 = 0.57;
                    pos = 0.5f - ((kit.Items.Skip(10).ToList().Count > 10 ? 10 : kit.Items.Skip(10).ToList().Count) * 0.09f + ((kit.Items.Skip(10).ToList().Count > 10 ? 10 : kit.Items.Skip(10).ToList().Count) - 1) * 0.005f) / 2;
                }
                if (item.B == 19)
                {
                    newline1 = 0.28;
                    newline2 = 0.42;
                    pos = 0.5f - ((kit.Items.Skip(20).ToList().Count > 10 ? 10 : kit.Items.Skip(20).ToList().Count) * 0.09f + ((kit.Items.Skip(20).ToList().Count > 10 ? 10 : kit.Items.Skip(20).ToList().Count) - 1) * 0.005f) / 2;
                }
            }
            CuiHelper.AddUi(player, container);
        }
        private int? ChangeSelect(int x)
        {
            var num = (from number in config.RaritiesColor.Select(p => p.Chance)
                       let difference = Math.Abs(number - x)
                       orderby difference, Math.Abs(number), number descending
                       select number)
                .FirstOrDefault();
            return num;
        }
        private void SendEffectToPlayer2(BasePlayer player, string effectPrefab)
        {
            EffectNetwork.Send(new Effect(effectPrefab, player.transform.position, Vector3.zero), player.net.connection);
        }
        [ConsoleCommand("UIkits_adminSettings")]
        void adminSettingsKit(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            int page = int.Parse(args.Args[0]);
            if (!AdminSetting.Contains(player))
            {
                AdminSetting.Add(player);
                DestroyUI(player);
                TriggerUI(player, page);
            }
            else
            {
                AdminSetting.Remove(player);
                DestroyUI(player);
                TriggerUI(player, page);
            }
        }
        private KitData GetPlayerData(string name, ulong playerid = 533504)
        {
            if (!PlayersData.ContainsKey(playerid)) PlayersData[playerid] = new Dictionary<string, KitData>();
            if (!PlayersData[playerid].ContainsKey(name)) PlayersData[playerid][name] = new KitData();
            return PlayersData[playerid][name];
        }
        private List<KitItem> GetPlayerItems(BasePlayer player)
        {
            List<KitItem> kititems = new List<KitItem>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "wear");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "main");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "belt");
                    kititems.Add(iteminfo);
                }
            }
            return kititems;
        }
        string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString).Replace("{Prefix}", config.DefaultPrefix);
        private KitItem ItemToKit(Item item, string container)
        {
            KitItem kitem = new KitItem();
            kitem.Amount = item.amount;
            kitem.Container = container;
            kitem.SkinID = item.skin;
            kitem.Blueprint = item.blueprintTarget;
            kitem.ShortName = item.info.shortname;
            kitem.Condition = item.condition;
            kitem.Change = 100;
            kitem.Weapon = null;
            kitem.Content = null;
            if (item.info.category == ItemCategory.Weapon)
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    kitem.Weapon = new Weapon();
                    kitem.Weapon.ammoType = weapon.primaryMagazine.ammoType.shortname;
                    kitem.Weapon.ammoAmount = weapon.primaryMagazine.contents;
                }
            }
            if (item.contents != null)
            {
                kitem.Content = new List<ItemContent>();
                foreach (var cont in item.contents.itemList)
                {
                    kitem.Content.Add(new ItemContent()
                    {
                        Amount = cont.amount,
                        Condition = cont.condition,
                        ShortName = cont.info.shortname
                    }
                    );
                }
            }
            return kitem;
        }
        private List<Kit> GetKitsForPlayer(BasePlayer player)
        {
            if (AdminSetting.Contains(player))
            {
                return kitsList.ToList();
            }
            else return kitsList.Where(kit => !kit.Hide && (string.IsNullOrEmpty(kit.Permission) || permission.UserHasPermission(player.UserIDString, kit.Permission)) && (kit.Amount == 0 || (kit.Amount > 0 && GetPlayerData(kit.Name, player.userID).Amount < kit.Amount))).ToList();
        }
        private BasePlayer FindPlayer(BasePlayer player, string nameOrID)
        {
            ulong id;
            if (ulong.TryParse(nameOrID, out id) && nameOrID.StartsWith("7656119") && nameOrID.Length == 17)
            {
                var findedPlayer = BasePlayer.FindByID(id);
                if (findedPlayer == null || !findedPlayer.IsConnected)
                {
                    SendReply(player, GetMsg("Not Found Player", player));
                    return null;
                }
                return findedPlayer;
            }
            var foundPlayers = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrID.ToLower()));
            if (foundPlayers.Count() == 0)
            {
                SendReply(player, GetMsg("Not Found Player", player));
                return null;
            }
            if (foundPlayers.Count() > 1)
            {
                SendReply(player, GetMsg("To Many Player", player));
                return null;
            }
            return foundPlayers.First();
        }
        private double GetCurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        private static class TimeExtensions
        {
            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0) result += $"{time.Days} д. ";
                if (time.Hours != 0) result += $"{time.Hours} ч. ";
                if (time.Minutes != 0) result += $"{time.Minutes} м. ";
                if (time.Seconds != 0) result += $"{time.Seconds} с. ";
                return result;
            }
            public static string FormatTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0) result += $"{Format(time.Days, "дней", "дня", "день")} ";
                if (time.Hours != 0) result += $"{Format(time.Hours, "часов", "часа", "час")} ";
                if (time.Minutes != 0) result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";
                if (time.Seconds != 0) result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";
                return result;
            }
            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;
                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
                if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
                return $"{units} {form3}";
            }
        }
        [HookMethod("isKit")]
        public bool isKit(string kitName)
        {
            if (kitsList.Select(p => p.Name == kitName) != null) return true;
            return false;
        }
        [HookMethod("GetAllKits")] public string[] GetAllKits() => kitsList.Select(p => p.Name).ToArray();
    }
}