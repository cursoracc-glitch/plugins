using Oxide.Core;
using UnityEngine;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("SkipNight", "walkinrey", "1.0.4")]
    class SkipNight : RustPlugin
    {
      #region Configuration
      class Configuration
      {
        [JsonProperty(PropertyName = "Сколько секунд будет длиться голосование (по умолчанию 60)")]
        public float VoteTime = 60f;
        [JsonProperty(PropertyName = "Сообщение при пропуске ночи")]
        public string SkipNightString = "Голосование окончено. Большинство проголосовало за пропуск ночи.";
        [JsonProperty(PropertyName = "Сообщение против пропуска ночи")]
        public string DisskipNightString = "Голосование окончено. Большинство проголосовало против пропуска ночи, оставляем текущее время суток.";
        [JsonProperty(PropertyName = "Какое время устанавливать при пропуске ночи (по умолчанию 12)")]
        public float TimeSet = 12f;
        [JsonProperty(PropertyName = "Раз в сколько секунд проверять текущее время на сервере? (влияет на производительность сервера)")]
        public float CheckTime = 5f;
        [JsonProperty(PropertyName = "Во сколько начинать голосование по игровому времени? (по умолчанию 18)")]
        public float VoteGameTime = 18f;
        [JsonProperty(PropertyName = "Какие дни будут пропускаться")]
        public int[] daysDisable = {1, 5, 12, 18, 24};
      }
      protected override void LoadDefaultConfig() => config = new Configuration();
      protected override void LoadConfig()
      {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }
            Config.WriteObject(config, true);
      }
      #endregion
      #region References
      private Configuration config;
      private int AgreeVoted;
      private int DisagreeVoted;
      private bool VoteActive;
      private bool DisagreeActive;
      private List<BasePlayer> PlayersVoted = new List<BasePlayer>();
      [PluginReference] private Plugin ImageLibrary;
      #endregion
      #region Methods
      private void OnServerInitialized(bool initial) 
      {
         StartTimer();
      }
      [ChatCommand("disagreevote")]
      private void DisagreeVote(BasePlayer player)
      {
        if(PlayersVoted.Contains(player))
        {
          SendReply(player, "Вы уже голосовали раннее!");
        }
        else
        {
          if(VoteActive == true)
          {
                DestroyGUI(player);
                DisagreeVoted += 1;
                PlayersVoted.Add(player);
          }
        else
          {
            SendReply(player, "В данный момент не проходит голосование.");
          }   
        }
      }
      [ChatCommand("agreevote")]
      private void AgreeVote(BasePlayer player)
      {
        if(PlayersVoted.Contains(player))
        {
          SendReply(player, "Вы уже голосовали раннее!");
        }
        else
        {
                 if(VoteActive == true)
        {
        DestroyGUI(player);
        AgreeVoted += 1;
        PlayersVoted.Add(player);
        }
        else
        {
            SendReply(player, "В данный момент не проходит голосование.");
        }   
        }
      }
      private void VoteTimer()
      {
        float seconds = config.VoteTime;
        timer.Once(seconds, () =>
        {
           if(AgreeVoted > DisagreeVoted)
           {
             PrintToChat(config.SkipNightString);
             covalence.Server.Command("env.time " + config.TimeSet);
             SetDay();
             DestroyGUIAll();
           }
           else
           {
               PrintToChat(config.DisskipNightString);
               DestroyGUIAll();
               DisagreeActive = true;
           }
           VoteActive = false;
           AgreeVoted = 0;
           PlayersVoted = new List<BasePlayer>();
           DisagreeVoted = 0;
        });
      }
      private void SetDay()
      {
        int day = (TOD_Sky.Instance.Cycle.Day) + 1;
        if(day > 30) day = 1;
        TOD_Sky.Instance.Cycle.Day = day;
      }
      private float GetCurrentTime()
      {
        float time = TOD_Sky.Instance.Cycle.Hour;
        return time;
      }
      private bool IsDayCheck()
      {
         float time = GetCurrentTime();
         if(time < 18) return true;
         else return false;
      }
      private void StartTimer()
      {
        VoteActive = false;
         timer.Every(config.CheckTime, () =>
         {
           if(VoteActive == false)
           {
             float currentTime = GetCurrentTime();
             bool isDay = IsDayCheck();
             if(DisagreeActive == true && isDay == true) DisagreeActive = false;
             if(currentTime > config.VoteGameTime && DisagreeActive == false) CreateVoteGUI();
           }
         });
      }
      #region Hooks
      private void Loaded()
      {
        if(ImageLibrary == null)
        {
           PrintError("ОТКЛЮЧЕНИЕ ПЛАГИНА. У вас не установлен ImageLibrary!");
           Interface.Oxide.UnloadPlugin(Title);
           return;
        }
        ImageLibrary.CallHook("AddImage", "https://i.imgur.com/XPBv6WR.png", "SkipNightUI");
         LoadConfig();
      }
      private void Unload()
      {
        DestroyGUIAll();
        PlayersVoted = new List<BasePlayer>();
        AgreeVoted = 0;
        DisagreeVoted = 0;
      }
      #endregion
      #endregion
        #region GUI
        private void DestroyGUI(BasePlayer player)
        {
          CuiHelper.DestroyUi(player, "ButtonNo");
          CuiHelper.DestroyUi(player, "ImageVote");
          CuiHelper.DestroyUi(player, "ButtonYes");
        }
        private void DestroyGUIAll()
        {
           var activePlayerList = BasePlayer.activePlayerList.ToArray().ToList();
           foreach(var players in activePlayerList)
           {
          CuiHelper.DestroyUi(players, "ButtonNo");
          CuiHelper.DestroyUi(players, "ImageVote");
          CuiHelper.DestroyUi(players, "ButtonYes");
           }
        }
        private void CreateVoteGUI()
        {
          bool isSkipDay = false;
          for(int i = 0; i < config.daysDisable.Length; i++)
          {
            if(TOD_Sky.Instance.Cycle.Day == config.daysDisable[i]) {isSkipDay = true; break;}
          }
          if(isSkipDay == true) return;
          VoteTimer();
          VoteActive = true;
           var elements = CreateObjects();
           var activePlayerList = BasePlayer.activePlayerList.ToArray().ToList();
           foreach(var players in activePlayerList)
           {
              CuiHelper.AddUi(players, elements);
           }
        }
        private CuiElementContainer CreateObjects()
        {
          var elements = new CuiElementContainer();
          elements.Add(new CuiElement
            {
              Name = "ImageVote",
              Parent = "Overlay",
                Components =
                {       
                    new CuiRawImageComponent
                    {
                        Png = (string)ImageLibrary.CallHook("GetImage", "SkipNightUI")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.051",
                        AnchorMax = "0.281 0.19"
                    }
                }
            });
          var ButtonNo = elements.Add(new CuiButton
          {
            Button =
            {
              Close = "",
              Color = "0.31 0.31 0.31 0",
              Command = "chat.say /disagreevote"
            },
            RectTransform = 
            {
              AnchorMin = "0.091 0.076",
              AnchorMax = "0.159 0.115"
            },
            Text =
            {
              Text = "",
              FontSize = 11,
              Align = TextAnchor.MiddleCenter
            }
          }, "Overlay", "ButtonNo");
          var ButtonYes = elements.Add(new CuiButton
          {
            Button =
            {
              Close = "",
              Color = "0.31 0.31 0.31 0",
              Command = "chat.say /agreevote"
            },
            RectTransform = 
            {
              AnchorMin = "0.011 0.079",
              AnchorMax = "0.077 0.115"
            },
            Text =
            {
              Text = "",
              FontSize = 11,
              Align = TextAnchor.MiddleCenter
            }
          }, "Overlay", "ButtonYes");
          return elements;
        }
        #endregion
    }
}