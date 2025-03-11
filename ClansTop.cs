using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Clans Top", "Mevent", "1.0.1")]
	public class ClansTop : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin
			Clans = null;

		private static ClansTop _instance;

		private const string Layer = "UI.TopClans";

		private string _topJson;

		#endregion

		#region Config

		private Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Interface Settings")]
			public InterfaceSettings UI = new InterfaceSettings
			{
				DisplayType = "Overlay",
				MaxClansOnString = 3,
				Colors = new List<IColor>
				{
					new IColor("#FF6060"),
					new IColor("#4B68FF"),
					new IColor("#FFD01B")
				},
				BackgroundColor = new IColor("#000000", 80),
				BottomIndent = 0f,
				SideIndent = 0f,
				Width = 80,
				Height = 17.5f,
				Margin = 15,
				NumberSize = 12,
				TextSize = 12,
				TextAlign = TextAnchor.MiddleCenter,
				ShowScore = false,
				ScoreFormat = " ({0})",
				ValueAbbreviation = true
			};
		}

		private class InterfaceSettings
		{
			[JsonProperty(PropertyName = "Display type (Overlay/Hud)")]
			public string DisplayType;

			[JsonProperty(PropertyName = "Max clans on string")]
			public int MaxClansOnString;

			[JsonProperty(PropertyName = "Colors", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<IColor> Colors = new List<IColor>();

			[JsonProperty(PropertyName = "Background Color")]
			public IColor BackgroundColor;

			[JsonProperty(PropertyName = "Bottom Indent")]
			public float BottomIndent;

			[JsonProperty(PropertyName = "Side Indent")]
			public float SideIndent;

			[JsonProperty(PropertyName = "Width")] public float Width;

			[JsonProperty(PropertyName = "Height")]
			public float Height;

			[JsonProperty(PropertyName = "Margin")]
			public float Margin;

			[JsonProperty(PropertyName = "Number Text Size")]
			public int NumberSize;

			[JsonProperty(PropertyName = "Text Size")]
			public int TextSize;

			[JsonProperty(PropertyName = "TextAlign")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor TextAlign;

			[JsonProperty(PropertyName = "Show Score")]
			public bool ShowScore;

			[JsonProperty(PropertyName = "Score Format")]
			public string ScoreFormat;

			[JsonProperty(PropertyName = "Use value abbreviation?")]
			public bool ValueAbbreviation;

			public string GetScore(string clanTag)
			{
				return string.Format(ScoreFormat, _instance.GetValue(_instance.CLANS_GetClanScore(clanTag)));
			}
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string Hex;

			[JsonProperty(PropertyName = "Opacity (0 - 100)")]
			public readonly float Alpha;

			[JsonIgnore] private string _color;

			[JsonIgnore]
			public string Get
			{
				get
				{
					if (string.IsNullOrEmpty(_color))
						_color = GetColor();

					return _color;
				}
			}

			private string GetColor()
			{
				if (string.IsNullOrEmpty(Hex)) Hex = "#FFFFFF";

				var str = Hex.Trim('#');
				if (str.Length != 6) throw new Exception(Hex);
				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

				return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
			}

			public IColor()
			{
			}

			public IColor(string hex, float alpha = 100)
			{
				Hex = hex;
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

		#region Hooks

		private void OnServerInitialized()
		{
			_instance = this;

			foreach (var player in BasePlayer.activePlayerList)
				OnPlayerConnected(player);
		}

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);

			_instance = null;
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			StartUpdate(player);
		}

		private void OnClanTopUpdated()
		{
			NextTick(StartUpdate);
		}

		#endregion

		#region Interface

		private void UpdateUI(string json)
		{
			foreach (var player in BasePlayer.activePlayerList)
				UpdateUI(player, json);
		}

		private void UpdateUI(BasePlayer player, string json)
		{
			if (string.IsNullOrEmpty(json)) return;

			CuiHelper.DestroyUi(player, Layer);

			CuiHelper.AddUi(player, json);
		}

		private string GenerateTopUI(IReadOnlyDictionary<int, string> clans)
		{
			var container = new CuiElementContainer();

			if (clans.Count > 0)
			{
				#region Background

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = $"{_config.UI.SideIndent} {_config.UI.BottomIndent}",
						OffsetMax = $"{_config.UI.SideIndent} {_config.UI.BottomIndent + _config.UI.Height}"
					},
					Image = {Color = "0 0 0 0"}
				}, _config.UI.DisplayType, Layer);

				#endregion

				var count = Mathf.Min(_config.UI.MaxClansOnString, clans.Count);

				var xSwitch = -((count * _config.UI.Width +
				                 (count - 1) * _config.UI.Margin) / 2f);

				for (var i = 1; i <= count; i++)
				{
					string clanTag;
					if (clans.TryGetValue(i, out clanTag) == false) continue;

					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = $"{xSwitch} 0",
							OffsetMax = $"{xSwitch + _config.UI.Width} 0"
						},
						Image =
						{
							Color = "0 0 0 0"
						}
					}, Layer, Layer + $".Top.{i}");

					#region Number

					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 1",
							OffsetMin = "0 0",
							OffsetMax = $"{_config.UI.NumberSize} 0"
						},
						Text =
						{
							Text = $"{i}",
							Align = TextAnchor.MiddleLeft,
							Font = "robotocondensed-bold.ttf",
							FontSize = _config.UI.NumberSize,
							Color = "1 1 1 1"
						}
					}, Layer + $".Top.{i}");

					#endregion

					#region Panel

					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = $"{_config.UI.NumberSize} 0",
							OffsetMax = "0 0"
						},
						Image =
						{
							Color = _config.UI.BackgroundColor.Get
						}
					}, Layer + $".Top.{i}", Layer + $".Top.{i}.Panel");

					#region Line

					if (i - 1 < _config.UI.Colors.Count)
						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 0",
								OffsetMin = "0 0",
								OffsetMax = "0 1.5"
							},
							Image =
							{
								Color = _config.UI.Colors[i - 1].Get
							}
						}, Layer + $".Top.{i}.Panel");

					#endregion

					#region Name

					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "0 0",
							OffsetMax = "0 0"
						},
						Text =
						{
							Text =
								_config.UI.ShowScore ? $"{clanTag}{_config.UI.GetScore(clanTag)}" : $"{clanTag}",
							Align = _config.UI.TextAlign,
							Font = "robotocondensed-regular.ttf",
							FontSize = _config.UI.TextSize
						}
					}, Layer + $".Top.{i}.Panel");

					#endregion

					#endregion

					xSwitch += _config.UI.Width + _config.UI.Margin;
				}
			}

			var json = CuiHelper.ToJson(container);
			return json;
		}

		#endregion

		#region Utils

		private string GetValue(float value)
		{
			if (!_config.UI.ValueAbbreviation)
				return Mathf.Round(value).ToString(CultureInfo.InvariantCulture);

			var t = string.Empty;
			while (value > 1000)
			{
				t += "K";
				value /= 1000;
			}

			return Mathf.Round(value) + t;
		}

		private Dictionary<int, string> CLANS_GetTopClans()
		{
			return Clans?.Call<Dictionary<int, string>>("GetTopClans") ?? new Dictionary<int, string>();
		}

		private float CLANS_GetClanScore(string clanTag)
		{
			return Convert.ToSingle(Clans?.Call("GetClanScores", clanTag));
		}

		private void StartUpdate()
		{
			UpdateTopJson();

			UpdateUI(_topJson);
		}

		private void StartUpdate(BasePlayer player)
		{
			if (string.IsNullOrEmpty(_topJson))
				UpdateTopJson();

			UpdateUI(player, _topJson);
		}

		private void UpdateTopJson()
		{
			_topJson = GenerateTopUI(CLANS_GetTopClans());
		}

		#endregion
	}
}