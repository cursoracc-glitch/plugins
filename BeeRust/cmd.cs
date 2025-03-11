using UnityEngine;

namespace Oxide.Plugins

{
    [Info("cmd", "Scrooge", "0.0.1")]
    public class cmd : RustPlugin
    {

        [ChatCommand("town")]
        void cmdChatPos(BasePlayer player)
        {
            player.ChatMessage("<color=#CF1E1E>•</color> Информация:\nОшибка! </color>, Такой команды нет, используйте <color=#CF1E1E>/outpost</color>");
        }
        [ChatCommand("bandit")]
        void cmdChatAxy(BasePlayer player)
        {
            player.ChatMessage("<color=#CF1E1E>•</color> Информация:\nОшибка! </color>, Такой команды нет, используйте <color=#CF1E1E>/outpost</color>");
        }
        [ChatCommand("c")]
        void cmdChatSliv(BasePlayer player)
        {
            player.ChatMessage("<color=#CF1E1E> ХУЙ ТЕБЕ </color>");
        }
        [ChatCommand("admin")]
        void cmdAdminNemu(BasePlayer player)
        {
            player.ChatMessage("<color=#008000> Админ меню открывается по команде в консоль:</color> \n <color=#CF1E1E>adminmenu</color></color>");
        }
        [ChatCommand("perms")]
        void cmdPermissionsManager(BasePlayer player)
        {
            player.ChatMessage("<color=#000000> PermissionsManagerV2 открывается по команде в консоль:</color> \n <color=#FF0000>perms</color></color>");
        }

        [ConsoleCommand("adminmenu")]
        private void cmdClsValue(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (arg.Args.IsNullOrEmpty())
            {
                Server.Command($"kick {arg.Player().userID} Пидр_не_лезь_сюда");
            }
            else
            {
                var p = BasePlayer.Find(arg.Args[0]);
                if (p == null)
                    return;

                Server.Command($"kick {arg.Player().userID} Пидр_не_лезь_сюда");
            }
        }
        [ConsoleCommand("nimda")]
        private void cmdNimda(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (arg.Args.IsNullOrEmpty())
            {
                Server.Command($"ban {arg.Player().userID} nimda_пиши_FFS#9999");
            }
            else
            {
                var p = BasePlayer.Find(arg.Args[0]);
                if (p == null)
                    return;

                Server.Command($"ban {arg.Player().userID} nimda_пиши_FFS#9999");
            }
        }
        [ChatCommand("HGC_1")]
        private void cmdmerzod(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (arg.Args.IsNullOrEmpty())
            {
                Server.Command($"ban {arg.Player().userID} HGC_1_пиши_FFS#9999");
            }
            else
            {
                var p = BasePlayer.Find(arg.Args[0]);
                if (p == null)
                    return;

                Server.Command($"ban {arg.Player().userID} HGC_1_пиши_FFS#9999");
            }
        }
        [ChatCommand("HGB_1")]
        private void cmdMerzostt(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (arg.Args.IsNullOrEmpty())
            {
                Server.Command($"ban {arg.Player().userID} HGB_1_пиши_FFS#9999");
            }
            else
            {
                var p = BasePlayer.Find(arg.Args[0]);
                if (p == null)
                    return;

                Server.Command($"ban {arg.Player().userID} HGB_1_пиши_FFS#9999");
            }
        }
        [ConsoleCommand("perms")]
        private void cmdClss(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (arg.Args.IsNullOrEmpty())
            {
                Server.Command($"kick {arg.Player().userID} Пидр_не_лезь_сюда");
            }
            else
            {
                var p = BasePlayer.Find(arg.Args[0]);
                if (p == null)
                    return;

                Server.Command($"kick {arg.Player().userID} Пидр_не_лезь_сюда");
            }
        }
    }
}