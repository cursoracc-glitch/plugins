using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("SuperMedkit", "chhhh", "1.0.0")]
    class SuperMedkit : RustPlugin
    {                		
	
        #region Variables
		
		private const string BloodItemName = "blood";
		private const string MedkitItemName = "largemedkit";
		private static HashSet<ResourceDispenser> GivenAnimals = new HashSet<ResourceDispenser>();
		private static Dictionary<ulong, double> Cooldown = new Dictionary<ulong, double>();
		
		#endregion
		
		#region Hooks
		
		private void Init() => LoadVariables();
		
		private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {			
            if (dispenser == null || entity == null || item == null) return;

			if (configData.BloodRates.ContainsKey(item.info.shortname) && !GivenAnimals.Contains(dispenser))			
			{
				BasePlayer player = entity.ToPlayer();
				if (player == null) return;				
				GiveBlood(player, configData.BloodRates[item.info.shortname]);
				GivenAnimals.Add(dispenser);
			}
		}
		
		private object OnItemSplit(Item item, int split_Amount)
        {
            if (item.info.shortname == BloodItemName)
            {
				var byItemId = ItemManager.CreateByName(BloodItemName, 1);				
                byItemId.amount = split_Amount;
                byItemId.name = item.name;
				item.amount -= split_Amount;
                item.MarkDirty();
                return byItemId;
            }
			
			if (item.info.shortname == MedkitItemName && item.skin == configData.SuperMedkitSkin)
            {
				var byItemId = ItemManager.CreateByName(MedkitItemName, 1, configData.SuperMedkitSkin);				
                byItemId.amount = split_Amount;
                byItemId.name = item.name;
				item.amount -= split_Amount;
                item.MarkDirty();
                return byItemId;
            }
			
            return null;
        }
		
		private object CanStackItem(Item item, Item anotherItem)
        {
            if (item.info.shortname == MedkitItemName && item.skin != anotherItem.skin) return false;
            return null;
        }        

        private object CanCombineDroppedItem(DroppedItem drItem, DroppedItem anotherDrItem)
        {
            if (drItem.item.info.shortname == MedkitItemName && drItem.item.info.itemid == anotherDrItem.item.info.itemid && drItem.item.skin != anotherDrItem.item.skin) return false;
            return null;
        }
		
		private bool? CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item == null || playerLoot == null) return null;
            var player = playerLoot.containerMain.playerOwner;
            if (player == null) return null;
			
            if (item.info.shortname == BloodItemName)
            {
                var container = playerLoot.FindContainer(targetContainer);
                if (container != null)
                {
                    var getItem = container.GetSlot(targetSlot);
                    if (getItem != null && getItem.info.shortname == MedkitItemName)
                    {
						if (getItem.amount > getItem.info.stackable)
						{
							SendReply(player, "Расстакайте аптечки, прежде чем их улучшать!");
							return null;
						}
						
						if (ChangeItem(getItem))						
						{
							Effect.server.Run("assets/prefabs/misc/xmas/presents/effects/wrap.prefab", player, 0, Vector3.zero, Vector3.forward);
							item.UseItem();
							player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
							return false;
						}
                    }
                }
            }
			
			return null;
        }
		
		private object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || player == null) return null;
            if (action == "consume" && item.info.shortname == MedkitItemName && item.skin == configData.SuperMedkitSkin)
            {
                if (!Cooldown.ContainsKey(player.userID))
                    Cooldown.Add(player.userID, GrabCurrentTime() + 1);
                else 
					if (Cooldown[player.userID] > GrabCurrentTime()) 
						return false;
                
				var consumable = item.info.GetComponent<ItemModConsumable>();
								
				item.UseItem();
				if (consumable != null)
				{					
					foreach (ItemModConsumable.ConsumableEffect effect in consumable.effects)
					{
						if (effect.type != MetabolismAttribute.Type.Health)						
						{							
							player.metabolism.ApplyChange(effect.type, effect.amount, effect.time);
							
							if (effect.type.ToString() == "HealthOverTime")
							{
								player.metabolism.ApplyChange(effect.type, effect.amount, effect.time);
								player.metabolism.ApplyChange(effect.type, effect.amount, effect.time);
							}							
						}
						else													
							player.health += effect.amount;
					}
				}
								
                player.health += 50;
                player.SendNetworkUpdate();
                //Effect.server.Run("assets/bundled/prefabs/fx/gestures/take_pills.prefab", player, 0, Vector3.zero, Vector3.forward);
                Cooldown[player.userID] = GrabCurrentTime() + 10;
                
				return false;
            }
			
            return null;
        }
		
		#endregion
		
		#region Main
		
		private bool ChangeItem(Item item)
		{
			if (item == null)  return false;
			
			item.skin = configData.SuperMedkitSkin;
			item.name = configData.SuperMedkitName;
			item.MarkDirty();
			
			return true;
		}
		
		private void GiveBlood(BasePlayer player, int amount)
		{
			var item = ItemManager.CreateByName(BloodItemName, amount);
            item.name = configData.BloodName;
            player.GiveItem(item);
		}
		
		private double GrabCurrentTime() => DateTime.UtcNow.ToLocalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
		
		#endregion
		
		#region Commands
		
		[ChatCommand("sm_give")]
        private void CommandGive(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;            			
			
			GiveBlood(player, 5);
			
			var byItemId = ItemManager.CreateByName(MedkitItemName, 50, configData.SuperMedkitSkin);
			byItemId.name = configData.SuperMedkitName;
			player.GiveItem(byItemId);			
        }
		
		#endregion
		
		#region Config
		
        private static ConfigData configData;
		
        private class ConfigData
        {            
			[JsonProperty(PropertyName = "При добыче мяса животных выпадает кровь")]
			public Dictionary<string, int> BloodRates; 
			[JsonProperty(PropertyName = "Название на пакете с кровью")]
			public string BloodName;
			[JsonProperty(PropertyName = "Скин супер аптечки")]
			public ulong SuperMedkitSkin;
			[JsonProperty(PropertyName = "Название супер аптечки")]
			public string SuperMedkitName;
        }
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                BloodRates = new Dictionary<string, int>()
				{
					{ "wolfmeat.raw", 1 },
					{ "meat.boar", 1 },
					{ "deermeat.raw", 1 },
					{ "bearmeat", 2 }
				},
				BloodName = "Кровь (перетащите кровь на аптечку для её апгрейда)",
				SuperMedkitSkin = 2001406820,
				SuperMedkitName = "Улучшенная аптечка"
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion
		
    }
}