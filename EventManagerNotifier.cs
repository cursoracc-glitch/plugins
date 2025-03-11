using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EventManagerNotifier", "M&B-Studios", "1.0.2")]
    public class EventManagerNotifier : RustPlugin
    {
        private ConfigData configData;

        class EventConfig
        {
            public bool MessagesActive { get; set; }
            public string EventName { get; set; }
            public string StartCommand { get; set; }
            public string StartMessage { get; set; }
        }

        class ConfigData
        {
            public List<EventConfig> Events { get; set; } = new List<EventConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData
            {
                Events = new List<EventConfig>
                {
                    new EventConfig
                    {
                        MessagesActive = true,
                        EventName = "Airdrop",
                        StartCommand = "em_spawn assets/prefabs/misc/supply drop/supply_drop.prefab",
                        StartMessage = "Airdrop was <color=green>dropped</color>"
                    },
                    new EventConfig
                    { 
                        MessagesActive = true,
                        EventName = "Chinook 47",
                        StartCommand = "em_spawn assets/prefabs/npc/ch47/ch47scientists.entity.prefab",
                        StartMessage = "Chinook 47 has <color=green>started</color> and is making its rounds"
                    },
                    new EventConfig
                    {
                        MessagesActive = true,
                        EventName = "Cargoship",
                        StartCommand = "em_spawn assets/content/vehicles/boats/cargoship/cargoshiptest.prefab",
                        StartMessage = "CargoShip sets sail and has <color=green>started</color> the journey"
                    },
                    new EventConfig
                    {
                        MessagesActive = true,
                        EventName = "Helicopter",
                        StartCommand = "em_spawn assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab",
                        StartMessage = "Helicopter <color=green>started</color> the sightseeing flight"
                    }
                }
            };
            SaveConfig();
        }

        private void OnEventStarted(string eventname)
        {
            var @event = configData.Events.FirstOrDefault(x => x.EventName == eventname);
            if (@event != null)
                Server.Broadcast(@event.StartMessage);
        }
        
        void Init()
        {
            configData = Config.ReadObject<ConfigData>();
            if (configData == null || configData.Events == null || configData.Events.Count == 0)
            {
                LoadDefaultConfig();
            }

            foreach (var eventConfig in configData.Events)
            {
                cmd.AddChatCommand(eventConfig.StartCommand, this, nameof(CmdEventStart));
                cmd.AddConsoleCommand(eventConfig.StartCommand, this, nameof(cmdEventStartConsole));   
                // AddCovalenceCommand(eventConfig.StartCommand, "CmdEventStart");
            }
        }

        private void cmdEventStartConsole(ConsoleSystem.Arg arg) => CmdEventStart(arg.Player(), "", arg.Args);

        private void CmdEventStart(BasePlayer player, string command, string[] args)
        {
            var eventConfig = configData.Events.Find(e => e.StartCommand == command);
            if (eventConfig != null && eventConfig.MessagesActive)
            {
                Server.Broadcast(eventConfig.StartMessage);
            }
        }

        void SaveConfig() => Config.WriteObject(configData);
    }
}
