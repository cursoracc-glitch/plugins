using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections;
using Oxide.Game.Rust.Cui;
using UI = Oxide.Plugins.AimTrainUI.UIMethods;
using Anchor = Oxide.Plugins.AimTrainUI.Anchor;

namespace Oxide.Plugins
{
    [Info("AimTrain", "iAciid", "1.1.2")]
    public class AimTrain : RustPlugin
    {
        #region Declaration

        [PluginReference]
        Plugin Kits, NoEscape;
        Configuration config;
        StoredData storedData;
        Dictionary<string, Arena> ArenasCache = new Dictionary<string, Arena>();
        Dictionary<ulong, PlayerData> PlayersCache = new Dictionary<ulong, PlayerData>();
        Dictionary<ulong, string> EditArena = new Dictionary<ulong, string>();
        List<ulong> NoAmmo = new List<ulong>();
        public static AimTrain Instance;
        string MainContainer = "Main.Container";
        string StatsContainer = "Stats.Container";
        CuiElementContainer CachedContainer;

        #endregion

        #region Config

        public class Configuration
        {
            [JsonProperty(PropertyName = "Enable AimTrain")]
            public bool EnableAimTrain = true;
            [JsonProperty(PropertyName = "Use permissons for Arena")]
            public bool ArenaPermission = false;
            [JsonProperty(PropertyName = "Needs empty inventory to join")]
            public bool IgnoreInv = true;
            [JsonProperty(PropertyName = "Enable UI")]
            public bool EnableUI = true;
            [JsonProperty(PropertyName = "Use NoEscape Raid/Combatblock")]
            public bool UseNoEscape = false;
            [JsonProperty(PropertyName = "Bot Names")]
            public List<string> BotNames;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    BotNames = new List<string>()
                    {
                        "Bot1",
                        "Bot2",
                        "Bot3"
                    },
                };
            }

        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ClearInv"] = "Please clear your inventory before you join AimTrain.",
                ["JoinAT"] = "You've joined AimTrain! Use /at to leave.",
                ["LeaveAT"] = "You've left AimTrain.",
                ["SpawnBot"] = "Created a new Bot spawn point({0}).",
                ["SpawnPlayer"] = "Created the spawn point for players({0}).",
                ["ATAdmin"] = string.Join("\n", new[]{
                    "<size=16><color=#4286f4>AimTrain</color></size>",
                    "/aimtrain add <name> - <i>Create a Arena</i>",
                    "/at_edit <name> - <i>to edit a Arena</i>",
                    "/aimtrain delete <name> - <i>Delete a Arena</i>",
                    "/aimtrain list - <i>See all Arenas</i>",
                    "/aimtrain info <name> - <i>See the settings for your Arena</i>",
                    "/aimtrain sbot - <i>Set spawn points for bots</i>",
                    "/aimtrain splayer - <i>Set spawn points for players)</i>",
                    "/aimtrain botkit <name> - <i>Add a kit for the Bots</i>",
                    "/aimtrain playerkit <name> - <i>Add a kit for the players</i>",
                    "/aimtrain movement - <i>Enable bot moving</i>",
                    "/aimtrain enable - <i>Enable AimTrain</i>",
                    "/aimtrain botcount <amount> - <i>Change the amount of bots in the Arena</i>"}),
                ["EnableAimTrain"] = "AimTrain enabled: {0}",
                ["ErrorSpawns"] = "No spawn points for players set.",
                ["ErrorSpawnsbot"] = "You don't have spawn points for bots set {0} / 2.",
                ["CantWhileAimTrain"] = "You cant perform this action while you are in AimTrain.",
                ["NoPerm"] = "You don't have permissions to join this Arena!",
                ["ArenaExist"] = "This Arena already exists!",
                ["ArenaNotExisting"] = "This Arena doesn't exist, use /aimtrain add to create a Arena.",
                ["EditArena"] = "You are now editing Arena: {0}.",
                ["NotEditingArena"] = "You aren't editing a Arena, use /at_edit <name> in order to do so",
                ["ArenaCreated"] = "You created a new Arena called: {0}.",
                ["ArenaDeleted"] = "You deleted the Arena: {0}.",
                ["InvalidName"] = "Not a valid Arena Name.",
                ["ClearBotKit"] = "You cleared all Bot Kits.",
                ["ClearBotSpawns"] = "You cleared all spawn points for the Bots!",
                ["ClearPlayerSpawns"] = "You cleared all spawn points for the Players!",
                ["AddedBotKit"] = "You added the Kit <i>{0}</i> to the Bot Kits.",
                ["AddedPlayerKit"] = "You changed the Player Kit to <i>{0}</i>."
            }, this);
        }

        #endregion

        #region Data

        public class Position
        {
            public float PosX;
            public float PosY;
            public float PosZ;

            public Position()
            {
            }

            public Position(float x, float y, float z)
            {
                PosX = x;
                PosY = y;
                PosZ = z;
            }

            public Vector3 ToVector3()
            {
                return new Vector3(PosX, PosY, PosZ);
            }
        }

        public class Arena
        {
            public bool Enabled = true;
            public string PlayerKit = "Player Kit";
            public List<string> Kits = new List<string>();
            public int BotCount = 5;
            public bool BotMoving = true;
            public Dictionary<int, Position> SpawnsBot = new Dictionary<int, Position>();
            public Dictionary<int, Position> SpawnsPlayer = new Dictionary<int, Position>();

            [JsonIgnore]
            public int Players;

            public Arena()
            {
            }
        }

        public class PlayerData
        {
            public string Arena;
            public Position position;
            public int Hits;
            public int Bullets;
            public int Headshots;

            public PlayerData()
            {
            }
        }

        public class StoredData
        {
            public Dictionary<string, Arena> Arenas = new Dictionary<string, Arena>();

            public StoredData()
            {
            }
        }

        #endregion

        #region Functions

        void ClearBots(string arenaName)
        {
            foreach (var player in UnityEngine.Object.FindObjectsOfType<BasePlayer>())
                if (player.GetComponent<Bot>() != null && player.GetComponent<Bot>().arena == arenaName)
                    player.Kill();
        }

        void DeleteAll<T>() where T : MonoBehaviour
        {
            foreach (var type in UnityEngine.Object.FindObjectsOfType<T>())
                GameObject.Destroy(type);
        }

        void SaveCacheData()
        {
            storedData.Arenas = ArenasCache;
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        IEnumerator ChangeBotAmount(int bots, string arenaName)
        {
            for (var i = 0; i < bots; i++)
            {
                if (ArenasCache[arenaName].Players == 0)
                    yield break;
                CreateBot(arenaName);
                yield return new WaitForSeconds(1f);
            }
        }

        void MovePlayer(BasePlayer player, Vector3 pos)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.Teleport(pos);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading");
            player.SendFullSnapshot();
        }

        void ChangeBotCount(int amount, string arenaName)
        {           
            ClearBots(arenaName);
            if (amount == 0)
                return;
            ServerMgr.Instance.StartCoroutine(ChangeBotAmount(amount, arenaName));
        }

        void CreateBot(string arenaName)
        {
            var spawnPosition = UnityEngine.Random.Range(1, ArenasCache[arenaName].SpawnsBot.Count);
            var newBot = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", ArenasCache[arenaName].SpawnsBot[spawnPosition].ToVector3(), Quaternion.identity);
            newBot.Spawn();
            var botMover = newBot.GetComponent<Bot>() ?? newBot.gameObject.AddComponent<Bot>();
            botMover.arena = arenaName;
            var Random = UnityEngine.Random.Range(0, 2);
            if (Random.Equals(1))
            {
                botMover.minSpeed = 5.5f;
                botMover.maxSpeed = 5.5f;
            }
            else
            {
                botMover.minSpeed = 2.4f;
                botMover.maxSpeed = 2.4f;
            }
            newBot.SetFlag(BaseEntity.Flags.Reserved1, true);
            Kits?.Call("GiveKit", newBot, ArenasCache[arenaName].Kits.GetRandom());
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void JoinAT(BasePlayer player, string arenaName)
        {
            if (ArenasCache[arenaName].SpawnsPlayer.Count == 0)
            {
                PrintWarning("No player spawn points set");
                SendReply(player, Lang("ErrorSpawns"));
                return;
            }
            ArenasCache[arenaName].Players++;
            int randomSpawn = UnityEngine.Random.Range(1, ArenasCache[arenaName].SpawnsPlayer.Count);
            PlayersCache.Add(player.userID, new PlayerData());
            if (ArenasCache[arenaName].Players == 1)
                ChangeBotCount(ArenasCache[arenaName].BotCount, arenaName);
            PlayersCache[player.userID].Arena = arenaName;
            PlayersCache[player.userID].position = new Position(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            MovePlayer(player, ArenasCache[arenaName].SpawnsPlayer[randomSpawn].ToVector3());
            if (config.EnableUI)
            {
                CuiHelper.AddUi(player, CachedContainer);
                UpdateTimer(player);
            }
            if (!config.IgnoreInv)
                StripPlayer(player);
            player.limitNetworking = true;
            foreach (var players in BasePlayer.activePlayerList)
                player.SendNetworkUpdate();
            SendReply(player, Lang("JoinAT"));
            Kits?.Call("GiveKit", player, ArenasCache[arenaName].PlayerKit);
        }

        void LeaveAT(BasePlayer player)
        {
            string arenaName = PlayersCache[player.userID].Arena;
            StripPlayer(player);
            MovePlayer(player, PlayersCache[player.userID].position.ToVector3());
            player.limitNetworking = false;
            foreach (var players in BasePlayer.activePlayerList)
                player.SendNetworkUpdate();
            ArenasCache[arenaName].Players--;
            PlayersCache.Remove(player.userID);
            if (ArenasCache[arenaName].Players == 0)
            {
                ServerMgr.Instance.StopCoroutine("ChangeBotAmount");
                ClearBots(arenaName);
            }
            if (config.EnableUI)
            {
                CuiHelper.DestroyUi(player, MainContainer);
                CuiHelper.DestroyUi(player, StatsContainer);
                if (NoAmmo.Contains(player.userID))
                    NoAmmo.Remove(player.userID);
            }
            SendReply(player, Lang("LeaveAT"));
        }

        void StripPlayer(BasePlayer player)
        {
            StripContainer(player.inventory.containerBelt);
            StripContainer(player.inventory.containerMain);
            StripContainer(player.inventory.containerWear);
        }

        void StripContainer(ItemContainer container)
        {
            foreach (var item in container.itemList.ToList())
            {
                item.RemoveFromWorld();
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        string AmmoStatus(ulong playerID)
        {
            string ammoStatus = "";
            if (NoAmmo.Contains(playerID))
                ammoStatus = "Unlimited Ammo: OFF";
            else
                ammoStatus = "Unlimited Ammo: ON";
            return
                ammoStatus;
        }

        bool IsAimTraining(ulong playerID)
        {
            if (PlayersCache.ContainsKey(playerID))
                return true;
            else return false;
        }
        #endregion

        #region Hooks

        void Init()
        {
            DeleteAll<Bot>();
            Instance = this;
            LoadConfig();
            if(config.EnableUI)
                ConstructUi();
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            ArenasCache = storedData.Arenas;
            permission.RegisterPermission("aimtrain.join", this);
            permission.RegisterPermission("aimtrain.admin", this);
            if(config.ArenaPermission)
            {
                foreach (var arena in ArenasCache.Keys)
                {
                    permission.RegisterPermission($"aimtrain.{arena}", this);
                    Puts("Added extra permission: " + $"aimtrain.{arena}");
                }
            }
            if(config.EnableUI)
                timer.Repeat(2f, 0, () =>
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        if (PlayersCache.ContainsKey(player.userID))
                            UpdateTimer(player);
                });
        }

        object CanTrade(BasePlayer player)
        {
            if (PlayersCache.ContainsKey(player.userID))
                return Lang("CantWhileAimTrain");
            return null;
        }

        object CanTeleport(BasePlayer player)
        {
            if (PlayersCache.ContainsKey(player.userID))
                return Lang("CantWhileAimTrain");
            return null;
        }

        object CanBank(BasePlayer player)
        {
            if (PlayersCache.ContainsKey(player.userID))
                return Lang("CantWhileAimTrain");
            return null;
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if(PlayersCache.ContainsKey(attacker.userID) && info.HitEntity is BasePlayer)
            {
                PlayersCache[attacker.userID].Hits++;
                if(info.isHeadshot)
                    PlayersCache[attacker.userID].Headshots++;
            }
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (player == null)
                return;
            if(PlayersCache.ContainsKey(player.userID) && config.EnableUI)
                PlayersCache[player.userID].Bullets++;
            if (!PlayersCache.ContainsKey(player.userID) || NoAmmo.Contains(player.userID))
                return;
            projectile.GetItem().condition = projectile.GetItem().info.condition.max;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();       
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item == null)
                return;
            var player = item.GetOwnerPlayer();
            if (player == null)
                return;
            if (PlayersCache.ContainsKey(player.userID))
                item.Remove();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (PlayersCache.ContainsKey(player.userID))
                LeaveAT(player);
        }

        object OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null)
                return null;
            if (player.GetComponent<Bot>() != null && player.HasFlag(BaseEntity.Flags.Reserved1))
            {
                player.health = 100;
                var botPlayer = player.gameObject.GetComponent<Bot>();
                var spawnPosition = UnityEngine.Random.Range(1, ArenasCache[botPlayer.arena].SpawnsBot.Keys.Count);
                botPlayer._isLerping = false;
                StripPlayer(player);
                player.Teleport(ArenasCache[botPlayer.arena].SpawnsBot[spawnPosition].ToVector3());
                Kits?.Call("GiveKit", player, ArenasCache[botPlayer.arena].Kits.GetRandom());
                var Random = UnityEngine.Random.Range(0, 2);
                if (Random.Equals(1))
                {
                    botPlayer.minSpeed = 5.5f;
                    botPlayer.maxSpeed = 5.5f;
                }
                else
                {
                    botPlayer.minSpeed = 2.4f;
                    botPlayer.maxSpeed = 2.4f;
                }
                return false;
            }
            if (PlayersCache.ContainsKey(player.userID))
                LeaveAT(player);
            return null;
        }

        object CanBeWounded(BasePlayer player, HitInfo info)
        {
            if (player.GetComponent<Bot>() != null && player.HasFlag(BaseEntity.Flags.Reserved1))
                return false;
            return null;
        }

        void OnServerSave() => SaveCacheData();

        void Unload()
        {
            foreach (var _player in BasePlayer.activePlayerList)
            {
                if (PlayersCache.ContainsKey(_player.userID))
                {
                    LeaveAT(_player);
                    if(config.EnableUI)
                    {
                        CuiHelper.DestroyUi(_player, MainContainer);
                        CuiHelper.DestroyUi(_player, StatsContainer);
                    }
                }
            }
            foreach (var arena in ArenasCache)
            {
                arena.Value.Players = 0;
                ClearBots(arena.Key);
            }
            ServerMgr.Instance.StopAllCoroutines();
            SaveCacheData();
            DeleteAll<Bot>();
        }

        #endregion

        #region Bot Class

        public class Bot : MonoBehaviour
        {
            BasePlayer bot;
            public Boolean _isLerping;
            Vector3 startPos;
            Vector3 endPos;
            float timeTakenDuringLerp = 0f;
            public float minSpeed;
            public float maxSpeed;
            float lastDelta = 0f;
            public string arena;

            void SetViewAngle(Quaternion viewAngles)
            {
                if (viewAngles.eulerAngles == default(Vector3))
                    return;
                bot.OverrideViewAngles(viewAngles.eulerAngles);
                bot.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }

            void Start()
            {
                bot = GetComponent<BasePlayer>();
                bot.InitializeHealth(100, 100);
                bot.displayName = Instance.config.BotNames.GetRandom();
                StartLerping();
            }

            void StartLerping()
            {
                if (Instance.ArenasCache[arena].SpawnsBot.Count <= 1)
                {
                    _isLerping = false;
                    return;
                }
                if (Instance.ArenasCache[arena].SpawnsBot.Keys.Count > 1)
                {
                    var spawnPoint = Instance.ArenasCache[arena].SpawnsBot.ElementAt(UnityEngine.Random.Range(1, Instance.ArenasCache[arena].SpawnsBot.Keys.Count));
                    endPos = new Vector3(spawnPoint.Value.PosX, spawnPoint.Value.PosY, spawnPoint.Value.PosZ);
                    startPos = transform.position;
                    if (endPos != bot.transform.position)
                        SetViewAngle(Quaternion.LookRotation(endPos - bot.transform.position));
                    float distanceToDestination = Vector3.Distance(startPos, endPos);
                    timeTakenDuringLerp = distanceToDestination / UnityEngine.Random.Range(minSpeed, maxSpeed);
                    lastDelta = 0.0f;
                    _isLerping = true;
                }
            }

            public float GetGroundY(Vector3 position)
            {
                RaycastHit hitinfo;
                if (Physics.Raycast(position + Vector3.up, new Vector3(0f, -1f, 0f), out hitinfo, 100f, LayerMask.GetMask("Construction", "Clutter", "World")))
                {
                    var posY = Math.Max(hitinfo.point.y, TerrainMeta.HeightMap.GetHeight(position));
                    return posY;
                }
                float height = TerrainMeta.HeightMap.GetHeight(position);
                Vector3 pos = new Vector3(position.x, height, position.z);
                return pos.y;
            }

            void FixedUpdate()
            {
                if (!Instance.ArenasCache[arena].BotMoving)
                    return;

                if (_isLerping)
                {
                    lastDelta += Time.deltaTime;
                    float pct = lastDelta / timeTakenDuringLerp;
                    Vector3 nextPos = Vector3.Lerp(startPos, endPos, pct);
                    nextPos.y = GetGroundY(nextPos);
                    bot.MovePosition(nextPos);
                    bot.UpdatePlayerCollider(true);
                    if (pct >= 1.0f)
                    {
                        _isLerping = false;
                        StartLerping();
                    }
                }
                else
                {
                    if (Instance.ArenasCache[arena].SpawnsBot.Keys.Count > 1)
                        StartLerping();
                }
            }
        }

        #endregion

        #region GUI       

        void UpdateTimer(BasePlayer player)
        {
            var container = DrawTimer(player);
            CuiHelper.DestroyUi(player, StatsContainer);
            CuiHelper.AddUi(player, container);
        }

        void NewBorder(Anchor Min, Anchor Max) => UI.Border(Min, Max, ref CachedContainer, 0.001f, "1 1 1 1", MainContainer);

        CuiElementContainer DrawTimer(BasePlayer player)
        {
            int percentComplete = 0;
            if (PlayersCache[player.userID].Bullets != 0 && PlayersCache[player.userID].Hits != 0)
                percentComplete = (int)Math.Round((double)(100 * PlayersCache[player.userID].Hits) / PlayersCache[player.userID].Bullets);
            var container = UI.Container(StatsContainer, "0 0 0 0", new Anchor(0f, 0.35f), new Anchor(0.1f, 0.6f), "Hud.Menu");
            UI.Text("", StatsContainer, ref container, TextAnchor.MiddleRight, "0 0 0 1", 13, PlayersCache[player.userID].Headshots.ToString() + " ", new Anchor(0f, 0.8f), new Anchor(0.99f, 0.9f));
            UI.Text("", StatsContainer, ref container, TextAnchor.MiddleRight, "0 0 0 1", 13, PlayersCache[player.userID].Bullets.ToString() + " ", new Anchor(0f, 0.7f), new Anchor(0.99f, 0.8f));
            UI.Text("", StatsContainer, ref container, TextAnchor.MiddleRight, "0 0 0 1", 13, PlayersCache[player.userID].Hits.ToString() + " ", new Anchor(0f, 0.6f), new Anchor(0.99f, 0.7f));
            UI.Text("", StatsContainer, ref container, TextAnchor.MiddleRight, "0 0 0 1", 13, percentComplete.ToString() + "%" + " ", new Anchor(0f, 0.5f), new Anchor(0.99f, 0.6f));
            UI.Text("", StatsContainer, ref container, TextAnchor.MiddleLeft, "0 0 0 1", 13, "  " + AmmoStatus(player.userID), new Anchor(0f, 0.4f), new Anchor(0.99f, 0.5f));
            return container;
        }

        void ConstructUi()
        {
            CachedContainer = UI.Container(MainContainer, "0 0 0 0", new Anchor(0f, 0.35f), new Anchor(0.1f, 0.6f), "Overlay");
            UI.Text("Main.Name", MainContainer, ref CachedContainer, TextAnchor.MiddleCenter, "0 0 0 1", 15, "AimTrain", new Anchor(0f, 0.9f), new Anchor(1f, 1f));
            UI.Text("", MainContainer, ref CachedContainer, TextAnchor.MiddleLeft, "0 0 0 1", 13, "  Headshots: ", new Anchor(0f, 0.8f), new Anchor(0.99f, 0.9f));
            UI.Text("", MainContainer, ref CachedContainer, TextAnchor.MiddleLeft, "0 0 0 1", 13, "  Bullets fired: ", new Anchor(0f, 0.7f), new Anchor(0.99f, 0.8f));
            UI.Text("", MainContainer, ref CachedContainer, TextAnchor.MiddleLeft, "0 0 0 1", 13, "  Hits: ", new Anchor(0f, 0.6f), new Anchor(0.99f, 0.7f));
            UI.Text("", MainContainer, ref CachedContainer, TextAnchor.MiddleLeft, "0 0 0 1", 13, "  Accuracy: ", new Anchor(0f, 0.5f), new Anchor(0.99f, 0.6f));
            UI.Button("Button.Reset", MainContainer, ref CachedContainer, new Anchor(0f, 0.27f), new Anchor(0.99f, 0.4f), $"global.ResetAT", "Reset", "0 0 0", 12, "0 0 0 0");
            UI.Button("Button.Ammo", MainContainer, ref CachedContainer, new Anchor(0f, 0.14f), new Anchor(0.99f, 0.27f), $"global.AmmoAT", "Toggle Ammo", "0 0 0", 12, "0 0 0 0");
            UI.Button("Button.Leave", MainContainer, ref CachedContainer, new Anchor(0f, 0f), new Anchor(0.99f, 0.14f), $"global.LeaveAT", "Leave", "0 0 0", 12, "0 0 0 0");
            NewBorder(new Anchor(0f, 0.27f), new Anchor(0.99f, 0.4f));
            NewBorder(new Anchor(0f, 0.14f), new Anchor(0.99f, 0.27f));
            NewBorder(new Anchor(0f, 0f), new Anchor(1f, 1f));
            UI.Element("", MainContainer, ref CachedContainer, new Anchor(0.993f, 0f), new Anchor(0.99f, 1f), "1 1 1 1");
        }

        #endregion

        #region Commands

        [ChatCommand("at")]
        void cmdAT(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "aimtrain.join") || !config.EnableAimTrain)
                return;
            if(config.UseNoEscape)
            {
                if (plugins.Exists("NoEscape") && (bool)NoEscape?.Call("IsCombatBlocked", player) || plugins.Exists("NoEscape") && (bool)NoEscape?.Call("IsRaidBlocked", player))
                    return;
            }
            if (!(player.inventory.AllItems().Length == 0) && !PlayersCache.ContainsKey(player.userID) && config.IgnoreInv)
            {
                SendReply(player, Lang("ClearInv"));
                return;
            }
            if (PlayersCache.ContainsKey(player.userID))
            {
                LeaveAT(player);
                return;
            }
            if (ArenasCache.Keys.Count == 1)
            {
                var arena = ArenasCache.Keys.First();
                if (!ArenasCache[arena].SpawnsPlayer.Count.Equals(0) && !ArenasCache[arena].SpawnsBot.Count.Equals(0))
                {
                    if(config.ArenaPermission)
                    {
                        if(permission.UserHasPermission(player.UserIDString, "aimtrain." + arena))
                        {
                            JoinAT(player, arena);
                            return;
                        }
                        else
                        {
                            SendReply(player, Lang("NoPerm"));
                            return;
                        }
                    }
                    else
                    {
                        JoinAT(player, arena);
                        return;
                    }
                }
                return;
            }
            if (args.Length < 1)
                return;
            var arenaName = args[0];
            if(arenaName == null || !ArenasCache.ContainsKey(arenaName))
            {
                SendReply(player, Lang("ArenaNotExisting"));
                return;
            }
            if (ArenasCache.ContainsKey(arenaName) && !ArenasCache[arenaName].SpawnsBot.Equals(0) && !ArenasCache[arenaName].SpawnsPlayer.Equals(0))
            {
                if (config.ArenaPermission)
                {
                    if (permission.UserHasPermission(player.UserIDString, "aimtrain." + arenaName))
                    {
                        JoinAT(player, arenaName);
                        return;
                    }
                    else
                    {
                        SendReply(player, Lang("NoPerm"));
                        return;
                    }
                }
                else
                    JoinAT(player, arenaName);
            }
        }

        [ChatCommand("at_edit")]
        void cmdATEdit(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, "aimtrain.admin") || args[0] == null || args.Length < 1)
                return;
            var arenaName = args[0];
            if (ArenasCache.ContainsKey(arenaName))
            {
                if (EditArena.ContainsKey(player.userID))
                {
                    EditArena[player.userID] = arenaName;
                    SendReply(player, Lang("EditArena", null, arenaName));
                }
                else
                {
                    EditArena.Add(player.userID, arenaName);
                    SendReply(player, Lang("EditArena", null, arenaName));
                }
            }
            else SendReply(player, Lang("ArenaNotExisting"));
        }

        [ChatCommand("aimtrain")]
        void cmdAimTrain(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "aimtrain.admin"))
                return;
            if (args.Length < 1)
            {
                SendReply(player, Lang("ATAdmin"));
                return;
            }
            switch (args[0])
            {
                case "add":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, Lang("InvalidName"));
                            return;
                        }
                        var arenaName = args[1];
                        if (ArenasCache.ContainsKey(arenaName))
                        {
                            SendReply(player, Lang("ArenaExist"));
                            return;
                        }
                        ArenasCache.Add(arenaName, new Arena());
                        if (config.ArenaPermission)
                            permission.RegisterPermission($"aimtrain.{arenaName}", this);
                        SendReply(player, Lang("ArenaCreated", null, arenaName));
                    }
                    break;
                case "delete":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, Lang("InvalidName"));
                            return;
                        }
                        var arenaName = args[1];
                        if (!ArenasCache.ContainsKey(arenaName))
                        {
                            SendReply(player, Lang("ArenaNotExisting"));
                            return;
                        }
                        foreach (var _player in PlayersCache.Keys.ToList())
                        {
                            if (PlayersCache[_player].Arena == arenaName)
                                LeaveAT(BasePlayer.FindByID(_player));
                        }
                        SendReply(player, Lang("ArenaDeleted", null, arenaName));
                        ArenasCache.Remove(arenaName);

                    }
                    break;
                case "info":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, Lang("InvalidName"));
                            return;
                        }
                        var arenaName = args[1];
                        if (!ArenasCache.ContainsKey(arenaName))
                        {
                            SendReply(player, Lang("ArenaNotExisting"));
                            return;
                        }
                        SendReply(player, string.Join("\n", new[]
                        {
                        $"<size=16><color=#4286f4>AimTrain</color></size> Arena: <i>{arenaName}</i>",
                        $"Bot Kits: " +  string.Join(", ", ArenasCache[arenaName].Kits.ToArray()),
                        $"Player Kit: {ArenasCache[arenaName].PlayerKit}",
                        $"Bot Spawns: {ArenasCache[arenaName].SpawnsBot.Count.ToString()}",
                        $"Player Spawns: {ArenasCache[arenaName].SpawnsPlayer.Count.ToString()}",
                        $"Enabled: {ArenasCache[arenaName].Enabled}",
                        $"Movement: {ArenasCache[arenaName].BotMoving}"
                        }));
                    }
                    break;
                case "list":
                    {
                        var arenas = new List<string>();
                        foreach (var arena in ArenasCache)
                            arenas.Add(arena.Key);
                        SendReply(player, "<size=16><color=#4286f4>AimTrain</color></size> Arenas:\n" + string.Join("\n", arenas.ToArray()));
                    }
                    break;
                case "botkit":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, Lang("InvalidName"));
                            return;
                        }
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        var kitName = args[1];
                        if(kitName == "clear")
                        {
                            ArenasCache[arenaEdit].Kits.Clear();
                            SendReply(player, Lang("ClearBotKit", null, arenaEdit));
                            return;
                        }
                        if (!ArenasCache[arenaEdit].Kits.Contains(kitName))
                        {
                            SendReply(player, Lang("AddedBotKit", null, kitName));
                            ArenasCache[arenaEdit].Kits.Add(kitName);
                        }
                    }
                    break;
                case "playerkit":
                    {
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        if (args.Length < 2)
                        {
                            SendReply(player, Lang("InvalidName"));
                            return;
                        }
                        var kitName = args[1];
                        SendReply(player, Lang("AddedPlayerKit", null, kitName));
                        ArenasCache[arenaEdit].PlayerKit = kitName;
                    }
                    break;
                case "sbot":
                    {
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        if (args.Length < 2)
                        {
                            ArenasCache[arenaEdit].SpawnsBot[ArenasCache[arenaEdit].SpawnsBot.Count + 1] = new Position(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                            SendReply(player, Lang("SpawnBot", null, ArenasCache[arenaEdit].SpawnsBot.Count.ToString()));
                        }
                        else if (args[1] == "clear")
                        {
                            ArenasCache[arenaEdit].SpawnsBot.Clear();
                            SendReply(player, Lang("ClearBotSpawns", null, arenaEdit));
                            return;
                        }
                    }
                    break;
                case "splayer":
                    {
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        if (args.Length < 2)
                        {
                            ArenasCache[arenaEdit].SpawnsPlayer[ArenasCache[arenaEdit].SpawnsPlayer.Count + 1] = new Position(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                            SendReply(player, Lang("SpawnPlayer", null, ArenasCache[arenaEdit].SpawnsPlayer.Count.ToString()));
                        }
                        else if (args[1] == "clear")
                        {
                            ArenasCache[arenaEdit].SpawnsPlayer.Clear();
                            SendReply(player, Lang("ClearPlayerSpawns", null, arenaEdit));
                            return;
                        }
                    }
                    break;
                case "movement":
                    {
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        if (ArenasCache[arenaEdit].BotMoving)
                            ArenasCache[arenaEdit].BotMoving = false;
                        else
                            ArenasCache[arenaEdit].BotMoving = true;
                        SendReply(player, $"Movement: {ArenasCache[arenaEdit].BotMoving}");
                    }
                    break;
                case "enable":
                    {
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        if (ArenasCache[arenaEdit].Enabled)
                            ArenasCache[arenaEdit].Enabled = false;
                        else
                            ArenasCache[arenaEdit].Enabled = true;
                        SendReply(player, $"Enabled: {ArenasCache[arenaEdit].Enabled}");
                    }
                    break;
                case "botcount":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, "Not a valid number.");
                            return;
                        }
                        if (!EditArena.ContainsKey(player.userID))
                        {
                            SendReply(player, Lang("NotEditingArena"));
                            return;
                        }
                        var arenaEdit = EditArena[player.userID];
                        int amount;
                        if (!int.TryParse(args[1], out amount))
                        {
                            SendReply(player, "Not a valid number.");
                            return;
                        }
                        ChangeBotCount(amount, arenaEdit);
                        ArenasCache[arenaEdit].BotCount = amount;
                        SendReply(player, $"Changed Bot amount in Arena: {arenaEdit} to {amount}");
                    }
                    break;
            }
        }

        [ConsoleCommand("LeaveAT")]
        void cmdUILeaveAT(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            LeaveAT(player);
        }

        [ConsoleCommand("AmmoAT")]
        void cmdUIAmmoAT(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (NoAmmo.Contains(player.userID))
                NoAmmo.Remove(player.userID);
            else
                NoAmmo.Add(player.userID);
            UpdateTimer(player);
        }

        [ConsoleCommand("ResetAT")]
        void cmdUIResetAT(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            PlayersCache[player.userID].Hits = 0;
            PlayersCache[player.userID].Headshots = 0;
            PlayersCache[player.userID].Bullets = 0;
            UpdateTimer(player);
        }
        #endregion
    }
}
namespace Oxide.Plugins.AimTrainUI
{
    public class UIMethods
    {
        public static CuiElementContainer Container(string name, string bgColor, Anchor Min, Anchor Max,
            string parent = "Overlay", float fadeOut = 0f, float fadeIn = 0f)
        {
            var newElement = new CuiElementContainer()
            {
                new CuiElement()
                {
                    Name = name,
                    Parent = parent,
                    FadeOut = fadeOut,
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = bgColor,
                            FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = $"{Min.X} {Min.Y}",
                            AnchorMax = $"{Max.X} {Max.Y}"
                        }
                    }
                },
            };
            return newElement;
        }

        public static void Panel(string name, string parent, ref CuiElementContainer container, string bgColor,
            Anchor Min, Anchor Max, bool cursor = false)
        {
            container.Add(new CuiPanel()
            {
                Image =
                {
                    Color = bgColor
                },
                CursorEnabled = cursor,
                RectTransform =
                {
                    AnchorMin = $"{Min.X} {Min.Y}",
                    AnchorMax = $"{Max.X} {Max.Y}"
                }
            }, parent, name);
        }

        public static void Label(string name, string parent, ref CuiElementContainer container, Anchor Min, Anchor Max,
            string text, string color = "1 1 1 1", int fontSize = 15, TextAnchor textAnchor = TextAnchor.MiddleCenter,
            string font = "robotocondensed-bold.ttf")
        {
            container.Add(new CuiLabel()
            {
                Text =
                {
                    Align = textAnchor,
                    Color = color,
                    Font = font,
                    FontSize = fontSize
                },
                RectTransform =
                {
                    AnchorMin = $"{Min.X} {Min.Y}",
                    AnchorMax = $"{Max.X} {Max.Y}"
                }
            }, parent, name);
        }

        public static void Button(string name, string parent, ref CuiElementContainer container, Anchor Min,
            Anchor Max, string command, string text, string textColor,
            int fontSize, string color = "1 1 1 1", TextAnchor anchor = TextAnchor.MiddleCenter, float fadeOut = 0f,
            float fadeIn = 0f, string font = "robotocondensed-bold.ttf")
        {
            container.Add(new CuiButton()
            {
                FadeOut = fadeOut,
                Button =
                {
                    Color = color,
                    Command = command,
                },
                RectTransform =
                {
                    AnchorMin = $"{Min.X} {Min.Y}",
                    AnchorMax = $"{Max.X} {Max.Y}"
                },
                Text =
                {
                    Text = text,
                    Color = textColor,
                    Align = anchor,
                    Font = font,
                    FontSize = fontSize,
                    FadeIn = fadeIn
                }
            }, parent, name);
        }

        public static void Text(string name, string parent, ref CuiElementContainer container, TextAnchor anchor,
            string color, int fontSize, string text,
            Anchor Min, Anchor Max, string font = "robotocondensed-bold.ttf", float fadeOut = 0f,
            float fadeIn = 0f)
        {
            container.Add(new CuiElement()
            {
                Name = name,
                Parent = parent,
                FadeOut = fadeOut,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = text,
                        Align = anchor,
                        FontSize = fontSize,
                        Font = font,
                        FadeIn = fadeIn,
                        Color = color
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"{Min.X} {Min.Y}",
                        AnchorMax = $"{Max.X} {Max.Y}"
                    }
                }
            });
        }

        public static void Element(string name, string parent, ref CuiElementContainer container, Anchor Min, Anchor Max,
            string bgColor, string material = "", float fadeOut = 0f, float fadeIn = 0f)
        {
            container.Add(new CuiElement()
            {
                Name = name,
                Parent = parent,
                FadeOut = fadeOut,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = bgColor,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"{Min.X} {Min.Y}",
                        AnchorMax = $"{Max.X} {Max.Y}"
                    }
                }
            });
        }

        public static void Border(Anchor posMin, Anchor posMax, ref CuiElementContainer container, float borderSize = 0.001f, string color = "1 1 1 1", string parent = "Overlay")
        {
            Element("", parent, ref container, posMin, new Anchor(posMax.X, posMin.Y + (borderSize * 2)), "1 1 1 1");
            Element("", parent, ref container, new Anchor(posMin.X, posMax.Y - (borderSize * 2)), posMax, "1 1 1 1");
            Element("", parent, ref container, posMin, new Anchor(posMin.X + borderSize, posMax.Y), "1 1 1 1");
            Element("", parent, ref container, new Anchor(posMax.X, posMin.Y), new Anchor(posMax.X + borderSize, posMax.Y), "1 1 1 1");
        }
    }

    public class Anchor
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Anchor()
        {
        }

        public Anchor(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Anchor operator +(Anchor first, Anchor second)
        {
            return new Anchor(first.X + second.X, first.Y + second.Y);
        }

        public static Anchor operator -(Anchor first, Anchor second)
        {
            return new Anchor(first.X - second.X, first.Y - second.Y);
        }
    }

    public class Rgba
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public Rgba()
        {
        }

        public Rgba(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public string Format()
        {
            return $"{R / 255} {G / 255} {B / 255} {A}";
        }
    }
}