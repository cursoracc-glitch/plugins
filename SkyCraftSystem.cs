using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SkyCraftSystem", "fhff444", "1.0.{DarkPluginsID}")]
    class SkyCraftSystem : RustPlugin
    {
        #region Reference
        Plugin ImageLibrary => Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();

        private class Configuration
        {
            [JsonProperty("Настройка системы крафта | SettingsPluign")]
            public List<CraftItemsConfiguration> SettingsPlugins = new List<CraftItemsConfiguration>();
            [JsonProperty("Настройка интерфейса | Interface Settings")]
            public CraftInterface SettingsInterface = new CraftInterface();

            internal class CraftItemsConfiguration
            {
                #region ItemSettings
                [JsonProperty("Включить крафт этого предмета | On-Off craft this item")]
                public bool OnOffCraft;
                [JsonProperty("Права для крафта | Permissions for craft")]
                public string Permissions;
                [JsonProperty("Ссылка на префаб | Prefab [ЕСЛИ ВЫ НЕ ЗНАЕТЕ ЧТО ЭТО - НЕ ТРОГАЙТЕ.ПРОСТО ИСПОЛЬЗУЙТЕ BOOL(TRUE / FALSE)]")]
                public string Prefab;
                [JsonProperty("Shortname заменяемого предмета | Shortname reply items for prefabs")]
                public string ShortnameReplyPrefab;
                [JsonProperty("Название предмета | DisplayName items")]
                public string DisplayName;
                [JsonProperty("Уровень верстака для крафта | WorkBench Level for Craft")]
                public int WorkBenchLevel;
                [JsonProperty("SkinID предмету(С фоткой)")]
                public ulong SkinID;
                [JsonProperty("Ссылка на картинку(512x512) | LINKPNG.png(512x512)")]
                public string PNGLink;
                [JsonProperty("Предметы для крафта | Crafting Items")]
                public Dictionary<string, int> CraftingItems = new Dictionary<string, int>();
                #endregion
            }

            internal class CraftInterface
            {
                #region Interface

                [JsonProperty("[Главная/Main]Цвет заднего фона | Background color")]
                public string BackgrounColorMain;
                [JsonProperty("[Главная/Main]Материал заднего фона | Metrial Background")]
                public string BackgrounMaterialMain;
                [JsonProperty("[Главная/Main]Спрайт заднего фона | Sprite Background")]
                public string BackgrounSpriteMain;

                [JsonProperty("[Панель предлметов/Panel Items]Цвет заднего фона | Background color")]
                public string PItemsBackgroundColor;
                [JsonProperty("[Доп.Панель предлметов/Additional Panel Items]Цвет заднего фона | Background color")]
                public string AdditionalPItemsBackgroundColor;

                [JsonProperty("[Цвет верстака/WorkBenchColor]Цвет верстака 0 уровня | WorkBench level - 0")]
                public string WorkBenchLevelColorNull;
                [JsonProperty("[Цвет верстака/WorkBenchColor]Цвет верстака 1 уровня | WorkBench level - 1")]
                public string WorkBenchLevelColorOne;
                [JsonProperty("[Цвет верстака/WorkBenchColor]Цвет верстака 2 уровня | WorkBench level - 2")]
                public string WorkBenchLevelColorTwo;
                [JsonProperty("[Цвет верстака/WorkBenchColor]Цвет верстака 3 уровня | WorkBench level - 3")]
                public string WorkBenchLevelColorThree;

                [JsonProperty("Цвет кнопки | Color Button")]
                public string ColorButtonCreate;
                [JsonProperty("Цвет панели с ресурсом,если конкретный ресурс собран | The color of the resource panel, if a specific resource is compiled")]
                public string ColorPanelResourceComplete;
                [JsonProperty("Цвет панели с ресурсом,если конкретный ресурс не собран | Color of the panel with the resource, if a specific resource is not assembled")]
                public string ColorPanelResourceNoComplete;

                #endregion
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    SettingsPlugins = new List<CraftItemsConfiguration>
                    {
                        #region ItemConfig
                        new CraftItemsConfiguration
                        {
                            OnOffCraft = true,
                            Permissions = "SkyCraftSystem.minicopter",
                            Prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
                            ShortnameReplyPrefab = "electric.flasherlight",
                            DisplayName = "<size=20>Миникоптер</size>",
                            WorkBenchLevel = 2,
                            SkinID = 1764551112,
                            PNGLink = "https://i.imgur.com/SIRebqz.png",
                            CraftingItems = new Dictionary<string, int>
                            {
                                ["wood"] = 1500,
                                ["stones"] = 2300,
                            }
                        },
                        new CraftItemsConfiguration
                        {
                            OnOffCraft = true,
                            Permissions = "SkyCraftSystem.recycler",
                            Prefab = "assets/bundled/prefabs/static/recycler_static.prefab",
                            ShortnameReplyPrefab = "research.table",
                            DisplayName = "<size=18>Домашний переработчик</size>",
                            WorkBenchLevel = 1,
                            SkinID = 1764552507,
                            PNGLink = "https://i.imgur.com/xXL3d47.png",
                            CraftingItems = new Dictionary<string, int>
                            {
                                ["wood"] = 1000,
                                ["stones"] = 5000,
                            }
                        },
                        new CraftItemsConfiguration
                        {
                            OnOffCraft = true,
                            Permissions = "SkyCraftSystem.rowboat",
                            Prefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                            ShortnameReplyPrefab = "electric.hbhfsensor",
                            DisplayName = "<size=18>Деревянная лодка</size>",
                            WorkBenchLevel = 2,
                            SkinID = 1764552960,
                            PNGLink = "https://i.imgur.com/UGuYMkA.png",
                            CraftingItems = new Dictionary<string, int>
                            {
                                ["wood"] = 20000,
                            }
                        },
                        new CraftItemsConfiguration
                        {
                            OnOffCraft = true,
                            Permissions = "SkyCraftSystem.rhibboat",
                            Prefab = "assets/content/vehicles/boats/rhib/rhib.prefab",
                            ShortnameReplyPrefab = "electric.sirenlight",
                            DisplayName = "<size=19>Военная лодка</size>",
                            WorkBenchLevel = 3,
                            SkinID = 1764553295,
                            PNGLink = "https://i.imgur.com/u5QgVGS.png",
                            CraftingItems = new Dictionary<string, int>
                            {
                                ["wood"] = 1500,
                                ["stones"] = 50000,
                            }
                        },
                        new CraftItemsConfiguration
                        {
                            OnOffCraft = true,
                            Permissions = "SkyCraftSystem.sedan",
                            Prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab",
                            ShortnameReplyPrefab = "woodcross",
                            DisplayName = "<size=20>Машина</size>",
                            WorkBenchLevel = 2,
                            SkinID = 1764553578,
                            PNGLink = "https://i.imgur.com/KMDl39b.png",
                            CraftingItems = new Dictionary<string, int>
                            {
                                ["wood"] = 1500,
                                ["stones"] = 2300,
                            }
                        },
                        new CraftItemsConfiguration
                        {
                            OnOffCraft = true,
                            Permissions = "SkyCraftSystem.hotairball",
                            Prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab",
                            ShortnameReplyPrefab = "wall.graveyard.fence",
                            DisplayName = "<size=19>Воздушный шар</size>",
                            WorkBenchLevel = 3,
                            SkinID = 1764553961,
                            PNGLink = "https://i.imgur.com/86CDnDd.png",
                            CraftingItems = new Dictionary<string, int>
                            {
                                ["wood"] = 2281337,
                                ["stones"] = 2300,
                            }
                        },                      

                        #endregion
                    },
                    SettingsInterface = new CraftInterface
                    {
                        #region Interface
                        BackgrounColorMain = "#2D2D2D88",
                        BackgrounMaterialMain = "assets/content/ui/uibackgroundblur.mat",
                        BackgrounSpriteMain = "assets/content/ui/ui.background.transparent.radial.psd",

                        PItemsBackgroundColor = "#76767678",
                        AdditionalPItemsBackgroundColor ="#FFFFFF13",

                        WorkBenchLevelColorNull = "#0071FF53",
                        WorkBenchLevelColorOne = "#16EB8578",
                        WorkBenchLevelColorTwo = "#FF73008C",
                        WorkBenchLevelColorThree = "#EB173678",

                        ColorButtonCreate = "#2ABD86FF",
                        ColorPanelResourceComplete = "#A60D0D2F",
                        ColorPanelResourceNoComplete = "#1FB91931",
                        #endregion
                    },
                };
            }          
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks
        void OnServerInitialized()
        {
            RegisteredPlugin();

            PrintError($"-----------------------------------");
            PrintError($"           SkyCraftSystem          ");
            PrintError($"          Created - Sky Eye        ");
            PrintError($"        Author = Mercury#5212      ");
            PrintError($"    https://vk.com/skyeyeplugins   ");
            PrintError($"-----------------------------------");
        }

        private void OnEntityBuilt(Planner plan, GameObject go) => CheckDeploy(go.ToBaseEntity());

        private Item OnItemSplit(Item item, int amount)
        {
            for (int i = 0; i < config.SettingsPlugins.Count; i++)
            {
                var cfg = config.SettingsPlugins[i];
                if (item.skin == cfg.SkinID)
                {
                    Item x = ItemManager.CreateByPartialName(cfg.ShortnameReplyPrefab, amount);
                    x.name = cfg.DisplayName;
                    x.skin = cfg.SkinID;
                    x.amount = amount;
                    item.amount -= amount;
                    return x;
                }
            }
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin) return false;

            return null;
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin) return false;

            return null;
        }

        #endregion

        #region Command

        [ConsoleCommand("craftssystem")]
        void ConsoleCraftSystem(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            switch(arg.Args[0])
            {
                case "craft_select":
                    {
                        int index = Convert.ToInt32(arg.Args[1]);
                        UI_CraftMenuAction(player, index);
                        break;
                    }
                case "craft_item":
                    {
                        int index = Convert.ToInt32(arg.Args[1]);
                        var cfg = config.SettingsPlugins[index];

                        foreach (var info in cfg.CraftingItems)                        
                            if (CheckResourceCraft(player, info.Key, info.Value))
                            {
                                MessageUI(player, lang.GetMessage("INTERFACEACTION_NO_RESOURCE_BTN", this));
                                Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetlost.prefab", player.transform.localPosition);
                                CuiHelper.DestroyUi(player, PARENT_MAINMENU);
                                return;
                            }
                        if(player.currentCraftLevel < cfg.WorkBenchLevel)
                        {
                            MessageUI(player, lang.GetMessage("INTERFACEACTION_NO_WORKBENCH_BTN", this));
                            Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetlost.prefab", player.transform.localPosition);
                            CuiHelper.DestroyUi(player, PARENT_MAINMENU);
                            return;
                        }

                        TakeItemForCraft(player, index);
                        CuiHelper.DestroyUi(player, PARENT_MAINMENU);
                        Effect.server.Run("assets/prefabs/deployable/tier 1 workbench/effects/experiment-start.prefab", player.transform.localPosition);
                        GiveItems(player,index);
                        break;
                    }              
            }
        }

        [ChatCommand("craft")]
        void OpenCraftMenuChat(BasePlayer player)
        {
            UI_MainMenu(player);
        }

        [ConsoleCommand("craft")]
        void OpenCraftMenuConsole(ConsoleSystem.Arg arg)
        {
            UI_MainMenu(arg.Player());
        }
        #endregion

        #region Metods

        void RegisteredPlugin()
        {
            for(int i = 0; i < config.SettingsPlugins.Count; i++)
            {
                var cfg = config.SettingsPlugins[i];
                permission.RegisterPermission($"{cfg.Permissions}".ToLower(), this);
                AddImage(cfg.PNGLink, cfg.DisplayName);
            }
        }

        #region CraftItemsMetods

        private void TakeItemForCraft(BasePlayer player,int index)
        {
            var ICFG = config.SettingsPlugins[index].CraftingItems;

            foreach (var item in ICFG)
                player.inventory.Take(null, ItemManager.FindItemDefinition(item.Key).itemid, item.Value);
        }

        private void GiveItems(BasePlayer player,int index)
        {
            var item = CreateItem(index);
            player.GiveItem(item);
        }

        private Item CreateItem(int index)
        {
            var cfg = config.SettingsPlugins[index];

            var item = ItemManager.CreateByName(cfg.ShortnameReplyPrefab, 1, cfg.SkinID);
            if (item == null)
                return null;

            item.name = cfg.DisplayName;
            return item;
        }

        private void CheckDeploy(BaseEntity entity)
        {
            if (entity == null) return;
            if (!ItemCheck(entity.skinID)) return;
            SpawnItem(entity.transform.position, entity.skinID, entity.transform.rotation, entity.OwnerID);
            entity.Kill();
        }

        private bool ItemCheck(ulong skin)
        {
            for (int i = 0; i < config.SettingsPlugins.Count; i++)
            {
                var cfg = config.SettingsPlugins[i];
                if (skin != 0 && skin == cfg.SkinID)
                    return true;
            }
            return false;
        }

        #endregion

        #region SpawnPrefabs

        private void SpawnItem(Vector3 position,ulong SkinID, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            for (int i = 0; i < config.SettingsPlugins.Count; i++)
            {
                var cfg = config.SettingsPlugins[i];

                if (cfg.SkinID == SkinID)
                {
                    BaseEntity Item = GameManager.server.CreateEntity(cfg.Prefab, position, rotation);
                    if (Item == null) { return; }
                    Item.Spawn();
                    break;
                }
            }
        }

        #endregion

        #region BoolMetods

        public bool PermissionCheck(string ID, string permissions)
        {
            if (permission.UserHasPermission(ID, permissions))
                return true;
            else return false;
        }

        private bool CheckResourceCraft(BasePlayer player, string Key, int Value)
        {
            var more = new Dictionary<string, int>();
            var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(Key).itemid);
            if (has < Value)
            {
                if (!more.ContainsKey(Key))
                    more.Add(Key, 0);

                more[Key] += Value - has;
            }

            if (more.ContainsKey(Key))
                return true;
            else
                return false;
        }

        #endregion

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_TEXT"] = "<size=30>The system of creating useful items</size>",
                ["TITLE_DESC"] = "<size=16>Select an available item and go to craft</size>",

                ["INTERFACE_BTN"] = "<size=20>Select</size>",
                ["INTERFACE_WORKBENCHLEVEL"] = "<size=14>WorkBench-level {0}</size>",
                ["INTERFACE_WORKBENCHLEVELNULL"] = "<size=14>No workbench</size>",

                ["INTERFACEACTION_TITLE"] = "<size=30>Item Creation Menu : {0}</size>",
                ["INTERFACEACTION_BTN"] = "<size=20>Create</size>",
                ["INTERFACEACTION_NO_WORKBENCH_BTN"] = "<size=16>Go to the workbench</size>",
                ["INTERFACEACTION_NO_RESOURCE_BTN"] = "<size=16>Not enough resources</size>",

                ["INTERFACEACTION_COLLECTED_RESORUCE"] = "<size=18>Collected</size>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_TEXT"] = "<size=30>Система создания полезных предметов</size>",
                ["TITLE_DESC"] = "<size=16>Bыберите доступный предмет и переходите к крафту</size>",

                ["INTERFACE_BTN"] = "<size=20>Выбрать</size>",
                ["INTERFACE_WORKBENCHLEVEL"] = "<size=14>Верстак {0} уровня</size>",
                ["INTERFACE_WORKBENCHLEVELNULL"] = "<size=14>Верстак не требуется</size>",

                ["INTERFACEACTION_TITLE"] = "<size=30>Mеню создания предмета : {0}</size>",
                ["INTERFACEACTION_BTN"] = "<size=20>Создать</size>",
                ["INTERFACEACTION_NO_WORKBENCH_BTN"] = "<size=16>Подойдите к верстаку</size>",
                ["INTERFACEACTION_NO_RESOURCE_BTN"] = "<size=16>Недостаточно ресурсов</size>",

                ["INTERFACEACTION_COLLECTED_RESORUCE"] = "<size=18>Собрано</size>",

            }, this, "ru");
            PrintWarning("Lang loaded");
        }
        #endregion

        #region UI

        #region Parent

        public static string PARENT_MAINMENU = "MainMenuUIParent";
        public static string PARENT_MENUACTION = "MenuActionCraftParent";

        #endregion

        public void UI_MainMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_MAINMENU);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.2f, Color = HexToRustFormat(config.SettingsInterface.BackgrounColorMain), Material = config.SettingsInterface.BackgrounMaterialMain, Sprite = config.SettingsInterface.BackgrounSpriteMain }
            }, "Overlay", PARENT_MAINMENU);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = PARENT_MAINMENU, Color = "0 0 0 0" },
                Text = { FadeIn = 0.8f, Text = "" }
            }, PARENT_MAINMENU);

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9185187", AnchorMax = "1 0.9675928" },
                Text = { Text = lang.GetMessage("TITLE_TEXT", this), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF89") }
            }, PARENT_MAINMENU);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8833333", AnchorMax = "1 0.9231482" },
                Text = { Text = lang.GetMessage("TITLE_DESC", this), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF89") }
            }, PARENT_MAINMENU);

            #endregion

            #region LoadItemsUI

            #region SettingsCenter

            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.483646f - 0.301563f;
            float itemMargin = 0.419895f - 0.403646f;

            int truecraft = 0;
            for (int count = 0; count < config.SettingsPlugins.Count; count++)
                if (config.SettingsPlugins[count].OnOffCraft) truecraft++;

            int itemCount = truecraft;
            float itemMinHeight = 0.665741f;
            float itemHeight = 0.758333f - 0.555741f;

            if (itemCount > 5)
            {
                itemMinPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                itemCount -= 5;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            for (int i = 0; i < config.SettingsPlugins.Count; i++)
            {
                var cfg = config.SettingsPlugins[i];
                if (!cfg.OnOffCraft) continue;

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { Color = HexToRustFormat(config.SettingsInterface.PItemsBackgroundColor) }
                }, PARENT_MAINMENU, $"ITEM_{i}");

                #region PanelItem

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01362395 0.2233593", AnchorMax = $"0.9836512 0.9746563" },
                    Image = { Color = HexToRustFormat(config.SettingsInterface.AdditionalPItemsBackgroundColor) }
                }, $"ITEM_{i}", $"PANEL_ITEM_{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.3830987 0.3307694", AnchorMax = "1 1" },
                    Text = { Text = cfg.DisplayName, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
                }, $"PANEL_ITEM_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"PANEL_ITEM_{i}",
                    Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(cfg.DisplayName),  Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{  AnchorMin = $"0 0.3153852", AnchorMax = $"0.3408451 1" },
                        }
                });

                string color = cfg.WorkBenchLevel == 1 ? config.SettingsInterface.WorkBenchLevelColorOne : cfg.WorkBenchLevel == 2 ? config.SettingsInterface.WorkBenchLevelColorTwo : cfg.WorkBenchLevel == 3 ? config.SettingsInterface.WorkBenchLevelColorThree : config.SettingsInterface.WorkBenchLevelColorNull;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.008450687 0.02521083", AnchorMax = $"0.9943672 0.2945745" },
                    Image = { Color = HexToRustFormat(color) }
                }, $"PANEL_ITEM_{i}", $"WORKBENCH_PANEL_{i}");

                string text = cfg.WorkBenchLevel == 0 ? "INTERFACE_WORKBENCHLEVELNULL" : "INTERFACE_WORKBENCHLEVEL";
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = String.Format(lang.GetMessage(text, this), cfg.WorkBenchLevel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFF9") }
                }, $"WORKBENCH_PANEL_{i}");

                #endregion

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.01634854 0.03200842", AnchorMax = "0.9836512 0.2081301" },
                    Button = { Command = $"craftssystem craft_select {i}", Color = HexToRustFormat(config.SettingsInterface.ColorButtonCreate) },
                    Text = { Text = lang.GetMessage("INTERFACE_BTN", this), Align = TextAnchor.MiddleCenter }
                }, $"ITEM_{i}");

                #region SettingsCenter

                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % 5 == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));

                    if (itemCount > 5)
                    {
                        itemMinPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                        itemCount -= 5;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                #endregion
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }

        public void UI_CraftMenuAction(BasePlayer player,int index)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_MENUACTION);
            var cfg = config.SettingsPlugins[index];
            var interfacecfg = config.SettingsInterface;
            int i = 0;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1953125 0.01944444", AnchorMax = "0.8348958 0.4185185" },
                Image = { Color = "0 0 0 0" }
            },  PARENT_MAINMENU, PARENT_MENUACTION);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8352668", AnchorMax = "1 1" },
                Text = { Text = String.Format(lang.GetMessage("INTERFACEACTION_TITLE", this),cfg.DisplayName), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF89") }
            }, PARENT_MENUACTION);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.232899 0.2227378", AnchorMax = "0.769544 0.8120649" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_MENUACTION, "ITEM_PANEL");

            #region SettingsCenter

            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.473646f - 0.301563f;
            float itemMargin = 0.419895f - 0.403646f;

            int itemCount = config.SettingsPlugins[index].CraftingItems.Count;
            float itemMinHeight = 0.665741f;
            float itemHeight = 0.908333f - 0.505741f;

            if (itemCount > 6)
            {
                itemMinPosition = 0.5f - 6 / 2f * itemWidth - (6 - 1) / 2f * itemMargin;
                itemCount -= 6;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            foreach (var info in cfg.CraftingItems)
            {
                var Formul = player.inventory.GetAmount(ItemManager.FindItemDefinition(info.Key).itemid);
                string Color = CheckResourceCraft(player, info.Key, info.Value) ? interfacecfg.ColorPanelResourceComplete : interfacecfg.ColorPanelResourceNoComplete;
                string Status = CheckResourceCraft(player, info.Key, info.Value) ? $"{Convert.ToInt32(info.Value - Formul).ToString()}" : lang.GetMessage("INTERFACEACTION_COLLECTED_RESORUCE", this);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { Color = HexToRustFormat(Color) }
                },  "ITEM_PANEL",$"ITEM_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{i}",
                    Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(info.Key),  Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{  AnchorMin = $"0 0", AnchorMax = $"1 1" },
                        }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Status, Font = "robotocondensed-regular.ttf", Align = TextAnchor.LowerCenter, Color = HexToRustFormat("#FFFFFF89") }
                },  $"ITEM_{i}");

                i++;

                #region SettingsCenter

                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % 6 == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));

                    if (itemCount > 6)
                    {
                        itemMinPosition = 0.5f - 6 / 2f * itemWidth - (6 - 1) / 2f * itemMargin;
                        itemCount -= 6;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                #endregion
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3566774 0.07031658", AnchorMax = "0.6457654 0.1708162" },
                Button = { Command = $"craftssystem craft_item {index}", Color = HexToRustFormat(interfacecfg.ColorButtonCreate) },
                Text = { Text = lang.GetMessage("INTERFACEACTION_BTN", this), Align = TextAnchor.MiddleCenter }
            },  PARENT_MENUACTION);

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Help

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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        void MessageUI(BasePlayer player, string Messages)
        {
            rust.RunClientCommand(player, "gametip.hidegametip");
            rust.RunClientCommand(player, $"gametip.showgametip", Messages);
            timer.Once(4f, () => { rust.RunClientCommand(player, "gametip.hidegametip"); });
        }

        #endregion
    }
}