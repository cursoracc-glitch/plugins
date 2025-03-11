using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
	[Info("Notifications", "Sempai#3239", "1.0.1")]
	[Description("Adds an notification system")]
	public class Notifications : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin ImageLibrary;

		private static Notifications _instance;

		private const string Layer = "Com.Mevent.Notification";

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Настройка интерфейса | Interface Settings")]
			public IBackground Background = new IBackground
			{
				AnchorMin = "1 0",
				AnchorMax = "1 0",
				Height = 105,
				Width = 300,
				Margin = 5,
				Color = new IColor("#28253C", 98),
				Icon = new InterfacePosition
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "10 -30", OffsetMax = "30 -10"
				},
				Title = new IText
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "-100 -35", OffsetMax = "100 -5",
					FontSize = 18,
					Font = "robotocondensed-bold.ttf",
					Align = TextAnchor.MiddleCenter,
					Color = new IColor("#FFFFFF", 100)
				},
				Description = new IText
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-140 20", OffsetMax = "140 75",
					FontSize = 16,
					Font = "robotocondensed-regular.ttf",
					Align = TextAnchor.MiddleCenter,
					Color = new IColor("#4D4A62", 100)
				},
				Close = new IText
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-25 -25", OffsetMax = "-5 -5",
					FontSize = 16,
					Font = "robotocondensed-bold.ttf",
					Align = TextAnchor.MiddleCenter,
					Color = new IColor("#4D4A62", 100)
				}
			};
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
						Text = text, Align = Align, FontSize = FontSize, Color = Color.Get(),
						Font = Font
					}
				}, parent, name);
			}
		}


		private abstract class IAnchors
		{
			public string AnchorMin;

			public string AnchorMax;
		}

		private class IBackground : IAnchors
		{
			[JsonProperty(PropertyName = "Высота | Height")]
			public float Height;

			[JsonProperty(PropertyName = "Ширина | Width")]
			public float Width;

			[JsonProperty(PropertyName = "Отступ | Margin")]
			public float Margin;

			[JsonProperty(PropertyName = "Цвет | Color")]
			public IColor Color;

			[JsonProperty(PropertyName = "Иконка | Icon")]
			public InterfacePosition Icon;

			[JsonProperty(PropertyName = "Заглавие | Title")]
			public IText Title;

			[JsonProperty(PropertyName = "Описание | Description")]
			public IText Description;

			[JsonProperty(PropertyName = "Закрыть | Close")]
			public IText Close;
		}

		private class InterfacePosition : IAnchors
		{
			public string OffsetMin;

			public string OffsetMax;
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
				SaveConfig();
			}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
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

		private PluginData _data;

		private void SaveData()
		{
			Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
		}

		private void LoadData()
		{
			try
			{
				_data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_data == null) _data = new PluginData();
		}

		private class PluginData
		{
			[JsonProperty(PropertyName = "Image List", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, string> Images = new Dictionary<string, string>
			{
				["warning"] = "https://i.imgur.com/p3tKXJV.png"
			};
		}

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;
			LoadData();
		}

		private void OnServerInitialized()
		{
			if (!ImageLibrary)
				PrintWarning("IMAGE LIBRARY IS NOT INSTALLED.");
			else
				foreach (var image in _data.Images)
					ImageLibrary.Call("AddImage", image.Value, image.Key);
		}

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);

			SaveData();

			_instance = null;
			_config = null;
		}

		#endregion

		#region Commands

		[ConsoleCommand("UI_Notifications")]
		private void CmdConsoleNotify(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "remove":
				{
					int index;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out index)) return;
					
					player.GetComponent<NotifyComponent>()?.RemoveByIndex(index);
					break;
				}
			}
		}

		#endregion

		#region Component

		private class UiNotify
		{
			public readonly string GUID;
			
			public float TimeLeft;

			public readonly string Title;

			public readonly string Description;

			public readonly string Image;

			public readonly CuiElementContainer Container;

			public UiNotify(string guid, float delay, string title, string description, string image, CuiElementContainer cont = null)
			{
				GUID = guid;
				TimeLeft = delay;
				Title = title;
				Description = description;
				Image = image;
				
				Container = cont ?? new CuiElementContainer();
			}
		}

		private class NotifyComponent : FacepunchBehaviour
		{
			private BasePlayer _player;

			private readonly List<UiNotify> _notifies = new List<UiNotify>();

			private const float _timer = 0.1f;

			private void Awake()
			{
				_player = GetComponent<BasePlayer>();

				Invoke(TimeHandle, _timer);
			}

			private void TimeHandle()
			{
				CancelInvoke();
				if (_player == null)
				{
					Kill();
					return;
				}

				var toRemove = new List<UiNotify>();

				_notifies.ForEach(notify =>
				{
					notify.TimeLeft -= _timer;
					if (notify.TimeLeft <= 0) toRemove.Add(notify);
				});

				if (toRemove.Count > 0)
				{
					toRemove.ForEach(notify =>
					{
						Interface.Oxide.CallHook("OnNotifyRemove", _player, notify.GUID);
						_notifies.Remove(notify);
					});

					if (_notifies.Count <= 0)
						Kill();
					else
						RefreshUi();
				}

				if (_notifies.Count > 0)
				{
					Invoke(TimeHandle, _timer);
				}
			}

			private void RefreshUi()
			{
				if (_player == null)
				{
					Kill();
					return;
				}

				var container = new CuiElementContainer();

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = _config.Background.AnchorMin,
						AnchorMax = _config.Background.AnchorMax
					},
					Image = {Color = "0 0 0 0"}
				}, "Overlay", Layer);

				var ySwitch = _config.Background.Margin;
				for (var i = 0; i < _notifies.Count; i++)
				{
					var notify = _notifies[i];

					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0",
							AnchorMax = "1 1",
							OffsetMin =
								$"-{_config.Background.Width + _config.Background.Margin} {ySwitch}",
							OffsetMax =
								$"-{_config.Background.Margin} {ySwitch + _config.Background.Height}"
						},
						Image =
						{
							Color = _config.Background.Color.Get()
						}
					}, Layer, Layer + $".Notify.{i}");

					if (!notify.Image.IsNullOrEmpty() && _instance._data.Images.ContainsKey(notify.Image))
					{
						container.Add(new CuiElement
						{
							Parent = Layer + $".Notify.{i}",
							Components =
							{
								new CuiRawImageComponent
								{
									Png = _instance.ImageLibrary.Call<string>("GetImage", notify.Image)
								},
								new CuiRectTransformComponent
								{
									AnchorMin = _config.Background.Icon.AnchorMin,
									AnchorMax = _config.Background.Icon.AnchorMax,
									OffsetMin = _config.Background.Icon.OffsetMin,
									OffsetMax = _config.Background.Icon.OffsetMax
								}
							}
						});}

					_config.Background.Title.Get(ref container, Layer + $".Notify.{i}", null, notify.Title);

					_config.Background.Description.Get(ref container, Layer + $".Notify.{i}", null, notify.Description);

					_config.Background.Close.Get(ref container, Layer + $".Notify.{i}", Layer + $".Notify.{i}.Close",
						"✕");

					container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Text = {Text = ""},
						Button =
						{
							Color = "0 0 0 0",
							Command = $"UI_Notifications remove {_notifies.IndexOf(notify)}"
						}
					}, Layer + $".Notify.{i}.Close");
					
					foreach (var element in notify.Container)
					{
						if (element.Parent.IsNullOrEmpty()) 
							element.Parent = Layer + $".Notify.{i}";
						
						container.Add(element);
					}
					
					ySwitch += _config.Background.Height + _config.Background.Margin;
				}
				
				
				CuiHelper.DestroyUi(_player, Layer);
				CuiHelper.AddUi(_player, container);
			}

			public void RemoveByGuid(string guid)
			{
				_notifies.ToList().ForEach(notify =>
				{
					if (notify.GUID == guid && Interface.Oxide.CallHook("OnNotifyRemove", _player, notify.GUID) == null)
					{
						_notifies.Remove(notify);
					}
				});
				
				RefreshUi();
			}
			
			public void RemoveByIndex(int index)
			{
				if (index < 0 || _notifies.Count <= index) return;

				var notify = _notifies[index];

				if (Interface.Oxide.CallHook("OnNotifyRemove", _player, notify.GUID) == null) 
					_notifies.Remove(notify);

				RefreshUi();
			}

			public void AddNotify(UiNotify notyify)
			{
				_notifies.Add(notyify);

				if (!IsInvoking(TimeHandle))
					Invoke(TimeHandle, _timer);
				
				RefreshUi();
			}

			private void OnDestroy()
			{
				CancelInvoke();

				if (_player != null)
					CuiHelper.DestroyUi(_player, Layer);

				Destroy(this);
			}

			public void Kill()
			{
				Destroy(this);
			}
		}

		#endregion

		#region API

		private void AddImage(string image, string url)
		{
			if (_data.Images.ContainsKey(image))
				_data.Images[image] = url;
			else
				_data.Images.Add(image, url);

			ImageLibrary.Call("AddImage", url, image);
		}

		private void ShowNotify(ulong user, float delay, string title, string description, string image, CuiElementContainer container = null)
		{
			ShowNotify(BasePlayer.FindByID(user), delay, title, description, image, container);
		}

		private void RemoveNotify(BasePlayer player, string guid)
		{
			if (player == null) return;
			
			var notify = player.GetComponent<NotifyComponent>() ?? player.gameObject.AddComponent<NotifyComponent>();
			if (notify == null) return;

			notify.RemoveByGuid(guid);
		}
		
		private string ShowNotify(BasePlayer player, float delay, string title, string description, string image, CuiElementContainer container = null)
		{
			if (player == null) return string.Empty; 
			
			var notify = player.GetComponent<NotifyComponent>() ?? player.gameObject.AddComponent<NotifyComponent>();
			if (notify == null) return string.Empty;

			var uiNotify = new UiNotify(CuiHelper.GetGuid(), delay, title, description, image, container);
			notify.AddNotify(uiNotify);
			return uiNotify.GUID;
		}
		
		#endregion
	}
}