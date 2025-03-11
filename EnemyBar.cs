using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("EnemyBar", "TopPlugin.ru", "1.0.2")]
// 332 строка добавлено                 if (targetPlayer == null) return;
    public class EnemyBar : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        private const string elemMain = "healthbar.main";
        private const string elemPanel = "healthbar.panel";
        private static EnemyBar plugin;

     #region ConfigData

        private static ConfigData configData;
        public class ConfigData
        {
            [JsonProperty("ОСНОВНЫЕ НАСТРОЙКИ")]
            public SettingBasic OptionsBasic;
            [JsonProperty("НАСТРОЙКИ ГРАФИЧЕСКОГО ИНТЕРФЕЙСА")]
            public SettingGUI OptionsGUI;

            public class SettingBasic
            {
                [JsonProperty("Разрешение для отображения интерфейса")]
                public string permUse;
                [JsonProperty("Текст в графическом интерфейсе")]
                public string woundedText;
                [JsonProperty("Интервал проверки здоровья")]
                public float healthCheckInterval;
            }

            public class SettingGUI
            {
                [JsonProperty("Позиция графического интерфейса")]
                public string position;
                [JsonProperty("Цвет фона графического интерфейса")]
                public string colorBackground;
                [JsonProperty("Цвет шкалы здоровья графического интерфейса")]
                public string colorLine;
                [JsonProperty("Размер текста в графическом интерфейсе")]
                public int textSize;
                [JsonProperty("Цвет текста в графическом интерфейсе")]
                public string textColor;
                [JsonProperty("Ссылка на изображение")]
                public string iconUrl;
                [JsonProperty("Время отображения шкалы здоровья |секунд|")]
                public int duration;
                [JsonProperty("Время принудительного удаления шкалы здоровья |секунд|")]
                public int forceDuration;
            }
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                OptionsBasic = new ConfigData.SettingBasic
                {
                    healthCheckInterval = 0.2f,
                    woundedText = "<color=#FF0000>ранен</color>",
                    permUse = "enemybar.use"
                },
                OptionsGUI = new ConfigData.SettingGUI
                {
                    position = "0.5 0.15",
                    colorBackground = "0.8 0.8 0.8 0.3",
                    colorLine = "0.55 0.78 0.24 1",
                    textSize = 14,
                    textColor = "1 1 1 0.8",
                    iconUrl = "https://i.imgur.com/OIeOcBr.png",
                    duration = 5,
                    forceDuration = 15
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();
        }
        protected override void SaveConfig() => Config.WriteObject(configData);

     #endregion

        private void Init()
        {
            permission.RegisterPermission(configData.OptionsBasic.permUse, this);
            plugin = this;
            timer.Every(Core.Random.Range(500, 700), CheckPlayers);
        }

        private void OnServerInitialized()
        {
            
            AddImage(elemMain, configData.OptionsGUI.iconUrl);
            CheckPlayers();
        }

        private void Unload()
        {
            DestroyScripts();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, configData.OptionsBasic.permUse) == false)
            {
                return;
            }
            player.gameObject.GetOrAddComponent<HealthBar>();
        }

        private void OnPlayerAttack(BasePlayer player, HitInfo info)
        {
            var script = player.GetComponent<HealthBar>();
            if (script != null)
            {
                script.OnAttacked(info?.HitEntity);
            }
        }

        private void CheckPlayers()
        {
            timer.Once(1f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    OnPlayerConnected(player);
                }
            });
        }

        private void DestroyScripts()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var script = player.GetComponent<HealthBar>();
                UnityEngine.Object.Destroy(script);
            }
        }

        private void CreateGUI(BasePlayer player, float fraction, string value)
        {
            var container = new CuiElementContainer();
            var cfg = configData.OptionsGUI;
            var sizeX = 0.98 * fraction;

            container.Add(new CuiElement
            {
                Name = elemMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = cfg.position,
                        AnchorMax = cfg.position
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = elemPanel,
                Parent = elemMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = cfg.colorBackground,
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent
                    {
                        OffsetMin = "-95.5 0",
                        OffsetMax = "95.5 25"
                    }
                }
            });
            container.Add(new CuiElement
            {
                Parent = elemPanel,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = cfg.colorLine,
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.133 0.13",
                        AnchorMax = $"{sizeX} 0.87"
                    }
                }
            });
            container.Add(new CuiElement
            {
                Parent = elemPanel,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = value,
                        Align = TextAnchor.MiddleLeft,
                        FontSize = cfg.textSize,
                        Color = cfg.textColor
                    },
                    new CuiRectTransformComponent
                    {
                        OffsetMin = "32 0",
                        OffsetMax = "32 0",
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            container.Add(new CuiElement
            {
                Parent = elemPanel,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(elemMain),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax="0 0",
                        OffsetMin = "0 0", OffsetMax="25 25"
                    }
                }
            });
            CuiHelper.DestroyUi(player, elemMain);
            CuiHelper.AddUi(player, container);
        }

        private static void DestroyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, elemMain);
        }

        private void AddImage(string name, string url)
        {
            if (ImageLibrary == null || ImageLibrary?.IsLoaded == false)
            {
                timer.Once(3f, () =>
                {
                    AddImage(name, url);
                });
                return;
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                return;
            }
            ImageLibrary.CallHook("AddImage", url, name, (ulong) 0);
        }

        private string GetImage(string name)
        {
            return ImageLibrary?.Call<string>("GetImage", name);
        }

        private class HealthBar : MonoBehaviour
        {
            private BasePlayer player;
            private BaseCombatEntity target;
            private BasePlayer targetPlayer;
            private float lastHealth;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            public void OnAttacked(BaseEntity entity)
            {
                if (entity == null || target == entity)
                {
                    return;
                }
                target = entity.GetComponent<BaseCombatEntity>();
                targetPlayer = entity.GetComponent<BasePlayer>();

                if (IsInvoking(nameof(CheckHealth)) == false)
                {
                    InvokeRepeating(nameof(CheckHealth), configData.OptionsBasic.healthCheckInterval, configData.OptionsBasic.healthCheckInterval);
                }

                if (configData.OptionsGUI.forceDuration != 0)
                {
                    if (IsInvoking(nameof(ForceDestroy)) == false)
                    {
                        Invoke(nameof(ForceDestroy), configData.OptionsGUI.forceDuration);
                    }
                }
            }

            private void CheckHealth()
            {
                if (target == null)
                {
                    CancelInvoke(nameof(CheckHealth));
                    DestroyGUI(player);
                    return;
                }

                

                if (Math.Abs(target.Health() - lastHealth) > 0.2f)
                {
                    OnDamaged();
                }
            }

            private void OnDamaged()
            {
                if (targetPlayer == null) return;
                lastHealth = target.Health();
                var value = Convert.ToInt32(target.Health()).ToString();
                var fraction = target.Health() / target.MaxHealth();

                if (lastHealth < 10)
                {
                    if (targetPlayer?.IsWounded() ?? false)
                    {
                        value = configData.OptionsBasic.woundedText;
                        fraction = 0.15f;
                    }
                }

                plugin.CreateGUI(player, fraction, value);
                CancelInvoke(nameof(TimedDestroy));
                Invoke(nameof(TimedDestroy), configData.OptionsGUI.duration);
            }

            private void ForceDestroy()
            {
                target = null;
                CancelInvoke(nameof(CheckHealth));
                DestroyGUI(player);
            }

            private void TimedDestroy()
            {
                DestroyGUI(player);
            }
        }
    }
}
