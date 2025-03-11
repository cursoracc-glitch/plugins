using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{  
    [Info("Foundation Drop", "Server-rust.ru", "0.0.81")]
    public class FoundationDrop : RustPlugin
    {
        #region References

        [PluginReference] private Plugin Backpack;

        #endregion
        
        #region Classes

        private class Event
        {
            public double StartTime;
            public bool Started = false;
            public bool Finished = false;
            public int Received = 0;
            

            public Timer StartTimer;
            public Timer DestroyTimer;
            
            public Dictionary<ulong, Vector3> PlayerConnected = new Dictionary<ulong, Vector3>();
            public List<List<BaseEntity>> BlockList = new List<List<BaseEntity>>();
            public int EventHashID = 12;

            public void JoinEvent(BasePlayer player)
            {
                if (Started)
                {
                    SendMessage(player, $"Мероприятие уже началось, вы <color=#538fef>не успели</color>!");
                    return;
                }
                else
                {
                    if (PlayerConnected.ContainsKey(player.userID))
                    {
                        SendMessage(player, $"Вы уже участник мероприятия!");
                        return;
                    }
                    if (player.inventory.AllItems().Length != 0)
                    {
                        SendMessage(player, $"На мероприятие можно попасть только <color=#538fef><b>полностью голым</b></color>");
                        return;
                    }
                    
                    CuiHelper.DestroyUi(player, CONF_UI_MainLayer);
                    player.inventory.Strip();
                    player.SendNetworkUpdate();
                    
                    PlayerConnected.Add(player.userID, player.transform.position);
                    ClearTeleport(player, EventPosition + new Vector3(0, 5, 0));
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
                    
                    UI_DrawInfo(player);
                }
            }

            public void LeftEvent(BasePlayer player, bool external = false)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
                player.inventory.Strip();
                player.StartSleeping();
                
                ClearTeleport(player, PlayerConnected[player.userID]);

                PlayerConnected.Remove(player.userID);
                if ((PlayerConnected.Count == 0 || Finished) && !external)
                {
                    if (Received < instance.config.CONF_MaxWinners)
                    {
                        SendMessage(player, $"Вы стали <b><color=#538fef>победителем</color></b> этого мероприятия!");
                        var finish = instance.config.RewardList.ToList().GetRandom();
                        
                        if (finish.Key.StartsWith("command"))
                        {
                            string workingPart = finish.Key.Split('+')[1];
                            instance.Server.Command(workingPart.Replace("%STEAMID%", player.UserIDString));
                        }
                        else if (finish.Key.StartsWith("item"))
                        {
                            string itemName = finish.Key.Split('+')[1];
                            int itemAmount = finish.Value;

                            Item prize = ItemManager.CreateByPartialName(itemName, itemAmount);
                            prize.MoveToContainer(player.inventory.containerMain);

                            player.Command("note.inv", new object[] {prize.info.itemid, prize.amount});
                            Received++;
                        }
                    }
                }
            }

            public void HandlePlayers()
            {
                if (Finished)
                    return;
                
                List<ulong> copyList = PlayerConnected.Keys.ToList();
                foreach (var check in copyList)
                {
                    BasePlayer target = BasePlayer.FindByID(check);
                    if (target == null)
                    {
                        PlayerConnected.Remove(check);
                    }
                    else
                    {
                        if (target.IsSwimming())
                            LeftEvent(target);
                    }
                }

                if (PlayerConnected.Count == 1)
                {
                    instance.StopEvent();
                    return;
                }

                StartTimer = instance.timer.Once(1, HandlePlayers);
            }

            public void StartEvent(int startDelay)
            {
                Interface.Oxide.LogWarning($"Foundation Drop announced!");
                BasePlayer.activePlayerList.ForEach(p => SendMessage(p, "Началась регистрация на мероприятие, нажмите на кнопку <b><color=#538fef>зарегистрироваться</color></b>"));
                StartTimer = instance.timer.Once(startDelay, () =>
                {
                    BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, CONF_UI_MainLayer));
                    BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, CONF_UI_SideLayer));
                    
                    List<ulong> removeKeys = new List<ulong>();
                    foreach (var check in PlayerConnected)
                    {
                        BasePlayer target = BasePlayer.FindByID(check.Key);
                        if (target != null)
                        {
                            CuiHelper.DestroyUi(target, CONF_UI_SideLayer);
                            continue;
                        }
                        else
                        {
                            removeKeys.Add(check.Key);
                        }
                    }

                    foreach (var check in removeKeys)
                        PlayerConnected.Remove(check);
                    
                    
                    if (PlayerConnected.Count <= 1)
                    {
                        instance.StopEvent(true);
                        return;
                    }
                    
                    BasePlayer.activePlayerList.ForEach(p =>
                    {
                        if (!PlayerConnected.ContainsKey(p.userID))
                            return;
                        
                        if (instance.config.CONF_GivePistol)
                        {
                            Item item = ItemManager.CreateByPartialName("pistol.m92", 1);
                            item.name = $"Опасное оружие";

                            item.GetHeldEntity().GetComponent<BaseProjectile>().primaryMagazine.capacity = instance.config.CONF_GivePistolAmount;
                            item.GetHeldEntity().GetComponent<BaseProjectile>().primaryMagazine.contents = instance.config.CONF_GivePistolAmount;
                        
                            item.MoveToContainer(p.inventory.containerBelt);
                        }
                    });
                    
                    instance.DropFoundation();
                    HandlePlayers();
                });
            }
            
            public void InitializeEvent(int startDelay)
            {
                Started = false;
                StartTime = CurrentTime() + startDelay;
                ServerMgr.Instance.StartCoroutine(instance.InitializeFoundation(startDelay));
            }

            public void FinishEvent(bool external = false)
            {
                Finished = true;

                StartTimer?.Destroy();
                DestroyTimer?.Destroy();
               
                BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, CONF_UI_MainLayer));
                BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, CONF_UI_SideLayer));
                BasePlayer.activePlayerList.ForEach(p => p.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false));
                var listPredicted = BasePlayer.activePlayerList.Where(p => cEvent.PlayerConnected.ContainsKey(p.userID));
                foreach (var check in listPredicted)
                    LeftEvent(check, external);
                
                foreach (var check in BlockList.SelectMany(p => p))
                    check?.Kill();

                string message = external ? "Мероприятие закончено по <b><color=#538fef>техническим причинам</color></b>!" : "Мероприятие окончено, всем <b><color=#538fef>спасибо</color></b> за участие!";
                BasePlayer.activePlayerList.ForEach(p => SendMessage(p, message)); 
            }
        }

        private class Configuration
        {
            [JsonProperty("Размер арены в квадратах")]
            public int CONF_ArenaSize = 10;
            [JsonProperty("Выдавать ли специальный пистолет для разрушения блоков")]
            public bool CONF_GivePistol = true;
            [JsonProperty("Количество патронов в пистолете")]
            public int CONF_GivePistolAmount = 1;
            [JsonProperty("Интвервал между удалениями блоков")]
            public float CONF_DelayDestroy = 0.1f;
            [JsonProperty("Время ожидания игроков с момента объявления ивента")]
            public int CONF_WaitTime = 30;
            [JsonProperty("Максимальное количество победителей")]
            public int CONF_MaxWinners = 1;
            
            [JsonProperty("Список наград (выбирается случайно)")]
            public Dictionary<string, int> RewardList;

            public static Configuration GetConfiguration()
            {
                return new Configuration()
                {
                    RewardList = new Dictionary<string, int>
                    {
                        ["item+rifle.ak"] = 1,
                        ["command+ownerid 76561190000000000"] = 1 
						/* 
						 * Пример: command+ownerid 76561190000000000 - Выдать права администратора
						 * Пример: command+oxide.grant user 76561190000000000 skin.box - Выдать права на использования команды /skin
						 *
						 */
                    }
                };
            }
        }

        #endregion
        
        #region Variables

        private Configuration config;
        
        #region System

        private static FoundationDrop instance;
        private static Vector3 EventPosition = new Vector3((float) World.Size/2, 10, (float) World.Size/2);
        private static Event cEvent = null;
        private static string CONF_UI_MainLayer = "UI_DF_Layer";
        private static string CONF_UI_SideLayer = "UI_DF_Layer";

        #endregion

        #endregion

        #region Initialization
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.RewardList == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.GetConfiguration();
        } 
        
        protected override void SaveConfig() => Config.WriteObject(config);
        
        private void OnServerInitialized()
        {
            instance = this;
        }

        private void Unload()
        {
            if (cEvent != null)
            {
                StopEvent(true);
            }
            
            BasePlayer.activePlayerList.ForEach(p => p.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false));
            
            foreach (var check in UnityEngine.Object.FindObjectsOfType<BaseEntity>().Where(p => p.name == "S23qRT").Where(p => !p.IsDestroyed))
                check?.Kill();
            
            BasePlayer.activePlayerList.ForEach(p =>
            {
                CuiHelper.DestroyUi(p, CONF_UI_MainLayer);
                CuiHelper.DestroyUi(p, CONF_UI_SideLayer);
            });
        }

        #endregion

        #region Hooks
        
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.cmd.FullName.ToLower().Contains("backpack.open") && arg.Player() != null && cEvent != null &&
                cEvent.PlayerConnected.ContainsKey(arg.Player().userID))
                return false;

            return null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
            {
                BasePlayer player = entity as BasePlayer;
                
                if (cEvent != null && cEvent.PlayerConnected.ContainsKey(player.userID))
                {
                    info.damageTypes.ScaleAll(0);
                    return false;
                }
            }

            return null;
        }

        private void OnPlayerDie(BasePlayer player)
        {
            if (cEvent != null && cEvent.PlayerConnected.ContainsKey(player.userID))
            {
                cEvent.LeftEvent(player);
                return;
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (cEvent != null && cEvent.PlayerConnected.ContainsKey(player.userID))
            {
                cEvent.LeftEvent(player);
                return;
            }
        }

        #endregion

        #region Commands

        [ConsoleCommand("UI_FE_Handler")]
        private void cmdConsoleHandler(ConsoleSystem.Arg args)
        {
            if (cEvent == null)
                return;
            
            if (args.HasArgs(1))
            {
                if (args.Args[0] == "join" && !cEvent.PlayerConnected.ContainsKey(args.Player().userID))
                {
                    cEvent.JoinEvent(args.Player());
                }
            }
        }
        
        [ConsoleCommand("fe.start")]
        private void cmdStartEvent(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
                return;

            StartEvent();
        }

        [ConsoleCommand("fe.stop")]
        private void cmdStopEvent(ConsoleSystem.Arg args)
        {                                             
            if (args.Player() != null)
                return;
            
            StopEvent(true);
        }

        #endregion
        
        #region Functions

        private static void SendMessage(BasePlayer player, string text)
        {
            player.ChatMessage($"<size=16>Мероприятие '<color=#538fef>падающие платформы</color>'</size>\n{text}");
        }

        private static void ClearTeleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
            {
                player.ClientRPCPlayer(null, player, "StartLoading");
            }
            player.StartSleeping();
            player.MovePosition(position);
            
            if (player.net?.connection != null)
            {
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            }
            
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null)
            {
                return;
            }
            player.SendFullSnapshot();
        }

        private void StartEvent()
        {
            if (cEvent != null)
            {
                PrintError("Попытка начать новое мероприятие, пока не закончено предыдущее!");
                return;
            }
            else
            {
                cEvent = new Event();
                cEvent.InitializeEvent(instance.config.CONF_WaitTime);
            }
        }

        private void StopEvent(bool external = false)
        {
            if (cEvent == null)
            {
                PrintError("Попытка отключить несуществующий ивент!");
                return;
            }
            else
            {
                BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, CONF_UI_MainLayer));
                BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, CONF_UI_SideLayer));
                BasePlayer.activePlayerList.ForEach(p => p.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false));
                cEvent.FinishEvent(external);
                cEvent = null;
            }
        }

        private IEnumerator InitializeFoundation(int startDelay)
        {
            for (int i = -instance.config.CONF_ArenaSize / 2; i < instance.config.CONF_ArenaSize / 2; i++)
            {
                for (int t = -instance.config.CONF_ArenaSize / 2; t < instance.config.CONF_ArenaSize / 2; t++)
                {
                    cEvent.BlockList.Add(new List<BaseEntity>());
                    for (int grade = 4; grade > 0; grade--)
                    {
                        var newFoundation = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", EventPosition + new Vector3(i * 3, grade * 1f, t * 3));
                        newFoundation.Spawn();
						newFoundation.name = "S23qRT";
                        
                        newFoundation.GetComponent<BuildingBlock>().SetGrade((BuildingGrade.Enum) grade);
                        cEvent.BlockList.Last().Add(newFoundation);
                        yield return i;
                    }
                }
            }
            cEvent.StartEvent(startDelay);

            foreach (var check in BasePlayer.activePlayerList)
            {
                UI_DrawInvite(check);
            }
        }

        private void DropFoundation()
        {
            if (cEvent.Finished)
            {
                return;
            }
            
            if (cEvent.BlockList.Count == 0)
            {
                StopEvent();
                return;
            }
            var cStack = cEvent.BlockList.GetRandom();
            if (cStack.Count == 0)
            {
                cEvent.BlockList.Remove(cStack);
                DropFoundation();
                return;
            }

            var cBlock = cStack.First();
            if (cBlock == null || cBlock.IsDestroyed)
            {
                cStack.RemoveAt(0);
                DropFoundation();
                return;
            }
            
            cStack.RemoveAt(0);
            cBlock.Kill();

            cEvent.DestroyTimer = timer.Once(instance.config.CONF_DelayDestroy, DropFoundation);
        }

        #endregion

        #region Interface
        
        private static void UI_DrawInfo(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, CONF_UI_SideLayer);
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiElement
            {
                Parent = "Overlay",
                Name = CONF_UI_SideLayer,
                Components =
                {
                    new CuiRawImageComponent()  { FadeIn = 0.3f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.9" },
                    new CuiRectTransformComponent  { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                }        
            });

            string text = $"<size=35><b><color=#538fefFF>ОЖИДАНИЕ НАЧАЛА ИГРЫ</color></b></size>\n" +
                          $"\n" +
                          $"<size=18>Главная задача мероприятия - выжить последним, либо уцелеть на последнем блоке\n" +
                          $"После начала игры блоки начнут разрушаться один за другим, если вы упадёте в воду - вы проиграете</size>";
            
            if (instance.config.CONF_GivePistol)
                text += "\n\n\n<size=16>После начала игры вы также получите пистолет с одним патроном, благодаря ему вы сможете разрушить одну\n" +
                        "платформу под ногами противника, советуем не тратить её в самом начале!</size>";
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { FadeIn = 0.3f, Text = text, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, CONF_UI_SideLayer);
            
            CuiHelper.AddUi(player, container);
            UI_DrawLeftTime(player);
        }

        private static void UI_DrawLeftTime(BasePlayer player)
        {
            if (cEvent == null || !cEvent.PlayerConnected.ContainsKey(player.userID))
            {
                CuiHelper.DestroyUi(player, CONF_UI_SideLayer);
            }
            else
            {
                CuiHelper.DestroyUi(player, CONF_UI_SideLayer + ".LeftTime");
            }
            
            CuiElementContainer container = new CuiElementContainer();

            int leftTime = (int) (cEvent.StartTime - CurrentTime());
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = leftTime.ToString(), Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-bold.ttf" }
            }, CONF_UI_SideLayer, CONF_UI_SideLayer + ".LeftTime");

            CuiHelper.AddUi(player, container);

            if (leftTime != 1)
            {
                instance.timer.Once(1, () => UI_DrawLeftTime(player));
            }
        }

        private void UI_DrawInvite(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, CONF_UI_MainLayer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.9994791 0.7222223", AnchorMax = "0.9994791 0.7222223", OffsetMin = "-244.6 -82" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", CONF_UI_MainLayer);
            
            container.Add(new CuiElement
            {
                Parent = CONF_UI_MainLayer,
                Components =
                {
                    new CuiImageComponent { FadeIn = 0.3f, Color = HexToRustFormat("#605F5332") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.6422766", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { FadeIn = 0.3f, Text = $"ПАДАЮЩИЕ ПЛАТФОРМЫ", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 15 }
            }, CONF_UI_MainLayer);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.005 0.1707318", AnchorMax = "0.995 0.8943092", OffsetMax = "0 0" },
                Text = { FadeIn = 0.3f, Text = $"Началась регистрация на мероприятие, чтобы принять участие вы должны быть голыми", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 11 }
            }, CONF_UI_MainLayer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.01226182 0.02845511", AnchorMax = "0.9854766 0.3414635", OffsetMax = "0 0" },
                Button = { Color = HexToRustFormat("#7C7C7C6E"), Command = "UI_FE_Handler join" },
                Text = { Text = "ЗАРЕГИСТРИРОВАТЬСЯ", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 16 }
            }, CONF_UI_MainLayer);

            CuiHelper.AddUi(player, container);
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
        
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        
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