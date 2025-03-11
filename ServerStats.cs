
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Facepunch;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Rust;

using UnityEngine;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("ServerStats", "TheRyuzaki & skyplugins.ru", "1.0.6")]
    public class ServerStats : CovalencePlugin
    {
        private static ServerStats _instance;
        private WSH _wsh;
        private FPSVisor ActiveVisor;

        public class WSH
        {
            public WebSocket client;
            public bool hasUnloaded = false;
            public bool networkStatus = false;
            public List<string> pool = new List<string>();
            public Coroutine coroutine;
            public WSH(string WS)
            {
                client = new WebSocket(WS);
                client.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

                client.OnMessage += OnSocketMessage;
                client.OnOpen += OnSocketConnected;
                client.OnClose += OnSocketDisconnected;
            }

            public void connect()
            {
                if (!this.hasUnloaded)
                {
                    _instance.PrintWarning("[WS] Trying to establish a connection to server...");
                    try
                    {
                        client.ConnectAsync();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            public void close()
            {
                this.hasUnloaded = true;
                client.CloseAsync();
            }
            public bool send(Dictionary<string, object> packet, bool networkIgnore = false)
            {
                return this.send(JsonConvert.SerializeObject(packet, Formatting.None), networkIgnore);
            }

            public bool send(string packet, bool networkIgnore = false)
            {
                if (this.networkStatus || networkIgnore)
                {
#if DEBUG
                    _instance.PrintWarning($"[DEBUG][WS][<-----]: {packet}");
#endif
                    client.SendAsync(packet, new Action<bool>((completed) =>
                    {
                        if (!completed)
                        {
                            _instance.PrintWarning($"[WS] Something went wrong! The message was not sent and was added to the pool!");
                            pool.Add(packet);
                        }
                    }));

                    return true;
                }

                _instance.PrintWarning($"[WS] Send error! We are not connected! The message has been added to the pool and will be sent later!");
                pool.Add(packet);

                return false;
            }

            private void OnSocketConnected(object sender, EventArgs e)
            {
                _instance.PrintWarning("[WS] Socket Connected!");
                this.networkStatus = true;

                NetworkWelcomePacket packet = Pool.Get<NetworkWelcomePacket>();        

                this.client.SendAsync(JsonConvert.SerializeObject(packet), (res) => { });

                Pool.Free(ref packet);
                if (pool.Count > 0)
                {
                    foreach (string packets in pool)
                        this.send(packets, true);

                    pool.Clear();
                }

                this.coroutine = Global.Runner.StartCoroutine(this.Sender());
            }

            private void OnSocketMessage(object sender, MessageEventArgs e)
            {
#if DEBUG
                _instance.PrintWarning($"[DEBUG][WS][----->]: {e.Data}");
#endif
                _instance.NextFrame(() =>
                {
                    try
                    {

                    }
                    catch (Exception ex)
                    {
                        _instance.PrintError($"Exception from OnSocketMessage: {ex}");
                    }
                });
            }

            private void OnSocketDisconnected(object sender, CloseEventArgs e)
            {
                if (e == null || string.IsNullOrEmpty(e.Reason))
                    _instance.PrintWarning("[WS] Socket Disconnected.");
                else
                    _instance.PrintError($"[WS] Socket Disconnected: {e.Reason}");

                this.networkStatus = false;

                if (this.coroutine != null)
                    Global.Runner.StopCoroutine(this.coroutine);

                if (!this.hasUnloaded)
                {
                    _instance.timer.Once(10, this.connect);
                }
            }
            private IEnumerator Sender()
            {
                yield return CoroutineEx.waitForSeconds(1f);

                while (true)
                {
                    _instance.QueueWorkerThread(_ =>
                    {
                        NetworkTickPacket packet = Pool.Get<NetworkTickPacket>();

                        var listPlugins = _instance.plugins.PluginManager.GetPlugins().ToArray();

                        for (var i = 0; i < listPlugins.Length; i++)
                        {
                            packet.ListPlugins.Add(new NetworkTickPacket.PluginItem
                            {
                                Name = listPlugins[i].Name,
                                Version = listPlugins[i].Version.ToString(),
                                Author = listPlugins[i].Author,
                                Hash = listPlugins[i].Name.GetHashCode(),
                                Time = listPlugins[i].TotalHookTime
                            });
                        }


                        client.SendAsync(JsonConvert.SerializeObject(packet), (res) => { });
                        Pool.Free(ref packet);
                    });
                    yield return CoroutineEx.waitForSeconds(1f);
                }
            }

        }


        private void Init()
        {
            _instance = this;
            _wsh = new WSH("wss://s1.server-stats.skyplugins.ru:5191/");

            Unsubscribe("OnPlayerConnected");
            Unsubscribe("OnPlayerDisconnected");
            Unsubscribe("OnPluginLoaded");
            Unsubscribe("OnPluginUnloaded");
        }

        private void OnServerInitialized()
        {
            _wsh.connect();
            this.ActiveVisor = Terrain.activeTerrain.gameObject.AddComponent<FPSVisor>();
        }

        private void Unload()
        {
            _wsh.close();
        }























































        private int MinimalFPS = 9999;

        protected override void LoadDefaultConfig()
        {
            this.Config["Password"] = Random.Range(1000, 999999);
            this.LogWarning("Config file ServerStats.json is not found, you new password: " + this.Config["Password"]);
            this.Config.Save();
        }

        public class FPSVisor : MonoBehaviour
        {
            private void Update()
            {
                if (_instance.MinimalFPS > (int)global::Performance.current.frameRate)
                    _instance.MinimalFPS = (int)global::Performance.current.frameRate;
            }
        }


        public class NetworkWelcomePacket : Pool.IPooled
        {

            [JsonProperty("method")]
            public string Method { get; } = "reg_server";

            [JsonProperty("ServerIp")]
            public string ServerIp { get; } = _instance.server.Address + ":" + _instance.server.Port;
            [JsonProperty("serverName")]
            public string ServerName { get; } = _instance.server.Name;
            [JsonProperty("password")]
            public string Password => _instance.Config["Password"].ToString();

            public void EnterPool()
            {

            }

            public void LeavePool()
            {

            }
        }


        public class NetworkTickPacket: Pool.IPooled
        {
            [JsonProperty("method")]
            public string Method { get; } = "tick_server";
            [JsonProperty("listPlugins")]
            public List<PluginItem> ListPlugins { get; } = Pool.GetList<PluginItem>();

            [JsonProperty("minfps")]
            public int MinimalFps
            {
                get
                {
                    int currentValue = _instance.MinimalFPS;
                    _instance.MinimalFPS = 9999;
                    return currentValue;
                }
            }
            [JsonProperty("fps")]
            public int Fps => Performance.current.frameRate;

            [JsonProperty("ent")]
            public int Ent => BaseNetworkable.serverEntities.Count;
            [JsonProperty("online")]
            public int Online => _instance.players.Connected.Count();
            [JsonProperty("SleepPlayer")]
            public int SleepPlayer => BasePlayer.sleepingPlayerList.Count;
            [JsonProperty("JoiningPlayer")]
            public int JoiningPlayer => ServerMgr.Instance.connectionQueue.Joining;
            [JsonProperty("QueuedPlayer")]
            public int QueuedPlayer => ServerMgr.Instance.connectionQueue.Queued;

            public void EnterPool()
            {
                this.ListPlugins.Clear();
            }

            public void LeavePool()
            {

            }

            public struct PluginItem
            {
                [JsonProperty("name")]
                public string Name
                {
                    get; set;
                }
                [JsonProperty("author")]
                public string Author
                {
                    get; set;
                }
                [JsonProperty("version")]
                public string Version
                {
                    get; set;
                }
                [JsonProperty("hash")]
                public int Hash
                {
                    get; set;
                }
                [JsonProperty("time")]
                public double Time
                {
                    get; set;
                }
            }
        }
    }
}