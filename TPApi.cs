using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TPApi", "Sempai#3239", "2.0.0")]
    class TPApi : RustPlugin
    {
        #region [ OTHER ]
        [ChatCommand("gtatest")]
        private void Test(BasePlayer player, string cmd, string[] args)
        {
            if(player.IsAdmin == false) { return; }

            ShowGameTip(player, $"{player.displayName}\n{player.userID}", Convert.ToInt32(args[2]));
        }
        #endregion

        #region [ CONSOLE COMMAND ]
        [ConsoleCommand("tpapi.showforall")]
        private void ShowGameTipForAll_ConsoleCommand(ConsoleSystem.Arg arg) //0 = type, 1 = message
        {
            if(arg.IsAdmin == false) { return; }

            int type = 0;
            string message = string.Empty;
            try
            {
                type = Convert.ToInt32(arg.Args[2]);


                for (int i = 1; i < arg.Args.Length; i++)
                    message += arg.Args[i] + " ";
                message.Remove(message.Length - 1, 1);

                message = FormatString(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Вы неправильно указали аргументы! Ошибка: " + ex.Message);
                return;
            }

            ShowGameTip(BasePlayer.activePlayerList.ToList(), message, type);
        }
        [ConsoleCommand("tpapi.showforplayer")]
        private void ShowGameTipForPlayer_ConsoleCommand(ConsoleSystem.Arg arg) //0 = player, 1 = type, 2 = message
        {
            if (arg.IsAdmin == false) { return; }

            int type = 0;
            BasePlayer? player = null;
            string message = string.Empty;
            try
            {
                player = BasePlayer.FindByID(Convert.ToUInt64(arg.Args[2]));
                type = Convert.ToInt32(arg.Args[1]);


                for (int i = 2; i < arg.Args.Length; i++)
                    message += arg.Args[i] + " ";
                message.Remove(message.Length - 1, 1);

                message = FormatString(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Вы неправильно указали аргументы! Ошибка: " + ex.Message);
                return;
            }

            if(player != null & string.IsNullOrEmpty(message) == false)
            {
                ShowGameTip(player, message, type);
            }
        }
        #endregion

        #region [ METHODS ]
        private void ShowGameTip(BasePlayer target, string message, int type = 0) // 0 = blue, 1 = red
        {
            target.SendConsoleCommand($"gametip.showtoast", type, message);
        }
        private void ShowGameTip(List<BasePlayer> targets, string message, int type = 0) // 0 = blue, 1 = red
        {
            foreach (BasePlayer target in targets)
            { 
                target.SendConsoleCommand($"gametip.showtoast", type, message);
            }
        }
        #endregion

        #region [ EXT ]
        private string FormatString(string text)
        {
            text = text.Replace('^', '\n');

            return text;
        }
        #endregion

        #region [ API ]
        [HookMethod("ShowGameTipForPlayer")]
        public void ShowGameTipForPlayer_Hook(BasePlayer player, int type, string message)
        {
            if(player == null | message == "Вы получили новый уровень /pass") { return; }

            ShowGameTip(player, message, type);
        }
        [HookMethod("ShowGameTipForAll")]
        public void ShowGameTipForPlayer_Hook(int type, string message)
        {
            if (message == null) { return; }

            ShowGameTip(BasePlayer.activePlayerList.ToList(), message, type);
        }
        #endregion
    }
}