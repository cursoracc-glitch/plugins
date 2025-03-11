//Requires: RustNET
using Facepunch;
using Rust;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("AutomatedSearchlights", "k1lly0u", "0.2.30")]
    [Description("Create searchlights that will automatically turn on and follow a variety of objects")]
    class AutomatedSearchlights : RustPlugin
    {
        #region Fields
        private StoredData storedData;
        private DynamicConfigFile data;

        private static AutomatedSearchlights ins;
        private static LinkManager linkManager;
        private static int ctrlLayerMask;

        private FrameBudgeter frameBudgeter;

        private bool wipeDetected;
        private bool automationEnabled;
        private bool isInitialized;
        private bool nlConsume;

        private const string PERM_USE = "automatedsearchlights.use";
        private const string PERM_IGNORE = "automatedsearchlights.ignorelimit";

        private const string FX_OFFLINE = "assets/prefabs/npc/autoturret/effects/offline.prefab";
        private const string FX_ONLINE = "assets/prefabs/npc/autoturret/effects/online.prefab";
        private const string FX_ACQUIRED = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab";
        private const string FX_LOST = "assets/prefabs/npc/autoturret/effects/targetlost.prefab";

        private const string ASUI_OVERLAY = "ASUI_Overlay";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission(PERM_USE, this);
            permission.RegisterPermission(PERM_IGNORE, this);
            foreach (string key in configData.Management.Max.Keys)
            {
                if (permission.PermissionExists(key, this))
                    continue;
                permission.RegisterPermission(key, this);
            }

            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("RustNET/searchlights");

            linkManager = new LinkManager();
        }

        private void OnServerInitialized()
        {
            ins = this;

            frameBudgeter = new GameObject("AutomatedSearchlights.FrameBudgeter").AddComponent<FrameBudgeter>();
            LightManager.allManagers = new List<LightManager>();

            LoadData();

            ctrlLayerMask = LayerMask.GetMask("Default", "Water", "Deployed", "AI", "Player_Movement", "Vehicle_Movement", "World", "Player_Server", "Construction", "Terrain", "Clutter", "Debris", "Tree");
                        
            if (wipeDetected)
            {
                storedData = new StoredData();
                SaveData();
            }

            MonitorTime(true);

            LoadDefaultImages();

            InvokeHandler.Invoke(ServerMgr.Instance, InitializeAllLinks, 10f);

            RustNET.RegisterModule(Title, this);

            if (!configData.Management.AutoRegister)
                Unsubscribe(nameof(OnEntitySpawned));
        }

        private void OnNewSave(string filename) => wipeDetected = true;

        private void OnServerSave() => SaveData();

        private void OnEntitySpawned(SearchLight searchLight)
        {
            if (searchLight == null)
                return;

            BasePlayer owner = BasePlayer.FindByID(searchLight.OwnerID);
            if (owner == null)
                return;

            BuildingManager.Building building = RustNET.GetBuilding(searchLight);
            if (building == null)
                return;

            if (!building.GetDominatingBuildingPrivilege()?.IsAuthed(owner) ?? false)            
                return;

            RustNET.LinkManager.Link link = RustNET.linkManager.GetLinkOf(building);
            if (link == null)            
                return;            

            if (Vector3.Distance(searchLight.transform.position, link.terminal.DroppedItem.transform.position) > configData.Management.DistanceFromTerminal)            
                return;            

            LinkManager.LightLink lightLink = linkManager.GetLinkOf(link.terminal.TerminalID);
            if (lightLink == null)
                lightLink = new LinkManager.LightLink(link.terminal.TerminalID, searchLight, "");
            else
            {
                int lightLimit = GetMaxLights(owner.userID);
                if (!permission.UserHasPermission(owner.UserIDString, PERM_IGNORE) && lightLink.Managers.Count >= lightLimit)                
                    return;                

                lightLink.AddLightToLink(searchLight, link.terminal.TerminalID, "");
            }

            searchLight.GetComponent<LightManager>().ToggleAutomation(automationEnabled);

            SendReply(owner, msg("Success.Set", owner.userID));
            SaveData();
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            if (networkable != null)
                linkManager.OnEntityDeath(networkable);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
            {
                LinkManager.LightLink link = linkManager.GetLinkOf(controller);
                if (link != null)
                    link.CloseLink(controller);
            }
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
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

        private void Unload()
        {
            UnityEngine.Object.Destroy(frameBudgeter.gameObject);

            if (InvokeHandler.IsInvoking(ServerMgr.Instance, InitializeAllLinks))
                InvokeHandler.CancelInvoke(ServerMgr.Instance, InitializeAllLinks);

            if (isInitialized)
                SaveData();

            linkManager.DestroyAllLinks();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, ASUI_OVERLAY);

            LightManager.allManagers?.Clear();
            LightManager.allManagers = null;

            ins = null;
            linkManager = null;
        }
        #endregion

        #region Functions
        private void InitializeAllLinks()
        {
            for (int i = storedData.registeredSearchlights.Length - 1; i >= 0; i--)
            {
                LightManager.LightData lightData = storedData.registeredSearchlights.ElementAt(i);

                SearchLight searchLight = BaseEntity.serverEntities.Find(lightData.lightId) as SearchLight;
                if (searchLight == null || (lightData.terminalId != 0 && !RustNET.linkManager.IsValidTerminal(lightData.terminalId)))                
                    continue;                

                LinkManager.LightLink link = linkManager.GetLinkOf(lightData.terminalId);
                if (link == null)
                    link = new LinkManager.LightLink(lightData.terminalId, searchLight, lightData.lightName);
                else link.AddLightToLink(searchLight, lightData.terminalId, lightData.lightName);
            }
            MonitorTime(true);
            isInitialized = true;
        }

        private void MonitorTime(bool firstLoad = false)
        {
            if (!configData.Options.NightOnly)
            {
                automationEnabled = true;
                return;
            }

            float currentTime = TOD_Sky.Instance.Cycle.Hour;

            bool isNight = (currentTime > 0 && currentTime < configData.Options.Sunrise) || currentTime > configData.Options.Sunset;

            if (automationEnabled != isNight || firstLoad)
            {
                automationEnabled = isNight;
                ServerMgr.Instance.StartCoroutine(ToggleAllLights(automationEnabled));
            }
            timer.In(10, ()=> MonitorTime());
        }

        private IEnumerator ToggleAllLights(bool status)
        {
            foreach(LinkManager.LightLink link in linkManager.links)
            {
                foreach(LightManager manager in link.Managers)
                {
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.2f));

                    if (manager == null)
                        continue;

                    manager.ToggleAutomation(status);
                }
            }            
        }       
             
        private int GetMaxLights(ulong playerId)
        {
            int max = 0;
            foreach (var entry in configData.Management.Max)
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
        private void DestroyAllLinks() => linkManager.DestroyAllLinks();        

        private void OnLinkShutdown(int terminalId)
        {
            LinkManager.LightLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.OnLinkTerminated(false);
        }

        private void OnLinkDestroyed(int terminalId)
        {
            LinkManager.LightLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.OnLinkTerminated(true);
        }

        private LightManager[] GetAvailableSearchlights(int terminalId)
        {
            LinkManager.LightLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                return link.Managers.ToArray();
            return new LightManager[0];
        }

        private bool IsEntityEnabled(int terminalId, uint managerId)
        {
            LinkManager.LightLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
            {
                LightManager manager = link.Managers.Find(x => x.Searchlight.net.ID == managerId);
                if (manager != null)
                    return manager.IsEnabled();
            }
            return false;
        }

        private void InitializeController(BasePlayer player, uint managerId, int terminalId)
        {
            LinkManager.LightLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.InitiateLink(player, managerId);
        }

        private void ToggleAutomation(uint managerId, int terminalId, bool active)
        {
            LinkManager.LightLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
            {
                LightManager manager = link.Managers.Find(x => x.Searchlight.net.ID == managerId);
                if (manager != null)
                {
                    manager.ToggleSearchAutomation(active);
                }
            }
        }

        private string GetHelpString(ulong playerId, bool title) => title ? msg("UI.Help.Title", playerId) : msg("UI.Help", playerId);

        private bool AllowPublicAccess() => true;
        #endregion

        #region Component
        private class LinkManager
        {
            public List<LightLink> links = new List<LightLink>();

            public LightLink GetLinkOf(LightManager manager) => links.Find(x => x.Managers.Contains(manager)) ?? null;

            public LightLink GetLinkOf(Controller controller) => links.Find(x => x.Controllers.Contains(controller)) ?? null;

            public LightLink GetLinkOf(int terminalId) => links.Find(x => x.TerminalID == terminalId) ?? null;

            public LightLink GetLinkOf(SearchLight searchLight)
            {
                LightManager component = searchLight.GetComponent<LightManager>();
                if (component == null)
                    return null;
                return GetLinkOf(component);
            }

            public void OnEntityDeath(BaseNetworkable networkable)
            {
                for (int i = links.Count - 1; i >= 0; i--)
                    links.ElementAt(i).OnEntityDeath(networkable);
            }

            public void DestroyAllLinks()
            {
                foreach (LightLink link in links)
                    link.OnLinkTerminated(false);
            }

            public class LightLink
            {
                public int TerminalID { get; private set; }
                public List<Controller> Controllers { get; private set; }
                public List<LightManager> Managers { get; private set; }

                public LightLink() { }
                public LightLink(int terminalId, SearchLight searchLight, string lightName)
                {
                    this.TerminalID = terminalId;
                    this.Controllers = new List<Controller>();
                    this.Managers = new List<LightManager>();

                    AddLightToLink(searchLight, terminalId, lightName);
                    linkManager.links.Add(this);
                }

                public void AddLightToLink(SearchLight searchLight, int terminalId, string lightName)
                {
                    LightManager manager = searchLight.gameObject.AddComponent<LightManager>();
                    manager.lightName = lightName;
                    manager.terminalId = terminalId;
                    Managers.Add(manager);
                }

                public void InitiateLink(BasePlayer player, uint managerId)
                {
                    LightManager manager = Managers.FirstOrDefault(x => x.Searchlight.net.ID == managerId);
                    if (manager != null)
                    {
                        player.inventory.crafting.CancelAll(true);
                        Controller controller = player.gameObject.AddComponent<Controller>();
                        Controllers.Add(controller);
                        controller.InitiateLink(TerminalID);
                        controller.SetLightLink(this);
                        controller.SetSpectateTarget(Managers.IndexOf(manager));
                    }
                }

                public void CloseLink(Controller controller, bool isDead = false)
                {
                    if (controller != null)
                    {                        
                        Controllers.Remove(controller);
                        controller.FinishSpectating(isDead);
                    }
                }

                public void OnEntityDeath(BaseNetworkable networkable)
                {
                    LightManager manager = networkable.GetComponent<LightManager>();
                    if (manager != null && Managers.Contains(manager))
                    {
                        if (manager.Controller != null)
                        {
                            manager.Controller.player.ChatMessage(ins.msg("Warning.SearchlightDestroyed", manager.Controller.player.userID));
                            CloseLink(manager.Controller);
                        }
                        Managers.Remove(manager);
                        return;
                    }

                    if (networkable.GetComponent<BaseCombatEntity>())
                    {
                        foreach (LightManager lightManager in Managers)
                            lightManager.OnEntityDeath(networkable as BaseCombatEntity);
                    }
                }

                public void OnLinkTerminated(bool isDestroyed)
                {
                    for (int i = Controllers.Count - 1; i >= 0; i--)
                    {
                        Controller controller = Controllers.ElementAt(i);
                        controller.player.ChatMessage(isDestroyed ? ins.msg("Warning.TerminalDestroyed", controller.player.userID) : ins.msg("Warning.TerminalShutdown", controller.player.userID));
                        CloseLink(controller);
                    }

                    DestroyLightManagers(isDestroyed);
                }

                private void DestroyLightManagers(bool isDestroyed)
                {
                    foreach (LightManager manager in Managers)
                    {                        
                        UnityEngine.Object.Destroy(manager);
                    }
                }
            }
        }

        private class FrameBudgeter : MonoBehaviour
        {
            private double maxMilliseconds = 0.2;

            private Stopwatch sw = Stopwatch.StartNew();

            private int lastIndex = 0;

            private void Update()
            {
                sw.Reset();
                sw.Start();

                int count = LightManager.allManagers?.Count ?? 0;
                if (lastIndex >= count)
                    lastIndex = 0;

                for (int i = lastIndex; i < count; i++)
                {
                    if (sw.Elapsed.TotalMilliseconds > maxMilliseconds)
                    {
                        lastIndex = i;
                        return;
                    }

                    LightManager.allManagers[i].DoUpdate();
                }

                lastIndex = 0;
            }
        }

        private class LightManager : MonoBehaviour
        {
            internal static List<LightManager> allManagers;

            public SearchLight Searchlight { get; private set; }
            public Controller Controller { get; private set; }

            private SphereCollider collider;
            private Rigidbody rb;

            private BaseCombatEntity targetEntity;
            private float maxTargetDistance;
            private int threatLevel = 10;

            private float searchRadius;
            private bool requiresPower;
            private ConfigData.LightOptions.FlickerMode flicker;
            private ConfigData.LightOptions.SearchMode search;
            private ConfigData.DetectionOptions detection;

            private Hash<int, HashSet<BaseCombatEntity>> threats;
            private Hash<int, float> threatRadius;

            private float[] visabilityOffsets = new float[] { 0, 0.2f, -0.2f };

            private float nextCheckTime;
            private float threatProbabilityRate;

            private float idleTime = 0;
            private float lostTargetTime = 0;

            private Vector3[] searchPattern = new Vector3[9];
            private int lastSearchPoint = 0;
            private int nextSearchPoint = 1;
            private bool searchForwards;
         
            private float searchTime;
            private bool resetSearch = true;
            private bool isSearching;

            public string lightName;
            public int terminalId;

            public bool isDisabled;            

            private void Awake()
            {
                Searchlight = GetComponent<SearchLight>();

                threatProbabilityRate = 3f + UnityEngine.Random.Range(0.1f, 1.0f);

                InitializeSettings();

                allManagers.Add(this);
            }

            internal void DoUpdate()
            {
                if (Searchlight.IsMounted() || Searchlight.HasFlag(BaseEntity.Flags.Reserved5) || !Searchlight.IsPowered())                
                    return;

                if (Controller != null && !Controller.lightsOn)
                    return;

                if (!flicker.Enabled)
                {
                    if (!Searchlight.IsOn())
                        Searchlight.SetFlag(BaseEntity.Flags.On, true);
                }
                else
                {
                    float healthPercent = Searchlight.health / Searchlight.MaxHealth();
                    if (healthPercent * 100 <= flicker.Health)
                    {
                        if (healthPercent * 100 <= flicker.Health / 2)
                            Searchlight.SetFlag(BaseEntity.Flags.On, UnityEngine.Random.Range(1, 5) != 2);
                        else Searchlight.SetFlag(BaseEntity.Flags.On, UnityEngine.Random.Range(1, 10) != 2);
                    }
                    else
                    {
                        if (!Searchlight.IsOn())
                            Searchlight.SetFlag(BaseEntity.Flags.On, true);
                    }
                }

                if (Controller != null)
                    return;
                else
                {
                    nextCheckTime += Time.deltaTime;
                    if (nextCheckTime > threatProbabilityRate)
                    {
                        CalculateThreatProbability();
                        nextCheckTime = 0;
                    }

                    if (targetEntity == null)
                    {
                        if (search.Enabled)
                        {
                            idleTime += Time.deltaTime;

                            if (idleTime > 3f)
                            {
                                if (isSearching)
                                {
                                    searchTime = searchTime + Time.deltaTime;
                                    var single = Mathf.InverseLerp(0f, search.Speed / 9, searchTime);

                                    Searchlight.SetTargetAimpoint(Vector3.Lerp(searchPattern[lastSearchPoint], searchPattern[nextSearchPoint], single));
                                    Searchlight.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                                    if (single >= 1)
                                    {
                                        searchTime = 0;
                                        SetNextSearchPoint();
                                    }
                                }
                                else if (resetSearch)
                                {
                                    lastSearchPoint = 4;
                                    nextSearchPoint = searchForwards ? 5 : 3;
                                    searchTime = 0;
                                    Searchlight.SetTargetAimpoint(searchPattern[4]);
                                    Searchlight.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                                    resetSearch = false;
                                    isSearching = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (IsObjectVisible(targetEntity) && Vector3.Distance(Searchlight.transform.position, targetEntity.transform.position) <= maxTargetDistance)
                        {
                            Searchlight.SetTargetAimpoint(targetEntity.transform.position);
                            Searchlight.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            lostTargetTime = 0;
                        }
                        else
                        {
                            lostTargetTime += Time.deltaTime;
                            if (lostTargetTime > 3f)
                                ClearAimTarget();
                        }
                    }
                }
            }
                        
            private void OnDestroy()
            {
                Destroy(collider);
                Destroy(rb);

                allManagers?.Remove(this);
            }
              
            private void OnTriggerEnter(Collider col)
            {
                BaseCombatEntity potentialTarget = col.gameObject?.GetComponentInParent<BaseCombatEntity>();
                if (potentialTarget == null || potentialTarget.IsDestroyed)
                    return;

                float threatDistance = Vector3.Distance(Searchlight.transform.position, potentialTarget.transform.position);

                if (potentialTarget is BasePlayer)
                {
                    if (RustNET.IsFriendlyPlayer(Searchlight.OwnerID, potentialTarget.ToPlayer().userID))
                    {
                        if (detection.Friends.Enabled && threatDistance <= detection.Friends.Radius)
                        {
                            if (threats[detection.Friends.Threat].Contains(potentialTarget))
                                return;
                            threats[detection.Friends.Threat].Add(potentialTarget);
                        }
                    }
                    else if (detection.Enemies.Enabled && threatDistance <= detection.Enemies.Radius)
                    {
                        if (threats[detection.Enemies.Threat].Contains(potentialTarget))
                            return;
                        threats[detection.Enemies.Threat].Add(potentialTarget);
                    }
                }
                else if (potentialTarget is BaseNpc)
                {
                    if (detection.Animals.Enabled && threatDistance <= detection.Animals.Radius)
                    {
                        if (threats[detection.Animals.Threat].Contains(potentialTarget))
                            return;
                        threats[detection.Animals.Threat].Add(potentialTarget);
                    }
                }
                else if (potentialTarget is BasicCar || potentialTarget is BradleyAPC)
                {
                    if (detection.Vehicles.Enabled && threatDistance <= detection.Vehicles.Radius)
                    {
                        if (threats[detection.Vehicles.Threat].Contains(potentialTarget))
                            return;
                        threats[detection.Vehicles.Threat].Add(potentialTarget);
                    }
                }
                else if (potentialTarget is BaseHelicopter)
                {
                    if (detection.Helicopters.Enabled && threatDistance <= detection.Helicopters.Radius)
                    {
                        if (threats[detection.Helicopters.Threat].Contains(potentialTarget))
                            return;
                        threats[detection.Helicopters.Threat].Add(potentialTarget);
                    }
                }                
            }

            private void OnTriggerExit(Collider col)
            {
                BaseCombatEntity potentialTarget = col.gameObject.GetComponent<BaseCombatEntity>();
                if (potentialTarget == null)
                    return;

                foreach(var threatType in threats)
                {
                    if (threatType.Value.Contains(potentialTarget))
                    {
                        threatType.Value.Remove(potentialTarget);
                        break;
                    }
                }

                if (potentialTarget == targetEntity)
                    ClearAimTarget();
            }

            private void InitializeSettings()
            {
                requiresPower = ins.configData.Options.RequirePower;
                flicker = ins.configData.Options.Flicker;
                search = ins.configData.Options.Search;
                detection = ins.configData.Detection;

                searchRadius = detection.Animals.Radius;
                if (detection.Enemies.Radius > searchRadius)
                    searchRadius = detection.Enemies.Radius;
                if (detection.Friends.Radius > searchRadius)
                    searchRadius = detection.Friends.Radius;
                if (detection.Helicopters.Radius > searchRadius)
                    searchRadius = detection.Helicopters.Radius;
                if (detection.Vehicles.Radius > searchRadius)
                    searchRadius = detection.Vehicles.Radius;

                if (search.Enabled)
                    GenerateSearchPattern();

                threats = new Hash<int, HashSet<BaseCombatEntity>>
                {
                    [detection.Animals.Threat] = new HashSet<BaseCombatEntity>(),
                    [detection.Enemies.Threat] = new HashSet<BaseCombatEntity>(),
                    [detection.Friends.Threat] = new HashSet<BaseCombatEntity>(),
                    [detection.Helicopters.Threat] = new HashSet<BaseCombatEntity>(),
                    [detection.Vehicles.Threat] = new HashSet<BaseCombatEntity>()
                };

                threatRadius = new Hash<int, float>
                {
                    [detection.Animals.Threat] = detection.Animals.Radius,
                    [detection.Enemies.Threat] = detection.Enemies.Radius,
                    [detection.Friends.Threat] = detection.Friends.Radius,
                    [detection.Helicopters.Threat] = detection.Helicopters.Radius,
                    [detection.Vehicles.Threat] = detection.Vehicles.Radius
                };

                rb = Searchlight.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                collider = Searchlight.gameObject.AddComponent<SphereCollider>();
                collider.gameObject.layer = (int)Layer.Reserved1;
                collider.radius = searchRadius;
                collider.isTrigger = true;
            }

            private void GenerateSearchPattern()
            {
                int count = 0;
                for (int i = -40; i <= 40; i += 10)
                {
                    searchPattern[count] = Searchlight.transform.position + (Searchlight.transform.rotation * (new Vector3(Mathf.Cos(i * Mathf.Deg2Rad), -0.3f, Mathf.Sin(i * Mathf.Deg2Rad)) * 30));
                    count++;
                }
            }

            private void SetNextSearchPoint()
            {
                if (nextSearchPoint == 0 || nextSearchPoint == 8)
                    searchForwards = !searchForwards;

                lastSearchPoint = nextSearchPoint;
                nextSearchPoint = searchForwards ? nextSearchPoint += 1 : nextSearchPoint -= 1;
            }

            private void CalculateThreatProbability()
            {
                BaseCombatEntity target = null;
                float targetDistance = searchRadius;
                int threatRating = 10;

                foreach (var list in threats.Where(x => x.Key <= threatLevel && x.Value.Count > 0).OrderByDescending(x => x.Key))
                {
                    foreach (BaseCombatEntity potentialTarget in list.Value)
                    {
                        if (potentialTarget == null || potentialTarget.IsDestroyed || !IsObjectVisible(potentialTarget))
                            continue;

                        float distance = Vector3.Distance(Searchlight.transform.position, potentialTarget.transform.position);

                        if (distance < threatRadius[list.Key])
                        {
                            target = potentialTarget;
                            targetDistance = distance;
                            threatRating = list.Key;
                        }
                    }
                }

                if (target == targetEntity) 
                    return;                

                if (target != null)
                {
                    if (targetEntity != null)
                    {
                        if (threatRating < threatLevel || (threatRating == threatLevel && targetDistance < Vector3.Distance(Searchlight.transform.position, target.transform.position)))
                            SetAimTarget(target, threatRating);
                    }
                    else SetAimTarget(target, threatRating);
                }
            }

            private void SetAimTarget(BaseCombatEntity target, int threatRating)
            {
                nextCheckTime = 0;
                idleTime = 0;
                isSearching = false;
                targetEntity = target;

                if (Searchlight.HasFlag(BaseEntity.Flags.On))
                    Effect.server.Run(FX_ACQUIRED, Searchlight, 0, Vector3.zero, Vector3.zero, null, false);
                maxTargetDistance = target is BasePlayer ? (RustNET.IsFriendlyPlayer(Searchlight.OwnerID, targetEntity.ToPlayer().userID) ? detection.Friends.Radius : detection.Enemies.Radius) : target is BaseNpc ? detection.Animals.Radius : (target is BasicCar || target is BradleyAPC) ? detection.Vehicles.Radius : detection.Helicopters.Radius;
                threatLevel = threatRating;
            }

            private void ClearAimTarget()
            {
                resetSearch = true;
                if (targetEntity != null && Searchlight.HasFlag(BaseEntity.Flags.On))
                    Effect.server.Run(FX_LOST, Searchlight, 0, Vector3.zero, Vector3.zero, null, false);
                targetEntity = null;
                threatLevel = 10;
            }

            private bool IsObjectVisible(BaseCombatEntity obj)
            {               
                List<RaycastHit> list = Pool.GetList<RaycastHit>();

                Vector3 castPoint = Searchlight.transform.position + (Vector3.up * 1.4f);
                Vector3 aimPoint = AimOffset(obj);                
                Vector3 direction = aimPoint - castPoint;
                Vector3 cross = Vector3.Cross(direction.normalized, Vector3.up);

                float distance = Vector3.Distance(aimPoint, castPoint);                
                for (int i = 0; i < 3; i++)
                {
                    Vector3 altCastPoint = aimPoint + (cross * visabilityOffsets[i]);
                    Vector3 altDirection = (altCastPoint - castPoint).normalized;
                    
                    list.Clear();
                    GamePhysics.TraceAll(new Ray(castPoint, altDirection), 0f, list, distance * 1.1f, 1084434689, QueryTriggerInteraction.UseGlobal);
                    int num = 0;
                    while (num < list.Count)
                    {
                        BaseEntity foundEntity = list[num].GetEntity();
                        if (foundEntity != null && (foundEntity == obj || foundEntity.EqualNetID(obj)))
                        {
                            Pool.FreeList<RaycastHit>(ref list);
                            return true;
                        }
                        if (foundEntity == Searchlight)
                        {
                            num++;
                            continue;
                        }
                        if (!(foundEntity != null) || foundEntity.ShouldBlockProjectiles())                        
                            break;                        
                        else num++;                        
                    }
                }
                Pool.FreeList<RaycastHit>(ref list);
                return false;
            }
          
            private Vector3 AimOffset(BaseCombatEntity aimat)
            {
                BasePlayer basePlayer = aimat as BasePlayer;
                if (basePlayer != null)
                {
                    return basePlayer.eyes.position;
                }
                return aimat.transform.position + new Vector3(0f, 0.3f, 0f);
            }
            
            public void ToggleAutomation(bool status)
            {
                if (isDisabled || enabled == status)
                    return;

                if (requiresPower)
                {
                    if (!Searchlight.HasFlag(BaseEntity.Flags.Reserved8))
                        return;
                }
                else Searchlight.SetFlag(BaseEntity.Flags.Reserved8, status);

                enabled = status;

                Effect.server.Run(status ? FX_ONLINE : FX_OFFLINE, Searchlight, 0, Vector3.zero, Vector3.zero, null, false);                    
                
                Searchlight.SetFlag(BaseEntity.Flags.On, status);
            }

            public void AdjustLightRotation(float rotation)
            {
                bool wasEnabled = enabled;
                if (enabled)
                    ToggleAutomation(false);

                Searchlight.transform.eulerAngles = new Vector3(0, rotation - 90, 0);
                Searchlight.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                if (search.Enabled)
                {
                    GenerateSearchPattern();
                    lastSearchPoint = 4;
                    nextSearchPoint = searchForwards ? 5 : 3;
                    searchTime = 0;
                }

                if (wasEnabled)
                    ToggleAutomation(true);
            }

            public void SetController(Controller controller) => this.Controller = controller;

            public bool IsEnabled()
            {
                return !isDisabled;
            }

            public void ToggleSearchAutomation(bool status)
            {
                print($"toggle3 {status}");
                if (status)
                {
                    isDisabled = false;
                    if (Controller == null)
                    {
                        if (!enabled && ins.automationEnabled)                        
                            ToggleAutomation(true);
                    }
                }
                else
                {
                    print($"toggle4 {enabled} && {Controller == null}");
                    isDisabled = true;
                    if (enabled && Controller == null)
                    {
                        print("toggle ioff");
                        enabled = false;
                        if (Searchlight.HasFlag(BaseEntity.Flags.On))
                        {
                            Searchlight.SetFlag(BaseEntity.Flags.On, false);
                            Searchlight.SendNetworkUpdate();
                        }
                    }
                }
            }

            public void OnEntityDeath(BaseCombatEntity baseCombatEntity)
            {
                if (baseCombatEntity == targetEntity)                
                    ClearAimTarget();                
            }

            public LightData GetLightData()
            {
                if (Searchlight == null || Searchlight.IsDestroyed)
                    return null;

                return new LightData(this);
            }

            public class LightData
            {
                public uint lightId;
                public int terminalId;
                public string lightName;

                public LightData() { }
                public LightData(LightManager manager)
                {
                    lightId = manager.Searchlight.net.ID;
                    terminalId = manager.terminalId;
                    lightName = manager.lightName;
                }
            }
        }

        class Controller : RustNET.Controller
        {
            public LightManager manager { get; private set; }

            private LinkManager.LightLink link;
            private int spectateIndex = 0;
            private bool switchingTargets;

            private bool wasEnabled = false;
            private bool canCycle;
            private Vector3 offset = new Vector3(0, 0.3f, 0); 
            
            public bool lightsOn { get; private set; }
                       
            public override void Awake()
            {
                base.Awake();
                enabled = false;
                canCycle = ins.configData.Management.Remote.CanCycle;
                player.ClearEntityQueue(null);                
            }
           
            private void FixedUpdate()
            {
                if (player == null || player.serverInput == null || switchingTargets)
                    return;

                InputState input = player.serverInput; 
                if (manager != null && manager.Controller == this)
                {
                    if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                    {
                        lightsOn = !lightsOn;
                        if (lightsOn)
                        {
                            if (manager.Searchlight.IsPowered())
                                manager.Searchlight.SetFlag(BaseEntity.Flags.On, true);
                        }
                        else manager.Searchlight.SetFlag(BaseEntity.Flags.On, false);
                    }
                  
                    Vector3 aimTarget = player.transform.position + (Quaternion.Euler(input.current.aimAngles) * (Vector3.forward * 10));
                    
                    manager.Searchlight.SetTargetAimpoint(aimTarget);
                    manager.Searchlight.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }

                if (input.WasJustPressed(BUTTON.USE))
                {
                    enabled = false;
                    link.CloseLink(this);
                }
                else
                {
                    if (canCycle)
                    {
                        if (input.WasJustPressed(BUTTON.JUMP))
                            UpdateSpectateTarget(1);
                        else if (input.WasJustPressed(BUTTON.DUCK))
                            UpdateSpectateTarget(-1);
                    }
                }
            }

            public override void OnDestroy()
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);

                if (manager != null && manager.Controller == this)
                    manager.SetController(null);
                base.OnDestroy();
            }

            public void SetLightLink(LinkManager.LightLink link)
            {
                this.link = link;
                BeginSpectating();
            }

            public void SetSpectateTarget(int spectateIndex)
            {
                this.spectateIndex = spectateIndex;
                manager = link.Managers[spectateIndex];

                player.SendEntitySnapshot(manager.Searchlight);
                player.gameObject.Identity();
                //player.SetParent(manager.searchLight, 0);

                RustNET.MovePosition(player, manager.Searchlight.transform.position + (Vector3.up * 1.5f), false);

                if (manager.Controller == null)                
                    manager.SetController(this);                
                else player.ChatMessage(ins.msg("Warning.InUse", player.userID));

                CreateCameraOverlay();
            }

            public void UpdateSpectateTarget(int index = 0)
            {
                switchingTargets = true;
                player.Invoke(() => switchingTargets = false, 0.25f);

                int newIndex = spectateIndex + index;

                if (newIndex > link.Managers.Count - 1)
                    newIndex = 0;
                else if (newIndex < 0)
                    newIndex = link.Managers.Count - 1;

                if (spectateIndex == newIndex)
                    return;

                if (manager.Controller == this)                
                    manager.SetController(null);

                manager = null;
                SetSpectateTarget(newIndex);                
            }

            public void BeginSpectating()
            {
                RustNET.StripInventory(player);

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.gameObject.SetLayerRecursive(10);
                player.CancelInvoke("InventoryUpdate");
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
                //player.Command("client.camoffset", new object[] { new Vector3(0, 2.2f, 0) });

                if (Network.Net.sv.write.Start())
                {
                    Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                    Network.Net.sv.write.EntityID(player.net.ID);
                    Network.Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                    Network.Net.sv.write.Send(new Network.SendInfo(player.net.group.subscribers.Where(x => x.userid != player.userID).ToList()));
                }

                player.ChatMessage(ins.msg("Help.Toggle", player.userID));

                if (canCycle)
                    player.ChatMessage(ins.msg("Help.ControlInfo", player.userID));
                else player.ChatMessage(ins.msg("Help.ControlInfo.NoCycle", player.userID));

                enabled = true;
            }

            public void FinishSpectating(bool isDead)
            {
                enabled = false;

                //player.SetParent(null, 0);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.gameObject.SetLayerRecursive(17);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
                player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
                player.Command("client.camoffset", new object[] { new Vector3(0, 1.2f, 0) });

                CuiHelper.DestroyUi(player, ASUI_OVERLAY);

                if (manager.Controller == this)                
                    manager.SetController(null);                

                if (!isDead)
                    Destroy(this);               
            }

            public override void OnPlayerDeath(HitInfo info)
            {
                enabled = false;
                link.CloseLink(this, true);

                base.OnPlayerDeath(info);
            }

            private void CreateCameraOverlay()
            {
                if (!ins.configData.Management.Remote.Overlay)
                    return;

                CuiElementContainer container = RustNET.UI.Container("0 0 0 0", "0 0", "1 1", false, "Under", ASUI_OVERLAY);
                RustNET.UI.Image(ref container, ins.GetImage("searchlightoverlay"), "0 0", "1 1", ASUI_OVERLAY);

                RustNET.UI.Panel(ref container, "0 0 0 0.4", "0.82 0.9", "0.96 0.94", ASUI_OVERLAY);
                RustNET.UI.Label(ref container, string.IsNullOrEmpty(manager.lightName) ? string.Format(ins.msg("UI.SearchlightName", player.userID), spectateIndex + 1) : manager.lightName, 13, "0.82 0.9", "0.96 0.94", TextAnchor.MiddleCenter, ASUI_OVERLAY);

                CuiHelper.DestroyUi(player, ASUI_OVERLAY);
                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region UI
        private void CreateConsoleWindow(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = RustNET.ins.GetBaseContainer(player, terminalId, Title);

            LightManager[] entityIds = GetAvailableSearchlights(terminalId);

            RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], "0.04 0.765", "0.96 0.8");
            RustNET.UI.Label(ref container, msg("UI.Select.Searchlight", player.userID), 12, "0.05 0.765", "0.5 0.8", TextAnchor.MiddleLeft);
            RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.MainMenu", player.userID), 11, "0.82 0.765", "0.96 0.8", $"rustnet.changepage {terminalId}");

            if (entityIds == null || entityIds.Length == 0)
                RustNET.UI.Label(ref container, msg("UI.NoSearchlights", player.userID), 12, "0.05 0.5", "0.95 0.7");
            else
            {
                int count = 0;
                int startAt = page * 18;
                for (int i = startAt; i < (startAt + 18 > entityIds.Length ? entityIds.Length : startAt + 18); i++)
                {
                    LightManager manager = entityIds.ElementAt(i);
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                    RustNET.UI.Label(ref container, string.IsNullOrEmpty(manager.lightName) ? string.Format(msg("UI.Searchlight", player.userID), count + 1) : $"> {manager.lightName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.31 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);

                    if (configData.Management.Remote.ToggleEnabled)
                    {
                        bool isEnabled = manager.IsEnabled();
                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], isEnabled ? RustNET.msg("UI.Enable", player.userID) : RustNET.msg("UI.Disable", player.userID), 11, $"0.32 {0.725f - (count * 0.04f)}", $"0.53 {0.755f - (count * 0.04f)}", $"automatedsearchlights.toggle {manager.Searchlight.net.ID} {terminalId} {page} {!isEnabled}");
                    }

                    if (configData.Management.Remote.RemoteControl)
                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Control", player.userID), 11, $"0.54 {0.725f - (count * 0.04f)}", $"0.75 {0.755f - (count * 0.04f)}", $"automatedsearchlights.control {manager.Searchlight.net.ID} {terminalId}");

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

        [ConsoleCommand("automatedsearchlights.toggle")]
        private void ccmdToggle(ConsoleSystem.Arg arg)
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

            ToggleAutomation(arg.GetUInt(0), arg.GetInt(1), arg.GetBool(3));
            RustNET.ins.DisplayToPlayer(player, arg.GetInt(1), Title, arg.GetInt(2));            
        }

        [ConsoleCommand("automatedsearchlights.control")]
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
        [ChatCommand("sl")]
        private void cmdSL(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_USE)) 
                return;

            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>{Title}</color><color=#939393>  v{Version}  -</color> <color=#ce422b>{Author} @ www.chaoscode.io</color>");
                SendReply(player, msg("Help.Main", player.userID));
                if (configData.Management.NoTerminal)
                    SendReply(player, msg("Help.Add", player.userID));
                SendReply(player, msg("Help.AddTerminal", player.userID));
                SendReply(player, msg("Help.Remove", player.userID));
                SendReply(player, msg("Help.Rotate", player.userID));
                SendReply(player, msg("Help.Name", player.userID));
                return;
            }            

            SearchLight searchLight = RustNET.FindEntityFromRay(player) as SearchLight;
            if (searchLight == null)
            {
                SendReply(player, msg("Error.NoEntity", player.userID));
                return;
            };

            if (searchLight.OwnerID != player.userID && !RustNET.IsFriendlyPlayer(searchLight.OwnerID, player.userID))
            {
                SendReply(player, msg("Error.NotOwner", player.userID));
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                    {
                        if (args.Length != 2)
                        {
                            if (configData.Management.NoTerminal)
                            {
                                if (searchLight.GetComponent<LightManager>())
                                {
                                    SendReply(player, msg("Error.AlreadyRegistered", player.userID));
                                    return;
                                }

                                LinkManager.LightLink lightLink = linkManager.GetLinkOf(0);
                                if (lightLink == null)
                                    lightLink = new LinkManager.LightLink(0, searchLight, "");
                                else lightLink.AddLightToLink(searchLight, 0, "");

                                searchLight.GetComponent<LightManager>().ToggleAutomation(automationEnabled);

                                SendReply(player, msg("Success.Set", player.userID));
                                SaveData();
                            }
                            else
                            {
                                SendReply(player, msg("Error.TerminalID", player.userID));
                                SendReply(player, msg("Help.AddTerminal", player.userID));
                                return;
                            }
                        }
                        else
                        {
                            if (searchLight.GetComponent<LightManager>())
                            {
                                SendReply(player, msg("Error.AlreadyRegistered", player.userID));
                                return;
                            }

                            int terminalId;
                            if (!int.TryParse(args[1], out terminalId))
                            {
                                SendReply(player, msg("Error.TerminalID", player.userID));
                                return;
                            }

                            if (!RustNET.linkManager.IsValidTerminal(terminalId))
                            {
                                SendReply(player, msg("Error.RustNETID", player.userID));
                                return;
                            }

                            RustNET.LinkManager.Link link = RustNET.linkManager.GetLinkOf(terminalId);
                            if (link == null)
                            {
                                SendReply(player, msg("Error.NoLink", player.userID));
                                return;
                            }

                            if (!link.IsPublicLink)
                            {
                                BuildingManager.Building building = link.terminal.ParentEntity.GetBuilding();
                                if (building == null)
                                {
                                    SendReply(player, msg("Error.NoBuilding", player.userID));
                                    return;
                                }

                                if (!building.GetDominatingBuildingPrivilege()?.IsAuthed(player) ?? false)
                                {
                                    SendReply(player, msg("Error.NoPrivilege", player.userID));
                                    return;
                                }

                                if (Vector3.Distance(searchLight.transform.position, link.terminal.DroppedItem.transform.position) > configData.Management.DistanceFromTerminal)
                                {
                                    SendReply(player, msg("Error.Distance", player.userID));
                                    return;
                                }
                            }

                            LinkManager.LightLink lightLink = linkManager.GetLinkOf(terminalId);
                            if (lightLink == null)
                                lightLink = new LinkManager.LightLink(terminalId, searchLight, "");
                            else
                            {
                                int lightLimit = GetMaxLights(player.userID);
                                if (!permission.UserHasPermission(player.UserIDString, PERM_IGNORE) && lightLink.Managers.Count >= lightLimit)
                                {
                                    SendReply(player, msg("Error.Limit", player.userID));
                                    return;
                                }

                                lightLink.AddLightToLink(searchLight, terminalId, "");
                            }

                            searchLight.GetComponent<LightManager>().ToggleAutomation(automationEnabled);

                            SendReply(player, msg("Success.Set", player.userID));
                            SaveData();
                        }
                    }
                    return;                
                case "remove":
                    {                        
                        if (!searchLight.GetComponent<LightManager>())
                        {
                            SendReply(player, msg("Error.NotRegistered", player.userID));
                            return;
                        }

                        LinkManager.LightLink link = linkManager.GetLinkOf(searchLight);
                        if (link == null)
                        {
                            SendReply(player, msg("Error.NoLink", player.userID));
                            return;
                        }

                        LightManager manager = searchLight.GetComponent<LightManager>();
                        if (manager == null)
                        {
                            SendReply(player, msg("Error.NoComponent", player.userID));
                            return;
                        }

                        if (manager.Controller != null)
                            link.CloseLink(manager.Controller);

                        link.Managers.Remove(manager);
                        UnityEngine.Object.Destroy(manager);

                        SaveData();
                        SendReply(player, msg("Success.Remove", player.userID));                               
                    }
                    return;
                case "rotate":
                    {              
                        if (!searchLight.GetComponent<LightManager>())
                        {
                            SendReply(player, msg("Error.NoComponent", player.userID));
                            return;
                        }

                        LightManager follower = searchLight.GetComponent<LightManager>();
                        follower.AdjustLightRotation(player?.eyes?.rotation.eulerAngles.y ?? 0);                       
                        SendReply(player, msg("Warning.Rotation", player.userID));
                    }
                    return;
                case "name":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, msg("Error.NoNameSpecified", player.userID));
                            return;
                        }

                        LightManager manager = RustNET.FindEntityFromRay(player)?.GetComponent<LightManager>();
                        if (manager == null)
                        {
                            SendReply(player, msg("Error.NoEntity", player.userID));
                            return;
                        }

                        manager.lightName = args[1];

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
            if (!configData.Management.Remote.Overlay)
                return;

            if (attempts > 3)
            {
                PrintError("ImageLibrary not found. Unable to load camera overlay UI");
                configData.Management.Remote.Overlay = false;
                return;
            }

            if (configData.Management.Remote.Overlay && !string.IsNullOrEmpty(configData.Management.Remote.OverlayImage))                            
                AddImage("searchlightoverlay", configData.Management.Remote.OverlayImage);

            if (!string.IsNullOrEmpty(configData.Management.Remote.RustNETIcon))
                AddImage(Title, configData.Management.Remote.RustNETIcon);
        }

        private void AddImage(string imageName, string fileName) => RustNET.ins.AddImage(imageName, fileName);

        private string GetImage(string name) => RustNET.ins.GetImage(name);
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Detection Options")]
            public DetectionOptions Detection { get; set; }

            [JsonProperty(PropertyName = "Management Options")]
            public LightManagement Management { get; set; }

            [JsonProperty(PropertyName = "Light Options")]
            public LightOptions Options { get; set; }            

            public class DetectionOptions
            {               
                [JsonProperty(PropertyName = "Animals")]
                public DetectSettings Animals { get; set; }

                [JsonProperty(PropertyName = "Enemy players and NPCs")]
                public DetectSettings Enemies { get; set; }

                [JsonProperty(PropertyName = "Friends and owner")]
                public DetectSettings Friends { get; set; }

                [JsonProperty(PropertyName = "Cars and tanks")]
                public DetectSettings Vehicles { get; set; }

                [JsonProperty(PropertyName = "Helicopters")]
                public DetectSettings Helicopters { get; set; }

                public class DetectSettings
                {
                    [JsonProperty(PropertyName = "Enable this detection type")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Range of detection")]
                    public float Radius { get; set; }

                    [JsonProperty(PropertyName = "Threat rating for priority targeting (1 - 5, 1 being the highest threat)")]
                    public int Threat { get; set; }
                }
            }
            public class LightManagement
            {
                [JsonProperty(PropertyName = "Automatically register light to terminal when placed (if meets other requirements)")]
                public bool AutoRegister { get; set; }

                [JsonProperty(PropertyName = "Allow lights to be set without requiring a terminal")]
                public bool NoTerminal { get; set; }

                [JsonProperty(PropertyName = "Maximum allowed searchlights per base (Permission | Amount)")]
                public Dictionary<string, int> Max { get; set; }

                [JsonProperty(PropertyName = "Maximum distance a searchlight controller can be set away from the terminal")]
                public float DistanceFromTerminal { get; set; }

                [JsonProperty(PropertyName = "Remote Settings")]
                public RemoteOptions Remote { get; set; }

                public class RemoteOptions
                {
                    [JsonProperty(PropertyName = "Allow players to cycle through all linked searchlights")]
                    public bool CanCycle { get; set; }

                    [JsonProperty(PropertyName = "Can players toggle searchlight automation")]
                    public bool ToggleEnabled { get; set; }

                    [JsonProperty(PropertyName = "Can players control the searchlight remotely")]
                    public bool RemoteControl { get; set; }

                    [JsonProperty(PropertyName = "Display camera overlay UI")]
                    public bool Overlay { get; set; }

                    [JsonProperty(PropertyName = "Camera overlay image URL")]
                    public string OverlayImage { get; set; }

                    [JsonProperty(PropertyName = "Searchlight icon URL for RustNET menu")]
                    public string RustNETIcon { get; set; }
                }
            }
            public class LightOptions
            {
                [JsonProperty(PropertyName = "Require power for light automation")]
                public bool RequirePower { get; set; }

                [JsonProperty(PropertyName = "Sunrise hour")]
                public float Sunrise { get; set; }

                [JsonProperty(PropertyName = "Sunset hour")]
                public float Sunset { get; set; }

                [JsonProperty(PropertyName = "Only automate lights at night time")]
                public bool NightOnly { get; set; }

                [JsonProperty(PropertyName = "Search mode")]
                public SearchMode Search { get; set; }

                [JsonProperty(PropertyName = "Flicker mode")]
                public FlickerMode Flicker { get; set; }

                public class SearchMode
                {
                    [JsonProperty(PropertyName = "Enable search mode when no targets are visable")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Rotation speed of search mode")]
                    public float Speed { get; set; }
                }

                public class FlickerMode
                {
                    [JsonProperty(PropertyName = "Enable flickering lights when damaged")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Percentage of health before flickering starts")]
                    public float Health { get; set; }
                }
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
                Detection = new ConfigData.DetectionOptions
                {
                    Animals = new ConfigData.DetectionOptions.DetectSettings
                    {
                        Enabled = true,
                        Radius = 30,
                        Threat = 4
                    },
                    Enemies = new ConfigData.DetectionOptions.DetectSettings
                    {
                        Enabled = true,
                        Radius = 45,
                        Threat = 2,
                    },
                    Friends = new ConfigData.DetectionOptions.DetectSettings
                    {
                        Enabled = true,
                        Radius = 45,
                        Threat = 5,
                    },
                    Helicopters = new ConfigData.DetectionOptions.DetectSettings
                    {
                        Enabled = true,
                        Radius = 100,
                        Threat = 3
                    },
                    Vehicles = new ConfigData.DetectionOptions.DetectSettings
                    {
                        Enabled = true,
                        Radius = 60,
                        Threat = 1
                    }
                },
                Management = new ConfigData.LightManagement
                {
                    AutoRegister = false,
                    Max = new Dictionary<string, int>
                    {
                        ["automatedsearchlights.use"] = 4,
                        ["automatedsearchlights.pro"] = 10
                    },
                    DistanceFromTerminal = 50,
                    NoTerminal = false,
                    Remote = new ConfigData.LightManagement.RemoteOptions
                    {
                        CanCycle = true,
                        RemoteControl = true,
                        ToggleEnabled = true,
                        OverlayImage = "http://www.rustedit.io/images/RustNET/camera.png",
                        Overlay = true,
                        RustNETIcon = "http://www.rustedit.io/images/RustNET/searchlighticon.png"
                    }
                },
                Options = new ConfigData.LightOptions
                {
                    RequirePower = false,
                    Flicker = new ConfigData.LightOptions.FlickerMode
                    {
                        Enabled = true,
                        Health = 50
                    },
                    NightOnly = true,
                    Search = new ConfigData.LightOptions.SearchMode
                    {
                        Enabled = true,
                        Speed = 15f
                    },
                    Sunrise = 8f,
                    Sunset = 19f
                },               
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 2, 0))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(0, 2, 02))
                configData.Management.NoTerminal = baseConfig.Management.NoTerminal;

            if (configData.Version < new VersionNumber(0, 2, 04))
                configData.Management.Max = baseConfig.Management.Max;

            if (configData.Version < new VersionNumber(0, 2, 10))
                configData.Management.Remote = baseConfig.Management.Remote;

            if (configData.Version < new VersionNumber(0, 2, 13))
                configData.Management.Remote.CanCycle = baseConfig.Management.Remote.CanCycle;

            if (configData.Version < new VersionNumber(0, 2, 27))
                configData.Options.RequirePower = true;
            
            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            if (storedData == null || storedData.registeredSearchlights == null)
                storedData = new StoredData();

            storedData.registeredSearchlights = LightManager.allManagers?.Where(x => x != null && x.Searchlight != null && !x.Searchlight.IsDestroyed)?.Select(x => x.GetLightData())?.ToArray() ?? new LightManager.LightData[0];
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
            public LightManager.LightData[] registeredSearchlights = new LightManager.LightData[0];            
        }        
        #endregion

        #region Localization
        string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
        
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Main"] = "<color=#ce422b>/rustnet</color><color=#939393> - Display the help menu for using RustNET</color>",
            ["Help.Add"] = "<color=#ce422b>/sl add</color><color=#939393> - Turn the light into an standalone automatic searchlight</color>",
            ["Help.AddTerminal"] = "<color=#ce422b>/sl add <terminal ID></color><color=#939393> - Turn the light into an automatic searchlight registered to a terminal</color>",
            ["Help.Remove"] = "<color=#ce422b>/sl remove</color><color=#939393> - Remove the lights automation</color>",
            ["Help.Rotate"] = "<color=#ce422b>/sl rotate</color><color=#939393> - Rotates the light you are looking at to the direction you are facing (adjust auto-rotation)</color>",
            ["Help.Name"] = "<color=#ce422b>/sl name <name></color><color=#939393> - Set a name for the searchlight you are looking at</color>",
            ["Help.ControlInfo"] = "<color=#939393>Press <color=#ce422b>'JUMP'</color> and <color=#ce422b>'DUCK'</color> to cycle through available searchlights.\nPress <color=#ce422b>'USE'</color> to exit the controller!</color>",
            ["Help.ControlInfo.NoCycle"] = "Press <color=#ce422b>'USE'</color> to exit the controller!</color>",
            ["Help.Toggle"] = "<color=#939393>You can toggle the light on/off by pressing <color=#ce422b>'FIRE'</color></color>",
            ["Success.Set"] = "<color=#939393>You have successfully registered this searchlight to the terminal</color>",
            ["Success.Remove"] = "<color=#939393>You have successfully removed this searchlight from the terminal</color>",
            ["Warning.TerminalDestroyed"] = "<color=#ce422b>The terminal has been destroyed!</color>",
            ["Warning.TerminalShutdown"] = "<color=#ce422b>The terminal has been shutdown</color>",
            ["Warning.SearchlightDestroyed"] = "<color=#ce422b>The searchlight you were controlling has been destroyed</color>",
            ["Warning.Rotation"] = "<color=#939393>The rotation has been adjusted!</color>",
            ["Warning.NoRemoval"] = "<color=#939393>You can not remove fuel from a automated searchlight when fuel consumption is disabled</color>",
            ["Warning.InUse"] = "<color=#939393>This searchlight is already in use!</color>",
            ["Error.NotOwner"] = "<color=#939393>This searchlight does not belong to you!</color>",
            ["Error.Limit"] = "<color=#939393>This building already has the maximum number of automated search lights!</color>",
            ["Error.AlreadyRegistered"] = "<color=#939393>This light is already a automated searchlight!</color>",
            ["Error.NoComponent"] = "<color=#939393>This light is not a automated searchlight!</color>",
            ["Error.InvalidCommand"] = "<color=#939393>Invalid command! Type <color=#ce422b>/sl</color> for available commands</color>",
            ["Error.TerminalID"] = "<color=#939393>You need to enter a valid terminal ID</color>",
            ["Error.RustNETID"] = "<color=#939393>Invalid terminal ID selected! You can find the terminal ID by opening the terminal</color>",
            ["Error.NoLink"] = "<color=#939393>[ERROR] Unable to find building link</color>",
            ["Error.NoBuilding"] = "<color=#939393>[ERROR] The selected terminal does not have a building</color>",
            ["Error.NoPrivilege"] = "<color=#939393>You do not have building privilege in the terminal building</color>",           
            ["Error.NotRegistered"] = "<color=#939393>The searchlight you are looking at has not been registered</color>",
            ["Error.NoNameSpecified"] = "<color=#939393>You must enter a name for the searchlight!</color>",
            ["Error.NoEntity"] = "<color=#939393>You are not looking at a automated searchlight</color>",
            ["Error.Distance"] = "<color=#939393>This searchlight is too far away from the terminal</color>",
            ["Success.NameSet"] = "<color=#939393>You have set the name of this searchlight to <color=#ce422b>{0}</color></color>",
            ["UI.Searchlight"] = "> Searchlight {0}",
            ["UI.SearchlightName"] = "Searchlight {0}",
            ["UI.Select.Searchlight"] = "> <color=#28ffa6>Searchlights</color> <",
            ["UI.NoSearchlights"] = "No searchlights registered to this terminal",
            ["UI.Help.Title"] = "> <color=#28ffa6>Searchlight Help Menu</color> <",
            ["UI.Help"] = "> To register a searchlight you will need the terminal ID noted above.\n\n> Creating a Automated Searchlight\nStep 1. Deploy a searchlight in or around your base.\nStep 2. Look at your searchlight and type <color=#28ffa6>/sl add <terminal ID></color> replacing <terminal ID> with the ID of the terminal you are using.\n\nYour searchlight is now registered to the terminal, It will search for targets at night time and can be accessed remotely via this control panel.\n\n> Removing a searchlight and restoring it to default\nTo remove a searchlight look at it and type <color=#28ffa6>/sl remove</color>. This will remove its automation and remote functionality and restore it to default",
        };
        #endregion        
    }
}
