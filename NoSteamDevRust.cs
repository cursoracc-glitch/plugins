using Network;

namespace Oxide.Plugins
{
    [Info("HKSHOST AutoInsecure", "playermodel", "1.0.0")]
    class NoSteamDevRust : RustPlugin
    {		

        void OnClientAuth(Connection connection)
        {
			Server.Command("secure 0");
			Server.Command("encryption 0");
            Server.Command("o.unload NoSteamDevRust");
		}
	}
}