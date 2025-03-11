using System; 
using System.Reflection; 
using System.Collections.Generic; 
using System.Collections; 
using System.Linq; 
using UnityEngine; 
using Facepunch.Steamworks; 
using Oxide.Core; 
using Oxide.Core.Libraries; 
using Oxide.Core.Plugins; 
using Newtonsoft.Json; 
using Oxide.Game.Rust.Cui;  

namespace Oxide.Plugins 
{ 
    [Info("SkinBox", "FuJiCuRa", "1.12.8", ResourceId = 17)] 
      //  Слив плагинов server-rust by Apolo YouGame
	class SkinBox : RustPlugin 
	{ 
	    [PluginReference] Plugin QuickSort, SortItems, ServerRewards, Economics, StacksExtended;  
		
		bool skinsLoaded; 
		bool Initialized; 
		bool Changed; 
		static SkinBox skinBox = null;
		bool isValidated;
		bool activeServerRewards;
		bool activeEconomics;
		bool activePointSystem;
		int maxItemsShown;
		bool _hasStacksExtended;
		bool _stacksExtendedExtrasDisabled;
		
		Dictionary<string, LinkedList<ulong>> skinsCache = new Dictionary<string, LinkedList<ulong>>();
		Dictionary<string, LinkedList<ulong>> skinsCacheLimited = new Dictionary<string, LinkedList<ulong>>();
		Dictionary<string, int> approvedSkinsCount = new Dictionary<string, int>();
		
		List<ulong> checkedPlayers = new List <ulong>();
		
		Dictionary<string, DateTime> cooldownTimes = new Dictionary<string, DateTime>();
		Dictionary<string, string> NameToItemName = new Dictionary<string, string>();
		Dictionary<string, string> ItemNameToName = new Dictionary<string, string>();
		Dictionary <string, object> manualAddedSkinsPre = new Dictionary <string, object>();
		Dictionary <string, List<ulong>> manualAddedSkins = new Dictionary <string, List<ulong>>();

		List<ulong> excludedSkins = new List<ulong>(); 
		List<object> excludedSkinsPre = new List<object>();

		Dictionary <ulong, Vector3> activeBoxUsers = new Dictionary <ulong, Vector3>(); 
		Dictionary <ulong, SkinBoxHandler> activeSkinBoxes = new Dictionary <ulong, SkinBoxHandler>(); 
		Dictionary <ulong, string> skinWorkshopNames = new Dictionary <ulong, string>();

		List<Workshop.Query> getRankedQueries; 
		List<Workshop.Query> getApprovedQueries; 
		
		int countQueriedGetRanked; 
		int countQueriedGetApproved;

		IEnumerator _getApprovedWorkshopSkins;
		IEnumerator _getRankedWorkshopSkins;
		IEnumerator _validateManualAddedSkins;
		IEnumerator _searchWorkshopSkin;

		string skinBoxCommand; 
		string permissionUse;
		bool forceClientPreload;
		bool forceClientAlways;
		bool forceAlsoWearables;
		bool showLoadedSkinCounts;
		int exludedSkinsAuthLevel;
		bool hideQuickSort;
		bool hideSortItems;
		float boxUseDistance;
		string steamApiKey;
		int accessOverrideAuthLevel;
		bool allowStackedItems;
		bool enableCustomPerms;
		string permCustomPlayerwearable;
		string permCustomWeapon;
		string permCustomDeployable;
		bool useInbuiltSkins;
		bool useApprovedSkins;
		int approvedSkinsLimit;
		bool useManualAddedSkins;
		bool useWebskinsRankedByTrend;
		int usedRankedByTrendDays;
		int usedThreadsGetRanked;
		int maxPagesShown;
		bool enableCooldown;
		int cooldownBox;
		bool cooldownOverrideAdmin;
		int cooldownOverrideAuthLevel;
		bool activateAfterSkinTaken;
		bool enableUsageCost;
		bool useServerRewards;
		bool useEconomics;
		int costBoxOpen;
		int costWeapon;
		int costPlayerwearable;
		int costDeployable;
		bool costExcludeAdmins;
		string costExcludePerm;
		bool costExcludePermEnabled;

		object GetConfig(string menu, string datavalue, object defaultValue)
		{
			var data = Config[menu] as Dictionary<string, object>;
			if (data == null)
			{
				data = new Dictionary<string, object>();
				Config[menu] = data;
				Changed = true;
			} 
			object value; 
			
			if (!data.TryGetValue(datavalue, out value))
			{
				value = defaultValue;
				data[datavalue] = value;
				Changed = true; 
			} 
			return value; 
		}  
		
		void LoadVariables()
		{
			useInbuiltSkins =  Convert.ToBoolean(GetConfig("AvailableSkins", "useInbuiltSkins", true));
			useApprovedSkins =  Convert.ToBoolean(GetConfig("AvailableSkins", "useApprovedSkins", true));
			approvedSkinsLimit = Convert.ToInt32(GetConfig("AvailableSkins", "approvedSkinsLimit", -1));
			useManualAddedSkins =  Convert.ToBoolean(GetConfig("AvailableSkins", "useManualAddedSkins", true));
			useWebskinsRankedByTrend =  Convert.ToBoolean(GetConfig("AvailableSkins", "useWebskinsRankedByTrend", false));
			usedRankedByTrendDays = Convert.ToInt32(GetConfig("AvailableSkins", "usedRankedByTrendDays", 30));
			usedThreadsGetRanked = Convert.ToInt32(GetConfig("AvailableSkins", "usedThreadsGetRanked", 3));
			maxPagesShown = Convert.ToInt32(GetConfig("AvailableSkins", "maxPagesShown", 1));
			
			skinBoxCommand = Convert.ToString(GetConfig("Settings", "skinBoxCommand", "skinbox"));
			permissionUse = Convert.ToString(GetConfig("Settings", "permissionUse", "skinbox.use"));
			forceClientPreload = Convert.ToBoolean(GetConfig("Settings", "forceClientPreload", false));
			forceClientAlways = Convert.ToBoolean(GetConfig("Settings", "forceClientAlways", false));
			forceAlsoWearables = Convert.ToBoolean(GetConfig("Settings", "forceAlsoWearables", true));
			showLoadedSkinCounts = Convert.ToBoolean(GetConfig("Settings", "showLoadedSkinCounts", true));
			exludedSkinsAuthLevel =  Convert.ToInt32(GetConfig("Settings", "exludedSkinsAuthLevel", 2));
			accessOverrideAuthLevel = Convert.ToInt32(GetConfig("Settings", "accessOverrideAuthLevel", 2));
			hideQuickSort = Convert.ToBoolean(GetConfig("Settings", "hideQuickSort", true));
			hideSortItems = Convert.ToBoolean(GetConfig("Settings", "hideSortItems", true));
			allowStackedItems = Convert.ToBoolean(GetConfig("Settings", "allowStackedItems", false));
			boxUseDistance = Convert.ToSingle(GetConfig("Settings", "boxUseDistance", 5.0));
			steamApiKey = Convert.ToString(GetConfig("Settings", "steamApiKey", ""));
			
			enableCustomPerms = Convert.ToBoolean(GetConfig("CustomPermissions", "enableCustomPerms", false));
			permCustomPlayerwearable = Convert.ToString(GetConfig("CustomPermissions", "permCustomPlayerwearable", "skinbox.playerwearable"));
			permCustomWeapon= Convert.ToString(GetConfig("CustomPermissions", "permCustomWeapon", "skinbox.weapon"));
			permCustomDeployable = Convert.ToString(GetConfig("CustomPermissions", "permCustomDeployable", "skinbox.deployable"));
			
			enableCooldown = Convert.ToBoolean(GetConfig("Cooldown", "enableCooldown", false));
			cooldownBox = Convert.ToInt32(GetConfig("Cooldown", "cooldownBox", 60));
			cooldownOverrideAdmin = Convert.ToBoolean(GetConfig("Cooldown", "cooldownOverrideAdmin", true));
			cooldownOverrideAuthLevel = Convert.ToInt32(GetConfig("Cooldown", "cooldownOverrideAuthLevel", 2));
			activateAfterSkinTaken = Convert.ToBoolean(GetConfig("Cooldown", "activateAfterSkinTaken", true));
			
			manualAddedSkinsPre = (Dictionary<string, object>)GetConfig("SkinsAdded", "SkinList", new Dictionary<string, object> {} );
			excludedSkinsPre = (List<object>)GetConfig("SkinsExcluded", "SkinList", new List<object> {} );
			
			enableUsageCost = Convert.ToBoolean(GetConfig("UsageCost", "enableUsageCost", false));
			useServerRewards = Convert.ToBoolean(GetConfig("UsageCost", "useServerRewards", true));
			useEconomics = Convert.ToBoolean(GetConfig("UsageCost", "useEconomics", false));
			costBoxOpen = Convert.ToInt32(GetConfig("UsageCost", "costBoxOpen", 5));
			costWeapon = Convert.ToInt32(GetConfig("UsageCost", "costWeapon", 30));
			costPlayerwearable = Convert.ToInt32(GetConfig("UsageCost", "costPlayerwearable", 20));
			costDeployable = Convert.ToInt32(GetConfig("UsageCost", "costDeployable", 10));
			costExcludeAdmins = Convert.ToBoolean(GetConfig("UsageCost", "costExcludeAdmins", true));
			costExcludePerm = Convert.ToString(GetConfig("UsageCost", "costExcludePerm", "skinbox.costexcluded"));
			costExcludePermEnabled = Convert.ToBoolean(GetConfig("UsageCost", "costExcludePermEnabled", false));

			var configremoval = false;
			
			if ((Config.Get("AvailableSkins") as Dictionary<string,object>).ContainsKey("MissingSkinNames"))
			{
				(Config.Get("AvailableSkins") as Dictionary<string,object>).Remove("MissingSkinNames"); 
				configremoval = true; 
			}

			if (!Changed &!configremoval) return;
			SaveConfig();
			Changed = false;
			configremoval = false; 
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadVariables();
		}

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				{"NoPermission", "У вас нет доступа к этой команде!"},
				//{"ToNearPlayer", "The SkinBox is currently not usable at this place"},
				{"CooldownTime", "Нельзя так часто использовать команду: /skin.\nПодождите: {0} секунд."},
				{"NotEnoughBalanceOpen", "У вас не хватает «{0} $» что бы воспользоваться командой: /skin"},
				{"NotEnoughBalanceUse", "Вам необходимо «{0} $» что бы поставить '{1}'"},
				{"NotEnoughBalanceTake", "Скин '{0}' не был очищен!\nУ вас не хватило денег!"},
			},this); 
		}

		void Loaded()
		{
			LoadVariables();
			LoadDefaultMessages();
			UnsubscribeAll();
			
			if (allowStackedItems) SubscribeSplit();
			else 
				UnsubscribeSplit(); 
			
			cmd.AddChatCommand(skinBoxCommand, this, "cmdSkinBox");
			
			if (!permission.PermissionExists(permissionUse)) permission.RegisterPermission(permissionUse, this);
			if (enableCustomPerms)
			{
				if (!permission.PermissionExists(permCustomPlayerwearable)) permission.RegisterPermission(permCustomPlayerwearable, this);
				if (!permission.PermissionExists(permCustomWeapon)) permission.RegisterPermission(permCustomWeapon, this);
				if (!permission.PermissionExists(permCustomDeployable)) permission.RegisterPermission(permCustomDeployable, this);
			}
			if (costExcludePermEnabled)
			{
				if (!permission.PermissionExists(costExcludePerm)) permission.RegisterPermission(costExcludePerm, this);
			} 
			
			skinsCache = new Dictionary<string, LinkedList<ulong>>();
			skinsCacheLimited = new Dictionary<string, LinkedList<ulong>>();
			approvedSkinsCount = new Dictionary<string, int>();
			NameToItemName = new Dictionary<string, string>();
			ItemNameToName = new Dictionary<string, string>();
			checkedPlayers = new List <ulong>();
			activeBoxUsers = new Dictionary <ulong, Vector3>();
			skinsLoaded = false;
			skinBox = this;

			if (maxPagesShown < 1) maxPagesShown = 1; maxItemsShown = (36 * maxPagesShown); 
		}

		void Unload()
		{
			var objs = UnityEngine.Object.FindObjectsOfType<SkinBoxHandler>().ToList();
			if (objs.Count > 0) foreach (var obj in objs)
			{
				if (obj.looter == null) continue;
				obj.looter.EndLooting();
				obj.PlayerStoppedLooting(obj.looter);
				GameObject.Destroy(obj);
			}
			
			if (Interface.Oxide.IsShuttingDown) return;
			if (_getApprovedWorkshopSkins != null) ServerMgr.Instance.StopCoroutine(_getApprovedWorkshopSkins);
			if (_getRankedWorkshopSkins != null) ServerMgr.Instance.StopCoroutine(_getRankedWorkshopSkins);
			if (_searchWorkshopSkin != null) ServerMgr.Instance.StopCoroutine(_searchWorkshopSkin);
			if (_validateManualAddedSkins != null) ServerMgr.Instance.StopCoroutine(_validateManualAddedSkins);
		}

		void OnServerInitialized()
		{
			if (enableUsageCost)
			{
				if (ServerRewards && useServerRewards) activeServerRewards = true;
				if (Economics && useEconomics) activeEconomics = true;
				if (activeServerRewards && activeEconomics) activeEconomics = false;
				if (activeServerRewards || activeEconomics) activePointSystem = true;
			}
			
			if (allowStackedItems && StacksExtended)
			{
				_hasStacksExtended = true;
				_stacksExtendedExtrasDisabled = (bool)StacksExtended.CallHook("DisableExtraFeatures");
				if (!_stacksExtendedExtrasDisabled)
				{
					Unsubscribe(nameof(CanStackItem));
					Unsubscribe(nameof(OnItemSplit));
				}
				else
				{
					Subscribe(nameof(CanStackItem));
					Subscribe(nameof(OnItemSplit));
				}
			}
			
			foreach (var skin in Skinnable.All.ToList())
			{
				if (skin.Name == null || skin.Name == string.Empty) continue;
				if (skin.ItemName == null || skin.ItemName == string.Empty) continue;
				if (!NameToItemName.ContainsKey(skin.Name.ToLower())) NameToItemName.Add(skin.Name.ToLower(), skin.ItemName.ToLower());
				if (!ItemNameToName.ContainsKey(skin.ItemName.ToLower())) ItemNameToName.Add(skin.ItemName.ToLower(), skin.Name.ToLower());
			}
			
			foreach (var manual in manualAddedSkinsPre) manualAddedSkins.Add(manual.Key.ToString(), (manual.Value as List<object>).ConvertAll(obj => Convert.ToUInt64(obj)));
			excludedSkins = excludedSkinsPre.ConvertAll(obj => Convert.ToUInt64(obj));
			if (useManualAddedSkins)
			{
				_validateManualAddedSkins = ValidateManualAddedSkins();
				ServerMgr.Instance.StartCoroutine(_validateManualAddedSkins);
			}
			else
			{
				isValidated = true;
				GetItemSkins();
			}
			Initialized = true;
		}

		IEnumerator ValidateManualAddedSkins()
		{
			if (showLoadedSkinCounts) Puts($"Calling workshop to validate '{manualAddedSkins.Values.Sum(list => list.Count)}' manual added skins");
			int removeCount = 0;
			foreach (var pair in manualAddedSkins.ToList())
			{
				var wsQuery = Rust.Global.SteamServer.Workshop.CreateQuery();
				wsQuery.Page = 1;
				wsQuery.PerPage = pair.Value.Count;
				wsQuery.FileId = new List<ulong>(pair.Value);
				wsQuery.Run();
				yield return new WaitWhile(new System.Func<bool>(() => wsQuery.IsRunning));

				foreach( var item in wsQuery.Items.ToList())
				{
					if (item.Title == string.Empty || item.Tags.Any(t => t.ToLower() == "version2"))
					{
						manualAddedSkins[pair.Key].Remove(item.Id);
						removeCount++;
					}
				}
				wsQuery.Dispose();
			}
			if (showLoadedSkinCounts) if (removeCount > 0)
			{
				Puts($"Removed '{removeCount}' invalids from manual skinlist");
				Config["SkinsAdded", "SkinList"] = manualAddedSkins;
				Config.Save();
			}
			else Puts($"Manual skinlist successful validated");
			isValidated = true;
			GetItemSkins();
			yield return null;
		}

		void GetItemSkins()
		{
			if (skinsLoaded) return; int countInbuilt = 0;
			foreach (var itemDef in ItemManager.GetItemDefinitions())
			{
				List<ulong> skins = new List<ulong>{ 0 };
				if (useInbuiltSkins) skins.AddRange(ItemSkinDirectory.ForItem(itemDef).Select(skin => Convert.ToUInt64(skin.id)));
				skinsCache.Add(itemDef.shortname, new LinkedList<ulong>(skins));
				if (skins.Count > 1) countInbuilt += (skins.Count -1);
			}
			if (showLoadedSkinCounts && useInbuiltSkins) Puts($"Loaded {countInbuilt} inbuilt skins");
			if (useManualAddedSkins)
			{
				int countManual = 0;
				foreach (var manualskins in manualAddedSkins)
				{
					string shortname = manualskins.Key;
					if (!ItemNameToName.ContainsKey(shortname)) continue;
					string itemname = ItemNameToName[shortname];
					List<ulong> fileids = manualskins.Value;
					foreach (var fileid in fileids)
					{
						if (!skinsCache.ContainsKey(shortname))
						{
							skinsCache.Add(shortname, new LinkedList<ulong>());
							skinsCache[shortname].AddLast(0);
						}
						if (!skinsCache[shortname].Contains(fileid))
						{
							skinsCache[shortname].AddLast(fileid);
							countManual++;
						}
					}
				}
				if (showLoadedSkinCounts && countManual > 0) Puts($"Loaded {countManual} manual added skins");
			}
			if (useApprovedSkins)
			{
				_getApprovedWorkshopSkins = GetApprovedWorkshopSkins();
				ServerMgr.Instance.StartCoroutine(_getApprovedWorkshopSkins);
				return;
			}
			else
			if (useWebskinsRankedByTrend)
			{
				_getRankedWorkshopSkins = GetRankedWorkshopSkins();
				ServerMgr.Instance.StartCoroutine(_getRankedWorkshopSkins);
				return;
			}
			else
			{
				Interface.CallHook("OnSkinCacheUpdate", new Dictionary<string, LinkedList<ulong>>(skinsCache), true);
				skinsLoaded = true;
			} 
		}

		object getSkincache()
		{
			if (skinsLoaded) return skinsCache;
			else return false;
		}

		public IEnumerator GetApprovedWorkshopSkins(bool timed = false)
		{
			getApprovedQueries = new List<Workshop.Query>();
			countQueriedGetApproved = 0;
			if (usedThreadsGetRanked < 1) usedThreadsGetRanked = 1;
			var waitCounter = new WaitWhile(() => getApprovedQueries.Count > 10 );
			if (showLoadedSkinCounts) Puts($"Checking approved skins of '{Skinnable.All.Length}' skinnables");
			foreach (var pair in ItemNameToName.ToList())
			{
				Workshop.Query wsQuery = Rust.Global.SteamServer.Workshop.CreateQuery();
				wsQuery.Page = 1;
				wsQuery.PerPage = maxItemsShown;
				wsQuery.RequireTags.Add("version3");
				wsQuery.RequireTags.Add("skin");
				wsQuery.RequireTags.Add(pair.Value.ToString());
				wsQuery.RequireAllTags = true;
				wsQuery.Order = Workshop.Order.AcceptedForGameRankedByAcceptanceDate;
				wsQuery.OnResult = new Action<Workshop.Query>(OnApprovedWorkshopQuery);
				getApprovedQueries.Add(wsQuery);
				wsQuery.Run();
				yield return waitCounter;
			}
			yield return new WaitUntil(() => getApprovedQueries.Count == 0 );
			if (showLoadedSkinCounts && countQueriedGetApproved > 0) Puts($"Imported {countQueriedGetApproved} approved skins for '{skinsCache.Where(c => c.Value.Count > 1).ToList().Count}' types");
			if (useWebskinsRankedByTrend) { _getRankedWorkshopSkins = GetRankedWorkshopSkins(); ServerMgr.Instance.StartCoroutine(_getRankedWorkshopSkins);
			}
			else
			{
				Interface.CallHook("OnSkinCacheUpdate", new Dictionary<string, LinkedList<ulong>>(skinsCache), true);
				skinsLoaded = true;
			}
		}

		void OnApprovedWorkshopQuery(Workshop.Query query)
		{
			if (query.Items == null) return;
			foreach (var item in query.Items.Where(i => i.Title != string.Empty).ToList())
			{
				foreach (var tag in item.Tags.Where(t => t.ToLower() != "version3" && t.ToLower() != "skin"))
				{
					if (NameToItemName.ContainsKey(tag))
					{
						skinWorkshopNames[item.Id] = item.Title;
						string shortname = NameToItemName[tag];
						if (!approvedSkinsCount.ContainsKey(shortname)) approvedSkinsCount[shortname] = 0;
						if (approvedSkinsLimit > 0 && approvedSkinsCount[shortname] >= approvedSkinsLimit)
						{
							if (!skinsCacheLimited.ContainsKey(shortname)) skinsCacheLimited[shortname] = new LinkedList<ulong>();
							skinsCacheLimited[shortname].AddLast(item.Id);
						}

						if (skinsCacheLimited.ContainsKey(shortname) && skinsCacheLimited[shortname].Contains(item.Id)) continue;
						if (!skinsCache.ContainsKey(shortname)) skinsCache[shortname] = new LinkedList<ulong>();
						if (!skinsCache[shortname].Contains(item.Id) && skinsCache[shortname].Count < maxItemsShown)
						{
							skinsCache[shortname].AddLast(item.Id);
							approvedSkinsCount[shortname]++;
							countQueriedGetApproved++;
						}
					}
				}
			}
			getApprovedQueries.Remove(query);
			query.Dispose();
		}

		IEnumerator GetRankedWorkshopSkins()
		{
			getRankedQueries = new List<Workshop.Query>();
			countQueriedGetRanked = 0;
			if (usedThreadsGetRanked < 1) usedThreadsGetRanked = 1;
			var waitCounter = new WaitWhile(() => getRankedQueries.Count > usedThreadsGetRanked );
			if (showLoadedSkinCounts) Puts($"Filling up the Cache by '{usedThreadsGetRanked}' workshop threads");
			foreach (var pair in ItemNameToName.ToList())
			{
				Workshop.Query wsQuery = Rust.Global.SteamServer.Workshop.CreateQuery();
				wsQuery.Page = 1;
				wsQuery.PerPage = maxItemsShown;
				wsQuery.ExcludeTags.Add("version2");
				wsQuery.RequireTags.Add("version3");
				wsQuery.RequireTags.Add("skin");
				wsQuery.RequireTags.Add(pair.Value.ToString());
				wsQuery.RequireAllTags = true;
				wsQuery.Order = Workshop.Order.RankedByTrend;
				wsQuery.RankedByTrendDays = usedRankedByTrendDays;
				wsQuery.OnResult = new Action<Workshop.Query>(OnRankedWorkshopQuery);
				getRankedQueries.Add(wsQuery);
				wsQuery.Run();
				yield return waitCounter;
			}
			yield return new WaitUntil(() => getRankedQueries.Count == 0 );
			if (showLoadedSkinCounts && countQueriedGetRanked > 0) Puts($"Cache for {ItemNameToName.Count} Skinnables filled with {countQueriedGetRanked} ranked skins");
			Interface.CallHook("OnSkinCacheUpdate", new Dictionary<string, LinkedList<ulong>>(skinsCache), true);
			skinsLoaded = true;
		}

		void OnRankedWorkshopQuery(Workshop.Query query)
		{
			if (query.Items == null) return;
			foreach (var item in query.Items.Where(i => i.Title != string.Empty).ToList())
			{
				foreach (var tag in item.Tags.Where(t => t.ToLower() != "version3" && t.ToLower() != "skin"))
				{
					if (NameToItemName.ContainsKey(tag))
					{
						skinWorkshopNames[item.Id] = item.Title;
						string shortname = NameToItemName[tag];
						if (!skinsCache.ContainsKey(shortname)) continue;
						if (!skinsCache[shortname].Contains(item.Id) && skinsCache[shortname].Count < maxItemsShown)
						{
							skinsCache[shortname].AddLast(item.Id);
							countQueriedGetRanked++;
						}
					}
				}
			}
			getRankedQueries.Remove(query);
			query.Dispose();
		}

		[ConsoleCommand("skinbox.addexcluded")]
		void consoleAddExcluded(ConsoleSystem.Arg arg)
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (arg.Args == null || arg.Args.Length < 1)
			{
				SendReply(arg, "You need to type in one or more Workshop FileId's");
				return;
			}
			
			List<ulong> fileIds = new List<ulong>();
			for (int i = 0; i < arg.Args.Length; i++)
			{
				ulong fileId = 0uL;
				if (!ulong.TryParse(arg.Args[i], out fileId))
				{
					SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
					continue;
				}
				else
				{
					if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
					{
						SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
						continue;
					}
					fileIds.Add(fileId);
				}
			}
			
			int countAdded = 0;
			foreach (var fileId in fileIds) if (!excludedSkins.Contains(fileId))
			{
				excludedSkins.Add(fileId);
				countAdded++;
			}
			if (countAdded > 0)
			{
				Config["SkinsExcluded", "SkinList"] = excludedSkins;
				Config.Save();
				SendReply(arg, $"Added {countAdded} skins to exclusion list");
			}
		}

		[ConsoleCommand("skinbox.removeexcluded")]
		void consoleRemoveExcluded(ConsoleSystem.Arg arg)
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (arg.Args == null || arg.Args.Length < 1)
			{
				SendReply(arg, "You need to type in one or more Workshop FileId's");
				return;
			}

			List<ulong> fileIds = new List<ulong>();
			for (int i = 0; i < arg.Args.Length; i++)
			{
				ulong fileId = 0uL;
				if (!ulong.TryParse(arg.Args[i], out fileId))
				{
					SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
			        continue;
				}
				else
				{
					if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
					{
						SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
						continue;
					}
					fileIds.Add(fileId);
				}
			}

			int countRemoved = 0;
			foreach (var fileId in fileIds) if (excludedSkins.Contains(fileId))
			{
				excludedSkins.Remove(fileId); countRemoved++;
			}
			if (countRemoved > 0)
			{
				Config["SkinsExcluded", "SkinList"] = excludedSkins;
				Config.Save();
				SendReplyCl(arg, $"Removed {countRemoved} skins from exclusion");
			}
		}

		[ConsoleCommand("skin.addskin")]
		void consoleAddSkin(ConsoleSystem.Arg arg)
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (arg.Args == null || arg.Args.Length < 1)
			{
				SendReply(arg, "You need to type in one or more Workshop FileId's");
				return;
			}

			List<ulong> fileIds = new List<ulong>();
			for (int i = 0; i < arg.Args.Length; i++)
			{
				ulong fileId = 0uL;
				if (!ulong.TryParse(arg.Args[i], out fileId))
				{
					SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
					continue;
				}
				else
				{
					if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
					{
						SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
						continue;
					}
					fileIds.Add(fileId);
				}
			}
			ServerMgr.Instance.StartCoroutine(SearchWorkshopSkin(fileIds, arg));
		}

		[ConsoleCommand("skinbox.removeskin")]
		void consoleRemoveSkin(ConsoleSystem.Arg arg)
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (arg.Args == null || arg.Args.Length < 1)
			{
				SendReply(arg, "You need to type in one or more Workshop FileId's");
				return;
			}

			List<ulong> fileIds = new List<ulong>();
			for (int i = 0; i < arg.Args.Length; i++)
			{
				ulong fileId = 0uL;
				if (!ulong.TryParse(arg.Args[i], out fileId))
				{
					SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
					continue;
				}
				else
				{
					if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
					{
						SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
						continue;
					}
					fileIds.Add(fileId);
				}
			}

			bool doSave = false;
			int removed = 0;
			foreach (var addedSkins in manualAddedSkins)
			{
				foreach (var fileId in fileIds)
				{
					if (addedSkins.Value.Contains(fileId))
					{
						manualAddedSkins[addedSkins.Key].Remove(fileId);
						skinsCache[addedSkins.Key].Remove(fileId);
						removed++;
						doSave = true;
					}
				}
			}
			if (doSave)
			{
				Config["SkinsAdded", "SkinList"] = manualAddedSkins;
				Config.Save(); SendReply(arg, $"Removed {removed} FileId's");
				Interface.CallHook("OnSkinCacheUpdate", new Dictionary<string, LinkedList<ulong>>(skinsCache), true);
			}
		}

		public IEnumerator SearchWorkshopSkin(List<ulong> fileIds, ConsoleSystem.Arg arg = null)
		{
			var wsQuery = Rust.Global.SteamServer.Workshop.CreateQuery();
			wsQuery.Page = 1;
			wsQuery.PerPage = 1;
			wsQuery.RequireTags.Add("version3");
			wsQuery.RequireTags.Add("version2");
			wsQuery.FileId = new List<ulong>(fileIds);
			SendReplyCl(arg, $"Calling Workshop for '{fileIds.Count}' FileId's");
			wsQuery.Run();
			yield return new WaitWhile(new System.Func<bool>(() => wsQuery.IsRunning));
			bool doSave = false;
			foreach ( var item in wsQuery.Items.Where(i => i.Title != string.Empty).ToList())
			{
				if (item.Tags.Any(t => t.ToLower() == "version2"))
				{
					SendReplyCl(arg, $"Version2 skins are not more supported in the game ({item.Id})'");
					continue;
				}
				
				bool matchedAny = false;
				foreach (var tag in item.Tags.Where(t => t.ToLower() != "version3" && t.ToLower() != "skin"))
				{
					if (NameToItemName.ContainsKey(tag))
					{
						string shortname = NameToItemName[tag];
						if (manualAddedSkins.ContainsKey(shortname))
						{
							if (manualAddedSkins[shortname].Contains(item.Id))
							{
								SendReplyCl(arg, $"'{item.Title} ({item.Id})' was already added");
								matchedAny = true;
								continue;
							}
						}
						if (skinsCache.ContainsKey(shortname)) if ((skinsCache[shortname] as LinkedList<ulong>).Contains(item.Id))
						{
							SendReplyCl(arg, $"'{item.Title} ({item.Id})' belongs already to approved/ranked");
							matchedAny = true;
							continue;
						}
						if (!manualAddedSkins.ContainsKey(shortname)) manualAddedSkins.Add(shortname, new List<ulong>());
						manualAddedSkins[shortname].Add(item.Id);
						if (!skinsCache.ContainsKey(shortname))
						{
							skinsCache.Add(shortname, new LinkedList<ulong>());
							skinsCache[shortname].AddLast(0);
						}
						skinsCache[shortname].AddAfter(skinsCache[shortname].First, item.Id);
						skinWorkshopNames[item.Id] = item.Title;
						SendReplyCl(arg, $"'{item.Title} ({item.Id})' added to the list for '{shortname}'");
						matchedAny = true;
						doSave = true;
					}
				}
				if (!matchedAny) SendReplyCl(arg, $"No shortname found for '{item.Title}|{item.Id}'. Fix it inside 'MissingSkinNames'");
			}

			if (doSave)
			{
				Config["SkinsAdded", "SkinList"] = manualAddedSkins;
				Config.Save();
				Interface.CallHook("OnSkinCacheUpdate", new Dictionary<string, LinkedList<ulong>>(skinsCache), true);
			}
			wsQuery.Dispose();
		}

		[ConsoleCommand("skinbox.addcollection")]
		void consoleAddCollection(ConsoleSystem.Arg arg)
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (steamApiKey == null || steamApiKey == string.Empty)
			{
				SendReply(arg, "This functions needs valid defined steam api-key");
				return;
			}
			if (arg.Args == null || arg.Args.Length < 1)
			{
				SendReply(arg, "You need to type in a valid collection id");
				return;
			}
			
			ulong collId = 0;
			if (!ulong.TryParse(arg.Args[0], out collId))
			{
				SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' is not a number");
				return;
			}
			else
			{
				if (arg.Args[0].Length < 9 || arg.Args[0].Length > 10)
				{
					SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' has not 9/10-Digits");
					return;
				}
			}

			var url = $"https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
			var body = $"?key={steamApiKey}&collectioncount=1&publishedfileids[0]={arg.Args[0]}";
			try
			{
				webrequest.Enqueue(url, body, (code, response) => PostCallbackAdd(code, response, arg), this, RequestMethod.POST);
			}
			catch
			{
				SendReplyCl(arg, "Steam webrequest failed!");
			}
		}

		void PostCallbackAdd(int code, string response, ConsoleSystem.Arg arg = null)
		{
			if (response == null || code != 200)
			{
				SendReplyCl(arg, "Steam webrequest failed by wrong response!");
				return;
			}
			
			var col = JsonConvert.DeserializeObject<GetCollectionDetails>(response);
			if (col == null || !(col is GetCollectionDetails))
			{
				SendReplyCl(arg, "No Collection data received!");
				return;
			}
			
			if (col.response.resultcount == 0 || col.response.collectiondetails == null || col.response.collectiondetails.Count == 0 || col.response.collectiondetails[0].result != 1)
			{
				SendReplyCl(arg, "The Steam collection could not be found!");
				return;
			}

			List<ulong> fileIds = new List<ulong>();
			foreach (var child in col.response.collectiondetails[0].children)
			{
				try
				{
					fileIds.Add(Convert.ToUInt64(child.publishedfileid)); 
				}
				catch {}
			}

			if (fileIds.Count == 0)
			{
				SendReplyCl(arg, "No skin numbers found. Workshop search cancelled.");
				return;
			}

			if (_searchWorkshopSkin != null) ServerMgr.Instance.StopCoroutine(_searchWorkshopSkin);
			_searchWorkshopSkin = SearchWorkshopSkin(fileIds, arg);
			ServerMgr.Instance.StartCoroutine(_searchWorkshopSkin);
		}

		[ConsoleCommand("skinbox.removecollection")]
		void consoleRemoveCollection(ConsoleSystem.Arg arg)
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (steamApiKey == null || steamApiKey == string.Empty)
			{
				SendReply(arg, "This functions needs valid defined steam api-key");
				return;
			}

			if (arg.Args == null || arg.Args.Length < 1)
			{
				SendReply(arg, "You need to type in a valid collection id");
				return;
			}

			ulong collId = 0;
			if (!ulong.TryParse(arg.Args[0], out collId))
			{
				SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' is not a number");
				return;
			}
			else
			{
				if (arg.Args[0].Length < 9 || arg.Args[0].Length > 10)
				{
					SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' has not 9/10-Digits");
					return;
				}
			}

			var url = $"https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
			var body = $"?key={steamApiKey}&collectioncount=1&publishedfileids[0]={arg.Args[0]}";
			try
			{
				webrequest.Enqueue(url, body, (code, response) => PostCallbackRemove(code, response, arg), this, RequestMethod.POST);
			}
			catch
			{
				SendReplyCl(arg, "Steam webrequest failed!");
			}
		}

		void PostCallbackRemove(int code, string response, ConsoleSystem.Arg arg = null)
		{
			if (response == null || code != 200)
			{
				SendReplyCl(arg, "Steam webrequest failed by wrong response!");
				return;
			}

			var col = JsonConvert.DeserializeObject<GetCollectionDetails>(response);
			if (col == null || !(col is GetCollectionDetails))
			{
				SendReplyCl(arg, "No Collection data received!");
				return;
			}

			if (col.response.resultcount == 0 || col.response.collectiondetails == null || col.response.collectiondetails.Count == 0 || col.response.collectiondetails[0].result != 1)
			{
				SendReplyCl(arg, "The Steam collection could not be found!");
				return;
			}

			List<ulong> fileIds = new List<ulong>();
			foreach (var child in col.response.collectiondetails[0].children)
			{
				try
				{
					fileIds.Add(Convert.ToUInt64(child.publishedfileid));
				}
				catch {}
			}

			if (fileIds.Count == 0)
			{
				SendReplyCl(arg, "No skin numbers found. Workshop search cancelled.");
				return;
			}

			int removed = 0;
			foreach (var addSkins in new Dictionary <string, List<ulong>>(manualAddedSkins))
			{
				foreach(var skin in addSkins.Value.ToList())
				{
					if (fileIds.Contains(skin))
					{
						manualAddedSkins[addSkins.Key].Remove(skin);
						if (skinsCache.ContainsKey(addSkins.Key)) skinsCache[addSkins.Key].Remove(skin);
						removed++;
					}
				}
			}

			if (removed > 0)
			{
				SendReplyCl(arg, $"Removed '{removed}' manual skins by collection remove.");
				Config["SkinsAdded", "SkinList"] = manualAddedSkins;
				Config.Save();
			}
			else
				SendReplyCl(arg, $"No manual skins to remove by collection remove.");
		}

		public class GetCollectionDetails
		{
			[JsonProperty("response")] 
			public Response response;

			public class Response
			{
				[JsonProperty("result")]
				public int result;

				[JsonProperty("resultcount")]
				public int resultcount;

				[JsonProperty("collectiondetails")]
				public List<Collectiondetail> collectiondetails;

				public class Collectiondetail
				{
					[JsonProperty("publishedfileid")]
					public string publishedfileid;

					[JsonProperty("result")]
					public int result;

					[JsonProperty("children")]
					public List<Child> children;

					public class Child
					{
						[JsonProperty("publishedfileid")]
						public string publishedfileid;

						[JsonProperty("sortorder")]
						public int sortorder;

						[JsonProperty("filetype")]
						public int filetype;
					}
				}
			}
		}

		void OnPlayerInit(BasePlayer player)
		{
			if (player == null) return;
			if (!forceClientPreload || (checkedPlayers.Contains(player.userID) && !forceClientAlways)) return;
			timer.Once(0.1f, () => InitPreload(player));
		}

		void InitPreload(BasePlayer player)
		{
			ItemContainer beltInv = player.inventory.containerBelt;
			ItemContainer wearInv = player.inventory.containerWear;
			var wearList = new List<Item>();
			if (forceAlsoWearables)
			{
				foreach (var item in wearInv.itemList.ToList())
				{
					wearList.Add(item);
					item.RemoveFromContainer();
				}
			    player.inventory.SendUpdatedInventory(PlayerInventory.Type.Wear, wearInv, false);
			}

			beltInv.capacity++;
			foreach ( var skinCache in skinsCache)
			{
				if ((skinCache.Value as LinkedList<ulong>).Count <= 1) continue;
				foreach (var skin in (LinkedList<ulong>)skinCache.Value)
				{
					if (skin == 0) continue;
					var itemDef = ItemManager.FindItemDefinition(skinCache.Key);
					if (itemDef == null) continue;
					Item item = ItemManager.Create(itemDef, 1, skin);
					if (item == null || !item.IsValid()) continue;
					if (item.info.category ==  ItemCategory.Attire && !forceAlsoWearables)
					{
						item.Remove(0f);
						continue;
					}

					if (item.info.category ==  ItemCategory.Attire && forceAlsoWearables)
					{
						item.MoveToContainer(wearInv);
						player.inventory.SendUpdatedInventory(PlayerInventory.Type.Wear, wearInv, false);
					}
					else
					{
						item.MoveToContainer(beltInv, 6, false);
						player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, beltInv, false);
					}

					item.RemoveFromContainer();
					item.Remove(0f);
				} 
			}

			beltInv.capacity--;
			if (forceAlsoWearables)
			{
				foreach (var item in wearList) item.MoveToContainer(wearInv);
				player.inventory.SendUpdatedInventory(PlayerInventory.Type.Wear, wearInv, false);
				wearList.Clear();
			}

			if (!checkedPlayers.Contains(player.userID)) checkedPlayers.Add(player.userID);
			ItemManager.DoRemoves();
		}

		[ConsoleCommand("skinbox.open")]
		void consoleSkinboxOpen(ConsoleSystem.Arg arg)
		{
			if (arg == null || !isValidated) return;
			if (arg.Connection == null)
			{
				if  (arg.Args == null || arg.Args.Length == 0)
				{
					SendReply(arg, $"'skinbox.open' cmd needs a passed steamid ");
					return;
				}

				ulong argId = 0uL;
				if (!ulong.TryParse(arg.Args[0], out argId))
				{
					SendReply(arg, $"'skinbox.open' cmd for '{arg.Args[0]}' failed (no valid number)");
					return;
				}

				BasePlayer argPlayer = BasePlayer.FindByID(argId);
				if (argPlayer == null)
				{
					SendReply(arg, $"'skinbox.open' cmd for userID '{argId}' failed (player not found)");
					return;
				}

				if (!argPlayer.inventory.loot.IsLooting()) OpenSkinBox(argPlayer);
			}
			else
			if(arg.Connection != null && arg.Connection.player != null)
			{
				BasePlayer player = arg.Player();
				if (player.inventory.loot.IsLooting()) return;
				if(!(player.IsAdmin || player.net.connection.authLevel >= accessOverrideAuthLevel) && !permission.UserHasPermission(player.UserIDString, permissionUse))
				{
					player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
					return;
				}

				if (!CheckDistance(player) || !CheckOpenBalance(player)) return;
				if (enableCooldown && !(cooldownOverrideAdmin && (player.IsAdmin || player.net.connection.authLevel >= cooldownOverrideAuthLevel)))
				{
					DateTime now = DateTime.UtcNow;
					DateTime time;
					var key = player.UserIDString + "-box";
					if (cooldownTimes.TryGetValue(key, out time))
					{
						if (time > now.AddSeconds(-cooldownBox))
						{
							player.ChatMessage(string.Format(lang.GetMessage("CooldownTime", this, player.UserIDString),(time - now.AddSeconds(-cooldownBox)).Seconds));
							return;
						}
					}
				}
				OpenSkinBox(player);
			}
		}

		void cmdSkinBox(BasePlayer player, string command, string[] args)
		{
			if (player.inventory.loot.IsLooting() || !isValidated) return;
			if(!(player.IsAdmin || player.net.connection.authLevel >= accessOverrideAuthLevel) && !permission.UserHasPermission(player.UserIDString, permissionUse))
			{
				player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
				return;
			}

			if (!CheckDistance(player) || !CheckOpenBalance(player)) return;
			if (enableCooldown && !(cooldownOverrideAdmin && (player.IsAdmin || player.net.connection.authLevel >= cooldownOverrideAuthLevel)))
			{
				DateTime now = DateTime.UtcNow;
				DateTime time;
				var key = player.UserIDString + "-box";
				if (cooldownTimes.TryGetValue(key, out time))
				{
					if (time > now.AddSeconds(-cooldownBox))
					{
						player.ChatMessage(string.Format(lang.GetMessage("CooldownTime", this, player.UserIDString),(time - now.AddSeconds(-cooldownBox)).Seconds));
						return;
					}
				}
			}

			timer.Once(0.2f, () =>
			{
				OpenSkinBox(player);
			});
		}

		Boolean CheckOpenBalance(BasePlayer player)
		{
			if (!activePointSystem || costBoxOpen <= 0 || (player.IsAdmin && costExcludeAdmins) || (costExcludePermEnabled && permission.UserHasPermission(player.UserIDString, costExcludePerm))) return true;
			object getMoney = null;
			if (activeServerRewards) getMoney = (int)(Interface.Oxide.CallHook("CheckPoints", player.userID) ?? 0);
			if (activeEconomics) getMoney = (double)(Interface.Oxide.CallHook("Balance", player.UserIDString) ?? 0.0);
			int playerMoney = 0;
			playerMoney = Convert.ToInt32(getMoney);
			if (playerMoney < costBoxOpen)
			{
				player.ChatMessage(string.Format(lang.GetMessage("NotEnoughBalanceOpen", this, player.UserIDString), costBoxOpen));
				return false;
			}
			
			if (activeServerRewards) Interface.Oxide.CallHook("TakePoints", player.userID, costBoxOpen);
			if (activeEconomics) Interface.Oxide.CallHook("Withdraw", player.userID, Convert.ToDouble(costBoxOpen));
			return true;
		}

		Boolean CheckSkinBalance(BasePlayer player, Item item)
		{
			if (!activePointSystem || (player.IsAdmin && costExcludeAdmins) || (costExcludePermEnabled && permission.UserHasPermission(player.UserIDString, costExcludePerm))) return true;
			object getMoney = null;
			if (activeServerRewards) getMoney = (int)(Interface.Oxide.CallHook("CheckPoints", player.userID) ?? 0);
			if (activeEconomics) getMoney = (double)(Interface.Oxide.CallHook("Balance", player.UserIDString) ?? 0.0);
			int playerMoney = 0;
			playerMoney = Convert.ToInt32(getMoney);
			bool hasBalance = false;
			int getCost = 0;
			switch (item.info.category.ToString())
			{
				case "Weapon":
				case "Tool":
   				    if (costWeapon <= 0 || playerMoney > costWeapon) hasBalance = true;
				    getCost = costWeapon;
				    break;
				case "Attire": 
				    if (costPlayerwearable <= 0 || playerMoney > costPlayerwearable) hasBalance = true;
				    getCost = costPlayerwearable;
				    break;
				case "Items":
				case "Construction":
				    if (costDeployable <= 0 || playerMoney > costDeployable) hasBalance = true;
				    getCost = costDeployable;
				    break;
				default:
				    hasBalance = true;
				    break;
			}

			if (!hasBalance)
			{
				player.ChatMessage(string.Format(lang.GetMessage("NotEnoughBalanceUse", this, player.UserIDString), getCost, item.info.displayName.translated));
				return false;
			}
			return true;
		}

		Boolean WithdrawBalance(BasePlayer player, Item item)
		{
			if (!activePointSystem || (player.IsAdmin && costExcludeAdmins) || (costExcludePermEnabled && permission.UserHasPermission(player.UserIDString, costExcludePerm))) return true;
			int getCost = 0;
			switch (item.info.category.ToString())
			{
				case "Weapon":
				case "Tool":
				    getCost = costWeapon;
					break;
				case "Attire":
				    getCost = costPlayerwearable;
					break;
				case "Items":
				case "Construction":
				    getCost = costDeployable;
					break;
				default:
				    break;
			}

			bool hadMoney = false;
			if (activeServerRewards && (bool)Interface.Oxide.CallHook("TakePoints", player.userID, getCost)) hadMoney = true;
			if (activeEconomics && (bool)Interface.Oxide.CallHook("Withdraw", player.userID, Convert.ToDouble(getCost))) hadMoney = true;
			if (!hadMoney)
			{
				player.ChatMessage(string.Format(lang.GetMessage("NotEnoughBalanceTake", this, player.UserIDString), item.info.displayName.translated));
				return false;
			}
			return true;
		}

		sealed class SkinBoxHandler : MonoBehaviour
		{
			public bool isCreating;
			public bool isBlocked;
			public bool isEmptied;
			public int itemId;
			public int itemAmount;
			public Item currentItem;
			public BasePlayer looter;
			ItemContainer loot;
			public BaseEntity entityOwner;
			public ulong skinId;
			public int currentPage;
			public int totalPages;
			public int lastPageCount;
			public LinkedList<ulong> itemSkins;
			public int skinsTotal;
			public int perPageTotal;
			public int maxPages;
			
			void Awake()
			{
				isCreating = false;
				isBlocked = false;
				isEmptied = false;
				skinId = 0uL;
				currentPage = 1;
				totalPages = 1;
				lastPageCount = 0;
				skinsTotal = 1;
				perPageTotal = 1;
				itemAmount = 1;
				maxPages = skinBox.maxPagesShown;
				itemSkins = new LinkedList<ulong>();
			}

			public void ShowUI()
			{
				if (totalPages > 1 && maxPages > 1)
				{
					var p = Math.Min(maxPages, totalPages);
					skinBox.CreateUI(looter, currentPage, p );
				}
			}

			public void CloseUI()
			{
				skinBox.DestroyUI(looter);
			}

			public void PageNext()
			{
				if (totalPages > 1 && currentPage < maxPages && currentPage < totalPages && !isCreating)
				{
					currentPage++;
					FillSkinBox(currentPage);
					ShowUI();
				}
			}

			public void PagePrev()
			{
				if (totalPages > 1 && currentPage > 1 && !isCreating)
				{
					currentPage--;
					FillSkinBox(currentPage);
					ShowUI();
				}
			}

			public void StartNewItem(ItemContainer container, Item item)
			{
				isBlocked = true;
				currentItem = item;
				itemAmount = item.amount;
				itemId = item.info.itemid;
				skinId = item.skin;
				string shortname = currentItem.info.shortname == "rifle.lr300" ? "lr300.item" : currentItem.info.shortname;
				itemSkins = new LinkedList<ulong>(skinBox.skinsCache[shortname] as LinkedList<ulong>);
				itemSkins.Remove(0uL);
				itemSkins.Remove(skinId);
				skinsTotal = itemSkins.Count;
				perPageTotal = (skinId == 0uL ? 35 : 34);
				currentPage = 1;
				totalPages = Mathf.CeilToInt( skinsTotal / (float)perPageTotal);
				lastPageCount = totalPages == 1 ? skinsTotal : skinsTotal % perPageTotal;
				loot = container;
				entityOwner = loot.entityOwner;
			}

			public void FillSkinBox(int page = 1)
			{
				isCreating = true;
				string shortname = currentItem.info.shortname == "rifle.lr300" ? "lr300.item" : currentItem.info.shortname;
				string origname = currentItem.info.shortname;
				bool hasCondition = currentItem.hasCondition;
				float condition = currentItem.condition;
				float maxCondition = currentItem.maxCondition;
				bool isWeapon = currentItem.GetHeldEntity() is BaseProjectile;
				bool hasMods = false;
				int contents = 0;
				int capacity = 0;
				ItemDefinition ammoType = null;
				Dictionary<int, float> itemMods = new Dictionary<int, float>();
				if (isWeapon)
				{
					contents =  (currentItem.GetHeldEntity() as BaseProjectile).primaryMagazine.contents;
					capacity =  (currentItem.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
					ammoType = (currentItem.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType;
					if (currentItem.contents != null && currentItem.contents.itemList.Count > 0)
					{
						hasMods = true;
						foreach ( var mod in currentItem.contents.itemList) itemMods.Add(mod.info.itemid, mod.condition);
					}
				}
				isEmptied = false;
				skinBox.RemoveItem(loot, currentItem);
				skinBox.ClearContainer(loot);
				int startIndex = ((page * perPageTotal ) - perPageTotal);
				int rangeIndex = page == totalPages ? lastPageCount : perPageTotal;
				var skins = new List<ulong> { 0uL } ;
				if (skinId != 0uL) skins.Add(skinId);
				if (maxPages > 1 && totalPages > 1) skins.AddRange(itemSkins.ToList().GetRange(startIndex, rangeIndex));
				else 
					skins.AddRange(itemSkins.ToList());
				loot.capacity = skins.Count();
				var itemDef = ItemManager.FindItemDefinition(origname);
				if (skins.Count() > 36) loot.capacity = 36;
				else
					loot.capacity = skins.Count;
				foreach (var skin in skins)
				{
					if (loot.IsFull()) break;
					if (skinBox.excludedSkins.Contains(skin) && looter.net.connection.authLevel < skinBox.exludedSkinsAuthLevel) continue;
					Item newItem = ItemManager.Create(itemDef, 1, skin);
					if (skinBox.skinWorkshopNames.ContainsKey(skin)) newItem.name = skinBox.skinWorkshopNames[skin];
					if (hasCondition)
					{
						newItem.condition = condition;
						newItem.maxCondition = maxCondition;
					}
					if (isWeapon)
					{
						var gun = (newItem.GetHeldEntity() as BaseProjectile);
						gun.primaryMagazine.contents = contents;
						gun.primaryMagazine.capacity = capacity;
						gun.primaryMagazine.ammoType = ammoType;
						if (hasMods)
						{
							foreach ( var mod in itemMods)
							{
								Item newMod = ItemManager.CreateByItemID((int)mod.Key, 1);
								newMod.condition = Convert.ToSingle(mod.Value);
								newMod.MoveToContainer(newItem.contents, -1, false);
							}
							newItem.contents.SetFlag(ItemContainer.Flag.IsLocked, true);
							newItem.contents.SetFlag(ItemContainer.Flag.NoItemInput, true);
							newItem.contents.MarkDirty();
						}
					}
					newItem.MarkDirty();
					skinBox.InsertItem(loot, newItem);
				}
				isCreating = false;
				loot.MarkDirty();
			}

			public void PlayerStoppedLooting(BasePlayer player)
			{
				skinBox.activeBoxUsers.Remove(player.userID);
				if (!isEmptied && currentItem != null)
				{
					isEmptied = true;
					player.GiveItem(currentItem);
				}
				
				if (skinBox.activeBoxUsers.Count() == 0 && !Interface.Oxide.IsShuttingDown) skinBox.UnsubscribeAll();
				if (!GetComponent<BaseEntity>().IsDestroyed) GetComponent<BaseEntity>().Kill(BaseNetworkable.DestroyMode.None);
				if (skinBox.enableCooldown) skinBox.cooldownTimes[player.UserIDString + "-box"] = DateTime.UtcNow;
			}

			void OnDestroy()
			{
				skinBox.DestroyUI(looter);
				skinBox.activeSkinBoxes.Remove(looter.userID);
				skinBox.activeBoxUsers.Remove(looter.userID);
				if (!isEmptied && currentItem!= null)
				{
					isEmptied = true; looter.GiveItem(currentItem);
				}
				looter.EndLooting();
			}
		}

		Boolean CheckDistance(BasePlayer player)
		{
			/* var playerPos = player.transform.position; foreach (var id in activeBoxUsers)
			{
				if (Vector3.Distance(playerPos, id.Value) < boxUseDistance)
				{
					player.ChatMessage(lang.GetMessage("ToNearPlayer", this, player.UserIDString));
					return false;
				}
			}*/
			return true;
		}

		void OpenSkinBox(BasePlayer player)
		{
			if (activeBoxUsers.Count() == 0) SubscribeAll();
			var skinBox = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", player.transform.position - new Vector3(UnityEngine.Random.Range(-15f, 15f), 500f+UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-15f, 15f)));
			(skinBox as BaseNetworkable).limitNetworking = true;
			UnityEngine.Object.Destroy(skinBox.GetComponent<DestroyOnGroundMissing>());
			UnityEngine.Object.Destroy(skinBox.GetComponent<GroundWatch>());
			skinBox.Spawn();
			var lootHandler = skinBox.gameObject.AddComponent<SkinBoxHandler>();
			lootHandler.looter = player;
			var container = skinBox.GetComponent<StorageContainer>();
			if (!allowStackedItems)
			{
				container.maxStackSize = 1;
				container.inventory.maxStackSize = 1;
			}
			container.inventory.capacity = 1;
			container.SetFlag(BaseEntity.Flags.Open, true, false);
			if (SortItems && hideSortItems) StartLootingEntity(player.inventory.loot, container);
			if (QuickSort && hideQuickSort) StartLootingEntity(player.inventory.loot, container);
			else
				player.inventory.loot.StartLootingEntity(container, false);
			player.inventory.loot.AddContainer(container.inventory);
			player.inventory.loot.SendImmediate();
			player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");
			container.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
			activeBoxUsers[player.userID] = player.transform.position; activeSkinBoxes.Add(player.userID, lootHandler);
		}

		public void StartLootingEntity(PlayerLoot loot, BaseEntity targetEntity)
		{
			loot.Clear();
			if (!targetEntity) return;
			loot.PositionChecks = false;
			loot.entitySource = targetEntity;
			loot.itemSource = null;
			loot.MarkDirty();
		}

		void ClearContainer(ItemContainer container)
		{
			while (container.itemList.Count > 0)
			{
				var removeItem = container.itemList[0];
				RemoveItem(container, removeItem);
				removeItem.Remove(0f);
			}
		}

		object CanAcceptItem(ItemContainer container, Item item)
		{
			if (container.entityOwner == null) return null;
			SkinBoxHandler lootHandler;
			if ( (lootHandler = container.entityOwner.GetComponent<SkinBoxHandler>()) == null) return null;
			string shortname = item.info.shortname == "rifle.lr300" ? "lr300.item" : item.info.shortname;
			if (lootHandler.isCreating || lootHandler.isBlocked || (enableCustomPerms && !CheckItemPerms(lootHandler.looter, item)) || item.isBroken || !skinsCache.ContainsKey(shortname) || (skinsCache[shortname] as LinkedList<ulong>).Count <= 1 || !CheckSkinBalance(lootHandler.looter, item)) return ItemContainer.CanAcceptResult.CannotAccept;
			return null;
		}

		bool CheckItemPerms(BasePlayer player, Item item)
		{
			string category = item.info.category.ToString();
			switch (category)
			{
				case "Weapon":
     				if (permission.UserHasPermission(player.UserIDString, permCustomWeapon)) return true;
					break;
				case "Tool":
    				if (permission.UserHasPermission(player.UserIDString, permCustomWeapon)) return true;
					break;
				case "Attire":
    				if (permission.UserHasPermission(player.UserIDString, permCustomPlayerwearable)) return true;
					break;
				case "Items":
    				if (permission.UserHasPermission(player.UserIDString, permCustomDeployable)) return true;
					break;
    			case "Construction": 
    				if (permission.UserHasPermission(player.UserIDString, permCustomDeployable)) return true;
					break;
				default:
				return true;
			}
			return false;
		}

		void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if (container == null || item == null || container.entityOwner == null) return;
			var lootHandler = container.entityOwner.GetComponent<SkinBoxHandler>();
			if (lootHandler == null || lootHandler.isBlocked) return;
			lootHandler.StartNewItem(container, item);
			lootHandler.FillSkinBox();
			if (maxPagesShown > 1) lootHandler.ShowUI();
		}

		bool InsertItem(ItemContainer container, Item item, bool mark = false)
		{
			if (container.itemList.Contains(item)) return false;
			if (container.IsFull()) return false;
			container.itemList.Add(item);
			item.parent = container;
			if (!container.FindPosition(item)) return false;
			if (mark) container.MarkDirty();
			if (container.onItemAddedRemoved != null) container.onItemAddedRemoved(item, true);
			return true;
		}

		bool RemoveItem(ItemContainer container, Item item, bool mark = false)
		{
			if (!container.itemList.Contains(item)) return false;
			if (container.onPreItemRemove != null) container.onPreItemRemove(item);
			container.itemList.Remove(item);
			item.parent = null;
			if (mark) container.MarkDirty();
			if (container.onItemAddedRemoved != null) container.onItemAddedRemoved(item, false);
			return true;
		}

		void OnItemRemovedFromContainer(ItemContainer container, Item item)
		{
			if (container == null || item == null || container.entityOwner == null) return;
			var lootHandler = container.entityOwner.GetComponent<SkinBoxHandler>();
			if (lootHandler == null || !lootHandler.isBlocked) return;
			if (item.GetHeldEntity() is BaseProjectile && item.contents != null)
			{
				item.contents.SetFlag(ItemContainer.Flag.IsLocked, false);
				item.contents.SetFlag(ItemContainer.Flag.NoItemInput, false);
			}
			if (lootHandler.itemAmount > 1)
			{
				item.amount = lootHandler.itemAmount;
				item.MarkDirty();
				lootHandler.itemAmount = 1;
			}
			ClearContainer(container);
			if (lootHandler.currentItem != null)
			{
				lootHandler.currentItem.Remove(0f);
				lootHandler.currentItem = null;
			}
			container.MarkDirty();
			lootHandler.isEmptied = true;
			lootHandler.CloseUI();
			container.capacity = 1;
			if (item.skin == 0uL)
			{
				lootHandler.skinId = 0uL;
				lootHandler.isBlocked = false;
				return;
			}
			if (!WithdrawBalance(lootHandler.looter, item))
			{
				item.skin = lootHandler.skinId;
				if (item.GetHeldEntity()) item.GetHeldEntity().skinID = lootHandler.skinId;
				item.MarkDirty();
			}
			if (enableCooldown && activateAfterSkinTaken && !(cooldownOverrideAdmin && (lootHandler.looter.IsAdmin || lootHandler.looter.net.connection.authLevel >= cooldownOverrideAuthLevel)) && item.skin != lootHandler.skinId)
			{
				skinBox.activeBoxUsers.Remove(lootHandler.looter.userID);
				lootHandler.looter.EndLooting();
				skinBox.cooldownTimes[lootHandler.looter.UserIDString + "-box"] = DateTime.UtcNow;
			}
			lootHandler.skinId = 0uL;
			lootHandler.isBlocked = false;
		}

		void UnsubscribeAll()
		{
			Unsubscribe(nameof(CanAcceptItem));
			Unsubscribe(nameof(OnItemAddedToContainer));
			Unsubscribe(nameof(OnItemRemovedFromContainer));
		}

		void SubscribeAll()
		{
			Subscribe(nameof(CanAcceptItem));
			Subscribe(nameof(OnItemAddedToContainer));
			Subscribe(nameof(OnItemRemovedFromContainer));
		}

		void UnsubscribeSplit()
		{
			Unsubscribe(nameof(OnItemSplit));
			Unsubscribe(nameof(CanStackItem));
		}

		void SubscribeSplit()
		{
			Subscribe(nameof(OnItemSplit));
			Subscribe(nameof(CanStackItem));
		}

		void SendReplyCl(ConsoleSystem.Arg arg, string format)
		{
			if (arg != null && arg.Connection != null) SendReply(arg, format);
			Puts(format);
		}

		void DestroyUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "SkinBoxUI");
		}

		void CreateUI(BasePlayer player, int page = 1, int total = 1)
		{
			var panelName = "SkinBoxUI";
			CuiHelper.DestroyUi(player, panelName);
			string contentColor = "0.7 0.7 0.7 1.0";
			string buttonColor = "0.75 0.75 0.75 0.1";
			string buttonTextColor = "0.77 0.68 0.68 1";
			var result = new CuiElementContainer();
			var rootPanelName = 
			result.Add(new CuiPanel
			{
				Image = new CuiImageComponent { Color = "0 0 0 0" },
				RectTransform = { AnchorMin = "0.9505 0.15", AnchorMax = "0.99 0.6" } 
			}, "Hud.Menu", panelName);
			
			result.Add(new CuiPanel 
			{
				Image = new CuiImageComponent { Color = "0.65 0.65 0.65 0.06" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } 
			}, rootPanelName);
			
	        result.Add(new CuiButton 
			{
				RectTransform = { AnchorMin = "0.025 0.7", AnchorMax = "0.975 1.0" },
				Button = { Command = "skinbox.pageprev", Color = buttonColor },
				Text = { Align = TextAnchor.MiddleCenter, Text = "◀", Color = buttonTextColor, FontSize = 50 } 
			}, rootPanelName);
			
		    result.Add(new CuiLabel 
			{
				RectTransform = { AnchorMin = "0.025 0.3", AnchorMax = "0.975 0.7" },
				Text = { Align = TextAnchor.MiddleCenter, Text = $"{page}\nof\n{total}", Color = contentColor, FontSize = 20 } 
			}, rootPanelName);
			
			result.Add(new CuiButton 
			{
				RectTransform = { AnchorMin = "0.025 0", AnchorMax = "0.975 0.3" },
				Button = { Command = "skinbox.pagenext", Color = buttonColor },
				Text = { Align = TextAnchor.MiddleCenter, Text = "▶", Color = buttonTextColor, FontSize = 50 } 
			}, rootPanelName);
			
			CuiHelper.AddUi(player, result);
		}

		[ConsoleCommand("skinbox.pagenext")]
		void cmdPageNext(ConsoleSystem.Arg arg)
		{
			if (maxPagesShown <= 1 || arg == null || arg.Connection == null) return;
			var player = arg.Connection?.player as BasePlayer;
			if (player == null) return;
			if (activeSkinBoxes.ContainsKey(player.userID)) activeSkinBoxes[player.userID].PageNext();
		}

		[ConsoleCommand("skinbox.pageprev")]
		void cmdPagePrev(ConsoleSystem.Arg arg)
		{
			if (maxPagesShown <= 1 || arg == null || arg.Connection == null) return;
			var player = arg.Connection?.player as BasePlayer;
			if (player == null) return;
			if (activeSkinBoxes.ContainsKey(player.userID)) activeSkinBoxes[player.userID].PagePrev();
		}

		void OnPluginUnloaded(Plugin name) 
		{
			if (Initialized && name.Name == "StacksExtended") CheckSubscriptions(true);
		}

		void OnPluginLoaded(Plugin name)
		{
			if (Initialized && name.Name == "StacksExtended") CheckSubscriptions(false);
		}

		void CheckSubscriptions(bool wasUnload)
		{
			if (!allowStackedItems) return;
			if (wasUnload)
			{
				_hasStacksExtended = false;
				_stacksExtendedExtrasDisabled = false;
				Subscribe(nameof(CanStackItem));
				Subscribe(nameof(OnItemSplit));
			}
			else
			{
				_hasStacksExtended = true;
				_stacksExtendedExtrasDisabled = (bool)StacksExtended.CallHook("DisableExtraFeatures");
				if (!_stacksExtendedExtrasDisabled)
				{
					Unsubscribe(nameof(CanStackItem));
					Unsubscribe(nameof(OnItemSplit));
				}
				else
				{
					Subscribe(nameof(CanStackItem));
					Subscribe(nameof(OnItemSplit));
				}
			}
		}

		object OnItemSplit(Item thisI, int split_Amount)
		{
			if (thisI.skin == 0uL) return null;
			Item item = null;
			item = ItemManager.CreateByItemID(thisI.info.itemid, 1, thisI.skin);
			if (item != null)
			{
				thisI.amount -= split_Amount;
				thisI.MarkDirty();
				item.amount = split_Amount;
				item.OnVirginSpawn();
				if (thisI.IsBlueprint()) item.blueprintTarget = thisI.blueprintTarget;
				if (thisI.hasCondition) item.condition = thisI.condition;
				item.MarkDirty();
				return item;
			}
			return null;
		}

		object CanStackItem(Item thisI, Item item)
		{
			if (thisI.skin == item.skin) return null;
			if (thisI.skin != item.skin) return false;
			if (thisI.skin == item.skin)
			{
				if (thisI.hasCondition && item.hasCondition) if (item.condition != thisI.condition) return false;
				return true;
			}
			return null;
		} 
	}
}
