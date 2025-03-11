using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SetupFurnaces", "Sempai", "1.1.0")]
    internal class SetupFurnaces : RustPlugin
    {
        #region Static

        private const string Layer = "UI_SetupFurnaces";
        private const string perm = "setupfurnaces.use";
        private Configuration _config;
        private Dictionary<string, FurnaceDefenition> furnacesSlots = new Dictionary<string, FurnaceDefenition>
        {
            ["campfire"] = new FurnaceDefenition()
            {
                InputType = "raw",
                OutputAmount = 2,
                SlotsType = new List<SlotType>
                {
                    SlotType.FUEL,
                    SlotType.INPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                }
            },
            ["bbq.deployed"] = new FurnaceDefenition()
            {
                InputType = "raw",
                OutputAmount = 8,
                SlotsType = new List<SlotType>
                {
                    SlotType.FUEL,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                }
            },
            ["furnace"] = new FurnaceDefenition()
            {
                InputType = "ore",
                OutputAmount = 3,
                SlotsType = new List<SlotType>
                {
                    SlotType.FUEL,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                }
            },
            ["furnace.large"] = new FurnaceDefenition()
            {
                OutputAmount = 15,
                InputType = "ore",
                SlotsType = new List<SlotType>
                {
                    SlotType.FUEL,
                    SlotType.FUEL,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.INPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                }
            },
            ["refinery_small_deployed"] = new FurnaceDefenition()
            {
                OutputAmount = 3,
                InputType = "crude",
                SlotsType = new List<SlotType>
                {
                    SlotType.FUEL,
                    SlotType.INPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                    SlotType.OUTPUT,
                }
            }
        };

        private enum SlotType : byte
        {
            FUEL,
            INPUT,
            OUTPUT
        }

        private class FurnaceDefenition
        {
            public string InputType;
            public float OutputAmount;
            public List<SlotType> SlotsType = new List<SlotType>();
        }

        private class Configuration
        {
            [JsonProperty(PropertyName = "Oven setup for players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, SetupOven> playersOvenSetup = new Dictionary<string, SetupOven>()
            {
                ["setupfurnaces.default"] = new SetupOven
                {
                    quickSmelt = 1,
                    stopBurn = new Dictionary<string, bool>()
                    {
                        ["furnace"] = true,
                        ["furnace.large"] = false,
                        ["refinery_small_deployed"] = false,
                        ["campfire"] = false,
                        ["bbq.deployed"] = false
                    },
                    quickSmelting = new Dictionary<string, bool>
                    {
                        ["furnace"] = true,
                        ["furnace.large"] = false,
                        ["refinery_small_deployed"] = false,
                        ["campfire"] = false,
                        ["bbq.deployed"] = false
                    },
                },
                ["setupfurnaces.vip"] = new SetupOven
                {
                    quickSmelt = 1,
                    stopBurn = new Dictionary<string, bool>()
                    {
                        ["furnace"] = true,
                        ["furnace.large"] = true,
                        ["refinery_small_deployed"] = true,
                        ["campfire"] = true,
                        ["bbq.deployed"] = true
                    },
                    quickSmelting = new Dictionary<string, bool>
                    {
                        ["furnace"] = true,
                        ["furnace.large"] = true,
                        ["refinery_small_deployed"] = true,
                        ["campfire"] = true,
                        ["bbq.deployed"] = true
                    },
                },
            };
        }

        private class SetupOven
        {
            [JsonProperty(PropertyName = "Quick smelt value")]
            public int quickSmelt = 1;

            public Dictionary<string, bool> stopBurn = new Dictionary<string, bool>
            {
                ["furnace"] = true,
                ["furnace.large"] = true,
                ["refinery_small_deployed"] = true,
                ["campfire"] = true,
                ["bbq.deployed"] = true
            };

            public Dictionary<string, bool> quickSmelting = new Dictionary<string, bool>
            {
                ["furnace"] = true,
                ["furnace.large"] = true,
                ["refinery_small_deployed"] = true,
                ["campfire"] = true,
                ["bbq.deployed"] = true
            };
        }
        
        #region Config

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

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #endregion

        #region OxideHooks

        private void OnServerInitialized()
        {
            permission.RegisterPermission(perm, this);
            foreach (var check in _config.playersOvenSetup) permission.RegisterPermission(check.Key, this);
        }

        private void Unload()
        {
            foreach (var check in BasePlayer.activePlayerList) CuiHelper.DestroyUi(check, Layer + ".bg");
        }


        private void OnOvenCook(BaseOven oven, Item item, BaseEntity slot)
        {
            if (oven == null) return;
            
            string playerPermission = "";
            foreach (var check in _config.playersOvenSetup)
                if (permission.UserHasPermission(oven.OwnerID.ToString(), check.Key)) playerPermission = check.Key;
            
            bool isOn;
            if (string.IsNullOrEmpty(playerPermission) || !_config.playersOvenSetup[playerPermission].stopBurn.TryGetValue(oven.ShortPrefabName, out isOn) || !isOn) return;

            var curOvenInputType = furnacesSlots[oven.ShortPrefabName].InputType;
            foreach (var check in oven.inventory.itemList)
                if (check.info.displayName.english.ToLower().Contains(curOvenInputType))
                    return;

            NextTick(oven.StopCooking);
        }

        private void OnLootEntity(BasePlayer player, BaseOven oven)
        {
            if (player == null || oven == null || !furnacesSlots.ContainsKey(oven.ShortPrefabName)) return;
            var curOven = furnacesSlots[oven.ShortPrefabName];
            var curOvenInput = curOven.InputType;
            var curOvenOutputAmount = curOven.OutputAmount;
            var ovenInv = oven.inventory;
            var ovenItemList = ovenInv.itemList;
            var playerContainerMain = player.inventory.containerMain;
            var playerContainerBelt = player.inventory.containerBelt;

            // COLLECT
            foreach (var check in ovenItemList.ToArray())
            {
                if (curOven.SlotsType[check.position] == SlotType.OUTPUT)
                    check.MoveToContainer(check.CanMoveTo(player.inventory.containerMain) ? player.inventory.containerMain : player.inventory.containerBelt);
            }    
            
            // SORT INPUT
            foreach (var check in ovenItemList.ToArray())
            {
               if (curOven.SlotsType[check.position] == SlotType.INPUT)
                    check.MoveToContainer(ovenInv);    
            }

            // ADD FUEL
            foreach (var check in playerContainerMain.itemList.ToArray())
                if (check.info.shortname == "wood")
                    check.MoveToContainer(ovenInv);
            foreach (var check in playerContainerBelt.itemList.ToArray())
                if (check.info.shortname == "wood")
                    check.MoveToContainer(ovenInv);

            // ADD INPUT
            foreach (var check in playerContainerMain.itemList.ToArray())
                if (check.info.displayName.english.ToLower().Contains(curOvenInput)) 
                    check.MoveToContainer(ovenInv);    
            foreach (var check in playerContainerBelt.itemList.ToArray())
                if (check.info.displayName.english.ToLower().Contains(curOvenInput)) 
                    check.MoveToContainer(ovenInv);
            
            // REMOVE SURPLUS
            float wood = 0;
            foreach (var check in ovenInv.itemList.ToArray())
                if (check.info.shortname == "wood") wood += check.amount;

            foreach (var check in ovenItemList.ToArray())
            {
                if (!check.info.displayName.english.ToLower().Contains(curOvenInput)) continue;
                if ((int) wood <= 1 && check.amount > 1)
                {
                    player.GiveItem(ItemManager.CreateByName(check.info.shortname, check.amount));
                    Take(ovenInv.itemList, check.info.shortname, check.amount);
                    continue;
                }
                int needFuel;
                switch (check.info.shortname)
                {
                    case "sulfur.ore":
                        needFuel = 2;
                        break;
                    case "metal.ore":
                        needFuel = 5;
                        break;
                    case "hq.metal.ore":
                        needFuel = 10;
                        break;
                    case "crude.oil":
                        needFuel = 6;
                        break;
                    default:
                        needFuel = 3;
                        break;
                }

                var amountResourcesRemain = wood / needFuel * curOvenOutputAmount;
                wood -=  check.amount / curOvenOutputAmount * needFuel;
                
                if ((int) wood >= 0 && check.amount - amountResourcesRemain <= 1) continue;
                player.GiveItem(ItemManager.CreateByName(check.info.shortname, (int) (check.amount - amountResourcesRemain)));
                Take(ovenInv.itemList, check.info.shortname, (int) (check.amount - amountResourcesRemain));
            }
            foreach (var check in ovenItemList.ToArray())
            {
                if (curOven.SlotsType[check.position] == SlotType.INPUT)
                    check.MoveToContainer(ovenInv);    
            }
            if ((int)wood > 1)
            {
                player.GiveItem(ItemManager.CreateByName("wood", (int)wood));
                Take(ovenInv.itemList, "wood", (int)wood);
            }
            
            if (!oven.HasFlag(BaseEntity.Flags.On))
                NextTick(()=>
                {
                    string playerPermission = "";
                    foreach (var check in _config.playersOvenSetup)
                        if (permission.UserHasPermission(oven.OwnerID.ToString(), check.Key)) playerPermission = check.Key;
                    
                    bool isOn;
                    var setupOven = _config.playersOvenSetup[playerPermission];
                    if (player == null || string.IsNullOrEmpty(playerPermission) || !setupOven.quickSmelting.TryGetValue(oven.ShortPrefabName, out isOn) || !isOn) return;
                    if (oven.IsDestroyed) return;
                    var speed = 0.5f / setupOven.quickSmelt;
                    oven.CancelInvoke(oven.Cook);
                    oven.inventory.temperature = oven.cookingTemperature;
                    oven.UpdateAttachmentTemperature();
                    oven.InvokeRepeating(oven.Cook, speed, speed);
                    oven.SetFlag(BaseEntity.Flags.On, true);
                });
        }


        #endregion      

        #region Commands

        [ChatCommand("fsetup")]
        private void cmdChatmenu(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                SendReply(player, "You haven't permission for use this command");
                return;
            }

            if (args.Length != 0)
            {
                SendReply(player, "Use command without args - <color=yellow>/fsetup</color>");
                return;
            }
            
            ShowUIBG(player);
        }

        [ConsoleCommand("UI_SF")]
        private void cmdConsole(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs()) return;
            var player = arg.Player();
            switch (arg.GetString(0))
            {
                case "SMELTSPEED":
                    _config.playersOvenSetup[arg.GetString(1)].quickSmelt = arg.GetInt(2) < 1 ? 1 : arg.GetInt(2);
                    break;
                case "STOPBURNING":
                    _config.playersOvenSetup[arg.GetString(1)].stopBurn[arg.GetString(2)] = !_config.playersOvenSetup[arg.GetString(1)].stopBurn[arg.GetString(2)];
                    break;
                case "QUICKSMELT":
                    _config.playersOvenSetup[arg.GetString(1)].quickSmelting[arg.GetString(2)] = !_config.playersOvenSetup[arg.GetString(1)].quickSmelting[arg.GetString(2)];
                    break;
                case "BACK":
                    ShowUISelectPERM(player);
                    return;
            }
            ShowUISetupFurnaces(player, arg.GetString(1));
        }

        #endregion

        #region Functions

        private void Take(IEnumerable<Item> itemList, string shortname, int iAmount)
        {
            var num1 = 0;
            if (iAmount == 0) return;
            var list = Facepunch.Pool.GetList<Item>();
            foreach (var obj in itemList)
            {
                if (obj.info.shortname != shortname) continue;
                var num2 = iAmount - num1;
                if (num2 <= 0) continue;
                if (obj.amount > num2)
                {
                    obj.MarkDirty();
                    obj.amount -= num2;
                    break;
                }

                if (obj.amount <= num2)
                {
                    num1 += obj.amount;
                    list.Add(obj);
                }

                if (num1 == iAmount) break;
            }

            foreach (var obj in list) obj.RemoveFromContainer();
            Facepunch.Pool.FreeList(ref list);
        }

        #endregion

        #region UI
        
        private void ShowUISetupFurnaces(BasePlayer player, string fperm)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Parent = Layer + ".bg",
                Name = Layer,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                        OffsetMin = "-250 -150", OffsetMax = "250 150"
                    },
                    new CuiImageComponent
                    {
                        Color = "0.15 0.15 0.15 0.9",
                        Material = "assets/content/ui/binocular_overlay.mat",
                    },
                },
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".label",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1", AnchorMax = "1 1",
                        OffsetMin = "0 -30", OffsetMax = "0 0"
                    },
                    new CuiTextComponent
                    {
                        Text = "SETUP FURNACES",
                        FontSize = 20, Color = "1 1 0 1",
                        Align = TextAnchor.MiddleCenter, Font = "permanentmarker.ttf",
                    },
                    new CuiOutlineComponent { Distance = "-0.5 -0.5", Color = "0 0 0 1"},
                },
            });

            var ovenSetup = _config.playersOvenSetup[fperm];
            Dictionary<string, string> dicOvens = new Dictionary<string, string>
            {
                ["furnace"] = "Furnace",
                ["furnace.large"] = "Large Furnace",
                ["refinery_small_deployed"] = "Refinery",
                ["campfire"] = "Campfire",
                ["bbq.deployed"] = "Barbeque"
            };
            
            var posY = -85;
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".label",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05 1", AnchorMax = "0.45 1",
                        OffsetMin = "0 -60", OffsetMax = "0 -30"
                    },
                    new CuiTextComponent
                    {
                        Text = "QUICK SMELT",
                        FontSize = 18, Color = "0.00 0.87 0.95 1.00",
                        Align = TextAnchor.MiddleCenter, Font = "permanentmarker.ttf",
                    },
                    new CuiOutlineComponent { Distance = "-0.5 -0.5", Color = "0 0 0 1"},
                },
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".label",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.55 1", AnchorMax = "0.95 1",
                        OffsetMin = "0 -60", OffsetMax = "0 -30"
                    },
                    new CuiTextComponent
                    {
                        Text = "STOP BURN",
                        FontSize = 18, Color = "0.00 0.87 0.95 1.00",
                        Align = TextAnchor.MiddleCenter, Font = "permanentmarker.ttf",
                    },
                    new CuiOutlineComponent { Distance = "-0.5 -0.5", Color = "0 0 0 1"},
                },
            });
            
            // QUICK SMELT
            foreach (var check in dicOvens)
            {
                  container.Add(new CuiButton
                  {
                      RectTransform =
                      {
                          AnchorMin = "0.05 1", AnchorMax = "0.45 1",
                          OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 25}"
                      },
                      Text =
                      {
                          Text = check.Value + $":   {(ovenSetup.quickSmelting[check.Key] ? "<color=green>ON</color>" : "<color=red>OFF</color>")}",
                          FontSize = 16, Color = "1 1 1 1",
                          Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                      },
                      Button =
                      {
                          Command = $"UI_SF QUICKSMELT {fperm} {check.Key}",
                          Color = "0 0 0 0",
                      },
                  }, Layer, Layer + ".button");

                  posY -= 25;
            }
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".label",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05 1", AnchorMax = "0.45 1",
                        OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 25}"
                    },
                    new CuiTextComponent
                    {
                        Text = "Quick smelt speed:",
                        FontSize = 16, Color = "1 1 1 1",
                        Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf",
                    },
                    new CuiOutlineComponent { Distance = "-0.5 -0.5", Color = "0 0 0 1"},
                },
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".panelInp",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.45 1", AnchorMax = "0.45 1",
                        OffsetMin = $"-50 {posY}", OffsetMax = $"0 {posY + 25}"
                    },
                    new CuiImageComponent
                    {
                        Color = "0.33 0.33 0.33 1",
                        Material = "assets/icons/iconmaterial.mat",
                    },
                },
            });
            container.Add(new CuiElement
            {
                Parent = Layer + ".panelInp",
                Name = Layer + ".input",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1",
                    },
                    new CuiInputFieldComponent
                    {
                        Text = ovenSetup.quickSmelt.ToString(),
                        Command = $"UI_SF SMELTSPEED {fperm}", CharsLimit = 3,
                        FontSize = 16, Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    },
                }
            });

            posY = -85;
            // STOP BURN
            foreach (var check in dicOvens)
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.55 1", AnchorMax = "0.95 1",
                        OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 25}"
                    },
                    Text =
                    {
                        Text = check.Value + $":   {(ovenSetup.stopBurn[check.Key] ? "<color=green>ON</color>" : "<color=red>OFF</color>")}",
                        FontSize = 16, Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    },
                    Button =
                    {
                        Command = $"UI_SF QUICKSMELT {fperm} {check.Key}",
                        Color = "0 0 0 0",
                    },
                }, Layer, Layer + ".button");

                posY -= 25;
            }
            
            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0 1", AnchorMax = "0 1",
                    OffsetMin = "-30 -30", OffsetMax = "0 0"
                },
                Text =
                {
                    Text = "<",
                    FontSize = 20, Color = "1 1 1 1",
                    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                },
                Button =
                {
                    Command = "UI_SF BACK",
                    Color = "0.2 0.2 0.2 0.85",
                },
            }, Layer, Layer + ".button");
        
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void ShowUISelectPERM(BasePlayer player)
        {
            var container = new CuiElementContainer();

            var ovenSetupList = _config.playersOvenSetup;
            var ovenSetupListCount = ovenSetupList.Count;
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".bg",
                Name = Layer,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                        OffsetMin = $"-125 {ovenSetupListCount * -25}", OffsetMax = $"125 {ovenSetupListCount * 25}"
                    },
                    new CuiImageComponent
                    {
                        Color = "0.15 0.15 0.15 0.9",
                        Material = "assets/content/ui/binocular_overlay.mat",
                    },
                },
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".label",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1", AnchorMax = "1 1",
                        OffsetMin = "0 -30", OffsetMax = "0 0"
                    },
                    new CuiTextComponent
                    {
                        Text = "SELECT PERMISSION",
                        FontSize = 20, Color = "1 1 0 1",
                        Align = TextAnchor.MiddleCenter, Font = "permanentmarker.ttf",
                    },
                    new CuiOutlineComponent { Distance = "-0.5 -0.5", Color = "0 0 0 1"},
                },
            });

            var posY = -55;
            foreach (var check in ovenSetupList)
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "1 1",
                        OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 25}"
                    },
                    Text =
                    {
                        Text = check.Key,
                        FontSize = 16, Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    },
                    Button =
                    {
                        Command = $"UI_SF SELECTPERM {check.Key}",
                        Color = "0 0 0 0",
                    },
                }, Layer, Layer + ".button");
                posY -= 25;
            }
        
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }        
        
        private void ShowUIBG(BasePlayer player)
        {
            var container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                KeyboardEnabled = true,
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.92", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Overlay", Layer + ".bg");
            
            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1",
                    OffsetMin = "0 0", OffsetMax = "0 0"
                },
                Text =
                {
                    Text = "",
                    FontSize = 16, Color = "1 1 1 1",
                    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                },
                Button =
                {
                    Close = Layer + ".bg",
                    Color = "0 0 0 0",
                },
            }, Layer + ".bg", Layer + ".button");
            
            CuiHelper.DestroyUi(player, Layer + ".bg");
            CuiHelper.AddUi(player, container);

            ShowUISelectPERM(player);
        }

        #endregion
    }
}