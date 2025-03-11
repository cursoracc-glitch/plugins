using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("AutoCleanup", "Server-rust.ru fixed by pahan0772", "1.0.8")]
    class AutoCleanup : RustPlugin
    {
        bool Changed = false;

        bool logToConsole, broadcastToChat, cleanOnLoad;
        float timerIntervalInSeconds, decayPercentage;
        string commandPermission, excludePermission, cleanupChatCommand, cleanupConsoleCommand;

        List<object> entityList;

        List<object> GetDefaultEntityList()
        {
            return new List<object>()
            {
                "assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab",
                "assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab",
                "assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab",
                "assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab",
                "assets/prefabs/misc/item drop/item_drop_backpack.prefab",
                "assets/prefabs/gamemodes/objects/reclaim/reclaimbackpack.prefab",
                "assets/prefabs/misc/item drop/item_drop.prefab",
                "assets/prefabs/misc/item drop/item_drop.prefab",

                "assets/prefabs/deployable/barricades/barricade.concrete.prefab",
                "assets/prefabs/deployable/barricades/barricade.metal.prefab",
                "assets/prefabs/deployable/barricades/barricade.sandbags.prefab",
                "assets/prefabs/deployable/barricades/barricade.stone.prefab",
                "assets/prefabs/deployable/barricades/barricade.wood.prefab",
                "assets/prefabs/deployable/barricades/barricade.woodwire.prefab",
                
            };
        }

        void Init()
        {
            LoadVariables();
            LoadDefaultMessages();
            RegisterPermissions();

            cmd.AddChatCommand(cleanupChatCommand, this, "cmdCleanupChatCommand");
            cmd.AddConsoleCommand(cleanupConsoleCommand, this, "cmdCleanupConsoleCommand");

            if (cleanOnLoad) CleanUp();

            timer.Every(timerIntervalInSeconds, () => { CleanUp(); });
        }

        void cmdCleanupChatCommand(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, commandPermission))
            {
                player.ChatMessage(Lang("NoPermission", player.UserIDString));
                return;
            }

            CleanUp();
        }

        void cmdCleanupConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Connection != null && !permission.UserHasPermission(arg?.Player()?.userID.ToString(), commandPermission)) return;

            CleanUp();
        }
        void CleanUp()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.ChatMessage($"<size=13>Запущена оптимизация карты</color></size>\nВсе стенки будут уничтожены в течение <color=#8EBD2E>20 секунд.</color>");

            }
            timer.Once(20f, () =>
            {
                CCleanUp();
            });
        }
        void CCleanUp()
        {
            if (broadcastToChat)
                PrintToChat(Lang("LocatingEntities", null));
            if (logToConsole)
                PrintWarning(Lang("LocatingEntities", null));

            int reduced = 0;
            int destroyed = 0;

            foreach (var entity in BaseNetworkable.serverEntities.Where(e => (e as BaseEntity).OwnerID != 0 && !permission.UserHasPermission((e as BaseEntity).OwnerID.ToString(), excludePermission) && entityList.Contains((e as BaseEntity).name)).ToList())
            {
                var entityRadius = Physics.OverlapSphere(entity.transform.position, 0.5f, LayerMask.GetMask("Trigger"));
                int cupboards = 0;

                foreach (var cupboard in entityRadius.Where(x => x.GetComponentInParent<BuildingPrivlidge>() != null)) cupboards++;

                if (cupboards == 0)
                {
                    var ent = ((BaseCombatEntity)entity);

                    if (ent.health - (ent.MaxHealth() * (decayPercentage / 100)) <= 0)
                    {
                        entity?.KillMessage();
                        destroyed++;
                    }
                    else
                    {
                        ent.health -= (ent.MaxHealth() * (decayPercentage / 100));

                        ent.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        reduced++;
                    }
                }
            }

            /*if (broadcastToChat)
            {
                PrintToChat(Lang("EntitiesReducedHealth", null, reduced, decayPercentage));
                PrintToChat(Lang("EntitiesDestroyed", null, destroyed));
            }

            if (logToConsole)
            {
                PrintWarning(Lang("EntitiesReducedHealth", null, reduced, decayPercentage));
                PrintWarning(Lang("EntitiesDestroyed", null, destroyed));
            }*/
        }

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LocatingEntities"] = "Locating all External Walls/Gates & Barricades outside Cupboard Range...",
                ["EntitiesReducedHealth"] = "Reduced the health of {0} External Walls/Gates & Barricades by {1}%.",
                ["EntitiesDestroyed"] = "Destroyed {0} External Walls/Gates & Barricades.",
                ["NoPermission"] = "You do not have permission to use this command."
            }, this);
        }

        void RegisterPermissions()
		{
			permission.RegisterPermission(excludePermission, this);
			permission.RegisterPermission(commandPermission, this);
		}

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        void LoadVariables()
        {
            timerIntervalInSeconds = Convert.ToSingle(GetConfig("Settings", "Timer Interval (Seconds)", 3600f));
            decayPercentage = Convert.ToSingle(GetConfig("Settings", "Reduce health by (Percentage)", 10.0f));

            logToConsole = Convert.ToBoolean(GetConfig("Settings", "Log Messages to Console", true));
            broadcastToChat = Convert.ToBoolean(GetConfig("Settings", "Broadcast Messages to Chat", true));
            cleanOnLoad = Convert.ToBoolean(GetConfig("Settings", "Clean up when plugin is loaded", false));


            entityList = (List<object>)GetConfig("Settings", "List of entities", GetDefaultEntityList());

            excludePermission = Convert.ToString(GetConfig("Permissions", "ExcludePermission", "autocleanup.exclude"));
            commandPermission = Convert.ToString(GetConfig("Permissions", "CommandPermission", "autocleanup.cleanup"));
			
			
            cleanupChatCommand = Convert.ToString(GetConfig("Commands", "CleanupChatCommand", "cleanup"));
            cleanupConsoleCommand = Convert.ToString(GetConfig("Commands", "CleanupConsoleCommand", "autocleanup.cleanup"));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
                Changed = true;
            }

            return value;
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
    }
}