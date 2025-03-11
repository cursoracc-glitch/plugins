using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hardcore Workbench", "Marat", "1.0.3")]
    [Description("Removes tech tree from workbenches")]
    public class HardcoreWorkbench : RustPlugin
    {
        #region Variables
		
        private const string WorkbenchLayer = "UI_WorkbenchLayer";
        private const string permissionName = "hardcoreworkbench.use";
        private readonly Dictionary<BasePlayer, WorkbenchBehavior> benchOpen = new Dictionary<BasePlayer, WorkbenchBehavior>();
        private List<BaseEntity> vendingMachine = new List<BaseEntity>();
		
        #endregion
		
        #region Hooks
		
        private void OnServerInitialized()
        {
            LoadData();
            if (config.usePermission) permission.RegisterPermission(permissionName, this);
            
            var bp = ItemManager.GetBlueprints();
            for (int i = 0; i < bp.Count; i++)
            {
                if (!storedData.cachedLevel.ContainsKey(bp[i].name))
                {
                    if (bp[i].workbenchLevelRequired < 1) continue;
                    storedData.cachedLevel.Add(bp[i].name, bp[i].workbenchLevelRequired);
                    SaveData();
                }
                bp[i].workbenchLevelRequired = config.disableCraftMode ? 0 : storedData.cachedLevel[bp[i].name];
            }
            
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }
            
            if (config.addVehiclesParts)
            {
                var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
                for (int i = 0; i < monuments.Length; i++)
                {
                    if (monuments[i] != null && monuments[i].name.Contains("compound"))
                    {
                        var pos = monuments[i].transform.position + monuments[i].transform.rotation * new Vector3(0.7f, 0.25f, 6.85f);
                        var rot = monuments[i].transform.rotation * Quaternion.Euler(0f, 0f, 0f);

                        var vending = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_vehicleshigh.prefab", pos, rot) as NPCVendingMachine;
                        if (vending != null)
                        {
                            vending.Spawn();
                            vending.SendNetworkUpdateImmediate(true);
                            vendingMachine.Add(vending);
                        }
                    }
                }
            }
        }
        
        private void Unload()
        {
			var player = BasePlayer.activePlayerList;
			for (int i = 0; i < player.Count; i++)
			{
				CuiHelper.DestroyUi(player[i], WorkbenchLayer);
				OnCloseBox(player[i]);
			}
			
            if (config.addVehiclesParts)
            {
                for (int i = 0; i < vendingMachine.Count; i++)
                {
                    if (!vendingMachine[i].IsDestroyed)
                        vendingMachine[i]?.Kill();
                }
                vendingMachine.Clear();
            }
			
			var bp = ItemManager.GetBlueprints();
			for (int i = 0; i < bp.Count; i++)
			{
				if (storedData.cachedLevel.ContainsKey(bp[i].name))
					bp[i].workbenchLevelRequired = storedData.cachedLevel[bp[i].name];
			}
			
            config = null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }
            
            if (config.disableCraftMode)
                player.ClientRPCPlayer(null, player, "craftMode", 1);
            else
                player.ClientRPCPlayer(null, player, "craftMode", 0);
        }
        
        private void CanLootEntity(BasePlayer player, Workbench container)
        {
            if (player == null || container == null) return;
            if (config.useMenuWorkbench) OpenWorkbench(player);
            else OnOpenBox(player);
        }
        
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (player == null || entity == null) return;
            if (entity is Workbench && config.useMenuWorkbench)
            {
                CuiHelper.DestroyUi(player, WorkbenchLayer);
                return;
            }
            if (entity is ResearchTable)
            {
                OnCloseBox(player);
				return;
            }
        }
        
        private void OnEntityLeave(TriggerWorkbench trigger, BasePlayer player)
        {
            if (player == null || player.IsNpc) return;
            OnCloseBox(player);
        }
        
        private object OnEntityVisibilityCheck(ResearchTable table, BasePlayer player)
        {
            if (table == null || table?.net.ID == null || player == null) return null;
            if (table.GetComponent<WorkbenchBehavior>() != null)
            {
                if (table.HasFlag(BaseEntity.Flags.Reserved1) && table.HasFlag(BaseEntity.Flags.Reserved2))
                {
                    table.SetFlag(BaseEntity.Flags.Reserved3, true, false, true);
                    return false;
                }
                return true;
            }
            return null;
        }
        
        private void OnOpenBox(BasePlayer player)
        {
            if (!benchOpen.ContainsKey(player) || benchOpen[player] == null)
            {
                var box = player.gameObject.AddComponent<WorkbenchBehavior>();
                benchOpen[player] = box;
                box?.Open(player);
            }
        }
        
        private void OnCloseBox(BasePlayer player)
        {
            if (benchOpen.ContainsKey(player) && benchOpen[player] != null)
            {
                var box = benchOpen[player];
                box?.Close(player);
                benchOpen.Remove(player);
                player.gameObject.GetComponent<WorkbenchBehavior>().Destroy();
            }
        }
        
        #endregion
        
        #region Configuration
        
        private class PluginConfig
        {
            [JsonProperty("Use workbench menu")] public bool useMenuWorkbench;
            [JsonProperty("Remove need for workbench")] public bool disableCraftMode;
            [JsonProperty("Time to research item")] public float itemResearchTime;
            [JsonProperty("Add vehicles parts vending machine")] public bool addVehiclesParts;
            [JsonProperty("Use permission to open workbench")] public bool usePermission;
        }
        
        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                useMenuWorkbench = true,
                disableCraftMode = false,
                itemResearchTime = 10f,
                addVehiclesParts = true,
                usePermission = true
            };
        }
        
        private static PluginConfig config;
        
        protected override void SaveConfig() => Config.WriteObject(config);
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<PluginConfig>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("The config file contains an error and has been replaced with the default config.");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        
        #endregion

        #region Commands
        
        [ConsoleCommand("UI_Workbench")]
        private void CmdWorkbenchUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (config.usePermission && !permission.UserHasPermission(player.UserIDString, permissionName)) return;
            OnOpenBox(player);
        }
        
        #endregion

        #region Interface
        
        private void OpenWorkbench(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, WorkbenchLayer);
            CuiElementContainer container = new CuiElementContainer();
            
            string text = config.usePermission && !permission.UserHasPermission(player.UserIDString, permissionName) ? GetMessage("Lang_NoPermissions",player.UserIDString) : GetMessage("Lang_OpenWorkbench",player.UserIDString);
            string color = config.usePermission && !permission.UserHasPermission(player.UserIDString, permissionName) ? "0.74 0.33 0.28 1.0" : "0.4531373 0.5550981 0.2627451 1";
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.0", AnchorMax = "0.5 0.0", OffsetMin = "320.5 117", OffsetMax = "448.5 149" },
                Button = { Command = "UI_Workbench", Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = text, Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.93 0.93 0.93 1" }
            }, "Overlay", WorkbenchLayer);
            
            CuiHelper.AddUi(player, container);
        }
		
        #endregion

        #region Classes
        
        public class WorkbenchBehavior : FacepunchBehaviour
        {
            private ResearchTable container;
            
            public void Awake()
            {
                container = gameObject.GetComponent<ResearchTable>();
            }
            
            public void Open(BasePlayer player)
            {
				if (player != null)
                {
					container = CreateTable(player);
					Invoke(() => StartLoot(player), 0.1f);
				}
            }
            
            public void Close(BasePlayer player)
            {
                if (container != null && !container.IsDestroyed)
                {
                    for (int i = container.inventory.itemList.Count - 1; i >= 0; i--)
                    {
                        player.GiveItem(container.inventory.itemList[i], BaseEntity.GiveItemReason.Generic);
                    }
                    container.inventory.itemList.Clear();
                    container.Kill();
                    UnityEngine.Object.Destroy(container);
                    container = null;
                }
                player.EndLooting();
                UnityEngine.Object.Destroy(this);
            }
            
            public void Destroy() => UnityEngine.Object.Destroy(this);
            
            public static ResearchTable CreateTable(BasePlayer player)
            {
                var table = GameManager.server.CreateEntity("assets/prefabs/deployable/research table/researchtable_deployed.prefab", player.transform.position + new Vector3(0, -1000, 0)) as ResearchTable;
                if (table == null) return null;
				
                UnityEngine.Object.DestroyImmediate(table.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.DestroyImmediate(table.GetComponent<GroundWatch>());
                foreach (var collider in table.GetComponentsInChildren<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
				
                table._limitedNetworking = false;
                table.EnableSaving(false);
                table.researchDuration = config.itemResearchTime;
                table.Spawn();
                table.gameObject.AddComponent<WorkbenchBehavior>();
                table.SendNetworkUpdate();
				table.UpdateNetworkGroup();
                
                return table;
            }
            
            public void StartLoot(BasePlayer player)
            {
                player.inventory.loot.StartLootingEntity(container, false);
                player.inventory.loot.AddContainer(container.inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", container.panelName);
                container.SendNetworkUpdate();
                if (config.useMenuWorkbench) EffectNetwork.Send(new Effect("assets/prefabs/npc/flame turret/effects/flameturret-deploy.prefab", player, 0, new Vector3(0, 0f, 0), new Vector3()), player.Connection);
            }
        }
		
        #endregion
		
        #region Lang
		
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Lang_OpenWorkbench"] = "OPEN WORKBENCH",
                ["Lang_NoPermissions"] = "NO PERMISSION"
            }, this);
			
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Lang_OpenWorkbench"] = "ОТКРЫТЬ ВЕРСТАК",
                ["Lang_NoPermissions"] = "НЕТ РАЗРЕШЕНИЯ"
            }, this, "ru");
        }
		
        private string GetMessage(string key, string steamID) => lang.GetMessage(key, this, steamID);
		
        #endregion

        #region Data
		
		private StoredData storedData;
		
		private class StoredData
        {
            public Dictionary<string, int> cachedLevel = new Dictionary<string, int>();
        }
		
		private void SaveData()
		{
            if (storedData != null)
			{
                Interface.Oxide.DataFileSystem.WriteObject($"{Title}_cachedLevel", storedData, true);
            }
        }
		
		private void LoadData()
		{
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>($"{Title}_cachedLevel");
            if (storedData == null)
			{
                storedData = new StoredData();
                SaveData();
            }
        }
		
		#endregion
    }
}