using System;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Rust;
using Network;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Facepunch;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("VikingBoat", "Colon Blow", "1.0.7")]
    class VikingBoat : RustPlugin
    {

        #region Data

        BaseEntity newVikingBoat;
        static Dictionary<ulong, string> hasVikingBoat = new Dictionary<ulong, string>();

        static List<uint> storedVikingBoats = new List<uint>();
        private DynamicConfigFile data;
        private bool initialized;

        void Loaded()
        {
            LoadVariables();
            permission.RegisterPermission("vikingBoat.builder", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("VikingBoat_data");
        }

        private void OnServerInitialized()
        {
            initialized = true;
            LoadData();
            timer.In(3, RestoreVikingBoats);
        }
        private void OnServerSave()
        {
            SaveData();
        }

        private void RestoreVikingBoats()
        {
            if (storedVikingBoats.Count > 0)
            {
                BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseEntity).ToArray();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        if (!obj.IsValid() || obj.IsDestroyed)
                            continue;

                        if (storedVikingBoats.Contains(obj.net.ID))
                        {
                            var spawnpos = obj.transform.position;
                            var spawnrot = obj.transform.rotation;
                            var userid = obj.OwnerID;
                            string codestr = "0";
                            string guestcodestr = "0";
                            foreach (Transform child in obj.GetComponent<Transform>())
                            {
                                if (child.name.Contains("keypad/lock.code"))
                                {
                                    CodeLock codelock = child.GetComponent<CodeLock>() ?? null;
                                    if (codelock != null) codestr = codelock.code;
                                    if (codelock != null) guestcodestr = codelock.guestCode;
                                }
                            }

                            storedVikingBoats.Remove(obj.net.ID);
                            obj.Invoke("KillMessage", 0.1f);
                            timer.Once(3f, () => RespawnVikingBoat(spawnpos, spawnrot, userid, codestr, guestcodestr));
                        }
                    }
                }
            }
        }

        void SaveData() => data.WriteObject(storedVikingBoats.ToList());
        void LoadData()
        {
            try
            {
                storedVikingBoats = data.ReadObject<List<uint>>();
            }
            catch
            {
                storedVikingBoats = new List<uint>();
            }
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        public void AddPlayerVikingBoat(ulong id)
        {
            if (!OnlyOneActiveVikingBoat) return;
            if (hasVikingBoat.ContainsKey(id)) return;
            hasVikingBoat.Add(id, "");
        }

        public void RemovePlayerVikingBoat(ulong id)
        {
            if (!OnlyOneActiveVikingBoat) return;
            if (!hasVikingBoat.ContainsKey(id)) return;
            hasVikingBoat.Remove(id);
        }

        #endregion

        #region Hooks

        public void BuildVikingBoat(BasePlayer player)
        {
            string prefabstr = "assets/prefabs/deployable/chair/chair.deployed.prefab";
            var waterheight = TerrainMeta.WaterMap.GetHeight(player.transform.position);
            var spawnpos = new Vector3(player.transform.position.x, waterheight, player.transform.position.z);
            newVikingBoat = GameManager.server.CreateEntity(prefabstr, spawnpos, new Quaternion(), true);
            var mount = newVikingBoat.GetComponent<BaseMountable>();
            mount.isMobile = true;
            newVikingBoat.enableSaving = true;
            newVikingBoat.OwnerID = player.userID;
            newVikingBoat?.Spawn();
            var addVikingBoat = newVikingBoat.gameObject.AddComponent<VikingBoatEntity>();
            AddPlayerVikingBoat(player.userID);
            storedVikingBoats.Add(newVikingBoat.net.ID);
            mount.MountPlayer(player);
            SaveData();
        }

        public void RespawnVikingBoat(Vector3 spawnpos, Quaternion spawnrot, ulong userid, string codestr, string guestcodestr)
        {
            string prefabstr = "assets/prefabs/deployable/chair/chair.deployed.prefab";
            newVikingBoat = GameManager.server.CreateEntity(prefabstr, spawnpos, spawnrot, true);
            var mount = newVikingBoat.GetComponent<BaseMountable>();
            mount.isMobile = true;
            newVikingBoat.enableSaving = true;
            newVikingBoat.OwnerID = userid;
            newVikingBoat?.Spawn();
            var addVikingBoat = newVikingBoat.gameObject.AddComponent<VikingBoatEntity>();
            if (codestr != "0")
            {
                var codelock = addVikingBoat.boatlock.GetComponent<CodeLock>() ?? null;
                if (codelock != null)
                {
                    codelock.whitelistPlayers.Add(userid);
                    codelock.code = codestr;
                    codelock.guestCode = guestcodestr;
                    codelock.SetFlag(BaseEntity.Flags.Locked, true, false);
                }
            }
            AddPlayerVikingBoat(userid);
            storedVikingBoats.Add(newVikingBoat.net.ID);
            SaveData();
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
            var activeVikingBoat = player.GetMounted().GetComponentInParent<VikingBoatEntity>() ?? null;
            if (activeVikingBoat == null) return;
            if (player.GetMounted() != activeVikingBoat.entity) return;
            if (!isAllowed(player, "vikingBoat.builder")) return;
            if (activeVikingBoat.boatlock.IsLocked() == true) return;
            if (input != null)
            {
                if (input.WasJustPressed(BUTTON.FORWARD)) { activeVikingBoat.moveforward = true; }
                if (input.WasJustReleased(BUTTON.FORWARD)) activeVikingBoat.moveforward = false;
                if (input.WasJustPressed(BUTTON.BACKWARD)) { activeVikingBoat.movebackward = true; }
                if (input.WasJustReleased(BUTTON.BACKWARD)) activeVikingBoat.movebackward = false;
                if (input.WasJustPressed(BUTTON.RIGHT)) activeVikingBoat.rotright = true;
                if (input.WasJustReleased(BUTTON.RIGHT)) activeVikingBoat.rotright = false;
                if (input.WasJustPressed(BUTTON.LEFT)) activeVikingBoat.rotleft = true;
                if (input.WasJustReleased(BUTTON.LEFT)) activeVikingBoat.rotleft = false;
                if (input.WasJustPressed(BUTTON.JUMP))
                {
                    activeVikingBoat.moveforward = false;
                    activeVikingBoat.movebackward = false;
                    activeVikingBoat.rotright = false;
                    activeVikingBoat.rotleft = false;
                }
                return;
            }
        }

        private object OnEntityGroundMissing(BaseEntity entity)
        {
            var VikingBoat = entity.GetComponentInParent<VikingBoatEntity>();
            if (VikingBoat != null) return false;
            return null;
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            var VikingBoat = networkable.GetComponentInParent<VikingBoatEntity>();
            if (VikingBoat != null && storedVikingBoats.Contains(networkable.net.ID))
            {
                storedVikingBoats.Remove(networkable.net.ID);
                SaveData();
            }
        }

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            if (mountable == null || player == null) return;
            var VikingBoatentity = mountable.GetComponentInParent<VikingBoatEntity>();
            if (VikingBoatentity == null) return;
            if (mountable != VikingBoatentity.entity) return;
            if (VikingBoatentity != null && VikingBoatentity.player == null)
            {
                SendReply(player, msg("captain", player.UserIDString));
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity1, HitInfo hitInfo)
        {
            if (entity1 == null || hitInfo == null) return null;
            var isboat = entity1.GetComponentInParent<VikingBoatEntity>();
            if (isboat)
            {
                if (entity1 == isboat.entity) return false;
                if (BlockDecay && hitInfo.damageTypes.GetMajorityDamageType().ToString().Contains("Decay")) return false;
            }
            return null;
        }

        private object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            if (entity == null || player == null) return null;
            if (entity.GetComponentInParent<VikingBoatEntity>()) return false;
            return null;
        }

        object CanPickupLock(BasePlayer player, BaseLock baseLock)
        {
            if (baseLock == null || player == null) return null;
            if (baseLock.GetComponentInParent<VikingBoatEntity>()) return false;
            return null;
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.isMounted)
                {
                    var activeVikingBoat = player.GetMounted().GetComponentInParent<VikingBoatEntity>();
                    if (activeVikingBoat != null) player.DismountObject();
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

        [ChatCommand("vikingboat.build")]
        void cmdVikingBoatBuild(BasePlayer player, string command, string[] args)
        {
            if (OnlyOneActiveVikingBoat && hasVikingBoat.ContainsKey(player.userID)) { SendReply(player, msg("hasVikingBoatalready", player.UserIDString)); return; }
            if (!isAllowed(player, "vikingBoat.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!IsStandingInWater(player) || player.IsSwimming()) { SendReply(player, msg("notstandingwater", player.UserIDString)); return; }
            if (player.isMounted) return;
            if (CheckUpgradeMats(player, MaterialID, MaterialsForVikingBoat, "Base VikingBoat")) BuildVikingBoat(player);
        }

        [ConsoleCommand("vikingboat.build")]
        void cmdConsoleVikingBoatBuild(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (OnlyOneActiveVikingBoat && hasVikingBoat.ContainsKey(player.userID)) { SendReply(player, msg("hasVikingBoatalready", player.UserIDString)); return; }
            if (!isAllowed(player, "vikingBoat.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            if (!IsStandingInWater(player) || player.IsSwimming()) { SendReply(player, msg("notstandingwater", player.UserIDString)); return; }
            if (player.isMounted) return;
            if (CheckUpgradeMats(player, MaterialID, MaterialsForVikingBoat, "Base VikingBoat")) BuildVikingBoat(player);
        }

        [ChatCommand("vikingboat.loc")]
        void cmdVikingBoatLoc(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "vikingBoat.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            string location = player.transform.position.x + " / " + player.transform.position.z;
            SendReply(player, "you position is : " + location);
        }

        [ConsoleCommand("vikingboat.loc")]
        void cmdConsoleVikingBoatLoc(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!isAllowed(player, "vikingBoat.builder")) { SendReply(player, msg("notauthorized", player.UserIDString)); return; }
            string location = player.transform.position.x + " / " + player.transform.position.z;
            SendReply(player, "you position is : " + location);
        }

        [ChatCommand("vikingboat.destroy")]
        void cmdVikingBoatDestroy(BasePlayer player, string command, string[] args)
        {
            BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseEntity).ToArray();
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (!obj.IsValid() || obj.IsDestroyed)
                        continue;

                    var isvikingboat = obj.GetComponent<VikingBoatEntity>();
                    if (isvikingboat && obj.OwnerID == player.userID)
                    {
                        storedVikingBoats.Remove(obj.net.ID);
                        SaveData();
                        isvikingboat.entity.Invoke("KillMessage", 0.1f);
                    }
                }
            }
        }

        #endregion

        #region VikingBoat Entity

        public class VikingBoatEntity : BaseEntity
        {
            VikingBoat VikingBoat;
            public BaseEntity entity;
            BaseEntity nosepoint;
            BaseEntity trianglefloor1;
            BaseEntity trianglefloor2;
            BaseEntity floor1;
            BaseEntity floor2;
            BaseEntity floor3;
            BaseEntity floor4;
            BaseEntity floor5;
            BaseEntity wall1;
            BaseEntity wall2;
            BaseEntity wall3;
            BaseEntity wall1r;
            BaseEntity wall2r;
            BaseEntity wall3r;
            BaseEntity wall4;
            BaseEntity wall4r;
            BaseEntity wall5;
            BaseEntity wall5r;
            BaseEntity wallback;
            BaseEntity wallfrontl;
            BaseEntity wallfrontr;
            BaseEntity chairleft1;
            BaseEntity chairleft2;
            BaseEntity chairleft3;
            BaseEntity chairleft4;
            BaseEntity chairleft5;
            BaseEntity chairright2;
            BaseEntity chairright3;
            BaseEntity chairright4;
            BaseEntity chairright5;
            BaseEntity sailfront;
            BaseEntity sailback;
            BaseEntity oarright1;
            BaseEntity oarright2;
            BaseEntity oarright3;
            BaseEntity oarright4;
            BaseEntity oarright5;
            BaseEntity oarleft1;
            BaseEntity oarleft2;
            BaseEntity oarleft3;
            BaseEntity oarleft4;
            BaseEntity oarleft5;
            BaseEntity oarcenter1;
            BaseEntity oarcenter2;
            BaseEntity oarcenter3;
            BaseEntity oarcenter4;
            BaseEntity oarcenter5;
            BaseEntity rudder;
            public BaseEntity boatlock;

            Vector3 entitypos;
            Quaternion entityrot;
            public BasePlayer player;
            ulong ownerid;
            int counter;
            public bool ismoving;
            public bool moveforward;
            public bool movebackward;
            public bool rotright;
            public bool rotleft;
            float waterheight;
            Vector3 movedirection;
            Vector3 rotdirection;
            Vector3 startloc;
            Vector3 startrot;
            Vector3 endloc;
            float steps;
            float incrementor;

            void Awake()
            {
                entity = GetComponent<BaseEntity>();
                entitypos = entity.transform.position;
                entityrot = Quaternion.identity;
                ownerid = entity.OwnerID;
                VikingBoat = new VikingBoat();
                counter = 0;
                incrementor = 0;
                ismoving = false;
                moveforward = false;
                movebackward = false;
                rotright = false;
                rotleft = false;
                startrot = entity.transform.eulerAngles;
                startloc = entity.transform.position;
                steps = DefaultVikingBoatMovementSpeed;

                SpawnVikingBoat();
                SpawnLock();
                SpawnSideWalls();
                SpawnSails();
                SpawnOars();
                SpawnPassengerChairs();
                RefreshAll();
            }

            void SpawnVikingBoat()
            {

                string testfloor = "assets/prefabs/building core/floor/floor.prefab";
                string testtrianglefloor = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab";
                string prefabpillar = "assets/prefabs/deployable/signs/sign.post.single.prefab";
                string prefabskull = "assets/prefabs/misc/skull/skull.prefab";

                nosepoint = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, false);
                nosepoint.transform.localEulerAngles = new Vector3(0, 270, 270);
                nosepoint.transform.localPosition = new Vector3(-1f, 0.9f, 2f);
                nosepoint?.Spawn();
                nosepoint.SetParent(entity);
                SpawnRefresh(nosepoint);

                trianglefloor1 = GameManager.server.CreateEntity(testtrianglefloor, entitypos, entityrot, false);
                trianglefloor1.transform.localEulerAngles = new Vector3(0, 0, 0);
                trianglefloor1.transform.localPosition = new Vector3(-1f, 0f, 1.5f);
                trianglefloor1?.Spawn();
                trianglefloor1.SetParent(entity);
                SpawnRefresh(trianglefloor1);

                trianglefloor2 = GameManager.server.CreateEntity(testtrianglefloor, entitypos, entityrot, false);
                trianglefloor2.transform.localEulerAngles = new Vector3(0, 0, 0);
                trianglefloor2.transform.localPosition = new Vector3(-1f, 0.9f, 1.5f);
                trianglefloor2?.Spawn();
                trianglefloor2.SetParent(entity);
                SpawnRefresh(trianglefloor2);

                floor1 = GameManager.server.CreateEntity(testfloor, entitypos, entityrot, false);
                floor1.transform.localEulerAngles = new Vector3(0, 0, 0);
                floor1.transform.localPosition = new Vector3(-1f, 0f, 0f);
                floor1?.Spawn();
                floor1.SetParent(entity);
                SpawnRefresh(floor1);

                floor2 = GameManager.server.CreateEntity(testfloor, entitypos, entityrot, false);
                floor2.transform.localEulerAngles = new Vector3(0, 90, 0);
                floor2.transform.localPosition = new Vector3(-1f, 0f, -3f);
                floor2?.Spawn();
                floor2.SetParent(entity);
                SpawnRefresh(floor2);

                floor3 = GameManager.server.CreateEntity(testfloor, entitypos, entityrot, false);
                floor3.transform.localEulerAngles = new Vector3(0, 90, 0);
                floor3.transform.localPosition = new Vector3(-1f, 0f, -6f);
                floor3?.Spawn();
                floor3.SetParent(entity);
                SpawnRefresh(floor3);

                floor4 = GameManager.server.CreateEntity(testfloor, entitypos, entityrot, false);
                floor4.transform.localEulerAngles = new Vector3(0, 90, 0);
                floor4.transform.localPosition = new Vector3(-1f, 0f, -9f);
                floor4?.Spawn();
                floor4.SetParent(entity);
                SpawnRefresh(floor4);

                floor5 = GameManager.server.CreateEntity(testfloor, entitypos, entityrot, false);
                floor5.transform.localEulerAngles = new Vector3(0, 90, 0);
                floor5.transform.localPosition = new Vector3(-1f, 0f, -12f);
                floor5?.Spawn();
                floor5.SetParent(entity);
                SpawnRefresh(floor5);
            }

            void SpawnLock()
            {
                string copterlockprefab = "assets/prefabs/locks/keypad/lock.code.prefab";

                boatlock = GameManager.server.CreateEntity(copterlockprefab, entitypos, entityrot, true);
                boatlock.transform.localEulerAngles = new Vector3(0, 0, 0);
                boatlock.transform.localPosition = new Vector3(0.4f, 0.8f, 1.2f);
                boatlock.OwnerID = ownerid;
                boatlock?.Spawn();
                boatlock.SetParent(entity, 0);
            }

            public void SpawnSideWalls()
            {
                string prefabtop = "assets/prefabs/building core/wall.low/wall.low.prefab";
                string prefabfloor = "assets/prefabs/building core/floor/floor.prefab";
                string prefabrudder = "assets/prefabs/deployable/signs/sign.post.single.prefab";

                wallfrontl = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wallfrontl.transform.localEulerAngles = new Vector3(0, -30, 0);
                wallfrontl.transform.localPosition = new Vector3(-0.25f, 0f, 2.8f);
                wallfrontl?.Spawn();
                wallfrontl.SetParent(entity);
                SpawnRefresh(wallfrontl);

                wallfrontr = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wallfrontr.transform.localEulerAngles = new Vector3(0, 210, 0);
                wallfrontr.transform.localPosition = new Vector3(-1.75f, 0f, 2.8f);
                wallfrontr?.Spawn();
                wallfrontr.SetParent(entity);
                SpawnRefresh(wallfrontr);

                wall1 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall1.transform.localEulerAngles = new Vector3(0, 180, 0);
                wall1.transform.localPosition = new Vector3(-2.5f, 0f, 0f);
                wall1?.Spawn();
                wall1.SetParent(entity);
                SpawnRefresh(wall1);

                wall1r = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall1r.transform.localEulerAngles = new Vector3(0, 0, 0);
                wall1r.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                wall1r?.Spawn();
                wall1r.SetParent(entity);
                SpawnRefresh(wall1r);

                wall2 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall2.transform.localEulerAngles = new Vector3(0, 180, 0);
                wall2.transform.localPosition = new Vector3(-2.5f, 0f, -3f);
                wall2?.Spawn();
                wall2.SetParent(entity);
                SpawnRefresh(wall2);

                wall2r = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall2r.transform.localEulerAngles = new Vector3(0, 0, 0);
                wall2r.transform.localPosition = new Vector3(0.5f, 0f, -3f);
                wall2r?.Spawn();
                wall2r.SetParent(entity);
                SpawnRefresh(wall2r);

                wall3 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall3.transform.localEulerAngles = new Vector3(0, 180, 0);
                wall3.transform.localPosition = new Vector3(-2.5f, 0f, -6f);
                wall3?.Spawn();
                wall3.SetParent(entity);
                SpawnRefresh(wall3);

                wall3r = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall3r.transform.localEulerAngles = new Vector3(0, 0, 0);
                wall3r.transform.localPosition = new Vector3(0.5f, 0f, -6f);
                wall3r?.Spawn();
                wall3r.SetParent(entity);
                SpawnRefresh(wall3r);

                wall4 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall4.transform.localEulerAngles = new Vector3(0, 180, 0);
                wall4.transform.localPosition = new Vector3(-2.5f, 0f, -9f);
                wall4?.Spawn();
                wall4.SetParent(entity);
                SpawnRefresh(wall4);

                wall4r = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall4r.transform.localEulerAngles = new Vector3(0, 0, 0);
                wall4r.transform.localPosition = new Vector3(0.5f, 0f, -9f);
                wall4r?.Spawn();
                wall4r.SetParent(entity);
                SpawnRefresh(wall4r);

                wall5 = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall5.transform.localEulerAngles = new Vector3(0, 180, 0);
                wall5.transform.localPosition = new Vector3(-2.5f, 0f, -12f);
                wall5?.Spawn();
                wall5.SetParent(entity);
                SpawnRefresh(wall5);

                wall5r = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wall5r.transform.localEulerAngles = new Vector3(0, 0, 0);
                wall5r.transform.localPosition = new Vector3(0.5f, 0f, -12f);
                wall5r?.Spawn();
                wall5r.SetParent(entity);
                SpawnRefresh(wall5r);

                wallback = GameManager.server.CreateEntity(prefabtop, entitypos, entityrot, false);
                wallback.transform.localEulerAngles = new Vector3(0, 90, 0);
                wallback.transform.localPosition = new Vector3(-1f, 0f, -13.5f);
                wallback?.Spawn();
                wallback.SetParent(entity);
                SpawnRefresh(wallback);

                rudder = GameManager.server.CreateEntity(prefabrudder, entitypos, entityrot, false);
                rudder.transform.localEulerAngles = new Vector3(0, 270, 130);
                rudder.transform.localPosition = new Vector3(-1f, 0.9f, -13f);
                var rudderstab = rudder.GetComponent<StabilityEntity>();
                if (rudderstab) rudderstab.grounded = true;
                rudder?.Spawn();
                rudder.SetParent(entity);
                SpawnRefresh(rudder);

            }

            void SpawnOars()
            {
                string prefabpillar = "assets/prefabs/deployable/signs/sign.post.single.prefab";

                oarright1 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarright1.transform.localEulerAngles = new Vector3(0, 0, 240);
                oarright1.transform.localPosition = new Vector3(0.5f, 0.75f, 0.7f);
                oarright1?.Spawn();
                oarright1.SetParent(entity);
                SpawnRefresh(oarright1);

                oarright2 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarright2.transform.localEulerAngles = new Vector3(0, 0, 240);
                oarright2.transform.localPosition = new Vector3(0.5f, 0.75f, -2.3f);
                oarright2?.Spawn();
                oarright2.SetParent(entity);
                SpawnRefresh(oarright2);

                oarright3 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarright3.transform.localEulerAngles = new Vector3(0, 0, 240);
                oarright3.transform.localPosition = new Vector3(0.5f, 0.75f, -5.3f);
                oarright3?.Spawn();
                oarright3.SetParent(entity);
                SpawnRefresh(oarright3);

                oarright4 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarright4.transform.localEulerAngles = new Vector3(0, 0, 240);
                oarright4.transform.localPosition = new Vector3(0.5f, 0.75f, -8.3f);
                oarright4?.Spawn();
                oarright4.SetParent(entity);
                SpawnRefresh(oarright4);

                oarright5 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarright5.transform.localEulerAngles = new Vector3(0, 0, 240);
                oarright5.transform.localPosition = new Vector3(0.5f, 0.75f, -11.3f);
                oarright5?.Spawn();
                oarright5.SetParent(entity);
                SpawnRefresh(oarright5);

                oarleft1 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarleft1.transform.localEulerAngles = new Vector3(0, 0, 120);
                oarleft1.transform.localPosition = new Vector3(-2.5f, 0.75f, 0.7f);
                oarleft1?.Spawn();
                oarleft1.SetParent(entity);
                SpawnRefresh(oarleft1);

                oarleft2 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarleft2.transform.localEulerAngles = new Vector3(0, 0, 120);
                oarleft2.transform.localPosition = new Vector3(-2.5f, 0.75f, -2.3f);
                oarleft2?.Spawn();
                oarleft2.SetParent(entity);
                SpawnRefresh(oarleft2);

                oarleft3 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarleft3.transform.localEulerAngles = new Vector3(0, 0, 120);
                oarleft3.transform.localPosition = new Vector3(-2.5f, 0.75f, -5.3f);
                oarleft3?.Spawn();
                oarleft3.SetParent(entity);
                SpawnRefresh(oarleft3);

                oarleft4 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarleft4.transform.localEulerAngles = new Vector3(0, 0, 120);
                oarleft4.transform.localPosition = new Vector3(-2.5f, 0.75f, -8.3f);
                oarleft4?.Spawn();
                oarleft4.SetParent(entity);
                SpawnRefresh(oarleft4);

                oarleft5 = GameManager.server.CreateEntity(prefabpillar, entitypos, entityrot, true);
                oarleft5.transform.localEulerAngles = new Vector3(0, 0, 120);
                oarleft5.transform.localPosition = new Vector3(-2.5f, 0.75f, -11.3f);
                oarleft5?.Spawn();
                oarleft5.SetParent(entity);
                SpawnRefresh(oarleft5);
            }

            void SpawnSails()
            {
                string prefabsail = "assets/prefabs/deployable/signs/sign.pole.banner.large.prefab";

                sailfront = GameManager.server.CreateEntity(prefabsail, entitypos, entityrot, true);
                sailfront.transform.localEulerAngles = new Vector3(0, 0, 0);
                sailfront.transform.localPosition = new Vector3(-1f, 0f, -2f);
                sailfront?.Spawn();
                sailfront.SetParent(entity);
                SpawnRefresh(sailfront);

                sailback = GameManager.server.CreateEntity(prefabsail, entitypos, entityrot, true);
                sailback.transform.localEulerAngles = new Vector3(0, 0, 0);
                sailback.transform.localPosition = new Vector3(-1f, 0f, -9f);
                sailback?.Spawn();
                sailback.SetParent(entity);
                SpawnRefresh(sailback);
            }

            public void SpawnPassengerChairs()
            {
                string prefabchair = "assets/prefabs/deployable/chair/chair.deployed.prefab";

                chairleft1 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairleft1.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairleft1.transform.localPosition = new Vector3(-2f, 0f, 0f);
                chairleft1?.Spawn();
                chairleft1.SetParent(entity);
                SpawnRefresh(chairleft1);

                chairleft2 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairleft2.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairleft2.transform.localPosition = new Vector3(-2f, 0f, -3);
                chairleft2?.Spawn();
                chairleft2.SetParent(entity);
                SpawnRefresh(chairleft2);

                chairleft3 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairleft3.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairleft3.transform.localPosition = new Vector3(-2f, 0f, -6);
                chairleft3?.Spawn();
                chairleft3.SetParent(entity);
                SpawnRefresh(chairleft3);

                chairleft4 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairleft4.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairleft4.transform.localPosition = new Vector3(-2f, 0f, -9);
                chairleft4?.Spawn();
                chairleft4.SetParent(entity);
                SpawnRefresh(chairleft4);

                chairleft5 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairleft5.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairleft5.transform.localPosition = new Vector3(-2f, 0f, -12);
                chairleft5?.Spawn();
                chairleft5.SetParent(entity);
                SpawnRefresh(chairleft5);

                chairright2 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairright2.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairright2.transform.localPosition = new Vector3(0f, 0f, -3);
                chairright2?.Spawn();
                chairright2.SetParent(entity);
                SpawnRefresh(chairright2);

                chairright3 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairright3.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairright3.transform.localPosition = new Vector3(0f, 0f, -6);
                chairright3?.Spawn();
                chairright3.SetParent(entity);
                SpawnRefresh(chairright3);

                chairright4 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairright4.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairright4.transform.localPosition = new Vector3(0f, 0f, -9);
                chairright4?.Spawn();
                chairright4.SetParent(entity);
                SpawnRefresh(chairright4);

                chairright5 = GameManager.server.CreateEntity(prefabchair, entitypos, entityrot, true);
                chairright5.transform.localEulerAngles = new Vector3(0, 0, 0);
                chairright5.transform.localPosition = new Vector3(0f, 0f, -12);
                chairright5?.Spawn();
                chairright5.SetParent(entity);
                SpawnRefresh(chairright5);

            }

            void SpawnRefresh(BaseNetworkable entity1)
            {
                var hasstab = entity1.GetComponent<StabilityEntity>();
                if (hasstab)
                {
                    hasstab.grounded = true;
                }
                var hasmount = entity1.GetComponent<BaseMountable>();
                if (hasmount)
                {
                    hasmount.isMobile = true;
                }
                var hasblock = entity1.GetComponent<BuildingBlock>();
                if (hasblock)
                {
                    hasblock.SetGrade(BuildingGrade.Enum.Wood);
                    hasblock.SetHealthToMax();
                    hasblock.UpdateSkin();
                    hasblock.ClientRPC(null, "RefreshSkin");
                }
            }

            bool hitSomething(Vector3 position)
            {
                var directioncheck = new Vector3();
                if (moveforward) directioncheck = position + (transform.forward * 4);
                if (movebackward) directioncheck = position - (transform.forward * 8);
                if (GamePhysics.CheckSphere(directioncheck, 1f, UnityEngine.LayerMask.GetMask("World", "Construction", "Default"), 0)) return true;
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

            void SplashEffect()
            {
                Effect.server.Run("assets/content/vehicles/boats/effects/splashloop.prefab", wall5.transform.position);
                Effect.server.Run("assets/content/vehicles/boats/effects/splashloop.prefab", wall5r.transform.position);
            }

            void CalculateSpeed()
            {
                if (chairleft1.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairleft2.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairleft3.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairleft4.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairleft5.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (entity.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairright2.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairright3.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairright4.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
                if (chairright5.GetComponent<BaseMountable>().IsMounted()) steps = steps + 1;
            }

            void FixedUpdate()
            {
                if (!ismoving && !(moveforward || movebackward || rotright || rotleft)) return;
                if (!PlayerIsMounted()) { ResetMovement(); RefreshAll(); ismoving = false; return; }
                var currentloc = entity.transform.position;
                waterheight = TerrainMeta.WaterMap.GetHeight(currentloc);
                startloc = new Vector3(currentloc.x, waterheight, currentloc.z);
                startrot = entity.transform.eulerAngles;
                startrot.x = 0f;
                if (rotright) rotdirection = new Vector3(startrot.x, startrot.y + 1, startrot.z);
                else if (rotleft) rotdirection = new Vector3(startrot.x, startrot.y - 1, startrot.z);
                CalculateSpeed();
                if (moveforward) endloc = startloc + (transform.forward * steps) * Time.deltaTime;
                else if (movebackward) endloc = startloc + (transform.forward * -steps) * Time.deltaTime;

                if (hitSomething(endloc)) { endloc = startloc; ResetMovement(); RefreshAll(); return; }
                if (!isStillInWater(endloc)) { endloc = startloc; ResetMovement(); RefreshAll(); return; }
                if (endloc.x >= 3900 || endloc.x <= -3900 || endloc.z >= 3900 || endloc.z <= -3900) { endloc = startloc; ResetMovement(); RefreshAll(); return; }

                if (endloc == new Vector3(0f, 0f, 0f)) endloc = startloc;

                MoveOars();

                entity.transform.eulerAngles = rotdirection;
                entity.transform.localPosition = endloc;
                if (ShowWaterSplash) SplashEffect();
                RefreshAll();
                steps = DefaultVikingBoatMovementSpeed;
            }

            void MoveOars()
            {
                var rotorpos1r = oarright1.transform.eulerAngles;
                if (rotorpos1r.x >= 80f) rotorpos1r.x = 0f;
                oarright1.transform.eulerAngles = new Vector3(rotorpos1r.x + 5f, rotorpos1r.y, rotorpos1r.z);

                var rotorpos1l = oarleft1.transform.eulerAngles;
                if (rotorpos1l.x >= 80f) rotorpos1l.x = 0f;
                oarleft1.transform.eulerAngles = new Vector3(rotorpos1l.x + 5f, rotorpos1l.y, rotorpos1l.z);

                var rotorpos2r = oarright2.transform.eulerAngles;
                if (rotorpos2r.x >= 80f) rotorpos2r.x = 0f;
                oarright2.transform.eulerAngles = new Vector3(rotorpos2r.x + 5f, rotorpos2r.y, rotorpos2r.z);

                var rotorpos2l = oarleft2.transform.eulerAngles;
                if (rotorpos2l.x >= 80f) rotorpos2l.x = 0f;
                oarleft2.transform.eulerAngles = new Vector3(rotorpos2l.x + 5f, rotorpos2l.y, rotorpos2l.z);

                var rotorpos3r = oarright3.transform.eulerAngles;
                if (rotorpos3r.x >= 80f) rotorpos3r.x = 0f;
                oarright3.transform.eulerAngles = new Vector3(rotorpos3r.x + 5f, rotorpos3r.y, rotorpos3r.z);

                var rotorpos3l = oarleft3.transform.eulerAngles;
                if (rotorpos3l.x >= 80f) rotorpos3l.x = 0f;
                oarleft3.transform.eulerAngles = new Vector3(rotorpos3l.x + 5f, rotorpos3l.y, rotorpos3l.z);

                var rotorpos4r = oarright4.transform.eulerAngles;
                if (rotorpos4r.x >= 80f) rotorpos4r.x = 0f;
                oarright4.transform.eulerAngles = new Vector3(rotorpos4r.x + 5f, rotorpos4r.y, rotorpos4r.z);

                var rotorpos4l = oarleft4.transform.eulerAngles;
                if (rotorpos4l.x >= 80f) rotorpos4l.x = 0f;
                oarleft4.transform.eulerAngles = new Vector3(rotorpos4l.x + 5f, rotorpos4l.y, rotorpos4l.z);

                var rotorpos5r = oarright5.transform.eulerAngles;
                if (rotorpos5r.x >= 80f) rotorpos5r.x = 0f;
                oarright5.transform.eulerAngles = new Vector3(rotorpos5r.x + 5f, rotorpos5r.y, rotorpos5r.z);

                var rotorpos5l = oarleft5.transform.eulerAngles;
                if (rotorpos5l.x >= 80f) rotorpos5l.x = 0f;
                oarleft5.transform.eulerAngles = new Vector3(rotorpos5l.x + 5f, rotorpos5l.y, rotorpos5l.z);
            }

            void ResetMovement()
            {
                ismoving = false;
                moveforward = false;
                movebackward = false;
                rotright = false;
                rotleft = false;
            }

            public void RefreshAll()
            {
                if (!PlayerIsMounted()) { ResetMovement(); return; }
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
                        var isblock = entity.children[i].GetComponent<BuildingBlock>() ?? null;
                        if (isblock != null)
                        {
                            isblock.UpdateSkin();
                            isblock.ClientRPC(null, "RefreshSkin");
                        }
                        var hasmount = entity.children[i].GetComponent<BaseMountable>() ?? null;
                        if (hasmount != null)
                        {
                            hasmount.isMobile = true;
                        }
                        entity.children[i].SendNetworkUpdateImmediate(false);
                        entity.children[i].UpdateNetworkGroup();
                    }
            }

            public void OnDestroy()
            {
                if (hasVikingBoat.ContainsKey(ownerid)) hasVikingBoat.Remove(ownerid);
                if (entity != null && !entity.IsDestroyed) { entity.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

        #region Configuration

        static float DefaultVikingBoatMovementSpeed = 4f;
        bool OnlyOneActiveVikingBoat = true;
        static int MaterialsForVikingBoat = 10000;
        static int MaterialID = -151838493;
        bool BlockDecay = true;
        static bool ShowWaterSplash = false;

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
            CheckCfg("Effect - Show water splash effect when moving ? ", ref ShowWaterSplash);
            CheckCfg("Decay - Block all decay damage to Viking Boat ? ", ref BlockDecay);
            CheckCfg("Usage - Only 1 Active VikingBoat per player ? ", ref OnlyOneActiveVikingBoat);
            CheckCfg("Materials - VikingBoat- Amount of Wood needed to build : ", ref MaterialsForVikingBoat);
            CheckCfg("Materials - Item ID of material needed (default is wood) : ", ref MaterialID);
            CheckCfgFloat("Speed - Default VikingBoat Movement Speed : ", ref DefaultVikingBoatMovementSpeed);
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
            ["boatlocked"] = "Boat is locked, you cannot access it !!!",
            ["hasVikingBoatalready"] = "You already have a VikingBoat in the world !!!",
            ["captain"] = "You are now the Captain of this boat !!!",
            ["alreadyadded"] = "That part is already installed !!!",
            ["missingmaterials"] = "You are missing the required materials to uprade to that !! ",
            ["endofworld"] = "Movement blocked !!! You are at the end of the playable world !!!",
            ["notstandingwater"] = "You must be in deeper water but NOT swimming to build a VikingBoat !!"
        };

        #endregion

    }
}