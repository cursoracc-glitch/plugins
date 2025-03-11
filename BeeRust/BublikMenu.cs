using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BublikMenu", "King", "1.0.0")]
    public class BublikMenu : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin ImageLibrary = null;

        private const String Layer = "BubliliMenu.Layer";
        private List<UInt64> ButtonOpen = new List<UInt64>();

        private Dictionary<string, string> EventColor = new Dictionary<string, string>
        {
            ["PatrolHelicopter"] = "1 1 1 0.9",
            ["BradleyAPC"] = "1 1 1 0.9"
        };

        private Dictionary<String, String> ButtonList = new Dictionary<String, String>()
        {
            ["Клан"] = "/clan",
            ["КланТоп"] = "/ctop",
            ["Топ"] = "/top",
            ["Прицелы"] = "/hair",
            ["Репорты"] = "/report"
            
        };
        #endregion

        #region [ImageLibrary]
        private Boolean HasImage(String imageName, UInt64 imageId = 0) => (Boolean)ImageLibrary.Call("HasImage", imageName, imageId);
        private Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private String GetImage(String shortname, UInt64 skin = 0) => (String)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [Oxide-Api]
        private void OnServerInitialized()
        {
             AddImage("https://cdn.discordapp.com/attachments/1092142749035278414/1175407539597344808/pchela_9vpufk9409gm_64.png?ex=656b1e93&is=6558a993&hm=6980a10c9198e77bc980b67642dfa98ea055a26309ec266d6e3c43a2fa54e243&", $"{Name}.Online");
            AddImage("https://cdn.discordapp.com/attachments/1092142749035278414/1175406934778708028/icons8-honey-jar-64.png?ex=656b1e03&is=6558a903&hm=e3e4df45a561f4bd2dd41cd6d8bcce40f6159a58306d75ddb721d7d646cc160e&", $"{Name}.Button");
            AddImage("https://cdn.discordapp.com/attachments/1092142749035278414/1175363589943603210/8nx4pnW.png?ex=656af5a5&is=655880a5&hm=f9790fe542d7b608c93e989c74df6b0a57d6383a3d600bdfa4684a1becb0e870&", $"{Name}.PatrolHelicopter");
            AddImage("https://cdn.discordapp.com/attachments/1092142749035278414/1175363500902723595/bqB9Gkb.png?ex=656af58f&is=6558808f&hm=298194b03e80f042a70310ba292353fd3cb6719dccc543a15d569b3068cfda7c&", $"{Name}.BradleyAPC");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

			foreach (var entity in BaseNetworkable.serverEntities)
				OnEntitySpawned(entity as BaseEntity);
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer + ".Button");
                CuiHelper.DestroyUi(player, Layer + ".Online");
                CuiHelper.DestroyUi(player, Layer + ".Event");
            }
        }
        #endregion
        
        #region [Rust-Api]
        private void OnPlayerConnected(BasePlayer player)
        {
			if (player.IsReceivingSnapshot || player.IsSleeping())
			{
				timer.In(1, () => OnPlayerConnected(player));
				return;
			}

            InitializeBublikMenu(player);
            UpdateOnline();
        }

		private void OnPlayerDisconnected(BasePlayer player)
		{
            if (ButtonOpen.Contains(player.userID))
                ButtonOpen.Remove(player.userID);

			timer.In(1f, UpdateOnline);
		}

		private void OnEntitySpawned(BaseEntity entity)
		{
			EntityHandle(entity, true);
		}
		private void OnEntityKill(BaseEntity entity)
		{
			EntityHandle(entity, false);
		}
        #endregion

        #region [Functional]
        private void InitializeBublikMenu(BasePlayer player)
        {
			CuiElementContainer container = new CuiElementContainer();

            ButtonBublikMenu(ref container, player);
            OnlineBublikMenu(ref container, player);
            EventBublikMenu(ref container, player);

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void ButtonBublikMenu(ref CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "4 -42.5", OffsetMax = "42.5 -4" },
                Image = { Color = "1 0.96 0.88 0.15" },
            }, "Overlay", Layer + ".Button");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Button",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{Name}.Button") },
                    new CuiRectTransformComponent { AnchorMin = "0.125 0.125", AnchorMax = "0.875 0.875" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "BublikiButton" },
                Text = { Text = "" }
            }, Layer + ".Button");

            CuiHelper.DestroyUi(player, Layer + ".Button");
        }

        private void OnlineBublikMenu(ref CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "46.5 -42.5", OffsetMax = "85 -4" },
                Image = { Color = "1 0.96 0.88 0.15" },
            }, "Overlay", Layer + ".Online");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Online",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{Name}.Online") },
                    new CuiRectTransformComponent { AnchorMin = "0.125 0.25", AnchorMax = "0.875 0.925" }
                }
            });

			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.32" },
                Text = { Text = $"{BasePlayer.activePlayerList.Count}", Align = TextAnchor.MiddleCenter, FontSize = 9, Color = "1 1 1 1" }
            }, Layer + ".Online", Layer + ".Online" + ".Text");

            CuiHelper.DestroyUi(player, Layer + ".Online");
        }

        private void EventBublikMenu(ref CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1" },
                Image = { Color = "0 0 0 0" },
            }, "Overlay", Layer + ".Event");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "89 -18.9", OffsetMax = "103.2 -4.7" },
                Image = { Color = "1 0.96 0.88 0.15" },
            }, Layer + ".Event", Layer + ".Event" + ".HeliCopter");

            container.Add(new CuiElement
            {
                Name = Layer + ".Event" + ".HeliCopter" + ".Image",
                Parent = Layer + ".Event" + ".HeliCopter",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{Name}.PatrolHelicopter"), Color = EventColor["PatrolHelicopter"] },
                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "89 -36.1", OffsetMax = "103.2 -21.9" },
                Image = { Color = "1 0.96 0.88 0.15" },
            }, Layer + ".Event", Layer + ".Event" + ".BradleyAPC");

            container.Add(new CuiElement
            {
                Name = Layer + ".Event" + ".BradleyAPC" + ".Image",
                Parent = Layer + ".Event" + ".BradleyAPC",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{Name}.BradleyAPC"), Color = EventColor["BradleyAPC"] },
                    new CuiRectTransformComponent { AnchorMin = "0.13 0.1", AnchorMax = "0.85 0.9" }
                }
            });

            CuiHelper.DestroyUi(player, Layer + ".Event");
        }

        private void ButtonsBublikMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1" },
				Image = { Color = "0 0 0 0" }
			}, Layer + ".Button", Layer + ".Buttons");

            Single ySwitchMin = -66.5f;
            Single ySwitchMax = -46.5f;

            foreach (KeyValuePair<String,String> button in ButtonList)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"0 {ySwitchMin}", OffsetMax = $"81 {ySwitchMax}"
					},
					Image = { Color = "1 0.96 0.88 0.15" }
				}, Layer + ".Buttons", Layer + ".Buttons" + $".{button.Key}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = button.Key, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 12, Color = "1 1 1 1" }
                }, Layer + ".Buttons" + $".{button.Key}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"playerSendly chat.say {button.Value}" },
                    Text = { Text = "" }
                }, Layer + ".Buttons" + $".{button.Key}");

                ySwitchMin += -24f;
                ySwitchMax += -24f;
            }

            CuiHelper.DestroyUi(player, Layer + ".Buttons");
            CuiHelper.AddUi(player, container);
        }

        private void UpdateOnline()
        {
            CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.32" },
                Text = { Text = $"{BasePlayer.activePlayerList.Count}", Align = TextAnchor.MiddleCenter, FontSize = 11, Color = "1 1 1 1" }
            }, Layer + ".Online", Layer + ".Online" + ".Text");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer + ".Online" + ".Text");
                CuiHelper.AddUi(player, container);
            }
        }

        private void EntityHandle(BaseEntity entity, bool spawn)
		{
            if (entity == null) return;

            if (entity is PatrolHelicopter)
            {
                EventColor["PatrolHelicopter"] = spawn ? "0.97 0.62 0.62 1" : "1 1 1 0.9";

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = Layer + ".Event" + ".HeliCopter" + ".Image",
                    Parent = Layer + ".Event" + ".HeliCopter",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"{Name}.PatrolHelicopter"), Color = EventColor["PatrolHelicopter"] },
                        new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                    }
                });

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, Layer + ".Event" + ".HeliCopter" + ".Image");
                    CuiHelper.AddUi(player, container);
                }
            }

            if (entity is BradleyAPC)
            {
                EventColor["BradleyAPC"] = spawn ? "1.00 0.84 0.52 1" : "1 1 1 0.9";

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = Layer + ".Event" + ".BradleyAPC" + ".Image",
                    Parent = Layer + ".Event" + ".BradleyAPC",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"{Name}.BradleyAPC"), Color = EventColor["BradleyAPC"] },
                        new CuiRectTransformComponent { AnchorMin = "0.13 0.1", AnchorMax = "0.85 0.9" }
                    }
                });

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, Layer + ".Event" + ".BradleyAPC" + ".Image");
                    CuiHelper.AddUi(player, container);
                }
            }
		}
        #endregion

        #region [ConsoleCommand]
		[ConsoleCommand("BublikiButton")]
		private void cmdBublikiButton(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			if (player == null) return;

			if (ButtonOpen.Contains(player.userID))
			{
				CuiHelper.DestroyUi(player, Layer + ".Buttons");
				ButtonOpen.Remove(player.userID);
			}
			else
			{
				ButtonsBublikMenu(player);
				ButtonOpen.Add(player.userID);
			}
		}

		[ConsoleCommand("playerSendly")]
		private void SendlyPlayer(ConsoleSystem.Arg args)
		{
			if (args.Player() != null)
			{
				var player = args.Player();
				var convertcmd =
					$"{args.Args[0]}  \" {string.Join(" ", args.Args.ToList().GetRange(1, args.Args.Length - 1))}\" 0";
				player.SendConsoleCommand(convertcmd);
			}
		}
        #endregion
    }
}