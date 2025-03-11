using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("SortItems", "Own3r", "1.0.1")]
    class SortItems : RustPlugin
    {
        #region CONFIGURATION

        #endregion

        #region FIELDS

        const string permAccess = "sortitems.access";
        List<string> toggleCaptions = new List<string>() { "<-", "->" };
        List<ulong> putPlayers = new List<ulong>();
        #endregion


        #region COMMANDS

        [ConsoleCommand("sortitems.toggle")]
        void cmdToggle(ConsoleSystem.Arg arg)
        {

            if (arg.Connection == null) return;
            var player = arg.Player();
            if (InDuel(player))
            {
                DestroyUI(player);
                return;
            }
            var userId = player.userID;

            if (putPlayers.Contains(userId))
            {
                putPlayers.Remove(userId);
                DestroyUI(player);
                DrawUI(player);
            }
            else
            {
                putPlayers.Add(userId);
                DestroyUI(player);
                DrawUI(player);
            }
        }

        [ConsoleCommand("sortitems.sort")]
        void cmdSort(ConsoleSystem.Arg arg)
        {

            if (arg.Connection == null) return;
            var player = arg.Player();
            if (InDuel(player))
            {
                DestroyUI(player);
                return;
            }
            var category = arg.GetInt(0);

            var lootContainer = player.inventory.loot?.containers?.Count > 0 ? player.inventory.loot?.containers[0] : null;
            if (lootContainer == null) return;
            var playerContainer = player.inventory.containerMain;

            if (lootContainer == null || playerContainer == null)
            {
                DestroyUI(player);
                return;
            }

            var inputContainer = putPlayers.Contains(player.userID) ? playerContainer : lootContainer;
            var outputContainer = inputContainer == lootContainer ? playerContainer : lootContainer;

            GetItemsByCategory(inputContainer, category).ForEach(item => item.MoveToContainer(outputContainer));
        }

        #endregion

        #region OXIDE HOOKS
        [PluginReference]
        Plugin Duel;

        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }
        void OnPlayerSleepEnded(PlayerLoot inventory, BasePlayer player)
        {
            if (InDuel(player))
            {
                player.EndLooting();
                DestroyUI(player);
                return;
            }
           
           
            var invectory = inventory.GetComponent<BasePlayer>();
            if (invectory == null)
            {
                DestroyUI(invectory);
            }
        }


        void OnPlayerInit(BasePlayer player)
        {
            DestroyUI(player);
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permAccess, this);
        }


        void OnLootEntity(BasePlayer player, BaseEntity entry)
        {
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, permAccess))
            {
                if (entry is StorageContainer)
                {
                    StorageContainer box = entry as StorageContainer;
                    if (!(box.panelName == "largewoodbox" || box.panelName == "smallwoodbox"
                          || box.panelName == "fuelstorage" || box.panelName == "smallstash"
                          || box.name.Contains("hopperoutput")
                          || box.prefabID == 349880778))
                        return;
                }
                DrawUI(player);
            }
        }


        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            var player = inventory.GetComponent<BasePlayer>();
            if (player != null)
            {
                Puts("1");
                DestroyUI(player);
                return;
            }
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, permAccess))
                DestroyUI(player);
        }

        #endregion

        #region CORE

        List<Item> GetItemsByCategory(ItemContainer container, int category)
        {
            List<ItemCategory> categories = new List<ItemCategory>();
            switch (category)
            {
                case 0:
                    categories.Add(ItemCategory.Resources);
                    break;
                case 1:
                    categories.Add(ItemCategory.Weapon);
                    break;
                case 2:
                    categories.Add(ItemCategory.Ammunition);
                    break;
                case 3:
                    categories.Add(ItemCategory.Medical);
                    break;
                case 4:
                    categories.Add(ItemCategory.Attire);
                    break;
                case 5:
                    categories.Add(ItemCategory.Component);
                    break;
                case 6:
                    categories.Add(ItemCategory.Tool);
                    break;
                case 7:
                    categories.Add(ItemCategory.Construction);
                    categories.Add(ItemCategory.Items);
                    categories.Add(ItemCategory.Traps);
                    categories.Add(ItemCategory.Misc);
                    categories.Add(ItemCategory.Common);
                    categories.Add(ItemCategory.Search);
                    break;
                case 8:
                    for (int i = 0; i < 15; i++)
                        categories.Add((ItemCategory)i);
                    break;
            }
            return container.itemList.Where(item => item != null && categories.Contains(item.info.category)).ToList();
        }

        bool HasAccess(BasePlayer player) => player.IsAdmin;

        #endregion

        #region UI
        string HandleArgs(string json, params object[] args)
        {
            var reply = 431;
            for (int i = 0; i < args.Length; i++)
                json = json.Replace("{" + i + "}", args[i].ToString());
            return json;
        }

        string GUI = "[{\"name\":\"sortitems_panel\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.655 0.6308895 0.6308895 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6505206 0.02222223\",\"anchormax\":\"0.8294269 0.1361113\"}]},{\"name\":\"sortitems_direction\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.toggle\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.1850519 0.9674798\"}]},{\"parent\":\"sortitems_direction\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{0}\",\"fontSize\":35,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_res\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 0\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.195051 0\",\"anchormax\":\"0.4570595 0.3333329\"}]},{\"parent\":\"sortitems_res\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Ресурсы\",\"fontSize\":13,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_weapons\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 1\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1950507 0.3658533\",\"anchormax\":\"0.4556037 0.6207555\"}]},{\"parent\":\"sortitems_weapons\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Оружие\",\"fontSize\":11,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_ammo\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 2\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7161568 0\",\"anchormax\":\"0.9981656 0.3333328\"}]},{\"parent\":\"sortitems_ammo\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Боеприпасы\",\"fontSize\":11,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_medical\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 3\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4669705 0.6422769\",\"anchormax\":\"0.7063342 0.9674798\"}]},{\"parent\":\"sortitems_medical\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Медицина\",\"fontSize\":11,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_tool\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 6\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7161568 0.6422771\",\"anchormax\":\"0.9981656 0.9674798\"}]},{\"parent\":\"sortitems_tool\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Инструменты\",\"fontSize\":10,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_other\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 7\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7161568 0.3658533\",\"anchormax\":\"0.9981656 0.6207555\"}]},{\"parent\":\"sortitems_other\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Остальное\",\"fontSize\":11,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_components\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 5\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4669705 0\",\"anchormax\":\"0.7063342 0.3333328\"}]},{\"parent\":\"sortitems_components\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Компоненты\",\"fontSize\":10,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_all\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 8\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1950509 0.6422771\",\"anchormax\":\"0.4556037 0.9674798\"}]},{\"parent\":\"sortitems_all\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Всё\",\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]},{\"name\":\"sortitems_altire\",\"parent\":\"sortitems_panel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"sortitems.sort 4\",\"color\":\"0.45 0.52 0.26 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4669705 0.3659533\",\"anchormax\":\"0.7063342 0.6207555\"}]},{\"parent\":\"sortitems_altire\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Одежда\",\"fontSize\":11,\"align\":\"MiddleCenter\",\"color\":\"0.67 0.76 0.50 1.00\"},{\"type\":\"RectTransform\"}]}]";

        void DrawUI(BasePlayer player)
        {

            CuiHelper.AddUi(player, HandleArgs(GUI, toggleCaptions[putPlayers.Contains(player.userID) ? 1 : 0]));
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "sortitems_panel");
        }

        #endregion
    }
}