using Random = UnityEngine.Random;
namespace Oxide.Plugins
{
    [Info("Forest Berries", "0xPani", "1.0.0")]
    class ForestBerries : RustPlugin
    {
		private float chance = 20f; //Шанс выпадения ягод при сборе грибов лол
		
		object OnCollectiblePickup(Item item, BasePlayer player)
		{
			if (item.info.shortname.Contains("mushroom"))
			{
				if (UnityEngine.Random.Range(0f, 100f) < chance)
				{
					var amount = Random.Range(1, 3); //Рандомное количество ягод за один сбор (от 1 до 3)
					Item berry = ItemManager.CreateByItemID(-586342290, amount);
					player.inventory.GiveItem(berry);
					string msg = amount == 1 ? "Вы нашли ягодки" : "Вы нашли ягодки";
					PrintToChat(player, msg);
				}
			}
			return null;
		}
    }
}
