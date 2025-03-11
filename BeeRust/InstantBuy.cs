
namespace Oxide.Plugins
{
    [Info("Instant Buy", "Jake_Rich/collect_vood/Bushhy", "1.0.3")]
    [Description("Vending Machine has no delay")]

    public class InstantBuy : CovalencePlugin
    {
        #region Constants

        private const string permUse = "instantbuy.use";

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
        }

        private object OnBuyVendingItem(VendingMachine machine, BasePlayer player, int sellOrderID, int amount)
        {
            if (machine == null || player == null) return null;

            if (!permission.UserHasPermission(player.UserIDString, permUse)) return null;
			
			if (player.inventory.containerMain.IsFull() && player.inventory.containerBelt.IsFull()) return null;

            machine.ClientRPC<int>(null, "CLIENT_StartVendingSounds", sellOrderID);
            machine.DoTransaction(player, sellOrderID, amount);
            return false;
        }

        #endregion
    }
}