using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;  
using Oxide.Core.Plugins;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;
using Oxide.Core.Libraries;
using System.Linq;
using System.Globalization;
namespace Oxide.Plugins
{
    [Info("RaidNotice", "S1m0n", "1.0.0")]
    [Description("RaidNotice for players")]
	class RaidNotice : RustPlugin 
    {
		string permissionvk = "raidnotice.allowed.vk";
		string permissionphone = "raidnotice.allowed.phone";
		string raidnotice = "На ваш дом напали!";
		Dictionary<ulong, int> playersCodePhone = new Dictionary<ulong, int>();
		Dictionary<ulong, int> playersCodeVk = new Dictionary<ulong, int>();
		Dictionary<BasePlayer, float> lastTimeSended = new Dictionary<BasePlayer, float>();
		public Dictionary<ulong, string> tempvk = new Dictionary<ulong, string>();
		public Dictionary<ulong, string> tempphone = new Dictionary<ulong, string>();
		
		List<string> allowedentity = new List<string>()
		{
			"door",
			"wall.window.bars.metal",
			"wall.window.bars.toptier",
			"wall.external",
			"gates.external.high",
			"floor.ladder",
			"embrasure",
			"floor.grill",
			"wall.frame.fence",
			"wall.frame.cell",
		};
		
		class StoredData
		{ 
			public Dictionary<ulong, DateTime> raidCDVK = new Dictionary<ulong, DateTime>();
			public Dictionary<ulong, DateTime> raidCDPHONE = new Dictionary<ulong, DateTime>();
			public Dictionary<ulong, DateTime> msgVKCD = new Dictionary<ulong, DateTime>();
			public Dictionary<ulong, DateTime> msgPHONECD = new Dictionary<ulong, DateTime>();
			public Dictionary<ulong, int> addMAX = new Dictionary<ulong, int>();
			public Dictionary<ulong, string> vkids = new Dictionary<ulong, string>();
			public Dictionary<ulong, string> phones = new Dictionary<ulong, string>();
		}
		  
		StoredData db;  
		
		void SaveData()
        {
			Interface.Oxide.DataFileSystem.WriteObject("RaidNotice", db);
		}
		
		void Loaded()
		{
			db = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("RaidNotice");
		}
		
		void Unloaded()
		{
			SaveData();
		}
		
		void OnServerSave()
		{
			SaveData();
		}
		
		void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
		{
			if (hitInfo == null) return;
			if (hitInfo.Initiator?.ToPlayer() == null) return;
			if (hitInfo.Initiator?.ToPlayer().userID == entity.OwnerID) return;
			if (hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Explosion && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Heat && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Bullet) return; 
			if (entity is BaseEntity)
			{ 
				BuildingBlock block = entity.GetComponent<BuildingBlock>();
				if (block != null)
				{
					if (block.currentGrade.gradeBase.type.ToString() == "Twigs" || block.currentGrade.gradeBase.type.ToString() == "Wood")
					{
						return;
					}						
				}
				else
				{
					bool ok = false;
					foreach (var ent in allowedentity)
					{
						if (entity.LookupPrefab().name.Contains(ent))
						{
							ok = true;
						}
					}
					if(!ok) return;
				}
				if (entity.OwnerID == null && entity.OwnerID == 0) return;
				if(!isOnline(entity.OwnerID))
				{
					SendOfflineMessage(entity.OwnerID);
				}
				BasePlayer player = FindOnlinePlayer(entity.OwnerID.ToString());
				if(player != null)
				{
					Msg(player);
				}
			}
		}
		
		[ChatCommand("rn")]
		void rn(BasePlayer player, string command, string[] arg)
		{ 
			if (!permission.UserHasPermission(player.userID.ToString(), permissionvk) && !permission.UserHasPermission(player.userID.ToString(), permissionphone))
			{
				PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Купить эту услугу вы можете в магазине сервера");
				return; 
			}
			string   = null;
			db.vkids.TryGetValue(player.userID, out value);
			string valuephone = null;
			db.phones.TryGetValue(player.userID, out valuephone);
			if (arg.Length == 0)
			{
				if (value == null && valuephone == null)
				{
					PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Напишите /rn add +12345678910 или /rn add vk.com/id123456789");
					return;
				}
				if (value != null && valuephone != null)
				{
					PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы указали:\nНомер телефона: <color=#a2d953>{valuephone}</color>\nВК: <color=#a2d953>{value}</color>");
					return;
				}
				if (value != null)
				{
					PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Ваш вк указан как: <color=#a2d953>{value}</color>");
					return;
				}
				if (valuephone != null)
				{
					PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Ваш номер телефона указан как: <color=#a2d953>{valuephone}</color>");
					return;
				}
			}
			
			if (arg.Length > 0)
			{
				if ((string) arg[0].ToLower() == "delete")
				{ 
					if (arg.Length == 2)
					{ 
						#region PHONE
						if ((string) arg[1].ToLower() == "phone")
						{
							string tempvid;
							tempphone.TryGetValue(player.userID, out tempvid);
							string vid;
							db.phones.TryGetValue(player.userID, out vid);
							if (tempvid == null && vid == null)
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: У вас нет привязанного телефона");
								return;
							} 
							DateTime cd;
							if(db.msgPHONECD.TryGetValue(player.userID, out cd))
							{
								var howmuch = cd - DateTime.UtcNow;
								if(howmuch.Minutes > -5 )
								{
									PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы сможете удалить свой номер телефона через <color=#CD2626>{howmuch.Minutes + 5} мин.</color>");
									return;
								}
							}
							tempphone.Remove(player.userID);
							db.phones.Remove(player.userID);
							PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Ваш номер телефона был удален. Укажите новый командой /rn add");
							return;
						}
						#endregion
						
						#region VK
						if ((string) arg[1].ToLower() == "vk")
						{
							string tempvid;
							tempvk.TryGetValue(player.userID, out tempvid);
							string vid;
							db.vkids.TryGetValue(player.userID, out vid);
							if (tempvid == null && vid == null)
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: У вас нет привязанного VK");
								return;
							}
							DateTime cd;
							if(db.msgVKCD.TryGetValue(player.userID, out cd))
							{
								var howmuch = cd - DateTime.UtcNow;
								if(howmuch.Minutes > -5 )
								{
									PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы сможете удалить свой VK через <color=#CD2626>{howmuch.Minutes + 5} мин.</color>");
									return;
								}
							}
							tempvk.Remove(player.userID);
							db.vkids.Remove(player.userID);
							PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Ваш VK был отвязан. Укажите новый командой /rn add");
							return;
						}
						#endregion
					}
					else
					{
						PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Напишите:\n/rn delete vk - отвязать вк\n/rn delete phone - отвязать номер");
						return;
					}
				}
			}
			
			if (arg.Length > 0)
			{
				if ((string) arg[0].ToLower() == "accept")
				{
					if (arg.Length == 2)
					{ 
						if(string.IsNullOrEmpty(arg[1]))
						{
							PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Вы не указали проверочный код");
							return;
						}
						int code = 0;
						#region VK
						if (playersCodeVk.TryGetValue(player.userID, out code))
						{ 	
							if ((string) arg[1] == playersCodeVk[player.userID].ToString())
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Отлично! Ваш VK подтвержден!");
								db.vkids[player.userID] = tempvk[player.userID];
								playersCodeVk.Remove(player.userID);
								tempvk.Remove(player.userID);
								return;
							}
							else
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Неправильный код подтверждения");
								return;
							}
						}
						#endregion
						
						#region PHONE
						if (playersCodePhone.TryGetValue(player.userID, out code))
						{ 
							if ((string) arg[1] == playersCodePhone[player.userID].ToString())
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Отлично! Ваш номер телефона подтвержден!");
								db.phones[player.userID] = tempphone[player.userID];
								playersCodePhone.Remove(player.userID);
								tempphone.Remove(player.userID);
								return;
							}
							else
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Неправильный код подтверждения");
								return;
							}
						}
						PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Произведите операцию заново!");
						return;
						
						#endregion
					}
					else
					{
						PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Вы не указали проверочный код");
						return;
					}
				}
				
				if ((string) arg[0].ToLower() == "add")
				{
					if (arg.Length == 2)
					{
						string id = (string) arg[1];
						if(string.IsNullOrEmpty(id))
						{
							PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Ошибка при вводе");
							return;
						}
						int codevk = 0;
						playersCodeVk.TryGetValue(player.userID, out codevk);
						int codephone = 0;
						playersCodePhone.TryGetValue(player.userID, out codephone);
						
						if (codevk != 0 || codephone != 0)
						{
							PrintToChat(player, "Завершите текущее подтверждение");
							return;
						}
						int valuemax = 0;
						if (!db.addMAX.TryGetValue(player.userID, out valuemax))
						{
							db.addMAX[player.userID] = 1;
						}
						else
						{
							if (db.addMAX[player.userID] == 5)
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Ошибка! Обратитесь к администратору");
								PrintError($"{player.userID.ToString()} пытался добавить больше 3 раз");
								return;
							}
							db.addMAX[player.userID] += 1;
						}
						
						#region VK
						if (id.ToLower().Contains("vk.com/")) 
						{
							if (!permission.UserHasPermission(player.userID.ToString(), permissionvk))
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: У вас нет привелегий для добавления ВК");
								return;
							}
							string valueid;
							if (db.vkids.TryGetValue(player.userID, out valueid))
							{
								PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: У вас уже привязан VK: <color=#a2d953>{valueid}</color>");
								return;
							}
							
							int val = 0;
							if(playersCodeVk.TryGetValue(player.userID, out val))
							{ 
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Вам уже отправлено сообщение на ваш адрес VK.\nПодождите <color=#CD2626>5 минут</color> прежде чем вк можно будет указать заного");
								return;
							} 
							
							db.msgVKCD[player.userID] = DateTime.UtcNow;
							tempvk[player.userID] = id;
							int code = UnityEngine.Random.Range(1000,9999);
							playersCodeVk[player.userID] = code;
							
							GetRequest(player, id, playersCodeVk[player.userID].ToString(), "0");
							return;
						} 
						#endregion
						  
						#region PHONE
						String cont = id.Substring(0, 1);
						if (id.ToLower().Contains("+") && id.Length > 9) 
						{
							if (!permission.UserHasPermission(player.userID.ToString(), permissionphone))
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: У вас нет привелегий для добавления телефона");
								return;
							}
							string valueid;
							if (db.phones.TryGetValue(player.userID, out valueid))
							{
								PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: У вас уже привязан номер: <color=#a2d953>{valueid}</color>");
								return;
							}
							
							int val = 0;
							if(playersCodePhone.TryGetValue(player.userID, out val))
							{ 
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Вам уже отправлено сообщение на ваш номер.\nПодождите <color=#CD2626>5 минут</color> прежде чем номер можно будет указать заного");
								return;
							}
							
							db.msgPHONECD[player.userID] = DateTime.UtcNow;
							if (id.Contains("+"))
							{ 
								id = id.Trim( new Char[] { '+'});
							}
							if (!id.All(char.IsDigit))
							{
								PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Неверный номер телефона!");
								return;
							}
							tempphone[player.userID] = id;
							int code = UnityEngine.Random.Range(1000,9999);
							playersCodePhone[player.userID] = code;
							
							GetRequest(player, id, playersCodePhone[player.userID].ToString(), "1");
						}
						else 
						{
							PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Вы неверно ввели номер телефона. Перед номером указывайте +\nНапример +1234567890");
							return;
						}
						#endregion
					}
					else
					{
						PrintToChat(player, "<color=#008B8B>[Оповещение-Рейда]</color>: Вы не указали номер телефона или VK ID");
						return;
					} 
				} 
			}
		}  
		
		void GetRequest(BasePlayer player, string id, string key, string device)
        {
			webrequest.EnqueueGet($"http://art3m4z7.beget.tech/sendrsrn.php?id={id}&key={key}&seckey=raidnotice1111124141&device={device}", (code, response) => GetCallback(code, response, player, id, device), this);
        }

        void GetCallback(int code, string response, BasePlayer player, string id, string device)
        {  
            if (response == null || code != 200)
            { 
                PrintError($"Ошибка для {player.displayName}");
                return;
            }
			if (player == null)
			{
				return;
			}
			if (response.Contains("Good"))
			{  
				if (device == "0")
				{  
					PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы указали VK: <color=#a2d953>{id}</color>\nВам в VK отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
				}
				if (device == "1")
				{
					PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы указали телефон: <color=#a2d953>{id}</color>\nВам на телефон отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
				}
				return;
			}
			if (response.Contains("PrivateMessage"))
			{
				tempvk.Remove(player.userID);
				playersCodeVk.Remove(player.userID);
				db.msgVKCD.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Ваши настройки приватности не позволяют отправить вам сообщение (<color=#a2d953>{id}</color>)");
				return;
			}
			if (response.Contains("ErrorSend"))
			{
				tempvk.Remove(player.userID);
				playersCodeVk.Remove(player.userID);
				db.msgVKCD.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nПроверьте правильность ссылки (<color=#a2d953>{id}</color>) или повторите позже");
				return;
			}
			if (response.Contains("BlackList"))
			{
				tempvk.Remove(player.userID);
				playersCodeVk.Remove(player.userID);
				db.msgVKCD.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nВы добавили бота в черный список");
				return;
			}
			if (response.Contains("BadPhone"))
			{
				tempphone.Remove(player.userID);
				playersCodePhone.Remove(player.userID);
				db.msgPHONECD.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы указали неправильный номер телефона. Повторите попытку");
				return;
			}
			if (response.Contains("BadBalance"))
			{
				tempvk.Remove(player.userID);
				playersCodeVk.Remove(player.userID);
				db.msgVKCD.Remove(player.userID);
				tempphone.Remove(player.userID);
				playersCodePhone.Remove(player.userID);
				db.msgPHONECD.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Произошла ошибка! Обратитесь к администратору");
				PrintError("Баланс для смсок иссяк");
				return;
			}
			
			if (device == "0")
			{
				playersCodeVk.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы указали неверный VK ID (<color=#a2d953>{id}</color>)");
			}
			if (device == "1")
			{
				playersCodePhone.Remove(player.userID);
				PrintToChat(player, $"<color=#008B8B>[Оповещение-Рейда]</color>: Вы указали неверный номер (<color=#a2d953>{id}</color>)");
			}
		}
		
		void SendMsg(BasePlayer player)
		{
			float value = 0;
			if (lastTimeSended.TryGetValue(player, out value))
			{
				if (lastTimeSended[player] + 1 > UnityEngine.Time.realtimeSinceStartup)
				{
					return;
				}
				Msg(player);
				lastTimeSended[player] = UnityEngine.Time.realtimeSinceStartup;
			}
			else
			{
				Msg(player);
				lastTimeSended[player] = UnityEngine.Time.realtimeSinceStartup;
			}
		}
		
		bool isOnline(ulong id)
		{
			foreach(BasePlayer active in BasePlayer.activePlayerList)
			{
				if (active.userID == id) return true;
			}
			return false;
		}
		
		public static BasePlayer FindOnlinePlayer(string nameOrIdOrIp)
        { 
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            return null;
        } 
		  
		void SendOfflineMessage(ulong id)
		{ 
			string value = null;
			db.vkids.TryGetValue(id, out value);
			string valuephone = null;
			db.phones.TryGetValue(id, out valuephone);
			 
			if (value != null || valuephone != null)
			{ 
				DateTime valueCDVK;
				if (!db.raidCDVK.TryGetValue(id, out valueCDVK))
				{ 
					if (permission.UserHasPermission(id.ToString(), permissionvk) && value != null) 
					{
						GetRequest(null, value, "1", "0");
						db.raidCDVK[id] = DateTime.UtcNow;
					} 
				}
				DateTime valueCDPHONE;
				if (!db.raidCDPHONE.TryGetValue(id, out valueCDPHONE))
				{
					if (permission.UserHasPermission(id.ToString(), permissionphone) && valuephone != null)
					{
						db.raidCDPHONE[id] = DateTime.UtcNow;
						timer.Once(5f, () => GetRequest(null, valuephone, "1", "1"));
					}
				} 
				
				var howmuchVK = valueCDVK - DateTime.UtcNow;
				var howmuchPHONE = valueCDPHONE - DateTime.UtcNow;
                if (howmuchVK.Hours <= -1 && howmuchVK.Minutes <= -1)
				{ 
					if (permission.UserHasPermission(id.ToString(), permissionvk) && value != null) 
					{
						db.raidCDVK[id] = DateTime.UtcNow;
						GetRequest(null, value, "1", "0");
					}
				}
					
				if (howmuchPHONE.Hours <= -6 && howmuchPHONE.Minutes <= 1)
				{
					if (permission.UserHasPermission(id.ToString(), permissionphone) && valuephone != null)
					{
						db.raidCDPHONE[id] = DateTime.UtcNow;
						timer.Once(5f, () => GetRequest(null, valuephone, "1", "1"));
					}
				}
			}
		}
		
		void Msg(BasePlayer player)
		{
			if (permission.UserHasPermission(player.userID.ToString(), permissionvk)) 
			{
				PrintToChat(player, raidnotice);
				return;
			}
			if (permission.UserHasPermission(player.userID.ToString(), permissionphone))
			{
				PrintToChat(player, raidnotice);
			}
		}
		
		void OnServerInitialized() 
		{
			if (!permission.PermissionExists(permissionvk)) permission.RegisterPermission(permissionvk, this);
			if (!permission.PermissionExists(permissionphone)) permission.RegisterPermission(permissionphone, this);
		}
		
	}
}