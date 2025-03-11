using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("AutoMute", "Oxide Россия - oxide-russia.ru", "2.1.11")]
    class AutoMute : CovalencePlugin
    {
        Dictionary<string, int> mutes = new Dictionary<string, int>();
        DynamicConfigFile saveFile = Interface.Oxide.DataFileSystem.GetFile("ChatMutes");

        void OnServerInitialized()
        {
            timer.Every(1f, () =>
            {
                List<string> toRemove = mutes.Keys.ToList().Where(uid => --mutes[uid] < 0).ToList();
                toRemove.ForEach(p => mutes.Remove(p));
            });
            mutes = saveFile.ReadObject<Dictionary<string, int>>();
        }

        private MuteList Chat;
        private class MuteList
        {
            public MuteList(string[] Say, string[] Exclusion, int MuteTime, int AuthLevel, bool AdminBlock)
            {
                this.Слова = Say;
                this.Исключения = Exclusion;
                this.MuteTime = MuteTime;
                this.AdminBlock = AdminBlock;
            }
            [JsonProperty("3. Запрещенные фразы")]
            public string[] Слова { get; set; }
            [JsonProperty("4. Слова-исключения")]
            public string[] Исключения { get; set; }
            [JsonProperty("2. Настройки: Время блокировки чата (сек)")]
            public int MuteTime;
            [JsonProperty("1. Настройки: Блокировать ли администраторов?")]
            public bool AdminBlock;
        }

        void Loaded()
        {
            Chat = Config.ReadObject<MuteList>();
        }
        void Unload()
        {
            saveFile.WriteObject(mutes);
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            PrintError("Маты и исключения добавлять в /config/AutoMute.json");

            Config.WriteObject(DefaultConfig(), true);
        }
        object OnBetterChat(Dictionary<string, object> data)
        {
            var player = (IPlayer)data["Player"];
            if (mutes.ContainsKey(player.Id))
            {
                return false;
            }
            return null;
        }
        object OnUserChat(IPlayer player, string message)
        {
            bool exclud = true;
            string SteamID = player.Id;
            string Nickname = player.Name;
            BasePlayer bplayer = BasePlayer.FindByID(ulong.Parse(player.Id));
            if (player == null || message == null) return null;
            if (!Chat.AdminBlock)
            {
                if (player.IsAdmin)
                {
                    return null;
                }
            }
            if (mutes.ContainsKey(player.Id))
            {
                player.Command("chat.add", new object[] { "0", $"<color=RED>Чат заблокирован!</color> До окончания: <color=green>{mutes[player.Id]} сек.</color>" });
                return false;
            }
            foreach (var word in Chat.Слова)
            {
                if (message.ToLower().Contains(word))
                {
                    foreach (var exc in Chat.Исключения)
                    {
                        if (message.ToLower().Contains(exc)) { exclud = false; break; }
                    }
                    if (exclud)
                    {
                        Mute(bplayer);
                        ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { 0, $"Игроку <color=#47ff47>{Nickname}</color> отключен чат! \nДлительность: <color=#47ff47>{Chat.MuteTime} сек.</color>  \nПричина: <color=#47ff47>Нецензурная лексика!</color>" });
                        PrintWarning("Auto mute " + Nickname + "(" + SteamID + $") {mutes[player.Id]}с. Причина: (" + message + ")");
                        LogToFile("log", $"({DateTime.Now.ToShortDateString()}) ({DateTime.Now.ToShortTimeString()}) Игроку {Nickname} ({SteamID}) был отключен чат на {Chat.MuteTime} сек. Сообщение: ({message})", this, false);
                        return false;
                    }
                }
            }
            return null;
        }
        void Mute(BasePlayer player)
        {
            mutes.Add(player.UserIDString, Chat.MuteTime);
        }

        #region DefaultConfig
        private MuteList DefaultConfig()
        {
            string[] Say =
            {
                "бля",
                "еба",
                "аху",
                "впиз",
                "въеб",
                "выбля",
                "выеб",
                "выёб",
                "гнид",
                "гонд",
                "доеб",
                "долбо",
                "дроч",
                "ёб",
                "елд",
                "заеб",
                "заёб",
                "залуп",
                "захуя",
                "заяб",
                "злоеб",
                "ипа",
                "лох",
                "лошар",
                "манд",
                "мля",
                "мраз",
                "муд",
                "наеб",
                "наёб",
                "напизд",
                "нах",
                "нех",
                "нии",
                "обоср",
                "отпиз",
                "отъеб",
                "оху",
                "падл",
                "падон",
                "педр",
                "пез",
                "перд",
                "пид",
                "пиз",
                "подъеб",
                "поеб",
                "поёб",
                "похе",
                "похр",
                "поху",
                "придур",
                "приеб",
                "проеб",
                "разху",
                "разъеб",
                "распиз",
                "соси",
                "спиз",
                "сук",
                "суч",
                "трах",
                "ублю",
                "уеб",
                "уёб",
                "ху",
                "целка",
                "чмо",
                "шалав",
                "шлюх",
                "ска"
            };
            string[] Ex =
            {
                  "мандар",
                  "мудр",
                  "наха",
                  "нахо",
                  "нахл",
                  "нехо",
                  "нехв",
                  "неха",
                  "пидж",
                  "похуд",
                  "сосиск",
                  "худ",
                  "хуж",
                  "хут",
                  "хур",
                  "хулиг",
                  "Команда",
                  "команда",
                  "команду",
                  "тебе",
                  "тебя",
                  "скайп",
                  "скайпу",
                  "скайпе",
                  "сказано",
                  "стёб",
                  "стеб"
            };

            return new MuteList(Say, Ex, 120, 2, true);
        }
        #endregion

    }
}
