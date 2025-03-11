using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using VLB;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Crafts", "Mevent", "1.9.0⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")]
    public class Crafts : RustPlugin
    {
        //TODO: Добавить время крафта

        #region Fields⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        [PluginReference] private Plugin ImageLibrary;

        private const string Layer = "UI.Crafts";

        private static Crafts _instance;
        
        private readonly List<int> _blockedLayers = new List<int>
        {
            (int) Rust.Layer.Water, (int) Rust.Layer.Construction, (int) Rust.Layer.Trigger,
            (int) Rust.Layer.Prevent_Building,
            (int) Rust.Layer.Deployed, (int) Rust.Layer.Tree
        };

        private enum WorkbenchLevel
        {
            None = 0,
            One = 1,
            Two = 2,
            Three = 3
        }

        private enum CraftType
        {
            Команда,
            Транспорт,
            Предмет,
            Переработчик
        }

        #endregion

        #region Config⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Цвет кнопки когда все предметы присутствуют")]
            public string GreenColor = "#80FF8080";

            [JsonProperty(PropertyName = "Комманда меню крафта")]
            public string Command = "craft";

            [JsonProperty(PropertyName = "Включить дебаг?")]
            public bool useDebug = true;

            [JsonProperty(PropertyName = "Настройка цветов верстаков",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<WorkbenchLevel, WorkbenchConfig> Workbenchs =
                new Dictionary<WorkbenchLevel, WorkbenchConfig>
                {
                    [WorkbenchLevel.None] = new WorkbenchConfig("#00000080", "Верстак не требуется"),
                    [WorkbenchLevel.One] = new WorkbenchConfig("#80400080", "Верстак 1 уровня"),
                    [WorkbenchLevel.Two] = new WorkbenchConfig("#0080FF80", "Верстак 2 уровня"),
                    [WorkbenchLevel.Three] = new WorkbenchConfig("#FF000080", "Верстак 3 уровня")
                };

            [JsonProperty(PropertyName = "Настройка крафтов", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CraftConfig> CraftsList = new List<CraftConfig>
            {
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/YXjADeE.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "givecopter",
                    Permission = "crafts.all",
                    DisplayName = "Миникоптер",
                    ShortName = "electric.flasherlight",
                    SkinID = 2080145158,
                    Type = CraftType.Транспорт,
                    Prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
                    Level = WorkbenchLevel.One,
                    UseDistance = true,
                    Distance = 1.5f,
                    GiveCommand = string.Empty,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/dmWQOm6.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "giverowboat",
                    Permission = "crafts.all",
                    DisplayName = "Деревянная лодка",
                    ShortName = "coffin.storage",
                    SkinID = 2080150023,
                    Type = CraftType.Транспорт,
                    Prefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                    Level = WorkbenchLevel.Two,
                    UseDistance = true,
                    Distance = 1.5f,
                    GiveCommand = string.Empty,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/CgpVw2j.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "giverhibboat",
                    Permission = "crafts.all",
                    DisplayName = "Военная лодка",
                    ShortName = "electric.sirenlight",
                    SkinID = 2080150770,
                    Type = CraftType.Транспорт,
                    GiveCommand = string.Empty,
                    Prefab = "assets/content/vehicles/boats/rhib/rhib.prefab",
                    Level = WorkbenchLevel.Three,
                    UseDistance = true,
                    Distance = 1.5f,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/eioxlvK.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "givesedan",
                    Permission = "crafts.all",
                    DisplayName = "Машина",
                    ShortName = "woodcross",
                    SkinID = 2080151780,
                    Type = CraftType.Транспорт,
                    GiveCommand = string.Empty,
                    Prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab",
                    Level = WorkbenchLevel.Two,
                    UseDistance = true,
                    Distance = 1.5f,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/cp2Xx2A.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "givehotair",
                    Permission = "crafts.all",
                    DisplayName = "Воздушный шар",
                    ShortName = "box.repair.bench",
                    SkinID = 2080152635,
                    Type = CraftType.Транспорт,
                    GiveCommand = string.Empty,
                    Prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab",
                    Level = WorkbenchLevel.Three,
                    UseDistance = true,
                    Distance = 1.5f,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/7JZE0Lr.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "givescrapheli",
                    Permission = "crafts.all",
                    DisplayName = "Грузовой вертолёт",
                    ShortName = "lantern",
                    SkinID = 2080154394,
                    Type = CraftType.Транспорт,
                    GiveCommand = string.Empty,
                    Prefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
                    Level = WorkbenchLevel.Three,
                    UseDistance = true,
                    Distance = 1.5f,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/LLB2AVi.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "giverecycler",
                    Permission = "crafts.all",
                    DisplayName = "Домашний Переработчик",
                    ShortName = "research.table",
                    SkinID = 2186833264,
                    Type = CraftType.Переработчик,
                    Prefab = "assets/bundled/prefabs/static/recycler_static.prefab",
                    GiveCommand = string.Empty,
                    Level = WorkbenchLevel.Two,
                    UseDistance = true,
                    Distance = 1.5f,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                },
                new CraftConfig
                {
                    Enabled = true,
                    ImageURL = "https://i.imgur.com/mw1T17x.png",
                    Description = new List<string>
                    {
                        "Для создания требуется:",
                        "- Шестерни (5 шт)",
                        "- Дорожные знаки (5 шт)",
                        "- Металл (2000 шт)"
                    },
                    Command = "givelr300",
                    Permission = "crafts.all",
                    DisplayName = string.Empty,
                    ShortName = "rifle.lr300",
                    SkinID = 0,
                    Type = CraftType.Предмет,
                    Prefab = string.Empty,
                    GiveCommand = string.Empty,
                    Level = WorkbenchLevel.None,
                    UseDistance = true,
                    Distance = 1.5f,
                    Ground = true,
                    Structure = true,
                    Items = new List<ItemForCraft>
                    {
                        new ItemForCraft("gears", 5, 0),
                        new ItemForCraft("roadsigns", 5, 0),
                        new ItemForCraft("metal.fragments", 2000, 0)
                    }
                }
            };
            
            [JsonProperty(PropertyName = "Настройка переработчика")]
            public RecyclerConfig Recycler = new RecyclerConfig
            {
                Speed = 5f,
                Radius = 7.5f,
                Text = "<size=19>ПЕРЕРАБОТЧИК</size>\n<size=15>{0}/{1}</size>",
                Color = "#C5D0E6",
                Delay = 0.75f,
                Available = true,
                Owner = true,
                Amounts = new[] {0.9f, 0, 0, 0, 0, 0.5f, 0, 0, 0, 0.9f, 0.5f, 0.5f, 0, 1, 1, 0.5f, 0, 0, 0, 0, 0, 1, 1},
                Scale = 0.5f,
                DDraw = true,
                Building = true
            };
            
            [JsonProperty(PropertyName = "Настройка машины")]
            public CarConfig Car = new CarConfig
            {
                ActiveItems = new ActiveItemOptions
                {
                    Disable = true,
                    BlackList = new[]
                    {
                        "explosive.timed", "rocket.launcher", "surveycharge", "explosive.satchel"
                    }
                },
                Radius = 7.5f,
                Text = "<size=15>{0}/{1}</size>",
                Color = "#C5D0E6",
                Delay = 0.75f,
            };
        }
        
        private class CarConfig
        {
            [JsonProperty(PropertyName = "Активные предметы (которые в руки)")]
            public ActiveItemOptions ActiveItems;
            
            [JsonProperty(PropertyName = "Радиус в котором будет показан текст на машине")]
            public float Radius;
            
            [JsonProperty(PropertyName = "Текст на машине")]
            public string Text;
            
            [JsonProperty(PropertyName = "Цвет текста на машине")]
            public string Color;

            [JsonProperty(PropertyName = "Время показа текста на машине (сек)")]
            public float Delay;
        }
        
        public class ActiveItemOptions
        {
            [JsonProperty(PropertyName = "Запретить держать все предметы")]
            public bool Disable;

            [JsonProperty(PropertyName = "Список запрещённых к держанию предметов (shortname)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string[] BlackList;
        }
        
        private class RecyclerConfig
        {
            [JsonProperty(PropertyName = "Скорость переработки")]
            public float Speed;

            [JsonProperty(PropertyName = "Радиус в котором будет показан текст на переработчике")]
            public float Radius;

            [JsonProperty(PropertyName = "Показывать дамаг на переработчике")]
            public bool DDraw;
            
            [JsonProperty(PropertyName = "Текст на переработчике")]
            public string Text;

            [JsonProperty(PropertyName = "Цвет текста на переработчике")]
            public string Color;

            [JsonProperty(PropertyName = "Время показа текста на переработчике (сек)")]
            public float Delay;
            
            [JsonProperty(PropertyName = "Можно ли подбирать переработчик")]
            public bool Available;
            
            [JsonProperty(PropertyName = "Подбор только владельцем?")]
            public bool Owner;
            
            [JsonProperty(PropertyName = "Право на постройку для подбора")]
            public bool Building;

            [JsonProperty(PropertyName = "Настройка BaseProtection",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float[] Amounts;

            [JsonProperty(PropertyName = "Множитель урона по переработчику")]
            public float Scale;
        }

        private class WorkbenchConfig
        {
            [JsonProperty(PropertyName = "Цвет")] 
            public string Color;

            [JsonProperty(PropertyName = "Надпись")]
            public string Title;

            public WorkbenchConfig(string color, string title)
            {
                Color = color;
                Title = title;
            }
        }

        private class CraftConfig
        {
            [JsonProperty(PropertyName = "Включить крафт?")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Картинка")]
            public string ImageURL;

            [JsonProperty(PropertyName = "Описание")]
            public List<string> Description;

            [JsonProperty(PropertyName = "Команда для получения")]
            public string Command;

            [JsonProperty(PropertyName = "Право на крафт")]
            public string Permission;

            [JsonProperty(PropertyName = "Отображаемое имя заменяемого предмета")]
            public string DisplayName;

            [JsonProperty(PropertyName = "Shortname заменяемого предмета")]
            public string ShortName;

            [JsonProperty(PropertyName = "Скин заменяемого предмета")]
            public ulong SkinID;

            [JsonProperty(PropertyName = "Тип предмета (Предмет/Команда/Транспорт)")]
            [JsonConverter(typeof(StringEnumConverter))]
            public CraftType Type;

            [JsonProperty(PropertyName = "Префаб (для транспорта)")]
            public string Prefab;

            [JsonProperty(PropertyName = "Команда при получении")]
            public string GiveCommand;

            [JsonProperty(PropertyName = "Уровень верстака")]
            public WorkbenchLevel Level;

            [JsonProperty(PropertyName = "Включить проверку на дистанцию?")]
            public bool UseDistance;

            [JsonProperty(PropertyName = "Дистанция")]
            public float Distance;
            
            [JsonProperty(PropertyName = "Установка на землю")]
            public bool Ground;
            
            [JsonProperty(PropertyName = "Установка на строения")]
            public bool Structure;
            
            [JsonProperty(PropertyName = "Настройка предметов для крафта",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ItemForCraft> Items;

            public Item ToItem()
            {
                var newItem = ItemManager.CreateByName(ShortName, 1, SkinID);
                if (newItem == null)
                {
                    Debug.LogError($"Error creating item with ShortName: '{ShortName}'");
                    return null;
                }

                if (!DisplayName.IsNullOrEmpty()) newItem.name = DisplayName;

                return newItem;
            }

            public void Give(BasePlayer player)
            {
                if (player == null) return;

                var item = ToItem();
                if (item == null) return;
                
                player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
            }
        }

        private class ItemForCraft
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string ShortName;

            [JsonProperty(PropertyName = "Количество")]
            public int Amount;

            [JsonProperty(PropertyName = "Скин")] public ulong SkinID;

            public ItemForCraft(string shortname, int amount, ulong skin)
            {
                ShortName = shortname;
                Amount = amount;
                SkinID = skin;
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

        #region Hooks⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        private void OnServerInitialized()
        {
            _instance = this;
            
            if (!ImageLibrary) PrintWarning("IMAGE LIBRARY IS NOT INSTALLED.");

            foreach (var item in _config.CraftsList)
            {
                if (!item.ImageURL.IsNullOrEmpty())
                    ImageLibrary?.Call("AddImage", item.ImageURL, item.ImageURL);

                if (!item.Command.IsNullOrEmpty())
                    AddCovalenceCommand(item.Command, nameof(CmdGiveItem));

                if (!item.Permission.IsNullOrEmpty() && !permission.PermissionExists(item.Permission))
                    permission.RegisterPermission(item.Permission, this);
            }

            foreach (var ent in BaseNetworkable.serverEntities)
            {
                OnEntitySpawned(ent as BaseEntity);
            }
            
            cmd.AddChatCommand(_config.Command, this, nameof(CmdChatOpenUI));
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);

            foreach (var component in UnityEngine.Object.FindObjectsOfType<RecyclerComponent>())
                if (component != null)
                    component.Kill();
            
            foreach (var component in UnityEngine.Object.FindObjectsOfType<CarController>())
                if (component != null)
                    component.Kill();
            
            _config = null;
            _instance = null;
        }

        private void OnEntityBuilt(Planner held, GameObject go)
        {
            if (held == null || go == null) return;

            var player = held.GetOwnerPlayer();
            if (player == null) return;

            var entity = go.ToBaseEntity();
            if (entity == null || entity.skinID == 0) return;

            var craft = _config.CraftsList.FirstOrDefault(x => (x.Type == CraftType.Транспорт || x.Type == CraftType.Переработчик) && x.SkinID == entity.skinID);
            if (craft == null) return;

            var transform = entity.transform;
            
            var itemName = !string.IsNullOrEmpty(craft.DisplayName)
                ? craft.DisplayName
                : ItemManager.FindItemDefinition(craft.ShortName)?.displayName.translated ?? "ITEM";
            
            NextTick(() =>
            {
                if (entity != null) 
                    entity.Kill();
            });
            
            RaycastHit rHit;
            if (Physics.Raycast(transform.position + new Vector3(0, 0.1f, 0), Vector3.down, out rHit, 4f,
                LayerMask.GetMask("Construction")) && rHit.GetEntity() != null)
            {
                if (!craft.Structure)
                {
                    Reply(player, OnStruct, itemName);
                    GiveCraft(player, craft);
                    return;
                }
            }
            else
            { 
                if (!craft.Ground)
                {
                    Reply(player, OnGround, itemName);
                    GiveCraft(player, craft);
                    return;
                }
            }

            SpawnVehicle(craft.Prefab, player.userID, craft.SkinID, transform.position, transform.rotation);
        }
        
        private object CanResearchItem(BasePlayer player, Item item)
        {
            if (player == null || item == null || !_config.CraftsList.Exists(x => x.Type == CraftType.Транспорт && x.SkinID == item.skin)) return null;
            return false;
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null) return;

            if (entity is Recycler) 
                entity.gameObject.AddComponent<RecyclerComponent>();

            if (entity is BasicCar)
                entity.gameObject.AddComponent<CarController>();
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || entity.OwnerID == 0) return;

            var recycler = entity.GetComponent<RecyclerComponent>();
            if (recycler != null)
            {
                info.damageTypes.ScaleAll(_config.Recycler.Scale);
                recycler.DDraw();
            }

            var car = entity.GetComponent<CarController>();
            if (car != null)
            {
                car.ManageDamage(info);
                car.DDraw();
            }
        }

        private object OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (recycler == null || player == null) return null;

            var component = recycler.GetComponent<RecyclerComponent>();
            if (component == null) return null;

            if (!recycler.IsOn())
            {
                foreach (var obj in recycler.inventory.itemList)
                    obj.CollectedForCrafting(player);

                component.StartRecycling();
            }
            else
            {
                component.StopRecycling();
            }

            return false;
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return;

            var entity = info.HitEntity;
            if (entity == null) return;

            var component = entity.GetComponent<RecyclerComponent>();
            if (component == null) return;

            if (!_config.Recycler.Available)
            {
                Reply(player, NotTake);
                return;
            }
            
            component.TryPickup(player);
        }

        #endregion

        #region Commands⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        [ConsoleCommand("UI_Crafts")]
        private void CmdConsoleCraft(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (!arg.HasArgs())
            {
                DrawUI(player, isFirst: true);
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "page":
                {
                    var page = 0;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

                    DrawUI(player, page);
                    break;
                }
                case "craft":
                {
                    int itemid;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out itemid)
                                        || !(itemid >= 0 && _config.CraftsList.Count > itemid)) return;

                    var craftItem = _config.CraftsList[itemid];
                    if (craftItem == null) return;

                    if (!HasWorkbench(player, craftItem.Level))
                    {
                        Reply(player, "NOT WORKBENCH");
                        return;
                    }

                    var playerItems = player.inventory.AllItems();

                    if (!HasAllItems(playerItems, craftItem))
                    {
                        Reply(player, "NOT.RESOURCES");
                        return;
                    }

                    for (var i = 0; i < craftItem.Items.Count; i++)
                    {
                        var item = craftItem.Items[i];
                        if (item == null) continue;
                        Take(playerItems, item.ShortName, item.SkinID, item.Amount);
                    }

                    GiveCraft(player, craftItem);
                    //CraftItem(player, craftItem);
                    Reply(player, "GIVED CRAFT",
                        !string.IsNullOrEmpty(craftItem.DisplayName)
                            ? craftItem.DisplayName
                            : ItemManager.FindItemDefinition(craftItem.ShortName).displayName.translated);
                    break;
                }
            }
        }

        private void CmdGiveItem(IPlayer iPlayer, string cmd, string[] args)
        {
            if (args.Length == 0) return;
            var player = BasePlayer.Find(args[0]);
            if (player == null)
            {
                Reply(iPlayer, "PLAYER NOT FOUND", args[0]);
                return;
            }

            var craftItem = _config.CraftsList.FirstOrDefault(x => x.Command == cmd);
            if (craftItem == null)
            {
                iPlayer.Reply("COMMAND NOT FOUND", cmd);
                return;
            }

            var item = craftItem.ToItem();
            if (item == null)
                return;

            var itemName = !string.IsNullOrEmpty(craftItem.DisplayName)
                ? craftItem.DisplayName
                : item.info.displayName.translated;

            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
            Reply(player, "GIVED CRAFT", itemName);

            if (_config.useDebug) Reply(iPlayer, "GIVE.DEBUG", player.displayName, player.UserIDString, itemName);
        }

        private void CmdChatOpenUI(BasePlayer player, string cmd, string[] args)
        {
            DrawUI(player, isFirst: true);
        }

        #endregion

        #region Interface⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        private void DrawUI(BasePlayer player, int page = 0, bool isFirst = false)
        {
            var container = new CuiElementContainer();

            var playerItems = player.inventory.AllItems();

            #region First

            if (isFirst)
            {
                CuiHelper.DestroyUi(player, Layer);

                #region BG

                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Image =
                    {
                        Color = "0.1 0.1 0.05 0.75",
                        Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    CursorEnabled = true
                }, "Overlay", Layer);

                #endregion

                #region Title

                container.Add(new CuiLabel
                {
                    RectTransform =
                        {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-200 200", OffsetMax = "200 300"},
                    Text =
                    {
                        Text = lang.GetMessage("Title", this, player.UserIDString), Align = TextAnchor.MiddleCenter,
                        FontSize = 28
                    }
                }, Layer);

                #endregion
            }

            #endregion

            #region Main

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0"}
            }, Layer, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = "-45 -45", OffsetMax = "-5 -5"
                },
                Text =
                {
                    Text = "✕",
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 28,
                    Color = "1 1 1 1"
                },
                Button =
                {
                    Color = "0 0 0 0",
                    Close = Layer
                }
            }, Layer + ".Main");

            #region Items

            var list = GetPlayerCrafts(player, page);

            if (list.Count > 0)
            {
                var xSwitch = -(220 * list.Count + 40 * (list.Count - 1)) / 2;

                for (var i = 0; i < list.Count; i++)
                {
                    var craft = list[i];

                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{xSwitch} -100",
                            OffsetMax = $"{xSwitch + 220} 200"
                        },
                        Image = {Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"}
                    }, Layer + ".Main", Layer + $".Craft.{xSwitch}");

                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".Craft.{xSwitch}",
                        Components =
                        {
                            new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", craft.ImageURL)},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-70 0", OffsetMax = "70 140"
                            }
                        }
                    });

                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-85 -10", OffsetMax = "85 -5"
                        },
                        Image = {Color = "1 1 1 1", Sprite = "assets/content/ui/gameui/compass/alpha_mask.png"}
                    }, Layer + $".Craft.{xSwitch}");

                    container.Add(new CuiLabel
                    {
                        RectTransform =
                            {AnchorMin = "0 0.5", AnchorMax = "1 0.5", OffsetMin = "0 -130", OffsetMax = "0 -25"},
                        Text =
                        {
                            Text = string.Join("\n", craft.Description), Align = TextAnchor.UpperCenter,
                            FontSize = 14
                        }
                    }, Layer + $".Craft.{xSwitch}");

                    container.Add(new CuiPanel
                    {
                        RectTransform =
                            {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -50", OffsetMax = "0 -5"},
                        Image =
                        {
                            Color = HexToCuiColor(_config.Workbenchs[craft.Level].Color),
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                        }
                    }, Layer + $".Craft.{xSwitch}", Layer + $".Craft.{xSwitch}.Workbench");

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text =
                        {
                            Text = _config.Workbenchs[craft.Level].Title, Align = TextAnchor.MiddleCenter,
                            FontSize = 14
                        }
                    }, Layer + $".Craft.{xSwitch}.Workbench");

                    var active = HasAllItems(playerItems, craft) && HasWorkbench(player, craft.Level);
                    container.Add(new CuiButton
                    {
                        RectTransform =
                            {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -105", OffsetMax = "0 -55"},
                        Button =
                        {
                            Command = active ? $"UI_Crafts craft {_config.CraftsList.IndexOf(craft)}" : "",
                            Color = active ? HexToCuiColor(_config.GreenColor) : "0 0 0 0.5",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Close = active ? Layer : ""
                        },
                        Text =
                        {
                            Text = lang.GetMessage("CREATE", this, player.UserIDString),
                            Align = TextAnchor.MiddleCenter, FontSize = 24
                        }
                    }, Layer + $".Craft.{xSwitch}");

                    xSwitch += 260;
                }
            }
            else
            {
                container.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-250 -150", OffsetMax = "250 150"
                    },
                    Text =
                    {
                        Text = lang.GetMessage("NOT CRAFTS", this, player.UserIDString),
                        Align = TextAnchor.MiddleCenter, FontSize = 34
                    }
                }, Layer + ".Main");
            }

            #endregion

            #region Pages

            if (list.Count > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                        {AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "5 -25", OffsetMax = "55 25"},
                    Button =
                    {
                        Command = page > 0 ? $"UI_Crafts page {page - 1}" : "",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 40,
                        Color = page > 0 ? "1 1 1 1" : "1 1 1 0.5"
                    }
                }, Layer + ".Main");

                var count = _config.CraftsList.Count(craft => craft.Enabled &&
                                                              (string.IsNullOrEmpty(craft.Permission) ||
                                                               permission.UserHasPermission(player.UserIDString,
                                                                   craft.Permission)));
                
                container.Add(new CuiButton
                {
                    RectTransform =
                        {AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-55 -25", OffsetMax = "-5 25"},
                    Button =
                    {
                        Command = count > (page + 1) * 3 ? $"UI_Crafts page {page + 1}" : "",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 40,
                        Color = count > (page + 1) * 3 ? "1 1 1 1" : "1 1 1 0.5"
                    }
                }, Layer + ".Main");
            }

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.AddUi(player, container);
        }
        
        #endregion

        #region Utils⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        private void GiveCraft(BasePlayer player, CraftConfig cfg)
        {
            switch (cfg.Type)
            {
                case CraftType.Команда:
                {
                    var command = cfg.GiveCommand.Replace("\n", "|")
                        .Replace("%steamid%", player.UserIDString, StringComparison.OrdinalIgnoreCase).Replace(
                            "%username%",
                            player.displayName, StringComparison.OrdinalIgnoreCase);

                    foreach (var check in command.Split('|')) Server.Command(check);
                    break;
                }
                default:
                {
                    cfg.Give(player);
                    break;
                }
            }
        }

        private void CraftItem(BasePlayer player, CraftConfig item)
        {
            var defenition = ItemManager.FindItemDefinition(item.ShortName);

            var task = Pool.Get<ItemCraftTask>();
            task.blueprint = defenition.Blueprint;
            task.endTime = 0.0f;
            task.taskUID = player.inventory.crafting.taskUID + 1;
            task.owner = player;
            task.instanceData = null;
            if (task.instanceData != null)
                task.instanceData.ShouldPool = false;
            task.amount = 1;
            task.skinID = (int) item.SkinID;

            player.inventory.crafting.queue.AddLast(task);
            if (task.owner != null)
                task.owner.Command("note.craft_add", (object) task.taskUID, (object) task.blueprint.targetItem.itemid,
                    (object) 1, (object) task.skinID);
        }

        private static bool HasWorkbench(BasePlayer player, WorkbenchLevel level)
        {
            return level == WorkbenchLevel.Three ? player.HasPlayerFlag(BasePlayer.PlayerFlags.Workbench3)
                : level == WorkbenchLevel.Two ? player.HasPlayerFlag(BasePlayer.PlayerFlags.Workbench3) ||
                                                player.HasPlayerFlag(BasePlayer.PlayerFlags.Workbench2)
                : level == WorkbenchLevel.One ? player.HasPlayerFlag(BasePlayer.PlayerFlags.Workbench3) ||
                                                player.HasPlayerFlag(BasePlayer.PlayerFlags.Workbench2) ||
                                                player.HasPlayerFlag(BasePlayer.PlayerFlags.Workbench1)
                : level == WorkbenchLevel.None;
        }

        private static bool HasAllItems(IReadOnlyList<Item> items, CraftConfig craftConfig)
        {
            for (var i = 0; i < craftConfig.Items.Count; i++)
            {
                var itemForCraft = craftConfig.Items[i];

                if (ItemCount(items, itemForCraft.ShortName, itemForCraft.SkinID) < itemForCraft.Amount) return false;
            }

            return true;
        }

        private static int ItemCount(IReadOnlyList<Item> items, string shortname, ulong skin)
        {
            var result = 0;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.info.shortname == shortname && (skin == 0 || item.skin == skin))
                    result += item.amount;
            }

            return result;
        }

        private void Take(IEnumerable<Item> itemList, string shortname, ulong skinId, int iAmount)
        {
            var num1 = 0;
            if (iAmount == 0) return;

            var list = Pool.GetList<Item>();

            foreach (var item in itemList)
            {
                if (item.info.shortname != shortname ||
                    skinId != 0 && item.skin != skinId) continue;

                var num2 = iAmount - num1;
                if (num2 <= 0) continue;
                if (item.amount > num2)
                {
                    item.MarkDirty();
                    item.amount -= num2;
                    num1 += num2;
                    break;
                }

                if (item.amount <= num2)
                {
                    num1 += item.amount;
                    list.Add(item);
                }

                if (num1 == iAmount)
                    break;
            }

            foreach (var obj in list)
                obj.RemoveFromContainer();

            Pool.FreeList(ref list);
        }

        private void SpawnVehicle(string prefab, ulong owner, ulong skin, Vector3 position,
            Quaternion rotation)
        {
            var entity = GameManager.server.CreateEntity(prefab, position, rotation);
            if (entity == null) return;
            entity.skinID = skin;
            entity.OwnerID = owner;
            entity.Spawn();
        }

        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8) throw new Exception(hex);

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private List<CraftConfig> GetPlayerCrafts(BasePlayer player, int page, int count = 3)
        {
            var result = new List<CraftConfig>();
            var skipCount = page * count;

            for (var i = 0; i < _config.CraftsList.Count; i++)
            {
                var craft = _config.CraftsList[i];

                if (i < skipCount) continue;

                if (craft.Enabled && (string.IsNullOrEmpty(craft.Permission) ||
                                      permission.UserHasPermission(player.UserIDString, craft.Permission)))
                    result.Add(craft);

                if (result.Count >= count) break;
            }

            return result;
        }
        
        private static Color HexToUnityColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8) throw new Exception(hex);

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return color;
        }
        
        private static void SetPlayerFlag(BasePlayer player, BasePlayer.PlayerFlags f, bool b)
        {
            if (b)
            {
                if (player.HasPlayerFlag(f)) return;
                player.playerFlags |= f;
            }
            else
            {
                if (!player.HasPlayerFlag(f)) return;
                player.playerFlags &= ~f;
            }

            player.SendNetworkUpdateImmediate();
        }

        #endregion

        #region Recycler Component⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        
        private class RecyclerComponent : FacepunchBehaviour
        {
            private Recycler recycler;

            private GroundWatch groundWatch;
            private DestroyOnGroundMissing groundMissing;

            [NonSerialized] private readonly BaseEntity[] SensesResults = new BaseEntity[64];

            private void Awake()
            {
                recycler = GetComponent<Recycler>();

                if (recycler.OwnerID != 0)
                {
                    recycler.baseProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
                    recycler.baseProtection.amounts = _config.Recycler.Amounts;
                    
                    groundWatch = recycler.GetOrAddComponent<GroundWatch>();

                    groundMissing = recycler.GetOrAddComponent<DestroyOnGroundMissing>();
                }
            }

            public void DDraw()
            {
                if (recycler == null)
                {
                    Kill();
                    return;
                }

                if (recycler.OwnerID == 0 || !_config.Recycler.DDraw)
                    return;

                var inSphere = BaseEntity.Query.Server.GetInSphere(recycler.transform.position, _config.Recycler.Radius,
                    SensesResults, entity => entity is BasePlayer);
                if (inSphere == 0)
                    return;

                for (var i = 0; i < inSphere; i++)
                {
                    var user = SensesResults[i] as BasePlayer;
                    if (user == null || user.IsDestroyed || !user.IsConnected || user.IsNpc ||
                        !user.userID.IsSteamId()) continue;

                    if (user.Connection.authLevel < 2) SetPlayerFlag(user, BasePlayer.PlayerFlags.IsAdmin, true);

                    user.SendConsoleCommand("ddraw.text", _config.Recycler.Delay, HexToUnityColor(_config.Recycler.Color),
                        recycler.transform.position + new Vector3(0.25f, 1, 0), 
                        string.Format(_config.Recycler.Text, recycler.health, recycler._maxHealth));

                    if (user.Connection.authLevel < 2) SetPlayerFlag(user, BasePlayer.PlayerFlags.IsAdmin, false);
                }
            }

            #region Methods

            public void StartRecycling()
            {
                if (recycler.IsOn())
                    return;

                InvokeRepeating(RecycleThink, _config.Recycler.Speed, _config.Recycler.Speed);
                Effect.server.Run(recycler.startSound.resourcePath, recycler, 0U, Vector3.zero, Vector3.zero);
                recycler.SetFlag(BaseEntity.Flags.On, true);

                recycler.SendNetworkUpdateImmediate();
            }

            public void StopRecycling()
            {
                CancelInvoke(RecycleThink);

                if (!recycler.IsOn())
                    return;

                Effect.server.Run(recycler.stopSound.resourcePath, recycler, 0U, Vector3.zero, Vector3.zero);
                recycler.SetFlag(BaseEntity.Flags.On, false);
                recycler.SendNetworkUpdateImmediate();
            }

            public void RecycleThink()
            {
                var flag = false;
                var num1 = recycler.recycleEfficiency;
                for (var slot1 = 0; slot1 < 6; ++slot1)
                {
                    var slot2 = recycler.inventory.GetSlot(slot1);
                    if (slot2 != null)
                    {
                        if (Interface.CallHook("OnRecycleItem", recycler, slot2) != null)
                        {
                            if (HasRecyclable())
                                return;
                            StopRecycling();
                            return;
                        }

                        if (slot2.info.Blueprint != null)
                        {
                            if (slot2.hasCondition)
                                num1 = Mathf.Clamp01(
                                    num1 * Mathf.Clamp(slot2.conditionNormalized * slot2.maxConditionNormalized, 0.1f,
                                        1f));
                            var num2 = 1;
                            if (slot2.amount > 1)
                                num2 = Mathf.CeilToInt(Mathf.Min(slot2.amount, slot2.info.stackable * 0.1f));
                            if (slot2.info.Blueprint.scrapFromRecycle > 0)
                            {
                                var iAmount = slot2.info.Blueprint.scrapFromRecycle * num2;
                                if (slot2.info.stackable == 1 && slot2.hasCondition)
                                    iAmount = Mathf.CeilToInt(iAmount * slot2.conditionNormalized);
                                if (iAmount >= 1)
                                    recycler.MoveItemToOutput(ItemManager.CreateByName("scrap", iAmount));
                            }

                            if (!string.IsNullOrEmpty(slot2.info.Blueprint.RecycleStat))
                            {
                                var list = Pool.GetList<BasePlayer>();
                                Vis.Entities(transform.position, 3f, list, 131072);
                                foreach (var basePlayer in list)
                                    if (basePlayer.IsAlive() && !basePlayer.IsSleeping() &&
                                        basePlayer.inventory.loot.entitySource == recycler)
                                    {
                                        basePlayer.stats.Add(slot2.info.Blueprint.RecycleStat, num2,
                                            Stats.Steam | Stats.Life);
                                        basePlayer.stats.Save();
                                    }

                                Pool.FreeList(ref list);
                            }

                            slot2.UseItem(num2);
                            using (var enumerator = slot2.info.Blueprint.ingredients.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    var current = enumerator.Current;
                                    if (current != null && current.itemDef.shortname != "scrap")
                                    {
                                        var num3 = current.amount / slot2.info.Blueprint.amountToCreate;
                                        var num4 = 0;
                                        if (num3 <= 1.0)
                                        {
                                            for (var index = 0; index < num2; ++index)
                                                if (Random.Range(0.0f, 1f) <= num3 * (double) num1)
                                                    ++num4;
                                        }
                                        else
                                        {
                                            num4 = Mathf.CeilToInt(
                                                Mathf.Clamp(num3 * num1 * Random.Range(1f, 1f), 0.0f, current.amount) *
                                                num2);
                                        }

                                        if (num4 > 0)
                                        {
                                            var num5 = Mathf.CeilToInt(num4 / (float) current.itemDef.stackable);
                                            for (var index = 0; index < num5; ++index)
                                            {
                                                var iAmount = num4 > current.itemDef.stackable
                                                    ? current.itemDef.stackable
                                                    : num4;
                                                if (!recycler.MoveItemToOutput(ItemManager.Create(current.itemDef,
                                                    iAmount)))
                                                    flag = true;
                                                num4 -= iAmount;
                                                if (num4 <= 0)
                                                    break;
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                if (!flag && HasRecyclable())
                    return;
                StopRecycling();
            }

            public bool HasRecyclable()
            {
                for (var slot1 = 0; slot1 < 6; ++slot1)
                {
                    var slot2 = recycler.inventory.GetSlot(slot1);
                    if (slot2 != null)
                    {
                        var can = Interface.CallHook("CanRecycle", recycler, slot2);
                        if (can is bool)
                            return (bool) can;

                        if (slot2.info.Blueprint != null)
                            return true;
                    }
                }

                return false;
            }

            #endregion

            #region Destroy

            public void TryPickup(BasePlayer player)
            {
                if (_config.Recycler.Building && !player.CanBuild())
                {
                    player.ChatMessage("Для этого у вас должна быть способность строить!");
                    return;
                }

                if (_config.Recycler.Owner && recycler.OwnerID != player.userID)
                {
                    player.ChatMessage("Только владелец может подбирать переработчик!");
                    return;
                }

                if (recycler.SecondsSinceDealtDamage < 30f)
                {
                    player.ChatMessage("Переработчик был недавно поврежден, вы можете забрать его через 30 секунд!");
                    return;
                }

                recycler.Kill();
                
                var craft = _config.CraftsList.FirstOrDefault(x => x.Type == CraftType.Переработчик);
                if (craft == null)
                {
                    player.ChatMessage($"Обратитесь к администратору. Переработчик не может быть выдан");
                    return;
                }
                
                _instance?.GiveCraft(player, craft);
            }

            private void OnDestroy()
            {
                CancelInvoke();

                Destroy(this);
            }

            public void Kill()
            {
                Destroy(this);
            }

            #endregion
        }
        
        #endregion

        #region Car Component⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        public class CarController : FacepunchBehaviour
        {
            public BasicCar entity;
            public BasePlayer player;
            public bool isDieing;

            private bool allowHeldItems;
            private string[] disallowedItems;

            [NonSerialized] private readonly BaseEntity[] SensesResults = new BaseEntity[64];

            private void Awake()
            {
                entity = GetComponent<BasicCar>();

                allowHeldItems = !_config.Car.ActiveItems.Disable;
                disallowedItems = _config.Car.ActiveItems.BlackList;
            }

            private void Update()
            {
                UpdateHeldItems();
                CheckWaterLevel();
            }

            public void ManageDamage(HitInfo info)
            {
                if (isDieing)
                {
                    NullifyDamage(info);
                    return;
                }

                if (info.damageTypes.GetMajorityDamageType() == DamageType.Bullet)
                    info.damageTypes.ScaleAll(200);

                if (info.damageTypes.Total() >= entity.health)
                {
                    isDieing = true;
                    NullifyDamage(info);
                    OnDeath();
                    return;
                }
            }

            public void DDraw()
            {
                if (entity == null)
                {
                    Kill();
                    return;
                }

                if (entity.OwnerID == 0)
                    return;

                var inSphere = BaseEntity.Query.Server.GetInSphere(entity.transform.position, _config.Car.Radius,
                    SensesResults, ent => ent is BasePlayer);
                if (inSphere == 0)
                    return;

                for (var i = 0; i < inSphere; i++)
                {
                    var user = SensesResults[i] as BasePlayer;
                    if (user == null || user.IsDestroyed || !user.IsConnected || user.IsNpc ||
                        !user.userID.IsSteamId()) continue;

                    if (user.Connection.authLevel < 2) SetPlayerFlag(user, BasePlayer.PlayerFlags.IsAdmin, true);

                    user.SendConsoleCommand("ddraw.text", _config.Car.Delay, HexToUnityColor(_config.Car.Color),
                        entity.transform.position + new Vector3(0.25f, 1, 0), 
                        string.Format(_config.Car.Text, entity.health, entity._maxHealth));

                    if (user.Connection.authLevel < 2) SetPlayerFlag(user, BasePlayer.PlayerFlags.IsAdmin, false);
                }
            }
            
            private void NullifyDamage(HitInfo info)
            {
                info.damageTypes = new DamageTypeList();
                info.HitEntity = null;
                info.HitMaterial = 0;
                info.PointStart = Vector3.zero;
            }

            public void UpdateHeldItems()
            {
                if (player == null)
                    return;

                var item = player.GetActiveItem();
                if (item == null || item.GetHeldEntity() == null)
                    return;

                if (disallowedItems.Contains(item.info.shortname) || !allowHeldItems)
                {
                    _instance?.Reply(player, ItemNotAllowed);
                    
                    var slot = item.position;
                    item.SetParent(null);
                    item.MarkDirty();

                    Invoke(() =>
                    {
                        if (player == null || item == null) return;
                        item.SetParent(player.inventory.containerBelt);
                        item.position = slot;
                        item.MarkDirty();
                    }, 0.15f);
                }
            }

            public void CheckWaterLevel()
            {
                if (WaterLevel.Factor(entity.WorldSpaceBounds().ToBounds()) > 0.7f)                
                    StopToDie();                
            }

            public void StopToDie(bool death = true)
            {
                if (entity != null)
                {
                    entity.SetFlag(BaseEntity.Flags.Reserved1, false, false);

                    foreach (var wheel in entity.wheels)
                    {
                        wheel.wheelCollider.motorTorque = 0;
                        wheel.wheelCollider.brakeTorque = float.MaxValue;
                    }

                    entity.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    if (player != null)
                        entity.DismountPlayer(player);
                }
                if (death) OnDeath();
            }

            private void OnDeath()
            {
                isDieing = true;

                if (player != null)                
                    player.EnsureDismounted();                

                Invoke(() =>
                {
                    Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", transform.position);
                    _instance.NextTick(() =>
                    {
                        if (entity != null && !entity.IsDestroyed)
                            entity.DieInstantly();
                        Destroy(this);
                    });
                }, 5f);
            }

            public void Kill()
            {
                StopToDie(false);
                Destroy(this);
            }
        }

        #endregion
        
        #region Lang⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        private const string 
            NOTRESOURCES = "NOT.RESOURCES",
            PLAYERNOTFOUND = "PLAYER NOT FOUND",
            COMMANDNOTFOUND = "COMMAND NOT FOUND",
            GIVECRAFT = "GIVECRAFT",
            GIVEDEBUG = "GIVE.DEBUG",
            NOTWORKBENCH = "NOT WORKBENCH", 
            NOTCRAFTS = "NOT CRAFTS",
            CREATE = "CREATE",
            Title = "Title",
            OnGround = "OnGround",
            BuildDistance = "BuildDistance",
            OnStruct = "OnStruct",
            NotTake = "NotTake",
            ItemNotAllowed = "ItemNotAllowed";
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [NOTRESOURCES] = "Недостаточно ресурсов",
                [PLAYERNOTFOUND] = "Игрок {0} не найден",
                [COMMANDNOTFOUND] = "Комманда {0} не найдена",
                [GIVECRAFT] = "Поздравляем! Вы получили {0}",
                [GIVEDEBUG] = "Игроку {0} ({1}) выдано: {2}",
                [NOTWORKBENCH] = "Не достаточный уровень верстака для крафта!",
                [NOTCRAFTS] = "Для Вас нет доступных крафтов",
                [CREATE] = "<b>СОЗДАТЬ</b>",
                [Title] = "<b>МЕНЮ КРАФТОВ</b>",
                [OnGround] = "{0} нельзя ставить на землю!",
                [BuildDistance] = "Установка ближе {0}м к себе запрещена!",
                [OnStruct] = "{0} нельзя ставить на строения!",
                [NotTake] = "Подбор переработчиков выключен",
                [ItemNotAllowed] = "Предмет запрещён к ношению"
            }, this);
        }

        private void Reply(BasePlayer player, string key, params object[] obj)
        {
            SendReply(player, string.Format(lang.GetMessage(key, this, player.UserIDString), obj));
        }

        private void Reply(IPlayer player, string key, params object[] obj)
        {
            player.Reply(string.Format(lang.GetMessage(key, this, player.Id), obj));
        }

        #endregion
    }
}