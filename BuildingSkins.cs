using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Building Skins", "Marat", "2.0.3")]
    [Description("Automatic application of DLC skins for building blocks")]
    class BuildingSkins : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        
        #region Field
        
        private const string InitialLayer = "UI_BuildingLayer";
        
        private const string permissionUse = "buildingskins.use";
        private const string permissionAll = "buildingskins.all";
        private const string permissionBuild = "buildingskins.build";
        private const string permissionAdmin = "buildingskins.admin";
        
        private readonly Dictionary<ulong, Coroutine> runningCoroutines = new();
        private readonly Dictionary<BuildingGrade.Enum, List<ulong>> gradesSkin = new();
        
        private readonly Dictionary<uint, string> colors = new()
        {
            [1] = "0.38 0.56 0.74 1.0",  [2] = "0.45 0.71 0.34 1.0",
            [3] = "0.57 0.29 0.83 1.0",  [4] = "0.42 0.17 0.11 1.0",
            [5] = "0.82 0.46 0.13 1.0",  [6] = "0.87 0.87 0.87 1.0",
            [7] = "0.20 0.20 0.18 1.0",  [8] = "0.40 0.33 0.27 1.0",
            [9] = "0.20 0.22 0.34 1.0",  [10] = "0.24 0.35 0.20 1.0",
            [11] = "0.73 0.30 0.18 1.0", [12] = "0.78 0.53 0.39 1.0",
            [13] = "0.84 0.66 0.22 1.0", [14] = "0.34 0.33 0.31 1.0",
            [15] = "0.21 0.34 0.37 1.0", [16] = "0.66 0.61 0.56 1.0"
        };
        
        #endregion
        
        #region Oxide Hooks
        
        private void OnServerInitialized()
        {
            if (ImageLibrary == null)
            {
                PrintError("[ImageLibrary] not found! Plugin is disabled!");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }
            
            LoadData();
            AddCovalenceCommand(config.Commands, nameof(CmdChangeSkin));
            permission.RegisterPermission(permissionUse, this);
            permission.RegisterPermission(permissionAll, this);
            permission.RegisterPermission(permissionBuild, this);
            permission.RegisterPermission(permissionAdmin, this);
            
            foreach (var list in config.BuildingImages)
            {
                var skinId = list.Value.Select(x => x.SkinId).ToList();
                gradesSkin.Add((BuildingGrade.Enum)(list.Key + 1), skinId);
                
                foreach (var info in list.Value.Where(x => !string.IsNullOrEmpty(x.Url)))
                {
                    ImageLibrary.Call("AddImage", info.Url, info.Title);
                    if (config.SeparatePermissions && info.SkinId != 0 && !string.IsNullOrEmpty(info.Title))
                    {
                        if (!permission.PermissionExists(info.Title))
                        {
                            permission.RegisterPermission($"buildingskins.{info.Title}".ToLower(), this);
                        }
                    }
                }
            }
            
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }
        }
        
        private void Unload()
        {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var player = BasePlayer.activePlayerList[i];
                CuiHelper.DestroyUi(player, InitialLayer);
                StopCoroutine(player);
            }
            SaveData();
            runningCoroutines.Clear();
            config = null;
        }
        
        private void OnServerSave() => SaveData();
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }
            StartCoroutine(player, PreloadImages(player));
            if (!storedData.PlayerData.TryGetValue(player.userID, out var data))
            {
                data = new Data
                {
                    ChangeHammer = true, NeedsRepair = true, EnableAnimation = true, RandomColor = false, Color = 9
                };
                storedData.PlayerData[player.userID] = data;
            }
            player.LastBlockColourChangeId = data.RandomColor ? player.LastBlockColourChangeId : data.Color;
        }
        
        private void OnPlayerDisconnected(BasePlayer player) => StopCoroutine(player);
        
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            var data = storedData.PlayerData.TryGetValue(player.userID, out var playerData) ? playerData : null;
            player.LastBlockColourChangeId = data?.RandomColor == false ? data.Color : player.LastBlockColourChangeId;
        }
        
        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || info.HitEntity == null) return;
            if (!permission.UserHasPermission(player.UserIDString, permissionUse)) return;
            var block = info.HitEntity as BuildingBlock;
            if (block == null || !gradesSkin.TryGetValue(block.grade, out var skinId) || skinId == null) return;
            var skinID = GetPlayerSkinID(player, block.grade);
            var playerData = storedData.PlayerData[player.userID];
            //if (skinID == 10226 && block.grade.ToString() == "Wood" && block.prefabID == 870964632) return;
            if (block.skinID != 0 && block.grade.ToString() == "Metal" && !playerData.RandomColor) block.SetCustomColour(player.LastBlockColourChangeId);
            if (block.skinID == skinID) return;
            if ((config.BuildingBlocked && !player.CanBuild() || block.OwnerID != player.userID) && !permission.UserHasPermission(player.UserIDString, permissionAdmin)) return;
            if (!playerData.ChangeHammer || block.health != block.MaxHealth() && !playerData.NeedsRepair) return;
            block.skinID = skinID;
            block.ChangeGradeAndSkin(block.grade, skinID, true, true);
            if (playerData.EnableAnimation) block.ClientRPC(null, "DoUpgradeEffect", (int)block.grade, skinID);
        }
        
        private object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade, ulong skin)
        {
            if (player == null || block == null) return null;
            if (!permission.UserHasPermission(player.UserIDString, permissionUse)) return null;
            if (skin != 0 && player.blueprints.steamInventory.HasItem((int)skin)) return null;
            if (!gradesSkin.TryGetValue(grade, out var skinId) || skinId == null) return null;
            var skinID = GetPlayerSkinID(player, grade);
            var playerData = storedData.PlayerData[player.userID];
            //if (skinID == 10226 && grade.ToString() == "Wood" && block.prefabID == 870964632) return null;
            if (block.skinID != 0 && grade.ToString() == "Metal" && !playerData.RandomColor) block.SetCustomColour(player.LastBlockColourChangeId);
            if (block.skinID == skinID && block.grade == grade) return false;
            NextTick(() =>
            {
                if (block == null || block.IsDestroyed) return;
                block.skinID = skinID;
                block.ChangeGradeAndSkin(block.grade, skinID, true, true);
                if (playerData.EnableAnimation) block.ClientRPC(null, "DoUpgradeEffect", (int)block.grade, skinID);
                ///for plugin BuildingGrades
                if (block.skinID != 0 && grade.ToString() == "Metal" && !playerData.RandomColor) block.SetCustomColour(player.LastBlockColourChangeId);
            });
            return null;
        }
        
        ///for plugin BuildingGrades
        private void OnStructureGradeUpdated(BuildingBlock block, BasePlayer player, BuildingGrade.Enum oldGrade, BuildingGrade.Enum newGrade)
        {
            OnStructureUpgrade(block, player, newGrade, block.skinID);
        }
        
        #endregion
        
        #region Configuration
        
        private static PluginConfig config;
        
        private class PluginConfig
        {
            [JsonProperty("Building skin change commands")] public string[] Commands;
            [JsonProperty("Block building skin in building blocked")] public bool BuildingBlocked;
            [JsonProperty("Number of blocks updated per tick")] public int UpdatesPerTick;
            [JsonProperty("Use separate permissions for skins")] public bool SeparatePermissions;
            [JsonProperty("Image and description settings")] public Dictionary<int, List<BlockInfo>> BuildingImages;
            public Oxide.Core.VersionNumber Version;
        }
        
        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                Commands = new string[] {"bskin", "building.skin"},
                BuildingBlocked = true,
                UpdatesPerTick = 5,
                SeparatePermissions = false,
                BuildingImages = new Dictionary<int, List<BlockInfo>>
                {
                    [0] = new List<BlockInfo>
                    {
                        new("Wood", "https://i.ibb.co/yqsWpbp/wood.png", 0),
                        new("Frontier", "https://i.ibb.co/b2bZFXj/frontier.png", 10232),
                        new("Gingerbread", "https://i.ibb.co/Tw67yBM/gingerbread.png", 2)
                    },
                    [1] = new List<BlockInfo>
                    {
                        new("Stone", "https://i.ibb.co/jw9FJFP/stone.png", 0),
                        new("Adobe", "https://i.ibb.co/Ky1MBJ7/adobe.png", 10220),
                        new("Brick", "https://i.ibb.co/vjqh3Hj/brick.png", 10223),
                        new("Brutalist", "https://i.ibb.co/86bpvS2/brutalist.png", 10225)
                    },
                    [2] = new List<BlockInfo>
                    {
                        new("Metal", "https://i.ibb.co/M9RPSZ2/metal.png", 0),
                        new("Container", "https://i.ibb.co/YWzfwS4/container.png", 10221)
                    },
                    [3] = new List<BlockInfo>
                    {
                        new("TopTier", "https://i.ibb.co/T0Nwfvp/toptire.png", 0)
                    }
                },
                Version = Version
            };
        }
        
        private class BlockInfo
        {
            public string Title;
            public string Url;
            public ulong SkinId;
            
            public BlockInfo(string title, string url, ulong skinId)
            {
                Title = title;
                Url = url;
                SkinId = skinId;
            }
        }
        
        protected override void SaveConfig() => Config.WriteObject(config);
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<PluginConfig>();
                if (config == null) LoadDefaultConfig();
                if (config.Version < Version)
                {
                    PrintWarning("Config update detected! Updating config values...");
                    if (config.Version < new Core.VersionNumber(2, 0, 0))
                    {
                        LoadDefaultConfig();
                    }
                    if (config.Version < new Core.VersionNumber(2, 0, 3))
                    {
                        if (config.BuildingImages[0][3].SkinId == 10225)
                        {
                            config.BuildingImages[0][3].SkinId = 3;
                        }
                    }
                    config.Version = Version;
                    PrintWarning("Config update completed!");
                }
            }
            catch
            {
                PrintWarning("The config file contains an error and has been replaced with the default config.");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        
        #endregion
        
        #region Commands
        
        [ConsoleCommand("UI_BuildingController")]
        private void CmdConsoleHandler(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            var playerData = storedData.PlayerData[player.userID];
            SoundEffect(player, "assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab");
            
            int index, skinIndex;
            switch (arg.Args[0].ToLower())
            {
                case "change":
                {
                    if (!int.TryParse(arg.Args[1], out index) || !int.TryParse(arg.Args[2], out skinIndex)) return;
                    ImageLayers(player, index, skinIndex);
                    break;
                }
                case "choose":
                {
                    if (!int.TryParse(arg.Args[1], out index) || !int.TryParse(arg.Args[2], out skinIndex)) return;
                    var grades = gradesSkin.ElementAt(index);
                    playerData.GetType().GetField(grades.Key.ToString())?.SetValue(playerData, grades.Value[skinIndex]);
                    ImageLayers(player, index, skinIndex);
                    SoundEffect(player, "assets/prefabs/missions/effects/mission_objective_complete.prefab");
                    break;
                }
                case "settings":
                {
                    SettingsLayer(player);
                    break;
                }
                case "colors":
                {
                    if (!int.TryParse(arg.Args[1], out index)) return;
                    ColorLayer(player, index);
                    break;
                }
                case "setcolor":
                {
                    if (!int.TryParse(arg.Args[1], out index) || !uint.TryParse(arg.Args[2], out uint colorId)) return;
                    player.LastBlockColourChangeId = colorId;
                    playerData.Color = colorId;
                    ColorLayer(player, index);
                    SoundEffect(player, "assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab");
                    break;
                }
                case "randomcolor":
                {
                    if (!int.TryParse(arg.Args[1], out index)) return;
                    var randomColor = !playerData.RandomColor;
                    playerData.RandomColor = randomColor;
                    player.LastBlockColourChangeId = randomColor ? 0U : playerData.Color;
                    ColorLayer(player, index);
                    break;
                }
                case "hammer":
                {
                    var changeHammer = !playerData.ChangeHammer;
                    playerData.ChangeHammer = changeHammer;
                    SettingsLayer(player);
                    break;
                }
                case "repair":
                {
                    var needsRepair = !playerData.NeedsRepair;
                    playerData.NeedsRepair = needsRepair;
                    SettingsLayer(player);
                    break;
                }
                case "animation":
                {
                    var enableAnimation = !playerData.EnableAnimation;
                    playerData.EnableAnimation = enableAnimation;
                    SettingsLayer(player);
                    break;
                }
            }
        }
        
        private void CmdChangeSkin(IPlayer ipPlayer, string command, string[] arg)
        {
            var player = ipPlayer?.Object as BasePlayer;
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, permissionUse))
            {
                PrintToChat(player, GetMessage("Lang_NoPermissions", player));
                return;
            }
            if (arg.Length == 0)
            {
                InitializeLayers(player, true);
                return;
            }
            if (runningCoroutines.ContainsKey(player.userID))
            {
                PrintToChat(player, GetMessage("Lang_UpdateProgress", player));
                return;
            }
            if (arg.Length > 0 && config.BuildingBlocked && !player.CanBuild() && !permission.UserHasPermission(player.UserIDString, permissionAdmin))
            {
                PrintToChat(player, GetMessage("Lang_BuildingBlocked", player));
                return;
            }
            switch (arg[0].ToLower())
            {
                case "build":
                {
                    if (!permission.UserHasPermission(player.UserIDString, permissionBuild))
                    {
                        PrintToChat(player, GetMessage("Lang_NoPermissions", player));
                        return;
                    }
                    var entity = GetLookEntity(player);
                    if (entity == null)
                    {
                        PrintToChat(player, GetMessage("Lang_NotFoundBuilding", player));
                        return;
                    }
                    if (entity.OwnerID != player.userID && !permission.UserHasPermission(player.UserIDString, permissionAdmin))
                    {
                        PrintToChat(player, GetMessage("Lang_NotOwnerBuilding", player));
                        return;
                    }
                    var blocks = entity.GetBuilding()?.buildingBlocks.ToArray();
                    if (blocks == null) return;
                    PrintToChat(player, GetMessage("Lang_UpdateBuilding", player));
                    StartCoroutine(player, UpgradeSkin(player, blocks));
                    break;
                }
                case "all":
                {
                    if (!permission.UserHasPermission(player.UserIDString, permissionAll))
                    {
                        PrintToChat(player, GetMessage("Lang_NoPermissions", player));
                        return;
                    }
                    if (arg.Length > 1 && !ulong.TryParse(arg[1], out var owner))
                    {
                        PrintToChat(player, GetMessage("Lang_NotFoundPlayer", player));
                        return;
                    }
                    var targetOwner = arg.Length > 1 ? ulong.Parse(arg[1]) : player.userID;
                    if (!permission.UserHasPermission(player.UserIDString, permissionAdmin) && targetOwner != player.userID)
                    {
                        PrintToChat(player, GetMessage("Lang_NoPermissions", player));
                        return;
                    }
                    var blockOwner = BaseNetworkable.serverEntities.OfType<BuildingBlock>().Where(x => x.OwnerID == targetOwner).ToArray();
                    if (blockOwner.Length == 0)
                    {
                        PrintToChat(player, GetMessage("Lang_NotFoundBlocks", player));
                        return;
                    }
                    PrintToChat(player, GetMessage(targetOwner != player.userID ? "Lang_UpdateAllTarget" : "Lang_UpdateAll", player));
                    StartCoroutine(player, UpgradeSkin(player, blockOwner));
                    break;
                }
            }
        }
        
        #endregion
        
        #region Methods
        
        private void StartCoroutine(BasePlayer player, IEnumerator routine)
        {
            if (runningCoroutines.ContainsKey(player.userID)) return;
            var coroutine = ServerMgr.Instance?.StartCoroutine(routine);
            if (coroutine != null) runningCoroutines[player.userID] = coroutine;
        }
        
        private void StopCoroutine(BasePlayer player)
        {
            if (!runningCoroutines.ContainsKey(player.userID)) return;
            var coroutine = runningCoroutines[player.userID];
            if (coroutine != null) ServerMgr.Instance?.StopCoroutine(coroutine);
            runningCoroutines.Remove(player.userID);
        }
        
        private IEnumerator UpgradeSkin(BasePlayer player, BuildingBlock[] blocks)
        {
            var count = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (block == null || block.IsDestroyed) continue;
                if (!gradesSkin.TryGetValue(block.grade, out var skinId) || skinId == null) continue;
                var skinID = GetPlayerSkinID(player, block.grade);
                var playerData = storedData.PlayerData[player.userID];
                if (skinID != 0 && player.blueprints.steamInventory.HasItem((int)skinID)) continue;
                //if (skinID == 10226 && block.grade.ToString() == "Wood" && block.prefabID == 870964632) continue;
                if (block.skinID != 0 && block.grade.ToString() == "Metal" && !playerData.RandomColor) block.SetCustomColour(player.LastBlockColourChangeId);
                if (block.skinID == skinID) continue;
                block.skinID = skinID;
                block.ChangeGradeAndSkin(block.grade, skinID, true, true);
                count++;
                if (i % config.UpdatesPerTick == 0)
                    yield return CoroutineEx.waitForFixedUpdate;
            }
            if (count == 0) PrintToChat(player, GetMessage("Lang_UpdateNotRequired", player));
            else PrintToChat(player, GetMessage("Lang_UpdateCompleted", player), count, blocks.Length);
            StopCoroutine(player);
        }
        
        private bool HasPermission(BasePlayer player, string name)
        {
            foreach (var blockInfoList in config.BuildingImages.Values)
            {
                foreach (var blockInfo in blockInfoList)
                {
                    if (blockInfo.Title == name && permission.UserHasPermission(player.UserIDString, $"buildingskins.{blockInfo.Title}".ToLower()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        private ulong GetPlayerSkinID(BasePlayer player, BuildingGrade.Enum grade)
        {
            var playerData = storedData.PlayerData.TryGetValue(player.userID, out var data) ? data : null;
            return (ulong)(playerData?.GetType().GetField(grade.ToString()).GetValue(playerData) ?? 0);
        }
        
        private BuildingBlock GetLookEntity(BasePlayer player)
        {
            return Physics.Raycast(player.eyes.HeadRay(), out RaycastHit raycastHit, 4f, Rust.Layers.Mask.Construction) ? raycastHit.GetEntity() as BuildingBlock : null;
        }
        
        private static void SoundEffect(BasePlayer player, string effect = null)
        {
            EffectNetwork.Send(new Effect(effect, player, 0, new Vector3(), new Vector3()), player.Connection);
        }
        
        private string GetMessage(string key, BasePlayer player)
        {
            return lang.GetMessage(key, this, player.UserIDString);
        }
        
        #endregion
        
        #region Interfaces
        
        private void InitializeLayers(BasePlayer player, bool update)
        {
            float fade = !update ? 0f : 0.25f;
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiElement()
            {
                Parent = "Overlay",
                Name = InitialLayer,
                DestroyUi = InitialLayer,
                Components =
                {
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                    new CuiImageComponent { Color = "0.235 0.227 0.2 0.9" },
                    new CuiNeedsCursorComponent()
                }
            });
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Close = InitialLayer, Color = "0.141 0.137 0.096 0.98", Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
            }, InitialLayer);
            
            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-250 -320", OffsetMax = $"250 300" },
                Image = { Color = "0.117 0.121 0.109 0.8", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = fade }
            }, InitialLayer, InitialLayer + ".Main");
            
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "30 -60", OffsetMax = "-30 -2" },
                Text = { Text = GetMessage("Lang_InterfaceTitle", player).ToUpper(), Color = "0.78 0.74 0.70 1.0", FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = fade }
            }, InitialLayer + ".Main");
            
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 5", OffsetMax = "0 25" },
                Text = { Text = $"• {GetMessage("Lang_InterfaceDescr", player)}".ToUpper(), Color = "0.78 0.74 0.70 0.6", FontSize = 12, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = fade }
            }, InitialLayer + ".Main");
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-25 -25", OffsetMax = "-5 -5" },
                Button = { Close = InitialLayer, Color = "0.71 0.22 0.15 1.0", Sprite = "assets/icons/close.png" }
            }, InitialLayer + ".Main");
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -25", OffsetMax = "25 -5" },
                Button = { Command = $"UI_BuildingController settings", Color = "0.31 0.64 0.89 0.5", Sprite = "assets/icons/gear.png" }
            }, InitialLayer + ".Main", InitialLayer + ".Settings");
            
            CuiHelper.AddUi(player, container);
            
            for (var i = 0; i < config.BuildingImages.Count; i++)
            {
                var grades = gradesSkin.ElementAt(i);
                var skinID = GetPlayerSkinID(player, grades.Key);
                var skinIndex = grades.Value.IndexOf(skinID);
                ImageLayers(player, i, skinIndex);
            }
        }
        
        private void ImageLayers(BasePlayer player, int index, int skinIndex)
        {
            var playerData = storedData.PlayerData[player.userID];
            var listIndex = config.BuildingImages[index];
            var nextIndex = (skinIndex + 1) % listIndex.Count;
            var grades = gradesSkin.ElementAt(index);
            var skinID = GetPlayerSkinID(player, grades.Key);
            var selected = listIndex[skinIndex].SkinId == skinID;
            var hasSkin = listIndex.Any(x => !string.IsNullOrEmpty(x.Url) && x.SkinId != 0);
            var hasPermission = HasPermission(player, listIndex[skinIndex].Title);
            var isBlocked = config.SeparatePermissions && !hasPermission && skinIndex != 0;
            
            if (isBlocked && selected)
            {
                playerData.GetType().GetField(grades.Key.ToString())?.SetValue(playerData, grades.Value[0]);
                ImageLayers(player, index, 0);
                return;
            }
            
            const int marginTop = 10, margin = 6, width = 200, height = 250;
            var offsetX = (index % 2 == 0 ? -1 : 1) * ((width + margin) / 2 + margin);
            var offsetY = (index < 2 ? 1 : -1) * ((height + margin) / 2 + margin) - marginTop;
            
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offsetX - width / 2} {offsetY - height / 2}", OffsetMax = $"{offsetX + width / 2} {offsetY + height / 2}" },
                Button = { Command = hasSkin ? $"UI_BuildingController change {index} {nextIndex}" : "", Color = "0.22 0.25 0.16 0.9", Material = "assets/content/ui/uibackgroundblur.mat" },
            }, InitialLayer + ".Main", InitialLayer + $".Button.{index}");
            
            container.Add(new CuiElement()
            {
                Parent = InitialLayer + $".Button.{index}",
                Name = InitialLayer + $".Image.{index}",
                DestroyUi = InitialLayer + $".Image.{index}",
                Components =
                {
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-85 -72", OffsetMax = "85 98" },
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", listIndex[skinIndex].Title), Color = hasSkin && !isBlocked ? "1 1 1 1" : "0.8 0.8 0.8 0.5" }
                }
            });
            
            if (hasSkin)
            {
                container.Add(new CuiPanel()
                {
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-28 -28", OffsetMax = "-5 -5" },
                    Image = { Color = "0.81 0.77 0.74 0.8", Sprite = "assets/icons/refresh.png" }
                }, InitialLayer + $".Button.{index}");
            }
            if (!hasSkin || isBlocked)
            {
                container.Add(new CuiPanel()
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22 -30", OffsetMax = "22 30" },
                    Image = { Color = "0.81 0.77 0.74 0.8", Sprite = "assets/content/ui/lock@4x.png" }
                }, InitialLayer + $".Image.{index}");
            }
            
            if (grades.Key.ToString() == "Metal" && !isBlocked)
            {
                if (skinIndex != 0)
                {
                    var colorIcon = colors.TryGetValue(player.LastBlockColourChangeId, out var value) ? value : "0.20 0.30 0.40 1.0";
                    
                    container.Add(new CuiButton()
                    {
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -28", OffsetMax = "28 -5" },
                        Button = { Command = $"UI_BuildingController colors {index}", Color = colorIcon, Sprite = "assets/icons/circle_closed.png" }
                    }, InitialLayer + $".Button.{index}", InitialLayer + ".Color");
                    
                    container.Add(new CuiPanel()
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-2 -2", OffsetMax = "2 2" },
                        Image = { Color = "0.81 0.77 0.74 1.0", Sprite = "assets/icons/workshop.png" }
                    }, InitialLayer + ".Color");
                }
                else CuiHelper.DestroyUi(player, InitialLayer + ".Color");
            }
            
            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 26" },
                Image = { Color = "0 0 0 0.5" }
            }, InitialLayer + $".Button.{index}", InitialLayer + $".Title.{index}");
            
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 0", OffsetMax = "125 26" },
                Text = { Text = GetMessage(listIndex[skinIndex].Title, player).ToUpper(), Color = "0.81 0.77 0.74 1.0", FontSize = 11, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, InitialLayer + $".Title.{index}");
            
            var textTitle = GetMessage(selected ? "Lang_SkinInstalled" : (isBlocked ? "Lang_Unavailable" : "Lang_InterfaceApply"), player);
            var colorTitle = selected ? "0.59 0.84 0.18 1.0" : isBlocked ? "0.81 0.77 0.74 1.0" : "0.30 0.65 0.90 1.0";
            var colorButton = selected ? "0.30 0.36 0.16 1.0" : isBlocked ? "0.34 0.33 0.31 1.0" : "0.20 0.30 0.40 1.0";
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-75 0", OffsetMax = "0 26" },
                Button = { Command = hasSkin && !isBlocked && !selected ? $"UI_BuildingController choose {index} {skinIndex}" : "", Color = colorButton, Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = textTitle.ToUpper(), Color = colorTitle, FontSize = 11, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, InitialLayer + $".Title.{index}");
            
            CuiHelper.AddUi(player, container);
        }
        
        private void SettingsLayer(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiElement()
            {
                Parent = InitialLayer + ".Settings",
                Name = InitialLayer + ".SettingsMenu",
                DestroyUi = InitialLayer + ".SettingsMenu",
                Components =
                {
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-350 -5", OffsetMax = "5 5" },
                    new CuiImageComponent { Color = "0.20 0.30 0.40 1.0", Material = "assets/content/ui/uibackgroundblur.mat" }
                }
            });
            
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 0", OffsetMax = "0 0" },
                Text = { Text = GetMessage("Lang_MenuSettings", player).ToUpper(), Color = "0.78 0.74 0.70 1.0", FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, InitialLayer + ".SettingsMenu");
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-25 -25", OffsetMax = "-5 -5" },
                Button = { Close = InitialLayer + ".SettingsMenu", Color = "0.71 0.22 0.15 1.0", Sprite = "assets/icons/close.png" }
            }, InitialLayer + ".SettingsMenu");
            
            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "2 -150", OffsetMax = "-35 0" },
                Image = { Color = "0.117 0.121 0.109 0.8", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, InitialLayer + ".SettingsMenu", InitialLayer + ".SettingsButton");
            
            string[] langKeys = {"Lang_ChangeHammer", "Lang_NeedsRepair", "Lang_EnableAnimation"};
            string[] commands = {"UI_BuildingController hammer", "UI_BuildingController repair", "UI_BuildingController animation"};
            var playerData = storedData.PlayerData[player.userID];
            var margin = 0;
            
            for (var i = 0; i < 3; i++)
            {
                var active = new[]{playerData.ChangeHammer, playerData.ChangeHammer && playerData.NeedsRepair, playerData.EnableAnimation}[i];
                var text = GetMessage($"Lang_Setting{(active ? "Enable" : "Disable")}", player);
                var textlang = GetMessage(langKeys[i], player);
                
                container.Add(new CuiButton()
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"10 {-50 - margin}", OffsetMax = $"-10 {-15 - margin}" },
                    Button = { Command = commands[i], Color = "0.34 0.33 0.31 1.0", Material = "assets/content/ui/uibackgroundblur.mat" },
                }, InitialLayer + ".SettingsButton", InitialLayer + $".SettingsButton.{i}");
                
                container.Add(new CuiPanel()
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "50 0" },
                    Image = { Color = active ? "0.36 0.44 0.22 1.0" : "0.71 0.22 0.15 1.0", Material = "assets/content/ui/uibackgroundblur.mat" },
                }, InitialLayer + $".SettingsButton.{i}", InitialLayer + $".TextButton");
                
                container.Add(new CuiLabel()
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                    Text = { Text = text.ToUpper(), Color = active ? "0.78 0.74 0.70 1.0" : "0.78 0.74 0.70 0.6", FontSize = 16, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, InitialLayer + $".TextButton");
                
                container.Add(new CuiLabel()
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "60 0", OffsetMax = "-5 0" },
                    Text = { Text = textlang.ToUpper(), Color = "0.78 0.74 0.70 1.0", FontSize = 12, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, InitialLayer + $".SettingsButton.{i}");
                
                margin += 45;
            }
            
            CuiHelper.AddUi(player, container);
        }
        
        private void ColorLayer(BasePlayer player, int index)
        {
            var playerData = storedData.PlayerData[player.userID];
            var playerColor = player.LastBlockColourChangeId;
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiElement()
            {
                Parent = InitialLayer + ".Color",
                Name = InitialLayer + ".SetColor",
                DestroyUi = InitialLayer + ".SetColor",
                Components =
                {
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-250 -5", OffsetMax = "5 5" },
                    new CuiImageComponent { Color = colors.TryGetValue(playerColor, out var value) ? value : "0.20 0.30 0.40 1.0", Material = "assets/content/ui/uibackgroundblur.mat" }
                }
            });
            
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 0", OffsetMax = "0 0" },
                Text = { Text = GetMessage("Lang_ColorSettings", player).ToUpper(), Color = "0.78 0.74 0.70 1.0", FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, InitialLayer + ".SetColor");
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-25 -25", OffsetMax = "-5 -5" },
                Button = { Close = InitialLayer + ".SetColor", Color = "0.71 0.22 0.15 1.0", Sprite = "assets/icons/close.png" }
            }, InitialLayer + ".SetColor");
            
            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"2 {(playerData.RandomColor ? -42 : -228)}", OffsetMax = "-80 0" },
                Image = { Color = "0.117 0.121 0.109 0.8", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, InitialLayer + ".SetColor", InitialLayer + ".ColorButton");
            
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-89 -35", OffsetMax = "89 -6" },
                Button = { Command = $"UI_BuildingController randomcolor {index}", Color = "0.34 0.33 0.31 1.0", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, InitialLayer + ".ColorButton", InitialLayer + ".RandomButton");
            
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0.25 0", AnchorMax = "0.9 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                Text = { Text = GetMessage("Lang_Randomcolor", player).ToUpper(), Color = "0.78 0.74 0.70 1.0", FontSize = 12, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, InitialLayer + ".RandomButton");
            
            var sprite = playerData.RandomColor ? "assets/icons/circle_closed.png" : "assets/icons/circle_open.png";
            
            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "23 5", OffsetMax = "42 -5" },
                Image = { Color = "0.85 0.85 0.85 0.8", Sprite = sprite }
            }, InitialLayer + ".RandomButton", InitialLayer + ".Panel");
            
            if (playerData.RandomColor)
            {
                container.Add(new CuiPanel()
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3" },
                    Image = { Color = "0 0 0 1", Sprite = "assets/icons/check.png" }
                }, InitialLayer + ".Panel");
            }
            else
            {
                const int marginTop = 2, margin = 6, width = 40, height = 40;
                
                for (var i = 0; i < colors.Count; i++)
                {
                    var colorIndex = colors.ElementAt(i).Key;
                    var colorValue = colors.ElementAt(i).Value;
                    var offsetX = i % 4 * (width + margin) - (2 * width + 1.5 * margin);
                    var offsetY = -i / 4 * (height + margin) + 35 - marginTop;
                    
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offsetX} {offsetY}", OffsetMax = $"{offsetX + width} {offsetY + height}" },
                        Button = { Command = colorIndex != playerColor ? $"UI_BuildingController setcolor {index} {colorIndex}" : "", Color = colorValue, Material = "assets/content/ui/uibackgroundblur.mat" }
                    }, InitialLayer + ".ColorButton", InitialLayer + ".PanelButton");
                    
                    if (colorIndex == playerColor)
                    {
                        container.Add(new CuiPanel()
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Image = { Color = "0.59 0.84 0.18 1.0", Sprite = "assets/icons/vote_up.png" }
                        }, InitialLayer + ".PanelButton");
                    }
                }
            }
            
            CuiHelper.AddUi(player, container);
        }
        
        private IEnumerator PreloadImages(BasePlayer player)
        {
            if (player == null || !player.IsConnected) yield break;
            for (var i = 0; i < config.BuildingImages.Count; i++)
            {
                CuiElementContainer temp = new CuiElementContainer();
                
                foreach (var blockInfo in config.BuildingImages[i])
                {
                    if (string.IsNullOrEmpty(blockInfo.Url)) continue;
                    temp.Add(new CuiElement()
                    {
                        Parent = "Hud",
                        Name = $".{i}",
                        DestroyUi = $".{i}",
                        Components =
                        {
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0" },
                            new CuiRawImageComponent { Png = (string) ImageLibrary?.Call("GetImage", blockInfo.Title) }
                        }
                    });
                    CuiHelper.AddUi(player, temp);
                    yield return new WaitForSeconds(1.0f);
                    CuiHelper.DestroyUi(player, $".{i}");
                }
            }
            StopCoroutine(player);
        }
        
        #endregion
        
        #region Language
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Lang_UpdateAll"] = "Skin update for all your buildings has started...",
                ["Lang_UpdateBuilding"] = "Skin update for selected building has started...",
                ["Lang_UpdateAllTarget"] = "Skin update for all players buildings has started...",
                ["Lang_UpdateProgress"] = "Please wait for the building skin update to finish.",
                ["Lang_UpdateCompleted"] = "Building skin update completed.\nUpdated {0} of {1} building blocks.",
                ["Lang_UpdateNotRequired"] = "All building blocks already have your chosen skin.",
                ["Lang_NotFoundBlocks"] = "No available buildings found for the selected player.",
                ["Lang_NotFoundPlayer"] = "Player not found. Use only the Steam Id of the player.",
                ["Lang_NotFoundBuilding"] = "Building not found. Get closer to the building and repeat again.",
                ["Lang_NotOwnerBuilding"] = "You are not the owner of this building.",
                ["Lang_BuildingBlocked"] = "You can't use this command if the building is blocked.",
                ["Lang_NoPermissions"] = "You don't have permission to use this command.",
                ["Lang_InterfaceTitle"] = "Choose a default skin for the building block",
                ["Lang_InterfaceDescr"] = "The skin will be automatically applied to building blocks.",
                ["Lang_ChangeHammer"] = "Change the skin of a building block with a hammer",
                ["Lang_NeedsRepair"] = "Allow skin changing with a hammer if a building needs repair",
                ["Lang_EnableAnimation"] = "Allow building block skin update animation",
                ["Lang_InterfaceApply"] = "Apply",
                ["Lang_SkinInstalled"] = "Installed",
                ["Lang_Unavailable"] = "Unavailable",
                ["Lang_MenuSettings"] = "Settings",
                ["Lang_ColorSettings"] = "Set color",
                ["Lang_Randomcolor"] = "Use random color",
                ["Lang_SettingEnable"] = "On",
                ["Lang_SettingDisable"] = "Off",
                ["Wood"] = "Wood skin",
                ["Stone"] = "Stone skin",
                ["Metal"] = "Metal skin",
                ["TopTier"] = "TopTier skin",
                ["Adobe"] = "Adobe skin",
                ["Brick"] = "Brick skin",
                ["Brutalist"] = "Brutalist skin",
                ["Container"] = "Container skin",
                ["Frontier"] = "Frontier skin",
                ["Gingerbread"] = "Gingerbread skin"
            }, this);
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Lang_UpdateAll"] = "Обновление скина для всех ваших построек началось...",
                ["Lang_UpdateBuilding"] = "Обновление скина для выбранной постройки началось...",
                ["Lang_UpdateAllTarget"] = "Обновление скина для всех построек игрока началось...",
                ["Lang_UpdateProgress"] = "Пожалуйста, дождитесь завершения обновления скина построек.",
                ["Lang_UpdateCompleted"] = "Обновление скина построек завершено.\nОбновлено {0} из {1} строительных блоков.",
                ["Lang_UpdateNotRequired"] = "Все строительные блоки уже имеют выбранный вами скин.",
                ["Lang_NotFoundBlocks"] = "Не найдено доступных построек для выбранного игрока.",
                ["Lang_NotFoundPlayer"] = "Игрок не найден. Используйте только Steam Id игрока.",
                ["Lang_NotFoundBuilding"] = "Постройка не найдена. Подойдите ближе к постройке и повторите снова.",
                ["Lang_NotOwnerBuilding"] = "Вы не являетесь владельцем этой постройки.",
                ["Lang_BuildingBlocked"] = "Вы не можете использовать эту команду в зоне блокировки строительства.",
                ["Lang_NoPermissions"] = "У вас нет разрешения на использование этой команды.",
                ["Lang_InterfaceTitle"] = "Выберите скин по умолчанию для строительного блока",
                ["Lang_InterfaceDescr"] = "Скин будет автоматически применяться к постройке.",
                ["Lang_ChangeHammer"] = "Изменять скин постройки при помощи киянки",
                ["Lang_NeedsRepair"] = "Разрешить смену скина киянкой, если требуется ремонт постройки",
                ["Lang_EnableAnimation"] = "Разрешить анимацию обновления скина постройки",
                ["Lang_InterfaceApply"] = "Применить",
                ["Lang_SkinInstalled"] = "Установлен",
                ["Lang_Unavailable"] = "Недоступен",
                ["Lang_MenuSettings"] = "Настройки",
                ["Lang_ColorSettings"] = "Выбор цвета",
                ["Lang_Randomcolor"] = "Случайный цвет",
                ["Lang_SettingEnable"] = "Вкл",
                ["Lang_SettingDisable"] = "Выкл",
                ["Wood"] = "Деревянный скин",
                ["Stone"] = "Каменный скин",
                ["Metal"] = "Металлический скин",
                ["TopTier"] = "МВК скин",
                ["Adobe"] = "Саманный скин",
                ["Brick"] = "Кирпичный скин",
                ["Brutalist"] = "Брутализм скин",
                ["Container"] = "Контейнерный скин",
                ["Frontier"] = "Фронтир скин",
                ["Gingerbread"] = "Пряничный скин"
            }, this, "ru");
        }
        
        #endregion
        
        #region Data
        
        private StoredData storedData;
        
        private class StoredData
        {
            public Dictionary<ulong, Data> PlayerData = new Dictionary<ulong, Data>();
        }
        
        private class Data
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public bool ChangeHammer;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public bool NeedsRepair;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public bool EnableAnimation;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public bool RandomColor;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public uint Color;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public ulong Wood;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public ulong Stone;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public ulong Metal;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public ulong TopTier;
        }
        
        private void SaveData()
        {
            if (storedData != null)
            {
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}_Data", storedData, true);
            }
        }
        
        private void LoadData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>($"{Name}_Data");
            if (storedData == null)
            {
                storedData = new StoredData();
                SaveData();
            }
        }
        
        #endregion
    }
}