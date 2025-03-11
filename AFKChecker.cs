using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AFKChecker", "Автор Hougan, дополнил FREDWAY и Ryamkk", "0.0.1")]
    [Description("Проверяет игрока на длительность отсутствия за игрокй")]
    public class AFKChecker : RustPlugin
    {
		private string CheckPermission = "AFKChecker.Moder";
        private string IgnorePermission = "AFKChecker.Ignore";
        private int Interval = 5;
		private ulong senderID = 76561198236355670;
		private string header = "Виртуальный помощник";
		private string prefixcolor = "#81B67A";
		private string prefixsize = "16";
		
		private void LoadDefaultConfig()
        {
            GetConfig("Настройки сообщений", "Иконка для сообщений в чате (необходимо указать id профиля steam)", ref senderID);
            GetConfig("Настройки сообщений", "Названия префикса", ref header);
			GetConfig("Настройки сообщений", "Цвет префикса", ref prefixcolor);
			GetConfig("Настройки сообщений", "Размер префиса", ref prefixsize);
			
			GetConfig("Основные настройки", "Разрешение на проверку игрока", ref CheckPermission);
			GetConfig("Основные настройки", "Разрешение на игнорирование проверки", ref IgnorePermission);
			GetConfig("Основные настройки", "Интервал проверки на движение/крафт/и прочие действия", ref Interval);
            SaveConfig();
        }
		
		[JsonProperty("Словарь хранящий время АФК игроков")]
        private Dictionary<ulong, Player> afkDictionary = new Dictionary<ulong, Player>();
        private class Player
        {
            [JsonProperty("Время без движений")] 
            public int AFKTime = 0;
            [JsonProperty("Прочие действия в этом интервале")]
            public bool Actions = false;
            
            [JsonProperty("Последняя позиция")] 
            public Vector3 LastPosition;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(CheckPermission, this);
            permission.RegisterPermission(IgnorePermission, this);
            
            PrintWarning($"Проверка игрока на AFK запущена, интервал: {Interval} сек.");
            timer.Every(Interval, TrackAFK);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (afkDictionary.ContainsKey(player.userID))
                afkDictionary.Remove(player.userID);
        }

        private void OnPlayerChat(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null)
                return;
            
            if (!afkDictionary.ContainsKey(player.userID))
                return;
            
            afkDictionary[player.userID].Actions = true;
        }
        
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!afkDictionary.ContainsKey(player.userID))
                return;
            
            afkDictionary[player.userID].Actions = true;
        }
        
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (!afkDictionary.ContainsKey(player.userID))
                return;
            
            afkDictionary[player.userID].Actions = true;
        }

        private void TrackAFK()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!afkDictionary.ContainsKey(player.userID))
                {
                    afkDictionary.Add(player.userID, new Player 
					{
						LastPosition = player.transform.position 
					});
                    continue;
                }
                
                Player currentPlayer = afkDictionary[player.userID];
                if (currentPlayer.Actions)
                {
                    currentPlayer.LastPosition = player.transform.position;
                    currentPlayer.Actions = false;
                    currentPlayer.AFKTime = 0;
                    continue;
                }

                if (Vector3.Distance(currentPlayer.LastPosition, player.transform.position) > 1)
                {
                    currentPlayer.LastPosition = player.transform.position;
                    currentPlayer.AFKTime = 0;
                    continue;
                }

                if (player.inventory.crafting.queue.Count > 0)
                {
                    currentPlayer.AFKTime = 0;
                    continue;
                }

                currentPlayer.AFKTime += Interval;
            }
        }

        [ChatCommand("afkcheck")]
        private void cmdAFKCheck(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, CheckPermission))
            {
                ReplyWithHelper(player, $"У вас недостаточно прав для использования этой команды");
                return;
            }

            if (args.Length == 0)
            {
                ReplyWithHelper(player, $"Вы не ввели имя игрока которого хотите проверить");
                return;
            }

            List<BasePlayer> targetList = FindPlayers(args[0]);
            switch (targetList.Count)
            {
                case 0:
                {
                    ReplyWithHelper(player, $"Мы не смогли найти игрока по вашему запросу!");
                    break;
                }
                case 1:
                {
                    BasePlayer target = targetList[0];
                    if (permission.UserHasPermission(target.UserIDString, IgnorePermission))
                    {
                        ReplyWithHelper(player, $"Вы не можете проверять этого игрока!");
                        return;
                    }
                    
                    if (!afkDictionary.ContainsKey(target.userID))
                    {
                        ReplyWithHelper(player, $"Непредвиденная ошибка, попробуйте позже!");
                        return;
                    }

                    Player targetAFK = afkDictionary[target.userID];
                    if (targetAFK.AFKTime == 0)
                    {
                        ReplyWithHelper(player, $"Игрок двигался или совершал другие действия в течение последних {Interval} секунд!");
                        return;
                    }
                    
                    ReplyWithHelper(player, $"Игрок не совершал никаких действий в течении последних: {new TimeSpan(0, 0, targetAFK.AFKTime).ToShortString()}!");
                    break;
                }
                default:
                {
                    string message = $"Мы нашли несколько игроков по вашему запросу:\n\n";

                    for (int i = 0; i < targetList.Count; i++)
                        message += $"[<color=#81B67A>{i}.</color>] {targetList[i].displayName} [{targetList[i].userID}]";
                    
                    player.ChatMessage(message);
                    break;
                }
            }
        }

        private bool IsAFK(BasePlayer player)
        {
            Player targetAFK = afkDictionary[player.userID];
            if (targetAFK.AFKTime == 0)
                return false;

            return true;
        }

        private List<BasePlayer> FindPlayers(string nameOrId)
        {
            List<BasePlayer> targetList = new List<BasePlayer>();
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.userID.ToString() == nameOrId)
                    return new List<BasePlayer> { check };
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()))
                    targetList.Add(check);
            }
            
            return targetList;
        }
		
		public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
	        {
                message = string.Format(message, args);
	        }
			
	        player.SendConsoleCommand("chat.add", senderID, string.Format("<size=prefixsize><color=prefixcolor>{0}</color>:</size>\n{1}", header, message));
        }
		
		private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
    }
}