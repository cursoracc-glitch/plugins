using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Corpsedel", "Toshik", "1.0.1")]
    public class Corpsedel : RustPlugin
    {
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;

            if (entity.ShortPrefabName == "player_corpse_new" || entity.ShortPrefabName == "scientist_corpse" || entity.ShortPrefabName == "player_corpse")
            {
                var damageable = entity.GetComponent<BaseCombatEntity>();
                if (damageable != null)
                {
                    Timer damageTimer = null;
                    damageTimer = timer.Repeat(1f, 0, () =>
                    {
                        if (damageable.IsDestroyed || damageable == null) damageTimer.Destroy();
                        else damageable.Hurt(300000f);
                    });
                }
            }
        }
    }
}
