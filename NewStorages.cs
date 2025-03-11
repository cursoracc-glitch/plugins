using System.Collections.Generic;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NewStorages", "Ryamkk", "1.0.0")]
    public class NewStorages : RustPlugin
    {
        private void OnServerInitialized()
        {
            var list = new List<BasePlayer>();
            list.AddRange(BasePlayer.activePlayerList);
            list.AddRange(BasePlayer.sleepingPlayerList);
            
            foreach (var player in list.ToList())
            {
                CheckAllContainers(player.userID);
            }
        }
        
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            CheckEntity(entity);
        }

        private void UpgradeLargeBox(StorageContainer container)
        {
            container.inventory.capacity = 42;
            container.panelName = "genericlarge";
            container.SendNetworkUpdate();
        }
        
        private void UpgradeSmallBox(StorageContainer container)
        {
            container.inventory.capacity = 18;
            container.panelName = "largewoodbox";
            container.SendNetworkUpdate();
        }

        private void CheckAllContainers(ulong id)
        {
            foreach (var container in UnityEngine.Object.FindObjectsOfType<StorageContainer>())
            {
                if (!container.OwnerID.IsSteamId() || container.OwnerID != id) { continue; }
                if (container.PrefabName.Contains("box.wooden.large")) { UpgradeLargeBox(container); }
                if (container.PrefabName.Contains("woodenbox")) { UpgradeSmallBox(container); }
            }
        }
        
        private void CheckEntity(BaseNetworkable a)
        {
            var container = a.GetComponent<StorageContainer>();
            
            if (container == null) { return; }
            if (!container.OwnerID.IsSteamId()) { return; }
            if (container.PrefabName.Contains("box.wooden.large")) { UpgradeLargeBox(container); }
            if (container.PrefabName.Contains("woodenbox")) { UpgradeSmallBox(container); }
        }
    }
}