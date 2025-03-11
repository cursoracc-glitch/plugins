using System.Collections;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Collections.Generic;
		   		 		  						  	   		  	  			  		  		  	 	 		  			 
namespace Oxide.Plugins 
{
    [Info("XFarmRoom", "Monster", "1.0.1")]
    class XFarmRoom : RustPlugin
    {
		
		private bool CanPickupEntity(BasePlayer player, BaseEntity entity)
		{
			if(entity.OwnerID == 100002)
				return false;
			
			return true;
		}
		
		private void ChatMessage(BasePlayer player, string message) => Player.Reply(player, config.Setting.PrefixChat + message, config.Setting.SteamID);
		 
		protected override void LoadConfig()
        {
            base.LoadConfig(); 
			 
			try
			{
				config = Config.ReadObject<FarmRoomConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		
		private void OnPlayerDeath(BasePlayer player, HitInfo info)
		{
			if(player != null && _players_in_room.ContainsKey(player.userID))
				LeaveFarmRoom(player);
		}
		
		private void Unload()
		{
			if(_coroutine != null)
				ServerMgr.Instance.StopCoroutine(_coroutine);
			
			foreach(var player_in_room in _players_in_room)
			{
				BasePlayer player = BasePlayer.FindByID(player_in_room.Key) ?? BasePlayer.FindSleeping(player_in_room.Key);
				
				if(player != null)
				{
					LeaveFarmRoom(player, true);
					CuiHelper.DestroyUi(player, ".GUI_FARMROOM");
				}
			}
				
			SaveData();
		}
		internal class PrefabsRoom
		{
			
			public PrefabsRoom(string shortname, Vector3 pos, Vector3 rot)
			{
				ShortPrefabName = shortname; Position = pos; Rotation = rot;
			}
			public string ShortPrefabName;
			public Vector3 Rotation;
			public Vector3 Position;
		}
		
		[ConsoleCommand("xfarmroom_give_ore")]
		void ccmdGiveOre(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(player == null || player.IsAdmin)
				if(args.Args != null && args.Args.Length >= 3)
				{
					ulong steamID;
					ulong.TryParse(args.Args[0], out steamID);
					
					string ore_shortname = args.Args[1];
					
					int amount;
					int.TryParse(args.Args[2], out amount);
					
					if(!_ores_shortname.Contains(ore_shortname)) return;
					
					if(StoredData.ContainsKey(steamID))
						StoredData[steamID].Ore[ore_shortname] += amount;
					else
						if(steamID.IsSteamId())
						{
							StoredData.Add(steamID, new Data(0, new Dictionary<string, int>{ ["sulfur-ore"] = 0, ["metal-ore"] = 0, ["stone-ore"] = 0 }));
							StoredData[steamID].Ore[ore_shortname] += amount;
						}
				}
		}
		
		[ConsoleCommand("xfarmroom_clear_ore")]
		void ccmdClearOre(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(player == null || player.IsAdmin)
				if(args.Args != null && args.Args.Length >= 1)
				{
					ulong steamID;
					ulong.TryParse(args.Args[0], out steamID);
					
					if(StoredData.ContainsKey(steamID))
						StoredData[steamID].Ore = new Dictionary<string, int>{ ["sulfur-ore"] = 0, ["metal-ore"] = 0, ["stone-ore"] = 0 };
				}
		}
		
		private object CanBuild(Planner planner)
		{
			BasePlayer player = planner.GetOwnerPlayer();
			
			if(player != null && _players_in_room.ContainsKey(player.userID))
				return true;
			
			return null;
		}
		
		private void RemoveRoomOrEntity(Vector3 position)
		{
			List<BaseEntity> list_entity = new List<BaseEntity>();
			Vis.Entities(position, 10, list_entity);
			
			list_entity = list_entity.Distinct().ToList();
			list_entity = list_entity.Where(x => !(x is BasePlayer) && !(x is PlayerCorpse) && !(x is DroppedItemContainer) && !(x is DroppedItem)).ToList();
			
			foreach(BaseEntity entity in list_entity)
				if(entity != null && !entity.IsDestroyed)
					entity.Kill();
		}
		
				
				
		[ChatCommand("roomtp")]
		void cmdTPFarmRoom(BasePlayer player)
		{
			if(player.IsDead()) return;
			
			if(!permission.UserHasPermission(player.UserIDString, "xfarmroom.use"))
			{
				ChatMessage(player, lang.GetMessage("CHAT_NO_PERM", this, player.UserIDString));
				return;
			}
			
			if(!_players_in_room.ContainsKey(player.userID))
			{
				if(Cooldowns.ContainsKey(player.userID))
				{
					int cd = permission.UserHasPermission(player.UserIDString, "xfarmroom.nocdtp") ? 5 : config.Setting.CDTPRoom;
					
					if(DateTimeOffset.Now.ToUnixTimeSeconds() - Cooldowns[player.userID] <= cd)
					{
						ChatMessage(player, string.Format(lang.GetMessage("CHAT_CD_TP", this, player.UserIDString), Cooldowns[player.userID] + cd - DateTimeOffset.Now.ToUnixTimeSeconds()));
						
						return;
					}
				}
				
				if(config.Setting.UseMaxCountItem && player.inventory.AllItems().Count() > config.Setting.MaxCountItem)
				{
					ChatMessage(player, string.Format(lang.GetMessage("CHAT_LIMIT_ITEMS", this, player.UserIDString), config.Setting.MaxCountItem));
					
					return;
				}
				
				bool no_permission = GetOresPermission(player);
				
				if(!StoredData.ContainsKey(player.userID))
				{
					ChatMessage(player, lang.GetMessage("CHAT_NO_ALL_PERM", this, player.UserIDString));
					
					return;
				}
				
				if(StoredData[player.userID].Ore.Values.Sum() <= 0)
				{
					if(no_permission)
						ChatMessage(player, lang.GetMessage("CHAT_NOT_ORES_NOT_PERM", this, player.UserIDString));
					else
						if(config.Setting.UpdateOption)
							ChatMessage(player, lang.GetMessage("CHAT_NOT_ORES_WIPE", this, player.UserIDString));
						else
							ChatMessage(player, string.Format(lang.GetMessage("CHAT_NOT_ORES_UPDATE", this, player.UserIDString), StoredData[player.userID].LastUpdate + config.Setting.UpdateSecond - DateTimeOffset.Now.ToUnixTimeSeconds()));
					
					return;
				}
				
				if(_spawn_room_position.Count != 0)
				{
					Vector3 position = _spawn_room_position.GetRandom();
					_spawn_room_position.Remove(position);
				
					_players_in_room.Add(player.userID, new PlayersInRoom(position, player.transform.position, GetOres(player.userID)));
				
					SpawnFarmRoom(position);
					SpawnOre(player.userID);
					TP(player, position);
				
					GUIFarmRoom(player);
					
					ChatMessage(player, lang.GetMessage("CHAT_JOIN_ROOM", this, player.UserIDString));
				}
				else
					ChatMessage(player, lang.GetMessage("CHAT_NO_ROOMS", this, player.UserIDString));
			}
			else
				ChatMessage(player, lang.GetMessage("CHAT_ALREADY_IN_ROOM", this, player.UserIDString));
		}
		
		private void TP(BasePlayer player, Vector3 position)
		{
			try
			{
				if(player.IsSleeping())
				{
					player.RemoveFromTriggers();
					player.Teleport(position);
					
					if (!IsInvisible(player))
					{
						player.UpdateNetworkGroup();
						player.SendNetworkUpdateImmediate(false);
					}
				}
				else
                {
                    player.UpdateActiveItem(v);
					player.EnsureDismounted();
		   		 		  						  	   		  	  			  		  		  	 	 		  			 
					if (player.HasParent())
						player.SetParent(null, true, true);
	
					player.EndLooting();
					player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
					player.CancelInvoke("InventoryUpdate");
					player.CancelInvoke("TeamUpdate");
	
					player.RemoveFromTriggers();
					player.Teleport(position);

					player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
					player.ClientRPCPlayer(null, player, "StartLoading");
					player.SendEntityUpdate();
		   		 		  						  	   		  	  			  		  		  	 	 		  			 
					if (!IsInvisible(player))
					{
						player.UpdateNetworkGroup();
						player.SendNetworkUpdateImmediate(false);
					}
				}
			}
			finally
			{
				if (!IsInvisible(player))
					player.ForceUpdateTriggers();
			}
		}
        protected override void SaveConfig() => Config.WriteObject(config);
		public List<Vector3> _spawn_room_position = new List<Vector3>();
		private void OnUserPermissionRevoked(string id, string permName) => CheckPermission(id, permName, false);
		
		private bool IsInvisible(BasePlayer player)
        {
            return Vanish != null && Convert.ToBoolean(Vanish.Call("IsInvisible", player));
        }
		
		private List<string> GetOres(ulong userID)
		{
			List<string> list_ore = new List<string>();
			
			foreach(var ore in StoredData[userID].Ore)
			{
				if(ore.Value > 0)
					list_ore.Add(ore.Key);
			}
			
			return list_ore;
		}
		
				
				
		private void OnUserPermissionGranted(string id, string permName) => CheckPermission(id, permName, true);
		
		private void SpawnOre(ulong userID)
		{
			StagedResourceEntity ore = GameManager.server.CreateEntity($"assets/bundled/prefabs/autospawn/resource/ores/{_players_in_room[userID].OresSpawn.GetRandom()}.prefab", _players_in_room[userID].RoomPosition + new Vector3(3, 0.5f, 0)) as StagedResourceEntity;
			
			ore.OwnerID = 10009;
			ore.Spawn();
		}
		
		private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if(entity.OwnerID == 100002)
				return true;
			
			return null;
		}
		
		private void CheckGroup(string id, string groupName, bool added)
		{
			if(permission.GroupHasPermission(groupName, "xfarmroom.use"))
			{
				BasePlayer player = BasePlayer.FindByID(ulong.Parse(id));
				
				if(player != null)
					if(added)
					{
						if(config.Setting.ChatMessages)
							ChatMessage(player, lang.GetMessage("GRANTED_PERM", this, player.UserIDString));
					}
					else if(!added && !permission.UserHasPermission(player.UserIDString, "xfarmroom.use"))
					{
						if(config.Setting.ChatMessages)
							ChatMessage(player, lang.GetMessage("REVOKED_PERM", this, player.UserIDString));
						
						if(_players_in_room.ContainsKey(player.userID))
							LeaveFarmRoom(player);
					}
			}
		}
		
				
				
		private object OnServerCommand(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(player != null && player.IsAdmin)
				return null;
			if(player != null && !config.Setting.ConsoleCommand.Contains(args.cmd.FullName) && _players_in_room.ContainsKey(player.userID))
			{
				player.SendConsoleCommand($"echo [ <color=white>FARMROOM</color> ] - {lang.GetMessage("CMD_BLOCK", this, player.UserIDString)}");
				
				return true;
			}
		   		 		  						  	   		  	  			  		  		  	 	 		  			 
			return null;
		}
		
		internal class PlayersInRoom
		{
			public Vector3 RoomPosition;
			public Vector3 LastPlayerPosition;
			public List<string> OresSpawn;
			
			public PlayersInRoom(Vector3 roomposition, Vector3 lastplayerposition, List<string> oresspawn)
			{
				RoomPosition = roomposition; LastPlayerPosition = lastplayerposition; OresSpawn = oresspawn;
			}
		}
		
		private bool CheckPosition(Vector3 position)
		{
			List<BaseEntity> list_entity = new List<BaseEntity>();
			Vis.Entities(position, 10, list_entity);
			
			return list_entity.Count == 0;
		}
		
				
				
		private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_LEAVE"] = "LEAVE",
				["CHAT_UPDATE_ORES"] = "The number of available ores has been updated!",
				["CHAT_JOIN_ROOM"] = "You have joined the farm room!",
				["CHAT_LEAVE_ROOM"] = "You have leaved the farm room!",
				["CHAT_NO_PERM"] = "You do not have access to the farm room!",
				["CHAT_NO_ALL_PERM"] = "You do not have all permissions to access the farm room!\n\n<size=10>We did not find you in the DB, you need to purchase ores.</size>",
				["CHAT_SPAWN_POINTS"] = "Unoccupied spawn points  -  {0}",
				["CHAT_NO_ROOMS"] = "All farm rooms are busy, please try again later!",
				["CHAT_ALREADY_IN_ROOM"] = "You are already in the farm room!",
				["CHAT_NOT_IN_ROOM"] = "You are not in the farm room!",
				["CHAT_CD_TP"] = "To get back into the farm room, wait  -  {0} sec.",
				["CHAT_LIMIT_ITEMS"] = "A maximum of {0} items can be taken into a room!",
				["CHAT_NOT_ORES_NOT_PERM"] = "0 ores available!\n<size=12>You can purchase limited/unlimited ores in our shop!</size>",
				["CHAT_NOT_ORES_WIPE"] = "0 ores available!\n<size=12>Update in the next wipe!</size>",
				["CHAT_NOT_ORES_UPDATE"] = "0 ores available!\n<size=12>Update after {0} sec.",
				["CMD_BLOCK"] = "It is forbidden to use this command in the farm room!",
				["GRANTED_PERM"] = "You have access to the farm room functionality!\n\n<size=10>This message will appear when granting access by any means. There may be several.</size>",
				["REVOKED_PERM"] = "You no longer have access to the farm room functionality!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_LEAVE"] = "ПОКИНУТЬ",
				["CHAT_UPDATE_ORES"] = "Кол-во доступных камней обновлено!",
				["CHAT_JOIN_ROOM"] = "Вы вошли в фарм комнату!",
				["CHAT_LEAVE_ROOM"] = "Вы покинули фарм комнату!",
				["CHAT_NO_PERM"] = "У вас нет доступа к фарм комнате!",
				["CHAT_NO_ALL_PERM"] = "У вас нет всех разрешений для доступа к фарм комнате!\n\n<size=10>Мы не нашли вас в БД, вам необходимо приобрести камни.</size>",
				["CHAT_SPAWN_POINTS"] = "Свободных точек для спавна  -  {0}",
				["CHAT_NO_ROOMS"] = "Все фарм комнаты заняты, попробуйте позже!",
				["CHAT_ALREADY_IN_ROOM"] = "Вы уже находитесь в фарм комнате!",
				["CHAT_NOT_IN_ROOM"] = "Вы не находитесь в фарм комнате!",
				["CHAT_CD_TP"] = "Чтобы снова попасть в фарм комнату подождите  -  {0} сек.",
				["CHAT_LIMIT_ITEMS"] = "В комнату можно взять максимум {0} предмета!",
				["CHAT_NOT_ORES_NOT_PERM"] = "Доступно 0 камней!\n<size=12>Вы можете приобрести ограниченное/неограниченное кол-во камней в нашем магазине!</size>",
				["CHAT_NOT_ORES_WIPE"] = "Доступно 0 камней!\n<size=12>Обновление в следующем вайпе!</size>",
				["CHAT_NOT_ORES_UPDATE"] = "Доступно 0 камней!\n<size=12>Обновление через {0} сек.</size>",
				["CMD_BLOCK"] = "В фарм комнате запрещено использовать данную команду!",
				["GRANTED_PERM"] = "У вас появился доступ к функционалу фарм комнаты!\n\n<size=10>Данное сообщение будет появляться при выдаче доступа любыми методами. Их может быть несколько.</size>",
				["REVOKED_PERM"] = "Вы больше не имеете доступа к функционалу фарм комнаты!"
            }, this, "ru");
        }
		
		private Dictionary<ulong, Data> StoredData = new Dictionary<ulong, Data>();
		
		[ChatCommand("roomspawns")]
		void cmdSpawnRoomPosition(BasePlayer player)
		{
			if(player.IsAdmin)
			{
				foreach(var position in _spawn_room_position)
					player.SendConsoleCommand("ddraw.sphere", 20.0f, Color.green, position, 10.0f);
				
				ChatMessage(player, string.Format(lang.GetMessage("CHAT_SPAWN_POINTS", this, player.UserIDString), _spawn_room_position.Count));
			}
		}
		
		[ChatCommand("roomleave")]
		void cmdLeaveFarmRoom(BasePlayer player)
		{
			if(!permission.UserHasPermission(player.UserIDString, "xfarmroom.use"))
			{
				ChatMessage(player, lang.GetMessage("CHAT_NO_PERM", this, player.UserIDString));
				return;
			}
			
			if(_players_in_room.ContainsKey(player.userID))
				LeaveFarmRoom(player);
			else
				ChatMessage(player, lang.GetMessage("CHAT_NOT_IN_ROOM", this, player.UserIDString));
		}
		
				
				
		private bool API_PlayerInRoom(ulong userID)
		{
			return _players_in_room.ContainsKey(userID);
		}
		private void OnUserGroupRemoved(string id, string groupName) => CheckGroup(id, groupName, false);
		
		private Coroutine _coroutine;
		
		[ConsoleCommand("farm_ore")]
		void ccmdFarmOre(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(player == null || !_players_in_room.ContainsKey(player.userID)) return;
			
			if(args.Args != null && args.Args.Length >= 2)
			{
				bool ore_farm = Convert.ToBoolean(args.Args[1]);
				string ore_shortname = args.Args[0];
				
				if(StoredData[player.userID].Ore[ore_shortname] > 0)
					if(_players_in_room[player.userID].OresSpawn.Contains(ore_shortname))
						_players_in_room[player.userID].OresSpawn.Remove(ore_shortname);
					else
						_players_in_room[player.userID].OresSpawn.Add(ore_shortname);
			}
			
			GUIFarmRoom(player);
			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
		}
		
		private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			if(item.info.shortname == "metal.refined" || item.info.shortname == "hq.metal.ore") return;
			if(dispenser == null || player == null) return;
			
			BaseEntity ore = dispenser.GetComponent<BaseEntity>();
			ulong userID = player.userID;
			
			if(ore != null && ore.OwnerID == 10009 && _players_in_room.ContainsKey(userID))
			{
				string shortname = ore.ShortPrefabName;
				StoredData[userID].Ore[shortname] -= 1;
				
				if(StoredData[userID].Ore[shortname] <= 0)
					_players_in_room[userID].OresSpawn.Remove(shortname);
				
				if(_players_in_room[userID].OresSpawn.Count != 0)
				{
					SpawnOre(userID);
					GUIFarmRoom(player);
				}
				else
					LeaveFarmRoom(player);
			}
		}
		
		private Dictionary<ulong, int> Cooldowns = new Dictionary<ulong, int>();
		
		private object OnPlayerCommand(BasePlayer player, string command, string[] args)
		{
			if(player.IsAdmin)
				return null;
			if(!config.Setting.ChatCommand.Contains(command) && _players_in_room.ContainsKey(player.userID))
			{
				ChatMessage(player, lang.GetMessage("CMD_BLOCK", this, player.UserIDString));
				
				return true;
			}	
			
			return null;
		}
		
		private Dictionary<ulong, PlayersInRoom> _players_in_room = new Dictionary<ulong, PlayersInRoom>();
		
		private void OnNewSave()
		{
			timer.Once(10, () =>
			{
				if(config.Setting.DataClear)
				{
					StoredData.Clear();
					SaveData();
				}
				else
				{
					int count = StoredData.Count;
					
					for(int i = 0; i < count; i++)
						StoredData.ElementAt(i).Value.LastUpdate = 0;
					
					SaveData();
				}
			});
		}
		
				
				
		private FarmRoomConfig config;
		
		private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("XFarmRoom", StoredData);
		
		private void CheckPermission(string id, string permName, bool granted)
		{
			if(permName == "xfarmroom.use")
			{
				BasePlayer player = BasePlayer.FindByID(ulong.Parse(id));
			
				if(player != null)
					if(granted)
					{
						if(config.Setting.ChatMessages)
							ChatMessage(player, lang.GetMessage("GRANTED_PERM", this, player.UserIDString));
					}
					else if(!granted && !permission.UserHasPermission(player.UserIDString, "xfarmroom.use"))
					{
						if(config.Setting.ChatMessages)
							ChatMessage(player, lang.GetMessage("REVOKED_PERM", this, player.UserIDString));
						
						if(_players_in_room.ContainsKey(player.userID))
							LeaveFarmRoom(player);
					}
			}
		}
		
				
				
		private void GUIFarmRoom(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".GUI_FARMROOM");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-300 16", OffsetMax = "-210 98" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", ".GUI_FARMROOM");
			
			int y = 0;
			
			foreach(var ore in StoredData[player.userID].Ore)
			{
				bool ore_no_zero = ore.Value > 0, in_list = _players_in_room[player.userID].OresSpawn.Contains(ore.Key);
				
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {-26 - (y * 28)}", OffsetMax = $"0 {0 - (y * 28)}" },
					Image = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat" }
				}, ".GUI_FARMROOM", ".GUI_ORE");				
				
				container.Add(new CuiElement 
				{
					Parent = ".GUI_ORE",
					Components =
					{
						new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", ore.Key + 150), Color = ore_no_zero ? "1 1 1 1" : "1 1 1 0.2004985" },
						new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "2.5 -10.5", OffsetMax = "23.5 10.5" }
					}
				});
				
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-18 -10.5", OffsetMax = "18 10.5" },
                    Text = { Text = $"{ore.Value}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = ore_no_zero ? "1 1 1 1" : "1 1 1 0.2004985" }
                }, ".GUI_ORE");
				
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-23.5 -10.5", OffsetMax = "-2.5 10.5" },
					Button = { Color = ore_no_zero && in_list ? "0.55004985 0.65004985 0.45 1" : ore_no_zero && !in_list ? "0.85004985 0.49004985 0.44 1" : "0.85 0.49 0.44 0.5", Command = $"farm_ore {ore.Key} {!in_list}" },
					Text = { Text = "" }
				}, ".GUI_ORE");
				
				y++;
			}
			
			if(config.Setting.UseUIButton)
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-92 0", OffsetMax = "-2 26" },
					Button = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat", Command = "chat.say /roomleave" },
					Text = { Text = lang.GetMessage("UI_LEAVE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 13, Color = "1 1 1 0.2004985" }
				}, ".GUI_FARMROOM");
			
			CuiHelper.AddUi(player, container);
		}
		protected override void LoadDefaultConfig() => config = FarmRoomConfig.GetNewConfiguration();
		
				
				
		internal class Data
		{
			[JsonProperty("Последнее обновление доступных камней")] public int LastUpdate;
			[JsonProperty("Доступные камни")] public Dictionary<string, int> Ore;
			
			public Data(int lastupdate, Dictionary<string, int> ore)
			{
				LastUpdate = lastupdate; Ore = ore;
			}
		}
		
		private bool GetOresPermission(BasePlayer player)
		{
			foreach(var perm in config.Permission)
				if(permission.UserHasPermission(player.UserIDString, perm.Key))
				{
					Dictionary<string, int> ores = perm.Value.ToDictionary(x => x.Key, x => x.Value);
					
					if(StoredData.ContainsKey(player.userID))
					{
						if(config.Setting.UpdateOption && StoredData[player.userID].LastUpdate == 0 || !config.Setting.UpdateOption && DateTimeOffset.Now.ToUnixTimeSeconds() - StoredData[player.userID].LastUpdate >= config.Setting.UpdateSecond)
						{
							StoredData[player.userID].LastUpdate = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
							StoredData[player.userID].Ore = ores;
							
							ChatMessage(player, lang.GetMessage("CHAT_UPDATE_ORES", this, player.UserIDString));
						}
					}
					else
						StoredData.Add(player.userID, new Data((int)DateTimeOffset.Now.ToUnixTimeSeconds(), ores));
					
					return false;
				}
				
			return true;
		}
		
		public List<string> _ores_shortname = new List<string>
		{
			"sulfur-ore",
			"metal-ore",
			"stone-ore"
		};
		
				
				
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.4985\n" +
			"-----------------------------");
			
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XFarmRoom"))
				StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Data>>("XFarmRoom");
			
			if(!ImageLibrary)
			{
				PrintError("У вас не установлен плагин - ImageLibrary!");
				Interface.Oxide.UnloadPlugin(Name);
				
				return;
			}
			
			foreach(var image in _ore_images)
			    if (!ImageLibrary.Call<bool>("HasImage", image.Key + 150))
					ImageLibrary.Call("AddImage", image.Value, image.Key + 150);
				
			foreach(var perm in config.Permission)
				permission.RegisterPermission(perm.Key, this);
				
			permission.RegisterPermission("xfarmroom.use", this);
			permission.RegisterPermission("xfarmroom.nocdtp", this);
			
			InitializeLang();
			
			timer.Every(120, () => SaveData());
			
			if(_coroutine == null)
				_coroutine = ServerMgr.Instance.StartCoroutine(GeneratePosition());
		}
		
		private void SpawnFarmRoom(Vector3 position)
		{
			foreach(var prefabs_room in _prefabs_room)
			{
				BaseEntity prefab = GameManager.server.CreateEntity(prefabs_room.ShortPrefabName, position + prefabs_room.Position, Quaternion.Euler(prefabs_room.Rotation)) as BaseEntity;
				
				prefab.OwnerID = 100002;
				prefab.Spawn();
				
				if(prefab is BuildingBlock)
				{
					BuildingBlock block = prefab as BuildingBlock;
					
					block.grade = BuildingGrade.Enum.Stone;
					block.SetHealthToMax();
				}
				
				if(prefab is IOEntity)
				{
					IOEntity io_entity = prefab as IOEntity;
					
					io_entity.UpdateHasPower(io_entity.ConsumptionAmount(), 0);
					//io_entity.SetFlag(BaseEntity.Flags.On, true);
				}
			}
		}

        private class FarmRoomConfig  
        {		
			internal class GeneralSetting 
			{
				[JsonProperty("Вариант обновления доступных камней. ( только с разрешением на обновление ). [ True - раз в вайп | False - раз в N секунд ]")] public bool UpdateOption;
				[JsonProperty("Очищать дату после вайпа")] public bool DataClear;
				[JsonProperty("Ограничивать кол-во предметов которые можно взять в комнату")] public bool UseMaxCountItem;
				[JsonProperty("Использовать UI кнопку для выхода из комнаты")] public bool UseUIButton;
				[JsonProperty("Раз в сколько сек. обновлять кол-во доступных камней. ( проверяется только тогда, когда игрок пытается/попадает в комнату )")] public int UpdateSecond;
				[JsonProperty("Сколько максимум одновременно активных комнат может быть. ( для оптимизации )")] public int MaxCountRoom;
				[JsonProperty("Сколько максимум предметов можно взять в комнату")] public int MaxCountItem;
				[JsonProperty("Перерыв на телепортацию в комнату сек.")] public int CDTPRoom;
				[JsonProperty("Префикс в чате")] public string PrefixChat;
				[JsonProperty("Уведомлять игрока когда ему выдали или отобрали доступ к функционалу фарм комнаты")] public bool ChatMessages;
				[JsonProperty("SteamID профиля для кастомной аватарки")] public ulong SteamID;
				[JsonProperty("Список разрешенных консольных команд в фарм комнате")] public List<string> ConsoleCommand;
				[JsonProperty("Список разрешенных чат команд в фарм комнате")] public List<string> ChatCommand;
			}
			
			[JsonProperty("Общие настройки")]
			public GeneralSetting Setting = new GeneralSetting();
			[JsonProperty("Пермишен - кол-во камней. [ Изменять можно только значение и пермишен ]")]
			public Dictionary<string, Dictionary<string, int>> Permission = new Dictionary<string, Dictionary<string, int>>();											
			
			public static FarmRoomConfig GetNewConfiguration()
            {
                return new FarmRoomConfig
                {
					Setting = new GeneralSetting
					{
						UpdateOption = false,
						DataClear = true,
						UseMaxCountItem = true,
						UseUIButton = true,
						UpdateSecond = 3600,
						MaxCountRoom = 10,
						MaxCountItem = 6,
						CDTPRoom = 90,
						PrefixChat = "<size=12><color=#FFFFFF50>[</color> <color=#00FF0050>XFarmRoom</color> <color=#FFFFFF50>]</color></size>\n",
						ChatMessages = false,
						SteamID = 0,
						ConsoleCommand = new List<string>
						{
							"global.farm_ore"
						},
						ChatCommand = new List<string>
						{
							"roomtp",
							"roomleave"
						}
					},
					Permission = new Dictionary<string, Dictionary<string, int>>
					{
						["xfarmroom.300"] = new Dictionary<string, int>
						{
							["sulfur-ore"] = 300,
							["metal-ore"] = 300,
							["stone-ore"] = 300
						},						
						["xfarmroom.100"] = new Dictionary<string, int>
						{
							["sulfur-ore"] = 100,
							["metal-ore"] = 100,
							["stone-ore"] = 100
						}
					}
				};
			}
        }
		
		private void OnUserGroupAdded(string id, string groupName) => CheckGroup(id, groupName, true);
		
				
		[PluginReference] private Plugin ImageLibrary, Vanish;
		
		private IEnumerator GeneratePosition()
		{
			PrintWarning("Началась генерация точек для спавна фарм комнат!");
			
			int valid_position_count = 0, z = 1;
			float start_position_x_z = World.Serialization.world.size / 2;
			Vector3 start_position = new Vector3(start_position_x_z, 750, start_position_x_z);
			
			while(valid_position_count < config.Setting.MaxCountRoom)
			{
				RemoveRoomOrEntity(start_position);
				
				yield return CoroutineEx.waitForSeconds(0.2f);
				
				if(CheckPosition(start_position))
				{
					_spawn_room_position.Add(start_position);
					valid_position_count++;
				}
				
				yield return CoroutineEx.waitForSeconds(0.2f);
				
				start_position += new Vector3(0, 0, -100);
				
				if(z == 20)
				{
					z = 0;
					
					start_position.z = start_position_x_z;
					start_position += new Vector3(-100, 0, 0);
				}
				
				z++;
			}
			
			PrintWarning($"Генерация завершена! Сгенерировано: {valid_position_count} точек.");
			
			_coroutine = null;
			yield return 0;
		}
		
		private Dictionary<string, string> _ore_images = new Dictionary<string, string>
		{
			["sulfur-ore"] = "https://i.imgur.com/ay3nWKl.png",
			["metal-ore"] = "https://i.imgur.com/fB4VmNf.png",
			["stone-ore"] = "https://i.imgur.com/hJtQcrK.png"
		};
		
		public List<PrefabsRoom> _prefabs_room = new List<PrefabsRoom>
		{
			new PrefabsRoom("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(0, 0, 0), new Vector3(0, 0, 0)),
			new PrefabsRoom("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(3, 0, 0), new Vector3(0, 0, 0)),
			new PrefabsRoom("assets/prefabs/building core/wall/wall.prefab", new Vector3(4.5f, 0, 0), new Vector3(0, 0, 0)),
			new PrefabsRoom("assets/prefabs/building core/wall/wall.prefab", new Vector3(-1.5f, 0, 0), new Vector3(0, 180, 0)),
			new PrefabsRoom("assets/prefabs/building core/wall/wall.prefab", new Vector3(3, 0, 1.5f), new Vector3(0, 270, 0)),
			new PrefabsRoom("assets/prefabs/building core/wall/wall.prefab", new Vector3(3, 0, -1.5f), new Vector3(0, 90, 0)),
			new PrefabsRoom("assets/prefabs/building core/wall/wall.prefab", new Vector3(0, 0, 1.5f), new Vector3(0, 270, 0)),
			new PrefabsRoom("assets/prefabs/building core/wall/wall.prefab", new Vector3(0, 0, -1.5f), new Vector3(0, 90, 0)),
			new PrefabsRoom("assets/prefabs/building core/floor/floor.prefab", new Vector3(0, 3, 0), new Vector3(0, 0, 0)),
			new PrefabsRoom("assets/prefabs/building core/floor/floor.prefab", new Vector3(3, 3, 0), new Vector3(0, 0, 0)),
			new PrefabsRoom("assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab", new Vector3(1.5f, 3, 0), new Vector3(180, 0, 0)),
			new PrefabsRoom("assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab", new Vector3(-1.25f, 0.1f, 1.25f), new Vector3(0, 135, 0))
			
		};
        private ItemId v;

        private void LeaveFarmRoom(BasePlayer player, bool isunload = true)
		{
			var player_in_room = _players_in_room[player.userID];
			int time_now = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
				
			TP(player, player_in_room.LastPlayerPosition);
			
			if(!isunload)
			{
				_players_in_room.Remove(player.userID);
				_spawn_room_position.Add(player_in_room.RoomPosition);
				
				Cooldowns[player.userID] = time_now;
				
				ChatMessage(player, lang.GetMessage("CHAT_LEAVE_ROOM", this, player.UserIDString));
			}
			
			CuiHelper.DestroyUi(player, ".GUI_FARMROOM");
			
			StoredData[player.userID].LastUpdate = time_now;
			
			NextTick(() => RemoveRoomOrEntity(player_in_room.RoomPosition));
		}
		
		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if(player != null && _players_in_room.ContainsKey(player.userID))
				NextTick(() => LeaveFarmRoom(player));
		}
		
			}
}