using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("RPGFarm", "Replace ServerRust", "0.1.1")]
    public class RPGFarm : RustPlugin
    {
        private static readonly string _permissionMax = "rpgfarm.max";


        Dictionary<ulong, string> playerResolution = new Dictionary<ulong, string>();


        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("RPGFarm");

        Dictionary<ulong, UserStats> userStats;

        Dictionary<Skills, double> MaxRate = new Dictionary<Skills, double>
        {
            {Skills.Cutting, 10.0},
            {Skills.Mining, 10.0},
            {Skills.Skinning, 10.0}
        };

        Dictionary<Skills, int> MaxHits = new Dictionary<Skills, int>
        {
            {Skills.Cutting, 3000},
            {Skills.Mining, 2000},
            {Skills.Skinning, 1000}
        };

        Dictionary<Skills, string> SkillTranslate = new Dictionary<Skills, string>
        {
            {Skills.Cutting, "Дровосек"},
            {Skills.Mining, "Рудокоп"},
            {Skills.Skinning, "Охотник"}
        };

        Dictionary<Skills, string> skillColors = new Dictionary<Skills, string>
        {
            {Skills.Cutting, "0 0.6 1 0.8"},
            {Skills.Mining, "0.95 0.75 0.05 0.8"},
            {Skills.Skinning, "0.5 0.75 0 0.8"}
        };

        enum Skills
        {
            Cutting,
            Mining,
            Skinning
        }

        class UserStats
        {
            public Dictionary<Skills, int> Hits = new Dictionary<Skills, int>
            {
                {Skills.Cutting, 0},
                {Skills.Mining, 0},
                {Skills.Skinning, 0}
            };
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(_permissionMax, this);
        }

        void Loaded()
        {

            try
            {
                userStats = dataFile.ReadObject<Dictionary<ulong, UserStats>>();
            }
            catch
            {
                userStats = new Dictionary<ulong, UserStats>();
            }



            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "StatsUI");
            }

            dataFile.WriteObject(userStats);
        }

        void OnServerSave()
        {
            dataFile.WriteObject(userStats);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null)
                return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }

            UserStats stats;

            if (!userStats.TryGetValue(player.userID, out stats))
            {
                userStats.Add(player.userID, new UserStats()); // #2 stats ?
            }

            if (permission.UserHasPermission(player.UserIDString, _permissionMax))
            {
                stats.Hits[Skills.Cutting] = MaxHits[Skills.Cutting];
                stats.Hits[Skills.Mining] = MaxHits[Skills.Mining];
                stats.Hits[Skills.Skinning] = MaxHits[Skills.Skinning];
            }
            RenderUI(player);
        }

        void OnUserPermissionGranted(string id, string perm)
        {
            if (perm == _permissionMax)
            {
                ulong userId = Convert.ToUInt64(id);

                UserStats stats; 

                if (userStats.TryGetValue(userId, out stats))
                {
                    stats.Hits[Skills.Cutting] = MaxHits[Skills.Cutting];
                    stats.Hits[Skills.Mining] = MaxHits[Skills.Mining];
                    stats.Hits[Skills.Skinning] = MaxHits[Skills.Skinning];

                    BasePlayer targetPlayer = BasePlayer.FindByID(userId);

                    if (targetPlayer != null)
                    {
                        if (targetPlayer.IsConnected)
                        {
                            RenderUI(targetPlayer);
                            SendReply(targetPlayer, "Вы подключили услугу «Полная прокачка RPG».");
                        }
                    }
                }
            }
        }

        void OnUserPermissionRevoked(string id, string perm)
        {
            if (perm == _permissionMax)
            {
                ulong userId = Convert.ToUInt64(id);
                UserStats stats;
                if (userStats.TryGetValue(userId, out stats))
                {
                    stats.Hits[Skills.Cutting] = 0;
                    stats.Hits[Skills.Mining] = 0;
                    stats.Hits[Skills.Skinning] = 0;

                    BasePlayer targetPlayer = BasePlayer.FindByID(userId);

                    if (targetPlayer != null)
                    {
                        if (targetPlayer.IsConnected)
                        {
                            RenderUI(targetPlayer);
                            SendReply(targetPlayer, "Срок действия услуги «Полная прокачка RPG» истек, продлите услугу.");
                        }
                    }
                }
            }
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (player == null) return;

            switch ((int)dispenser.gatherType)
            {
                case 0:
                    RateHandler(player, item, Skills.Cutting);
                    break;
                case 1:
                    RateHandler(player, item, Skills.Mining);
                    break;
                case 2:
                    RateHandler(player, item, Skills.Skinning);
                    break;
            }
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (userStats.Keys.Contains(quarry.OwnerID))
                item.amount = (int)(item.amount * GetCurrentRate(Skills.Mining, userStats[quarry.OwnerID].Hits[Skills.Mining]));
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            switch (item.info.shortname.ToLower())
            {
                case "wood":
                    RateHandler(player, item, Skills.Cutting);
                    break;
                case "cloth":
                case "mushroom":
                case "corn":
                case "pumpkin":
                case "seed.pumpkin":
                case "seed.corn":
                    RateHandler(player, item, Skills.Skinning);
                    break;
                case "metal.ore":
                case "sulfur.ore":
                case "stones":
                    RateHandler(player, item, Skills.Mining);
                    break;
                default:
                    //Puts("Developer missed this item, which can be picked up: [" + item.info.shortname + "]. Let him know on Oxide forums!");
                    break;
            }
        }

        void RateHandler(BasePlayer player, Item item, Skills skill)
        {
            double oldRate = GetCurrentRate(skill, userStats[player.userID].Hits[skill]);
            double newRate = GetCurrentRate(skill, ++userStats[player.userID].Hits[skill]);

            item.amount = (int)(item.amount * newRate);

            if (oldRate != newRate)
                RenderUI(player);
        }

        private void RenderUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "StatsUI");



            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.009 0.022",
                    AnchorMax = "0.1585 0.136"
                }
            }, "Hud", "StatsUI");

            int fontSize = 12;

            int SkillCount = Enum.GetValues(typeof(Skills)).Length;

            foreach (Skills skill in Enum.GetValues(typeof(Skills))) // rev — from end
            {
                FillElements(elements, skill, mainName, (int)skill + 1, SkillCount, userStats[player.userID], fontSize);
            }

            CuiHelper.AddUi(player, elements);
        }

        private void FillElements(CuiElementContainer elements, Skills skill, string mainPanel, int rowNumber, int maxRows, UserStats stats, int fontSize)
        {
            int percent = GetCurrentPercent(skill, stats.Hits[skill]);

            float value = 1 / (float)maxRows;

            float positionMin = 1 - (value * rowNumber); // xp line size
            float positionMax = 2 - (1 - (value * (1 - rowNumber)));

            var xpBarPlaceholder1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                {
                    new CuiImageComponent { Color = "1 0.95 0.875 0.025" },
                    new CuiRectTransformComponent{ AnchorMin = "0 " + positionMin.ToString("0.####"), AnchorMax = "1 " + positionMax.ToString("0.####") }
                }
            };
            elements.Add(xpBarPlaceholder1);

            var innerXPBar1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = xpBarPlaceholder1.Name,
                Components =
                        {
                            new CuiImageComponent { Color = "0 0 0 0"},
                            new CuiRectTransformComponent{ AnchorMin = "0.02 0.12", AnchorMax = "0.97 0.8" }
                        }
            };
            elements.Add(innerXPBar1);

            if (percent != 0)
            {
                var innerXPBarProgress1 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerXPBar1.Name,
                    Components =
                        {
                            new CuiImageComponent() { Color = skillColors[skill]},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = (percent / 100.0).ToString() + " 1" }
                        }
                };
                elements.Add(innerXPBarProgress1);
            }

            var innerXPBarText1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = innerXPBar1.Name,
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 1", Text = SkillTranslate[skill], FontSize = fontSize, Align = TextAnchor.MiddleCenter},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1" }
                        }
            };
            elements.Add(innerXPBarText1);

            var xpText1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = innerXPBar1.Name,
                Components =
                        {
                            new CuiTextComponent { Text = percent + "%", FontSize = fontSize, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.98 1" }
                        }
            };
            elements.Add(xpText1);

            var lvText1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = innerXPBar1.Name,
                Components =
                        {
                            new CuiTextComponent { Text = "x" + GetCurrentRate(skill, stats.Hits[skill])  + "/x" + MaxRate[skill], FontSize = fontSize, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                            new CuiRectTransformComponent{ AnchorMin = "0.01 0", AnchorMax = "0.5 1" }
                        }
            };
            elements.Add(lvText1);
        }

        double GetCurrentRate(Skills skill, int Hits)
        {
            return Math.Round(1 + (MaxRate[skill] - 1) / MaxHits[skill] * Math.Min(Hits, MaxHits[skill]), 2); // стоит проверить правильность конвертации
        }

        int GetCurrentPercent(Skills skill, int Hits)
        {
            if (Hits == 0) return 0;
            return Convert.ToInt32((float)Math.Min(Hits, MaxHits[skill]) / MaxHits[skill] * 100);
        }

        int GetLeftHits(Skills skill, int Hits)
        {
            return Math.Max(MaxHits[skill] - Hits, -1);
        }
    }
}