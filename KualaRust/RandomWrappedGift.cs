using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("Random Wrapped Gift", "Ryz0r", "1.1.1")]
    [Description("Enables players with permission to receive a randomly wrapped gift in a configured interval.")]
    public class RandomWrappedGift : RustPlugin
    {
        private const string GifteePerm = "randomwrappedgift.giftee";
        private const string GifterPerm = "randomwrappedgift.gifter";
        public string EffectToUse = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab";
        private Random random = new Random();
        
        #region Config/Lang
        private Configuration _config;
        protected override void SaveConfig() => Config.WriteObject(_config);
        protected override void LoadDefaultConfig() => _config = new Configuration();

        private class Configuration
        {
            [JsonProperty(PropertyName = "Gift Items (Item Shortname)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> GiftItems = new Dictionary<string, int>
            {
                {"rifle.ak", 1},
                {"stones", 1500}
            };

            [JsonProperty(PropertyName = "Wrapped Gift Interval (Seconds)")]
            public float WrappedGiftInterval = 300f;

            [JsonProperty(PropertyName = "Play Effect When Opened?")]
            public bool EffectWhenOpened = true;
            
            [JsonProperty(PropertyName = "Give gift to sleepers with permissions?")]
            public bool GiftToSleepers = false;
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
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPerm"] = "You do not have permissions to use this command.",
                ["Given"] = "You have given players gifts. Yay!",
                ["Gifted"] = "You have received a randomly wrapped gift. Enjoy!"
            }, this); 
        }
        #endregion
        
        private void CreateGift(BasePlayer bp)
        {
            var theItem = _config.GiftItems.ElementAt(random.Next(0, _config.GiftItems.Count));
            var createdItem = ItemManager.CreateByName(theItem.Key);
            
            var soonWrapped = ItemManager.CreateByItemID(204970153, 1);
            
            soonWrapped.contents.AddItem(createdItem.info, theItem.Value);
            bp.GiveItem(soonWrapped);
            bp.ChatMessage(lang.GetMessage("Gifted", this, bp.UserIDString));

            if (_config.EffectWhenOpened)
            {
                EffectNetwork.Send(new Effect(EffectToUse, bp.GetNetworkPosition(), Vector3.zero), bp.net.connection);
            }
        }
        
        
        private void Init()
        {
            AddCovalenceCommand("give", nameof(GiveGiftCommand));
            permission.RegisterPermission(GifteePerm, this);
            permission.RegisterPermission(GifterPerm, this);
        }

        private void OnServerInitialized()
        {
            GiveGifts();
            timer.Every(_config.WrappedGiftInterval, GiveGifts);
        }

        private void GiveGifts()
        {
            if (_config.GiftToSleepers)
            {
                foreach (var p in BasePlayer.allPlayerList)
                {
                    if (permission.UserHasPermission(p.UserIDString, GifteePerm))
                    {
                        CreateGift(p);
                    }
                }
            }
            else
            {
                foreach (var p in BasePlayer.activePlayerList)
                {
                    if (permission.UserHasPermission(p.UserIDString, GifteePerm))
                    {
                        CreateGift(p);
                    }
                }
            }
        }

        private void GiveGiftCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, GifterPerm))
            {
                player.Reply(lang.GetMessage("NoPerm", this, player.Id));
                return;
            }
            
            player.Reply(lang.GetMessage("Given", this, player.Id));
            GiveGifts();
        }
    }
}