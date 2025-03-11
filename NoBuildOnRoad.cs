using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("No Build On Road", "supreme", "1.0.4")]
    [Description("Disallow building over road")]
    public class NoBuildOnRoad : RustPlugin
    {
        const string NoBuildOnRoadIgnore = "nobuildonroad.ignore";
        
        #region Lang
        
        protected override void LoadDefaultMessages() 
        {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            {"NoPermMsg", "Building on road is not allowed."}

        }, this, "en");

        lang.RegisterMessages(new Dictionary<string, string>
        {
            {"NoPermMsg", "Vous n'avez pas la permission de construire sur les routes."}
            
        }, this, "fr"); 
        }

    #endregion
    
    #region Configuration
    
    private Configuration _config;
    private class Configuration
    {
        [JsonProperty(PropertyName = "Prefix")]
        public string usedPrefix = "[NBOR]";
        
        [JsonProperty(PropertyName = "Prefix Color")]
        public string usedPrefixColor = "#ACFA58";
        
        [JsonProperty(PropertyName = "Chat Icon [SteamID64]")]
        public ulong chatIcon = 76561198278456562;
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

    #region Hooks
        
        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go == null) return;
            var player = plan.GetOwnerPlayer();
            var block = go.ToBaseEntity();
            bool ignore = permission.UserHasPermission(player.UserIDString, NoBuildOnRoadIgnore);
            if (ignore) return;
            bool incave = CheckRoad(block.transform.position);
            if (incave)
            {
                if (block == null) return;
                NextFrame( () => block.Kill());
                GiveRefund(block, player);
                Player.Message(player, $"{lang.GetMessage("NoPermMsg", this, player.UserIDString)}", $"<color={_config.usedPrefixColor}>{_config.usedPrefix}</color>", _config.chatIcon);
            }
        }
        
        void GiveRefund(BaseEntity entity, BasePlayer player)
        {
            var name = entity.ShortPrefabName.Replace(".deployed", "").Replace("_deployed", "");
            if (name == "wall.external.high.wood")
            {
                name = name.Replace(".wood", "");
            }

            if (name == "refinery_small")
            {
                name = name.Replace("refinery_small", "small.oil.refinery");
            }

            if (name == "vendingmachine")
            {
                name = name.Replace("vendingmachine", "vending.machine");
            }

            if (name == "woodbox")
            {
                name = name.Replace("woodbox", "box.wooden");
            }

            if (name == "researchtable")
            {
                name = name.Replace("researchtable", "research.table");
            }
            
            if (name == "repairbench")
            {
                name = name.Replace("repairbench", "box.repair.bench");
            }

            if (name == "hitchtrough")
            {
                name = name.Replace("hitchtrough", "hitchtroughcombo");
            }

            if (name == "water_catcher_small")
            {
                name = name.Replace("water_catcher_small", "water.catcher.small");
            }
            
            if (name == "water_catcher_large")
            {
                name = name.Replace("water_catcher_large", "water.catcher.large");
            }
            
            if (name == "fireplace")
            {
                name = name.Replace("fireplace", "fireplace.stone");
            }
            
            if (name == "sleepingbag_leather")
            {
                name = name.Replace("sleepingbag_leather", "sleepingbag");
            }
            
            if (name == "waterbarrel")
            {
                name = name.Replace("waterbarrel", "water.barrel");
            }
            
            if (name == "sam_site_turret")
            {
                name = name.Replace("sam_site_turret", "samsite");
            }
            
            if (name == "beartrap")
            {
                name = name.Replace("beartrap", "trap.bear");
            }
            
            if (name == "landmine")
            {
                name = name.Replace("landmine", "trap.landmine");
            }
            
            if (name == "electric.windmill.small")
            {
                name = name.Replace("electric.windmill.small", "generator.wind.scrap");
            }
            
            var item = ItemManager.CreateByName(name);
            if (item != null)
            {
                player.GiveItem(item);
                return;
            }

            var block = entity.GetComponent<BaseCombatEntity>();
            if (block != null)
            {
                var cost = block.BuildCost();
                if (cost != null)
                {
                    foreach (var value in cost)
                    {
                        var x = ItemManager.Create(value.itemDef, Convert.ToInt32(value.amount * (entity.Health() / entity.MaxHealth())));
                        if (x == null) continue;
                        player.GiveItem(x);
                    }
                }
            }
        }
        
        void Init()
        {
            permission.RegisterPermission(NoBuildOnRoadIgnore, this);
        }
        
        #endregion
        
        #region Check for road layer
        
        bool CheckRoad(Vector3 Position)
        {
            RaycastHit hitInfo;
            if (!Physics.Raycast(Position, Vector3.down, out hitInfo, 66f, LayerMask.GetMask("Terrain", "World", "Construction", "Water"), QueryTriggerInteraction.Ignore) || hitInfo.collider == null) return false;
            if (hitInfo.collider.name.ToLower().Contains("road")) return true;
            return false;
        }
        
        #endregion
    }
}