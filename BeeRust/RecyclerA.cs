using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Physics = UnityEngine.Physics;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Recycler", "backwood", "1.0.2")]
    class RecyclerA : RustPlugin
    {
        [PluginReference] 
		private Plugin NoteUI;
		
        #region Classes
        public class DataConfig
        {
            [JsonProperty("Скин предмета")]
            public ulong skin;
            [JsonProperty("Имя предмета")]
            public string itemName;
            [JsonProperty("Описание предмета")]
            public string description;
            [JsonProperty("Подбор переработчика")]
            public bool recyclerpickup;
            [JsonProperty("Подбор переработчика без прописки в шкафу")]
            public bool noauthpickup;
			[JsonProperty("Использовать плагин NoteUI для вывода уведомлений? (заменит все сообщения в чат)")]
            public bool usenoteui;
        }

        #endregion

        #region Config

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
                skin = 908940141,
                itemName = "<color=#249c00>Переработчик</color>",
                description = "<size=10>Установите и используйте!</size>",
                recyclerpickup = true,
                noauthpickup = true,
				usenoteui = true,
            };
        }

        #endregion

        #region Hooks and Methods

        void OnServerInitialized()
        {
            if (cfg.usenoteui && !plugins.Exists("NoteUI"))
            {
                PrintWarning("Плагин 'NoteUI' не загружен, дальнейшая работа плагина невозможна!");
                return;
            }
        }

        bool GiveRecycler(BasePlayer player)
        {
            var item = ItemManager.CreateByName("box.wooden.large", 1, cfg.skin);
            item.name = cfg.itemName + " " + cfg.description;
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }

        [ConsoleCommand("recycler.give")]
        private void CmdGiveRecycler(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                SendError(arg, "[Ошибка] У вас нет доступа к этой команде!");
                return;
            }
            if (!arg.HasArgs())
            {
                PrintError(
                ":\n[Ошибка] Введите recycler.give steamid/nickname\n[Пример] recycler.give backwood\n[Пример] recycler.give 76561198311233564");
                return;
            }
            var player = BasePlayer.Find(arg.Args[0]);
            if (player == null)
            {
                PrintError($"[Ошибка] Не удается найти игрока {arg.Args[0]}");
                return;
            }
            GiveRecycler(player);
        }

        private bool? CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin)
                return false;

            return null;
        }

        private bool? CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem() == null || targetItem.GetItem() == null)
                return null;

            if (item.GetItem().skin != targetItem.GetItem().skin)
                return false;

            return null;
        }


        private Item OnItemSplit(Item item, int amount)
        {
            if (item != null && item.skin == cfg.skin)
            {
                Item x = ItemManager.CreateByName("box.wooden.large", 1);
                x.name = cfg.itemName;
                x.skin = cfg.skin;
                x.amount = amount;

                item.amount -= amount;
                item.MarkDirty();
                return x;
            }

            return null;
        }

        void OnEntityBuilt(Planner planner, GameObject gameobject, BasePlayer player)
        {
            BaseEntity entity = gameobject.ToBaseEntity();
            if (entity == null) return;
            if (entity.skinID != cfg.skin) return;
            entity.Kill();
            var ePos = entity.transform.position;
            Vector3 position = new Vector3(ePos.x, ePos.y + 1, ePos.z);
            var hitted = false;
            RaycastHit Hit;
            if (Physics.Raycast(position, Vector3.down, out Hit, LayerMask.GetMask(new string[] { "Construction" })))
            {
                var rhEntity = Hit.GetEntity();
                if (rhEntity != null)
                {
                    hitted = true;
                }
            }
            BaseEntity rEntity = GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab", entity.transform.position, entity.GetNetworkRotation(), true);                                 // 1
            rEntity.Spawn();
            rEntity.skinID = cfg.skin;
            if (!hitted) return;
        }

        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            BaseEntity entity = info.HitEntity;
            if (entity == null) return;
            if (!entity.ShortPrefabName.Contains("recycler")) return;
            if (!cfg.recyclerpickup) return;
            if (!cfg.noauthpickup && player.IsBuildingBlocked())
            {
				if (!cfg.usenoteui)
				{
					SendReply(player, "Вы должны быть авторизированы в шкафу чтобы подобрать переработчик!");
				}
				NoteUI?.Call("DrawLockNote", player, "ОШИБКА", $"Вы должны быть авторизированы в шкафу!");
                return;
            }

            entity.Kill();
            GiveRecycler(player);
        }

        #endregion
    }
}