using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AutoSaver", "walkinrey", "1.0.0")]
    [Description("Сохраняет сервер прежде чем он выключится :)")]
    class AutoSaver : RustPlugin
    {
        void OnServerShutdown() => covalence.Server.Command("save");
    }
}