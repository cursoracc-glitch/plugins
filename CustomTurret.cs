using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CustomTurret", "BadMandarin", "1.0.0")]
    [Description("CustomTurret")]
    class CustomTurret : RustPlugin
    {
        #region Classes
        private class PluginConfig
        {
            [JsonProperty("Сколько берёт патронов за спрей")]
            public int TakeAmmo = 10;
            [JsonProperty("Дистанция наводки")]
            public float Distance = 100f;
            [JsonProperty("Здоровье турели (стандарт 1000)")]
            public float Health = 1000f;
            [JsonProperty("Прицеливание (стандарт 4 обычная турель) чем меньше тем лучше")]
            public float AimCone = 4f;
            [JsonProperty("Множитель урона (стандарт 1f)")]
            public float DamageScale = 1f;
            [JsonProperty("Ресурсы чтобы скрафтить")]
            public Dictionary<string, int> ResourcesToCraft;
        }
        #endregion

        #region Variables
        ulong skinid = 1587601905;
        int ammoid = -1211166256;
        string turretPrefab = "assets/content/props/sentry_scientists/sentry.scientist.static.prefab";
        
        private PluginConfig config;
        private static CustomTurret plugin;

        string permSet = "";
        string permCraft = "";

        [PluginReference]
        Plugin ImageLibrary;
        string sentryimage;

        private List<uint> customTurrets;
        #endregion

        #region Oxide
        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        private void OnServerInitialized()
        {
            PrintWarning("Start plugin. Author: BadMandarin.");
            CheckSentry();

            permSet = "customturret.set";
            if (!permission.PermissionExists(permSet))
                permission.RegisterPermission(permSet, this);
            permCraft = "customturret.craft";
            if (!permission.PermissionExists(permCraft))
                permission.RegisterPermission(permCraft, this);

            sentryimage  = "https://i.imgur.com/LVfBuXF.png";
            string name = GetNameByURL(sentryimage);
            if (!(bool)ImageLibrary?.Call("HasImage", name))
            {
                ImageLibrary?.Call("AddImage", sentryimage, name);
            }
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if(info.Initiator is NPCAutoTurret && entity.OwnerID != 0)
            {
                info.damageTypes.ScaleAll(config.DamageScale);
            }
            return null;
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            NPCAutoTurret turret = info.HitEntity as NPCAutoTurret;
            if (turret != null)
            {
                if (!turret.IsOffline())
                {
                    turret.InitiateShutdown();
                }
                else
                {
                    turret.InitiateStartup();
                }
                if (turret.health < turret._maxHealth)
                    TurretRepair(player, turret);
            }
            return null;
        }

        private void TurretRepair(BasePlayer player, NPCAutoTurret turret)
        {
            int repairCost = (int)((turret._maxHealth - turret.health) * 0.1);
            if (repairCost == 0) return;
            Item itemRepair = ItemManager.CreateByName("metal.refined", repairCost);
            int itemamount = player.inventory.GetAmount(itemRepair.info.itemid);
            if (itemamount == 0)
            {
                SendReply(player, "Недостаточно ресурсов для починки!");
            }
            else if(itemamount < repairCost)
            {
                List<Item> itemList = player.inventory.containerMain.itemList.FindAll(x => x.info.shortname == itemRepair.info.shortname);
                player.inventory.Take(itemList, itemRepair.info.itemid, itemamount);
                turret.health += itemamount*10;
                turret.SendNetworkUpdate();
                SendReply(player, "Вы успешно починили турель!");
            }
            else
            {
                List<Item> itemList = player.inventory.containerMain.itemList.FindAll(x => x.info.shortname == itemRepair.info.shortname);
                player.inventory.Take(itemList, itemRepair.info.itemid, repairCost);
                turret.health = turret._maxHealth;
                turret.SendNetworkUpdate();
                SendReply(player, "Вы успешно починили турель!");
            }
        }

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            BaseEntity entity = gameobject.ToBaseEntity();
            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null || entity == null) return;

            if (entity.skinID != skinid) return;

            entity.Kill();
            
            Vector3 ePos = entity.transform.position;

            BaseEntity Turret = GameManager.server.CreateEntity(turretPrefab, ePos, entity.transform.rotation, true);
            
            RaycastHit rHit;

            if (Physics.Raycast(new Vector3(ePos.x, ePos.y + 1, ePos.z), Vector3.down, out rHit, 2f, LayerMask.GetMask(new string[] { "Construction" })) && rHit.GetEntity() == null)
            {
                SendReply(player, "Турель может быть установлена только на фундамент!");
                GiveSentry(player);
                Turret.Kill();
                return;
            }
            
            if (!permission.UserHasPermission(player.UserIDString, permSet))
            {
                SendReply(player, "Вы не можете устанавливать продвинутую турель!");
                GiveSentry(player);
                Turret.Kill();
                return;
            }

            Turret.OwnerID = player.userID;
            Turret.Spawn();
            var t = Turret as NPCAutoTurret;
            t.InitiateShutdown();
            t.SetPeacekeepermode(false);
            t.aimCone = config.AimCone;
            t.sightRange = config.Distance;
            t.inventory.capacity = 6;
            Turret.gameObject.AddComponent<ExtendedSentryComponent>();
            SetupProtection(t);
            t.SendNetworkUpdate();
            t.SendNetworkUpdateImmediate();
        }

        private void SetupProtection(BaseCombatEntity turret)
        {
            float health = config.Health;
            turret._maxHealth = health;
            turret.health = health;

            turret.baseProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
            turret.baseProtection.amounts = new float[]
            {
                1,1,1,1,1,0.8f,1,1,1,0.9f,0.5f,
                0.5f,1,1,0,0.5f,0,1,1,0,1,0.9f
            };
        }

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if(turret is NPCAutoTurret && turret.OwnerID != 0)
            {
                if (turret.inventory.HasAmmo(Rust.AmmoTypes.RIFLE_556MM))
                {
                    var con = turret.inventory.itemList;
                    Item itm = con.First();
                    //int amount = turret.inventory.GetAmount(ammoid, true);
                    if (itm.amount > config.TakeAmmo) itm.amount -= config.TakeAmmo;
                    else itm.RemoveFromContainer();
                    return null;
                }
                return false;
            }

            return null;
        }

        bool CanPickupEntity(BasePlayer player, BaseCombatEntity entity)
        {
            if(entity is NPCAutoTurret && entity.OwnerID != 0)
            {
                var tur = entity as NPCAutoTurret;
                entity.skinID = skinid;
            }
            return true;
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if(item.info.shortname.Contains("autoturret") && item.skin != 0)
            {
                item.condition = 100;
            }
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.info.shortname.Contains("autoturret"))
                if (item.skin != 0 || targetItem.skin != 0)
                    return false;
            
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.item.info.shortname.Contains("autoturret"))
                if (item.skinID != 0 || targetItem.skinID != 0)
                    return false;

            return null;
        }
        #endregion

        #region Interface
        string UI_Layer = "UI_SentryCraft";
        private void DrawUi(BasePlayer player)
        {
            string name = GetNameByURL(sentryimage);
            string ID = (string)ImageLibrary?.Call("GetImage", name);
            if (ID == "")
                ID = (string)ImageLibrary?.Call("GetImage", name) ?? ID;
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },

                        Image = { Color = GetColor("#000000", 0.95f) }
                    },
                    "Overlay", UI_Layer
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"ПРОДВИНУТАЯ ТУРЕЛЬ",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 26,
                                Font = "robotocondensed-bold.ttf",
                                Color = GetColor("#FFFFFF", 1f)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.6",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.1 -0.1", Color = GetColor("#000000")}
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "14330 14330" },
                        Button = { Color = "0 0 0 0", Close = UI_Layer },
                        Text = { Text = "" }
                    }, UI_Layer
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = ID
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = $"0.5 0.5",
                                OffsetMin = "-100 -100",
                                OffsetMax = "100 200"
                            },
                        }
                    }
                },
                new CuiElement
                {
                    Parent = $"{UI_Layer}",
                    Name = $"{UI_Layer}.Craft",
                    Components =
                    {
                        new CuiImageComponent {
                            Color = GetColor("#6E6E6E"), Material = "assets/content/ui/uibackgroundblur.mat"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.15",
                            AnchorMax = "0.5 0.15",
                            OffsetMin = "-100 -25",
                            OffsetMax = "100 25"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"sentry.craft" },
                        Text = { Text = "СКРАФТИТЬ", Align = TextAnchor.MiddleCenter, FontSize = 26 }
                    }, $"{UI_Layer}.Craft"
                },
            };

            int counter = 0;
            float max = config.ResourcesToCraft.Count;
            foreach (var item in config.ResourcesToCraft)
            {
                ItemDefinition def = ItemManager.FindItemDefinition(item.Key);
                bool hasItem = player.inventory.GetAmount(def.itemid) > item.Value ? true : false;
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = $"{UI_Layer}.ItemNeed.{counter}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = GetColor(hasItem ? "#81F781" : "#FA5858", 0.6f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.5 - max*0.01} 0.3",
                            AnchorMax = $"{0.5 - max*0.01} 0.3",
                            OffsetMin = $"{-25 + counter*55} -25",
                            OffsetMax = $"{25 + counter*55} 25"
                        },
                    }
                });
                ID = (string)ImageLibrary?.Call("GetImage", item.Key);
                if (ID == "")
                    ID = (string)ImageLibrary?.Call("GetImage", item.Key) ?? ID;
                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.ItemNeed.{counter}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = ID
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.ItemNeed.{counter}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"x{item.Value}", Align = TextAnchor.LowerCenter, FontSize = 12
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                    }
                });
                counter++;
            }



            CuiHelper.DestroyUi(player, UI_Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Commands
        [ChatCommand("sentry")]
        void Chat_ShowTop(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, permCraft))
                DrawUi(player);
            else SendReply(player, "В доступе отказано!");
        }

        [ConsoleCommand("sentry.craft")]
        private void SentryCraft(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, UI_Layer);
            var n = player.inventory.containerMain.itemList;
            foreach (var r in config.ResourcesToCraft)
            {
                var def = ItemManager.FindItemDefinition(r.Key);
                if (player.inventory.GetAmount(def.itemid) < r.Value)
                {
                    SendReply(player, "Недостаточно ресурсов");
                    return;
                }
            }

            foreach (var r in config.ResourcesToCraft)
            {
                var g = n.FindAll(x => x.info.shortname == r.Key);
                player.inventory.containerMain.Take(g, g[0].info.itemid, r.Value);
            }

            GiveSentry(player);
            SendReply(player, "Вы успешно скрафтили продвинутую турель!");
        }

        [ConsoleCommand("sentry.add")]
        private void AddSentry(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                SendError(arg, "[Ошибка] У вас нет доступа к этой команде!");
                return;
            }

            if (!arg.HasArgs())
            {
                PrintError(":\n[Ошибка] Введите sentry.add steamid/nickname\n[Пример] sentry.add Имя\n[Пример] sentry.add 76561198311240000");
                return;
            }

            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            if (player == null)
            {
                PrintError($"[Ошибка] Не удается найти игрока {arg.Args[0]}");
                return;
            }

            GiveSentry(player);
        }
        #endregion

        #region Utils
        private string GetNameByURL(string url)
        {
            var splitted = url.Split('/');
            var endUrl = splitted[splitted.Length - 1];
            var name = endUrl.Split('.')[0];
            return name;
        }

        private static string GetColor(string hex, float alpha = 1f)
        {
            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;
            //var a = Convert.ToInt16(color.A) / 4330f;
            return $"{r} {g} {b} {alpha}";
        }

        private void GiveSentry(BasePlayer player)
        {
            Item s = ItemManager.CreateByName("autoturret", 1, skinid);
            s.MoveToContainer(player.inventory.containerMain);
        }

        private void GiveSentry(Vector3 position)
        {
            Item s = ItemManager.CreateByName("autoturret", 1, skinid);
            s.Drop(position, Vector3.down);
        }

        private void CheckSentry()
        {
            foreach (var sentry in UnityEngine.Object.FindObjectsOfType<NPCAutoTurret>())
            {
                if (sentry.OwnerID != 0 && sentry.GetComponent<ExtendedSentryComponent>() == null)
                {
                    sentry.gameObject.AddComponent<ExtendedSentryComponent>();
                    SetupProtection(sentry);
                    sentry.aimCone = config.AimCone;
                    sentry.sightRange = config.Distance;
                    sentry.inventory.capacity = 6;
                    sentry.SendNetworkUpdate();
                    sentry.SendNetworkUpdateImmediate();
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig()
            {
                ResourcesToCraft = new Dictionary<string, int>()
                {
                    ["wood"] = 100,
                    ["metal.refined"] = 50
                }
            };
        }

        private class ExtendedSentryComponent : MonoBehaviour
        {
            private NPCAutoTurret sentry;

            private void Awake()
            {
                sentry = GetComponent<NPCAutoTurret>();
                InvokeRepeating("CheckGround", 5f, 5f);
            }

            private void CheckGround()
            {
                RaycastHit rhit;
                var cast = Physics.Raycast(sentry.transform.position + new Vector3(0, 0.1f, 0), Vector3.down, out rhit, 4f, LayerMask.GetMask("Terrain", "Construction"));
                var distance = cast ? rhit.distance : 3f;

                if (distance > 0.2f)
                {
                    GroundMissing();
                }
            }

            private void GroundMissing()
            {
                if (sentry == null)
                {
                    DoDestroy(); return;
                }
                sentry.Kill();
                plugin.GiveSentry(sentry.transform.position);
            }

            public void DoDestroy()
            {
                Destroy(this);
            }
        }
        #endregion
    }
}
