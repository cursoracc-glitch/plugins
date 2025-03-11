using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Facepunch;
using Facepunch.Math;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using ProtoBuf;

/**********************************************************************
*
*   2.1.0   -   Npc displaynames will now be propperly used
*           -   Npc without names will use their translated prefab name or custom name
*           -   Fixed -1 notification when blown up
*           -   When blown up will show what explosive is used
*           -   Removed old Blownup sequence and using the regular kill messages,
*               since the weaponcheck check is updated with explosives
*           -   Added Animal kills to the feed (by example of crazyR17)
*   2.1.1   -   Removed obsolete Blown messages from language file
*           -   Reverted distance check
*           -   Hotfix
*   2.1.2   -   More checks in GetWeaponName
*           -   Added Bradley shell kills and feed
*           -   Added direct falling deaths to the feed
*           -   For animals : instead of entity prefab check on each death it just checks for BaseAnimalNPC
*               then does the check listed in cfg (this should improve performance when animal feed is used)
*           -   Added SimpleShark to the feed
*           -   Fixed occasional error on suicide
*           -   Possible fix for npc suicides
*           +   "MsgFeedKillFall", "<color=#ff686b>{0}</color> died by <color=orange>Fall</color>"
*           +   "MsgFeedKillBrad", "<color=#ff686b>{0}</color> was killed by a <color=orange>Shell</color>"
*   2.2.0   -   Disabled showing npc killing eachother
*           -   Fix for console spamm
*   2.2.1   -   Possible fix for unknown animal/npc kill trigger
*           -   Removed Catch
*   2.2.2   -   Added nullcheck if attacker is somehow null
*           -   Added a check for turrets killing npc
*   2.2.3   -   Fixed Npc kills showing when disabled in cfg
*           -   Added Damagetype events for Cold/Drowning/Fall/Hunger & Thirst/Electricshock
*           -   Added messages in language file for the new Damagetypes
*           -   Added kill triggers to the cfg (backup and delete the old cfg file before updating)
*   2.2.4   -   Added Killed by outpost checks
*           -   Returns when deaths are generic
*           -   Kills by entities should display better (was blocked in certain cases)
*           -   Trap triggers are moved earlyer inside the check queue
*           -   Killfeed wil now display traps and outpost sentry kills seperatly
*           -   Npc kills will display explosives again
*           -   Cactus deaths no longer throw errors and are displayed in the feed
*           -   new language file entries for MsgFeedKillSentry/MsgFeedKillCactus
*   2.2.5   -   Added API SendKillfeedmessage(string msg)
*               SimpleKillFeed?.Call("SendKillfeedmessage", "Test message for killfeed api");
*               Catching when target has invalid requirements
*               Removed Generic
*   2.2.6   -   Possible fix for double clan tag using Clans by Mevent
*           -   Seperated animal kils and death by animals (cfg)
*   2.2.7   -   Added MLRS
*               Added ZombieHorde npc support
*               Fix for animal deaths showing when disabled
*               Changed from Hud to Under as suggested
*               Modular Cars are now shown as weapon
*               Fixed kill Distance when using a car
*   2.2.8       Hotfix
*           
***********************************************************************/
namespace Oxide.Plugins
{
    [Info("Simple Kill Feed", "Krungh Crow", "2.2.8")]
    [Description("A kill feed, that displays various kill events in the top right corner.")]
    public class SimpleKillFeed : RustPlugin
    {
        #region Fields

        [PluginReference] private Plugin Clans;
        private bool _isClansReborn;
        private bool _isClansIo;
        private bool _isClansMevent;
        private readonly Dictionary<uint, string> _itemNameMapping = new Dictionary<uint, string>();
        private GameObject _holder;
        private KillQueue _killQueue;
        private static SKFConfig configdata;
        private static SimpleKillFeedData _data;

        #endregion

        #region Config

        private class SKFConfig
        {
            [JsonProperty("Show Traps and Entitys in Kill Feed")]
            public bool EnableEntityFeed;
            [JsonProperty("Show Animals kills (default true)")]
            public bool EnableAnimalFeed;
            [JsonProperty("Show Npcs kills (Default true)")]
            public bool EnableNpcFeed;
            [JsonProperty("Show suicides (Default: true)")]
            public bool EnableSuicides;
            [JsonProperty("Show Deaths by Animals (Default: true)")]
            public bool EnableAnimal;
            [JsonProperty("Show Deaths by Cold (Default: true)")]
            public bool EnableCold;
            [JsonProperty("Show deaths by Drowning (Default: true)")]
            public bool EnableDrowning;
            [JsonProperty("Show Deaths by Fall (Default: true)")]
            public bool EnableFall;
            [JsonProperty("Show Deaths by Hunger (Default: true)")]
            public bool EnableHunger;
            [JsonProperty("Show Deaths by Electricution (Default: true)")]
            public bool EnableElectricution;
            [JsonProperty("Show Deaths by Radiation (Default: true)")]
            public bool EnableRadiationKills;
            [JsonProperty("Chat Icon Id (Steam profile ID)")]
            public ulong IconId;
            [JsonProperty("Max messages in feed (Default: 5)")]
            public int MaxFeedMessages;
            [JsonProperty("Max player name length in feed (Default: 18)")]
            public int MaxPlayerNameLength;
            [JsonProperty("Feed message TTL in seconds (Default: 7)")]
            public int FeedMessageTtlSec;
            [JsonProperty("Allow kill messages in chat along with kill feed")]
            public bool EnableChatFeed;
            [JsonProperty("Log PvP Kill events")]
            public bool EnableLogging;
            [JsonProperty("Height ident (space between messages). Default: 0.0185")]
            public float HeightIdent;
            [JsonProperty("Feed Position - Anchor Max. (Default: 0.995 0.986")]
            public string AnchorMax;
            [JsonProperty("Feed Position - Anchor Min. (Default: 0.723 0.964")]
            public string AnchorMin;
            [JsonProperty("Font size of kill feed (Default: 12)")]
            public int FontSize;
            [JsonProperty("Outline Text Size (Default: 0.5 0.5)")]
            public string OutlineSize;
            [JsonProperty("Default color for distance (if too far from any from the list). Default: #FF8000")]
            public string DefaultDistanceColor;
            [JsonProperty("Distance Colors List (Certain color will apply if distance is <= than specified)")]
            public DistanceColor[] DistanceColors;
            [JsonProperty("Custom Entity Names, you can remove or add more!")]
            public Dictionary<string, string> Ents = new Dictionary<string, string>();
            [JsonProperty("Custom Animal Names, you can remove or add more!")]
            public Dictionary<string, string> Animal = new Dictionary<string, string>();
            [JsonProperty("Custom Weapon Names, you can add more!")]
            public Dictionary<string, string> Weapons = new Dictionary<string, string>();
            [JsonProperty("Custom Npc Names, you can add more!")]
            public Dictionary<string, string> Npcs = new Dictionary<string, string>();

            [OnDeserialized]
            internal void OnDeserialized(StreamingContext ctx) => Array.Sort(DistanceColors, (o1, o2) => o1.DistanceThreshold.CompareTo(o2.DistanceThreshold));

            public class DistanceColor
            {
                public int DistanceThreshold;
                public string Color;
                public bool TestDistance(int distance) => distance <= DistanceThreshold;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configdata = Config.ReadObject<SKFConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            configdata = new SKFConfig
            {
                EnableEntityFeed = true,
                EnableAnimalFeed = true,
                EnableNpcFeed = true,
                EnableFall = true ,
                EnableHunger = true ,
                EnableElectricution = true,
                EnableSuicides = true,
                EnableAnimal = true ,
                EnableCold = true ,
                EnableDrowning = true,
                EnableRadiationKills = true,
                IconId = 76561197960839785UL,
                MaxFeedMessages = 5,
                MaxPlayerNameLength = 18,
                FeedMessageTtlSec = 7,
                EnableChatFeed = true,
                EnableLogging = false,
                HeightIdent = 0.0185f,
                AnchorMax = "0.995 0.986",
                AnchorMin = "0.723 0.964",
                FontSize = 12,
                OutlineSize = "0.5 0.5",
                DefaultDistanceColor = "#FF8000",
                DistanceColors = new[]
                {
                    new SKFConfig.DistanceColor
                    {
                        Color = "#FFFFFF",
                        DistanceThreshold = 50
                    },
                    new SKFConfig.DistanceColor
                    {
                        Color = "#91D6FF",
                        DistanceThreshold = 100
                    },
                    new SKFConfig.DistanceColor
                    {
                        Color = "#FFFF00",
                        DistanceThreshold = 150
                    }
                },
                Ents = new Dictionary<string, string>()
                {
                    { "autoturret_deployed","Auto Turret" },
                    { "flameturret.deployed","Flame Turret"},
                    { "guntrap.deployed","Gun Trap"},
                    { "landmine","Landmine"},
                    { "beartrap","Bear Trap"},
                    { "sam_site_turret_deployed","Sam Site Turret"},
                    { "patrolhelicopter","Helicopter"},
                    { "bradleyapc","Bradley APC"}
                },
                Animal = new Dictionary<string, string>()
                {
                    { "bear","Bear" },
                    { "polarbear","PolarBear" },
                    { "wolf","Wolf" },
                    { "stag","Stag"},
                    { "boar","Boar" },
                    { "chicken","Chicken" },
                    { "horse","Horse"},
                    { "simpleshark","Shark" }
                },
                Weapons = new Dictionary<string, string>()
                {
                    { "Assault Rifle","Ak-47" },
                    { "LR-300 Assault Rifle","LR-300" },
                    { "L96 Rifle","L96" },
                    { "Bolt Action Rifle","Bolt" },
                    { "Semi-Automatic Rifle","Semi-AR" },
                    { "Semi-Automatic Pistol","Semi-AP" },
                    { "Spas-12 Shotgun","Spas-12" },
                    { "M92 Pistol","M92" }
                },
                Npcs = new Dictionary<string, string>()
                {
                    { "scientist","Scientist Npc" }
                }
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configdata);

        #endregion

        #region Data (ProtoBuf)

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        private class SimpleKillFeedData
        {
            public HashSet<ulong> DisabledUsers = new HashSet<ulong>();
        }

        private void LoadData()
        {
            _data = ProtoStorage.Load<SimpleKillFeedData>(nameof(SimpleKillFeed)) ?? new SimpleKillFeedData();
        }

        private void SaveData()
        {
            if (_data == null) return;
            ProtoStorage.Save(_data, nameof(SimpleKillFeed));
        }

        #endregion

        #region ChatCommand

        [ChatCommand("feed")]
        private void ToggleFeed(BasePlayer player)
        {
            if (!_data.DisabledUsers.Contains(player.userID))
            {
                _data.DisabledUsers.Add(player.userID);
                Player.Message(player, _("Disabled", player), null, configdata.IconId);
            }
            else
            {
                _data.DisabledUsers.Remove(player.userID);
                Player.Message(player, _("Enabled", player), null, configdata.IconId);
            }
        }

        #endregion

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            _isClansReborn = Clans != null && Clans.Author.Contains("k1lly0u");
            _isClansIo = Clans != null && Clans.Author.Contains("playrust.io / dcode");
            _isClansMevent = Clans != null && Clans.Author.Contains("mevent");
            foreach (var blueprint in ItemManager.bpList.Where(bp => bp.targetItem.category == ItemCategory.Weapon || bp.targetItem.category == ItemCategory.Tool))
            {
                var md = blueprint.targetItem.GetComponent<ItemModEntity>();
                if (!md)
                    continue;
                if (!_itemNameMapping.ContainsKey(md.entityPrefab.resourceID))
                    _itemNameMapping.Add(md.entityPrefab.resourceID, blueprint.targetItem.displayName.english);
            }
            _holder = new GameObject("SKFHolder");
            UnityEngine.Object.DontDestroyOnLoad(_holder);
            _killQueue = _holder.AddComponent<KillQueue>();
            Pool.ResizeBuffer<KillEvent>(configdata.MaxFeedMessages);
        }

        private void Init() => LoadData();

        private void Unload()
        {
            _killQueue = null;
            UnityEngine.Object.Destroy(_holder);
            _holder = null;
            for (var i = 0; i < configdata.MaxFeedMessages; i++)
                KillQueue.RemoveKillCui($"kf-{i}");
            configdata = null;
            Pool.IPoolCollection value;
            Pool.Directory.TryRemove(typeof(KillEvent), out value);
            SaveData();
            _data = null;
        }

        #endregion

        #region Oxide Hooks (Deaths)

        private void OnEntityDeath(BaseAnimalNPC victim, HitInfo hitInfo)
        {
            if (victim == null || victim is BasePlayer || !IsAnimal(victim)) return;
            BasePlayer attacker = hitInfo.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc || IsAnimal(attacker) || IsZombieHorde(attacker)) return;
            if (!attacker.userID.IsSteamId()) return;
            if (!configdata.EnableAnimalFeed) return;

            string VictimName = configdata.Animal[victim.ShortPrefabName];
            string AttackerName = SanitizeName(GetClan(attacker) + attacker.displayName);
            string WeaponName = GetCustomWeaponName(hitInfo);
            var distance = (int)Vector3.Distance(attacker.transform.position, victim.transform.position);
            _killQueue.OnDeath(attacker, null, string.Format(_("MsgFeedKillAnimalFromPlayer"), AttackerName, VictimName, WeaponName, GetDistanceColor(distance), distance));
        }

        private void OnEntityDeath(SimpleShark victim, HitInfo hitInfo)
        {
            if (victim == null || victim is BasePlayer || !IsAnimal(victim)) return;
            BasePlayer attacker = hitInfo.InitiatorPlayer;

            if (attacker == null || attacker.IsNpc || IsAnimal(attacker) || IsZombieHorde(attacker)) return;
            if (!configdata.EnableAnimalFeed) return;

            string VictimName = configdata.Animal[victim.ShortPrefabName];
            string AttackerName = SanitizeName(GetClan(attacker) + attacker.displayName);
            string WeaponName = GetCustomWeaponName(hitInfo);
            var distance = (int)Vector3.Distance(attacker.transform.position, victim.transform.position);
            _killQueue.OnDeath(attacker, null, string.Format(_("MsgFeedKillAnimalFromPlayer"), AttackerName, VictimName, WeaponName, GetDistanceColor(distance), distance));

        }

        private void OnPlayerDeath(BasePlayer victim, HitInfo hitInfo)
        {
            try
            {
                if (victim == null) return;

                var wAttacker = victim.lastAttacker?.ToPlayer();
                if (!victim.userID.IsSteamId() && !configdata.EnableNpcFeed)
                {
                    return;
                }
                if (hitInfo.Initiator is BaseAnimalNPC && victim.IsWounded())
                {
                    if (!configdata.EnableAnimal) return;
                    OnKilledByAnimal(hitInfo.Initiator , victim); return;
                }

                if (IsTrap(hitInfo.Initiator))
                {
                    if (!configdata.EnableEntityFeed) return;
                    OnKilledByEnt(hitInfo.Initiator , victim); return;
                }
                if (victim.lastAttacker is NPCAutoTurret)
                {
                    if (!configdata.EnableEntityFeed) return;
                    OnSentry(victim);
                    return;
                }

                if (victim.lastAttacker.ToString().Contains("cactus"))
                {
                    if (!victim.userID.IsSteamId() && !configdata.EnableNpcFeed) return;
                    OnCactus(victim); return;
                }

                if (!wAttacker.userID.IsSteamId() && !victim.userID.IsSteamId())
                {
                    //Puts($"Npc kill : {wAttacker} Killed {victim}");
                    return;
                }
                if ((wAttacker.userID.IsSteamId() && !victim.userID.IsSteamId()) && !configdata.EnableNpcFeed)
                {
                    return;
                }

                if (hitInfo == null && (wAttacker != null && victim.IsWounded()))
                {
                    OnDeathFromWounds(wAttacker , victim);
                    return;
                }

                if (victim.lastAttacker?.ToPlayer() == victim)
                {
                    if (!configdata.EnableSuicides) return;
                    OnSuicide(victim); return;
                    return;
                }

                if (!victim.userID.IsSteamId() && hitInfo.Initiator is HotAirBalloon) return;

                if (hitInfo.WeaponPrefab.prefabID == 3032863244)//Cannon Shell prefab id
                {
                    if (!configdata.EnableEntityFeed) return;
                    OnKilledByBradley(victim); return;
                }

                if (IsRadiation(hitInfo))
                {
                    if (!configdata.EnableRadiationKills) return;
                    OnKilledByRadiation(victim); return;
                }

                if (IsFall(hitInfo) && victim.IsWounded())
                {
                    if (!configdata.EnableFall) return;
                    DeathByFall(victim); return;
                }

                if (IsCold(hitInfo))
                {
                    if (!configdata.EnableCold) return;
                    OnFrozen(victim); return;
                }

                if (IsDrowning(hitInfo))
                {
                    if (!configdata.EnableDrowning) return;
                    OnDrowning(victim); return;
                }

                if (IsHunger(hitInfo) || IsThirst(hitInfo) || IsPoison(hitInfo))
                {
                    if (!configdata.EnableHunger) return;
                    OnHunger(victim); return;
                }

                if (IsShock(hitInfo))
                {
                    if (!configdata.EnableElectricution) return;
                    OnShock(victim); return;
                    return;
                }
                var distance = !hitInfo.IsProjectile() ? (int)Vector3.Distance(hitInfo.PointStart , hitInfo.HitPositionWorld) : (int)hitInfo.ProjectileDistance;
                if (IsCar(hitInfo)) distance = 0;
                var attacker = hitInfo.InitiatorPlayer;

                if (attacker == null) return;

                else if (IsFlame(hitInfo))
                {
                    OnBurned(attacker , victim);
                }
                //if (victim.IsNpc && IsExplosion(hitInfo)) return;//BotRespawn npc's can explode throwing errors
                else if (!IsDrowning(hitInfo))
                {
                    OnKilled(attacker , victim , hitInfo , distance);
                    return;
                }
                return;
            }
            catch
            {
                Puts("invalid target");
                return;
            }
        }

        #endregion

        #region LanguageAPI

        protected override void LoadDefaultMessages() => lang.RegisterMessages(new Dictionary<string, string>
        {
            {"MsgAttacker", "You killed <color=#ff686b>{0}</color> from {1}m in <color=#ff686b>{2}</color>."},
            {"MsgVictim", "<color=#ff686b>{0}</color> killed you from {1}m with their {2} to <color=#ff686b>{3}</color>."},
            {"MsgFeedKill", "<color=#00ff00>{0}</color> killed <color=#ff686b>{1}</color>, <color=#ff686b>{2}</color>, <color=#ff686b>{3}</color><color={4}>({5}m)</color>"},
            {"MsgFeedKillNpc", "<color=#00ff00>{0}</color> killed <color=#ff686b>{1}</color>, <color={2}>({3}m)</color>"},
            {"MsgFeedKillAnimalFromPlayer", "<color=#00ff00>{0}</color> killed a <color=#ff686b>{1}</color>, <color=#ff686b>{2}</color>, <color={3}>({4}m)</color>"},

            {"MsgAtkWounded", "You wounded <color=#ff686b>{0}</color> till death."},
            {"MsgVictimWounded", "<color=#ff686b>{0}</color> has wounded you till death."},
            {"MsgFeedWounded", "<color=#00ff00>{0}</color> finished <color=#ff686b>{1}</color>"},

            {"MsgAtkBurned", "You burned <color=#ff686b>{0}</color> alive!"},
            {"MsgVictimBurned", "<color=#ff686b>{0}</color> has burned you alive!"},
            {"MsgFeedBurned", "<color=#00ff00>{0}</color> burned <color=#ff686b>{1}</color>!"},

            {"MsgFeedKillBrad", "<color=#ff686b>{0}</color> was killed by a <color=orange>Shell</color>"},
            {"MsgFeedKillEnt", "<color=#ff686b>{0}</color> was killed by <color=orange>{1}</color>"},
            {"MsgFeedKillAnimal", "<color=#ff686b>{0}</color> was killed by <color=orange>{1}</color>"},

            {"MsgFeedKillSuicide", "<color=#ff686b>{0}</color> committed <color=orange>Suicide</color>"},
            {"MsgFeedKillRadiation", "<color=#ff686b>{0}</color> died to <color=orange>Radiation</color>"},
            {"MsgFeedKillFall", "<color=#ff686b>{0}</color> died by <color=orange>Fall</color>"},
            {"MsgFeedKillCold", "<color=#ff686b>{0}</color> died by <color=orange>Cold</color>"},
            {"MsgFeedKillDrowned", "<color=#ff686b>{0}</color> <color=orange>Drowned</color>"},
            {"MsgFeedKillHunger", "<color=#ff686b>{0}</color> <color=orange>Starved</color>"},
            {"MsgFeedKillShock", "<color=#ff686b>{0}</color> got <color=orange>Electrocuted</color>"},
            {"MsgFeedKillSentry", "<color=#ff686b>{0}</color> got killed by <color=orange>Outpost</color>"},
            {"MsgFeedKillCactus", "<color=#ff686b>{0}</color> died to a <color=orange>Cactus</color>"},

            {"Enabled", "KillFeed Enabled"},
            {"Disabled", "KillFeed Disabled"}
        }, this);

        #endregion

        #region API for other plugins to use the feed

        private void SendKillfeedmessage(string msg)
        {
            _killQueue.OnDeath(null , null , string.Format(_(msg)));
        }

        #endregion

        #region Kill Events

        private void OnKilled(BasePlayer attacker, BasePlayer victim, HitInfo hitInfo, int dist)
        {
            var HitBone = hitInfo.boneArea.ToString();
            if (HitBone == "-1") HitBone = "Body";
            if (IsCar(hitInfo)) dist = 0;

            if ((IsZombieHorde(attacker) || attacker.IsNpc) && configdata.EnableNpcFeed)
            {
                var npc = attacker;
                _killQueue.OnDeath(victim, null, string.Format(_("MsgFeedKillNpc"), CustomNpcName(npc), SanitizeName(GetClan(victim) + victim.displayName), GetDistanceColor(dist), dist));
                return;
            }

            if ((victim.IsNpc || IsZombieHorde(victim)) && configdata.EnableNpcFeed)
            {
                var npc = victim;
                _killQueue.OnDeath(attacker, null, string.Format(_("MsgFeedKill"), SanitizeName(GetClan(attacker) + attacker.displayName), CustomNpcName(npc), GetCustomWeaponName(hitInfo), HitBone, GetDistanceColor(dist), dist));
                return;
            }

            if (configdata.EnableChatFeed)
            {
                if (!_data.DisabledUsers.Contains(attacker.userID))
                    Player.Message(attacker, _("MsgAttacker", attacker), null, configdata.IconId, GetClan(victim) + victim.displayName, dist, HitBone);
                if (!_data.DisabledUsers.Contains(victim.userID))
                    Player.Message(victim, _("MsgVictim", victim), null, configdata.IconId, GetClan(attacker) + attacker.displayName, dist, GetCustomWeaponName(hitInfo), HitBone);
            }
            _killQueue.OnDeath(victim, attacker, string.Format(_("MsgFeedKill"), SanitizeName(GetClan(attacker) + attacker.displayName), SanitizeName(GetClan(victim) + victim.displayName), GetCustomWeaponName(hitInfo), HitBone, GetDistanceColor(dist), dist));

            if (!configdata.EnableLogging) return;
            var sfkLog = new StringBuilder($"{DateTime.Now}: ({attacker.UserIDString}){attacker.displayName} killed ({victim.UserIDString}){victim.displayName} from {dist}m in {HitBone}");
            LogToFile("SimpleKillFeed", sfkLog.ToString(), this);
        }

        private void OnSuicide(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillSuicide") , SanitizeName(GetClan(victim) + victim.displayName)));

        private void OnKilledByRadiation(BasePlayer victim) => _killQueue.OnDeath(victim, null, string.Format(_("MsgFeedKillRadiation"), SanitizeName(GetClan(victim) + victim.displayName)));

        private void OnKilledByEnt(BaseEntity attacker , BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillEnt") , SanitizeName(GetClan(victim) + victim.displayName) , CustomEntName(attacker)));

        private void OnKilledByBradley(BasePlayer victim) => _killQueue.OnDeath(victim, null, string.Format(_("MsgFeedKillBrad"), SanitizeName(GetClan(victim) + victim.displayName)));

        private void OnKilledByAnimal(BaseEntity attacker, BasePlayer victim) => _killQueue.OnDeath(victim, null, string.Format(_("MsgFeedKillAnimal"), SanitizeName(GetClan(victim) + victim.displayName), CustomAnimalName(attacker)));

        private void DeathByFall(BasePlayer victim) => _killQueue.OnDeath(victim, null, string.Format(_("MsgFeedKillFall"), SanitizeName(GetClan(victim) + victim.displayName)));

        private void OnDeathFromWounds(BasePlayer attacker, BasePlayer victim)
        {
            if (configdata.EnableChatFeed)
            {
                if (!_data.DisabledUsers.Contains(attacker.userID) && attacker.userID.IsSteamId())
                    Player.Message(attacker, _("MsgAtkWounded", attacker), null, configdata.IconId, GetClan(victim) + victim.displayName);
                if (!_data.DisabledUsers.Contains(victim.userID))
                    Player.Message(victim, _("MsgVictimWounded", victim), null, configdata.IconId, GetClan(attacker) + attacker.displayName);
            }
            _killQueue.OnDeath(victim, attacker, string.Format(_("MsgFeedWounded"), SanitizeName(GetClan(attacker) + attacker.displayName), SanitizeName(GetClan(victim) + victim.displayName)));

            if (!configdata.EnableLogging) return;
            var sfkLog = new StringBuilder($"{DateTime.Now}: ({attacker.UserIDString}){attacker.displayName} finished ({victim.UserIDString}){victim.displayName}");
            LogToFile("SimpleKillFeed", sfkLog.ToString(), this);
        }

        private void OnBurned(BasePlayer attacker, BasePlayer victim)
        {
            if (victim.IsNpc)
            {
                if (!configdata.EnableNpcFeed) return;
                var npc = victim;
                _killQueue.OnDeath(victim, attacker, string.Format(_("MsgFeedBurned"), SanitizeName(GetClan(attacker) + attacker.displayName), CustomNpcName(npc)));
                return;
            }

            if (attacker.IsNpc)
            {
                if (!configdata.EnableNpcFeed) return;
                var npc = attacker;
                _killQueue.OnDeath(victim, attacker, string.Format(_("MsgFeedBurned"), CustomNpcName(npc), SanitizeName(GetClan(victim) + victim.displayName)));
                return;
            }
            if (configdata.EnableChatFeed)
            {
                if (!_data.DisabledUsers.Contains(attacker.userID))
                    Player.Message(attacker, _("MsgAtkBurned", attacker), null, configdata.IconId, GetClan(victim) + victim.displayName);
                if (!_data.DisabledUsers.Contains(victim.userID))
                    Player.Message(victim, _("MsgVictimBurned", victim), null, configdata.IconId, GetClan(attacker) + attacker.displayName);
            }
            _killQueue.OnDeath(victim, attacker, string.Format(_("MsgFeedBurned"), SanitizeName(GetClan(attacker) + attacker.displayName), SanitizeName(GetClan(victim) + victim.displayName)));

            if (!configdata.EnableLogging) return;
            var sfkLog = new StringBuilder($"{DateTime.Now}: ({attacker.UserIDString}){attacker.displayName} burned ({victim.UserIDString}){victim.displayName}");
            LogToFile("SimpleKillFeed", sfkLog.ToString(), this);
        }

        private void OnDrowning(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillDrowned") , SanitizeName(GetClan(victim) + victim.displayName)));
        private void OnFrozen(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillCold") , SanitizeName(GetClan(victim) + victim.displayName)));
        private void OnHunger(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillHunger") , SanitizeName(GetClan(victim) + victim.displayName)));
        private void OnShock(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillShock") , SanitizeName(GetClan(victim) + victim.displayName)));
        private void OnSentry(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillSentry") , SanitizeName(GetClan(victim) + victim.displayName)));
        private void OnCactus(BasePlayer victim) => _killQueue.OnDeath(victim , null , string.Format(_("MsgFeedKillCactus") , SanitizeName(GetClan(victim) + victim.displayName)));

        #endregion

        #region UI

        private class KillEvent : Pool.IPooled
        {
            public int DisplayUntil;
            public string Text;

            public KillEvent Init(string text, int displayUntil)
            {
                Text = text;
                DisplayUntil = displayUntil;
                return this;
            }

            public void EnterPool()
            {
                Text = null;
                DisplayUntil = 0;
            }

            public void LeavePool() { }
        }

        private class KillQueue : MonoBehaviour
        {
            private readonly WaitForSeconds _secondDelay = new WaitForSeconds(1f);
            private readonly Queue<KillEvent> _queue = new Queue<KillEvent>(configdata.MaxFeedMessages);
            private readonly CuiOutlineComponent _outlineStatic = new CuiOutlineComponent { Distance = configdata.OutlineSize, Color = "0 0 0 1" };
            private readonly CuiRectTransformComponent[] _rectTransformStatic = new CuiRectTransformComponent[configdata.MaxFeedMessages];
            private readonly CuiTextComponent[] _textStatic = new CuiTextComponent[configdata.MaxFeedMessages];
            private readonly CuiElementContainer _cui = new CuiElementContainer();
            private bool _needsRedraw;
            private int _currentlyDrawn;

            public void OnDeath(BasePlayer victim, BasePlayer attacker, string text)
            {
                if (_queue.Count == configdata.MaxFeedMessages)
                    DequeueEvent(_queue.Dequeue());
                PushEvent(Pool.Get<KillEvent>().Init(text, Epoch.Current + configdata.FeedMessageTtlSec));
            }

            private void PushEvent(KillEvent evt)
            {
                _queue.Enqueue(evt);
                _needsRedraw = true;
                DoProccessQueue();
            }

            private void Start()
            {
                for (var i = 0; i < configdata.MaxFeedMessages; i++)
                {
                    _rectTransformStatic[i] = new CuiRectTransformComponent
                    {
                        AnchorMax =
                            $"{configdata.AnchorMax.Split(Convert.ToChar(' '))[0]} {float.Parse(configdata.AnchorMax.Split(Convert.ToChar(' '))[1]) - (configdata.HeightIdent * i)}",
                        AnchorMin =
                            $"{configdata.AnchorMin.Split(Convert.ToChar(' '))[0]} {float.Parse(configdata.AnchorMin.Split(Convert.ToChar(' '))[1]) - (configdata.HeightIdent * i)}"
                    };
                    _textStatic[i] = new CuiTextComponent { Align = TextAnchor.MiddleRight, FontSize = configdata.FontSize, Text = string.Empty };
                }
                StartCoroutine(ProccessQueue());
            }

            private void DequeueEvent(KillEvent evt)
            {
                Pool.Free(ref evt);
                _needsRedraw = true;
            }

            private void DoProccessQueue()
            {
                while (_queue.Count > 0 && _queue.Peek().DisplayUntil < Epoch.Current)
                    DequeueEvent(_queue.Dequeue());

                if (!_needsRedraw)
                    return;
                var toBeRemoved = _currentlyDrawn;
                _currentlyDrawn = 0;
                foreach (var killEvent in _queue)
                {
                    var cuiText = _textStatic[_currentlyDrawn];
                    cuiText.Text = killEvent.Text;
                    _cui.Add(new CuiElement
                    {
                        Name = $"kf-{_currentlyDrawn}",
                        Parent = "Under",
                        Components =
                        {
                            cuiText,
                            _rectTransformStatic[_currentlyDrawn],
                            _outlineStatic
                        }
                    });
                    if (++_currentlyDrawn == configdata.MaxFeedMessages)
                        break;
                }
                _needsRedraw = false;
                SendKillCui(_cui, toBeRemoved);
                _cui.Clear();
            }

            private IEnumerator ProccessQueue()
            {
                while (!Interface.Oxide.IsShuttingDown)
                {
                    DoProccessQueue();
                    yield return _secondDelay;
                }
            }

            private static void SendKillCui(CuiElementContainer cui, int toBeRemoved)
            {
                var json = cui.ToJson();
                foreach (var plr in BasePlayer.activePlayerList)
                {
                    for (var i = toBeRemoved; i > 0; i--)
                        CuiHelper.DestroyUi(plr, $"kf-{i - 1}");
                    if (!_data.DisabledUsers.Contains(plr.userID))
                        CuiHelper.AddUi(plr, json);
                }
            }

            public static void RemoveKillCui(string name)
            {
                foreach (var plr in BasePlayer.activePlayerList) 
                    CuiHelper.DestroyUi(plr, name);
            }
        }

        #endregion

        #region Helpers

        private string _(string msg, BasePlayer player = null) => lang.GetMessage(msg, this, player?.UserIDString);

        private string GetCustomWeaponName(HitInfo hitInfo)
        {
            var name = GetWeaponName(hitInfo);
            if (string.IsNullOrEmpty(name))
                return null;

            string translatedName;
            if (configdata.Weapons.TryGetValue(name, out translatedName))
                return translatedName;

            configdata.Weapons.Add(name, name);
            Config.WriteObject(configdata);
            return name;
        }

        private string CustomNpcName(BasePlayer npc)
        {
            var name = npc.ShortPrefabName;
            if (string.IsNullOrEmpty(name))
                return null;
            if (npc.displayName != npc.userID.ToString())
                return npc.displayName;
            string translatedName;
            if (configdata.Npcs.TryGetValue(name, out translatedName))
                return translatedName;

            configdata.Npcs.Add(name, name);
            Config.WriteObject(configdata);
            return name;
        }

        private string CustomEntName(BaseEntity attacker)
        {
            var name = attacker.ShortPrefabName;
            if (string.IsNullOrEmpty(name))
                return null;
            string translatedName;
            if (configdata.Ents.TryGetValue(name, out translatedName))
                return translatedName;

            configdata.Npcs.Add(name, name);
            Config.WriteObject(configdata);
            return name;
        }

        private string CustomAnimalName(BaseEntity attacker)
        {
            var name = attacker.ShortPrefabName;
            if (string.IsNullOrEmpty(name))
                return null;
            string translatedName;
            if (configdata.Animal.TryGetValue(name, out translatedName))
                return translatedName;

            configdata.Npcs.Add(name, name);
            Config.WriteObject(configdata);
            return name;
        }

        private string GetWeaponName(HitInfo hitInfo)
        {
            var _WeaponName = "??Unknown??";
            if (hitInfo.Weapon == null)
            {
                if (hitInfo.WeaponPrefab.prefabID == 3898309212) _WeaponName = "C4";//3898309212
                if (hitInfo.WeaponPrefab.prefabID == 3046924118) _WeaponName = "Rocket";//3046924118
                if (hitInfo.WeaponPrefab.prefabID == 1217937936) _WeaponName = "HV Rocket";//1217937936
                if (hitInfo.WeaponPrefab.prefabID == 2742759844) _WeaponName = "Satchel";//1217937936
                if (hitInfo.WeaponPrefab.prefabID == 2144399804) _WeaponName = "Beancan";//2144399804
                if (hitInfo.WeaponPrefab.prefabID == 1859672190) _WeaponName = "Shell";//1217937936
                if (hitInfo.WeaponPrefab.prefabID == 1128089209) _WeaponName = "Grenade";//1128089209
                if (hitInfo.WeaponPrefab.prefabID == 3717106868) _WeaponName = "Flamethrower";//3717106868
                if (hitInfo.WeaponPrefab.prefabID == 3032863244) _WeaponName = "Cannon Shell";//3032863244

                if (hitInfo.Initiator is GunTrap) _WeaponName = "GunTrap";
                if (hitInfo.Initiator is FlameTurret) _WeaponName = "FlameTurret";
                if (hitInfo.Initiator is AutoTurret) _WeaponName = "AutoTurret";
                else if (hitInfo.damageTypes.GetMajorityDamageType() == DamageType.Heat) _WeaponName = "Fire";
                else if (hitInfo.damageTypes.GetMajorityDamageType() == DamageType.Fun_Water) _WeaponName = "WaterGun";
                if (hitInfo.WeaponPrefab.ToString().Contains("car_")) _WeaponName = "Car";
                //Puts(hitInfo.WeaponPrefab.prefabID.ToString());
                return _WeaponName;
            }

            if (hitInfo.Weapon != null)
            {
                var _Weapon = hitInfo.Weapon;
                var item = _Weapon.GetItem();
                if (item != null)
                    _WeaponName = item.info.displayName.english;
                if (hitInfo.WeaponPrefab.prefabID == 1233562048) _WeaponName = "Grenade Launcher";
            }
            if (hitInfo.Initiator is GunTrap) _WeaponName = "GunTrap";
            if (hitInfo.Initiator is FlameTurret) _WeaponName = "FlameTurret";
            if (hitInfo.Initiator is AutoTurret) _WeaponName = "AutoTurret";
            if (hitInfo.Initiator is HotAirBalloon) _WeaponName = "Hot Air Balloon";
            if (hitInfo.WeaponPrefab is MLRSRocket) _WeaponName = "MLRS";
            return _WeaponName;
        }

        private static bool IsExplosion(HitInfo hit) => (hit.WeaponPrefab != null && (hit.WeaponPrefab.ShortPrefabName.Contains("grenade") || hit.WeaponPrefab.ShortPrefabName.Contains("explosive")))
                                                        || hit.damageTypes.GetMajorityDamageType() == DamageType.Explosion || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Explosion));

        private static bool IsFlame(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Heat || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Heat));

        private static bool IsRadiation(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Radiation || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Radiation));

        private static bool IsCold(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Cold || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Cold));
        private static bool IsHunger(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Hunger || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Hunger));
        private static bool IsThirst(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Thirst || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Thirst));
        private static bool IsDrowning(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Drowned /* || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Drowned))*/;
        private static bool IsPoison(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Poison || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Poison));
        private static bool IsShock(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.ElectricShock || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.ElectricShock));
        private static bool IsFall(HitInfo hit) => hit.damageTypes.GetMajorityDamageType() == DamageType.Fall || (!hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Fall));
        private static bool IsCar(HitInfo hit) => hit.WeaponPrefab.ToString().Contains("car_");

        private static bool IsTrap(BaseEntity ent) => ent != null && configdata.Ents.ContainsKey(ent.ShortPrefabName);

        private static bool IsAnimal(BaseEntity animal) => animal?.ShortPrefabName != null && configdata.Animal.ContainsKey(animal.ShortPrefabName);

        private static bool IsZombieHorde(BasePlayer player) => player.GetType().Name.Equals("ZombieNPC");

        private static string GetDistanceColor(int dist)
        {
            foreach (var distanceColor in configdata.DistanceColors)
            {
                if (distanceColor.TestDistance(dist))
                    return distanceColor.Color;
            }
            return configdata.DefaultDistanceColor ?? "white";
        }

        private string GetClan(BasePlayer player)
        {
            if (_isClansReborn || _isClansIo ||_isClansMevent || Clans == null) return null;
            var clan = (string)Clans.Call("GetClanOf", player.UserIDString);
            if (clan == null) return null;
            var format = string.Format("[" + clan + "] ");
            return format;
        }

        private static string SanitizeName(string name)
        {
            if (name.Length > configdata.MaxPlayerNameLength)
                name = name.Substring(0, configdata.MaxPlayerNameLength).Trim();
            return name.Replace("\"", "''");
        }
        #endregion
    }
}
