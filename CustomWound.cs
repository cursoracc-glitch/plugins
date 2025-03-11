using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CustomWound", "CustomWound", "1.0.9")]
	[Description("Custom Recovery")]
    public class CustomWound : RustPlugin
    {
        #region Components

        private class PlayerRecover : MonoBehaviour
        {
            private BasePlayer player;
            private float lastUpdate = 0;
            private float lastScream = 0;

            public int endChance = 0;
            private int resultChance; 
            private int different;

            public bool timeDrawed = false;
            
            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                if (player == null)
                {
                    Destroy(this);
                    return;
                }
                player.StartWounded();
                
                if (!CONF_DisableWoundEnd)
                {
                    endChance = instance.GetWakeChance(player);
                    resultChance = Core.Random.Range(0, 100);
                    different = resultChance - endChance;

                    if (CONF_CanCraftSpecialItem)
                    {
                        UI_DrawItemRecovery(player);
                    }
                    if (CONF_ShowChances)
                    {
                        UI_DrawWakeChances(player);
                    }
                }
                
                if (CONF_ShowEndWoundTime)
                {
                    UI_DrawLeftTime(player);
                }
            }

            private void Update()
            {
                lastUpdate += Time.deltaTime;

                if (lastUpdate > 1)
                {
                    lastUpdate = 0;
                    lastScream++;
                    
                    if (player.IsDead() || !player.IsWounded())
                    {
                        DisableObject(false);
                        return;
                    }
                    int leftTime = CONF_WoundTime - Convert.ToInt32(player.secondsSinceWoundedStarted);
                    if (leftTime > 0)
                    {
                        if (CONF_ShowEndWoundTime)
                        {
                            UI_DrawLeftTime(player);
                        }
                    }
                    else if (leftTime == 0)
                    {
                        if (CONF_DisableWoundEnd)
                        {
                            DisableObject(true);
                            return;
                        }
                        
                        if (resultChance <= endChance)
                        {
                            player.StopWounded();
                            DisableObject(false);
                        }
                        else
                        {
                            player.Die();
                            /*if (CONF_CustomMessages.Count != 0)
                            {
                                var getMessage = CONF_CustomMessages.First(p => p.Key > different).Value;
                                //UI_DrawChances(player, getMessage);
                            }*/
                            DisableObject(false);
                        }
                    }

                    if (lastScream > 5 && CONF_EnableScream)
                    {
                        MakeScream();
                        lastScream = 0;
                    }
                }
            }

            public void DisableObject(bool withDie)
            {
                CuiHelper.DestroyUi(player, UI_LayerItem);
                CuiHelper.DestroyUi(player, UI_LayerChance);
                CuiHelper.DestroyUi(player, UI_LayerTime);
                if (withDie)
                    player.Die();
                
                Destroy(this);
            }

            private void MakeScream()
            {
                Effect.server.Run("assets/bundled/prefabs/fx/player/beartrap_scream.prefab", player, 0, new Vector3(), new Vector3());
            }
        }

        #endregion
        
        #region Variables

        #region Configuration

        [JsonProperty("Отключить состояние ранения? (Автоматическая смерть)")]
        private static bool CONF_DisableWound = false;
        [JsonProperty("Отключить возможность встать после ранения?")]
        private static bool CONF_DisableWoundEnd = false;
        [JsonProperty("Новое время состояния ранения (Должно быть меньше 40)")]
        private static int CONF_WoundTime = 40;
        //[JsonProperty("Шанс упасть, при попадании пули (любого урона)")]
        //private static int CONF_WoundFromDamageChance = 3;
        [JsonProperty("Шанс встать после состояния ранения")]
        private static int CONF_WoundEndChance = 50;
        
        [JsonProperty("Показывать время до окончания ранения?")]
        private static bool CONF_ShowEndWoundTime = true;
        //[JsonProperty("Можно ли встать после ранения в зоне чужого шкафа?")]
        //private static bool CONF_CanEndWoundInBB = true;
        [JsonProperty("Разрешать поднимать игроков уколом шприца")]
        private static bool CONF_CanEndWoundBySyringe = true;

        [JsonProperty("Дополнительный шанс встать, при метаболизме > 250")]
        private static int CONF_StopWoundFromMetabolism = 10;
        [JsonProperty("Включить тряску экрана после подъёма предметом")]
        private static bool CONF_ShakeAfterWoundEnd = true;

        [JsonProperty("Специальная картинка для предмета")]
        private static string CONF_PictureURL = "https://i.imgur.com/FHC3hp7.png";
        [JsonProperty("Отображать шанс встать после падения")]
        private static bool CONF_ShowChances = true;
        [JsonProperty("Стандартное количество дефибрилляторов")]
        private static int CONF_DefaultDef = 5;
        [JsonProperty("Разрешить крафтить предмет специальный предмет")]
        private static bool CONF_CanCraftSpecialItem = true;
        [JsonProperty("Предметы необходимые для крафта")]
        public Dictionary<string, int> CONF_SpecialItemReceipt = new Dictionary<string,int>
        {
            ["syringe.medical"] = 5,
            ["largemedkit"] = 2
        };
        
        [JsonProperty("Включить крик игрока в состоянии ранения")]
        private static bool CONF_EnableScream = true;
        [JsonProperty("Дополнительные шансы встать для игроков с привилегиями")]
        private static Dictionary<string, int> CONF_CustomChances = new Dictionary<string, int>
        {
            ["customwound.40"] = 40,
            ["customwound.50"] = 50
        };
        /*[JsonProperty("Сообщение после смерти из-за ранения")]
        private static Dictionary<int, string> CONF_CustomMessages = new Dictionary<int, string>
        {
            [0] = "Да ладно, серьёзно: {0}",
            [10] = "Это было так близко: {0}",
            [20] = "Не слишком близко, но могло случиться: {0}",
            [30] = "Не стоит отчаиваться, в следующий раз повезёт: {0}",
            [40] = "Не везёт в игре, повезёт в любви: {0}",
            [40] = "Нет, ты даже близок не был: {0}"
        };*/

        #endregion

        #region System

        [PluginReference] private Plugin ImageLibrary;

        private static CustomWound instance;

        private static string UI_LayerTime = "UI_CustomRecovery_LeftTime";
        private static string UI_LayerChance = "UI_CustomRecovery_Chance";
        private static string UI_LayerItem = "UI_CustomRecovery_Item";
        
        private static List<string> ShakeEffects = new List<string>
        {
            "assets/prefabs/tools/jackhammer/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/doubleshotgun/effects/attack_shake.prefab",
            "assets/prefabs/weapons/hatchet/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/rock/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/smg/effects/attack_shake.prefab",
            "assets/prefabs/weapons/torch/effects/strike_screenshake.prefab"
        };

        #endregion

        #region Data

        [JsonProperty("Количество предметов для подъёма у игроков")]
        private static Dictionary<ulong, int> PlayerRecovery = new Dictionary<ulong, int>();
        
        #endregion

        #endregion

        #region Initialization

        protected override void LoadDefaultConfig()
        {
            GetConfig("Общие настройки", "Отключить состояние 'ранен' (Автоматическая смерть)", ref CONF_DisableWound);                    
            GetConfig("Общие настройки", "Отключить возможность случайно встать после ранения?", ref CONF_DisableWoundEnd);
            GetConfig("Общие настройки", "Заставлять игрока кричать при ранении?", ref CONF_EnableScream);
            
             GetConfig("Основные настройки ранения", "Отображать шанс встать по окончанию ранения", ref CONF_ShowEndWoundTime);
            GetConfig("Основные настройки ранения", "Отображать оставшееся время до окончания ранения", ref CONF_ShowEndWoundTime);
            GetConfig("Основные настройки ранения", "Изменить время ранения? (Должно быть меньше 40)", ref CONF_WoundTime);
            GetConfig("Основные настройки ранения", "Изменить стандартный шанс встать после ранения? (0 - 100)", ref CONF_WoundEndChance);
            //GetConfig("Основные настройки ранения", "Шанс упасть от любой пули", ref CONF_WoundFromDamageChance);
            GetConfig("Основные настройки ранения", "Дополнительный шанс встать, при метаболизме > 250", ref CONF_StopWoundFromMetabolism);
            GetConfig("Основные настройки ранения", "Отдельные шансы встать по привилегии", ref CONF_CustomChances);
            
            GetConfig("Расширенные настройки ранения", "Разрешить поднимать игроков уколом шприца?", ref CONF_CanEndWoundBySyringe);
            GetConfig("Расширенные настройки ранения", "Включить отрицательные эффекты при подъеме шприцом / предметом", ref CONF_ShakeAfterWoundEnd);
            //GetConfig("Расширенные настройки ранения", "Отображать текст при окончании ранения, ключ - рамки нехватаюших очков, значение - сообщение", ref CONF_CustomMessages);
            
            GetConfig("Поднимающий предмет", "Разрешить крафтить дефибриллятор, который позволяет досрочно встать", ref CONF_CanCraftSpecialItem);
            GetConfig("Поднимающий предмет", "Стандартное количество 'дефибрилляторов' для нового игрока", ref CONF_DefaultDef);
            GetConfig("Поднимающий предмет", "Предметы необходимые для крафта дефибриллятора", ref CONF_SpecialItemReceipt);
            GetConfig("Поднимающий предмет", "Дополнительное изображение для дефибриллятора", ref CONF_PictureURL);
            
            SaveConfig();
        }

        private void OnServerInitialized()
        {		
            instance = this;
            LoadDefaultConfig();
            
            if (CONF_PictureURL != "")
                ImageLibrary.Call("AddImage", CONF_PictureURL, "UI_CW_Custom");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("PlayerItems"))
                PlayerRecovery = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("PlayerItems");
            
            foreach (var check in CONF_CustomChances)
            {
                PrintWarning($"Зарегистрировали {check.Key}");
                permission.RegisterPermission(check.Key, this);
            }
            
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            SaveData();
        }

        #endregion

        #region Hooks
        
        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            player.GetComponent<PlayerRecover>()?.DisableObject(false);
            return;
        }

        private void OnPlayerRespawn(BasePlayer player)
        {
            player.GetComponent<PlayerRecover>()?.DisableObject(false);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            player.GetComponent<PlayerRecover>()?.DisableObject(false);
        }
        
        private void OnPlayerRecover(BasePlayer player)
        {
            NextTick(() =>
            {
                if (player != null && !player.IsNpc && player.GetComponent<NPCPlayer>() == null && player.GetComponent<PlayerRecover>() != null && !player.IsDead())
                    player.GetComponent<PlayerRecover>().DisableObject(false);
            });
            return;
        }
        
        private void OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (tool.ShortPrefabName == "syringe_medical.entity")
            {
                var healingPlayer = tool.GetOwnerPlayer();
                if (healingPlayer != null && healingPlayer.IsWounded())
                {
                    player.ChatMessage($"Игрок поднял вас <color=#FF5733>медицинским шприцом</color>");
                    player.StopWounded();
            
                    player.GetComponent<PlayerRecover>()?.DisableObject(false);
                    if (CONF_ShakeAfterWoundEnd)
                    {
                        StartShake(player, 0);
                    }
                }
            }
        }

        private void StartShake(BasePlayer player, float amount)
        {
            if (Math.Abs(amount - 0.25f * 100) < 0.5 || player.IsDead())
                return;
            
            Effect effect = new Effect(ShakeEffects.GetRandom(), player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);
            amount += 0.25f;

            timer.Once(0.25f, () => StartShake(player, amount));
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!PlayerRecovery.ContainsKey(player.userID))
                PlayerRecovery.Add(player.userID, CONF_DefaultDef);
        }

        private void OnPlayerWound(BasePlayer player)
        {
            if (player == null || player.IsNpc || player.GetComponent<NPCPlayer>() != null)
                return;
            
            NextTick(() =>
            {
                if (player.IsDead())
                    return;
                
                if (CONF_DisableWound)
                {
                    player.Die();
                }
                else
                {
                    if (player.GetComponent<PlayerRecover>() == null)
                        player.gameObject.AddComponent<PlayerRecover>();
                }
            });
            return;
        }

        private void Unload()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<PlayerRecover>())
                UnityEngine.Object.Destroy(obj);
            
            SaveData();
        }

        #endregion

        #region Functions

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("PlayerItems", PlayerRecovery);
            timer.Once(300, SaveData);
        }

        private int GetWakeChance(BasePlayer player)
        {
            int result = CONF_WoundEndChance;
            foreach (var check in CONF_CustomChances.OrderByDescending(p => p.Value))
            {
                if (permission.UserHasPermission(player.UserIDString, check.Key))
                {
                    result = check.Value;
                    break;
                }
            }

            if (player.metabolism.calories.value > 250)
                result += CONF_StopWoundFromMetabolism;
            
            return result;
        }

        #endregion

        #region GUI

        private static void UI_DrawWakeChances(BasePlayer player)
        {
            PlayerRecover playerRecover = player.GetComponent<PlayerRecover>();
            if (playerRecover != null)
            {
                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, UI_LayerChance);
                int wakeChance = playerRecover.endChance;
            
                container.Add(new CuiElement
                {
                    Parent = "Overlay",
                    Name = UI_LayerChance,
                    Components =
                    {
                        new CuiImageComponent { FadeIn = 1f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.7" },
                        new CuiRectTransformComponent { AnchorMin = "0.447916 0.3138872", AnchorMax = "0.447916 0.3138872", OffsetMax = "133.3333 82.6666" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = UI_LayerChance,
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 1f, Text = wakeChance.ToString() + "%", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 60 },
                        new CuiRectTransformComponent { AnchorMin = "0 0.06799136", AnchorMax = "1 1.153219" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = UI_LayerChance,
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 1f, Text = "ШАНС ВСТАТЬ", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, FontSize = 16 },
                        new CuiRectTransformComponent { AnchorMin = "0 0.04034095", AnchorMax = "1 0.3145292" }
                    }
                });
                
                CuiHelper.AddUi(player, container);
            }
        }

        private static void UI_DrawLeftTime(BasePlayer player)
        {
            PlayerRecover playerRecover = player.GetComponent<PlayerRecover>();
            if (playerRecover != null)
            {
                int leftTime = (int) (CONF_WoundTime - player.secondsSinceWoundedStarted);
                CuiElementContainer container = new CuiElementContainer();
                if (!playerRecover.timeDrawed)
                {
                    CuiHelper.DestroyUi(player, UI_LayerTime);
                
                    container.Add(new CuiElement
                    {
                        Parent = "Overlay",
                        Name = UI_LayerTime,
                        Components =
                        {
                            new CuiImageComponent { FadeIn = 1f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.7" },
                            new CuiRectTransformComponent { AnchorMin = "0.4479166 0.1212941", AnchorMax = "0.4479166 0.1212941", OffsetMax = "133.3333 133.3333" }
                        }
                    });
                    
                    container.Add(new CuiElement
                    {
                        Name = UI_LayerTime + ".LeftTime",
                        Parent = UI_LayerTime,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = 1f, Text = leftTime.ToString(), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 80 },
                            new CuiRectTransformComponent { AnchorMin = "0 0.1650116", AnchorMax = "1 1.035" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = UI_LayerTime,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = 1f, Text = "СЕКУНД", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, FontSize = 25 },
                            new CuiRectTransformComponent { AnchorMin = "0 -0.07499988", AnchorMax = "1 0.4250001" }
                        }
                    });

                    playerRecover.timeDrawed = true;
                }
                else
                {
                    CuiHelper.DestroyUi(player, UI_LayerTime + ".LeftTime");
                    container.Add(new CuiElement
                    {
                        Name = UI_LayerTime + ".LeftTime",
                        Parent = UI_LayerTime,
                        Components =
                        {
                            new CuiTextComponent { FadeIn = 1f, Text = leftTime.ToString(), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 80 },
                            new CuiRectTransformComponent { AnchorMin = "0 0.1650116", AnchorMax = "1 1.035" }
                        }
                    });
                }
                
                CuiHelper.AddUi(player, container);
            }
        }

        private static void UI_DrawItemRecovery(BasePlayer player)
        {
            PlayerRecover playerRecover = player.GetComponent<PlayerRecover>();
            if (playerRecover != null)
            {
                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, UI_LayerItem);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.8369792 0.6129634", AnchorMax = "0.8369792 0.6129634", OffsetMax = "200 284.66666" },
                    Image = { Color = "0 0 0 0" }
                }, "Overlay", UI_LayerItem); 
                
                container.Add(new CuiElement
                {
                    Parent = UI_LayerItem,
                    Components =
                    {
                        new CuiImageComponent { FadeIn = 1f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.7" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Name = UI_LayerItem + ".Avatar",
                    Parent = UI_LayerItem,
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) instance.ImageLibrary.Call("GetImage", "UI_CW_Custom") },
                        new CuiRectTransformComponent { AnchorMin = "0 0.2963888", AnchorMax = "1 1", OffsetMin = "15 15", OffsetMax = "-15 -15" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = UI_LayerItem + ".Avatar",
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 1f, Text = $"Осталось: {PlayerRecovery[player.userID]} шт.", Font = "robotocondensed-regular.ttf", Align = TextAnchor.LowerRight, FontSize = 10 },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = UI_LayerItem,
                    Components =
                    {
                        new CuiTextComponent { FadeIn = 1f, Text = "Моментально поставит вас на ноги", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, FontSize = 16 },
                        new CuiRectTransformComponent { AnchorMin = "0.03 0.09833303", AnchorMax = "0.97 0.3583333" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.02500007", AnchorMax = "1 0.1250005", OffsetMin = "7 0", OffsetMax = "-7 0" },
                    Button = { Color = "1 1 1 1", Command = "UI_CW_Handler recoverUser" },
                    Text = { Text = "ИСПОЛЬЗОВАТЬ", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" }
                }, UI_LayerItem);

                CuiHelper.AddUi(player, container);
            }
        }

        #endregion

        #region Commands

        [ChatCommand("recover")]
        private void cmdChatRecover(BasePlayer player, string command, string[] args)
        {
            if (!CONF_CanCraftSpecialItem)
                return;
            if (args.Length == 0)
            {
                string message = "Предметы необходимые для крафта <color=#FF5733>дефибриллятора</color>:";
                foreach (var check in CONF_SpecialItemReceipt)
                    message += $"\n - {ItemManager.FindItemDefinition(check.Key).displayName.english}: {check.Value} шт.";
                message += $"\n/recover craft - скрафтить <color=#FF5733>дефибриллятор</color>";
                
                player.ChatMessage(message);
            }
            else if (args[0].ToLower() == "craft")
            {
                foreach (var check in CONF_SpecialItemReceipt)
                {
                    int currentAmount = player.inventory.GetAmount(ItemManager.FindItemDefinition(check.Key).itemid);
                    if (currentAmount < check.Value)
                    {
                        player.ChatMessage($"Вам не хватает: {ItemManager.FindItemDefinition(check.Key).displayName.english}: {check.Value - currentAmount} шт.");
                        return;
                    }
                }

                foreach (var check in CONF_SpecialItemReceipt)
                {
                    player.inventory.Take(null, ItemManager.FindItemDefinition(check.Key).itemid, check.Value);
                }

                player.ChatMessage($"Вы успешно скрафтили новый <color=#FF5733>дефибриллятор</color>!\n" +
                                   $"Новое количество: {++PlayerRecovery[player.userID]} шт.");
            }
        }
        
        [ConsoleCommand("UI_CW_Handler")]
        private void consoleHandler(ConsoleSystem.Arg args)
        {    
            BasePlayer player = args.Player();
            if (player == null)
                return;

            if (!args.HasArgs(1))
                return;

            if (args.Args[0].ToLower() == "recoveruser")
            {
                int playerLeft = PlayerRecovery[player.userID];
                if (playerLeft == 0)
                {
                    player.ChatMessage($"У вас <color=#FF5733>закончились</color> дефибрилляторы\n" +
                                       $"Скрафтить их вы можете при помощи: <color=#FF5733>/recover</color>");
                    return;
                }

                player.ChatMessage($"Вы успешно применили <color=#FF5733>дефибриллятор</color>\n" +
                                   $"У вас осталось: <color=#FF5733>{--PlayerRecovery[player.userID]} шт.</color>");
                player.StopWounded();
                
                player.GetComponent<PlayerRecover>()?.DisableObject(false);
                if (CONF_ShakeAfterWoundEnd)
                {
                    StartShake(player,  0);    
                }
            }
        }

        [ConsoleCommand("cw")]
        private void cmdAdminCommnand(ConsoleSystem.Arg args)
        { 
            if (args.Player() != null)
                return;
            if (!args.HasArgs(2))
            {
                PrintError($"Используйте команду правильно: cw <steamId> <amount>");
                return;
            }

            ulong targetId;
            if (!ulong.TryParse(args.Args[0], out targetId))
            {
                PrintError("Вы указали не ID в первом аргументе");
                return;
            }

            int amount;
            if (!int.TryParse(args.Args[1], out amount))
            {
                PrintError("Вы указали не число во втором аргументе!");
                return;
            }

            if (!PlayerRecovery.ContainsKey(targetId))
            {
                PrintError($"Игрок с указанным ID не найден!");
                return;
            }

            PlayerRecovery[targetId] += amount;
            PrintWarning($"Число д-в для игрока изменено. Новое количество: {PlayerRecovery[targetId]}");
        }

        #endregion

        #region Utils
        
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

        private void GetConfig<T>(string menu, string key, ref T varObject)
        {
            if (Config[menu, key] != null)
            {
                varObject = Config.ConvertValue<T>(Config[menu, key]);
            }
            else
            {
                Config[menu, key] = varObject;
            }
        }

        #endregion
    }
}