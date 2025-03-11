using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    using SteamID = System.UInt64;
    [Info("Fucking Bank (BBank)", "bazuka5801", "1.0.0")]
    public class BBank : RustPlugin
    {
        #region [Section] Config

        private float NPC_Radius = 3f;
        
        private const int Hour = 3600;

        private const int CardCount = 3;

        enum CardType
        {
            Silver = 0,
            Gold = 1,
            Premium = 2
        }
        
        class CardConfig
        {
            public string Name;
            public int SecondsToUpgrade;
            public string CostItemShortname;
            public int CostItemAmount;
            public float AmountPercent;
            public float TimePercent;
            public int MaxItemAmount;
        }

        static Dictionary<CardType, CardConfig> Cards = new Dictionary<CardType, CardConfig>()
        {
            [CardType.Silver] = new CardConfig()
            {
                Name = "Silver",
                SecondsToUpgrade = 3 * Hour,
                AmountPercent = 15f,
                CostItemShortname = "sulfur",
                CostItemAmount = 2000,
                MaxItemAmount = 5000,
                TimePercent = 8 * Hour,
            },
            [CardType.Gold] = new CardConfig()
            {
                Name = "Gold",
                SecondsToUpgrade = 6 * Hour,
                AmountPercent = 25f,
                CostItemShortname = "sulfur",
                CostItemAmount = 5000,
                MaxItemAmount = 10000,
                TimePercent = 8 * Hour,
            },
            [CardType.Premium] = new CardConfig()
            {
                Name = "Platinum",
                SecondsToUpgrade = 10 * Hour,
                AmountPercent = 50f,
                CostItemShortname = "sulfur",
                CostItemAmount = 10000,
                MaxItemAmount = 15000,
                TimePercent = 8 * Hour,
            },
        };

        static CardConfig GetCardConfig(CardType type)
        {
            return Cards[type];
        }

        #endregion
        #region [Section] Hooks

        void OnServerInitialized()
        {
            UploadImages();
            LoadData();
            timer.Every(1f, TimeTracker_Tick);
            BasePlayer.activePlayerList.ForEach(ShowGUI_Icon);
        }

        void Unload()
        {
            SaveData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity && entity.net.ID == Data.NPCID)
            {
                return true;
            }
            return null;
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.Once(0.1f, () => OnPlayerInit(player));
                return;
            }
            
            ShowGUI_Icon(player);
        }

        #endregion

        #region [Section] Core

        void TimeTracker_Tick()
        {
            BasePlayer.activePlayerList.ForEach((player) =>
            {
                GetData(player).TimeTracker_Tick(player);
            });
        }

        #region [Method] SpawnNPC

        [ChatCommand("spawnnpc")]
        void SpawnNPC(BasePlayer player)
        {
            if (player.IsAdmin == false)
            {
                return;
            }
            
            if (Data.NPCID > 0)
            {
                var ent = BaseNetworkable.serverEntities.Find(Data.NPCID);
                if (ent && ent.IsDestroyed == false)
                {
                    ent.Kill();
                    player.ChatMessage("<color=#FF0000>Банк:</color> <color=#00FFFF>Предыдущий NPC уничтожен!</color>");
                }
            }
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit))
            {
                var ent = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", hit.point);
                ent.Spawn();
                Data.NPCID = ent.net.ID;
                Data.NPCPosition = ent.transform.position;
                player.ChatMessage("<color=#FF0000>Банк:</color> <color=#00FFFF>NPC успешно установлен!</color>");
            }
        }
        #endregion
        
        #region [Methods] BoxOpenning

        Dictionary<SteamID, BBankBox> boxesDB = new Dictionary<ulong, BBankBox>();
        
        void OpenLoot(BasePlayer player)
        {
            var box = BBankBox.Spawn(player, "Банковская ячейка (Перетащите сюда свой ресурс)");
            boxesDB[player.userID] = box;
            var pData = GetData(player);
            if (pData.Amount > 0 && string.IsNullOrEmpty(pData.Shortname).Equals(false))
            {
                box.Push(new List<Item>()
                {
                    ItemManager.CreateByPartialName(pData.Shortname, pData.GetAmountWithBonus())
                });
            }

            box.StartLoot();
        }

        void OnSaveLoot(BBankBox box, BasePlayer player)
        {
            var pData = GetData(player);
            
            var items = box.GetItems;
            if (items.Count > 0)
            {
                var item = items[0];
                pData.OnItemChanged(player, item);
            }
            else
            {
                pData.Amount = 0;
                pData.Shortname = "";
            }
            box.ClearItems();
            box.Close();
        }

        #endregion

        #region [Method] IsNPCNear

        bool IsNPCNear(BasePlayer player)
        {
            return Vector3.Distance(player.transform.position, Data.NPCPosition) < NPC_Radius;
        }
        
        #endregion
        
        #endregion

        #region [Section] Commands

        [ConsoleCommand("bbank.menu")]
        void cmdMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (IsNPCNear(player))
            {
                ShowGUI_Menu(player, GetData(player));
                return;
            }
            
            player.ChatMessage("<color=#FF0000>Вам нужно добраться до банкира!</color>");
        }
        
        [ConsoleCommand("bbank.open")]
        void cmdOpen(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (IsNPCNear(player))
            {
                DestroyGUI_Menu(player);
                OpenLoot(player);
                return;
            }
            
            player.ChatMessage("<color=#FF0000>Вам нужно добраться до банкира!</color>");
        }
        [ConsoleCommand("bbank.upgrade")]
        void cmdUpgrade(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (IsNPCNear(player))
            {
                if (GetData(player).CardUpgrade(player))
                {
                    ShowGUI_Menu(player, GetData(player));
                }
                return;
            }
            
            player.ChatMessage("<color=#FF0000>Вам нужно добраться до банкира!</color>");
        }
        [ConsoleCommand("bbank.addtime")]
        void cmdAddTime(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            
            if (player.IsAdmin == false)
                return;

            GetData(player).PlayingSeconds += 2 * Hour;
            GetData(player).DepositSeconds += 2 * Hour;
            player.ChatMessage("<color=#FF0000>Депутат:</color> <color=#00FFFF>Вам начислено 2 часа игры!</color>");
        }
        #endregion

        #region [Section] Data
        
        BBankData Data;

        class BBankData
        {
            public Dictionary<SteamID, BankAccountData> Accounts;
            public HashSet<string> CardNumberList;
            
            public Vector3 NPCPosition = new Vector3(0,0,0);
            public uint NPCID = 0;
            
            public BBankData()
            {
                Accounts = new Dictionary<ulong, BankAccountData>();
                CardNumberList = new HashSet<string>();
            }
        }
        
        class BankAccountData
        {
            public SteamID UserID;
            public string CardNumber;
            public string Shortname = "";

            public CardType CardType = 0;
            public int Amount = 0;
            public int PlayingSeconds = 0;
            public int DepositSeconds = 0;

            public bool NewCardAvailable = false;

            public CardConfig CardConfig => GetCardConfig(CardType);
            public bool IsLastCard => CardCount == (int) CardType + 1;

            public BankAccountData(SteamID steamid, string cardNumber)
            {
                this.UserID = steamid;
                this.CardNumber = cardNumber;
            }

            public int GetAmountWithBonus()
            {
                return Amount * (1 + Mathf.FloorToInt((float) DepositSeconds / CardConfig.TimePercent));
            }
            
            public int GetNextBonus()
            {
                return Amount;
            }

            public int GetNextBonusSeconds()
            {
                float time = DepositSeconds;
                while (time > CardConfig.TimePercent)
                {
                    time -= CardConfig.TimePercent;
                }

                return (int)(CardConfig.TimePercent - time);
            }

            public void TimeTracker_Tick(BasePlayer player)
            {
                PlayingSeconds++;
                DepositSeconds++;
                if (NewCardAvailable == false && IsLastCard == false && GetCardConfig(CardType + 1).SecondsToUpgrade < PlayingSeconds)
                {
                    player.ChatMessage("<color=#FF0000>Банк:</color> <color=#00FFFF>Доступна новая карта</color>");
                    NewCardAvailable = true;
                }
            }

            public bool CardUpgrade(BasePlayer player)
            {
                if (IsLastCard)
                {
                    player.ChatMessage($"<color=#FF0000>Банк:</color> <color=#00FFFF>У вас и так самая лучшая карта!</color>");
                    return false;
                }
                var newCardConfig = GetCardConfig(CardType + 1);
                
                if (NewCardAvailable == false)
                {
                    var remainTime = newCardConfig.SecondsToUpgrade - PlayingSeconds;
                    player.ChatMessage($"<color=#FF0000>Банк:</color> <color=#00FFFF>Улучшение карты будет доступно через {FormatTime(TimeSpan.FromSeconds(remainTime), 2)}</color>");
                    return false;
                }
                var costitem = ItemManager.FindItemDefinition(newCardConfig.CostItemShortname);
                var itemid = costitem.itemid;
                var remain = newCardConfig.CostItemAmount - player.inventory.GetAmount(itemid); 
                if (remain > 0)
                {
                    player.ChatMessage($"<color=#FF0000>Банк: Нехватает {remain} {costitem.shortname}!</color>");
                    return false;
                }

                List<Item> collectedItems = new List<Item>();
                player.inventory.Take(collectedItems, itemid, newCardConfig.CostItemAmount);
                collectedItems.ForEach(i=>i.Remove());

                player.ChatMessage($"<color=#FF0000>Банк:</color> <color=#00FFFF>Ваша карта успешно улучшена до {newCardConfig.Name}</color>");
                
                CardType++;
                NewCardAvailable = false;
                return true;
            }

            public void OnItemChanged(BasePlayer player, Item item)
            {
                if (item.info.shortname != Shortname || item.amount != GetAmountWithBonus())
                {
                    if (item.amount > CardConfig.MaxItemAmount)
                    {
                        var retAmount = item.amount - CardConfig.MaxItemAmount;
                        player.inventory.GiveItem(item.SplitItem(retAmount));
                        player.ChatMessage($"<color=#FF0000>Банк:</color> <color=#00FFFF>Ресурсы не вместились!\n </color>" +
                                           $"<color=#FF0000>Банк:</color> <color=#00FFFF>Макс. вместимость {CardConfig.Name} карты - {CardConfig.MaxItemAmount}\n </color>" +
                                           $"<color=#FF0000>Банк:</color> <color=#00FFFF>Возвращено {retAmount} {item.info.shortname} </color>");
                    }

                    Amount = item.amount;
                    Shortname = item.info.shortname;
                    DepositSeconds = 0;
                    item.Remove(0);
                }
            }
        }

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("BBank.Data") == false)
                Interface.Oxide.DataFileSystem.WriteObject("BBank.Data", new BBankData());
            Data = Interface.Oxide.DataFileSystem.ReadObject<BBankData>("BBank.Data");
            if (Data == null)
                Data = new BBankData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BBank.Data", Data);
        }

        BankAccountData GetData(BasePlayer player)
        {
            BankAccountData account;
            if (Data.Accounts.TryGetValue(player.userID, out account))
                return account;

            string cardNum;
            do
            {
                cardNum = CardNumber();
            } while (Data.CardNumberList.Contains(cardNum));
            
            return Data.Accounts[player.userID] = new BankAccountData(player.userID, cardNum);
        }
        
        #endregion

        #region [Section] Helpers

        #region [Class] StorageBox
        public class BBankBox : MonoBehaviour
        {

            LootableCorpse corpse;
            BasePlayer owner;

            public void Init( LootableCorpse storage, BasePlayer owner )
            {
                this.corpse = storage;
                this.owner = owner;
            }

            public static BBankBox Spawn( BasePlayer player, string name, int size = 1)
            {
                player.EndLooting();
                var storage = SpawnContainer( player, size, name );
                var box = storage.gameObject.AddComponent<BBankBox>();
                box.Init( storage, player );
                return box;
            } 

            static int rayColl = LayerMask.GetMask( "Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building" );

            public static LootableCorpse SpawnContainer( BasePlayer player, int size, string name )
            {
                var entity = GameManager.server.CreateEntity("assets/prefabs/player/player_corpse.prefab") as BaseCorpse;
                if (entity == null) return null;
                entity.parentEnt = null;
                entity.transform.position = new Vector3(player.transform.position.x, -300, player.transform.position.z);
                entity.CancelInvoke(nameof(BaseCorpse.RemoveCorpse));

                var corpse = entity as LootableCorpse;
                if (corpse == null) return null;

                ItemContainer container = new ItemContainer { playerOwner = player };
                container.ServerInitialize(null, size);
                if ((int)container.uid == 0)
                    container.GiveUID();

                corpse.containers = new ItemContainer[1];
                corpse.containers[0] = container;
                corpse.containers[0].playerOwner = player;

                corpse.playerName = name;
                corpse.lootPanelName = "generic";
                corpse.playerSteamID = 0;
                corpse.enableSaving = false;

                corpse.Spawn();
                corpse.GetComponentInChildren<Rigidbody>().useGravity = false;
                return corpse;
            }

            private void PlayerStoppedLooting( BasePlayer player )
            {
                Interface.Oxide.RootPluginManager.GetPlugin( "BBank" ).Call( "OnSaveLoot",  this, player);
            }

            public void Close()
            {
                for (var i = corpse.children.Count - 1; i >= 0; i--)
                {
                    corpse.children[i].Kill();
                }
                ClearItems();

                // bypass ItemContainer.Drop
                corpse.containers = null;
                corpse.Kill(); 
                if (this)
                UnityEngine.Object.Destroy(this);
            }

            public void StartLoot()
            {
                var panel = corpse.lootPanelName;
                corpse.lootPanelName = "generic";
                corpse.SetFlag(BaseEntity.Flags.Open, true, false);
                owner.inventory.loot.StartLootingEntity(corpse, false);
                owner.inventory.loot.AddContainer(corpse.containers[0]);
                owner.inventory.loot.SendImmediate();
                owner.ClientRPCPlayer(null, owner, "RPC_OpenLootPanel", "generic");
                corpse.SendNetworkUpdate();
            }

            public void Push( List<Item> items )
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    items[ i ].MoveToContainer( corpse.containers[0] );
            }

            public void ClearItems()
            {
                for (var i = corpse.containers[0].itemList.Count - 1; i >= 0; i--)
                {
                    corpse.containers[0].itemList[i].Remove();
                }
            }

            public List<Item> GetItems => corpse.containers[0].itemList.Where( i => i != null ).ToList();

        }
        #endregion

        #region [Random]

        public static string CardNumber()
        {
            return $"{RandomNum(4, false)} {RandomNum(4, true)} {RandomNum(4, true)} {RandomNum(4, true)}";
        }
        public static string RandomNum(int length, bool withZero)
        {
            string chars = "123456789";
            if (withZero)
                chars += "0";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[UnityEngine.Random.Range(0,s.Length-1)]).ToArray());
        }

        #endregion

        #region [Method] FormatTime
        public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru", bool @short = false)
        {
            string result = string.Empty;
            switch (language)
            {
                case "ru":
                    int i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";
                        
                        result += (@short ? $"{time.Days}дн." : Format(time.Days, "дней", "дня", "день"));
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += (@short ? $"{time.Hours}ч." : Format(time.Hours, "часов", "часа", "час"));
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += (@short ? $"{time.Minutes}м." : Format(time.Minutes, "минут", "минуты", "минута" ));
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += (@short ? $"{time.Seconds}c." : Format(time.Seconds, "секунд", "секунды", "секунда"));
                        i++;
                    }

                        break;
                case "en":
                    result = string.Format( "{0}{1}{2}{3}",
                        time.Duration().Days > 0 ? $"{time.Days:0} day{( time.Days == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Hours > 0 ? $"{time.Hours:0} hour{( time.Hours == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{( time.Minutes == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{( time.Seconds == 1 ? String.Empty : "s" )}" : string.Empty );

                    if (result.EndsWith( ", " )) result = result.Substring( 0, result.Length - 2 );

                    if (string.IsNullOrEmpty( result )) result = "0 seconds";
                    break;
            }
            return result;
        }
        
        private static string Format( int units, string form1, string form2, string form3 )
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        #endregion

        #endregion

        #region [Section] GUI

        private const string GUI_MENU = "[{\"name\":\"bbank_gui\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"bbank_gui\",\"color\":\"1 1 1 0\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_panel\",\"parent\":\"bbank_gui\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-128 -128\",\"offsetmax\":\"128 128\"}]},{\"name\":\"bbank_money\",\"parent\":\"bbank_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{money}\",\"fontSize\":24,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2265625 0.4843751\",\"anchormax\":\"0.9817706 0.6223959\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_cardnum\",\"parent\":\"bbank_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{cardnum}\",\"fontSize\":15,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2109373 0.333334\",\"anchormax\":\"0.7760416 0.4713562\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_username\",\"parent\":\"bbank_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{username}\",\"fontSize\":18,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2109373 0.1927092\",\"anchormax\":\"0.7760416 0.3307311\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_upgrade\",\"parent\":\"bbank_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.03645841 0.06770828\",\"anchormax\":\"0.393229 0.1927082\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_upgrade_text\",\"parent\":\"bbank_upgrade\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Улучшить\",\"fontSize\":18,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"0.5019608 0.7294118 0.4039216 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.7921569 0.01960784 0.01960784 1\",\"distance\":\"0.4 -0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_upgrad_btn\",\"parent\":\"bbank_upgrade\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"bbank.upgrade\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_open\",\"parent\":\"bbank_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5989584 0.06770828\",\"anchormax\":\"0.9557267 0.1927082\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_open_text\",\"parent\":\"bbank_open\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Открыть\",\"fontSize\":18,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"0.6202191 0.5499281 0.5499281 1\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.3583119 0 0 1\",\"distance\":\"0.4 -0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"bbank_open_btn\",\"parent\":\"bbank_open\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"bbank.open\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";
        private const string GUI_ICON = "[{\"name\":\"bbank_icon\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"$\",\"fontSize\":24,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 1\",\"anchormax\":\"0 1\",\"offsetmin\":\"0 -80\",\"offsetmax\":\"60 -40\"}]},{\"name\":\"bbank_icon_btn\",\"parent\":\"bbank_icon\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"bbank.menu\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        void ShowGUI_Icon(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "bbank_icon");
            CuiHelper.AddUi(player, GUI_ICON);
        }
        void ShowGUI_Menu(BasePlayer player, BankAccountData accountData)
        {
            CuiHelper.DestroyUi(player, "bbank_gui");
            CuiHelper.AddUi(player, GUI_MENU
                .Replace("{png}", (string)ImageLibrary.Call("GetImage", ImageData.Keys.ElementAt((int)accountData.CardType)))
                .Replace("{cardnum}", accountData.CardNumber)
                .Replace("{money}", $"{accountData.GetAmountWithBonus()} + <size=12>{accountData.GetNextBonus()}({FormatTime(TimeSpan.FromSeconds(accountData.GetNextBonusSeconds()), 2, @short: true)})</size>")
                .Replace("{username}", player.displayName));
        }

        void DestroyGUI_Menu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "bbank_gui");
        }

        #endregion


        #region [Methods] ImageLibrary
        
        [PluginReference] private Plugin ImageLibrary;

        private static Dictionary<string, string> ImageData = new Dictionary<string, string>()
        {
            ["bbank.card.silver"]   = "https://i.imgur.com/sqrINPa.png",
            ["bbank.card.gold"]     = "https://i.imgur.com/f2WzVDK.png",
            ["bbank.card.platinum"] = "https://i.imgur.com/kjd2T6H.png",
        };
        
        private int LoadedImages = 0;

        private bool IsLoadedImages()
        {
            return LoadedImages == ImageData.Count;
        }
        
        void UploadImages()
        {
			if (!ImageLibrary)
            {
                PrintError("ImageLibrary not found! Download from Umod.org");
                return;
            }
            foreach (var image in ImageData)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key, (ulong)0, (Action)(() => LoadedImages++ ));
            }
        }

        #endregion
    }
}