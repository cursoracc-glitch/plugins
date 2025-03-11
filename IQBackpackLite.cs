using System;
using UnityEngine;
using Object = System.Object;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;
using ConVar;
using System.Text;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQBackpack Lite", "Mercury", "1.15.31")]
    [Description("t.me/slivplugin")]
    class IQBackpackLite : RustPlugin
    {
        private List<Item> GetItemBlacklist(BasePlayer player)
        {
            Configuration.Backpack.BackpackCraft Backpack = GetBackpackOption(player);
            if (Backpack == null || Backpack.BlackListItems == null || Backpack.BlackListItems.Count == 0) return null;
            List<Item> ItemList = new List<Item>();

            foreach (Item item in player.inventory.AllItems())
                foreach (String Shortname in Backpack.BlackListItems)
                    if (item.info.shortname == Shortname)
                        ItemList.Add(item);

            return ItemList;
        }
        public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);

        
        
                private const Boolean LanguageEn = false;
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            if (!player.userID.IsSteamId() || player.IsNpc) return;
            if (IsDuel(player.userID)) return;
            if (permission.UserHasPermission(player.UserIDString, PermissionNoDropBP)) return;

            Object hookResult = CanDropBackpack(player.userID, new Vector3());
            if (hookResult is Boolean && (Boolean)hookResult == false) return;
            
            DropBackpack(player, config.TurnedsSetting.TypeDropBackpack);
            return;
        }
		   		 		  						  	   		  	 	 		  	 				  	   		   			
        void OnUserGroupRemoved(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            foreach (var Option in config.BackpackItem.BackpacOption.OrderByDescending(x => x.AmountSlot).Where(x => PermissionsGroup.Contains(x.Permissions)))
                UpdatePermissions(id, Option.Permissions, false);
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        
                private void OnNewSave(String filename) => ClearData();
        
        private const String PermissionNoDropBP = "iqbackpacklite.nodropbp";

                private Item OnItemSplit(Item item, int amount)
        {
            if (item == null) return null;
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null;
            if (item.IsLocked())
            {
                Item x = ItemManager.CreateByPartialName(item.info.shortname, amount);
                x.name = item.name;
                x.skin = item.skin;
                x.amount = amount;
                x.SetFlag(global::Item.Flag.IsLocked, true);
                item.amount -= amount;
                return x;
            }
            return null;
        }
        private class InterfaceBuilder
        {
            
            public static InterfaceBuilder Instance;
            public const String UI_Backpack_Visual = "UI_BACKPACK_VISUAL";
            public Dictionary<String, String> Interfaces;

            
            
            public InterfaceBuilder()
            {
                Instance = this;
                Interfaces = new Dictionary<String, String>();

                BuildingBackpack_Visual_Backpack_Slot();
            }

            public static void AddInterface(String name, String json)
            {
                if (Instance.Interfaces.ContainsKey(name))
                {
                    _.PrintError($"Error! Tried to add existing cui elements! -> {name}");
                    return;
                }

                Instance.Interfaces.Add(name, json);
            }

            public static string GetInterface(String name)
            {
                string json = string.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];

                    CuiHelper.DestroyUi(player, UI_Backpack_Visual);
                }
            }

            
                        private void BuildingBackpack_Visual_Backpack_Slot()
            {
                CuiElementContainer container = new CuiElementContainer();
                Configuration.Turneds.VisualBackpackSlot.Position Position = config.TurnedsSetting.VisualBackpackSlots.PositionSlotVisual;

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.15" },
                    RectTransform = { AnchorMin = Position.AnchorMin, AnchorMax = Position.AnchorMax, OffsetMin = Position.OffsetMin, OffsetMax = Position.OffsetMax }
                }, "Overlay", UI_Backpack_Visual);

                container.Add(new CuiElement
                {
                    Name = "BpImage",
                    Parent = UI_Backpack_Visual,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = _.GetImage(config.BackpackItem.UrlBackpack) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                }
                });

                if (config.TurnedsSetting.VisualBackpackSlots.UseSlots)
                {
                    container.Add(new CuiElement
                    {
                        Name = "IsFullSlots",
                        Parent = UI_Backpack_Visual,
                        Components = {
                        new CuiTextComponent { Text = "%SLOTS_INFO%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "0.91 0.87 0.83 0.5"  },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26.01 -30.07", OffsetMax = "26.01 -11.00" } 
                    }
                    });
                }

                if (config.TurnedsSetting.VisualBackpackSlots.UseSlots)
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0 0 0 0.2" },
                        RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -30.07", OffsetMax = "3.73 30.07" } 
                    }, UI_Backpack_Visual, "IsFullPanel");

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "%Y_PROGRESS_COLOR%" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 %Y_PROGRESS%", OffsetMin = "0.5 1", OffsetMax = "0 0" }
                    }, "IsFullPanel", "IsFullProgress");
                }

                if (config.TurnedsSetting.VisualBackpackSlots.UseButton)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = "bp" },
                        Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }, UI_Backpack_Visual, "OPEN_BACKPACK");
                }

                AddInterface("UI_Backpack_Visual_Backpack_Slot", container.ToJson());
            }
                    }

        void OnUserGroupAdded(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;

            foreach (var Option in config.BackpackItem.BackpacOption.OrderByDescending(x => x.AmountSlot).Where(x => PermissionsGroup.Contains(x.Permissions)))
                UpdatePermissions(id, Option.Permissions, true);
        }

        void Unload()
        {
            ServerMgr.Instance.StopCoroutine(DownloadImages());
            InterfaceBuilder.DestroyAll();
            WriteData();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                foreach (Item item in player.inventory.AllItems())
                    item.SetFlag(global::Item.Flag.IsLocked, false);

            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(p => PlayerBackpack.ContainsKey(p) && PlayerBackpack[p] != null))
                PlayerBackpack[player].Destroy();

            _ = null;
        }

        
                private class BackpackBehaviour : FacepunchBehaviour
        {
            private BasePlayer Player = null;
            public StorageContainer Container = null;
            public UInt64 BackpackID = 0;
            private Dictionary<Item, Item.Flag> SaveFlags = new Dictionary<Item, Item.Flag>();
            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                BackpackID = Player.userID;
            }
            private void BlackListAction(Boolean State)
            {
                List<Item> Itemlist = _.GetItemBlacklist(Player);
                if (Itemlist == null) return;

                foreach (Item item in Itemlist)
                {
                    if (State)
                        if (!SaveFlags.ContainsKey(item))
                            SaveFlags.Add(item, item.flags);

                    item.SetFlag(global::Item.Flag.IsLocked, State);
                }
		   		 		  						  	   		  	 	 		  	 				  	   		   			
                if (!State)
                    foreach (KeyValuePair<Item, Item.Flag> Items in SaveFlags)
                        Items.Key.SetFlag(Items.Value, true);

                Player.SendNetworkUpdate();
            }
            public void Open()
            {
                Container = CreateContainer(Player);
		   		 		  						  	   		  	 	 		  	 				  	   		   			
                PushItems();

                _.timer.Once(0.1f, () => PlayerLootContainer(Player, Container));
                BlackListAction(true);

                if (!_.PlayerUseBackpacks.Contains(Player))
                    _.PlayerUseBackpacks.Add(Player);
                
                Interface.Oxide.CallHook("OnBackpackOpened", Player, Container.OwnerID, Container);
            }

            public void Close()
            {
                Interface.Oxide.CallHook("OnBackpackClosed", Player, Container.OwnerID, Container);

                _.Backpacks[BackpackID].Items = SaveItems(Container.inventory.itemList);
                Container.inventory.Clear();
                Container.Kill();
                Container = null;
                
                Destroy(false);
                BlackListAction(false);
                if (_.PlayerUseBackpacks.Contains(Player))
                    _.PlayerUseBackpacks.Remove(Player);
            }

            private void PushItems()
            {
                _.Unsubscribe("OnItemAddedToContainer");

                var items = RestoreItems(_.Backpacks[BackpackID].Items);
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(Container.inventory, items[i].position);

                _.Subscribe("OnItemAddedToContainer");
            }

            public void Destroy(bool isClose = true)
            {
                if (isClose)
                    Close();

                UnityEngine.Object.Destroy(this);
            }
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (_interface == null)
            {
                timer.Once(3f, () => OnPlayerConnected(player));
                return;
            }
            if (player == null) return;

            if (!PlayerBackpack.ContainsKey(player))
                PlayerBackpack.Add(player, null);
		   		 		  						  	   		  	 	 		  	 				  	   		   			
                if (!Backpacks.ContainsKey(player.userID))
                    Backpacks.Add(player.userID, new BackpackInfo
                    {
                        AmountSlot = GetAvailableSlots(player)
                    });

            CheckConnectionPermission(player);
            
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
            
            if (!player.IsDead() && !IsDuel(player.userID) && !player.IsSleeping())
                DrawUI_Backpack_Visual(player);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        void OnPlayerSleepEnded(BasePlayer player) => OnPlayerConnected(player);
        private void OnServerShutdown() => Unload();
        
                public void SendChat(String Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            Configuration.Reference.IQChat Chat = config.References.IQChatSetting;
            if (IQChat)
                if (Chat.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private void ClearData()
        {
            if (!config.TurnedsSetting.WipeCleaning) return;
            foreach (KeyValuePair<UInt64, BackpackInfo> Backpack in Backpacks)
                Backpack.Value.Items.Clear();
            WriteData();
        }
        static Item BuildItem(BackpackInfo.SavedItem sItem)
        {
            if (sItem.Amount < 1) sItem.Amount = 799 > 0 ? 1 : 0;
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, sItem.Amount, sItem.Skinid);
            item.position = sItem.TargetSlot;

            if (sItem.Text != null && !String.IsNullOrWhiteSpace(sItem.Text))
                item.text = sItem.Text;
            
            if (sItem.GrowableGenes != 0)
                GrowableGeneEncoding.EncodeGenesToItem(sItem.GrowableGenes, item);
            
            if (sItem.Mods != null)
            {
                foreach (var mod in sItem.Mods)
                    item.contents.AddItem(BuildItem(mod).info, mod.Amount, mod.Skinid);
            }
            
            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
                item.busyTime = sItem.BusyTime;
            }

            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            if (sItem.Name != null)
                item.name = sItem.Name;

            if (sItem.OnFire)
                item.SetFlag(global::Item.Flag.OnFire, true);

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower)
                flameThrower.ammo = sItem.Flamefuel;
            
            BaseEntity Subentity;
            UInt64 entityId = item.instanceData?.subEntity.Value ?? 0;
            if (entityId == 0)
            {
                ItemModSign itemModSign = item.info.GetComponent<ItemModSign>();
                if (itemModSign == null)
                    return item;
		   		 		  						  	   		  	 	 		  	 				  	   		   			
                Subentity = itemModSign.CreateAssociatedEntity(item);
            }
            else
            {
                Subentity = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as BaseEntity;

                if (Subentity == null)
                    return item;
            }

            PhotoEntity photoEntity = Subentity as PhotoEntity;
            if ((Object)photoEntity != null)
            {
                Byte[] fileContent = FileStorage.server.Get(sItem.FileContents, FileStorage.Type.jpg, Subentity.net.ID);

                photoEntity.SetImageData(sItem.IdPhoto, fileContent);

                return item;
            }
            
            return item;
        }
        
        void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null || player == null) return;
            
            if(entity is MLRS)
                DrawUI_Backpack_Visual(player);
        }
        private class NoRagdollCollision : FacepunchBehaviour
        {
            private Collider _collider;

            private void Awake()
            {
                _collider = GetComponent<Collider>();
            }

            private void OnCollisionEnter(Collision collision)
            {
                if (collision.collider.IsOnLayer(Rust.Layer.Ragdoll))
                {
                    UnityEngine.Physics.IgnoreCollision(_collider, collision.collider);
                }
            }
        }
        
        private void UpdatePermissions(String ID, String Permissions, Boolean IsGranted, Boolean ReCheack = false)
        {
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);
            if (player == null) return;

            if (IQPermissions && !ReCheack)
            {
                timer.In(3f, () =>
                {
                    UpdatePermissions(ID, Permissions, permission.UserHasPermission(ID, Permissions), true);
                });
                return;
            }

            if (config.BackpackItem.BackpacOption.Find(x => x.Permissions.Equals(Permissions)) == null) return;
            if (!Backpacks.ContainsKey(player.userID)) return;
            player.EndLooting();

            Int32 AvailableSlots = GetAvailableSlots(player);
            if (Backpacks[player.userID].AmountSlot == AvailableSlots) return;
            if (AvailableSlots < GetBusySlotsBackpack(player))
            {
                Int32 Count = Backpacks[player.userID].Items.Count - 1;
                foreach (BackpackInfo.SavedItem Sitem in Backpacks[player.userID].Items.Take((Backpacks[player.userID].Items.Count - AvailableSlots)))
                {
                    NextTick(() =>
                    {
                        Item itemDrop = BuildItem(Sitem);
                        itemDrop.DropAndTossUpwards(player.transform.position, 2f);

                        Backpacks[player.userID].Items.RemoveAt(Count);
                        Count--;
                    });
                }
            }
            Backpacks[player.userID].AmountSlot = AvailableSlots;

            NextTick(() => {
                DrawUI_Backpack_Visual(player);
                SendChat(GetLang((IsGranted ? "BACKPACK_GRANT" : "BACKPACK_REVOKE"), player.UserIDString, AvailableSlots), player);
            });
        }
        private Int32 GetSlotsBackpack(BasePlayer player)
        {
            Int32 SlotsBackpack = 0;

            if (Backpacks.ContainsKey(player.userID))
                SlotsBackpack = Backpacks[player.userID].AmountSlot;

            return SlotsBackpack;
        }
        
        
        private Configuration.Backpack.BackpackCraft GetBackpackOption(BasePlayer player)
        {
            Configuration.Backpack.BackpackCraft BCraft = config.BackpackItem.BackpacOption.OrderByDescending(x => x.AmountSlot).FirstOrDefault(x => permission.UserHasPermission(player.UserIDString, x.Permissions));
            return BCraft;
        }
        private class Configuration
        {
            internal class Backpack
            {
                [JsonProperty(LanguageEn ? "Link to the picture to display the backpack" : "Ссылка на картинку для отображения рюкзака")]
                public String UrlBackpack = "https://cdn.discordapp.com/attachments/1124746976093814796/1208383710144237579/rPeKd9R.png";

                [JsonProperty(LanguageEn ? "Variations of backpacks by privileges (An available set is given to the player who is higher than others)" : "Вариации рюкзаков по привилегиям (Дается доступный набор игроку, который выше других)")]
                public List<BackpackCraft> BackpacOption = new List<BackpackCraft>();
                internal class BackpackCraft
                {
                    [JsonProperty(LanguageEn ? "Permission to be able to craft and carry this backpack (do not leave this field empty, otherwise it will not be taken into account)" : "Права для возможности крафтить и носить данный рюкзак(не оставляйте это поле пустым, иначе оно не будет учитываться)")]
                    public String Permissions = "iqbackpacklite.7slot";
                    [JsonProperty(LanguageEn ? "The amount slots in this backpack" : "Количество слотов у данного рюкзака")]
                    public Int32 AmountSlot = 7;
                    [JsonProperty(LanguageEn ? "Blacklist of items for this backpack" : "Черный список предметов для данного рюкзака")]
                    public List<String> BlackListItems = new List<String>();
                }

            }
            internal class Reference
            {
                [JsonProperty(LanguageEn ? "Setting up IQChat" : "Настройка IQChat")]
                public IQChat IQChatSetting = new IQChat();
                internal class IQChat
                {
                    [JsonProperty(LanguageEn ? "IQChat : Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix = "[IQBackpack-Lite]";
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat (If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar = "0";
                    [JsonProperty(LanguageEn ? "IQChat : Use UI notifications" : "IQChat : Использовать UI уведомления")]
                    public Boolean UIAlertUse = false;
                }
            }

            [JsonProperty(LanguageEn ? "Additional configuration" : "Дополнительная настройка")]
            public Turneds TurnedsSetting = new Turneds();
            [JsonProperty(LanguageEn ? "Setting up a backpack" : "Настройка рюкзака")]
            public Backpack BackpackItem = new Backpack();
            [JsonProperty(LanguageEn ? "Configuring supporting plugins" : "Настройка поддерживающих плагинов")]
            public Reference References = new Reference();
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    BackpackItem = new Backpack
                    {
                        UrlBackpack = "https://cdn.discordapp.com/attachments/1124746976093814796/1208383710144237579/rPeKd9R.png",

                        BackpacOption = new List<Backpack.BackpackCraft>
                        {
                            new Backpack.BackpackCraft
                            {
                                AmountSlot = 7,
                                Permissions = "iqbackpacklite.7slot",
                                BlackListItems = new List<String> { },                              
                            },
                            new Backpack.BackpackCraft
                            {
                                AmountSlot = 15,
                                Permissions = "iqbackpacklite.15slot",
                                BlackListItems = new List<String>
                                {
                                    "rocket.launcher",
                                    "ammo.rocket.basic",
                                    "explosive.satchel",
                                    "supply.signal",
                                    "explosive.timed",
                                },                              
                            }
                        }
                    },
                    TurnedsSetting = new Turneds
                    {
                        TypeDropBackpack = TypeDropBackpack.DropBackpack,
                        RemoveBackpack = 200f,
                        WipeCleaning = false,
                        ClosePressedAgain = true,
                        VisualBackpackSlots = new Turneds.VisualBackpackSlot
                        {
                            UseSlots = true,
                            UseButton = true,
                            ColorProgressBar = new Turneds.VisualBackpackSlot.ColorProgress
                            {
                                ColorMinimal = "0.44 0.53 0.26 1.00",
                                ColorAverage = "0.98 0.53 0.26 1.00",
                                ColorMaximum = "0.98 0.20 0.28 1.00",
                            },
                            PositionSlotVisual = new Turneds.VisualBackpackSlot.Position
                            {
                                AnchorMin = "0.5 0",
                                AnchorMax = "0.5 0",
                                OffsetMin = "-264.27 17.94",
                                OffsetMax = "-203.72 78.08"
                            },
                        },
                    },
                    References = new Reference
                    {
                        IQChatSetting = new Reference.IQChat
                        {
                            CustomAvatar = "0",
                            CustomPrefix = "[IQBackpackLite] ",
                            UIAlertUse = false,
                        }
                    }
                };
            }
            internal class Turneds
            {
                [JsonProperty(LanguageEn ? "Time to remove the backpack when falling out (Works with : 2 - Throws the backpack with objects)" : "Время удаления рюкзака при выпадении (Работает с : 2 - Выбрасывает рюкзак с предметами)")]
                public Single RemoveBackpack = 200f;
                [JsonProperty(LanguageEn ? "Automatically clear the inventory of players' backpacks after the vape (true - yes/false - no)" : "Автоматически очищать инвентарь рюкзаков игроков после вайпа (true - да/false - нет)")]
                public Boolean WipeCleaning = false;
                [JsonProperty(LanguageEn ? "Close the backpack when clicking on the UI again/using the bind if it is open" : "Закрывать рюкзак при повторном нажатии на UI/использовании бинда, если он открыт")]
                public Boolean ClosePressedAgain = true;
                [JsonProperty(LanguageEn ? "Interface Setup" : "Настройка интерфейса")]
                public VisualBackpackSlot VisualBackpackSlots = new VisualBackpackSlot();
                internal class VisualBackpackSlot
                {
                    [JsonProperty(LanguageEn ? "Setting up the position of the UI slot with a backpack" : "Настройка позиции UI слота с рюкзаком")]
                    public Position PositionSlotVisual = new Position();
                    internal class ColorProgress
                    {
                        [JsonProperty(LanguageEn ? "The color of the fullness strip when the backpack is >30% full" : "Цвет полосы заполненности, когда рюкзак заполнен на >30%")]
                        public String ColorMinimal = "0.44 0.53 0.26 1.00";
                        [JsonProperty(LanguageEn ? "The color of the fullness band when the backpack is >80% full" : "Цвет полосы заполненности, когда рюкзак заполнен на >80%")]
                        public String ColorMaximum = "0.98 0.20 0.28 1.00";
                        [JsonProperty(LanguageEn ? "The color of the fullness band when the backpack is >60% full" : "Цвет полосы заполненности, когда рюкзак заполнен на >60%")]
                        public String ColorAverage = "0.98 0.53 0.26 1.00";
                    }
                    [JsonProperty(LanguageEn ? "Allow to open the backpack by clicking on the UI interface (true - yes/false - no)" : "Разрешить открывать рюкзак нажава на UI интерфейс (true - да/false - нет)")]
                    public Boolean UseButton = true;
                    internal class Position
                    {
                        public String AnchorMin;
                        public String AnchorMax;
                        public String OffsetMin;
                        public String OffsetMax;
                    }
                    [JsonProperty(LanguageEn ? "Display the number of slots in the backpack on the UI (true - yes/false - no)" : "Отображать количество слотов в рюкзаке на UI (true - да/false - нет)")]
                    public Boolean UseSlots = true;
                    [JsonProperty(LanguageEn ? "Adjusting the colors of the fullness bar" : "Настройка цветов полосы заполненности")]
                    public ColorProgress ColorProgressBar = new ColorProgress();

                }
                [JsonProperty(LanguageEn ? "The type of work of the backpack: 0 - you need to put it on to use, 1 - only permissions are required (from the variations of backpacks)" : "Тип выпадения рюкзака : 0 - Не выпадает при смерти, 1 - Выбрасывает предметы вокруг трупа, 2 - Выбрасывает рюкзак с предметами")]
                public TypeDropBackpack TypeDropBackpack = TypeDropBackpack.DropBackpack;
            }
        }
        static List<BackpackInfo.SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();

                public Boolean IsDuel(UInt64 userID)
        {
            if (EventHelper)
                if (EventHelper.Call<Boolean>("IsPlayerSetup", userID))
                    return true;
            
            if (Battles) return (Boolean)Battles?.Call("IsPlayerOnBattle", userID);
            if (Duel) return (Boolean)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            if (OneVSOne) return (Boolean)OneVSOne?.Call("IsEventPlayer", BasePlayer.FindByID(userID));
            if (ArenaTournament) return ArenaTournament.Call<Boolean>("IsOnTournament", userID);
            return false;
        }
        void OnGroupPermissionRevoked(string name, string perm)
        {
            String[] GroupUser = permission.GetUsersInGroup(name);
            if (GroupUser == null) return;

            foreach (String IDs in GroupUser)
                UpdatePermissions(IDs.Substring(0, 17), perm, false);
        }
        
        public static Object CanOpenBackpack(BasePlayer player, UInt64 ownerId)
        {
            return Interface.CallHook("CanOpenBackpack", player, ownerId);
        }
        private static void PlayerLootContainer(BasePlayer player, StorageContainer container)
        {
            container.SetFlag(BaseEntity.Flags.Open, true, false);
            player.inventory.loot.StartLootingEntity(container, false);
            player.inventory.loot.AddContainer(container.inventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
            container.DecayTouch();
            container.SendNetworkUpdate();
        }
        
                private void DropBackpack(BasePlayer player, TypeDropBackpack typeDropBackpack)
        {
            if (!PlayerBackpack.ContainsKey(player)) return;
            if (PlayerBackpack[player] != null)
                PlayerBackpack[player].Close();
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
            UInt64 ID = player.userID;
            List<BackpackInfo.SavedItem> SavedList = GetSavedList(ID);
            if (SavedList == null || SavedList.Count == 0) return;
            switch (typeDropBackpack)
            {
                case TypeDropBackpack.DropItems:
                    {
                        foreach (BackpackInfo.SavedItem sItem in SavedList)
                        {
                            Item BuildedItem = BuildItem(sItem);
                            BuildedItem.DropAndTossUpwards(player.transform.position, Oxide.Core.Random.Range(2, 6));
                        }
                        break;
                    }
                case TypeDropBackpack.DropBackpack:
                    {
                        String Prefab = "assets/prefabs/misc/item drop/item_drop_backpack.prefab";
                        DroppedItemContainer BackpackDrop = (BaseEntity)GameManager.server.CreateEntity(Prefab, player.transform.position + new Vector3(Oxide.Core.Random.Range(-1f, 1f), 0f, 0f)) as DroppedItemContainer;
                        BackpackDrop.gameObject.AddComponent<NoRagdollCollision>();

                        BackpackDrop.lootPanelName = "generic_resizable";
                        BackpackDrop.playerName = $"{player.displayName ?? "Somebody"}'s Backpack";
                        BackpackDrop.playerSteamID = player.userID;

                        BackpackDrop.inventory = new ItemContainer();
                        BackpackDrop.inventory.ServerInitialize(null, GetSlotsBackpack(player));
                        BackpackDrop.inventory.GiveUID();
                        BackpackDrop.inventory.entityOwner = BackpackDrop;
                        BackpackDrop.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);

                        foreach (BackpackInfo.SavedItem sItem in SavedList)
                        {
                            Item BuildedItem = BuildItem(sItem);
                            BuildedItem.MoveToContainer(BackpackDrop.inventory, sItem.TargetSlot);
                        }

                        BackpackDrop.SendNetworkUpdate();
                        BackpackDrop.Spawn();
                        BackpackDrop.ResetRemovalTime(Math.Max(config.TurnedsSetting.RemoveBackpack, BackpackDrop.CalculateRemovalTime()));
                        break;
                    }
                default:
                    break;
            }
            SavedList.Clear();
        }
        void OnUserPermissionRevoked(string id, string permName) => UpdatePermissions(id, permName, false);
        
                private void OpenBP(BasePlayer player)
        {        
            if (GetSlotsBackpack(player) == 0)
            {
                SendChat(GetLang("BACKPACK_NULL", player.UserIDString), player);
                return;
            }
            if (IsDuel(player.userID)) return;
            
            Object hookResult = CanOpenBackpack(player, player.userID);
            if (hookResult is String)
            {
                SendChat(hookResult as String, player);
                return;
            }

            BackpackBehaviour backpackHandler = null;
            if (PlayerBackpack.ContainsKey(player))
            {
                if (PlayerBackpack[player] != null)
                    backpackHandler = PlayerBackpack[player];
            }
            else PlayerBackpack.Add(player, null);
            if (backpackHandler == null)
            {
                backpackHandler = player.gameObject.AddComponent<BackpackBehaviour>();

                PlayerBackpack[player] = backpackHandler;
            }
            if (backpackHandler.Container != null)
            {
                if (config.TurnedsSetting.ClosePressedAgain)
                    player.EndLooting();
                else SendChat(GetLang("BACKPACK_IS_OPENED", player.UserIDString), player);
                return;
            }
            
            player.EndLooting();
            backpackHandler.Open();
        }
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQBackpackLite/Backpacks", Backpacks);
        
                private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }

        internal class BackpackInfo
        {
            public Int32 AmountSlot = 0;
            public List<SavedItem> Items = new List<SavedItem>();

            internal class SavedItem
            {
                public Int32 TargetSlot;
                public String Shortname;
                public Int32 Itemid;
                public Single Condition;
                public Single Maxcondition;
                public Int32 Amount;
                public Int32 Ammoamount;
                public String Ammotype;
                public Int32 Flamefuel;
                public UInt64 Skinid;
                public String Name;
                public Boolean Weapon;
                public Int32 Blueprint;
                public Single BusyTime;
                public Boolean OnFire;
                public UInt32 FileContents;
                public UInt64 IdPhoto;
                public String Text;
                public Int32 GrowableGenes;
                public List<SavedItem> Mods;
            }
        }
        public static IQBackpackLite _ = null;
        public Dictionary<UInt64, BackpackInfo> _old_Backpacks = new Dictionary<UInt64, BackpackInfo>();

        
                public static StorageContainer CreateContainer(BasePlayer player)
        {
            StorageContainer storage = GameManager.server.CreateEntity("assets/prefabs/misc/halloween/coffin/coffinstorage.prefab") as StorageContainer;
            if (storage == null) return null;

            var containerEntity = storage as StorageContainer;
            if (containerEntity == null)
            {
                UnityEngine.Object.Destroy(storage);
                return null;
            }
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<GroundWatch>());

            foreach (var collider in storage.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(collider);

            storage.transform.position = new Vector3(player.ServerPosition.x, player.ServerPosition.y - 100f, player.ServerPosition.z);
            storage.panelName = "generic_resizable";

            ItemContainer container = new ItemContainer { playerOwner = player };
            container.ServerInitialize((Item)null, _.GetSlotsBackpack(player));
            if ((Int32)container.uid.Value == 0)
                container.GiveUID();

            container.entityOwner = storage;
            storage.inventory = container;
            storage.OwnerID = player.userID;

            storage._limitedNetworking = false;
            storage.EnableSaving(false);

            storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)SendMessageOptions.DontRequireReceiver);
            storage.Spawn();

            storage.inventory.allowedContents = ItemContainer.ContentsType.Generic;
            return storage;
        }
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (entity == null || player == null) return;

            BackpackBehaviour backpackHandler = null;
            if (PlayerBackpack.ContainsKey(player) && PlayerBackpack[player] != null)
                backpackHandler = PlayerBackpack[player];

            StorageContainer storage = entity as StorageContainer;

            if (player != null && storage != null && backpackHandler != null && storage == backpackHandler.Container)
            {
                backpackHandler.Close();
                DrawUI_Backpack_Visual(player);
            }
        }
        void OnServerInitialized()
        {
            _ = this;
            ServerMgr.Instance.StartCoroutine(DownloadImages());
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            RegisteredPermissions();
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            if (config.TurnedsSetting.TypeDropBackpack == TypeDropBackpack.NoDrop)
                Unsubscribe("OnPlayerDeath");
        }
        private Int32 GetAvailableSlots(BasePlayer player)
        {
            Int32 AvailableSlots = 0;

            Configuration.Backpack.BackpackCraft BCraft = GetBackpackOption(player);
            if (BCraft == null) return AvailableSlots;
            AvailableSlots = BCraft.AmountSlot;

            return AvailableSlots;
        }
        
        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (container == null || item == null) return null;
            BasePlayer player = container.playerOwner;
            if (player == null || !player.userID.IsSteamId() || player.IsNpc) return null;
            if (!PlayerUseBackpacks.Contains(player)) return null;

            Configuration.Backpack.BackpackCraft OptionBackpack = GetBackpackOption(player);
            if (OptionBackpack == null)
                return null;
            
            if (OptionBackpack.BlackListItems.Contains(item.info.shortname) && item.IsLocked())
                return ItemContainer.CanAcceptResult.CannotAccept;

            return null;
        }
        private List<BasePlayer> PlayerUseBackpacks = new List<BasePlayer>();
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
        /// <summary>
        /// Обновление 1.0.х
        /// - Добавлен хук при открытии рюкзака : void OnBackpackOpened(BasePlayer player, ulong backpackOwnerID, ItemContainer backpackContainer)
        /// - Добавлен хук при закрытии рюкзака : void OnBackpackClosed(BasePlayer player, ulong backpackOwnerID, ItemContainer backpackContainer)
        /// - Добавлена поддержка генов
        /// - Добавлена корректировка UI если игрок спит - UI не будет появляться
        /// - Добавлена корректировка UI если игрок сел в MLRS - UI не будет появляться
        /// - Исправлено NRE с фото
        /// - Перезалил картинки на новый фото-хостинг

                [PluginReference] Plugin ImageLibrary, IQChat, Battles, Duel, OneVSOne, ArenaTournament, EventHelper, IQPermissions;
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private void RegisteredPermissions()
        {
            foreach (Configuration.Backpack.BackpackCraft BPCraft in config.BackpackItem.BackpacOption)
                permission.RegisterPermission(BPCraft.Permissions, this);
                
            permission.RegisterPermission(PermissionNoDropBP, this);

        }

        [ChatCommand("bp")]
        void OpenBackpackChat(BasePlayer player)
        {
            if (player == null) return;
            
            timer.Once(0.3f, ()=> OpenBP(player));
        }

        
        
        public Dictionary<UInt64, BackpackInfo> Backpacks = new Dictionary<UInt64, BackpackInfo>();
        static List<Item> RestoreItems(List<BackpackInfo.SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.Weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            }).Where(i => i != null).ToList();
        }

        
        
        
                private Int32 GetSlotsPercent(Single Percent, Single Slots)
        {
            Single ReturnSlot = (((Single)Slots / 100.0f) * Percent);
            return (Int32)ReturnSlot;
        }
        static Item BuildWeapon(BackpackInfo.SavedItem sItem)
        {
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, 1, sItem.Skinid);
            item.position = sItem.TargetSlot;

            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
            }

            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.Ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.Ammoamount;
            }

            if (sItem.Mods != null)
                foreach (var mod in sItem.Mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }
        static BackpackInfo.SavedItem SaveItem(Item item)
        {
            BackpackInfo.SavedItem iItem = new BackpackInfo.SavedItem
            {
                TargetSlot = item.position,
                Shortname = item.info.shortname,
                Amount = item.amount,
                Mods = new List<BackpackInfo.SavedItem>(),
                Skinid = item.skin,
                BusyTime = item.busyTime,

            };
            
            if (item.info.amountType == ItemDefinition.AmountType.Genetics && item.instanceData != null && item.instanceData.dataInt != 0)
                iItem.GrowableGenes = item.instanceData.dataInt;
            
            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                iItem.OnFire = true;
            }
            
            iItem.Text = item.text;
            
            if (item.info == null) return iItem;
            iItem.Itemid = item.info.itemid;
            iItem.Weapon = false;
            
            UInt64 subEntityId = item.instanceData?.subEntity.Value ?? 0;
            if (subEntityId != 0)
            {
                BaseEntity subEntity = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as BaseEntity;
                if (subEntity == null) return iItem;

                PhotoEntity photoEntity = subEntity as PhotoEntity;
                if (photoEntity != null && photoEntity.ImageCrc != 0)
                {
                    iItem.IdPhoto = photoEntity.PhotographerSteamId;
                    iItem.FileContents = photoEntity.ImageCrc;
                    return iItem;
                }
            }
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            if (item.contents != null && item.info.category.ToString() != "Weapon")
            {
                foreach (var itemCont in item.contents.itemList)
                {
                    Debug.Log(itemCont.info.shortname);

                    if (itemCont.info.itemid != 0)
                        iItem.Mods.Add(SaveItem(itemCont));
                }
            }

            iItem.Name = item.name;
            if (item.hasCondition)
            {
                iItem.Condition = item.condition;
                iItem.Maxcondition = item.maxCondition;
            }

            if (item.blueprintTarget != 0) iItem.Blueprint = item.blueprintTarget;

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null)
                iItem.Flamefuel = flameThrower.ammo;
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.Ammoamount = weapon.primaryMagazine.contents;
            iItem.Ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.Weapon = true;

            if (item.contents != null)
                foreach (var mod in item.contents.itemList)
                    if (mod.info.itemid != 0)
                        iItem.Mods.Add(SaveItem(mod));
            return iItem;
        }
                
        
        public static Object CanDropBackpack(UInt64 ownerId, Vector3 position)
        {
            return Interface.CallHook("CanDropBackpack", ownerId, position);
        }

        
        
        void OnUserPermissionGranted(string id, string permName) => UpdatePermissions(id, permName, true);
        private Dictionary<BasePlayer, BackpackBehaviour> PlayerBackpack = new Dictionary<BasePlayer, BackpackBehaviour>();
        private enum TypeDropBackpack
        {
            NoDrop,
            DropItems,
            DropBackpack,
        }
        
        private static InterfaceBuilder _interface;
        
                public static StringBuilder sb = new StringBuilder();
        
        
        
                
        private void CheckConnectionPermission(BasePlayer player)
        {
            UInt64 IDBackpack = player.userID;
            if (!Backpacks.ContainsKey(IDBackpack)) return;
            
            Int32 AvailableSlots = GetAvailableSlots(player);
            if (AvailableSlots < GetBusySlotsBackpack(player))
            {
                Int32 Count = Backpacks[IDBackpack].Items.Count - 1;
                foreach (BackpackInfo.SavedItem Sitem in Backpacks[IDBackpack].Items.Take((Backpacks[IDBackpack].Items.Count - AvailableSlots)))
                {
                    NextTick(() =>
                    {
                        Item itemDrop = BuildItem(Sitem);
                        itemDrop.DropAndTossUpwards(player.transform.position, 2f);

                        Backpacks[IDBackpack].Items.RemoveAt(Count);
                        Count--;
                    });
                }
            }
            Backpacks[IDBackpack].AmountSlot = AvailableSlots;
        }
        private Int32 GetBusySlotsBackpack(BasePlayer player)
        {
            if (Backpacks.ContainsKey(player.userID))
                return Backpacks[player.userID].Items.Count;
            return 0;
        }

        
                private List<BackpackInfo.SavedItem> GetSavedList(UInt64 ID)
        {
            List<BackpackInfo.SavedItem> SavedList = null;
            if (Backpacks.ContainsKey(ID))
                SavedList = Backpacks[ID].Items;

            return SavedList;
        }

        
        
        [ConsoleCommand("bp")]
        void OpenBackpackConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            timer.Once(0.3f, ()=> OpenBP(player));
        }
        void Init() => ReadData();

        void ReadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("IQSystem/IQBackpackLite/Backpacks"))
            {
                Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQSystem/IQBackpackLite/Backpacks");
                return;
            }

            _old_Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQBackpackLite/Backpacks");
            Backpacks = _old_Backpacks;
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQBackpackLite/Backpacks", Backpacks);
            Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQSystem/IQBackpackLite/Backpacks");

        }
        private IEnumerator DownloadImages()
        {
            Puts(LanguageEn ? "Generating the interface..." : "Генерируем интерфейс...");
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            if (!HasImage($"{config.BackpackItem.UrlBackpack}"))
                AddImage(config.BackpackItem.UrlBackpack, config.BackpackItem.UrlBackpack);
           
            yield return new WaitForSeconds(0.04f);

            Puts(LanguageEn ? "The interface has been successfully generated!" : "Интерфейс был успешно сгенерирован!");

            _interface = new InterfaceBuilder();

            timer.Once(3f, () =>
            {
                foreach (BasePlayer player in BasePlayer.allPlayerList)
                    OnPlayerConnected(player);
            });
        }

        
        
        private static Configuration config = new Configuration();
        void OnGroupPermissionGranted(string name, string perm)
        {
            String[] GroupUser = permission.GetUsersInGroup(name);
            if (GroupUser == null) return;

            foreach (String IDs in GroupUser)
                UpdatePermissions(IDs.Substring(0, 17), perm, true);
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
                PrintWarning(LanguageEn ? "Error " + $"reading the configuration 'oxide/config/{Name}', creating a new configuration!!" : "Ошибка " + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null || player == null) return;

            if (entity is MLRS)
                CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
        }
        private void DrawUI_Backpack_Visual(BasePlayer player)
        {
            if (_interface == null) return;
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Visual_Backpack_Slot");
            if (Interface == null) return;

            Single BusySlots = (Single)GetBusySlotsBackpack(player);
            Single Slots = (Single)GetSlotsBackpack(player);
            if (Slots == 0) return;
            Single Y_Progress = (Single)((Single)BusySlots / (Single)Slots);

            String Y_Progress_Color = BusySlots >= GetSlotsPercent(80.0f, Slots) ? config.TurnedsSetting.VisualBackpackSlots.ColorProgressBar.ColorMaximum :
                                      BusySlots >= GetSlotsPercent(60.0f, Slots) ? config.TurnedsSetting.VisualBackpackSlots.ColorProgressBar.ColorAverage :
                                                                                   config.TurnedsSetting.VisualBackpackSlots.ColorProgressBar.ColorMinimal;
		   		 		  						  	   		  	 	 		  	 				  	   		   			
            Interface = Interface.Replace("%CRAFT_BTN%", GetLang("CRAFT_BTN", player.UserIDString));
            Interface = Interface.Replace("%SLOTS_INFO%", $"<b>{BusySlots}/{Slots}</b>");
            Interface = Interface.Replace("%Y_PROGRESS%", $"{Y_Progress}");
            Interface = Interface.Replace("%Y_PROGRESS_COLOR%", $"{Y_Progress_Color}");

            CuiHelper.AddUi(player, Interface);
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {                
                ["BACKPACK_TITLE"] = "BACKPACK {0} SLOT(S)",
                ["BACKPACK_IS_OPENED"] = "Do you already have your backpack open!",
                ["BACKPACK_NO_INITIALIZE"] = "The plugin is loading, expect you will be able to open crafting soon!",

                ["BACKPACK_GRANT"] = "You have successfully received a backpack, the number of slots has been increased to : {0}",
                ["BACKPACK_REVOKE"] = "Your extra slots privilege expired, slots reduced to : {0}",
                ["BACKPACK_NULL"] = "You don't have a backpack available",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BACKPACK_TITLE"] = "РЮКЗАК {0} СЛОТА(ОВ)",
                ["BACKPACK_IS_OPENED"] = "У вас уже открыт рюкзак!",
                ["BACKPACK_NO_INITIALIZE"] = "Плагин загружается, ожидайте, вскоре вы сможете открыть крафт!",

                ["BACKPACK_GRANT"] = "Вы успешно получили рюкзак, количество слотов увеличено до : {0}",
                ["BACKPACK_REVOKE"] = "У вас истекла привилегия с дополнительными слотами, слоты уменьшились до : {0}",
                ["BACKPACK_NULL"] = "У вас нет доступного рюкзака",

            }, this, "ru");
        }

            }
}