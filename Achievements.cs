using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Network;
using UnityEngine;
using UnityEngine.UI;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.IO;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Achievments", "FADERUST", "1.0.1")]
    class Achievements : RustPlugin
    {
        public class Achievs
        {
            public string name;
            public Dictionary<string, bool> dostij;
            public Dictionary<string, int> dones;
            public string uid;
        }
        public double Xmin = 0.05;
        public double Xmin2 = 0.95;
        public double ymin = 0.82;
        public double ymin2 = 0.9;
        public double Xbutton = 0.60;
        public Dictionary<string, Achievs> achievementses = new Dictionary<string, Achievs>();
        public double Xbutton2 = 0.85;
        public double ybutton = 0.84;
        public double ybutton2 = 0.88;
        public Dictionary<ulong, Dictionary<string, int>> pagea = new Dictionary<ulong, Dictionary<string, int>>();
        public Dictionary<string, Achievs> achievs = new Dictionary<string, Achievs>();

        DynamicConfigFile players_File = Interface.Oxide.DataFileSystem.GetFile("Achievments/achievs");
        [PluginReference]
        Plugin RustShop, Case;

        public void GiveCase(BasePlayer player, int casecount)
        {
            Case.Call("GiveCase", player, casecount);
        }
        public void AddBalance(ulong userid, int Amount)
        {
            RustShop.Call("AddBalance", userid, Amount);
        }
        [ChatCommand("ach")]
        void CommandAchievments(BasePlayer player)
        {
            createachievmentsmenu(player);
        }
        public Dictionary<string, int> getallachivments(BasePlayer player)
        {
            var achievdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            Dictionary<string, int> allachievs = new Dictionary<string, int>();
            allachievs.Add("CollectWood", 0);
            allachievs.Add("CollectStone", 0);
            allachievs.Add("CollectMetall", 0);
            allachievs.Add("CollectSulfur", 0);
            allachievs.Add("CollectHQM", 0);
            allachievs.Add("KillAnimals", 0);
            allachievs.Add("KillPlayers", 0);
            allachievs.Add("CrateBuildings", 0);
            allachievs.Add("CraftRifle", 0);
            allachievs.Add("KillModerators", 0);
            allachievs.Add("WinEvent", 0);
            allachievs.Add("CraftExplosiveC4", 0);
            allachievs.Add("CraftExplosiveRockets", 0);
            foreach (var achievsplayer in achievdata)
            {
                if (achievsplayer.Key.Equals(player.UserIDString))
                {
                    if (achievsplayer.Value != null)
                    {
                        if (achievsplayer.Value.dones == null)
                        {
                            return allachievs;
                        }
                    }
                    if (achievsplayer.Value != null)
                    {

                        if (achievsplayer.Value.dones != null)
                        {
                            foreach (var dostijs in achievsplayer.Value.dones)
                            {
                                if (allachievs.ContainsKey(dostijs.Key))
                                {
                                    allachievs.Remove(dostijs.Key);
                                    allachievs.Add(dostijs.Key, dostijs.Value);
                                }
                            }
                        }
                    }
                }
            }
            return allachievs;
        }
        void getgiveresource(BasePlayer player, string name)
        {
            if (name.Equals("CollectWood")) { player.ChatMessage("Вам был выдан калаш"); }
            if (name.Equals("CollectStone")) { }
        }
        public string getnameitem(string name)
        {
            if (name.Equals("stones")) { name = "CollectStone"; }
            if (name.Equals("wood")) { name = "CollectWood"; }
            if (name.Equals("sulfur_ore")) { name = "CollectSulfur"; }
            if (name.Equals("metal_ore")) { name = "CollectMetall"; }
            if (name.Equals("hq_metal_ore")) { name = "CollectHQM"; }
            return name;
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            var achivdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            var achivlist = new Dictionary<string, int>();
            string items1 = item.info.name.Replace(".item", "");
            if (items1.Equals("stones") || items1.Equals("wood") || items1.Equals("metal_ore") || items1.Equals("sulfur_ore"))
            {
                string items = getnameitem(items1);
                foreach (var playersachivs in achivdata)
                {
                    if (playersachivs.Key.Equals(player.UserIDString))
                    {
                        if (playersachivs.Value.dones == null)
                        {
                            if (achivlist != null)
                            {
                                if (achivlist.ContainsKey(items))
                                { achivlist.Remove(items); }
                            }
                            achivlist.Add(items, item.amount);
                            achievsplayeradd(player, playersachivs.Value.dostij, achivlist);
                            return;
                        }
                        if (playersachivs.Value.dones != null)
                        {
                            if(playersachivs.Value.dostij == null)
                            {
                                foreach (var achivs in playersachivs.Value.dones)
                                {
                                    achivlist.Add(achivs.Key, achivs.Value);
                                }
                                foreach (var namedones in playersachivs.Value.dones)
                                {
                                    if (namedones.Key.Equals(items))
                                    {
                                        if (achivlist.ContainsKey(items))
                                        { achivlist.Remove(items); }
                                        achivlist.Add(items, namedones.Value + item.amount);
                                        achievsplayeradd(player, playersachivs.Value.dostij, achivlist);
                                        if(getmax(items) <= namedones.Value + item.amount)
                                        {
                                            if (namedones.Value < getmax(items))
                                            {
                                                dostijeniecompletehud(player, items);
                                            }
                                        }
                                        return;
                                    }
                                    if (!playersachivs.Value.dones.ContainsKey(items))
                                    {
                                        if (achivlist.ContainsKey(items))
                                        { achivlist.Remove(items); }
                                        achivlist.Add(items, item.amount);
                                        achievsplayeradd(player, playersachivs.Value.dostij, achivlist);
                                        return;
                                    }
                                }
                            }
                            if (playersachivs.Value.dostij != null)
                            {
                                foreach (var playerdostijes in playersachivs.Value.dostij)
                                {
                                    if (playerdostijes.Key.Equals(items))
                                    {
                                        return;
                                    }
                                }
                                foreach (var achivs in playersachivs.Value.dones)
                                {
                                    achivlist.Add(achivs.Key, achivs.Value);
                                }
                                foreach (var namedones in playersachivs.Value.dones)
                                {
                                    if (namedones.Key.Equals(items))
                                    {      
                                        if (achivlist.ContainsKey(items))
                                        { achivlist.Remove(items); }
                                        achivlist.Add(items, namedones.Value + item.amount);
                                        achievsplayeradd(player, playersachivs.Value.dostij, achivlist);
                                        if (getmax(items) <= namedones.Value + item.amount)
                                        {
                                            if (namedones.Value < getmax(items))
                                            {
                                                dostijeniecompletehud(player, items);
                                            }
                                        }
                                        return;
                                    }
                                    if (!playersachivs.Value.dones.ContainsKey(items))
                                    {
                                        if (achivlist.ContainsKey(items))
                                        { achivlist.Remove(items); }
                                        achivlist.Add(items, item.amount);
                                        achievsplayeradd(player, playersachivs.Value.dostij, achivlist);
                                        if (getmax(items) <= namedones.Value + item.amount)
                                        {
                                            if (namedones.Value < getmax(items))
                                            {
                                                dostijeniecompletehud(player, items);
                                            }
                                        }
                                        return;
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        private void dostijeniecompletehud(BasePlayer player, string items)
        {
            CuiHelper.DestroyUi(player, "dostijescomplete");
            var container = new CuiElementContainer();
            var panel = container.Add(new CuiPanel
            {
                 Image =
                {
                    Color = "0.5450980392156863 0.1568627450980392 0.8784313725490196 0.8"
                },
                RectTransform =
                {
                    AnchorMin = "0.8 0.88",
                    AnchorMax = "1 1"
                }
            }, "Hud", "dostijescomplete");
            container.Add(new CuiElement()
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiTextComponent {Text = getcompletetext(items), Align = TextAnchor.UpperCenter, FontSize = 16},
                    new CuiRectTransformComponent {AnchorMin = " 0.1 0.05"}
                }
            });
            CuiHelper.AddUi(player, container);
            timer.Once(15, () =>
            {
                CuiHelper.DestroyUi(player, "dostijescomplete");
            });
        }
        void OnPlayerDie(BasePlayer killed, HitInfo info)
        {
            var achivdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            var achivlist = new Dictionary<string, int>();
            if(killed == null) {return; }
            if (info == null) { return; }
            if (info.InitiatorPlayer == null) { return; }
            if (killed.IsNpc) { return; }
            if (info.InitiatorPlayer.IsNpc) { return; }
            BasePlayer player = info.InitiatorPlayer;
            achievslistadd(player, "KillPlayers", 1);
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            string items1 = item.info.name.Replace(".item", "");
            string items = getnameitem(items1);
            int count = item.amount;
            achievslistadd(player, items, count);
        }
        public void achievslistadd(BasePlayer player, string name, int count)
        {
            var achivdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            var achivlist = new Dictionary<string, int>();
            foreach (var players in achivdata)
            {
                if (players.Key.Equals(player.UserIDString))
                {
                    if (players.Value.dones != null)
                    {
                        foreach (var donest in players.Value.dones)
                        {
                            if (donest.Key.Equals(name))
                            {
                                foreach (var achivslists in players.Value.dones)
                                {
                                    achivlist.Add(achivslists.Key, achivslists.Value);
                                }
                                if (players.Value.dostij == null)
                                {
                                    if (achivlist.ContainsKey(name)) { achivlist.Remove(name); }
                                    achivlist.Add(name, donest.Value + count);
                                    if (donest.Value + count >= getmax(name))
                                    {
                                        if (donest.Value < getmax(name))
                                        {
                                            dostijeniecompletehud(player, name);
                                        }
                                    }
                                    achievsplayeradd(player, players.Value.dostij, achivlist);
                                    return;
                                }
                                if (players.Value.dostij != null)
                                {
                                    foreach (var dostijes in players.Value.dostij)
                                    {
                                        if (dostijes.Key.Equals(name)) { return; }
                                    }
                                    if (achivlist.ContainsKey(name)) { achivlist.Remove(name); }
                                    achivlist.Add(name, donest.Value + count);
                                    if (donest.Value + count >= getmax(name))
                                    {
                                        if (donest.Value < getmax(name))
                                        {
                                            dostijeniecompletehud(player, name);
                                        }
                                    }
                                    achievsplayeradd(player, players.Value.dostij, achivlist);
                                    return;
                                }
                            }
                            if (!players.Value.dones.ContainsKey(name))
                            {
                                foreach (var achivslists in players.Value.dones)
                                {
                                    achivlist.Add(achivslists.Key, achivslists.Value);
                                }
                                achivlist.Add(name, count);
                                achievsplayeradd(player, players.Value.dostij, achivlist);
                                return;
                            }
                        }
                    }
                    if (players.Value.dones == null)
                    {
                        achivlist.Add(name, count);
                        achievsplayeradd(player, players.Value.dostij, achivlist);
                        return;
                    }
                }
            }
    }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) { return; }
            if (info == null) { return; }
            if (info.InitiatorPlayer == null) { return; }
            if (entity is BaseAnimalNPC)
            {
                BasePlayer player = info.InitiatorPlayer;
                achievslistadd(player, "KillAnimals", 1);
            }
        }
        public string getcompletetext(string items)
        {
            string text = null;
            if(items.Equals("CollectWood")) { text = "\n Вы выполнили достижение" + "\n" + "\n"  + "Добыть 500000 дерева"; }
            if (items.Equals("CollectStone")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Добыть 300000 камня"; }
            if (items.Equals("CollectSulfur")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Добыть 100000 серы"; }
            if (items.Equals("CollectMetall")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Добыть 200000 металла"; }
            if (items.Equals("KillPlayers")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Убить 20 игроков"; }
            if (items.Equals("KillAnimals")) {text = "\n Вы выполнили достижение" + "\n" + "\n" + "Убить 100 животных"; }
            if (items.Equals("CraftRifle")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Скрафтить 25 винтовок"; }
            if (items.Equals("CraftExplosiveC4")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Скрафтить 50 c4"; }
            if (items.Equals("CraftExplosiveRockets")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Скрафтить 100 ракет"; }
            if (items.Equals("CrateBuildings")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Построить 1000 объектов"; }
            if (items.Equals("CollectHQM")) { text = "\n Вы выполнили достижение" + "\n" + "\n" + "Добыть 1000 мвк"; }

                return text;
        }
        void OnItemCraft(ItemCraftTask item)
        {
            BasePlayer player = item.owner;
            string items = item.blueprint.name.Replace(".item", "");
            if(items.Equals("ak47u")) { achievslistadd(player, "CraftRifle", item.amount); return; }
            if (items.Equals("explosive.timed")) { achievslistadd(player, "CraftExplosiveC4", item.amount); return; }
            if(items.Equals("ammo_rocket_basic")) { achievslistadd(player, "CraftExplosiveRockets", item.amount); return; }
        }
        [ConsoleCommand("page2open")]
        void page2(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            CuiHelper.DestroyUi(player, "AchievmentsPanel");
            var container = new CuiElementContainer();
            var achievsdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            Dictionary<string, int> activeacivements = new Dictionary<string, int>();
            foreach (var playerstigs in pagea)
            {
                if (playerstigs.Key.Equals(player.userID))
                {
                    activeacivements = playerstigs.Value;
                }
            }
            var panel = container.Add(new CuiPanel
            {
                Image = {
                    Color = "0.7686274509803922 0.5529411764705882 0.7137254901960784 0.4",
                },
                CursorEnabled = true,
                RectTransform =
                {
                    AnchorMin = "0.35 0.11",
                    AnchorMax = "0.60 0.95",
                }

            }, "Hud", "AchievmentsPanel2");
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiButtonComponent {Close = "AchievmentsPanel2", Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "-5 -4", AnchorMax = "0 5"}
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiButtonComponent {Close = "AchievmentsPanel2", Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "1 -4", AnchorMax = "6 5"}
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = "https://i.imgur.com/8poCpfR.png"
                    },
                    new CuiRectTransformComponent
                    {
                    AnchorMin = "0.80 0.02",
                    AnchorMax = "0.90 0.08",
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = "https://i.imgur.com/xyzRFoD.png"

                    },
                    new CuiRectTransformComponent
                    {
                    AnchorMin = "0.1 0.02",
                    AnchorMax = "0.2 0.08",
                    }
                }
            });
            container.Add(new CuiButton
            {
                Button =
                {
                  Command = "createachivmenu",
                  Color = "0 0 0 0",
                },
                Text =
                {
                    Text = ""
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.02",
                    AnchorMax = "0.2 0.08",
                }
            }, panel, "Paget2");
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiTextComponent {Text = "Достижения", Align = TextAnchor.UpperLeft, FontSize = 20},
                    new CuiRectTransformComponent {AnchorMin = "0.35 0.92", AnchorMax = "0.8 0.98"}
                }
            });
            foreach (var dostij in activeacivements)
            {
                string xs = Xmin + " " + ymin;
                string ys = Xmin2 + " " + ymin2;


                if (ymin > 0.12)
                {
                    container.Add(new CuiPanel
                    {
                        Image =
                {
                    Color = "0.5490196078431373 0.1843137254901961 0.8313725490196078 0.7"
                },
                        RectTransform =
                {
                    AnchorMin = xs,
                    AnchorMax = ys,
                }


                    }, panel, "Dostjenie");
                    if (dostij.Value < getmax(dostij.Key))
                    {
                        container.Add(new CuiElement
                        {
                            Name = CuiHelper.GetGuid(),
                            Parent = panel,
                            Components =
                        {
                            new CuiTextComponent
                            {
                                Text = gettext(dostij.Key, dostij.Value), Align = TextAnchor.MiddleCenter
                            },
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                        });
                    }
                    if (dostij.Value >= getmax(dostij.Key))
                    {
                        foreach (var test in achievsdata)
                        {
                            if (test.Key.Equals(player.UserIDString))
                            {
                                if (test.Value.dostij != null)
                                {
                                    foreach (var dostijenia in test.Value.dostij)
                                    {
                                        if (dostijenia.Key.Equals(dostij.Key))
                                        {
                                            container.Add(new CuiElement
                                            {
                                                Name = CuiHelper.GetGuid(),
                                                Parent = panel,
                                                Components =
                        {
                            new CuiTextComponent { Text = gettext(dostij.Key, dostij.Value) + " Выполнено", Align = TextAnchor.MiddleCenter},
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        foreach (var test in achievsdata)
                        {
                            Dictionary<string, bool> dostiges = new Dictionary<string, bool>();
                            if (test.Key.Equals(player.UserIDString))
                            {
                                if (test.Value.dostij != null)
                                {
                                    foreach (var dostig in test.Value.dostij)
                                    {
                                        dostiges.Add(dostig.Key, dostig.Value);
                                    }
                                    if (!dostiges.ContainsKey(dostij.Key))
                                    {
                                        string xsbutton1 = Xbutton + " " + ybutton;
                                        string xsbutton2 = Xbutton2 + " " + ybutton2;
                                        container.Add(new CuiElement
                                        {
                                            Name = CuiHelper.GetGuid(),
                                            Parent = panel,
                                            Components =
                        {
                            new CuiTextComponent
                            {
                                Text = gettext(dostij.Key, dostij.Value), Align = TextAnchor.MiddleLeft
                            },
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                                        });
                                        container.Add(new CuiButton
                                        {

                                            Button =
                                    {
                                        Command = "takeprize " + dostij.Key, Color = "0.407843137254902 0.2 0.8313725490196078 0.6",
                                    },
                                            Text =
                                    {
                                       Text = "Забрать", Align = TextAnchor.MiddleCenter
                                    },
                                            RectTransform =
                                    {
                                        AnchorMin = xsbutton1, AnchorMax = xsbutton2
                                    }

                                        }, panel, "Buttonget");
                                    }
                                }

                                if (test.Value.dostij == null)
                                {
                                    string xsbutton1 = Xbutton + " " + ybutton;
                                    string xsbutton2 = Xbutton2 + " " + ybutton2;
                                    container.Add(new CuiElement
                                    {
                                        Name = CuiHelper.GetGuid(),
                                        Parent = panel,
                                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = gettext(dostij.Key, dostij.Value), Align = TextAnchor.MiddleLeft
                            },
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                                    });
                                    container.Add(new CuiButton
                                    {

                                        Button =
                                    {
                                        Command = "takeprize " + dostij.Key, Color = "0.407843137254902 0.2 0.8313725490196078 0.6",
                                    },
                                        Text =
                                    {
                                       Text = "Забрать", Align = TextAnchor.MiddleCenter
                                    },
                                        RectTransform =
                                    {
                                        AnchorMin = xsbutton1, AnchorMax = xsbutton2
                                    }

                                    }, panel, "Buttonget");
                                }
                            }
                        }
                    }
                    ymin = ymin - 0.1;
                    ymin2 = ymin2 - 0.1;
                    ybutton = ybutton - 0.1;
                    ybutton2 = ybutton2 - 0.1;
                }
            }
            Xmin = 0.05;
            Xmin2 = 0.95;
            ymin = 0.82;
            ymin2 = 0.9;
            Xbutton = 0.60;
            Xbutton2 = 0.85;
            ybutton = 0.84;
            ybutton2 = 0.88;
            CuiHelper.AddUi(player, container);
                }
        private string gettext(string key, int count)
        {
            string text = "Xex";
            int getmaxs = getmax(key);
            if (key.Equals("CollectWood")) { if (count < getmaxs) { text = "Добудьте 500000 дерева. Добыто: " +  + count + "/" + getmaxs + "\n Приз: 100 монет, 1 кейс"; }  if (count >= getmaxs) { text = "           Добудьте 500000 дерева - "; } }
            if (key.Equals("CollectStone")) { if (count < getmaxs) { text = "Добудьте 300000 камня. Добыто: " + count + "/" + getmaxs + "\n Приз: 150 монет"; } if (count >= getmaxs) { text = "           Добудьте 300000 камня - "; } }
            if (key.Equals("CollectMetall")) { if (count < getmaxs) { text = "Добудьте 200000 металла. Добыто: " + count + "/" + getmaxs + "\n Приз: 180 монет, 1 кейс"; } if (count >= getmaxs) { text = "           Добудьте 200000 металла - "; } }
            if (key.Equals("CollectSulfur")) { if (count < getmaxs) { text = "Добудьте 100000 серы. Добыто: " + count + "/" + getmaxs + "\n Приз: 200 монет, 1 кейс"; } if (count >= getmaxs) { text = "           Добудьте 100000 серы - "; } }
            if (key.Equals("CollectHQM")) { if (count < getmaxs) { text = "Добудьте 1000 мвк. Добыто: " + count + "/" + getmaxs + "\n Приз: 140 монет"; } if (count >= getmaxs) { text = "           Добудьте 1000 мвк - "; } }
            if (key.Equals("KillAnimals")) { if (count < getmaxs) { text = "Убейте 100 животных. Убито: " + count + "/" + getmaxs + "\n Приз: 50 монет"; } if (count >= getmaxs) { text = "           Убейте 100 животных - "; } }
            if (key.Equals("KillPlayers")) { if (count < getmaxs) { text = "Убейте 20 игроков. Убито: " + count + "/" + getmaxs + "\n Приз: 120 монет."; } if (count >= getmaxs) { text = "           Убейте 20 игроков - "; } }
            if (key.Equals("CrateBuildings")) { if (count < getmaxs) { text = "Постройте 1000 объектов. Построено: " + count + "/" + getmaxs + "\n Приз: 1 кейс, 50 монет"; } if (count >= getmaxs) { text = "           Постройте 1000 объектов - "; } }
            if (key.Equals("CraftRifle")) { if (count < getmaxs) { text = "Скрафтите 25 винтовок. Скрафчено: " + count + "/" + getmaxs + "\n Приз: 60 монет"; } if (count >= getmaxs) { text = "           Скрафтите 25 винтовок - "; } }
            if (key.Equals("KillModerators")) { if (count < getmaxs) { text = "Убейте модератора. Убито: " + count + "/" + getmaxs + "\n Приз: 2 кейса, 120 монет"; } if (count >= getmaxs) { text = "           Убейте модератора - "; } }
            if (key.Equals("WinEvent")) { if (count < getmaxs) { text = "Выиграйте 10 ивентов. Выиграно: " + count + "/" + getmaxs + "\n Приз: 2 кейса, 200 монет"; } if (count >= getmaxs) { text = "           Выиграйте 10 ивентов - "; } }
            if (key.Equals("CraftExplosiveC4")) { if (count < getmaxs) { text = "Скрафтите 50 c4. Скрафчено: " + count + "/" + getmaxs + "\n Приз: 100 монет"; } if (count >= getmaxs) { text = "           Скрафтите 50 c4 - "; } }
            if (key.Equals("CraftExplosiveRockets")) { if (count < getmaxs) { text = "Скрафтите 100 ракет. Скрафчено: " + count + "/" + getmaxs + "\n Приз: 110 монет, 1 кейс"; } if (count >= getmaxs) { text = "           Скрафтите 100 ракет - "; } }
            return text;
        }
        private int getmax(string name)
        {
            int countmax = 0;
            if (name.Equals("CollectWood")) { countmax = 500000; }
            if (name.Equals("CollectStone")) { countmax = 300000; }
            if (name.Equals("CollectMetall")) { countmax = 200000; }
            if (name.Equals("CollectSulfur")) { countmax = 100000; }
            if (name.Equals("CollectHQM")) { countmax = 1000; }
            if (name.Equals("KillAnimals")) { countmax = 100; }
            if (name.Equals("KillPlayers")) { countmax = 20; }
            if (name.Equals("CrateBuildings")) { countmax = 1000; }
            if (name.Equals("CraftRifle")) { countmax = 25; }
            if (name.Equals("KillModerators")) { countmax = 1; }
            if (name.Equals("WinEvent")) { countmax = 10; }
            if (name.Equals("CraftExplosiveC4")) { countmax = 50; }
            if (name.Equals("CraftExplosiveRockets")) { countmax = 100; }
            return countmax;
        }
        void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            achievslistadd(player, "CrateBuildings", 1);
        }
        [ConsoleCommand("createachivmenu")]
        void achivscreatemenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            createachievmentsmenu(player);
        }
        [ConsoleCommand("takeprize")]
        void takeprize(ConsoleSystem.Arg arg)
        {
            Dictionary<string, bool> dostij = new Dictionary<string, bool>();
            Achievs achivs = new Achievs();
            BasePlayer player = arg.Connection.player as BasePlayer;
            string name = arg.Args[0];
            var achievsdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            foreach (var players in achievsdata)
            {
                if(players.Key.Equals(player.UserIDString))
                {
                   achivs = players.Value;
                }
            }
            if(achivs.dostij != null && achivs.dostij.Count > 0)
            foreach(var dostijesg in achivs.dostij)
            {
                dostij.Add(dostijesg.Key, dostijesg.Value);
            }
            if (name.Equals("CollectStone")) { player.ChatMessage("Вам были выданы: 100 монет и 1 кейс!"); dostij.Add("CollectStone", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 100); GiveCase(player, 1); }
            if (name.Equals("CollectWood")) { player.ChatMessage("Вам были выданы: 150 монет!"); dostij.Add("CollectWood", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 150); }
            if (name.Equals("CollectMetall")) { player.ChatMessage("Вам были выданы: 180 монет и 1 кейс!"); dostij.Add("CollectMetall", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 180); GiveCase(player, 1); }
            if (name.Equals("CollectSulfur")) { player.ChatMessage("Вам были выданы: 200 монет и 1 кейс!"); dostij.Add("CollectSulfur", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 200); GiveCase(player, 1); }
            if (name.Equals("CollectHQM")) { player.ChatMessage("Вам были выданы: 140 монет!"); dostij.Add("CollectHQM", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 140); }
            if (name.Equals("KillAnimals")) { player.ChatMessage("Вам были выданы: 50 монет!"); dostij.Add("KillAnimals", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 50); }
            if (name.Equals("KillPlayers")) { player.ChatMessage("Вам были выданы: 120 монет!"); dostij.Add("KillPlayers", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 120); }
            if (name.Equals("CrateBuildings")) { player.ChatMessage("Вам были выданы: 50 монет и 1 кейс!"); dostij.Add("CrateBuildings", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 50); GiveCase(player, 1); }
            if (name.Equals("CraftRifle")) { player.ChatMessage("Вам были выданы: 100 монет и 1 кейс!"); dostij.Add("CraftRifle", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 100); GiveCase(player, 1); }
            if (name.Equals("KillModerators")) { player.ChatMessage("Вам были выданы: 120 монет и 2 кейса!"); dostij.Add("KillModerators", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 120); GiveCase(player, 2); }
            if (name.Equals("WinEvent")) { player.ChatMessage("Вам были выданы: 200 монет и 2 кейса!"); dostij.Add("WinEvent", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 200); GiveCase(player, 2); }
            if (name.Equals("CraftExplosiveRockets")) { player.ChatMessage("Вам были выданы: 100 монет!"); dostij.Add("CraftExplosiveRockets", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 100); }
            if (name.Equals("CraftExplosiveC4")) { player.ChatMessage("Вам были выданы: 110 монет и 1 кейс!"); dostij.Add("CraftExplosiveC4", true); achievsplayeradd(player, dostij, achivs.dones); createachievmentsmenu(player); AddBalance(player.userID, 110); GiveCase(player, 1); }
        }
        public void createachievmentsmenu(BasePlayer player)
        {
            var allachievsofplayer = getallachivments(player);
            CuiHelper.DestroyUi(player, "AchievmentsPanel");
            var achievsdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            CuiHelper.DestroyUi(player, "AchievmentsPanel2");
            Dictionary<string, int> dostijes = new Dictionary<string, int>();
            CuiElementContainer container = new CuiElementContainer();

            var panel = container.Add(new CuiPanel
            {
                Image = {
                    Color = "0.7686274509803922 0.5529411764705882 0.7137254901960784 0.4",
                },
                CursorEnabled = true,
                RectTransform =
                {
                    AnchorMin = "0.35 0.11",
                    AnchorMax = "0.60 0.95",
                }

            }, "Hud", "AchievmentsPanel");
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiButtonComponent {Close = "AchievmentsPanel", Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "-5 -4", AnchorMax = "0 5"}
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiButtonComponent {Close = "AchievmentsPanel", Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "1 -4", AnchorMax = "6 5"}
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiTextComponent {Text = "Достижения", Align = TextAnchor.UpperLeft, FontSize = 20},
                    new CuiRectTransformComponent {AnchorMin = "0.35 0.92", AnchorMax = "0.8 0.98"}
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = "https://i.imgur.com/8poCpfR.png"
                    },
                    new CuiRectTransformComponent
                    {
                    AnchorMin = "0.80 0.02",
                    AnchorMax = "0.90 0.08",
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Url = "https://i.imgur.com/xyzRFoD.png"

                    },
                    new CuiRectTransformComponent
                    {
                    AnchorMin = "0.1 0.02",
                    AnchorMax = "0.2 0.08",
                    }
                }
            });
            container.Add(new CuiButton
            {
                Button =
                {
                  Command = "page2open",
                  Color = "0 0 0 0",
                },
                Text =
                {
                    Text = ""
                },
                RectTransform =
                {
                    AnchorMin = "0.80 0.02",
                    AnchorMax = "0.90 0.08",
                }
            }, panel, "Paget2");
            foreach (var dostij in allachievsofplayer)
            {
                string xs = Xmin + " " + ymin;
                string ys = Xmin2 + " " + ymin2;

                
                if (ymin > 0.12)
                {
                    container.Add(new CuiPanel
                    {
                        Image =
                {
                    Color = "0.5490196078431373 0.1843137254901961 0.8313725490196078 0.7"
                },
                        RectTransform =
                {
                    AnchorMin = xs,
                    AnchorMax = ys,
                }


                    }, panel, "Dostjenie");
                    if (dostij.Value < getmax(dostij.Key))
                    {
                        container.Add(new CuiElement
                        {
                            Name = CuiHelper.GetGuid(),
                            Parent = panel,
                            Components =
                        {
                            new CuiTextComponent
                            {
                                Text = gettext(dostij.Key, dostij.Value), Align = TextAnchor.MiddleCenter
                            },
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                        });
                    }
                    if (dostij.Value >= getmax(dostij.Key))
                    {
                        foreach (var test in achievsdata)
                        {
                            if (test.Key.Equals(player.UserIDString))
                            {
                                if (test.Value.dostij != null)
                                {
                                    foreach (var dostijenia in test.Value.dostij)
                                    {
                                        if (dostijenia.Key.Equals(dostij.Key))
                                        {
                                            container.Add(new CuiElement
                                            {
                                                Name = CuiHelper.GetGuid(),
                                                Parent = panel,
                                                Components =
                        {
                            new CuiTextComponent { Text = gettext(dostij.Key, dostij.Value) + " Выполнено", Align = TextAnchor.MiddleCenter},
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        foreach (var test in achievsdata)
                        {
                            Dictionary<string, bool> dostiges = new Dictionary<string, bool>();
                            if (test.Key.Equals(player.UserIDString))
                            {
                                if (test.Value.dostij != null)
                                {
                                    foreach (var dostig in test.Value.dostij)
                                    {
                                        dostiges.Add(dostig.Key, dostig.Value);
                                    }
                                    if (!dostiges.ContainsKey(dostij.Key))
                                    {
                                        string xsbutton1 = Xbutton + " " + ybutton;
                                        string xsbutton2 = Xbutton2 + " " + ybutton2;
                                        container.Add(new CuiElement
                                        {
                                            Name = CuiHelper.GetGuid(),
                                            Parent = panel,
                                            Components =
                        {
                            new CuiTextComponent
                            {
                                Text = gettext(dostij.Key, dostij.Value), Align = TextAnchor.MiddleLeft
                            },
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                                        });
                                        container.Add(new CuiButton
                                        {

                                            Button =
                                    {
                                        Command = "takeprize " + dostij.Key, Color = "0.407843137254902 0.2 0.8313725490196078 0.6",
                                    },
                                            Text =
                                    {
                                       Text = "Забрать", Align = TextAnchor.MiddleCenter
                                    },
                                            RectTransform =
                                    {
                                        AnchorMin = xsbutton1, AnchorMax = xsbutton2
                                    }

                                        }, panel, "Buttonget");
                                    }
                                }

                                if (test.Value.dostij == null)
                                {
                                    string xsbutton1 = Xbutton + " " + ybutton;
                                    string xsbutton2 = Xbutton2 + " " + ybutton2;
                                    container.Add(new CuiElement
                                    {
                                        Name = CuiHelper.GetGuid(),
                                        Parent = panel,
                                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = gettext(dostij.Key, dostij.Value), Align = TextAnchor.MiddleLeft
                            },
                            new CuiRectTransformComponent { AnchorMin = xs, AnchorMax = ys }
                        }
                                    });
                                    container.Add(new CuiButton
                                    {

                                        Button =
                                    {
                                        Command = "takeprize " + dostij.Key, Color = "0.407843137254902 0.2 0.8313725490196078 0.6",
                                    },
                                        Text =
                                    {
                                       Text = "Забрать", Align = TextAnchor.MiddleCenter
                                    },
                                        RectTransform =
                                    {
                                        AnchorMin = xsbutton1, AnchorMax = xsbutton2
                                    }

                                    }, panel, "Buttonget");
                                }
                            }
                        }
                    }
                    ymin = ymin - 0.1;
                    ymin2 = ymin2 - 0.1;
                    ybutton = ybutton - 0.1;
                    ybutton2 = ybutton2 - 0.1;
                }
                if (ymin < 0.12)
                {
                    if (!dostij.Key.Equals("CrateBuildings"))
                    {
                        dostijes.Add(dostij.Key, dostij.Value);
                    }
                }
            }
            if (pagea.ContainsKey(player.userID))
            {
                pagea.Remove(player.userID);
            }
                pagea.Add(player.userID, dostijes);
            
        Xmin = 0.05;
        Xmin2 = 0.95;
        ymin = 0.82;
        ymin2 = 0.9;
        Xbutton = 0.60;
        Xbutton2 = 0.85;
        ybutton = 0.84;
        ybutton2 = 0.88;
            CuiHelper.AddUi(player, container);
        }
        void achievsplayeradd(BasePlayer player, Dictionary<string, bool> dostij, Dictionary<string, int> dones)
        {
            var achievdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            Dictionary<string, Achievs> playersachievs = new Dictionary<string, Achievs>();
            achievementses.Add(player.UserIDString, new Achievs()
            {
                dones = dones,
                dostij = dostij,
                name = player.displayName,
                uid = player.UserIDString
            });
            foreach (var pldf in achievdata)
            {
                if (pldf.Value != null)
                {
                    if (!pldf.Value.uid.Equals(player.UserIDString))
                    {
                        achievementses.Add(pldf.Value.uid, new Achievs()
                        {
                            name = pldf.Value.name,
                            dostij = pldf.Value.dostij,
                            dones = pldf.Value.dones,
                            uid = pldf.Value.uid,
                        });
                    }
                }
            }
          Interface.Oxide.DataFileSystem.WriteObject("Achievments/achievs", achievementses);
            achievementses = new Dictionary<string, Achievs>();
        }
        void OnPlayerInit(BasePlayer player)
        {
            Dictionary<string, Achievs> PlayerAchievs = new Dictionary<string, Achievs>();
            PlayerAchievs = players_File.ReadObject<Dictionary<string, Achievs>>();
            var achievdata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Achievs>>("Achievments/achievs");
            if (!achievdata.ContainsKey(player.UserIDString))
            {
                PlayerAchievs.Add(player.UserIDString, new Achievs()
                {
                    name = player.displayName,
                    uid = player.UserIDString,
                    dostij = null,
                    dones = null,
                });
                Interface.Oxide.DataFileSystem.WriteObject("Achievments/achievs", PlayerAchievs);
            }
        }
        void OnServerIntializied()
        {
        }
    }
}
                                                                                                     