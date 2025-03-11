using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Xcrafting", "Sparkless", "0.0.2")]
    public class Xcraft : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        private ConfigData Settings { get; set; }

        public class ShopItems
        {
            [JsonProperty("Shortname предмета")] public string ShortName;
            [JsonProperty("Кол-во")] public int Amount;
        }


        class ConfigData
        {

            [JsonProperty("Можно ли устанавливать переработчик на структурах?")]
            public bool Structure = true;
            [JsonProperty("Можно ли устанавливать переработчик на землю?")]
            public bool Ground = false;
            [JsonProperty("Включить/Выключить подбор переработчика")]
            public bool Available = true;
            [JsonProperty("Запрещать устанавливать переработчик в зоне действия чужого шкафа")]
            public bool Privelege = true;
            
            [JsonProperty("Ресурсы для крафта коптера(макс 6)")]
            public List<ShopItems> ShopItem { get; set; }

            [JsonProperty("Ресурсы для крафта переработчика(макс 6)")]
            public List<ShopItems> ShopItemrec { get; set; }

            public static ConfigData GetNewCong()
            {
                ConfigData newConfig = new ConfigData();

                newConfig.ShopItem = new List<ShopItems>
                {
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                };
                newConfig.ShopItemrec = new List<ShopItems>
                {
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                    new ShopItems()
                    {
                        ShortName = "hq.metal.ore",
                        Amount = 100,
                    },
                };
                return newConfig;
            }
        }
        
        protected override void LoadDefaultConfig() => Settings = ConfigData.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(Settings);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<ConfigData>();
                if (Settings?.ShopItem == null) LoadDefaultConfig();
                if (Settings?.ShopItemrec == null) LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        void OnServerInitialized()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<Recycler>();
            foreach (var r in allobjects)
            {
                if (r.OwnerID != 0 && r.gameObject.GetComponent<RecyclerEntity>() == null)
                    r.gameObject.AddComponent<RecyclerEntity>();
            }

            foreach (var check in Settings.ShopItem)
            {
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check.ShortName}.png",
                    check.ShortName);
            }
            ImageLibrary.Call("AddImage", "https://i.imgur.com/AptcbtT.png", "CopterImage");
            ImageLibrary.Call("AddImage", "https://imgur.com/QEUXtZJ.png", "RecyclerImage");
        }

        [ChatCommand("craft")]
        void cmdcraftopen(BasePlayer player)
        {
            opencraftmenu(player, 1, true);
        }


        private string Layer = "LayerMenu";

        void opencraftmenu(BasePlayer player, int page, bool first = false)
        {
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            
            
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#202020C2"), Material = "assets/content/ui/uibackgroundblur.mat"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", Layer);
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color =  "0.5176471 0.5176471 0.5176471 0.3137255"},
                    new CuiRectTransformComponent {AnchorMin = "0.2432291 0.1870371", AnchorMax = "0.7473958 0.8101852"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.438021 0.5361111", AnchorMax = "0.5499998 0.7370371" },
                Button = { Color = "0.5176471 0.5176471 0.5176471 0.3137255" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer, ".Images");
 
            container.Add(new CuiElement
            {
                Parent = ".Images",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage",  "CopterImage") },
                    new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95", OffsetMax = "0 0" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "0.9529412 0.9529412 0.9529412 0.3529412", FadeIn = 0.25f, Text = "КРАФТ МИНИКОПТЕРА", FontSize = 25, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.2447917 0.7388889", AnchorMax = "0.7432292 0.8092592" },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "0.9529412 0.9529412 0.9529412 0.3529412", FadeIn = 0.25f, Text = "РЕСУРСЫ ДЛЯ КРАФТА", FontSize = 20, Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.2442709 0.4925926", AnchorMax = "0.746875 0.5333333" },
                }
            });
            foreach (var check in Settings.ShopItem.Select((i, t) => new {A = i, B = t - (page - 1) * 6})
                .Skip((page - 1) * 6).Take(6))
            {
                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.2734405 + check.B * 0.075 - Math.Floor((double) check.B / 6) * 6 * 0.075} {0.3412039 - Math.Floor((double) check.B / 6) * 0.18}",
                            AnchorMax =
                                $"{0.3421903 + check.B * 0.075 - Math.Floor((double) check.B / 6) * 6 * 0.075} {0.46713 - Math.Floor((double) check.B / 6) * 0.18}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = "1 1 1 0.01", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Command = $""
                        },
                        Text =
                        {
                            Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 15
                        }
                    }, Layer, Layer + $".{check.B}");

                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}",
                    Name = Layer + $".{check.B}.Img",
                    Components =
                    {
                        new CuiRawImageComponent
                            {FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", check.A.ShortName)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}",
                    Name = Layer + $".{check.B}.Txt",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"x{check.A.Amount}", Align = TextAnchor.LowerRight,
                            Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 0.6"
                        },
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
            }
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2432292 0.8240736", AnchorMax = "0.7473959 0.9166662" },
                Button = { Close = Layer, Command = "recyclercraft", Color = "0.5176471 0.5176471 0.5176471 0.3137255", FadeIn = 0.1f},
                Text = { Text = "КРАФТ ПЕРЕРАБОТЧИКА", FontSize = 50, Align = TextAnchor.MiddleCenter, Color = "0.9529412 0.9529412 0.9529412 0.3529412", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4380207 0.2120358", AnchorMax = "0.55 0.2657412" },
                Button = { Close = Layer, Command = "coptercraft", Color = "0.5176471 0.5176471 0.5176471 0.3137255", FadeIn = 0.1f},
                Text = { Text = "СКРАФТИТЬ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.9529412 0.9529412 0.9529412 0.3529412", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.82 0.9", AnchorMax = "0.98 0.96", OffsetMax = "0 0" },
                Button = { FadeIn = 2f, Color = HexToCuiColor("#FFFFFF2C"), Close = Layer},
                Text = { Text = "ВЫЙТИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15 }
            }, Layer);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("recyclercraft")]
        void cmdopenrecyclercraft(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            openmenucraftrec(player, 1, true);
        }

        void openmenucraftrec(BasePlayer player, int page, bool first = false)
        {
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            
            
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#202020C2"), Material = "assets/content/ui/uibackgroundblur.mat"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", Layer);
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color =  "0.5176471 0.5176471 0.5176471 0.3137255"},
                    new CuiRectTransformComponent {AnchorMin = "0.2432291 0.1870371", AnchorMax = "0.7473958 0.8101852"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.438021 0.5361111", AnchorMax = "0.5499998 0.7370371" },
                Button = { Color = "0.5176471 0.5176471 0.5176471 0.3137255" },
                Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer, ".Images");
 
            container.Add(new CuiElement
            {
                Parent = ".Images",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage",  "RecyclerImage") },
                    new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95", OffsetMax = "0 0" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "0.9529412 0.9529412 0.9529412 0.3529412", FadeIn = 0.25f, Text = "КРАФТ ПЕРЕРАБОТЧИКА ", FontSize = 25, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.2447917 0.7388889", AnchorMax = "0.7432292 0.8092592" },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = "0.9529412 0.9529412 0.9529412 0.3529412", FadeIn = 0.25f, Text = "РЕСУРСЫ ДЛЯ КРАФТА", FontSize = 20, Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.2442709 0.4925926", AnchorMax = "0.746875 0.5333333" },
                }
            });
            foreach (var check in Settings.ShopItemrec.Select((i, t) => new {A = i, B = t - (page - 1) * 6})
                .Skip((page - 1) * 6).Take(6))
            {
                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.2734405 + check.B * 0.075 - Math.Floor((double) check.B / 6) * 6 * 0.075} {0.3412039 - Math.Floor((double) check.B / 6) * 0.18}",
                            AnchorMax =
                                $"{0.3421903 + check.B * 0.075 - Math.Floor((double) check.B / 6) * 6 * 0.075} {0.46713 - Math.Floor((double) check.B / 6) * 0.18}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = "1 1 1 0.01", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            Command = $""
                        },
                        Text =
                        {
                            Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 15
                        }
                    }, Layer, Layer + $".{check.B}");

                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}",
                    Name = Layer + $".{check.B}.Img",
                    Components =
                    {
                        new CuiRawImageComponent
                            {FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", check.A.ShortName)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });

                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}",
                    Name = Layer + $".{check.B}.Txt",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"x{check.A.Amount}", Align = TextAnchor.LowerRight,
                            Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 0.6"
                        },
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
            }
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2432292 0.8240736", AnchorMax = "0.7473959 0.9166662" },
                Button = { Close = Layer, Command = "chat.say /craft", Color = "0.5176471 0.5176471 0.5176471 0.3137255", FadeIn = 0.1f},
                Text = { Text = "КРАФТ КОПТЕРА", FontSize = 50, Align = TextAnchor.MiddleCenter, Color = "0.9529412 0.9529412 0.9529412 0.3529412", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4380207 0.2120358", AnchorMax = "0.55 0.2657412" },
                Button = { Close = Layer, Command = "craftrecycler", Color = "0.5176471 0.5176471 0.5176471 0.3137255", FadeIn = 0.1f},
                Text = { Text = "СКРАФТИТЬ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.9529412 0.9529412 0.9529412 0.3529412", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            
            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.82 0.9", AnchorMax = "0.98 0.96", OffsetMax = "0 0" },
                    Button = { FadeIn = 2f, Color = HexToCuiColor("#FFFFFF2C"), Close = Layer},
                    Text = { Text = "ВЫЙТИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15 }
                }, Layer);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("coptercraft")]
        private void cmdcoptercraft(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            foreach (var ct in Settings.ShopItem.Select((i, t) => new {A = i, B = t}))
            {
                int HaveCount = player.inventory.GetAmount(ItemManager.FindItemDefinition(ct.A.ShortName).itemid);

                if (HaveCount < ct.A.Amount)
                {
                    SendReply(player, $"Вам не хватает: {ItemManager.FindItemDefinition(ct.A.ShortName).displayName.english}: {ct.A.Amount - HaveCount}");
                    return;
                }
            }

            foreach (var ct in Settings.ShopItem.Select((i, t) => new {A = i, B = t}))
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(ct.A.ShortName).itemid, ct.A.Amount);
            }
            GiveCopter(player);
            SendReply(player, "<color=#7f35ff>Вы успешно скрафтили коптер!</color>");
        }
        
        
        [ConsoleCommand("craftrecycler")]
        private void cmdrecyclercraft(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            foreach (var ct in Settings.ShopItemrec.Select((i, t) => new {A = i, B = t}))
            {
                int haveCount = player.inventory.GetAmount(ItemManager.FindItemDefinition(ct.A.ShortName).itemid);

                if (haveCount < ct.A.Amount)
                {
                    SendReply(player, $"Вам не хватает: {ItemManager.FindItemDefinition(ct.A.ShortName).displayName.english}: {ct.A.Amount - haveCount} шт");
                    return;
                }
            }
            foreach (var ct in Settings.ShopItemrec.Select((i, t) => new {A = i, B = t}))
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(ct.A.ShortName).itemid, ct.A.Amount);
            }
            GiveRecycler(player);
            SendReply(player,$"<color=#7f35ff>Вы успешно скрафтили переработчик</color>");
        }
        object CanStackItem(Item item, Item anotherItem)
        {
            if (item.info.itemid == 833533164 && item.skin == 1321253094)
                return false;
            if (item.info.itemid == 833533164 && item.skin == 1663370375)
                return false;
            return null;
        }
        
        bool GiveRecycler(ItemContainer container) 
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1321253094);
            item.name = "Переработчик";
            return item.MoveToContainer(container, -1, false);
        }

        bool GiveCopter(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1663370375);
            item.name = "Коптер";
            if (!player.inventory.GiveItem(item)) {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }
        bool GiveRecycler(BasePlayer player) 
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1321253094);
            item.name = "Переработчик";
            if (!player.inventory.GiveItem(item)) {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }   
        bool Check(BaseEntity entity)
        {
            GroundWatch component = entity.gameObject.GetComponent<GroundWatch>();
            List<Collider> list = Facepunch.Pool.GetList<Collider>();
            Vis.Colliders<Collider>(entity.transform.TransformPoint(component.groundPosition), component.radius, list, component.layers, QueryTriggerInteraction.Collide);
            foreach (Collider collider in list)
            {
                if (!(collider.transform.root == entity.gameObject.transform.root))
                {
                    BaseEntity baseEntity = collider.gameObject.ToBaseEntity();
                    if ((!(bool)(baseEntity) || !baseEntity.IsDestroyed && !baseEntity.isClient) && baseEntity is BuildingBlock)
                    {
                        Facepunch.Pool.FreeList<Collider>(ref list);
                        return true;
                    }
                }
            }
            Facepunch.Pool.FreeList<Collider>(ref list);
            return true;
        }
        
        private class RecyclerEntity : MonoBehaviour
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
        
        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            var player = plan.GetOwnerPlayer();
            var entity = obj.ToBaseEntity();
            if (entity == null) return;

            var ePos = entity.transform.position;
            RaycastHit rHit;

            if (entity != null && entity.ShortPrefabName == "box.wooden.large" && entity.skinID == 1663370375L)
            {
                MiniCopter minicopter = GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab",entity.transform.position, entity.transform.rotation) as MiniCopter;
                minicopter.Spawn();
                entity.Kill();
            }

            if (entity != null && entity.ShortPrefabName == "box.wooden.large" && entity.skinID == 1321253094L)
            {
                if (Physics.Raycast(new Vector3(ePos.x, ePos.y + 1, ePos.z), Vector3.down, out rHit, 2f, LayerMask.GetMask("Construction")) && rHit.GetEntity() != null)
                {
                    if (!Settings.Structure)
                    {
                        SendReply(player, "Переработчики нельзя ставить на строения (фундамент, потолки и тд)");
                        GiveRecycler(player);
                        entity.Kill();
                        return;
                    }
                }
                else
                {
                    if (!Settings.Ground)
                    {
                        GiveRecycler(player);
                        SendReply(player, "Нельзя ставить на землю");
                        entity.Kill();
                        return;
                    }
                }
                Recycler recycler = GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab", entity.transform.position, entity.transform.rotation) as Recycler;
                recycler.Spawn();
                entity.Kill();
                recycler.gameObject.AddComponent<RecyclerEntity>();
            }
        } 
        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            var entity = info.HitEntity;
            if (entity == null) return;
 
            if (!entity.ShortPrefabName.Contains("recycler")) return;
 
            if (!Settings.Available)
            {
                SendReply(player, "Подбор переработчика запрещен!");
                return;
            }
            if (Settings.Privelege && !player.IsBuildingAuthed())
            {
                SendReply (player, "Вам нужно право на строительство чтобы подобрать переработчик");
                return;
            }
            if (GiveRecycler(player))
            {
                SendReply(player, "Вы успешно подобрали переработчик!");
            }
            else
            {
                SendReply(player, "У вас недостаточно места в инвентаре!");
            }
 
            info.HitEntity.Kill();
        }
        private static string HexToCuiColor(string hex)
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
    }
}