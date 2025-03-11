using Facepunch;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Remove", "Ryamkk", "1.2.43")]
    class Remove : RustPlugin
    {
		[PluginReference]
        Plugin Clans, Friends, NoEscape;
		
        #region FIELDS
        static int constructionColl = LayerMask.GetMask(new string[] { "Construction", "Deployable", "Prevent Building", "Deployed" });
		private DynamicConfigFile BaseEntityes = Interface.Oxide.DataFileSystem.GetFile("Remove_NewEntity");
        private static Dictionary<string, int> deployedToItem = new Dictionary<string, int>();
        Dictionary<BasePlayer, int> timers = new Dictionary<BasePlayer, int>();
        Dictionary<ulong, string> activePlayers = new Dictionary<ulong, string>();
        int currentRemove = 0;
		
		bool IsClanMember(ulong playerID, ulong targetID) { return (bool)(Clans?.Call("IsTeammate", playerID, targetID) ?? false); }
        bool IsFriends(ulong playerID, ulong targetID) { return (bool)(Friends?.Call("IsFriend", playerID, targetID) ?? false); }
        #endregion

        #region Configuration
        int resetTime = 40;
        float refundPercent = 1.0f;
        float refundItemsPercent = 1.0f;
        float refundStoragePercent = 1.0f;
        bool friendRemove = false;
        bool clanRemove = false;
        bool EnTimedRemove = false;
        bool cupboardRemove = false;
        bool selfRemove = false;
        bool removeFriends = false;
        bool removeClans = false;
        bool refundItemsGive = false;
        float Timeout = 3600.0f;
		bool useNoEscape = false;
		bool storageCount = true;
		bool PermissionSupport = false;
		string removeUse = "remove.use";
		string removeAdmin = "remove.admin";
		
        private string PanelAnchorMin = "0.0 0.908";
        private string PanelAnchorMax = "1 0.958";
        private string PanelColor = "0 0 0 0.50";
        private int TextFontSize = 18;
        private string TextСolor = "0 0 0 1";
        private string TextAnchorMin = "0 0";
        private string TextAnchorMax = "1 1";
		private string FontName = "robotocondensed-regular.ttf";

		private void LoadDefaultConfig()
        {
            GetConfig("Основные настройки", "Время действия режима удаления", ref resetTime);
            GetConfig("Основные настройки", "Включить запрет на удаление объекта для игрока после истечения N времени указанным в конфигурации", ref EnTimedRemove);
            GetConfig("Основные настройки", "Время на запрет удаление объекта после истечения указаного времени (в секундах)", ref Timeout);
            GetConfig("Основные настройки", "Процент возвращаемых ресурсов с Items (Максимум 1.0 - это 100%)", ref refundItemsPercent);
            GetConfig("Основные настройки", "Процент возвращаемых ресурсов с построек (Максимум 1.0 - это 100%)", ref refundPercent);
            GetConfig("Основные настройки", "Включить возрат объектов (При удаление объектов(сундуки, печки и тд.) будет возращать объект а не ресурсы)", ref refundItemsGive);
			GetConfig("Основные настройки", "Процент выпадающих ресурсов (не вещей) с удаляемых ящиков (Максимум 1.0 - это 100%)", ref refundStoragePercent);
			GetConfig("Основные настройки", "Запрещать удалять объекты во время Реид-Блока!", ref useNoEscape);
			GetConfig("Основные настройки", "Запрещать удалять объект если он имеет содержимое (Если false содержимое объекта упадёт на землю)", ref storageCount);
			
            GetConfig("Разрешения на удаления", "(Разрешить/Запретить) удаление объектов друзей без авторизации в шкафу", ref friendRemove);
            GetConfig("Разрешения на удаления", "(Разрешить/Запретить) удаление объектов соклановцев без авторизации в шкафу", ref clanRemove);
			GetConfig("Разрешения на удаления", "(Разрешить/Запретить) удаление чужих объектов при наличии авторизации в шкафу", ref cupboardRemove);
			GetConfig("Разрешения на удаления", "(Разрешить/Запретить) удаление собственных объектов без авторизации в шкафу", ref selfRemove);
			GetConfig("Разрешения на удаления", "(Разрешить/Запретить) удаление обьектов друзьям", ref removeFriends);
			GetConfig("Разрешения на удаления", "(Разрешить/Запретить) удаление объектов соклановцев", ref removeClans);

            GetConfig("Графический интерфейс", "Панель AnchorMin", ref PanelAnchorMin);
            GetConfig("Графический интерфейс", "Панель AnchorMax", ref PanelAnchorMax);
            GetConfig("Графический интерфейс", "Цвет фона панели", ref PanelColor);
            GetConfig("Графический интерфейс", "Текст AnchorMin", ref TextAnchorMin);
            GetConfig("Графический интерфейс", "Текст AnchorMax", ref TextAnchorMax);
            GetConfig("Графический интерфейс", "Цвет текста", ref TextСolor);
			GetConfig("Графический интерфейс", "Размер текста", ref TextFontSize);
			GetConfig("Графический интерфейс", "Названия шрифта", ref FontName);
			
			GetConfig("Управления разрешениями", "(Разрешить/Запретить) использования функционала плагина только тем игрокам у которых есть привилегия", ref PermissionSupport);
			GetConfig("Управления разрешениями", "Названия привилегии обычного удаления объектов", ref removeUse);
			GetConfig("Управления разрешениями", "Названия привилегии админ команд плагина", ref removeAdmin);
			
            SaveConfig();
        }
        #endregion

        #region COMMANDS
        [ChatCommand("remove")]
        void cmdRemove(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
			if (PermissionSupport)
			{
				if (!permission.UserHasPermission(player.UserIDString, removeUse) && !player.IsAdmin)
                {
                    SendReply(player, Messages["NoPermission"]);
                    return;
                }
			}
            if (args == null || args.Length == 0)
            {
                if (activePlayers.ContainsKey(player.userID))
                {
                    timers.Remove(player);
                    DeactivateRemove(player.userID);
                    DestroyUI(player);
                    return;
                }
                else
                {
                    SendReply(player, Messages["enabledRemove"]);
                    timers[player] = resetTime;
                    DrawUI(player, resetTime, "normal");
                    ActivateRemove(player.userID, "normal");
                    return;
                }
            }
            switch (args[0])
            {
                case "admin":

                    if (!permission.UserHasPermission(player.UserIDString, removeAdmin) && !player.IsAdmin)
                    {
                        SendReply(player, Messages["NoPermission"]);
                        return;
                    }
                    if (activePlayers.ContainsKey(player.userID))
                    {
                        timers.Remove(player);
                        DeactivateRemove(player.userID);
                        DestroyUI(player);
                        return;
                    }
                    timers[player] = resetTime;
                    DrawUI(player, resetTime, "admin");
                    ActivateRemove(player.userID, "admin");
                    break;
                case "all":
                    if (!permission.UserHasPermission(player.UserIDString, removeAdmin) && !player.IsAdmin)
                    {
                        SendReply(player, Messages["NoPermission"]);
                        return;
                    }
                    if (activePlayers.ContainsKey(player.userID))
                    {
                        timers.Remove(player);
                        DeactivateRemove(player.userID);
                        DestroyUI(player);
                        return;
                    }
                    timers[player] = resetTime;
                    DrawUI(player, resetTime, "all");
                    ActivateRemove(player.userID, "all");
                    break;
            }
        }
        #endregion

        #region OXIDE HOOKS
		Dictionary<uint, float> entityes = new Dictionary<uint, float>();
        void LoadEntity() => entityes = BaseEntityes.ReadObject<Dictionary<string, float>>().ToDictionary(v => uint.Parse(v.Key), t => t.Value);
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
        private double Cooldown = 30f;
        private void OnActiveItemChanged(BasePlayer player, Item newItem)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem == null || activeItem.info.shortname != "building.planner")
                return;
            if (EnTimedRemove)
            {
                if (activeItem.info.shortname == "building.planner")
                {

                    if (Cooldowns.ContainsKey(player))
                    {
                        double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalSeconds;
                        if (seconds >= 0) return;
                    }
                    SendReply(player, Messages["enabledRemoveTimer"], FormatTime(TimeSpan.FromSeconds(Timeout)));
                    Cooldowns[player] = DateTime.Now.AddSeconds(Cooldown);
                }
            }
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null) return;

            if (EnTimedRemove)
            {
                BaseEntity entity = go.ToBaseEntity();
                if (entity?.net?.ID == null)
                    return;
                entityes[entity.net.ID] = Timeout;
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            List<uint> Remove = new List<uint>();
            foreach (var ent in entityes)
            {
                if (ent.Key == entity.net.ID)
                {
                    Remove.Add(ent.Key);
                }
            }
            foreach (var id in Remove)
            {
                entityes.Remove(id);
            }
        }

        void OnNewSave()
        {
            if (EnTimedRemove)
            {
                Puts("Обнаружен вайп. Очищаем сохраненные объекты!");
                LoadEntity();
                entityes.Clear();
            }
        }

        void OnServerSave() => BaseEntityes.WriteObject(entityes);
		
        void Loaded()
        {
            if (EnTimedRemove) LoadEntity();
        }

        void Init()
        {
            permission.RegisterPermission(removeUse, this);
			permission.RegisterPermission(removeAdmin, this);
        }

        void Unload()
        {
            OnServerSave();
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }

        private Timer entitycheck;
        int check = 30;
        void OnServerInitialized()
        {
            LoadDefaultConfig();
            deployedToItem.Clear();
            LoadEntity();
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            InitRefundItems();
            timer.Every(1f, TimerHandler);
            if (EnTimedRemove) entitycheck = timer.Every(check, TimerEntity);
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                if (itemdef?.GetComponent<ItemModDeployable>() == null) continue;
                if (deployedToItem.ContainsKey(itemdef.GetComponent<ItemModDeployable>().entityPrefab.resourcePath)) continue;
                deployedToItem.Add(itemdef.GetComponent<ItemModDeployable>().entityPrefab.resourcePath, itemdef.itemid);
            }
        }

        private bool CupboardPrivlidge(BasePlayer player, Vector3 position, BaseEntity entity)
        {
            return player.IsBuildingAuthed(position, new Quaternion(0, 0, 0, 0),
                new Bounds(Vector3.zero, Vector3.zero));
        }

        void RemoveAllFrom(Vector3 pos)
        {
            removeFrom.Add(pos);
            DelayRemoveAll();
        }

        List<BaseEntity> wasRemoved = new List<BaseEntity>();
        List<Vector3> removeFrom = new List<Vector3>();

        void DelayRemoveAll()
        {
            if (currentRemove >= removeFrom.Count)
            {
                currentRemove = 0;
                removeFrom.Clear();
                wasRemoved.Clear();
                return;
            }
            List<BaseEntity> list = Pool.GetList<BaseEntity>();
            Vis.Entities<BaseEntity>(removeFrom[currentRemove], 3f, list, constructionColl);
            for (int i = 0; i < list.Count; i++)
            {
                BaseEntity ent = list[i];
                if (wasRemoved.Contains(ent)) continue;
                if (!removeFrom.Contains(ent.transform.position))
                    removeFrom.Add(ent.transform.position);
                wasRemoved.Add(ent);
                DoRemove(ent);
            }
            currentRemove++;
            timer.Once(0.01f, () => DelayRemoveAll());
        }

        static void DoRemove(BaseEntity removeObject)
        {
            if (removeObject == null) return;

            StorageContainer Container = removeObject.GetComponent<StorageContainer>();

            if (Container != null)
            {
                DropUtil.DropItems(Container.inventory, removeObject.transform.position, Container.dropChance);
            }

            EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/item_break.prefab", removeObject, 0, Vector3.up, Vector3.zero) { scale = UnityEngine.Random.Range(0f, 1f) });

            removeObject.KillMessage();
        }

        void TryRemove(BasePlayer player, BaseEntity removeObject) => RemoveAllFrom(removeObject.transform.position);
		
        object OnHammerHit(BasePlayer player, HitInfo info, Vector3 pos)
        {
            var entity = info?.HitEntity;
            if (entity == null) return null;
            if (info == null) return null;
            if (entity.IsDestroyed) return false;
            if (!activePlayers.ContainsKey(player.userID)) return null;
            if (entity.OwnerID == 0) return false;
            if (activePlayers[player.userID] == "admin")
            {
                RemoveEntityAdmin(player, entity);
                return true;
            }
            if (activePlayers[player.userID] == "all")
            {
                TryRemove(player, info.HitEntity);
                RemoveEntityAll(player, entity, pos);
                return true;
            }
		    var storage = entity as StorageContainer;
		    if(storageCount)
			{
                if (storage != null && storage.inventory.itemList.Count != 0)
                {
				    SendReply(player, Messages["storage"]);
                    return true;
                }
			}
            if ((!(entity is DecayEntity) && !(entity is Signage)) && !entity.ShortPrefabName.Contains("shelves") && !entity.ShortPrefabName.Contains("ladder") && !entity.ShortPrefabName.Contains("quarry")) return null;
            if (!entity.OwnerID.IsSteamId()) return null;
            var ret = Interface.Call("CanRemove", player, entity);
            if (ret is string)
            {
                SendReply(player, (string)ret);
                return null;
            }
            if (ret is bool && (bool)ret)
            {
                RemoveEntity(player, entity);
                return true;
            }
            if (useNoEscape)
            {
                if (plugins.Exists("NoEscape"))
                {
					var blockTime = (bool) (NoEscape?.Call("IsRaidBlocked", player) ?? false);
					if (blockTime)
					{
						return "Вы не можете удалять постройки <color=#81B67A>во время рейда!</color>!";
					}
                }
            }
            var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
            if (cupboardRemove)
            {
                if (privilege != null && player.IsBuildingAuthed())
                {
                    RemoveEntity(player, entity);
                    return true;
                }
            }
            if (privilege != null && !player.IsBuildingAuthed())
            {
                if (selfRemove && entity.OwnerID == player.userID)
                {
                    RemoveEntity(player, entity);
                    return true;
                }
                if (friendRemove)
                {
                    if (removeFriends)
                    {
                        if (IsFriends(entity.OwnerID, player.userID))
                        {
                            RemoveEntity(player, entity);
                            return true;
                        }
                    }
                }
                if (clanRemove)
                {
                    if (removeClans)
                    {
                        if (IsClanMember(entity.OwnerID, player.userID))
                        {
                            RemoveEntity(player, entity);
                            return true;
                        }
                    }
                }

                SendReply(player, Messages["ownerCup"]);
                return false;
            }

            if (entity.OwnerID != player.userID)
            {
                if (removeFriends)
                {
                    if (IsFriends(entity.OwnerID, player.userID))
                    {
                        RemoveEntity(player, entity);
                        return true;
                    }
                }
                if (removeClans)
                {
                    if (IsClanMember(entity.OwnerID, player.userID))
                    {
                        RemoveEntity(player, entity);
                        return true;
                    }
                }

                SendReply(player, Messages["norights"]);
                return false;
            }
            RemoveEntity(player, entity);
            return true;
        }

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }


        #endregion

        #region CORE
        void TimerEntity()
        {
            List<uint> remove = entityes.Keys.ToList().Where(ent => (entityes[ent] -= check) < 0).ToList();
            List<uint> Remove = new List<uint>();
            foreach (var entity in entityes)
            {
                var seconds = entity.Value;
                if (seconds < 0.0f)
                {
                    Remove.Add(entity.Key);
                    continue;
                }
                if (seconds > Timeout)
                {
                    entityes[entity.Key] = Timeout;
                    continue;
                }
            }
            foreach (var id in Remove)
            {
                entityes.Remove(id);
            }
        }

        void TimerHandler()
        {
            foreach (var player in timers.Keys.ToList())
            {
                var seconds = --timers[player];
                if (seconds <= 0)
                {
                    timers.Remove(player);
                    DeactivateRemove(player.userID);
                    DestroyUI(player);
                    continue;
                }
                DrawUI(player, seconds, activePlayers[player.userID]);
            }
        }

        void RemoveEntity(BasePlayer player, BaseEntity entity)
        {
            if (EnTimedRemove)
            {
                if (!entityes.ContainsKey(entity.net.ID))
                {
                    SendReply(player, Messages["blockremovetime"], FormatTime(TimeSpan.FromSeconds(Timeout)));
                    return;
                }
            }
            Refund(player, entity);
            entity.Kill();
            UpdateTimer(player, "normal");
        }

        void RemoveEntityAdmin(BasePlayer player, BaseEntity entity)
        {
            entity.Kill();
            UpdateTimerAdmin(player, "admin");
        }
        void RemoveEntityAll(BasePlayer player, BaseEntity entity, Vector3 pos)
        {
            removeFrom.Add(pos);
            DelayRemoveAll();
            UpdateTimerAll(player, "all");
        }

        Dictionary<uint, Dictionary<ItemDefinition, int>> refundItems = new Dictionary<uint, Dictionary<ItemDefinition, int>>();
        void Refund(BasePlayer player, BaseEntity entity)
        {
            if (entity is BuildingBlock)
            {
                BuildingBlock buildingblock = entity as BuildingBlock;
                if (buildingblock.blockDefinition == null) return;
                int buildingblockGrade = (int)buildingblock.grade;
                if (buildingblock.blockDefinition.grades[buildingblockGrade] != null)
                {
                    float refundRate = buildingblock.healthFraction * refundPercent;
                    List<ItemAmount> currentCost = buildingblock.blockDefinition.grades[buildingblockGrade].costToBuild as List<ItemAmount>;
                    foreach (ItemAmount ia in currentCost)
                    {
                        int amount = (int)(ia.amount * refundRate);
                        if (amount <= 0 || amount > ia.amount || amount >= int.MaxValue)
                            amount = 1;
                        if (refundRate != 0)
                        {
                            Item x = ItemManager.CreateByItemID(ia.itemid, amount);
                            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);

                        }
                    }

                }
            }
			
            StorageContainer storage = entity as StorageContainer;
            if (storage)
            {
                for (int i = storage.inventory.itemList.Count - 1; i >= 0; i--)
                {
                    var item = storage.inventory.itemList[i];
                    if (item == null) continue;
                    item.amount = (int)(item.amount * refundStoragePercent);
                    float single = 20f;
                    Vector3 vector32 = Quaternion.Euler(UnityEngine.Random.Range(-single * 0.1f, single * 0.1f), UnityEngine.Random.Range(-single * 0.1f, single * 0.1f), UnityEngine.Random.Range(-single * 0.1f, single * 0.1f)) * Vector3.up;
                    BaseEntity baseEntity = item.Drop(storage.transform.position + (Vector3.up * 0f), vector32 * UnityEngine.Random.Range(5f, 10f), UnityEngine.Random.rotation);
                    baseEntity.SetAngularVelocity(UnityEngine.Random.rotation.eulerAngles * 5f);
                }
            }
			
            if (deployedToItem.ContainsKey(entity.gameObject.name))
            {
                ItemDefinition def = ItemManager.FindItemDefinition(deployedToItem[entity.gameObject.name]);
                foreach (var ingredient in def.Blueprint.ingredients)
                {
                    var reply = 22;
                    if (reply == 0) { }
                    var amountOfIngridient = ingredient.amount;
                    var amount = Mathf.Floor(amountOfIngridient * refundItemsPercent);
                    if (amount <= 0 || amount > amountOfIngridient || amount >= int.MaxValue)
                        amount = 1;
                    if (!refundItemsGive)
                    {
                        if (refundItemsPercent != 0)
                        {
                            Item x = ItemManager.Create(ingredient.itemDef, (int)amount);
                            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
                        }
                    }
                    else
                    {
                        GiveAndShowItem(player, deployedToItem[entity.PrefabName], 1);
                        return;
                    }

                }
            }
        }

        void GiveAndShowItem(BasePlayer player, int item, int amount)
        {
            Item x = ItemManager.CreateByItemID(item, amount);
            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
        }

        void InitRefundItems()
        {
            foreach (var item in ItemManager.itemList)
            {
                var deployable = item.GetComponent<ItemModDeployable>();
                if (deployable != null)
                {
                    if (item.Blueprint == null || deployable.entityPrefab == null) continue;
                    refundItems.Add(deployable.entityPrefab.resourceID, item.Blueprint.ingredients.ToDictionary(p => p.itemDef, p => (Mathf.CeilToInt(p.amount * refundPercent))));
                }
            }
        }

        #endregion

        #region UI
        private string GUI = @"[{""name"": ""remove.panel"",""parent"": ""Hud"",""components"": 
		                       [{""type"": ""UnityEngine.UI.Image"",""color"": ""{PanelColor}""},
							    {""type"": ""RectTransform"",""anchormin"": ""{PanelAnchorMin}"",""anchormax"": ""{PanelAnchorMax}""}]}, 
								{""name"": ""remove.text"",""parent"": ""remove.panel"",""components"": 
							   [{""type"": ""UnityEngine.UI.Text"",""text"": ""{msg}"",""fontSize"": ""{TextFontSize}"",""font"":""{FontName}"",""align"": ""MiddleCenter""}, 
							    {""type"": ""UnityEngine.UI.Outline"",""color"": ""{TextСolor}"",""distance"": ""0.1 -0.1""}, 
								{""type"": ""RectTransform"",""anchormin"": ""{TextAnchorMin}"",""anchormax"": ""{TextAnchorMax}""}]}]";

        void DrawUI(BasePlayer player, int seconds, string type)
        {
            DestroyUI(player);
            var msg = "";
            if (type == "normal")
            {
                msg = Messages["RNormal"];
            }
            else
                msg = type == "admin" ? Messages["RAdmin"] : Messages["RAll"];

            CuiHelper.AddUi(player,
                GUI.Replace("{PanelColor}", PanelColor.ToString())
                   .Replace("{PanelAnchorMin}", PanelAnchorMin.ToString())
                   .Replace("{PanelAnchorMax}", PanelAnchorMax.ToString())
                   .Replace("{TextFontSize}", TextFontSize.ToString())
                   .Replace("{TextСolor}", TextСolor.ToString())
                   .Replace("{TextAnchorMin}", TextAnchorMin.ToString())
                   .Replace("{TextAnchorMax}", TextAnchorMax.ToString())
				   .Replace("{FontName}", FontName.ToString())
                   .Replace("{msg}", msg)
                   .Replace("{1}", FormatTime(TimeSpan.FromSeconds(seconds))));
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "remove.panel");
            CuiHelper.DestroyUi(player, "remove.text");
        }
        #endregion

        #region API
        void ActivateRemove(ulong userId, string type)
        {
            if (!activePlayers.ContainsKey(userId))
            {
                activePlayers.Add(userId, type);
            }
        }

        void DeactivateRemove(ulong userId)
        {
            if (activePlayers.ContainsKey(userId))
            {
                activePlayers.Remove(userId);
            }
        }

        void UpdateTimer(BasePlayer player, string type)
        {
            timers[player] = resetTime;
            DrawUI(player, timers[player], type);
        }

        void UpdateTimerAdmin(BasePlayer player, string type)
        {
            timers[player] = resetTime;
            DrawUI(player, timers[player], type);
        }

        void UpdateTimerAll(BasePlayer player, string type)
        {
            timers[player] = resetTime;
            DrawUI(player, timers[player], type);
        }

        #endregion

        #region Localization
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"raidremove", "Ремув во время рейда запрещён!\nОсталось<color=#ffd479> {0}</color>." },
            {"blockremovetime", "Извините, но этот объект уже нельзя удалить, он был создан более чем <color=#ffd479>{0}</color> назад!" },
            {"NoPermission", "У Вас нету прав на использование этой команды!" },
            {"enabledRemove", "<size=16>Используйте киянку для удаления объектов</size>." },
            {"enabledRemoveTimer", "<color=#ffd479>Внимание:</color> Объекты созданые более чем <color=#ffd479>{0}</color> назад, удалить нельзя." },
            {"ownerCup", "Что бы удалять постройки, вы должны быть авторизированы в шкафу!" },
            {"norights", "Вы не имеете права удалять чужие постройки!" },
			{"storage", "Объект имеет содержимое, ремув запрещен!" },
            {"RNormal", "Режим удаления выключится через <color=#ffd479>{1}</color>." },
            {"RAdmin", "Режим админ удаления выключится через <color=#ffd479>{1}</color>." },
            {"RAll", "Режим удаления всех объектов выключится через <color=#ffd479>{1}</color>." },
        };
        #endregion
		
	    private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
    }
}