using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SeedEvent", "Own3r/Nericai", "1.1.0")]
    [Description("Ивент позволяет выращивать камни на грядках")]
    class SeedEvent : RustPlugin
    {
        private class Random
        {
            [JsonProperty("Название ресурса")] public string DisplayName;

            [JsonProperty("Название объекта (не менять)")]
            public string Prefabs;

            [JsonProperty("Используется в рандоме")]
            public bool Active;
        }

        private class PlantSeedConfig
        {
            [JsonProperty("Время прорастания семечки")]
            public int TimeToUp;

            [JsonProperty("Список выращиваемых предметов")]
            public List<Random> RandomList;

            [JsonProperty("Skin семечки")] public ulong SkinIdPlant;

            [JsonProperty("Максимальное ХП камня (Стандартное 1)")]
            public float startHealth;
        }

        private PlantSeedConfig config;

        protected override void LoadDefaultConfig()
        {
            config = new PlantSeedConfig
            {
                TimeToUp = 20,
                RandomList = new List<Random>
                {
                    new Random
                    {
                        DisplayName = "Металл",
                        Prefabs = "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab",
                        Active = true
                    },
                    new Random
                    {
                        DisplayName = "Сера",
                        Prefabs = "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab",
                        Active = true
                    }
                },
                SkinIdPlant = 1562930487,
                startHealth = 1f
            };
            SaveConfig();
            PrintWarning("Создаем дефолтный конфиг");
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PlantSeedConfig>();
        }

        private void SendInfo(BasePlayer player, string message)
        {
            player.SendConsoleCommand("gametip.showgametip", message);
            timer.Once(3f, () => player.SendConsoleCommand("gametip.hidegametip"));
        }

        void OnServerInitialized()
        {
            if (plugins.Exists("Stacks") || plugins.Exists("Stacks")) UnsubscribeSplit();
            else SubscribeSplit();
            permission.RegisterPermission("seedevent.allow", this);
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (!go.name.Contains("corn")) return;
            var player = plan.GetOwnerPlayer();
            var isSeed = go.GetComponent<PlantEntity>();
            if (player == null || isSeed == null || isSeed.skinID != config.SkinIdPlant) return;
            if (!permission.UserHasPermission(player.UserIDString, "seedevent.allow"))
            {
                isSeed.Kill();
                player.inventory.GiveItem(ItemManager.CreateByName("clone.corn", 1, config.SkinIdPlant));
                SendInfo(player, "Вы не можете делать этого!");
                return;
            }

            NextTick(() =>
            {
                if (isSeed.skinID != config.SkinIdPlant) return;
                if (!(isSeed.GetParentEntity() is PlanterBox))
                {
                    SendInfo(player, "Вы можете посадить это семечко только на грядках");
                    isSeed.Kill();
                    player.inventory.GiveItem(ItemManager.CreateByName("clone.corn", 1, config.SkinIdPlant));
                    return;
                }

                var ent = go.ToBaseEntity();
                var coords = ent.transform.position;
                var ore = GameManager.server.CreateEntity(GetRandomOre(), coords) as OreResourceEntity;
                if (ore != null)
                {
                    isSeed.Kill();
                    ore.Spawn();
                    ore.gameObject.AddComponent<BaseCombatEntity>();
                    ore.GetComponent<BaseCombatEntity>().InitializeHealth(0.1f, config.startHealth);
                    UpdateVisible(ore.GetComponent<StagedResourceEntity>());
                    StartTimerToThis(ore);
                }

                SendInfo(player, $"Вы успешно посадили семечку, она прорастет через {config.TimeToUp} секунд");
            });
        }

        private void GiveSeeds(BasePlayer player)
        {
            Item x = ItemManager.CreateByName("clone.corn", 1, config.SkinIdPlant);
            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
        }

        private string GetRandomOre()
        {
            return config.RandomList.Where(p => p.Active).ToList().GetRandom().Prefabs;
        }

        private void StartTimerToThis(OreResourceEntity ore)
        {
            var stageComponent = ore.GetComponent<StagedResourceEntity>();
            var healthComponent = ore.GetComponentInParent<BaseCombatEntity>();
            var resComponent = ore.GetComponentInParent<ResourceDispenser>();
            resComponent.containedItems.ForEach(x => x.amount = x.startAmount / (config.startHealth / 0.1f));
            timer.Once(config.TimeToUp / 10f, () => Iterac(ore, stageComponent, healthComponent, resComponent));
        }

        private void Iterac(OreResourceEntity ore, StagedResourceEntity stageComponent,
            BaseCombatEntity healthComponent, ResourceDispenser resComponent)
        {
            if (ore == null || stageComponent == null || healthComponent == null) return;
            healthComponent.health += 0.1f;
            resComponent.containedItems.ForEach(x => x.amount += x.startAmount / (config.startHealth / 0.1f));
            UpdateVisible(stageComponent);
            if (healthComponent.health >= healthComponent.MaxHealth())
            {
                ore.CancelInvoke();
                return;
            }

            timer.Once(config.TimeToUp / 10f, () => Iterac(ore, stageComponent, healthComponent, resComponent));
        }

        private void UpdateVisible(StagedResourceEntity stageComponent)
        {
            var newStage =
                stageComponent.stages.FirstOrDefault(p =>
                    p.health <= stageComponent.GetComponentInParent<BaseCombatEntity>().health) ??
                stageComponent.stages.First();
            stageComponent.stage = stageComponent.stages.IndexOf(newStage);
            newStage.instance.SetActive(true);
            GroundWatch.PhysicsChanged(stageComponent.gameObject);
            stageComponent.SendNetworkUpdate();
        }

        void UnsubscribeSplit()
        {
            Unsubscribe(nameof(OnItemSplit));
            Unsubscribe(nameof(CanStackItem));
        }

        void SubscribeSplit()
        {
            Subscribe(nameof(OnItemSplit));
            Subscribe(nameof(CanStackItem));
        }

        object OnItemSplit(Item thisI, int split_Amount)
        {
            if (thisI.info.itemid != -778875547 && thisI.skin != config.SkinIdPlant) return null;
            Item item = null;
            item = ItemManager.CreateByItemID(thisI.info.itemid, split_Amount, thisI.skin);
            if (item != null)
            {
                thisI.amount -= split_Amount;
                thisI.MarkDirty();
                item.amount = split_Amount;
                item.OnVirginSpawn();
                item.MarkDirty();
                return item;
            }

            return null;
        }

        object CanStackItem(Item thisI, Item item)
        {
            if (thisI.skin == 774) return null;
            if (thisI.skin != item.skin) return false;
            if (thisI.info.itemid != -778875547 && thisI.skin != config.SkinIdPlant) return null;
            if (thisI.skin == item.skin && thisI.skin == config.SkinIdPlant) return true;
            return null;
        }

        [ConsoleCommand("seedevent")]
        private void cmdChange(ConsoleSystem.Arg args)
        {
            ulong pid;
            int count;
            if (!args.IsAdmin && !args.IsRcon && !args.IsServerside) return;
            if (args.Args[0] == null || args.Args[1] == null || args.Args.Length != 2) return;
            if (!ulong.TryParse(args.Args[0], out pid)) return;
            BasePlayer player = BasePlayer.FindByID(pid);
            if (player == null)
            {
                SendReply(player, $"Игрок со SteamID {pid} не найден.");
                return;
            }

            if (!int.TryParse(args.Args[1], out count)) count = 1;

            Item rec = ItemManager.CreateByName("clone.corn", count, config.SkinIdPlant);
            rec.MoveToContainer(player.inventory.containerMain);
            SendReply(player, $"Вы получили {count} семечка.");
        }
    }
}