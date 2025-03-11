using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info ( "Harvester", "TopPlugin.ru", "1.2.0" )]
    [Description ( "Use a harvester to cut your crops when they fully grow." )]
    public class Harvester : RustPlugin
    {
        public static Harvester Instance { get; private set; }

        public bool IsInitialized { get; set; }

        #region Plugins

        [PluginReference] public Plugin Friends;
        [PluginReference] public Plugin Clans;

        private void RefreshPlugins ()
        {
            if ( Friends == null || !Friends.IsLoaded ) Friends = plugins.Find ( "Friends" );
            if ( Clans == null || !Clans.IsLoaded ) Clans = plugins.Find ( "Clans" );
        }

        #endregion

        #region Overrides

        private void Init ()
        {
            Instance = this;

            InstallPermissions ();
            RefreshPlugins ();
        }
        private void Loaded ()
        {
            if ( !IsInitialized ) return;

            if ( ConfigFile == null ) ConfigFile = new Core.Configuration.DynamicConfigFile ( $"{Manager.ConfigPath}{Path.DirectorySeparatorChar}{Name}.json" );

            CreateBehaviour ();

            InitiateHarvesters ();

            if ( !ConfigFile.Exists () )
            {
                ResetConfig ();

                ConfigFile.WriteObject ( Config );
            }
            else
            {
                try
                {
                    Config = ConfigFile.ReadObject<RootConfig> ();

                    foreach ( var harvester in Config.Harvesters )
                    {
                        HarvesterManagerBehaviour.StartCoroutine ( harvester.RefreshEverything () );
                        var generator = harvester.GetGenerator ();
                        if ( generator != null && harvester.PreviousGeneratorSettings.IsOn ) generator?.TurnOn (); else generator?.TurnOff ();
                    }

                    Puts ( $"Initialized {Config.Harvesters.Count.ToString ( "n0" )} harvester{( Config.Harvesters.Count == 1 ? "" : "s" )}." );

                    RefreshHarvesters ();
                }
                catch ( Exception exception )
                {
                    Puts ( $"Broken configuration: {exception.Message}" );
                }
            }

        }
        private void Unload ()
        {
            RefreshHarvesters ();

            foreach ( var harvester in Config.Harvesters )
            {
                HarvesterManagerBehaviour.StartCoroutine ( harvester.RemoveOutputContainerSkin () );
                harvester.GetGenerator ().TurnOff ();
            }

            Instance = null;
            ServerMgr.Instance.Invoke ( () => ClearBehaviour (), 5f );
        }
        private void OnServerInitialized ()
        {
            IsInitialized = true;

            Loaded ();
        }
        private void OnServerSave ()
        {
            if ( !IsInitialized ) return;
            RefreshHarvesters ();

            foreach ( var harvester in Config.Harvesters.ToArray () ) { harvester.PreviousGeneratorSettings.IsOn = harvester.GetGenerator ().IsOn (); }

            ConfigFile.WriteObject ( Config );

        }
        private bool CanPickupEntity ( BasePlayer player, FuelGenerator entity )
        {
            var harvester = Config.Harvesters.FirstOrDefault ( x => x.GeneratorId == entity.net.ID );
            if ( harvester != null )
            {
                Config.Harvesters.Remove ( harvester );
                RefreshHarvesters ();
            }

            return true;
        }
        private void OnEntityKill ( FuelGenerator entity )
        {
            if ( Config.Harvesters.Any ( x => x.GeneratorId == entity.net.ID ) )
            {
                RefreshHarvesters ();
            }
        }
        private void OnEntityDeath ( FuelGenerator entity, HitInfo info )
        {
            if ( Config.Harvesters.Any ( x => x.GeneratorId == entity.net.ID ) )
            {
                RefreshHarvesters ();
            }
        }
        private void OnServerShutdown ()
        {
            OnServerSave ();
        }
        private void OnPlayerDisconnected ( BasePlayer player, string reason )
        {
            StopEditing ( player.userID );
        }
        private object OnPlayerSleep ( BasePlayer player )
        {
            StopEditing ( player.userID );
            return null;
        }
        private object OnHammerHit ( BasePlayer player, HitInfo info )
        {
            var harvester = ( RootConfig.Harvester )null;
            var planterEntity = info?.HitEntity as PlanterBox;
            var smallGenerator = info?.HitEntity as FuelGenerator;

            if ( smallGenerator != null )
            {
                if ( IsEditing ( player.userID, out harvester ) ) { StopEditing ( player.userID ); if ( harvester != null && harvester.GeneratorId == smallGenerator.net.ID ) return false; }

                if ( Config.Harvesters.Any ( x => x.GeneratorId == smallGenerator.net.ID ) )
                {
                    if ( StartEditing ( player.userID, smallGenerator ) )
                    {
                        var settings = GetIdealSetting ( player.userID );
                        EditorTimer.Add ( player.userID, timer.Once ( settings.EditorTimeout, () => { StopEditing ( player.userID ); } ) );
                        return false;
                    }
                }

                return null;
            }

            if ( IsEditing ( player.userID, out harvester ) )
            {
                if ( harvester == null ) { StopEditing ( player.userID ); return null; }

                var ownerPlayer = BasePlayer.FindByID ( harvester.OwnerPlayerId );
                var isInTeam = ownerPlayer.Team?.members.Contains ( player.userID );

                var isFriends = false;
                var isClan = false;

                try { isFriends = Friends != null && Friends.IsLoaded && ( bool )Friends?.Call ( "AreFriends", player.userID, harvester.OwnerPlayerId ); } catch { }
                try { isClan = Clans != null && Clans.IsLoaded && ( bool )Clans?.Call ( "IsMemberOrAlly", player.userID.ToString (), harvester.OwnerPlayerId.ToString () ); } catch { }

                if ( planterEntity == null )
                {
                    Print ( $"You're not hitting a Planter!", player );
                    return false;
                }

                if ( Config.Harvesters.Any ( x => x.Planters.Any ( y => y.PlanterId == planterEntity.net.ID ) && x != harvester ) )
                {
                    Print ( $"This planter is already linked to a different Harvester!", player );
                    return false;
                }

                var distance = 0f;
                var setting = ( RootConfig.Setting )null;

                if ( !ValidDistance ( harvester.OwnerPlayerId, planterEntity, harvester, out distance, out setting ) )
                {
                    Print ( $"The planter is too far away from the Harvester — {distance.ToString ( "0.0" )}m. It needs to be under or equal to {setting.PlanterDistance}m.", player );
                    return false;
                }

                var planter = harvester.Planters.FirstOrDefault ( x => x.PlanterId == planterEntity.net.ID );
                if ( planter != null )
                {
                    if ( player.userID != harvester.OwnerPlayerId )
                    {
                        //
                        // Check if the owner of the Harvester owns the planter
                        //
                        if ( player.userID != planter.OwnerPlayerId )
                        {
                            if ( planter.OwnerPlayerId == harvester.OwnerPlayerId )
                            {
                                Print ( $"You cannot remove a planter assigned by the owner of this Harvester.", player );
                                return false;
                            }
                        }

                        //
                        // Check if the Harvester owner allows us to do it
                        //
                        if ( ownerPlayer != null )
                        {
                            var allow = harvester.AllowTeamToManage && isInTeam != null && isInTeam.Value;
                            if ( !allow ) allow = harvester.AllowFriendsToManage && isFriends;
                            if ( !allow ) allow = harvester.AllowClanToManage && isClan;
                            if ( !allow )
                            {
                                Print ( $"The owner of the Harvester does not allow you to execute this action.", player );
                                return false;
                            }
                        }
                    }

                    harvester.Planters.Remove ( planter );
                    harvester.RefreshEverything ();

                    Print ( $"You've unlinked this planter from this Harvester! Currently {harvester.Planters.Count.ToString ( "n0" )} out of {( setting.PlantersPerHarvester == -1 ? "unlimited" : setting.PlantersPerHarvester.ToString ( "n0" ) )} planters.", player );
                    OnServerSave ();
                    return false;
                }
                else
                {
                    if ( setting.PlantersPerHarvester != -1 && harvester.Planters.Count >= setting.PlantersPerHarvester )
                    {
                        Print ( $"You've reached the maximum amount of planters for this Harvester.", player );
                        return false;
                    }

                    harvester.Planters.Add ( new RootConfig.Planter ( planterEntity.net.ID, player.userID ) );
                    harvester.RefreshEverything ();

                    Print ( $"You've linked this planter with this Harvester! Currently {harvester.Planters.Count.ToString ( "n0" )} out of {( setting.PlantersPerHarvester == -1 ? "unlimited" : setting.PlantersPerHarvester.ToString ( "n0" ) )} planters.", player );
                    OnServerSave ();
                    return false;
                }
            }

            return null;
        }
        private void OnPluginLoaded ( Plugin name )
        {
            if ( name.Name == "Friends" || name.Name == "Clans" ) RefreshPlugins ();
        }
        private void OnPluginUnloaded ( Plugin name )
        {
            if ( name.Name == "Friends" || name.Name == "Clans" ) RefreshPlugins ();
        }

        private void InitiateHarvesters ()
        {
            timer.Every ( 1f, () =>
            {
                foreach ( var harvester in Config.Harvesters.ToArray () )
                {
                    var generator = harvester.GetGenerator ();

                    if ( generator == null )
                    {
                        HarvesterManagerBehaviour.StartCoroutine ( harvester.RemoveOutputContainerSkin () );
                        RefreshHarvesters ();
                        break;
                    }

                    HarvesterManagerBehaviour.StartCoroutine ( harvester.ApplyOutputContainerSkin () );

                    if ( !harvester.IsGeneratorRunning () || !harvester.HasOutputContainerAssigned () ) { continue; }

                    HarvesterManagerBehaviour.StartCoroutine ( harvester.RefreshPlanterGrowables () );
                }

                HarvesterManagerBehaviour.StartCoroutine ( CheckPlanters () );
            } );
        }

        private void ResetConfig ()
        {
            Config = new RootConfig ();

            Config.Settings.Add ( "admin", new RootConfig.Setting { MaximumHarvesters = -1, PlantersPerHarvester = -1, FuelPerSecond = -1, PlanterDistance = 50, EditorTimeout = 120 } );
            Config.Settings.Add ( "vip", new RootConfig.Setting { MaximumHarvesters = 5, PlantersPerHarvester = 4, FuelPerSecond = 0.01f, PlanterDistance = 35, EditorTimeout = 35 } );
            Config.Settings.Add ( "default", new RootConfig.Setting { MaximumHarvesters = 1, PlantersPerHarvester = 3, FuelPerSecond = 0.02f, PlanterDistance = 15, EditorTimeout = 20 } );
        }

        #endregion

        public void Print ( object message, BasePlayer player = null )
        {
            if ( player == null ) PrintToChat ( $"<color=orange>{Name}</color>: {message}" );
            else PrintToChat ( player, $"<color=orange>{Name}</color> (OY): {message}" );
        }

        public HarvesterManager HarvesterManagerBehaviour { get; private set; }
        private void CreateBehaviour ()
        {
            ClearBehaviour ();

            var gameObject = new GameObject ( "HarvesterManager" );
            HarvesterManagerBehaviour = gameObject.AddComponent<HarvesterManager> ();
        }
        private void ClearBehaviour ()
        {
            if ( HarvesterManagerBehaviour != null )
            {
                UnityEngine.Object.Destroy ( HarvesterManagerBehaviour.gameObject );
            }
        }

        public class HarvesterManager : MonoBehaviour { }

        private IEnumerator CheckPlanters ()
        {
            foreach ( var harvester in Config.Harvesters.ToArray () )
            {
                if ( !harvester.IsGeneratorRunning () && !harvester.HasOutputContainerAssigned () ) continue;

                var growables = harvester.GetPlanterGrowables ();
                if ( growables == null ) yield break;

                foreach ( var growable in growables )
                {
                    var somethingDied = false;

                    switch ( growable.State )
                    {
                        case PlantProperties.State.Ripe:
                            {
                                if ( growable.StageProgressFraction < 1.0f ) continue;

                                ItemManager.CreateByName ( growable.SourceItemDef.shortname, growable.CurrentPickAmount ).MoveToContainer ( harvester.GetOutputContainer ().inventory );
                                growable.ChangeState ( PlantProperties.State.Dying, false );
                                break;
                            }

                        case PlantProperties.State.Dying:
                            {
                                if ( Config.HarvestDyingPlants )
                                {
                                    ItemManager.CreateByName ( "plantfiber", 1 ).MoveToContainer ( harvester.GetOutputContainer ().inventory );

                                    growable.Kill ();
                                    somethingDied = true;
                                }

                                break;
                            }
                    }

                    if ( somethingDied ) { break; }

                    yield return new WaitForSeconds ( 0.5f );
                }

                yield return new WaitForSeconds ( 2 );
            }
        }
        public void RefreshHarvesters ()
        {
            var changed = false;

            Config.Harvesters.RemoveAll ( ( x ) =>
            {
                if ( x.GetGenerator () == null || x.GetGenerator ().IsDead () || x.GetGenerator ().IsDestroyed )
                {
                    if ( IsEditing ( x.OwnerPlayerId ) ) StopEditing ( x.OwnerPlayerId );

                    HarvesterManagerBehaviour.StartCoroutine ( x.RemoveOutputContainerSkin () );
                    changed = true;
                    return true;
                }

                return false;
            } );

            if ( changed ) OnServerSave ();
        }

        private static bool TryGetPlayerView ( BasePlayer player, out Quaternion viewAngle )
        {
            viewAngle = new Quaternion ( 0f, 0f, 0f, 0f );

            var input = player.serverInput;

            if ( input == null )
                return false;
            if ( input.current == null )
                return false;

            viewAngle = Quaternion.Euler ( input.current.aimAngles );
            return true;
        }
        private bool TryGetClosestRayPoint ( Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint )
        {
            Vector3 sourceEye = sourcePos + new Vector3 ( 0f, 1.5f, 0f );
            UnityEngine.Ray ray = new UnityEngine.Ray ( sourceEye, sourceDir * Vector3.forward );

            var hits = UnityEngine.Physics.RaycastAll ( ray );
            var closestdist = 999999f;
            closestHitpoint = sourcePos;
            closestEnt = false;

            foreach ( var hit in hits )
            {
                if ( hit.collider.GetComponentInParent<TriggerBase> () == null && !hit.collider.name.Contains ( "prevent" ) )
                {
                    if ( hit.distance < closestdist )
                    {
                        closestdist = hit.distance;
                        closestEnt = hit.collider;
                        closestHitpoint = hit.point;
                    }
                }
            }

            if ( closestEnt is bool )
                return false;

            return true;
        }
        private bool ValidDistance ( ulong playerId, BaseEntity entity, RootConfig.Harvester harvester, out float distance, out RootConfig.Setting setting )
        {
            setting = GetIdealSetting ( playerId );
            distance = Vector3.Distance ( harvester.GetGenerator ().ServerPosition, entity.ServerPosition );

            return distance <= setting.PlanterDistance;
        }

        #region Permission

        public const string AdminPerm = "harvester.admin";
        public const string UsePerm = "harvester.use";

        private void InstallPermissions ()
        {
            permission.RegisterPermission ( AdminPerm, this );
            permission.RegisterPermission ( UsePerm, this );
        }
        private bool HasPermission ( BasePlayer player, string perm )
        {
            if ( !permission.UserHasPermission ( player.UserIDString, perm ) )
            {
                SendReply ( player, $"You need to have the \"{perm}\" permission to run this command." );
                return false;
            }

            return true;
        }
        private RootConfig.Setting GetIdealSetting ( ulong player )
        {
            return Config.Settings.FirstOrDefault ( x => permission.UserHasGroup ( player.ToString (), x.Key ) ).Value;
        }

        #endregion

        #region Commands

        [ChatCommand ( "allowteamaccess" )]
        private void AllowTeamAccess ( BasePlayer player, string command, string [] args )
        {
            var harvester = ( RootConfig.Harvester )null;

            if ( IsEditing ( player.userID, out harvester ) )
            {
                harvester.AllowTeamToManage = !harvester.AllowTeamToManage;
                Print ( $"You've <color={( harvester.AllowTeamToManage ? "#91D930>enabled" : "#D93E30>disabled" )}</color> Team Access for this harvester.", player );
            }
            else
            {
                Print ( $"You're not editing a Harvester. Please do that first, then retry.", player );
            }
        }

        [ChatCommand ( "allowfriendsaccess" )]
        private void AllowFriendsAccess ( BasePlayer player, string command, string [] args )
        {
            var harvester = ( RootConfig.Harvester )null;

            if ( IsEditing ( player.userID, out harvester ) )
            {
                harvester.AllowFriendsToManage = !harvester.AllowFriendsToManage;
                Print ( $"You've <color={( harvester.AllowFriendsToManage ? "#91D930>enabled" : "#D93E30>disabled" )}</color> Friends Access for this harvester.", player );
            }
            else
            {
                Print ( $"You're not editing a Harvester. Please do that first, then retry.", player );
            }
        }

        [ChatCommand ( "allowclanaccess" )]
        private void AllowClanAccess ( BasePlayer player, string command, string [] args )
        {
            var harvester = ( RootConfig.Harvester )null;

            if ( IsEditing ( player.userID, out harvester ) )
            {
                harvester.AllowClanToManage = !harvester.AllowClanToManage;
                Print ( $"You've <color={( harvester.AllowClanToManage ? "#91D930>enabled" : "#D93E30>disabled" )}</color> Clan Access for this harvester.", player );
            }
            else
            {
                Print ( $"You're not editing a Harvester. Please do that first, then retry.", player );
            }
        }

        [ChatCommand ( "setharvester" )]
        private void SetHarvester ( BasePlayer player, string command, string [] args )
        {
            if ( !HasPermission ( player, UsePerm ) ) return;

            Quaternion currentRot;
            object closestEnt;
            Vector3 closestHitpoint;

            if ( !TryGetPlayerView ( player, out currentRot ) ) return;
            if ( !TryGetClosestRayPoint ( player.transform.position, currentRot, out closestEnt, out closestHitpoint ) ) return;

            var entity = ( closestEnt as Collider )?.gameObject;
            if ( closestEnt == null || entity == null ) return;

            var generator = entity.GetComponent<FuelGenerator> ();
            if ( generator == null )
            {
                Print ( $"You're not looking at a Small Generator!", player );
                return;
            }

            if ( Config.Harvesters.Any ( x => x.GeneratorId == generator.net.ID ) )
            {
                Print ( $"This Harvester has already been set!", player );
                return;
            }

            var setting = GetIdealSetting ( player.userID );
            var harvesters = Config.Harvesters.Count ( x => x.OwnerPlayerId == player.userID );
            if ( setting.MaximumHarvesters != -1 && harvesters >= setting.MaximumHarvesters )
            {
                Print ( $"You've reached the maximum Harvester amount! You've got <color=orange>{harvesters.ToString ( "n0" )}</color> harvester{( harvesters == 1 ? "" : "s" )}.", player );
                return;
            }

            var harvester = new RootConfig.Harvester ( player, generator );
            harvester.BackSettingsUp ();

            generator.outputEnergy = 0;

            Config.Harvesters.Add ( harvester );
            harvesters = Config.Harvesters.Count ( x => x.OwnerPlayerId == player.userID );
            Print ( $"You've set this Harvester! You've got <color=orange>{harvesters.ToString ( "n0" )}</color> out of {( setting.MaximumHarvesters == -1 ? "unlimited" : setting.MaximumHarvesters.ToString ( "n0" ) )} harvester{( setting.MaximumHarvesters == 1 ? "" : "s" )}.", player );

            if ( IsEditing ( player.userID ) ) StopEditing ( player.userID );
            if ( StartEditing ( player.userID, generator ) )
            {
                var settings = GetIdealSetting ( player.userID );
                EditorTimer.Add ( player.userID, timer.Once ( settings.EditorTimeout, () => { StopEditing ( player.userID ); } ) );
            }

            OnServerSave ();
        }

        [ChatCommand ( "unsetharvester" )]
        private void UnsetHarvester ( BasePlayer player, string command, string [] args )
        {
            if ( !HasPermission ( player, UsePerm ) ) return;

            Quaternion currentRot;
            object closestEnt;
            Vector3 closestHitpoint;

            if ( !TryGetPlayerView ( player, out currentRot ) ) return;
            if ( !TryGetClosestRayPoint ( player.transform.position, currentRot, out closestEnt, out closestHitpoint ) ) return;

            var entity = ( closestEnt as Collider )?.gameObject;
            if ( closestEnt == null || entity == null ) return;

            var generator = entity.GetComponent<FuelGenerator> ();
            if ( generator == null )
            {
                Print ( $"You're not looking at a Small Generator!", player );
                return;
            }

            var harvester = Config.Harvesters.FirstOrDefault ( x => x.GeneratorId == generator.net.ID );
            if ( harvester == null )
            {
                Print ( $"This Small Generator is not a Harvester!", player );
                return;
            }
            if ( harvester.OwnerPlayerId != player.userID )
            {
                Print ( $"You cannot perform this action since you're not the owner of this Harvester!", player );
                return;
            }

            harvester.GetGenerator ().TurnOff ();
            harvester.LoadBackupSettings ();
            HarvesterManagerBehaviour.StartCoroutine ( harvester.RemoveOutputContainerSkin () );

            Config.Harvesters.Remove ( harvester );
            StopEditing ( player.userID );

            Print ( $"You've removed this Harvester!", player );
            OnServerSave ();
        }

        [ChatCommand ( "setoutput" )]
        private void SetOutputContainer ( BasePlayer player, string command, string [] args )
        {
            if ( !HasPermission ( player, AdminPerm ) ) return;

            if ( !IsEditing ( player.userID ) )
            {
                Print ( $"You're not editing a Harvester. Please do that first, then retry.", player );
                return;
            }
            var harvester = GetEdit ( player );

            Quaternion currentRot;
            object closestEnt;
            Vector3 closestHitpoint;

            if ( !TryGetPlayerView ( player, out currentRot ) ) return;
            if ( !TryGetClosestRayPoint ( player.transform.position, currentRot, out closestEnt, out closestHitpoint ) ) return;

            var entity = ( closestEnt as Collider )?.gameObject;
            if ( closestEnt == null || entity == null ) return;

            var boxStorage = entity.GetComponent<BoxStorage> ();
            if ( boxStorage == null )
            {
                Print ( $"You're not looking at a Box Storage!", player );
                return;
            }
            if ( boxStorage.net.ID == harvester.OutputContainerId )
            {
                Print ( $"You've already set this Box Storage for this Harvester!", player );
                return;
            }
            if ( Config.Harvesters.Any ( x => x.OutputContainerId == boxStorage.net.ID ) )
            {
                Print ( $"This storage is already linked with an another Harvester!", player );
                return;
            }
            if ( player.userID != harvester.OwnerPlayerId )
            {
                Print ( $"You're not the owner of this Harvester!", player );
                StopEditing ( player.userID );
                return;
            }

            harvester.RemoveOutputContainerSkin ();

            harvester.OutputContainerId = boxStorage.net.ID;
            harvester.PreviousOutputContainerSkinId = boxStorage.skinID;

            harvester.RefreshEverything ();
            harvester.ApplyOutputContainerSkin ();

            Print ( $"You've set the output container for this Harvester!", player );
            OnServerSave ();
        }

        [ChatCommand ( "unsetoutput" )]
        private void UnsetOutputContainer ( BasePlayer player, string command, string [] args )
        {
            if ( !HasPermission ( player, AdminPerm ) ) return;

            if ( !IsEditing ( player.userID ) )
            {
                Print ( $"You're not editing a Harvester. Please do that first, then retry.", player );
                return;
            }
            var harvester = GetEdit ( player );

            if ( player.userID != harvester.OwnerPlayerId )
            {
                Print ( $"You're not the owner of this Harvester!", player );
                StopEditing ( player.userID );
                return;
            }

            harvester.RemoveOutputContainerSkin ();
            harvester.OutputContainerId = 0;
            harvester.RefreshEverything ();

            Print ( $"You've removed the output container for this Harvester!", player );
            OnServerSave ();
        }

        #endregion

        #region Editor 

        public bool StartEditing ( ulong playerId, FuelGenerator generator )
        {
            if ( Editor.ContainsKey ( playerId ) )
            {
                Print ( $"You're already editing a Harvester!" );
                return false;
            }

            var harvester = Config.Harvesters.FirstOrDefault ( x => x.GeneratorId == generator.net.ID );
            var ownerPlayer = BasePlayer.FindByID ( harvester.OwnerPlayerId );
            if ( harvester.OwnerPlayerId != playerId )
            {
                var isInTeam = ownerPlayer?.Team?.members.Contains ( playerId );
                var isFriends = false;
                var isClan = false;
                try { isFriends = Friends != null && Friends.IsLoaded && ( bool )Friends?.Call ( "AreFriends", playerId, harvester.OwnerPlayerId ); } catch { }
                try { isClan = Clans != null && Clans.IsLoaded && ( bool )Clans?.Call ( "IsMemberOrAlly", playerId.ToString (), harvester.OwnerPlayerId.ToString () ); } catch { }

                var allow = harvester.AllowTeamToManage && isInTeam != null && isInTeam.Value;
                if ( !allow ) allow = harvester.AllowFriendsToManage && isFriends;
                if ( !allow ) allow = harvester.AllowClanToManage && isClan;
                if ( !allow ) return false;
            }

            Editor.Add ( playerId, generator.net.ID );

            Print ( $"You started editing a Harvester.", BasePlayer.FindByID ( playerId ) );
            return true;
        }
        public bool StopEditing ( ulong playerId )
        {
            if ( !Editor.ContainsKey ( playerId ) )
            {
                // Print ( $"You're not editing a Harvester!" );
                return false;
            }

            if ( EditorTimer.ContainsKey ( playerId ) ) { EditorTimer [ playerId ].Destroy (); EditorTimer.Remove ( playerId ); }

            Editor.Remove ( playerId );
            Print ( $"You stopped editing a Harvester.", BasePlayer.FindByID ( playerId ) );
            return true;
        }
        public bool IsEditing ( ulong playerId )
        {
            return Editor.ContainsKey ( playerId );
        }
        public bool IsEditing ( ulong playerId, out RootConfig.Harvester harvester )
        {
            harvester = GetEdit ( playerId );
            return IsEditing ( playerId );
        }
        public RootConfig.Harvester GetEdit ( ulong playerId )
        {
            if ( !IsEditing ( playerId ) ) return null;

            return Config.Harvesters.FirstOrDefault ( x => x.GeneratorId == Editor [ playerId ] );
        }
        public RootConfig.Harvester GetEdit ( BasePlayer player )
        {
            return GetEdit ( player.userID );
        }

        #endregion

        #region Config

        public Core.Configuration.DynamicConfigFile ConfigFile { get; set; }
        public new RootConfig Config { get; set; } = new RootConfig ();
        public Dictionary<ulong, uint> Editor { get; set; } = new Dictionary<ulong, uint> ();
        public Dictionary<ulong, Timer> EditorTimer { get; set; } = new Dictionary<ulong, Timer> ();

        public class RootConfig
        {
            public bool HarvestDyingPlants { get; set; } = true;
            public Dictionary<string, Setting> Settings { get; set; } = new Dictionary<string, Setting> ();

            public OutputContainerSkins GeneratorOnSkin { get; set; } = new OutputContainerSkins ( 2101522859, 1262018145 );
            public OutputContainerSkins GeneratorOffSkin { get; set; } = new OutputContainerSkins ( 1312998395, 826323166 );

            public List<Harvester> Harvesters { get; set; } = new List<Harvester> ();

            public class OutputContainerSkins
            {
                public ulong WoodStorageBox { get; set; }
                public ulong LargeWoodBox { get; set; }

                public OutputContainerSkins () { }
                public OutputContainerSkins ( ulong woodStorageBoxSkin, ulong largeWoodBoxSkin )
                {
                    WoodStorageBox = woodStorageBoxSkin;
                    LargeWoodBox = largeWoodBoxSkin;
                }
            }

            public class Harvester
            {
                public Harvester () { }
                public Harvester ( BasePlayer player, FuelGenerator generator ) { OwnerPlayerId = player.userID; GeneratorId = generator.net.ID; }

                public ulong OwnerPlayerId { get; set; }
                public uint GeneratorId { get; set; }
                public uint OutputContainerId { get; set; }
                public List<Planter> Planters { get; set; } = new List<Planter> ();

                public bool AllowTeamToManage { get; set; } = false;
                public bool AllowFriendsToManage { get; set; } = false;
                public bool AllowClanToManage { get; set; } = false;

                #region Backup

                public FuelGeneratorSettings PreviousGeneratorSettings { get; set; } = new FuelGeneratorSettings ();
                public ulong PreviousOutputContainerSkinId { get; set; }

                public void BackSettingsUp ()
                {
                    var generator = GetGenerator ();
                    PreviousGeneratorSettings.OutputEnergy = generator.outputEnergy;
                    PreviousGeneratorSettings.FuelPerSecond = generator.fuelPerSec;
                }
                public void LoadBackupSettings ()
                {
                    var generator = GetGenerator ();
                    generator.outputEnergy = PreviousGeneratorSettings.OutputEnergy;
                    generator.fuelPerSec = PreviousGeneratorSettings.FuelPerSecond;
                }

                #endregion

                private FuelGenerator _generator { get; set; }
                private StorageContainer _storageContainer { get; set; }
                private PlanterBox [] _planters { get; set; }
                private GrowableEntity [] _planterGrowables { get; set; }

                public FuelGenerator GetGenerator ()
                {
                    var generator = _generator ?? ( _generator = ( FuelGenerator )BaseNetworkable.serverEntities.Find ( GeneratorId ) );
                    if ( generator != null )
                    {
                        generator.fuelPerSec = Instance.GetIdealSetting ( OwnerPlayerId ).FuelPerSecond;

                        if ( !HasOutputContainerAssigned () )
                        {
                            generator.TurnOff ();
                        }
                    }

                    return generator;
                }
                public StorageContainer GetOutputContainer () { return _storageContainer ?? ( _storageContainer = ( StorageContainer )BaseNetworkable.serverEntities.Find ( OutputContainerId ) ); }
                public PlanterBox [] GetPlanters ()
                {
                    Planters.RemoveAll ( ( Planter planter ) =>
                    {
                        if ( planter == null ) return false;

                        var planterEntity = BaseNetworkable.serverEntities.Find ( planter.PlanterId );
                        if ( planterEntity == null || planterEntity.IsDestroyed ) return true;

                        return false;
                    } );

                    return _planters ?? ( _planters = BaseNetworkable.serverEntities.Where ( x => Planters.Any ( y => y.PlanterId == x.net.ID ) ).Select ( x => ( PlanterBox )x ).ToArray () );
                }
                public GrowableEntity [] GetPlanterGrowables ()
                {
                    if ( _planterGrowables == null )
                    {
                        var planters = GetPlanters ();
                        if ( planters == null ) return null;

                        var list = new List<GrowableEntity> ();
                        foreach ( var planter in planters )
                        {
                            list.AddRange ( planter.GetComponentsInChildren<GrowableEntity> () );
                        }

                        _planterGrowables = list.ToArray ();
                    }

                    return _planterGrowables;
                }

                #region Methods

                public bool HasOutputContainerAssigned ()
                {
                    return OutputContainerId != 0 && GetOutputContainer () != null;
                }

                public IEnumerator RefreshEverything ()
                {
                    _generator = null;
                    _storageContainer = null;
                    _planters = null;
                    _planterGrowables = null;

                    yield return GetGenerator ();
                    yield return GetOutputContainer ();
                    yield return GetPlanters ();
                    yield return GetPlanterGrowables ();
                }
                public IEnumerator RefreshPlanterGrowables ()
                {
                    _planterGrowables = null;
                    yield return GetPlanterGrowables ();
                }

                public IEnumerator ApplyOutputContainerSkin ()
                {
                    var container = ( StorageContainer )null;
                    yield return container = GetOutputContainer ();

                    if ( container == null ) yield break;

                    var skin = 0uL;

                    if ( container.PrefabName.Contains ( "woodbox_deployed" ) )
                        skin = IsGeneratorRunning () ? Instance.Config.GeneratorOnSkin.WoodStorageBox : Instance.Config.GeneratorOffSkin.WoodStorageBox;
                    else if ( container.PrefabName.Contains ( "box.wooden.large" ) )
                        skin = IsGeneratorRunning () ? Instance.Config.GeneratorOnSkin.LargeWoodBox : Instance.Config.GeneratorOffSkin.LargeWoodBox;

                    if ( skin == 0 || container.skinID == skin ) yield break;

                    container.skinID = skin;
                    container.SendNetworkUpdateImmediate ();
                }
                public IEnumerator RemoveOutputContainerSkin ()
                {
                    var container = ( StorageContainer )null;
                    yield return container = GetOutputContainer ();

                    if ( container == null ) yield break;

                    container.skinID = PreviousOutputContainerSkinId;
                    container.SendNetworkUpdateImmediate ();
                }

                public bool IsGeneratorRunning ()
                {
                    return _generator != null && _generator.IsOn ();
                }

                #endregion

                public class FuelGeneratorSettings
                {
                    public bool IsOn { get; set; }
                    public int OutputEnergy { get; set; }
                    public float FuelPerSecond { get; set; }
                }
            }
            public class Planter
            {
                public uint PlanterId { get; set; }
                public ulong OwnerPlayerId { get; set; }

                public Planter () { }
                public Planter ( uint planterId, ulong ownerPlayerId )
                {
                    PlanterId = planterId;
                    OwnerPlayerId = ownerPlayerId;
                }
            }
            public class Setting
            {
                public int MaximumHarvesters { get; set; }
                public int PlantersPerHarvester { get; set; }
                public float FuelPerSecond { get; set; }
                public float PlanterDistance { get; set; }
                public float EditorTimeout { get; set; } = 20f;
            }
        }

        #endregion
    }
}
