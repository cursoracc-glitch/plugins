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
    [Info("Vanish", "Fartus", "1.0.0")]
    [Description("Позволяет игрокам с разрешением становиться невидимыми")]
    public class Vanish : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Ссылка на картинку индикатора (.png или .jpg)")]
            public string ImageUrlIcon;

            [JsonProperty(PropertyName = "Включить звуковой эффект (true/false)")]
            public bool PlaySoundEffect;

            [JsonProperty(PropertyName = "Показать индикатор невидимости (true/false)")]
            public bool ShowGuiIcon;

            [JsonProperty(PropertyName = "Время в режиме невидимости (в секундах, 0 - отключить)")]
            public int VanishTimeout;

            [JsonProperty(PropertyName = "Включить видимость для админов (true/false)")]
            public bool VisibleToAdmin;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    ImageUrlIcon = "https://i.imgur.com/5vjIch3.png",
                    PlaySoundEffect = true,
                    ShowGuiIcon = true,
                    VanishTimeout = 0,
                    VisibleToAdmin = true
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
                PrintWarning($"Не удалось прочитать oxide/config/{Name}.json, создание нового файла конфигурации...");
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
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CantDamageBuilds"] = "Вы не можете нанести урон зданиям в режиме невидимки",
                ["CantHurtAnimals"] = "Вы не можете нанести урон животным в режиме невидимки",
                ["CantHurtPlayers"] = "Вы не можете нанести урон игрокам в режиме невидимки",
                ["CantUseTeleport"] = "Вы не можете телепортироваться в режиме невидимки",
                ["CommandVanish"] = "vanish",
                ["NotAllowed"] = "Извините, Вы не можете использовать '{0}' сейчас!",
                ["PlayersOnly"] = "Комманда '{0}' может использоваться только игроком",
                ["VanishDisabled"] = "Вы вышли из режима невидимки!",
                ["VanishEnabled"] = "Вы вошли в режим невидимки!",
                ["VanishTimedOut"] = "Таймаут режима невидимки!",
                ["NotAllowedPerm"] = "У вас нет разрешения! ({0})",
            }, this);
        }

        #endregion

        #region Initialization

        private const string defaultEffect = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab";
        private const string VANISH_PERM = "vanish.use";
		private const string VANISH_DAMAGE_PERM = "vanish.damage.all";
		private const string VANISH_ABILITIES_PERM = "vanish.abilities.all";

        private void Init()
        {
            permission.RegisterPermission(VANISH_PERM, this);
			permission.RegisterPermission(VANISH_DAMAGE_PERM, this);
			permission.RegisterPermission(VANISH_ABILITIES_PERM, this);

            AddCommandAliases("CommandVanish", "VanishCommand");

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

        private void VanishCommand(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null)
            {
                player.Reply(Lang("PlayersOnly", player.Id, command));
                return;
            }

            if (!player.HasPermission(VANISH_PERM))
            {
                Message(player, Lang("NotAllowedPerm", player.Id, VANISH_PERM));
                return;
            }

            if (config.PlaySoundEffect) Effect.server.Run(defaultEffect, basePlayer.transform.position);
            if (IsInvisible(basePlayer)) Reappear(basePlayer);
            else Disappear(basePlayer);
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
            Message(basePlayer.IPlayer, "VanishEnabled");

            if (config.VanishTimeout > 0f) timer.Once(config.VanishTimeout, () =>
            {
                if (!onlinePlayers[basePlayer].IsInvisible) return;

                Reappear(basePlayer);
                Message(basePlayer.IPlayer, "VanishTimedOut");
            });

            Subscribe();

            BaseEntity.Query.Server.RemovePlayer(basePlayer);
            Puts("Removed Player From Animal Grid");
        }

        // Hide from other players
        private object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            var basePlayer = entity as BasePlayer ?? (entity as HeldEntity)?.GetOwnerPlayer();
            if (basePlayer == null || target == null || basePlayer == target) return null;
            if (config.VisibleToAdmin && target.IPlayer.IsAdmin) return null;
            if (IsInvisible(basePlayer)) return false;

            return null;
        }

        // Hide from helis/turrets
        private object CanBeTargeted(BaseCombatEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return false;

            return null;
        }

        // Hide from the bradley APC
        private object CanBradleyApcTarget(BradleyAPC apc, BaseEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return false;

            return null;
        }

        // Hide from the patrol helicopter
        private object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer basePlayer)
        {
            if (IsInvisible(basePlayer)) return false;

            return null;
        }

        // Hide from scientist NPCs
        private object OnNpcPlayerTarget(NPCPlayerApex npc, BaseEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return 0f;

            return null;
        }

        // Hide from all other NPCs
        private object OnNpcTarget(BaseNpc npc, BaseEntity entity)
        {
            var basePlayer = entity as BasePlayer;
            if (basePlayer != null && IsInvisible(basePlayer)) return 0f;

            return null;
        }

        private void OnPlayerSleepEnded(BasePlayer basePlayer)
        {
            if (IsInvisible(basePlayer)) // TODO: Add persistence permission check
            {
                Disappear(basePlayer);
                // TODO: Send message that still vanished
            }
        }

        private object OnPlayerLand(BasePlayer player, float num)
        {
            if (IsInvisible(player))
            {
                return false;
            }
            return null;
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (IsInvisible(player))
            {
                if (permission.UserHasPermission(player.UserIDString, VANISH_ABILITIES_PERM) || player.IsImmortal())
                {
                    return true;
                }
            }
            return null;
        }

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
            Puts("Added Player From Animal Grid");
            //Add player back to Grid so AI can find it
            BaseEntity.Query.Server.AddPlayer(basePlayer);

            Message(basePlayer.IPlayer, "VanishDisabled");
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
                    new CuiRawImageComponent { Color = "1 1 1 0.3", Url = config.ImageUrlIcon },
                    new CuiRectTransformComponent { AnchorMin = "0.262 0.026",  AnchorMax = "0.3 0.092" }
                }
            });

            CuiHelper.AddUi(basePlayer, elements);
        }

        #endregion

        #region Damage Blocking

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var basePlayer = (info?.Initiator as BasePlayer) ?? entity as BasePlayer;
            if (basePlayer == null || !basePlayer.IsConnected || !onlinePlayers[basePlayer].IsInvisible) return null;

            var player = basePlayer.IPlayer;

            // Block damage to animals
            if (entity is BaseNpc)
            {
                if (player.HasPermission(VANISH_DAMAGE_PERM)) return null;

                Message(player, "CantHurtAnimals");
                return true;
            }

            // Block damage to buildings
            if (!(entity is BasePlayer))
            {
                if (player.HasPermission(VANISH_DAMAGE_PERM)) return null;

                Message(player, "CantDamageBuilds");
                return true;
            }

            // Block damage to players
            if (info?.Initiator is BasePlayer)
            {
                if (player.HasPermission(VANISH_DAMAGE_PERM)) return null;

                Message(player, "CantHurtPlayers");
                return true;
            }

            if (basePlayer == info.HitEntity)
            {
                // Block damage to self
                if (player.HasPermission(VANISH_ABILITIES_PERM))
                {
                    info.damageTypes = new DamageTypeList();
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                    return true;
                }
            }

            return null;
        }

        #endregion

        #region Weapon Blocking

        private void OnPlayerTick(BasePlayer basePlayer)
        {
            if (!onlinePlayers[basePlayer].IsInvisible) return;

            var held = basePlayer.GetHeldEntity();
            if (held != null && basePlayer.IPlayer.HasPermission(VANISH_ABILITIES_PERM)) held.SetHeld(false);
        }

        #endregion

        #region Teleport Blocking

        private object CanTeleport(BasePlayer basePlayer)
        {
            if (onlinePlayers[basePlayer] == null)
            {
                return null;
            }

            //Ignore for normal teleport plugins
            if (!onlinePlayers[basePlayer].IsInvisible)
            {
                return null;
            }

            var canTeleport = basePlayer.IPlayer.HasPermission(VANISH_ABILITIES_PERM);
            return !canTeleport ? Lang("CantUseTeleport", basePlayer.UserIDString) : null;
        }

        #endregion

        #region Persistence Handling

        private void OnPlayerInit(BasePlayer basePlayer)
        {
            // TODO: Persistence permission check and handling
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

        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));

        private bool IsInvisible(BasePlayer player) => onlinePlayers[player]?.IsInvisible ?? false;

        #endregion
    }
}
