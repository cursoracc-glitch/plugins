using System;
using System.Collections.Generic;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("UnlimitedFix", "Ryamkk", "2.0.5")]
	public class UnlimitedFix : RustPlugin
	{
		private Dictionary<BaseEntity, HitInfo> lastHitInfo = new Dictionary<BaseEntity, HitInfo>();
		
		[JsonProperty("Список запрещённых команд на сервере")]
		List<string> blacklistCMD = new List<string>()
		{
			"oxide.plugins",
			"o.reload",
			"o.unload",
			"o.load",
			"o.plugins",
			//"giveto",
			"plugins",
			"global.plugins",
			"oxide.load",
			"global.load",
			"oxide.reload",
			"global.reload",
			"oxide.unload",
			"global.unload",
			"oxide.grant",
			"global.grant",
			"oxide.group",
			"global.group",
			"oxide.revoke",
			"global.revoke",
			"oxide.show",
			"global.show",
			"oxide.usergroup",
			"global.usergroup",
			"oxide.version",
			"global.ownerid"
		};
		
		[JsonProperty("Список запрещённых ников на сервере")]
		private List<List<string>> ForbiddenWords = new List<List<string>>()
		{
			new List<string>() {"U-N-L-I-M-I-T-E-D"},
			new List<string>() {"UNLIMITED"},
			new List<string>() {"UNLIMITED HACK"},
			new List<string>() {"unlimited hack"},
			new List<string>() {"H-A-C-K"},
			new List<string>() {"unlimited_hack"},
			new List<string>() {"unlimited-hack"},
			new List<string>() {"vk.com/unlimited_hack"},
			new List<string>() {"unlimited-hack.cf"},
			new List<string>() {"unlimited-hack.pw"},
			new List<string>() {"[ U-N-L-I-M-I-T-E-D | H-A-C-K ]"},
			new List<string>() {"[ U-N-L-I-M-I-T-E-D| H-A-C-K ]"},
			new List<string>() {"[ U-N-L-I-M-I-T-E-D |H-A-C-K ]"},
			new List<string>() {"Ass#6143"},
			new List<string>() {"azp2033#2743"},
			new List<string>() {"KorolkovTeam"},
			new List<string>() {"Prota#7232"},
			new List<string>() {"arfinov"},
			new List<string>() {"1290"},
			new List<string>() {"UBER-RUST"},
			new List<string>() {"Unlimited Hack"},
			new List<string>() {"Unlimited"}
		};
		
		[JsonProperty("Дата файл репортов на игроков")]
		public Dictionary<ulong, NumberDetectData> NumberDetectUser = new Dictionary<ulong, NumberDetectData>();

		public class NumberDetectData
		{
			[JsonProperty("Количество детектов за большое расстояние")] public int ProjectileDistanceMore;
			[JsonProperty("Количество детектов за фейковое расстояние")] public int FakeDistance;
			[JsonProperty("Количество детектов за использование SilentAim")] public int SilentAim;
		}
		
		void ReadData()
		{
			NumberDetectUser = Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, NumberDetectData>>("UnlimitedFix/UnlimitedFixData");
		}
            
		void WriteData()
		{
			Core.Interface.Oxide.DataFileSystem.WriteObject("UnlimitedFix/UnlimitedFixData", NumberDetectUser);
		}
        
		void RegisteredDataUser(BasePlayer player)
		{
			if (!NumberDetectUser.ContainsKey(player.userID))
			{
				NumberDetectUser.Add(player.userID, new NumberDetectData
				{
					ProjectileDistanceMore = 0,
					FakeDistance = 0,
				});
			}
		}


		private void Unload()
		{
			//SendVKLogs($"Сервер: STORM RUST 236 DEV - MAIN\n UnlimitedFix( ПЕРВЫЙ СЕРВЕРНЫЙ АНТИ-ЧИТ ) unloaded");
			WriteData();
		}

		private void OnServerInitialized()
		{
			ReadData();
			SendVKLogs("Сервер: STORM RUST 236 DEV \n Сервер запущен");
			/*SendVKLogs("Initialized.")*/
			foreach (var player in BasePlayer.activePlayerList)
			{
				RegisteredDataUser(player);
			}
		}
		
		void OnPlayerInit(BasePlayer player)
		{
			RegisteredDataUser(player);
		}
		
		private object OnServerCommand(ConsoleSystem.Arg arg)
		{
			if (arg == null || arg.Connection == null || arg.cmd == null) return null;

			if (blacklistCMD.Contains(arg.cmd.FullName))
			{
				SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {arg.Connection.username} ({arg.Connection.userid}) ({arg.Connection.ipaddress.Split(':')[0]}) ввёл консольную команду {arg.cmd.FullName}");
				return true;
			}

			return null;
		}

		private object OnUserApprove(Connection connection)
		{
			foreach (var words in ForbiddenWords)
			{
				foreach (var word in words)
				{
					if (connection.username == word)
					{
						ConnectionAuth.Reject(connection, "Вы забанены на этом сервере, причина: Чит");
						connection.rejected = true;

						SendVKLogs($" Сервер: STORM RUST - 236\n Игрок {connection.username} ({connection.userid}) был заблокирован за попытку зайти с запрещённым ником {word}");
					}
				}
			}

			return null;
		}

		private void PlayerBanForbiddenWords(BasePlayer player, string message)
		{
			SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {player.displayName} ({player.userID}) был заблокирован за отправленное сообщение: {message}");
			Server.Command($"ban {player.userID} \"Вы забанены на этом сервере, причина: Чит\"");
		}

		private void OnPlayerChat(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			var message = arg.GetString(0, "text").Trim();

			if (player == null) return;
			if (arg.Args == null || arg.Args.Length == 0) return;

			foreach (var words in ForbiddenWords)
			{
				int cnt = 0;
				foreach (var word in words)
					cnt = message.Contains(word) ? cnt + 1 : 0;

				if (words.Count == cnt && cnt > 0)
				{
					timer.Once(0.1f, () => PlayerBanForbiddenWords(player, message));
					return;
				}
			}
		}

		private object OnPlayerDie(BasePlayer player, HitInfo info)
		{
			BasePlayer target = info?.HitEntity as BasePlayer;
			
			if (info == null)
			{
				if (lastHitInfo.TryGetValue(player, out info) == false)
				{
					return null;
				}
			}
			
			if (player != null || target != null || info != null || info.InitiatorPlayer != null)
			{
				float Distance = Vector3.Distance(info.InitiatorPlayer.transform.position, target.transform.position);
				float ProjectileDistance = info.ProjectileDistance;
				float CheckFakeDistance = ProjectileDistance - Distance;

				//string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?" +
				             //$"key=9F6382327AC5261B6402A13BE0248E7A&steamids={info.InitiatorPlayer.UserIDString}&personalname&format=json";
				
				string ActiveItem = info.InitiatorPlayer.GetActiveItem().info.displayName.english;
				string BoneName = info.boneName;

				if (ProjectileDistance > 350f)
				{
					NumberDetectUser[info.InitiatorPlayer.userID].ProjectileDistanceMore += 1; 
					WriteData();
					
					if (NumberDetectUser[info.InitiatorPlayer.userID].ProjectileDistanceMore >= 3)
					{
						Server.Command($"ban {info.InitiatorPlayer.userID} \"Вы забанены на этом сервере, причина: Чит\"");
					}
				}
				
				if (CheckFakeDistance > 10)
				{
					SendVKLogs($"На сервере {ConVar.Server.hostname}\n" +
					           $"\nИгрок {info.InitiatorPlayer.displayName} убил игрока {target.displayName}\n" +
					           $"SteamID Убийцы: {info.InitiatorPlayer.UserIDString}\n" +
					           $"Используя оружие: {ActiveItem}\n" +
					           $"Попал в: {BoneName}\n" +
					           $"Настоящие расстояние: {Distance}\n" +
					           $"Фейковое расстояние: {ProjectileDistance}");
					
					NumberDetectUser[info.InitiatorPlayer.userID].FakeDistance += 1; 
					WriteData();
					
					if (NumberDetectUser[info.InitiatorPlayer.userID].FakeDistance >= 8)
					{
						Server.Command($"ban {info.InitiatorPlayer.userID} \"Вы забанены на этом сервере, причина: Чит\"");
					}
				}
				
				if (ProjectileDistance > 200f)
				{
					if(info.InitiatorPlayer.userID == 76561198357402326) return null;
					
					SendVKLogs($"На сервере {ConVar.Server.hostname}\n" +
					           $"\nИгрок {info.InitiatorPlayer.displayName} убил игрока {target.displayName}\n" +
					           $"SteamID Убийцы: {info.InitiatorPlayer.UserIDString}\n" +
					           $"Используя оружие: {ActiveItem}\n" +
					           $"Попал в: {BoneName}\n" +
					           $"Расстояние убийства: {ProjectileDistance}");
				}

				foreach (var words in ForbiddenWords)
				{
					foreach (var word in words)
					{
						if (info.InitiatorPlayer.displayName == word)
						{
							Server.Command($"ban {info.InitiatorPlayer.userID} \"Вы забанены на этом сервере, причина: Чит\"");
						}
					}
				}

				/*webrequest.EnqueueGet(url, (code, response) =>
				{
					JObject InfoResponse = JObject.Parse(response);
					foreach (var item in InfoResponse["response"]["players"])
					{
						DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToDouble(item["timecreated"]));

						if (info.InitiatorPlayer.displayName != item["personaname"].ToString())
						{
							SendVKLogs($"На сервере {ConVar.Server.hostname}\n" +
							           $"\nИгрок {info.InitiatorPlayer.displayName} убил игрока {victim.ToPlayer().displayName} " +
							           $"используя оружие {info.InitiatorPlayer.GetActiveItem().info.displayName.english}\n" +
							           $"\nУ игрока {info.InitiatorPlayer.displayName} отличается ник на сервере.\n" +
							           $"Расстояние убийства: {ProjectileDistance}\n" +
							           $"Его настоящий ник в стиме: {item["personaname"]}\n" +
							           $"Его настоящий Steam ID: {item["steamid"]}\n" +
							           $"Его настоящий IP addres: {info.InitiatorPlayer.net.connection.ipaddress.Split(':')[0]}\n" +
							           $"Дата создания аккаунта: {date}\n" +
							           $"\nСсылка на его Steam Profile:\n{item["profileurl"]}");
							return;
						}
					}
				}, this);*/
			}
			
			return null;
		}

		[ConsoleCommand("testvk")]
		void testvk(ConsoleSystem.Arg args)
		{
			SendVKLogs($"ТЕСТОВОЕ СООБЩЕНИЕ ПРИШЛО СУДЫ,А НЕКИЙ КИРИЛЛ БАКЛАНОВ ХОХОЛ РУССКИЙ");
		}

		private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
		{
		    BasePlayer target = info?.HitEntity as BasePlayer;

		    if (target != null)
		    {
		        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
		        float _distance = info.ProjectileDistance;
		        float Distance = distance - _distance;
		        
		        //float CheckFakePlayerModelV1 = Vector3.Distance(target.transform.position, info.HitPositionWorld);
		        //float CheckFakePlayerModelV2 = info.HitEntity.Distance2D(info.HitPositionWorld);
		        
		        //SendVKLogs($"Игрок {attacker.displayName} тест1: {CheckFakePlayerModelV1}, тест2: {CheckFakePlayerModelV2}");

		        if (Distance > 35f || Distance < -35f)
		        {
		            SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {attacker.displayName} ({attacker.UserIDString}) подозревается в использовании SilentAim #1");
		            NumberDetectUser[info.InitiatorPlayer.userID].SilentAim += 1; 
		            WriteData();
		            
		            if (NumberDetectUser[info.InitiatorPlayer.userID].SilentAim >= 5)
		            {
			            SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {attacker.displayName} ({attacker.UserIDString}) был заблокирован за использование SilentAim #1");
			            Server.Command($"ban {attacker.UserIDString} \"Вы забанены на этом сервере, причина: Чит\"");
		            }
		        }
		        
		        if (_distance < 1f && distance > 6f)
		        {
		            SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {attacker.displayName} ({attacker.UserIDString}) подозревается в использовании SilentAim #2");
		            NumberDetectUser[info.InitiatorPlayer.userID].SilentAim += 1; 
		            WriteData();
		            
		            if (NumberDetectUser[info.InitiatorPlayer.userID].SilentAim >= 8)
		            {
			            SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {attacker.displayName} ({attacker.UserIDString}) был заблокирован за использование SilentAim #2");
			            Server.Command($"ban {attacker.UserIDString} \"Вы забанены на этом сервере, причина: Чит\"");
		            }
		        };
		        
		        if (info.HitEntity.Distance(target) < 15f) return;
		        
		        if (Mathf.Abs(info.HitPositionWorld.y - info.HitEntity.CenterPoint().y) >= 2.05f)
		        {
		            SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {attacker.displayName} ({attacker.UserIDString}) подозревается в использовании SilentAim #3");
		            NumberDetectUser[info.InitiatorPlayer.userID].SilentAim += 1; 
		            WriteData();
		            
		            if (NumberDetectUser[info.InitiatorPlayer.userID].SilentAim >= 8)
		            {
			            SendVKLogs($"Сервер: STORM RUST - 236\n Игрок {attacker.displayName} ({attacker.UserIDString}) был заблокирован за использование SilentAim #3");
			            Server.Command($"ban {attacker.UserIDString} \"Вы забанены на этом сервере, причина: Чит\"");
		            }
		        }
		    }
		}

		private string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }
        
        void SendVKLogs(string msg)
            {
  			int RandomID = UnityEngine.Random.Range(0, 9999);
            int id = 4;
            string token = "vk1.a.tcoWQY16tiO4ql598VY8ZojOrDShgLJF1vCtogWQYDdDSFE0fh5PBmo6qu8eg4SXvdctglNooHJ6Dcj0vMCuGS_b47KzKPU8iEhezGAUdG07eTq_6Lyw7BmzdmkOXqBVKECRwOFZ4iAYEwlI2LJ5msAxN5yHj5H_KkHP7SEPMUk_HfWk6caWxEqPsv_5nBI8hDxNLDRvlG14vOdgr1S1gQ";
            while (msg.Contains("#"))
                msg = msg.Replace("#", "%23");
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={id}&random_id={RandomID}&message={msg}&access_token={token}&v=5.92", null, (code, response) => { }, this);
        }
    }
}