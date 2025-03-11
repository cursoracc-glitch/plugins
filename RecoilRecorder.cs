
/*
 ########### README ####################################################
 #                                                                     #
 #   1. If you found a bug, please report them to developer!           #
 #   2. Don't edit that file (edit files only in CONFIG/LANG/DATA)     #
 #                                                                     #
 ########### CONTACT INFORMATION #######################################
 #                                                                     #
 #   Website: https://rustworkshop.space/                              #
 #   Discord: Orange#0900                                              #
 #   Email: official.rustworkshop@gmail.com                            #
 #                                                                     #
 #######################################################################
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    // Creation date: 09-11-2020
    // Last update date: 20-02-2021
    [Info("Recoil Recorder", "Orange", "1.1.6")]
    [Description("https://rustworkshop.space/resources/recoil-recorder.247/")]
    public class RecoilRecorder : RustPlugin
    {
        #region Vars

        //Default hit symbol = U+25CF
        private const string hookOnRecorded = "OnRecoilRecorded"; // BasePlayer player, int[] shots, int[] pattern
        private const int border = 135;
        private static Dictionary<string, int[]> patternsList = new Dictionary<string, int[]>();
        private const string urlPatterns = "https://api.rustworkshop.space/Files/RecoilPatterns.json";
        private const float shootingCooldown = 3f;

        #endregion
        
        #region Oxide Hooks

        private void Init()
        {
            BuildUI();

            if (config.activeZonesIdOrName.Length == 0)
            {
                Unsubscribe(nameof(OnEnterZone));
                Unsubscribe(nameof(OnExitZone));
            }
        }

        private void OnServerInitialized()
        {
            LoadPatterns();
            timer.Once(3f, CheckPlayers);
        }

        private void Unload()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<ScriptRecorder>())
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            var obj = player.GetComponent<ScriptRecorder>();
            if (obj == null)
            {
                obj = player.gameObject.AddComponent<ScriptRecorder>();
            }
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectiles)
        {
            var obj = player.GetComponent<ScriptRecorder>();
            if (obj != null)
            {
                obj.OnFired(projectile);
            }
        }
        
        private void OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            var obj = player.GetComponent<ScriptRecorder>();
            if (obj != null)
            {
                obj.OnReloaded();
            }
        }

        private void OnEnterZone(string ZoneID, BasePlayer player)
        {
            if (Match(ZoneID))
            {
                var obj = player.GetComponent<ScriptRecorder>();
                if (obj != null)
                {
                    obj.Enable();
                }
            }
        }

        private void OnExitZone(string ZoneID, BasePlayer player)
        {
            if (Match(ZoneID))
            {
                var obj = player.GetComponent<ScriptRecorder>();
                if (obj != null)
                {
                    obj.Disable();
                }
            }
        }

        #endregion
        
        #region Core

        private void CheckPlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }

            if (config.activeZonesIdOrName.Length > 0)
            {
                var players = new List<BasePlayer>();
                foreach (var value in config.activeZonesIdOrName)
                {
                    var members = Zone.GetPlayersInZone(value);
                    if (members != null)
                    {
                        foreach (var member in members)
                        {
                            if (players.Contains(member) == false)
                            {
                                players.Add(member);
                            }
                        }
                    }
                }

                foreach (var player in players.Distinct())
                {
                    if (player.IsValid() == true)
                    {
                        var obj = player.GetComponent<ScriptRecorder>();
                        if (obj != null)
                        {
                            obj.Enable();
                        }
                    }
                }
            }
        }

        private bool Match(string zoneId)
        {
            var name = Zone.GetName(zoneId)?.ToLower();
            var id = zoneId.ToLower();
            return config.activeZonesIdOrName.Any(x => x.ToLower() == name || x.ToLower() == id);
        }

        private void LoadPatterns() 
        {
            webrequest.Enqueue(urlPatterns, "", (i, s) =>
            {
                try
                {
                    var dic = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(s);
                    patternsList = dic;
                    Puts($"Loaded x{patternsList.Count} weapon patterns");
                }
                catch (Exception e)
                {
                    patternsList = new Dictionary<string, int[]>
                {
                    {
                        "ak47u.entity", new[]
                        {
                            0,
                            12,
                            12,
                            27,
                            51,
                            51,
                            44,
                            33,
                            17,
                            2,
                            -16,
                            -32,
                            -44,
                            -51,
                            -51,
                            -45,
                            -36,
                            -19,
                            -6,
                            17,
                            32,
                            52,
                            66,
                            75,
                            80,
                            75,
                            62,
                            42,
                            25,
                            6,
                        }
                    },
                    {
                        "lr300.entity", new[]
                        {
                            0,
                            1,
                            3,
                            8,
                            12,
                            20,
                            25,
                            29,
                            30,
                            26,
                            20,
                            11,
                            4,
                            0,
                            0,
                            3,
                            6,
                            11,
                            17,
                            23,
                            29,
                            35,
                            40,
                            45,
                            48,
                            49,
                            44,
                            32,
                            17,
                            5
                        }
                    }
                };
                
                PrintError("Failed to load weapon patterns from website! Using x2 default patterns...");
                }
            }, this);
        }

        #endregion

        #region Graphical Interface

        private const string elemMain = "recoil.main";
        private const string elemPattern = "recoil.pattern";
        private static string jsonMain;
        private const int startY = -50;
        private const int offsetY = -6;
        private const int sizePanel = 250;

        private void BuildUI()
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = elemMain,
                Components = 
                {
                    new CuiImageComponent
                    {
                        Color = "0.5 0.5 0.5 0.7",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = config.uiPositionAnchor,
                        AnchorMax = config.uiPositionAnchor,
                        OffsetMin = $"-{sizePanel} -{sizePanel}",
                        OffsetMax = "0 0"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Parent = elemMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = config.textHeader.Replace("{version}", Version.ToString()),
                        Align = TextAnchor.UpperCenter,
                        FontSize = 15,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.95",
                    }
                }
            });

            jsonMain = container.ToString();
        }

        private static void ShowPattern(BasePlayer player, int[] pattern)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Parent = elemMain,
                Name = elemPattern,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            if (pattern != null)
            {
                for (var i = pattern.Length; i > 0; i--)
                {
                    var value = -pattern[i - 1]; 
                    var x = sizePanel / 2 + value;
                    var y = i * offsetY + startY;
                
                    if (x < 0 || x > sizePanel)
                    {
                        continue;
                    }
                    
                    if (Math.Abs(y) > sizePanel + offsetY)
                    {
                        continue;
                    }
                
                    container.Add(new CuiElement
                    {
                        Parent = elemPattern,
                        Components =
                        { 
                            new CuiTextComponent
                            {
                                Color = "1 1 1 1",
                                Text = config.hitSymbol,
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 10,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"{x - 15} {y - 15}",
                                OffsetMax = $"{x + 15} {y + 15}"
                            }
                        }
                    });
                }
            }

            CuiHelper.DestroyUi(player, elemPattern);
            CuiHelper.AddUi(player, container);
        }
        
        private static void ShowShot(BasePlayer player, int shotValue, int shotCount, bool hit)
        {
            var container = new CuiElementContainer();
            var x = sizePanel / 2 + shotValue;
            var y = shotCount * offsetY + startY;

            if (x < 0 || x > sizePanel)
            {
                return;
            }

            if (Math.Abs(y) > sizePanel + offsetY)
            {
                return;
            }
            
            container.Add(new CuiElement
            {
                Parent = elemPattern,
                Components =
                { 
                    new CuiTextComponent
                    {
                        Color = hit ? "0 1 0 1" : "1 0 0 1",
                        Text = config.hitSymbol,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"{x - 15} {y - 15}",
                        OffsetMax = $"{x + 15} {y + 15}"
                    }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #endregion 
        
        #region Classes
 
        private static ConfigDefinition config = new ConfigDefinition();

        private class ConfigDefinition
        {
            [JsonProperty("Active zones (name or id)")]
            public string[] activeZonesIdOrName =
            {
                "test1",
                "zone2",
                "orange#1",
                "94402235",
            };

            [JsonProperty("Hit Symbol")]
            public string hitSymbol = "●";

            [JsonProperty("Header text")]
            public string textHeader = "Recoil Recorder v{version}";
            
            [JsonProperty("Maximal distance between shots to count hit")]
            public int hitMaxDistance = 10;
            
            [JsonProperty("UI Position (Anchor)")]
            public string uiPositionAnchor = "1 1";
        }

        #endregion

        #region Script Recorder

        private class ScriptRecorder : MonoBehaviour
        {
            private List<int> shots = new List<int>();
            private BaseProjectile lastWeapon;
            private BasePlayer player;
            private float firstShot = 0;
            private float lastShootTime;
            private bool showUi = false;
            
            public int[] Pattern = {};
            public int[] Shots => shots.ToArray();
            private BasePlayer[] spectators => player.children.Where(x => x != null && x is BasePlayer).Select(x => x as BasePlayer).ToArray();

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            private void Start()
            {
                player.ConsoleMessage($"[{nameof(RecoilRecorder)}] Loaded plugin by Orange");
                InvokeRepeating(nameof(CheckConnection), 60, 60);
            }

            public void Enable()
            {
                CuiHelper.DestroyUi(player, elemMain);
                CuiHelper.AddUi(player, jsonMain);
                showUi = true;
            }

            public void Disable()
            {
                showUi = false;
                CuiHelper.DestroyUi(player, elemMain);
            }

            private void OnDestroy()
            { 
               Disable();
            }

            public void OnFired(BaseProjectile weapon)
            {
                CheckWeapon(weapon);
                AddValue();

                var didHit = false;
                var index = shots.Count - 1;
                var shotX = shots[index];

                if (Pattern == null || Pattern.Length < shots.Count)
                {
                    return;
                }
                
                var patternX = -Pattern[index];
                    
                if (Math.Abs(shotX) < config.hitMaxDistance)
                {
                    shotX = patternX;
                    didHit = true;
                }

                lastShootTime = Time.realtimeSinceStartup;

                if (showUi)
                {
                    ShowShot(player, shotX, index, didHit);
                }
                   
                foreach (var spectator in spectators)
                {
                    ShowShot(spectator, shotX, index, didHit);
                }
            }
            
            public void OnReloaded()
            {
                Interface.Call(hookOnRecorded, player, Shots, Pattern);
                ClearShots();
            }

            private void AddValue()
            {
                var current = player.eyes.rotation.eulerAngles.y;
                var value = 0f;
                if (shots.Count == 0)
                {
                    firstShot = current;
                    shots.Add(0);
                    return;
                }

                if (firstShot > border)
                {
                    if (current < border)
                    {
                        value = 360 - firstShot + current;
                    }
                    else
                    {
                        value = current - firstShot;
                    }
                }
                else
                {
                    if (current > border)
                    {
                        value = -(360 - current);
                    }
                    else
                    {
                        value = current - firstShot;
                    }
                }
                value *= 10;
                shots.Add(Convert.ToInt32(value));
            }

            private void CheckWeapon(BaseProjectile weapon)
            {
                if (lastWeapon != weapon)
                { 
                    player.ConsoleMessage($"[{nameof(RecoilRecorder)}] Weapon was changed to {weapon.ShortPrefabName}");
                    lastWeapon = weapon;
                     
                    if (patternsList.TryGetValue(weapon.ShortPrefabName, out Pattern) == false)
                    {
                        Pattern = new int[]{};
                    }
                    
                    ClearShots();
                    return;
                }
                
                if (weapon != null && shots.Count >= weapon.primaryMagazine.capacity)
                {
                    ClearShots();
                    return;
                }

                if (Time.realtimeSinceStartup > lastShootTime + shootingCooldown)
                {
                    ClearShots();
                }
            }

            private void ClearShots()
            {
                player.ConsoleMessage($"[{nameof(RecoilRecorder)}] Clearing shots");
                shots.Clear();

                if (showUi)
                {
                    ShowPattern(player, Pattern);
                }

                foreach (var spectator in spectators)
                {
                    CuiHelper.DestroyUi(spectator, elemMain);
                    CuiHelper.AddUi(spectator, jsonMain);
                    ShowPattern(spectator, Pattern);
                }
            }

            private void CheckConnection()
            {
                if (player.IsConnected == false)
                {
                    Destroy(this);
                }
            }
        }

        #endregion

        #region Configuration v2.1

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigDefinition>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                config = new ConfigDefinition();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigDefinition();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
        
        #region Zone Manager Helper 1.0.0

        private class Zone 
        {
            private static Oxide.Core.Libraries.Plugins plugins;
            private const string filename = "ZoneManager";
            private static Plugin plugin;

            public static void Unload()
            {
                plugin = null;
                plugins = null;
            }

            public static string GetName(string id)
            {
                return Call("GetZoneName", id) as string;
            }

            public static Vector3? GetLocation(string id)
            {
                return Call("GetZoneLocation", id) as Vector3?;
            }

            public static string[] GetAllZones()
            {
                return Call("GetZoneIDs") as string[];
            }

            public static List<BasePlayer> GetPlayersInZone(string id)
            {
                return Call("GetPlayersInZone", id) as List<BasePlayer>;
            }

            public static bool IsInside(string id, BasePlayer player)
            {
                return (bool) Call("IsPlayerInZone", id, player);
            }

            private static object Call(string name, params object[] args)
            {
                if (plugin == null)
                {
                    FindPlugin();
                    
                    if (plugin == null)
                    {
                        return null;
                    }
                }

                return plugin.Call(name, args);
            }

            private static void FindPlugin()
            {
                if (plugins == null)
                {
                    plugins = Interface.Oxide.GetLibrary<Oxide.Core.Libraries.Plugins>();
                }

                if (plugins != null)
                {
                    plugin = plugins.GetAll().FirstOrDefault(x => x.Name == filename);
                }
            }
        }

        #endregion
    }
}
