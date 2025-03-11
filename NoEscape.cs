using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoEscape", "OxideBro", "1.1.0")]
    class NoEscape : RustPlugin
    {
        [PluginReference] Plugin Clans, Friends, RustMap, VKBot, Duel;

        bool IsClanMember(ulong playerID, ulong targetID)
        {
            return (bool) (Clans?.Call("HasFriend", playerID, targetID) ?? false);
        }

        bool IsFriends(ulong playerID, ulong friendId)
        {
            return (bool) (Friends?.Call("AreFriends", playerID, friendId) ?? false);
        }

        List<ulong> GetMembersClan(ulong playerId)
        {
            if (Clans)
            {
                List<ulong> Members = new List<ulong>();
                if (Clans.ResourceId == 2087)
                {
                    var clan1tag = (string) Clans.Call("GetClanOf", playerId);
                    if (clan1tag != null)
                    {
                        var members = ((JObject) Clans.Call("GetClan", clan1tag))["members"].ToObject<List<ulong>>();
                        if (members != null) return members;
                    }

                    return null;
                }

                if (Clans.ResourceId == 14)
                {
                    var clanmates = Clans.Call("GetClanMembers", playerId) as List<string>;
                    if (clanmates != null) return clanmates.Select(ulong.Parse) as List<ulong>;
                }
            }

            return null;
        }

        private bool IsDuelPlayer(BasePlayer player)
        {
            if (Duel == null) return false;
            var dueler = Duel.Call("IsPlayerOnActiveDuel", player);
            if (dueler is bool) return (bool) dueler;
            return false;
        }

        DateTime LNT;

        class Raid
        {
            public HashSet<ulong> owners;
            public HashSet<ulong> raiders;
            public Vector3 pos;

            public Raid(List<ulong> owners, List<ulong> raiders)
            {
                this.owners = new HashSet<ulong>(owners);
                this.raiders = new HashSet<ulong>(raiders);
            }
        }

        class DamageTimer
        {
            public List<ulong> owners;
            public int seconds;

            public DamageTimer(List<ulong> owners, int seconds)
            {
                this.owners = owners;
                this.seconds = seconds;
            }
        }

        static DynamicConfigFile config;
        bool EnabledBuildingPrivilage = false;
        float radius = 50f;
        int blockTime = 120;
        int offlineOut = 1;
        int blockAttackTime = 10;
        bool blockAttack = false;
        int ownerBlockTime = 120;
        bool useDamageScale = false;
        bool useVK = false;
        bool blockOwner = true;
        bool friedsAPI = false;
        bool clansAPI = false;
        bool canBuild = true;
        bool canRemove = true;
        bool canRemoveStan = true;
        bool canKits = true;
        bool canRepair = true;
        bool canUpgrade = true;
        bool canTeleport = true;
        bool EnabledGUI = true;
        bool EnabledGUITimer = true;
        bool canTrade = true;
        bool canRec = true;
        bool CanBuilt = true;
        bool CanBuiltNoEscape = true;
        bool LadderBuilding = false;
        float offlineScale = 0.5f;
        bool MsgAttB = false;
        string MsgAtt = "photo-1_265827614";
        float offlineScaleFriendsClans = 0.5f;
        public List<string> WriteList = new List<string>();
        string GUITimerAnchormin = "0.352 0.1138889";
        string GUITimerAnchormax = "0.632 0.1462963";
        string GUISendAnchormin = "0 0.8619792";
        string GUISendAnchormax = "1 0.9166667";

        private string formatMessage =
            "Доброго времени суток.\nУведомляем Вас о том, что начался рейд Вашего имущества, который инициирован игроком {attacker}.";

        private void LoadDefaultConfig()
        {
            GetConfig("Основное", "Размер радиуса блокировки", ref radius);
            GetConfig("Основное",
                "Включить отключение блокировки при разрушении шкафа (BuildingPrivilage), если функция будет включена игроку не будет давать блокировку если разрушенный объект будет вне билдинг зоны",
                ref EnabledBuildingPrivilage);
            GetConfig("Основное", "Время блокировки атакующего", ref blockTime);
            GetConfig("Основное", "Блокировать игроков при нанесение урона (Блокировка инициатора и жертвы)",
                ref blockAttack);
            GetConfig("Основное", "Время блокировки при нанесение урона по игрокам (Блокировка инициатора и жертвы)",
                ref blockAttackTime);
            GetConfig("Основное", "Поддержка плагина Clans", ref clansAPI);
            GetConfig("Основное", "Блокировать хозяина строения, если он не в радиусе блокировки", ref blockOwner);
            GetConfig("Основное", "Поддержка плагина Friends", ref friedsAPI);
            GetConfig("Основное", "Время блокировки хозяина", ref ownerBlockTime);
            GetConfig("Основное", "Запретить установку штурмовых лестниц в радиусе зоны чужого шкафа", ref CanBuilt);
            GetConfig("GUI", "Включить GUI окно-оповещение о начале рейда (Текст вы сможете изменить в lang)",
                ref EnabledGUI);
            GetConfig("GUI", "Включить GUI окошко таймера рейд блока", ref EnabledGUITimer);
            GetConfig("GUI", "Окно таймера: AnchorMin", ref GUITimerAnchormin);
            GetConfig("GUI", "Окно таймера: AnchorMax", ref GUITimerAnchormax);
            GetConfig("GUI", "Окно оповещения: AnchorMin", ref GUISendAnchormin);
            GetConfig("GUI", "Окно оповещения: AnchorMax", ref GUISendAnchormax);
            GetConfig("Множитель", "Множитель урона если хозяина нет в сети (наносимый урон = урон*SCALE)",
                ref offlineScale);
            GetConfig("Множитель", "Множитель урона если хозяин друг или соклановец (наносимый урон = урон*SCALE)",
                ref offlineScaleFriendsClans);
            GetConfig("Множитель", "Использовать множитель урона", ref useDamageScale);
            GetConfig("VK", "Использовать оповещения о рейде с помощью VKBot", ref useVK);
            GetConfig("VK", "Сообщение оповещения о рейде дома", ref formatMessage);
            GetConfig("VK", "Частота оповещений оффлайн игрокам в ВК (в минутах)", ref offlineOut);
            GetConfig("VK", "Прикрепить к сообщению изображение?", ref MsgAttB);
            GetConfig("VK", "Ссылка на изображение, пример: photo - 1_265827614", ref MsgAtt);
            GetConfig("Блокировка", "Блокировать строительство", ref canBuild);
            var _WriteList = new List<object>() {"wall.external.high.stone", "barricade.metal"};
            GetConfig("Блокировка", "Белый список предметов какие можно строить при рейдблоке", ref _WriteList);
            WriteList = _WriteList.Select(p => p.ToString()).ToList();
            GetConfig("Блокировка", "Блокировать удаление построек (CanRemove)", ref canRemove);
            GetConfig("Блокировка", "Блокировать использование китов", ref canKits);
            GetConfig("Блокировка", "Блокировать ремонт построек (стандартный)", ref canRepair);
            GetConfig("Блокировка", "Блокировать улучшение построек (стандартное)", ref canUpgrade);
            GetConfig("Блокировка", "Блокировать телепорты", ref canTeleport);
            GetConfig("Блокировка", "Блокировать удаление построек (стандартное)", ref canRemoveStan);
            GetConfig("Блокировка", "Блокировать обмен между игроками (Trade)", ref canTrade);
            GetConfig("Блокировка", "Блокировать переработчик (Recycler)", ref canRec);
            GetConfig("Блокировка", "Блокировать установку штурмовых лесниц в рейд блоке", ref LadderBuilding);
            SaveConfig();
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T) Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        Dictionary<ulong, RaidBlock> timers = new Dictionary<ulong, RaidBlock>();

        public class RaidBlock
        {
            public BuildingPrivlidge building;
            public double Seconds;
            public bool Attack;
        }

        Dictionary<BuildingPrivlidge, Raid> raids = new Dictionary<BuildingPrivlidge, Raid>();
        List<DamageTimer> damageTimers = new List<DamageTimer>();
        private Dictionary<BasePlayer, DateTime> CooldownsAttackDic = new Dictionary<BasePlayer, DateTime>();
        private double CooldownAttack = 15f;
        private string PERM_IGNORE = "noescape.ignore";
        private string PERM_VK_NOTIFICATION = "noescape.vknotification";

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go == null || plan == null) return;
            if (!CanBuilt) return;
            var player = plan.GetOwnerPlayer();
            BaseEntity entity = go.ToBaseEntity();
            var targetLocation = player.transform.position + (player.eyes.BodyForward() * 4f);
            if (go.name == "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab" &&
                player.IsBuildingBlocked(targetLocation, new Quaternion(0, 0, 0, 0),
                    new Bounds(Vector3.zero, Vector3.zero)))
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    if (!LadderBuilding) return;
                }

                SendReply(player, Messages["BlockLadders"]);
                entity.Kill();
				player.inventory.GiveItem(ItemManager.CreateByItemID(-316250604));
            }
        }

        void OnServerInitialized()
        {
            PermissionService.RegisterPermissions(this, new List<string>() {PERM_IGNORE, PERM_VK_NOTIFICATION});
            LoadDefaultConfig();
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            timer.Every(1f, NoEscapeTimerHandle);
            timer.Every(1f, RaidZoneTimerHandle);
            LoadData();
            if (RustMap != null) InitImages();
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info, Item item)
        {
            if (!(entity is BuildingBlock) && !(entity is Door) &&
                !entity.ShortPrefabName.Contains("external.high.")) return;
            var player = info?.InitiatorPlayer;
            if (entity == null && info == null) return;
            if (player == null) return;
            if (player.userID == entity.OwnerID) return;
            var block = entity as BuildingBlock;
            if (block && block.grade <= 0) return;
            if (EnabledBuildingPrivilage && entity?.GetBuildingPrivilege() == null ||
                entity.GetBuildingPrivilege()?.buildingID == 0) return;
            InitImages();
            bool justCreated;
            var raid = GetRaid(entity.GetNetworkPosition(), out justCreated,
                EnabledBuildingPrivilage ? entity?.GetBuildingPrivilege() : null);
            if (PermissionService.HasPermission(player.userID, PERM_IGNORE)) return;
            if (!timers.ContainsKey(player.userID))
            {
                if (EnabledGUI)
                {
                    var p = BasePlayer.FindByID(entity.OwnerID);
                    DrawUI(p, info.Initiator?.ToPlayer().displayName, Messages["yourbuildingdestroyOwner"]);
                    timer.Once(5.0f, () => DestroyUI(p));
                }

                if (blockOwner)
                {
                    var p = BasePlayer.FindByID(entity.OwnerID);
                    SendReply(p, Messages["blockactive"], FormatTime(TimeSpan.FromSeconds(ownerBlockTime)));
                    SendReply(player, Messages["blockactiveAttacker"],
                        FormatTime(TimeSpan.FromSeconds(ownerBlockTime)));
                }
                else
                {
                    var p = BasePlayer.FindByID(entity.OwnerID);
                    SendReply(p, Messages["noblockowner"]);
                    SendReply(player, Messages["blockactiveAttacker"],
                        FormatTime(TimeSpan.FromSeconds(ownerBlockTime)));
                }
            }

            GetAroundPlayers(entity.GetNetworkPosition()).ForEach(p =>
                BlockPlayer(p, EnabledBuildingPrivilage ? entity.GetBuildingPrivilege() : null, "raid"));
            if (useVK)
            {
                if (!IsOnline(entity.OwnerID))
                {
                    SendOfflineMessage(entity.OwnerID, info.Initiator?.ToPlayer().displayName);
                }
            }

            if (clansAPI && Clans.Call("GetClanMembers", entity.OwnerID) != null)
            {
                bool sendRemoveOwnerMessage = false;
                var team = (Clans.Call("GetClanMembers", entity.OwnerID) as List<string>).Select(ulong.Parse);
                foreach (var uid in team.ToList())
                {
                    if (!raid.owners.Contains(uid))
                    {
                        var p = BasePlayer.FindByID(uid);
                        raid.owners.Add(uid);
                        if (useDamageScale)
                        {
                            if (justCreated && p && !sendRemoveOwnerMessage)
                            {
                                sendRemoveOwnerMessage = true;
                                damageTimers.Add(new DamageTimer(raid.owners.ToList(), 3600));
                                SendReply(player, Messages["DamageOnlineOwner"]);
                            }
                            else if ((justCreated && !p && !sendRemoveOwnerMessage) ||
                                     (!sendRemoveOwnerMessage && team.Last() == uid))
                            {
                                sendRemoveOwnerMessage = true;
                                SendReply(player, Messages["DamageNotOnlineOwner"]);
                            }
                        }
                    }
                }
            }
            else
            {
                if (!raid.owners.Contains(entity.OwnerID))
                {
                    var p = BasePlayer.FindByID(entity.OwnerID);
                    if (blockOwner)
                    {
                        raid.owners.Add(entity.OwnerID);
                    }

                    if (useDamageScale)
                    {
                        if (justCreated && p)
                        {
                            damageTimers.Add(new DamageTimer(raid.owners.ToList(), 3600));
                        }
                    }
                }
            }

            foreach (var owner in GetOwnersByOwner(entity.OwnerID, entity.GetNetworkPosition())) raid.owners.Add(owner);
        }

        bool IsOnline(ulong id)
        {
            foreach (BasePlayer active in BasePlayer.activePlayerList)
            {
                if (active.userID == id) return true;
            }

            return false;
        }

        Raid GetRaid(Vector3 pos, out bool justCreated, BuildingPrivlidge building = null)
        {
            justCreated = false;
            foreach (var raid in raids)
                if (Vector3.Distance(raid.Value.pos, pos) < 50)
                    return raid.Value;
            justCreated = true;
            var ownerraid = new Raid(new List<ulong>(), new List<ulong>()) {pos = pos};
            raids.Add(building == null ? new BuildingPrivlidge() : building, ownerraid);
            return ownerraid;
        }

        void BlockPlayer(BasePlayer player, BuildingPrivlidge building = null, string mode = "", bool owner = false)
        {
            if (player.IsSleeping()) return;
            if (player == null) return;
            if (mode == "raid")
            {
                if (!owner || !timers.ContainsKey(player.userID))
                {
                    var secs = owner ? ownerBlockTime : blockTime;
                    timers[player.userID] = new RaidBlock() {building = building, Seconds = secs, Attack = false};
                    SetCooldown(player, "raid", secs);
                    SaveData();
                }

                return;
            }

            if (mode == "attack")
            {
                var cooldown = GetCooldown(player, "raid");
                if (cooldown != 0)
                {
                    return;
                }

                if (!timers.ContainsKey(player.userID))
                {
                    player.ChatMessage(string.Format(Messages[owner ? "ownerhome" : "blockattackactive"],
                        FormatTime(TimeSpan.FromSeconds(owner ? blockAttackTime : blockAttackTime))));
                }

                if (!owner || !timers.ContainsKey(player.userID))
                {
                    var secs = owner ? blockAttackTime : blockAttackTime;
                    timers[player.userID] = new RaidBlock()
                        {building = null, Seconds = secs, Attack = building == null,};
                    SetCooldown(player, "attack", secs);
                    SaveData();
                }
            }
        }

        private string GetFormatTime(double seconds)
        {
            double minutes = Math.Floor((double) (seconds / 60));
            seconds -= (int) (minutes * 60);
            return string.Format("{0}:{1:00}", minutes, seconds);
        }

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0) result += $"{Format(time.Days, "дней", "дня", "день")} ";
            if (time.Hours != 0) result += $"{Format(time.Hours, "часов", "часа", "час")} ";
            if (time.Minutes != 0) result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";
            if (time.Seconds != 0) result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";
            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;
            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
            if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
            return $"{units} {form3}";
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
                DestroyUITimer(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (useDamageScale)
            {
                foreach (var raid in raids)
                    if (raid.Value.owners.Contains(player.userID) &&
                        raid.Value.owners.Count(p => BasePlayer.FindByID(p) != null) <= 1 &&
                        raid.Value.owners.Any(o => damageTimers.All(t => !t.owners.Contains(o))))
                    {
                        foreach (var raider in raid.Value.raiders)
                            BasePlayer.FindByID(raider)?.ChatMessage(Messages["OwnerEnterOnline"]);
                        break;
                    }
            }
        }

        private List<string> damageTypes;

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info, BaseCombatEntity victim)
        {
            try
            {
                var initiator = info?.InitiatorPlayer;
                var victimBP = entity.ToPlayer();
                if (entity == null) return;
                if (initiator == null) return;
                if (victimBP == null) return;
                if (initiator == victimBP) return;
                if (blockAttack)
                {
                    if (entity is BasePlayer && info.Initiator is BasePlayer)
                    {
                        if (!IsDuelPlayer(initiator) && entity != null && victimBP != null)
                        {
                            BlockPlayer(initiator, null, "attack");
                            BlockPlayer(victimBP, null, "attack");
                        }
                    }
                }

                if (info.WeaponPrefab is BaseProjectile) return;
                if (damageTimers.Any(p => p.owners.Contains(entity.OwnerID))) return;
                if (!(entity is BuildingBlock) && !(entity is Door)) return;
                if (entity.OwnerID == 0) return;
                if (useDamageScale)
                {
                    bool justCreated;
                    var raid = GetRaid(entity.GetNetworkPosition(), out justCreated,
                        EnabledBuildingPrivilage ? entity.GetBuildingPrivilege() : null);
                    bool SendMessages = false;
                    SendMessages = false;
                    if (CooldownsAttackDic.ContainsKey(initiator))
                    {
                        double seconds = CooldownsAttackDic[initiator].Subtract(DateTime.Now).TotalSeconds;
                        if (seconds >= 0)
                        {
                            SendMessages = true;
                        }
                    }

                    if (clansAPI)
                    {
                        bool sendRemoveOwnerMessage = false;
                        if (justCreated && initiator && !sendRemoveOwnerMessage)
                        {
                            if (IsClanMember(entity.OwnerID, initiator.userID))
                            {
                                damageTimers.Add(new DamageTimer(raid.owners.ToList(), 3600));
                                info.damageTypes.ScaleAll(offlineScaleFriendsClans);
                                sendRemoveOwnerMessage = true;
                                justCreated = false;
                                if (SendMessages == false)
                                {
                                    SendReply(initiator, Messages["isClanMember"]);
                                    SendMessages = true;
                                }

                                CooldownsAttackDic[initiator] = DateTime.Now.AddSeconds(CooldownAttack);
                                return;
                            }
                        }
                    }

                    if (friedsAPI)
                    {
                        bool sendRemoveOwnerMessage = false;
                        if (justCreated && initiator && !sendRemoveOwnerMessage)
                        {
                            if (IsFriends(entity.OwnerID, initiator.userID))
                            {
                                damageTimers.Add(new DamageTimer(raid.owners.ToList(), 3600));
                                sendRemoveOwnerMessage = true;
                                info.damageTypes.ScaleAll(offlineScaleFriendsClans);
                                if (SendMessages == false)
                                {
                                    SendReply(initiator, Messages["isFriendMember"]);
                                    SendMessages = true;
                                }

                                CooldownsAttackDic[initiator] = DateTime.Now.AddSeconds(CooldownAttack);
                                return;
                            }
                        }
                    }

                    if (clansAPI && GetMembersClan(entity.OwnerID) != null)
                    {
                        var team = GetMembersClan(entity.OwnerID);
                        if (team.Contains(initiator.userID)) return;
                        if (!team.Select(BasePlayer.FindByID).Any(p => p))
                        {
                            info.damageTypes.ScaleAll(offlineScale);
                            return;
                        }
                    }
                    else
                    {
                        bool sendRemoveOwnerMessage = false;
                        if (!BasePlayer.FindByID(entity.OwnerID))
                        {
                            info.damageTypes.ScaleAll(offlineScale);
                            damageTimers.Add(new DamageTimer(raid.owners.ToList(), 3600));
                            if (SendMessages == false)
                            {
                                SendReply(initiator, Messages["DamageNotOnlineOwner"]);
                                SendMessages = true;
                            }

                            CooldownsAttackDic[initiator] = DateTime.Now.AddSeconds(CooldownAttack);
                        }

                        if (justCreated && initiator && !sendRemoveOwnerMessage)
                        {
                            sendRemoveOwnerMessage = true;
                            justCreated = false;
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (player == null) return null;
            if (canUpgrade)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blockupgrade"], seconds));
                    return false;
                }
            }

            return null;
        }

        object OnStructureDemolish(BaseCombatEntity entity, BasePlayer player)
        {
            if (player == null) return null;
            if (canRemoveStan)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blockremove"], seconds));
                    return false;
                }
            }

            return null;
        }

        object OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (player == null) return null;
            if (canRepair)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blockrepair"], seconds));
                    return false;
                }
            }

            return null;
        }

        object CanUpgrade(BasePlayer player)
        {
            if (player == null) return null;
            if (canUpgrade)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blockupgrade"], seconds));
                    return false;
                }
            }

            return null;
        }

        readonly int cupboardMask = LayerMask.GetMask("Deployed");

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null) return null;
            if (canBuild)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    if (prefab.fullName.Contains("Twigs")) return null;
                    if (!LadderBuilding && prefab.fullName.Contains("ladder.wooden")) return null;
                    if (WriteList.Contains(prefab.CreateConstruction(target).ShortPrefabName)) return null;
                    SendReply(player, string.Format(Messages["blockbuld"], seconds));
                    if (prefab.fullName.Contains("cupboard.tool"))
                    {
                        BuildingPrivlidge privilege =
                            target.entity.GetBuildingPrivilege(target.entity.WorldSpaceBounds());
                        if (privilege != null) privilege.Kill();
                    }

                    return false;
                }
            }

            return null;
        }

        object CanTeleport(BasePlayer player)
        {
            if (player == null) return null;
            var cooldown = GetCooldown(player, "attack");
            if (cooldown > 0 && !player.IsAdmin)
            {
                return false;
            }

            var seconds = ApiGetTime(player.userID);
            return seconds > 0 ? string.Format(Messages["blocktp"], seconds) : null;
        }

        object CanTrade(BasePlayer player)
        {
            if (player == null) return null;
            if (canTrade)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blocktrade"], seconds));
                    return false;
                }
            }

            return null;
        }

        object canRedeemKit(BasePlayer player)
        {
            if (player == null) return null;
            if (canKits)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blockKits"], seconds));
                    return false;
                }
            }

            return null;
        }

        object CanRecycleCommand(BasePlayer player)
        {
            if (player == null) return null;
            if (canRec)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["blockrec"], seconds));
                    return false;
                }
            }

            return null;
        }

        object CanRemove(BasePlayer player, BaseEntity entity)
        {
            if (player == null) return null;
            if (canRemove)
            {
                var seconds = ApiGetTime(player.userID);
                if (seconds > 0)
                {
                    SendReply(player, string.Format(Messages["raidremove"], seconds));
                    return false;
                }
            }

            return null;
        }

        [ChatCommand("ne")]
        void cmdChatRaid(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            var seconds = ApiGetTime(player.userID);
            if (seconds != 0)
            {
                SendReply(player, string.Format(Messages["isblock"], FormatTime(TimeSpan.FromSeconds(seconds))));
                return;
            }

            if (seconds == 0)
            {
                SendReply(player, string.Format(Messages["noblock"]));
                return;
            }
        }

        private string TextReplace(string key, params KeyValuePair<string, string>[] replacements)
        {
            string message = key;
            foreach (var replacement in replacements)
                message = message.Replace($"{{{replacement.Key}}}", replacement.Value);
            return message;
        }

        void SendOfflineMessage(ulong id, string raidername)
        {
            if (!PermissionService.HasPermission(id, PERM_VK_NOTIFICATION)) return;
            var userVK = (string) VKBot?.Call("GetUserVKId", id);
            if (userVK == null) return;
            var LastNotice = (string) VKBot?.Call("GetUserLastNotice", id);
            if (LastNotice == null)
            {
                string text = TextReplace(formatMessage, new KeyValuePair<string, string>("attacker", raidername));
                VKBot?.Call("VKAPIMsg", text, MsgAtt, userVK, MsgAttB);
                string LastRaidNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                VKBot?.Call("VKAPISaveLastNotice", id, LastRaidNotice);
            }
            else
            {
                if (DateTime.TryParseExact(LastNotice, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out LNT))
                {
                    if (TimeLeft(LNT).TotalMinutes >= offlineOut)
                    {
                        string text = TextReplace(formatMessage,
                            new KeyValuePair<string, string>("attacker", raidername));
                        VKBot?.Call("VKAPIMsg", text, MsgAtt, userVK, MsgAttB);
                        string LastRaidNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        VKBot?.Call("VKAPISaveLastNotice", id, LastRaidNotice);
                    }
                }
                else
                {
                    LogToFile("error", $"[{DateTime.Now}] Ошибка обработки времени последнего оповещения игрока {id}",
                        this);
                    return;
                }
            }
        }

        void NoEscapeTimerHandle()
        {
            foreach (var uid in timers.Keys.ToList())
            {
                if (timers[uid].building == null && !timers[uid].Attack && EnabledBuildingPrivilage)
                    timers[uid].Seconds = 0;
                if (--timers[uid].Seconds <= 0)
                {
                    bool cont = false;
                    foreach (var raid in raids)
                        if (raid.Value.owners.Contains(uid))
                            cont = true;
                    if (cont) continue;
                    timers.Remove(uid);
                    if (EnabledGUITimer)
                        foreach (var player in BasePlayer.activePlayerList)
                            CuiHelper.DestroyUi(player, "noescape_main2");
                    BasePlayer.activePlayerList.Find(p => p.userID == uid)?.ChatMessage(Messages["blocksuccess"]);
                }
                else
                {
                    if (EnabledGUITimer) DrawUITimer(BasePlayer.activePlayerList.Find(p => p.userID == uid));
                }
            }

            for (int i = damageTimers.Count - 1; i >= 0; i--)
            {
                var rem = damageTimers[i];
                if (--rem.seconds <= 0)
                {
                    damageTimers.RemoveAt(i);
                    continue;
                }
            }
        }

        void RaidZoneTimerHandle()
        {
            List<BuildingPrivlidge> toRemove = new List<BuildingPrivlidge>();
            foreach (var raid in raids)
            {
                foreach (var player in GetAroundPlayers(raid.Value.pos))
                {
                    if (raid.Value.owners.Contains(player.userID))
                    {
                        BlockPlayer(player, raid.Key, "raid", true);
                    }
                }

                raid.Value.raiders.RemoveWhere(raider => !timers.ContainsKey(raider));
                if (raid.Value.raiders.Count <= 0)
                {
                    foreach (var owner in raid.Value.owners)
                    {
                        timers[owner] = new RaidBlock()
                            {building = raid.Key, Seconds = ownerBlockTime, Attack = raid.Key == null};
                        var p = BasePlayer.FindByID(owner);
                        if (p) SetCooldown(p, "raid", ownerBlockTime);
                        SaveData();
                    }

                    toRemove.Add(raid.Key);
                }
            }

            toRemove.ForEach(raid => raids.Remove(raid));
        }

        List<BasePlayer> GetAroundPlayers(Vector3 position)
        {
            var coliders = new List<BaseEntity>();
            Vis.Entities(position, radius, coliders, Rust.Layers.Server.Players);
            return coliders.OfType<BasePlayer>().ToList();
        }

        List<ulong> GetOwnersByOwner(ulong owner, Vector3 position, ulong pIayerid = 8142848)
        {
            var coliders = new List<BaseEntity>();
            Vis.Entities(position, radius, coliders, Rust.Layers.Server.Deployed);
            var codelocks = coliders.OfType<BoxStorage>().Select(s => s.GetSlot(BaseEntity.Slot.Lock))
                .OfType<CodeLock>().ToList();
            var owners = new HashSet<ulong>();
            var reply = 1;
            if (reply == 0)
            {
            }

            foreach (var codelock in codelocks)
            {
                var whitelist = codelock.whitelistPlayers;
                if (whitelist == null) continue;
                if (!whitelist.Contains(owner)) continue;
                foreach (var uid in whitelist)
                    if (uid != owner)
                        owners.Add(uid);
            }

            return owners.ToList();
        }

        string DrawGUIAnno =
            "[{\"name\":\"noescape_background\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0.5261458 0.1478219 0.1478219 0.4666667\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"noescape_text\",\"parent\":\"noescape_background\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{0}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.3513073 0 0 0.5438426\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]}]";

        void DrawUI(BasePlayer player, string name, string messag)
        {
            DestroyUI(player);
            CuiHelper.AddUi(player,
                DrawGUIAnno.Replace("{0}", messag.ToString()).Replace("{1}", name.ToString())
                    .Replace("{min}", GUISendAnchormin.ToString()).Replace("{max}", GUISendAnchormax.ToString()));
        }

        string UI =
            "[{\"name\":\"noescape_main2\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0.1119509\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\",\"offsetmax\":\"0 0\"}]},{\"name\":\"noescape_main3\",\"parent\":\"noescape_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0.5937501 1 0.7485165\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"{amax} 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"noescape_main4\",\"parent\":\"noescape_main2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{0}\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 1\",\"distance\":\"0.5 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.001751313 0\",\"anchormax\":\"1.001751 1\",\"offsetmax\":\"0 0\"}]}]";

        void DrawUITimer(BasePlayer player)
        {
            if (player != null && timers.ContainsKey(player.userID))
            {
                var time = GetFormatTime(ApiGetTime(player.userID));
                CuiHelper.DestroyUi(player, "noescape_main2");
                var amax = Convert.ToDouble((float) Math.Min(ApiGetTime(player.userID), blockTime) / blockTime);
                CuiHelper.AddUi(player,
                    UI.Replace("{0}", Messages["guitimertext"].Replace("{0}", time.ToString()).ToString())
                        .Replace("{min}", GUITimerAnchormin).Replace("{max}", GUITimerAnchormax)
                        .Replace("{amax}", amax.ToString()));
            }
        }

        void DestroyUITimer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "noescape_main2");
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "noescape_background");
        }

        DynamicConfigFile cooldownsFile = Interface.Oxide.DataFileSystem.GetFile("NoEscapeCooldown");

        private class Cooldown
        {
            public ulong UserId;
            public double Expired;
            [JsonIgnore] public Action OnExpired;
        }

        private static Dictionary<string, List<Cooldown>> cooldowns;

        internal static void Service()
        {
            var time = GrabCurrentTime();
            List<string> toRemove = new List<string>();
            foreach (var cd in cooldowns)
            {
                var keyCooldowns = cd.Value;
                List<string> toRemoveCooldowns = new List<string>();
                for (var i = keyCooldowns.Count - 1; i >= 0; i--)
                {
                    var cooldown = keyCooldowns[i];
                    if (cooldown.Expired < time)
                    {
                        cooldown.OnExpired?.Invoke();
                        keyCooldowns.RemoveAt(i);
                        ;
                    }
                }

                if (keyCooldowns.Count == 0) toRemove.Add(cd.Key);
            }

            toRemove.ForEach(p => cooldowns.Remove(p));
        }

        public static void SetCooldown(BasePlayer player, string key, int seconds, Action onExpired = null)
        {
            List<Cooldown> cooldownList;
            if (!cooldowns.TryGetValue(key, out cooldownList)) cooldowns[key] = cooldownList = new List<Cooldown>();
            cooldownList.Add(new Cooldown()
                {UserId = player.userID, Expired = GrabCurrentTime() + (double) seconds, OnExpired = onExpired});
        }

        public static int GetCooldown(BasePlayer player, string key)
        {
            List<Cooldown> source = new List<Cooldown>();
            if (cooldowns.TryGetValue(key, out source))
            {
                Cooldown cooldown =
                    source.FirstOrDefault<Cooldown>(
                        (Func<Cooldown, bool>) (p => (long) p.UserId == (long) player.userID));
                if (cooldown != null) return (int) (cooldown.Expired - GrabCurrentTime());
            }

            return 0;
        }

        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        private Image raidhomeImage;

        void OnMapInitialized()
        {
            InitImages();
        }

        TimeSpan TimeLeft(DateTime time)
        {
            return DateTime.Now.Subtract(time);
        }

        void InitImages()
        {
            try
            {
                if (plugins.Exists("RustMap"))
                {
                    uint raidhomeCRC = uint.Parse((string) RustMap?.Call("RaidHomePng"));
                    raidhomeImage = (Bitmap) (new ImageConverter().ConvertFrom(FileStorage.server.Get(raidhomeCRC,
                        FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID)));
                }
            }
            catch
            {
            }
        }

        double ApiGetTime(ulong player) => timers.ContainsKey(player) ? timers[player].Seconds : 0;

        List<Vector3> ApiGetOwnerRaidZones(ulong uid)
        {
            return new List<Vector3>(raids.Where(p => p.Value.owners.Contains(uid)).Select(r => r.Value.pos));
        }

        bool IsRaidBlock(ulong id)
        {
            if (timers.ContainsKey(id)) return true;
            return false;
        }

        void OnServerSave()
        {
            cooldownsFile.WriteObject(cooldowns);
        }

        void LoadData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("NoEscapeCooldown", new Dictionary<string, FileInfo>());
            cooldowns = cooldownsFile.ReadObject<Dictionary<string, List<Cooldown>>>() ??
                        new Dictionary<string, List<Cooldown>>();
        }

        void SaveData()
        {
            cooldownsFile.WriteObject(cooldowns);
        }

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"blocksuccess", "Блок деактивирован. Функции разблокированы"},
            {"guitimertext", "<b>Блокировка:</b> Осталось {0}"},
            {"noblockowner", "Ваше строение разрушено! Летите на защиту"},
            {
                "blockactive",
                "Ваше строение разрушено, активирован рейд блок на <color=#ECBE13>{0}</color>\nНекоторые функции временно недоступны."
            },
            {
                "blockactiveAttacker",
                "Вы уничтожили чужой объект, активирован рейд блок на <color=#ECBE13>{0}</color>\nНекоторые функции временно недоступны."
            },
            {"blockattackactive", "Включен режим боя, активирован блок на {0}! Некоторые функции временно недоступны."},
            {"blockrepair", "Вы не можете ремонтировать строения во время рейда, подождите {0} сек."},
            {
                "isblock",
                "NoEscape by <color=#ECBE13><size=15>RustPlugin.ru</size></color>\nУ вас есть активный рейд блок!\nДо окончания: <color=#ECBE13>{0}</color>"
            },
            {"noblock", "NoEscape by <color=#ECBE13><size=15>RustPlugin.ru</size></color>\nУ вас нету рейд блока :)"},
            {"blocktp", "Вы не можете использовать телепорт во время рейда, подождите {0} сек."},
            {"blockremove", "Вы не можете удалить постройки во время рейда, подождите {0} сек."},
            {"blockupgrade", "Вы не можете использовать автоулучшение построек во время рейда, подождите {0} сек."},
            {"blockKits", "Вы не можете использовать киты во время рейда, подождите {0} сек."},
            {"blockbuld", "Вы не можете строить во время рейда, подождите {0} сек."},
            {"yourbuildingdestroy", "Вас, или дом Вашего соклана рейдят! Добавлена отметка на карту"},
            {"yourbuildingdestroyOwner", "Внимание! Ваш дом рейдит игрок {1}! Добавлена отметка на карту"},
            {"isClanMember", "Внимание! Данное строение пренадлежит Вашему соклановцу! Урон уменьшен"},
            {"isFriendMember", "Внимание! Данное строение пренадлежит Вашему другу! Урон уменьшен"},
            {"removerestrict", "Хозяев нет в сети! Ремув запрещён!"},
            {"ownerhome", "Вы рядом со своим домом, который рейдят!\nРейдблок активирован на {0}!"},
            {"raidremove", "Вы не можете удалять обьекты во время рейда, подождите {0} сек."},
            {"blockrec", "Вы не можете использовать переработчик во время рейда, подождите {0} сек."},
            {"ownernotonline", "Владельцов нет в сети, ремув недоступен!"},
            {"DamageOnlineOwner", "Один их хозяев постройки сейчас в сети.\nУрон по объектам владельца стандартный"},
            {
                "DamageNotOnlineOwner",
                "Не одного владельца постройки нет в сети. \nУрон по объектам владельца уменьшен в 2 раза!"
            },
            {"OwnerEnterOnline", "Хозяин постройки зашел в игру.\nУрон по объектам владельца стандартный!"},
            {"blocktrade", "Вы не можете использовать обмен во время рейда, подождите {0} сек."},
            {"BlockLadders", "Вы не можете установить штурмовую лестницу в зоне действия чужого шкафа"}
        };

        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(ulong uid, string permissionName)
            {
                return !string.IsNullOrEmpty(permissionName) &&
                       permission.UserHasPermission(uid.ToString(), permissionName);
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");
                foreach (var permissionName in permissions.Where(permissionName =>
                    !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
    }
}                        