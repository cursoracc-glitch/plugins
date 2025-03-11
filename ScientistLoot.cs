using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("ScientistLoot", "SkiTles", "1.0")]
    class ScientistLoot : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Vars
        private bool Initialized = false;
        class NPCItem
        {
            public string shortname;
            public int minamount;
            public int maxamount;
            public bool bp;
            public int rarity;
        }
        #endregion

        #region Config
        private static ConfigFile config;
        private class ConfigFile
        {
            [JsonProperty(PropertyName = "Минимальное количество предметов")]
            public int minamount { get; set; }

            [JsonProperty(PropertyName = "Максимальное количество предметов")]
            public int maxamount { get; set; }

            [JsonProperty(PropertyName = "Сохранять шанс на выпадение оружия?")]
            public bool weaponsave { get; set; }

            [JsonProperty(PropertyName = "Список лута")]
            public List<NPCItem> npcitemlist { get; set; }

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    minamount = 1,
                    maxamount = 6,
                    weaponsave = true,
                    npcitemlist = new List<NPCItem>()
                    {
                        new NPCItem() { shortname = "syringe.medical", minamount = 1, maxamount = 2, bp = false, rarity = 1 },
                        new NPCItem() { shortname = "flare", minamount = 2, maxamount = 5, bp = false, rarity = 1 },
                        new NPCItem() { shortname = "keycard_green", minamount = 1, maxamount = 1, bp = false, rarity = 1 },
                        new NPCItem() { shortname = "semibody", minamount = 1, maxamount = 3, bp = false, rarity = 1 },
                        new NPCItem() { shortname = "pickaxe", minamount = 1, maxamount = 1, bp = false, rarity = 1 },
                        new NPCItem() { shortname = "scrap", minamount = 8, maxamount = 33, bp = false, rarity = 1 },
                        new NPCItem() { shortname = "grenade.f1", minamount = 2, maxamount = 3, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "smgbody", minamount = 1, maxamount = 1, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "ammo.shotgun.fire", minamount = 4, maxamount = 7, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "syringe.medical", minamount = 1, maxamount = 1, bp = true, rarity = 2 },
                        new NPCItem() { shortname = "techparts", minamount = 1, maxamount = 1, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "weapon.mod.flashlight", minamount = 1, maxamount = 1, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "hazmatsuit", minamount = 1, maxamount = 1, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "ammo.pistol", minamount = 30, maxamount = 60, bp = false, rarity = 2 },
                        new NPCItem() { shortname = "explosives", minamount = 1, maxamount = 1, bp = false, rarity = 3 },
                        new NPCItem() { shortname = "riflebody", minamount = 1, maxamount = 1, bp = false, rarity = 3 },
                        new NPCItem() { shortname = "weapon.mod.holosight", minamount = 1, maxamount = 1, bp = false, rarity = 3 },
                        new NPCItem() { shortname = "ammo.shotgun", minamount = 8, maxamount = 11, bp = false, rarity = 3 }
                    }
                };
            }
        }
        protected override void LoadDefaultConfig()
        {
            config = ConfigFile.DefaultConfig();
            PrintWarning("Создан новый файл конфигурации. Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigFile>();
                if (config == null)
                    Regenerate();
            }
            catch { Regenerate(); }
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private void Regenerate()
        {
            PrintWarning($"Конфигурационный файл 'oxide/config/{Name}.json' поврежден, создается новый...");
            LoadDefaultConfig();
        }
        #endregion

        #region OxideHooks
        private void OnServerInitialized()
        {
            Initialized = true;
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!Initialized) return;
            if (entity.ShortPrefabName.Contains("scientist_corpse"))
            {
                var npccorpse = entity as LootableCorpse;
                if (npccorpse == null) return;
                NextTick(() => { ChangeLoot(npccorpse); });
            }
        }
        #endregion

        #region Main
        private void ChangeLoot(LootableCorpse npccorpse)
        {
            var minv = npccorpse.containers[0];
            if (config.weaponsave)
            {
                foreach (var item in minv.itemList)
                {
                    if (item.info.shortname != "smg.mp5" && item.info.shortname != "pistol.m92" && item.info.shortname != "shotgun.spas12") { item.RemoveFromWorld(); item.Remove(); }
                }
            }
            else { minv.Clear(); }
            int amount = Random.Range(config.minamount, config.maxamount);
            List<int> Choices = new List<int>();
            for (int i = 0; i < amount; i++)
            {
                var choice1 = Random.Range(1, 100);
                int rare = 1;
                if (choice1 >= 97) rare = 3;
                if (choice1 >= 85 && choice1 < 97) rare = 2;
                int test = Random.Range(config.npcitemlist.Count);
                if (rare == config.npcitemlist.ElementAt(test).rarity)
                {
                    if (!Choices.Contains(test))
                    {
                        Choices.Add(test);
                        var itemdef = config.npcitemlist.ElementAt(test);
                        string shortname = itemdef.shortname;
                        if (itemdef.bp) shortname = "blueprintbase";
                        int itemamount = 1;
                        if (!itemdef.bp) itemamount = Random.Range(itemdef.minamount, itemdef.maxamount);
                        Item newitem = ItemManager.CreateByName(shortname, itemamount);
                        if (itemdef.bp) newitem.blueprintTarget = ItemManager.CreateByName(itemdef.shortname).info.itemid;
                        newitem.MoveToContainer(minv);
                    }
                    else
                    { amount++; }
                }
                else
                { amount++; }
            }
        }
        #endregion
    }
}