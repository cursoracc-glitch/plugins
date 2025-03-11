using System;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Rust;
using Network;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("DrawBridges", "Colon Blow", "2.0.12")]
    public class DrawBridges : RustPlugin
    {

        // fixed sign being damaged

        [PluginReference]
        Plugin Clans;

        [PluginReference]
        Plugin Friends;

        BaseEntity newBridge;
        public static DrawBridges instance;

        static Dictionary<ulong, PlayerBridgeData> loadplayer = new Dictionary<ulong, PlayerBridgeData>();

        static List<uint> storedBridges = new List<uint>();
        private DynamicConfigFile data;
        private bool initialized;

        public class PlayerBridgeData
        {
            public BasePlayer player;
            public int bridgecount;
        }

        #region Configuration

        static bool UseSoundsEffects = true;
        bool UseMaxBridgeChecks = true;
        bool BlockBuildingOnBridge = true;
        bool AllowOnFloors = true;
        bool AllowOnFoundations = true;
        static bool UseFriendsChecks = false;
        static bool UseClansChecks = false;
        bool UseStabilityCheck = true;
        bool MasterBuilderFreedom = false;
        static bool UsePressurePlates = false;
        public int maxbridges = 2;
        public int maxvipbridges = 10;
        float userange = 20f;
        float rotaterange = 0.5f;
        float setramprange = 0.5f;
        float lockrange = 0.5f;
        float invertrange = 0.5f;
        float destroyrange = 0.5f;
        float minstability = 50f;
        bool Changed;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        private void LoadConfigVariables()
        {
            CheckCfg("Max Bridges : Normal Authenicated User : ", ref maxbridges);
            CheckCfg("Max Bridges : VIP Authenticated User : ", ref maxvipbridges);
            CheckCfgFloat("Range : Player will activate all bridges within this range : ", ref userange);
            CheckCfgFloat("Range : Player will rotate all bridges within this range : ", ref rotaterange);
            CheckCfgFloat("Range : Player will set ramp angle on all bridges within this range : ", ref setramprange);
            CheckCfgFloat("Range : Player will lock all bridges within this range : ", ref lockrange);
            CheckCfgFloat("Range : Player will invert all bridges within this range : ", ref lockrange);
            CheckCfgFloat("Range : Player will destroy all bridges within this range : ", ref destroyrange);
            CheckCfgFloat("Stability : Minimum Stability needed to place drawbrige on floor : ", ref minstability);
            CheckCfg("Usage - Use Bridge Sound Effects ? ", ref UseSoundsEffects);
            CheckCfg("Usage - Use Max Bridge Checks ? ", ref UseMaxBridgeChecks);
            CheckCfg("Usage - Use Stability Check when building ? ", ref UseStabilityCheck);
            CheckCfg("Usage - Allow Bridge to be built on Floors ? ", ref AllowOnFloors);
            CheckCfg("Usage - Allow Bridge to be built on Foundations ? ", ref AllowOnFoundations);
            CheckCfg("Usage - Use Friends to use Owners Bridges ? ", ref UseFriendsChecks);
            CheckCfg("Usage - Use Clan memebers to use Owners Bridges ? ", ref UseClansChecks);
            CheckCfg("Usage - Allow Master Builders to place bridge on ANY building block ? ", ref MasterBuilderFreedom);
            CheckCfg("Usage - Add Pressure Plate to bridge when building ? ", ref UsePressurePlates);
            CheckCfg("Usage - Block players from building on bridge floor itself ? ", ref BlockBuildingOnBridge);
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        #endregion

        #region Localization

        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["helptext1"] = "type <color=orange>/bridge.build</color> to build a bridge on floor/foundation you are looking at.",
            ["helptext2"] = "type <color=orange>/bridge.use</color>  to operate any of your bridges within range.",
            ["helptext3"] = "type <color=orange>/bridge.rotate</color>  to rotate bridge right 90 degrees.",
            ["helptext4"] = "type <color=orange>/bridge.invert</color>  to flip bridge to work upside down, master builders only",
            ["helptext5"] = "type <color=orange>/bridge.setplate</color>  to add/remove a pressure activation plate to the bridge",
            ["helptext0"] = "type <color=orange>/bridge.setpublicplate</color>  to add/remove a public activatoion plate to the bridge",
            ["helptext6"] = "type <color=orange>/bridge.setramp #</color>  , to set ramp a angle. example, /bridge.setramp 45",
            ["helptext7"] = "type <color=orange>/bridge.lock</color>  to lock/unlock bridge at current angle",
            ["helptext8"] = "type <color=orange>/bridge.count</color>  to show your current number of built bridges",
            ["helptext9"] = "type <color=orange>/bridge.destroy</color>  will destroy the bridge your standing on",
            ["notauthorized"] = "You are not authorized to use that command !!",
            ["notallowed"] = "You are allowed to build on top of a bridge !!",
            ["notabridge"] = "You can only place that on a bridge base block !!",
            ["notowner"] = "You have to be owner of block to use/bulid here !!",
            ["maxbridges"] = "You have reached your Maximum Bridge build limit !!",
            ["notcorrectblock"] = "You need to build a Bridge on a Floor or Foundation !!",
            ["notstable"] = "Need a more stable platform to build bridge on !!",
            ["alreadybridge"] = "That Block already has a Bridge on it !!",
            ["alreadytrigger"] = "That Block already has a Trigger on it !!",
            ["notaangle"] = "Ramp Angle incorrect, please try again !!",
            ["bridgelocked"] = "Bridge is now locked in place !!",
            ["bridgeunlocked"] = "Bridge has been unlocked !!",
            ["notowner"] = "You must be owner of bridge to use this!!!"
        };

        #endregion

        #region Hooks

        void Loaded()
        {
            LoadVariables();
            permission.RegisterPermission("drawbridges.builder", this);
            permission.RegisterPermission("drawbridges.buildervip", this);
            permission.RegisterPermission("drawbridges.masterbuilder", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("bridge_data");
        }

        private void OnServerInitialized()
        {
            instance = this;
            initialized = true;
            LoadData();
            timer.In(3, RestoreBridges);
        }
        private void OnServerSave()
        {
            if (storedBridges.Count > 0) SaveData();
        }

        void Unload()
        {
            DestroyAll<BridgeEntity>();
        }

        private void RestoreBridges()
        {
            if (storedBridges.Count > 0)
            {
                BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseEntity).ToArray();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        if (!obj.IsValid() || obj.IsDestroyed)
                            continue;

                        if (storedBridges.Contains(obj.net.ID))
                        {
                            var userid = obj.OwnerID;
                            var addbridge = obj.gameObject.AddComponent<BridgeEntity>();
                            if (obj.GetComponent<BaseEntity>().skinID == 1) addbridge.SpawnPressurePlate();
                            if (obj.GetComponent<BaseEntity>().skinID == 2) addbridge.SpawnPublicPressurePlate();
                            AddPlayerID(userid);
                        }
                    }
                }
            }
        }

        void SaveData() => data.WriteObject(storedBridges.ToList());
        void LoadData()
        {
            try
            {
                storedBridges = data.ReadObject<List<uint>>();
            }
            catch
            {
                storedBridges = new List<uint>();
            }
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        bool isCorrectBlock(BaseEntity entity, BasePlayer player)
        {
            if (!entity is BuildingBlock) return false;
            if (MasterBuilderFreedom && isAllowed(player, "bridge.masterbuilder")) return true;
            if (AllowOnFloors && entity.name.Contains("floor/floor")) return true;
            if (AllowOnFoundations && entity.name.Contains("foundation/foundation")) return true;
            return false;
        }

        bool BridgeLimitReached(BasePlayer player)
        {
            if (UseMaxBridgeChecks)
            {
                if (isAllowed(player, "bridge.masterbuilder")) return false;
                if (loadplayer.ContainsKey(player.userID))
                {
                    var currentcount = loadplayer[player.userID].bridgecount;
                    var maxallowed = maxbridges;
                    if (isAllowed(player, "bridge.buildervip")) maxallowed = maxvipbridges;
                    if (currentcount >= maxallowed) return true;
                }
            }
            return false;
        }

        private bool IsFriend(ulong playerid, ulong friendid)
        {
            var Friends = plugins.Find("Friends");
            bool areFriends = Convert.ToBoolean(Friends?.Call("HasFriend", playerid, friendid));
            return areFriends;
        }

        private bool IsClanmate(ulong playerid, ulong friendid)
        {
            var Clans = plugins.Find("Clans");
            var ownersClan = (string)Clans.Call("GetClanOf", playerid);
            var usersClan = (string)Clans.Call("GetClanOf", friendid);
            if (ownersClan == null || usersClan == null || !ownersClan.Equals(usersClan)) return false;
            return true;
        }

        public void BuildBridge(BasePlayer player)
        {
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity)) newBridge = hit.GetTransform().gameObject.ToBaseEntity();
            if (newBridge == null) return;
            bool isbuildingblock = newBridge?.GetComponent<BuildingBlock>();
            if (!isbuildingblock) return;
            if (newBridge.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
            if (isCorrectBlock(newBridge, player))
            {
                var hasbridge = newBridge.GetComponentInParent<BridgeEntity>();
                if (hasbridge) { SendReply(player, msg("alreadybridge", player.UserIDString)); return; }
                if (UseStabilityCheck)
                {
                    var stabent = newBridge.GetComponent<StabilityEntity>();
                    var stabvalue = stabent.SupportValue() * 100;
                    if (stabvalue < minstability) { SendReply(player, msg("notstable", player.UserIDString)); return; }
                }
                var addbridge = newBridge.gameObject.AddComponent<BridgeEntity>();
                newBridge.OwnerID = player.userID;
                AddPlayerID(player.userID);
                storedBridges.Add(newBridge.net.ID);
                SaveData();
                return;
            }
            SendReply(player, msg("notcorrectblock", player.UserIDString));
        }


        void DeployPressurePlate(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, setramprange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = p.GetComponentInParent<BaseEntity>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    foundent.TogglePressurePlate();
                }
            }
        }

        void DeployPublicPressurePlate(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, setramprange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = p.GetComponentInParent<BaseEntity>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    foundent.TogglePublicPressurePlate();
                }
            }
        }

        object CanBuild(Planner plan, Construction prefab, object obj)
        {
            if (!BlockBuildingOnBridge) return null;
            if (plan == null || prefab == null || obj == null) return null;
            if (obj is Construction.Target)
            {
                var target = (Construction.Target)obj;
                var targetent = target.entity as BaseEntity;
                var isbridge = targetent?.GetComponentInParent<BridgeEntity>();
                if (isbridge)
                {
                    if (targetent == isbridge.floor2) return false;
                    if (targetent == isbridge.floor1) return false;
                }
                return null;
            }
            else return null;
        }

        private object OnEntityGroundMissing(BaseEntity entity)
        {
            var bridge = entity.GetComponentInParent<BridgeEntity>();
            if (bridge != null) return false;
            return null;
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            var bridge = networkable.GetComponentInParent<BridgeEntity>();
            if (bridge != null && storedBridges.Contains(networkable.net.ID))
            {
                storedBridges.Remove(networkable.net.ID);
                SaveData();
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null) return;
            var isbridge = entity.GetComponentInParent<BridgeEntity>() ?? null;
            if (isbridge && entity.name.Contains("assets/prefabs/deployable/signs/sign.small.wood.prefab"))
            {
                hitInfo.damageTypes.ScaleAll(0);
            }
            return;
        }

        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (player == null) return null;
            if (StandingOnBridge(player)) return false;
            return null;
        }

        bool StandingOnBridge(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, 3f, bridgelist);
            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent) return true;
            }
            return false;
        }

        void UseLocalBridge(BasePlayer player, bool setangle, float rotationx)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, userange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = p.GetComponentInParent<BaseEntity>();
                    if (UseFriendsChecks && IsFriend(entity.OwnerID, player.userID)) foundent.Activate(setangle, rotationx);
                    if (UseClansChecks && IsClanmate(entity.OwnerID, player.userID)) foundent.Activate(setangle, rotationx);
                    if (player.userID == entity.OwnerID) foundent.Activate(setangle, rotationx);
                }
            }
        }

        void SetRampLocalBridge(BasePlayer player, bool setangle, float rotationx)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, setramprange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = p.GetComponentInParent<BaseEntity>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    foundent.Activate(setangle, rotationx);
                }
            }
        }

        void RotateLocalBridge(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, rotaterange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = foundent.GetComponentInParent<BuildingBlock>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    Vector3 newentrot = new Vector3(entity.transform.eulerAngles.x, entity.transform.eulerAngles.y + 90, entity.transform.eulerAngles.z);
                    entity.transform.eulerAngles = newentrot;
                    entity.GetComponent<BaseEntity>().transform.hasChanged = true;
                    entity.GetComponent<BaseEntity>().SendNetworkUpdateImmediate();
                    entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    entity.UpdateSkin();
                    entity.ClientRPC(null, "RefreshSkin");
                    foundent.RefreshFloor();
                }
            }
        }

        void InvertLocalBridge(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, invertrange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = foundent.GetComponentInParent<BuildingBlock>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    Vector3 newentrot = new Vector3(entity.transform.eulerAngles.x + 180, entity.transform.eulerAngles.y, entity.transform.eulerAngles.z);
                    entity.transform.eulerAngles = newentrot;
                    entity.GetComponent<BaseEntity>().transform.hasChanged = true;
                    entity.GetComponent<BaseEntity>().SendNetworkUpdateImmediate();
                    entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    entity.UpdateSkin();
                    entity.ClientRPC(null, "RefreshSkin");
                    foundent.RefreshFloor();
                }
            }
        }


        void LockLocalBridge(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, lockrange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var bridgelocked = foundent.islocked;
                    var entity = p.GetComponentInParent<BaseEntity>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    if (bridgelocked) { foundent.islocked = false; SendReply(player, msg("bridgeunlocked", player.UserIDString)); return; }
                    else { foundent.islocked = true; SendReply(player, msg("bridgelocked", player.UserIDString)); return; }
                }
            }
        }

        void DestroyLocalBridge(BasePlayer player)
        {
            List<BaseEntity> bridgelist = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, destroyrange, bridgelist);

            foreach (BaseEntity p in bridgelist)
            {
                var foundent = p.GetComponentInParent<BridgeEntity>();
                if (foundent)
                {
                    var entity = p.GetComponentInParent<BaseEntity>();
                    if (entity.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                    entity.Invoke("KillMessage", 0.1f);
                }
            }
        }

        void AddPlayerID(ulong ownerid)
        {
            if (!loadplayer.ContainsKey(ownerid))
            {
                loadplayer.Add(ownerid, new PlayerBridgeData
                {
                    bridgecount = 1
                });
                return;
            }
            loadplayer[ownerid].bridgecount = loadplayer[ownerid].bridgecount + 1;
        }

        void RemovePlayerID(ulong ownerid)
        {
            if (loadplayer.ContainsKey(ownerid)) loadplayer[ownerid].bridgecount = loadplayer[ownerid].bridgecount - 1;
            return;
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        #endregion

        #region Commands

        [ChatCommand("bridge.help")]
        void cmdBridgeHelp(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                SendReply(player, msg("helptext1", player.UserIDString));
                SendReply(player, msg("helptext2", player.UserIDString));
                SendReply(player, msg("helptext3", player.UserIDString));
                SendReply(player, msg("helptext4", player.UserIDString));
                SendReply(player, msg("helptext5", player.UserIDString));
                SendReply(player, msg("helptext0", player.UserIDString));
                SendReply(player, msg("helptext6", player.UserIDString));
                SendReply(player, msg("helptext7", player.UserIDString));
                SendReply(player, msg("helptext8", player.UserIDString));
                SendReply(player, msg("helptext9", player.UserIDString));
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.build")]
        void cmdBridgeBuild(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                if (BridgeLimitReached(player)) { SendReply(player, msg("maxbridges", player.UserIDString)); return; }
                BuildBridge(player);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.use")]
        void cmdBridgeUse(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                UseLocalBridge(player, false, 0f);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }


        [ChatCommand("bridge.setplate")]
        void cmdSetPressurePlate(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                DeployPressurePlate(player);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.setpublicplate")]
        void cmdSetPublicPressurePlate(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                DeployPublicPressurePlate(player);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.setramp")]
        void cmdBridgeSetRamp(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (!isAllowed(player, "bridge.masterbuilder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            var str0 = "0";
            if (args != null && args.Length > 0)
            {
                float rotationx;
                if (float.TryParse(args[0].ToLower(), out rotationx))
                {
                    SetRampLocalBridge(player, true, rotationx);
                    return;
                }
                SendReply(player, msg("notaangle", player.UserIDString));
            }
        }

        [ChatCommand("bridge.lock")]
        void cmdBridgeLock(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                LockLocalBridge(player);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.invert")]
        void cmdBridgeInvert(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return;
            if (!isAllowed(player, "bridge.masterbuilder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            InvertLocalBridge(player);
        }

        [ChatCommand("bridge.rotate")]
        void cmdBridgeRotate(BasePlayer player, string command, string[] args)
        {
            if (!player.CanBuild()) return; ;
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                RotateLocalBridge(player);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.destroy")]
        void cmdBridgeDestroy(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "bridge.builder") || isAllowed(player, "bridge.buildervip") || isAllowed(player, "bridge.masterbuilder"))
            {
                DestroyLocalBridge(player);
                return;
            }
            SendReply(player, msg("notauthorized", player.UserIDString));
        }

        [ChatCommand("bridge.count")]
        void cmdChatBridgeCount(BasePlayer player, string command, string[] args)
        {
            if (!loadplayer.ContainsKey(player.userID))
            {
                SendReply(player, "You have no Bridges");
                return;
            }
            SendReply(player, "Current Bridges : " + (loadplayer[player.userID].bridgecount));
        }

        #endregion

        #region Bridge Entity

        class BridgeEntity : BaseEntity
        {
            BaseEntity entity;
            public BaseEntity floor1;
            public BaseEntity floor2;
            public BaseEntity hinge;
            BaseEntity plate;
            BaseEntity pplate;
            BuildingBlock floor1block;
            BuildingBlock floor2block;
            BuildingBlock blockentity;
            Vector3 entitypos;
            Quaternion entityrot;
            bool isup;
            public bool isfloor;
            public bool islocked;
            ulong ownerid;
            float secsToTake;
            float secsTaken;
            Vector3 startRot;
            Vector3 endRot;
            public bool isRotating;
            bool triggered;
            int counter;
            SphereCollider sphereCollider;

            string prefabfloor = "assets/prefabs/building core/floor/floor.prefab";
            string prefabhinge = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
            string prefabwall = "assets/prefabs/building core/wall/wall.prefab";

            void Awake()
            {
                entity = GetComponentInParent<BaseEntity>();
                ownerid = entity.OwnerID;
                blockentity = entity.GetComponent<BuildingBlock>();
                entitypos = entity.transform.position;
                entityrot = Quaternion.identity;
                triggered = false;

                isup = true;
                isfloor = true;
                islocked = false;
                isRotating = false;
                counter = 0;
                secsToTake = 0.25f;
                SpawnPos();
                if (UsePressurePlates) SpawnPressurePlate();
                startRot = new Vector3(0f, hinge.transform.localEulerAngles.y, hinge.transform.localEulerAngles.z);
                endRot = new Vector3(90f, hinge.transform.localEulerAngles.y, hinge.transform.localEulerAngles.z);
                isRotating = false;
                RefreshFloor();
            }

            public void Activate(bool setangle, float rotationx)
            {
                if (islocked) return;
                if (!setangle && isup) { ActivateDown(90f); return; }
                if (!setangle && !isup) { ActivateUp(0f); return; }
                if (setangle) { ActivateDown(rotationx); return; }
                else ActivateUp(0f);
            }

            public void ActivateDown(float rotationx)
            {
                if (isRotating) return;
                startRot = hinge.transform.localEulerAngles;
                endRot = new Vector3(rotationx, startRot.y, startRot.z);
                isRotating = true;
                isup = false;
            }

            public void ActivateUp(float rotationx)
            {
                if (isRotating) return;
                startRot = hinge.transform.localEulerAngles;
                endRot = new Vector3(rotationx, startRot.y, startRot.z);
                isRotating = true;
                isup = true;
            }

            void SpawnPos()
            {
                BuildingBlock entityblock = entity.GetComponent<BuildingBlock>();
                var entitygrade = entityblock.grade;

                hinge = GameManager.server.CreateEntity(prefabhinge, entitypos, entityrot, true);
                hinge.enableSaving = false;
                hinge.transform.localEulerAngles = new Vector3(0, 0, 0);
                hinge.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                hinge?.Spawn();
                hinge.SetParent(entity);
                hinge.SetFlag(BaseEntity.Flags.Busy, true, true);

                if (isfloor)
                {
                    floor1 = GameManager.server.CreateEntity(prefabfloor, entitypos, entityrot, true);
                    floor1.enableSaving = false;
                    floor1.transform.localEulerAngles = new Vector3(270, 0, 0);
                    floor1.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                }
                else if (!isfloor)
                {
                    floor1 = GameManager.server.CreateEntity(prefabwall, entitypos, entityrot, true);
                    floor1.enableSaving = false;
                    floor1.transform.localEulerAngles = new Vector3(270, 270, 0);
                    floor1.transform.localPosition = new Vector3(-1.5f, 1.5f, 0f);
                }
                floor1.GetComponent<StabilityEntity>().grounded = true;
                floor1block = floor1.GetComponent<BuildingBlock>();
                floor1block.GetComponent<BaseEntity>().OwnerID = ownerid;
                floor1block?.Spawn();
                floor1block.SetParent(hinge);
                floor1block.SetGrade((BuildingGrade.Enum)entitygrade);
                SpawnRefresh(floor1block);

                if (isfloor)
                {
                    floor2 = GameManager.server.CreateEntity(prefabfloor, entitypos, entityrot, true);
                    floor2.enableSaving = false;
                    floor2.transform.localEulerAngles = new Vector3(0, 0, 0);
                    floor2.transform.localPosition = new Vector3(0f, 0f, 3f);
                }
                else if (!isfloor)
                {
                    floor2 = GameManager.server.CreateEntity(prefabwall, entitypos, entityrot, true);
                    floor2.enableSaving = false;
                    floor2.transform.localEulerAngles = new Vector3(0, 0, 0);
                    floor2.transform.localPosition = new Vector3(0f, 0f, 3f);
                }

                floor2.GetComponent<StabilityEntity>().grounded = true;
                floor2block = floor2.GetComponent<BuildingBlock>();
                floor2block.GetComponent<BaseEntity>().OwnerID = ownerid;
                floor2block?.Spawn();
                floor2block.SetParent(floor1);
                floor2block.SetGrade((BuildingGrade.Enum)entitygrade);
                SpawnRefresh(floor2block);


                isup = true;
            }

            void SpawnRefresh(BaseNetworkable entity1)
            {
                var hasstab = entity1.GetComponent<StabilityEntity>();
                if (hasstab)
                {
                    hasstab.grounded = true;
                }
                var hasblock = entity1.GetComponent<BuildingBlock>();
                if (hasblock)
                {
                    hasblock.SetHealthToMax();
                    hasblock.UpdateSkin();
                    hasblock.ClientRPC(null, "RefreshSkin");
                }
                entity1.SendNetworkUpdateImmediate();
            }

            void ClntDstry(BaseNetworkable entity, bool recursive = true)
            {
                if (Net.sv.write.Start())
                {
                    Net.sv.write.PacketID(Message.Type.EntityDestroy);
                    Net.sv.write.UInt32(entity.net.ID);
                    Net.sv.write.UInt8(0);
                    Net.sv.write.Send(new SendInfo(entity.net.group.subscribers));
                }
                if (recursive && entity.children != null) for (int i = 0; i < entity.children.Count; i++) ClntDstry(entity.children[i], false);
            }

            void EnttSnpsht(BaseNetworkable entity, bool recursive = true)
            {
                entity.InvalidateNetworkCache(); List<Connection> subscribers = entity.net.group == null ? Net.sv.connections : entity.net.group.subscribers; if (subscribers != null && subscribers.Count > 0) { for (int i = 0; i < subscribers.Count; i++) { Connection connection = subscribers[i]; BasePlayer basePlayer = connection.player as BasePlayer; if (!(basePlayer == null)) { if (Net.sv.write.Start()) { connection.validate.entityUpdates = connection.validate.entityUpdates + 1u; BaseNetworkable.SaveInfo saveInfo = new BaseNetworkable.SaveInfo { forConnection = connection, forDisk = false }; Net.sv.write.PacketID(Message.Type.Entities); Net.sv.write.UInt32(connection.validate.entityUpdates); entity.ToStreamForNetwork(Net.sv.write, saveInfo); Net.sv.write.Send(new SendInfo(connection)); } } } }
                if (recursive && entity.children != null) for (int i = 0; i < entity.children.Count; i++) EnttSnpsht(entity.children[i], false);
            }

            public void RefreshFloor()
            {
                BuildingBlock entityblock = entity.GetComponent<BuildingBlock>();
                var entitygrade = entityblock.grade;

                if (hinge != null) hinge.transform.hasChanged = true;
                if (hinge != null) ClntDstry(hinge, false); EnttSnpsht(hinge, false);
                if (hinge != null) hinge.SendNetworkUpdateImmediate();
                if (hinge != null) hinge.UpdateNetworkGroup();
                if (hinge != null) hinge.GetComponent<DestroyOnGroundMissing>().enabled = false;
                if (hinge != null) hinge.GetComponent<GroundWatch>().enabled = false;

                if (floor1block != null) floor1.transform.hasChanged = true;
                if (floor1block != null) ClntDstry(floor1block, false); EnttSnpsht(floor1block, false);
                if (floor1block != null) floor1block.SetGrade((BuildingGrade.Enum)entitygrade);
                if (floor1block != null) floor1.SendNetworkUpdateImmediate();
                if (floor1block != null) floor1.UpdateNetworkGroup();
                if (floor1block != null) floor1.GetComponent<BuildingBlock>().UpdateSkin();
                if (floor1block != null) floor1.GetComponent<BuildingBlock>().ClientRPC(null, "RefreshSkin");

                if (floor2block != null) floor2.transform.hasChanged = true;
                if (floor2block != null) ClntDstry(floor2block, false); EnttSnpsht(floor2block, false);
                if (floor2block != null) floor2block.SetGrade((BuildingGrade.Enum)entitygrade);
                if (floor2block != null) floor2.SendNetworkUpdateImmediate();
                if (floor2block != null) floor2.UpdateNetworkGroup();
                if (floor2block != null) floor2block.UpdateSkin();
                if (floor2block != null) floor2block.GetComponent<BuildingBlock>().ClientRPC(null, "RefreshSkin");
            }

            void ResetAnimationSound()
            {
                if (counter == 1)
                {
                    Effect.server.Run("assets/prefabs/deployable/recycler/effects/start.prefab", floor1block.transform.position);
                }
                counter = counter + 1;
                if (counter == 4) counter = 0;
            }

            public void SpawnPressurePlate()
            {
                string prefabpressureplate = "assets/prefabs/deployable/landmine/landmine.prefab";
                plate = GameManager.server.CreateEntity(prefabpressureplate, entitypos, entityrot, false);
                plate.enableSaving = false;
                plate.SetFlag(BaseEntity.Flags.Reserved5, false, false);
                plate.transform.localEulerAngles = new Vector3(0, 0, 0);
                plate.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                plate?.Spawn();
                plate.SetParent(entity);

                entity.skinID = 1;

                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.2f;
            }

            public void SpawnPublicPressurePlate()
            {
                string prefabpressureplate = "assets/prefabs/deployable/landmine/landmine.prefab";
                pplate = GameManager.server.CreateEntity(prefabpressureplate, entitypos, entityrot, false);
                pplate.enableSaving = false;
                pplate.SetFlag(BaseEntity.Flags.Reserved5, false, false);
                pplate.transform.localEulerAngles = new Vector3(0, 0, 0);
                pplate.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                pplate?.Spawn();
                pplate.SetParent(entity);

                entity.skinID = 2;

                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.2f;
            }

            public void TogglePressurePlate()
            {
                if (plate != null) { plate.Invoke("KillMessage", 0.1f); GameObject.Destroy(sphereCollider); return; }
                if (pplate != null) { pplate.Invoke("KillMessage", 0.1f); GameObject.Destroy(sphereCollider); }
                SpawnPressurePlate();
            }

            public void TogglePublicPressurePlate()
            {
                if (pplate != null) { pplate.Invoke("KillMessage", 0.1f); GameObject.Destroy(sphereCollider); return; }
                if (plate != null) { plate.Invoke("KillMessage", 0.1f); GameObject.Destroy(sphereCollider); }
                SpawnPublicPressurePlate();
            }

            private void OnTriggerEnter(Collider col)
            {
                var target = col.GetComponentInParent<BasePlayer>();
                if (pplate != null) Activate(false, 0f);
                if (!triggered && target != null)
                {
                    if (UseFriendsChecks && instance.IsFriend(ownerid, target.userID)) { Activate(false, 0f); return; }
                    if (UseClansChecks && instance.IsClanmate(ownerid, target.userID)) { Activate(false, 0f); return; }
                    if (target.userID == ownerid) Activate(false, 0f);
                }
            }

            void FixedUpdate()
            {
                if (hinge == null || floor1 == null) { entity.Invoke("KillMessage", 0.1f); return; }
                if (!isRotating) return;
                secsTaken = secsTaken + UnityEngine.Time.deltaTime;
                if (UseSoundsEffects) ResetAnimationSound();
                float single = Mathf.InverseLerp(0f, 5f, secsTaken);
                hinge.transform.localEulerAngles = Vector3.Lerp(startRot, endRot, single);
                if (hinge != null && hinge.gameObject.active == true) hinge.gameObject.SetActive(false);
                if (floor1block != null && floor1block.gameObject.active == true) floor1block.gameObject.SetActive(false);
                if (floor2block != null && floor2block.gameObject.active == true) floor2block.gameObject.SetActive(false);
                if (single >= 1)
                {
                    hinge.transform.localEulerAngles = endRot;
                    secsTaken = 0;
                    isRotating = false;
                    if (hinge != null) hinge.gameObject.SetActive(true);
                    if (floor1block != null) floor1block.gameObject.SetActive(true);
                    if (floor2block != null) floor2block.gameObject.SetActive(true);
                }
                RefreshFloor();
            }

            public void OnDestroy()
            {
                if (loadplayer.ContainsKey(ownerid)) loadplayer[ownerid].bridgecount = loadplayer[ownerid].bridgecount - 1;
                if (plate != null) { plate.Invoke("KillMessage", 0.1f); }
                if (pplate != null) { pplate.Invoke("KillMessage", 0.1f); }
                if (sphereCollider != null) { GameObject.Destroy(sphereCollider); }
                if (floor2block != null) { floor2block.Invoke("KillMessage", 0.1f); }
                if (floor1block != null) { floor1block.Invoke("KillMessage", 0.1f); }
                if (hinge != null) { hinge.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

    }
}