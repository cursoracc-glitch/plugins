//---------------------Используем исходники----------------------
using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using System.Reflection;
using Oxide.Core;
using System.Linq;
using Oxide.Game.Rust.Cui;
//---------------------------------------------------------------
namespace Oxide.Plugins
{
    [Info("HCraft", "Hougan", "1.0", ResourceId = 1855)]
    public class HCraft : RustPlugin
    {
        Dictionary<ulong, int> Craft = new Dictionary<ulong, int>();
        Dictionary<ulong, bool> Pay = new Dictionary<ulong, bool>();
        void Loaded()
        {
            permission.RegisterPermission("HCraft.User", this);
            foreach(var tried in BasePlayer.activePlayerList)
            {
                Pay.Add(tried.userID, false);
                Craft.Add(tried.userID, 0);
            }
        }
        void OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            task.cancelled = true;
            Craft[crafter.userID] = task.amount;
            while (Craft[crafter.userID]!=0)
            {
                if(30 - crafter.inventory.containerMain.itemList.Count - crafter.inventory.containerBelt.itemList.Count <= 0)
                {
                    var i = task.takenItems.Count;
                    if (Pay[crafter.userID] == true)
                    {
                        SendReply(crafter, "[<color=#DC143C>RM</color>] Ваш инвентарь <color=#DC143C>переполнен</color>!\n<size=12><color=#D3D3D3>(Ресурсы <color=#DC143C>не возвращены</color>, вы были <color=#DC143C>предупреждены</color>!)</color></size>");
                        Craft[crafter.userID]=0;
                        return;
                    }
                    for (int b = 0; b<i; b++)
                        ItemManager.CreateByItemID(Convert.ToInt32(task.takenItems[b].info.itemid), task.takenItems[b].amount/task.amount* Craft[crafter.userID]).Drop(crafter.transform.position, Vector3.zero);
                    SendReply(crafter, "[<color=#DC143C>RM</color>] Ваш инвентарь <color=#DC143C>переполнен</color>!\n<size=12><color=#D3D3D3>(В течении <color=#DC143C>5-и</color> секунд ресурсы возвращаться <color=#DC143C>не будут</color>!)</color></size>");
                    Pay[crafter.userID] = true;
                    Craft[crafter.userID] = 0;
                    timer.Once(5, () =>
                    {
                        Pay[crafter.userID] = false;
                    });
                }
                else
                {
                    crafter.inventory.GiveItem(ItemManager.CreateByItemID(task.blueprint.targetItem.itemid, task.blueprint.amountToCreate));
                    Craft[crafter.userID]--;
                }
            }
            Craft[crafter.userID] = 0;
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            Craft.Remove(player.userID);
            Pay.Remove(player.userID);
        }
    }
}
