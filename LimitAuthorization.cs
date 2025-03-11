using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mono.Security.X509;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LimitAuthorization", "Own3r/Nericai", "1.1.3")]
    [Description(
        "Ограничивает лимит авторизаций и позволяет авторизовать в замках, шкафах, турелях,")]
    class LimitAuthorization : RustPlugin
    {
        #region Поля

        [PluginReference] private Plugin Clans, Friends, FriendSystem, ImageLibrary;

        private List<AuthManager> authManager = new List<AuthManager>
        {
            new AuthManager("https://rustlabs.com/img/items180/lock.code.png", "authCode"),
            new AuthManager("https://rustlabs.com/img/items180/autoturret.png", "authTurret"),
            new AuthManager("https://rustlabs.com/img/items180/cupboard.tool.png", "authCups")
        };

        private class AuthManager
        {
            public string PictureURL;
            public string Command;

            public AuthManager(string pictureUrl, string command)
            {
                PictureURL = pictureUrl;
                Command = command;
            }
        }

        #endregion

        #region Конфигурация

        private class AuthConfig
        {
            [JsonProperty("Лимит авторизаций в шкафу")]
            public int CupboardLimit;

            [JsonProperty("Лимит авторизаций в замках")]
            public int CodelockLimit;

            [JsonProperty("Лимит авторизаций в турелях")]
            public int TurretLimit;

            [JsonProperty("Разрешить авторизацию кланам")]
            public bool ClanAllow;

            [JsonProperty("Разрешить авторизацию друзьям")]
            public bool FriendsAllow;
        }

        private AuthConfig config;

        protected override void LoadDefaultConfig()
        {
            config = new AuthConfig
            {
                CodelockLimit = 3,
                CupboardLimit = 3,
                TurretLimit = 3,
                ClanAllow = true,
                FriendsAllow = true
            };
            SaveConfig();
            PrintWarning("Создаем дефолтный конфиг");
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<AuthConfig>();
        }

        #endregion

        #region Хуки Oxide

        private void OnServerInitialized()
        {
            if (!plugins.Exists("Clans"))
            {
                if (config.ClanAllow) PrintWarning("Плагин Clans не установлен.\nАвторизация сокланов отключена!");
                config.ClanAllow = false;
            }

            if (!plugins.Exists("Friends") && !plugins.Exists("FriendSystem"))
            {
                if (config.FriendsAllow) PrintWarning("Плагин Friends не установлен.\nАвторизация друзей отключена!");
                config.FriendsAllow = false;
            }

            foreach (var check in authManager.Select((i, t) => new
                {
                    row = i,
                    index = t
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.row.PictureURL)))
                ImageLibrary?.Call("AddImage", check.row.PictureURL, $"Auth.{check.index}");

            timer.Every(5f, StartClanUpdate);
        }

        private Coroutine clanListUpdater;
        private Coroutine friendListUpdater;

        public void StartClanUpdate()
        {
            if (clanListUpdater != null) return;
            if (config.ClanAllow)
                clanListUpdater = ServerMgr.Instance.StartCoroutine(UpdateMemebers(GetClanMembers, currentClanMembers,
                    () => clanListUpdater = null));

            if (friendListUpdater != null) return;
            if (config.FriendsAllow)
                friendListUpdater =
                    ServerMgr.Instance.StartCoroutine(UpdateMemebers(GetFriends, currentFriends,
                        () => friendListUpdater = null));
        }

        public IEnumerator UpdateMemebers(Func<BasePlayer, List<string>> cb,
            Dictionary<ulong, List<string>> membersList, Action finalize)
        {
            var players = BasePlayer.activePlayerList.ToArray();

            foreach (var player in players.ToList())
            {
                var newList = cb(player);
                if (newList == null) yield return null;

                if (!membersList.ContainsKey(player.userID))
                {
                    membersList.Add(player.userID, new List<string>());
                    membersList[player.userID] = newList;
                    yield return null;
                }

                if (newList != null && membersList[player.userID] != null)
                {
                    var deletedList = membersList[player.userID].Except(newList).ToList();
                    DeAuthAll(player, deletedList);
                }

                membersList[player.userID] = newList;
                yield return null;
            }

            finalize();
            yield return null;
        }

        Dictionary<ulong, List<string>> currentClanMembers = new Dictionary<ulong, List<string>>();
        Dictionary<ulong, List<string>> currentFriends = new Dictionary<ulong, List<string>>();

        void Unload()
        {
            if (clanListUpdater != null) ServerMgr.Instance.StopCoroutine(clanListUpdater);
            if (friendListUpdater != null) ServerMgr.Instance.StopCoroutine(friendListUpdater);
        }

        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null) return null;

            var count = privilege.authorizedPlayers.Count;
            if (count == 0) return null;

            if (count > config.CupboardLimit)
            {
                privilege.authorizedPlayers.RemoveRange(0, count - 1);
            }

            return null;
        }

        object OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
        {
            if (codeLock == null || player == null) return null;

            var count = codeLock.whitelistPlayers.Count + codeLock.guestPlayers.Count;
            if (count == 0) return null;

            if (!(code == codeLock.guestCode || code == codeLock.code)) return null;

            if (count > config.CodelockLimit)
            {
                codeLock.whitelistPlayers.RemoveRange(0, count - 1);
            }

            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity targ)
        {
            if (!(targ is BasePlayer) || turret.OwnerID < 0) return null;
            var player = (BasePlayer) targ;
            if (turret == null || player == null) return null;
            var count1 = turret.authorizedPlayers.Count;
            if (count1 == 0) return null;
            if (count1 > config.TurretLimit)
            {
                turret.authorizedPlayers.RemoveRange(0, count1 - 1);
            }

            return null;
        }

        /*object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
            if (turret == null || player == null) return null;

            var count = turret.authorizedPlayers.Count;
            if (count == 0) return null;

            if (count > config.TurretLimit)
            {
                turret.authorizedPlayers.RemoveRange(0, count - config.TurretLimit);
            }

            return null;
        }*/

        #endregion

        #region Логика

        class ClanUModGetClanResult
        {
            public string tag;
            public string owner;
            public JArray moderators;
            public JArray members;
            public JArray invited;
            public JArray allies;
            public JArray invitedallies;
        }

        public List<string> GetClanMembersUmod(BasePlayer player)
        {
            var clanTag = Clans.Call<string>("GetClanOf", player.userID);
            if (clanTag == null) return null;

            var resultRaw = Clans.Call<JObject>("GetClan", clanTag);
            if (resultRaw == null) return null;

            var result = JsonConvert.DeserializeObject<ClanUModGetClanResult>(resultRaw.ToString());
            if (result == null) return null;

            var members = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.members.ToString());
            return members != null ? new List<string>(members.Keys) : null;
        }

        public List<string> GetClanMembersOvh(BasePlayer player)
        {
            var result = Clans.Call<List<ulong>>("ApiGetClanMembers", player.userID);

            return result?.ConvertAll(i => i.ToString());
        }

        public List<string> GetFriendsOvh(BasePlayer player)
        {
            var result = (List<ulong>) Friends.CallHook("ApiGetFriends", player.userID);

            return result?.ConvertAll(i => i.ToString());
        }
		
		public List<string> GetFriendsOxide(BasePlayer player)
        {
            var result = (string[]) Friends.CallHook("GetFriends", player.userID);

            return result?.ToList();
        }
		
		public List<string> GetFriendsApi(BasePlayer player)
        {
            var result = (string[]) Friends.CallHook("GetFriendsS", player.userID.ToString());

            return result?.ToList();
        }
		
		public List<string> GetFriendsAp(BasePlayer player)
        {
            var result = (string[]) FriendSystem.CallHook("GetFriendsS", player.userID.ToString());

            return result?.ToList();
        }
    

        public List<string> GetFriends(BasePlayer player)
        {
            if (config.FriendsAllow && plugins.Exists("Friends"))
            {
                //Friends by Moscow.OVH
                if (Friends.Author.ToLower().Contains("sanlerus"))
                {
                    return GetFriendsOvh(player);
                }

                //Friends by Dcode
                if (Friends.Author.ToLower().Contains("dcode"))
                {
                    return GetFriendsOxide(player);
                }

                //Friends by Umod
                if (Friends.Author.ToLower().Contains("nogrod"))
                {
                    return GetFriendsApi(player);
                }

                return null;
            }

            if (config.FriendsAllow && plugins.Exists("FriendSystem"))
            {
                //FriendSystem A1M41K
                if (FriendSystem.Author.ToLower().Contains("a1m41k"))
                {
                    return GetFriendsAp(player);
                }

                return null;
            }

            return null;
        }

        public List<string> GetClanMembers(BasePlayer player)
        {
            if (!config.ClanAllow || !plugins.Exists("Clans")) return null;

            //Clans by k1lly0u (uMod/Oxide)
            if (Clans.Author.ToLower().Contains("k1lly0u"))
            {
                return GetClanMembersUmod(player);
            }

            //Clans by Moscow.OVH
            if (Clans.Author.ToLower().Contains("sanlerus"))
            {
                return GetClanMembersOvh(player);
            }

            //Clans reborn by Fujicura
            return Clans.Call<List<string>>("GetClanMembers", player.userID);
        }

        private void AuthOnEntity<T>(BasePlayer player, string where, bool deAuth, Action<T, BasePlayer> callback)
        {
            List<string> clanMembers = null;
            List<string> friends = null;

            if (config.ClanAllow)
            {
                clanMembers = GetClanMembers(player);

                if (clanMembers == null && !config.FriendsAllow)
                {
                    SendReply(player, "Вы не состоите в клане, либо в нем только вы");
                    return;
                }
            }

            if (config.FriendsAllow)
            {
                friends = GetFriends(player);

                if (friends == null && !config.ClanAllow)
                {
                    SendReply(player, "У вас нет друзей");
                    return;
                }
            }

            if (friends == null && clanMembers == null)
            {
                SendReply(player, "У вас нет друзей и вы не состоите в клане (или вы в клане один)");
                return;
            }

            if (friends == null) friends = new List<string>();
            if (clanMembers == null) clanMembers = new List<string>();

            var contragents = clanMembers.Union(friends).ToList();

            var playerEntities = GetPlayerEnitityByType<T>(player);

            foreach (var contragen in contragents)
            {
                var contragentBasePlayer = BasePlayer.Find(contragen);
                if (contragentBasePlayer == null) continue;

                foreach (var entity in playerEntities)
                {
                    callback(entity, contragentBasePlayer);
                }
            }

            var whatSuccess = config.ClanAllow && config.FriendsAllow
                ? "клан и друзей"
                : config.FriendsAllow
                    ? "друзей"
                    : "клан";

            SendReply(player, $"Вы успешно {(deAuth ? "де" : "")}авторизовали {whatSuccess} в {where}");
        }

        private static List<T> GetPlayerEnitityByType<T>(BasePlayer player)
        {
            var entities = UnityEngine.Object.FindObjectsOfType(typeof(T));
            var playerEntities = new List<T>();

            foreach (object entity in entities)
            {
                if (!(entity is BaseEntity)) continue;

                if ((entity as BaseEntity).OwnerID == player.userID)
                {
                    playerEntities.Add((T) entity);
                }
            }

            return playerEntities;
        }

        private static string parseColor(string where, int pos) =>
            (float) byte.Parse(@where.Substring(pos, 2), NumberStyles.HexNumber) / 255 + "";

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFF00";
            if (hex.IndexOf('#') != 0) return hex;

            var str = hex.Trim('#');																																				            var flag = 1622;
            if (str.Length == 3) str = $"{str[0]}0{str[1]}0{str[2]}0";
            if (str.Length == 6) str += "FF";
            if (str.Length == 4) str = $"{str[0]}0{str[1]}0{str[2]}0{str[3]}{str[3]}";
            if (str.Length != 8) throw new Exception(hex);

            return $"{parseColor(str, 0)} {parseColor(str, 2)} {parseColor(str, 4)} {parseColor(str, 6)}";
        }

        #endregion

        #region Команды

        [ChatCommand("auth")]
        private void auth(BasePlayer player)
        {
            if (player == null) return;

            if (!config.ClanAllow && !config.FriendsAllow)
            {
                SendReply(player, "Авторизация недоступна");
                return;
            }

            UI_DrawMainLayer(player);
        }

        private void DeAuthOnEntity<T>(BasePlayer player, List<string> contragents, Action<T, BasePlayer> callback)
        {
            var playerEntities = GetPlayerEnitityByType<T>(player);

            foreach (var contragen in contragents)
            {
                var contragentBasePlayer = BasePlayer.Find(contragen);
                if (contragentBasePlayer == null) continue;

                foreach (var entity in playerEntities)
                {
                    callback(entity, contragentBasePlayer);
                }
            }
        }

        private void DeAuthAll(BasePlayer player, List<string> contragents)
        {
            if (contragents == null || contragents.Count < 1 || player == null) return;

            DeAuthOnEntity<CodeLock>(player, contragents, (where, user) => where.whitelistPlayers.Remove(user.userID));
            DeAuthOnEntity<AutoTurret>(player, contragents, (where, user) =>
            {
                where.authorizedPlayers.RemoveAll(x => x.userid == user.userID);
                where.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            });


            DeAuthOnEntity<BuildingPrivlidge>(player, contragents, (where, user) =>
            {
                where.authorizedPlayers.RemoveAll(x => x.userid == user.userID);
                where.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            });
        }

        [ConsoleCommand("UI_LimitHandler")]
        private void cmdConsoleHandler(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null || !args.HasArgs() && !args.HasArgs(2)) return;

            var deAuth = args.HasArgs(2) && args.Args[1] == "deauth";

            switch (args.Args[0].ToLower())
            {
                case "authcode":
                {
                    AuthOnEntity<CodeLock>(player,
                        "замках",
                        deAuth,
                        (where, user) =>
                        {
                            if (!deAuth)
                                where.whitelistPlayers.Add(user.userID);
                            else
                                where.whitelistPlayers.Remove(user.userID);
                        });
                    break;
                }
                case "authturret":
                {
                    AuthOnEntity<AutoTurret>(player,
                        "турелях",
                        deAuth,
                        (where, user) =>
                        {
                            if (!deAuth)
                                where.authorizedPlayers.Add(new PlayerNameID
                                {
                                    userid = user.userID,
                                    username = user.displayName
                                });
                            else
                                where.authorizedPlayers.RemoveAll(x => x.userid == user.userID);
                            where.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        });
                    break;
                }
                case "authcups":
                {
                    AuthOnEntity<BuildingPrivlidge>(player,
                        "шкафах",
                        deAuth,
                        (where, user) =>
                        {
                            if (!deAuth)
                                where.authorizedPlayers.Add(new PlayerNameID
                                {
                                    userid = user.userID,
                                    username = user.displayName
                                });
                            else
                                where.authorizedPlayers.RemoveAll(x => x.userid == user.userID);
                            where.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        });
                    break;
                }
                default:
                {
                    PrintWarning("Ошибка в ГУИ");
                    return;
                }
            }
        }

        #endregion

        #region UI

        private const string MainLayer = "UI.LimitAuthorization";

        private void UI_DrawMainLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainLayer);
            var container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-211.5 -121.4",
                            OffsetMax = "211.5 121.4"
                        },
                        Image =
                        {
                            Color = "0 0 0 0"
                        }
                    },
                    "Hud", MainLayer
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "-100 -100",
                            AnchorMax = "100 100"
                        },
                        Button =
                        {
                            Color = "0 0 0 0",
                            Close = MainLayer
                        },
                        Text =
                        {
                            Text = ""
                        }
                    },
                    MainLayer
                }
            };

            for (var i = 0; i < 3; i++)
            {
                var currentAuth = authManager[i];

                container.Add(new CuiElement
                {
                    Name = MainLayer + $".{i}",
                    Parent = MainLayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#638964FF")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.07330071 + i * 0.28} 0.04789267",
                            AnchorMax = $"{0.32635 + i * 0.28} 0.7183909"
                        }
                    }
                });

                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.1714286",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = HexToRustFormat("#7c1200FF"),
                            Command = $"UI_LimitHandler {currentAuth.Command} deauth"
                        },
                        Text =
                        {
                            Text = "Деавторизовать",
                            FontSize = 15,
                            Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#c14832FF")
                        }
                    },
                    MainLayer + $".{i}");

                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0.1714286",
                            AnchorMax = "1 0.3428572",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = HexToRustFormat("#3D563EFF"),
                            Command = $"UI_LimitHandler {currentAuth.Command}"
                        },
                        Text =
                        {
                            Text = "Авторизовать",
                            FontSize = 16,
                            Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#638964FF")
                        }
                    },
                    MainLayer + $".{i}");

                if (currentAuth.PictureURL != "")
                {
                    container.Add(new CuiElement
                    {
                        Parent = MainLayer + $".{i}",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage", $"Auth.{i}"),
                                Color = "1 1 1 1"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0.3428572",
                                AnchorMax = "1 1",
                                OffsetMax = "0 0"
                            }
                        }
                    });
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}