using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("NPC Grenades", "Sempai#3239", "1.2.2")]
    [Description("F1 grenades spawn various NPCs when thrown.")]
    public class NPCGrenades: CovalencePlugin
    {
        #region plugin refs

        [PluginReference]
        private Plugin Friends, Clans, Kits;

        #endregion

        #region Consts

        private static NPCGrenades plugin;
        private static System.Random random = new System.Random();
        private static VersionNumber previousVersion;

        public const int f1GrenadeId = 143803535;
        public const ulong scientistSkinID = 2640541557;
        public const ulong heavySkinID = 2640541496;
        public const ulong juggernautSkinID = 2647297156;
        public const ulong tunnelSkinID = 2676146196;
        public const ulong underwaterSkinID = 2676146329;
        public const ulong murdererSkinID = 2643502595;
        public const ulong scarecrowSkinID = 2647297210;
        public const ulong mummySkinID = 2643385137;
        public const ulong bearSkinID = 2647301111;
        public const ulong polarbearSkinID = 2868239755;
        public const ulong wolfSkinID = 2647303718;
        public const ulong boarSkinID = 2643502513;
        public const ulong stagSkinID = 2647297256;
        public const ulong chickenSkinID = 2647297056;
        public const ulong bradleySkinID = 2643385052;
        
        public const string scientistPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab";
        public const string heavyPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab";
        public const string scarecrowPrefab = "assets/prefabs/npc/scarecrow/scarecrow.prefab";
        public const string bearPrefab = "assets/rust.ai/agents/bear/bear.prefab";
        public const string polarbearPrefab = "assets/rust.ai/agents/bear/polarbear.prefab";
        public const string wolfPrefab = "assets/rust.ai/agents/wolf/wolf.prefab";
        public const string boarPrefab = "assets/rust.ai/agents/boar/boar.prefab";
        public const string stagPrefab = "assets/rust.ai/agents/stag/stag.prefab";
        public const string chickenPrefab = "assets/rust.ai/agents/chicken/chicken.prefab";
        public const string bradleyPrefab = "assets/prefabs/npc/m2bradley/bradleyapc.prefab";
        
        public const string murdererChatter = "assets/prefabs/npc/murderer/sound/breathing.prefab";
        public const string murdererDeath = "assets/prefabs/npc/murderer/sound/death.prefab";
        public const string fleshBloodImpact = "assets/bundled/prefabs/fx/impacts/slash/flesh/fleshbloodimpact.prefab";
        public const string explosionSound = "assets/prefabs/weapons/f1 grenade/effects/f1grenade_explosion.prefab";
        public const string bradleyExplosion = "assets/prefabs/npc/m2bradley/effects/bradley_explosion.prefab";

        public const string permScientist = "npcgrenades.scientist";
        public const string permHeavy = "npcgrenades.heavy";
        public const string permJuggernaut = "npcgrenades.juggernaut";
        public const string permTunnel = "npcgrenades.tunnel";
        public const string permUnderwater = "npcgrenades.underwater";
        public const string permMurderer = "npcgrenades.murderer";
        public const string permScarecrow = "npcgrenades.scarecrow";
        public const string permMummy = "npcgrenades.mummy";
        public const string permBear = "npcgrenades.bear";
        public const string permPolarbear = "npcgrenades.polarbear";
        public const string permWolf = "npcgrenades.wolf";
        public const string permBoar = "npcgrenades.boar";
        public const string permStag = "npcgrenades.stag";
        public const string permChicken = "npcgrenades.chicken";
        public const string permBradley = "npcgrenades.bradley";
        public const string permAdmin = "npcgrenades.admin";

        #endregion

        #region Language

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Syntax"] = "Invalid syntax, use: /npcnade.give <type> <SteamID/PlayerName>",
                ["WrongType"] = "Grenade type \"{0}\" not recognised, please check & try again.",
                ["Received"] = "You received a {0}!",
                ["PlayerReceived"] = "Player {0} ({1}) received a {2}!",
                ["Permission"] = "You do not have permission to use {0}!",
                ["NotEnabled"] = "{0} is not enabled!",
                ["NotAdmin"] = "You do not have permission to use that command!",
                ["PlayerNotFound"] = "Can't find a player with the name or ID: {0}",
                ["PlayersFound"] = "Multiple players found, please be more specific: {0}",
                ["UnderWater"] = "{0} ({1}) spawned under water and was killed.",
                ["InSafeZone"] = "{0} ({1}) spawned in as Safe Zone and was killed.",
                ["Outside"] = "{0} ({1}) spawned inside and was killed.",
                ["OnStructure"] = "{0} ({1}) spawned on a building and was killed.",
                ["IsInRock"] = "{0} ({1}) spawned inside terrain and was killed.",
                ["ConsoleSyntax"] = "Invalid syntax, use: npcnade.give <type> <SteamID/PlayerName>",
                ["InvalidNade"] = "Grenade type \"{0}\" not recognised, please check and try again!"
            }, this);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args) {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        private void Message(IPlayer player, string messageKey, params object[] args)
        {
            var message = GetMessage(messageKey, player.Id, args);
            if (config.options.usePrefix)
            {
                player.Reply(config.options.chatPrefix + message);
            }
            else
            {
                player.Reply(message);
            }
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player is NPCPlayer)
            {
                return;
            }
            var message = GetMessage(messageKey, player.UserIDString, args);
            if (config.options.usePrefix && config.options.chatPrefix != string.Empty)
            {
                player.ChatMessage(config.options.chatPrefix + message);
            }
            else
            {
                player.ChatMessage(message);
            }
        }

        #endregion

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            plugin = this;
        }

        private void Init()
        {
            LoadNadeInfo();
            permission.RegisterPermission(permAdmin, this);
            foreach (var item in NadeInfo.Keys)
            {
                permission.RegisterPermission(NadeInfo[item].Perm, this);
            }
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
                if (storedData == null)
                {
                    Puts("Data file is blank. Creating default data file.");
                    LoadDefaultData();
                }
            }
            catch (Exception ex)
            {
                if (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonReaderException)
                {
                    Puts($"Exception Type: {ex.GetType()}");
                    Puts("Data file contains errors. Either fix the errors or delete the data file and reload the plugin for default values.");
                    return;
                }
                throw;
            }
            LoadDefaultData();
        }

        private void Unload()
        {
            plugin = null;
            NadeInfo = null;
            GrenadeNPCData = null;
            BaseNpcData = null;
            BradleyAPCData = null;
            NPCInventories = null;
        }

        private void OnServerSave()
        {
            int delay  = random.Next(10, 30);
            timer.Once(delay, () =>
            {
                SaveData();
            });
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (IsNpcGrenade(entity.skinID))
            {
                NPCPlayer npc = entity as NPCPlayer;
                if (npc is ScientistNPC || npc is ScarecrowNPC)
                {
                    if (npc?.userID != null && GrenadeNPCData.ContainsKey(npc.userID) && !NPCInventories.ContainsKey(npc.userID)) 
                    {
                        npc.children.Clear();
                        ItemContainer[] source = { npc.inventory.containerMain, npc.inventory.containerWear, npc.inventory.containerBelt };
                        Inv npcInv = new Inv() { name = npc.displayName, };
                        NPCInventories.Add(npc.userID, npcInv);
                        for (int i = 0; i < source.Length; i++)
                        {
                            foreach (var item in source[i].itemList)
                            {
                                npcInv.inventory[i].Add(new InvContents
                                {
                                    ID = item.info.itemid,
                                    amount = item.amount,
                                    skinID = item.skin,
                                });
                            }
                        }
                    }
                    timer.Once(5.0f, () =>
                    {
                        RemoveNpcData(npc.userID);
                    });
                }
                else
                {
                    RemoveNpcData(entity.net.ID);
                }
            }
        }

        // Below adds proper grenade names to items added via kits/loadouts by plugins which don't specify a name
        // in the config. This means when players click on the item in inventory it keeps the NPC Nade name.
        // It is also needed for checks later on.
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (IsNpcGrenade(item.skin))
            {
                if (item.name == null)
                {
                    if (NadeInfo.ContainsKey(item.skin))
                    {
                        item.name = NadeInfo[item.skin].Name;
                    }
                }
            }
        }

        // This hook works regardless of whether player or NPC player. OnExplosiveThrown only hooks for players.
        private void OnExplosiveFuseSet(TimedExplosive explosive, float fuseLength)
        {
            var npc = explosive.creatorEntity as BasePlayer;
            if (npc == null || !npc.IsAlive())
                return;

            var item = npc.GetActiveItem();
            if (item == null || explosive == null)
                return;

            if (IsNpcGrenade(item.skin))
            {
                OnNpcNadeThrown(npc, explosive, item);
            }
        }

        // Changed to a custom call due to OnExplosiveThrown not hooking NPC throws
        private void OnNpcNadeThrown(BaseEntity thrower, BaseEntity entity, Item npcNade)
        {
            var player = thrower as BasePlayer;
            if (!IsNpcGrenade(npcNade.skin))
            {
                return;
            }
            else if (!NadeInfo[npcNade.skin].Enabled)
            {
                NextTick(() => {
                    entity.Kill();
                });
                if (player is NPCPlayer)
                {
                    Puts($"An NPC player is trying to throw an NPCGrenade which is not enabled in the config: {npcNade.name}");
                    return;
                }
                else
                {
                    GiveNade(player, npcNade.skin, npcNade.name, "refund");
                    Message(player, "NotEnabled", npcNade.name);
                    return;
                }
            }
            if (config.options.usePerms && !HasPermission(player, npcNade))
            {
                NextTick(() => {
                    entity.Kill();
                });
                GiveNade(player, npcNade.skin, npcNade.name, "refund");
                Message(player, "Permission", npcNade.name);
                return;
            }
            timer.Once(2.4f, () =>
            {
                if (entity != null)
                {
                    var position = entity.transform.position;
                    NextTick(() => {
                        entity.Kill();
                    });
                    if (player == null || !player.IsAlive())
                    {
                        return;
                    }
                    if (storedData.HumanNPC.ContainsKey(npcNade.name))
                    {
                        NPCPlayerData settings = new NPCPlayerData
                        {
                            Name = storedData.HumanNPC[npcNade.name].Name,
                            Prefab = storedData.HumanNPC[npcNade.name].Prefab,
                            Health = storedData.HumanNPC[npcNade.name].Health,
                            MaxRoamRange = storedData.HumanNPC[npcNade.name].MaxRoamRange,
                            SenseRange = storedData.HumanNPC[npcNade.name].SenseRange,
                            ListenRange = storedData.HumanNPC[npcNade.name].ListenRange,
                            AggroRange = storedData.HumanNPC[npcNade.name].AggroRange,
                            DeAggroRange = storedData.HumanNPC[npcNade.name].DeAggroRange,
                            TargetLostRange = storedData.HumanNPC[npcNade.name].TargetLostRange,
                            MemoryDuration = storedData.HumanNPC[npcNade.name].MemoryDuration,
                            VisionCone = storedData.HumanNPC[npcNade.name].VisionCone,
                            CheckVisionCone = storedData.HumanNPC[npcNade.name].CheckVisionCone,
                            CheckLOS = storedData.HumanNPC[npcNade.name].CheckLOS,
                            IgnoreNonVisionSneakers = storedData.HumanNPC[npcNade.name].IgnoreNonVisionSneakers,
                            DamageScale = storedData.HumanNPC[npcNade.name].DamageScale,
                            PeaceKeeper = storedData.HumanNPC[npcNade.name].PeaceKeeper,
                            IgnoreSafeZonePlayers = storedData.HumanNPC[npcNade.name].IgnoreSafeZonePlayers,
                            RadioChatter = storedData.HumanNPC[npcNade.name].RadioChatter,
                            DeathSound = storedData.HumanNPC[npcNade.name].DeathSound,
                            NumberToSpawn = storedData.HumanNPC[npcNade.name].NumberToSpawn,
                            SpawnRadius = storedData.HumanNPC[npcNade.name].SpawnRadius,
                            DespawnTime = storedData.HumanNPC[npcNade.name].DespawnTime,
                            KillInSafeZone = storedData.HumanNPC[npcNade.name].KillInSafeZone,
                            StripCorpseLoot = storedData.HumanNPC[npcNade.name].StripCorpseLoot,
                            KitList = storedData.HumanNPC[npcNade.name].KitList,
                            Speed = storedData.HumanNPC[npcNade.name].Speed,
                            Acceleration = storedData.HumanNPC[npcNade.name].Acceleration,
                            FastSpeedFraction = storedData.HumanNPC[npcNade.name].FastSpeedFraction,
                            NormalSpeedFraction = storedData.HumanNPC[npcNade.name].NormalSpeedFraction,
                            SlowSpeedFraction = storedData.HumanNPC[npcNade.name].SlowSpeedFraction,
                            SlowestSpeedFraction = storedData.HumanNPC[npcNade.name].SlowestSpeedFraction,
                            LowHealthMaxSpeedFraction = storedData.HumanNPC[npcNade.name].LowHealthMaxSpeedFraction,
                            TurnSpeed = storedData.HumanNPC[npcNade.name].TurnSpeed,
                            ExplosionSound = storedData.HumanNPC[npcNade.name].ExplosionSound
                        };
                        if (settings.Prefab.Contains("scarecrow"))
                        {
                            SpawnScarecrow(player, npcNade, position, settings);
                            return;
                        }
                        else
                        {
                            SpawnScientist(player, npcNade, position, settings);
                            return;
                        }
                    }
                    else if (storedData.AnimalNPC.ContainsKey(npcNade.name))
                    {
                        SpawnAnimal(player, npcNade, position);
                        return;
                    }
                    else if (storedData.BradleyNPC.ContainsKey(npcNade.name))
                    {
                        SpawnBradley(player, npcNade, position);
                        return;
                    }
                }
            });
        }

        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity, Item item)
        {
            if (item.skin == null)
            {
                return;
            }
            else if (!IsNpcGrenade(item.skin))
            {
                return;
            }
            OnNpcNadeThrown(player, entity, item);
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
            {
                return null;
            }
            if (IsNpcGrenade(entity.skinID))
            {
                // Stop melee armed NPC injuring themselves which happens when their swing misses the target
                if (info.Initiator == info.HitEntity)
                {
                    return true;
                }
            }
            if (!config.bradley.bradleyBaseDamage)
            {
                var damageType = info.damageTypes.GetMajorityDamageType();
                if (entity is BasePlayer)
                {
                    return null;
                }
                else if (damageType == DamageType.Blunt)
                {
                    if(entity.GetBuildingPrivilege() && info.WeaponPrefab.name == "MainCannonShell")
                    {
                        // Blunt damage from APC cannon shells blocked
                        CancelHit(info);
                        return true;
                    }
                }
                else if (damageType == DamageType.Bullet && info.Initiator is BradleyAPC)
                {
                    // Bullet damage from APC machine gun bullets blocked
                    CancelHit(info);
                    return true;
                }
            }

            return null;
        }

        private object OnNpcTarget(BaseEntity entity, BaseEntity target)
        {
            if (entity != null && target != null)
            {
                var isEntityNade = IsNpcGrenade(entity.skinID);
                var isTargetNade = IsNpcGrenade(target.skinID);

                if (isEntityNade && isTargetNade)
                {
                    return true;
                }
                else if (isEntityNade && !isTargetNade)
                {
                    // Target is Player
                    if (target is BasePlayer)
                    {
                        var player = target as BasePlayer;
                        if (player.IsSleeping() && config.options.sleeperSafe)
                        {
                            return true;
                        }
                        if (!config.options.attackOwner && entity.OwnerID == player.userID)
                        {
                            return true;
                        }
                        if (config.options.useFriends || config.options.useClans || config.options.useTeams)
                        {
                            if (entity.OwnerID != player.userID && IsFriend(entity.OwnerID, player.userID))
                            {
                                return true;
                            }
                            return null;
                        }
                    }
                    else if (target is NPCPlayer)
                    {
                        if (config.options.npcSafe)
                        {
                            return true;
                        }
                    }
                    else if (target is BaseNpc)
                    {
                        if (config.options.npcSafe)
                        {
                            return true;
                        }
                    }
                }
                else if (!isEntityNade && isTargetNade)
                {
                    if (entity is NPCPlayer && config.options.npcSafe)
                    {
                        return true;
                    }
                    else if (entity is BaseNpc && config.options.animalSafe)
                    {
                        return true;
                    }
                }
            }
            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BaseEntity target)
        {
            if (target != null)
            {
                if (config.options.turretSafe && IsNpcGrenade(target.skinID))
                {
                    return true;
                }
            }
            return null;
        }

        private object CanBradleyApcTarget(BradleyAPC bradley, BaseEntity target)
        {
            if (target != null)
            {
                if (IsNpcGrenade(target.skinID))
                {
                    if (config.options.bradleySafe)
                    {
                        return false;
                    }
                }
                var player = target as BasePlayer;
                if (IsNpcGrenade(bradley.skinID))
                {
                    if (player.IsSleeping() && config.options.sleeperSafe)
                    {
                        return false;
                    }
                    if (!config.options.attackOwner && bradley.OwnerID == player.userID)
                    {
                        return false;
                    }
                    if (config.options.useFriends || config.options.useClans || config.options.useTeams)
                    {
                        if (bradley.OwnerID != player.userID && IsFriend(bradley.OwnerID, player.userID))
                        {
                            return true;
                        }
                    }
                }
            }
            return null;
        }

        private object OnBradleyApcInitialize(BradleyAPC bradley)
        {
            if (!IsNpcGrenade(bradley.skinID))
            {
                return null;
            }
            else
            {
                var key = "Bradley Grenade";
                bradley.health = storedData.BradleyNPC[key].Health;
                bradley._maxHealth = storedData.BradleyNPC[key].Health;
                bradley.searchRange = storedData.BradleyNPC[key].SearchRange;
                bradley.viewDistance = storedData.BradleyNPC[key].ViewDistance;
                bradley.maxCratesToSpawn = storedData.BradleyNPC[key].CratesToSpawn;
                bradley.throttle = storedData.BradleyNPC[key].ThrottleResponse;
                bradley.leftThrottle = bradley.throttle;
                bradley.rightThrottle = bradley.throttle;
                bradley.ClearPath();
                bradley.currentPath.Clear();
                bradley.currentPathIndex = 0;
                bradley.DoAI = true;
                bradley.DoSimpleAI();

                var position = bradley.transform.position;
                for (int i = 0; i < storedData.BradleyNPC[key].PatrolPathNodes; i++)
                {
                    position = position + UnityEngine.Random.onUnitSphere * storedData.BradleyNPC[key].PatrolRange;
                    position.y = TerrainMeta.HeightMap.GetHeight(position);
                    bradley.currentPath.Add(position);
                }
                return true;
            }
            return null;
        }

        void OnEntitySpawned(NPCPlayerCorpse corpse)
        {
            if (corpse == null)
            {
                return;
            }

            Inv npcInv = new Inv();
            timer.Once(0.2f, () =>
            {

                if (corpse == null || corpse.IsDestroyed)
                {
                    return;
                }
                ulong id = corpse.playerSteamID;
                if (!NPCInventories.ContainsKey(id))
                {
                    return;
                }
                npcInv = NPCInventories[id];

                corpse._playerName = npcInv.name;
                corpse.lootPanelName = npcInv.name;

                var key = corpse._playerName + " Grenade";
                if (storedData.HumanNPC[key].StripCorpseLoot)
                {
                    corpse.containers[0].Clear();
                }
                else
                {
                    for (int i = 0; i < npcInv.inventory.Length; i++)
                    {
                        foreach (var item in npcInv.inventory[i])
                        {
                            var giveItem = ItemManager.CreateByItemID(item.ID, item.amount, item.skinID);
                            if (!giveItem.MoveToContainer(corpse.containers[i], -1, true))
                                giveItem.Remove();
                        }
                    }
                }
                timer.Once(5f, () => NPCInventories?.Remove(id));
            });
        }

        #endregion

        #region Main

        private void SpawnScientist(BasePlayer player, Item npcNade, Vector3 position, NPCPlayerData settings)
        {
            if (player == null || npcNade == null || position == null || settings == null)
            {
                return;
            }
            DoExplosion(settings.ExplosionSound, position);
            for (int i = 0; i < settings.NumberToSpawn; i++)
            {
                if (settings.NumberToSpawn > 1)
                {
                    position = position + UnityEngine.Random.onUnitSphere * settings.SpawnRadius;
                    position.y = TerrainMeta.HeightMap.GetHeight(position);
                }
                var npc = (ScientistNPC)GameManager.server.CreateEntity(settings.Prefab, position + new Vector3(0, 0.1f, 0), new Quaternion(), true);
                if (npc == null)
                {
                    return;
                }
                npc.Spawn();

                var nav = npc.GetComponent<BaseNavigator>(); 
                if (nav == null)
                {
                    return;
                }
                npc.NavAgent.enabled = true;
                nav.CanUseNavMesh = true; 
                nav.DefaultArea = "Walkable";
                npc.NavAgent.areaMask = 1;
                npc.NavAgent.agentTypeID = -1372625422;  
                npc.NavAgent.autoTraverseOffMeshLink = true; 
                npc.NavAgent.autoRepath = true;
                nav.CanUseCustomNav = true; 
                npc.NavAgent.baseOffset = -0.1f;
                nav.PlaceOnNavMesh();

                var brain = npc.gameObject.AddComponent<ScientistAI>();
                brain.Settings = settings;

                var move = npc.gameObject.AddComponent<ScientistMovement>();
                move.HomeLoc = position;
                move.Settings = settings;

                npc.enableSaving = false;
                npc.skinID = npcNade.skin;
                npc.OwnerID = player.userID;
                npc.displayName = settings.Name;
                npc.damageScale = settings.DamageScale;
                npc.startHealth = settings.Health;
                npc.InitializeHealth(settings.Health, settings.Health);
                npc.EnablePlayerCollider();

                timer.Once(0.2f, () =>
                {
                    if (npc == null || npc.IsDestroyed)
                    {
                        return;
                    }
                    if (SpawnAborted(player, npc, npcNade, position, npc.userID))
                    {
                        return;
                    }
                    GiveGrenadeNpcKit(npc, npcNade);
                    if (settings.DespawnTime > 0)
                    {
                        DespawnNPC(npc, npc.userID, settings.DespawnTime);
                    }
                    GrenadeNPCData.Add(npc.userID, npc);
                });
            }
        }

        private void SpawnScarecrow(BasePlayer player, Item npcNade, Vector3 position, NPCPlayerData settings)
        {
            if (player == null || npcNade == null || position == null || settings == null)
            {
                return;
            }
            DoExplosion(settings.ExplosionSound, position);
            for (int i = 0; i < settings.NumberToSpawn; i++)
            {
                if (settings.NumberToSpawn > 1)
                {
                    position = position + UnityEngine.Random.onUnitSphere * settings.SpawnRadius;
                    position.y = TerrainMeta.HeightMap.GetHeight(position);
                }

                var npc = (ScarecrowNPC)GameManager.server.CreateEntity(settings.Prefab, position + new Vector3(0, 0.1f, 0), new Quaternion(), true);
                
                if (npc == null)
                {
                    return;
                }
                npc.Spawn();

                var nav = npc.GetComponent<BaseNavigator>(); 
                if (nav == null)
                {
                    return;
                }
                npc.NavAgent.enabled = true;
                nav.CanUseNavMesh = true; 
                nav.DefaultArea = "Walkable";
                npc.NavAgent.areaMask = 1;
                npc.NavAgent.agentTypeID = -1372625422;  
                npc.NavAgent.autoTraverseOffMeshLink = true; 
                npc.NavAgent.autoRepath = true;
                nav.CanUseCustomNav = true; 
                npc.NavAgent.baseOffset = -0.1f;
                nav.PlaceOnNavMesh();

                var brain = npc.gameObject.AddComponent<ScarecrowAI>();
                brain.Settings = settings;

                var move = npc.gameObject.AddComponent<ScarecrowMovement>();
                move.HomeLoc = position;
                move.Settings = settings;

                npc.enableSaving = false;
                npc.skinID = npcNade.skin;
                npc.OwnerID = player.userID;
                npc.displayName = settings.Name;
                npc.damageScale = settings.DamageScale;
                npc.startHealth = settings.Health;
                npc.InitializeHealth(settings.Health, settings.Health);
                npc.EnablePlayerCollider();

                timer.Once(0.2f, () =>
                {
                    if (npc == null || npc.IsDestroyed)
                    {
                        return;
                    }
                    if (SpawnAborted(player, npc, npcNade, position, npc.userID))
                    {
                        return;
                    }
                    GiveGrenadeNpcKit(npc, npcNade);

                    if (settings.DespawnTime > 0)
                    {
                        DespawnNPC(npc, npc.userID, settings.DespawnTime);
                    }
                    GrenadeNPCData.Add(npc.userID, npc);
                });
            }
        }

        private void SpawnAnimal(BasePlayer player, Item npcNade, Vector3 position)
        {
            if (storedData.AnimalNPC.ContainsKey(npcNade.name))
            {
                string npcPrefab = storedData.AnimalNPC[npcNade.name].Prefab;
                string npcName = storedData.AnimalNPC[npcNade.name].Name;
                int spawnAmount = storedData.AnimalNPC[npcNade.name].NumberToSpawn;
                string exploSound = storedData.AnimalNPC[npcNade.name].ExplosionSound;

                DoExplosion(exploSound, position);

                for (int i = 0; i < spawnAmount; i++)
                {
                    if (spawnAmount > 1)
                    {
                        position = position + UnityEngine.Random.onUnitSphere * storedData.AnimalNPC[npcNade.name].SpawnRadius;
                        position.y = TerrainMeta.HeightMap.GetHeight(position);
                    }
                    BaseNpc npc = (BaseNpc)GameManager.server.CreateEntity(npcPrefab, position);
                    npc.Spawn();

                    npc.startHealth = storedData.AnimalNPC[npcNade.name].Health;
                    npc.InitializeHealth(storedData.AnimalNPC[npcNade.name].Health, storedData.AnimalNPC[npcNade.name].Health);
                    npc.OwnerID = player.userID;
                    npc.skinID = npcNade.skin;
                    npc.CurrentBehaviour = BaseNpc.Behaviour.Attack;
                    npc.SetFact(BaseNpc.Facts.CanTargetEnemies, 1, true, true);
                    npc.SetFact(BaseNpc.Facts.IsAggro, 1, true, true);

                    if (SpawnAborted(player, npc, npcNade, position, npc.net.ID))
                    {
                        return;
                    }
                    float despawnTime = storedData.AnimalNPC[npcNade.name].DespawnTime;
                    if (despawnTime > 0)
                    {
                        DespawnNPC(npc, npc.net.ID, despawnTime);
                    }
                    BaseNpcData.Add(npc.net.ID, npc);
                }
            }
        }
            
        private void SpawnBradley(BasePlayer player, Item npcNade, Vector3 position)
        {
            if (storedData.BradleyNPC.ContainsKey(npcNade.name))
            {
                string npcPrefab = storedData.BradleyNPC[npcNade.name].Prefab;
                string npcName = storedData.BradleyNPC[npcNade.name].Name;
                int spawnAmount = storedData.BradleyNPC[npcNade.name].NumberToSpawn;
                string exploSound = storedData.BradleyNPC[npcNade.name].ExplosionSound;

                DoExplosion(exploSound, position);

                for (int i = 0; i < spawnAmount; i++)
                {
                    if (spawnAmount > 1)
                    {
                        position = position + UnityEngine.Random.onUnitSphere * storedData.BradleyNPC[npcNade.name].SpawnRadius;
                        position.y = TerrainMeta.HeightMap.GetHeight(position);
                    }

                    BradleyAPC npc = (BradleyAPC)GameManager.server.CreateEntity(npcPrefab, position);
                    npc.OwnerID = player.userID;
                    npc.skinID = npcNade.skin;
                    npc.Spawn();

                    if (SpawnAborted(player, npc, npcNade, position, npc.net.ID))
                    {
                        return;
                    }
                    float despawnTime = storedData.BradleyNPC[npcNade.name].DespawnTime;
                    if (despawnTime > 0)
                    {
                        DespawnNPC(npc, npc.net.ID, despawnTime);
                    }
                    BradleyAPCData.Add(npc.net.ID, npc);
                }
            }
        }

        private void DespawnNPC(BaseEntity npc, ulong botId, float despawnTime)
        {
            timer.Once(despawnTime, () =>
            {
                if (npc != null && !npc.IsDestroyed)
                {
                    NextTick(() =>
                    {
                        npc.Kill();
                        RemoveNpcData(botId);
                    });
                    return;
                }
            });
        }

        #endregion

        #region Helpers

        private void CancelHit(HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.DidHit = false;
            info.DoHitEffects = false;
        }

        private object OnNpcKits(ulong npcUserID) // Prevents conflict with NPCKits.
        {
            return GrenadeNPCData.ContainsKey(npcUserID) ? true : (object)null;
        }

        private void GiveNade(BasePlayer player, ulong skinId, string nadeName, string reason)
        {
            if (player == null && skinId == null || nadeName == null || reason == null)
            {
                return;
            }
            Item npcNade = ItemManager.CreateByItemID(f1GrenadeId, 1, skinId);
            npcNade.name = nadeName;

            if (reason == "give")
            {
                player.inventory.GiveItem(npcNade);
                Message(player, "Received", nadeName);
            }
            else if (reason == "refund")
            {
                player.inventory.GiveItem(npcNade);
            }
        }

        public Item GiveInventoryItem(ItemContainer itemContainer, string shortName, int itemAmount, ulong skinId)
        {
            Item item = ItemManager.CreateByName(shortName, itemAmount, skinId);
            if (item == null) return null;
            if (!item.MoveToContainer(itemContainer))
            {
                item.Remove(0f);
                return null;
            }
            return item;
        }

        private bool HasPermission(BasePlayer player, Item npcNade)
        {
            if (player == null || npcNade == null)
            {
                return false;
            }
            if (player is NPCPlayer)
            {
                // Allow NPCs spawned by plugins which arm with NPCGrenades to work regardless of perms
                return true;
            }
            else if (storedData.HumanNPC.ContainsKey(npcNade.name))
            {
                string humanPerm = storedData.HumanNPC[npcNade.name].Permission;
                if (permission.UserHasPermission(player.UserIDString, humanPerm))
                {
                    return true;
                }
            }
            else if (storedData.AnimalNPC.ContainsKey(npcNade.name))
            {
                string animalPerm = storedData.AnimalNPC[npcNade.name].Permission;
                if (permission.UserHasPermission(player.UserIDString, animalPerm))
                {
                    return true;
                }
            }
            else if (storedData.BradleyNPC.ContainsKey(npcNade.name))
            {
                string apcPerm = storedData.BradleyNPC[npcNade.name].Permission;
                if (permission.UserHasPermission(player.UserIDString, apcPerm))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsNpcGrenade(ulong skinId)
        {
            if(NadeInfo.ContainsKey(skinId))
            {
                return true;
            }
            return false;
        }

        public Vector3 GetNavPoint(Vector3 position)
        {
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(position, out hit, 5, -1))
            {
                return position;
            }
            else if (Physics.RaycastAll(hit.position + new Vector3(0, 100, 0), Vector3.down, 99f, 1235288065).Any())
            {
                return position;
            }
            else if (hit.position.y < TerrainMeta.WaterMap.GetHeight(hit.position))
            {
                return position;
            }
            position = hit.position;
            return position;
        }

        private bool IsInSafeZone(Vector3 position)
        {
            int loop = Physics.OverlapSphereNonAlloc(position, 1f, Vis.colBuffer, 1 << 18, QueryTriggerInteraction.Collide);
            for (int i = 0; i < loop; i++)
            {
                Collider collider = Vis.colBuffer[i];
                if (collider.GetComponent<TriggerSafeZone>())
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsOnStructure(Vector3 position)
        {
            RaycastHit hit;
            var heightOffset = new Vector3(0, 0.1f, 0);
            if (Physics.Raycast(position + heightOffset, Vector3.down,
                    out hit, 4f, LayerMask.GetMask("Construction")) && hit.GetEntity().IsValid())
            {
                if (hit.GetEntity().name.Contains("building"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsInRock(Vector3 position)
        {
            RaycastHit hit;
            string[] colliders = new string[] { "rock", "cliff", "junk", "range", "invisible" };
            Physics.queriesHitBackfaces = true;
            if (Physics.Raycast(position, Vector3.up, out hit, 25f, 65536, QueryTriggerInteraction.Ignore))
            {
                if (colliders.Any(x => hit.collider?.gameObject?.name.Contains(x, CompareOptions.OrdinalIgnoreCase) != null))
                {
                    return true;
                }
            }
            Physics.queriesHitBackfaces = false;
            return false;
        }

        private void DoExplosion(string sound, Vector3 position)
        {
            try
            {
                Effect.server.Run(sound, position);
            }
            catch
            {
                Puts($"Invalid explosion effect path specified.");
            }
        }

        private IPlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in covalence.Players.Connected)
            {
                if (activePlayer.Id == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.Name.Contains(nameOrIdOrIp))
                    return activePlayer;
                if (activePlayer.Name.ToLower().Contains(nameOrIdOrIp.ToLower()))
                    return activePlayer;
                if (activePlayer.Address == nameOrIdOrIp)
                    return activePlayer;
            }
            return null;
        }

        private bool IsFriend(ulong playerId, ulong targetId)
        {
            if (playerId == 0 || targetId == 0)
            {
                return false;
            }
            if (playerId == targetId)
            {
                return true;
            }
            if (Clans)
            {
                var result = Clans?.Call("IsMemberOrAlly", playerId, targetId);
                if (result != null && Convert.ToBoolean(result))
                {
                    return true;
                }
            }
            if (Friends)
            {
                var result = Friends?.Call("AreFriends", playerId, targetId);
                if (result != null && Convert.ToBoolean(result))
                {
                    return true;
                }
            }
            RelationshipManager.PlayerTeam team;
            RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out team);
            if (team == null)
            {
                return false;
            }
            if (team.members.Contains(targetId))
            {
                return true;
            }
            return false;
        }

        private bool SpawnAborted(BasePlayer player, BaseEntity npc, Item npcNade, Vector3 position, ulong npcId)
        {
            if (npc == null || npc.IsDestroyed)
            {
                return true;
            }
            if (npc.WaterFactor() > 0.7f)
            {
                npc.Kill();
                Message(player, "UnderWater", npcNade.name, npcId);
                return true;
            }
            else if (IsInSafeZone(position))
            {
                npc.Kill();
                Message(player, "InSafeZone", npcNade.name, npcId);
                return true;
            }
            else if (!npc.IsOutside())
            {
                npc.Kill();
                Message(player, "Outside", npcNade.name, npcId);
                return true;
            }
            else if (IsOnStructure(position))
            {
                npc.Kill();
                Message(player, "OnStructure", npcNade.name, npcId);
                return true;
            }
            else if (IsInRock(position))
            {
                npc.Kill();
                Message(player, "IsInRock", npcNade.name, npcId);
                return true;
            }
            return false;
        }

        private void RemoveNpcData(ulong npcId)
        {
            if (GrenadeNPCData.ContainsKey(npcId))
            {
                GrenadeNPCData.Remove(npcId);
            }
            else if (BaseNpcData.ContainsKey(npcId))
            {
                BaseNpcData.Remove(npcId);
            }
            else if (BradleyAPCData.ContainsKey(npcId))
            {
                BradleyAPCData.Remove(npcId);
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }
        #endregion

        #region Kits

        private void GiveGrenadeNpcKit(NPCPlayer npc, Item npcNade)
        {
            if (storedData.HumanNPC.ContainsKey(npcNade.name))
            {
                int kitCount = storedData.HumanNPC[npcNade.name].KitList.Count();
                if (Kits)
                {
                    if (kitCount > 0)
                    {
                        var kit = storedData.HumanNPC[npcNade.name].KitList[random.Next(kitCount)];
                        object kitCheck = Kits?.CallHook("GetKitInfo", kit, true);
                        if (kitCheck == null)
                        {
                            Puts($"Kit: \"{kit}\" does not exist, using default kit.");
                        }
                        else
                        {
                            npc.inventory.Strip();
                            Kits?.Call($"GiveKit", npc, kit, true);
                            return;
                        }
                    }
                }

                if (npc == null) return;
                npc.inventory.Strip();

                foreach (var item in storedData.HumanNPC[npcNade.name].DefaultLoadout)
                {
                    var container = item.Container.ToLower();
                    if (container == "belt")
                    {
                        GiveInventoryItem(npc.inventory.containerBelt, item.Shortname, item.Amount, item.SkinID);
                    }
                    else if (container == "wear")
                    {
                        GiveInventoryItem(npc.inventory.containerWear, item.Shortname, item.Amount, item.SkinID);
                    }
                    else
                    {
                        GiveInventoryItem(npc.inventory.containerMain, item.Shortname, item.Amount, item.SkinID);
                    }
                }
            }
        }

        #endregion

        #region Commands

        [Command("npcnade.give")]
        private void CmdGiveNpcNade(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin))
            {
                Message(player, "NotAdmin");
                return;
            }
            else if (args.Length < 2)
            {
                if (player.IsServer)
                {
                    Message(player, "ConsoleSyntax");
                    return;
                }
                else
                {
                    Message(player, "Syntax");
                    return;
                }
            }
            var target = FindPlayer(args[1])?.Object as BasePlayer;
            if (target == null)
            {
                Message(player, "PlayerNotFound", args[1]);
                return;
            }
            string npcCmd = args[0].ToLower();
            ulong skinId = 0;
            string nadeName = string.Empty;
            bool isEnabled = false;
            foreach (var item in NadeInfo.Keys)
            {
                if (npcCmd == NadeInfo[item].Cmd)
                {
                    skinId = NadeInfo[item].ID;
                    nadeName = NadeInfo[item].Name;
                    isEnabled = NadeInfo[item].Enabled;
                    break;
                }
            }
            if (skinId == 0)
            {
                Message(player, "InvalidNade", npcCmd);
                return;
            }
            else if (nadeName != null && !isEnabled)
            {
                Message(player, "NotEnabled", nadeName);
                return;
            }
            GiveNade(target, skinId, nadeName, "give");
            Message(player, "PlayerReceived", target.displayName, target.userID, nadeName);
        }

        #endregion

        #region Brain AI

        private class ScarecrowAI : FacepunchBehaviour
        {
            private ScarecrowNPC Scarecrow;
            public NPCPlayerData Settings;
            public bool isEquippingWeapon = false;
            public AttackEntity CurrentWeapon { get; private set; }

            private void Start()
            {
                Scarecrow = GetComponent<ScarecrowNPC>();
                Invoke(nameof(InitBrain), 0.25f);
                Invoke(nameof(EquipWeapon), 1f);
                InvokeRepeating(nameof(MeleeAttack), 1f, 1f);
            }

            private void InitBrain()
            {
                Scarecrow.Brain.Navigator.MaxRoamDistanceFromHome = Settings.MaxRoamRange;
                Scarecrow.Brain.Navigator.BestRoamPointMaxDistance = Settings.MaxRoamRange;
                Scarecrow.Brain.Navigator.BestMovementPointMaxDistance = Settings.MaxRoamRange;
                Scarecrow.Brain.Navigator.Speed = Settings.Speed;
                Scarecrow.Brain.Navigator.Acceleration = Settings.Acceleration;
                Scarecrow.Brain.Navigator.FastSpeedFraction = Settings.FastSpeedFraction;
                Scarecrow.Brain.Navigator.NormalSpeedFraction = Settings.NormalSpeedFraction;
                Scarecrow.Brain.Navigator.SlowSpeedFraction = Settings.SlowSpeedFraction;
                Scarecrow.Brain.Navigator.SlowestSpeedFraction = Settings.SlowestSpeedFraction;
                Scarecrow.Brain.Navigator.LowHealthMaxSpeedFraction = Settings.LowHealthMaxSpeedFraction;
                Scarecrow.Brain.Navigator.TurnSpeed = Settings.TurnSpeed;

                Scarecrow.Brain.AllowedToSleep = false;
                Scarecrow.Brain.sleeping = false;
                Scarecrow.Brain.ForceSetAge(0);
                Scarecrow.Brain.SenseRange = Settings.SenseRange;
                Scarecrow.Brain.ListenRange = Settings.ListenRange;
                Scarecrow.Brain.HostileTargetsOnly = Settings.PeaceKeeper;
                Scarecrow.Brain.TargetLostRange = Settings.TargetLostRange;
                Scarecrow.Brain.CheckVisionCone = Settings.CheckVisionCone;
                Scarecrow.Brain.IgnoreSafeZonePlayers = Settings.IgnoreSafeZonePlayers;
                Scarecrow.Brain.IgnoreNonVisionSneakers = Settings.IgnoreNonVisionSneakers;
                Scarecrow.Brain.VisionCone = Vector3.Dot(Vector3.forward, Quaternion.Euler(0f, Settings.VisionCone, 0f) * Vector3.forward);

                Scarecrow.Brain.Senses.Init(Scarecrow, Settings.MemoryDuration, Settings.AggroRange, Settings.DeAggroRange, Settings.VisionCone,
                                            Settings.CheckVisionCone, Settings.CheckLOS, Settings.IgnoreNonVisionSneakers, Settings.ListenRange,
                                            Settings.PeaceKeeper, Scarecrow.Brain.MaxGroupSize > 0, Settings.IgnoreSafeZonePlayers, EntityType.Player, true);
            }

            private void MeleeAttack()
            {
                BaseEntity target = Scarecrow.Brain.Senses.GetNearestTarget(Settings.AggroRange);
                BaseMelee heldEntity = Scarecrow?.GetActiveItem()?.GetHeldEntity() as BaseMelee;

                if (target != null && Vector3.Distance(target.transform.position, Scarecrow.transform.position) < 1.5f)
                {
                    Scarecrow.StartAttacking(target);
                    Scarecrow.Brain.Navigator.SetFacingDirectionEntity(target);

                    Vector3 serverPos = target.ServerPosition - Scarecrow.ServerPosition;
                    Scarecrow.ServerRotation = Quaternion.LookRotation(serverPos.normalized);

                    heldEntity.StartAttackCooldown(heldEntity.repeatDelay * 2f);
                    Scarecrow.SignalBroadcast(BaseEntity.Signal.Attack, string.Empty, null);

                    if (!(heldEntity is Chainsaw))
                    {
                        if (heldEntity.swingEffect.isValid)
                        {
                            Effect.server.Run(heldEntity.swingEffect.resourcePath, heldEntity.transform.position, Vector3.forward, Scarecrow.net.connection, false);
                        }

                        plugin.timer.Once(0.4f, () =>
                        {
                            if (Scarecrow == null)
                            {
                                return;
                            }
                            Vector3 position = Scarecrow.eyes.position;
                            Vector3 direction = Scarecrow.eyes.BodyForward();
                            for (int i = 0; i < 2; ++i)
                            {
                                List<RaycastHit> list = Pool.GetList<RaycastHit>();
                                GamePhysics.TraceAll(new Ray(position - direction * (i == 0 ? 0.0f : 0.2f), direction), i == 0 ? 0.0f : heldEntity.attackRadius,
                                                    list, heldEntity.effectiveRange + 0.2f, 1219701521, QueryTriggerInteraction.UseGlobal);
                                bool flag = false;
                                for (int j = 0; j < list.Count; ++j)
                                {
                                    RaycastHit item = list[j];
                                    BaseEntity entity = item.GetEntity();
                                    if (entity != null && Scarecrow != null)
                                    {
                                        float single = 0.0f;
                                        foreach (Rust.DamageTypeEntry damageType in heldEntity.damageTypes)
                                        {
                                            single += damageType.amount;
                                        }
                                        entity.OnAttacked(new HitInfo(Scarecrow, entity, Rust.DamageType.Slash, single * Scarecrow.damageScale));
                                        HitInfo hitInfo = Pool.Get<HitInfo>();
                                        hitInfo.HitEntity = entity;
                                        hitInfo.HitPositionWorld = item.point;
                                        hitInfo.HitNormalWorld = -direction;
                                        if (entity is BaseNpc || entity is NPCPlayer || entity is BasePlayer)
                                        {
                                            hitInfo.HitMaterial = StringPool.Get("Flesh");
                                        }
                                        else
                                        {
                                            hitInfo.HitMaterial = StringPool.Get((item.GetCollider().sharedMaterial != null ? item.GetCollider().sharedMaterial.GetName() : "generic"));
                                        }
                                        string strikeEffectPath = heldEntity.GetStrikeEffectPath(heldEntity.name);
                                        if (strikeEffectPath == null)
                                        {
                                            Effect.server.ImpactEffect(hitInfo);
                                        }
                                        else
                                        {
                                            Effect.server.Run(strikeEffectPath, hitInfo.HitEntity, hitInfo.HitBone, hitInfo.HitPositionLocal, hitInfo.HitNormalLocal);
                                            Effect.server.Run(fleshBloodImpact, hitInfo.HitEntity, hitInfo.HitBone, hitInfo.HitPositionLocal, hitInfo.HitNormalLocal);
                                        }
                                        Pool.Free<HitInfo>(ref hitInfo);
                                        flag = true;
                                        if (!(entity != null) || entity.ShouldBlockProjectiles())
                                        {
                                            break;
                                        }
                                    }
                                }
                                Pool.FreeList<RaycastHit>(ref list);
                                if (flag)
                                {
                                    break;
                                }
                            }
                        });
                    }
                    else if (heldEntity is Chainsaw)
                    {
                        if (!(heldEntity as Chainsaw).EngineOn())
                        {
                            (heldEntity as Chainsaw).ServerNPCStart();
                        }
                        heldEntity.SetFlag(BaseEntity.Flags.Busy, true, false, true);
                        heldEntity.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);
                    }
                }
                else if (target != null && Vector3.Distance(target.transform.position, Scarecrow.transform.position) > 2.0f)
                {
                    if (heldEntity is Chainsaw)
                    {
                        if (!(heldEntity as Chainsaw).EngineOn())
                        {
                            (heldEntity as Chainsaw).ServerNPCStart();
                        }
                        heldEntity.SetFlag(BaseEntity.Flags.Busy, false, false, true);
                        heldEntity.SetFlag(BaseEntity.Flags.Reserved8, false, false, true);

                    }
                }
            }

            private void EquipWeapon()
            {
                if (!isEquippingWeapon)
                {
                    StartCoroutine(EquippingWeapon());
                }
            }

            private IEnumerator EquippingWeapon()
            {
                Item slot = null;
                if (Scarecrow.inventory.containerBelt != null)
                {
                    isEquippingWeapon = true;
                    if (slot == null)
                    {
                        for (int i = 0; i < Scarecrow.inventory.containerBelt.itemList.Count; i++)
                        {
                            Item item = Scarecrow.inventory.containerBelt.GetSlot(i);
                            if (item != null && item.GetHeldEntity() is AttackEntity)
                            {
                                slot = item;
                                break;
                            }
                        }
                    }
                    if (slot != null)
                    {
                        HeldEntity heldEntity = slot.GetHeldEntity() as HeldEntity;
                        if (heldEntity != null)
                        {
                            if (heldEntity is AttackEntity)
                            {
                                (heldEntity as AttackEntity).TopUpAmmo();
                            }
                            if (heldEntity is Chainsaw)
                            {
                                (heldEntity as Chainsaw).ServerNPCStart();
                            }
                        }
                        CurrentWeapon = heldEntity as AttackEntity;
                    }
                    isEquippingWeapon = false;
                    yield return null;
                }
            }
        }

        private class ScientistAI : FacepunchBehaviour
        {
            private ScientistNPC Scientist;
            public NPCPlayerData Settings;
            public bool isEquippingWeapon = false;
            public AttackEntity CurrentWeapon { get; private set; }

            private void Start()
            {
                Scientist = GetComponent<ScientistNPC>();
                Invoke(nameof(InitBrain), 0.1f);
                Invoke(nameof(EquipWeapon), 1f);
            }

            private void InitBrain()
            {
                Scientist.Brain.SwitchToState(AIState.Combat, 0);

                Scientist.Brain.Navigator.MaxRoamDistanceFromHome = Settings.MaxRoamRange;
                Scientist.Brain.Navigator.BestRoamPointMaxDistance = Settings.MaxRoamRange;
                Scientist.Brain.Navigator.BestMovementPointMaxDistance = Settings.MaxRoamRange;
                Scientist.Brain.Navigator.Speed = Settings.Speed;
                Scientist.Brain.Navigator.Acceleration = Settings.Acceleration;
                Scientist.Brain.Navigator.FastSpeedFraction = Settings.FastSpeedFraction;
                Scientist.Brain.Navigator.NormalSpeedFraction = Settings.NormalSpeedFraction;
                Scientist.Brain.Navigator.SlowSpeedFraction = Settings.SlowSpeedFraction;
                Scientist.Brain.Navigator.SlowestSpeedFraction = Settings.SlowestSpeedFraction;
                Scientist.Brain.Navigator.LowHealthMaxSpeedFraction = Settings.LowHealthMaxSpeedFraction;
                Scientist.Brain.Navigator.TurnSpeed = Settings.TurnSpeed;

                Scientist.Brain.AllowedToSleep = false;
                Scientist.Brain.sleeping = false;
                Scientist.Brain.ForceSetAge(0);
                Scientist.Brain.SenseRange = Settings.SenseRange;
                Scientist.Brain.ListenRange = Settings.ListenRange;
                Scientist.Brain.HostileTargetsOnly = Settings.PeaceKeeper;
                Scientist.Brain.TargetLostRange = Settings.TargetLostRange;
                Scientist.Brain.CheckVisionCone = Settings.CheckVisionCone;
                Scientist.Brain.IgnoreSafeZonePlayers = Settings.IgnoreSafeZonePlayers;
                Scientist.Brain.IgnoreNonVisionSneakers = Settings.IgnoreNonVisionSneakers;
                Scientist.Brain.VisionCone = Vector3.Dot(Vector3.forward, Quaternion.Euler(0f, Settings.VisionCone, 0f) * Vector3.forward);

                Scientist.Brain.Navigator.Init(Scientist, Scientist.NavAgent);
                Scientist.Brain.Senses.Init(Scientist, Settings.MemoryDuration, Settings.AggroRange, Settings.DeAggroRange, Settings.VisionCone,
                                        Settings.CheckVisionCone, Settings.CheckLOS, Settings.IgnoreNonVisionSneakers, Settings.ListenRange,
                                        Settings.PeaceKeeper, Scientist.Brain.MaxGroupSize > 0, Settings.IgnoreSafeZonePlayers, EntityType.Player, true);
            }

            private void EquipWeapon()
            {
                if (!isEquippingWeapon)
                {
                    StartCoroutine(EquippingWeapon());
                }
            }

            private IEnumerator EquippingWeapon()
            {
                Item slot = null;
                if (Scientist.inventory.containerBelt != null)
                {
                    isEquippingWeapon = true;

                    if (slot == null)
                    {
                        for (int i = 0; i < Scientist.inventory.containerBelt.itemList.Count; i++)
                        {
                            Item item = Scientist.inventory.containerBelt.GetSlot(i);
                            if (item != null && item.GetHeldEntity() is AttackEntity)
                            {
                                slot = item;
                                break;
                            }
                        }
                    }

                    if (slot != null)
                    {
                        HeldEntity heldEntity = slot.GetHeldEntity() as HeldEntity;
                        if (heldEntity != null)
                        {
                            if (heldEntity is AttackEntity)
                            {
                                (heldEntity as AttackEntity).TopUpAmmo();
                            }
                            if (heldEntity is Chainsaw)
                            {
                                (heldEntity as Chainsaw).ServerNPCStart();
                            }
                        }
                        CurrentWeapon = heldEntity as AttackEntity;
                    }
                    isEquippingWeapon = false;
                    yield return null;
                }
            }
        }

        #endregion

        #region Scientist Movement

        private class ScientistMovement : MonoBehaviour
        {
            public ScientistNPC Scientist;
            public Vector3 HomeLoc;
            public NPCPlayerData Settings;
            public bool returningHome = false;
            public bool isRoaming = true;

            private void Start()
            {
                Scientist = GetComponent<ScientistNPC>();
                Invoke(nameof(Init), 1f);
                InvokeRepeating(nameof(MoveScientist), 2f, 8f);
            }

            private void Init()
            {
                Scientist.Brain.Navigator.Destination = HomeLoc;
            }

            private void ClearSenses()
            {
                if (!Scientist.HasBrain)
                {
                    return;
                }
                Scientist.Brain.Senses.Players.Clear();
                Scientist.Brain.Senses.Memory.Players.Clear();
                Scientist.Brain.Senses.Memory.Targets.Clear();
                Scientist.Brain.Senses.Memory.Threats.Clear();  
                Scientist.Brain.Senses.Memory.LOS.Clear();
                Scientist.Brain.Senses.Memory.All.Clear();
                //Scientist.Brain.SwitchToState(AIState.Idle, 0);
                Scientist.Brain.SwitchToState(AIState.Roam, 0);
            }
            private void MoveScientist()
            {
                if (Scientist == null || Scientist.IsDestroyed || !Scientist.HasBrain)
                {
                    return;
                }

                if (Scientist.WaterFactor() > 0.7f)
                {
                    Scientist.Kill();
                    return;
                }

                if (Scientist.Brain.Senses.Memory.Targets.Count > 0)
                {
                    for (var i = 0; i < Scientist.Brain.Senses.Memory.Targets.Count; i++)
                    {
                        BaseEntity target = Scientist.Brain.Senses.Memory.Targets[i];
                        BasePlayer player = target as BasePlayer;
                        if (target == null || !player.IsAlive())
                        {
                            ClearSenses();
                            returningHome = true;
                            isRoaming = false;
                            return;
                        }
                        if (Scientist.Distance(player.transform.position) > Settings.TargetLostRange)
                        {
                            ClearSenses();
                            returningHome = true;
                            isRoaming = false;
                            return;
                        }
                        if (config.options.attackOwner == true && Scientist.OwnerID == player.userID)
                        {
                            Scientist.Brain.SwitchToState(AIState.Attack, 0);
                            return;
                        }
                        if (config.options.useFriends || config.options.useClans || config.options.useTeams)
                        {
                            if (Scientist.OwnerID != player.userID && !plugin.IsFriend(Scientist.OwnerID, player.userID))
                            {
                                Scientist.Brain.SwitchToState(AIState.Attack, 0);
                                return;
                            }
                        }
                    }
                }
                var distanceHome = Vector3.Distance(Scientist.transform.position, HomeLoc);
                if (returningHome == false)
                {
                    if (isRoaming == true && distanceHome > Settings.MaxRoamRange)
                    {
                        returningHome = true;
                        isRoaming = false;
                        return;
                    }
                    if (isRoaming == true && distanceHome < Settings.MaxRoamRange)
                    {
                        Vector3 random = UnityEngine.Random.insideUnitCircle.normalized * Settings.MaxRoamRange;
                        Vector3 newPos = plugin.GetNavPoint(Scientist.transform.position + new Vector3(random.x, 0f, random.y));
                        SetDest(newPos);
                        return;
                    }
                }
                if (returningHome && distanceHome > 2)
                {
                    if (Scientist.Brain.Navigator.Destination == HomeLoc)
                    {
                        return;
                    }
                    ClearSenses();
                    SetDest(HomeLoc);
                    return;
                }
                returningHome = false;
                isRoaming = true;
            }

            private void SetDest(Vector3 position)
            {
                Scientist.Brain.Navigator.Destination = position;
                Scientist.Brain.Navigator.SetDestination(position, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
                Scientist.Brain.SwitchToState(AIState.Roam, 0);
            }

            private void OnDestroy()
            {
                if (Scientist != null && !Scientist.IsDestroyed)
                {
                    Scientist.Kill();
                }
                CancelInvoke(nameof(MoveScientist));
            }
        }

        #endregion

        #region Scarecrow Movement

        private class ScarecrowMovement : MonoBehaviour
        {
            public ScarecrowNPC Scarecrow;
            public Vector3 HomeLoc;
            public Vector3 TargetPos;
            public NPCPlayerData Settings;
            public bool returningHome = false;
            public bool isRoaming = true;
            public StateStatus status = StateStatus.Error;

            private void Start()
            {
                Scarecrow = GetComponent<ScarecrowNPC>();
                Invoke(nameof(Init), 1f);
                InvokeRepeating(nameof(MoveScarecrow), 2f, 4f);
                InvokeRepeating(nameof(CheckPosition), 1f, 1f);
                if (Settings.RadioChatter)
                {
                    InvokeRepeating(nameof(BreathingChatter), 2f, 10f);
                }
            }

            private void Init()
            {
                Scarecrow.Brain.Navigator.Destination = HomeLoc;
                Scarecrow.Brain.SwitchToState(AIState.Roam, 0);
            }

            private void CheckPosition()
            {
                if (Scarecrow.Brain.Navigator.Destination == HomeLoc)
                {
                    var distanceToHome = Vector3.Distance(Scarecrow.transform.position, HomeLoc);
                    if (distanceToHome < 2)
                    {
                        Scarecrow.Brain.Navigator.Stop();
                        Scarecrow.Brain.SwitchToState(AIState.Idle, 0);
                        status = StateStatus.Finished;
                        returningHome = false;
                        isRoaming = true;
                        return;
                    }
                }
                else if (Scarecrow.Brain.Navigator.Destination == TargetPos)
                {
                    var distanceToTarget = Vector3.Distance(Scarecrow.transform.position, TargetPos);
                    if (distanceToTarget < 2)
                    {
                        Scarecrow.Brain.Navigator.Stop();
                        Scarecrow.Brain.SwitchToState(AIState.Idle, 0);
                        status = StateStatus.Finished;
                        return;
                    }
                }
            }

            private void ClearSenses()
            {
                if (!Scarecrow.HasBrain)
                {
                    return;
                }
                Scarecrow.Brain.Senses.Players.Clear();
                Scarecrow.Brain.Senses.Memory.Players.Clear();
                Scarecrow.Brain.Senses.Memory.Targets.Clear();
                Scarecrow.Brain.Senses.Memory.Threats.Clear();  
                Scarecrow.Brain.Senses.Memory.LOS.Clear();
                Scarecrow.Brain.Senses.Memory.All.Clear();
                Scarecrow.Brain.Navigator.ClearFacingDirectionOverride();
                Scarecrow.Brain.SwitchToState(AIState.Roam, 0);
                status = StateStatus.Finished;
            }

            private void MoveScarecrow()
            {
                if (Scarecrow.WaterFactor() > 0.8f)
                {
                    Scarecrow.Kill();
                    return;
                }
                if (Scarecrow.Brain.Senses.Memory.Targets.Count > 0)
                {
                    for (var i = 0; i < Scarecrow.Brain.Senses.Memory.Targets.Count; i++)
                    {
                        BasePlayer player = Scarecrow.Brain.Senses.Memory.Targets[i] as BasePlayer;
                        if (player == null || !player.IsAlive())
                        {
                            ClearSenses();
                            returningHome = true;
                            isRoaming = false;
                            return;
                        }
                        else if (Scarecrow.Distance(player.transform.position) > Settings.TargetLostRange)
                        {
                            ClearSenses();
                            returningHome = true;
                            isRoaming = false;
                            return;
                        }
                        else if (config.options.attackOwner && Scarecrow.OwnerID == player.userID)
                        {
                            Scarecrow.Brain.SwitchToState(AIState.Attack, 0);
                            return;
                        }
                        else if (config.options.useFriends || config.options.useClans || config.options.useTeams)
                        {
                            if (Scarecrow.OwnerID != player.userID && !plugin.IsFriend(Scarecrow.OwnerID, player.userID))
                            {
                                Scarecrow.Brain.SwitchToState(AIState.Attack, 0);
                                return;
                            }
                        }
                    }
                }

                if (status == StateStatus.Finished || status == StateStatus.Error)
                {
                    var distanceHome = Vector3.Distance(Scarecrow.transform.position, HomeLoc);
                    if (!returningHome)
                    {
                        if (isRoaming && distanceHome > Settings.MaxRoamRange)
                        {
                            returningHome = true;
                            isRoaming = false;
                            return;
                        }
                        if (isRoaming && distanceHome < Settings.MaxRoamRange)
                        {
                            Vector3 random = UnityEngine.Random.insideUnitCircle.normalized * Settings.MaxRoamRange;
                            Vector3 newPos = plugin.GetNavPoint(Scarecrow.transform.position + new Vector3(random.x, 0f, random.y));
                            SetDest(newPos, Scarecrow);
                            return;
                        }
                    }
                    else if (returningHome)
                    {
                        ClearSenses();
                        SetDest(HomeLoc, Scarecrow);
                        return;
                    }
                }
            }

            private void SetDest(Vector3 position, ScarecrowNPC scarecrow)
            {
                TargetPos = position;
                scarecrow.Brain.Navigator.Destination = position;
                Scarecrow.Brain.SwitchToState(AIState.Roam, 0);
                scarecrow.Brain.Navigator.SetDestination(position, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
                status = StateStatus.Running;
            }

            private void BreathingChatter()
            {
                BaseEntity target = Scarecrow.Brain.Senses.GetNearestTarget(Settings.AggroRange);
                if (target != null && Scarecrow.IsAlive() && Vector3.Distance(Scarecrow.transform.position, target.transform.position) < Settings.AggroRange)
                {
                    Effect.server.Run(murdererChatter, Scarecrow, StringPool.Get("head"), Vector3.zero, Vector3.zero, null, false);
                }
            }

            private void OnDestroy()
            {
                if (Scarecrow != null && !Scarecrow.IsDestroyed)
                {
                    Scarecrow.Kill();
                }
                if (Settings.DeathSound)
                {
                    Effect.server.Run(murdererDeath, Scarecrow.transform.position);
                }
                CancelInvoke(nameof(MoveScarecrow));
                CancelInvoke(nameof(BreathingChatter));
                CancelInvoke(nameof(CheckPosition));
            }
        }

        #endregion

        #region Config

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "General Options")]
            public Options options;
            [JsonProperty(PropertyName = "Human NPC Options")]
            public Human human;
            [JsonProperty(PropertyName = "Animal NPC Options")]
            public Animal animal;
            [JsonProperty(PropertyName = "Bradley APC Options")]
            public Bradley bradley;
            
            public class Options
            {
                [JsonProperty(PropertyName = "Attack Owner")]
                public bool attackOwner;
                [JsonProperty(PropertyName = "Use Friends")]
                public bool useFriends;
                [JsonProperty(PropertyName = "Use Clans")]
                public bool useClans;
                [JsonProperty(PropertyName = "Use Teams")]
                public bool useTeams;
                [JsonProperty(PropertyName = "Use Permissions")]
                public bool usePerms;
                [JsonProperty(PropertyName = "Chat Prefix")]
                public string chatPrefix;
                [JsonProperty(PropertyName = "Use Chat Prefix")]
                public bool usePrefix;
                [JsonProperty(PropertyName = "NPC Safe")]
                public bool npcSafe;
                [JsonProperty(PropertyName = "Bradley Safe")]
                public bool bradleySafe;
                [JsonProperty(PropertyName = "Turret Safe")]
                public bool turretSafe;
                [JsonProperty(PropertyName = "Animal Safe")]
                public bool animalSafe;
                [JsonProperty(PropertyName = "Sleeper Safe")]
                public bool sleeperSafe;
            }
            public class Human
            {
                [JsonProperty(PropertyName = "Scientist Enabled")]
                public bool npcScientist;
                [JsonProperty(PropertyName = "Heavy Scientist Enabled")]
                public bool npcHeavy;
                [JsonProperty(PropertyName = "Juggernaut Enabled")]
                public bool npcJuggernaut;
                [JsonProperty(PropertyName = "Tunnel Dweller Enabled")]
                public bool npcTunnel;
                [JsonProperty(PropertyName = "Underwater Dweller Enabled")]
                public bool npcUnderwater;
                [JsonProperty(PropertyName = "Murderer Enabled")]
                public bool npcMurderer;
                [JsonProperty(PropertyName = "Scarecrow Enabled")]
                public bool npcScarecrow;
                [JsonProperty(PropertyName = "Mummy Enabled")]
                public bool npcMummy;
            }
            public class Animal
            {
                [JsonProperty(PropertyName = "Bear Enabled")]
                public bool npcBear;
                [JsonProperty(PropertyName = "Polar Bear Enabled")]
                public bool npcPolarbear;
                [JsonProperty(PropertyName = "Wolf Enabled")]
                public bool npcWolf;
                [JsonProperty(PropertyName = "Boar Enabled")]
                public bool npcBoar;
                [JsonProperty(PropertyName = "Stag Enabled")]
                public bool npcStag;
                [JsonProperty(PropertyName = "Chicken Enabled")]
                public bool npcChicken;
            }
            public class Bradley
            {
                [JsonProperty(PropertyName = "Bradley APC Enabled")]
                public bool npcBradley;
                [JsonProperty(PropertyName = "Bradley Can Damage Player Bases")]
                public bool bradleyBaseDamage;
            }
            public VersionNumber Version { get; set; }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                options = new ConfigData.Options
                {
                    attackOwner = false,
                    useFriends = true,
                    useClans = true,
                    useTeams = true,
                    usePerms = true,
                    chatPrefix = "[NPC Grenades]: ",
                    usePrefix = true,
                    npcSafe = true,
                    bradleySafe = true,
                    turretSafe = true,
                    animalSafe = true,
                    sleeperSafe = true
                },
                human = new ConfigData.Human
                {
                    npcScientist = true,
                    npcHeavy = true,
                    npcJuggernaut = true,
                    npcTunnel = true,
                    npcUnderwater = true,
                    npcMurderer = true,
                    npcScarecrow = true,
                    npcMummy = true
                },
                animal = new ConfigData.Animal
                {
                    npcBear = true,
                    npcPolarbear = true,
                    npcWolf = true,
                    npcBoar = true,
                    npcStag = true,
                    npcChicken = true
                },
                bradley = new ConfigData.Bradley
                {
                    npcBradley = true,
                    bradleyBaseDamage = false
                },
                Version = Version
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                if (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonReaderException)
                {
                    Puts($"Exception Type: {ex.GetType()}");
                    LoadDefaultConfig();
                    return;
                }
                throw;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Configuration file missing or corrupt, creating default config file.");
            config = GetDefaultConfig();
            SaveConfig();
        }
        
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void UpdateConfigValues()
        {
            previousVersion = config.Version;

            ConfigData defaultConfig = GetDefaultConfig();
            if (config.Version < Version)
            {
                Puts("Config update detected! Updating config file...");
                if (config.Version < new VersionNumber(1, 1, 0))
                {
                    config.options.useFriends = defaultConfig.options.useFriends;
                    config.options.useClans = defaultConfig.options.useClans;
                    config.options.useTeams = defaultConfig.options.useTeams;
                    config.options.usePerms = defaultConfig.options.usePerms;
                    config.human.npcScientist = defaultConfig.human.npcScientist;
                    config.human.npcHeavy = defaultConfig.human.npcHeavy;
                    config.human.npcJuggernaut = defaultConfig.human.npcJuggernaut;
                    config.human.npcMurderer = defaultConfig.human.npcMurderer;
                    config.human.npcScarecrow = defaultConfig.human.npcScarecrow;
                    config.human.npcMummy = defaultConfig.human.npcMummy;
                    config.animal.npcBear = defaultConfig.animal.npcBear;
                    config.animal.npcWolf = defaultConfig.animal.npcWolf;
                    config.animal.npcBoar = defaultConfig.animal.npcBoar;
                    config.animal.npcStag = defaultConfig.animal.npcStag;
                    config.animal.npcChicken = defaultConfig.animal.npcChicken;
                    config.bradley.npcBradley = defaultConfig.bradley.npcBradley;
                }
                if (config.Version < new VersionNumber(1, 1, 4))
                {
                    config.options.chatPrefix = defaultConfig.options.chatPrefix;
                    config.options.usePrefix = defaultConfig.options.usePrefix;
                    config.options.npcSafe = defaultConfig.options.npcSafe;
                    config.options.bradleySafe = defaultConfig.options.bradleySafe;
                    config.options.turretSafe = defaultConfig.options.turretSafe;
                    config.options.animalSafe = defaultConfig.options.animalSafe;
                }
                if (config.Version < new VersionNumber(1, 1, 5))
                {
                    config.options.attackOwner = defaultConfig.options.attackOwner;
                    config.options.sleeperSafe = defaultConfig.options.sleeperSafe;
                }
                if (config.Version < new VersionNumber(1, 1, 8))
                {
                    config.human.npcTunnel = defaultConfig.human.npcTunnel;
                    config.human.npcUnderwater = defaultConfig.human.npcUnderwater;
                    config.human.npcMurderer = defaultConfig.human.npcMurderer;
                    config.human.npcMummy = defaultConfig.human.npcMummy;
                    config.human.npcScarecrow = defaultConfig.human.npcScarecrow;
                }
                if (config.Version < new VersionNumber(1, 1, 12))
                {
                    config.human.npcMurderer = defaultConfig.human.npcMurderer;
                    config.human.npcMummy = defaultConfig.human.npcMummy;
                    config.human.npcScarecrow = defaultConfig.human.npcScarecrow;
                }
                if (config.Version < new VersionNumber(1, 1, 19))
                {
                    config.bradley.bradleyBaseDamage = defaultConfig.bradley.bradleyBaseDamage;
                }
                if (config.Version < new VersionNumber(1, 2, 1))
                {
                    config.animal.npcPolarbear = defaultConfig.animal.npcPolarbear;
                }
                Puts("Config update complete!");
            }
            config.Version = Version;
            SaveConfig();
        }

        #endregion

        #region Temporary Data

        private Dictionary<ulong, NPCPlayer> GrenadeNPCData = new Dictionary<ulong, NPCPlayer>();
        private Dictionary<ulong, BaseNpc> BaseNpcData = new Dictionary<ulong, BaseNpc>();
        private Dictionary<ulong, BradleyAPC> BradleyAPCData = new Dictionary<ulong, BradleyAPC>();
        private Dictionary<ulong, Inv> NPCInventories = new Dictionary<ulong, Inv>();
        private Dictionary<ulong, GrenadeData> NadeInfo = new Dictionary<ulong, GrenadeData>();

        private class Inv
        {
            public string name;
            public List<InvContents>[] inventory = { new List<InvContents>(), new List<InvContents>(), new List<InvContents>() };
        }

        private class InvContents
        {
            public int ID;
            public int amount; 
            public ulong skinID;
        }

        private class GrenadeData
        {
            public string Name;
            public ulong ID;
            public string Cmd;
            public bool Enabled;
            public string Perm;
            public string Prefab;
        }

        private void LoadNadeInfo()
        {
            NadeInfo.Add(scientistSkinID, new GrenadeData());
            NadeInfo[scientistSkinID].Name = "Scientist Grenade";
            NadeInfo[scientistSkinID].ID = scientistSkinID;
            NadeInfo[scientistSkinID].Cmd = "scientist";
            NadeInfo[scientistSkinID].Enabled = config.human.npcScientist;
            NadeInfo[scientistSkinID].Perm = permScientist;
            NadeInfo[scientistSkinID].Prefab = scientistPrefab;

            NadeInfo.Add(heavySkinID, new GrenadeData());
            NadeInfo[heavySkinID].Name = "Heavy Scientist Grenade";
            NadeInfo[heavySkinID].ID = heavySkinID;
            NadeInfo[heavySkinID].Cmd = "heavy";
            NadeInfo[heavySkinID].Enabled = config.human.npcHeavy;
            NadeInfo[heavySkinID].Perm = permHeavy;
            NadeInfo[heavySkinID].Prefab = heavyPrefab;

            NadeInfo.Add(juggernautSkinID, new GrenadeData());
            NadeInfo[juggernautSkinID].Name = "Juggernaut Grenade";
            NadeInfo[juggernautSkinID].ID = juggernautSkinID;
            NadeInfo[juggernautSkinID].Cmd = "juggernaut";
            NadeInfo[juggernautSkinID].Enabled = config.human.npcJuggernaut;
            NadeInfo[juggernautSkinID].Perm = permJuggernaut;
            NadeInfo[juggernautSkinID].Prefab = heavyPrefab;

            NadeInfo.Add(tunnelSkinID, new GrenadeData());
            NadeInfo[tunnelSkinID].Name = "Tunnel Dweller Grenade";
            NadeInfo[tunnelSkinID].ID = tunnelSkinID;
            NadeInfo[tunnelSkinID].Cmd = "tunnel";
            NadeInfo[tunnelSkinID].Enabled = config.human.npcTunnel;
            NadeInfo[tunnelSkinID].Perm = permTunnel;
            NadeInfo[tunnelSkinID].Prefab = scientistPrefab;

            NadeInfo.Add(underwaterSkinID, new GrenadeData());
            NadeInfo[underwaterSkinID].Name = "Underwater Dweller Grenade";
            NadeInfo[underwaterSkinID].ID = underwaterSkinID;
            NadeInfo[underwaterSkinID].Cmd = "underwater";
            NadeInfo[underwaterSkinID].Enabled = config.human.npcUnderwater;
            NadeInfo[underwaterSkinID].Perm = permUnderwater;
            NadeInfo[underwaterSkinID].Prefab = scientistPrefab;

            NadeInfo.Add(murdererSkinID, new GrenadeData());
            NadeInfo[murdererSkinID].Name = "Murderer Grenade";
            NadeInfo[murdererSkinID].ID = murdererSkinID;
            NadeInfo[murdererSkinID].Cmd = "murderer";
            NadeInfo[murdererSkinID].Enabled = config.human.npcMurderer;
            NadeInfo[murdererSkinID].Perm = permMurderer;
            NadeInfo[murdererSkinID].Prefab = scarecrowPrefab;

            NadeInfo.Add(scarecrowSkinID, new GrenadeData());
            NadeInfo[scarecrowSkinID].Name = "Scarecrow Grenade";
            NadeInfo[scarecrowSkinID].ID = scarecrowSkinID;
            NadeInfo[scarecrowSkinID].Cmd = "scarecrow";
            NadeInfo[scarecrowSkinID].Enabled = config.human.npcScarecrow;
            NadeInfo[scarecrowSkinID].Perm = permScarecrow;
            NadeInfo[scarecrowSkinID].Prefab = scarecrowPrefab;

            NadeInfo.Add(mummySkinID, new GrenadeData());
            NadeInfo[mummySkinID].Name = "Mummy Grenade";
            NadeInfo[mummySkinID].ID = mummySkinID;
            NadeInfo[mummySkinID].Cmd = "mummy";
            NadeInfo[mummySkinID].Enabled = config.human.npcMummy;
            NadeInfo[mummySkinID].Perm = permMummy;
            NadeInfo[mummySkinID].Prefab = scarecrowPrefab;

            NadeInfo.Add(bearSkinID, new GrenadeData());
            NadeInfo[bearSkinID].Name = "Bear Grenade";
            NadeInfo[bearSkinID].ID = bearSkinID;
            NadeInfo[bearSkinID].Cmd = "bear";
            NadeInfo[bearSkinID].Enabled = config.animal.npcBear;
            NadeInfo[bearSkinID].Perm = permBear;
            NadeInfo[bearSkinID].Prefab = bearPrefab;

            NadeInfo.Add(polarbearSkinID, new GrenadeData());
            NadeInfo[polarbearSkinID].Name = "Polar Bear Grenade";
            NadeInfo[polarbearSkinID].ID = polarbearSkinID;
            NadeInfo[polarbearSkinID].Cmd = "polarbear";
            NadeInfo[polarbearSkinID].Enabled = config.animal.npcPolarbear;
            NadeInfo[polarbearSkinID].Perm = permPolarbear;
            NadeInfo[polarbearSkinID].Prefab = polarbearPrefab;

            NadeInfo.Add(wolfSkinID, new GrenadeData());
            NadeInfo[wolfSkinID].Name = "Wolf Grenade";
            NadeInfo[wolfSkinID].ID = wolfSkinID;
            NadeInfo[wolfSkinID].Cmd = "wolf";
            NadeInfo[wolfSkinID].Enabled = config.animal.npcWolf;
            NadeInfo[wolfSkinID].Perm = permWolf;
            NadeInfo[wolfSkinID].Prefab = wolfPrefab;

            NadeInfo.Add(boarSkinID, new GrenadeData());
            NadeInfo[boarSkinID].Name = "Boar Grenade";
            NadeInfo[boarSkinID].ID = boarSkinID;
            NadeInfo[boarSkinID].Cmd = "boar";
            NadeInfo[boarSkinID].Enabled = config.animal.npcBoar;
            NadeInfo[boarSkinID].Perm = permBoar;
            NadeInfo[boarSkinID].Prefab = boarPrefab;

            NadeInfo.Add(stagSkinID, new GrenadeData());
            NadeInfo[stagSkinID].Name = "Stag Grenade";
            NadeInfo[stagSkinID].ID = stagSkinID;
            NadeInfo[stagSkinID].Cmd = "stag";
            NadeInfo[stagSkinID].Enabled = config.animal.npcStag;
            NadeInfo[stagSkinID].Perm = permStag;
            NadeInfo[stagSkinID].Prefab = stagPrefab;

            NadeInfo.Add(chickenSkinID, new GrenadeData());
            NadeInfo[chickenSkinID].Name = "Chicken Grenade";
            NadeInfo[chickenSkinID].ID = chickenSkinID;
            NadeInfo[chickenSkinID].Cmd = "chicken";
            NadeInfo[chickenSkinID].Enabled = config.animal.npcChicken;
            NadeInfo[chickenSkinID].Perm = permChicken;
            NadeInfo[chickenSkinID].Prefab = chickenPrefab;

            NadeInfo.Add(bradleySkinID, new GrenadeData());
            NadeInfo[bradleySkinID].Name = "Bradley Grenade";
            NadeInfo[bradleySkinID].ID = bradleySkinID;
            NadeInfo[bradleySkinID].Cmd = "bradley";
            NadeInfo[bradleySkinID].Enabled = config.bradley.npcBradley;
            NadeInfo[bradleySkinID].Perm = permBradley;
            NadeInfo[bradleySkinID].Prefab = bradleyPrefab;
        }

        #endregion

        #region Stored Data

        private StoredData storedData;

        private class StoredData
        {
            public Dictionary<string, NPCPlayerData> HumanNPC = new Dictionary<string, NPCPlayerData>();
            public Dictionary<string, AnimalData> AnimalNPC = new Dictionary<string, AnimalData>();
            public Dictionary<string, APCData> BradleyNPC = new Dictionary<string, APCData>();
        }

        private class NPCPlayerData
        {
            public string Name;
            public string Prefab;
            public float Health;
            public float MaxRoamRange;
            public float SenseRange;
            public float ListenRange;
            public float AggroRange;
            public float DeAggroRange;
            public float TargetLostRange;
            public float MemoryDuration;
            public float VisionCone;
            public bool CheckVisionCone;
            public bool CheckLOS;
            public bool IgnoreNonVisionSneakers;
            public float DamageScale;
            public bool PeaceKeeper;
            public bool IgnoreSafeZonePlayers;
            public bool RadioChatter;
            public bool DeathSound;
            public int NumberToSpawn;
            public int SpawnRadius;
            public float DespawnTime;
            public bool KillInSafeZone;
            public bool StripCorpseLoot;
            public List<string> KitList = new List<string>();
            public float Speed;
            public float Acceleration;
            public float FastSpeedFraction;
            public float NormalSpeedFraction;
            public float SlowSpeedFraction;
            public float SlowestSpeedFraction;
            public float LowHealthMaxSpeedFraction;
            public float TurnSpeed;
            public ulong GrenadeSkinID;
            public string Permission;
            public string ExplosionSound;
            public List<Loadout> DefaultLoadout = new List<Loadout>();
        }

        private class AnimalData
        {
            public string Name;
            public string Prefab;
            public float Health;
            public bool KillInSafeZone;
            public float DespawnTime;
            public int NumberToSpawn;
            public int SpawnRadius;
            public ulong GrenadeSkinID;
            public string Permission;
            public string ExplosionSound;
        }

        private class APCData
        {
            public string Name;
            public string Prefab;
            public float Health;
            public float ViewDistance;
            public float SearchRange;
            public float PatrolRange;
            public int PatrolPathNodes;
            public float ThrottleResponse;
            public int CratesToSpawn;
            public bool KillInSafeZone;
            public float DespawnTime;
            public int NumberToSpawn;
            public int SpawnRadius;
            public ulong GrenadeSkinID;
            public string Permission;
            public string ExplosionSound;
        }

        private class Loadout
        {
            public string Shortname;
            public ulong SkinID;
            public int Amount;
            [JsonProperty(PropertyName = "Container Type (Belt, Main, Wear)")]
            public string Container;
        }

        private void UpdateStoredData()
        {
            if (previousVersion < new VersionNumber(1, 2, 1))
            {
                Puts($"Updating new data values, all existing values will remain unchanged.");
                string key = "Scientist Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = ScientistLoadout();
                }
                key = "Heavy Scientist Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = HeavyLoadout();
                }
                key = "Juggernaut Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = JuggernautLoadout();
                }
                key = "Tunnel Dweller Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = TunnelLoadout();
                }
                key = "Underwater Dweller Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = UnderwaterLoadout();
                }
                key = "Murderer Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = MurdererLoadout();
                }
                key = "Scarecrow Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = ScarecrowLoadout();
                }
                key = "Mummy Grenade";
                if(storedData.HumanNPC.ContainsKey(key))
                {
                    storedData.HumanNPC[key].DefaultLoadout = MummyLoadout();
                }
                key = String.Empty;
            }
        }

        private void LoadDefaultData()
        {
            string key = "Scientist Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Scientist";
                storedData.HumanNPC[key].Prefab = scientistPrefab;
                storedData.HumanNPC[key].Health = 150f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 1f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = true;
                storedData.HumanNPC[key].DeathSound = true;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = scientistSkinID;
                storedData.HumanNPC[key].Permission = permScientist;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = ScientistLoadout();
            }
            key = "Heavy Scientist Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Heavy Scientist";
                storedData.HumanNPC[key].Prefab = heavyPrefab;
                storedData.HumanNPC[key].Health = 300f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 2f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = true;
                storedData.HumanNPC[key].DeathSound = true;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = heavySkinID;
                storedData.HumanNPC[key].Permission = permHeavy;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = HeavyLoadout();
            }
            key = "Juggernaut Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Juggernaut";
                storedData.HumanNPC[key].Prefab = heavyPrefab;
                storedData.HumanNPC[key].Health = 900f;
                storedData.HumanNPC[key].MaxRoamRange = 40f;
                storedData.HumanNPC[key].SenseRange = 60f;
                storedData.HumanNPC[key].ListenRange = 50f;
                storedData.HumanNPC[key].AggroRange = 50f;
                storedData.HumanNPC[key].DeAggroRange = 60f;
                storedData.HumanNPC[key].TargetLostRange = 70f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 180f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 3f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = true;
                storedData.HumanNPC[key].DeathSound = true;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = juggernautSkinID;
                storedData.HumanNPC[key].Permission = permJuggernaut;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = JuggernautLoadout();
            }
            key = "Tunnel Dweller Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Tunnel Dweller";
                storedData.HumanNPC[key].Prefab = scientistPrefab;
                storedData.HumanNPC[key].Health = 150f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 1f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = false;
                storedData.HumanNPC[key].DeathSound = false;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = tunnelSkinID;
                storedData.HumanNPC[key].Permission = permTunnel;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = TunnelLoadout();
            }
            key = "Underwater Dweller Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Underwater Dweller";
                storedData.HumanNPC[key].Prefab = scientistPrefab;
                storedData.HumanNPC[key].Health = 150f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 1f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = false;
                storedData.HumanNPC[key].DeathSound = false;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = underwaterSkinID;
                storedData.HumanNPC[key].Permission = permUnderwater;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = UnderwaterLoadout();
            }
            key = "Murderer Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Murderer";
                storedData.HumanNPC[key].Prefab = scarecrowPrefab;
                storedData.HumanNPC[key].Health = 200f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 1f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = true;
                storedData.HumanNPC[key].DeathSound = true;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = murdererSkinID;
                storedData.HumanNPC[key].Permission = permMurderer;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = MurdererLoadout();
            }
            key = "Scarecrow Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Scarecrow";
                storedData.HumanNPC[key].Prefab = scarecrowPrefab;
                storedData.HumanNPC[key].Health = 200f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 1f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = true;
                storedData.HumanNPC[key].DeathSound = true;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = scarecrowSkinID;
                storedData.HumanNPC[key].Permission = permScarecrow;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = ScarecrowLoadout();
            }
            key = "Mummy Grenade";
            if(!storedData.HumanNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.HumanNPC.Add(key, new NPCPlayerData());
                storedData.HumanNPC[key].Name = "Mummy";
                storedData.HumanNPC[key].Prefab = scarecrowPrefab;
                storedData.HumanNPC[key].Health = 200f;
                storedData.HumanNPC[key].MaxRoamRange = 30f;
                storedData.HumanNPC[key].SenseRange = 40f;
                storedData.HumanNPC[key].ListenRange = 30f;
                storedData.HumanNPC[key].AggroRange = 30f;
                storedData.HumanNPC[key].DeAggroRange = 40f;
                storedData.HumanNPC[key].TargetLostRange = 50f;
                storedData.HumanNPC[key].MemoryDuration = 10f;
                storedData.HumanNPC[key].VisionCone = 135f;
                storedData.HumanNPC[key].CheckVisionCone = true;
                storedData.HumanNPC[key].CheckLOS = true;
                storedData.HumanNPC[key].IgnoreNonVisionSneakers = true;
                storedData.HumanNPC[key].DamageScale = 1f;
                storedData.HumanNPC[key].PeaceKeeper = false;
                storedData.HumanNPC[key].IgnoreSafeZonePlayers = true;
                storedData.HumanNPC[key].RadioChatter = true;
                storedData.HumanNPC[key].DeathSound = true;
                storedData.HumanNPC[key].NumberToSpawn = 1;
                storedData.HumanNPC[key].SpawnRadius = 10;
                storedData.HumanNPC[key].DespawnTime = 300f;
                storedData.HumanNPC[key].KillInSafeZone = true;
                storedData.HumanNPC[key].StripCorpseLoot = false;
                storedData.HumanNPC[key].KitList = new List<string>();
                storedData.HumanNPC[key].Speed = 6.2f;
                storedData.HumanNPC[key].Acceleration = 12f;
                storedData.HumanNPC[key].FastSpeedFraction = 1f;
                storedData.HumanNPC[key].NormalSpeedFraction = 0.5f;
                storedData.HumanNPC[key].SlowSpeedFraction = 0.3f;
                storedData.HumanNPC[key].SlowestSpeedFraction = 0.1f;
                storedData.HumanNPC[key].LowHealthMaxSpeedFraction = 0.5f;
                storedData.HumanNPC[key].TurnSpeed = 120f;
                storedData.HumanNPC[key].GrenadeSkinID = mummySkinID;
                storedData.HumanNPC[key].Permission = permMummy;
                storedData.HumanNPC[key].ExplosionSound = explosionSound;
                storedData.HumanNPC[key].DefaultLoadout = MummyLoadout();
            }
            /////////////////////// BaseNPC ////////////////////////
            key = "Bear Grenade";
            if(!storedData.AnimalNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.AnimalNPC.Add(key, new AnimalData());
                storedData.AnimalNPC[key].Name = "Bear";
                storedData.AnimalNPC[key].Prefab = bearPrefab;
                storedData.AnimalNPC[key].Health = 400f;
                storedData.AnimalNPC[key].KillInSafeZone = true;
                storedData.AnimalNPC[key].DespawnTime = 300f;
                storedData.AnimalNPC[key].NumberToSpawn = 1;
                storedData.AnimalNPC[key].SpawnRadius = 10;
                storedData.AnimalNPC[key].GrenadeSkinID = bearSkinID;
                storedData.AnimalNPC[key].Permission = permBear;
                storedData.AnimalNPC[key].ExplosionSound = explosionSound;
            }
            key = "Polar Bear Grenade";
            if(!storedData.AnimalNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.AnimalNPC.Add(key, new AnimalData());
                storedData.AnimalNPC[key].Name = "Polar Bear";
                storedData.AnimalNPC[key].Prefab = polarbearPrefab;
                storedData.AnimalNPC[key].Health = 400f;
                storedData.AnimalNPC[key].KillInSafeZone = true;
                storedData.AnimalNPC[key].DespawnTime = 300f;
                storedData.AnimalNPC[key].NumberToSpawn = 1;
                storedData.AnimalNPC[key].SpawnRadius = 10;
                storedData.AnimalNPC[key].GrenadeSkinID = polarbearSkinID;
                storedData.AnimalNPC[key].Permission = permPolarbear;
                storedData.AnimalNPC[key].ExplosionSound = explosionSound;
            }
            key = "Wolf Grenade";
            if(!storedData.AnimalNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.AnimalNPC.Add(key, new AnimalData());
                storedData.AnimalNPC[key].Name = "Wolf";
                storedData.AnimalNPC[key].Prefab = wolfPrefab;
                storedData.AnimalNPC[key].Health = 150f;
                storedData.AnimalNPC[key].KillInSafeZone = true;
                storedData.AnimalNPC[key].DespawnTime = 300f;
                storedData.AnimalNPC[key].NumberToSpawn = 1;
                storedData.AnimalNPC[key].SpawnRadius = 10;
                storedData.AnimalNPC[key].GrenadeSkinID = wolfSkinID;
                storedData.AnimalNPC[key].Permission = permWolf;
                storedData.AnimalNPC[key].ExplosionSound = explosionSound;
            }
            key = "Boar Grenade";
            if(!storedData.AnimalNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.AnimalNPC.Add(key, new AnimalData());
                storedData.AnimalNPC[key].Name = "Boar";
                storedData.AnimalNPC[key].Prefab = boarPrefab;
                storedData.AnimalNPC[key].Health = 150f;
                storedData.AnimalNPC[key].KillInSafeZone = true;
                storedData.AnimalNPC[key].DespawnTime = 300f;
                storedData.AnimalNPC[key].NumberToSpawn = 1;
                storedData.AnimalNPC[key].SpawnRadius = 10;
                storedData.AnimalNPC[key].GrenadeSkinID = boarSkinID;
                storedData.AnimalNPC[key].Permission = permBoar;
                storedData.AnimalNPC[key].ExplosionSound = explosionSound;
            }
            key = "Stag Grenade";
            if(!storedData.AnimalNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.AnimalNPC.Add(key, new AnimalData());
                storedData.AnimalNPC[key].Name = "Stag";
                storedData.AnimalNPC[key].Prefab = stagPrefab;
                storedData.AnimalNPC[key].Health = 150f;
                storedData.AnimalNPC[key].KillInSafeZone = true;
                storedData.AnimalNPC[key].DespawnTime = 300f;
                storedData.AnimalNPC[key].NumberToSpawn = 1;
                storedData.AnimalNPC[key].SpawnRadius = 10;
                storedData.AnimalNPC[key].GrenadeSkinID = stagSkinID;
                storedData.AnimalNPC[key].Permission = permStag;
                storedData.AnimalNPC[key].ExplosionSound = explosionSound;
            }
            key = "Chicken Grenade";
            if(!storedData.AnimalNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.AnimalNPC.Add(key, new AnimalData());
                storedData.AnimalNPC[key].Name = "Chicken";
                storedData.AnimalNPC[key].Prefab = chickenPrefab;
                storedData.AnimalNPC[key].Health = 25f;
                storedData.AnimalNPC[key].KillInSafeZone = true;
                storedData.AnimalNPC[key].DespawnTime = 300f;
                storedData.AnimalNPC[key].NumberToSpawn = 1;
                storedData.AnimalNPC[key].SpawnRadius = 10;
                storedData.AnimalNPC[key].GrenadeSkinID = chickenSkinID;
                storedData.AnimalNPC[key].Permission = permChicken;
                storedData.AnimalNPC[key].ExplosionSound = explosionSound;
            }
            //////////////////////// BradleyAPC /////////////////////////
            key = "Bradley Grenade";
            if(!storedData.BradleyNPC.ContainsKey(key))
            {
                Puts($"Data contains no entries for {key}, populating default values.");
                storedData.BradleyNPC.Add(key, new APCData());
                storedData.BradleyNPC[key].Name = "Bradley APC";
                storedData.BradleyNPC[key].Prefab = bradleyPrefab;
                storedData.BradleyNPC[key].Health = 1000f;
                storedData.BradleyNPC[key].ViewDistance = 60f;
                storedData.BradleyNPC[key].SearchRange = 40f;
                storedData.BradleyNPC[key].PatrolRange = 20f;
                storedData.BradleyNPC[key].PatrolPathNodes = 6;
                storedData.BradleyNPC[key].ThrottleResponse = 1f;
                storedData.BradleyNPC[key].CratesToSpawn = 3;
                storedData.BradleyNPC[key].KillInSafeZone = true;
                storedData.BradleyNPC[key].DespawnTime = 300f;
                storedData.BradleyNPC[key].NumberToSpawn = 1;
                storedData.BradleyNPC[key].SpawnRadius = 20;
                storedData.BradleyNPC[key].GrenadeSkinID = bradleySkinID;
                storedData.BradleyNPC[key].Permission = permBradley;
                storedData.BradleyNPC[key].ExplosionSound = bradleyExplosion;
            }
            UpdateStoredData();
            SaveData();
        }

        #endregion

        #region Default Loadouts

        private static List<Loadout> ScientistLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "hazmatsuit_scientist", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "rifle.ak", SkinID = 0, Amount = 1, Container = "Belt" },
                new Loadout { Shortname = "ammo.rifle", SkinID = 0, Amount = 30, Container = "Main"}
            };
        }

        private static List<Loadout> HeavyLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "scientistsuit_heavy", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "lmg.m249", SkinID = 0, Amount = 1, Container = "Belt" },
                new Loadout { Shortname = "ammo.rifle", SkinID = 0, Amount = 50, Container = "Main"}
            };
        }

        private static List<Loadout> JuggernautLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "heavy.plate.helmet", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "heavy.plate.jacket", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "heavy.plate.pants", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "shoes.boots", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "lmg.m249", SkinID = 0, Amount = 1, Container = "Belt" },
                new Loadout { Shortname = "ammo.rifle", SkinID = 0, Amount = 100, Container = "Main"}
            };
        }

        private static List<Loadout> TunnelLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "hat.gas.mask", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "jumpsuit.suit", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "shoes.boots", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "pistol.m92", SkinID = 0, Amount = 1, Container = "Belt" },
                new Loadout { Shortname = "ammo.pistol", SkinID = 0, Amount = 20, Container = "Main"}
            };
        }

        private static List<Loadout> UnderwaterLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "hat.gas.mask", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "jumpsuit.suit.blue", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "shoes.boots", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "pistol.m92", SkinID = 0, Amount = 1, Container = "Belt" },
                new Loadout { Shortname = "ammo.pistol", SkinID = 0, Amount = 20, Container = "Main"}
            };
        }

        private static List<Loadout> MurdererLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "burlap.headwrap", SkinID = 807624505, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "gloweyes", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "tshirt", SkinID = 795997221, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "burlap.gloves", SkinID = 1132774091, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "burlap.trousers", SkinID = 806966575, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "shoes.boots", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "machete", SkinID = 0, Amount = 1, Container = "Belt" }
            };
        }

        private static List<Loadout> ScarecrowLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "scarecrow.suit", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "chainsaw", SkinID = 0, Amount = 1, Container = "Belt" },
                new Loadout { Shortname = "lowgradefuel", SkinID = 0, Amount = 30, Container = "Main"}
            };
        }

        private static List<Loadout> MummyLoadout()
        {
            return new List<Loadout>
            {
                new Loadout { Shortname = "halloween.mummysuit", SkinID = 0, Amount = 1, Container = "Wear" },
                new Loadout { Shortname = "sickle", SkinID = 0, Amount = 1, Container = "Belt" }
            };
        }

        #endregion
    }
}