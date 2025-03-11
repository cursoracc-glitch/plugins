using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Farmer", "TopPlugin.ru", "3.0.0")]
    class Farmer : RustPlugin
    {																			
		
		#region Variables
		
		private static System.Random Rnd = new System.Random();
		
		private static List<ulong> NeedInitPlayers = new List<ulong>();
		
		private const string EarnedScoreText = "<color=white>ОЧКИ ФЕРМЕРА</color>";
		
		private static Dictionary<string, string> Images = new Dictionary<string, string>();
		
		private const string MainPanelSizeMin = "-291 -180";
		private const string MainPanelSizeMax = "291 220";
		
		private const int MaxImages = 15; // изменять тут
		
		private const string FarmMainFon = /*"https://i.imgur.com/Zkj8Mq0.png";*/"https://i.imgur.com/yAgKJps.png";
		private const string FarmGreyFon = /*"https://i.imgur.com/YHOIQIy.png";*/"https://i.imgur.com/DT4j3D5.png";
		
		private const string FarmExit = "https://i.imgur.com/mxD6pkk.png";
		private const string FarmBack = "https://i.imgur.com/eKEwvCQ.png";
		private const string FarmExchange = "https://i.imgur.com/r2OI5aq.png";
		
		private readonly Dictionary<int, string> FarmArts = new Dictionary<int, string>()
		{
			{ 1, "https://i.imgur.com/xkvRBMp.png" },
			{ 2, "https://i.imgur.com/4U8Uyur.png" },
			{ 3, "https://i.imgur.com/srwzAIt.png" },
			{ 4, "https://i.imgur.com/uF6l4Ao.png" },
			{ 5, "https://i.imgur.com/Nzx528V.png" },
			{ 6, "https://i.imgur.com/Qnn2kpA.png" },
			{ 7, "https://i.imgur.com/EB7V0bD.png" },
			{ 8, "https://i.imgur.com/1xDvHZn.png" },
			{ 9, "https://i.imgur.com/BS6bnOI.png" },
			{ 10, "https://i.imgur.com/yH3afVL.png" }
		};				
		
		#endregion
		
		#region Hooks
		
		private void Init() 
		{
                        PrintWarning("\n-----------------------------\n" +
                        "     Author - https://topplugin.ru/\n" +
                        "     VK - https://vk.com/rustnastroika\n" +
                        "     Discord - https://discord.com/invite/5DPTsRmd3G\n" +
                        "-----------------------------");
			LoadVariables();
			LoadData();
		}				
		
		private void OnServerInitialized() => DownloadImages();
		
		private void OnServerSave() => SaveData();
		
		private void OnNewSave()
		{
			PlayerData.Clear();
			SaveData();
		}
		
		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList)
			{
				if (player == null) continue;
				CuiHelper.DestroyUi(player, MainPanel);
				CuiHelper.DestroyUi(player, InitPanel);
			}
			
			SaveData();			
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player == null) return;						
				
			if (!NeedInitPlayers.Contains(player.userID))
				NeedInitPlayers.Add(player.userID);
		}
		
		private void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
		{
			if (plant == null || item == null || player == null) return;
			
			if (plant.OwnerID == player.userID)
			{
				if ( (item.info.shortname == "cloth" && (plant.State.ToString() == "Ripe" || plant.State.ToString() == "Fruiting")) || 
				     ((item.info.shortname == "pumpkin" || item.info.shortname == "corn" || item.info.shortname == "potato") && (plant.State.ToString() == "Ripe" || plant.State.ToString() == "Fruiting")) )
				{
					int scores = 0;
					if (/*item.info.shortname == "cloth" &&*/ plant.GetPlanter() != null)
						scores+=2;
					
					scores++;
					var type = item.info.shortname == "cloth" ? "hemp" : item.info.shortname;
					API_AddPlayerScores(player.userID, type, scores);										
				}								
			}
		}
		
		private void DoAction(Item item, BasePlayer player, bool giveSeed)
		{
			ItemModConsume mod = null;
			
			var itemModArray = item.info.itemMods;
			for (int i = 0; i < (int)itemModArray.Length; i++)
			{
				if (itemModArray[i] is ItemModMenuOption)
				{
					mod = (itemModArray[i] as ItemModMenuOption).actionTarget as ItemModConsume;
					break;
				}
			}
			
			if (mod != null)
			{
				if (item.amount < 1)				
					return;
				
				GameObjectRef consumeEffect = mod.GetConsumeEffect();
				if (consumeEffect.isValid)
				{
					Vector3 vector3 = (player.IsDucked() ? new Vector3(0f, 1f, 0f) : new Vector3(0f, 2f, 0f));
					Effect.server.Run(consumeEffect.resourcePath, player, 0, vector3, Vector3.zero, null, false);
				}
				player.metabolism.MarkConsumption();
				ItemModConsumable consumable = mod.GetConsumable();
				
				float single = (float)Mathf.Max(consumable.amountToConsume, 1);
				float single1 = (float)Mathf.Min((float)item.amount, single);
				float single2 = single1 / single;
				float single3 = item.conditionNormalized;
				if (consumable.conditionFractionToLose > 0f)
				{
					single3 = consumable.conditionFractionToLose;
				}
				foreach (ItemModConsumable.ConsumableEffect effect in consumable.effects)
				{
					if (Mathf.Clamp01(player.healthFraction + player.metabolism.pending_health.Fraction()) > effect.onlyIfHealthLessThan)
					{
						continue;
					}
					if (effect.type != MetabolismAttribute.Type.Health)
					{
						player.metabolism.ApplyChange(effect.type, effect.amount * single2 * single3, effect.time * single2 * single3);
					}
					else if (effect.amount >= 0f)
					{
						BasePlayer basePlayer = player;
						basePlayer.health = basePlayer.health + effect.amount * single2 * single3;
					}
					else
					{
						player.OnAttacked(new HitInfo(player, player, Rust.DamageType.Generic, -effect.amount * single2 * single3, player.transform.position + (player.transform.forward * 1f)));
					}
				}
				if (mod.product != null && giveSeed)
				{
					ItemAmountRandom[] itemAmountRandomArray = mod.product;
					for (int i = 0; i < (int)itemAmountRandomArray.Length; i++)
					{
						ItemAmountRandom itemAmountRandom = itemAmountRandomArray[i];
						int num = Mathf.RoundToInt((float)itemAmountRandom.RandomAmount() * single3);
						if (num > 0)
						{
							Item item1 = ItemManager.Create(itemAmountRandom.itemDef, num, (ulong)0);
							player.GiveItem(item1, BaseEntity.GiveItemReason.Generic);
						}
					}
				}
				if (string.IsNullOrEmpty(mod.eatGesture))
				{
					player.SignalBroadcast(BaseEntity.Signal.Gesture, mod.eatGesture, null);
				}
				if (consumable.conditionFractionToLose <= 0f)
				{
					item.UseItem((int)single1);
					return;
				}
				item.LoseCondition(consumable.conditionFractionToLose * item.maxCondition);
			}
			
		}
		
		private bool? OnItemAction(Item item, string action, BasePlayer player)
        {			
            if (item == null || player == null || !(item.info.shortname == "pumpkin" || item.info.shortname == "corn" || item.info.shortname == "potato") || !(action == "consume")) return null;
				
			if (player.metabolism.CanConsume())
				DoAction(item, player, Rnd.Next(1,3)==1);
						
			return false;
		}
		
		#endregion
		
		#region Images
		
		private static bool AreImagesLoaded() => Images.Count >= MaxImages;		
		
		private void DownloadImages() 
		{						
			ServerMgr.Instance.StartCoroutine(DownloadImage(FarmMainFon));
			ServerMgr.Instance.StartCoroutine(DownloadImage(FarmGreyFon));
			ServerMgr.Instance.StartCoroutine(DownloadImage(FarmExit));
			ServerMgr.Instance.StartCoroutine(DownloadImage(FarmBack));
			ServerMgr.Instance.StartCoroutine(DownloadImage(FarmExchange));
			
			foreach (var pair in FarmArts)
				ServerMgr.Instance.StartCoroutine(DownloadImage(pair.Value));
		}
		
		private IEnumerator DownloadImage(string url)
        {
            using (var www = new WWW(url))
            {
                yield return www;                
                if (www.error != null)                
                    PrintWarning($"Ошибка добавления изображения. Неверная ссылка на изображение:\n {url}");
                else
                {
                    var tex = www.texture;
                    byte[] bytes = tex.EncodeToPNG();															
                    var image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
					
					if (!Images.ContainsKey(url))
						Images.Add(url, image);
					else
						Images[url] = image;
					
                    UnityEngine.Object.DestroyImmediate(tex);
                    yield break;
                }
            }
        }
		
		#endregion
		
		#region Commands
		
		[ChatCommand("farmer_add_scores")]
        private void CommandFarmScores(BasePlayer player, string command, string[] args)
        {
            if (player == null || !player.IsAdmin) return;
			API_AddPlayerScores(player.userID, "pumpkin", 500);
		}
		
		[ChatCommand("fermer")]
        private void CommandFarm1(BasePlayer player, string command, string[] args) => CommandFarm(player, command, args);
		
		[ChatCommand("farmer")]
        private void CommandFarm(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;			
			
			if (!AreImagesLoaded())
			{
				SendReply(player, "Плагин еще инициализируется, подождите пару секунд.\nЕсли это сообщение не проходит, сообщите администратору.");
				return;
			}
			
			//API_AddPlayerScores(76561198241364488, "pumpkin", 66);
			
			if (NeedInitPlayers.Contains(player.userID))
			{
				DoInitImages(player);
				timer.Once(0.1f, ()=> DrawUI(player));
			}						
			else
				DrawUI(player);
        }
		
		[ChatCommand("fermer_top")]
        private void CommandFarmTop1(BasePlayer player, string command, string[] args) => CommandFarmTop2(player, command, args);
		
		[ChatCommand("farmer_top")]
        private void CommandFarmTop2(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;			
			
			int num = 0;
			var result = "ТОП Фермеров за текущий вайп:\n";
			foreach (var pair in PlayerData.OrderByDescending(x=> x.Value.PotatoScores+x.Value.PumpkinScores+x.Value.CornScores+x.Value.HempScores+x.Value.EggScores+x.Value.OysterScores))			
			{
				var total = pair.Value.PotatoScores + pair.Value.PumpkinScores + pair.Value.CornScores + pair.Value.HempScores + pair.Value.EggScores + pair.Value.OysterScores;
				result += $"{++num}. "+"<color=#8ABB50>" + GetPlayerName(pair.Key) + "</color>" + $": {total} всего, {pair.Value.PumpkinScores} ткв, {pair.Value.CornScores} ккз, {pair.Value.PotatoScores} крш, {pair.Value.HempScores} кон, {pair.Value.EggScores} яйц, {pair.Value.OysterScores} уст\n";
				if (num >= 10) break;
			}
						
			SendReply(player, "<size=15>"+result+"</size>");
        }
		
		#endregion
		
		#region Init Images
		
		private void DoInitImages(BasePlayer player, bool isFull = false)
		{
			if (player == null) return;						
			
			var container = new CuiElementContainer();
			UI_MainPanel(ref container, "0 0 0 0", "1.1 1.1", "1.2 1.2", "Overlay", false, false, InitPanel);
			
			if (!isFull)
			{				
				UI_Image(ref container, Images[FarmExit], "0 0", "1 1", 0f, 0f, InitPanel);
				UI_Image(ref container, Images[FarmBack], "0 0", "1 1", 0f, 0f, InitPanel);
				UI_Image(ref container, Images[FarmExchange], "0 0", "1 1", 0f, 0f, InitPanel);
				
				UI_Image(ref container, Images[FarmMainFon], "0 0", "1 1", 0f, 0f, InitPanel);
				UI_Image(ref container, Images[FarmGreyFon], "0 0", "1 1", 0f, 0f, InitPanel);
			}
			else
			{
				foreach (var img in FarmArts.Values)
					UI_Image(ref container, Images[img], "0 0", "1 1", 0f, 0f, InitPanel);
			}
			
			CuiHelper.DestroyUi(player, InitPanel);
			CuiHelper.AddUi(player, container);	
		}
		
		#endregion
		
		#region GUI
		
		private static void SendЕarnedScores(BasePlayer player, int score)
		{
			if (player == null) return;			
			player.Command("note.inv", new object[] { -1002156085, score, EarnedScoreText });
		}
		
		private static void SendЕarnedScores(ulong userID, int score)
		{
			var player = BasePlayer.FindByID(userID);
			if (player == null) return;
			player.Command("note.inv", new object[] { -1002156085, score, EarnedScoreText });
		}
		
		private void DrawUI(BasePlayer player)
		{
			if (player == null) return;
			
			CuiHelper.DestroyUi(player, MainPanel);
			CuiHelper.DestroyUi(player, InitPanel);
			
			var container = new CuiElementContainer();
			UI_MainPanel(ref container, "0 0 0 0", "0 0", "1 1", "Menu_UI2", true, false);
			
			UI_Button(ref container, "0 0 0 0", "", 14, "0 0", "1 1", "farmer_9182743.exit", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, MainPanel, "farmer.globalexit.button");
			
			UI_Panel(ref container, "0 0 0 0", "0.5 0.5", "0.5 0.5", MainPanel, "farmer.mainfon.panel", MainPanelSizeMin, MainPanelSizeMax);
			
			UI_Image(ref container, Images[FarmMainFon], "0 0", "1 1", 0.5f, 0.5f, "farmer.mainfon.panel", "farmer.mainfon.image");
			
			// кнопки
			UI_Button(ref container, "0 0 0 0", "", 14, "0.1417222 0.4110795", "0.243 0.6348011", "farmer_9182743.art 1", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art1.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.2537222 0.4110795", "0.407 0.6348011", "farmer_9182743.art 2", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art2.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.4167221 0.4110798", "0.5690001 0.6348007", "farmer_9182743.art 3", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art3.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.5767221 0.41108", "0.7200001 0.6348003", "farmer_9182743.art 4", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art4.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.728722 0.4110799", "0.8639999 0.6348006", "farmer_9182743.art 5", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art5.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.1367223 0.1541199", "0.278 0.3778401", "farmer_9182743.art 6", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art6.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.2867224 0.1553983", "0.428 0.3791185", "farmer_9182743.art 7", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art7.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.4387224 0.1566766", "0.564 0.380397", "farmer_9182743.art 8", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art8.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.5717223 0.1579551", "0.7129999 0.3816754", "farmer_9182743.art 9", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art9.button");
			UI_Button(ref container, "0 0 0 0", "", 14, "0.7217223 0.1592336", "0.8629999 0.3829538", "farmer_9182743.art 10", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.mainfon.panel", "farmer.art10.button");
			
			// очки
			PreparePlayerScores(player, ref container, false);
			
			// выход			
			//UI_Panel(ref container, "0.5 0.5 0.5 0.9", "0.7207223 0.051", "0.874 0.12", "farmer.mainfon.panel");
			UI_Image(ref container, Images[FarmExit], "0.7207223 0.051", "0.874 0.12", 0.5f, 0.5f, "farmer.mainfon.panel", "farmer.exit.image");
			UI_Button(ref container, "0 0 0 0", "", 14, "0 0", "1 1", "farmer_9182743.exit", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.exit.image", null, 0.5f, 0.5f);
			
			CuiHelper.AddUi(player, container);
			
			if (NeedInitPlayers.Contains(player.userID))
			{
				DoInitImages(player, true);
				NeedInitPlayers.Remove(player.userID);				
			}
		}
		
		private void PreparePlayerScores(BasePlayer player, ref CuiElementContainer container, bool isSecond)
		{
			if (!isSecond)
			{
				CuiHelper.DestroyUi(player, "farmer.scores.label");
				var scores = GetPlayerScores(player.userID, "total.real");			
				UI_FLabel(ref container, scores.ToString(), "0 0 0 0", 28, "0.1307223 0.042", "0.228 0.13", 0f, 0f, TextAnchor.MiddleCenter, "robotocondensed-bold.ttf", "farmer.mainfon.panel", "farmer.scores.label");
			}
			else
			{
				CuiHelper.DestroyUi(player, "farmer.scores.label2");
				var scores = GetPlayerScores(player.userID, "total.real");			
				UI_FLabel(ref container, scores.ToString(), "0 0 0 0", 28, "0.1307223 0.042", "0.228 0.13", 0f, 0f, TextAnchor.MiddleCenter, "robotocondensed-bold.ttf", "farmer.greyfon.panel", "farmer.scores.label2");
			}
		}
		
		private void DrawArt(BasePlayer player, int num)
		{
			if (player == null) return;
			
			var container = new CuiElementContainer();
			
			UI_Panel(ref container, "0 0 0 0", "0.5 0.5", "0.5 0.5", MainPanel, "farmer.greyfon.panel", MainPanelSizeMin, MainPanelSizeMax);
			
			UI_Image(ref container, Images[FarmGreyFon], "0 0", "1 1", 0f, 0f, "farmer.greyfon.panel", "farmer.greyfon.image");
			
			UI_Image(ref container, Images[FarmArts[num]], "0.215 0.2717329", "0.788 0.6757102", 0.2f, 0.2f, "farmer.greyfon.panel", "farmer.art.image");
			
			// обменять			
			var colorExchange = GetCaseCost(num) <= GetPlayerScores(player.userID, "total.real") ? "0.5 0.7 0.2 0.5" : "0.9 0.9 0.9 0.5";
			
			UI_Image(ref container, Images[FarmExchange], "0.39 0.2180398", "0.61 0.2845171", 0f, 0f, "farmer.greyfon.panel", "farmer.exchange.image");
			UI_Button(ref container, colorExchange, "", 14, "0 0", "1 1", $"farmer_9182743.exchange {num}", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.exchange.image", null);
			
			// очки
			PreparePlayerScores(player, ref container, true);
			
			// назад
			//UI_Panel(ref container, "0.5 0.5 0.5 0.9", "0.7207223 0.051", "0.874 0.12", "farmer.greyfon.panel");
			UI_Image(ref container, Images[FarmBack], "0.7207223 0.051", "0.874 0.12", 0f, 0f, "farmer.greyfon.panel", "farmer.back.image");
			UI_Button(ref container, "0 0 0 0", "", 14, "0 0", "1 1", "farmer_9182743.back", "robotocondensed-regular.ttf", TextAnchor.MiddleCenter, "farmer.back.image", null, 0f, 0f);
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
		
		#region GUI Commands
		
		[ConsoleCommand("farmer_9182743.exit")]
        private void cmdGUIClose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null) return;						
									
			CuiHelper.DestroyUi(player, MainPanel);
		}
		
		[ConsoleCommand("farmer_9182743.art")]
        private void cmdGUIArt(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null) return;
			
			var num = Convert.ToInt32(arg.Args[0]);
			DrawArt(player, num);
		}
		
		[ConsoleCommand("farmer_9182743.back")]
        private void cmdGUIBack(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null) return;
			
			CuiHelper.DestroyUi(player, "farmer.greyfon.panel");
		}
		
		[ConsoleCommand("farmer_9182743.exchange")]
        private void cmdGUIExchange(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null) return;
			
			var num = Convert.ToInt32(arg.Args[0]);
			var cost = GetCaseCost(num);
			if (cost <= GetPlayerScores(player.userID, "total.real"))
			{
				if (GivePlayerLoot(player, num))
				{
					UsePlayerScores(player.userID, cost);
				
					var container = new CuiElementContainer();				
					PreparePlayerScores(player, ref container, false);
					CuiHelper.AddUi(player, container);
					
					CuiHelper.DestroyUi(player, "farmer.greyfon.panel");
					
					ShowMessage(player, "Вы открыли ящик и достали оттуда случайный предмет.\nПроверьте свой инвентарь.");
				}
			}
		}
		
		#endregion
		
		#region GUI Helpers
		
		private const string MainPanel = "FarmerMainPanel";		
		private const string InitPanel = "FarmerInitPanel";
		
		private static void UI_MainPanel(ref CuiElementContainer container, string color, string aMin, string aMax, string Parent = "Menu_UI2", bool isNeedCursor = true, bool isBlur = false, string panel = MainPanel)
		{					
			container.Add(new CuiPanel
			{
				Image = { Color = color, Material = isBlur ? "assets/content/ui/uibackgroundblur.mat" : "Assets/Icons/IconMaterial.mat" },
				RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
				CursorEnabled = isNeedCursor
			}, Parent, panel);
		}
		
		private static void UI_Panel(ref CuiElementContainer container, string color, string aMin, string aMax, string panel = MainPanel, string name = null, string oMin = "0.0 0.0", string oMax = "0.0 0.0", float fadeIn = 0f, float fadeOut = 0f)
		{			
			container.Add(new CuiPanel
			{
				FadeOut = fadeOut,
				Image = { Color = color, FadeIn = fadeIn },
				RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax }
			}, panel, name);
		}
		
		private static void UI_Label(ref CuiElementContainer container, string text, int size, string aMin, string aMax, float fadeIn = 0f, float fadeOut = 0f, TextAnchor align = TextAnchor.MiddleCenter, string font = "robotocondensed-regular.ttf", string panel = MainPanel, string name = null)
		{						
			container.Add(new CuiElement
			{
				Name = !string.IsNullOrEmpty(name) ? name : CuiHelper.GetGuid(),
				Parent = panel,
				FadeOut = fadeOut,
				Components =
				{
					new CuiTextComponent { FontSize = size, Align = align, Text = text, Font = font, FadeIn = fadeIn },
					new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }					
				}
			});
		}
		
		private static void UI_FLabel(ref CuiElementContainer container, string text, string fcolor, int size, string aMin, string aMax, float fadeIn = 0f, float fadeOut = 0f, TextAnchor align = TextAnchor.MiddleCenter, string font = "robotocondensed-regular.ttf", string panel = MainPanel, string name = null)
		{						
			if (string.IsNullOrEmpty(fcolor))
				fcolor = "0.0 0.0 0.0 1.0";
			
			container.Add(new CuiElement
			{
				Name = !string.IsNullOrEmpty(name) ? name : CuiHelper.GetGuid(),
				Parent = panel,
				FadeOut = fadeOut,
				Components =
				{
					new CuiTextComponent { FontSize = size, Align = align, Text = text, Font = font, FadeIn = fadeIn },
					new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax },
					new CuiOutlineComponent { Distance = "1 1", Color = fcolor }
				}
			});
		}
				
		private static void UI_Button(ref CuiElementContainer container, string color, string text, int size, string aMin, string aMax, string command, string font = "robotocondensed-regular.ttf", TextAnchor align = TextAnchor.MiddleCenter, string panel = MainPanel, string name = null, float fadeIn = 0f, float fadeOut = 0f)
		{
			if (string.IsNullOrEmpty(color)) color = "0 0 0 0";
			
			container.Add(new CuiButton
			{
				FadeOut = fadeOut,
				Button = { Color = color, Command = command, FadeIn = fadeIn },
				RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
				Text = { Text = text, FontSize = size, Align = align, Font = font }
			}, panel, name);
		}
		
		private static void UI_Image(ref CuiElementContainer container, string png, string aMin, string aMax, float fadeIn = 0f, float fadeOut = 0f, string panel = MainPanel, string name = null, string oMin = null, string oMax = null)
		{
			container.Add(new CuiElement
			{
				Name = !string.IsNullOrEmpty(name) ? name : CuiHelper.GetGuid(),
				Parent = panel,
				FadeOut = fadeOut,
				Components =
				{
					(png.Contains("https://") || png.Contains("http://")) ? new CuiRawImageComponent { Url = png, FadeIn = fadeIn } : new CuiRawImageComponent { Png = png, FadeIn = fadeIn },
					string.IsNullOrEmpty(oMin) ? new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax } : new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax }
				}
			});
		}
		
		#endregion
		
		#region SendMessage
		
		private static Dictionary<ulong, Timer> SMPlayerTimer = new Dictionary<ulong, Timer>();
		
		private void ShowMessage(BasePlayer player, string message)
		{						
			if (player == null || string.IsNullOrEmpty(message)) return;						
		
			ClearMessages(player);			
			var container = new CuiElementContainer();
						
			UI_Panel(ref container, "0.9 0.9 0.9 0.9", "0.213 0.7792612", "0.7909999 0.8649148", "farmer.mainfon.panel", "farmer.sm.panel", "0.0 0.0", "0.0 0.0", 0.5f, 0.5f);
			UI_Label(ref container, $"<color=black>{message}</color>", 18, "0 0", "1 1", 0.5f, 0.5f, TextAnchor.MiddleCenter, "robotocondensed-bold.ttf", "farmer.sm.panel", "farmer.sm.label");
			
			CuiHelper.AddUi(player, container);	
			
			if (!SMPlayerTimer.ContainsKey(player.userID))
				SMPlayerTimer.Add(player.userID, null);
			
			SMPlayerTimer[player.userID] = timer.Once(4f, ()=> 
			{
				CuiHelper.DestroyUi(player, "farmer.sm.label");
				CuiHelper.DestroyUi(player, "farmer.sm.panel");
			});
		}
		
		private static void ClearMessages(BasePlayer player)
		{
			if (player == null) return;
			
			if (SMPlayerTimer.ContainsKey(player.userID) && SMPlayerTimer[player.userID] != null)
			{	
				SMPlayerTimer[player.userID].Destroy();
				SMPlayerTimer[player.userID] = null;
			}
			
			CuiHelper.DestroyUi(player, "farmer.sm.panel");
		}
		
		#endregion
		
		#region Helpers
		
		private string GetPlayerName(ulong userID)
		{
			var data = permission.GetUserData(userID.ToString());															
			return data.LastSeenNickname;
		}
		
		private static int GetPlayerScores(ulong userID, string type)
		{
			if (!PlayerData.ContainsKey(userID))
				return 0;
			
			switch (type)
			{
				case "total.real" : return PlayerData[userID].PotatoScores + PlayerData[userID].PumpkinScores + PlayerData[userID].CornScores + PlayerData[userID].HempScores + PlayerData[userID].EggScores + PlayerData[userID].OysterScores - PlayerData[userID].UsedScores;
				case "total.all" : return PlayerData[userID].PotatoScores + PlayerData[userID].PumpkinScores + PlayerData[userID].CornScores + PlayerData[userID].HempScores + PlayerData[userID].EggScores + PlayerData[userID].OysterScores;
				case "used" : return PlayerData[userID].UsedScores;
				case "pumpkin" : return PlayerData[userID].PumpkinScores;
				case "corn" : return PlayerData[userID].CornScores;
				case "hemp" : return PlayerData[userID].HempScores;
				case "egg" : return PlayerData[userID].EggScores;
				case "oyster" : return PlayerData[userID].OysterScores;
				case "potato" : return PlayerData[userID].PotatoScores;
			}
			
			return 0;
		}
		
		private static void UsePlayerScores(ulong userID, int amount)
		{
			if (!PlayerData.ContainsKey(userID))
				PlayerData.Add(userID, new PData());
				
			PlayerData[userID].UsedScores += amount;
		}
		
		private void API_AddPlayerScores(ulong userID, string type, int amount)
		{
			if (!PlayerData.ContainsKey(userID))
				PlayerData.Add(userID, new PData());
			
			switch (type)
			{
				case "pumpkin" : PlayerData[userID].PumpkinScores += amount; break;
				case "corn" : PlayerData[userID].CornScores += amount; break;
				case "hemp" : PlayerData[userID].HempScores += amount; break;
				case "egg" : PlayerData[userID].EggScores += amount; break;
				case "oyster" : PlayerData[userID].OysterScores += amount; break;
				case "potato" : PlayerData[userID].PotatoScores += amount; break;
			}
			
			SendЕarnedScores(userID, amount);
		}
		
		private static int GetCaseCost(int num)
		{
			if (!configData.Costs.ContainsKey(num)) 
				return int.MaxValue;
			
			return configData.Costs[num];
		}
		
		#endregion
		
		#region Give
		
		private bool GivePlayerLoot(BasePlayer player, int num)
		{
			if (player == null || !configData.Items.ContainsKey(num))
				return false;
			
			var listItems = configData.Items[num].Where(x=> x.Enabled).ToList();
			
			if (listItems.Count == 0)
				return false;
			
			int rndNum = 0;
			FarmItem item = null;
			try
			{
				var max = Rnd.Next(1,21);
				for (int ii = 0; ii <= max; ii++)
					rndNum = (int)Math.Truncate(Rnd.Next(1, listItems.Count*10)/10f);
				
				item = listItems[rndNum];
				
				var newItem = ItemManager.CreateByName(item.ItemName, Rnd.Next(item.MinAmount, item.MaxAmount + 1));
				player.GiveItem(newItem, BaseEntity.GiveItemReason.ResourceHarvested);
			}
			catch
			{
				PrintWarning($"Ошибка создания предмета, имя {item.ItemName}, позиция в списке {rndNum}");
				return false;
			}
			
			return true;
		}
		
		#endregion
		
		#region Config
		
        private static ConfigData configData;
		
        private class ConfigData
        {            
			[JsonProperty(PropertyName = "Список кейсов и их стоимость")]
			public Dictionary<int, int> Costs;			
			[JsonProperty(PropertyName = "Список кейсов и их лут")]
			public Dictionary<int, List<FarmItem>> Items;
        }
		
		private class FarmItem
		{
			[JsonProperty(PropertyName = "Номер предмета (для ссылок)")]
			public int Num;
			[JsonProperty(PropertyName = "Включен")]
			public bool Enabled;
			[JsonProperty(PropertyName = "Игровое название предмета")]
			public string ItemName;
			[JsonProperty(PropertyName = "Количество (минимум)")]
			public int MinAmount;
			[JsonProperty(PropertyName = "Количество (максимум)")]
			public int MaxAmount;
			[JsonProperty(PropertyName = "Список предметов, которые будут падать совместно с этим")]
			public List<int> AddItems;
			
			public FarmItem(int Num, bool Enabled, string ItemName, int MinAmount, int MaxAmount, List<int> AddItems)
			{
				this.Num = Num;
				this.Enabled = Enabled;
				this.ItemName = ItemName;
				this.MinAmount = MinAmount;
				this.MaxAmount = MaxAmount;
				this.AddItems = AddItems;
			}
		}
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                Costs = new Dictionary<int, int>()
				{
					{ 1, 25 },
					{ 2, 50 },
					{ 3, 75 },
					{ 4, 80 },
					{ 5, 100 },
					{ 6, 200 },
					{ 7, 250 },
					{ 8, 300 },
					{ 9, 325 },
					{ 10, 350 }
				},
				Items = new Dictionary<int, List<FarmItem>>()
				{
					{ 1, new List<FarmItem>() 
						{ 
							new FarmItem(1, true, "pants", 1, 1, null),
							new FarmItem(2, true, "hoodie", 1, 1, null),
							new FarmItem(3, true, "tactical.gloves", 1, 1, null),
							new FarmItem(4, true, "shoes.boots", 1, 1, null),
							new FarmItem(5, true, "jacket.snow", 1, 1, null),
							new FarmItem(6, true, "hazmatsuit", 1, 1, null)
						} 
					},
					{ 2, new List<FarmItem>() 
						{ 
							new FarmItem(7, true, "hatchet", 1, 1, null),
							new FarmItem(8, true, "pickaxe", 1, 1, null),
							new FarmItem(9, true, "icepick.salvaged", 1, 1, null),
							new FarmItem(10, true, "axe.salvaged", 1, 1, null),
							new FarmItem(11, true, "jackhammer", 1, 1, null),
							new FarmItem(12, true, "chainsaw", 1, 1, null)
						} 
					},
					{ 3, new List<FarmItem>() 
						{ 
							new FarmItem(13, true, "bed", 1, 1, null),
							new FarmItem(14, true, "furnace.large", 1, 1, null),
							new FarmItem(15, true, "small.oil.refinery", 1, 1, null),
							new FarmItem(16, true, "vending.machine", 1, 1, null),
							new FarmItem(17, true, "shelves", 1, 1, null),
							new FarmItem(18, true, "locker", 1, 1, null)
						} 
					},
					{ 4, new List<FarmItem>() 
						{ 
							new FarmItem(19, true, "coffeecan.helmet", 1, 1, null),
							new FarmItem(20, true, "roadsign.jacket", 1, 1, null),
							new FarmItem(21, true, "roadsign.kilt", 1, 1, null),
							new FarmItem(22, true, "roadsign.gloves", 1, 1, null)							
						} 
					},
					{ 5, new List<FarmItem>() 
						{ 
							new FarmItem(23, true, "wall.frame.garagedoor", 1, 1, null),
							new FarmItem(24, true, "floor.ladder.hatch", 1, 1, null),
							new FarmItem(25, true, "ladder.wooden.wall", 1, 1, null),
							new FarmItem(26, true, "wall.external.high.stone", 1, 1, null),
							new FarmItem(27, true, "gates.external.high.stone", 1, 1, null),
							new FarmItem(28, true, "gates.external.high.wood", 1, 1, null),
							new FarmItem(29, true, "wall.external.high", 1, 1, null),
							new FarmItem(30, true, "wall.window.glass.reinforced", 1, 1, null),
							new FarmItem(31, true, "barricade.metal", 1, 1, null),
							new FarmItem(32, true, "wall.frame.shopfront.metal", 1, 1, null)							
						} 
					},
					{ 6, new List<FarmItem>() 
						{ 
							new FarmItem(33, true, "stones", 7000, 10000, null),
							new FarmItem(34, true, "wood", 9000, 12000, null),
							new FarmItem(35, true, "cloth", 500, 800, null),
							new FarmItem(36, true, "leather", 400, 700, null),
							new FarmItem(37, true, "lowgradefuel", 400, 700, null),
							new FarmItem(38, true, "metal.fragments", 3000, 5000, null),
							new FarmItem(39, true, "metal.refined", 80, 120, null),
							new FarmItem(40, true, "crude.oil", 200, 300, null),
							new FarmItem(41, true, "sulfur", 2000, 3000, null),
							new FarmItem(42, true, "gunpowder", 1500, 2000, null),
							new FarmItem(43, true, "charcoal", 9000, 12000, null)
						}
					},
					{ 7, new List<FarmItem>() 
						{ 
							new FarmItem(44, true, "techparts", 7, 10, null),
							new FarmItem(45, true, "smgbody", 5, 7, null),
							new FarmItem(46, true, "riflebody", 5, 7, null),
							new FarmItem(47, true, "semibody", 7, 10, null),
							new FarmItem(48, true, "rope", 40, 50, null),
							new FarmItem(49, true, "sewingkit", 20, 30, null),
							new FarmItem(50, true, "sheetmetal", 15, 20, null),
							new FarmItem(51, true, "roadsigns", 15, 25, null),
							new FarmItem(52, true, "metalspring", 10, 15, null),
							new FarmItem(53, true, "metalpipe", 15, 25, null),
							new FarmItem(54, true, "gears", 15, 20, null)
						}
					},
					{ 8, new List<FarmItem>() 
						{ 
							new FarmItem(55, true, "grenade.beancan", 1, 1, null),
							new FarmItem(56, true, "explosive.satchel", 1, 1, null),
							new FarmItem(57, true, "explosives", 1, 1, null),
							new FarmItem(58, true, "flamethrower", 1, 1, null),
							new FarmItem(59, true, "ammo.rifle.explosive", 1, 1, null)
						}
					},
					{ 9, new List<FarmItem>() 
						{ 
							new FarmItem(60, true, "pistol.semiauto", 1, 1, null),
							new FarmItem(61, true, "shotgun.pump", 1, 1, null),
							new FarmItem(62, true, "smg.thompson", 1, 1, null),
							new FarmItem(63, true, "smg.2", 1, 1, null),							
							new FarmItem(65, true, "rifle.semiauto", 1, 1, null)
						}
					},
					{ 10, new List<FarmItem>() 
						{ 
							new FarmItem(66, true, "pistol.m92", 1, 1, null),
							new FarmItem(67, true, "rifle.lr300", 1, 1, null),
							new FarmItem(68, true, "rifle.l96", 1, 1, null),
							new FarmItem(69, true, "smg.mp5", 1, 1, null),
							new FarmItem(70, true, "shotgun.spas12", 1, 1, null),
							new FarmItem(71, true, "rifle.m39", 1, 1, null)
						}
					}					
				}
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion
		
		#region Data
		
		private static Dictionary<ulong, PData> PlayerData = new Dictionary<ulong, PData>();
		
		private class PData
		{
			public int PumpkinScores;	// pumpkin
			public int CornScores;		// corn
			public int HempScores;		// hemp
			public int PotatoScores;	// potato
			public int EggScores;		// egg
			public int OysterScores;	// oyster
			public int UsedScores;	
		}
		
		private void LoadData() => PlayerData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PData>>("FarmerData");					
		
		private static void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("FarmerData", PlayerData);		
		
		#endregion
		
	}
	
}	