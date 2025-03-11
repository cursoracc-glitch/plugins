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
    [Info("DVBuildingUpgrade", "ClayMond", "2.0.3")]
    public class DVBuildingUpgrade : RustPlugin
    {
        #region Classes
        private class CurrentGrade
        { 
            [JsonProperty("Текущий индекс улучшения")]
            public int Grade;
            [JsonProperty("Время до де-активации")]
            public int DeActivateTime = 40;

            [JsonProperty("Таймер обновления")]
            public Timer DeTimer;

            public void UpdateTime(BasePlayer player, int time)
            {
                DeActivateTime = time;
                DeTimer?.Destroy();
                instance.UpdateTimer(player);
            }
            
            public void UpGrade(BasePlayer player, int time)
            {
				if (player==null) return;
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
                    player.ChatMessage($"Неизвестная ошибка. Сообщите адмнистрации: <color=#F24525>{Grade}</color>");
                    return;
                }
            }

            public void Activate(BasePlayer player, int grade, int time)
            {
				if (player==null) return;
                if (grade != Grade)
                { 
                    player.ChatMessage($"");
                }
                
                Grade = grade;
                DeActivateTime = time;

                if (Grade > 0)
                {
                    CuiHelper.DestroyUi(player, instance.Layer);
                    
                    var container = new CuiElementContainer();

                    var image = grade < 5 ? "UpImage" : "RemoveImage";

                    container.Add(new CuiElement
                    {
                        Name = instance.Layer,
                        Parent = "Hud",
                        Components =
                        {   
                            new CuiRawImageComponent { Png = (string) instance.ImageLibrary.Call("GetImage", image), Color = HexToRustFormat("#F65050") },
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-184.3 673.95", OffsetMax = "-143 713.9"}
                        }
                    });

                    CuiHelper.AddUi(player, container);
                }
                else
                {
                    CuiHelper.DestroyUi(player, instance.Layer);
                }

                instance.UpdateTimer(player);
            }

            public void DeActivate(BasePlayer player) 
            {
				if (player==null) return;
                player.ChatMessage($"");
                Grade = 0;
                Activate(player, 0, 0);
            } 
            
        }

        #endregion
 
        #region Variables

        #region Config

        #region Remove

        private const float RefundPercent = 0.5f;
        private static string CONF_RemovePermission = "GradeDestroy.Remove";
        private static bool CONF_RemoveActivated = true;
        private static int CONF_RemoveTime = 14400;
        private static int CONF_RemoveDefaultTime = 40;
        private static int CONF_RemoveHitTime = 5;
        private static bool CONF_RemoveGameFriends = false;
        private static bool CONF_RemoveFriends;
        private static bool CONF_RemoveClans;
        private static bool CONF_RemoveByCup;
        private static bool CONF_BlockRemoveOnRaid = true;

        #endregion

        #region Upgrade

        private static bool CONF_UpActivated = true;
        private static string CONF_UpPermission = "GradeDestroy.Up";
        private static bool CONF_BlockUpgradeOnRaid = true;
        private static bool CONF_EnableOnHit;

        #endregion

        #region Other
        [PluginReference] Plugin ImageLibrary;
        
        private static List<string> CONF_GradeNames = new List<string>
        {
            "отключено",
            "дерево",
            "камень",
            "металл",
            "мвк",
            "ремув"
        };
        
        public static Dictionary<string, ItemBlueprint> DeployableToBlueprint = new Dictionary<string, ItemBlueprint>();

        #endregion
        
        #endregion

        #region System
        
        [PluginReference] private Plugin Friends, Clans, NoEscape;
        private string Layer = "UI.Remove";
        private static DVBuildingUpgrade instance;

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

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Temporary/DVBuildingUpgrade/Objects"))
            {
                removeTimers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, double>>("Temporary/DVBuildingUpgrade/Objects");
            }
            
            timer.Every(325, SaveData);
            
            foreach (var item in ItemManager.itemList)
            {
                var itemDep = item.GetComponent<ItemModDeployable>();
                var itemBlueprint = item.GetComponent<ItemBlueprint>();
                if (itemDep != null && itemBlueprint != null)
                {
                    DeployableToBlueprint[itemDep.entityPrefab.resourcePath] = itemBlueprint;
                }
            }
			
			if (ImageLibrary){        
			
				ImageLibrary.Call("AddImage", "https://i.imgur.com/7Ay2thv.png", "RemoveImage");
				ImageLibrary.Call("AddImage", "https://i.imgur.com/QoT9u4b.png", "UpImage");
			}
        }

        private void Unload()
        {
            SaveData();
            foreach (var p in BasePlayer.activePlayerList) CuiHelper.DestroyUi(p, Layer);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Temporary/DVBuildingUpgrade/Objects", removeTimers);
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

        private string CanRemove(BasePlayer player, BaseEntity entity)
        {
			if (player==null || entity==null) return "Ошибка";
            if (!player.IsBuildingAuthed())
            { 
                return "Вы не можете удалять постройки без шкафа</color>"; 
            }
             
            if (!player.CanBuild())
            {
                return "Вы не можете удалять в зоне действия<color=#F24525>чужого шкафа</color>!"; 
            }

            var time = GetRaidBlockTime(player.userID);
             
            if (time > 0)
            {
                return $"Вы не можете улучшать постройки! Подождите: {FormatTime(TimeSpan.FromSeconds(time))}</color>";
            }

            double maxRemoveTime;
            if (CONF_RemoveTime != 0 && removeTimers.TryGetValue(entity.net.ID, out maxRemoveTime) && maxRemoveTime <= CurrTimestamp())
            { 
                return "Вышло время удаления данного объекта!</color>";
            }

            bool isOwner = entity.OwnerID == player.userID;
            bool areFriends = (bool) (Friends?.Call("ApiIsFriend", player.userID, entity.OwnerID) ?? false);
            List<string> areClanMates = (List<string>) (Clans?.Call("GetClanMembers", player.userID) ?? new List<string>());
            
            if (CONF_RemoveByCup || CONF_RemoveFriends && areFriends || CONF_RemoveClans && areClanMates.Contains(entity.OwnerID.ToString()) || isOwner)
            {
                return "";
            }

            return "Вы можете удалять свои постройки, а также постройки друзей!</color>";
        }

        private string TryUpgrade(BasePlayer player, BuildingBlock block, CurrentGrade currentGrade)
        {
			if (player==null || block==null) return "Ошибка";
            if (currentGrade.Grade <= (int) block.lastGrade )
                return "";

            var time = GetRaidBlockTime(player.userID);
            
            if (time > 0)
            {
                return $"Вы не можете улучшать постройки! Подождите: {FormatTime(TimeSpan.FromSeconds(time))}</color>";
            }

            var building = BuildingManager.server.GetBuilding(block.buildingID);
            if (building != null && !building.HasBuildingPrivileges())
            {
                return $"Вы не можете использовать автоапгрейд <color=#F24525>без шкафа</color>.</color>";
            }

            if (CONF_BlockUpgradeOnRaid && NoEscape)
            {
                var blockTime = (bool) (NoEscape?.Call("IsRaidBlocked", player) ?? false);
                if (blockTime)
                {
                    currentGrade.DeActivate(player); 
                    return "Вы не можете авто-улучшать постройки во время рейда</color>!";
                }
            }
            
            if (block.SecondsSinceAttacked < 30)
            { 
                return $"Это объект можно будет улучшить через {FormatTime(TimeSpan.FromSeconds(30 - (int) block.SecondsSinceAttacked), maxSubstr:2)}</color>";
            }

            if (block.blockDefinition.checkVolumeOnUpgrade &&
                DeployVolume.Check(block.transform.position, block.transform.rotation, 
                    PrefabAttribute.server.FindAll<DeployVolume>(block.prefabID), ~(1 << block.gameObject.layer)))
            {
                return $"Улучшение данного объекта чем-то заблокированно</color>";
            }

            
            foreach (var check in block.blockDefinition.grades[currentGrade.Grade].costToBuild)
            {
                if (player.inventory.GetAmount(check.itemid) < check.amount)
                {
                    CuiHelper.DestroyUi(player, Layer);
                    currentGrade.DeActivate(player);
                    return "У вас не хватает ресурсов для автоматического улучшения!</color>";
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
            CurrentGrade currentGrade = GetPlayerCurrentGrade(player);
            if (currentGrade.Grade == 0)
            {
                CuiHelper.DestroyUi(player, Layer);
                return;
            }
            
            CuiHelper.DestroyUi(player, Layer + ".Text");
            
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiLabel
            {
                Text = { Text = $"{CONF_GradeNames[currentGrade.Grade].ToUpper()}", FontSize = 9, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 20.7" }
            }, Layer, Layer + ".Text");

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

        [ConsoleCommand("building.upgrade")]
        void cmdConsoleUpgrade(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;

            if (player == null) return;

            CurrentGrade currentGrade = GetPlayerCurrentGrade(player);
            currentGrade.UpGrade(player, CONF_RemoveDefaultTime); 
        }
        
        [ChatCommand("remove")]
        private void cmdChatRemove(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!CONF_RemoveActivated)
                return;
            
            CurrentGrade currentGrade = GetPlayerCurrentGrade(player);

            if (currentGrade.Grade == 5)
            {
                currentGrade.DeActivate(player);
            }
            else
            {
                currentGrade.Activate(player, 5, CONF_RemoveDefaultTime);
            }
        }

        [ChatCommand("up")]
        private void cmdChatUpgrade(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!CONF_UpActivated)
                return;

            CurrentGrade currentGrade = GetPlayerCurrentGrade(player);

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
                        SendReply(player, $"Введенный уровень улучшение <color=#D500C3>не существует</color>!\n" +
                                                $"Попробуйте:\n" +
                                                $"\n" + 
                                                $"0</color> => Отключить\n" +
                                                $"1</color> => дерево\n" +
                                                $"2</color> => камень\n" +
                                                $"3</color> => металл\n" +
                                                $"4</color> => мвк");
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
            if (plan == null) return;
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null)
                return;

            var baseEnt = go.ToBaseEntity();
            BuildingBlock block = go.ToBaseEntity().GetComponent<BuildingBlock>();

            if (CONF_RemoveTime != 0 && (block != null || baseEnt is Door))
            {
                removeTimers.Add(baseEnt.net.ID, CurrTimestamp() + CONF_RemoveTime);
            }
            
            if (block == null) 
                return;

            var currentGrade = GetPlayerCurrentGrade(player);
            if (currentGrade.Grade <= 0 || currentGrade.Grade > 4)
                return;

            var result = TryUpgrade(player, block, currentGrade);
            if (result != string.Empty)
            {
                player.ChatMessage(result);
            }
        }
        
        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (info == null)
                return null;
            
            var currentGrade = GetPlayerCurrentGrade(player);

            var ent = info.HitEntity;
            if (ent == null)
                return null;
            
            if (currentGrade.Grade == 5)
            {
                if ((ent is BuildingBlock || DeployableToBlueprint.ContainsKey(ent.PrefabName)) && !ent.IsDestroyed)
                {
                    var tryRemoveResult = CanRemove(player, ent);
                    if (tryRemoveResult != "")
                        SendReply(player, tryRemoveResult);
                    else
                        RemoveEntity(player, ent);
                    return false;
                }
            }
            else if (currentGrade.Grade != 0)
            {
                var block = ent as BuildingBlock;
                if (block != null && HasUpgradePrivilege(player, block))
                {
                    var tryUpgradeResult = TryUpgrade(player, block, currentGrade);
                    if (tryUpgradeResult != string.Empty)
                        SendReply(player, tryUpgradeResult);
                    return false;
                }
            }
            else
            {
                if (CanRemove(player, ent) == string.Empty)
                {
                    var block = ent as BuildingBlock;
                    if (block != null && HasUpgradePrivilege(player, block))
                    {
                        double maxRemoveTime;
                        if (removeTimers.TryGetValue(block.net.ID, out maxRemoveTime) && player.SecondsSinceAttacked > 10) 
                        {
                            double leftRemove = maxRemoveTime - CurrTimestamp();
                            if (leftRemove < 0) 
                            {
                                SendReply(player, "Время удаления данного объекта<color=#D500C3>закончилось</color>!");
                            }
                            else
                            {
                                SendReply(player, $"Через<color=#F24525>{FormatTime(TimeSpan.FromSeconds(leftRemove), maxSubstr:2)}</color> вы не сможете удалить этот объект!");
                            }
                            
                            player.Hurt(0);
                        }
                    }
                }
            }
            
            return null;
        }

        private bool HasUpgradePrivilege(BasePlayer player, BaseEntity entity)
        {
            return !player.IsBuildingBlocked(entity.transform.position, entity.transform.rotation, entity.bounds);
        }

        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
			if (player==null) return null;
            if (CONF_EnableOnHit && permission.UserHasPermission(player.UserIDString, CONF_UpPermission))
            {
                CurrentGrade currentGrade = GetPlayerCurrentGrade(player);
                currentGrade.Activate(player, (int) grade, CONF_RemoveHitTime);
            }

            if (player.SecondsSinceAttacked > 10)
            { 
                SendReply(player, $"Вы можете удалить построенный объект в течении <color=#F24525>{FormatTime(TimeSpan.FromSeconds(CONF_RemoveTime), maxSubstr:2)}</color>");
                player.Hurt(0);
            }
            return null;
        }

        #endregion

        #region Utils
        
        private void RemoveEntity(BasePlayer player, BaseEntity hitEntity)
        {
			if (player==null || hitEntity==null) return;
            RefundEntity(player, hitEntity);
            
            var storageContainer = hitEntity as StorageContainer;
            if (storageContainer != null)
            {
                storageContainer.DropItems();
            }

            removeTimers.Remove(hitEntity.net.ID);
            hitEntity.Kill(BaseNetworkable.DestroyMode.Gib);
        }

        private static void RefundEntity(BasePlayer player, BaseEntity entity)
        {
			if (player==null || entity==null) return;
            if (entity.name == "assets/prefabs/deployable/planters/planter.small.deployed.prefab")
            {
                return;
            }

            List<ItemAmount> ingredientList = null;

            var entityBb = entity as BuildingBlock;
            ItemBlueprint itemBp;
            if (DeployableToBlueprint.TryGetValue(entity.name, out itemBp))
            {
                ingredientList = itemBp.ingredients;
            }
            else if (entityBb != null)
            {
                ingredientList = entityBb.blockDefinition.grades[(int) entityBb.grade].costToBuild;
            }

            if (ingredientList == null || ingredientList.Count == 0)
            {
                return;
            }
            
            foreach (ItemAmount ingredient in ingredientList)
            {
                var amount = (int)ingredient.amount;
                amount = amount == 0 ? 1 : amount;
                amount = (int) Math.Round( (double) amount * RefundPercent);

                if (amount <= 0)
                {
                    continue;
                }

                Item item = ItemManager.CreateByItemID(ingredient.itemid, amount);
                player.GiveItem(item);
            }
        }
        
        private CurrentGrade GetPlayerCurrentGrade(BasePlayer player)
        {
            CurrentGrade currentGrade;
            if (!playerGrades.TryGetValue(player.userID, out currentGrade))
                currentGrade = playerGrades[player.userID] = new CurrentGrade();
            return currentGrade;
        }

        public double GetRaidBlockTime(ulong userID)
        {
            if (!plugins.Find("NoEscape")) return 0;
            var time = plugins.Find("NoEscape").CallHook("ApiGetTime", userID);
            return Convert.ToDouble(time);
        }

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
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        
        public static string FormatTime(TimeSpan time, int maxSubstr = 5)
        {
            string result = string.Empty;

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
            }

            return result;
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
        private static double CurrTimestamp() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion
    }
}