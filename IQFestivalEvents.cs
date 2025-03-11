using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Collections;
using Oxide.Core.Plugins;
using System.Text;

namespace Oxide.Plugins
{
    [Info("IQFestivalEvents", "TopPlugin.ru", "0.0.5")]
    public class IQFestivalEvents : RustPlugin
    {
        /// <summary>
        /// Обновление 0.0.5
        /// - Поправил отлов ошибки при загрузке конфигурации
        /// - Изменил сохранение конфигурации
        /// - Изменил OnEntityDeath(BaseCombatEntity entity, HitInfo info) -> OnEntityDeath(BaseEntity entity, HitInfo info), чтобы избавиться от лишней конвертации
        /// - Изменил проверку на уничтоженный объект в OnEntityDeath
        /// - Поправил рандом
        /// - Исправил StringBuilder, теперь он не является статичным
        /// - Добавил словарь с компонентами и избавился от GetComponent 

        [PluginReference] Plugin IQChat;
        public void SendChat(string Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                if (config.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, config.CustomPrefix, config.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        #region Vars

        public enum ItemType
        {
            Item,
            Blueprint
        }

        private Dictionary<BaseEntity, EffectComponent> EffectsComponent = new Dictionary<BaseEntity, EffectComponent>();
        public Dictionary<String, Coroutine> RoutineList = new Dictionary<String, Coroutine>();
        public static IQFestivalEvents _;
        private Dictionary<String, List<BaseEntity>> EventStarted = new Dictionary<String, List<BaseEntity>>();

        private const String PermissionStartEvent = "iqfestivalevents.usestart";
        private const String PermissionStopEvent = "iqfestivalevents.usestop";
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            //[JsonProperty("IQChat : Кастомный префикс в чате")]
            [JsonProperty("IQ Chat : Custom prefix in the chat")]
            public String CustomPrefix = "[IQFestival]";
         //   [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
            [JsonProperty("IQChat : Custom avatar in the chat (If required)")]
            public String CustomAvatar = "0";
         //   [JsonProperty("IQChat : Использовать UI уведомления")]
            [JsonProperty("IQChat : Use UI notifications")]
            public Boolean UIAlertUse = false;
            //[JsonProperty("Уведомление в чате о начале мероприятия (true - включено / false - выключено)")]
            [JsonProperty("Notification in the chat about the start of the event (true - enabled / false - disabled)")]
            public Boolean UseAlertPlayer = false;
           // [JsonProperty("Список мероприятий и их настройка : [Уникальное название] = Настройка")]
            [JsonProperty("List of events and their settings : [Unique name] = Setup")]
            public Dictionary<String, EventSetting> EventList = new Dictionary<String, EventSetting>();
            public class EventSetting
            {
               // [JsonProperty("Включить мероприятие (true - да/false - нет)")]
                [JsonProperty("Enable the event (true - yes/false - no)")]
                public Boolean TurnedEvent;
                //[JsonProperty("При каком онлайне запускать мероприятие (К примеру : 10 (запустится, если на сервере будет >= 10 игроков)")]
                [JsonProperty("When to launch an event online (For example: 10 (it will start if there are >= 10 players on the server)")]
                public Int32 OnlineStart = 0;   
              //  [JsonProperty("Дистанция спавна Entity у этого мероприятия (К примеру : Entity с соблюдением всех условий будет спавнится на дистанции более 5 метров друг от друга) ")]
                [JsonProperty("The distance of the spawn Entity at this event (For example: The Entity, subject to all conditions, will spawn at a distance of more than 5 meters from each other)")]
                public Int32 DistanceSpawnEntity = 5;
               // [JsonProperty("Сообщение при запуске мероприятия")]
                [JsonProperty("A message at the start of the event")]
                public String StartMessageEvent;
               // [JsonProperty("Через сколько начинать мероприятие (Циклично, т.е к примеру каждые 300 секунд)")]
                [JsonProperty("After how long to start the event (Cyclically, i.e., for example, every 300 seconds)")]
                public Int32 StartTimeEvent;
              //  [JsonProperty("Список монументов на котором спавнить ивент")]
                [JsonProperty("The list of monuments on which to compare the event")]
                public List<String> MonumentsSpawn = new List<String>();
                //[JsonProperty("Настройка Entity на мероприятии")]
                [JsonProperty("Setting up an Entity at an event")]
                public SettingEntity SettingEntitys = new SettingEntity();
                internal class SettingEntity
                {
                    //[JsonProperty("Эффект для проигрывания. Если эффект не нужен, оставьте поле пустым (если не знаете что это, не трогайте данный пункт)")]
                    [JsonProperty("Effect for playback. If the effect is not needed, leave the field empty (if you do not know what it is, do not touch this item)")]
                    public String EffectPath;
                //    [JsonProperty("Настройка количества спавна Entity")]
                    [JsonProperty("Setting the amount of Spawn Entity")]
                    public CountSetting CountSettings = new CountSetting();
                   // [JsonProperty("Shortname для спавна Entity (можно использовать несколько, они выбираются случайным образом)")]
                    [JsonProperty("Shortname for Spawn Entity (multiple can be used, they are randomly selected)")]
                    public List<String> ShortnameListEntity;
                  //  [JsonProperty("Х-урона по Entity на монументе")]
                    [JsonProperty("X-damage by Entity on the monument")]
                    public Single DamageEntity;
                    //[JsonProperty("Настройка выпадаемых предметов")]
                    [JsonProperty("Setting up drop-down items")]
                    public List<DropItems> DropItemList = new List<DropItems>();
                }

                internal class DropItems
                {
                   // [JsonProperty("Тип выпадаемого предмета : 0 - Итем, 1 - Чертеж (Не забывайте указывать Shortname в пункте ниже)")]
                    [JsonProperty("The type of item to drop out: 0 - Item, 1 - Drawing (Do not forget to specify the Shortname in the paragraph below)")]
                    public ItemType TypeItem = ItemType.Item;
                    [JsonProperty("Shortname")]
                    public String Shortname;
                    [JsonProperty("SkinID")]
                    public UInt64 SkinID;
                    //[JsonProperty("Отображаемое имя (Если оставить пустым, будет стандартное)")]
                    [JsonProperty("Display name (If left blank, it will be standard)")]
                    public String DisplayName;

                 //   [JsonProperty("Настройка выпадения предметов")]
                    [JsonProperty("Setting up the dropout of items")]
                    public DropSettings DropsSetting = new DropSettings();
                    internal class DropSettings
                    {
                     //   [JsonProperty("Настройка количества")]
                        [JsonProperty("Setting up the quantity")]
                        public CountSetting CountSettings = new CountSetting();
                        //[JsonProperty("Шанс выпадения (0.0 - 100.0)")]
                        [JsonProperty("Chance of falling out (0.0 - 100.0)")]
                        public Single RareDrop;
                    }

                    public Item CrateItem()
                    {
                        Int32 CountItem = Oxide.Core.Random.Range(DropsSetting.CountSettings.MinCount, DropsSetting.CountSettings.MaxCount);
                        if (CountItem <= 0) return null;

                        Item DropItem = null;
                        if (TypeItem == ItemType.Item)
                        {
                            DropItem = ItemManager.CreateByName(Shortname, CountItem, SkinID);
                            if (DropItem == null)
                            {///
                              //  _.PrintError($"Error #444 Предмет равен null, возможно вы ошиблись в Shortname({Shortname})");
                                _.PrintError($"Error #444 The subject is null, you may have made a mistake in Shortname({Shortname})");
                                return null;
                            }
                        }
                        else
                        {
                            DropItem = ItemManager.CreateByItemID(-996920608, CountItem);
                            DropItem.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == Shortname)?.itemid ?? 0;
                        }
                        if (!String.IsNullOrWhiteSpace(DisplayName))
                            DropItem.name = DisplayName;
                      
                        return DropItem;
                    }
                }
            }
            
            internal class CountSetting
            {
                //[JsonProperty("Минимальное количество")]
                [JsonProperty("Minimum amount")]
                public Int32 MinCount;
               // [JsonProperty("Максимальное количество")]
                [JsonProperty("Maximum amount")]
                public Int32 MaxCount;
            }

           // [JsonProperty("Список доступных монументов (При смене карты они могут меняться в соответствии с наличием")]
            [JsonProperty("List of available monuments (When changing the map, they may change according to availability")]
            public List<Monument> Monuments = new List<Monument>();
            internal class Monument
            {
                public String Name;
                public Vector3 Position;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UseAlertPlayer = true,
                    CustomAvatar = "0",
                    CustomPrefix = "[IQFestival]",
                    UIAlertUse = false,
                    
                    EventList = new Dictionary<String, EventSetting>
                    {
                        ["halloween"] = new EventSetting
                        {
                            TurnedEvent = true,
                          //  StartMessageEvent = "Мероприятие хэллоуин! На карте будут разбросаны чучела, сломав которые вы сможете получить хэллоуиновский подарок!",
                            StartMessageEvent = "Halloween event! Stuffed animals will be scattered on the map, breaking which you can get a Halloween gift!",
                            MonumentsSpawn = new List<String> { "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab", "assets/bundled/prefabs/autospawn/monument/roadside/supermarket_1.prefab", "assets/bundled/prefabs/autospawn/power substations/small/power_sub_small_2.prefab", "assets/bundled/prefabs/autospawn/monument/roadside/warehouse.prefab" },
                            StartTimeEvent = 30,
                            OnlineStart = 0,
                            DistanceSpawnEntity = 5,
                            SettingEntitys = new EventSetting.SettingEntity
                            {
                                EffectPath = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab",
                                DamageEntity = 10,
                                ShortnameListEntity = new List<String> { "assets/prefabs/misc/halloween/scarecrow/scarecrow.deployed.prefab" },
                                DropItemList = new List<EventSetting.DropItems>
                                {
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "halloween.candy",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 40f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "halloween.lootbag.medium",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 30f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "halloween.lootbag.large",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 10f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "pumpkinbasket",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 80f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "halloween.lootbag.small",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 55f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                     new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Blueprint,
                                        DisplayName = "",
                                        Shortname = "pistol.python",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 30f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Blueprint,
                                        DisplayName = "",
                                        Shortname = "rifle.semiauto",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 10f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Blueprint,
                                        DisplayName = "",
                                        Shortname = "pistol.semiauto",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Blueprint,
                                        DisplayName = "",
                                        Shortname = "smg.2",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 65f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Blueprint,
                                        DisplayName = "",
                                        Shortname = "icepick.salvaged",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 60f,CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                },
                                CountSettings = new CountSetting
                                {
                                    MinCount = 5,
                                    MaxCount = 15,
                                }
                            }
                        },
                        ["newyear"] = new EventSetting
                        {
                            TurnedEvent = false,
                          //  StartMessageEvent = "Новогоднее настроение!На карте по РТ будут разбросаны снеговики - разбив которые, вы получите ценный приз!",
                            StartMessageEvent = "New Year's mood!Snowmen will be scattered all over the RT on the map - breaking them, you will get a valuable prize!",
                            MonumentsSpawn = new List<String> { "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab" },
                            StartTimeEvent = 60,
                            OnlineStart = 5,
                            DistanceSpawnEntity = 10,
                            SettingEntitys = new EventSetting.SettingEntity
                            {
                                EffectPath = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab",
                                DamageEntity = 10,
                                ShortnameListEntity = new List<String> { "assets/prefabs/misc/xmas/snowman/snowman.deployed.prefab", "assets/prefabs/misc/xmas/xmastree/xmas_tree.deployed.prefab", "assets/prefabs/misc/xmas/lollipop_bundle/giantlollipops.deployed.prefab", "assets/prefabs/misc/xmas/giant_candy_cane/giantcandycane.deployed.prefab" },
                                DropItemList = new List<EventSetting.DropItems>
                                {
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "xmas.present.large",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 30f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "xmas.present.small",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 40f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "xmas.present.medium",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "coal",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 80f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                },
                                CountSettings = new CountSetting
                                {
                                    MinCount = 10,
                                    MaxCount = 20,
                                }
                            }
                        },
                        ["bonfires"] = new EventSetting
                        {
                            TurnedEvent = false,
                           // StartMessageEvent = "Мероприятие - горящие костры! Сломайте костер и получите случайную пищу для выживания на острове!",
                            StartMessageEvent = "The event is burning bonfires! Break the bonfire and get random food to survive on the island!",
                            MonumentsSpawn = new List<String> { "assets/bundled/prefabs/autospawn/monument/roadside/supermarket_1.prefab" },
                            StartTimeEvent = 60,
                            OnlineStart = 5,
                            DistanceSpawnEntity = 10,
                            SettingEntitys = new EventSetting.SettingEntity
                            {
                                EffectPath = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab",
                                DamageEntity = 5,
                                ShortnameListEntity = new List<String> { "assets/prefabs/misc/halloween/skull_fire_pit/skull_fire_pit.prefab" },
                                DropItemList = new List<EventSetting.DropItems>
                                {
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "chicken.cooked",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 30f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "deermeat.cooked",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 40f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "bearmeat.cooked",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 3 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "horsemeat.cooked",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 80f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "horsemeat.burned",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 55f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "deermeat.burned",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 44f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "chicken.burned",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 33f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "fish.cooked",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 65f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "humanmeat.burned",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 10f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 1 } },
                                    },
                                },
                                CountSettings = new CountSetting
                                {
                                    MinCount = 10,
                                    MaxCount = 20,
                                }
                            }
                        },
                        ["cupboard"] = new EventSetting
                        {
                            TurnedEvent = true,
                           // StartMessageEvent = "Мероприятие - шкафы удачи! Сломайте шкафы разбросанные возле супермаркетов и получите случайные предметы!",
                            StartMessageEvent = "Event - cabinets of good luck! Break the cabinets scattered near supermarkets and get random items!",
                            MonumentsSpawn = new List<String> { "assets/bundled/prefabs/autospawn/monument/roadside/supermarket_1.prefab" },
                            StartTimeEvent = 60,
                            OnlineStart = 5,
                            DistanceSpawnEntity = 30,
                            SettingEntitys = new EventSetting.SettingEntity
                            {
                                EffectPath = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab",
                                DamageEntity = 30,
                                ShortnameListEntity = new List<String> { "assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab" },
                                DropItemList = new List<EventSetting.DropItems>
                                {
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "cloth",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 50 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "stones",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 30f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 3000 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "wood",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 90f, CountSettings = new CountSetting { MinCount = 300, MaxCount = 5000 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "metal.fragments",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 30f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 1000 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "sulfur.ore",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 20f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 800 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "sulfur",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 10f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 500 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "metal.ore",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 5000 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "charcoal",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 80f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 5000 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "cctv.camera",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 5 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "fat.animal",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 90f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 500 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "scrap",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 50f, CountSettings = new CountSetting { MinCount = 15, MaxCount = 150 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "targeting.computer",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 60f, CountSettings = new CountSetting { MinCount = 3, MaxCount = 6 } },
                                    },
                                    new EventSetting.DropItems
                                    {
                                        TypeItem = ItemType.Item,
                                        DisplayName = "",
                                        Shortname = "metal.refined",
                                        SkinID = 0,
                                        DropsSetting = new EventSetting.DropItems.DropSettings { RareDrop = 10f, CountSettings = new CountSetting { MinCount = 1, MaxCount = 30 } },
                                    },
                                },
                                CountSettings = new CountSetting
                                {
                                    MinCount = 10,
                                    MaxCount = 20,
                                }
                            }
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                NextTick(SaveConfig);
            }
            catch(Exception ex)
            {///
              //  PrintWarning("Ошибка #444" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!! | Error #444" + $" reading the configuration 'oxide/config/{Name}', creating a new configuration!!\n\n{ex}");
                PrintWarning("Error #444" + $" reading the configuration 'oxide/config/{Name}', creating a new configuration!!\n\n{ex}");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks
        void OnServerInitialized()
        {
            _ = this;

            config.Monuments.Clear();
            MonumentInfo[] MonumentsMap = UnityEngine.GameObject.FindObjectsOfType<MonumentInfo>();
            foreach (MonumentInfo MInfo in MonumentsMap)
                config.Monuments.Add(new Configuration.Monument { Name = MInfo.name, Position = MInfo.transform.position });

            SaveConfig();
                        
            foreach (var Event in config.EventList.Where(e => e.Value != null && e.Value.TurnedEvent == true))
                timer.Once(Event.Value.StartTimeEvent, () => {
                    if (!RoutineList.ContainsKey(Event.Key))
                        RoutineList.Add(Event.Key, ServerMgr.Instance.StartCoroutine(StartEvent(Event.Key)));
                });

            RegisteredPermissions();
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null || entity.IsDestroyed || !EffectsComponent.ContainsKey(entity)) return;
            EffectsComponent[entity].DropItems(entity.transform.position);
        }

         private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
         {
            if (entity == null || info == null || entity.IsDestroyed || !EffectsComponent.ContainsKey(entity.GetEntity())) return null;
            EffectComponent Component = EffectsComponent[entity.GetEntity()];///entity.GetEntity().GetComponent<EffectComponent>();
            if (Component == null) return null;

            Single Damage = info.damageTypes.Total() * config.EventList[Component.EventKey].SettingEntitys.DamageEntity;
            Single NewHealth = entity.health -= Damage;

            if (NewHealth > 0)
            {
                entity.OnHealthChanged(entity.health, NewHealth);
                entity.SendNetworkUpdateImmediate();
            }
            else entity.Die(info);
            return false;
         }

        void Unload()
        {
            DestroyAll<EffectComponent>();

            foreach (var RList in RoutineList.Where(x => x.Value != null))
                ServerMgr.Instance.StopCoroutine(RList.Value);

            _ = null;
            RoutineList.Clear();
            RoutineList = null;
            EventStarted.Clear();
            EventStarted = null;
        }

        #endregion

        #region Metods
        void RegisteredPermissions()
        {
            permission.RegisterPermission(PermissionStartEvent, this);
            permission.RegisterPermission(PermissionStopEvent, this);
            PrintWarning("Permissions - completed");
        }
        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 50f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff"))
                return Mathf.Max(hit.point.y, y);
            return y;
        }

        static Vector3 RandomCircle(Vector3 center, float radius = 2)
        {
            Vector3 pos;
            pos.x = center.x + UnityEngine.Random.Range(-radius, radius);
            pos.z = center.z + UnityEngine.Random.Range(-radius, radius);
            pos.y = center.y;
            pos.y = GetGroundPosition(pos);
            return pos;
        }

        private IEnumerator StartEvent(String KeyEvent)
        {
            if (!config.EventList.ContainsKey(KeyEvent)) yield break;

            EndEvent(KeyEvent);

            if (config.EventList[KeyEvent].OnlineStart > BasePlayer.activePlayerList.Count)
            {
                if (RoutineList.ContainsKey(KeyEvent) && RoutineList[KeyEvent] != null)
                {
                    ServerMgr.Instance.StopCoroutine(RoutineList[KeyEvent]);
                    RoutineList.Remove(KeyEvent);
                }

                timer.Once(config.EventList[KeyEvent].StartTimeEvent, () => {
                    if (!RoutineList.ContainsKey(KeyEvent))
                        RoutineList.Add(KeyEvent, ServerMgr.Instance.StartCoroutine(StartEvent(KeyEvent)));
                });

                yield break;
            }

            if (!EventStarted.ContainsKey(KeyEvent))
                EventStarted.Add(KeyEvent, new List<BaseEntity> { });

            Configuration.EventSetting Event = config.EventList[KeyEvent];

            if (config.UseAlertPlayer)
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    SendChat(Event.StartMessageEvent, player);

            foreach (Configuration.Monument MonumentSpawn in config.Monuments.Where(m => Event.MonumentsSpawn.Contains(m.Name)))
            {
                yield return CoroutineEx.waitForSeconds(0.1f);

                Int32 CountSpawn = UnityEngine.Random.Range(Event.SettingEntitys.CountSettings.MinCount, Event.SettingEntitys.CountSettings.MaxCount);
                Configuration.EventSetting.SettingEntity SettingEntity = Event.SettingEntitys;
                if (SettingEntity == null) yield break;

                for (Int32 i = 0; i < CountSpawn; i++)
                {
                    yield return CoroutineEx.waitForSeconds(0.1f);

                    Vector3 CirclePosition = RandomCircle(MonumentSpawn.Position, 40);
                    if (TerrainMeta.HeightMap.GetHeight(CirclePosition) + 10 < CirclePosition.y || EventStarted[KeyEvent].Where(p => p != null && Vector3.Distance(p.transform.position, CirclePosition) < Event.DistanceSpawnEntity).Count() > 0)
                    {
                        i--;
                        continue;
                    }
                    String EntityShortname = SettingEntity.ShortnameListEntity.GetRandom();
                    if (String.IsNullOrWhiteSpace(EntityShortname)) continue;

                    BaseEntity Entity = GameManager.server.CreateEntity(EntityShortname, CirclePosition, new Quaternion());

                    Entity.enableSaving = false;
                    if(Entity is StorageContainer || Entity is LootContainer)
                    {
                        Entity.SetFlag(BaseEntity.Flags.Busy, true);
                        Entity.SetFlag(BaseEntity.Flags.Locked, true);
                    }
                    if(Entity is BaseOven)
                    {
                        Entity.SetFlag(BaseEntity.Flags.On, true);
                        Entity.SetFlag(BaseEntity.Flags.OnFire, true);
                    }
  
                    Entity.Spawn();

                    if (Entity != null)
                    {
                        EffectComponent Effect = Entity.gameObject.AddComponent<EffectComponent>();
                        Effect.Initialize(KeyEvent);
                        if (!EffectsComponent.ContainsKey(Entity))
                            EffectsComponent.Add(Entity, Effect);
                        EventStarted[KeyEvent].Add(Entity);
                    }
                }
            }

            timer.Once(Event.StartTimeEvent, () => {
                if (RoutineList.ContainsKey(KeyEvent))
                    EndEvent(KeyEvent);

                RoutineList.Add(KeyEvent, ServerMgr.Instance.StartCoroutine(StartEvent(KeyEvent)));
            });
            yield break;
        }
        void EndEvent(String EventKey)
        {
            if (!EventStarted.ContainsKey(EventKey) || !config.EventList.ContainsKey(EventKey)) return;

            foreach (BaseEntity entity in EventStarted[EventKey].Where(entity => entity != null && !entity.IsDestroyed))
                entity.Kill();

            if (RoutineList.ContainsKey(EventKey))
            {
                ServerMgr.Instance.StopCoroutine(RoutineList[EventKey]);
                RoutineList.Remove(EventKey);
            }
            EventStarted.Remove(EventKey);
        }
        #endregion

        #region Command

        [ChatCommand("iqfe")]
        void CommandIQFEChat(BasePlayer player, String cmd, String[] arg)
        {
            if (arg == null || arg.Length < 2)
            {
                if (player != null)
                    SendChat(GetLang("COMMAND_START_ERROR_SYNTAX", player.UserIDString), player);
                else Puts(GetLang("COMMAND_START_ERROR_SYNTAX", null));
                return;
            }

            String Action = arg[0];
            String KeyEvent = arg[1];
            if(String.IsNullOrWhiteSpace(KeyEvent) || !config.EventList.ContainsKey(KeyEvent))
            {
                if (player != null)
                    SendChat(GetLang("COMMAND_ERROR_KEY", player.UserIDString, KeyEvent), player);
                else Puts(GetLang("COMMAND_ERROR_KEY", null, KeyEvent));
                return;
            }

            switch(Action)
            {
                case "start":
                    {
                        StartEventUser(player, KeyEvent);
                        break;
                    }
                case "stop":
                    {
                        StopEventUser(player, KeyEvent);
                        break;

                    }
            }
        }

        [ConsoleCommand("iqfe")]
        void CommandIQFEConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (arg.Args == null || arg.Args.Length < 2)
            {
                if (player != null)
                    SendChat(GetLang("COMMAND_START_ERROR_SYNTAX", player.UserIDString), player);
                else Puts(GetLang("COMMAND_START_ERROR_SYNTAX", null));
                return;
            }

            String Action = arg.Args[0];
            String KeyEvent = arg.Args[1];
            if (String.IsNullOrWhiteSpace(KeyEvent) || !config.EventList.ContainsKey(KeyEvent))
            {
                if (player != null)
                    SendChat(GetLang("COMMAND_ERROR_KEY", player.UserIDString, KeyEvent), player);
                else Puts(GetLang("COMMAND_ERROR_KEY", null, KeyEvent));
                return;
            }

            switch (Action)
            {
                case "start":
                    {
                        StartEventUser(player, KeyEvent);
                        break;
                    }
                case "stop":
                    {
                        StopEventUser(player, KeyEvent);
                        break;

                    }
            }
        }


        private void StartEventUser(BasePlayer player, String KeyEvent)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, PermissionStartEvent))
            {
                SendChat(GetLang("COMMAND_NOT_PERMISSIONS", player.UserIDString), player);
                return;
            }

            if (!RoutineList.ContainsKey(KeyEvent))
                RoutineList.Add(KeyEvent, ServerMgr.Instance.StartCoroutine(StartEvent(KeyEvent)));
            else
            {
                if (!EventStarted.ContainsKey(KeyEvent))
                {
                    if (player != null)
                        SendChat(GetLang("COMMAND_STOP_ERROR_NOT_STARTED", player.UserIDString, KeyEvent), player);
                    else Puts(GetLang("COMMAND_STOP_ERROR_NOT_STARTED", null, KeyEvent));
                    return;
                }
                EndEvent(KeyEvent);
                StartEventUser(player, KeyEvent);
                return;
            }

            if (player != null)
                SendChat(GetLang("COMMAND_START_SUCCESS", player.UserIDString, KeyEvent), player);
            else Puts(GetLang("COMMAND_START_SUCCESS", null, KeyEvent));
        }

        private void StopEventUser(BasePlayer player, String KeyEvent)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, PermissionStopEvent))
            {
                SendChat(GetLang("COMMAND_NOT_PERMISSIONS", player.UserIDString), player);
                return;
            }

            if (!EventStarted.ContainsKey(KeyEvent))
            {
                if (player != null)
                    SendChat(GetLang("COMMAND_STOP_ERROR_NOT_STARTED", player.UserIDString, KeyEvent), player);
                else Puts(GetLang("COMMAND_STOP_ERROR_NOT_STARTED", null, KeyEvent));
                return;
            }

            EndEvent(KeyEvent);

            timer.Once(config.EventList[KeyEvent].StartTimeEvent, () => {
                if (!RoutineList.ContainsKey(KeyEvent))
                    RoutineList.Add(KeyEvent, ServerMgr.Instance.StartCoroutine(StartEvent(KeyEvent)));
            });

            if (player != null)
                SendChat(GetLang("COMMAND_STOP_SUCCESS", player.UserIDString, KeyEvent), player);
            else Puts(GetLang("COMMAND_STOP_SUCCESS", null, KeyEvent));
        }
        #endregion

        #region Script
        public class EffectComponent : FacepunchBehaviour
        {
            public String EventKey;
            private String EffectPath;
            private BaseEntity entity;

            void Awake() => entity = GetComponent<BaseEntity>();

            public void Initialize(String EventKey)
            {
                this.EventKey = EventKey;

                Configuration.EventSetting Event = config.EventList[EventKey];
                if (Event == null) return;

                this.EffectPath = Event.SettingEntitys.EffectPath;

                if (!String.IsNullOrWhiteSpace(EffectPath) && entity != null)
                    InvokeRepeating(nameof(EffectSpawn), 0, 30f);
            }

            void EffectSpawn()
            {
                if (entity == null) return;
                Effect.server.Run(EffectPath, entity.CenterPoint() + new Vector3(0, 1f, 0));
            }

            public void DropItems(Vector3 DropPosition)
            {
                if (!config.EventList.ContainsKey(EventKey)) return;
                Configuration.EventSetting Event = config.EventList[EventKey];
                if (Event == null) return;
                if (Event.SettingEntitys.DropItemList == null) return;

                var RandomReward = Event.SettingEntitys.DropItemList.Where(x => UnityEngine.Random.Range(0, 101) <= x.DropsSetting.RareDrop).ToList();
                if (RandomReward == null) return;

                Configuration.EventSetting.DropItems DropItem = RandomReward.GetRandom();
                if (DropItem == null) return;

                Item item = DropItem.CrateItem();
                if (item == null) return;

                item.DropAndTossUpwards(DropPosition + Vector3.up);
            }

            void OnDestroy()
            {
                CancelInvoke(nameof(EffectSpawn));
                if (entity != null && !entity.IsDestroyed)
                    entity.Kill();
                Destroy(this);
            }
        }
        private void DestroyAll<T>()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(T));
            objects?.ToList().ForEach(UnityEngine.Object.Destroy);
        }
        #endregion

        #region Lang

        private StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["COMMAND_START_ERROR_SYNTAX"] = "You made a mistake in the syntax of the command, use the command - info start Key",
                ["COMMAND_STOP_ERROR_SYNTAX"] = "You made a mistake in the syntax of the command, use the command - info stop Key",
                ["COMMAND_ERROR_KEY"] = "The key you entered : {0} - does not exist",
                ["COMMAND_START_SUCCESS"] = "You have successfully launched an event with the key {0}",
                ["COMMAND_STOP_SUCCESS"] = "You have successfully stopped the event with the key {0}}",
                ["COMMAND_STOP_ERROR_NOT_STARTED"] = "The event with the key {0} has not been launched yet",
                ["COMMAND_NOT_PERMISSIONS"] = "The event with the key {0} has not been launched yet",
                ["COMMAND_NOT_PERMISSIONS"] = "You don't have enough rights to use this command",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["COMMAND_START_ERROR_SYNTAX"] = "Вы допустили ошибку в синтаксисе команды, используйте команду - iqfe start Key",
                ["COMMAND_STOP_ERROR_SYNTAX"] = "Вы допустили ошибку в синтаксисе команды, используйте команду - iqfe stop Key",
                ["COMMAND_ERROR_KEY"] = "Введнного вами ключа : {0} - не существует",
                ["COMMAND_START_SUCCESS"] = "Вы успешно запустили мероприятие с ключом {0}",
                ["COMMAND_STOP_SUCCESS"] = "Вы успешно остановили мероприятие с ключом {0}",
                ["COMMAND_STOP_ERROR_NOT_STARTED"] = "Мероприятие с ключом {0} еще не запущен",
                ["COMMAND_NOT_PERMISSIONS"] = "У вас недостаточно прав для использования этой команды",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно | The language file was uploaded successfully");
        }
        #endregion
    }
}
