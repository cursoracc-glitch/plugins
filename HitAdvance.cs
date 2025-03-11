using Facepunch;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Color = UnityEngine.Color;
using Time = UnityEngine.Time;

namespace Oxide.Plugins 
{ 
    [Info("HitAdvance", "Monster. x Kirtne", "1.0.1")]
    class HitAdvance : RustPlugin  
    {
        #region Hooks
        [PluginReference] private Plugin ImageLibrary;
        private void OnServerInitialized()
        {
            InitializeLang();
            LoadImages();
        }
        void LoadImages()
        {
            foreach (var imgKey in Images.Keys.ToList())
            {
                ImageLibrary.Call("AddImage", Images[imgKey], imgKey);
            }
        }
        Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            { "hitmarker.kill", "https://s3.aeza.cloud/stormrust/hitmarker/death.png" },
            { "hitmarker.hit.wound", "https://s3.aeza.cloud/stormrust/hitmarker/woundead.png" },
        };
        private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            BaseEntity entity = info.HitEntity;
            
            if (entity == null || !(entity is BasePlayer)) return;
            
            NextTick(() =>
            {
                BasePlayer victim = entity as BasePlayer;

                if (attacker.currentTeam == victim.currentTeam && attacker.currentTeam != 0)
                {
                    CuiHelper.DestroyUi(attacker, "normalhit");
                    HitGUI(attacker, lang.GetMessage("FRIEND", this, attacker.UserIDString));
                }
                else if (victim.IsDead())
                {
                    CuiHelper.DestroyUi(attacker, "hitpng");
                    CuiHelper.DestroyUi(attacker, "normalhit");
                    HitPng(attacker, "hitmarker.kill");
                }
                else if (info.isHeadshot)
                {
                    CuiHelper.DestroyUi(attacker, "normalhit");
                    HitGUI(attacker, $"<color=#ff6b6b>-{info.damageTypes.Total().ToString("F0")}</color>");
                }
                else
                {
                    CuiHelper.DestroyUi(attacker, "normalhit");
                    HitGUI(attacker, "-" + info.damageTypes.Total().ToString("F0"));
                }
            });
        }
        void OnPlayerWound(BasePlayer player)
        {
            var attacker = player?.lastAttacker as BasePlayer;
            if (attacker == null) return;
            CuiHelper.DestroyUi(attacker, "normalhit");
            HitGUI(attacker, lang.GetMessage("WOUNDED", this, attacker.UserIDString));
            CuiHelper.DestroyUi(attacker, "hitpng");
            HitPng(attacker, "hitmarker.hit.wound");
        }

        #endregion

        #region GUI

        private void HitGUI(BasePlayer attacker, string text)
        {
            CuiElementContainer container = new CuiElementContainer();
            
            float x = (float)Core.Random.Range(0.47, 0.53), y = (float)Core.Random.Range(0.5, 0.5);
            string ID = CuiHelper.GetGuid().ToString(); 
            
            container.Add(new CuiElement
            {
                Parent = "Hud",
                Name = "normalhit",
                FadeOut = 0.3f,
                Components =
                {
                    new CuiTextComponent { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 13, FadeIn = 0.5f },
                    new CuiRectTransformComponent { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-25 -45", OffsetMax = "25 -25" },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.15 0.15" }
                }
            }); 
            
            CuiHelper.AddUi(attacker, container);
            attacker.Invoke(() => CuiHelper.DestroyUi(attacker, "normalhit"), 0.5f);
        }

        private void HitPng(BasePlayer attacker, string png)
        {
            CuiElementContainer container = new CuiElementContainer();

            float x = (float)Core.Random.Range(0.47, 0.53), y = (float)Core.Random.Range(0.5, 0.5);
            string ID = CuiHelper.GetGuid().ToString();

            container.Add(new CuiElement
            {
                Parent = "Hud",
                Name = "hitpng",
                FadeOut = 0.3f,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", png) },
                    new CuiRectTransformComponent { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                }
            });

            CuiHelper.AddUi(attacker, container);
            attacker.Invoke(() => CuiHelper.DestroyUi(attacker, "hitpng"), 0.5f);
        }

        #endregion

        #region Lang

        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FRIEND"] = "<color=#6bff72>FRIEND</color>",
                ["DEAD"] = "<color=#ff6b6b>DEAD</color>",
                ["WOUNDED"] = "<color=#ffb86b>WOUNDED</color>"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FRIEND"] = "<color=#6bff72>ДРУГ</color>",
                ["DEAD"] = "<color=#ff6b6b>УБИТ</color>",
                ["WOUNDED"] = "<color=#ffb86b>УПАЛ</color>"
            }, this, "ru");
        }

        #endregion
    }
}
