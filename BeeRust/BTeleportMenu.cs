using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("BTeleportMenu", "King", "1.0.0")]
	public class BTeleportMenu : RustPlugin
	{
        #region [Vars]
        [PluginReference] Plugin Clans, NTeleportation, Teleportation;
        public string Layer = "BTeleportMenu.Layer";
        #endregion

        #region [Data]
        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>("BTeleportMenu");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (data == null) data = new PluginData();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("BTeleportMenu", data);

        private class PluginData
        {
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
        }
    
        private static PluginData data;

        private class PlayerData
        {
            public bool AutoAccept;

            public static PlayerData GetOrAdd(BasePlayer player)
            {
                return GetOrAdd(player.userID);
            }

            public static PlayerData GetOrAdd(ulong userId)
            {
                if (!data.PlayerData.ContainsKey(userId))
                    data.PlayerData.Add(userId, new PlayerData
                    {
                        AutoAccept = true,
                    });

                return data.PlayerData[userId];
            }
        }
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            cmd.AddChatCommand("tpmenu", this, "TeleportUi");
            cmd.AddChatCommand("atp", this, "cmdAutoTeleport");
            LoadData();
        }
        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) 
                CuiHelper.DestroyUi(player, Layer);
            SaveData();
            data = null;
        }
        #endregion

        #region [AutoTeleport]
        private void CheckTeleport(BasePlayer receiver, BasePlayer caller)
        {
            var data = PlayerData.GetOrAdd(receiver);
            if(data.AutoAccept)
            {
                if (IsClanMember(caller.userID, receiver.userID))
                {
                    receiver.SendConsoleCommand("chat.say /tpa");
                    return;
                }
            }
        }
        private void OnTeleportRequested(BasePlayer receiver, BasePlayer caller) => CheckTeleport(receiver, caller);
        private void cmdAutoTeleport(BasePlayer player)
        {
            var data = PlayerData.GetOrAdd(player);
            if (data.AutoAccept)
            {
                data.AutoAccept = false;
                player.ChatMessage($"Вы включили автоматическое <color=#AAFF81FF>/tpa</color>");

            }
            else
            {
                data.AutoAccept = true;
                player.ChatMessage($"Вы отключили автоматическое <color=#AAFF81FF>/tpa</color>");
            }
            return;
        }
        #endregion

        #region [Main-Ui]
        private void TeleportUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.85" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.2", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            CuiHelper.AddUi(player, container);
            TeleportUiMain(player);
        }
        private void TeleportUiMain(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiElementContainer container = new CuiElementContainer();
            var data = PlayerData.GetOrAdd(player);

            container.Add(new CuiElement
            {
                Name = Layer + ".Main",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = "0.36 0.34 0.32 0.45", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-345 -170", OffsetMax = "347 177"}
                }
            });

            #region [Text]
            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 0.85", Text = "МЕНЮ ТЕЛЕПОРТАЦИИ", FontSize = 28, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-345 177", OffsetMax = "345 227.5"},
                    new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.05 0.05"},
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components = {
                    new CuiTextComponent() { Color = "1 1 1 0.85", Text = "ТЕЛЕПОРТАЦИЯ К ДРУГУ", FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0 0.4745", AnchorMax = "1 0.5625" },
                    new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "0.01 0.01" },
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components = {
                    new CuiTextComponent() { Color = "1 1 1 0.85", Text = "ТЕЛЕПОРТАЦИЯ ДОМОЙ", FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0 0.2435", AnchorMax = "1 0.3325" },
                    new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "0.01 0.01" },
                }
            });
            #endregion

            #region [Buttons]
            var setHome = GetGridString(player.transform.position);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1965 0.8525", AnchorMax = "0.49 0.965" },
                Button = { Close = Layer, Command = $"chat.say /outpost", Color = "0 0 0 0.85" },
                Text = { Text = $"Город NPC", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.507 0.8525", AnchorMax = "0.8025 0.965" },
                Button = { Close = Layer, Command = $"chat.say /outpost", Color = "0 0 0 0.85" },
                Text = { Text = $"Город Бандитов", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1965 0.707", AnchorMax = "0.49 0.8185" },
                Button = { Close = Layer, Command = $"BTeleport_UI /sethome {setHome}", Color = "0.81 0.51 0.18 0.85" },
                Text = { Text = $"Сохранить Дом", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.507 0.707", AnchorMax = "0.8025 0.8185" },
                Button = { Close = Layer, Command = $"chat.say /atp", Color = data.AutoAccept == true ? "0.44 0.50 0.29 0.85" : "0.54 0.22 0.18 0.85" },
                Text = { Text = $"Автопринятия ТП", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1965 0.5625", AnchorMax = "0.49 0.67515" },
                Button = { Close = Layer, Command = $"chat.say /tpa", Color = "0.44 0.50 0.29 0.85" },
                Text = { Text = $"Принять ТП", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.507 0.5625", AnchorMax = "0.8025 0.67515" },
                Button = { Close = Layer, Command = $"chat.say /tpc", Color = "0.54 0.22 0.18 0.85" },
                Text = { Text = $"Отклонить ТП", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");
            #endregion

            #region [Team]
            container.Add(new CuiElement
            {
                Name = Layer + ".Main" + ".TeamLayer",
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiImageComponent { Color =  "0.2 0.2 0.2 0.45", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent {AnchorMin = "0.01446 0.3325", AnchorMax = "0.9825 0.4745"}
                }
            });
 
            string clanTag = GetClan(player.userID);
            if (!string.IsNullOrEmpty(clanTag))
            {
                var clanMembers = GetClanMembers(player.userID);
                if (clanMembers != null && clanMembers.Count > 0)
                {
                    foreach (var check in clanMembers.Select((i, t) => new { A = i, B = t }))
                    {
                        var clanMember = covalence.Players.FindPlayerById(check.A.ToString());
                        if (clanMember == null) continue;
                        
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{0.015 + check.B * 0.247 - Math.Floor((float) check.B / 4) * 5 * 0.247} {0.2 - Math.Floor((float) check.B/ 4) * 0}",
                                              AnchorMax = $"{0.245 + check.B * 0.247 - Math.Floor((float) check.B / 4) * 5 * 0.247} {0.78 - Math.Floor((float) check.B / 4) * 0}", },
                            Button = { Color = "0.52 0.47 0.48 0.75", Command = $"BTeleport_UI /tpr {clanMember.Id}", Close = Layer },
                            Text = { Text = $"{clanMember.Name}", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7", Font = "robotocondensed-bold.ttf" }
                        }, Layer + ".Main" + ".TeamLayer",  Layer + ".Main" + ".TeamLayer" + $".{check.B}");
                    }
                }
            }
            #endregion

            #region [Home]
            var homeName = GetHomes(player);
            if (homeName.ContainsKey(setHome))
            {
                var count = homeName.Where(p => p.Key.Contains(setHome)).Count();
                setHome = setHome + $"({count})";
            }

            container.Add(new CuiElement
            {
                Name = Layer + ".Main" + ".HomeLayer",
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiImageComponent { Color =  "0.2 0.2 0.2 0.45", Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent {AnchorMin = "0.01446 0.02786", AnchorMax = "0.9825 0.2435"}
                }
            });

            if (homeName != null && homeName.Count > 0)
            {
                foreach (var check in homeName.Select((i, t) => new { A = i, B = t }).Take(10))
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{0.015 + check.B * 0.0985 - Math.Floor((float) check.B / 10) * 10 * 0.0985} {0.13 - Math.Floor((float) check.B/ 10) * 0.215}",
                                          AnchorMax = $"{0.097 + check.B * 0.0985 - Math.Floor((float) check.B / 10) * 10 * 0.0985} {0.85 - Math.Floor((float) check.B / 10) * 0.215}", },
                        Button = { Color = "0.52 0.47 0.48 0.75", Command = $"BTeleport_UI /home {check.A.Key}", Close = Layer },
                        Text = { Text = $"{check.A.Key}", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7", Font = "robotocondensed-bold.ttf" }
                    }, Layer + ".Main" + ".HomeLayer",  Layer + ".Main" + ".HomeLayer" + $".{check.B}");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.8 0.8", AnchorMax = "1 1" },
                        Button = { Command = $"BTeleport_UI /home remove {check.A.Key}", Color = "1.00 0.54 0.54 1.00", Sprite = "assets/icons/close.png", Close = Layer },
                        Text = { Text = "" }
                    }, Layer + ".Main" + ".HomeLayer" + $".{check.B}");
                }
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [Func]
        [ConsoleCommand("BTeleport_UI")]
        private void cmdSendCommand(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (args.FullString.Contains("/"))
                player.Command("chat.say", args.FullString);
            else
                player.Command(args.FullString);
        }
        private string GetGridString(Vector3 position)
        {
            Vector2 adjPosition = new Vector2((World.Size / 2) + position.x, (World.Size / 2) - position.z);
            return $"{NumberToString((int)(adjPosition.x / 150))}{(int)(adjPosition.y / 150)}";
        }
        private string NumberToString(int number)
        {
            bool a = number > 26;
            Char c = (Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }
        Dictionary<string, Vector3> GetHomes(BasePlayer player)
        {
            var a1 = (Dictionary<string, Vector3>)NTeleportation?.Call("API_GetHomes", player) ?? new Dictionary<string, Vector3>();
            var a2 = (Dictionary<string, Vector3>)Teleportation?.Call("GetHomes", player.userID) ?? new Dictionary<string, Vector3>();
            return a1.Concat(a2).GroupBy(p => p.Key).ToDictionary(p => p.Key, p => p.First().Value);
        }
        private string GetClan(ulong userID) => Clans?.Call<string>("GetClanTag", userID);
        bool IsClanMember(ulong playerid = 1, ulong targetID = 0) => (bool)(Clans?.Call("IsTeammates", playerid, targetID) ?? false);
        private List<ulong> GetClanMembers(ulong playerId)
        {
            if (Clans)
            {
                var clan = Clans?.Call("BTeleportMenuHook", playerId) as List<ulong>;
                return clan.ToList();
            }
            return new List<ulong>();
        }
        #endregion
    }
}