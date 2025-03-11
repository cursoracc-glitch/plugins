using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Ladder Modes", "Lomarine", "2.0.0")]
    [Description("Плагин для управления лестницами, разработал - https://vk.com/lomarine")] 
    public class LadderModes : RustPlugin
    {
		[PluginReference] Plugin NoEscape;

	    #region Oxide Hooks

	    private void Init()
	    {			
		    if (!plugins.Exists("NoEscape")) PrintError("Отсутствует NoEscape!");
		    LoadDefaultMessages();
		    LoadDefaultConfig();
	    }
		
	    private void OnEntityBuilt(Planner planner, GameObject gameobject)
	    {
		    BaseEntity entity = gameobject.ToBaseEntity();
			
		    if(entity is BaseLadder == false) return;
			
		    BasePlayer player = planner.GetOwnerPlayer();

		    var block = NoEscape.Call<bool>("IsRaidBlocked", player);

		    if (block)
		    {
			    if (!raid)
			    {
				    entity.Kill();
				    player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				    player.ChatMessage(lang.GetMessage("NORAID", this));  
				    return;
			    }

			    if (raidPriv && !player.CanBuild())
			    {
				    entity.Kill();
				    player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				    player.ChatMessage(lang.GetMessage("PRIVELEGE", this));
			    }
		    }
		    else
		    {
			    if(!free)
			    {
				    entity.Kill();
				    player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				    player.ChatMessage(lang.GetMessage("NOFREE", this));
				    return;
			    }

			    if (freePriv && !player.CanBuild())
			    {
				    entity.Kill();
				    player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				    player.ChatMessage(lang.GetMessage("PRIVELEGE", this));
			    }
		    }
	    }

	    #endregion

		#region Language

		protected override void LoadDefaultMessages()
		{
		    lang.RegisterMessages(new Dictionary<string, string>
		    {
			    ["NORAID"] = "<color=#DC143C>[Ladder Master]</color> Штурмовые лестницы нельзя ставить вне рейда!",
			    ["NOFREE"] = "<color=#DC143C>[Ladder Master]</color> Штурмовые лестницы нельзя ставить во время рейда!",
				["PRIVELEGE"] = "<color=#DC143C>[Ladder Master]</color> Вам нужно право на постройку для установки штурмовой лестницы!",
		    }, this);
	    }

		#endregion

		#region Config

	    private bool raidPriv, freePriv, raid , free;

	    private const string 
		    config1 = "Можно ли стравить в рейдблоке", 
		    config2 = "Можно ли их ставить вне рейдблока", 
		    config3 = "Требование права на постройку в рейдблоке", 
		    config4 = "Требование права на постройку вне рейблока";

		protected override void LoadDefaultConfig()
        {
            Config[config1] = raid     = GetConfig(config1, true);
	        Config[config2] = free     = GetConfig(config2, true);
			Config[config3] = raidPriv = GetConfig(config3, false);
	        Config[config4] = freePriv = GetConfig(config4, false);
            SaveConfig();
        }

		T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

		#endregion
	}
}