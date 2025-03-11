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
    [Info("XRPG", "SkuliDropek.", "1.1.101")]
    class XRPG : RustPlugin
    {
        #region Configuration

        private RPGConfig config;

        private class RPGConfig
        {
            internal class PanelSetting
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;                
			    [JsonProperty("Отображение прогресса: TRUE - от минимальных до максимальных рейтов | FALSE - от 0 до максимальных рейтов ")] public bool Progress;
            }

            internal class SSetting
            {
                [JsonProperty("Включить замедление прокачки | Чем больше рейты, тем медленнее прокачка")] public bool Slow;                
				[JsonProperty("Включить рпг панель")] public bool Panel;
                [JsonProperty("Включить рпг сообщения")] public bool Messages;
                [JsonProperty("Стартовый умножитель прокачки рейтов")] public float Boost;
                [JsonProperty("Размер текста")] public int TextSize;
				[JsonProperty("Список игнорируемых инструментов")] public List<string> BlockItem;
            }

            internal class Permissions
            {
                [JsonProperty("Максимальные рейты лесоруба")] public float WoodRate;
                [JsonProperty("Максимальные рейты рудокопа")] public float OreRate;
                [JsonProperty("Максимальные рейты охотника")] public float AnimalRate;
                [JsonProperty("Множитель прокачки рейтов")] public float Boost;
            }

            internal class WoodSetting
            {
                [JsonProperty("Максимальные рейты лесоруба")] public float RateMax;
                [JsonProperty("Стартовые рейты лесоруба")] public float RateStart;
                [JsonProperty("Включить прокачку рейтов лесоруба добывая ресурсы")] public bool Bonus;
                [JsonProperty("Включить прокачку рейтов лесоруба подбирая ресурсы")] public bool Pickup;

                [JsonProperty("Ресурсы за добычу/подбор которых начислять рейты лесоруба | Ресурсы на которые будут действовать рейты лесоруба")] public Dictionary<string, float> Item = new Dictionary<string, float>();
            }

            internal class OreSetting
            {
                [JsonProperty("Максимальные рейты рудокопа")] public float RateMax;
                [JsonProperty("Стартовые рейты рудокопа")] public float RateStart;
                [JsonProperty("Включить прокачку рейтов рудокопа добывая ресурсы")] public bool Bonus;
                [JsonProperty("Включить прокачку рейтов рудокопа подбирая ресурсы")] public bool Pickup;

                [JsonProperty("Ресурсы за добычу/подбор которых начислять рейты рудокопа | Ресурсы на которые будут действовать рейты рудокопа")] public Dictionary<string, float> Item = new Dictionary<string, float>();
            }
 
            internal class AnimalSetting
            {
                [JsonProperty("Максимальные рейты охотника")] public float RateMax;
                [JsonProperty("Стартовые рейты охотника")] public float RateStart;
                [JsonProperty("Включить прокачку рейтов охотника добывая ресурсы")] public bool Bonus;
                [JsonProperty("Включить прокачку рейтов охотника подбирая ресурсы")] public bool Pickup;
                [JsonProperty("Включить прокачку рейтов охотника убивая животных")] public bool Kill;

                [JsonProperty("Ресурсы за добычу/подбор которых начислять рейты охотника | Ресурсы на которые будут действовать рейты охотника")] public Dictionary<string, float> Item = new Dictionary<string, float>();
                [JsonProperty("Животные за убийство которых начислять рейты охотника")] public Dictionary<string, float> Animal = new Dictionary<string, float>();
            }

            [JsonProperty("Общее")]
            public SSetting Setting = new SSetting();
            [JsonProperty("Расположение мини-панели")]
            public PanelSetting Panel = new PanelSetting();
            [JsonProperty("Настройка пермишенов")]
            public Dictionary<string, Permissions> Permisssion = new Dictionary<string, Permissions>();
            [JsonProperty("Настройка лесоруба")]
            public WoodSetting Wood = new WoodSetting();
            [JsonProperty("Настройка рудокопа")]
            public OreSetting Ore = new OreSetting();
            [JsonProperty("Настройка охотника")]
            public AnimalSetting Animal = new AnimalSetting();

            public static RPGConfig GetNewConfiguration()
            {
                return new RPGConfig
                {
                    Panel = new PanelSetting
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-402 16",
                        OffsetMax = "-210 98",
						Progress = true
                    },
                    Setting = new SSetting
                    {
						Slow = false,
                        Panel = true,
                        Messages = true,
                        Boost = 1.0f,
						TextSize = 13,
						BlockItem = new List<string>
						{
							"jackhammer.entity",
							"chainsaw.entity"
						}
                    },
                    Permisssion = new Dictionary<string, Permissions>
                    {
                        ["xrpg.default"] = new Permissions
                        {
                            WoodRate = 12.5f,
                            OreRate = 12.5f,
                            AnimalRate = 12.5f,
                            Boost = 1.25f
                        }
                    },
                    Wood = new WoodSetting
                    {
                        RateMax = 10.0f,
                        RateStart = 2.5f,
                        Bonus = true,
                        Pickup = true,
                        Item = new Dictionary<string, float>
                        {
                            ["wood"] = 0.05f,
                            ["charcoal"] = 0.05f
                        }
                    },
                    Ore = new OreSetting
                    {
                        RateMax = 10.0f,
                        RateStart = 1.75f,
                        Bonus = true,
                        Pickup = true,
                        Item = new Dictionary<string, float>
                        { 
                            ["stones"] = 0.05f,
                            ["metal.ore"] = 0.05f,
                            ["metal.fragments"] = 0.05f,
                            ["sulfur.ore"] = 0.05f,
                            ["sulfur"] = 0.05f
                        }
                    },
                    Animal = new AnimalSetting
                    {
                        RateMax = 10.0f,
                        RateStart = 1.5f,
                        Bonus = true,
                        Pickup = true,
                        Kill = true,
                        Item = new Dictionary<string, float>
                        {
                            ["cloth"] = 0.05f,
                            ["leather"] = 0.05f,
                            ["bone.fragments"] = 0.05f,
                            ["fat.animal"] = 0.05f
                        },
                        Animal = new Dictionary<string, float>
                        {
                            ["boar"] = 0.75f,
                            ["bear"] = 0.75f,
                            ["stag"] = 0.75f,
                            ["wolf"] = 0.75f,
                            ["testridablehorse"] = 0.75f,
                            ["chicken"] = 0.75f
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<RPGConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = RPGConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data

        private class RPGData
        {
            [JsonProperty("Лесоруб")]
            public float Wood;
            [JsonProperty("Рудокоп")]
            public float Ore;
            [JsonProperty("Охотник")]
            public float Animal;
            [JsonProperty("Максимальные рейты лесоруба")]
            public float WoodRate;
            [JsonProperty("Максимальные рейты рудокопа")]
            public float OreRate;
            [JsonProperty("Максимальные рейты охотника")]
            public float AnimalRate;
            [JsonProperty("Множитель прокачки рейтов")]
            public float Boost;            
			[JsonProperty("Активность UI")]
            public bool ActiveUI = true;
        }

        private Dictionary<ulong, RPGData> StoredData = new Dictionary<ulong, RPGData>();

        #endregion

        #region Commands

        [ChatCommand("rank")]
        void cmdTOP(BasePlayer player) => TOP(player);        
		
		[ConsoleCommand("ui")]
        void ccmdUI(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			switch(args.Args[0])
			{
				case "show":
				{
					CuiHelper.DestroyUi(player, ".Show");
					StoredData[player.userID].ActiveUI = true;
					
					RPGUI(player);
					HideUI(player);
					break;
				}				
				case "hide":
				{
					CuiHelper.DestroyUi(player, ".RPG");
					CuiHelper.DestroyUi(player, ".Hide");
					StoredData[player.userID].ActiveUI = false;
					
					ShowUI(player);
					break;
				}
			}
		}

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n" +
            "     Author - SkuliDropek\n" +
            "     VK - vk.com/idannopol\n" +
            "    Discord - Skuli Dropek#4816 - KINGSkuliDropek#4837\n" +
            "     Config - v.1436\n" +
            "-----------------------------");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("XRPG"))
                StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, RPGData>>("XRPG");

            timer.Every(120, () => { Interface.Oxide.DataFileSystem.WriteObject("XRPG", StoredData); });

            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);

            foreach (var perm in config.Permisssion)
                permission.RegisterPermission(perm.Key, this);

            permission.RegisterPermission("xrpg.use", this);
            permission.RegisterPermission("xrpg.top", this);
			
			InitializeLang();
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
                CuiHelper.DestroyUi(player, ".RPG");
				CuiHelper.DestroyUi(player, ".Hide");
				CuiHelper.DestroyUi(player, ".Show");
			}

            Interface.Oxide.DataFileSystem.WriteObject("XRPG", StoredData);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (!StoredData.ContainsKey(player.userID))
            {
                StoredData.Add(player.userID, new RPGData());

                StoredData[player.userID].Wood = config.Wood.RateStart;
                StoredData[player.userID].Ore = config.Ore.RateStart;
                StoredData[player.userID].Animal = config.Animal.RateStart;

                StoredData[player.userID].WoodRate = config.Wood.RateMax;
                StoredData[player.userID].OreRate = config.Ore.RateMax;
                StoredData[player.userID].AnimalRate = config.Animal.RateMax;
            }

            if (config.Setting.Panel && permission.UserHasPermission(player.UserIDString, "xrpg.use"))
			{
				if (StoredData[player.userID].ActiveUI)
				{
					RPGUI(player);
				    HideUI(player);
				}
			    else
				    ShowUI(player);
			}
			if (permission.UserHasPermission(player.UserIDString, "xrpg.use"))
				MaxRate(player);
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || item == null || player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, "xrpg.use")) return;

            if (config.Wood.Bonus)
                if (config.Wood.Item.ContainsKey(item.info.shortname))
				{
                        if (!(StoredData[player.userID].Wood >= StoredData[player.userID].WoodRate))
							if(!config.Setting.BlockItem.Contains(player.GetHeldEntity().ShortPrefabName))
							{
								var itemw = config.Wood.Item[item.info.shortname];
								float wrate = 0; 
							
								if (config.Setting.Slow)
									wrate = (itemw - (itemw / StoredData[player.userID].WoodRate * (int)StoredData[player.userID].Wood)) * StoredData[player.userID].Boost;
								else
									wrate = itemw * StoredData[player.userID].Boost;

								StoredData[player.userID].Wood += wrate;

								if (config.Setting.Panel)
									if (StoredData[player.userID].ActiveUI)
									{
										WoodUI(player);
										if (config.Setting.Messages)
											UPMessageWood(player, $"+ {Math.Round(wrate, 5)}");
									}
							}
						
					item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Wood, 1);
				}
            if (config.Ore.Bonus)
                if (config.Ore.Item.ContainsKey(item.info.shortname))
				{
                        if (!(StoredData[player.userID].Ore >= StoredData[player.userID].OreRate))
							if(!config.Setting.BlockItem.Contains(player.GetHeldEntity().ShortPrefabName))
							{	
								var itemo = config.Ore.Item[item.info.shortname];
								float orate = 0;
							
								if (config.Setting.Slow)
									orate = (itemo - (itemo / StoredData[player.userID].OreRate * (int)StoredData[player.userID].Ore)) * StoredData[player.userID].Boost;
								else
									orate = itemo * StoredData[player.userID].Boost;

								StoredData[player.userID].Ore += orate;

								if (config.Setting.Panel)
									if (StoredData[player.userID].ActiveUI)
									{
										OreUI(player);
										if (config.Setting.Messages)
											UPMessageOre(player, $"+ {Math.Round(orate, 5)}");
									}
							}
						
					item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Ore, 1);
				}
            if (config.Animal.Bonus)
                if (config.Animal.Item.ContainsKey(item.info.shortname))
				{
                        if (!(StoredData[player.userID].Animal >= StoredData[player.userID].AnimalRate))
							if(!config.Setting.BlockItem.Contains(player.GetHeldEntity().ShortPrefabName))
							{
								var itema = config.Animal.Item[item.info.shortname];
								float arate = 0;
							
								if (config.Setting.Slow)
									arate = (itema - (itema / StoredData[player.userID].AnimalRate * (int)StoredData[player.userID].Animal)) * StoredData[player.userID].Boost;
								else
									arate = itema * StoredData[player.userID].Boost;

								StoredData[player.userID].Animal += arate;

								if (config.Setting.Panel)
									if (StoredData[player.userID].ActiveUI)
									{
										AnimalUI(player);
										if (config.Setting.Messages)
											UPMessageAnimal(player, $"+ {Math.Round(arate, 5)}");
									}
							}
						
					item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Animal, 1);
				}
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();

            if (dispenser == null || item == null || player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, "xrpg.use")) return;

            if (config.Wood.Item.ContainsKey(item.info.shortname))
                item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Wood, 1);
            if (config.Ore.Item.ContainsKey(item.info.shortname))
                item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Ore, 1);
            if (config.Animal.Item.ContainsKey(item.info.shortname))
                item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Animal, 1);
        }

        void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (entity == null || item == null || player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, "xrpg.use")) return;

            if (config.Wood.Pickup)
                if (config.Wood.Item.ContainsKey(item.info.shortname))
				{
                        if (!(StoredData[player.userID].Wood >= StoredData[player.userID].WoodRate))
                        {
							var itemw = config.Wood.Item[item.info.shortname];
							float wrate = 0;
							
							if (config.Setting.Slow)
                                wrate = (itemw - (itemw / StoredData[player.userID].WoodRate * (int)StoredData[player.userID].Wood)) * StoredData[player.userID].Boost;
							else
								wrate = itemw * StoredData[player.userID].Boost;

                            StoredData[player.userID].Wood += wrate;

                            if (config.Setting.Panel)
								if (StoredData[player.userID].ActiveUI)
								{
                                    WoodUI(player);
                                    if (config.Setting.Messages)
                                        UPMessageWood(player, $"+ {Math.Round(wrate, 5)}");
								}
                        }
						
					item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Wood, 1);
				}
            if (config.Ore.Pickup)
                if (config.Ore.Item.ContainsKey(item.info.shortname))
				{
                        if (!(StoredData[player.userID].Ore >= StoredData[player.userID].OreRate))
                        {
							var itemo = config.Ore.Item[item.info.shortname];
							float orate = 0;
							
							if (config.Setting.Slow)
                                orate = (itemo - (itemo / StoredData[player.userID].OreRate * (int)StoredData[player.userID].Ore)) * StoredData[player.userID].Boost;
							else
								orate = itemo * StoredData[player.userID].Boost;

                            StoredData[player.userID].Ore += orate;

                            if (config.Setting.Panel)
								if (StoredData[player.userID].ActiveUI)
								{
                                    OreUI(player);
                                    if (config.Setting.Messages)
                                        UPMessageOre(player, $"+ {Math.Round(orate, 5)}");
								}
                        }
						
					item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Ore, 1);
				}
            if (config.Animal.Pickup)
                if (config.Animal.Item.ContainsKey(item.info.shortname))
				{
                        if (!(StoredData[player.userID].Animal >= StoredData[player.userID].AnimalRate))
                        {
							var itema = config.Animal.Item[item.info.shortname];
							float arate = 0;
							
							if (config.Setting.Slow)
                                arate = (itema - (itema / StoredData[player.userID].AnimalRate * (int)StoredData[player.userID].Animal)) * StoredData[player.userID].Boost;
							else
								arate = itema * StoredData[player.userID].Boost;

                            StoredData[player.userID].Animal += arate;

                            if (config.Setting.Panel)
								if (StoredData[player.userID].ActiveUI)
								{
                                    AnimalUI(player);
                                    if (config.Setting.Messages)
                                        UPMessageAnimal(player, $"+ {Math.Round(arate, 5)}");
								}
                        }
						
					item.amount = (int)Math.Round(item.amount * StoredData[player.userID].Animal, 1);
				}
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            BasePlayer player = info?.InitiatorPlayer;

			if (player == null || player.IsNpc) return;

            if (!permission.UserHasPermission(player.UserIDString, "xrpg.use")) return;

            if (config.Animal.Kill)
            {
                if (!(StoredData[player.userID].Animal >= StoredData[player.userID].AnimalRate))
                        if (config.Animal.Animal.ContainsKey(entity.ShortPrefabName))
                        {
							var itema = config.Animal.Animal[entity.ShortPrefabName];
							float boost = 0;
							
							if (config.Setting.Slow)
                                boost = (itema - (itema / StoredData[player.userID].AnimalRate * (int)StoredData[player.userID].Animal)) * StoredData[player.userID].Boost;
							else
								boost = itema * StoredData[player.userID].Boost;

                            StoredData[player.userID].Animal += boost;

                            if (config.Setting.Panel)
								if (StoredData[player.userID].ActiveUI)
								{
                                    AnimalUI(player);
                                    if (config.Setting.Messages)
                                        UPMessageAnimal(player, $"+ {Math.Round(boost, 5)}");
								}
                        }
            }
        }

        void TOP(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "xrpg.top"))
            {
                SendReply(player, lang.GetMessage("NP", this, player.UserIDString));
                return;
            }

            int x = 1, y = 1, z = 1;

            SendReply(player, lang.GetMessage("WT", this, player.UserIDString));
            foreach (var i in StoredData.OrderByDescending(h => h.Value.Wood).Take(3))
            {
                var rplayer = covalence.Players.FindPlayerById(i.Key.ToString());

                SendReply(player, $"<color=orange>{x}</color>. {rplayer.Name} | {Math.Round(i.Value.Wood, 2)}");
                x++;
            }

            SendReply(player, lang.GetMessage("OT", this, player.UserIDString));
            foreach (var i in StoredData.OrderByDescending(h => h.Value.Ore).Take(3))
            {
                var rplayer = covalence.Players.FindPlayerById(i.Key.ToString());

                SendReply(player, $"<color=orange>{y}</color>. {rplayer.Name} | {Math.Round(i.Value.Ore, 2)}");
                y++;
            }

            SendReply(player, lang.GetMessage("AT", this, player.UserIDString));
            foreach (var i in StoredData.OrderByDescending(h => h.Value.Animal).Take(3))
            {
                var rplayer = covalence.Players.FindPlayerById(i.Key.ToString());

                SendReply(player, $"<color=orange>{z}</color>. {rplayer.Name} | {Math.Round(i.Value.Animal, 2)}");
                z++;
            }
        }

        void MaxRate(BasePlayer player)
        {
            float wrate = 0, orate = 0, arate = 0, boost = 0, x = 0;

            foreach (var perm in config.Permisssion)
                if (permission.UserHasPermission(player.UserIDString, perm.Key))
                {
                    wrate = perm.Value.WoodRate;
                    orate = perm.Value.OreRate;
                    arate = perm.Value.AnimalRate;
                    boost = perm.Value.Boost;

                    x++;

                    break;
                }

            if (x == 0)
            {
                if (StoredData[player.userID].Wood >= config.Wood.RateMax)
                {
                    StoredData[player.userID].Wood = config.Wood.RateMax;
                    StoredData[player.userID].WoodRate = config.Wood.RateMax;
                }
                else
                    StoredData[player.userID].WoodRate = config.Wood.RateMax;

                if (StoredData[player.userID].Ore >= config.Ore.RateMax)
                {
                    StoredData[player.userID].Ore = config.Ore.RateMax;
                    StoredData[player.userID].OreRate = config.Ore.RateMax;
                }
                else
                    StoredData[player.userID].OreRate = config.Ore.RateMax;

                if (StoredData[player.userID].Animal >= config.Animal.RateMax)
                {
                    StoredData[player.userID].Animal = config.Animal.RateMax;
                    StoredData[player.userID].AnimalRate = config.Animal.RateMax;
                }
                else
                    StoredData[player.userID].AnimalRate = config.Animal.RateMax;

                StoredData[player.userID].Boost = config.Setting.Boost;
            }
            else
            {
                StoredData[player.userID].WoodRate = wrate;
                StoredData[player.userID].OreRate = orate;
                StoredData[player.userID].AnimalRate = arate;

                StoredData[player.userID].Boost = boost;
            }
        }

        #endregion

        #region GUI

        void RPGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ".RPG");
            CuiElementContainer container = new CuiElementContainer();
 
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = config.Panel.AnchorMin, AnchorMax = config.Panel.AnchorMax, OffsetMin = config.Panel.OffsetMin, OffsetMax = config.Panel.OffsetMax },
                Image = { Color = "0 0 0 0" }
            }, "Hud", ".RPG");

            CuiHelper.AddUi(player, container); 
			
			WoodUI(player);
			OreUI(player);
			AnimalUI(player);
        }
		
		void HideUI(BasePlayer player)
		{ 
			CuiHelper.DestroyUi(player, ".Hide");  
            CuiElementContainer container = new CuiElementContainer();		
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-0.1475 0", AnchorMax = "-0.01 0.315", OffsetMax = "0 0" },
                Button = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat", Command = "ui hide" },
                Text = { Text = ">>", Align = TextAnchor.MiddleCenter, FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".RPG", ".Hide");
			
			CuiHelper.AddUi(player, container);
		}
		
		void ShowUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".Show");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-236 16", OffsetMax = "-210 42" },
                Button = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat", Command = "ui show" },
                Text = { Text = "<<", Align = TextAnchor.MiddleCenter, FontSize = 13, Color = "1 1 1 0.6" }
            }, "Overlay", ".Show");
			
			CuiHelper.AddUi(player, container);  
		}
		
		void WoodUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".Wood");
            CuiElementContainer container = new CuiElementContainer();
			
			float ratewood = StoredData[player.userID].Wood;
            float maxratewood = StoredData[player.userID].WoodRate;
            string pregresswood = ratewood >= maxratewood ? "0.985 0.9" : config.Panel.Progress ?  $"{0.137 + ((ratewood - config.Wood.RateStart) * (0.848 / (maxratewood - config.Wood.RateStart)))} 0.9" : $"{0.137 + (ratewood * (0.848 / maxratewood))} 0.9";
            string textwood = ratewood >= maxratewood ? $"{lang.GetMessage("W", this, player.UserIDString)} {maxratewood}x" : $"{lang.GetMessage("W", this, player.UserIDString)} {Math.Round(ratewood, 2)}x";
 
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.685", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat" }
            }, ".RPG", ".Wood");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0125 0.155", AnchorMax = "0.11 0.845", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.5", Sprite = "assets/icons/level_wood.png" },
                Text = { Text = "" }
            }, ".Wood"); 

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.137 0.1", AnchorMax = pregresswood, OffsetMax = "0 0" },
                Image = { Color = "0.5586275 0.7376471 0.2484314 0.92921569", Material = "assets/icons/greyout.mat" }
            }, ".Wood");    	

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = textwood, Align = TextAnchor.MiddleLeft, FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".Wood");
			
			CuiHelper.AddUi(player, container);
		}		
		
		void OreUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".Ore");
            CuiElementContainer container = new CuiElementContainer();
			
			float rateore = StoredData[player.userID].Ore; 
            float maxrateore = StoredData[player.userID].OreRate;
			string pregressore = rateore >= maxrateore ? "0.985 0.9" : config.Panel.Progress ?  $"{0.137 + ((rateore - config.Ore.RateStart) * (0.848 / (maxrateore - config.Ore.RateStart)))} 0.9" : $"{0.137 + (rateore * (0.848 / maxrateore))} 0.9";
            string textore = rateore >= maxrateore ? $"{lang.GetMessage("O", this, player.UserIDString)} {maxrateore}x" : $"{lang.GetMessage("O", this, player.UserIDString)} {Math.Round(rateore, 2)}x";

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.345", AnchorMax = "1 0.66", OffsetMax = "0 0" },
                Image = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat" }
            }, ".RPG", ".Ore");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0125 0.155", AnchorMax = "0.11 0.845", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.5", Sprite = "assets/icons/player_carry.png" },
                Text = { Text = "" }
            }, ".Ore");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.137 0.1", AnchorMax = pregressore, OffsetMax = "0 0" },
                Image = { Color = "0.2986275 0.6076471 0.8384314 0.92921569", Material = "assets/icons/greyout.mat" }
            }, ".Ore");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = textore, Align = TextAnchor.MiddleLeft, FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".Ore");
			
			CuiHelper.AddUi(player, container); 
		}		
		 
		void AnimalUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".Animal");
            CuiElementContainer container = new CuiElementContainer();
			
			float rateanimal = StoredData[player.userID].Animal;
            float maxrateanimal = StoredData[player.userID].AnimalRate;
			string pregressanimal = rateanimal >= maxrateanimal ? "0.985 0.9" : config.Panel.Progress ?  $"{0.137 + ((rateanimal - config.Animal.RateStart) * (0.848 / (maxrateanimal - config.Animal.RateStart)))} 0.9" : $"{0.137 + (rateanimal * (0.848 / maxrateanimal))} 0.9";
            string textanimal = rateanimal >= maxrateanimal ? $"{lang.GetMessage("A", this, player.UserIDString)} {maxrateanimal}x" : $"{lang.GetMessage("A", this, player.UserIDString)} {Math.Round(rateanimal, 2)}x";

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.315", OffsetMax = "0 0" },
                Image = { Color = "0.9686275 0.9176471 0.8784314 0.02921569", Material = "assets/icons/greyout.mat" }
            }, ".RPG", ".Animal");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0125 0.155", AnchorMax = "0.11 0.845", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.5", Sprite = "assets/icons/bite.png" },
                Text = { Text = "" }
            }, ".Animal");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.137 0.1", AnchorMax = pregressanimal, OffsetMax = "0 0" },
                Image = { Color = "0.7886275 0.4476471 0.2184314 0.92921569", Material = "assets/icons/greyout.mat" }
            }, ".Animal");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = textanimal, Align = TextAnchor.MiddleLeft, FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".Animal");
			
			CuiHelper.AddUi(player, container);
		}

        #endregion

        #region Message

        void UPMessageWood(BasePlayer player, string Messages)
        {
            CuiHelper.DestroyUi(player, ".UPMessageWood");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                FadeOut = 0.5f,
                RectTransform = { AnchorMin = "-0.5 0", AnchorMax = "-0.165 1", OffsetMax = "0 0" },
                Text = { FadeIn = 0.5f, Text = $"{Messages}", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".Wood", ".UPMessageWood");

            CuiHelper.AddUi(player, container);

            timer.Once(2.5f, () => { CuiHelper.DestroyUi(player, ".UPMessageWood"); });
        }

        void UPMessageOre(BasePlayer player, string Messages)
        {
            CuiHelper.DestroyUi(player, ".UPMessageOre");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                FadeOut = 0.5f,
                RectTransform = { AnchorMin = "-0.5 0", AnchorMax = "-0.165 1", OffsetMax = "0 0" },
                Text = { FadeIn = 0.5f, Text = $"{Messages}", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".Ore", ".UPMessageOre");

            CuiHelper.AddUi(player, container);

            timer.Once(2.5f, () => { CuiHelper.DestroyUi(player, ".UPMessageOre"); });
        }

        void UPMessageAnimal(BasePlayer player, string Messages)
        {
            CuiHelper.DestroyUi(player, ".UPMessageAnimal");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                FadeOut = 0.5f,
                RectTransform = { AnchorMin = "-0.5 0", AnchorMax = "-0.165 1", OffsetMax = "0 0" },
                Text = { FadeIn = 0.5f, Text = $"{Messages}", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = config.Setting.TextSize, Color = "1 1 1 0.6" }
            }, ".Animal", ".UPMessageAnimal");

            CuiHelper.AddUi(player, container);

            timer.Once(2.5f, () => { CuiHelper.DestroyUi(player, ".UPMessageAnimal"); });
        }

        #endregion
		
		#region Lang

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["W"] = "LUMBERJACK:",					
                ["O"] = "MINER:",					
                ["A"] = "HUNTER:",					
                ["WT"] = "TOP LUMBERJACK:",					
                ["OT"] = "TOP MINER:",					
                ["AT"] = "TOP HUNTER:",					
                ["NP"] = "Недостаточно прав!",					
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["W"] = "ЛЕСОРУБ:",					
                ["O"] = "РУДОКОП:",					
                ["A"] = "ОХОТНИК:",					
                ["WT"] = "ТОП ЛЕСОРУБОВ:",					
                ["OT"] = "ТОП РУДОКОПОВ:",					
                ["AT"] = "ТОП ОХОТНИКОВ:",					
                ["NP"] = "Недостаточно прав!",						
            }, this, "ru");
        }

        #endregion
    }
}