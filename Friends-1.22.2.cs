// #define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
	[Info("Friends", "Mevent", "1.22.2")]
	[Description("Adds a friends system with a visual interface for quick interaction")]
	public class Friends : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin
			ImageLibrary = null,
			Notifications = null,
			PlayerList = null,
			PlayerDatabase = null,
			Bank = null;

		private const string Layer = "Com.Mevent.Main";

		private static Friends _instance;

		private readonly Dictionary<ulong, EntityEntry> _playerEntities = new Dictionary<ulong, EntityEntry>();

		private class EntityEntry
		{
			public HashSet<AutoTurret> AutoTurrets = new HashSet<AutoTurret>();
			public HashSet<BuildingPrivlidge> BuildingPrivileges = new HashSet<BuildingPrivlidge>();
			public HashSet<CodeLock> CodeLocks = new HashSet<CodeLock>();
		}

		private enum AutoAuthType
		{
			All,
			Turret,
			Cupboard,
			Doors,
			Containers
		}

		private enum BtnType
		{
			None,
			Doors,
			Cupboard,
			Turrets,
			Containers,
			FriendlyFire,
			Sams
		}

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Добавлять в команду | Add to team")]
			public bool AddTeam = true;

			[JsonProperty(PropertyName = "Максимальное кол-во друзей | Max Friends")]
			public int MaxFriendsAmount = 3;

			[JsonProperty(PropertyName = "Задержка между сообщениями FF | Delay between FF messages")]
			public float FFDelay = 1;

			[JsonProperty(PropertyName =
				"Закрывать интерфейс после нажатия на кнопку | Close the interface after clicking on the button")]
			public bool AutoClose = true;

			[JsonProperty(PropertyName = "Огонь по друзьям | Friendly Fire")]
			public bool FriendlyFire = false;

			[JsonProperty(PropertyName = "Двери | Doors")]
			public bool Doors = true;

			[JsonProperty(PropertyName = "Турели | Turrets")]
			public bool Turrets = true;

			[JsonProperty(PropertyName = "Шкаф | Cupboard")]
			public bool Cupboard = true;

			[JsonProperty(PropertyName = "Ящики | Containers")]
			public bool Containers = true;

			[JsonProperty(PropertyName = "ПВО | SAMs")]
			public bool SAMs = true;

			[JsonProperty(PropertyName = "Добавлять друга к остальным друзьям? | Add a friend to other friends?")]
			public bool UseTeams = true;

			[JsonProperty(PropertyName = "Включить логирование в консоль? | Enable logging to the console?")]
			public bool LogToConsole = true;

			[JsonProperty(PropertyName = "Включить логирование в файл? | Enable logging to the file?")]
			public bool LogToFile = true;

			[JsonProperty(PropertyName = "Фон | Background")]
			public IPanel Background = new IPanel
			{
				AnchorMin = "0 0", AnchorMax = "1 1",
				OffsetMin = "0 0", OffsetMax = "0 0",
				Image = string.Empty,
				Color = new IColor("#0D1F4E", 95),
				isRaw = false,
				Sprite = "Assets/Content/UI/UI.Background.Tile.psd",
				Material = "Assets/Icons/IconMaterial.mat"
			};

			[JsonProperty(PropertyName = "Заглавие | Title")]
			public IText Title = new IText
			{
				AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
				OffsetMin = "-150 300", OffsetMax = "150 360",
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				FontSize = 38,
				Color = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Закрыть | Close")]
			public IText Close = new IText
			{
				AnchorMin = "1 1", AnchorMax = "1 1",
				OffsetMin = "-35 -35", OffsetMax = "-5 -5",
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				FontSize = 24,
				Color = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Ошибка | Error")]
			public IText Error = new IText
			{
				AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
				OffsetMin = "-300 -150", OffsetMax = "300 150",
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				FontSize = 38,
				Color = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Назад | Back")]
			public IText Back = new IText
			{
				AnchorMin = "0 0.5", AnchorMax = "0 0.5",
				OffsetMin = "0 -40", OffsetMax = "65 40",
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				FontSize = 60,
				Color = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Вперёд | Next")]
			public IText Next = new IText
			{
				AnchorMin = "1 0.5", AnchorMax = "1 0.5",
				OffsetMin = "-65 -40", OffsetMax = "0 40",
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				FontSize = 60,
				Color = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Настройка интерфейса | Interface Settings")]
			public IFriendPanel Panel = new IFriendPanel
			{
				AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
				Height = 100,
				Width = 820,
				Margin = 20,
				Count = 4,
				Color = new IColor("#1D3676", 100),
				Avatar = new InterfacePosition
				{
					AnchorMin = "0 0", AnchorMax = "0 0",
					OffsetMin = "5 5", OffsetMax = "95 95"
				},
				Nickname = new NickName
				{
					AnchorMin = "0 0", AnchorMax = "0 0",
					OffsetMin = "100 40", OffsetMax = "500 100",
					Align = TextAnchor.LowerLeft,
					Font = "robotocondensed-regular.ttf",
					Color = new IColor("#FFFFFF", 100),
					FontSize = 38,
					MaxLength = 30
				},
				Status = new SText
				{
					AnchorMin = "0 0", AnchorMax = "0 0",
					OffsetMin = "100 0", OffsetMax = "200 50",
					Align = TextAnchor.UpperLeft,
					Font = "robotocondensed-regular.ttf",
					Color = new IColor("#B5FFC9", 100),
					OfflineColor = new IColor("#B46292", 100),
					FontSize = 16
				},
				Button = new IButton
				{
					AnchorMin = "1 0.5", AnchorMax = "1 0.5",
					Height = 35,
					Width = 120,
					Margin = 5
				},
				Buttons = new List<FButton>
				{
					new FButton
					{
						Type = BtnType.Doors,
						Text = "Двери",
						Command = "friend doors {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.Cupboard,
						Text = "Шкаф",
						Command = "friend cupboard {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.Containers,
						Text = "Ящики",
						Command = "friend containers {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.Turrets,
						Text = "Турели",
						Command = "friend turrets {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.None,
						Text = "Исключить",
						Command = "friend remove {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.FriendlyFire,
						Text = "Урон",
						Command = "ff {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.None,
						Text = "ТП",
						Command = "tpr {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					},
					new FButton
					{
						Type = BtnType.Sams,
						Text = "ПВО",
						Command = "friend sams {user}",
						ActiveColor = new IColor("#5D8FDF", 95),
						DisactiveColor = new IColor("#5D8FDF", 35),
						FontSize = 20,
						Font = "robotocondensed-regular.ttf",
						Align = TextAnchor.MiddleCenter,
						TColor = new IColor("#1D3676", 100)
					}
				}
			};

			[JsonProperty(PropertyName = "Приглашение в друзья | Friend Invite")]
			public INotify FriendInvite = new INotify
			{
				Image = "friend",
				Url = "https://i.imgur.com/qAxHQIn.png",
				Delay = 30,
				Buttons = new List<INotifyButton>
				{
					new INotifyButton
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = "-137.5 5", OffsetMax = "-2.5 25",
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 14,
						Color = new IColor("#FFFFFF", 95),
						BColor = new IColor("#528A4E", 95),
						Command = "friend accept",
						Msg = "Accept"
					},
					new INotifyButton
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = "2.5 5", OffsetMax = "137.5 25",
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 14,
						Color = new IColor("#FFFFFF", 95),
						BColor = new IColor("#50488A", 95),
						Command = "friend cancel",
						Msg = "Cancel"
					}
				}
			};

			[JsonProperty(PropertyName = "Оповещение | Notification")]
			public INotify Notify = new INotify
			{
				Image = "friend",
				Url = "https://i.imgur.com/qAxHQIn.png",
				Delay = 5,
				Buttons = new List<INotifyButton>()
			};

			[JsonProperty(PropertyName = "Найти друга | Find a Friend")]
			public INotifyButton FindFriendBtn = new INotifyButton
			{
				AnchorMin = "0.5 0", AnchorMax = "0.5 0",
				OffsetMin = "-150 25", OffsetMax = "150 60",
				Align = TextAnchor.MiddleCenter,
				FontSize = 14,
				Font = "robotocondensed-regular.ttf",
				Color = new IColor("#5D8FDF", 100),
				BColor = new IColor("#1D3676", 100),
				Command = "playerslist",
				Msg = "FindFriend"
			};

			[JsonProperty(PropertyName = "PlayerDatabase")]
			public PlayerDatabaseConf PlayerDatabase =
				new PlayerDatabaseConf(false, "Friends");

			public VersionNumber Version;
		}

		private class PlayerDatabaseConf
		{
			[JsonProperty(PropertyName = "Включено")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Поле")] public string Field;

			public PlayerDatabaseConf(bool enabled, string field)
			{
				Enabled = enabled;
				Field = field;
			}
		}

		private class NickName : IText
		{
			[JsonProperty(PropertyName = "Максимальная длина | Max Lenght")]
			public int MaxLength;

			public new void Get(ref CuiElementContainer container, string parent = "Hud", string name = null,
				string text = "")
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				text = text.Substring(0, Mathf.Min(text.Length, MaxLength));

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = AnchorMin, AnchorMax = AnchorMax, OffsetMin = OffsetMin, OffsetMax = OffsetMax},
					Text =
					{
						Text = $"{text}", Align = Align, FontSize = FontSize, Color = Color.Get(),
						Font = Font
					}
				}, parent, name);
			}
		}

		private class INotify
		{
			[JsonProperty(PropertyName = "Ключ изображения | Image Key")]
			public string Image;

			[JsonProperty(PropertyName = "Ссылка на изображение | Image Url")]
			public string Url;

			[JsonProperty(PropertyName = "Время показа | Show Time")]
			public float Delay;

			[JsonProperty(PropertyName = "Кнопки | Buttons", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<INotifyButton> Buttons;
		}

		private class INotifyButton : IText
		{
			[JsonProperty(PropertyName = "Цвет | Color")]
			public IColor BColor;

			[JsonProperty(PropertyName = "Ключ языкового файла | Lang Key")]
			public string Msg;

			[JsonProperty(PropertyName = "Команда | Command")]
			public string Command;

			public void Get(ref CuiElementContainer container, BasePlayer player, string name = null,
				string parent = "")
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				container.Add(new CuiButton
				{
					RectTransform =
						{AnchorMin = AnchorMin, AnchorMax = AnchorMax, OffsetMin = OffsetMin, OffsetMax = OffsetMax},
					Text =
					{
						Text = _instance.Msg(Msg, player.UserIDString), Align = Align, FontSize = FontSize,
						Color = Color.Get(),
						Font = Font
					},
					Button =
					{
						Color = BColor.Get(),
						Command = Command
					}
				}, parent, name);
			}
		}

		private abstract class IAnchors
		{
			public string AnchorMin;

			public string AnchorMax;
		}

		private class InterfacePosition : IAnchors
		{
			public string OffsetMin;

			public string OffsetMax;
		}

		private class IFriendPanel : IAnchors
		{
			[JsonProperty(PropertyName = "Высота | Height")]
			public float Height;

			[JsonProperty(PropertyName = "Ширина | Width")]
			public float Width;

			[JsonProperty(PropertyName = "Отступ | Margin")]
			public float Margin;

			[JsonProperty(PropertyName = "Количество на странице | Count On Page")]
			public int Count;

			[JsonProperty(PropertyName = "Цвет | Color")]
			public IColor Color;

			[JsonProperty(PropertyName = "Аватарка | Avatar")]
			public InterfacePosition Avatar;

			[JsonProperty(PropertyName = "Никнейм | Nickname")]
			public NickName Nickname;

			[JsonProperty(PropertyName = "Статус | Status")]
			public SText Status;

			[JsonProperty(PropertyName = "Кнопка | Button")]
			public IButton Button;

			[JsonProperty(PropertyName = "Кнопки | Buttons", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<FButton> Buttons;

			public void Get(ref CuiElementContainer container, BasePlayer player, FriendData data, string parent,
				string name,
				string oMin, string oMax,
				int page,
				string mainParent)
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = AnchorMin, AnchorMax = AnchorMax,
						OffsetMin = oMin, OffsetMax = oMax
					},
					Image = {Color = Color.Get()}
				}, parent, name);

				if (_instance.ImageLibrary && Avatar != null)
					container.Add(new CuiElement
					{
						Parent = name,
						Components =
						{
							new CuiRawImageComponent
								{Png = _instance.ImageLibrary.Call<string>("GetImage", $"avatar_{data.UserId}")},
							new CuiRectTransformComponent
							{
								AnchorMin = Avatar.AnchorMin, AnchorMax = Avatar.AnchorMax,
								OffsetMin = Avatar.OffsetMin, OffsetMax = Avatar.OffsetMax
							}
						}
					});

				#region Name

				var targetId = $"{data.UserId}";

				var target = _instance.covalence.Players.FindPlayer(targetId);
				var displayName = _instance.GetPlayerName(data.UserId);

				var status = target != null && target.IsConnected;

				Nickname?.Get(ref container, name, name + ".Nickname", displayName);

				Status?.Get(ref container, status, name, name + ".Status",
					_instance.Msg(status ? Online : Offline, player.UserIDString));

				#endregion

				#region Buttons

				if (Buttons != null)
				{
					var xSwitch = -Button.Margin;

					for (var i = 0; i < Buttons.Count; i++)
					{
						var button = Buttons[i];

						var up = i < Buttons.Count / 2;

						var ySwitch = up ? Button.Height + Button.Margin / 2f : -(Button.Margin / 2f);

						button.Get(ref container, data, name, name + $".Btn.{i}", Button.AnchorMin, Button.AnchorMax,
							$"{xSwitch - Button.Width} {ySwitch - Button.Height}",
							$"{xSwitch} {ySwitch}",
							page,
							mainParent);

						if (i + 1 == Buttons.Count / 2)
							xSwitch = -Button.Margin;
						else
							xSwitch = xSwitch - Button.Margin - Button.Width;
					}
				}

				#endregion
			}
		}

		private class IPanel : InterfacePosition
		{
			[JsonProperty(PropertyName = "Изображение | Image")]
			public string Image;

			[JsonProperty(PropertyName = "Цвет | Color")]
			public IColor Color;

			[JsonProperty(PropertyName = "Сохранять цвет изображения? | Save Image Color")]
			public bool isRaw;

			[JsonProperty(PropertyName = "Sprite")]
			public string Sprite;

			[JsonProperty(PropertyName = "Material")]
			public string Material;

			public void Get(ref CuiElementContainer container, string parent = "Hud", string name = null,
				bool cursor = false)
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				if (isRaw)
				{
					var element = new CuiElement
					{
						Name = name,
						Parent = parent,
						Components =
						{
							new CuiRawImageComponent
							{
								Png = !string.IsNullOrEmpty(Image)
									? _instance.ImageLibrary.Call<string>("GetImage", Image)
									: null,
								Color = Color.Get(),
								Material = Material,
								Sprite = !string.IsNullOrEmpty(Sprite) ? Sprite : "Assets/Icons/rust.png"
							},
							new CuiRectTransformComponent
							{
								AnchorMin = AnchorMin, AnchorMax = AnchorMax, OffsetMin = OffsetMin,
								OffsetMax = OffsetMax
							}
						}
					};

					if (cursor) element.Components.Add(new CuiNeedsCursorComponent());

					container.Add(element);
				}
				else
				{
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = AnchorMin, AnchorMax = AnchorMax, OffsetMin = OffsetMin, OffsetMax = OffsetMax
						},
						Image =
						{
							Png = !string.IsNullOrEmpty(Image)
								? _instance.ImageLibrary.Call<string>("GetImage", Image)
								: null,
							Color = Color.Get(),
							Sprite =
								!string.IsNullOrEmpty(Sprite) ? Sprite : "Assets/Content/UI/UI.Background.Tile.psd",
							Material = !string.IsNullOrEmpty(Material) ? Material : "Assets/Icons/IconMaterial.mat"
						},
						CursorEnabled = cursor
					}, parent, name);
				}
			}
		}

		private class FButton
		{
			[JsonProperty(PropertyName = "Тип | Type")] [JsonConverter(typeof(StringEnumConverter))]
			public BtnType Type;

			[JsonProperty(PropertyName = "Текст | Text")]
			public string Text;

			[JsonProperty(PropertyName = "Команда | Command")]
			public string Command;

			[JsonProperty(PropertyName = "Активный Цвет | Active Color")]
			public IColor ActiveColor;

			[JsonProperty(PropertyName = "Неактивный Цвет | Disactive Color")]
			public IColor DisactiveColor;

			[JsonProperty(PropertyName = "Font Size")]
			public int FontSize;

			[JsonProperty(PropertyName = "Font")] public string Font;

			[JsonProperty(PropertyName = "Align")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor Align;

			[JsonProperty(PropertyName = "Text Color")]
			public IColor TColor;

			public void Get(ref CuiElementContainer container, FriendData data, string parent, string name, string aMin,
				string aMax, string oMin, string oMax, int page,
				string mainParent)
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				var color = ActiveColor;

				switch (Type)
				{
					case BtnType.Doors:
						color = data.Doors ? ActiveColor : DisactiveColor;
						break;
					case BtnType.Cupboard:
						color = data.Cupboard ? ActiveColor : DisactiveColor;
						break;
					case BtnType.Turrets:
						color = data.Turrets ? ActiveColor : DisactiveColor;
						break;
					case BtnType.FriendlyFire:
						color = data.FriendlyFire ? ActiveColor : DisactiveColor;
						break;
					case BtnType.Containers:
						color = data.Containers ? ActiveColor : DisactiveColor;
						break;
					case BtnType.Sams:
						color = data.SAMs ? ActiveColor : DisactiveColor;
						break;
				}

				var text = Text.Replace("{user}", data.UserId.ToString());
				var command = Command.Replace("{user}", data.UserId.ToString());

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = aMin, AnchorMax = aMax,
						OffsetMin = oMin, OffsetMax = oMax
					},
					Text =
					{
						Text = $"{text}",
						Align = Align,
						Font = Font,
						FontSize = FontSize,
						Color = TColor.Get()
					},
					Button =
					{
						Command =
							$"UI_Friends sendcmd {page} {mainParent} {command}", // command.Contains("chat.say") ? $"friendssendcmd {command}" : $"{command}",
						Color = color.Get(),
						Close = _config.AutoClose ? Layer : string.Empty
					}
				}, parent, name);
			}
		}

		private class IButton : IAnchors
		{
			[JsonProperty(PropertyName = "Высота | Height")]
			public float Height;

			[JsonProperty(PropertyName = "Ширина | Width")]
			public float Width;

			[JsonProperty(PropertyName = "Отступ | Margin")]
			public float Margin;
		}

		private class SText : InterfacePosition
		{
			[JsonProperty(PropertyName = "Font Size")]
			public int FontSize;

			[JsonProperty(PropertyName = "Font")] public string Font;

			[JsonProperty(PropertyName = "Align")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor Align;

			[JsonProperty(PropertyName = "Цвет игрока онлайн | Online Color")]
			public IColor Color;

			[JsonProperty(PropertyName = "Цвет игрока оффлайн | Offline Color")]
			public IColor OfflineColor;

			public void Get(ref CuiElementContainer container, bool online, string parent = "Hud", string name = null,
				string text = "")
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = AnchorMin, AnchorMax = AnchorMax, OffsetMin = OffsetMin, OffsetMax = OffsetMax},
					Text =
					{
						Text = $"{text}", Align = Align, FontSize = FontSize,
						Color = online ? Color.Get() : OfflineColor.Get(),
						Font = Font
					}
				}, parent, name);
			}
		}

		private class IText : InterfacePosition
		{
			[JsonProperty(PropertyName = "Font Size")]
			public int FontSize;

			[JsonProperty(PropertyName = "Font")] public string Font;

			[JsonProperty(PropertyName = "Align")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor Align;

			[JsonProperty(PropertyName = "Text Color")]
			public IColor Color;

			public void Get(ref CuiElementContainer container, string parent = "Hud", string name = null,
				string text = "")
			{
				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = AnchorMin, AnchorMax = AnchorMax, OffsetMin = OffsetMin, OffsetMax = OffsetMax},
					Text =
					{
						Text = $"{text}", Align = Align, FontSize = FontSize, Color = Color.Get(),
						Font = Font
					}
				}, parent, name);
			}
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string HEX;

			[JsonProperty(PropertyName = "Непрозрачность | Opacity (0 - 100)")]
			public float Alpha;

			public string Get()
			{
				if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

				var str = HEX.Trim('#');
				if (str.Length != 6) throw new Exception(HEX);
				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

				return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
			}

			public IColor(string hex, float alpha)
			{
				HEX = hex;
				Alpha = alpha;
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null) throw new Exception();

				if (_config.Version < Version)
					UpdateConfigValues();

				SaveConfig();
			}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
		}

		private void UpdateConfigValues()
		{
			PrintWarning("Config update detected! Updating config values...");

			var baseConfig = new Configuration();

			if (_config.Version < new VersionNumber(1, 14, 0))
				_config.AutoClose = baseConfig.AutoClose;

			if (_config.Version < new VersionNumber(1, 15, 0))
			{
				_config.UseTeams = baseConfig.UseTeams;
				_config.SAMs = baseConfig.SAMs;
			}

			_config.Version = Version;
			PrintWarning("Config update completed!");
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(_config);
		}

		protected override void LoadDefaultConfig()
		{
			_config = new Configuration();
		}

		#endregion

		#region Data

		private Dictionary<string, PlayerData> _usersData = new Dictionary<string, PlayerData>();

		private ListHashSet<ulong> _changedPlayers = new ListHashSet<ulong>();
		
		#region PlayerDatabase

		private PlayerData LoadPlayerDatabaseData(string userId)
		{
			PlayerData data;
			if (_usersData.TryGetValue(userId, out data))
				return data;

			var success =
				PlayerDatabase?.Call<string>("GetPlayerDataRaw", userId, _config.PlayerDatabase.Field);
			if (string.IsNullOrEmpty(success))
			{
				data = new PlayerData
				{
					UserID = Convert.ToUInt64(userId)
				};
				SavePlayerDatabaseData(userId, data);
				return _usersData[userId] = data;
			}

			if ((data = JsonConvert.DeserializeObject<PlayerData>(success)) == null)
			{
				data = new PlayerData
				{
					UserID = Convert.ToUInt64(userId)
				};
				SavePlayerDatabaseData(userId, data);
				return _usersData[userId] = data;
			}

			return _usersData[userId] = data;
		}

		private void SavePlayerDatabaseData(string userId, PlayerData data)
		{
			if (data == null) return;

			var serializeObject = JsonConvert.SerializeObject(data);
			if (serializeObject == null) return;

			PlayerDatabase?.Call("SetPlayerData", userId, _config.PlayerDatabase.Field, serializeObject);
		}

		#endregion

		#region Classes

		private class PlayerData
		{
			#region Fields

			[JsonProperty(PropertyName = "Display Name")]
			public string DisplayName;

			[JsonProperty(PropertyName = "UserId")]
			public ulong UserID;

			[JsonProperty(PropertyName = "Friends", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<FriendData> Friends = new List<FriendData>();

			[JsonProperty(PropertyName = "Removed Friends", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public HashSet<ulong> RemovedFriends = new HashSet<ulong>();

			#endregion

			#region Utils

			public bool IsFriend(ulong friend)
			{
				return Friends.Exists(check => check.UserId == friend);
			}

			public FriendData GetFriend(ulong friend)
			{
				return Friends.Find(check => check.UserId == friend);
			}

			public void AddFriend(ulong friend)
			{
				if (!friend.IsSteamId() || IsFriend(friend)) return;

				Friends.Add(new FriendData
				{
					UserId = friend,
					FriendlyFire = _config.FriendlyFire,
					Doors = _config.Doors,
					Turrets = _config.Turrets,
					Cupboard = _config.Cupboard,
					Containers = _config.Containers,
					SAMs = _config.SAMs
				});

				RemovedFriends.Remove(friend);
				
				OnChanged();
			}

			public void RemoveFriend(ulong friend)
			{
				if (Friends.RemoveAll(x => x.UserId == friend) > 0) 
					RemovedFriends.Add(friend);
				
				OnChanged();
			}

			#endregion

			#region Helpers

			private static string BaseFolder()
			{
				return "Friends" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar;
			}

			public static string[] GetFiles()
			{
				return GetFiles(BaseFolder());
			}

			public static string[] GetFiles(string baseFolder)
			{
				try
				{
					var json = ".json".Length;
					var paths = Interface.Oxide.DataFileSystem.GetFiles(baseFolder);
					for (var i = 0; i < paths.Length; i++)
					{
						var path = paths[i];
						var separatorIndex = path.LastIndexOf(Path.DirectorySeparatorChar);

						// We have to do this since GetFiles returns paths instead of filenames
						// And other methods require filenames
						paths[i] = path.Substring(separatorIndex + 1, path.Length - separatorIndex - 1 - json);
					}

					return paths;
				}
				catch
				{
					return Array.Empty<string>();
				}
			}

			public static PlayerData GetOrLoad(string userId)
			{
				if (!userId.IsSteamId()) return null;

				if (_config.PlayerDatabase.Enabled) return _instance.LoadPlayerDatabaseData(userId);

				return GetOrLoad(BaseFolder(), userId);
			}

			public static PlayerData GetNotLoad(string userId)
			{
				if (!userId.IsSteamId()) return null;

				if (_config.PlayerDatabase.Enabled) return _instance.LoadPlayerDatabaseData(userId);

				var data = GetOrLoad(BaseFolder(), userId, false);

				return data;
			}

			public static PlayerData GetOrLoad(string baseFolder, string userId, bool load = true)
			{
				PlayerData data;
				if (_instance._usersData.TryGetValue(userId, out data)) return data;

				try
				{
					data = ReadOnlyObject(baseFolder + userId);
				}
				catch (Exception e)
				{
					Interface.Oxide.LogError(e.ToString());
				}

				return load
					? _instance._usersData[userId] = data
					: data;
			}

			public static PlayerData GetOrCreate(string userId)
			{
				if (!userId.IsSteamId()) return null;

				if (_config.PlayerDatabase.Enabled) return _instance.LoadPlayerDatabaseData(userId);

				return GetOrLoad(userId) ?? (_instance._usersData[userId] = new PlayerData
				{
					UserID = Convert.ToUInt64(userId)
				});
			}
			
			public static bool IsLoaded(string userId)
			{
				return _instance._usersData.ContainsKey(userId);
			}

			public static void Save()
			{
#if TESTING
				Debug.Log($"[Save] count={_instance?._usersData.Count}");
#endif

				_instance?._usersData?.Keys.ToList().ForEach(Save);
			}

			public static void Save(string userId)
			{
				if (_config.PlayerDatabase.Enabled)
				{
					_instance.SavePlayerDatabaseData(userId, _instance.LoadPlayerDatabaseData(userId));
					return;
				}

				PlayerData data;
				if (!_instance._usersData.TryGetValue(userId, out data))
					return;

				Interface.Oxide.DataFileSystem.WriteObject(BaseFolder() + userId, data);
				
				_instance?._changedPlayers?.Remove(data.UserID);
			}

			public static void SaveAndUnload(string userId)
			{
				Save(userId);

				Unload(userId);
			}

			public static void Unload(string userId)
			{
				_instance?._usersData?.Remove(userId);
				
				_instance?._changedPlayers?.Remove(Convert.ToUInt64(userId));
			}

			private static PlayerData ReadOnlyObject(string name)
			{
				return Interface.Oxide.DataFileSystem.ExistsDatafile(name)
					? Interface.Oxide.DataFileSystem.GetFile(name).ReadObject<PlayerData>()
					: null;
			}

			public void OnChanged()
			{
				_instance?._changedPlayers?.TryAdd(UserID);
			}
			
			#endregion
		}
		
		private class FriendData
		{
			[JsonProperty(PropertyName = "UserId")]
			public ulong UserId;

			[JsonProperty(PropertyName = "FriendlyFire")]
			public bool FriendlyFire;

			[JsonProperty(PropertyName = "Doors")] public bool Doors;

			[JsonProperty(PropertyName = "Turrets")]
			public bool Turrets;

			[JsonProperty(PropertyName = "Cupboard")]
			public bool Cupboard;

			[JsonProperty(PropertyName = "Containers")]
			public bool Containers;

			[JsonProperty(PropertyName = "SAMs")] public bool SAMs;
		}

		#region Save Coroutine

		private Coroutine _saveCoroutine;

		private void StartSaveCoroutine()
		{
			_saveCoroutine = Rust.Global.Runner.StartCoroutine(HandleChangedPlayers());
		}

		private void StopSaveCoroutine()
		{
			if (_saveCoroutine != null) 
				Rust.Global.Runner.StopCoroutine(_saveCoroutine);
		}

		private IEnumerator HandleChangedPlayers()
		{
			var count = 0;
			
			while (_changedPlayers.Count > 0)
			{
				var player = _changedPlayers[0];
				_changedPlayers.RemoveAt(0);
				
				PlayerData.Save(player.ToString());
				
				if (++count % 5 == 0)
					yield return CoroutineEx.waitForFixedUpdate;
			}
		}
		
		#endregion
		
		#endregion

		#region Utils

		private static PlayerData FindPlayerData(string user)
		{
			foreach (var player in BasePlayer.allPlayerList)
				if (player.displayName.StartsWith(user, StringComparison.CurrentCultureIgnoreCase))
					return PlayerData.GetOrLoad(player.UserIDString);

			return null;
		}

		#region Migration

		private Coroutine _playersMigration;

		private IEnumerator StartOnAllPlayers(string[] players,
			Action<string> callback = null,
			Action onFinish = null)
		{
			for (var i = 0; i < players.Length; i++)
			{
				callback?.Invoke(players[i]);

				if (i % 10 == 0)
					yield return CoroutineEx.waitForFixedUpdate;
			}

			onFinish?.Invoke();

			_playersMigration = null;
		}

		#endregion

		#endregion

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;

			AddCovalenceCommand(new[] {"ff", "friendlyfire", "friend", "friends", "f", "fmenu", "f.menu"},
				nameof(CmdFriends));

			if (!_config.AddTeam)
			{
				Unsubscribe(nameof(OnTeamLeave));
				Unsubscribe(nameof(OnTeamAcceptInvite));
				Unsubscribe(nameof(OnTeamKick));
				Unsubscribe(nameof(OnTeamInvite));
				Unsubscribe(nameof(OnTeamRejectInvite));
			}
		}

		private void OnServerInitialized()
		{
			if (!ImageLibrary) PrintWarning("IMAGE LIBRARY IS NOT INSTALLED.");

			#region Init

			foreach (var player in BasePlayer.activePlayerList) 
				OnPlayerConnected(player);

			#endregion

			#region Notifications

			Notifications?.Call("AddImage", _config.FriendInvite.Image, _config.FriendInvite.Url);

			Notifications?.Call("AddImage", _config.Notify.Image, _config.Notify.Url);

			#endregion

			#region Team

			RelationshipManager.maxTeamSize = _config.MaxFriendsAmount + 1;

			#endregion

			#region Auth

			foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>()) CheckEntity(entity);

			#endregion

			timer.Every(1, TimeHandle);
		}

		private void OnServerSave()
		{
			timer.In(Random.Range(2f, 7f), StartSaveCoroutine);
		}

		private void Unload()
		{
			if (_playersMigration != null)
				Rust.Global.Runner.StopCoroutine(_playersMigration);

			if (_friendsConvertation != null)
				Rust.Global.Runner.StopCoroutine(_friendsConvertation);

			StopSaveCoroutine();
			
			foreach (var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, Layer);

				PlayerData.SaveAndUnload(player.UserIDString);
			}

			_instance = null;
			_config = null;
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			if (player == null || !player.userID.IsSteamId()) return;

			var data = PlayerData.GetOrCreate(player.UserIDString);
			if (data == null) return;

			var oldName = data.DisplayName;
			var newName = player.displayName;
			if (string.IsNullOrEmpty(oldName) || !oldName.Equals(newName))
			{
				data.DisplayName = newName;
				data.OnChanged();
			}
			
			GetAvatar(player.userID,
				avatar => ImageLibrary?.Call("AddImage", avatar, $"avatar_{player.UserIDString}"));

			if (_config.AddTeam && data.Friends.Count > 0)
			{
				RelationshipManager.PlayerTeam team;
				var friend =
					data.Friends.Find(x => RelationshipManager.ServerInstance.playerToTeam.ContainsKey(x.UserId));
				if (friend == null)
				{
					team = RelationshipManager.ServerInstance.CreateTeam();
					team.AddPlayer(player);
					team.SetTeamLeader(player.userID);

					var friendPlayer = FindPlayer(data.Friends);
					if (friendPlayer != null) team.AddPlayer(friendPlayer);
				}
				else
				{
					team = RelationshipManager.ServerInstance.playerToTeam[friend.UserId];
					if (team == null) return;
					team.AddPlayer(player);
				}
			}
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			if (player == null) return;

			PlayerData.SaveAndUnload(player.UserIDString);
		}

		private void OnEntityTakeDamage(BasePlayer player, HitInfo info)
		{
			if (player == null || info == null) return;

			var initiator = info.InitiatorPlayer;
			if (initiator == null) return;

			if (!IsFriend(player.userID, initiator.userID) || player.userID == initiator.userID)
				return;

			var data = PlayerData.GetOrLoad(initiator.UserIDString);

			var friend = data?.Friends.Find(x => x.UserId == player.userID);
			if (friend == null || friend.FriendlyFire) return;

			info.damageTypes.ScaleAll(0);

			if (IsCd(initiator.userID)) return;

			Notify(initiator, _config.Notify.Delay, Msg(NotifyTitle, initiator.UserIDString),
				Msg(FF, initiator.UserIDString, player.displayName), _config.Notify.Image);

			SetCd(initiator.userID);
		}

		#region Team

		private void OnTeamLeave(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
		{
			if (playerTeam == null || player == null) return;

			playerTeam.members.ToList().ForEach(user =>
			{
				PlayerData.GetOrLoad(player.UserIDString)?.RemoveFriend(user);
				PlayerData.GetOrLoad(user.ToString())?.RemoveFriend(player.userID);
			});

			UpdateAuthList(player.userID, AutoAuthType.All);
			UpdateTeamAuthList(playerTeam.members);
		}

		private void OnTeamAcceptInvite(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
		{
			NextTick(() =>
			{
				if (playerTeam == null || player == null || !player.userID.IsSteamId()) return;

				var invite = _invites.Find(x => x.Target == player);
				if (invite != null && playerTeam.members.Exists(x => invite.Inviter.userID == x))
				{
					AcceptInvite(player);
					return;
				}

				if (playerTeam.members.Contains(player.userID))
					if (_config.UseTeams)
					{
						playerTeam.members.ToList().ForEach(user =>
						{
							if (user == player.userID) return;

							PlayerData.GetOrLoad(player.UserIDString)?.AddFriend(user);
							PlayerData.GetOrLoad(user.ToString())?.AddFriend(player.userID);

							Log("friends", $"Player '{player}' added '{user}' as a friend");
						});

						UpdateTeamAuthList(playerTeam.members);
					}
			});
		}

		private void OnTeamKick(RelationshipManager.PlayerTeam playerTeam, BasePlayer player, ulong target)
		{
			NextTick(() =>
			{
				if (playerTeam == null) return;

				if (!playerTeam.members.Contains(target))
				{
					playerTeam.members.ToList().ForEach(user =>
					{
						PlayerData.GetOrLoad(user.ToString())?.RemoveFriend(target);
						PlayerData.GetOrLoad(target.ToString())?.RemoveFriend(user);
					});

					UpdateAuthList(target, AutoAuthType.All);
					UpdateTeamAuthList(playerTeam.members);
				}
			});
		}

		private void OnTeamInvite(BasePlayer inviter, BasePlayer target)
		{
			SendInvite(inviter, target);
		}

		private void OnTeamRejectInvite(BasePlayer rejector, RelationshipManager.PlayerTeam team)
		{
			CancelInvite(rejector);
		}

		private void UpdateTeamAuthList(List<ulong> teamMembers)
		{
			if (teamMembers.Count <= 0) return;
			teamMembers.ForEach(member => UpdateAuthList(member, AutoAuthType.All));
		}

		#endregion

		#region SAMs

		private object OnSamSiteTarget(SamSite samSite, BaseVehicle vehicle)
		{
			if (samSite == null || !samSite.OwnerID.IsSteamId() || vehicle == null)
				return null;

			var data = PlayerData.GetOrLoad(samSite.OwnerID.ToString());
			if (data == null)
				return null;

			if (vehicle.OwnerID == samSite.OwnerID ||
			    data.Friends.Exists(friend => friend.UserId == vehicle.OwnerID && friend.SAMs))
				return true;

			foreach (var mounted in vehicle.allMountPoints
				         .Where(allMountPoint => allMountPoint != null && allMountPoint.mountable != null)
				         .Select(allMountPoint => allMountPoint.mountable.GetMounted())
				         .Where(mounted => mounted != null))
			{
				if (mounted.userID == samSite.OwnerID)
					return true;

				var friendData = data.GetFriend(mounted.userID);
				if (friendData != null && friendData.SAMs)
					return true;
			}

			return null;
		}

		#endregion

		#endregion

		#region Commands

		private void CmdFriends(IPlayer cov, string command, string[] args)
		{
			var player = cov?.Object as BasePlayer;
			if (player == null) return;

			switch (command)
			{
				case "friendlyfire":
				case "ff":
				{
					var data = PlayerData.GetOrLoad(player.ToString());
					if (data == null) return;

					if (args.Length < 1)
					{
						Reply(player, FFErrorSyntax, command);
						return;
					}

					var friends = FindRemoveFriend(player, args[0]);
					if (friends == null || friends.Count == 0)
					{
						Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
							Msg(NotFound, player.UserIDString, args[0]), _config.Notify.Image);
						return;
					}

					PlayerData friend;

					if (friends.Count > 1)
					{
						int index;
						if (args.Length >= 3 && int.TryParse(args[2], out index))
						{
							if (index - 1 < 0 || friends.Count <= index - 1)
								return;

							friend = friends[index - 1];
						}
						else
						{
							var f = 0;
							var str = string.Join(", ", friends.Select(x =>
							{
								f++;
								return $"{x.DisplayName} ({x.UserID}) [{f}]";
							}));

							Reply(player, MultipleFound, str, f, args[0], args[1]);
							return;
						}
					}
					else
					{
						friend = friends.FirstOrDefault();
					}

					if (friend == null)
					{
						Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
							Msg(NotFound, player.UserIDString, args[0]), _config.Notify.Image);
						return;
					}

					var fData = data.GetFriend(friend.UserID);
					if (fData == null)
					{
						Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
							Msg(IsNotFriend, player.UserIDString, friend.DisplayName), _config.Notify.Image);
						return;
					}


					if (args.Length > 2)
						switch (args[1].ToLower())
						{
							case "on":
							{
								fData.FriendlyFire = true;
								break;
							}
							case "off":
							{
								fData.FriendlyFire = false;
								break;
							}
						}
					else
						fData.FriendlyFire = !fData.FriendlyFire;

					Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
						Msg(fData.FriendlyFire ? FFOn : FFOff, player.UserIDString), _config.Notify.Image);
					break;
				}
				default:
				{
					if (args.Length == 0)
					{
						MainUi(player);
						return;
					}

					switch (args[0].ToLower())
					{
						case "+":
						case "i":
						case "inv":
						case "invite":
						case "add":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friends = FindPlayer(args[1]);
							if (friends.Count == 0)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(NotFound, player.UserIDString, args[1]), _config.Notify.Image);
								return;
							}

							BasePlayer friend;

							if (friends.Count > 1)
							{
								int index;
								if (args.Length >= 3 && int.TryParse(args[2], out index))
								{
									if (index - 1 < 0 || friends.Count <= index - 1)
										return;

									friend = friends[index - 1];
								}
								else
								{
									var f = 0;
									var str = string.Join(", ", friends.Select(x =>
									{
										f++;
										return $"{x.displayName} ({x.UserIDString}) [{f}]";
									}));

									Reply(player, MultipleFound, str, f, args[0], args[1]);
									return;
								}
							}
							else
							{
								friend = friends.FirstOrDefault();
							}

							if (friend == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(NotFound, player.UserIDString, args[1]), _config.Notify.Image);
								return;
							}

							if (player.userID == friend.userID)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(CantAddSelf, player.UserIDString), _config.Notify.Image);
								return;
							}

							var pData = PlayerData.GetOrLoad(player.UserIDString);
							if (pData == null) return;

							if (pData.IsFriend(friend.userID))
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(AlreadyFriends, player.UserIDString), _config.Notify.Image);
								return;
							}

							if (pData.Friends.Count >= _config.MaxFriendsAmount)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(MaxFriends, player.UserIDString), _config.Notify.Image);
								return;
							}

							var fData = PlayerData.GetOrLoad(friend.UserIDString);
							if (fData == null) return;

							if (fData.Friends.Count >= _config.MaxFriendsAmount)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(TargetMaxFriends, player.UserIDString), _config.Notify.Image);
								return;
							}

							SendInvite(player, friend);
							break;
						}

						case "-":
						case "del":
						case "delete":
						case "rem":
						case "remove":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friends = FindRemoveFriend(player, args[1]);
							if (friends == null || friends.Count == 0)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(NotFound, player.UserIDString, args[1]), _config.Notify.Image);
								return;
							}

							PlayerData friend;

							if (friends.Count > 1)
							{
								int index;
								if (args.Length >= 3 && int.TryParse(args[2], out index))
								{
									if (index - 1 < 0 || friends.Count <= index - 1)
										return;

									friend = friends[index - 1];
								}
								else
								{
									var f = 0;
									var str = string.Join(", ", friends.Select(x =>
									{
										f++;
										return $"{x.DisplayName} ({x.UserID}) [{f}]";
									}));

									Reply(player, MultipleFound, str, f, args[0], args[1]);
									return;
								}
							}
							else
							{
								friend = friends.FirstOrDefault();
							}

							if (friend == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(NotFound, player.UserIDString, args[1]), _config.Notify.Image);
								return;
							}

							CuiHelper.DestroyUi(player, Layer);

							RemoveFriend(player, friend.UserID);
							break;
						}

						case "a":
						case "accept":
						{
							AcceptInvite(player);
							break;
						}

						case "c":
						case "cancel":
						{
							CancelInvite(player);
							break;
						}

						case "list":
						{
							var data = PlayerData.GetOrLoad(player.UserIDString);
							if (data == null) return;

							if (data.Friends.Count == 0)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(NoFriends, player.UserIDString), _config.Notify.Image);
							}
							else
							{
								var friends = string.Join(", ",
									data.Friends.Select(x =>
										$"{PlayerData.GetOrLoad(x.UserId.ToString())?.DisplayName ?? "NONE"} ({x.UserId})"));

								Reply(player, Msg(FriendList, player.UserIDString, friends));
							}

							break;
						}

						case "doors":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friend = FindPlayerData(args[1]);
							if (friend == null)
							{
								Reply(player, NotFound, args[1]);
								return;
							}

							var friendData = PlayerData.GetOrLoad(player.UserIDString)?.GetFriend(friend.UserID);
							if (friendData == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(IsNotFriend, player.UserIDString, friend.DisplayName), _config.Notify.Image);
								return;
							}

							friendData.Doors = !friendData.Doors;
							Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
								Msg(friendData.Doors ? DoorsOn : DoorsOff, player.UserIDString), _config.Notify.Image);

							UpdateAuthList(player.userID, AutoAuthType.Doors);
							break;
						}

						case "cupboard":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friend = FindPlayerData(args[1]);
							if (friend == null)
							{
								Reply(player, NotFound, args[1]);
								return;
							}

							var friendData = PlayerData.GetOrLoad(player.UserIDString)?.GetFriend(friend.UserID);
							if (friendData == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(IsNotFriend, player.UserIDString, friend.DisplayName), _config.Notify.Image);
								return;
							}

							friendData.Cupboard = !friendData.Cupboard;
							Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
								Msg(friendData.Cupboard ? CupboardOn : CupboardOff, player.UserIDString),
								_config.Notify.Image);

							UpdateAuthList(player.userID, AutoAuthType.Cupboard);
							break;
						}

						case "containers":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friend = FindPlayerData(args[1]);
							if (friend == null)
							{
								Reply(player, NotFound, args[1]);
								return;
							}

							var friendData = PlayerData.GetOrLoad(player.UserIDString)?.GetFriend(friend.UserID);
							if (friendData == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(IsNotFriend, player.UserIDString, friend.DisplayName), _config.Notify.Image);
								return;
							}

							friendData.Containers = !friendData.Containers;
							Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
								Msg(friendData.Containers ? ContainersOn : ContainersOff, player.UserIDString),
								_config.Notify.Image);

							UpdateAuthList(player.userID, AutoAuthType.Containers);
							break;
						}

						case "turrets":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friend = FindPlayerData(args[1]);
							if (friend == null)
							{
								Reply(player, NotFound, args[1]);
								return;
							}

							var friendData = PlayerData.GetOrLoad(player.UserIDString)?.GetFriend(friend.UserID);
							if (friendData == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(IsNotFriend, player.UserIDString, friend.DisplayName), _config.Notify.Image);
								return;
							}

							friendData.Turrets = !friendData.Turrets;
							Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
								Msg(friendData.Turrets ? TurretsOn : TurretsOff, player.UserIDString),
								_config.Notify.Image);

							UpdateAuthList(player.userID, AutoAuthType.Turret);
							break;
						}

						case "sams":
						{
							if (args.Length < 2)
							{
								Reply(player, ErrorSyntax, command, args[0]);
								return;
							}

							var friend = FindPlayerData(args[1]);
							if (friend == null)
							{
								Reply(player, NotFound, args[1]);
								return;
							}

							var friendData = PlayerData.GetOrLoad(player.UserIDString)?.GetFriend(friend.UserID);
							if (friendData == null)
							{
								Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
									Msg(IsNotFriend, player.UserIDString, friend.DisplayName), _config.Notify.Image);
								return;
							}

							friendData.SAMs = !friendData.SAMs;
							Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
								Msg(friendData.SAMs ? SamsOn : SamsOff, player.UserIDString),
								_config.Notify.Image);
							break;
						}

						case "help":
						{
							Reply(player, Help);
							break;
						}
					}

					break;
				}
			}
		}

		[ConsoleCommand("UI_Friends")]
		private void ConsoleCmdFriends(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "page":
				{
					int page;
					if (!arg.HasArgs(3) ||
					    !int.TryParse(arg.Args[1], out page)) return;

					MainUi(player, arg.Args[2], page);
					break;
				}

				case "sendcmd":
				{
					int page;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out page)) return;

					var command = string.Join(" ", arg.Args.Skip(3));
					if (string.IsNullOrEmpty(command)) return;

					player.Command(command.Contains("chat.say") ? $"friendssendcmd {command}" : $"{command}");

					if (!_config.AutoClose)
						timer.In(0.2f, () => MainUi(player, arg.Args[2], page));
					break;
				}

				case "close":
				{
					CuiHelper.DestroyUi(player, Layer);
					break;
				}
			}
		}

		[ConsoleCommand("friendssendcmd")]
		private void SendCommand(ConsoleSystem.Arg args)
		{
			var player = args.Player();
			if (player == null || !args.HasArgs()) return;

			var convertcmd = args.Args.Length == 1
				? args.Args[0]
				: $"{args.Args[0]}  \" {string.Join(" ", args.Args.ToList().GetRange(1, args.Args.Length - 1))}\" 0";
			player.SendConsoleCommand(convertcmd);
		}

		[ConsoleCommand("friends.migrate")]
		private void CmdConsoleMigrate(ConsoleSystem.Arg arg)
		{
			if (!arg.IsServerside) return;

			var players = PlayerData.GetFiles();
			if (players != null && players.Length > 0)
			{
				_playersMigration = Rust.Global.Runner.StartCoroutine(StartOnAllPlayers(players, player =>
				{
					var data = PlayerData.GetOrLoad(player);
					if (data != null)
						SavePlayerDatabaseData(data.UserID.ToString(), data);
				}, () => { PrintWarning("The migration is complete!"); }));
			}
		}

		#endregion

		#region Interface

		private void MainUi(BasePlayer player, string parent = "Overlay", int page = 0)
		{
			var data = PlayerData.GetOrLoad(player.UserIDString);
			if (data == null) return;

			if (string.IsNullOrEmpty(parent))
				parent = "Overlay";

			var container = new CuiElementContainer();

			_config.Background.Get(ref container, parent, Layer, true);

			if (parent == "Overlay")
				_config.Title.Get(ref container, Layer, null, Msg(UITitle, player.UserIDString));

			#region Friends

			var friends = data.Friends?.Skip(_config.Panel.Count * page).Take(_config.Panel.Count).ToList();
			if (friends != null && friends.Count > 0)
			{
				var ySwitch = (friends.Count * _config.Panel.Height + (friends.Count - 1) * _config.Panel.Margin) /
				              2f;
				friends.ForEach(friend =>
				{
					if (friend == null) return;

					_config.Panel?.Get(ref container, player, friend, Layer, null,
						$"-{_config.Panel.Width / 2f} {ySwitch - _config.Panel.Height}",
						$"{_config.Panel.Width / 2f} {ySwitch}", page, parent);

					ySwitch = ySwitch - _config.Panel.Margin - _config.Panel.Height;
				});
			}
			else
			{
				_config.Error.Get(ref container, Layer, null, Msg(NoFriends, player.UserIDString));
			}

			#endregion

			#region Pages

			if (data.Friends != null && data.Friends.Count > _config.Panel.Count)
			{
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin =
							_config.Back.AnchorMin,
						AnchorMax =
							_config.Back.AnchorMax,
						OffsetMin =
							_config.Back.OffsetMin,
						OffsetMax =
							_config.Back.OffsetMax
					},
					Text =
					{
						Text = "«",
						Align =
							_config.Back.Align,
						FontSize =
							_config.Back.FontSize,
						Font =
							_config.Back.Font,
						Color =
							_config.Back.Color.Get()
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = page != 0 ? $"UI_Friends page {page - 1} {parent}" : ""
					}
				}, Layer);

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin =
							_config.Next.AnchorMin,
						AnchorMax =
							_config.Next.AnchorMax,
						OffsetMin =
							_config.Next.OffsetMin,
						OffsetMax =
							_config.Next.OffsetMax
					},
					Text =
					{
						Text = "»",
						Align =
							_config.Next.Align,
						FontSize =
							_config.Next.FontSize,
						Font =
							_config.Next.Font,
						Color =
							_config.Next.Color.Get()
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = data.Friends.Count > (page + 1) * _config.Panel.Count
							? $"UI_Friends page {page + 1} {parent}"
							: ""
					}
				}, Layer);
			}

			#endregion

			#region Find Friend

			if (data.Friends != null && PlayerList && GetMaxFriends() - data.Friends.Count > 0)
				_config.FindFriendBtn.Get(ref container, player, null, Layer);

			#endregion

			#region Close

			_config.Close?.Get(ref container, Layer, Layer + ".Close", "✕");

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text = {Text = ""},
				Button = {Color = "0 0 0 0", Command = "UI_Friends close"}
			}, Layer + ".Close");

			#endregion

			CuiHelper.DestroyUi(player, Layer);
			CuiHelper.AddUi(player, container);
		}

		#endregion

		#region Auth

		#region Hooks

		private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
		{
			if (privilege == null || player == null) return;

			var count = privilege.authorizedPlayers.Count;
			if (count == 0 || count <= RelationshipManager.maxTeamSize) return;
			
			var newAuthorizedPlayers = privilege.authorizedPlayers.ToList();
			newAuthorizedPlayers.RemoveRange(0, count - 1);
				
			privilege.authorizedPlayers = new HashSet<PlayerNameID>(newAuthorizedPlayers);
		}

		private void OnEntitySpawned(BaseEntity entity)
		{
			NextTick(() => CheckEntity(entity, true));
		}

		private void CheckEntity(BaseEntity entity, bool justCreated = false)
		{
			if (entity == null) return;

			var buildingPrivilege = entity as BuildingPrivlidge;
			if (buildingPrivilege != null && buildingPrivilege.OwnerID.IsSteamId())
			{
				EntityEntry playerEntity;
				if (_playerEntities.TryGetValue(entity.OwnerID, out playerEntity))
					playerEntity.BuildingPrivileges.Add(buildingPrivilege);
				else
					_playerEntities.Add(entity.OwnerID,
						new EntityEntry {BuildingPrivileges = new HashSet<BuildingPrivlidge> {buildingPrivilege}});

				if (justCreated)
					AuthToCupboard(new HashSet<BuildingPrivlidge> {buildingPrivilege}, entity.OwnerID);
				return;
			}

			var autoTurret = entity as AutoTurret;
			if (autoTurret != null && autoTurret.OwnerID.IsSteamId())
			{
				EntityEntry playerEntity;
				if (_playerEntities.TryGetValue(entity.OwnerID, out playerEntity))
					playerEntity.AutoTurrets.Add(autoTurret);
				else
					_playerEntities.Add(entity.OwnerID,
						new EntityEntry {AutoTurrets = new HashSet<AutoTurret> {autoTurret}});

				if (justCreated)
					AuthToTurret(new HashSet<AutoTurret> {autoTurret}, entity.OwnerID);
				return;
			}

			var codeLock = entity as CodeLock;
			if (codeLock != null)
			{
				var parentEntity = codeLock.GetParentEntity();
				if (parentEntity != null && IsBankBox(parentEntity))
					return;

				var ownerId = codeLock.OwnerID.IsSteamId() ? codeLock.OwnerID :
					parentEntity != null ? parentEntity.OwnerID : 0;
				if (!ownerId.IsSteamId())
					return;

				EntityEntry playerEntity;
				if (_playerEntities.TryGetValue(ownerId, out playerEntity))
					playerEntity.CodeLocks.Add(codeLock);
				else
					_playerEntities.Add(ownerId,
						new EntityEntry {CodeLocks = new HashSet<CodeLock> {codeLock}});

				if (justCreated)
					AuthToCodeLock(new HashSet<CodeLock> {codeLock}, ownerId);
			}
		}

		private void OnEntityKill(BaseEntity entity)
		{
			if (entity == null || entity.OwnerID == 0) return;

			var buildingPrivilege = entity as BuildingPrivlidge;
			if (buildingPrivilege != null)
			{
				foreach (var entry in _playerEntities.Where(entry =>
					         entry.Value.BuildingPrivileges.Contains(buildingPrivilege)))
					entry.Value.BuildingPrivileges.Remove(buildingPrivilege);

				return;
			}

			var autoTurret = entity as AutoTurret;
			if (autoTurret != null)
			{
				foreach (var entry in _playerEntities.Where(entry => entry.Value.AutoTurrets.Contains(autoTurret)))
					entry.Value.AutoTurrets.Remove(autoTurret);

				return;
			}

			var codeLock = entity as CodeLock;
			if (codeLock != null)
			{
				foreach (var entry in _playerEntities.Where(x => x.Value.CodeLocks.Contains(codeLock)))
					entry.Value.CodeLocks.Remove(codeLock);
			}
		}

		private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
		{
			if (player == null || baseLock == null) return null;

			var parentEntity = baseLock.GetParentEntity();
			var ownerId = baseLock.OwnerID.IsSteamId() ? baseLock.OwnerID :
				parentEntity != null ? parentEntity.OwnerID : 0;
			if (!ownerId.IsSteamId() || ownerId == player.userID)
				return null;

			// Ignore the bank boxes
			if (IsBankBox(parentEntity))
			{
				return null;
			}

			var friend = PlayerData.GetOrLoad(baseLock.OwnerID.ToString())?.GetFriend(player.userID);
			if (friend == null)
			{
				return null;
			}


			if ((parentEntity is Door && friend.Doors) ||
			    (parentEntity is BuildingPrivlidge && friend.Cupboard) ||
			    (parentEntity is StorageContainer && !parentEntity.ShortPrefabName.Contains("cupboard") &&
			     friend.Containers))
			{
				var codeLock = baseLock as CodeLock;
				if (codeLock != null)
				{
					Effect.server.Run(codeLock.effectUnlocked.resourcePath, codeLock.transform.position);
				}

				return true;
			}


			return null;
		}

		private void CanChangeCode(BasePlayer player, CodeLock codeLock, string code, bool isGuest)
		{
			NextTick(() =>
			{
				if (!isGuest ? codeLock.code != code : codeLock.guestCode != code)
					return;

				var parentEntity = codeLock.GetParentEntity();
				var ownerId = codeLock.OwnerID.IsSteamId() ? codeLock.OwnerID :
					parentEntity != null ? parentEntity.OwnerID : 0;
				if (!ownerId.IsSteamId())
					return;

				UpdateAuthList(ownerId, AutoAuthType.All);
			});
		}

		#endregion

		private void UpdateAuthList(ulong playerID, AutoAuthType autoAuthType)
		{
			EntityEntry entityEntry;
			if (!_playerEntities.TryGetValue(playerID, out entityEntry)) return;

			switch (autoAuthType)
			{
				case AutoAuthType.All:
				{
					AuthToCupboard(entityEntry.BuildingPrivileges, playerID);

					AuthToTurret(entityEntry.AutoTurrets, playerID);
					break;
				}

				case AutoAuthType.Turret:
				{
					AuthToTurret(entityEntry.AutoTurrets, playerID);
					break;
				}

				case AutoAuthType.Cupboard:
				{
					AuthToCupboard(entityEntry.BuildingPrivileges, playerID);
					break;
				}
			}

			AuthToCodeLock(entityEntry.CodeLocks, playerID);
		}

		private void AuthToCodeLock(HashSet<CodeLock> codeLocks, ulong playerID)
		{
			if (codeLocks.Count <= 0) return;

			var data = PlayerData.GetOrLoad(playerID.ToString());
			if (data == null) return;

			var authList = GetPlayerNameIDs(playerID, AutoAuthType.All);

			foreach (var codeLock in codeLocks)
			{
				if (codeLock == null || codeLock.IsDestroyed) continue;

				codeLock.whitelistPlayers.Clear();
				codeLock.whitelistPlayers.Add(playerID);

				var parentEntity = codeLock.GetParentEntity();
				if (parentEntity != null && !IsBankBox(parentEntity))
				{
					codeLock.guestPlayers.Clear();

					foreach (var friend in authList)
					{
						var friendData = data.GetFriend(friend.userid);
						if (friendData != null)
						{
							var friendDataDoors = (parentEntity is Door && friendData.Doors);
							var friendDataCupboard = (parentEntity is BuildingPrivlidge && friendData.Cupboard);
							var friendDataContainers = (parentEntity is StorageContainer &&
							                            !parentEntity.ShortPrefabName.Contains("cupboard") &&
							                            friendData.Containers);

							if (friendDataDoors ||
							    friendDataCupboard ||
							    friendDataContainers)
							{
								codeLock.guestPlayers.Add(friend.userid);
							}
						}
					}
				}

				codeLock.SendNetworkUpdate();
			}
		}

		private void AuthToCupboard(HashSet<BuildingPrivlidge> cupboards, ulong playerID)
		{
			if (cupboards.Count <= 0) return;
			var authList = GetPlayerNameIDs(playerID, AutoAuthType.Cupboard);

			foreach (var buildingPrivilege in cupboards.Where(buildingPrivilege =>
				         buildingPrivilege != null && !buildingPrivilege.IsDestroyed))
			{
				buildingPrivilege.authorizedPlayers.Clear();

				foreach (var friend in authList)
					buildingPrivilege.authorizedPlayers.Add(friend);

				buildingPrivilege.SendNetworkUpdateImmediate();
			}
		}

		private void AuthToTurret(HashSet<AutoTurret> autoTurrets, ulong playerID)
		{
			if (autoTurrets.Count <= 0) return;
			var authList = GetPlayerNameIDs(playerID, AutoAuthType.Turret);

			foreach (var autoTurret in autoTurrets)
			{
				if (autoTurret == null || autoTurret.IsDestroyed) continue;
				var isOnline = false;
				if (autoTurret.IsOnline())
				{
					autoTurret.SetIsOnline(false);
					isOnline = true;
				}

				autoTurret.authorizedPlayers.Clear();

				foreach (var friend in authList) autoTurret.authorizedPlayers.Add(friend);

				if (isOnline) autoTurret.SetIsOnline(true);
				autoTurret.SendNetworkUpdateImmediate();
			}
		}

		private List<PlayerNameID> GetPlayerNameIDs(ulong playerId, AutoAuthType autoAuthType)
		{
			var playerNameIDs = new List<PlayerNameID>();
			var authList = GetAuthList(playerId, autoAuthType);

			playerNameIDs.AddRange(authList.Select(auth => new PlayerNameID
			{
				userid = auth, username = covalence.Players.FindPlayer(auth.ToString())?.Name ?? string.Empty,
				ShouldPool = true
			}));

			return playerNameIDs;
		}

		private HashSet<ulong> GetAuthList(ulong playerID, AutoAuthType autoAuthType)
		{
			var sharePlayers = new HashSet<ulong> {playerID};
			
			var data = PlayerData.GetOrLoad(playerID.ToString());
			if (data == null) return sharePlayers;

			data.Friends?.ForEach(friend =>
			{
				if (autoAuthType == AutoAuthType.All ||
				    autoAuthType == AutoAuthType.Turret ? friend.Turrets :
				    autoAuthType == AutoAuthType.Cupboard ? friend.Cupboard :
				    autoAuthType == AutoAuthType.Doors ? friend.Doors :
				    autoAuthType == AutoAuthType.Containers && friend.Containers)
				{
					sharePlayers.Add(friend.UserId);
				}
			});

			return sharePlayers;
		}

		#endregion

		#region Utils

		#region Convert

		private Coroutine _friendsConvertation;

		#region Commands

		[ConsoleCommand("friends.convert")]
		private void CmdConsoleConvert(ConsoleSystem.Arg arg)
		{
			if (!arg.IsServerside) return;

			var data = LoadOldPluginData();
			if (data == null) return;

			_friendsConvertation = Rust.Global.Runner.StartCoroutine(StartConvertOldData(data.Players.ToArray(),
				() => { Puts($"{data.Players.Count} players was converted!"); }));
		}

		private OldPluginClasses.PluginData LoadOldPluginData()
		{
			OldPluginClasses.PluginData data = null;

			try
			{
				data = Interface.Oxide.DataFileSystem.ReadObject<OldPluginClasses.PluginData>(Name);
			}
			catch (Exception e)
			{
				Interface.Oxide.LogError("There was an error reading plugin data: {0}", e.Message);
			}

			return data;
		}

		private IEnumerator StartConvertOldData(KeyValuePair<ulong, OldPluginClasses.PluginData.PlayerData>[]
				players,
			Action onFinish = null)
		{
			for (var i = 0; i < players.Length; i++)
			{
				var check = players[i];

				var data = PlayerData.GetOrCreate(check.Key.ToString());
				if (data == null) continue;

				data.DisplayName = check.Value.DisplayName;
				data.Friends = check.Value.Friends;
				data.UserID = check.Value.UserID;
				data.RemovedFriends = new HashSet<ulong>(check.Value.RemovedFriends);
				
				data.OnChanged();
				
				if (i % 10 == 0)
					yield return CoroutineEx.waitForFixedUpdate;
			}

			onFinish?.Invoke();

			_friendsConvertation = null;
		}

		#endregion

		#region Classes

		private class OldPluginClasses
		{
			public class PluginData
			{
				[JsonProperty(PropertyName = "Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<ulong, PlayerData> Players = new Dictionary<ulong, PlayerData>();

				public class PlayerData
				{
					[JsonProperty(PropertyName = "Display Name")]
					public string DisplayName;

					[JsonProperty(PropertyName = "UserId")]
					public ulong UserID;

					[JsonProperty(PropertyName = "Friends", ObjectCreationHandling = ObjectCreationHandling.Replace)]
					public List<FriendData> Friends = new List<FriendData>();

					[JsonProperty(PropertyName = "Removed Friends",
						ObjectCreationHandling = ObjectCreationHandling.Replace)]
					public List<ulong> RemovedFriends = new List<ulong>();
				}
			}
		}

		#endregion

		#endregion

		private bool IsBankBox(BaseNetworkable entity)
		{
			return Bank != null && Bank.Call<bool>("IsBankBox", entity);
		}

		#region Log

		private void Log(string filename, string text)
		{
			if (_config.LogToConsole) Puts(text);

			if (_config.LogToFile) LogToFile(filename, $"[{DateTime.Now}] {text}", this);
		}

		#endregion

		private readonly Regex _regex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");

		private void GetAvatar(ulong userId, Action<string> callback)
		{
			if (callback == null) return;

			webrequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1", null, (code, response) =>
			{
				if (code != 200 || response == null)
					return;

				var avatar = _regex.Match(response).Groups[1].ToString();
				if (string.IsNullOrEmpty(avatar))
					return;

				callback.Invoke(avatar);
			}, this);
		}

		private string GetPlayerName(ulong target)
		{
			var data = PlayerData.GetOrLoad(target.ToString());
			var result = data == null
				? covalence.Players.FindPlayerById(target.ToString())?.Name ?? "UNKNOWN"
				: data.DisplayName;

			return string.IsNullOrEmpty(result) ? "UNKNOWN" : result;
		}

		private void TimeHandle()
		{
			var toRemove = Pool.GetList<Invite>();

			_invites?.ForEach(invite =>
			{
				if (Time.time - invite.Cooldown >= 0)
				{
					RemoveNotify(invite.Target, invite.Guid);

					Notify(invite.Inviter, _config.Notify.Delay, Msg(NotifyTitle, invite.Inviter.UserIDString),
						Msg(TimeLose, invite.Inviter.UserIDString), _config.Notify.Image);

					Notify(invite.Target, _config.Notify.Delay, Msg(NotifyTitle, invite.Target.UserIDString),
						Msg(TargetTimeLose, invite.Target.UserIDString, invite.Inviter.displayName),
						_config.Notify.Image);

					toRemove.Add(invite);
				}
			});

			toRemove.ForEach(invite =>
			{
				if (_config.AddTeam)
					if (invite.Inviter != null && invite.Target != null)
					{
						var team = GetOrCreateTeam(invite.Inviter);
						if (team != null)
							if (!team.invites.Contains(invite.Target.userID))
								team.RejectInvite(invite.Target);
					}

				_invites.Remove(invite);
			});
			Pool.FreeList(ref toRemove);
		}

		private BasePlayer FindPlayer(List<FriendData> friendDatas)
		{
			return friendDatas.Select(friendData => FindPlayer(friendData.UserId))
				.FirstOrDefault(friend => friend != null);
		}

		private BasePlayer FindPlayer(ulong user)
		{
			foreach (var player in BasePlayer.activePlayerList)
				if (player.userID == user)
					return player;

			foreach (var player in BasePlayer.sleepingPlayerList)
				if (player.userID == user)
					return player;

			return null;
		}

		private List<BasePlayer> FindPlayer(string steamOrIdOrName)
		{
			var result = new List<BasePlayer>();

			foreach (var player in BasePlayer.activePlayerList)
				if (player.UserIDString == steamOrIdOrName ||
				    player.displayName.StartsWith(steamOrIdOrName, StringComparison.CurrentCultureIgnoreCase) ||
				    player.displayName.Contains(steamOrIdOrName))
					result.Add(player);

			return result;
		}

		private List<PlayerData> FindRemoveFriend(BasePlayer player, string friend)
		{
			var result = new List<PlayerData>();

			PlayerData.GetOrLoad(player.UserIDString)?.Friends?.ForEach(f =>
			{
				var fData = PlayerData.GetOrLoad(f.UserId.ToString());
				if (fData == null) return;

				if (f.UserId.ToString() == friend ||
				    (!string.IsNullOrEmpty(fData.DisplayName) &&
				     fData.DisplayName.StartsWith(friend, StringComparison.CurrentCultureIgnoreCase)))
					result.Add(fData);
			});

			
			
			return result;
		}

		private void RemoveFriend(BasePlayer player, ulong friend)
		{
			var data = PlayerData.GetOrLoad(player.UserIDString);
			if (data == null) return;
			
			var fData = PlayerData.GetOrLoad(friend.ToString());
			if (fData == null)
			{
				return;
			}

			var target = BasePlayer.FindByID(friend);

			var name = target != null ? target.displayName : $"{friend}";

			if (!data.Friends.Exists(x => x.UserId == friend))
			{
				Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
					Msg(IsNotFriend, player.UserIDString, name), _config.Notify.Image);
				return;
			}

			data.RemoveFriend(friend);
			fData.RemoveFriend(player.userID);

			if (_config.AddTeam) player.Team?.RemovePlayer(friend);

			if (_config.UseTeams)
				GetTeamList(player.userID)?.ForEach(member =>
				{
					PlayerData.GetOrLoad(member.ToString())?.RemoveFriend(friend);
					fData.RemoveFriend(member);
				});

			Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
				Msg(FriendRemoved, player.UserIDString, name), _config.Notify.Image);

			if (target != null)
				Notify(target, _config.Notify.Delay, Msg(NotifyTitle, target.UserIDString),
					Msg(FriendRemoved, target.UserIDString, player.displayName), _config.Notify.Image);

			NextTick(() =>
			{
				UpdateAuthList(player.userID, AutoAuthType.All);

				UpdateAuthList(friend, AutoAuthType.All);
			});
		}

		private RelationshipManager.PlayerTeam GetOrCreateTeam(BasePlayer player)
		{
			var team = RelationshipManager.ServerInstance.FindPlayersTeam(player.userID);
			if (team == null)
			{
				team = RelationshipManager.ServerInstance.CreateTeam();
				team.AddPlayer(player);
				team.SetTeamLeader(player.userID);
			}

			return team;
		}

		#region Invites

		private class Invite
		{
			public BasePlayer Inviter;
			public BasePlayer Target;
			public string Guid;
			public float Cooldown;

			public Invite(BasePlayer inviter, BasePlayer target, string guid)
			{
				Inviter = inviter;
				Target = target;
				Guid = guid;
				Cooldown = Time.time + _config.FriendInvite.Delay;
			}
		}

		private readonly List<Invite> _invites = new List<Invite>();

		private void SendInvite(BasePlayer inviter, BasePlayer target)
		{
			if (inviter == null || target == null || !inviter.userID.IsSteamId() || !target.userID.IsSteamId()) return;

			var data = PlayerData.GetOrLoad(inviter.UserIDString);
			if (data == null) return;

			if (data.Friends.Count >= _config.MaxFriendsAmount)
			{
				Notify(inviter, _config.Notify.Delay, Msg(NotifyTitle, inviter.UserIDString),
					Msg(TargetMaxFriends, inviter.UserIDString), _config.Notify.Image);
				return;
			}

			if (_invites.Exists(x => x.Inviter == inviter && x.Target == target))
			{
				Notify(inviter, _config.Notify.Delay, Msg(NotifyTitle, inviter.UserIDString),
					Msg(AlreadyPending, inviter.UserIDString), _config.Notify.Image);
				return;
			}

			if (_invites.Exists(x => x.Inviter == target || x.Target == target))
			{
				Notify(inviter, _config.Notify.Delay, Msg(NotifyTitle, inviter.UserIDString),
					Msg(PendingBusy, inviter.UserIDString), _config.Notify.Image);
				return;
			}

			var guid = string.Empty;
			if (Notifications)
			{
				var container = new CuiElementContainer();
				_config.FriendInvite.Buttons.ForEach(btn => btn.Get(ref container, target));

				guid = (string) Notifications.Call("ShowNotify",
					target,
					_config.FriendInvite.Delay,
					Msg(NotifyTitle, target.UserIDString),
					Msg(Pending, target.UserIDString, inviter.displayName),
					_config.FriendInvite.Image,
					container);

				Notify(inviter, _config.Notify.Delay, Msg(NotifyTitle, inviter.UserIDString),
					Msg(PendingSuccessSend, inviter.UserIDString), _config.Notify.Image);
			}
			else
			{
				Reply(target, Pending, inviter.displayName);
				Reply(inviter, PendingSuccessSend);
			}

			if (_config.AddTeam)
			{
				var team = GetOrCreateTeam(inviter);
				if (team != null)
					if (!team.invites.Contains(target.userID))
						team.SendInvite(target);
			}

			_invites.Add(new Invite(inviter, target, guid));
		}

		private void AcceptInvite(BasePlayer target)
		{
			var invite = _invites.Find(x => x.Target == target);
			if (invite == null)
			{
				Notify(target, _config.Notify.Delay, Msg(NotifyTitle, target.UserIDString),
					Msg(PendingNotFound, target.UserIDString), _config.Notify.Image);
				return;
			}

			var inviter = invite.Inviter;
			if (inviter == null) return;

			var iData = PlayerData.GetOrLoad(inviter.UserIDString);
			if (iData == null) return;

			var tData = PlayerData.GetOrLoad(target.UserIDString);
			if (tData == null) return;

			if (target.userID == inviter.userID)
			{
				Notify(target, _config.Notify.Delay, Msg(NotifyTitle, target.UserIDString),
					Msg(CantAddSelf, target.UserIDString), _config.Notify.Image);
				return;
			}

			if (_config.UseTeams)
			{
				GetTeamList(inviter.userID)?.ForEach(player =>
				{
					PlayerData.GetOrLoad(player.ToString())?.AddFriend(target.userID);
					tData.AddFriend(player);

					Log("friends", $"Player '{player}' added '{target.userID}' as a friend");
				});
			}
			else
			{
				PlayerData.GetOrLoad(inviter.UserIDString)?.AddFriend(target.userID);
				tData.AddFriend(inviter.userID);

				Log("friends", $"Player '{inviter.userID}' added '{target.userID}' as a friend");
			}

			RemoveNotify(invite.Target, invite.Guid);

			Notify(target, _config.Notify.Delay, Msg(NotifyTitle, target.UserIDString),
				Msg(FriendAdded, target.UserIDString, inviter.displayName), _config.Notify.Image);

			Notify(inviter, _config.Notify.Delay, Msg(NotifyTitle, inviter.UserIDString),
				Msg(FriendAdded, inviter.UserIDString, target.displayName), _config.Notify.Image);

			_invites.Remove(invite);

			if (_config.AddTeam)
			{
				target.Team?.RemovePlayer(target.userID);

				if (inviter.Team == null || inviter.Team.teamID == 0)
				{
					var team = RelationshipManager.ServerInstance.CreateTeam();
					team.AddPlayer(inviter);
					team.SetTeamLeader(inviter.userID);
					team.AddPlayer(target);
				}
				else
				{
					inviter.Team.AddPlayer(target);
				}
			}

			NextTick(() =>
			{
				if (_config.AddTeam && inviter.Team != null) UpdateTeamAuthList(GetTeamList(inviter.userID));
			});
		}

		private List<ulong> GetTeamList(ulong user)
		{
			var result = new List<ulong> {user};

			PlayerData.GetOrLoad(user.ToString())?.Friends.ForEach(friend =>
			{
				if (!result.Contains(friend.UserId))
					result.Add(friend.UserId);
			});

			return result;
		}

		private void CancelInvite(BasePlayer player)
		{
			var invite = _invites.Find(x => x.Target == player);
			if (invite == null)
			{
				Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
					Msg(PendingNotFound, player.UserIDString), _config.Notify.Image);
				return;
			}

			Notify(invite.Inviter, _config.Notify.Delay, Msg(NotifyTitle, invite.Inviter.UserIDString),
				Msg(InviterFriendCancel, invite.Inviter.UserIDString, player.displayName), _config.Notify.Image);

			Notify(player, _config.Notify.Delay, Msg(NotifyTitle, player.UserIDString),
				Msg(FriendCancel, player.UserIDString, invite.Inviter.displayName), _config.Notify.Image);

			RemoveNotify(invite.Target, invite.Guid);

			_invites.Remove(invite);
		}

		#endregion

		#endregion

		#region Lang

		private const string
			UITitle = "UITitle",
			Online = "Online",
			Offline = "Offline",
			NotifyTitle = "InviteTitle",
			Pending = "Pending",
			AlreadyPending = "AlreadyPending",
			PendingBusy = "PendingBusy",
			PendingSuccessSend = "PendingSuccessSend",
			PendingNotFound = "PendingNotFound",
			AlreadyFriends = "AlreadyFriends",
			CantAddSelf = "CantAddSelf",
			MaxFriends = "MaxFriends",
			TargetMaxFriends = "TargetMaxFriends",
			TimeLose = "TimeLose",
			TargetTimeLose = "TargetTimeLose",
			FriendAdded = "FriendAdded",
			FriendCancel = "FriendCancel",
			InviterFriendCancel = "InviterFriendCancel",
			NoFriends = "NoFriends",
			FriendList = "FriendList",
			IsNotFriend = "IsNotFriend",
			FriendRemoved = "FriendRemoved",
			NotFound = "NotFound",
			FF = "FF",
			FFOn = "FFOn",
			FFOff = "FFOff",
			FFErrorSyntax = "FFErrorSyntax",
			ErrorSyntax = "ErrorSyntax",
			SamsOn = "SamsOn",
			SamsOff = "SamsOff",
			DoorsOn = "DoorsOn",
			DoorsOff = "DoorsOff",
			TurretsOn = "TurretsOn",
			TurretsOff = "TurretsOff",
			CupboardOn = "CupboardOn",
			CupboardOff = "CupboardOff",
			ContainersOn = "ContainersOn",
			ContainersOff = "ContainersOff",
			Accept = "Accept",
			Cancel = "Cancel",
			FindFriend = "FindFriend",
			Help = "Help",
			MultipleFound = "MultipleFound";

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				[UITitle] = "FRIENDS SYSTEM",
				[Online] = "Online",
				[Offline] = "Offline",
				[NotifyTitle] = "Friend System",
				[Pending] = "{0} sent you a friend request",
				[AlreadyPending] =
					"Your previous friend request has not yet been answered by the player, please wait for a response!",
				[PendingBusy] =
					"The player already has a request from another player, please wait until the player responds to the other",
				[PendingSuccessSend] = "Request sent successfully, please wait while it accepts it.",
				[PendingNotFound] = "You have no friend requests",
				[AlreadyFriends] = "You are already friends!",
				[CantAddSelf] = "You cannot add yourself as a friend.",
				[MaxFriends] = "You have reached the maximum number of friends",
				[TargetMaxFriends] = "The player has reached the maximum number of friends",
				[TimeLose] = "The player did not have time to accept the invitation!",
				[TargetTimeLose] = "You did not have time to accept the invitation from {0}",
				[FriendAdded] = "{0} has become your friend.",
				[FriendCancel] = "You gave up your friendship with {0}",
				[InviterFriendCancel] = "{0} refused to befriend you.",
				[NoFriends] = "You have no friends = (",
				[FriendList] = "Your friends list: {0}",
				[IsNotFriend] = "The player is not your friend",
				[FriendRemoved] = "You ended your friendship with {0}",
				[NotFound] = "Player '{0}' not found!",
				[FF] = "Attention! {0} Your friend, you cannot hurt him. Enable/Disable damage on friends: /friend ff",
				[FFOn] = "You enabled Friends Damage!",
				[FFOff] = "You turned off damage to friends!",
				[FFErrorSyntax] = "Use: /{0} name/steamid [on/off]",
				[ErrorSyntax] = "Use: /{0} {1} name/steamid",
				[DoorsOn] = "You have enabled friends to access your doors!",
				[DoorsOff] = "You have disabled friends' access to your doors!",
				[TurretsOn] = "You have enabled the authorization of friends in turrets",
				[TurretsOff] = "You have disabled the authorization of friends in turrets",
				[SamsOn] = "You have enabled the authorization of friends in SAMs",
				[SamsOff] = "You have disabled the authorization of friends in SAMs",
				[CupboardOn] = "You have enabled cupboard friends authorization",
				[CupboardOff] = "You have disabled cupboard friends authorization",
				[ContainersOn] = "You have enabled containers friends authorization",
				[ContainersOff] = "You have disabled containers friends authorization",
				[Accept] = "Accept",
				[Cancel] = "Cancel",
				[FindFriend] = "FIND FRIEND",
				[Help] = "FRIEND HELP: /friend add|remove|accept|cancel|list|doors|cupboard|turrets",
				[MultipleFound] = "Found multiple players: {0}\nUse: /{1} {2} {3} [ID]"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				[UITitle] = "СИСТЕМА ДРУЗЕЙ",
				[Online] = "Онлайн",
				[Offline] = "Оффлайн",
				[NotifyTitle] = "Система друзей",
				[Pending] = "{0} отправил вам запрос в друзья",
				[AlreadyPending] = "На ваш предыдущий запрос дружбы игрок еще не ответил, ожидайте его ответа!",
				[PendingBusy] =
					"У игрока уже есть запрос от другого игрока, ожидайте пока игрок игрок не ответит другому",
				[PendingSuccessSend] = "Запрос успешно отправлен, ожидайте пока он примет его.",
				[PendingNotFound] = "К вам нет запросов в друзья",
				[AlreadyFriends] = "Вы уже являетесь друзьями!",
				[CantAddSelf] = "Вы не можете добавить себя в друзья.",
				[MaxFriends] = "У вас достигнуто максимальное количество друзей",
				[TargetMaxFriends] = "У игрока достигнуто максимальное количество друзей",
				[TimeLose] = "Игрок не успел принять приглашение!",
				[TargetTimeLose] = "Вы не успели принять приглашение от {0}",
				[FriendAdded] = "{0} стал Вашим другом.",
				[FriendCancel] = "Вы отказались от дружбы с {0}",
				[InviterFriendCancel] = "{0} отказался от дружбы с Вами.",
				[NoFriends] = "У Вас нет друзей =(",
				[FriendList] = "Список ваших друзей: {0}",
				[IsNotFriend] = "Игрок '{0}' не является Вашим другом",
				[FriendRemoved] = "Вы прекратили дружбу с {0}",
				[NotFound] = "Игрок '{0}' не найден!",
				[FF] =
					"Внимание! {0} Ваш друг, вы не можете его ранить. Включение/Отключение урона по друзьям /friend ff",
				[FFOn] = "Вы включили урон по друзьям!",
				[FFOff] = "Вы выключили урон по друзьям!",
				[FFErrorSyntax] = "Используйте: /{0} name/steamid [on/off]",
				[ErrorSyntax] = "Используйте: /{0} {1} name/steamid",
				[DoorsOn] = "Вы включили доступ друзей к вашим дверям!",
				[DoorsOff] = "Вы выключили доступ друзей к вашим дверям!",
				[TurretsOn] = "Вы включили авторизацию друзей в турелях",
				[TurretsOff] = "Вы выключили авторизацию друзей в турелях",
				[SamsOn] = "Вы включили авторизацию друзей в ПВО",
				[SamsOff] = "Вы выключили авторизацию друзей в ПВО",
				[CupboardOn] = "Вы включили авторизацию друзей в шкафах",
				[CupboardOff] = "Вы выключили авторизацию друзей в шкафах",
				[ContainersOn] = "Вы включили авторизацию друзей в ящиках",
				[ContainersOff] = "Вы выключили авторизацию друзей в ящиках",
				[Accept] = "Принять",
				[Cancel] = "Отклонить",
				[FindFriend] = "НАЙТИ ДРУГА",
				[Help] = "ПОМОЩЬ ПО ДРУЗЬЯМ: /friend add|remove|accept|cancel|list|doors|cupboard|turrets",
				[MultipleFound] = "Найдено несколько игроков: {0}\nИспользуйте: /{1} {2} {3} [ID]"
			}, this, "ru");
		}

		private void Notify(BasePlayer player, float delay, string title, string description, string image)
		{
			if (player == null) return;

			if (Notifications)
				Notifications.Call("ShowNotify", player, delay, title, description, image);
			else
				SendReply(player, description);

			var reply = "";
			if (reply == "14879")
			{
			}
		}

		private void RemoveNotify(BasePlayer player, string guid)
		{
			Notifications?.Call("RemoveNotify", player, guid);
		}

		private void Reply(BasePlayer player, string key, params object[] obj)
		{
			SendReply(player, Msg(key, player.UserIDString, obj));
		}

		private string Msg(string key, string userid = null, params object[] obj)
		{
			return string.Format(lang.GetMessage(key, this, userid), obj);
		}

		#endregion

		#region Cooldown

		private readonly Dictionary<ulong, float> _cooldown = new Dictionary<ulong, float>();

		private bool IsCd(ulong user)
		{
			return GetCd(user) >= 0;
		}

		private int GetCd(ulong user)
		{
			return _cooldown.ContainsKey(user) ? (int) (_cooldown[user] - Time.time) : -1;
		}

		private void SetCd(ulong user)
		{
			var time = Time.time + _config.FFDelay;
			if (_cooldown.ContainsKey(user))
				_cooldown[user] = time;
			else
				_cooldown.Add(user, time);
		}

		#endregion

		#region API

		private ulong[] GetFriends(string playerId)
		{
			return GetFriends(ulong.Parse(playerId));
		}

		private ulong[] GetFriends(ulong playerId)
		{
			return PlayerData.GetOrLoad(playerId.ToString())?.Friends.Select(x => x.UserId).ToArray();
		}

		private ulong[] GetFriendList(string playerId)
		{
			return GetFriendList(ulong.Parse(playerId));
		}

		private ulong[] GetFriendList(ulong playerId)
		{
			return GetFriends(playerId);
		}

		private bool AreFriends(string playerId, string friendId)
		{
			return AreFriends(ulong.Parse(playerId), ulong.Parse(friendId));
		}

		private bool AreFriends(ulong playerId, ulong friendId)
		{
			return PlayerData.GetOrLoad(playerId.ToString())?.IsFriend(friendId) == true;
		}

		private bool HasFriend(string playerId, string friendId)
		{
			return HasFriend(ulong.Parse(playerId), ulong.Parse(friendId));
		}

		private bool HasFriend(ulong playerId, ulong friendId)
		{
			return AreFriends(playerId, friendId);
		}

		private bool HasFriends(string playerId, string friendId)
		{
			return AreFriends(ulong.Parse(playerId), ulong.Parse(friendId));
		}

		private bool HasFriends(ulong playerId, ulong friendId)
		{
			return AreFriends(playerId, friendId);
		}

		private bool IsFriend(string playerId, string friendId)
		{
			return IsFriend(ulong.Parse(playerId), ulong.Parse(friendId));
		}

		private bool IsFriend(ulong playerId, ulong friendId)
		{
			return AreFriends(playerId, friendId);
		}

		private bool WasFriend(string playerId, string friendId)
		{
			return WasFriend(ulong.Parse(playerId), ulong.Parse(friendId));
		}

		private bool WasFriend(ulong playerId, ulong friendId)
		{
			return PlayerData.GetOrLoad(playerId.ToString())?.RemovedFriends.Contains(friendId) == true;
		}

		private int GetMaxFriends()
		{
			return _config.MaxFriendsAmount;
		}

		#endregion
	}
}