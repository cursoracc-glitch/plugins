using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Rust;

namespace Oxide.Plugins
{
    [Info("ZetaQuest", "fermenspwnz", "0.1.0")]
    [Description("Достали кактусы...")]
    class ZetaQuest : RustPlugin
    {
        #region Plugins
        Plugin Kits => Interface.Oxide.RootPluginManager.GetPlugin("Kits");
        Plugin XKits => Interface.Oxide.RootPluginManager.GetPlugin("XKits");
        Plugin ImageLibrary => Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        BaseEntity scientist;
        BaseEntity bandit;
        BaseEntity chair;

        private List<ulong> _page = new List<ulong>();
        private List<ulong> _page2 = new List<ulong>();
        private Dictionary<string, baseplayer> currentquests = new Dictionary<string, baseplayer>();
        class baseplayer
        {
            public bool refresh = false;
            public List<string> listquests = new List<string>();
            public Dictionary<string, currentquest> quests = new Dictionary<string, currentquest>();
        }

        class currentquest
        {
            public int count = 0;
            public bool finish = false;
        }

        private ItemDefinition FindItem(string itemNameOrId)
        {
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemNameOrId.ToLower());
            if (itemDef == null)
            {
                int itemId;
                if (int.TryParse(itemNameOrId, out itemId))
                {
                    itemDef = ItemManager.FindItemDefinition(itemId);
                }
            }
            return itemDef;
        }

        private void ItemSpawner(Item item, Vector3 dropPosition)
        {
            ItemContainer container = new ItemContainer();
            container.Insert(item);
            DropUtil.DropItems(container, dropPosition);
        }

        void God(BasePlayer player)
        {
            player._maxHealth = float.MaxValue;
            player.health = float.MaxValue;
            player.metabolism.bleeding.max = 0;
            player.metabolism.bleeding.value = 0;
            player.metabolism.calories.min = 500;
            player.metabolism.calories.value = 500;
            player.metabolism.dirtyness.max = 0;
            player.metabolism.dirtyness.value = 0;
            player.metabolism.heartrate.min = 0.5f;
            player.metabolism.heartrate.max = 0.5f;
            player.metabolism.heartrate.value = 0.5f;
            player.metabolism.hydration.min = 250;
            player.metabolism.hydration.value = 250;
            player.metabolism.oxygen.min = 1;
            player.metabolism.oxygen.value = 1;
            player.metabolism.poison.max = 0;
            player.metabolism.poison.value = 0;
            player.metabolism.radiation_level.max = 0;
            player.metabolism.radiation_level.value = 0;
            player.metabolism.radiation_poison.max = 0;
            player.metabolism.radiation_poison.value = 0;
            player.metabolism.temperature.min = 32;
            player.metabolism.temperature.max = 32;
            player.metabolism.temperature.value = 32;
            player.metabolism.wetness.max = 0;
            player.metabolism.wetness.value = 0;
            player.metabolism.SendChangesToClient();
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L)) return true;
            return false;
        }

        const string MainNpcName = "Тех.Админ";
        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            if (trigger == null) return;
            if (trigger.name == "QuestNPC")
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || IsNPC(player)) return;
                if (!_page.Contains(player.userID)) player.SendConsoleCommand("quest.open");
            }
            else if (trigger.name == "Pepa")
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || IsNPC(player)) return;
                if (!_page2.Contains(player.userID))
                {
                    player.SendConsoleCommand("quest.bandit");
                    _page2.Add(player.userID);
                }
            }
        }

        private void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        {
            if (trigger == null) return;
            if (trigger.name == "QuestNPC")
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || IsNPC(player)) return;
                if (_page.Contains(player.userID)) player.SendConsoleCommand("quest.exit");
            }
            else if (trigger.name == "Pepa")
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || IsNPC(player)) return;
                if (_page2.Contains(player.userID)) _page2.Remove(player.userID);
            }
        }
        List<GameObject> gameObjects = new List<GameObject>();

        private void SpawnScientist()
        {
            foreach (var z in BasePlayer.sleepingPlayerList.Where(x => IsNPC(x) && x.displayName.Equals(MainNpcName))) z.Kill();
            scientist = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", compound.transform.position + new Vector3(compound.transform.forward.x * 10f, 0.3f, compound.transform.forward.z * 8f), compound.transform.rotation, true);
            if (scientist != null)
            {
                scientist.enableSaving = false;
                scientist.Spawn();
                BasePlayer player = scientist.GetComponent<BasePlayer>();
                player.displayName = MainNpcName;
                player._name = MainNpcName;
                God(player);
                player.SendNetworkUpdateImmediate();
                Kits?.Call("GiveKit", player, "bot", true);
                Kits?.Call("GiveKit", player, "bot", 0, true);
                XKits?.Call("GiveKit", player, "bot", true);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = player.transform.position;
                TriggerBase trigger = sphere.GetComponent<TriggerBase>() ?? sphere.gameObject.AddComponent<TriggerBase>();
                trigger.interestLayers = LayerMask.GetMask("Player (Server)");
                trigger.enabled = true;
                sphere.layer = (int)Layer.Reserved1;
                sphere.name = "QuestNPC";
                SphereCollider sphereCollider = sphere.GetComponent<SphereCollider>();
                sphereCollider.radius = 3f;
                sphereCollider.isTrigger = true;
                sphereCollider.enabled = true;
                gameObjects.Add(sphere);
            }

            bandit = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", bandit_town.transform.position);
            if (bandit != null)
            {
                bandit.enableSaving = false;
                bandit.Spawn();
                BasePlayer player = bandit.GetComponent<BasePlayer>();
                player.displayName = "Авдос";
                player._name = "Авдос";
                God(player);
                player.SendNetworkUpdateImmediate();
                Kits?.Call("GiveKit", player, "bot", true);
                Kits?.Call("GiveKit", player, "bot", 0, true);
                XKits?.Call("GiveKit", player, "bot", true);
                Vector3 pos2 = bandit_town.transform.position + new Vector3(bandit_town.transform.forward.x * -19f, 1.8f, bandit_town.transform.forward.z * -22f);
                chair = GameManager.server.CreateEntity("assets/bundled/prefabs/static/chair.static.prefab", pos2);
                chair.enableSaving = false;
                chair.transform.localEulerAngles = bandit_town.transform.eulerAngles + new Vector3(0f, 45f);
                chair.Spawn();
                chair.GetComponent<BaseMountable>().MountPlayer(player);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = pos2;
                TriggerBase trigger = sphere.GetComponent<TriggerBase>() ?? sphere.gameObject.AddComponent<TriggerBase>();
                trigger.interestLayers = LayerMask.GetMask("Player (Server)");
                trigger.enabled = true;
                sphere.layer = (int)Layer.Reserved1;
                sphere.name = "Pepa";
                SphereCollider sphereCollider = sphere.GetComponent<SphereCollider>();
                sphereCollider.radius = 2f;
                sphereCollider.isTrigger = true;
                sphereCollider.enabled = true;
                gameObjects.Add(sphere);
            }
        }

        void agclose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            DestroyUI(player);
        }

        void agcloseinfo(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "questglobal");
        }

        void agopen(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            MenuLeftUI(player);
        }

        void agopeninfo(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !_page.Contains(player.userID)) return;
            GUIGlobal(player);
        }

        private bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            if (player.serverInput?.current == null) return false;
            viewAngle = Quaternion.Euler(player.serverInput.current.aimAngles);
            return true;
        }

        void revokeitem(PlayerInventory inv, string name, int amount)
        {
            List<Item> source2 = inv.FindItemIDs(FindItem(name).itemid).ToList();
            int num6 = 0;
            foreach (Item obj2 in source2)
            {
                int split_Amount = Mathf.Min(amount - num6, obj2.amount);
                (obj2.amount > split_Amount ? obj2.SplitItem(split_Amount) : obj2).DoRemove();
                num6 += split_Amount;
                if (num6 >= amount) break;
            }
        }

        #region questprogress
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (entity is BaseHelicopter)
            {
                List<BaseCombatEntity> Keys = LastHeliHit.Keys.ToList();
                if (Keys.Contains(entity))
                {
                    BasePlayer player = BasePlayer.FindByID(LastHeliHit[entity]);
                    if (player != null) ProgressQuest(player, typequest.helicopter, 1);
                    LastHeliHit.Remove(entity);
                }
            }
            if (attacker != null)
            {
                if (entity is Wolf)
                {
                    ProgressQuest(attacker, typequest.wolf, 1);
                }
                else if (entity is Bear)
                {
                    ProgressQuest(attacker, typequest.bear, 1);
                }
                else if (entity is BradleyAPC)
                {
                    ProgressQuest(attacker, typequest.tank, 1);
                }
                else if (entity is ScientistNPC)
                {
                    if (entity.name.Equals("assets/rust.ai/agents/npcplayer/humannpc/heavyscientist/heavyscientist.prefab")) ProgressQuest(attacker, typequest.heavynpc, 1);
                }
            }
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity?.ToPlayer();
            if (player != null)
            {
                if (item.info.shortname.Equals("wood")) ProgressQuest(player, typequest.wood, item.amount);
                if (item.info.shortname.Equals("stones")) ProgressQuest(player, typequest.stones, item.amount);
            }
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (player != null)
            {
                if (item.info.shortname.Equals("mushroom"))
                {
                    string user = player.UserIDString;
                    check(user);
                    if (currentquests[user].quests.Count(x => quests[x.Key].type == typequest.spoiledapple && !x.Value.finish) == 0) return;
                    float rand = Random.Range(0, 1f);
                    if (rand < 0.2f)
                    {
                        giveitem(player, "apple.spoiled", 1);
                    }
                }
                else if (item.info.shortname.Equals("wood")) ProgressQuest(player, typequest.wood, item.amount);
                else if (item.info.shortname.Equals("stones")) ProgressQuest(player, typequest.stones, item.amount);
            }
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            var player = task.owner;
            if (player != null)
            {
                if (item.info.shortname.Equals("autoturret") || item.info.shortname.Equals("samsite") || item.info.shortname.Equals("guntrap") || item.info.shortname.Equals("flameturret")) ProgressQuest(player, typequest.craftturel, item.amount);
            }
        }

        void ProgressQuest(BasePlayer player, typequest typer, int count)
        {
            string user = player.UserIDString;
            check(user);
            if (currentquests[user].quests.Count(x => quests[x.Key].type == typer && !x.Value.finish) == 0) return;
            List<string> keys = currentquests[user].quests.Keys.Where(x => quests[x].type == typer && currentquests[user].quests[x].count < quests[x].count).ToList();
            foreach (var z in keys)
            {
                currentquests[user].quests[z].count += count;
                if (currentquests[user].quests[z].count >= quests[z].count)
                {
                    GuiInfo(player, $"Задание ⟪{z}⟫ выполнено!\nЗаберите награду в городе.");
                    currentquests[user].quests[z].count = quests[z].count;
                }
                else if (typer.Equals(typequest.online))
                {
                    if (onlinetimer.ContainsKey(player.UserIDString))
                    {
                        Timer ss = onlinetimer[player.UserIDString];
                        timer.Destroy(ref ss);
                        onlinetimer.Remove(player.UserIDString);
                    }
                    onlinetimer.Add(player.UserIDString, timer.Once(60, () => ProgressQuest(player, typequest.online, 1)));
                }
            }
        }
        #endregion

        #region guiinfo
        const string GUIINFO = "[{\"name\":\"MiddlePanelquest\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.00 0.00 0.50 0.2\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.6\",\"anchormax\":\"1 .7\"}]},{\"name\":\"1c685dcb8da54498a6531a1f8c74ab27\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3 0.3 0.3 0.95\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"5ce694cf4a684bc89633e2ad1c3a2f0b\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.5\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 1\",\"distance\":\"0.6 0.6\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05 0\",\"anchormax\":\"0.95 1\"}]}]";
        Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        void GuiInfo(BasePlayer player, string text, float time = 3f)
        {
            Effect.server.Run("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player.transform.position);
            Timer ss;
            if (timers.TryGetValue(player.UserIDString, out ss)) ss?.Destroy();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "MiddlePanelquest");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIINFO.Replace("{text}", text));
            timers[player.UserIDString] = timer.Once(time, () => CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "MiddlePanelquest"));
        }
        #endregion

        private Dictionary<BaseCombatEntity, ulong> LastHeliHit = new Dictionary<BaseCombatEntity, ulong>();
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
            {
                LastHeliHit[entity] = info.InitiatorPlayer.userID;
            }
        }


        private void DestroyUI(BasePlayer player)
        {
            if (_page.Contains(player.userID)) _page.Remove(player.userID);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "questglobal");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "questmainui");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "questui");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "MiddlePanelquest");
        }

        enum typequest
        {
            wolf, wood, bot1, heavynpc, stones, cookedfish, spoiledapple, bear, craftturel, online, helicopter, tank
        }

        class quest
        {
            public typequest type;
            public int count;
            public string description;
            public List<items> rewards;
        }

        class items
        {
            public string name;
            public int count;
            public string command;
            public string image;
        }

        Dictionary<string, quest> quests = new Dictionary<string, quest>();
        void check(string user)
        {
            if (!currentquests.ContainsKey(user)) currentquests.Add(user, new baseplayer());
        }
        Dictionary<string, Timer> onlinetimer = new Dictionary<string, Timer>();
        void agquest(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !arg.HasArgs()) return;
            check(player.UserIDString);
            string name = string.Join(" ", arg.Args.Skip(0));
            if (!quests.ContainsKey(name))
            {
                GuiInfo(player, "Задание не найдено!");
                return;
            }
            if (currentquests[player.UserIDString].quests.Count() >= 5)
            {
                GuiInfo(player, "В день можно выполнить только 5 заданий.");
                return;
            }
            if (player.Distance(scientist) > 2f)
            {
                GuiInfo(player, $"Взять задание можно у {scientist.ToPlayer().displayName}а в мирном городе!");
                return;
            }
            if (currentquests[player.UserIDString].quests.ContainsKey(name))
            {
                GuiInfo(player, "Вы уже выполняете это задание!");
                return;
            }
            if (quests[name].type == typequest.bot1)
            {
                giveitem(player, "blood", 3);
            }
            if (quests[name].type == typequest.online)
            {
                if (onlinetimer.ContainsKey(player.UserIDString))
                {
                    Timer ss = onlinetimer[player.UserIDString];
                    timer.Destroy(ref ss);
                    onlinetimer.Remove(player.UserIDString);
                }

                onlinetimer.Add(player.UserIDString, timer.Once(60, () => ProgressQuest(player, typequest.online, 1)));
            }
            currentquests[player.UserIDString].quests.Add(name, new currentquest());
            MenuLeftUI(player);
            if (quests[name].type == typequest.cookedfish || quests[name].type == typequest.spoiledapple) currentquests[player.UserIDString].quests[name].count = quests[name].count;
        }

        bool haveinventory(PlayerInventory inv, string name, int count, string desc = null)
        {
            List<Item> source2 = new List<Item>();
            if (FindItem(name) != null) source2 = inv.FindItemIDs(FindItem(name).itemid).ToList();
            if (source2.Count > 0)
            {
                int num3 = source2.Sum<Item>((Func<Item, int>)(x => x.amount));
                if (num3 >= count) return true;
            }
            return false;
        }

        void agquestbandit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || arg.HasArgs()) return;
            check(player.UserIDString);
            string name = quests.Where(x => x.Value.type == typequest.bot1).FirstOrDefault().Key;
            if (player.Distance(bandit) > 2f) return;

            if (!currentquests[player.UserIDString].quests.ContainsKey(name) || currentquests[player.UserIDString].quests[name].count >= quests[name].count)
            {
                List<string> messages = new List<string>
                {
                "власть, заставляющая уважать себя на законодательном уровне, только за одно это заслуживает всего лишь презрение.",
                "актеры в советских фильмах с каждым годом играют все лучше.",
                "любой червяк может попасть в яблочко!",
                "я готовлю хреново, зато как наливаю!",
                "нa cxoдкe в Maгaданe виpyc был pacкopoнoвaн.",
                "иногда бывает так плохо, что не знаешь: или 03 набрать, или 0,5 открыть…",
                "сейчас проблема не в том, что мы пользуемся говном китайского производства, а в том, что сами и такого не производим.",
                "мой друг любит кофе, я предпочитаю чай, поэтому, когда мы встречаемся, то пьем водку.",
                "верующих много. Одни верят в Бога. Другие – в деньги. Знающих мало."
                };
                GuiInfo(player, "Авдос: " + messages[Random.Range(0, messages.Count())], 4f);
                return;
            }

            if (!haveinventory(player.inventory, "antiradpills", 1))
            {
                GuiInfo(player, "Авдос: " + "без пилюлей мне не о чем с тобой разговарить!", 3f);
                return;
            }

            if (!haveinventory(player.inventory, "blood", 3))
            {
                GuiInfo(player, "Авдос: " + "где кровь то?", 3f);
                return;
            }
            revokeitem(player.inventory, "blood", 3);
            revokeitem(player.inventory, "antiradpills", 1);
            ProgressQuest(player, typequest.bot1, 1);
        }

        void agquestrefresh(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !arg.HasArgs() || arg.Args.Length != 1) return;
            int INT;
            bool isint = int.TryParse(arg.Args[0], out INT);
            if (!isint) return;

            check(player.UserIDString);

            if (!currentquests[player.UserIDString].refresh)
            {
                if (currentquests[player.UserIDString].quests.Count() > 0 && !player.IsAdmin)
                {
                    GuiInfo(player, "Нельзя обновить задания, если у вас есть активные задания или выполненые!");
                    return;
                }
                if (!haveinventory(player.inventory, "apple", 10))
                {
                    GuiInfo(player, "Для обновления заданий нужно 10 яблок!");
                    return;
                }
                revokeitem(player.inventory, "apple", 10);
            }
            if (INT == 1) refreshlist(player.UserIDString);
            currentquests[player.UserIDString].refresh = false;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "MiddlePanelquest");
            MenuLeftUI(player);
            if (INT == 1) if (!currentquests[player.UserIDString].refresh) GuiInfo(player, "Задания обновлены!");
        }

        void refreshlist(string user)
        {
            List<string> list = quests.Keys.ToList();
            List<string> list2 = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                if (list.Count() == 0) break;
                int rand = Random.Range(0, list.Count());
                list2.Add(list[rand]);
                list.Remove(list[rand]);
            }
            currentquests[user].quests.Clear();
            currentquests[user].listquests = list2;
        }
        void agquestend(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !arg.HasArgs()) return;
            check(player.UserIDString);
            string name = string.Join(" ", arg.Args.Skip(0));
            if (!quests.ContainsKey(name))
            {
                GuiInfo(player, "Задание не найдено!");
                return;
            }
            if (!currentquests[player.UserIDString].quests.ContainsKey(name))
            {
                GuiInfo(player, "У вас нет такого задания!");
                return;
            }
            if (currentquests[player.UserIDString].quests[name].count < quests[name].count)
            {
                GuiInfo(player, "Задание не выполнено!");
                return;
            }
            if (Vector3.Distance(player.transform.position, scientist.transform.position) > 2f)
            {
                GuiInfo(player, $"Награду нужно забрать у {scientist.ToPlayer().displayName}а!");
                return;
            }
            if (currentquests[player.UserIDString].quests[name].finish)
            {
                GuiInfo(player, "Вы уже получили награду за это задание!");
                return;
            }
            if (quests[name].type == typequest.wood && !haveinventory(player.inventory, "wood", quests[name].count))
            {
                GuiInfo(player, "А где дерево? Нарубить - нарубил, а принести забыл...");
                return;
            }
            if (quests[name].type == typequest.stones && !haveinventory(player.inventory, "stones", quests[name].count))
            {
                GuiInfo(player, "А где камень? Добыть - добыл, а принести забыл...");
                return;
            }
            if (quests[name].type == typequest.cookedfish && !haveinventory(player.inventory, "fish.cooked", quests[name].count))
            {
                GuiInfo(player, "Ну и где же мой завтрак?");
                return;
            }
            if (quests[name].type == typequest.cookedfish && !haveinventory(player.inventory, "fish.cooked", quests[name].count))
            {
                GuiInfo(player, "Ну и где же мой завтрак?");
                return;
            }
            if (quests[name].type == typequest.spoiledapple && !haveinventory(player.inventory, "fertilizer", quests[name].count))
            {
                GuiInfo(player, "Принеси мне мое удобрение!");
                return;
            }

            if (quests[name].type == typequest.wood) revokeitem(player.inventory, "wood", quests[name].count);
            else if (quests[name].type == typequest.stones) revokeitem(player.inventory, "stones", quests[name].count);
            else if (quests[name].type == typequest.cookedfish) revokeitem(player.inventory, "fish.cooked", quests[name].count);
            else if (quests[name].type == typequest.spoiledapple) revokeitem(player.inventory, "fertilizer", quests[name].count);

            foreach (var z in quests[name].rewards)
            {
                if (z.command == null)
                {
                    giveitem(player, z.name, z.count);
                }
                else
                {
                    Server.Command(z.command.Replace("{steamid}", player.UserIDString));
                }
            }
            currentquests[player.UserIDString].quests[name].finish = true;
            MenuLeftUI(player);
            LogIt($"[{DateTime.Now.ToShortDateString()}] Выполнил задание - {player.displayName} ({player.UserIDString}) - {name}");
            if (quests[name].type == typequest.spoiledapple) GuiInfo(player, "Спасибо, с этим удобрением процессы мутации ускорились, как минимум на десять суток! Награда получена.", 3f);
            else GuiInfo(player, "Награда получена.", 2f);
        }

        void LogIt(string text)
        {
            logs.Add(text);
        }

        void giveitem(BasePlayer player, string name, int amount)
        {
            Item item = ItemManager.Create(FindItem(name));
            item.amount = amount;
            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
        }

        void agquestremove(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !arg.HasArgs()) return;
            check(player.UserIDString);
            string name = string.Join(" ", arg.Args.Skip(0));
            if (!quests.ContainsKey(name))
            {
                GuiInfo(player, "Задание не найдено!");
                return;
            }
            if (Vector3.Distance(player.transform.position, scientist.transform.position) > 2f)
            {
                GuiInfo(player, $"Отменить задание можно у {scientist.ToPlayer().displayName}а!");
                return;
            }
            if (!currentquests[player.UserIDString].quests.ContainsKey(name))
            {
                GuiInfo(player, "У вас нет такого задания!");
                return;
            }
            if (quests[name].type == typequest.bot1 && !haveinventory(player.inventory, "blood", 3))
            {
                GuiInfo(player, "Это задание нельзя отменить без предметов которые вам дали для выполнения его!");
                return;
            }
            if (quests[name].type == typequest.bot1)
            {
                revokeitem(player.inventory, "blood", 3);
            }
            currentquests[player.UserIDString].quests.Remove(name);
            MenuLeftUI(player);
        }

        #region GUI
        const string GLOBALGUI = "[{\"name\":\"questglobal\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1 0.1 0.1 0.98\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]}{main},{\"name\":\"ea83ada0eb804d15a57f1da9d9960136\",\"parent\":\"questglobal\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"quest.infoexit\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]}]";
        const string GLOBALMAIN = ",{\"name\":\"50dd878184b2434abd8f5a695393426c\",\"parent\":\"questglobal\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3 0.3 0.3 0.9\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.35 {end}\",\"anchormax\":\"0.7 {start}\"}]},{\"name\":\"4d1209178ec244e6a1c09421e4401856\",\"parent\":\"questglobal\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3 0.6 0.3 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.35 {end}\",\"anchormax\":\"{xmax} {start}\"}]},{\"name\":\"600b5e0444304a62a7711f74c59802f5\",\"parent\":\"questglobal\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{key}\",\"fontSize\":16,\"align\":\"MiddleLeft\",\"color\":\"1 1 1 0.5\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 1\",\"distance\":\"0.6 0.6\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.36 {end}\",\"anchormax\":\"0.7 {start}\"}]},{\"name\":\"6987ef1611f64b8fa4be65331d711cd5\",\"parent\":\"questglobal\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{count}\",\"fontSize\":16,\"align\":\"MiddleRight\",\"color\":\"1 1 1 0.5\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 1\",\"distance\":\"0.6 0.6\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.36 {end}\",\"anchormax\":\"0.69 {start}\"}]}";
        const string GLOBALNOMAIN = ",{\"name\":\"bb6154fe410c47f990078f31371ae657\",\"parent\":\"questglobal\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"У вас нет активных заданий\",\"fontSize\":16,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.5\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 1\",\"distance\":\"0.6 0.6\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4 0.55\",\"anchormax\":\"0.6 0.65\"}]}";
        private void GUIGlobal(BasePlayer player)
        {
            check(player.UserIDString);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "questglobal");
            string GUI = "";
            float start = 0.7f;
            float end = 0f;
            if (currentquests[player.UserIDString].quests.Count > 0)
            {
                foreach (var z in currentquests[player.UserIDString].quests.Where(x => !x.Value.finish))
                {
                    end = start - 0.05f;
                    GUI += GLOBALMAIN.Replace("{count}", $"{z.Value.count}/{quests[z.Key].count}").Replace("{end}", end.ToString()).Replace("{key}", z.Key).Replace("{xmax}", (0.35f + z.Value.count * 1f / quests[z.Key].count * 0.35f).ToString()).Replace("{start}", start.ToString());
                    start -= start - end + 0.005f;
                }
            }
            else GUI += GLOBALNOMAIN;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GLOBALGUI.Replace("{main}", GUI));
        }

        const string MAINGUI = "[{\"name\":\"questui\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.005 0.005 0.005 0.99\",\"fadeIn\":0.5},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"},{\"type\":\"NeedsCursor\"}]},{\"name\":\"961b572ae9bd47678cb0a9d23ea44fd2\",\"parent\":\"questui\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"quest.refresh 1\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"1.00 0.00 0.50 0.20\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.27 0.02\",\"anchormax\":\"0.42 0.07\"}]},{\"parent\":\"961b572ae9bd47678cb0a9d23ea44fd2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Обновить задания\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]},{\"name\":\"2471f5bee84246e4904e8f0eb8ab9236\",\"parent\":\"questui\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"quest.info\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"0.00 1.00 0.00 0.20\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.44 0.02\",\"anchormax\":\"0.64 0.07\"}]},{\"parent\":\"2471f5bee84246e4904e8f0eb8ab9236\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Прогресс выполнения\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]},{\"name\":\"ffe15c9be17b44a9830d55c4e0319d95\",\"parent\":\"questui\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"quest.exit\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"1.00 0.00 0.00 0.20\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.66 0.02\",\"anchormax\":\"0.8 0.07\"}]},{\"parent\":\"ffe15c9be17b44a9830d55c4e0319d95\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Выйти\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]}]";
        private void MenuLeftUI(BasePlayer player, int type = 1, int next = 0)
        {
            check(player.UserIDString);
            if (currentquests[player.UserIDString].listquests.Count == 0)
            {
                refreshlist(player.UserIDString);
                timer.Once(0.1f, () => MenuLeftUI(player, type, next));
                return;
            }
            if (!_page.Contains(player.userID))
            {
                _page.Add(player.userID);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "questui");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", MAINGUI);
            }

            var container = LMUI.CreateElementContainer("questmain", "0 0 0 0", "0 0.1", "1 1", false, "questui");
            float start = 0.98f;
            float xline = 0.1f;
            float end = 0f;
            int i = 0;
            foreach (var z in currentquests[player.UserIDString].listquests)
            {
                end = start - 0.29f * 1.1f;
                LMUI.CreatePanel(ref container, "questmain", "0.3 0.3 0.3 0.4", xline + " " + end, (xline + 0.38f) + " " + start);

                if (!currentquests[player.UserIDString].quests.ContainsKey(z)) LMUI.CreateButton(ref container, "questmain", "1.00 0.50 0.00 0.20", "Выполнить", 16, (xline + 0.3f) + " " + (start - 0.05f), (xline + 0.38f) + " " + (start - 0.01f), "quest.take " + z);
                else if (currentquests[player.UserIDString].quests.ContainsKey(z) && !currentquests[player.UserIDString].quests[z].finish && currentquests[player.UserIDString].quests[z].count < quests[z].count) LMUI.CreateButton(ref container, "questmain", "0.7 0.5 0.5 0.4", "Отказаться", 16, (xline + 0.3f) + " " + (start - 0.05f), (xline + 0.38f) + " " + (start - 0.01f), "quest.remove " + z);
                else if (currentquests[player.UserIDString].quests.ContainsKey(z) && !currentquests[player.UserIDString].quests[z].finish && currentquests[player.UserIDString].quests[z].count >= quests[z].count) LMUI.CreateButton(ref container, "questmain", "0.5 0.5 0.7 0.4", "Завершить задание", 16, (xline + 0.26f) + " " + (start - 0.05f), (xline + 0.38f) + " " + (start - 0.01f), "quest.end " + z);
                else LMUI.CreateButton(ref container, "questmain", "0.5 0.5 0.7 0.4", "Выполнено", 16, (xline + 0.3f) + " " + (start - 0.05f), (xline + 0.38f) + " " + (start - 0.01f), "");

                LMUI.CreateLabel(ref container, "questmain", "1 1 1 0.9", "<b>" + z + "</b>", 19, xline + " " + (start - 0.05f), (xline + 0.3f) + " " + (start - 0.01f), TextAnchor.MiddleCenter);
                LMUI.CreateLabel(ref container, "questmain", "1 1 1 0.5", quests[z].description, 13, (xline + 0.005f) + " " + (start - 0.15f), (xline + 0.375f) + " " + (start - 0.06f), TextAnchor.UpperLeft, "Robotocondensed-regular.ttf");
                LMUI.CreateLabel(ref container, "questmain", "1 1 1 0.5", "<color=#F0E68C>НАГРАДА ЗА ВЫПОЛНЕНИЕ ЗАДАНИЯ</color>", 13, (xline + 0.005f) + " " + (start - 0.18f), (xline + 0.375f) + " " + (start - 0.15f), TextAnchor.MiddleCenter, "Droidsansmono.ttf");

                float Xstart = 0.04f;
                float Xend = 0f;
                foreach (var x in quests[z].rewards)
                {
                    Xend = Xstart + 0.05f;
                    LMUI.CreatePanel(ref container, "questmain", "0.3 0.3 0.3 0.4", (xline + Xstart) + " " + (start - 0.28f), (xline + Xend) + " " + (start - 0.19f));
                    LMUI.LoadImage(ref container, "questmain", GetImage(x.command == null ? x.name : x.image), (xline + Xstart + 0.005f) + " " + (start - 0.27f), (xline + Xend - 0.005f) + " " + (start - 0.2f));
                    LMUI.CreateLabel(ref container, "questmain", "1 1 1 0.5", "x" + x.count, 14, (xline + Xstart) + " " + (start - 0.28f), (xline + Xend - 0.001f) + " " + (start - 0.19f), TextAnchor.LowerRight);
                    Xstart += Xend - Xstart + 0.01f;
                }

                if (i == 2)
                {
                    xline = 0.52f;
                    start = 0.98f;
                }
                else start -= start - end + 0.01f;
                i++;
            }
            CuiHelper.DestroyUi(player, "questmain");
            CuiHelper.AddUi(player, container);
            if (currentquests[player.UserIDString].refresh)
            {
                Effect.server.Run("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player.transform.position);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "MiddlePanelquest");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", REFRESHUI);
            }
        }

        const string REFRESHUI = "[{\"name\":\"MiddlePanelquest\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1 0.1 0.1 0.98\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"db7b79d51fc348fe8f594c9270c06b28\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.005 0.005 0.005 0.9\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"e8a1339c1c8849fa97ac73922190e28b\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3 0.3 0.3 0.9\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.55\",\"anchormax\":\"1 0.7\"}]},{\"name\":\"fa329e2eacc545f5a4c524272654748d\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Доступно обновление квестов, обновление сбросит прогресс текущих заданий, обновить?\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.5\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 1\",\"distance\":\"0.6 0.6\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05 0.6\",\"anchormax\":\"0.95 0.7\"}]},{\"name\":\"81a491a92be54c4f86360124fb555204\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"quest.refresh 1\",\"color\":\"0.3 0.7 0.3 0.8\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4 0.57\",\"anchormax\":\"0.48 0.61\"}]},{\"parent\":\"81a491a92be54c4f86360124fb555204\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Да\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]},{\"name\":\"04a8e472463c47ff8a55ba274bc28428\",\"parent\":\"MiddlePanelquest\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"quest.refresh 0\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"0.7 0.3 0.3 0.8\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.52 0.57\",\"anchormax\":\"0.6 0.61\"}]},{\"parent\":\"04a8e472463c47ff8a55ba274bc28428\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Нет\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]}]";
        #endregion

        private void Unload()
        {
            if (scientist != null) scientist.Kill();
            if (bandit != null) bandit.Kill();
            if (chair != null) chair.Kill();
            foreach (var z in gameObjects.ToList()) UnityEngine.GameObject.Destroy(z);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "questmain");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "questglobal");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "questmainui");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "questui");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "MiddlePanelquest");
            SaveData();
            SaveQuests();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("quest_players", currentquests);
        private void SaveQuests()
        {
            Interface.Oxide.DataFileSystem.WriteObject("quest_data", quests);
            Interface.Oxide.DataFileSystem.WriteObject("quest_logs", logs);
        }
        private void chatcommandquest(BasePlayer player, string cmd)
        {
            MenuLeftUI(player);
        }

        private void timernextday()
        {
            //86400 - DateTime.UtcNow.AddHours(3).Hour * 3600 - DateTime.UtcNow.Minute * 60 - DateTime.UtcNow.Second
            timer.Once(86400 - DateTime.UtcNow.AddHours(3).Hour * 3600 - DateTime.UtcNow.Minute * 60 - DateTime.UtcNow.Second, () =>
            {
                List<string> zk = currentquests.Keys.ToList();
                foreach (var z in zk)
                {
                    if (!currentquests[z].refresh) currentquests[z].refresh = true;
                }
                SaveData();
                timernextday();
            });
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (onlinetimer.ContainsKey(player.UserIDString))
            {
                Timer ss = onlinetimer[player.UserIDString];
                timer.Destroy(ref ss);
                onlinetimer.Remove(player.UserIDString);
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            ProgressQuest(player, typequest.online, 1);
        }

        static List<string> logs = new List<string>();
        private void OnServerInitialized()
        {
            LoadConfig();
            currentquests = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, baseplayer>>("quest_players");
            logs = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("quest_logs");
            quests = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, quest>>("quest_data");

            if (quests.Count() == 0)
            {
                quests.Add("Скрывающийся оборотень", new quest { type = typequest.wolf, count = 5, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Так как вампиры появляются ночью, у них случаются столкновения с другими ночными существами, поэтому в городе стало неспокойно. Если хотите, вы можете восстановить былое спокойствие в городе. Если вы зажжете факел, найдете 5 волков и убьете, то в городе воцарится спокойствие." });
                quests.Add("Сбор ресурсов", new quest { type = typequest.wood, count = 10000, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Бедные фермеры... Да, конечно, они трусоваты. Но они ведь попросту не умеют сражаться, а им нужно заготавливать древесину для печек. Рейдеры нападают на них... все это просто ужасно! Но если мы хотим продержаться, нам нужна эта древесина. Отправляйся в лес, собери 10 тысяч древесины и доставь ее сюда." });
                quests.Add("Чума Отрекшихся", new quest { type = typequest.bot1, count = 1, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Так вот; отнеси эту штуку обратно в лагерь бандитов и отдай Пеппи Нетосопло. Она, конечно, не самый лучший алхимик, но другого у нас нет. Только учти – она очень любит противорадиационные таблетки, так что для начала загляни в магазинчик либо пошарься по помойкам. В общем, она знает, что делать с этой штукой." });
                quests.Add("Сбор ресурсов II", new quest { type = typequest.stones, count = 12500, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Для обеспечения и улучшения защиты города требуются новые ресурсы. Неподалёку от города обнаружены залежи камня. Соберите 12'500 камня и принесите его нам." });
                quests.Add("Древний фрагмент", new quest { type = typequest.heavynpc, count = 3, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { command = "ag.give {steamid} 1 Фрагмент для VIP", image = "https://gspics.org/images/2019/03/16/m5LsD.png", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "У меня есть древний фрагмент, который перешёл ко мне от великого предка. Я мог бы поделится им  в обмен на услугу. Приплыви к нефтяной вышки и уничтожь 3-х тяжелых NPC. Не спрашивай зачем мне это нужно, просто выполни поручение." });
                quests.Add("Довольно грибов!", new quest { type = typequest.cookedfish, count = 5, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { command = "ag.give {steamid} 1 Фрагмент для VIP", image = "https://gspics.org/images/2019/03/16/m5LsD.png", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Грибной суп, жаркое из грибов, грибной соус... Грибы, грибы, грибы! Кроме грибов, есть тут вообще нечего! Так вот, меня это достало. Я знаю: тут, в озерах, водится рыба! Сколько ты потребуешь с меня за 5 приготовленных рыбин, чтобы я впервые за несколько недель мог нормально поесть?" });
            }

            if (quests.Count() < 12)
            {
                if (quests.ContainsKey("Эффективное удобрение")) quests.Remove("Эффективное удобрение");
                if (quests.ContainsKey("Месть Вайрины")) quests.Remove("Месть Вайрины");
                if (quests.ContainsKey("Легендарное орудие")) quests.Remove("Легендарное орудие");
                if (quests.ContainsKey("Время для наград")) quests.Remove("Время для наград");
                if (quests.ContainsKey("Тяжелая артиллерия")) quests.Remove("Тяжелая артиллерия");
                if (quests.ContainsKey("Важный груз")) quests.Remove("Важный груз");
                quests.Add("Эффективное удобрение", new quest { type = typequest.spoiledapple, count = 5, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Буквально несколько дней назад мои собратья грибники нашли отличные удобрения на грибных лужайках,  которые, на мой взгляд, идеально подойдут для ускорения мутации моих цветов. Принеси мне 5 гнилых яблок и тогда я тебя отблагодарю." });
                quests.Add("Месть Вайрины", new quest { type = typequest.bear, count = 1, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Год назад весь Приют странников охотился на огромного медведя, но ни одному смельчаку не посчастливилось его одолеть. Тогда Дерил решил выступить против зверя сам. Разумеется, он ничего не добился. Убей медведя, и не сомневаюсь, что даже Дерил ненадолго лишится дара речи." });
                quests.Add("Легендарное орудие", new quest { type = typequest.craftturel, count = 1, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Четыре  орудия... четыри великих пути. Скрафтить одно из этих орудий – первый шаг к победе над Рейдерами. Со временем мощь оружия в твоих руках будет расти, но тем временем тебе в этом будут помогать твои верные друзья. Если захочешь, позже сможешь раздобыть и три других. Но первое орудие важно раздобыть сейчас." });
                quests.Add("Время для наград", new quest { type = typequest.online, count = 60, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Тут все просто, просто проведи на сервере 1 час и получи в замен ништячки. " });
                quests.Add("Тяжелая артиллерия", new quest { type = typequest.tank, count = 1, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "У меня дурные вести: у повстанцев появилась серьезная огневая мощь. Мы видели танк на космодроме, он патрулирует полностью всё РТ  и зачищает местность от выживших. Нельзя, чтобы это продолжалось! А может, дадим им попробовать свинца? Представляешь, какую бойню ты им тогда устроишь?" });
                quests.Add("Важный груз", new quest { type = typequest.helicopter, count = 1, rewards = new List<items>() { new items { name = "rifle.ak", count = 1 }, new items { name = "wood", count = 5000 }, new items { name = "wood", count = 5000 } }, description = "Все пропало! Мой вертолёт... его сперли! Я говорил капитанше, что с этим бесценным грузом нужно быть осторожнее, но разве она стала меня слушать?! О, нет! Груз не должен никому достаться, ты слышишь меня? НИКОМУ!!! Отправляйся в путь и уничтожь вертолет любой ценой. Возможно, это наша последняя надежда." });
            }

            foreach (var z in quests)
            {
                foreach (var x in z.Value.rewards)
                {
                    if (x.image != null) AddImage(x.image, x.image);
                    else GetImage(x.name);
                }
            }

            timernextday();
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.exit", this, "agclose");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.open", this, "agopen");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddChatCommand("quest", this, "chatcommandquest");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.take", this, "agquest");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.info", this, "agopeninfo");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.infoexit", this, "agcloseinfo");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.remove", this, "agquestremove");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.end", this, "agquestend");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.bandit", this, "agquestbandit");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("quest.refresh", this, "agquestrefresh");
            foreach (var z in BasePlayer.activePlayerList) ProgressQuest(z, typequest.online, 1);

            foreach (MonumentInfo info in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (info.name.Equals("assets/bundled/prefabs/autospawn/monument/medium/compound.prefab")) compound = info;
                else if (info.name.Equals("assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab")) bandit_town = info;
            }

            timer.Once(5f, () => SpawnScientist());

            Puts("ЗАПУСКАЕМ КВЕСТ МАШИНУ ДЫР-ДЫР-ВЖУХ!");
        }

        MonumentInfo compound;
        MonumentInfo bandit_town;

        #region UI
        class LMUI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
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
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel, CuiHelper.GetGuid());
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, string font = "RobotoCondensed-Bold.ttf", float fadeIn = 0f, float fadeout = 0f)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadeIn, Text = text, Font = font },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    FadeOut = fadeout
                },
                panel, CuiHelper.GetGuid());

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadeIn = 0f, float fade = 0f, string material = "assets/content/ui/uibackgroundblur-ingamemenu.mat")
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadeIn, Material = material },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align },
                    FadeOut = fade
                },
                panel, CuiHelper.GetGuid());
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void OutlineText(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, string colorout = "0 0 0 1", float fadeIn = 0f)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiTextComponent {Color = color, FontSize = size, Align = align, FadeIn = fadeIn, Text = text },
                        new CuiOutlineComponent { Distance = "0.6 0.6", Color = colorout },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateInput(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, int chars = 100, TextAnchor align = TextAnchor.UpperLeft)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Color = color,
                            Text = text,
                            FontSize = size,
                            Command = command,
                            CharsLimit = chars,
                            Align = align,
                            IsPassword = false
                        },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
        }
        #endregion
    }
}