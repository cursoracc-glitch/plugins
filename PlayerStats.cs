using UnityEngine;
using System;
using System.Globalization;
using System.Linq;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("Player Stats", "HzHzHzHz", "0.1.4")]
    class PlayerStats : RustPlugin
    {

        #region Configuration
        bool init = false;
        bool autoGrantOnWipe = false;
        string cmdConfigStat = "stat";

        List<string> Kills = new List<string>();
        List<string> Gathers = new List<string>();
        List<string> Raiders = new List<string>();
        List<string> Onlines = new List<string>();
        List<string> Topformonths = new List<string>();

        private void LoadConfigValues()
        {
            bool changed = false;
            if (GetConfig("Основные настройки", "Включить автоматическую выдачу призов после вайпа?", ref autoGrantOnWipe))
            {
                Puts("Добавлен новый параметр в конфиг - Включить автоматическую выдачу призов после вайпа");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Чатовая команда открытия статистики", ref cmdConfigStat))
            {
                Puts("Добавлен новый параметр в конфиг - Чатовая команда открытия статистики");
                changed = true;
            }

            var _Kills = new List<object>()
            {
                {"usergroup add {player} kills"},
                {"usergroup add {player} kills"}
            };
            var _Gathers = new List<object>()
            {
                {"usergroup add {player} gather"},
                {"usergroup add {player} gather"}
            };
            var _Raider = new List<object>()
            {
                {"usergroup add {player} raider"},
                {"usergroup add {player} raider"}
            };
            var _Onlines = new List<object>()
            {
                {"usergroup add {player} online"},
                {"usergroup add {player} online"}
            };
            var _topformonths = new List<object>()
            {
                {"usergroup add {player} topformonths"},
                {"usergroup add {player} topformonths"}

            };
            if (GetConfig("Награда игроков", "Награда игрока занявшего четыре раза подряд один и тот же ТОП", ref _topformonths))
            {
                PrintWarning("Добавлены новые параметры в конфиг - Награда игроков");
                changed = true;
            }
            Topformonths = _topformonths.Select(p => p.ToString()).ToList();

            if (GetConfig("Награда игроков", "Награда для ТОП-1 по онлайну", ref _Onlines))
                changed = true;
            Onlines = _Onlines.Select(p => p.ToString()).ToList();

            if (GetConfig("Награда игроков", "Награда для ТОП-1 в количестве использованых взрывных материалов (ТОП Рейдер)", ref _Raider))
            {
                PrintWarning("Добавлен новый список-параметр - Награда для ТОП-1 в количестве использованых взрывных материалов (ТОП Рейдер)");
                changed = true;
            }
            Raiders = _Raider.Select(p => p.ToString()).ToList();

            if (GetConfig("Награда игроков", "Награда для ТОП-1 в добыче ресурсов", ref _Gathers))
                changed = true;
            Gathers = _Gathers.Select(p => p.ToString()).ToList();

            if (GetConfig("Награда игроков", "Награда для ТОП-1 в убийствах", ref _Kills))
                changed = true;
            Kills = _Kills.Select(p => p.ToString()).ToList();

            if (changed)
                SaveConfig();
        }

        private bool GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
                return false;
            }
            Config[MainMenu, Key] = var;
            return true;
        }
        #endregion
        
        float kills = 2;
        float deaths = 1;
        float resGather = 0.005f;
        float Raider = 0.25f;
        float level = 1;
        int topCount = 20;
        int timeInHours = 24 * 7;

        // общая структура данных
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("PlayerStats/PlayerStats");
        readonly DynamicConfigFile topFile = Interface.Oxide.DataFileSystem.GetFile("PlayerStats/PlayerTopList");
        readonly DynamicConfigFile timingFile = Interface.Oxide.DataFileSystem.GetFile("PlayerStats/PlayerTopTiming");
        readonly DynamicConfigFile lastWipeTopUsersDataFile = Interface.Oxide.DataFileSystem.GetFile("PlayerStats/LastWipeTopUsers");

        int time;

        Dictionary<ulong, PlayerData> data = new Dictionary<ulong, PlayerData>(); //человек-данные
        List<SkillTopUser> skillTopUsers;

        class PlayerData
        {
            public string name = "Неизвестно";
            public string avatar = "Неизвестно";
            public Gather gather;
            public Pvp pvp;
            public Raid raid;
            public int minutes = 0;
            public PlayerData() { }
        }

        class Gather
        {
            public int wood = 0;
            public int stone = 0;
            public int metalOre = 0;
            public int sulfurOre = 0;
            public int hqmetalOre = 0;
            public Gather() { }
        }

        class Pvp
        {
            public int kills = 0;
            public int deaths = 0;
            public Pvp() { }
        }
        class Raid
        {
            public int Explosive = 0;
            public int Beancan = 0;
            public int F1 = 0;
            public int Satchel = 0;
            public int TimedExpl = 0;
            public int RocketLaunched = 0;
            public int Rocket = 0;
            public int Incendiary = 0;
            public int High = 0;
            public Raid() { }
        }

        enum topSkill
        {
            kills,
            gather,
            online,
            raid,
            topformonth
        }

        class SkillTopUser
        {
            public topSkill skillType;
            public ulong userId;
            public int wipeCount;
            public bool giftTaken;

            public SkillTopUser(topSkill skillType, ulong userId, int wipeCount)
            {
                this.skillType = skillType;
                this.userId = userId;
                this.wipeCount = wipeCount;
            }
        }

        void CreatePlayerData(ulong id)
        {
            if (data.ContainsKey(id)) return;
            
            PlayerData i = new PlayerData();
            i.gather = new Gather();
            i.pvp = new Pvp();
            i.raid = new Raid();
            BasePlayer player = BasePlayer.FindByID(id) ?? BasePlayer.FindSleeping(id);
            if (player != null) i.name = player.displayName;
            data.Add(id, i);
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer)
                return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null || IsNPC(player)) return;
            ProcessItem(player, item);
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            ProcessItem(player, item);
        }

        void ProcessItem(BasePlayer player, Item item)
        {
            if (!data.ContainsKey(player.userID))
            {
                CreatePlayerData(player.userID);
            }
            switch (item.info.shortname)
            {
                case "wood":
                    data[player.userID].gather.wood += item.amount;
                    break;
                case "stones":
                    data[player.userID].gather.stone += item.amount;
                    break;
                case "metal.ore":
                    data[player.userID].gather.metalOre += item.amount;
                    break;
                case "sulfur.ore":
                    data[player.userID].gather.sulfurOre += item.amount;
                    break;
                case "hq.metal.ore":
                    data[player.userID].gather.hqmetalOre += item.amount;
                    break;
            }
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null || (entity is SupplySignal) || (entity is SurveyCharge) || IsNPC(player)) return;
            if (InEvent(player) || InDuel(player)) return;
            if (!data.ContainsKey(player.userID))
            {
                CreatePlayerData(player.userID);
            }
            if (entity.ShortPrefabName == "grenade.beancan.deployed")
            {
                data[player.userID].raid.Beancan++;
                data[player.userID].raid.Explosive++;
            }
            if (entity.ShortPrefabName == "grenade.f1.deployed")
            {
                data[player.userID].raid.F1++;
                data[player.userID].raid.Explosive++;
            }
            if (entity.ShortPrefabName == "explosive.satchel.deployed")
            {
                data[player.userID].raid.Satchel++;
                data[player.userID].raid.Explosive++;
            }
            if (entity.ShortPrefabName == "explosive.timed.deployed")
            {
                data[player.userID].raid.TimedExpl++;
                data[player.userID].raid.Explosive++;
            }

        }

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null || IsNPC(player)) return;
            if (InEvent(player) || InDuel(player)) return;
            if (!data.ContainsKey(player.userID))
            {
                CreatePlayerData(player.userID);
            }
            if (entity.ShortPrefabName == "rocket_basic")
            {
                data[player.userID].raid.Rocket++;
                data[player.userID].raid.RocketLaunched++;
            }
            if (entity.ShortPrefabName == "rocket_fire")
            {
                data[player.userID].raid.Incendiary++;
                data[player.userID].raid.RocketLaunched++;
            }
            if (entity.ShortPrefabName == "rocket_hv")
            {
                data[player.userID].raid.High++;
                data[player.userID].raid.RocketLaunched++;
            }
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null) return;
            BasePlayer vict = victim.ToPlayer();
            if (vict == null) return;
            //killer
            if (info == null) return;
            BasePlayer killer = info.InitiatorPlayer;
            if (IsNPC(killer)) return;
            if (InEvent(vict) || InDuel(vict)) return;
            if (killer != null)
            {
                if (InDuel(killer))
                    return;
            }
            

            if (killer != null && killer != vict)
            {
                if (InEvent(killer) || InDuel(killer)) return;
                if (!data.ContainsKey(killer.userID))
                {
                    CreatePlayerData(killer.userID);
                }
                data[killer.userID].pvp.kills++;
            }
            if (IsNPC(vict)) return;
            if (!data.ContainsKey(vict.userID))
            {
                CreatePlayerData(vict.userID);
            }
            if (!InDuel(vict) || !IsNPC(vict)) data[vict.userID].pvp.deaths += 1;
        }


        void SaveData()
        {
            dataFile.WriteObject(data);

        }


        void OnServerSave()
        {
            SaveData();
        }

        void OnServerInitialized()
        {
            InitFileManager();
            CommunityEntity.ServerInstance.StartCoroutine(LoadImages());
            LoadConfig();
            LoadConfigValues();
            cmd.AddChatCommand(cmdConfigStat, this, showGui);
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
            }


        }

        IEnumerator LoadImages()
        {
            foreach (var imgKey in Images.Keys.ToList())
            {
                yield return CommunityEntity.ServerInstance.StartCoroutine(
                    m_FileManager.LoadFile(imgKey, Images[imgKey]));
                Images[imgKey] = m_FileManager.GetPng(imgKey);
            }
        }
        Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            { "imagesplayer", "http://i.imgur.com/Ok1A1b7.png" },
            { "imagesplayers", "http://i.imgur.com/eXfS40S.png" },
            { "imageskills", "http://i.imgur.com/YvsDkNs.png" },
            { "imagesgather", "http://i.imgur.com/WxQShQh.png" },
            { "imagesgather1", "https://i.imgur.com/yGmuasy.png" },
        };


        void OnNewSave()
        {
            if (!autoGrantOnWipe) return;
            GenerateLastWipeTopPlayers();
            data = new Dictionary<ulong, PlayerData>();
        }


        bool enabled = false;


        void GenerateLastWipeTopPlayers()
        {
            Dictionary<topSkill, ulong> topUsers = new Dictionary<topSkill, ulong>
            {
                {
                    topSkill.kills, data.OrderByDescending(x => x.Value.pvp.kills).Select(x => x.Key).First()
                },
                {
                    topSkill.raid, data.OrderByDescending(x => x.Value.raid.Explosive + x.Value.raid.RocketLaunched).Select(x => x.Key).First()
                },
                {
                    topSkill.gather, data.OrderByDescending(x => x.Value.gather.wood + x.Value.gather.stone + x.Value.gather.metalOre + x.Value.gather.sulfurOre + x.Value.gather.hqmetalOre).Select(x => x.Key).First()
                },
                {
                    topSkill.online, data.OrderByDescending(x => x.Value.minutes).Select(x => x.Key).First()
                }
            };

            List<SkillTopUser> topBuffer = new List<SkillTopUser>();

            foreach (KeyValuePair<topSkill, ulong> item in topUsers)
                topBuffer.Add(new SkillTopUser(item.Key, item.Value, GetWipeCount(item.Key, item.Value) + 1));

            var topformonth = topBuffer.FindAll(x => x.wipeCount >= 4);

            foreach (SkillTopUser user in topformonth)
            {
                topBuffer.Add(new SkillTopUser(topSkill.topformonth, user.userId, 0));
                user.wipeCount = 0;
            }

            skillTopUsers.Clear();
            skillTopUsers.AddRange(topBuffer);

            lastWipeTopUsersDataFile.WriteObject(skillTopUsers);
        }

        int GetWipeCount(topSkill skill, ulong userId) => (skillTopUsers.Find(x => x.skillType == skill && x.userId == userId)?.wipeCount ?? 0);

        void Loaded()
        {
            data = dataFile.ReadObject<Dictionary<ulong, PlayerData>>();
            time = timingFile.ReadObject<int>();
            timer.Every(3600, timingHandle);
            timer.Every(60, playtimeHandle);
            try
            {
                skillTopUsers = lastWipeTopUsersDataFile.ReadObject<List<SkillTopUser>>();
            }
            catch
            {
                skillTopUsers = new List<SkillTopUser>();
            }
        }

        void timingHandle()
        {
            time = time - 1;
            if (time <= 0)
            {
                PrintWarning("Отчет ТОП сохранен!");
                writeTop();
                time = timeInHours;
            }
            timingFile.WriteObject(time);
        }

        void playtimeHandle()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!data.ContainsKey(player.userID))
                {
                    CreatePlayerData(player.userID);
                }
                data[player.userID].minutes++;
            }
        }

        void Unload()
        {
            UnityEngine.Object.Destroy(FileManagerObject);
            SaveData();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "StatsParent");
                CuiHelper.DestroyUi(player, "TopParent");
                CuiHelper.DestroyUi(player, "PlayerStats_bp");
                CuiHelper.DestroyUi(player, "PlayerStats_bp2");
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            List<SkillTopUser> availableGifts = skillTopUsers.FindAll(x => x.userId == player.userID && !x.giftTaken);
            if (IsNPC(player)) return;
            if (!data.ContainsKey(player.userID))
            {
                CreatePlayerData(player.userID);
            }
            GetAvatar(player.userID, avatar =>
            {
                data[player.userID].avatar = avatar;
                CommunityEntity.ServerInstance.StartCoroutine(m_FileManager.LoadFile($"avatar{player.userID}", avatar));
                SaveData();
            });
            if (availableGifts.Count > 0) // .FindAll не возвращает null
            {
                timer.Once(10f, () =>
                {
                    SendReply(player, $"У вас есть призы по результатам статистики прошлого вайпа.\nДоступные призы: {string.Join(", ", availableGifts.Select(x => x.skillType.ToString()).ToArray())}.\nВведите /topgift <giftname> для получения приза.");
                });
            }
        }

        [ChatCommand("topgift")]
        void cmdChat_topGift(BasePlayer player, string cmd, string[] args)
        {
            List<SkillTopUser> userTOPs = skillTopUsers.FindAll(x => x.userId == player.userID && !x.giftTaken);

            if (userTOPs.Count < 1)
            {
                SendReply(player, "Нет доступных призов.");
                return;
            }

            if (args != null && args.Length > 0)
            {
                topSkill skill;

                try
                {
                    skill = (topSkill)Enum.Parse(typeof(topSkill), args[0].ToLower());
                }
                catch
                {
                    SendReply(player, "Введите /topgift <skillname> для получения приза.");
                    return;
                }

                SkillTopUser skillTopUser = userTOPs.Find(x => x.skillType == skill);

                if (skillTopUser == null)
                {
                    SendReply(player, $"Вы не получали призовое место за скилл {args[0]} или приз уже был получен.");
                    return;
                }
                if (args[0] == "kills")
                {
                    foreach (string command in Kills)
                    {
                        rust.RunServerCommand(command.Replace("{player}", player.userID.ToString()));
                    }
                }
                if (args[0] == "raid")
                {
                    foreach (string command in Raiders)
                    {
                        rust.RunServerCommand(command.Replace("{player}", player.userID.ToString()));
                    }
                }
                if (args[0] == "gather")
                {
                    foreach (string command in Gathers)
                    {
                        rust.RunServerCommand(command.Replace("{player}", player.userID.ToString()));
                    }
                }
                if (args[0] == "online")
                {
                    foreach (string command in Onlines)
                    {
                        rust.RunServerCommand(command.Replace("{player}", player.userID.ToString()));
                    }
                }
                if (args[0] == "topformonth")
                {
                    foreach (string command in Topformonths)
                    {
                        rust.RunServerCommand(command.Replace("{player}", player.userID.ToString()));
                    }
                }

                skillTopUser.giftTaken = true;

                lastWipeTopUsersDataFile.WriteObject(skillTopUsers);

                SendReply(player, "Приз успешно выдан.");

                return;
            }

            SendReply(player, $"Доступные призы: {string.Join(", ", userTOPs.Select(x => x.skillType.ToString()).ToArray())}.");
        }

        void showGui(BasePlayer player, string cmd, string[] args)
        {
            if (!data.ContainsKey(player.userID))
            {
                CreatePlayerData(player.userID);
            }
            drawWindow(player);
        }

        [ConsoleCommand("stat")]
        void drawstatConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player != null)
            {
                if (!data.ContainsKey(player.userID))
                {
                    CreatePlayerData(player.userID);
                }
                drawWindow(player);
            }
        }

        [ConsoleCommand("grantplayers")]
        void grantstatConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player == null || IsNPC(player))
            {
                GenerateLastWipeTopPlayers();
                PrintWarning("Игрокам ТОП один выданы бонусы!");
                data = new Dictionary<ulong, PlayerData>();
                SaveData();
            }
        }

        [ConsoleCommand("closestat2")]
        void destroyStat2(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            CuiHelper.DestroyUi(player, "PlayerStats_bp2");
        }

        [ConsoleCommand("drawTop")]
        void drawtopConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player == null || IsNPC(player)) return;
            CuiHelper.DestroyUi(player, "PlayerStats_bp");
            CreateUINext(player);
        }

        [ConsoleCommand("closestat")]
        void destroyStat(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            CuiHelper.DestroyUi(player, "PlayerStats_bp");
            CuiHelper.DestroyUi(player, "PlayerStats_bp2");
        }

        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin Duel;

        bool InEvent(BasePlayer player)
        {
            try
            {
                bool result = (bool)EventManager?.Call("isPlaying", new object[] { player });
                return result;
            }
            catch
            {
                return false;
            }
        }

        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        Dictionary<ulong, string> startGui = new Dictionary<ulong, string>();


        void GetAvatar(ulong uid, Action<string> callback)
        {
            if (callback == null) return;
            var reply = 1;
            if (reply == 0) { }
            string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=3F2959BD838BF8FB544B9A767F873457&" +
                "steamids=" + uid;
            webrequest.EnqueueGet(url,
                (i, json) => callback?.Invoke((string)JObject.Parse(json)["response"]["players"][0]["avatarfull"]),
                this);
        }

        string GUI = "[{\"name\":\"PlayerStats_bp\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0.3019608 0.4156863 0.3921569 0.3921569\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player\",\"parent\":\"PlayerStats_bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04853591 0.1542709\",\"anchormax\":\"0.2985358 0.9242711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_allstats\",\"parent\":\"bp_player\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.87957\",\"anchormax\":\"0.98 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_text\",\"parent\":\"bp_allstats\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"СТАТИСТИКА ИГРОКОВ\",\"fontSize\":24,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.8784314\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4203148\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"images_bp\",\"parent\":\"bp_player\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{imagesplayer}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.09713647 0.3486559\",\"anchormax\":\"0.8971364 0.8022848\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_namebp\",\"parent\":\"bp_player\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.2039216 0.2901961 0.2666667 0.5137255\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.1655244\",\"anchormax\":\"1 0.2836826\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Bp_name\",\"parent\":\"bp_namebp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{name}\",\"fontSize\":24,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.412474\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_timebp\",\"parent\":\"bp_player\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.2039216 0.2901961 0.2666667 0.5137255\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.04546227\",\"anchormax\":\"0.9999999 0.1547619\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Bp_timetitle\",\"parent\":\"bp_timebp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Вы на сервере:\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4124726\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.2475422\",\"anchormax\":\"1 1.247542\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Bp_timerinfo\",\"parent\":\"bp_timebp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{время на сервере}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4235241\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"-0.002928257 -0.2011282\",\"anchormax\":\"0.9970717 0.7988722\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeath\",\"parent\":\"PlayerStats_bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3285359 0.694271\",\"anchormax\":\"0.6685358 0.9242711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7235056\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killtext\",\"parent\":\"bp_killsdeathbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"PVP\",\"fontSize\":24,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3992829\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02846438 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_imageskill\",\"parent\":\"bp_killsdeathbp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{imageskills}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.89 0.07952497\",\"anchormax\":\"0.98 0.949525\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Убийств: {убийств}  Смертей: {смертей}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.1120924\",\"anchormax\":\"1 0.6329262\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gather\",\"parent\":\"PlayerStats_bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3285359 0.4229688\",\"anchormax\":\"0.6685358 0.652969\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gatherbp\",\"parent\":\"bp_gather\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7235056\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gathertitle\",\"parent\":\"bp_gatherbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Добыто руками\",\"fontSize\":24,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4106175\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02846438 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gatherimages\",\"parent\":\"bp_gatherbp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{imagesgather}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.87 0.149525\",\"anchormax\":\"0.9753553 0.828357\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gather1\",\"parent\":\"bp_gather\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Дерево:  {дерево}  Камень:  {камень}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4142732\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.4651261\",\"anchormax\":\"1 0.71422\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gather2\",\"parent\":\"bp_gather\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Серная руда:  {серная руда}  МВК:  {МВК}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4142732\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.2669832\",\"anchormax\":\"1 0.43682\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_gather3\",\"parent\":\"bp_gather\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Металическая руда:  {руда}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4142732\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.004529946\",\"anchormax\":\"1 0.2330159\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_raid\",\"parent\":\"PlayerStats_bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3285359 0.1542709\",\"anchormax\":\"0.6685358 0.3842711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_raidbp\",\"parent\":\"bp_raid\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7235056\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_raidtext\",\"parent\":\"bp_raidbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Использовано взрывчаток\",\"fontSize\":24,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3992815\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02846438 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_raidimages\",\"parent\":\"bp_raidbp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{imagesgather1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.89 0.03857487\",\"anchormax\":\"0.9800001 0.9085751\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_raidloun\",\"parent\":\"bp_raid\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Выпущено ракет:\nОбычных: {rocket}  Зажигательных: {incend}  Скоростных: {hight}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4142732\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 -0.002717715\",\"anchormax\":\"1 0.4218744\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_raidexp\",\"parent\":\"bp_raid\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"F1:  {f1}  C4:  {c4}  Beancan: {gbeancan}\nСумка с зарядом: {Satchel}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4142732\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0004735941 0.387907\",\"anchormax\":\"1 0.7219194\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1\",\"parent\":\"PlayerStats_bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6985359 0.1542709\",\"anchormax\":\"0.9485359 0.9242711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1rait\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"drawTop\",\"color\":\"0.2078431 0.2901961 0.2745098 0.5137255\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.2101457\",\"anchormax\":\"0.9999998 0.3001456\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1raittext\",\"parent\":\"bp_player1rait\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"< ОБЩИЙ РЕЙТИНГ >\",\"fontSize\":28,\"align\":\"MiddleCenter\",\"color\":\"0.317647 0.7411765 0.522305 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3233584\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1exitbp\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"closestat\",\"color\":\"0.2078431 0.2901961 0.2745098 0.5137255\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.08926517\",\"anchormax\":\"0.9999999 0.1792652\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1exitexit\",\"parent\":\"bp_player1exitbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"< ВЫХОД >\",\"fontSize\":28,\"align\":\"MiddleCenter\",\"color\":\"0.3137255 0.7411765 0.5215687 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3233584\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1images\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{imagesplayers}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.09713647 0.3486559\",\"anchormax\":\"0.8971364 0.8022848\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]}]";

        string NextGUI = "[{\"name\":\"PlayerStats_bp2\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0.3019608 0.4156863 0.3921569 0.3921569\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player\",\"parent\":\"PlayerStats_bp2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04853591 0.1542709\",\"anchormax\":\"0.2985358 0.9242711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_allstats\",\"parent\":\"bp_player\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.9224834\",\"anchormax\":\"0.98 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_text\",\"parent\":\"bp_allstats\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП 20 СЕРВЕРА\",\"fontSize\":24,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.8784314\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4203148\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext\",\"parent\":\"bp_player\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{ТОП 20}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.880208\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeath\",\"parent\":\"PlayerStats_bp2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3285359 0.1549479\",\"anchormax\":\"0.6685358 0.9242711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.92\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_imageskill\",\"parent\":\"bp_killsdeathbp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{imageskills}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.89 0.07952497\",\"anchormax\":\"0.98 0.949525\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killtext\",\"parent\":\"bp_killsdeathbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП в разных категориях\",\"fontSize\":24,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3960784\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02415806 0\",\"anchormax\":\"0.9715356 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp1\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.1048265\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.46\",\"anchormax\":\"0.5 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext1\",\"parent\":\"bp_killsdeathbp1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП 10 убийц:\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"1 0.4223743 0.4223743 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8779236\",\"anchormax\":\"1 0.9779239\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext\",\"parent\":\"bp_killsdeathbp1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{ТОП 10 убийц}\",\"fontSize\":17,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.8779238\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp1\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.4616926\",\"anchormax\":\"1.001679 0.9216925\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext1\",\"parent\":\"bp_killsdeathbp1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП 10 рейдеров:\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"1 0.4223743 0.4223743 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8779236\",\"anchormax\":\"1 0.9779239\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext\",\"parent\":\"bp_killsdeathbp1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{ТОП 10 рейдеров}\",\"fontSize\":17,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.8779238\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp2\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.1048265\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"1 0.46\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext3\",\"parent\":\"bp_killsdeathbp2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП 10 смертей:\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"1 0.4223743 0.4223743 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8779236\",\"anchormax\":\"1 0.9779239\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext\",\"parent\":\"bp_killsdeathbp2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{ТОП 10 смертей}\",\"fontSize\":17,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.8779238\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp2\",\"parent\":\"bp_killsdeath\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.5 0.46\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext6\",\"parent\":\"bp_killsdeathbp2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП 10 по ресурсам:\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"1 0.4223743 0.4223743 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8779236\",\"anchormax\":\"1 0.9779239\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext\",\"parent\":\"bp_killsdeathbp2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{ТОП 10 ресурсы}\",\"fontSize\":17,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.8779238\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1\",\"parent\":\"PlayerStats_bp2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6985359 0.1542709\",\"anchormax\":\"0.9485359 0.9242711\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1rait\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"stat\",\"color\":\"0.2039216 0.2901961 0.2666667 0.5137255\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.2101457\",\"anchormax\":\"0.9999998 0.3001456\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1raittext\",\"parent\":\"bp_player1rait\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"< НАЗАД >\",\"fontSize\":28,\"align\":\"MiddleCenter\",\"color\":\"0.317647 0.7411765 0.522305 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3233584\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1exitbp\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"closestat2\",\"color\":\"0.2078431 0.2901961 0.2745098 0.5137255\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.08926517\",\"anchormax\":\"0.9999999 0.1792652\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_player1exitexit\",\"parent\":\"bp_player1exitbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"< ВЫХОД >\",\"fontSize\":28,\"align\":\"MiddleCenter\",\"color\":\"0.3137255 0.7411765 0.5215687 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3233584\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp2\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.33\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext5\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{ТОП 10 время}\",\"fontSize\":17,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3700673\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.32\",\"anchormax\":\"1 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killsdeathbp\",\"parent\":\"bp_player1\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3019608 0.4156863 0.3921569 0.8784314\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.92\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp_killdeathtext4\",\"parent\":\"bp_killsdeathbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Топ 15 по онлайну\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3921569\",\"distance\":\"2 -2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        void CreateUINext(BasePlayer player)
        {
            topList topdata = calcSum();
            string topTop = topdata.arr;

            IOrderedEnumerable<KeyValuePair<ulong, PlayerData>> items = from pair in data orderby pair.Value.pvp.kills descending select pair;

            int i = 1;
            string topPvp = "";
            foreach (KeyValuePair<ulong, PlayerData> pair in items)
            {
                string name = pair.Value.name;
                if (name == null) name = "Неизвестный";
                topPvp = topPvp + i.ToString() + ". " + name + " - " + pair.Value.pvp.kills.ToString() + "\n";
                i++;
                if (i > 10) break;
            }

            int i1 = 1;
            
            items = from pair in data orderby pair.Value.pvp.deaths descending select pair;
            string topPve = "";
            foreach (KeyValuePair<ulong, PlayerData> pair in items)
            {
                string name = pair.Value.name;
                if (name == null) name = "Неизвестный";
                topPve = topPve + i1.ToString() + ". " + name + " - " + pair.Value.pvp.deaths.ToString() + "\n";
                i1++;
                if (i1 > 10) break;
            }

            items = from pair in data orderby pair.Value.minutes descending select pair;
            i = 1;
            string topTime = "";
            foreach (KeyValuePair<ulong, PlayerData> pair in items)
            {
                string name = pair.Value.name;
                if (name == null) name = "Неизвестный";
                topTime = topTime + i.ToString() + ". " + name + " - " + FormatTime(TimeSpan.FromMinutes(pair.Value.minutes)) + "\n";
                i++;
                if (i > 15) break;
            }

            items = from pair in data orderby (pair.Value.gather.wood + pair.Value.gather.stone + pair.Value.gather.metalOre + pair.Value.gather.sulfurOre + pair.Value.gather.hqmetalOre) descending select pair;
            i = 1;
            string topGve = "";
            foreach (KeyValuePair<ulong, PlayerData> pair in items)
            {
                string name = pair.Value.name;
                if (name == null) name = "Неизвестный";
                topGve = topGve + i.ToString() + ". " + name + " - " + (pair.Value.gather.wood + pair.Value.gather.stone + pair.Value.gather.metalOre + pair.Value.gather.sulfurOre +
                pair.Value.gather.hqmetalOre).ToString() + "\n";
                i++;
                if (i > 10) break;
            }

            items = from pair in data orderby (pair.Value.raid.Explosive + pair.Value.raid.RocketLaunched) descending select pair;
            i = 1;
            string topRaid = "";
            foreach (KeyValuePair<ulong, PlayerData> pair in items)
            {
                string name = pair.Value.name;
                if (name == null) name = "Неизвестный";
                topRaid = topRaid + i.ToString() + ". " + name + " - " + (pair.Value.raid.Explosive + pair.Value.raid.RocketLaunched).ToString() + "\n";
                i++;
                if (i > 10) break;
            }

            CuiHelper.DestroyUi(player, "PlayerStats_bp2");
            CuiHelper.AddUi(player,
                NextGUI.Replace("{name}", data[player.userID].name.ToString())
                .Replace("{imageskills}", Images["imageskills"])
                .Replace("{ТОП 20}", topTop)
                .Replace("{ТОП 10 убийц}", topPvp)
                .Replace("{ТОП 10 смертей}", topPve)
                .Replace("{ТОП 10 время}", topTime)
                .Replace("{ТОП 10 ресурсы}", topGve)
                .Replace("{ТОП 10 рейдеров}", topRaid));
        }


        void CreateUI(BasePlayer player)
        {
            float points = kills * data[player.userID].pvp.kills - deaths * data[player.userID].pvp.deaths + resGather * (data[player.userID].gather.wood + data[player.userID].gather.stone + data[player.userID].gather.metalOre + data[player.userID].gather.sulfurOre + data[player.userID].gather.hqmetalOre) +
            Raider * (data[player.userID].raid.Beancan + data[player.userID].raid.F1 + data[player.userID].raid.High + data[player.userID].raid.Incendiary + data[player.userID].raid.Rocket + data[player.userID].raid.Satchel + data[player.userID].raid.TimedExpl) + level;

            CuiHelper.DestroyUi(player, "PlayerStats_bp");
            CuiHelper.AddUi(player,
                GUI.Replace("{name}", data[player.userID].name.ToString())
                .Replace("{убийств}", data[player.userID].pvp.kills.ToString())
                .Replace("{смертей}", data[player.userID].pvp.deaths.ToString())

                .Replace("{дерево}", data[player.userID].gather.wood.ToString())
                .Replace("{камень}", data[player.userID].gather.stone.ToString())
                .Replace("{руда}", data[player.userID].gather.metalOre.ToString())
                .Replace("{серная руда}", data[player.userID].gather.sulfurOre.ToString())
                .Replace("{МВК}", data[player.userID].gather.hqmetalOre.ToString())

                .Replace("{f1}", data[player.userID].raid.F1.ToString())
                .Replace("{c4}", data[player.userID].raid.TimedExpl.ToString())
                .Replace("{gbeancan}", data[player.userID].raid.Beancan.ToString())
                .Replace("{Satchel}", data[player.userID].raid.Satchel.ToString())

                .Replace("{rocket}", data[player.userID].raid.Rocket.ToString())
                .Replace("{incend}", data[player.userID].raid.Incendiary.ToString())
                .Replace("{hight}", data[player.userID].raid.High.ToString())

                .Replace("{время на сервере}", FormatTime(TimeSpan.FromMinutes(data[player.userID].minutes)))

                .Replace("{imagesplayer}", m_FileManager.GetPng("avatar" + player.userID))
                .Replace("{imagesplayers}", Images["imagesplayers"])
                .Replace("{imagesgather}", Images["imagesgather"])
                .Replace("{imagesgather1}", Images["imagesgather1"])
                .Replace("{imageskills}", Images["imageskills"])
                );
        }

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минуту")} ";

            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        void drawWindow(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "StatsParent");
            CuiHelper.DestroyUi(player, "PlayerStats_bp");
            CuiHelper.DestroyUi(player, "PlayerStats_bp2");
            CreateUI(player);
        }


        topList calcSum()
        {
            topList a = new topList();
            a.list = new List<string>();
            var culture = new CultureInfo("en-GB");
            a.list.Add(DateTime.Now.ToString(culture));
            IOrderedEnumerable<KeyValuePair<ulong, PlayerData>> items = from pair in data
                                                                        orderby
            (kills * pair.Value.pvp.kills - deaths * pair.Value.pvp.deaths + resGather * (pair.Value.gather.wood + pair.Value.gather.stone + pair.Value.gather.metalOre + pair.Value.gather.sulfurOre + pair.Value.gather.hqmetalOre) +
                Raider * (pair.Value.raid.Beancan + pair.Value.raid.F1 + pair.Value.raid.High + pair.Value.raid.Incendiary + pair.Value.raid.Rocket + pair.Value.raid.Satchel + pair.Value.raid.TimedExpl) + level)
            descending
                                                                        select pair;
            int i = 1;
            a.arr = "";
            foreach (KeyValuePair<ulong, PlayerData> pair in items)
            {
                string name = pair.Value.name;
                if (name == null) name = "Неизвестный";
                string newstring = i.ToString() + ". " + name + " - " + (kills * pair.Value.pvp.kills - deaths * pair.Value.pvp.deaths + resGather * (pair.Value.gather.wood + pair.Value.gather.stone + pair.Value.gather.metalOre + pair.Value.gather.sulfurOre + pair.Value.gather.hqmetalOre) +
                Raider * (pair.Value.raid.Beancan + pair.Value.raid.F1 + pair.Value.raid.High + pair.Value.raid.Incendiary + pair.Value.raid.Rocket + pair.Value.raid.Satchel + pair.Value.raid.TimedExpl) + level).ToString() + "\n";
                a.arr = a.arr + newstring;
                newstring = newstring.Replace("\n", "");
                a.list.Add(newstring);
                i++;
                if (i > topCount) break;
            }
            return a;
        }

        class topList
        {
            public List<string> list;
            public string arr;
        }

        #region Help on Next update
        //float points = kills * data[player.userID].pvp.kills - deaths * data[player.userID].pvp.deaths + resGather * (data[player.userID].gather.wood + data[player.userID].gather.stone + data[player.userID].gather.metalOre + data[player.userID].gather.sulfurOre + data[player.userID].gather.hqmetalOre) +
        //   resQuarry * (data[player.userID].quarry.stone + data[player.userID].quarry.metal + data[player.userID].quarry.metalOre + data[player.userID].quarry.sulfurOre + data[player.userID].quarry.hqmetal + data[player.userID].quarry.hqmetalOre) + level;

        #endregion


        void writeTop()
        {
            topList topdata = calcSum();
            topFile.WriteObject(topdata.list);
        }


        //cache
        BasePlayer findPlayer(string name)
        {
            List<BasePlayer> list = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(name.ToLower())) list.Add(player);
            }
            if (list.Count == 0) return null;
            if (list.Count > 1) return null;
            return list[0];
        }

        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        #region Images Load

        private GameObject FileManagerObject;
        private FileManager m_FileManager;
        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject("PlayerStats_FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }
        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();


            private class FileInfo
            {
                public string Url;
                public string Png;
            }


            public string GetPng(string name) => files[name].Png;

            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url, size));
            }

            IEnumerator LoadImageCoroutine(string name, string url, int size = -1)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        var bytes = www.bytes;

                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId).ToString();
                        files[name].Png = crc32;
                    }
                }
                loaded++;
            }
            #endregion
        }

    }
}
               