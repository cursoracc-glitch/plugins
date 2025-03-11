using System;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

// TODO: Look into utilizing CommunityEntity.ServerInstance.StartCoroutine

namespace Oxide.Plugins
{
    [Info("Prefab Sniffer", "Wulf", "2.0.0")]
    [Description("Searches the game files for prefab file locations")]
    public class PrefabSniffer : CovalencePlugin
    {
        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandPrefab"] = "prefab",
                ["NoResultsFound"] = "No results found for {0}",
                ["ResultsSaved"] = "Prefab results saved to logs/{0}.txt",
                ["UsagePrefab"] = "Usage: {0} <build, find, fx, or all> [keyword]",
                ["UsagePrefabFind"] = "Usage: prefabs find <keyword>"
            }, this);
        }

        #endregion Localization

        #region Initializaton

        private const string permissionUse = "prefabsniffer.use";

        private Dictionary<string, UnityEngine.Object> files;
        private GameManifest.PooledString[] manifest;

        private void OnServerInitialized()
        {
            AddLocalizedCommand(nameof(CommandPrefab));
            permission.RegisterPermission(permissionUse, this);

            files = FileSystemBackend.cache;
            manifest = GameManifest.Current.pooledStrings;
        }

        #endregion Initialization

        #region Commands

        private void CommandPrefab(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permissionUse))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            if (args.Length == 0)
            {
                Message(player, "UsagePrefab", command);
                return;
            }

            List<string> resourcesList = new List<string>();
            string argName = "";

            switch (args[0].ToLower())
            {
                case "find":
                    if (args.Length > 2)
                    {
                        Message(player, "UsagePrefabFind", command);
                    }
                    foreach (GameManifest.PooledString asset in manifest)
                    {
                        if (asset.str.Contains(args[1]) && asset.str.EndsWith(".prefab"))
                        {
                            resourcesList.Add(asset.str);
                        }
                    }
                    argName = "find";
                    break;

                case "build":
                    foreach (string asset in files.Keys)
                    {
                        if ((!asset.StartsWith("assets/content/")
                            && !asset.StartsWith("assets/bundled/")
                            && !asset.StartsWith("assets/prefabs/"))
                            || !asset.EndsWith(".prefab")) continue;

                        if (asset.Contains(".worldmodel.")
                            || asset.Contains("/fx/")
                            || asset.Contains("/effects/")
                            || asset.Contains("/build/skins/")
                            || asset.Contains("/_unimplemented/")
                            || asset.Contains("/ui/")
                            || asset.Contains("/sound/")
                            || asset.Contains("/world/")
                            || asset.Contains("/env/")
                            || asset.Contains("/clothing/")
                            || asset.Contains("/skins/")
                            || asset.Contains("/decor/")
                            || asset.Contains("/monument/")
                            || asset.Contains("/crystals/")
                            || asset.Contains("/projectiles/")
                            || asset.Contains("/meat_")
                            || asset.EndsWith(".skin.prefab")
                            || asset.EndsWith(".viewmodel.prefab")
                            || asset.EndsWith("_test.prefab")
                            || asset.EndsWith("_collision.prefab")
                            || asset.EndsWith("_ragdoll.prefab")
                            || asset.EndsWith("_skin.prefab")
                            || asset.Contains("/clutter/")) continue;

                        GameObject go = GameManager.server.FindPrefab(asset);
                        if (go?.GetComponent<BaseEntity>() != null)
                        {
                            resourcesList.Add(asset);
                        }
                    }
                    argName = "build";
                    break;

                case "fx":
                    foreach (GameManifest.PooledString asset in manifest)
                    {
                        if ((!asset.str.StartsWith("assets/content/")
                            && !asset.str.StartsWith("assets/bundled/")
                            && !asset.str.StartsWith("assets/prefabs/"))
                            || !asset.str.EndsWith(".prefab")) continue;

                        if (asset.str.Contains("/fx/"))
                        {
                            resourcesList.Add(asset.str);
                        }
                    }
                    argName = "fx";
                    break;

                case "all":
                    foreach (GameManifest.PooledString asset in manifest)
                    {
                        resourcesList.Add(asset.str);
                    }
                    argName = "all";
                    break;

                default:
                    Message(player, "UsagePrefab", command);
                    break;
            }

            if (!string.IsNullOrEmpty(argName))
            {
                if (resourcesList.Count > 0)
                {
                    for (int i = 0; i < resourcesList.Count; i++)
                    {
                        player.Reply($"{i} - {resourcesList[i]}");
                        LogToFile(argName, $"{i} - {resourcesList[i]}", this);
                    }
                    Message(player, "ResultsSaved", $"{Name}/{Name.ToLower()}/{argName}-{DateTime.Now:yyyy-MM-dd}");
                } else {
                    Message(player, "NoResultsFound", args[1]);
                }
            }
        }

        #endregion Commands

        #region Helpers

        private void AddLocalizedCommand(string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages)
                {
                    if (message.Key.Equals(command))
                    {
                        if (!string.IsNullOrEmpty(message.Value))
                        {
                            AddCovalenceCommand(message.Value, command);
                        }
                    }
                }
            }
        }

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }

        private void Message(IPlayer player, string textOrLang, params object[] args)
        {
            if (player.IsConnected)
            {
                string message = GetLang(textOrLang, player.Id, args);
                player.Reply(message != textOrLang ? message : textOrLang);
            }
        }

        #endregion Helpers
    }
}
