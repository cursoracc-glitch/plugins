using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Linq;
using GameTips;
using UnityEngine.UI;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SlotPanel", "Morecelo", "0.2.1")]
    [Description("Плагин добавляет ГУИ панель под слоты игрока, которая отображает основные события на сервере")]
    class SlotPanel : RustPlugin
    {
        #region Переменные
        
        // Конфиг | Отображение убийств
        private bool P_ShowKill = true;
        
        // Конфиг | Отображение случайных сообщений
        private bool P_ShowMessages = true;
        private bool P_SM_DoubleToChat = false;
        private int P_SM_Interval = 20;
        
        // Конфиг | Отображение подключений к серверу
        private bool P_ShowConnections = true;
        private bool P_ShowDisconnections = false;
        
        // Конфиг | Дополнительные параметры
        private string P_ShadowSize = "";
        private string P_FontName = "";
        private int P_FontSize = 14;
        
        #endregion

        #region Инициализация
        
        void OnServerInitialized()
        {
            LoadDefaultConfig();
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                // Случайные сообщения в чате должны начинаться со слова RANDOM
                { "RANDOM_1", "Добро пожаловать на сервер, <color=#FF5733>%NAME%</color>!" },
                { "RANDOM_2", "На дворе <color=#FF5733>%TIME%</color>, а ты на сервере? Молодец!"},
                { "RANDOM_3", "Текущая статистика сервера <color=#FF5733>%ONLINE%</color> / <color=#FF5733>%SLOTS%</color>!" },
                { "RANDOM_4", "<color=#FF5733>%NAME%</color>, спасибо что остаёшься с нами!" },
                { "RANDOM_5", "Не забудь <color=#FF5733>подписаться</color> на нашу группу ВК!" },
                { "RANDOM_6", "Ты можешь <color=#FF5733>заработать</color>, сообщив нам об ошибке!" },
                // Отправляться будут только те сообщения, ключ которых начинается со слова RANDOM
                { "TEACH", "Чтобы добавить новое сообщение в панель, добавьте новую строку, в ключе укажите RANDOM_ и любое раннее не использованное число"},
                { "KILL_FORMAT", "<color=#FF5733>%KILLER%</color> застрелил <color=#FF5733>%DEAD%</color>" },
                { "SUICIDE_FORMAT", "<color=#FF5733>%KILLER%</color> застрелился" },
                { "CONNECT_FORMAT", "<color=#FF5733>%NAME%</color> присоединился к серверу" },
                { "DISCONNECT_FORMAT", "<color=#DC143C>%NAME%</color> отсоединился от сервера" },
            }, this, "en");

            if (P_ShowMessages)
                timer.Every(P_SM_Interval, () => StartBroadcast());
        }

        protected override void LoadDefaultConfig()
        {
            Config["Отображать убийства в панеле"] = P_ShowKill = GetConfig("Отображать убийства в панеле", true);
            Config["Отображать случайные сообщения"] = P_ShowMessages = GetConfig("Отображать случайные сообщения", true);
            Config["Отображать подключения к серверу"] = P_ShowConnections = GetConfig("Отображать подключения к серверу", true);
            Config["Отображать отключения от сервера"] = P_ShowDisconnections = GetConfig("Отображать отключения от сервера", false);
            
            Config["Дублировать случайное сообщение в чат"] = P_SM_DoubleToChat = GetConfig("Дублировать случайное сообщение в чат", false);
            Config["Интервал показа случайных сообщений"] = P_SM_Interval = GetConfig("Интервал показа случайных сообщений", 60);
            
            Config["Название шрифта в панеле"] = P_FontName = GetConfig("Название шрифта в панеле", "robotocondensed-regular.ttf");
            Config["Размер обводки текста"] = P_ShadowSize = GetConfig("Размер обводки текста", "0.155 0.155");
            Config["Размер шрифта в панеле"] = P_FontSize = GetConfig("Размер шрифта в панеле", 14);
            
            SaveConfig();
        }

        private void Unload()
        {
            BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, "AlertMessage"));
        }
        
        #endregion
        
        #region Функции
        
        void StartBroadcast()
        {
            string stringToBroadcast = GetMessage();
            foreach (var check in BasePlayer.activePlayerList)
                PanelGUI(check, stringToBroadcast);
            
            if (P_SM_DoubleToChat)
                Server.Broadcast(stringToBroadcast);
        }

        string GetMessage()
        {
            List<string> tempList = new List<string>();
            foreach (var badAlgorithm in lang.GetMessages("en", this).Where(p => p.Key.StartsWith("RANDOM")))
                tempList.Add(badAlgorithm.Value);
            
            return tempList.GetRandom();
        }

        string ReplaceMessage(BasePlayer player, string currentText)
        {
            currentText = currentText.Replace("%NAME%", player.displayName);
            currentText = currentText.Replace("%ONLINE%", BasePlayer.activePlayerList.Count.ToString());
            currentText = currentText.Replace("%SLEEPER%", BasePlayer.sleepingPlayerList.Count.ToString());
            currentText = currentText.Replace("%SLOTS%", ConVar.Server.maxplayers.ToString());
            currentText = currentText.Replace("%TIME%", DateTime.Now.ToShortTimeString());

            return currentText;
        }
        
        #endregion

        #region Хуки
        
        void OnPlayerInit(BasePlayer player)
        {
            if (!P_ShowConnections)
                return;
            
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            
            foreach (var check in BasePlayer.activePlayerList)
                PanelGUI(check, lang.GetMessage("CONNECT_FORMAT", this).Replace("%NAME%", player.displayName));
        }
        
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (P_ShowDisconnections)
                foreach (var check in BasePlayer.activePlayerList)
                    PanelGUI(check, lang.GetMessage("DISCONNECT_FORMAT", this).Replace("%NAME%", player.displayName));
        }
        
        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (!P_ShowKill)
                return;

            if (!(info?.Initiator is BasePlayer))
                return;

            string killMessage;

            if (info?.InitiatorPlayer != player)
            {
                string killer = info?.InitiatorPlayer.displayName;
                string victim = player.displayName;
                if (info?.InitiatorPlayer.GetComponent<NPCPlayer>() != null)
                    killer = "Учёный";
                if (player.GetComponent<NPCPlayer>() != null)
                    victim = "Учёного";
                
                killMessage = lang.GetMessage("KILL_FORMAT", this);
                killMessage = killMessage.Replace("%KILLER%", killer);
                killMessage = killMessage.Replace("%DEAD%", victim);
                killMessage = killMessage.Replace("%DISTANCE%", Math.Floor(Vector3.Distance(player.transform.position, info.InitiatorPlayer.transform.position)).ToString());
            }
            else
            {
                killMessage = lang.GetMessage("SUICIDE_FORMAT", this);
                killMessage = killMessage.Replace("%KILLER%", player.displayName);
            }
            
            foreach (var check in BasePlayer.activePlayerList)
                    PanelGUI(check, killMessage);
        }

        #endregion
        
        #region GUI
        
        void PanelGUI(BasePlayer player, string message)
        {
            CuiHelper.DestroyUi(player, "AlertMessage.Box");
            
            var Panel = new CuiElementContainer();
            var PanelBox = Panel.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = "0.2786458 0", AnchorMax = "0.7057291 0.02129629" },
                CursorEnabled = false,
            }, "Hud", "AlertMessage");

            Panel.Add(new CuiElement
            {
                FadeOut = 0.5f,
                Name = "AlertMessage.Box",
                Parent = "AlertMessage",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = ReplaceMessage(player, message),
                        Align = TextAnchor.MiddleCenter,
                        Font = P_FontName,
                        FontSize = P_FontSize,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent() { Color = "0 0 0 1", Distance = P_ShadowSize}
                }
            });
            CuiHelper.AddUi(player, Panel);
            timer.Once(5, () => CuiHelper.DestroyUi(player, "AlertMessage.Box"));
        }
        
        #endregion
        
        #region Helper
        
        void Reply(BasePlayer player, string message, params object[] args) => SendReply(player, lang.GetMessage(message, this, player.UserIDString), args);
        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
        
        #endregion
    }
}