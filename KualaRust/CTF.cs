using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json.Converters;
using Facepunch;
using VLB;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rust;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("CTF", "Sempai#3239", "1.1.2")]
    class CTF : RustPlugin
    {
        static CTF ins;
        PluginConfig config;

        public class PluginConfig
        {
            [JsonProperty("Изображение на флаг (url)")]
            public string FlagImage;

            [JsonProperty("Минимальное количество игроков для запуска ивента")]
            public int MininamOnline;
            [JsonProperty("Время через которое запускается ивент после старта сервера (в минутах)")]
            public float AwakeTime;
            [JsonProperty("Время через которое запускается ивент после окончания предыдущего ивента (в минутах)")]
            public float NextTime;
            [JsonProperty("Длительность ивента (в минутах)")]
            public float EventTime;
            [JsonProperty("Приостанавливать время ивента, если игрок удерживает флаг?")]
            public bool FreezTime;

            [JsonProperty("Защита игрока с флагом в % от нормальной (0 - стандартная, Значения больше нуля увеличат защиту))")]
            public float AdditionalProtection;
            [JsonProperty("Сколько минут игрок должен удерживать флаг?")]
            public float FlagHoldingTime;
            [JsonProperty("Если игрок потерял флаг, сбрасывать время удержания? (false - будет учитывать суммарное время с нескольких удержаний)")]
            public bool ResetSumTime;

            [JsonProperty("Если закончилось время ивента, выбирать игрока с наибольшим временем удержания?")]
            public bool CanMaxTimeWin;

            [JsonProperty("Скорость захвата флага")]
            public float CapturingSpeed;

            [JsonProperty("Разрешить игроку несущему флаг строить?")]
            public bool CanBuild;
            [JsonProperty("Разрешить игроку несущему флаг открывать двери?")]
            public bool CanOpenDoor;
            [JsonProperty("Разрешить игроку несущему флаг использовать транспорт?")]
            public bool CanUseVehicle;
            [JsonProperty("Разрешить игроку несущему флаг дропнуть флаг?")]
            public bool CanDrop;
            [JsonProperty("Разрешить игроку заносить флаг в зону строительства?")]
            public bool CanEnterToBP;
            [JsonProperty("Радиус в котором запрещено строить рядом с флагом")]
            public float CanBuildRadius;

            [JsonProperty("Призы за победу на ивенте")]
            public List<CustomItem> customItems;

            [JsonProperty("Команды запрещённые  к использованию игроком несущим флаг")]
            public List<string> BlockedComamnds;
        }

        public class CustomItem
        {
            [JsonProperty("Шортнейм предмета")]
            public string ShortName;
            [JsonProperty("Исполняемая команда (%STEAMID% - ключ для вставки SteamID игрока)")]
            public string Command;
            [JsonProperty("Кастомное имя предмета")]
            public string CustomName;
            [JsonProperty("Количество предмета")]
            public int Amount;
            [JsonProperty("SkinID предмета")]
            public ulong SkinID;
            [JsonProperty("Описания приза для оповещения {amount} - количество если необходимо")]
            public string Description;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                FlagImage = "",
                MininamOnline = 5,
                AwakeTime = 1,
                NextTime = 60,
                EventTime = 30,
                FreezTime = true,
                AdditionalProtection = 0,
                FlagHoldingTime = 5,
                ResetSumTime = false,
                CanMaxTimeWin = true,
                CanBuild = false,
                CanOpenDoor = false,
                CanUseVehicle = false,
                CanDrop = true,
                CanEnterToBP = false,
                CanBuildRadius = 10f,
                CapturingSpeed = 60,
                customItems = new List<CustomItem>()
                {
                    new CustomItem
                    {
                        ShortName = "scrap",
                        Amount = 1000,
                        SkinID = 0,
                        Command = "",
                        CustomName = "",
                        Description = "Металлолом {amount}",
                    },

                    new CustomItem
                    {
                        ShortName = "",
                        Amount = 0,
                        SkinID = 0,
                        Command = "addgroup %STEAMID% premium 7d",
                        CustomName = "",
                        Description = "Премиум на 7 дней",
                    },
                },
                BlockedComamnds = new List<string>()
                {
                    "/trade",
                    "/kit",
                    "/bp",
                    "/storage",
                    "/pass",
                    "/tp",
                    "/tpa",
                    "kit",
                    "backpack.open",
                }
            };
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

        private void OnServerInitialized()
        {
            ins = this;
            LoadMessages();

            Unsubscribe("OnPlayerInput");
            Unsubscribe("OnEntityTakeDamage");

            if (!string.IsNullOrEmpty(config.FlagImage))
                ImageLibrary?.Call("AddImage", config.FlagImage, "flagImage");

            if (config.AwakeTime > 0)
                timer.In(config.AwakeTime * 60f, StartEvent);

            timer.Every(1f, RedrawTopPlayers);
        }

        void Unload()
        {
            if (ctfEvent != null)
            {
                UnityEngine.Object.Destroy(ctfEvent);
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "TopPanel.BG");
                CuiHelper.DestroyUi(player, "CapruteBG");
            }
        }

        void OnServerShutdown()
        {
            if (ctfEvent != null)
            {
                UnityEngine.Object.Destroy(ctfEvent);
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }

            if (ctfEvent != null)
            {
                CTFMarker.marker.UpdateMarker();
                ctfTime[player] = 0;
                DrawTopPanel(player);
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player.GetComponentInChildren<CTFEvent>() && input.WasJustPressed(BUTTON.USE))
            {
                player.GetComponentInChildren<CTFEvent>().StartDrop();
            }
            if (player.GetComponentInChildren<CTFEvent>() && input.WasDown(BUTTON.USE))
            {
                player.GetComponentInChildren<CTFEvent>().DropProcess();
            }
            if (player.GetComponentInChildren<CTFEvent>() && input.WasJustReleased(BUTTON.USE))
            {
                player.GetComponentInChildren<CTFEvent>().StopDrop();
            }
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;

            if (ctfEvent != null && ctfEvent.GetFlagEntity() == entity)
                return false;

            var victumPlayer = entity.ToPlayer();
            if (victumPlayer == null) return null;
            if (!victumPlayer.userID.IsSteamId()) return null;

            if (ctfEvent != null && victumPlayer.GetComponentInChildren<CTFEvent>() && config.AdditionalProtection != 0)
            {
                info.damageTypes.ScaleAll(1f - (config.AdditionalProtection / 100f));
            }
            return null;
        }

        private object OnUserCommand(IPlayer ipl, string command, string[] args)
        {
            if (ipl == null || !ipl.IsConnected) return null;
            var player = ipl.Object as BasePlayer;
            if (player == null) return null;
            command = command.Insert(0, "/");
            if (config.BlockedComamnds.Contains(command.ToLower()) && ctfEvent != null && player.GetComponentInChildren<CTFEvent>())
            {
                SendReply(player, GetMsg("commandbanned", player));
                return false;
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var connection = arg.Connection;
            if (connection == null || string.IsNullOrEmpty(arg.cmd?.FullName)) return null;
            var player = arg.Player();
            if (player == null) return null;
            if ((config.BlockedComamnds.Contains(arg.cmd.Name.ToLower()) || config.BlockedComamnds.Contains(arg.cmd.FullName.ToLower())) && ctfEvent != null && player.GetComponentInChildren<CTFEvent>())
            {
                SendReply(player, GetMsg("commandbanned", player));
                return false;
            }
            return null;
        }
        Vector3 GetEventPosition()
        {
            var eventPos = Vector3.zero;
            int maxRetries = 100;
            var monuments = TerrainMeta.Path.Monuments.Select(monument => monument.transform.position).ToList();
            while (eventPos == Vector3.zero && maxRetries > 0)
            {
                var reply = 0;

                eventPos = GetSafeDropPosition(RandomDropPosition());

                eventPos.y = GetGroundPosition(eventPos);

                if (reply == 0) { }

                foreach (var monument in monuments)
                {
                    if (Vector3.Distance(eventPos, monument) < 150f)
                    {
                        eventPos = Vector3.zero;
                        break;
                    }
                }
            }

            return eventPos;
        }

        Vector3 GetSafeDropPosition(Vector3 position)
        {
            RaycastHit hit;
            position.y += 200f;
            int blockedMask = LayerMask.GetMask(new[] { "Player (Server)", "Trigger", "Prevent Building" });
            var BlockedLayers = new List<int> { (int)Layer.Water, (int)Layer.Construction, (int)Layer.Trigger, (int)Layer.Prevent_Building, (int)Layer.Deployed, (int)Layer.Tree };
            if (Physics.Raycast(position, Vector3.down, out hit))
            {
                if (hit.collider?.gameObject == null) return Vector3.zero;
                string ColName = hit.collider.name;
                if (!BlockedLayers.Contains(hit.collider.gameObject.layer) && ColName != "MeshColliderBatch" && ColName != "iceberg_3" && ColName != "iceberg_2" && !ColName.Contains("rock_cliff"))
                {
                    position.y = Mathf.Max(hit.point.y, TerrainMeta.HeightMap.GetHeight(position));
                    var colliders = Pool.GetList<Collider>();
                    Vis.Colliders(position, 1, colliders, blockedMask, QueryTriggerInteraction.Collide);
                    bool blocked = colliders.Count > 0;
                    Pool.FreeList<Collider>(ref colliders);
                    if (!blocked) return position;
                }
            }

            return Vector3.zero;
        }

        Vector3 RandomDropPosition()
        {
            var vector = Vector3.zero;
            var filter = new SpawnFilter();
            float num = 1000f, x = TerrainMeta.Size.x / 3;
            do
            {
                vector = Vector3Ex.Range(-x, x);
            }
            while (filter.GetFactor(vector) == 0f && (num -= 1f) > 0f);
            float max = TerrainMeta.Size.x / 2;
            float height = TerrainMeta.HeightMap.GetHeight(vector);
            vector.y = height;
            return vector;
        }

        [ConsoleCommand("ctf")]
        void CTFManualcmd(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            if (ctfEvent == null)
            {
                ctfEvent = new GameObject().AddComponent<CTFEvent>();
                ctfEvent.CrateEvent();
                NextTick(DrawPlayerTop);

                foreach (var pl in BasePlayer.activePlayerList)
                    SendReply(pl, GetMsg("manualstart", pl).Replace("{time}", config.FlagHoldingTime.ToString()));

                PrintWarning($"Manual Event Started");
            }
            else
            {
                UnityEngine.Object.Destroy(ctfEvent);
                ctfEvent = new GameObject().AddComponent<CTFEvent>();
                ctfEvent.CrateEvent();
                NextTick(DrawPlayerTop);

                foreach (var pl in BasePlayer.activePlayerList)
                    SendReply(pl, GetMsg("manualrestart", pl).Replace("{time}", config.FlagHoldingTime.ToString()));
                PrintWarning($"Manual Event Started");
            }
        }

        [ChatCommand("ctf")]
        void CTFManual(BasePlayer player)
        {
            if (!player.IsAdmin) return;

            if (ctfEvent == null)
            {
                ctfEvent = new GameObject().AddComponent<CTFEvent>();
                ctfEvent.CrateEvent(player.transform.position);
                NextTick(DrawPlayerTop);

                foreach (var pl in BasePlayer.activePlayerList)
                    SendReply(pl, GetMsg("manualstart", pl).Replace("{time}", config.FlagHoldingTime.ToString()));
            }
            else
            {
                UnityEngine.Object.Destroy(ctfEvent);
                ctfEvent = new GameObject().AddComponent<CTFEvent>();
                ctfEvent.CrateEvent(player.transform.position);
                NextTick(DrawPlayerTop);

                foreach (var pl in BasePlayer.activePlayerList)
                    SendReply(pl, GetMsg("manualrestart", pl).Replace("{time}", config.FlagHoldingTime.ToString()));
            }
        }

        void StartEvent()
        {
            if (ctfEvent != null)
            {
                return;
            }

            if (config.MininamOnline > BasePlayer.activePlayerList.Count)
            {
                StartNextEvent();
                return;
            }

            PrintWarning("CTF Event STARTED");
            ctfEvent = new GameObject().AddComponent<CTFEvent>();
            ctfEvent.CrateEvent();
            NextTick(DrawPlayerTop);

            foreach (var player in BasePlayer.activePlayerList)
                SendReply(player, GetMsg("autostart", player).Replace("{time}", config.FlagHoldingTime.ToString()));
        }

        void StartNextEvent()
        {
            timer.In(config.NextTime * 60f, StartEvent);
        }

        void OnDoorClosed(Door door, BasePlayer player)
        {
            if (ctfEvent != null && player.GetComponentInChildren<CTFEvent>() && !config.CanOpenDoor)
            {
                if (!door.IsOpen())
                    door.SetOpen(true);

                SendReply(player, GetMsg("canclosedoor", player));
            }
        }

        void OnDoorOpened(Door door, BasePlayer player)
        {
            if (ctfEvent != null && player.GetComponentInChildren<CTFEvent>() && !config.CanOpenDoor)
            {
                if (door.IsOpen())
                    door.CloseRequest();

                SendReply(player, GetMsg("canopendoor", player));
            }
        }

        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (ctfEvent != null && player.GetComponentInChildren<CTFEvent>() && !config.CanUseVehicle)
            {
                SendReply(player, GetMsg("canvehicle", player));
                return false;
            }

            return null;
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null || target.GetWorldPosition() == null || planner.GetOwnerPlayer()?.GetActiveItem() == null) return null;
            var player = planner.GetOwnerPlayer();

            if (ctfEvent != null)
            {
                if (player.GetComponentInChildren<CTFEvent>() && !config.CanBuild)
                {
                    SendReply(player, GetMsg("canbuild", player));
                    return false;
                }
                if (config.CanBuildRadius > 0)
                {
                    if (Vector3.Distance(target.position, ctfEvent.GetPos()) <= config.CanBuildRadius)
                    {
                        SendReply(player, GetMsg("closeBiuld", player));
                        return false;
                    }
                }
            }

            return null;
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player.GetComponentInChildren<CTFEvent>())
            {
                if (config.ResetSumTime)
                    ctfTime[player] = 0;

                var pos = player.transform.position;
                pos.y = GetGroundPosition(pos);
                ctfEvent.UnParrentPlayer(pos);
                CuiHelper.DestroyUi(player, "CaptureInfo");

                foreach (var pl in BasePlayer.activePlayerList)
                    SendReply(pl, GetMsg("flagloss", pl).Replace("{displayName}", player.displayName));
            }

            return null;
        }

        void DrawPlayerTop()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                ctfTime[player] = 0;
                DrawTopPanel(player);
            }
        }
        Dictionary<BasePlayer, float> ctfTime = new Dictionary<BasePlayer, float>();
        void DrawTopPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement()
            {
                Parent = "Hud",
                Name = "TopPanel.BG",
                Components =
                {
                    new CuiImageComponent{Color = "1 1 1 0.1", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                    new CuiRectTransformComponent{AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-240 -250", OffsetMax = "-10 -50"}
                }
            });
            container.Add(new CuiElement()
            {
                Parent = "TopPanel.BG",
                Name = "Header",
                Components =
                {
                    new CuiTextComponent{Text = "CTF TOP", Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.65", FontSize = 22, Font = "permanentmarker.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-120 -35", OffsetMax = "120 0"}
                }
            });

            CuiHelper.DestroyUi(player, "TopPanel.BG");
            CuiHelper.AddUi(player, container);
        }

        void RedrawTopPlayers()
        {
            if (ctfEvent == null) return;
            var container = new CuiElementContainer();
            var gaph = 0;
            int pos = 1;
            container.Add(new CuiElement()
            {
                Parent = "TopPanel.BG",
                Name = "TopPanel",
                Components =
                    {
                        new CuiImageComponent{Color = "1 1 1 0.0"},
                        new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
            });
            foreach (var ctf in ctfTime.OrderByDescending(x => x.Value).Take(5))
            {
                container.Add(new CuiElement()
                {
                    Parent = "TopPanel",
                    Name = "TopPlayer",
                    Components =
                        {
                            new CuiTextComponent{Text = $"{pos}. {string.Join("",ctf.Key.displayName.Take(10))} : {(ctf.Value > 0 ? TimeSpan.FromSeconds(ctf.Value).ToShortString() : "00:00:00")}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 0.65", FontSize = 17, Font = "permanentmarker.ttf"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.1 0.1"},
                            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-110 -{65 + gaph}", OffsetMax = $"115 -{35 + gaph}"}
                        }
                });
                pos++;
                gaph += 30;

                container.Add(new CuiElement()
                {
                    Parent = "TopPanel",
                    Name = "EventTime",
                    Components =
                    {
                        new CuiTextComponent{Text = ctfEvent.GetEventTime(), Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.65", FontSize = 22},
                        new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.5 0.5"},
                        new CuiRectTransformComponent{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-100 -35", OffsetMax = "100 -5"}
                    }
                });
            }
            foreach (var player in BasePlayer.activePlayerList.ToArray())
            {
                CuiHelper.DestroyUi(player, "TopPanel");
                CuiHelper.AddUi(player, container);
            }
        }
        private float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity,
                LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff")) return Mathf.Max(hit.point.y, y);
            return y;
        }

        void HookUnsubscribe(string method)
        {
            Unsubscribe(method);
        }

        void HookSubscribe(string method)
        {
            Subscribe(method);
        }

        void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            var player = entity.ToPlayer();
            if (trigger.name == "SafeZone" && player != null)
            {
                if (ctfEvent != null)
                {
                    if (player.GetComponentInChildren<CTFEvent>())
                    {
                        var pos = trigger.transform.position - player.transform.position;
                        var targetPos = player.transform.position + (pos / pos.magnitude) * (Vector3.Distance(player.GetComponentInChildren<CTFEvent>().transform.position, player.transform.position) - 5f);
                        targetPos.y = ins.GetGroundPosition(targetPos);
                        player.GetComponentInChildren<CTFEvent>().Drop(targetPos);
                        Puts($"Targetpos: {targetPos} : Center {trigger.transform.position}");
                    }
                }
            }
        }

        class CTFMarker : FacepunchBehaviour
        {
            MapMarkerGenericRadius mapMarker;
            VendingMachineMapMarker MarkerT;
            public static CTFMarker marker;


            public void UpdateMarker()
            {
                mapMarker.SendUpdate();
            }

            void Awake()
            {
                marker = this;
                mapMarker = (MapMarkerGenericRadius)GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab");
                mapMarker.enableSaving = false;
                mapMarker.globalBroadcast = true;
                mapMarker.Spawn();
                mapMarker.radius = 0.2f;
                mapMarker.alpha = 1f;
                UnityEngine.Color color = new UnityEngine.Color(0.58f, 0.18f, 0.11f, 1.00f);
                UnityEngine.Color color2 = new UnityEngine.Color(0, 0, 0, 0);
                mapMarker.color1 = color;
                mapMarker.color2 = color2;
                mapMarker.SendUpdate();

                MarkerT = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab") as VendingMachineMapMarker;
                MarkerT.markerShopName = "CTF";
                MarkerT.enableSaving = false;
                MarkerT.globalBroadcast = true;
                MarkerT.Spawn();
            }

            void FixedUpdate()
            {
                if (mapMarker == null || MarkerT == null) return;

                mapMarker.transform.position = gameObject.transform.position;
                MarkerT.transform.position = gameObject.transform.position;

                mapMarker.SendNetworkUpdate();
                MarkerT.SendNetworkUpdate();
            }

            void OnDestroy()
            {
                if (MarkerT?.IsDestroyed == false)
                    MarkerT?.Kill();

                if (mapMarker?.IsDestroyed == false)
                    mapMarker?.Kill();

                Destroy(this);
            }
        }
        [PluginReference]
        Plugin ImageLibrary;

        CTFEvent ctfEvent;
        class CTFEvent : FacepunchBehaviour
        {
            BaseEntity flag;
            BasePlayer capturePlayer;
            SphereCollider captureZone;
            CTFMarker markers;

            public BaseEntity GetFlagEntity() => flag;

            void CheckBuildingPrivlidge()
            {
                if (capturePlayer == null) return;

                var bp = capturePlayer.GetBuildingPrivilege();
                if (bp == null) return;

                var pos = bp.transform.position - capturePlayer.transform.position;
                var targetPos = capturePlayer.transform.position + (pos / pos.magnitude) * (Vector3.Distance(transform.position, capturePlayer.transform.position) - 5f);
                targetPos.y = ins.GetGroundPosition(targetPos);
                Drop(targetPos);
            }

            public void StopDrop()
            {
                processLenght = 2f;
                CuiHelper.DestroyUi(capturePlayer, "CapruteBG");
            }

            public void StartDrop()
            {
                processLenght = 2f;
                DrawDropUI();
            }

            public void DropProcess()
            {
                processLenght += Time.fixedDeltaTime * 120;
                DrawProcessLine();
                if (processLenght >= 298)
                    Drop();
            }

            public void Drop(Vector3 pos = new Vector3())
            {
                foreach (var pl in BasePlayer.activePlayerList)
                    ins.SendReply(pl, ins.GetMsg("flagdroped", pl).Replace("{displayName}", capturePlayer.displayName));

                CuiHelper.DestroyUi(capturePlayer, "CapruteBG");
                CuiHelper.DestroyUi(capturePlayer, "CaptureInfo");
                processLenght = 2;

                if (pos == new Vector3())
                    pos = capturePlayer.transform.position;

                var posY = ins.GetGroundPosition(capturePlayer.transform.position);
                pos.y = posY;
                capturePlayer = null;
                isGrap = false;
                capruting = false;
                gameObject.transform.SetParent(null, true);
                flag.SetParent(null, true, true);

                gameObject.transform.position = pos;
                flag.transform.position = pos + Vector3.up * 10;
                ins.HookUnsubscribe(nameof(OnPlayerInput));
            }

            public void UnParrentPlayer(Vector3 pos)
            {
                processLenght = 2;
                capturePlayer = null;
                isGrap = false;
                capruting = false;
                gameObject.transform.SetParent(null, true);
                flag.SetParent(null, true, true);

                gameObject.transform.position = pos;
                flag.transform.position = pos + Vector3.up * 10;
                ins.HookUnsubscribe(nameof(OnPlayerInput));
            }

            float eventTime = 0;

            void DrawCatureHelp()
            {
                if (!ins.config.CanDrop) return;

                var container = new CuiElementContainer();
                container.Add(new CuiElement()
                {
                    Parent = "TopPanel.BG",
                    Name = "CaptureInfo",
                    Components =
                    {
                        new CuiTextComponent{Color = "1 1 1 0.65", Text= "Hold <color=red>E</color> to drop the Flag", Align= TextAnchor.MiddleCenter, FontSize = 12},
                        new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.5 0.5"},
                        new CuiRectTransformComponent{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-100 -85", OffsetMax = "100 -55"}
                    }
                });

                CuiHelper.DestroyUi(capturePlayer, "CaptureInfo");

                CuiHelper.AddUi(capturePlayer, container);
            }

            public void CrateEvent(Vector3 pos = new Vector3())
            {
                ins.HookSubscribe(nameof(OnEntityTakeDamage));

                eventTime = ins.config.EventTime * 60f;
                var posEvent = pos == new Vector3() ? ins.GetEventPosition() : pos;
                gameObject.transform.position = posEvent;
                flag = GameManager.server.CreateEntity("assets/prefabs/deployable/signs/sign.hanging.banner.large.prefab", posEvent + Vector3.up * 10);
                flag.globalBroadcast = true;
                flag.Spawn();

                flag.EnableGlobalBroadcast(true);
                Destroy(flag.GetComponent<DestroyOnGroundMissing>());
                Destroy(flag.GetComponent<GroundWatch>());
                var banner = flag.GetComponent<Signage>();
                banner.EnsureInitialized();
                if (!string.IsNullOrEmpty(ins.config.FlagImage))
                    banner.textureIDs[0] = ins.ImageLibrary.Call<uint>("GetImage", "flagImage");
                banner.SendNetworkUpdate();
                flag.SetFlag(BaseEntity.Flags.Locked, true);

                captureZone = gameObject.AddComponent<SphereCollider>();
                captureZone.gameObject.layer = (int)Layer.Reserved1;
                captureZone.isTrigger = true;
                captureZone.radius = 3f;

                markers = new GameObject().AddComponent<CTFMarker>();
                markers.transform.position = flag.transform.position;
            }

            void DrawCaptureUI()
            {
                var container = new CuiElementContainer();
                container.Add(new CuiElement()
                {
                    Parent = "Hud.Menu",
                    Name = "CapruteBG",
                    Components =
                    {
                        new CuiImageComponent{Color = "1 1 1 0.3", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                        new CuiRectTransformComponent{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-150 150", OffsetMax = "150 170"}
                    }
                });
                container.Add(new CuiElement()
                {
                    Parent = "CapruteBG",
                    Name = "Title",
                    Components =
                    {
                        new CuiTextComponent{Text = ins.GetMsg("captyring", capturePlayer), Color = "1 1 1 00.65", Align = TextAnchor.MiddleCenter, FontSize = 28, Font = "robotocondensed-regular.ttf"},
                        new CuiRectTransformComponent{AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-150 5", OffsetMax = "150 45"}
                    }
                });

                container.Add(new CuiElement()
                {
                    Parent = "CapruteBG",
                    Name = "Img",
                    Components =
                    {
                        new CuiImageComponent{Sprite = "assets/icons/stopwatch.png"},
                        new CuiRectTransformComponent{AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-35 -15", OffsetMax = "-5 15"}
                    }
                });
                CuiHelper.DestroyUi(capturePlayer, "CapruteBG");

                CuiHelper.AddUi(capturePlayer, container);
                DrawProcessLine();
            }
            void DrawDropUI()
            {
                var container = new CuiElementContainer();
                container.Add(new CuiElement()
                {
                    Parent = "Hud.Menu",
                    Name = "CapruteBG",
                    Components =
                    {
                        new CuiImageComponent{Color = "1 1 1 0.3", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                        new CuiRectTransformComponent{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-150 150", OffsetMax = "150 170"}
                    }
                });
                container.Add(new CuiElement()
                {
                    Parent = "CapruteBG",
                    Name = "Title",
                    Components =
                    {
                        new CuiTextComponent{Text = ins.GetMsg("dropping", capturePlayer), Color = "1 1 1 00.65", Align = TextAnchor.MiddleCenter, FontSize = 28, Font = "robotocondensed-regular.ttf"},
                        new CuiRectTransformComponent{AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-150 5", OffsetMax = "150 45"}
                    }
                });

                container.Add(new CuiElement()
                {
                    Parent = "CapruteBG",
                    Name = "Img",
                    Components =
                    {
                        new CuiImageComponent{Sprite = "assets/icons/stopwatch.png"},
                        new CuiRectTransformComponent{AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-35 -15", OffsetMax = "-5 15"}
                    }
                });
                CuiHelper.DestroyUi(capturePlayer, "CapruteBG");

                CuiHelper.AddUi(capturePlayer, container);
                DrawProcessLine();
            }
            float processLenght = 2;
            void DrawProcessLine()
            {
                var container = new CuiElementContainer();
                container.Add(new CuiElement()
                {
                    Parent = "CapruteBG",
                    Name = "ProcessLine",
                    Components =
                    {
                        new CuiImageComponent{Color = "0 1 0 0.3", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                        new CuiRectTransformComponent{AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "2 -8", OffsetMax = $"{processLenght} 8"}
                    }
                });
                CuiHelper.DestroyUi(capturePlayer, "ProcessLine");
                CuiHelper.AddUi(capturePlayer, container);
            }
            bool capruting = false;
            bool isGrap = false;

            public string GetEventTime()
            {
                string time = TimeSpan.FromSeconds(eventTime).ToShortString();

                if (isGrap && ins.config.FreezTime)
                {
                    time = $"<color=#9F0000FF>{TimeSpan.FromSeconds(eventTime).ToShortString()}</color>";
                }

                return time;

            }

            void Update()
            {
                if (flag == null) return;

                markers.transform.position = flag.transform.position;

                if (!isGrap || !ins.config.FreezTime)
                    eventTime -= Time.deltaTime;

                if (flag.IsDestroyed)
                {
                    Destroy(this);
                    return;
                }

                flag.transform.RotateAround(gameObject.transform.position, Vector3.up, Time.deltaTime * 60);
                flag.SendNetworkUpdateImmediate();

                if (eventTime <= 0)
                {
                    EndEvent();
                    return;
                }

                if (capturePlayer == null) return;
                CheckBuildingPrivlidge();
                if (capruting)
                {
                    if (capturePlayer.IsDead() || !capturePlayer.IsConnected || capturePlayer.isMounted)
                    {
                        processLenght = 2;
                        CuiHelper.DestroyUi(capturePlayer, "CapruteBG");
                        capturePlayer = null;
                        isGrap = false;
                        capruting = false;
                        gameObject.transform.SetParent(null, true);
                        flag.SetParent(null, true, true);
                        ins.HookUnsubscribe(nameof(OnPlayerInput));
                        return;
                    }

                    processLenght += Time.deltaTime * UnityEngine.Random.value * ins.config.CapturingSpeed;
                    DrawProcessLine();
                    if (processLenght >= 298)
                    {
                        capruting = false;

                        foreach (var player in BasePlayer.activePlayerList)
                            ins.SendReply(player, ins.GetMsg("capturplayer", player).Replace("{displayName}", capturePlayer.displayName));

                        CuiHelper.DestroyUi(capturePlayer, "CapruteBG");

                        gameObject.transform.SetParent(capturePlayer.transform);
                        gameObject.transform.position = capturePlayer.transform.position;
                        gameObject.transform.localPosition = new Vector3();

                        flag.gameObject.Identity();
                        flag.SetParent(capturePlayer, "spine_END", false);
                        flag.transform.localPosition = Vector3.up * 10;
                        flag.transform.localRotation = new Quaternion();

                        isGrap = true;

                        if (ins.config.CanDrop)
                            ins.HookSubscribe(nameof(OnPlayerInput));

                        DrawCatureHelp();
                    }
                }
                if (isGrap)
                {
                    if (capturePlayer.IsDead() || !capturePlayer.IsConnected)
                    {
                        processLenght = 2;
                        CuiHelper.DestroyUi(capturePlayer, "CapruteBG");
                        CuiHelper.DestroyUi(capturePlayer, "CaptureInfo");
                        capturePlayer = null;
                        isGrap = false;
                        capruting = false;
                        gameObject.transform.SetParent(null, true);
                        flag.SetParent(null, true, true);

                        ins.HookUnsubscribe(nameof(OnPlayerInput));
                        return;
                    }

                    ins.ctfTime[capturePlayer] += Time.deltaTime;
                    if (ins.ctfTime[capturePlayer] >= ins.config.FlagHoldingTime * 60f)
                    {
                        GetWinner();
                    }
                }
            }

            void GetWinner(ulong playerid = 0)
            {
                foreach (var player in BasePlayer.activePlayerList)
                    ins.SendReply(player, ins.GetMsg("winnerplayer", player).Replace("{displayName}", capturePlayer.displayName));

                string msg = "‌﻿‌​‍‍";
                msg = $"{ins.GetMsg("winprizeinfo", capturePlayer)}\n";
                foreach (var it in ins.config.customItems)
                {
                    if (!string.IsNullOrEmpty(it.Command))
                    {
                        string cmd = "";
                        cmd = it.Command.Replace("%STEAMID%", capturePlayer.UserIDString);
                        ins.rust.RunServerCommand(cmd);
                        if (!string.IsNullOrEmpty(it.Description))
                            msg += $"{it.Description}\n";
                    }
                    else if (!string.IsNullOrEmpty(it.ShortName))
                    {
                        Item item = ItemManager.CreateByName(it.ShortName, it.Amount, it.SkinID);
                        if (string.IsNullOrEmpty(it.CustomName))
                            item.name = it.CustomName;
                        capturePlayer.GiveItem(item);
                        if (!string.IsNullOrEmpty(it.Description))
                            msg += $"{it.Description.Replace("{amount}", item.amount.ToString())}\n";
                    }
                }
                ins.SendReply(capturePlayer, msg);
                ins.HookUnsubscribe(nameof(OnEntityTakeDamage));
                ins.HookUnsubscribe(nameof(OnPlayerInput));
                ins.StartNextEvent();
                OnDestroy();
            }

            void EndEvent()
            {
                if (ins.config.CanMaxTimeWin)
                {
                    var winner = ins.ctfTime.OrderByDescending(x => x.Value).FirstOrDefault(x => x.Key.IsConnected && x.Value > 0).Key;

                    if (winner != null)
                    {
                        foreach (var player in BasePlayer.activePlayerList)
                            ins.SendReply(player, ins.GetMsg("winnerplayer", player).Replace("{displayName}", capturePlayer.displayName));

                        string msg = $"{ins.GetMsg("winprizeinfo", capturePlayer)}\n";
                        foreach (var it in ins.config.customItems)
                        {
                            if (!string.IsNullOrEmpty(it.Command))
                            {
                                string cmd = "";
                                cmd = it.Command.Replace("%STEAMID%", winner.UserIDString);
                                ins.rust.RunServerCommand(cmd);
                                if (!string.IsNullOrEmpty(it.Description))
                                    msg += $"{it.Description}\n";
                            }
                            else if (!string.IsNullOrEmpty(it.ShortName))
                            {
                                Item item = ItemManager.CreateByName(it.ShortName, it.Amount, it.SkinID);
                                if (string.IsNullOrEmpty(it.CustomName))
                                    item.name = it.CustomName;
                                winner.GiveItem(item);
                                if (!string.IsNullOrEmpty(it.Description))
                                    msg += $"{it.Description.Replace("{amount}", item.amount.ToString())}\n";
                            }
                        }
                        ins.SendReply(winner, msg);
                    }
                    else
                        foreach (var player in BasePlayer.activePlayerList)
                            ins.SendReply(player, ins.GetMsg("endnowinner", player));
                }
                else
                    foreach (var player in BasePlayer.activePlayerList)
                        ins.SendReply(player, ins.GetMsg("endnowinner", player));


                ins.HookUnsubscribe(nameof(OnEntityTakeDamage));
                ins.HookUnsubscribe(nameof(OnPlayerInput));
                ins.StartNextEvent();
                OnDestroy();
            }

            public Vector3 GetPos() => flag.transform.position;

            void OnTriggerEnter(Collider col)
            {
                if (col.name.Contains("npc")) return;
                BasePlayer player = col?.GetComponentInParent<BasePlayer>();
                if (player != null && player.userID.IsSteamId() && !player.isMounted)
                {
                    if (capturePlayer == null)
                    {
                        processLenght = 2;
                        capturePlayer = player;
                        DrawCaptureUI();
                        isGrap = false;
                        capruting = true;
                    }
                }
            }

            void OnTriggerStay(Collider col)
            {
                BasePlayer player = col?.GetComponentInParent<BasePlayer>();
                if (player == null || player.IsNpc || player.IsDead()) return;
                OnTriggerEnter(col);
            }

            void OnTriggerExit(Collider col)
            {
                if (col.name.Contains("npc")) return;
                BasePlayer player = col?.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    if (capturePlayer == player)
                    {
                        //capturePlayer.EnableGlobalBroadcast(false);
                        capturePlayer = null;
                        CuiHelper.DestroyUi(player, "CapruteBG");
                        isGrap = false;
                        capruting = false;
                        ins.HookUnsubscribe(nameof(OnPlayerInput));
                    }
                }
            }

            void OnDestroy()
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "TopPanel.BG");
                    CuiHelper.DestroyUi(player, "CapruteBG");
                }

                Destroy(markers);

                if (flag?.IsDestroyed == false)
                {
                    flag?.Kill();
                }

                Destroy(captureZone);
                Destroy(this);
            }

        }

        string GetMsg(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["endnowinner"] = "The event is over. The winners have not been determined",
                ["commandbanned"] = "You can not use commands when a flag is in your hands.",
                ["winprizeinfo"] = "For winning the event, you received:",
                ["winnerplayer"] = "Player {displayName} won the Capture the Flag event",
                ["capturplayer"] = "The flag was captured by the player {displayName}",
                ["captyring"] = "CAPTURE",
                ["dropping"] = "Dropping the Flag",
                ["flagloss"] = "Player {displayName} loss the Flag.",
                ["canbuild"] = "Your hands are busy with the flag, you can not build!",
                ["closeBiuld"] = "You can not build close to the flag!",
                ["canvehicle"] = "You can't get into a transport with a flag",
                ["canopendoor"] = "You can't open the doors, your hands are busy with the flag",
                ["canclosedoor"] = "You can't close the doors, your hands are busy with the flag",
                ["autostart"] = "The CAPTURE THE FLAG event has been STARTED!\nThe location is marked on the map!\nHold the flag for {time} min. and get a reward",
                ["manualstart"] = "The Flag Capture event was launched by the Administrator!\nThe location is marked on the map!\nHold the flag for {time} min. and get a reward",
                ["manualrestart"] = "Event Capture Flag has been restarted by the Administrator!\nThe location is marked on the map!\nHold the flag for {time} min. and get a reward",
                ["flagdroped"] = "Player {displayName} drop the Flag."

            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["endnowinner"] = "Ивент закончен.\nПобедители не определены.",
                ["commandbanned"] = "Нельзя использовать команды когда в руках находится флаг. ",
                ["winprizeinfo"] = "За победу на ивенте Вы получили:",
                ["winnerplayer"] = "Игрок {displayName} победил в ивенте Захват Флага",
                ["capturplayer"] = "Флаг захвачен игроком {displayName}",
                ["captyring"] = "ЗАХВАТ",
                ["dropping"] = "Сброс флага",
                ["flagloss"] = "Игрок {displayName} потерял флаг.",
                ["canbuild"] = "Ваши руки заняты флагом, строить нельзя!",
                ["closeBiuld"] = "Нельзя строить рядом с флагом!",
                ["canvehicle"] = "С флагом нельзя садиться в транспорт",
                ["canopendoor"] = "Нельзя открыть двери, руки заняты флагом",
                ["canclosedoor"] = "Нельзя закрыть двери, руки заняты флагом",
                ["autostart"] = "Запущен ивента ЗАХВАТ ФЛАГА!\nМестоположение отмечено на карте!\nУдержите флаг в течении {time} мин. и получите награду",
                ["manualstart"] = "Ивента Захват Флага запущен Администратором!\nМестоположение отмечено на карте!\nУдержите флаг в течении {time} мин. и получите награду",
                ["manualrestart"] = "Ивента Захват Флага перезапущен Администратором!\nМестоположение отмечено на карте!\nУдержите флаг в течении {time} мин. и получите награду",
                ["flagdroped"] = "Игрок {displayName} бросил флаг."

            }, this, "ru");
        }
    }
}
