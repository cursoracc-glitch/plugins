using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Battle Pass", "CASHR#6906", "1.0.0")]
    internal class BattlePass : RustPlugin
    {
        #region Static

        [PluginReference] private Plugin ImageLibrary;
        private Configuration _config;
        private const string Layer = "UI_BATTLEPASS_MAINLAYER";
        private const string perm = "battlepass.vip";
        private const string perm1 = "battlepass.exp";

        #endregion

        #region Config

        private class Configuration
        {
            [JsonProperty(PropertyName = "Ссылка на логотип сервера(64x64 px)")]
            public string serverLogoURL = "https://i.imgur.com/v0i0Rid.png";

            [JsonProperty(PropertyName = "Название сервера")]
            public string serverName = "The best of the best - Beast";
            
            [JsonProperty("Настройка очков")] public readonly PointSettings Point = new PointSettings();

            [JsonProperty("Настройки уровней обычных уровней")]
            public readonly LevelSettings LevelDefault = new LevelSettings();

            [JsonProperty("Настройки уровней донатных уровней")]
            public readonly LevelSettings LevelDonate = new LevelSettings();

            internal class LevelSettings
            {

                [JsonProperty(PropertyName = "Список уровней", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly List<Settings> LevelList = new List<Settings>
                {
                    new Settings
                    {
                        Level = 1,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 2,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 3,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 4,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 5,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 6,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 7,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 8,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 9,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 10,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 11,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    },
                    new Settings
                    {
                        Level = 12,
                        Exp = 1000,
                        Image = "https://i.imgur.com/WUtLEeN.png",
                        Reward = new Settings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            command = new List<string>()
                            {
                                "givecoin %STEAMID% 10",
                                "o.grant user %STEAMID% kits.vip"
                            }

                        }
                    }
                };

                internal class Settings
                {
                    [JsonProperty("Номер уровня")] public int Level;

                    [JsonProperty("Количество Exp для получения этого уровня")]
                    public int Exp;

                    [JsonProperty("Картинка отображения награды")]
                    public string Image;

                    [JsonProperty("Награда за уровень")] public RewardSettings Reward;

                    internal class RewardSettings
                    {
                        [JsonProperty("ShortName")] public string ShortName;
                        [JsonProperty("Amount")] public int Amount;
                        [JsonProperty("SkinID")] public ulong SkinID;

                        [JsonProperty("Команды, которые должны выполниться")]
                        public List<string> command;
                    }
                }
            }

            internal class PointSettings
            {
                [JsonProperty("Множитель очков у донатера")]
                public readonly int DonateAmount = 1;

                [JsonProperty("Количество очков за убийство игрока")]
                public readonly int killPlayer = 1;

                [JsonProperty("Количество очков за убийство животных")]
                public readonly int killHuman = 1;

                [JsonProperty("Количество очков за убийство вертолета")]
                public readonly int killHeli = 1;

                [JsonProperty("Количество очков за убийство НПС")]
                public readonly int killNPC = 1;

                [JsonProperty("Количество очков за убийство танка")]
                public readonly int killBredly = 1;

                [JsonProperty("Количество отнимаемых очков за смерть")]
                public readonly int deathPlayer = 1;

                [JsonProperty("Настройки добычи")] public readonly GatherSettings Gather = new GatherSettings();
            }

            internal class GatherSettings
            {
                [JsonProperty(PropertyName = "Настройка добычи(shortname/количество очков", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly Dictionary<string, int> GatherList = new Dictionary<string, int>
                {
                    ["wood"] = 2
                };
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

        #region OxideHooks

        private void OnServerInitialized()
        {

            if (lang.GetServerLanguage() == "ru")
            {
                PrintWarning("" + "\n=====================" + "\n=====================Author: CASHR" +
                             "\n=====================VK: vk.com/cashrdev" +
                             "\n=====================Discord: CASHR#6906" +
                             "\n=====================Email: pipnik99@gmail.com" +
                             "\n=====================Если вы хотите заказать у меня плагин, я жду вас в любой момент." +
                             "\n=====================");
                PrintWarning(
                    "Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            }
            else
            {
                PrintWarning("" + "\n=====================" + "\n=====================Author: CASHR" +
                             "\n=====================VK: vk.com/cashrdev" +
                             "\n=====================Discord: CASHR#6906" +
                             "\n=====================Email: pipnik99@gmail.com" +
                             "\n=====================If you want to order a plugin from me, I am waiting for you in discord." +
                             "\n=====================");
            }

            ImageLibrary?.Call("AddImage", "https://i.imgur.com/MGh0CxA.png", "MAINPAGEIMAGEBP");
            ImageLibrary?.Call("AddImage", _config.serverLogoURL, _config.serverLogoURL);
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/cBLIoWr.png", ".iconpattern");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/8bmAyzO.png", ".rewardisaccepted");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/OLJP48p.png", ".blockereward");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/MGh0CxA.png", "secondbpmain");
            LoadData();
            permission.RegisterPermission(perm, this);
            permission.RegisterPermission(perm1, this);
            foreach (var check in _config.LevelDefault.LevelList)
                ImageLibrary?.Call("AddImage", check.Image, check.Image);
            foreach (var check in _config.LevelDonate.LevelList)
                ImageLibrary?.Call("AddImage", check.Image, check.Image);
            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            if (_data.ContainsKey(player.userID)) return;
            _data.Add(player.userID, new Data());
        }

        private void Unload()
        {
            SaveData();
        }

        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (item == null || player == null) return null;
            if (!_config.Point.Gather.GatherList.ContainsKey(item.info.shortname)) return null;
            GivePoint(player.userID, _config.Point.Gather.GatherList[item.info.shortname]);
            return null;
        }

        private void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (player == null || item == null) return;
            GivePoint(player.userID, 1);
        }

        private readonly Dictionary<uint, ulong> LastHeliHit = new Dictionary<uint, ulong>();

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            var player = info.InitiatorPlayer;
            if (player == null) return null;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
            {
                if (!LastHeliHit.ContainsKey(entity.net.ID))
                    LastHeliHit.Add(entity.net.ID, info.InitiatorPlayer.userID);
                LastHeliHit[entity.net.ID] = info.InitiatorPlayer.userID;
            }

            return null;
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null || info.InitiatorPlayer == null) return;
            var player = info.InitiatorPlayer;
            if (entity as BaseAnimalNPC)
            {
                GivePoint(player.userID, _config.Point.killHuman);
                return;
            }

            if (entity as BaseHelicopter)
            {
                if (!LastHeliHit.ContainsKey(entity.net.ID)) return;
                GivePoint(LastHeliHit[entity.net.ID], _config.Point.killHeli);
                return;
            }

            if (entity as BradleyAPC)
            {
                GivePoint(player.userID, _config.Point.killBredly);
                return;
            }

            if (entity as NPCPlayer || entity as NPCMurderer || entity.IsNpc)
            {
                GivePoint(player.userID, _config.Point.killNPC);
                return;
            }

            if (entity.ToPlayer() == null) return;
            if (entity.ToPlayer().userID != player.userID)
            {
                GivePoint(player.userID, _config.Point.killPlayer);
            }

            if (!_data.ContainsKey(entity.ToPlayer().userID)) return;
            if (info.InitiatorPlayer == null) return;
            if (info.InitiatorPlayer.IsNpc) return;
            _data[entity.ToPlayer().userID].Score -= _config.Point.deathPlayer;
        }

        #endregion

        #region Data

        private Dictionary<ulong, Data> _data;

        private class Data
        {
            public int Level;
            public int Score;
            public readonly List<int> DefaultRewardID = new List<int>();
            public readonly List<int> DonateRewardID = new List<int>();
        }

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/PlayerData"))
                _data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Data>>(
                    $"{Name}/PlayerData");
            else _data = new Dictionary<ulong, Data>();
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _data);
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void SaveData()
        {
            if (_data != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _data);
        }

        #endregion

        #region Commands

        [ConsoleCommand("UI_BATTLEPASS_GETREWARDDEFULT")]
        private void cmdChatUI_BATTLEPASS_GETREWARD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var id = int.Parse(arg.Args[0]);
            var page = int.Parse(arg.Args[1]);
            var settings = _config.LevelDefault.LevelList.FirstOrDefault(p => p.Level == id);
            var reward = settings.Reward;
            _data[player.userID].DefaultRewardID.Add(id);
            ShowUIMain(player, page);

            if (!string.IsNullOrEmpty(reward.ShortName))
            {
                var item = ItemManager.CreateByName(reward.ShortName, reward.Amount, reward.SkinID);
                player.GiveItem(item);
            }

            if (reward.command.Count <= 0) return;
            for (var index = 0; index < reward.command.Count; index++)
            {
                var check = reward.command[index];
                rust.RunServerCommand(check.Replace("%STEAMID%", player.UserIDString));
            }

        }

        [ChatCommand("pass")]
        private void cmdChatpass(BasePlayer player, string command, string[] args)
        {
            ShowUIMain(player, 0);
        }

        [ChatCommand("lvl")]
        private void cmdChatpa22ss(BasePlayer player, string command, string[] args)
        {
            ShowUIMain(player, 0);
        }

        [ChatCommand("level")]
        private void cmdChatpas2s(BasePlayer player, string command, string[] args)
        {
            ShowUIMain(player, 0);
        }

        [ConsoleCommand("UI_BATTLEPASS_GETREWARDDONATE")]
        private void cmdChatUI_BATTLEPASS_DDONATE(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var id = int.Parse(arg.Args[0]);
            var page = int.Parse(arg.Args[1]);
            var settings = _config.LevelDonate.LevelList.FirstOrDefault(p => p.Level == id);
            var reward = settings.Reward;
            _data[player.userID].DonateRewardID.Add(id);
            ShowUIMain(player, page);
            if (!string.IsNullOrEmpty(reward.ShortName))
            {
                var item = ItemManager.CreateByName(reward.ShortName, reward.Amount, reward.SkinID);
                player.GiveItem(item);
            }

            if (reward.command.Count <= 0) return;
            for (var index = 0; index < reward.command.Count; index++)
            {
                var check = reward.command[index];
                rust.RunServerCommand(check.Replace("%STEAMID%", player.UserIDString));
            }
        }

        [ConsoleCommand("givepoint")]
        private void cmdChatgivepoint(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args?.Length != 2)
            {
                PrintError(
                    "Вы не правильно используете команду. Пример: givepoint STEAMID количество");
                return;
            }

            var player = arg.Player();
            if (arg.Connection != null)
                if (!player.IsAdmin)
                    return;
            var userID = ulong.Parse(arg.Args[0]);
            var amount = int.Parse(arg.Args[1]);
            GivePoint(userID, amount);

        }

        [ConsoleCommand("UI_BATTLEPASS_CHANGEPAGE")]
        private void cmdChatUI_BATTLEPASS_CHANGEPAGE(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (arg == null || arg.Args.Length == 0)
            {
                player.ChatMessage("Не верное число");
                return;
            }

            var page = int.Parse(arg.Args[0]);
            ShowUIMain(player, page);
        }

        #endregion

        #region Function

        private void GivePoint(ulong userid, int amount)
        {
            if (!_data.ContainsKey(userid)) return;
            if (permission.UserHasPermission(userid.ToString(), perm1))
                amount *= _config.Point.DonateAmount;
            var data = _data[userid];
            var settings = _config.LevelDefault.LevelList.FirstOrDefault(p => p.Level == data.Level + 1);
            if (settings == null) return;

            data.Score += amount;
            if (data.Score < settings.Exp) return;
            data.Level++;
            data.Score -= settings.Exp;
            GivePoint(userid, 0);
        }

        #endregion
        
        private void ShowUIMain(BasePlayer player, int page)
        {
            var container = new CuiElementContainer();
            var d = _data[player.userID];

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -360", OffsetMax = "640 360"},
                Image = {Color = "0 0 0 0"}
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", permission.UserHasPermission(player.UserIDString, perm) ? "secondbpmain" : "MAINPAGEIMAGEBP")},
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -360", OffsetMax = "640 360"}
                }
            });

            #region header

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", _config.serverLogoURL)},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -64", OffsetMax = "84 0"}
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "100 -64", OffsetMax = "800 0"},
                Text =
                {
                    Text = _config.serverName, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleLeft,
                    Color = "0.93 0.77 0.58 1.00"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-72 -53", OffsetMax = "-31 -11"},
                Button = {Color = "0 0 0 0", Close = Layer},
                Text = {Text = ""}
            }, Layer);

            #endregion

            #region Level/Bar

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "130 -186", OffsetMax = "287 -143"},
                Text =
                {
                    Text = $"Уровень <size=30>{d.Level}</size>", Font = "robotocondensed-bold.ttf", FontSize = 28, Align = TextAnchor.MiddleLeft,
                    Color = "0.27 0.33 0.42 1.00"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "308 -180", OffsetMax = "1180 -175"},
                Image = {Color = "0.18 0.20 0.24 1.00"}
            }, Layer, Layer + ".progressbar");
            
            var settings = _config.LevelDefault.LevelList.FirstOrDefault(p => p.Level == d.Level + 1);
            if (settings != null)
            {
                var progress = (float)d.Score / settings.Exp;

                container.Add(new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "308 -172", OffsetMax = "500 -150"},
                    Text =
                    {
                        Text = $"ОЧКИ ОПЫТА <color=white>{d.Score}/{settings.Exp}</color>", Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleLeft,
                        Color = "0.93 0.77 0.58 1.00"
                    }
                }, Layer);

                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = $"{872 * progress} 5"},
                    Image = {Color = "0.93 0.77 0.58 1.00"}
                }, Layer + ".progressbar");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "308 -172", OffsetMax = "500 -150"},
                    Text =
                    {
                        Text = "ОЧКИ ОПЫТА <color=white>МАКСИМУМ</color>", Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleLeft,
                        Color = "0.93 0.77 0.58 1.00"
                    }
                }, Layer);

                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = $"872 5"},
                    Image = {Color = "0.93 0.77 0.58 1.00"}
                }, Layer + ".progressbar");
            }

            #endregion

            #region body

            var levels = _config.LevelDefault.LevelList.Skip(page * 10).Take(10);
            var donate = _config.LevelDonate.LevelList.Skip(page * 10).Take(10);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "93 -343", OffsetMax = "192 -323"},
                Text =
                {
                    Text = "FREE", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "0.27 0.33 0.42 1.00"
                }
            }, Layer);
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "93 -543", OffsetMax = "192 -512"},
                Text =
                {
                    Text = "EPIC", Font = "robotocondensed-bold.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter,
                    Color = "0.27 0.33 0.42 1.00"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "198 -561", OffsetMax = "1087 -204"},
                Image = {Color = "0 0 0 0"}
            }, Layer, Layer + ".rewards");
            var pos = 0;
            var i = 1;
            foreach (var check in levels)
            {
                var lvl = check.Level;
                if (lvl % 10f != 0)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{pos} -357", OffsetMax = $"{pos + 99} 0"},
                        Image = {Color = "0 0 0 0"}
                    }, Layer + ".rewards", Layer + ".rewards" + lvl);
                    
                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -36", OffsetMax = "95 0"},
                        Text =
                        {
                            Text = $"Ур. {lvl}", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                            Color = "0.84 0.85 0.80 1.00", FadeIn = 0.5f * i
                        }
                    }, Layer + ".rewards" + lvl);

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".iconpattern"), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -120", OffsetMax = "80 -55"}
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -120", OffsetMax = "80 -55"},
                        Text =
                        {
                            Text = check.Reward.Amount.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.LowerCenter,
                            Color = "1 1 1 1", FadeIn = 0.5f * i
                        }
                    }, Layer + ".rewards" + lvl);

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", check.Image), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "24 -104", OffsetMax = "72 -55"}
                        }
                    });

                    if (d.DefaultRewardID.Contains(check.Level))
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".rewards" + lvl,
                            Components =
                            {
                                new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".rewardisaccepted"), FadeIn = 0.5f * i},
                                new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "26 -104", OffsetMax = "74 -55"}
                            }
                        });
                    }
                    else
                    {
                        if (check.Level > d.Level)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = Layer + ".rewards" + lvl,
                                Components =
                                {
                                    new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".blockereward"), FadeIn = 0.5f * i},
                                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "66 -69", OffsetMax = "83 -52"}
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -120", OffsetMax = "80 -55"},
                                Button = {Color = "0 0 0 0", Command = $"UI_BATTLEPASS_GETREWARDDEFULT {check.Level} {page}", FadeIn = 0.5f * i},
                                Text = {Text = ""}
                            }, Layer + ".rewards" + lvl);
                        }
                    }
                }
                else
                {
                    container.Add(new CuiPanel  
                    {
                        RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-185 -561", OffsetMax = "-80 -196"},
                        Image = {Color = "0 0 0 0"}
                    }, Layer, Layer + ".rewards" + lvl);
                     
                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -51", OffsetMax = "104 -23"},
                        Text =
                        {
                            Text = $"Ур. {lvl}", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                            Color = "0.27 0.33 0.42 1.00", FadeIn = 0.5f * i
                        }
                    }, Layer + ".rewards" + lvl);

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".iconpattern"), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -134", OffsetMax = "85 -69"}
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -134", OffsetMax = "85 -69"},
                        Text =
                        {
                            Text = check.Reward.Amount.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.LowerCenter,
                            Color = "1 1 1 1", FadeIn = 0.5f * i
                        }
                    }, Layer + ".rewards" + lvl);

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", check.Image), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "28 -118", OffsetMax = "77 -69"}
                        }
                    });

                    if (d.DefaultRewardID.Contains(check.Level))
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".rewards" + lvl,
                            Components =
                            {
                                new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".rewardisaccepted"), FadeIn = 0.5f * i},
                                new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "28 -118", OffsetMax = "77 -69"}
                            }
                        });
                    }
                    else
                    {
                        if (check.Level > d.Level)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = Layer + ".rewards" + lvl,
                                Components =
                                {
                                    new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".blockereward"), FadeIn = 0.5f * i},
                                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "73 -83", OffsetMax = "89 -66"}
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -134", OffsetMax = "85 -69"},
                                Button = {Color = "0 0 0 0", Command = $"UI_BATTLEPASS_GETREWARDDEFULT {check.Level} {page}", FadeIn = 0.5f * i},
                                Text = {Text = ""}
                            }, Layer + ".rewards" + lvl);
                        }
                    }
                }
                pos += 99;
                i++;
            }

            i = 1;
            foreach (var check in donate)
            {
                var lvl = check.Level;
                if (lvl % 10f != 0)
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".iconpattern"), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -275", OffsetMax = "80 -210"}
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -275", OffsetMax = "80 -210"},
                        Text =
                        {
                            Text = check.Reward.Amount.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.LowerCenter,
                            Color = "1 1 1 1", FadeIn = 0.5f * i
                        }
                    }, Layer + ".rewards" + lvl);

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", check.Image), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "24 -260", OffsetMax = "72 -210"}
                        }
                    });

                    if (d.DonateRewardID.Contains(check.Level))
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".rewards" + lvl,
                            Components =
                            {
                                new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".rewardisaccepted"), FadeIn = 0.5f * i},
                                new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -260", OffsetMax = "80 -210"}
                            }
                        });
                    }
                    else
                    {
                        if (check.Level > d.Level || !permission.UserHasPermission(player.UserIDString, perm))
                        {
                            container.Add(new CuiElement
                            {
                                Parent = Layer + ".rewards" + lvl,
                                Components =
                                {
                                    new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".blockereward"), FadeIn = 0.5f * i},
                                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "66 -224", OffsetMax = "83 -207"}
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "16 -275", OffsetMax = "80 -210"},
                                Button = {Color = "0 0 0 0", Command = $"UI_BATTLEPASS_GETREWARDDONATE {check.Level} {page}", FadeIn = 0.5f * i},
                                Text = {Text = ""}
                            }, Layer + ".rewards" + lvl);
                        }
                    }
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".iconpattern"), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -278", OffsetMax = "85 -210"}
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -278", OffsetMax = "85 -210"},
                        Text =
                        {
                            Text = check.Reward.Amount.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.LowerCenter,
                            Color = "1 1 1 1", FadeIn = 0.5f * i
                        }
                    }, Layer + ".rewards" + lvl);

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".rewards" + lvl,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", check.Image), FadeIn = 0.5f * i},
                            new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "28 -260", OffsetMax = "77 -210"}
                        }
                    });

                    if (d.DonateRewardID.Contains(check.Level))
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".rewards" + lvl,
                            Components =
                            {
                                new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".rewardisaccepted"), FadeIn = 0.5f * i},
                                new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "28 -260", OffsetMax = "77 -210"}
                            }
                        });
                    }
                    else
                    {
                        if (check.Level > d.Level)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = Layer + ".rewards" + lvl,
                                Components =
                                {
                                    new CuiRawImageComponent {Png = (String) ImageLibrary.Call("GetImage", ".blockereward"), FadeIn = 0.5f * i},
                                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "73 -224", OffsetMax = "89 -207"}
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -278", OffsetMax = "85 -210"},
                                Button = {Color = "0 0 0 0", Command = $"UI_BATTLEPASS_GETREWARDDONATE {check.Level} {page}", FadeIn = 0.5f * i},
                                Text = {Text = ""}
                            }, Layer + ".rewards" + lvl);
                        }
                    }
                }

                i++;
            }

            #endregion

            if (page > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "38 345", OffsetMax = "62 375"},
                    Button = {Color = "0 0 0 0", Command = $"UI_BATTLEPASS_CHANGEPAGE {page - 1}"},
                    Text = {Text = ""}
                }, Layer);
            
            if (_config.LevelDefault.LevelList.Count - 10 * (page + 1) > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-62 345", OffsetMax = "-38 375"},
                    Button = {Color = "0 0 0 0", Command = $"UI_BATTLEPASS_CHANGEPAGE {page + 1}"},
                    Text = {Text = ""}
                }, Layer);

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        
    }
}
