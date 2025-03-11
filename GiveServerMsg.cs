namespace Oxide.Plugins
{
    [Info("GiveServerMsg", "", "0.1", ResourceId = 2336)]

    public class GiveServerMsg : RustPlugin
    {
        object OnServerMessage(string m, string n) => m.Contains("gave") && n == "SERVER" ? (object)true : null;
    }
}