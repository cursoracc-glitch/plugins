using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GradeDestroy", "Hougan", "1.2.0")]
    public class GradeDestroy : RustPlugin
    {
        #region Classes

        private class CurrentGrade
        {
            [JsonProperty("Текущий индекс улучшения")]
            public int Grade = 0;
            [JsonProperty("Время до де-активации")]
            public int DeActivateTime = 40;

            [JsonProperty("Таймер обновления")]
            public Timer DeTimer = null;

            public void UpdateTime(BasePlayer player, int time)
            {
                DeActivateTime = time;
                DeTimer?.Destroy();
                instance.UpdateTimer(player);
            }
            
            public void UpGrade(BasePlayer player, int time)
            {
                if (Grade >= 0 && Grade < 4)
                {
                    UpdateTime(player, time);
                    Activate(player, Grade + 1, CONF_RemoveDefaultTime);
                }
                else if (Grade >= 4)
                {
                    DeActivate(player);
                }
                else
                {
                    player.ChatMessage($"Неизвестная ошибка. Сообщите адмнистрации: <color=#81B67A>{Grade}</color>");
                    return;
                }
            }

            public void Activate(BasePlayer player, int grade, int time)
            {
                if (grade != Grade && instance.Initializated())
                {
                    player.ChatMessage($"Вы успешно активировали режим: <color=#81B67A>{CONF_GradeNames[grade]}</color>");
                }
                
                this.Grade = grade;
                this.DeActivateTime = time;
                instance.UpdateTimer(player);
            }

            public void DeActivate(BasePlayer player)
            {
                Grade = 0;
                Activate(player, 0, 0);
            } 
            
        }

        #endregion

        #region Variables

        #region Config

        #region Remove

        private static string CONF_RemovePermission = "GradeDestroy.Remove";
        private static bool CONF_RemoveActivated = true;
        private static int CONF_RemoveTime = 14400;
        private static int CONF_RemoveDefaultTime = 60;
        private static int CONF_RemoveHitTime = 5;
        private static bool CONF_RemoveGameFriends = false;
        private static bool CONF_RemoveFriends = false;
        private static bool CONF_RemoveClans = false;
        private static bool CONF_RemoveByCup = false;
        private static bool CONF_RemoveOneHit = false;

        private static bool CONF_BlockRemoveOnRaid = true;

        #endregion

        #region Upgrade

        private static bool CONF_UpActivated = true;
        private static string CONF_UpPermission = "GradeDestroy.Up";
        private static bool CONF_BlockUpgradeOnRaid = true;
        private static bool CONF_EnableOnHit = false;

        #endregion

        #region Other

        private static List<string> CONF_GradeNames = new List<string>
        {
            "отключено",
            "улучшение в дерево",
            "улучшение в камень",
            "улучшение в метал",
            "улучшение в МВК",
            "удаление построек"
        };

        #endregion
        
        #endregion

        #region System
        
        [PluginReference] private Plugin Friends, Clans, NoEscape;
        private string Layer = "UI.Remove";
        private static GradeDestroy instance;

        // Список объектов с кулдаунами до удаления
        Dictionary<uint, double> removeTimers = new Dictionary<uint, double>();
        // Информация об игроках
        private Dictionary<ulong, CurrentGrade> playerGrades = new Dictionary<ulong,CurrentGrade>();

        #endregion


        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            instance = this;
            LoadDefaultConfig();

            permission.RegisterPermission(CONF_RemovePermission, this);
            permission.RegisterPermission(CONF_UpPermission, this);

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("GradeDestroy/Objects") && Initializated())
                removeTimers =
                    Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, double>>("GradeDestroy/Objects");

            BasePlayer.activePlayerList.ForEach(OnPlayerInit);

            List<uint> removeList = new List<uint>();
            foreach (var check in removeTimers)
            {
                if (check.Value < LogTime())
                    removeList.Add(check.Key);
            }

            PrintError($"Удалено {removeList.Count} объектов");
            foreach (var check in removeList)
                removeTimers.Remove(check);

            timer.Every(300, SaveData);
        }

        private void Unload()
        {
            SaveData();
            BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, Layer));
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("GradeDestroy/Objects", removeTimers);
        }
        
        protected override void LoadDefaultConfig()
        {
            GetConfig("Удаление построек", "Разрешать игрокам удалять постройки", ref CONF_RemoveActivated);
            GetConfig("Удаление построек", "Разрешение для удаления построек", ref CONF_RemovePermission);
            GetConfig("Удаление построек", "Лимит времени для удаления (0 - бесконечно)", ref CONF_RemoveTime);
            GetConfig("Удаление построек", "Разрешить удалять постройки друзей", ref CONF_RemoveFriends);
            GetConfig("Удаление построек", "Разрешить удалять постройка со-кланов", ref CONF_RemoveClans);
            GetConfig("Удаление построек", "Разрешить удалять при авторизации в шкафу", ref CONF_RemoveByCup);
            GetConfig("Удаление построек", "Запрещать удалять во время рейда", ref CONF_BlockRemoveOnRaid);
            GetConfig("Удаление построек", "Моментальный ремув объекта", ref CONF_RemoveOneHit);
            
            GetConfig("Улучшение построек", "Разрешать игрокам улучшать постройки", ref CONF_UpActivated);
            GetConfig("Улучшение построек", "Разрешение для улучшения построек", ref CONF_UpPermission);
            GetConfig("Улучшение построек", "Время автоматического включения улучшения", ref CONF_RemoveHitTime);
            GetConfig("Улучшение построек", "Включать режим автоматического улучшения при ручном улучшения", ref CONF_EnableOnHit);
            GetConfig("Улучшение построек", "Запрещать улучшать во время рейда", ref CONF_BlockUpgradeOnRaid);
            

            GetConfig("Остальное", "Отображение имён улучшения", ref CONF_GradeNames);
            
            Config.Save();
        }

        #endregion

        #region Functions

        private string TryRemove(BasePlayer player, BaseEntity entity)
        {
            if (!player.CanBuild() || !Initializated())
            {
                return "Вы не можете удалять постройки на <color=#81B67A>чужой территории</color>!";
            }

            if (entity is DecayEntity && !(entity is BuildingBlock))
            {
                return "Вы можете удалять <color=#81B67a>только конструкции</color>";
            }
            
            if (CONF_RemoveTime != 0 && removeTimers.ContainsKey(entity.net.ID))
            {
                
                if (removeTimers[entity.net.ID] <= LogTime())
                {
                    removeTimers.Remove(entity.net.ID);
                    return "Вышло время удаления данного объекта!";
                }
            }
            else if (CONF_RemoveTime != 0 && !removeTimers.ContainsKey(entity.net.ID))
            {
                return "Вышло время удаления данного объекта!";
            }
            
            if (CONF_BlockRemoveOnRaid && NoEscape)
            {
                var blockTime = (bool) (NoEscape?.Call("IsRaidBlocked", player) ?? false);
                if (blockTime)
                {
                    return "Вы не можете удалять постройки <color=#81B67A>во время рейда</color>!";
                }
            }

            bool isOwner = entity.OwnerID == player.userID;
            bool areFriends = (bool) ((Friends?.Call("AreFriends", player.userID, entity.OwnerID) ?? false));
            List<string> areClanMates = (List<string>) (Clans?.Call("GetClanMembers", player.userID) ?? new List<string>());
            
            if (CONF_RemoveByCup == true || (CONF_RemoveFriends && areFriends) || (CONF_RemoveClans && areClanMates.Contains(entity.OwnerID.ToString())) || isOwner)
            {
                return "";
            }
            else
            {
                return "Вы можете удалять свои постройки, а также постройки друзей!";
            }

            return "";
        }

        private string TryUpgrade(BasePlayer player, BuildingBlock block, CurrentGrade currentGrade)
        {
            if (currentGrade.Grade <= (int) block.lastGrade || !Initializated())
                return "";

            if (CONF_BlockUpgradeOnRaid && NoEscape)
            {
                var blockTime = (bool) (NoEscape?.Call("IsRaidBlocked", player) ?? false);
                if (blockTime)
                {
                    currentGrade.DeActivate(player);
                    return "Вы не можете авто-улучшать постройки <color=#81B67A>во время рейда</color>!";
                }
            }
            
            if (!player.CanBuild())
            {
                currentGrade.DeActivate(player);
                return "Вы находитесь на чужой территории, автоматическое улучшение <color=#81B67A>отключено</color>!";
            }
            
            if (block.SecondsSinceAttacked < 30)
            {
                return $"Это объект можно будет улучшить через <color=#81B67A>{FormatTime(TimeSpan.FromSeconds(30 - (int) block.SecondsSinceAttacked), maxSubstr:2)}</color>";
            }
            
            foreach (var check in block.blockDefinition.grades[currentGrade.Grade].costToBuild)
            {
                if (player.inventory.GetAmount(check.itemid) < check.amount)
                {
                    CuiHelper.DestroyUi(player, Layer);
                    currentGrade.DeActivate(player);
                    return "У вас не хватает ресурсов для автоматического улучшения!";
                }
            }
            foreach (var check in block.blockDefinition.grades[currentGrade.Grade].costToBuild)
            {
                player.inventory.Take(null, check.itemid, (int) check.amount);
                player.Command("note.inv", check.itemid, check.amount * -1f);
            }
            block.SetGrade((BuildingGrade.Enum) currentGrade.Grade);
            block.UpdateSkin();
            block.SetHealthToMax();
            block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            currentGrade.UpdateTime(player, CONF_RemoveDefaultTime);
            
            return "";
        }

        private void UpdateTimer(BasePlayer player)
        {
            if (!playerGrades.ContainsKey(player.userID) || !Initializated())
                playerGrades.Add(player.userID, new CurrentGrade());

            CurrentGrade currentGrade = playerGrades[player.userID];
            if (currentGrade.Grade == 0)
            {
                CuiHelper.DestroyUi(player, Layer);
                return;
            }
            
            CuiHelper.DestroyUi(player, Layer);
            
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.4359375 0.1148148", AnchorMax = "0.55 0.1379629", OffsetMax = "0 0 " },
                Image = { Color = HexToRustFormat("#81B67B3C"), Sprite = "assets/content/ui/ui.background.tile.psd"},
            }, "Hud", Layer);
            container.Add(new CuiLabel
            {
                Text = { Text = $"{CONF_GradeNames[currentGrade.Grade].ToUpper()}: {currentGrade.DeActivateTime} СЕК.", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, Layer);

            CuiHelper.AddUi(player, container);
            
            currentGrade.DeActivateTime--;
            if (currentGrade.DeActivateTime < 0)
            {
                currentGrade.DeActivate(player);
                CuiHelper.DestroyUi(player, Layer);
                return;
            }
            
            currentGrade.DeTimer?.Destroy();
            currentGrade.DeTimer = timer.Once(1, () => UpdateTimer(player));
        }

        #endregion

        #region Commands

        [ChatCommand("remove")]
        private void cmdChatRemove(BasePlayer player, string command, string[] args)
        {
            if (!CONF_RemoveActivated)
                return;
            
            if (!permission.UserHasPermission(player.UserIDString, CONF_RemovePermission))
            {
                SendReply(player, "У вас <color=#81B67a>недостаточно</color> прав для использования этой команды!");
                return;
            }
            
            OnPlayerInit(player);
            CurrentGrade currentGrade = playerGrades[player.userID];

            if (currentGrade.Grade == 5)
            {
                currentGrade.DeActivate(player);
                return;
            }
            else
            {
                currentGrade.Activate(player, 5, CONF_RemoveDefaultTime);
            }
        }

        [ChatCommand("up")]
        private void cmdChatUpgrade(BasePlayer player, string command, string[] args)
        {
            if (!CONF_UpActivated)
                return;
            
            if (!permission.UserHasPermission(player.UserIDString, CONF_UpPermission))
            {
                SendReply(player, "У вас <color=#81B67a>недостаточно</color> прав для использования этой команды!");
                return;
            }
            
            OnPlayerInit(player);
            CurrentGrade currentGrade = playerGrades[player.userID];

            switch (args.Length)
            {
                case 0:
                {
                    currentGrade.UpGrade(player, CONF_RemoveDefaultTime);
                    break;
                }
                case 1:
                {
                    int newGrade;
                    if (!int.TryParse(args[0], out newGrade) || newGrade < 0 || newGrade > 4)
                    {
                        SendReply(player, $"Мы <color=#81B67A>не смогли</color> распознать введеный уровень улучшения!\n" +
                                                $"Возможные варианты:\n" +
                                                $"\n" +
                                                $"[<color=#81B67A>0</color>] -> Отключить\n" +
                                                $"[<color=#81B67A>1</color>] -> В дерево\n" +
                                                $"[<color=#81B67A>2</color>] -> В камень\n" +
                                                $"[<color=#81B67A>3</color>] -> В метал\n" +
                                                $"[<color=#81B67A>4</color>] -> В МВК");
                        return;
                    }
                    currentGrade.Activate(player, newGrade, CONF_RemoveDefaultTime);
                    break;
                }
                default:
                {
                    cmdChatUpgrade(player, command, new string[] { });
                    return;
                }
            }
        }

        #endregion

        #region Hooks

        
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null)
                return;
            
            if (!playerGrades.ContainsKey(player.userID))
                playerGrades.Add(player.userID, new CurrentGrade());
            
            CurrentGrade currentGrade = playerGrades[player.userID];
            
            
            BuildingBlock block = go.ToBaseEntity().GetComponent<BuildingBlock>();
            if (block == null)
                return;
            
            if (CONF_RemoveTime != 0)
                removeTimers.Add(block.net.ID, LogTime() + CONF_RemoveTime);
            
            if (currentGrade.Grade <= 0 || currentGrade.Grade > 4)
                return;

            var result = TryUpgrade(player, block, currentGrade);
            if (result != "")
            {
                player.ChatMessage(result);
                return;
            }
        }
        
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (info == null)
                return;
            
            if (!playerGrades.ContainsKey(player.userID))
                playerGrades.Add(player.userID, new CurrentGrade());
            
            CurrentGrade currentGrade = playerGrades[player.userID];

            if (currentGrade.Grade == 5)
            {
                if (info.HitEntity is DecayEntity || info.HitEntity is BaseOven || info.HitEntity is BuildingBlock || info.HitEntity is SimpleBuildingBlock)
                {
                    var tryRemoveResult = TryRemove(player, info.HitEntity);
                    if (tryRemoveResult != "")
                        SendReply(player, tryRemoveResult);
                    else
                    {
                        NextTick(() =>
                        {
                            if (info.HitEntity.GetComponent<StorageContainer>() != null)
                            {
                                info.HitEntity.GetComponent<StorageContainer>().DropItems();
                            }
                            if (CONF_RemoveOneHit)
                                info.HitEntity.GetComponent<BaseCombatEntity>().Kill();
                            else
                                info.HitEntity.GetComponent<BaseCombatEntity>().Hurt(2500);
                        });
                    }
                }
            }
            else if (currentGrade.Grade != 0)
            {
                if (info.HitEntity is BuildingBlock)
                {
                    BuildingBlock block = info.HitEntity as BuildingBlock;
                    
                    var tryUpgradeRsult = TryUpgrade(player, block, currentGrade);
                    if (tryUpgradeRsult != "")
                        SendReply(player, tryUpgradeRsult);
                }
            }
            else
            {
                if (TryRemove(player, info.HitEntity) == "")
                {
                    if (info.HitEntity is BuildingBlock)
                    {
                        BuildingBlock block = info.HitEntity as BuildingBlock;
                        if (removeTimers.ContainsKey(block.net.ID) && player.SecondsSinceAttacked > 10)
                        {
                            double leftRemove = removeTimers[block.net.ID] - LogTime();
                            if (leftRemove < 0)
                            {
                                SendReply(player, "Время удаления данного объекта <color=#81B67a>закончилось</color>!");
                            }
                            else
                            {
                                SendReply(player, $"Через <color=#81B67A>{FormatTime(TimeSpan.FromSeconds(leftRemove), maxSubstr:2)}</color> вы не сможете удалить этот объект!");
                            }
                            player.Hurt(0);
                            return;
                        }
                    }
                }
                return;
            }
        }
        
        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (!playerGrades.ContainsKey(player.userID))
                playerGrades.Add(player.userID, new CurrentGrade());

            CurrentGrade currentGrade = playerGrades[player.userID];

            if (CONF_EnableOnHit && permission.UserHasPermission(player.UserIDString, CONF_UpPermission))
            {
                currentGrade.Activate(player, (int) grade, CONF_RemoveHitTime);
            }

            if (player.SecondsSinceAttacked > 10)
            {
                SendReply(player, $"Вы можете удалить построенный объект в течении <color=#81B67A>{FormatTime(TimeSpan.FromSeconds(CONF_RemoveTime), maxSubstr:2)}</color>");
                player.Hurt(0);
            }
            return null;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!playerGrades.ContainsKey(player.userID))
                playerGrades.Add(player.userID, new CurrentGrade());
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
        
        public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
        {
            string result = string.Empty;
            switch (language)
            {
                case "ru":
                    int i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";
                        
                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")}";
                        i++;
                    }

                        break;
                case "en":
                    result = string.Format( "{0}{1}{2}{3}",
                        time.Duration().Days > 0 ? $"{time.Days:0} day{( time.Days == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Hours > 0 ? $"{time.Hours:0} hour{( time.Hours == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{( time.Minutes == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{( time.Seconds == 1 ? String.Empty : "s" )}" : string.Empty );

                    if (result.EndsWith( ", " )) result = result.Substring( 0, result.Length - 2 );

                    if (string.IsNullOrEmpty( result )) result = "0 seconds";
                    break;
            }
            return result;
        }
        
        public bool Initializated()
        {
            int result = 0;
            foreach (var check in Author.ToCharArray())
                result += (int) check;
            return result == 610 ? true : false;
        }
      
        private static string Format(int units, string form1, string form2, string form3 )
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        
        private bool GetConfig<T>(string mainMenu, string key, ref T var)
        {
            if (Config[mainMenu, key] != null)
            {
                var = Config.ConvertValue<T>(Config[mainMenu, key]);
                return false;
            }

            Config[mainMenu, key] = var;
            return true;
        }
        
        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static double LogTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion
    }
}