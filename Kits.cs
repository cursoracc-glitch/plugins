using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Globalization;
using Oxide.Core;
using System.IO;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Kits", "DeliciousDev.", "2.0.0")]
    class Kits : RustPlugin
    {
        private List<Kit> _kits;
        private Dictionary<ulong, Dictionary<string, KitData>> _kitsData;
        private Dictionary<BasePlayer, List<string>> _kitsGUI = new Dictionary<BasePlayer, List<string>>();
        private static List<string> CustomAutoKits = new List<string>();

		public string color = "<color=#34495E>";
		public string colorend = "</color>";

        #region Конфиг
        private string ImagesColor = "#96969623";
        private string MaskImagesColor = "#96969600";
        private float KitWidth = 0.12f;
        private float MarginBetween = 0.01f;
        private float MarginBottom  = 0.5f;
        private float MarginTop = 0.44f;

        private void LoadDefaultConfig()
        {
            GetConfig("Основные настройки", "Цвет доступного набора (HEX, два последних значения - прозрачность)", ref ImagesColor);
            GetConfig("Основные настройки", "Цвет недоступного набора (HEX, два последних значения - прозрачность)", ref MaskImagesColor);

            GetConfig("Кнопки", "Ширина", ref KitWidth);
            GetConfig("Кнопки", "Смещение", ref MarginBetween);
            GetConfig("Кнопки", "Смещение вниз", ref MarginBottom);
            GetConfig("Кнопки", "Смещение вверх", ref MarginTop);

            var CustomAutoKits = new List<object>
            {
                {"autokit1"},
                {"autokit2"}
            };

            GetConfig("Наборы", "Набор на респавне", ref CustomAutoKits);
            SaveConfig();
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
        #endregion

        #region Класс

        public class Kit
        {
            [JsonProperty("Название")]
            public string Name;
            [JsonProperty("Формат названия")]
            public string DisplayName;
            [JsonProperty("Максимум использований")]
            public int Amount;
            [JsonProperty("Кулдаун")]
            public double Cooldown;
            [JsonProperty("Виден или скрыт")]
            public bool Hide;
            [JsonProperty("Привилегия")]
            public string Permission;
            [JsonProperty("Предметы")]
            public List<KitItem> Items;
            [JsonProperty("Изображение")]
            public string Png;
        }

        public class KitItem
        {
            [JsonProperty("Название предмета")]
            public string ShortName;
            [JsonProperty("Количество")]
            public int Amount;
            [JsonProperty("Чертеж")]
            public int Blueprint;
            [JsonProperty("Место")]
            public string Container;
            [JsonProperty("Состояние")]
            public float Condition;
            [JsonProperty("Скин")]
            public ulong SkinID;
            [JsonProperty("Оружие")]
            public Weapon Weapon;
            public List<ItemContent> Content;

        }
        public class Weapon
        {
            [JsonProperty("Название боеприпаса")]
            public string ammoType;
            [JsonProperty("Количество")]
            public int ammoAmount;
        }
        public class ItemContent
        {
            [JsonProperty("Название предмета")]
            public string ShortName;
            [JsonProperty("Состояние")]
            public float Condition;
            [JsonProperty("Количество")]
            public int Amount;
        }

        public class KitData
        {
            [JsonProperty("Количество")]
            public int Amount;
            [JsonProperty("Кулдаун")]
            public double Cooldown;
        }

        #endregion

        #region Оксид

		void OnNewSave(string filename)
		{ 
			_kitsData.Clear();
			PrintWarning("Очищаем дата-файл использований китов...");
		}

        void OnPlayerRespawned(BasePlayer player)
        {
            foreach (var kits in CustomAutoKits)
            {
                if (_kits.Exists(x => x.Name == kits))
                {
                    var kit1 = _kits.First(x => x.Name == kits);
                    if (permission.UserHasPermission(player.UserIDString, kit1.Permission))
                    {
                        player.inventory.Strip();
                        GiveItems(player, kit1);
                        return;
                    }
                }
            }
            if (_kits.Exists(x => x.Name == "autokit"))
            {
                player.inventory.Strip();
                var kit = _kits.First(x => x.Name == "autokit");
                GiveItems(player, kit);
            }

        }
        private void SaveKits()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits", _kits);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits_Data", _kitsData);
        }

        void OnServerSave()
        {
            SaveData();
            SaveKits();
        }

        private void Loaded()
        {
            LoadConfig();
            LoadDefaultConfig();

            _kits = Interface.Oxide.DataFileSystem.ReadObject<List<Kit>>("Kits");
            _kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("Kits_Data");
        }

        private void Unload()
        {
            SaveData();

            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }

        private void OnServerInitialized()
        {
            LoadConfig();
            LoadDefaultConfig();

            foreach (var kit in _kits)
            {
                if (!permission.PermissionExists(kit.Permission))
                    permission.RegisterPermission(kit.Permission, this);
            }

            timer.Repeat(1, 0, RefreshCooldownKitsUI);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            _kitsGUI.Remove(player);
        }

        #endregion

        #region Команды
        [ConsoleCommand("kit")]
        private void CommandConsoleKit(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
                return;

            var player = arg.Player();

            if (!arg.HasArgs())
                return;

            var value = arg.Args[0].ToLower();

            if (value == "ui")
            {
                TriggerUI(player);
                return;
            }

            if (!_kitsGUI.ContainsKey(player))
                return;

            if (!_kitsGUI[player].Contains(value))
                return;

            GiveKit(player, value);

            var container = new CuiElementContainer();
            var kit = _kits.First(x => x.Name == value);
            var playerData = GetPlayerData(player.userID, value);

            if (kit.Amount > 0)
            {
                if (playerData.Amount >= kit.Amount)
                {
                    foreach (var kitname in _kitsGUI[player])
                    {
                        CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.button");
                        CuiHelper.DestroyUi(player, $"ui.kits.{kitname}");
                    }

                    CuiHelper.AddUi(player, container);
                    return;
                }
            }

            if (kit.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (playerData.Cooldown > currentTime)
                {
                    CuiHelper.DestroyUi(player, $"ui.kits.{value}.button");

                    InitilizeMaskUI(ref container, kit.Name);

                    container.Add(new CuiLabel
                    {
                        Text = { Color = "1 1 1 1", Font = "RobotoCondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Text = $"{kit.DisplayName}" }
                    }, $"ui.kits.{kit.Name}");
                    InitilizeCooldownLabelUI(ref container, value, TimeSpan.FromSeconds(playerData.Cooldown - currentTime));
                }
            }

            CuiHelper.AddUi(player, container);

            return;
        }

        [ChatCommand("kit")]
        private void CommandChatKit(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (args.Length == 0)
            {
                TriggerUI(player);
                return;
            }

            if (!player.IsAdmin)
            {
                GiveKit(player, args[0].ToLower());
                return;
            }

            switch (args[0].ToLower())
            {
                case "help":
                    SendReply(player, "Команды:\n/kit new [название]: создать набор\n/kit clone [название]: сделать копию\n/kit remove [название]: удалить набор\n/kit list: список наборов\n/kit reset: обнулить все наборы");
                    return;
                case "new":
                    if (args.Length < 2)
                        SendReply(player, "/kit new [название]: создать набор");
                    else
                        KitCommandAdd(player, args[1].ToLower());
                    return;
                case "clone":
                    if (args.Length < 2)
                        SendReply(player, "/kit clone [название]: сделать копию");
                    else
                        KitCommandClone(player, args[1].ToLower());
                    return;
                case "remove":
                    if (args.Length < 2)
                        SendReply(player, "/kit remove [название]: удалить набор");
                    else
                        KitCommandRemove(player, args[1].ToLower());
                    return;
                case "list":
                    KitCommandList(player);
                    return;
                case "reset":
                    KitCommandReset(player);
                    return;
                case "give":
                    if (args.Length < 3)
                    {
                        SendReply(player, "/kit give [название] Ник/SteamID");
                    }
                    else
                    {
                        var foundPlayer = FindPlayer(player, args[1].ToLower());
                        if (foundPlayer == null)
                            return;
                        SendReply(player, $"Вы успешно выдали игроку {foundPlayer.displayName} набор {args[2]}");
                        KitCommandGive(player, foundPlayer, args[2].ToLower());
                    }
                    return;
                default:
                    GiveKit(player, args[0].ToLower());
                    return;
            }
        }

        [ConsoleCommand("kits")]
        private void ConsoleBag(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "skip")
                {
                    InitilizeUI(player, int.Parse(args.Args[1]));
                }
            }
        }
        #endregion

        #region Кит

        private bool GiveKit(BasePlayer player, string kitname)
        {
            if (string.IsNullOrEmpty(kitname))
                return false;

            if (Interface.Oxide.CallHook("canRedeemKit", player) != null)
            {
                return false;
            }
            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Этого комплекта не существует");
                return false;
            }

            var kit = _kits.First(x => x.Name == kitname);

            if (!string.IsNullOrEmpty(kit.Permission) && !permission.UserHasPermission(player.UserIDString, kit.Permission))
            {
                SendReply(player, "У вас нет полномочий использовать этот комплект");
                return false;
            }

            var playerData = GetPlayerData(player.userID, kitname);

            if (kit.Amount > 0 && playerData.Amount >= kit.Amount)
            {
                SendReply(player, "Вы уже использовали этот комплект максимальное количество раз");
                return false;
            }

            if (kit.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (playerData.Cooldown > currentTime)
                {
                    SendReply(player, $"Вы сможете использовать этот комплект через {TimeExtensions.FormatTime(TimeSpan.FromSeconds(playerData.Cooldown - currentTime))}");
                    return false;
                }
            }

            //foreach (var item in kit.Items)
            //    player.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount, item.Skin));
            int beltcount = kit.Items.Where(i => i.Container == "belt").Count();
            int wearcount = kit.Items.Where(i => i.Container == "wear").Count();
            int maincount = kit.Items.Where(i => i.Container == "main").Count();
            int totalcount = beltcount + wearcount + maincount;
            if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < beltcount || (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < wearcount || (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < maincount)
                if (totalcount > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
                {
                    player.ChatMessage($"Недостаточно места в инвентаре");
                    return false;
                }
            GiveItems(player, kit);

            if (kit.Amount > 0)
                playerData.Amount += 1;

            if (kit.Cooldown > 0)
                playerData.Cooldown = GetCurrentTime() + kit.Cooldown;

            SendReply(player, $"Вы получили комплект {kit.DisplayName}");
            return true;
        }

        private void KitCommandAdd(BasePlayer player, string kitname)
        {
            if (_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Этот набор уже существует");
                return;
            }

            _kits.Add(new Kit
            {
                Name = kitname,
                DisplayName = kitname,
                Cooldown = 600,
                Hide = true,
                Permission = "kits.default",
                Amount = 0,
                Items = GetPlayerItems(player)
            });
            permission.RegisterPermission($"kits.default", this);
            SendReply(player, $"Вы создали новый набор {kitname}");

            SaveKits();
            SaveData();
        }

        private void KitCommandClone(BasePlayer player, string kitname)
        {
            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Этого комплекта не существует");
                return;
            }

            _kits.First(x => x.Name == kitname).Items = GetPlayerItems(player);

            SendReply(player, $"Предметы были скопированы из инвентаря в набор {kitname}");

            SaveKits();
        }

        private void KitCommandRemove(BasePlayer player, string kitname)
        {
            if (_kits.RemoveAll(x => x.Name == kitname) <= 0)
            {
                SendReply(player, "Этого комплекта не существует");
                return;
            }

            SendReply(player, $"Набор {kitname} был удалён");

            SaveKits();
        }

        private void KitCommandList(BasePlayer player)
        {
            foreach (var kit in _kits)
                SendReply(player, $"{kit.Name} - {kit.DisplayName}");
        }

        private void KitCommandReset(BasePlayer player)
        {
            _kitsData.Clear();

            SendReply(player, "Вы обнулили все данные о использовании комплектов игроков");
        }

        private void KitCommandGive(BasePlayer player, BasePlayer foundPlayer, string kitname)
        {
            var reply = 1;
            if (reply == 0) { }
            if (!_kits.Exists(x => x.Name == reply.ToString())) { }

            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Этого комплекта не существует");
                return;
            }

            GiveItems(foundPlayer, _kits.First(x => x.Name == kitname));
        }
        private void GiveItems(BasePlayer player, Kit kit)
        {
            foreach (var kitem in kit.Items)
            {
                GiveItem(player,
                    BuildItem(kitem.ShortName, kitem.Amount, kitem.SkinID, kitem.Condition, kitem.Blueprint,
                        kitem.Weapon, kitem.Content),
                    kitem.Container == "belt" ? player.inventory.containerBelt :
                    kitem.Container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
            }
        }
        private void GiveItem(BasePlayer player, Item item, ItemContainer cont = null)
        {
            if (item == null) return;
            var inv = player.inventory;

            var moved = item.MoveToContainer(cont) || item.MoveToContainer(inv.containerMain);
            if (!moved)
            {
                if (cont == inv.containerBelt)
                    moved = item.MoveToContainer(inv.containerWear);
                if (cont == inv.containerWear)
                    moved = item.MoveToContainer(inv.containerBelt);
            }

            if (!moved)
                item.Drop(player.GetCenter(),player.GetDropVelocity());
        }
        private Item BuildItem(string ShortName, int Amount, ulong SkinID, float Condition, int blueprintTarget, Weapon weapon, List<ItemContent> Content)
        {
            Item item = ItemManager.CreateByName(ShortName, Amount > 1 ? Amount : 1, SkinID);
            item.condition = Condition;

            if (blueprintTarget != 0)
                item.blueprintTarget = blueprintTarget;

            if (weapon != null)
            {
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.ammoAmount;
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType = ItemManager.FindItemDefinition(weapon.ammoType);
            }
            if (Content != null)
            {
                foreach (var cont in Content)
                {
                    Item new_cont = ItemManager.CreateByName(cont.ShortName, cont.Amount);
                    new_cont.condition = cont.Condition;
                    new_cont.MoveToContainer(item.contents);
                }
            }
            return item;
        }
        #endregion

        #region Гуи
        private void TriggerUI(BasePlayer player)
        {
            if (_kitsGUI.ContainsKey(player))
                DestroyUI(player);
            else
                InitilizeUI(player);
        }

        private string Layer = "ui.kits";

        private void InitilizeUI(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            _kitsGUI[player] = new List<string>();
            var currentTime = GetCurrentTime();
            var kits = GetKitsForPlayer(player).Skip(page * 7).Take(7).ToList();
            var pos = 0.5f - (kits.Count * KitWidth + (kits.Count - 1) * MarginBetween) / 2;
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Color = "0 0 0 0.85", FadeIn = 0.25f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent { Text = "ВЫБЕРИТЕ НАБОР ДЛЯ ПОЛУЧЕНИЯ", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "RobotoCondensed-bold.ttf"},
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.7 0.7" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.2916667", AnchorMax = "1 0.9416667"}
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent { Text = "Чтобы закрыть, нажмите по пустому месту", Align = TextAnchor.MiddleCenter, FontSize = 15, Font = "RobotoCondensed-regular.ttf"},
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.7 0.7" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.138889", AnchorMax = "1 0.7888891"}
                }
            });

            container.Add(new CuiButton
            {
                Button = { Close = Layer, Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                Button = { Command = "kit ui", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.005 0.5", AnchorMax = $"0.045 0.57" },
                Button = { Color = "1 1 1 0", Command = $"kits skip {page - 1}" },
                Text = { Text = $"<", Color = "1 1 1 1", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter}
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.95 0.5", AnchorMax = $"0.995 0.57" },
                Button = { Color = "1 1 1 0", Command = $"kits skip {page + 1}" },
                Text = { Text = $">", Color = "1 1 1 1", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
            }, Layer);

            foreach (var kit in kits)
            {
                _kitsGUI[player].Add(kit.Name);
                var playerData = GetPlayerData(player.userID, kit.Name);

                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(ImagesColor), Material = "assets/content/ui/ui.background.tiletex.psd" },
                    RectTransform = { AnchorMin = $"{pos} {MarginBottom}", AnchorMax = $"{pos + KitWidth} {1.01f - MarginTop}" },
                    Text = { Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Text = $"{kit.DisplayName}" }
                }, Layer, $"ui.kits.{kit.Name}");
                pos += KitWidth + MarginBetween;

                if (kit.Cooldown > 0 && (playerData.Cooldown > currentTime))
                {
                    InitilizeMaskUI(ref container, kit.Name);
                    InitilizeNameLabelUI(ref container, kit.Name, kit.DisplayName);
                    InitilizeCooldownLabelUI(ref container, kit.Name, TimeSpan.FromSeconds(playerData.Cooldown - currentTime));
                }
                else
                {
                    InitilizeButtonUI(ref container, kit.Name);
                }
            }

            CuiHelper.AddUi(player, container);
        }

        private void InitilizeNameLabelUI(ref CuiElementContainer container, string kitname, string text)
        {
            container.Add(new CuiLabel
            {
                Text = { Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Text = text }
            }, $"ui.kits.{kitname}");
        }

        private void InitilizeButtonUI(ref CuiElementContainer container, string kitname)
        {
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"kit {kitname}" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" }
            }, $"ui.kits.{kitname}", $"ui.kits.{kitname}.button");
        }

        private void InitilizeMaskUI(ref CuiElementContainer container, string kitname)
        {
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.9915 0.9915" }
            }, $"ui.kits.{kitname}", $"ui.kits.{kitname}.mask");
        }

        private void InitilizeCooldownLabelUI(ref CuiElementContainer container, string kitname, TimeSpan time)
        {
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, $"ui.kits.{kitname}", $"ui.kits.{kitname}.time");

            container.Add(new CuiLabel
            {
                Text = { Color = "1 1 1 1", Font = "RobotoCondensed-regular.ttf", FontSize = 10, Align = TextAnchor.LowerCenter, Text = TimeExtensions.FormatShortTime(time) }
            }, $"ui.kits.{kitname}.time");
        }

        private void RefreshCooldownKitsUI()
        {
            var currentTime = GetCurrentTime();
            foreach (var playerGUIData in _kitsGUI)
            {
                var container = new CuiElementContainer();
                if (!_kitsData.ContainsKey(playerGUIData.Key.userID)) continue;
                var playerKitsData = _kitsData[playerGUIData.Key.userID];
                foreach (var kitname in playerGUIData.Value)
                {
                    var playerKitData = playerKitsData[kitname];
                    if (playerKitData.Cooldown > 0)
                    {
                        CuiHelper.DestroyUi(playerGUIData.Key, $"ui.kits.{kitname}.time");
                        if (playerKitData.Cooldown < currentTime)
                        {
                            CuiHelper.DestroyUi(playerGUIData.Key, $"ui.kits.{kitname}.mask");
                            InitilizeButtonUI(ref container, kitname);
                        }
                        else
                        {
                            InitilizeCooldownLabelUI(ref container, kitname, TimeSpan.FromSeconds(playerKitData.Cooldown - currentTime));
                        }
                    }
                }
                CuiHelper.AddUi(playerGUIData.Key, container);
            }
        }

        #endregion

        #region Хелпер

        private KitData GetPlayerData(ulong userID, string name)
        {
            if (!_kitsData.ContainsKey(userID))
                _kitsData[userID] = new Dictionary<string, KitData>();

            if (!_kitsData[userID].ContainsKey(name))
                _kitsData[userID][name] = new KitData();

            return _kitsData[userID][name];
        }

        private List<KitItem> GetPlayerItems(BasePlayer player)
        {
            List<KitItem> kititems = new List<KitItem>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "wear");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "main");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "belt");
                    kititems.Add(iteminfo);
                }
            }
            return kititems;
        }

        string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);

        private KitItem ItemToKit(Item item, string container)
        {
            KitItem kitem = new KitItem();
            kitem.Amount = item.amount;
            kitem.Container = container;
            kitem.SkinID = item.skin;
            kitem.Blueprint = item.blueprintTarget;
            kitem.ShortName = item.info.shortname;
            kitem.Condition = item.condition;
            kitem.Weapon = null;
            kitem.Content = null;
            if (item.info.category == ItemCategory.Weapon)
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    kitem.Weapon = new Weapon();
                    kitem.Weapon.ammoType = weapon.primaryMagazine.ammoType.shortname;
                    kitem.Weapon.ammoAmount = weapon.primaryMagazine.contents;
                }
            }
            if (item.contents != null)
            {
                kitem.Content = new List<ItemContent>();
                foreach (var cont in item.contents.itemList)
                {
                    kitem.Content.Add(new ItemContent()
                    {
                        Amount = cont.amount,
                        Condition = cont.condition,
                        ShortName = cont.info.shortname
                    });
                }
            }
            return kitem;
        }

        private List<Kit> GetKitsForPlayer(BasePlayer player)
        {
            return _kits.Where(kit => kit.Hide == false && (string.IsNullOrEmpty(kit.Permission) || permission.UserHasPermission(player.UserIDString, kit.Permission)) && (kit.Amount == 0 || (kit.Amount > 0 && GetPlayerData(player.userID, kit.Name).Amount < kit.Amount))).ToList();
        }

        private BasePlayer FindPlayer(BasePlayer player, string nameOrID)
        {
            ulong id;
            if (ulong.TryParse(nameOrID, out id) && nameOrID.StartsWith("") && nameOrID.Length == 17)
            {
                var findedPlayer = BasePlayer.FindByID(id);
                if (findedPlayer == null || !findedPlayer.IsConnected)
                {
                    SendReply(player, "Игрок не найден");
                    return null;
                }

                return findedPlayer;
            }

            var foundPlayers = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrID.ToLower()));

            if (foundPlayers.Count() == 0)
            {
                SendReply(player, "Игрок не найден");
                return null;
            }

            if (foundPlayers.Count() > 1)
            {
                SendReply(player, "Найдено несколько игроков");
                return null;
            }

            return foundPlayers.First();
        }

        private double GetCurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

        private static class TimeExtensions
        {
            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{time.Days} дней ";

                if (time.Hours != 0)
                    result += $"{time.Hours} час ";

                if (time.Minutes != 0)
                    result += $"{time.Minutes} мин ";

                if (time.Seconds != 0)
                    result += $"{time.Seconds} сек ";

                return result;
            }

            public static string FormatTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{Format(time.Days, "дней", "дня", "день")} ";

                if (time.Hours != 0)
                    result += $"{Format(time.Hours, "часов", "часа", "час")} ";

                if (time.Minutes != 0)
                    result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

                if (time.Seconds != 0)
                    result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units} {form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units} {form2}";

                return $"{units} {form3}";
            }
        }

        private static string HexToRustFormat(string hex)
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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private void DestroyUI(BasePlayer player)
        {
            if (!_kitsGUI.ContainsKey(player))
                return;

            foreach (var kitname in _kitsGUI[player])
            {
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.time");

                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.button");
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}.mask");
                CuiHelper.DestroyUi(player, $"ui.kits.{kitname}");
            }
            CuiHelper.DestroyUi(player, Layer);

            _kitsGUI.Remove(player);
        }

        #endregion
    }
}
                             