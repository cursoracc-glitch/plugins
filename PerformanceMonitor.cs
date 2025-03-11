using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Performance Monitor", "Orange", "1.2.7")]
    [Description("Tool for collecting information about server performance")]
    public class PerformanceMonitor : RustPlugin
    {
        #region Vars

        private const string commandString = "monitor.createreport";
        private const string commandString2 = "monitor.report";
        private PerformanceDump currentReport;

        #endregion

        #region Oxide Hooks

        private void Init()
        {

            cmd.AddConsoleCommand(commandString, this, nameof(cmdCompleteNow));
            cmd.AddConsoleCommand(commandString2, this, nameof(cmdCompleteNow));
        }

        private void OnServerInitialized()
        {
            if (config.checkTime > 0)
            {
                timer.Every(config.checkTime, CreateReport);
            }
        }

        #endregion

        #region Commands

        private void cmdCompleteNow(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false)
            {
                return;
            }

            CreateReport();
        }

        #endregion

        #region Core

        private void CreateReport()
        {
            ServerMgr.Instance.StartCoroutine(CreateActualReport());
        }

        private IEnumerator CreateActualReport()
        {
            if (currentReport != null)
            {
                yield break;
            }

            currentReport = new PerformanceDump();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            CompletePluginsReport();
            ServerMgr.Instance.StartCoroutine(CompleteEntitiesReport());

            while (!currentReport.entities.completed && config.runEntitiesReport)
            {
                Puts($"Report status: {currentReport.statusBar}% [{currentReport.entitiesChecked}/{currentReport.entitiesTotal}]");
                yield return new WaitForEndOfFrame();
            }

            SaveReport(currentReport);
            currentReport = null;
            sw.Stop();
            Puts($"Performance report was completed in {sw.Elapsed.Seconds + (sw.Elapsed.Milliseconds / 1000.0)} seconds.");
        }

        private void CompletePluginsReport()
        {
            List<string> list = Pool.GetList<string>();

            if (config.runPluginsReport == false)
            {
                return;
            }

            if (config.sortByHookTime)
            {
                foreach (var plugin in plugins.GetAll().OrderByDescending(x => x.TotalHookTime))
                {
                    ProcessPlugins(plugin, list);
                }
            }
            else
            {
#if CARBON
                foreach (var plugin in plugins.GetAll().OrderByDescending(x => x.TotalMemoryUsed))
#else
                foreach (var plugin in plugins.GetAll().OrderByDescending(x => x.TotalHookTime))
#endif
                {
                    ProcessPlugins(plugin, list);
                }
            }
            currentReport.plugins = list.ToArray();
            Pool.FreeList(ref list);
        }

        private void ProcessPlugins(Plugin plugin, List<string> list)
        {

            string name = plugin.Name;
            if (name == Name || plugin.IsCorePlugin || config.excludedPlugins.Contains(name))
            {
                return;
            }
#if CARBON
            string memory = FormatBytes(plugin.TotalMemoryUsed);
            double time = Math.Round(plugin.TotalHookTime.TotalSeconds, 6);
#else
          // string memory = FormatBytes(plugin.);
            double time = Math.Round(plugin.TotalHookTime, 6);
#endif
            VersionNumber version = plugin.Version;

            string info = $"{name} ({version}), Total Hook Time = {time}";

            list.Add(info);
        }

        private IEnumerator CompleteEntitiesReport()
        {
            if (config.runEntitiesReport == false)
            {
                yield break;
            }

            // Swapped to serverEntities.OfType<> Instead of UnityEngine.FindObjectOfType<> due to absolute absurdity of time scale between the two.
            // serverEntities took 0.0151 ms, UnityEngine took 201.12 ms.
            var entities = BaseNetworkable.serverEntities.OfType<BaseEntity>();
            var entitiesByShortname = currentReport.entities.list;

            currentReport.entitiesTotal = entities.Count();

            int count = 0;
            foreach (BaseEntity entity in entities)
            {
                currentReport.entitiesChecked++;
                currentReport.statusBar = Convert.ToInt32(count * 100 / currentReport.entitiesTotal);

                if (entity.IsValid() == false)
                {
                    continue;
                }

                var shortname = entity.ShortPrefabName;
                if (config.excludedEntities.Contains(shortname))
                {
                    continue;
                }

                EntityInfo info;
                if (entitiesByShortname.TryGetValue(shortname, out info) == false)
                {
                    info = new EntityInfo();
                    entitiesByShortname.Add(shortname, info);
                }


                if (entity.OwnerID == 0)
                {
                    info.countUnowned++;
                    currentReport.entities.countUnowned++;
                }
                else
                {
                    info.countOwned++;
                    currentReport.entities.countOwned++;
                }

                info.countGlobal++;
                currentReport.entities.countGlobal++;
            }

            currentReport.entities.list = currentReport.entities.list.OrderByDescending(x => x.Value.countGlobal).ToDictionary(x => x.Key, y => y.Value);

            currentReport.entities.completed = true;
        }

        #endregion

        #region Utils

        private void SaveReport(PerformanceDump dump)
        {
            string name1;
            if (config.EuropeanTimeSave) name1 = DateTime.Now.ToString("dd/MM/yyyy").Replace("/", "-");
            else name1 = DateTime.Now.ToString("MM/dd/yyyy").Replace("/", "-");
            string name2 = DateTime.Now.ToString(Time()).Replace(':', '-');
            string filename = $"PerformanceMonitor/Reports/{name1}/{name2}";
            Interface.Oxide.DataFileSystem.WriteObject(filename, dump);
        }

        private string Time()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        // From RustCore Class, thank you Rust/Oxide.
        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L)
                return $"{bytes} B";
            if (bytes < 1048576L)
                return $"{bytes / 1024.0} KB";
            return bytes < 1073741824L ? $"{Math.Round(bytes / 1048576.0, 3)} MB" : $"{Math.Round(bytes / 1073741824.0, 3)} GB";
        }

        private static string FormatBytes(double bytes)
        {
            if (bytes < 1024L)
                return $"{bytes} B";
            if (bytes < 1048576L)
                return $"{bytes / 1024.0} KB";
            return bytes < 1073741824L ? $"{Math.Round(bytes / 1048576.0, 3)} MB" : $"{Math.Round(bytes / 1073741824.0, 3)} GB";
        }

        #endregion

        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Save file dates in European Format 27-06-2023 (other format is North American 06-27-2023)")]
            public bool EuropeanTimeSave = false;

            [JsonProperty(PropertyName = "Create reports every (seconds)")]
            public int checkTime = 0;

            [JsonProperty(PropertyName = "Create plugins report")]
            public bool runPluginsReport = true;

            [JsonProperty(PropertyName = "Sort Plugins By Hook Time (If set false, sorts by Memory Usage)")]
            public bool sortByHookTime = true;

            [JsonProperty(PropertyName = "Create entities report")]
            public bool runEntitiesReport = true;

            [JsonProperty(PropertyName = "Excluded entities")]
            public string[] excludedEntities =
            {
                "shortname here",
                "another here"
            };

            [JsonProperty(PropertyName = "Excluded plugins")]
            public string[] excludedPlugins =
            {
                "name here",
                "another name"
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
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");

                timer.Every(10f,
                    () =>
                    {
                        PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                    });
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
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

        #region Classes

        private class PerformanceDump
        {
            [JsonProperty(PropertyName = "Online Players")]
            public int onlinePlayers = BasePlayer.activePlayerList.Count;

            [JsonProperty(PropertyName = "Offline Players")]
            public int offlinePlayers = BasePlayer.sleepingPlayerList.Count;

            [JsonProperty(PropertyName = "Entities Report")]
            public EntitiesReport entities = new EntitiesReport();

            [JsonProperty(PropertyName = "Plugins Report")]
            public string[] plugins;

            [JsonProperty(PropertyName = "Performance Report")]
            public Performance.Tick performance = Performance.current;

            [JsonIgnore]
            public int statusBar;

            [JsonIgnore]
            public int entitiesChecked;

            [JsonIgnore]
            public int entitiesTotal;
        }

        private class EntitiesReport
        {
            [JsonProperty(PropertyName = "Total")]
            public int countGlobal;

            [JsonProperty(PropertyName = "Owned")]
            public int countOwned;

            [JsonProperty(PropertyName = "Unowned")]
            public int countUnowned;

            [JsonProperty(PropertyName = "List")]
            public Dictionary<string, EntityInfo> list = new Dictionary<string, EntityInfo>();

            [JsonIgnore]
            public bool completed;
        }

        private class EntityInfo
        {
            [JsonProperty(PropertyName = "Total")]
            public int countGlobal;

            [JsonProperty(PropertyName = "Owned")]
            public int countOwned;

            [JsonProperty(PropertyName = "Unowned")]
            public int countUnowned;
        }

        #endregion
    }
}