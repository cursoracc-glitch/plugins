using UnityEngine;
using Oxide.Core;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{

    [Info("ShiningSeed", "Anonymuspro", "0.0.2")]
    public class ShiningSeed : RustPlugin
    {
        #region Class

        public class Seed
        {
            public string shortname;
            public string name;
            public ulong skinId;
        }

        Seed seed = new Seed()
        {
            shortname = "seed.hemp",
            name = "Лайтинг семечка",
            skinId = 1787823357
        };

        public class Wood
        {
            [JsonProperty("Не трогать!")]
            public BasePlayer player;

            [JsonProperty("Не трогать!")]
            public BaseEntity wood;

            public Vector3 woodPos;

            public PlantEntity plant;

            public List<BaseEntity> boxs;

            public int BoxCount = 4;

            public bool DownBox;

            public bool isSpawned;

            public int CurrentMaturation = 0;

            public int MaxMaturation = 10;

            public int BoxMat;

            public int BoxMaxMat = 10;

            public int CurrentEtap;

            public int MaxEtap;

        }

        public class CaseItem
        {
            public string shortname;
            public bool Random;
            public int Min;
            public int Max;
            public int Count;
        }

        public List<Wood> woods;

        public Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"CmdError", "Неправильно ввели команду." },
            {"CountError", "Неверное кол-во!" },
            {"Permission", "У вас нет прав!" },
            {"SeedGived", "Вам выпала уникальная семечка!\nПосадите ее и у вас выростет дерево с лутом!" },
            {"Wood", "Вы посадили уникальное дерево\nСкоро оно вырастет" },
            {"Matured",  "Ваше магическое дерево созрело!\nСкоро на нем начнут рости ящики"},
            {"Box", "На дереве еще нет ящиков,\nВы не можете его срубить!" },
            {"Seed", "Ваш лайтинг саженец сломали!"},
            {"Destroy", "Это магическое дерево\nЕго можно только добыть когда оно созреет!" }
        };

        public class DataConfig
        {
            [JsonProperty("Время роста дерева в секундах")]
            public int Time;

            [JsonProperty("Количество вещей в ящике")]
            public int ItemsCount;

            [JsonProperty("Кол-во ящиков на дереве")]
            public int BoxCount;

            [JsonProperty("Время появления ящиков в секундах")]
            public int BoxTime;

            [JsonProperty("Максимальное кол-во этапов для дерева")]
            public int MaxEtap;

            [JsonProperty("Список префабов этапов дерева")]
            public List<string> etaps;

            [JsonProperty("Права на выдачу")]
            public string Permission = "seed.perm";

            [JsonProperty("Путь до ящика")]
            public string CrateBasic = "assets/bundled/prefabs/radtown/crate_basic.prefab";

            [JsonProperty("Шанс выпдаения с дерева (макс-100)")]
            public int Chance;

            [JsonProperty("Список вещей в ящиках")]
            public List<CaseItem> casesItems;

            [JsonProperty("Ссылка на удачный эффект")]
            public string SucEffect;
            [JsonProperty("Ссылка на эффект ошибки")]
            public string ErrorEffect;

            [JsonProperty("Эффект сообщения после спавна для игрока")]
            public string WoodMessageEffect = "";

            [JsonProperty("Размер шрифта")]
            public int FontSize;

            [JsonProperty("Цвет текста")]
            public string Color;

            [JsonProperty("Позиция текста на дереве")]
            public Vector3 pos;

            [JsonProperty("Размер шрифта процентов")]
            public int FontProcentSize;

            [JsonProperty("Размер шрифта этапов")]
            public int FontEtapSize;
        }

        DataConfig cfg;

        #endregion

        #region DefaultMethods

        protected override void LoadConfig()
        {
            base.LoadConfig();
            cfg = Config.ReadObject<DataConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            cfg = new DataConfig()
            {
                BoxTime = 10,
                ItemsCount = 2,
                Permission = "seed.perm",
                CrateBasic = "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",
                BoxCount = 4,
                Chance = 5,
                Time = 10,
                casesItems = new List<CaseItem>()
                {
                new CaseItem
                {
                shortname = "stone",
                Random = true,
                Min = 300,
                Max = 1000,
                Count = 550
                },
                },
                SucEffect = "assets/prefabs/misc/xmas/candy cane club/effects/hit.prefab",
                ErrorEffect = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                WoodMessageEffect = "assets/prefabs/tools/keycard/effects/swipe.prefab",
                FontSize = 30,
                Color = "#DEE2E5",
                pos = new Vector3(1.5f, 0, 0),
                etaps = new List<string>()
                {
                  "assets/bundled/prefabs/autospawn/resource/v2_temp_forest_small/pine_d.prefab",
                  "assets/bundled/prefabs/autospawn/resource/v2_temp_forest_small/douglas_fir_d.prefab",
                  "assets/bundled/prefabs/autospawn/resource/v2_tundra_forest/oak_b_tundra.prefab"
                },
                MaxEtap = 3,
                FontEtapSize = 18,
                FontProcentSize = 22
            };
        }

        #endregion

        #region OxideHooks

        private void Init()
        {
            permission.RegisterPermission(cfg.Permission, this);
            woods = new List<Wood>();
            timer.Every(1f, TimerTick);
            timer.Every(0.2f, UpdateText);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;

            Wood wd2 = SearchFromEntity(entity as PlantEntity);
            if (wd2 != null)
            {
                BasePlayer player = info.Initiator.ToPlayer();
                if (player != null)
                {
                    if (wd2.player != null)
                    {
                        if (wd2.player != player)
                        {
                            SendReply(wd2.player, string.Format(Messages["Seed"]));
                            Effect.server.Run(cfg.ErrorEffect, wd2.player.ServerPosition);
                        }
                    }
                }
                woods.Remove(wd2);
                return;
            }

            foreach (var wood in woods)
            {
                if (wood.wood == null) return;
                if (wood.wood == entity)
                {
                    woods.Remove(wood);
                    return;
                }
            }
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            var entity = go?.GetComponent<BaseEntity>();
            Vector3 ePos = entity.transform.position;
            if (entity == null || ePos == null || player == null) return;
            if (entity.skinID == seed.skinId)
            {

                woods.Add(new Wood
                {

                    woodPos = ePos,
                    plant = entity as PlantEntity,
                    BoxCount = cfg.BoxCount,
                    isSpawned = false,
                    CurrentMaturation = 0,
                    DownBox = false,
                    MaxMaturation = cfg.Time,
                    BoxMaxMat = cfg.BoxTime,
                    BoxMat = 0,
                    player = player,
                    MaxEtap = cfg.MaxEtap,
                    CurrentEtap = 0,
                }
                );

                SendReply(player, string.Format(Messages["Wood"]));
                return;
            }
        }

        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity?.ToPlayer();
            if (player == null) return null;

            switch (item.info.shortname)
            {
                case "wood":
                    if (UnityEngine.Random.Range(0f, 350f) < cfg.Chance)
                    {
                        AddSeed(player);
                    }
                    BaseEntity wood1 = dispenser.GetComponentInParent<TreeEntity>();

                    List<Wood> list = new List<Wood>();
                    foreach (var wood in woods)
                    {
                        if (wood == null || wood.wood == null) return null;
                        if (wood.DownBox) return null;
                        if (wood.wood == wood1)
                        {
                            if (wood.boxs.Count == 0)
                            {
                                SendReply(player, string.Format(Messages["Box"]));
                                return false;
                            }
                            foreach (var box in wood.boxs)
                            {
                                FreeableLootContainer l = box.GetComponent<FreeableLootContainer>();
                                l.SetFlag(BaseEntity.Flags.Reserved8, false, true);
                                l.SendNetworkUpdate();
                                box.SetFlag(BaseEntity.Flags.Reserved8, false, true);
                                box.SendNetworkUpdate();
                                Rigidbody rb = box.GetComponent<Rigidbody>();
                                rb.useGravity = true;
                                rb.mass = 4f;
                                rb.isKinematic = false;
                                box.SendNetworkUpdate();
                                wood.DownBox = true;
                            }
                            woods.Remove(wood);
                            return null;
                        }
                    }
                    break;
            }
            return null;
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                Item x = player.inventory.FindItemID(seed.shortname);
                if (x != null)
                {
                    x.RemoveFromContainer();
                    x.Remove(0f);
                }
            }
            foreach (var wood in woods)
            {
                if (wood.plant != null)
                    wood.plant.Kill();

                if (wood.boxs != null)
                {
                    foreach (var box in wood.boxs)
                    {
                        box.Kill();
                    }
                }

                if (wood.wood != null)
                    wood.wood.Kill();
            }
        }

        #endregion

        #region MyMethods

        #region Timer
        public void TimerTick()
        {
            if (woods.Count > 0 || woods != null)
            {
                woods.ForEach(wood => WoodRost(wood));
            }
        }

        #endregion

        #region Wood

        public void WoodRost(Wood wood)
        {
            if (wood == null) return;

            if (wood.CurrentEtap < wood.MaxEtap)
            {
                wood.isSpawned = false;
                if (wood.CurrentMaturation < wood.MaxMaturation)
                {
                    wood.CurrentMaturation++;
                    if (!wood.isSpawned)
                    {
                        if (wood.CurrentEtap == 0)
                        {
                            wood.plant.growthAge = 1f;
                            wood.plant.state = PlantProperties.State.Seedling;
                            wood.plant.SendNetworkUpdate();
                        }
                    }
                }
                else
                {
                    if (!wood.isSpawned)
                    {
                        SpawnWood(wood, wood.CurrentEtap);
                    }
                    wood.CurrentMaturation = 0;
                    wood.CurrentEtap++;
                    return;
                }
            }
            else
            {
                if (wood.isSpawned)
                {
                    if (!wood.DownBox)
                    {
                        int count = wood.boxs.Count - 1;
                        if (wood.boxs.Count < cfg.BoxCount)
                        {
                            if (wood.BoxMat < wood.BoxMaxMat)
                            {
                                wood.BoxMat++;
                            }
                            else
                            {
                                wood.BoxMat = 0;
                                Puts("Спавню ящики");
                                SpawnBox(wood, count);
                            }
                        }
                    }
                    return;

                }
            }
        }

        public void SpawnWood(Wood wood, int etap)
        {
            if (wood == null) return;

            if (etap > 0)
                wood.wood.Kill();

            BaseEntity Wood = GameManager.server.CreateEntity(cfg.etaps[etap], wood.woodPos);
            wood.plant.Kill();
            Wood.Spawn();
            wood.wood = Wood;
            wood.boxs = new List<BaseEntity>();
            wood.isSpawned = true;
            Wood.SendNetworkUpdate();
        }


        #endregion

        #region Seed

        [ChatCommand("seed")]
        void GiveSeed(BasePlayer player, string command, string[] args)
        {
            if (player.IPlayer.HasPermission(cfg.Permission))
            {
                if (player.IsAdmin)
                    player.IPlayer.GrantPermission(cfg.Permission);
                else
                {
                    SendReply(player, string.Format(Messages["Permission"]));
                    Effect.server.Run(cfg.ErrorEffect, player, 0, Vector3.zero, Vector3.forward);
                    return;
                }
            }
            if (player == null) return;
            if (args.Length != 0)
            {
                SendReply(player, string.Format(Messages["CmdError"]));
                Effect.server.Run(cfg.ErrorEffect, player, 0, Vector3.zero, Vector3.forward);
                return;
            }
            AddSeed(player);
        }

        void AddSeed(BasePlayer player)
        {
            if (player == null) return;
            Item sd = ItemManager.CreateByName(seed.shortname, 1, seed.skinId);
            sd.name = seed.name;
            player.GiveItem(sd, BaseEntity.GiveItemReason.Crafted);
            SendReply(player, string.Format(Messages["SeedGived"]));
            Effect.server.Run(cfg.SucEffect, player, 0, Vector3.zero, Vector3.forward);
        }

        #endregion

        #region Boxs

        public void SpawnBox(Wood wood, int i)
        {
            if (wood == null) return;
            if (wood != null)
            {
                Vector3 pos = wood.wood.ServerPosition;
                pos.y += UnityEngine.Random.Range(4.9f, 5.0f);
                pos.x += UnityEngine.Random.Range(-6, 6 + i);
                pos.z += UnityEngine.Random.Range(-i, 6);
                BaseEntity box = GameManager.server.CreateEntity(cfg.CrateBasic, pos);
                wood.boxs.Add(box);
                box.Spawn();
                AddLoot(box);
                box.SendNetworkUpdate();
            }
        }

        public void AddLoot(BaseEntity box)
        {
            if (box == null) return;
            LootContainer container = box.GetComponent<LootContainer>();
            container.inventory.itemList.Clear();
            for (int i = 0; i < cfg.ItemsCount; i++)
            {
                CaseItem c = cfg.casesItems[UnityEngine.Random.Range(0, cfg.casesItems.Count - 1)];
                int Count = 0;
                if (c.Random)
                {
                    Count = UnityEngine.Random.Range(c.Min, c.Max);
                }
                else
                {
                    Count = c.Count;
                }
                Item x = ItemManager.CreateByName(c.shortname, Count);
                x.MoveToContainer(container.inventory, -1);
            }
        }

        #endregion

        #region Other

        Wood SearchFromEntity(PlantEntity entity)
        {
            if (entity == null) return null;
            foreach (var wood in woods)
            {
                if (wood.plant == entity)
                {
                    return wood;
                }
            }
            return null;
        }

        public void UpdateText()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player != null && !(player is BaseNpc))
                {
                    foreach (var wood in woods)
                    {
                        if (wood != null)
                        {
                            if (wood.wood != null)
                            {
                                float distance = Vector3.Distance(player.ServerPosition, wood.wood.ServerPosition);
                                if (distance <= 4.2f)
                                {
                                    ShowText(wood, player);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ShowText(Wood wood, BasePlayer player)
        {
            if (wood == null || wood.wood == null || player == null) return;
            string text = "";
            string color = "#adadad";
            if (wood.CurrentEtap < wood.MaxEtap) text = $"<size={cfg.FontSize}><color={color}>ЛАЙТИНГ ДЕРЕВО</color></size>\n\n<size={cfg.FontEtapSize}>Стадия: {wood.CurrentEtap} / {wood.MaxEtap}</size>";
            else text = $"<size={cfg.FontSize}><color={color}>ЛАЙТИНГ ДЕРЕВО</color></size>\n<size={cfg.FontProcentSize}>Ящики: {wood.boxs.Count} / {cfg.BoxCount}</size>\n\n<size={cfg.FontEtapSize}>Стадия: {wood.CurrentEtap} / {wood.MaxEtap}</size>";

            player.SendConsoleCommand("ddraw.text", 0.3f, cfg.Color, wood.wood.transform.position + cfg.pos, text);
        }

        #endregion


        #endregion

    }
}