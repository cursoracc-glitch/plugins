using System;
using System.Collections.Generic;
using System.Diagnostics;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Plugins;

namespace Oxide.Plugins
{
    [Oxide.Plugins.Info("Vote Bonus From Launcher", "TheRyuzaki", "0.0.2")]
    public class VoteBonusFromLauncher : RustPlugin
    {
        private const string CONST_SHOP_ID = "";   // ID магазина
        private const string CONST_SHOP_KEY = "";  // API Ключ магазина
        private const int CONST_BONUS_RUB = 3;     // Сколько рублей давать за голос
        private const bool CONST_SAY_BONUS = true; // Оповещать людей в чате? О том, что кто либо проголосовал?
        private const string CONST_ADDRES_SERVER = "37.230.137.36:22031"; // Адрес сервера как написан в лаунчере

        private HashSet<int> HashSetVotes { get; set; }
        private Timer timerSearchVote { get; set; }
        private Queue<String> ListMessageToMainTheard = new Queue<string>();

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("VoteBonusFromLauncher"))
            {
                HashSetVotes = Interface.Oxide.DataFileSystem.ReadObject<HashSet<int>>("VoteBonusFromLauncher");
            }
            else
            {
                HashSetVotes = new HashSet<int>();
            }

            timerSearchVote = timer.Repeat(15f, 0, OnStartSearchVote);
        }

        [HookMethod("Unload")]
        void Unload()
        {
            if (HashSetVotes.Count != 0)
            {
                Interface.Oxide.DataFileSystem.WriteObject<HashSet<int>>("VoteBonusFromLauncher", HashSetVotes);
            }
            
            timerSearchVote.Destroy();
        }

        void OnStartSearchVote()
        {
            this.webrequest.Enqueue($"https://expshop.alkad.org/Api/Launcher.VoteList?date={DateTime.Now:yyyy-MM-dd}&server={CONST_ADDRES_SERVER}", "", OnResonseSearchVote, this);
        }

        private void OnResonseSearchVote(int code, string response)
        {
            string[] lines = response.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 20)
                {
                    string[] valuesFromLine = lines[i].Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                    if (valuesFromLine.Length == 3)
                    {
                        int id = 0;
                        if (int.TryParse(valuesFromLine[0], out id))
                        {
                            if (HashSetVotes.Contains(id) == false)
                            {
                                HashSetVotes.Add(id);
                                ulong steamid = 0;
                                if (ulong.TryParse(valuesFromLine[1], out steamid))
                                {
                                    BasePlayer player = BasePlayer.FindByID(steamid);
                                    string targetPlayer = (player == null ? steamid.ToString() : player.displayName);
                                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[]
                                    {
                                        0,
                                        $"<color=orange>Игрок [{targetPlayer}] проголосовал за сервер, и получил бонус в донат магазине [{CONST_BONUS_RUB} руб]. Спасибо тебе игрок, за поддержку!</color>"
                                    });
                                    this.OnSendBonusVote(steamid);
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError("[VoteBonusFromLauncher]: Not found SteamID from line: " + lines[i]);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnSendBonusVote(ulong steamid)
        {
            string url = $"http://panel.gamestores.ru/api?shop_id={CONST_SHOP_ID}&secret={CONST_SHOP_KEY}&action=moneys&type=plus&steam_id={steamid}&amount={CONST_BONUS_RUB}&mess=Vote Bonus";
            webrequest.EnqueueGet(url, OnResponseBonusVote, this);
        }

        private void OnResponseBonusVote(int code, string response)
        {
            this.Puts("Response from GameStores: " + response);
        }
    }
}