using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Picklock", "TopPlugin.ru", "1.0.2")]
    public class Picklock : RustPlugin
    {
        #region Config
        private PluginConfig _config;

        protected override void LoadDefaultConfig() => _config = PluginConfig.DefaultConfig();

        protected override void LoadConfig() { base.LoadConfig(); _config = Config.ReadObject<PluginConfig>(); Config.WriteObject(_config, true); }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class CrateConfig
        {
            [JsonProperty("Prefab")] public string Prefab;
            [JsonProperty("Минимальное кол-во")] public int MinAmount;
            [JsonProperty("Максимальное кол-во")] public int MaxAmount;
            [JsonProperty("Шанс выпадения предмета [0.0-100.0]")] public float Chance;
        }

        private class PluginConfig
        {
            [JsonProperty("Время открытия замка [sec.]")] public float TimeUnlock;
            [JsonProperty("Вероятность открытия замка [%]")] public float ChanceUnlock;
            [JsonProperty("Расстояние от игрока до замка [m]")] public float DistanceUnlock;
            [JsonProperty("Открывать при помощи отмычки дверной замок? [true/false]")] public bool IsKeyLock;
            [JsonProperty("Открывать при помощи отмычки кодовый замок? [true/false]")] public bool IsCodeLock;
            [JsonProperty("Открывать только замки плагина Raidable Bases? [true/false]")] public bool OnlyRaidableBases;
            [JsonProperty("Открывать дверь после взлома замка? [true/false]")] public bool IsOpenDoor;
            [JsonProperty("Настройка появления отмычек в ящиках")] public List<CrateConfig> Crates;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    TimeUnlock = 10f,
                    ChanceUnlock = 5f,
                    DistanceUnlock = 3f,
                    IsKeyLock = true,
                    IsCodeLock = true,
                    OnlyRaidableBases = false,
                    IsOpenDoor = true,
                    Crates = new List<CrateConfig>
                    {
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            MinAmount = 1,
                            MaxAmount = 2,
                            Chance = 15f
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            MinAmount = 1,
                            MaxAmount = 1,
                            Chance = 10f
                        }
                    }
                };
            }
        }
        #endregion Config

        #region Oxide Hooks
        private static Picklock ins;

        void Init() => ins = this;

        void OnServerInitialized() => LoadDefaultMessages();

        void Unload()
        {
            foreach (ulong steamID in UnlockPlayers)
            {
                BasePlayer player = BasePlayer.FindByID(steamID);
                if (player != null) CuiHelper.DestroyUi(player, "BG_Unlock");
            }
            ins = null;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustReleased(BUTTON.FIRE_THIRD) && UnlockPlayers.Contains(player.userID))
            {
                UnlockPlayers.Remove(player.userID);
                CuiHelper.DestroyUi(player, "BG_Unlock");
            }
            if (input.WasJustPressed(BUTTON.FIRE_THIRD) && player.GetActiveItem() != null && player.GetActiveItem().skin == 2591851360 && !UnlockPlayers.Contains(player.userID))
            {
                BaseEntity target = RaycastAll<BaseEntity>(player.eyes.HeadRay());
                if (target == null) return;
                BaseLock key = (BaseLock)(target.ShortPrefabName.Contains("lock.key") ? target : target.GetSlot(BaseEntity.Slot.Lock));
                if (key == null) return;
                if (_config.OnlyRaidableBases && !isRaidableBases(key)) return;
                if ((_config.IsKeyLock && key is KeyLock) || (_config.IsCodeLock && key is CodeLock))
                {
                    UnlockPlayers.Add(player.userID);
                    Unlock(player, key, _config.TimeUnlock);
                }
            }
        }

        void OnLootSpawn(LootContainer container)
        {
            if (container == null || container.inventory == null) return;
            if (_config.Crates.Any(x => x.Prefab == container.PrefabName))
            {
                CrateConfig config = _config.Crates.Where(x => x.Prefab == container.PrefabName).FirstOrDefault();
                if (UnityEngine.Random.Range(0f, 100f) <= config.Chance)
                {
                    if (container.inventory.itemList.Count == container.inventory.capacity) container.inventory.capacity++;
                    Item picklock = GetPicklock(UnityEngine.Random.Range(config.MinAmount, config.MaxAmount + 1));
                    if (!picklock.MoveToContainer(container.inventory)) picklock.Remove();
                }
            }
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item == null || targetItem == null) return null;
            if (item.info.itemid == targetItem.info.itemid && item.skin != targetItem.skin) return false;
            return null;
        }

        object CanCombineDroppedItem(DroppedItem drItem, DroppedItem anotherDrItem)
        {
            if (drItem == null || anotherDrItem == null) return null;
            if (drItem.item.info.itemid == anotherDrItem.item.info.itemid && drItem.item.skin != anotherDrItem.item.skin) return false;
            return null;
        }

        Item OnItemSplit(Item item, int amount)
        {
            if (item.skin == 2591851360)
            {
                item.amount -= amount;
                Item newItem = ItemManager.CreateByItemID(item.info.itemid, amount, item.skin);
                newItem.name = item.name;
                item.MarkDirty();
                return newItem;
            }
            return null;
        }
        #endregion Oxide Hooks

        #region Helper
        [PluginReference] Plugin RaidableBases;

        List<ulong> UnlockPlayers = new List<ulong>();

        void Unlock(BasePlayer player, BaseLock key, float time)
        {
            if (!UnlockPlayers.Contains(player.userID)) return;
            if (Vector3.Distance(player.transform.position, key.transform.position) > _config.DistanceUnlock || player == null || key == null || player.IsDead() || player.IsWounded() || player.IsSleeping())
            {
                UnlockPlayers.Remove(player.userID);
                CuiHelper.DestroyUi(player, "BG_Unlock");
                return;
            }
            UnlockGUI(player, time);
            if (time <= 0f)
            {
                UnlockPlayers.Remove(player.userID);
                CuiHelper.DestroyUi(player, "BG_Unlock");
                if (UnityEngine.Random.Range(0f, 100f) <= _config.ChanceUnlock)
                {
                    Door door = key.GetParentEntity() as Door;
                    if (door != null && _config.IsOpenDoor) door.SetOpen(true);
                    if (key != null && !key.IsDestroyed) key.Kill();
                }
                else
                {
                    Item picklock = player.GetActiveItem();
                    if (picklock != null)
                    {
                        if (picklock.amount > 1) picklock.amount--;
                        else NextTick(() => picklock.Remove());
                        player.inventory.containerBelt.MarkDirty();
                    }
                    Effect effect = new Effect("assets/bundled/prefabs/fx/item_break.prefab", player, 0, new Vector3(), new Vector3());
                    EffectNetwork.Send(effect, player.Connection);
                }
                return;
            }
            timer.In(0.1f, () => Unlock(player, key, time - 0.1f));
        }

        BaseEntity RaycastAll<T>(Ray ray) where T : BaseEntity
        {
            RaycastHit[] hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            float distance = _config.DistanceUnlock;
            BaseEntity target = null;
            foreach (RaycastHit hit in hits)
            {
                BaseEntity ent = hit.GetEntity();
                if (ent is T && hit.distance < distance)
                {
                    target = ent;
                    break;
                }
            }
            return target;
        }

        Item GetPicklock(int amount)
        {
            Item item = ItemManager.CreateByName("sticks", amount, 2591851360);
            item.name = "Picklock";
            return item;
        }

        private bool isRaidableBases(BaseEntity entity) => (bool)RaidableBases?.Call("EventTerritory", entity.transform.position);
        #endregion Helper

        #region GUI
        void UnlockGUI(BasePlayer player, float time)
        {
            CuiHelper.DestroyUi(player, "BG_Unlock");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0.16 0.16 0.14 0.95", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -5", OffsetMax = "100 5" },
                CursorEnabled = false,
            }, "Hud", "BG_Unlock");

            container.Add(new CuiElement
            {
                Parent = "BG_Unlock",
                Components =
                {
                    new CuiImageComponent { Color = "0.25 0.3 0.15 0.95", Material = "assets/content/ui/uibackgroundblur.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"{1f - time / _config.TimeUnlock} 0.95" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "BG_Unlock",
                Components =
                {
                    new CuiTextComponent() { Color = "0.79 0.75 0.72 0.95", Text = GetMessage("GUI", player.userID, Math.Round(time, 1)), Align = TextAnchor.MiddleCenter, FontSize = 8 },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion GUI

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GUI"] = "{0} SEC. LEFT",
                ["NoTarget"] = "The specified player <color=#ce3f27>was not found</color>!",
                ["GivePicklock"] = "The player <color=#55aaff>{0}</color> <color=#738d43>has been given</color> a <color=#55aaff>Picklock</color> item (<color=#55aaff>{1}</color> pieces)"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GUI"] = "ОСТАЛОСЬ {0} СЕК.",
                ["NoTarget"] = "Указанный игрок <color=#ce3f27>не найден</color>!",
                ["GivePicklock"] = "Игроку <color=#55aaff>{0}</color> <color=#738d43>выдан</color> предмет <color=#55aaff>Picklock</color> (<color=#55aaff>{1}</color> шт.)"
            }, this, "ru");
        }

        string GetMessage(string langKey, ulong UID) => ins.lang.GetMessage(langKey, ins, UID.ToString());

        string GetMessage(string langKey, ulong UID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, UID) : string.Format(GetMessage(langKey, UID), args);
        #endregion Lang

        #region MoveItem
        void MoveItem(BasePlayer player, Item item)
        {
            int spaceCountItem = GetSpaceCountItem(player, item.info.shortname, item.MaxStackable(), item.skin);
            int inventoryItemCount;
            if (spaceCountItem > item.amount) inventoryItemCount = item.amount;
            else inventoryItemCount = spaceCountItem;

            if (inventoryItemCount > 0)
            {
                Item itemInventory = ItemManager.CreateByName(item.info.shortname, inventoryItemCount, item.skin);
                if (item.skin != 0) itemInventory.name = item.name;

                item.amount -= inventoryItemCount;
                MoveInventoryItem(player, itemInventory);
            }

            if (item.amount > 0) MoveOutItem(player, item);
        }

        int GetSpaceCountItem(BasePlayer player, string shortname, int stack, ulong skinID)
        {
            int slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            int taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            int result = (slots - taken) * stack;
            foreach (Item item in player.inventory.AllItems()) if (item.info.shortname == shortname && item.skin == skinID && item.amount < stack) result += stack - item.amount;
            return result;
        }

        void MoveInventoryItem(BasePlayer player, Item item)
        {
            if (item.amount <= item.MaxStackable())
            {
                foreach (Item itemInv in player.inventory.AllItems())
                {
                    if (itemInv.info.shortname == item.info.shortname && itemInv.skin == item.skin && itemInv.amount < itemInv.MaxStackable())
                    {
                        if (itemInv.amount + item.amount <= itemInv.MaxStackable())
                        {
                            itemInv.amount += item.amount;
                            itemInv.MarkDirty();
                            return;
                        }
                        else
                        {
                            item.amount -= itemInv.MaxStackable() - itemInv.amount;
                            itemInv.amount = itemInv.MaxStackable();
                        }
                    }
                }
                if (item.amount > 0) player.inventory.GiveItem(item);
            }
            else
            {
                while (item.amount > item.MaxStackable())
                {
                    Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                    if (item.skin != 0) thisItem.name = item.name;
                    player.inventory.GiveItem(thisItem);
                    item.amount -= item.MaxStackable();
                }
                if (item.amount > 0) player.inventory.GiveItem(item);
            }
        }

        void MoveOutItem(BasePlayer player, Item item)
        {
            if (item.amount <= item.MaxStackable()) item.Drop(player.transform.position, Vector3.up);
            else
            {
                while (item.amount > item.MaxStackable())
                {
                    Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                    if (item.skin != 0) thisItem.name = item.name;
                    thisItem.Drop(player.transform.position, Vector3.up);
                    item.amount -= item.MaxStackable();
                }
                if (item.amount > 0) item.Drop(player.transform.position, Vector3.up);
            }
        }
        #endregion MoveItem

        #region Commands
        [ConsoleCommand("givepicklock")]
        void ConsoleGivePicklock(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length != 2) return;
            BasePlayer player = arg.Player();
            BasePlayer target = BasePlayer.FindByID(Convert.ToUInt64(arg.Args[0]));
            int amount = Convert.ToInt32(arg.Args[1]);
            if (amount <= 0) return;
            if (player == null)
            {
                if (target == null)
                {
                    Puts("The specified player was not found!");
                    return;
                }
                MoveItem(target, GetPicklock(amount));
                Puts($"The player {target.displayName} has been given a Picklock item ({amount} pieces)");
            }
            else
            {
                if (player.IsAdmin)
                {
                    if (target == null)
                    {
                        PrintToChat(player, GetMessage("NoTarget", player.userID));
                        return;
                    }
                    MoveItem(target, GetPicklock(amount));
                    PrintToChat(player, GetMessage("GivePicklock", player.userID, target.displayName, amount));
                }
            }
        }
        #endregion Commands
    }
}