using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// ReSharper disable UnusedMember.Local

namespace Oxide.Plugins
{
    [Info("Blueprints Unlocker", "Vlad-00003", "1.2.1")]
    [Description("Unlock all blueprints to the players")]
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     */
    class BPUnlockerVip : RustPlugin
    {
        #region Vars
        private PluginConfig _config;
        private readonly List<ItemDefinition> _available = new List<ItemDefinition>();
        private Timer _updater;
        //private static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();
        private readonly Queue<ulong> _order = new Queue<ulong>();
        private bool _inProgress;
        #endregion

        #region Config
        private class PluginConfig
        {
            [JsonProperty("Permission to automaticly unlock ALL blueprints")]
            public string All = "bpunlockervip.all";
            [JsonProperty("Permission to remove workbench requirements")]
            public string NoWorkbench = "bpunlockervip.noworkbench";
            [JsonProperty("List of cutom permissions")]
            public Dictionary<string, List<string>> CustomPermissions;
            [JsonProperty("List of all available blueprints to unlock(editing does nothing)")]
            public Dictionary<string, List<string>> Available = new Dictionary<string, List<string>>();
            [JsonProperty("Command to unlock/lock blueprints for the player")]
            public string Command = "bp";
            [JsonProperty("Permission to use command")]
            public string CommandPermission = "bpunlockervip.admin";

            [JsonIgnore]
            public readonly Dictionary<string, List<ItemDefinition>> Custom = new Dictionary<string, List<ItemDefinition>>();
        }
        #endregion

        #region Config Initialization
        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            var bplist = ItemManager.GetBlueprints();
            foreach (var bp in bplist)
            {
                if (bp.userCraftable && !bp.defaultBlueprint)
                {
                    if (!_config.Available.ContainsKey(bp.targetItem.category.ToString("F")))
                    {
                        _config.Available[bp.targetItem.category.ToString("F")] = new List<string>() { bp.targetItem.displayName.english };
                    }
                    else
                    {
                        _config.Available[bp.targetItem.category.ToString("F")].Add(bp.targetItem.displayName.english);
                    }
                }
            }
            _config.CustomPermissions = new Dictionary<string, List<string>>()
            {
                ["bpunlockervip.sniper"] = new List<string>()
                {
                    "HV 5.56 Rifle Ammo",
                    "Bolt Action Rifle",
                    "Large Medkit",
                    "Coffee Can Helmet",
                    "Road Sign Jacket",
                    "Road Sign Kilt"
                },
                ["bpunlockervip.heavy"] = new List<string>()
                {
                    "Heavy Plate Helmet",
                    "Heavy Plate Jacket",
                    "Heavy Plate Pants",
                    "5.56 Rifle Ammo",
                    "Assault Rifle",
                    "Explosive 5.56 Rifle Ammo",
                },
                ["bpunlockervip.builder"] = new List<string>()
                {
                    "Auto Turret",
                    "Concrete Barricade",
                    "Metal Barricade",
                    "Sandbag Barricade",
                    "Bed",
                    "Locker",
                    "Mail Box",
                    "High External Stone Gate",
                    "High External Wooden Gate",
                    "High External Stone Wall",
                    "High External Wooden Wall",
                }
            };
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.NoWorkbench == null)
                _config.NoWorkbench = "bpunlockervip.noworkbench"; //Для совместимости с прошлыми версиями
            permission.RegisterPermission(_config.CommandPermission, this);
            permission.RegisterPermission(_config.All, this);
            permission.RegisterPermission(_config.NoWorkbench, this);
            foreach (var perm in _config.CustomPermissions)
                permission.RegisterPermission(perm.Key, this);
            var itemdefs = ItemManager.GetItemDefinitions();
            foreach (var perm in _config.CustomPermissions)
            {
                foreach (var item in perm.Value)
                {
                    ItemDefinition itemdef = itemdefs.FirstOrDefault(p => p.displayName.english == item || p.shortname == item);
                    if (itemdef == null)
                    {
                        PrintWarning(GetMsg("NoDefFound", null, item, perm.Key));
                        continue;
                    }
                    if (!_config.Custom.ContainsKey(perm.Key))
                    {
                        _config.Custom[perm.Key] = new List<ItemDefinition>() { itemdef };
                    }else
                    {
                        _config.Custom[perm.Key].Add(itemdef);
                    }
                }
            }
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        #endregion

        #region Init
        void Init()
        {
            AddCovalenceCommand(_config.Command, "CmdBp", _config.CommandPermission);
            var bplist = ItemManager.GetBlueprints();
            foreach (var bp in bplist)
            {
                if (bp.userCraftable && !bp.defaultBlueprint)
                {
                    _available.Add(bp.targetItem);
                }
            }
        }
        void Unload()
        {
            if (_updater != null && !_updater.Destroyed)
                _updater.Destroy();
        }
        void OnServerInitialized()
        {
            _updater = timer.Every(1f, UpdatePermissions);
        }
        #endregion

        #region Localization
        private string GetMsg(string langline, object userId = null, params object[] args)
        {
            string msg = lang.GetMessage(langline, this, userId?.ToString());
            return string.Format(msg, args);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["UserUnlocked"] = "Unlocked all blueprints for player \"{0}\"",
                ["UserUnlockedSome"] = "Player \"{0}\" has unlocked blueprints pack \"{1}\"",
                ["UserLocked"] = "Blueprints for player \"{0}\" reseted to default",
                ["Syntax"] = "Wrong syntax! Use /{0} [unlock/lock] [player] [blueprint or custom permissions group or all]",
                ["Syntax1"] = "You forgot to specify what to unlock",
                ["NoPlayer"] = "Player {0} not found on the server!",
                ["NoDefFound"] = "Defenition for item \"{0}\" (permission \"{1}\") not found! Check your config!",
                ["ItemNotFound"] = "Item \"{0}\" not found!",
                ["ItemUnlocked"] = "Blueprint for item \"{0}\" has being unlocked to player \"{1}\"!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["UserUnlocked"] = "Игроку {0} теперь доступны все чертежи",
                ["UserUnlockedSome"] = "Игроку \"{0}\" разблокирован набор чертежей \"{1}\"",
                ["UserLocked"] = "Чертежи игрока \"{0}\" сброшены до стандартных",
                ["Syntax"] = "Неверный синтаксис! Использвуте /{0} [unlock/lock] [игрок] [чертёж или набор чертежей или all]",
                ["Syntax1"] = "Вы забыли указать что разблокировать",
                ["NoPlayer"] = "Игрок \"{0}\" сейчас не находится на сервере!",
                ["NoDefFound"] = "Предмет \"{0}\" (привилегия \"{1}\") не найден! Проверьте файл конфигурации!",
                ["ItemNotFound"] = "Предмет \"{0}\" не найден!",
                ["ItemUnlocked"] = "Чертёж предмета \"{0}\" разблокирован для игрока \"{1}\"!"
            }, this, "ru");
        }
        #endregion

        #region Oxide hooks
        private void OnPlayerInit(BasePlayer player)
        {
            if (!_order.Contains(player.userID))
                _order.Enqueue(player.userID);
        }
    object CanCraft(PlayerBlueprints bps, ItemDefinition itemDef, int skinId)
        {
            var player = bps.GetComponent<BasePlayer>();
            if (player)
            {
                var reply = 0;
                bool hasperm = permission.UserHasPermission(player.UserIDString, _config.NoWorkbench);
                bool hasSkin = skinId == 0 || bps.steamInventory.HasItem(skinId);
                bool unlock = (bps.HasUnlocked(itemDef) && player.currentCraftLevel >= (double)itemDef.Blueprint.workbenchLevelRequired);
                
                return hasSkin && (unlock || hasperm);
            }
            return null;
        }
        #endregion

        #region Functions
        private void UpdatePermissions()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!_order.Contains(player.userID))
                {
                    _order.Enqueue(player.userID);
                }

                player.ClientRPCPlayer(null, player, "craftMode",
                    permission.UserHasPermission(player.UserIDString, _config.NoWorkbench) ? 1 : 0);

                if (_order.Any() && !_inProgress)
                {
                    _inProgress = true;
                    ServerMgr.Instance.StartCoroutine(Unlock(_order.Dequeue()));
                }
            }
        }
        IEnumerator Unlock(ulong userId, List<ItemDefinition> list = null)
        {
            var player = BasePlayer.FindByID(userId);
            if (player == null || !player.IsConnected)
            {
                _inProgress = false;
                yield break;
            }
            if(list == null)
                list = GetBpist(player);
            if (list.All(player.blueprints.IsUnlocked))
            {
                _inProgress = false;
                yield break;
            }
            PersistantPlayer playerInfo = ServerMgr.Instance.persistance.GetPlayerInfo(player.userID);
            foreach (var def in list)
            {
                if (!playerInfo.unlockedItems.Contains(def.itemid))
                {
                    playerInfo.unlockedItems.Add(def.itemid);
                }
                //yield return WaitForFixedUpdate;
            }
            ServerMgr.Instance.persistance.SetPlayerInfo(player.userID, playerInfo);
            player?.SendNetworkUpdateImmediate();
            player?.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);
            _inProgress = false;
        }
        #endregion

        #region Commands
        private void CmdBp(IPlayer player, string cmd, string[] args)
        {
            if (args == null || args.Length < 2)
            {
                player.Message(GetMsg("Syntax", player.Id, _config.Command));
                return;
            }
            BasePlayer target = BasePlayer.Find(args[1]);
            if (target == null)
            {
                player.Message(GetMsg("NoPlayer", player.Id, args[1]));
                return;
            }
            switch (args[0].ToLower())
            {
                case "unlock":
                    if(args.Length < 3)
                    {
                        string msg = GetMsg("Syntax", player.Id, _config.Command) + "\n" + GetMsg("Syntax1", player.Id);
                        player.Message(msg);
                        return;
                    }
                    if(args[2].ToLower() == "all")
                    {
                        //target.blueprints.UnlockAll();
                        ServerMgr.Instance.StartCoroutine(Unlock(target.userID,_available));
                        player.Message(GetMsg("UserUnlocked", player.Id, target.displayName));
                        return;
                    }
                    if (_config.Custom.ContainsKey(args[2].ToLower()))
                    {
                        _config.Custom[args[2].ToLower()].ForEach(bp => target.blueprints.Unlock(bp));
                        player.Message(GetMsg("UserUnlockedSome", player.Id, target.displayName, args[2].ToLower()));
                        return;
                    }
                    var def = _available.FirstOrDefault(p => p.displayName.english == args[2] || p.shortname == args[2]);
                    if(def == null)
                    {
                        player.Message(GetMsg("ItemNotFound", player.Id, args[2]));
                        return;
                    }
                    target.blueprints.Unlock(def);
                    player.Message(GetMsg("ItemUnlocked", player.Id, def.displayName.english, target.displayName));
                    return;
                case "lock":
                    target.blueprints.Reset();
                    target.SendNetworkUpdateImmediate();
                    target.ClientRPCPlayer(null, target, "UnlockedBlueprint", 0);
                    player.Message(GetMsg("UserLocked", player.Id, target.displayName));
                    return;
            }
        }
        #endregion

        #region Helpers
        private List<ItemDefinition> GetBpist(object player)
        {
            string userId;
            if (player is BasePlayer)
                userId = ((BasePlayer) player).UserIDString;
            else
                userId = player.ToString();

            if (permission.UserHasPermission(userId, _config.All))
                return _available;
            return _config.Custom.Where(p => permission.UserHasPermission(userId, p.Key))
                .SelectMany(x => x.Value).ToList();
        }
        #endregion

    }
}
////////////////////////////////////////////////////////////////////
