
/*
 ########### README ####################################################
 #                                                                     #
 #   1. If you found a bug, please report them to developer!           #
 #   2. Don't edit that file (edit files only in CONFIG/LANG/DATA)     #
 #                                                                     #
 ########### CONTACT INFORMATION #######################################
 #                                                                     #
 #   Website: https://rustworkshop.space/                              #
 #   Discord: Orange#0900                                              #
 #   Email: admin@rustworkshop.space                                   #
 #                                                                     #
 #######################################################################
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Warps", "Orange", "1.1.1")]
    [Description("https://rustworkshop.space/resources/warps.180/")]
    public class Warps : RustPlugin
    {
        #region Vars

        private static CollisionDetector[] AllWarps => UnityEngine.Object.FindObjectsOfType<CollisionDetector>();

        #endregion
        
        #region Oxide Hooks

        private void Init()
        {
            foreach (var obj in config.warps)
            {
                if (string.IsNullOrEmpty(obj.permission) == false && permission.PermissionExists(obj.permission) == false)
                {
                    permission.RegisterPermission(obj.permission, this);
                }
            }

            foreach (var command in config.commands)
            {
                cmd.AddChatCommand(command, this, nameof(cmdControlChat));
            }
        }

        private void OnServerInitialized()
        {
            DestroyWarps();
            timer.Once(1f, CreateWarps);
        }

        private void Unload()
        {
            DestroyWarps();
        }

        #endregion

        #region Commands

        private void cmdControlChat(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            {
                player.ChatMessage("No Permission");
                return;
            }
            
            var action = args?.Length > 0 ? args[0] : "null";
            var name = args?.Length > 1 ? args[1] : "null";
            var text = $"{player.displayName} [{player.userID}] ";

            switch (action.ToLower())
            {
                case "clear":
                    text += "removed all warps!";
                    DestroyWarps();
                    config.warps.Clear();
                    SaveConfig();
                    break;
                
                case "add":
                    var def = new WarpDefinition
                    {
                        shortname = name,
                        position = player.transform.position,
                        note = $"Created by {player.displayName} at {DateTime.UtcNow:dd/MM/yyyy}"
                    };
  
                    text += $"added warp ({def.shortname}) at {def.position}";
                    config.warps.Add(def);
                    SaveConfig();
                    CreateWarp(def);
                    player.ChatMessage($"Warp with name {def.shortname} was successfully created!");
                    break;
                
                case "remove":
                    var match = config.warps.Where(x => x.shortname.Contains(name, CompareOptions.OrdinalIgnoreCase)).ToArray();
                    text += "removed warp ";
                    var objs = AllWarps;
                    
                    foreach (var value in match)
                    {
                        text += $"({value.shortname}) ";
                        config.warps.Remove(value);
                        var obj = objs.FirstOrDefault(x => x.name == value.shortname);
                        UnityEngine.Object.Destroy(obj);
                    }
                    
                    SaveConfig();
                    player.ChatMessage($"{match.Length} warps was removed!");
                    break;
                
                default:
                    player.ChatMessage("Usage:\n/warp add/remove NAME - to add/remove warp with NAME\n/warps clear - remove all warps");
                    return;
            }
            
            PrintWarning(text);
            LogToFile("general", text, this);
        }

        #endregion

        #region Core

        private void CreateWarps()
        {
            foreach (var warp in config.warps)
            {
                CreateWarp(warp);
            }

            timer.Once(1f, () =>
            {
                Puts($"{config.warps.Count} warps was created! Total: {AllWarps.Length}");
            });
        }

        private void DestroyWarps()
        {
            foreach (var obj in AllWarps)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        private void CreateWarp(WarpDefinition definition)
        {
            var obj = CollisionDetector.Create();
            obj.OnPlayerEnter = player => OnPlayerJoined(player, definition);
            obj.Radius = definition.radius;
            obj.name = definition.shortname;
            obj.transform.position = definition.position;
        }

        private void OnPlayerJoined(BasePlayer player, WarpDefinition warp)
        {
            if (player.IsSleeping() || player.IsReceivingSnapshot == true || player.IsDead())
            {
                Message.Send(player, Message.Key.PreventingWarp, "{name}", warp.displayName);
                return;
            }

            if (string.IsNullOrEmpty(warp.permission) == false &&  permission.UserHasPermission(player.UserIDString, warp.permission) == false)
            {
                Message.Send(player, Message.Key.Permission);
                return;
            }
            
            Message.Send(player, Message.Key.EnteringWarp, "{name}", warp.displayName);

            foreach (var command in warp.commandsToPlayer)
            {
                if (string.IsNullOrWhiteSpace(command) == false && command.StartsWith("example") == false)
                {
                    var str = GetReplacedString(command, player, warp);
                    player.SendConsoleCommand(str);
                }
            }
            
            foreach (var command in warp.commandsToServer)
            {
                if (string.IsNullOrWhiteSpace(command) == false && command.StartsWith("example") == false)
                {
                    var str = GetReplacedString(command, player, warp);
                    Server.Command(str);
                }
            }

            if (warp.positionToTeleport != new Vector3())
            {
                Teleport(player, warp.positionToTeleport);
            }
        }

        private static void Teleport(BasePlayer player, Vector3 position)
        {
            player.ConsoleMessage($"[Warps] Teleporting to {position}");
            player.RemoveFromTriggers();
            //player.EnableServerFall(false);
            player.EnsureDismounted();
            player.SetParent(null, true, true);
            player.StartSleeping(); 
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.Teleport(position);
            player.UpdateNetworkGroup();
            player.SendEntityUpdate();
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "StartLoading");
            player.SendFullSnapshot();
            player.ForceUpdateTriggers();
        }

        private static string GetReplacedString(string original, BasePlayer player, WarpDefinition warp)
        {
            return original
                .Replace("{userid}", player.UserIDString, StringComparison.OrdinalIgnoreCase)
                .Replace("{steamid}", player.UserIDString, StringComparison.OrdinalIgnoreCase)
                .Replace("{warp}", warp.shortname, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
        
        #region Classes

        private static ConfigDefinition config = new ConfigDefinition();

        private class ConfigDefinition
        {
            [JsonProperty("Commands")]
            public string[] commands = {"warp", "warps"};
            
            [JsonProperty("Warps")]
            public List<WarpDefinition> warps = new List<WarpDefinition>();
        }
        
        private class WarpDefinition
        {
            [JsonProperty("Shortname")]
            public string shortname = Core.Random.Range(0, 99999).ToString();

            [JsonProperty("Display Name")] 
            public string displayName = "Best Warp";
            
            [JsonProperty("Position")]
            public Vector3 position = new Vector3();

            [JsonProperty("Permission")]
            public string permission = "";

            [JsonProperty("Radius (meters)")]
            public float radius = 1.5f;

            [JsonProperty("Position to teleport")]
            public Vector3 positionToTeleport = new Vector3();
            
            [JsonProperty("Note")]
            public string note = "Description";

            [JsonProperty("Commands called to player")]
            public string[] commandsToPlayer =
            {
                "example.run {userID}",
                "example.run {userID}",
                "example.run {userID}",
            };

            [JsonProperty("Commands called to server")]
            public string[] commandsToServer =
            {
                "example.run {userID}",
                "example.run {userID}",
                "example.run {userID}",
            };
        }
        
        private partial class Message
        {
            private static Dictionary<Key, object> messages = new Dictionary<Key, object>
            {
                {Key.EnteringWarp, "You are entering warp <color=#ffff00>{name}</color>"},
                {Key.PreventingWarp, "We prevented using warp <color=#ffff00>{name}</color> because you was sleeping or loading"},
                {Key.Permission, "You don't have permission to do that!"}
            };
            
            public enum Key
            { 
                EnteringWarp,
                PreventingWarp,
                Permission,
            }
        }

        #endregion

        #region API

        private void FillWarpsInformation(Dictionary<string, Vector3> warps)
        {
            foreach (var warp in config.warps)
            {
                warps.Add(warp.shortname, warp.position);
            }
        }
        
        private Dictionary<string, Vector3> GetAllWarpsWithPositions()
        {
            return config.warps.ToDictionary(x => x.shortname, y => y.position);
        }

        private string[] GetAllWarps()
        {
            return config.warps.Select(x => x.shortname).ToArray();
        }

        #endregion
        
        #region CollisionDetector v1.1
        
        private class CollisionDetector : MonoBehaviour
        {
            private SphereCollider collider;
            public float Delay = 1f;
            public float Radius;
            public Action<GameObject> OnGameObjectEnter;
            public Action<GameObject> OnGameObjectExit;
            public Action<BasePlayer> OnPlayerEnter;
            public Action<BasePlayer> OnPlayerExit;

            public static CollisionDetector Create()
            {
                var obj = new GameObject().AddComponent<CollisionDetector>();
                return obj;
            }

            public static CollisionDetector Create(BaseEntity entity)
            {
                if (entity.IsValid() == false)
                {
                    return null;
                }
                
                var obj = new GameObject().AddComponent<CollisionDetector>();
                obj.transform.SetParent(entity.transform, false);
                return obj;
            }

            private void Start()
            {
                if (Radius < 0.001f)
                {
                    Destroy(this);
                    throw new Exception("Failed to create collision detector because it was not setted up properly");
                }
                
                Invoke(nameof(AddCollider), Delay);
            }
            
            private void OnDestroy()
            {
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            private void AddCollider()
            {
                collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = Radius;
                collider.isTrigger = true;
                collider.gameObject.layer = (int) Layer.Reserved1;
            }

            private void Enter(GameObject go)
            {
                if (OnGameObjectEnter != null)
                {
                    OnGameObjectEnter(go);
                }

                if (OnPlayerEnter != null)
                {
                    var player = go.ToBaseEntity() as BasePlayer;
                    if (player != null)
                    {
                        OnPlayerEnter(player);
                    }
                }
            }

            private void Exit(GameObject go)
            {
                if (OnGameObjectExit != null)
                {
                    OnGameObjectExit(go);
                }
                
                if (OnPlayerExit != null)
                {
                    var player = go.ToBaseEntity() as BasePlayer;
                    if (player != null)
                    {
                        OnPlayerExit(player);
                    }
                }
            }

            private void OnTriggerEnter(Collider component)
            {
                var go = component.gameObject;
                if (go != null)
                {
                    Enter(go);
                }
            }

            private void OnCollisionEnter(Collision component)
            {
                var go = component.gameObject;
                if (go != null)
                {
                    Enter(go);
                }
            }

            private void OnTriggerExit(Collider component)
            {
                var go = component.gameObject;
                if (go != null)
                {
                    Exit(go);
                }
            }

            private void OnCollisionExit(Collision component)
            {
                var go = component.gameObject;
                if (go != null)
                {
                    Exit(go);
                }
            }
        }

        #endregion
        
        #region Configuration v2.1

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigDefinition>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                config = new ConfigDefinition();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigDefinition();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Language System v2.3

        protected override void LoadDefaultMessages()
        {
            Message.Load(lang, this);
        }

        private partial class Message
        {
            private static RustPlugin plugin;
            private static Lang lang;
            private static ulong senderID = 0;

            public static void ChangeSenderID(ulong newValue)
            {
                senderID = newValue;
            }

            public static void Load(Lang v1, RustPlugin v2)
            {
                lang = v1;
                plugin = v2;

                var dictionary = new Dictionary<string, string>();
                foreach (var pair in messages)
                {
                    var key = pair.Key.ToString();
                    var value = pair.Value.ToString();
                    dictionary.TryAdd(key, value);
                }

                lang.RegisterMessages(dictionary, plugin);
            }

            public static void Unload()
            {
                lang = null;
                plugin = null;
            }

            public static void Console(string message, Type type = Type.Normal)
            {
                message = $"[{plugin.Name}] {message}";
                switch (type)
                {
                    case Type.Normal:
                        Debug.Log(message);
                        break;

                    case Type.Warning:
                        Debug.LogWarning(message);
                        break;

                    case Type.Error:
                        Debug.LogError(message);
                        break;
                }
            }

            public static void Send(object receiver, string message, params object[] args)
            {
                message = FormattedMessage(message, args);
                SendMessage(receiver, message);
            }

            public static void Send(object receiver, Key key, params object[] args)
            {
                var userID = (receiver as BasePlayer)?.UserIDString;
                var message = GetMessage(key, userID, args);
                SendMessage(receiver, message);
            }

            public static void Broadcast(string message, params object[] args)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    message = FormattedMessage(message, args);
                    SendMessage(player, message);
                }
            }

            public static void Broadcast(Key key, params object[] args)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    var message = GetMessage(key, player.UserIDString, args);
                    SendMessage(player, message);
                }
            }

            public static string GetMessage(Key key, string playerID = null, params object[] args)
            {
                var keyString = key.ToString();
                var message = lang.GetMessage(keyString, plugin, playerID);
                if (message == keyString)
                {
                    return $"{keyString} is not defined in plugin!";
                }

                if (Interface.CallHook("OnLanguageValidate") != null)
                {
                    message = messages.FirstOrDefault(x => x.Key == key).Value as string;
                }

                return FormattedMessage(message, args);
            }

            public static string FormattedMessage(string message, params object[] args)
            {
                if (args != null && args.Length > 0)
                {
                    var organized = OrganizeArgs(args);
                    return ReplaceArgs(message, organized);
                }

                return message;
            }

            private static void SendMessage(object receiver, string message)
            {
                if (receiver == null || string.IsNullOrEmpty(message))
                {
                    return;
                }

                BasePlayer player = null;
                IPlayer iPlayer = null;
                ConsoleSystem.Arg console = null;

                if (receiver is BasePlayer)
                {
                    player = receiver as BasePlayer;
                }

                if (player == null && receiver is IPlayer)
                {
                    iPlayer = receiver as IPlayer;
                    player = BasePlayer.Find(iPlayer.Id);
                }

                if (player == null && receiver is ConsoleSystem.Arg)
                {
                    console = receiver as ConsoleSystem.Arg;
                    player = console.Connection?.player as BasePlayer;
                    message = $"[{plugin?.Name}] {message}";
                }

                if (player == null && receiver is Component)
                {
                    var obj = receiver as Component;
                    player = obj.GetComponent<BasePlayer>() ?? obj.GetComponentInParent<BasePlayer>() ?? obj.GetComponentInChildren<BasePlayer>();
                }

                if (player == null)
                {
                    message = $"[{plugin?.Name}] {message}";
                    Debug.Log(message);
                    return;
                }

                if (player.IsConnected == false)
                {
                    return;
                }
                
                if (console != null)
                {
                    player.SendConsoleCommand("echo " + message);
                }

                if (senderID > 0)
                {
                    if (Interface.CallHook("OnMessagePlayer", message, player) != null)
                    {
                        return;
                    }

                    player.SendConsoleCommand("chat.add", (object) 2, (object) senderID, (object) message);
                }
                else
                {
                    player.ChatMessage(message);
                }
            }
            
            private static Dictionary<string, object> OrganizeArgs(object[] args)
            {
                var dic = new Dictionary<string, object>();
                for (var i = 0; i < args.Length; i += 2)
                {
                    var value = args[i].ToString();
                    var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                    dic.TryAdd(value, nextValue);
                }

                return dic;
            }

            private static string ReplaceArgs(string message, Dictionary<string, object> args)
            {
                if (args == null || args.Count < 1)
                {
                    return message;
                }

                foreach (var pair in args)
                {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }

                return message;
            }

            public enum Type
            {
                Normal,
                Warning,
                Error
            }
        }
        
        #endregion
    }
}
