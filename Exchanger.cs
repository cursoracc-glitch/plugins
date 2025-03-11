using ConVar;

using Newtonsoft.Json;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Exchanger", "Sempai#3239", "2.0.0")]
    public class Exchanger : RustPlugin
    {
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Exchanger"] = "<size=16>Exchanger:</size>",
                ["Exchanger_not_enough"] = "You do not have enough {0} to share!",
                ["Exchanger_perfect"] = "Exchange completed successfully",
                ["Exchanger_no_auth"] = "In order to get a balance you must be logged in to the store!",
                ["Exchanger_UI_ex"] = "INTERNAL EXCHANGER",
                ["Exchanger_UI_yes"] = "EXCHANGE",
                ["Exchanger_UI_no"] = "NOT AVAILABLE",
                ["Exchanger_UI_course"] = "<b>EXCHANGE RATE</b>\n",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Exchanger"] = "<size=16>Обменник:</size>",
                ["Exchanger_not_enough"] = "У вас недостаточно {0} для обмена!",
                ["Exchanger_perfect"] = "Обмен совершен успешно",
                ["Exchanger_no_auth"] = "Для того чтобы получить баланс вы должны быть авторизованы в магазине!",
                ["Exchanger_UI_ex"] = "ВНУТРЕННИЙ ОБМЕННИК",
                ["Exchanger_UI_yes"] = "ОБМЕНЯТЬ",
                ["Exchanger_UI_no"] = "НЕДОСТУПНО",
                ["Exchanger_UI_course"] = "<b>КУРС ОБМЕНА</b>\n",

            }, this, "ru");
        }

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {

            [JsonProperty("Использовать магазин GameStore (Если OVH то ставим False)")]
            public bool UseGameStore;
            [JsonProperty("[GameStores] ID магазина")]
            public string ShopID = "ShopID";
            [JsonProperty("[GameStores] ID сервера")]
            public string ServerID = "";
            [JsonProperty("[GameStores] Секретный ключ")]
            public string SecretKey = "SecretKey";
            [JsonProperty("Команда для открытия обменника")]
            public string OpenEX;


            [JsonProperty("Настройка обменника")]
            public Dictionary<string, CustomItem> customs;


            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UseGameStore = false,
                    ServerID = "",
                    ShopID = "ShopID",
                    SecretKey = "SecretKey",
                    OpenEX = "emerald",

                    customs = new Dictionary<string, CustomItem>
                    {
                        ["Радужная+пыль"] = new CustomItem
                        {
                            DisplayName = "Радужная пыль",
                            ReplaceShortName = "glue",
                            DropAmount = 1,
                            ReplaceID = 1757726873,
                            PictureURL = "https://i.imgur.com/NtpIRnf.png",
                            containerList = new Dictionary<string, int>()
                            {
                                { "codelockedhackablecrate", 30 },
                                { "crate_elite", 25 },
                                { "loot_barrel_2", 20 },
                                { "crate_normal", 10 },
                                { "heli_crate", 40 },
                            },

                            ExchangeOptions = new CustomItem.Exchange
                            {
                                SecondItemName = "Радужный+осколок",
                                FirstAmount = 10,
                            }
                        },
                        ["Радужный+осколок"] = new CustomItem
                        {
                            DisplayName = "Радужный осколок",
                            ReplaceShortName = "ducttape",
                            DropAmount = 1,
                            ReplaceID = 1757727611,
                            PictureURL = "https://i.imgur.com/EIBMNK1.png",
                            containerList = new Dictionary<string, int>()
                            {
                                { "codelockedhackablecrate", 30 },
                                { "crate_elite", 25 },
                                { "loot_barrel_2", 20 },
                                { "crate_normal", 10 },
                                { "heli_crate", 40 },
                            },

                            ExchangeOptions = new CustomItem.Exchange
                            {
                                SecondItemName = "Радужный+кристалл",
                                FirstAmount = 5,
                            }
                        },
                        ["Радужный+кристалл"] = new CustomItem
                        {
                            DisplayName = "Радужный кристалл",
                            ReplaceShortName = "bleach",
                            DropAmount = 1,
                            ReplaceID = 1757728373,
                            PictureURL = "https://i.imgur.com/ePSDZQ2.png",
                            containerList = new Dictionary<string, int>()
                            {
                                { "codelockedhackablecrate", 30 },
                                { "crate_elite", 25 },
                                { "loot_barrel_2", 20 },
                                { "crate_normal", 10 },
                                { "heli_crate", 40 },
                            },

                            ExchangeOptions = new CustomItem.Exchange
                            {
                                SecondItemName = "Рубли",
                                FirstAmount = 2,
                            }
                        },
                        ["Рубли"] = new CustomItem
                        {
                            DisplayName = "Рубли",
                            ReplaceShortName = "rock",
                            DropAmount = 1,
                            ReplaceID = 0,
                            PictureURL = "https://i.imgur.com/s0qdh7c.png",
                            containerList = null,

                            ExchangeOptions = null
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
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintWarning("Ошибка139" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.GetNewConfiguration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Classes

        private class CustomItem
        {
            internal class Exchange
            {
                [JsonProperty("Обмен на какой предмет")]
                public string SecondItemName;
                [JsonProperty("Необходимое количество для обмена")]
                public int FirstAmount;
            }

            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("Название предмета который он будет заменять")]
            public string ReplaceShortName;

            [JsonProperty(PropertyName = "Контейнер тз которого может выпасть этот предмет и процент выпадения")]
            public Dictionary<string, int> containerList;
            [JsonProperty("Кол-вл выпадения")]
            public int DropAmount;

            [JsonProperty("Ссылка на изображение")]
            public string PictureURL;
            [JsonProperty("Скин ID предмета")]
            public ulong ReplaceID;

            [JsonProperty("Курс обмена предмета")]
            public Exchange ExchangeOptions = new Exchange();

            public int GetItemId()
            {
                return ItemManager.FindItemDefinition(ReplaceShortName).itemid;
            }

            public int GetItemAmount(BasePlayer player)
            {
                return player.inventory.GetAmount(GetItemId());
            }

            public Item Copy(int amount = 1)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;

                return x;
            }
            public void CreateItem(BasePlayer player, Vector3 position, int amount)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;

                if (player != null)
                {
                    if (player.inventory.containerMain.itemList.Count < 24)
                    {
                        x.MoveToContainer(player.inventory.containerMain);
                    }
                    else
                    {
                        x.Drop(player.transform.position, Vector3.zero);
                    }

                    return;
                }

                if (position != Vector3.zero)
                {
                    x.Drop(position, Vector3.down);
                    return;
                }
            }

            public bool CanExchange(BasePlayer player)
            {
                return GetItemAmount(player) >= ExchangeOptions.FirstAmount;
            }
        }

        #endregion

        #region Variables

        [PluginReference] private readonly Plugin ImageLibrary, IQChat;
        private static readonly System.Random Rnd = new System.Random();

        #endregion

        #region Initialization
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError($"ERROR! Plugin ImageLibrary not found!");
                return;
            }

            foreach (KeyValuePair<string, CustomItem> check in config.customs)
            {
                ImageLibrary.Call("AddImage", check.Value.PictureURL, check.Key);
            }

            cmd.AddChatCommand(config.OpenEX, this, nameof(cmdChatEmerald));
            if (config.UseGameStore)
            {
                if (config.ShopID == "ShopID" || config.SecretKey == "SecretKey")
                {
                    PrintError("Вы не настроили ShopID И SecretKey от магазина GameStores");
                    return;
                }
            }
        }

        #endregion

        #region Hooks
        private void AddEmeralds(LootContainer cont)
        {
            if (cont == null)
            {
                return;
            }

            Dictionary<string, int> spawnItems = new Dictionary<string, int>();

            foreach (KeyValuePair<string, CustomItem> emeralds in config.customs.Where(x => x.Value.ExchangeOptions != null))
            {
                foreach (KeyValuePair<string, int> em in emeralds.Value.containerList)
                {
                    if (cont.PrefabName.Contains(em.Key))
                    {
                        spawnItems.Add(emeralds.Key, em.Value);
                    }
                }
            }

            if (spawnItems.Count == 0)
            {
                return;
            }

            cont.minSecondsBetweenRefresh = -1;
            cont.maxSecondsBetweenRefresh = 0;
            cont.GetComponent<FacepunchBehaviour>().CancelInvoke(new Action(cont.SpawnLoot));

            KeyValuePair<string, int> rndItem = spawnItems.OrderBy(x => Rnd.Next()).FirstOrDefault();
            string emeraldKey = string.Empty;

            bool isGoodChance = Rnd.Next(0, 1000) / 10f <= rndItem.Value;
            if (isGoodChance)
            {
                emeraldKey = rndItem.Key;
            }

            if (string.IsNullOrEmpty(emeraldKey))
            {
                return;
            }

            CustomItem cfgItem = config.customs[rndItem.Key];
            Item item = cfgItem.Copy(cfgItem.DropAmount);

            if (cont.inventory.capacity < cont.inventory.itemList.Count() + 1)
            {
                cont.inventory.capacity++;
            }

            item.MoveToContainer(cont.inventory, -1, true);
        }
        private void OnLootSpawn(LootContainer container)
        {
            if (container == null)
            {
                return;
            }

            AddEmeralds(container);
        }

        private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin)
            {
                return false;
            }

            return null;
        }

        private object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin)
            {
                return false;
            }

            return null;
        }
        #endregion

        #region Commands

        [ConsoleCommand("emerald.give")]
        private void CmdConsoleGive(ConsoleSystem.Arg args)
        {
            if (args.Player() != null || !args.HasArgs(3))
            {
                return;
            }

            ulong targetId;
            string name = args.Args[1];
            int amount;
            if (ulong.TryParse(args.Args[0], out targetId) && int.TryParse(args.Args[2], out amount))
            {
                if (config.customs.ContainsKey(name))
                {
                    BasePlayer target = BasePlayer.FindByID(targetId);
                    if (target != null && target.IsConnected)
                    {
                        config.customs[name].CreateItem(target, Vector3.zero, amount);
                    }
                }
            }
        }

        [ConsoleCommand("exchangeUI")]
        private void cmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0].ToLower())
                {
                    case "exchange":
                        {
                            if (args.HasArgs(3))
                            {
                                if (config.customs.ContainsKey(args.Args[1]) && config.customs.ContainsKey(args.Args[2]))
                                {
                                    CustomItem firstItem = config.customs[args.Args[1]];
                                    CustomItem secondItem = config.customs[args.Args[2]];
                                    int firstAmount = firstItem.GetItemAmount(player);
                                    int secondAmount = (int)Math.Floor((double)firstAmount / firstItem.ExchangeOptions.FirstAmount);

                                    if (firstAmount < firstItem.ExchangeOptions.FirstAmount)
                                    {
                                        SendChat(lang.GetMessage("Exchanger", this, player.UserIDString), string.Format(lang.GetMessage("Exchanger_not_enough", this, player.UserIDString), firstItem.DisplayName), player);
                                        return;
                                    }

                                    if (secondItem.ExchangeOptions != null)
                                    {
                                        player.inventory.Take(null, firstItem.GetItemId(), (int)Math.Floor((double)firstAmount / firstItem.ExchangeOptions.FirstAmount) * firstItem.ExchangeOptions.FirstAmount);
                                        secondItem.CreateItem(player, Vector3.zero, secondAmount);
                                        SendChat(lang.GetMessage("Exchanger", this, player.UserIDString), lang.GetMessage("Exchanger_perfect", this, player.UserIDString), player);
                                    }
                                    else
                                    {
                                        if (config.UseGameStore)
                                        {
                                            string uri = $"https://gamestores.ru/api?shop_id={config.ShopID}&secret={config.SecretKey}&server={config.ServerID}&action=moneys&type=plus&steam_id={player.userID}&amount={secondAmount}&mess=Обменник";
                                            webrequest.Enqueue(uri, null, (i, s) =>
                                            {
                                                if (i != 200) { }
                                                if (s.Contains("success"))
                                                {
                                                    SendChat(lang.GetMessage("Exchanger", this, player.UserIDString), lang.GetMessage("Exchanger_perfect", this, player.UserIDString), player);
                                                    player.inventory.Take(null, firstItem.GetItemId(), (int)Math.Floor((double)firstAmount / firstItem.ExchangeOptions.FirstAmount) * firstItem.ExchangeOptions.FirstAmount);
                                                    CuiHelper.DestroyUi(player, Layer);
                                                }
                                                else
                                                {
                                                    SendChat(lang.GetMessage("Exchanger", this, player.UserIDString), lang.GetMessage("Exchanger_no_auth", this, player.UserIDString), player);
                                                    CuiHelper.DestroyUi(player, Layer);
                                                }
                                            }, this);
                                        }
                                        else
                                        {
                                            plugins.Find("RustStore")?.CallHook("APIChangeUserBalance", player.userID, secondAmount, new Action<string>((result) =>
                                            {
                                                if (result == "SUCCESS")
                                                {
                                                    SendChat(lang.GetMessage("Exchanger", this, player.UserIDString), lang.GetMessage("Exchanger_perfect", this, player.UserIDString), player);
                                                    player.inventory.Take(null, firstItem.GetItemId(), (int)Math.Floor((double)firstAmount / firstItem.ExchangeOptions.FirstAmount) * firstItem.ExchangeOptions.FirstAmount);
                                                    CuiHelper.DestroyUi(player, Layer);
                                                    return;
                                                }
                                                Interface.Oxide.LogDebug($"Баланс не был изменен, ошибка: {result}");
                                                SendChat(lang.GetMessage("Exchanger", this, player.UserIDString), lang.GetMessage("Exchanger_no_auth", this, player.UserIDString), player);
                                                CuiHelper.DestroyUi(player, Layer);
                                            }));
                                        }
                                    }

                                    UI_DrawExchange(player);
                                }
                            }
                            break;
                        }
                }
            }
        }

        [ConsoleCommand("emeralds")]
        private void ConsoleOpenMenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            UI_DrawExchange(player);
        }

        private void cmdChatEmerald(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 0 && args[0] == "secrettest" && player.IsAdmin)
            {
                foreach (KeyValuePair<string, CustomItem> check in config.customs)
                {
                    check.Value.CreateItem(player, Vector3.zero, 1000000);
                }
            }
            UI_DrawExchange(player);
        }

        #endregion

        #region Interface

        private const string Layer = "UI_Emerald";
        private void UI_DrawExchange(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-229 -143", OffsetMax = "229 143" },
                        Image = { Color = "0 0 0 0" }
                    },
                    "Hud",
                    Layer
                },

                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "-1000 -1000", AnchorMax = "1000 1000", OffsetMax = "0 0" },
                        Button = { Close = Layer, Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.9" },
                        Text = { Text = "" }
                    },
                    Layer
                },

                new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#FFFFFF0E"), Material = "assets/content/ui/scope_2.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
                },
                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.00509456 0.8319815", AnchorMax = "0.9992717 0.9965037", OffsetMax = "0 0" },
                        Text = { Text = lang.GetMessage("Exchanger_UI_ex", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter }
                    },
                    Layer
                }
            };

            foreach (var check in config.customs.Select((i, t) => new { A = i, B = t }).Where(p => p.A.Value.ExchangeOptions != null))
            {
                #region FirstItem

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.1084428 {0.5722611 - check.B * 0.25}", AnchorMax = $"0.2540031 {0.8053614 - check.B * 0.25}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#7777773C"), Material = "assets/icons/iconmaterial.mat" },
                    Text = { Text = "x" + check.A.Value.ExchangeOptions.FirstAmount + " ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer, "EmeraldFirst" + check.A.Key);

                container.Add(new CuiElement
                {
                    Parent = "EmeraldFirst" + check.A.Key,
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.A.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });

                #endregion

                #region Button Change

                string btnColor = check.A.Value.CanExchange(player) ? "#77A16DFF" : "#A16D6DFF";
                string btnText = check.A.Value.CanExchange(player) ? lang.GetMessage("Exchanger_UI_yes", this, player.UserIDString) : lang.GetMessage("Exchanger_UI_no", this, player.UserIDString);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.3 {0.5722611 - check.B * 0.25}", AnchorMax = $"0.7 {0.6857343 - check.B * 0.25}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat(btnColor), Command = $"exchangeUI exchange {check.A.Key} {check.A.Value.ExchangeOptions.SecondItemName}", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = btnText, Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter }
                }, Layer, "ExchangeButton" + check.A.Key);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1.1", AnchorMax = "1 2.1" },
                    Text = { FontSize = 14, Text =lang.GetMessage("Exchanger_UI_course", this, player.UserIDString) +
                                    $"{check.A.Value.DisplayName} x{check.A.Value.ExchangeOptions.FirstAmount} -> {check.A.Value.ExchangeOptions.SecondItemName} x1", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                        Color = HexToRustFormat("#BEBEBE94") }
                }, "ExchangeButton" + check.A.Key);

                #endregion

                #region SecondItem

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.7620094 {0.5722611 - check.B * 0.25}", AnchorMax = $"0.9075698 {0.8053614 - check.B * 0.25}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#7777773C"), Material = "assets/icons/iconmaterial.mat" },
                    Text = { Text = "x1 ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer, "EmeraldSecond" + check.A.Key);

                container.Add(new CuiElement
                {
                    Parent = "EmeraldSecond" + check.A.Key,
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.A.Value.ExchangeOptions.SecondItemName) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });

                #endregion
            }

            CuiHelper.AddUi(player, container);
        }

        public void SendChat(string Descrip, string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
            {
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Descrip);
            }
            else
            {
                player.SendConsoleCommand("chat.add", channel, 0, Message);
            }
        }

        #endregion

        #region Utils

        private string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            string str = hex.Trim('#');

            if (str.Length == 6)
            {
                str += "FF";
            }

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            byte r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            byte a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}