using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Rust;
using UnityEngine;
using Server = ConVar.Server;

namespace Oxide.Plugins
{
    [Info("Raid Alerts", "Ryz0r/Mevent", "1.0.2")]
    [Description("Allows players with permissions to receive alerts when explosives are thrown or fired.")]
    public class RaidAlerts : CovalencePlugin
    {
        private const string RaidAlertCommands = "raidalerts.use";

        private readonly Dictionary<string, float> _alertedUsers = new Dictionary<string, float>();

        private readonly List<string> _enabledPlayersList = new List<string>();

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _enabledPlayersList);
        }

        #region Configuration

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "UseWebhook")]
            public bool UseWebhook;

            [JsonProperty(PropertyName = "WebhookURL")]
            public string WebhookUrl = "";

            [JsonProperty(PropertyName = "OutputCooldown")]
            public float OutputCooldown = 5f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("A new configuration file is being generated.");
            _config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["IncorrectArgs"] = "You are using the command incorrectly. Try /alerts on or /alerts off.",
                ["AlreadyReceiving"] = "You are already receiving alerts. Try /alerts off.",
                ["NotReceiving"] = "You are not receiving alerts. Try /alerts on.",
                ["NowReceiving"] = "You will now receive alerts when a raid is happening.",
                ["NoLongerReceiving"] = "You will no longer receive alerts when a raid is happening.",
                ["ThrownAlert"] = "{0} has thrown a {1} at the location {2}.",
                ["FiredAlert"] = "{0} has fired a Rocket/HE Grenade at the location {1}.",
                ["GenRaidAlert"] = "{0} is using explosive ammo or fire ammo at the location {1}.",
                ["NoPerm"] = "You don't have the permissions to use this command."
            }, this);
        }

        private void OnNewSave(string filename)
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name))
            {
                Interface.Oxide.DataFileSystem.GetFile(Name).Clear();
                Interface.Oxide.DataFileSystem.GetFile(Name).Save();

                Puts($"Wiped '{Name}.json'");
            }
        }

        private void Init()
        {
            permission.RegisterPermission(RaidAlertCommands, this);
        }

        [Command("alerts")]
        private void AlertsCommand(IPlayer player, string command, string[] args)
        {
            if (player.HasPermission(RaidAlertCommands))
            {
                if (args.Length == 0 || args.Length > 1)
                {
                    player.Reply(lang.GetMessage("IncorrectArgs", this, player.Id));
                    return;
                }

                switch (args[0])
                {
                    case "on":
                        if (_enabledPlayersList.Contains(player.Id))
                        {
                            player.Reply(lang.GetMessage("AlreadyReceiving", this, player.Id));
                            return;
                        }

                        _enabledPlayersList.Add(player.Id);
                        player.Reply(lang.GetMessage("NowReceiving", this, player.Id));
                        SaveData();
                        break;
                    case "off":
                        if (!_enabledPlayersList.Contains(player.Id))
                        {
                            player.Reply(lang.GetMessage("NotReceiving", this, player.Id));
                            return;
                        }

                        _enabledPlayersList.Remove(player.Id);
                        player.Reply(lang.GetMessage("NoLongerReceiving", this, player.Id));
                        SaveData();
                        break;
                }
            }
            else
            {
                player.Reply(lang.GetMessage("NoPerm", this, player.Id));
            }
        }

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            var playerThrow = player.displayName;
            var entityLocation = entity.transform.position;
            var explosiveUsed = item.ShortPrefabName;

            switch (explosiveUsed)
            {
                case "explosive.timed.entity":
                    explosiveUsed = "C4";
                    break;
                case "explosive.satchel.entity":
                    explosiveUsed = "Satchel";
                    break;
                case "grenade.beancan.entity":
                    explosiveUsed = "Beancan";
                    break;
                case "grenade.f1.entity":
                    explosiveUsed = "Grenade";
                    break;
                case "survey_charge":
                    explosiveUsed = "Survey Charge";
                    break;
            }

            foreach (var user in BasePlayer.activePlayerList)
                if (_enabledPlayersList.Contains(user.UserIDString))
                    user.ChatMessage(string.Format(lang.GetMessage("ThrownAlert", this, player.UserIDString),
                        playerThrow, explosiveUsed, GetGrid(entityLocation)));

            if (_config.UseWebhook && _config.WebhookUrl != null)
                SendDiscordMessage(playerThrow, entityLocation, explosiveUsed);
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!info.damageTypes.Has(DamageType.Explosion) || info.InitiatorPlayer == null ||
                _alertedUsers.ContainsValue(entity.transform.position.x)) return null;

            var attackerName = info.InitiatorPlayer.displayName;

            timer.Once(_config.OutputCooldown, () => _alertedUsers.Remove(attackerName));

            _alertedUsers[attackerName] = entity.transform.position.x;

            foreach (var user in BasePlayer.activePlayerList.Where(user =>
                _enabledPlayersList.Contains(user.UserIDString)))
            {
                user.ChatMessage(string.Format(lang.GetMessage("GenRaidAlert", this, user.UserIDString),
                    attackerName, GetGrid(entity.transform.position)));
                return true;
            }

            if (_config.UseWebhook && _config.WebhookUrl != null)
                SendDiscordMessage(attackerName, entity.transform.position, "Explo/Fire");

            return null;
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            var playerThrow = player.displayName;
            var entityLocation = entity.transform.position;

            foreach (var user in BasePlayer.activePlayerList.Where(user =>
                _enabledPlayersList.Contains(user.UserIDString)))
                user.ChatMessage(string.Format(lang.GetMessage("FiredAlert", this, player.UserIDString),
                    playerThrow, GetGrid(entityLocation)));

            if (_config.UseWebhook && _config.WebhookUrl != null)
                SendDiscordMessage(playerThrow, entityLocation, "Rocket/HE Grenade");
        }

        private void SendDiscordMessage(string playerName, Vector3 entityLocation, string explosive)
        {
            var embed = new Embed()
                .AddField("Player Name:", playerName, true)
                .AddField("Explosive Used:", explosive, false)
                .AddField("Rocket Location:", GetGrid(entityLocation), false);

            webrequest.Enqueue(_config.WebhookUrl, new DiscordMessage("", embed).ToJson(), (code, response) => { },
                this,
                RequestMethod.POST, new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                });
        }

        #region Discord Stuff

        private class DiscordMessage
        {
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = content;
                Embeds = embeds.ToList();
            }

            [JsonProperty("content")] public string Content { get; set; }
            [JsonProperty("embeds")] public List<Embed> Embeds { get; set; }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        private class Embed
        {
            [JsonProperty("fields")] public List<Field> Fields { get; set; } = new List<Field>();

            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, Regex.Replace(value, "<.*?>", string.Empty), inline));

                return this;
            }
        }

        private class Field
        {
            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }

            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("value")] public string Value { get; set; }
            [JsonProperty("inline")] public bool Inline { get; set; }
        }

        #endregion

        #region Utils

        private static string GetGrid(Vector3 pos)
        {
            var letter = 'A';
            var x = Mathf.Floor((pos.x + Server.worldsize / 2f) / 146.3f) % 26;
            var z = Mathf.Floor(Server.worldsize / 146.3f) -
                    Mathf.Floor((pos.z + Server.worldsize / 2f) / 146.3f);
            letter = (char)(letter + x);
            return $"{letter}{z}";
        }

        #endregion
    }
}