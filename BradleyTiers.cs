using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

#region Changelogs and ToDo
/**********************************************************************
* 
* 1.0.0 :       -   Initial release
* 1.0.1 :       -   Added support for GUIAnnouncements
*               -   Added Estemated GridLocations to messages
*               -   Made messages more dynamic
*               -   Vanilla Bradleys will now spawn on random (true false setting)
* 1.0.2 :       -   Added dmg and accuracy
*               -   Added support for Economics and ServerRewards
* 1.0.3 :       -   Added Lootprofiles per tier
* 1.0.4 :       -   Added chatmessage on 1st hit to show tier (2 minutes between messages)
*               -   Added Chat/announcement options to cfg (true false)
*               -   Fixed a typo in the language section
* 1.0.5         -   Fix for NRE when bradley inflicts selfdamage
* 1.0.6         -   crates Should now be removing the vanilla loot better before adding custom profile loot
*                   Vanilla Bradley loot and crates will now remain vanilla
*                   Option to custom rename the tiers
* 1.0.7         -   Possible fix for top 2 tiers having wrong HP on occasion
* 1.0.8         -   Patch for nov 4 rust update
* 1.0.9         -   Extra nullcheck (OnEntityTakeDamage)
* 1.1.0         -   Improved GridLocation call
*               -   Added Attackmessage to lang file
*               -   Added support for Notify and UINotify
*               -   Cleaned up and refractured the Bradley spawning modifiers
*               -   Cleaned up and refractured the Bradley Spawning messagesystem
*               -   Cleaned up and refractured the Bradley Kills messagesystem
*               -   Optimised colorscheme
* 1.1.1         -   Added KillNotification that was previously hardcoded to lang file
*               -   Fixed Notifcations when it is a Convoy Bradley
*               -   Fixed Vanilla Bradley colors in messages
* 1.1.2         -   Added SpawnMessage to Lang file
*               -   Single message line is now used from Lang file on bradley spawns
*               -   Pre patched for Armored Train plugin update
* 1.1.3         -   Fixed ServerRewards not handing out rewards
*               -   Fixed the console notification for Convoy check
*               -   fixed Exploit upgrading leftover crates from previous bradley
* 1.1.4         -   Delayed checks for ArmoredTrain apc spawns
*               -   Possible fix for messages forcing themselves when not a tiered apc
*               -   Fixed Targeting ArmoredTrain apc
* 1.2.0         -   Added bulletdamage
*               -   Added probability to the lootsystem
*               -   Added custom item names to the loot system
*               -   Added Add Custom Loot (true/false) for each profile
*               -   Added implementation for SatDishEvent BradleyAPC
*               -   Added better implementation for ArmoredTrain BradleyAPC being updated
*               -   Added better implementation for Convoy BradleyAPC being updated
*               -   Added ignore triggers when included APC is set to false
*               -   Added include ArmoredTrain APC to main cfg (true/false)
*               -   Added include Convoy APC to main cfg (true/false)
*               -   Added include SatDishEvent APC to main cfg (true/false)
*               -   Added Bradley can interact with npc to main cfg (true/false)
*               -   Added Bradley can Target npc when hostile towards Bradley
*               -   Fix for Rewards not being handed out
*   1.2.1       -   Patched for ArmoredTrain
*
*   1.2.2       -   Added OnBradleyTiersVanillaSpawned(BradleyAPC bradley) Hook
*               -   Added OnBradleyTiersEasySpawned(BradleyAPC bradley) Hook
*               -   Added OnBradleyTiersMediumSpawned(BradleyAPC bradley) Hook
*               -   Added OnBradleyTiersHardSpawned(BradleyAPC bradley) Hook
*               -   Added OnBradleyTiersNightmareSpawned(BradleyAPC bradley) Hook
*
*               -   Added OnDestroyBradleyTiersVanilla(BradleyAPC apc, HitInfo info) Hook
*               -   Added OnDestroyBradleyTiersEasy(BradleyAPC apc, HitInfo info) Hook
*               -   Added OnDestroyBradleyTiersMedium(BradleyAPC apc, HitInfo info) Hook
*               -   Added OnDestroyBradleyTiersHard(BradleyAPC apc, HitInfo info) Hook
*               -   Added OnDestroyBradleyTiersNightmare(BradleyAPC apc, HitInfo info) Hook
*
*               -   Added OnBradleyTiersHitVanilla(BradleyAPC bradley, HitInfo info) Hook
*               -   Added OnBradleyTiersHitEasy(BradleyAPC bradley, HitInfo info) Hook
*               -   Added OnBradleyTiersHitMedium(BradleyAPC bradley, HitInfo info) Hook
*               -   Added OnBradleyTiersHitHard(BradleyAPC bradley, HitInfo info) Hook
*               -   Added OnBradleyTiersHitNightmare(BradleyAPC bradley, HitInfo info) Hook
*               
*               -   Added support for BradleyDrops
*               -   Updated Include SatDish/Harbor Event Bradley inside the cfg
*
*   TODO :
*
*               -   Add a spawn probability
*               -   Add buyable bradley tiered APC
*                              
**********************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("BradleyTiers", "Krungh Crow", "1.2.2")]
    [Description("Bradley with difficulties and events")]

    class BradleyTiers : RustPlugin
    {
        [PluginReference]
        Plugin ArmoredTrain, Convoy ,Economics, GUIAnnouncements, Notify, UINotify, ServerRewards;

        #region Variables
        const string Use_Perm = "bradleytiers.use";

        private System.Random random = new System.Random();
        private Dictionary<string, List<ulong>> Skins { get; set; } = new Dictionary<string, List<ulong>>();

        ulong chaticon = 76561199183246772;
        string prefix;

        float accuracy;
        string _announce;
        int CaseCount;
        string color1;
        string color2;
        float damagescale;
        string difficulty;
        string _vanilla;
        string _easy;
        string _medium;
        string _hard;
        string _nightmare;
        string lootprofile;
        int reward;
        int total;
        int Pid = 0;

        bool Debug = false;
        bool showchat = true;
        bool showchatannouncement = true;
        bool showchatreward = true;
        bool UseNotify = false;
        bool IsConvoyAPC = false;


        #endregion

        #region Timers

        Dictionary<string, CooldownTimer> CoolDowns = new Dictionary<string, CooldownTimer>();
        public class CooldownTimer
        {
            public Timer timer;
            public float start;
            public float countdown;
        }

        #endregion

        #region Configuration
        void Init()
        {
            if (!LoadConfigVariables())
            {
            Puts("Config file issue detected. Please delete file, or check syntax and fix.");
            return;
            }

            permission.RegisterPermission(Use_Perm, this);

            Debug = configData.PlugCFG.Debug;
            showchat = configData.PlugCFG.ChatUse;
            showchatannouncement = configData.PlugCFG.ChatAnnounceUse;
            showchatreward = configData.PlugCFG.ChatRewardUse;
            UseNotify = configData.PlugCFG.UseNotify;
            Pid = configData.PlugCFG.PiD;
            prefix = configData.PlugCFG.Prefix;
            _vanilla = configData.Naming.Vanilla;
            _easy = configData.Naming.Easy;
            _medium = configData.Naming.Medium;
            _hard = configData.Naming.Hard;
            _nightmare = configData.Naming.Nightmare;
            if (Debug) Puts($"[Debug] Debug is Activated ,if unintentional check cfg and set to false");
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Main config")]
            public SettingsPlugin PlugCFG = new SettingsPlugin();
            [JsonProperty(PropertyName = "Tier Names")]
            public SettingsNames Naming = new SettingsNames();
            [JsonProperty(PropertyName = "Kill Rewards")]
            public SettingsReward Rewards = new SettingsReward();
            [JsonProperty(PropertyName = "Loot Tables")]
            public SettingsLoot Loot = new SettingsLoot();
            [JsonProperty(PropertyName = "Easy Bradley")]
            public SettingsBradEasy Easy = new SettingsBradEasy();
            [JsonProperty(PropertyName = "Medium Bradley")]
            public SettingsBradMedium Medium = new SettingsBradMedium();
            [JsonProperty(PropertyName = "Hard Bradley")]
            public SettingsBradHard Hard = new SettingsBradHard();
            [JsonProperty(PropertyName = "Nightmare Bradley")]
            public SettingsBradNightmare Nightmare = new SettingsBradNightmare();
        }

        class SettingsPlugin
        {
            [JsonProperty(PropertyName = "Debug")]
            public bool Debug = false;
            [JsonProperty(PropertyName = "Chat Prefix")]
            public string Prefix = "[<color=yellow>Bradley Tiers</color>] ";
            [JsonProperty(PropertyName = "Use GUIAnnouncement")]
            public bool GUIAUse = false;
            [JsonProperty(PropertyName = "Reply to player in chat on attack")]
            public bool ChatUse = true;
            [JsonProperty(PropertyName = "Reply to player in chat on reward")]
            public bool ChatRewardUse = true;
            [JsonProperty(PropertyName = "Show kills/spawns in Global chat")]
            public bool ChatAnnounceUse = true;
            [JsonProperty(PropertyName = "Use Notify")]
            public bool UseNotify = false;
            [JsonProperty(PropertyName = "Notify profile ID")]
            public int PiD = 0;
            [JsonProperty(PropertyName = "Include Vanilla Bradley")]
            public bool VanillaUse = false;
            [JsonProperty(PropertyName = "Include ArmoredTrain Bradley")]
            public bool TrainUse = false;
            [JsonProperty(PropertyName = "Include Convoy Bradley")]
            public bool ConvoyUse = false;
            [JsonProperty(PropertyName = "Include SatDish/Harbor Event Bradley")]
            public bool SatdishUse = false;
            [JsonProperty(PropertyName = "Bradley Tiers can interact with NPC")]
            public bool DmgNPCUse = false;
        }

        class SettingsNames
        {
            [JsonProperty(PropertyName = "Vanilla")]
            public string Vanilla = "Vanilla";
            [JsonProperty(PropertyName = "Easy")]
            public string Easy = "Easy";
            [JsonProperty(PropertyName = "Medium")]
            public string Medium = "Medium";
            [JsonProperty(PropertyName = "Hard")]
            public string Hard = "Hard";
            [JsonProperty(PropertyName = "Nightmare")]
            public string Nightmare = "Nightmare";
        }

        class SettingsReward
        {
            [JsonProperty(PropertyName = "Use Economics?")]
            public bool UseEco = false;
            [JsonProperty(PropertyName = "Use ServerRewards?")]
            public bool UseSR = false;
            [JsonProperty(PropertyName = "Vanilla amount")]
            public int Vanilla = 500;
            [JsonProperty(PropertyName = "Easy amount")]
            public int Easy = 1000;
            [JsonProperty(PropertyName = "Medium amount")]
            public int Medium = 1500;
            [JsonProperty(PropertyName = "Hard amount")]
            public int Hard = 2000;
            [JsonProperty(PropertyName = "Nightmare amount")]
            public int Nightmare = 2500;
        }

        class SettingsBradEasy
        {
            [JsonProperty(PropertyName = "Bradley Health")]
            public int BradleyHealth = 2000;
            [JsonProperty(PropertyName = "Bradley Max Fire Range")]
            public int BradleyMaxFireRange = 100;
            [JsonProperty(PropertyName = "Bradley Bulletdamage (15 is vanilla)")]
            public float BradleyBDamage = 15f;
            [JsonProperty(PropertyName = "Bradley Throttle Responce")]
            public float BradleySpeed = 1.0f;
            [JsonProperty(PropertyName = "Bradley Accuracy (0-1)")]
            public float BradleyAccuracy = 0.6f;
            [JsonProperty(PropertyName = "Bradley Damage scale (0-1)")]
            public float BradleyDamageScale = 0.6f;
            [JsonProperty(PropertyName = "Add Custom Loot")]
            public bool UpdateCratesLoot = true;
            [JsonProperty(PropertyName = "Bradley Max crates after kill")]
            public int BradleyCratesAmount = 4;
            [JsonProperty(PropertyName = "Spawn Min Amount Items")]
            public int MinAmount { get; set; } = 2;
            [JsonProperty(PropertyName = "Spawn Max Amount Items")]
            public int MaxAmount { get; set; } = 6;
            [JsonProperty(PropertyName = "Loot Table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<BradleyItem> Loot { get; set; } = DefaultLoot;
        }

        class SettingsBradMedium
        {
            [JsonProperty(PropertyName = "Bradley Health")]
            public int BradleyHealth = 4000;
            [JsonProperty(PropertyName = "Bradley Max Fire Range")]
            public int BradleyMaxFireRange = 100;
            [JsonProperty(PropertyName = "Bradley Bulletdamage (15 is vanilla)")]
            public float BradleyBDamage = 15f;
            [JsonProperty(PropertyName = "Bradley Throttle Responce")]
            public float BradleySpeed = 1.0f;
            [JsonProperty(PropertyName = "Bradley Accuracy (0-1)")]
            public float BradleyAccuracy = 0.75f;
            [JsonProperty(PropertyName = "Bradley Damage scale (0-1)")]
            public float BradleyDamageScale = 0.7f;
            [JsonProperty(PropertyName = "Add Custom Loot")]
            public bool UpdateCratesLoot = true;
            [JsonProperty(PropertyName = "Bradley Max crates after kill")]
            public int BradleyCratesAmount = 5;
            [JsonProperty(PropertyName = "Spawn Min Amount Items")]
            public int MinAmount { get; set; } = 2;
            [JsonProperty(PropertyName = "Spawn Max Amount Items")]
            public int MaxAmount { get; set; } = 6;
            [JsonProperty(PropertyName = "Loot Table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<BradleyItem> Loot { get; set; } = DefaultLoot;
        }

        class SettingsBradHard
        {
            [JsonProperty(PropertyName = "Bradley Health")]
            public int BradleyHealth = 10000;
            [JsonProperty(PropertyName = "Bradley Max Fire Range")]
            public int BradleyMaxFireRange = 100;
            [JsonProperty(PropertyName = "Bradley Bulletdamage (15 is vanilla)")]
            public float BradleyBDamage = 15f;
            [JsonProperty(PropertyName = "Bradley Throttle Responce")]
            public float BradleySpeed = 1.0f;
            [JsonProperty(PropertyName = "Bradley Accuracy (0-1)")]
            public float BradleyAccuracy = 0.8f;
            [JsonProperty(PropertyName = "Bradley Damage scale (0-1)")]
            public float BradleyDamageScale = 0.85f;
            [JsonProperty(PropertyName = "Add Custom Loot")]
            public bool UpdateCratesLoot = true;
            [JsonProperty(PropertyName = "Bradley Max crates after kill")]
            public int BradleyCratesAmount = 8;
            [JsonProperty(PropertyName = "Spawn Min Amount Items")]
            public int MinAmount { get; set; } = 2;
            [JsonProperty(PropertyName = "Spawn Max Amount Items")]
            public int MaxAmount { get; set; } = 6;
            [JsonProperty(PropertyName = "Loot Table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<BradleyItem> Loot { get; set; } = DefaultLoot;
        }

        class SettingsBradNightmare
        {
            [JsonProperty(PropertyName = "Bradley Health")]
            public int BradleyHealth = 15000;
            [JsonProperty(PropertyName = "Bradley Max Fire Range")]
            public int BradleyMaxFireRange = 100;
            [JsonProperty(PropertyName = "Bradley Bulletdamage (15 is vanilla)")]
            public float BradleyBDamage = 15f;
            [JsonProperty(PropertyName = "Bradley Throttle Responce")]
            public float BradleySpeed = 1.0f;
            [JsonProperty(PropertyName = "Bradley Accuracy (0-1)")]
            public float BradleyAccuracy = 0.85f;
            [JsonProperty(PropertyName = "Bradley Damage scale (0-1)")]
            public float BradleyDamageScale = 1.0f;
            [JsonProperty(PropertyName = "Add Custom Loot")]
            public bool UpdateCratesLoot = true;
            [JsonProperty(PropertyName = "Bradley Max crates after kill")]
            public int BradleyCratesAmount = 12;
            [JsonProperty(PropertyName = "Spawn Min Amount Items")]
            public int MinAmount { get; set; } = 2;
            [JsonProperty(PropertyName = "Spawn Max Amount Items")]
            public int MaxAmount { get; set; } = 6;
            [JsonProperty(PropertyName = "Loot Table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<BradleyItem> Loot { get; set; } = DefaultLoot;
        }

        class SettingsLoot
        {
            [JsonProperty(PropertyName = "Use lootsystem")]
            public bool UseLoot = false;
            [JsonProperty(PropertyName = "Use Random Skins")]
            public bool RandomSkins { get; set; } = true;
        }

        private bool LoadConfigVariables()
        {
            try
            {
            configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
            return false;
            }
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Fresh install detected Creating a new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Bradley"] = "Bradley",
                ["SpawnMessage"] = "A {0} Bradley has spawned around {1}",
                ["Destroyed"] = "finally destroyed the",
                ["Info"] = "\n<color=green>Available Commands</color>\n<color=green>/bt info</color> : Shows info on version/author and commands",
                ["InvalidInput"] = "<color=red>Please enter a valid command!</color>",
                ["KillRewardMessage"] = "You recieved {0}$ for Destroying the {1} BradleyApc",
                ["KillNotification"] = "<color=orange>{0}</color> Destroyed a {1} BradleyApc",
                ["AttackMessage"] = "You are taking on a {0} Bradleyapc",
                ["Version"] = "Version : V",
                ["NoPermission"] = "<color=green>You do not have permission to use that command!</color>",
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("bt")]
        private void cmdPrimary(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Use_Perm))
            {
                Player.Message(player, prefix + string.Format(msg("NoPermission", player.UserIDString)), chaticon);
                if (Debug) Puts($"[Debug] {player} had no permission for using Commands");
                return;
            }

            if (args.Length == 0)
            {
                Player.Message(player, prefix + string.Format(msg("InvalidInput", player.UserIDString)), chaticon);
            }
            else
            {
                if (args[0].ToLower() == "info")
                {
                    Player.Message(player, prefix + string.Format(msg("Version", player.UserIDString)) + this.Version.ToString() + " By : " + this.Author.ToString()
                    + msg("Info")
                    , chaticon);
                    return;
                }
                else
                {
                    Player.Message(player, prefix + string.Format(msg("InvalidInput", player.UserIDString)), chaticon);
                }

            }
        }
        #endregion

        #region Message helper

        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        #endregion

        #region Hooks

        private void OnBradleyApcInitialize(BradleyAPC bradley)
        {
            var dropposition = bradley.transform.position;
            string Grid = GetGrid(dropposition);
            int CaseCount = 4;
            if (configData.PlugCFG.VanillaUse) CaseCount = 5;
            timer.Once(0.5f, () =>
            {
                if (Interface.CallHook("IsTrainBradley", bradley) != null && configData.PlugCFG.TrainUse == false)
                {
                    Puts($"Disabled Tiered apc spawn Settings for Armored Train {bradley}");
                    return;
                }

                if (Interface.CallHook("IsConvoyVehicle", bradley) != null && bradley.skinID == 755446 && configData.PlugCFG.ConvoyUse == false)
                {
                    Puts($"Disabled Tiered apc spawn Settings for Convoy {bradley}");
                    return;
                }

                if (Interface.CallHook("CanBradleyTiersEdit", bradley) != null && configData.PlugCFG.SatdishUse == false)
                {
                        if (Debug) Puts($"Disabled Tiered apc for SatDishEvent {bradley}");
                        return;
                }
                if (Interface.CallHook("IsBradleyDrop" , bradley) != null)
                {
                    if (Debug) Puts($"Disabled Tiered apc for BradleyDrops {bradley}");
                    return;
                }

                switch (UnityEngine.Random.Range(0, CaseCount))
            {
                case 0:
                    bradley._maxHealth = configData.Easy.BradleyHealth;
                    bradley.health = configData.Easy.BradleyHealth;
                    bradley.bulletDamage = configData.Easy.BradleyBDamage;
                    bradley.viewDistance = configData.Easy.BradleyMaxFireRange;
                    bradley.searchRange = configData.Easy.BradleyMaxFireRange;
                    bradley.throttle = configData.Easy.BradleySpeed;
                    bradley.leftThrottle = bradley.throttle;
                    bradley.rightThrottle = bradley.throttle;
                    bradley.maxCratesToSpawn = configData.Easy.BradleyCratesAmount;
                    bradley.name = $"{_easy} BradleyApc[{bradley.net.ID}]";
                    Interface.CallHook("OnBradleyTiersEasySpawned", bradley);
                    difficulty = _easy;
                    color1 = "green";
                    _announce = "BradEasy";
                    break;
                case 1:
                    bradley._maxHealth = configData.Medium.BradleyHealth;
                    bradley.health = configData.Medium.BradleyHealth;
                    bradley.bulletDamage = configData.Medium.BradleyBDamage;
                    bradley.viewDistance = configData.Medium.BradleyMaxFireRange;
                    bradley.searchRange = configData.Medium.BradleyMaxFireRange;
                    bradley.throttle = configData.Medium.BradleySpeed;
                    bradley.leftThrottle = bradley.throttle;
                    bradley.rightThrottle = bradley.throttle;
                    bradley.maxCratesToSpawn = configData.Medium.BradleyCratesAmount;
                    bradley.name = $"{_medium} BradleyApc[{bradley.net.ID}]";
                    Interface.CallHook("OnBradleyTiersMediumSpawned", bradley);
                    difficulty = _medium;
                    color1 = "purple";
                    _announce = "BradMedium";
                    break;
                case 2:
                    bradley._maxHealth = configData.Hard.BradleyHealth;
                    bradley.health = configData.Hard.BradleyHealth;
                    bradley.bulletDamage = configData.Hard.BradleyBDamage;
                    bradley.viewDistance = configData.Hard.BradleyMaxFireRange;
                    bradley.searchRange = configData.Hard.BradleyMaxFireRange;
                    bradley.throttle = configData.Hard.BradleySpeed;
                    bradley.leftThrottle = bradley.throttle;
                    bradley.rightThrottle = bradley.throttle;
                    bradley.maxCratesToSpawn = configData.Hard.BradleyCratesAmount;
                    bradley.name = $"{_hard} BradleyApc[{bradley.net.ID}]";
                    Interface.CallHook("OnBradleyTiersHardSpawned", bradley);
                    difficulty = _hard;
                    color1 = "red";
                    _announce = "BradHard";
                    break;
                case 3:
                    bradley._maxHealth = configData.Nightmare.BradleyHealth;
                    bradley.health = configData.Nightmare.BradleyHealth;
                    bradley.bulletDamage = configData.Nightmare.BradleyBDamage;
                    bradley.viewDistance = configData.Nightmare.BradleyMaxFireRange;
                    bradley.searchRange = configData.Nightmare.BradleyMaxFireRange;
                    bradley.throttle = configData.Nightmare.BradleySpeed;
                    bradley.leftThrottle = bradley.throttle;
                    bradley.rightThrottle = bradley.throttle;
                    bradley.maxCratesToSpawn = configData.Nightmare.BradleyCratesAmount;
                    bradley.name = $"{_nightmare} BradleyApc[{bradley.net.ID}]";
                    Interface.CallHook("OnBradleyTiersNightmareSpawned", bradley);
                    difficulty = _nightmare;
                    color1 = "yellow";
                    _announce = "BradNightmare";
                    break;
                default:
                    bradley._maxHealth = 1000;
                    bradley.health = 1000;
                    bradley.maxCratesToSpawn = 3;
                    Interface.CallHook("OnBradleyTiersVanillaSpawned", bradley);
                    difficulty = _vanilla;
                    color1 = "#EC1349";
                    _announce = "BradVanilla";
                    break;
            }

                bradley.SendNetworkUpdate();
                var bradleytype = $"<color={color1}>{difficulty}</color>";
                var Gridcolor = $"<color={color1}>{Grid}</color>";
                string _MSG = $"A {bradleytype} BradleyAPC spawned at {Grid}";
                if ((Notify || UINotify) && UseNotify == true) foreach (var player in BasePlayer.activePlayerList)
                {
                    if (Notify) Notify.Call("SendNotify", player, Pid, _MSG);
                    if (UINotify) UINotify.Call("SendNotify", player, Pid, _MSG);
                }
                if (showchatannouncement) Server.Broadcast(prefix + string.Format(msg($"SpawnMessage"), bradleytype, Gridcolor), chaticon);
                if (_announce == "BradVanilla") color1 = "green";
                if (GUIAnnouncements != null && configData.PlugCFG.GUIAUse) GUIAnnouncements?.Call("CreateAnnouncement", string.Format(msg($"SpawnMessage"), difficulty, Grid), color1, "white");
                if (difficulty != _vanilla) Puts($"Upgraded a BradleyApc to a [{difficulty}] Tier");
                if (difficulty == _vanilla) Puts($"Allowed a Vanilla BradleyApc to spawn");
            });
        }

        void OnEntityDeath(BradleyAPC apc, HitInfo info)
        {
            if (apc == null) return;

            if (Interface.CallHook("IsTrainBradley", apc) != null && configData.PlugCFG.TrainUse == false)
            {
                Puts($"Disabled Tiered apc loot for Armored Train {apc}");
                return;
            }

            if (Interface.CallHook("IsConvoyVehicle", apc) != null && apc.skinID == 755446 && configData.PlugCFG.ConvoyUse == false)
            {
                Puts($"Disabled Tiered apc loot for Convoy {apc}");
                return;
            }

            if (Interface.CallHook("CanBradleyTiersEdit", apc) != null && configData.PlugCFG.SatdishUse == false)
            {
                if (Debug) Puts($"Disabled Tiered apc loot for SatDishEvent {apc}");
                return;
            }

            if (Interface.CallHook("IsBradleyDrop" , apc) != null)
            {
                if (Debug) Puts($"Disabled Tiered apc loot for BradleyDrops {apc}");
                return;
            }

            if (apc._maxHealth == 1000)
            {
                Interface.CallHook("OnDestroyBradleyTiersVanilla", apc, info);
                difficulty = _vanilla;
                reward = configData.Rewards.Vanilla;
                color1 = "#EC1349";
                color2 = "white";
            }

            else if (apc._maxHealth == configData.Easy.BradleyHealth)
            {
                Interface.CallHook("OnDestroyBradleyTiersEasy", apc, info);
                difficulty = _easy;
                reward = configData.Rewards.Easy;
                color1 = "green";
                color2 = "white";
            }

            else if (apc._maxHealth == configData.Medium.BradleyHealth)
            {
                Interface.CallHook("OnDestroyBradleyTiersMedium", apc, info);
                difficulty = _medium;
                reward = configData.Rewards.Medium;
                color1 = "purple";
                color2 = "white";
            }

            else if (apc._maxHealth == configData.Hard.BradleyHealth)
            {
                Interface.CallHook("OnDestroyBradleyTiersHard", apc, info);
                difficulty = _hard;
                reward = configData.Rewards.Hard;
                color1 = "red";
                color2 = "white";
            }

            else if (apc._maxHealth == configData.Nightmare.BradleyHealth)
            {
                Interface.CallHook("OnDestroyBradleyTiersNightmare", apc, info);
                difficulty = _nightmare;
                reward = configData.Rewards.Nightmare;
                color1 = "yellow";
                color2 = "red";
            }
            var killer = info.InitiatorPlayer.displayName;
            var bradleytype = $"<color={color1}>{difficulty}</color>";

            var _MSG = string.Format(msg("KillNotification"), killer, $" <color={color1}>{bradleytype}</color>");
            Puts($"{difficulty} {apc} at {GetGrid(apc.transform.position)} {apc.transform.position} was destroyed by {killer}");

            if (showchatannouncement) Server.Broadcast(prefix
            + _MSG
            , chaticon);
            if (GUIAnnouncements != null && configData.PlugCFG.GUIAUse)
            {
                if (apc._maxHealth == 1000) color1 = "green";
                GUIAnnouncements?.Call("CreateAnnouncement", (_MSG), color1, color2);
            }
            if (apc._maxHealth == 1000) color1 = "#EC1349";

            if ((Notify || UINotify) && UseNotify == true) foreach (var player in BasePlayer.activePlayerList)
                {
                    if (Notify) Notify.Call("SendNotify", player, Pid, _MSG);
                    if (UINotify) UINotify.Call("SendNotify", player, Pid, _MSG);
                }

            if (ServerRewards != null && configData.Rewards.UseSR == true)
            {
                ServerRewards?.Call("AddPoints", info.InitiatorPlayer.userID, reward);
                if (Debug) Puts($"[Debug] ServerRewards reward : {reward}RP handed out to {info.InitiatorPlayer}");
                if (showchatreward) Player.Message(info.InitiatorPlayer, prefix + string.Format(msg($"KillRewardMessage", info.InitiatorPlayer.UserIDString), $"<color={color1}>{reward}</color>", $"<color={color1}>{difficulty}</color>"), chaticon);
            }

            else if (Economics != null && configData.Rewards.UseEco == true)
            {
                if ((bool)Economics?.Call("Deposit", info.InitiatorPlayer.userID, (double)reward));
                if (Debug) Puts($"[Debug] Economics reward : {reward}$ handed out to {info.InitiatorPlayer}");
                if (showchatreward) Player.Message(info.InitiatorPlayer, prefix + string.Format(msg($"KillRewardMessage", info.InitiatorPlayer.UserIDString), $"<color={color1}>{reward}</color>", $"<color={color1}>{difficulty}</color>"), chaticon);
            }

            Vector3 position = apc.transform.position;
            int count = 0;

            if (configData.Loot.UseLoot == true)
            {
                timer.Once(1f, () =>
                {
                    float distance = 15f;
                    List<LootContainer> list = new List<LootContainer>();
                    Vis.Entities<LootContainer>(position, distance, list);

                    foreach (LootContainer crate in list)

                        if (crate.PrefabName.Contains("bradley_crate") && crate.skinID != 290578)
                        {
                            count++;
                            //crate.inventory.itemList.Clear();
                            if (difficulty == _vanilla)
                            {
                                crate.panelName = "generic_resizable";
                                crate.name = $"{difficulty}_Crate[{crate.net.ID}]";
                                crate.skinID = 290578;
                                return;
                            }
                            else if (difficulty == _easy && configData.Easy.UpdateCratesLoot == true)
                            {
                                crate.inventory.itemList.Clear();
                                SpawnLoot(crate.inventory, configData.Easy.Loot.ToList());

                            }
                            else if (difficulty == _medium && configData.Medium.UpdateCratesLoot == true)
                            {
                                crate.inventory.itemList.Clear();
                                SpawnLoot(crate.inventory, configData.Medium.Loot.ToList());

                            }
                            else if (difficulty == _hard && configData.Hard.UpdateCratesLoot == true)
                            {
                                crate.inventory.itemList.Clear();
                                SpawnLoot(crate.inventory, configData.Hard.Loot.ToList());

                            }
                            else if (difficulty == _nightmare && configData.Nightmare.UpdateCratesLoot == true)
                            {
                                crate.inventory.itemList.Clear();
                                SpawnLoot(crate.inventory, configData.Nightmare.Loot.ToList());

                                if (Debug) Puts($"[Debug] {difficulty} bradley loot test trigger");
                            }
                            crate.panelName = "generic_resizable";
                            crate.name = $"{difficulty}_Crate[{crate.net.ID}]";
                            crate.skinID = 290578;
                        }
                    Puts($"{count.ToString()} vanilla crates upgraded with {difficulty} Loot");
                });
                return;
            }
            if (Debug) Puts($"[Debug] Lootsystem skipped");
            return;
        }

        void OnEntityTakeDamage(BradleyAPC bradley, HitInfo info)
        {
            if (info.Initiator.IsValid())
            {
                if (Interface.CallHook("IsTrainBradley", bradley) != null && configData.PlugCFG.TrainUse == false) return;

                if (Interface.CallHook("IsConvoyVehicle", bradley) != null && bradley.skinID == 755446 && configData.PlugCFG.ConvoyUse == false) return;

                if (Interface.CallHook("CanBradleyTiersEdit", bradley) != null && configData.PlugCFG.SatdishUse == false) return;

                if (Interface.CallHook("IsBradleyDrop" , bradley) != null) return;

                BasePlayer player = info.InitiatorPlayer;
                if (!player ) return;
                string id = player.UserIDString;
                var Cooldown = new CooldownTimer();

                if (info?.Initiator is BasePlayer && info != null)
                {
                    if (bradley._maxHealth == 1000)
                    {
                        Interface.CallHook("OnBradleyTiersHitVanilla", bradley, info);
                        difficulty = _vanilla;
                        color1 = "#EC1349";
                    }

                    else if (bradley._maxHealth == configData.Easy.BradleyHealth)
                    {
                        Interface.CallHook("OnBradleyTiersHitEasy", bradley, info);
                        difficulty = _easy;
                        color1 = "green";
                    }

                    else if (bradley._maxHealth == configData.Medium.BradleyHealth)
                    {
                        Interface.CallHook("OnBradleyTiersHitMedium", bradley, info);
                        difficulty = _medium;
                        color1 = "purple";
                    }

                    else if (bradley._maxHealth == configData.Hard.BradleyHealth)
                    {
                        Interface.CallHook("OnBradleyTiersHitHard", bradley, info);
                        difficulty = _hard;
                        color1 = "red";
                    }

                    else if (bradley._maxHealth == configData.Nightmare.BradleyHealth)
                    {
                        Interface.CallHook("OnBradleyTiersHitNightmare", bradley, info);
                        difficulty = _nightmare;
                        color1 = "yellow";
                    }
                }

                if (!CoolDowns.ContainsKey(player.UserIDString) && info != null)
                {
                    if (player != null)
                    {
                        Cooldown.timer = timer.Once((float)120, () =>
                        {
                            CoolDowns.Remove(id);
                            if (Debug) Puts($"[Debug] Removed cooldown for {player}");

                        });
                        Cooldown.start = Time.realtimeSinceStartup;
                        Cooldown.countdown = 120f;
                        CoolDowns.Add(id, Cooldown);
                        if (Debug) Puts($"[Debug] Added cooldown for {player}");
                        var BradType = $" <color={color1}>{difficulty}</color>";
                        if (showchat) Player.Message(player, prefix + string.Format(msg($"AttackMessage"), BradType), chaticon);
                        if (Debug) Puts($"[Debug] {player} hit on a {difficulty} Bradleyapc");
                    }
                }
                string timesince = Math.Floor((CoolDowns[player.UserIDString].start + CoolDowns[player.UserIDString].countdown - Time.realtimeSinceStartup)).ToString();

                if (Debug) Puts($"[Debug] Msg cooldown {player} on a {difficulty} Bradleyapc with {timesince} sec left");
                //Player.Message(player, prefix + string.Format(msg($"[Debug] msg cooldown {player.displayName}\nOn a {$"<color={color1}>{difficulty}</color>"} Bradleyapc with {timesince} sec left", player.UserIDString)), chaticon);//dev debugging
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info?.Initiator is BradleyAPC && entity.IsNpc && configData.PlugCFG.DmgNPCUse == false) return null;
            
            if (info?.Initiator is BradleyAPC && entity is BasePlayer)
            {
                var bradley = info?.Initiator.GetComponent<BradleyAPC>() ?? null;
                {
                    if (Interface.CallHook("IsTrainBradley", bradley) != null && configData.PlugCFG.TrainUse == false) return null;

                    if (Interface.CallHook("IsConvoyVehicle", bradley) != null && bradley.skinID == 755446 && configData.PlugCFG.ConvoyUse == false) return null;

                    if (Interface.CallHook("CanBradleyTiersEdit", bradley) != null && configData.PlugCFG.SatdishUse == false) return null;

                    if (bradley._maxHealth == 1000)
                    {
                        accuracy = 1;
                        difficulty = _vanilla;
                        damagescale = 1;
                    }

                    else if (bradley._maxHealth == configData.Easy.BradleyHealth)
                    {
                        accuracy = configData.Easy.BradleyAccuracy;
                        difficulty = _easy;
                        damagescale = configData.Easy.BradleyDamageScale;
                    }

                    else if (bradley._maxHealth == configData.Medium.BradleyHealth)
                    {
                        accuracy = configData.Medium.BradleyAccuracy;
                        difficulty = _medium;
                        damagescale = configData.Medium.BradleyDamageScale;
                    }

                    else if (bradley._maxHealth == configData.Hard.BradleyHealth)
                    {
                        accuracy = configData.Hard.BradleyAccuracy;
                        difficulty = _hard;
                        damagescale = configData.Hard.BradleyDamageScale;
                    }

                    else if (bradley._maxHealth == configData.Nightmare.BradleyHealth)
                    {
                        accuracy = configData.Nightmare.BradleyAccuracy;
                        difficulty = _nightmare;
                        damagescale = configData.Nightmare.BradleyDamageScale;
                    }
                }

                float rand = (float)random.Next(1, 100) / 100f;

                if (accuracy < rand)
                {
                    if (Debug) Puts($"[Debug] Accuracy randomiser is {rand * 100}% with {accuracy * 100}% in cfg : skipped dmg dealing");
                    return true;
                }
                else
                {
                    if (Debug) Puts($"[Debug] A {difficulty} BradleyApc dealth {info.damageTypes.Total() * damagescale} Damage to {entity} with {damagescale * 100}% force");
                    info.damageTypes.ScaleAll(damagescale);
                    return null;
                }
            }

            return null;
        }

        private object CanBradleyApcTarget(BradleyAPC bradley, NPCPlayer npc)
        {
            if (Interface.CallHook("IsTrainBradley", bradley) != null && configData.PlugCFG.TrainUse == false) return null;

            if (Interface.CallHook("IsConvoyVehicle", bradley) != null && bradley.skinID == 755446 && configData.PlugCFG.ConvoyUse == false) return null;

            if (Interface.CallHook("CanBradleyTiersEdit", bradley) != null && configData.PlugCFG.SatdishUse == false) return null;

            if (Interface.CallHook("IsBradleyDrop" , bradley) != null) return null;

            if (npc != null && configData.PlugCFG.DmgNPCUse) return true;
            return null;
        }

        #endregion

        #region Event helpers

        private static string GetGrid(Vector3 position) => PhoneController.PositionToGridCoord(position);

        #endregion

        #region Loot System

        private static List<BradleyItem> DefaultLoot
        {
            get
            {
                return new List<BradleyItem>
                {
                    new BradleyItem { probability = 1f, shortname = "ammo.pistol", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "ammo.pistol.fire", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "ammo.rifle.explosive", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "ammo.rifle.hv", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "ammo.rifle.incendiary", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "ammo.shotgun", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "ammo.shotgun", name = "", amountMin = 128, amount = 256, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "explosive.timed", name = "", amountMin = 1, amount = 5, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "explosives", name = "", amountMin = 200, amount = 200, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "pistol.m92", name = "", amountMin = 1, amount = 1, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "shotgun.spas12", name = "", amountMin = 1, amount = 1, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "pickaxe", name = "", amountMin = 1, amount = 1, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "hatchet", name = "", amountMin = 1, amount = 1, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "can.beans", name = "", amountMin = 3, amount = 5, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "can.tuna", name = "", amountMin = 3, amount = 5, skin = 0 },
                    new BradleyItem { probability = 1f, shortname = "black.raspberries", name = "", amountMin = 3, amount = 5, skin = 0 },
                    new BradleyItem { probability = 0.6f, shortname = "hmlmg", name = "", amountMin = 1, amount = 1, skin = 0 },
                    new BradleyItem { probability = 0.6f, shortname = "supply.signal", name = "", amountMin = 1, amount = 1, skin = 0 },
                };
            }
        }

        public class BradleyItem
        {
            public float probability { get; set; } = 1f;
            public string shortname { get; set; }
            public string name { get; set; } = "";
            public ulong skin { get; set; }
            public int amountMin { get; set; }
            public int amount { get; set; }
        }

        private void SpawnLoot(ItemContainer container, List<BradleyItem> loot)
        {
            if (difficulty == _vanilla)
            {
                if (Debug) Puts($"[Debug] {difficulty} bradley loot test trigger");
            }
            else if (difficulty == _easy)
            {
                total = UnityEngine.Random.Range(Math.Min(loot.Count, configData.Easy.MinAmount), Math.Min(loot.Count, configData.Easy.MaxAmount));
                if (Debug) Puts($"[Debug] {difficulty} bradley loot test trigger");
            }
            else if (difficulty == _medium)
            {
                total = UnityEngine.Random.Range(Math.Min(loot.Count, configData.Medium.MinAmount), Math.Min(loot.Count, configData.Medium.MaxAmount));
                if (Debug) Puts($"[Debug] {difficulty} bradley loot test trigger");
            }
            else if (difficulty == _hard)
            {
                total = UnityEngine.Random.Range(Math.Min(loot.Count, configData.Hard.MinAmount), Math.Min(loot.Count, configData.Hard.MaxAmount));
                if (Debug) Puts($"[Debug] {difficulty} bradley loot test trigger");
            }
            else if (difficulty == _nightmare)
            {
                total = UnityEngine.Random.Range(Math.Min(loot.Count, configData.Nightmare.MinAmount), Math.Min(loot.Count, configData.Nightmare.MaxAmount));
                if (Debug) Puts($"[Debug] {difficulty} bradley loot test trigger");
            }

            if (total == 0 || loot.Count == 0)
            {
                return;
            }
            if (Debug) Puts($"[Debug] Upgraded a {difficulty} crate with {total} items");
            container.Clear();

            container.capacity = total;
            ItemDefinition def;
            List<ulong> skins;
            BradleyItem lootItem;

            for (int j = 0; j < total; j++)
            {
                if (loot.Count == 0)
                {
                    break;
                }

                lootItem = loot.GetRandom();

                loot.Remove(lootItem);

                if (lootItem.amount <= 0)
                {
                    continue;
                }

                string shortname = lootItem.shortname;
                if (UnityEngine.Random.value > lootItem.probability)
                {
                    if (!string.IsNullOrEmpty(lootItem.name)) Puts($"Skipping Custom({j + 1}) : {lootItem.name} : probability = {lootItem.probability}");
                    else if (string.IsNullOrEmpty(lootItem.name)) Puts($"Skipping Normal({j + 1}) : {lootItem.shortname} : probability = {lootItem.probability}");
                    j--;
                    continue;
                }

                bool isBlueprint = shortname.EndsWith(".bp");

                if (isBlueprint)
                {
                    shortname = shortname.Replace(".bp", string.Empty);
                }

                def = ItemManager.FindItemDefinition(shortname);

                if (def == null)
                {
                    Puts("Invalid shortname: {0}", lootItem.shortname);
                    continue;
                }

                ulong skin = lootItem.skin;

                if (configData.Loot.RandomSkins && skin == 0)
                {
                    skins = GetItemSkins(def);

                    if (skins.Count > 0)
                    {
                        skin = skins.GetRandom();
                    }
                }

                int amount = lootItem.amount;

                if (amount <= 0)
                {
                    continue;
                }

                if (lootItem.amountMin > 0 && lootItem.amountMin < lootItem.amount)
                {
                    amount = UnityEngine.Random.Range(lootItem.amountMin, lootItem.amount);
                }

                Item item;

                if (isBlueprint)
                {
                    item = ItemManager.CreateByItemID(-996920608, 1, 0);

                    if (item == null) continue;

                    item.blueprintTarget = def.itemid;
                    item.amount = amount;
                }
                else item = ItemManager.Create(def, amount, skin);

                if (!item.MoveToContainer(container, -1, false))
                {
                    item.Remove();
                }

                if (!string.IsNullOrEmpty(lootItem.name)) { item.name = lootItem.name; }
                if (Debug)
                {
                    if (string.IsNullOrEmpty(item.name))
                    {
                        Puts($"Normal({j + 1}) : {item.info.shortname} : Amount {item.amount} SkinID : {item.skin}");
                    }
                    if (!string.IsNullOrEmpty(item.name))
                    {
                        Puts($"Custom({j + 1}) : {item.name} : Amount : {item.amount} SkinID : {item.skin}");
                    }
                }
            }
        }

        private List<ulong> GetItemSkins(ItemDefinition def)
        {
            List<ulong> skins;
            if (!Skins.TryGetValue(def.shortname, out skins))
            {
                Skins[def.shortname] = skins = ExtractItemSkins(def, skins);
            }

            return skins;
        }

        private List<ulong> ExtractItemSkins(ItemDefinition def, List<ulong> skins)
        {
            skins = new List<ulong>();

            foreach (var skin in def.skins)
            {
                skins.Add(Convert.ToUInt64(skin.id));
            }
            foreach (var asi in Rust.Workshop.Approved.All.Values)
            {
                if (!string.IsNullOrEmpty(asi.Skinnable.ItemName) && asi.Skinnable.ItemName == def.shortname)
                {
                    skins.Add(Convert.ToUInt64(asi.WorkshopdId));
                }
            }

            return skins;
        }
        #endregion
    }
}