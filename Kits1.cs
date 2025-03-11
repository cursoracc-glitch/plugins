// Name: Kits
// Documentation: https://gist.github.com/JVCVkrSzVqsfEcwJqk7N/cec76ff33a5653acd3f13418b065190e
// Changelog:
// * [1.0.0] Release
// 
// End
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits", "Orange", "1.0.1")]
    [Description("Kits with features for your server! Made by Orange#0900")]
    public class Kits1 : RustPlugin
    {
        #region Vars

        [PluginReference] private Plugin ImageLibrary;

        private void AddImage(string name, string url)
        {
            if (ImageLibrary == null)
            {
                return;
            }
            
            if (!ImageLibrary.IsLoaded)
            {
                timer.Once(1f, () =>
                {
                    AddImage(name, url);
                });
            }
            
            ImageLibrary.CallHook("AddImage", url, name, 0UL);
        }
        
        private string GetImage(string name)
        {
            return ImageLibrary?.Call<string>("GetImage", name);
        }

        private class Kit
        {
            [JsonProperty(PropertyName = "1. Name")]
            public string name;

            [JsonProperty(PropertyName = "2. Display name")]
            public string displayName;

            [JsonProperty(PropertyName = "3. Permission")]
            public string permission;

            [JsonProperty(PropertyName = "4. Cooldown")]
            public int cooldown;

            [JsonProperty(PropertyName = "5. Wipe-block time")]
            public int block;

            [JsonProperty(PropertyName = "6. Icon")]
            public string url;

            [JsonProperty(PropertyName = "7. Description")]
            public string description;

            [JsonProperty(PropertyName = "8. Max uses")]
            public int uses;

            [JsonProperty(PropertyName = "9. Give on respawn")]
            public bool auto;

            [JsonProperty(PropertyName = "Items:")]
            public List<BaseItem> items;
        }

        private class BaseItem
        {
            public string shortname;
            public int amount;
            public ulong skin;
            public string container;
            public int position;
        }

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            LoadData();
            lang.RegisterMessages(EN, this);
            permission.RegisterPermission("Kits.Unknown", this);
            cmd.AddChatCommand(config.command, this, "Command");
            cmd.AddConsoleCommand(config.command, this, "Command");
        }

        private void Loaded()
        {
            var mask = lang.GetMessage("Kit", this);
            
            foreach (var kit in config.kits)
            {
                permission.RegisterPermission(kit.permission, this);
                AddImage(kit.name, kit.url);
            }

            foreach (var item in config.kits.SelectMany(x => x.items).Distinct())
            {
                var name = item.shortname;
                AddImage(name, $"https://rustlabs.com/img/items180/{name}.png");
            }
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnNewSave()
        {
            SaveData();
        }
        
        private void OnPlayerRespawned(BasePlayer player) // TODO: Change
        {
            player.inventory.Strip();
            GiveKit(player, config.kits.FirstOrDefault(x => x.auto == true));
        }

        #endregion

        #region Config

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "1. Command")]
            public string command;

            [JsonProperty(PropertyName = "Kit list:")]
            public List<Kit> kits;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                command = "kit",
                kits = new List<Kit>
                {
                    new Kit
                    {
                        name = "starter",
                        displayName = "Starter",
                        permission = "",
                        cooldown = 600,
                        block = 0,
                        description = "Start items",
                        url = "https://i.imgur.com/IIP8QMF.png",
                        uses = 0,
                        auto = false,
                        items = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                shortname = "stonehatchet",
                                amount = 1
                            },
                            new BaseItem
                            {
                                shortname = "stone.pickaxe",
                                amount = 1
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Data

        private const string filename = "Temp/Kits/players_data";
        private PlayerData data = new PlayerData();

        private class PlayerData
        {
            public Dictionary<ulong, Dictionary<string, double>> cooldowns =
                new Dictionary<ulong, Dictionary<string, double>>();

            public Dictionary<ulong, Dictionary<string, int>> uses = new Dictionary<ulong, Dictionary<string, int>>();
        }

        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>(filename);
            }
            catch (Exception e)
            {
                PrintWarning(e.Message);
            }

            SaveData();
            timer.Every(150f, SaveData);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(filename, data);
        }

        #endregion

        #region Localization

        private Dictionary<string, string> EN = new Dictionary<string, string>
        {
            {
                "Usage", "Usage:\n" +
                         " * /kit list - Get list of all kits\n" +
                         " * /kit NAME - Get kit with name\n" +
                         " * /kit add NAME - Add new kit with name (copy your items on creation)\n" +
                         " * /kit remove NAME - Remove kit with name"
            },
            {"Permission", "You don't have permission to use that!"},
            {"Cooldown", "Cooldown for {0} seconds!"},
            {"Blocked", "Kit is blocked for {0} since wipe"},
            {"Added", "You successfully added kit '{0}' with '{1}' items"},
            {"Removed", "You successfully removed kit '{0}'"},
            {"Can't find", "Can't find kit with name '{0}'"},
            {"Kits", "Available kits:\n{0}"},
            {"Kit", " * {0}, {1}, Cooldown {2}\n"},
            {"Uses", "You already used maximal amount [{0}] of that kit!"},
            {"Available", "You can get following kits:"},
            {"GUI ON", "Available"},
            {"GUI OFF", "{0}"},
            {"Day", "d"},
            {"Hour", "h"},
            {"Minute", "m"},
            {"Second", "s"},
            {"ImageLibrary", "Image library not installed!"}
        };

        private void message(BasePlayer player, string key, params object[] args)
        {
            var message = string.Format(lang.GetMessage(key, this, player.UserIDString), args);
            player.ChatMessage(message);
        }

        #endregion

        #region Commands

        private void Command(BasePlayer player, string command, string[] args)
        {
            Command(player, args);
        }

        private void Command(ConsoleSystem.Arg arg)
        {
            Command(arg.Player(), arg.Args);
        }

        private void Command(BasePlayer player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            if (args == null || args.Length == 0)
            {
                CreatePanel(player);
                return;
            }

            var action = args[0].ToLower();
            var name = args.Length > 1 ? args[1] : "null";
            
            switch (action)
            {
                case "add":
                    AddKit(player, name);
                    break;

                case "remove":
                    RemoveKit(player, name);
                    break;
                
                case "info":
                    ShowInfo(player, name);
                    break;

                default:
                    TryGiveKit(player, action);
                    break;
            }
        }

        #endregion

        #region Core

        private List<BaseItem> GetItems(BasePlayer player)
        {
            var container = player.inventory;
            var items = new List<BaseItem>();

            foreach (var item in container.containerMain.itemList)
            {
                if (item.position < 24)
                {
                    items.Add(new BaseItem
                    {
                        shortname = item.info.shortname,
                        amount = item.amount,
                        skin = item.skin,
                        container = "Main",
                        position = item.position
                    });
                }
            }

            foreach (var item in container.containerWear.itemList)
            {
                items.Add(new BaseItem
                {
                    shortname = item.info.shortname,
                    amount = item.amount,
                    skin = item.skin,
                    container = "Wear",
                    position = item.position
                });
            }

            foreach (var item in container.containerBelt.itemList)
            {
                items.Add(new BaseItem
                {
                    shortname = item.info.shortname,
                    amount = item.amount,
                    skin = item.skin,
                    container = "Belt",
                    position = item.position
                });
            }

            return items;
        }

        private void AddKit(BasePlayer player, string name)
        {
            if (!player.IsAdmin)
            {
                message(player, "Permission");
                return;
            }

            var items = GetItems(player);

            config.kits.Add(new Kit
            {
                name = name,
                items = items,
                cooldown = 3600,
                permission = "Kits.Unknown",
                description = "Kit description",
                url = "",
                displayName = "",
                auto = false,
                block = 0,
                uses = 0
            });

            SaveConfig();
            message(player, "Added", name, items.Count);
        }

        private void RemoveKit(BasePlayer player, string name)
        {
            if (!player.IsAdmin)
            {
                message(player, "Permission");
                return;
            }

            var kit = GetKit(name);
            
            if (kit != null)
            {
                config.kits.Remove(kit);
                SaveConfig();
                message(player, "Removed", name);
            }
            else
            {
                message(player, "Can't find", name);
            }
        }

        private void TryGiveKit(BasePlayer player, string name)
        {
            timer.Once(0.2f, () => { CreateKits(player); });
            
            if (!CanUse(player))
            {
                return;
            }
            
            var kit = GetKit(name);
            if (kit == null)
            {
                message(player, "Can't find", name);
                return;
            }

            var id = player.userID;

            if (!HasPermission(id.ToString(), kit.permission))
            {
                message(player, "Permission");
                return;
            }

            var block = GetBlockTime(kit.block);
            if (block > 0)
            {
                message(player, "Block", block);
                return;
            }

            var uses = GetUses(id, kit.name);
            if (kit.uses != 0 && uses >= kit.uses)
            {
                message(player, "Uses", kit.uses);
                return;
            }

            var cooldown = GetCooldown(id, kit.name, kit.cooldown);
            if (cooldown > 0)
            {
                message(player, "Cooldown", cooldown);
                return;
            }

            data.cooldowns[id][kit.name] = Now();
            data.uses[id][kit.name]++;
            GiveKit(player, kit);
        }

        private void GiveKit(BasePlayer player, Kit kit)
        {
            foreach (var value in kit.items)
            {
                var item = ItemManager.CreateByName(value.shortname, value.amount, value.skin);
                if (item == null)
                {
                    continue;
                }

                var position = value.position;

                switch (value.container)
                {
                    case "Main":
                        item.MoveToContainer(player.inventory.containerMain, position);
                        break;
                    case "Wear":
                        item.MoveToContainer(player.inventory.containerWear, position);
                        break;
                    case "Belt":
                        item.MoveToContainer(player.inventory.containerBelt, position);
                        break;
                }

                if (item.GetRootContainer() == null)
                {
                    player.GiveItem(item);
                }
            }
        }

        private void GiveKit(BasePlayer player, string name)
        {
            var kit = GetKit(name);
            if (kit == null)
            {
                PrintError($"Can't find kit with name '{name}'!!!");
                return;
            }

            GiveKit(player, kit);
        }

        private Kit GetKit(string name)
        {
            try
            {
                return config.kits.First(x => string.Equals(x.name, name, StringComparison.CurrentCultureIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private bool CanUse(BasePlayer player)
        {
            return Interface.Oxide.CallHook("canRedeemKit", player) == null;
        }

        #endregion

        #region Helpers

        private double Now()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        private int Passed(double a)
        {
            return Convert.ToInt32(Now() - a);
        }

        private double SaveTime()
        {
            return SaveRestore.SaveCreatedTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        private int GetBlockTime(int time)
        {
            return time - Passed(SaveTime());
        }

        private int GetCooldown(ulong id, string name, int cooldown)
        {
            data.cooldowns.TryAdd(id, new Dictionary<string, double>());
            data.cooldowns[id].TryAdd(name, 0);
            return cooldown - Passed(data.cooldowns[id][name]);
        }

        private int GetUses(ulong id, string name)
        {
            data.uses.TryAdd(id, new Dictionary<string, int>());
            data.uses[id].TryAdd(name, 0);
            return data.uses[id][name];
        }

        private bool HasPermission(string id, string name)
        {
            return string.IsNullOrEmpty(name) || permission.UserHasPermission(id, name);
        }
        
        private string GetTimeString(int time)
        {
            var timeString = string.Empty;
            var days = Convert.ToInt32(time / 86400);
            var temp = 0;
            time = time % 86400;
            if (days > 0)
            {
                timeString += days + $" {lang.GetMessage("Day", this)}";
                temp = days;
            }
            
            var hours = Convert.ToInt32(time / 3600);
            time = time % 3600;
            if (hours > 0)
            {
                if (temp> 0)
                {
                    timeString += ", ";
                }
                
                timeString += hours + $" {lang.GetMessage("Hour", this)}";
                temp = hours;
            }
            
            var minutes = Convert.ToInt32(time / 60);
            time = time % 60;
            if (minutes > 0)
            {
                if (temp> 0)
                {
                    timeString += ", ";
                }
                
                timeString += minutes + $" {lang.GetMessage("Minute", this)}";
                temp = minutes;
            }
            
            var seconds = Convert.ToInt32(time);
            if (seconds > 0)
            {
                if (temp> 0)
                {
                    timeString += ", ";
                }
                
                timeString += seconds + $" {lang.GetMessage("Second", this)}";
            }

            return timeString;
        }

        #endregion

        #region GUI
               
        private const string elemHud = "kits.hud";
        private const string elemMain = "kits.main";
        private const string elemInfo = "kits.info";
        private const string outlineColor =  "0 0 0 1";
        private const string outlineDistance = "1.0 -0.5";

        private void CreatePanel(BasePlayer player)
        {
            if (ImageLibrary == null)
            {
                message(player, "ImageLibrary");
                return;
            }
            
            if (!CanUse(player))
            {
                return;
            }
            
            var container = new CuiElementContainer
            {
                new CuiElement // Hud
                {
                    Name = elemHud,
                    Parent = "Hud.Menu",
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = "0.25 0.25 0.25 0.75",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Close = elemHud
                        },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiNeedsCursorComponent()
                    }
                },
                new CuiElement // Text
                {
                    Parent = elemHud,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = lang.GetMessage("Available", this, player.UserIDString),
                            Color = "1 0.71 0.51 1",
                            FontSize = 25,
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0 0.7", 
                            AnchorMax = "1.0 0.8"
                        }
                    }
                }
            };

            CuiHelper.DestroyUi(player, elemHud);
            CuiHelper.AddUi(player, container);
            CreateKits(player);
        }

        private void CreateKits(BasePlayer player)
        {
            var x = -0.1;
            var y = 0.8;
            var sizeX = 0.22;
            var sizeY = 0.2;
            var kits = config.kits.Where(kit => HasPermission(player.UserIDString, kit.permission)).ToList();
            
            var container = new CuiElementContainer
            {
                new CuiElement // Main Panel
                {
                    Name = elemMain,
                    Parent = elemHud,
                    Components =
                    {
                        new CuiButtonComponent {Color = "1 1 1 0"},
                        new CuiRectTransformComponent {AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.7"}
                    }
                }
            };
            
            for (var i = 0; i < kits.Count; i++)
            {
                if (i != 0 && i % 5 == 0)
                {
                    x = -0.1;
                    y -= sizeY + 0.05;
                }

                var kit = kits[i];
                var id = kit.name;
                var cooldown = GetCooldown(player.userID, kit.name, kit.cooldown);
                var mask = lang.GetMessage(cooldown > 0 ? "GUI OFF" : "GUI ON", this, player.UserIDString);
                var cooldownText = string.Format(mask, GetTimeString(cooldown));

                if (string.IsNullOrEmpty(kit.url))
                {
                    container.Add(new CuiElement
                    {
                        Name = id,
                        Parent = elemMain,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0.25 0.25 0.25 0.5"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{x} {y}",
                                AnchorMax = $"{x + sizeX} {y + sizeY}"
                            }
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = id,
                        Parent = elemMain,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png =  GetImage(id),
                                Color = "1 1 1 1"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{x} {y}",
                                AnchorMax = $"{x + sizeX} {y + sizeY}"
                            }
                        }
                    });
                }

                container.Add(new CuiElement // Kit name
                {
                    Parent = id,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = kit.displayName,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 15
                        },
                        new CuiOutlineComponent
                        {
                            Color = outlineColor,
                            Distance = outlineDistance
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.7",
                            AnchorMax = "1 0.95"
                        }
                    }
                });
                
                container.Add(new CuiElement // Info text
                {
                    Parent = id,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "????",
                            Align = TextAnchor.UpperRight,
                            Color = "1 1 1 1",
                            FontSize = 15
                        },
                        new CuiOutlineComponent
                        {
                            Color = outlineColor,
                            Distance = outlineDistance
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0.95 0.95"
                        }
                    }
                });

                container.Add(new CuiElement // Cooldown
                {
                    Parent = id,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text  = cooldownText,
                            Align = TextAnchor.LowerCenter  
                        },
                        new CuiOutlineComponent
                        {
                            Color = outlineColor,
                            Distance = outlineDistance
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.03",
                            AnchorMax = "1 0.5"
                        }
                    }
                });
                
                container.Add(new CuiElement // Info button
                {
                    Parent = id,
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Command = $"{config.command} info {kit.name}",
                            Color = "1 1 1 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.77",
                            AnchorMax = "1 1"
                        }
                    }
                });
                
                container.Add(new CuiElement // Get button
                {
                    Parent = id,
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Command = $"{config.command} {kit.name}",
                            Color = "1 1 1 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.77"
                        }
                    }
                });

                x += sizeX + 0.02;
            }
            
            CuiHelper.DestroyUi(player, elemMain);
            CuiHelper.AddUi(player, container);
        }

        private void ShowInfo(BasePlayer player, string name)
        {
            if (ImageLibrary == null)
            {
                message(player, "ImageLibrary");
                return;
            }
            
            var kit = GetKit(name);
            if (kit == null) {return;}
            
            var container = new CuiElementContainer
            {
                new CuiElement // Hud
                {
                    Name = elemInfo,
                    Parent = "Hud.Menu",
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = "0.25 0.25 0.25 0.75",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Close = elemInfo
                        },
                        new CuiRectTransformComponent 
                        {
                            AnchorMin = "0 0", 
                            AnchorMax = "1 1"
                        }
                    }
                },
                new CuiElement // Name
                {
                    Parent = elemInfo,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = kit.displayName.ToUpper(),
                            Color = "1 1 1 1",
                            FontSize = 30,
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0 0.7", 
                            AnchorMax = "1 1"
                        }
                    }
                },
                new CuiElement // Info
                {
                    Parent = elemInfo,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"{kit.description}\n" +
                                   $"Перезарядка {GetTimeString(kit.cooldown)}\n" +
                                   (kit.uses == 0 ? "" : $"Uses {kit.uses}"),
                            Color = "1 1 1 1",
                            FontSize = 15,
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0 0.7", 
                            AnchorMax = "1 0.8"
                        }
                    }
                }
            };

            var x = 0.3;
            var y = 0.6;
            var sizeX = 0.05;
            var sizeY = 0.08;
            var items = kit.items; 

            for (var i = 0; i < items.Count; i++)
            {
                if (i != 0 && i % 8 == 0)
                {
                    x = 0.3;
                    y -= sizeY + 0.01;
                }

                var item = items[i];

                container.Add(new CuiElement
                {
                    Parent = elemInfo,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.5 0.5 0.5 0.75"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{x} {y}",
                            AnchorMax = $"{x + sizeX} {y + sizeY}"
                        }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = elemInfo,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.shortname)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{x} {y}",
                            AnchorMax = $"{x + sizeX} {y + sizeY}"
                        }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = elemInfo,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"x {item.amount}",
                            Align = TextAnchor.LowerRight
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{x} {y}",
                            AnchorMax = $"{x + sizeX} {y + sizeY}"
                        }
                    }
                });

                x += sizeX + 0.005;
            }

            CuiHelper.DestroyUi(player, elemInfo);
            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}