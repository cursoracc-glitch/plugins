using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DeathStats", "SkiTles", "0.3")]
    [Description("Отображение статистики на экране смерти")]
    class DeathStats : RustPlugin
    {
        private List<string> openUI = new List<string>();
        private bool NewWipe = false;

        #region OxideHooks
        void OnServerInitialized()
        {
            DSdata = Interface.Oxide.DataFileSystem.GetFile("DeathStats");
            LoadData();
            if (NewWipe)
            {
                foreach (var player in data.PlayersStats.Keys)
                {
                    ClearStats(player);
                }
                SaveData();
            }
            int changes = 0;
            foreach (var player in BasePlayer.activePlayerList)
            {
                AddPlayerT(player);
                if (!data.PlayersStats.ContainsKey(player.userID))
                {
                    AddPlayer(player);
                    changes++;
                }
            }
            if (changes > 0) SaveData();
        }
        void OnNewSave(string filename)
        {
            NewWipe = true;
        }
        void OnServerSave() => SaveData();
        void Unload()
        {
            SaveData();
            foreach (var entry in openUI)
            {
                var player = BasePlayer.Find(entry);
                if (player == null) continue;
                CuiHelper.DestroyUi(player, "StatsGUI");
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            AddPlayerT(player);
            if (!data.PlayersStats.ContainsKey(player.userID))
            {
                AddPlayer(player);
                SaveData();
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            TempStats.Remove(player.userID);
            if (openUI.Contains(player.UserIDString)) openUI.Remove(player.UserIDString);
        }
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.name.Contains("corpse")) return;
            if (info == null) return;
            if (entity is BasePlayer)
            {
                var victim = entity?.ToPlayer();
                if (victim == null) return;
                if (!IsNPC(victim) && data.PlayersStats.ContainsKey(victim.userID) && TempStats.ContainsKey(victim.userID))
                {
                    data.PlayersStats[victim.userID].deaths++;
                    openUI.Add(victim.UserIDString);
                    DrawGUI(victim);
                }
                var killer = info?.Initiator?.ToPlayer();
                if (killer == null) return;
                if (killer == victim) return;
                if (IsNPC(killer) || !data.PlayersStats.ContainsKey(killer.userID) || !TempStats.ContainsKey(killer.userID)) return;
                data.PlayersStats[killer.userID].kills++;
                TempStats[killer.userID].kills++;
            }
        }
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null) return;
            if (openUI.Contains(player.UserIDString)) DestroyGUI(player);
            ClearStatsT(player);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null) return;
            if (openUI.Contains(player.UserIDString)) DestroyGUI(player);
            ClearStatsT(player);
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            data.PlayersStats[player.userID].shoots++;
            TempStats[player.userID].shoots++;
        }
        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (hitinfo == null || attacker == null || !attacker.IsConnected) return;
            if (hitinfo.HitEntity is BaseNpc) return;
            var victim = hitinfo.HitEntity as BasePlayer;
            if (victim == null) return;
            if (victim == attacker) return;
            if (hitinfo.isHeadshot)
            {
                data.PlayersStats[attacker.userID].hs++;
                TempStats[attacker.userID].hs++;
            }
            data.PlayersStats[attacker.userID].hits++;
            TempStats[attacker.userID].hits++;
            if (hitinfo.damageTypes.IsMeleeType()) Puts("melee");
            int damage = (int)hitinfo.damageTypes.Total();
            if (damage > 0)
            {
                data.PlayersStats[attacker.userID].damage = data.PlayersStats[attacker.userID].damage + damage;
                TempStats[attacker.userID].dmg = TempStats[attacker.userID].dmg + damage;
            }
        }
        #endregion

        #region GUI
        private CuiElement MainPanel(string name, string color, string anMin, string anMax)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent { Color = color },
                    new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Panel(string name, string parent, string color, string anMin, string anMax)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiImageComponent { Color = color },
                    new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Text(string parent, string color, string text, TextAnchor pos, string fname, int fsize, string anMin, string anMax)
        {
            var Element = new CuiElement()
            {
                Parent = parent,
                Components =
                {
                    new CuiTextComponent() { Color = color, Text = text, Align = pos, Font = fname, FontSize = fsize },
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Button(string name, string parent, string command, string color, string anMin, string anMax)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiButtonComponent { Command = command, Color = color},
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private void DestroyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "StatsGUI");
            openUI.Remove(player.UserIDString);
        }
        private void DrawGUI(BasePlayer player)
        {
            string fcolor = "1 1 1 0.35";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(MainPanel("StatsGUI", "1 1 1 0", "0 0.2", "0.26 0.756"));
            container.Add(Panel("Lpanel", "StatsGUI", "1 1 1 0", "0 0.2", "0.7 1"));
            container.Add(Panel("Fpanel", "StatsGUI", "1 1 1 0", "0.713 0.2", "1 1"));
            container.Add(Panel("Bpanel", "StatsGUI", "1 1 1 0", "0 0", "1 0.19"));
            //container.Add(Text("Lpanel", fcolor, "Статистика жизни:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 18, "0 0.83", "1 0.91"));
            container.Add(Text("Lpanel", fcolor, "Выстрелов:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.78", "1 0.83"));
            container.Add(Text("Lpanel", fcolor, "Попаданий:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.73", "1 0.78"));
            container.Add(Text("Lpanel", fcolor, "Хедшотов:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.68", "1 0.73"));
            container.Add(Text("Lpanel", fcolor, "Точность:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.63", "1 0.68"));
            container.Add(Text("Lpanel", fcolor, "Урон:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.58", "1 0.63"));
            container.Add(Text("Lpanel", fcolor, "Убийств:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.53", "1 0.58"));
            container.Add(Text("Lpanel", fcolor, "Общая статистика:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 18, "0 0.43", "1 0.51"));
            container.Add(Text("Lpanel", fcolor, "Выстрелов:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.38", "1 0.43"));
            container.Add(Text("Lpanel", fcolor, "Попаданий:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.33", "1 0.38"));
            container.Add(Text("Lpanel", fcolor, "Хедшотов:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.28", "1 0.33"));
            container.Add(Text("Lpanel", fcolor, "Точность:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.23", "1 0.28"));
            container.Add(Text("Lpanel", fcolor, "Убийств:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.18", "1 0.23"));
            container.Add(Text("Lpanel", fcolor, "Смертей:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.13", "1 0.18"));
            container.Add(Text("Lpanel", fcolor, "Соотношение У/С:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.08", "1 0.13"));
            container.Add(Text("Lpanel", fcolor, "Средний урон:", TextAnchor.MiddleRight, "robotocondensed-bold.ttf", 14, "0 0.03", "1 0.08"));
            container.Add(Text("Fpanel", fcolor, TempStats[player.userID].shoots.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.78", "1 0.83"));
            container.Add(Text("Fpanel", fcolor, TempStats[player.userID].hits.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.73", "1 0.78"));
            container.Add(Text("Fpanel", fcolor, TempStats[player.userID].hs.ToString() + " (" + GetAccuracy(TempStats[player.userID].hs, TempStats[player.userID].hits) + ")", TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.68", "1 0.73"));
            container.Add(Text("Fpanel", fcolor, GetAccuracy(TempStats[player.userID].hits, TempStats[player.userID].shoots), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.63", "1 0.68"));
            container.Add(Text("Fpanel", fcolor, TempStats[player.userID].dmg.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.58", "1 0.63"));
            container.Add(Text("Fpanel", fcolor, TempStats[player.userID].kills.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.53", "1 0.58"));
            container.Add(Text("Fpanel", fcolor, data.PlayersStats[player.userID].shoots.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.38", "1 0.43"));
            container.Add(Text("Fpanel", fcolor, data.PlayersStats[player.userID].hits.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.33", "1 0.38"));
            container.Add(Text("Fpanel", fcolor, data.PlayersStats[player.userID].hs.ToString() + " (" + GetAccuracy(data.PlayersStats[player.userID].hs, data.PlayersStats[player.userID].hits) + ")", TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.28", "1 0.33"));
            container.Add(Text("Fpanel", fcolor, GetAccuracy(data.PlayersStats[player.userID].hits, data.PlayersStats[player.userID].shoots), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.23", "1 0.28"));
            container.Add(Text("Fpanel", fcolor, data.PlayersStats[player.userID].kills.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.18", "1 0.23"));
            container.Add(Text("Fpanel", fcolor, data.PlayersStats[player.userID].deaths.ToString(), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.13", "1 0.18"));
            container.Add(Text("Fpanel", fcolor, GetKD(data.PlayersStats[player.userID].kills, data.PlayersStats[player.userID].deaths), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.08", "1 0.13"));
            container.Add(Text("Fpanel", fcolor, GetAVG(data.PlayersStats[player.userID].damage, data.PlayersStats[player.userID].deaths), TextAnchor.MiddleLeft, "robotocondensed-bold.ttf", 14, "0 0.03", "1 0.08")); //avg dmg = dmg/deaths
            //button clearstats
            container.Add(Button("Reset", "Bpanel", "ds.clear", "0.7 1 0.6 0.4", "0.5 0.7", "0.9 1"));
            container.Add(Text("Reset", "1 1 1 1", "Сбросить статистику", TextAnchor.MiddleCenter, "robotocondensed-bold.ttf", 14, "0 0", "1 1"));
            //button closestats
            container.Add(Button("Close", "Bpanel", "ds.close", "1 0 0 0.4", "0.5 0.3", "0.9 0.6"));
            container.Add(Text("Close", "1 1 1 1", "Закрыть", TextAnchor.MiddleCenter, "robotocondensed-bold.ttf", 14, "0 0", "1 1"));
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Commands
        [ConsoleCommand("ds.clear")]
        private void Clear(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            ClearStats(player.userID);
            SaveData();
        }
        [ConsoleCommand("ds.close")]
        private void Close(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            DestroyGUI(player);
        }
        #endregion

        #region Data
        class PlayerInfo
        {
            public int shoots;
            public int hits;
            public int hs;
            public int kills;
            public int deaths;
            public int damage;
        }
        class DataStorageStats
        {
            public Dictionary<ulong, PlayerInfo> PlayersStats = new Dictionary<ulong, PlayerInfo>();
            public DataStorageStats() { }
        }
        DataStorageStats data;
        private DynamicConfigFile DSdata;
        void LoadData()
        {
            try { data = Interface.GetMod().DataFileSystem.ReadObject<DataStorageStats>("DeathStats"); }
            catch { data = new DataStorageStats(); }
        }
        void SaveData()
        {
            DSdata.WriteObject(data);
        }
        private Dictionary<ulong, TempPlayerInfo> TempStats = new Dictionary<ulong, TempPlayerInfo>();
        class TempPlayerInfo
        {
            public int shoots;
            public int hits;
            public int hs;
            public int dmg;
            public int kills;
        }
        #endregion

        #region Helpers
        private string GetKD(int kills, int deaths)
        {
            string kd = "0";
            if (kills > 0)
            {
                kd = ((double)kills / deaths).ToString();
                if (kd.Length > 3) kd = kd.Remove(3);
            }
            return kd;
        }
        private string GetAccuracy(int hits, int shoots)
        {
            string acc = "0%";
            if (shoots < hits)
            {
                acc = "100%";
            }
            if (shoots > hits && hits > 0 && shoots > 0)
            {
                acc = (((double)hits / shoots) * 100).ToString();
                if (acc.Length > 2) acc = acc.Remove(2);
                acc = acc + "%";
            }
            return acc;
        }
        private string GetAVG(int dmg, int deaths)
        {
            string avg = "0";
            if (dmg == 0) return avg;
            avg = ((double)dmg / deaths).ToString();
            if (avg.Length > 3) avg = avg.Remove(3);
            return avg;
        }
        private void AddPlayer(BasePlayer player)
        {
            data.PlayersStats.Add(player.userID, new PlayerInfo()
            {
                shoots = 0,
                hits = 0,
                hs = 0,
                kills = 0,
                deaths = 0,
                damage = 0
            });
        }
        private void AddPlayerT(BasePlayer player)
        {
            TempStats.Add(player.userID, new TempPlayerInfo()
            {
                shoots = 0,
                hits = 0,
                hs = 0,
                dmg = 0,
                kills = 0
            });
        }
        private void ClearStatsT(BasePlayer player)
        {
            TempStats[player.userID].shoots = 0;
            TempStats[player.userID].hits = 0;
            TempStats[player.userID].hs = 0;
            TempStats[player.userID].dmg = 0;
            TempStats[player.userID].kills = 0;
        }
        private void ClearStats(ulong userid)
        {
            data.PlayersStats[userid].damage = 0;
            data.PlayersStats[userid].deaths = 0;
            data.PlayersStats[userid].hits = 0;
            data.PlayersStats[userid].hs = 0;
            data.PlayersStats[userid].kills = 0;
            data.PlayersStats[userid].shoots = 0;
        }
        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer)
                return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }
        #endregion
    }
}