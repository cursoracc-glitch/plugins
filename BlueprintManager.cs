using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("Blueprint Manager", "Whispers88", "2.1.3")]
    [Description("Allows you to manage and modify blueprints")]

    public class BlueprintManager : RustPlugin
    {
        #region Config
        private Configuration config;
        private static Dictionary<int, BlueprintData> defaultsetup = new Dictionary<int, BlueprintData>();
        public class BlueprintData
        {
            public bool defaultBP;
            public bool canResearch;
            public int scrapRequired;
            public int unlockMinutesAfterWipe;
            public bool autoUnlockMinutesAfterWipe;

            public BlueprintData()
            {
                defaultBP = false;
                canResearch = true;
                scrapRequired = 1;
                unlockMinutesAfterWipe = -1;
                autoUnlockMinutesAfterWipe = false;
            }
        }

        public class Configuration
        {

            [JsonProperty("Simple Mode (disables advance blueprint management options)")]
            public bool SimpleMode = true;

            [JsonProperty("Update players on permission change (automatically updates a players BPs when their permissions change)")]
            public bool updateBPs = true;

            [JsonProperty("Wipe BPs with Map Wipe")]
            public bool wipeOnMap = false;

            [JsonProperty("Blacklist (items from being automatically learnt)")]
            public List<string> Blacklist = new List<string>();

            [JsonProperty("DefaultBPs (Blueprints to be automatically learnt)")]
            public List<string> DefaultBPs = new List<string>();

            [JsonProperty("Assign custom BP unlocks to various perms (permission, BP List", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<string>> BonusBPs = new Dictionary<string, List<string>> { { "customperm1", new List<string>() { "rock" } }, { "customperm2", new List<string>() { "torch" } } };

            [JsonProperty("Advanced Blueprint Management Options")]
            public Dictionary<string, BlueprintData> BlueprintOptions = new Dictionary<string, BlueprintData>();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                Puts($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Config

        #region Init

        private const string permunlockall = "blueprintmanager.all";
        private const string permadmin = "blueprintmanager.admin";

        private DateTime _lastWipe;
        private Coroutine unlockAfterWipeCoroutine;
        private Coroutine updateAllPlayers;
        private List<string> _permissions = new List<string> { permadmin };
        private List<int> _blacklist = new List<int>();
        private List<int> _defaultBlueprints = new List<int>();
        private Dictionary<string, List<int>> _permissionBPs = new Dictionary<string, List<int>>();
        private Dictionary<ItemBlueprint, int> _unlockAfterWipe = new Dictionary<ItemBlueprint, int>();
        private List<string> commands = new List<string> { nameof(CMDBPReset), nameof(CMDUnlock), nameof(CMDUnlockAll), nameof(CMDWipeAll), nameof(CMDBPRemove) };
        private void OnServerInitialized()
        {

            //Unsub hooks
            if (!config.updateBPs)
            {
                Unsubscribe("OnUserGroupAdded");
                Unsubscribe("OnUserPermissionGranted");
            }

            //Register commands
            commands.ForEach(command => AddLocalizedCommand(command));

            //Check blacklist BPs in config
            config.Blacklist.ForEach(blacklistBP =>
            {
                ItemBlueprint bp = ItemManager.FindItemDefinition(blacklistBP)?.Blueprint;
                if (bp != null)
                    _blacklist.Add(bp.targetItem.itemid);
                else
                    Puts(GetLang("CannotFindBPConfig", null, blacklistBP, "Blacklist"));
            });

            //Create Default Permission Sets
            _permissionBPs.Add(permunlockall, new List<int>());

            foreach (ItemBlueprint bp in ItemManager.bpList)
            {
                if (_blacklist.Contains(bp.targetItem.itemid)) continue;
                //Add BPs to workbench permissions
                List<int> bpList;
                if (!_permissionBPs.TryGetValue($"blueprintmanager.WorkbenchLvL{bp.workbenchLevelRequired}", out bpList))
                    _permissionBPs.Add($"blueprintmanager.WorkbenchLvL{bp.workbenchLevelRequired}", new List<int>() { bp.targetItem.itemid });
                else
                    bpList.Add(bp.targetItem.itemid);

                //Add BPs to ItemCategory permissions
                if (!_permissionBPs.TryGetValue($"blueprintmanager.{Enum.GetName(typeof(ItemCategory), bp.targetItem.category)}", out bpList))
                    _permissionBPs.Add($"blueprintmanager.{Enum.GetName(typeof(ItemCategory), bp.targetItem.category)}", new List<int>() { bp.targetItem.itemid });
                else
                    bpList.Add(bp.targetItem.itemid);

                _permissionBPs[permunlockall].Add(bp.targetItem.itemid);
            }

            //Check default BPs in config
            foreach (string defaultBP in config.DefaultBPs)
            {
                ItemBlueprint bp = ItemManager.FindItemDefinition(defaultBP)?.Blueprint;
                if (bp == null)
                    Puts(GetLang("CannotFindBPConfig", null, defaultBP, "DefaultBPs"));
                else if (!_blacklist.Contains(bp.targetItem.itemid))
                    _defaultBlueprints.Add(bp.targetItem.itemid);
            }

            //Check custom perm BPs
            foreach (var key in config.BonusBPs.Keys)
            {
                _permissionBPs.Add($"blueprintmanager.{key}", new List<int>());
            }

            foreach (var bonusBPSet in config.BonusBPs)
            {
                bonusBPSet.Value.ForEach(bonusBP =>
                {
                    ItemBlueprint bp = ItemManager.FindItemDefinition(bonusBP)?.Blueprint;
                    if (bp == null)
                        Puts(GetLang("CannotFindBPConfig", null, bonusBP, $"Custom BPs set {bonusBPSet.Key}"));
                    else if (!_blacklist.Contains(bp.targetItem.itemid))
                        _permissionBPs[$"blueprintmanager.{bonusBPSet.Key}"].Add(bp.targetItem.itemid);
                });
            }

            //Register Perms
            foreach (var perm in _permissionBPs)
                permission.RegisterPermission(perm.Key, this);

            _permissions.ForEach(perm => permission.RegisterPermission(perm, this));

            if (!config.SimpleMode)
            {
                foreach (ItemBlueprint bp in ItemManager.bpList)
                {
                    defaultsetup.Add(bp.targetItem.itemid, new BlueprintData() { defaultBP = bp.defaultBlueprint, scrapRequired = bp.scrapRequired, canResearch = bp.isResearchable });
                    BlueprintData blueprintData;
                    if (!config.BlueprintOptions.TryGetValue(bp.targetItem.shortname, out blueprintData))
                        config.BlueprintOptions.Add(bp.targetItem.shortname, new BlueprintData() { defaultBP = bp.defaultBlueprint, scrapRequired = bp.scrapRequired, canResearch = bp.isResearchable });
                    else
                    {
                        bp.isResearchable = blueprintData.canResearch;
                        bp.scrapRequired = blueprintData.scrapRequired;
                        bp.defaultBlueprint = blueprintData.defaultBP;

                        if (blueprintData.unlockMinutesAfterWipe > 0)
                        {
                            _unlockAfterWipe[bp] = blueprintData.unlockMinutesAfterWipe;
                        }

                        if (blueprintData.defaultBP)
                        {
                            _defaultBlueprints.Add(bp.targetItem.itemid);
                        }
                    }
                }

                _lastWipe = SaveRestore.SaveCreatedTime;

                _unlockAfterWipe = _unlockAfterWipe.OrderBy(pair => pair.Value).ToDictionary(x => x.Key, x => x.Value);

                unlockAfterWipeCoroutine = ServerMgr.Instance.StartCoroutine(UnlockAfterWipe());
            }
            updateAllPlayers = ServerMgr.Instance.StartCoroutine(UpdateAllPlayers());
            SaveConfig();
        }
        private IEnumerator UpdateAllPlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
                yield return CoroutineEx.waitForEndOfFrame;
            }
            updateAllPlayers = null;
        }

        IEnumerator UnlockAfterWipe()
        {
            float timetillUnlock = (_unlockAfterWipe.First().Value * 60) - Time.realtimeSinceStartup;
            if (timetillUnlock > 0)
                yield return new WaitForSeconds(timetillUnlock);
            bool changed = false;
            foreach (var bptounlock in _unlockAfterWipe.ToDictionary(x => x.Key, x => x.Value))
            {
                if (Time.realtimeSinceStartup - 1 > (bptounlock.Value * 60))
                {
                    ItemManager.FindBlueprint(bptounlock.Key.targetItem).isResearchable = true;
                    _unlockAfterWipe.Remove(bptounlock.Key);

                    BlueprintData blueprintData;
                    if (!config.BlueprintOptions.TryGetValue(bptounlock.Key.targetItem.shortname, out blueprintData))
                        continue;

                    if (blueprintData.autoUnlockMinutesAfterWipe && !_defaultBlueprints.Contains(bptounlock.Key.targetItem.itemid))
                    {
                        _defaultBlueprints.Add(bptounlock.Key.targetItem.itemid);
                        changed = true;
                    }
                }
            }

            if (changed && updateAllPlayers == null)
            {
                updateAllPlayers = ServerMgr.Instance.StartCoroutine(UpdateAllPlayers());
            }
            if (!_unlockAfterWipe.IsNullOrEmpty())
                unlockAfterWipeCoroutine = ServerMgr.Instance.StartCoroutine(UnlockAfterWipe());
        }

        private void Unload()
        {
            foreach (ItemBlueprint bp in ItemManager.bpList)
            {
                BlueprintData blueprintData;
                if (!defaultsetup.TryGetValue(bp.targetItem.itemid, out blueprintData)) continue;
                bp.isResearchable = blueprintData.canResearch;
                bp.scrapRequired = blueprintData.scrapRequired;
                bp.defaultBlueprint = blueprintData.defaultBP;
            }

            if (updateAllPlayers != null)
                ServerMgr.Instance.StopCoroutine(updateAllPlayers);

            if (unlockAfterWipeCoroutine != null)
                ServerMgr.Instance.StopCoroutine(unlockAfterWipeCoroutine);
        }

        #endregion Init

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPerms"] = "You don't have permission to use this command.",
                ["CMDBPResetArgs"] = "This command needs one argument in the format /bpreset playerName or playerID",
                ["CMDUnlockAllArgs"] = "This command needs one argument in the format /bplunlockall playerName or playerID",
                ["CMDRemoveArgs"] = "This command needs at least two arguments in the format /bpremove playerName or playerID item.shortname",
                ["PlayerNotFound"] = "Cannot find player by the {0} identifier",
                ["ResetPlayersBps"] = "{0} BPs were reset",
                ["ResetAllBps"] = "All BPs were reset",
                ["UnlockAllPlayersBps"] = "All BPs were unlocked for {0}",
                ["UnlockPlayersBps"] = "{0} was unlocked for {1}",
                ["RemovePlayersBps"] = "{0} was removed for {1}",
                ["CannotFindBPConfig"] = "Cannot find a blueprint for {0} in the {1} config. Use the item shortname or ID",
                ["CannotFindBP"] = "Cannot find a blueprint for {0}. Use the item shortname or ID",
                ["BlueprintLocked"] = "The {0} blueprint is locked for {1}",
                //Commands
                ["CMDUnlockAll"] = "bpunlockall",
                ["CMDUnlock"] = "bpunlock",
                ["CMDBPReset"] = "bpreset",
                ["CMDBPRemove"] = "bpremove",
                ["CMDWipeAll"] = "bpwipeall"
            }, this);
        }

        #endregion Localization

        #region Commands
        private void CMDBPReset(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permadmin))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length == 0)
            {
                Message(iplayer, "CMDBPResetArgs");
                return;
            }

            BasePlayer targetPlayer = BasePlayer.Find(args[0]);
            if (targetPlayer == null || !targetPlayer.IsConnected)
            {
                Message(iplayer, "PlayerNotFound", args[0]);
                return;
            }

            WipeBPs(targetPlayer);

            Message(iplayer, "ResetPlayersBps", targetPlayer.displayName);
        }

        private void CMDUnlockAll(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permadmin))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length == 0)
            {
                Message(iplayer, "CMDUnlockAllArgs");
                return;
            }

            BasePlayer targetPlayer = BasePlayer.Find(args[0]);
            if (targetPlayer == null || !targetPlayer.IsConnected)
            {
                Message(iplayer, "PlayerNotFound", args[0]);
                return;
            }

            UnlockAllBPs(targetPlayer);

            Message(iplayer, "UnlockAllPlayersBps", targetPlayer.displayName);
        }


        private void CMDUnlock(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permadmin))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 2)
            {
                Message(iplayer, "CMDUnlockAllArgs");
                return;
            }

            BasePlayer targetPlayer = BasePlayer.Find(args[0]);
            if (targetPlayer == null || !targetPlayer.IsConnected)
            {
                Message(iplayer, "PlayerNotFound", args[0]);
                return;
            }

            var BPlist = Pool.Get<List<int>>();

            for (int i = 1; i < args.Length; i++)
            {
                ItemBlueprint? bp = ItemManager.FindItemDefinition(args[i])?.Blueprint;
                if (bp == null)
                {
                    Message(iplayer, "CannotFindBP", args[i]);
                    continue;
                }

                if (!BPlist.Contains(bp.targetItem.itemid))
                    BPlist.Add(bp.targetItem.itemid);

                Message(iplayer, "UnlockPlayersBps", bp.name, targetPlayer.displayName);
            }

            if (BPlist.Count == 0)
            {
                Pool.FreeUnmanaged(ref BPlist);
                return;
            }
            UnlockBPs(targetPlayer, BPlist);
        }

        private void CMDBPRemove(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permadmin))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 2)
            {
                Message(iplayer, "CMDRemoveArgs");
                return;
            }

            BasePlayer targetPlayer = BasePlayer.Find(args[0]);
            if (targetPlayer == null || !targetPlayer.IsConnected)
            {
                Message(iplayer, "PlayerNotFound", args[0]);
                return;
            }

            var BPlist = Pool.Get<List<int>>();

            for (int i = 1; i < args.Length; i++)
            {
                ItemBlueprint? bp = ItemManager.FindItemDefinition(args[i])?.Blueprint;
                if (bp == null)
                {
                    Message(iplayer, "CannotFindBP", args[i]);
                    continue;
                }

                if (!BPlist.Contains(bp.targetItem.itemid))
                    BPlist.Add(bp.targetItem.itemid);

                Message(iplayer, "RemovePlayersBps", bp.name, targetPlayer.displayName);
            }

            if (BPlist.Count == 0)
            {
                Pool.FreeUnmanaged(ref BPlist);
                return;
            }
            RemoveBPs(targetPlayer, BPlist);
        }

        private void CMDWipeAll(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permadmin))
            {
                Message(iplayer, "NoPerms");
                return;
            }

            WipeAllBps();

            Message(iplayer, "ResetAllBps");
        }

        #endregion Commands

        #region Methods
        private void WipeAllBps()
        {
            foreach (var player in BasePlayer.allPlayerList)
            {
                WipeBPs(player);
            }
            if (updateAllPlayers == null)
                updateAllPlayers = ServerMgr.Instance.StartCoroutine(UpdateAllPlayers());
        }

        private void WipeBPs(BasePlayer player)
        {
            var persistantPlayerInfo = player.PersistantPlayerInfo;
            persistantPlayerInfo.unlockedItems.Clear();
            player.PersistantPlayerInfo = persistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPC<int>(RpcTarget.Player("UnlockedBlueprint", player), 0);
        }

        private void RemoveBPs(BasePlayer player, List<int> bps)
        {
            var persistantPlayerInfo = player.PersistantPlayerInfo;
            for(int i = persistantPlayerInfo.unlockedItems.Count - 1; i >= 0; i--)
            {
                if (bps.Contains(persistantPlayerInfo.unlockedItems[i]))
                    persistantPlayerInfo.unlockedItems.RemoveAt(i);
            }
            player.PersistantPlayerInfo = persistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPC<int>(RpcTarget.Player("UnlockedBlueprint", player), 0);
            Pool.FreeUnmanaged(ref bps);
        }

        private void UnlockBPs(BasePlayer player, List<int> bps)
        {
            var persistantPlayerInfo = player.PersistantPlayerInfo;
            foreach (var bp in bps)
            {
                if (persistantPlayerInfo.unlockedItems.Contains(bp))
                    continue;
                persistantPlayerInfo.unlockedItems.Add(bp);
            }
            player.PersistantPlayerInfo = persistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPC<int>(RpcTarget.Player("UnlockedBlueprint", player), 0);
            Pool.FreeUnmanaged(ref bps);
        }

        private void UnlockAllBPs(BasePlayer player)
        {
            var persistantPlayerInfo = player.PersistantPlayerInfo;
            foreach (var bp in _permissionBPs[permunlockall])
            {
                if (persistantPlayerInfo.unlockedItems.Contains(bp))
                    continue;
                persistantPlayerInfo.unlockedItems.Add(bp);
            }
            player.PersistantPlayerInfo = persistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPC<int>(RpcTarget.Player("UnlockedBlueprint", player), 0);
        }

        private void UpdatePlayerBPs(BasePlayer player, bool defaultOnly = false)
        {
            List<int> bpsToUnlock = Pool.Get<List<int>>();
            bpsToUnlock.AddRange(_defaultBlueprints);

            if (!defaultOnly)
            {
                foreach (var perm in _permissionBPs)
                {
                    if (!HasPerm(player.UserIDString, perm.Key)) continue;
                    foreach (var bp in perm.Value)
                    {
                        if (!bpsToUnlock.Contains(bp))
                            bpsToUnlock.Add(bp);
                    }
                }
            }

            var PersistantPlayerInfo = player.PersistantPlayerInfo;

            bool update = false;
            foreach (var bp in bpsToUnlock)
            {
                if (player.PersistantPlayerInfo.unlockedItems.Contains(bp)) continue;
                player.PersistantPlayerInfo.unlockedItems.Add(bp);
                update = true;
            }

            if (!update) return; //Nothing to update

            player.PersistantPlayerInfo = PersistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPC<int>(RpcTarget.Player("UnlockedBlueprint", player), 0);
            Pool.FreeUnmanaged(ref bpsToUnlock);
        }

        #endregion Methods

        #region Hooks
        void OnNewSave(string filename)
        {
            if (!config.wipeOnMap)
                return;
            WipeAllBps();
        }

        object CanUnlockTechTreeNode(BasePlayer player, TechTreeData.NodeInstance node, TechTreeData techTree)
        {
            ItemBlueprint itemBlueprint = node.itemDef.Blueprint;
            if (itemBlueprint != null && !itemBlueprint.isResearchable)
            {
                int mins = -1;
                _unlockAfterWipe.TryGetValue(itemBlueprint, out mins);
                Message(player.IPlayer, "BlueprintLocked", itemBlueprint.name, (mins == -1) ? "forever" : $"{TimeSpan.FromSeconds(mins * 60 - Time.realtimeSinceStartup):hh\\:mm}");
                return false;
            }
            return null;
        }
        private void OnUserPermissionGranted(string userId, string perm)
        {
            if (!perm.Contains("blueprintmanager")) return;
            BasePlayer player = BasePlayer.Find(userId);
            if (player == null) return;
            UpdatePlayerBPs(player);
        }

        private void OnUserGroupAdded(string userId, string groupname)
        {
            foreach (var groupperms in permission.GetGroupPermissions(groupname))
            {
                if (!groupperms.Contains("blueprintmanager")) continue;
                BasePlayer player = BasePlayer.Find(userId);
                if (player == null) return;
                UpdatePlayerBPs(player);
                return;
            }
        }

        private void OnPlayerConnected(BasePlayer player) => UpdatePlayerBPs(player);

        #endregion Hooks

        #region Helpers
        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }
        private void Message(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected) player.Message(GetLang(langKey, player.Id, args));
        }

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private void AddLocalizedCommand(string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages)
                {
                    if (!message.Key.Equals(command)) continue;

                    if (string.IsNullOrEmpty(message.Value)) continue;

                    AddCovalenceCommand(message.Value, command);
                }
            }
        }
        #endregion Helpers
    }
}