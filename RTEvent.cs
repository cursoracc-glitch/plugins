using Network;
using Newtonsoft.Json;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using Steamworks.ServerList;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RTEvent", "EcoSmile Сделал CUI - Deversive", "1.0.7")]
    class RTEvent : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary, NoteUI;
        private string mainui = "mainui5";
        static RTEvent ins;
        PluginConfig config;

        //[JsonConverter(typeof(StringEnumConverter))]
        //public enum EnumType { none }

        public class PluginConfig
        {
            [JsonProperty("Настройка спавна")]
            public Dictionary<string, RTSetting> zoneSettings;
            [JsonProperty("Радиус маркера")]
            public float MarkerRadius;
            [JsonProperty("Часы дня когд ивент стартует")]
            public string[] StartTimes;
            [JsonProperty("Время до открытия ящика после спвна в минутах")]
            public float OpenTime;
            [JsonProperty("Список предметов выпадаемых из ящика")]
            public List<CustomItem> dropList;
            [JsonProperty("Время через которое ящик будет удален если не залутали до конца (в сек)")]
            public float DeletTime;
            [JsonProperty("Цвет маркера")]
            public string MarkerColor;
            [JsonProperty("Сообщение о начале ивента")]
            public string StartMessage;

            [JsonProperty("Сообщение об разблокировании {rt}")]
            public string UnlockMessage;

            [JsonProperty("Сообщение об открытии ящика {player}, {rt}")]
            public string OpenMessage;
        }

        public class RTSetting
        {
            [JsonProperty("Спавнить ящик на этом РТ?")]
            public bool Enable;
            [JsonProperty("Координаты точки спавна (команда /pos)")]
            public string Offset;
        }

        public class CustomItem
        {
            [JsonProperty("Шортнейм предмета")]
            public string ShortName;
            [JsonProperty("Кастомное имя предмета")]
            public string CustomName;
            [JsonProperty("Количество предмета Min")]
            public int AmountMin;
            [JsonProperty("Количество предмета Max")]
            public int AmountMax;
            [JsonProperty("SkinID предмета")]
            public ulong SkinID;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                zoneSettings = GetMonuments(),
                MarkerRadius = 50f,
                StartTimes = new string[]
                {
                    "12:00",
                    "14:00",
                    "16:00",
                    "18:00",
                    "20:00",
                },
                OpenTime = 10,
                DeletTime = 300,
                MarkerColor = "#942E1DFF",
                StartMessage = "Начался ивент МегаЯщик.\nМестоположение отмечено на карте",
                OpenMessage = "Мегаящик на {rt} был залутан игроком {player}",
                UnlockMessage = "Мегаящик на {rt} был разблокирован.",
                dropList = new List<CustomItem>()
                {
                    new CustomItem()
                    {
                        ShortName = "rifle.bolt",
                        AmountMin = 1,
                        AmountMax = 1,
                        SkinID = 0,
                        CustomName = ""
                    },
                    new CustomItem()
                    {
                        ShortName = "rifle.ak",
                        AmountMin = 1,
                        AmountMax = 1,
                        SkinID = 0,
                        CustomName = ""
                    },
                    new CustomItem()
                    {
                        ShortName = "sulfur",
                        AmountMin = 1000,
                        AmountMax = 2000,
                        SkinID = 0,
                        CustomName = ""
                    },
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        Dictionary<string, RTSetting> GetMonuments()
        {
            var monumentList = new Dictionary<string, RTSetting>();
            var monuments = TerrainMeta.Path.Monuments;
            foreach (var mon in monuments)
            {
                string monumentname = mon.displayPhrase.english;
                monumentname = monumentname.Replace("\n", "");
                if (string.IsNullOrEmpty(monumentname)) continue;
                if (!monumentList.ContainsKey(monumentname))
                    monumentList.Add(monumentname, new RTSetting()
                    {
                        Enable = false,
                        Offset = "0 0 0",
                    });
            }
            return monumentList;
        }
        EventData data;
        public class EventData
        {
            public int EventCount = 0;
        }

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name + "/Data"))
                data = Interface.Oxide.DataFileSystem.ReadObject<EventData>(Name + "/Data");
            else
                Interface.Oxide.DataFileSystem.WriteObject(Name + "/Data", data = new EventData());
        }

        private DateTime NextStart;
        private Dictionary<DateTime, TimeSpan> CalcNextRestartDict = new Dictionary<DateTime, TimeSpan>();
        private List<DateTime> EventTimes = new List<DateTime>();
        private void OnServerInitialized()
        {
            ins = this;
            LoadData();
            LoadImage();

            EventTimes = config.StartTimes.ToList().Select(date => DateTime.Parse(date)).ToList();
            GetNextRestart(EventTimes);
            timer.Every(60f, CheckStart);

        }

        
        #region ImageLibrary

        private string mainui1 = "https://imgur.com/XmJ8VdI.png";
        private string close = "https://imgur.com/MFS0gCS.png";
        
        
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        
        

        void LoadImage()
        {
            AddImage(mainui1, "fon");
            AddImage(close, "close");
        }
        
        #endregion
        

        [ConsoleCommand("testgive")]
        void testGiveGameStores()
        {
            string userID = "76561199129000011";
            GameStoresGive(userID);
            Puts("Всё ок");
        }
        
        void GameStoresGive(string userID)
        {
            int shopID = 43024;

           string SecretKey = "5047e570403ad2d3dee9f2356fa4ae44";
            int serverID = 32533;
            int Balance = 15;
            string descriptions = "Вы получили баланс за ОГРОМНЫЙ ящик";
            webrequest.Enqueue($"https://gamestores.ru/api?shop_id={shopID}&secret={SecretKey}&server={serverID}&action=moneys&type=plus&steam_id={userID}&amount={Balance}&mess={descriptions}", null, (i, s) =>
            {
                BasePlayer player = BasePlayer.Find(userID);
                if (i != 200) { }
                if (s.Contains("success"))
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    if (player == null) return;
                    return;
                }
                if (s.Contains("fail"))
                {
                    Puts($"Пользователь {userID} не авторизован в магазине");
                    if (player == null) return;
                }
            }, this);
        }


        void MainUi(BasePlayer player)
        {
            /*Puts("1");*/
            /*CuiHelper.DestroyUi(player, "mainui5");
            CuiHelper.DestroyUi(player, mainui);*/
            
            timer.Once(15f, () => { CuiHelper.DestroyUi(player, mainui); });
            timer.Once(15f, () => { CuiHelper.DestroyUi(player, "mainui5"); });
            
            CuiElementContainer container = new CuiElementContainer();
                
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.4379627", AnchorMax = "0.1723957 0.5490744" },
                Image = { FadeIn = 1f, Color = "0 0 0 0", }
            },  "Hud", mainui);
            
            container.Add(new CuiElement
            {
                Parent = "mainui5",
                FadeOut = 1f,
                //Name = mainui + "mainui5",
                Components =
                {
                    new CuiImageComponent { Png = GetImage("fon") , Material = "assets/icons/greyout.mat", },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "mainui5",
                FadeOut = 1f,
                //Name = mainui + "mainui5",
                Components =
                {
                    new CuiTextComponent { Text = "ОГРОМНЫЙ ЯЩИК", Color = HexToRustFormat("#CAD5DF"),  Align = TextAnchor.UpperLeft, FontSize = 13, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.07902735 0.5499993", AnchorMax = "0.8267483 0.833331" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "mainui5",
                FadeOut = 1f,
                //Name = mainui + "mainui5",
                Components =
                {
                    new CuiTextComponent { Text = "ВНИМАНИЕ! На сервере заспавнился ОГРОМНЫЙ ящик с ресурсами", Color = HexToRustFormat("#8E8E8E") ,Align = TextAnchor.UpperLeft, FontSize = 9, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.07902735 0.2250011", AnchorMax = "0.7234047 0.6416651" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "mainui5",
                FadeOut = 1f,
                //Name = mainui + "mainui5",
                Components =
                {
                    new CuiTextComponent { Text = "МЕСТОПОЛОЖЕНИЕ ОТМЕЧЕНО НА КАРТЕ", Color = HexToRustFormat("#CAD5DF") ,Align = TextAnchor.UpperLeft, FontSize = 9, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.02127674 0.01666876", AnchorMax = "0.8358668 0.1916654" }
                }
            });
            
            /*container.Add(new CuiElement
            {
                Parent = "mainui5",
                //Name = mainui + "mainui5",
                Components =
                {
                    new CuiImageComponent { Png = GetImage("close") , Material = "assets/icons/greyout.mat", Color = "0.5568628 0.4078431 0.454902 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.844985 0.7083331", AnchorMax = "0.9118547 0.8999998" }
                }
            });
            
            container.Add(new CuiButton()
            {
                Button = { Color = "0 0 0 0", Command = $"chat.say /closeui" },
                RectTransform = { AnchorMin = "0.844985 0.7083331", AnchorMax = "0.9118547 0.8999998" },
                Text = { Text = "" }
            }, "mainui5" );*/
            
            CuiHelper.AddUi(player, container);
        }
        
        private static string HexToRustFormat(string hex)
        { 
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        
        
        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/Data", data);

            //if (sphereEntity != null && !sphereEntity.IsDestroyed)
            //    sphereEntity?.Kill();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "mainui5");
                CuiHelper.DestroyUi(player, mainui);
            }

            if (crate != null && !crate.IsDestroyed)
                crate?.Kill();

            if (mapMarker != null && !mapMarker.IsDestroyed)
                mapMarker?.Kill();

            destroyCrate?.Destroy();
            downTimer?.Destroy();
        }

        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/Data", data);
        }

        void OnNewSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/Data", data = new EventData());
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;

            if (player.IsReceivingSnapshot)
            {
                timer.In(2f, () => OnPlayerConnected(player));
                return;
            }

            if (started)
            {
                mapMarker.SendUpdate();
            }
        }

        bool started = false;

        void CheckStart()
        {
            if (NextStart > DateTime.Now) return;
            if (started) return;
            StartEvent();
        }

        [ConsoleCommand("tst")]
        void tstCmd(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            StartEvent();
        }

        [ChatCommand("tst")]
        void Start(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            StartEvent();
        }
         
        void StartEvent()
        {
            started = true;

            if (crate != null && !crate.IsDestroyed)
                crate?.Kill();

            if (mapMarker != null && !mapMarker.IsDestroyed)
                mapMarker?.Kill();

            destroyCrate?.Destroy();
            downTimer?.Destroy();

            var monumentConfig = config.zoneSettings.Where(x => x.Value.Enable && TerrainMeta.Path.Monuments.Any(y => y.displayPhrase.english.Replace("\n", "") == x.Key)).ToDictionary(x => x.Key, y => y.Value);
            if (monumentConfig.Count() <= 0)
            {
                started = false;
                PrintError($"НЕ НАЙДЕНО НЕ ОДНОГО ПОДХОДЯЩЕГО РТ ДЛЯ ЗАПУСКА ИВЕНТА");
                EventTimes = config.StartTimes.ToList().Select(date => DateTime.Parse(date)).ToList();
                GetNextRestart(EventTimes);
                return;
            }

            var monNames = monumentConfig.Keys.ToList().GetRandom();
            var mon = TerrainMeta.Path.Monuments.FirstOrDefault(x => x.displayPhrase.english.Replace("\n", "") == monNames);
            if (mon == null)
            {
                started = false;
                PrintError($"НЕ УДАЛОСЬ НАЙТИ РТ {monNames} НА КАРТЕ");
                EventTimes = config.StartTimes.ToList().Select(date => DateTime.Parse(date)).ToList();
                GetNextRestart(EventTimes);
                return;
            }
            data.EventCount++;
            Vector3 localPoint = config.zoneSettings[monNames].Offset.ToVector3();
            var pos = mon.transform.position + mon.transform.rotation * localPoint;
            RtName = monNames;
            PrintWarning($"Event Started on: {RtName}");
            SpawnCrate(pos);
        }

        string RtName = "";
        MapMarkerGenericRadius mapMarker;

        StorageContainer crate;

        void SpawnCrate(Vector3 pos)
        {
            if (crate != null && !crate.IsDestroyed)
                crate.Kill();

            crate = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab", pos) as StorageContainer;
            crate.globalBroadcast = true;
            crate.skinID = 12521454;
            crate.name = "RTEvent";
            
            crate.Spawn();
            crate.panelName = "largewoodbox";
            crate.inventory.Clear();
            crate.inventory.ServerInitialize(null, config.dropList.Count);

            mapMarker = (MapMarkerGenericRadius)GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", pos, new Quaternion());
            mapMarker.enableSaving = false;
            mapMarker.globalBroadcast = true;
            mapMarker.Spawn();
            mapMarker.radius = config.MarkerRadius / 146.3f;
            mapMarker.alpha = 1f;
            UnityEngine.Color color = hexToColor(config.MarkerColor);
            UnityEngine.Color color2 = new UnityEngine.Color(0, 0, 0, 0);
            mapMarker.color1 = color;
            mapMarker.color2 = color2;
            mapMarker.SendUpdate();
             
            timer.In(1f, () =>
            {
                foreach (var item in config.dropList)
                {
                    var amount = UnityEngine.Random.Range(item.AmountMin, item.AmountMax + 1);
                    var it = ItemManager.CreateByName(item.ShortName, amount, item.SkinID);
                    if (!string.IsNullOrEmpty(item.CustomName))
                        it.name = item.CustomName;

                    it.MoveToContainer(crate.inventory);
                }
                crate.SetFlag(BaseEntity.Flags.Locked, true);
                lockedTime = (int)Math.Ceiling(config.OpenTime * 60);
                downTimer = timer.Every(1f, TimerDown);
                PrintToChat(config.StartMessage);
                //BasePlayer player = BasePlayer.activePlayerList.ToList();
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                MainUi(player);
                crate.SendNetworkUpdate();
            });
        }
        Timer destroyCrate;
        Timer downTimer;
        int lockedTime = 0;
        void TimerDown()
        {
            lockedTime--;
            if (lockedTime <= 0)
            {
                PrintToChat(config.UnlockMessage.Replace("{rt}", RtName));

                crate.SetFlag(BaseEntity.Flags.Locked, false);
                downTimer?.Destroy();
            } 
        }
        
        
        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == crate && container.HasFlag(BaseEntity.Flags.Locked))
            {
                SendReply(player, $"ОГРОМНЫЙ ящик закрыт! Он откроется через: {TimeSpan.FromSeconds(lockedTime).ToShortString()}");
                return null;
            }
            if (container == crate && !container.HasFlag(BaseEntity.Flags.Reserved10))
            {
                PrintToChat(config.OpenMessage.Replace("{rt}", RtName).Replace("{player}", player.displayName));
                GameStoresGive(player.UserIDString);
                BasePlayer.allPlayerList.Select(p => NoteUI?.Call("DrawInfoNote", p,
                    $"<color=#8e6874>ОГРОМНЫЙ</color> ящик на {RtName} был залутан игроком <color=#8e6874>{player.displayName}</color>"));
                CuiHelper.DestroyUi(player, "mainui5");
                container.SetFlag(BaseEntity.Flags.Reserved10, true);
            }
            return null;
        }

        [ChatCommand("testui")]
        void testui(BasePlayer player)
        {
            MainUi(player);
        }
        
        [ChatCommand("closeui")]
        void closeui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "mainui5");
            CuiHelper.DestroyUi(player, mainui);
        }

        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity container)
        {
            if (started && crate != null && container != null && container.GetComponent<StorageContainer>() != null && container.GetComponent<StorageContainer>() == crate)
            {
                var storage = container.GetComponent<StorageContainer>();
                NextTick(() => CheckRespawn(storage));
            }
        } 
         
        private void CheckRespawn(StorageContainer container)
        {
            var isEmpty = container == null || container.inventory?.itemList == null || container.inventory.itemList.Count <= 0;
              
            if (!isEmpty)
            {
                if (destroyCrate != null)
                    destroyCrate?.Destroy();

                destroyCrate = timer.In(config.DeletTime, EventEnd);
                return;
            }
            EventEnd();
             
        }
        void EventEnd()
        {
            PrintWarning($"ИВЕНТ ЗАКОНЧИЛСЯ.");
            started = false;

            if (crate != null && !crate.IsDestroyed)
                crate.Kill();

            if (mapMarker != null && !mapMarker.IsDestroyed)
                mapMarker.Kill();

            destroyCrate?.Destroy();
            downTimer?.Destroy();

            GetNextRestart(EventTimes);
        }

        void GetNextRestart(List<DateTime> DateTimes)
        {
            EventTimes = config.StartTimes.ToList().Select(date => DateTime.Parse(date)).ToList();
            CalcNextRestartDict.Clear();
            var e = DateTimes.GetEnumerator();
            for (var i = 0; e.MoveNext(); i++)
            {
                if (DateTime.Compare(DateTime.Now, e.Current) < 0)
                {
                    CalcNextRestartDict.Add(e.Current, e.Current.Subtract(DateTime.Now));
                }
                if (DateTime.Compare(DateTime.Now, e.Current) > 0)
                {
                    CalcNextRestartDict.Add(e.Current.AddDays(1), e.Current.AddDays(1).Subtract(DateTime.Now));
                }
            }
            NextStart = CalcNextRestartDict.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            CalcNextRestartDict.Clear();
            PrintWarning("Следующий старт через " + NextStart.Subtract(DateTime.Now).ToShortString() + " в " + NextStart.ToLongTimeString());
        }

        [ChatCommand("poscrate")]
        void CheckPos(BasePlayer player)
        {
            if (!player.IsAdmin) return;

            var mon = TerrainMeta.Path.Monuments.Find(x => Vector3.Distance(player.transform.position, x.transform.position) < 100);
            string msg = $"{mon.displayPhrase.english} {mon.transform.worldToLocalMatrix.MultiplyPoint(player.transform.position)}\nСообщение дублируется в консоль.";
            SendReply(player, msg);
            Puts(msg);
        }

        public static UnityEngine.Color hexToColor(string hex)
        {
            hex = hex.Replace("0x", "");
            hex = hex.Replace("#", "");
            byte a = 160;
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r, g, b, a);
        }
    }
}
