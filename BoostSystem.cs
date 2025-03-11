using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BoostSystem", "Fanatey", "0.0.4")]
    class BoostSystem : RustPlugin
    {
        [PluginReference] Plugin IQEconomic, ImageLibrary;
        #region зависимости
        public void apiremovebalance(ulong userID, int Balance) => IQEconomic?.CallHook("API_REMOVE_BALANCE", userID, Balance);
        public bool apiisremoved(ulong userID, int Amount) => (bool)IQEconomic?.CallHook("API_IS_REMOVED_BALANCE", userID, Amount);
        #endregion зависимости
        private List<string> ListGold = new List<string>() // Выпадение золота!
        {
            {"codelockedhackablecrate"},
            {"crate_basic"},
            {"crate_elite"},
            {"crate_normal"},
            {"supply_drop"},
            {"loot-barrel-1"},
            {"loot-barrel-2"},
            {"bradley_crate"},
        };
        int ammounttoupgrade1;
        int ammounttoupgrade2;
        int ammounttoupgrade3;
        float healbost1;
        float healbost2;
        float healbost3;
        float damagebost1;
        float damagebost2;
        float damagebost3;
        float protectdamage1;
        float protectdamage2;
        float protectdamage3;
        bool wipedData;
        bool Economic;
        int itemid;
        int dropchance;
        string projectname;
        
        Timer heall;
        Timer damagee;
        Timer takedamagee;

        private string NameGold = "Золото";
        int skinid;
        string shortname;
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "Колличество коинов для апгрейда до уровня 1", out ammounttoupgrade1, 7);
            GetVariable(Config, "Колличество коинов для апгрейда до уровня 2", out ammounttoupgrade2, 13);
            GetVariable(Config, "Колличество коинов для апгрейда до уровня 3", out ammounttoupgrade3, 19);
            GetVariable(Config, "Множитель хила на уровень 1", out healbost1, 1f);
            GetVariable(Config, "Множитель хила на уровень 2", out healbost2, 2f);
            GetVariable(Config, "Множитель хила на уровень 3", out healbost3, 4f);
            GetVariable(Config, "Множитель буста дамага на уровень 1", out damagebost1, 1.1f);
            GetVariable(Config, "Множитель буста дамага на уровень 2", out damagebost2, 1.4f);
            GetVariable(Config, "Множитель буста дамага на уровень 3", out damagebost3, 1.6f);
            GetVariable(Config, "Множитель защиты на уровень 1", out protectdamage1, 0.8f);
            GetVariable(Config, "Множитель защиты на уровень 2", out protectdamage2, 0.5f);
            GetVariable(Config, "Множитель защиты на уровень 3", out protectdamage3, 0.3f);
            GetVariable(Config, "Поддержка плагина IQEconomic", out Economic, false);
            GetVariable(Config, "Название проекта", out projectname, "CaseRust");
            GetVariable(Config, "Шанс выпадения монеты из контейнера", out dropchance, 3);
            GetVariable(Config, "ID предмета", out itemid, -1899491405);
            GetVariable(Config, "skinid предмета", out skinid, 1707233455);
            GetVariable(Config, "shortname предмета", out shortname, "glue");
            SaveConfig();
        }
        Dictionary<ulong, Dictionary<string, string>> boost;
        private List<LootContainer> handledContainers = new List<LootContainer>();
        void OnLootEntity(BasePlayer player, BaseEntity entity, Item item)
        {
            if (!(entity is LootContainer)) return;
            var container = (LootContainer)entity;
            if (handledContainers.Contains(container) || container.ShortPrefabName == "stocking_large_deployed" ||
               container.ShortPrefabName == "stocking_small_deployed") return;
            handledContainers.Add(container);
            List<int> ItemsList = new List<int>();
            if (ListGold.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(0f, 100f) < dropchance)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }

                    if (!ItemsList.Contains(-1899491405))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        var count = UnityEngine.Random.Range(1, 3 + 1);
                        item = ItemManager.CreateByName("glue");
                        item.name = NameGold;
                        item.MoveToContainer(itemContainer);
                    }
                }
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info, Item item)
        {
            if (info == null) return;
            if (entity?.net?.ID == null) return;
            var container = entity as LootContainer;
            var player = info?.InitiatorPlayer;
            if (player == null || container == null) return;
            List<int> ItemsList = new List<int>();
            if (ListGold.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(0f, 100f) < dropchance)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }

                    if (!ItemsList.Contains(-1899491405))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        var count = UnityEngine.Random.Range(1, 3 + 1);
                        item = ItemManager.CreateByName("glue");
                        item.name = NameGold;
                        item.MarkDirty();
                        item.MoveToContainer(itemContainer);
                    }
                }
            }
            handledContainers.Remove(container);
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item == null || item.info == null) return;
            var iname = item.info.shortname.ToLower();
            if (iname == shortname)
            {
                ulong result = Convert.ToUInt64(skinid);
                item.name = NameGold;
                item.skin = result;
            }
        }
        #region cui
        void cuimenu(BasePlayer player)
        {
            CuiElementContainer boot = new CuiElementContainer();
            healbar(player);
            takedamagebar(player);
            damagebar(player);
            CuiHelper.DestroyUi(player, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3098958 0.3870371", AnchorMax = "0.6776037 0.7462963" },
                Image = { Color = "0.142171 0.1008802 0.2442628 0.7720931" },
                CursorEnabled = true
            }, "Hud", Layer); 
            boot.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8696894 0.9020619", AnchorMax = "0.9957519 0.9922678" },
                Button = { Color = "0.8509278 0.4762424 0.4762328 0.7092233", Close = Layer, Command = "cmddestroutimer" },//
                Text = { Text = "Закрыть", Align = TextAnchor.MiddleCenter }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.03541084 0.6262889", AnchorMax = "0.2138813 0.9484539"},
                Image = { Color = "0.4784049 0 0.9803424 0.3647059" }
            }, Layer);
            boot.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                        {
                        new CuiRawImageComponent
                        {
                             Url = "http://rust.skyplugins.ru/getimage/largemedkit/512",
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0.03541084 0.6262889",
                             AnchorMax = "0.2138813 0.9484539"
                        }
                        }
            });
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2705384 0.6262889", AnchorMax = "0.449009 0.9484537"},
                Image = { Color = "0.4784049 0 0.9803424 0.3647059" }
            }, Layer);
            boot.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                        {
                        new CuiRawImageComponent
                        {
                             Url = "http://rust.skyplugins.ru/getimage/ammo.rifle/512",//ammo.rifle
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0.2705384 0.6262889",
                             AnchorMax = "0.449009 0.9484537"
                        }
                        }
            });
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5028335 0.6262889", AnchorMax = "0.6813042 0.9484539"},
                Image = { Color = "0.4784049 0 0.9803424 0.3647059" }
            }, Layer);
            boot.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                        {
                        new CuiRawImageComponent
                        {
                             Url = "http://rust.skyplugins.ru/getimage/heavy.plate.jacket/512" //
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0.5028335 0.6262889",
                             AnchorMax = "0.6813042 0.9484539"
                        }
                        }
            });
            boot.Add(new CuiLabel
            { 
            RectTransform = {AnchorMin = "0.6926354 0.5567008", AnchorMax = "0.9886697 0.8788658" },
            Text = { Text = $"Добро пожаловать\n на проект\n <color=orange>{projectname}</color>,\n<color=orange>{player.displayName}</color>!\nИграй и побеждай!\n\n", Align = TextAnchor.UpperCenter }           
            }, Layer);
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02549576 0.154639", AnchorMax = "0.2209635 0.533505" },
                Text = { Align = TextAnchor.UpperCenter, Text = "Прокачивая этот скилл вы будете регениться в разы быстрее!" }
            }, Layer);//healtext
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2577906 0.162371", AnchorMax = "0.4603405 0.5309277" },
                Text = { Align = TextAnchor.UpperCenter, Text = "Прокачивая этот скилл вы будете наносить больше урона!" }
            }, Layer);//boosttext
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4943349 0.1572161", AnchorMax = "0.6883861 0.5309273" },
                Text = { Align = TextAnchor.UpperCenter, Text = "Прокачивая этот скилл вы будете получать меньше урона!" }
            }, Layer);//bostfix damage
            boot.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                        {
                        new CuiRawImageComponent
                        {
                             Png = (string)ImageLibrary?.Call("GetImage", player.UserIDString)
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0.7152982 0.05154641",
                             AnchorMax = "0.963174 0.5025772"
                        }
                        }
            });
            boot.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.03541092 0.05154641", AnchorMax = "0.2138814 0.1546392" },
                Button = { Color = "0.8509278 0.4762328 0.4762328 0.7092233", Command = "cmdbostheal" },//
                Text = { Text = "Вкачать", Align = TextAnchor.MiddleCenter }
            }, Layer);
            boot.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2691222 0.05154641", AnchorMax = "0.4475926 0.1546392"},
                Button = { Color = "0.8509278 0.4762328 0.4762328 0.7092233", Command = "cmdbostdamage" },//
                Text = { Text = "Вкачать", Align = TextAnchor.MiddleCenter }
            }, Layer);
            boot.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5028335 0.05154641", AnchorMax = "0.6813039 0.1546392" },
                Button = { Color = "0.8509278 0.4762328 0.4762328 0.7092233", Command = "cmdbosttakedamage" },//
                Text = { Text = "Вкачать", Align = TextAnchor.MiddleCenter }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.03541084 0.5360824", AnchorMax = "0.09206813 0.6185565" },
                Image = { Color = "0 0 0 0.75" }
            }, Layer, "heal1");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03541084 0.5360824", AnchorMax = "0.09206813 0.6185565" },
                Text = { Text = ammounttoupgrade1.ToString() , Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.09631741 0.5360821", AnchorMax = "0.1529747 0.6185562" },
                Image = { Color = "0 0 0 0.75" }
            }, Layer, "heal2");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09631741 0.5360821", AnchorMax = "0.1529747 0.6185562" },
                Text = { Text = ammounttoupgrade2.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.157224 0.5360821", AnchorMax = "0.2138813 0.6185563"},
                Image = { Color = "0 0 0 0.75" }
            }, Layer, "heal3");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.157224 0.5360821", AnchorMax = "0.2138813 0.6185563" },
                Text = { Text = ammounttoupgrade3.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2705386 0.5360825", AnchorMax = "0.3271959 0.6185563"},
                Image = { Color = "0 0 0 0.75" }
            }, Layer, "boost1");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2705386 0.5360825", AnchorMax = "0.3271959 0.6185563" },
                Text = { Text = ammounttoupgrade1.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3314451 0.5360821", AnchorMax = "0.3881024 0.6185563"},
                Image = { Color = "0 0 0 0.75" }
            }, Layer, "boost2");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3314451 0.5360821", AnchorMax = "0.3881024 0.6185563" },
                Text = { Text = ammounttoupgrade2.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3923517 0.536082", AnchorMax = "0.449009 0.6185561"},
                Image = { Color = "0 0 0 0.75" }//424
            }, Layer, "boost3");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3923517 0.536082", AnchorMax = "0.449009 0.6185561" },
                Text = { Text = ammounttoupgrade3.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5028337 0.536082", AnchorMax = "0.5594907 0.6185561"},
                Image = { Color = "0 0 0 0.75" }//424
            }, Layer, "damage1");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5028337 0.536082", AnchorMax = "0.5594907 0.6185561" },
                Text = { Text = ammounttoupgrade1.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5637401 0.536082", AnchorMax = "0.6203976 0.6185561"},
                Image = { Color = "0 0 0 0.75" }//424
            }, Layer, "damage2");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5637401 0.536082", AnchorMax = "0.6203976 0.6185561" },
                Text = { Text = ammounttoupgrade2.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            boot.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.6246464 0.5360821", AnchorMax = "0.6813039 0.6185563" },
                Image = { Color = "0 0 0 0.75" }
            }, Layer, "damage3");
            boot.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6246464 0.5360821", AnchorMax = "0.6813039 0.6185563" },
                Text = { Text = ammounttoupgrade3.ToString(), Align = TextAnchor.MiddleCenter, Color = "1 0.6431373 0 1" }
            }, Layer);
            CuiHelper.AddUi(player, boot);
        }
        void healbar(BasePlayer player)
        {
            var boostbox = boost[player.userID];
            CuiElementContainer bot = new CuiElementContainer();
            if (boostbox.ContainsKey("HealBoost"))
            {
                var hbox = boostbox["HealBoost"];
                if (hbox == "Heal1")
                {
                    bot.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02499963 0.03124986", AnchorMax = "0.9749994 0.937501"},
                        Image = { Color = "0.621307 0 1 0.35"}
                    }, "heal1");
                }
                else
                if (hbox == "Heal2")
                {
                    bot.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02499965 0.03124986", AnchorMax = "0.9750003 0.9375014"},
                        Image = { Color = "0.621307 0 1 0.35" }
                    }, "heal2");
                }
                else
                if (hbox == "Heal3")
                {
                    bot.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02499965 0.03124986", AnchorMax = "0.9750004 0.9375001"},
                        Image = { Color = "0.621307 0 1 0.35" }
                    }, "heal3");
                }
            }
            CuiHelper.AddUi(player, bot);          
        }
        void damagebar(BasePlayer player)
        {
            var boostbox = boost[player.userID];
            CuiElementContainer dmg = new CuiElementContainer();
            if (boostbox.ContainsKey("DamageBoost"))
            {
                var hbox = boostbox["DamageBoost"];
                if (hbox == "Dast1")
                {
                    dmg.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02475003 0.03093767", AnchorMax = "0.9747511 0.9371914" },
                        Image = { Color = "0.621307 0 1 0.35" }
                    }, "boost1");
                }
                else
                if (hbox == "Dast2")
                {
                    dmg.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02475306 0.03094125", AnchorMax = "0.9747525 0.9371917" },
                        Image = { Color = "0.621307 0 1 0.35" }
                    }, "boost2");
                }
                else
                if (hbox == "Dast3")
                {
                    dmg.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02474999 0.03093755", AnchorMax = "0.974749 0.9371879" },
                        Image = { Color = "0.621307 0 1 0.35" }
                    }, "boost3");
                }
            }
            CuiHelper.AddUi(player, dmg);
        }
        void takedamagebar(BasePlayer player)
        {
            var boostbox = boost[player.userID];
            CuiElementContainer tdmg = new CuiElementContainer();
            if (boostbox.ContainsKey("TakeDamageBoost"))
            {
                var hbox = boostbox["TakeDamageBoost"];
                if (hbox == "takeboos1")
                {
                    tdmg.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02475009 0.03093755", AnchorMax = "0.974753 0.937188" },
                        Image = { Color = "0.621307 0 1 0.35" }//424
                    }, "damage1");
                }
                else
                if (hbox == "takeboos2")
                {
                    tdmg.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02474994 0.03093755", AnchorMax = "0.9747475 0.9371886" },
                        Image = { Color = "0.621307 0 1 0.35" }//424
                    }, "damage2");
                }
                else
                if (hbox == "takeboos3")
                {
                    tdmg.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.02474994 0.03093755", AnchorMax = "0.9747475 0.9371886" },
                        Image = { Color = "0.621307 0 1 0.35" }
                    }, "damage3");
                }
            }
            CuiHelper.AddUi(player, tdmg);
        }
#endregion cui
        void DestroyTimer(Timer timer)
        {
            timer.DestroyToPool();
            timer = null;
        }
        [JsonProperty("Системный слой")]
        private string Layer = "Boost_UI";
        [ConsoleCommand("wp")]
        void cmdvp(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player.net.connection.authLevel == 2)
            {
                WipeData();
                PrintWarning("Dataremove!");
            }
        }
        object OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity entity)
        {
            var player = entity as BasePlayer;
            if (player != null)
            {
                if (!boost.ContainsKey(player.userID))
                {
                    return null;
                }
                else
                {
                    var boostbox = boost[player.userID];
                    if (boostbox.ContainsKey("HealBoost"))
                    {
                        var hbox = boostbox["HealBoost"];
                        if (hbox == "Heal1")
                        {
                            player.health += healbost2;
                            return null;
                        }
                        if (hbox == "Heal2")
                        {
                            player.health += healbost2;
                            return null;
                        }
                        if (hbox == "Heal3")
                        {
                            player.health += healbost3;
                            return null;
                        }
                    }
                    else
                    { return null; }
                }
            }
            return null;
        }
        
        [ChatCommand("boost")]
        void Bosst(BasePlayer player)
        {
            Dictionary<string, string> boostbox;
            if (!boost.TryGetValue(player.userID, out boostbox)) boostbox = (boost[player.userID] = new Dictionary<string, string>());
            cuimenu(player);
            SaveData();
        }
        [ConsoleCommand("cmdbostheal")]
        void cmdboostheal(ConsoleSystem.Arg arg)
        {        
            var player = arg.Player();
            heall = timer.Once(0.09f, () => healbar(player));
            string value1 = "Heal1";
            string value2 = "Heal2";
            string value3 = "Heal3";     
            var boostbox = boost[player.userID];
            var name = "HealBoost";
            if (!boostbox.ContainsKey(name))
            {
                if (Economic)
                {
                    if (!IQEconomic) return;
                    else
                    {
                        if (apiisremoved(player.userID, ammounttoupgrade1))
                        {
                            apiremovebalance(player.userID, ammounttoupgrade1);
                            if(boostbox.ContainsKey(name))
                            {
                                boostbox.Remove(name);
                            }
                            boostbox.Add(name, value1);
                            SendReply(player, "Вы получили первый уровень регена!");
                        }
                        else
                        {
                            SendReply(player, "Не достаточно коинов для улучшения уровня!");
                            return;
                        }
                    }
                }
                if (player.inventory.GetAmount(itemid) - ammounttoupgrade1 >= 0)
                {
                    if (boostbox.ContainsKey(name))
                    {
                        boostbox.Remove(name);
                    }
                    boostbox.Add(name, value1);
                    player.inventory.Take(null, itemid, ammounttoupgrade1);
                    SendReply(player, "Вы получили первый уровень регена!");
                    SaveData();
                    return;
                }
                else
                {
                    SendReply(player, "Не достаточно коинов для улучшения уровня!");
                    return;
                }
            }
            else
            if (boostbox.ContainsKey(name))
            {
                if (boostbox.ContainsValue(value1))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {

                            if (apiisremoved(player.userID, ammounttoupgrade2))
                            {
                                apiremovebalance(player.userID, ammounttoupgrade2);
                                boostbox.Remove(name);
                                boostbox.Add(name, value1);
                                SendReply(player, "Вы получили второй уровень регена!");

                            }
                            else
                            {
                                SendReply(player, "Не достаточно коинов для улучшения уровня!");
                                return;
                            }
                        }
                    }
                    if (player.inventory.GetAmount(itemid) - ammounttoupgrade2 >= 0)
                    {
                        boostbox.Remove(name);
                        boostbox.Add(name, value2);
                        player.inventory.Take(null, itemid, ammounttoupgrade2);
                        SendReply(player, "Вы получили второй уровень регена!");
                        SaveData();
                        return;
                    }
                    else
                    {
                        SendReply(player, "Не достаточно коинов для улучшения уровня!");
                        return;
                    }
                }
                else
                if (boostbox.ContainsValue(value2))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {

                            if (apiisremoved(player.userID, ammounttoupgrade3))
                            {
                                apiremovebalance(player.userID, ammounttoupgrade3);
                                boostbox.Remove(name);
                                boostbox.Add(name, value1);
                                SendReply(player, "Вы достигли максимального уровня регена!");
                            }
                            else
                            {
                                SendReply(player, "Не достаточно коинов для улучшения уровня!");
                                return;
                            }
                        }
                    }
                    if (player.inventory.GetAmount(itemid) - ammounttoupgrade3 >= 0)
                    {
                        boostbox.Remove(name);
                        boostbox.Add(name, value3);
                        player.inventory.Take(null, itemid, ammounttoupgrade3);
                        SendReply(player, "Вы достигли максимального уровня регена!");
                        SaveData();
                        return;
                    }
                    else
                    {
                        SendReply(player, "Не достаточно коинов для улучшения уровня!");
                        return;
                    }
                }
                else
                if (boostbox.ContainsValue(value3))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {
                            SendReply(player, "Вы уже достигли максимального уровня регена!");
                            DestroyTimer(heall);
                            return;
                        }
                    }
                    SendReply(player, "Вы уже достигли максимального уровня регена!");
                    DestroyTimer(heall);
                    SaveData();
                    return;
                }            
            }
        }
        [ConsoleCommand("cmdbostdamage")]
        void cmdbostdamage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            damagee = timer.Once(0.09f, () => damagebar(player));
            string value1 = "Dast1";
            string value2 = "Dast2";
            string value3 = "Dast3";
            var boostbox = boost[player.userID];
            var name = "DamageBoost";
            if (!boostbox.ContainsKey(name))
            {
                if (Economic)
                {
                    if (!IQEconomic) return;
                    else
                    {
                        if (apiisremoved(player.userID, ammounttoupgrade1))
                        {
                            apiremovebalance(player.userID, ammounttoupgrade1);
                            boostbox.Remove(name);
                            boostbox.Add(name, value1);
                            SendReply(player, "Вы получили первый уровень буста дамага!");
                        }
                        else
                        {
                            SendReply(player, "Не достаточно коинов для улучшения уровня!");
                            return;
                        }
                    }
                }
                if (player.inventory.GetAmount(itemid) - ammounttoupgrade1 >= 0)
                {
                    boostbox.Remove(name);
                    boostbox.Add(name, value1);                  
                    SendReply(player, "Вы получили первый уровень буста дамага!");
                    player.inventory.Take(null, itemid, ammounttoupgrade1);
                    SaveData();
                    return;
                }
                else
                {
                    SendReply(player, "Не достаточно коинов для улучшения уровня!");
                    return;
                }
            }
            else
            if (boostbox.ContainsKey(name))
            {
                if (boostbox.ContainsValue(value1))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {

                            if (apiisremoved(player.userID, ammounttoupgrade2))
                            {
                                apiremovebalance(player.userID, ammounttoupgrade2);
                                if (boostbox.ContainsKey(name))
                                {
                                    boostbox.Remove(name);
                                }
                                boostbox.Add(name, value1);
                                SendReply(player, "Вы получили второй уровень буста дамага!");
                            }
                            else
                            {
                                SendReply(player, "Не достаточно коинов для улучшения уровня!");
                                return;
                            }
                        }
                    }
                    if (player.inventory.GetAmount(itemid) - ammounttoupgrade2 >= 0)
                    {
                        if (boostbox.ContainsKey(name))
                        {
                            boostbox.Remove(name);
                        }
                        boostbox.Add(name, value2);
                        player.inventory.Take(null, itemid, ammounttoupgrade2);
                        SendReply(player, "Вы получили второй уровень буста дамага!");
                        SaveData();
                        return;
                    }
                    else
                    {
                        SendReply(player, "Не достаточно коинов для улучшения уровня!");
                        return;
                    }
                }
                else
                if (boostbox.ContainsValue(value2))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {
                            if (apiisremoved(player.userID, ammounttoupgrade3))
                            {
                                apiremovebalance(player.userID, ammounttoupgrade3);
                                boostbox.Remove(name);
                                boostbox.Add(name, value1);
                                SendReply(player, "Вы достигли максимального уровня буста дамага!");
                            }
                            else
                            {
                                SendReply(player, "Не достаточно коинов для улучшения уровня!");
                                return;
                            }
                        }
                    }
                    if (player.inventory.GetAmount(itemid) - ammounttoupgrade3 >= 0)
                    {
                        boostbox.Remove(name);
                        boostbox.Add(name, value3);
                        player.inventory.Take(null, itemid, ammounttoupgrade3);
                        SendReply(player, "Вы достигли максимального уровня буста дамага!");
                        SaveData();
                        return;
                    }
                    else
                    {
                        SendReply(player, "Не достаточно коинов для улучшения уровня!");

                        return;
                    }
                }
                else
                if (boostbox.ContainsValue(value3))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {
                            SendReply(player, "Вы уже достигли максимального уровня буста дамага!");
                            DestroyTimer(damagee);
                            return;
                        }
                    }
                    SendReply(player, "Вы уже достигли максимального уровня буста дамага!");
                    DestroyTimer(damagee);
                    SaveData();
                    return;
                }
            }
        }    
        [ConsoleCommand("cmdbosttakedamage")]
        void cmdbosttakedamage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            takedamagee = timer.Once(0.09f, () => takedamagebar(player));
            string value1 = "takeboos1";
            string value2 = "takeboos2";
            string value3 = "takeboos3";
            var boostbox = boost[player.userID];
            var name = "TakeDamageBoost";
            if (!boostbox.ContainsKey(name))
            {
                if (Economic)
                {
                    if (!IQEconomic) return;
                    else
                    {

                        if (apiisremoved(player.userID, ammounttoupgrade1))
                        {
                            if (boostbox.ContainsKey(name))
                            {
                                boostbox.Remove(name);
                            }
                            apiremovebalance(player.userID, ammounttoupgrade1);
                            boostbox.Add(name, value1);
                            SendReply(player, "Вы получили первый уровень уменьшения получаемого урона!");
                        }
                        else
                        {
                            SendReply(player, "Не достаточно коинов для улучшения уровня!");
                            return;
                        }
                    }
                }
                if (player.inventory.GetAmount(itemid) - ammounttoupgrade1 >= 0)
                {
                    if (boostbox.ContainsKey(name))
                    {
                        boostbox.Remove(name);
                    }
                    boostbox.Add(name, value1);
                    SendReply(player, "Вы получили первый уровень уменьшения получаемого урона!");
                    player.inventory.Take(null, itemid, ammounttoupgrade1);
                    SaveData();
                    return;
                }
                else
                {
                    SendReply(player, "Не достаточно коинов для улучшения уровня!");
                    return;
                }
            }
            else
            if (boostbox.ContainsKey(name))
            {
                if (boostbox.ContainsValue(value1))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {
                            if (apiisremoved(player.userID, ammounttoupgrade2))
                            {
                                apiremovebalance(player.userID, ammounttoupgrade2);
                                boostbox.Remove(name);
                                boostbox.Add(name, value1);
                                SendReply(player, "Вы получили второй уровень уменьшения получаемого урона!");
                            }
                            else
                            {
                                SendReply(player, "Не достаточно коинов для улучшения уровня!");
                                return;
                            }
                        }
                    }
                    else
                    if (player.inventory.GetAmount(itemid) - ammounttoupgrade2 >= 0)
                    {
                        boostbox.Remove(name);
                        boostbox.Add(name, value2);
                        player.inventory.Take(null, itemid, ammounttoupgrade2);
                        SendReply(player, "Вы получили второй уровень уменьшения получаемого урона!");
                        SaveData();
                        return;
                    }
                    else
                    {
                        SendReply(player, "Не достаточно коинов для улучшения уровня!");
                        return;
                    }
                }
                if (boostbox.ContainsValue(value2))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {

                            if (apiisremoved(player.userID, ammounttoupgrade3))
                            {
                                apiremovebalance(player.userID, ammounttoupgrade3);
                                boostbox.Remove(name);
                                boostbox.Add(name, value1);
                                SendReply(player, "Вы достигли максимального уровня уменьшения получаемого урона!");
                            }
                            else
                            {
                                SendReply(player, "Не достаточно коинов для улучшения уровня!");
                                return;
                            }
                        }
                    }
                    else
                    if (player.inventory.GetAmount(itemid) - ammounttoupgrade3 >= 0)
                    {
                        boostbox.Remove(name);
                        boostbox.Add(name, value3);
                        player.inventory.Take(null, itemid, ammounttoupgrade3);
                        SendReply(player, "Вы достигли максимального уровня уменьшения получаемого урона!");
                        SaveData();
                        return;
                    }
                    else
                    {
                        SendReply(player, "Не достаточно коинов для улучшения уровня!");
                        return;
                    }
                }
                else
                if (boostbox.ContainsValue(value3))
                {
                    if (Economic)
                    {
                        if (!IQEconomic) return;
                        else
                        {
                            SendReply(player, "Вы уже достигли максимального уровня уменьшения получаемого урона!");
                            DestroyTimer(takedamagee);
                            return;
                        }
                    }
                    SendReply(player, "Вы уже достигли максимального уровня уменьшения получаемого урона!");
                    DestroyTimer(takedamagee);
                    SaveData();
                    return;
                }
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            BasePlayer Initiator = hitInfo.InitiatorPlayer;
            BasePlayer Target = entity as BasePlayer;
            if (hitInfo != null && Initiator != null && entity != null)
            {
                if (entity.GetComponent<BasePlayer>())
                {
                    if (!boost.ContainsKey(Initiator.userID))
                    {
                        return;
                    }
                    else
                    {
                        var name = "DamageBoost";
                        var boostbox = boost[Initiator.userID];
                        var mtotal = hitInfo.damageTypes.Total();

                        if (boostbox.ContainsKey(name))
                        {
                            var bbox = boostbox[name];
                            if (bbox == "Dast1")
                            {
                                if (hitInfo != null && hitInfo.InitiatorPlayer != null && entity != null)
                                {
                                    hitInfo.damageTypes.ScaleAll(damagebost1);
                                }
                            }
                            if (bbox == "Dast2")
                            {
                                if (hitInfo != null && hitInfo.InitiatorPlayer != null && entity != null)
                                {
                                    hitInfo.damageTypes.ScaleAll(damagebost2);

                                }
                            }
                            if (bbox == "Dast3")
                            {
                                if (hitInfo != null && hitInfo.InitiatorPlayer != null && entity != null)
                                {
                                    hitInfo.damageTypes.ScaleAll(damagebost3);
                                }
                            }
                        }
                        else
                        { return; }
                    }
                }
            }
            if (entity == Target)
            {
                if (entity.GetComponent<BasePlayer>())
                {
                    if (hitInfo != null && entity != null)
                    {
                        if (!boost.ContainsKey(Target.userID))
                        {
                            return;
                        }
                        else
                        {
                            var boostbox = boost[Target.userID];
                            var name = "TakeDamageBoost";
                            var damage = hitInfo.damageTypes.Total();
                            if (boostbox.ContainsKey(name))
                            {
                                var btbox = boostbox[name];
                                if (btbox == "takeboos1")
                                {
                                    hitInfo.damageTypes.ScaleAll(protectdamage1);
                                }
                                if (btbox == "takeboos2")
                                {
                                    hitInfo.damageTypes.ScaleAll(protectdamage2);
                                }
                                if (btbox == "takeboos3")
                                {
                                    hitInfo.damageTypes.ScaleAll(protectdamage3);
                                }
                            }
                        }
                    }
                }
               
            }
        }

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
        DynamicConfigFile boostfile = Interface.Oxide.DataFileSystem.GetFile("BoostSystem/playerslvl");
        void LoadData()
        {
            try
            {
                    boost = boostfile.ReadObject<Dictionary<ulong, Dictionary<string, string>>>();
                
                if (boost == null)
                {
                    PrintError("File Homes is null! Create new data files");
                        boost = new Dictionary<ulong, Dictionary<string, string>>();                   
                }
            }
            catch
            {

                    boost = new Dictionary<ulong, Dictionary<string, string>>();
            }
        }
        void SaveData()
        {
                if (boost != null) boostfile.WriteObject(boost);           
        }
        void OnServerInitialized()
        {
            LoadData();
            LoadDefaultConfig();
        }
        void Unload() => SaveData();
        private void GetNewSave()
        {
            if (wipedData)
            {
                PrintWarning("Обнаружен вайп. Очищаем данные с BoostSystem/playerslvl");
                WipeData();
            }
        }
        void WipeData()
        {
            LoadData();
                boost = new Dictionary<ulong, Dictionary<string, string>>();
            SaveData();
        }
    }
}
