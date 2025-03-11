using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Color = UnityEngine.Color;
using System.Globalization;
using System.Collections;
using ConVar;

namespace Oxide.Plugins
{
    [Info("ShopSystemRevolution", "DezLife", "1.4.0")]
    [Description("Большой и настраиваемый внутриигровой магазин")]
    public class ShopSystemRevolution : RustPlugin
    {
        public enum ItemType
        {
            Предмет,
            Чертёж,
            КастомПредмет,
            Команда
        }

        [PluginReference] Plugin IQChat, ImageLibrary, Economics, IQEconomic, Friends, Clans, Battles, Duel;

        #region ref

        public bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public bool IsClans(ulong userID, ulong targetID)
        {
            if (Clans)
                return (bool)Clans?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }

        #endregion

        #region Classes
        public class ItemStores
        {
            [JsonProperty("Тип предмета(0 - Предмет, 1 - Чертёж, 2 - кастомный предмет, 3 - Команда)")]
            public ItemType type;
            [JsonProperty("Shortame")]
            public string ShortName;
            [JsonProperty("Цена")]
            public int Price;
            [JsonProperty("Количество при покупке")]
            public int Amount;
            [JsonProperty("Кастом имя предмета (Использовать с типом предмета 2 и 3)")]
            public string Name;
            [JsonProperty("SkinID предмета (Использовать с типом предмета 2)")]
            public ulong SkinID;
            [JsonProperty("Команда(Использовать с типом предмета 3)")]
            public string Command;
            [JsonProperty("URL картинки (Использовать с типом предмета 3)")]
            public string Url;
        }
        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ShopSystem_UI_TITLE"] = "<b>toy shop</b>\n",
                ["ShopSystem_UI_CLOSE"] = "<b>CLOSE</b>",
                ["ShopSystem_UI_CATEGORY1"] = "<b>product categories</b>",
                ["ShopSystem_UI_CATEGORY2"] = "<b>products from the selected category</b>",
                ["ShopSystem_UI_BALANCEINFO"] = "Your balance: {0}\nPersonal discount: {1}%",
                ["ShopSystem_UI_BUTTON_NEXT"] = "<b>NEXT</b>",
                ["ShopSystem_UI_BUTTON_BACK"] = "<b>BACK</b>",
                ["ShopSystem_UI_BUY"] = "Buy",
                ["ShopSystem_UI_BUYITEM"] = "You want to buy: {0}\nPurchase price: {1}\nYour balance {2}",
                ["ShopSystem_CHAT_BUYITEM_NOMONEY"] = "You do not have enough funds for this purchase",
                ["ShopSystem_CHAT_BUYITEM_ERROR"] = "An unexpected error has occurred.Contact administrator",
                ["ShopSystem_CHAT_BUYITEM"] = "You have successfully bought {0} behind {1} koynov",
                ["ShopSystem_CHAT_BUYITEM_INVENTORY_ISFULL"] = "Out of inventory",
                ["ShopSystem_CHAT_MONEYGIVE"] = "You have earned {0} coins for your store balance",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ShopSystem_UI_TITLE"] = "<b>магазин игрушек</b>\n",
                ["ShopSystem_UI_CLOSE"] = "<b>ЗАКРЫТЬ</b>",
                ["ShopSystem_UI_CATEGORY1"] = "<b>категории товаров</b>",
                ["ShopSystem_UI_CATEGORY2"] = "<b>товары из выбранной категории</b>",
                ["ShopSystem_UI_BALANCEINFO"] = "Ваш баланс: {0}\nПерсональная скидка: {1}%",
                ["ShopSystem_UI_BUTTON_NEXT"] = "<b>ВПЕРЕД</b>",
                ["ShopSystem_UI_BUTTON_BACK"] = "<b>НАЗАД</b>",
                ["ShopSystem_UI_BUY"] = "Купить",
                ["ShopSystem_UI_BUYITEM"] = "Вы хотите купить: {0}\nЦена покупки: {1}\nВаш Баланс {2}",
                ["ShopSystem_CHAT_BUYITEM_NOMONEY"] = "У вас недостаточно средств для данной покупки",
                ["ShopSystem_CHAT_BUYITEM_ERROR"] = "Произошла непредвиденная ошибка. Обратитесь к администратору",
                ["ShopSystem_CHAT_BUYITEM"] = "Вы успешно купили {0} за {1} койнов ",
                ["ShopSystem_CHAT_BUYITEM_INVENTORY_ISFULL"] = "Недостаточно места в инвентаре",
                ["ShopSystem_CHAT_MONEYGIVE"] = "Вы заработали {0} монет на баланс магазина",
            }, this, "ru");
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Основные настройки плагина")]
            public GeneralSetings generalSetings;
            [JsonProperty("Скидки по пермешенам")]
            public DiscountStores discountStores;
            [JsonProperty("Настройка валюты")]
            public CurrencyForStores currencyForStores;
            [JsonProperty("Настройка выпадения валюты (Только для типа default)")]
            public DropCurrencySetings dropCurrencySetings;
            [JsonProperty("Связать магазин с плагином Human NPC")]
            public HumanNPCSettings humanNPCSettings;
            [JsonProperty("Предметы")]
            public Dictionary<string, List<ItemStores>> itemstores;

            internal class GeneralSetings
            {
                [JsonProperty("Чат комманда для открытия шопа (Не будет действовать если вы включили поддержку Human NPC)")]
                public string CommandOpenUI;
                [JsonProperty("Цвет фона магазина")]
                public string FonColor;
                [JsonProperty("Цвет не активной категории")]
                public string CategoryFonINActive;
                [JsonProperty("Цвет активной категории")]
                public string CategoryFonActive;
                [JsonProperty("Цвет кнопок вперед/назад")]
                public string ButonPageColor;
                [JsonProperty("Цвет окна подтверждения покупки")]
                public string ButonByeSucc;
                [JsonProperty("Цвет панельки 'категории товаров'")]
                public string Butoncategoryitem;
                [JsonProperty("Цвет панельки 'товары из выбранной категории'")]
                public string Butoncategoryactiveitem;
                [JsonProperty("Включить кнопку быстрого доступа к магазину")]
                public bool ButtonIcon;
                [JsonProperty("Кнопка быстрого магазина OffsetMin ")]
                public string ButtonIconOffsetMin;
                [JsonProperty("Кнопка быстрого магазина OffsetMax")]
                public string ButtonIconOffsetMax;
                [JsonProperty("Основное окно магазина OffsetMin ")]
                public string MainUIOffsetMin;
                [JsonProperty("КОсновное окно магазина OffsetMax")]
                public string MainUIOffsetMax;
            }
            internal class CurrencyForStores
            {
                [JsonProperty("Тип валюты (default, economic, IQEconomic)")]
                public string TypeBalance;
                [JsonProperty("Названия валюты (Работает с типом default)")]
                public string NameCurrency;
                [JsonProperty("Начальный баланс игроков (Работает с типом default)")]
                public int BalanceStart;
                [JsonProperty("Отчищать баланс при вайпе ? (Работает с типом default)")]
                public bool WipeData;
            }
            internal class HumanNPCSettings
            {
                [JsonProperty("Включить ли поддержку плагина Human NPC")]
                public bool NpsUse;
                [JsonProperty("ID бота у которого можно будет открыть магазин")]
                public ulong NpsId;
            }

            internal class DropCurrencySetings
            {
                [JsonProperty("Сколько монет давать за убийство игроков")]
                public DropSet PlayerMoneyDive = new DropSet();
                [JsonProperty("Сколько монет давать за убийство NPC")]
                public DropSet NPCMoneyDive = new DropSet();
                [JsonProperty("Сколько монет давать за убийство животных")]
                public DropSet AnimalMoneyDive = new DropSet();
                [JsonProperty("Сколько монет давать за взрыв танка")]
                public DropSet BradleyMoneyDive = new DropSet();
                [JsonProperty("Сколько монет давать за взрыв Вертолета")]
                public DropSet HeliMoneyDive = new DropSet();
                [JsonProperty("Сколько монет давать за добычу руды")]
                public Dictionary<string, DropSet> OreMoneyDive = new Dictionary<string, DropSet>();
                [JsonProperty("Сколько монет давать за добычу трупов")]
                public DropSet CorpseMoneyDive = new DropSet();

                internal class DropSet
                {
                    [JsonProperty("Шанс выпадения валюты (Если поставить 0 то падать не будет)")]
                    public int Chance;
                    [JsonProperty("Минимальное количество выпадения валюты")]
                    public int MinDrop;
                    [JsonProperty("Максимальное количество выпадения валюты")]
                    public int MaxDrop;
                }
            }

            internal class DiscountStores
            {
                [JsonProperty("Пермешен/Скидка %")]
                public Dictionary<string, int> DiscountPerm;
            }


            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    generalSetings = new GeneralSetings
                    {
                        CommandOpenUI = "shop",
                        FonColor = "#AE3F19CA",
                        CategoryFonINActive = "#A04D4DD8",
                        CategoryFonActive = "#EF7A7AD8",
                        ButonPageColor = "#2AE9748B",
                        ButonByeSucc = "#7A8FEF87",
                        Butoncategoryitem = "#2AE9748B",
                        Butoncategoryactiveitem = "#2AE9748B",
                        ButtonIcon = true,
                        ButtonIconOffsetMin = "5 -40",
                        ButtonIconOffsetMax = "40 -5",
                        MainUIOffsetMin = "-570 -340",
                        MainUIOffsetMax = "570 335"
                    },
                    currencyForStores = new CurrencyForStores
                    {
                        TypeBalance = "default",
                        NameCurrency = "Coin",
                        BalanceStart = 5,
                        WipeData = true
                    },
                    discountStores = new DiscountStores
                    {
                        DiscountPerm = new Dictionary<string, int>
                        {
                            ["ShopSystemRevolution.10"] = 10
                        }
                    },
                    dropCurrencySetings = new DropCurrencySetings
                    {
                        AnimalMoneyDive = new DropCurrencySetings.DropSet
                        {
                            Chance = 30,
                            MinDrop = 1,
                            MaxDrop = 2
                        },
                        NPCMoneyDive = new DropCurrencySetings.DropSet
                        {
                            Chance = 40,
                            MinDrop = 1,
                            MaxDrop = 3
                        },
                        PlayerMoneyDive = new DropCurrencySetings.DropSet
                        {
                            Chance = 60,
                            MinDrop = 1,
                            MaxDrop = 5
                        },
                        CorpseMoneyDive = new DropCurrencySetings.DropSet
                        {
                            Chance = 20,
                            MinDrop = 1,
                            MaxDrop = 3
                        },
                        HeliMoneyDive = new DropCurrencySetings.DropSet
                        {
                            Chance = 60,
                            MinDrop = 4,
                            MaxDrop = 7
                        },
                        BradleyMoneyDive = new DropCurrencySetings.DropSet
                        {
                            Chance = 60,
                            MinDrop = 4,
                            MaxDrop = 7
                        },
                        OreMoneyDive = new Dictionary<string, DropCurrencySetings.DropSet>
                        {
                            ["Sulfur"] = new DropCurrencySetings.DropSet
                            {
                                Chance = 50,
                                MinDrop = 1,
                                MaxDrop = 3
                            },
                            ["metal.ore"] = new DropCurrencySetings.DropSet
                            {
                                Chance = 30,
                                MinDrop = 1,
                                MaxDrop = 2
                            },
                            ["Wood"] = new DropCurrencySetings.DropSet
                            {
                                Chance = 40,
                                MinDrop = 1,
                                MaxDrop = 2
                            },
                            ["hq.metal.ore"] = new DropCurrencySetings.DropSet
                            {
                                Chance = 50,
                                MinDrop = 1,
                                MaxDrop = 5
                            },
                            ["Stones"] = new DropCurrencySetings.DropSet
                            {
                                Chance = 20,
                                MinDrop = 1,
                                MaxDrop = 2
                            }
                        },
                    },

                    #region Test Item
                    itemstores = new Dictionary<string, List<ItemStores>>
                    {
                        ["Конструкции"] = new List<ItemStores>
                       {
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "door.hinged.toptier",
                                Amount = 1,
                                Price = 120,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "door.double.hinged.toptier",
                                Amount = 1,
                                Price = 150,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "barricade.woodwire",
                                Amount = 5,
                                Price = 65,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "barricade.concrete",
                                Amount = 5,
                                Price = 30,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.frame.fence.gate",
                                Amount = 1,
                                Price = 25,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "floor.grill",
                                Amount = 3,
                                Price = 50,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.frame.garagedoor",
                                Amount = 1,
                                Price = 90,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.external.high",
                                Amount = 3,
                                Price = 25,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "gates.external.high.wood",
                                Amount = 1,
                                Price = 15,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "mining.quarry",
                                Amount = 1,
                                Price = 100,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.window.glass.reinforced",
                                Amount = 5,
                                Price = 60,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "floor.ladder.hatch",
                                Amount = 1,
                                Price = 70,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "gates.external.high.stone",
                                Amount = 1,
                                Price = 40,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.external.high.stone",
                                Amount = 5,
                                Price = 80,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "door.closer",
                                Amount = 5,
                                Price = 10,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.window.bars.toptier",
                                Amount = 1,
                                Price = 50,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "shutter.metal.embrasure.a",
                                Amount = 1,
                                Price = 30,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "watchtower.wood",
                                Amount = 1,
                                Price = 10,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "wall.window.bars.metal",
                                Amount = 3,
                                Price = 20,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "mining.pumpjack",
                                Amount = 1,
                                Price = 70,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "ladder.wooden.wall",
                                Amount = 3,
                                Price = 15,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "bed",
                                Amount = 1,
                                Price = 10,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "dropbox",
                                Amount = 3,
                                Price = 5,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "workbench1",
                                Amount = 1,
                                Price = 30,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "workbench2",
                                Amount = 1,
                                Price = 45,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "workbench3",
                                Amount = 1,
                                Price = 65,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "box.repair.bench",
                                Amount = 1,
                                Price = 15,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "furnace.large",
                                Amount = 1,
                                Price = 25,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Предмет,
                                ShortName = "arcade.machine.chippy",
                                Amount = 1,
                                Price = 30,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                       },
                        ["Кастомные предметы"] = new List<ItemStores>
                       {
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "sticks",
                                Amount = 10,
                                Price = 100,
                                Command = "",
                                Name = "Радиактивный металл",
                                SkinID = 1989988490,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "bleach",
                                Amount = 10,
                                Price = 130,
                                Command = "",
                                Name = "Радиактивная сера",
                                SkinID = 1989987965,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "glue",
                                Amount = 10,
                                Price = 80,
                                Command = "",
                                Name = "Радиактивный камень",
                                SkinID = 1989988784,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "tshirt.long",
                                Amount = 1,
                                Price = 160,
                                Command = "",
                                Name = "Рюкзак",
                                SkinID = 1978119207,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "geiger.counter",
                                Amount = 1,
                                Price = 40,
                                Command = "",
                                Name = "Грелка",
                                SkinID = 1978119616,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "keycard_red",
                                Amount = 1,
                                Price = 70,
                                Command = "",
                                Name = "Карта общего доступа",
                                SkinID = 1977450795,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.КастомПредмет,
                                ShortName = "sticks",
                                Amount = 1,
                                Price = 45,
                                Command = "",
                                Name = "-радиация",
                                SkinID = 1977071544,
                                Url = "",
                           },
                       },
                        ["Чертежи"] = new List<ItemStores>
                       {
                           new ItemStores
                           {
                                type = ItemType.Чертёж,
                                ShortName = "icepick.salvaged",
                                Amount = 1,
                                Price = 50,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Чертёж,
                                ShortName = "rocket.launcher",
                                Amount = 1,
                                Price = 90,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Чертёж,
                                ShortName = "rifle.semiauto",
                                Amount = 1,
                                Price = 70,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Чертёж,
                                ShortName = "pistol.revolver",
                                Amount = 1,
                                Price = 35,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           },
                           new ItemStores
                           {
                                type = ItemType.Чертёж,
                                ShortName = "pistol.semiauto",
                                Amount = 1,
                                Price = 50,
                                Command = "",
                                Name = "",
                                SkinID = 0,
                                Url = "",
                           }
                       },
                        ["Привилегии"] = new List<ItemStores>
                       {
                           new ItemStores
                           {
                                type = ItemType.Команда,
                                ShortName = "",
                                Amount = 1,
                                Price = 200,
                                Command = "o.grant group add %STEAMID% pony",
                                Name = "Поняшка 3 дня",
                                SkinID = 0,
                                Url = "https://i.imgur.com/sxNzimL.png",
                           },
                           new ItemStores
                           {
                                type = ItemType.Команда,
                                ShortName = "",
                                Amount = 1,
                                Price = 150,
                                Command = "o.grant group add %STEAMID% vip",
                                Name = "VIP 7 Дней",
                                SkinID = 0,
                                Url = "https://i.imgur.com/ONYOmTR.png",
                           },
                       }
                    },
                    #endregion

                    humanNPCSettings = new HumanNPCSettings
                    {
                        NpsUse = false,
                        NpsId = 0
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
            }
            catch
            {
                PrintWarning("Ошибка #127" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data    

        public Dictionary<ulong, double> usersBalance;

        #endregion

        #region Metods

        #region DATA
        private Dictionary<ulong, int> BalanceData = new Dictionary<ulong, int>();
        #endregion
        public void UnsubscribeHook()
        {
            Unsubscribe("OnDispenserBonus");
            Unsubscribe("OnDispenserGather");
            Unsubscribe("OnEntityDeath");
            Unsubscribe("OnEntityTakeDamage");
        }

        bool Chance(int chance)
        {
            if (rnd.Next(0, 100) >= (100 - chance))
                return true;
            else return false;
        }
        System.Random rnd = new System.Random();
        int RandomAmount(int min, int max)
        {
            return rnd.Next(min, max);
        }
        #region CheckCFG
        private void CheckCFG()
        {
            foreach (var cfg in config.itemstores)
            {
                for (int i = 0; i < cfg.Value.Count; i++)
                {
                    if (cfg.Value[i].Amount == 0)
                    {
                        PrintError("В конфиге количество предмета равно 0! Срочно исправьте");
                    }
                    if (cfg.Value[i].Price == 0)
                    {
                        PrintError("В конфиге цена у предмета равно 0! Срочно исправьте");
                    }
                    if (cfg.Value[i].type == ItemType.КастомПредмет)
                    {
                        if (cfg.Value[i].SkinID == 0)
                        {
                            PrintError("В конфиге SkinId у кастом предмета равно 0! Срочно исправьте");
                        }
                        if (cfg.Value[i].Name == "")
                        {
                            PrintError("В конфиге у Кастом Предмет параметр name пуст ! Срочно исправьте");
                        }
                        if (cfg.Value[i].ShortName == "")
                        {
                            PrintError("В конфиге не указан shortname у кастом предмета! Срочно исправьте");
                        }
                    }
                    if (cfg.Value[i].type == ItemType.Команда)
                    {
                        if (cfg.Value[i].Command == "")
                        {
                            PrintError("В конфиге у типа комманда отсутствует комманда! Срочно исправьте");
                        }
                        if (cfg.Value[i].Url == "")
                        {
                            PrintError("В конфиге не указан url у команды! Срочно исправьте");
                        }
                        if (cfg.Value[i].Name == "")
                        {
                            PrintError("В конфиге у комманды параметр name пуст ! Срочно исправьте");
                        }
                    }
                    if (cfg.Value[i].type == ItemType.Предмет)
                    {
                        if (cfg.Value[i].ShortName == "")
                        {
                            PrintError("В конфиге не указан shortname у предмета! Срочно исправьте");
                        }
                    }
                    if (cfg.Value[i].type == ItemType.Чертёж)
                    {
                        if (cfg.Value[i].ShortName == "")
                        {
                            PrintError("В конфиге не указан shortname у чертежа! Срочно исправьте");
                        }
                    }
                }
            }
            ServerMgr.Instance.StartCoroutine(DownloadImages());
        }
        #endregion

        private void PurchaseItem(BasePlayer player, int category, int item, int page)
        {
            ItemStores itembuy = config.itemstores.ElementAt(category).Value.ElementAt(item);
            Item itemcrate = ItemManager.CreateByName(itembuy.ShortName);
            string NameItem = itembuy.type == ItemType.Команда ? itembuy.Name : itembuy.type == ItemType.КастомПредмет ? itembuy.Name : itemcrate.info.displayName.english;
            var discount = (itembuy.Price * GetUserDiscount(player) / 100);
            int price = (itembuy.Price - discount);

            if (player.inventory.containerMain.itemList.Count > 24)
            {
                SendChat(player, lang.GetMessage("ShopSystem_CHAT_BUYITEM_INVENTORY_ISFULL", this, player.UserIDString));
                return;
            }
            if (itembuy != null)
            {
                switch (config.currencyForStores.TypeBalance)
                {
                    case "economic":
                        if ((double)Economics?.Call("Balance", player.userID) >= price)
                        {
                            if ((bool)Economics?.Call("Withdraw", player.userID, (double)price))
                            {
                                GiveItem(player, itembuy);
                                ShopMainUI(player, category, page, true);
                                SendChat(player, String.Format(lang.GetMessage("ShopSystem_CHAT_BUYITEM", this, player.UserIDString), NameItem, price));
                                Log($"Item buy\n Player {player.userID}, buy {NameItem} success");
                            }
                            else SendChat(player, lang.GetMessage("ShopSystem_CHAT_BUYITEM_NOMONEY", this, player.UserIDString));
                        }
                        else SendChat(player, lang.GetMessage("ShopSystem_CHAT_BUYITEM_NOMONEY", this, player.UserIDString));
                        break;
                    case "IQEconomic":
                        if ((bool)IQEconomic?.Call("API_IS_REMOVED_BALANCE", player.userID, price))
                        {
                            IQEconomic?.Call("API_REMOVE_BALANCE", player.userID, price);
                            GiveItem(player, itembuy);
                            ShopMainUI(player, category, page, true);
                            SendChat(player, String.Format(lang.GetMessage("ShopSystem_CHAT_BUYITEM", this, player.UserIDString), NameItem, price));
                            Log($"Item buy\n Player {player.userID}, buy {NameItem} success");
                        }
                        else SendChat(player, lang.GetMessage("ShopSystem_CHAT_BUYITEM_NOMONEY", this, player.UserIDString));
                        break;
                    case "default":
                        if (BalanceData[player.userID] >= price)
                        {
                            BalanceData[player.userID] -= price;
                            GiveItem(player, itembuy);
                            ShopMainUI(player, category, page, true);
                            SendChat(player, String.Format(lang.GetMessage("ShopSystem_CHAT_BUYITEM", this, player.UserIDString), NameItem, price));
                            Log($"Item buy\n Player {player.userID}, buy {NameItem} success");

                        }
                        else SendChat(player, lang.GetMessage("ShopSystem_CHAT_BUYITEM_NOMONEY", this, player.UserIDString));
                        break;
                }
            }
            else SendChat(player, lang.GetMessage("Error", this));
        }

        private void GiveItem(BasePlayer player, ItemStores item)
        {
            switch (item.type)
            {
                case ItemType.КастомПредмет:
                    Item i = ItemManager.CreateByName(item.ShortName, item.Amount, item.SkinID);
                    i.name = item.Name;
                    player.GiveItem(i, BaseEntity.GiveItemReason.PickedUp);
                    break;
                case ItemType.Команда:
                    Server.Command(item.Command.Replace("%STEAMID%", player.UserIDString));
                    break;
                case ItemType.Предмет:
                    Item items = ItemManager.CreateByName(item.ShortName, item.Amount);
                    player.GiveItem(items, BaseEntity.GiveItemReason.PickedUp);
                    break;
                case ItemType.Чертёж:
                    Item itembp;
                    itembp = ItemManager.CreateByItemID(-996920608, item.Amount);
                    itembp.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == item.ShortName)?.itemid ?? 0;
                    player.GiveItem(itembp, BaseEntity.GiveItemReason.PickedUp);
                    break;
            }
        }

        #endregion

        #region Hooks

        void OnNewSave(string filename)
        {
            if (config.currencyForStores.WipeData)
            {
                BalanceData.Clear();
                PrintWarning("Обнаружен WIPE . Дата игроков сброшена");
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
            if (!config.humanNPCSettings.NpsUse && config.generalSetings.ButtonIcon)
            {
                InitIcon(player);
            }
            if (!BalanceData.ContainsKey(player.userID) && config.currencyForStores.TypeBalance == "default")
            {
                BalanceData.Add(player.userID, config.currencyForStores.BalanceStart);
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("BalanceData", BalanceData);
            }
        }

        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError($"ERROR! Plugin ImageLibrary not found!");
                Interface.Oxide.UnloadPlugin(Title);
            }
            LoadConfig();
            CheckCFG();

            if (!config.humanNPCSettings.NpsUse)
            {
                Unsubscribe("OnUseNPC");
                cmd.AddChatCommand(config.generalSetings.CommandOpenUI, this, nameof(opencommand));
            }

            if (config.currencyForStores.TypeBalance == "default")
            {
                if (Interface.Oxide.DataFileSystem.ExistsDatafile("BalanceData"))
                    BalanceData = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("BalanceData");
            }
            else
            {
                UnsubscribeHook();
            }
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }

            foreach (var perm in config.discountStores.DiscountPerm)
                permission.RegisterPermission(perm.Key, this);
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (config.humanNPCSettings.NpsId != npc.userID) return;
            ShopMainUI(player);
        }

        private void Unload()
        {
            if (config.currencyForStores.TypeBalance == "default")
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("BalanceData", BalanceData);

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], shopmain);
                CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], shopmain + "icon");
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return;
            if (config.dropCurrencySetings.OreMoneyDive.ContainsKey(item.info.shortname))
            {
                if (config.dropCurrencySetings.OreMoneyDive[item.info.shortname].Chance != 0)
                {
                    if (Chance(config.dropCurrencySetings.OreMoneyDive[item.info.shortname].Chance))
                    {
                        int money = RandomAmount(config.dropCurrencySetings.OreMoneyDive[item.info.shortname].MinDrop, config.dropCurrencySetings.OreMoneyDive[item.info.shortname].MaxDrop);
                        BalanceData[player.userID] += money;
                        SendChat(player, string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, player.UserIDString), money));
                    }
                }
            }
        }
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || entity.ToPlayer() == null || item == null || dispenser.gatherType != ResourceDispenser.GatherType.Flesh) return;
            if (config.dropCurrencySetings.CorpseMoneyDive.Chance != 0)
            {
                if (Chance(config.dropCurrencySetings.CorpseMoneyDive.Chance))
                {
                    int money = RandomAmount(config.dropCurrencySetings.CorpseMoneyDive.MinDrop, config.dropCurrencySetings.CorpseMoneyDive.MaxDrop);
                    BalanceData[entity.ToPlayer().userID] += money;
                    SendChat(entity.ToPlayer(), string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, entity.ToPlayer().UserIDString), money));
                }
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null) return;
                BasePlayer player = null;
                if (info.InitiatorPlayer != null)
                    player = info.InitiatorPlayer;
                else if (entity.GetComponent<BaseHelicopter>() != null)
                    player = BasePlayer.FindByID(GetLastAttacker(entity.net.ID));
                if (player == null) return;

                if (entity is NPCPlayer || entity is NPCMurderer)
                {
                    if (config.dropCurrencySetings.NPCMoneyDive.Chance != 0)
                    {
                        if (Chance(config.dropCurrencySetings.NPCMoneyDive.Chance))
                        {
                            int money = RandomAmount(config.dropCurrencySetings.NPCMoneyDive.MinDrop, config.dropCurrencySetings.NPCMoneyDive.MaxDrop);
                            BalanceData[player.userID] += money;
                            SendChat(player, string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, player.UserIDString), money));
                            return;
                        }
                    }
                }

                if (entity as BasePlayer)
                {
                    if (entity.ToPlayer() != null)
                    {
                        BasePlayer targetPlayer = entity.ToPlayer();
                        if (targetPlayer == null) return;
                        if (targetPlayer.userID != player.userID && config.dropCurrencySetings.PlayerMoneyDive.Chance != 0)
                        {
                            if (IsFriends(player.userID, targetPlayer.userID)) return;
                            if (IsClans(player.userID, targetPlayer.userID)) return;
                            if (IsDuel(player.userID)) return;
                            if (Chance(config.dropCurrencySetings.PlayerMoneyDive.Chance))
                            {
                                int money = RandomAmount(config.dropCurrencySetings.PlayerMoneyDive.MinDrop, config.dropCurrencySetings.PlayerMoneyDive.MaxDrop);
                                BalanceData[player.userID] += money;
                                SendChat(player, string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, player.UserIDString), money));
                                return;
                            }
                        }
                    }
                }

                if (entity is BaseAnimalNPC)
                {
                    if (config.dropCurrencySetings.AnimalMoneyDive.Chance != 0)
                    {
                        if (Chance(config.dropCurrencySetings.AnimalMoneyDive.Chance))
                        {
                            int money = RandomAmount(config.dropCurrencySetings.AnimalMoneyDive.MinDrop, config.dropCurrencySetings.AnimalMoneyDive.MaxDrop);
                            BalanceData[player.userID] += money;
                            SendChat(player, string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, player.UserIDString), money));
                            return;
                        }
                    }
                }

                if (entity is BradleyAPC)
                {
                    if (config.dropCurrencySetings.BradleyMoneyDive.Chance != 0)
                    {
                        if (Chance(config.dropCurrencySetings.BradleyMoneyDive.Chance))
                        {
                            int money = RandomAmount(config.dropCurrencySetings.BradleyMoneyDive.MinDrop, config.dropCurrencySetings.BradleyMoneyDive.MaxDrop);
                            BalanceData[player.userID] += money;
                            SendChat(player, string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, player.UserIDString), money));
                            return;
                        }
                    }
                }

                if (entity is BaseHelicopter)
                {
                    if (config.dropCurrencySetings.HeliMoneyDive.Chance != 0)
                    {
                        if (Chance(config.dropCurrencySetings.HeliMoneyDive.Chance))
                        {
                            player = BasePlayer.FindByID(GetLastAttacker(entity.net.ID));
                            int money = RandomAmount(config.dropCurrencySetings.HeliMoneyDive.MinDrop, config.dropCurrencySetings.HeliMoneyDive.MaxDrop);
                            BalanceData[player.userID] += money;
                            SendChat(player, string.Format(lang.GetMessage("ShopSystem_CHAT_MONEYGIVE", this, player.UserIDString), money));
                            return;
                        }
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                Log(ex.Message, "ShopError");
            }  
        }
        private Dictionary<uint, Dictionary<ulong, int>> HeliAttackers = new Dictionary<uint, Dictionary<ulong, int>>();

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (victim.GetComponent<BaseHelicopter>() != null && info?.Initiator?.ToPlayer() != null)
            {
                var heli = victim.GetComponent<BaseHelicopter>();
                var player = info.Initiator.ToPlayer();
                NextTick(() =>
                {
                    if (heli == null) return;
                    if (!HeliAttackers.ContainsKey(heli.net.ID))
                        HeliAttackers.Add(heli.net.ID, new Dictionary<ulong, int>());
                    if (!HeliAttackers[heli.net.ID].ContainsKey(player.userID))
                        HeliAttackers[heli.net.ID].Add(player.userID, 0);
                    HeliAttackers[heli.net.ID][player.userID]++;
                });
            }
        }

        private ulong GetLastAttacker(uint id)
        {
            int hits = 0;
            ulong majorityPlayer = 0U;
            if (HeliAttackers.ContainsKey(id))
            {
                foreach (var score in HeliAttackers[id])
                {
                    if (score.Value > hits)
                        majorityPlayer = score.Key;
                }
            }
            return majorityPlayer;
        }

        private IEnumerator DownloadImages()
        {
            if (!(bool)ImageLibrary?.Call("HasImage", "BluePrint")) ImageLibrary.Call("AddImage", "https://i.imgur.com/b48U2XA.png", "BluePrint");
            if (!(bool)ImageLibrary?.Call("HasImage", "Icon")) ImageLibrary.Call("AddImage", "https://i.imgur.com/StBTcET.png", "Icon");

            PrintError("AddImages...");
            foreach (var img in config.itemstores)
            {
                for (int i = 0; i < img.Value.Count; i++)
                {
                    if (img.Value[i].type == ItemType.КастомПредмет)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", img.Value[i].ShortName, img.Value[i].SkinID)) ImageLibrary.Call("AddImage", $"http://rust.skyplugins.ru/getskin/{img.Value[i].SkinID}/", img.Value[i].ShortName, img.Value[i].SkinID);
                    }
                    else if (img.Value[i].type == ItemType.Команда)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", img.Value[i].Url)) ImageLibrary.Call("AddImage", img.Value[i].Url, img.Value[i].Url);
                    }
                    else
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", img.Value[i].ShortName + 129)) ImageLibrary.Call("AddImage", $"http://rust.skyplugins.ru/getimage/{img.Value[i].ShortName}/128", img.Value[i].ShortName + 129);
                        if (!(bool)ImageLibrary?.Call("HasImage", img.Value[i].ShortName + 64)) ImageLibrary.Call("AddImage", $"http://rust.skyplugins.ru/getimage/{img.Value[i].ShortName}/64", img.Value[i].ShortName + 64);
                    }
                    yield return new WaitForSeconds(0.04f);

                }
            }
            yield return 0;
        }

        #endregion

        [ConsoleCommand("Money_give")]
        void ShopMoneyGive(ConsoleSystem.Arg arg)
        {
            switch (arg.Args[0])
            {
                case "give":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int Balance = Convert.ToInt32(arg.Args[2]);
                        BalanceData[userID] += Balance;
                        Puts($"Игроку {userID} успешно зачислено {Balance} монет");
                        break;
                    }
                case "remove":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int Balance = Convert.ToInt32(arg.Args[2]);
                        BalanceData[userID] -= Balance;
                        Puts($"Игроку {userID} успешно снято {Balance} монет");
                        break;
                    }
            }
        }

        [ConsoleCommand("Shop_CMD")]
        private void CMD_ShopToggle(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            switch (arg.Args[0])
            {
                case "CategoryGoTo":
                    ShopMainUI(player, Convert.ToInt32(arg.Args[1]), 0, true);
                    break;
                case "page":
                    ShopMainUI(player, Convert.ToInt32(arg.Args[1]), Convert.ToInt32(arg.Args[3]), true);
                    break;
                case "BuyItem":
                    ShopBuyItem(player, Convert.ToInt32(arg.Args[1]), Convert.ToInt32(arg.Args[2]), Convert.ToInt32(arg.Args[3]));
                    break;
                case "BuyItemGo":
                    PurchaseItem(player, Convert.ToInt32(arg.Args[1]), Convert.ToInt32(arg.Args[2]), Convert.ToInt32(arg.Args[3]));
                    break;
            }
        }

        #region UI
        #region Parent
        public static string shopmain = "SHOP_MAIN";
        #endregion

        #region GuiBuyItem


        public void ShopBuyItem(BasePlayer p, int category, int item, int page)
        {
            ItemStores itembuy = config.itemstores.ElementAt(category).Value.ElementAt(item);
            Item itemcrate = ItemManager.CreateByName(itembuy.ShortName);

            var dicsount = (itembuy.Price * GetUserDiscount(p) / 100);
            int price = (itembuy.Price - dicsount);
            string NameItem = itembuy.type == ItemType.Команда ? itembuy.Name : itembuy.type == ItemType.КастомПредмет ? itembuy.Name : itemcrate.info.displayName.english;
            var balance = config.currencyForStores.TypeBalance == "economic" ? Economics?.Call("Balance", p.userID).ToString() : config.currencyForStores.TypeBalance == "IQEconomic" ? IQEconomic?.Call("API_GET_BALANCE", p.userID).ToString() : BalanceData[p.userID].ToString();
            string Icon = itembuy.type == ItemType.Команда ? (string)ImageLibrary.Call("GetImage", itembuy.Url) : itembuy.type == ItemType.КастомПредмет ? (string)ImageLibrary.Call("GetImage", itembuy.ShortName, itembuy.SkinID) : (string)ImageLibrary.Call("GetImage", itembuy.ShortName + 64);
            #region UI

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-120 -60", OffsetMax = "120 65" },
                Image = { Color = GetColor("#FFFFFF00") }
            }, shopmain, shopmain + ".BUYYES");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Color = "0 0 0 0", Close = shopmain + ".BUYYES" },
                Text = { Text = "" }
            }, shopmain + ".BUYYES");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "3 2", OffsetMax = "118 28" },
                Button = { Color = GetColor("#89FE98BE"), Command = $"Shop_CMD BuyItemGo {category} {item} {page}" },
                Text = { Text =lang.GetMessage("ShopSystem_UI_BUY", this, p.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12 }
            }, shopmain + ".BUYYES");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-118 2", OffsetMax = "-3 28" },
                Button = { Color = GetColor("#FD8888BE"), Close = shopmain + ".BUYYES" },
                Text = { Text = lang.GetMessage("ShopSystem_UI_CLOSE", this, p.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12 }
            }, shopmain + ".BUYYES");

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -95", OffsetMax = "240 0" },
                Image = { Color = GetColor(config.generalSetings.ButonByeSucc), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, shopmain + ".BUYYES", shopmain + ".BUYYES1");

            if (itembuy.type == ItemType.Чертёж)
            {
                container.Add(new CuiElement
                {
                    Parent = shopmain + ".BUYYES1",
                    Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string)ImageLibrary.Call("GetImage", "BluePrint")

                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = "3 -92",
                                    OffsetMax = "92 -3"
                                },
                            }
                });
            }

            container.Add(new CuiElement
            {
                Parent = shopmain + ".BUYYES1",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = Icon
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = "3 -92",
                                    OffsetMax = "92 -3"
                                },
                            }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-140 3", OffsetMax = "-3 92" },
                Text = { Text = String.Format(lang.GetMessage("ShopSystem_UI_BUYITEM", this, p.UserIDString), NameItem, price, balance), FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, shopmain + ".BUYYES1");

            CuiHelper.AddUi(p, container);

            #endregion
        }

        #endregion

        public void InitIcon(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, shopmain + "icon");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Parent = "Overlay",
                Name = shopmain + "icon",
                Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Icon") },
                            new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = config.generalSetings.ButtonIconOffsetMin, OffsetMax = config.generalSetings.ButtonIconOffsetMax }
                        }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = $"chat.say /{config.generalSetings.CommandOpenUI}" },
                Text = { Text = "" }
            }, shopmain + "icon");

            CuiHelper.AddUi(player, container);
        }

        void opencommand(BasePlayer player)
        {
            ShopMainUI(player);
        }
        void ShopMainUI(BasePlayer player, int page = 0, int ispages = 0, bool update = false)
        {
            CuiHelper.DestroyUi(player, "SHOP_UI_BG");
            CuiHelper.DestroyUi(player, shopmain + ".BUYYES");
            int discount = GetUserDiscount(player);
            var balance = config.currencyForStores.TypeBalance == "economic" ? Economics?.Call("Balance", player.userID).ToString() : config.currencyForStores.TypeBalance == "IQEconomic" ? IQEconomic?.Call("API_GET_BALANCE", player.userID).ToString() : BalanceData[player.userID].ToString();

            CuiElementContainer container = new CuiElementContainer();
            if (!update)
            {
                CuiHelper.DestroyUi(player, shopmain);
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = config.generalSetings.MainUIOffsetMin, OffsetMax = config.generalSetings.MainUIOffsetMax },
                    Image = { Color = GetColor(config.generalSetings.FonColor), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, "Overlay", shopmain);

                #region Title

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.0947 0.903", AnchorMax = "0.891 0.982" },
                    Text = { Text = lang.GetMessage("ShopSystem_UI_TITLE", this, player.UserIDString), FontSize = 40, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, shopmain);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-140 -75", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Close = shopmain },
                    Text = { Text = lang.GetMessage("ShopSystem_UI_CLOSE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 17 }
                }, shopmain);

                #endregion

                #region Main

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "", OffsetMax = "1140 600" },
                    Image = { Color = GetColor("#292929B4") }
                }, shopmain, "SHOP_M");

                container.Add(new CuiElement
                {
                    Parent = "SHOP_M",
                    Name = "SHOP_M_a",
                    Components =
                {
                   new CuiImageComponent { Color = GetColor(config.generalSetings.Butoncategoryitem) },
                   new CuiRectTransformComponent {  AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "2 -31", OffsetMax = "221 -2" },
                }
                });

                container.Add(new CuiElement
                {
                    Parent = "SHOP_M",
                    Name = "SHOP_M_b",
                    Components =
                {
                   new CuiImageComponent { Color = GetColor(config.generalSetings.Butoncategoryactiveitem)},
                   new CuiRectTransformComponent {  AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-914 -31", OffsetMax = "-2 -2" },
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("ShopSystem_UI_CATEGORY1", this, player.UserIDString), FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "SHOP_M_a");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("ShopSystem_UI_CATEGORY2", this, player.UserIDString), FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "SHOP_M_b");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "223 0", OffsetMax = "224 600" },
                    Image = { Color = GetColor(config.generalSetings.FonColor), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, "SHOP_M");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 61", OffsetMax = "223 62" },
                    Image = { Color = GetColor(config.generalSetings.FonColor), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, "SHOP_M");
            }

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "", OffsetMax = "1140 600" },
                Image = { Color = GetColor("#FFFFFF00") }
            }, shopmain, "SHOP_UI_BG");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "221 60" },
                Text = { Text = String.Format(lang.GetMessage("ShopSystem_UI_BALANCEINFO", this, player.UserIDString), balance, discount), FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, $"SHOP_UI_BG");

            int size = 35;
            int i = 0;
            foreach (var cfg in config.itemstores)
            {
                string color = page == i ? config.generalSetings.CategoryFonActive : config.generalSetings.CategoryFonINActive;
                container.Add(new CuiElement
                {
                    Parent = "SHOP_UI_BG",
                    Name = $"SHOP_UI_BG.{i}",
                    Components =
                    {
                         new CuiImageComponent { Color = GetColor(color) },
                         new CuiRectTransformComponent
                         {
                             AnchorMin = $"0 1",
                             AnchorMax = $"0 1",
                             OffsetMin = $"2 {-65 - i*size}",
                             OffsetMax = $"221 {-35 - i*size}"
                         },
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"<b>{cfg.Key}</b>", FontSize = 19, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, $"SHOP_UI_BG.{i}");

                if (i != page)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Command = $"Shop_CMD CategoryGoTo {i}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, $"SHOP_UI_BG.{i}");
                }

                if (page == i)
                {
                    int a = 0;
                    int f = 0;
                    int s = 131;
                    int d = 133;
                    int itemsperpage = (cfg.Value.Count - 28 * ispages > 28 ? 28 : (cfg.Value.Count - 28 * ispages));
                    for (int sd = 0; sd < itemsperpage; sd++)
                    {
                        ItemStores ItemSale = cfg.Value.ElementAt(sd + ispages * 28);

                        container.Add(new CuiElement
                        {
                            Parent = "SHOP_UI_BG",
                            Name = $"SHOP_UI_BG.item.{f}.{a}",
                            Components =
                            {
                                new CuiImageComponent { Color = GetColor("#414141B6") },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",

                                    OffsetMin = $"{226 + f*s} {-162 - a*d}",
                                    OffsetMax = $"{352 + f*s} {-35 - a*d}"
                                },
                            }
                        });

                        if (ItemSale.type == ItemType.Чертёж)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = $"SHOP_UI_BG.item.{f}.{a}",
                                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string)ImageLibrary.Call("GetImage", "BluePrint")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"1 1",
                                    AnchorMax = $"1 1",
                                    OffsetMin = "-110 -95",
                                    OffsetMax = "-15 -5"
                                },
                            }
                            });
                        }

                        var trueImage = ItemSale.type == ItemType.Команда ? (string)ImageLibrary.Call("GetImage", ItemSale.Url) : ItemSale.type == ItemType.КастомПредмет ? (string)ImageLibrary.Call("GetImage", ItemSale.ShortName, ItemSale.SkinID) : (string)ImageLibrary.Call("GetImage", ItemSale.ShortName + 129);

                        container.Add(new CuiElement
                        {
                            Parent = $"SHOP_UI_BG.item.{f}.{a}",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = trueImage
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"1 1",
                                    AnchorMax = $"1 1",
                                    OffsetMin = "-110 -95",
                                    OffsetMax = "-15 -5"
                                },
                            }
                        });

                        #region Узнаем скидочку
                        var dicsount = (ItemSale.Price * discount / 100);
                        int price = (ItemSale.Price - dicsount);
                        #endregion

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = $"1 0", AnchorMax = $"1 0", OffsetMin = "-125 1", OffsetMax = "0 28" },
                            Text = { Text = $"<b>x{ItemSale.Amount}\nЦена: {price.ToString()}</b>", FontSize = 11, Align = TextAnchor.MiddleCenter }
                        }, $"SHOP_UI_BG.item.{f}.{a}");

                        //container.Add(new CuiLabel
                        //{
                        //    RectTransform = { AnchorMin = $"1 0", AnchorMax = $"1 0", OffsetMin = "-207 1", OffsetMax = "0 28" },
                        //    Text = { Text = $"<b>x{ItemSale.Amount}\nЦена: {price.ToString()}</b>", FontSize = 11, Align = TextAnchor.MiddleCenter }
                        //}, $"SHOP_UI_BG.item.{f}.{a}");

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            Button = { Color = "0 0 0 0", Command = $"Shop_CMD BuyItem {page} {sd + ispages * 28} {ispages}" },
                            Text = { Text = "" }
                        }, $"SHOP_UI_BG.item.{f}.{a}");

                        f++;
                        if (f >= 7)
                        {
                            a++; f = 0;
                        }

                    }
                    if (cfg.Value.Count > 20)
                    {
                        if (ispages > 0)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = "SHOP_UI_BG",
                                Name = $"SHOP_UI_BG.Back",
                                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = GetColor(config.generalSetings.ButonPageColor), Material = "assets/content/ui/uibackgroundblur.mat"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0 0",
                                        AnchorMax = $"0 0",
                                        OffsetMin = "227 2",
                                        OffsetMax = "315 30"
                                    }
                                }
                            });

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Button = { Color = "0 0 0 0", Command = $"Shop_CMD page {page} -1 {ispages - 1}" },
                                Text = { Text = lang.GetMessage("ShopSystem_UI_BUTTON_BACK", this, player.UserIDString), FontSize = 15, Align = TextAnchor.MiddleCenter }
                            }, $"SHOP_UI_BG.Back");
                        }
                        if (itemsperpage >= 24)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = "SHOP_UI_BG",
                                Name = $"SHOP_UI_BG.Next",
                                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = GetColor(config.generalSetings.ButonPageColor), Material = "assets/content/ui/uibackgroundblur.mat"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"1 0",
                                        AnchorMax = $"1 0",
                                        OffsetMin = "-90 2",
                                        OffsetMax = "-2 30"
                                    }
                                }
                            });

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Button = { Color = "0 0 0 0", Command = $"Shop_CMD page {page} -1 {ispages + 1}" },
                                Text = { Text = lang.GetMessage("ShopSystem_UI_BUTTON_NEXT", this, player.UserIDString), FontSize = 15, Align = TextAnchor.MiddleCenter }
                            }, $"SHOP_UI_BG.Next");
                        }
                    }
                }
                i++;
            }
            #endregion
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region help

        void Log(string msg, string file = "log")
        {
            LogToFile(file, $"[{DateTime.Now}] {msg}", this);
        }

        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, lang.GetMessage("ShopSystem_UI_TITLE", this, player.UserIDString));
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private static string GetColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private int GetUserDiscount(BasePlayer player)
        {
            int Discounts = 0;
            foreach (var Discount in config.discountStores.DiscountPerm)
            {
                if (permission.UserHasPermission(player.UserIDString, Discount.Key))
                    if (Discounts < Discount.Value)
                        Discounts = Discount.Value;
            }

            return Discounts;
        }

        #endregion
    }
}
