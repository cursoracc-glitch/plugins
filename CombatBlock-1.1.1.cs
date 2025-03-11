using Oxide.Core;
using ConVar;
using System.Text;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries;
using System;
using Oxide.Plugins.CombatBlockExt;
using Oxide.Core.Plugins;
using System.Collections;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using Time = UnityEngine.Time;

namespace Oxide.Plugins.CombatBlockExt
{
	public static class ExtensionMethods
	{
		private static readonly Lang Lang = Interface.Oxide.GetLibrary<Lang>();

		
		public static string GetAdaptedMessage(this string langKey, string userID = null, params object[] args)
		{
			string message = Lang.GetMessage(langKey, CombatBlock.Instance, userID);

			if (args == null || args.Length == 0)
			{
				return message;
			}
            
			return new StringBuilder().AppendFormat(message, args).ToString();
		}

				
		
		public static string ToTimeFormat(this float source)
		{
			TimeSpan ts = TimeSpan.FromSeconds(source);
			return $"{ts.Minutes:D1}:{ts.Seconds:D2}";
		}

			}
}
namespace Oxide.Plugins
{
	[Info("CombatBlock", "Mercury", "1.1.1")]
	public class CombatBlock : RustPlugin
	{
		   		 		  						  	   		  		 			  	   		  		  		   		 
				
		
				private static void RunEffect(BasePlayer player, string prefab)
		{
			Effect effect = new Effect();
			effect.Init(Effect.Type.Generic, player.transform.position, Vector3.zero);
			effect.pooledString = prefab;
			EffectNetwork.Send(effect, player.net.connection);
		}

				
		
		private List<BasePlayer> _playerInCache = new();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new Exception();
                }

                SaveConfig();
            }
            catch
            {
                for (int i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
            }
            ValidateConfig();
            SaveConfig();
        }

		
		
		private void OnRaidBlock(BasePlayer player, Vector3 position)
		{
			if (player != null && player.gameObject != null)
			{
				if (player.gameObject.TryGetComponent(out CombatPlayer combatBlocker))
					combatBlocker.KillComponent(true);
			}
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.TryGetComponent(out CombatPlayer rp))
				rp.CreateUI();
		}

		private Boolean IsCombatBlock(BasePlayer player)
		{
			if (!_combatPlayers.ContainsKey(player)) return false;
			CombatPlayer combatP = _combatPlayers[player];
			if (combatP == null) return false;
			if (combatP.UnblockTimeLeft <= 0) return false;

			return true;
		}

				
		        
        private void DrawUI_CB_Main(BasePlayer player,  float timeLeft = 0f)
        {
            string Interface = InterfaceBuilder.GetInterface(InterfaceBuilder.CB_MAIN);
            if (Interface == null) return;

            Interface = InterfaceBuilder.TypeUi switch
            {
	            0 => Interface.Replace("%Descriptions%", "COMBATBLOCK_UI_DESCRIPTIONS_V1".GetAdaptedMessage(player.UserIDString)),
	            1 => Interface.Replace("%Descriptions%", "COMBATBLOCK_UI_DESCRIPTIONS_V2".GetAdaptedMessage(player.UserIDString)),
	            _ => Interface
            };

            Interface = Interface.Replace("%Title%", "COMBATBLOCK_UI_TITLE".GetAdaptedMessage(player.UserIDString));

		   		 		  						  	   		  		 			  	   		  		  		   		 
            CuiHelper.DestroyUi(player, InterfaceBuilder.CB_MAIN);
            CuiHelper.AddUi(player, Interface);
            
            DrawUI_CB_Updated(player, timeLeft == 0 ? config.CombatBlockMain.CombatBlockDuration : timeLeft, true);
        }	
		   		 		  						  	   		  		 			  	   		  		  		   		 
		
		
		private bool IsDuel(ulong userID)
		{
			object playerId = ObjectCache.Get(userID);
			BasePlayer player = null;
			if (Duel != null || Duelist != null)
				player = BasePlayer.FindByID(userID);

			object result = EventHelper?.Call("EMAtEvent", playerId);
			if (result is bool && ((bool)result) == true)
				return true;
		   		 		  						  	   		  		 			  	   		  		  		   		 

			if (Battles != null && Battles.Call<bool>("IsPlayerOnBattle", playerId))
				return true;


			if (Duel != null && Duel.Call<bool>("IsPlayerOnActiveDuel", player))
				return true;
			if (Duelist != null && Duelist.Call<bool>("inEvent", player))
				return true;
		   		 		  						  	   		  		 			  	   		  		  		   		 
			if (ArenaTournament != null && ArenaTournament.Call<bool>("IsOnTournament", playerId))
				return true;

			return false;
		}
		private Dictionary<BasePlayer ,CombatPlayer> _combatPlayers = new();

		
		private Boolean IsCombatBlocked(BasePlayer player) => IsCombatBlock(player);
		private static ImageUI _imageUI;
		private object canTeleport(BasePlayer player) => CanTeleport(player);

		
        
        private class CombatPlayer : FacepunchBehaviour
        {
	        public BasePlayer player;
            private float _timeToUnblock;
            private int _combatBlockDuration;
            public float UnblockTimeLeft => _timeToUnblock - Time.realtimeSinceStartup;

            private void Awake()
            {
	            player = gameObject.GetComponent<BasePlayer>();
	            _combatBlockDuration = Instance.config.CombatBlockMain.CombatBlockDuration;
	            _timeToUnblock = Time.realtimeSinceStartup + _combatBlockDuration;
	            CreateUI();
	            InvokeRepeating(RefreshUI, 0f, 1);
	            
	            Instance.SendChat("COMBATBLOCK_ENTER_COMBAT_INITIATOR".GetAdaptedMessage(player.UserIDString, UnblockTimeLeft.ToTimeFormat()), player);
            }
		   		 		  						  	   		  		 			  	   		  		  		   		 
            private void OnDestroy()
            {
	            Interface.CallHook ("OnCombatBlockStopped", player);
            }
            
            public void UpdateBlockTime()
            {
	            _timeToUnblock = Time.realtimeSinceStartup + _combatBlockDuration;
            }
		   		 		  						  	   		  		 			  	   		  		  		   		 
            public void CreateUI()
            {
	            if (player != null)
	            {
		            Instance.DrawUI_CB_Main(player, UnblockTimeLeft);
	            }
            }

            
            public void KillComponent(bool isRaidBlocked = false)
            {
	            if (player != null)
		            CuiHelper.DestroyUi(player, InterfaceBuilder.CB_MAIN);
	            
	            if(!isRaidBlocked)
					Instance.SendChat("COMBATBLOCK_END_BLOCK".GetAdaptedMessage(player.UserIDString), player);

	            CancelInvoke(nameof(RefreshUI));
	            if (Instance._combatPlayers.ContainsKey(player))
		            Instance._combatPlayers.Remove(player);
	            
	            Destroy(this);
            }

            private void RefreshUI()
            {
	            if (UnblockTimeLeft <= 0)
	            {
		            KillComponent();
	            }
	            else if (player != null)
	            {
		            Instance.DrawUI_CB_Updated(player, UnblockTimeLeft);
	            }
            }
        }
		private Boolean IsCombatBlocked(String playerID)
		{
			if (!UInt64.TryParse(playerID, out UInt64 id)) return false;
			BasePlayer player = BasePlayer.FindByID(id);
			return player != null && IsCombatBlock(player);
		}
		private object CanRedeemKit(BasePlayer player) => CanActions(player);
		private object CanTrade(BasePlayer player) => CanActions(player, true);
		
		private void Unload()
		{
			foreach (CombatPlayer obj in _combatPlayers.Values)
				UnityEngine.Object.Destroy(obj);
			InterfaceBuilder.DestroyAll();
			_interface = null;
			Instance = null;
		}

                
        
		private class ImageUI
		{
			private readonly string _paths;
			private readonly string _printPath;
			private readonly Dictionary<string, ImageData> _images;

			private enum ImageStatus
			{
				NotLoaded,
				Loaded,
				Failed
			}

			public ImageUI()
			{
				_paths = Instance.Name + "/Images/";
				_printPath = "data/" + _paths;
				_images = new Dictionary<string, ImageData>
				{
					{ "CB_FON0", new ImageData() },
					{ "CB_FON1", new ImageData() },
					{ "CB_FON2", new ImageData() },
					{ "CB_VARIANT0_ICON", new ImageData() },
					{ "CB_VARIANT1_ICON_FON", new ImageData() },
					{ "CB_VARIANT1_ICON", new ImageData() },
					{ "CB_VARIANT2_ICON", new ImageData() },
				};
			}

			private class ImageData
			{
				public ImageStatus Status = ImageStatus.NotLoaded;
				public string Id { get; set; }
			}

			public string GetImage(string name)
			{
				ImageData image;
				if (_images.TryGetValue(name, out image) && image.Status == ImageStatus.Loaded)
					return image.Id;
				return null;
			}

			public void DownloadImage()
			{
				KeyValuePair<string, ImageData>? image = null;
				foreach (KeyValuePair<string, ImageData> img in _images)
				{
					if (img.Value.Status == ImageStatus.NotLoaded)
					{
						image = img;
						break;
					}
				}

				if (image != null)
				{
					ServerMgr.Instance.StartCoroutine(ProcessDownloadImage(image.Value));
				}
				else
				{
					List<string> failedImages = new List<string>();

					foreach (KeyValuePair<string, ImageData> img in _images)
					{
						if (img.Value.Status == ImageStatus.Failed)
						{
							failedImages.Add(img.Key);
						}
					}

					if (failedImages.Count > 0)
					{
						string images = string.Join(", ", failedImages);
						Instance.PrintError(LanguageEn
							? $"Failed to load the following images: {images}. Perhaps you did not upload them to the '{_printPath}' folder."
							: $"Не удалось загрузить следующие изображения: {images}. Возможно, вы не загрузили их в папку '{_printPath}'.");
						Interface.Oxide.UnloadPlugin(Instance.Name);
					}
					else
					{
						Instance.Puts(LanguageEn
							? $"{_images.Count} images downloaded successfully!"
							: $"{_images.Count} изображений успешно загружено!");
                        
                        Instance.Puts(LanguageEn ? "Generating the interface, please wait for approximately 5-10 seconds" : "Генерируем интерфейс, ожидайте ~5-10 секунд");
                        _interface = new InterfaceBuilder();
                        Instance.Puts(LanguageEn ? "The interface has been successfully loaded" : "Интерфейс успешно загружен!");
					}
				}
			}

			public void UnloadImages()
			{
				foreach (KeyValuePair<string, ImageData> item in _images)
					if (item.Value.Status == ImageStatus.Loaded)
						if (item.Value?.Id != null)
							FileStorage.server.Remove(uint.Parse(item.Value.Id), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);

				_images?.Clear();
			}

            private IEnumerator ProcessDownloadImage(KeyValuePair<string, ImageData> image)
			{
                string url = "file://" + Interface.Oxide.DataDirectory + "/" + _paths + image.Key + ".png";

                using UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();

                if (www.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                {
                    image.Value.Status = ImageStatus.Failed;
                }
                else
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(www);
                    image.Value.Id = FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                    image.Value.Status = ImageStatus.Loaded;
                    UnityEngine.Object.DestroyImmediate(tex);
                }

                DownloadImage();
            }
		}

		private void OnServerInitialized()
		{
			permission.RegisterPermission(Name + _permIgnoreCombat,this);
			
			_imageUI = new ImageUI();
			_imageUI.DownloadImage();
			
			if(config.BlockDetect.ActivateOnNpcDamageReceived)
				Subscribe(nameof(OnEntityTakeDamage));

			if (config.ActionsBlocked.CanTeleport)
			{
				Subscribe(nameof(CanTeleport));
				Subscribe(nameof(canTeleport));
			}
			if (config.ActionsBlocked.CanTrade)
			{
				Subscribe(nameof(canTrade));
				Subscribe(nameof(CanTrade));
			}
			if (config.ActionsBlocked.CanUseKit)
			{
				Subscribe(nameof(CanRedeemKit));
			}
		   		 		  						  	   		  		 			  	   		  		  		   		 
			if (config.CombatBlockMain.CombatBlockOnRaidBlock)
			{
				Subscribe(nameof(OnRaidBlock));
				Subscribe(nameof(OnRaidBlockStarted));
			}
			
			if (config.ActionsBlocked.BlockedCommands != null && config.ActionsBlocked.BlockedCommands.Count != 0)
			{
				if (config.ActionsBlocked.BlockedCommands.Count == 1 && config.ActionsBlocked.BlockedCommands[0].Equals("commandExample")) return;
				
				Subscribe(nameof(OnPlayerCommand));
				Subscribe(nameof(OnServerCommand));
			}
		}

		
		
		private void Init()
		{
			Instance = this;
			Unsubscribe(nameof(OnEntityTakeDamage));
			Unsubscribe(nameof(OnPlayerCommand));
			Unsubscribe(nameof(OnServerCommand));
			Unsubscribe(nameof(CanTeleport));
			Unsubscribe(nameof(canTeleport));
			Unsubscribe(nameof(CanRedeemKit));
			Unsubscribe(nameof(canTrade));
			Unsubscribe(nameof(CanTrade));
			Unsubscribe(nameof(OnRaidBlock));
			Unsubscribe(nameof(OnRaidBlockStarted));
		}

		private void StartCombatBlocked(BasePlayer player)
		{
			if (player == null)
			{
				Debug.LogError("StartCombatBlocked was called with a null player.");
				return;
			}

			if (permission.UserHasPermission(player.UserIDString, $"{Name}{_permIgnoreCombat}"))
				return;

			if (Interface.Call("CanCombatBlocked", player) != null) 
				return;
			
			if (Interface.Call("CanCombatBlock", player) != null) 
				return;

			if (player.gameObject.TryGetComponent(out CombatPlayer combatBlocker))
			{
				combatBlocker.UpdateBlockTime();
			}
			else
			{
				try
				{
					combatBlocker = player.gameObject.AddComponent<CombatPlayer>();
					_combatPlayers.Add(player, combatBlocker);
				}
				catch (Exception ex)
				{
					Debug.LogError($"Error adding CombatPlayer component: {ex.Message}");
					return;
				}
			}

			Interface.CallHook("OnCombatBlockStarted", player);
		}

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        
        private void DrawUI_CB_Updated(BasePlayer player, float timeLeft, bool upd = false)
        {
            string Interface = InterfaceBuilder.GetInterface(InterfaceBuilder.CB_PROGRESS_BAR);
            if (Interface == null) return;
            double factor = InterfaceBuilder.Factor * timeLeft / config.CombatBlockMain.CombatBlockDuration;

            Interface = Interface.Replace("%left%", factor.ToString(CultureInfo.InvariantCulture));
            Interface = Interface.Replace("%TimeLeft%", "COMBATBLOCK_UI_TIMER".GetAdaptedMessage(player.UserIDString, timeLeft.ToTimeFormat()));
            if(!upd)
                Interface = Interface.Replace("0.222", "0");

            CuiHelper.DestroyUi(player, InterfaceBuilder.CB_PROGRESS);
            CuiHelper.DestroyUi(player, InterfaceBuilder.CB_PROGRESS_TIMER);
            CuiHelper.AddUi(player, Interface);
        }
		private object canTrade(BasePlayer player) => CanTrade(player);

		private bool IsClans(string userID, string targetID)
		{
			if (Clans)
			{
				string tagUserID = (string)Clans?.Call("GetClanOf", userID);
				string tagTargetID = (string)Clans?.Call("GetClanOf", targetID);
				if (tagUserID == null && tagTargetID == null)
				{
					return false;
				}

				return tagUserID == tagTargetID;
			}

			return false;
		}

		private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
		{
			if (entity.ToPlayer() == null)
				return;
			BasePlayer player = entity.ToPlayer();

			if (player != null && player.gameObject != null)
			{
				CombatPlayer combatBlocker = player.gameObject.GetComponent<CombatPlayer>();
				if (config.BlockDetect.DeactivateOnPlayerDeath && combatBlocker != null)
					combatBlocker.KillComponent();
			}
		}
        

        private class InterfaceBuilder
		{
			
			private static InterfaceBuilder _instance;
            public const string CB_MAIN = "CB_MAIN";
            public const string CB_PROGRESS_BAR = "CB_PROGRESS_BAR";
            public const string CB_PROGRESS = "CB_PROGRESS";
            public const string CB_PROGRESS_TIMER = "CB_PROGRESS_TIMER";

            public static int TypeUi;
            public static double Factor;
            private Configuration.CombatBlockUi.CombatBlockUiSettings _uiSettings;
            private static float _fade;

			private Dictionary<string, string> _interfaces;

			
						public InterfaceBuilder()
			{
				_instance = this;
				_interfaces = new Dictionary<string, string>();
                TypeUi = Instance.config.CombatBlockInterface.UiType;
                Factor = TypeUi switch
                {
                    0 => 142,
                    1 => 195,
                    _ => 130
                };
                _uiSettings = TypeUi switch
                {
                    0 => Instance.config.CombatBlockInterface.InterfaceSettingsVariant0,
                    1 => Instance.config.CombatBlockInterface.InterfaceSettingsVariant1,
                    2 => Instance.config.CombatBlockInterface.InterfaceSettingsVariant2,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _fade = _uiSettings.SmoothTransition;
              

                switch (TypeUi)
                {
                    case 0:
                        BuildingCombatBlockMain();
                        BuildingCombatBlockUpdated();
                        break;
                    case 1:
                        BuildingCombatBlockMainV2();
                        BuildingCombatBlockUpdatedV2();
                        break;
                    case 2:
                        BuildingCombatBlockMainV3();
                        BuildingCombatBlockUpdatedV3();
                        break;
                }
            }

			private static void AddInterface(string name, string json)
			{
				if (_instance._interfaces.ContainsKey(name))
				{
					Instance.PrintError($"Error! Tried to add existing cui elements! -> {name}");
					return;
				}

				_instance._interfaces.Add(name, json);
			}

			public static string GetInterface(string name)
			{
				string json;
				if (_instance._interfaces.TryGetValue(name, out json) == false)
				{
					Instance.PrintWarning($"Warning! UI elements not found by name! -> {name}");
				}

				return json;
			}

			public static void DestroyAll()
			{
				foreach (BasePlayer player in BasePlayer.activePlayerList)
					CuiHelper.DestroyUi(player, CB_MAIN);
			}

			
			
			private void BuildingCombatBlockMain()
            {
		   		 		  						  	   		  		 			  	   		  		  		   		 
				CuiElementContainer container = new CuiElementContainer();
                
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0", FadeIn = _fade },
                    RectTransform ={ AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = $"{-179.006 + Instance.config.CombatBlockInterface.OffsetX} {-110.5 + Instance.config.CombatBlockInterface.OffsetY}", OffsetMax = $"{-0.006 + Instance.config.CombatBlockInterface.OffsetX} {-40.5 + Instance.config.CombatBlockInterface.OffsetY}" }
                },Instance.config.CombatBlockInterface.Layers ,CB_MAIN);
                
                container.Add(new CuiElement
                {
                    Name = "CB_BACKGROUND",
                    Parent = CB_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.BackgroundColor, Png = _imageUI.GetImage("CB_FON0"), FadeIn = _fade },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.ProgressBarBackgroundColor, FadeIn = _fade },
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-71.001 9.367", OffsetMax = "70.999 12.033" }
                },CB_MAIN,CB_PROGRESS_BAR);
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-71.001 -9.637", OffsetMax = "84.101 14.717" },
                    Text = { Text = "%Descriptions%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = _uiSettings.SecondaryTextColor, FadeIn = _fade }
                }, CB_MAIN);
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-71.001 14.717", OffsetMax = "5.043 29.111"},
                    Text = { Text = "%Title%", Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = _uiSettings.MainTextColor , FadeIn = _fade}
                }, CB_MAIN);
                
                container.Add(new CuiElement
                {
                    Name = "CB_ICON",
                    Parent = CB_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.IconColor, Png = _imageUI.GetImage("CB_VARIANT0_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "6.38 16.914", OffsetMax = "15.62 26.914" }
                    }
                });
		   		 		  						  	   		  		 			  	   		  		  		   		 
                AddInterface(CB_MAIN, container.ToJson());
			}
            
            private void BuildingCombatBlockUpdated()
            {
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.ProgressBarMainColor, FadeIn = _fade },
                    RectTransform ={ AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -1.333", OffsetMax = "%left% 1.333" }
                },CB_PROGRESS_BAR,CB_PROGRESS);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-71.001 -21.162", OffsetMax = "35.499 -9.638"},
                    Text = {  Text = "%TimeLeft%", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = _uiSettings.MainTextColor }
                }, CB_MAIN, CB_PROGRESS_TIMER);

                AddInterface(CB_PROGRESS_BAR, container.ToJson());
            }
                        
            
			private void BuildingCombatBlockMainV2()
            {
				CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0", FadeIn = _fade },
                    RectTransform ={ AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = $"{-200.01 + Instance.config.CombatBlockInterface.OffsetX} {-96 + Instance.config.CombatBlockInterface.OffsetY}", OffsetMax = $"{-0.01 + Instance.config.CombatBlockInterface.OffsetX} {-56 + Instance.config.CombatBlockInterface.OffsetY}"
                    }

                },Instance.config.CombatBlockInterface.Layers,CB_MAIN);
                
                container.Add(new CuiElement
                {
                    Name = "CB_BACKGROUND",
                    Parent = CB_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.BackgroundColor , Png = _imageUI.GetImage("CB_FON1"), FadeIn = _fade },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.001 -17", OffsetMax = "99.999 20" }
                    }
                });
                
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.ProgressBarBackgroundColor, FadeIn = _fade },
                    RectTransform ={ AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-97.5 0", OffsetMax = "97.5 3.33" }
                },CB_MAIN,CB_PROGRESS_BAR);

                container.Add(new CuiLabel
                {
                    RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.622 0", OffsetMax = "5.368 15.562" },
                    Text = {  Text = "%Title%", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = _uiSettings.MainTextColor, FadeIn = _fade }
                }, CB_MAIN);
                
                container.Add(new CuiLabel
                {
                    RectTransform = {  AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.622 -12.76", OffsetMax = "68.698 2.76" },
                    Text = { Text = "%Descriptions%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = _uiSettings.SecondaryTextColor, FadeIn = _fade}
                }, CB_MAIN);
                
                container.Add(new CuiElement
                {
                    Name = "CB_ICON_FON",
                    Parent = CB_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.AdditionalElementsColor, Png = _imageUI.GetImage("CB_VARIANT1_ICON_FON") },
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "8.199 -10", OffsetMax = "32.199 13" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "CB_ICON",
                    Parent = "CB_ICON_FON",
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.IconColor, Png = _imageUI.GetImage("CB_VARIANT1_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-9 -8.5", OffsetMax = "9 8.5" }
                    }
                });
                
                AddInterface(CB_MAIN, container.ToJson());
			}
            
            private void BuildingCombatBlockUpdatedV2()
            {
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.ProgressBarMainColor, FadeIn = _fade },
                    RectTransform ={ AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -1.665", OffsetMax = "%left% 1.665" }
                },CB_PROGRESS_BAR,CB_PROGRESS);
                AddInterface(CB_PROGRESS_BAR, container.ToJson());
            }
                        
            
			private void BuildingCombatBlockMainV3()
            {
				CuiElementContainer container = new CuiElementContainer();
                
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0", FadeIn = _fade },
                    RectTransform ={ AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = $"{-200 + Instance.config.CombatBlockInterface.OffsetX} {-104 + Instance.config.CombatBlockInterface.OffsetY}", OffsetMax = $"{0 + Instance.config.CombatBlockInterface.OffsetX} {-48 + Instance.config.CombatBlockInterface.OffsetY}"
                    }
                },Instance.config.CombatBlockInterface.Layers,CB_MAIN);
                
                container.Add(new CuiElement
                {
                    Name = "CB_BACKGROUND",
                    Parent = CB_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.BackgroundColor, Png = _imageUI.GetImage("CB_FON2"), FadeIn = _fade },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.ProgressBarBackgroundColor, FadeIn = _fade },
                    RectTransform ={ AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-46.03 9.425", OffsetMax = "83.97 12.755" }
                },CB_MAIN,CB_PROGRESS_BAR);
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15.954 1.549", OffsetMax = "83.97 21.851" },
                    Text = { Text = "%Title%", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleRight, Color = _uiSettings.MainTextColor, FadeIn = _fade}
                }, CB_MAIN);
                
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.AdditionalElementsColor },
                    RectTransform ={ AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-7.9 -25", OffsetMax = "-1.9 26" }
                },CB_MAIN,"CB_RIGHT_PANEL");
                
                container.Add(new CuiElement
                {
                    Name = "CB_ICON",
                    Parent = CB_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = _uiSettings.IconColor, Png = _imageUI.GetImage("CB_VARIANT2_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "2.3 -20", OffsetMax = "42.3 20" }
                    }
                });
                
                AddInterface(CB_MAIN, container.ToJson());
			}
            
            private void BuildingCombatBlockUpdatedV3()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = _uiSettings.ProgressBarMainColor, FadeIn = _fade },
                    RectTransform ={ AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -1.665", OffsetMax = "%left% 1.665" }
                },CB_PROGRESS_BAR,CB_PROGRESS);
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-46.03 -12.245", OffsetMax = "83.97 5.465" },
                    Text = { Text = "%TimeLeft%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = _uiSettings.SecondaryTextColor, FadeIn = _fade}
                }, CB_MAIN, CB_PROGRESS_TIMER);

                AddInterface(CB_PROGRESS_BAR, container.ToJson());
            }
            		}

		private void OnRaidBlockStarted(BasePlayer player)
		{
			if (player != null && player.gameObject != null)
			{
				if (player.gameObject.TryGetComponent(out CombatPlayer combatBlocker))
					combatBlocker.KillComponent(true);
			}
		}

				private bool IsRaidBlocked(BasePlayer player, bool chat = false)
		{
			if (!config.CombatBlockMain.CombatBlockOnRaidBlock && !chat)
				return false;
			if (RaidBlock)
				return RaidBlock.Call<bool>("IsRaidBlocked", player);
			if (NoEscape)
				return NoEscape.Call<bool>("IsRaidBlocked", player);

			return false;
		}

				
				[PluginReference] Plugin RaidBlock, NoEscape, IQChat, Friends, Clans, Duelist, Duel, EventHelper, Battles, ArenaTournament;
		
				
		        private Configuration config;

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

		private void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
		{
			if (hitInfo.HitEntity is not BasePlayer target) return;
			if (!config.BlockDetect.ActivateOnNpcAttack && target.IsNpc) return;
			if (!config.BlockDetect.ActivateOnSleeperAttack && target.IsSleeping()) return;
			if (!config.BlockDetect.ActivateOnNpcDamageReceived && attacker.IsNpc) return;
		   		 		  						  	   		  		 			  	   		  		  		   		 
			if (!target.IsNpc && !attacker.IsNpc)
				if (IsFriends(attacker.userID.Get(), target.userID.Get()) || IsClans(attacker.UserIDString, target.UserIDString))
					return;

			if (!IsRaidBlocked(target) && !IsDuel(target.userID.Get()))
				StartCombatBlocked(target);
			if (!IsRaidBlocked(attacker) && !IsDuel(attacker.userID.Get()))
				StartCombatBlocked(attacker);
		}

		private const string _permIgnoreCombat = ".ignore";

		private Boolean IsCombatBlocked(UInt64 playerID)
		{
			BasePlayer player = BasePlayer.FindByID(playerID);
			return player != null && IsCombatBlock(player);
		}

		private object OnServerCommand(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			if (player == null || arg.cmd.FullName == "chat.say") return null;
			
			String command = arg.cmd.Name;
			if (arg.Args != null && arg.Args.Length != 0)
				command += " " + String.Join(" ", arg.Args);
            
			String onlyCommand = !String.IsNullOrWhiteSpace(command) && command.Contains(" ") ? command.Substring(0, command.IndexOf(" ", StringComparison.Ordinal)) : command;
			
			return config.ActionsBlocked.BlockedCommands.Contains(onlyCommand.ToLower()) ? CanActions(player) : null;
		}
		private void SendChat(string message, BasePlayer player, Single timeout = 0f, Chat.ChatChannel channel = Chat.ChatChannel.Global)
		{
			if(IsRaidBlocked(player, true))
				return;
			if (_playerInCache.Contains(player)) return;
			if (timeout != 0)
			{
				_playerInCache.Add(player);
				player.Invoke(() => _playerInCache.Remove(player), timeout);
			}
            
			Configuration.IQChat chat = config.IQChatSetting;
			if (IQChat)
				if (chat.UIAlertUse)
					IQChat?.Call("API_ALERT_PLAYER_UI", player, message);
				else IQChat?.Call("API_ALERT_PLAYER", player, message, chat.CustomPrefix, chat.CustomAvatar);
			else player.SendConsoleCommand("chat.add", channel, 0, message);
		}
		private const bool LanguageEn = false;
        		
				
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["COMBATBLOCK_ACTION_BLOCKED"] = "You are prohibited from performing this action during a <color=#C26D33>combat-block</color>, please wait {0}",
				["COMBATBLOCK_END_BLOCK"] = "Block <color=#738D45>deactivated</color>.\nFunctions unlocked",
				["COMBATBLOCK_ENTER_COMBAT_INITIATOR"] = "Combat activity!\nCombat-block activated for <color=#C26D33>{0}</color>.\nSome functions are temporarily unavailable",
				["COMBATBLOCK_UI_TIMER"] = "Time remaining {0}",
				["COMBATBLOCK_UI_TITLE"] = "COMBAT BLOCK",
				["COMBATBLOCK_UI_DESCRIPTIONS_V1"] = "Combat activity, some functions have been restricted",
				["COMBATBLOCK_UI_DESCRIPTIONS_V2"] = "Some functions have been disabled",
			}, this);
			
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["COMBATBLOCK_ACTION_BLOCKED"] = "Вам запрещено совершать это действие во время <color=#C26D33>комбат-блока</color>, подождите {0}",
				["COMBATBLOCK_END_BLOCK"] = "Блок <color=#738D45>деактивирован</color>.\nФункции разблокированы",
				["COMBATBLOCK_ENTER_COMBAT_INITIATOR"] = "Боевая активность!\nАктивирован комбат-блок на <color=#C26D33>{0}</color>.\nНекоторые функции временно недоступны.",
				["COMBATBLOCK_UI_TIMER"] = "Осталось {0}",
				["COMBATBLOCK_UI_TITLE"] = "COMBAT BLOCK",
				["COMBATBLOCK_UI_DESCRIPTIONS_V1"] = "Боевая активность, некоторые функции были ограничены",
				["COMBATBLOCK_UI_DESCRIPTIONS_V2"] = "Некоторые функции были отключены",
			}, this, "ru");
		}
		
		public static CombatBlock Instance;

        private class Configuration
        {
             
            [JsonProperty(LanguageEn ? "Primary combat settings" : "Основные настройки комбат-блока")] 
            public CombatBlock CombatBlockMain = new();
	        public class CombatBlockActionsBlocked
	        {
		        [JsonProperty(LanguageEn ? "Disable teleportation capability (true - yes/false - no)" : "Блокировать возможность телепортироваться (true - да/false - нет)")]
		        public bool CanTeleport = true;
		        [JsonProperty(LanguageEn ? "Disable the use of kits (true - yes/false - no)" : "Блокировать возможность использования китов (true - да/false - нет)")]
		        public bool CanUseKit = true;
		        [JsonProperty(LanguageEn ? "Disable trade functionality (true - yes/false - no)" : "Блокировать возможность обмена (Trade) (true - да/false - нет)")]
		        public bool CanTrade = true;

		        [JsonProperty(LanguageEn ? "List of prohibited commands during active lockdown [Specify them without a slash (/)]" : "Список запрещенных команд при активной блокировки [указывайте их без слэша (/)]", ObjectCreationHandling = ObjectCreationHandling.Replace)]
		        public List<string> BlockedCommands = new List<string>()
		        {
			        "commandExample",
		        };
	        }
	        public class CombatBlock
	        {
		        
		        [JsonProperty(LanguageEn ? "Disable the combat block when receiving a raid block (true - yes/false - no)" : "Отключить комбат блок при получении рейд блока (true - да/false - нет)")]
                public bool CombatBlockOnRaidBlock = true;
		        [JsonProperty(LanguageEn ? "Lockout time (seconds)" : "Время блокировки (секунды)")]
		        public int CombatBlockDuration = 150;
	        }
            [JsonProperty(LanguageEn ? "Interface settings" : "Настройки интерфейса")] 
            public CombatBlockUi CombatBlockInterface = new();
             
             public class IQChat
             {
	             [JsonProperty(LanguageEn ? "IQChat : Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
	             public String CustomPrefix = "[<color=#C26D33>CombatBlock</color>]";
	             [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat (If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
	             public String CustomAvatar = "0";
	             [JsonProperty(LanguageEn ? "IQChat : Use UI notifications" : "IQChat : Использовать UI уведомления")]
	             public Boolean UIAlertUse = false;
             }
            [JsonProperty(LanguageEn ? "Trigger settings" : "Настройка триггеров")] 
            public CombatBlockDetect BlockDetect = new();
            
             public class CombatBlockUi
            {
                [JsonProperty(LanguageEn ? "Interface variant (0, 1, 2) - example: " : "Вариант интерфейса (0, 1, 2)")]
                public int UiType = 0;
                [JsonProperty(LanguageEn ? "Interface layer: Overlay - will overlay other UI, Hud - will be overlaid by other interfaces" : "Слой интерфейса : Overlay - будет перекрывать другие UI, Hud - будет перекрываться другим интерфейсом")]
                public string Layers = "Hud";
                [JsonProperty(LanguageEn ? "Vertical padding" : "Вертикальный отступ")]
                public int OffsetY = 0;
                [JsonProperty(LanguageEn ? "Horizontal padding" : "Горизонтальный отступ")]
                public int OffsetX = 0;
                
                [JsonProperty(LanguageEn ? "Interface settings for variant 0" : "Настройки интерфейса для варианта 0")]
                public CombatBlockUiSettings InterfaceSettingsVariant0 = new()
                {
                    BackgroundColor = "0.1921569 0.1921569 0.1921569 1",
                    IconColor = "0 0.7764706 1 1",
                    AdditionalElementsColor = "",
                    MainTextColor = "1 1 1 1",
                    SecondaryTextColor = "1 1 1 0.5019608",
                    ProgressBarMainColor = "0.3411765 0.5490196 0.9607843 1",
                    ProgressBarBackgroundColor = "1 1 1 0.1019608",
                    SmoothTransition = 0.222f,
                };
                
                [JsonProperty(LanguageEn ? "Interface settings for variant 1" : "Настройки интерфейса для варианта 1")]
                public CombatBlockUiSettings InterfaceSettingsVariant1 = new()
                {
                    BackgroundColor = "0.9607843 0.772549 0.7333333 0.7019608",
                    IconColor = "1 1 1 1",
                    AdditionalElementsColor = "0.9215686 0.3058824 0.172549 1",
                    MainTextColor = "0.1921569 0.192081 0.1921569 1",
                    SecondaryTextColor = "0.1320755 0.1320755 0.1320755 1",
                    ProgressBarMainColor = "0.9215686 0.3058824 0.172549 1",
                    ProgressBarBackgroundColor = "1 1 1 0.4117647",
                    SmoothTransition = 0.222f
                };
                
                [JsonProperty(LanguageEn ? "Interface settings for variant 2" : "Настройки интерфейса для варианта 2")]
                public CombatBlockUiSettings InterfaceSettingsVariant2 = new()
                {
                    BackgroundColor = "0.1921569 0.1921569 0.1921569 1",
                    IconColor = "0.9411765 0.3137255 0.286081 1",
                    AdditionalElementsColor = "0.9568627 0.3607843 0.2627451 1",
                    MainTextColor = "1 1 1 1",
                    SecondaryTextColor = "1 1 1 0.5019608",
                    ProgressBarMainColor = "1 1 1 1",
                    ProgressBarBackgroundColor = "1 1 1 0.4117647",
                    SmoothTransition = 0.222f
                };
                
                public class CombatBlockUiSettings
                {
                    [JsonProperty(LanguageEn ? "Background color (RGBA)" : "Цвет фона (RGBA)")]
                    public string BackgroundColor;
                    [JsonProperty(LanguageEn ? "Icon color (RGBA)" : "Цвет иконки (RGBA)")]
                    public string IconColor;
                    [JsonProperty(LanguageEn ? "Color of additional elements (RGBA)" : "Цвет дополнительных элементов (RGBA)")]
                    public string AdditionalElementsColor;
                    [JsonProperty(LanguageEn ? "Main text color (RGBA)" : "Цвет основного текста (RGBA)")]
                    public string MainTextColor;
                    [JsonProperty(LanguageEn ? "Secondary text Color (RGBA)" : "Цвет второстепенного текста (RGBA)")]
                    public string SecondaryTextColor;
                    [JsonProperty(LanguageEn ? "Main color of the progress-bar (RGBA)" : "Основной цвет прогресс-бара (RGBA)")]
                    public string ProgressBarMainColor;
                    [JsonProperty(LanguageEn ? "Background Color of the Progress Bar (RGBA)" : "Цвет фона прогресс-бара (RGBA)")]
                    public string ProgressBarBackgroundColor;
                    [JsonProperty(LanguageEn ? "Delay before the UI appears and disappears (for smooth transitions)" : "Задержка перед появлением и исчезновением UI (для плавности)")]
                    public float SmoothTransition;
                }
            }
            [JsonProperty(LanguageEn ? "Setting IQChat" : "Настройка IQChat")]
            public IQChat IQChatSetting = new IQChat();
	        public class CombatBlockDetect
	        {
		        [JsonProperty(LanguageEn ? "Activate combat mode upon NPC attack (true - yes/false - no)" : "Активировать комбат-блок при атаке NPC (true - да/false - нет)")]
		        public bool ActivateOnNpcAttack = true;
		        [JsonProperty(LanguageEn ? "Activate combat mode upon receiving damage from NPCs (true - yes/false - no)" : "Активировать комбат-блок при получении урона от NPC (true - да/false - нет)")]
		        public bool ActivateOnNpcDamageReceived = true;
		        [JsonProperty(LanguageEn ? "Activate combat block when dealing damage to a sleeping player (true - yes/false - no)" : "Активировать комбат-блок при нанесении урона спящему игроку (true - да/false - нет)")]
		        public bool ActivateOnSleeperAttack = false;
		        [JsonProperty(LanguageEn ? "Deactivate combat mode after death (true - yes/false - no)" : "Деактивировать комбат-блок после смерти (true - да/false - нет)")]
		        public bool DeactivateOnPlayerDeath = true;
	        }
            [JsonProperty(LanguageEn ? "Combat mode restrictions settings" : "Настройка ограничений во время комбат-блока")] 
            public CombatBlockActionsBlocked ActionsBlocked = new();
            
        }

		
		
		private object OnPlayerCommand(BasePlayer player, String command, String[] args)
		{
			if (player == null) return null;
            
			if (args != null && args.Length != 0)
				command += " " + String.Join(" ", args);
            
			String onlyCommand = !String.IsNullOrWhiteSpace(command) && command.Contains(" ") ? command.Substring(0, command.IndexOf(" ", StringComparison.Ordinal)) : command;
            
			return config.ActionsBlocked.BlockedCommands.Contains(onlyCommand.ToLower()) ? CanActions(player) : null;
		}

        private void ValidateConfig()
        {
            
        }


		
		
		private static class ObjectCache
		{
			private static readonly object True = true;
			private static readonly object False = false;
		   		 		  						  	   		  		 			  	   		  		  		   		 
			private static class StaticObjectCache<T>
			{
				private static readonly Dictionary<T, object> CacheByValue = new Dictionary<T, object>();

				public static object Get(T value)
				{
					object cachedObject;
					if (!CacheByValue.TryGetValue(value, out cachedObject))
					{
						cachedObject = value;
						CacheByValue[value] = cachedObject;
					}

					return cachedObject;
				}
			}

			public static object Get<T>(T value)
			{
				return StaticObjectCache<T>.Get(value);
			}

			public static object Get(bool value)
			{
				return value ? True : False;
			}
		}

				
				private object CanTeleport(BasePlayer player) => CanActions(player, true);

		
		
		private bool IsFriends(ulong userID, ulong targetID)
		{
			if (Friends is not null)
				return Friends.Call("HasFriend", userID, targetID) is true;
    
			return RelationshipManager.ServerInstance.playerToTeam.TryGetValue(userID, out RelationshipManager.PlayerTeam team) && team.members.Contains(targetID);
		}
		private object CanActions(BasePlayer player, bool returnMessage = false)
		{
			if (!IsCombatBlock(player)) return null;
			CombatPlayer combatP = _combatPlayers[player];
			
			RunEffect(player, "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab");
			if (returnMessage == false)
			{
				SendChat("COMBATBLOCK_ACTION_BLOCKED".GetAdaptedMessage(player.UserIDString, combatP.UnblockTimeLeft.ToTimeFormat()), player);
				return false;
			}

			return "COMBATBLOCK_ACTION_BLOCKED".GetAdaptedMessage(player.UserIDString, combatP.UnblockTimeLeft.ToTimeFormat());

		}
		private static InterfaceBuilder _interface;

		
		
		private void OnEntityTakeDamage(BasePlayer target, HitInfo hitInfo)
		{
			if (target == null || hitInfo == null) return;
			if (target.IsNpc || hitInfo.Initiator is not BasePlayer attacker) return;
			if(!attacker.IsNpc) return;

			if (!IsRaidBlocked(target) && !IsDuel(target.userID))
				StartCombatBlocked(target);
		}

        	}
}
