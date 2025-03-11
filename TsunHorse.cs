using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.Text;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("TsunHorse", "k1lly0u", "3.0.1", ResourceId = 0)]
    class TsunHorse : RustPlugin
    {
        #region Fields  
        private StoredData storedData;
        private DynamicConfigFile data;

        private List<Controller> controllers = new List<Controller>();
        private static TsunHorse ins = null;

        const string chairPrefab = "assets/prefabs/vehicle/seats/passengerchair.prefab";
        const string animalPrefab = "assets/rust.ai/agents/{0}/{0}.prefab";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("tsunhorse.stophorse", this);
            permission.RegisterPermission("tsunhorse.ridehorse", this);
            permission.RegisterPermission("tsunhorse.spawnhorse", this);
            lang.RegisterMessages(Messages, this);

            data = Interface.Oxide.DataFileSystem.GetFile("tsunhorse_cooldowns");

            ins = this;

            LoadData();
        }

        private void OnServerSave() => SaveData();
     
        private void Unload()
        {
            for (int i = controllers.Count - 1; i >= 0; i--)
            {
                Controller controller = controllers[i];
                controller.Player.DismountObject();
                UnityEngine.Object.Destroy(controller);
            }
            ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return null;

            BaseNpc baseNpc = entity.GetComponent<BaseNpc>();
            if (baseNpc == null)
                return null;

            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController == null)
            {
                if (configData.Command.Invulnerable)
                {
                    if (!baseNpc.GetNavAgent.enabled && (!baseNpc.Entity?.GetComponent<Apex.AI.Components.UtilityAIComponent>()?.enabled ?? false))
                        return true;
                }
                return null;
            }

            if (configData.Settings.Invulnerable)
                return true;

            return false;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return;

            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
            {
                if (animalController.Human != null)
                {
                    animalController.Human.Player.DismountObject();
                    UnityEngine.Object.Destroy(animalController.Human);
                }
                UnityEngine.Object.Destroy(animalController);
                return;
            }

            Controller controller = entity.GetComponent<Controller>();
            if (controller != null)
            {
                controller.Player.DismountObject();
                UnityEngine.Object.Destroy(controller);
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || !permission.UserHasPermission(player.UserIDString, "tsunhorse.ridehorse") || player.isMounted) return;

            if (input.WasJustPressed(BUTTON.USE))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 1.5f))
                {
                    BaseNpc baseNpc = hit.GetEntity()?.GetComponent<BaseNpc>();
                    if (baseNpc == null)
                        return;

                    if (!configData.Settings.AllowedTypes.Any(x => x.Contains(baseNpc.ShortPrefabName, CompareOptions.IgnoreCase)))
                    {
                        SendReply(player, msg("Warning.InvalidType", player.UserIDString));
                        return;
                    }

                    if (baseNpc.health <= 1)
                    {
                        SendReply(player, msg("Warning.NearDeath", player.UserIDString));
                        return;
                    }

                    if (baseNpc.GetComponent<AnimalController>())
                    {
                        SendReply(player, msg("Warning.InUse", player.UserIDString));
                        return;
                    }

                    Controller controller = player.gameObject.AddComponent<Controller>();
                    controller.MountToNpc(baseNpc);
                    controllers.Add(controller);
                }
            }            
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller == null)
                return;

            controllers.Remove(controller);
            UnityEngine.Object.Destroy(controller);
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller == null)
                return;
           
            controllers.Remove(controller);
            UnityEngine.Object.Destroy(controller);
        }

        private object CanAnimalAttack(BaseNpc entity, BasePlayer player)
        {
            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
                return false;
            return null;
        }

        private object CanNPCEat(BaseNpc entity, BaseEntity target)
        {
            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
                return false;
            return null;
        }

        private object OnNpcTarget(BaseNpc entity, BaseEntity target)
        {
            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
                return false;
            return null;
        }
        #endregion

        #region Functions
        private static double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            int days = dateDifference.Days;
            int hours = dateDifference.Hours;
            int mins = dateDifference.Minutes;
            int secs = dateDifference.Seconds;

            if (days > 0)
                return string.Format("{0:00}d:{1:00}h:{2:00}m:{3:00}s", days, hours, mins, secs);
            else if (hours > 0)
                return string.Format("{0:00}h:{1:00}m:{2:00}s", hours, mins, secs);
            else if (mins > 0)
                return string.Format("{0:00}m:{1:00}s", mins, secs);
            else return string.Format("{0}s", secs);
        }

        private bool SpawnAnimal(string type, Vector3 position, out BaseNpc baseNpc)
        {
            Vector3 point;
            if (FindPointOnNavmesh(position, 1, out point))
            {
                baseNpc = InstantiateEntity(string.Format(animalPrefab, type), point) as BaseNpc;
                if (baseNpc == null)
                    return false;

                baseNpc.Spawn();                
                return true;
            }
            baseNpc = null;
            return false;
        }

        private BaseEntity InstantiateEntity(string type, Vector3 position)
        {
            var gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(type), position, new Quaternion());
            gameObject.name = type;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }

        private bool FindPointOnNavmesh(Vector3 center, float range, out Vector3 result)
        {
            for (int i = 0; i < 30; i++)
            {
                Vector3 randomPoint = center + UnityEngine.Random.insideUnitSphere * range;
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }
        #endregion

        #region Controller
        private class Controller : MonoBehaviour
        {
            public BasePlayer Player { get; private set; }
            public AnimalController Animal { get; private set; }

            public ConfigData.RidingSettings config;

            private float acceleration = 0f;
            private float steering = 0f;
            private bool sprint = false;

            private Vector3 targetPosition;

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                enabled = false;

                config = ins.configData.Settings;
                InvokeHandler.InvokeRepeating(this, BlockTargeting, 5f, 5f);                
            }

            private void BlockTargeting()
            {
                Animal.Npc.BlockEnemyTargeting(5f);
                Animal.Npc.BlockFoodTargeting(5f);
                
                InvokeHandler.Invoke(this, BlockTargeting, 5f);
            }

            public void FixedUpdate()
            {
                if (Player == null || Animal == null || Animal.Npc == null)
                    return;
               
                Animal.Npc.AutoBraking = false;               

                Animal.Npc.IsDormant = false;
                Animal.Npc.IsStopped = false;

                if (acceleration == 0 && steering == 0)
                {
                    Animal.Npc.IsStopped = true;
                    Animal.Npc.SetFact(BaseNpc.Facts.CanTargetFood, 0, true, true);
                    Animal.Npc.ToSpeedEnum(0);

                    Animal.TargetSpeed = 0;
                }

                Animal.TargetSpeed = sprint ? Animal.stats.Sprint : Animal.stats.Walk;

                Animal.Npc.SetFact(BaseNpc.Facts.Speed, (byte)(sprint ? BaseNpc.SpeedEnum.Run : BaseNpc.SpeedEnum.Walk), true, true);

                Transform trans = Animal.Npc.transform;
                
                Vector3 steerPos = ((trans.rotation * Vector3.right) * steering) * 0.75f;
                Vector3 accelPos = ((trans.rotation * Vector3.forward) * acceleration) * 5f;

                targetPosition = trans.position + steerPos + (acceleration > 0 ? accelPos : trans.forward);

                Animal.Npc.UpdateDestination(targetPosition);
            }           

            public void LateUpdate()
            {
                InputState input = Player.serverInput;

                if (input.WasJustPressed(BUTTON.JUMP))
                {
                    enabled = false;
                    Destroy(this);
                    return;
                }

                sprint = input.IsDown(BUTTON.SPRINT);
                acceleration = input.IsDown(BUTTON.FORWARD) ? 1f : 0f;
                steering = input.IsDown(BUTTON.LEFT) ? -1f : input.IsDown(BUTTON.RIGHT) ? 1f : 0f;

                if (input.WasJustPressed(BUTTON.FIRE_THIRD))
                    Player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, !Player.HasPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode));
            }

            public void OnDestroy()
            {
                if (Player != null)
                {
                    if (Player.isMounted)
                        Player.DismountObject();                    
                    Player.EnsureDismounted();
                    Player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
                    Player.MovePosition(Animal.transform.position + (Animal.transform.right * 1.5f));
                }

                if (ins.controllers.Contains(this))
                    ins.controllers.Remove(this);

                Destroy(Animal);
            }

            public void MountToNpc(BaseNpc baseNpc)
            {
                Animal = baseNpc.gameObject.AddComponent<AnimalController>();
                Animal.CreateMountable(this);
                Player.MountObject(Animal.Mountable);
                Animal.Npc.Resume();
                enabled = true;

                Player.ChatMessage(msg("Help.Controls1", Player.UserIDString));

                BlockTargeting();

                InvokeHandler.InvokeRepeating(this, UpdatePosition, 0, 2);
            }

            private void UpdatePosition()
            {
                Player.transform.position = Animal.Mountable.transform.position;                
            }
        }

        private class AnimalController : MonoBehaviour
        {
            public BaseNpc Npc { get; private set; }
            public BaseMountable Mountable { get; private set; }
            public Controller Human { get; private set; }

            public float TargetSpeed { get; set; }

            public ConfigData.SpeedSettings stats;            

            private void Awake()
            {
                Npc = GetComponent<BaseNpc>();
                enabled = false;
                Npc.Pause();
                Npc.NewAI = false;

                Npc.CancelInvoke(Npc.TickAi);
                AnimalSensesLoadBalancer.animalSensesLoadBalancer.Remove(Npc);

                stats = ins.configData.Speeds[Npc.ShortPrefabName];

                Npc.InvokeRandomized(new Action(this.TickAi), 0.1f, 0.1f, 0.00500000035f);
            }

            public void CreateMountable(Controller controller)
            {
                this.Human = controller;

                Mountable = GameManager.server.CreateEntity(chairPrefab, Npc.transform.position, new Quaternion()) as BaseMountable;
                Mountable.enableSaving = false;
                Mountable.isMobile = true;
                Mountable.skinID = 1169930802;
                Mountable.maxMountDistance = 1.5f;

                Mountable.Spawn();
                Mountable.isMobile = true;
                Destroy(Mountable.GetComponent<DestroyOnGroundMissing>());
                Destroy(Mountable.GetComponent<GroundWatch>());
                Mountable.GetComponent<MeshCollider>().convex = true;

                Mountable.SetParent(Npc);

                KeyValuePair<Vector3, Vector3> offset;
                if (ins.mountingPositions.TryGetValue(Npc.ShortPrefabName, out offset))
                {
                    Mountable.transform.localPosition = offset.Key;
                    Mountable.transform.localEulerAngles = offset.Value;
                }
                Npc.Resume();
            }

            private void TickAi()
            {
                if (TerrainMeta.WaterMap == null)
                {
                    Npc.wasSwimming = false;
                    Npc.swimming = false;
                    Npc.waterDepth = 0f;
                }
                else
                {
                    Npc.waterDepth = TerrainMeta.WaterMap.GetDepth(Npc.ServerPosition);
                    Npc.wasSwimming = Npc.swimming;
                    Npc.swimming = Npc.waterDepth > Npc.Stats.WaterLevelNeck * 0.25f;
                }

                Npc.TickNavigation();
                
                if (Npc.GetNavAgent.enabled)
                {
                    TickSpeed();                    
                }
            }

            private void TickSpeed()
            {
                float speed = TargetSpeed;               
                
                float single = Mathf.Min(Npc.NavAgent.speed / stats.Sprint, 1f);
                Vector3 vector3 = Npc.transform.forward;
                Vector3 navAgent = Npc.NavAgent.nextPosition - Npc.ServerPosition;
                float single1 = 1f - 0.9f * Vector3.Angle(vector3, navAgent.normalized) / 180f * single * single;
                speed *= single1;
                Npc.NavAgent.speed = Mathf.Lerp(Npc.NavAgent.speed, speed, 0.5f);
                Npc.NavAgent.angularSpeed = 1f * (1.1f - single);
                Npc.NavAgent.acceleration = Npc.Stats.Acceleration;
            }

            private void OnDestroy()
            {               
                if (Mountable != null && !Mountable.IsDestroyed)
                {
                    if (Mountable.IsMounted())
                        Mountable.DismountAllPlayers();

                    Mountable.Kill(BaseNetworkable.DestroyMode.None);
                }

                if (Npc != null && !Npc.IsDestroyed)
                {
                    AnimalSensesLoadBalancer.animalSensesLoadBalancer.Add(Npc);
                    Npc.CancelInvoke(TickAi);

                    Npc.InvokeRandomized(Npc.TickAi, 0.1f, 0.1f, 0.00500000035f);

                    Npc.StopMoving();
                    Npc.Pause();

                    Npc.Invoke(Npc.Resume, 30f);
                }
            }
        }

        private Dictionary<string, KeyValuePair<Vector3, Vector3>> mountingPositions = new Dictionary<string, KeyValuePair<Vector3, Vector3>>
        {
            ["horse"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.6f, 0.2f), new Vector3(0, 0, 0)),
            ["stag"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.35f, 0.1f), new Vector3(0, 0, 0)),
            ["chicken"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0, 0), new Vector3(0, 0, 0)),
            ["wolf"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.1f, 0.2f), new Vector3(0, 0, 0)),
            ["bear"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.6f, 0), new Vector3(0, 0, 0)),
            ["boar"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.1f, 0.1f), new Vector3(0, 0, 0)),
        };
        #endregion

        #region Commands
        [ChatCommand("spawnhorse")]
        private void cmdSpawn(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "tsunhorse.spawnhorse"))
            {
                SendReply(player, msg("Error.NoPermission", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, msg("Help.Spawn", player.UserIDString));
                SendReply(player, string.Format(msg("Help.AvailableTypes", player.UserIDString), configData.Settings.AllowedTypes.ToSentence()));
                return;
            }

            string type = args[0].ToLower();
            if (!configData.Settings.AllowedTypes.Contains(type))
            {
                SendReply(player, msg("Error.InvalidAnimal", player.UserIDString) + " " + string.Format(msg("Help.AvailableTypes", player.UserIDString), configData.Settings.AllowedTypes.ToSentence()));
                return;
            }

            double remaining = 0;
            double current = CurrentTime();

            StoredData.Cooldown cooldown;

            if (storedData.cooldowns.TryGetValue(player.userID, out cooldown))
            {
                remaining = cooldown.GetValue(true);

                if (remaining > current)
                {
                    double time = remaining - current;
                    SendReply(player, string.Format(msg("Error.Cooldown", player.UserIDString), FormatTime(time)));
                    return;
                }
            }

            BaseNpc baseNpc;
            if (!SpawnAnimal(type, player.transform.position, out baseNpc))
            {
                SendReply(player, msg("Error.InvalidPosition", player.UserIDString));
                return;
            }

            baseNpc.Resume();
            timer.In(2f, () =>
            {
                if (baseNpc != null)
                {
                    baseNpc.StopMoving();
                    baseNpc.Pause();
                }
            });

            if (storedData.cooldowns.ContainsKey(player.userID))
                storedData.cooldowns[player.userID].Spawn = current + configData.Command.SpawnCooldown;
            else storedData.cooldowns.Add(player.userID, new StoredData.Cooldown(0, configData.Command.SpawnCooldown));

            SendReply(player, string.Format(msg("Msg.SpawnSuccess", player.UserIDString), type));
        }

        [ChatCommand("stop")]
        private void cmdStop(BasePlayer player, string command, string[] args)
        {
            if (!configData.Command.Command)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "tsunhorse.stophorse"))
            {
                SendReply(player, msg("Error.NoPermission", player.UserIDString));
                return;
            }

            double remaining = 0;
            double current = CurrentTime();

            StoredData.Cooldown cooldown;

            if (storedData.cooldowns.TryGetValue(player.userID, out cooldown))
            {
                remaining = cooldown.GetValue(false);

                if (remaining > current)
                {
                    double time = remaining - current;
                    SendReply(player, string.Format(msg("Error.Cooldown", player.UserIDString), FormatTime(time)));
                    return;
                }
            }
          
            List<BaseNpc> entities = Pool.GetList<BaseNpc>();
            Vis.Entities<BaseNpc>(player.transform.position, configData.Command.StopRadius, entities);
            int count = 0;
            foreach(BaseNpc baseNpc in entities.Distinct())
            {
                if (baseNpc.GetComponent<AnimalController>())
                    continue;

                if (configData.Settings.AllowedTypes.Any(x => x.Contains(baseNpc.ShortPrefabName, CompareOptions.IgnoreCase)))
                {
                    count++;
                    baseNpc.StopMoving();
                    baseNpc.Pause();

                    timer.In(configData.Command.StopTime, () => baseNpc?.Resume());
                }
            }
            Pool.FreeList(ref entities);
            if (count > 0)
            {
                if (storedData.cooldowns.ContainsKey(player.userID))
                    storedData.cooldowns[player.userID].Stop = current + configData.Command.StopCooldown;
                else storedData.cooldowns.Add(player.userID, new StoredData.Cooldown(configData.Command.StopCooldown, 0));

                SendReply(player, string.Format(msg("Help.StoppedAnimals", player.UserIDString), count));
            }
            else SendReply(player, msg("Help.NoStoppedAnimals", player.UserIDString));
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            public RidingSettings Settings { get; set;}
            public CommandSettings Command { get; set; }
            public Dictionary<string, SpeedSettings> Speeds { get; set; }

            public class RidingSettings
            {
                [JsonProperty(PropertyName = "Allowed animal types (Horse, Bear, Boar, Chicken, Stag, Wolf)")]
                public string[] AllowedTypes { get; set; }
                [JsonProperty(PropertyName = "Are animals invincible when being ridden")]
                public bool Invulnerable { get; set; }
                [JsonProperty(PropertyName = "Allow third person toggle (middle mouse button)")]
                public bool ThirdPersonHotkey { get; set; } 
            }

            public class CommandSettings
            {
                [JsonProperty(PropertyName = "Stop command cooldown time (seconds)")]
                public int StopCooldown { get; set; }
                [JsonProperty(PropertyName = "Spawn command cooldown time (seconds)")]
                public int SpawnCooldown { get; set; }
                [JsonProperty(PropertyName = "Enable command to stop animals within radius")]
                public bool Command { get; set; }
                [JsonProperty(PropertyName = "Radius in which to stop nearby animals")]
                public int StopRadius { get; set; }
                [JsonProperty(PropertyName = "Are animals invincible whilst stopped")]
                public bool Invulnerable { get; set; }
                [JsonProperty(PropertyName = "Amount of time animals stay stopped")]
                public int StopTime { get; set; }
            }

            public class SpeedSettings
            {
                [JsonProperty(PropertyName = "Walking speed")]
                public float Walk;
                [JsonProperty(PropertyName = "Sprinting speed")]
                public float Sprint;                
            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Command = new ConfigData.CommandSettings
                {
                    Command = true,
                    StopCooldown = 30,
                    SpawnCooldown = 3600,
                    StopRadius = 10,
                    Invulnerable = true,
                    StopTime = 10
                },
                Settings = new ConfigData.RidingSettings
                {
                    AllowedTypes = new string[] { "horse", "chicken" },
                    Invulnerable = true,                    
                    ThirdPersonHotkey = true,
                },
                Speeds = new Dictionary<string, ConfigData.SpeedSettings>
                {
                    ["bear"] = new ConfigData.SpeedSettings
                    {
                        Walk = 1.8f,
                        Sprint = 8f,
                    },
                    ["boar"] = new ConfigData.SpeedSettings
                    {
                        Walk = 1.4f,
                        Sprint = 7f,
                    },
                    ["chicken"] = new ConfigData.SpeedSettings
                    {
                        Walk = 0.4f,
                        Sprint = 2f,
                    },
                    ["horse"] = new ConfigData.SpeedSettings
                    {
                        Walk = 2.4f,
                        Sprint = 12f,
                    },
                    ["stag"] = new ConfigData.SpeedSettings
                    {
                        Walk = 2.2f,
                        Sprint = 11f,
                    },
                    ["wolf"] = new ConfigData.SpeedSettings
                    {
                        Walk = 1.8f,
                        Sprint = 9f,
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(3, 0, 1))
            {
                configData.Command.Invulnerable = true;
                configData.Command.StopTime = 10;
                configData.Command.SpawnCooldown = 3600;
                configData.Command.StopCooldown = 30;

                configData.Speeds = baseConfig.Speeds;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData() => data.WriteObject(storedData);

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public Dictionary<ulong, Cooldown> cooldowns = new Dictionary<ulong, Cooldown>();
            
            public class Cooldown
            {
                public double Stop { get; set; }
                public double Spawn { get; set; }

                public Cooldown() { }

                public Cooldown(double stop, double spawn)
                {
                    this.Stop = stop;
                    this.Spawn = spawn;
                }

                public double GetValue(bool isSpawn) => isSpawn ? Spawn : Stop;
            }
        }
        #endregion

        #region Localization
        private static string msg(string key, string playerId = null) => ins.lang.GetMessage(key, ins, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Controls1"] = "<color=#ce422b>Animal Controls:\nForward</color> - Move Forward\n<color=#ce422b>Left/Right</color> - Turn\n<color=#ce422b>Sprint</color> - Sprint\n<color=#ce422b>Jump</color> - Dismount animal",
            ["Help.StoppedAnimals"] = "You have temporarily stopped <color=#ce422b>{0}</color> animal(s)!",
            ["Help.NoStoppedAnimals"] = "<color=#ce422b>There are no animals nearby...</color>",
            ["Help.Spawn"] = "<color=#ce422b>/spawnhorse <type></color> - Spawn a animal you can ride",
            ["Help.AvailableTypes"] = "Available types are; <color=#ce422b>{0}</color>",
            ["Msg.SpawnSuccess"] = "You have spawned a <color=#ce422b>{0}</color>! You can mount it by looking at it and pressing the <color=#ce422b>USE</color> key",
            ["Error.InvalidPosition"] = "<color=#ce422b>Unable to spawn a animal at this position</color>",
            ["Error.InvalidAnimal"] = "<color=#ce422b>Invalid animal type selected!</color>",
            ["Error.NoPermission"] = "<color=#ce422b>You do not have permission to use this command</color>",
            ["Error.Cooldown"] = "You have a cooldown of <color=#ce422b>{0}</color> remaining",
            ["Warning.InvalidType"] = "<color=#ce422b>That animal is not rideable</color>",
            ["Warning.NearDeath"] = "<color=#ce422b>That animal is nearing death...</color>",
            ["Warning.InUse"] = "<color=#ce422b>That animal already has someone riding it</color>"
        };
        #endregion
    }   
}
