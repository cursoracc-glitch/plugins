using System.Linq;
using Network;

namespace Oxide.Plugins 
{
    [Info("ConnectionFix", "Zirper", "1.0.0")]
    class ConnectionFix : RustPlugin
    {
        void OnClientAuth(Connection connection)
        {
            NextTick(() =>
            {
                if (BasePlayer.FindByID(connection.userid) != null)
                    BasePlayer.FindByID(connection.userid).OnDisconnected();
                if (ConnectionAuth.m_AuthConnection.Any((Connection item) => item.userid == connection.userid))
                    ConnectionAuth.m_AuthConnection.Remove(connection);
            });
        }
    }
}
