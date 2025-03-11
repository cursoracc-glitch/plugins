using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Turrets Extended", "supreme", "3.0.3")]
    [Description("Allows players to toggle on/off the turrets/sam sites without the need of electricity")]
    public class TurretsExtended : RustPlugin
    {
        private readonly Vector3 _turretPos = new Vector3(0f, -0.6f, 0.3f);
        private readonly Vector3 _samPos = new Vector3(0f, -0.6f, -0.92f);
        private readonly Vector3 _npcPos = new Vector3(0f, -0.8f, 0.9f);
        private const string SwitchPrefab = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        
        private void OnServerInitialized()
        {
            foreach (var turret in UnityEngine.Object.FindObjectsOfType<ContainerIOEntity>())
            {
                if (turret.OwnerID != 0)
                {
                    AddSwitch(turret);
                    AddSwitchN(turret);
                }
            }
        }

        private void Unload()
        {
            foreach (var turret in UnityEngine.Object.FindObjectsOfType<ContainerIOEntity>())
            {
                var sw = turret.GetComponentInChildren<ElectricSwitch>();
                if (turret.OwnerID != 0)
                { 
                    if (sw != null) 
                    { 
                        sw.SetParent(null); 
                        sw.Kill();
                    }
                }
            }
        }

        private void Toggle(ContainerIOEntity entity)
        {
            if (entity is AutoTurret)
            {
                ToggleTurret(entity as AutoTurret);
            }
            else if (entity is SamSite)
            {
                ToggleSam(entity as SamSite);
            }
        }
        
        private void ToggleTurret(AutoTurret turret)
        {
            if (turret.IsOnline())
            {
                turret.SetIsOnline(false);
            }
            else
            {
                turret.SetIsOnline(true);
            }
            turret.SendNetworkUpdateImmediate();
        }

        private void ToggleSam(SamSite sam)
        {
            if (sam.IsPowered())
            {
                sam.UpdateHasPower(0, 1);
            }
            else
            {
                sam.UpdateHasPower(sam.ConsumptionAmount(), 1);
            }
            sam.SendNetworkUpdateImmediate();
        }
        
        private object CanPickupEntity(BasePlayer player, ElectricSwitch entity)
        {
            if (entity.HasParent())
            {
                return false;
            }
            return null;
        }
        
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go.name != "assets/prefabs/npc/autoturret/autoturret_deployed.prefab" && go.name != "assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab") return;
            ContainerIOEntity entity = go.GetComponent<ContainerIOEntity>();
            if (entity == null) return;
            NextTick(() => AddSwitch(entity));
        }
        
        void OnEntitySpawned(NPCAutoTurret entity)
        {
            if (entity.OwnerID == 0) return;
            ContainerIOEntity turret = entity.GetComponent<ContainerIOEntity>();
            if (turret == null) return;
            NextTick(() => AddSwitchN(entity));
        }
        
        private object OnSwitchToggle(ElectricSwitch switchz, BasePlayer player)
        {
            var entity = switchz.GetComponentInParent<ContainerIOEntity>();
            if (!switchz.HasParent()) return null;
            if (entity is AutoTurret)
            {
                var turret = switchz.GetComponentInParent<AutoTurret>();
                var isAuthed = turret.IsAuthed(player);
                if (entity == null || !player.IsBuildingAuthed() || !isAuthed)
                {
                    if (_config.gameTip)
                    {
                        player.SendConsoleCommand("gametip.showgametip", Lang("NoAuth", player.UserIDString));
                        timer.In(_config.gameTipTime, () => player.Command("gametip.hidegametip"));
                    }
                    
                    if (_config.chatMessage)
                    {
                        player.ChatMessage(Lang("NoAuthChat", player.UserIDString));
                    }
                    return true;
                }
            }
            if (entity == null || !player.IsBuildingAuthed())
            {
                if (_config.gameTip)
                {
                    player.SendConsoleCommand("gametip.showgametip", Lang("NoAuth", player.UserIDString));
                    timer.In(_config.gameTipTime, () => player.Command("gametip.hidegametip"));
                }

                if (_config.chatMessage)
                {
                    player.ChatMessage(Lang("NoAuthChat", player.UserIDString));
                }
                return true;
            }
            Toggle(entity);
            return null;
        }

        private void AddSwitchN(ContainerIOEntity entity)
        {
            if (entity == null) return;
            Vector3 spawnPos = Vector3.zero;
            if (entity.name.Contains("sentry.scientist"))
            {
                spawnPos = _npcPos;
                ElectricSwitch sw = GameManager.server.CreateEntity(SwitchPrefab, spawnPos) as ElectricSwitch;
                if (sw == null) return;
                sw.Spawn();
                sw.SetParent(entity);
                DestroyGroundWatch(sw);
                sw.SetFlag(BaseEntity.Flags.On, entity.IsOn());
                sw.UpdateHasPower(30, 0);
            }
        }
        
        private void AddSwitch(ContainerIOEntity entity)
        {
            if (entity == null) return;
            Vector3 spawnPos = Vector3.zero;
            if (entity.name.Contains("autoturret_deployed"))
            {
                spawnPos = _turretPos;
                ElectricSwitch sw = GameManager.server.CreateEntity(SwitchPrefab, spawnPos) as ElectricSwitch;
                if (sw == null) return;
                sw.Spawn();
                sw.SetParent(entity);
                DestroyGroundWatch(sw);
                sw.SetFlag(BaseEntity.Flags.On, entity.IsOn());
                sw.UpdateHasPower(30, 0);
            }
            else if (entity is SamSite)
            {
                spawnPos = _samPos;
                ElectricSwitch sw = GameManager.server.CreateEntity(SwitchPrefab, spawnPos, Quaternion.Euler(0, 180, 0)) as ElectricSwitch;
                if (sw == null) return;
                sw.Spawn();
                sw.SetParent(entity);
                DestroyGroundWatch(sw);
                sw.SetFlag(BaseEntity.Flags.On, entity.IsPowered());
                sw.UpdateHasPower(30, 0);
            }
            else
            {
                return;
            }
        }
        
        object OnEntityTakeDamage(ElectricSwitch swtichz, HitInfo info)
        {
            if (swtichz != null)
            {
                var turret = swtichz.GetComponentInParent<AutoTurret>();
                if (turret != null)
                {
                    turret.Hurt(info);
                    return true;
                }
            }
            return null;
        }
        
        private void DestroyGroundWatch(ElectricSwitch entity)
        {
            DestroyOnGroundMissing missing = entity.GetComponent<DestroyOnGroundMissing>();
            if (missing != null)
            {
                GameObject.Destroy(missing);
            }
            
            GroundWatch watch = entity.GetComponent<GroundWatch>();
            if (watch != null)
            {
                GameObject.Destroy(watch);
            }
        }
        
        #region Configuration

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Enable Gametip message")]
            public bool gameTip = true;
            
            [JsonProperty(PropertyName = "Gametip message time")]
            public float gameTipTime = 5f;
            
            [JsonProperty(PropertyName = "Enable Chat message")]
            public bool chatMessage = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion
        
        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoAuth"] = "У вас нет прав на строительство или у вас нет прав на турель!",
                ["NoAuthChat"] = "<color=#ce422b>У вас нет прав на строительство или вы не имеете права на турель!</color>"
            }, this);
        }
        
        #endregion
        
        #region Helpers

        string Lang(string key, string id, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}