using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System.Globalization;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("RankMe", "NameTAG", "0.0.1")]
	//
    class RankMe : RustPlugin
    {	
        [PluginReference]
        private Plugin Friends,	TimedItemBlocker;
		
		//	Создаем чат команды
		[ChatCommand("stats")]
        void TurboRankCommand(BasePlayer player, string command)
        {
			var TopPlayer = (from x in Tops where x.UID == player.UserIDString select x);
			PrintToChat(player, "<size=14><color=#00FF00>Статистика игрока</color></size>");
			foreach (var top in TopPlayer)
            { 
				PrintToChat(player, $"<size=14> <color=#FFFFFF>Убито Людей</color> <color=#FF0000>{top.УбийствPVP}</color></size>",null,"0");
				PrintToChat(player, $"<size=14> <color=#FFFFFF>Убито в голову</color> <color=#FF0000>{top.Хедшоты}</color></size>", null, "0");
                PrintToChat(player, $"<size=14> <color=#FFFFFF>Смертей</color> <color=#FF0000>{top.Смертей}</color></size>", null, "0");
                PrintToChat(player, $"<size=14> <color=#FFFFFF>Убито Животных</color> <color=#FF0000>{top.УбийствЖивотных}</color></size>", null, "0");
                PrintToChat(player, $"<size=14> <color=#FFFFFF>Добыто дерево</color> <color=#FF0000>{top.Дерево}</color></size>",null,"0");
                PrintToChat(player, $"<size=14> <color=#FFFFFF>Добыто камня</color> <color=#FF0000>{top.Камень}</color></size>", null, "0");
            }
			return;
		}
		
		void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
		{

			if (victim == null || info == null) return;
			
            // Смерть Животных
            if (victim is BaseNpc)
            {
                if (!(bool)victim?.name?.Contains("agents/")) return;
                
                var hunter = info.InitiatorPlayer;
            
                if (hunter == null) return;
            
                TopData con1 = (from x in Tops where x.UID == hunter.UserIDString select x).FirstOrDefault();

                if (con1 == null) return;

                con1.УбийствЖивотных +=1;
            }
            //  Смерть Игрока
            else if (victim is BasePlayer)
            {
                var playerV = victim as BasePlayer;
                
                if (playerV == null || playerV.IsSleeping()) return;
                
                TopData con2 = (from x in Tops where x.UID == playerV.UserIDString select x).FirstOrDefault();
            
                if (con2 == null) return;
                
                con2.Смертей += 1;
                
                var initiator = info.InitiatorPlayer;
                
                if (initiator == null || initiator == playerV /*|| AreFriendsAPIFriend(initiator.UserIDString, playerV.UserIDString)*/) return;
                
                TopData con3 = (from x in Tops where x.UID == initiator.UserIDString select x).FirstOrDefault();
                
                if (con3 == null) return;
                
                //	Считаем убийства игроков / хедшоты / дистанцию
                        
                con3.УбийствPVP += 1;
					
                var distance = Vector3.Distance(initiator.transform.position, playerV.transform.position);
                        
                if ((int)distance > con3.Дистанция)
                {
                    con3.Дистанция = (int)distance;
                }
            
                if (info.isHeadshot)
                    con3.Хедшоты += 1;
            }
		}
        
        private bool AreFriendsAPIFriend(string playerId, string friendId)
        {
            try
            {
                bool result = (bool)Friends?.CallHook("AreFriends", playerId, friendId);
                return result;
            }
            catch
            {
                return false;
            }
        }
		
		//	Считаем взрывчатку
		void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
		{
			if (TimedItemBlocker) return;
            
            if (entity.ShortPrefabName == "survey_charge.deployed" || entity.ShortPrefabName == "grenade.smoke.deployed") return;

			TopData con = (from x in Tops where x.UID == player.UserIDString select x).FirstOrDefault();
			con.ВзрывчатокИспользовано += 1;
			//Saved();
		}
		
		//	Считаем ракеты
		void OnRocketLaunched(BasePlayer player, BaseEntity entity)
		{
            if (TimedItemBlocker) return;
			
			TopData con = (from x in Tops where x.UID == player.UserIDString select x).FirstOrDefault();
			con.РакетВыпущено += 1;
			//Saved();
		}

		//	Считаем строительство
        void OnEntityBuilt(Planner plan, GameObject go)
        {
            var player = plan.GetOwnerPlayer();
            
			if (player == null) return;
            
			TopData con = (from x in Tops where x.UID == player.UserIDString select x).FirstOrDefault();
			con.Построек += 1;
			//Saved();
        }
		
		//	Дерево / Камень
		void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
            var player = entity.ToPlayer();
            if (player == null || dispenser == null || item == null) return;
			
			TopData con = (from x in Tops where x.UID == player.UserIDString select x).FirstOrDefault();

            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
			{
                con.Дерево += item.amount;
				//Saved();
				return;
			}
            else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
			{
                con.Камень += item.amount;
				//Saved();
				return;
			}
		}
		
		void CreateInfo(BasePlayer player)
        {
			//if (player == null) return;
			Tops.Add(new TopData((string)player.displayName, player.UserIDString, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
			//Saved();
		}
		
		//	Добавляем игрока в Базу Данных
		void OnPlayerInit(BasePlayer player)
		{
			//if (player == null) return;
			
			var check = (from x in Tops where x.UID == player.UserIDString select x).Count(); 
			if (check == 0)
				CreateInfo(player);
		}
		
		//	Загружаем TopData.json и проверяем есть ли все игроки в Базе Данных
		void Loaded()
        { 
			int place;
			int rr = 0;
			int r;
			
			Tops = Interface.Oxide.DataFileSystem.ReadObject<HashSet<TopData>>("TopData"); 
			foreach (var player in BasePlayer.activePlayerList)
			{	
				var check = (from x in Tops where x.UID == player.UserIDString select x).Count(); 
				if(check==0)CreateInfo(player);
			}
			//	Показываем в чат каждые 10мин трех лучших из одной случайной категории
			timer.Repeat (300, 0, ()=>
			{	
				place = 0;
				//	Выбираем случайную категорию
				r = Core.Random.Range(1,10);
				//	антиповтор	//////////////////////
				if (r == rr)
				{
					if (r >= 9)
					{
						r--;
					}
					else
					{
						r++;
					}
				}
				//////////////////////////////////////
				if(r==1)
				{
					rr = 1;
					var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(5);
					PrintToChat($"<size=14>     <color=#00FF00>УБИЙЦЫ</color></size>");
					foreach (var top in TopPlayer)
					{
						place += 1;
						PrintToChat($"<size=14><color=#FFFFFF>{place}.</color>  <color=#FFFFFF>{top.Ник}</color> <color=#00FF00>{top.УбийствPVP}</color></size>");
						Puts($"[ТОП Убийца] {top.Ник}  [{top.УбийствPVP}]");
					}
				}
				else if(r==2)
				{
					rr = 2;
					var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствЖивотных).Take(5);
					PrintToChat($"<size=14>     <color=#00FF00>ОХОТНИКИ</color></size>");
					foreach (var top in TopPlayer)
					{
						place += 1;
						PrintToChat($"<size=14><color=#FFFFFF>{place}.</color>  <color=#FFFFFF>{top.Ник}</color> <color=#00FF00>{top.УбийствЖивотных}</color></size>");
						Puts($"[ТОП Охотник] {top.Ник}  [{top.УбийствЖивотных}]");
					}
				}
				else if(r==3)
				{
					rr = 3;
					var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.Дерево).Take(5);	
					PrintToChat($"<size=14>     <color=#00FF00>ДРОВОСЕКИ </color></size>");
					foreach (var top in TopPlayer)
					{
						place += 1;
						PrintToChat($"<size=14><color=#FFFFFF>{place}.</color>  <color=#FFFFFF>{top.Ник}</color> <color=#00FF00>{top.Дерево}</color></size>");
						Puts($"[ТОП Дровосек] {top.Ник}  [{top.Дерево}]");
					}
				}
				else if(r==4)
				{
					rr = 4;
					var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.Смертей).Take(5);
					PrintToChat($"<size=14>     <color=#00FF00>СМЕРТНИКИ</color></size>");
					foreach (var top in TopPlayer)
					{
						place += 1;
						PrintToChat($"<size=14><color=#FFFFFF>{place}.</color>  <color=#FFFFFF>{top.Ник}</color> <color=#00FF00>{top.Смертей}</color></size>");
						Puts($"[ТОП Смертник] {top.Ник}  [{top.Смертей}]");
					}
				}
				else if(r==5)
				{
					rr = 5;
					var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.Хедшоты).Take(5);
					PrintToChat($"<size=14>     <color=#00FF00>ГОЛОВОСТРЕЛЫ</color></size>");
					foreach (var top in TopPlayer)
					{
						place += 1;
						PrintToChat($"<size=14><color=#FFFFFF>{place}.</color>  <color=#FFFFFF>{top.Ник}</color> <color=#00FF00>{top.Хедшоты}</color></size>");
						Puts($"[ТОП Головострел] {top.Ник}  [{top.Хедшоты}]");
					}
				}
				else if(r==6)
				{
					rr = 6;
					var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.Камень).Take(5);
					PrintToChat($"<size=14>     <color=#00FF00>ШАХТЕРЫ</color></size>");
					foreach (var top in TopPlayer)
					{
						place += 1;
						PrintToChat($"<size=14><color=#FFFFFF>{place}.</color>  <color=#FFFFFF>{top.Ник}</color> <color=#00FF00>{top.Камень}</color></size>");
						Puts($"[ТОП Шахтер] {top.Ник}  [{top.Камень}]");
					}
				}
			}); 
		}
		
		//	Сохраняем инфу в TopData
		
        Timer saveDataBatchedTimer = null;

        // Collects all save calls within delay and saves once there are no more updates.
        void Saved(float delay = 3f)
        {
            if (saveDataBatchedTimer == null)
                saveDataBatchedTimer = timer.Once(delay, saveDataImmediate);
            else
                saveDataBatchedTimer.Reset(delay);
        }
		
        void Unload()
        {
			saveDataImmediate();
        }
		
        void OnServerShutdown()
        {
			saveDataImmediate();
        }

        void saveDataImmediate()
        {
            if (saveDataBatchedTimer != null)
            {
                saveDataBatchedTimer.DestroyToPool();
                saveDataBatchedTimer = null;
            }
            Interface.Oxide.DataFileSystem.WriteObject("TopData", Tops);
        }
		
		//	Вместо list используем hashset -> работа с большим количеством данных будет быстрее
		public HashSet<TopData> Tops = new HashSet<TopData>();
        public class TopData
        {
            public TopData(string Ник, string UID, int РакетВыпущено, int УбийствPVP, int Хедшоты, int Дистанция, int ВзрывчатокИспользовано, int УбийствЖивотных, int Смертей, int Дерево, int Камень, int Построек )
            {
                this.Ник = Ник;
                this.UID = UID;
				this.РакетВыпущено = РакетВыпущено;
				this.УбийствPVP = УбийствPVP;
				this.Хедшоты = Хедшоты;
				this.Дистанция = Дистанция;
                this.ВзрывчатокИспользовано = ВзрывчатокИспользовано;
				this.УбийствЖивотных = УбийствЖивотных; 
				this.Смертей = Смертей;
				this.Камень = Камень;
				this.Дерево = Дерево;
				this.Построек = Построек;
            }
			
			public string Ник { get; set; }
			public string UID { get; set; }
            public int РакетВыпущено { get; set; }
            public int УбийствPVP { get; set; }
			public int Хедшоты { get; set; }
			public int Дистанция { get; set; }
			public int ВзрывчатокИспользовано { get; set; }
			public int УбийствЖивотных { get; set; }
			public int Смертей { get; set; }
			public int Камень { get; set; }
			public int Дерево { get; set; }
			public int Построек { get; set; }
        }
    }
}
