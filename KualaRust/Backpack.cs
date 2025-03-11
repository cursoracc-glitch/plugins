using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Physics = UnityEngine.Physics;
using System.IO;
namespace Oxide.Plugins
{
    [Info("Backpack", "OxideBro", "1.4.6")]
    public class Backpack : RustPlugin
    {
        static Backpack ins;
        object OnEntityGroundMissing(BaseEntity entity)
        {
            var container = entity as StorageContainer;
            if (container != null)
            {
                var opened = openedBackpacks.Values.Select(x => x.storage);
                if (opened.Contains(container)) return false;
            }
            return null;
        }


        public class BackpackBox : MonoBehaviour
        {
            public StorageContainer storage;
            BasePlayer owner;
            public void Init(StorageContainer storage, BasePlayer owner)
            {
                this.storage = storage;
                this.owner = owner;
            }
            public static BackpackBox Spawn(BasePlayer player, ulong ownerid, int size = 1)
            {
                player.EndLooting();
                var storage = SpawnContainer(player, size, false, ownerid);
                var box = storage.gameObject.AddComponent<BackpackBox>();
                box.Init(storage, player);
                return box;
            }
            static int rayColl = LayerMask.GetMask("Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building");
            public static StorageContainer SpawnContainer(BasePlayer player, int size, bool die, ulong ownerid)
            {
                var pos = player.transform.position;
                if (die)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(player.GetCenter(), Vector3.down), out hit, 1000, rayColl, QueryTriggerInteraction.Ignore))
                    {
                        pos = hit.point;
                    }
                }
                else
                {
                    pos -= new Vector3(0, 100, 0);
                }
                return SpawnContainer(player, size, pos, ownerid);
            }

            private static StorageContainer SpawnContainer(BasePlayer player, int size, Vector3 position, ulong ownerid, ulong playerid = 533504)
            {
                var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab") as StorageContainer;
                if (storage == null) return null;
                storage.transform.position = position;
                storage.panelName = "genericlarge";
                ItemContainer container = new ItemContainer();
                container.ServerInitialize(null, !ownerid.IsSteamId() ? ins.GetBackpackSize(player.UserIDString) : ins.GetBackpackSize(ownerid.ToString()));
                if ((int)container.uid == 0) container.GiveUID();
                storage.inventory = container;
                if (!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
                storage.OwnerID = player.userID;
                storage.Spawn();
                return storage;
            }

            private void PlayerStoppedLooting(BasePlayer player)
            {
                Interface.Oxide.RootPluginManager.GetPlugin("Backpack").Call("BackpackHide", player.userID);
            }
            public void Close()
            {
                ClearItems();
                storage.Kill();
            }
            public void StartLoot()
            {
                storage.SetFlag(BaseEntity.Flags.Open, true, false);
                owner.inventory.loot.StartLootingEntity(storage, false);
                owner.inventory.loot.AddContainer(storage.inventory);
                owner.inventory.loot.SendImmediate();
                owner.ClientRPCPlayer(null, owner, "RPC_OpenLootPanel", storage.panelName);
                storage.DecayTouch();
                storage.SendNetworkUpdate();
            }
            public void Push(List<Item> items)
            {
                for (int i = items.Count - 1;
                i >= 0;
                i--) items[i].MoveToContainer(storage.inventory);
            }
            public void ClearItems()
            {
                storage.inventory.itemList.Clear();
            }
            public List<Item> GetItems => storage.inventory.itemList.Where(i => i != null).ToList();
        }
        public Dictionary<ulong, BackpackBox> openedBackpacks = new Dictionary<ulong, BackpackBox>();
        public Dictionary<ulong, List<SavedItem>> savedBackpacks;
        public Dictionary<ulong, BaseEntity> visualBackpacks = new Dictionary<ulong, BaseEntity>();
        public Color GetBPColor(int count, int max)
        {
            float n = max > 0 ? (float)clrs.Length / max : 0;
            var index = (int)(count * n);
            if (index > 0) index--;
            return hexToColor(clrs[index]);
        }
        public static Color hexToColor(string hex)
        {
            hex = hex.Replace("0x", "");
            hex = hex.Replace("#", "");
            byte a = 160;
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r, g, b, a);
        }
        private string[] clrs = {
            "#ffffff", "#fffbf5", "#fff8ea", "#fff4e0", "#fff0d5", "#ffedcb", "#ffe9c1", "#ffe5b6", "#ffe2ac", "#ffdea1", "#ffda97", "#ffd78d", "#ffd382", "#ffcf78", "#ffcc6d", "#ffc863", "#ffc458", "#ffc14e", "#ffbd44", "#ffb939", "#ffb62f", "#ffb224", "#ffae1a", "#ffab10", "#ffa705", "#ffa200", "#ff9b00", "#ff9400", "#ff8d00", "#ff8700", "#ff8000", "#ff7900", "#ff7200", "#ff6c00", "#ff6500", "#ff5e00", "#ff5800", "#ff5100", "#ff4a00", "#ff4300", "#ff3d00", "#ff3600", "#ff2f00", "#ff2800", "#ff2200", "#ff1b00", "#ff1400", "#ff0d00", "#ff0700", "#ff0000"
        }
        ;
        DynamicConfigFile backpacksFile = Interface.Oxide.DataFileSystem.GetFile("Backpack_Data");
        void LoadBackpacks()
        {
            try
            {
                savedBackpacks = backpacksFile.ReadObject<Dictionary<ulong, List<SavedItem>>>();
            }
            catch (Exception)
            {
                savedBackpacks = new Dictionary<ulong, List<SavedItem>>();
            }
        }
        void OnServerSave()
        {
            SaveBackpacks();
        }
        void SaveBackpacks() => backpacksFile.WriteObject(savedBackpacks);
        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item == null || playerLoot == null) return null;
            var player = playerLoot.GetComponent<BasePlayer>();
            if (player == null) return null;
            if (openedBackpacks.ContainsKey(player.userID))
            {
                var target = playerLoot.FindContainer(targetContainer)?.GetSlot(targetSlot);
                if (target != null && targetContainer != item.GetRootContainer().uid)
                {
                    if (!PermissionService.HasPermission(player.UserIDString, BPIgnoreBlackListed)) if (BlackListed.Contains(target.info.shortname))
                        {
                            SendReply(player, $"<color=#AF5085>[Backpack]:</color> Извините но данный предмет <color=#AF5085>{target.info.displayName.english}</color> находиться в <color=RED>черном списке</color>. Его нельзя перенести в рюкзак.\nРазблокировка доступна для VIP игроков");
                            return false;
                        }
                }
                if (openedBackpacks[player.userID].storage.inventory.uid == targetContainer)
                {
                    if (!PermissionService.HasPermission(player.UserIDString, BPIgnoreBlackListed)) if (BlackListed.Contains(item.info.shortname))
                        {
                            SendReply(player, $"<color=#AF5085>[Backpack]:</color> Извините но данный предмет <color=#AF5085>{item.info.displayName.english}</color> находиться в <color=RED>черном списке</color>. Его нельзя перенести в рюкзак.\nРазблокировка доступна для VIP игроков");
                            return false;
                        }
                }
                if (DisabledMoveOtherBackpack && openedBackpacks[player.userID].storage.OwnerID != player.userID && openedBackpacks[player.userID].storage.inventory.uid == targetContainer)
                {
                    SendReply(player, $"<color=#AF5085>[Backpack]:</color> Запрещено переносить предметы в чужой рюкзак");
                    return false;
                }
            }
            return null;
        }
        bool IsBackpackContainer(uint uid, ulong userId) => openedBackpacks.ContainsKey(userId) ? true : false;
        void OnEntityDeath(BaseCombatEntity ent, HitInfo info)
        {
            if (!(ent is BasePlayer)) return;
            var player = (BasePlayer)ent;
            if (InDuel(player) || InEvent(player)) return;
            BackpackHide(player.userID);
            if (PermissionService.HasPermission(player.UserIDString, BPIGNORE)) return;
            List<SavedItem> savedItems;
            List<Item> items = new List<Item>();
            if (savedBackpacks.TryGetValue(player.userID, out savedItems))
            {
                items = RestoreItems(savedItems);
                savedBackpacks.Remove(player.userID);
            }
            if (items.Count <= 0) return;
            if (DropWithoutBackpack)
            {
                foreach (var item in items)
                {
                    item.Drop(player.transform.position + Vector3.up, Vector3.up);
                }
                return;
            }
            var iContainer = new ItemContainer();
            iContainer.ServerInitialize(null, items.Count);
            iContainer.GiveUID();
            iContainer.entityOwner = player;
            iContainer.SetFlag(ItemContainer.Flag.NoItemInput, true);
            for (int i = items.Count - 1;
            i >= 0;
            i--) items[i].MoveToContainer(iContainer);
            DroppedItemContainer droppedItemContainer = ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop_backpack.prefab", player.transform.position + Vector3.up, Quaternion.identity, iContainer);
            if (droppedItemContainer != null)
            {
                droppedItemContainer.playerName = $"Рюкзак игрока <color=#FF8080>{player.displayName}</color>";
                droppedItemContainer.playerSteamID = player.userID;
                timer.Once(KillTimeout, () =>
                {
                    if (droppedItemContainer != null && !droppedItemContainer.IsDestroyed) droppedItemContainer.Kill();
                }
                );
                Effect.server.Run("assets/bundled/prefabs/fx/dig_effect.prefab", droppedItemContainer.transform.position);
            }
        }
        string BPIGNORE = "backpack.ignore";
        string BPIgnoreBlackListed = "backpack.blignore";
        string BPPrivilageMainLoot = "backpack.otherloot";
        bool DropWithoutBackpack = false;
        bool EnabledMainBackpackLoot = true;
        float KillTimeout = 300f;
        string ImageURL = "https://i.imgur.com/afIPQeW.png";
        static Dictionary<string, int> permisions = new Dictionary<string, int>();
        List<string> BlackListed = new List<string>();
        private string TextInButton = "<b>ОТКРЫТЬ</b>";
        private bool SizeEnabled = true;
        private bool AutoWipe = false;
        private bool DisabledMoveOtherBackpack = true;
        private bool EnabledUI = true;
        private bool DisabledOpenBPInFly = true;
        private int Type = 1;
        private void LoadConfigValues()
        {
            bool changed = false;
            if (GetConfig("Основные настройки", "При смерти игрока выкидывать вещи без рюкзака", ref DropWithoutBackpack))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Время удаления рюкзака после выпадения", ref KillTimeout))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Привилегия игнорирования выпадение рюкзака", ref BPIGNORE))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Привилегия игнорирования чёрного списка", ref BPIgnoreBlackListed))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Запретить открывать рюкзак в полёте", ref DisabledOpenBPInFly))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Запретить открывать рюкзак в полёте");
                changed = true;
            }
            if (GetConfig("Основные настройки", "ССылка на изображение иконки UI", ref ImageURL))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Ссылка на изображение иконки UI");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Текст в кнопке UI (Если не хотите надпись, оставте поле пустым)", ref TextInButton))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Текст в кнопке UI");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Включить отображение размера рюкзака в UI", ref SizeEnabled))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Включить отображение размера рюкзака в UI");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Включить отображение UI рюкзака", ref EnabledUI))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Включить отображение UI рюкзака");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Включить автоматическую очистку рюкзаков при вайпе карты", ref AutoWipe))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Включить автоматическую очистку рюкзаков при вайпе карты");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Разрешить лутание чужих рюкзаков по привилегии указаной в конфигурации", ref EnabledMainBackpackLoot))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Разрешить лутание чужих рюкзаков по привилегии указаной в конфигурации");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Привилегия на разрешение лутания чужих рюкзаков", ref BPPrivilageMainLoot))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Привилегия на разрешение лутания чужих рюкзаков");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Запретить переносить свои предметы в чужой рюкзак (если включена функция лутания чужих рюкзаков)", ref DisabledMoveOtherBackpack))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Запретить переносить свои предметы в чужой рюкзак (если включена функция лутания чужих рюкзаков)");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Разрешить лутание чужих рюкзаков по привилегии указаной в конфигурации", ref EnabledMainBackpackLoot))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Разрешить лутание чужих рюкзаков по привилегии указаной в конфигурации");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Какой вид заполнения использовать в UI (Примеры есть в описании плагина на сайте RustPlugin.ru)", ref Type))
            {
                PrintWarning("Конфигурация обновлена! Добавлен пункт: Какой вид заполнения использовать в UI (Примеры есть в описании плагина на сайте RustPlugin.ru)");
                changed = true;
            }
            var _permisions = new Dictionary<string, object>() {
                    {
                    "backpack.size1", 1
                }
                , {
                    "backpack.size6", 6
                }
                , {
                    "backpack.size15", 15
                }
                , {
                    "backpack.size30", 30
                }
                , {
                    "backpack.size40", 40
                }
                ,
            }
            ;
            if (GetConfig("Основные настройки", "Список привилегий и размера рюкзака (Привилегия (формат: backpack.): Размер слотов)", ref _permisions))
            {
                PrintWarning("Новые привилегии загружены.");
                changed = true;
            }
            permisions = _permisions.ToDictionary(p => p.Key.ToString(), p => Convert.ToInt32(p.Value));
            var _BlackListed = new List<object>() {
                    {
                    "ammo.rocket.basic"
                }
                , {
                    "ammo.rifle.explosive"
                }
            }
            ;
            if (GetConfig("Основные настройки", "Список запрещенных вещей какие нельзя носить в рюкзаке", ref _BlackListed))
            {
                PrintWarning("Чёрный список предметов обновлен и загружен!");
                changed = true;
            }
            BlackListed = _BlackListed.Select(p => p.ToString()).ToList();
            if (changed) SaveConfig();
        }
        private bool GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
                return false;
            }
            Config[MainMenu, Key] = var;
            return true;
        }
        void OnNewSave()
        {
            if (AutoWipe)
            {
                LoadBackpacks();
                savedBackpacks = new Dictionary<ulong, List<SavedItem>>();
                SaveBackpacks();
                PrintWarning("Wipe detected! Player backpacks clear!");
            }
        }
        void Loaded()
        {
            ins = this;
            LoadBackpacks();
        }
        private bool loaded = false;
        void OnServerInitialized()
        {
            LoadConfig();
            LoadConfigValues();
            InitFileManager();
            ServerMgr.Instance.StartCoroutine(m_FileManager.LoadFile("backpackImage", ImageURL));
            PermissionService.RegisterPermissions(this, permisions.Keys.ToList());
            PermissionService.RegisterPermissions(this, new List<string>() {
                BPIGNORE, BPIgnoreBlackListed, BPPrivilageMainLoot
            }
            );
            BasePlayer.activePlayerList.ToList().ForEach(OnPLayerConnection);
        }
        void OnPLayerConnection(BasePlayer player)
        {
            if (!EnabledUI) return;
            DrawUI(player);
        }
        void Unload()
        {
            var keys = openedBackpacks.Keys.ToList();
            for (int i = openedBackpacks.Count - 1;
            i >= 0;
            i--) BackpackHide(keys[i]);
            SaveBackpacks();
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);
            UnityEngine.Object.Destroy(FileManagerObject);
        }
        void OnPreServerRestart()
        {
            foreach (var dt in Resources.FindObjectsOfTypeAll<StashContainer>()) dt.Kill();
            foreach (var ent in Resources.FindObjectsOfTypeAll<TimedExplosive>().Where(ent => ent.name == "backpack")) ent.KillMessage();
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            if (OpenOtherBackpack.ContainsKey(player.userID))
                OpenOtherBackpack[player.userID].EndLooting();
            if (!EnabledUI) return;
            DrawUI(player);

        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            var target = entity.GetComponent<BasePlayer>();
            if (target != null) ShowUIPlayer(player, target);
        }



        void BackpackShow(BasePlayer player, ulong target = 0)
        {
            if (InDuel(player) || InEvent(player)) return;
            if (BackpackHide(player.userID)) return;
            var canBackpack = Interface.Call("CanBackpack", player);
            if (canBackpack != null)
                return;
            var reply = 521;
            if (reply == 0) { }
            if (player.inventory.loot?.entitySource != null) player.EndLooting();
            var backpackSize = GetBackpackSize(player.UserIDString);
            if (backpackSize == 0) return;
            timer.Once(0.1f, () =>
            {
                if (DisabledOpenBPInFly && !player.IsOnGround()) return;
                List<SavedItem> savedItems;
                List<Item> items = new List<Item>();
                if (target != 0 && savedBackpacks.TryGetValue(target, out savedItems)) items = RestoreItems(savedItems);
                if (target == 0 && savedBackpacks.TryGetValue(player.userID, out savedItems)) items = RestoreItems(savedItems);
                BackpackBox box = BackpackBox.Spawn(player, target, backpackSize);
                openedBackpacks.Add(player.userID, box);
                box.storage.OwnerID = target != 0 ? target : player.userID;
                if (box.GetComponent<StorageContainer>() != null)
                {
                    box.GetComponent<StorageContainer>().OwnerID = target != 0 ? target : player.userID;
                    box.GetComponent<StorageContainer>().SendNetworkUpdate();
                }
                if (items.Count > 0) box.Push(items);
                box.StartLoot();
            }
            );
        }

        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            var player = inventory.GetComponent<BasePlayer>();
            if (player != null) CuiHelper.DestroyUi(player, "backpack_playermain");
        }

        void ShowUIPlayer(BasePlayer player, BasePlayer target)
        {
            CuiHelper.DestroyUi(player, "backpack_playermain");
            if (EnabledMainBackpackLoot && !permission.UserHasPermission(player.UserIDString, BPPrivilageMainLoot)) return;
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.5 0", AnchorMax="0.5 0", OffsetMin="215 18", OffsetMax="430 60"
                }
                ,
                Button = {
                    Color="1 1 1 0.03", Command=savedBackpacks.ContainsKey(target.userID) ? $"backpack_mainopen {target.userID}": "", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text="", Align=TextAnchor.MiddleCenter, Font="robotocondensed-regular.ttf", FontSize=24
                }
                ,
            }
            , "Overlay", "backpack_playermain");
            container.Add(new CuiElement
            {
                Parent = "backpack_playermain",
                Components = {
                    new CuiRawImageComponent {
                        Png=m_FileManager.GetPng("backpackImage"), Color="0.91 0.87 0.84 1.00"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.005 0.1", AnchorMax="0.18 0.9"
                    }
                    ,
                }
                ,
            }
            );
            container.Add(new CuiElement
            {
                Parent = "backpack_playermain",
                Components = {
                    new CuiTextComponent {
                        Color=savedBackpacks.ContainsKey(target.userID) ? "0.91 0.87 0.84 1.00": "1.00 0.37 0.38 1.00", Text=savedBackpacks.ContainsKey(target.userID) ? $"ОТКРЫТЬ РЮКЗАК ИГРОКА": "РЮКЗАК ИГРОКА ПУСТ", FontSize=14, Align=TextAnchor.MiddleCenter,
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0.14 0", AnchorMax=$"1 1"
                    }
                    ,
                }
                ,
            }
            );
            CuiHelper.AddUi(player, container);
        }

        public Dictionary<ulong, BasePlayer> OpenOtherBackpack = new Dictionary<ulong, BasePlayer>();

        [ConsoleCommand("backpack_mainopen")]
        private void ConsoleOpenMainBackpack(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            ulong targetID = ulong.Parse(args.Args[0]);
            if (EnabledMainBackpackLoot && !permission.UserHasPermission(player.UserIDString, BPPrivilageMainLoot)) return;

            if (OpenOtherBackpack.ContainsKey(targetID))
                if (OpenOtherBackpack[targetID] != player)
                {
                    SendReply(player, $"<color=#AF5085>[Backpack]:</color> Рюкзак уже кто то лутает");
                    return;
                }

            if (!OpenOtherBackpack.ContainsKey(targetID))
                OpenOtherBackpack.Add(targetID, player);
            else
                OpenOtherBackpack[targetID] = player;

            BackpackShow(player, targetID);
        }
        int GetBackpackSize(string UserID)
        {
            int size = 0;
            permisions.ToList().ForEach(p =>
            {
                if (PermissionService.HasPermission(UserID, p.Key)) if (p.Value > size) size = p.Value;
            }
            );
            return size;
        }

        [HookMethod("BackpackHide")]
        bool BackpackHide(ulong userId)
        {
            BackpackBox box;
            if (!openedBackpacks.TryGetValue(userId, out box)) return false;
            openedBackpacks.Remove(userId);
            if (box == null) return false;
            var items = SaveItems(box.GetItems);
            var owner = box.GetComponent<StorageContainer>();
            if (OpenOtherBackpack.ContainsKey(owner.OwnerID))
                OpenOtherBackpack.Remove(owner.OwnerID);
            if (items.Count > 0) savedBackpacks[owner.OwnerID] = SaveItems(box.GetItems);
            else savedBackpacks.Remove(owner.OwnerID);
            box.Close();
            var otherPlayer = BasePlayer.FindByID(owner.OwnerID);

            if (otherPlayer != null) DrawUI(otherPlayer);
            else
                DrawUI(BasePlayer.FindByID(userId));
            return true;

        }
        void OnUserPermissionGranted(string id, string permName)
        {
            if (permisions.ContainsKey(permName))
            {
                var player = BasePlayer.Find(id);
                if (player != null) DrawUI(player);
            }
        }
        void OnUserPermissionRevoked(string id, string permName)
        {
            if (permisions.ContainsKey(permName))
            {
                var player = BasePlayer.Find(id);
                if (player != null) DrawUI(player);
            }
        }
        void DrawUI(BasePlayer player)
        {
            if (!EnabledUI) return;
            if (!m_FileManager.IsFinished)
            {
                timer.Once(1f, () => DrawUI(player));
                return;
            }
            CuiHelper.DestroyUi(player, "backpack.image");
            List<SavedItem> savedItems;
            if (!savedBackpacks.TryGetValue(player.userID, out savedItems)) savedItems = new List<SavedItem>();
            var bpSize = GetBackpackSize(player.UserIDString);
            if (bpSize == 0) return;
            int backpackCount = savedItems?.Count ?? 0;
            if (backpackCount > bpSize) backpackCount = bpSize;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {
                    Color="1 1 1 0.03", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                RectTransform = {
                    AnchorMin="0.296 0.025", AnchorMax="0.295 0.025", OffsetMax="60 60"
                }
                ,
            }
            , "Overlay", "backpack.image");
            var AnchorType = (float)backpackCount / bpSize - 0.03f;
            string AcnhorMax = "1 1";
            string alpha = "1";
            switch (Type)
            {
                case 1:
                    AnchorType = (float)Math.Min(backpackCount, bpSize) / bpSize - 0.03f;
                    AcnhorMax = $"0.05 {AnchorType}";
                    break;
                case 2:
                    AnchorType = (float)backpackCount / bpSize - 0.03f;
                    AcnhorMax = $"1 {AnchorType}";
                    alpha = "0.5";
                    break;
                case 3:
                    AnchorType = (float)backpackCount / bpSize - 0.03f;
                    AcnhorMax = $"{AnchorType} 1";
                    alpha = "0.5";
                    break;
                default:
                    AnchorType = (float)Math.Min(backpackCount, bpSize) + bpSize - 0.03f;
                    AcnhorMax = $"0.05 {AnchorType}";
                    break;
            }
            container.Add(new CuiPanel
            {
                Image = {
                    Color=SetColor(GetBPColor(backpackCount, bpSize), alpha), Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                RectTransform = {
                    AnchorMin="0 0", AnchorMax=$"{AcnhorMax}"
                }
                ,
            }
            , "backpack.image");
            container.Add(new CuiElement
            {
                Parent = "backpack.image",
                Components = {
                    new CuiRawImageComponent {
                        Png=m_FileManager.GetPng("backpackImage"), Color="1 1 1 0.5"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.1 0.25", AnchorMax="0.9 0.95"
                    }
                    ,
                }
                ,
            }
            );
            if (!string.IsNullOrEmpty(TextInButton)) container.Add(new CuiElement
            {
                Parent = "backpack.image",
                Components = {
                    new CuiTextComponent {
                        Color="1 1 1 0.5", Text=TextInButton, FontSize=11, Align=TextAnchor.MiddleCenter,
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0 0", AnchorMax=$"1 0.3"
                    }
                    ,
                }
                ,
            }
            );
            if (SizeEnabled) container.Add(new CuiElement
            {
                Parent = "backpack.image",
                Components = {
                    new CuiTextComponent {
                        Color="1 1 1 0.2", Text=$"{backpackCount}/{bpSize}", FontSize=11, Align=TextAnchor.MiddleRight,
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0.5 0.79", AnchorMax=$"0.997 1"
                    }
                    ,
                }
                ,
            }
            );
            container.Add(new CuiElement
            {
                Parent = "backpack.image",
                Components = {
                    new CuiButtonComponent {
                        Color="0 0 0 0", Command="backpack.open"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                    ,
                }
                ,
            }
            );
            CuiHelper.AddUi(player, container);
        }
        string SetColor(Color color, string alpha) => $"{color.r} {color.g} {color.b} {alpha}";
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "backpack.image");
        }

        [ChatCommand("bp")]
        void cmdBackpackShow(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            player.EndLooting();
            NextTick(() => BackpackShow(player));
        }

        [ConsoleCommand("backpack.open")]
        void cmdOnBackPackShowClick(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (player.inventory.loot?.entitySource != null)
            {
                BackpackBox bpBox;
                if (openedBackpacks.TryGetValue(player.userID, out bpBox) && bpBox.gameObject == player.inventory.loot.entitySource.gameObject) return;
                player.EndLooting();
                NextTick(() => BackpackShow(player));
                return;
            }
            else BackpackShow(player);
        }
        public class SavedItem
        {
            public string shortname;
            public string name;
            public int itemid;
            public float condition;
            public float maxcondition;
            public int amount;
            public int ammoamount;
            public string ammotype;
            public int flamefuel;
            public ulong skinid;
            public bool weapon;
            public int blueprint;
            public List<SavedItem> mods;
        }
        List<SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();
        SavedItem SaveItem(Item item)
        {
            SavedItem iItem = new SavedItem
            {
                shortname = item.info?.shortname,
                name = item.name,
                amount = item.amount,
                mods = new List<SavedItem>(),
                skinid = item.skin,
                blueprint = item.blueprintTarget
            }
            ;
            if (item.info == null) return iItem;
            iItem.itemid = item.info.itemid;
            iItem.weapon = false;
            if (item.hasCondition)
            {
                iItem.condition = item.condition;
                iItem.maxcondition = item.maxCondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null) iItem.flamefuel = flameThrower.ammo;
            Chainsaw chainsaw = item.GetHeldEntity()?.GetComponent<Chainsaw>();
            if (chainsaw != null) iItem.flamefuel = chainsaw.ammo;
            if (item.contents != null) foreach (var mod in item.contents.itemList) if (mod.info.itemid != 0) iItem.mods.Add(SaveItem(mod));
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.ammoamount = weapon.primaryMagazine.contents;
            iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.weapon = true;
            return iItem;
        }
        List<Item> RestoreItems(List<SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            }
            ).Where(i => i != null).ToList();
        }
        Item BuildItem(SavedItem sItem)
        {
            if (sItem.amount < 1) sItem.amount = 1;
            Item item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);
            if (!string.IsNullOrEmpty(sItem.name)) item.name = sItem.name;
            item.blueprintTarget = sItem.blueprint;
            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower) flameThrower.ammo = sItem.flamefuel;
            Chainsaw chainsaw = item.GetHeldEntity()?.GetComponent<Chainsaw>();
            if (chainsaw) chainsaw.ammo = sItem.flamefuel;
            if (sItem.mods != null) foreach (var mod in sItem.mods) item.contents.AddItem(BuildItem(mod).info, mod.amount);
            return item;
        }
        Item BuildWeapon(SavedItem sItem)
        {
            Item item = ItemManager.CreateByItemID(sItem.itemid, 1, sItem.skinid);
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
            if (sItem.mods != null) foreach (var mod in sItem.mods) item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }
        [PluginReference] Plugin Duel, OneVSOne;
        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;
        bool InEvent(BasePlayer player) => OneVSOne?.Call<bool>("IsEventPlayer", player) ?? false;
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();
            public static bool HasPermission(string userId, string permissionName)
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(permissionName)) return false;
                if (permission.UserHasPermission(userId, permissionName)) return true;
                return false;
            }
            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");
                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        private GameObject FileManagerObject;
        private FileManager m_FileManager;
        void InitFileManager()
        {
            FileManagerObject = new GameObject("MAP_FileManagerObject⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }
        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;
            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();
            private class FileInfo
            {
                public string Url;
                public string Png;
            }
            public string GetPng(string name) => files[name].Png;
            public IEnumerator LoadFile(string name, string url)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo()
                {
                    Url = url
                }
                ;
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url));
            }
            IEnumerator LoadImageCoroutine(string name, string url)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    using (MemoryStream stream = new MemoryStream())
                    {
                        if (string.IsNullOrEmpty(www.error))
                        {
                            var entityId = CommunityEntity.ServerInstance.net.ID;
                            var crc32 = FileStorage.server.Store(www.bytes, FileStorage.Type.png, entityId).ToString();
                            files[name].Png = crc32;
                        }
                    }
                }
                loaded++;
            }
        }
    }
}