
using System.Collections.Generic;
using System.Linq;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vanish", "GrazyCat", "0.7.1")]
    [Description("Ваниш как у москвы , и даже круче ! ")]
    public class Vanish : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {


            [JsonProperty(PropertyName = "Включить звуковой эффект ? (true/false)")]
            public bool PlaySoundEffect;

            [JsonProperty(PropertyName = "Показать индикатор невдимости ? (true/false)")]
            public bool ShowGuiIcon;

            [JsonProperty(PropertyName = "Включить видимость для админов ? (true/false)")]
            public bool VisibleToAdmin;

            [JsonProperty(PropertyName = "Выключить режим призрака  ? (true/false)")]
            public bool Ghost;


            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    PlaySoundEffect = true,
                    ShowGuiIcon = true,
                    VisibleToAdmin = true,
                    Ghost = false
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.PlaySoundEffect == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Не найден конфиг... Создам новый ! ");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CantDamageBuilds"] = "<size=15><color=red>Вы не можете повредить обьекты в инвизе!</color></size>",
                ["CantHurtAnimals"] = "<size=15><color=red>Вы не можете убивать животных в инвизе!</color></size>",
                ["CantHurtPlayers"] = "<size=15><color=red>Вы не можете нанести урон по игрока!</color></size>",
                ["VanishCommand"] = "vanish",
                ["NotAllowed"] = "<size=15><color=red>Что то пошло не так !</color></size>",
                ["PlayersOnly"] = "<size=15><color=red>Команда '{0}' может использоваться только игроком!</color></size>",
                ["VanishDisabled"] = "<size=15>Вы <color=red>ВЫКЛЮЧИЛИ</color> инвиз !</size>",
                ["VanishEnabled"] = "<size=15>Вы <color=green>ВКЛЮЧИЛИ</color> инвиз !</size>",
                ["NotAllowedPerm"] = "<size=15><color=red>Что то пошло не так !</color></size>",
                ["LootBlock"] = "<size=15><color=RED>Лутание в инвизе не возможно!</color></size>",
                ["AuthBlock"] = "<size=15><color=RED>Авторизация в инвизе не возможна!</color></size>",
                ["ClAuthBlock"] = "<size=15><color=RED>Очистка в инвизе не возможна!</color></size>",
                ["UpBlock"] = "<size=15><color=RED>Апгрейд  в инвизе не возможен!</color></size>",
                ["DevAuthBlock"] = "<size=15><color=RED>Деавторизация в инвизе не возможна!</color></size>",
                ["RemoveBlock"] = "<size=15><color=RED>Авторизация в инвизе не возможна!</color></size>",
                ["BuildBlock"] = "<size=15><color=RED>Строительство в инвизе не возможно!</color></size>",
                ["PickUpBlock"] = "<size=15><color=RED>Забарть предмет в инвизе не возможно!</color></size>",
                ["GatherBlock"] = "<size=15><color=RED>Добыча  в инвизе не возможна!</color></size>",
                ["DropBlock"] = "<size=15><color=RED>Выкинуть предмет в инвизе не возможно!</color></size>"
            }, this);
        }

        #endregion

        #region Initialization

        private const string defaultEffect = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab";
        private const string permGhostOff = "vanish.GhostOff";
        private const string permUse = "vanish.use";


        private void Init()
        {
            permission.RegisterPermission(permGhostOff, this);
            permission.RegisterPermission(permUse, this);


            AddCommandAliases("VanishCommand", "VanishChatCmd");



            Unsubscribe();
        }

        private void Subscribe()
        {
            Subscribe(nameof(CanNetworkTo));
            Subscribe(nameof(CanBeTargeted));
            Subscribe(nameof(CanBradleyApcTarget));
            Subscribe(nameof(OnNpcPlayerTarget));
            Subscribe(nameof(OnNpcTarget));
            Subscribe(nameof(OnEntityTakeDamage));
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(OnPlayerLand));
        }

        private void Unsubscribe()
        {
            Unsubscribe(nameof(CanNetworkTo));
            Unsubscribe(nameof(CanBeTargeted));
            Unsubscribe(nameof(CanBradleyApcTarget));
            Unsubscribe(nameof(OnNpcPlayerTarget));
            Unsubscribe(nameof(OnNpcTarget));
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnPlayerLand));
        }

        #endregion

        #region Data Storage

        private class OnlinePlayer
        {
            public BasePlayer Player;
            public bool IsInvisible;
        }

        [OnlinePlayers]
        private Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        #endregion

        #region Commands

        private void VanishChatCmd(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null)
            {
                player.Reply(Lang("PlayersOnly", player.Id, command));
                return;
            }

            if (!player.HasPermission(permUse))
            {
                Message(basePlayer, Lang("NotAllowedPerm", player.Id, permUse));
                return;
            }

            if (config.PlaySoundEffect) Effect.server.Run(defaultEffect, basePlayer.transform.position);
            if (IsInvisible(basePlayer)) Reappear(basePlayer);
            else Disappear(basePlayer);
        }

        // Запрет лутания 
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return;
			
            if (IsInvisible(player))
            {
                StopLooting(player);
                return;
            }
        }

        private void StopLooting(BasePlayer player)
        {
            NextTick(player.EndLooting);
            Message(player, "LootBlock");
        }

        //Запрет добычи ресов
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {

            BasePlayer player = entity.ToPlayer();
			
            if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return;
			
            if (IsInvisible(player))
            {
                item.amount = 0;
                Message(player, "GatherBlock");
                return;
            }
        }

        void OnItemDropped(BasePlayer player, Item item)
        {
            Message(player, "DropBlock");
            return;
        }

        #endregion

        #region Vanishing Act

        private void Disappear(BasePlayer basePlayer)
        {
            var connections = new List<Connection>();
            foreach (var target in BasePlayer.activePlayerList)
            {
                if (basePlayer == target || !target.IsConnected) continue;
                if (config.VisibleToAdmin && target.IPlayer.IsAdmin) continue;
                connections.Add(target.net.connection);
            }

            var held = basePlayer.GetHeldEntity();
            if (held != null)
            {
                held.SetHeld(false);
                held.UpdateVisiblity_Invis();
                held.SendNetworkUpdate();
            }

            if (Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                Net.sv.write.EntityID(basePlayer.net.ID);
                Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                Net.sv.write.Send(new SendInfo(connections));
            }

            basePlayer.UpdatePlayerCollider(false);

            if (config.ShowGuiIcon) VanishGui(basePlayer);
            onlinePlayers[basePlayer].IsInvisible = true;
            Message(basePlayer, "VanishEnabled");


            Subscribe();

            BaseEntity.Query.Server.RemovePlayer(basePlayer);

        }

        // Скрыть от игрока
        private object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            var basePlayer = entity as BasePlayer ?? (entity as HeldEntity)?.GetOwnerPlayer();
            if (basePlayer == null || target == null || basePlayer == target) return null;
            if (config.VisibleToAdmin && target.IPlayer.IsAdmin) return null;
            if (IsInvisible(basePlayer)) return false;
            return null;
        }

        // Скрыть от верта и турелей
        private object CanBeTargeted(BaseCombatEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return false;

            return null;
        }

        // Скрыть от верта и турелей танка
        private object CanBradleyApcTarget(BradleyAPC apc, BaseEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return false;

            return null;
        }

        // Скрыть от верта и турелей танка
        private object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer basePlayer)
        {
            if (IsInvisible(basePlayer)) return false;

            return null;
        }

        // Скрыть от животных
        private object OnNpcPlayerTarget(NPCPlayerApex npc, BaseEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return 0f;

            return null;
        }

        // Скрыть от животных
        private object OnNpcTarget(BaseNpc npc, BaseEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return 0f;

            return null;
        }
        // Скрыть слиперов
        private void OnPlayerSleepEnded(BasePlayer basePlayer)
        {
            if (IsInvisible(basePlayer))
            {
                Disappear(basePlayer);
            }
        }
		// Скрыть слиперов
        private object OnPlayerLand(BasePlayer player, float num)
        {
            if (IsInvisible(player))
            {
                return false;
            }
            return null;
        }
		// Запрет блокировки
        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {

            return null;
        }
        //Запрет на постройку
        private object CanBuild(Planner plan, Construction prefab)
        {
            var player = plan.GetOwnerPlayer();
            if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
            if (config.Ghost)
            {
                return null;
            }

            if (IsInvisible(player))
            {
                Message(player, "BuildBlock");
                return false;
            }
            return null;
        }
        // Запрет на авторизацию
        private object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;

            if (config.Ghost)
            {
                return null;
            }

            if (IsInvisible(player))
            {
                Message(player, "AuthBlock");
                return false;
            }
            return null;
        }

        // Запрет на очистку авторизации
        private object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }

            if (IsInvisible(player))
            {
                Message(player, "ClAuthBlock");
                return false;
            }
            return null;
        }
        //Запрет на деавторизацию из шакафа 
        private object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;

            if (config.Ghost)
            {
                return null;
            }

            if (IsInvisible(player))
            {
                Message(player, "DevAuthBlock");
                return false;
            }
            return null;
        }
        //Запрет на апгрейд 
        private object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }

            if (IsInvisible(player))
            {
                Message(player, "UpBlock");
                return false;
            }
            return null;
        }

        // Запрет на удаление 
        private object OnStructureDemolish(BaseCombatEntity entity, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }

            if (IsInvisible(player))
            {
                Message(player, "RemoveBlock");
                return false;
            }
            return null;
        }

        //Запрет на авторизацию в турели
        private object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }
			
            if (IsInvisible(player))
            {
                Message(player, "AuthBlock");
                return false;
            }
            return null;
        }

        // Запрет на подьем вещей
        private object OnItemPickup(Item item, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }
			
            if (IsInvisible(player))
            {
                return false;
            }
            return null;
        }
        //Заперт на выброс предмета 
        private object OnItemAction(Item item, string action, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }
            
            if (IsInvisible(player))
            {
                OnItemDropped(player, item);
                return false;
            }
            return null;
        }

        // Запрет на подьем предметов
        private object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
			if (permission.UserHasPermission(player.UserIDString, permGhostOff)) return null;
			
            if (config.Ghost)
            {
                return null;
            }
            
            if (IsInvisible(player))
            {
                Message(player, "PickUpBlock");
                return false;
            }
            return null;
        }

        // Запрет на испольвание таблички
        // void CanUpdateSign(BasePlayer player, Signage sign)
        // {	
        // if (config.Ghost) 
        // {	
        // return null;
        // }
        // // if (player.HasPermission(permGhostoff)) return null;
        // if (IsInvisible(player))
        // {
        // return true;
        // }
        // return null;
        // }

        #endregion

        #region Reappearing Act

        private void Reappear(BasePlayer basePlayer)
        {
            onlinePlayers[basePlayer].IsInvisible = false;
            basePlayer.SendNetworkUpdate();

            var held = basePlayer.GetHeldEntity();
            if (held != null)
            {
                held.UpdateVisibility_Hand();
                held.SendNetworkUpdate();
            }

            basePlayer.UpdatePlayerCollider(true);

            string gui;
            if (guiInfo.TryGetValue(basePlayer.userID, out gui)) CuiHelper.DestroyUi(basePlayer, gui);

            BaseEntity.Query.Server.AddPlayer(basePlayer);

            Message(basePlayer, "VanishDisabled");
            if (onlinePlayers.Values.Count(p => p.IsInvisible) <= 0) Unsubscribe(nameof(CanNetworkTo));
        }

        #endregion

        #region GUI Indicator

        private Dictionary<ulong, string> guiInfo = new Dictionary<ulong, string>();

        private void VanishGui(BasePlayer basePlayer)
        {
            string gui;
            if (guiInfo.TryGetValue(basePlayer.userID, out gui)) CuiHelper.DestroyUi(basePlayer, gui);

            var elements = new CuiElementContainer();
            guiInfo[basePlayer.userID] = CuiHelper.GetGuid();

            elements.Add(new CuiElement
            {
                Name = guiInfo[basePlayer.userID],
                Components =
                {
                    new CuiRawImageComponent { Color = "1 1 1 1", Url = "https://i.imgur.com/DjlwRwN.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.168 0.018",  AnchorMax = "0.24 0.13" }
                }
            });

            CuiHelper.AddUi(basePlayer, elements);
        }

        #endregion

        #region Damage Blocking

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
			if (entity == null|| info == null) return null;
            var initiator = info?.InitiatorPlayer;
            if (initiator == null) return null;
            var basePlayer = (info?.Initiator as BasePlayer) ?? entity as BasePlayer;
            if (basePlayer == null || !basePlayer.IsConnected || !onlinePlayers[basePlayer].IsInvisible) return null;
            if (permission.UserHasPermission(initiator.UserIDString, permGhostOff)) return null;
            if (entity is BaseNpc)
            {
                Message(basePlayer, "CantHurtAnimals");
                return true;
            }

            // Блок строений
            if (!(entity is BasePlayer))
            {
                Message(basePlayer, "CantDamageBuilds");
                return true;
            }

            // Блок игроков
            if (info?.Initiator is BasePlayer)
            {
                Message(basePlayer, "CantHurtPlayers");
                return true;
            }

            return null;
        }

        #endregion


        #region Weapon Blocking

        private void OnPlayerTick(BasePlayer basePlayer)
        {
            if (!onlinePlayers[basePlayer].IsInvisible) return;

            var held = basePlayer.GetHeldEntity();
        }

        #endregion

        #region Cleanup

        private void Unload()
        {
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                string gui;
                if (guiInfo.TryGetValue(basePlayer.userID, out gui)) CuiHelper.DestroyUi(basePlayer, gui);
            }
        }

        #endregion

        #region Helpers

        private void AddCommandAliases(string key, string command)
        {
            foreach (var language in lang.GetLanguages(this))
            {
                var messages = lang.GetMessages(language, this);
                foreach (var message in messages.Where(m => m.Key.Equals(key))) AddCovalenceCommand(message.Value, command);
            }
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(BasePlayer player, string key, params object[] args) =>
            Player.Message(player, Lang(key, player.UserIDString), null);

        private bool IsInvisible(BasePlayer player) => onlinePlayers[player]?.IsInvisible ?? false;

        #endregion
    }
}
