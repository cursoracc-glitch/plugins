using System.Collections.Generic;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("AntiRB", "King", "1.0.0")]
    public class AntiRB : RustPlugin
    {
                        [PluginReference] private Plugin
        NoEscape = null, Clans = null, MenuAlerts = null, ImageLibrary = null;

        private static AntiRB plugin = null;
        public Dictionary<string, double> _cooldown = new Dictionary<string, double>();

        #region [Data]
        private class Data
        {
            public readonly BasePlayer _player;
            public readonly Single _cooldown;
            public readonly Boolean _coin;

            public Data(BasePlayer player, Single cooldown, Boolean isRaidCoin = false)
            {
                _player = player;
                _cooldown = Time.time + cooldown;
                _coin = isRaidCoin;
            }
        }

        private List<Data> _data = new List<Data>();
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            plugin = this;

            ImageLibrary?.Call("AddImage", "https://i.imgur.com/i7QkC5B.png", "Point_Image");
            ImageLibrary?.Call("AddImage", "https://i.postimg.cc/7696CGRZ/dollar-2.png", "AntiRB_Image");
            ImageLibrary?.Call("AddImage", "https://i.postimg.cc/nVKJCPNH/imgonline-com-ua-Resize-YN5d-Vq-C7x1.png", "AntiR_B_Image");
            ImageLibrary?.Call("AddImage", "https://i.postimg.cc/dQr2b8pk/bet.png", "XUron_Image");       

            if (!NoEscape) PrintWarning("NOESCAPE IS NOT INSTALLED.");

            GameObject obj = new GameObject();
            if (config._SettingsAntiRBChinook.useChinook)
                AntiRBComp = obj.AddComponent<AntiRBComponent>();
            if (config._SettingsChinookPoint.useChinook)
                ChinookPointComp = obj.AddComponent<ChinookPointComponent>();

            timer.Every(1, TimeHandle);
        }

        private void Unload()
        {
            _data?.ForEach(data =>
            {
                if (data._coin)
                    rust.RunServerCommand($"oxide.usergroup remove {data._player.userID} antirb");
            });

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                MenuAlerts?.Call("RemoveAlertMenu", player, $"{Name}.Coin");
                MenuAlerts?.Call("RemoveAlertMenu", player, $"{Name}.Point");
            }

            UnityEngine.Object.Destroy(AntiRBComp);
            UnityEngine.Object.Destroy(ChinookPointComp);

            plugin = null;
        }
        #endregion

        private void TimeHandle()
        {
            List<Data> toRemove = Pool.GetList<Data>();

            _data?.ForEach(data =>
            {
                if (Time.time - data._cooldown >= 0)
                {
                    if (data._coin)
                        rust.RunServerCommand($"oxide.usergroup remove {data._player.userID} antirb");

                    toRemove.Add(data);
                }
            });

            toRemove.ForEach(data => _data.Remove(data));
            
            Pool.FreeList(ref toRemove);
        }

        #region [Rust]
        object OnItemAction(Item item, String action, BasePlayer player)
        {
            if (action != "unwrap") return null;

            if (item.info.shortname == config._SettingsAntiRB.ShortName && item.skin == config._SettingsAntiRB.SkinID)
            {
                String clan = ClanTag(player);
                if (_cooldown.ContainsKey(clan))
                {
                    Double time = _cooldown[clan] - CurrentTime();
                    if (time <= 0)
                        _cooldown.Remove(clan);
                    else
                    {
                        player.ChatMessage($"Вы уже использовали этот предмет,повторно вы сможете использовать его через {TimeSpan.FromSeconds(time).ToShortString()}!");
                        return false;
                    }
                }

                Server.Broadcast($"Игрок <color=green>{player.displayName}</color> активировал анти-рб монету!");
                rust.RunServerCommand($"oxide.usergroup add {player.userID} antirb");

                UnblockedPlayer(player);
                _data.Add(new Data(player, config._SettingsAntiRB.timeActiveAntiRB, true));
                if (!_cooldown.ContainsKey(clan))
                    _cooldown.Add(clan, CurrentTime() + config._cooldown);
                if (item.amount > 1) item.amount--;
                else item.RemoveFromContainer();
                return false;
            }

            if (item.info.shortname == config._SettingsDamageCoin.ShortName && item.skin == config._SettingsDamageCoin.SkinID)
            {
                Server.Broadcast($"Игрок <color=green>{player.displayName}</color> активировал монету дополнительного урона!");

                _data.Add(new Data(player, config._SettingsDamageCoin.timeActiveDamageCoin, false));
                if (item.amount > 1) item.amount--;
                else item.RemoveFromContainer();
                return false;
            }

            if (item.info.shortname == config._SettingsChinookPoint.ShortName && item.skin == config._SettingsChinookPoint.SkinID)
            {
                String clan = ClanTag(player);
                if (string.IsNullOrEmpty(clan))
                {
                    player.ChatMessage($"Вы не можете активировать тикет на {config._SettingsChinookPoint.howPoint} очков. Чтобы активировать создайте клан!");
                    return false;
                }

                Clans?.Call("GiveClanPoints", clan, config._SettingsChinookPoint.howPoint);
                player.ChatMessage($"Вы успешно активировали тикет на {config._SettingsChinookPoint.howPoint} очков.");
                if (item.amount > 1) item.amount--;
                else item.RemoveFromContainer();
                return false;
            }

            return null;
        }

		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info?.InitiatorPlayer == null) return;

            BasePlayer player = info.InitiatorPlayer;
            if (player == null) return;
			
			Data find = _data.Find(x => x._player == player);
            if (find == null) return;

            if (entity is BuildingBlock || entity is Door || entity is SimpleBuildingBlock)
                info.damageTypes.ScaleAll(1.0f * config._SettingsDamageCoin.DamageCoinPerc);
		}

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.userID.IsSteamId()) return;

            if (AntiRBComp != null && AntiRBComp.IsStartEvent && AntiRBComp.IsStartHacked)
            {
                MenuAlerts?.Call("SendAlertMenu", player, Facepunch.Math.Epoch.Current - AntiRBComp.CurrentTime, (Int32)config._SettingsAntiRBChinook.timeOpen, $"CHINOOK COIN", $"Квадрат: {GetGrid(AntiRBComp.transform.position)}", false, "AntiRB_Image", $"{Name}.Coin");
            }

            if (ChinookPointComp != null && ChinookPointComp.IsStartEvent && ChinookPointComp.IsStartHacked)
            {
                MenuAlerts?.Call("SendAlertMenu", player, Facepunch.Math.Epoch.Current - ChinookPointComp.CurrentTime, (Int32)config._SettingsAntiRBChinook.timeOpen, $"CHINOOK POINT", $"Квадрат: {GetGrid(ChinookPointComp.transform.position)}", false, "Point_Image", $"{Name}.Point");
            }
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null || crate.OwnerID == 0) return;

            if (AntiRBComp != null && AntiRBComp.IsStartEvent && AntiRBComp.CrateEntity == crate)
            {
                AntiRBComp.IsStartHacked = true;
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    MenuAlerts?.Call("SendAlertMenu", player, Facepunch.Math.Epoch.Current, (Int32)config._SettingsAntiRBChinook.timeOpen, $"CHINOOK COIN", $"Квадрат: {GetGrid(AntiRBComp.transform.position)}", false, "AntiRB_Image", $"{Name}.Coin");
                }
                Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Начался взлом чинука с монетами");
            }

            if (ChinookPointComp != null && ChinookPointComp.IsStartEvent && ChinookPointComp.CrateEntity == crate)
            {
                ChinookPointComp.IsStartHacked = true;
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    MenuAlerts?.Call("SendAlertMenu", player, Facepunch.Math.Epoch.Current, (Int32)config._SettingsAntiRBChinook.timeOpen, $"CHINOOK POINT", $"Квадрат: {GetGrid(ChinookPointComp.transform.position)}", false, "Point_Image", $"{Name}.Point");
                }
                Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Начался взлом чинука с очками");
            }
        }

        private void OnEntityKill(HackableLockedCrate crate)
        {
            if (crate == null || crate.OwnerID == 0) return;

            if (AntiRBComp != null && AntiRBComp.IsStartEvent && AntiRBComp.CrateEntity == crate)
            {
                AntiRBComp.EndedEvent();
            }

            if (ChinookPointComp != null && ChinookPointComp.IsStartEvent && ChinookPointComp.CrateEntity == crate)
            {
                ChinookPointComp.EndedEvent();
            }
        }
        #endregion

        #region [AntiRB || Damage Chinook]
        private AntiRBComponent AntiRBComp = null;

        private class AntiRBComponent : FacepunchBehaviour
        {
            private Int32 TotalTime = 0;
            public Int32 CurrentTime = 0;

            public Boolean IsStartHacked = false;
            public Boolean IsStartEvent = false;
            public HackableLockedCrate CrateEntity = null;

            private void Awake()
            {
                gameObject.layer = (Int32)Rust.Layer.Reserved1;
                enabled = false;
                InvokeRepeating(UpdateTime, 1f, 1);
            }

            public void DestroyComp() => OnDestroy();
            private void OnDestroy()
            {
                RemoveChinook();
                Destroy(this);
            }

            private void UpdateTime()
            {
                if (!IsStartEvent)
                {
                    TotalTime++;
                    if (TotalTime >= plugin.config._SettingsAntiRBChinook.eventCooldown)
                    {
                        StartEvent();
                    }
                }
                else if (IsStartHacked)
                {
                    CurrentTime++;
                    if (CurrentTime >= plugin.config._SettingsAntiRBChinook.timeOpen + plugin.config._SettingsAntiRBChinook.eventDestoroyTime)
                    {
                        EndedEvent();
                    }
                }
            }

            public void StartEvent()
            {
                transform.position = plugin.config._SettingsAntiRBChinook.chinookPosition;

                if (transform.position == Vector3.zero) return;

                SpawnChinook();

                TotalTime = 0;
                IsStartEvent = true;
                plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Начался ивент <color=#ffde5a>CHINOOK COIN</color>.\nМестоположение отмечено на карте.");
            }

            public void EndedEvent()
            {
                IsStartHacked = false;
                IsStartEvent = false;
                TotalTime = 0;
                CurrentTime = 0;

                RemoveChinook();

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    plugin.MenuAlerts?.Call("RemoveAlertMenu", player, $"{plugin.Name}");
                }
                plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Закончился ивент <color=#ffde5a>CHINOOK COIN</color>.\nСледующий ивент будет через {TimeExtensions.FormatShortTime(TimeSpan.FromSeconds(plugin.config._SettingsAntiRBChinook.eventCooldown))}");
            }

            private void SpawnChinook()
            {
                CrateEntity = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", transform.position, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), true) as HackableLockedCrate;
                CrateEntity.enableSaving = false;
                CrateEntity.OwnerID = 231313354353;
                CrateEntity.Spawn();
                CrateEntity.inventory.itemList.Clear();
                plugin.FillingChinookItem(CrateEntity);
                CrateEntity.inventory.capacity = CrateEntity.inventory.itemList.Count;
                CrateEntity.inventory.MarkDirty();
                CrateEntity.SendNetworkUpdate();
                CrateEntity.hackSeconds = HackableLockedCrate.requiredHackSeconds - plugin.config._SettingsAntiRBChinook.timeOpen;
            }

            private void RemoveChinook()
            {
                if (CrateEntity != null && !CrateEntity.IsDestroyed)
                    CrateEntity.Kill();
            }
        }
        #endregion

        #region [ChinookPoint]
        private ChinookPointComponent ChinookPointComp = null;

        private class ChinookPointComponent : FacepunchBehaviour
        {
            private Int32 TotalTime = 0;
            public Int32 CurrentTime = 0;

            public Boolean IsStartHacked = false;
            public Boolean IsStartEvent = false;
            public HackableLockedCrate CrateEntity = null;

            private void Awake()
            {
                gameObject.layer = (Int32)Rust.Layer.Reserved1;
                enabled = false;
                InvokeRepeating(UpdateTime, 1f, 1);
            }

            public void DestroyComp() => OnDestroy();
            private void OnDestroy()
            {
                RemoveChinook();
                Destroy(this);
            }

            private void UpdateTime()
            {
                if (!IsStartEvent)
                {
                    TotalTime++;
                    if (TotalTime >= plugin.config._SettingsChinookPoint.eventCooldown)
                    {
                        StartEvent();
                    }
                }
                else if (IsStartHacked)
                {
                    CurrentTime++;
                    if (CurrentTime >= plugin.config._SettingsChinookPoint.timeOpen + plugin.config._SettingsChinookPoint.eventDestoroyTime)
                    {
                        EndedEvent();
                    }
                }
            }

            public void StartEvent()
            {
                transform.position = plugin.config._SettingsChinookPoint.chinookPosition;

                if (transform.position == Vector3.zero) return;

                SpawnChinook();

                TotalTime = 0;
                IsStartEvent = true;
                plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Начался ивент <color=#ffde5a>CHINOOK POINT</color>.\nМестоположение отмечено на карте.");
            }

            public void EndedEvent()
            {
                IsStartHacked = false;
                IsStartEvent = false;
                TotalTime = 0;
                CurrentTime = 0;

                RemoveChinook();

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    plugin.MenuAlerts?.Call("RemoveAlertMenu", player, $"{plugin.Name}");
                }
                plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Закончился ивент <color=#ffde5a>CHINOOK POINT</color>.\nСледующий ивент будет через {TimeExtensions.FormatShortTime(TimeSpan.FromSeconds(plugin.config._SettingsChinookPoint.eventCooldown))}");
            }

            private void SpawnChinook()
            {
                CrateEntity = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", transform.position, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), true) as HackableLockedCrate;
                CrateEntity.enableSaving = false;
                CrateEntity.OwnerID = 231313354353;
                CrateEntity.Spawn();
                CrateEntity.inventory.itemList.Clear();
                Item netItem = ItemManager.CreateByName(plugin.config._SettingsChinookPoint.ShortName, 1, plugin.config._SettingsChinookPoint.SkinID);
                netItem.name = $"Билет на {plugin.config._SettingsChinookPoint.howPoint} очков.";
                netItem.MoveToContainer(CrateEntity.inventory);
                CrateEntity.inventory.capacity = CrateEntity.inventory.itemList.Count;
                CrateEntity.inventory.MarkDirty();
                CrateEntity.SendNetworkUpdate();
                CrateEntity.hackSeconds = HackableLockedCrate.requiredHackSeconds - plugin.config._SettingsChinookPoint.timeOpen;
            }

            private void RemoveChinook()
            {
                if (CrateEntity != null && !CrateEntity.IsDestroyed)
                    CrateEntity.Kill();
            }
        }
        #endregion

        #region [Functional]
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static Double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        public String ClanTag(BasePlayer player) => Clans?.Call<String>("GetClanTag", player.userID);
        private void UnblockedPlayer(BasePlayer player) => NoEscape?.Call("UnblockedPlayer", player);
        #endregion

        #region [FillingChinook]
        private void FillingChinookItem(LootContainer LootContainer)
        {
            if (LootContainer == null) return;
            Int32 i = Core.Random.Range(0, 2);

            switch (i)
            {
                case 0:
                {
                    Item netItem = ItemManager.CreateByName(config._SettingsAntiRB.ShortName, 1, config._SettingsAntiRB.SkinID);
                    netItem.name = "Анти рб монета";
                    netItem.MoveToContainer(LootContainer.inventory);
                    break;
                }
                case 1:
                {
                    Item netItem = ItemManager.CreateByName(config._SettingsDamageCoin.ShortName, 1, config._SettingsDamageCoin.SkinID);
                    netItem.name = "Двойной урон монета";
                    netItem.MoveToContainer(LootContainer.inventory);
                    break;
                }
            }
        }
        #endregion

        #region [ChatCommand]
        [ChatCommand("chinook")]
        private void NewChinookPosition(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            if (args.Length == 0)
            {
                    player.ChatMessage("Доступные команды:"
                                       + "\n/chinook antirb start - Начать антирб чинук" 
                                       + "\n/chinook antirb stop - Закончить антирб чинук"
                                       + "\n/chinook antirb position - Установить местоположение чинука"
                                       + "\n/chinook point start - Начать ивент чинук с очками" 
                                       + "\n/chinook point stop - Закончить ивент чинук с очками"
                                       + "\n/chinook point position - Установить местоположение чинука с очками");
                return;
            }

            if (args[0] == "antirb")
            {
                if (args[1] == "start")
                {
                    if (AntiRBComp == null || AntiRBComp.IsStartEvent) return;

                    AntiRBComp.StartEvent();
                    player.ChatMessage("Вы успешно начали ивент анти рб.");
                }
                else if (args[1] == "stop")
                {
                    if (AntiRBComp == null || !AntiRBComp.IsStartEvent) return;

                    AntiRBComp.EndedEvent();
                    player.ChatMessage("Вы успешно остановили ивент анти рб.");
                }
                else if (args[1] == "position")
                {
                    config._SettingsAntiRBChinook.chinookPosition = player.GetNetworkPosition();
                    SaveConfig();
                    player.ChatMessage("Вы успешно установили новое положения для чинука!");
                }
            }
            else if (args[0] == "point")
            {
                if (args[1] == "start")
                {
                    if (ChinookPointComp == null || ChinookPointComp.IsStartEvent) return;

                    ChinookPointComp.StartEvent();
                    player.ChatMessage("Вы успешно начали ивент чинук с очками.");
                }
                else if (args[1] == "stop")
                {
                    if (ChinookPointComp == null || !ChinookPointComp.IsStartEvent) return;

                    ChinookPointComp.EndedEvent();
                    player.ChatMessage("Вы успешно остановили ивент чинук с очками.");
                }
                else if (args[1] == "position")
                {
                    config._SettingsChinookPoint.chinookPosition = player.GetNetworkPosition();
                    SaveConfig();
                    player.ChatMessage("Вы успешно установили новое положения для чинука с очками!");
                }
            }
        }
        #endregion

        #region [Positon]
        public string GetGrid(Vector3 pos)
        {
            char letter = 'A';
            Single x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            Single z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f)) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((Int32)letter) + x);
            return $"{letter}{z}";
        }
        #endregion

        #region [FormatTime]
        private static class TimeExtensions
        {
            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0) result += $"{time.Days}д ";
                if (time.Hours != 0) result += $"{time.Hours}ч ";
                if (time.Minutes != 0) result += $"{time.Minutes}м ";
                if (time.Seconds != 0) result += $"{time.Seconds}с ";
                return result;
            }
            private static string Format(Int32 units, string form1, string form2, string form3)
            {
                Int32 tmp = units % 10;
                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
                if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
                return $"{units} {form3}";
            }
        }
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class SettingsAntiRBChinook
        {
            [JsonProperty("Использовать этот вариант чинука ?")]
            public Boolean useChinook;

            [JsonProperty("Время открытия чинука (в секундах)")]
            public Int32 timeOpen;

            [JsonProperty("Раз во сколько будет запускаться ивент (в секундах)")]
            public Int32 eventCooldown;

            [JsonProperty("Через сколько заканчивать ивент если чинук никто не залутал (в секундах)")]
            public Int32 eventDestoroyTime;

            [JsonProperty("Местоположение чинука ( Не указывать )")]
            public Vector3 chinookPosition;
        }

        public class SettingsChinookPoint
        {
            [JsonProperty("Использовать этот вариант чинука ?")]
            public Boolean useChinook;
            
            [JsonProperty("Время открытия чинука (в секундах)")]
            public Int32 timeOpen;

            [JsonProperty("Раз во сколько будет запускаться ивент (в секундах)")]
            public Int32 eventCooldown;

            [JsonProperty("Через сколько заканчивать ивент если чинук никто не залутал (в секундах)")]
            public Int32 eventDestoroyTime;

            [JsonProperty("Местоположение чинука ( Не указывать )")]
            public Vector3 chinookPosition;

            [JsonProperty("ShortName")]
            public String ShortName;

            [JsonProperty("SkinID")]
            public ulong SkinID;

            [JsonProperty("Сколько очков давать за активацию тикета")]
            public Int32 howPoint;
        }

        public class SettingsAntiRB
        {
            [JsonProperty("ShortName")]
            public String ShortName;

            [JsonProperty("SkinID")]
            public ulong SkinID;

            [JsonProperty("Время действия монеты")]
            public Single timeActiveAntiRB;
        }

        public class SettingsDamageCoin
        {
            [JsonProperty("ShortName")]
            public String ShortName;

            [JsonProperty("SkinID")]
            public ulong SkinID;

            [JsonProperty("Время действия монеты")]
            public Single timeActiveDamageCoin;

            [JsonProperty("На сколько умножать урон по постройкам")]
            public Single DamageCoinPerc;
        }

        private class PluginConfig
        {
            [JsonProperty("Настройки анти-рб монеты")]
            public SettingsAntiRB _SettingsAntiRB = new SettingsAntiRB();

            [JsonProperty("Настройки чинука с очками клана")]
            public SettingsChinookPoint _SettingsChinookPoint = new SettingsChinookPoint();

            [JsonProperty("Настройки чинука с анти рб и монетки на урон")]
            public SettingsAntiRBChinook _SettingsAntiRBChinook = new SettingsAntiRBChinook();

            [JsonProperty("Настройки монеты на увеления урона по постройкам")]
            public SettingsDamageCoin _SettingsDamageCoin = new SettingsDamageCoin();

            [JsonProperty("Перезарядка использования анти-рб монеты")]
            public Int32 _cooldown;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _SettingsAntiRB = new SettingsAntiRB
                    {
                        ShortName = "xmas.present.small",
                        SkinID = 2000,
                        timeActiveAntiRB = 20f,
                    },
                    _SettingsChinookPoint = new SettingsChinookPoint
                    {
                        useChinook = true,
                        timeOpen = 300,
                        eventCooldown = 800,
                        eventDestoroyTime = 300,
                        chinookPosition = Vector3.zero,
                        ShortName = "xmas.present.small",
                        SkinID = 1995,
                        howPoint = 200,
                    },
                    _SettingsAntiRBChinook = new SettingsAntiRBChinook
                    {
                        useChinook = true,
                        timeOpen = 300,
                        eventCooldown = 600,
                        eventDestoroyTime = 300,
                        chinookPosition = Vector3.zero,
                    },
                    _SettingsDamageCoin = new SettingsDamageCoin
                    {
                        ShortName = "xmas.present.small",
                        SkinID = 1997,
                        timeActiveDamageCoin = 20f,
                        DamageCoinPerc = 2f,
                    },
                    _cooldown = 3600,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion
    }
}