using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Building Workbench", "", "1.1.2")]
    [Description("Расширяет диапазон верстака для работы внутри всего здания")]
    public class BuildingWorkbench : RustPlugin
    {
        #region Class Fields
        [PluginReference] private readonly Plugin GameTipAPI;

        private PluginConfig _pluginConfig; //Plugin Config

        private TriggerBase _triggerBase;
        private GameObject _object;

        private const string UsePermission = "buildingworkbench.use";
        private const string CancelCraftIgnorePermission = "buildingworkbench.cancelcraftignore";
        private const string AccentColor = "#de8732";

        private readonly List<ulong> _notifiedPlayer = new List<ulong>();
        private readonly Hash<ulong, int> _playerLevel = new Hash<ulong, int>();

        private Coroutine _routine;

        private bool _init;
        #endregion

        #region Setup & Loading
        private void Init()
        {
            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(CancelCraftIgnorePermission, this);
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
                [LangKeys.Notification] = "Ваш верстак был увеличен для работы внутри вашего здания",
                [LangKeys.CraftCanceled] = "Ваше крафт был отменён, потому что вы покинули здание"
            }, this);
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_pluginConfig);
        }

        private void OnServerInitialized()
        {
             _object = new GameObject("BuildingWorkbenchObject");
             _triggerBase = _object.AddComponent<TriggerBase>();
            
            
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
            
            InvokeHandler.Instance.InvokeRepeating(StartUpdatingWorkbench, 1f, _pluginConfig.UpdateRate);
            
            _init = true;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.triggers == null || !player.triggers.Contains(_triggerBase))
            {
                player.EnterTrigger(_triggerBase);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            player.LeaveTrigger(_triggerBase);
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                player.LeaveTrigger(_triggerBase);
            }
            
            InvokeHandler.Instance.CancelInvoke(StartUpdatingWorkbench);
            if (_routine != null)
            {
                InvokeHandler.Instance.StopCoroutine(_routine);
            }
            
            GameObject.Destroy(_object);
        }
        #endregion

        #region Workbench Handler

        private void StartUpdatingWorkbench()
        {
            if (BasePlayer.activePlayerList.Count == 0)
            {
                return;
            }
            
            _routine = InvokeHandler.Instance.StartCoroutine(HandleWorkbenchUpdate());
        }

        private IEnumerator HandleWorkbenchUpdate()
        {
            Hash<uint, int> benchCache = new Hash<uint, int>();
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                BasePlayer player = BasePlayer.activePlayerList[i];
                yield return null;

                if (!HasPermission(player, UsePermission))
                {
                    continue;
                }
                
                UpdatePlayerPriv(player, benchCache);
            }
        }

        private void UpdatePlayerPriv(BasePlayer player, Hash<uint, int> cache = null)
        {
            BuildingPrivlidge priv = player.GetBuildingPrivilege();
            if (priv == null || !priv.IsAuthed(player))
            {
                if (_playerLevel[player.userID] != 0)
                {
                    UpdatePlayerBench(player, 0, 0f);
                    OnPlayerLeftBuilding(player);
                }
                
                return;
            }

            int level;
            if (cache != null && cache.ContainsKey(priv.buildingID))
            {
                level = cache[priv.buildingID];
            }
            else
            {
                level = GetBuildingWorkbenchLevel(priv.buildingID);
                if (cache != null)
                {
                    cache[priv.buildingID] = level;
                }
            }

            UpdatePlayerBench(player, level, _pluginConfig.UpdateRate);
        }

        private void UpdatePlayerBench(BasePlayer player, int level, float checkOffset)
        {
            player.nextCheckTime = Time.realtimeSinceStartup + checkOffset + .5f;
            player.cachedCraftLevel = level;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Workbench1, level == 1);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Workbench2, level == 2);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Workbench3, level == 3);
            player.SendNetworkUpdateImmediate();
            _playerLevel[player.userID] = level;
        }

        private void UpdateNearbyPlayers(Vector3 pos, uint buildingId, BasePlayer player = null)
        {
            NextTick(() =>
            {
                int level = GetBuildingWorkbenchLevel(buildingId);
                float duration = level == 0 ? 0 : _pluginConfig.UpdateRate;
                BuildingPrivlidge priv = BuildingManager.server.GetBuilding(buildingId)?.GetDominatingBuildingPrivilege();
                if (priv == null)
                {
                    return;
                }

                foreach (BasePlayer buildingPlayer in BaseNetworkable.GetConnectionsWithin(pos, 50f).Select(c => (BasePlayer)c.player))
                {
                    if (priv.IsAuthed(buildingPlayer))
                    {
                        UpdatePlayerBench(buildingPlayer, level, duration);
                    }
                }
            });
        }

        private void OnPlayerLeftBuilding(BasePlayer player)
        {
            if (_pluginConfig.CancelCraft && !HasPermission(player, CancelCraftIgnorePermission) && player.inventory.crafting.queue.Count != 0)
            {
                player.inventory.crafting.CancelAll(true);
                if (_pluginConfig.CancelCraftNotification)
                {
                    Chat(player, Lang(LangKeys.CraftCanceled, player));
                }
            }
        }
        #endregion

        #region Oxide Hooks
        private void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        {
            BasePlayer player = entity.ToPlayer();
            if (player != null && trigger == _triggerBase)
            {
                player.EnterTrigger(_triggerBase);
            }
        }

        private void OnEntitySpawned(Workbench bench)
        {
            if (!_init)
            {
                return;
            }
            
            BasePlayer player = BasePlayer.FindByID(bench.OwnerID);
            if (player == null)
            {
                return;
            }
            
            UpdateNearbyPlayers(bench.transform.position, bench.buildingID, player);

            if (!_pluginConfig.EnableNotifications)
            {
                return;
            }
            
            if (_notifiedPlayer.Contains(player.userID))
            {
                return;
            }
            
            _notifiedPlayer.Add(player.userID);
            
            if (GameTipAPI == null)
            {
                Chat(player, Lang(LangKeys.Notification, player));
            }
            else
            {
                GameTipAPI.Call("ShowGameTip", player, Lang(LangKeys.Notification, player), 6f);
            }
        }

        private void OnEntityKill(Workbench bench)
        {
            if (!_init)
            {
                return;
            }
            
            UpdateNearbyPlayers(bench.transform.position, bench.buildingID);
        }
        
        private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            OnAuthChanged(player);
        }
        
        private void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            OnAuthChanged(player);
        }
        
        private void OnAuthChanged(BasePlayer player)
        {
            NextTick(() =>
            {
                UpdatePlayerPriv(player);
            });
        }

        private void OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            NextTick(() =>
            {
                Hash<uint, int> cache = new Hash<uint, int>();
                foreach (BasePlayer nearbyPlayers in BaseNetworkable.GetConnectionsWithin(privilege.transform.position, 50f).Select(c => (BasePlayer)c.player))
                {
                    UpdatePlayerPriv(nearbyPlayers, cache);
                }
            });
        }
        #endregion

        #region Helper Methods

        private int GetBuildingWorkbenchLevel(uint buildingId)
        {
            return BuildingManager.server.GetBuilding(buildingId)?.decayEntities
                .OfType<Workbench>()
                .Select(bench => bench.Workbenchlevel)
                .Concat(new[] {0})
                .Max() ?? 0;
        }

        private void Chat(BasePlayer player, string format, params object[] args) => PrintToChat(player, Lang(LangKeys.Chat, player, format), args);
        
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        
        private string Lang(string key, BasePlayer player = null, params object[] args) => string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
        #endregion

        #region Classes
        private class PluginConfig
        {
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Enable Notifications")]
            public bool EnableNotifications { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Cancel craft when leaving building")]
            public bool CancelCraft { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Cancel craft notification")]
            public bool CancelCraftNotification { get; set; }
            
            [DefaultValue(3f)]
            [JsonProperty(PropertyName = "Update Rate (Seconds)")]
            public float UpdateRate { get; set; }
        }
        
        private class LangKeys
        {
            public const string Chat = "Chat";
            public const string Notification = "Notification";
            public const string CraftCanceled = "CraftCanceled";
        }
        #endregion
    }
}
