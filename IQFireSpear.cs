using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQFireSpear", "Sempai#3239", "0.0.1")]
    [Description("Огненное копье,при ударе есть шанс оставить искру,при броске поджигает под собой все")]
    class IQFireSpear : RustPlugin
    {
        [PluginReference] Plugin IQChat;

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("При поджигании копья,ломать его постепенно(true/false)")]
            public bool ConditionUse;
            [JsonProperty("SkinID для предмета(Пример : 2000653461)")]
            public ulong SkinID;
            [JsonProperty("DisplayName для предмета")]
            public string DisplayName;
            [JsonProperty("Shortname для предмета(обязательно , которое можно кинуть и нанести урон)")]
            public string Shortname;
            [JsonProperty("Шанс возгарания при ударе копьем в игрока")]
            public int RareFireDamagePlayer;
            [JsonProperty("Шанс возгарания при броске копья")]
            public int RareFireThrow;
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    ConditionUse = true,
                    SkinID = 2006575943,
                    Shortname = "spear.wooden",
                    DisplayName = "Огненное копье",
                    RareFireDamagePlayer = 10,
                    RareFireThrow = 100,
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #85" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! 321!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (attacker == null) return;
            if (info == null) return;

            if (info.damageProperties.name == "Damage.Throwable")
            {
                if (info.Weapon.skinID == config.SkinID)
                    if (config.RareFireThrow >= UnityEngine.Random.Range(0, 100))
                    {
                        BaseEntity FireBallArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/fireball.prefab", info.HitPositionWorld);
                        FireBallArrow?.Spawn();
                    }
            }

            if (info.damageProperties.name == "Damage.Melee")
            {
                if (info.HitEntity == null) return;
                if (info.HitEntity.ShortPrefabName == "campfire")
                {
                    if (info.HitEntity.HasFlag(BaseEntity.Flags.On))
                    {
                        Item ActiveWeapon = info.Weapon.GetItem();
                        if (ActiveWeapon == null) return;
                        if (info.Weapon.skinID == config.SkinID) return;
                        if (ActiveWeapon.info.shortname != config.Shortname) return;

                        ActiveWeapon.name = config.DisplayName;
                        ActiveWeapon.skin = config.SkinID;
                        info.Weapon.skinID = config.SkinID;
                        if (config.ConditionUse)
                            timer.Every(1f, () => { ActiveWeapon.condition--; });
                        SendChat(lang.GetMessage("ON_FIRE", this), attacker);
                        return;
                    }
                }

                if (info.Weapon.skinID == config.SkinID)
                {
                    BasePlayer target = (BasePlayer)info.HitEntity;
                    if (target == null) return;
                    if (config.RareFireDamagePlayer >= UnityEngine.Random.Range(0, 100))
                    {
                        BaseEntity FireBallArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/fireball.prefab", target.transform.position);
                        FireBallArrow?.Spawn();
                    }
                }
            }
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ON_FIRE"] = "Вы подожгли копье,теперь при броске этого копья ,место куда оно упадет загорится!",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ON_FIRE"] = "Вы подожгли копье,теперь при броске этого копья ,место куда оно упадет загорится!",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Helps
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion
    }
}

