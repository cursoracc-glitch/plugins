using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("MenuAlerts", "King", "1.0.5")]
    public class MenuAlerts : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin ImageLibrary = null;
        private static MenuAlerts plugin;
        private const String Layer = "MenuAlerts_UI";
        #endregion

        #region [ImageLibrary]
        private Boolean HasImage(String imageName, ulong imageId = 0) => (Boolean)ImageLibrary.Call("HasImage", imageName, imageId);
        private Boolean AddImage(String url, String shortname, ulong skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private String GetImage(String shortname, ulong skin = 0) => (String)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [MenuAlerts Data]
        private readonly Dictionary<BasePlayer, MenuAlertsComponent> _MenuAlerts = new Dictionary<BasePlayer, MenuAlertsComponent>();

        private class AlertsData
        {
            public Int32 _startTime;
            public Int32 _cooldown;
            
            public String _titleUI;
            public String _textUI;

            public Boolean _isLarge;
            public Boolean _isOpen;
            public String _imageUI;
            public String _pluginName;
        }
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            plugin = this;
        }

        private void Unload()
        {
            _MenuAlerts.Values.ToList().ForEach(MenuAlerts =>
            {
                if (MenuAlerts != null)
                    MenuAlerts.Kill();
            });

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);

            plugin = null;
        }
        #endregion

        #region [ConsoleCommand]
		[ConsoleCommand("MenuAlerts_UI")]
		private void CmdConsoleNotify(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg?.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "openMenu":
				{
                    MenuAlertsComponent menuAlert = GetComponent(player);
                    if (menuAlert == null) return;

                    AlertsData find = menuAlert._alertsData.FirstOrDefault(p => p._pluginName == arg.Args[1]);
                    if (find == null || find._isOpen) return;

                    find._isOpen = true;
                    menuAlert.MainUi();
					break;
				}
                case "closeMenu":
                {
                    MenuAlertsComponent menuAlert = GetComponent(player);
                    if (menuAlert == null) return;

                    AlertsData find = menuAlert._alertsData.FirstOrDefault(p => p._pluginName == arg.Args[1]);
                    if (find == null || !find._isOpen) return;
            
                    find._isOpen = false;
                    menuAlert.MainUi();
                    break;
                }
			}
		}
        #endregion

        #region [Component]
        private MenuAlertsComponent GetComponent(BasePlayer player)
        {
            MenuAlertsComponent component;
            return _MenuAlerts.TryGetValue(player, out component)
                ? component
                : (player.gameObject.AddComponent<MenuAlertsComponent>());
        }

        private class MenuAlertsComponent : FacepunchBehaviour
        {
            #region [Fields]
            private BasePlayer _player;
            public readonly List<AlertsData> _alertsData = new List<AlertsData>();

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();

                plugin._MenuAlerts[_player] = this;

                Invoke(MenuAlertController, 1);
            }

            private void MenuAlertController()
            {
                CancelInvoke(MenuAlertController);
                if (_alertsData.Count == 0 || !_player.IsConnected)
                {
                    Kill();
                    return;
                }

                List<AlertsData> result = Pool.GetList<AlertsData>();

				_alertsData.ForEach(key =>
				{
                    if (Facepunch.Math.Epoch.Current - key._startTime >= key._cooldown)
                    {
                        result.Add(key);
                    }
                    else
                    {
					    CuiElementContainer container = new CuiElementContainer();
					    MenuLine(ref container, key);
                        if (key._isOpen)
                        {
                            TextUi(ref container, key);
                        }
					    CuiHelper.AddUi(_player, container);
                    }
				});


                if (result.Count > 0)
                {
                    foreach (AlertsData key in result)
                    {
                        RemoveMenu(_alertsData.IndexOf(key));
                    }
                }

                Pool.FreeList(ref result);
                Invoke(MenuAlertController, 1);
            }
            #endregion

            #region [Destroy]
            private void OnDestroy()
            {
                CancelInvoke();

                CuiHelper.DestroyUi(_player, Layer);

                plugin?._MenuAlerts.Remove(_player);
            }

            public void Kill()
            {
                DestroyImmediate(this);
            }
            #endregion

            #region [AddRemove]
            public void AddMenu(AlertsData data)
            {
                _alertsData.Add(data);

                MainUi();
            }

            public void RemoveMenu(Int32 index)
            {
                _alertsData.RemoveAt(index);

                if (_alertsData.Count == 0)
                {
                    Kill();
                    return;
                }

                MainUi();
            }
            #endregion

            #region [UI]
            public void MainUi()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5" },
                    Image = {Color = "0 0 0 0"}
                }, "Overlay", Layer);

                Single StartPosition = _alertsData.Count == 1 ? 0f : _alertsData.Count * 15f;
                Single SwitchMax = 20f + StartPosition;
                Single SwitchMin = -20f + StartPosition;

				_alertsData.ForEach(key =>
				{
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = $"-44 {SwitchMin}", OffsetMax = $"-4 {SwitchMax}" },
                        Image = { Color = "0.5 0.5 0.5 0.25", Material = "assets/icons/greyout.mat" }
                    }, Layer, Layer + $".{key._pluginName}");

                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".{key._pluginName}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = plugin.GetImage(key._imageUI)},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "4 4", OffsetMax = "-4 -4"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.15 0.15"}
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = {  Color = "0 0 0 0", Command = $"MenuAlerts_UI openMenu {key._pluginName}" },
                        Text = { Text = "" }
                    }, Layer + $".{key._pluginName}");

                    MenuLine(ref container, key);

                    if (key._isOpen)
                    {
                        InfoUi(ref container, key);
                    }

                    if (key._isLarge && key._isOpen)
                    {
                        SwitchMin += -86;
                        SwitchMax += -86;
                    }
                    else
                    {
                        SwitchMin += -44;
                        SwitchMax += -44;
                    }
				});

                CuiHelper.DestroyUi(_player, Layer);
                CuiHelper.AddUi(_player, container);
            }

			public void MenuLine(ref CuiElementContainer container, AlertsData key)
			{
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"{0.97f - ((float)(Facepunch.Math.Epoch.Current - key._startTime) / key._cooldown)} 0.025" },
                    Image = { Color = GetColorLine(Facepunch.Math.Epoch.Current - key._startTime, key._cooldown) }
                }, Layer + $".{key._pluginName}", Layer + $".{key._pluginName}" + ".Bar");

                CuiHelper.DestroyUi(_player, Layer + $".{key._pluginName}" + ".Bar");
			}

            public void InfoUi(ref CuiElementContainer container, AlertsData key)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"-203.5 {(key._isLarge == true ? -42 : 0)}", OffsetMax = $"-44 0" },
                    Image = { Color = "0.5 0.5 0.5 0.25", Material = "assets/icons/greyout.mat" }
                }, Layer + $".{key._pluginName}", Layer + $".{key._pluginName}" + ".Info");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.93 {(key._isLarge == true ? 0.825 : (0.625))}", AnchorMax = $"0.995 {(key._isLarge == true ? 0.99 : (0.97))}"},
                    Button = { Command = $"MenuAlerts_UI closeMenu {key._pluginName}", Color = "0.9 0 0 0.65", Material = "assets/icons/greyout.mat" },
                    Text = { Text = "✘", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" }
                }, Layer + $".{key._pluginName}" + ".Info");

                container.Add(new CuiElement()
                {
                    Parent = Layer + $".{key._pluginName}" + ".Info",
                    Components =
                    {
                        new CuiTextComponent{Color = "1 1 1 0.9", Text = $"{key._textUI}", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "robotocondensed-regular.ttf"},
                        new CuiRectTransformComponent{AnchorMin = "0.03 0", AnchorMax = $"0.925 {(key._isLarge == true ? 0.77 : 0.5)}"},
                        new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.45 0.45"},
                    }
                });

                TextUi(ref container, key);
                CuiHelper.DestroyUi(_player, Layer + $".{key._pluginName}" + ".Info");
            }

            public void TextUi(ref CuiElementContainer container, AlertsData key)
            {
                TimeSpan time = TimeSpan.FromSeconds(key._startTime - Facepunch.Math.Epoch.Current + key._cooldown);
                container.Add(new CuiElement()
                {
                    Parent = Layer + $".{key._pluginName}" + ".Info",
                    Name = Layer + $".{key._pluginName}" + ".Info" + ".Text",
                    Components =
                    {
                        new CuiTextComponent{Color = "1 1 1 1", Text = $"{key._titleUI}: ({GetFormatTime(time)})", Align = TextAnchor.UpperLeft, FontSize = 16, Font = "robotocondensed-bold.ttf"},
                        new CuiRectTransformComponent{AnchorMin = "0.03 0", AnchorMax = $"0.925 {(key._isLarge == true ? 0.98 : 0.95)}"},
                        new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.45 0.45"},
                    }
                });

                CuiHelper.DestroyUi(_player, Layer + $".{key._pluginName}" + ".Info" + ".Text");
            }
            #endregion

            private String GetColorLine(Int32 count, Int32 max)
            {
                float n = max > 0 ? (float)ColorLine.Length / max : 0;
                Int32 index = (Int32)(count * n);
                if (index > 0) index--;
                return ColorLine[index];
            }

            private String[] ColorLine = { "1.00 1.00 1.00 1.00", "1.00 0.98 0.96 1.00", "1.00 0.97 0.92 1.00", "1.00 0.96 0.88 1.00", "1.00 0.94 0.84 1.00", "1.00 0.93 0.80 1.00", "1.00 0.91 0.76 1.00", "1.00 0.90 0.71 1.00", "1.00 0.89 0.67 1.00", "1.00 0.87 0.63 1.00", "1.00 0.85 0.59 1.00", "1.00 0.84 0.55 1.00", "1.00 0.83 0.51 1.00", "1.00 0.81 0.47 1.00", "1.00 0.80 0.43 1.00", "1.00 0.78 0.39 1.00", "1.00 0.77 0.35 1.00", "1.00 0.76 0.31 1.00", "1.00 0.74 0.27 1.00", "1.00 0.73 0.22 1.00", "1.00 0.71 0.18 1.00", "1.00 0.70 0.14 1.00", "1.00 0.68 0.10 1.00", "1.00 0.67 0.06 1.00", "1.00 0.65 0.02 1.00", "1.00 0.64 0.00 1.00", "1.00 0.61 0.00 1.00", "1.00 0.58 0.00 1.00", "1.00 0.55 0.00 1.00", "1.00 0.53 0.00 1.00", "1.00 0.50 0.00 1.00", "1.00 0.47 0.00 1.00", "1.00 0.45 0.00 1.00", "1.00 0.42 0.00 1.00", "1.00 0.40 0.00 1.00", "1.00 0.37 0.00 1.00", "1.00 0.35 0.00 1.00", "1.00 0.32 0.00 1.00", "1.00 0.29 0.00 1.00", "1.00 0.26 0.00 1.00", "1.00 0.24 0.00 1.00", "1.00 0.21 0.00 1.00", "1.00 0.18 0.00 1.00", "1.00 0.16 0.00 1.00", "1.00 0.13 0.00 1.00", "1.00 0.11 0.00 1.00", "1.00 0.08 0.00 1.00", "1.00 0.05 0.00 1.00", "1.00 0.03 0.00 1.00", "1.00 0.00 0.00 1.00" };

            private String GetFormatTime(TimeSpan timespan) => String.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }
        #endregion

        #region [API]
        private void UpdateCooldownTimeMenu(BasePlayer player, Int32 cooldown, String pluginName)
        {
            MenuAlertsComponent menuAlert = GetComponent(player);
            if (menuAlert == null) return;

            AlertsData find = menuAlert._alertsData.FirstOrDefault(p => p._pluginName == pluginName);
            if (find == null) return;

            find._cooldown = cooldown;
        }

        private void UpdateStartTimeMenu(BasePlayer player, Int32 startTime, String pluginName)
        {
            MenuAlertsComponent menuAlert = GetComponent(player);
            if (menuAlert == null) return;

            AlertsData find = menuAlert._alertsData.FirstOrDefault(p => p._pluginName == pluginName);
            if (find == null) return;

            find._startTime = startTime;
        }

        private void SendAlertMenu(BasePlayer player, Int32 startTime, Int32 cooldown, String titleUI, String textUI, Boolean isLarge, String imageUI, String pluginName)
        {
            MenuAlertsComponent menuAlert = GetComponent(player);
            if (menuAlert == null) return;

            AlertsData find = menuAlert._alertsData.FirstOrDefault(p => p._pluginName == pluginName);
            if (find != null)
            {
                find._startTime = startTime;
                find._cooldown = cooldown;

                find._titleUI = titleUI;
                find._textUI = textUI;

                find._isLarge = isLarge;
                find._isOpen = false;
                find._imageUI = imageUI;

                menuAlert.MainUi();
                return;
            }

            AlertsData data = new AlertsData
            {
                _startTime = startTime,
                _cooldown = cooldown,

                _titleUI = titleUI,
                _textUI = textUI,

                _isLarge = isLarge,
                _isOpen = false,
                _imageUI = imageUI,
                _pluginName = pluginName
            };

            menuAlert.AddMenu(data);
        }

        private void RemoveAlertMenu(BasePlayer player, String pluginName)
        {
            if (player == null) return;

            MenuAlertsComponent menuAlert = GetComponent(player);
            if (menuAlert == null) return;

            AlertsData find = menuAlert._alertsData.FirstOrDefault(p => p._pluginName == pluginName);
            if (find == null) return;

            menuAlert.RemoveMenu(menuAlert._alertsData.IndexOf(find));
        }
        #endregion
    }
}