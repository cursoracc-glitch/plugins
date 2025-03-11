using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using UnityEngine;
using Network;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("ESPBlocker", "RustPlugin.RU", "1.0.0")]
    class ESPBlocker : RustPlugin
    {
        class ESPPlayer : MonoBehaviour
        {
            private static int blockedLayer = LayerMask.GetMask("Deployed","Player (Server)", "Prevent Building");

            Transform transform;
            BasePlayer player;
            List<BaseEntity> blockedEntities = new List<BaseEntity>();
            int current = 0;
            bool init = true;
            
            void Awake()
            {
                transform = GetComponent<Transform>();
                player = GetComponent<BasePlayer>();
            }

            float nextTick = 0;
            float nextUpdaterStorages = 0;
            Vector3 lastPosition = Vector3.zero;
            bool IsTimerBlock()
            {
                var time = Time.time;
                if (time < nextTick) return true;
                nextTick = time + 0.1f;        
                return false;
            }

            void UpdateBlockedEntities()
            {
                var time = Time.time;
                if (time < nextUpdaterStorages) return;
                nextUpdaterStorages = time + UnityEngine.Random.Range(2, 5);
                blockedEntities.RemoveAll(s => s.IsDestroyed || s.net == null);
                blockedEntities.Clear();
                Vis.Entities(transform.position, instance.storageRadius, blockedEntities, blockedLayer,
                    QueryTriggerInteraction.Collide);
                foreach (var entity in blockedEntities)
                {
                    if (!instance.whitelists.ContainsKey(entity.net.ID))
                        instance.whitelists[entity.net.ID] = new List<ulong>();
                }
                blockedEntities.RemoveAll(e => e?.net?.ID == null || instance.whitelists[e.net.ID].Contains(player.userID) || !instance.IsBlockEntity(e));
                if (init)
                {
                    init = false;
                    foreach (var storage in blockedEntities)
                        instance.DestroyClientEntity(player,storage);
                }
            }

            bool IsAFK()
            {
                if (lastPosition == transform.position || (transform.position - lastPosition).magnitude < 0.0001f)
                {
                    return true;
                }
                lastPosition = transform.position;
                return false;
            }

            void FixedUpdate()
            {
                if (IsTimerBlock()) return;
                UpdateBlockedEntities();

                if (current == 0 && IsAFK()) return;

                if (blockedEntities.Count <= 0||--current < 0 || current > blockedEntities.Count-1)
                {
                    current = blockedEntities.Count;
                    return;
                }

                

                var entity = blockedEntities[current];
                if (entity == null || entity.net?.ID == null)
                {
                    blockedEntities.RemoveAt(current);
                    return;
                }
                Vector3 entityPosition = entity.CenterPoint();
                if (CanVisible(entityPosition))
                {

                    instance.SetVisibleEntity(entity, player.userID);
                    blockedEntities.RemoveAt(current);
                    entity.SendNetworkUpdate();
                }
            }
            public bool ContainsAny( string value, params string[] args )
            {
                return args.Any( value.Contains );
            }
            bool CanVisible(Vector3 pos)
            {
                RaycastHit[] hits = new RaycastHit[50];
                Vector3 pos1 = pos;
                Vector3 pos2 = player.eyes.position;
                var length = Physics.RaycastNonAlloc(new Ray(pos1, (pos2 - pos1)), hits, instance.storageRadius,
                    LayerMask.GetMask("Construction", "World", "Terrain", "Player (Server)"),
                    QueryTriggerInteraction.Collide);
                var objhits = new RaycastHit[length];
                for (int i = 0; i < length; i++)
                    objhits[i] = hits[i];

                var results = objhits.OrderBy(h => h.distance).Select(p => p.GetEntity()).Where(p => p).ToList();
                results.RemoveAll(p => p.ShortPrefabName != "wall" &&
                !ContainsAny(p.ShortPrefabName, "foundation", "door", "player", "floor"));

                if (results.Count > 0)
                {
                    var result = results[0];
                    if (player.IsAdmin)
                    {
                        foreach (var p in BasePlayer.activePlayerList)
                            if (p.GetCenter() == pos)
                        instance.Arrow(player, pos1, pos2);
                    }
                    if (result == player)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        float storageRadius;
        bool adminIgnore;
        bool clansSupport;

        protected override void LoadDefaultConfig()
        {
            Config[ "Радиус видимости ящиков" ] = storageRadius =  GetConfig( "Радиус видимости ящиков", 50f);
            Config[ "Игнорирование админов" ] = adminIgnore =  GetConfig( "Игнорирование админов", true );
            Config[ "Поддержка кланов(Уменьшает нагрузку если есть игроки, играющие вместе)" ] = clansSupport = GetConfig( "Поддержка кланов(Уменьшает нагрузку если есть игроки, играющие вместе)", true );            
			
			SaveConfig();
			        }
        T GetConfig<T>( string name, T defaultValue )
            => Config[ name ] == null ? defaultValue : (T) Convert.ChangeType( Config[ name ], typeof( T ) );
        static ESPBlocker instance;
        static int PlayerLayer = LayerMask.NameToLayer("Player (Server)");
        
        Dictionary<BasePlayer, ESPPlayer> players = new Dictionary<BasePlayer, ESPPlayer>();
             Dictionary<uint, List<ulong>> whitelists;
        
        void Loaded()
        {
            instance = this;
            LoadData();
        }

        bool init = false;

        int raycastCount = 0;
        void OnServerInitialized()
        {
            LoadDefaultConfig();
			
            CommunityEntity.ServerInstance.StartCoroutine(InitCore());
        }

        void Unload()
        {
            SaveData();
            foreach (var p in players)
                UnityEngine.Object.Destroy(p.Value);
        }
        

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity?.net?.ID == null || !init) return;
            var box = entity as BoxStorage;
            if (box == null) return;
            whitelists.Remove(box.net.ID);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (players.ContainsKey(player))
            {
                UnityEngine.Object.Destroy(players[player]);
                players.Remove(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            AddEspPlayer(player);
        }
        
        object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            if (!init) return null;
            BaseEntity baseEntity = entity as BaseEntity;
            if (baseEntity == null || !IsBlockEntity(baseEntity)) return null;
            if (baseEntity.net?.ID == null) return null;
            if (adminIgnore && target.IsAdmin) return null;
            List<ulong> whitelistPlayers;
            if (whitelists.TryGetValue(baseEntity.net.ID, out whitelistPlayers))
                return whitelistPlayers.Contains(target.userID);
            else if (baseEntity.OwnerID == target.userID)
            {
                SetVisibleEntity(baseEntity,target.userID);
                return true;
            }
            return false;
        }

        bool IsBlockEntity(BaseEntity entity)
        {
            return entity.ShortPrefabName == "shelves" || entity is BoxStorage || entity is BuildingPrivlidge || (entity is BaseOven && entity.ShortPrefabName == "furnace") || (entity is BasePlayer && ((BasePlayer)entity).IsSleeping()) || entity is SleepingBag;
        }


        void SetVisibleEntity(BaseEntity entity, ulong userID)
        {
            var whitelist = instance.whitelists[entity.net.ID] = new List<ulong>();
            if (!whitelist.Contains(userID))
                whitelist.Add(userID);
            if (instance.clansSupport)
            {
                var members = instance.GetClanMembers(userID);
                if (members != null && members.Count > 0)
                {
                    foreach (var member in members)
                        if (!whitelist.Contains(member))
                            whitelist.Add(member);
                }
            }
        }

        [ChatCommand("clear")]
        void cmdClear(BasePlayer player)
        {
            if (player.IsAdmin)
                UnityEngine.Object.FindObjectOfType<BoxStorage>().SendNetworkUpdate();
        }
 
        

        ESPPlayer GetEspPlayer(BasePlayer player)
        {
            ESPPlayer espPlayer;
            if (players.TryGetValue(player, out espPlayer))
                return espPlayer;
            AddEspPlayer(player);
            return GetEspPlayer(player);
        }

        void AddEspPlayer(BasePlayer player)
        {
            if (!players.ContainsKey(player))
            players.Add(player, player.gameObject.AddComponent<ESPPlayer>());
        }

        void DestroyClientEntity(BasePlayer player, BaseEntity entity)
        {
            if (Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Message.Type.EntityDestroy);
                Net.sv.write.EntityID(entity.net.ID);
                Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                Net.sv.write.Send(new SendInfo(player.net.connection));
            }   
        }

        IEnumerator InitCore()
        {
            var objs = UnityEngine.Object.FindObjectsOfType<BoxStorage>();
            int i = 0;
            int lastpercent = -1;
            StopwatchUtils.StopwatchStart("ESPBlocker.InitCore");
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                i++;
                int percent = (int) (i/(float)BasePlayer.activePlayerList.Count*100);
                if (StopwatchUtils.StopwatchElapsedMilliseconds("ESPBlocker.InitCore") > 10 || percent != lastpercent)
                {
                    StopwatchUtils.StopwatchStart("ESPBlocker.InitCore");
                    if (percent != lastpercent)
                    {
                        if (percent%20 == 0)
                        {
                            Puts($"Идёт загрузка ESPPlayer: {percent}%");
                        }
                        lastpercent = percent;
                    }
                    if (Performance.report.frameTime < 100)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
                AddEspPlayer(player);
            }
            init = true;
        }
        public static class StopwatchUtils
        {
            static Dictionary<string, Stopwatch> watches = new Dictionary<string, Stopwatch>();

            /// <summary>
            /// Start Stopwatch
            /// </summary>
            /// <param name="name">KEY</param>
            public static void StopwatchStart( string name )
            {
                watches[ name ] = Stopwatch.StartNew();
            }

            /// <summary>
            /// Get Elapsed Milliseconds
            /// </summary>
            /// <param name="name">KEY</param>
            /// <returns></returns>
            public static long StopwatchElapsedMilliseconds( string name ) => watches[ name ].ElapsedMilliseconds;

            /// <summary>
            /// Remove StopWatch
            /// </summary>
            /// <param name="name"></param>
            public static void StopwatchStop( string name )
            {
                watches.Remove( name );
            }
        }
        public void Arrow(BasePlayer player, Vector3 from, Vector3 to)
        {
            player.SendConsoleCommand("ddraw.arrow", 5, Color.magenta, from, to, 0.1f);
        }
        [PluginReference]
        Plugin Clans;

        List<ulong> GetClanMembers(ulong uid)
        {
            return Clans?.Call("GetClanMembers", uid) as List<ulong>;
        }

        DynamicConfigFile whitelistFile = Interface.Oxide.DataFileSystem.GetFile("ESPBlockerWhitelist");

        void OnServerSave()
        {
            if (!init) return;
            SaveData();
        }

        void LoadData()
        {
            whitelists = whitelistFile.ReadObject<Dictionary<string, List<ulong>>>().ToDictionary(p=>uint.Parse(p.Key), p=>p.Value);
        }

        void SaveData()
        {
            Dictionary<string, List<ulong>> data = whitelists.ToDictionary(p => p.Key.ToString(), p => p.Value);
            whitelistFile.WriteObject(data);
        }
    }
}
