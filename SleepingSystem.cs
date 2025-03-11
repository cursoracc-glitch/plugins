using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SleepingSystem", "Hougan", "0.0.1")]
    public class SleepingSystem : RustPlugin
    {
        #region Classes

        private class Configuration
        {
            [JsonProperty("Количество восстанавливаемого здоровья за одну секунду")]
            public float HealPerSecond = 1f;
            [JsonProperty("Количество отнимаемого 'голода' за одну секунду")]
            public float CaloriedPerSecond = 4f; 
            [JsonProperty("Количество отнимаемой 'воды' за одну секунду")]
            public float HydraPerSecond = 2f; 

            [JsonProperty("Разрешать спать только на спальниках")]
            public bool SleepOnlyOnBag = true; 
            [JsonProperty("Запрещать спать в зоне чужого шкафа")]
            public bool BlockBuildSleep = true;
        }

        private class Sleeper : MonoBehaviour
        {
            private const string UpdateLayer = Layer + ".CounterLayer";
            private const string HydraLayer = Layer + ".Hydra";
            private const string FoodLayer = Layer + ".Food";
            private const string Layer = "UI_SleeperLayer";
            
            private BasePlayer Player;
            private float Interval = 0.25f;
            
            private float HealAmount;
            private float HydraAmount;
            private float CaloriesAmount;
            
            public void Awake()
            {
                Player = GetComponent<BasePlayer>();
                Player.StartSleeping();

                HealAmount = Interval * Settings.HealPerSecond;
                HydraAmount = Interval * Settings.HydraPerSecond;
                CaloriesAmount = Interval * Settings.CaloriedPerSecond;
                
                DrawInterface();
                
                InvokeRepeating(nameof(ControlUpdate), 0, Interval); 
            }

            public void DrawInterface()
            {
                CuiHelper.DestroyUi(Player, Layer);
                
                CuiElementContainer container = new CuiElementContainer();
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.2", AnchorMax = "0.5 0.2", OffsetMin = "-220 -20", OffsetMax = "220 20" },
                    Image = { Color = "1 1 1 0.2", Material = "" }
                }, "Overlay", Layer);
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0.7", AnchorMax = $"1 1", OffsetMin = "35 -55", OffsetMax = "-35 -45" },
                    Image = { Color = "1 1 1 0.202840789", Material = "" }
                }, Layer, FoodLayer);
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0.7", AnchorMax = $"1 1", OffsetMin = "35 -80", OffsetMax = "-35 -70" },
                    Image = { Color = "1 1 1 0.200728489", Material = "" }
                }, Layer, HydraLayer);

                if (Settings.CaloriedPerSecond + Settings.HydraPerSecond > 0)
                {
                    container.Add(new CuiLabel 
                    {
                        RectTransform = { AnchorMin = $"0 1", AnchorMax = "1 1", OffsetMin = "-200 5", OffsetMax = "200 500" },
                        Text = { Text = "Во время восстановления здоровья, тратятся калории, имейте ввиду!", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.4" }
                    }, Layer);
                }

                CuiHelper.AddUi(Player, container);
            }

            public void ControlUpdate()
            {
                if (Player.IsDead() || Player.IsWounded())
                {
                    Destroy(this);
                    return;
                }

                if (Player.health != 100)
                {
                    Player.metabolism.calories.Subtract(CaloriesAmount);
                    Player.metabolism.hydration.Subtract(HydraAmount); 
                    Player.metabolism.SendChangesToClient();
                    Player.Heal(HealAmount);
                }

                if (Player.metabolism.calories.value < CaloriesAmount || Player.metabolism.hydration.value < HydraAmount)
                {
                    Destroy(this);
                    return;
                }


                UpdateInterface();
            }

            public void UpdateInterface()
            {
                CuiHelper.DestroyUi(Player, UpdateLayer);
                CuiHelper.DestroyUi(Player, UpdateLayer + ".F");
                CuiHelper.DestroyUi(Player, UpdateLayer + ".D");
                
                CuiHelper.DestroyUi(Player, Layer + ".UpdateLayer");
                CuiElementContainer container = new CuiElementContainer();
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"{Player.health / Player._maxHealth} 1", OffsetMax = "0 0" },
                    Image = { Color = "0.5 0.8 0.5 0.6", Material = "" }
                }, Layer, UpdateLayer);
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"{Player.metabolism.calories.value / Player.metabolism.calories.max} 1", OffsetMax = "0 0" },
                    Image = { Color = "1 0.8 0.5 0.6", Material = "" }
                }, FoodLayer, UpdateLayer + ".F"); 
                
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"{Player.metabolism.hydration.value / Player.metabolism.hydration.max} 1", OffsetMax = "0 0" },
                    Image = { Color = "0.5 0.8 0.8 0.6", Material = "" }
                }, HydraLayer, UpdateLayer + ".D");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{Player.health:F1} / {Player._maxHealth:F1} HP", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 24 }
                }, Layer, Layer + ".UpdateLayer");

                CuiHelper.AddUi(Player, container);
            }

            public void OnDestroy()
            {
                if (Player.IsConnected)
                {
                    CuiHelper.DestroyUi(Player, Layer);
                    Player.EndSleeping();
                }
                
            }
        }
        
        #endregion

        #region Variables

        private static Configuration Settings = new Configuration();

        #endregion

        #region Hooks
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            { 
                Settings = Config.ReadObject<Configuration>();
                if (Settings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => Settings = new Configuration(); 
        protected override void SaveConfig() => Config.WriteObject(Settings);

        private void OnPlayerInput(BasePlayer player, InputState state)
        {
            if (state.IsDown(BUTTON.DUCK) && state.WasJustPressed(BUTTON.RELOAD))
            {
                if (Settings.BlockBuildSleep && player.IsBuildingBlocked())
                {
                    player.ChatMessage("Вы не можете спать на чужой территории!");
                    return;
                }
                if (Settings.SleepOnlyOnBag) 
                {
                    RaycastHit hitInfo; 
                    if (!Physics.Raycast(player.transform.position, Vector3.down, out hitInfo, 2.284f) || hitInfo.GetEntity() == null) return;
                    var entity = hitInfo.GetEntity();
                    
                    if (!(entity is SleepingBag)) return;
                }
                var obj = player.GetComponent<Sleeper>();
                if (obj == null) player.gameObject.AddComponent<Sleeper>();
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            var obj = player.GetComponent<Sleeper>();
            if (obj != null) UnityEngine.Object.Destroy(obj); 
        }

        private void Unload()
        {
            foreach (var sleeper in UnityEngine.Object.FindObjectsOfType<Sleeper>())
                UnityEngine.Object.Destroy(sleeper);
        }

        #endregion
    }
}