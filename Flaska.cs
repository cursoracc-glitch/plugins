using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Flaska", "TopPlugin.ru", "1.0.0")]
    class Flaska : RustPlugin
    {
        #region Хуки
        void OnServerInitialized()
        {
            MutationRegistered();
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            UseTools(player, item);
            return null;
        }
        #endregion

        #region Команда
        [ConsoleCommand("flask_give")]
        void UraniumToolGive(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && !player.IsAdmin) return;
            BasePlayer target = BasePlayer.Find(args.Args[0]);
            CreateItem(target);
        }
        #endregion

        #region Mutations
        Dictionary<ItemDefinition, ItemDefinition> Transmutations;
        List<string> MutationItemList = new List<string>
        {
            "chicken.raw",
            "humanmeat.raw",
            "bearmeat",
            "deermeat.raw",
            "meat.boar",
            "wolfmeat.raw",
            "hq.metal.ore",
            "metal.ore",
            "sulfur.ore"
        };

        void MutationRegistered()
        {
            Transmutations = ItemManager.GetItemDefinitions().Where(p => MutationItemList.Contains(p.shortname)).ToDictionary(p => p, p => p.GetComponent<ItemModCookable>()?.becomeOnCooked);
            ItemDefinition wood = ItemManager.FindItemDefinition(-151838493);
            ItemDefinition charcoal = ItemManager.FindItemDefinition(-1938052175);
            Transmutations.Add(wood, charcoal);
        }
        #endregion

        #region Методы
        void UseTools(BasePlayer player, Item item)
        {
            foreach (var check in player.inventory.containerWear.itemList)
            {
                if (check.skin == 1552162306)
                {
                    if (Transmutations.ContainsKey(item.info))
                        item.info = Transmutations[item.info];
                }
            }
        }

        void CreateItem(BasePlayer player)
        {
            Item item = ItemManager.CreateByName("tactical.gloves", 1, 1552162306);
            item.name = "Волшебная фласка";
            player.GiveItem(item);
        }
        #endregion
    }
}