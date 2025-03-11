using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Scrap Vending Exchange", "Sempai#3239", "1.0.5")]
    [Description("Vending Exchange, exchange scrap for custom currency via any npc vending machine.")]
    public class ScrapVendingExchange : RustPlugin
    {
        #region Fields
        
        private const string PERM_USE = "scrapvendingexchange.use";

        private const string GRANTED_PREFAB = "assets/prefabs/deployable/research table/effects/research-success.prefab";
        private const string DENIED_PREFAB = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";

        private Effect _effectInstance = new Effect();
        private PluginConfig _config;

        #endregion

        #region Config

        protected override void LoadDefaultConfig() => _config = PluginConfig.DefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    throw new JsonException();
                }

                if (_config.ToDictionary().Keys
                    .SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)) return;

                PrintWarning("Config has been updated.");

                SaveConfig();
            }
            catch
            {
                PrintWarning("Default config loaded.");

                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private class PluginConfig
        {
            [JsonProperty("Item to sell item id")]
            public int SellItemId;
            
            [JsonProperty("Item to sell skin id")]
            public ulong SellSkinId;
            
            [JsonProperty("Min amount to be exchanged")]
            public int MinAmount;

            [JsonProperty("Max amount to be exchanged")]
            public int MaxAmount;

            [JsonProperty("Exchange rate (default payout 80% percent)")]
            public double ExchangeRate;

            [JsonProperty("Deposit command (default Economics arguments {userid} {amount})")]
            public string DepositCommand;

            [JsonProperty("Allowed Machines (allowed prefab names, leave empty for all)")]
            public string[] AllowedMachines = {};

            [JsonProperty("Scrap Ui Position (sets position of the scrap vending ui)")]
            public string[] ScrapUiPosition = { "0.68 0.85", "0.87 0.998" };

            [JsonProperty("Popup Ui Position (sets position of the popup vending ui)")]
            public string[] PopupUiPosition = { "0.085 0.945", "0.905 1" };

            public string ToJson()
                => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary()
                => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    SellItemId = -932201673,
                    SellSkinId = 0UL,
                    MinAmount = 0,
                    MaxAmount = 1000,
                    ExchangeRate = 80.00,
                    DepositCommand = "deposit {userid} {amount}",
                    ScrapUiPosition = new []{ "0.68 0.85", "0.87 0.998" },
                    PopupUiPosition = new []{ "0.085 0.945", "0.905 1" },
                    AllowedMachines = {}
                };
            }
        }

        #endregion
        
        #region Lang

        private const string MESSAGE_INVALID = "MessageInvalid";
        private const string MESSAGE_ENOUGH = "MessageEnough";
        private const string MESSAGE_TITLE = "MessageTitle";
        private const string MESSAGE_AMOUNT = "MessageAmount";
        private const string MESSAGE_EXCHANGED = "MessageExchanged";
        private const string BUTTON_EXCHANGE = "ButtonExchange";
        private const string BUTTON_CLEAR = "ButtonClear";
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {MESSAGE_INVALID, "Please provide a valid amount of scrap to exchange."},
                {MESSAGE_ENOUGH, "You do not have enough scrap for this exchange."},
                {MESSAGE_EXCHANGED, "You successfully exchanged scrap for currency ${0}."},
                {MESSAGE_AMOUNT, "Scrap {0} = ${1}"},
                {MESSAGE_TITLE, "SCRAP EXCHANGE"},
                {BUTTON_EXCHANGE, "EXCHANGE"},
                {BUTTON_CLEAR, "CLEAR"}
            }, this);
        }

        private string GetMessage(string key, string userid = null, params object[] args)
            => string.Format(lang.GetMessage(key, this, userid), args);

        #endregion

        #region Oxide

        private void OnServerInitialized()
        {
            permission.RegisterPermission(PERM_USE, this);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, EXCHANGE_NAME);
            }

            _effectInstance = null;
        }

        private void OnVendingShopOpened(NPCVendingMachine machine, BasePlayer player)
        {
            if (!HasPermission(player))
                return;

            if (!IsAllowedMachine(machine.PrefabName))
                return;

            CreateUI(player, 0, true);
        }

        private void OnLootEntityEnd(BasePlayer player, NPCVendingMachine machine)
        {
            RemoveUI(player);
        }

        #endregion

        #region CUI

        private const string MAIN_FONT_COLOR = "0.7 0.7 0.7 0.25";
        private const string GREEN_BUTTON_COLOR = "0.337 0.424 0.196 0.4";
        private const string GREEN_BUTTON_FONT_COLOR = "0.607 0.705 0.431";
        private const string GRAY_BUTTON_COLOR = "0.75 0.75 0.75 0.3";
        private const string GRAY_BUTTON_FONT_COLOR = "0.75 0.75 0.75 1";

        private const string INPUT_FONT_COLOR = "1 1 1 0.9";
        private const string INPUT_COLOR = "0 0 0 0.58";

        private const string POPUP_FONT_COLOR = "0 0 0 0.89";
        private const string POPUP_ERROR_COLOR = "0.631 0.282 0.22 0.98";
        private const string POPUP_SUCCESS_COLOR = "0.337 0.424 0.196 0.98";

        private const string EXCHANGE_NAME = "ExchangeUI";
        private const string EXCHANGE_POPUP_NAME = "ExchangeUIPopup";
        private const string EXCHANGE_HEADER_NAME = "ExchangeUIHeader";
        private const string EXCHANGE_CONTENT_NAME = "ExchangeUIContent";

        private void CreateUI(BasePlayer player, int amount = 0, bool isFirst = false)
        {
            CuiElementContainer container = new CuiElementContainer();

            #region First Pass

            if (isFirst)
            {
                CuiHelper.DestroyUi(player, EXCHANGE_NAME);

                container.Add(UiHelpers.Panel("0 0 0 0",
                    _config.ScrapUiPosition[0],
                    _config.ScrapUiPosition[1]), "Hud.Menu", EXCHANGE_NAME);

                container.Add(UiHelpers.Panel(GRAY_BUTTON_COLOR,
                    "0 0.775",
                    "1 1"), EXCHANGE_NAME, EXCHANGE_HEADER_NAME);

                container.Add(UiHelpers.Label(GRAY_BUTTON_FONT_COLOR,
                    "0.051 0",
                    "1 0.95",
                    GetMessage(MESSAGE_TITLE, player.UserIDString)), EXCHANGE_HEADER_NAME);
            }

            #endregion

            #region Content

            CuiHelper.DestroyUi(player, EXCHANGE_CONTENT_NAME);

            container.Add(UiHelpers.Panel("0.65 0.65 0.65 0.25",
                "0 0",
                "1 0.74"), EXCHANGE_NAME, EXCHANGE_CONTENT_NAME);

            #endregion

            #region Buttons

            container.Add(UiHelpers.Button(GREEN_BUTTON_COLOR, GREEN_BUTTON_FONT_COLOR,
                "0.02 0.42",
                "0.47 0.9",
                GetMessage(BUTTON_EXCHANGE, player.UserIDString),
                $"vendingscrapexchange.scrap {amount}"), EXCHANGE_CONTENT_NAME);

            container.Add(UiHelpers.Button(GRAY_BUTTON_COLOR, GRAY_BUTTON_FONT_COLOR,
                "0.49 0.42",
                "0.975 0.9",
                GetMessage(BUTTON_CLEAR, player.UserIDString),
                "vendingscrapexchange.set 0"), EXCHANGE_CONTENT_NAME);

            #endregion

            #region Amount

            container.Add(UiHelpers.Button(GRAY_BUTTON_COLOR, GRAY_BUTTON_FONT_COLOR,
                    "0.02 0.05",
                    "0.07 0.35",
                    "◀",
                    $"vendingscrapexchange.set {Mathf.Clamp(amount - 1, _config.MinAmount, _config.MaxAmount)}"),
                EXCHANGE_CONTENT_NAME);

            container.Add(UiHelpers.Panel(INPUT_COLOR,
                "0.08 0.05",
                "0.92 0.35"), EXCHANGE_CONTENT_NAME);

            container.Add(UiHelpers.Label(MAIN_FONT_COLOR,
                "0.08 0.05",
                "0.92 0.35",
                GetMessage(MESSAGE_AMOUNT, player.UserIDString, amount,
                    (amount / (double) 100 * _config.ExchangeRate))), EXCHANGE_CONTENT_NAME);

            container.Add(UiHelpers.Input(INPUT_FONT_COLOR,
                "0.07 0.05",
                "0.92 0.35",
                "vendingscrapexchange.set", EXCHANGE_CONTENT_NAME));

            container.Add(UiHelpers.Button(GRAY_BUTTON_COLOR, GRAY_BUTTON_FONT_COLOR,
                    "0.92 0.05",
                    "0.975 0.35",
                    "▶",
                    $"vendingscrapexchange.set {Mathf.Clamp(amount + 1, _config.MinAmount, _config.MaxAmount)}"),
                EXCHANGE_CONTENT_NAME);

            #endregion

            CuiHelper.AddUi(player, container);
        }

        private void RemoveUI(BasePlayer player) => CuiHelper.DestroyUi(player, EXCHANGE_NAME);

        private class UiHelpers
        {
            public static CuiPanel Panel(string panelColor, string anchorMin, string anchorMax)
            {
                return new CuiPanel
                {
                    Image = new CuiImageComponent {Color = panelColor},
                    RectTransform = {AnchorMin = anchorMin, AnchorMax = anchorMax}
                };
            }

            public static CuiLabel Label(string textColor, string anchorMin, string anchorMax, string text,
                TextAnchor textAnchor = TextAnchor.MiddleCenter)
            {
                return new CuiLabel
                {
                    RectTransform = {AnchorMin = anchorMin, AnchorMax = anchorMax},
                    Text = {Text = text, Align = textAnchor, Color = textColor, FontSize = 11}
                };
            }

            public static CuiElement Input(string textColor, string anchorMin, string anchorMax, string command,
                string parent, TextAnchor textAnchor = TextAnchor.MiddleCenter)
            {
                return new CuiElement
                {
                    Parent = parent,
                    FadeOut = 1f,
                    Components =
                    {
                        new CuiInputFieldComponent
                            {FontSize = 12, Align = textAnchor, Color = textColor, Command = command,},
                        new CuiRectTransformComponent {AnchorMin = anchorMin, AnchorMax = anchorMax},
                    }
                };
            }

            public static CuiButton Button(string color, string textColor, string anchorMin, string anchorMax,
                string text, string command, TextAnchor textAnchor = TextAnchor.MiddleCenter)
            {
                return new CuiButton
                {
                    RectTransform = {AnchorMin = anchorMin, AnchorMax = anchorMax},
                    Button = {Command = command, Color = color},
                    Text = {Align = textAnchor, Color = textColor, Text = text, FontSize = 11}
                };
            }

            public static void Popup(BasePlayer player, string message, string panelColor, string textColor, string anchorMin, string anchorMax)
            {
                CuiElementContainer container = new CuiElementContainer
                {
                    {Panel(panelColor, anchorMin, anchorMax), "Overlay", EXCHANGE_POPUP_NAME},
                    {Label(textColor, "0 0", "1 1", message), EXCHANGE_POPUP_NAME}
                };

                CuiHelper.DestroyUi(player, EXCHANGE_POPUP_NAME);
                CuiHelper.AddUi(player, container);

                player.Invoke(() => CuiHelper.DestroyUi(player, EXCHANGE_POPUP_NAME), 2f);
            }
        }

        #endregion
        
        #region Currency Command

        private void CurrencyCommand(BasePlayer player, double amount)
        {
            ConsoleSystem.Run(ConsoleSystem.Option.Server,
                _config.DepositCommand
                    .Replace("{userid}", player.UserIDString)
                    .Replace("{amount}", amount.ToString(CultureInfo.InvariantCulture)));
        }

        #endregion

        #region Inventory Methods | Needed To Check For Skinned Scrap

        private bool HasAmount(BasePlayer player, int amount)
            => GetAmount(player.inventory, _config.SellItemId, _config.SellSkinId) >= amount;

        private void TakeAmount(BasePlayer player, int amount)
            => TakeAmount(player.inventory, _config.SellItemId, _config.SellSkinId, amount);

        private int GetAmount(PlayerInventory inventory, int itemid, ulong skinID = 0UL)
        {
            if (itemid == 0)
                return 0;

            int num = 0;

            if (inventory.containerMain != null)
                num += GetAmount(inventory.containerMain, itemid, skinID, true);

            if (inventory.containerBelt != null)
                num += GetAmount(inventory.containerBelt, itemid, skinID, true);

            return num;
        }

        private int GetAmount(ItemContainer container, int itemid, ulong skinID = 0UL, bool usable = false)
        {
            int num = 0;

            foreach (Item obj in container.itemList)
            {
                if (obj.info.itemid == itemid && obj.skin == skinID && (!usable || !obj.IsBusy()))
                    num += obj.amount;
            }

            return num;
        }

        private int TakeAmount(PlayerInventory inventory, int itemid, ulong skinID, int amount)
        {
            int num1 = 0;

            if (inventory.containerMain != null)
            {
                int num2 = TakeAmount(inventory.containerMain, itemid, amount, skinID);
                num1 += num2;
                amount -= num2;
            }

            if (amount <= 0)
                return num1;

            if (inventory.containerBelt != null)
            {
                int num2 = TakeAmount(inventory.containerBelt, itemid, amount, skinID);
                num1 += num2;
            }

            return num1;
        }

        private int TakeAmount(ItemContainer container, int itemid, int amount, ulong skinID)
        {
            int num1 = 0;

            if (amount == 0) 
                return num1;

            List<Item> list = Facepunch.Pool.GetList<Item>();

            foreach (Item obj in container.itemList)
            {
                if (obj.info.itemid != itemid || obj.skin != skinID) continue;

                int num2 = amount - num1;

                if (num2 <= 0) continue;

                if (obj.amount > num2)
                {
                    obj.MarkDirty();
                    obj.amount -= num2;
                    num1 += num2;
                    Item byItemId = ItemManager.CreateByItemID(itemid);
                    byItemId.amount = num2;
                    byItemId.CollectedForCrafting(container.playerOwner);
                    break;
                }

                if (obj.amount <= num2)
                {
                    num1 += obj.amount;

                    list.Add(obj);
                }

                if (num1 == amount)
                    break;
            }

            list.ForEach(obj =>
            {
                if (obj == null) return;
                
                obj.RemoveFromContainer();
                obj.Remove();
            });
            
            ItemManager.DoRemoves();

            Facepunch.Pool.FreeList(ref list);

            return num1;
        }

        #endregion

        #region Commands

        [ConsoleCommand("vendingscrapexchange.set")]
        private void AmountCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player == null || !IsLootingMachine(player))
                return;
            
            if (!arg.HasArgs())
                return;

            CreateUI(player, arg.GetInt(0), false);
        }

        [ConsoleCommand("vendingscrapexchange.scrap")]
        private void ScrapCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player == null || !IsLootingMachine(player))
                return;

            if (!arg.HasArgs())
            {
                UiHelpers.Popup(player, GetMessage(MESSAGE_INVALID, player.UserIDString), POPUP_ERROR_COLOR,
                    POPUP_FONT_COLOR, _config.PopupUiPosition[0], _config.PopupUiPosition[1]);
                PlayEffect(player, DENIED_PREFAB);
                return;
            }

            int amount = arg.GetInt(0);

            if (amount <= 0 || !HasAmount(player, amount))
            {
                UiHelpers.Popup(player, GetMessage(MESSAGE_ENOUGH, player.UserIDString), POPUP_ERROR_COLOR,
                    POPUP_FONT_COLOR, _config.PopupUiPosition[0], _config.PopupUiPosition[1]);
                PlayEffect(player, DENIED_PREFAB);
                return;
            }

            TakeAmount(player, amount);

            double exchanged = (amount / (double) 100 * _config.ExchangeRate);

            CurrencyCommand(player, exchanged);

            UiHelpers.Popup(player, GetMessage(MESSAGE_EXCHANGED, player.UserIDString, exchanged), POPUP_SUCCESS_COLOR,
                GREEN_BUTTON_FONT_COLOR, _config.PopupUiPosition[0], _config.PopupUiPosition[1]);
            PlayEffect(player, GRANTED_PREFAB);

            CreateUI(player, 0, false);
        }

        #endregion
        
        #region Helpers

        private bool HasPermission(BasePlayer player)
            => permission.UserHasPermission(player.UserIDString, PERM_USE);

        private bool IsAllowedMachine(string prefab)
            => _config.AllowedMachines.Length == 0 || _config.AllowedMachines.Contains(prefab);

        private bool IsLootingMachine(BasePlayer player)
        {
            NPCVendingMachine machine = player.inventory.loot.entitySource as NPCVendingMachine;

            return machine != null && IsAllowedMachine(machine.PrefabName);
        }

        private void PlayEffect(BasePlayer player, string prefabPath)
        {
            if (player?.Connection == null)
                return;

            _effectInstance.Init(Effect.Type.Generic, player.ServerPosition, Vector3.zero);
            _effectInstance.pooledString = prefabPath;
            EffectNetwork.Send(_effectInstance, player.Connection);
            _effectInstance.Clear();
        }

        #endregion
    }
}