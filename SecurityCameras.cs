//Requires: RustNET
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Rust;
using Facepunch;
using Network;

namespace Oxide.Plugins
{
    [Info("SecurityCameras", "k1lly0u", "0.2.28")]
    [Description("Deploy security cameras around your base and view them by accessing a RustNET terminal")]
    class SecurityCameras : RustPlugin
    {
        #region Fields
        private StoredData storedData;
        private DynamicConfigFile data;

        private static SecurityCameras ins;
        private static LinkManager linkManager;
        private static int layerPlcmnt;

        private List<CameraManager> cameraManagers = new List<CameraManager>();
       
        private bool wipeDetected;
        private bool isInitialized;

        const string permUse = "securitycameras.use";
        const string permIgnore = "securitycameras.ignorelimit";
        const string permPublic = "securitycameras.public";

        const string SCUI_Overlay = "SCUI_Overlay";

        const string CHAIR_PREFAB = "assets/prefabs/deployable/chair/chair.deployed.prefab";
        const int CAMERA_ID = 634478325;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permIgnore, this);
            permission.RegisterPermission(permPublic, this);

            foreach (string key in configData.Camera.Max.Keys)
            {
                if (permission.PermissionExists(key, this))
                    continue;
                permission.RegisterPermission(key, this);
            }

            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("RustNET/cameras");

            linkManager = new LinkManager();
        }

        private void OnServerInitialized()
        {
            ins = this; 
            LoadData(); 
            LoadDefaultImages(); 
            layerPlcmnt = LayerMask.GetMask("Construction", "Default", "Deployed", "World", "Terrain");

            if (wipeDetected)
            {
                storedData = new StoredData();
                SaveData();
            }

            InvokeHandler.Invoke(ServerMgr.Instance, InitializeAllLinks, 10f);
            RustNET.RegisterModule(Title, this);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
            {
                LinkManager.CameraLink link = linkManager.GetLinkOf(controller);
                if (link != null)
                    link.CloseLink(controller);
            }
        }

        private object OnPlayerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player != null && player.GetComponent<Controller>())
                return false;
            return null;
        }

        private object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
                return false;
            return null;
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            if (networkable != null && (networkable is BuildingBlock || networkable is DroppedItem))
                linkManager.OnEntityDeath(networkable);
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            if (entity is BasePlayer)
            {
                if (entity.GetComponent<Controller>())
                {
                    info.damageTypes = new DamageTypeList();
                    info.HitEntity = null;
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                }
            }
            else if (entity is BaseMountable)                            
                linkManager.OnEntityTakeDamage(entity, info);            
        }

        private void OnNewSave(string filename) => wipeDetected = true;

        private void OnServerSave()
        {
            if (isInitialized)
                SaveData();
        }
                       
        private object CanDismountEntity(BasePlayer player, BaseMountable mountable)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
                return false;
            return null;
        }

        private void Unload()
        {
            if (InvokeHandler.IsInvoking(ServerMgr.Instance, InitializeAllLinks))            
                InvokeHandler.CancelInvoke(ServerMgr.Instance, InitializeAllLinks);

            if (!ServerMgr.Instance.Restarting)
                SaveData();

            foreach (BasePlayer player in BasePlayer.activePlayerList)            
                CuiHelper.DestroyUi(player, SCUI_Overlay);
            
            linkManager.DestroyAllLinks(true);

            for (int i = cameraManagers.Count - 1; i >= 0; i--)
                cameraManagers[i].Destroy();

            ins = null;
            linkManager = null;
        }
        #endregion

        #region Functions
        private bool CanPlaceItem(BasePlayer player, bool ignoreRestriction)
        {
            int cameraCount = 0;

            BuildingManager.Building building = player.GetBuildingPrivilege()?.GetBuilding();
            if (building != null)
            {
                RustNET.LinkManager.Link buildingLink = RustNET.linkManager.GetLinkOf(building);
                if (buildingLink == null && !ignoreRestriction)
                {
                    SendReply(player, msg("Error.NoTerminal", player.userID));
                    return false;
                }
               
                cameraCount = linkManager.GetLinkOf(building)?.managers.Count ?? 0;
            }
            else
            {
                if (!ignoreRestriction)
                {
                    SendReply(player, msg("Error.NoBuildingPrivilege", player.userID));
                    return false;
                }
            }

            int cameraLimit = GetMaxCameras(player.userID);
            if (!permission.UserHasPermission(player.UserIDString, permIgnore) && cameraCount >= cameraLimit)
            {
                SendReply(player, msg("Error.Limit", player.userID));
                return false;
            }

            return true;
        }

        private void InitializeAllLinks()
        {
            for (int i = 0; i < storedData.cameraData.Length; i++)
            {
                CameraManager.CameraData cameraData = storedData.cameraData.ElementAt(i);

                int terminalId = cameraData.terminalId;
                BuildingManager.Building building = null;

                RustNET.LinkManager.Link link = RustNET.linkManager.GetLinkOf(cameraData.terminalId);
                if (link == null || !link.IsPublicLink)
                {
                    building = RustNET.GetBuilding(new Vector3(cameraData.position[0], cameraData.position[1], cameraData.position[2]), 0.5f);
                    if (building == null) 
                        continue;
                    else terminalId = RustNET.linkManager.GetLinkOf(building)?.terminal?.terminalId ?? 0;                    
                }

                LinkManager.CameraLink cameraLink = linkManager.GetLinkOf(terminalId);
                if (cameraLink == null)
                    cameraLink = new LinkManager.CameraLink(terminalId, building, cameraData);
                else cameraLink.AddCameraToLink(cameraData);
            }
            isInitialized = true;
        }
       
        private CameraManager InitializeCamera(CameraManager.CameraData cameraData, int terminalId)
        {
            CameraManager camera = RustNET.SpawnDroppedItem(CAMERA_ID, new Vector3(cameraData.position[0], cameraData.position[1], cameraData.position[2]), new Vector3(cameraData.baseRotation[0], cameraData.baseRotation[1], cameraData.baseRotation[2])).gameObject.AddComponent<CameraManager>();
            camera.SetRotation(new float[] { cameraData.baseRotation[0], cameraData.baseRotation[1], cameraData.baseRotation[2] });
            camera.terminalId = terminalId;
            camera.cameraName = cameraData.cameraName;
            return camera;
        }

        private void InitializeCamera(BasePlayer player, DroppedItem droppedItem)
        {
            BuildingManager.Building building = RustNET.GetBuilding(droppedItem.transform.position, 0.5f);
            if (building == null)
            {
                droppedItem.DestroyItem();
                droppedItem.Kill();
                player.GiveItem(ItemManager.CreateByItemID(CAMERA_ID), BaseEntity.GiveItemReason.PickedUp);
                SendReply(player, msg("Error.InvalidPlacement", player.userID));
                return;
            }

            int terminalId = RustNET.linkManager.GetLinkOf(building)?.terminal?.terminalId ?? 0;

            CameraManager camera = RustNET.SpawnDroppedItem(CAMERA_ID, droppedItem.transform.position, droppedItem.transform.eulerAngles).gameObject.AddComponent<CameraManager>();

            camera.SetRotation(new float[] { droppedItem.transform.eulerAngles.x, droppedItem.transform.eulerAngles.y, droppedItem.transform.eulerAngles.z });
            camera.terminalId = terminalId;

            LinkManager.CameraLink cameraLink = linkManager.GetLinkOf(terminalId);
            if (cameraLink == null)
                cameraLink = new LinkManager.CameraLink(terminalId, building, camera);
            else cameraLink.AddCameraToLink(camera);
        }

        private void InitializeCamera(BasePlayer player, DroppedItem droppedItem, int terminalId)
        {            
            CameraManager camera = RustNET.SpawnDroppedItem(CAMERA_ID, droppedItem.transform.position, droppedItem.transform.eulerAngles).gameObject.AddComponent<CameraManager>();

            camera.SetRotation(new float[] { droppedItem.transform.eulerAngles.x, droppedItem.transform.eulerAngles.y, droppedItem.transform.eulerAngles.z });
            camera.terminalId = terminalId;

            LinkManager.CameraLink cameraLink = linkManager.GetLinkOf(terminalId);
            if (cameraLink == null)
                cameraLink = new LinkManager.CameraLink(terminalId, null, camera);
            else cameraLink.AddCameraToLink(camera);
        }

        private int GetMaxCameras(ulong playerId)
        {
            int max = 0;
            foreach(var entry in configData.Camera.Max)
            {
                if (permission.UserHasPermission(playerId.ToString(), entry.Key))
                {
                    if (max < entry.Value)
                        max = entry.Value;
                }                    
            }
            return max;
        }
        #endregion

        #region RustNET Integration
        private void DestroyAllLinks() => linkManager.DestroyAllLinks(true);

        private void OnTerminalCreated(RustNET.TerminalManager terminal)
        {
            BuildingManager.Building building = terminal.parentEntity?.GetBuilding();

            if (building == null)
                return;

            for (int i = cameraManagers.Count - 1; i >= 0; i--)
            {
                CameraManager camera = cameraManagers[i];
                if (camera.parent?.GetBuilding() == building)
                {
                    LinkManager.CameraLink link = linkManager.GetLinkOf(terminal.terminalId);
                    if (link == null)                    
                        link = new LinkManager.CameraLink(terminal.terminalId, building, camera);                    
                    else link.AddCameraToLink(camera);                    
                }
            }            
        }

        private void OnTerminalRemoved(int terminalId)
        {
            LinkManager.CameraLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
            {
                for (int i = cameraManagers.Count - 1; i >= 0; i--)
                {
                    CameraManager camera = cameraManagers[i];
                    if (camera.terminalId == terminalId)                    
                        camera.terminalId = 0;  
                }

                linkManager.links.Remove(link);
            }           
        }

        private void OnLinkShutdown(int terminalId)
        {
            LinkManager.CameraLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.OnLinkTerminated(false);
        }

        private void OnLinkDestroyed(int terminalId)
        {
            LinkManager.CameraLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.OnLinkTerminated(true);
        }

        private CameraManager[] GetAvailableCameras(int terminalId)
        {
            LinkManager.CameraLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)           
                return link.managers.Where(x => x != null && x.camera != null).ToArray();            
            return new CameraManager[0];
        }
        
        private void InitializeController(BasePlayer player, uint managerId, int terminalId)
        {
            LinkManager.CameraLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.InitiateLink(player, managerId);
        }

        private string GetHelpString(ulong playerId, bool title)
        {
            if (title)
                return msg("UI.Help.Title", playerId);
            else
            {
                string message = msg("UI.Help", playerId);
                if (permission.UserHasPermission(playerId.ToString(), permPublic))
                    message += msg("UI.Help.Public", playerId);
                return message;
            }
        }

        private bool AllowPublicAccess() => true;
        #endregion

        #region Components
        private class LinkManager
        {
            public List<CameraLink> links = new List<CameraLink>();

            public CameraLink GetLinkOf(BuildingManager.Building building) => links.Find(x => x.building == building) ?? null;

            public CameraLink GetLinkOf(CameraManager camera) => links.Find(x => x.managers.Contains(camera)) ?? null;

            public CameraLink GetLinkOf(Controller controller) => links.Find(x => x.controllers.Contains(controller)) ?? null;

            public CameraLink GetLinkOf(int terminalId) => links.Find(x => x.terminalId == terminalId) ?? null;

            public CameraLink GetLinkOf(DroppedItem camera)
            {
                CameraManager component = camera.GetComponent<CameraManager>();
                if (component == null)
                    return null;
                return GetLinkOf(component);
            }
           
            public void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
            {
                for (int i = links.Count - 1; i >= 0; i--)
                    links.ElementAt(i).OnEntityTakeDamage(entity, info);
            }

            public void OnEntityDeath(BaseNetworkable networkable)
            {
                for (int i = links.Count - 1; i >= 0; i--)
                    links.ElementAt(i).OnEntityDeath(networkable);
            }

            public void DestroyAllLinks(bool shutdown)
            {
                foreach (CameraLink link in links)
                {
                    link.OnLinkTerminated(false);
                    if (shutdown)
                        link.DestroyCameraManagers();
                }
            }

            public class CameraLink
            {
                public int terminalId { get; set; }
                public BuildingManager.Building building { get; private set; }
                public List<Controller> controllers { get; private set; }
                public List<CameraManager> managers { get; private set; }

                public CameraLink() { }
                public CameraLink(int terminalId, BuildingManager.Building building, CameraManager camera)
                {
                    this.terminalId = terminalId;
                    this.building = building;
                    this.controllers = new List<Controller>();
                    this.managers = new List<CameraManager>();

                    AddCameraToLink(camera);
                    linkManager.links.Add(this);
                }

                public CameraLink(int terminalId, BuildingManager.Building building, CameraManager.CameraData cameraData)
                {
                    this.terminalId = terminalId;
                    this.building = building;
                    this.controllers = new List<Controller>();
                    this.managers = new List<CameraManager>();

                    AddCameraToLink(cameraData);

                    linkManager.links.Add(this);
                }

                public void AddCameraToLink(CameraManager camera)
                {                    
                    managers.Add(camera);
                    if (!ins.cameraManagers.Contains(camera))
                        ins.cameraManagers.Add(camera);
                }

                public void AddCameraToLink(CameraManager.CameraData cameraData)
                {
                    CameraManager camera = ins.InitializeCamera(cameraData, terminalId);
                    managers.Add(camera);
                    if (!ins.cameraManagers.Contains(camera))
                        ins.cameraManagers.Add(camera);
                }

                public void InitiateLink(BasePlayer player, uint managerId)
                {                   
                    CameraManager manager = managers.FirstOrDefault(x => x.camera.net.ID == managerId);
                    if (manager != null)
                    {
                        player.inventory.crafting.CancelAll(true);
                        Controller controller = player.gameObject.AddComponent<Controller>();
                        controllers.Add(controller);
                        controller.InitiateLink(terminalId);
                        controller.SetCameraLink(this);
                        controller.SetSpectateTarget(managers.IndexOf(manager));
                    }
                }

                public void CloseLink(Controller controller, bool isDead = false)
                {
                    if (controller != null)
                    {                        
                        controllers.Remove(controller);                        
                        controller.FinishSpectating(isDead);
                    }
                }

                public void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
                {
                    foreach (Controller controller in controllers)
                    {
                        if (controller != null && entity == controller.mountPoint)
                        {
                            info.damageTypes = new DamageTypeList();
                            info.HitEntity = null;
                            info.HitMaterial = 0;
                            info.PointStart = Vector3.zero;
                        }
                    }
                }

                public void OnEntityDeath(BaseNetworkable networkable)
                {
                    CameraManager manager = networkable.GetComponent<CameraManager>();
                    if (manager != null && managers.Contains(manager))
                    {
                        if (manager.controller != null)
                        {
                            manager.controller.player.ChatMessage(ins.msg("Warning.CameraDestroyed", manager.controller.player.userID));
                            CloseLink(manager.controller);
                        }
                        OnCameraDestroyed(manager);
                        return;
                    }

                    BuildingBlock buildingBlock = networkable.GetComponent<BuildingBlock>();
                    if (buildingBlock != null)
                    {
                        for (int i = managers.Count - 1; i >= 0; i--)
                        {
                            CameraManager camera = managers.ElementAt(i);
                            if (camera.parent == buildingBlock)
                            {
                                if (camera.controller != null)
                                {
                                    camera.controller.player.ChatMessage(ins.msg("Warning.CameraDestroyed", camera.controller.player.userID));
                                    CloseLink(camera.controller);
                                }

                                OnCameraDestroyed(camera);
                            }
                        }
                    }
                }

                public void OnLinkTerminated(bool isDestroyed)
                {
                    for (int i = controllers.Count - 1; i >= 0; i--)
                    {
                        Controller controller = controllers.ElementAt(i);
                        controller.player.ChatMessage(isDestroyed ? ins.msg("Warning.TerminalDestroyed", controller.player.userID) : ins.msg("Warning.TerminalShutdown", controller.player.userID));
                        CloseLink(controller);
                    }
                }

                public void OnCameraDestroyed(CameraManager manager)
                {
                    ins.cameraManagers.Remove(manager);
                    managers.Remove(manager);
                    UnityEngine.Object.Destroy(manager);

                    if (managers.Count == 0)
                    {
                        linkManager.links.Remove(this);
                        return;
                    }

                    for (int i = managers.Count - 1; i >= 0; i--)
                    {
                        CameraManager m = managers.ElementAt(i);
                        if (m == null || m.camera == null)
                            managers.RemoveAt(i);
                    }
                }

                public void DestroyCameraManagers()
                {
                    foreach (CameraManager manager in managers)                    
                        UnityEngine.Object.Destroy(manager);
                    
                    managers.Clear();
                }
            }
        }
        
        class CameraManager : MonoBehaviour
        {            
            public DroppedItem camera { get; private set; }
            public Controller controller { get; private set; }

            public BuildingBlock parent { get; private set; }
            public Vector3 baseRotation { get; set; }
            public int terminalId { get; set; }
            public string cameraName { get; set; }

            private void Awake()
            {
                camera = GetComponent<DroppedItem>();
                enabled = false;
                baseRotation = camera.transform.eulerAngles;

                OBB obb = camera.WorldSpaceBounds();
                List<BuildingBlock> list = Pool.GetList<BuildingBlock>();
                Vis.Entities<BuildingBlock>(obb.position, 0.5f + obb.extents.magnitude, list, 2097152, QueryTriggerInteraction.Collide);

                if (list.Count > 0)
                {
                    parent = list[0];
                    ins.cameraManagers.Add(this);
                    terminalId = RustNET.linkManager.GetLinkOf(parent.GetBuilding())?.terminal?.terminalId ?? 0;
                }              
                Pool.FreeList<BuildingBlock>(ref list);               
            }  
            
            private void OnDestroy()
            {
                if (camera != null)
                {
                    camera.DestroyItem();
                    if (!camera.IsDestroyed)
                        camera.Kill();
                }
            }

            public void Destroy() => Destroy(this);

            public void SetRotation(float[] rotation)
            {
                baseRotation = new Vector3(rotation[0], rotation[1], rotation[2]);
                camera.transform.rotation = Quaternion.Euler(baseRotation);
            }

            public void SetController(Controller controller) => this.controller = controller;
            
            public CameraData GetCameraData() => new CameraData(this);
                       
            public class CameraData
            {
                public float[] position;
                public float[] baseRotation;
                public string cameraName;
                public int terminalId;

                public CameraData() { }

                public CameraData(CameraManager camera)
                {
                    position = new float[] { camera.camera.transform.position.x, camera.camera.transform.position.y, camera.camera.transform.position.z };
                    baseRotation = new float[] { camera.baseRotation.x, camera.baseRotation.y, camera.baseRotation.z };
                    cameraName = camera.cameraName;
                    terminalId = camera.terminalId;
                }
            }
        }

        class Controller : RustNET.Controller
        {
            public CameraManager manager { get; private set; }
            public BaseMountable mountPoint { get; private set; }
            private LinkManager.CameraLink link;
            
            private int spectateIndex;
            private bool switchingTargets;
            private bool canCyle;

            public override void Awake()
            {
                base.Awake();
                enabled = false;

                canCyle = ins.configData.Camera.CanCycle;

                mountPoint = GameManager.server.CreateEntity(CHAIR_PREFAB, player.transform.position) as BaseMountable;
                mountPoint.enableSaving = false;
                mountPoint.skinID = (ulong)1169930802; // 1311472987
                mountPoint.isMobile = true;
                mountPoint.Spawn();

                Destroy(mountPoint.GetComponent<DestroyOnGroundMissing>());
                Destroy(mountPoint.GetComponent<GroundWatch>());
                Destroy(mountPoint.GetComponent<MeshCollider>());                
            }

            public override void OnDestroy()
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.DismountObject();
                player.EnsureDismounted();

                if (mountPoint != null && !mountPoint.IsDestroyed)
                    mountPoint.Kill();

                base.OnDestroy();
            }

            private void Update()
            {
                if (player == null || player.serverInput == null || switchingTargets)
                    return;

                InputState input = player.serverInput;
               
                if (manager.controller == this)
                {                   
                    Vector3 aimAngle = player.serverInput.current.aimAngles;
                    manager.camera.transform.rotation = Quaternion.Euler(aimAngle.x, aimAngle.y, 0) * Quaternion.Euler(0, 90, 0);                    
                }

                if (input.WasJustPressed(BUTTON.USE))
                {
                    enabled = false;
                    link.CloseLink(this);
                }
                else
                {
                    if (canCyle)
                    {
                        if (input.WasJustPressed(BUTTON.JUMP))
                            UpdateSpectateTarget(1);
                        else if (input.WasJustPressed(BUTTON.DUCK))
                            UpdateSpectateTarget(-1);
                    }
                }
            }

            public void SetCameraLink(LinkManager.CameraLink link)
            {
                this.link = link;
                BeginSpectating();
            }

            public void BeginSpectating()
            {
                RustNET.StripInventory(player);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.MountObject(mountPoint);

                if (Net.sv.write.Start())
                {
                    Net.sv.write.PacketID(Message.Type.EntityDestroy);
                    Net.sv.write.EntityID(player.net.ID);
                    Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                    Net.sv.write.Send(new SendInfo(player.net.group.subscribers.Where(x => x.userid != player.userID).ToList()));
                }

                player.limitNetworking = true;
                player.syncPosition = false;

                if (canCyle)
                    player.ChatMessage(ins.msg("Help.ControlInfo", player.userID));
                else player.ChatMessage(ins.msg("Help.ControlInfo.NoCycle", player.userID));
            }

            public void FinishSpectating(bool isDead)
            {
                enabled = false;
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.limitNetworking = false;
                player.syncPosition = true;

                InvokeHandler.CancelInvoke(player, UpdateNetworkGroup);

                player.DismountObject();
                player.EnsureDismounted();
                CuiHelper.DestroyUi(player, SCUI_Overlay);

                if (!isDead)
                    Destroy(this);
            }

            public void SetSpectateTarget(int spectateIndex)
            {
                this.spectateIndex = spectateIndex;
                manager = link.managers[spectateIndex];

                enabled = true;
                
                mountPoint.transform.position = manager.camera.transform.position + (Vector3.down * 1.5f) + (-manager.camera.transform.right * 0.2f);
                mountPoint.transform.rotation = Quaternion.Euler(manager.baseRotation.x, manager.baseRotation.y, 0) * Quaternion.Euler(0, 270, 0);
                mountPoint.SendNetworkUpdate();

                UpdateNetworkGroup();

                if (manager.controller == null)                
                    manager.SetController(this);                
                else player.ChatMessage(ins.msg("Warning.InUse", player.userID));                

                CreateCameraOverlay();                
            }

            private void UpdateNetworkGroup()
            {
                Network.Visibility.Group group = Net.sv.visibility.GetGroup(manager.camera.transform.position);
                if (mountPoint.net.group != group)                
                    mountPoint.net.SwitchGroup(group);                    
                
                if (player.net.group != group)
                    player.net.SwitchGroup(group);
            }

            public void UpdateSpectateTarget(int index = 0)
            {
                switchingTargets = true;
                player.Invoke(() => switchingTargets = false, 0.25f);

                int newIndex = spectateIndex + index;

                if (newIndex > link.managers.Count - 1)
                    newIndex = 0;
                else if (newIndex < 0)
                    newIndex = link.managers.Count - 1;

                if (spectateIndex == newIndex)
                    return;

                if (manager.controller == this)
                    manager.SetController(null);
                manager = null;
                SetSpectateTarget(newIndex);
            }

            public override void OnPlayerDeath(HitInfo info)
            {
                enabled = false;
                link.CloseLink(this, true);

                base.OnPlayerDeath(info);
            }

            private void CreateCameraOverlay()
            {
                if (!ins.configData.Camera.Overlay)
                    return;

                CuiElementContainer container = RustNET.UI.Container("0 0 0 0", "0 0", "1 1", false, "Under", SCUI_Overlay);
                RustNET.UI.Image(ref container, ins.GetImage("camoverlay"), "0 0", "1 1", SCUI_Overlay);
                RustNET.UI.Panel(ref container, "0 0 0 0.4", "0.04 0.9", "0.18 0.94", SCUI_Overlay);
                RustNET.UI.Label(ref container, "<color=red>REC</color>", 18, "0.04 0.9", "0.18 0.94", TextAnchor.MiddleCenter, SCUI_Overlay);

                RustNET.UI.Panel(ref container, "0 0 0 0.4", "0.82 0.9", "0.96 0.94", SCUI_Overlay);
                RustNET.UI.Label(ref container, string.IsNullOrEmpty(manager.cameraName) ? string.Format(ins.msg("UI.CameraName", player.userID), spectateIndex + 1) : manager.cameraName, 18, "0.82 0.9", "0.96 0.94", TextAnchor.MiddleCenter, SCUI_Overlay);

                CuiHelper.DestroyUi(player, SCUI_Overlay);
                CuiHelper.AddUi(player, container);
            }           
        }
        #endregion

        #region UI
        private void CreateConsoleWindow(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = RustNET.ins.GetBaseContainer(player, terminalId, Title);

            CameraManager[] entityIds = GetAvailableCameras(terminalId);

            RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], "0.04 0.765", "0.96 0.8");
            RustNET.UI.Label(ref container, msg("UI.Select.Camera", player.userID), 12, "0.05 0.765", "0.5 0.8", TextAnchor.MiddleLeft);
            RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.MainMenu", player.userID), 11, "0.82 0.765", "0.96 0.8", $"rustnet.changepage {terminalId}");

            if (entityIds == null || entityIds.Length == 0)            
                RustNET.UI.Label(ref container, msg("UI.NoCameras", player.userID), 12, "0.05 0.5", "0.95 0.7");            
            else
            {
                int count = 0;
                int startAt = page * 18;
                for (int i = startAt; i < (startAt + 18 > entityIds.Length ? entityIds.Length : startAt + 18); i++)
                {
                    CameraManager manager = entityIds.ElementAt(i);
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                    RustNET.UI.Label(ref container, string.IsNullOrEmpty(manager.cameraName) ? string.Format(msg("UI.Camera", player.userID), i + 1) : $"> {manager.cameraName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.31 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);                   
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Control", player.userID), 11, $"0.76 {0.725f - (count * 0.04f)}", $"0.96 {0.755f - (count * 0.04f)}", $"securitycameras.control {manager.camera.net.ID} {terminalId}");

                    count++;
                }

                int totalPages = entityIds.Length / 18;

                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"rustnet.changepage {terminalId} {Title} {page - 1}" : "");
                RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"rustnet.changepage {terminalId} {Title} {page + 1}" : "");
            }

            CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("securitycameras.control")]
        private void ccmdControl(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!RustNET.linkManager.IsValidTerminal(arg.GetInt(1)))
            {
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                SendReply(player, RustNET.msg("Warning.TerminalDestroyed", player.userID));
                return;
            }

            CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
            InitializeController(player, arg.GetUInt(0), arg.GetInt(1));            
        }
        #endregion

        #region Commands
        [ChatCommand("sc")]
        private void cmdSC(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return;
            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>{Title}</color><color=#939393>  v{Version}  -</color> <color=#ce422b>{Author} @ www.chaoscode.io</color>");
                SendReply(player, msg("Help.Main", player.userID));
                SendReply(player, msg("Help.Add", player.userID));

                if (permission.UserHasPermission(player.UserIDString, permPublic))
                    SendReply(player, msg("Help.AddPublic", player.userID));

                SendReply(player, msg("Help.Remove", player.userID));
                SendReply(player, msg("Help.Name", player.userID));
                return;
            }

            if (configData.Camera.RequirePrivilege && !player.IsBuildingAuthed() &&!player.IsAdmin)
            {
                SendReply(player, msg("Error.NoPrivilege", player.userID));
                return;
            }
                   
            switch (args[0].ToLower())
            {
                case "add":
                    {
                        bool isPublic = permission.UserHasPermission(player.UserIDString, permPublic) && args.Length > 2 && args[1].ToLower() == "public";

                        Item activeItem = player.GetActiveItem();
                        if (activeItem == null || activeItem.info.itemid != CAMERA_ID)
                        {
                            SendReply(player, msg("Error.NoCamera", player.userID));
                            return;
                        }

                        int cameraCount = 0;

                        BuildingManager.Building building = player.GetBuildingPrivilege()?.GetBuilding();
                        if (building != null)
                        {
                            RustNET.LinkManager.Link buildingLink = RustNET.linkManager.GetLinkOf(building);
                            if (buildingLink == null && !isPublic)
                            {
                                SendReply(player, msg("Error.NoTerminal", player.userID));
                                return;
                            }
                          
                            cameraCount = linkManager.GetLinkOf(building)?.managers.Count ?? 0;
                        }
                        else
                        {
                            if (!isPublic)
                            {
                                SendReply(player, msg("Error.NoBuildingPrivilege", player.userID));
                                return;
                            }
                        }

                        int cameraLimit = GetMaxCameras(player.userID); 
                        if (!permission.UserHasPermission(player.UserIDString, permIgnore) && cameraCount >= cameraLimit)
                        {
                            SendReply(player, msg("Error.Limit", player.userID));
                            return;
                        }                        

                        if (isPublic)
                        {
                            int terminalId;
                            if (!int.TryParse(args[2], out terminalId))
                            {
                                SendReply(player, msg("Error.TerminalID", player.userID));
                                return;
                            }

                            if (!RustNET.linkManager.IsValidTerminal(terminalId))
                            {
                                SendReply(player, msg("Error.RustNETID", player.userID));
                                return;
                            }

                            RustNET.ItemPlacement placement = player.gameObject.AddComponent<RustNET.ItemPlacement>();
                            placement.terminalId = terminalId;
                            placement.SetRequiredItem(CAMERA_ID, new Vector3(0, 90, 0), new Vector3(0, 0, -0.1f), true, true, null, InitializeCamera, this);
                        }
                        else
                        {
                            RustNET.ItemPlacement placement = player.gameObject.AddComponent<RustNET.ItemPlacement>();
                            placement.SetRequiredItem(CAMERA_ID, new Vector3(0, 90, 0), new Vector3(0, 0, -0.1f), true, false, InitializeCamera, null, this);
                        }

                        SendReply(player, msg("Placement.Enabled", player.userID));
                    }
                    return;
                case "remove":
                    {
                        CameraManager manager = RustNET.FindEntityFromRay(player)?.GetComponent<CameraManager>();
                        if (manager == null)
                        {
                            SendReply(player, msg("Error.NoEntity", player.userID));
                            return;
                        }

                        if (manager.terminalId != 0)
                        {
                            RustNET.LinkManager.Link rustNetLink = RustNET.linkManager.GetLinkOf(manager.terminalId);
                            if (rustNetLink != null && rustNetLink.IsPublicLink)
                            {
                                if (!permission.UserHasPermission(player.UserIDString, permPublic))
                                {
                                    SendReply(player, msg("Error.PublicLink", player.userID));
                                    return;
                                }
                            }
                        }

                        LinkManager.CameraLink link = linkManager.GetLinkOf(manager);
                        if (link != null)
                            link.OnCameraDestroyed(manager);
                        else UnityEngine.Object.Destroy(manager);
                       
                        player.GiveItem(ItemManager.CreateByItemID(CAMERA_ID), BaseEntity.GiveItemReason.PickedUp);
                        SendReply(player, msg("Placement.Removed", player.userID));
                    }
                    return;
                case "name":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, msg("Error.NoNameSpecified", player.userID));
                            return;
                        }

                        CameraManager manager = RustNET.FindEntityFromRay(player)?.GetComponent<CameraManager>();
                        if (manager == null)
                        {
                            SendReply(player, msg("Error.NoEntity", player.userID));
                            return;
                        }

                        manager.cameraName = args[1];
                        
                        SendReply(player, string.Format(msg("Success.NameSet", player.userID), args[1]));
                    }
                    return;
                default:
                    SendReply(player, msg("Error.InvalidCommand", player.userID));
                    return;
            }
        }
        #endregion
       
        #region Image Management
        private void LoadDefaultImages(int attempts = 0)
        {
            if (attempts > 3)
            {
                PrintError("ImageLibrary not found. Unable to load camera overlay UI");
                configData.Camera.Overlay = false;
                return;
            }

            if (configData.Camera.Overlay && !string.IsNullOrEmpty(configData.Camera.OverlayImage))    
                AddImage("camoverlay", configData.Camera.OverlayImage);

            if (!string.IsNullOrEmpty(configData.Camera.RustNETIcon))
                AddImage(Title, configData.Camera.RustNETIcon);            
        }

        private void AddImage(string imageName, string fileName) => RustNET.ins.AddImage(imageName, fileName);

        private string GetImage(string name) => RustNET.ins.GetImage(name);
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {            
            [JsonProperty(PropertyName = "Camera Options")]
            public CameraOptions Camera { get; set; }

            public class CameraOptions
            {
                [JsonProperty(PropertyName = "Allow players to cycle through all linked cameras")]
                public bool CanCycle { get; set; }
                [JsonProperty(PropertyName = "Allow friends and clan members to place/remove cameras")]
                public bool Friends { get; set; }
                [JsonProperty(PropertyName = "Require building privilege to place/remove cameras")]
                public bool RequirePrivilege { get; set; }
                [JsonProperty(PropertyName = "Maximum allowed cameras per base (Permission | Amount)")]
                public Dictionary<string, int> Max { get; set; }
                [JsonProperty(PropertyName = "Camera placement and removal distance")]
                public int Distance { get; set; }
                [JsonProperty(PropertyName = "Display camera overlay UI")]
                public bool Overlay { get; set; }
                [JsonProperty(PropertyName = "Camera overlay image URL")]
                public string OverlayImage { get; set; }
                [JsonProperty(PropertyName = "Camera icon URL for RustNET menu")]
                public string RustNETIcon { get; set; }
            }           
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Camera = new ConfigData.CameraOptions
                {
                    CanCycle = true,
                    Distance = 4,
                    Friends = true,
                    Max = new Dictionary<string, int>
                    {
                        ["securitycameras.use"] = 4,
                        ["securitycameras.pro"] = 10
                    },
                    Overlay = true,
                    OverlayImage = "http://www.rustedit.io/images/RustNET/camera.png",
                    RustNETIcon = "http://www.rustedit.io/images/RustNET/cameraicon.png"
                },                
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 05))
                configData.Camera.Max = baseConfig.Camera.Max;

            if (configData.Version < new VersionNumber(0, 2, 0))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(0, 2, 10))
            {
                configData.Camera.Overlay = baseConfig.Camera.Overlay;
                configData.Camera.OverlayImage = baseConfig.Camera.OverlayImage;
                configData.Camera.RustNETIcon = baseConfig.Camera.RustNETIcon;
            }

            if (configData.Version < new VersionNumber(0, 2, 14))
                configData.Camera.CanCycle = baseConfig.Camera.CanCycle;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            storedData.cameraData = cameraManagers.Where(x => x != null).Select(x => x.GetCameraData()).ToArray();
            data.WriteObject(storedData);
        }

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public CameraManager.CameraData[] cameraData = new CameraManager.CameraData[0];
        }        
        #endregion

        #region Localization
        string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Main"] = "<color=#ce422b>/rustnet</color><color=#939393> - Display the help menu for using RustNET</color>",
            ["Help.Add"] = "<color=#ce422b>/sc add</color><color=#939393> - Activates the camera placement tool. Requires a camera in your hands!</color>",
            ["Help.AddPublic"] = "<color=#ce422b>/sc add public <terminal ID></color><color=#939393> - Activates the public camera placement tool. These cameras can be placed anywhere and need to be registered to a specific terminal</color>",
            ["Help.Remove"] = "<color=#ce422b>/sc remove</color><color=#939393> - Remove the camera you are looking at</color>",
            ["Help.Name"] = "<color=#ce422b>/sc name <name></color><color=#939393> - Set a name for the camera you are looking at</color>",
            ["Help.ControlInfo"] = "<color=#939393>Press <color=#ce422b>'JUMP'</color> and <color=#ce422b>'DUCK'</color> to cycle through available cameras.\nPress <color=#ce422b>'USE'</color> to exit the controller!</color>",
            ["Help.ControlInfo.NoCycle"] = "Press <color=#ce422b>'USE'</color> to exit the controller!</color>",
            ["Warning.TerminalDestroyed"] = "<color=#ce422b>The terminal has been destroyed!</color>",
            ["Warning.TerminalShutdown"] = "<color=#ce422b>The terminal has been shutdown</color>",
            ["Warning.InUse"] = "<color=#ce422b>This camera is currently in use</color>",
            ["Warning.CameraDestroyed"] = "<color=#ce422b>This camera has been destroyed!</color>",
            ["Placement.Enabled"] = "<color=#939393>You have <color=#ce422b>enabled</color> the camera placement tool!</color>",
            ["Placement.Removed"] = "<color=#939393>You have <color=#ce422b>removed</color> this security camera!</color>",
            ["Error.NoPrivilege"] = "<color=#ce422b>You require tool cupboard access to use these commands!</color>",
            ["Error.Limit"] = "<color=#939393>This building already has the maximum number of security cameras!</color>",
            ["Error.NoBuilding"] = "<color=#939393>The camera controller needs to be attached to the building the cameras are on</color>",
            ["Error.InvalidCommand"] = "<color=#939393>Invalid command! Type <color=#ce422b>/sc</color> for available commands</color>",
            ["Error.NoEntity"] = "<color=#939393>You are not looking at a security camera</color>",            
            ["Error.InvalidPlacement"] = "<color=#ce422b>Invalid camera placement!</color> <color=#939393>Unable to find a neighbouring building block</color>",            
            ["Error.NoCamera"] = "<color=#939393>You must have a camera in your hands to activate the placement tool!</color>",
            ["Error.NoTerminal"] = "<color=#939393>The building requires a terminal to place a camera!</color>",
            ["Error.NoBuildingPrivilege"] = "<color=#939393>You must be inside building privilege to place a camera!</color>",
            ["Error.NoNameSpecified"] = "<color=#939393>You must enter a name for the camera!</color>",
            ["Error.TerminalID"] = "<color=#939393>You need to enter a valid terminal ID</color>",
            ["Error.RustNETID"] = "<color=#939393>Invalid terminal ID selected! You can find the terminal ID by opening the terminal</color>",
            ["Error.PublicLink"] = "<color=#939393>You do not have permission to remove public cameras</color>",
            ["Success.NameSet"] = "<color=#939393>You have set the name of this camera to <color=#ce422b>{0}</color></color>",
            ["UI.Select.Camera"] = "> <color=#28ffa6>Cameras</color> <",
            ["UI.NoCameras"] = "No cameras registered to this terminal",
            ["UI.Camera"] = "> Camera {0}",
            ["UI.CameraName"] = "Camera {0}",
            ["UI.Help.Title"] = "> <color=#28ffa6>Camera Help Menu</color> <",
            ["UI.Help"] = "> Creating a security camera\nStep 1. Place a camera item in your hands.\nStep 2. Type <color=#28ffa6>/sc add</color>. The camera placement tool will now be activated. You can place the camera by pressing the <color=#28ffa6>FIRE</color> key, or cancel placement by pressing the <color=#28ffa6>AIM</color> key.\n\n> Cameras can only be placed on building blocks.\n> If the building block the camera is placed on is destroyed, the camera will also be destroyed.\n> Once you place a camera it will be registered to the terminal of the base that you placed it on\n> You can access any camera attached to your base by interacting with the RustNET terminal.\n\n> Removing a camera\nTo remove a camera look at it and type <color=#28ffa6>/sc remove</color>. This will remove the camera and give you a camera item back to be placed elsewhere.",
            ["UI.Help.Public"] = "\n\n> Creating a public security camera\nPublic cameras can be placed anywhere and accessed by anyone, although they should NOT be placed on building blocks or player bases.\nYou will require a public terminal already placed somewhere in the world and its terminal ID to create a public camera.\n\nStep 1. Place a camera item in your hands.\nStep 2. Type <color=#28ffa6>/sc add public <terminal ID></color>. The ID is the 4 digit number displayed when you open the terminal.\nYou can place the camera by pressing the <color=#28ffa6>FIRE</color> key, or cancel placement by pressing the <color=#28ffa6>AIM</color> key.",
        };
        #endregion       
    }
}
