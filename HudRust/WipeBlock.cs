using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WipeBlock", "VooDoo", "1.0.0")]
    [Description("WipeBlock and Tab for XMenu")]
    public class WipeBlock : RustPlugin
    {
        [PluginReference] Plugin XMenu;
        [PluginReference] Plugin Notifications;
        public static WipeBlock instance;

        #region ImageLibrary Addon
        [PluginReference] Plugin ImageLibrary;
        bool AddImage(string url, string imageName, ulong imageId, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);
        #endregion

        #region Config
        private Dictionary<string, int> blockTime = new Dictionary<string, int>();
        private PluginConfig config;
        private class PluginConfig
        {
            public Dictionary<string, Dictionary<int, List<string>>> blockedItems;

            public ColorConfig colorConfig;
            public string serverName;
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
                serverName = "<b>ЖИРНЫЙ <color=#D04425>RUST</color> X20 CLANS</b>",

                colorConfig = new PluginConfig.ColorConfig()
                {
                    menuContentHighlighting = "#0000007f",
                    menuContentHighlightingalternative = "#FFFFFF10",

                    menuContentTextAlternative = "#FFFFFFAA",
                    menuContentText = "#FFFFFFAA",

                    gradientColor = "#00000099",
                },

                blockedItems = new Dictionary<string, Dictionary<int, List<string>>>()
                {
                    ["Оружие"] = new Dictionary<int, List<string>>
                    {
                        [3600] = new List<string>
                        {
                            "crossbow",
                            "bow.compound",
                        },
                        [7200] = new List<string>
                        {
                            "shotgun.waterpipe",
                            "pistol.revolver",
                        },
                        [14400] = new List<string>
                        {
                            "pistol.semiauto",
                            "pistol.python",
                            "shotgun.double",
                            "shotgun.pump",
                        },
                        [28800] = new List<string>
                        {
                            "pistol.m92",
                            "shotgun.spas12",
                        },
                        [43200] = new List<string>
                        {

                            "rifle.semiauto",
                            "rifle.m39",
                        },
                        [57600] = new List<string>
                        {
                            "smg.2",
                            "smg.thompson",
                            "smg.mp5",
                            "surveycharge",
                            "grenade.beancan",
                            "grenade.f1",
                        },
                        [86400] = new List<string>
                        {
                            "rifle.bolt",
                            "rifle.ak",
                            "rifle.lr300",
                            "rifle.l96",
                            "lmg.m249",
                            "multiplegrenadelauncher",
                            "explosive.satchel",
                        },
                        [172800] = new List<string>
                        {
                            "rocket.launcher",
                            "explosive.timed"
                        },
                    },
                    ["Броня"] = new Dictionary<int, List<string>>
                    {
                        [57600] = new List<string>
                        {
                            "coffeecan.helmet",
                            "roadsign.jacket",
                            "roadsign.kilt"
                        },
                        [86400] = new List<string>
                        {
                            "metal.facemask",
                            "metal.plate.torso",
                            "heavy.plate.helmet",
                            "heavy.plate.jacket",
                            "heavy.plate.pants",
                        },
                    },
                    ["Боеприпасы"] = new Dictionary<int, List<string>>
                    {
                        [86400] = new List<string>
                        {
                            "ammo.rifle.explosive",
                        },
                        [172800] = new List<string>
                        {
                            "ammo.rocket.basic",
                            "ammo.rocket.fire",
                            "ammo.rocket.hv",
                        },
                    }
                }
            };
        }
        #endregion

        #region U'Mod Hook's
        private long SaveCreatedTime = 0;
        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            instance = this;
            SaveCreatedTime = Interface.Oxide.DataFileSystem.ReadObject<long>("WipeBlock/WipeTime");
            if (SaveCreatedTime == 0)
            {
                SaveCreatedTime = ToEpoch(DateTime.UtcNow);
                Interface.Oxide.DataFileSystem.WriteObject("WipeBlock/WipeTime", SaveCreatedTime);
            }

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "WipeBlock", "assets/icons/bullet.png", "RenderWipeBlock", null);
                    cmd.AddChatCommand("wipeblock", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true WipeBlock"));
                    TimerInitialize.Destroy();
                }
            });

            foreach (var category in config.blockedItems)
                foreach (var timeCategory in category.Value)
                    foreach (var item in timeCategory.Value)
                        blockTime.Add(item, timeCategory.Key);
        }

        private void OnNewSave()
        {
            SaveCreatedTime = ToEpoch(DateTime.UtcNow);
            Interface.Oxide.DataFileSystem.WriteObject("WipeBlock/WipeTime", SaveCreatedTime);
        }

        private bool? CanWearItem(PlayerInventory inventory, Item item)
        {

            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (!player.userID.IsSteamId()) return null;

            var isBlocked = IsBlocked(item.info) > 0 ? false : (bool?)null;
            if (isBlocked == false) DrawBlock(player, item);
            return isBlocked;
        }

        private bool? CanEquipItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null || !player.userID.IsSteamId()) return null;
            if (player.IsAdmin) return null;

            var isBlocked = IsBlocked(item.info) > 0 ? false : (bool?)null;
            if (isBlocked == false) DrawBlock(player, item);
            return isBlocked;
        }

        private object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            if (!player.userID.IsSteamId()) return null;

            var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?)null;
            if (isBlocked == false) Notifications.Call("API_AddUINote", player.userID, $"Вы не можете использовать этот тип боеприпасов!");
            return isBlocked;
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (!player.userID.IsSteamId()) return;

            var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?)null;
            if (isBlocked == false)
            {
                projectile.primaryMagazine.contents = 0;
                projectile.GetItem().LoseCondition(projectile.GetItem().maxCondition);
                projectile.SendNetworkUpdate();
                player.SendNetworkUpdate();
                Item ammo = ItemManager.CreateByItemID(projectile.primaryMagazine.ammoType.itemid, 1, 0);
                Notifications.Call("API_AddUINote", player.userID, $"Хорошая попытка, правда ваше оружие теперь сломано!");
            }
        }


        private object OnReloadMagazine(BasePlayer player, BaseProjectile projectile)
        {
            if (!player.userID.IsSteamId())
                return null;
            if (player.IsAdmin)
                return null;

            NextTick(() =>
            {
                var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?)null;
                if (isBlocked == false)
                {
                    projectile.primaryMagazine.contents = 0;
                    projectile.GetItem().LoseCondition(projectile.GetItem().maxCondition);
                    projectile.SendNetworkUpdate();
                    player.SendNetworkUpdate();
                    Item ammo = ItemManager.CreateByItemID(projectile.primaryMagazine.ammoType.itemid, 1, 0);
                    Notifications.Call("API_AddUINote", player.userID, $"Хорошая попытка, правда ваше оружие теперь сломано!");
                }
            });
            return null;
        }

        private void DrawBlock(BasePlayer player, Item item)
        {
            string inputText = "Предмет {name} временно заблокирован, подождите {1}".Replace("{name}", item.info.displayName.english).Replace("{1}", $"{Convert.ToInt32(Math.Floor(TimeSpan.FromSeconds(IsBlocked(item.info)).TotalHours))} час. {TimeSpan.FromSeconds(IsBlocked(item.info)).Minutes} минут.");
            Notifications.Call("API_AddUINote", player.userID, inputText);
        }
        #endregion

        #region UI
        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion

        private void RenderWipeBlock(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
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

            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Title",
                Parent = MenuContent,
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"<color={config.colorConfig.menuContentText}><b>ВАЙПОВАЯ БЛОКИРОВКА ПРЕДМЕТОВ НА СЕРВЕРЕ</b> <color={config.colorConfig.menuContentTextAlternative}>{config.serverName}</color>\n<size=24>Вайп сервера был произведён: {epoch.AddSeconds(SaveCreatedTime).ToString("dd-MM-yyyy")}</size></color>",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 30,
                            Font = "robotocondensed-regular.ttf",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"0 -80",
                            OffsetMax = $"920 0",
                        }
                    }
            });
            for (int i = 0, x = 0, y = 3; i < config.blockedItems.Count; i++, x = 0)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Category_{i}",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"<color={config.colorConfig.menuContentText}>{config.blockedItems.ElementAt(i).Key}</color>",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 16,
                            Font = "robotocondensed-regular.ttf",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"0 {50 - y * 55}",
                            OffsetMax = $"920 {100 - y * 55}",
                        }
                    }
                });
                y++;
                for (int j = 0; j < config.blockedItems.ElementAt(i).Value.Count; j++)
                {
                    for (int k = 0; k < config.blockedItems.ElementAt(i).Value.ElementAt(j).Value.Count; k++, x++)
                    {
                        if (x == 15)
                        {
                            x = 0;
                            y++;
                        }

                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Item_{k}",
                            Parent = MenuContent,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.menuContentHighlightingalternative),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{35 + x * 55} {50 - y * 55}",
                                    OffsetMax = $"{85 + x * 55} {100 - y * 55}"
                                }
                            }
                        });
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Item_{k}",
                            Parent = MenuContent,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImage(config.blockedItems.ElementAt(i).Value.ElementAt(j).Value.ElementAt(k)),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{35 + x * 55} {50 - y * 55}",
                                    OffsetMax = $"{85 + x * 55} {100 - y * 55}"
                                }
                            }
                        });
                        string text = IsBlocked(config.blockedItems.ElementAt(i).Value.ElementAt(j).Value.ElementAt(k)) > 0
                        ? $"<color=#FFFFFFB3><size=9>ОСТАЛОСЬ\nЖДАТЬ:</size></color>\n\n<color=#FFAA00FF><size=11>{TimeSpan.FromSeconds((int)IsBlocked(config.blockedItems.ElementAt(i).Value.ElementAt(j).Value.ElementAt(k))).ToShortString()}</size></color>"
                        : "";
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + $".Item_{k}_Title",
                            Parent = MenuContent,
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color={config.colorConfig.menuContentText}>{text}</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 6,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{35 + x * 55} {50 - y * 55}",
                                    OffsetMax = $"{85 + x * 55} {100 - y * 55}"
                                }
                            }
                        });
                    }
                }
                y++;
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

        private double IsBlocked(ItemDefinition itemDefinition) => IsBlocked(itemDefinition.shortname);
        private double IsBlocked(string shortname)
        {
            if (!blockTime.ContainsKey(shortname))
                return 0;

            var lefTime = blockTime[shortname] + SaveCreatedTime - CurrentTime();

            return lefTime > 0 ? lefTime : 0;
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }
        private static string ToShortString(TimeSpan timeSpan)
        {
            int i = 0;
            string resultText = "";
            if (timeSpan.Days > 0)
            {
                resultText += timeSpan.Days + " День";
                i++;
            }
            if (timeSpan.Hours > 0 && i < 2)
            {
                if (resultText.Length != 0)
                    resultText += " ";
                resultText += timeSpan.Days + " Час";
                i++;
            }
            if (timeSpan.Minutes > 0 && i < 2)
            {
                if (resultText.Length != 0)
                    resultText += " ";
                resultText += timeSpan.Days + " Мин.";
                i++;
            }
            if (timeSpan.Seconds > 0 && i < 2)
            {
                if (resultText.Length != 0)
                    resultText += " ";
                resultText += timeSpan.Days + " Сек.";
                i++;
            }

            return resultText;
        }

        private long ToEpoch(DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        #endregion
    }
}