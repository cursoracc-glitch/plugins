﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tournament", "Xavier", "1.0.0")]
    public class Tournament : RustPlugin
    {

        [PluginReference] private Plugin Clans;
        public class CupSettings
        {
            public ulong ownerclan;
            public bool isremove;
            public uint build;
            // МАРКЕР
            public VendingMachineMapMarker Marker;
        }
        public List<CupSettings> ClanData = new List<CupSettings>(); 

        void AddMarker(CupSettings settings)
        {
            string name = Clans?.Call("ClanAlready", settings.ownerclan) as string;
            var vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", (Vector3) BaseNetworkable.serverEntities.Find(settings.build)?.transform.position).GetComponent<VendingMachineMapMarker>();
            vendingMarker.markerShopName = name; 
            vendingMarker.Spawn();
            vendingMarker.enabled = false;
            settings.Marker = vendingMarker;
            WriteData();
        }
        void RemoveMarker(CupSettings settings)
        {
            // ReSharper disable once Unity.NoNullPropagation
            settings.Marker?.Kill();
        }

        void OnServerInitialized()
        {
            ReadData();
            if (ClanData.Count > 0)
            {
                foreach (var cup in ClanData)
                {
                    var entity = BaseNetworkable.serverEntities.Find(cup.build);
                    if (entity == null && cup.isremove == false)
                    {
                        cup.isremove = true;
                    }
                    if (entity != null && cup.isremove == false)
                    {
                        //AddMarker(cup);
                    }
                }
            }
            time += TimeSpan.FromSeconds(config.times);
            timer.Every(1f, () => { time -= TimeSpan.FromSeconds(1);});
            if (!Oxide.Core.Interface.Oxide.DataFileSystem.ExistsDatafile("Tournament/Data"))
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("Tournament/Data", ClanData);
        }
        void OnServerSave()
        {
            WriteData();
        }
        #region Configuration
        private Configur config;

        private class Configur
        {      
            [JsonProperty("Изначальное время в секундах")]
            public int times = 600;

            public static Configur GetNewConfiguration()
            {
                return new Configur
                {
                   
                };
            }
        }
        protected override void LoadDefaultConfig()
        {
            config = Configur.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<Configur>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion 
        void ReadData() => ClanData = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<CupSettings>>("Tournament/Data");
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("Tournament/Data", ClanData);
        TimeSpan time = TimeSpan.FromSeconds(0);
        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;

            result += $"{time.Hours.ToString("00")}:";

            result += $"{time.Minutes.ToString("00")}:";

            result += $"{time.Seconds.ToString("00")}";

            return result;
        }
        [ConsoleCommand("tournament")]
        void testcm(ConsoleSystem.Arg arg)
        {
            if(arg == null) return;
            if(arg.Player() != null && arg.Player().IsAdmin == false) return;
            var player = arg.Player();
            switch(arg.Args[0])
            {
                case "time":
                    {
                        if(arg.Player() == null)
                            Puts($"{FormatShortTime(time)}");
                        else
                            player.ConsoleMessage($"<color=green><size=20>{time} до начала</size></color>");
                        break;
                    }
                case "addtime":
                    {
                        var tim = int.Parse(arg.Args[1]);

                        time += TimeSpan.FromSeconds(tim);

                        if(arg.Player() == null)
                            Puts($"{tim} сек. добавлено");
                        else
                            player.ConsoleMessage($"<color=green><size=20>{tim} сек. добавленоn\n{time} до начала</size></color>");
                        break;
                    }
                case "removetime":
                    {
                        var tim = int.Parse(arg.Args[1]);

                        time -= TimeSpan.FromSeconds(tim);

                        if(arg.Player() == null)
                            Puts($"{tim} сек. убавлено");
                        else
                            player.ConsoleMessage($"<color=green><size=20>{tim} сек. убавлено\n{time} до начала</size></color>");
                        break;
                    }
            }
        }

        void Unload()
        {
            if (ClanData.Count > 0)
            {
                foreach (var cup in ClanData)
                {
                    if (cup.isremove == false || cup != null)
                    {
                        RemoveMarker(cup);
                    }
                }
            }
            WriteData();
            foreach(var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, CupLayer);
        }
        
        string CanRemove(BasePlayer player, BaseEntity entity)
        {
            if (!(entity as BuildingPrivlidge)) return null;
            var find = ClanData.FirstOrDefault(p => p.build == entity.net.ID);
            if (find != null && find.isremove == false)
            {
                return "Вы не имеете право удалять данный шкаф!";
            }
            return null;
        }
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (info.InitiatorPlayer == null) return null;
            if (entity as BuildingPrivlidge)
            {
                var find = ClanData.FirstOrDefault(p => p.build == entity.net.ID);
                if (find != null)
                {
                    var clan = Clans?.CallHook("CheckClans", entity.net.ID, info.InitiatorPlayer.OwnerID);
                    if (clan is bool && (bool)clan)
                    {
                        info.InitiatorPlayer.ChatMessage("Вы не имеете право уничтожать турнирный шкаф своего же клана!");
                        info.damageTypes.ScaleAll(0f);
                        return false;
                    }
                    if (entity.OwnerID == info.InitiatorPlayer.userID)
                    {
                        info.InitiatorPlayer.ChatMessage("Вы не имеете право уничтожать турнирный шкаф своего же клана!");
                        info.damageTypes.ScaleAll(0f);
                        return false;
                    }
                }
            }
            return null;
        }



        bool CheckTournament(ulong owner)
        {
            var find = ClanData.FirstOrDefault(p => p.ownerclan == owner);
            if (find == null)
            {
                return false;
            }
            if (find.isremove)
            {
                return false;
            }
            return true;
        }
        
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity as BuildingPrivlidge && info != null && info.InitiatorPlayer != null)
            {
                var find = ClanData.FirstOrDefault(p => p.build == entity.net.ID);
                if (find != null)
                {
                    if (find.isremove == true)
                    {
                        info.InitiatorPlayer.ChatMessage("Произошла критическая ошибка! Плагин не выдал очки");    
                        return;
                    }
                    Clans?.CallHook("ScoreRemove", find.ownerclan, info.InitiatorPlayer.userID);
                    info.InitiatorPlayer.ChatMessage("Вы уничтожили турнирный шкаф чужого клана!");
                    find.isremove = true;
                    string name = Clans?.Call("ClanAlready", find.ownerclan) as string;
                    RemoveMarker(find);
                    LogToFile("Tournament", $"Клан {name} вылетел из турнира в {DateTime.Now.ToString(CultureInfo.InvariantCulture)}", this);
                }
            }
        }
        
        public string CupLayer = "UI_CupLayer";
        
        [ConsoleCommand("registration.clanssss")]
        void RegClan(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            uint ent = uint.Parse(args.Args[0]);
            ClanData.Add(new CupSettings(){ownerclan = player.userID,isremove = false,build = ent});
            player.ChatMessage("Вы успешно зарегистрировались на турнир!");
            CuiHelper.DestroyUi(player, CupLayer);
            //AddMarker(new CupSettings(){ownerclan = player.userID,isremove = false,build = ent});
            string name = Clans?.Call("ClanAlready", new CupSettings(){ownerclan = player.userID,isremove = false,build = ent}.ownerclan) as string;
            LogToFile("Tournament", $"Клан {name} зарегистрировались на турнир в {DateTime.Now.ToString(CultureInfo.InvariantCulture)}", this);
            WriteData();
        }
        
        
        private static string HexToCuiColor(string hex) { if (string.IsNullOrEmpty(hex)) { hex = "#FFFFFFFF"; } var str = hex.Trim('#'); if (str.Length == 6) str += "FF"; if (str.Length != 8) { throw new Exception(hex); throw new InvalidOperationException("Cannot convert a wrong format."); } var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber); var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber); var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber); var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber); Color color = new Color32(r, g, b, a); return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}"; }
        void ui(BasePlayer player)
        {
            var text = $"До начала - {FormatShortTime(time)}";
            if(time < TimeSpan.FromSeconds(0))
                text = $"Турнир начался!";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "txt",
                Parent = CupLayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = text,
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter,
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.08181816 0.0208335",
                        AnchorMax = "0.9454545 0.4166667"
                    }
                }
            });
            CuiHelper.DestroyUi(player, "txt");
            CuiHelper.AddUi(player, container);
        }
        private readonly Dictionary<ulong, Timer> timers = new Dictionary<ulong, Timer>();
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null) return;
            if (entity as BuildingPrivlidge)
            {
                CuiHelper.DestroyUi(player, CupLayer);
                if (entity.OwnerID != player.userID) return;
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform =
                        {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-230 440", OffsetMax = "210 600"},
                    Image = {Color = "0 0 0 0"}
                }, "Overlay", CupLayer);
                
                timers[player.userID] = timer.Every(0.5f, () => { ui(player); });
                object clan = Clans?.CallHook("ClanCount", player.userID);
                if (clan is bool && !(bool) clan)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0.08181816 0.3208335", AnchorMax = "0.9454545 0.7166667"},
                        Button = {Color = HexToCuiColor("#919191BA"), Command = $""},
                        Text =
                        {
                            Text =
                                $"Ошибка! Возможно вы не состоите в клане, или же не являетесь главой клана в котором вы состоите",
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf", FontSize = 24
                        }
                    }, CupLayer);
                    CuiHelper.AddUi(player, container);
                    return;
                }

                var build = entity.GetBuildingPrivilege(entity.WorldSpaceBounds()).GetBuilding();
                var foundationlist = build.decayEntities.ToList().FindAll(p => p.PrefabName == "assets/prefabs/building core/foundation/foundation.prefab" || p.PrefabName == "assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab");
                if (foundationlist.Count < 1)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0.08181816 0.3208335", AnchorMax = "0.9454545 0.7166667"},
                        Button = {Color = HexToCuiColor("#919191BA"), Command = $""},
                        Text =
                        {
                            Text = $"Необходимо иметь 50 фундаментов!", Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf", FontSize = 24
                        }
                    }, CupLayer);
                    CuiHelper.AddUi(player, container);
                    return;
                }
                var find = ClanData.FirstOrDefault(p => p.ownerclan == player.userID);
                if (find != null)
                {
                    if (find.isremove == true)
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = {AnchorMin = "0.08181816 0.3208335", AnchorMax = "0.9454545 0.7166667"},
                            Button = {Color = HexToCuiColor("#919191BA"), Command = $""},
                            Text =
                            {
                                Text = $"Ваш шкаф уже был уничтожен! вы вылетели с турнира!",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf", FontSize = 24
                            }
                        }, CupLayer);
                        CuiHelper.AddUi(player, container);
                        return;
                    }

                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0.08181816 0.3208335", AnchorMax = "0.9454545 0.7166667"},
                        Button = {Color = HexToCuiColor("#919191BA"), Command = $""},
                        Text =
                        {
                            Text = $"Ошибка! Ваш клан уже зарегистрирован на турнир!", Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf", FontSize = 24
                        }
                    }, CupLayer);
                    CuiHelper.AddUi(player, container);
                    return;
                }

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.08181816 0.3208335", AnchorMax = "0.9454545 0.7166667"},
                    Button = {Color = HexToCuiColor("#919191BA"), Command = $"registration.clanssss {entity.net.ID}"},
                    Text =
                    {
                        Text = $"ЗАРЕГИСТРИРОВАТЬСЯ НА ТУРНИР!", Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf", FontSize = 24
                    }
                }, CupLayer);
                CuiHelper.AddUi(player, container);
            }
        }

        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) => CuiHelper.DestroyUi(player, CupLayer);


        object OnEntityGroundMissing(BaseEntity entity)
        {
            if (entity == null) return null;
            if (ClanData.Any(p => p.build == entity.net.ID))
            {
                return false;
            }
            return null;
        }
    }
}