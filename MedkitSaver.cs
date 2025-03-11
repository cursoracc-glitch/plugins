//Requires: ImageLibrary

using UnityEngine;
using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("MedkitSaver", "walkinrey", "1.0.4")]
    class MedkitSaver : RustPlugin
    {
      #region Configuration
      class Conf
      {
         [JsonProperty("Сколько моментально установить хп при использовании аптечки (по умолчанию 15)")] public float hpMomental = 15f;
         [JsonProperty("Сколько хп восстанавливать при использовании аптечки (по умолчанию 100)")] public float hpRevive = 100f;
         [JsonProperty("Выводить сообщения в чат о использовании аптечки? (по умолчанию да)")] public bool messageUse = true;
         [JsonProperty("Сообщение о использовании аптечки")] public string messageString = "Вы использовали <color=red>аптечку!</color>";
      }
      protected override void LoadDefaultConfig() => config = new Conf();
      protected override void LoadConfig()
      {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Conf>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }
            Config.WriteObject(config, true);
            Config.ReadObject<Conf>();
      }
      #endregion
        #region References
        private Conf config;
        [PluginReference] private Plugin ImageLibrary;
        List<string> steamIDs = new List<string>();
        #endregion
        #region Hooks
        object OnPlayerWound(BasePlayer player, BasePlayer source)
        {       
          if(player.inventory.GetAmount(254522515) > 0)
          {
            CuiHelper.AddUi(player, CreateElements());
            steamIDs.Add(player.userID.ToString());
          }
            return null;
        }
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if(player != null && player.userID.IsSteamId() && player.IsConnected && steamIDs.Contains(player.userID.ToString()))
            {
                DestroyMedkitUi(player);
                steamIDs.Remove(player.userID.ToString());
            }
            return null;
        }
        void Loaded()
        {
           LoadConfig();
        }
        #endregion
        #region Methods
        CuiElementContainer CreateElements()
        {
          CuiElementContainer container = new CuiElementContainer();
          CuiElement medkitPanel = new CuiElement
          {
             Name = "medkit_panel",
             Parent = "Overlay",
             Components = 
             {
               new CuiRawImageComponent
               {
                  Color = "0.49 0.49 0.49 0.39",
                  Sprite = "Assets/Content/UI/UI.Background.Tile.psd"
               },
               new CuiRectTransformComponent
               {
                 AnchorMin = "0.648 0.015",
                 AnchorMax = "0.688 0.086"
               }
             }
          };
          container.Add(medkitPanel);
          CuiElement medkitImage = new CuiElement
          {
            Name = "medkit_image",
            Parent = "Overlay",
            Components =
            {
              new CuiRawImageComponent
              {
                Png = (string)ImageLibrary.Call("GetImage", "largemedkit")
              },
              new CuiRectTransformComponent
              {
                AnchorMin = "0.650 0.019",
                AnchorMax = "0.688 0.086"
              }
            }
          };
          CuiElement medkitButton = new CuiElement
          {
            Name = "medkit_button",
            Parent = "Overlay",
            Components =
            {
              new CuiButtonComponent
              {
                Command = "medkituse",
                Color = "0 0 0 0"
              },
              new CuiRectTransformComponent
              {
                AnchorMin = "0.648 0.015",
                AnchorMax = "0.688 0.086"
              }
            }
          };
          container.Add(medkitImage);
          container.Add(medkitButton);
          return container;
        }
        [ConsoleCommand("medkituse")]
        private void cmd_Use(ConsoleSystem.Arg arg)
        {
          var player = arg.Player();
          DestroyMedkitUi(player);
          player.inventory.Take(null, 254522515, 1);
          player.IPlayer.Health = config.hpMomental;
          player.metabolism.pending_health.value = config.hpRevive;
          player.StopWounded();
          if(config.messageUse) SendReply(player, config.messageString);
        }
        private void DestroyMedkitUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "medkit_panel");
            CuiHelper.DestroyUi(player, "medkit_image");
            CuiHelper.DestroyUi(player, "medkit_button");
        }
        #endregion
    }
}