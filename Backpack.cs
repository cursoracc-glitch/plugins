using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("Backpack", "RustPlugin.ru / EcoSmile","1.0.2")]
    public class Backpack : RustPlugin
    {
        #region Classes

        public class BackpackBox : MonoBehaviour
        {
            public static readonly List<int> sizes = new List<int>() {6,15,30};

            StorageContainer storage;
            BasePlayer owner;

            public void Init(StorageContainer storage, BasePlayer owner)
            {
                this.storage = storage;
                this.owner = owner;
            }
            
            public static BackpackBox Spawn(BasePlayer player,  int size = 1)
            {
                player.EndLooting();
                var storage = SpawnContainer(player,size,false);
                var box = storage.gameObject.AddComponent<BackpackBox>();
                box.Init(storage, player);
                return box;
            }

            static int rayColl = LayerMask.GetMask("Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building");

            public static StorageContainer SpawnContainer (BasePlayer player, int size, bool die)
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
                return SpawnContainer(player, size, pos);
            }


            private static StorageContainer SpawnContainer(BasePlayer player, int size, Vector3 position)
            {
                var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab") as StorageContainer;
                if(storage == null) return null;
                storage.transform.position = position;
                storage.panelName = "largewoodbox";
                ItemContainer container = new ItemContainer { playerOwner = player };
                container.ServerInitialize((Item)null, sizes[size]);
                if((int)container.uid == 0)
                    container.GiveUID();
                storage.inventory = container;
                if(!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
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
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(storage.inventory);
            }

            public void ClearItems()
            {
                storage.inventory.itemList.Clear();
            }

            public List<Item> GetItems => storage.inventory.itemList.Where(i => i != null).ToList();

        }

        #endregion

        #region VARIABLES
        
        public Dictionary<ulong, BackpackBox> openedBackpacks = new Dictionary<ulong, BackpackBox>();
        public Dictionary<ulong, List<SavedItem>> savedBackpacks;
        public Dictionary<ulong, BaseEntity> visualBackpacks = new Dictionary<ulong, BaseEntity>();

        #endregion

        #region DATA

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

        void SaveBackpacks() => backpacksFile.WriteObject(savedBackpacks);

        #endregion

        #region OXIDE HOOKS
        void OnEntityDeath(BaseCombatEntity ent, HitInfo info)
        {
            if (!(ent is BasePlayer)) return;
            var player = (BasePlayer) ent;
            if (PermissionService.HasPermission(player, BPIGNORE)) return;
            if (InDuel(player)) return;
            BackpackHide(player.userID);
                List<SavedItem> savedItems;
                List<Item> items = new List<Item>();
                if (savedBackpacks.TryGetValue(player.userID, out savedItems))
                {
                    items = RestoreItems(savedItems);
                    savedBackpacks.Remove(player.userID);
                }
                if (items.Count <= 0) return;
                var container = BackpackBox.SpawnContainer(player, GetBackpackSize(player), true);
                if (container == null) return;
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(container.inventory);
                timer.Once(300f, () =>
                {
                    if (container != null && !container.IsDestroyed)
                        container.Kill();
                });
                Effect.server.Run("assets/bundled/prefabs/fx/dig_effect.prefab", container.transform.position);
            
        }
        const string BPIGNORE = "backpack.ignore";
        void Loaded()
        {
            LoadBackpacks();
            PermissionService.RegisterPermissions(this, permisions);
            PermissionService.RegisterPermissions(this, new List<string>() { BPIGNORE });
        }

        void OnServerInitialized()
        {
            InitFileManager();
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        private bool loaded = false;
        IEnumerator LoadImages()
        {
            foreach (var name in images.Keys.ToList())
            {
                yield return m_FileManager.StartCoroutine( m_FileManager.LoadFile( name, images[ name ]) );
                images[ name ] = m_FileManager.GetPng( name );
            }
            loaded = true;
            foreach (var player in BasePlayer.activePlayerList) DrawUI( player );
        }

        private Dictionary<string, string> images = new Dictionary<string, string>()
        {
            ["backpackImg"] = "http://i.imgur.com/dJs7pK3.png"
        };
        void Unload(BasePlayer player)
        {
            var keys = openedBackpacks.Keys.ToList();
            for (int i = openedBackpacks.Count - 1; i >= 0; i--)
                BackpackHide(keys[i]);
            SaveBackpacks();
            DestroyUI(player);
        }

        void OnPreServerRestart()
        {
            foreach (var dt in Resources.FindObjectsOfTypeAll<StashContainer>())
                dt.Kill();
            foreach (var ent in Resources.FindObjectsOfTypeAll<TimedExplosive>().Where(ent => ent.name == "backpack"))
                ent.KillMessage();
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            DrawUI(player);
        }

        void OnPlayerAspectChanged(BasePlayer player)
        {
            DrawUI(player);
        }
        #endregion

        #region FUNCTIONS

        void BackpackShow(BasePlayer player)
        {
            if (BackpackHide(player.userID)) return;

            if (player.inventory.loot?.entitySource != null) return;
            
            timer.Once(0.1f, () =>
            {
                if (!player.IsOnGround())
                {
                    SendReply(player, "Нельзя открывать рюкзар к полёте!");

                    return;
                }
                List<SavedItem> savedItems;
                List<Item> items = new List<Item>();
                if (savedBackpacks.TryGetValue(player.userID, out savedItems))
                    items = RestoreItems(savedItems);
                var backpackSize = GetBackpackSize(player);
                BackpackBox box = BackpackBox.Spawn(player, backpackSize);
                openedBackpacks.Add(player.userID, box);
                if (items.Count > 0)
                    box.Push(items);
                box.StartLoot();
            });
        }

        int GetBackpackSize(BasePlayer player)
        {
            for (int i = permisions.Count-1; i >= 0; i--)
                if (PermissionService.HasPermission(player, permisions[i]) && (i == 0 || PermissionService.HasPermission(player, permisions[i-1])))
                    return i+1;
                else if (i > 0 && PermissionService.HasPermission(player, permisions[i])) return i;
            return 0;
        }
        

        [HookMethod("BackpackHide")]
        bool BackpackHide(ulong playerId)
        {
            BackpackBox box;
            if (!openedBackpacks.TryGetValue(playerId, out box)) return false;
            openedBackpacks.Remove(playerId);
            if (box == null) return false;
            var items = SaveItems(box.GetItems);
            if (items.Count > 0)
            {
                savedBackpacks[playerId] = SaveItems(box.GetItems);
                //SpawnVisual(BasePlayer.FindByID(player));
            }
            else { savedBackpacks.Remove(playerId); //RemoveVisual(BasePlayer.FindByID(player));
            }
            box.Close();
            var player = BasePlayer.FindByID(playerId);
            if (player)
                DrawUI(player);
            return true;
        }


        #endregion

         #region UI
        void DrawUI(BasePlayer player)
        {
            if (!loaded) return;
            var backpackSize = GetBackpackSize( player );

            List<SavedItem> savedItems;
            List<Item> items = new List<Item>();
            if (!savedBackpacks.TryGetValue( player.userID, out savedItems ))
                savedItems = new List<SavedItem>();
            int backpackCount = savedItems?.Count ?? 0;
            CuiHelper.DestroyUi(player, "backpack.btn" );
            CuiHelper.DestroyUi(player, "backpack.text2" );
            CuiHelper.DestroyUi(player, "backpack.text1" );
            CuiHelper.DestroyUi(player, "backpack.image" );
			if (backpackCount < BackpackBox.sizes[backpackSize] * 0.5){
			CuiHelper.DestroyUi(player, "backpack.btn" );
            CuiHelper.DestroyUi(player, "backpack.text2" );
            CuiHelper.DestroyUi(player, "backpack.text1" );
            CuiHelper.DestroyUi(player, "backpack.image" );
            CuiHelper.AddUi(player, GUI0
                .Replace("{0}", backpackCount.ToString())//text1
                .Replace("{1}", BackpackBox.sizes[backpackSize].ToString())//text2
                .Replace("{2}", images["backpackImg"]));
			}
			if (backpackCount >= BackpackBox.sizes[backpackSize] * 0.5){
			CuiHelper.DestroyUi(player, "backpack.btn" );
            CuiHelper.DestroyUi(player, "backpack.text2" );
            CuiHelper.DestroyUi(player, "backpack.text1" );
            CuiHelper.DestroyUi(player, "backpack.image" );
            CuiHelper.AddUi(player, GUI2
                .Replace("{0}", backpackCount.ToString())//text1
                .Replace("{1}", BackpackBox.sizes[backpackSize].ToString())//text2
                .Replace("{2}", images["backpackImg"]));
			}
			if (backpackCount >= BackpackBox.sizes[backpackSize] - BackpackBox.sizes[backpackSize] * 0.3){
			CuiHelper.DestroyUi(player, "backpack.btn" );
            CuiHelper.DestroyUi(player, "backpack.text2" );
            CuiHelper.DestroyUi(player, "backpack.text1" );
            CuiHelper.DestroyUi(player, "backpack.image" );
            CuiHelper.AddUi(player, GUI1
                .Replace("{0}", backpackCount.ToString())//text1
                .Replace("{1}", BackpackBox.sizes[backpackSize].ToString())//text2
                .Replace("{2}", images["backpackImg"]));
			}
					
        }
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "backpack.btn");
            CuiHelper.DestroyUi(player, "backpack.text2");
            CuiHelper.DestroyUi(player, "backpack.text1");
            CuiHelper.DestroyUi(player, "backpack.image");
        }
    #region Gui0

        private string GUI0 = @"
[{
	""name"": ""backpack.image"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.RawImage"", 
		""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",
		""color"": ""1 1 1 1"",
		""png"": ""{2}""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.29112 0.01944441"",
		""anchormax"": ""0.3416667 0.1027779"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text1"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{0}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7058824""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.19 0.19 0.19 1.00"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.220705 0.1333331"",
		""anchormax"": ""0.478305 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text2"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{1}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7061956""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.19 0.19 0.19 1.00"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.513671 0.1333331"",
		""anchormax"": ""0.7712711 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.btn"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.Button"",
		""command"": ""backpack.open"",
		""color"": ""1 1 1 0""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.2937759 0.02592597"",
		""anchormax"": ""0.3383071 0.1013889"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}]";
        #endregion Gui0
	#region Gui1
	private string GUI1 = @"
[{
	""name"": ""backpack.image"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.RawImage"",
		""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",
		""color"": ""1 0 0 0.7"",
		""png"": ""{2}""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.29112 0.01944441"",
		""anchormax"": ""0.3416667 0.1027779"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text1"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{0}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7058824""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.19 0.19 0.19 1.00"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.220705 0.1333331"",
		""anchormax"": ""0.478305 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text2"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{1}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7061956""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.19 0.19 0.19 1.00"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.513671 0.1333331"",
		""anchormax"": ""0.7712711 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.btn"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.Button"",
		""command"": ""backpack.open"",
		""color"": ""1 1 1 0""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.2937759 0.02592597"",
		""anchormax"": ""0.3383071 0.1013889"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}]";
	#endregion Gui1
	#region Gui2
	private string GUI2 = @"
[{
	""name"": ""backpack.image"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.RawImage"",
		""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",
		""color"": ""1 1 0.5 0.7"",
		""png"": ""{2}""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.29112 0.01944441"",
		""anchormax"": ""0.3416667 0.1027779"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text1"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{0}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7058824""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.19 0.19 0.19 0.9"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.220705 0.1333331"",
		""anchormax"": ""0.478305 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text2"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{1}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7061956""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.19 0.19 0.19 0.9"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.513671 0.1333331"",
		""anchormax"": ""0.7712711 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.btn"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.Button"",
		""command"": ""backpack.open"",
		""color"": ""1 1 1 0""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.2937759 0.02592597"",
		""anchormax"": ""0.3383071 0.1013889"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}]";


	#endregion Gui2
        #endregion

        #region COMMANDS

        [ChatCommand("bp")]
        void cmdBackpackShow(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            /*if (!player.IsAdmin)
            {
                player.ChatMessage("Рюкзак на тех работах, не беспокойтесь, ваши вещи не пропадут!");
                return;
            }*/
           
            if (player == null) return;
            if (player.inventory.loot?.entitySource != null)
            {
                BackpackBox bpBox;
                if (openedBackpacks.TryGetValue(player.userID, out bpBox) &&
                    bpBox.gameObject == player.inventory.loot.entitySource.gameObject)
                {
                    return;
                }
                player.EndLooting();
                NextTick(() => BackpackShow(player));
                return;
            }
            else
            {
                BackpackShow(player);
            }
        }

        [ConsoleCommand("backpack.open")]
        void cmdOnBackPackShowClick(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (player.inventory.loot?.entitySource != null)
            {
                BackpackBox bpBox;
                if (openedBackpacks.TryGetValue(player.userID, out bpBox) &&
                    bpBox.gameObject == player.inventory.loot.entitySource.gameObject)
                {
                    return;
                }
                player.EndLooting();
                NextTick(() => BackpackShow(player));
                return;
            }
            else
            {
                BackpackShow(player);
            }
        }

        #endregion

        #region ITEM EXTENSION

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
            public bool weapon;
            public List<SavedItem> mods;
        }

        List<SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();

        SavedItem SaveItem(Item item)
        {
            SavedItem iItem = new SavedItem
            {
                shortname = item.info?.shortname,
                amount = item.amount,
                mods = new List<SavedItem>(),
                skinid = item.skin
            };
            if (item.info == null) return iItem;
            iItem.itemid = item.info.itemid;
            iItem.weapon = false;
            if (item.hasCondition)
            {
                iItem.condition = item.condition;
                iItem.maxcondition = item.maxCondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if(flameThrower != null)
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
            Item item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);
            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if(flameThrower)
                flameThrower.ammo = sItem.flamefuel;
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

            if (sItem.mods != null)
                foreach (var mod in sItem.mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }

        #endregion

        #region EXTERNAL CALLS

        [PluginReference] Plugin Duel;

        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        #endregion
        

        public List<string> permisions = new List<string>()
        {
            "backpack.size1",
            "backpack.size2"
        };




        #region File Manager

        private GameObject FileManagerObject;
        private FileManager m_FileManager;

        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject( "MAP_FileManagerObject" );
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile( "Backpack/Images" );

            private class FileInfo
            {
                public string Url;
                public string Png;
            }

            public void SaveData()
            {
                dataFile.WriteObject( files );
            }

            public string GetPng( string name ) => files[ name ].Png;

            private void Awake()
            {
                files = dataFile.ReadObject<Dictionary<string, FileInfo>>() ?? new Dictionary<string, FileInfo>();
            }

            public IEnumerator LoadFile( string name, string url )
            {
                if (files.ContainsKey( name ) && files[ name ].Url == url && !string.IsNullOrEmpty( files[ name ].Png )) yield break;
                files[ name ] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine( LoadImageCoroutine( name, url ) );
            }

            IEnumerator LoadImageCoroutine( string name, string url )
            {
                using (WWW www = new WWW( url ))
                {
                    yield return www;
                    using (MemoryStream stream = new MemoryStream())
                    {
                        if (string.IsNullOrEmpty( www.error ))
                        {
                            var entityId = CommunityEntity.ServerInstance.net.ID;
                            var crc32 = FileStorage.server.Store(www.bytes, FileStorage.Type.png, entityId ).ToString();
                            files[ name ].Png = crc32;
                        }
                    }
                }
                loaded++;
            }
        }

        #endregion
		#region permissions
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                if(player == null || string.IsNullOrEmpty(permissionName))
                    return false;

                var uid = player.UserIDString;
                if(permission.UserHasPermission(uid, permissionName))
                    return true;

                return false;
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if(owner == null) throw new ArgumentNullException("owner");
                if(permissions == null) throw new ArgumentNullException("commands");

                foreach(var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
		#endregion
    }
}
