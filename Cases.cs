using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Cases", "Drop Dead / Redesign and fix by Deversive", "1.0.2")]
    public class Cases : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        private static Cases _ins;
        bool addday = false;
        string Layer = "Cases.Main";
                string Case231 = "1";
        private string Case341 = "2";
        private string Case56161 = "3";
        private string Case612354 = "4";
        public Dictionary<ulong, int> time = new Dictionary<ulong, int>();
        public Dictionary<ulong, List<Inventory>> inventory = new Dictionary<ulong, List<Inventory>>();
        public List<int> day = new List<int>();
        public Dictionary<ulong, int> taked = new Dictionary<ulong, int>();
        public List<ulong> openedui = new List<ulong>();


        string MainIMG = "https://imgur.com/uXKc6US.png";
        string InventoryIMG = "https://imgur.com/MV2Za1Z.png";
        private string sera1 = "https://imgur.com/pXlrvM7.png";
        private string case1s = "https://imgur.com/o0KIoCM.png";
        private string case2s = "https://imgur.com/iLR76AW.png";
        private string case3s = "https://imgur.com/Wbw850T.png";
        private string case4s = "https://imgur.com/B41YB7l.png";
        private string button = "https://imgur.com/HYj6vSU.png";
        

        public class Inventory
        {
            public bool command;
            public string strcommand;
            public string shortname;
            public int amount;

        }

        public class random
        {
            [JsonProperty("Шанс выпадения")]
            public int chance;
            [JsonProperty("Минимальное количество")]
            public int min;
            [JsonProperty("Максимальное количество")]
            public int max;
        }

        public class chance
        {
            [JsonProperty("Шанс выпадения")]
            public int chances;
            [JsonProperty("Картинка")]
            public string image;
        }

        public class Case
        {
            [JsonProperty("Сколько времени должен отыграть игрок для открытия кейса (в секундах)")]
            public int time = 300;

            [JsonProperty("Использовать выпадение предметов?")]
            public bool items = true;

            [JsonProperty("Использовать выдачу команды?")]
            public bool command = false;

            [JsonProperty("Команды для выполнения (%steamid% заменяется на айди игрока)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, chance> strcommands = new Dictionary<string, chance>
            {
                ["say %steamid%"] = new chance { image = "https://i.imgur.com/DXB7GRi.png", chances = 100 },
                ["example"] = new chance { image = "https://i.imgur.com/sLZm4on.png", chances = 100 },
            };

            [JsonProperty("Предметы которые могут выпасть при открытии", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, random> itemsdrop = new Dictionary<string, random>
            {
                ["sulfur"] = new random { chance = 100, min = 10, max = 50}, 
                ["metal.fragments"] = new random { chance = 50, min = 50, max = 150},
            };
        }

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Настройки кейсов")]
            public MainSettings settings = new MainSettings();

            public class MainSettings
            {
                [JsonProperty("День кейса, её настройки", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, Case> cases = new Dictionary<string, Case>()
                {
                    ["1"] = new Case(),
                    ["2"] = new Case(),
                    ["3"] = new Case(),
                    ["4"] = new Case(),
                    ["5"] = new Case(),
                };
            }
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/Cooldown", time);
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/Day", day);
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/Taked", taked);
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/Inventory", inventory);
        }

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Title}/Cooldown"))
                time = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>($"{Title}/Cooldown");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Title}/Day"))
                day = Interface.Oxide.DataFileSystem.ReadObject<List<int>>($"{Title}/Day");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Title}/Taked"))
                taked = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>($"{Title}/Taked");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Title}/Inventory"))
                inventory = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<Inventory>>>($"{Title}/Inventory");
        }

        void OnServerInitialized()
        {
            _ins = this;
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("ImageLibrary not found. Install it and reload plugin!");
                return;
            }

            LoadData();
            if (day == null || day.Count < 1) day.Add(1);
            if (GetWipeDay() > 4) day[0] = 1;

            if (!IMGLibrary.HasImage(MainIMG, 0)) IMGLibrary.AddImage(MainIMG, MainIMG, 0);
            if (!IMGLibrary.HasImage(sera1, 0)) IMGLibrary.AddImage(sera1, sera1, 0);
            if (!IMGLibrary.HasImage(case1s, 0)) IMGLibrary.AddImage(case1s, case1s, 0);
            if (!IMGLibrary.HasImage(case2s, 0)) IMGLibrary.AddImage(case2s, case2s, 0);
            if (!IMGLibrary.HasImage(case3s, 0)) IMGLibrary.AddImage(case3s, case3s, 0);
            if (!IMGLibrary.HasImage(case4s, 0)) IMGLibrary.AddImage(case4s, case4s, 0);
            if (!IMGLibrary.HasImage(button, 0)) IMGLibrary.AddImage(button, button, 0);
            if (!IMGLibrary.HasImage(InventoryIMG, 0)) IMGLibrary.AddImage(InventoryIMG, InventoryIMG, 0);
            foreach (var item in cfg.settings.cases.Values) 
            {
                foreach (var cmd in item.strcommands) 
                {
                    if (!string.IsNullOrEmpty(cmd.Value.image) && !IMGLibrary.HasImage(cmd.Key, 0)) IMGLibrary.AddImage(cmd.Value.image, cmd.Key, 0);
                }
                foreach (var cmd in item.itemsdrop) 
                {
                    if (!string.IsNullOrEmpty(cmd.Key) && !IMGLibrary.HasImage(cmd.Key, 0)) IMGLibrary.AddImage("https://rustlabs.com/img/items180/" + cmd.Key + ".png", cmd.Key, 0);
                }
            }


            if (BasePlayer.activePlayerList.Count > 0) foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);
            InvokeHandler.Instance.InvokeRepeating(UpdateTime, 60f, 60f);
            InvokeHandler.Instance.InvokeRepeating(UpdateUI, 1f, 1f);
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            InvokeHandler.Instance.CancelInvoke(UpdateTime);
            InvokeHandler.Instance.CancelInvoke(UpdateUI);
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);
            SaveData();
            _ins = null;
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
            if (!time.ContainsKey(player.userID)) time.Add(player.userID, 0);
            if (!inventory.ContainsKey(player.userID)) inventory.Add(player.userID, new List<Inventory>());
        }

        void UpdateUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!time.ContainsKey(player.userID)) time.Add(player.userID, 0);
                time[player.userID]++;

                if (openedui.Contains(player.userID))
                {
                    var container = new CuiElementContainer();
                    if (GetWipeDay() == 1)
                    {
                        CuiHelper.DestroyUi(player, "1");
                        
                        /*container.Add(new CuiElement
                        {
                            Name = "1",
                            Parent = "container",
                            Components =
                            {
                                new CuiImageComponent { Png = GetImage(button), Material = "assets/icons/greyout.mat"},
                                new CuiRectTransformComponent { AnchorMin = "0.09528343 0.2108527", AnchorMax = "0.1955846 0.255814" }
                            }
                        });*/
                        
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.09528343 0.2108527", AnchorMax = "0.1955846 0.255814" },
                            Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 1) && GetWipeDay() == 1 ? "TakeCase 1" : "" },
                            Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 1).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                        }, "container", "1");
                    }
                    if (GetWipeDay() == 2)
                    {
                        CuiHelper.DestroyUi(player, "2");
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.3279826 0.2124031", AnchorMax = "0.4282839 0.2573644" },
                            Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 2) && GetWipeDay() == 2 ? "TakeCase 2" : "" },
                            Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 2).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                        }, "container", "2");
                    }
                    if (GetWipeDay() == 3)
                    {
                        CuiHelper.DestroyUi(player, "3");
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5757279 0.2093023", AnchorMax = "0.6760302 0.2542633" },
                            Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 3) && GetWipeDay() == 3 ? "TakeCase 3" : "" },
                            Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 3).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                        }, "container", "3");
                    }
                    
                    if (GetWipeDay() == 4)
                    {
                        CuiHelper.DestroyUi(player, "4");
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.8204627 0.2093022", AnchorMax = "0.9207657 0.2542628" },
                            Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 4) && GetWipeDay() == 4 ? "TakeCase 4" : "" },
                            Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 4).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                        }, "container", "4");
                    }
                    CuiHelper.AddUi(player, container);
                }
            }
        }

        void UpdateTime()
        {
            if (DateTime.UtcNow.AddHours(3).ToString("HH:mm") == "02:00")
            {
                if (day.Count > 0)
                {
                    day[0]++;
                    //Puts("Started new day..");
                    time.Clear();
                    SaveData();
                }
            }
        }

        int GetWipeDay()
        {
            if (day == null) day.Add(1);
            return day[0];
        }

        bool HasCooldown(BasePlayer player, int Day)
        {
            if (!time.ContainsKey(player.userID)) time.Add(player.userID, 0);
            foreach (var i in cfg.settings.cases)
            {
                if (Day.ToString() != i.Key) continue;
                var cooldown = time[player.userID];
                if (cooldown >= i.Value.time) return false;
            }
            return true;
        }

        int GetCooldown(BasePlayer player, int Day)
        {
            if (!time.ContainsKey(player.userID)) time.Add(player.userID, 0);
            int amount = 0;
            foreach (var i in cfg.settings.cases)
            {
                if (Day.ToString() != i.Key) continue;
                var cooldown = time[player.userID];
                amount = i.Value.time - cooldown;
                if (amount < 0) return 0;
            }
            return amount;
        }

        string CanTake(BasePlayer player, int Day)
        {
            string text = "ОТКРЫТЬ";
            if (taked.ContainsKey(player.userID) && taked[player.userID] == Day) return "ПОЛУЧЕНО";
            if (Day != GetWipeDay()) return "НЕДОСТУПНО";
            if (HasCooldown(player, Day) == true) return TimeToString(GetCooldown(player, Day));
            return text;
        }
        
        //private Dictionary<BasePlayer, List<string>> caseuizs = new Dictionary<BasePlayer, List<string>>();
        
        
        [ChatCommand("case")]
        private void CaseCommand(BasePlayer player)
        {
            if (player == null) return;
            DrawMainUI(player);
        }

        [ConsoleCommand("CloseUI1248712389")]
        private void CloseUI(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (openedui.Contains(player.userID)) openedui.Remove(player.userID);
            CuiHelper.DestroyUi(player, Layer);
        }

        private bool ShouldItemDrop(int chance)
        {
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        private int GetRandomAmount(int min, int max)
        {
            return Oxide.Core.Random.Range(min, max);
        }

        private bool HasItemInInventory(Dictionary<ulong, List<Inventory>> inventory, ulong playerId, string shortname)
        {
            if (inventory.ContainsKey(playerId))
            {
                foreach (var item in inventory[playerId])
                {
                    if (item.shortname == shortname)
                        return true;
                }
            }
            return false;
        }

        private void AddItemToInventory(Dictionary<ulong, List<Inventory>> inventory, ulong playerId, string shortname, int amount)
        {
            if (inventory.ContainsKey(playerId))
            {
                foreach (var item in inventory[playerId])
                {
                    if (item.shortname == shortname)
                    {
                        item.amount += amount;
                        return;
                    }
                }
                inventory[playerId].Add(new Inventory { command = false, shortname = shortname, amount = amount });
            }
            else
            {
                inventory.Add(playerId, new List<Inventory> { new Inventory { command = false, shortname = shortname, amount = amount } });
            }
        }


        [ConsoleCommand("inventoryuiopenz")]
        void inventoryui(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (openedui.Contains(player.userID)) openedui.Remove(player.userID);
            CuiHelper.DestroyUi(player, Layer);
            DrawInventoryUI(player);
        }
        
        [ConsoleCommand("TakeCase")]
        private void TakeCase(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (!args.HasArgs(1)) return;

            var day = args.Args[0];
            if (HasCooldown(player, int.Parse(day))) return;
            if (taked.ContainsKey(player.userID) && taked[player.userID] >= int.Parse(day)) return;

            foreach (var capsule in cfg.settings.cases)
            {
                if (capsule.Key != day) continue;

                if (capsule.Value.items)
                {
                    foreach (var item in capsule.Value.itemsdrop)
                    {
                        if (ShouldItemDrop(item.Value.chance))
                        {
                            var amount = GetRandomAmount(item.Value.min, item.Value.max);
                            if (!HasItemInInventory(inventory, player.userID, item.Key))
                            {
                                AddItemToInventory(inventory, player.userID, item.Key, amount);
                                break; // Выходим из цикла, чтобы добавить только один предмет
                            }
                        }
                    }
                }

                if (capsule.Value.command)
                {
                    foreach (var cmd in capsule.Value.strcommands)
                    {
                        if (ShouldItemDrop(cmd.Value.chances))
                        {
                            if (inventory.ContainsKey(player.userID))
                                inventory[player.userID].Add(new Inventory { command = true, strcommand = cmd.Key });
                            else
                                inventory.Add(player.userID, new List<Inventory> { new Inventory { command = true, strcommand = cmd.Key } });
                        }
                    }
                }
            }

            if (taked.ContainsKey(player.userID)) taked[player.userID] = int.Parse(day);
            else taked.Add(player.userID, int.Parse(day));

            var effect = new Effect("assets/prefabs/misc/xmas/presents/effects/unwrap.prefab", player, 0, Vector3.zero, Vector3.forward);
            EffectNetwork.Send(effect, player.net.connection);
        }

        [ConsoleCommand("casepage")]
        private void ChangePage(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                var page = int.Parse(args.Args[0]);
                if (page * 14 <= inventory[player.userID].Count)
                {
                    DrawInventoryUI(player, page);
                }
            }
        }

        [ConsoleCommand("TakeItem")]
        private void TakeItem(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (!args.HasArgs(2)) return;

            var shortname = args.Args[0];
            var page = int.Parse(args.Args[1]);

            foreach (var item in inventory[player.userID])
            {
                if (item.command || item.shortname != shortname) continue;
                var newItem = ItemManager.CreateByName(item.shortname, item.amount);
                if (newItem == null) continue;
                player.GiveItem(newItem);
                //if (!player.inventory.GiveItem(newItem))
                //    newItem.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                break;
            }

            for (int i = 0; i < inventory[player.userID].Count; i++)
            {
                var key = inventory[player.userID][i];
                if (key.shortname != shortname) continue;
                inventory[player.userID].Remove(inventory[player.userID][i]);
                break;
            }

            DrawInventoryUI(player, page);
        }

        [ConsoleCommand("TakePerm")]
        private void TakePerm(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (!args.HasArgs(2)) return;

            var perm = args.Args[0].Replace("*", " ");
            var page = int.Parse(args.Args[1]);

            foreach (var item in inventory[player.userID])
            {
                if (!item.command || item.strcommand != perm) continue;
                Server.Command(perm.Replace("%steamid%", player.UserIDString));
                break;
            }

            for (int i = 0; i < inventory[player.userID].Count; i++)
            {
                var key = inventory[player.userID][i];
                if (!key.command || key.strcommand != perm) continue;
                inventory[player.userID].Remove(inventory[player.userID][i]);
                break;
            }

            DrawInventoryUI(player, page);
        }

        void DrawMainUI(BasePlayer player)
        {
            if (!openedui.Contains(player.userID)) openedui.Add(player.userID);
            //caseuizs[player] = new List<string>();
            
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            
            
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(MainIMG), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.2406265 0.1981481", AnchorMax = "0.7598959 0.795370" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, "container");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4242712 0.8139536", AnchorMax = "0.6459364 0.9689922" },
                Text = { Text = $"КЕЙСЫ", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 28, Font = "robotocondensed-bold.ttf" }
            },  "container");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3741207 0.7937984", AnchorMax = "0.665997 0.9100775" },
                Text = { Text = $"Здесь вы можете открыть ежедневные бесплатные кейсы", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 13, Font = "robotocondensed-regular.ttf" }
            },  "container");
            
            container.Add(new CuiElement
            {
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(sera1), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.402205 0.8620155", AnchorMax = "0.5025063 1.017055" }
                }
            });
            
            container.Add(new CuiElement
            {
                Name = "1",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(case1s), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.03911462 0.1937985", AnchorMax = "0.241723 0.8139534" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(button), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.4363075 0.02945746", AnchorMax = "0.5596776 0.07751947" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4363075 0.02945746", AnchorMax = "0.5596776 0.07751947" },
                Button = { Color = "0 0 0 0", Command = "inventoryuiopenz" },
                Text = { Text = $"ИНВЕНТАРЬ", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.MiddleCenter, FontSize = 15, Font = "robotocondensed-bold.ttf" }
            }, "container");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09428035 0.7007753", AnchorMax = "0.1945815 0.7534884" },
                Text = { Text = $"ДЕНЬ 1", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 15, Font = "robotocondensed-bold.ttf" }
            },  "container", "1");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04814178 0.6217054", AnchorMax = "0.2336989 0.7116279" },
                Text = { Text = $"В первый день вайпа вы можете открыть кейс с довольно полезным лутом для начала вайпа.", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 9, Font = "robotocondensed-regular.ttf" }
            },  "container", "1");
            
            container.Add(new CuiElement
            {
                Name = "1",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(button), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.09528343 0.2108527", AnchorMax = "0.1955846 0.255814" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.09528343 0.2108527", AnchorMax = "0.1955846 0.255814" },
                Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 1) && GetWipeDay() == 1 ? "TakeCase 1" : "" },
                Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 1).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "container", "1");




            container.Add(new CuiElement
            {
                Name = "2",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(case2s), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.2748225 0.1968992", AnchorMax = "0.4744217 0.815504" }
                }
            });
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3269798 0.7023253", AnchorMax = "0.4272808 0.7550379" },
                Text = { Text = $"ДЕНЬ 2", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 15, Font = "robotocondensed-bold.ttf" }
            },  "container", "2");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2808405 0.6232556", AnchorMax = "0.4663976 0.7131781" },
                Text = { Text = $"Во второй день вайпа вы можете открыть этот кейс и получить с некоторым шансом привилегию Lightning на 7 дней.", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 9, Font = "robotocondensed-regular.ttf" }
            },  "container", "2");
            
            container.Add(new CuiElement
            {
                Name = "2",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(button), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.3279826 0.2124031", AnchorMax = "0.4282839 0.2573644" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3279826 0.2124031", AnchorMax = "0.4282839 0.2573644" },
                Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 2) && GetWipeDay() == 2 ? "TakeCase 2" : "" },
                Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 2).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "container", "2");
            
            
            
            container.Add(new CuiElement
            {
                Name = "3",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(case3s), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.5185544 0.1922481", AnchorMax = "0.7231687 0.8170543" }
                }
            });
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5747257 0.6992249", AnchorMax = "0.6750247 0.7519373" },
                Text = { Text = $"ДЕНЬ 3", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 15, Font = "robotocondensed-bold.ttf" }
            },  "container", "3");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5285843 0.6201547", AnchorMax = "0.7141411 0.7100775" },
                Text = { Text = $"В третий день вы получаете доступ к открытию этого кейса, в котором вам может выпасть Hurricane на 3 дня или метаболизм на 7 дней.", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 9, Font = "robotocondensed-regular.ttf" }
            },  "container", "3");
            
            container.Add(new CuiElement
            {
                Name = "3",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(button), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.5757279 0.2093023", AnchorMax = "0.6760302 0.2542633" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5757279 0.2093023", AnchorMax = "0.6760302 0.2542633" },
                Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 3) && GetWipeDay() == 3 ? "TakeCase 3" : "" },
                Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 3).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "container", "3");
            
            
            
            container.Add(new CuiElement
            {
                Name = "4",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(case4s), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.7652953 0.192248", AnchorMax = "0.9648945 0.8170542" }
                }
            });
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.8194606 0.6992244", AnchorMax = "0.9197595 0.7519375" },
                Text = { Text = $"ДЕНЬ 4", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 15, Font = "robotocondensed-bold.ttf" }
            },  "container", "4");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7733191 0.6201547", AnchorMax = "0.9588759 0.7100775" },
                Text = { Text = $"В четвертый день при открытии этого кейса у вас есть шанс выбить Hurricane на 7 дней или карманный переработчик на 7 дней", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 9, Font = "robotocondensed-regular.ttf" }
            },  "container", "4");
            
            container.Add(new CuiElement
            {
                Name = "4",
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(button), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.8204627 0.2093022", AnchorMax = "0.9207657 0.2542628" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8204627 0.2093022", AnchorMax = "0.9207657 0.2542628" },
                Button = { Color = "0 0 0 0", Command = !HasCooldown(player, 4) && GetWipeDay() == 4 ? "TakeCase 4" : "" },
                Text = { Color = HexToRustFormat("#FFFFFFFF"), Text = CanTake(player, 4).ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "container", "4");
            
            
            

            CuiHelper.AddUi(player, container);
        }
        
        void DrawInventoryUI(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = "container",
                Components =
                {
                    new CuiRawImageComponent {Png = GetImage(InventoryIMG) },
                    new CuiRectTransformComponent { AnchorMin = "0.2406265 0.1981481", AnchorMax = "0.7598959 0.795370" }
                }
            });
            
            if (inventory.ContainsKey(player.userID) && inventory[player.userID].Count == 0)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"ВАШ ИНВЕНТАРЬ ПУСТ!", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-bold.ttf" }
                }, "container");
            }
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, "container");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4322953 0.8899231", AnchorMax = "0.6539608 0.9596905" },
                Text = { Text = $"ИНВЕНТАРЬ", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 26, Font = "robotocondensed-bold.ttf" }
            },  "container");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3721144 0.7488377", AnchorMax = "0.680039 0.9038764" },
                Text = { Text = $"Данном инвентаре вы можете забрать приз с кейса", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 13, Font = "robotocondensed-regular.ttf" }
            },  "container");
            
            container.Add(new CuiElement
            {
                Parent = "container",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(sera1), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.3681024 0.8542643", AnchorMax = "0.4684036 0.9953494" }
                }
            });
            

            const double startAnMinX = 0.05315678;
            const double startAnMaxX = 0.1532617;
            const double startAnMinY = 0.5689927;
            const double startAnMaxY = 0.7983856;
            double anMinX = startAnMinX;
            double anMaxX = startAnMaxX;
            double anMinY = startAnMinY;
            double anMaxY = startAnMaxY;

            List<Inventory> dict = inventory[player.userID].Skip(14 * page).Take(14).ToList();
            for (int i = 0; i < dict.Count; i++)
            {
                var value = dict[i];
                if (value == null) continue;

                if ((i != 0) && (i % 7 == 0))
                {
                    anMinX = startAnMinX;
                    anMaxX = startAnMaxX;
                    anMinY -= 0.4013889;
                    anMaxY -= 0.4013889;
                }

                container.Add(new CuiElement
                {
                    Parent = "container",
                    Name = i.ToString(),
                    Components =
                    {
                        new CuiImageComponent {Color = HexToRustFormat("#27141B") },
                        new CuiRectTransformComponent { AnchorMin = $"{anMinX} {anMinY}", AnchorMax = $"{anMaxX} {anMaxY}" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = i.ToString(),
                    Components =
                    {
                        new CuiRawImageComponent {Png = value.command ? GetImage(value.strcommand) : GetImage(value.shortname) },
                        new CuiRectTransformComponent {AnchorMin = "0.1302546 0.3784849", AnchorMax = "0.8717052 0.8786259"}
                    }
                });
                if (!value.command)
                {
                    container.Add(new CuiElement
                    {
                        Parent = i.ToString(),
                        Components =
                        {
                            new CuiTextComponent { Color = HexToRustFormat("#949494FF"), Text = "x" + value.amount.ToString(), Align = TextAnchor.LowerRight, FontSize = 10, Font = "RobotoCondensed-bold.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.4308421 0.2495073", AnchorMax = "0.8684198 0.4122787"}
                        }
                    });
                }
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.1202347 0.06082799", AnchorMax = "0.9017637 0.216277" },
                    Button = { Color = "0 0 0 0", Command = !value.command ? "TakeItem " + value.shortname + " " + page.ToString() : $"TakePerm {value.strcommand.Replace(" ", "*")}" + " " + page.ToString() },
                    Text = { Color = HexToRustFormat("#747474FF"), Text = "ЗАБРАТЬ", Align = TextAnchor.UpperCenter, FontSize = 10, Font = "RobotoCondensed-bold.ttf" }
                }, i.ToString());
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = !value.command ? "TakeItem " + value.shortname + " " + page.ToString() : $"TakePerm {value.strcommand.Replace(" ", "*")}" + " " + page.ToString() },
                    Text = { Text = "" }
                }, i.ToString());

                anMinX += 0.12812545;
                anMaxX += 0.12812545;
            }

            container.Add(new CuiElement
            {
                Parent = "container",
                Components =
                {
                    new CuiTextComponent { Color = HexToRustFormat("#747474FF"), Text = $"{page + 1}", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "RobotoCondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.4601567 0.05277855", AnchorMax = "0.5382817 0.1916674"}
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4476566 0.08472304", AnchorMax = "0.4742191 0.1611119" },
                Button = { Command = $"casepage {page - 1}", Color = "0 0 0 0" },
                Text = { Color = HexToRustFormat("#747474FF"), Text = $"<", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "RobotoCondensed-bold.ttf" }
            }, "container");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5265617 0.08472304", AnchorMax = "0.5531241 0.1611119" },
                Button = { Command = $"casepage {page + 1}", Color = "0 0 0 0" },
                Text = { Color = HexToRustFormat("#747474FF"), Text = $">", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "RobotoCondensed-bold.ttf"  }
            }, "container");

            CuiHelper.AddUi(player, container);
        }

        public string TimeToString(double time)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(time);
            int hours = elapsedTime.Hours;
            int minutes = elapsedTime.Minutes;
            int seconds = elapsedTime.Seconds;
            int days = Mathf.FloorToInt((float)elapsedTime.TotalDays);
            string s = "";
            if (days > 0) s += $"{days} дн.";
            if (hours > 0) s += $"{hours} ч. ";
            if (minutes > 0) s += $"{minutes} мин. ";
            if (seconds >= 0) s += $"{seconds} сек.";
            else s = s.TrimEnd(' ');
            return s;
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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

        string GetImage(string name) => (string)ImageLibrary?.Call("GetImage", name);

        public static class IMGLibrary
        {
            public static bool AddImage(string url, string imageName, ulong imageId = 0, Action callback = null) => (bool)_ins.ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
            public static bool AddImageData(string imageName, byte[] array, ulong imageId = 0, Action callback = null) => (bool)_ins.ImageLibrary.Call("AddImageData", imageName, array, imageId, callback);
            public static string GetImageURL(string imageName, ulong imageId = 0) => (string)_ins.ImageLibrary.Call("GetImageURL", imageName, imageId);
            public static string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)_ins.ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);
            public static List<ulong> GetImageList(string name) => (List<ulong>)_ins.ImageLibrary.Call("GetImageList", name);
            public static Dictionary<string, object> GetSkinInfo(string name, ulong id) => (Dictionary<string, object>)_ins.ImageLibrary.Call("GetSkinInfo", name, id);
            public static bool HasImage(string imageName, ulong imageId) => (bool)_ins.ImageLibrary.Call("HasImage", imageName, imageId);
            public static bool IsInStorage(uint crc) => (bool)_ins.ImageLibrary.Call("IsInStorage", crc);
            public static bool IsReady() => (bool)_ins.ImageLibrary.Call("IsReady");
            public static void ImportImageList(string title, Dictionary<string, string> imageList, ulong imageId = 0, bool replace = false, Action callback = null) => _ins.ImageLibrary.Call("ImportImageList", title, imageList, imageId, replace, callback);
            public static void ImportItemList(string title, Dictionary<string, Dictionary<ulong, string>> itemList, bool replace = false, Action callback = null) => _ins.ImageLibrary.Call("ImportItemList", title, itemList, replace, callback);
            public static void ImportImageData(string title, Dictionary<string, byte[]> imageList, ulong imageId = 0, bool replace = false, Action callback = null) => _ins.ImageLibrary.Call("ImportImageData", title, imageList, imageId, replace, callback);
            public static void LoadImageList(string title, List<KeyValuePair<string, ulong>> imageList, Action callback = null) => _ins.ImageLibrary.Call("LoadImageList", title, imageList, callback);
            public static void RemoveImage(string imageName, ulong imageId) => _ins?.ImageLibrary?.Call("RemoveImage", imageName, imageId);
        }
    }
}