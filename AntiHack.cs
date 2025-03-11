using Network;
using Network.Visibility;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AntiHack", "OxideBro", "1.0.42")]
    public class AntiHack : RustPlugin
    {
        #region References
        [PluginReference]
        private Plugin Discord;
        #endregion

        private static Dictionary<ulong, HashSet<uint>> playersHidenEntities = new Dictionary<ulong, HashSet<uint>>();
        private static int radius = 35;
        private Dictionary<BasePlayer, CurrentLog> currentAdminsLog = new Dictionary<BasePlayer, CurrentLog>();
        private Dictionary<ulong, float> lastSpeedAttackAttackTime = new Dictionary<ulong, float>();
        private Dictionary<ulong, float> lastShootingThroughWallTime = new Dictionary<ulong, float>();
        public List<BasePlayer> Admins = new List<BasePlayer>();
        private Dictionary<ulong, int> speedAttackDetections = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> shootingThroughWallDetections = new Dictionary<ulong, int>();
        private static bool isSaving = false;
        private static float minPlayersWallHackDistanceCheck = 0.0f;
        private static float minObjectsWallHackDistanceCheck = 50.0f;
        private static float maxPlayersWallHackDistanceCheck = 250f;
        private static float maxObjectsWallHackDistanceCheck = 150f;
        private static float tickRate = 0.1f;
        private static int globalMask = LayerMask.GetMask("Construction", "Deployed", "World", "Default");
        private static int cM = LayerMask.GetMask("Construction");
        private static int playerWallHackMask = LayerMask.GetMask("Construction", "World", "Default", "Deployed");
        private static int entityMask = LayerMask.GetMask("Deployed");
        private static int constructionAndDeployed = LayerMask.GetMask("Construction", "Deployed");
        private static Dictionary<ulong, int> playersKicks = new Dictionary<ulong, int>();
        private static Dictionary<ulong, HackHandler> playersHandlers = new Dictionary<ulong, HackHandler>();
        private static Dictionary<int, Dictionary<int, Chunk>> chunks = new Dictionary<int, Dictionary<int, Chunk>>();
        private static HashSet<Chunk> chunksList = new HashSet<Chunk>();
        private List<string> neededEntities = new List<string>()
        {
          "cupboard.tool.deployed",
          "sleepingbag_leather_deployed",
          "bed_deployed"
        };
        public bool IsConnected(BasePlayer player) => BasePlayer.activePlayerList.Contains(player);
        public void Kick(BasePlayer player, string reason = "⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠") => player.Kick(reason);
        public bool IsBanned(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Banned);
        private static AntiHack instance;
        private bool isLoaded;
        private static bool wallHackPlayersEnabled;
        private static bool wallHackObjectsEnabled;
        private static bool enableFlyHackLog;
        private static bool enableFlyHackCar;
        private static bool enableSpeedHackLog;
        private static bool enableTextureHackLog;
        private static bool enableSpeedAttackLog;
        private static bool enableWallHackAttackLog;
        private static bool needKick;
        private static bool needKickEndKill;
        private static bool needBan;
        private bool configChanged;
        private const int intervalBetweenTextureHackMessages = 50;
        private const int maxFalseFlyDetects = 5;
        private const int maxFalseSpeedDetects = 5;
        private static int maxFlyWarnings;
        private static int maxSpeedWarnings;

        private static bool SendDiscordMessages;
        private static StoredData db;

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            Dictionary<string, object> dictionary = Config[menu] as Dictionary<string, object>;
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, object>();
                Config[menu] = (object)dictionary;
                configChanged = true;
            }
            object obj;
            if (!dictionary.TryGetValue(datavalue, out obj))
            {
                obj = defaultValue;
                dictionary[datavalue] = obj;
                configChanged = true;
            }
            return obj;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        private void DiscordMessages(string Messages, int type, string Reason = "⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")
        {
            if (SendDiscordMessages)
            {
                if (type == 0)
                    instance.Discord.Call("SendMessage", $"\n**ANTIHACK DETECTED!**\n\n{Messages}");
                else instance.Discord.Call("SendMessage", $"{Reason}!\n\n{Messages}");
            }
        }

        private void LoadVariables()
        {
            maxFlyWarnings = Convert.ToInt32(GetConfig("Основное", "Количество детектов FlyHack для наказания:", 10));
            maxSpeedWarnings = Convert.ToInt32(GetConfig("Основное", "Количество детектов SpeedHack для наказания:", 10));
            needKick = Convert.ToBoolean(GetConfig("Основное", "Наказать киком:", false));
            needKickEndKill = Convert.ToBoolean(GetConfig("Основное", "TextureHack - Наказать киком и убить игрока:", false));
            needBan = Convert.ToBoolean(GetConfig("Основное", "Наказать баном:", false));
            enableFlyHackLog = Convert.ToBoolean(GetConfig("Основное", "Логировать детекты FlyHack:", true));
            enableFlyHackCar = Convert.ToBoolean(GetConfig("Основное", "Не логировать детекты в машине?", true));
            enableSpeedHackLog = Convert.ToBoolean(GetConfig("Основное", "Логировать детекты SpeedHack:", true));
            enableTextureHackLog = Convert.ToBoolean(GetConfig("Основное", "Логировать детекты TextureHack:", false));
            enableSpeedAttackLog = Convert.ToBoolean(GetConfig("Основное", "Логировать детекты на быстрое добывание:", true));
            enableWallHackAttackLog = Convert.ToBoolean(GetConfig("Основное", "Логировать детекты на WallHackAttack:", true));
            wallHackObjectsEnabled = Convert.ToBoolean(GetConfig("Экспериментальное", "Включить AntiESP на объекты (Внимание! Может нагружать сервер!)", false));
            wallHackPlayersEnabled = Convert.ToBoolean(GetConfig("Экспериментальное", "Включить AntiESP на людей (Внимание! Может сильно нагружать сервер!)", false));
            SendDiscordMessages = Convert.ToBoolean(GetConfig("Основное", "Включить отправку детектов и сообщений на канал Discord (Нужен плагин Discord)", false));
            if (!configChanged)
                return;
            SaveConfig();
            configChanged = false;
        }

        public static void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>("AntiHack_Detects", db, false);
        }

        public void ShowDetects(BasePlayer player, string[] args)
        {
            string s = null;
            if (args.Length == 2)
            {
                if (args[1] == "0")
                {
                    player.ChatMessage("Очищаем номер детекта для телепорта...");
                    currentAdminsLog.Remove(player);
                }
                else
                    s = args[1];
            }
            string user = args[0];
            Log log;
            if (user.Contains("765"))
            {
                ulong id;
                ulong.TryParse(args[0], out id);
                log = db.logs.Find(x => (long)x.steamID == (long)id);
            }
            else
                log = db.logs.Find(x => x.name.Contains(user, CompareOptions.IgnoreCase));
            if (log == null)
            {
                player.ChatMessage("Ошибка. В логах нет такого игрока");
            }
            else
            {
                CurrentLog currentLog;
                if (!currentAdminsLog.TryGetValue(player, out currentLog))
                {
                    currentAdminsLog[player] = new CurrentLog()
                    {
                        detect = 1,
                        steamID = log.steamID
                    };
                    player.ChatMessage(string.Format("Игрок {0}\nКоличество детектов: {1}", log.name, log.detectsAmount));
                }
                else if ((long)currentLog.steamID != (long)log.steamID)
                {
                    currentAdminsLog[player] = new CurrentLog()
                    {
                        detect = 1,
                        steamID = log.steamID
                    };
                    player.ChatMessage(string.Format("Игрок {0}\nКоличество детектов: {1}", log.name, log.detectsAmount));
                }
                else if (s == null)
                {
                    if (log.detectsAmount >= currentLog.detect + 1)
                    {
                        ++currentLog.detect;
                    }
                    else
                    {
                        player.ChatMessage(string.Format("Больше детектов у игрока {0} нет", log.name));
                        currentAdminsLog.Remove(player);
                        return;
                    }
                }
                int result = 0;
                int.TryParse(s, out result);
                bool flag = false;
                for (int index = 0; index < log.detects.Count; ++index)
                {
                    Detect detect = log.detects[index];
                    if (result == 0)
                    {
                        if (currentAdminsLog[player].detect == index + 1)
                        {
                            foreach (Coordinates coordinate in detect.coordinates)
                            {
                                Vector3 vector3_1 = coordinate.startPos.ToVector3();
                                Vector3 vector3_2 = coordinate.endPos.ToVector3();
                                player.SendConsoleCommand("ddraw.arrow", 20f, Color.white, vector3_1, vector3_2, 0.2f);
                                if (!flag)
                                {
                                    player.Teleport(vector3_1);
                                    flag = true;
                                    player.ChatMessage(string.Format("Телепорт на детект {0} игрока {1}", (object)currentAdminsLog[player].detect, (object)log.name));
                                }
                            }
                        }
                    }
                    else if (result == index + 1)
                    {
                        foreach (Coordinates coordinate in detect.coordinates)
                        {
                            Vector3 vector3_1 = coordinate.startPos.ToVector3();
                            Vector3 vector3_2 = coordinate.endPos.ToVector3();
                            player.SendConsoleCommand("ddraw.arrow", (object)20f, (object)Color.white, (object)vector3_1, (object)vector3_2, (object)0.2f);
                            if (!flag)
                            {
                                player.Teleport(vector3_1);
                                flag = true;
                                player.ChatMessage(string.Format("Телепорт на детект {0} игрока {1}", (object)result, (object)log.name));
                                currentAdminsLog[player].detect = result;
                            }
                        }
                    }
                }
            }
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || !(entity is BasePlayer) || item == null || dispenser == null) return;
            if (entity.ToPlayer() is BasePlayer) { };

        }
        public static void LogHandler(BasePlayer player, Vector3 lastGroundPos, TemporaryCoordinates temp, bool isSpeedHack = false)
        {
            Log log1 = db.logs.Find((Predicate<Log>)(x => (long)x.steamID == (long)player.userID));
            Vector3 position = player.transform.position;
            if (isSpeedHack)
            {
                position.y += 0.7f;
                lastGroundPos.y += 0.7f;
            }
            Coordinates coordinates;
            coordinates.startPos = lastGroundPos.ToString();
            coordinates.endPos = position.ToString();
            Detect detect = new Detect();
            if (log1 == null)
            {
                Log log2 = new Log();
                log2.detectsAmount = 1;
                if (temp.coordinates.Count > 0)
                {
                    detect.coordinates.AddRange((IEnumerable<Coordinates>)temp.coordinates);
                    log2.detects.Add(detect);
                }
                else if (isSpeedHack)
                {
                    detect.coordinates.Add(coordinates);
                    log2.detects.Add(detect);
                }
                log2.name = player.displayName;
                log2.steamID = player.userID;
                db.logs.Add(log2);
            }
            else
            {
                ++log1.detectsAmount;
                if (temp.coordinates.Count > 0)
                {
                    detect.coordinates.AddRange((IEnumerable<Coordinates>)temp.coordinates);
                    log1.detects.Add(detect);
                }
                else if (isSpeedHack)
                {
                    detect.coordinates.Add(coordinates);
                    log1.detects.Add(detect);
                }
                log1.name = player.displayName;
            }
        }

        void ShowLog(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;
            if (!CanGetReport(player))
                return;
            if (args.Length == 0)
            {
                SendReply(player, "<size=15><color=orange>AntiHack development from RustPlugin.ru</color></size>\n" +
                    "Используйте:\n" +
                    "<color=orange>/ah on/off</color> - Включить/отключить иммунитет к проверкам античита\n" +
                    "<color=orange>/ah SteamID/NAME</color> - Телепортация на первый детект игрока\n" +
                    "<color=orange>/ah SteamID/NAME 0 </color>- Телепортация на первый детект игрока");
                return;
            }
            if (args[0] == "on" || args[0] == "off")
            {
                switch (args[0])
                {
                    case "on":
                        if (Admins.Contains(player))
                        {
                            SendReply(player, "У Вас уже включен иммунитет к проверкам античита");
                            return;
                        }
                        Admins.Add(player);
                        player.ChatMessage("Вы включючили иммунитет к проверкам античита");
                        break;
                    case "off":
                        if (!Admins.Contains(player))
                        {
                            SendReply(player, "У Вас уже выключен иммунитет к проверкам античита");
                            return;
                        }
                        HackHandler component = player.GetComponent<HackHandler>();
                        component.lastGroundPosition = player.transform.position;
                        component.playerPreviousPosition = player.transform.position;
                        Admins.Remove(player);
                        player.ChatMessage("Вы отключили иммунитет к проверкам античита");
                        break;
                }
            }
            else
                ShowDetects(player, args);
        }

        private static HashSet<BaseEntity> GetEntitiesFromAllChunks()
        {
            HashSet<BaseEntity> baseEntitySet = new HashSet<BaseEntity>();
            foreach (Chunk chunks in chunksList)
            {
                foreach (BaseEntity entity in chunks.entities)
                {
                    if (!(entity == null) && !entity.IsDestroyed)
                        baseEntitySet.Add(entity);
                }
            }
            return baseEntitySet;
        }

        private static HashSet<BaseEntity> GetEntitiesFromChunksNearPointOptimized(Vector3 point)
        {
            Chunk chunkFromPoint = GetChunkFromPoint(point);
            HashSet<BaseEntity> baseEntitySet = new HashSet<BaseEntity>();
            if (chunkFromPoint == null)
                return baseEntitySet;
            foreach (BaseEntity entity in chunkFromPoint.entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x + 1][chunkFromPoint.z + 1].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x - 1][chunkFromPoint.z - 1].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x][chunkFromPoint.z + 1].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x + 1][chunkFromPoint.z].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x - 1][chunkFromPoint.z].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x][chunkFromPoint.z - 1].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x - 1][chunkFromPoint.z + 1].entities)
                baseEntitySet.Add(entity);
            foreach (BaseEntity entity in chunks[chunkFromPoint.x + 1][chunkFromPoint.z - 1].entities)
                baseEntitySet.Add(entity);
            return baseEntitySet;
        }

        private static Chunk GetChunkFromPoint(Vector3 point)
        {
            Dictionary<int, Chunk> dictionary;
            Chunk chunk;
            if (chunks.TryGetValue((int)((double)point.x / (double)radius), out dictionary) && dictionary.TryGetValue((int)((double)point.z / (double)radius), out chunk))
                return chunk;
            return null;
        }

        private void SetPlayer(BasePlayer player)
        {
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "antihack.logs"))
            {
                if (!Admins.Contains(player))
                    Admins.Add(player);
            }
            HackHandler hackHandler = player.gameObject.AddComponent<HackHandler>() ?? player.GetComponent<HackHandler>();
            playersHandlers[player.userID] = hackHandler;
            lastSpeedAttackAttackTime[player.userID] = UnityEngine.Time.realtimeSinceStartup;
            speedAttackDetections[player.userID] = 0;
            shootingThroughWallDetections[player.userID] = 0;
            lastShootingThroughWallTime[player.userID] = UnityEngine.Time.realtimeSinceStartup;
        }

        public static bool CanGetReport(BasePlayer player)
        {
            return Interface.Oxide.GetLibrary<Permission>(null).UserHasPermission(player.userID.ToString(), "antihack.logs") || player.IsAdmin;
        }

        private static void SendReportToOnlineModerators(string report)
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (CanGetReport(activePlayer))
                    activePlayer.ChatMessage(string.Format("[AntiHack] {0}", (object)report));
            }
        }

        public static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrIdOrIp || activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                Networkable net = activePlayer.net;
                if ((net != null ? net.connection : (Network.Connection)null) != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString == nameOrIdOrIp || sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return (BasePlayer)null;
        }

        private void ShootingThroughWallHanlder(BasePlayer attacker, HitInfo info, float timeNow)
        {
            BaseEntity hitEntity = info.HitEntity;
            if (hitEntity == null || (hitEntity as BasePlayer) == null)
                return;
            Vector3 hitPositionWorld = info.HitPositionWorld;
            Vector3 pointStart = info.PointStart;
            if (!Physics.Linecast(pointStart, hitPositionWorld, cM))
            {
                if (shootingThroughWallDetections[attacker.userID] == 0 || timeNow - lastShootingThroughWallTime[attacker.userID] <= 10.0)
                    return;
                shootingThroughWallDetections[attacker.userID] = 0;
            }
            else
            {
                lastShootingThroughWallTime[attacker.userID] = timeNow;
                Dictionary<ulong, int> throughWallDetections = shootingThroughWallDetections;
                ulong userId = attacker.userID;
                long num1 = (long)userId;
                int num2 = throughWallDetections[(ulong)num1];
                long num3 = (long)userId;
                int num4 = num2 + 1;
                throughWallDetections[(ulong)num3] = num4;
                if (shootingThroughWallDetections[attacker.userID] <= 5)
                    return;
                int averagePing = Network.Net.sv.GetAveragePing(attacker.net.connection);
                string str = string.Format("WallHackAttack Detected\n{0} [{1}]\n{2} -> {3}\nПинг: {4} мс.\nПредупреждений: {5}", (object)attacker.displayName, (object)attacker.userID, (object)pointStart, (object)hitPositionWorld, (object)averagePing, (object)shootingThroughWallDetections[attacker.userID]);
                string strMessage = string.Format("WallHackAttack | {0} [{1}] | {2} -> {3} | {4} мс. | Предупреждений: {5}", (object)attacker.displayName, (object)attacker.userID, (object)pointStart, (object)hitPositionWorld, (object)averagePing, (object)shootingThroughWallDetections[attacker.userID]);
                DiscordMessages(str, 0);
                instance.LogToFile("Log", strMessage, instance);
                SendReportToOnlineModerators(str);
                Interface.Oxide.LogError(str);
            }
        }

        private void SpeedAttackHandler(BasePlayer attacker, HitInfo info, BaseMelee melee, float timeNow)
        {
            if (attacker.IsAdmin)
                return;
            if ((double)timeNow - (double)lastSpeedAttackAttackTime[attacker.userID] < (double)melee.repeatDelay - 0.25)
            {
                info.damageTypes = new DamageTypeList();
                info.HitEntity = (BaseEntity)null;
                Dictionary<ulong, int> attackDetections = speedAttackDetections;
                ulong userId = attacker.userID;
                long num1 = (long)userId;
                int num2 = attackDetections[(ulong)num1];
                long num3 = (long)userId;
                int num4 = num2 + 1;
                attackDetections[(ulong)num3] = num4;
                if (speedAttackDetections[attacker.userID] > 5)
                {
                    int averagePing = Network.Net.sv.GetAveragePing(attacker.net.connection);
                    string str = string.Format("SpeedGather Detected\n{0} [{1}]\nПозиция: {2}\nПинг: {3} мс.\nПредупреждений: {4}", (object)attacker.displayName, (object)attacker.userID, (object)attacker.transform.position, (object)averagePing, (object)speedAttackDetections[attacker.userID]);
                    string strMessage = string.Format("SpeedGather | {0} [{1}] | {2} | {3} мс. | Предупреждений: {4}", (object)attacker.displayName, (object)attacker.userID, (object)attacker.transform.position, (object)averagePing, (object)speedAttackDetections[attacker.userID]);
                    DiscordMessages(str, 0);
                    instance.LogToFile("Log", strMessage, instance);
                    SendReportToOnlineModerators(str);
                    Interface.Oxide.LogError(str);
                }
            }
            else
                speedAttackDetections[attacker.userID] = 0;
            lastSpeedAttackAttackTime[attacker.userID] = timeNow;
        }

        object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            if (!wallHackPlayersEnabled || !isLoaded || (!playersHidenEntities.ContainsKey(target.userID) || !playersHidenEntities[target.userID].Contains(entity.net.ID)))
                return (object)null;
            return (object)false;
        }

        void OnEntitySpawned(BaseEntity ent, GameObject gameObject)
        {
            if (!wallHackObjectsEnabled || (ent as BasePlayer) != null || ent.GetComponent<LootContainer>() != null || ent.GetComponent<StorageContainer>() == null && !neededEntities.Contains(ent.ShortPrefabName))
                return;
            Chunk chunkFromPoint = GetChunkFromPoint(ent.transform.position);
            if (chunkFromPoint == null)
                return;
            chunkFromPoint.entities.Add(ent);
        }

        void OnEntityKill(BaseNetworkable ent)
        {
            if (!wallHackObjectsEnabled || !isLoaded || (ent as BasePlayer) != null || ent.GetComponent<LootContainer>() != null || ent.GetComponent<StorageContainer>() == null && !neededEntities.Contains(ent.ShortPrefabName))
                return;
            Chunk chunkFromPoint = GetChunkFromPoint(ent.transform.position);
            if (chunkFromPoint == null || !chunkFromPoint.entities.Contains(ent as BaseEntity))
                return;
            chunkFromPoint.entities.Remove(ent as BaseEntity);
        }

        private void Init()
        {
            instance = this;

            LoadVariables();
            if (wallHackObjectsEnabled)
            {
                radius = ConVar.Server.worldsize / 100;
                int num = 100;
                for (int index1 = num * -1; index1 < num; ++index1)
                {
                    chunks[index1] = new Dictionary<int, Chunk>();
                    for (int index2 = num * -1; index2 < num; ++index2)
                    {
                        Chunk chunk = new Chunk()
                        {
                            x = index1,
                            z = index2
                        };
                        chunks[index1][index2] = chunk;
                        chunksList.Add(chunk);
                    }
                }
                Puts(string.Format("[Debug WallHackHandler] Chunks: {0} Radius: {1} Map size: {2}", (object)chunks.Count, (object)radius, (object)ConVar.Server.worldsize));
            }
            db = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("AntiHack_Detects");
            Interface.Oxide.GetLibrary<Permission>(null).RegisterPermission("antihack.logs", (Plugin)this);
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddChatCommand("ah", (Plugin)this, "ShowLog");
            Interface.Oxide.GetLibrary<Game.Rust.Libraries.Command>(null).AddConsoleCommand("antihack", (Plugin)this, "AntiHackCmd");
        }

        void OnServerInitialized()
        {
            if (wallHackObjectsEnabled)
            {
                int num = 0;
                foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities)
                {
                    if (!((serverEntity as BasePlayer) != null) && ((!(serverEntity.GetComponent<StorageContainer>() == null) || neededEntities.Contains(serverEntity.ShortPrefabName)) && !(serverEntity.GetComponent<LootContainer>() != null)))
                    {
                        Chunk chunkFromPoint = GetChunkFromPoint(serverEntity.transform.position);
                        if (chunkFromPoint != null)
                            chunkFromPoint.entities.Add(serverEntity as BaseEntity);
                        ++num;
                    }
                }
                Interface.Oxide.LogInfo(string.Format("[Debug WallHackHandler] Added new {0} entities ({1} all)", (object)num, (object)GetEntitiesFromAllChunks().Count));
            }
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                SetPlayer(activePlayer);
            isLoaded = true;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            HackHandler component = player.GetComponent<HackHandler>();
            if (component == null)
                return;
            component.Disconnect();
        }

        void OnServerSave()
        {
            isSaving = true;
            NextTick(() => isSaving = false);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            SetPlayer(player);
        }

        void OnServerShutdown()
        {
            SaveData();
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                playersHandlers.Remove(player.userID);
                HackHandler component = player.GetComponent<HackHandler>();
                if (component != null)
                {
                    component.Disconnect();
                }
            }
            SaveData();
            DestroyAll<HackHandler>();
        }

        void DestroyAll<T>()
        {
            UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (UnityEngine.Object gameObj in objects)
                    GameObject.Destroy(gameObj);

        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (isSaving || attacker == null || (attacker.IsAdmin || info.Weapon == null))
                return;
            float realtimeSinceStartup = UnityEngine.Time.realtimeSinceStartup;
            BaseMelee component = info.Weapon.GetComponent<BaseMelee>();
            if (component == null && enableWallHackAttackLog)
            {
                ShootingThroughWallHanlder(attacker, info, realtimeSinceStartup);
            }
            else
            {
                if (!enableSpeedAttackLog)
                    return;
                SpeedAttackHandler(attacker, info, component, realtimeSinceStartup);
            }
        }

        private bool IsPlayerGotImmunity(ulong playerid = 3902464)
        {
            object obj = Interface.CallHook("AntiHackIsPlayerGotImmunity", (object)playerid);
            return obj != null && (bool)obj;
        }

        private class Chunk
        {
            public HashSet<BaseEntity> entities = new HashSet<BaseEntity>();

            public int x { get; set; }

            public int z { get; set; }
        }

        private class Log
        {
            public List<Detect> detects = new List<Detect>();
            public int detectsAmount;
            public ulong steamID;
            public string name;
        }

        public struct Coordinates
        {
            public string startPos;
            public string endPos;
        }

        public class Detect
        {
            public List<Coordinates> coordinates = new List<Coordinates>();
        }

        public class TemporaryCoordinates
        {
            public List<Coordinates> coordinates = new List<Coordinates>();
        }

        private class StoredData
        {
            public List<Log> logs = new List<Log>();
        }

        private class CurrentLog
        {
            public ulong steamID;
            public int detect;
        }

        private class HackHandler : MonoBehaviour
        {
            private int flyWarnings = 0;
            private int textureWarnings = 0;
            private int speedWarnings = 0;
            private float ownTickRate = 0.1f;
            private bool IsFlying = false;
            private int falseFlyDetects = 0;
            private int falseSpeedDetects = 0;
            private int ping = 0;
            private TemporaryCoordinates temp = new TemporaryCoordinates();
            public HashSet<BaseEntity> hidedEntities = new HashSet<BaseEntity>();
            public Dictionary<ulong, HashSet<BaseEntity>> hidedPlayersEntities = new Dictionary<ulong, HashSet<BaseEntity>>();
            public HashSet<BaseEntity> seenObjects = new HashSet<BaseEntity>();
            public Vector3 lastPosition = new Vector3();
            private bool IsHided = false;
            private bool isShowedAll = false;
            public BasePlayer player;
            private float lastTick;
            private float deltaTime;
            private float flyTime;
            private float flyTimeStart;
            public Vector3 lastGroundPosition;
            public Vector3 playerPreviousPosition;
            private Network.Connection connection;

            static Vector3 ENTlastGroundPosition;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                ownTickRate = UnityEngine.Random.Range(0.09f, 0.11f);
                playersHidenEntities[player.userID] = new HashSet<uint>();
                connection = player.net.connection;
                lastGroundPosition = player.transform.position;
                playerPreviousPosition = player.transform.position;
                if (!wallHackObjectsEnabled)
                    return;
                if (player.IsReceivingSnapshot || player.IsSleeping())
                    CheckSnapshot();
                else
                    HideAll();
            }

            private void Update()
            {
                if ((double)UnityEngine.Time.realtimeSinceStartup - (double)lastTick < (double)ownTickRate)
                    return;
                if (instance.IsPlayerGotImmunity(player.userID))
                {
                    ShowAllEntities();
                    lastPosition = player.GetNetworkPosition();
                    playerPreviousPosition = player.transform.position;
                    lastGroundPosition = player.transform.position;
                }
                else
                {
                    isShowedAll = false;
                    if (player.IsAdmin && instance.Admins.Contains(player))
                    {
                        WallHackHandler();
                        lastPosition = player.GetNetworkPosition();
                        playerPreviousPosition = player.transform.position;
                    }
                    else
                    {
                        CorrectValues();
                        lastPosition = player.GetNetworkPosition();
                        FlyHackHandler();
                        PlayerWallHackHandler();
                        PlayerWallHackHandlerSleepers();
                        SpeedHackHandler();
                        WallHackHandler();
                        if (player.IsSleeping() || player.IsDead())
                        {
                            playerPreviousPosition = player.transform.position;
                            lastGroundPosition = player.transform.position;
                        }
                        else
                        {
                            TextureHackHandler();
                            playerPreviousPosition = player.transform.position;
                        }
                    }
                }
            }

            private void SpeedHackHandler()
            {
                if (player.IsOnGround())
                {
                    if (enableFlyHackCar && player.GetMounted() != null) return;
                    if (player.GetParentEntity() is HotAirBalloon || player.GetParentEntity() is CargoShip) return;
                    RaycastHit hit;
                    if (Physics.Raycast(player.transform.position, Vector3.down, out hit, cM))
                    {
                        if (hit.transform.position != ENTlastGroundPosition) return;
                        ENTlastGroundPosition = hit.transform.position;
                    }

                    Vector3 position = player.transform.position;
                    if ((double)playerPreviousPosition.y - (double)position.y < 0.5)
                    {
                        float num = Vector3Ex.Distance2D(playerPreviousPosition, position);
                        float maxSpeed = (float)(((double)player.GetMaxSpeed() + 1.0) * (double)tickRate * (double)deltaTime * 1.54999995231628);
                        if ((double)num > (double)maxSpeed)
                        {
                            falseSpeedDetects = falseSpeedDetects + 1;
                            if (falseSpeedDetects <= 5)
                                return;
                            falseSpeedDetects = 0;
                            speedWarnings = speedWarnings + 1;
                            if (enableSpeedHackLog)
                            {
                                CreateLogSpeedHack(position, maxSpeed);
                                LogHandler(player, playerPreviousPosition, temp, true);
                            }
                            ReturnBack(playerPreviousPosition);
                            if (speedWarnings >= maxSpeedWarnings)
                                CrimeHandler("SpeedHack");
                            return;
                        }
                    }
                }
                falseSpeedDetects = 0;
            }
            public List<Blacklist> list = new List<Blacklist>();
            public class Blacklist
            {
                public Blacklist(string Player, string SteamId)
                {
                    this.Player =Player;
                    this.SteamId = SteamId;
                }
                public string Player { get; set; }
                public string SteamId { get; set; }


            }
            private void FlyHackHandler()
            {
                if (player.GetParentEntity() is HotAirBalloon || player.GetParentEntity() is CargoShip) return;

                Vector3 position = player.transform.position;
                if (player.IsOnGround() || (double)player.WaterFactor() > 0.0)
                {
                    lastGroundPosition = position;
                    Reset();
                    falseFlyDetects = 0;
                }
                else
                {
                    if (!IsFlying)
                    {
                        flyTimeStart = UnityEngine.Time.realtimeSinceStartup;
                        IsFlying = true;
                    }
                    flyTime = lastTick - flyTimeStart;
                    AddTemp();
                    if ((double)flyTime < 0.600000023841858 && (double)position.y - (double)lastGroundPosition.y < 3.0)
                        return;
                    float num1 = Vector3.Distance(position, lastGroundPosition);
                    float num2 = Vector3Ex.Distance2D(position, lastGroundPosition);
                    if ((double)num1 > 1.20000004768372 * (double)deltaTime && ((double)position.y - (double)lastGroundPosition.y > 1.20000004768372 || (double)num2 > 15.0) && (((double)playerPreviousPosition.y < (double)position.y || (double)num2 > 15.0) && (double)num1 > (double)Vector3.Distance(playerPreviousPosition, lastGroundPosition) && !UnityEngine.Physics.Raycast(position, Vector3.down, 1.2f)))
                    {
                        falseFlyDetects = falseFlyDetects + 1;
                        if (falseFlyDetects <= 5)
                            return;
                        falseFlyDetects = 0;
                        flyWarnings = flyWarnings + 1;
                        if (enableFlyHackLog)
                        {
                            LogHandler(player, lastGroundPosition, temp, false);
                            CreateLogFlyHack(position);
                        }
                        ReturnBack(lastGroundPosition);
                        if (flyWarnings >= maxFlyWarnings)
                            CrimeHandler("FlyHack");
                    }
                    else
                        falseFlyDetects = 0;
                }
            }

            private void TextureHackHandler()
            {
                Vector3 position = player.transform.position;
                foreach (RaycastHit hit in UnityEngine.Physics.RaycastAll(new Ray(position + Vector3.up * 10f, Vector3.down), 50f, globalMask))
                {
                    if (!(hit.collider == null))
                    {
                        if (hit.GetEntity() != null)
                        {
                            BaseEntity entity = hit.GetEntity();
                            if (IsInsideFoundation(entity))
                            {
                                textureWarnings = textureWarnings + 1;
                                if (enableTextureHackLog && textureWarnings % 50 == 0)
                                {
                                    CreateLogTextureHack(position, entity.ShortPrefabName);
                                }

                                ReturnBack(playerPreviousPosition);
                                break;
                            }
                        }
                        if ((!(hit.collider.name != "Mesh") || hit.collider.name.Contains("rock_small") || hit.collider.name.Contains("ores")) && IsInsideCave(hit.collider))
                        {
                            string objectName = hit.collider.name;
                            if (objectName == "Mesh")
                                objectName = "Rock";
                            textureWarnings = textureWarnings + 1;
                            if (enableTextureHackLog && textureWarnings % 20 == 0)
                            {
                                if (objectName.Contains("assets") && objectName.Length > 23)
                                    objectName = objectName.Remove(0, 23);
                                CreateLogTextureHack(position, objectName);
                            }
                            ReturnBack(playerPreviousPosition);
                            break;
                        }
                    }
                }
            }

            private bool IsInsideFoundation(BaseEntity block)
            {
                BuildingBlock buildingBlock = block as BuildingBlock;
                if (buildingBlock != null)
                {
                    if (!buildingBlock.PrefabName.Contains("foundation") || buildingBlock.PrefabName.Contains("foundation.steps") && buildingBlock.grade != BuildingGrade.Enum.TopTier || (buildingBlock.grade == BuildingGrade.Enum.Twigs || buildingBlock.grade == BuildingGrade.Enum.Wood))
                        return false;
                }
                else if (!block.PrefabName.Contains("wall.external"))
                    return false;
                OBB obb = block.WorldSpaceBounds();
                Vector3 center1 = obb.ToBounds().center;
                obb = player.WorldSpaceBounds();
                Vector3 center2 = obb.ToBounds().center;
                center2.y -= 0.7f;
                Vector3 direction = center1 - center2;
                RaycastHit hitInfo;
                return !UnityEngine.Physics.Raycast(new Ray(center2, direction), out hitInfo, direction.magnitude + 1f, cM);
            }

            private bool IsInsideCave(Collider collider)
            {
                Vector3 center1 = collider.bounds.center;
                Vector3 center2 = player.WorldSpaceBounds().ToBounds().center;
                Vector3 direction = center1 - center2;
                Ray ray = new Ray(center2, direction);
                RaycastHit hitInfo;
                return !collider.Raycast(ray, out hitInfo, direction.magnitude + 1f);
            }

            private void CheckSnapshot()
            {
                if (player.IsReceivingSnapshot || player.IsSleeping())
                    Invoke("CheckSnapshot", 0.1f);
                else
                    HideAll();
            }

            public void HideAll()
            {
                if (IsHided)
                    return;
                IsHided = true;
                foreach (BaseEntity entitiesFromAllChunk in GetEntitiesFromAllChunks())
                    Hide(entitiesFromAllChunk);
            }

            public void Hide(BaseEntity entity)
            {
                if (seenObjects.Contains(entity) || hidedEntities.Contains(entity))
                    return;
                if (Network.Net.sv.write.Start())
                {
                    Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                    Network.Net.sv.write.EntityID(entity.net.ID);
                    Network.Net.sv.write.UInt8((byte)0);
                    Network.Net.sv.write.Send(new SendInfo(connection));
                }
                hidedEntities.Add(entity);
            }

            private void Show(BaseEntity entity, bool needRemove = true)
            {
                seenObjects.Add(entity);
                if (!hidedEntities.Contains(entity))
                    return;
                if (needRemove)
                    hidedEntities.Remove(entity);
                player.QueueUpdate(BasePlayer.NetworkQueue.Update, (BaseNetworkable)entity);
            }

            private void ShowLines(Vector3 start, Vector3 target, bool isVisible)
            {
                if (!player.IsAdmin)
                    return;
                if (isVisible)
                    player.SendConsoleCommand("ddraw.arrow", (object)0.1f, (object)Color.blue, (object)start, (object)target, (object)0.1);
                else
                    player.SendConsoleCommand("ddraw.arrow", (object)0.1f, (object)Color.red, (object)start, (object)target, (object)0.1);
            }

            private bool TryLineCast(Vector3 start, Vector3 target, float plusTarget = 0.0f, float plusPlayer = 1.5f)
            {
                target.y += plusTarget;
                start.y += plusPlayer;
                return !UnityEngine.Physics.Linecast(start, target, cM);
            }

            private bool IsObjectVisible(Vector3 start, Vector3 target)
            {
                return TryLineCast(start, target, 0.0f, 1.5f) || (double)Vector3.Distance(start, target) <= 25.0 && (TryLineCast(start, target, 0.0f, 0.5f) || TryLineCast(start, target, 0.5f, 0.5f) || TryLineCast(start, target, 0.5f, 1f));
            }

            private void WallHackHandler()
            {
                if (!wallHackObjectsEnabled)
                    return;
                Vector3 position = player.transform.position;
                if ((double)Vector3.Distance(player.transform.position, playerPreviousPosition) < 1.0 / 1000.0)
                    return;
                foreach (BaseEntity entity in GetEntitiesFromChunksNearPointOptimized(position))
                {
                    if (!(entity == null) && !seenObjects.Contains(entity))
                    {
                        entity.WorldSpaceBounds().ToBounds();
                        if (IsObjectVisible(position, entity.WorldSpaceBounds().ToBounds().center))
                            Show(entity, true);
                    }
                }
            }

            private void PlayerWallHackHandlerSleepers()
            {
                if (!wallHackPlayersEnabled || (double)Vector3.Distance(player.transform.position, playerPreviousPosition) < 1.0 / 1000.0)
                    return;
                foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
                {
                    if (!(sleepingPlayer == player) && ((double)player.Distance((BaseEntity)sleepingPlayer) >= (double)minPlayersWallHackDistanceCheck && (double)player.Distance((BaseEntity)sleepingPlayer) <= (double)maxPlayersWallHackDistanceCheck && !seenObjects.Contains((BaseEntity)sleepingPlayer)))
                    {
                        if (!IsVisible((BaseEntity)sleepingPlayer, true))
                            HidePlayer(sleepingPlayer, true);
                        else
                            ShowPlayer(sleepingPlayer, true, true);
                    }
                }
            }

            private void PlayerWallHackHandler()
            {
                if (!wallHackPlayersEnabled)
                    return;
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (!(activePlayer == player) && ((double)player.Distance((BaseEntity)activePlayer) >= (double)minPlayersWallHackDistanceCheck && (double)player.Distance((BaseEntity)activePlayer) <= (double)maxPlayersWallHackDistanceCheck && !activePlayer.net.connection.ipaddress.StartsWith("127.0")) && ((double)Vector3.Distance(playersHandlers[activePlayer.userID].lastPosition, activePlayer.GetNetworkPosition()) < 1.0 / 1000.0 || (double)player.Distance((BaseEntity)activePlayer) > 50.0 || activePlayer.IsDucked()))
                    {
                        if (!IsVisible((BaseEntity)activePlayer, false))
                            HidePlayer(activePlayer, false);
                        else
                            ShowPlayer(activePlayer, true, false);
                    }
                }
            }

            private bool DoLine(Vector3 start, Vector3 target, float plusTarget = 0.0f, float plusPlayer = 1.5f)
            {
                target.y += plusTarget;
                start.y += plusPlayer;
                return !UnityEngine.Physics.Linecast(start, target, cM);
            }

            private bool IsBehindStairs(Vector3 start, Vector3 target)
            {
                RaycastHit hitInfo;
                if (UnityEngine.Physics.Linecast(start, target, out hitInfo, cM))
                {
                    BaseEntity entity1 = hitInfo.GetEntity();
                    if (entity1 != null && ((entity1.ShortPrefabName == "block.stair.lshape" || entity1.ShortPrefabName == "block.stair.ushape") && UnityEngine.Physics.Linecast(target, start, out hitInfo, cM)))
                    {
                        BaseEntity entity2 = hitInfo.GetEntity();
                        if (entity2 != null && (entity2.ShortPrefabName == "block.stair.lshape" || entity2.ShortPrefabName == "block.stair.ushape"))
                            return true;
                    }
                }
                return false;
            }

            private bool IsVisible(BaseEntity target, bool isSleeper = false)
            {
                Vector3 position1 = player.transform.position;
                Vector3 position2 = target.transform.position;
                if (isSleeper)
                    return DoLine(position1, position2, 0.0f, 1.5f) || IsBehindStairs(new Vector3(position1.x, position1.y + 1.2f, position1.z), new Vector3(position2.x, position2.y + 1.2f, position2.z));
                if ((target as BasePlayer).IsDucked())
                    position2.y -= 0.5f;
                float num = player.Distance(target);
                if (DoLine(position1, position2, 1.5f, 1.5f) || IsBehindStairs(new Vector3(position1.x, position1.y + 1.2f, position1.z), new Vector3(position2.x, position2.y + 1.2f, position2.z)))
                    return true;
                if ((double)num > 120.0)
                    return false;
                if (DoLine(position1, position2, 0.0f, 1.5f) || DoLine(position1, position2, 1.2f, 1.5f) || (DoLine(position1, position2, 0.9f, 1.5f) || DoLine(position1, position2, 0.5f, 1.5f)) || (DoLine(position1, position2, 1.9f, 1.5f) || DoLine(position1, position2, 1.5f, 0.0f)))
                    return true;
                if ((double)num > 75.0)
                    return false;
                bool flag1 = !UnityEngine.Physics.Linecast(position1, Quaternion.Euler(player.GetNetworkRotation().eulerAngles) * Vector3.left + position1, cM);
                bool flag2 = !UnityEngine.Physics.Linecast(position1, Quaternion.Euler(player.GetNetworkRotation().eulerAngles) * Vector3.right + position1, cM);
                return flag1 && DoLine(Quaternion.Euler(player.GetNetworkRotation().eulerAngles) * Vector3.left + position1, position2, 1.1f, 1.5f) || flag2 && DoLine(Quaternion.Euler(player.GetNetworkRotation().eulerAngles) * Vector3.right + position1, position2, 1.1f, 1.5f) || (flag1 && DoLine(Quaternion.Euler(player.GetNetworkRotation().eulerAngles) * Vector3.left + position1, position2, 1.1f, 1.1f) || flag2 && DoLine(Quaternion.Euler(player.GetNetworkRotation().eulerAngles) * Vector3.right + position1, position2, 1.1f, 1.1f));
            }

            private void ShowPlayer(BasePlayer target, bool needRemove = true, bool isSleeper = false)
            {
                if (isSleeper)
                    seenObjects.Add((BaseEntity)target);
                if (!hidedPlayersEntities.ContainsKey(target.userID))
                    return;
                player.QueueUpdate(BasePlayer.NetworkQueue.Update, (BaseNetworkable)target);
                player.QueueUpdate(BasePlayer.NetworkQueue.Update, target != null ? (BaseNetworkable)target.GetHeldEntity() : (BaseNetworkable)null);
                playersHidenEntities[player.userID].Remove(target.net.ID);
                foreach (BaseEntity baseEntity in hidedPlayersEntities[target.userID])
                {
                    if (!(baseEntity == null) && !baseEntity.IsDestroyed)
                    {
                        player.QueueUpdate(BasePlayer.NetworkQueue.Update, (BaseNetworkable)baseEntity);
                        playersHidenEntities[player.userID].Remove(baseEntity.net.ID);
                    }
                }
                if (needRemove)
                    hidedPlayersEntities.Remove(target.userID);
            }

            private void HidePlayer(BasePlayer target, bool isSleeper = false)
            {
                if (isSleeper)
                {
                    if (seenObjects.Contains((BaseEntity)target))
                        return;
                }
                else if (seenObjects.Contains((BaseEntity)target))
                    seenObjects.Remove((BaseEntity)target);
                if (hidedPlayersEntities.ContainsKey(target.userID))
                    return;
                hidedPlayersEntities[target.userID] = new HashSet<BaseEntity>();
                if (Network.Net.sv.write.Start())
                {
                    Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                    Network.Net.sv.write.EntityID(target.net.ID);
                    Network.Net.sv.write.UInt8((byte)0);
                    Network.Net.sv.write.Send(new SendInfo(connection));
                }
                Item activeItem = target.GetActiveItem();
                if ((activeItem != null ? activeItem.GetHeldEntity() : null) != null && Network.Net.sv.write.Start())
                {
                    Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                    Network.Net.sv.write.EntityID(activeItem.GetHeldEntity().net.ID);
                    Network.Net.sv.write.UInt8((byte)0);
                    Network.Net.sv.write.Send(new SendInfo(connection));
                    hidedPlayersEntities[target.userID].Add(activeItem.GetHeldEntity());
                    playersHidenEntities[player.userID].Add(activeItem.GetHeldEntity().net.ID);
                }
                HidePlayersHostile(target);
                hidedPlayersEntities[target.userID].Add((BaseEntity)target);
                playersHidenEntities[player.userID].Add(target.net.ID);
            }

            private void HidePlayersHostile(BasePlayer target)
            {
                foreach (Item obj in target.inventory.containerBelt.itemList)
                {
                    if (target.IsHostileItem(obj))
                    {
                        if (!((obj != null ? obj.GetHeldEntity() : null) == null))
                        {
                            if (Network.Net.sv.write.Start())
                            {
                                Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                                Network.Net.sv.write.EntityID(obj.GetHeldEntity().net.ID);
                                Network.Net.sv.write.UInt8((byte)0);
                                Network.Net.sv.write.Send(new SendInfo(connection));
                                hidedPlayersEntities[target.userID].Add(obj.GetHeldEntity());
                                playersHidenEntities[player.userID].Add(obj.GetHeldEntity().net.ID);
                            }
                        }
                        else
                            break;
                    }
                }
                foreach (Item obj in target.inventory.containerMain.itemList)
                {
                    if (target.IsHostileItem(obj))
                    {
                        if ((obj != null ? obj.GetHeldEntity() : null) == null)
                            break;
                        if (Network.Net.sv.write.Start())
                        {
                            Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                            Network.Net.sv.write.EntityID(obj.GetHeldEntity().net.ID);
                            Network.Net.sv.write.UInt8((byte)0);
                            Network.Net.sv.write.Send(new SendInfo(connection));
                            hidedPlayersEntities[target.userID].Add(obj.GetHeldEntity());
                            playersHidenEntities[player.userID].Add(obj.GetHeldEntity().net.ID);
                        }
                    }
                }
            }

            private void ShowAllEntities()
            {
                if (isShowedAll)
                    return;
                isShowedAll = true;
                playersHidenEntities[player.userID] = new HashSet<uint>();
                HashSet<BaseEntity> hidedEntities = this.hidedEntities;
                foreach (KeyValuePair<ulong, HashSet<BaseEntity>> hidedPlayersEntity in hidedPlayersEntities)
                    hidedEntities.UnionWith((IEnumerable<BaseEntity>)hidedPlayersEntity.Value);
                foreach (BaseEntity baseEntity in hidedEntities)
                {
                    if (!(baseEntity == null) && !baseEntity.IsDestroyed)
                        player.QueueUpdate(BasePlayer.NetworkQueue.Update, (BaseNetworkable)baseEntity);
                }
            }

            private void CorrectValues()
            {
                ping = Network.Net.sv.GetAveragePing(player.net.connection);
                if (ping == 0)
                    ping = 1;
                int frameRate = Performance.current.frameRate;
                float num = 1f;
                if (frameRate < 100)
                    num = 4f;
                if (frameRate < 50)
                    num = 6f;
                deltaTime = (float)(1.0 + (double)ping * 0.00400000018998981 + (((double)UnityEngine.Time.realtimeSinceStartup - (double)lastTick) * (double)num - (double)tickRate));
                lastTick = UnityEngine.Time.realtimeSinceStartup;
            }

            private void ReturnBack(Vector3 pos)
            {
                player.MovePosition(pos);
                Networkable net1 = player.net;
                if ((net1 != null ? net1.connection : (Network.Connection)null) != null)
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
                Networkable net2 = player.net;
                if ((net2 != null ? net2.connection : (Network.Connection)null) == null)
                    return;
                try
                {
                    player.ClearEntityQueue((Group)null);
                }
                catch
                {
                }
            }
            private void CreateLogFlyHack(Vector3 playerPosition)
            {
                string str = string.Format("FlyHack detected\n{0} [{1}]\nНачальная позиция: {2}\nКонечная позиция: {3}\nВремя в полете: {4} сек.\nДистанция: {5} м.\nПинг: {6} мс.\nПредупреждений: {7}", (object)player.displayName, (object)player.userID, (object)lastGroundPosition, (object)playerPosition, (object)string.Format("{0:0.##}", (object)flyTime), (object)string.Format("{0:0.##}", (object)Vector3.Distance(playerPosition, lastGroundPosition)), (object)ping, (object)flyWarnings);
                string strMessage = string.Format("FlyHack | {0} [{1}] | {2} -> {3} | Время: {4} сек. | Дистанция: {5} м. | {6} мс. | Предупреждений: {7}", (object)player.displayName, (object)player.userID, (object)lastGroundPosition, (object)playerPosition, (object)string.Format("{0:0.##}", (object)flyTime), (object)string.Format("{0:0.##}", (object)Vector3.Distance(playerPosition, lastGroundPosition)), (object)ping, (object)flyWarnings);
                Interface.Oxide.LogError(str);
                instance.DiscordMessages(str, 0);
                instance.LogToFile("Log", strMessage, instance);
                SendReportToOnlineModerators(str);
                var reply = 3811;
                if (reply == 0) { }
            }

            private void CreateLogSpeedHack(Vector3 playerPosition, float maxSpeed)
            {
                string str = string.Format("SpeedHack detected\n{0} [{1}]\nНачальная позиция: {2}\nКонечная позиция: {3}\nСкорость: {4} м/c (Максимально допустимая: {5} м/c).\nПинг: {6} мс.\nПредупреждений: {7}", (object)player.displayName, (object)player.userID, (object)playerPreviousPosition, (object)playerPosition, (object)string.Format("{0:0.##}", (object)(float)((double)Vector3.Distance(playerPosition, playerPreviousPosition) * 5.0)), (object)string.Format("{0:0.##}", (object)(float)((double)maxSpeed * 5.0)), (object)ping, (object)speedWarnings);
                string strMessage = string.Format("SpeedHack | {0} [{1}] | {2} -> {3} | Скорость: {4} м/c (Макс: {5} м/c).| {6} мс. | Предупреждений: {7}", (object)player.displayName, (object)player.userID, (object)playerPreviousPosition, (object)playerPosition, (object)string.Format("{0:0.##}", (object)(float)((double)Vector3.Distance(playerPosition, playerPreviousPosition) * 5.0)), (object)string.Format("{0:0.##}", (object)(float)((double)maxSpeed * 5.0)), (object)ping, (object)speedWarnings);
                Interface.Oxide.LogError(str);
                instance.DiscordMessages(str, 0);
                instance.LogToFile("Log", strMessage, instance);
                SendReportToOnlineModerators(str);
            }

            private void CreateLogTextureHack(Vector3 playerPosition, string objectName)
            {
                string str = string.Format("TextureHack detected\n{0} [{1}]\nПозиция: {2}\nОбъект: {3}\nПинг: {4} мс.\nПопыток: {5}", (object)player.displayName, (object)player.userID, (object)playerPosition, (object)objectName, (object)ping, (object)textureWarnings);
                string strMessage = string.Format("TextureHack | {0} [{1}] | {2} | Объект: {3} | {4} мс. | Попыток: {5}", (object)player.displayName, (object)player.userID, (object)playerPosition, (object)objectName, (object)ping, (object)textureWarnings);
                string reason = string.Format("AntiHack: TextureHack");
                Interface.Oxide.LogError(str);
                instance.DiscordMessages(str, 0);
                instance.LogToFile("Log", strMessage, instance);
                if (needKickEndKill)
                {
                    instance.Kick(player, $"{reason}");
                    player.KillMessage();
                }
                SendReportToOnlineModerators(str);
            }

            private void AddTemp()
            {
                Coordinates coordinates;
                coordinates.startPos = playerPreviousPosition.ToString();
                coordinates.endPos = player.transform.position.ToString();
                temp.coordinates.Add(coordinates);
            }

            private void Reset()
            {
                temp.coordinates.Clear();
                IsFlying = false;
            }

            private void CrimeHandler(string reason)
            {
                if (needBan)
                {
                    ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, string.Format("ban {0} {1}", (object)player.userID, (object)reason));
                    var str = $"Игрок: {player.userID} забанен! Причина: {reason}";
                    instance.DiscordMessages(str, 1, "BAN");
                }
                if (needKick)
                {
                    ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, string.Format("kick {0} {1}", (object)player.userID, (object)reason));
                    var str = $"Игрок: {player.userID} кикнут! Причина: {reason}";
                    instance.DiscordMessages(str, 1, "KICK");
                }
            }

            public void Disconnect()
            {
                if (playersHandlers.ContainsKey(player.userID))
                    playersHandlers.Remove(player.userID);
                Destroy(this);
            }
            public void ShowAllPlayers()
            {
                foreach (HashSet<BaseEntity> baseEntitySet in hidedPlayersEntities.Values)
                {
                    foreach (BaseNetworkable ent in baseEntitySet)
                        player.QueueUpdate(BasePlayer.NetworkQueue.Update, ent);
                }
            }

            public void Destroy()
            {
                foreach (BaseEntity hidedEntity in hidedEntities)
                {
                    if ((hidedEntity as BasePlayer) != null)
                        ShowPlayer(hidedEntity as BasePlayer, false, false);
                    else
                        Show(hidedEntity, false);
                }
                playersHandlers.Remove(player.userID);
                Destroy(this);
            }
        }
    }
}
                             