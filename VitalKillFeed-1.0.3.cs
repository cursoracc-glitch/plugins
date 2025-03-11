using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("VitalKillFeed", "Xonafied", "1.0.3")]
    [Description("Vital kill feed msgs")]

    public class VitalKillFeed : CovalencePlugin
    {
        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["killmsg"] = "<color=#C57039>[KillFeed]</color> {0} killed you from {1} meters away with a {2}",
                ["killmsg2"] = "<color=#C57039>[KillFeed]</color> You killed {0} from {1} meters away",
                ["killtoggle"] = "Kill feed messages are now {0}"
            }, this);
        }

        #endregion Localization

        private static readonly string permhide = "rustxkillfeed.hide";

        #region Hooks

        void OnServerInitialized()
        {
            //Register Perms
            permission.RegisterPermission(permhide, this);
            AddCovalenceCommand("kf", "ToggleDeathMsgs");
        }

        void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || player.IsNpc) return;

            var customInfo = new CustomInfo
            {
                info = info,
                weapon = info.Weapon.ShortPrefabName
            };

            var initiatorPlayer = info.InitiatorPlayer;
            if (initiatorPlayer != null && !initiatorPlayer.IsNpc)
            {
                customInfo.weapon = initiatorPlayer.GetActiveItem()?.info.displayName.english ?? customInfo.weapon;
                SendHitInfoData(initiatorPlayer, customInfo, player);
            }

            if (_deathData.TryGetValue(player.userID, out CustomInfo existingInfo))
            {
                existingInfo = customInfo;
            }
            else
            {
                _deathData.Add(player.userID, customInfo);
            }
        }


        void OnPlayerRespawn(BasePlayer player, BasePlayer.SpawnPoint spawnPoint) => SendHitInfoData(player);

        void OnPlayerRespawn(BasePlayer player, SleepingBag sleepingBag) => SendHitInfoData(player);

        #endregion

        #region Methods
        private Dictionary<ulong, CustomInfo> _deathData = new Dictionary<ulong, CustomInfo>();

        public struct CustomInfo
        {
            public HitInfo info;
            public string weapon;
        }
        private void SendHitInfoData(BasePlayer player)
        {
            if (!_deathData.TryGetValue(player.userID, out CustomInfo custominfo))
                return;

            if (HasPerm(player.UserIDString, permhide))
                return;

            string killerName = custominfo.info.InitiatorPlayer?.displayName ?? "Unknown";
            double dist = Math.Round(custominfo.info.ProjectileDistance, 0);

            player.Invoke(() => {
                ChatMessage(player.IPlayer, "killmsg", killerName, dist, custominfo.weapon);
            }, 0.3f);
        }

        private void SendHitInfoData(BasePlayer player, CustomInfo custominfo, BasePlayer killed)
        {
            if (HasPerm(player.UserIDString, permhide))
                return;

            double dist = Math.Round(custominfo.info.ProjectileDistance, 0);

            ChatMessage(player.IPlayer, "killmsg2", killed.displayName, dist);
        }

        #endregion Methods

        #region Commands
        private void ToggleDeathMsgs(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer? player = iplayer.Object as BasePlayer;
            if (player == null) return;

            if (!HasPerm(player.UserIDString, permhide))
            {
                permission.GrantUserPermission(player.UserIDString, permhide, this);
                ChatMessage(iplayer, "killtoggle", "disabled");
                return;
            }

            permission.RevokeUserPermission(player.UserIDString, permhide);
            ChatMessage(iplayer, "killtoggle", "enabled");
        }
        #endregion

        #region Helpers
        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }
        private void ChatMessage(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected)
                player.Message(GetLang(langKey, player.Id, args));
            else Puts(GetLang(langKey, player.Id, args));
        }
        #endregion Helpers
    }
}