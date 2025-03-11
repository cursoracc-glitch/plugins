using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Backpack", "TopPlugin.ru", "1.0.41")]
    public class Backpack : RustPlugin
    {
        #region Fields 

        [PluginReference] private Plugin ImageLibrary;

        public Dictionary<ulong, BackpackStorage> opensBackpack = new Dictionary<ulong, BackpackStorage>();
        public static Backpack ins = null;
        public string Layer = "UI.Backpack";
        public string LayerBlur = "UI.Backpack.Blur";

        #endregion

        #region Commands

        [ChatCommand("updatecraft")]
        void chatCmdUpdateCraft(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            List<ItemInfo> craftInfo = new List<ItemInfo>();

            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (craftInfo.Find(x => x.shortname.Equals(item.info.shortname)) != null)
                {
                    continue;
                }

                craftInfo.Add(new ItemInfo()
                {
                    shortname = item.info.shortname,
                    amount = item.amount,
                    skinID = item.skin,
                    itemID = item.info.itemid
                });
            }

            _config.items = craftInfo;

            SendReply(player, "Крафт успешно обновлён!");

            CheckConfig();

            SaveConfig();
        }

        [ConsoleCommand("backpack.give")]
        void chatCmdBackpackGive(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            var player = BasePlayer.FindByID(arg.GetULong(0));

            if (player == null)
            {
                PrintError("Игрок не был найден!");
                return;
            }

            Item item = ItemManager.CreateByPartialName("santabeard", 1);
            item.name = _config.displayName;
            item.skin = _config.skinIdBackpack;

            string trimmed = _config.displayName.Trim();
            var name = trimmed.Substring(0, trimmed.IndexOf('\n'));
            item.MoveToContainer(player.inventory.containerMain);
            player.SendConsoleCommand($"note.inv {item.info.itemid} 1 \"{name}\"");
            PrintWarning($"Выдали рюкзак игроку {player.displayName}");
        }

        [ConsoleCommand("UI_Backpack")]
        void consoleCommandBackpackUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;

            if (player == null) return;

            switch (arg.GetString(0))
            {
                case "close":

                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, LayerBlur);

                    break;
                case "menuha":

                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, LayerBlur);
                    player.SendConsoleCommand("chat.say /menu");
                    break;

                case "craft":

                    var success = true;

                    Dictionary<Item, int> items = new Dictionary<Item, int>();

                    foreach (var craftedItem in _config.items)
                    {

                        var haveItem = HaveItem(player, craftedItem.itemID, craftedItem.skinID, craftedItem.amount);
                        if (!haveItem)
                        {
                            success = false;
                            SendReply(player,
                        "[<color=#ff8f3a><size=16>Создание рюкзака</size></color>]\n<color=#ff0000>Вы не можете скрафтить предмет!</color> Не хватает ингредиента!");
                            return;
                        }
                        var itemCraft = FindItem(player, craftedItem.itemID, craftedItem.skinID, craftedItem.amount);

                        items.Add(itemCraft, craftedItem.amount);
                    }

                    foreach (var itemCraft in items)
                    {
                        itemCraft.Key.UseItem(itemCraft.Value);
                    }

                    if (success)
                    {
                        player.SendConsoleCommand("UI_Backpack close");
                        Item craft = ItemManager.CreateByName("santabeard", 1, _config.skinIdBackpack);

                        craft.name = _config.displayName;

                        craft.MoveToContainer(player.inventory.containerMain);

                        string trimmed = _config.displayName.Trim();
                        var name = trimmed.Substring(0, trimmed.IndexOf('\n'));
                        player.SendConsoleCommand($"note.inv {craft.info.itemid} 1 \"{name}\"");
                    }

                    break;
            }
        }

        [ChatCommand("backpack.spawn")]
        void chatCmdBackpackSpawn(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            Item item = ItemManager.CreateByPartialName("santabeard", 1);
            item.name = _config.displayName;
            item.skin = _config.skinIdBackpack;
            string trimmed = _config.displayName.Trim();
            var name = trimmed.Substring(0, trimmed.IndexOf('\n'));

            item.MoveToContainer(player.inventory.containerMain);
            player.SendConsoleCommand($"note.inv {item.info.itemid} 1 \"{name}\"");
            SendReply(player, "Рюкзак был успешно выдан!");
        }

        private void chatCmdBackpackOpen(BasePlayer player, string command, string[] args)
        {
            player.SendConsoleCommand(_config.consoleCommand);
        }

        private void chatCmdBackpack(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.DestroyUi(player, LayerBlur);

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = HexToRGB("#202020C2"), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", LayerBlur);

            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "UI_Backpack close", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "Overlay", Layer);

            container.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "santabeard", _config.skinIdBackpack)
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"0.415625 0.521296",
                        AnchorMax = $"0.571875 0.799074"
                    },
                }
            });

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0.446 0.806", AnchorMax = "0.68 0.861" },
                Text = { Text = $"<color=#a100ff>КОЛИЧЕСТВО СЛОТОВ: {_config.backpackSize}</color>", Align = TextAnchor.MiddleLeft, Color = HexToRGB("#FFFFFFDA"), Font = "RobotoCondensed-Regular.ttf", FontSize = 14 }
            }, Layer);

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0.378124 0.938889", AnchorMax = "0.633542 0.99537" },
                Text = { Text = $"<b>КРАФТ РЮКЗАКА</b>", Align = TextAnchor.MiddleCenter, Color = HexToRGB("#a100ff"), Font = "RobotoCondensed-Bold.ttf", FontSize = 24 }
            }, Layer);

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0.388 0.401", AnchorMax = "0.622 0.513" },
                Text = { Text = $"НЕОБХОДИМЫЕ ПРЕДМЕТЫ \nДЛЯ КРАФТА", Align = TextAnchor.MiddleCenter, Color = HexToRGB("#a100ff"), Font = "RobotoCondensed-Regular.ttf", FontSize = 22 }
            }, Layer);

            float itemMinPosition = 515f;
            float itemWidth = 0.403646f - 0.351563f;
            float itemMargin = 0.409895f - 0.403646f;
            int itemCount = _config.items.Count;
            float itemMinHeight = 0.315741f;
            float itemHeight = 0.408333f - 0.315741f;

            if (itemCount > 5)
            {
                itemMinPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                itemCount -= 5;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            var countItem = 0;

            foreach (var itemCraft in _config.items)
            {
                countItem++;

                container.Add(new CuiElement()
                {
                    Parent = Layer,
                    Name = Layer + $".Item{itemCraft.itemID}",
                    Components =
                    {
                        new CuiRawImageComponent()
                        {
                            Png = (string) ImageLibrary.Call("GetImage", itemCraft.shortname, itemCraft.skinID),
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = $"{itemMinPosition} {itemMinHeight}",
                            AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}"
                        },
                    }
                });

                container.Add(new CuiLabel()
                {
                    RectTransform = { AnchorMin = "0 0.05", AnchorMax = "0.98 1" },
                    Text = { Text = $"x{itemCraft.amount}", Align = TextAnchor.LowerRight, FontSize = 12 },
                }, Layer + $".Item{itemCraft.itemID}");

                itemMinPosition += (itemWidth + itemMargin);

                if (countItem % 5 == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));

                    if (itemCount > 5)
                    {
                        itemMinPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                        itemCount -= 5;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
            }

            itemMinHeight -= ((itemMargin * 3f) + (0.162037f - 0.0925926f));

            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = $"0.389062 {itemMinHeight}", AnchorMax = $"0.615103 {itemMinHeight + (0.162037f - 0.0925926f)}" },
                Button = { Color = "0.00 0.50 0.00 1.00", Command = $"UI_Backpack craft", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = "СКРАФТИТЬ", Font = "RobotoCondensed-Regular.ttf", FontSize = 28, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, Layer);
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0.765 0.943", AnchorMax = "0.866 0.985" },
                Button = { Color = "1.00 0.56 0.23 1.00", Command = $"UI_Backpack menuha", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = "В главное меню", Font = "RobotoCondensed-Regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.63 0.00 1.00 1.00" }
            }, Layer);
            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0.875 0.943", AnchorMax = "0.977 0.9856" },
                Button = { Color = "1.00 0.56 0.23 1.00", Command = $"UI_Backpack close", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = "Закрыть", Font = "RobotoCondensed-Regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.63 0.00 1.00 1.00" }
            }, Layer);

            CuiHelper.AddUi(player, container);
        }

        private void consoleCmdBackpack(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;

            if (player == null) return;

            if (player.inventory.loot?.entitySource != null)
            {
                BackpackStorage backpackStorage;
                if (opensBackpack.TryGetValue(player.userID, out backpackStorage) &&
                    backpackStorage.gameObject == player.inventory.loot.entitySource.gameObject) return;

                player.EndLooting();

                timer.Once(0.1f, () => BackpackOpen(player));
            }
            else BackpackOpen(player);
        }

        #endregion

        #region Hooks

        private bool? CanWearItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;

            if (player == null) return null;
            if (player.IsNpc) return null;

            if (item.skin == _config.skinIdBackpack)
            {
                if (player.inventory.containerWear.itemList.Find(x => x.skin == _config.skinIdBackpack) != null)
                    return false;
                if (player.inventory.containerWear.itemList.Find(x => x.info.shortname == "santabeard") != null)
                    return true;
            }
            return null;
        }

        void OnServerInitialized()
        {
            LoadData();
            LoadConfig();

            CheckConfig();
            UpdateData();

            cmd.AddConsoleCommand(_config.consoleCommand, this, "consoleCmdBackpack");
            cmd.AddChatCommand(_config.chatCommand, this, chatCmdBackpack);
            cmd.AddChatCommand(_config.chatCommandOpen, this, chatCmdBackpackOpen);

            ins = this;
        }

        void UpdateData()
        {
            SaveData();
            timer.Once(300f, () => UpdateData());
        }

        object CanAcceptItem(ItemContainer container, Item item)
        {
            if (item.IsLocked())
                return ItemContainer.CanAcceptResult.CannotAccept;

            return null;
        }

        void Unload()
        {
            foreach (var backpack in opensBackpack)
                backpack.Value.Close();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, LayerBlur);
            }

            SaveData();
        }

        #endregion

        #region Methods

        public Item FindItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            Item item = null;

            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null && player.inventory.FindItemID(itemID).amount >= amount)
                    return player.inventory.FindItemID(itemID);
            }
            else
            {

                List<Item> items = new List<Item>();

                items.AddRange(player.inventory.FindItemIDs(itemID));

                foreach (var findItem in items)
                {
                    if (findItem.skin == skinID && findItem.amount >= amount)
                    {
                        return findItem;
                    }
                }
            }

            return item;
        }

        public bool HaveItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null &&
                    player.inventory.FindItemID(itemID).amount >= amount) return true;
                return false;
            }

            List<Item> items = new List<Item>();

            items.AddRange(player.inventory.FindItemIDs(itemID));

            foreach (var item in items)
            {
                if (item.skin == skinID && item.amount >= amount)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator DownloadImages()
        {
            ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/skin/getImage/{_config.skinIdBackpack}",
                "santabeard", _config.skinIdBackpack);
            foreach (var item in _config.items)
            {
                if (item.skinID != 0U)
                    ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/skin/getImage/{item.skinID}",
                        item.shortname, item.skinID);
                yield return new WaitForSeconds(0.04f);
            }
            yield return 0;
        }

        private static string HexToRGB(string hex)
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

        List<SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();

        bool BackpackHide(uint itemID, ulong playerId)
        {
            BackpackStorage backpackStorage;
            if (!opensBackpack.TryGetValue(playerId, out backpackStorage)) return false;
            opensBackpack.Remove(playerId);
            if (backpackStorage == null) return false;
            var items = SaveItems(backpackStorage.GetItems);
            if (items.Count > 0) storedData.backpacks[itemID] = items;
            else storedData.backpacks.Remove(itemID);

            backpackStorage.Close();

            return true;
        }

        void BackpackOpen(BasePlayer player)
        {
            if (player.inventory.loot?.entitySource != null) return;

            Item backpack = null;

            foreach (var item in player.inventory.containerWear.itemList)
            {

                if (item.skin == _config.skinIdBackpack)
                {
                    backpack = item;
                    break;
                }
            }

            if (backpack == null)
            {
                SendReply(player, "Для того, чтобы воспользоваться рюкзаком, необходимо одеть его в слоты одежды!");
                return;
            }

            if (Interface.Oxide.CallHook("CanBackpackOpen", player) != null) return;

            timer.Once(0.1f, () =>
            {
                if (!player.IsOnGround())
                {
                    SendReply(player, "Сначала приземлитесь, а потом пробуйте открыть рюкзак!!");
                    return;
                }

                List<SavedItem> savedItems;
                List<Item> items = new List<Item>();
                if (storedData.backpacks.TryGetValue(backpack.uid, out savedItems))
                    items = RestoreItems(savedItems);
                BackpackStorage backpackStorage = BackpackStorage.Spawn(player);

                opensBackpack.Add(player.userID, backpackStorage);
                if (items.Count > 0)
                    backpackStorage.Push(items);
                backpackStorage.StartLoot();
            });
        }

        List<Item> RestoreItems(List<SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            }).Where(i => i != null).ToList();
        }

        Item BuildItem(SavedItem sItem)
        {
            if (sItem.amount < 1) sItem.amount = 1;
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);

            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
                item.busyTime = sItem.busyTime;
            }

            if (sItem.name != null)
            {
                item.name = sItem.name;
            }

            if (sItem.OnFire)
            {
                item.SetFlag(global::Item.Flag.OnFire, true);
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower)
                flameThrower.ammo = sItem.flamefuel;
            return item;
        }

        Item BuildWeapon(SavedItem sItem)
        {
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.itemid, 1, sItem.skinid);

            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
            }
            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.ammoamount;
            }

            if (sItem.mods != null)
                foreach (var mod in sItem.mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }

        SavedItem SaveItem(Item item)
        {
            SavedItem iItem = new SavedItem
            {
                shortname = item.info?.shortname,
                amount = item.amount,
                mods = new List<SavedItem>(),
                skinid = item.skin,
                busyTime = item.busyTime,

            };
            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                iItem.OnFire = true;
            }
            if (item.info == null) return iItem;
            iItem.itemid = item.info.itemid;
            iItem.weapon = false;

            iItem.name = item.name;
            if (item.hasCondition)
            {
                iItem.condition = item.condition;
                iItem.maxcondition = item.maxCondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null)
                iItem.flamefuel = flameThrower.ammo;
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.ammoamount = weapon.primaryMagazine.contents;
            iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.weapon = true;
            if (item.contents != null)
                foreach (var mod in item.contents.itemList)
                    if (mod.info.itemid != 0)
                        iItem.mods.Add(SaveItem(mod));
            return iItem;
        }

        #endregion

        #region Class

        public class BackpackStorage : MonoBehaviour
        {
            public StorageContainer container;
            public Item backpack;
            public BasePlayer player;

            public void Initialization(StorageContainer container, Item backpack, BasePlayer player)
            {
                this.container = container;
                this.backpack = backpack;
                this.player = player;

                container.ItemFilter(backpack, -1);

                BlockBackpackSlots(true);
            }

            public List<Item> GetItems => container.inventory.itemList.Where(i => i != null).ToList();

            public static StorageContainer CreateContainer(BasePlayer player)
            {
                var storage =
                    GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab")
                        as StorageContainer;
                if (storage == null) return null;
                storage.transform.position = new Vector3(0f, 100f, 0);
                storage.panelName = "largewoodbox";
                storage.name = "backpack";
                //storage.SetFlag(BaseEntity.Flags.Reserved1, true); // = "backpack";

                ItemContainer container = new ItemContainer { playerOwner = player };
                container.ServerInitialize((Item)null, ins._config.backpackSize);

                if ((int)container.uid == 0)
                    container.GiveUID();

                storage.inventory = container;
                if (!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
                storage.Spawn();

                return storage;
            }

            private void PlayerStoppedLooting(BasePlayer player)
            {
                BlockBackpackSlots(false);

                ins.BackpackHide(backpack.uid, player.userID);
            }

            public void StartLoot()
            {
                container.SetFlag(BaseEntity.Flags.Open, true, false);
                player.inventory.loot.StartLootingEntity(container, false);
                player.inventory.loot.AddContainer(container.inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", container.panelName);
                container.DecayTouch();
                container.SendNetworkUpdate();
            }

            public static BackpackStorage Spawn(BasePlayer player)
            {
                player.EndLooting();
                var storage = CreateContainer(player);

                Item backpack = null;

                backpack = player.inventory.containerWear.itemList.Find(x => x.skin == ins._config.skinIdBackpack);

                if (backpack == null) return null;

                var box = storage.gameObject.AddComponent<BackpackStorage>();

                box.Initialization(storage, backpack, player);

                return box;
            }

            public void Close()
            {
                container.inventory.itemList.Clear();
                container.Kill();
            }

            public void Push(List<Item> items)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(container.inventory);
            }

            public void BlockBackpackSlots(bool state)
            {
                backpack.LockUnlock(state);

                foreach (var item in player.inventory.AllItems())
                    if (item.skin == ins._config.skinIdBackpack)
                        item.LockUnlock(state);
            }
        }

        #endregion

        #region Data

        class StoredData
        {
            public Dictionary<uint, List<SavedItem>> backpacks = new Dictionary<uint, List<SavedItem>>();
        }

        public class SavedItem
        {
            public string shortname;
            public int itemid;
            public float condition;
            public float maxcondition;
            public int amount;
            public int ammoamount;
            public string ammotype;
            public int flamefuel;
            public ulong skinid;
            public string name;
            public bool weapon;
            public float busyTime;
            public bool OnFire;
            public List<SavedItem> mods;
        }

        void SaveData()
        {
            BackpackData.WriteObject(storedData);
        }

        void LoadData()
        {
            BackpackData = Interface.Oxide.DataFileSystem.GetFile(_config.fileName);
            try
            {
                storedData =
                    Interface.Oxide.DataFileSystem.ReadObject<StoredData>(_config.fileName);
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        StoredData storedData;
        private DynamicConfigFile BackpackData;

        #endregion

        #region Configuration

        public void CheckConfig()
        {
            if (_config.backpackSize > 30)
                _config.backpackSize = 30;

            foreach (var item in _config.items)
            {
                if (item.itemID != 0) continue;

                var itemDef = ItemManager.FindItemDefinition(item.shortname);
                item.itemID = itemDef.itemid;
            }
            ServerMgr.Instance.StartCoroutine(DownloadImages());
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration()
            {
                backpackSize = 12,
                fileName = "Backpack/backpack",
                skinIdBackpack = 1720637353U,
                consoleCommand = "backpack.open",
                chatCommandOpen = "bp",
                chatCommand = "backpack",
                displayName = "Рюкзак\n  <size=16>████████████████████</size>\n<size=12>   Рюкзак, позволяет переносить в себе предметы. \n   Количество слотов: 12</size>",
                items = new List<ItemInfo>()
                {
                    new ItemInfo()
                    {
                        shortname = "cloth",
                        amount = 300,
                    },
                    new ItemInfo()
                    {
                        shortname = "leather",
                        amount = 150,
                    },
                    new ItemInfo()
                    {
                        shortname = "metal.fragments",
                        amount = 1000,
                    },
                    new ItemInfo()
                    {
                        shortname = "metal.refined",
                        amount = 30
                    }
                }
            };
        }

        public Configuration _config;

        public class Configuration
        {
            [JsonProperty("Консольная команда для открытия рюкзака")]
            public string consoleCommand = "";

            [JsonProperty("Чат команда для открытия крафта рюкзака")]
            public string chatCommand = "";

            [JsonProperty("Чат команда для открытия самого рюкзака")]
            public string chatCommandOpen = "bp";

            [JsonProperty("Количество слотов в рюкзаке")]
            public int backpackSize = 0;

            [JsonProperty("Название для рюкзака")]
            public string displayName = "";

            [JsonProperty("СкинИД рюкзака")]
            public ulong skinIdBackpack = 1719575499U;

            [JsonProperty("Расположение Data файла")]
            public string fileName = "Backpack/backpack";

            [JsonProperty("Необходимые предметы для крафта рюкзака")]
            public List<ItemInfo> items = new List<ItemInfo>();
        }

        public class ItemInfo
        {
            [JsonProperty("Шортнейм предмета")] public string shortname = "";
            [JsonProperty("Количество предмета")] public int amount = 0;
            [JsonProperty("СкинИД предмета")] public ulong skinID = 0U;
            [JsonProperty("АйтемИД предмета")] public int itemID = 0;
        }

        #endregion
    }
}