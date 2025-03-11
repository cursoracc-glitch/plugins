using Rust;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Facepunch;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Trains", "Colon Blow", "1.0.5")]
    class Trains : CovalencePlugin
    {

        #region Load

        [PluginReference]
        Plugin TrainsE1;

        const string permAdmin = "trains.admin";
        const string permConductor = "trains.conductor";

        List<ulong> playersInTrackMode = new List<ulong>();
        List<BaseEntity> trackEditMarkers = new List<BaseEntity>();

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permConductor, this);
            permission.RegisterPermission(permAdmin, this);
            LoadDataFile();
            timer.In(5, RespawnAllTrains);
        }

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            public TrainSettings trainSettings { get; set; }

            public class TrainSettings
            {
                [JsonProperty(PropertyName = "Data File Setting : Use Data File named : ")] public string saveInfoName { get; set; }
                [JsonProperty(PropertyName = "Global Setting - Enable Event Train ? ")] public bool enableEventTrain { get; set; }
                [JsonProperty(PropertyName = "Global Setting - Destroy things in trains way ?")] public bool damageOnCollision { get; set; }
                [JsonProperty(PropertyName = "Global Setting - Eject Sleepers from train ?")] public bool ejectSleepers { get; set; }
                [JsonProperty(PropertyName = "Global Setting - Trains speeds up and down depending on angle ? ")] public bool useAngleSpeed { get; set; }
                [JsonProperty(PropertyName = "Editor - seconds to show current selected tracks waypoint number above waypoint when toggled (1k or more waypoints recommend 5 or less) ")] public float timeShowText { get; set; }
                [JsonProperty(PropertyName = "Editor - seconds to show current selected tracks lines betweet waypoints (1k or more waypoints recommend 5 or less) ")] public float timeShowLines { get; set; }
                [JsonProperty(PropertyName = "Editor - seconds to show current selected tracks stop number above stop when toggled ")] public float timeShowStops { get; set; }
                [JsonProperty(PropertyName = "Custom Prefab - Prefab string for Train Type 5 (default is rowboat) ")] public string customPrefabStr { get; set; }
            }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                trainSettings = new PluginConfig.TrainSettings
                {
                    saveInfoName = "Trains",
                    enableEventTrain = true,
                    damageOnCollision = true,
                    ejectSleepers = true,
                    useAngleSpeed = true,
                    timeShowText = 15f,
                    timeShowLines = 15f,
                    timeShowStops = 15f,
                    customPrefabStr = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAuthorized"] = "You are NOT Authorized to do that !!",
                ["ClearedData"] = "You have removed all saved tracks !!",
                ["TrackModeEnabled1"] = "Track Mode enabled. ",
                ["TrackModeEnabled2"] = "Track Mode enabled and showing markers for track ",
                ["TrackModeDisabled"] = "Track Mode disable.",
                ["UnknownTrack"] = "The selected Track does not exist !!",
                ["KnownTrack"] = "The selected Track already exists, please use another name !!",
                ["SelectTrack"] = "You need to select Track to edit !!",
                ["CreatedTrack"] = "You createed new track ",
                ["RemovedTrack"] = "You removed track ",
                ["ShowMarkers"] = "Now showing all Track Markers",
                ["HideMarkers"] = "Now hiding all track Markers",
                ["ToggledMiddleMouse"] = "Middle Mouse Button Toggled to allow adding waypoints to selected Track.",
                ["UnToggledMiddleMouse"] = "Middle Mouse Button Toggled to allow adding waypoints to selected Track.",
                ["UnknownCommand"] = "You must specify a secondary  commmand !!",
                ["UnknownWaypoint"] = "You must specify a secondary  commmand !!",
                ["MarkedWaypoint"] = "You Marked a new waypoint location for ",
                ["DeletedWaypoint"] = "You Have Removed the Last Waypoint in Current Track list ",
                ["MarkedStoppoint"] = "You Marked a new stop location for ",
                ["DeletedStoppoint"] = "You Have Removed the Last Stop Point in Current Track list ",
                ["ChangedTrainType"] = "You have changed current tracks train type. ",
                ["ChangedLooping"] = "You have changed whether current track loops or not. ",
                ["NotEditMode"] = "You must NOT be in track edit mode to do that !!",
                ["EditMode"] = "You must be in edit mode to do that !!"
            }, this);
        }

        #endregion

        #region Data

        static StoredData storedData = new StoredData();
        DynamicConfigFile dataFile;

        public class StoredData
        {
            public Dictionary<string, TrainTrackData> trainTrackData = new Dictionary<string, TrainTrackData>();
            public class TrainTrackData
            {
                public int trainType;
                public bool looping;
                public bool autospawn;
                public bool autostart;
                public float maxSpeed;
                public float stopWaitTime;
                public List<Vector3> trackMarkers = new List<Vector3>();
                public List<Vector3> trainStops = new List<Vector3>();
                public TrainTrackData() { }
            }
            public StoredData() { }
        }

        private void LoadDataFile()
        {
            dataFile = Interface.Oxide.DataFileSystem.GetFile("Trains/" + config.trainSettings.saveInfoName);

            try
            {
                storedData = dataFile.ReadObject<StoredData>();
            }
            catch { }

            if (storedData == null)
                storedData = new StoredData();
        }

        private void SaveData()
        {
            if (dataFile != null && storedData != null)
            {
                dataFile.WriteObject(storedData);
            }
        }

        private void OnServerSave()
        {
            if (storedData.trainTrackData.Count == 0) return;
            SaveData();
        }

        #endregion

        #region Commands

        [Command("train")]
        private void cmdTrainHelp(IPlayer player, string command)
        {
            if (!player.HasPermission(permAdmin)) return;
            StringBuilder newHelpString = new StringBuilder();
            newHelpString.Append("<color=orange>/trackmode </color> - turns on train track edit mode on/off. (remove active trains)\n");
            newHelpString.Append("<color=orange>/track.create </color><color=green>trackname</color> - creates specified track.\n");
            newHelpString.Append("<color=orange>/track.remove </color> - removes selected track while in edit mode.\n");
            newHelpString.Append("<color=orange>/track.edit </color><color=green>trackname</color> - changed current track in edit mode.\n");
            newHelpString.Append("<color=orange>/track.traintype </color> - changes train type of selected track while in edit mode.\n");
            newHelpString.Append("<color=orange>/track.trainloop </color> - changes train looping or not of selected track while in edit mode.\n");
            newHelpString.Append("<color=orange>/track.showlines </color> - toggles visual waypoint lines on and off for all tracks.\n");
            newHelpString.Append("<color=orange>/track.showtext </color> - toggles visual waypoint text on and off for all tracks.\n");
            newHelpString.Append("<color=orange>/track.mark </color> - use to mark current player position as next waypoint for selected track in edit mode.\n");
            newHelpString.Append("<color=orange>/track.markstop </color> - use to mark current player position as next stop point for selected track in edit mode.\n");
            newHelpString.Append("<color=orange>/track.deletelastmark </color> - deletes the last waypoint position marked of selected track in edit mode.\n");
            newHelpString.Append("<color=orange>/track.deletelaststop </color> - deletes the last stop position marked of selected track in edit mode.\n");
            newHelpString.Append("<color=orange>/train.eraseall</color> - removes all waypoints and stops from databases.\n");
            newHelpString.Append("<color=orange>/train.spawn </color><color=green>trackname</color> - spawns train on specifed track.\n");
            newHelpString.Append("<color=orange>/train.setconfig </color><color=green>config_name</color> - sets config to that name, creates a new blank config if it doesnt exist\n");
            player.Reply(newHelpString.ToString());
        }

        [Command("trackmode")]
        private void cmdTrackEnable(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasGui = basePlayer.GetComponent<TrackEditGUI>();
                if (hasGui) hasGui.OnDestroy();

                playersInTrackMode.Remove(basePlayer.userID);
                player.Message(lang.GetMessage("TrackModeDisabled", this, player.Id));
                RespawnAllTrains();
                return;
            }
            else
            {
                playersInTrackMode.Add(basePlayer.userID);
                DestroyAll<TrainEntity>();
                var addEditor = basePlayer.gameObject.AddComponent<TrackEditGUI>();
                string trackName = string.Join(" ", addEditor.trackstring).ToLower();
                if (storedData.trainTrackData.Any())
                {
                    trackName = storedData.trainTrackData.First().Key;
                }
                addEditor.RefreshTrackList(basePlayer, trackName);
                player.Message(lang.GetMessage("TrackModeEnabled1", this, player.Id));
            }
        }

        [Command("track.create")]
        private void cmdTrackCreate(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                if (args.Length > 0)
                {
                    string trackName = args[0].ToLower();
                    if (!storedData.trainTrackData.ContainsKey(trackName))
                    {
                        storedData.trainTrackData.Add(args[0].ToLower(), new StoredData.TrainTrackData
                        {
                            trainType = 1,
                            looping = false,
                            autospawn = true,
                            autostart = true,
                            maxSpeed = 10f,
                            stopWaitTime = 10f,
                            trackMarkers = new List<Vector3>(),
                            trainStops = new List<Vector3>()
                        });
                        SaveData();
                        var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                        if (hasEditor)
                        {
                            hasEditor.RefreshTrackList(basePlayer, trackName);
                        }
                    }
                    else player.Message(lang.GetMessage("KnownTrack", this, player.Id));
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.remove")]
        private void cmdTrackRemove(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                if (args.Length > 0)
                {
                    string trackName = string.Join(" ", args).ToLower();

                    CuiHelper.DestroyUi(basePlayer, trackName);
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        storedData.trainTrackData.Remove(trackName);
                        SaveData();
                        player.Message(lang.GetMessage("RemovedTrack", this, player.Id) + trackName);
                        var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                        if (hasEditor)
                        {
                            hasEditor.trackstring = trackName;
                            hasEditor.RefreshTrackList(basePlayer, trackName);
                        }
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.edit")]
        private void cmdTrackEdit(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                if (args.Length > 0)
                {
                    string trackName = string.Join(" ", args).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                        if (hasEditor)
                        {
                            hasEditor.RefreshTrackList(basePlayer, trackName);
                        }
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.traintype")]
        private void cmdTrackTrainType(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        int traintype = storedData.trainTrackData[trackName].trainType;
                        if (traintype == 1) traintype = 2;
                        else if (traintype == 2) traintype = 3;
                        else if (traintype == 3) traintype = 4;
                        else if (traintype == 4) traintype = 5;
                        else if (traintype >= 5) traintype = 1;
                        storedData.trainTrackData[trackName].trainType = traintype;
                        SaveData();
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.trainloop")]
        private void cmdTrackTrainLoop(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        bool looping = storedData.trainTrackData[trackName].looping;
                        looping = !looping;
                        storedData.trainTrackData[trackName].looping = looping;
                        SaveData();
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.autospawn")]
        private void cmdTrackAutoSpawn(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        bool autospawn = storedData.trainTrackData[trackName].autospawn;
                        autospawn = !autospawn;
                        storedData.trainTrackData[trackName].autospawn = autospawn;
                        SaveData();
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.autostart")]
        private void cmdTrackAutoStart(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        bool autostart = storedData.trainTrackData[trackName].autostart;
                        autostart = !autostart;
                        storedData.trainTrackData[trackName].autostart = autostart;
                        SaveData();
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.maxspeed")]
        private void cmdTrackMaxSpeed(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        float maxTrainSpeed = storedData.trainTrackData[trackName].maxSpeed;
                        maxTrainSpeed += 2f;
                        if (maxTrainSpeed > 20f) maxTrainSpeed = 2f;
                        storedData.trainTrackData[trackName].maxSpeed = maxTrainSpeed;
                        SaveData();
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.waittime")]
        private void cmdTrackWaitTime(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        float trainWaitTime = storedData.trainTrackData[trackName].stopWaitTime;
                        trainWaitTime += 5f;
                        if (trainWaitTime > 60f) trainWaitTime = 5f;
                        storedData.trainTrackData[trackName].stopWaitTime = trainWaitTime;
                        SaveData();
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.showlines")]
        private void cmdTrackShowLines(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID) && basePlayer.net?.connection?.authLevel >= 1)
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        hasEditor.enableShowLines = !hasEditor.enableShowLines;
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
        }

        [Command("track.showtext")]
        private void cmdTrackShowText(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID) && basePlayer.net?.connection?.authLevel >= 1)
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        hasEditor.enableShowText = !hasEditor.enableShowText;
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
        }

        [Command("track.showstops")]
        private void cmdTrackShowStops(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID) && basePlayer.net?.connection?.authLevel >= 1)
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    if (storedData.trainTrackData.ContainsKey(trackName))
                    {
                        hasEditor.enableShowStops = !hasEditor.enableShowStops;
                        hasEditor.RefreshTrackList(basePlayer, trackName);
                    }
                }
            }
        }

        [Command("track.enablemiddle")]
        private void cmdTrackEnableMiddle(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                if (hasEditor)
                {
                    string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                    hasEditor.enableMiddleMouse = !hasEditor.enableMiddleMouse;
                    hasEditor.RefreshTrackList(basePlayer, trackName);
                    if (hasEditor.enableMiddleMouse) player.Message(lang.GetMessage("ToggledMiddleMouse", this, player.Id));
                    else player.Message(lang.GetMessage("UnToggledMiddleMouse", this, player.Id));
                }
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("track.mark")]
        private void cmdTrackMarkWaypoint(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
            if (hasEditor)
            {
                string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                if (storedData.trainTrackData.ContainsKey(trackName))
                {
                    storedData.trainTrackData[trackName].trackMarkers.Add(basePlayer.transform.position);
                    SaveData();
                    player.Message(lang.GetMessage("MarkedWaypoint", this, player.Id) + trackName);
                    hasEditor.RefreshTrackList(basePlayer, trackName);
                }
            }
        }

        [Command("track.markstop")]
        private void cmdTrackMarkStop(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
            if (hasEditor)
            {
                string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                if (storedData.trainTrackData.ContainsKey(trackName))
                {
                    storedData.trainTrackData[trackName].trainStops.Add(basePlayer.transform.position);
                    SaveData();
                    player.Message(lang.GetMessage("MarkedStoppoint", this, player.Id) + trackName);
                    hasEditor.RefreshTrackList(basePlayer, trackName);
                }
            }
        }

        [Command("track.deletelastmark")]
        private void cmdTrackDeleteLastMark(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
            if (hasEditor)
            {
                string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                if (storedData.trainTrackData.ContainsKey(trackName) && storedData.trainTrackData[trackName].trackMarkers.Count > 0)
                {
                    Vector3 lastpos = storedData.trainTrackData[trackName].trackMarkers.Last();
                    if (lastpos != null)
                    {
                        storedData.trainTrackData[trackName].trackMarkers.Remove(lastpos);
                        SaveData();
                        player.Message(lang.GetMessage("DeletedWaypoint", this, player.Id) + trackName);
                    }
                    hasEditor.RefreshTrackList(basePlayer, trackName);
                }
            }
        }

        [Command("track.deletelaststop")]
        private void cmdTrackDeleteLastStop(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
            if (hasEditor)
            {
                string trackName = string.Join(" ", hasEditor.trackstring).ToLower();
                if (storedData.trainTrackData.ContainsKey(trackName) && storedData.trainTrackData[trackName].trainStops.Count > 0)
                {
                    Vector3 lastpos = storedData.trainTrackData[trackName].trainStops.Last();
                    if (lastpos != null)
                    {
                        storedData.trainTrackData[trackName].trainStops.Remove(lastpos);
                        SaveData();
                        player.Message(lang.GetMessage("DeletedStoppoint", this, player.Id) + trackName);
                    }
                    hasEditor.RefreshTrackList(basePlayer, trackName);
                }
            }
        }

        [Command("track.eraseall")]
        private void cmdTrackEraseAll(IPlayer player, string command)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (playersInTrackMode.Contains(basePlayer.userID))
            {
                storedData.trainTrackData.Clear();
                SaveData();
                player.Message(lang.GetMessage("ClearData", this, player.Id));
                RefeshPlayerGUI(basePlayer);
            }
            else player.Message(lang.GetMessage("EditMode", this, player.Id));
        }

        [Command("train.setconfig")]
        private void cmdTrainsSetConfig(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            var basePlayer = player.Object as BasePlayer;
            if (!playersInTrackMode.Contains(basePlayer.userID))
            {
                if (args.Length > 0)
                {
                    DestroyAll<TrainEntity>();
                    string configName = args[0].ToString();
                    config.trainSettings.saveInfoName = configName;
                    SaveConfig();
                    SaveData();
                    LoadDataFile();
                    LoadConfig();
                    timer.Once(2f, () => RespawnAllTrains());
                    player.Message(lang.GetMessage("<color=yellow>Created new Data File called : </color> ", this, player.Id) + configName);
                }
            }
            else player.Message(lang.GetMessage("NotEditMode", this, player.Id));

        }

        [Command("train.spawn")]
        private void cmdTrainSpawnTrain(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            if (args != null && args.Length > 0)
            {
                if (storedData.trainTrackData.ContainsKey(args[0].ToLower()))
                {
                    AddTrainEntityToTrack(args[0].ToLower());
                    SaveData();
                }
                else player.Message(lang.GetMessage("UnknownTrack", this, player.Id));
            }
        }

        [Command("train.spawnevent1")]
        private void cmdTrainSpawnTrainEvent1(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin)) return;
            if (TrainsE1 == null) return;
            if (args != null && args.Length > 0)
            {
                if (storedData.trainTrackData.ContainsKey(args[0].ToLower()))
                {
                    TrainsE1?.Call("SpawnEventTrain", storedData.trainTrackData[args[0].ToLower()].trackMarkers, storedData.trainTrackData[args[0].ToLower()].trainStops);
                }
                else player.Message(lang.GetMessage("UnknownTrack", this, player.Id));
            }
        }
        #endregion

        #region Hooks

        private void RefeshPlayerGUI(BasePlayer basePlayer)
        {
            var hasEditGUI = basePlayer.GetComponent<TrackEditGUI>();
            if (hasEditGUI) hasEditGUI.OnDestroy();
            basePlayer.gameObject.AddComponent<TrackEditGUI>();
        }

        private void AddTrainEntityToTrack(string listname)
        {
            if (storedData.trainTrackData[listname].trackMarkers.ElementAtOrDefault(0) == null) return;
            Vector3 position = storedData.trainTrackData[listname].trackMarkers.ElementAtOrDefault(0);
            string strPrefab = "assets/prefabs/visualization/sphere.prefab";
            BaseEntity sphereEntity = GameManager.server.CreateEntity(strPrefab, position, Quaternion.identity, true);
            SphereEntity ball = sphereEntity.GetComponent<SphereEntity>();
            ball.lerpRadius = 1f;
            ball.lerpSpeed = 100f;
            StabilityEntity getStab = sphereEntity.GetComponent<StabilityEntity>();
            if (getStab) getStab.grounded = true;
            sphereEntity.Spawn();
            TrainEntity addTrainEntity = sphereEntity.gameObject.AddComponent<TrainEntity>();
            addTrainEntity.SpawnTrain(listname);
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || entity is BasePlayer) return;
            var istrain = entity.GetComponentInParent<TrainEntity>();
            if (istrain)
            {
                hitInfo.damageTypes.ScaleAll(0);
            }
        }

        private object OnStructureRotate(BaseCombatEntity entity, BasePlayer player)
        {
            var istrain = entity.GetComponentInParent<TrainEntity>();
            if (istrain) return false;
            return null;
        }

        private object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            var istrain = entity.GetComponentInParent<TrainEntity>();
            if (istrain) return false;
            return null;
        }

        private object OnEntityGroundMissing(BaseEntity entity)
        {
            var istrain = entity.GetComponentInParent<TrainEntity>();
            if (istrain != null) return false;
            return null;
        }

        private object OnSwitchToggle(ElectricSwitch electricSwitch, BasePlayer player)
        {
            if (electricSwitch == null || player == null) return null;
            var isTrainEntity = electricSwitch.GetComponentInParent<TrainEntity>();
            if (isTrainEntity)
            {
                if (permission.UserHasPermission(player.UserIDString, permConductor)) return null;
                if (permission.UserHasPermission(player.UserIDString, permAdmin)) return null;
                else return false;
            }
            return null;
        }

        private object CanPickupEntity(BasePlayer player, BaseCombatEntity entity)
        {
            if (entity == null || player == null) return null;
            if (entity.GetComponentInParent<TrainEntity>()) return false;
            return null;
        }

        private void OnPlayerInput(BasePlayer basePlayer, InputState input)
        {
            if (input.WasJustPressed(BUTTON.FIRE_THIRD))
            {
                if (playersInTrackMode.Contains(basePlayer.userID))
                {
                    var hasEditor = basePlayer.GetComponent<TrackEditGUI>();
                    if (hasEditor)
                    {
                        if (!hasEditor.enableMiddleMouse) return;
                        string trackName = hasEditor.trackstring;
                        if (storedData.trainTrackData.ContainsKey(trackName))
                        {
                            storedData.trainTrackData[trackName].trackMarkers.Add(basePlayer.transform.position);
                            SaveData();
                            hasEditor.RefreshTrackList(basePlayer, trackName);
                            if (basePlayer != null && basePlayer.net?.connection?.authLevel >= 1) basePlayer.SendConsoleCommand("ddraw.text", new object[] { 10.0f, Color.red, basePlayer.transform.position + Vector3.up, "X" });
                        }
                    }
                }
            }
        }

        private void RespawnAllTrains()
        {
            DestroyAll<TrainEntity>();
            if (storedData.trainTrackData.Any())
            {
                foreach (string listname in storedData.trainTrackData.Keys)
                {
                    if (storedData.trainTrackData[listname].trackMarkers.Count > 0 && storedData.trainTrackData[listname].autospawn) AddTrainEntityToTrack(listname);
                }
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!config.trainSettings.ejectSleepers) return;
            if (player.net?.connection?.authLevel >= 0)
            {
                var onTrain = player.GetComponentInParent<TrainEntity>();
                if (onTrain) player.SetParent(null, true, true);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (!config.trainSettings.ejectSleepers) return;
            var onTrain = player.GetComponentInParent<TrainEntity>();
            if (onTrain) player.SetParent(null, true, true);
        }

        private void Unload()
        {
            DestroyAll<TrainEntity>();
            DestroyAll<TrackEditGUI>();
            SaveData();
        }

        private static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        #endregion

        #region Trolley Entity

        class TrainEntity : MonoBehaviour
        {
            Trains instance;
            BaseEntity railCart;
            BoxCollider boxcollider;
            Rigidbody rigidHitBody;
            int traintype;
            float steps;

            BaseEntity furnace1, furnace2, frontwall, frontdoor, midwall, rearwall, reardoor;

            BaseEntity frontsign;
            BaseEntity frontDoorWay, rearDoorWay;
            BaseEntity frontLowWall, rearLowWall;
            BaseEntity floor1, floor2, floor3, floor4;
            BaseEntity ceiling1, ceiling2, ceiling3, ceiling4;
            BaseEntity rightwall1, rightwall2, rightwall3, rigthwall4;
            BaseEntity leftwall1, leftwall2, leftwall3, leftwall4;
            BaseEntity wheel1, wheel2, wheel3, wheel4, wheel5, wheel6, wheel7, wheel8;
            BaseEntity lightright, lightleft;
            BaseEntity lightright2, lightleft2;
            BaseEntity logoleft1, logoleft2, logoright1, logoright2;
            BaseEntity chair1, chair2, chair3, chair4, chair5, chair6;
            BaseEntity seatback1, seatback2, seat1, seat2;
            BaseEntity rowBoat;

            public BaseEntity frontlock;
            public BaseEntity frontswitch;
            int counter;
            public bool npcmove;
            bool findnext;
            bool moveforward;
            bool findnextstop;
            public bool loopmovement;
            bool useAngleSpeed;
            bool spawncomplete;
            bool autoStartTrain;
            bool setGameObjectActive;
            float maxSpeed;
            float stopWait;
            string lockCodestr;
            string usingListNamed;

            public List<Vector3> movetolist;
            public List<Vector3> stoplist;

            public Vector3 movetopoint;
            Vector3 lastStopPos;
            Vector3 currentPos, targetDir, newDir;

            string floorprefab = "assets/prefabs/building core/floor/floor.prefab";
            string frameprefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab";
            string wheelprefab = "assets/prefabs/deployable/spinner_wheel/spinner.wheel.deployed.prefab";
            string wallprefab = "assets/prefabs/building core/wall/wall.prefab";
            string windowwallprefab = "assets/prefabs/building core/wall.window/wall.window.prefab";
            string wallframeprefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab";
            string doorwayprefab = "assets/prefabs/building core/wall.doorway/wall.doorway.prefab";
            string solorpanelprefab = "assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab";
            string batteryprefab = "assets/prefabs/deployable/playerioents/batteries/smallrechargablebattery.deployed.prefab";
            string lightprefab = "assets/prefabs/tools/flashlight/flashlight.entity.prefab";
            string lowwallprefab = "assets/prefabs/building core/wall.low/wall.low.prefab";
            string simplelightprefab = "assets/prefabs/deployable/ceiling light/ceilinglight.deployed.prefab";
            string doorcontroller = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
            string rugprefab = "assets/prefabs/deployable/rug/rug.deployed.prefab";
            string refineryprefab = "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab";
            string garagedoorprefab = "assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab";
            string chairprefab = "assets/bundled/prefabs/static/chair.invisible.static.prefab";
            string seatprefab = "assets/prefabs/deployable/hitch & trough/hitchtrough.deployed.prefab";
            string customPrefab = config.trainSettings.customPrefabStr;

            private void Awake()
            {
                instance = new Trains();
                movetopoint = new Vector3();
                movetolist = new List<Vector3>();
                stoplist = new List<Vector3>();
                lastStopPos = new Vector3();
                railCart = GetComponentInParent<BaseEntity>();
                currentPos = railCart.transform.position;
                counter = 0;
                steps = 0f;
                moveforward = true;
                findnext = true;
                findnextstop = true;
                spawncomplete = false;
                loopmovement = false;
                traintype = 1;
                npcmove = true;
                setGameObjectActive = false;
                useAngleSpeed = config.trainSettings.useAngleSpeed;
            }

            public void SpawnTrain(string listname)
            {
                usingListNamed = listname;
                if (listname == null) { print("Debug...no list for trains to spawn on !!"); return; }
                movetopoint = storedData.trainTrackData[listname].trackMarkers.ElementAtOrDefault(0);
                movetolist = storedData.trainTrackData[listname].trackMarkers;
                stoplist = storedData.trainTrackData[listname].trainStops;
                loopmovement = storedData.trainTrackData[listname].looping;
                traintype = storedData.trainTrackData[listname].trainType;
                autoStartTrain = storedData.trainTrackData[listname].autostart;
                maxSpeed = storedData.trainTrackData[listname].maxSpeed;
                stopWait = storedData.trainTrackData[listname].stopWaitTime;
                SpawnRailBase();
            }

            BaseEntity SpawnPart(string prefab, BaseEntity entitypart, bool setactive, int eulangx, int eulangy, int eulangz, float locposx, float locposy, float locposz, BaseEntity parent, ulong skinid)
            {
                entitypart = new BaseEntity();
                entitypart = GameManager.server.CreateEntity(prefab, railCart.transform.position, railCart.transform.rotation, setactive);
                entitypart.transform.localEulerAngles = new Vector3(eulangx, eulangy, eulangz);
                entitypart.transform.localPosition = new Vector3(locposx, locposy, locposz);
                entitypart.SetParent(parent, 0, false, false);
                entitypart.skinID = skinid;
                entitypart.enableSaving = false;
                entitypart?.Spawn();
                SpawnRefresh(entitypart);
                return entitypart;
            }

            private void SpawnRailBase()
            {
                if (traintype == 1) SpawnRailCart1();
                if (traintype == 2) SpawnRailCart2();
                if (traintype == 3) SpawnEngine();
                if (traintype == 4) SpawnCoasterCar();
                if (traintype == 5) SpawnCustomPrefab();
            }

            private void SpawnCoasterCar()
            {
                frontswitch = SpawnPart(doorcontroller, frontswitch, setGameObjectActive, 0, 180, 0, -0.5f, 0.5f, 1.4f, railCart, 1);
                frontswitch.SetFlag(BaseEntity.Flags.Reserved8, true, false);
                frontswitch.SetFlag(BaseEntity.Flags.On, autoStartTrain, false);

                floor1 = SpawnPart(floorprefab, floor1, setGameObjectActive, 0, 0, 0, 0f, 1f, 0f, railCart, 1);

                chair1 = SpawnPart(chairprefab, chair1, false, 0, 0, 0, 1.0f, 1f, 0.8f, railCart, 1);
                chair2 = SpawnPart(chairprefab, chair2, false, 0, 0, 0, 0.0f, 1f, 0.8f, railCart, 1);
                chair3 = SpawnPart(chairprefab, chair3, false, 0, 0, 0, -1.0f, 1f, 0.8f, railCart, 1);

                chair4 = SpawnPart(chairprefab, chair4, false, 0, 0, 0, 1.0f, 1f, -0.8f, railCart, 1);
                chair5 = SpawnPart(chairprefab, chair5, false, 0, 0, 0, 0.0f, 1f, -0.8f, railCart, 1);
                chair6 = SpawnPart(chairprefab, chair6, false, 0, 0, 0, -1.0f, 1f, -0.8f, railCart, 1);

                seatback1 = SpawnPart(lowwallprefab, seatback1, false, 0, 270, 10, 0.0f, 1f, 0.1f, railCart, 1);
                seatback2 = SpawnPart(lowwallprefab, seatback2, false, 0, 270, 10, 0.0f, 1f, -1.4f, railCart, 1);

                seat1 = SpawnPart(lowwallprefab, seat1, false, 0, 270, 90, 0.0f, 1.4f, 1.0f, railCart, 1);
                seat2 = SpawnPart(lowwallprefab, seat2, false, 0, 270, 90, 0.0f, 1.4f, -0.5f, railCart, 1);

                rightwall1 = SpawnPart(lowwallprefab, rightwall1, setGameObjectActive, 0, 0, 0, 1.4f, 0.7f, 0.0f, railCart, 1);
                leftwall1 = SpawnPart(lowwallprefab, leftwall1, setGameObjectActive, 0, 180, 0, -1.4f, 0.7f, 0.0f, railCart, 1);

                frontLowWall = SpawnPart(lowwallprefab, frontDoorWay, setGameObjectActive, 0, 270, 0, 0f, 0.7f, 1.5f, railCart, 1);
                rearLowWall = SpawnPart(lowwallprefab, rearDoorWay, setGameObjectActive, 0, 90, 0, 0f, 0.7f, -1.5f, railCart, 1);

                lightleft = SpawnPart(lightprefab, lightleft, true, -15, 10, 0, 1.1f, 1f, 1.5f, railCart, 1);
                lightright = SpawnPart(lightprefab, lightright, true, -15, 10, 0, -1.1f, 1f, 1.5f, railCart, 1);

                lightleft2 = SpawnPart(lightprefab, lightleft, true, -15, 190, 0, 1.1f, 1f, -1.5f, railCart, 1);
                lightright2 = SpawnPart(lightprefab, lightright, true, -15, 190, 0, -1.1f, 1f, -1.5f, railCart, 1);

                wheel1 = SpawnPart(wheelprefab, wheel1, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, 1.0f, railCart, 1);
                wheel2 = SpawnPart(wheelprefab, wheel2, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, 1.0f, railCart, 1);
                wheel3 = SpawnPart(wheelprefab, wheel3, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, -1.0f, railCart, 1);
                wheel4 = SpawnPart(wheelprefab, wheel4, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, -1.0f, railCart, 1);

                spawncomplete = true;
            }

            private void SpawnEngine()
            {
                frontswitch = SpawnPart(doorcontroller, frontswitch, setGameObjectActive, 0, 180, 0, -0.3f, 0.9f, 0f, railCart, 1);
                frontswitch.SetFlag(BaseEntity.Flags.Reserved8, true, false);
                frontswitch.SetFlag(BaseEntity.Flags.On, autoStartTrain, false);

                lightleft = SpawnPart(lightprefab, lightleft, false, -15, 0, 0, 1.1f, 1f, 6.1f, railCart, 1);
                lightright = SpawnPart(lightprefab, lightright, false, -15, 0, 0, -1.1f, 1f, 6.1f, railCart, 1);

                frontwall = SpawnPart(lowwallprefab, frontwall, false, 0, 270, 0, 0f, 1f, 6f, railCart, 1);
                midwall = SpawnPart(windowwallprefab, midwall, false, 0, 270, 0, 0f, 1f, 0f, railCart, 1);
                rearwall = SpawnPart(wallframeprefab, rearwall, false, 0, 90, 0, 0f, 1f, -6f, railCart, 1);
                reardoor = SpawnPart(garagedoorprefab, reardoor, false, 0, 90, 0, 0f, 1f, -6f, railCart, 1);

                furnace1 = SpawnPart(refineryprefab, furnace1, false, 0, 0, 0, 0f, 1.5f, 4f, railCart, 1);
                furnace2 = SpawnPart(refineryprefab, furnace2, false, 0, 0, 0, 0f, 1.5f, 2.5f, railCart, 1);

                floor1 = SpawnPart(floorprefab, floor1, false, 0, 0, 0, 0f, 1f, 4.5f, railCart, 1);
                floor2 = SpawnPart(floorprefab, floor2, false, 0, 0, 0, 0f, 1f, 1.5f, railCart, 1);
                floor3 = SpawnPart(floorprefab, floor1, false, 0, 0, 0, 0f, 1f, -1.5f, railCart, 1);
                floor4 = SpawnPart(floorprefab, floor2, false, 0, 0, 0, 0f, 1f, -4.5f, railCart, 1);

                ceiling1 = SpawnPart(floorprefab, ceiling1, false, 0, 0, 0, 0f, 2f, 4.5f, railCart, 1);
                ceiling2 = SpawnPart(floorprefab, ceiling2, false, 0, 0, 0, 0f, 2f, 1.5f, railCart, 1);
                ceiling3 = SpawnPart(floorprefab, ceiling3, false, 0, 0, 0, 0f, 4f, -1.5f, railCart, 1);
                ceiling4 = SpawnPart(floorprefab, ceiling4, false, 0, 0, 0, 0f, 4f, -4.5f, railCart, 1);

                rightwall1 = SpawnPart(lowwallprefab, rightwall1, false, 0, 0, 0, 1.5f, 1f, 4.5f, railCart, 1);
                rightwall2 = SpawnPart(lowwallprefab, rightwall2, false, 0, 0, 0, 1.5f, 1f, 1.5f, railCart, 1);
                rightwall3 = SpawnPart(windowwallprefab, rightwall3, false, 0, 0, 0, 1.5f, 1f, -1.5f, railCart, 1);
                rigthwall4 = SpawnPart(doorwayprefab, rigthwall4, false, 0, 0, 0, 1.5f, 1f, -4.5f, railCart, 1);

                leftwall1 = SpawnPart(lowwallprefab, leftwall1, false, 0, 180, 0, -1.5f, 1f, 4.5f, railCart, 1);
                leftwall2 = SpawnPart(lowwallprefab, leftwall2, false, 0, 180, 0, -1.5f, 1f, 1.5f, railCart, 1);
                leftwall3 = SpawnPart(windowwallprefab, leftwall3, false, 0, 180, 0, -1.5f, 1f, -1.5f, railCart, 1);
                leftwall4 = SpawnPart(doorwayprefab, leftwall4, false, 0, 180, 0, -1.5f, 1f, -4.5f, railCart, 1);

                wheel1 = SpawnPart(wheelprefab, wheel1, false, 90, 0, 90, 0.75f, 0.5f, 4f, railCart, 1);
                wheel2 = SpawnPart(wheelprefab, wheel2, false, 90, 0, 90, 0.75f, 0.5f, 2.5f, railCart, 1);
                wheel3 = SpawnPart(wheelprefab, wheel3, false, 90, 0, 90, -0.75f, 0.5f, 4f, railCart, 1);
                wheel4 = SpawnPart(wheelprefab, wheel4, false, 90, 0, 90, -0.75f, 0.5f, 2.5f, railCart, 1);

                wheel5 = SpawnPart(wheelprefab, wheel5, false, 90, 0, 90, 0.75f, 0.5f, -4f, railCart, 1);
                wheel6 = SpawnPart(wheelprefab, wheel6, false, 90, 0, 90, 0.75f, 0.5f, -2.5f, railCart, 1);
                wheel7 = SpawnPart(wheelprefab, wheel7, false, 90, 0, 90, -0.75f, 0.5f, -4f, railCart, 1);
                wheel8 = SpawnPart(wheelprefab, wheel8, false, 90, 0, 90, -0.75f, 0.5f, -2.5f, railCart, 1);

                boxcollider = railCart.gameObject.AddComponent<BoxCollider>();
                boxcollider.gameObject.layer = (int)Layer.Reserved1;
                boxcollider.isTrigger = true;
                boxcollider.center = new Vector3(0f, 4.1f, 0f);
                // left/right   up/down   front/back
                boxcollider.size = new Vector3(3f, 4.5f, 12f);

                rigidHitBody = boxcollider.gameObject.AddComponent<Rigidbody>();
                rigidHitBody.isKinematic = true;
                rigidHitBody.detectCollisions = true;
                rigidHitBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidHitBody.useGravity = false;

                spawncomplete = true;
            }

            private void SpawnRailCart1()
            {
                frontswitch = SpawnPart(doorcontroller, frontswitch, setGameObjectActive, 0, 180, 0, -0.3f, 0.9f, 5.9f, railCart, 1);
                frontswitch.SetFlag(BaseEntity.Flags.Reserved8, true, false);
                frontswitch.SetFlag(BaseEntity.Flags.On, autoStartTrain, false);

                lightleft = SpawnPart(lightprefab, lightleft, true, -15, 10, 0, 1.1f, 1f, 6.1f, railCart, 1);
                lightright = SpawnPart(lightprefab, lightright, true, -15, 10, 0, -1.1f, 1f, 6.1f, railCart, 1);

                lightleft2 = SpawnPart(lightprefab, lightleft, true, -15, 190, 0, 1.1f, 1f, -6.1f, railCart, 1);
                lightright2 = SpawnPart(lightprefab, lightright, true, -15, 190, 0, -1.1f, 1f, -6.1f, railCart, 1);

                floor1 = SpawnPart(floorprefab, floor1, setGameObjectActive, 0, 0, 0, 0f, 1f, 4.5f, railCart, 1);
                floor2 = SpawnPart(floorprefab, floor2, setGameObjectActive, 0, 0, 0, 0f, 1f, 1.5f, railCart, 1);
                floor3 = SpawnPart(floorprefab, floor1, setGameObjectActive, 0, 0, 0, 0f, 1f, -1.5f, railCart, 1);
                floor4 = SpawnPart(floorprefab, floor2, setGameObjectActive, 0, 0, 0, 0f, 1f, -4.5f, railCart, 1);

                wheel1 = SpawnPart(wheelprefab, wheel1, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, 4f, railCart, 1);
                wheel2 = SpawnPart(wheelprefab, wheel2, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, 2.5f, railCart, 1);
                wheel3 = SpawnPart(wheelprefab, wheel3, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, 4f, railCart, 1);
                wheel4 = SpawnPart(wheelprefab, wheel4, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, 2.5f, railCart, 1);

                wheel5 = SpawnPart(wheelprefab, wheel5, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, -4f, railCart, 1);
                wheel6 = SpawnPart(wheelprefab, wheel6, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, -2.5f, railCart, 1);
                wheel7 = SpawnPart(wheelprefab, wheel7, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, -4f, railCart, 1);
                wheel8 = SpawnPart(wheelprefab, wheel8, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, -2.5f, railCart, 1);

                frontDoorWay = SpawnPart(windowwallprefab, frontDoorWay, setGameObjectActive, 0, 270, 0, 0f, 1f, 6f, railCart, 1);
                rearDoorWay = SpawnPart(windowwallprefab, rearDoorWay, setGameObjectActive, 0, 90, 0, 0f, 1f, -6f, railCart, 1);

                rightwall1 = SpawnPart(doorwayprefab, rightwall1, setGameObjectActive, 0, 0, 0, 1.5f, 1f, 4.5f, railCart, 1);
                rightwall2 = SpawnPart(windowwallprefab, rightwall2, setGameObjectActive, 0, 0, 0, 1.5f, 1f, 1.5f, railCart, 1);
                rightwall3 = SpawnPart(windowwallprefab, rightwall3, setGameObjectActive, 0, 0, 0, 1.5f, 1f, -1.5f, railCart, 1);
                rigthwall4 = SpawnPart(doorwayprefab, rigthwall4, setGameObjectActive, 0, 0, 0, 1.5f, 1f, -4.5f, railCart, 1);

                leftwall1 = SpawnPart(doorwayprefab, leftwall1, setGameObjectActive, 0, 180, 0, -1.5f, 1f, 4.5f, railCart, 1);
                leftwall2 = SpawnPart(windowwallprefab, leftwall2, setGameObjectActive, 0, 180, 0, -1.5f, 1f, 1.5f, railCart, 1);
                leftwall3 = SpawnPart(windowwallprefab, leftwall3, setGameObjectActive, 0, 180, 0, -1.5f, 1f, -1.5f, railCart, 1);
                leftwall4 = SpawnPart(doorwayprefab, leftwall4, setGameObjectActive, 0, 180, 0, -1.5f, 1f, -4.5f, railCart, 1);

                ceiling1 = SpawnPart(floorprefab, ceiling1, setGameObjectActive, 0, 0, 0, 0f, 4f, 4.5f, railCart, 1);
                ceiling2 = SpawnPart(floorprefab, ceiling2, setGameObjectActive, 0, 0, 0, 0f, 4f, 1.5f, railCart, 1);
                ceiling3 = SpawnPart(floorprefab, ceiling3, setGameObjectActive, 0, 0, 0, 0f, 4f, -1.5f, railCart, 1);
                ceiling4 = SpawnPart(floorprefab, ceiling4, setGameObjectActive, 0, 0, 0, 0f, 4f, -4.5f, railCart, 1);

                boxcollider = railCart.gameObject.AddComponent<BoxCollider>();
                boxcollider.gameObject.layer = (int)Layer.Reserved1;
                boxcollider.isTrigger = true;
                boxcollider.center = new Vector3(0f, 4.1f, 0f);
                // left/right   up/down   front/back
                boxcollider.size = new Vector3(3f, 4.5f, 12f);

                rigidHitBody = boxcollider.gameObject.AddComponent<Rigidbody>();
                rigidHitBody.isKinematic = true;
                rigidHitBody.detectCollisions = true;
                rigidHitBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidHitBody.useGravity = false;

                spawncomplete = true;
            }

            private void SpawnRailCart2()
            {
                frontswitch = SpawnPart(doorcontroller, frontswitch, setGameObjectActive, 0, 180, 0, -0.3f, 0.9f, 5.9f, railCart, 1);
                frontswitch.SetFlag(BaseEntity.Flags.Reserved8, true, false);
                frontswitch.SetFlag(BaseEntity.Flags.On, autoStartTrain, false);

                lightleft = SpawnPart(lightprefab, lightleft, setGameObjectActive, -15, 10, 0, 1.1f, 1f, 6.1f, railCart, 1);
                lightright = SpawnPart(lightprefab, lightright, setGameObjectActive, -15, 10, 0, -1.1f, 1f, 6.1f, railCart, 1);

                lightleft2 = SpawnPart(lightprefab, lightleft, setGameObjectActive, -15, 190, 0, 1.1f, 1f, -6.1f, railCart, 1);
                lightright2 = SpawnPart(lightprefab, lightright, setGameObjectActive, -15, 190, 0, -1.1f, 1f, -6.1f, railCart, 1);

                floor1 = SpawnPart(floorprefab, floor1, setGameObjectActive, 0, 0, 0, 0f, 1f, 4.5f, railCart, 1);
                floor2 = SpawnPart(floorprefab, floor2, setGameObjectActive, 0, 0, 0, 0f, 1f, 1.5f, railCart, 1);
                floor3 = SpawnPart(floorprefab, floor1, setGameObjectActive, 0, 0, 0, 0f, 1f, -1.5f, railCart, 1);
                floor4 = SpawnPart(floorprefab, floor2, setGameObjectActive, 0, 0, 0, 0f, 1f, -4.5f, railCart, 1);

                wheel1 = SpawnPart(wheelprefab, wheel1, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, 4f, railCart, 1);
                wheel2 = SpawnPart(wheelprefab, wheel2, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, 2.5f, railCart, 1);
                wheel3 = SpawnPart(wheelprefab, wheel3, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, 4f, railCart, 1);
                wheel4 = SpawnPart(wheelprefab, wheel4, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, 2.5f, railCart, 1);

                wheel5 = SpawnPart(wheelprefab, wheel5, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, -4f, railCart, 1);
                wheel6 = SpawnPart(wheelprefab, wheel6, setGameObjectActive, 90, 0, 90, 0.75f, 0.5f, -2.5f, railCart, 1);
                wheel7 = SpawnPart(wheelprefab, wheel7, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, -4f, railCart, 1);
                wheel8 = SpawnPart(wheelprefab, wheel8, setGameObjectActive, 90, 0, 90, -0.75f, 0.5f, -2.5f, railCart, 1);

                frontDoorWay = SpawnPart(doorwayprefab, frontDoorWay, setGameObjectActive, 0, 270, 0, 0f, 1f, 4.5f, railCart, 1);
                rearDoorWay = SpawnPart(doorwayprefab, rearDoorWay, setGameObjectActive, 0, 90, 0, 0f, 1f, -4.5f, railCart, 1);

                frontLowWall = SpawnPart(lowwallprefab, frontDoorWay, setGameObjectActive, 0, 270, 0, 0f, 1f, 6f, railCart, 1);
                rearLowWall = SpawnPart(lowwallprefab, rearDoorWay, setGameObjectActive, 0, 90, 0, 0f, 1f, -6f, railCart, 1);

                rightwall1 = SpawnPart(windowwallprefab, rightwall1, setGameObjectActive, 0, 0, 0, 1.5f, 1f, 3.0f, railCart, 1);
                rightwall2 = SpawnPart(windowwallprefab, rightwall2, setGameObjectActive, 0, 0, 0, 1.5f, 1f, 1.5f, railCart, 1);
                rightwall3 = SpawnPart(windowwallprefab, rightwall3, setGameObjectActive, 0, 0, 0, 1.5f, 1f, -1.5f, railCart, 1);
                rigthwall4 = SpawnPart(windowwallprefab, rigthwall4, setGameObjectActive, 0, 0, 0, 1.5f, 1f, -3.0f, railCart, 1);

                leftwall1 = SpawnPart(windowwallprefab, leftwall1, setGameObjectActive, 0, 180, 0, -1.5f, 1f, 3.0f, railCart, 1);
                leftwall2 = SpawnPart(windowwallprefab, leftwall2, setGameObjectActive, 0, 180, 0, -1.5f, 1f, 1.5f, railCart, 1);
                leftwall3 = SpawnPart(windowwallprefab, leftwall3, setGameObjectActive, 0, 180, 0, -1.5f, 1f, -1.5f, railCart, 1);
                leftwall4 = SpawnPart(windowwallprefab, leftwall4, setGameObjectActive, 0, 180, 0, -1.5f, 1f, -3.0f, railCart, 1);

                ceiling1 = SpawnPart(floorprefab, ceiling1, setGameObjectActive, 0, 0, 0, 0f, 4f, 4.5f, railCart, 1);
                ceiling2 = SpawnPart(floorprefab, ceiling2, setGameObjectActive, 0, 0, 0, 0f, 4f, 1.5f, railCart, 1);
                ceiling3 = SpawnPart(floorprefab, ceiling3, setGameObjectActive, 0, 0, 0, 0f, 4f, -1.5f, railCart, 1);
                ceiling4 = SpawnPart(floorprefab, ceiling4, setGameObjectActive, 0, 0, 0, 0f, 4f, -4.5f, railCart, 1);

                boxcollider = railCart.gameObject.AddComponent<BoxCollider>();
                boxcollider.gameObject.layer = (int)Layer.Reserved1;
                boxcollider.isTrigger = true;
                boxcollider.center = new Vector3(0f, 4.1f, 0f);
                // left/right   up/down   front/back
                boxcollider.size = new Vector3(3f, 4.5f, 12f);

                rigidHitBody = boxcollider.gameObject.AddComponent<Rigidbody>();
                rigidHitBody.isKinematic = true;
                rigidHitBody.detectCollisions = true;
                rigidHitBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidHitBody.useGravity = false;

                spawncomplete = true;
            }

            private void SpawnCustomPrefab()
            {
                frontswitch = SpawnPart(doorcontroller, frontswitch, setGameObjectActive, 0, 0, 0, 0f, 0f, 0f, railCart, 1);
                frontswitch.SetFlag(BaseEntity.Flags.Reserved8, true, false);
                frontswitch.SetFlag(BaseEntity.Flags.On, autoStartTrain, false);
                rowBoat = SpawnPart(customPrefab, rowBoat, setGameObjectActive, 0, 0, 0, 0f, 0f, 0f, railCart, 1);
                spawncomplete = true;
            }

            private void SpawnRefresh(BaseNetworkable entity1)
            {
                var hasstab = entity1.GetComponent<StabilityEntity>();
                if (entity1.GetComponent<StabilityEntity>())
                {
                    hasstab.grounded = true;
                }
                var hasblock = entity1.GetComponent<BuildingBlock>();
                if (hasblock)
                {
                    hasblock.SetGrade(BuildingGrade.Enum.Metal);
                    hasblock.SetHealthToMax();
                    hasblock.UpdateSkin();
                    hasblock.ClientRPC(null, "RefreshSkin");
                }
            }

            void OnTriggerEnter(Collider col)
            {
                if (col.name.Contains("/player/player.prefab"))
                {
                    var player = col.GetComponentInParent<BasePlayer>() ?? null;
                    if (player != null && player.isMounted)
                    {
                        return;
                    }
                    if (player != null && player.IsSleeping())
                    {
                        return;
                    }
                    if (player.GetParentEntity() == base.gameObject.ToBaseEntity())
                    {
                        return;
                    }
                    if (player != null)
                    {
                        BaseEntity getpar = player.GetParentEntity() ?? null;
                        if (getpar == null)
                        {
                            player.SetParent(railCart, true, true);
                            player.PauseFlyHackDetection(99999f);
                            player.PauseSpeedHackDetection(99999f);
                            player.PauseVehicleNoClipDetection(99999f);
                        }
                    }
                    return;
                }
                if (config.trainSettings.damageOnCollision)
                {
                    var getEntity = col.GetComponentInParent<BaseCombatEntity>();
                    if (getEntity)
                    {
                        getEntity.Hurt(2500f, Rust.DamageType.Explosion, null, true);
                    }
                }
            }

            void OnTriggerExit(Collider col)
            {
                if (col.name.Contains("/player/player.prefab"))
                {
                    var player = col.GetComponentInParent<BasePlayer>() ?? null;
                    if (player != null)
                    {
                        if (player != null && player.IsSleeping())
                        {
                            player.SetParent(null, true, true);
                            return;
                        }
                        if (player.GetParentEntity() != base.gameObject.ToBaseEntity())
                        {
                            return;
                        }
                        player.SetParent(null, true, true);
                        player.PauseFlyHackDetection(5f);
                        player.PauseSpeedHackDetection(5f);
                        player.PauseVehicleNoClipDetection(5f);
                    }
                }
            }

            private void applyBlastDamage(BasePlayer player, float damageamount, float radius, Rust.DamageType damagetype, Vector3 location)
            {
                List<BaseCombatEntity> entityList = Pool.GetList<BaseCombatEntity>();
                Vis.Entities<BaseCombatEntity>(location, radius, entityList);

                foreach (BaseCombatEntity combatentity in entityList)
                {
                    if (!(combatentity is BuildingPrivlidge))
                    {
                        combatentity.Hurt(damageamount, damagetype, player, true);
                    }
                }
                Pool.FreeList<BaseCombatEntity>(ref entityList);
            }

            private void FindNextBusStop()
            {
                Vector3 currentPosition = railCart.transform.position;

                foreach (Vector3 busstops in stoplist)
                {
                    Vector3 directionToTarget = busstops - currentPosition;
                    float dSqrToTarget = directionToTarget.sqrMagnitude;
                    if (dSqrToTarget < 12f && dSqrToTarget > 4f)
                    {
                        findnext = false;
                        frontswitch.SetFlag(BaseEntity.Flags.On, false, false);
                        instance.timer.Once(stopWait, () => { frontswitch.SetFlag(BaseEntity.Flags.On, true, false); });
                        instance.timer.Once(stopWait + 5f, () => { findnext = true; });
                    }
                }
            }

            private Vector3 FindCoords()
            {
                movetolist = storedData.trainTrackData[usingListNamed].trackMarkers;
                Vector3 point1 = movetolist.ElementAtOrDefault(0);
                if (moveforward) counter++;
                else counter--;
                point1 = movetolist.ElementAtOrDefault(counter);
                return point1;
            }

            private void FixedUpdate()
            {
                if (!spawncomplete) return;
                if (frontswitch != null && frontswitch.IsOn() && steps < maxSpeed) steps += 2f;
                if (frontswitch != null && !frontswitch.IsOn()) steps -= 0.5f;

                if (npcmove)
                {
                    currentPos = railCart.transform.position;
                    if (movetopoint == new Vector3(0f, 0f, 0f)) return;
                    if (findnext && findnextstop) FindNextBusStop();
                    if (loopmovement && currentPos == movetolist.Last()) { movetopoint = movetolist.ElementAtOrDefault(0); counter = 0; moveforward = true; }
                    else if (!loopmovement && moveforward && currentPos == movetolist.Last()) { counter = movetolist.Count; moveforward = false; }
                    else if (!loopmovement && !moveforward && currentPos == movetolist.ElementAtOrDefault(0)) { counter = 0; moveforward = true; }
                    if (currentPos == movetopoint)
                    {
                        movetopoint = FindCoords();
                    }
                    if (moveforward) targetDir = movetopoint - railCart.transform.position;
                    else targetDir = railCart.transform.position - movetopoint;

                    newDir = Vector3.RotateTowards(transform.forward, targetDir, 2.5f * Time.deltaTime, 0.0F);

                    if (useAngleSpeed)
                    {
                        var angleOA = (Convert.ToInt32(newDir.y * 100f));
                        if (moveforward && angleOA > 10f) maxSpeed = storedData.trainTrackData[usingListNamed].maxSpeed * 0.5f;
                        else if (moveforward && angleOA < -10f) maxSpeed = storedData.trainTrackData[usingListNamed].maxSpeed * 5f;
                        else if (!moveforward && angleOA > 10f) maxSpeed = storedData.trainTrackData[usingListNamed].maxSpeed * 5f;
                        else if (!moveforward && angleOA < -10f) maxSpeed = storedData.trainTrackData[usingListNamed].maxSpeed * 0.5f;
                        else maxSpeed = storedData.trainTrackData[usingListNamed].maxSpeed;
                    }

                    if (steps > maxSpeed) steps = steps -= 0.5f;
                    if (steps <= 0f) steps = 0f;

                    railCart.transform.position = Vector3.MoveTowards(transform.position, movetopoint, (steps) * Time.deltaTime);
                    railCart.transform.rotation = Quaternion.LookRotation(newDir);
                    ServerMgr.Instance.StartCoroutine(RefreshTrain());
                }

            }

            private IEnumerator RefreshTrain()
            {
                railCart.transform.hasChanged = true;
                for (int i = 0; i < railCart.children.Count; i++)
                {
                    if (railCart.children[i] is BuildingBlock)
                    {
                        var isblock = (BuildingBlock)railCart.children[i];
                        isblock.ClientRPC(null, "RefreshSkin");
                    }
                    if (railCart.children[i] is SpinnerWheel)
                    {
                        railCart.children[i].transform.hasChanged = true;
                        railCart.children[i].SendNetworkUpdateImmediate();
                    }
                }
                railCart.SendNetworkUpdateImmediate();
                yield return new WaitForEndOfFrame();
            }

            private void OnDestroy()
            {
                if (boxcollider != null) GameObject.Destroy(boxcollider);
                if (railCart != null) railCart.Kill(BaseNetworkable.DestroyMode.None);
                GameObject.Destroy(this);
            }
        }

        #endregion

        #region Track Edit GUI

        class TrackEditGUI : MonoBehaviour
        {
            BasePlayer player;
            Trains instance;
            public string trackstring;
            int typeOfTrain;
            bool loopingTrain;
            bool autoSpawn;
            bool autoStart;
            float maxSpeed;
            float waitTime;
            int waypointCount;
            int stoppointCount;
            string redcolor;
            string greencolor;
            string orangecolor;
            string blackcolor;
            string bluecolor;
            Double guiMax;
            Double guiMin;
            Double guiIncrementor;
            public bool enableShowLines = false;
            public bool enableShowText = false;
            public bool enableShowStops = false;
            public bool enableMiddleMouse = false;
            bool debugShowCompleted = false;

            string buttonShowLinesColor;
            string buttonShowTextColor;
            string middleMouseColor;
            string buttonShowStopsColor;

            CuiElementContainer trackEditGUI;
            CuiElementContainer backgroundGUI;
            CuiElementContainer trackListCUI;

            public void Awake()
            {
                player = GetComponent<BasePlayer>();
                instance = new Trains();
                typeOfTrain = 1;
                loopingTrain = false;
                autoSpawn = true;
                autoStart = true;
                waypointCount = 0;
                stoppointCount = 0;
                redcolor = "1.0 0.3 0.3 0.7";
                greencolor = "0.0 0.7 0.0 0.7";
                orangecolor = "0.9 0.3 0.0 0.7";
                blackcolor = "0.0 0.0 0.0 0.9";
                bluecolor = "0.0 0.0 0.7 0.9";
                guiMax = 0.940;
                guiMin = 0.900;
                guiIncrementor = 0.045;
                AddBackground(player);
            }

            public void RefreshTrackList(BasePlayer guiplayer, string trackname)
            {
                trackstring = trackname;
                player = guiplayer;
                guiMax = 0.940;
                guiMin = 0.900;
                AddListTracks(player);
                AddEditGUI(player, trackname);
            }

            private void AddGUIButton(CuiElementContainer container, string command, string bcolor, string anchmin, string anchmax, string buttontxt, string buttonname, int fontsize = 10)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = command, Color = bcolor },
                    RectTransform = { AnchorMin = anchmin, AnchorMax = anchmax },
                    Text = { Text = buttontxt, FontSize = fontsize, Color = "1.0 1.0 1.0 1.0", Align = TextAnchor.MiddleCenter }
                }, "Overall", buttonname);
            }

            private void AddBackground(BasePlayer player)
            {
                DestroyBackgroundGUI(player);
                backgroundGUI = new CuiElementContainer();
                AddGUIButton(backgroundGUI, "", blackcolor, "0.30 0.665", "0.475 0.95", "", "background1", 10);
                AddGUIButton(backgroundGUI, "", blackcolor, "0.475 0.665", "0.77 0.95", "", "backgroundedit", 10);
                CuiHelper.AddUi(player, backgroundGUI);
            }

            private void AddEditGUI(BasePlayer player, string trackname)
            {
                trackstring = trackname;
                if (storedData.trainTrackData.ContainsKey(trackstring.ToLower()))
                {
                    typeOfTrain = storedData.trainTrackData[trackstring].trainType;
                    loopingTrain = storedData.trainTrackData[trackstring].looping;
                    autoSpawn = storedData.trainTrackData[trackstring].autospawn;
                    autoStart = storedData.trainTrackData[trackstring].autostart;
                    waypointCount = storedData.trainTrackData[trackstring].trackMarkers.Count;
                    stoppointCount = storedData.trainTrackData[trackstring].trainStops.Count;
                    maxSpeed = storedData.trainTrackData[trackstring].maxSpeed;
                    waitTime = storedData.trainTrackData[trackstring].stopWaitTime;
                }
                AddEditGui(player);
            }

            private void DestroyBackgroundGUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "background1");
                CuiHelper.DestroyUi(player, "backgroundedit");
            }

            public void AddListTracks(BasePlayer player)
            {
                DestroyListGUI(player);
                trackListCUI = new CuiElementContainer();

                AddGUIButton(trackListCUI, "", blackcolor, "0.28 0.665", "0.30 0.95", "<color=white>T\nR\nA\nI\nN\nS</color>", "titlebar", 25);

                if (storedData.trainTrackData.Any())
                {
                    AddGUIButton(trackListCUI, "", blackcolor, "0.37 " + guiMin.ToString(), "0.47 " + guiMax.ToString(), "<color=cyan>Avaialble Tracks</color>", "tracknames", 15);

                    guiMax -= guiIncrementor;
                    guiMin -= guiIncrementor;

                    var listbuttoncolor = blackcolor;
                    foreach (var listname in storedData.trainTrackData.Keys)
                    {
                        AddGUIButton(trackListCUI, "track.edit " + listname, listbuttoncolor = listname.ToLower() == trackstring.ToLower() ? greencolor : blackcolor, "0.37 " + guiMin.ToString(), "0.47 " + guiMax.ToString(), listname, listname, 10);
                        guiMax -= guiIncrementor;
                        guiMin -= guiIncrementor;
                    }
                }
                else
                {
                    AddGUIButton(trackListCUI, "", blackcolor, "0.37 0.900", "0.47 0.940", " <color=orange>No Tracks Listed</color>", "tracknames", 10);
                }
                AddGUIButton(trackListCUI, "trackmode", blackcolor, "0.31 0.675", "0.36 0.715", "<color=red>EXIT</color>", "exitbutton", 15);
                CuiHelper.AddUi(player, trackListCUI);
            }

            public void AddEditGui(BasePlayer player)
            {
                DestroyEditGUI(player);
                trackEditGUI = new CuiElementContainer();
                AddGUIButton(trackEditGUI, "track.enablemiddle", middleMouseColor = enableMiddleMouse ? greencolor : blackcolor, "0.31 0.900", "0.36 0.940", "<color=yellow>Toggle\n Middle Mark</color>", "togglemiddlemouse", 10);
                AddGUIButton(trackEditGUI, "track.showlines", bluecolor, "0.31 0.855", "0.36 0.895", "<color=yellow>Show\nLines</color>", "showlines", 10);
                AddGUIButton(trackEditGUI, "track.showtext", bluecolor, "0.31 0.810", "0.36 0.850", "<color=yellow>Show\nPoints</color>", "showtext", 10);
                AddGUIButton(trackEditGUI, "track.showstops", bluecolor, "0.31 0.765", "0.36 0.805", "<color=yellow>Show\nStops</color>", "showstops", 10);
                AddGUIButton(trackEditGUI, "", blackcolor, "0.48 0.900", "0.76 0.940", "<color=cyan>Editing Track : </color>" + trackstring, "traintrackname", 15);
                AddGUIButton(trackEditGUI, "", blackcolor, "0.48 0.855", "0.58 0.895", "Train Type = <color=yellow>" + typeOfTrain.ToString() + "</color>", "traintypetext", 10);
                AddGUIButton(trackEditGUI, "track.traintype " + trackstring.ToString(), blackcolor, "0.59 0.855", "0.64 0.895", "<color=yellow>Switch\nTrain Type</color>", "traintypebutton", 10);

                var showAutoSpawnColor = blackcolor;
                if (autoSpawn) showAutoSpawnColor = greencolor;
                AddGUIButton(trackEditGUI, "track.autospawn " + trackstring.ToString(), showAutoSpawnColor, "0.65 0.855", "0.70 0.895", "<color=yellow>Auto\nSpawn</color>", "autospawnbutton", 10);
                AddGUIButton(trackEditGUI, "track.maxspeed " + trackstring.ToString(), blackcolor, "0.71 0.855", "0.76 0.895", "Top Speed\n<color=yellow>" + maxSpeed.ToString() + "</color>", "maxspeedcount", 10);
                AddGUIButton(trackEditGUI, "track.waittime " + trackstring.ToString(), blackcolor, "0.71 0.810", "0.76 0.850", "Stop Wait\n<color=yellow>" + waitTime.ToString() + "</color>", "waittimecount", 10);
                AddGUIButton(trackEditGUI, "", blackcolor, "0.48 0.810", "0.58 0.850", "Track Loops = <color=yellow>" + loopingTrain.ToString() + "</color>", "loopingtext", 10);
                AddGUIButton(trackEditGUI, "track.trainloop " + trackstring.ToString(), blackcolor, "0.59 0.810", "0.64 0.850", "<color=yellow>Switch\nLooping</color>", "looptypebutton", 10);

                var showAutoStartColor = blackcolor;
                if (autoStart) showAutoStartColor = greencolor;
                AddGUIButton(trackEditGUI, "track.autostart " + trackstring.ToString(), showAutoStartColor, "0.65 0.810", "0.70 0.850", "<color=yellow>Auto\nStart</color>", "autostartbutton", 10);

                AddGUIButton(trackEditGUI, "", blackcolor, "0.48 0.765", "0.58 0.805", "Track Waypoints : <color=yellow>" + waypointCount + "</color>", "numwaypoints", 10);
                AddGUIButton(trackEditGUI, "track.mark " + trackstring, blackcolor, "0.59 0.765", "0.64 0.805", "<color=yellow>Mark New\nWaypoint</color>", "markwaypoint", 10);
                AddGUIButton(trackEditGUI, "track.deletelastmark " + trackstring.ToString(), blackcolor, "0.65 0.765", "0.70 0.805", "<color=orange>Del Last\nWaypoint</color>", "dellastwaypoint", 10);
                AddGUIButton(trackEditGUI, "", blackcolor, "0.48 0.720", "0.58 0.760", "Track Stops : <color=yellow>" + stoppointCount + "</color>", "numstops", 10);
                AddGUIButton(trackEditGUI, "track.markstop " + trackstring, blackcolor, "0.59 0.720", "0.64 0.760", "<color=yellow>Mark New\nStop</color>", "markstoppoint", 10);
                AddGUIButton(trackEditGUI, "track.deletelaststop " + trackstring.ToString(), blackcolor, "0.65 0.720", "0.70 0.760", "<color=orange>Del Last\nStop</color>", "dellaststop", 10);
                AddGUIButton(trackEditGUI, "track.remove " + trackstring, blackcolor, "0.48 0.675", "0.70 0.715", "<color=red>DELETE THIS TRACK</color>", "removetrack", 15);
                CuiHelper.AddUi(player, trackEditGUI);
            }

            private void DestroyListGUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "titlebar");
                CuiHelper.DestroyUi(player, "exitbutton");
                foreach (string listname in storedData.trainTrackData.Keys)
                {
                    CuiHelper.DestroyUi(player, listname);
                }
                CuiHelper.DestroyUi(player, "tracknames");
            }

            private void DestroyEditGUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "showlines");
                CuiHelper.DestroyUi(player, "showtext");
                CuiHelper.DestroyUi(player, "showstops");
                CuiHelper.DestroyUi(player, "togglemiddlemouse");
                CuiHelper.DestroyUi(player, "traintrackname");
                CuiHelper.DestroyUi(player, "traintypetext");
                CuiHelper.DestroyUi(player, "traintypebutton");
                CuiHelper.DestroyUi(player, "loopingtext");
                CuiHelper.DestroyUi(player, "looptypebutton");
                CuiHelper.DestroyUi(player, "autospawnbutton");
                CuiHelper.DestroyUi(player, "autostartbutton");
                CuiHelper.DestroyUi(player, "maxspeedcount");
                CuiHelper.DestroyUi(player, "waittimecount");
                CuiHelper.DestroyUi(player, "markwaypoint");
                CuiHelper.DestroyUi(player, "markstoppoint");
                CuiHelper.DestroyUi(player, "numwaypoints");
                CuiHelper.DestroyUi(player, "dellastwaypoint");
                CuiHelper.DestroyUi(player, "numstops");
                CuiHelper.DestroyUi(player, "dellaststop");
                CuiHelper.DestroyUi(player, "removetrack");
            }


            public IEnumerator ShowLines()
            {
                for (int i = 0; i < storedData.trainTrackData[trackstring].trackMarkers.Count; i++)
                {
                    if (storedData.trainTrackData[trackstring].trackMarkers[i] != storedData.trainTrackData[trackstring].trackMarkers.Last())
                    {
                        if (player != null) player.SendConsoleCommand("ddraw.line", new object[] { config.trainSettings.timeShowLines, Color.white, storedData.trainTrackData[trackstring].trackMarkers[i] + Vector3.up, storedData.trainTrackData[trackstring].trackMarkers[i + 1] + Vector3.up });
                    }
                    yield return new WaitForEndOfFrame();
                }
            }

            public IEnumerator ShowText()
            {
                for (int i = 0; i < storedData.trainTrackData[trackstring].trackMarkers.Count; i++)
                {

                    if (player != null) player.SendConsoleCommand("ddraw.text", new object[] { config.trainSettings.timeShowText, Color.white, storedData.trainTrackData[trackstring].trackMarkers[i] + Vector3.up, i.ToString() });
                    yield return new WaitForEndOfFrame();
                }
            }

            public IEnumerator ShowStops()
            {
                for (int i = 0; i < storedData.trainTrackData[trackstring].trainStops.Count; i++)
                {
                    if (player != null) player.SendConsoleCommand("ddraw.text", new object[] { config.trainSettings.timeShowStops, Color.red, storedData.trainTrackData[trackstring].trainStops[i] + Vector3.up, i.ToString() });
                    yield return new WaitForEndOfFrame();
                }
            }

            private void FixedUpdate()
            {
                if (enableShowLines) { enableShowLines = false; ServerMgr.Instance.StartCoroutine(ShowLines()); }
                if (enableShowText) { enableShowText = false; ServerMgr.Instance.StartCoroutine(ShowText()); }
                if (enableShowStops) { enableShowStops = false; ServerMgr.Instance.StartCoroutine(ShowStops()); }
            }

            public void OnDestroy()
            {
                DestroyBackgroundGUI(player);
                DestroyListGUI(player);
                DestroyEditGUI(player);
                Destroy(this);
            }
        }

        #endregion

    }
}