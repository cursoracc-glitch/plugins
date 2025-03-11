using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ ПОФИКШЕН С ПОМОЩЬЮ ПРОГРАММЫ СКАЧАНОЙ С https://discord.gg/dNGbxafuJn */ [Info("TreeLimit", "https://discord.gg/dNGbxafuJn", "1.0.5")]    
    public class TreeLimit : RustPlugin
    {						
		
		#region Variables
		
		private static float MinDistance = 6f * 6f;
		private static bool WasRestart = false;
		
		private static Dictionary<string, int> SaveTreeLimit = new Dictionary<string, int>()
		{
			{ "v2_temp_forest", 10000 },
			{ "v2_temp_forest_small", 10000 },
			{ "v2_tundra_forest", 10000 },
			{ "v2_tundra_forest_small", 2000 },
			{ "v2_arctic_forest", 1500 },
			{ "v2_arctic_forest_snow", 3500 },
			{ "v2_arid_cactus", 2000 },
			{ "v2_arid_palms_dense", 3000 },
			{ "v2_arid_palms_light", 600 },
			{ "v2_arid_palms_superdense", 10000 },
			{ "v2_temp_beachforest_small", 5000 },
			{ "v2_temp_field_small", 1750 },
			{ "v2_arid_palm_beach", 8000 },
			{ "v2_temp_field_large", 100 },
			{ "v2_temp_forest_deciduous", 10000 },
			{ "v2_temp_forest_small_deciduous", 10000 }		
		};
		
		private class TreeInfo
		{
			public TreeEntity tree;
			public Vector2 pos;
			public bool isNeedDel;
		}
		
		#endregion
		
		#region Hooks
	
		private void Init() 
		{
			LoadVariables();
			MinDistance = configData.MinDistance * configData.MinDistance;
		}

		private void OnTerrainInitialized() => WasRestart = true;		
		
		private void OnServerInitialized() 
		{				
			if (!WasRestart) return;
			WasRestart = false;
			timer.Once(configData.StartClearTime, ()=> ReFillTree());
		}
		
		#endregion
		
		#region Main
		
		private void ReFillTree() => ServerMgr.Instance.StartCoroutine(SpawnFill());
		
		private IEnumerator SpawnFill()
        {
			Puts($"Поиск близко стоящих деревьев");
			
            var allSpawnPopulations = SpawnHandler.Instance.AllSpawnPopulations;
            SpawnHandler.Instance.StopCoroutine("SpawnTick");            
                        			
			SpawnDistribution[] spawndists = SpawnHandler.Instance.SpawnDistributions;
			for (int i = 0; i < allSpawnPopulations.Length; i++)
			{
				if (!(allSpawnPopulations[i] == null) && allSpawnPopulations[i].name.Contains("v2_"))
				{
					if ((allSpawnPopulations[i] as DensitySpawnPopulation)._targetDensity <= 1f && SaveTreeLimit.ContainsKey(allSpawnPopulations[i].name))
						(allSpawnPopulations[i] as DensitySpawnPopulation)._targetDensity = SaveTreeLimit[allSpawnPopulations[i].name];
					
					(allSpawnPopulations[i] as DensitySpawnPopulation)._targetDensity = (float)Math.Round((configData.DensityPercent/100f) * (allSpawnPopulations[i] as DensitySpawnPopulation)._targetDensity);					
					SpawnHandler.Instance.SpawnInitial(allSpawnPopulations[i], spawndists[i]);					
				}
			}			
            
            SpawnHandler.Instance.StartCoroutine("SpawnTick");						
			
			var trees_ = BaseNetworkable.serverEntities.OfType<TreeEntity>().ToList();	
			var trees = trees_.Select(x=> new TreeInfo() { tree = x, isNeedDel = false, pos = new Vector2(x.transform.position.x, x.transform.position.z) }).ToList();
			int tCount = trees.Count, gIndex = 0;
			int lastPerc = -1;
						
			for (int ii = 0; ii < tCount; ii++)
			{
				if (trees[ii].tree != null && !trees[ii].isNeedDel)
				{
					for (int jj = ii + 1; jj < tCount; jj++)
					{
						if (trees[ii].tree == null || trees[ii].isNeedDel || trees[jj].tree == null || trees[jj].isNeedDel) continue; 
						
						if ((trees[ii].pos - trees[jj].pos).sqrMagnitude <= MinDistance)
							trees[jj].isNeedDel = true;
						
						gIndex++;
						
						if (gIndex % 150000 == 0)
							yield return new WaitForEndOfFrame();
					}
				}
				
				var perc = (int)Math.Round((100f * ii) / tCount);
					
				if (lastPerc != perc)
				{
					if (perc % 10 == 0)
						Puts($"обработано {perc}%");
					
					lastPerc = perc;					
				}
			}
			
			Puts($"Поиск близко стоящих деревьев завершен");
			Puts($"Удаление близко стоящих деревьев [было {trees_.Count()} шт]");	
			
			gIndex = 0;
			foreach(var info in trees.Where(x=> x.isNeedDel && x.tree != null && !x.tree.IsDestroyed))
			{
				info.tree.Kill();
				
				if (gIndex % 150 == 0)
					yield return new WaitForEndOfFrame();
				
				gIndex++;
			}						

			trees_ = BaseNetworkable.serverEntities.OfType<TreeEntity>().ToList();	
			Puts($"Удаление близко стоящих деревьев завершено [стало {trees_.Count()} шт]");
        }
		
		#endregion
		
		#region Commands
		
		[ConsoleCommand("tl.count")] 
		private void CmdTreeCount(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;        
			var trees = BaseNetworkable.serverEntities.OfType<TreeEntity>().ToList();	
			Puts($"Всего деревьев на сервере: {trees.Count()}");
        }
		
		[ConsoleCommand("tl.refill")] 
		private void CmdReFillTree(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;  
			ReFillTree();
        }
		
		#endregion
		
		#region Config        				
		
        private static ConfigData configData;
		
        private class ConfigData
        {
			[JsonProperty(PropertyName = "Задержка запуска чистки после старта сервера (секунды)")]
			public float StartClearTime;
			[JsonProperty(PropertyName = "Минимальное расстояние между деревьями (метры)")]
			public float MinDistance;
			[JsonProperty(PropertyName = "Какой процент деревьев от оригинала оставить")]
			public float DensityPercent;
        }
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
				StartClearTime = 30f,
                MinDistance = 6f,
				DensityPercent = 30f
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion				
		
    }
}
