using Facepunch;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PreferredEnvironment", "Sempai#3239", "2.0.12")]
    [Description("Allows players to customize their environment settings, or create presets that apply to specified zones")]
    public class PreferredEnvironment : RustPlugin
    {
        private const string PERMISSION_USE = "preferredenvironment.use";
        private const string PERMISSION_ADMIN = "preferredenvironment.admin";

        private const string WEATHER_VAR_FILTER = "weather.";

        private Dictionary<ulong, EnvironmentInfo> userEnvironmentInfo = new Dictionary<ulong, EnvironmentInfo>();
        private DynamicConfigFile userData;

        private static Hash<string, ConsoleSystem.Command> weatherConvars;

        private bool initialized = false;

        #region Oxide
        private void Init()
        {
            permission.RegisterPermission(PERMISSION_USE, this);
            permission.RegisterPermission(PERMISSION_ADMIN, this);

            if (!permission.PermissionExists(configData.TimePermission))
                permission.RegisterPermission(configData.TimePermission, this);

            if (!permission.PermissionExists(configData.WeatherPermission))
                permission.RegisterPermission(configData.WeatherPermission, this);

            SetWeatherVars();

            ConsoleSystem.OnReplicatedVarChanged += OnReplicatedVarChanged;
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Error.NoPermission"] = "You are not allowed to use this command",
                ["Error.NoTimeVar"] = "You must enter a number between 0.0 and 24.0 (-1 to disable)",
                ["Notification.WeatherDisabled"] = "Custom weather has been disabled",
                ["Notification.TimeDisabled"] = "Custom time has been disabled",
                ["Notification.HasZoneOverride"] = "You currently can not edit time/weather settings as you are in a zone override",
                ["Notification.AdminOverride"] = "A admin has set your environment variable \"{0}\" to \"{1}\"",
                ["Notification.SetTime"] = "You have set the time to {0}",
                ["Weather.Fog"] = "Fog",
                ["Weather.Rain"] = "Rain",
                ["Weather.Rainbow"] = "Rainbow",
                ["Weather.Thunder"] = "Thunder",
                ["Weather.Wind"] = "Wind",
                ["Weather.AtmosphereBrightness"] = "Atmosphere Brightness",
                ["Weather.AtmosphereContrast"] = "Atmosphere Contrast",
                ["Weather.AtmosphereDirectionality"] = "Atmosphere Directionality",
                ["Weather.AtmosphereMie"] = "Atmosphere Mie",
                ["Weather.AtmosphereRayleigh"] = "Atmosphere Rayleigh",
                ["Weather.CloudAttenuation"] = "Cloud Attenuation",
                ["Weather.CloudBrightness"] = "Cloud Brightness",
                ["Weather.CloudColoring"] = "Cloud Coloring",
                ["Weather.CloudCoverage"] = "Cloud Coverage",
                ["Weather.CloudOpacity"] = "Cloud Opacity",
                ["Weather.CloudSaturation"] = "Cloud Saturation",
                ["Weather.CloudScattering"] = "Cloud Scattering",
                ["Weather.CloudSharpness"] = "Cloud Sharpness",
                ["Weather.CloudSize"] = "Cloud Size",
                ["Weather.ClearChance"] = "Clear Chance",
                ["Weather.DustChance"] = "Dust Chance",
                ["Weather.FogChance"] = "Fog Chance",
                ["Weather.OvercastChance"] = "Overcast Chance",
                ["Weather.RainChance"] = "Rain Chance",
                ["Weather.StormChance"] = "Storm Chance",
                ["Weather.ProgressTime"] = "Progress Time",
                ["Menu.EnviromentEditor"] = "Environment Editor",
                ["Menu.TimeEditor"] = "Time Editor",
                ["Menu.ServerEnviromentEditor"] = "Server Environment Editor",
                ["Menu.ServerTimeEditor"] = "Server Time Editor",
                ["Menu.Reset"] = "Reset",
                ["Menu.ServerSet"] = "Use Server Value",
                ["Menu.Automated"] = "Automated"
            },
            this);

            if (configData.EnableSaving)
                LoadData();
            else Unsubscribe(nameof(OnServerSave));
        }

        private void OnServerInitialized()
        {
            initialized = true;

            LoadServerVars();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void OnServerSave() => userData.WriteObject(userEnvironmentInfo);

        private void OnPlayerConnected(BasePlayer player) => player.Invoke(()=> DelayedEnvironmentUpdate(player), 1f);

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            EnvironmentInfo environmentInfo;
            if (userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
            {
                if (environmentInfo.ShouldRemove())
                    userEnvironmentInfo.Remove(player.userID);
            }         
        }

        private void OnUserPermissionRevoked(string id, string permission)
        {
            if (permission.Equals(PERMISSION_USE))
            {
                ulong playerId = ulong.Parse(id);

                EnvironmentInfo environmentInfo;
                if (userEnvironmentInfo.TryGetValue(playerId, out environmentInfo))
                {
                    if (!environmentInfo.HasZoneOverride())
                    {                        
                        SendServerReplicatedVars(FindPlayer(id));                         
                        userEnvironmentInfo.Remove(playerId);
                    }
                }
            }
        }

        private object CanNetworkTo(EnvSync env, BasePlayer player)
        {
            EnvironmentInfo environmentInfo;
            if (!userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo) || environmentInfo.GetDesiredTime < 0f)
                return null;

            if (Net.sv.write.Start())
            {
                Connection connection = player.net.connection;
                connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
                BaseNetworkable.SaveInfo saveInfo = new BaseNetworkable.SaveInfo
                {
                    forConnection = player.net.connection,
                    forDisk = false
                };

                Net.sv.write.PacketID(Message.Type.Entities);
                Net.sv.write.UInt32(player.net.connection.validate.entityUpdates);

                using (saveInfo.msg = Pool.Get<ProtoBuf.Entity>())
                {
                    env.Save(saveInfo);

                    float desiredTime = environmentInfo.GetDesiredTime;

                    TOD_CycleParameters time = TOD_Sky.Instance.Cycle;

                    DateTime dateTime = new DateTime(0L, DateTimeKind.Utc);
                    
                    dateTime = dateTime.AddYears(time.Year - 1);
                    dateTime = dateTime.AddMonths(time.Month - 1);
                    dateTime = dateTime.AddDays(time.Day - 1);

                    int hours = Mathf.FloorToInt(desiredTime);
                    dateTime = dateTime.AddHours(hours);
                    dateTime = dateTime.AddMinutes(((Mathf.Round(desiredTime * 100) / 100) - hours) * 60);

                    saveInfo.msg.environment.dateTime = dateTime.ToBinary();

                    saveInfo.msg.ToProto(Net.sv.write);
                    Net.sv.write.Send(new SendInfo(player.net.connection));
                }
            }

            return false;
        }
        
        private void OnServerCommand(string cmd, string[] args)
        {
            if (cmd.StartsWith(WEATHER_VAR_FILTER, StringComparison.OrdinalIgnoreCase))
                SendReplicatedVarsAll();            
        }

        private void Unload()
        {
            ConsoleSystem.OnReplicatedVarChanged -= OnReplicatedVarChanged;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UI_MENU);
                CuiHelper.DestroyUi(player, UI_DUMMY);
            }

            weatherConvars = null;

            if (initialized)
                ServerMgr.SendReplicatedVars(WEATHER_VAR_FILTER);
        }
        #endregion

        #region Functions        
        private void LoadData()
        {
            userData = Interface.Oxide.DataFileSystem.GetFile("preferred_environment");

            userEnvironmentInfo = userData.ReadObject<Dictionary<ulong, EnvironmentInfo>>();            
        }

        private void LoadServerVars()
        {
            if (configData.Server._clearChance != -1)
                ConVar.Weather.clear_chance = configData.Server._clearChance;

            if (configData.Server._dustChance != -1)
                ConVar.Weather.dust_chance = configData.Server._dustChance;

            if (configData.Server._fogChance != -1)
                ConVar.Weather.fog_chance = configData.Server._fogChance;

            if (configData.Server._overcastChance != -1)
                ConVar.Weather.overcast_chance = configData.Server._overcastChance;

            if (configData.Server._rainChance != -1)
                ConVar.Weather.rain_chance = configData.Server._rainChance;

            if (configData.Server._stormChance != -1)
                ConVar.Weather.storm_chance = configData.Server._stormChance;

            if (configData.Server._atmosphere_brightness != -1)
                ConVar.Weather.atmosphere_brightness = configData.Server._atmosphere_brightness;

            if (configData.Server._atmosphere_contrast != -1)
                ConVar.Weather.atmosphere_contrast = configData.Server._atmosphere_contrast;

            if (configData.Server._atmosphere_directionality != -1)
                ConVar.Weather.atmosphere_directionality = configData.Server._atmosphere_directionality;

            if (configData.Server._atmosphere_mie != -1)
                ConVar.Weather.atmosphere_mie = configData.Server._atmosphere_mie;

            if (configData.Server._atmosphere_rayleigh != -1)
                ConVar.Weather.atmosphere_rayleigh = configData.Server._atmosphere_rayleigh;

            if (configData.Server._cloud_attenuation != -1)
                ConVar.Weather.cloud_attenuation = configData.Server._cloud_attenuation;

            if (configData.Server._cloud_brightness != -1)
                ConVar.Weather.cloud_brightness = configData.Server._cloud_brightness;

            if (configData.Server._cloud_coloring != -1)
                ConVar.Weather.cloud_coloring = configData.Server._cloud_coloring;

            if (configData.Server._cloud_coverage != -1)
                ConVar.Weather.cloud_coverage = configData.Server._cloud_coverage;

            if (configData.Server._cloud_opacity != -1)
                ConVar.Weather.cloud_opacity = configData.Server._cloud_opacity;

            if (configData.Server._cloud_saturation != -1)
                ConVar.Weather.cloud_saturation = configData.Server._cloud_saturation;

            if (configData.Server._cloud_scattering != -1)
                ConVar.Weather.cloud_scattering = configData.Server._cloud_scattering;

            if (configData.Server._cloud_sharpness != -1)
                ConVar.Weather.cloud_sharpness = configData.Server._cloud_sharpness;

            if (configData.Server._cloud_size != -1)
                ConVar.Weather.cloud_size = configData.Server._cloud_size;

            ConVar.Env.progresstime = configData.Server._progressTime;
        }

        private void SetWeatherVars()
        {
            weatherConvars = new Hash<string, ConsoleSystem.Command>();

            foreach (ConsoleSystem.Command replicated in ConsoleSystem.Index.Server.Replicated)
            {
                if (replicated.FullName.StartsWith(WEATHER_VAR_FILTER))
                    weatherConvars.Add(replicated.FullName, replicated);
            }
        }

        private void DelayedEnvironmentUpdate(BasePlayer player)
        {
            if (player != null && player.IsConnected)
            {
                EnvironmentInfo environmentInfo;
                if (userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
                {
                    environmentInfo.BuildReplicatedConvarList();

                    if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE) || environmentInfo.ShouldRemove())
                    {
                        SendServerReplicatedVars(player);
                        userEnvironmentInfo.Remove(player.userID);
                    }
                    else environmentInfo.SendCustomReplicatedVars(player);
                }
            }
        }

        private void OnReplicatedVarChanged(string command, string value)
        {
            if (command.StartsWith(WEATHER_VAR_FILTER, StringComparison.OrdinalIgnoreCase))            
                SendReplicatedVarsAll();            
        }

        private void SendReplicatedVarsAll()
        {
            if (initialized)
                ServerMgr.Instance.StartCoroutine(SendReplicatedVarsAllEnumerator());
        }
        
        private IEnumerator SendReplicatedVarsAllEnumerator()
        {
            List<BasePlayer> list = Pool.GetList<BasePlayer>();
            list.AddRange(BasePlayer.activePlayerList);

            for (int i = 0; i < list.Count; i++)
            {
                BasePlayer player = list[i];

                if (player == null || !player.IsConnected)
                    continue;

                EnvironmentInfo environmentInfo;
                if (userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
                    environmentInfo.SendCustomReplicatedVars(player);
                else SendServerReplicatedVars(player);
                
                yield return null;
                yield return null;
            }

            Pool.FreeList(ref list);
        }

        public void SendServerReplicatedVars(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            if (Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Message.Type.ConsoleReplicatedVars);
                Net.sv.write.Int32(weatherConvars.Count);

                foreach (KeyValuePair<string, ConsoleSystem.Command> kvp in weatherConvars)
                {
                    Net.sv.write.String(kvp.Key);
                    Net.sv.write.String(kvp.Value.String);
                }

                Net.sv.write.Send(new SendInfo(player.net.connection));
            }
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("setenv")]
        private void ccmdSetEnvironment(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Connection.userid.ToString(), PERMISSION_ADMIN))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length < 3)
            {
                SendReply(arg, "\n\nsetenv <name or id> <variable> <value> - Manually set an environment variable for the target player\n\nEnvironment variables;\nfog (0.0 - 1.0)\nrain (0.0 - 1.0)\nrainbow  (0.0 - 1.0)\nthunder (0.0 - 1.0)\nwind (0.0 - 1.0)\natmosphere_brightness (0.0 - 1.0)\natmosphere_contrast (0.0 - 1.0)\natmosphere_directionality (0.0 - 1.0)\natmosphere_mie (0.0 - 1.0)\natmosphere_rayleigh (0.0 - 1.0)\ncloud_attenuation (0.0 - 1.0)\ncloud_brightness (0.0 - 1.0)\ncloud_coloring (0.0 - 1.0)\ncloud_coverage (0.0 - 1.0)\ncloud_opacity (0.0 - 1.0)\ncloud_saturation (0.0 - 1.0)\ncloud_scattering (0.0 - 1.0)\ncloud_sharpness (0.0 - 1.0)\ncloud_size\n\ntime (0.0-24.0)\n\nTo disable any of these variables set the value to '-1'");
                return;
            }

            BasePlayer player = FindPlayer(arg.GetString(0));
            if (player == null)
            {
                SendReply(arg, $"Unable to find a player with the name or ID \"{arg.GetString(0)}\"");
                return;
            }

            string variable = arg.GetString(1);
            float value = arg.GetFloat(2);

            EnvironmentInfo environmentInfo;
            if (!userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
            {
                userEnvironmentInfo.Add(player.userID, new EnvironmentInfo());
            }

            switch (variable)
            {
                case "fog":
                    environmentInfo.Fog = value;
                    break;
                case "rain":
                    environmentInfo.Rain = value;
                    break;
                case "rainbow":
                    environmentInfo.Rainbow = value;
                    break;
                case "thunder":
                    environmentInfo.Thunder = value;
                    break;
                case "wind":
                    environmentInfo.Wind = value;
                    break;
                case "atmosphere_brightness":
                    environmentInfo.AtmosphereBrightness = value;
                    break;
                case "atmosphere_contrast":
                    environmentInfo.AtmosphereContrast = value;
                    break;
                case "atmosphere_directionality":
                    environmentInfo.AtmosphereDirectionality = value;
                    break;
                case "atmosphere_mie":
                    environmentInfo.AtmosphereMie = value;
                    break;
                case "atmosphere_rayleigh":
                    environmentInfo.AtmosphereRayleigh = value;
                    break;
                case "cloud_attenuation":
                    environmentInfo.CloudAttenuation = value;
                    break;
                case "cloud_brightness":
                    environmentInfo.CloudBrightness = value;
                    break;
                case "cloud_coloring":
                    environmentInfo.CloudColoring = value;
                    break;
                case "cloud_coverage":
                    environmentInfo.CloudCoverage = value;
                    break;
                case "cloud_opacity":
                    environmentInfo.CloudOpacity = value;
                    break;
                case "cloud_saturation":
                    environmentInfo.CloudSaturation = value;
                    break;
                case "cloud_scattering":
                    environmentInfo.CloudScattering = value;
                    break;
                case "cloud_sharpness":
                    environmentInfo.CloudSharpness = value;
                    break;
                case "cloud_size":
                    environmentInfo.CloudSize = value;
                    break;
                case "time":
                    environmentInfo.Time = value;
                    break;
                default:
                    SendReply(arg, "Invalid variable selected!");
                    return;
            }

            SendReply(arg, $"Set environment variable \"{variable}\" to \"{value}\" for player {player.displayName}");

            SendReply(player, string.Format(_msg("Notification.AdminOverride", player.UserIDString), variable, value));

            environmentInfo.BuildReplicatedConvarList();

            if (environmentInfo.ShouldRemove())
            {
                SendServerReplicatedVars(player);
                userEnvironmentInfo.Remove(player.userID);
            }
            else environmentInfo.SendCustomReplicatedVars(player);
        }

        private BasePlayer FindPlayer(string partialNameOrID) => BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.displayName.Equals(partialNameOrID, StringComparison.OrdinalIgnoreCase)) ??
                                                                 BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.displayName.Contains(partialNameOrID, CompareOptions.OrdinalIgnoreCase)) ??
                                                                 BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.UserIDString == partialNameOrID);
        #endregion

        private string _msg(string key, string id = null)
        {
            return lang.GetMessage(key, this, id);
        }

        #region ZoneManager
        private void OnEnterZone(string zoneID, BasePlayer player)
        {
            EnvironmentInfo zoneEnvironmentInfo;

            if (!configData.Zones.TryGetValue(zoneID, out zoneEnvironmentInfo))
                return;

            EnvironmentInfo environmentInfo;
            if (!userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
            {
                userEnvironmentInfo.Add(player.userID, environmentInfo = new EnvironmentInfo());
            }

            environmentInfo.OnEnterZone(zoneID, zoneEnvironmentInfo);
            zoneEnvironmentInfo.SendCustomReplicatedVars(player);
        }

        private void OnExitZone(string zoneID, BasePlayer player)
        {
            if (!configData.Zones.ContainsKey(zoneID))
                return;

            EnvironmentInfo environmentInfo;
            if (userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
            {
                environmentInfo.OnExitZone(zoneID, player);

                if (environmentInfo.ShouldRemove() || (!environmentInfo.HasZoneOverride() && !permission.UserHasPermission(player.UserIDString, PERMISSION_USE)))
                {
                    SendServerReplicatedVars(player);
                    userEnvironmentInfo.Remove(player.userID);
                }
                else environmentInfo.SendCustomReplicatedVars(player);
            }
        }
        #endregion

        #region UI         
        public static class UI
        {
            public static CuiElementContainer Container(string panelName, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName.ToString()
                    }
                };
                return container;
            }

            public static CuiElementContainer BlurContainer(string panelName, UI4 dimensions, string color = "0 0 0 0.55", string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = true
                        },
                        new CuiElement().Parent = parent,
                        panelName.ToString()
                    }
                };
                return container;
            }

            public static void Panel(ref CuiElementContainer container, string panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                panel.ToString());
            }

            public static void BlurPanel(ref CuiElementContainer container, string panel, UI4 dimensions, string color = "0 0 0 0.5")
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = false
                },
                panel, CuiHelper.GetGuid());
            }

            public static void Label(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter, FontStyle fontStyle = FontStyle.RobotoCondensed)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text, Font = ToFontString(fontStyle) },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel.ToString());

            }

            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter, FontStyle fontStyle = FontStyle.RobotoCondensed)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align, Font = ToFontString(fontStyle) }
                },
                panel.ToString());
            }

            internal static void Toggle(ref CuiElementContainer container, string panel, string color, int fontSize, UI4 dimensions, string command, bool isOn)
            {
                UI.Panel(ref container, panel, color, dimensions);

                if (isOn)
                    UI.Label(ref container, panel, "✔", fontSize, dimensions);

                UI.Button(ref container, panel, "0 0 0 0", string.Empty, 0, dimensions, command);
            }

            public static void Image(ref CuiElementContainer container, string panel, string png, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent { Png = png },
                        new CuiRectTransformComponent { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static void Input(ref CuiElementContainer container, string panel, string text, int size, string command, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 300,
                            Command = command,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }

            public enum FontStyle { DroidSansMono, RobotoCondensed, RobotoCondensedBold, PermanantMarker }

            private static string ToFontString(FontStyle fontStyle)
            {
                switch (fontStyle)
                {
                    case FontStyle.DroidSansMono:
                        return "droidsansmono.ttf";
                    case FontStyle.PermanantMarker:
                        return "permanentmarker.ttf";
                    case FontStyle.RobotoCondensed:
                        return "robotocondensed-regular.ttf";
                    case FontStyle.RobotoCondensedBold:
                    default:
                        return "robotocondensed-bold.ttf";
                }
            }
        }

        public class UI4
        {
            [JsonProperty(PropertyName = "Left (0.0 - 1.0)")]
            public float xMin;

            [JsonProperty(PropertyName = "Bottom (0.0 - 1.0)")]
            public float yMin;

            [JsonProperty(PropertyName = "Right (0.0 - 1.0)")]
            public float xMax;

            [JsonProperty(PropertyName = "Top (0.0 - 1.0)")]
            public float yMax;

            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            public string GetMin() => $"{xMin} {yMin}";
            public string GetMax() => $"{xMax} {yMax}";

            public static UI4 zero = new UI4(0f, 0f, 0f, 0f);
        }
        #endregion

        #region UI Creation
        private const string UI_MENU = "pe.menu";
        private const string UI_DUMMY = "pe.dummy";

        private void OpenDummyContainer(BasePlayer player)
        {
            CuiElementContainer container = UI.BlurContainer(UI_DUMMY, UI4.zero);
            CuiHelper.DestroyUi(player, UI_DUMMY);
            CuiHelper.AddUi(player, container);
        }

        private void OpenEnvironmentEditor(BasePlayer player, EnvironmentInfo environmentInfo)
        {
            bool hasWeatherPerm = permission.UserHasPermission(player.UserIDString, configData.WeatherPermission);
            bool hasTimePerm = permission.UserHasPermission(player.UserIDString, configData.TimePermission);

            if (!hasWeatherPerm && hasTimePerm)
            {
                CuiElementContainer container = UI.BlurContainer(UI_MENU, new UI4(0.35f, 0.53f, 0.65f, 0.6f));
                UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 0.5f, 1f, 1f), UI.Color("000000", 0.7f));
                UI.Label(ref container, UI_MENU, _msg("Menu.TimeEditor", player.UserIDString), 16, new UI4(0.01f, 0.55f, 0.95f, 0.95f), TextAnchor.MiddleLeft, UI.FontStyle.RobotoCondensedBold);
                UI.Button(ref container, UI_MENU, UI.Color("d85540", 1f), _msg("Menu.Reset", player.UserIDString), 12, new UI4(0.85f, 0.55f, 0.95f, 0.95f), "pe.reset");
                UI.Button(ref container, UI_MENU, UI.Color("d85540", 1f), "✘", 12, new UI4(0.955f, 0.55f, 0.995f, 0.95f), "pe.closeui");

                UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 0.05f, 1f, 0.45f), UI.Color("000000", 0.7f));
                UI.Button(ref container, UI_MENU, UI.Color("6a8b38", 1f), "-", 14, new UI4(0.015f, 0.05f, 0.055f, 0.45f), $"pe.editvalue time {(environmentInfo.Time <= 0 ? -1f : Mathf.Clamp(environmentInfo.Time - 0.5f, 0f, 24f))}");

                if (environmentInfo.Time > 0f)
                    UI.Panel(ref container, UI_MENU, UI.Color("387097", 1f), new UI4(0.06f, 0.05f, 0.06f + ((0.95f - 0.06f) * (environmentInfo.Time / 24f)), 0.45f));

                if (environmentInfo.Time < 0f)
                    UI.Label(ref container, UI_MENU, _msg("Menu.ServerSet", player.UserIDString), 12, new UI4(0.06f, 0.05f, 0.95f, 0.45f), TextAnchor.MiddleCenter);
                else UI.Label(ref container, UI_MENU, $"{environmentInfo.Time}", 12, new UI4(0.06f, 0.05f, 0.95f, 0.45f), TextAnchor.MiddleCenter);

                float progressWidth = (0.95f - 0.06f) * 0.05f;

                for (int i = 0; i < 20; i++)
                {
                    float left = 0.06f + (progressWidth * i);
                    float right = left + progressWidth;

                    UI.Button(ref container, UI_MENU, "0 0 0 0", string.Empty, 0, new UI4(left, 0.05f, right, 0.45f), $"pe.editvalue time {(i + 1) * 1.2f}");
                }

                UI.Button(ref container, UI_MENU, UI.Color("6a8b38", 1f), "+", 14, new UI4(0.955f, 0.05f, 0.995f, 0.45f), $"pe.editvalue time {(environmentInfo.Time < 0f ? 0f : Mathf.Clamp(0.5f + environmentInfo.Time, 0f, 24f))}");

                CuiHelper.DestroyUi(player, UI_MENU);
                CuiHelper.AddUi(player, container);
            }
            else if (hasWeatherPerm)
            {
                float totalSize = hasWeatherPerm && hasTimePerm ? 22f : hasWeatherPerm ? 20f : hasTimePerm ? 2f : 22f;

                CuiElementContainer container = UI.BlurContainer(UI_MENU, new UI4(0.35f, 0.19f, 0.65f, 0.8f));

                float size = 1f / totalSize;
                int count = 0;

                UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (count * size) - size, 1f, 1f - (count * size)), UI.Color("000000", 0.7f));
                UI.Label(ref container, UI_MENU, _msg("Menu.EnviromentEditor", player.UserIDString), 16, new UI4(0.01f, 1f - (count * size) - size, 0.95f, 1f - (count * size)), TextAnchor.MiddleLeft, UI.FontStyle.RobotoCondensedBold);
                UI.Button(ref container, UI_MENU, UI.Color("d85540", 1f), _msg("Menu.Reset", player.UserIDString), 12, new UI4(0.85f, (1f - (count * size) - size) + 0.005f, 0.95f, 1f - (count * size) - 0.005f), "pe.reset");
                UI.Button(ref container, UI_MENU, UI.Color("d85540", 1f), "✘", 12, new UI4(0.955f, (1f - (count * size) - size) + 0.005f, 0.995f, 1f - (count * size) - 0.005f), "pe.closeui");
                count++;

                UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (20 * size), 0.4f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));

                UI.BlurPanel(ref container, UI_MENU, new UI4(0.405f, 1f - (20 * size), 1f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));

                AddMenuOption(ref container, _msg("Weather.Fog", player.UserIDString), "fog", environmentInfo.Fog, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.Rain", player.UserIDString), "rain", environmentInfo.Rain, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.Rainbow", player.UserIDString), "rainbow", environmentInfo.Rainbow, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.Thunder", player.UserIDString), "thunder", environmentInfo.Thunder, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.Wind", player.UserIDString), "wind", environmentInfo.Wind, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.AtmosphereBrightness", player.UserIDString), "atmosphere_brightness", environmentInfo.AtmosphereBrightness, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.AtmosphereContrast", player.UserIDString), "atmosphere_contrast", environmentInfo.AtmosphereContrast, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.AtmosphereDirectionality", player.UserIDString), "atmosphere_directionality", environmentInfo.AtmosphereDirectionality, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.AtmosphereMie", player.UserIDString), "atmosphere_mie", environmentInfo.AtmosphereMie, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.AtmosphereRayleigh", player.UserIDString), "atmosphere_rayleigh", environmentInfo.AtmosphereRayleigh, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudAttenuation", player.UserIDString), "cloud_attenuation", environmentInfo.CloudAttenuation, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudBrightness", player.UserIDString), "cloud_brightness", environmentInfo.CloudBrightness, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudColoring", player.UserIDString), "cloud_coloring", environmentInfo.CloudColoring, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudCoverage", player.UserIDString), "cloud_coverage", environmentInfo.CloudCoverage, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudOpacity", player.UserIDString), "cloud_opacity", environmentInfo.CloudOpacity, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudSaturation", player.UserIDString), "cloud_saturation", environmentInfo.CloudSaturation, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudScattering", player.UserIDString), "cloud_scattering", environmentInfo.CloudScattering, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudSharpness", player.UserIDString), "cloud_sharpness", environmentInfo.CloudSharpness, count, size, player.UserIDString);
                count++;

                AddMenuOption(ref container, _msg("Weather.CloudSize", player.UserIDString), "cloud_size", environmentInfo.CloudSize, count, size, player.UserIDString);
                count++;

                if (hasTimePerm)
                {
                    UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (count * size) - size, 1f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));
                    UI.Label(ref container, UI_MENU, _msg("Menu.TimeEditor", player.UserIDString), 16, new UI4(0.01f, 1f - (count * size) - size, 0.95f, 1f - (count * size)), TextAnchor.MiddleLeft, UI.FontStyle.RobotoCondensedBold);
                    count++;

                    UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (count * size) - size, 1f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));
                    AddTimeOption(ref container, environmentInfo.Time, count, size, player.UserIDString);
                    count++;
                }

                CuiHelper.DestroyUi(player, UI_MENU);
                CuiHelper.AddUi(player, container);
            }           
        }

        private void OpenServerEnvironmentEditor(BasePlayer player)
        {
            CuiElementContainer container = UI.BlurContainer(UI_MENU, new UI4(0.35f, 0.02f, 0.65f, 0.96f));

            float size = 1f / 29f;
            int count = 0;

            UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (count * size) - size, 1f, 1f - (count * size)), UI.Color("000000", 0.7f));
            UI.Label(ref container, UI_MENU, _msg("Menu.ServerEnviromentEditor", player.UserIDString), 16, new UI4(0.01f, 1f - (count * size) - size, 0.95f, 1f - (count * size)), TextAnchor.MiddleLeft, UI.FontStyle.RobotoCondensedBold);

            UI.Button(ref container, UI_MENU, UI.Color("d85540", 1f), _msg("Menu.Reset", player.UserIDString), 12, new UI4(0.85f, (1f - (count * size) - size) + 0.005f, 0.95f, 1f - (count * size) - 0.005f), "pe.servreset");
            UI.Button(ref container, UI_MENU, UI.Color("d85540", 1f), "✘", 12, new UI4(0.955f, (1f - (count * size) - size) + 0.005f, 0.995f, 1f - (count * size) - 0.005f), "pe.closeui");
            count++;

            UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (26 * size), 0.4f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));

            UI.BlurPanel(ref container, UI_MENU, new UI4(0.405f, 1f - (26 * size), 1f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));

            AddMenuOption(ref container, _msg("Weather.Fog", player.UserIDString), "fog", ConVar.Weather.fog, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.Rain", player.UserIDString), "rain", ConVar.Weather.rain, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.Rainbow", player.UserIDString), "rainbow", ConVar.Weather.rainbow, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.Thunder", player.UserIDString), "thunder", ConVar.Weather.thunder, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.Wind", player.UserIDString), "wind", ConVar.Weather.wind, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.AtmosphereBrightness", player.UserIDString), "atmosphere_brightness", ConVar.Weather.atmosphere_brightness, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.AtmosphereContrast", player.UserIDString), "atmosphere_contrast", ConVar.Weather.atmosphere_contrast, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.AtmosphereDirectionality", player.UserIDString), "atmosphere_directionality", ConVar.Weather.atmosphere_directionality, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.AtmosphereMie", player.UserIDString), "atmosphere_mie", ConVar.Weather.atmosphere_mie, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.AtmosphereRayleigh", player.UserIDString), "atmosphere_rayleigh", ConVar.Weather.atmosphere_rayleigh, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudAttenuation", player.UserIDString), "cloud_attenuation", ConVar.Weather.cloud_attenuation, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudBrightness", player.UserIDString), "cloud_brightness", ConVar.Weather.cloud_brightness, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudColoring", player.UserIDString), "cloud_coloring", ConVar.Weather.cloud_coloring, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudCoverage", player.UserIDString), "cloud_coverage", ConVar.Weather.cloud_coverage, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudOpacity", player.UserIDString), "cloud_opacity", ConVar.Weather.cloud_opacity, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudSaturation", player.UserIDString), "cloud_saturation", ConVar.Weather.cloud_saturation, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudScattering", player.UserIDString), "cloud_scattering", ConVar.Weather.cloud_scattering, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudSharpness", player.UserIDString), "cloud_sharpness", ConVar.Weather.cloud_sharpness, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.CloudSize", player.UserIDString), "cloud_size", ConVar.Weather.cloud_size, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.ClearChance", player.UserIDString), "clear_chance", ConVar.Weather.clear_chance, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.DustChance", player.UserIDString), "dust_chance", ConVar.Weather.dust_chance, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.FogChance", player.UserIDString), "fog_chance", ConVar.Weather.fog_chance, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.OvercastChance", player.UserIDString), "overcast_chance", ConVar.Weather.overcast_chance, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.RainChance", player.UserIDString), "rain_chance", ConVar.Weather.rain_chance, count, size, player.UserIDString, true);
            count++;

            AddMenuOption(ref container, _msg("Weather.StormChance", player.UserIDString), "storm_chance", ConVar.Weather.storm_chance, count, size, player.UserIDString, true);
            count++;

            UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - (count * size) - size, 1f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));
            UI.Label(ref container, UI_MENU, _msg("Menu.ServerTimeEditor", player.UserIDString), 16, new UI4(0.01f, 1f - (count * size) - size, 0.95f, 1f - (count * size)), TextAnchor.MiddleLeft, UI.FontStyle.RobotoCondensedBold);
            count++;

            UI.BlurPanel(ref container, UI_MENU, new UI4(0f, 1f - ((count + 1) * size) - size, 1f, 1f - (count * size) - 0.005f), UI.Color("000000", 0.7f));
            AddTimeOption(ref container, ConVar.Env.time, count, size, player.UserIDString, true);
            count++;

            AddMenuToggle(ref container, _msg("Weather.ProgressTime", player.UserIDString), "progress_time", ConVar.Env.progresstime, count, size, player.UserIDString, true);
            count++;

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.AddUi(player, container);
        }

        private void AddMenuOption(ref CuiElementContainer container, string title, string variable, float currentValue, int position, float size, string playerId, bool isServer = false)
        {
            float top = 1f - (position * size);
            float bottom = top - size;
            
            UI.Label(ref container, UI_MENU, title, 14, new UI4(0.015f, bottom, 0.5f, top), TextAnchor.MiddleLeft);

            UI.Button(ref container, UI_MENU, UI.Color("6a8b38", 1f), "-", 14, new UI4(0.415f, bottom + 0.005f, 0.455f, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} {variable} {(currentValue <= 0f ? -1f : Mathf.Clamp01(currentValue - 0.1f))}");
           
            if (currentValue >= 0f)
            {
                if (currentValue > 0f)
                    UI.Panel(ref container, UI_MENU, UI.Color("387097", 1f), new UI4(0.46f, bottom + 0.015f, 0.46f + ((0.95f - 0.46f) * currentValue), top - 0.0175f));

                UI.Label(ref container, UI_MENU, $"{Mathf.RoundToInt(currentValue * 100f)}%", 12, new UI4(0.46f, bottom + 0.005f, 0.95f, top - 0.0075f), TextAnchor.MiddleCenter);
            }
            else
            {
                UI.Label(ref container, UI_MENU, isServer ? _msg("Menu.Automated", playerId) : _msg("Menu.ServerSet", playerId), 12, new UI4(0.46f, bottom + 0.005f, 0.95f, top - 0.0075f), TextAnchor.MiddleCenter);
            }

            float progressWidth = (0.95f - 0.46f) * 0.1f;

            for (int i = 0; i < 10; i++)
            {
                float left = 0.46f + (progressWidth * i);
                float right = left + progressWidth;

                UI.Button(ref container, UI_MENU, "0 0 0 0", string.Empty, 0, new UI4(left, bottom + 0.005f, right, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} {variable} {(i + 1) * 0.1f}");
            }

            UI.Button(ref container, UI_MENU, UI.Color("6a8b38", 1f), "+", 14, new UI4(0.955f, bottom + 0.005f, 0.995f, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} {variable} {(currentValue < 0f ? 0f : Mathf.Clamp01(currentValue + 0.05f))}");
        }

        private void AddTimeOption(ref CuiElementContainer container, float currentValue, int position, float size, string playerId, bool isServer = false)
        {
            float top = 1f - (position * size);
            float bottom = top - size;

            UI.Button(ref container, UI_MENU, UI.Color("6a8b38", 1f), "-", 14, new UI4(0.015f, bottom + 0.005f, 0.055f, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} time {(currentValue <= 0 ? -1f : Mathf.Clamp(currentValue - 0.5f, 0f, 24f))}");

            if (currentValue > 0f)
                UI.Panel(ref container, UI_MENU, UI.Color("387097", 1f), new UI4(0.06f, bottom + 0.015f, 0.06f + ((0.95f - 0.06f) * (currentValue / 24f)), top - 0.0175f));

            if (currentValue < 0f)
                UI.Label(ref container, UI_MENU, isServer ? _msg("Menu.Automated", playerId) : _msg("Menu.ServerSet", playerId), 12, new UI4(0.06f, bottom + 0.005f, 0.95f, top - 0.0075f), TextAnchor.MiddleCenter);
            else UI.Label(ref container, UI_MENU, $"{currentValue}", 12, new UI4(0.06f, bottom + 0.005f, 0.95f, top - 0.0075f), TextAnchor.MiddleCenter);

            float progressWidth = (0.95f - 0.06f) * 0.05f;

            for (int i = 0; i < 20; i++)
            {
                float left = 0.06f + (progressWidth * i);
                float right = left + progressWidth;

                UI.Button(ref container, UI_MENU, "0 0 0 0", string.Empty, 0, new UI4(left, bottom + 0.005f, right, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} time {(i + 1) * 1.2f}");
            }

            UI.Button(ref container, UI_MENU, UI.Color("6a8b38", 1f), "+", 14, new UI4(0.955f, bottom + 0.005f, 0.995f, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} time {(currentValue < 0f ? 0f : Mathf.Clamp(0.5f + currentValue, 0f, 24f))}");
        }

        private void AddMenuToggle(ref CuiElementContainer container, string title, string variable, bool currentValue, int position, float size, string playerId, bool isServer = false)
        {
            float top = 1f - (position * size);
            float bottom = top - size;

            UI.Label(ref container, UI_MENU, title, 14, new UI4(0.015f, bottom, 0.5f, top), TextAnchor.MiddleLeft);

            UI.Toggle(ref container, UI_MENU, UI.Color("6a8b38", 1f), 12, new UI4(0.415f, bottom + 0.005f, 0.455f, top - 0.0075f), $"{(isServer ? "pe.server.editvalue" : "pe.editvalue")} {variable} {(currentValue ? 0f : 1f)}", currentValue);
            
        }
        #endregion

        #region UI Commands        
        [ConsoleCommand("pe.closeui")]
        private void ccmdCloseUI(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.DestroyUi(player, UI_DUMMY);

            EnvironmentInfo environmentInfo;
            if (userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
            {
                if (environmentInfo.ShouldRemove())
                {
                    SendServerReplicatedVars(player);
                    userEnvironmentInfo.Remove(player.userID);
                }
            }
        }

        [ConsoleCommand("pe.reset")]
        private void ccmdReset(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!userEnvironmentInfo.ContainsKey(player.userID))
                return;

            EnvironmentInfo environmentInfo = userEnvironmentInfo[player.userID] = new EnvironmentInfo();

            environmentInfo.SendCustomReplicatedVars(player);

            OpenEnvironmentEditor(player, environmentInfo);
        }

        [ConsoleCommand("pe.servreset")]
        private void ccmdServerReset(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                player.ChatMessage(_msg("Error.NoPermission", player.UserIDString));
                return;
            }

            ConVar.Weather.fog = configData.Server.Fog = -1;
            ConVar.Weather.rain = configData.Server.Rain = -1;
            ConVar.Weather.rainbow = configData.Server.Rainbow = -1;
            ConVar.Weather.thunder = configData.Server.Thunder = -1;
            ConVar.Weather.wind = configData.Server.Wind = -1;
            ConVar.Weather.atmosphere_brightness = configData.Server.AtmosphereBrightness = -1;
            ConVar.Weather.atmosphere_contrast = configData.Server.AtmosphereContrast = -1;
            ConVar.Weather.atmosphere_directionality = configData.Server.AtmosphereDirectionality = -1;
            ConVar.Weather.atmosphere_mie = configData.Server.AtmosphereMie = -1;
            ConVar.Weather.atmosphere_rayleigh = configData.Server.AtmosphereRayleigh = -1;
            ConVar.Weather.cloud_attenuation = configData.Server.CloudAttenuation = -1;
            ConVar.Weather.cloud_brightness = configData.Server.CloudBrightness = -1;
            ConVar.Weather.cloud_coloring = configData.Server.CloudColoring = -1;
            ConVar.Weather.cloud_coverage = configData.Server.CloudCoverage = -1;
            ConVar.Weather.cloud_opacity = configData.Server.CloudOpacity = -1;
            ConVar.Weather.cloud_saturation = configData.Server.CloudSaturation = -1;
            ConVar.Weather.cloud_scattering = configData.Server.CloudScattering = -1;
            ConVar.Weather.cloud_sharpness = configData.Server.CloudSharpness = -1;
            ConVar.Weather.cloud_size = configData.Server.CloudSize = -1;
            ConVar.Weather.clear_chance = configData.Server._clearChance = -1;
            ConVar.Weather.dust_chance = configData.Server._dustChance = -1;
            ConVar.Weather.fog_chance = configData.Server._fogChance = -1;
            ConVar.Weather.overcast_chance = configData.Server._overcastChance = -1;
            ConVar.Weather.rain_chance = configData.Server._rainChance = -1;
            ConVar.Weather.storm_chance = configData.Server._stormChance = -1;
            ConVar.Env.progresstime = configData.Server._progressTime = true;
        
            SaveConfig();
            SendReplicatedVarsAll();
            OpenServerEnvironmentEditor(player);
        }

        [ConsoleCommand("pe.editvalue")]
        private void ccmdEditValue(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            EnvironmentInfo environmentInfo;
            if (!userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
                return;

            string propertyName = arg.GetString(0);
            float value = arg.GetFloat(1);

            switch (propertyName)
            {
                case "fog":
                    environmentInfo.Fog = value;
                    break;
                case "rain":
                    environmentInfo.Rain = value;
                    break;
                case "rainbow":
                    environmentInfo.Rainbow = value;
                    break;
                case "thunder":
                    environmentInfo.Thunder = value;
                    break;
                case "wind":
                    environmentInfo.Wind = value;
                    break;
                case "atmosphere_brightness":
                    environmentInfo.AtmosphereBrightness = value;
                    break;
                case "atmosphere_contrast":
                    environmentInfo.AtmosphereContrast = value;
                    break;
                case "atmosphere_directionality":
                    environmentInfo.AtmosphereDirectionality = value;
                    break;
                case "atmosphere_mie":
                    environmentInfo.AtmosphereMie = value;
                    break;
                case "atmosphere_rayleigh":
                    environmentInfo.AtmosphereRayleigh = value;
                    break;
                case "cloud_attenuation":
                    environmentInfo.CloudAttenuation = value;
                    break;
                case "cloud_brightness":
                    environmentInfo.CloudBrightness = value;
                    break;
                case "cloud_coloring":
                    environmentInfo.CloudColoring = value;
                    break;
                case "cloud_coverage":
                    environmentInfo.CloudCoverage = value;
                    break;
                case "cloud_opacity":
                    environmentInfo.CloudOpacity = value;
                    break;
                case "cloud_saturation":
                    environmentInfo.CloudSaturation = value;
                    break;
                case "cloud_scattering":
                    environmentInfo.CloudScattering = value;
                    break;
                case "cloud_sharpness":
                    environmentInfo.CloudSharpness = value;
                    break;
                case "cloud_size":
                    environmentInfo.CloudSize = value;
                    break;
                case "time":
                    environmentInfo.Time = value;
                    break;
                default:
                    break;
            }

            environmentInfo.SendCustomReplicatedVars(player);

            OpenEnvironmentEditor(player, environmentInfo);
        }

        [ConsoleCommand("pe.server.editvalue")]
        private void ccmdEditServerValue(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                player.ChatMessage(_msg("Error.NoPermission", player.UserIDString));
                return;
            }

            string propertyName = arg.GetString(0);
            float value = arg.GetFloat(1);

            switch (propertyName)
            {
                case "fog":
                    ConVar.Weather.fog = configData.Server.Fog = value;                    
                    break;
                case "rain":
                    ConVar.Weather.rain = configData.Server.Rain = value;
                    break;
                case "rainbow":
                    ConVar.Weather.rainbow = configData.Server.Rainbow = value;
                    break;
                case "thunder":
                    ConVar.Weather.thunder = configData.Server.Thunder = value;
                    break;
                case "wind":
                    ConVar.Weather.wind = configData.Server.Wind = value;
                    break;
                case "atmosphere_brightness":
                    ConVar.Weather.atmosphere_brightness = configData.Server.AtmosphereBrightness = value;
                    break;
                case "atmosphere_contrast":
                    ConVar.Weather.atmosphere_contrast = configData.Server.AtmosphereContrast = value;
                    break;
                case "atmosphere_directionality":
                    ConVar.Weather.atmosphere_directionality = configData.Server.AtmosphereDirectionality = value;
                    break;
                case "atmosphere_mie":
                    ConVar.Weather.atmosphere_mie = configData.Server.AtmosphereMie = value;
                    break;
                case "atmosphere_rayleigh":
                    ConVar.Weather.atmosphere_rayleigh = configData.Server.AtmosphereRayleigh = value;
                    break;
                case "cloud_attenuation":
                    ConVar.Weather.cloud_attenuation = configData.Server.CloudAttenuation = value;
                    break;
                case "cloud_brightness":
                    ConVar.Weather.cloud_brightness = configData.Server.CloudBrightness = value;
                    break;
                case "cloud_coloring":
                    ConVar.Weather.cloud_coloring = configData.Server.CloudColoring = value;
                    break;
                case "cloud_coverage":
                    ConVar.Weather.cloud_coverage = configData.Server.CloudCoverage = value;
                    break;
                case "cloud_opacity":
                    ConVar.Weather.cloud_opacity = configData.Server.CloudOpacity = value;
                    break;
                case "cloud_saturation":
                    ConVar.Weather.cloud_saturation = configData.Server.CloudSaturation = value;
                    break;
                case "cloud_scattering":
                    ConVar.Weather.cloud_scattering = configData.Server.CloudScattering = value;
                    break;
                case "cloud_sharpness":
                    ConVar.Weather.cloud_sharpness = configData.Server.CloudSharpness = value;
                    break;
                case "cloud_size":
                    ConVar.Weather.cloud_size = configData.Server.CloudSize = value;
                    break;
                case "time":
                    ConVar.Env.time = configData.Server.Time = value;
                    break;
                case "clear_chance":
                    ConVar.Weather.clear_chance = configData.Server._clearChance = Mathf.Clamp01(value);
                    break;
                case "dust_chance":
                    ConVar.Weather.dust_chance = configData.Server._dustChance = Mathf.Clamp01(value);
                    break;
                case "fog_chance":
                    ConVar.Weather.fog_chance = configData.Server._fogChance = Mathf.Clamp01(value);
                    break;
                case "overcast_chance":
                    ConVar.Weather.overcast_chance = configData.Server._overcastChance = Mathf.Clamp01(value);
                    break;
                case "rain_chance":
                    ConVar.Weather.rain_chance = configData.Server._rainChance = Mathf.Clamp01(value);
                    break;
                case "storm_chance":
                    ConVar.Weather.storm_chance = configData.Server._stormChance = Mathf.Clamp01(value);
                    break;
                case "progress_time":
                    ConVar.Env.progresstime = configData.Server._progressTime = value < 0.5f ? false : true;
                    break;
                default:
                    break;
            }

            SaveConfig();
            SendReplicatedVarsAll();
            OpenServerEnvironmentEditor(player);
        }
        #endregion

        #region Commands
        [ChatCommand("env")]
        private void cmdEnv(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, configData.TimePermission) && !permission.UserHasPermission(player.UserIDString, configData.WeatherPermission))
            {
                player.ChatMessage(_msg("Error.NoPermission", player.UserIDString));
                return;
            }

            EnvironmentInfo environmentInfo;

            if (!userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
                userEnvironmentInfo[player.userID] = environmentInfo = new EnvironmentInfo();

            if (environmentInfo.HasZoneOverride())
            {
                player.ChatMessage(_msg("Notification.HasZoneOverride", player.UserIDString));
                return;
            }

            OpenDummyContainer(player);
            OpenEnvironmentEditor(player, environmentInfo);
        }

        [ChatCommand("senv")]
        private void cmdServerEnv(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                player.ChatMessage(_msg("Error.NoPermission", player.UserIDString));
                return;
            }

            OpenDummyContainer(player);
            OpenServerEnvironmentEditor(player);
        }

        [ChatCommand("mytime")]
        private void cmdMyTime(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, configData.TimePermission))
            {
                player.ChatMessage(_msg("Error.NoPermission", player.UserIDString));
                return;
            }

            float time;
            if (args.Length != 1 || !float.TryParse(args[0], out time))
            {
                player.ChatMessage(_msg("Error.NoTimeVar", player.UserIDString));
                return;
            }

            EnvironmentInfo environmentInfo;

            if (!userEnvironmentInfo.TryGetValue(player.userID, out environmentInfo))
                userEnvironmentInfo[player.userID] = environmentInfo = new EnvironmentInfo();

            if (environmentInfo.HasZoneOverride())
            {
                player.ChatMessage(_msg("Notification.HasZoneOverride", player.UserIDString));
                return;
            }

            environmentInfo.Time = Mathf.Clamp(time, -1, 24);
            player.ChatMessage(string.Format(_msg("Notification.SetTime", player.UserIDString), environmentInfo.Time));
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty("Save players custom environment settings and apply after restart/relog")]
            public bool EnableSaving { get; set; }

            [JsonProperty("Custom permission to change time")]
            public string TimePermission { get; set; }

            [JsonProperty("Custom permission to change weather")]
            public string WeatherPermission { get; set; }

            [JsonProperty("Zone Environment Profiles. (To disable a variable and use the value set on the server, set the option to -1)")]
            public Dictionary<string, EnvironmentInfo> Zones { get; set; }

            [JsonProperty("Server Environment Profile")]
            public ServerEnvironmentInfo Server { get; set; }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                EnableSaving = true,
                TimePermission = "preferredenvironment.use",
                WeatherPermission = "preferredenvironment.use",

                Zones = new Dictionary<string, EnvironmentInfo>
                {
                    ["ExampleZoneID"] = new EnvironmentInfo
                    {
                        _fog = -1f,
                        _rain = -1f,
                        _time = -1f,
                        _wind = -1f,
                        _atmosphere_brightness = -1f,
                        _atmosphere_contrast = -1f,
                        _atmosphere_directionality = -1f,
                        _atmosphere_mie = -1f,
                        _atmosphere_rayleigh = -1f,
                        _cloud_attenuation = -1f,
                        _cloud_brightness = -1f,
                        _cloud_coloring = -1f,
                        _cloud_coverage = -1f,
                        _cloud_opacity = -1f,
                        _cloud_saturation = -1f,
                        _cloud_scattering = -1f,
                        _cloud_sharpness = -1f,
                        _cloud_size = -1f,
                        _rainbow = -1f,
                        _thunder = -1f,                        
                    }
                },
                Server = new ServerEnvironmentInfo()
                {
                    _fog = -1f,
                    _rain = -1f,
                    _time = -1f,
                    _wind = -1f,
                    _atmosphere_brightness = -1f,
                    _atmosphere_contrast = -1f,
                    _atmosphere_directionality = -1f,
                    _atmosphere_mie = -1f,
                    _atmosphere_rayleigh = -1f,
                    _cloud_attenuation = -1f,
                    _cloud_brightness = -1f,
                    _cloud_coloring = -1f,
                    _cloud_coverage = -1f,
                    _cloud_opacity = -1f,
                    _cloud_saturation = -1f,
                    _cloud_scattering = -1f,
                    _cloud_sharpness = -1f,
                    _cloud_size = -1f,
                    _rainbow = -1f,
                    _thunder = -1f,
                    _clearChance = -1,
                    _dustChance = -1,
                    _fogChance = -1,
                    _overcastChance = -1,
                    _progressTime = true,
                    _rainChance = -1,
                    _stormChance = -1
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(2, 0, 0))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(2, 0, 6) || configData.Server == null)
                configData.Server = baseConfig.Server;

            if (configData.Version < new VersionNumber(2, 0, 9))
            {
                configData.TimePermission = configData.WeatherPermission = baseConfig.TimePermission;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        private class ServerEnvironmentInfo : EnvironmentInfo
        {
            [JsonProperty("Progress Time")]
            public bool _progressTime = true;

            [JsonProperty("Storm Chance")]
            public float _stormChance = -1f;

            [JsonProperty("Rain Chance")]
            public float _rainChance = -1f;

            [JsonProperty("Overcast Chance")]
            public float _overcastChance = -1f;

            [JsonProperty("Fog Chance")]
            public float _fogChance = -1f;

            [JsonProperty("Dust Chance")]
            public float _dustChance = -1f;

            [JsonProperty("Clear Chance")]
            public float _clearChance = -1f;
        }

        private class EnvironmentInfo
        {
            [JsonProperty("Time (0.0 - 24.0)")]
            public float _time = -1f;

            [JsonProperty("Rain (0.0 - 1.0)")]
            public float _rain = -1f;

            [JsonProperty("Wind (0.0 - 1.0)")]
            public float _wind = -1f;

            [JsonProperty("Fog (0.0 - 1.0)")]
            public float _fog = -1f;

            [JsonProperty("Rainbow (0.0 - 1.0)")]
            public float _rainbow = -1f;

            [JsonProperty("Thunder (0.0 - 1.0)")]
            public float _thunder = -1f;

            [JsonProperty("Atmosphere Brightness (0.0 - 1.0)")]
            public float _atmosphere_brightness = -1f;

            [JsonProperty("Atmosphere Contrast (0.0 - 1.0)")]
            public float _atmosphere_contrast = -1f;

            [JsonProperty("Atmosphere Directionality (0.0 - 1.0)")]
            public float _atmosphere_directionality = -1f;

            [JsonProperty("Atmosphere Mie (0.0 - 1.0)")]
            public float _atmosphere_mie = -1f;

            [JsonProperty("Atmosphere Rayleigh (0.0 - 1.0)")]
            public float _atmosphere_rayleigh = -1f;

            [JsonProperty("Cloud Attenuation (0.0 - 1.0)")]
            public float _cloud_attenuation = -1f;

            [JsonProperty("Cloud Brightness (0.0 - 1.0)")]
            public float _cloud_brightness = -1f;

            [JsonProperty("Cloud Coloring (0.0 - 1.0)")]
            public float _cloud_coloring = -1f;

            [JsonProperty("Cloud Coverage (0.0 - 1.0)")]
            public float _cloud_coverage = -1f;

            [JsonProperty("Cloud Opacity (0.0 - 1.0)")]
            public float _cloud_opacity = -1f;

            [JsonProperty("Cloud Saturation (0.0 - 1.0)")]
            public float _cloud_saturation = -1f;

            [JsonProperty("Cloud Scattering (0.0 - 1.0)")]
            public float _cloud_scattering = -1f;

            [JsonProperty("Cloud Sharpness (0.0 - 1.0)")]
            public float _cloud_sharpness = -1f;

            [JsonProperty("Cloud Size (0.0 - 1.0)")]
            public float _cloud_size = -1f;

            [JsonIgnore]
            public float Rain
            {
                get { return _rain; }
                set { _rain = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float Wind
            {
                get { return _wind; }
                set { _wind = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float Fog
            {
                get { return _fog; }
                set { _fog = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float AtmosphereBrightness
            {
                get { return _atmosphere_brightness; }
                set { _atmosphere_brightness = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float AtmosphereContrast
            {
                get { return _atmosphere_contrast; }
                set { _atmosphere_contrast = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }
            [JsonIgnore]
            public float AtmosphereDirectionality
            {
                get { return _atmosphere_directionality; }
                set { _atmosphere_directionality = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float AtmosphereMie
            {
                get { return _atmosphere_mie; }
                set { _atmosphere_mie = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float AtmosphereRayleigh
            {
                get { return _atmosphere_rayleigh; }
                set { _atmosphere_rayleigh = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudAttenuation
            {
                get { return _cloud_attenuation; }
                set { _cloud_attenuation = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudBrightness
            {
                get { return _cloud_brightness; }
                set { _cloud_brightness = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudColoring
            {
                get { return _cloud_coloring; }
                set { _cloud_coloring = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudCoverage
            {
                get { return _cloud_coverage; }
                set { _cloud_coverage = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudOpacity
            {
                get { return _cloud_opacity; }
                set { _cloud_opacity = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudSaturation
            {
                get { return _cloud_saturation; }
                set { _cloud_saturation = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudScattering
            {
                get { return _cloud_scattering; }
                set { _cloud_scattering = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudSharpness
            {
                get { return _cloud_sharpness; }
                set { _cloud_sharpness = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float CloudSize
            {
                get { return _cloud_size; }
                set { _cloud_size = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float Rainbow
            {
                get { return _rainbow; }
                set { _rainbow = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float Thunder
            {
                get { return _thunder; }
                set { _thunder = value >= 0f ? Mathf.Clamp(value, 0, 1f) : -1f; VarsDirty = true; }
            }

            [JsonIgnore]
            public float Time
            {
                get { return _time; }
                set { _time = value >= 0f ? Mathf.Clamp(value, 0, 24f) : -1f; }
            }

            [JsonIgnore]
            private Hash<string, EnvironmentInfo> zoneOverrides;

            [JsonIgnore]
            private string currentZoneId = string.Empty;

            public bool HasZoneOverride()
            {
                if (zoneOverrides == null || currentZoneId == string.Empty || zoneOverrides.Count == 0)                
                    return false;
                
                return true;
            }

            public bool GetZoneOverride(out EnvironmentInfo environmentInfo)
            {
                if (zoneOverrides == null || currentZoneId == string.Empty || zoneOverrides.Count == 0)
                {
                    environmentInfo = null;
                    return false;
                }

                environmentInfo = zoneOverrides[currentZoneId];
                return true;
            }

            public void OnEnterZone(string zoneId, EnvironmentInfo environmentInfo)
            {
                if (zoneOverrides == null)
                    zoneOverrides = new Hash<string, EnvironmentInfo>();

                currentZoneId = zoneId;
                zoneOverrides[zoneId] = environmentInfo;
            }

            public void OnExitZone(string zoneId, BasePlayer player)
            {
                if (zoneOverrides != null)
                {
                    zoneOverrides.Remove(zoneId);
                    if (currentZoneId == zoneId)
                    {
                        if (zoneOverrides.Count > 0)
                        {
                            foreach (var kvp in zoneOverrides)
                            {
                                currentZoneId = kvp.Key;
                                return;
                            }
                        }
                        else currentZoneId = string.Empty;
                    }
                }
            }

            [JsonIgnore]
            public bool HasReplicatedVars => replicatedVars?.Count > 0;

            [JsonIgnore]
            public bool VarsDirty = true;

            [JsonIgnore]
            private Hash<string, string> replicatedVars;
            
            [JsonIgnore]
            public float GetDesiredTime => HasZoneOverride() ? zoneOverrides[currentZoneId].Time : Time;

            public bool ShouldRemove()
            {
                if (HasZoneOverride())
                    return false;

                if (VarsDirty)
                    BuildReplicatedConvarList();

                if (replicatedVars?.Count > 0 || Time >= 0f)
                    return false;

                return true;                
            }
           
            public void SendCustomReplicatedVars(BasePlayer player)
            {
                if (HasZoneOverride())
                {
                    zoneOverrides[currentZoneId].SendCustomReplicatedVars(player);
                    return;
                }

                if (VarsDirty)
                    BuildReplicatedConvarList();

                if (Net.sv.write.Start())
                {
                    Net.sv.write.PacketID(Message.Type.ConsoleReplicatedVars);
                    Net.sv.write.Int32(weatherConvars.Count);

                    foreach (KeyValuePair<string, ConsoleSystem.Command> kvp in weatherConvars)
                    {
                        Net.sv.write.String(kvp.Key);
                        Net.sv.write.String(replicatedVars.ContainsKey(kvp.Key) ? replicatedVars[kvp.Key] : kvp.Value.String);
                    }
                    
                    Net.sv.write.Send(new SendInfo(player.net.connection));
                }
            }

            public void BuildReplicatedConvarList()
            {
                if (replicatedVars == null)
                    replicatedVars = new Hash<string, string>();

                replicatedVars.Clear();

                if (_atmosphere_brightness >= 0)
                    replicatedVars["weather.atmosphere_brightness"] = _atmosphere_brightness.ToString();

                if (_atmosphere_contrast >= 0)
                    replicatedVars["weather.atmosphere_contrast"] = _atmosphere_contrast.ToString();

                if (_atmosphere_directionality >= 0)
                    replicatedVars["weather.atmosphere_directionality"] = _atmosphere_directionality.ToString();

                if (_atmosphere_directionality >= 0)
                    replicatedVars["weather.atmosphere_directionality"] = _atmosphere_directionality.ToString();

                if (_atmosphere_mie >= 0)
                    replicatedVars["weather.atmosphere_mie"] = _atmosphere_mie.ToString();

                if (_atmosphere_rayleigh >= 0)
                    replicatedVars["weather.atmosphere_rayleigh"] = _atmosphere_rayleigh.ToString();

                if (_cloud_attenuation >= 0)
                    replicatedVars["weather.cloud_attenuation"] = _cloud_attenuation.ToString();

                if (_cloud_brightness >= 0)
                    replicatedVars["weather.cloud_brightness"] = _cloud_brightness.ToString();

                if (_cloud_coloring >= 0)
                    replicatedVars["weather.cloud_coloring"] = _cloud_coloring.ToString();

                if (_cloud_coverage >= 0)
                    replicatedVars["weather.cloud_coverage"] = _cloud_coverage.ToString();

                if (_cloud_opacity >= 0)
                    replicatedVars["weather.cloud_opacity"] = _cloud_opacity.ToString();

                if (_cloud_saturation >= 0)
                    replicatedVars["weather.cloud_saturation"] = _cloud_saturation.ToString();

                if (_cloud_scattering >= 0)
                    replicatedVars["weather.cloud_scattering"] = _cloud_scattering.ToString();

                if (_cloud_sharpness >= 0)
                    replicatedVars["weather.cloud_sharpness"] = _cloud_sharpness.ToString();

                if (_cloud_size >= 0)
                    replicatedVars["weather.cloud_size"] = _cloud_size.ToString();

                if (_fog >= 0)
                    replicatedVars["weather.fog"] = _fog.ToString();

                if (_rain >= 0)
                    replicatedVars["weather.rain"] = _rain.ToString();

                if (_rainbow >= 0)
                    replicatedVars["weather.rainbow"] = _rainbow.ToString();

                if (_thunder >= 0)
                    replicatedVars["weather.thunder"] = _thunder.ToString();

                if (_wind >= 0)
                    replicatedVars["weather.wind"] = _wind.ToString();

                if (replicatedVars.Count > 0)
                {
                    replicatedVars["weather.clear_chance"] = "0";
                    replicatedVars["weather.dust_chance"] = "0";
                    replicatedVars["weather.fog_chance"] = "0";
                    replicatedVars["weather.overcast_chance"] = "0";
                    replicatedVars["weather.rain_chance"] = "0";
                    replicatedVars["weather.storm_chance"] = "0";
                }

                VarsDirty = false;
            }
        }

    }
}
