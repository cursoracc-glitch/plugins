using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// https://server-rust.ru/resources/galaxychest.892/

namespace Oxide.Plugins
{
    [Info("GalaxyChest", "123", "1.0.0")]
    [Description("GalaxyChest")]
    class GalaxyChest : RustPlugin
    {
        #region Classes
        private class PluginConfig
        {
            public ulong ChestSkin = 1852572105;
            public ulong DustSkin = 1853777192;
            public float TimeOpen = 10f;
            public Dictionary<string, DustSett> dustDrops;
            public Dictionary<string, PrizeSett> prizeSett;
        }

        private class DustSett
        {
            public int Amount;
            public int Chance;
        }

        private class ChestData
        {
            public double OpenTime;
            public uint NetId;
        }

        private class PrizeSett
        {
            public int MinValue = 5;
            public int MaxValue = 15;
            public double Chance = 100;
        }
        #endregion

        #region Variables
        private Dictionary<ulong, ChestData> chestsData;
        private PluginConfig config;
        private string DataPath = "GalaxyChest/Data";
        static GalaxyChest instance;
        GameObject controller;
        #endregion

        #region Oxide
        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        void OnServerInitialized()
        {
            instance = this;

            chestsData = new Dictionary<ulong, ChestData>();
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(DataPath))
                chestsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ChestData>>(DataPath);

            controller = new GameObject();
            ChestUpdator component;
            if (controller != null)
            {
                component = controller.GetComponent<ChestUpdator>();
                if (component != null) UnityEngine.Object.Destroy(component);
            }
            controller.AddComponent<ChestUpdator>();

            
            PrintWarning("Plugin loaded! Author: BadMandarin!");
        }

        void Unload()
        {
            if (controller != null)
            {
                var component = controller.GetComponent<ChestUpdator>();
                if (component != null) UnityEngine.Object.Destroy(component);
            }
            Interface.Oxide.DataFileSystem.WriteObject(DataPath, chestsData);

            PrintWarning("Plugin unloaded! Author: BadMandarin!");
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item.skin == config.DustSkin && container.capacity == 12 && container.entityOwner is BoxStorage && !(container.entityOwner is Recycler))
            {
                if (container.itemList.Count() > 1) return;


                BaseEntity e = container.entityOwner;
                if (chestsData.ContainsKey(e.OwnerID)) return;
                item.RemoveFromContainer();
                e.SetFlag(BaseEntity.Flags.Reserved4, true);
                container.SetLocked(true);
                e.skinID = config.ChestSkin;
                e.SendNetworkUpdateImmediate();
                chestsData.Add(e.OwnerID, new ChestData() { OpenTime = GetCurrentTime() + config.TimeOpen, NetId = e.net.ID });
            }
        }

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            var chests = chestsData.Where(x => x.Value.NetId == container.net.ID);
            if (chests != null && chests.Count() > 0)
            {
                var chest = chests.ToList()[0];
                if (chest.Value.OpenTime > GetCurrentTime()) return false;
            }
            return null;
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            BaseEntity e = container.entityOwner;
            if (e == null) return;
            var chests = chestsData.Where(x => x.Value.NetId == e.net.ID);
            if (chests != null && chests.Count() > 0)
            {
                if (container.itemList.Count() < 1) e.Kill();
            }
        }

        bool CanStackItem(Item item, Item targetItem)
        {
            if (item.info.displayName != targetItem.info.displayName)
                return false;
            if (item.skin != targetItem.skin)
                return false;
            if (item.info.shortname != targetItem.info.shortname)
                return false;
            if (item.skin == config.DustSkin)
                return false;
            return true;
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (container == null) return;
            if (!config.dustDrops.ContainsKey(container.ShortPrefabName)) return;
            var dusts = config.dustDrops[container.ShortPrefabName];
            if (Core.Random.Range(0, 100) > dusts.Chance) return;

            if (container.inventory.itemList.Count == container.inventory.capacity)
                container.inventory.capacity++;

            var itm = ItemManager.CreateByName("glue", dusts.Amount, config.DustSkin);
            itm.name = "Космическая пыль";
            itm.MoveToContainer(container.inventory);
        }/*
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            try
            { if (entity.HasFlag(BaseEntity.Flags.Reserved7)) return;
                entity.SetFlag(BaseEntity.Flags.Reserved7, true);
                
                var obj = entity.GetComponent<StorageContainer>();
                if (obj == null) return;

                var dusts = config.dustDrops.Where(p => entity.PrefabName.Contains(p.Key));
                if (dusts == null || dusts.Count() < 1) return;
                var dust = dusts.ToList()[0];
                if (Oxide.Core.Random.Range(0, 100) > dust.Value.Chance) return;

                if (obj.inventory.itemList.Count == obj.inventory.capacity)
                    obj.inventory.capacity++;

                Item itm = ItemManager.CreateByName("glue", dust.Value.Amount, config.DustSkin);
                itm.name = "Волшебный порошок";
                itm.MoveToContainer(obj.inventory);
               
            }
            catch { }
        }
*/
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if ((bool)entity?.PrefabName?.Contains("barrel"))
            {
                var container = entity as LootContainer;
                if (container == null) return;
                var dusts = config.dustDrops.Where(p => entity.PrefabName.Contains(p.Key));
                if (dusts == null || dusts.Count() < 1) return;
                var dust = dusts.ToList()[0];
                if (Oxide.Core.Random.Range(0, 100) > dust.Value.Chance) return;

                if (container.inventory.itemList.Count == container.inventory.capacity)
                    container.inventory.capacity++;

                Item itm = ItemManager.CreateByName("glue", dust.Value.Amount, config.DustSkin);
                itm.name = "Космическая пыль";
                itm.MoveToContainer(container.inventory);
            }
        }
        #endregion

        #region Commands
        [ConsoleCommand("test")]
        private void Console_Test(ConsoleSystem.Arg arg)
        {

        }
        [ChatCommand("test")]
        void Chat_Test(BasePlayer player, string command, string[] args)
        {
            
        }
        #endregion

        #region Utils
        private static void DrawMarker(BasePlayer player, Vector3 position, string text, int length = 1)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendEntityUpdate();
            player.SendConsoleCommand("ddraw.text", length, Color.white, position, text);
            player.SendConsoleCommand("camspeed 0");

            if (player.Connection.authLevel < 2)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);

            player.SendEntityUpdate();
        }

        private static double GetCurrentTime()
        {
            return new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        }
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                prizeSett = new Dictionary<string, PrizeSett>()
                {
                    ["sulfur"] = new PrizeSett()
                    {
                        MinValue = 1000,
                        MaxValue = 1000,
                        Chance = 100
                    },
                },
                dustDrops = new Dictionary<string, DustSett>()
                {
                    ["barrel"] = new DustSett
                    {
                        Amount = 1,
                        Chance = 100
                    }
                }
            };
        }
        #endregion

        #region ChestUpdator
        private class ChestUpdator : MonoBehaviour
        {
            private void Awake()
            {
                InvokeRepeating(nameof(UpdateInfo), 1f, 1f);
            }

            private void UpdateInfo() => instance.chestsData.ToList().ForEach(x => CheckChest(BasePlayer.FindByID(x.Key)));

            private void CheckChest(BasePlayer player)
            {
                if (player == null || !player.IsConnected) return;

                BaseEntity Chest = BaseNetworkable.serverEntities.Find(instance.chestsData[player.userID].NetId) as BaseEntity;
                if (Chest == null || Chest.IsDestroyed)
                {
                    instance.chestsData.Remove(player.userID);
                    return;
                }

                if (CheckDistance(player, Chest)) return;

                if (instance.chestsData[player.userID].OpenTime > GetCurrentTime())
                {
                    DrawMarker(player, Chest.transform.position + new Vector3(0, 0.5f, 0), $"<size=25><color=#BF00FF>GALAXYBOX</color></size>\n<size=18>Откроется через: {(int)(instance.chestsData[player.userID].OpenTime - GetCurrentTime())}с!</size>");
                    return;
                }
                if (Chest.HasFlag(BaseEntity.Flags.Reserved4))
                {
                    Chest.SetFlag(BaseEntity.Flags.Reserved4, false);
                    StorageContainer storage = Chest as StorageContainer;
                    storage.inventory.SetLocked(false);
                    instance.config.prizeSett.ToList().ForEach(x => {
                        if(Core.Random.Range(0, 100) <= x.Value.Chance)
                        {
                            Item tempitm = ItemManager.CreateByName(x.Key, Core.Random.Range(x.Value.MinValue, x.Value.MaxValue));
                            tempitm.MoveToContainer(storage.inventory);
                        }
                    });
                }
                DrawMarker(player, Chest.transform.position + new Vector3(0, 0.5f, 0), "<size=25><color=#BF00FF>GALAXYBOX</color></size>\n<size=18>Заберите свой приз!</size>");
            }

            private bool CheckDistance(BasePlayer player, BaseEntity ent)
            {
                if (Vector3.Distance(player.transform.position, ent.transform.position) >= 5) return true;

                return false;
            }

            private void OnDestroy()
            {
                CancelInvoke(nameof(UpdateInfo));
            }
        }
        #endregion
    }
}
