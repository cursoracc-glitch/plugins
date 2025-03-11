using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using TinyJSON;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("MKitBR", "TopPlugin.ru", "3.0.0")]
    public class MKitBR : RustPlugin
    {
        #region var

        string Layer = "Kit_UI";

        [PluginReference] private Plugin ImageLibrary;

        private string prefix = "mkitbr.";


        #endregion

        #region Image

        private string nw = "https://i.ibb.co/gVwn5NG/wipe.png";
        private string nwName = "nwIcon";

        #endregion

        #region data

        public class PlayerKit
        {
            public string name;
            public int amount;
            public double time;
        }

        public Dictionary<ulong, List<PlayerKit>> playerData = new Dictionary<ulong, List<PlayerKit>>();

        public class ItemData
        {
            public string shortname;
            public int amount;
            public ulong skinId = 0;
            public string container;
        }

        public List<KitData> Kits = new List<KitData>();

        public class KitData
        {
            [JsonProperty("Name")] public string name;
            [JsonProperty("Permission")] public string permission;
            [JsonProperty("Wipe delay")] public int wipeDelay;
            [JsonProperty("Amount")] public int amount;
            [JsonProperty("Cooldown")] public int cooldown;
            [JsonProperty("Image")] public string kitImage;
            [JsonProperty("Items")] public List<ItemData> kitItems;
        }

        #endregion

        #region config



        public string BannerURL =
            "https://i.ibb.co/wMhhtg7/baner.png";

        public string Description = "Больше наборов на <color=#b22222>вашем магазине</color>";

        #endregion

        #region Oxide Hooks
        
        void Unload()
        {
            SavePlayersData();
            SaveKitData();
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("MKit/KitData")) CreateDefautlKits();
            LoadKitData();

            if (ServerIsWiped())
            {
                for (int i = 0; i < 3; i++)
                {
                    PrintError("WIPE DETECTED");
                }
                Interface.Oxide.DataFileSystem.GetFile("MKit/PlayerData").Clear();
            }

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("MKit/PlayerData"))
            {
                if (BasePlayer.activePlayerList.Count > 0)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        CreatePlayerData(player);

                    }

                    SavePlayersData();
                }
                
            }
            else
            {
                LoadPlayersData();
            }
            timer.Once(3f, (() => ImagesAndPermissions()));

        }

        protected override void LoadDefaultConfig()
        {
            Config["Ссылка на банер"] = BannerURL = GetConfig("Ссылка на банер",
                "https://i.ibb.co/wMhhtg7/baner.png");
            Config["Описание"] = Description = GetConfig("Описание",
                "Больше наборов на <color=#b22222>вашем магазине</color>");
            SaveConfig();
        }
        
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SavePlayersData();
        }

        #endregion

        #region hooks

        void CreateDefautlKits()
        {
            KitData defKit = new KitData
            {
                name = "start",
                amount = 2,
                cooldown = 2,
                kitImage =
                    "https://yt3.ggpht.com/a/AATXAJzeeYQsAYa1zViLl5tEXPcoSi5wkpMclT9vXp_PkA=s900-c-k-c0xffffffff-no-rj-mo",
                permission = prefix + "default",
                wipeDelay = 100,
                kitItems = new List<ItemData>
                {
                    new ItemData
                    {
                        shortname = "wood",
                        amount = 5000,
                        container = "main"
                    },
                    new ItemData
                    {
                        shortname = "pickaxe",
                        amount = 1,
                        container = "belt"
                    }
                }
            };
            Kits.Add(defKit);
            SaveKitData();
            
        }

        #region Test

        /*[ChatCommand("wipe")]
        void wipePLayer(BasePlayer player)
        {
            if (playerData.ContainsKey(player.userID))
            {
                playerData.Remove(player.userID);
                SavePlayersData();
            }
        }*/
        

        #endregion

        void ImagesAndPermissions()
        {
            ImageLibrary.Call("AddImage", BannerURL, "Banner");
            ImageLibrary.Call("AddImage", nw, nwName);
            
            foreach (var check in Kits)
            {
                ImageLibrary.Call("AddImage", check.kitImage, check.kitImage);
                permission.RegisterPermission(check.permission, this);
                foreach (var item in check.kitItems)
                {
                    ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{item.shortname}.png",
                        item.shortname);
                }


            }
        }

        bool ServerIsWiped()
        {
            bool status = Player.Sleepers.Count == 0 && Player.Players.Count == 0;
            return status;

        }

        void SaveKitData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("MKit/KitData", Kits);
        }

        void LoadKitData()
        {
            Kits = Interface.Oxide.DataFileSystem.ReadObject<List<KitData>>("MKit/KitData");
            foreach (var kit in Kits)
            {
                PrintWarning($"Kit {kit.name} loaded");
            }

            PrintWarning("Kits loaded");
        }

        void LoadPlayersData()
        {
            playerData =
                Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<PlayerKit>>>("MKit/PlayerData");
            
        }

        List<string> GetKitList(List<KitData> kits)
        {
            List<string> kitNames = new List<string>();
            foreach (var kit in kits)
            {
                kitNames.Add(kit.name);
            }

            return kitNames;
        }

        void PlayerDataUpdate(BasePlayer player)
        {
            if (!playerData.ContainsKey(player.userID))
            {
                CreatePlayerData(player);
            }
            RefreshPlayerData(player);
        }
        void RefreshPlayerData(BasePlayer player)
        {
            if (playerData.Count <1 || BasePlayer.activePlayerList.Count<1)
            {
                return;
            }

            if (playerData.Count == 0)
            {
                return;
            }
            
            
                foreach (var kit in Kits.Where(z => (string.IsNullOrEmpty(z.permission) || permission.UserHasPermission(player.UserIDString, z.permission))))
                {
                    
                    if (!GetPlayerKit(BasePlayer.FindByID(player.userID)).Contains(kit.name))
                    {
                        playerData[player.userID].Add(new PlayerKit
                        {
                            name = kit.name,
                            amount = kit.amount,
                            time = CurTime()
                        });
                        
                    }
                    
                }
            
            SavePlayersData();
        }

        void RefreshPlayersData()
        {
            if (playerData.Count <1 || BasePlayer.activePlayerList.Count<1)
            {
                return;
            }

            if (playerData.Count == 0)
            {
                return;
            }
            
            foreach (var player in playerData)
            {
                foreach (var kit in Kits.Where(z => (string.IsNullOrEmpty(z.permission) || permission.UserHasPermission(player.Key.ToString(), z.permission))))
                {
                    
                    if (!GetPlayerKit(BasePlayer.FindByID(player.Key)).Contains(kit.name))
                    {
                        playerData[player.Key].Add(new PlayerKit
                            {
                                name = kit.name,
                                amount = kit.amount,
                                time = CurTime()
                            });
                    }
                    
                }
            }
            SavePlayersData();
        }

       
        bool CanRecieveKit(BasePlayer player, KitData kitData)
        {
            bool canRecieve = false;

            int belt = player.inventory.containerBelt.itemList.Count;
            int main = player.inventory.containerMain.itemList.Count;
            int wear = player.inventory.containerWear.itemList.Count;

            int cb = player.inventory.containerBelt.capacity;
            int cm = player.inventory.containerMain.capacity;
            int cw = player.inventory.containerWear.capacity;

            int kbelt = 0;
            int kmain = 0;
            int kwear = 0;
            foreach (var item in kitData.kitItems)
            {
                if (item.container == "belt")
                {
                    kbelt++;
                }
                if (item.container == "main")
                {
                    kmain++;
                }
                if (item.container == "wear")
                {
                    kwear++;
                }
            }

            if (cb-belt>=kbelt && cm-main>=kmain && cw-wear>=kwear)
            {
                canRecieve = true;
            }
            
            
            return canRecieve;
        }

        

        void KitRedeem(BasePlayer player, KitData kitData)
        {
            foreach (var item in kitData.kitItems)
            {
                Item cItem =ItemManager.CreateByPartialName(item.shortname, item.amount, item.skinId);
                switch (item.container)
                {
                    case "belt":
                        cItem.MoveToContainer(player.inventory.containerBelt);
                        break;
                    case "main":
                        cItem.MoveToContainer(player.inventory.containerMain);
                        break;
                    case "wear":
                        cItem.MoveToContainer(player.inventory.containerWear);
                        break;
                }
            }
            SendReply(player,$"<color=#921100>Набор</color> <color=#4ef41a>{kitData.name}</color> <color=#921100>получен! Наслаждайся!</color>");
        }

        

        void CreatePlayerData(BasePlayer player)
        {
            if (playerData.ContainsKey(player.userID)) return;
            List<PlayerKit>playerKits= new List<PlayerKit>();
            if (CurTime()<21600)
            {
                
                foreach (var pkit in Kits.Where(z => (string.IsNullOrEmpty(z.permission) || permission.UserHasPermission(player.UserIDString, z.permission))))
                {
                    playerKits.Add(new PlayerKit
                    {
                        name = pkit.name,
                        amount = pkit.amount,
                        time = pkit.wipeDelay + CurTime()
                    });
                }
            }
            else
            {
                foreach (var pkit in Kits.Where(z => (string.IsNullOrEmpty(z.permission) || permission.UserHasPermission(player.UserIDString, z.permission))))
                {
                    playerKits.Add(new PlayerKit
                    {
                        name = pkit.name,
                        amount = pkit.amount,
                        time = CurTime()
                    });
                }
            }
            playerData.Add(player.userID,playerKits);
            SavePlayersData();
            
        }

        void SavePlayersData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("MKit/PlayerData",playerData);
           
        }

        List<string> GetPlayerKit(BasePlayer player)
        {
            if (!playerData.ContainsKey(player.userID))
            {
                CreatePlayerData(player);
            }
            ulong playerid = player.userID;
            List<string> playerkits = new List<string>();

            foreach (var kit in playerData[playerid])
            {
                playerkits.Add(kit.name);
            }
            
            return playerkits;
        }

        void RefreshPlayerKitCooldown()
        {
            timer.Every(1f, (() =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "Use");
                    foreach (var var in playerData[player.userID])
                    {
                        var container = new CuiElementContainer();
                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = $"0.36 0.24", AnchorMax = $"1 0.69", OffsetMax = "0 0" },
                            Image = { Color = "0 0 0 0" }
                        }, "Kits", "Use");

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                            Text = { Text = $"{var.time}\n{var.amount}", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                        }, "Use");
                        CuiHelper.AddUi(player, "Use");
                    }
                
                }
            }));
            
        }

        void OnPlayerConnected(BasePlayer player)
        {
            PlayerDataUpdate(player);
        }

        PlayerKit GetPlayerKit(BasePlayer player, string name)
        {
            PlayerKit currentKit = new PlayerKit();
            if (!playerData.ContainsKey(player.userID))
            {
                CreatePlayerData(player);
            }

            currentKit = playerData[player.userID].Find(p => p.name == name);

            return currentKit;
        } 

        #endregion

        #region helper

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            result = $"{time.Hours.ToString("00")}:";
            result += $"{time.Minutes.ToString("00")}:";
            result += $"{time.Seconds.ToString("00")}";
            return result;
        }

        double CurTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion
        #region UI

        void KitUI(BasePlayer player)
        {
            LoadKitData();
            
            PlayerDataUpdate(player);
            
            CuiHelper.DestroyUi(player, Layer);
            //RefreshPlayersData();
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.284 0", AnchorMax = "0.952 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.6" },
            }, "Menu_UI", Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.032 0.893", AnchorMax = $"0.347 0.954", OffsetMax = "0 0" },
                Image = { Color = "0.92 0.21 0 1" }
            }, Layer, "Title");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Наборы", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf" }
            }, "Title");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.36 0.893", AnchorMax = $"0.97 0.954", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = Description, Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Description");

            CuiHelper.AddUi(player, container);
            UI(player, "");
        }
        void UI(BasePlayer player, string name)
        {
            var container = new CuiElementContainer();

            if (name == "")
            {
                CuiHelper.DestroyUi(player, "Name");
                CuiHelper.DestroyUi(player, "Back");
                CuiHelper.DestroyUi(player, "Inventory");
                CuiHelper.DestroyUi(player, "Clothing");
                CuiHelper.DestroyUi(player, "HotBar");
                CuiHelper.DestroyUi(player, "Items");
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.03 0.75", AnchorMax = $"0.97 0.86", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "Banner");

                container.Add(new CuiElement
                {
                    Parent = "Banner",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Banner"), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });

                CuiHelper.AddUi(player, container);
                InterfaceKit(player);
            }
            else
            {
                CuiHelper.DestroyUi(player, "Kit");
                CuiHelper.DestroyUi(player, "Banner");
                var check = Kits.FirstOrDefault(z => z.name == name);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.15 0.8", AnchorMax = $"0.97 0.86", OffsetMax = "0 0" },
                    Image = { Color = "0.92 0.21 0 1" }
                }, Layer, "Name");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, Layer, "Items");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = check.name.ToUpper(), Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-bold.ttf" }
                }, "Name");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.03 0.8", AnchorMax = $"0.14 0.86", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0.6", Command = "kit back" },
                    Text = { Text = "Назад", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, Layer, "Back");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.03 0.72", AnchorMax = $"0.495 0.79", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "Inventory");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = "Инвентарь", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-regular.ttf" }
                }, "Inventory");

                float width = 0.0782f, height = 0.09f, startxBox = 0.028f, startyBox = 0.715f - height, xmin = startxBox, ymin = startyBox;
                for (int z = 0; z < 24; z++)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                        Button = { Color = "0 0 0 0.5", Command = $"" },
                        Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                    }, "Items");

                    xmin += width;
                    if (xmin + width + 0.45f >= 1)
                    {
                        xmin = startxBox;
                        ymin -= height;
                    }
                }

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.505 0.72", AnchorMax = $"0.97 0.79", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "Clothing");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = "Одежда", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-regular.ttf" }
                }, "Clothing");

                float width1 = 0.0782f, height1 = 0.09f, startxBox1 = 0.503f, startyBox1 = 0.715f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
                for (int z = 0; z < 6; z++)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                        Button = { Color = "0 0 0 0.5", Command = $"" },
                        Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                    }, "Items");

                    xmin1 += width1;
                    if (xmin1 + width1>= 1)
                    {
                        xmin1 = startxBox1;
                        ymin1 -= height1;
                    }
                }
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{startxBox1} {startyBox1-0.09}", AnchorMax = $"{startxBox1 + width1} {startyBox1 + height1 * 1-0.09}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = "0 0 0 0.5", Command = $"" },
                    Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, "Items");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.505 {0.538-width1-0.01}", AnchorMax = $"0.97 {0.622 - width1-0.01}", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "HotBar");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = "Пояс", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-regular.ttf" }
                }, "HotBar");

                float width2 = 0.0782f, height2 = 0.09f, startxBox2 = 0.503f, startyBox2 = 0.535f - height2, xmin2 = startxBox2, ymin2 = startyBox2;
                for (int z = 0; z < 6; z++)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{xmin2} {ymin2-width1-0.01}", AnchorMax = $"{xmin2 + width2} {ymin2 + height2 * 1-width1-0.01}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                        Button = { Color = "0 0 0 0.5", Command = $"" },
                        Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                    }, "Items");

                    xmin2 += width2;
                    if (xmin2 + width2>= 1)
                    {
                        xmin2 = startxBox2;
                        ymin2 -= height2;
                    }
                }

                float width3 = 0.0782f, height3 = 0.09f, startxBox3 = 0.028f, startyBox3 = 0.715f - height3, xmin3 = startxBox3, ymin3 = startyBox3;
                float width4 = 0.0782f, height4 = 0.09f, startxBox4 = 0.503f, startyBox4 = 0.715f - height4, xmin4 = startxBox4, ymin4 = startyBox4;
                float width5 = 0.0782f, height5 = 0.09f, startxBox5 = 0.503f, startyBox5 = 0.535f - height5, xmin5 = startxBox5, ymin5 = startyBox5;
                foreach (var item in check.kitItems)
                {
                    if (item.container == "main")
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{xmin3} {ymin3}", AnchorMax = $"{xmin3 + width3} {ymin3 + height3 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Button = { Color = "0 0 0 0", Command = $"" },
                            Text = { Text = $"x{item.amount} ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                        }, "Items", "Item");

                        container.Add(new CuiElement
                        {
                            Parent = "Item",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.shortname), FadeIn = 0.5f},
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "8 8", OffsetMax = "-8 -8" }
                            }
                        });

                        xmin3 += width3;
                        if (xmin3 + width3 + 0.45f >= 1)
                        {
                            xmin3 = startxBox3;
                            ymin3 -= height3;
                        }
                    }
                    if (item.container == "wear")
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{xmin4} {ymin4}", AnchorMax = $"{xmin4 + width4} {ymin4 + height4 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Button = { Color = "0 0 0 0", Command = $"" },
                            Text = { Text = $"x{item.amount} ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                        }, "Items", "Item");
                        
                        container.Add(new CuiElement
                        {
                            Parent = "Item",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.shortname), FadeIn = 0.5f },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "8 8", OffsetMax = "-8 -8" }
                            }
                        });

                        xmin4 += width4;
                        if (xmin4 + width4>= 1)
                        {
                            xmin4 = startxBox1;
                            ymin4 -= height1;
                        }
                    }
                    if (item.container == "belt")
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{xmin5} {ymin5-width1-0.01}", AnchorMax = $"{xmin5 + width5} {ymin5 + height5 * 1-width1-0.01}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Button = { Color = "0 0 0 0", Command = $"" },
                            Text = { Text = $"x{item.amount} ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                        }, "Items", "Item");

                        container.Add(new CuiElement
                        {
                            Parent = "Item",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.shortname), FadeIn = 0.5f },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "8 8", OffsetMax = "-8 -8" }
                            }
                        });

                        xmin5 += width5;
                    }
                }

                CuiHelper.AddUi(player, container);
            }
        }

        void InterfaceKit(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, "Kit");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" },
            }, Layer, "Kit");
            if (GetPlayerKit(player).Count > (page + 1)*6)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.85 0.03", AnchorMax = $"0.97 0.09", OffsetMax = "0 0" },
                    Button = { Color = "0.92 0.21 0 1", Command = Kits.Count() > (page + 1) * 6 ? $"kit skip {page + 1}" : "" },
                    Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
                }, "Kit");
            }

            if (page >= 1)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.72 0.03", AnchorMax = $"0.84 0.09", OffsetMax = "0 0" },
                    Button = { Color = "0.92 0.21 0 1", Command = page >= 1 ? $"kit skip {page - 1}" : "" },
                    Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
                }, "Kit");
            }
            

            float width = 0.472f, height = 0.2f, startxBox = 0.028f, startyBox = 0.72f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in Kits.Where(z => (string.IsNullOrEmpty(z.permission) || permission.UserHasPermission(player.UserIDString, z.permission))).Skip(page * 6).Take(6).ToList())
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                    Button = { Color = "0.38 0.37 0.38 0.6", Command = $"" },
                    Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, "Kit", "Kits");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.35 1", OffsetMax = "0 0" },
                    Image = { Color = "0.38 0.37 0.38 1" }
                }, "Kits", "KitImage");

                container.Add(new CuiElement
                {
                    Parent = $"KitImage",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.kitImage), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" }
                    }
                });

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.36 0.7", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, "Kits", "Name");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = check.name.ToUpper(), Align = TextAnchor.MiddleLeft, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, "Name");

                var db = GetPlayerKit(player, check.name);
                var Time = CurTime();
                var time = db.time > 0 && (db.time > Time) ? $"Доустпно через {FormatShortTime(TimeSpan.FromSeconds(db.time - Time))}" : "Можно взять";
                string amount = db.amount.ToString();
                if (db.amount>0)
                {
                    amount = $"{db.amount} использований осталось";
                }

                if (db.amount == 0)
                {
                    amount = "Available next wipe";
                }

                if (db.amount <= -1)
                {
                    amount = "";
                }
                if (amount == "Available next wipe")
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Kit",
                        Components = {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", nwName), FadeIn = 0.5f } ,
                            new CuiRectTransformComponent { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    
                        }});
                }
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.36 0.24", AnchorMax = $"1 0.69", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, "Kits", "Use");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{time}\n{amount}", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Use");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.37 0.03", AnchorMax = $"0.67 0.23", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 1", Command = $"kit previev {check.name}" },
                    Text = { Text = "Посмотреть", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Kits");

                if (amount != "Available next wipe" && db.time<Time)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.68 0.03", AnchorMax = $"0.98 0.23", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 1", Command = $"kit take {check.name}" },
                        Text = { Text = "Взять", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                    }, "Kits");
                }
                

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region chat

        [ConsoleCommand("kit")]
        void ConsoleKit(ConsoleSystem.Arg args)
        {
            
            var Time = CurTime();
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "take")
                {
                    var check = Kits.FirstOrDefault(z => z.name == args.Args[1]);
                    /*if (player.inventory.containerMain.itemList.Count >= 24 || player.inventory.containerWear.itemList.Count >= 7 || player.inventory.containerBelt.itemList.Count >= 6)
                    {
                        SendReply(player, "Not enough space");
                        return;
                    }*/
                    var db = GetPlayerKit(player, check.name);
                    
                    if (!CanRecieveKit(player,Kits.Find(p => p.name == db.name)))
                    {
                        SendReply(player, "Не хватает места");
                        return;
                    }
                    KitRedeem(player,check);
                    
                    if (db.time > Time)
                    {
                        SendReply(player, "Подождите");
                        return;
                    }
                    if (check.cooldown > 0)
                    {
                        //db.time = Time + check.cooldown;
                        playerData[player.userID].Find(p => p.name == db.name).time = Time+check.cooldown;
                        SavePlayersData();
                    };
                    if (check.amount == 0)
                    {
                        SendReply(player, "Вы не можете получить этот набор");
                        return;
                    }

                    playerData[player.userID].Find(p => p.name == db.name).amount -= 1;
                    //db.amount -= 1;
                    SavePlayersData();
                    InterfaceKit(player);
                    Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
                    EffectNetwork.Send(x, player.Connection);
                }
                if (args.Args[0] == "back")
                {
                    UI(player, "");
                }
                if (args.Args[0] == "previev")
                {
                    UI(player, args.Args[1]);
                }
                if (args.Args[0] == "skip")
                {
                    InterfaceKit(player, int.Parse(args.Args[1]));
                }
            }
        }

        #endregion
        #region admin

        List<ItemData> SimpleGetItemsToKit(BasePlayer player)
        {
            List<ItemData> itemList = new List<ItemData>();
            foreach (var items in player.inventory.containerBelt.itemList)
            {
                itemList.Add(new ItemData
                {
                    shortname = items.info.shortname,
                    amount = items.amount,
                    container = "belt",
                    skinId = items.skin
                });
            }
            foreach (var items in player.inventory.containerMain.itemList)
            {
                itemList.Add(new ItemData
                {
                    shortname = items.info.shortname,
                    amount = items.amount,
                    container = "main",
                    skinId = items.skin
                });
            }
            foreach (var items in player.inventory.containerWear.itemList)
            {
                itemList.Add(new ItemData
                {
                    shortname = items.info.shortname,
                    amount = items.amount,
                    container = "wear",
                    skinId = items.skin
                });
            }

            return itemList;
        }

        KitData GetNewKit(string name, int cd, int amount, string perm, List<ItemData> items,
            string url = "https://files.facepunch.com/s/84f338d00e7e.png")
        {
            KitData newKit = new KitData();
            newKit.name = name;
            newKit.amount = amount;
            newKit.cooldown = cd;
            newKit.permission = "mkitbr." + perm;
            newKit.kitItems = items;
            newKit.kitImage = url;
            return newKit;
        }

        [ChatCommand("addkit")]
        void AdminCreateKit(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args.Length<4)
            {
                SendReply(player,$"Используйте /addkit Название КД количество пермишен\n например /kit start 600 -1 use http:/image.com/ima.png");
                return;
            }

            string url = "https://files.facepunch.com/s/84f338d00e7e.png";

            string name = args[0];
            int cd = args[1].ToInt();
            int amount = args[2].ToInt();
            string perm = args[3];
            if (args.Length>4)
            {
                url = args[4];
            }
            KitData newKit = GetNewKit(name, cd, amount, perm, SimpleGetItemsToKit(player),url);
            Kits.Add(newKit);
            permission.RegisterPermission(newKit.permission,this);
            ImageLibrary.Call("AddImage", url, url);
            SaveKitData();

        }

        [ChatCommand("seturl")]
        void SetKitImage(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)return;
            if (args.Length<2)
            {
                SendReply(player,"Напишите название набора и url");
            }

            KitData kit =Kits.Find(p => p.name == args[0]);
            if (kit == null)
            {
                SendReply(player,$"Набор с именеи {args[0]} не существует");
                return;
            }

            kit.kitImage = args[1];
            ImageLibrary.Call("AddImage", args[1], args[1]);
            

        }

        [ConsoleCommand("add.kit")]
        void AddKitByAdmin(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;
            if (!player.IsAdmin) return;
            
            
        }

        #endregion
    }
}