using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using ProtoBuf;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("Friendsbyfermens", "fermens", "0.1.42")]
    [Description("Система друзей и FF")]
    class Friendsbyfermens : RustPlugin
    {
        #region Config
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Максимальное количество игроков в команде")]
            public int maxcount;

            [JsonProperty("Префикс")]
            public string prefix;

            [JsonProperty("Максимальное количество символов в теге команды")]
            public int max;

            [JsonProperty("Минимальное количество символов в теге команды")]
            public int min;

            [JsonProperty("Запрещенные названия в теге команде")]
            public string[] blacklist;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    maxcount = 3,
                    prefix = "<color=#ff8000>KualaFriends™</color> ",
                    min = 2,
                    max = 4,
                    blacklist = new string[] { "MOD", "MODR", "ADMI", "ADM" },

                };
            }
        }
        #endregion
        [PluginReference] private Plugin OneVSOne, Battles;
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (entity is BasePlayer && info.Initiator is BasePlayer)
            {
                BasePlayer takeDamage = entity.ToPlayer();
                if (takeDamage == null) return;
                BasePlayer initiator = info.InitiatorPlayer;
                if (initiator == null || initiator == takeDamage || takeDamage.currentTeam == 0) return;
                settings one;
                if (takeDamage.currentTeam == initiator.currentTeam && setplayers.TryGetValue(initiator.userID, out one) && one.ff)
                {
                    if (OneVSOne != null && OneVSOne.Call<bool>("IsEventPlayer", takeDamage) || Battles != null && Battles.Call<bool>("IsPlayerOnBattle", takeDamage.userID)) return;
                    clear(info);
                }
            }
        }

        void clear(HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.HitEntity = null;
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }

        Dictionary<SamSite, BuildingPrivlidge> samsites = new Dictionary<SamSite, BuildingPrivlidge>();
        private object OnSamSiteTarget(SamSite samSite, BaseCombatEntity target)
        {
            if (target is BaseMountable || target is HotAirBalloon)
            {
                List<BasePlayer> players = new List<BasePlayer>();
                Vis.Entities(target.transform.position, 2.5f, players);
                if (players == null || players.Count == 0) return null;
                BuildingPrivlidge buildingPrivlidge;
                if (!samsites.TryGetValue(samSite, out buildingPrivlidge) || buildingPrivlidge.IsDestroyed)
                {
                    buildingPrivlidge = samSite.GetBuildingPrivilege();
                    if (buildingPrivlidge == null) return null;
                    samsites[samSite] = buildingPrivlidge;
                }

                if (players.Any(player => buildingPrivlidge.IsAuthed(player))) return false;
            }

            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (entity == null) return null;
            BasePlayer player = entity.ToPlayer();
            if (player == null) return null;
            if (turret.OwnerID == 0) return null;
            if (player.Team != null)
            {
                if(player.Team.members.Any(x=> turret.authorizedPlayers.Any(z => x == z.userid && setplayers.ContainsKey(x) && setplayers[x].turret)))
                {
                    turret.authorizedPlayers.Add(GetPlayerNameId(player));
                    turret.SendNetworkUpdate();
                    return false;
                }
            }
            return null;
        }

        private static PlayerNameID GetPlayerNameId(BasePlayer player)
        {
            var playerNameId = new PlayerNameID()
            {
                userid = player.userID,
                username = player.displayName
            };
            return playerNameId;
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (player == null || baseLock == null || baseLock.GetEntity() == null || !baseLock.IsLocked()) return null;
            ulong ownerID = baseLock.GetEntity().OwnerID;
            if (ownerID.Equals(0)) return null;
            if (player.Team != null && player.Team.members.Contains(ownerID) && setplayers.ContainsKey(ownerID) && setplayers[ownerID].codelock)
            {
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", baseLock.transform.position);
                return true;
            }
            return null;
        }

        private void Save()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Friendsbyfermens", setplayers);
        }

        private void Unload()
        {
         /*   teams.Clear();
            foreach (var z in RelationshipManager.Instance.teams)
            {
                PLAYERTEAM pLAYERTEAM;
                if (!teams.TryGetValue(z.Key, out pLAYERTEAM))
                {
                    teams.Add(z.Key, new PLAYERTEAM { members = new List<ulong>() });
                    pLAYERTEAM = teams[z.Key];
                }
                pLAYERTEAM.teamLeader = z.Value.teamLeader;
                pLAYERTEAM.teamName = z.Value.teamName;
                pLAYERTEAM.members.AddRange(z.Value.members);
            }
            if(teams.Count > 0) Interface.Oxide.DataFileSystem.WriteObject("teams", teams);*/
            Save();
        }

        private void initializeset(ulong id)
        {
            if (!setplayers.ContainsKey(id)) setplayers.Add(id, new settings());
        }

        private static Dictionary<ulong, PLAYERTEAM> teams = new Dictionary<ulong, PLAYERTEAM>();

        class PLAYERTEAM
        {
            public string teamName;
            public ulong teamLeader;
            public List<ulong> members;
        }

        private void OnServerInitialized()
        {
          /*  Debug.LogError("Конец халявы, ищи альтернативу!");
            Server.Command("o.unload Friendsbyfermens");
            return;
            */
            if(config.blacklist == null)
            {
                config.blacklist = new string[] { "MOD", "MODR", "ADMI", "ADM" };
                config.max = 4;
                config.min = 2;
                SaveConfig();
            }
            setplayers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, settings>>("Friendsbyfermens");
            foreach (var z in BasePlayer.activePlayerList) initializeset(z.userID);
            RelationshipManager.maxTeamSize = config.maxcount;
            var com = Interface.Oxide.GetLibrary<ru.Libraries.Command>(null);
            com.AddChatCommand("friend", this, "COMMANDER");
            com.AddChatCommand("team", this, "COMMANDER");
            com.AddConsoleCommand("friend", this, "CmdConsolecommandinvite");
            com.AddConsoleCommand("team", this, "CmdConsolecommandinvite");
            
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!player.IsConnected) return;

            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }

            initializeset(player.userID);
        }

        Dictionary<ulong, settings> setplayers = new Dictionary<ulong, settings>();
        class settings
        {
            public bool ff = true;
            public bool turret = true;
            public bool codelock = true;
        }

        void CmdConsolecommandinvite(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs()) return;
            COMMANDER(player, "friend", arg.Args);
        }

        private void COMMANDER(BasePlayer player, string command, string[] args)
        {
            if (string.IsNullOrEmpty(command)) command = "friend";
            settings settings;
            if (!setplayers.TryGetValue(player.userID, out settings))
            {
                setplayers.Add(player.userID, new settings());
                settings = setplayers[player.userID];
            }
            if (args != null && args.Length > 0)
            {
                if(args.Length == 2)
                {
                    if (args[0] == "invite")
                    {
                        if (player.Team == null)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "Создайте сначала команду!");
                            return;
                        }
                        if (!player.Team.teamLeader.Equals(player.userID))
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "Только лидер команды может приглашать в команду!");
                            return;
                        }
                        var listplayers = BasePlayer.activePlayerList.Where(z => z.displayName.Contains(args[1]));
                        int countfiend = listplayers.Count();
                        if (countfiend == 1)
                        {
                            BasePlayer friend = listplayers.FirstOrDefault();
                            if (friend.Team != null)
                            {
                                player.Command("chat.add", 2, 0, config.prefix + $"Игрок <color=#ff8000>'{friend.displayName}'</color> уже состоит в другой команде.");
                                return;
                            }
                            player.Team.SendInvite(friend);
                            player.Command("chat.add", 2, 0, config.prefix + $"Игроку <color=#ff8000>'{friend.displayName}'</color> отправлено приглашение в команду.");
                        }
                        else if (countfiend.Equals(0))
                        {
                            player.Command("chat.add", 2, 0, config.prefix + $"Игрок <color=#ff8000>'{args[1]}'</color> не найден.");
                            return;
                        }
                        else
                        {
                            player.Command("chat.add", 2, 0, config.prefix + $"Найдено несколько игроков: <color=#ff8000>{string.Join(" ", listplayers.Select(p => p.displayName).ToArray())}</color>");
                            return;
                        }
                    }
                    else if(args[0] == "name")
                    {
                        if (player.Team == null)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "У вас нет команды!");
                            return;
                        }

                        if (player.Team.teamLeader != player.userID)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "Только лидер команды может менять тим <color=#ff8000>ТЕГ</color>");
                            return;
                        }

                        if (args[1].Length < config.min || args[1].Length > config.max)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + $"Название должно состоять от 2-х до 4-х символов!\nПример: /{command} name Kuala");
                            return;
                        }
                        string name = args[1].ToUpper();
                        if (config.blacklist.Contains(name) || RelationshipManager.Instance.teams.Any(x => !string.IsNullOrEmpty(x.Value.teamName) && x.Value.teamName == name))
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "Этот тим <color=#ff8000>ТЕГ</color> уже занят!");
                            return;
                        }

                        player.Team.teamName = name;
                        List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => player.Team.members.Contains(x.userid)).ToList();
                        string text = config.prefix + $"Игрок <color=#ff8000>{player.displayName}</color> изменил тим <color=#ff8000>ТЕГ</color> на <color=#32C8C8>{name}</color>";
                        ConsoleNetwork.SendClientCommand(sendto, "chat.add", 0, player.UserIDString, text);
                    }
                }
                else if(args.Length == 1)
                {
                    if(args[0] == "create")
                    {
                        if (player.Team != null)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "У вас уже есть команда!");
                            return;
                        }
                        player.Command("relationshipmanager.trycreateteam");
                    }
                    else if(args[0] == "ff")
                    {
                        if (player.Team == null)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "У вас нет команды!");
                            return;
                        }
                        /*  if (!player.Team.teamLeader.Equals(player.userID))
                          {
                              player.Command("chat.add", 2, 0, config.prefix + "Только лидер команды может менять этот параметр!");
                              return;
                          }*/
                        if (settings.ff)
                        {
                            settings.ff = false;
                            foreach (var z in player.Team.members)
                            {
                                BasePlayer gg = BasePlayer.FindByID(z);
                                if (player == null) continue;
                                gg.Command("chat.add", 2, 0, config.prefix + $"Игрок <color=#ff8000>{player.displayName}</color> включил урон по тиммейтам!");
                            }
                        }
                        else
                        {
                            settings.ff = true;
                            List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => player.Team.members.Contains(x.userid)).ToList();
                            string text = config.prefix + $"Игрок <color=#ff8000>{player.displayName}</color> выключил урон по тиммейтам.";
                            ConsoleNetwork.SendClientCommand(sendto, "chat.add", 0, player.UserIDString, text);
                        }
                    }
                    else if (args[0] == "codelock")
                    {
                        if (player.Team == null)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "У вас нет команды!");
                            return;
                        }
                        if (settings.codelock)
                        {
                            settings.codelock = false;
                            List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => player.Team.members.Contains(x.userid)).ToList();
                            string text = config.prefix + $"Игрок <color=#ff8000>{player.displayName}</color> запретил пользоваться его замками!";
                            ConsoleNetwork.SendClientCommand(sendto, "chat.add", 0, player.UserIDString, text);
                        }
                        else
                        {
                            settings.codelock = true;
                            List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => player.Team.members.Contains(x.userid)).ToList();
                            string text = config.prefix + $"Игрок <color=#ff8000>{player.displayName}</color> разрешил пользоваться его замками.";
                            ConsoleNetwork.SendClientCommand(sendto, "chat.add", 0, player.UserIDString, text);
                        }
                    }
                    else if (args[0] == "turret")
                    {
                        if (player.Team == null)
                        {
                            player.Command("chat.add", 2, 0, config.prefix + "У вас нет команды!");
                            return;
                        }
                        if (settings.turret)
                        {
                            settings.turret = false;
                            List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => player.Team.members.Contains(x.userid)).ToList();
                            string text = config.prefix + $"Игрок <color=#ff8000>{player.displayName}</color> отключил автоматическую авторизацию в его турелях!";
                            ConsoleNetwork.SendClientCommand(sendto, "chat.add", 0, player.UserIDString, text);
                        }
                        else
                        {
                            settings.turret = true;
                            List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => player.Team.members.Contains(x.userid)).ToList();
                            string text = config.prefix + $"Теперь туррели игрока <color=#ff8000>{player.displayName}</color> для вас безобидны.";
                            ConsoleNetwork.SendClientCommand(sendto, "chat.add", 0, player.UserIDString, text);
                        }
                    }
                }
                
            }
            else
            {
                if (player.Team == null) player.Command("chat.add", 2, 0, config.prefix + $"Создайте сначала команду!\n             <color=#ff8000>/{command} create</color> - создать команду.");
                else player.Command("chat.add", 2, 0, config.prefix + $"\n\n    <color=#FF0077><3</color>     <color=#ff8000>/{command} name</color> <color=#32C8C8>ТЕГ</color> - изменить тим <color=#32C8C8>ТЕГ</color>.\n    <color=#FF0077><3</color>     <color=#ff8000>/{command} invite</color> <color=#32C8C8>никнейм</color> - пригласить в команду \n {onoff(settings.codelock)} <color=#ff8000>/{command} codelock</color> - разрешить тиммейтам пользоваться вашими замками.\n {onoff(setplayers[player.userID].turret)} <color=#ff8000>/{command} turret</color> - авторизовывать тиммейтов в ваших туреллях, когда они на них агряться.\n {onoff(setplayers[player.userID].ff)} <color=#ff8000>/{command} ff</color> - выключить урон по тиммейтам.");
            }
        }

        private bool IsPvp(ulong id)
        {
            settings settings;
            if (!setplayers.TryGetValue(id, out settings)) return true;
            return settings.ff;
        }

        private bool IsTurret(ulong id)
        {
            settings settings;
            if (!setplayers.TryGetValue(id, out settings)) return true;
            return settings.turret;
        }

        private bool IsCodelock(ulong id)
        {
            settings settings;
            if (!setplayers.TryGetValue(id, out settings)) return true;
            return settings.codelock;
        }

        string onoff(bool on)
        {
            if (on) return "<color=#29F500> - On -</color>";
            else return "<color=#FF0B00>- Off -</color>";
        }
    }
}