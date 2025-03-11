using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins {
    [Info ("ReportVK and AlertCheater", "BROK#", "1.1.1")]
    [Description ("Reports notifications via VK.COM and alert cheater!")]
    class ReportVK : RustPlugin {
        private Dictionary<BasePlayer, DateTime> CooldownsReport = new Dictionary<BasePlayer, DateTime> ();
        private Dictionary<BasePlayer, DateTime> CooldownsSkype = new Dictionary<BasePlayer, DateTime> ();
        private double CooldownReport = 30f;
        private double CooldownSkype = 10f;
        public const string permissionName = "reportvk.admin";
        private Dictionary<string, bool> GUIinfo = new Dictionary<string, bool> ();
        private Dictionary<string, int> adminProtection = new Dictionary<string, int> ();

        private void LoadMessages () {
            lang.RegisterMessages (new Dictionary<string, string> {
                ["player already alert"] = "Игрок уже имеет уведомление!",
                ["player alert"] = "Игрок {0} уведомлен!",
                ["player unalert"] = "С игрока {0} снято уведомление!",
                ["no permissions"] = "У вас нет доступа к этой команде!",
                ["player not found"] = "Игрок не найден или он оффлайн!",
                ["unalert invalid syntax"] = "Неправильный ситаксис! /unalert <НИК / ИД>",
                ["alert invalid syntax"] = "Неправильный ситаксис! /alert <НИК / ИД>",
                ["More than one result"] = "Найдено несколько игроков с данным запросом! Введите ник полностью!",
                ["okay"] = "Ваша жалоба успешно отправлена!",
                ["info"] = "{0} \"сообщение\" - отправить жалобу VK",
                ["okayskype"] = "Ваш skype успешно отправлен!",
                ["infoskype"] = "{0} \"НИК в СКАЙПЕ\" - отправить skype Администратору",
                ["cooldowns"] = "Вы сможете использовать команду через {0:0} минут!",

            }, this);

            lang.RegisterMessages (new Dictionary<string, string> {
                ["player already alert"] = "Игрок уже имеет уведомление!",
                ["player alert"] = "Игрок {0} уведомлен!",
                ["player unalert"] = "C игрока {0} снято уведомление!",
                ["no permissions"] = "У вас нет доступа к этой команде!",
                ["player not found"] = "Игрок не найден или он оффлайн!",
                ["unalert invalid syntax"] = "Неправильный ситаксис! /unalert <НИК / ИД>",
                ["alert invalid syntax"] = "Неправильный ситаксис! /alert <НИК / ИД>",
                ["More than one result"] = "Найдено несколько игроков с данным запросом! Введите ник полностью!",
                ["okay"] = "Ваша жалоба успешно отправлена!",
                ["info"] = "{0} \"сообщение\" - отправить жалобу VK",
                ["okayskype"] = "Ваш skype успешно отправлен!",
                ["infoskype"] = "{0} \"НИК в СКАЙПЕ\" - отправить skype Администратору",
                ["cooldowns"] = "Вы сможете использовать команду через {0:0} минут!",

            }, this, "ru");
        }

        void Loaded () {
            LoadMessages ();
            permission.RegisterPermission (permissionName, this);

        }

        protected override void LoadDefaultConfig () {
            if (Config["token"] == null) Config["token"] = "access_token VK";
            if (Config["userid"] == null) Config["userid"] = "id группы, страницы для жалоб (оставить пустым если заполнено chatid)";
            if (Config["useridskype"] == null) Config["useridskype"] = "id группы, страницы для скайпов (оставить пустым если заполнено chatidskype)";
            if (Config["time"] == null) Config["time"] = "30";
            if (Config["Color"] == null) Config["Color"] = "0 0 0 0.95";
            if (Config["Position"] == null) Config["Position"] = "0 1 1 0.9";
            if (Config["ColorText"] == null) Config["ColorText"] = "1 1 1 1";
            if (Config["DefaultText"] == null) Config["DefaultText"] = "Вы подозреваетесь в использовании читов. Пройдите проверку на наличие читов. \nНапишите свой скайп с помощью команды /skype <НИК в СКАЙПЕ>. \nЕсли вы покините сервер, вы будете забанены на нашем проекте серверов.";
            if (Config["SizeText"] == null) Config["SizeText"] = "20";
            if (Config["nameserver"] == null) Config["nameserver"] = "Название сервера";
            if (Config["chatid"] == null) Config["chatid"] = "id беседы для жалоб (оставить пустым если заполнено userid)";
            if (Config["chatidskype"] == null) Config["chatidskype"] = "id беседы для скайпа (оставить пустым если заполнено useridskype)";

            SaveConfig ();
        }

        void Unload () {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (GUIinfo.ContainsKey (player.UserIDString))
                    if (GUIinfo[player.UserIDString])
                        GUIDestroy (player);
        }

        void OnEntityTakeDamage (BaseCombatEntity entity, HitInfo info) {
            if (!(entity is BasePlayer)) return;
            if (!adminProtection.ContainsKey (entity.ToPlayer ().UserIDString)) return;
            if (!(info.Initiator is BasePlayer)) return;
            BasePlayer targetplayer = info.InitiatorPlayer;
            if (targetplayer.IsConnected) {
                if (GUIinfo.ContainsKey (targetplayer.UserIDString)) return;
                DoGUI (targetplayer, adminProtection[entity.ToPlayer ().UserIDString], false, null);
            }
            return;
        }

        [ChatCommand ("report")]
        void cmdVKChat (BasePlayer player, string cmd, string[] args) {
            if (args.Length == 0) {
                player.ChatMessage (string.Format (msg ("info", player.userID.ToString ()), "/report"));
                return;
            }
            CooldownReport = Convert.ToDouble (Config["time"]);
            if (CooldownsReport.ContainsKey (player)) {
                double minutes = CooldownsReport[player].Subtract (DateTime.Now).TotalMinutes;
                if (minutes >= 0) {
                    player.ChatMessage (string.Format (msg ("cooldowns"), minutes));
                    return;
                }
            }
            CooldownsReport[player] = DateTime.Now.AddMinutes (CooldownReport);
            string allArgs;
            allArgs = Convert.ToString (args[0]);

            foreach (string arg in args) {
                if (arg == Convert.ToString (args[0])) {
                    continue;
                }

                allArgs = allArgs + " " + arg;
            }

            player.ChatMessage (msg ("okay", player.userID.ToString ()));
            webrequest.EnqueuePost ("https://api.vk.com/method/messages.send?v=5.69", "&access_token=" + Config["token"] +"&chat_id=" + Config["chatid"] + "&user_id=" + Config["userid"] + "&message=Сервер: " + Config["nameserver"] + "\nНик игрока: " + player.displayName + "\nSteam ID: " + player.userID.ToString () + "\nЖалоба: " + allArgs, (code, response) => PostCallback (code, response, player), this);

        }

        [ChatCommand ("skype")]
        void cmdVKSkype (BasePlayer player, string cmd, string[] args) {
            if (args.Length == 0) {
                player.ChatMessage (string.Format (msg ("infoskype", player.userID.ToString ()), "/skype"));
                return;
            }
            if (CooldownsSkype.ContainsKey (player)) {
                double minutes = CooldownsSkype[player].Subtract (DateTime.Now).TotalMinutes;
                if (minutes >= 0) {
                    player.ChatMessage (string.Format (msg ("cooldowns"), minutes));
                    return;
                }
            }
            CooldownsSkype[player] = DateTime.Now.AddMinutes (CooldownSkype);
            string allArgs;
            allArgs = Convert.ToString (args[0]);

            foreach (string arg in args) {
                if (arg == Convert.ToString (args[0])) {
                    continue;
                }

                allArgs = allArgs + " " + arg;
            }

            player.ChatMessage (msg ("okayskype", player.userID.ToString ()));
            webrequest.EnqueuePost ("https://api.vk.com/method/messages.send?v=5.69", "&access_token=" + Config["token"] + "&chat_id=" + Config["chatidskype"] +"&user_id=" + Config["useridskype"] + "&message=Сервер: " + covalence.Server.Name + "\nНик игрока: " + player.displayName + "\nSteam ID: " + player.userID.ToString () + "\nСкайп: " + allArgs, (code, response) => PostCallback (code, response, player), this);

        }

        [ChatCommand ("cc")]
        void blindCMD (BasePlayer player, string command, string[] args) {
            if (!permission.UserHasPermission (player.UserIDString, permissionName)) {
                player.ChatMessage (msg ("no permissions", player.userID.ToString ()));
                return;

            }

            if (args.Length == 1) {

                List<BasePlayer> PlayerList = FindPlayer (args[0]);
                if (PlayerList.Count > 1) {
                    player.ChatMessage (msg ("More than one result", player.userID.ToString ()));
                    return;
                }

                if (PlayerList.Count == 0) {
                    player.ChatMessage (msg ("player not found", player.userID.ToString ()));
                    return;
                }

                BasePlayer targetplayer = PlayerList[0];

                if (targetplayer == null) {

                    player.ChatMessage (msg ("player not found", player.userID.ToString ()));
                    return;
                }

                if (!targetplayer.IsConnected) {

                    player.ChatMessage (msg ("target offline", player.userID.ToString ()));
                    return;
                }

                if (GUIinfo.ContainsKey (targetplayer.UserIDString)) {

                    if (GUIinfo[targetplayer.UserIDString]) {
                        player.ChatMessage (msg ("player already alert", player.userID.ToString ()));
                        return;
                    }
                    DoGUI (targetplayer, 0.0f, true, null);
                    return;
                } else {

                    DoGUI (targetplayer, 0.0f, false, null);
                    player.ChatMessage (string.Format (msg ("player alert", player.userID.ToString ()), targetplayer.displayName));
                    return;
                }
            } else
                player.ChatMessage (msg ("alert invalid syntax", player.userID.ToString ()));
        }

        [ChatCommand ("uncc")]
        void unblindCMD (BasePlayer player, string command, string[] args) {
            if (!permission.UserHasPermission (player.UserIDString, permissionName)) {
                player.ChatMessage (msg ("no permissions", player.userID.ToString ()));
                return;

            }

            if (args.Length == 1) {

                List<BasePlayer> PlayerList = FindPlayer (args[0]);
                if (PlayerList.Count > 1) {

                    player.ChatMessage (msg ("More than one result", player.userID.ToString ()));
                    return;
                }

                if (PlayerList.Count == 0) {
                    player.ChatMessage (msg ("player not found", player.userID.ToString ()));
                    return;
                }

                BasePlayer targetplayer = PlayerList[0];
                if (targetplayer == null) {
                    player.ChatMessage (msg ("player not found", player.userID.ToString ()));
                    return;
                }

                if (!targetplayer.IsConnected) {
                    player.ChatMessage (msg ("player not found", player.userID.ToString ()));
                    return;
                }

                if (GUIinfo.ContainsKey (targetplayer.UserIDString)) {
                    if (!GUIinfo[targetplayer.UserIDString]) {
                        player.ChatMessage (string.Format (msg ("player unalert", player.userID.ToString ()), targetplayer.displayName));
                        return;
                    } else {
                        GUIDestroy (targetplayer);
                        player.ChatMessage (string.Format (msg ("player unalert", player.userID.ToString ()), targetplayer.displayName));
                        return;
                    }
                }
                return;
            } else
                player.ChatMessage (msg ("unalert invalid syntax", player.userID.ToString ()));
        }

        [ConsoleCommand ("alert")]
        void cmdalert (ConsoleSystem.Arg arg) {
            if (arg.Connection != null) return;
            if (arg.Args == null) {
                Puts (msg ("alert invalid syntax"));
                return;
            }

            if (arg.Args.Length == 1) {

                List<BasePlayer> PlayerList = FindPlayer (arg.Args[0]);
                if (PlayerList.Count > 1) {
                    Puts (msg ("More than one result"));
                    return;
                }

                if (PlayerList.Count == 0) {
                    Puts (msg ("player not found"));
                    return;
                }

                BasePlayer targetplayer = PlayerList[0];

                if (targetplayer == null) {

                    Puts (msg ("player not found"));
                    return;
                }

                if (!targetplayer.IsConnected) {

                    Puts (msg ("target offline"));
                    return;
                }

                if (GUIinfo.ContainsKey (targetplayer.UserIDString)) {

                    if (GUIinfo[targetplayer.UserIDString]) {
                        Puts (msg ("player already alert"));
                        return;
                    }
                    DoGUI (targetplayer, 0.0f, true, null);
                    return;
                } else {

                    DoGUI (targetplayer, 0.0f, false, null);
                    Puts (string.Format (msg ("player alert"), targetplayer.displayName));
                    return;
                }
            } else
                Puts (msg ("alert invalid syntax"));

        }

        [ConsoleCommand ("unalert")]
        void cmdunalert (ConsoleSystem.Arg arg) {
            if (arg.Connection != null) return;
            if (arg.Args == null) {
                Puts (msg ("unalert invalid syntax"));
                return;
            }

            if (arg.Args.Length == 1) {

                List<BasePlayer> PlayerList = FindPlayer (arg.Args[0]);
                if (PlayerList.Count > 1) {

                    Puts (msg ("More than one result"));
                    return;
                }

                if (PlayerList.Count == 0) {
                    Puts (msg ("player not found"));
                    return;
                }

                BasePlayer targetplayer = PlayerList[0];
                if (targetplayer == null) {
                    Puts (msg ("player not found"));
                    return;
                }

                if (!targetplayer.IsConnected) {
                    Puts (msg ("player not found"));
                    return;
                }

                if (GUIinfo.ContainsKey (targetplayer.UserIDString)) {
                    if (!GUIinfo[targetplayer.UserIDString]) {
                        Puts (string.Format (msg ("player unalert"), targetplayer.displayName));
                        return;
                    } else {
                        GUIDestroy (targetplayer);
                        Puts (string.Format (msg ("player unalert"), targetplayer.displayName));
                        return;
                    }
                }
                return;
            } else
                Puts (msg ("unalert invalid syntax"));
        }

        void GUIDestroy (BasePlayer player) {
            CuiHelper.DestroyUi (player, Panel);
            GUIinfo[player.UserIDString] = false;
        }

        void DoGUI (BasePlayer targetplayer, float length, bool indic, string message) {
            var splitChars = new [] { ' ' };
            string[] positions = Convert.ToString (Config["Position"]).Split (splitChars, 4);

            if (indic == true) {

                var element = UI.CreateElementContainer (Panel, Convert.ToString (Config["Color"]), positions[0] + " " + positions[3], positions[2] + " " + positions[1], false);

                UI.CreateTextOutline (ref element, Panel, Convert.ToString (Config["DefaultText"]), Convert.ToString (Config["ColorText"]), "0 0 0 0", "1", "1", Convert.ToInt32 (Config["SizeText"]), "0 0", "1 1", TextAnchor.MiddleCenter);

                CuiHelper.AddUi (targetplayer, element);
                GUIinfo[targetplayer.UserIDString] = true;

                if (length > 0.0f)
                    timer.Once (length, () => { GUIDestroy (targetplayer); });
            } else if (indic == false) {
                GUIinfo.Add (targetplayer.UserIDString, false);

                var element = UI.CreateElementContainer (Panel, Convert.ToString (Config["Color"]), positions[0] + " " + positions[3], positions[2] + " " + positions[1], false);

                UI.CreateTextOutline (ref element, Panel, Convert.ToString (Config["DefaultText"]), Convert.ToString (Config["ColorText"]), "0 0 0 0", "1", "1", Convert.ToInt32 (Config["SizeText"]), "0 0", "1 1", TextAnchor.MiddleCenter);

                CuiHelper.AddUi (targetplayer, element);
                GUIinfo[targetplayer.UserIDString] = true;

                if (length > 0.0f)
                    timer.Once (length, () => { GUIDestroy (targetplayer); });
            }
        }

        private string Panel = "AlertPanel";

        public class UI {
            static public CuiElementContainer CreateElementContainer (string panel, string color, string aMin, string aMax, bool cursor = false) {
                var NewElement = new CuiElementContainer () {
                    {
                        new CuiPanel {
                            Image = { Color = color },
                                RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                                CursorEnabled = cursor,
                        },
                        new CuiElement ().Parent,
                            panel
                    }
                };
                return NewElement;
            }

            static public void CreateTextOutline (ref CuiElementContainer element, string panel, string text, string colorText, string colorOutline, string DistanceA, string DistanceB, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter) {
                element.Add (new CuiElement {
                    Parent = panel,
                        Components = {
                            new CuiTextComponent { Color = colorText, FontSize = size, Align = align, Text = text },
                            new CuiOutlineComponent { Distance = DistanceA + " " + DistanceB, Color = colorOutline },
                            new CuiRectTransformComponent { AnchorMax = aMax, AnchorMin = aMin }
                        }
                });
            }

        }

        private static List<BasePlayer> FindPlayer (string nameOrId) {
            List<BasePlayer> x = new List<BasePlayer> ();

            foreach (var activePlayer in BasePlayer.activePlayerList) {
                if (activePlayer.UserIDString == nameOrId)
                    x.Add (activePlayer);
                if (activePlayer.displayName.Contains (nameOrId, CompareOptions.OrdinalIgnoreCase))
                    if (!x.Contains (activePlayer))
                        x.Add (activePlayer);
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrId)
                    if (!x.Contains (activePlayer))
                        x.Add (activePlayer);
            }
            return x;
        }

        void PostCallback (int code, string response, BasePlayer player) {
            if (response == null || code != 200) {
                PrintWarning ("Ошибка! сообщение не может быть отправлено.");
                return;
            } 
            
            else {
                PrintWarning (": игрок [" + player.displayName + "] отправил сообщение!");
                return;
            }
        }

        string msg (string key, object userID = null) => lang.GetMessage (key, this, userID == null ? null : userID.ToString ());

    }
}