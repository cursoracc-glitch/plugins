using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("AdminsOnline", "Koks", "1.0.0")]
    [Description("AdminsOnline")]
    public class AdminsOnline : RustPlugin
    {
        [ChatCommand("admins")]
        void AdminsCommand(BasePlayer player)
        {
            string result = "";
            List <string>admins=new List<string>();
            foreach(var pl in BasePlayer.activePlayerList)
            {
                if (pl.IsAdmin) admins.Add(pl.displayName);
            }
            if (admins.Count == 0) player.ChatMessage("Администрация оффлайне :(");
            else
            {
                int i = 0;
                foreach(var admin in admins)
                {
                    result=result+ admin;
                    if (i + 1 != admins.Count) result = result + ", ";
                    i++;
                }
                player.ChatMessage($"Администраторы в онлайне:\n{result}");
            }
        }
    }

}