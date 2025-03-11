using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQInfo", "Mercury", "0.0.1")]
    [Description("Приятное меню для вашего сервера")]
    class IQInfo : RustPlugin
    {
		#region Reference
		[PluginReference] Plugin ImageLibrary;

		#region ImageLibrary
		private String GetImage(String fileName, UInt64 skin = 0)
		{
			var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
			if (!string.IsNullOrEmpty(imageId))
				return imageId;
			return String.Empty;
		}
		public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
		public void SendImage(BasePlayer player, String imageName, UInt64 imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
		public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);

		void AddAllImage()
		{
			Configuration.Interface Image = config.InterfaceSetting;
			
			if (!HasImage($"IQInfo_{Image.BackgroundImage}"))
				AddImage(Image.BackgroundImage, $"IQInfo_{Image.BackgroundImage}");
			if (!HasImage($"IQInfo_{Image.BackPageImage}"))
				AddImage(Image.BackPageImage, $"IQInfo_{Image.BackPageImage}");
			if (!HasImage($"IQInfo_{Image.CategorySelectImage}"))
				AddImage(Image.CategorySelectImage, $"IQInfo_{Image.CategorySelectImage}");
			if (!HasImage($"IQInfo_{Image.CheckBoxImage}"))
				AddImage(Image.CheckBoxImage, $"IQInfo_{Image.CheckBoxImage}");
			if (!HasImage($"IQInfo_{Image.CheckImage}"))
				AddImage(Image.CheckImage, $"IQInfo_{Image.CheckImage}");
			if (!HasImage($"IQInfo_{Image.ExitButtonImage}"))
				AddImage(Image.ExitButtonImage, $"IQInfo_{Image.ExitButtonImage}");
			if (!HasImage($"IQInfo_{Image.NextPageImage}"))
				AddImage(Image.NextPageImage, $"IQInfo_{Image.NextPageImage}");

			Configuration.Content Content = config.ContenteSetting;

			if (Content.UseLogoImage)
				if (!HasImage($"IQInfo_{Content.LogoServerImage}"))
					AddImage(Content.LogoServerImage, $"IQInfo_{Content.LogoServerImage}");

			List<Configuration.Content.Categories> Categories = Content.CategoriesSettings;

			foreach (Configuration.Content.Categories Category in Categories)
			{
				if (!HasImage($"IQInfo_{Category.CategoryImage}"))
					AddImage(Category.CategoryImage, $"IQInfo_{Category.CategoryImage}");

				foreach (Configuration.Content.Categories.Pages Page in Category.PageList.Where(i => i.UseImage))
					if (!HasImage($"IQInfo_{Page.Image}"))
						AddImage(Page.Image, $"IQInfo_{Page.Image}");
			}
		}
		void CachedImage(BasePlayer player)
		{
			Configuration.Interface Image = config.InterfaceSetting;

			SendImage(player, $"IQInfo_{Image.BackgroundImage}");
			SendImage(player, $"IQInfo_{Image.BackPageImage}");
			SendImage(player, $"IQInfo_{Image.CategorySelectImage}");
			SendImage(player, $"IQInfo_{Image.CheckBoxImage}");
			SendImage(player, $"IQInfo_{Image.CheckImage}");
			SendImage(player, $"IQInfo_{Image.ExitButtonImage}");
			SendImage(player, $"IQInfo_{Image.NextPageImage}");

			Configuration.Content Content = config.ContenteSetting;

			if (Content.UseLogoImage)
				SendImage(player, $"IQInfo_{Content.LogoServerImage}");

			List<Configuration.Content.Categories> Categories = Content.CategoriesSettings;

			foreach (Configuration.Content.Categories Category in Categories)
			{
				SendImage(player, $"IQInfo_{Category.CategoryImage}");

				foreach (Configuration.Content.Categories.Pages Page in Category.PageList.Where(i => i.UseImage))
					SendImage(player, $"IQInfo_{Page.Image}");
			}

		}
		#endregion

		#endregion

		#region Configuration
		private static Configuration config = new Configuration();
        private class Configuration
		{
			[JsonProperty("Введите команду для открытия меню")]
			public String CommandCustom = "menu";
			[JsonProperty("Настройка интерфейса")]
            public Interface InterfaceSetting = new Interface();
			[JsonProperty("Настройка контента меню")]
			public Content ContenteSetting = new Content();
			internal class Interface
			{
				[JsonProperty("Ссылка на задний фон плагина (Ссылка на картинку без картинки внутри - https://i.imgur.com/cSyXwis.png). Вы можете использовать любую свою фотку PNG (1474х965)")]
				public String BackgroundImage;
				[JsonProperty("Ссылка на картинку с страницей НАЗАД PNG (11х21)")]
				public String BackPageImage;
				[JsonProperty("Ссылка на картинку с страницей ВПЕРЕД PNG (11х21)")]
				public String NextPageImage;	
				[JsonProperty("Ссылка на картинку с выделенной категории PNG (242x53)")]
				public String CategorySelectImage;		
				[JsonProperty("Ссылка на картинку с кнопки ВЫХОД PNG (23x23)")]
				public String ExitButtonImage;
				[JsonProperty("Ссылка на картинку с чек боксом PNG (21х20)")]
				public String CheckBoxImage;
				[JsonProperty("Ссылка на картинку с галочкой для чек бокса PNG (13х9)")]
				public String CheckImage;
				[JsonProperty("RGBA цвет для текста")]
				public String RGBAColor;
				[JsonProperty("RGBA цвет для активного текста")]
				public String RGBAActiveColor;
			}
			internal class Content
			{
				[JsonProperty("Разрешить юзерам скрывать меню для последующих открытий при входе на сервер")]
				public Boolean UsePlayerHide;
				[JsonProperty("Использовать картинку в лого сервера (true - да/false - нет)")]
				public Boolean UseLogoImage;
				[JsonProperty("Ссылка на лого вашего сервера PNG (128x128)")]
				public String LogoServerImage;
				[JsonProperty("Отображаемое название вашего сервера")]
				public String LogoServerName;
				[JsonProperty("Текст с вашими контактами (Можете оставить поле пустым, если оно вам не нужно0")]
				public String ContactInformation;
				[JsonProperty("Настройка категорий и страниц к ним")]
				public List<Categories> CategoriesSettings = new List<Categories>();
				internal class Categories
				{
					[JsonProperty("Название категории в меню")]
					public String CategoryName;
					[JsonProperty("Ссылка ни PNG картинку для категории (64x64) для точного отображения используйте ПОЛНОСТЬЮ БЕЛУЮ КАРТИНКУ.Плагин сам ее покрасит")]
					public String CategoryImage;
					[JsonProperty("Название открытой категории на странице")]
					public String PageHeaderName;

					[JsonProperty("Настройка страниц с контентом")]
					public List<Pages> PageList = new List<Pages>();
					internal class Pages
					{
						[JsonProperty("Использовать изображение вместо текста(true - да/false - нет)")]
						public Boolean UseImage;
						[JsonProperty("Ссылка на ваше изображение(795х500 PNG)")]
						public String Image;
						[JsonProperty("Строки с текстом на одной странице(Максимум 25 строк на страницу)")]
						public List<String> TextLines = new List<string>();
					}
				}
			}

            public static Configuration GetNewConfiguration()
            {
				return new Configuration
				{
					CommandCustom = "menu",
					InterfaceSetting = new Interface
					{
						BackgroundImage = "https://i.imgur.com/Q9JvbSp.png",
						BackPageImage = "https://i.imgur.com/utq2qu3.png",
						NextPageImage = "https://i.imgur.com/m2hqqYi.png",
						CategorySelectImage = "https://i.imgur.com/EIubon0.png",
						ExitButtonImage = "https://i.imgur.com/7ePuJEk.png",
						CheckBoxImage = "https://i.imgur.com/ocUAesA.png",
						CheckImage = "https://i.imgur.com/iuZEJ0O.png",
						RGBAColor = "0.2078431 0.2078431 0.2078431 1",
						RGBAActiveColor = "0.9764706 0.7411765 0.4980392 1"
					},
					ContenteSetting = new Content
					{
						UsePlayerHide = true,
						UseLogoImage = true,
						LogoServerImage = "https://i.imgur.com/xY0mUSW.png",
						LogoServerName = "MERCURY DEV",
						ContactInformation = "НАШИ КОНТАКТЫ: VK - VK.COM/MERCURYDEV | DISCORD - MERCURY#5212 | ЭТО ВСЕ МОЖНО ИЗМЕНИТЬ",
						CategoriesSettings = new List<Content.Categories>
                        {
							new Content.Categories
                            {
								CategoryImage = "https://i.imgur.com/92aTs55.png",
								CategoryName = "Новости сервера",
								PageHeaderName = "Самые свежие новости",
								PageList = new List<Content.Categories.Pages>
                                {
									new Content.Categories.Pages
                                    {
										UseImage = true,
										Image = "https://i.imgur.com/xY0mUSW.png",
										TextLines = new List<String>
                                        {
											"- На сервере произошло обновление и добавлено новое меню IQInfo",
											"- Полностью переписан плагин",
											"- Новый интерфейс",
											"- Полная настройка интерфейса",
                                        },
                                    }
                                },
                            },
							new Content.Categories
							{
								CategoryImage = "https://i.imgur.com/2pgjncA.png",
								CategoryName = "Правила сервера",
								PageHeaderName = "Самые свежие правила",
								PageList = new List<Content.Categories.Pages>
								{
									new Content.Categories.Pages
									{
										UseImage = false,
										Image = "",
										TextLines = new List<String>
										{
											"- На сервере произошло обновление и добавлено новое меню IQInfo",
											"- Полностью переписан плагин",
											"- Новый интерфейс",
											"- Полная настройка интерфейса",
										},
									}
								},
							},
							new Content.Categories
							{
								CategoryImage = "https://i.imgur.com/83VpxGD.png",
								CategoryName = "Описание привилегий",
								PageHeaderName = "Самые свежие привилегии",
								PageList = new List<Content.Categories.Pages>
								{
									new Content.Categories.Pages
									{
										UseImage = false,
										Image = "",
										TextLines = new List<String>
										{
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- VIP",
											"- PREMIUM",
											"- PREMIUM",
										},
									}
								},
							},
						},
					}
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #178" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #33");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
		#endregion

		#region Hooks
		private void Init() => ReadData();
		private void OnServerInitialized()
		{
			cmd.AddChatCommand(config.CommandCustom, this, nameof(InterfaceMenu));
			cmd.AddConsoleCommand(config.CommandCustom, this, nameof(InterfaceMenu));
			AddAllImage();
			foreach (var p in BasePlayer.activePlayerList)
				OnPlayerConnected(p);
		}
		void OnPlayerConnected(BasePlayer player)
		{
			CachedImage(player);
			if (!HidePlayers.Contains(player.userID))
				InterfaceMenu(player);
		}
		void Unload()
        {
			WriteData();

			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, IQInfo_Hud);
				CuiHelper.DestroyUi(player, IQInfo_Background);
			}
		}
		#endregion

		#region Data
		public List<UInt64> HidePlayers = new List<UInt64>();
		void ReadData() => HidePlayers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<UInt64>>("IQInfo/HidePlayers");
		void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQInfo/HidePlayers", HidePlayers);
		#endregion

		#region Metods
		private void TurnedPlayer(BasePlayer player)
        {
			if (HidePlayers.Contains(player.userID))
				HidePlayers.Remove(player.userID);
			else HidePlayers.Add(player.userID);
        }
		private void PageControllerCategory(BasePlayer player, String PageAction, Int32 Page)
        {
			switch (PageAction)
			{
				case "next":
					{
						InterfaceCategory(player, Page + 1);
						break;
					}
				case "back":
					{
						InterfaceCategory(player, Page - 1);
						break;
					}
			}
			InterfaceContent(player);
		}
		private void PageControllerInfo(BasePlayer player, String PageAction, Int32 IndexCategory, Int32 Page)
		{
		    switch (PageAction)
			{
				case "next":
					{
						InterfaceContent(player, IndexCategory, Page + 1);
						break;
					}
				case "back":
					{
						InterfaceContent(player, IndexCategory, Page - 1);
						break;
					}
			}
		}

        #endregion

        #region Commands

		[ConsoleCommand("iqinfo")] 
		void ConsoleSystemIQInfo(ConsoleSystem.Arg arg)
        {
			BasePlayer player = arg.Player();
			if (player == null) return;
			String Action = arg.Args[0];

			switch(Action)
            {
				case "page.controller":
                    {
						String PageAction = arg.Args[1];
						Int32 Page = Int32.Parse(arg.Args[2]);
						PageControllerCategory(player, PageAction, Page);
						break;
                    }
				case "page.controller.info": 
					{
						String PageAction = arg.Args[1];
						Int32 IndexCategory = Int32.Parse(arg.Args[2]);
						Int32 Page = Int32.Parse(arg.Args[3]);
						PageControllerInfo(player, PageAction, IndexCategory, Page);
						break;
                    }
				case "select.category": 
					{
						Int32 Page = Int32.Parse(arg.Args[1]);
						Int32 SelectedCategory = Int32.Parse(arg.Args[2]);
						Int32 IndexInfo = SelectedCategory + (Page * 11);

						InterfaceCategory(player, Page, SelectedCategory);
						InterfaceContent(player, IndexInfo);
						break;
                    }
				case "hide.turn": 
					{
						TurnedPlayer(player);
						HideTurned(player);
						break;
                    }
            }
        }

        #endregion

		#region Interface

		private static String IQInfo_Hud = "IQInfo_Parent";
		private static String IQInfo_Background = "IQInfo_Background";

        #region Menu
        private void InterfaceMenu(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, IQInfo_Hud);
			CuiElementContainer container = new CuiElementContainer();
			Configuration.Interface Image = config.InterfaceSetting;
			Configuration.Content Content = config.ContenteSetting;

			container.Add(new CuiPanel
			{
				CursorEnabled = true,
				Image = { Color = "0 0 0 0.3529412", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
			}, "Overlay", IQInfo_Hud);

			container.Add(new CuiElement
			{
				Name = "CloseButtonImg",
				Parent = IQInfo_Hud,
				Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.ExitButtonImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-523 9", OffsetMax = "-507 25" }
				}
			});

			container.Add(new CuiElement
			{
				Name = "CloseButtonLabel",
				Parent = IQInfo_Hud,
				Components = {
					new CuiTextComponent { Text = "ВЫХОД", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-488.403 -354.397", OffsetMax = "-441.997 -331.402" }
				}
			});

			container.Add(new CuiButton
			{
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
				Button = { Close = IQInfo_Hud, Color = "0 0 0 0" },
				Text = { Text = "" }
			},  IQInfo_Hud);

			container.Add(new CuiElement
			{
				Name = IQInfo_Background,
				Parent = IQInfo_Hud,
				Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.BackgroundImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-526.27 -327.126", OffsetMax = "526.27 327.126" }
				}
			});

			if (Content.UseLogoImage)
			{
				container.Add(new CuiElement
				{
					Name = "LogoServer",
					Parent = IQInfo_Background,
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Content.LogoServerImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-454.5 -85.8", OffsetMax = "-390.5 -21.8" }
				}
				});
			}

			container.Add(new CuiElement
			{
				Name = "NameServer",
				Parent = IQInfo_Background,
				Components = {
					new CuiTextComponent { Text = Content.LogoServerName, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = Image.RGBAColor },
					new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-526.51 -131.744", OffsetMax = "-318.49 -90.056" }
				}
			});

			if(!String.IsNullOrWhiteSpace(Content.ContactInformation))
            {
                container.Add(new CuiElement
                {
                    Name = "ContactLabel",
                    Parent = IQInfo_Background,
                    Components = {
                        new CuiTextComponent { Text = Content.ContactInformation, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = Image.RGBAColor },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-286.437 -310.65", OffsetMax = "424.804 -286.15" }
					}
				});
            }
     

            CuiHelper.AddUi(player, container);

			InterfaceCategory(player);
			if (Content.CategoriesSettings[0] != null)
				InterfaceContent(player);
			HideTurned(player);
		}

        #endregion

        #region Hide

        private void HideTurned(BasePlayer player)
		{
			Configuration.Content Content = config.ContenteSetting;
			if (!Content.UsePlayerHide) return;
			CuiElementContainer container = new CuiElementContainer();
			Configuration.Interface Image = config.InterfaceSetting;

			CuiHelper.DestroyUi(player, "HideButtonImg");
			CuiHelper.DestroyUi(player, "HideButtonLabel");
			CuiHelper.DestroyUi(player, "HideBtn");
			CuiHelper.DestroyUi(player, "HideCheck");

			container.Add(new CuiElement
			{
				Name = "HideButtonImg",
				Parent = IQInfo_Hud,
				Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.CheckBoxImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-411 9", OffsetMax = "-395 25" }
				}
			});

			if(HidePlayers.Contains(player.userID))
            {
				container.Add(new CuiElement
				{
					Name = "HideCheck",
					Parent = "HideButtonImg",
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.CheckImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.25 0.3", AnchorMax = "0.75 0.7" }
				}
				});
			}

			container.Add(new CuiButton
			{
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
				Button = { Command = "iqinfo hide.turn", Color = "0 0 0 0" },
				Text = { Text = "" }
			}, "HideButtonImg", "HideBtn");

			container.Add(new CuiElement
			{
				Name = "HideButtonLabel",
				Parent = IQInfo_Hud,
				Components = {
					new CuiTextComponent { Text = "БОЛЬШЕ НЕ ПОКАЗЫВАТЬ", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-376.774 -354.388", OffsetMax = "-235.836 -331.393" }
				}
			});

			CuiHelper.AddUi(player, container);
		}
		#endregion

		#region Content

		private void InterfaceContent(BasePlayer player, Int32 IndexCategory = 0, Int32 Page = 0)
		{
			CuiElementContainer container = new CuiElementContainer();
			Configuration.Interface Image = config.InterfaceSetting;
			Configuration.Content Content = config.ContenteSetting;
			Configuration.Content.Categories Category = Content.CategoriesSettings[IndexCategory];

			CuiHelper.DestroyUi(player, "InfoPageBack");
			CuiHelper.DestroyUi(player, "InfoPageNext");
			CuiHelper.DestroyUi(player, "InfoPageCount");
			CuiHelper.DestroyUi(player, "InfoPageNextBtn");
			CuiHelper.DestroyUi(player, "InfoPageBackBtn");
			CuiHelper.DestroyUi(player, "ContentHeader");
			CuiHelper.DestroyUi(player, "ContentLine");
			CuiHelper.DestroyUi(player, "ContentPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-286.434 -272.578", OffsetMax = "508.434 305.328" }
            },  IQInfo_Background, "ContentPanel");

			container.Add(new CuiElement
			{
				Name = "ContentHeader",
				Parent = "ContentPanel",
				Components = {
					new CuiTextComponent { Text = Category.PageHeaderName.ToUpper(), Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleRight, Color = Image.RGBAColor },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-397.426 245.222", OffsetMax = "397.434 288.958" }
				}
			});

			Configuration.Content.Categories.Pages Lines = Category.PageList[Page];

			if (!Lines.UseImage)
			{
				Int32 Line = 0;
				foreach (String TextLine in Lines.TextLines.Take(25))
				{
					container.Add(new CuiElement
					{
						Name = "ContentLine",
						Parent = "ContentPanel",
						Components = {
					new CuiTextComponent { Text = TextLine, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = Image.RGBAColor },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-397.42602658 {183.13 + (-20 * Line)}", OffsetMax = $"397.434 {211.01 + (-20 * Line)}" }
				}
					});
					Line++;
				}
			}
            else
            {
				container.Add(new CuiElement
				{
					Name = "ContentLine",
					Parent = "ContentPanel",
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Lines.Image}") },
					new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.9" }
				}
				});
			}

			#region Page

			if (Page > 0) 
			{
				container.Add(new CuiElement
				{
					Name = "InfoPageBack",
					Parent = IQInfo_Background,
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.BackPageImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "444 -308.1", OffsetMax = "455 -287.1" }
				}
				});

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					Button = { Command = $"iqinfo page.controller.info back {IndexCategory} {Page}", Color = "0 0 0 0" },
					Text = { Text = "" }
				}, "InfoPageBack", "InfoPageBackBtn");
			}

			if (Category.PageList.Count - 1 > Page)
			{
				container.Add(new CuiElement
				{
					Name = "InfoPageNext",
					Parent = IQInfo_Background,
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.NextPageImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "482.4 -308.9", OffsetMax = "493.4 -287.9" }
				}
				});

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					Button = { Command = $"iqinfo page.controller.info next {IndexCategory} {Page}", Color = "0 0 0 0" },
					Text = { Text = "" }
				}, "InfoPageNext", "InfoPageNextBtn");
			}

			container.Add(new CuiElement
			{
				Name = "InfoPageCount",
				Parent = IQInfo_Background,
				Components = {
					new CuiTextComponent { Text = $"{Page}", Font = "robotocondensed-regular.ttf", FontSize = 21, Align = TextAnchor.MiddleCenter, Color = Image.RGBAColor },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "455 -312.4", OffsetMax = "482.4 -284.4" }
                }
            });

            #endregion

            CuiHelper.AddUi(player, container);
		}

		#endregion

		#region Categories
		private void InterfaceCategory(BasePlayer player, Int32 Page = 0, Int32 SelectedIndex = 0)
		{
			CuiElementContainer container = new CuiElementContainer();
			Configuration.Interface Image = config.InterfaceSetting;
			Configuration.Content Content = config.ContenteSetting;
			List<Configuration.Content.Categories> Categories = Content.CategoriesSettings;

			CuiHelper.DestroyUi(player, "CategoryPageBack");
			CuiHelper.DestroyUi(player, "CategoryPageNext");
			CuiHelper.DestroyUi(player, "CategoryPageCount");
			CuiHelper.DestroyUi(player, "CategoryBlock");
			CuiHelper.DestroyUi(player, "CategoryPanel");
			CuiHelper.DestroyUi(player, "CategoryImg");
			CuiHelper.DestroyUi(player, "CategoryLabel");
			CuiHelper.DestroyUi(player, "PageBack");
			CuiHelper.DestroyUi(player, "PageNext");
			CuiHelper.DestroyUi(player, "CategorySelect");

			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "0 0 0 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-515.822 -267.01502658", OffsetMax = "-328.129 179.31302658" }
			}, IQInfo_Background, "CategoryBlock");

			Int32 Y = 0;
			foreach (Configuration.Content.Categories Category in Content.CategoriesSettings.Skip(11 * Page).Take(11))
			{
				String SelectedColor = Y == SelectedIndex ? "1 1 1 1" : "0 0 0 0";
				String SelectedColorText = Y == SelectedIndex ? Image.RGBAActiveColor : Image.RGBAColor;

				container.Add(new CuiElement
				{
					Name = "CategoryPanel",
					Parent = "CategoryBlock",
					Components = {
					new CuiRawImageComponent { Color = SelectedColor, Png = GetImage($"IQInfo_{Image.CategorySelectImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-93.839 {190.598 + (-40 * Y)}", OffsetMax = $"93.851 {223.163 + (-40 * Y)}" }
				}
				});

				if (!String.IsNullOrWhiteSpace(Category.CategoryImage))
				{
					container.Add(new CuiElement
					{
						Name = "CategoryImg",
						Parent = "CategoryPanel",
						Components = {
						new CuiRawImageComponent { Color = SelectedColorText, Png = GetImage($"IQInfo_{Category.CategoryImage}") },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-81.9 -11.5", OffsetMax = "-57.9 11" }
					}
					});
				}

				container.Add(new CuiElement
				{
					Name = "CategoryLabel",
					Parent = "CategoryPanel",
					Components = {
					new CuiTextComponent { Text = Category.CategoryName, Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = SelectedColorText },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38.251 -12.5", OffsetMax = "76.987 11.5" }
				}
				});

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					Button = { Command = $"iqinfo select.category {Page} {Y}", Color = "0 0 0 0" },
					Text = { Text = "" }
				}, "CategoryPanel", "CategorySelect");

				Y++;
			}

			#region Pages

			if (Page > 0)
			{
				container.Add(new CuiElement
				{
					Name = "CategoryPageBack",
					Parent = IQInfo_Background,
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage($"IQInfo_{Image.BackPageImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-451 -306.7", OffsetMax = "-440 -285.7" }
				}
				});

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					Button = { Command = $"iqinfo page.controller back {Page}", Color = "0 0 0 0" },
					Text = { Text = "" }
				},  "CategoryPageBack", "PageBack");
			}

			if (Content.CategoriesSettings.Skip(11 * (Page + 1)).Count() != 0)
			{
				container.Add(new CuiElement
				{
					Name = "CategoryPageNext",
					Parent = IQInfo_Background,
					Components = {
					new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage($"IQInfo_{Image.NextPageImage}") },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-412.6 -306.7", OffsetMax = "-401.6 -285.7" }
				}
				});

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					Button = { Command = $"iqinfo page.controller next {Page}", Color = "0 0 0 0" },
					Text = { Text = "" }
				}, "CategoryPageNext", "PageNext");
			}

			container.Add(new CuiElement
			{
				Name = "CategoryPageCount",
				Parent = IQInfo_Background,
				Components = {
					new CuiTextComponent { Text = $"{Page}", Font = "robotocondensed-regular.ttf", FontSize = 21, Align = TextAnchor.MiddleCenter, Color = Image.RGBAColor },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-440 -310.2", OffsetMax = "-412.6 -282.2" }
				}
			});

			#endregion

			CuiHelper.AddUi(player, container);
		}

        #endregion

        #endregion
    }
}
