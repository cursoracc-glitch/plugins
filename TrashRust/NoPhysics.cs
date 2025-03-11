using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("NoPhysics", "Mategus", "1.0.0")]

    class NoPhysics : RustPlugin
    {
        void OnServerInitialized()
        {
            DisableCollision();
        }

        private void DisableCollision()
        {
            Physics.IgnoreLayerCollision(26, 26, true);
        }
    }
}