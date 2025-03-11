
/*
 ########### README ####################################################
 #                                                                     #
 #   1. If you found a bug, please report them to developer!           #
 #   2. Don't edit that file (edit files only in CONFIG/LANG/DATA)     #
 #                                                                     #
 ########### CONTACT INFORMATION #######################################
 #                                                                     #
 #   Website: https://oxide-russia.ru/                                 #
 #   Discord: odin_ulveand_odin                                        #
 #   Mail: maksulrich@gmail.com                                        #
 #                                                                     #
 #######################################################################
*/

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using ProtoBuf;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Sentry Turrets", "Orange&WOLF SPIRIT", "2.4.1")]
    [Description("https://oxide-russia.ru/resources/1009/")]
    public class SentryTurrets : RustPlugin
    {
        #region Vars

        private const ulong skinID = 1587601905;
        private const string prefabSentry = "assets/content/props/sentry_scientists/sentry.scientist.static.prefab";
        private const string prefabSwitch = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        private const string itemName = "autoturret";
        private const string command = "sentryturrets.give";
        private const string itemDisplayName = "Sentry Turret";
        private const string ammoShortname = "ammo.rifle";
        private static Vector3 switchPosition = new Vector3(0, 2f, 1);
        
        
        #endregion

        #region Oxide Hooks

        private void Init()
        {
            cmd.AddConsoleCommand(command, this, nameof(cmdGiveConsole));
            
            if (config.spray == 0)
            {
                Unsubscribe(nameof(OnTurretTarget));
            }
        }

        private void OnServerInitialized()
        {
            CheckExistingTurrets();
        }

        private void Unload()
        {
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<TurretComponent>())
            {
                UnityEngine.Object.Destroy(entity);
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            CheckPlacement(plan, go);
        }

        private object CanPickupEntity(BasePlayer player, NPCAutoTurret entity)
        {
            CheckPickup(player, entity);
            return false;
        }

        private object OnTurretTarget(NPCAutoTurret turret, BasePlayer player)
        {
            if (player == null || turret.OwnerID == 0)
            {
                return null;
            }

            if (Vector3.Distance(turret.transform.position, player.transform.position) > config.range)
            {
                return true;
            }

            if (CanShootBullet(turret.inventory) == false)
            {
                return true;
            }

            return null;
        }

        private void OnSwitchToggle(ElectricSwitch entity, BasePlayer player)
        {
            var turret = entity.GetComponentInParent<NPCAutoTurret>();
            if (turret == null)
            {
                return;
            }
            
            if (turret.authorizedPlayers.Any(x => x.userid == player.userID) == false)
            {
                player.ChatMessage("No permission");
                entity.SetSwitch(!entity.IsOn());
                return;
            }

            if (entity.GetCurrentEnergy() < config.requiredPower)
            {
                player.ChatMessage("No power");
                entity.SetSwitch(!entity.IsOn());
                return;
            }
            
            turret.SetIsOnline(!turret.IsOn());
        }
        
        
        #endregion
        
        #region Commands

        private void cmdGiveConsole(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false)
            {
                SendReply(arg, "You don't have access to that command!");
                return;
            }

            var args = arg.Args;
            if (args == null || args?.Length == 0)
            {
                SendReply(arg, "Usage: sentryturrets.give {steamID / Name}");
                return;
            }

            var player = FindPlayer(args[0]);
            if (player != null)
            {
                GiveItem(player);
            }
        }

        #endregion

        #region Core
        
        private void CheckExistingTurrets()
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<NPCAutoTurret>().Where(x => x.OwnerID != 0);
            foreach (var turret in turrets)
            {
                SetupTurret(turret);
            }
        }

        private void CheckPickup(BasePlayer player, NPCAutoTurret entity)
        {
            var items = entity.inventory?.itemList.ToArray() ?? new Item[]{};
            
            foreach (var item in items)
            {
                player.GiveItem(item);
            }
            
            entity.Kill();
            GiveItem(player);
        }

        private void CheckPlacement(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity();
            if (entity == null)
            {
                return;
            }

            if (entity.skinID != skinID)
            {
                return;
            }

            var player = plan.GetOwnerPlayer();
            if (player == null)
            {
                return;
            }

            var transform = entity.transform;
            var position = transform.position;
            var rotation = transform.rotation;
            var owner = entity.OwnerID;
            NextTick(()=> { entity.Kill();});
            var turret = GameManager.server.CreateEntity(prefabSentry, position, rotation)?.GetComponent<NPCAutoTurret>();
            if (turret == null)
            {
                GiveItem(player);
                return;
            }
            
            turret.OwnerID = owner;
            turret.Spawn();
            turret.SetIsOnline(false);
            turret.SetPeacekeepermode(false);
            SetupTurret(turret);
        }

        private void SetupTurret(NPCAutoTurret turret)
        {
            turret.sightRange = config.range;
            turret.aimCone = config.aimCone;
            turret.inventory.capacity = 12;
            turret.inventory.canAcceptItem = null;
            turret.inventory.onlyAllowedItems = new []{ItemManager.FindItemDefinition(ammoShortname)};
            SetupProtection(turret);
            AuthorizeOthers(turret.OwnerID, turret);
            
            timer.Once(1f, () =>
            {
                CheckSwitch(turret);
                turret.GetOrAddComponent<TurretComponent>();
            });
            
            turret.SendNetworkUpdate();
        }

        private static void CheckSwitch(BaseEntity turret)
        {
            var entity = turret.GetComponentInChildren<ElectricSwitch>();
            if (entity == null)
            {
                var position = turret.transform.position + switchPosition;
                entity = GameManager.server.CreateEntity(prefabSwitch, position) as ElectricSwitch;
                if (entity == null)
                {
                    return;
                }
                
                entity.Spawn();
                entity.SetParent(turret, true);
            }
            
            entity.InitializeHealth(100 * 1000, 100 * 1000);
            entity.pickup.enabled = false;
            UnityEngine.Object.Destroy(entity.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(entity.GetComponent<GroundWatch>());
        }

        private static void SetupProtection(BaseCombatEntity turret)
        {
            var health = config.health;
            turret._maxHealth = health;
            turret.health = health;
            
            if (config.getDamage == true)
            {
                turret.baseProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
                turret.baseProtection.amounts = new float[]
                {
                    1,1,1,1,1,0.8f,1,1,1,0.9f,0.5f,
                    0.5f,1,1,0,0.5f,0,1,1,0,1,0.9f
                };
            }
        }

        private static Item CreateItem()
        {
            var item = ItemManager.CreateByName(itemName, 1, skinID);
            if (item == null)
            {
                return null;
            }
            
            item.name = itemDisplayName;
            return item;
        }
        
        private static void GiveItem(Vector3 position)
        {
            var item = CreateItem();
            if (item == null)
            {
                return;
            }
            
            item.Drop(position, Vector3.down);
        }

        private void GiveItem(BasePlayer player)
        {
            var item = CreateItem();
            if (item == null)
            {
                return;
            }
            
            player.GiveItem(item);
            Puts($"Turret was gave successfully to {player.displayName}");
        }

        private BasePlayer FindPlayer(string nameOrID)
        {
            var targets = BasePlayer.activePlayerList.Where(x => x.UserIDString == nameOrID || x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();
            
            if (targets.Count == 0)
            {
                Puts("There are no players with that Name or steamID!");
                return null;
            }

            if (targets.Count > 1)
            {
                Puts($"There are many players with that Name:\n{targets.Select(x => x.displayName).ToSentence()}");
                return null;
            }

            return targets[0];
        }

        private static bool CanShootBullet(ItemContainer inventory)
        {
            var items = inventory.itemList.Where(x => x.info.shortname == ammoShortname).ToArray();
            if (items.Length == 0)
            {
                return false;
            }

            var need = config.spray;
            var item = items[0];
            Consume(item, need);
            return true;
        }
        
        private static void Consume(Item item, int value)
        {
            if (item.amount > value)
            {
                item.amount -= value;
                item.MarkDirty();
            }
            else
            {
                item.GetHeldEntity()?.Kill();
                item.DoRemove();
            }
        }

        private void AuthorizeOthers(ulong userID, NPCAutoTurret entity)
        {
            if (config.authorizeOthers == true)
            {
                var team = RelationshipManager.ServerInstance.teams.FirstOrDefault(x => x.Value.members.Contains(userID)).Value;
                if (team?.members != null)
                {
                    foreach (var member in team.members)
                    {
                        entity.authorizedPlayers.Add(new PlayerNameID
                        {
                            userid = member,
                            username = "Player"

                        });
                    }
                }

                var friends = GetFriends(userID.ToString());
                if (friends != null)
                {
                    foreach (var friend in friends)
                    {
                        var friendID = (ulong)0;
                        if (ulong.TryParse(friend, out friendID) == true)
                        {
                            entity.authorizedPlayers.Add(new PlayerNameID
                            {
                                userid = friendID,
                                username = "Player"
                            });
                        }
                    }
                }
            }

            if (config.authorizeByTC == true)
            {
                var tc = entity.GetBuildingPrivilege();
                if (tc != null)
                {
                    foreach (var value in tc.authorizedPlayers)
                    {
                        entity.authorizedPlayers.Add(value);
                    }
                }
            }
    
            entity.authorizedPlayers = new HashSet<ProtoBuf.PlayerNameID>(entity.authorizedPlayers.Distinct());
        }

        #endregion
        
        #region Configuration 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Can get damage")]
            public bool getDamage = true;

            [JsonProperty(PropertyName = "Required power")]
            public int requiredPower = 0;

            [JsonProperty(PropertyName = "Authorize friends and team members")]
            public bool authorizeOthers = false;

            [JsonProperty(PropertyName = "Authorize tc members")]
            public bool authorizeByTC = false;

            [JsonProperty(PropertyName = "Amount of ammo for one spray (set to 0 for no-ammo mode)")]
            public int spray = 3;
            
            [JsonProperty(PropertyName = "Range (normal turret - 30")]
            public int range = 100;

            [JsonProperty(PropertyName = "Give back on ground missing")]
            public bool itemOnGroundMissing = true;

            [JsonProperty(PropertyName = "Health (normal turret - 1000)")]
            public float health = 1500;

            [JsonProperty(PropertyName = "Aim cone (normal turret - 4)")]
            public float aimCone = 2;
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
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private static void ValidateConfig()
        {
            
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Scripts

        private class TurretComponent : MonoBehaviour
        {
            private NPCAutoTurret entity;
            private ElectricSwitch eSwitch;

            private void Start()
            {
                entity = GetComponent<NPCAutoTurret>();
                eSwitch = GetComponentInChildren<ElectricSwitch>();
                InvokeRepeating(nameof(DoChecks), 5f, 5f);
            }

            private void DoChecks()
            {
                CheckPower();
                CheckGround();
            }

            private void CheckGround()
            {
                RaycastHit rhit;
                var cast = Physics.Raycast(entity.transform.position + new Vector3(0, 0.1f, 0), Vector3.down, out rhit, 4f, LayerMask.GetMask("Terrain", "Construction"));
                var distance = cast ? rhit.distance : 3f;

                if (distance > 0.2f)
                {
                    GroundMissing();
                }
            }

            private void CheckPower()
            {
                if (HasPower())
                {
                    return;
                }
                
                entity.SetIsOnline(false);
            }

            public bool HasPower()
            {
                return eSwitch != null && eSwitch.GetCurrentEnergy() >= config.requiredPower;
            }

            private void GroundMissing()
            {
                var position = entity.transform.position;
                entity.Kill();
                Effect.server.Run("assets/bundled/prefabs/fx/item_break.prefab", position);
                
                if (config.itemOnGroundMissing)
                {
                    GiveItem(position);
                }
            }
        }

        #endregion
        
        #region Friends Support

        [PluginReference] private Plugin Friends, RustIOFriendListAPI;

        private bool IsFriends(BasePlayer player1, BasePlayer player2)
        {
            return IsFriends(player1.userID, player2.userID);
        }

        private bool IsFriends(BasePlayer player1, ulong player2)
        {
            return IsFriends(player1.userID, player2);
        }

        private bool IsFriends(ulong player1, BasePlayer player2)
        {
            return IsFriends(player1, player2.userID);
        }

        private bool IsFriends(ulong id1, ulong id2)
        {
            var flag1 = Friends?.Call<bool>("AreFriends", id1, id2) ?? false;
            var flag2 = RustIOFriendListAPI?.Call<bool>("AreFriendsS", id1.ToString(), id2.ToString()) ?? false;
            return flag1 || flag2;
        }
        
        private string[] GetFriends(string playerID)
        {
            var flag1 = Friends?.Call<string[]>("GetFriends", playerID) ?? new string[]{};
            var flag2 = RustIOFriendListAPI?.Call<string[]>("GetFriends", playerID) ?? new string[]{};
            return flag1.Length > 0 ? flag1 : flag2;
        }

        #endregion
    }
}
