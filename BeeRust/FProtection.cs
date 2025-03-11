using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Globalization;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Rust;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("FProtection", "King", "1.0.2")]
    class FProtection : RustPlugin
	{ 
        #region [Vars]
        private Dictionary<BasePlayer, DateTime> Cooldown = new Dictionary<BasePlayer, DateTime>();
        private Boolean _IsTime = false;
        private Single _DamageEntity = 0f;
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 2))
                {
                    config._MainSettings.ChatNotify = true;
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class ProtectSettings
        {
			[JsonProperty("Начало защиты | Часы")]
            public int HourStart;

			[JsonProperty("Начало защиты | Минуты")]
            public int MinuteStart;

			[JsonProperty("Конец защиты | Часы")]
            public int HourEnd;

			[JsonProperty("Конец защиты | Минуты")]
            public int MinuteEnd;

            [JsonProperty("Процент защиты. 1.0 - 100%")]
            public float Damage;

            [JsonIgnore]
            public TimeSpan TimeOn,TimeOff;
        }

        public class MainSettings
        {
            [JsonProperty("Раз в сколько секунд проверять активность защиты ( Секунды )")] 
            public int Timer;

            [JsonProperty("Разрешить ломать солому во время защиты")] 
            public Boolean Twigs;

            [JsonProperty("Использовать чат оповещение при повреждении постройки ( При активной защите )")] 
            public Boolean ChatNotify;
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public MainSettings _MainSettings = new MainSettings();

            [JsonProperty("Настройки защиты")]
            public List<ProtectSettings> _ProtectSettings = new List<ProtectSettings>();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _MainSettings = new MainSettings
                    {
                        Timer = 120,
                        Twigs = true,
                        ChatNotify = true,
                    }, 
                    _ProtectSettings = new List<ProtectSettings>()
                    {
                        new ProtectSettings()
                        {
                            HourStart = 22,
                            MinuteStart = 0,
                            HourEnd = 23,
                            MinuteEnd = 0,
                            Damage = 0.5f,
                        },
                        new ProtectSettings()
                        {
                            HourStart = 23,
                            MinuteStart = 15,
                            HourEnd = 12,
                            MinuteEnd = 0,
                            Damage = 1f,
                        },
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [Oxide]
		private void OnServerInitialized()
		{
            FilingTime();

            Protection();
			timer.Every(config._MainSettings.Timer, () => Protection());
		}

        private void FilingTime()
        {
            foreach (var key in config._ProtectSettings)
            {
                TimeSpan TimeOn = new TimeSpan(key.HourStart, key.MinuteStart, 0);
                TimeSpan TimeOff = new TimeSpan(key.HourEnd, key.MinuteEnd, 0);

                key.TimeOn = TimeOn;
                key.TimeOff = TimeOff;
            }
        }
        #endregion

        #region [Functional]
        private void Protection() => _IsTime = CheckTime();

		private Boolean CheckTime()
		{
            var timeNow = DateTime.Now.TimeOfDay;
            var find = config._ProtectSettings.FirstOrDefault(time => (time.TimeOn <= time.TimeOff && time.TimeOn <= timeNow && timeNow <= time.TimeOff)
                                                            || (time.TimeOn > time.TimeOff && (time.TimeOn <= timeNow || timeNow  <= time.TimeOff)));
            if (find == null)
            {
                if (_DamageEntity != 0f)
                {
                    _DamageEntity = 0f;
                    Server.Broadcast($"Защита строений изменена, дополнительное сопротивление урону <color=#9ACD32>{_DamageEntity * 100}%</color>");
                }
                return false;
            }
            else if (_DamageEntity != find.Damage)
            {
                _DamageEntity = find.Damage;
                Server.Broadcast($"Защита строений изменена, дополнительное сопротивление урону <color=#9ACD32>{_DamageEntity * 100}%</color>");
            }

            return true;
        }

		private void Protection(BaseCombatEntity entity, HitInfo info)
		{
			if (_IsTime)
			{
				BasePlayer player = info.InitiatorPlayer;
				Boolean ent = entity is BuildingBlock;
				
				if(config._MainSettings.Twigs && ent)
					if((entity as BuildingBlock).grade == BuildingGrade.Enum.Twigs)
						return;
				
		        if (ent || entity is Door || entity is SimpleBuildingBlock || entity is SamSite || entity is AutoTurret)
			    {
                    info.damageTypes.ScaleAll(1.0f - _DamageEntity);

                    if (info.damageTypes.Total() >= 0.5)
                    {
						if (Cooldown.ContainsKey(player))
							if (Cooldown[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;

                        player.ChatMessage($"Защита строений активна, дополнительное сопротивление урону <color=#9ACD32>{_DamageEntity * 100}%</color>");
                        Cooldown[player] = DateTime.Now.AddSeconds(5);
                    }
			    }
			}
		}
        #endregion

        #region [Rust]
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info?.InitiatorPlayer == null) return;
			
			Protection(entity, info);
		}
        #endregion

        #region [ChatCommand]
        [ChatCommand("bps")]
        private void cmdShowBPS(BasePlayer player, string command, string[] args)
        {
            String Text = string.Empty;
            String protectionText = string.Empty;
            int i = 1;
            if (_DamageEntity == 0f)
            {
                foreach (var protect in config._ProtectSettings)
                {
                    protectionText += string.Format("<color=#9ACD32>{0:00}:00</color> до <color=#9ACD32>{1:00}:00</color>{2:00}", protect.HourStart, protect.HourEnd, i != config._ProtectSettings.Count ? " а также с " : "!!");
                    i++;
                }
                Text += $"Ночная защита не активна! Она будет действовать с {protectionText}";
            }
            else
            {
                Text += $"Ночная защита активна! Дополнительное сопротивление урону <color=#9ACD32>{_DamageEntity * 100}%</color>";
            }

            player.ChatMessage(Text);
        }
        #endregion
    }
}