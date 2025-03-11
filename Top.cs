using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Top", "RustPlugin.ru", "1.1.0")]

    class Top : RustPlugin
    {
        private int MessageNum = 0;
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание конфига");
            Config.Clear();
            Config["Цвет Панели"] = "0.0 0.0 0.0 0.8";
            Config["Время Между Сообщениями"] = 300f;
            Config["Цвет Оповещаний"] = "#ffa500";
            SaveConfig();
        }

        [ChatCommand("rank")]
        void TurboRankCommand(BasePlayer player, string command)
        {
            var TopPlayer = (from x in Tops where x.UID == player.UserIDString select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано);
            player.ChatMessage($"<size=15>Статистика игрока <color=#ffa500>{player.displayName}</color></size>");
            foreach (var top in TopPlayer)
            {
                SendReply(player, $"<size=14><color=#ffffff>Убийств игроков: <color=#ffa500>{top.УбийствPVP}</color> | Смертей: <color=#ffa500>{top.Смертей}</color></color></size>");
                SendReply(player, $"<size=14><color=#ffffff>Ракет выпущено: <color=#ffa500>{top.РакетВыпущено}</color> | Взрывчаток использовано: <color=#ffa500>{top.ВзрывчатокИспользовано}</color></color></size>");
                SendReply(player, $"<size=14><color=#ffffff>Ресурсов собрано: <color=#ffa500>{top.РесурсовСобрано}</color> | Животных убито: <color=#ffa500>{top.УбийствЖивотных}</color></color></size>");
                SendReply(player, $"<size=14><color=#ffffff>Пуль выпущено: <color=#ffa500>{top.ПульВыпущено}</color> | Стрел выпущено: <color=#ffa500>{top.СтрелВыпущено}</color></color></size>");
                SendReply(player, $"<size=14><color=#ffffff>Предметов скрафчено: <color=#ffa500>{top.ПредметовСкрафчено}</color> | Вертолетов сбито: <color=#ffa500>{top.ВертолётовУничтожено}</color></color></size>");
                SendReply(player, $"<size=14><color=#ffffff>NPC убито: <color=#ffa500>{top.NPCУбито}</color> | Танков уничтожено: <color=#ffa500>{top.ТанковУничтожено}</color></color></size>");
            }
            return;
        }

        [ChatCommand("top")]
        void TurboTopCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 1)
            {
                int n = 0;
                if (args[0] == "reset")
                {
                    if (!player.IsAdmin)
                    {
                        SendReply(player, $"<size=14><color=#FFA500>Ты кто такой? Давай досвиданье!</color></size>");
                        return;
                    }
                    var TopPlayer = (from x in Tops select x);
                    foreach (var top in TopPlayer)
                    {
                        top.Reset();
                        Saved();
                    }
                    SendReply(player, $"<size=14><color=#FFA500>Статистика игроков обнулена!</color></size>");
                    return;
                }
                if (args[0] == "farm")
                {
                    bool prov = false;
                    player.ChatMessage("<size=14><color=#FF6347>[СТАТИСТИКА]</color> ТОП Фармеров</size>");
                    var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РесурсовСобрано);
                    foreach (var top in TopPlayer)
                    {
                        n++;
                        if (n <= 5)
                        {
                            player.SendConsoleCommand("chat.add", top.UID, $"<size=14><color=#FFA500>{n}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РесурсовСобрано})</size>");
                            if (top.UID == player.UserIDString)
                            {
                                prov = true;
                            }
                        }
                    }
                    if (!prov)
                    {
                        player.ChatMessage("...");
                        int i = 0;
                        foreach (var top in TopPlayer)
                        {
                            i++;
                            if (top.UID == player.UserIDString)
                            {
                                player.SendConsoleCommand("chat.add", player.UserIDString, $"<size=14><color=#FFA500>{i}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РесурсовСобрано})</size>");
                            }
                        }
                    }
                    return;
                }
                if (args[0] == "pvp")
                {
                    bool prov = false;
                    player.ChatMessage("<size=14><color=#FF6347>[СТАТИСТИКА]</color> ТОП Убийств PVP</size>");
                    var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP);
                    foreach (var top in TopPlayer)
                    {
                        n++;
                        if (n <= 5)
                        {
                            player.SendConsoleCommand("chat.add", top.UID, $"<size=14><color=#FFA500>{n}.</color> <color=#FF8C00>{top.Ник}</color> ({top.УбийствPVP})</size>");
                            if (top.UID == player.UserIDString)
                            {
                                prov = true;
                            }
                        }
                    }
                    if (!prov)
                    {
                        player.ChatMessage("<size=14>...</size>");
                        int i = 0;
                        foreach (var top in TopPlayer)
                        {
                            i++;
                            if (top.UID == player.UserIDString)
                            {
                                player.SendConsoleCommand("chat.add", player.UserIDString, $"<size=14><color=#FFA500>{i}.</color> <color=#FF8C00>{top.Ник}</color> ({top.УбийствPVP})</size>");
                            }
                        }
                    }
                    return;
                }
                if (args[0] == "raid")
                {
                    bool prov = false;
                    player.ChatMessage("<size=14><color=#FF6347>[СТАТИСТИКА]</color> ТОП Рейдеров</size>");
                    var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано);
                    foreach (var top in TopPlayer)
                    {
                        n++;
                        if (n <= 5)
                        {
                            player.SendConsoleCommand("chat.add", top.UID, $"<size=14><color=#FFA500>{n}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РакетВыпущено + top.ВзрывчатокИспользовано})</size>");
                            if (top.UID == player.UserIDString)
                            {
                                prov = true;
                            }
                        }
                    }
                    if (!prov)
                    {
                        player.ChatMessage("<size=14>...</size>");
                        int i = 0;
                        foreach (var top in TopPlayer)
                        {
                            i++;
                            if (top.UID == player.UserIDString)
                            {
                                player.SendConsoleCommand("chat.add", player.UserIDString, $"<size=14><color=#FFA500>{i}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РакетВыпущено + top.ВзрывчатокИспользовано})</size>");
                            }
                        }
                    }
                    return;
                }
            }
            else
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Панелька");
                CuiElementContainer elements = CreatePanel("0");
                CuiHelper.AddUi(player, elements);
            }
        }

        [ConsoleCommand("top.show")]
        private void TopShowOpenCmd2(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Панелька");
            string number = arg.Args[0];
            CuiElementContainer elements = CreatePanel(number);
            CuiHelper.AddUi(player, elements);
            return;
        }

        CuiElementContainer CreatePanel(string number)
        {
            string cvet = Convert.ToString(Config["Цвет Панели"]);
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.75" },
                RectTransform = { AnchorMin = "0.2 0.13", AnchorMax = "0.8 0.95" },
                CursorEnabled = true
            }, "Hud", "Панелька");
            elements.Add(new CuiPanel
            {
                Image = { Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0 0.81", AnchorMax = "1 1" },
            }, panel);

            elements.Add(new CuiLabel
            {
                Text = { Text = "<color=#ffa500>TOP 10 ИГРОКОВ</color>", FontSize = 30, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.89", AnchorMax = "1 1" },
            }, panel);
            elements.Add(new CuiButton
            {
                Button = { Command = "top.exit", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.9 0.90", AnchorMax = "1 1" },
                Text = { Text = "<color=#ffa500>X</color>", FontSize = 18, Align = TextAnchor.MiddleCenter }
            }, panel);

            elements.Add(new CuiPanel
            {
                Image = { Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0 0.81", AnchorMax = "0.298 0.8899999" },
            }, panel);
            elements.Add(new CuiLabel
            {
                Text = { Text = "<color=#ffa500>Игрок</color>", FontSize = 20, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.81", AnchorMax = "0.29 0.8899999" },
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Убийства</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 1", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.30 0.81", AnchorMax = "0.454 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Смертей</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 2", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.455 0.81", AnchorMax = "0.578 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Животные</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 3", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.58 0.81", AnchorMax = "0.678 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Взрывов</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 4", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.68 0.81", AnchorMax = "0.859 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Ресурсы</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 5", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.86 0.81", AnchorMax = "1 0.8899999" }
            }, panel);

            string polosa = "0 0 0 0.9";
            int n = 0;
            var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(10);
            if (number == "2")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.Смертей).Take(10);
            }
            else if (number == "3")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствЖивотных).Take(10);
            }
            else if (number == "4")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано).Take(10);
            }
            else if (number == "5")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РесурсовСобрано).Take(10);
            }
            else
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(10);
            }
            foreach (var top in TopPlayer)
            {
                if (n % 2 == 0)
                {
                    polosa = "0 0 0 0.7";
                }
                else
                {
                    polosa = "1 1 1 0.05";
                }
                elements.Add(new CuiPanel
                {
                    Image = { Color = polosa },
                    RectTransform = { AnchorMin = $"0 {0.72 - (n * 0.08)}", AnchorMax = $"1 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.Ник), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 {0.72 - (n * 0.08)}", AnchorMax = $"0.29 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.УбийствPVP), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.3 {0.72 - (n * 0.08)}", AnchorMax = $"0.454 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.Смертей), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.455 {0.72 - (n * 0.08)}", AnchorMax = $"0.578 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.УбийствЖивотных), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.58 {0.72 - (n * 0.08)}", AnchorMax = $"0.678 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{ Convert.ToString(top.ВзрывчатокИспользовано + top.РакетВыпущено)}", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.68 {0.72 - (n * 0.08)}", AnchorMax = $"0.85 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.РесурсовСобрано), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.86 {0.72 - (n * 0.08)}", AnchorMax = $"0.99 {0.8 - (n * 0.08)}" },
                }, panel);
                n++;
            }
            return elements;
        }

        [ConsoleCommand("top.exit")]
        private void MagazineOpenCmd2(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            BasePlayer player = arg.Player();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Панелька");
        }

        private Dictionary<uint, string> LastHeliHit = new Dictionary<uint, string>();

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                LastHeliHit[entity.net.ID] = info.InitiatorPlayer.UserIDString;
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null || info == null) return;
            BasePlayer victimBP = victim.ToPlayer();
            BasePlayer initiator = info.InitiatorPlayer;
            if (victimBP != null && !IsNPC(victimBP))
            {
                string death = victimBP.UserIDString;
                TopData con = (from x in Tops where x.UID == death select x).FirstOrDefault();
                con.Смертей += 1;
            }
            if (initiator == null)
            {
                if (victim is BaseHelicopter)
                {
                    if (LastHeliHit.ContainsKey(victim.net.ID))
                    {
                        TopData data = Tops.Where(p => p.UID == LastHeliHit[victim.net.ID]).FirstOrDefault();
                        data.ВертолётовУничтожено += 1;
                        LastHeliHit.Remove(victim.net.ID);
                    }
                }
                return;
            }
            if (initiator != null && !IsNPC(initiator))
            {
                string killer = initiator.UserIDString;
                TopData con2 = (from x in Tops where x.UID == killer select x).FirstOrDefault();
                if (IsNPC(victimBP))
                {
                    con2.NPCУбито++;
                    return;
                }
                if (victim is BaseAnimalNPC)
                {
                    con2.УбийствЖивотных += 1;
                    return;
                }
                if (victim is BradleyAPC)
                {
                    con2.ТанковУничтожено++;
                    return;
                }
                if (victimBP != null && victimBP != initiator)
                {
                    con2.УбийствPVP += 1;
                    return;
                }
            }
            return;
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.ВзрывчатокИспользовано += 1;
        }

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.РакетВыпущено += 1;
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            if (projectile.primaryMagazine.definition.ammoTypes == Rust.AmmoTypes.BOW_ARROW)
            {
                con.СтрелВыпущено += 1;
            }
            else
            {
                con.ПульВыпущено += 1;
            }
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (task.owner is BasePlayer)
            {
                TopData con = (from x in Tops where x.UID == Convert.ToString(task.owner.userID) select x).FirstOrDefault();
                con.ПредметовСкрафчено += 1;
            }
        }

        void OnServerSave()
        {
            Saved();
        }

        void Unload()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Панелька");
            }
            Saved();
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            DoGather(player, item);
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || !(entity is BasePlayer) || item == null || dispenser == null) return;
            if (entity.ToPlayer() is BasePlayer)
                DoGather(entity.ToPlayer(), item);
        }

        void DoGather(BasePlayer player, Item item)
        {
            if (player == null) return;
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.РесурсовСобрано += item.amount;
            return;
        }

        void CreateInfo(BasePlayer player)
        {
            if (player == null) return;
            Tops.Add(new TopData(player.displayName, player.UserIDString));
        }

        void OnPlayerInit(BasePlayer player)
        {
            var check = (from x in Tops where x.UID == player.UserIDString select x).Count();
            if (check == 0) CreateInfo(player);
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.Ник = (string)player.displayName;
        }

        void Loaded()
        {
            Tops = Interface.Oxide.DataFileSystem.ReadObject<List<TopData>>("TopData");
            foreach (var player in BasePlayer.activePlayerList)
            {
                var check = (from x in Tops where x.UID == player.UserIDString select x).Count();
                if (check == 0) CreateInfo(player);
            }
            timer.Repeat(Convert.ToInt32(Config["Время Между Сообщениями"]), 0, () =>
            {
                MessageNum++;
                TopData data = null;
                switch (MessageNum)
                {
                    case 1:
                        data = Tops.OrderByDescending(p => p.УбийствPVP).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Киллер</color> - {data.Ник} ({data.УбийствPVP})</size>");
                        break;
                    case 2:
                        data = Tops.OrderByDescending(p => p.ВзрывчатокИспользовано + p.РакетВыпущено).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Рейдер</color> - {data.Ник} ({data.ВзрывчатокИспользовано + data.РакетВыпущено})</size>");
                        break;
                    case 3:
                        data = Tops.OrderByDescending(p => p.УбийствЖивотных).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Убийца животных</color> - {data.Ник} ({data.УбийствЖивотных})</size>");
                        break;
                    case 4:
                        data = Tops.OrderByDescending(p => p.ПульВыпущено).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Выпустил пуль</color> - {data.Ник} ({data.ПульВыпущено})</size>");
                        break;
                    case 5:
                        data = Tops.OrderByDescending(p => p.СтрелВыпущено).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Выпустил стрел</color> - {data.Ник} ({data.СтрелВыпущено})</size>");
                        break;
                    case 6:
                        data = Tops.OrderByDescending(p => p.Смертей).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Смертей</color> - {data.Ник} ({data.Смертей})</size>");
                        break;
                    case 7:
                        data = Tops.OrderByDescending(p => p.ПредметовСкрафчено).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Крафтер</color> - {data.Ник} ({data.ПредметовСкрафчено})</size>");
                        break;
                    case 8:
                        data = Tops.OrderByDescending(p => p.РесурсовСобрано).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Добытчик</color> - {data.Ник} ({data.РесурсовСобрано})</size>");
                        break;
                    case 9:
                        data = Tops.OrderByDescending(p => p.ВертолётовУничтожено).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Убийца вертолётов</color> - {data.Ник} ({data.ВертолётовУничтожено})</size>");
                        break;
                    case 10:
                        data = Tops.OrderByDescending(p => p.NPCУбито).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Убийца NPC</color> - {data.Ник} ({data.NPCУбито})</size>");
                        break;
                    case 11:
                        data = Tops.OrderByDescending(p => p.ТанковУничтожено).FirstOrDefault();
                        if (data != null)
                            Server.Broadcast($"<size=16><color={Convert.ToString(Config["Цвет Оповещаний"])}>TOP Убийца танков</color> - {data.Ник} ({data.ТанковУничтожено})</size>");
                        MessageNum = 0;
                        break;
                }
            });
        }

        void Saved()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TopData", Tops);
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer)
                return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }

        public List<TopData> Tops = new List<TopData>();

        public class TopData
        {
            public TopData(string Ник, string UID)
            {
                this.Ник = Ник;
                this.UID = UID;
                this.РакетВыпущено = 0;
                this.УбийствPVP = 0;
                this.ВзрывчатокИспользовано = 0;
                this.УбийствЖивотных = 0;
                this.ПульВыпущено = 0;
                this.СтрелВыпущено = 0;
                this.Смертей = 0;
                this.ПредметовСкрафчено = 0;
                this.РесурсовСобрано = 0;
                this.ВертолётовУничтожено = 0;
                this.NPCУбито = 0;
                this.ТанковУничтожено = 0;
            }
            public void Reset()
            {
                this.РакетВыпущено = 0;
                this.УбийствPVP = 0;
                this.ВзрывчатокИспользовано = 0;
                this.УбийствЖивотных = 0;
                this.ПульВыпущено = 0;
                this.СтрелВыпущено = 0;
                this.Смертей = 0;
                this.ПредметовСкрафчено = 0;
                this.РесурсовСобрано = 0;
                this.ВертолётовУничтожено = 0;
                this.NPCУбито = 0;
                this.ТанковУничтожено = 0;
            }

            public string Ник { get; set; }
            public string UID { get; set; }
            public int РакетВыпущено { get; set; }
            public int УбийствPVP { get; set; }
            public int ВзрывчатокИспользовано { get; set; }
            public int УбийствЖивотных { get; set; }
            public int ПульВыпущено { get; set; }
            public int СтрелВыпущено { get; set; }
            public int Смертей { get; set; }
            public int ПредметовСкрафчено { get; set; }
            public int РесурсовСобрано { get; set; }
            public int ВертолётовУничтожено { get; set; }
            public int ТанковУничтожено { get; set; }
            public int NPCУбито { get; set; }
        }
    }
}
