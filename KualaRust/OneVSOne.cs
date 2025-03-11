using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("OneVSOne", "fermens", "0.4.01")]
    [Description("Турнир одиночек")]
    public class OneVSOne : RustPlugin
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
            [JsonProperty("Позиция первого игрока")]
            public string posplayer1;

            [JsonProperty("Позиция второго игрока")]
            public string posplayer2;

            [JsonProperty("Позиции зрителей")]
            public List<string> posspectators;

            [JsonProperty("Сообщения")]
            public List<string> messages;

            [JsonProperty("Префикс")]
            public string prefix;

            [JsonProperty("Чат-команда")]
            public string command;

            [JsonProperty("Кит-награда (Оставить пустым, если нет)")]
            public string kitreward;

            [JsonProperty("Кит выдаваемый для дуэлянтов")]
            public string kit;

            [JsonProperty("Название арены в копипасте")]
            public string arenaname;

            [JsonProperty("Проверок на телепорт абузеров")]
            public int proverok;

            [JsonProperty("Выделенное время на одну дуэль")]
            public int timeonround;

            [JsonProperty("Дефолтное время на регистрацию")]
            public int timeforreg;

            [JsonProperty("Минимальное количество участников")]
            public int minplayers;

            [JsonProperty("ServerRewards-награда (Поставить 0, если нет)")]
            public int sRewards;

            [JsonProperty("Максимальное количество участников")]
            public int maxplayers;

            [JsonProperty("Автоивент (0 - если выключить)")]
            public float autoevent;

            [JsonProperty("Награда консольная команда, по местам (пусто, если нет)")]
            public List<string> rewardcommands;

            [JsonProperty("Замутить войс на ивенте")]
            public bool mutevoice;

            [JsonProperty("Координаты постройки арены")]
            public string positionforarena;

            [JsonProperty("GUI - кнопка")]
            public string button;

            [JsonProperty("GUI - фон")]
            public string fon;

            [JsonProperty("GUI - верхушка")]
            public string head;

            [JsonProperty("GUI - низушка")]
            public string foot;

            [JsonProperty("Оружия для автоивента (выбирается рандомно)")]
            public List<int> random;

            [JsonProperty("Выводить все сообщения")]
            public bool messagetrue;

            [JsonProperty("- STEAMID от кого отправляются сообщения (для аватарки)")]
            public ulong broadcast2;

            [JsonProperty("^^")]
            public string rofl;

            [JsonProperty("Заблокированные команды, которые используют global.")]
            public string[] blocked;

            [JsonProperty("Моды")]
            public List<int> mods;

            [JsonProperty("UI-сообщения")]
            public List<string> messageUI;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    rofl = "^_^",
                    broadcast2 = 76561198125444659,
                    posplayer1 = "",
                    posplayer2 = "",
                    posspectators = new List<string>(),
                    messages = new List<string>()
                    {
                        "<size=15><#CD5C5C>{prefix} Ошибка | Нет пар для дуэлей!</color></size>",
                        "<size=15><#CD5C5C>{prefix} Ошибка | Каким-то странным образом победитель не определился!</color></size>",
                        "<size=15>ИВЕНТ <color=#ccff33>ТУРНИР ОДИНОЧЕК</color> НАЧНЕТСЯ ЧЕРЕЗ <color=#ccff33>{time}</color>\n<color=#ccff33>/{command}</color> - попасть на ивент</size>",
                        "<size=15><#CD5C5C>{prefix} Багоюз | Попытка проникнуть на ивент телепорт-багом! СМЭРТЬ!</color></size>",
                        "<size=15>{prefix} <color=#ccff33>/{command}</color> - выйти с ивента</size>",
                        "<size=15>{prefix} <color=#ff6666>ЗАПРЕЩЕНО ИСПОЛЬЗОВАТЬ КОМАНДЫ НА ИВЕНТЕ!</color>\n<color=#ccff33>/{command}</color> - выйти с ивента</size>",
                        "<size=15>{prefix} <color=#ff6666>ИВЕНТ ОТМЕНЕН, НЕ СОБРАЛОСЬ МИНИМАЛЬНОЕ КОЛИЧЕСТВО УЧАСТНИКОВ!</color>",
                        "<size=15>{prefix} ИГРОК <color=#ccff33>{name}</color> ПОБЕДИЛ В ИВЕНТЕ <color=#ccff33>ТУРНИР ОДИНОЧЕК</color>\nПРИЗ ДВЕ ПЛЯШКИ ПЕННОГО.</size>",
                        "<size=15>{prefix} <color=#ccff33>{name}</color> ПРОХОДИТ В СЛЕДУЮЩИЙ РАУНД.</size>",
                        "<size=12>{prefix} <color=#ccff33>{name}</color> ПОКИДАЕТ ИВЕНТ.</size> - выключено -",
                        "<size=12>{prefix} <color=#ccff33>{name}</color> ЗАШЕЛ НА ИВЕНТ.</size>",
                        "<size=15><#CD5C5C>{prefix} ИВЕНТ ОТМЕНЕН!</color></size>",
                        "<i>- {prefix} Время вышло, действуйте быстрее -</i>",
                        "<size=15>{prefix} <color=#ccff33>{name1}</color> против <color=#ccff33>{name2}</color></size>"
                    },
                    prefix = "[EVENT]",
                    proverok = 10,
                    timeonround = 60,
                    command = "oo",
                    kitreward = "reward",
                    kit = "onekit",
                    arenaname = "arenaoo",
                    positionforarena = "(-1000.0, 800.0, -1000.0)",
                    minplayers = 2,
                    maxplayers = 64,
                    mutevoice = true,
                    sRewards = 50,
                    autoevent = 7200f,
                    timeforreg = 120,
                    messagetrue = true,
                    random = new List<int>()
                    {
                        1545779598,
                        1443579727,
                        1965232394,
                        -1812555177,
                        818877484,
                        -904863145
                    },
                    mods = new List<int>()
                    {
                        952603248,
                        442289265
                    },
                    messageUI = new List<string>
                    {
                        "ВАС УБИЛИ! ДЛЯ ВАС ИВЕНТ ЗАКОНЧЕН.",
                        "ПОЗДРАВЛЯЕМ С ВЕЛИКОЙ ПОБЕДОЙ, ЧЕМПИОН!"
                    },
                    rewardcommands = new List<string>
                    {
                        "addgroup {steamid} 1place 3h",
                        "addgroup {steamid} 2place 2h",
                        "addgroup {steamid} 3place 1h"
                    },
                    blocked = new string []
                    {
                        "tp", "home", "kit"
                    },
                    button = "1 0.83 0.47 0.35",
                    fon = "1 1 1 0.03",
                    head = "<color=#ffd479>ТУРНИР ОДИНОЧЕК\nоружие {gun}</color>",
                    foot = "УЧАСТНИКОВ: <color=#ffd479>{count}</color>\nСТАРТ ЧЕРЕЗ <color=#ffd479>{time}</color>"


                };
            }
        }
        #endregion

        #region Head
        List<string> logs = new List<string>();
        [PluginReference] Plugin CopyPaste, Kits, ServerRewards, Economics, XKits;
        Timer Timeronround;
        Vector3 positionforarena = Vector3.zero;
        int round = 1;
        static OneVSOne ins;
        List<ulong> cashplayers = new List<ulong>();
        List<BasePlayer> players = new List<BasePlayer>();
        List<Duel> duels = new List<Duel>();
        //
        Vector3 posplayer1 = Vector3.zero;
        Vector3 posplayer2 = Vector3.zero;
        List<Vector3> posspectators = new List<Vector3>();
        int start = 0;
        uint privilege;
        int gun = 1545779598;
        const int ammo = 300;
        //

        class Duel
        {
            public BasePlayer pl1;
            public BasePlayer pl2;
        }
        #endregion

        #region BlockDamage
        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            BaseEntity entity = info.HitEntity;
            if (entity == null) return;
            if (entity is DecayEntity)
            {
                var build = entity.GetBuildingPrivilege();
                if (build == null) return;
                if (build.net.ID.Equals(privilege))
                {
                    if (info.Initiator is FireBall) info.Initiator.Kill();
                    clear(info);
                }
            }
            else if (entity is BasePlayer)
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || !cashplayers.Contains(player.userID)) return;
                if (player.GetComponent<OnePlayer>().duel)
                {
                    float damageAmount = info.damageTypes.Total();
                    if (info.isHeadshot) damageAmount *= 1.5f;
                    if (entity.Health() - damageAmount <= 0f)
                    {
                        Duel duel = duels.Where(x => x.pl1.Equals(player) || x.pl2.Equals(player)).FirstOrDefault();
                        if (duel != null) endduel(player, true);
                        else player.GetComponent<OnePlayer>().ToDestroy();
                        timer.Once(1f, () =>
                        {
                            if (player == null) return;
                            if (player.IsWounded()) player.Hurt(1000f, DamageType.Suicide, player, false);
                        });
                    }
                }
                else clear(info);
            }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!cashplayers.Contains(player.userID)) return;
            Duel duel = duels.Where(x => x.pl1.Equals(player) || x.pl2.Equals(player)).FirstOrDefault();
            if (duel != null) endduel(player, true);
            else player.GetComponent<OnePlayer>().ToDestroy();
        }

        void OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity entity)
        {
            if (!(entity is BasePlayer)) return;
            var player = entity as BasePlayer;
            if (!cashplayers.Contains(player.userID)) return;
            if (player.metabolism.temperature.value < 20) player.metabolism.temperature.value = 21;
        }

        void clear(HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.HitEntity = null;
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }
        #endregion

        #region Main
        public void GiveKit(BasePlayer player)
        {
            Item item = ItemManager.CreateByItemID(gun);
            BaseProjectile projectile = item.GetHeldEntity()?.GetComponent<BaseProjectile>();
            if (projectile != null && projectile.primaryMagazine != null)
            {
                projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
                Item ammoitem = ItemManager.Create(projectile.primaryMagazine.ammoType, 300);
                player.inventory.GiveItem(ammoitem, player.inventory.containerMain);
            }

            if (config.mods.Count > 0 && item.contents != null)
            {
                foreach (var z in config.mods)
                {
                    Item mod = ItemManager.CreateByItemID(z);
                    mod.MoveToContainer(item.contents);
                }
            }

            player.inventory.GiveItem(item, player.inventory.containerBelt);
            Kits?.Call("GiveKit", player, config.kit);
            XKits?.Call("GiveKit", player, config.kit);
        }

        const string nameui = "fonTsadq2cqse3";
        const string textui = "Tsadq2cqse3";
        void destroyUI(List<Network.Connection> netcon)
        {
            if (netcon == null || netcon.Count < 0) return;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = netcon }, null, "DestroyUI", nameui);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = netcon }, null, "DestroyUI", textui);
        }

        void createUItext(List<Network.Connection> netcon)
        {
            if (netcon == null || netcon.Count < 0) return;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = netcon }, null, "DestroyUI", textui);
            string sendto = GUItext.Replace("{count}", players.Count.ToString()).Replace("{time}", TimeSpan.FromSeconds(start).ToString());
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = netcon }, null, "AddUI", sendto);
        }

        void createUItext(Network.Connection netcon)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = netcon }, null, "DestroyUI", textui);
            string sendto = GUItext.Replace("{count}", players.Count.ToString()).Replace("{time}", TimeSpan.FromSeconds(start).ToString());
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = netcon }, null, "AddUI", sendto);
        }

        void createUIEXIT(Network.Connection netcon)
        {
            if (netcon == null) return;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = netcon }, null, "DestroyUI", nameui);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = netcon }, null, "DestroyUI", textui);
            string def = ItemManager.FindItemDefinition(gun).displayName.english;
            string sendto1 = GUImain.Replace("{gun}", def).Replace("{button}", "ПРИСОЕДИНИТЬСЯ");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = netcon }, null, "AddUI", sendto1);
            createUItext(netcon);
        }

        void createUI(List<Network.Connection> netcon)
        {
            if (netcon == null || netcon.Count < 0) return;
            destroyUI(netcon);
            string def = ItemManager.FindItemDefinition(gun).displayName.english;
            string sendto1 = GUImain.Replace("{gun}", def).Replace("{button}", "ВЫЙТИ");
            string sendto2 = GUImain.Replace("{gun}", def).Replace("{button}", "ПРИСОЕДИНИТЬСЯ");
            List<Network.Connection> exit = netcon.Where(x => cashplayers.Contains(x.userid)).ToList();
            List<Network.Connection> enter = netcon.Where(x => !cashplayers.Contains(x.userid)).ToList();
            if (exit.Count > 0) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = exit }, null, "AddUI", sendto1);
            if (enter.Count > 0) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = enter }, null, "AddUI", sendto2); ;
            createUItext(netcon);
        }

        string GUItext = "";
        string GUImain = "";
        static Vector3 arenaposition;
        void Init() => Unsubscribe(nameof(OnPasteFinished));
        void OnServerInitialized()
        {
            if(config.rofl == null)
            {
                config.rofl = "^_^";
                config.broadcast2 = 76561198125444659;
                SaveConfig();
            }
            if(config.head == null)
            {
                config.head = "<color=#ffd479>ТУРНИР ОДИНОЧЕК\nоружие {gun}</color>";
                config.foot = "УЧАСТНИКОВ: <color=#ffd479>{count}</color>\nСТАРТ ЧЕРЕЗ <color=#ffd479>{time}</color>";
                SaveConfig();
            }
            if(config.fon == null)
            {
                config.button = "1 0.83 0.47 0.35";
                config.fon = "1 1 1 0.03";
                SaveConfig();
            }
            GUItext = "[{\"name\":\"Tsadq2cqse3\",\"parent\":\"Hud.Menu\",\"components\":[{\"type\":\"UnityEngine.UI.Image\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\"}]},{\"name\":\"4c277f7583f6463e9cad69f4b92aaf62\",\"parent\":\"Tsadq2cqse3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{foot}\",\"align\":\"MiddleLeft\",\"color\":\"1 1 1 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"-430 38\",\"offsetmax\":\"-265 78\"}]}]".Replace("{foot}", config.foot);
            GUImain = "[{\"name\":\"fonTsadq2cqse3\",\"parent\":\"Hud.Menu\",\"components\":[{\"type\":\"UnityEngine.UI.Image\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\"}]},{\"name\":\"c274caa359cf479bad4448ef8c47b792\",\"parent\":\"fonTsadq2cqse3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"{fon}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"-440 18\",\"offsetmax\":\"-265 78\"}]},{\"name\":\"790d2667be324c2e929c046594ca9a4d\",\"parent\":\"fonTsadq2cqse3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{head}\",\"align\":\"LowerCenter\",\"color\":\"1 1 1 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"-440 80\",\"offsetmax\":\"-265 120\"}]},{\"name\":\"003a131d6cab495d89698c7028ee2429\",\"parent\":\"fonTsadq2cqse3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"chat.say /{command}\",\"material\":\"assets/content/ui/ui.background.transparent.radial.psd\",\"color\":\"{buttoncolor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"-440 18\",\"offsetmax\":\"-265 38\"}]},{\"parent\":\"003a131d6cab495d89698c7028ee2429\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{button}\",\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.5\"},{\"type\":\"RectTransform\"}]}]".Replace("{fon}", config.fon).Replace("{buttoncolor}", config.button).Replace("{head}", config.head);
            if (config.blocked == null)
            {
                config.blocked = new string[]
                    {
                        "tp", "home", "kit"
                    };
                SaveConfig();
            }
            if (config.rewardcommands == null)
            {
                config.rewardcommands = new List<string>
                    {
                        "addgroup {steamid} 1place 3h",
                        "addgroup {steamid} 2place 2h",
                        "addgroup {steamid} 3place 1h",
                        "addgroup {steamid} 4place 1h"
                    };
                SaveConfig();
            }
            GUImain = GUImain.Replace("{command}", config.command);

            if (positionforarena.Equals(Vector3.zero)) positionforarena = config.positionforarena.ToVector3();
            ins = this;
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand(config.command, this, "cmdtoevent");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("event.oo", this, "cmdconsolecommand");
            unsubscribe();
            Debug.Log("Проверяем построена ли арена.");
            RaycastHit hitInfo;
            if (!Physics.Raycast(positionforarena + 10f * Vector3.down, Vector3.up * 100f, out hitInfo, LayerMask.GetMask("Construction")))
            {
                Debug.Log("Арена не построена...строим арену...");
                timer.Once(30f, () =>  createarena());
            }
            else
            {
                initialize();
                arenaposition = hitInfo.point;
                privilege = hitInfo.GetEntity().GetBuildingPrivilege().net.ID;
            }
            permission.RegisterPermission(Name + ".admin", this);
            if (config.autoevent > 0)
            {
                eventtime = DateTime.UtcNow.AddSeconds(config.autoevent);
                timer.Once(config.autoevent, () => autoeventON());
            }
        }
        private DateTime GetNextEventTime()
        {
            return eventtime;
        }

        private DateTime eventtime;
        void autoeventON()
        {
            eventtime = DateTime.UtcNow.AddSeconds(config.autoevent);
            timer.Once(config.autoevent, () => autoeventON());
            if (config.random.Count > 0) gun = config.random[Random.Range(0, config.random.Count)];
            startreg(config.timeforreg);
        }

        void spammes(string time) => Broadcast(config.messages[2].Replace("{prefix}", config.prefix).Replace("{time}", time).Replace("{command}", config.command));

        void startevent()
        {
            logs.Clear();
            destroyUI(Network.Net.sv.connections);
            if (players.Count < config.minplayers)
            {
                Broadcast(config.messages[6].Replace("{prefix}", config.prefix));
                endevent();
                return;
            }
            logs.Add($"[{DateTime.Now.ToString("H:mm")}] Участников: {players.Count} | Оружие: {ItemManager.FindItemDefinition(gun).displayName.english}");
            generetepairs();
        }

        List<Timer> timersspam = new List<Timer>();

        private bool IsEventArena(uint priv)
        {
            if (!privilege.Equals(priv)) return false;
            else return true;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (Vector3.Distance(arenaposition, entity.transform.position) < 70f) entity?.Kill();
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is PlayerCorpse || entity.prefabID == 1519640547)
            {
                if (Vector3.Distance(arenaposition, entity.transform.position) < 70f) entity?.Kill();
            }
        }

        void startreg(int time = 120)
        {
            if (privilege == 0)
            {
                Debug.LogError("Арена для ивента отсутствует!");
                return;
            }
            if (posplayer1.Equals(Vector3.zero) || posplayer2.Equals(Vector3.zero) || posspectators.Count.Equals(0))
            {
                Debug.LogError("Не указаны все точки спавна!");
                return;
            }
            endevent();
            messageUI.Clear();
            subscribe();
            start = time;
            Subscribe(nameof(OnPlayerDeath));
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(OnItemDropped));
            Subscribe(nameof(OnPlayerAttack));
            Subscribe(nameof(OnRunPlayerMetabolism));
            spammes(FormatTime(TimeSpan.FromSeconds(time)).ToUpper());
            NextTick(() =>
            {
                createUI(Network.Net.sv.connections);
            });
            timersspam.Add(timer.Repeat(1f, 0, () => ticktimer()));
        }

        void ticktimer()
        {
            start--;
            if (start <= 0)
            {
                foreach (var z in timersspam) destroytimer(z);
                startevent();
                return;
            }
            else if (start.Equals(60)) spammes("60 СЕКУНД");
            else if (start.Equals(30)) spammes("30 СЕКУНД");
            else if (start.Equals(15)) spammes("15 СЕКУНД");
            else if (start.Equals(10)) spammes("10 СЕКУНД");
            else if (start.Equals(3)) spammes("3 СЕКУНДЫ");
            else if (start.Equals(2)) spammes("2 СЕКУНДЫ");
            else if (start.Equals(1)) spammes("1 СЕКУНДУ");
            createUItext(Network.Net.sv.connections);
        }

        void createarena()
        {
            Subscribe(nameof(OnPasteFinished));
            if (CopyPaste == null)
            {
                Debug.LogError("Установите копипаст!");
                return;
            }
            var options = new List<string> { "Deployables", "true", "Inventories", "true", "height", positionforarena.y.ToString() };
            var successPaste = CopyPaste.Call("TryPasteFromVector3", positionforarena, 0f, config.arenaname, options.ToArray());
            if (successPaste is string)
            {
                PrintError(successPaste.ToString());
                Unsubscribe(nameof(OnPasteFinished));
            }
        }

        void initialize()
        {
            if (posplayer1.Equals(Vector3.zero))
            {
                if (string.IsNullOrEmpty(config.posplayer1))
                {
                    Debug.LogError("Не указаны координаты для первого игрока!");
                    return;
                }
                posplayer1 = config.posplayer1.ToVector3() + Vector3.up * 0.1f;
            }
            if (posplayer2.Equals(Vector3.zero))
            {
                if (string.IsNullOrEmpty(config.posplayer2))
                {
                    Debug.LogError("Не указаны координаты для второго игрока!");
                    return;
                }
                posplayer2 = config.posplayer2.ToVector3() + Vector3.up * 0.1f;
            }

            if (posspectators.Count.Equals(0))
            {
                if (config.posspectators.Equals(0))
                {
                    Debug.LogError("Не указаны координаты для зрителей!");
                    return;
                }

                foreach (var z in config.posspectators)
                {
                    Vector3 pos = z.ToVector3();
                    posspectators.Add(pos);
                }
            }
            Debug.Log("Координаты указаны.");
        }

        private void OnPasteFinished(List<BaseEntity> entitys)
        {
            posplayer1 = Vector3.zero;
            posplayer2 = Vector3.zero;
            config.posspectators.Clear();
            posspectators.Clear();

            foreach (var entity in entitys)
            {
                if (entity is FlasherLight)
                {
                    Vector3 pos = entity.transform.position + Vector3.up;
                    if (posplayer1.Equals(Vector3.zero))
                    {
                        posplayer1 = pos + Vector3.up * 0.1f;
                        config.posplayer1 = pos.ToString();
                    }
                    else if (posplayer2.Equals(Vector3.zero))
                    {
                        posplayer2 = pos + Vector3.up * 0.1f;
                        config.posplayer2 = pos.ToString();
                    }
                    entity.KillMessage();
                }
                if (entity is SirenLight)
                {
                    Vector3 pos = entity.transform.position + Vector3.up;
                    posspectators.Add(pos);
                    config.posspectators.Add(pos.ToString());
                    entity.KillMessage();
                }
            }
            Debug.Log("Арена построена!");
            var first = entitys.FirstOrDefault();
            privilege = first.GetBuildingPrivilege().net.ID;
            arenaposition = first.transform.position;
            SaveConfig();
            Unsubscribe(nameof(OnPasteFinished));
        }

        void destroytimer(Timer ss)
        {
            if (!ss.Destroyed) timer.Destroy(ref ss);
        }

        void generetepairs()
        {
            duels.Clear();
            string roundname = $"РАУНД <color=#ccff33>{round}</color>";
            string pairs = $"{config.prefix} {roundname}:\n";
            List<BasePlayer> swess = new List<BasePlayer>(players);
            while (swess.Count > 1)
            {
                Duel duel = new Duel();
                BasePlayer r1 = swess[Random.Range(0, swess.Count)];
                duel.pl1 = r1;
                swess.Remove(r1);
                BasePlayer r2 = swess[Random.Range(0, swess.Count)];
                duel.pl2 = r2;
                swess.Remove(r2);
                duels.Add(duel);
                pairs += $"<color=#ccff33>{duel.pl1.displayName}</color> - <color=#ccff33>{duel.pl2.displayName}</color>\n";
            }

            if (swess.Count > 0) Broadcast(config.messages[8].Replace("{prefix}", config.prefix).Replace("{name}", swess.FirstOrDefault().displayName.ToUpper()));
            if (config.messagetrue)
            {
                if (swess.Count.Equals(2)) roundname = "ФИНАЛ";
                else if (swess.Count <= 4) roundname = "ПОЛУФИНАЛ";
                if (swess.Count > 0) pairs += $"<color=#ccff33>{swess.FirstOrDefault().displayName}</color> проходит в следующий раунд";
                round++;
                Broadcast(pairs);
            }
            startduel();
        }

        void startduel()
        {
            if (players.Count < 2)
            {
                endevent();
                return;
            }
            if (duels.Count.Equals(0))
            {
                nextphase();
                return;
            }
            timersspam.Add(timer.Once(2f, () =>
            {
                if (duels.Count.Equals(0)) return;
                Duel duel = duels.FirstOrDefault();
                fight(duel.pl1, duel.pl2);
            }));
        }

        void fight(BasePlayer player1, BasePlayer player2)
        {
            OnePlayer pl1 = player1.GetComponent<OnePlayer>();
            OnePlayer pl2 = player2.GetComponent<OnePlayer>();
            if (pl1 == null || pl2 == null)
            {
                if (pl1 == null && players.Contains(player1)) players.Remove(player1);
                if (pl2 == null && players.Contains(player2)) players.Remove(player2);
                duels.RemoveAt(0);
                Debug.LogWarning("Ошибка | OnePlayer = null | ИСПРАВИЛИ");
                startduel();
                return;
            }
            pl1.duel = true;
            pl2.duel = true;
            if (player1.inventory.loot?.entitySource != null) player1.EndLooting();
            if (player2.inventory.loot?.entitySource != null) player2.EndLooting();
            pl1.Teleport(posplayer1);
            pl1.Maxatributes();
            pl1.Kit();
            pl2.Teleport(posplayer2);
            pl2.Maxatributes();
            pl2.Kit();
            Broadcast(config.messages[13].Replace("{prefix}", config.prefix).Replace("{name1}", player1.displayName).Replace("{name2}", player2.displayName));
            Timeronround = timer.Once(config.timeonround, () =>
            {
                if (pl1.Health() > pl2.Health()) endduel(player2, true);
                else if (pl1.Health() < pl2.Health()) endduel(player1, true);
                else
                {
                    Broadcast(config.messages[12].Replace("{prefix}", config.prefix));
                    if (Random.Range(0, 2).Equals(0)) endduel(player1, true);
                    else endduel(player2, true);
                }
            });
        }

        void restoreafter(BasePlayer player)
        {
            restore rs = restoreinventory[player.userID];
            if (player.IsWounded()) player.StopWounded();
            Teleport(player, rs.teleportfrom);
            player.metabolism.Reset();
            player.health = rs.health;
            player.metabolism.calories.value = rs.calories;
            player.metabolism.hydration.value = rs.hydration;
            player.metabolism.bleeding.value = 0;
            player.metabolism.SendChangesToClient();
            player.inventory.Strip();
            if (!RestoreItems(player, rs.inventory, InventoryType.Belt) || !RestoreItems(player, rs.inventory, InventoryType.Main) || !RestoreItems(player, rs.inventory, InventoryType.Wear))
            {
                Debug.LogError($"OneVSOne | Игрок {player.displayName} не получил все предметы.");
            }
            LockInventory(player, false);
            restoreinventory.Remove(player.userID);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            timer.Once(1f, () =>
            {
                if (!player.IsConnected || player.IsDead()) return;
                if (restoreinventory.ContainsKey(player.userID)) restoreafter(player);
                else if (player.GetComponent<OnePlayer>() == null && !player.IsAdmin && Vector3.Distance(arenaposition, player.transform.position) < 65f)
                {
                    BasePlayer.SpawnPoint spawnPoint = ServerMgr.FindSpawnPoint();
                    player.Teleport(spawnPoint.pos);
                }

                if (kitreward.Contains(player.userID))
                {
                    Kits?.Call("GiveKit", player, config.kitreward);
                    XKits?.Call("GiveKit", player, config.kitreward);
                    kitreward.Remove(player.userID);
                }

                if (!messageUI.ContainsKey(player)) return;
                string message = messageUI[player];
                timer.Once(1f, () => drawui(player, message));
                messageUI.Remove(player);
            });
        }

        const string drawuimes = "UIasdq2e12";
        string GUImes = "[{\"name\":\"UIasdq2e12\",\"parent\":\"Hud.Menu\",\"components\":[{\"type\":\"UnityEngine.UI.Image\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\"}]},{\"name\":\"UI_mesOnesOne\",\"parent\":\"UIasdq2e12\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{message}\",\"fontSize\":30,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.9\",\"fadeIn\":0.5},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-450 -60\",\"offsetmax\":\"450 60\"}]}]";
        void destroydrawUI(BasePlayer player)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", drawuimes);
        }
        Dictionary<BasePlayer, string> messageUI = new Dictionary<BasePlayer, string>();
        void drawui(BasePlayer player, string message)
        {
            destroydrawUI(player);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUImes.Replace("{message}", message));
            timer.Once(4f, () => destroydrawUI(player));
        }

        void endduel(BasePlayer lose, bool withmessage = false)
        {
            Duel duel = duels.FirstOrDefault(x => x.pl1.Equals(lose) || x.pl2.Equals(lose));
            if (duel == null)
            {
                Debug.LogError("Ошибка | Такой пары для дуэли не существует!");
                return;
            }
            messageUI.Add(lose, config.messageUI[0]);
            OnePlayer pl1 = duel.pl1?.GetComponent<OnePlayer>();
            OnePlayer pl2 = duel.pl2?.GetComponent<OnePlayer>();
            if (pl1 == null || pl2 == null)
            {
                Debug.LogError("Ошибка | КРИТИЧЕСКАЯ!");
                endevent();
                return;
            }
            bool active = pl1.duel;
            pl1.duel = false;
            pl2.duel = false;
            if (duel.pl2.Equals(lose))
            {
                logs.Add($"[{DateTime.Now.ToString("H:mm")}] победил: {duel.pl1.displayName} ({duel.pl1.UserIDString}) - {duel.pl2.displayName} ({duel.pl2.UserIDString}) :проиграл");
                pl1.Clear();
                pl1.Teleport(posspectators[Random.Range(0, ins.posspectators.Count)]);
                pl1.Maxatributes();
                int count = ins.cashplayers.Count - 1;
                if (count >= 0 && count < ins.config.rewardcommands.Count)
                {
                    ins.Server.Command(ins.config.rewardcommands[count].Replace("{steamid}", duel.pl2.UserIDString));
                }
                pl2.ToDestroy();
                if (withmessage) Broadcast(config.messages[8].Replace("{prefix}", config.prefix).Replace("{name}", duel.pl1.displayName.ToUpper()));
            }
            else
            {
                logs.Add($"[{DateTime.Now.ToString("H:mm")}] победил: {duel.pl2.displayName} ({duel.pl2.UserIDString}) - {duel.pl1.displayName} ({duel.pl1.UserIDString}) :проиграл");
                pl2.Clear();
                pl2.Teleport(posspectators[Random.Range(0, ins.posspectators.Count)]);
                pl2.Maxatributes();
                int count = ins.cashplayers.Count - 1;
                if (count >= 0 && count < ins.config.rewardcommands.Count)
                {
                    ins.Server.Command(ins.config.rewardcommands[count].Replace("{steamid}", duel.pl1.UserIDString));
                }
                pl1.ToDestroy();
                if (withmessage) Broadcast(config.messages[8].Replace("{prefix}", config.prefix).Replace("{name}", duel.pl2.displayName.ToUpper()));
            }

            duels?.Remove(duel);
            if (active)
            {
                destroytimer(Timeronround);
                if (duels.Count > 0)
                {
                    startduel();
                }
                else
                {
                    NextTick(() => nextphase());
                }
            }
        }

        void nextphase()
        {
            if (players.Count.Equals(0))
            {
                Debug.LogError(config.messages[1].Replace("{prefix}", config.prefix));
                Broadcast(config.messages[1].Replace("{prefix}", config.prefix));
                endevent();
                return;
            }
            if (players.Count.Equals(1))
            {
                BasePlayer player = players.FirstOrDefault();
                logs.Add($"[{DateTime.Now.ToString("H:mm")}] {player.displayName} ({player.UserIDString}) - ВЫИГРАЛ!");
                Interface.Oxide.DataFileSystem.WriteObject($"ONEVSONE/{DateTime.Now.ToString("HH_mm____dd_MM")}", logs);
                logs.Clear();
                messageUI.Add(player, config.messageUI[1]);
                Broadcast(config.messages[7].Replace("{prefix}", config.prefix).Replace("{name}", player.displayName.ToUpper()));
                endevent();
                timer.Once(1f, () =>
                {
                    if (ServerRewards != null && config.sRewards > 0) ConsoleSystem.Run(ConsoleSystem.Option.Server, $"sr add {player.userID} {config.sRewards}");
                    if (Economics != null && config.sRewards > 0) Economics?.Call("Deposit", player.userID, Convert.ToDouble(config.sRewards));
                    if (Kits != null && !string.IsNullOrEmpty(config.kitreward)) kitreward.Add(player.userID);
                    if (XKits != null && !string.IsNullOrEmpty(config.kitreward)) kitreward.Add(player.userID);
                    if (config.rewardcommands.Count > 0)
                    {
                        ins.Server.Command(ins.config.rewardcommands[0].Replace("{steamid}", player.UserIDString));
                    }
                });
                return;
            }
            generetepairs();
        }
        List<ulong> kitreward = new List<ulong>();
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.inventory.containerMain.HasFlag(ItemContainer.Flag.IsLocked)) LockInventory(player, false);
            NextTick(() =>
            {
                if (start <= 0 || player == null || player.net == null) return;
                if (player.GetComponent<OnePlayer>() != null) player.GetComponent<OnePlayer>().ToDestroy();
                createUI(new List<Network.Connection> { player.net.connection });
            });
        }

        void unsubscribe()
        {
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(OnItemDropped));
            Unsubscribe(nameof(OnPlayerVoice));
            Unsubscribe(nameof(OnPlayerAttack));
            Unsubscribe(nameof(OnUserCommand));
            Unsubscribe(nameof(OnServerCommand));
            Unsubscribe(nameof(OnPlayerDisconnected));
            Unsubscribe(nameof(OnPlayerDeath));
            Unsubscribe(nameof(OnRunPlayerMetabolism));
        }

        void subscribe()
        {
            if (config.mutevoice) Subscribe(nameof(OnPlayerVoice));
            Subscribe(nameof(OnUserCommand));
            Subscribe(nameof(OnServerCommand));
            Subscribe(nameof(OnPlayerDisconnected));
        }

        void endevent()
        {
            round = 1;
            start = 0;
            if (timersspam.Count > 0)
            {
                foreach (var z in timersspam) z?.Destroy();
            }

            unsubscribe();

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player != null && player.GetComponent<OnePlayer>() != null) player.GetComponent<OnePlayer>().ToDestroy();
            }
            // Если с ботами
            foreach (var player in players) player.GetComponent<OnePlayer>()?.ToDestroy();
            //
            destroyUI(Network.Net.sv.connections);
            players.Clear();
            cashplayers.Clear();
            duels.Clear();
        }

        private object OnUserCommand(IPlayer player, string com, string[] args)
        {
            com = com.TrimStart('/').Substring(com.IndexOf(".", StringComparison.Ordinal) + 1).ToLower();
            return blocker(BasePlayer.Find(player.Id), com);
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            string com = arg.cmd.FullName.ToLower();
            if (com.Equals("chat.teamsay")) return null;
            return blocker(arg.Player(), com);
        }

        private object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            if (player.GetComponent<OnePlayer>() != null && !player.IsAdmin && !permission.UserHasPermission(player.UserIDString, Name + ".admin")) return false;
            return null;
        }
        private static string[] whitelist = new string[] { "report", "mreport", "ban", "mute", "pm", "r" };
        private object blocker(BasePlayer player, string com)
        {
            if (player == null) return null;
            if (com.Equals(config.command) || com.Contains("global.") && !config.blocked.Contains(com) || whitelist.Contains(com)) return null;
            OnePlayer one = player.GetComponent<OnePlayer>();
            if (one != null)
            {
                if (DateTime.Now > one.lastmsg)
                {
                    Message(player, config.messages[5].Replace("{prefix}", config.prefix).Replace("{command}", config.command));
                    one.lastmsg = DateTime.Now.AddSeconds(3);
                }
                return true;
            }
            return null;
        }


        public class ItemClass
        {
            public int itemid;
            public ulong skin;
            public int amount;
            public float condition;
            public float maxCondition;
            public int ammo;
            public string ammotype;
            public ProtoBuf.Item.InstanceData instanceData;
            public ItemClass[] contents;
        }

        public enum InventoryType { Main, Wear, Belt };
        static IEnumerable<ItemClass> GetItems(ItemContainer container)
        {
            return container.itemList.Where(x => x.amount > 0).Select(item => new ItemClass
            {
                itemid = item.info.itemid,
                amount = item.amount,
                ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                skin = item.skin,
                maxCondition = item.maxCondition,
                condition = item.condition,
                instanceData = item.instanceData ?? null,
                contents = item.contents?.itemList.Select(item1 => new ItemClass
                {
                    itemid = item1.info.itemid,
                    amount = item1.amount,
                    condition = item1.condition,
                    maxCondition = item1.maxCondition
                }).ToArray()
            });
        }

        static bool RestoreItems(BasePlayer player, Dictionary<InventoryType, List<ItemClass>> items, InventoryType type)
        {
            ItemContainer container = type == InventoryType.Belt ? player.inventory.containerBelt : type == InventoryType.Wear ? player.inventory.containerWear : player.inventory.containerMain;

            for (int i = 0; i < container.capacity; i++)
            {
                var existingItem = container.GetSlot(i);
                if (existingItem != null)
                {
                    existingItem.RemoveFromContainer();
                    existingItem.Remove(0f);
                }
                if (items[type].Count > i)
                {
                    var itemData = items[type][i];
                    var item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                    item.condition = itemData.condition;
                    item.maxCondition = itemData.maxCondition;
                    if (itemData.instanceData != null)
                        item.instanceData = itemData.instanceData;

                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        if (!string.IsNullOrEmpty(itemData.ammotype))
                            weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                        weapon.primaryMagazine.contents = itemData.ammo;
                    }
                    if (itemData.contents != null)
                    {
                        foreach (var contentData in itemData.contents)
                        {
                            var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                            if (newContent != null)
                            {
                                newContent.condition = contentData.condition;
                                newContent.maxCondition = contentData.maxCondition;
                                newContent.MoveToContainer(item.contents);
                            }
                        }
                    }
                    item.position = i;
                    item.SetParent(container);
                }
            }
            if (container.itemList.Count == items[type].Count) return true;
            return false;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            proverochka(player);
        }

        void proverochka(BasePlayer player)
        {
            OnePlayer one = player.GetComponent<OnePlayer>();
            if (one != null)
            {
                Duel duel = duels.Where(x => x.pl1.Equals(player) || x.pl2.Equals(player)).FirstOrDefault();
                if (duel != null) endduel(player);
                else one.ToDestroy(true);
            }
        }

        static void maxatributes(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(-1262185308, 1, 0);
            player.GiveItem(item);
            player.metabolism.Reset();
            player.health = 100f;
            player.metabolism.calories.value = player.metabolism.calories.max;
            player.metabolism.hydration.value = player.metabolism.hydration.max;
            player.metabolism.bleeding.value = 0;
            player.metabolism.SendChangesToClient();
        }

        class OnePlayer : MonoBehaviour
        {
            public BasePlayer player;
            Vector3 teleportfrom;
            float health;
            float calories;
            float hydration;
            public DateTime lastmsg;
            public bool withoumessage;
            public bool duel = false;
            Dictionary<InventoryType, List<ItemClass>> inventory = new Dictionary<InventoryType, List<ItemClass>>();

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                if (player == null)
                {
                    Destroy(this);
                    return;
                }

                if (ins.config.messagetrue) Broadcast(ins.config.messages[10].Replace("{prefix}", ins.config.prefix).Replace("{name}", player.displayName.ToUpper()));

                teleportfrom = player.transform.position;
                player.inventory.crafting.CancelAll(true);
                inventory = new Dictionary<InventoryType, List<ItemClass>>
                {
                    { InventoryType.Belt, GetItems(player.inventory.containerBelt).ToList() },
                    { InventoryType.Main, GetItems(player.inventory.containerMain).ToList() },
                    { InventoryType.Wear, GetItems(player.inventory.containerWear).ToList() }
                };
                Clear();
                health = player.Health();
                calories = player.metabolism.calories.value;
                hydration = player.metabolism.hydration.value;
                ins.Teleport(player, ins.posspectators[Random.Range(0, ins.posspectators.Count)]);
                Maxatributes();
                ins.LockInventory(player, true);
                Message(player, ins.config.messages[4].Replace("{prefix}", ins.config.prefix).Replace("{command}", ins.config.command));
                if (!ins.players.Contains(player)) ins.players.Add(player);
                ins.cashplayers.Add(player.userID);
                ins.createUI(new List<Network.Connection> { player.net.connection });
            }

            public void Teleport(Vector3 pos) => player.MovePosition(pos);
            public void ToDestroy(bool with = false)
            {
                withoumessage = with;
                Destroy(this);
            }
            public void Maxatributes() => maxatributes(player);
            public float Health() => player.Health();
            public void Kit() => ins.GiveKit(player);
            public void Clear() => player.inventory.Strip();
            void OnDestroy()
            {
                if (player == null) return;
                Clear();
                ins.restoreinventory[player.userID] = new restore { calories = this.calories, health = this.health, hydration = this.hydration, inventory = this.inventory, teleportfrom = this.teleportfrom };
                if (!player.IsDead() && !player.IsWounded()) ins.restoreafter(player);
                else player.Hurt(1000f, DamageType.Generic, (BaseEntity)player, false);
                if (ins.start > 0 && !withoumessage) ins.createUIEXIT(player.net.connection);
                if (ins.players.Contains(player)) ins.players.Remove(player);
                if (ins.cashplayers.Contains(player.userID)) ins.cashplayers.Remove(player.userID);
            }
        }

        void LockInventory(BasePlayer player, bool lockOrNot)
        {
            if (player.inventory.containerWear.HasFlag(ItemContainer.Flag.IsLocked) != lockOrNot)
                player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, lockOrNot);

            if (player.inventory.containerBelt.HasFlag(ItemContainer.Flag.IsLocked) != lockOrNot)
                player.inventory.containerBelt.SetFlag(ItemContainer.Flag.IsLocked, lockOrNot);

            if (player.inventory.containerMain.HasFlag(ItemContainer.Flag.IsLocked) != lockOrNot)
                player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, lockOrNot);

            player.inventory.SendSnapshot();
        }

        class restore
        {
            public Vector3 teleportfrom;
            public float health;
            public float calories;
            public float hydration;
            public Dictionary<InventoryType, List<ItemClass>> inventory = new Dictionary<InventoryType, List<ItemClass>>();
        }

        Dictionary<ulong, restore> restoreinventory = new Dictionary<ulong, restore>();

        void Teleport(BasePlayer player, Vector3 position)
        {

            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        void Unload()
        {
            endevent();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", drawuimes);
        }
        private bool IsEventPlayer(BasePlayer player)
        {
            if (restoreinventory.ContainsKey(player.userID) || player.GetComponent<OnePlayer>() != null) return true;
            return false;
        }
        #endregion

        #region Message
        static void Message(BasePlayer player, string text)
        {
            ins.Player.Message(player, text, ins.config.broadcast2);
        }

        static void Broadcast(string text)
        {
            ins.Server.Broadcast(text, ins.config.broadcast2);
        }
        #endregion

        #region Commands
        void cmdconsolecommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            if (!arg.HasArgs())
            {
                Debug.Log($"Ивент начнется через {config.timeforreg} секунд.");
                startreg(config.timeforreg);
            }
            else
            {
                if (arg.Args[0].Equals("end"))
                {
                    Debug.Log("Ивент отменен.");
                    Broadcast(config.messages[11].Replace("{prefix}", config.prefix));
                    endevent();
                    return;
                }
                if (arg.Args.Length.Equals(2))
                {
                    int chika;
                    if (!int.TryParse(arg.Args[1], out chika)) chika = config.timeforreg;
                    int gunid;
                    if (int.TryParse(arg.Args[0], out gunid)) gun = gunid;
                    else gunid = gun;
                    startreg(chika);
                    Debug.Log($"Ивент начнется через {chika} секунд, GUNID: {gunid}.");
                }
            }
        }
        void cmdtoevent(BasePlayer player, string cmd, string[] args)
        {
            if (args == null || args.Length < 1)
            {
                OnePlayer inevent = player.GetComponent<OnePlayer>();
                if (start > 0)
                {
                    if (inevent == null)
                    {
                        if (player.IsWounded() || player.IsDead())
                        {
                            Message(player, config.prefix + " <color=#ff6666>ВЫ ТЯЖЕЛО РАНЕНЫ!</color>");
                            return;
                        }
                        if (players.Count >= config.maxplayers)
                        {
                            Message(player, config.prefix + " <color=#ff6666>ИЗВИНИТЕ, МЕСТ БОЛЬШЕ НЕТ!</color>");
                            return;
                        }
                        if (player.isMounted)
                        {
                            Message(player, config.prefix + " <color=#ff6666>НЕЛЬЗЯ ТЕЛЕПОРТИРОВАТЬСЯ НА ИВЕНТ, КОГДА ВЫ НАХОДИТЕСЬ В ТРАНСПОРТНОМ СРЕДСТВЕ!</color>");
                            return;
                        }
                        if (player.InSafeZone())
                        {
                            Message(player, config.prefix + " <color=#ff6666>НЕЛЬЗЯ ТЕЛЕПОРТИРОВАТЬСЯ НА ИВЕНТ, КОГДА ВЫ НАХОДИТЕСЬ В ГОРОДЕ NPC!</color>");
                            return;
                        }

                        if (player.inventory.loot?.entitySource != null) player.EndLooting();

                        player.gameObject.AddComponent<OnePlayer>();
                    }
                    else
                    {
                        if (!player.IsOnGround())
                        {
                            Message(player, config.prefix + " <color=#ff6666>ПОДОЖДИТЕ ПОКА ВАШ ПЕРСОНАЖ ПРИЗЕМЛИТЬСЯ НА ТВЕРДУЮ ПОВЕРХНОСТЬ!</color>");
                            return;
                        }
                        proverochka(player);
                    }
                }
                else
                {
                    if (inevent != null) proverochka(player);
                    else if (players.Count.Equals(0)) Message(player, config.prefix + " <color=#ff6666>ИВЕНТ НЕ ЗАПУЩЕН!</color>");
                    else Message(player, config.prefix + " <color=#ff6666>ИВЕНТ УЖЕ НАЧАЛСЯ!</color>");
                }
            }
            else if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, Name + ".admin"))
            {
                if (args[0].Equals("pl1"))
                {
                    player.Teleport(posplayer1);
                }
                else if (args[0].Equals("pl2"))
                {
                    player.Teleport(posplayer2);
                }
                else if (args[0].Equals("spec"))
                {
                    player.Teleport(posspectators[Random.Range(0, posspectators.Count)]);
                }
                else if (args[0].Equals("start"))
                {
                    if (args.Length > 1)
                    {
                        if (args.Length.Equals(3))
                        {
                            int chika;
                            if (int.TryParse(args[2], out chika))
                            {
                                startreg(chika);
                            }
                            else startreg(config.timeforreg);
                        }
                        int gunid;
                        if (int.TryParse(args[1], out gunid))
                        {
                            gun = gunid;
                        }
                    }
                    else startreg(config.timeforreg);
                }
                else if (args[0].Equals("end"))
                {
                    Broadcast(config.messages[11].Replace("{prefix}", config.prefix));
                    endevent();
                }
                else if (args[0].Equals("bots"))
                {
                    int bots = 1;
                    for (int i = 0; i < bots; i++)
                    {
                        BaseEntity bandit = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", player.transform.position);
                        bandit.Spawn();
                        bandit.gameObject.AddComponent<OnePlayer>();
                    }
                }
                else if (args[0].Equals("kill"))
                {
                    endduel(player, true);
                }
            }
        }
        #endregion

        #region Time
        private string FormatTime(TimeSpan time)
=> (time.Days == 0 ? string.Empty : FormatDays(time.Days)) + (time.Hours == 0 ? string.Empty : FormatHours(time.Hours)) + (time.Minutes == 0 ? string.Empty : FormatMinutes(time.Minutes)) + ((time.Seconds == 0 || time.Days != 0 || time.Hours != 0) ? string.Empty : FormatSeconds(time.Seconds));

        private string FormatDays(int days) => FormatUnits(days, "дней", "дня", "день");

        private string FormatHours(int hours) => FormatUnits(hours, "часов", "часа", "час");

        private string FormatMinutes(int minutes) => FormatUnits(minutes, "минут", "минуты", "минуту");

        private string FormatSeconds(int seconds) => FormatUnits(seconds, "секунд", "секунды", "секунд");

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
    }
}