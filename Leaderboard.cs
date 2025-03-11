using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Leaderboard", "Sempai#3239", "1.0.0")]
    class Leaderboard : RustPlugin
    {
        #region Вар
        string Layer = "Leaderboard_UI";

        Dictionary<ulong, DataBase> DB = new Dictionary<ulong, DataBase>();
        #endregion

        #region Класс
        public class DataBase
        {
            public string DisplayName;
            public bool IsConnected;
            public int Kills;
            public int Deaths;
            public int Hs;
            public int Hits;
        }
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Leaderboard/PlayerList"))
                DB = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DataBase>>("Leaderboard/PlayerList");

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!DB.ContainsKey(player.userID))
                DB.Add(player.userID, new DataBase());

            DB[player.userID].DisplayName = player.displayName;
            DB[player.userID].IsConnected = true;
        }

        void OnPlayerDisconnected(BasePlayer player) 
        {
            DB[player.userID].IsConnected = false;
            SaveDataBase();
        }
        
        void Unload()
        {
            SaveDataBase();
        }

        void SaveDataBase() 
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("Leaderboard/PlayerList", DB); 
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc || info.InitiatorPlayer == null || info.InitiatorPlayer.IsNpc) return;
            
            if (info.InitiatorPlayer != null)
            {
                var killer = info.InitiatorPlayer;

                if (killer != player) 
                { 
                    if (DB.ContainsKey(killer.userID))
                    {
                        DB[killer.userID].Kills++;
                    }
                }
                if (DB.ContainsKey(player.userID))
                {
                    DB[player.userID].Deaths++;
                }
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
            {
                var attacker = info.InitiatorPlayer;  
                
                if (attacker != null)
                {
                    if (!attacker.IsNpc)
                    {
                        if (info.isHeadshot)
                        {
                            DB[attacker.userID].Hs++;
                        }
                        DB[attacker.userID].Hits++;
                    }
                }
            }
            
        }
        #endregion

        #region Команда
        [ConsoleCommand("stats")]
        void ConsoleStats(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            Leader(player, int.Parse(args.Args[0]));
        }
        #endregion

        #region Интерфейс
        void LeaderboardUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.284 0", AnchorMax = "0.952 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.6" },
            }, "Menu", Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.032 0.893", AnchorMax = $"0.347 0.954", OffsetMax = "0 0" },
                Image = { Color = "0.86 0.55 0.35 1" }
            }, Layer, "Title");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"LEADERBOARDS", Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-bold.ttf" }
            }, "Title");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.36 0.893", AnchorMax = $"0.97 0.954", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = "Leaderboards updated when opening", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Description");

            CuiHelper.AddUi(player, container);
            Leader(player);
        }

        void Leader(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, "Lead");
            var container = new CuiElementContainer();
                        
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" },
            }, Layer, "Lead");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.85 0.03", AnchorMax = $"0.97 0.09", OffsetMax = "0 0" },
                Button = { Color = "0.86 0.55 0.35 1", Command = DB.Count() > (page + 1) * 15 ? $"stats {page + 1}" : "" },
                Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Lead");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.72 0.03", AnchorMax = $"0.84 0.09", OffsetMax = "0 0" },
                Button = { Color = "0.86 0.55 0.35 1", Command = page >= 1 ? $"stats {page - 1}" : "" },
                Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Lead");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.11", AnchorMax = $"0.97 0.19", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.2" }
            }, "Lead", "Stats");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.03 0.81", AnchorMax = $"0.97 0.85", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.5" },
                Text = { Text = $"        #                          Player Name                         Kills                          Deaths                          KDR                          HeadShots                          Hits                         Online", Color = "1 1 1 1", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Lead");

            float width = 0.945f, height = 0.04f, startxBox = 0.028f, startyBox = 0.805f - height, xmin = startxBox, ymin = startyBox, z = 0;
            var target = from targets in DB orderby targets.Value.Kills descending select targets;
            foreach (var check in target.Skip(page * 15).Take(15))
            {
                z++;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 1", OffsetMax = "-2 -1" },
                    Image = { Color = "0 0 0 0.5" }
                 }, "Lead", "PlayerTop");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.8 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.06 1", OffsetMax = "0 0" },
                    Text = { Text = $"{z + page * 15}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.07 0", AnchorMax = $"0.25 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.DisplayName}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.26 0", AnchorMax = $"0.32 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.Kills}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.38 0", AnchorMax = $"0.44 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.Deaths}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                var kdr = check.Value.Deaths == 0 ? check.Value.Kills : (float)Math.Round(((float)check.Value.Kills) / check.Value.Deaths, 1);
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.5 0", AnchorMax = $"0.56 1", OffsetMax = "0 0" },
                    Text = { Text = $"{kdr}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.63 0", AnchorMax = $"0.69 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.Hs}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.76 0", AnchorMax = $"0.82 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.Hits}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                var online = check.Value.IsConnected == true ? "online" : "offline";
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.87 0", AnchorMax = $"0.94 1", OffsetMax = "0 0" },
                    Text = { Text = $"{online}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.08 1", OffsetMax = "0 0" },
                Text = { Text = "YOUR\nSTATS", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, "Stats");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.08 0.1", AnchorMax = $"0.25 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Stats", "Kill");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Kills: {DB[player.userID].Kills}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, "Kill");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.26 0.1", AnchorMax = $"0.43 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Stats", "Death");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Death: {DB[player.userID].Deaths}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, "Death");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.44 0.1", AnchorMax = $"0.61 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Stats", "Kdr");
            
            var kd = DB[player.userID].Deaths == 0 ? DB[player.userID].Kills : (float)Math.Round(((float)DB[player.userID].Kills) / DB[player.userID].Deaths, 1);
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Kdr: {kd}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, "Kdr");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.62 0.1", AnchorMax = $"0.79 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Stats", "Hs");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"HeadShots: {DB[player.userID].Hs}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, "Hs");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.8 0.1", AnchorMax = $"0.97 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Stats", "Hits");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Hits: {DB[player.userID].Hits}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, "Hits");

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}