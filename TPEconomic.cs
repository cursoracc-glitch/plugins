using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TPEconomic", "Sempai#3239", "1.0.3")]
    class TPEconomic : RustPlugin
    {
        //апи пополнить забрать взять
        [ChatCommand("f")]
        void fd(BasePlayer player)
        {
         //   PrintWarning($"{API_GET_BALANCE(player.userID)}");
        }
        private float API_GET_BALANCE(UInt64 player)
        {
            return DataEconomics[player];
        }
    
        private void API_PUT_BALANCE_PLUS(ulong player, float money)
        {
            DataEconomics[player] += money;
            WriteData();
            AnoncePlayer(player, $"Вам зачислено {money}xp!");
        }
        private void API_PUT_BALANCE_MINUS(ulong player, float money)
        {
            DataEconomics[player] -= money;
            WriteData();
            AnoncePlayer(player, $"C баланса списано {money}xp!");
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }

            return null;
        }

        private void AnoncePlayer(ulong player, string txt)
        {
            BasePlayer playerBS = FindPlayer(player.ToString());
            if(playerBS == null) return;

            playerBS.ChatMessage(txt);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            RegisteredDataUser(player.userID);
        }

        void RegisteredDataUser(UInt64 player)
        {
            if (!player.IsSteamId()) return;
            if (!DataEconomics.ContainsKey(player))
                DataEconomics.Add(player, 100);
        }

        void OnServerInitialized()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            timer.Every(360f, WriteData);
        }

        void Unload() => WriteData();
        private void OnServerShutdown() => Unload();
        private void Init() => ReadData();

        [JsonProperty("Система экономики")] public Dictionary<ulong, float> DataEconomics = new Dictionary<ulong, float>();
        void ReadData()
        {
            DataEconomics = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, float>>("TP/DataEconomics");
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("TP/DataEconomics", DataEconomics);
        }
    }
}