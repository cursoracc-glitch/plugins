using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PluginCleaner", "Vinni P.", "1.5.8")]
    [Description("Removes config, lang, and data files for plugins that no longer exist.")]
    public class PluginCleaner : RustPlugin
    {
        private PluginCleanerConfig config;

        private class PluginCleanerConfig
        {
            public bool AutoRemoveFiles { get; set; } = true;
            public Dictionary<string, List<string>> CustomDataMappings { get; set; } = new Dictionary<string, List<string>>();
            public Dictionary<string, List<string>> IgnoredFiles { get; set; } = new Dictionary<string, List<string>>
            {
                { "config", new List<string>() },
                { "lang", new List<string>() },
                { "data", new List<string> { "oxide.users.data", "oxide.lang.data", "oxide.groups.data", "oxide.covalence.data", "vendordata_cf.db", "vendordata_svowner.db" } }
            };
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginCleanerConfig
            {
                AutoRemoveFiles = true,
                CustomDataMappings = new Dictionary<string, List<string>>
                {
                    { "BuildingSkins", new List<string> { "BuildingSkins_Data" } },
                    { "NTeleportation", new List<string> { "NTeleportationbandit", "NTeleportationoutpost" } },
                    { "StatsController", new List<string> { "statscontroller" } },
                    { "BetterVanish", new List<string> { "BetterVanish-SafePoints", "BetterVanish-PersistPlr" } }
                },
                IgnoredFiles = new Dictionary<string, List<string>>
                {
                    { "config", new List<string>() },
                    { "lang", new List<string>() },
                    { "data", new List<string> { "oxide.users.data", "oxide.lang.data", "oxide.groups.data", "oxide.covalence.data", "vendordata_cf.db", "vendordata_svowner.db" } }
                }
            };
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginCleanerConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        [ConsoleCommand("cleanplugins")]
        private void CleanPluginsCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                SendReply(arg, "You do not have permission to use this command.");
                return;
            }
        
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "Usage: cleanplugins <oxide|carbon>");
                return;
            }
        
            string framework = arg.Args[0].ToLower();
            string rootDirectory;
            string configDirectory;
        
            if (framework == "oxide")
            {
                rootDirectory = Path.Combine(Interface.Oxide.RootDirectory, "oxide");
                configDirectory = Path.Combine(rootDirectory, "config");
            }
            else if (framework == "carbon")
            {
                rootDirectory = "carbon";
                configDirectory = Path.Combine(rootDirectory, "configs");
            }
            else
            {
                SendReply(arg, "Invalid argument. Usage: cleanplugins <oxide|carbon>");
                return;
            }
        
            string pluginDirectory = Path.Combine(rootDirectory, "plugins");
            string langDirectory = Path.Combine(rootDirectory, "lang");
            string dataDirectory = Path.Combine(rootDirectory, "data");
        
            if (!Directory.Exists(pluginDirectory))
            {
                SendReply(arg, $"Required directory does not exist: {pluginDirectory}");
                return;
            }
            if (!Directory.Exists(configDirectory))
            {
                SendReply(arg, $"Required directory does not exist: {configDirectory}");
                return;
            }
            if (!Directory.Exists(langDirectory))
            {
                SendReply(arg, $"Required directory does not exist: {langDirectory}");
                return;
            }
            if (!Directory.Exists(dataDirectory))
            {
                SendReply(arg, $"Required directory does not exist: {dataDirectory}");
                return;
            }
        
            var pluginFiles = Directory.GetFiles(pluginDirectory, "*.cs").Select(Path.GetFileNameWithoutExtension).ToList();
            var configFiles = Directory.GetFiles(configDirectory, "*.json").Select(Path.GetFileNameWithoutExtension).ToList();
            var langFolders = Directory.GetDirectories(langDirectory).ToList();
            var dataFiles = Directory.GetFiles(dataDirectory, "*.json").Select(Path.GetFileNameWithoutExtension).ToList();
            var dataFolders = Directory.GetDirectories(dataDirectory).Select(Path.GetFileName).ToList();
        
            var configsToDelete = configFiles.Except(pluginFiles).Where(file => !config.IgnoredFiles["config"].Contains(file)).ToList();
            var dataFilesToDelete = dataFiles.Where(dataFile => !pluginFiles.Contains(dataFile) && !config.CustomDataMappings.Any(mapping => mapping.Value.Contains(dataFile)) && !config.IgnoredFiles["data"].Contains(dataFile)).ToList();
            var dataFoldersToDelete = dataFolders.Except(pluginFiles).Where(folder => !config.IgnoredFiles["data"].Contains(folder)).ToList();
            int langFilesDeletedCount = 0;
        
            if (config.AutoRemoveFiles)
            {
                foreach (var config in configsToDelete)
                {
                    string configPath = Path.Combine(configDirectory, config + ".json");
                    try
                    {
                        File.Delete(configPath);
                        Puts($"Deleted config file: {configPath}");
                    }
                    catch (Exception ex)
                    {
                        Puts($"Failed to delete config file: {configPath}. Error: {ex.Message}");
                    }
                }
        
                foreach (var langFolder in langFolders)
                {
                    var langFiles = Directory.GetFiles(langFolder, "*.json").Select(Path.GetFileNameWithoutExtension).ToList();
                    var langsToDelete = langFiles.Except(pluginFiles).Where(file => !config.IgnoredFiles["lang"].Contains(file)).ToList();
        
                    foreach (var lang in langsToDelete)
                    {
                        string langPath = Path.Combine(langFolder, lang + ".json");
                        try
                        {
                            File.Delete(langPath);
                            Puts($"Deleted lang file: {langPath}");
                            langFilesDeletedCount++;
                        }
                        catch (Exception ex)
                        {
                            Puts($"Failed to delete lang file: {langPath}. Error: {ex.Message}");
                        }
                    }
                }
        
                foreach (var dataFile in dataFilesToDelete)
                {
                    string dataFilePath = Path.Combine(dataDirectory, dataFile + ".json");
                    try
                    {
                        File.Delete(dataFilePath);
                        Puts($"Deleted data file: {dataFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Puts($"Failed to delete data file: {dataFilePath}. Error: {ex.Message}");
                    }
                }
        
                foreach (var dataFolder in dataFoldersToDelete)
                {
                    string dataFolderPath = Path.Combine(dataDirectory, dataFolder);
                    try
                    {
                        Directory.Delete(dataFolderPath, true);
                        Puts($"Deleted data folder: {dataFolderPath}");
                    }
                    catch (Exception ex)
                    {
                        Puts($"Failed to delete data folder: {dataFolderPath}. Error: {ex.Message}");
                    }
                }
            }
        
            SendReply(arg, $"Cleaned up {configsToDelete.Count} config files, {langFilesDeletedCount} lang files, {dataFilesToDelete.Count} data files, and {dataFoldersToDelete.Count} data folders.");
        }
    }
}