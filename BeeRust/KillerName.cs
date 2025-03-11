using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("KillerName", "Drop Dead", "1.0.0")]
    public class KillerName : RustPlugin
    {
        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Текст для трупа/рюкзака | Text for corpse/backpack")]
            public string text = "<color=#42beeb>{0}</color> убил <color=#42beeb>{1}</color>";
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        void OnPlayerCorpseSpawned(BasePlayer player, PlayerCorpse corpse)
        {
            if (player == null || corpse == null) return;
            if (player.lastAttacker == null) return;
            BasePlayer attacker = player.lastAttacker.ToPlayer();
            if (attacker == null) return;

            string text = cfg.text;
            if (!IsRealPlayer(player)) text = text.Replace("{1}", "NPC");
            else text = text.Replace("{1}", player.displayName);

            if (!IsRealPlayer(attacker)) text = text.Replace("{0}", "NPC");
            else text = text.Replace("{0}", attacker.displayName);

            corpse.playerName = text;
        }

        bool IsRealPlayer(BasePlayer player)
        {
            if (player == null) return false;
            bool real = false;
            if (player.UserIDString.StartsWith("765611")) real = true;
            return real;
        }

        BaseCorpse OnCorpsePopulate(BasePlayer npcPlayer, BaseCorpse corpse)
        {
            Puts("OnCorpsePopulate works!");
            return null;
        }
    }
}