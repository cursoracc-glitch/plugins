using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("RemoveUpgradeV2", "discord.gg/9vyTXsJyKR", "1.0.3")]
    [Description("ОПТИМИЗИРОВАН! Лучший плагин для ремува и апгрейда построек")]
    class RemoveUpgradeV2 : RustPlugin
    {
        #region Config
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        private class PluginConfig
        {
            [JsonProperty("Разрешить ремувать постройки друзьям")]
            public bool friends;

            [JsonProperty("Разрешить ломать постройки авторизованым в шкафу")]
            public bool cupboard;

            [JsonProperty("Выключить GUI?")]
            public bool disablegui;

            [JsonProperty("Время действия ремува/апгрейда")]
            public int time;

            [JsonProperty("Процент возвращаемых ресурсов с построек")]
            public float procent;

            [JsonProperty("Расположение GUI - AnchorMin")]
            public string AnchorMin;

            [JsonProperty("Расположение GUI - AnchorMax")]
            public string AnchorMax;

            [JsonProperty("Блокировка ремува объекта через определённое время (секунд) [-1 - выключить]")]
            public int blocktime;

            [JsonProperty("Версия рейдблока [0 - Хугана, 1 - С юмода]")]
            public int whatblock;

            [JsonProperty("GUI - Цвет фона")]
            public string color;

            [JsonProperty("Сообщения")]
            public List<string> messages;

            [JsonProperty("Сообщения - Названия")]
            public List<string> messages2;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    friends = true,
                    cupboard = false,
                    time = 60,
                    procent = 100f,
                    AnchorMin = "0.3447913 0.1135",
                    AnchorMax = "0.640625 0.1435",
                    color = "0.97 0.92 0.88 0.18",
                    blocktime = 7200,
                    whatblock = 0,
                    messages = new List<string>
                    {
                        "РЕЖИМ УДАЛЕНИЯ ВЫКЛЮЧИТСЯ ЧЕРЕЗ <color=#ffd479>{count} СЕКУНД</color>",
                        "<size=15>Режим удаления выключен</size>.",
                        "<color=red>Не найден объект для удаления.</color>",
                        "<color=#ffd479>BIND X REMOVE.USE</color> БИНД ДЛЯ <color=#ffd479>РЕМУВА</color>\n<color=#ffd479>НАЖМИТЕ НА 'E' </color>ДЛЯ <color=#ffd479>УЛУЧШЕНИЯ</color>",
                        "<color=#ffd479>BIND C UPGRADE.USE</color> БИНД ДЛЯ <color=#ffd479>УЛУЧШЕНИЯ</color>",
                        "<size=15>В контейнере есть предметы!</size>", //5
                        "<size=15>Ремув этого объекта стоит <color=#ffd479>{amount} дерева</color>!</size>",
                        "Объект уже уничтожен.",
                        "Системный объект.",//8
                        "<size=15>Прошло много времени после постройки данного объекта!</size>",
                        "<size=15>Нельзя использовать <color=#ffd479>remove</color> во время рейд-блока</size>",
                        "<size=15><color=yellow>Вы не имеете права удалять чужие постройки!</color></size>",
                        "<size=15><color=yellow>Что бы удалять постройки, вы должны быть авторизированы в шкафу!</color></size>",
                        "У тебя нету прав на использование этой команды",
                        "<size=15>Используйте <color=#ffd479>киянку</color> для удаления объектов.\nКоманда <color=#ffd479>/remove</color> - выключить режим удаления</size>",
                        "<size=15>Используйте <color=#ffd479>план постройки</color> и <color=#ffd479>киянку</color> для улучшения объектов.\nКоманда <color=#ffd479>/up</color> - сменить ресурс улучшения\nКоманда <color=#ffd479>/remove</color> - выключить режим улучшения</size>",
                        "<size=15>Вы изменили режим улучшения до <color=#ffd479>{grade}</color></size>", //16
                        "АВТОУЛУЧШЕНИЕ ДО <color=#ffd479>{grade}</color> ВЫКЛЮЧИТСЯ ЧЕРЕЗ <color=#ffd479>{count} СЕК</color>",
                        "<size=15>Режим улучшения выключен</size>.",
                        "<size=15>Этот объект не нуждается в улучшении до <color=#ffd479>{grade}</color>!</size>",
                        "<size=15>Вы не можете авто-улучшать постройки <color=#ffd479>во время рейда</color>!</size>", //20
                        "<size=15>Вы находитесь на чужой территории!</size>",
                        "Этот объект можно будет улучшить через <color=#ffd479>{count} секунд</color>",
                        "У вас не хватает ресурсов для улучшения до <color=#ffd479>{grade}</color>!" //23
                    },
                    messages2 = new List<string>()
                    {
                        { "None" }, { "ДЕРЕВА" }, { "КАМНЯ" }, { "МЕТАЛА" }, { "МВК" }
                    },
                    disablegui = false
                };
            }
        }
        #endregion

        Dictionary<ulong, DateTime> canremove = new Dictionary<ulong, DateTime>();
        private void OnEntitySpawned(BaseCombatEntity ent)
        {
            if (ent == null) return;
            if (ent is DecayEntity)
            {
                if (!ent.IsDestroyed) canremove[ent.net.ID.Value] = DateTime.Now.AddSeconds(config.blocktime);
            }
        }

        static string GUIjson = "";
        static RemoveUpgradeV2 _ins;
        string helpstring;

        void Init()
        {
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private static bool wipe = false;
        private void OnNewSave(string filename) => wipe = true;

        void OnServerInitialized()
        {
            if (wipe) wipe = false;
            else
            {
                Dictionary<ulong, string> saved = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("entspawned");

                foreach (var z in saved)
                {
                    DateTime dateTime = Convert.ToDateTime(z.Value);
                    if (dateTime < DateTime.Now) continue;
                    canremove[z.Key] = dateTime;
                }
            }

            LoadItemPrefabs();

            SaveConfig();
            if (config.disablegui) Unsubscribe(nameof(OnActiveItemChanged));
            if (config.blocktime >= 1) Subscribe(nameof(OnEntitySpawned));
            GUIjson = "[{\"name\":\"RemoverGUIBackground\", \"parent\":\"Hud\", \"components\":[{\"type\":\"UnityEngine.UI.Image\", \"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"-199.5 80\",\"offsetmax\":\"180.5 {max}\"}]},{\"name\":\"RemoverGUIText\",\"parent\":\"RemoverGUIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"font\":\"robotocondensed-regular.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.5\",\"distance\":\"0.5 -0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]}]".Replace("{max}", "100").Replace("{color}", config.color);

            float number;
            string[] ar = config.AnchorMax.Split(' ');
            if (float.TryParse(ar[1], out number))
            {
                helpstring = ar[0] + " " + (number + 0.03f).ToString();
            }
            else Debug.LogError("Конфиг поврежден!");

            _ins = this;

            permission.RegisterPermission("RemoveUpgradeV2.admin", this);
            permission.RegisterPermission("RemoveUpgradeV2.use", this);
            permission.RegisterPermission("RemoveUpgradeV2.refund", this);
            permission.RegisterPermission("RemoveUpgradeV2.remove", this);
            permission.RegisterPermission("RemoveUpgradeV2.up", this);
            permission.RegisterPermission("RemoveUpgradeV2.vip", this);

            foreach (ItemDefinition def in ItemManager.GetItemDefinitions())
            {
                if (!def.Blueprint) continue;
                ItemModDeployable deployable = def.GetComponent<ItemModDeployable>();
                if (deployable != null) deployedToItem[deployable.entityPrefab.resourceID] = def.itemid;
            }
        }

        void unloadbehavior(BasePlayer player)
        {
            player.GetComponent<ToolRemover>()?.DoDestroy();
            player.GetComponent<UpgradeConstruction>()?.DoDestroy();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            unloadbehavior(player);
        }

        private void Unload()
        {
            if (canremove.Count > 0) Interface.Oxide.DataFileSystem.WriteObject("entspawned", canremove);
            foreach (BasePlayer player in BasePlayer.activePlayerList) unloadbehavior(player);
        }

        private Dictionary<ulong, int> deployedToItem = new Dictionary<ulong, int>();

        enum RemoveType
        {
            Normal,
            Refund,
            Admin,
            All
        }

        static void PrintToChat(BasePlayer player, string message) => player.ChatMessage(message);


        class ToolRemover : MonoBehaviour
        {
            public BasePlayer player;
            int count;
            int maxcount;
            public RemoveType removeType;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                if (player == null) Destroy(this);
                SetCount(_ins.config.time);
                InvokeRepeating("UpdateGUI", 0, 1);
            }

            public void SetCount(int number)
            {
                count = number;
                maxcount = number;
            }

            public void ResetDestroy()
            {
                count = maxcount;
            }

            void UpdateGUI()
            {
                if (count.Equals(0)) DoDestroy();
                if (!_ins.config.disablegui)
                {
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "RemoverGUIBackground");
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIjson.Replace("{text}", _ins.config.messages[0].Replace("{count}", count.ToString())).Replace("{max}", _ins.config.AnchorMax));
                }
                count--;
            }

            public void DoDestroy()
            {
                Destroy(this);
            }

            void OnDestroy()
            {
                if (!_ins.config.disablegui) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "RemoverGUIBackground");
                PrintToChat(player, _ins.config.messages[1]);
            }
        }


        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return null;

			var entity = info.HitEntity as BaseCombatEntity;
			if (entity == null || entity.OwnerID == 0) return null;

            ToolRemover RemoverClass = player.GetComponent<ToolRemover>();
            if (RemoverClass != null)
            {
                TryRemove(player, entity, RemoverClass.removeType);
                RemoverClass.ResetDestroy();
                return false;
            }
            else if (info.HitEntity is BuildingBlock)
            {
				var block = entity as BuildingBlock;
				if (block == null) return null;

                var grade = player.GetComponent<UpgradeConstruction>();
                if (grade == null) return null;

                TryUpgrade(player, block, grade);
                return true;
            }
            return null;
        }

		private static void GiveRefund(BaseCombatEntity entity, BasePlayer player)
		{
			var shortPrefabName = entity.ShortPrefabName;

			if (!_ins._shortPrefabNamesToItem.TryGetValue(shortPrefabName, out shortPrefabName))
				shortPrefabName = Regex.Replace(entity.ShortPrefabName, "\\.deployed|_deployed", "");

			var item = ItemManager.CreateByName(shortPrefabName);
			if (item != null)
			{

				player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
				return;
			}

			entity.BuildCost()?.ForEach(value =>
			{
				var amount = Convert.ToInt32(_ins.config.procent < 100
					? value.amount * (_ins.config.procent / 100f)
					: value.amount);

				item = ItemManager.Create(value.itemDef, amount);
				if (item == null) return;

				player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
			});
		}

		private static void RemoveEntity(BasePlayer player, BaseCombatEntity entity, bool isRefund = false)
		{
            if (entity == null || entity.IsDestroyed) return;

            if (isRefund)
            {
                GiveRefund(entity, player);
            }

			DropContainer(entity.GetComponent<StorageContainer>());
			DropContainer(entity.GetComponent<ContainerIOEntity>());

            if (!entity.IsDestroyed)
			entity.Kill();
		}

		private static void DropContainer(StorageContainer container)
		{
			if (container == null || container.inventory.itemList.Count < 1) return;

			ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop.prefab", container.GetDropPosition(),
				container.Transform.rotation, container.inventory);
		}

		private static void DropContainer(ContainerIOEntity container)
		{
			if (container == null || container.inventory.itemList.Count < 1) return;

			ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop.prefab", container.GetDropPosition(),
				container.Transform.rotation, container.inventory);
		}

        private static bool IsUpgradeBlocked(BuildingBlock block)
        {
            var deployVolumeArray = PrefabAttribute.server.FindAll<DeployVolume>(block.prefabID);
            return DeployVolume.Check(block.transform.position, block.transform.rotation, deployVolumeArray, ~(1 << (block.gameObject.layer & 31)));
        }
        private void TryUpgrade(BasePlayer player, BuildingBlock block, UpgradeConstruction gr)
        {
            if (block == null) return;

            if ((int)block.grade >= gr.currentgrade)
            {
                PrintToChat(player, config.messages[19].Replace("{grade}", config.messages2[gr.currentgrade]));
                return;
            }

            if (NoEscape)
            {
                object can = NoEscape?.Call("IsRaidBlocked", player);
                if (can != null) if ((bool)can)
                {
                    gr.DoDestroy();
                    PrintToChat(player, config.messages[20]);
                    return;
                }
            }

            if (block.prefabID == 72949757 || block.prefabID == 3234260181)
            {
                if (IsUpgradeBlocked(block))
                {
                    PrintToChat(player, "<color=yellow>В фундаменте что-то находиться!</color>");
                    return;
                }
            }

            if (!player.CanBuild())
            {
                gr.DoDestroy();
                PrintToChat(player, config.messages[21]);
                return;
            }

            if (block.SecondsSinceAttacked < 30)
            {
                PrintToChat(player, config.messages[22].Replace("{count}", (30 - block.SecondsSinceAttacked).ToString()));
                return;
            }

            BuildingGrade.Enum grade = (BuildingGrade.Enum)gr.currentgrade;

            if (!CanAffordUpgrade(block, grade, player))
            {
                PrintToChat(player, config.messages[23].Replace("{grade}", config.messages2[gr.currentgrade]));
                return;
            }

            PayForUpgrade(block, grade, player);

            UpgradeBuildingBlock(block, grade);
            Effect.server.Run("assets/bundled/prefabs/fx/build/promote_" + grade.ToString().ToLower() + ".prefab", block, 0U, Vector3.zero, Vector3.zero);
            gr.count = _ins.config.time;
        }

        private void TryRemove(BasePlayer player, BaseCombatEntity removeObject, RemoveType removeType)
        {
            if (player == null) return;
            if (removeObject == null)
            {
                PrintToChat(player, config.messages[2]);
                return;
            }

            bool can = CanRemoveEntity(player, removeObject, removeType);
            if (!can) return;

            if (removeType.Equals(RemoveType.All)) Removeall(removeObject);
            else if (removeType.Equals(RemoveType.Refund)) RemoveEntity(player, removeObject, true);
            else RemoveEntity(player, removeObject);
        }

        Dictionary<ulong, Timer> activegui = new Dictionary<ulong, Timer>();
        List<ulong> activeusers = new List<ulong>();
        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null)
            {
                if (newItem.info.itemid.Equals(200773292) || newItem.info.itemid.Equals(1803831286))
                {
                    if (player.GetComponent<ToolRemover>() || player.GetComponent<UpgradeConstruction>()) return;
                    destroynotif(player);
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIjson.Replace("{text}", config.messages[3]).Replace("{max}", "100"));
                    activegui.Add(player.userID, timer.Once(7f, () =>
                    {
                        destroynotif(player);
                    }));
                }
                else if (newItem.info.itemid.Equals(1525520776))
                {
                    if (player.GetComponent<ToolRemover>() || player.GetComponent<UpgradeConstruction>()) return;
                    destroynotif(player);
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIjson.Replace("{text}", config.messages[4]).Replace("{max}", "100"));
                    activegui.Add(player.userID, timer.Once(7f, () =>
                    {
                        destroynotif(player);
                    }));
                }
                else if (activegui.ContainsKey(player.userID))
                {
                    destroynotif(player);
                }
            }
            else if (activegui.ContainsKey(player.userID))
            {
                destroynotif(player);
            }
        }

        void destroynotif(BasePlayer player)
        {
            if (activegui.ContainsKey(player.userID))
            {
                activegui[player.userID]?.Destroy();
                activegui.Remove(player.userID);
            }
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "RemoverGUIBackground");
        }

        static int constructionColl = LayerMask.GetMask(new string[] { "Construction", "Deployable", "Prevent Building", "Deployed" });

        void Removeall(BaseEntity removeObject)
        {
            ServerMgr.Instance.StartCoroutine(Loop(removeObject.transform.position));
        }

        private static IEnumerator Loop(Vector3 pos)
        {
            List<BaseEntity> list = Pool.GetList<BaseEntity>();
            yield return new WaitForEndOfFrame();
            Vis.Entities<BaseEntity>(pos, 10f, list, constructionColl);
            yield return new WaitForEndOfFrame();
            foreach (var z in list)
            {
                z.KillMessage();
                yield return new WaitForSeconds(0.05f);
            }
        }

        void DoRemove(BaseEntity removeObject, BasePlayer player)
        {
            if (removeObject is StorageContainer && removeObject.GetComponent<StorageContainer>().inventory.itemList.Count > 0)
            {
                PrintToChat(player, config.messages[5]);
                return;
            }
            EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/item_break.prefab", removeObject, 0, Vector3.up, Vector3.zero) { scale = UnityEngine.Random.Range(0f, 1f) });

            if (!removeObject.IsDestroyed)
            removeObject.KillMessage();
        }

        [PluginReference] Plugin NoEscape, RaidBlock, RaidZone, BLimitObject = null;

        public double GetMultiply(BasePlayer player, int costToBuild)
        {
            return (double)BLimitObject?.Call("GetMultiply", player, costToBuild);
        }

        bool CanRemoveEntity(BasePlayer player, BaseEntity entity, RemoveType removeType)
        {
            if (entity.IsDestroyed)
            {
                PrintToChat(player, config.messages[7]);
                return false;
            }

            if (entity.OwnerID == 0)
            {
                PrintToChat(player, config.messages[8]);
                return false;
            }

            if (removeType == RemoveType.Admin || removeType == RemoveType.All) return true;

            if (entity.OwnerID != player.userID && !config.cupboard)
            {
                if (config.friends)
                {
                    if (player.Team == null || !player.Team.members.Contains(entity.OwnerID))
                    {
                        PrintToChat(player, config.messages[11]);
                        return false;
                    }
                }
            }

            var externalPlugins = Interface.CallHook("canRemove", player, entity);
            if (externalPlugins != null)
            {
                player.ChatMessage("Не удалось использовать удаление постройки: внешний фактор заблокировал его использование");
                return false;
            }

            if (NoEscape)
            {
                object can = NoEscape?.Call("IsRaidBlocked", player);
                if (can != null) if ((bool)can)
                {
                    PrintToChat(player, config.messages[10]);
                    return false;
                }
            }

            var privilege = entity.GetBuildingPrivilege();
            if (privilege != null)
            {
                if (!privilege.IsAuthed(player))
                {
                    player.ChatMessage("Вы не можете удалять постройку без авторизации в шкафу!");
                    return false;
                }
            }

            if (!player.CanBuild())
            {
                PrintToChat(player, config.messages[12]);
                return false;
            }

            return true;
        }

        [ChatCommand("remove")]
        void cmdChatRemove(BasePlayer player, string command, string[] args)
        {
            removecommand(player, args);
        }

        [ChatCommand("up")]
        void cmdChatUpgrade(BasePlayer player, string command, string[] args)
        {
            upgradecommand(player, args);
        }

        [ConsoleCommand("upgrade.use")]
        void ConsoleUP(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            upgradecommand(player, arg.Args);
        }

        [ConsoleCommand("remove.use")]
        void ConsoleRemove(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            removecommand(player, arg.Args);
        }

        void removecommand(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.use") && !permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.remove"))
            {
                SendReply(player, config.messages[13]);
                return;
            }
            UpgradeConstruction tool = player.GetComponent<UpgradeConstruction>();
            if (tool != null)
            {
                tool.DoDestroy();
                return;
            }
            ToolRemover toolremover = player.GetComponent<ToolRemover>();
            if (toolremover != null)
            {
                toolremover.DoDestroy();
                return;
            }
            int removeTime = config.time;
            RemoveType removetype = permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.refund") ? RemoveType.Refund : RemoveType.Normal;

            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "admin":
                        if (!permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.admin") && !player.IsAdmin)
                        {
                            SendReply(player, config.messages[13]);
                            return;
                        }
                        removetype = RemoveType.Admin;
                        if (args.Length > 1) int.TryParse(args[1], out removeTime);
                        break;
                    case "all":
                        if (!permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.admin") && !player.IsAdmin)
                        {
                            SendReply(player, config.messages[13]);
                            return;
                        }
                        removetype = RemoveType.All;
                        if (args.Length > 1) int.TryParse(args[1], out removeTime);
                        break;
                    default:
                        int.TryParse(args[0], out removeTime);
                        break;
                }
            }
            if (removeTime == 0) removeTime = config.time;

            if (activegui != null && activegui.ContainsKey(player.userID))
            {
                activegui[player.userID]?.Destroy();
                activegui.Remove(player.userID);
            }
            toolremover = player.gameObject.AddComponent<ToolRemover>();

            PrintToChat(player, config.messages[14]);

            toolremover.SetCount(removeTime);
            toolremover.removeType = removetype;
        }


        #region Upgrade
        [ConsoleCommand("upgrade.off")]
        void ConsoleUasd1P(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            player.GetComponent<UpgradeConstruction>()?.DoDestroy();
        }

        [ConsoleCommand("remove.off")]
        void ConsoleUasd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            player.GetComponent<ToolRemover>()?.DoDestroy();
        }

        bool IsUpgrade(BasePlayer player)
        {
            UpgradeConstruction uc = player.GetComponent<UpgradeConstruction>();
            if (uc == null) return false;
            return true;
        }

        bool IsRemove(BasePlayer player)
        {
            ToolRemover re = player.GetComponent<ToolRemover>();
            if (re == null) return false;
            return true;
        }

        void upgradecommand(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.use") && !permission.UserHasPermission(player.UserIDString, "RemoveUpgradeV2.up"))
            {
                SendReply(player, config.messages[13]);
                return;
            }
            int current = 0;
            if (args != null && args.Length > 0) int.TryParse(args[0], out current);
            UpgradeConstruction tool = player.GetComponent<UpgradeConstruction>();
            if (tool != null)
            {
                if (current != 0) tool.changegrade(current);
                else tool.changegrade(tool.currentgrade + 1);
                return;
            }

            if (activegui != null && activegui.ContainsKey(player.userID))
            {
                activegui[player.userID]?.Destroy();
                activegui.Remove(player.userID);
            }
            tool = player.gameObject.AddComponent<UpgradeConstruction>();
            if (current != 0) tool.changegrade(current);
            PrintToChat(player, config.messages[15]);
        }

        class UpgradeConstruction : MonoBehaviour
        {
            BasePlayer player;
            public int count = _ins.config.time;
            public int currentgrade = 2;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                if (player == null) Destroy(this);
                ToolRemover tool = player.GetComponent<ToolRemover>();
                if (tool != null) tool.DoDestroy();
                InvokeRepeating("UpdateGUI", 0, 1f);
            }

            public void changegrade(int change)
            {
                if (change > 4) currentgrade = 1;
                else if (change <= 0) currentgrade = 2;
                else currentgrade = change;
                if (change != 0) PrintToChat(player, _ins.config.messages[16].Replace("{grade}", _ins.config.messages2[currentgrade]));
                count = _ins.config.time;
                UpdateGUI();
                EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, Vector3.up, Vector3.zero) { scale = UnityEngine.Random.Range(0f, 1f) }, player.net.connection);
            }

            void UpdateGUI()
            {
                if (count.Equals(0))
                {
                    DoDestroy();
                    return;
                }
                if (!_ins.config.disablegui)
                {
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "RemoverGUIBackground");
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIjson.Replace("{text}", _ins.config.messages[17].Replace("{grade}", _ins.config.messages2[currentgrade]).Replace("{count}", count.ToString())).Replace("{max}", "100"));
                }
                count--;
            }

            public void DoDestroy()
            {
                Destroy(this);
            }

            void OnDestroy()
            {
                if (!_ins.config.disablegui) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "RemoverGUIBackground");
                PrintToChat(player, _ins.config.messages[18]);
            }
        }

        private Dictionary<string, string> _shortPrefabNamesToItem = new Dictionary<string, string>();

		private void LoadItemPrefabs()
		{
			foreach (var itemDefinition in ItemManager.GetItemDefinitions())
			{
				var entityPrefab = itemDefinition.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
				if (string.IsNullOrEmpty(entityPrefab))
					continue;

				var shortPrefabName = Utility.GetFileNameWithoutExtension(entityPrefab);
				if (!string.IsNullOrEmpty(shortPrefabName))
					_shortPrefabNamesToItem.TryAdd(shortPrefabName, itemDefinition.shortname);
			}
		}

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity is BasePlayer)
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || player.IsNpc) return;
                ToolRemover tool = player.GetComponent<ToolRemover>();
                if (tool != null) tool.DoDestroy();
                UpgradeConstruction tool2 = player.GetComponent<UpgradeConstruction>();
                if (tool2 != null) tool2.DoDestroy();
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null) return;
            BasePlayer player = plan.GetOwnerPlayer();
            if (player == null) return;
            UpgradeConstruction gr = player.GetComponent<UpgradeConstruction>();
            if (gr == null) return;
            BuildingBlock block = go.GetComponent<BuildingBlock>();
            if (block == null) return;
            TryUpgrade(player, block, gr);
        }
        #endregion

        #region [Added]

        private bool CanAffordUpgrade(BuildingBlock block, BuildingGrade.Enum @enum, BasePlayer player)
        {
			var dict = new Dictionary<int, int>();

			foreach (var itemAmount in block.blockDefinition.GetGrade(@enum, block.skinID).CostToBuild())
			{
				int amount;
				if (!dict.TryGetValue(itemAmount.itemid, out amount))
					amount = player.inventory.GetAmount(itemAmount.itemid);
                double Factor = GetMultiply(player, (int)itemAmount.amount);
				if (amount < itemAmount.amount + Factor)
					return false;

				dict[itemAmount.itemid] = amount - Mathf.RoundToInt(itemAmount.amount);
			}

            return true;
        }

        private void PayForUpgrade(BuildingBlock block, BuildingGrade.Enum @enum, BasePlayer player)
        {
			var collect = new List<Item>();

            foreach (var itemAmount in block.blockDefinition.GetGrade(@enum, block.skinID).CostToBuild())
            {
                double Factor = GetMultiply(player, (int)itemAmount.amount);
                player.inventory.Take(collect, itemAmount.itemid, (int)itemAmount.amount + (int)Factor);
                player.Command("note.inv " + itemAmount.itemid + " " + (float) ((int) itemAmount.amount * -1.0));
            }

			foreach (var obj in collect)
				obj.Remove();
        }

        private static void UpgradeBuildingBlock(BuildingBlock block, BuildingGrade.Enum @enum)
        {
            if (block == null || block.IsDestroyed) return;

			block.SetGrade(@enum);
			block.SetHealthToMax();
			block.StartBeingRotatable();
			block.SendNetworkUpdate();
			block.UpdateSkin();
			block.ResetUpkeepTime();
			block.UpdateSurroundingEntities();
			block.GetBuilding()?.Dirty();
        }

        #endregion
    }

}
