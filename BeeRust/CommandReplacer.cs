using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("CommandReplacer", "YourName", "0.2.0")]
    [Description("Replace non-existent commands with custom text.")]
    class CommandReplacer : RustPlugin
    {
        private DynamicConfigFile config;

        private string replacementText = "Unknown command. Type /help for a list of commands."; // Default replacement text

        void Init()
        {
            config = Interface.Oxide.DataFileSystem.GetFile("commandreplacer");
            LoadConfig();
        }

        void LoadConfig()
        {
            // Load the configuration file
            replacementText = config.Get<string>("ReplacementText", "Unknown command. Type /help for a list of commands.");
            SaveConfig();
        }

        void SaveConfig()
        {
            // Save the configuration file
            config["ReplacementText"] = replacementText;
            config.Save();
        }

        private List<string> GetCommandList()
        {
            var commands = new List<string>();
            foreach (var loadedPlugin in plugins.GetAll())
            {
                if (loadedPlugin == null) continue;
                var pluginCommands = loadedPlugin.GetType().GetMethods()
                    .Where(m =>
                        m.GetCustomAttributes(typeof(ChatCommandAttribute), true)?.Length > 0 ||
                        m.GetCustomAttributes(typeof(ConsoleCommandAttribute), true)?.Length > 0)
                    .Select(m => m.GetCustomAttributes(typeof(ChatCommandAttribute), true)?.FirstOrDefault() ??
                                 m.GetCustomAttributes(typeof(ConsoleCommandAttribute), true)
                                     ?.FirstOrDefault())
                    .ToList();

                pluginCommands.ForEach(p => commands.Add(p.ToString().Replace("Oxide.Core.Libraries.Covalence.ChatCommandAttribute", "").Replace("Oxide.Core.Libraries.Command.ConsoleCommandAttribute", "").Trim()));
            }

            return commands;
        }

        void OnPlayerCommand(IPlayer player, string command, string[] args)
        {
            if (GetCommandList().All(cmd => cmd != "/" + command))
            {
                player.Message(replacementText);
                return;
            }
        }

        [ChatCommand("setreplacementtext")]
        void SetReplacementTextCommand(BasePlayer player, string command, string[] args)
        {
            // Check if the player is an admin
            if (!player.IsAdmin)
            {
                player.ChatMessage("You must be an admin to use this command.");
                return;
            }

            // Set the replacement text with the command argument
            if (args.Length >= 1)
            {
                replacementText = string.Join(" ", args);
                SaveConfig();
                player.ChatMessage("Replacement text set to: " + replacementText);
            }
        }
    }
}