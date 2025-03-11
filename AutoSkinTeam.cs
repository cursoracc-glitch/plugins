using System.Collections.Generic;
using Oxide.Core.Plugins;
using Newtonsoft.Json.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AutoSkinTeam", "Feyzi/Toolcub", "1.0.0")]
    public class AutoSkinTeam : RustPlugin
    {
        [PluginReference]
        private Plugin Clans;

        private HashSet<ulong> disabledChanges = new HashSet<ulong>();

        private const string PermUse = "autoskinteam.use";

        private void Init()
        {
            LoadData();
            permission.RegisterPermission(PermUse, this);
        }

        private void OnServerInitialized()
        {
            if (Clans == null)
            {
                Puts($"NO CLANS PLUGIN FOUND! UNLOADING PLUGIN!");
                Server.Command($"o.unload {Name}");
                return;
            }

            cmd.AddChatCommand("skinteam", this, nameof(ReskinCommand));
            cmd.AddChatCommand("autoskin", this, nameof(ReskinToggleCommand));
        }

        private void Unload()
        {
            SaveData();
        }

        private void ReskinCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            string clanTag = Clans.Call<string>("GetClanOf", player);
            if (string.IsNullOrEmpty(clanTag))
            {
                SendReply(player, Lang("NoClan", player.UserIDString));
                return;
            }

            JObject newClan = Clans.Call<JObject>("GetClan", clanTag);
            string clanOwner = (string)newClan["owner"];

            if (clanOwner != player.UserIDString)
            {
                bool validMod = false;
                foreach (string mod in newClan["moderators"])
                {
                    if (mod == player.UserIDString)
                    {
                        validMod = true;
                        break;
                    }
                }

                if (!validMod)
                {
                    SendReply(player, Lang("NotClanOwner", player.UserIDString));
                    return;
                }
            }

            SetSkins(player, (JArray)newClan["members"]);
        }

        private void ReskinToggleCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (disabledChanges.Contains(player.userID))
            {
                disabledChanges.Remove(player.userID);
                SendReply(player, Lang("SkinningEnabled", player.UserIDString));
            }
            else
            {
                disabledChanges.Add(player.userID);
                SendReply(player, Lang("SkinningDisabled", player.UserIDString));
            }
        }

        private void SetSkins(BasePlayer owner, JArray members)
        {
            Dictionary<string, ulong> playerSkins = new Dictionary<string, ulong>();
            foreach (var clothing in owner.inventory.containerWear.itemList)
                playerSkins.Add(clothing.info.shortname, clothing.skin);
            
            if (playerSkins.Count == 0)
            {
                SendReply(owner, Lang("NoItems", owner.UserIDString));
                return;
            }
            
            foreach (string member in members)
            {
                if (owner.UserIDString == member) 
                    continue;
                
                BasePlayer target = BasePlayer.FindAwakeOrSleeping(member);
                
                if (target == null) 
                    continue;
                
                if (disabledChanges.Contains(target.userID)) 
                    continue;
                
                bool changedAny = false;
                foreach (var item in target.inventory.containerWear.itemList)
                {
                    if (playerSkins.ContainsKey(item.info.shortname))
                    {
                        item.skin = playerSkins[item.info.shortname];
                        item.MarkDirty();
                        changedAny = true;
                    }
                }
                
                if (changedAny)
                    SendReply(target, Lang("SkinsUserUpdated", target.UserIDString));
            }
            SendReply(owner, Lang("SkinsUpdated", owner.UserIDString));
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoClan"] = "You are not in a clan!",
                ["NotClanOwner"] = "You are not a clan owner or moderator!",
                ["NoItems"] = "You don't have any wearable items on you!",
                ["SkinsUpdated"] = "Your teammates' skins have been updated!",
                ["SkinsUserUpdated"] = "Your skins have been updated by the clan owner.\nTo disable this feature, run /autoskin.",
                ["SkinningEnabled"] = "Clan owner skin synchronization has been enabled.",
                ["SkinningDisabled"] = "Clan owner skin synchronization has been disabled.",
                ["NoPermission"] = "You don't have permission to use this command!",
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void LoadData()
        {
            disabledChanges = Interface.Oxide.DataFileSystem.ReadObject<HashSet<ulong>>(Name);
            timer.Every(Core.Random.Range(500, 700), SaveData);
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, disabledChanges);
    }
}
