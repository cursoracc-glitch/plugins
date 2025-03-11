using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("QuarryLevels", "Death", "1.0.3")]
    class QuarryLevels : RustPlugin
    {
        #region Declarations
        [PluginReference] Plugin ImageLibrary, Economics;

        const string perm = "quarrylevels.use";
        System.Random random = new System.Random();

        Dictionary<BasePlayer, MiningQuarry> ActiveGUI = new Dictionary<BasePlayer, MiningQuarry>();
        #endregion

        #region Hooks
        void OnServerInitialized()
        {
            LoadConfig();

            List<KeyValuePair<string, ulong>> imageList = new List<KeyValuePair<string, ulong>>
            {
                { new KeyValuePair<string, ulong>("mining.quarry", 0ul) },
                { new KeyValuePair<string, ulong>("mining.pumpjack", 0ul) },
                { new KeyValuePair<string, ulong>("stones", 0ul) },
                { new KeyValuePair<string, ulong>("sulfur.ore", 0ul) },
                { new KeyValuePair<string, ulong>("hq.metal.ore", 0ul) },
                { new KeyValuePair<string, ulong>("metal.ore", 0ul) },
                { new KeyValuePair<string, ulong>("crude.oil", 0ul) },
            };

            ImageLibrary.Call("LoadImageList", Title, imageList, null);
            permission.RegisterPermission(perm, this);

            foreach (var quarry in BaseNetworkable.serverEntities.OfType<MiningQuarry>())
            {
                if (quarry.OwnerID != 0)
                {
                    OnEntitySpawned(quarry);
                }
            }
        }

        void Unload()
        {
            foreach (var active in ActiveGUI)
            {
                CuiHelper.DestroyUi(active.Key, "upgradebutton");
                CuiHelper.DestroyUi(active.Key, "upgradeconfirm");
            }
        }

        object CanLootEntity(BasePlayer player, ResourceExtractorFuelStorage quarry)
        {
            if (!options.PlayerSettings.PreventUnauthorizedLooting)
            {
                return null;
            }

            Puts(quarry.GetParentEntity().OwnerID.ToString());

            if (player.userID != quarry.GetParentEntity().OwnerID && !player.IsBuildingAuthed())
            {
                return true;
            }

            return null;
        }

        void OnLootEntity(BasePlayer player, ResourceExtractorFuelStorage quarry)
        {
            if (quarry.OwnerID != player.userID && !player.IsBuildingAuthed() || !quarry.HasParent() || !permission.UserHasPermission(player.UserIDString, perm))
            {
                return;
            }

            if (!ActiveGUI.ContainsKey(player))
            {
                CreateGUI(player);
                ActiveGUI.Add(player, quarry.GetParentEntity().GetComponent<MiningQuarry>());
            }
        }

        void OnLootEntityEnd(BasePlayer player, ResourceExtractorFuelStorage quarry)
        {
            if (ActiveGUI.ContainsKey(player))
            {
                DestroyCUI(player);
            }
        }

        void OnEntityKill(MiningQuarry quarry)
        {
            if (quarry.OwnerID == 0 || quarry.ShortPrefabName.Equals("pumpjack-static"))
            {
                return;
            }

            List<ResourceDepositManager.ResourceDeposit.ResourceDepositEntry> Resources = new List<ResourceDepositManager.ResourceDeposit.ResourceDepositEntry>(quarry._linkedDeposit._resources);

            foreach (var res in Resources)
            {
                if (res.workNeeded == 4 || res.workNeeded == 50)
                {
                    quarry._linkedDeposit._resources.Remove(res);
                }
            }

            Resources.Clear();
        }

        void OnQuarryToggled(MiningQuarry quarry, BasePlayer player)
        {
            if (options.PlayerSettings.PreventUnauthorizedToggling && player.userID == quarry.OwnerID || !player.IsBuildingAuthed())
            {
                quarry.SetOn(!quarry.IsOn());
                return;
            }

            if (!HasFuel(quarry))
            {
                DirectMessage(player, "This machine requires fuel to operate.");
                quarry.SetOn(false);
            }
        }

        void OnQuarryConsumeFuel(MiningQuarry quarry, Item item)
        {
            if (quarry.OwnerID == 0)
            {
                return;
            }

            item.amount -= (int)item.parent.entityOwner.GetParentEntity().skinID - 1;

            if (item.amount > 0)
            {
                item.MarkDirty();
                return;
            }

            item.amount = 0;
            item.Remove(0f);
        }

        void OnEntitySpawned(MiningQuarry quarry)
        {
            quarry.fuelStoragePrefab.instance.GetComponent<StorageContainer>().OwnerID = quarry.OwnerID;
            quarry.hopperPrefab.instance.GetComponent<StorageContainer>().OwnerID = quarry.OwnerID;

            if (quarry.skinID == 0)
            {
                quarry.skinID = 1;
            }

            var level = (int)quarry.skinID;
            var container = quarry.hopperPrefab.instance.GetComponent<StorageContainer>();
            var pumpjack = quarry.ShortPrefabName.Contains("pumpjack");

            if (level != 1)
            {
                container.inventory.capacity = 16 + (5 * level);
                container.inventory.MarkDirty();

                quarry.workToAdd = (pumpjack ? 10 * level : 7.5f * level);
            }

            List<string> Resources = new List<string>();

            foreach (var res in quarry._linkedDeposit._resources)
            {
                Resources.Add(res.type.shortname);
            }

            if (pumpjack && !Resources.Contains("crude.oil"))
            {
                quarry._linkedDeposit.Add(ItemManager.FindItemDefinition("crude.oil"), 1f, 1000, 10f, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, true);
            }

            if (level >= 3 && !Resources.Contains("metal.ore"))
            {
                quarry._linkedDeposit.Add(ItemManager.FindItemDefinition("metal.ore"), 1f, 1000, options.QuarryOptions.Metal_Production, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
            }

            if (level >= 4 && !Resources.Contains("sulfur.ore"))
            {
                quarry._linkedDeposit.Add(ItemManager.FindItemDefinition("sulfur.ore"), 1f, 1000, options.QuarryOptions.Sulfur_Production, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
            }

            if (level >= 5 && !Resources.Contains("hq.metal.ore"))
            {
                quarry._linkedDeposit.Add(ItemManager.FindItemDefinition("hq.metal.ore"), 1f, 1000, options.QuarryOptions.HQM_Production, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
            }

            Resources.Clear();
        }

        void OnResourceDepositCreatedWIP(ResourceDepositManager.ResourceDeposit resourceDeposit)
        {
            if (random.Next(0, 100) > options.SurveySettings.OilCraterChance)
            {
                return;
            }

            List<SurveyCrater> SurveyCraters = new List<SurveyCrater>();
            Vis.Entities<SurveyCrater>(resourceDeposit.origin, 10, SurveyCraters);

            if (SurveyCraters.Count == 0)
            {
                return;
            }

            var surveyCrater = SurveyCraters[0];

            if (surveyCrater == null)
            {
                return;
            }

            GameManager.server.CreateEntity("assets/prefabs/tools/surveycharge/survey_crater_oil.prefab", surveyCrater.transform.position, surveyCrater.transform.rotation)?.Spawn();
            surveyCrater.Kill();

            resourceDeposit._resources.Clear();
            resourceDeposit.Add(ItemManager.FindItemDefinition("crude.oil"), 1f, 1000, 10f, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, true);
        }
        #endregion

        #region Functions
        bool HasFuel(MiningQuarry quarry)
        {
            var item = quarry.fuelStoragePrefab.instance.GetComponent<StorageContainer>().inventory.FindItemsByItemName("lowgradefuel");

            return item != null && item.amount >= (int)quarry.skinID;
        }

        bool CanAfford(BasePlayer player, bool pumpjack)
        {
            var item = player.inventory.containerMain.FindItemsByItemName(pumpjack ? "mining.pumpjack" : "mining.quarry");

            if (item == null)
            {
                item = player.inventory.containerBelt.FindItemsByItemName(pumpjack ? "mining.pumpjack" : "mining.quarry");
            }

            if (item == null || item.amount < 1)
            {
                return false;
            }

            if (item.skin != 0ul)
            {
                return false;
            }

            item.UseItem(1);
            return true;
        }

        void DirectMessage(BasePlayer player, string message)
        {
            player.SendConsoleCommand("chat.add", 0, "76561198070759528", message);
        }

        #region UI
        void CreateGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "upgradebutton");

            CuiElementContainer u = UI.Container("upgradebutton", UI.Color("000000", 0), options.Button.ButtonBounds);
            UI.Button(ref u, "upgradebutton", UI.Color(options.Button.ButtonColor, options.Button.ButtonOpacity), $"<color={options.Button.ButtonFontColor}>Upgrade</color>", 10, new UI4(0, 0, 1, 1f), "ALSd01LASKDkaK2Qlasdka(1Kaklsdja2");

            CuiHelper.AddUi(player, u);
        }

        [ConsoleCommand("ALSd01LASKDkaK2Qlasdka(1Kaklsdja2")]
        void CreateConfirmation(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), "upgradebutton");
            CuiHelper.DestroyUi(arg.Player(), "upgradeconfirm");

            var machine = ActiveGUI[arg.Player()];
            var baseMachine = machine.GetComponent<BaseEntity>();

            var pumpjack = baseMachine.ShortPrefabName.Contains("pumpjack");
            var capacity = machine.hopperPrefab.instance.GetComponent<StorageContainer>().inventory.capacity;

            var level = (int)baseMachine.skinID;

            CuiElementContainer u = UI.Container("upgradeconfirm", UI.Color(options.Panel.PanelColor, options.Panel.PanelOpacity), options.Panel.PanelBounds);

            UI.Label(ref u, "upgradeconfirm", $"<color={options.Panel.PanelFontColor}>{(pumpjack ? "Pumpjack Manager" : "Quarry Manager")}</color>", 13, new UI4(0, 0.8f, 1, 1f), TextAnchor.MiddleCenter);
            UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(0.43f, 0.76f, 0.57f, 0.84f));
            UI.Label_Lower(ref u, "upgradeconfirm", $"<color={options.Panel.PanelFontColor}>Level {level}/{(pumpjack ? options.QuarrySettings.PumpjackMaxLevel : options.QuarrySettings.QuarryMaxLevel)}</color>", 8, new UI4(0, 0.75f, 1, 0.85f), TextAnchor.MiddleCenter);

            UI.Label(ref u, "upgradeconfirm", $"<color={options.Panel.PanelFontColor}>Current Level</color>", 9, new UI4(0.17f, 0.63f, 1, 0.73f), TextAnchor.MiddleLeft);
            UI.Label_Lower(ref u, "upgradeconfirm", $"<color={options.Panel.PanelFontColor}>Production:\nProcess Rate:\nCapacity:\nFuel Consumption:</color>", 8, new UI4(0.1f, 0.34f, 1, 0.64f), TextAnchor.MiddleLeft);
            UI.Label(ref u, "upgradeconfirm", $"<color={options.Panel.PanelFontColor}>{machine.workToAdd}\n{machine.processRate}\n{capacity}\n{level}</color>", 8, new UI4(0.1f, 0.34f, 0.4f, 0.64f), TextAnchor.MiddleRight);

            UI.Panel(ref u, "upgradeconfirm", UI.Color("#e8ddd4", 0.4f), new UI4(0.501f, 0.21f, 0.501f, 0.66f));
            UI.Image(ref u, "upgradeconfirm", GetImage($"{(pumpjack ? "mining.pumpjack" : "mining.quarry")}", 0ul), new UI4(0.44f, 0.37f, 0.56f, 0.57f));

            UI.Button(ref u, "upgradeconfirm", UI.Color("FF0000", 0.55f), $"<color={options.Panel.PanelFontColor}>Cancel</color>", 9, new UI4(0.502f, 0f, 0.997f, 0.13f), "$19%(!*aslLKAK123(!@*AKJSK!(49128!(@#!@*#$%!");
            UI.Button(ref u, "upgradeconfirm", UI.Color($"{(level >= (pumpjack ? options.QuarrySettings.PumpjackMaxLevel : options.QuarrySettings.QuarryMaxLevel) ? "FF0000" : "008000")}", 0.55f), $"<color={options.Panel.PanelFontColor}>{(level >= (pumpjack ? options.QuarrySettings.PumpjackMaxLevel : options.QuarrySettings.QuarryMaxLevel) ? "Max Level" : "Upgrade")}</color>", 9, new UI4(0f, 0f, 0.499f, 0.13f), $"{(level >= (pumpjack ? options.QuarrySettings.PumpjackMaxLevel : options.QuarrySettings.QuarryMaxLevel) ? string.Empty : "gLx$_+!)@laKS4391LAKS1291@$(!RKQSMDIO!@@")}");

            if (level < (pumpjack ? options.QuarrySettings.PumpjackMaxLevel : options.QuarrySettings.QuarryMaxLevel))
            {
                UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>Next Level</color>", 9, new UI4(0f, 0.64f, 0.81f, 0.74f), TextAnchor.MiddleRight);
                UI.Label_Lower(ref u, "upgradeconfirm", $"<color=#e8ddd4>Production:\nProcess Rate:\nCapacity:\nFuel Consumption:</color>", 8, new UI4(0.6f, 0.34f, 1, 0.64f), TextAnchor.MiddleLeft);
                UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>{machine.workToAdd + (pumpjack ? 10f : 7.5f)}\n{machine.processRate}\n{capacity + 2}\n{level + 1}</color>", 8, new UI4(0.6f, 0.34f, 0.9f, 0.64f), TextAnchor.MiddleRight);
            }

            List<string> Resources = new List<string>();

            var offsetL = 0.1f;
            var offsetR = 0.16f;

            var offsetRL = 0.6f;
            var offsetRR = 0.66f;

            if (pumpjack)
            {
                UI.Image(ref u, "upgradeconfirm", GetImage("crude.oil", 0ul), new UI4(offsetL, 0.21f, offsetR, 0.35f));
                UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetL, 0.16f, offsetR, 0.22f));
                UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>{level}</color>", 6, new UI4(offsetL, 0.14f, offsetR, 0.24f), TextAnchor.MiddleCenter);

                if (level != options.QuarrySettings.PumpjackMaxLevel)
                {
                    UI.Image(ref u, "upgradeconfirm", GetImage("crude.oil", 0ul), new UI4(offsetRL, 0.21f, offsetRR, 0.35f));
                    UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetRL, 0.16f, offsetRR, 0.22f));
                    UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>{1 + level}</color>", 6, new UI4(offsetRL, 0.14f, offsetRR, 0.24f), TextAnchor.MiddleCenter);
                }
            }
            else
            {
                foreach (var res in machine._linkedDeposit._resources)
                {
                    UI.Image(ref u, "upgradeconfirm", GetImage(res.type.shortname, 0ul), new UI4(offsetL, 0.21f, offsetR, 0.35f));
                    UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetL, 0.16f, offsetR, 0.22f));
                    UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>{(6 * (7.5f / res.workNeeded) * level).ToString("0.0")}</color>", 6, new UI4(offsetL, 0.14f, offsetR, 0.24f), TextAnchor.MiddleCenter);

                    offsetL += 0.07f;
                    offsetR += 0.07f;

                    if (level != options.QuarrySettings.QuarryMaxLevel)
                    {
                        UI.Image(ref u, "upgradeconfirm", GetImage(res.type.shortname, 0ul), new UI4(offsetRL, 0.21f, offsetRR, 0.35f));
                        UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetRL, 0.16f, offsetRR, 0.22f));
                        UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>{(6 * (7.5f / res.workNeeded) * (level + 1)).ToString("0.0")}</color>", 6, new UI4(offsetRL, 0.14f, offsetRR, 0.24f), TextAnchor.MiddleCenter);

                        offsetRL += 0.07f;
                        offsetRR += 0.07f;
                    }

                    Resources.Add(res.type.shortname);
                }

                if (level >= 2 && !Resources.Contains("metal.ore"))
                {
                    UI.Image(ref u, "upgradeconfirm", GetImage("metal.ore", 0ul), new UI4(offsetRL, 0.21f, offsetRR, 0.35f));
                    UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetRL, 0.16f, offsetRR, 0.22f));
                    UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>??</color>", 6, new UI4(offsetRL, 0.14f, offsetRR, 0.24f), TextAnchor.MiddleCenter);

                    offsetRL += 0.07f;
                    offsetRR += 0.07f;
                }

                if (level >= 3 && !Resources.Contains("sulfur.ore"))
                {
                    UI.Image(ref u, "upgradeconfirm", GetImage("sulfur.ore", 0ul), new UI4(offsetRL, 0.21f, offsetRR, 0.35f));
                    UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetRL, 0.16f, offsetRR, 0.22f));
                    UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>??</color>", 6, new UI4(offsetRL, 0.14f, offsetRR, 0.24f), TextAnchor.MiddleCenter);

                    offsetRL += 0.07f;
                    offsetRR += 0.07f;
                }

                if (level >= 4 && !Resources.Contains("hq.metal.ore"))
                {
                    UI.Image(ref u, "upgradeconfirm", GetImage("hq.metal.ore", 0ul), new UI4(offsetRL, 0.21f, offsetRR, 0.35f));
                    UI.Panel(ref u, "upgradeconfirm", UI.Color("000000", 0.6f), new UI4(offsetRL, 0.16f, offsetRR, 0.22f));
                    UI.Label(ref u, "upgradeconfirm", $"<color=#e8ddd4>??</color>", 6, new UI4(offsetRL, 0.14f, offsetRR, 0.24f), TextAnchor.MiddleCenter);
                }
            }

            Resources.Clear();
            CuiHelper.AddUi(arg.Player(), u);
        }

        void DestroyCUI(BasePlayer player)
        {
            ActiveGUI.Remove(player);

            CuiHelper.DestroyUi(player, "upgradebutton");
            CuiHelper.DestroyUi(player, "upgradeconfirm");
        }
        #endregion

        #region Commands
        [ConsoleCommand("ql")]
        void ConfigCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null || arg.Args == null)
            {
                return;
            }

            // Went ahead and used a switch to easily add more commands in the future.
            switch (arg.Args[0])
            {
                case "reload":
                    LoadConfig();
                    arg.ReplyWith("Config file has been reloaded.");
                    break;

                default:
                    arg.ReplyWith("Not a valid command.");
                    break;
            }
        }
        #endregion

        #region CallBacks
        [ConsoleCommand("$19%(!*aslLKAK123(!@*AKJSK!(49128!(@#!@*#$%!")]
        void CloseConfirm(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), "upgradeconfirm");
            CreateGUI(arg.Player());
        }

        [ConsoleCommand("gLx$_+!)@laKS4391LAKS1291@$(!RKQSMDIO!@@")]
        void Upgrade(ConsoleSystem.Arg arg)
        {
            var machine = ActiveGUI[arg.Player()];
            var baseMachine = machine.GetComponent<BaseEntity>();

            List<string> Resources = new List<string>();
            var pumpjack = baseMachine.ShortPrefabName.Contains("pumpjack");

            if (Economics && options.QuarrySettings.EnableEconomics)
            {
                if (!Economics.Call<bool>("Withdraw", arg.Player().userID, options.QuarrySettings.EconomicsCost))
                {
                    DirectMessage(arg.Player(), $"You cannot afford this upgrade. Requires <color=#add8e6ff>{options.QuarrySettings.EconomicsCost} {options.QuarrySettings.EconomicsCurrency}</color>!");
                    return;
                }
            }
            else
            {
                if (!CanAfford(arg.Player(), pumpjack))
                {
                    DirectMessage(arg.Player(), $"You cannot afford this upgrade. Requires 1 <color=#add8e6ff>[{(pumpjack ? "Pumpjack" : "Mining Quarry")}]</color>!");
                    return;
                }
            }

            var output = machine.hopperPrefab.instance.GetComponent<StorageContainer>();
            var level = (int)baseMachine.skinID;

            baseMachine.skinID += 1;
            output.inventory.capacity += 5;
            machine.workToAdd += pumpjack ? 10f : 7.5f;

            foreach (var res in machine._linkedDeposit._resources)
            {
                Resources.Add(res.type.shortname);
            }

            if (level >= 3 && !Resources.Contains("metal.ore"))
            {
                machine._linkedDeposit.Add(ItemManager.FindItemDefinition("metal.ore"), 1f, 1000, options.QuarryOptions.Metal_Production, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
            }

            if (level >= 4 && !Resources.Contains("sulfur.ore"))
            {
                machine._linkedDeposit.Add(ItemManager.FindItemDefinition("sulfur.ore"), 1f, 1000, options.QuarryOptions.Sulfur_Production, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
            }

            if (level >= 5 && !Resources.Contains("hq.metal.ore"))
            {
                machine._linkedDeposit.Add(ItemManager.FindItemDefinition("hq.metal.ore"), 1f, 1000, options.QuarryOptions.HQM_Production, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
            }

            Effect.server.Run("assets/bundled/prefabs/fx/build/promote_metal.prefab", output.transform.position);
            Resources.Clear();

            if (output.IsOpen())
            {
                arg.Player().EndLooting();
                output.PlayerOpenLoot(arg.Player(), string.Empty, true);
                return;
            }

            CreateGUI(arg.Player());
            CuiHelper.DestroyUi(arg.Player(), "upgradeconfirm");
        }
        #endregion

        #region Config
        ConfigFile options;

        class ConfigFile
        {
            public SurveyConfig SurveySettings = new SurveyConfig();
            public PlayerConfig PlayerSettings = new PlayerConfig();
            public QuarryLevelOptions QuarrySettings = new QuarryLevelOptions();
            public QuarryProduction QuarryOptions = new QuarryProduction();
            public ButtonConfig Button = new ButtonConfig();
            public PanelConfig Panel = new PanelConfig();
        }

        class PlayerConfig
        {
            public bool PreventUnauthorizedToggling = false;
            public bool PreventUnauthorizedLooting = false;
        }

        class SurveyConfig
        {
            public bool EnableOilCraters = false;
            public int OilCraterChance = 10;
        }

        class QuarryLevelOptions
        {
            public int QuarryMaxLevel = 5;
            public int PumpjackMaxLevel = 5;
            public bool EnableEconomics = false;
            public double EconomicsCost = 5000;
            public string EconomicsCurrency = "credits";
        }

        class QuarryProduction
        {
            public float Metal_Production = 4f;
            public float Sulfur_Production = 4f;
            public float HQM_Production = 50f;
        }

        class ButtonConfig
        {
            public UI4 ButtonBounds = new UI4(0.648f, 0.115f, 0.72f, 0.143f);
            public string ButtonColor = "FFFFF3";
            public float ButtonOpacity = 0.160f;
            public string ButtonFontColor = "#f7ebe1";
        }

        class PanelConfig
        {
            public UI4 PanelBounds = new UI4(0.39f, 0.55f, 0.61f, 0.75f);
            public string PanelColor = "FFFFF3";
            public float PanelOpacity = 0.160f;
            public string PanelFontColor = "#e8ddd4";
        }

        void LoadDefaultConfig()
        {
            var config = new ConfigFile();

            SaveConfig(config);
        }

        void LoadConfig()
        {
            options = Config.ReadObject<ConfigFile>();
            SaveConfig(options);
        }

        void SaveConfig(ConfigFile config)
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #region API
        private string GetImage(string imageName, ulong skinid)
        {
            object success = ImageLibrary.Call("GetImage", imageName, skinid);

            if (success is string)
            {
                return (string)success;
            }

            return string.Empty;
        }
        #endregion

        #region UI - Frame Work
        public static class UI
        {
            static public CuiElementContainer Container(string panel, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }

            static public void Panel(ref CuiElementContainer container, string panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                panel);
            }

            static public void Label(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);

            }

            static public void Label_Lower(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text, Font = "robotocondensed-regular.ttf" },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);

            }

            static public void Button(ref CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align, Font = "robotocondensed-regular.ttf" }
                },
                panel);
            }

            static public void Image(ref CuiElementContainer container, string panel, string png, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        public class UI4
        {
            public float xMin, yMin, xMax, yMax;
            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }
            public string GetMin() => $"{xMin} {yMin}";
            public string GetMax() => $"{xMax} {yMax}";
        }
        #endregion

        #endregion
    }
}