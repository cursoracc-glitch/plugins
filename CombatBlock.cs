using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CombatBlock", "King", "1.0.0")]
    public class CombatBlock : RustPlugin
    {
        #region [Vars]

        private const string Layer = "CombatBlock.Layer";
        private static CombatBlock plugin;
        private readonly Dictionary<BasePlayer, CombatManager> _components = new Dictionary<BasePlayer, CombatManager>();

        #endregion

        #region Configuration


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
            if (config.PluginVersion < new VersionNumber(1, 0, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }

            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
            
        private class PluginConfig
        {
            [JsonProperty("Настройки плагина")]
            public Settings _Settings;

            [JsonProperty("Версия конфигурации")] 
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                 {
                    _Settings = new Settings()
                    {
                        timeCombatBlock = 15f,
                        BlackListCommands = new List<string>()
                        {
                            "/bp",
                            "/info",
                            "/outpost"
                        },
                        blockFirePlayer = true,
                        blockHitPlayer = true,
                    },
                    PluginVersion = new VersionNumber(),
                };
            }
        }

        public class Settings
        {
            [JsonProperty("Длительность комбат-блока")]
            public float timeCombatBlock;

            [JsonProperty("Черный список команд какие запрещены при комбат блоке")]
            public List<string> BlackListCommands;

            [JsonProperty("Блокировать при попадании по игроку ?")]
            public bool blockFirePlayer;

            [JsonProperty("Блокировать при получении урона от игрока ?")]
            public bool blockHitPlayer;
        }

        #endregion

        #region [Oxide]

        private void Init()
		{
			plugin = this;
        }

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, Layer);
            }

			Array.ForEach(_components.Values.ToArray(), combat =>
			{
				if (combat != null)
					combat.Kill();
			});

            plugin = null;
        }

        #endregion

        #region [Rust-Api]

        private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (info == null || info.HitEntity == null || IsNPC(attacker)) return;
            if (info.HitEntity is BasePlayer)
            {
                BasePlayer target = info.HitEntity.ToPlayer();
                if (target == null || IsNPC(target)) return;
                if (config._Settings.blockFirePlayer)
                {
                    StartingCombatBlock(attacker);
                }
                if (config._Settings.blockHitPlayer)
                {
                    StartingCombatBlock(target);
                }
            }
        }

        private object OnUserCommand(IPlayer ipl, string command, string[] args)
        {
            if (ipl == null || !ipl.IsConnected) return null;
            var player = ipl.Object as BasePlayer;
            command = command.Insert(0, "/");
            if (player == null || !IsCombatBlocked(player)) return null;
            if (config._Settings.BlackListCommands.Contains(command.ToLower()))
            {
                player.ChatMessage("Вы не можете использовать команду во время комбат блока!");
                return false;
            }
            return null;
        }

        #endregion

        #region [Component]

        private class CombatManager : FacepunchBehaviour
        {
            #region [Vars]

            private BasePlayer _player;

            private float _startTime;

            private bool _started = true;

            private float _cooldown;

            #endregion

            #region [Init]

			private void Awake()
			{
				_player = GetComponent<BasePlayer>();

				plugin._components[_player] = this;

				enabled = false;
			}

			public void Init()
			{
				_startTime = Time.time;

				_cooldown = plugin.config._Settings.timeCombatBlock;

				MainUi();

				enabled = true;

				_started = true;
			}

            #endregion

            #region [Ui]

            public void MainUi()
            {
                var container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-39.256 117.37", OffsetMax = "-0.004 145.839" }
                }, "Hud", Layer);

                container.Add(new CuiPanel
                {
                    Image = { Color = "0.2745098 0.1921569 0.1921569 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-126.027 -14.234", OffsetMax = "19.623 14.234" }
                }, Layer, Layer + ".Main");

				CuiHelper.DestroyUi(_player, Layer);
				CuiHelper.AddUi(_player, container);
            }

            private void UpdateUi()
            {
                var container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0.2745098 0.1921569 0.1921569 0" },
                    RectTransform ={ AnchorMin = "0 0", AnchorMax = "1 1" }
                }, Layer + ".Main", Layer + ".Main" + ".Update");

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Main" + ".Update",
                    Components = 
                    {
                        new CuiTextComponent { Text = $"Комбат блок:  <color=#EEBF00FF>{GetLeftTime()} сек.</color>", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.203 -12.617", OffsetMax = "72.827 7.1" }
                    }
                });

                container.Add(new CuiPanel
                {
                    Image = { Color = "0.9294118 0.7490196 0 1" },
                    RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-72.7 -21.609", OffsetMax = "72.7 -14.535" }
                },Layer + ".Main" + ".Update", Layer + ".Main" + ".Update" + ".LinePanel");

                var progress = (_startTime + _cooldown - Time.time) / _cooldown;
                if (progress > 0)
                {
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = $"{progress} 0", AnchorMax = $"1 0.84",
						},
						Image =
						{
                            Color = "0.3960785 0.3960785 0.3764706 1"
						}
					}, Layer + ".Main" + ".Update" + ".LinePanel");
                }

				CuiHelper.DestroyUi(_player, Layer + ".Main" + ".Update");
				CuiHelper.AddUi(_player, container);
            }

            #endregion

            #region [Update]

			private void FixedUpdate()
			{
				if (!_started) return;

				var timeLeft = Time.time - _startTime;
				if (timeLeft > _cooldown)
				{
					Kill();
					return;
				}

				UpdateUi();
			}

            #endregion

            #region [Func]

			private int GetLeftTime()
			{
				return Mathf.RoundToInt(_startTime + _cooldown - Time.time);
			}

            #endregion

            #region [Destroy]

            public void DestroyComp() => OnDestroy();
			private void OnDestroy()
			{
				CancelInvoke();

				CuiHelper.DestroyUi(_player, Layer);

				plugin?._components.Remove(_player);

				Destroy(this);
			}

			public void Kill()
			{
				enabled = false;

				_started = false;

				DestroyImmediate(this);
			}

            #endregion
        }

        #endregion

        #region [Func]

        private static bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L)) return true;
            return false;
        }

		private CombatManager AddOrGetBuild(BasePlayer player)
		{
			CombatManager combat;
			if (_components.TryGetValue(player, out combat))
				return combat;

			combat = player.gameObject.AddComponent<CombatManager>();
			return combat;
		}

		private CombatManager IsCombatBlocked(BasePlayer player)
		{
			CombatManager combat;
			return _components.TryGetValue(player, out combat) ? combat : null;
		}

        private void StartingCombatBlock(BasePlayer player)
        {
            Global.Runner.StartCoroutine(StartUpdate(player));

            AddOrGetBuild(player).Init();
        }

        private IEnumerator StartUpdate(BasePlayer player)
        {
            yield return CoroutineEx.waitForFixedUpdate;
        }

        #endregion
    }
}