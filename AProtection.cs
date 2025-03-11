using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AProtection", "Seckret", "1.5.0")]
    public class AProtection : RustPlugin
    {
        #region Variables

        private class IAuthorize
        {
            [JsonIgnore] public bool IsAuthed;
            [JsonIgnore] public Vector3 LastPosition;
            [JsonIgnore] public Timer Timer;
            
            [JsonProperty("Пароль пользователя")]
            public string Password;
            [JsonProperty("Авторизованные IP адреса")]
            public List<string> AuthedIP;

            public void Authed(BasePlayer player, string ip)
            {
                if (Timer != null && !Timer.Destroyed)
                    Timer.Destroy();
                
                if (!AuthedIP.Contains(ip))
                    AuthedIP.Add(ip);
                CuiHelper.DestroyUi(player, Layer + ".Hide2");
                CuiHelper.DestroyUi(player, Layer + ".Hide1");
                CuiHelper.DestroyUi(player, Layer + ".Hide");
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        
        [JsonProperty("Информация об игроках")]
        private Dictionary<ulong, IAuthorize> StoredData = new Dictionary<ulong,IAuthorize>();
        [JsonProperty("Слой с интерфейсом")] private static string Layer = "UI.Login";

        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["АВТОРИЗОВАН.IP"] =           "<size=16>Вы автоматически <color=#4286f4>авторизовались</color>:</size>" +
                                               "\n<size=10>Вы уже авторизовались с IP: {0}</size>",
                
                ["ПОМОЩЬ.ЗАРЕГИСТРИРУЙТЕСЬ"] = "<size=16>Ваша учётная запись в <color=#4286f4>опасности</color>!</size>" +
                                               "\nЗагляните в консоль (<color=#4286f4>F1</color>) для устранения проблемы!",
                
                
                
                ["РЕГИСТРАЦИЯ. НЕ ВВЁЛ ПАРОЛЬ"] = "Вы не ввели пароль, либо его длина слишком мала!",
                
                ["РЕГИСТРАЦИЯ.УСПЕШНО"] = "Вы успешно зарегистрировались на сервере!\n" +
                                          "Выбранный пароль: {0}",
                
                ["АВТОРИЗАЦИЯ.УСПЕШНО"] = "Вы успешно авторизовались на сервере!",
                
                ["НЕВЕРНЫЙ ПАРОЛЬ"] = $"Вы ввели неверный пароль, попробуйте ещё раз!" +
                                      $"Команда авторизации: auth <ваш пароль>",
                ["ЗАРЕГИСТРИРУЙТЕСЬ"] = "ЗАРЕГИСТРИРУЙТЕСЬ",
                ["АВТОРИЗУЙТЕСЬ"] = "АВТОРИЗУЙТЕСЬ",
                ["ИНФО"] = "Вся информация находится в консоле",
                
                ["ПОДСКАЗКА.РЕГИСТРАЦИЯ"] = "Приветствую! Чтобы зарегистрироваться - придумай и впиши пароль в консоль!\n" +
                                            "Вам больше не придётся вводить пароль c IP: {0}\n" +
                                            $"Команда: auth <придуманный пароль>",
                ["ПОДСКАЗКА.АВТОРИЗАЦИЯ"] = "Приветствую! Вы зарегистрированы - впиши свой пароль в консоль!\n" +
                                            "Вам больше не придётся вводить пароль c IP: {0}\n" +
                                            $"Команда: auth <придуманный пароль>"
            }, this, "ru");
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["АВТОРИЗОВАН.IP"] =           "<size=16>You successful <color=#4286f4>authorized</color>:</size>" +
                                               "\n<size=10>You already authorized from IP: {0}</size>",
                
                ["ПОМОЩЬ.ЗАРЕГИСТРИРУЙТЕСЬ"] = "<size=16>Your account is <color=#4286f4>not protected</color>!</size>" +
                                               "\nCheck Console (<color=#4286f4>F1</color>) to fix trouble!",
                
                
                
                ["РЕГИСТРАЦИЯ. НЕ ВВЁЛ ПАРОЛЬ"] = "Password is empty!",
                
                ["РЕГИСТРАЦИЯ.УСПЕШНО"] = "You successful registered on server!\n" +
                                          "Your password: {0}",
                
                ["АВТОРИЗАЦИЯ.УСПЕШНО"] = "You successful authorized!",
                
                ["НЕВЕРНЫЙ ПАРОЛЬ"] = $"You entered wrong password!" +
                                      $"Command to authorize: auth <your password>",
                ["ЗАРЕГИСТРИРУЙТЕСЬ"] = "SIGN UP",
                ["АВТОРИЗУЙТЕСЬ"] = "LOG IN",
                ["ИНФО"] = "All information in console",
                
                ["ПОДСКАЗКА.РЕГИСТРАЦИЯ"] = "Hello! To sign up, enter auth <your password> in console !\n" +
                                            "You will not need to enter password from IP: {0}\n" +
                                            $"Command: auth <your password>",
                ["ПОДСКАЗКА.АВТОРИЗАЦИЯ"] = "Hello! To log in, enter auth <your password> in console!\n" +
                                            "You will not need to enter password from IP: {0}\n" +
                                            $"Command: auth <your password>"
            }, this, "en");
            
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("AProtection"))
                StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, IAuthorize>>("AProtection");
            
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }

        private void Unload() =>
            Interface.Oxide.DataFileSystem.WriteObject("AProtection", StoredData);

        #endregion

        #region Hooks
        
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !StoredData.ContainsKey(player.userID))
                return null;

            IAuthorize buffer = StoredData[player.userID];
            if (!buffer.IsAuthed && arg.cmd.FullName.ToLower() != "global.auth")
                return false;
            
            return null;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }

            string IP = GetIP(player.net.connection.ipaddress);
            
            if (!StoredData.ContainsKey(player.userID))
            {
                IAuthorize buffer = new IAuthorize();
                buffer.AuthedIP = new List<string>() /*{ IP }*/;
                buffer.LastPosition = player.transform.position;
                buffer.IsAuthed = false;
                StoredData.Add(player.userID, buffer);
            }
            else
            {
                IAuthorize buffer = StoredData[player.userID];
                buffer.LastPosition = player.transform.position;
                buffer.IsAuthed = false;
                
                if (buffer.AuthedIP.Contains(IP))
                {
                    buffer.IsAuthed = true;
                    player.ChatMessage(FL("АВТОРИЗОВАН.IP").Replace("{0}", IP));
                    return;
                }
            }

            ForceAuthorization(player);
            DrawInterface(player);
        }

        #endregion

        #region Commands

        [ConsoleCommand("auth")]
        private void cmdAuthConsole(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null)
                return;
            
            if (!StoredData.ContainsKey(player.userID))
                OnPlayerInit(player);

            if (!args.HasArgs(1) || args.FullString.Length == 0)
            {
                args.ReplyWithObject(FL("РЕГИСТРАЦИЯ. НЕ ВВЁЛ ПАРОЛЬ"));
                return;
            }

            string ip = GetIP(player.net.connection.ipaddress);
            string password = args.FullString;
            IAuthorize buffer = StoredData[player.userID];
            if (buffer.IsAuthed)
                return;

            if (string.IsNullOrEmpty(buffer.Password))
            {
                args.ReplyWithObject(FL("РЕГИСТРАЦИЯ.УСПЕШНО").Replace("{0}", password));
                buffer.Password = password;
                buffer.IsAuthed = true;
                buffer.Authed(player, ip);
            }
            else if (password == buffer.Password)
            {
                args.ReplyWithObject(FL("АВТОРИЗАЦИЯ.УСПЕШНО"));
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
                buffer.IsAuthed = true;
                buffer.Authed(player, ip);
            }
            else
            {
                args.ReplyWithObject(FL("НЕВЕРНЫЙ ПАРОЛЬ"));
                return;
            }
        }

        #endregion

        #region Functions

        private void ForceAuthorization(BasePlayer player)
        {
            IAuthorize buffer = StoredData[player.userID];
            if (buffer.IsAuthed)
                return;
            
            if (string.IsNullOrEmpty(buffer.Password))
            {
                player.ChatMessage(FL("ПОМОЩЬ.ЗАРЕГИСТРИРУЙТЕСЬ"));
                player.SendConsoleCommand($"echo {FL("ПОДСКАЗКА.РЕГИСТРАЦИЯ").Replace("{0}", GetIP(player.net.connection.ipaddress))}");
                buffer.Timer = timer.Once(10, () => ForceAuthorization(player));
                return;
            }
            
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
            player.SendConsoleCommand($"echo {FL("ПОДСКАЗКА.АВТОРИЗАЦИЯ").Replace("{0}", GetIP(player.net.connection.ipaddress))}");
            player.Teleport(buffer.LastPosition);
            buffer.Timer = timer.Once(2, () => ForceAuthorization(player));
        }

        #endregion

        #region Helpers

        private string GetIP(string input)
        {
            if (input.Contains(":"))
                return input.Split(':')[0];

            return input;
        }

        private string FL(string key) => lang.GetMessage(key, this);

        #endregion

        #region GUI
        
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private void DrawInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            string action = string.IsNullOrEmpty(StoredData[player.userID].Password) ? FL("ЗАРЕГИСТРИРУЙТЕСЬ") : FL("АВТОРИЗУЙТЕСЬ");

            container.Add(new CuiPanel
            {
                FadeOut = 2f,
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { FadeIn = 2f, Color = HexToRustFormat("#51514E47") }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                FadeOut = 2f,
                RectTransform = { AnchorMin = "0.3347656 0.4388889", AnchorMax = "0.6652344 0.5611111" },
                Text = { FadeIn = 2f, Text = "", FontSize = 32, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                Button = { FadeIn = 2f, Color = HexToRustFormat("#8484844A") },
            }, Layer, Layer + ".Hide");

            container.Add(new CuiLabel
            {
                FadeOut = 2f,
                RectTransform = { AnchorMin = "0 0.3", AnchorMax = "1 1" },
                Text = { FadeIn = 2f, Text = action, FontSize = 38, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
            }, Layer + ".Hide", Layer + ".Hide1");

            container.Add(new CuiLabel
            {
                FadeOut = 2f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.5" },
                Text = { FadeIn = 2f, Text = FL("ИНФО"), FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
            }, Layer + ".Hide", Layer + ".Hide2");

            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}