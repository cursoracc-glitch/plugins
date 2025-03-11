using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CraftSystem", "Chibubrik", "1.2.0")]
    class CraftSystem : RustPlugin
    {
        #region Вар
        string Layer = "Craft_UI";
        string LayerCraftInfo = "CraftInfo_UI";

        [PluginReference] Plugin ImageLibrary;
        #endregion

        #region Класс
        public class CraftSettings
        {
            [JsonProperty("Включить крафт этого предмета?")] public bool Enable;
            [JsonProperty("Название")] public string Name;
            [JsonProperty("Ссылка на префаб")] public string Prefab;
            [JsonProperty("Короткое название предмета")] public string ShortName;
            [JsonProperty("Описание предмета")] public string Info;
            [JsonProperty("SkinID предмета")] public ulong SkinID;
            [JsonProperty("Кол-во предмета")] public int Amount;
            [JsonProperty("Какой верстак нужен для крафта (Если 0 то не нужен)")] public int LevelWorkBench;
            [JsonProperty("Ссылка на изображение")] public string Url;
            [JsonProperty("Список предметов для крафта")] public Dictionary<string, int> ItemsList;
        }
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Список предметов")] public List<CraftSettings> craftSettings;
            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    craftSettings = new List<CraftSettings>
                    {
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "ВЕРТОЛЁТ",
                            Prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
                            ShortName = "electric.flasherlight",
                            Info = "Описание",
                            SkinID = 1663370375,
                            Amount = 1,
                            LevelWorkBench = 3,
                            Url = "https://imgur.com/Yp2WJl5.png",
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000,
                                ["stones"] = 100,
                                ["metal.ore"] = 1000,
                                ["scrap"] = 100
                            }
                        },
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "Переработчик",
                            Prefab = "assets/bundled/prefabs/static/recycler_static.prefab",
                            ShortName = "research.table",
                            Info = "Вы получите полноценный переработчик, который Вы сможете установить у себя\nдома.",
                            SkinID = 1789555932,
                            Amount = 1,
                            LevelWorkBench = 2,
                            Url = "https://i.imgur.com/xXL3d47.png",
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000,
                                ["scrap"] = 100
                            }
                        },
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "Военная лодка",
                            Prefab = "assets/content/vehicles/boats/rhib/rhib.prefab",
                            ShortName = "electric.sirenlight",
                            Info = "Описание",
                            SkinID = 1789555583,
                            Amount = 1,
                            LevelWorkBench = 2,
                            Url = "https://i.imgur.com/u5QgVGS.png",
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000
                            }
                        },
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "Деревянная лодка",
                            Prefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                            ShortName = "coffin.storage",
                            Info = "Описание",
                            SkinID = 1789554931,
                            Amount = 1,
                            LevelWorkBench = 2,
                            Url = "https://i.imgur.com/UGuYMkA.png",
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000
                            }
                        },
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "Воздушный шар",
                            Prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab",
                            ShortName = "wall.graveyard.fence",
                            Info = "Описание",
                            SkinID = 1789557339,
                            Amount = 1,
                            LevelWorkBench = 3,
                            Url = "https://i.imgur.com/86CDnDd.png",
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000
                            }
                        },
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "Машина",
                            Prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab",
                            ShortName = "woodcross",
                            Info = "Описание",
                            SkinID = 1789556977,
                            Amount = 1,
                            LevelWorkBench = 3,
                            Url = "https://i.imgur.com/KMDl39b.png",
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000
                            }
                        },
                        new CraftSettings
                        {
                            Enable = true,
                            Name = "Гранатомёт",
                            Prefab = null,
                            ShortName = "multiplegrenadelauncher",
                            Info = "Описание",
                            SkinID = 0,
                            Amount = 1,
                            LevelWorkBench = 3,
                            Url = null,
                            ItemsList = new Dictionary<string, int>
                            {
                                ["wood"] = 100,
                                ["sulfur"] = 1000
                            }
                        },
                    }
                };
            }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.craftSettings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
            foreach (var check in config.craftSettings)
            {
                ImageLibrary.Call("AddImage", check.Url, check.Url);
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player,LayerCraftInfo);
            }
        }

        class BasesEntity : MonoBehaviour
        {
            private DestroyOnGroundMissing desGround;
            private GroundWatch groundWatch;
            public ulong OwnerID;

            void Awake()
            {
                OwnerID = GetComponent<BaseEntity>().OwnerID;
                desGround = GetComponent<DestroyOnGroundMissing>();
                if (!desGround) gameObject.AddComponent<DestroyOnGroundMissing>();
                groundWatch = GetComponent<GroundWatch>();
                if (!groundWatch) gameObject.AddComponent<GroundWatch>();
            }
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity();

            foreach (var check in config.craftSettings)
            {
                if (entity != null && entity.skinID == check.SkinID && entity.skinID != 0)
                {
                    BaseEntity all = GameManager.server.CreateEntity(check.Prefab, entity.transform.position, entity.transform.rotation) as BaseEntity;
                    all.Spawn();
                    entity.Kill();
                    all.gameObject.AddComponent<BasesEntity>();
                }
            }
        }
        #endregion

        #region Команды
        [ChatCommand("craft")]
        void cmdCraft(BasePlayer player, string command, string[] args)
        {
            CraftUI(player, 1);
        }

        [ConsoleCommand("craft")]
        void ConsoleCraft(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "item")
                {
                    bool enable = true;
                    var items = config.craftSettings.FirstOrDefault(p => p.ShortName == args.Args[1]);
                    if (player.currentCraftLevel < items.LevelWorkBench)
                    {
                        SendReply(player, $"Нужен верстак {items.LevelWorkBench} уровня");
                        return;
                    }
                    foreach (var check in items.ItemsList)
                    {
                        var haveCount = player.inventory.GetAmount(ItemManager.FindItemDefinition(check.Key).itemid);
                        if (haveCount >= check.Value) continue;
                        enable = false;
                    }
                    if (!enable)
                    {
                        SendReply(player, "У вас не хватает ресурсов для крафта");
                        return;
                    }
                    foreach (var check in items.ItemsList)
                    {
                        player.inventory.Take(null, ItemManager.FindItemDefinition(check.Key).itemid, check.Value);
                    }
                    var skinId = items.SkinID != 0 ? items.SkinID : 0;
                    var item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(items.ShortName).itemid, 1, skinId);
                    item.name = items.Name;
                    if (!player.inventory.GiveItem(item))
                    {
                        item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                        return;
                    }
                    player.SendConsoleCommand("closs");
                    SendReply(player, $"Вы успешно скрафтили: {items.Name}");
                    return;
                }
                if (args.Args[0] == "info")
                {
                    string craft = args.Args[1];
                    CraftInfoUI(player, craft);
                }
                if (args.Args[0] == "skip")
                {
                    int page = 0;
                    if (!args.HasArgs(2) || !int.TryParse(args.Args[1], out page)) return;
                    CraftUI(player, page);
                }
            }
        }

        [ConsoleCommand("closs")]
        void ConsoleCloss(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, LayerCraftInfo);
        }
        #endregion

        #region Интерфейс
        private void CraftUI(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            int CraftCount = config.craftSettings.Count(), CountCraft = 0, Count = 4;
            float Position = 0.2f, Width = (0.146f), Height = 0.3f, Margin = 0.005f, MinHeight = 0.503f;

            if (CraftCount >= Count) Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
            else Position = 0.5f - CraftCount / 2f * Width - (CraftCount - 1) / 2f * Margin;
            CraftCount -= Count;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-2 -2", AnchorMax = "2 2", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.9", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"<b><size=30>Система крафта предметов</size></b>\nСтраница: {page} из {(config.craftSettings.Count() / 9) + 1}".ToUpper(), Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
            }, Layer);

            #region Скип
            if (page > 1)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.45", AnchorMax = "0.057 0.55", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"craft skip {page - 1}" },
                    Text = { Text = $"<", Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 60 }
                }, Layer);
            }

            if (page < (int)Math.Ceiling((double)config.craftSettings.ToList().Count / 8))
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.943 0.45", AnchorMax = "1 0.55", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"craft skip {page + 1}" },
                    Text = { Text = $">", Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 60 }
                }, Layer);
            }
            #endregion

            foreach (var check in config.craftSettings.Skip((page - 1) * 8).Take(8))
            {
                if (check.Enable)
                {
                    container.Add(new CuiButton()
                    {
                        RectTransform = { AnchorMin = $"{Position} {MinHeight}", AnchorMax = $"{Position + Width} {MinHeight + Height}", OffsetMax = "0 0" },
                        Button = { Color = "1 1 1 0.1" },
                        Text = { Text = $" " }
                    }, Layer, $"{check.ShortName}");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.1 0.21", AnchorMax = "0.9 0.84", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0" },
                        Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                    }, $"{check.ShortName}", "Image");

                    var images = check.Url != null ? $"{check.Url}" : $"{check.ShortName}";
                    container.Add(new CuiElement
                    {
                        Parent = "Image",
                        Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", images) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1 1", OffsetMax = "-1 -1" }
                            }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.028 0.03", AnchorMax = "0.972 0.2", OffsetMax = "0 0" },
                        Button = { Color = "0.39 0.73 0.49 0.9", Material = "assets/content/ui/uibackgroundblur.mat", Command = $"craft info {check.ShortName}", Close = Layer },
                        Text = { Text = "ОТКРЫТЬ", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.ShortName}");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0.85", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "1 1 1 0" },
                        Text = { Text = $"{check.Name}", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.ShortName}");

                    CountCraft += 1;
                    if (CountCraft % Count == 0)
                    {
                        if (CraftCount > Count)
                        {
                            Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
                            CraftCount -= Count;
                        }
                        else
                        {
                            Position = 0.5f - CraftCount / 2f * Width - (CraftCount - 1) / 2f * Margin;
                        }
                        MinHeight -= ((Margin * 2) + Height);
                    }
                    else
                    {
                        Position += (Width + Margin);
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #region Предмет
        private void CraftInfoUI(BasePlayer player, string craft)
        {
            CuiHelper.DestroyUi(player, LayerCraftInfo);
            CuiElementContainer container = new CuiElementContainer();
            CraftSettings check = config.craftSettings.Find(p => p.ShortName == craft);
            int ItemCount = check.ItemsList.Count(), Count = 6;
            float Position = 0.2f, Width = 0.08f, Height = 0.14f, Margin = 0.005f, MinHeight = 0.26f;
            Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
            int current = 1;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" },
            }, "Overlay", LayerCraftInfo);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-2 -2", AnchorMax = "2 2", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.9", Command = "chat.say /craft", Close = LayerCraftInfo },
                Text = { Text = "" }
            }, LayerCraftInfo);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.43 0.65", AnchorMax = "0.57 0.895", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "" }
            }, LayerCraftInfo, "Image");

            var images = check.Url != null ? $"{check.Url}" : $"{check.ShortName}";
            container.Add(new CuiElement
            {
                Parent = "Image",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", images) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.59", AnchorMax = "1 0.63", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"{check.Name}", Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-bold.ttf" }
            }, LayerCraftInfo);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.465", AnchorMax = "1 0.585", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"{check.Info}", Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.UpperCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" }
            }, LayerCraftInfo);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.425", AnchorMax = "1 0.46", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"Предметы необходимые для крафта", Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, FontSize = 22, Font = "robotocondensed-bold.ttf" }
            }, LayerCraftInfo);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4 0.13", AnchorMax = "0.6 0.18", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Command = $"craft item {check.ShortName}" },
                Text = { Text = "СКРАФТИТЬ", Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, FontSize = 22, Font = "robotocondensed-bold.ttf" }
            }, LayerCraftInfo);

            var textWork = 0 < check.LevelWorkBench ? $"Нужен верстак {check.LevelWorkBench} уровня" : "Верстак не нужен";
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.03", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = textWork, Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, LayerCraftInfo);

            foreach (var item in check.ItemsList)
            {
                int haveCount = player.inventory.GetAmount(ItemManager.FindItemDefinition(item.Key).itemid);
                var colors = haveCount >= item.Value ? "0.39 0.73 0.49 0.3" : "0.76 0.24 0.24 0.3";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{Position} {MinHeight}", AnchorMax = $"{Position + Width} {MinHeight + Height}", OffsetMax = "0 0" },
                    Button = { Color = colors, Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { Text = $"", Align = TextAnchor.LowerCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" }
                }, LayerCraftInfo, "Images");

                container.Add(new CuiElement
                {
                    Parent = "Images",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" }
                    }
                });

                var text = haveCount >= item.Value ? "СОБРАНО " : $"X{item.Value} ";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = text, Color = HexToUiColor("#FFFFFF5A"), Align = TextAnchor.LowerRight, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Images");
                Position += (Width + Margin);
                current++;
                if (current > 6)
                {
                    break;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Helpers
        private static string HexToUiColor(string hex)
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
        #endregion
    }
}