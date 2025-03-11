using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("RockEvent", "Drop Dead & Deversive", "1.0.5")]
    public class RockEvent : RustPlugin
    {
        [PluginReference] private Plugin ComponentsEvent;

        bool EventHasStart = false;
        string WorkLayer = "RockEvent.Main";
        DateTime canceldate;
        int count;
        int allcount;
        private List<BaseEntity> SpawnedStones = new List<BaseEntity>();
        private HashSet<Tuple<Vector3, Quaternion>> _spawnData = new HashSet<Tuple<Vector3, Quaternion>>();
        private const int ScanHeight = 100;
        private static int GetBlockMask => LayerMask.GetMask("Construction", "Prevent Building", "Water");
        private static bool MaskIsBlocked(int mask) => GetBlockMask == (GetBlockMask | (1 << mask));
        private Dictionary<MonumentInfo, float> monuments { get; set; } = new Dictionary<MonumentInfo, float>();

        #region config

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public Settings MainSettings = new Settings();
            [JsonProperty("Настройки выигрышей")]
            public AccesSets AccesSettings = new AccesSets();
            [JsonProperty("Дополнительные настройки")]
            public AdditionalSettings AddSettings = new AdditionalSettings();

            public class Settings
            {
                [JsonProperty("Включить автоматичесский старт ивента?")]
                public bool AutoStartEvent = true;
                [JsonProperty("Включить ли минимальное количество игроков для старта ивента?")]
                public bool MinPlayers = true;
                [JsonProperty("Минимальное количество игроков для старта ивента")]
                public int MinPlayersCount = 5;
                [JsonProperty("Время для начала ивента после старта сервера, перезагрузки плагина (первый раз)")]
                public float FirstStartTime = 300f;
                [JsonProperty("Время для начала ивента в последующие разы (второй, третий и тд)")]
                public float RepeatTime = 86400f;
                [JsonProperty("Время до конца ивента (в минутах)")]
                public double EventDuration = 60.0;
            }
            public class AccesSets
            {
                [JsonProperty("Название пермишна для использования команды /rockevent (с приставкой RockEvent)")]
                public string StartPermission = "RockEvent.Use";
            }
            public class AdditionalSettings
            {
                [JsonProperty("Цвет выделения текста в интерфейсе")]
                public string Color = "#8e6874";
            }
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        #endregion

        #region Hooks

        void Unload()
        {
            StopEvent();
            InvokeHandler.Instance.CancelInvoke(StartEvent);
            InvokeHandler.Instance.CancelInvoke(UpdateUI);
            InvokeHandler.Instance.CancelInvoke(SpawnStone);
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, WorkLayer);
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(cfg.AccesSettings.StartPermission, this);
            if (cfg.MainSettings.AutoStartEvent) InvokeHandler.Instance.InvokeRepeating(StartEvent, cfg.MainSettings.FirstStartTime, cfg.MainSettings.RepeatTime);
            foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);

            _spawnData.Clear();
            SpawnedStones.Clear();
            GeneratePositions();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsNpc) return;
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (EventHasStart)
            {
                InitializeUI(player);
            }
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (SpawnedStones.Contains(entity))
            {
                allcount -= 1;
                SpawnedStones.Remove(entity);
            }
        }

        #endregion

        #region API

        [HookMethod("RockEventIsStart")]
        public object RockEventIsStart()
        {
            if (EventHasStart) return "";
            else return null;
        }

        bool ComponentsEventIsStart()
        {
            var result = ComponentsEvent?.Call("ComponentsEventIsStart");
            if (result != null) return true;
            else return false;
        }

        #endregion

        #region Methods

        void StartEvent()
        {
            if (EventHasStart == true) return;

            if (ComponentsEventIsStart() == true)
            {
                Puts("Невозможно начать ивент так как в данный момент запущен ивент \"Фарм компонентов\"");
                EventLog("Невозможно начать ивент так как в данный момент запущен ивент \"Фарм компонентов\"");
                return;
            }

            EventHasStart = true;
            if (cfg.MainSettings.MinPlayers == true)
            {
                if (BasePlayer.activePlayerList.Count < cfg.MainSettings.MinPlayersCount)
                {
                    EventLog("Недостаточно игроков для старта ивента \"Двойные камни\"");
                    Puts("Недостаточно игроков для старта ивента \"Двойные камни\"");
                    return;
                }
            }
            canceldate = DateTime.Now.AddMinutes(cfg.MainSettings.EventDuration);
            foreach (var player in BasePlayer.activePlayerList)
            {
                InitializeUI(player);
                player.ChatMessage("Инвент <color=#8e6874>\"Двойной фарм\"</color> успешно начался, фармите как можно больше!");
                //player.ChatMessage(Messages["StartEvent"]);
            }

            InvokeHandler.Instance.InvokeRepeating(UpdateUI, 1f, 1f);
            EventLog("Ивент \"Двойные камни\" успешно запущен и инициализирован!");
            Puts("Ивент \"Двойные камни\" успешно запущен и инициализирован!");

            InvokeHandler.Instance.InvokeRepeating(SpawnStone, 1f, 900f);
        }

        void StopEvent()
        {
            if (EventHasStart == false) return;
            EventHasStart = false;
            InvokeHandler.Instance.CancelInvoke(UpdateUI);
            InvokeHandler.Instance.CancelInvoke(SpawnStone);
            EventLog("Ивент \"Двойные камни\" остановлен или закончился!");
            Puts("Ивент \"Двойные камни\" остановлен или закончился!");
            foreach (var player in BasePlayer.activePlayerList) 
            {
                CuiHelper.DestroyUi(player, WorkLayer);
                player.ChatMessage("Инвент <color=#8e6874>\"Двойной фарм\"</color> успешно завершился, спасибо всем за участие!");
            }

            Puts($"Удалено: {allcount} камней");
            foreach (var entity in SpawnedStones)
                if (!entity.IsDestroyed) entity.AdminKill();

            _spawnData.Clear();
            SpawnedStones.Clear();
            count = 0;
            allcount = 0;
        }

        void UpdateUI()
        {
            if (EventHasStart == false) return;

            TimeSpan timetocancel = DateTime.Now - canceldate;
            if (timetocancel.ToString("mm\\:ss") == "00:00")
            {
                StopEvent();
                return;
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "time");
                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = WorkLayer,
                    Name = "time",
                    FadeOut = 0.1f,
                    Components =
                    {
                        new CuiTextComponent {  Text = $"[{timetocancel.ToString("mm\\:ss")}]", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-bold.ttf" },
                        new CuiRectTransformComponent {AnchorMin = "0.3214282 0.2399999", AnchorMax = "0.4595238 0.9066674"},
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "0.5 0.5" }
                    }
                });
                CuiHelper.AddUi(player, container);
            }
        }

        private void GeneratePositions()
        {
            _spawnData.Clear();
            var generationSuccess = 0;
            var islandSize = ConVar.Server.worldsize / 2;
            for (var i = 0; i < 500 * 6; i++)
            {
                if (generationSuccess >= 500 * 2)
                {
                    break;
                }
                var x = Core.Random.Range(-islandSize, islandSize);
                var z = Core.Random.Range(-islandSize, islandSize);
                var original = new Vector3(x, ScanHeight, z);

                while (IsMonumentPosition(original) || IsOnRoad(original))
                {
                    x = Core.Random.Range(-islandSize, islandSize);
                    z = Core.Random.Range(-islandSize, islandSize);
                    original = new Vector3(x, ScanHeight, z);
                }

                var data = GetClosestValidPosition(original);
                if (data.Item1 != Vector3.zero)
                {
                    _spawnData.Add(data);
                    generationSuccess++;
                }
            }
        }

        private bool IsMonumentPosition(Vector3 target)
        {
            foreach (var monument in monuments)
            {
                if (InRange(monument.Key.transform.position, target, monument.Value))
                {
                    return true;
                }
            }

            return false;
        }
        private void SetupMonuments()
        {
            foreach (var monument in TerrainMeta.Path?.Monuments?.ToArray() ?? UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (string.IsNullOrEmpty(monument.displayPhrase.translated))
                {
                    float size = monument.name.Contains("power_sub") ? 35f : Mathf.Max(monument.Bounds.size.Max(), 75f);
                    monuments[monument] = monument.name.Contains("cave") ? 75f : monument.name.Contains("OilrigAI") ? 150f : size;
                }
                else
                {
                    monuments[monument] = GetMonumentFloat(monument.displayPhrase.translated.TrimEnd());
                }
            }
        }
        private float GetMonumentFloat(string monumentName)
        {
            switch (monumentName)
            {
                case "Abandoned Cabins":
                    return 54f;
                case "Abandoned Supermarket":
                    return 50f;
                case "Airfield":
                    return 200f;
                case "Bandit Camp":
                    return 125f;
                case "Giant Excavator Pit":
                    return 225f;
                case "Harbor":
                    return 150f;
                case "HQM Quarry":
                    return 37.5f;
                case "Large Oil Rig":
                    return 200f;
                case "Launch Site":
                    return 300f;
                case "Lighthouse":
                    return 48f;
                case "Military Tunnel":
                    return 100f;
                case "Mining Outpost":
                    return 45f;
                case "Oil Rig":
                    return 100f;
                case "Outpost":
                    return 250f;
                case "Oxum's Gas Station":
                    return 65f;
                case "Power Plant":
                    return 140f;
                case "Satellite Dish":
                    return 90f;
                case "Sewer Branch":
                    return 100f;
                case "Stone Quarry":
                    return 27.5f;
                case "Sulfur Quarry":
                    return 27.5f;
                case "The Dome":
                    return 70f;
                case "Train Yard":
                    return 150f;
                case "Water Treatment Plant":
                    return 185f;
                case "Water Well":
                    return 24f;
                case "Wild Swamp":
                    return 24f;
            }

            return 300f;
        }

        private static bool InRange(Vector3 a, Vector3 b, float distance, bool ex = true)
        {
            if (!ex)
            {
                return (a - b).sqrMagnitude <= distance * distance;
            }

            return (new Vector3(a.x, 0f, a.z) - new Vector3(b.x, 0f, b.z)).sqrMagnitude <= distance * distance;
        }

        private Tuple<Vector3, Quaternion> GetClosestValidPosition(Vector3 original)
        {
            var target = original - new Vector3(0, 200, 0);
            RaycastHit hitInfo;
            if (Physics.Linecast(original, target, out hitInfo) == false)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            var position = hitInfo.point;
            var collider = hitInfo.collider;
            var colliderLayer = 4;
            if (collider != null && collider.gameObject != null)
            {
                colliderLayer = collider.gameObject.layer;
            }

            if (collider == null)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            if (MaskIsBlocked(colliderLayer) || colliderLayer != 23)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            if (IsValidPosition(position) == false)
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }

            var rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * Quaternion.Euler(Vector3.zero);
            return new Tuple<Vector3, Quaternion>(position, rotation);
        }

        private bool IsValidPosition(Vector3 position)
        {
            var entities = new List<BuildingBlock>();
            Vis.Entities(position, 25, entities);
            return entities.Count == 0;
        }

        bool IsOnRoad(Vector3 target)
        {
            RaycastHit hitInfo;
            if (!Physics.Raycast(target, Vector3.down, out hitInfo, 66f, LayerMask.GetMask("Terrain", "World", "Construction", "Water"), QueryTriggerInteraction.Ignore) || hitInfo.collider == null)
                return false;

            if (hitInfo.collider.name.ToLower().Contains("road"))
                return true;
            return false;
        }

        string RandomStonePrefab()
        {
            var number = Oxide.Core.Random.Range(1, 3);
            if (number == 1) return "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab";
            if (number == 2) return "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab";
            if (number == 3) return "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab";

            return "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab";
        }

        void SpawnStone()
        {
            GenerateStones();
            timer.Once(2f, () =>
            {
                Puts($"Сгенерировано: {count} камней, всего за ивент: {allcount}");
                count = 0;
            });
        }

        private int GenerateStones()
        {
            var counter = 0;
            var neededCount = 200;
            for (var i = 0; i < neededCount; i++)
            {
                var spawnData = GetValidSpawnData();
                if (spawnData.Item1 == Vector3.zero)
                {
                    GeneratePositions();
                }
                spawnData = GetValidSpawnData();

                var stone = GameManager.server.CreateEntity(RandomStonePrefab(), spawnData.Item1, spawnData.Item2);
                stone.Spawn();
                Vector3 pos = new Vector3(2, 0, 0);
                var stone2 = GameManager.server.CreateEntity(RandomStonePrefab(), stone.GetNetworkPosition() + pos, stone.GetNetworkRotation());
                stone2.Spawn();

                SpawnedStones.Add(stone);
                SpawnedStones.Add(stone2);
                count++;
                allcount++;
                count++;
                allcount++;
            }
            return counter;
        }



        private Tuple<Vector3, Quaternion> GetValidSpawnData()
        {
            if (!_spawnData.Any())
            {
                return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
            }
            for (var i = 0; i < 25; i++)
            {
                var number = Core.Random.Range(0, _spawnData.Count);
                var spawnData = _spawnData.ElementAt(number);
                _spawnData.Remove(spawnData);
                if (IsValidPosition(spawnData.Item1))
                    return spawnData;
            }
            return new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);
        }

        #endregion

        #region Commands [Команды]

        [ConsoleCommand("rockevent.start")]
        private void ConsoleForcedEventStart(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin || args.IsClientside) return;
            if (EventHasStart == true) return;

            Puts("Принудительный старт ивента \"Двойные камни\"");
            EventLog("CONSOLE запустил принудительный старт ивента \"Двойные камни\"");

            StartEvent();
        }

        [ConsoleCommand("rockevent.stop")]
        private void ConsoleForcedEventStop(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin || args.IsClientside) return;
            if (EventHasStart == false) return;
            Puts("Принудительная остановка ивента \"Двойные камни\"");
            EventLog("CONSOLE принудительно остановил ивент \"Двойные камни\"");
            StopEvent();
        }

        [ConsoleCommand("rockevent.ui.close")]
        void ConsoleUIClose(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (EventHasStart == false) return;

            CuiHelper.DestroyUi(player, "text");
            CuiHelper.DestroyUi(player, "button");
            CuiHelper.DestroyUi(player, "helptext");

            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                FadeOut = 0.1f,
                Parent = WorkLayer,
                Name = "text",
                Components =
                {
                    new CuiTextComponent { Text = ">", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.478572 0.5733339", AnchorMax = "0.5404768 0.9200007"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "rockevent.ui.open", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "text", "button");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("rockevent.ui.open")]
        void ConsoleUIOpen(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (EventHasStart == false) return;

            CuiHelper.DestroyUi(player, "text");
            CuiHelper.DestroyUi(player, "button");

            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Parent = WorkLayer,
                Name = "helptext",
                Components =
                {
                    new CuiTextComponent { Text = $"На карте спавнятся двойные камни, собирайте их пока есть время!", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.02380949 0", AnchorMax = "1 0.5866665"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiElement
            {
                FadeOut = 0.1f,
                Parent = WorkLayer,
                Name = "text",
                Components =
                {
                    new CuiTextComponent { Text = "x", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.478572 0.5733339", AnchorMax = "0.5404768 0.9200007"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "rockevent.ui.close", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "text", "button");
            CuiHelper.AddUi(player, container);
        }

        [ChatCommand("rockevent")]
        void ChatForcedEventHandler(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, cfg.AccesSettings.StartPermission))
                return;

            if (args.Length < 1)
            {
                player.ChatMessage(" <color=#8e6874>/rockevent start</color> - запустить ивент Двойные камни\n <color=#8e6874>/rockevent stop</color> - остановить ивент Двойные камни\n <color=#8e6874>/rockevent tp</color> - телепортироваться к рандомному камню");
                return;
            }

            if (args[0] == "start")
            {
                if (EventHasStart == true)
                {
                    player.ChatMessage(" Ивент <color=#8e6874>Двойные камни</color> уже запущен");
                    return;
                }

                player.ChatMessage(" Вы успешно запустили ивент <color=#8e6874>Двойные камни</color>");
                Puts($"{player.displayName}/{player.userID} запустил принудительный старт ивента \"Двойные камни\"");
                EventLog($"{player.displayName}/{player.userID} запустил принудительный старт ивента \"Двойные камни\"");

                StartEvent();
            }
            if (args[0] == "stop")
            {
                if (EventHasStart == false)
                {
                    player.ChatMessage(" Ивент <color=#8e6874>Двойные камни</color> не был запущен");
                    return;
                }

                player.ChatMessage(" Вы успешно остановили ивент <color=#8e6874>Двойные камни</color>");
                Puts($"{player.displayName}/{player.userID} запустил принудительную остановку ивента \"Двойные камни\"");
                EventLog($"{player.displayName}/{player.userID} запустил принудительную остановку ивента \"Двойные камни\"");

                StopEvent();
            }

            if (args[0] == "tp")
            {
                if (EventHasStart == false)
                {
                    player.ChatMessage(" Ивент <color=#8e6874>Двойные камни</color> не был запущен");
                    return;
                }

                foreach (var entity in SpawnedStones)
                    player.Teleport(entity.transform.position);
            }
        }

        #endregion

        #region Helpers

        void EventLog(string text)
        {
            LogToFile("Events", text, this, true);
        }

        #endregion

        #region UI

        private void InitializeUI(BasePlayer player)
        {
            TimeSpan timetocancel = DateTime.Now - canceldate;

            CuiHelper.DestroyUi(player, WorkLayer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20 -120", OffsetMax = "300 -70" },
                CursorEnabled = false,
            }, "Hud", WorkLayer);

            container.Add(new CuiElement
            {
                Parent = WorkLayer,
                Components =
                {
                    new CuiTextComponent { Text = $"<color={cfg.AddSettings.Color}>Двойной фарм </color> ", Align = TextAnchor.UpperLeft, FontSize = 14, Font = "RobotoCondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.02380949 0", AnchorMax = "0.4380953 0.933334"},
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = WorkLayer,
                Name = "time",
                FadeOut = 0.1f,
                Components =
                {
                    new CuiTextComponent {  Text = $"[{timetocancel.ToString("mm\\:ss")}]", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.3214282 0.2399999", AnchorMax = "0.4595238 0.9066674"},
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = WorkLayer,
                Name = "helptext",
                Components =
                {
                    new CuiTextComponent { Text = $"На карте спавнятся двойные камни, собирайте их пока есть время!", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.02380949 0", AnchorMax = "1 0.5866665"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });

            container.Add(new CuiElement
            {
                FadeOut = 0.1f,
                Parent = WorkLayer,
                Name = "text",
                Components =
                {
                    new CuiTextComponent { Text = "x", Align = TextAnchor.UpperLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.478572 0.5733339", AnchorMax = "0.5404768 0.9200007"},
                    new CuiOutlineComponent { Color = "0 0 0 0.25", Distance = "0.5 0.5" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "rockevent.ui.close", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "text", "button");

            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}