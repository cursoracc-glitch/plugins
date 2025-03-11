using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DeathMessages", "VooDoo", "1.4.4")]  
    [Description("DeathMessages")]
    public class DeathMessages : RustPlugin
    {
        #region Var's
        [PluginReference]
        private Plugin ImageLibrary, Clans;

        private bool AddImage(string url, string imageName, ulong imageId = 0, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        private string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);

        private Dictionary<ulong, TemporaryBool> uiHide = new Dictionary<ulong, TemporaryBool>();
        private Dictionary<BaseEntity, HitInfo> lastHitInfo = new Dictionary<BaseEntity, HitInfo>();
        private Dictionary<uint, string> prefabID2Item = new Dictionary<uint, string>();
        private Dictionary<string, string> prefabName2Item = new Dictionary<string, string>()
        {
            ["40mm_grenade_he"] = "multiplegrenadelauncher",
            ["grenade.beancan.deployed"] = "grenade.beancan",
            ["grenade.f1.deployed"] = "grenade.f1",
            ["explosive.satchel.deployed"] = "explosive.satchel",
            ["explosive.timed.deployed"] = "explosive.timed",
            ["rocket_basic"] = "rocket.launcher",
            ["rocket_admin"] = "rocket.launcher",
            ["rocket_hv"] = "rocket.launcher",
            ["rocket_fire"] = "rocket.launcher",
            ["survey_charge.deployed"] = "surveycharge"
        };
        
        public struct TemporaryBool
        {
            public bool Value;
            public double Expire;
        }

        public static DeathMessages Instance;
        #endregion

        #region Configuration
        public PluginConfig Configuration;
        public class PluginConfig
        {
            public class ColorNickName
            {
                [JsonProperty("Цвет ника если игрока убили (hex)")]
                public string ColorDeath;
                [JsonProperty("Цвет ника если игрок убил (hex)")]
                public string ColorKill;
            }

            public class UISettings
            {
                [JsonProperty("Цвет задней панели убийства")]
                public string BackgroundColor;
                [JsonProperty("Цвет панели с дистанцией")]
                public string DistanceColor;
                [JsonProperty("Отступ сверху")]
                public int OffsetY;
            }

            [JsonProperty("Максимальное количество уведомлений")]
            public int MaxKillsForBar = 3;
            [JsonProperty("Показывать моды оружия")]
            public bool ShowWeaponMods = true;
            [JsonProperty("Показывать смерть животных")]
            public bool ShowAnimalsDeath = true;
            [JsonProperty("Показывать смерть НПЦ")]
            public bool ShowNPCDeath = true;
            [JsonProperty("Показывать хедшоты")]
            public bool ShowHeadShots = true;
            [JsonProperty("Цвет ника по привилегиям (По стандарту белый)")]
            public Dictionary<string, ColorNickName> ColorsNamePlayer = new Dictionary<string, PluginConfig.ColorNickName>
            {
                { "deathmessages.premium", new PluginConfig.ColorNickName { ColorDeath = "#55ff8a", ColorKill = "#55ff8a" } },
                { "deathmessages.vip", new PluginConfig.ColorNickName { ColorDeath = "#f9ff55", ColorKill = "#f9ff55" } },
                { "deathmessages.deluxe", new PluginConfig.ColorNickName { ColorDeath = "#7303c0", ColorKill = "#7303c0" } },
                { "deathmessages.godlike", new PluginConfig.ColorNickName { ColorDeath = "#ff0000", ColorKill = "#ff0000" } }
            };
            [JsonProperty("Настройка интерфейса")]
            public UISettings UI = new PluginConfig.UISettings
            {
                BackgroundColor = "#00000080",
                DistanceColor = "#FFFFFF00",
                OffsetY = 0
            };
            [JsonProperty("Названия")]
            public Dictionary<string, string> Names = new Dictionary<string, string>()
            {
                ["npcplayer"] = "NPC",
                ["guntrap.deployed"] = "Guntrap",
                ["landmine"] = "Landmine",
                ["beartrap"] = "Bear trap",
                ["flameturret.deployed"] = "Flame turret",
                ["flameturret_fireball"] = "Flame turret",
                ["autoturret_deployed"] = "Turret",
                ["sentry.scientist.static"] = "Turret NPC",
                ["sentry.bandit.static"] = "Turret NPC",
                ["spikes.floor"] = "Spikes",
                ["spikes_static"] = "Spikes",
                ["teslacoil.deployed"] = "Tesla",
                ["barricade.wood"] = "Barricade",
                ["barricade.woodwire"] = "Barricade",
                ["barricade.metal"] = "Barricade",
                ["bradleyapc"] = "BradleyAPC",
                ["gates.external.high.wood"] = "Gates",
                ["gates.external.high.stone"] = "Gates",
                ["icewall"] = "Ice wall",
                ["wall.external.high.ice"] = "Ice wall",
                ["wall.external.high.stone"] = "Wall",
                ["wall.external.high.wood"] = "Wall",
                ["campfire"] = "Campfire",
                ["skull_fire_pit"] = "Campfire",
                ["lock.code"] = "Codelock",
                ["boar"] = "Boar",
                ["bear"] = "Bear",
                ["wolf"] = "Wolf",
                ["stag"] = "Stag",
                ["chicken"] = "Chicken",
                ["horse"] = "Horse",
                ["minicopter.entity"] = "Minicopter",
                ["scraptransporthelicopter"] = "Transport helicopter",
                ["patrolhelicopter"] = "Patrol helicopter",
                ["napalm"] = "Napalm",
                ["fireball_small"] = "Fire",
                ["fireball_small_shotgun"] = "Fire",
                ["fireball_small_arrow"] = "Fire",
                ["sam_site_turret_deployed"] = "SAM",
                ["cactus-1"] = "Cactus",
                ["cactus-2"] = "Cactus",
                ["cactus-3"] = "Cactus",
                ["cactus-4"] = "Cactus",
                ["cactus-5"] = "Cactus",
                ["cactus-6"] = "Cactus",
                ["cactus-7"] = "Cactus",
                ["hotairballoon"] = "Hot air balloon",
                ["cave_lift_trigger"] = "Lift"
            };
            [JsonProperty("Префикс в чате")]
            public string ChatPrefix = "<color=#55ff8a>[DeathMessages]</color>";
            [JsonProperty("Время появления новой строчки в секундах")]
            public string FadeIn  = "1";
        }

        private void Init()
        {
            Configuration = Config.ReadObject<PluginConfig>();
            Config.WriteObject(Configuration);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"USAGE", ": Usage example: /dm <on>/<off>"},
                {"ENABLED", ": You enable DeathMessages"},
                {"DISABLED", ": You disable DeathMessages"}
            }, this);
        }

        #endregion

        #region U'mod Hook's
        private void OnServerInitialized()
        {
            Instance = this;
            uiHide = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, TemporaryBool>>("DMData");
            foreach (var itemDef in ItemManager.GetItemDefinitions())
            {
                Item newItem = ItemManager.CreateByName(itemDef.shortname, 1, 0);

                BaseEntity heldEntity = newItem.GetHeldEntity();
                if (heldEntity != null)
                {
                    prefabID2Item[heldEntity.prefabID] = itemDef.shortname;
                }

                var deployablePrefab = itemDef.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
                if (string.IsNullOrEmpty(deployablePrefab))
                {
                    continue;
                }

                var shortPrefabName = GameManager.server.FindPrefab(deployablePrefab)?.GetComponent<BaseEntity>()?.ShortPrefabName;
                if (!string.IsNullOrEmpty(shortPrefabName) && !prefabName2Item.ContainsKey(shortPrefabName))
                {
                    prefabName2Item.Add(shortPrefabName, itemDef.shortname);
                }
            }

            foreach (var item in ItemManager.itemDictionary)
            {
                if (HasImage(item.Value.shortname, 16) == false)
                {
                    AddImage($"https://api.skyplugins.ru/api/getimage/{item.Value.shortname}/16", item.Value.shortname, 16);
                }
            }

            AddImage($"https://i.imgur.com/aK1fE31.png", "headshot", 16);

            foreach (var perm in Configuration.ColorsNamePlayer)
                permission.RegisterPermission(perm.Key, this);

            if (Configuration.ShowAnimalsDeath == false)
            {
                Unsubscribe("OnEntityDeath");
            }

            List<ulong> hPlayersCache = new List<ulong>();
            foreach(var hPlayer in uiHide)
            {
                if(hPlayer.Value.Expire + 604800 < new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds())
                {
                    hPlayersCache.Add(hPlayer.Key);
                }
            }

            foreach(var hPlayer in hPlayersCache)
            {
                uiHide.Remove(hPlayer);
            }
        }

        private void Unload()
        {
            if (DeathNotesTimer != null)
                DeathNotesTimer.Destroy();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "DeathMessages");
            }

            Interface.Oxide.DataFileSystem.WriteObject("DMData", uiHide);
        }

        private double lastSave = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        private void OnServerSave()
        {
            double value = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
            if (lastSave + 1800 < value)
            {
                lastSave = value;
                Interface.Oxide.DataFileSystem.WriteObject("DMData", uiHide);
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity != null && hitInfo != null)
            {
                if (entity is BasePlayer || entity is BaseAnimalNPC)
                {
                    lastHitInfo[entity] = hitInfo;
                }
            }

            return null;
        }

        private object OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BaseAnimalNPC)
            {
                if (hitInfo == null)
                {
                    if (lastHitInfo.TryGetValue(entity, out hitInfo) == false)
                    {
                        return null;
                    }
                }

                if (hitInfo.InitiatorPlayer != null)
                {
                    OnDeath(entity, hitInfo.InitiatorPlayer, hitInfo);
                }
            }
            return null;
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (player != null)
            {
                if (hitInfo == null)
                {
                    if (lastHitInfo.TryGetValue(player, out hitInfo) == false)
                    {
                        return null;
                    }
                }

                if (hitInfo.InitiatorPlayer != null)
                {
                    if(hitInfo.InitiatorPlayer.IsNpc || player.IsNpc)
                    {
                        if(Configuration.ShowNPCDeath == false)
                        {
                            return null;
                        }
                    }

                    OnDeath(player, hitInfo.InitiatorPlayer, hitInfo);
                }
                else
                {
                    if (hitInfo.Initiator != null)
                    {
                        if (hitInfo.Initiator.IsNpc || player.IsNpc)
                        {
                            if (Configuration.ShowNPCDeath == false)
                            {
                                return null;
                            }
                        }
                        OnDeath(player, hitInfo.Initiator, hitInfo);
                    }
                }
            }
            return null;
        }
        #endregion

        #region Death Logic

        public struct WeaponInfo
        {
            public string WeaponName;
            public string[] WeaponMods;
            public bool IsHeadShot;
            public bool IsBody;
        }

        private void OnDeath(BasePlayer victim, BasePlayer initiator, HitInfo hitInfo)
        {
            string victimDisplayName = string.Empty;
            string victimColorName = string.Empty;
            string initiatorDisplayName = string.Empty;
            string initiatorColorName = string.Empty;

            bool needRenameVictim = false;
            bool needRenameInitiator = false;

            WeaponInfo weaponInfo = GetWeaponInfo(hitInfo);

            float fDistance = Vector3.Distance(victim.transform.position, initiator.transform.position);
            float sDistance = fDistance > 650 ? 650 : fDistance;

            weaponInfo.IsHeadShot = hitInfo.HitBone == 698017942;

            if (initiator.IsNpc == true || initiator.userID.IsSteamId() == false)
            {
                initiatorDisplayName = "npcplayer";
                needRenameInitiator = true;
            }
            else
            {
                initiatorDisplayName = initiator.displayName;
            }

            if (victim.IsNpc == true || victim.userID.IsSteamId() == false)
            {
                victimDisplayName = "npcplayer";
                needRenameVictim = true;
            }
            else
            {
                victimDisplayName = victim.displayName;
            }

            foreach (var perm in Configuration.ColorsNamePlayer)
            {
                if (victim.IsNpc == false)
                {
                    if (permission.UserHasPermission(victim.UserIDString, perm.Key))
                    {
                        victimColorName = perm.Value.ColorDeath;
                    }
                }

                if (initiator.IsNpc == false)
                {
                    if (permission.UserHasPermission(initiator.UserIDString, perm.Key))
                    {
                        initiatorColorName = perm.Value.ColorKill;
                    }
                }
            }

            new DeathNote(victimDisplayName, initiatorDisplayName, weaponInfo, sDistance, victimColorName, initiatorColorName, needRenameVictim, needRenameInitiator);
        }

        private void OnDeath(BaseEntity victim, BasePlayer initiator, HitInfo hitInfo)
        {
            string victimDisplayName = string.Empty;
            string victimColorName = string.Empty;
            string initiatorDisplayName = string.Empty;
            string initiatorColorName = string.Empty;

            bool needRenameVictim = false;
            bool needRenameInitiator = false;

            WeaponInfo weaponInfo = GetWeaponInfo(hitInfo);

            float fDistance = Vector3.Distance(victim.transform.position, initiator.transform.position);
            float sDistance = fDistance > 650 ? 650 : fDistance;

            weaponInfo.IsBody = hitInfo.HitBone == 383;
            weaponInfo.IsHeadShot = hitInfo.HitBone == 698017942;

            if (initiator.IsNpc == true || initiator.userID.IsSteamId() == false)
            {
                initiatorDisplayName = "npcplayer";
                needRenameInitiator = true;
            }
            else
            {
                initiatorDisplayName = initiator.displayName;
            }

            victimDisplayName = victim.ShortPrefabName;
            needRenameVictim = true;

            foreach (var perm in Configuration.ColorsNamePlayer)
            {
                if (initiator.IsNpc == false)
                {
                    if (permission.UserHasPermission(initiator.UserIDString, perm.Key))
                    {
                        initiatorColorName = perm.Value.ColorKill;
                    }
                }
            }

            new DeathNote(victimDisplayName, initiatorDisplayName, weaponInfo, sDistance, victimColorName, initiatorColorName, needRenameVictim, needRenameInitiator);
        }

        private WeaponInfo GetWeaponInfo(HitInfo hitInfo)
        {
            WeaponInfo weaponInfo = new WeaponInfo()
            {
                WeaponName = string.Empty,
                WeaponMods = new string[] { },
                IsHeadShot = false
            };

            if (hitInfo.Weapon != null)
            {
                Item itemWeapon = hitInfo.Weapon.GetItem();
                if (itemWeapon != null)
                {
                    weaponInfo.WeaponName = itemWeapon.info.shortname;
                    if (itemWeapon.contents != null)
                    {
                        weaponInfo.WeaponMods = itemWeapon.contents.itemList.Count > 0 ? itemWeapon.contents.itemList.Select(x => x.info.shortname).ToArray() : new string[] { };
                    }
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true && hitInfo.ProjectilePrefab != null)
            {
                if (hitInfo.ProjectilePrefab.sourceWeaponPrefab != null)
                {
                    Item itemWeapon = hitInfo.ProjectilePrefab.sourceWeaponPrefab.GetItem();
                    if (itemWeapon != null)
                    {
                        weaponInfo.WeaponName = itemWeapon.info.shortname;
                        if (itemWeapon.contents != null)
                        {
                            weaponInfo.WeaponMods = itemWeapon.contents.itemList.Count > 0 ? itemWeapon.contents.itemList.Select(x => x.info.shortname).ToArray() : new string[] { };
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true && hitInfo.WeaponPrefab != null)
            {
                if (prefabID2Item.TryGetValue(hitInfo.WeaponPrefab.prefabID, out weaponInfo.WeaponName) == false)
                {
                    prefabName2Item.TryGetValue(hitInfo.WeaponPrefab.ShortPrefabName, out weaponInfo.WeaponName);
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName))
            {
                weaponInfo.WeaponName = hitInfo.damageTypes.GetMajorityDamageType().ToString();
            }

            return weaponInfo;
        }

        private void OnDeath(BasePlayer victim, BaseEntity initiator, HitInfo hitInfo)
        {
            string victimDisplayName = string.Empty;
            string victimColorName = string.Empty;
            string initiatorDisplayName = string.Empty;
            bool needRenameVictim = false;
            bool needRenameInitiator = false;

            WeaponInfo weaponInfo = GetWeaponInfoFromPrefab(initiator);

            float fDistance = Vector3.Distance(victim.transform.position, initiator.transform.position);
            float sDistance = fDistance > 650 ? 650 : fDistance;

            if (initiator != null)
            {
                initiatorDisplayName = initiator.ShortPrefabName;

                if (Configuration.Names.ContainsKey(initiatorDisplayName) == false)
                {
                    LogToFile("DeathMessages", "[CONFIG ISSUE] Не найдено красивое названия для: " + initiator.ShortPrefabName, this, true);
                    return;
                }

                needRenameInitiator = true;
            }

            if (victim.IsNpc == true || victim.userID.IsSteamId() == false)
            {
                victimDisplayName = "npcplayer";
                needRenameVictim = true;
            }

            if (victim.IsNpc == false)
            {
                victimDisplayName = victim.displayName;
            }

            foreach (var perm in Configuration.ColorsNamePlayer)
            {
                if (!victim.IsNpc)
                {
                    if (permission.UserHasPermission(victim.UserIDString, perm.Key))
                    {
                        victimColorName = perm.Value.ColorDeath;
                    }
                }
            }

            new DeathNote(victimDisplayName, initiatorDisplayName, weaponInfo, sDistance, victimColorName, "#FFFFF", needRenameVictim, needRenameInitiator);
        }

        private WeaponInfo GetWeaponInfoFromPrefab(BaseEntity initiator)
        {
            WeaponInfo weaponInfo = new WeaponInfo()
            {
                WeaponName = string.Empty,
                WeaponMods = new string[0],
                IsHeadShot = false
            };

            if (initiator is AutoTurret)
            {
                AutoTurret autoTurret = initiator as AutoTurret;
                Item itemWeapon = autoTurret.inventory.itemList.Where(x => x.info.category == ItemCategory.Weapon).FirstOrDefault();
                if (itemWeapon != null)
                {
                    if (itemWeapon != null)
                    {
                        weaponInfo.WeaponName = itemWeapon.info.shortname;
                        if (itemWeapon.contents != null)
                        {
                            weaponInfo.WeaponMods = itemWeapon.contents.itemList.Count > 0 ? itemWeapon.contents.itemList.Select(x => x.info.shortname).ToArray() : new string[] { };
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true)
            {
                if (prefabID2Item.TryGetValue(initiator.prefabID, out weaponInfo.WeaponName) == false)
                {
                    prefabName2Item.TryGetValue(initiator.ShortPrefabName, out weaponInfo.WeaponName);
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true)
            {
                weaponInfo.WeaponName = initiator.ShortPrefabName;
            }

            return weaponInfo;
        }

        public static Encoding CP866 = Encoding.GetEncoding("CP866");
        public static string GetCleanString(string str)
        {
            try
            {
                return CP866.GetString(Encoding.Convert(Encoding.UTF8, CP866, Encoding.UTF8.GetBytes(str)));
            }
            catch(ArgumentOutOfRangeException exception)
            {
                return str;
            }
        }

        public static double GetStringWidth(string message, string font, int fontSize)
        {
            if (message.Contains("</color>"))
            {
                message = message.Substring(16);
                message = message.Replace("</color>", "");
            }
            
            System.Drawing.Font stringFont = new System.Drawing.Font(font, fontSize, System.Drawing.FontStyle.Bold);
            using (System.Drawing.Bitmap tempImage = new System.Drawing.Bitmap(200, 200))
            {
                System.Drawing.SizeF stringSize = System.Drawing.Graphics.FromImage(tempImage).MeasureString(message, stringFont);
                return stringSize.Width;
            }
        }
        #endregion

        #region KillFeed Controler
        private static Timer DeathNotesTimer;

        public class DeathNote
        {
            public string VictimName { get; set; }
            public string InitiatorName { get; set; }

            public WeaponInfo WeaponInfo { get; set; }

            public string Distance { get; set; }

            public static List<string> DeathNotes = new List<string>();

            public DeathNote(string victimName, string initiatorName, WeaponInfo weaponInfo, float distance, string colorVictim = "", string colorInitiator = "", bool needRenameVictim = false, bool needRenameInitiator = false)
            {
                this.WeaponInfo = weaponInfo;
                this.Distance = distance.ToString("0.0");

                if (needRenameVictim && Instance.Configuration.Names.TryGetValue(victimName, out victimName))
                    this.VictimName = $"{victimName}";
                else
                    this.VictimName = $"<color={(string.IsNullOrEmpty(colorVictim) ? "#ffffff" : colorVictim)}>{GetCleanString(victimName)}</color>";

                if (needRenameInitiator && Instance.Configuration.Names.TryGetValue(initiatorName, out initiatorName))
                    this.InitiatorName = $"{initiatorName}";
                else
                    this.InitiatorName = $"<color={(string.IsNullOrEmpty(colorInitiator) ? "#ffffff" : colorInitiator)}>{GetCleanString(initiatorName)}</color>";
                
                if (DeathNotes.Count > Instance.Configuration.MaxKillsForBar - 1)
                    DeathNotes.Remove(DeathNotes.LastOrDefault());

                if (DeathNotes.Count == 0)
                    DeathNotes.Add(ToJson());
                else
                    DeathNotes.Insert(0, ToJson());

                UpdateUI(true);
                UpdateTimer();
            }

            public string ToJson()
            {
                return Instance.GetReplacedString
                (
                    VictimName,
                    InitiatorName,
                    WeaponInfo.WeaponName,
                    WeaponInfo.WeaponMods,
                    WeaponInfo.IsHeadShot,
                    Distance,
                    GetStringWidth(VictimName, "Roboto Condensed", 8) + 20,
                    GetStringWidth(InitiatorName, "Roboto Condensed", 8) + 20
                );
            }
        }

        public static void UpdateUI(bool onInsert)
        {
            string deathContainer = Instance.GetUIContainerString();
            string[] dNotes = new string[DeathNote.DeathNotes.Count];
            for (int i = 0; i < DeathNote.DeathNotes.Count; i++)
            {
                string dNote = string.Copy(DeathNote.DeathNotes[i]);
                dNote = dNote.Replace($"{{MainOffsetMinY}}", $"{-66 + (Instance.Configuration.UI.OffsetY) - i * 25}");
                dNote = dNote.Replace($"{{MainOffsetMaxY}}", $"{-46 + (Instance.Configuration.UI.OffsetY) - i * 25}");

                if (i == 0 && onInsert)
                    dNote = dNote.Replace("1337", Instance.Configuration.FadeIn);
                else
                    dNote = dNote.Replace("1337", "0");

                dNotes[i] = dNote;
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                TemporaryBool showUI;
                if (Instance.uiHide.TryGetValue(player.userID, out showUI) && showUI.Value == false)
                    continue;

                CuiHelper.DestroyUi(player, "DeathMessages");
                CuiHelper.AddUi(player, deathContainer);

                for (int i = 0; i < dNotes.Length; i++)
                {
                    CuiHelper.AddUi(player, dNotes[i]);
                }
            }
        }

        public static void UpdateTimer()
        {
            if (DeathNotesTimer != null)
                DeathNotesTimer.Destroy();

            DeathNotesTimer = Instance.timer.In(5f, () =>
            {
                if (DeathNote.DeathNotes.Count > 0)
                {
                    DeathNote.DeathNotes.Remove(DeathNote.DeathNotes.LastOrDefault());

                    UpdateUI(false);
                    UpdateTimer();
                }
                else
                {
                    DeathNotesTimer.Destroy();
                }
            });
        }
        #endregion

        #region UI
        private string defaultString = string.Empty;

        private string defaultUIContainerString = string.Empty;

        private string defaultUIHeadShotString = string.Empty;

        private string defaultUIModString = string.Empty;

        private string GetReplacedString(string victimName, string initiatorName, string weaponName, string[] weaponMods, bool isHeadShot, string distance, double victimWidth, double initiatorWidth)
        {
            string killString = GetUIString();
            double iconsOffset = 0.0;

            if (Configuration.ShowHeadShots && isHeadShot)
            {
                string headShotString = GetUIHeadShotString();
                iconsOffset += 21.0;
                headShotString = headShotString.Replace($"{{WeaponImage}}", GetImage("headshot", 16));
                headShotString = headShotString.Replace($"{{WeaponOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset}");
                headShotString = headShotString.Replace($"{{WeaponOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18}");
                headShotString = headShotString.Substring(1, headShotString.Length - 2);
                headShotString = "," + headShotString;
                killString = killString.Insert(killString.Length - 1, headShotString);
            }

            if (Configuration.ShowWeaponMods && weaponMods.Length > 0)
            {
                foreach (var weaponMod in weaponMods)
                {
                    string weaponModString = GetUIModString();
                    iconsOffset += 18.0;
                    weaponModString = weaponModString.Replace($"{{WeaponImage}}", GetImage(weaponMod, 16));
                    weaponModString = weaponModString.Replace($"{{WeaponOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 6}");
                    weaponModString = weaponModString.Replace($"{{WeaponOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18}");
                    weaponModString = weaponModString.Substring(1, weaponModString.Length - 2);
                    weaponModString = "," + weaponModString;
                    killString = killString.Insert(killString.Length - 1, weaponModString);
                }
            }

            ItemDefinition item;
            if (ItemManager.itemDictionaryByName.TryGetValue(weaponName, out item) == false)
            {
                weaponName = "skull.human";
            }

            killString = killString.Replace($"{{MainOffsetMinX}}", $"-{victimWidth + initiatorWidth + iconsOffset + 18 + 15 + 6 + 50}");
            killString = killString.Replace($"{{InitiatorName}}", initiatorName);
            killString = killString.Replace($"{{InitiatorOffsetMaxX}}", initiatorWidth.ToString());
            killString = killString.Replace($"{{VictimName}}", victimName);
            killString = killString.Replace($"{{VictimOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5}");
            killString = killString.Replace($"{{VictimOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth}");
            killString = killString.Replace($"{{DistanceOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth}");
            killString = killString.Replace($"{{DistanceOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth + 60}");
            killString = killString.Replace($"{{DistanceM}}", distance + "m");
            killString = killString.Replace($"{{DistanceMOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth}");
            killString = killString.Replace($"{{DistanceMOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth + 60}");
            killString = killString.Replace($"{{WeaponImage}}", GetImage(weaponName, 16));
            killString = killString.Replace($"{{WeaponOffsetMinX}}", $"{5 + initiatorWidth}");
            killString = killString.Replace($"{{WeaponOffsetMaxX}}", $"{5 + initiatorWidth + 18}");


            return killString;
        }

        private string GetUIContainerString()
        {
            if (string.IsNullOrEmpty(defaultUIContainerString))
            {
                defaultUIContainerString = CreateUIContainerString();
            }

            return defaultUIContainerString;
        }

        private string GetUIString()
        {
            if (string.IsNullOrEmpty(defaultString))
            {
                defaultString = CreateUIString();
            }

            return defaultString;
        }

        private string GetUIHeadShotString()
        {
            if (string.IsNullOrEmpty(defaultUIHeadShotString))
            {
                defaultUIHeadShotString = CreateUIHeadShotString();
            }

            return defaultUIHeadShotString;
        }

        private string GetUIModString()
        {
            if (string.IsNullOrEmpty(defaultUIModString))
            {
                defaultUIModString = CreateUIModString();
            }

            return defaultUIModString;
        }

        private string CreateUIContainerString()
        {
            CuiElementContainer Container = new CuiElementContainer();
            Container.Add(new CuiElement
            {
                Name = "DeathMessages",
                Parent = "Hud",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "1 1",
                            AnchorMax = "1 1",
                        }
                    }
            });

            return Container.ToJson();
        }

        private string CreateUIString()
        {
            CuiElementContainer Container = new CuiElementContainer();
            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.[ValueRemoved]",
                Parent = "DeathMessages",
                Components =
                {
                    new CuiImageComponent
                    {
                        FadeIn = 1337f,
                        Color = HexToRustFormat(Configuration.UI.BackgroundColor),
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = $"{{MainOffsetMinX}} {{MainOffsetMinY}}",
                        OffsetMax = $"-6 {{MainOffsetMaxY}}",
                    }
                }
            });
            Container.Add(new CuiElement
            {
                Name = "DeathMessages.Initiator",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1337f,
                                Text = $"<color=#FFFFFF>{{InitiatorName}}</color>",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = $"5 -10",
                                OffsetMax = $"{{InitiatorOffsetMaxX}} 10",
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                                Distance = "-0.5 0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = "DeathMessages.Victim",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1337f,
                                Text = $"<color=#FFFFFF>{{VictimName}}</color>",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = $"{{VictimOffsetMinX}} -10",
                                OffsetMax = $"{{VictimOffsetMaxX}} 10",
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                                Distance = "-0.5 0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.Distance",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    FadeIn = 1337f,
                                    Color = HexToRustFormat(Configuration.UI.DistanceColor),
                                    Material = "assets/icons/iconmaterial.mat",
                                    Sprite = $"assets/icons/subtract.png",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{DistanceOffsetMinX}} -30",
                                    OffsetMax = $"{{DistanceOffsetMaxX}} 30",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = "DeathMessages.DistanceM",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1337f,
                                Text = $"<color=#404040>{{DistanceM}}</color>",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = $"{{DistanceMOffsetMinX}} -10",
                                OffsetMax = $"{{DistanceMOffsetMaxX}} 10",
                            },
                            new CuiOutlineComponent
                            {
                                Color = "1 1 1 1",
                                Distance = "-0.5 0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.WeaponImage",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    FadeIn = 1337f,
                                    Png = $"{{WeaponImage}}",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{WeaponOffsetMinX}} -9",
                                    OffsetMax = $"{{WeaponOffsetMaxX}} 9",
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1",
                                    Distance = "-0.5 0.5"
                                }
                            }
            });

            return Container.ToJson();
        }

        private string CreateUIModString()
        {
            CuiElementContainer Container = new CuiElementContainer();

            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.WeaponMod",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    FadeIn = 1337f,
                                    Png = $"{{WeaponImage}}",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{WeaponOffsetMinX}} -6",
                                    OffsetMax = $"{{WeaponOffsetMaxX}} 6",
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1",
                                    Distance = "-0.5 0.5"
                                }
                            }
            });

            return Container.ToJson();
        }

        private string CreateUIHeadShotString()
        {
            CuiElementContainer Container = new CuiElementContainer();

            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.WeaponHeadshot",
                Parent = $"DeathMessages.[ValueRemoved]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    FadeIn = 1337f,
                                    Png = $"{{WeaponImage}}",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{WeaponOffsetMinX}} -9",
                                    OffsetMax = $"{{WeaponOffsetMaxX}} 9",
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1",
                                    Distance = "-0.5 0.5"
                                }
                            }
            });

            return Container.ToJson();
        }
        #endregion

        #region Help
        public static StringBuilder sb = new StringBuilder();
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }
        #endregion

        #region Chat&Console
        [ChatCommand("dm")]
        private void DMCmd(BasePlayer player, string cmd, string[] args)
        {
            if(args.Length > 0)
            {
                switch (args[0])
                {
                    case "on":
                        {
                            uiHide[player.userID] = new TemporaryBool()
                            {
                                Value = true,
                                Expire = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
                            };
                            SendReply(player, Configuration.ChatPrefix + lang.GetMessage("ENABLED", this, player.UserIDString));
                            break;
                        }
                    case "off":
                        {
                            uiHide[player.userID] = new TemporaryBool()
                            {
                                Value = false,
                                Expire = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
                            };
                            CuiHelper.DestroyUi(player, "DeathMessages");
                            SendReply(player, Configuration.ChatPrefix + lang.GetMessage("DISABLED", this, player.UserIDString));
                            break;
                        }
                }
                return;
            }

            SendReply(player, Configuration.ChatPrefix + lang.GetMessage("USAGE", this, player.UserIDString));
        }
        #endregion
    }
}
