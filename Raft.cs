using System;
using Oxide.Core;
using Oxide.Core.Configuration;
using Network;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using Facepunch;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Raft", "Colon Blow", "1.0.24")]
    class Raft : RustPlugin
    {
        #region Hooks

        BaseEntity newRaft;
        static Dictionary<ulong, string> hasRaft = new Dictionary<ulong, string>();

        static List<uint> storedRafts = new List<uint>();
        private DynamicConfigFile data;
        private bool initialized;

        void Loaded()
        {
            LoadVariables();
            permission.RegisterPermission("raft.builder", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("raft_data");
        }

        private void OnServerInitialized()
        {
            initialized = true;
            LoadData();
            timer.In(3, RestoreRafts);
        }
        private void OnServerSave()
        {
            SaveData();
        }

        private void RestoreRafts()
        {
            if (storedRafts.Count > 0)
            {
                BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseEntity).ToArray();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        if (!obj.IsValid() || obj.IsDestroyed)
                            continue;

                        if (storedRafts.Contains(obj.net.ID))
                        {
                            bool hassail = false;
                            bool hasfire = false;
                            bool hasnet = false;
                            bool hasroof = false;
                            bool haswalls = false;
                            bool haschairs = false;
                            string codestr = "0";
                            string guestcodestr = "0";
                            foreach (Transform child in obj.GetComponent<Transform>())
                            {
                                if (child == null) continue;
                                if (child.name.Contains("wall.frame.garagedoor")) hassail = true;
                                if (child.name.Contains("campfire/campfire")) hasfire = true;
                                if (child.name.Contains("wall.frame.netting")) hasnet = true;
                                if (child.name.Contains("chair/chair") && child != obj) haschairs = true;
                                if (child.name.Contains("sign.post.single") && (child.GetComponent<BaseEntity>().skinID != 1)) hasroof = true;
                                if (child.name.Contains("sign.large.wood") && (child.GetComponent<BaseEntity>().skinID == 1)) haswalls = true;
                                if (child.name.Contains("keypad/lock.code"))
                                {
                                    CodeLock codelock = child.GetComponent<CodeLock>() ?? null;
                                    if (codelock != null) codestr = codelock.code;
                                    if (codelock != null) guestcodestr = codelock.guestCode;
                                }
                            }

                            var spawnpos = obj.transform.position;
                            var spawnrot = obj.transform.rotation;
                            var userid = obj.OwnerID;
                            storedRafts.Remove(obj.net.ID);
                            SaveData();
                            obj.Invoke("KillMessage", 0.1f);
                            timer.Once(2f, () => RespawnRaft(spawnpos, spawnrot, userid, hassail, hasfire, hasnet, hasroof, haswalls, haschairs, codestr, guestcodestr));
                        }
                    }
                }
            }
        }

        void SaveData() => data.WriteObject(storedRafts.ToList());
        void LoadData()
        {
            try
            {
                storedRafts = data.ReadObject<List<uint>>();
            }
            catch
            {
                storedRafts = new List<uint>();
            }
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        public void AddPlayerRaft(ulong id)
        {
            if (!OnlyOneActiveRaft) return;
            if (hasRaft.ContainsKey(id)) return;
            hasRaft.Add(id, "");
        }

        public void RemovePlayerRaft(ulong id)
        {
            if (!OnlyOneActiveRaft) return;
            if (!hasRaft.ContainsKey(id)) return;
            hasRaft.Remove(id);
        }

        public void BuildRaft(BasePlayer player)
        {
            string prefabstr = "assets/prefabs/deployable/chair/chair.deployed.prefab";
            var waterheight = TerrainMeta.WaterMap.GetHeight(player.transform.position);
            var spawnpos = new Vector3(player.transform.position.x, waterheight, player.transform.position.z);
            newRaft = GameManager.server.CreateEntity(prefabstr, spawnpos + new Vector3(0f, 0.4f, 0f), new Quaternion(), true);
            var mount = newRaft.GetComponent<BaseMountable>();
            mount.isMobile = true;
            newRaft.enableSaving = true;
            newRaft.OwnerID = player.userID;
            newRaft?.Spawn();
            var addraft = newRaft.gameObject.AddComponent<RaftEntity>();
            AddPlayerRaft(player.userID);
            storedRafts.Add(newRaft.net.ID);
            mount.MountPlayer(player);
            SaveData();
        }

        public void RespawnRaft(Vector3 spawnpos, Quaternion spawnrot, ulong userid, bool hassail, bool hasfire, bool hasnet, bool hasroof, bool haswalls, bool haschairs, string codestr, string guestcodestr)
        {
            string prefabstr = "assets/prefabs/deployable/chair/chair.deployed.prefab";
            newRaft = GameManager.server.CreateEntity(prefabstr, spawnpos, spawnrot, true);
            var mount = newRaft.GetComponent<BaseMountable>() ?? null;
            if (mount != null) mount.isMobile = true;
            newRaft.enableSaving = true;
            newRaft.OwnerID = userid;
            newRaft?.Spawn();
            var addraft = newRaft.gameObject.AddComponent<RaftEntity>();
            if (hassail) addraft.SpawnSail();
            if (hasfire) addraft.SpawnCampfire();
            if (hasnet) addraft.SpawnNet();
            if (hasroof) addraft.SpawnRoof();
            if (haswalls) addraft.SpawnSideWalls();
            if (haschairs) addraft.SpawnPassengerChairs();
            if (codestr != "0")
            {
                var codelock = addraft.boatlock.GetComponent<CodeLock>() ?? null;
                if (codelock != null)
                {
                    codelock.whitelistPlayers.Add(userid);
                    codelock.code = codestr;
                    codelock.guestCode = guestcodestr;
                    codelock.SetFlag(BaseEntity.Flags.Locked, true, false);
                }
            }
            AddPlayerRaft(userid);
            storedRafts.Add(newRaft.net.ID);
            SaveData();
        }

        void ScrapRaft(BasePlayer player)
        {
            Item woodrefund = ItemManager.CreateByItemID(-151838493, RefundForScrap);
            player.inventory.GiveItem(woodrefund, null);
            player.Command("note.inv", -151838493, RefundForScrap);
        }

        bool CheckUpgradeMats(BasePlayer player, int itemID, int amount, string str)
        {
            int HasReq = player.inventory.GetAmount(itemID);
            if (HasReq >= amount)
            {
                player.inventory.Take(null, itemID, amount);
                player.Command("note.inv", itemID, -amount);
                return true;
            }
            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemID);

            SendReply(player, "You need " + amount + " " + itemDefinition.shortname + " to build " + str);
            return false;
        }

        public bool IsStandingInWater(BasePlayer player)
        {
            var position = player.transform.position;
            var waterdepth = (TerrainMeta.WaterMap.GetHeight(position) - TerrainMeta.HeightMap.GetHeight(position));
            if (waterdepth >= 0.6f) return true;
            return false;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (player.GetMounted() != activeraft.entity) return;
            if (activeraft.boatlock.IsLocked() == true) return;
            if (input != null)
            {
                if (input.WasJustPressed(BUTTON.FORWARD)) { activeraft.setsail = false; activeraft.moveforward = true; }
                if (input.WasJustReleased(BUTTON.FORWARD)) { activeraft.moveforward = false; }
                if (input.WasJustPressed(BUTTON.BACKWARD)) { activeraft.setsail = false; activeraft.movebackward = true; }
                if (input.WasJustReleased(BUTTON.BACKWARD)) { activeraft.movebackward = false; }
                if (input.WasJustPressed(BUTTON.RIGHT)) { activeraft.rotright = true; }
                if (input.WasJustReleased(BUTTON.RIGHT)) { activeraft.rotright = false; }
                if (input.WasJustPressed(BUTTON.LEFT)) { activeraft.rotleft = true; }
                if (input.WasJustReleased(BUTTON.LEFT)) { activeraft.rotleft = false; }
                if (input.WasJustPressed(BUTTON.JUMP))
                {
                    activeraft.ismoving = false;
                    activeraft.moveforward = false;
                    activeraft.movebackward = false;
                    activeraft.rotright = false;
                    activeraft.rotleft = false;
                    activeraft.setsail = false;
                }
                return;
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (storedRafts.Contains(entity.net.ID))
            {
                storedRafts.Remove(entity.net.ID);
                SaveData();
            }
        }

        object OnEntityGroundMissing(BaseEntity entity)
        {
            var raft = entity.GetComponentInParent<RaftEntity>() ?? null;
            if (raft != null) return false;
            return null;
        }

        void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            if (mountable == null || player == null) return;
            var raftentity = mountable.GetComponentInParent<RaftEntity>() ?? null;
            if (raftentity == null) return;
            if (mountable.GetComponent<BaseEntity>() != raftentity.entity) return;
            if (raftentity != null && raftentity.player == null)
            {
                SendReply(player, msg("captain", player.UserIDString));
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null) return;
            var isboat = entity.GetComponentInParent<RaftEntity>() ?? null;
            if (isboat != null) hitInfo.damageTypes.ScaleAll(0);
            return;
        }

        object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            if (entity == null || player == null) return null;
            var raft = entity.GetComponentInParent<RaftEntity>() ?? null;
            if (raft != null) return false;
            return null;
        }

        object CanPickupLock(BasePlayer player, BaseLock baseLock)
        {
            if (baseLock == null || player == null) return null;
            if (baseLock.GetComponentInParent<RaftEntity>()) return false;
            return null;
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.isMounted)
                {
                    var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
                    if (activeraft != null) player.DismountObject();
                }
            }
        }

        void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        #endregion

        #region Commands

        [ChatCommand("raft")]
        void cmdChatRaftHelp(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            string raftstr = "/raft.build - need " + MaterialsForRaft + " Wood to Build a raft.";
            string roofstr = "/raft.addroof - need " + MaterialsForRoof + " Wood to add a Roof.";
            string wallsstr = "/raft.addwalls - need " + MaterialsForWalls + " Wood to add Walls.";
            string sailstr = "/raft.addsail - need " + MaterialsForSail + " Wood to add a Sail.";
            string netstr = "/raft.addnet - need " + MaterialsForNet + " Rope to add Netting.";
            string firestr = "/raft.addfire - need " + MaterialsForCampfire + " Wood to add a Campfire.";
            string chairsstr = "/raft.addchairs - need" + MaterialsForChairs + " Wood to add Chairs";
            string destroystr = "/raft.destroy - Destroys raft give back " + RefundForScrap + " wood.";
            string locationstr = "/raft.loc - Shows X and Y location Coordinates.";
            string sailmodestr = "/raft.setsail - Enables Sailing Mode.";
            string stashstr = "/raft.stash - Toggles stash hidden from others or not.";
            string consolestr = "To fix invisible parts, hit F1, then type culling.env 0 in console";
            SendReply(player, " Raft Chat Commands (while seated): \n " + raftstr + " \n " + roofstr + " \n " + wallsstr + " \n " + sailstr + " \n " + netstr + " \n " + firestr + " \n " + chairsstr + " \n " + destroystr + " \n " + locationstr + " \n " + sailmodestr + " \n " + stashstr + " \n " + consolestr);
        }

        [ChatCommand("raft.build")]
        void cmdChatRaftBuild(BasePlayer player, string command, string[] args)
        {
            if (OnlyOneActiveRaft && hasRaft.ContainsKey(player.userID)) { SendReply(player, msg("hasraftalready", player.UserIDString)); return; }
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!IsStandingInWater(player) || player.IsSwimming()) { SendReply(player, msg("notstandingwater", player.UserIDString)); return; }
            if (player.isMounted) return;
            if (CheckUpgradeMats(player, -151838493, MaterialsForRaft, "Base Raft")) BuildRaft(player);
        }

        [ConsoleCommand("raft.build")]
        void cmdConsoleRaftBuild(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (OnlyOneActiveRaft && hasRaft.ContainsKey(player.userID)) { SendReply(player, msg("hasraftalready", player.UserIDString)); return; }
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!IsStandingInWater(player) || player.IsSwimming()) { SendReply(player, msg("notstandingwater", player.UserIDString)); return; }
            if (player.isMounted) return;
            if (CheckUpgradeMats(player, -151838493, MaterialsForRaft, "Base Raft")) BuildRaft(player);
        }

        [ChatCommand("raft.addwalls")]
        void cmdRaftAddWalls(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>();
            if (activeraft == null) return;
            if (activeraft.haswalls) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForWalls, "Walls")) activeraft.SpawnSideWalls();
        }

        [ConsoleCommand("raft.addwalls")]
        void cmdConsoleRaftAddWalls(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>();
            if (activeraft == null) return;
            if (activeraft.haswalls) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForWalls, "Walls")) activeraft.SpawnSideWalls();
        }

        [ChatCommand("raft.addroof")]
        void cmdRaftAddRoof(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.hasroof) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForRoof, "Roof")) activeraft.SpawnRoof();
        }

        [ConsoleCommand("raft.addroof")]
        void cmdConsoleRaftAddRoof(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.hasroof) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForRoof, "Roof")) activeraft.SpawnRoof();
        }

        [ChatCommand("raft.addsail")]
        void cmdRaftAddSail(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.hassail) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForSail, "Sail")) activeraft.SpawnSail();
        }

        [ConsoleCommand("raft.addsail")]
        void cmdConsoleRaftAddSail(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.hassail) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForSail, "Sail")) activeraft.SpawnSail();
        }

        [ChatCommand("raft.addfire")]
        void cmdRaftAddFire(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.hasfire) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForCampfire, "Campfire")) activeraft.SpawnCampfire();
        }

        [ConsoleCommand("raft.addfire")]
        void cmdConsoleRaftAddFire(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.hasfire) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, -151838493, MaterialsForCampfire, "Campfire")) activeraft.SpawnCampfire();
        }

        [ChatCommand("raft.addchairs")]
        void cmdRaftAddChairs(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.haschairs) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, 1534542921, MaterialsForChairs, "Chairs")) activeraft.SpawnPassengerChairs();
        }

        [ConsoleCommand("raft.addchairs")]
        void cmdConsoleRaftAddChairs(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.haschairs) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, 1534542921, MaterialsForChairs, "Chairs")) activeraft.SpawnPassengerChairs();
        }

        [ChatCommand("raft.addnet")]
        void cmdRaftAddNet(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (!activeraft.hasroof) { SendReply(player, msg("nonet", player.UserIDString)); return; }
            if (activeraft.hasnet) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, 1414245522, MaterialsForNet, "Net")) activeraft.SpawnNet();
        }

        [ConsoleCommand("raft.addnet")]
        void cmdConsoleRaftAddNet(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (!activeraft.hasroof) { SendReply(player, msg("nonet", player.UserIDString)); return; }
            if (activeraft.hasnet) { SendReply(player, msg("alreadyadded", player.UserIDString)); return; }
            if (CheckUpgradeMats(player, 1414245522, MaterialsForNet, "Net")) activeraft.SpawnNet();
        }

        [ChatCommand("raft.setsail")]
        void cmdRaftSail(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (!activeraft.hassail) return;
            var sailisin = activeraft.door.IsOpen();
            if (sailisin)
            {
                activeraft.door.SetFlag(BaseEntity.Flags.Open, false, false);
            }
            activeraft.setsail = true;
            SendReply(player, msg("setsail", player.UserIDString));
        }

        [ConsoleCommand("raft.setsail")]
        void cmdConsoleRaftSail(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (!activeraft.hassail) return;
            var sailisin = activeraft.door.IsOpen();
            if (sailisin)
            {
                activeraft.door.SetFlag(BaseEntity.Flags.Open, false, false);
            }
            activeraft.setsail = true;
            SendReply(player, msg("setsail", player.UserIDString));
        }

        [ChatCommand("raft.loc")]
        void cmdRaftLoc(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            string location = player.transform.position.x + " / " + player.transform.position.z;
            SendReply(player, "you position is : " + location);
        }

        [ConsoleCommand("raft.loc")]
        void cmdConsoleRaftLoc(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            string location = player.transform.position.x + " / " + player.transform.position.z;
            SendReply(player, "you position is : " + location);
        }

        [ChatCommand("raft.destroy")]
        void cmdRaftDestroy(BasePlayer player, string command, string[] args)
        {
            BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseEntity).ToArray();
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (!obj.IsValid() || obj.IsDestroyed)
                        continue;

                    var israft = obj.GetComponent<RaftEntity>() ?? null;
                    if (israft != null && obj.OwnerID == player.userID)
                    {
                        storedRafts.Remove(obj.net.ID);
                        SaveData();
                        israft.entity.Invoke("KillMessage", 0.1f);
                        ScrapRaft(player);
                    }
                }
            }
        }

        [ChatCommand("raft.stash")]
        void cmdRaftStash(BasePlayer player, string command, string[] args)
        {
            if (DisableRaftStash) return;
            if (!isAllowed(player, "raft.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!player.isMounted) return;
            var activeraft = player.GetMounted().GetComponentInParent<RaftEntity>() ?? null;
            if (activeraft == null) return;
            if (activeraft.entity.OwnerID == player.userID) activeraft.ToggleStash();
            return;
        }

        #endregion

        #region Raft Entity

        public class RaftEntity : BaseEntity
        {
            Raft raft;
            public BaseEntity entity;
            BaseEntity floor1;
            BaseEntity floor2;
            BaseEntity barrel1;
            BaseEntity barrel2;
            BaseEntity barrel3;
            BaseEntity barrel4;
            BaseEntity barrel5;
            BaseEntity barrel6;
            BaseEntity backtop;
            BaseEntity backside1;
            BaseEntity backside2;
            BaseEntity rudder;
            BaseEntity pole1;
            BaseEntity pole2;
            BaseEntity pole3;
            BaseEntity pole4;
            public BaseEntity roof1;
            public BaseEntity roof2;
            BaseOven fire;
            BaseOven lantern;
            public BaseEntity net1;
            public BaseEntity door;
            BaseEntity door2;
            BaseEntity chairbackright;
            BaseEntity chairbackleft;
            BaseEntity stash;
            BaseEntity lootbarrel;
            public BaseEntity boatlock;

            Vector3 entitypos;
            Quaternion entityrot;
            public BasePlayer player;
            ulong ownerid;
            int counter;
            bool lootready;
            public bool ismoving;
            public bool moveforward;
            public bool movebackward;
            public bool rotright;
            public bool rotleft;
            public bool setsail;
            public bool hasroof;
            public bool haswalls;
            public bool hassail;
            public bool hasfire;
            public bool hasnet;
            public bool haschairs;
            bool spawnfullybuilt;
            Vector3 movedirection;
            Vector3 rotdirection;
            Vector3 startloc;
            Vector3 startrot;
            Vector3 endloc;
            float steps;
            float sailsteps;
            float incrementor;
            float waterheight;
            float groundheight;
            public float ghitDistance;
            public float whitDistance;
            private static int waterlayer;
            private static int groundlayer;
            private static int buildinglayer;

            void Awake()
            {
                entity = GetComponent<BaseEntity>();
                entitypos = entity.transform.position;
                entityrot = Quaternion.identity;
                ownerid = entity.OwnerID;
                raft = new Raft();
                waterlayer = UnityEngine.LayerMask.GetMask("Water");
                groundlayer = UnityEngine.LayerMask.GetMask("Terrain", "World", "Construction", "Default");
                buildinglayer = UnityEngine.LayerMask.GetMask("World", "Construction", "Default");
                counter = 0;
                incrementor = 0;
                ismoving = false;
                moveforward = false;
                movebackward = false;
                rotright = false;
                rotleft = false;
                setsail = false;
                hasroof = false;
                haswalls = false;
                hassail = false;
                hasfire = false;
                hasnet = false;
                haschairs = false;
                lootready = true;
                startrot = entity.transform.eulerAngles;
                startloc = entity.transform.position;
                steps = DefaultRaftMovementSpeed;
                sailsteps = DefaultRaftSailingSpeed;
                spawnfullybuilt = SpawnFullyBuilt;

                SpawnRaft();
                SpawnLock();
                if (!DisableRaftStash) SpawnStash();
                if (SpawnFullyBuilt)
                {
                    SpawnSideWalls();
                    SpawnRoof();
                    SpawnCampfire();
                    SpawnNet();
                    SpawnSail();
                    SpawnPassengerChairs();
                }
                RefreshAll();
            }

            public void SpawnStash()
            {
                string prefabstash = "assets/prefabs/deployable/small stash/small_stash_deployed.prefab";
                stash = GameManager.server.CreateEntity(prefabstash, entitypos, entityrot, false);
                stash.transform.localEulerAngles = new Vector3(0, 0, 0);
                stash.transform.localPosition = new Vector3(0f, 0f, 0f);
                var stashcont = stash.GetComponent<StashContainer>();
                stashcont.uncoverRange = -1f;
                stashcont.burriedOffset = 1f;
                var stashstab = stash.GetComponent<StabilityEntity>();
                if (stashstab) stashstab.grounded = true;
                stash.OwnerID = ownerid;
                stash?.Spawn();
                stash.SetParent(entity);
            }

            void SpawnLock()
            {
                string copterlockprefab = "assets/prefabs/locks/keypad/lock.code.prefab";

                boatlock = GameManager.server.CreateEntity(copterlockprefab, entitypos, entityrot, true);
                boatlock.transform.localEulerAngles = new Vector3(0, 90, 90);
                boatlock.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                boatlock.OwnerID = ownerid;
                boatlock?.Spawn();
                boatlock.SetParent(entity, 0);
            }

            public void SpawnSail()
            {
                string prefabdoor = "assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab";
                string prefabmast = "assets/prefabs/deployable/signs/sign.post.single.prefab";
                door = GameManager.server.CreateEntity(prefabdoor, entitypos, entityrot, true);
                door.transform.localEulerAngles = new Vector3(90, -90, 0);
                door.transform.localPosition = new Vector3(1.3f, 3.6f, 0.6f);
                var doorstab = door.GetComponent<StabilityEntity>();
                doorstab.grounded = true;
                door.skinID = 1199376910;
                door.OwnerID = ownerid;
                door?.Spawn();
                door.SetParent(entity);
                door.SetFlag(BaseEntity.Flags.Open, true, true);

                door2 = GameManager.server.CreateEntity(prefabdoor, entitypos, entityrot, true);
                door2.transform.localEulerAngles = new Vector3(90, -90, 0);
                door2.transform.localPosition = new Vector3(1.3f, 1.2f, 0.6f);
                var door2stab = door2.GetComponent<StabilityEntity>();
                door2stab.grounded = true;
                door2.skinID = 1199376910;
                door2.OwnerID = ownerid;
                door2?.Spawn();
                door2.SetParent(entity);
                door2.SetFlag(BaseEntity.Flags.Open, true, true);
                door2.SetFlag(BaseEntity.Flags.Locked, true, true);
                hassail = true;
            }

            void SpawnRaft()
            {
                string prefabfloor = "assets/prefabs/deployable/signs/sign.huge.wood.prefab";
                string prefabbarrel = "assets/bundled/prefabs/radtown/oil_barrel.prefab";
                string prefabrudder = "assets/prefabs/deployable/signs/sign.post.single.prefab";

                floor1 = GameManager.server.CreateEntity(prefabfloor, entitypos, entityrot, true);
                floor1.transform.localEulerAngles = new Vector3(-90, 90, 0);
                floor1.transform.localPosition = new Vector3(0f, 0f, 0f);
                var floor1stab = floor1.GetComponent<StabilityEntity>();
                if (floor1stab) floor1stab.grounded = true;
                floor1.OwnerID = ownerid;
                floor1?.Spawn();
                floor1.SetParent(entity);

                floor2 = GameManager.server.CreateEntity(prefabfloor, entitypos, entityrot, true);
                floor2.transform.localEulerAngles = new Vector3(-90, 90, 0);
                floor2.transform.localPosition = new Vector3(2f, 0f, 0f);
                var floor2stab = floor2.GetComponent<StabilityEntity>();
                if (floor2stab) floor2stab.grounded = true;
                floor2.OwnerID = ownerid;
                floor2?.Spawn();
                floor2.SetParent(entity);

                rudder = GameManager.server.CreateEntity(prefabrudder, entitypos, entityrot, true);
                rudder.transform.localEulerAngles = new Vector3(0, 270, 130);
                rudder.transform.localPosition = new Vector3(0f, 0.7f, -1.7f);
                var rudderstab = rudder.GetComponent<StabilityEntity>();
                if (rudderstab) rudderstab.grounded = true;
                rudder.skinID = 1;
                rudder.OwnerID = ownerid;
                rudder?.Spawn();
                rudder.SetParent(entity);
                rudder.SetFlag(BaseEntity.Flags.Busy, true, true);

                barrel1 = GameManager.server.CreateEntity(prefabbarrel, entitypos, entityrot, true);
                barrel1.transform.localEulerAngles = new Vector3(-90, 0, 0);
                barrel1.transform.localPosition = new Vector3(-1.8f, -0.4f, -1.5f);
                var barrel1stab = barrel1.GetComponent<StabilityEntity>();
                if (barrel1stab) barrel1stab.grounded = true;
                barrel1.OwnerID = ownerid;
                barrel1?.Spawn();
                barrel1.SetParent(entity);

                barrel2 = GameManager.server.CreateEntity(prefabbarrel, entitypos, entityrot, true);
                barrel2.transform.localEulerAngles = new Vector3(-90, 0, 0);
                barrel2.transform.localPosition = new Vector3(-1.8f, -0.4f, 0.5f);
                var barrel2stab = barrel2.GetComponent<StabilityEntity>();
                if (barrel2stab) barrel2stab.grounded = true;
                barrel2.OwnerID = ownerid;
                barrel2?.Spawn();
                barrel2.SetParent(entity);

                barrel3 = GameManager.server.CreateEntity(prefabbarrel, entitypos, entityrot, true);
                barrel3.transform.localEulerAngles = new Vector3(-90, 0, 0);
                barrel3.transform.localPosition = new Vector3(-1.8f, -0.4f, 2.5f);
                var barrel3stab = barrel3.GetComponent<StabilityEntity>();
                if (barrel3stab) barrel3stab.grounded = true;
                barrel3.OwnerID = ownerid;
                barrel3?.Spawn();
                barrel3.SetParent(entity);

                barrel4 = GameManager.server.CreateEntity(prefabbarrel, entitypos, entityrot, true);
                barrel4.transform.localEulerAngles = new Vector3(-90, 0, 0);
                barrel4.transform.localPosition = new Vector3(1.8f, -0.4f, -1.5f);
                var barrel4stab = barrel4.GetComponent<StabilityEntity>();
                if (barrel4stab) barrel4stab.grounded = true;
                barrel4.OwnerID = ownerid;
                barrel4?.Spawn();
                barrel4.SetParent(entity);

                barrel5 = GameManager.server.CreateEntity(prefabbarrel, entitypos, entityrot, true);
                barrel5.transform.localEulerAngles = new Vector3(-90, 0, 0);
                barrel5.transform.localPosition = new Vector3(1.8f, -0.4f, 0.5f);
                var barrel5stab = barrel5.GetComponent<StabilityEntity>();
                if (barrel5stab) barrel5stab.grounded = true;
                barrel5.OwnerID = ownerid;
                barrel5?.Spawn();
                barrel5.SetParent(entity);

                barrel6 = GameManager.server.CreateEntity(prefabbarrel, entitypos, entityrot, true);
                barrel6.transform.localEulerAngles = new Vector3(-90, 0, 0);
                barrel6.transform.localPosition = new Vector3(1.8f, -0.4f, 2.5f);
                var barrel6stab = barrel6.GetComponent<StabilityEntity>();
                if (barrel6stab) barrel6stab.grounded = true;
                barrel6.OwnerID = ownerid;
                barrel6?.Spawn();
                barrel6.SetParent(entity);

            }

            public void SpawnPassengerChairs()
            {
                string prefabchair = "assets/prefabs/deployable/chair/chair.deployed.prefab";
                chairbackright = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairbackright.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairbackright.transform.localPosition = new Vector3(0.9f, 0f, -2);
                var rmount = chairbackright.GetComponent<BaseMountable>();
                rmount.isMobile = true;
                chairbackright.OwnerID = ownerid;
                chairbackright?.Spawn();
                chairbackright.SetParent(entity);

                chairbackleft = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairbackleft.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairbackleft.transform.localPosition = new Vector3(-0.9f, 0f, -2);
                var lmount = chairbackleft.GetComponent<BaseMountable>();
                lmount.isMobile = true;
                chairbackleft.OwnerID = ownerid;
                chairbackleft?.Spawn();
                chairbackleft.SetParent(entity);
                haschairs = true;
            }

            public void SpawnNet()
            {
                string prefabnet = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab";
                net1 = GameManager.server.CreateEntity(prefabnet, entitypos, entityrot, false);
                net1.transform.localEulerAngles = new Vector3(0, 0, 17);
                net1.transform.localPosition = new Vector3(2.4f, -0.9f, -1f);
                var netstab1 = net1.GetComponent<StabilityEntity>();
                netstab1.grounded = true;
                net1.OwnerID = ownerid;
                net1?.Spawn();
                net1.SetParent(entity);
                hasnet = true;
            }

            public void SpawnCampfire()
            {
                string prefabfire = "assets/prefabs/deployable/campfire/campfire.prefab";
                fire = GameManager.server.CreateEntity(prefabfire, entitypos, entityrot, true) as BaseOven;
                fire.transform.localEulerAngles = new Vector3(0, 180, 0);
                fire.transform.localPosition = new Vector3(1.5f, 0f, 2f);
                fire.OwnerID = ownerid;
                fire?.Spawn();
                fire.SetParent(entity);
                fire.SetFlag(BaseEntity.Flags.On, false, true);

                string prefablantern = "assets/prefabs/deployable/lantern/lantern.deployed.prefab";
                lantern = GameManager.server.CreateEntity(prefablantern, entitypos, entityrot, true) as BaseOven;
                lantern.transform.localEulerAngles = new Vector3(0, 180, 0);
                lantern.transform.localPosition = new Vector3(0f, 0f, 2.4f);
                lantern.OwnerID = ownerid;
                lantern?.Spawn();
                lantern.SetParent(entity);
                hasfire = true;
            }

            public void SpawnSideWalls()
            {
                string prefabtop = "assets/prefabs/deployable/signs/sign.large.wood.prefab";
                backtop = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, true);
                backtop.transform.localEulerAngles = new Vector3(0, 180, 0);
                backtop.transform.localPosition = new Vector3(0f, 0f, -2.4f);
                backtop.skinID = 1;
                backtop.OwnerID = ownerid;
                backtop?.Spawn();
                backtop.SetParent(entity);

                backside1 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, true);
                backside1.transform.localEulerAngles = new Vector3(0, 90, 0);
                backside1.transform.localPosition = new Vector3(1.55f, 0f, -1f);
                backside1.OwnerID = ownerid;
                backside1?.Spawn();
                backside1.SetParent(entity);

                backside2 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, true);
                backside2.transform.localEulerAngles = new Vector3(0, 270, 0);
                backside2.transform.localPosition = new Vector3(-1.55f, 0f, -1f);
                backside2.OwnerID = ownerid;
                backside2?.Spawn();
                backside2.SetParent(entity);
                haswalls = true;
            }

            public void SpawnRoof()
            {
                string prefabrudder = "assets/prefabs/deployable/signs/sign.post.single.prefab";
                string prefabtop = "assets/prefabs/deployable/signs/sign.large.wood.prefab";
                pole1 = GameManager.server.CreateEntity(prefabrudder, entitypos, entityrot, true);
                pole1.transform.localEulerAngles = new Vector3(0, 90, 180);
                pole1.transform.localPosition = new Vector3(1.5f, 2.3f, 0f);
                pole1.OwnerID = ownerid;
                pole1?.Spawn();
                pole1.SetParent(entity);
                pole1.SetFlag(BaseEntity.Flags.Busy, true, true);

                pole2 = GameManager.server.CreateEntity(prefabrudder, entitypos, entityrot, true);
                pole2.transform.localEulerAngles = new Vector3(0, 90, 180);
                pole2.transform.localPosition = new Vector3(-1.5f, 2.3f, 0f);
                pole2.OwnerID = ownerid;
                pole2?.Spawn();
                pole2.SetParent(entity);
                pole2.SetFlag(BaseEntity.Flags.Busy, true, true);

                pole3 = GameManager.server.CreateEntity(prefabrudder, entitypos, entityrot, true);
                pole3.transform.localEulerAngles = new Vector3(0, 90, 180);
                pole3.transform.localPosition = new Vector3(1.5f, 2.3f, -1.5f);
                pole3.OwnerID = ownerid;
                pole3?.Spawn();
                pole3.SetParent(entity);
                pole3.SetFlag(BaseEntity.Flags.Busy, true, true);

                pole4 = GameManager.server.CreateEntity(prefabrudder, entitypos, entityrot, true);
                pole4.transform.localEulerAngles = new Vector3(0, 90, 180);
                pole4.transform.localPosition = new Vector3(-1.5f, 2.3f, -1.5f);
                pole4.OwnerID = ownerid;
                pole4?.Spawn();
                pole4.SetParent(entity);
                pole4.SetFlag(BaseEntity.Flags.Busy, true, true);

                roof1 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, true);
                roof1.transform.localEulerAngles = new Vector3(-90, 0, 0);
                roof1.transform.localPosition = new Vector3(0f, 2.2f, 0.5f);
                roof1.OwnerID = ownerid;
                roof1?.Spawn();
                roof1.SetParent(entity);

                roof2 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, true);
                roof2.transform.localEulerAngles = new Vector3(-90, 0, 0);
                roof2.transform.localPosition = new Vector3(0f, 2.2f, -1f);
                roof2.OwnerID = ownerid;
                roof2?.Spawn();
                roof2.SetParent(entity);
                hasroof = true;
            }

            bool hitSomething(Vector3 position)
            {
                var directioncheck = new Vector3();
                if (moveforward || setsail) directioncheck = position + (transform.forward * 2);
                if (movebackward) directioncheck = position - (transform.forward * 2);
                if (GamePhysics.CheckSphere(directioncheck, 1f, buildinglayer, 0)) return true;
                return false;
            }

            bool isStillInWater(Vector3 position)
            {
                var waterdepth = (TerrainMeta.WaterMap.GetHeight(position) - TerrainMeta.HeightMap.GetHeight(position));
                if (waterdepth >= 0.5f) return true;
                return false;
            }

            bool PlayerIsMounted()
            {
                bool flag = entity.GetComponent<BaseMountable>().IsMounted();
                return flag;

            }

            public void ToggleStash()
            {
                var stashcontainer = stash.GetComponent<StashContainer>() ?? null;
                if (stashcontainer != null && stashcontainer.IsHidden()) { stashcontainer.SetHidden(false); return; }
                if (stashcontainer != null) stashcontainer.SetHidden(true);
            }

            public void UnfurlTheSails()
            {
                door.SetFlag(BaseEntity.Flags.Open, false, false);
                setsail = true;
                if (door != null) door.transform.hasChanged = true;
                if (door != null) door.SendNetworkUpdateImmediate();
                if (door != null) door.UpdateNetworkGroup();
            }

            public void FurlTheSails()
            {
                door.SetFlag(BaseEntity.Flags.Open, true, true);
                setsail = false;
                if (door != null) door.transform.hasChanged = true;
                if (door != null) door.SendNetworkUpdateImmediate();
                if (door != null) door.UpdateNetworkGroup();
            }

            void SplashEffect()
            {
                Effect.server.Run("assets/bundled/prefabs/fx/water/midair_splash.prefab", barrel1.transform.position);
                Effect.server.Run("assets/bundled/prefabs/fx/water/midair_splash.prefab", barrel4.transform.position);
            }

            void SpawnRandomLoot()
            {
                if (!RandomOceanLootSpawn || !lootready) return;
                counter = counter + 1;
                if (counter >= RandomLootTick)
                {
                    int roll = UnityEngine.Random.Range(0, 100);
                    if (roll < RandomLootChance)
                    {
                        var randomlootprefab = "assets/bundled/prefabs/radtown/crate_basic.prefab";
                        int rlroll = UnityEngine.Random.Range(1, 6);
                        if (rlroll == 1) randomlootprefab = LootOption1;
                        if (rlroll == 2) randomlootprefab = LootOption2;
                        if (rlroll == 3) randomlootprefab = LootOption3;
                        if (rlroll == 4) randomlootprefab = LootOption4;
                        if (rlroll == 5) randomlootprefab = LootOption5;

                        var spawnpos = GetSpawnLocation();
                        lootbarrel = GameManager.server.CreateEntity(randomlootprefab, spawnpos, Quaternion.identity, true);
                        lootbarrel?.Spawn();
                        lootready = false;
                        raft.timer.Once(100f, () => ResetLootSpawn(lootbarrel));
                        counter = 0;
                    }
                    counter = 0;
                }
            }

            void ResetLootSpawn(BaseEntity lootbarrel)
            {
                lootready = true;
                if (lootbarrel != null) lootbarrel.Invoke("KillMessage", 0.1f);
            }

            Vector3 GetSpawnLocation()
            {
                var currentpos = entity.transform.position;
                Vector3 randomizer = new Vector3(UnityEngine.Random.Range(-40f, 40f), 0f, UnityEngine.Random.Range(-40f, 40f));
                Vector3 newp = (currentpos + (transform.forward * 40f)) + randomizer;
                var spawnPos = new Vector3(newp.x, currentpos.y + -0.5f, newp.z);
                return spawnPos;
            }

            void FixedUpdate()
            {
                if (!ismoving && !(setsail || moveforward || movebackward || rotright || rotleft)) return;
                if (!PlayerIsMounted()) { ResetMovement(); RefreshAll(); ismoving = false; return; }
                var currentloc = entity.transform.position;
                waterheight = TerrainMeta.WaterMap.GetHeight(currentloc);
                startloc = new Vector3(currentloc.x, waterheight + 0.4f, currentloc.z);
                startrot = entity.transform.eulerAngles;

                if (rotright) rotdirection = new Vector3(startrot.x, startrot.y + 1, startrot.z);
                if (rotleft) rotdirection = new Vector3(startrot.x, startrot.y - 1, startrot.z);

                if (setsail) endloc = startloc + (transform.forward * sailsteps) * Time.deltaTime;
                else if (moveforward) endloc = startloc + (transform.forward * steps) * Time.deltaTime;
                else if (movebackward) endloc = startloc + (transform.forward * -steps) * Time.deltaTime;

                if (hitSomething(endloc)) { endloc = startloc; ResetMovement(); RefreshAll(); return; }
                if (!isStillInWater(endloc)) { endloc = startloc; ResetMovement(); RefreshAll(); return; }
                if (endloc.x >= 3900 || endloc.x <= -3900 || endloc.z >= 3900 || endloc.z <= -3900) { endloc = startloc; ResetMovement(); RefreshAll(); return; }
                if (endloc.x >= 2000 || endloc.x <= -2000 || endloc.z >= 2000 || endloc.z <= -2000) SpawnRandomLoot();

                if (ShowWaterSplash) SplashEffect();

                if (endloc == new Vector3(0f, 0f, 0f)) endloc = startloc;
                entity.transform.eulerAngles = rotdirection;
                entity.transform.localPosition = endloc;
                RefreshAll();
            }

            void ResetMovement()
            {
                moveforward = false;
                movebackward = false;
                rotright = false;
                rotleft = false;
                setsail = false;
            }

            void RefreshAll()
            {
                if (entity == null) return;
                if (entity != null)
                {
                    entity.transform.hasChanged = true;
                    var entitymount = entity.GetComponent<BaseMountable>() ?? null;
                    if (entitymount != null)
                    {
                        entitymount.isMobile = true;
                    }
                    entity.SendNetworkUpdateImmediate();
                    entity.UpdateNetworkGroup();
                    entity.GetComponent<DestroyOnGroundMissing>().enabled = false;
                    entity.GetComponent<GroundWatch>().enabled = false;

                    if (entity.children != null)
                        for (int i = 0; i < entity.children.Count; i++)
                        {
                            entity.children[i].transform.hasChanged = true;
                            var hasmount = entity.children[i].GetComponent<BaseMountable>() ?? null;
                            if (hasmount != null)
                            {
                                hasmount.isMobile = true;
                            }
                            entity.children[i].SendNetworkUpdateImmediate();
                            entity.children[i].UpdateNetworkGroup();
                        }
                }
            }

            public void OnDestroy()
            {
                if (hasRaft.ContainsKey(ownerid)) hasRaft.Remove(ownerid);
                if (lootbarrel != null && !lootbarrel.IsDestroyed) { lootbarrel.Invoke("KillMessage", 0.1f); }
                if (entity != null && !entity.IsDestroyed) { entity.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

        #region Configuration

        static float DefaultRaftMovementSpeed = 4f;
        static float DefaultRaftSailingSpeed = 4f;
        static bool DisableRaftStash = false;
        static bool SpawnFullyBuilt = false;
        static bool OnlyOneActiveRaft = true;
        static bool ShowWaterSplash = false;

        static bool RandomOceanLootSpawn = true;
        static int RandomLootChance = 50;
        static int RandomLootTick = 100;
        static string LootOption1 = "assets/bundled/prefabs/radtown/crate_basic.prefab";
        static string LootOption2 = "assets/bundled/prefabs/radtown/crate_elite.prefab";
        static string LootOption3 = "assets/bundled/prefabs/radtown/crate_mine.prefab";
        static string LootOption4 = "assets/bundled/prefabs/radtown/crate_normal.prefab";
        static string LootOption5 = "assets/bundled/prefabs/radtown/crate_normal_2.prefab";

        static int MaterialsForRoof = 2000;
        static int MaterialsForWalls = 2000;
        static int MaterialsForRaft = 5000;
        static int MaterialsForSail = 3000;
        static int MaterialsForCampfire = 100;
        static int MaterialsForNet = 3;
        static int MaterialsForChairs = 2;
        static int RefundForScrap = 5000;

        bool Changed;

        bool isRestricted;
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
            CheckCfg("Spawn - Always Spawn fully built rafts ? ", ref SpawnFullyBuilt);
            CheckCfg("Stash - Disable Stash on Rafts ? ", ref DisableRaftStash);
            CheckCfg("Usage - Only 1 Active Raft per player ? ", ref OnlyOneActiveRaft);
            CheckCfg("Effect - Show water splash effect when moving ? ", ref ShowWaterSplash);

            CheckCfg("Loot - Toggle Random Deep Ocean Loot Spawns (Past 2000 X or Z coords): ", ref RandomOceanLootSpawn);
            CheckCfg("Loot - Percent Chance Random Deep Ocean Loot will spawn (if enabled) : ", ref RandomLootChance);
            CheckCfg("Loot - Tick rate to check if Random Ocean Loot will spawn : ", ref RandomLootTick);
            CheckCfg("Loot - Loot Option 1 prefab : ", ref LootOption1);
            CheckCfg("Loot - Loot Option 2 prefab : ", ref LootOption2);
            CheckCfg("Loot - Loot Option 3 prefab : ", ref LootOption3);
            CheckCfg("Loot - Loot Option 4 prefab : ", ref LootOption4);
            CheckCfg("Loot - Loot Option 5 prefab : ", ref LootOption5);

            CheckCfg("Materials - Roof - Amount of Wood needed to build : ", ref MaterialsForRoof);
            CheckCfg("Materials - Walls - Amount of Wood needed to build : ", ref MaterialsForWalls);
            CheckCfg("Materials - Raft- Amount of Wood needed to build : ", ref MaterialsForRaft);
            CheckCfg("Materials - Sail - Amount of Wood needed to build : ", ref MaterialsForSail);
            CheckCfg("Materials - Campfire - Amount of Wood needed to build : ", ref MaterialsForCampfire);
            CheckCfg("Materials - Net- Amount of Rope needed to build : ", ref MaterialsForNet);
            CheckCfg("Materials - Chairs - Amount of Chairs needed to build : ", ref MaterialsForChairs);
            CheckCfg("Scrap Refund - Amount of wood to refund player when using /raft.destroy : ", ref RefundForScrap);

            CheckCfgFloat("Speed - Default Raft Movement Speed : ", ref DefaultRaftMovementSpeed);
            CheckCfgFloat("Speed - Default Sail Mode Movement Speed : ", ref DefaultRaftSailingSpeed);
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
            ["notauthorized"] = "You are not authorized to use that command !!",
            ["notowner"] = "You must be owner of boat to pilot it !!!",
            ["hasraftalready"] = "You already have a raft in the world !!!",
            ["captain"] = "You are now the Captain of this boat !!!",
            ["setsail"] = "Your sails are unfurled, now your sailing !!!",
            ["alreadyadded"] = "That part is already installed !!!",
            ["missingmaterials"] = "You are missing the required materials to uprade to that !! ",
            ["nonet"] = "You need a roof installed before you can add a Net !!! ",
            ["endofworld"] = "Movement blocked !!! You are at the end of the playable world !!!",
            ["notstandingwater"] = "You must be in deeper water but NOT swimming to build a raft !!"
        };

        #endregion

    }
}