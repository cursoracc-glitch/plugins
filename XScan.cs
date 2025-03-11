using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("XScan", "Я", "1.0.1")]
    class XScan : RustPlugin
    {
		[PluginReference] private Plugin ImageLibrary;	

	    #region Configuration

        private ScanConfig config;

        private class ScanConfig
        {		
			internal class Settings
            {		
				[JsonProperty("Включить GUI сообщения")] public bool GUIMessage;
				[JsonProperty("Включить ЧАТ сообщения")] public bool ChatMessage;
				[JsonProperty("Хранить логи в дате")] public bool SaveData;				
				[JsonProperty("Автоматически очищать дату после вайпа")] public bool WipeData;				
				[JsonProperty("Хранить логи установленных шкафов и доступ к просмотру информации о шкафе")] public bool CupboardData;
				[JsonProperty("Перезарядка скана в сек.")]  public int CooldownScan;          			    
				[JsonProperty("Перезарядка сообщений в сек.")] public int CooldownMessage;					
				[JsonProperty("Список префабов. Отсканировав их можно узнать владельца шкафа и список авторизованных игроков")] public List<string> PrefabList;					
			}

			internal class GUISettings
            {	
				[JsonProperty("Время активности GUI")] public float GUIActive;					
				[JsonProperty("Максимальное кол-во отображаемых игроков")] public int MaxCount;					
				[JsonProperty("Максимальное кол-во отображаемых игроков в строке")] public int MaxCountString;					
			}			            		
			
			[JsonProperty("Общие настройки")]
            public Settings Setting = new Settings();
			[JsonProperty("Настройки GUI")]
            public GUISettings GUISetting = new GUISettings();						
			
			public static ScanConfig GetNewConfiguration()
            {
                return new ScanConfig
                {
					Setting = new Settings()
					{
						GUIMessage = true,
						ChatMessage = false,
						SaveData = true,
						WipeData = true,
						CupboardData = true,
						CooldownScan = 60,
						CooldownMessage = 5,
						PrefabList = new List<string>
						{
							"foundation"
						}
					},
					GUISetting = new GUISettings()
					{
                        GUIActive = 10.0f,
						MaxCount = 10,
						MaxCountString = 5
					}
				};
			}
        }

		protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<ScanConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = ScanConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		private Dictionary<BasePlayer, DateTime> CooldownsScan = new Dictionary<BasePlayer, DateTime>();
		private Dictionary<BasePlayer, DateTime> CooldownsMessage = new Dictionary<BasePlayer, DateTime>();

		#region Commands 

		[ChatCommand("scan")]
		private void cmdScan(BasePlayer player)
		{
			if (CooldownsScan.ContainsKey(player))
                if (CooldownsScan[player].Subtract(DateTime.Now).TotalSeconds >= 0)
				{
                    if (CooldownsMessage.ContainsKey(player))
                        if (CooldownsMessage[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
							
                    SendReply(player, string.Format(lang.GetMessage("CHATCD", this, player.UserIDString), TimeSpan.FromSeconds(Convert.ToInt32(CooldownsScan[player].Subtract(DateTime.Now).TotalSeconds))));
					CooldownsMessage[player] = DateTime.Now.AddSeconds(config.Setting.CooldownMessage);
					return;
				}
				
			if (!permission.UserHasPermission(player.UserIDString, "xscan.use"))
			{
				SendReply(player, lang.GetMessage("CHATNP", this, player.UserIDString));
				return;
			}

			if (permission.UserHasPermission(player.UserIDString, "xscan.unlimit")) 
				Scan(player);
			else if (StoredDataS.ContainsKey(player.userID))
			{
				StoredDataS[player.userID] -= 1; 
				Scan(player);
				
				SendReply(player, string.Format(lang.GetMessage("CHATSC", this, player.UserIDString), StoredDataS[player.userID]));
				
				if (StoredDataS[player.userID] == 0)
					StoredDataS.Remove(player.userID);
				
				Interface.Oxide.DataFileSystem.WriteObject("XScan/ScanCount", StoredDataS);
			}
			else
				SendReply(player, lang.GetMessage("CHATNSC", this, player.UserIDString));							

			if (!player.IsAdmin) 
				CooldownsScan[player] = DateTime.Now.AddSeconds(config.Setting.CooldownScan);
			else
				CooldownsScan[player] = DateTime.Now.AddSeconds(5);
			
		}
		
		[ConsoleCommand("scan_give")]
		void ccmdGiveSC(ConsoleSystem.Arg arg)
		{
			if (arg.Player() != null) return;
			
			ulong steamID = ulong.Parse(arg.Args[0]);
			
		    if (!StoredDataS.ContainsKey(steamID))
				StoredDataS.Add(steamID, 0);
			
			StoredDataS[steamID] += int.Parse(arg.Args[1]);
			Interface.Oxide.DataFileSystem.WriteObject("XScan/ScanCount", StoredDataS);
		}

		#endregion

		#region Data
		
		private Dictionary<string, string> StoredData = new Dictionary<string, string>();
		private Dictionary<ulong, string> StoredDataT = new Dictionary<ulong, string>();
		private Dictionary<ulong, int> StoredDataS = new Dictionary<ulong, int>();
		
		#endregion

		#region Hooks

		private void OnServerInitialized()
		{		
			
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XScan/ScanUSE"))
                StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("XScan/ScanUSE");
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XScan/CupboardList"))
                StoredDataT = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("XScan/CupboardList");			
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XScan/ScanCount"))
                StoredDataS = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("XScan/ScanCount"); 

			permission.RegisterPermission("xscan.use", this);
			permission.RegisterPermission("xscan.unlimit", this);
			permission.RegisterPermission("xscan.cupboardhome", this);
			permission.RegisterPermission("xscan.cupboardlist", this);
			permission.RegisterPermission("xscan.cupboard", this);
			permission.RegisterPermission("xscan.codelock", this);
			permission.RegisterPermission("xscan.autoturret", this);
			
			InitializeLang();
		}
		
		private int AuthorizedCount(int count)
		{
			int maxcount = config.GUISetting.MaxCount;
			
			if (count > maxcount) count = maxcount;
			int x = 0;

			while(count > 0)
			{
				count -= config.GUISetting.MaxCountString;
				x++;
			}
			
			return x;
		}
		
		private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (!config.Setting.CupboardData) return;
			
			var entity = go.ToBaseEntity();
			
			if (entity is BuildingPrivlidge)
			{
				StoredDataT.Add(entity.net.ID, $"{entity.OwnerID} | {entity.transform.position} | {DateTime.Now}");
				
				Interface.Oxide.DataFileSystem.WriteObject("XScan/CupboardList", StoredDataT);
			}
        }
		
		private object OnHammerHit(BasePlayer player, HitInfo info)
		{
			if (!config.Setting.CupboardData) return null;
			if (!permission.UserHasPermission(player.UserIDString, "xscan.cupboardlist")) return null;
			
			var entity = info?.HitEntity;
			
			if (entity is BuildingPrivlidge)
			{
				if (StoredDataT.ContainsKey(entity.net.ID))
					SendReply(player, string.Format(lang.GetMessage("CupboardInfo", this), StoredDataT[entity.net.ID]));
			}
			
			return null;
		}

        private void OnNewSave()
		{
			if (config.Setting.WipeData)
			{
			    StoredData.Clear();
			    StoredDataT.Clear();
				
				Interface.Oxide.DataFileSystem.WriteObject("XScan/ScanUSE", StoredData);
				Interface.Oxide.DataFileSystem.WriteObject("XScan/CupboardList", StoredDataT);
			}
		}

		#endregion
		
		#region Scan
		
		private void Scan(BasePlayer player)
		{
			RaycastHit rhit;
			
			if (!Physics.Raycast(player.eyes.HeadRay(), out rhit, 3f, LayerMask.GetMask("Construction", "Deployed"))) return;
			var entity = rhit.GetEntity();
			
			if (entity.OwnerID == 0) return;
			
			List<ulong> ListPlayers = new List<ulong>();
			
			if (permission.UserHasPermission(player.UserIDString, "xscan.cupboard") && entity is BuildingPrivlidge)
			{
				var Cupboard = entity as BuildingPrivlidge;

				foreach (var rplayer in Cupboard.authorizedPlayers)
					ListPlayers.Add(rplayer.userid);

                if (ListPlayers.Count != 0)
				{					
					if (config.Setting.GUIMessage) GUI(player, entity.OwnerID, ListPlayers, lang.GetMessage("GUICupboard", this, player.UserIDString));	
				    if (config.Setting.ChatMessage) ChatMessage(player, entity.OwnerID, ListPlayers, lang.GetMessage("CHATCupboard", this, player.UserIDString));
				}				
			}		
			else if (permission.UserHasPermission(player.UserIDString, "xscan.cupboardhome") && entity.GetBuildingPrivilege() && config.Setting.PrefabList.Contains(entity.ShortPrefabName))
			{
				var Cupboard = entity.GetBuildingPrivilege();
				
				foreach (var rplayer in Cupboard.authorizedPlayers)
					ListPlayers.Add(rplayer.userid);
					
				if (ListPlayers.Count != 0)
				{
					if (config.Setting.GUIMessage) GUI(player, entity.OwnerID, ListPlayers, lang.GetMessage("GUICupboard", this, player.UserIDString));
				    if (config.Setting.ChatMessage) ChatMessage(player, entity.OwnerID, ListPlayers, lang.GetMessage("CHATCupboard", this, player.UserIDString));
				}
			}
			else if (permission.UserHasPermission(player.UserIDString, "xscan.codelock") && entity is Door)
			{
				if (entity.GetSlot(BaseEntity.Slot.Lock) is CodeLock)
				{
					var CodeLock = (CodeLock)entity.GetSlot(BaseEntity.Slot.Lock);
				
				    foreach (var rplayer in CodeLock.whitelistPlayers)
						ListPlayers.Add(rplayer);								

				    foreach (var rplayer in CodeLock.guestPlayers)
						if(!ListPlayers.Contains(rplayer))
						    ListPlayers.Add(rplayer);
					
					if (ListPlayers.Count != 0)
					{
						if (config.Setting.GUIMessage) GUI(player, entity.OwnerID, ListPlayers, lang.GetMessage("GUICodelock", this, player.UserIDString));
					    if (config.Setting.ChatMessage) ChatMessage(player, entity.OwnerID, ListPlayers, lang.GetMessage("CHATCodelock", this, player.UserIDString));
					}
				}
			}
			else if (permission.UserHasPermission(player.UserIDString, "xscan.autoturret") && entity is AutoTurret)
			{
				var AutoTurret = entity as AutoTurret;
				
				foreach (var rplayer in AutoTurret.authorizedPlayers)
					ListPlayers.Add(rplayer.userid);

				if (ListPlayers.Count != 0) 
				{
					if (config.Setting.GUIMessage) GUI(player, entity.OwnerID, ListPlayers, lang.GetMessage("GUIAutoturret", this, player.UserIDString));
				    if (config.Setting.ChatMessage) ChatMessage(player, entity.OwnerID, ListPlayers, lang.GetMessage("CHATAutoturret", this, player.UserIDString));
				}
			}
			else
				if (config.Setting.ChatMessage) ChatMessageOwner(player, entity.OwnerID);
				
				 
			if (config.Setting.GUIMessage) GUIOwner(player, entity.OwnerID);

            if (config.Setting.SaveData)
			{
			    StoredData.Add($"{DateTime.Now}", $"{player.displayName} | {player.userID}");	
			    Interface.Oxide.DataFileSystem.WriteObject("XScan/ScanUSE", StoredData);
			}
		}

        private string Player_GetNameID(ulong id)
		{
			var player = covalence.Players.FindPlayerById(id.ToString());
			
			return $"{player.Name} | {player.Id} - {player.IsConnected ? lang.GetMessage("CHATOnline", this, player.Id) : lang.GetMessage("CHATOffline", this, player.Id)}";
		}

		private void ChatMessage(BasePlayer player, ulong ownerid, List<ulong> ListPlayers, string message)
		{
            var owner = covalence.Players.FindPlayerById(ownerid.ToString());

			SendReply(player, string.Format(lang.GetMessage("CHATOwner", this, player.UserIDString), owner.Name, owner.Id, owner.IsConnected ? lang.GetMessage("CHATOnline", this, player.UserIDString) : lang.GetMessage("CHATOffline", this, player.UserIDString)));

            if (ListPlayers.Count != 0)
			{
                SendReply(player, message);

			    foreach (var rplayer in ListPlayers)
				    SendReply(player, $"[ {Player_GetNameID(rplayer)} ]");
			}
		}
		
		private void ChatMessageOwner(BasePlayer player, ulong ownerid)
		{
			var owner = covalence.Players.FindPlayerById(ownerid.ToString());

			SendReply(player, string.Format(lang.GetMessage("CHATOwner", this, player.UserIDString), owner.Name, owner.Id, owner.IsConnected ? lang.GetMessage("CHATOnline", this, player.UserIDString) : lang.GetMessage("CHATOffline", this, player.UserIDString)));
		}
        
        #endregion			
		 
		#region GUI

		private void GUI(BasePlayer player, ulong ownerid, List<ulong> ListPlayers, string message)
		{		
			CuiHelper.DestroyUi(player, ".Overlay");
            CuiElementContainer container = new CuiElementContainer();
			
			int maxcountstring = config.GUISetting.MaxCountString;
			int maxcount = config.GUISetting.MaxCount;
			int count1 = ListPlayers.Count > maxcount ? maxcount : ListPlayers.Count;
			int count = count1 < maxcountstring ? count1 : maxcountstring;
			int autcount = AuthorizedCount(count1);
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"{-105 * count - ((1 + (0.5 * (count - 1))) * 5)} 230", OffsetMax = $"{105 * count + ((1 + (0.5 * (count - 1))) * 5)} {230 + (70 * autcount) - (--autcount * 5)}" },
                Image = { FadeIn = 0.75f, Color = "0.317 0.321 0.309 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Hud", ".Overlay");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-325 0", OffsetMax = "325 25" },
                Text = { FadeIn = 0.75f, Text = message, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 13, Color = "1 1 1 0.75" }
            }, ".Overlay");
			
			int x = 0, y = 0;
			
			foreach(var players in ListPlayers.Take(maxcount))
			{
				var targetplayers = covalence.Players.FindPlayerById(players.ToString());
				
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"{-105 - (105 * (count + (count * 0.025 - 0.025) - 1)) + (x * 215)} {-65 - (y * 65)}", OffsetMax = $"{105 - (105 * (count + (count * 0.025 - 0.025) - 1)) + (x * 215)} {-5 - (y * 65)}" },
                    Image = { FadeIn = 0.75f, Color = "0.55 0.55 0.55 0.75" }
                }, ".Overlay", ".GUI");
				
			    container.Add(new CuiElement
            	{
                    Parent = ".GUI",
                    Components =
                    {
					    new CuiRawImageComponent { FadeIn = 0.75f, Png = (string) ImageLibrary.Call("GetImage", targetplayers.Id) },
                    	new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-155 -5" }
                	}
            	});
			
				container.Add(new CuiLabel
            	{
                	RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 0", OffsetMax = "0 -5" },
                	Text = { FadeIn = 0.75f, Text = string.Format(lang.GetMessage("GUIInfoPlayer", this, player.UserIDString), targetplayers.Name, targetplayers.Id), Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.75" }
            	}, ".GUI");			
			
				container.Add(new CuiLabel
            	{
                	RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 5", OffsetMax = "0 -5" },
                	Text = { FadeIn = 0.75f, Text = targetplayers.Id == ownerid.ToString() ? lang.GetMessage("GUIOwner", this, player.UserIDString) : "", Align = TextAnchor.LowerLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.75" }
            	}, ".GUI");			
			
				container.Add(new CuiLabel
            	{
                	RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 5", OffsetMax = "-5 0" },
                	Text = { FadeIn = 0.75f, Text = targetplayers.IsConnected ? lang.GetMessage("GUIOnline", this, player.UserIDString) : lang.GetMessage("GUIOffline", this, player.UserIDString), Align = TextAnchor.LowerRight, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.75" }
            	}, ".GUI");
				
				x++;
                if (x == maxcountstring)
                {
                    x = 0;
                    y++;
					
					count = count1 - (count * y);
					
					if (count > maxcountstring)
					    count = maxcountstring;
                }
			}		
			
			CuiHelper.AddUi(player, container);
			
			timer.Once(config.GUISetting.GUIActive, () => { CuiHelper.DestroyUi(player, ".Overlay"); });
		}

        private void GUIOwner(BasePlayer player, ulong ownerid)
		{
			var owner = covalence.Players.FindPlayerById(ownerid.ToString());
			
			CuiHelper.DestroyUi(player, ".OverlayOwner");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-110 130", OffsetMax = "110 200" },
                Image = { FadeIn = 0.75f, Color = "0.317 0.321 0.309 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Hud", ".OverlayOwner");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { FadeIn = 0.75f, Color = "0.55 0.55 0.55 0.75" }
            }, ".OverlayOwner", ".GUIOwner");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-325 0", OffsetMax = "325 25" },
                Text = { FadeIn = 0.75f, Text = lang.GetMessage("GUIOwnerC", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 13, Color = "1 1 1 0.75" }
            }, ".OverlayOwner");
			
			container.Add(new CuiElement
            {
                Parent = ".GUIOwner",
                Components =
                {
					new CuiRawImageComponent { FadeIn = 0.75f, Png = (string) ImageLibrary.Call("GetImage", owner.Id) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-155 -5" }
                }
            });
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 0", OffsetMax = "0 -5" },
                Text = { FadeIn = 0.75f, Text = string.Format(lang.GetMessage("GUIInfoPlayer", this, player.UserIDString), owner.Name, owner.Id), Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.75" }
            }, ".GUIOwner");			
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 5", OffsetMax = "0 -5" },
                Text = { FadeIn = 0.75f, Text = owner.Id == ownerid.ToString() ? lang.GetMessage("GUIOwner", this, player.UserIDString) : "", Align = TextAnchor.LowerLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.75" }
            }, ".GUIOwner");			
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 5", OffsetMax = "-5 0" },
                Text = { FadeIn = 0.75f, Text = owner.IsConnected ? lang.GetMessage("GUIOnline", this, player.UserIDString) : lang.GetMessage("GUIOffline", this, player.UserIDString), Align = TextAnchor.LowerRight, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.75" }
            }, ".GUIOwner");
			
			CuiHelper.AddUi(player, container);
			
			timer.Once(config.GUISetting.GUIActive, () => { CuiHelper.DestroyUi(player, ".OverlayOwner"); });
		}

        #endregion

		#region Lang

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GUIOwner"] = "OWNER",									
                ["GUIOwnerC"] = "OWNER CONSTRUCTION",									
                ["GUIOnline"] = "<color=#7CFC00>ONLINE</color>",									
                ["GUIOffline"] = "<color=#FF0000>OFFLINE</color>",									
                ["GUIInfoPlayer"] = "NAME: {0}\nID: {1}",									
                ["GUICupboard"] = "LIST OF AUTHORIZED PLAYERS IN CUPBOARD",									
                ["GUICodelock"] = "LIST OF AUTHORIZED PLAYERS IN CODE LOCK",									
                ["GUIAutoturret"] = "LIST OF AUTHORIZED PLAYERS IN AUTO TURRET",									
                ["CHATOwner"] = "<color=#00BFFF>Owner construction:</color>\n[ {0} | {1} - {2} ]",																		
                ["CHATOnline"] = "<color=#7CFC0090>ONLINE</color>",																		
                ["CHATOffline"] = "<color=#FF000090>OFFLINE</color>",																		
                ["CHATCupboard"] = "<color=#7FFF00>List of authorized players in cupboard</color>",																		
                ["CHATCodelock"] = "<color=#7FFF00>List of authorized players in code lock</color>",																		
                ["CHATAutoturret"] = "<color=#7FFF00>List of authorized players in auto turret</color>",																	
                ["CHATCD"] = "Will be available through - {0}",																		
                ["CHATNP"] = "No permission!",
				["CHATSC"] = "Scans left - {0}!",	
				["CHATNSC"] = "No scans!",
                ["CupboardInfo"] = "<color=#FFFF0095>{0}</color>"			
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GUIOwner"] = "ВЛАДЕЛЕЦ",									
                ["GUIOwnerC"] = "ВЛАДЕЛЕЦ КОСТРУКЦИИ",									
                ["GUIOnline"] = "<color=#7CFC00>В СЕТИ</color>",									
                ["GUIOffline"] = "<color=#FF0000>НЕ В СЕТИ</color>",									
                ["GUIInfoPlayer"] = "ИМЯ: {0}\nID: {1}",									
                ["GUICupboard"] = "СПИСОК АВТОРИЗОВАННЫХ ИГРОКОВ В ШКАФУ",									
                ["GUICodelock"] = "СПИСОК АВТОРИЗОВАННЫХ ИГРОКОВ В ЗАМКЕ",									
                ["GUIAutoturret"] = "СПИСОК АВТОРИЗОВАННЫХ ИГРОКОВ В ТУРЕЛИ",									
                ["CHATOwner"] = "<color=#00BFFF>Владелец конструкции:</color>\n[ {0} | {1} - {2} ]",																		
                ["CHATOnline"] = "<color=#7CFC0090>В СЕТИ</color>",																		
                ["CHATOffline"] = "<color=#FF000090>НЕ В СЕТИ</color>",																		
                ["CHATCupboard"] = "<color=#7FFF00>Список авторизованных игроков в шкафу</color>",																		
                ["CHATCodelock"] = "<color=#7FFF00>Список авторизованных игроков в замке</color>",																		
                ["CHATAutoturret"] = "<color=#7FFF00>Список авторизованных игроков в турели</color>",																		
                ["CHATCD"] = "Будет доступно через - {0}",																		
                ["CHATNP"] = "Недостаточно прав!",	
                ["CHATSC"] = "Осталось сканов - {0}!",	
                ["CHATNSC"] = "Недостаточно сканов!",	
                ["CupboardInfo"] = "<color=#FFFF0095>{0}</color>"		
            }, this, "ru");
        }

        #endregion		
	}
}