using System;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoteUI", "redesign Deversive", "1.0.3")]
    public class NoteUI : RustPlugin
    {
        #region Variables

        [PluginReference] private Plugin ImageLibrary;
        string NoteUIHandler = "NoteUI_Main";

        #endregion

        #region Config

        public class DataConfig
        {
            [JsonProperty("Иконка на эффект 'Взрыва'")]
            public string explosioneffecticon;
            [JsonProperty("Иконка на эффект 'Информация'")]
            public string infoeffecticon;
            [JsonProperty("Иконка на эффект 'Заблокировано'")]
            public string lockeffecticon;
            [JsonProperty("Включить звук при получении уведомления? (false - нет)")]
            public bool usesounds;
            [JsonProperty("Время постепенного появления")]
            public float fadein;
            [JsonProperty("Время через которое оповещение будет удалено")]
            public int timetodelete;
        }

        public DataConfig cfg;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            cfg = Config.ReadObject<DataConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            cfg = new DataConfig()
            {
                explosioneffecticon = "https://i.imgur.com/EKlH8Hy.png",
                lockeffecticon = "https://i.imgur.com/ETrXVzq.png",
                infoeffecticon = "https://i.imgur.com/nCAejF7.png",
                usesounds = true,
                fadein = 0.4f,
                timetodelete = 5,
            };
        }

        #endregion

        #region HooksAndMethods

        [HookMethod("DrawExplosionNote")]
        public void DrawExplosionNote(BasePlayer player, string Text, string Description)
        {
            if (player == null || Text == null || Description == null) return;
            NoteUIAdd(player, "explosion", Text, Description);
        }

        [HookMethod("DrawLockNote")]
        public void DrawLockNote(BasePlayer player, string Name, string Description)
        {
            if (player == null || Name == null || Description == null) return;
            NoteUIAdd(player, "lock", Name, Description);
        }

        [HookMethod("DrawInfoNote")]
        public void DrawInfoNote(BasePlayer player, string Text)
        {
            if (player == null || Text == null) return;
            var Description = "";
            NoteUIAdd(player, "info", Text, Description);
        }

        private string notifimage = "https://imgur.com/26ZzZI5.png";
        private string bell = "https://imgur.com/e4Jg2qz.png";
        
        void OnServerInitialized()
        {
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintWarning("Плагин 'ImageLibrary' не загружен, дальнейшая работа плагина невозможна!");
                Unload();
                return;
            }
            //ImageLibrary.Call("AddImage", cfg.explosioneffecticon, "explosion");
            //ImageLibrary.Call("AddImage", cfg.lockeffecticon, "lock");
            ImageLibrary.Call("AddImage", notifimage, "info");
            ImageLibrary.Call("AddImage", bell, "bell");
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, NoteUIHandler);
            }
        }

        private void NoteUIAdd(BasePlayer player, string Type, string Name, string Description)
        {
            if (player.IsReceivingSnapshot || player.IsSleeping()) return;
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, NoteUIHandler);
            switch (Type)
            {
                /*case "explosion":
                    container.Add(new CuiPanel
                    {
                        Image = { FadeIn = cfg.fadein, Color = HexToCuiColor("#d4a1d400") },
                        RectTransform = { AnchorMin = "0.3011301 0.8373263", AnchorMax = "0.6792551 0.9283854" },
                        CursorEnabled = false,
                    }, "Overlay", NoteUIHandler);
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiRectTransformComponent { AnchorMin = "0.008419432 0.4477783", AnchorMax = "0.0580062 0.9277781" },
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.0709796 0.705116", AnchorMax = "0.9800709 1.293351"},
                            new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = Name, Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.07438016 0.4799993", AnchorMax = "0.8181818 0.8799993"},
                            new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = Description, Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "RobotoCondensed-regular.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.07644629 0.1399995", AnchorMax = "0.9896694 0.5399995"},
                            new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.3 0.3"}
                        }
                    });
                    break;
                case "lock":
                    container.Add(new CuiPanel
                    {
                        Image = { FadeIn = cfg.fadein, Color = HexToCuiColor("#d4a1d400") },
                        RectTransform = { AnchorMin = "0.3011301 0.8373263", AnchorMax = "0.6792551 0.9283854" },
                        CursorEnabled = false,
                    }, "Overlay", NoteUIHandler);
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiRectTransformComponent { AnchorMin = "0.008419432 0.4477783", AnchorMax = "0.0580062 0.9277781" },
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.0709796 0.6009494", AnchorMax = "0.9829992 1.189184"},
                            new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = Name, Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-regular.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.07438016 0.4799993", AnchorMax = "0.8181818 0.8799993"},
                            new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = Description, Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "RobotoCondensed-regular.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.07644629 0.1399995", AnchorMax = "0.9896694 0.5399995"},
                            new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    break;*/
                case "info":
                    container.Add(new CuiPanel
                    {
                        Image = { FadeIn = cfg.fadein, Color = HexToCuiColor("#d4a1d400") },
                        RectTransform = { AnchorMin = "0.3947916 0.8259259", AnchorMax = "0.6682292 0.9287037" },
                        CursorEnabled = false,
                    }, "Overlay", NoteUIHandler);
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                       //Name = "background",
                        Components =
                        {
                            //new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiImageComponent { Png = (string) ImageLibrary.Call("GetImage", "info"), Material = "assets/icons/greyout.mat"},
                            new CuiRectTransformComponent {AnchorMin = "0.1012163 0.2342343", AnchorMax = "0.9657144 0.8018019"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        //Name = "background",
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.UpperLeft, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.1658087 0.4234225", AnchorMax = "0.6905609 0.7837829"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        //Name = "background",
                        Components =
                        {
                            new CuiTextComponent { FadeIn = cfg.fadein, Text = Name, Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.1564356 0.2612609", AnchorMax = "0.9168315 0.6126121"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = NoteUIHandler,
                        //Name = "background",
                        Components =
                        {
                            //new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiImageComponent { Png = (string) ImageLibrary.Call("GetImage", "bell"), Material = "assets/icons/greyout.mat"},
                            new CuiRectTransformComponent {AnchorMin = "0 0.09909844", AnchorMax = "0.1980195 0.9999994"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    
                    break;
            }
            CuiHelper.AddUi(player, container);
            if (cfg.usesounds) Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", player, 0, Vector3.zero, Vector3.zero);
            timer.Once(cfg.timetodelete, () => CuiHelper.DestroyUi(player, NoteUIHandler));
        }

        #endregion

        #region Helpers

        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}