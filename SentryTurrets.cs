using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Sentry Turrets", "Rust", "2.2.4")]
    [Description("Leak By SparK")]
    public class SentryTurrets : RustPlugin
    {
        #region Vars

        private const ulong skinID = 1587601905;
        private const string prefabSentry = "assets/content/props/sentry_scientists/sentry.scientist.static.prefab";
        private const string itemName = "autoturret";

        #endregion Vars

        #region Oxide Hooks

        private void Init()
        {
            cmd.AddConsoleCommand("sentryturrets.give", this, nameof(cmdGiveConsole));

            if (config.spray == 0)
            {
                Unsubscribe("OnTurretTarget");
            }
        }

        private void OnServerInitialized()
        {
            CheckExistingTurrets();
        }

        private void Unload()
        {
            UnityEngine.Object.FindObjectsOfType<CheckGround>().ToList().ForEach(UnityEngine.Object.Destroy);
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            CheckPlacement(plan, go);
        }

        private object CanPickupEntity(BasePlayer player, NPCAutoTurret entity)
        {
            return CheckPickup(player, entity);
        }

        private object OnTurretTarget(NPCAutoTurret turret, BasePlayer player)
        {
            
            if (player != null && turret.OwnerID != 0 && CanShootBullet(turret, player))
            {
                return null;
            }

            return false;
        }

        #endregion Oxide Hooks

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

        [ChatCommand("t")]
        private void cmdToggleChat(BasePlayer player)
        {
            var entity = GetLookEntity(player);
            if (entity == null)
            {
                return;
            }

            if (entity.IsAuthed(player) == false)
            {
                return;
            }

            entity.SetIsOnline(entity.IsOn() == false);
        }

        #endregion Commands

        #region Core

        private object CheckPickup(BasePlayer player, NPCAutoTurret entity)
        {
            var items = entity.inventory?.itemList.ToList() ?? new List<Item>();

            foreach (var item in items)
            {
                player.GiveItem(item);
            }

            entity.Kill();
            GiveItem(player);
            return false;
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
            entity.Kill();

            var turret = GameManager.server.CreateEntity(prefabSentry, position, rotation).GetComponent<NPCAutoTurret>();
            turret.OwnerID = owner;
            turret.Spawn();
            turret.SetIsOnline(false);
            turret.SetPeacekeepermode(false);
            SetupTurret(turret);
        }


        public bool CanAcceptItem(Item item, int targetSlot)
        {
            return (item.info.itemid.Equals(-1211166256));
        }

        private void SetupTurret(NPCAutoTurret turret)
        {
            turret.sightRange = config.range;
            turret.aimCone = config.aimCone;
            turret.inventory.capacity = 12;
            turret.inventory.canAcceptItem = (Func<Item, int, bool>)Delegate.Combine(turret.inventory.canAcceptItem, new Func<Item, int, bool>(CanAcceptItem));
            SetupProtection(turret);
            AddComponent(turret);
            turret.SendNetworkUpdate();
        }

        private void CheckExistingTurrets()
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<NPCAutoTurret>().Where(x => x.OwnerID != 0);
            foreach (var turret in turrets)
            {
                SetupTurret(turret);
            }
        }

        private void SetupProtection(BaseCombatEntity turret)
        {
            var health = config.health;
            turret._maxHealth = health;
            turret.health = health;

            if (config.getDamage)
            {
                turret.baseProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
                turret.baseProtection.amounts = new float[]
                {
                    1,1,1,1,1,0.8f,1,1,1,0.9f,0.5f,
                    0.5f,1,1,0,0.5f,0,1,1,0,1,0.9f
                };
            }
        }

        private void AddComponent(BaseNetworkable entity)
        {
            entity.gameObject.GetOrAddComponent<CheckGround>();
            entity.SendNetworkUpdate();
        }

        private static Item CreateItem()
        {
            var item = ItemManager.CreateByName(itemName, 1, skinID);
            item.name = "Sentry Turret";
            return item;
        }

        private static void GiveItem(Vector3 position)
        {
            var item = CreateItem();
            item.Drop(position, Vector3.down);
        }

        private void GiveItem(BasePlayer player)
        {
            var item = CreateItem();
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

        private bool CanShootBullet(NPCAutoTurret nPCAutoTurret, BasePlayer player)
        {
            var items = nPCAutoTurret.inventory.itemList.Where(x => x.info.shortname == "ammo.rifle").ToList();
            if (items.Count == 0)
            {
                return false;
            }
            if (Vector3.Distance(player.transform.position, nPCAutoTurret.transform.position) > config.range)
            {
                return false;
            }
            var item = items[0];
            var need = config.spray;

            if (item.amount > need)
            {
                item.amount -= need;
            }
            else
            {
                item.GetHeldEntity()?.Kill();
                item.DoRemove();
            }

            return true;
        }

        private NPCAutoTurret GetLookEntity(BasePlayer player)
        {
            RaycastHit rHit;
            if (Physics.Raycast(player.eyes.HeadRay(), out rHit) == false) { return null; }
            return rHit.GetEntity()?.GetComponent<NPCAutoTurret>();
        }

        #endregion Core

        #region Configuration 1.1.0

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Can get damage")]
            public bool getDamage;

            [JsonProperty(PropertyName = "Amount of ammo for one spray (set to 0 for no-ammo mode)")]
            public int spray;

            [JsonProperty(PropertyName = "Range (normal turret - 30")]
            public int range;

            [JsonProperty(PropertyName = "Give back on ground missing")]
            public bool itemOnGroundMissing;

            [JsonProperty(PropertyName = "Health (normal turret - 1000)")]
            public float health;

            [JsonProperty(PropertyName = "Aim cone (normal turret - 4)")]
            public float aimCone;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                getDamage = true,
                spray = 3,
                range = 100,
                itemOnGroundMissing = true,
                health = 1500,
				/*aimTarge = %id%,*/
                aimCone = 2
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
            }
            catch
            {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion Configuration 1.1.0

        #region Scripts

        private class CheckGround : MonoBehaviour
        {
            private BaseEntity entity;

            private void Start()
            {
                entity = GetComponent<BaseEntity>();
                InvokeRepeating(nameof(Check), 5f, 5f);
            }

            private void Check()
            {
                if (entity == null)
                {
                    Destroy(this);
                    return;
                }

                RaycastHit rhit;
                var cast = Physics.Raycast(entity.transform.position + new Vector3(0, 0.1f, 0), Vector3.down, out rhit, 4f, LayerMask.GetMask("Terrain", "Construction"));
                var distance = cast ? rhit.distance : 3f;

                if (distance > 0.2f)
                {
                    GroundMissing();
                }
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

        #endregion Scripts
    }
}