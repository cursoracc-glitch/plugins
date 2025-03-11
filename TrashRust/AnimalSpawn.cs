using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Facepunch;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ ПОФИКШЕН С ПОМОЩЬЮ ПРОГРАММЫ СКАЧАНОЙ С https://discord.gg/dNGbxafuJn */ [Info("AnimalSpawn", "https://discord.gg/dNGbxafuJn", "1.0.2")]
    class AnimalSpawn : RustPlugin
    {																											

		#region Variables				
		
		private AnimalDataInfo AnimalData;
		private bool IsSpawning = false;
		private Dictionary<string, float> SaveOrigSpawn = new Dictionary<string, float>();
		private int GroundLayer = LayerMask.GetMask("Terrain", "World", "Construction", "Deployable", "Deployed", "Default", "Tree", "Resource", "Water");
		
		private class AnimalDataInfo
		{
			public int SquareSideSize;			
			public Dictionary<int, SquareInfo> Squares = new Dictionary<int, SquareInfo>();
			public List<AnimalInfo> AnimalBody = new List<AnimalInfo>();			
		}
		
		private class SquareInfo
		{
			public int minX;
			public int maxX;
			public int minY;
			public int maxY;			
			public SquareInfo(int minX, int maxX, int minY, int maxY) { this.minX = minX; this.maxX = maxX; this.minY = minY; this.maxY = maxY; }
			public bool noNeedSpawn;
		}
		
		private class AnimalInfo
		{
			[JsonIgnore]
			public BaseEntity entity;
			public ulong id;
			public string type;
			public int square;
		}
		
		#endregion
	
		#region Hooks
	
		private void Init() 
		{
			LoadVariables();
			LoadData();
		}
		
		private void OnServerInitialized()
        {   
			SaveOrigSpawn.Add("bear", Bear.Population);
			SaveOrigSpawn.Add("boar", Boar.Population);
			SaveOrigSpawn.Add("wolf", Wolf.Population);
			SaveOrigSpawn.Add("stag", Stag.Population);
			SaveOrigSpawn.Add("horse", Horse.Population);
			SaveOrigSpawn.Add("chicken", Chicken.Population);
			SaveOrigSpawn.Add("zombie", Zombie.Population);
			SaveOrigSpawn.Add("ridablehorse", RidableHorse.Population);
			
			Bear.Population = 0f;
			Boar.Population = 0f;
			Wolf.Population = 0f;
			Stag.Population = 0f;
			Horse.Population = 0f;
			Chicken.Population = 0f;
			Zombie.Population = 0f;
			RidableHorse.Population = 0f;
			
			var delAllAnimals = new HashSet<BaseEntity>(BaseNetworkable.serverEntities.OfType<BaseEntity>().Where(b => configData.AnimalsBySquare.ContainsKey(b.PrefabName)));
			foreach(var animal in delAllAnimals)
			{
				if (animal == null || animal.IsDestroyed) continue;
				animal.KillMessage();
			}						
			
            if (AnimalData == null || AnimalData.SquareSideSize == 0)
			{
				if (AnimalData == null)
					AnimalData = new AnimalDataInfo();												
				
				int count = 0;				
				AnimalData.SquareSideSize = configData.SquareSideSize;
				
				for(int ii=0;ii<(int)Math.Round((decimal)World.Size/configData.SquareSideSize);ii++)
					for(int jj=0;jj<(int)Math.Round((decimal)World.Size/configData.SquareSideSize);jj++)
					{
						SquareInfo si = new SquareInfo(configData.SquareSideSize*ii-(int)World.Size/2, configData.SquareSideSize*ii+(configData.SquareSideSize-1)-(int)World.Size/2, configData.SquareSideSize*jj-(int)World.Size/2, configData.SquareSideSize*jj+(configData.SquareSideSize-1)-(int)World.Size/2);						
						AnimalData.Squares.Add(count, si);
						count++;
					}
					
				SaveData();	
			}			
			
			timer.Once(50f, CheckSpawn);
        }				
		
		private void Unload()
        {   
			IsSpawning = true;
			
            foreach (var animal in AnimalData.AnimalBody.ToList())            
                if (animal.entity != null)
                {
                    UnityEngine.Object.Destroy(animal.entity.GetComponent<NPCController>());
                    animal.entity.KillMessage();				
                }			            
			
			AnimalData.AnimalBody.Clear();
			
            var objects = UnityEngine.Object.FindObjectsOfType<NPCController>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
					
			SaveData();		
			
			Bear.Population = SaveOrigSpawn["bear"];
			Boar.Population = SaveOrigSpawn["boar"];
			Wolf.Population = SaveOrigSpawn["wolf"];
			Stag.Population = SaveOrigSpawn["stag"];
			Horse.Population = SaveOrigSpawn["horse"];
			Chicken.Population = SaveOrigSpawn["chicken"];
			Zombie.Population = SaveOrigSpawn["zombie"];
			RidableHorse.Population = SaveOrigSpawn["ridablehorse"];
        }
		
		private void OnNewSave()
		{								
			AnimalData.SquareSideSize = 0;
			AnimalData.Squares.Clear();
			SaveData();
		}
		
		#endregion
		
		#region Main
		
		private void CheckSpawn()
		{			
			if (!IsSpawning)
			{
				ClearDieAnimals();
				CommunityEntity.ServerInstance.StartCoroutine(SpawnAnimals());			
			}
			
			timer.Once(configData.RespawnTime, CheckSpawn);
		}
		
		private void ClearDieAnimals()
		{			
			foreach (var animal in AnimalData.AnimalBody.ToList())            
                if (animal.entity == null || animal.entity.IsDestroyed)
					AnimalData.AnimalBody.Remove(animal);							
		}
		
		private IEnumerator SpawnAnimals()
		{
			IsSpawning = true;			
			foreach(var square in AnimalData.Squares.ToDictionary(x=>x.Key, x=>x.Value))
			{
				if (square.Value.noNeedSpawn) continue;
				
				foreach(var animalCfg in configData.AnimalsBySquare)
				{
					var count = AnimalData.AnimalBody.Where(x=>x.square == square.Key && x.type == animalCfg.Key).Count();
					
					for(int ii=count;ii<animalCfg.Value;ii++)
					{
						BaseEntity entity = null;
						var result = TrySpawnAnimal(animalCfg.Key, square.Value, ref entity);						
						AnimalData.Squares[square.Key].noNeedSpawn = !result;
												
						if (entity != null)
							AnimalData.AnimalBody.Add(new AnimalInfo() {entity = entity, id = entity.net.ID.Value, type = animalCfg.Key, square = square.Key});							
						
						yield return new WaitForSeconds(0.15f);
					}					
				}
				SaveData();
			}			
			IsSpawning = false;
		}

		private bool TrySpawnAnimal(string type, SquareInfo square, ref BaseEntity entity)
		{
			Vector3 newPos = default(Vector3);
			
			for(int ii=0;ii<configData.MaxTryFindSpawn;ii++)
			{
				var x = UnityEngine.Random.Range(square.minX, square.maxX);
				var z = UnityEngine.Random.Range(square.minY, square.maxY);								
				
				if (IsPlaceNorm(x, z, ref newPos))
				{
					var tmp = SpawnAnimal(type, newPos);
					if (tmp != null)					
					{
						entity = tmp;
						return true;					
					}
				}
			}
			
			return false;
		}
		
		private bool IsNobodyNear(Vector3 pos)
		{
			var distance = configData.MinDistanceSpawn * configData.MinDistanceSpawn;
			foreach(var player in BasePlayer.activePlayerList)			
				if ((pos-player.transform.position).sqrMagnitude <= distance)
					return false;
			
			return true;
		}
		
		private bool IsFlatPlace(Vector3 pos)
		{
			RaycastHit hitInfo;			
			
			var posTmp = pos + new Vector3(-1f,500f,0f);
			if (Physics.Raycast(posTmp, Vector3.down, out hitInfo, 1000f, GroundLayer))
			{
				if (Math.Abs(pos.y - hitInfo.point.y) >= configData.SpawnDelta)
					return false;
			}
			else
				return false;
			
			posTmp = pos + new Vector3(0f,500f,-1f);
			if (Physics.Raycast(posTmp, Vector3.down, out hitInfo, 1000f, GroundLayer))
			{
				if (Math.Abs(pos.y - hitInfo.point.y) >= configData.SpawnDelta)
					return false;
			}
			else
				return false;
			
			posTmp = pos + new Vector3(1f,500f,0f);
			if (Physics.Raycast(posTmp, Vector3.down, out hitInfo, 1000f, GroundLayer))
			{
				if (Math.Abs(pos.y - hitInfo.point.y) >= configData.SpawnDelta)
					return false;
			}
			else
				return false;
			
			posTmp = pos + new Vector3(0f,500f,1f);
			if (Physics.Raycast(posTmp, Vector3.down, out hitInfo, 1000f, GroundLayer))
			{
				if (Math.Abs(pos.y - hitInfo.point.y) >= configData.SpawnDelta)
					return false;
			}
			else
				return false;
			
			return true;
		}	
		
		private bool IsPlaceNorm(int x, int z, ref Vector3 newPos)
		{
			Vector3 pos = new Vector3(x, 500f, z);
			RaycastHit hitInfo;
			
			var result = Physics.SphereCast(pos, 2f, Vector3.down, out hitInfo, 1000, GroundLayer);						
			
			if (!result) 
				return false;
			
			if (hitInfo.collider.name != "Terrain")
				return false;
			
			if (hitInfo.point.y < 0)
				return false;
			
			newPos = new Vector3(x, hitInfo.point.y, z);
			
			if (!IsNobodyNear(newPos))
				return false;

			if (!IsFlatPlace(newPos))
				return false;
			
			return true;
		}
		
		private BaseEntity SpawnAnimal(string type, Vector3 newPos)
		{			
			if (newPos == default(Vector3)) return null;
			
			BaseEntity entity = InstantiateEntity(type, newPos);
			entity.Spawn();
			var npc = entity.gameObject.AddComponent<NPCController>();
			npc.SetHome(newPos);
			
			return entity;			
		}				
		
		private BaseEntity InstantiateEntity(string type, Vector3 position)
        {
            var gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(type), position, Quaternion.Euler(0, UnityEngine.Random.Range(0.0f, 360.0f), 0));
            gameObject.name = type;

            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }
		
		private class NPCController : MonoBehaviour
        {
            public BaseNpc npc;
			public BaseVehicle vcl;
            private Vector3 homePos;

            private void Awake()
            {
                npc = GetComponent<BaseNpc>();
				vcl = GetComponent<BaseVehicle>();
                enabled = false;
            }
			
            private void OnDestroy() => InvokeHandler.CancelInvoke(this, CheckLocation);            
			
            public void SetHome(Vector3 homePos)
            {
				if (npc == null) return;
				
                this.homePos = homePos;
                InvokeHandler.InvokeRepeating(this, CheckLocation, 1f, 20f);
            }

            private void CheckLocation()
            {
				if (npc == null) 
				{
					Destroy(this);
					return;
				}
				
				if (npc.NavAgent == null)
				{					
					npc.KillMessage();
					Destroy(this);
					return;
				}
				
                if (Vector3.Distance(npc.transform.position, homePos) > 100)                
                    npc.UpdateDestination(homePos);                
            }
        }
		
		private void Output(BasePlayer player, string output, string text)
		{
			if (output == "chat")
				SendReply(player, text);
			else
				if (output == "console")
					PrintToConsole(player, text);
				else
					Puts(text);		
		}
		
		#endregion
		
		#region Command		

		[Oxide.Plugins.ChatCommand("animal.tp")]
        private void TpCommand(BasePlayer player, string command, string[] args)
        {
			if (!player.IsAdmin) return;
			
			var list = UnityEngine.Object.FindObjectsOfType<BaseEntity>().Where(x=> x.ShortPrefabName == args[0]).ToList();
			
			if (list == null || list.Count==0) 
			{
				SendReply(player, "Указанные животные не найдены !");
				return;
			}	
			
            player.Teleport(list.GetRandom().transform.position); 
        }
		
		[ConsoleCommand("animal.count")]
        private void CountCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
			if (player != null && !player.IsAdmin) return;
			
			Dictionary<string, int> diff = new Dictionary<string, int>();
			var npcs = new HashSet<BaseEntity>(BaseNetworkable.serverEntities.OfType<BaseEntity>().Where(b => configData.AnimalsBySquare.ContainsKey(b.PrefabName)));
			
            foreach(var npc in npcs)
			{
				if (!diff.ContainsKey(npc.ShortPrefabName))				
					diff.Add(npc.ShortPrefabName, 1);					
				else
					diff[npc.ShortPrefabName]++;
			}
			
			if (diff.Count == 0)
				Output(player, player != null ? "console" : "", "Животные не найдены !");
			else			
				foreach(var npc in diff)
					Output(player, player != null ? "console" : "", string.Format("Тип NPC '{0}' - {1} шт.", npc.Key, npc.Value));								
		}
		
		#endregion
		
		#region Config        				
		
        private static ConfigData configData;
		
        private class ConfigData
        {            
			[JsonProperty(PropertyName = "Ширина стороны одного квадрата сетки спавна животных (метры)")]
			public int SquareSideSize;
			[JsonProperty(PropertyName = "Частота возрождения животных (секунды)")]
			public int RespawnTime;
			[JsonProperty(PropertyName = "Минимальное расстояние от игрока до места где может заспавнится животное (метры)")]
			public int MinDistanceSpawn;
			[JsonProperty(PropertyName = "Максимальное количество попыток найти подходящий спавн в заданном квадрате (50-500)")]
			public int MaxTryFindSpawn;
			[JsonProperty(PropertyName = "Допустимая величина отклонения неровности поверхности спавна")]
			public float SpawnDelta;
			[JsonProperty(PropertyName = "Количество животных на один квадрат сетки (целое число)")]
			public Dictionary<string, int> AnimalsBySquare;			
        }
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                SquareSideSize = 1000,
				RespawnTime = 120,
				MinDistanceSpawn = 30,
				MaxTryFindSpawn = 100,
				SpawnDelta = 0.25f,
				AnimalsBySquare = new Dictionary<string, int>()
				{
					{"assets/rust.ai/agents/zombie/zombie.prefab", 0},
					{"assets/rust.ai/agents/bear/bear.prefab", 3},
					{"assets/rust.ai/agents/boar/boar.prefab", 3},
					{"assets/rust.ai/agents/chicken/chicken.prefab", 4},
					{"assets/rust.ai/agents/horse/horse.prefab", 2},
					{"assets/rust.ai/agents/stag/stag.prefab", 3},
					{"assets/rust.ai/agents/wolf/wolf.prefab", 2},
					{"assets/rust.ai/nextai/testridablehorse.prefab", 2},
				}				
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion				
		
		#region Data				
		
		private void LoadData() 
		{
			AnimalData = Interface.GetMod().DataFileSystem.ReadObject<AnimalDataInfo>("AnimalSpawnData");							
			
			if (AnimalData == null)
				AnimalData = new AnimalDataInfo();
			
			if (AnimalData.Squares == null)
				AnimalData.Squares = new Dictionary<int, SquareInfo>();
			
			if (AnimalData.AnimalBody == null)
				AnimalData.AnimalBody = new List<AnimalInfo>();
		}
		
		private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("AnimalSpawnData", AnimalData);				
		
		#endregion
		
	}
	
}	