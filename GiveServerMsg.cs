namespace Oxide.Plugins
{
    [Info("GiveServerMsg", "EcoSmile", "1.0.0", ResourceId = 2336)]
    [Description("Hide server give message")]

    class GiveServerMsg : RustPlugin
    {
        object OnServerMessage(string m, string n) => m.Contains("gave") && n == "SERVER" ? (object)true : null;
    }
}
