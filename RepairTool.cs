using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Facepunch;
using System;

namespace Oxide.Plugins
{
    [Info("RepairTool", "RustPlugin", "1.0.0")]
    [Description("Ремонт киянкой по радиусу")]
    public class RepairTool : RustPlugin
    {
        [PluginReference]
        Plugin NoEscape;

        #region Constants
        private const string permName = "RepairTool.use";
        #endregion

        #region Members
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("RepairTool");
        Dictionary<ulong, bool> playerPrefs_IsActive = new Dictionary<ulong, bool>();
        private bool _allowRepairToolFixMessage = true; 
        private bool _allowAOERepair = true; 

        private PluginTimers RepairMessageTimer;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission(permName, this);

            LoadVariables();
            LoadMessages();

            try
            {
                playerPrefs_IsActive = dataFile.ReadObject<Dictionary<ulong, bool>>();
            }
            catch { }

            if (playerPrefs_IsActive == null)
                playerPrefs_IsActive = new Dictionary<ulong, bool>();
        }

        void LoadMessages()
        {
            string helpText = "RepairTool - Помощь - v {ver} \n"
                            + "-----------------------------\n"
                            + "/Repair - Shows your current preference for RepairTool.\n"
                            + "/Repair on/off - Turns RepairTool on/off.";

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Hired", "Ремонт по области был <color=#a2d953>Включен</color>"},
                {"Fired", "Ремонт по области был <color=#CD2626>Выключен</color>"},
                {"Fix", "Почините одно, чтобы починить все остальное в области."},
                {"NotAllowed", "Вам не разрешено здесь строить - По этому Вам нельзя производить починку."},
                {"IFixed", "В этом месте все еще есть ущерб(Не забывайте про ресурсы за починку)."},
                {"FixDone", "В этом месте уже все отремонтировано."},
                {"MissingFix", "Не могу найти ничего, чтобы починить."},
                {"NoPermission", "У вас нет привелегий использовать эту команду!" },
                {"Help", helpText}
            }, this);
        }

        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (!permission.UserHasPermission(player.UserIDString, permName) || IsRaidBlocked(player.UserIDString))
                return;


            var e = info.HitEntity.GetComponent<BaseCombatEntity>();


            if (e != null)
            {

                if (!playerPrefs_IsActive.ContainsKey(player.userID))
                {

                    playerPrefs_IsActive[player.userID] = DefaultRepairToolOn;
                    dataFile.WriteObject(playerPrefs_IsActive);
                }


                if (_allowAOERepair && playerPrefs_IsActive[player.userID])
                {
                    //calls our custom method for this
                    Repair(e, player);
                }
            }
        }

        #endregion

        #region HelpText Hooks

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, permName))
                player.ChatMessage(GetMsg("Help", player.userID).Replace("{ver}", Version.ToString()));
        }
        #endregion
        
        #region Repair Methods


        void Repair(BaseCombatEntity block, BasePlayer player)
        {

            ConfigureMessageTimer();


            if (player.CanBuild())
            {

                if (_allowRepairToolFixMessage)
                {

                    SendChatMessage(player, Title, GetMsg("Fix", player.userID));
                    _allowRepairToolFixMessage = false;
                }

                //Envoke the AOE repair set
                RepairAOE(block, player);
            }
            else
                SendChatMessage(player, Title, GetMsg("NotAllowed", player.userID));
        }


        private void RepairAOE(BaseCombatEntity block, BasePlayer player)
        {

            _allowAOERepair = false;


            var position = new OBB(block.transform, block.bounds).ToBounds().center;

            var entities = Pool.GetList<BaseCombatEntity>();


            Vis.Entities(position, RepairRange, entities);


            if (entities.Count > 0)
            {
                bool hasRepaired = false;


                foreach (var entity in entities)
                {
                    if (entity.gameObject.layer != (int)Rust.Layer.Construction)
                        continue;


                    if (entity.health < entity.MaxHealth())
                    {
                        //yes - repair
                        entity.DoRepair(player);
                        entity.SendNetworkUpdate();
                        hasRepaired = true;
                    }
                }
                Pool.FreeList(ref entities);

                //checks to see if any entities were repaired
                if (hasRepaired)
                {
                    //yes - indicate
                    SendChatMessage(player, Title, GetMsg("IFixed", player.userID));
                }
                else
                {
                    //No - indicate
                    SendChatMessage(player, Title, GetMsg("FixDone", player.userID));
                }
            }
            else
            {
                SendChatMessage(player, Title, GetMsg("MissingFix", player.userID));
            }
            
            _allowAOERepair = true;
        }

        /// <summary>
        /// Responsible for preventing spam to the user by setting a timer to prevent messages from RepairTool for a set duration.
        /// </summary>
        private void ConfigureMessageTimer()
        {
            //checks if our timer exists
            if (RepairMessageTimer == null)
            {
                //no - create it
                RepairMessageTimer = new PluginTimers(this);
                //set it to fire every xx seconds based on configuration
                RepairMessageTimer.Every(RepairToolChatInterval, RepairMessageTimer_Elapsed);
            }
        }


        private void RepairMessageTimer_Elapsed()
        {
            //set the allow message to true so the next message will show
            _allowRepairToolFixMessage = true;
        }

        #endregion


        #region Chat and Console Command Examples
        [ChatCommand("Repair")]
        private void ChatCommand_RepairTool(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permName))
            {
                SendChatMessage(player, Title, GetMsg("NoPermission", player.userID));
                return;
            }

            //checks if player preference for RepairTool exists on this player
            if (!playerPrefs_IsActive.ContainsKey(player.userID))
            {
                //no - create a default entry for this player based on the default RepairTool configuration state
                playerPrefs_IsActive[player.userID] = DefaultRepairToolOn;
                dataFile.WriteObject(playerPrefs_IsActive);
            }

            if (args.Length > 0)
            {
                if (args[0].ToLower() == "on")
                    playerPrefs_IsActive[player.userID] = true;
                else
                    playerPrefs_IsActive[player.userID] = false;

                dataFile.WriteObject(playerPrefs_IsActive);
            }

            SendChatMessage(player, Title, GetMsg(playerPrefs_IsActive[player.userID] ? "Hired" : "Fired", player.userID));
        }

        [ConsoleCommand("HealthCheck")]
        private void ConsoleCommand_HealthCheck() => Puts("RepairTool is running.");
        #endregion

        #region Helpers

        /// <summary>
        /// Retreives the configured message from the lang API storage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        bool IsRaidBlocked(string targetId) => UseRaidBlocker && (bool)(NoEscape?.Call("IsRaidBlockedS", targetId) ?? false);

        /// <summary>
        /// Writes message to player chat
        /// </summary>
        /// <param name="player"></param>
        /// <param name="prefix"></param>
        /// <param name="msg"></param>
        private void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        #endregion

        #region Config
        private bool Changed;
        private bool UseRaidBlocker;
        private bool DefaultRepairToolOn;
        private int RepairRange;
        private int RepairToolChatInterval;

        void LoadVariables() //Assigns configuration data once read
        {
            RepairToolChatInterval = Convert.ToInt32(GetConfig("Settings", "Chat Interval", 30));
            DefaultRepairToolOn = Convert.ToBoolean(GetConfig("Settings", "Default On", true));
            RepairRange = Convert.ToInt32(GetConfig("Settings", "Repair Range", 50));
            UseRaidBlocker = Convert.ToBoolean(GetConfig("Settings", "Use Raid Blocker", false));
			if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }

        /// <summary>
        /// Responsible for loading default configuration.
        /// Also creates the initial configuration file
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru.\n Если вы приобрели плагин в другом месте - вы теряете все гарантии.");
            
            Config.Clear();
            LoadVariables();
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
        #endregion
    }
}
