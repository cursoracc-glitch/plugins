using System.Collections.Generic;
using System.Linq;
using Network;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LoadingMessages", "VVoid", "1.0.1", ResourceId = 2762)]
    [Description("Shows custom texts on loading screen.")]
    public class LoadingMessages : RustPlugin
    {
        private readonly Dictionary<ulong, Connection> _clients = new Dictionary<ulong, Connection>();
        private readonly List<ulong> _disconnectedClients = new List<ulong>();

        #region Variables

        private MsgConfig _config;
        private Timer _timer;
        private MsgEntry _currentMsg;
        private int _msgIdx;
        private float _nextMsgChange;

        #endregion

        #region Config

        private class MsgConfig
        {
            [JsonProperty("Text Display Frequency (Seconds)")]
            public float TimerFreq;
            [JsonProperty("Enable Messages Cyclicity")]
            public bool EnableCyclicity;
            [JsonProperty("Use Random Cyclicity (Instead of sequential)")]
            public bool EnableRandomCyclicity;
            [JsonProperty("Cycle Messages Every ~N Seconds")]
            public float CyclicityFreq;
            [JsonProperty("Messages")]
            public List<MsgEntry> Messages;
            [JsonProperty("Last Message (When entering game)")]
            public MsgEntry LastMessage;
        }

        private class MsgEntry
        {
            [JsonProperty("Top Status")]
            public string TopString;
            [JsonProperty("Bottom Status")]
            public string BottomString;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<MsgConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new MsgConfig
            {
                TimerFreq = .30f,
                EnableCyclicity = false,
                EnableRandomCyclicity = false,
                CyclicityFreq = 3.0f,
                Messages = new List<MsgEntry>
                {
                    new MsgEntry{TopString = "<color=yellow>Welcome to our server!</color>", BottomString = "<color=lightblue>Enjoy your stay.</color>"},
                    new MsgEntry{TopString = "<color=lightblue>Welcome to our server!</color>", BottomString = "<color=yellow>Enjoy your stay.</color>"}
                },
                LastMessage = new MsgEntry {TopString = "<color=yellow>Welcome to our server!</color>", BottomString = "<color=green>Entering game...</color>"}
            };
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Hooks

        private void Init()
        {
            if (_config.Messages == null || _config.Messages.Count == 0)
            {
                Unsubscribe(nameof(OnUserApprove));
                Unsubscribe(nameof(OnPlayerInit));
                PrintWarning("No loading messages defined! Check your config.");
                return;
            }
            if (_config.EnableCyclicity && _config.Messages.Count <= 1)
            {
                _config.EnableCyclicity = false;
                PrintWarning("You have message cyclicity enabled, but only 1 message is defined. Check your config.");
            }
            _currentMsg = _config.Messages.First();
        }


        private void OnUserApprove(Connection connection)
        {
            _clients[connection.userid] = connection;
            if (_timer == null)
                _timer = timer.Every(_config.TimerFreq, HandleClients);
            DisplayMessage(connection, _currentMsg);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            _clients.Remove(player.userID);
            DisplayMessage(player.Connection, _config.LastMessage ?? _currentMsg);
        }

        #endregion

        #region Logic

        private void UpdateCurrentMessage()
        {
            if (!_config.EnableCyclicity || Time.realtimeSinceStartup < _nextMsgChange)
                return;
            _nextMsgChange = Time.realtimeSinceStartup + _config.CyclicityFreq;
            if (_config.EnableRandomCyclicity)
                _currentMsg = PickRandom(_config.Messages);
            else
            {
                _currentMsg = _config.Messages[_msgIdx++];
                if (_msgIdx >= _config.Messages.Count)
                    _msgIdx = 0;
            }
        }

        private void HandleClients()
        {
            if (_clients.Count == 0)
            {
                _timer.Destroy();
                _timer = null;
                return;
            }
            UpdateCurrentMessage();
            foreach (var client in _clients.Values)
            {
                if (!client.active)
                {
                    _disconnectedClients.Add(client.userid);
                    continue;
                }
                if (client.state == Connection.State.InQueue)
                    continue;
                DisplayMessage(client, _currentMsg);
            }

            if (_disconnectedClients.Count == 0)
                return;
            _disconnectedClients.ForEach(uid=>_clients.Remove(uid));
            _disconnectedClients.Clear();
        }

        private static void DisplayMessage(Connection con, MsgEntry msgEntry)
        {
            if (!Net.sv.write.Start())
                return;
            Net.sv.write.PacketID(Message.Type.Message);
            Net.sv.write.String(msgEntry.TopString);
            Net.sv.write.String(msgEntry.BottomString);
            Net.sv.write.Send(new SendInfo(con));
        }

        private static T PickRandom<T>(List<T> list) => list[Random.Range(0, list.Count - 1)];

        #endregion
    }
}
