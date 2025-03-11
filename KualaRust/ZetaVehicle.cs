using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("ZetaVehicle", "fermens", "0.1.3")]
    [Description("ПОЛНОЦЕННОЕ МЕНЮ ДЛЯ ПРОДАЖИ ЛИЧНОГО ТРАНСПОРТА ИГРОКАМ")]
    class ZetaVehicle : RustPlugin
    {
        #region Config
        private PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Транспортные средства")]
            public Dictionary<string, vehicle> prefabs;

            [JsonProperty("Фон-картинки")]
            public Dictionary<string, string> list;

            [JsonProperty("Сообщения")]
            public List<string> messages;

            [JsonProperty("Запретить садиться в чужой транспорт?")]
            public bool onlyfriends;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    prefabs = new Dictionary<string, vehicle>(),
                    list = new Dictionary<string, string>
                    {
                        { "background2", "https://img1.goodfon.ru/wallpaper/nbig/0/e8/abandoned-chevy-tucumcari-new.jpg" },
                        { "fon5", "https://gspics.org/images/2020/01/24/5MyHe.jpg"}
                    },
                    messages = new List<string>
                    {
                        "НЕ АБУЗЬ!",
                        "ВЫЙДИТЕ С ЧУЖОЙ ТЕРИТОРИИ!",
                        "У ВАС НЕТ РАЗРЕШЕНИЯ ДЛЯ ЭТОЙ КОМАНДЫ!",
                        "У ВАС УЖЕ ЕСТЬ АКТИВНЫЙ ТРАНСПОРТ ТЕКУЩЕГО ТИПА!\nОТОЗВИТЕ ЕГО, ЧТО БЫ ПРИЗВАТЬ НОВЫЙ!",
                        "У ВАС КД НА ЭТО ТРАНСПОРТНОЕ СРЕДСТВО,\nПОДОЖДИТЕ {time}.", // 4
                        "<color=#ffd479>{name}</color> В ВАШЕМ РАСПОРЯЖЕНИИ",
                        "У ВАС НЕТ ТРАНСПОРТНОГО СРЕДСТВА ТАКОГО ТИПА!",
                        "ВЫ НЕ МОЖЕТЕ ОТОЗВАТЬ ТРАНСПОРТНОЕ СРЕДСТВО,\nКОГДА В НЕМ КТО-ТО СИДИТ!", //7
                        "ВЫ УСПЕШНО ОТОЗВАЛИ ВАШЕ ТРАНСПОРТНОЕ СРЕДСТВО!",
                        "СЛЕДУЮЩАЯ СТРАНИЦА",
                        "ПРЕДЫДУЩАЯ СТРАНИЦА", //10
                        "НЕДОСТУПНО",
                        "ПРИЗВАТЬ",
                        "ОТОЗВАТЬ",//13
                        "ЗАКРЫТЬ МЕНЮ",
                        "подсказка: прыгните, чтобы закрыть меню",
                        "ДОСТУПНЫЕ ВАМ ТРАНСПОРТНЫЕ СРЕДСТВА", //16
                        "У ВАС НЕТ ДОСТУПНЫХ ТРАНСПОРТНЫХ СРЕДСТВ,\nКУПИТЕ РАЗРЕШЕНИЕ НА НИХ <color=#ffd479>В МАГАЗИНЕ</color>!",
                        "ВЫ ДОЛЖНЫ НАХОДИТЬСЯ В ВОДЕ!",
                    },
                    onlyfriends = false
                };
            }
        }
        #endregion

        #region Head
        [PluginReference] Plugin ImageLibrary;
        string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        void AddImage(string url, string shortname, ulong skin = 0)
        {
            ImageLibrary?.Call("AddImage", url, shortname, skin);
            gettimage(shortname);
        }

        class MyVehicles
        {
            public int lastpage = 0;
            public Dictionary<string, BaseEntity> vehicles = new Dictionary<string, BaseEntity>();
            public Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();
        }

        Dictionary<ulong, MyVehicles> vehicles = new Dictionary<ulong, MyVehicles>();

        class vehicle
        {
            [JsonProperty("Префаб")]
            public string prefab;

            [JsonProperty("Отображаемое название")]
            public string displayname;

            [JsonProperty("Картинка")]
            public string img;

            [JsonProperty("КД(минуты)")]
            public int cooldown;

            [JsonProperty("Бесконечное топливо")]
            public bool infinitefuel;

            [JsonProperty("Топливо")]
            public int fuel;

            [JsonProperty("Дистанция спавна")]
            public float distancespawn;
        }

        const string mainUI = "ZetaVehicle-01";
        const string nomainUI = "ZetaVehicle-02";
        const string drawUI = "ZetaVehicle-03";
        bool onJump;
        List<BasePlayer> active = new List<BasePlayer>();
        #endregion

        #region Main
        Dictionary<string, string> images = new Dictionary<string, string>();
        private void Init()
        {
            Unsubscribe("CanMountEntity");
        }

        void OnServerInitialized()
        {
            if (config.prefabs.Count.Equals(0))
            {
                config.prefabs.Add("testridablehorse", new vehicle { prefab = "assets/rust.ai/nextai/testridablehorse.prefab", displayname = "ЕЗДОВАЯ ЛОШАДЬ", img = "https://gspics.org/images/2020/01/24/5MNpI.png", cooldown = 60, distancespawn = 2f, infinitefuel = false, fuel = 0 });
                config.prefabs.Add("minicopter.entity", new vehicle { prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab", displayname = "МИНИКОПТЕР", img = "https://gspics.org/images/2020/01/24/5MfnD.png", cooldown = 60, distancespawn = 2f, fuel = 10, infinitefuel = true });
                config.prefabs.Add("scraptransporthelicopter", new vehicle { prefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab", displayname = "САМОДЕЛЬНЫЙ ВЕРТОЛЕТ", img = "https://gspics.org/images/2020/01/24/5mLnZ.png", cooldown = 60, distancespawn = 5f, fuel = 10, infinitefuel = false });
                config.prefabs.Add("sedantest.entity", new vehicle { prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab", displayname = "МАШИНА", img = "https://pic.moscow.ovh/images/2019/01/26/28c44213d440f9708aeb9c9ddc73dff5.png", cooldown = 60, distancespawn = 4f, infinitefuel = false, fuel = 0 });
                config.prefabs.Add("rowboat", new vehicle { prefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab", displayname = "МОТОРНАЯ ЛОДКА", img = "https://gspics.org/images/2020/01/24/5UEMw.png", cooldown = 60, distancespawn = 3f, fuel = 10, infinitefuel = true });
                config.prefabs.Add("rhib", new vehicle { prefab = "assets/content/vehicles/boats/rhib/rhib.prefab", displayname = "НАДУВНАЯ ЛОДКА", img = "https://gspics.org/images/2020/01/24/5UcxQ.png", cooldown = 60, distancespawn = 5f, fuel = 10, infinitefuel = true });
                config.prefabs.Add("hotairballoon", new vehicle { prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab", displayname = "ВОЗДУШНЫЙ ШАР", img = "https://gspics.org/images/2020/01/24/5Uthx.png", cooldown = 60, distancespawn = 5f, fuel = 10, infinitefuel = true });
                //  prefabs.Add("chinook", new vehicle { prefab = "assets/prefabs/npc/ch47/ch47.entity.prefab", displayname = "ТРАНСПОРТНЫЙ ВЕРТОЛЕТ", img = "https://gspics.org/images/2020/01/24/5Udte.png" });
                SaveConfig();
            }

            permission.RegisterPermission(Name + ".all", this);
            permission.RegisterPermission(Name + ".nocoldown", this);
            foreach (var z in config.prefabs)
            {
                permission.RegisterPermission(Name + "." + z.Key, this);
                AddImage(z.Value.img, z.Key);
            }

            foreach (var z in config.list)
            {
                AddImage(z.Value, z.Key);
            }
            NextTick(() => Unsubscribe(nameof(OnPlayerInput)));

            if (config.onlyfriends) Subscribe("CanMountEntity");
        }

        object CanMountEntity(BasePlayer player, BaseMountable baseMountable)
        {
            BaseVehicle baseVehicle = baseMountable.VehicleParent();
            if (baseVehicle == null || baseVehicle.OwnerID == 0) return null;
            if (baseVehicle is MiniCopter || baseVehicle is BasicCar || baseVehicle is BaseBoat)
            {
                if (baseVehicle.OwnerID == player.userID || player.Team != null && player.Team.members.Any(x => x == baseVehicle.OwnerID)) return null;
                player.ChatMessage("<color=yellow>ЭТО ТРАНСПОРТНОЕ СРЕДСТВО ПРИНАДЛЕЖИТ ДРУГОМУ ИГРОКУ!</color>");
                return false;
            }
            return null;
        }

        void gettimage(string name)
        {
            string img = GetImage(name) ?? "39274839";
            if (!img.Equals("39274839"))
            {
                if (!images.ContainsKey(name)) images.Add(name, img);
                return;
            }

            timer.Once(1f, () => gettimage(name));
        }

        string CanSpawn(BasePlayer player, string name)
        {
            if (!config.prefabs.ContainsKey(name)) return config.messages[0];
            if (player.IsBuildingBlocked()) return config.messages[1];
            if (!permission.UserHasPermission(player.UserIDString, Name + ".all") && !permission.UserHasPermission(player.UserIDString, Name + "." + name)) return config.messages[2];
            if (vehicles[player.userID].vehicles.ContainsKey(name)) return config.messages[3];
            if (vehicles[player.userID].cooldowns.ContainsKey(name))
            {
                DateTime col = vehicles[player.userID].cooldowns[name];
                if (col > DateTime.Now) return config.messages[4].Replace("{time}", FormatTime(col - DateTime.Now));
            }
            if ((name.Equals("rhib") || name.Equals("rowboat")) && player.modelState != null && player.modelState.waterLevel <= 0f) return config.messages[18];
            return null;
        }

        void remove(BaseEntity ent)
        {
            ulong owner = ent.OwnerID;
            if (owner.Equals(0)) return;
            string prefab = ent.ShortPrefabName;
            if (!vehicles.ContainsKey(owner) || !vehicles[owner].vehicles.ContainsKey(prefab) || !ent.Equals(vehicles[owner].vehicles[prefab])) return;
            MyVehicles veh = vehicles[owner];
            veh.vehicles.Remove(prefab);
            ent?.Kill();
            if (!permission.UserHasPermission(owner.ToString(), Name + ".nocoldown"))
            {
                int cooldown = config.prefabs[prefab].cooldown;
                if (!veh.cooldowns.ContainsKey(prefab)) vehicles[owner].cooldowns.Add(prefab, DateTime.Now.AddMinutes(cooldown));
                else veh.cooldowns[prefab] = DateTime.Now.AddMinutes(cooldown);
            }
        }

        void spawn(BasePlayer player, string prefab)
        {
            check(player);
            string can = CanSpawn(player, prefab);
            if (can is string)
            {
                UiMSG(player, can);
                return;
            }
            vehicle che = config.prefabs[prefab];
            Vector3 vector = player.transform.position + (player.eyes.MovementForward() * che.distancespawn) + Vector3.up * 2f;
            RaycastHit hit;
            if (!Physics.Raycast(vector, Vector3.down, out hit, 1000f, LayerMask.GetMask("Terrain", "World", "Construction"), QueryTriggerInteraction.Ignore))
            {
                UiMSG(player, "ВЫБЕРИТЕ БОЛЕЕ ПОДХОДЯЩЕЕ МЕСТО ДЛЯ СПАВНА!");
                return;
            }
            Vector3 spawnpos = hit.point;
            float water = TerrainMeta.WaterMap.GetHeight(vector);
            if (prefab.Equals("rhib") || prefab.Equals("rowboat"))
            {
                spawnpos.y = TerrainMeta.WaterMap.GetHeight(vector) + 2f;
            }
            else
            {
                if (water > hit.point.y)
                {
                    UiMSG(player, "ЭТОТ ВИД ТРАНСПОРТА НЕ ПЛАВАЕТ ПО ВОДЕ!");
                    return;
                }
            }
            BaseEntity entity = GameManager.server.CreateEntity(che.prefab, spawnpos);
            if (entity == null)
            {
                Debug.LogError($"[ZetaVehicle] Префаб для {che.displayname} не существует!");
                return;
            }
            entity.OwnerID = player.userID;
            entity.Spawn();
            if (entity is MiniCopter)
            {
                if (che.infinitefuel) entity.GetComponent<MiniCopter>().fuelPerSec = 0;
                StorageContainer str = entity.GetComponent<MiniCopter>().GetFuelSystem().fuelStorageInstance.Get(entity.isServer) as StorageContainer;
                givefuel(str.inventory, che.fuel, che.infinitefuel);
            }
            else if (entity is HotAirBalloon)
            {
                HotAirBalloon hotAirBalloon = entity as HotAirBalloon;
                if (che.infinitefuel) hotAirBalloon.fuelPerSec = 0;
                Debug.Log(entity.name);
                foreach (var z in hotAirBalloon.children)
                {
                    Debug.Log(z.PrefabName + " " + z.prefabID);
                    if (z.prefabID == 1394312733)
                    {
                        givefuel((z as StorageContainer).inventory, che.fuel, che.infinitefuel);
                        break;
                    }
                }
            }
            else if (entity is MotorRowboat)
            {
                MotorRowboat motorRowboat = entity as MotorRowboat;
                if (che.infinitefuel) motorRowboat.fuelPerSec = 0;
                foreach (var z in motorRowboat.children)
                {
                    if(z.prefabID == 198420611 || z.prefabID == 1394312733)
                    {
                        givefuel((z as StorageContainer).inventory, che.fuel, che.infinitefuel);
                        break;
                    }
                }
            }
            vehicles[player.userID].vehicles.Add(prefab, entity);
            DestroyUI(player);
            UiMSG(player, config.messages[5].Replace("{name}", che.displayname));
        }

        private void givefuel(ItemContainer container, int fuel, bool locked)
        {
            Item item = ItemManager.CreateByItemID(-946369541, fuel);
            if (locked && container.HasFlag(ItemContainer.Flag.IsLocked) != true) container.SetFlag(ItemContainer.Flag.IsLocked, true);
            item.MoveToContainer(container);
        }

        void destroytimer(Timer ss)
        {
            if (!ss.Destroyed) timer.Destroy(ref ss);
        }

        void destroyUIMSG(BasePlayer player)
        {
            if (Uitimer.ContainsKey(player))
            {
                destroytimer(Uitimer[player]);
                Uitimer.Remove(player);
            }
            CuiHelper.DestroyUi(player, drawUI);
        }
        Dictionary<BasePlayer, Timer> Uitimer = new Dictionary<BasePlayer, Timer>();
        void UiMSG(BasePlayer player, string msg)
        {
            destroyUIMSG(player);
            CuiElementContainer container = LMUI.CreateElementContainer(drawUI, "0.5 0.5", "0.5 0.5");
            LMUI.OutlineText(ref container, drawUI, "1 1 1 0.9", msg, 30);
            CuiHelper.AddUi(player, container);
            Uitimer.Add(player, timer.Once(2f, () =>
            {
                CuiHelper.DestroyUi(player, drawUI);
                if (Uitimer.ContainsKey(player)) Uitimer.Remove(player);
            }));
        }

        void Unload()
        {
            foreach (var z in BasePlayer.activePlayerList)
            {
                DestroyUI(z);
                CuiHelper.DestroyUi(z, drawUI);
            }
            foreach (var z in vehicles)
            {
                foreach (var x in z.Value.vehicles) x.Value?.KillMessage();
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity is BaseMountable || entity is HotAirBalloon) remove((BaseEntity)entity);
        }

        void DestroyUI(BasePlayer player)
        {
            if (active.Contains(player)) active.Remove(player);
            CuiHelper.DestroyUi(player, mainUI);
            CuiHelper.DestroyUi(player, nomainUI);
            if (active.Count.Equals(0))
            {
                Unsubscribe(nameof(OnPlayerInput));
                onJump = false;
            }
        }
        #endregion

        #region FormatTime
        private string FormatTime(TimeSpan time)
=> (time.Days == 0 ? string.Empty : FormatDays(time.Days)) + (time.Hours == 0 ? string.Empty : FormatHours(time.Hours)) + (time.Minutes == 0 ? string.Empty : FormatMinutes(time.Minutes)) + ((time.Seconds == 0 || time.Days != 0 || time.Hours != 0) ? string.Empty : FormatSeconds(time.Seconds));

        private string FormatDays(int days) => FormatUnits(days, "ДНЕЙ", "ДНЯ", "ДЕНЬ");

        private string FormatHours(int hours) => FormatUnits(hours, "ЧАСОВ", "ЧАСА", "ЧАС");

        private string FormatMinutes(int minutes) => FormatUnits(minutes, "МИНУТ", "МИНУТЫ", "МИНУТУ");

        private string FormatSeconds(int seconds) => FormatUnits(seconds, "СЕКУНД", "СЕКУНДЫ", "СЕКУНД");

        private string FormatUnits(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1} ";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2} ";

            return $"{units} {form3} ";
        }
        #endregion

        #region Commands
        [ChatCommand("vm")]
        void cmdvm(BasePlayer player, string cmd, string[] args)
        {
            if (!active.Contains(player)) opengui(player, vehicles.ContainsKey(player.userID) ? vehicles[player.userID].lastpage : 0);
            else DestroyUI(player);
        }

        [ConsoleCommand("zeta.open")]
        private void cmdzetaspawn(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            check(player);
            EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, Vector3.up, Vector3.zero) { scale = 1f }, player.net.connection);
            int page = 0;
            if (arg.HasArgs() && !int.TryParse(arg.Args[0], out page) && !arg.Args[0].Equals("True"))
            {
                if (arg.Args[0].Equals("close")) DestroyUI(player);
                else if (arg.Args[0].Equals("remove") && arg.Args.Length > 1)
                {
                    if (!vehicles.ContainsKey(player.userID) || !vehicles[player.userID].vehicles.ContainsKey(arg.Args[1]))
                    {
                        UiMSG(player, config.messages[6]);
                        return;
                    }
                    BaseEntity ent = vehicles[player.userID].vehicles[arg.Args[1]];
                    BaseMountable mount = ent.GetComponent<BaseMountable>();
                    if (mount != null && mount.IsMounted())
                    {
                        UiMSG(player, config.messages[7]);
                        return;
                    }
                    remove(ent);
                    DestroyUI(player);
                    UiMSG(player, config.messages[8]);
                }
                else spawn(player, arg.Args[0]);
            }
            else
            {
                if (arg.HasArgs() && !arg.Args[0].Equals("True") || !active.Contains(player)) opengui(player, (!arg.HasArgs() || arg.Args[0].Equals("True")) ? vehicles[player.userID].lastpage : page);
                else DestroyUI(player);
            }
        }

        void check(BasePlayer player)
        {
            if (!vehicles.ContainsKey(player.userID)) vehicles.Add(player.userID, new MyVehicles());
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.JUMP) && active.Contains(player))
            {
                DestroyUI(player);
            }
        }

        void opengui(BasePlayer player, int page = 0)
        {
            check(player);
            vehicles[player.userID].lastpage = page;
            destroyUIMSG(player);
            Dictionary<string, vehicle> allowed = new Dictionary<string, vehicle>();
            if (permission.UserHasPermission(player.UserIDString, Name + ".all"))
            {
                allowed = config.prefabs;
            }
            else
            {
                foreach (var z in config.prefabs)
                {
                    if (!permission.UserHasPermission(player.UserIDString, Name + "." + z.Key)) continue;
                    allowed.Add(z.Key, z.Value);
                }
            }
            int count = allowed.Count;
            int max = 5;

            CuiHelper.DestroyUi(player, nomainUI);
            CuiElementContainer nocontainer = LMUI.CreateElementContainerNO(nomainUI, "0 0 0 0", "0 0", "1 1", false);
            if (count > 0)
            {
                float x = 0.06f;
                int le = count - max * page;
                if (le == 1) x = 0.42f;
                else if (le == 2) x = 0.33f;
                else if (le == 3) x = 0.24f;
                else if (le == 4) x = 0.15f;
                if (count > max && count > max * (page + 1)) LMUI.CreateButton(ref nocontainer, nomainUI, "0 0 0 0", config.messages[9], 20, "0.4 0.16", "0.6 0.21", $"zeta.open {page + 1}");
                if (page > 0) LMUI.CreateButton(ref nocontainer, nomainUI, "0 0 0 0", config.messages[10], 20, "0.4 0.16", "0.6 0.21", $"zeta.open {page - 1}");

                foreach (var z in allowed.Skip(max * page).Take(max * (page + 1)))
                {

                    LMUI.CreatePanel(ref nocontainer, nomainUI, "0 0 0 1", $"{x} 0.3", $"{x + 0.16017f} 0.681");
                    LMUI.LoadImage(ref nocontainer, nomainUI, images["fon5"], $"{x} 0.3", $"{x + 0.16f} 0.68", "1 1 1 0.5");
                    LMUI.LoadImage(ref nocontainer, nomainUI, images[z.Key], $"{x + 0.02f} 0.35", $"{x + 0.14f} 0.6");
                    LMUI.CreateLabel(ref nocontainer, nomainUI, "1 1 1 0.5", z.Value.displayname, 24, $"{x} 0.59", $"{x + 0.16f} 0.68", TextAnchor.MiddleCenter);
                    if (vehicles[player.userID].cooldowns.ContainsKey(z.Key) && vehicles[player.userID].cooldowns[z.Key] > DateTime.Now)
                    {
                        LMUI.CreatePanel(ref nocontainer, nomainUI, "0 0 0 0.8", $"{x} 0.3", $"{x + 0.1602f} 0.681");
                        LMUI.CreateLabel(ref nocontainer, nomainUI, "1 1 1 0.5", FormatTime(vehicles[player.userID].cooldowns[z.Key] - DateTime.Now), 24, $"{x} 0.34", $"{x + 0.16f} 0.68", TextAnchor.MiddleCenter);
                        LMUI.CreateButton(ref nocontainer, nomainUI, "0 0 0 0.98", config.messages[11], 20, $"{x} 0.3", $"{x + 0.1606f} 0.34", "");
                        //vehicles[player.userID].cooldowns[z.Key]
                    }
                    else if (vehicles.ContainsKey(player.userID) && vehicles[player.userID].vehicles.ContainsKey(z.Key)) LMUI.CreateButton(ref nocontainer, nomainUI, "0 0 0 0.98", config.messages[13], 20, $"{x} 0.3", $"{x + 0.1606f} 0.34", "zeta.open remove " + z.Key);
                    else LMUI.CreateButton(ref nocontainer, nomainUI, "0 0 0 0.98", config.messages[12], 20, $"{x} 0.3", $"{x + 0.1606f} 0.34", "zeta.open " + z.Key);
                    x += 0.18f;
                }
            }
            LMUI.CreateButton(ref nocontainer, nomainUI, "0 0 0 0", "", 20, "0.4 0.1", "0.6 0.15", "zeta.open close");
            mainui(player, count);
            CuiHelper.AddUi(player, nocontainer);
        }
        void mainui(BasePlayer player, int count)
        {
            if (!active.Contains(player))
            {
                active.Add(player);
                if (!onJump)
                {
                    onJump = true;
                    Subscribe(nameof(OnPlayerInput));
                }
                CuiHelper.DestroyUi(player, mainUI);
                CuiElementContainer container = LMUI.CreateElementContainerNO(mainUI, "0 0 0 0", "0 0", "1 1", true);
                LMUI.LoadImage(ref container, mainUI, images["background2"], "0 0", "1 1");
                LMUI.CreateButton(ref container, mainUI, "0 0 0 0.98", config.messages[14], 20, "0.4 0.1", "0.6 0.15", "zeta.open close");
                LMUI.CreateLabel(ref container, mainUI, "1 1 1 0.8", config.messages[15], 10, "0.3 0.96", "0.7 1", TextAnchor.MiddleCenter);


                if (count > 0)
                {
                    LMUI.CreatePanel(ref container, mainUI, "0 0 0 0.95", "0.2 0.78", "0.8 0.86");
                    LMUI.CreateLabel(ref container, mainUI, "1 1 1 0.8", config.messages[16], 36, "0.2 0.75", "0.8 0.89", TextAnchor.MiddleCenter);
                    if (count > 5) LMUI.CreatePanel(ref container, mainUI, "0 0 0 0.98", "0.4 0.16", "0.6 0.21");
                }
                else
                {
                    LMUI.CreatePanel(ref container, mainUI, "0 0 0 0.95", "0.15 0.5", "0.85 0.7");
                    LMUI.CreateLabel(ref container, mainUI, "1 1 1 0.8", config.messages[17], 36, "0.15 0.5", "0.85 0.7", TextAnchor.MiddleCenter);
                }
                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region UI
        class LMUI
        {
            static public CuiElementContainer CreateElementContainerNO(string panelName, string color, string aMin, string aMax, bool cursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color, Sprite =  "assets/content/ui/ui.background.transparent.radial.psd"},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor,
                        FadeOut = 0f
                    },
                    new CuiElement().Parent = parent,
                    panelName
                }
            };
                return NewElement;
            }
            static public CuiElementContainer CreateElementContainer(string panelName, string anch1 = "0.5 0", string anch2 = "0.5 0")
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            RectTransform = {AnchorMin = anch1, AnchorMax = anch2},
                            CursorEnabled = false,
                            FadeOut = 0f
                        },
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, string oMin = "0 0", string oMax = "0 0", bool cursor = false, string material = "assets/content/ui/uibackgroundblur-ingamemenu.mat")
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    CursorEnabled = cursor
                },
                panel, CuiHelper.GetGuid());
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, string oMin = "0 0", string oMax = "0 0", string font = "RobotoCondensed-Bold.ttf", float fadeIn = 0f, float fadeout = 0f)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadeIn, Text = text, Font = font },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    FadeOut = fadeout
                },
                panel, CuiHelper.GetGuid());

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, string oMin = "0 0", string oMax = "0 0", float fadeIn = 0f, float fade = 0f, string material = "assets/content/ui/uibackgroundblur-ingamemenu.mat")
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadeIn },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    Text = { Text = text, FontSize = size, Align = align },
                    FadeOut = fade
                },
                panel, CuiHelper.GetGuid());
            }
            static public void OutlineText(ref CuiElementContainer container, string panel, string color, string text, int size)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiTextComponent {Color = color, FontSize = size, Align = TextAnchor.MiddleCenter, FadeIn = 0.5f, Text = text },
                        new CuiOutlineComponent { Distance = "0.6 0.6", Color = "0 0 0 0.8" },
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450 -60", OffsetMax = "450 60" },
                    }
                });
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax, string color = "1 1 1 1")
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Color = color },
                        new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
        }
        #endregion
    }
}