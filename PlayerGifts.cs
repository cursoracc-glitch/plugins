// Автор плагина FuzeEffect 
// Версия плагина 1.0.214
// Группа по разработке приватных плагинов - vk.com/skyeyeplugins
// Слив,перепродажа,подарок,перекупка или обмен караются БАНОМ И САНКЦИЯМИ,которые будут применены к вам!
// Уважайте разработчиков и не распространяйте данный плагин! Если такое не было обговорено!
// Приятного пользования!
// Плагин обфусцирован дабы защитить себя! На оптимизацию и работоспособность это не влияет!
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{

    [Info("PlayerGifts", "FuzeEffect", "1.0.104")]
    public class PlayerGifts : RustPlugin
    {
        protected override void SaveConfig() => Config.WriteObject(uuu);
        [Oxide.Core.Plugins.HookMethod("OnPlayerDisconnected")]
        void ooo(BasePlayer players, string aaa)
        {
            sss(players, aaa);
        }
        public List<ulong> ddd = new List<ulong>();
        public double fff = ggg();
        static string yyy = "XCC_MAINPANELGIFT214";
        void Gui(BasePlayer players)
        {
            CuiHelper.DestroyUi(players, Ui);
            CuiElementContainer Gui = new CuiElementContainer();
            Gui.Add(new CuiPanel { CursorEnabled = true, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, Image = { Color = uuu.xxx.ccc.vvv, Sprite = "assets/content/ui/ui.background.transparent.radial.psd", Material = "assets/content/ui/uibackgroundblur.mat" } }, "Overlay", Ui);
            Gui.Add(new CuiButton { RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" }, Button = { Close = Ui, Color = "0 0 0 0" }, Text = { FadeIn = 0.8f, Text = "" } }, Ui);
            Gui.Add(new CuiLabel { RectTransform = { AnchorMin = "0 0.9138894", AnchorMax = "0.981482 1", OffsetMax = "0 0" }, Text = { Text = uuu.xxx.ccc.bbb, Font = uuu.xxx.ccc.nnn, Align = TextAnchor.MiddleCenter } }, Ui);
            Gui.Add(new CuiLabel { RectTransform = { AnchorMin = "0 0.8962963", AnchorMax = "1 0.9324074", OffsetMax = "0 0" }, Text = { Text = uuu.xxx.ccc.mmm, Font = uuu.xxx.ccc.nnn, Align = TextAnchor.MiddleCenter } }, Ui);
            Gui.Add(new CuiPanel { FadeOut = 0.5f, RectTransform = { AnchorMin = "0.08958333 0.2629631", AnchorMax = "0.9520833 0.8916668" }, Image = { FadeIn = 0.5f, Color = "0 0 0 0" } }, Ui, "InventoryPanel");
            for (int i = 0, x = 0, y = 0;
            i < 36;
            i++)
            {
                Gui.Add(new CuiPanel { RectTransform = { AnchorMin = $"{0 + (x * uuu.xxx.ccc.qww)} {0.7805594 - (y * uuu.xxx.ccc.qee)}", AnchorMax = $"{0.09722221 + (x * uuu.xxx.ccc.qww)} {0.995 - (y * uuu.xxx.ccc.qee)}" }, Image = { Color = uuu.xxx.ccc.qrr, Sprite = "assets/content/ui/ui.background.transparent.radial.psd", Material = "assets/content/ui/uibackgroundblur.mat" } }, "InventoryPanel", $"Slot_{i }");
                x++;
                if (x >= 9)
                {
                    x = 0;
                    y++;
                }
                if (x >= 9 && y >= 4) break;
            }
            for (int i = 0, x = 0, y = 0;
          i < qtt[players.userID].qqq.Count;
          i++)
            {
                string qyy = string.IsNullOrEmpty(qtt[players.userID].qqq.ElementAt(i).Value) ? "Coins" : qtt[players.userID].qqq.ElementAt(i).Value;
                Gui.Add(new CuiElement { Parent = $"Slot_{i }", Name = "ItemInventory", Components = { new CuiRawImageComponent { Png = quu(qyy), }, new CuiRectTransformComponent { AnchorMin = $"0.08 0.04", AnchorMax = $"0.9 0.9" }, } });
                Gui.Add(new CuiButton { RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, Button = { Command = $"TakeItem {i }", Color = "0 0 0 0", Sprite = "assets/content/ui/ui.background.transparent.radial.psd", }, Text = { Text = qtt[players.userID].qqq.ElementAt(i).Amount.ToString() + "шт", Align = TextAnchor.LowerCenter, FontSize = 17, Font = uuu.xxx.ccc.nnn } }, "ItemInventory");
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
                if (x == 10 && y == 4) break;
            }
            Gui.Add(new CuiButton { RectTransform = { AnchorMin = "0.410417 0.2064815", AnchorMax = "0.5901042 0.2638889" }, Button = { Command = "TakeAll", Color = qcc("#0000005D") }, Text = { Text = "Забрать все", Align = TextAnchor.MiddleCenter, FontSize = 23, Font = uuu.xxx.ccc.nnn } }, Ui);
            CuiHelper.AddUi(players, Gui);
        }
        [Oxide.Core.Plugins.HookMethod("OnPlayerInit")]
        void tjj(BasePlayer p)
        {
            wtt(p);
        }

        [Oxide.Core.Plugins.HookMethod("OnServerSave")]
        void tgg()
        {
            rmm();
        }

        [ConsoleCommand("CheckGift")]
        void qnn(ConsoleSystem.Arg arg)
        {
            BasePlayer players = arg.Player();
            if (qtt[players.userID].qqq.Count >= 39)
            {
                ttt(players, lang.GetMessage("inventorypgfull", this));
                return;
            }
            qtt[players.userID].TimeGame = 0;
            wqq(players);
            var qpp = uuu.qaa.ElementAt(UnityEngine.Random.Range(0, uuu.qaa.Count));
            var qss = qtt[players.userID].qqq;
            qss.Add(new qdd.qff { Value = qpp.qgg, Amount = qpp.qhh, Url = qpp.qjj });
            ttt(players, lang.GetMessage("rewardgive", this));
            ddd.Remove(players.userID);
        }
        void ttt(BasePlayer players, string qll)
        {
            CuiHelper.DestroyUi(players, qzz);
            CuiElementContainer qxx = new CuiElementContainer();
            qxx.Add(new CuiPanel { RectTransform = { AnchorMin = "0.3291668 0.8583333", AnchorMax = "0.6614581 0.9166667" }, Image = { FadeIn = 0.4f, Color = qcc(uuu.xxx.ccc.tkk) } }, "Overlay", qzz);
            qxx.Add(new CuiLabel { RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, Text = { Text = String.Format(qll), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = qcc("#FFFFFFFF") } }, qzz);
            CuiHelper.AddUi(players, qxx);
            timer.Once(2f, () => {
                CuiHelper.DestroyUi(players, qzz);
            });
        }
        void sss(BasePlayer players, string qbb) => ddd.Remove(players.userID);
        void wqq(BasePlayer players)
        {
            CuiHelper.DestroyUi(players, yyy);
            CuiElementContainer wzz = new CuiElementContainer();
            string tll = qtt[players.userID].TimeGame >= uuu.wee ? "ActivePng" : "InactivePng";
            wzz.Add(new CuiElement { Parent = "Overlay", Name = yyy, Components = { new CuiRawImageComponent { Png = quu(tll), }, new CuiRectTransformComponent { AnchorMin = uuu.xxx.wjj.whh, AnchorMax = uuu.xxx.wjj.eyy, OffsetMin = uuu.xxx.wjj.wkk, OffsetMax = uuu.xxx.wjj.wll }, } });
            if (qtt[players.userID].TimeGame >= uuu.wee)
            {
                wzz.Add(new CuiButton { RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, Button = { Command = "CheckGift", Color = "0 0 0 0" }, Text = { Text = "" } }, yyy);
            }
            CuiHelper.AddUi(players, wzz);
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                uuu = Config.ReadObject<rtt>();
                if (uuu?.qaa == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name }', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        static double ggg() => DateTime.UtcNow.Subtract(wxx).TotalSeconds;
        public void wtt(BasePlayer players)
        {
            if (players.IsReceivingSnapshot)
            {
                NextTick(() => wtt(players));
                return;
            }
            if (!qtt.ContainsKey(players.userID))
            {
                qdd wuu = new qdd() { PlayerAuthTime = fff, TimeGame = 0.0, qqq = new List<qdd.qff> { } };
                qtt.Add(players.userID, wuu);
            }
            wqq(players);
            wcc(players);
        }
        private new void lans()
        {
            PrintWarning("Языковой файл загружается...");
            timer.In(2.5f, () => {
                Dictionary<string, string> woo = new Dictionary<string, string> { ["takeallnull"] = "У вас ничего нет", ["takeallinventory"] = "Вы успешно забрали весь инвентарь", ["inventoryfull"] = "Ваш инвентарь полон,награда выброшена под ноги!", ["inventorypgfull"] = "Ваш инвентарь полон,освободите слоты чтобы получить награду", ["takeitem"] = "Вы успешно забрали награду", ["rewardgive"] = "Вы успешно получили награду", ["noauth"] = "Для того чтобы получить баланс вы должны быть авторизованы в магазине!", };
                lang.RegisterMessages(woo, this, "en");
                PrintWarning("Языковой файл загружен успешно");
            });
        }
        public string quu(string wpp, ulong waa = 0) => (string)ImageLibrary?.Call("GetImage", wpp, waa);

        [ChatCommand("pg")]
        void wss(BasePlayer players)
        {
            Gui(players);
        }
        [Oxide.Core.Plugins.HookMethod("Unload")]
        void wff()
        {
            wgg();
        }
        public Dictionary<ulong, qdd> qtt = new Dictionary<ulong, qdd>();
        void rmm() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("PlayerGifts/PlayerTimer", qtt);
        private static string qcc(string tzz)
        {
            if (string.IsNullOrEmpty(tzz))
            {
                tzz = "#FFFFFFFF";
            }
            var wbb = tzz.Trim('#');
            if (wbb.Length == 6) wbb += "FF";
            if (wbb.Length != 8)
            {
                throw new Exception(tzz);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var thh = byte.Parse(wbb.Substring(0, 2), NumberStyles.HexNumber);
            var wvv = byte.Parse(wbb.Substring(2, 2), NumberStyles.HexNumber);
            var txx = byte.Parse(wbb.Substring(4, 2), NumberStyles.HexNumber);
            var wnn = byte.Parse(wbb.Substring(6, 2), NumberStyles.HexNumber);
            Color wmm = new Color32(thh, wvv, txx, wnn);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", wmm.r, wmm.g, wmm.b, wmm.a);
        }
        private class rtt
        {
            internal class tff
            {
                [JsonProperty("Настройка UI инвентаря")] public eqq ccc = new eqq();
                [JsonProperty("Настройка UI лого")] public eww wjj = new eww();
                internal class eww
                {
                    [JsonProperty("Не активная картинка(Будет показана если игрок не отыграл определенное время(ссылка)")] public string err = "https://i.imgur.com/WmLsDv1.png";
                    [JsonProperty("Aктивная картинка(Будет показана если игрок  отыграл определенное время(ссылка)")] public string ett = "https://i.imgur.com/CgvNwaS.png";
                    [JsonProperty("AnchorMin для иконки(для опытных юзеров)")] public string whh = "0.5 0.5";
                    [JsonProperty("AnchorMax для иконки(для опытных юзеров)")] public string eyy = "0.5 0.5";
                    [JsonProperty("OffsetMin для иконки(для опытных юзеров)")] public string wkk = "-260 -341";
                    [JsonProperty("OffsetMax для иконки(для опытных юзеров)")] public string wll = "-200 -282";
                }
                internal class eqq
                {
                    [JsonProperty("Настройка цвета ячеек в инвентаре")] public string qrr = "0 0 0 0.7";
                    [JsonProperty("Цвет UI с сообщением")] public string tkk = "#eb678a";
                    [JsonProperty("Текст в инвентаре")] public string bbb = "<size=30>Ваш инвентарь вещей за проведенное время на сервере</size>";
                    [JsonProperty("Описание в инвентаре")] public string mmm = "<size=18>Вы можете забрать вещи из инвентаря в любое время</size>";
                    [JsonProperty("Отступы для ячейки (Y)")] public double qee = 0.23;
                    [JsonProperty("Отступы для ячейки (X)")] public double qww = 0.105;
                    [JsonProperty("Настройка цвета для заднего фона инвентаря")] public string vvv = "0 0 0 0.7";
                    [JsonProperty("Шрифт текста в инвентаре")] public string nnn = "robotocondensed-bold.ttf";
                }
            }
            internal class tcc
            {
                [JsonProperty("API от Магазина(Секретный ключ)")] public string tww = "SecretKey";
                [JsonProperty("ServerID в магазине")] public string tyy = "ServerID";
                [JsonProperty("Сообщение при получении баланса(отображается в магазине)")] public string rrr = "Вы получили баланс за проведенное время на сервере!";
            }
            internal class tbb
            {
                [JsonProperty("Предмет из игры или команда. (Если вы ставите предмет из игры(Пример: rifle.ak) не заполняйте URL")] public string qgg;
                [JsonProperty("Ссылка на фото для команды или денешки")] public string qjj;
                [JsonProperty("Значение,сколько предметов вам дадут!(Если оставить Value пустым,выдадут баланс на GameStores)")] public int qhh;
            }
            [JsonProperty("Настройка магазина")] public tcc eee = new tcc();
            [JsonProperty("Список предметов! Когда игрок отыграет определенное время на сервере,ему дадут 1 награду из списка.")] public List<tbb> qaa = new List<tbb>();
            [JsonProperty("Настройка UI плагина")] public tff xxx = new tff();
            [JsonProperty("Сколько времени нужно отыграть игроку для получения награды (секунды)")] public ulong wee = 300;
            public static rtt tvv()
            {
                return new rtt { qaa = new List<tbb> { new tbb { qgg = "rifle.ak", qjj = "", qhh = 1 }, new tbb { qgg = "addgroup %STEAMID% vip 3d", qjj = "https://i.imgur.com/lZZeCob.png", qhh = 1 }, new tbb { qgg = "", qjj = "https://i.imgur.com/OUgxtD7.png", qhh = 150 } } };
            }
        }
        private void tpp()
        {
            PrintError($"-----------------------------------");
            PrintError($"            PlayerGifts            ");
            PrintError($"          Created - Sky Eye        ");
            PrintError($"      Author = FuzeEffect#5212     ");
            PrintError($"    https://vk.com/skyeyeplugins   ");
            PrintError($"-----------------------------------");
            qtt = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, qdd>>("PlayerGifts/PlayerTimer");
            lans();
            rww(uuu.xxx.wjj.err, "InactivePng");
            rww(uuu.xxx.wjj.ett, "ActivePng");
            foreach (var configItem in uuu.qaa)
            {
                if (!string.IsNullOrEmpty(configItem.qjj))
                {
                    string rqq = !string.IsNullOrEmpty(configItem.qgg) ? configItem.qgg : "Coins";
                    rww(configItem.qjj, rqq);
                }
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                wtt(player);
                wcc(player);
            }
        }
        void wcc(BasePlayer players)
        {
            timer.Every(uuu.wee / 5, () => {
                if (qtt[players.userID].TimeGame >= uuu.wee && !ddd.Contains(players.userID))
                {
                    wqq(players);
                    ddd.Add(players.userID);
                }
                else
                {
                    qtt[players.userID].TimeGame = Math.Max(qtt[players.userID].TimeGame + (ggg() - qtt[players.userID].PlayerAuthTime), 0);
                    qtt[players.userID].PlayerAuthTime = ggg();
                }
            });
        }
        protected override void LoadDefaultConfig() => uuu = rtt.tvv();
        Plugin ImageLibrary => Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
        static DateTime wxx = new DateTime(1970, 1, 1, 0, 0, 0);
        private static rtt uuu = new rtt();
        [Oxide.Core.Plugins.HookMethod("OnServerInitialized")]
        void tss()
        {
            tpp();
        }
        [ConsoleCommand("TakeAll")]
        void tdd(ConsoleSystem.Arg ryy)
        {
            BasePlayer players = ryy.Player();
            foreach (var AllItems in qtt[players.userID].qqq)
            {
                if (string.IsNullOrEmpty(AllItems.Url))
                {
                    Item taa = ItemManager.CreateByName(AllItems.Value, AllItems.Amount, 0);
                    players.GiveItem(taa);
                    timer.Once(0.5f, () => qtt[players.userID].qqq.Remove(AllItems));
                }
                if (!string.IsNullOrEmpty(AllItems.Url) && !string.IsNullOrEmpty(AllItems.Value))
                {
                    rust.RunServerCommand(AllItems.Value.Replace("%STEAMID%", $"{players.UserIDString }"));
                    timer.Once(0.5f, () => qtt[players.userID].qqq.Remove(AllItems));
                }
                if (string.IsNullOrEmpty(AllItems.Value) && AllItems.Amount != 0 && !string.IsNullOrEmpty(AllItems.Url))
                {
                    if (uuu.eee.tyy != "ServerID" || uuu.eee.tww != "SecretKey")
                    {
                        tee(players.userID, AllItems.Amount, uuu.eee.rrr, (Action<bool>)((tuu) => {
                            if (!tuu)
                            {
                                ttt(players, lang.GetMessage("noauth", this));
                                return;
                            }
                            timer.Once(0.5f, () => qtt[players.userID].qqq.Remove(AllItems));
                        }));
                    }
                    else
                    {
                        ttt(players, "Администратор не настроил плагин.Сообщите ему об этом!");
                        CuiHelper.DestroyUi(players, Ui);
                        return;
                    }
                }
            }
            if (qtt[players.userID].qqq.Count < 1) ttt(players, lang.GetMessage("takeallnull", this));
            else ttt(players, lang.GetMessage("takeallinventory", this));
            CuiHelper.DestroyUi(players, Ui);
        }
        private void rgg(Dictionary<string, string> tii, Action<bool> rpp)
        {
            string too = $"http://panel.gamestores.ru/api?shop_id={uuu.eee.tyy }&secret={uuu.eee.tww }" + $"{string.Join("", tii.Select(ruu => $"&{ruu.Key }={ruu.Value }").ToArray())}";
            webrequest.EnqueueGet(too, (rii, roo) => {
                if (rii != 200)
                {
                    PrintError($"Ошибка зачисления, подробнисти в ЛОГ-Файле");
                    LogToFile("PlayerGifts", $"Код ошибки: {rii }, подробности:\n{roo }", this);
                    rpp(false);
                }
                else
                {
                    if (roo.Contains("fail"))
                    {
                        rpp(false);
                        return;
                    }
                    rpp(true);
                }
            }, this);
        }
        private void tee(ulong raa, float rss, string rdd, Action<bool> rff)
        {
            rgg(new Dictionary<string, string>() { { "action", "moneys" }, { "type", "plus" }, { "steam_id", raa.ToString() }, { "amount", rss.ToString() }, { "mess", rdd } }, rff);
        }
        static string qzz = "XCC_MESSAGES_UI";
        public bool rww(string rkk, string rhh, ulong rjj = 0) => (bool)ImageLibrary?.Call("AddImage", rkk, rhh, rjj);
        public class qdd
        {
            public double TimeGame
            {
                get;
                set;
            }
            public double PlayerAuthTime
            {
                get;
                set;
            }
            public List<qff> qqq = new List<qff>();
            public class qff
            {
                public string Value
                {
                    get;
                    set;
                }
                public int Amount
                {
                    get;
                    set;
                }
                public string Url
                {
                    get;
                    set;
                }
                public qff(string rxx = "", int rll = 0, string rzz = "")
                {
                    this.Value = rxx;
                    this.Amount = rll;
                    this.Url = rzz;
                }
            }
        }
        static string Ui = "XCC_INVENTORY";
        [ConsoleCommand("TakeItem")]
        void trr(ConsoleSystem.Arg rcc)
        {
            BasePlayer players = rcc.Player();
            var www = qtt[players.userID].qqq.ElementAt(Convert.ToInt32(rcc.Args[0]));
            if (string.IsNullOrEmpty(www.Url))
            {
                Item rbb = ItemManager.CreateByName(www.Value, www.Amount, 0);
                players.GiveItem(rbb);
                qtt[players.userID].qqq.Remove(www);
                ttt(players, lang.GetMessage("takeitem", this));
            }
            if (!string.IsNullOrEmpty(www.Url) && !string.IsNullOrEmpty(www.Value))
            {
                rust.RunServerCommand(www.Value.Replace("%STEAMID%", $"{players.UserIDString }"));
                qtt[players.userID].qqq.Remove(www);
                ttt(players, lang.GetMessage("takeitem", this));
            }
            if (string.IsNullOrEmpty(www.Value) && www.Amount != 0 && !string.IsNullOrEmpty(www.Url))
            {
                if (uuu.eee.tyy != "ServerID" || uuu.eee.tww != "SecretKey")
                {
                    tee(players.userID, www.Amount, uuu.eee.rrr, (Action<bool>)((rnn) => {
                        if (!rnn)
                        {
                            ttt(players, lang.GetMessage("noauth", this));
                            return;
                        }
                        qtt[players.userID].qqq.Remove(www);
                        ttt(players, lang.GetMessage("takeitem", this));
                    }));
                }
                else
                {
                    ttt(players, "Администратор не настроил плагин.Сообщите ему об этом!");
                    return;
                }
            }
            CuiHelper.DestroyUi(players, Ui);
        }
        void wgg()
        {
            rmm();
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, yyy);
        }
    }
}