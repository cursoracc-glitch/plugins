using System;
using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using GameTips;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Skyfall", "Colon Blow", "1.0.14")]
    class Skyfall : RustPlugin
    {

        // added Thermals over player buildings

        #region Loadup

        [PluginReference]
        Plugin Clans;

        static Skyfall _instance;
        BaseEntity skyfallPlane;
        static LayerMask layerMask;
        static List<ulong> skyfallplayerlist = new List<ulong>();
        static List<ulong> isParachuting = new List<ulong>();
        static List<ulong> cooldownlist = new List<ulong>();


        void Loaded()
        {
            _instance = this;
            LoadVariables();
            LoadMessages();
            permission.RegisterPermission("skyfall.use", this);
            permission.RegisterPermission("skyfall.localrespawn", this);
            permission.RegisterPermission("skyfall.admin", this);
            layerMask = (1 << 29);
            layerMask |= (1 << 18);
            layerMask = ~layerMask;
        }

        #endregion

        #region Configuration

        bool enableLocalRespawn = false;
        float localRespawnDistance = 300f;
        static float parchuteFwdSpeed = 15f;
        static float parachuteDownSpeed = 15f;
        static float groundReleaseHeight = 10f;

        bool enableThermals = true;
        static float thermalsHeight = 150f;

        float ChaosDropCountdown = 10f;
        static float FlightDeck = 1000f;
        static float GlobalMapOffset = 500f;
        bool UseCooldown = true;
        float SkyFallCoolDown = 600f;
        bool AllowFreeFall = false;
        static bool ForceDismountFromPlane = true;
        static float SkyFallPlaneDespawn = 100f;
        bool DoSkyfallOnFirstTime = true;
        static float ForceJumpTime = 60f;
        bool EnableRespawnButton = true;
        bool UseRandomRespawn = true;
        static ulong wallskinid = 1320948157;
        bool Changed;

        void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void LoadConfigVariables()
        {
            CheckCfgFloat("Global - Map size Offset when finding global drop position (higher number means closer to center of map, less chance over water) ", ref GlobalMapOffset);
            CheckCfgFloat("Global - Ground Release Height for Player Parachute (default is 10.0) ", ref groundReleaseHeight);
            CheckCfg("Global - Thermals - Enable Checks for buildings under player ? ", ref enableThermals);
            CheckCfgFloat("Global - Thermals - Check for buildings under player starting at this height from ground (default is 150.0) : ", ref thermalsHeight);

            CheckCfgUlong("Logo - Skin ID for Back wall Skyfall plane : ", ref wallskinid);
            CheckCfg("Cooldown - Use Skyfall pack cooldown ? ", ref UseCooldown);

            CheckCfg("Parachute : Allow players to open there own Parachutes and allow freefalling (if false, chutes auto open and players cannot remove them till they land)? ", ref AllowFreeFall);

            CheckCfgFloat("Chaos Drop - Countdown time from this, (will annouce start, 75%, 50% and 25% time left) ", ref ChaosDropCountdown);

            CheckCfg("Plane - Enable force dismount of players after a certain time ? ", ref ForceDismountFromPlane);
            CheckCfgFloat("Plane - Players will be force dismounted after this many seconds while seated in Skyfall plane (if enabled) ", ref ForceJumpTime);

            CheckCfg("Respawn - Enable Button on respawn screen.(Will do random TP in air with Chute attached) ? ", ref EnableRespawnButton);
            CheckCfg("Respawn - Use Random Respawn location when pressing Skyfall respawn button ? ", ref UseRandomRespawn);
            CheckCfg("Respawn - Local Skyfall Respawn - Enable Skyfall respawn to only drop you within a local radius of corpse ?", ref enableLocalRespawn);
            CheckCfgFloat("Respawn - Local Skyfall Respawn - Max Distance from corpse ", ref localRespawnDistance);

            CheckCfgFloat("Plane - Skyfall plane will despawn if no players are on board after this long : ", ref SkyFallPlaneDespawn);
            CheckCfgFloat("Parachute - Downward speed when using parachute : ", ref parachuteDownSpeed);
            CheckCfgFloat("Parachute - Forward speed when using parachute and forward button : ", ref parchuteFwdSpeed);
            CheckCfgFloat("Flightdeck - Altitude at which Skyfall Plane flys at : ", ref FlightDeck);
            CheckCfgFloat("Cooldown - After using a Skyfall pack, time player must wait to use another : ", ref SkyFallCoolDown);
        }

        void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = System.Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        void CheckCfgUlong(string Key, ref ulong var)
        {

            if (Config[Key] != null)
                var = Convert.ToUInt64(Config[Key]);
            else
                Config[Key] = var;
        }

        #endregion

        #region Localization

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noperms"] = "You don't have permission to use this command.",
                ["readytojump"] = "You are ready to jump at any time.",
                ["notjumping"] = "You are not able to use that right now.",
                ["alreadyusedchute"] = "You have already used your chute for this jump... sorry...",
                ["undercooldown"] = "You must wait, you are under a cooldown",
                ["nomorecooldown"] = "Your Skyfall cooldown as been removed.",
                ["skyfallfull"] = "Skyfall is FULL, please wait and try again.",
                ["openchute"] = "Press your 'RELOAD' Key to open your chute when your ready !!!!"
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("skyfall")]
        void chatSkyfall(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "skyfall.use"))
            {
                if (player.isMounted) { SendReply(player, "You are already Mounted"); return; }
                if (cooldownlist.Contains(player.userID)) { PrintToChat(player, lang.GetMessage("undercooldown", this, player.UserIDString)); return; }
                if (UseCooldown) { CooldownAddPlayerID(player); }
                AddPlayerID(player);
                var hascontroller = player.GetComponent<PlayerJumpController>();
                if (!hascontroller) player.gameObject.AddComponent<PlayerJumpController>();
                ActivateJumpPlane(player);
            }
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        [ConsoleCommand("givechute")]
        void cmdConsoleGiveChute(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (permission.UserHasPermission(player.UserIDString, "skyfall.use"))
                    GiveChutePack(player);
                else
                    PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveChutePack(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("dorespawn")]
        void cmdConsoleDoRespawn(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length == 0) return;
            if (arg.Args.Length == 1)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                if (UseCooldown && CooldownListConstainsPlayerID(BasePlayer.FindByID(id))) return;
                if (UseCooldown) { CooldownAddPlayerID(BasePlayer.FindByID(id)); }
                if (UseRandomRespawn) { RespawnAtRandom(BasePlayer.FindByID(id), true); return; }
                else
                    RespawnAtPlane(BasePlayer.FindByID(id), true);
            }
        }

        [ConsoleCommand("chaosdrop")]
        void cmdConsoleChaosDrop(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (permission.UserHasPermission(player.UserIDString, "skyfall.admin"))
                {
                    ActivateChaosDrop();
                }
            }
            if (player == null)
            {
                ActivateChaosDrop();
            }
        }

        #endregion

        #region Hooks

        void ActivateChaosDrop()
        {
            ConVar.Chat.Broadcast("in .... " + ChaosDropCountdown.ToString("F0"), "Skyfall Chaos Drop", "#4286f4");
            timer.Once(ChaosDropCountdown * 0.25f, () => ConVar.Chat.Broadcast("in .... " + (ChaosDropCountdown * 0.75f).ToString("F0"), "Skyfall Chaos Drop", "#4286f4"));
            timer.Once(ChaosDropCountdown * 0.50f, () => ConVar.Chat.Broadcast("in .... " + (ChaosDropCountdown * 0.50f).ToString("F0"), "Skyfall Chaos Drop", "#4286f4"));
            timer.Once(ChaosDropCountdown * 0.75f, () => ConVar.Chat.Broadcast("in .... " + (ChaosDropCountdown * 0.25f).ToString("F0"), "Skyfall Chaos Drop", "#4286f4"));
            timer.Once(ChaosDropCountdown, () =>
            {
                PrintWarning("Chaos Drop Has Been Activated...");
                DoChaosDrop();
            });
        }

        void DoChaosDrop()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DoTPSkyfallRandom(player);
            }
        }

        public bool ListConstainsPlayerID(BasePlayer player)
        {
            if (skyfallplayerlist.Contains(player.userID)) return true;
            return false;
        }

        void AddPlayerID(BasePlayer player)
        {
            if (ListConstainsPlayerID(player)) return;
            skyfallplayerlist.Add(player.userID);
        }

        public void RemovePlayerID(BasePlayer player)
        {
            if (ListConstainsPlayerID(player))
            {
                skyfallplayerlist.Remove(player.userID);
                return;
            }
        }

        bool CooldownListConstainsPlayerID(BasePlayer player)
        {
            if (cooldownlist.Contains(player.userID)) return true;
            return false;
        }

        void CooldownAddPlayerID(BasePlayer player)
        {
            if (CooldownListConstainsPlayerID(player)) return;
            cooldownlist.Add(player.userID);
            float cooldown = SkyFallCoolDown;
            timer.Once(cooldown, () => { cooldownlist.Remove(player.userID); });
        }

        Vector3 FindSpawnPoint()
        {
            Vector3 spawnpoint = new Vector3();

            float spawnline = Convert.ToSingle((ConVar.Server.worldsize) / 2) - GlobalMapOffset;

            float spawnminx = spawnline;
            float spawnmaxx = spawnline;
            float spawnminz = spawnline;
            float spawnmaxz = spawnline;

            float xrandom1 = UnityEngine.Random.Range(0, 2);
            if (xrandom1 == 1) { spawnminx = spawnline; spawnmaxx = spawnline; spawnminz = 0; spawnmaxz = UnityEngine.Random.Range(0, spawnline); }
            if (xrandom1 == 0) { spawnminz = spawnline; spawnmaxz = spawnline; spawnminx = 0; spawnmaxx = UnityEngine.Random.Range(0, spawnline); }
            float yrandom = FlightDeck;
            spawnpoint = new Vector3(UnityEngine.Random.Range(spawnminx, spawnmaxx), yrandom, UnityEngine.Random.Range(spawnminz, spawnmaxz));

            float xrandom2 = UnityEngine.Random.Range(0, 2);
            float zrandom2 = UnityEngine.Random.Range(0, 2);
            if (xrandom2 == 1) spawnpoint.x = -spawnpoint.x;
            if (zrandom2 == 1) spawnpoint.z = -spawnpoint.z;
            return spawnpoint;
        }

        Vector3 FindPlayerSKyfallPoint()
        {
            Vector3 spawnpoint = new Vector3();
            float spawnline = ((ConVar.Server.worldsize) / 2) - GlobalMapOffset;
            float yrandom = UnityEngine.Random.Range(FlightDeck * 0.75f, FlightDeck * 1.25f);
            spawnpoint = new Vector3(UnityEngine.Random.Range(-spawnline, spawnline), yrandom, UnityEngine.Random.Range(-spawnline, spawnline));
            return spawnpoint;
        }

        Vector3 GetLocalSkyfallPoint(BasePlayer player)
        {
            Vector3 targetPos = new Vector3();
            Vector3 randomizer = new Vector3(UnityEngine.Random.Range(-localRespawnDistance, localRespawnDistance), 0f, UnityEngine.Random.Range(-localRespawnDistance, localRespawnDistance));
            Vector3 newp = (player.transform.position + randomizer);
            float yrandom = UnityEngine.Random.Range(FlightDeck * 0.75f, FlightDeck * 1.25f);
            targetPos = new Vector3(newp.x, yrandom, newp.z);
            return targetPos;
        }

        SkyfallPlane FindJumpPlane()
        {
            if (skyfallPlane != null)
            {
                var isplane = skyfallPlane.GetComponentInParent<SkyfallPlane>() ?? null;
                if (isplane != null)
                {
                    isplane.counter = 0f;
                    return isplane;
                }
            }
            Vector3 startloc = FindSpawnPoint();
            string sphereprefab = "assets/prefabs/visualization/sphere.prefab";
            skyfallPlane = GameManager.server.CreateEntity(sphereprefab, startloc, Quaternion.identity, true);
            skyfallPlane.Spawn();
            var newplane = skyfallPlane.gameObject.AddComponent<SkyfallPlane>();
            return newplane;
        }

        void ActivateJumpPlane(BasePlayer player)
        {
            var jumpplane = FindJumpPlane() as SkyfallPlane;
            if (jumpplane)
            {
                SendReply(player, "Skyfall Countdown 3...");
                timer.Once(1f, () => SendReply(player, "Skyfall Countdown 2..."));
                timer.Once(2f, () => SendReply(player, "Skyfall Countdown 1..."));
                timer.Once(3f, () =>
                {
                    if (player == null || jumpplane == null) return;
                    ActivateSkyfall(player, jumpplane);
                });
            }
            else return;
        }

        void ActivateSkyfall(BasePlayer player, SkyfallPlane plane)
        {
            if (player == null) return;
            if (player.isMounted) { SendReply(player, "You are already Mounted"); return; }
            var hascontroller = player.GetComponent<PlayerJumpController>() ?? null;
            if (hascontroller == null) player.gameObject.AddComponent<PlayerJumpController>();
            AddPlayerID(player);
            DoRespawnAt(player, plane.transform.position, plane.transform.rotation);
            if (plane.FindMountableChair(player))
            {
                return;
            }
            else RespawnAtRandom(player);
        }

        void GiveChutePack(BasePlayer player)
        {
            if (player == null) return;
            var item = ItemManager.CreateByItemID(-2022172587, 1, 1398786190);
            player.inventory.GiveItem(item);
        }

        private void CanWearItem(PlayerInventory inventory, Item item, int targetPos)
        {
            if (item == null || item.skin != 1398786190) return;
            if (inventory == null) return;
            var player = inventory.GetComponent<BasePlayer>() ?? null;
            if (player == null) return;
            if (player.isMounted) { SendReply(player, "You are already Mounted"); return; }
            if (cooldownlist.Contains(player.userID)) { PrintToChat(player, lang.GetMessage("undercooldown", this, player.UserIDString)); return; }
            item.RemoveFromWorld();
            item.RemoveFromContainer();
            item.Remove(0f);
            if (UseCooldown) { CooldownAddPlayerID(player); }
            AddPlayerID(player);
            ActivateJumpPlane(player);
        }

        public void DoRespawnAt(BasePlayer player, Vector3 position, Quaternion rotation, bool isrespawn = false)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Unused2, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Unused1, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.DisplaySash, false);
            player.transform.position = (position);
            player.transform.rotation = (rotation);
            player.StopWounded();
            player.StopSpectating();
            player.UpdateNetworkGroup();
            player.UpdatePlayerCollider(true);
            player.UpdatePlayerRigidbody(false);
            if (isrespawn) player.StartSleeping();
            if (isrespawn) player.metabolism.Reset();
            if (isrespawn) player.InitializeHealth(player.StartHealth(), player.StartMaxHealth());
            if (isrespawn) player.inventory.GiveDefaultItems();
            player.SendNetworkUpdateImmediate(false);
            player.ClearEntityQueue(null);
            if (isrespawn) player.ClientRPCPlayer(null, player, "StartLoading");
            if (isrespawn) Oxide.Core.Interface.CallHook("OnPlayerRespawned", player);
            player.SendFullSnapshot();
        }

        void RespawnAtPlane(BasePlayer player, bool isrespawn = false)
        {
            if (player == null) return;
            var hascontroller = player.GetComponent<PlayerJumpController>() ?? null;
            if (hascontroller == null) player.gameObject.AddComponent<PlayerJumpController>();
            AddPlayerID(player);
            var jumplane = FindJumpPlane() as SkyfallPlane;
            DoRespawnAt(player, jumplane.transform.position, jumplane.transform.rotation, isrespawn);
            AttachChute(player);
        }

        void RespawnAtRandom(BasePlayer player, bool isrespawn = false)
        {
            if (player == null) return;
            var hascontroller = player.GetComponent<PlayerJumpController>() ?? null;
            if (hascontroller == null) player.gameObject.AddComponent<PlayerJumpController>();

            AddPlayerID(player);
            Vector3 respawnpos = new Vector3();
            if (enableLocalRespawn || permission.UserHasPermission(player.UserIDString, "skyfall.localrespawn")) respawnpos = GetLocalSkyfallPoint(player);
            else respawnpos = FindPlayerSKyfallPoint();
            DoRespawnAt(player, respawnpos, Quaternion.identity, isrespawn);
            AttachChute(player);
        }

        void PreparePlayerTPSkyfall(BasePlayer player)
        {

            SendReply(player, "Skyfall Countdown 3...");
            timer.Once(1f, () => SendReply(player, "Skyfall Countdown 2..."));
            timer.Once(2f, () => SendReply(player, "Skyfall Countdown 1..."));
            timer.Once(3f, () =>
            {
                if (player == null) return;
                DoTPSkyfallRandom(player);
            });
        }

        void DoTPSkyfallRandom(BasePlayer player)
        {
            if (player == null) return;
            var hascontroller = player.GetComponent<PlayerJumpController>() ?? null;
            if (hascontroller == null) player.gameObject.AddComponent<PlayerJumpController>();
            AddPlayerID(player);
            Vector3 respawnpos = FindSpawnPoint();

            player.transform.position = respawnpos;
            player.ClientRPCPlayer(null, player, "ForcePositionTo", respawnpos);
            player.SendNetworkUpdate();

            AttachChute(player);
        }

        ///////////////////////////////////////////////////////////////////

        public void AttachChute(BasePlayer player)
        {
            if (player == null) return;
            string chairprefab = "assets/prefabs/deployable/chair/chair.deployed.prefab";
            var chutemount = GameManager.server.CreateEntity(chairprefab, player.transform.position, Quaternion.identity, true);
            chutemount.enableSaving = false;
            var hasstab = chutemount.GetComponent<StabilityEntity>();
            if (hasstab) hasstab.grounded = true;
            var hasmount = chutemount.GetComponent<BaseMountable>();
            if (hasmount) hasmount.isMobile = true;
            chutemount.skinID = 1311472987;
            chutemount?.Spawn();
            if (chutemount != null)
            {
                if (!isParachuting.Contains(player.userID)) isParachuting.Add(player.userID);
                var parachute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", new Vector3(), new Quaternion(), true);
                parachute.SetParent(chutemount, 0);
                parachute?.Spawn();

                var addchute = chutemount.gameObject.AddComponent<PlayerParachute>();
                hasmount.MountPlayer(player);
            }
            return;
        }

        object OnPlayerLand(BasePlayer player, float num)
        {
            if (player == null) return null;
            if (ListConstainsPlayerID(player)) return false;
            return null;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;
            if (ListConstainsPlayerID(player))
            {
                if (!player.isMounted)
                {
                    if (input.WasJustPressed(BUTTON.RELOAD))
                    {
                        AttachChute(player);
                        return;
                    }
                }
                if (player.isMounted)
                {
                    var haschute1 = player.GetMounted().GetComponentInParent<PlayerParachute>() ?? null;
                    if (haschute1)
                    {
                        haschute1.ChuteInput(input, player);
                    }
                    return;
                }
            }
        }

        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (player == null) return null;
            if (ListConstainsPlayerID(player)) return false;
            return null;
        }

        void SendInfoMessage(BasePlayer player, string message, float time)
        {
            player?.SendConsoleCommand("gametip.showgametip", message);
            timer.Once(time, () => player?.SendConsoleCommand("gametip.hidegametip"));
        }

        object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null) return null;
            if (!AllowFreeFall)
            {
                if (isParachuting.Contains(player.userID)) return true;
            }
            return null;
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            if (mountable == null || player == null) return;
            if (player.GetComponent<PlayerJumpController>())
            {
                if (AllowFreeFall)
                {
                    SendInfoMessage(player, "Press your <color=black>[ R E L O A D ]</color> key to open parachute !!", 10f);
                }
                if (!AllowFreeFall)
                {
                    AttachChute(player);
                }
            }
            return;
        }

        public void DisMountPlayer(BasePlayer player)
        {
            if (player == null || !player.isMounted) return;
            var isonplane = player.GetMounted().GetComponentInParent<SkyfallPlane>() ?? null;
            if (isonplane == null) return;
            if (player.isMounted && isonplane != null)
            {
                player?.EnsureDismounted();
                AttachChute(player);
            }
            else return;
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null) return;
            if (entity is BasePlayer)
            {
                BasePlayer victim = (BasePlayer)entity as BasePlayer;
                if (ListConstainsPlayerID(victim))
                {
                    hitInfo.damageTypes.ScaleAll(0);
                }
            }
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            RemovePlayerID(player);
            if (!EnableRespawnButton) return;
            timer.Once(4f, () => AddPlayerButton(player));
        }

        void AddPlayerButton(BasePlayer player)
        {
            if (player == null) return;
            player.gameObject.AddComponent<SkyfallRespawnButton>();
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null) return;
            var hasgui = player.GetComponent<SkyfallRespawnButton>() ?? null;
            if (hasgui != null) GameObject.Destroy(hasgui);
        }

        void Unload()
        {
            skyfallplayerlist.Clear();
            isParachuting.Clear();
            cooldownlist.Clear();
            DestroyAll<SkyfallRespawnButton>();
            DestroyAll<SkyfallPlane>();
            DestroyAll<PlayerParachute>();
            DestroyAll<PlayerJumpController>();
        }

        #endregion

        #region Respawn Button Cui

        class SkyfallRespawnButton : MonoBehaviour
        {
            BasePlayer player;

            void Awake()
            {
                player = base.GetComponentInParent<BasePlayer>();
                RespawnButton(player);
            }

            public void RespawnButton(BasePlayer player)
            {
                DestroyCui(player);
                if (player == null) { OnDestroy(); return; }

                var elements = new CuiElementContainer();
                string colorstring = "0.32 0.39 0.19 0.5";
                string textstring = "Use Skyfall \n Respawn";
                if (cooldownlist.Contains(player.userID)) { colorstring = "0.30 0.25 0.14 0.5"; textstring = "Skyfall Respawn \n Unavailable"; }

                string clickbutton = elements.Add(new CuiButton
                {
                    Button = { Command = $"dorespawn " + player.userID, Color = colorstring },
                    RectTransform = { AnchorMin = "0.45 0.16", AnchorMax = "0.71 0.26" },
                    Text = { Text = textstring, FontSize = 20, Color = "1.0 1.0 1.0 1.0", Align = TextAnchor.MiddleCenter }
                }, "Overall", "skyfallbutton");

                elements.Add(new CuiElement
                {
                    Name = "skyfallgui",
                    Parent = "Overall",
                    Components =
                            {
                            new CuiRawImageComponent { Color = "1 1 1 1", Url = "https://i.imgur.com/XPokWoq.png", Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                            new CuiRectTransformComponent { AnchorMin = "0.46 0.17",  AnchorMax = "0.50 0.25" }
                            }
                });

                CuiHelper.AddUi(player, elements);
            }

            void DestroyCui(BasePlayer player)
            {
                if (player == null) { OnDestroy(); return; }
                CuiHelper.DestroyUi(player, "skyfallgui");
                CuiHelper.DestroyUi(player, "skyfallbutton");
            }

            void OnDestroy()
            {
                DestroyCui(player);
                Destroy(this);
            }
        }

        #endregion

        #region Skyfall Plane Entity

        class SkyfallPlane : BaseEntity
        {
            BaseEntity cpentity;
            BaseEntity plane;
            Vector3 cpentitypos;
            public float counter;

            public BaseEntity jumpchair1;
            public BaseEntity jumpchair2;
            public BaseEntity jumpchair3;
            public BaseEntity jumpchair4;
            public BaseEntity jumpchair5;
            public BaseEntity jumpchair6;
            public BaseEntity jumpchair7;
            public BaseEntity jumpchair8;
            public BaseEntity jumpchair9;
            public BaseEntity jumpchair10;
            public BaseEntity jumpchair11;
            public BaseEntity jumpchair12;

            BaseEntity wallsign;

            Vector3 movetopoint;

            void Awake()
            {
                cpentity = GetComponent<BaseEntity>();
                if (cpentity == null) { OnDestroy(); return; }
                cpentitypos = cpentity.transform.position + new Vector3(0f, -5f, 0f);
                counter = 0f;
                plane = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", cpentity.transform.position + new Vector3(0f, -5f, 0f), Quaternion.identity, true);
                if (plane != null)
                {
                    plane.enabled = false;
                    plane.Spawn();
                    plane.SetParent(cpentity);
                }
                string chairprefab = "assets/prefabs/deployable/chair/chair.deployed.prefab";
                jumpchair1 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair1.transform.localEulerAngles = new Vector3(0, 90, 0);
                jumpchair1.transform.localPosition = new Vector3(-1.2f, 3f, -7.43f);
                SpawnRefresh(jumpchair1);
                jumpchair1?.Spawn();
                jumpchair1.SetParent(plane, 0);

                jumpchair2 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair2.transform.localEulerAngles = new Vector3(0, 90, 0);
                jumpchair2.transform.localPosition = new Vector3(-1.2f, 3f, -6.76f);
                SpawnRefresh(jumpchair2);
                jumpchair2?.Spawn();
                jumpchair2.SetParent(plane, 0);

                jumpchair3 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair3.transform.localEulerAngles = new Vector3(0, 90, 0);
                jumpchair3.transform.localPosition = new Vector3(-1.2f, 3f, -6.10f);
                SpawnRefresh(jumpchair3);
                jumpchair3?.Spawn();
                jumpchair3.SetParent(plane, 0);

                jumpchair4 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair4.transform.localEulerAngles = new Vector3(0, 90, 0);
                jumpchair4.transform.localPosition = new Vector3(-1.2f, 3f, -5.43f);
                SpawnRefresh(jumpchair4);
                jumpchair4?.Spawn();
                jumpchair4.SetParent(plane, 0);

                jumpchair5 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair5.transform.localEulerAngles = new Vector3(0, 90, 0);
                jumpchair5.transform.localPosition = new Vector3(-1.2f, 3f, -4.76f);
                SpawnRefresh(jumpchair5);
                jumpchair5?.Spawn();
                jumpchair5.SetParent(plane, 0);

                jumpchair6 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair6.transform.localEulerAngles = new Vector3(0, 90, 0);
                jumpchair6.transform.localPosition = new Vector3(-1.2f, 3f, -4.10f);
                SpawnRefresh(jumpchair6);
                jumpchair6?.Spawn();
                jumpchair6.SetParent(plane, 0);
                AddWallSign();

                jumpchair7 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair7.transform.localEulerAngles = new Vector3(0, 270, 0);
                jumpchair7.transform.localPosition = new Vector3(1.2f, 3f, -7.43f);
                SpawnRefresh(jumpchair7);
                jumpchair7?.Spawn();
                jumpchair7.SetParent(plane, 0);

                jumpchair8 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair8.transform.localEulerAngles = new Vector3(0, 270, 0);
                jumpchair8.transform.localPosition = new Vector3(1.2f, 3f, -6.76f);
                SpawnRefresh(jumpchair8);
                jumpchair8?.Spawn();
                jumpchair8.SetParent(plane, 0);

                jumpchair9 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair9.transform.localEulerAngles = new Vector3(0, 270, 0);
                jumpchair9.transform.localPosition = new Vector3(1.2f, 3f, -6.10f);
                SpawnRefresh(jumpchair9);
                jumpchair9?.Spawn();
                jumpchair9.SetParent(plane, 0);

                jumpchair10 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair10.transform.localEulerAngles = new Vector3(0, 270, 0);
                jumpchair10.transform.localPosition = new Vector3(1.2f, 3f, -5.43f);
                SpawnRefresh(jumpchair10);
                jumpchair10?.Spawn();
                jumpchair10.SetParent(plane, 0);

                jumpchair11 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair11.transform.localEulerAngles = new Vector3(0, 270, 0);
                jumpchair11.transform.localPosition = new Vector3(1.2f, 3f, -4.76f);
                SpawnRefresh(jumpchair11);
                jumpchair11?.Spawn();
                jumpchair11.SetParent(plane, 0);

                jumpchair12 = GameManager.server.CreateEntity(chairprefab, cpentitypos, Quaternion.identity, true);
                jumpchair12.transform.localEulerAngles = new Vector3(0, 270, 0);
                jumpchair12.transform.localPosition = new Vector3(1.2f, 3f, -4.10f);
                SpawnRefresh(jumpchair12);
                jumpchair12?.Spawn();
                jumpchair12.SetParent(plane, 0);

                movetopoint = FindCoords();
            }

            void AddWallSign()
            {
                if (plane == null) { OnDestroy(); return; }
                string prefabwallsign = "assets/prefabs/deployable/rug/rug.deployed.prefab";
                wallsign = GameManager.server.CreateEntity(prefabwallsign, cpentitypos, Quaternion.identity, true);
                wallsign.transform.localEulerAngles = new Vector3(0, 270, 90);
                wallsign.transform.localPosition = new Vector3(0f, 4.7f, -3.8f);
                wallsign.skinID = wallskinid;
                wallsign?.Spawn();
                wallsign.SetParent(plane, 0);
                SpawnRefresh(wallsign);
            }

            public bool FindMountableChair(BasePlayer player)
            {
                if (ChairIsAvailable(player, jumpchair1)) return true;
                if (ChairIsAvailable(player, jumpchair2)) return true;
                if (ChairIsAvailable(player, jumpchair3)) return true;
                if (ChairIsAvailable(player, jumpchair4)) return true;
                if (ChairIsAvailable(player, jumpchair5)) return true;
                if (ChairIsAvailable(player, jumpchair6)) return true;
                if (ChairIsAvailable(player, jumpchair7)) return true;
                if (ChairIsAvailable(player, jumpchair8)) return true;
                if (ChairIsAvailable(player, jumpchair9)) return true;
                if (ChairIsAvailable(player, jumpchair10)) return true;
                if (ChairIsAvailable(player, jumpchair11)) return true;
                if (ChairIsAvailable(player, jumpchair12)) return true;
                return false;
            }

            bool ChairIsAvailable(BasePlayer player, BaseEntity entity)
            {
                var findmount = entity.GetComponent<BaseMountable>() ?? null;
                if (findmount != null)
                {
                    if (findmount._mounted) return false;
                    else
                        findmount.MountPlayer(player);
                    return true;
                }
                return false;
            }

            public bool PlayersAreMounted()
            {
                if (jumpchair1 != null && jumpchair1.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair2 != null && jumpchair2.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair3 != null && jumpchair3.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair4 != null && jumpchair4.GetComponent<BaseMountable>()._mounted) return true;

                if (jumpchair5 != null && jumpchair5.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair6 != null && jumpchair6.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair7 != null && jumpchair7.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair8 != null && jumpchair8.GetComponent<BaseMountable>()._mounted) return true;

                if (jumpchair9 != null && jumpchair9.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair10 != null && jumpchair10.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair11 != null && jumpchair11.GetComponent<BaseMountable>()._mounted) return true;
                if (jumpchair12 != null && jumpchair12.GetComponent<BaseMountable>()._mounted) return true;
                return false;
            }



            void SpawnRefresh(BaseNetworkable entity1)
            {
                var hasstab = entity1.GetComponent<StabilityEntity>();
                if (hasstab)
                {
                    hasstab.grounded = true;
                }
                var hasmount = entity1.GetComponent<BaseMountable>();
                if (hasmount)
                {
                    hasmount.isMobile = true;
                }
            }

            Vector3 FindCoords()
            {
                float spawnline = ((ConVar.Server.worldsize) / 2) - GlobalMapOffset;
                float spawnminx = spawnline;
                float spawnmaxx = spawnline;
                float spawnminz = spawnline;
                float spawnmaxz = spawnline;

                float xrandom1 = UnityEngine.Random.Range(0, 2);
                if (xrandom1 == 1) { spawnminx = spawnline; spawnmaxx = spawnline; spawnminz = 0; spawnmaxz = UnityEngine.Random.Range(0, spawnline); }
                if (xrandom1 == 0) { spawnminz = spawnline; spawnmaxz = spawnline; spawnminx = 0; spawnmaxx = UnityEngine.Random.Range(0, spawnline); }

                Vector3 spawnpoint = new Vector3(UnityEngine.Random.Range(spawnminx, spawnmaxx), FlightDeck, UnityEngine.Random.Range(spawnminz, spawnmaxz));

                float xrandom2 = UnityEngine.Random.Range(0, 2);
                float zrandom2 = UnityEngine.Random.Range(0, 2);
                if (xrandom2 == 1) spawnpoint.x = -spawnpoint.x;
                if (zrandom2 == 1) spawnpoint.z = -spawnpoint.z;
                return spawnpoint;
            }

            void FixedUpdate()
            {
                if (cpentity == null && plane == null) { OnDestroy(); return; }
                if (!PlayersAreMounted())
                {
                    counter = counter + 1f;
                    if (counter >= (SkyFallPlaneDespawn * 15f)) OnDestroy();
                }
                else counter = 0f;

                if (cpentity.transform.position == movetopoint) movetopoint = FindCoords();
                Vector3 targetDir = movetopoint - transform.position;
                Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, 5f * Time.deltaTime, 0.0F);

                cpentity.transform.position = Vector3.MoveTowards(cpentity.transform.position, movetopoint, (25f) * Time.deltaTime);
                cpentity.transform.rotation = Quaternion.LookRotation(newDir);

                plane.transform.position = cpentity.transform.position + new Vector3(0f, -5f, 0f);
                plane.transform.rotation = cpentity.transform.rotation;

                RefreshAll();
            }

            public void RefreshAll()
            {
                cpentity.transform.hasChanged = true;

                if (cpentity.children != null)
                    for (int i = 0; i < cpentity.children.Count; i++)
                    {
                        cpentity.children[i].transform.hasChanged = true;
                        cpentity.children[i].SendNetworkUpdateImmediate(false);
                        cpentity.children[i].UpdateNetworkGroup();
                    }
                cpentity.SendNetworkUpdateImmediate();
                cpentity.UpdateNetworkGroup();
            }

            void OnDestroy()
            {
                if (plane != null) { plane.Invoke("KillMessage", 0.1f); }
                if (cpentity != null) { cpentity.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

        #region Player Parachute Entity

        class PlayerParachute : BaseEntity
        {
            BaseEntity mount;
            Vector3 direction;
            Vector3 position;
            public bool moveforward;
            public bool rotright;
            public bool rotleft;

            void Awake()
            {
                mount = GetComponentInParent<BaseEntity>();
                if (mount == null) { OnDestroy(); return; }
                position = mount.transform.position;
                moveforward = false;
            }

            bool PlayerIsMounted()
            {
                bool flag = mount.GetComponent<BaseMountable>().IsMounted();
                return flag;
            }

            public void ChuteInput(InputState input, BasePlayer player)
            {
                if (input == null || player == null) return;
                if (input.WasJustPressed(BUTTON.FORWARD)) moveforward = true;
                if (input.WasJustReleased(BUTTON.FORWARD)) moveforward = false;
                if (input.WasJustPressed(BUTTON.RIGHT)) rotright = true;
                if (input.WasJustReleased(BUTTON.RIGHT)) rotright = false;
                if (input.WasJustPressed(BUTTON.LEFT)) rotleft = true;
                if (input.WasJustReleased(BUTTON.LEFT)) rotleft = false;
            }

            bool CheckForThermals(Vector3 currentpos)
            {
                if (Physics.Raycast(new Ray(currentpos, Vector3.down), thermalsHeight, 2097152))
                {
                    return true;
                }
                return false;
            }

            void FixedUpdate()
            {
                if (!PlayerIsMounted() || mount == null) { OnDestroy(); return; }
                if (Physics.Raycast(new Ray(mount.transform.position, Vector3.down), groundReleaseHeight, layerMask))
                {
                    OnDestroy();
                    return;
                }
                if (rotright) mount.transform.eulerAngles += new Vector3(0, 2, 0);
                else if (rotleft) mount.transform.eulerAngles += new Vector3(0, -2, 0);

                if (_instance.enableThermals && CheckForThermals(mount.transform.position))
                {
                    mount.transform.localPosition += ((transform.forward * parchuteFwdSpeed * 4f) * Time.deltaTime);
                    mount.transform.position = Vector3.MoveTowards(mount.transform.position, mount.transform.position + Vector3.up, (parachuteDownSpeed * 4f) * Time.deltaTime);
                    mount.transform.hasChanged = true;
                    mount.SendNetworkUpdateImmediate();
                    mount.UpdateNetworkGroup();
                    return;
                }

                if (moveforward) mount.transform.localPosition += ((transform.forward * parchuteFwdSpeed) * Time.deltaTime);

                mount.transform.position = Vector3.MoveTowards(mount.transform.position, mount.transform.position + Vector3.down, (parachuteDownSpeed) * Time.deltaTime);
                mount.transform.hasChanged = true;
                mount.SendNetworkUpdateImmediate();
                mount.UpdateNetworkGroup();
            }

            public void OnDestroy()
            {
                if (mount != null) { mount.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

        #region Player Jump Controller

        class PlayerJumpController : MonoBehaviour
        {
            BasePlayer player;
            public bool usedchute;
            Skyfall _instance;
            float dismountcounter;

            void Awake()
            {
                _instance = new Skyfall();
                player = GetComponentInParent<BasePlayer>() ?? null;
                if (player == null) { OnDestroy(); return; }
                usedchute = false;
                dismountcounter = 0f;
                player.ClearEntityQueue();
            }

            void FixedUpdate()
            {
                if (player == null) { OnDestroy(); return; }
                if (Physics.Raycast(new Ray(player.transform.position, Vector3.down), 3f, layerMask))
                {
                    OnDestroy();
                    return;
                }
                if (ForceDismountFromPlane)
                {
                    dismountcounter = dismountcounter + 0.1f;
                    if (dismountcounter >= ForceJumpTime) { _instance.DisMountPlayer(player); dismountcounter = 0f; }
                }
            }

            public void OnDestroy()
            {
                if (skyfallplayerlist.Contains(player.userID)) skyfallplayerlist.Remove(player.userID);
                if (isParachuting.Contains(player.userID)) isParachuting.Remove(player.userID);
                GameObject.Destroy(this);
            }
        }

        #endregion

    }
}