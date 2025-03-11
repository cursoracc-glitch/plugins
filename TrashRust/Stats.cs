using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Plugins;
using Oxide.Core.SQLite.Libraries;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Stats", "", "1.0.0")]
    [Description("Player's stats")]
    public class Stats : RustPlugin
    {
        public static Stats instance;
        [PluginReference] Plugin XMenu;

        public static SQLite SQLite = Interface.Oxide.GetLibrary<SQLite>();
        public static Connection SQLiteConnection;
        public static string DataBase = "Stats.db";

        #region Config
        private PluginConfig config;
        private class PluginConfig
        {
            public ColorConfig colorConfig;
            public class ColorConfig
            {
                public string menuContentHighlighting;
                public string menuContentHighlightingalternative;

                public string menuContentText;
                public string menuContentTextAlternative;

                public string gradientColor;
            }
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                colorConfig = new PluginConfig.ColorConfig()
                {
                    menuContentHighlighting = "#0000007f",
                    menuContentHighlightingalternative = "#FFFFFF10",

                    menuContentTextAlternative = "#90BD47",
                    menuContentText = "#FFFFFFAA",

                    gradientColor = "#00000099",
                },
            };
        }
        #endregion

        #region U'Mod Hook's
        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            instance = this;
            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "Stats", "assets/icons/market.png", "RenderStats", null);

                    cmd.AddChatCommand("stats", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true Stats"));
                    TimerInitialize.Destroy();
                }
            });

            #region Initialize
            try
            {
                SQLiteConnection = SQLite.OpenDb(DataBase, this);
                if (SQLiteConnection == null)
                {
                    PrintWarning($"Couldn't open DataBase");
                }
                else
                {
                    SQLite.Insert(Core.Database.Sql.Builder.Append("CREATE TABLE IF NOT EXISTS Stats (" +
                              "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                              "steamid BIGINT(17) UNIQUE, " +
                              "name VARCHAR(256), " +
                              "kills INTEGER, " +
                              "death INTEGER, " +
                              "suicides INTEGER, " +
                              "killanimal INTEGER, " +
                              "killnpc INTEGER, " +
                              "killhelicopter INTEGER, " +
                              "killbradley INTEGER, " +
                              "wood INTEGER, " +
                              "stones INTEGER, " +
                              "metalore INTEGER, " +
                              "sulfurore INTEGER, " +
                              "hqmetalore INTEGER, " +
                              "resources INTEGER);"), SQLiteConnection);
                }

                timer.Once(3f, () => { foreach (var p in BasePlayer.activePlayerList) { OnPlayerConnected(p); } });
            }
            catch (Exception e)
            {
                PrintWarning(e.Message);
            }
            #endregion
        }

        void Unload()
        {
            SQLite.CloseDb(SQLiteConnection);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            InsertDataBase(player);
        }

        #region Insert/Update
        void InsertDataBase(BasePlayer player)
        {
            try
            {
                string displayName = player.displayName.Replace("\'", "").Replace("\"", "").Replace("@", "");
                SQLite.Insert(Core.Database.Sql.Builder.Append($"INSERT OR IGNORE into Stats ( steamid, name, kills, death, suicides, killanimal, killnpc, killhelicopter, killbradley, wood, stones, metalore, sulfurore, hqmetalore, resources ) values ( {player.userID}, '{Name}', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 );"), SQLiteConnection);
                SQLite.Insert(Core.Database.Sql.Builder.Append($@"UPDATE Stats SET name = '{displayName}' WHERE steamid={player.userID};"), SQLiteConnection);
            }
            catch (Exception e)
            {
                PrintWarning(e.Message);
            }
        }

        void UpdateDataBase(BasePlayer player, string Name, int Value)
        {
            try
            {
                string ValueName = Name.Replace(".", "").Replace("@", "");
                SQLite.Insert(Core.Database.Sql.Builder.Append($"UPDATE Stats SET {ValueName} = {ValueName} + {Value} WHERE steamid={player.userID};"), SQLiteConnection);
            }
            catch (Exception e)
            {
                PrintWarning(e.Message);
            }
        }
        #endregion

        #region OnEntityDeath
        private Dictionary<ulong, BasePlayer> lastHelicopterAttack = new Dictionary<ulong, BasePlayer>();
        private Dictionary<ulong, BasePlayer> lastBradleyAttack = new Dictionary<ulong, BasePlayer>();
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.InitiatorPlayer != null)
                lastHelicopterAttack[entity.net.ID.Value] = info.InitiatorPlayer;

            if (entity is BradleyAPC && info.InitiatorPlayer != null)
                lastBradleyAttack[entity.net.ID.Value] = info.InitiatorPlayer;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null)
                return;

            try
            {
                if (entity is BaseAnimalNPC)
                {
                    if (info.InitiatorPlayer != null)
                    {
                        UpdateDataBase(info.InitiatorPlayer, "killanimal", 1);
                    }
                    return;
                }
                if (entity is NPCPlayer)
                {
                    if (info != null && info.InitiatorPlayer != null)
                    {
                        UpdateDataBase(info.InitiatorPlayer, "killnpc", 1);
                    }
                    return;
                }
                if (entity is BasePlayer)
                {
                    if (info.InitiatorPlayer != null && !(info.InitiatorPlayer is NPCPlayer))
                    {
                        if ((entity as BasePlayer).userID == info.InitiatorPlayer.userID)
                        {
                            UpdateDataBase(info.InitiatorPlayer, "suicides", 1);
                        }
                        else
                        {
                            UpdateDataBase(info.InitiatorPlayer, "kills", 1);
                            UpdateDataBase((entity as BasePlayer), "death", 1);
                        }
                    }
                    else
                    {
                        UpdateDataBase((entity as BasePlayer), "death", 1);
                    }
                    return;
                }

                if (entity is BaseHelicopter || entity is CH47Helicopter)
                {
                    if (info.InitiatorPlayer != null)
                    {
                        UpdateDataBase(info.InitiatorPlayer, "killhelicopter", 1);
                    }
                    else
                    {
                        if (lastHelicopterAttack.ContainsKey(entity.net.ID.Value))
                        {
                            UpdateDataBase(lastHelicopterAttack[entity.net.ID.Value], "killhelicopter", 1);
                        }
                    }
                    return;
                }

                if (entity is BradleyAPC)
                {
                    if (info.InitiatorPlayer != null)
                    {
                        UpdateDataBase(info.InitiatorPlayer, "killbradley", 1);
                    }
                    else
                    {
                        if (lastBradleyAttack.ContainsKey(entity.net.ID.Value))
                        {
                            UpdateDataBase(lastHelicopterAttack[entity.net.ID.Value], "killbradley", 1);
                        }
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                PrintWarning(entity.PrefabName + "\n" + (info != null ? "info != null" : "info == null"));
                PrintWarning(entity.PrefabName + "\n" + (info.InitiatorPlayer != null ? $"{info.InitiatorPlayer.displayName} Initiator player != null" : "initiator player == null"));
                PrintWarning(entity.PrefabName + "\n" + ex.ToString());
            }
        }
        #endregion

        #region Resources
        void OnPlayerGather(BasePlayer player, Item item)
        {
            if (player == null) return;

            switch (item.info.shortname)
            {
                case "wood": UpdateDataBase(player, "wood", item.amount); break;
                case "stones": UpdateDataBase(player, "stones", item.amount); break;
                case "sulfur.ore": UpdateDataBase(player, "sulfurore", item.amount); break;
                case "metal.ore": UpdateDataBase(player, "metalore", item.amount); break;
                case "hq.metal.ore": UpdateDataBase(player, "hqmetalore", item.amount); break;
            }
            UpdateDataBase(player, "resources", item.amount);
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnPlayerGather(entity?.ToPlayer(), item);

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) => OnPlayerGather(player, item);

        void OnGrowableGather(GrowableEntity plant, Item item, BasePlayer player) => OnPlayerGather(player, item);

        void OnCollectiblePickup(Item item, BasePlayer player) => OnPlayerGather(player, item);
        #endregion
        #endregion

        #region UI
        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion

        private void RenderStats(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];
            string StatName = string.IsNullOrEmpty((string)objects[5]) ? "Kills" : (string)objects[5];

            BasePlayer player = BasePlayer.FindByID(userID);
            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-430 -230",
                            OffsetMax = "490 270"
                        },
                    }
            });

            #region Table
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Title",
                Parent = MenuContent,
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"<color={config.colorConfig.menuContentText}><b>СТАТИСТИКА ЛУЧШИХ ИГРОКОВ СЕРВЕРА TRASH <color=#1d71ff>RUST</color> X20</b></color>",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 32,
                            Font = "robotocondensed-bold.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"0 -50",
                            OffsetMax = $"920 0",
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.Name",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"77.5 -85",
                                    OffsetMax = $"190 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.NameTitle",
                Parent = MenuContent + $".Content.Stats.Name",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Никнейм</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.Kills",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"193 -85",
                                    OffsetMax = $"263 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.KillsTitle",
                Parent = MenuContent + $".Content.Stats.Kills",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Убийств</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.Deaths",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"266 -85",
                                    OffsetMax = $"336 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.DeathsTitle",
                Parent = MenuContent + $".Content.Stats.Deaths",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Смертей</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.KillNPC",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"339 -85",
                                    OffsetMax = $"409 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.KillNPCTitle",
                Parent = MenuContent + $".Content.Stats.KillNPC",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Ученых</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.KillAnimals",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"412 -85",
                                    OffsetMax = $"482 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.KillAnimalsTitle",
                Parent = MenuContent + $".Content.Stats.KillAnimals",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Животных</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.MetalOre",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"485 -85",
                                    OffsetMax = $"555 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.MetalOreTitle",
                Parent = MenuContent + $".Content.Stats.MetalOre",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Металл</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.SulfurOre",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"558 -85",
                                    OffsetMax = $"628 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.SulfurOreTitle",
                Parent = MenuContent + $".Content.Stats.SulfurOre",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Сера</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.HQMetal",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"631 -85",
                                    OffsetMax = $"701 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.HQMetalTitle",
                Parent = MenuContent + $".Content.Stats.HQMetal",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>МВК</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.Res",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"704 -85",
                                    OffsetMax = $"822.5 -60"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Content.Stats.ResTitle",
                Parent = MenuContent + $".Content.Stats.Res",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>Всего ресурсов</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            #endregion

            #region Buttons
            int y = 0;
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"77.5 {-85 - y * 35}",
                                        OffsetMax = $"190 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.Name");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 kills" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"193 {-85 - y * 35}",
                                        OffsetMax = $"263 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.Kills");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 death" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"266 {-85 - y * 35}",
                                        OffsetMax = $"336 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.Deaths");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 killnpc" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"339 {-85 - y * 35}",
                                        OffsetMax = $"409 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.KillNPC");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 killanimal" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"412 {-85 - y * 35}",
                                        OffsetMax = $"482 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.KillAnimals");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 metalore" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"485 {-85 - y * 35}",
                                        OffsetMax = $"555 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.MetalOre");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 sulfurore" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"558 {-85 - y * 35}",
                                        OffsetMax = $"628 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.SulfurOre");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 hqmetalore" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"631 {-85 - y * 35}",
                                        OffsetMax = $"701 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.HQMetal");
            Container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"custommenu false Stats 0 0 resources" },
                RectTransform = {   AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"704 {-85 - y * 35}",
                                        OffsetMax = $"822.5 {-60 - y * 35}" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, MenuContent, MenuContent + $".Content.Stats.Btn.Res");
            #endregion

            SelectDataBase(player, StatName, 12, 0);
            SelectPlayer(player);
        }

        public void SelectDataBase(BasePlayer player, string Name, int count, int offset)
        {
            try
            {
                CuiElementContainer Container = new CuiElementContainer();
                string ValueName = Name.Replace(".", "");
                var SQLString = Core.Database.Sql.Builder.Append($"SELECT * FROM Stats ORDER BY {ValueName} DESC LIMIT {count} OFFSET {offset};");
                SQLite.Query(SQLString, SQLiteConnection, obj =>
                {
                    if (obj != null)
                    {
                        for (int i = 0, y = 0; i < obj.Count; i++, y++)
                        {
                            string color = HexToRustFormat(config.colorConfig.menuContentHighlighting);
                            if (instance.IsEven(y)) color = HexToRustFormat(config.colorConfig.menuContentHighlightingalternative);
                            Container.Add(new CuiElement
                            {
                                Name = MenuLayer + $".Content.Stats.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = color,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"77.5 {-120 - y * 30}",
                                        OffsetMax = $"822.5 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.Name.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["name"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 10,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"77.5 {-120 - y * 30}",
                                        OffsetMax = $"190 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.Kills.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["kills"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"193 {-120 - y * 30}",
                                        OffsetMax = $"263 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.Deaths.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["death"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"266 {-120 - y * 30}",
                                        OffsetMax = $"336 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.KillNPC.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["killnpc"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"339 {-120 - y * 30}",
                                        OffsetMax = $"409 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.KillAnimals.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["killanimal"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"412 {-120 - y * 30}",
                                        OffsetMax = $"482 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.MetalOre.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["metalore"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"485 {-120 - y * 30}",
                                        OffsetMax = $"555 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.SulfurOre.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["sulfurore"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"558 {-120 - y * 30}",
                                        OffsetMax = $"628 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.HQMeta;.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["hqmetalore"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"631 {-120 - y * 30}",
                                        OffsetMax = $"701 {-95 - y * 30}"
                                    }
                                }
                            });
                            Container.Add(new CuiElement
                            {
                                Name = MenuContent + $".Content.Stats.Res.{i}",
                                Parent = MenuContent,
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentText}>{obj.ElementAt(i)["resources"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"704 {-120 - y * 30}",
                                        OffsetMax = $"822.5 {-95 - y * 30}"
                                    }
                                }
                            });
                        }
                        CuiHelper.AddUi(player, Container);
                    }
                });
            }
            catch (Exception e)
            {
                instance.PrintWarning(e.Message);
            }
        }

        public void SelectPlayer(BasePlayer player)
        {
            try
            {
                CuiElementContainer Container = new CuiElementContainer();
                string ValueName = instance.Name.Replace(".", "");
                var SQLString = Core.Database.Sql.Builder.Append($"SELECT * FROM Stats WHERE steamid='{player.userID}';");
                SQLite.Query(SQLString, SQLiteConnection, obj =>
                {
                    if (obj == null) return;
                    for (int i = 0, y = 12; i < obj.Count; i++, y++)
                    {
                        string color = HexToRustFormat(config.colorConfig.menuContentHighlighting);
                        if (instance.IsEven(y)) color = HexToRustFormat(config.colorConfig.menuContentHighlightingalternative);
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = color,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"77.5 {-120 - y * 30}",
                                        OffsetMax = $"822.5 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.Name.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>Ваша статистика:</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"77.5 {-120 - y * 30}",
                                        OffsetMax = $"190 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.Kills.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["kills"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"193 {-120 - y * 30}",
                                        OffsetMax = $"263 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.Deaths.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["death"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"266 {-120 - y * 30}",
                                        OffsetMax = $"336 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.KillNPC.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["killnpc"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"339 {-120 - y * 30}",
                                        OffsetMax = $"409 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.KillAnimals.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["killanimal"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"412 {-120 - y * 30}",
                                        OffsetMax = $"482 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.MetalOre.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["metalore"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"485 {-120 - y * 30}",
                                        OffsetMax = $"555 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.SulfurOre.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["sulfurore"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"558 {-120 - y * 30}",
                                        OffsetMax = $"628 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.HQMeta;.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["hqmetalore"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"631 {-120 - y * 30}",
                                        OffsetMax = $"701 {-95 - y * 30}"
                                    }
                                }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Content.Stats.Res.{i}",
                            Parent = MenuContent,
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color={config.colorConfig.menuContentTextAlternative}>{obj.ElementAt(i)["resources"]}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"704 {-120 - y * 30}",
                                        OffsetMax = $"822.5 {-95 - y * 30}"
                                    }
                                }
                        });
                    }
                    CuiHelper.AddUi(player, Container);
                });
            }
            catch (Exception e)
            {
                instance.PrintWarning(e.Message);
            }
        }
        #endregion

        #region Helpers
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private bool IsEven(int a)
        {
            return (a % 2) == 0;
        }
        #endregion
    }
}