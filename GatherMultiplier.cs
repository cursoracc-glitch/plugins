// Reference: System.Drawing
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Gather Multiplier", "RustPlugin.ru", "0.1.0")]

    public class GatherMultiplier : RustPlugin
    {
        #region CLASSES
        public static int START_TIME;
        public Dictionary<int, int> BONUSES;
        public Dictionary<string, int> BONUSMULTIPLIER;
        
        public class GatherData
        {
            public int Time = START_TIME;
            public int TotalAmount = 0;
            public string shortname;
            public int amount;
        }

        #endregion
        
        #region VARIABLES
        
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("GatherMultiplier");
        Dictionary<BasePlayer, int> notifierLasthit = new Dictionary<BasePlayer, int>();
        Dictionary<BasePlayer, int> bonuses = new Dictionary<BasePlayer, int>();

        Dictionary<string, string> itemsLoaclization = new Dictionary<string, string>()
        {
            {"hq.metal.ore", "МВК РУДА"},
            {"metal.ore", "ЖЕЛЕЗНАЯ РУДА"},
            {"sulfur.ore", "СЕРНАЯ РУДА"},
            {"stones", "КАМНИ"},
            {"metal.fragments", "МЕТАЛЛ. ФРАГ." },
            {"charcoal", "УГОЛЬ" },
            {"metal.refined", "МВК" }
        };

        Dictionary<int, int> gatherBonuses = new Dictionary<int, int>();
        Dictionary<BasePlayer, GatherData> gathers = new Dictionary<BasePlayer, GatherData>();

        protected override void LoadDefaultConfig()
        {
            Config["Стартовое время"] = START_TIME = GetConfig(80, "Стартовое время");
            Config["Бонус за непрерывную выдачу (Количество: Бонус)"] = BONUSES = GetConfig(new Dictionary<int, int>() { {100,100}}, "Бонус за непрерывную выдачу (Количество: Бонус)");
            Config["Бонусный множитель"] = BONUSMULTIPLIER = GetConfig(new Dictionary<string, int>()
            {
                {"hq.metal.ore", 1},
                {"metal.ore", 20},
                {"sulfur.ore", 10},
                {"stones", 30}
            }, "Бонусный множитель");
            
            SaveConfig();
        }
        
        #endregion

        #region Oxide Hooks

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            timer.Every(1, GatherTimerLoop);
            timer.Every(1, BonusTimerLoop);
            InitFileManager();
            CommunityEntity.ServerInstance.StartCoroutine(LoadImages());
        }

        void OnPluginLoaded(Plugin name)
        {
            if (name.ToString() == "ExtPlugin" && name.Author == "Sanlerus, Moscow.OVH")
            {
                Unsubscribe("OnDispenserGather");
                Subscribe("OnDispenserGather");
            }
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
                DestroyUILoop(player);
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            notifierLasthit.Remove(player);
            gathers.Remove(player);
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;
            var player = entity.ToPlayer();
            var gatherType = dispenser.gatherType.ToString("G");

            if (gatherType == "Ore")
            {
                GatherData data;
                if (!gathers.TryGetValue(player, out data))
                    gathers.Add(player, data = new GatherData());
                var lastAmount = data.TotalAmount;
                float del = 1;
                
                var am = (int) (item.amount/del);
                data.TotalAmount += am;
                int bonusKey;
                if (GetBonus(lastAmount, data.TotalAmount, out bonusKey))
                    GiveBonus(player, bonusKey);
                data.amount = am;
                data.shortname = item.info.shortname;
                data.Time = START_TIME;
                UIDrawNotifier(player, data);
                DestroyUILoop(player);
                timer.Destroy(ref mytimer);
                UIDrawNotifierLast(player, data);
                
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is BasePlayer))return;
            var player = (BasePlayer)entity;
            gathers.Remove(player);
            CuiHelper.DestroyUi(player, "gatheradvanced_panel");
        }

        void UpdateTimer(BasePlayer player, GatherData data)
        {
            data.Time = START_TIME;
            UIDrawNotifier(player, data);

        }
        #endregion

        #region Timers

        void GatherTimerLoop()
        {
            List<BasePlayer> removeList = new List<BasePlayer>();
            foreach (var gatherPair in gathers)
            {
                var data = gatherPair.Value;
                data.Time--;
                if (data.Time >= 0)
                {
                    UIDrawNotifier(gatherPair.Key, data, false);
                    continue;
                }
                removeList.Add(gatherPair.Key);
                DestroyUI(gatherPair.Key);
            }
            foreach (var p in removeList)
                gathers.Remove(p);
        }

        void BonusTimerLoop()
        {
            var time = Time;
            var removeList = (from bonusPair in bonuses
                              where bonusPair.Value <= time
                              select bonusPair.Key).ToList();
            for (int i = removeList.Count - 1; i >= 0; i--)
            {
                var player = removeList[i];
                CuiHelper.DestroyUi(player, "gatheradvanced_bonusParent");
                CuiHelper.DestroyUi(player, "gatheradvanced_bonusPanel");
                CuiHelper.DestroyUi(player, "gatheradvanced_bonusText");
                bonuses.Remove(player);
            }
        }

        void NotifierLasthitLoop()
        {
            var time = Time;
            List<BasePlayer> removeList = (from lasthitPair in notifierLasthit where lasthitPair.Value <= time select lasthitPair.Key).ToList();
            for (int i = removeList.Count - 1; i >= 0; i--)
            {
                var player = removeList[i];
                notifierLasthit.Remove(player);
            }
        }

        #endregion

        #region FUNCTIONS
        
        Dictionary<string,int> itemIDS = new Dictionary<string, int>()
        {
            {"hq.metal.ore", -1982036270},
            {"metal.ore", -4031221},
            {"sulfur.ore", -1157596551},
            {"stones", -2099697608}
        };

        void GiveBonus(BasePlayer player, int bonusKey)
        {
            var bonusAmount = BONUSES[bonusKey];
            var bonusType = GetBonusType();
            int amount = bonusAmount*(int) BONUSMULTIPLIER[bonusType];
            Item item = ItemManager.CreateByItemID(itemIDS[bonusType], amount);
            //Puts(player.displayName + ": "+item.info.shortname + " " + item.amount);
            player.inventory.GiveItem(item);
            UIDrawBonus(player, bonusKey, $"{amount} {itemsLoaclization[item.info.shortname]}");
        }

        public string GetBonusType() => BONUSMULTIPLIER.Keys.ToList()[UnityEngine.Random.Range(0, BONUSMULTIPLIER.Count)];

        public bool GetBonus(int lastAmount, int newAmount, out int bonusKey)
        {
            bonusKey = -1;
            foreach (var bonus in BONUSES)
                if (lastAmount < bonus.Key && newAmount >= bonus.Key)
                {
                    bonusKey = bonus.Key;
                    return true;
                }
            return false;
        }

        #endregion

        #region UI

        void UIDrawNotifier(BasePlayer player, GatherData data, bool destroy = true)
        {
            DestroyUI(player);
            if (destroy && data.amount != data.TotalAmount)
            {
                DestroyUI(player);
                notifierLasthit[player] = Time + 2;
            }
            if (!itemsLoaclization.ContainsKey(data.shortname))
                Puts("Invalid item: "+data.shortname);
            CuiHelper.AddUi(player,
                          HandleArgs(GUINot, data.TotalAmount, data.Time));
            
        }

        public Timer mytimer;

        void UIDrawNotifierLast(BasePlayer player, GatherData data, bool destroy = true)
        {
            
            
            if (destroy && data.amount != data.TotalAmount)
            {
                notifierLasthit[player] = Time + 2;
            }
            if (!itemsLoaclization.ContainsKey(data.shortname))
                Puts("Invalid item: " + data.shortname);
            mytimer = timer.Once(5, () =>
            {
                DestroyUILoop(player);
            });
            CuiHelper.AddUi(player,
                          HandleArgs(GUILastHit, data.amount, itemsLoaclization[data.shortname], Images["Notify"]));
           
        }

        int Time => (int)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        void UIDrawBonus(BasePlayer player, int bonusKey, string item)
        {
            var bonusAmount = BONUSES[bonusKey];
            if (bonuses.ContainsKey(player))
            {
                CuiHelper.DestroyUi(player, "gatheradvanced_bonusParent");
                CuiHelper.DestroyUi(player, "gatheradvanced_bonusPanel");
                CuiHelper.DestroyUi(player, "gatheradvanced_bonusText");
                DestroyUI(player);
            }
            CuiHelper.AddUi(player,
                          HandleArgs(GUI, bonusKey.ToString(), item.ToString()));
            timer.Once(3, () => CuiHelper.DestroyUi(player, "gatheradvanced_bonusParent"));
            bonuses[player] = Time + 4;
        }

        string HandleArgs(string json, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                json = json.Replace("{" + i + "}", args[i].ToString());
            return json;
        }
        Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            { "Notify", "http://i.imgur.com/VQAmjJ1.png" },
        };

        string GUI = "[{\"name\":\"gatheradvanced_bonusParent\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.6392157 0.6156863 0.6156863 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3421874 0.1194445\",\"anchormax\":\"0.6401041 0.2231482\"}],\"fadeOut\":0.5},{\"name\":\"gatheradvanced_bonusPanel\",\"parent\":\"gatheradvanced_bonusParent\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.6418388 0.6161654 0.6161654 0.1849999\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.005244754 0\",\"anchormax\":\"1 1\"}],\"fadeOut\":0.5},{\"name\":\"gatheradvanced_bonusText\",\"parent\":\"gatheradvanced_bonusParent\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<color=#fec384>Поздравляем!\nБонус за непрерывную добычу <color=#d2722d>{0}</color> ресурсов</color>\n<color=#d2722d>+{1}</color>\",\"fontSize\":17,\"align\":\"MiddleCenter\",\"color\":\"0 0 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.01785707\",\"anchormax\":\"1 1\"}],\"fadeOut\":0.5}]";
        string GUINot = "[{\"name\":\"gatheradvanced_panel\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.6156863 0.5921569 0.5882353 0.095\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.725 0.02222222\",\"anchormax\":\"0.8359375 0.0972222\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"gatheradvanced_timer\",\"parent\":\"gatheradvanced_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Время: <color=#d2722d>{1}</color> секунд\",\"fontSize\":15,\"align\":\"MiddleCenter\",\"color\":\"0.9960784 0.7647059 0.5176471 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009375773 0\",\"anchormax\":\"1 0.5061733\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"gatheradvanced_total\",\"parent\":\"gatheradvanced_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Всего: <color=#d2722d>{0}</color>\",\"fontSize\":15,\"align\":\"MiddleCenter\",\"color\":\"0.9960784 0.7647059 0.5176471 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009389528 0.5185185\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";
        string GUILastHit = "[{\"name\":\"gatheradvanced_lastHitPanel\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{2}\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0.7252604 0.1000001\",\"anchormax\":\"0.8359374 0.1351852\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}],\"fadeOut\":1.0},{\"name\":\"gatheradvanced_lastHitCount\",\"parent\":\"gatheradvanced_lastHitPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b><color=#4e6031>+{0}</color></b>\",\"fontSize\":13,\"align\":\"MiddleLeft\",\"color\":\"0.9176471 1 0.4 1\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0.01882368 0\",\"anchormax\":\"0.2235298 0.9999994\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}],\"fadeOut\":1.0},{\"name\":\"gatheradvanced_lastHitName\",\"parent\":\"gatheradvanced_lastHitPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b><color=#4e6031>{1}</color></b>\",\"fontSize\":13,\"align\":\"MiddleCenter\",\"color\":\"0.7372549 0.8 0.3215686 1\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0.2870598 0\",\"anchormax\":\"1 0.9999994\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}],\"fadeOut\":1.0}]";
        
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "gatheradvanced_panel");
            
        }

        void DestroyUILoop(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "gatheradvanced_lastHitPanel");
            CuiHelper.DestroyUi(player, "gatheradvanced_lastHitCount");
            CuiHelper.DestroyUi(player, "gatheradvanced_lastHitName");

        }
        #endregion

        #region CONFIG

        T GetConfig<T>(T defaultValue, string firstKey, string secondKey = null, string thirdKey = null)
        {
            try
            {
                object value;

                // get the value associated with the provided keys
                if(thirdKey != null)
                {
                    value = Config[firstKey, secondKey, thirdKey];
                }
                else if(secondKey != null)
                {
                    value = Config[firstKey, secondKey];
                }
                else
                {
                    value = Config[firstKey];
                }

                // if the value is a dictionary, add the key/value pairs to a dictionary and return it
                // this particular implementation only handles dictionarys with string key/value pairs
                if(defaultValue.GetType() == typeof(Dictionary<string,int>))           // checks if the value is a dictionary
                {
                    Dictionary<string, int> valueDictionary = Config.ConvertValue<Dictionary<string, int>>(value);
                    
                    return (T)Convert.ChangeType(valueDictionary, typeof(T));
                }
                if(defaultValue.GetType() == typeof(Dictionary<int, int>))           // checks if the value is a dictionary
                {
                    Dictionary<string, int> valueDictionary =Config.ConvertValue<Dictionary<string,int>>(value);
                    Dictionary<int, int> values = valueDictionary.Keys.ToDictionary(int.Parse, key => (int) valueDictionary[key]);

                    return (T)Convert.ChangeType(values, typeof(T));
                }
                // if the value is a list, add the list elements to a list and return it
                // this particular implementation only handles lists with char elements
                else if(value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(List<>))             // checks if the value is a list
                {
                    IList valueList = (IList)value;
                    List<char> values = new List<char>();

                    foreach(object obj in valueList)
                    {
                        if(obj is string)
                        {
                            char result;
                            if(char.TryParse((string)obj, out result))
                            {
                                values.Add(result);
                            }
                        }
                    }
                    return (T)Convert.ChangeType(values, typeof(T));
                }
                // handles every other type
                else
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch(Exception)
            {
                return defaultValue;
            }
        }

        #endregion

        #region File Manager
     


        IEnumerator LoadImages()
        {
            foreach (var imgKey in Images.Keys.ToList())
            {
                yield return CommunityEntity.ServerInstance.StartCoroutine(
                    m_FileManager.LoadFile(imgKey, Images[imgKey]));
                Images[imgKey] = m_FileManager.GetPng(imgKey);
            }
        }

        private GameObject FileManagerObject;
        private FileManager m_FileManager;

        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject("MAP_FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;
            
            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

            private class FileInfo
            {
                public string Url;
                public string Png;
            }


            public string GetPng(string name) => files[name].Png;


            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                var reply = 710;
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url, size));
            }

            IEnumerator LoadImageCoroutine(string name, string url, int size = -1)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        var bytes = size == -1 ? www.bytes : Resize(www.bytes, size);


                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId).ToString();
                        files[name].Png = crc32;
                    }
                }
                loaded++;
            }

            static byte[] Resize(byte[] bytes, int size)
            {
                Image img = (Bitmap)(new ImageConverter().ConvertFrom(bytes));
                Bitmap cutPiece = new Bitmap(size, size);
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(cutPiece);
                graphic.DrawImage(img, new Rectangle(0, 0, size, size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
                graphic.Dispose();
                MemoryStream ms = new MemoryStream();
                cutPiece.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        #endregion

        #region PErmission
        

        
        #endregion
    }
}
           