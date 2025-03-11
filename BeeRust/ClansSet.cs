using System.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("ClansSet", "King", "1.1.0")]
    class ClansSet : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin ImageLibrary, Clans;
        private Dictionary<string, DateTime> Cooldowns = new Dictionary<string, DateTime>();
        private Dictionary<string, string> clansData = new Dictionary<string, string>();
        private Dictionary<string, int> _itemIds = new Dictionary<string, int>();
        private List<string> SetKeys = new List<string>();

        private string Layer = "ClansSet.Layer";
        #endregion
        
        #region [Data]
        private void LoadData() => clansData = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("ClansSet/Data");
        private void SaveData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("ClansSet/Data", clansData);
        #endregion

        #region [ImageLibrary]
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [Oxide]
	    private void OnServerInitialized()
	    {
            LoadData();
            cmd.AddChatCommand(config._SettingsPlugin.openMenuCommand, this, "cmdOpenSet");

		    foreach (var key in config._SettingsKits)
		    {
			    if(!SetKeys.Contains(key.Key))
				    SetKeys.Add(key.Key);

			    if(!String.IsNullOrWhiteSpace(key.Value.Permissions) && !permission.PermissionExists(key.Value.Permissions, this))
				    permission.RegisterPermission(key.Value.Permissions, this);
		    }

            AddImage("https://i.imgur.com/zvtUYCS.png", "ImageLine");
            AddImage("https://i.imgur.com/LR2j88f.png", "NextPage");
            AddImage("https://i.imgur.com/5iHnDXC.png", "BackPage");
            AddImage("https://i.imgur.com/uHTdwjY.png", "ItemBackground");
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);

            SaveData();
        }
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 1, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class SettingsPlugin
        {
            [JsonProperty("Команда для открытия меню выбора скинов")]
            public string openMenuCommand;

            [JsonProperty("КД на изменения скинов")]
            public float cooldown;
        }

        public class SettingsKits
        {
            [JsonProperty("Картинка - ( Для аватарка набора )")]
            public string Image;

            [JsonProperty("ShortName ( Для аватарки набора )")]
            public string ShortName;

            [JsonProperty("SkinID ( Для аватарка набора )")]
            public ulong SkinID;

            [JsonProperty("Права дающие возможность использовать набор (оставьте поле пустым - будет доступен для всех)")]
            public string Permissions;

			[JsonProperty(PropertyName = "Настройки одежды")]
			public List<WearSettings> WearSettings;
        }

        public class WearSettings
        {
            [JsonProperty("ShortName")]
            public string ShortName;

            [JsonProperty("SkinID")]
            public ulong SkinID;
        }

        private class PluginConfig
        {
            [JsonProperty("Настройки плагина")] 
            public SettingsPlugin _SettingsPlugin = new SettingsPlugin();

		    [JsonProperty("Настройка одежды")]
		    public Dictionary<string, SettingsKits> _SettingsKits = new Dictionary<string, SettingsKits>();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _SettingsPlugin = new SettingsPlugin()
                    {
                        openMenuCommand = "set",
                        cooldown = 5f
                    },
                    _SettingsKits = new Dictionary<string, SettingsKits>()
                    {
                        ["default_set"] = new SettingsKits
                        {
                            Image = string.Empty,
                            ShortName = "metal.facemask",
                            Permissions = string.Empty,
                            SkinID = 0,
                            WearSettings = new List<WearSettings>()
                            {
                                new WearSettings()
                                {
                                    ShortName = "metal.facemask",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "metal.plate.torso",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "roadsign.kilt",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "hoodie",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "pants",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "shoes.boots",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "rifle.ak",
                                    SkinID = 0,
                                },
                            },
                        },
                        ["white_set"] = new SettingsKits
                        {
                            Image = string.Empty,
                            ShortName = "metal.facemask",
                            Permissions = "clansset.white",
                            SkinID = 2432948498,
                            WearSettings = new List<WearSettings>()
                            {
                                new WearSettings()
                                {
                                    ShortName = "metal.facemask",
                                    SkinID = 2432948498,
                                },
                                new WearSettings()
                                {
                                    ShortName = "metal.plate.torso",
                                    SkinID = 2432947351,
                                },
                                new WearSettings()
                                {
                                    ShortName = "roadsign.kilt",
                                    SkinID = 2469019097,
                                },
                                new WearSettings()
                                {
                                    ShortName = "hoodie",
                                    SkinID = 2416648557,
                                },
                                new WearSettings()
                                {
                                    ShortName = "pants",
                                    SkinID = 2416647256,
                                },
                                new WearSettings()
                                {
                                    ShortName = "shoes.boots",
                                    SkinID = 1657109993,
                                },
                                new WearSettings()
                                {
                                    ShortName = "rifle.ak",
                                    SkinID = 2536316473,
                                },
                            },
                        },
                        ["black_set"] = new SettingsKits
                        {
                            Image = string.Empty,
                            ShortName = "metal.facemask",
                            Permissions = "clansset.black",
                            SkinID = 2105454370,
                            WearSettings = new List<WearSettings>()
                            {
                                new WearSettings()
                                {
                                    ShortName = "metal.facemask",
                                    SkinID = 2105454370,
                                },
                                new WearSettings()
                                {
                                    ShortName = "metal.plate.torso",
                                    SkinID = 2105505757,
                                },
                                new WearSettings()
                                {
                                    ShortName = "roadsign.kilt",
                                    SkinID = 2120628865,
                                },
                                new WearSettings()
                                {
                                    ShortName = "hoodie",
                                    SkinID = 2080975449,
                                },
                                new WearSettings()
                                {
                                    ShortName = "pants",
                                    SkinID = 2080977144,
                                },
                                new WearSettings()
                                {
                                    ShortName = "shoes.boots",
                                    SkinID = 2090776132,
                                },
                                new WearSettings()
                                {
                                    ShortName = "rifle.ak",
                                    SkinID = 2437435853,
                                },
                            },
                        },
                        ["opulent_set"] = new SettingsKits
                        {
                            Image = string.Empty,
                            ShortName = "metal.facemask",
                            Permissions = string.Empty,
                            SkinID = 2193149013,
                            WearSettings = new List<WearSettings>()
                            {
                                new WearSettings()
                                {
                                    ShortName = "metal.facemask",
                                    SkinID = 2193149013,
                                },
                                new WearSettings()
                                {
                                    ShortName = "metal.plate.torso",
                                    SkinID = 2193157606,
                                },
                                new WearSettings()
                                {
                                    ShortName = "roadsign.kilt",
                                    SkinID = 2199787450,
                                },
                                new WearSettings()
                                {
                                    ShortName = "hoodie",
                                    SkinID = 2207288699,
                                },
                                new WearSettings()
                                {
                                    ShortName = "pants",
                                    SkinID = 2207291626,
                                },
                                new WearSettings()
                                {
                                    ShortName = "shoes.boots",
                                    SkinID = 0,
                                },
                                new WearSettings()
                                {
                                    ShortName = "rifle.ak",
                                    SkinID = 2843727355,
                                },
                            },
                        }
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [UI]
        private void cmdOpenSet(BasePlayer player)
        {
            var clan = GetClanTag(player.userID);
            if (string.IsNullOrEmpty(clan))
            {
                player.ChatMessage("Вы не находитесь ни в одном клане!");
                return;
            }
            
            if (!IsLeader(player.userID))
            {
                player.ChatMessage("Вы не являетесь главой своего клана!");
                return;
            }

            if (!clansData.ContainsKey(clan))
                clansData.Add(clan, string.Empty);

            MainUI(player);
        }

        private void MainUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.75", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

			container.Add(new CuiPanel
			{
				Image = { Color = "1 1 1 0.4" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-506.733 -77.133", OffsetMax = "-486.733 -75.8" }
			}, Layer, Layer + ".LineOne");

			container.Add(new CuiPanel
			{
				Image = { Color = "1 1 1 0.4" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-130.333 -77.133", OffsetMax = "504.741 -75.8" }
			}, Layer, Layer + ".LineTwo");

			container.Add(new CuiElement
			{
				Name = Layer + ".Title",
				Parent = Layer,
				Components =
				{
					new CuiTextComponent { Text = "Скины этого набора, чтобы поставить нажмите на кнопку 'Использовать'", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-472.954 -83.327", OffsetMax = "-136.913 -67.606" }
				}
			});

            CuiHelper.AddUi(player, container);
            DrawUI_ShowSet(player, 0);
        }

	    private void DrawUI_ShowSet(BasePlayer player, int page = 0)
	    {
		    string SelectSetKey = SetKeys[page];
		    string BackSetKey = page - 1 >= 0 ? SetKeys[page - 1] : SetKeys[SetKeys.Count - 1];
		    string NextSetKey = page + 1 >= SetKeys.Count ? SetKeys[0] : SetKeys[page + 1];

		    var SetCentral = config._SettingsKits[SelectSetKey];
		    var SetBack = config._SettingsKits[BackSetKey];
		    var SetNext = config._SettingsKits[NextSetKey];

            var clan = GetClanTag(player.userID);
            var container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-313.267 -47.22", OffsetMax = "310.333 287.737" }
			}, Layer, Layer + ".PanelSet");

            if (!string.IsNullOrEmpty(SetBack.Image))
            {
                container.Add(new CuiElement
                {
                    Name = Layer + ".PanelSet" + ".ThreeSet",
                    Parent = Layer + ".PanelSet",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(SetBack.Image), Color = "1 1 1 0.45" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-247 -34.258",OffsetMax = "-113.666 99.075" }
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = Layer + ".PanelSet" + ".ThreeSet",
                    Parent = Layer + ".PanelSet",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(SetBack.ShortName), SkinId = SetBack.SkinID, Color = "1 1 1 0.45" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-247 -34.258",OffsetMax = "-113.666 99.075" }
                    }
                });
            }

            if (!string.IsNullOrEmpty(SetNext.Image))
            {
                container.Add(new CuiElement
                {
                    Name = Layer + ".PanelSet" + ".TwoSet",
                    Parent = Layer + ".PanelSet",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(SetNext.Image), Color = "1 1 1 0.45" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "119.667 -34.258", OffsetMax = "253 99.075" }
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = Layer + ".PanelSet" + ".TwoSet",
                    Parent = Layer + ".PanelSet",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(SetNext.ShortName), SkinId = SetNext.SkinID, Color = "1 1 1 0.45" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "119.667 -34.258", OffsetMax = "253 99.075" }
                    }
                });
            }

            if (!string.IsNullOrEmpty(SetCentral.Image))
            {
                container.Add(new CuiElement
                {
                    Name = Layer + ".PanelSet" + ".TwoSet",
                    Parent = Layer + ".PanelSet",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(SetCentral.Image), Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "119.667 -34.258", OffsetMax = "253 99.075" }
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = Layer + ".PanelSet" + ".TwoSet",
                    Parent = Layer + ".PanelSet",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(SetCentral.ShortName), SkinId = SetCentral.SkinID, Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-113.666 -84.258", OffsetMax = "119.667 149.075" }
                    }
                });
            }

			container.Add(new CuiElement
			{
				Name = Layer + ".PanelSet" + ".NextPage",
				Parent = Layer + ".PanelSet",
				Components =
				{
					new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage("NextPage") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "271.8 12.408", OffsetMax = "311.8 52.408" }
				}
			});

			container.Add(new CuiElement
			{
				Name = Layer + ".PanelSet" + ".BackPage",
				Parent = Layer + ".PanelSet",
				Components =
				{
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("BackPage") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-311.8 12.408", OffsetMax = "-271.8 52.408" }
				}
			});

			container.Add(new CuiButton
			{
				Button = { Color = "1 1 1 0", Command = $"UI_SETS page.change {page + 1}"},
				Text = { Text = "" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -20", OffsetMax = "20 20" }
			}, Layer + ".PanelSet" + ".NextPage");

			container.Add(new CuiButton
			{
				Button = { Color = "1 1 1 0", Command = $"UI_SETS page.change {page - 1}"},
				Text = { Text = "" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -20", OffsetMax = "20 20" }
			}, Layer + ".PanelSet" + ".BackPage");

			container.Add(new CuiElement
			{
				Name = Layer + ".PanelSet" + ".Button",
				Parent = Layer + ".PanelSet",
				Components =
				{
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("ImageLine") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-113.666 -144.392", OffsetMax = "119.667 -113.725" }
				}
			});

			container.Add(new CuiButton
			{
				Button = { Color = "1 1 1 0", Command = $"UI_SETS set.change {SelectSetKey} {page}"},
				Text = { Text = !isPermission(player, SetCentral.Permissions) ? "Недостаточно прав" : clansData[clan] == SelectSetKey ? "Уже используете" : "Использовать", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
			}, Layer + ".PanelSet" + ".Button");

            int x = 0, y = 0;
            for (int i = 0; i < 20; i++)
            {
                var Wear = SetCentral.WearSettings.Count - 1 >= i ? SetCentral.WearSettings[i] : null;

			    container.Add(new CuiElement
			    {
				    Name = Layer + ".PanelSet" + $".Item{i}",
				    Parent = Layer + ".PanelSet",
				    Components =
				    {
					    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("ItemBackground") },
					    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-505.133 + (x * 102)} {-311.666 - (y * 110)}", OffsetMax = $"{-409.8 + (x * 102)} {-208.333 - (y * 110)}" }
				    }
			    });

                if (Wear != null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".PanelSet" + $".Item{i}",
                        Components =
                        {
                            new CuiImageComponent { FadeIn = 0.12f * i,ItemId = FindItemID(Wear.ShortName), SkinId = Wear.SkinID },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                        }
                    });

			        container.Add(new CuiElement
			        {
				        Parent = Layer + ".PanelSet" + $".Item{i}",
				        Components =
				        {
					        new CuiTextComponent { Text = $"{ItemManager.FindItemDefinition(Wear.ShortName)?.displayName?.english}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1", FadeIn = 0.12f * i,},
					        new CuiRectTransformComponent { AnchorMin = "0 0.87", AnchorMax = "0.96 1" }
				        }
			        });
                }

                x++;
                if (x != 10) continue;

                x = 0;
                y++;
            }

		    CuiHelper.DestroyUi(player, Layer + ".PanelSet");
		    CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [ConsoleCommand]
        [ConsoleCommand("UI_SETS")]
        private void cmdClansSet(ConsoleSystem.Arg args)
        {
		    BasePlayer player = args.Player();
		    if (player == null) return;

            switch (args.Args[0])
            {
			    case "page.change":
			    {
				    int page = int.Parse(args.Args[1]);
				    if (page >= SetKeys.Count)
					    page = 0;

				    if (page < 0)
					    page = SetKeys.Count - 1;

				    DrawUI_ShowSet(player, page);
				    break;
			    }
                case "set.change":
                {
                    var clan = GetClanTag(player.userID);
                    if (string.IsNullOrEmpty(clan)) return;

			        if (Cooldowns.ContainsKey(clan))
                        if (Cooldowns[clan].Subtract(DateTime.Now).TotalSeconds >= 0) return;

                    if (!clansData.ContainsKey(clan))
                        clansData.Add(clan, string.Empty);

                    if (clansData[clan] == args.Args[1]) return;

                    if (!SetKeys.Contains(args.Args[1])) return;
                    var getSet = config._SettingsKits[args.Args[1]];

                    if(!isPermission(player, getSet.Permissions))
                    {
                        player.ChatMessage("У вас недостаточно прав для использования этого набора одежды!");
                        return;
                    }

                    foreach (var skin in getSet.WearSettings)
                        GetChangeSkin(player.userID, skin.ShortName, skin.SkinID);

                    Cooldowns[clan] = DateTime.Now.AddSeconds(config._SettingsPlugin.cooldown);
                    clansData[clan] = args.Args[1];
                    DrawUI_ShowSet(player, int.Parse(args.Args[2]));
                    break;
                }
            }
        }
        #endregion
        
        #region [Func]
		private int FindItemID(string shortName)
		{
			int val;
			if (_itemIds.TryGetValue(shortName, out val))
				return val;

			var definition = ItemManager.FindItemDefinition(shortName);
			if (definition == null) return 0;

			val = definition.itemid;
			_itemIds[shortName] = val;
			return val;
		}

        private bool isPermission(BasePlayer player, string Permission) => String.IsNullOrWhiteSpace(Permission) || permission.UserHasPermission(player.UserIDString, Permission);

        private string GetClanTag(ulong userID) => Clans?.Call<string>("GetClanTag", userID);
        private bool IsLeader(ulong userID) => (bool)Clans?.Call<bool>("GetClanOwner", userID);
        private void GetChangeSkin(ulong userID, string shortName, ulong skinID) => Clans?.Call("GetChangeSkin", userID, shortName, skinID);
        #endregion
    }   
}