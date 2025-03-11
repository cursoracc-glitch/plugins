using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins  
{
    [Info("CapsuleSystem", "https://topplugin.ru/", "1.0.0")]
    public class CapsuleSystem : RustPlugin
    {
        #region Fields
        public string Layer = "UI.Capsule";
        public HashSet<uint> openCrates = new HashSet<uint>();
        #endregion

        #region Hooks
		#region Experience
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is LootContainer && entity.net != null)
            {
                if (!openCrates.Add(entity.net.ID)) return;                
                var openName = "OpenCrate";
                if (entity.name.Contains("crate_elite.prefab"))
                    openName = "OpenEliteCrate";
                else if (entity.name.Contains("crate_normal.prefab"))
                    openName = "OpenMilitaryCrate";                
                GiveExperiencePlayer(player, _config.gainingExperience[openName]); 
            }
        }

        object OnCollectiblePickup(Item item, BasePlayer player)
        {
            GiveExperiencePlayer(player, _config.gainingExperience["Crop"]); 
            return null;
        }
        
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            GiveExperiencePlayer(player, _config.gainingExperience["Gather"]);
            return null;
        }
        
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null) return; 
            var player = info.InitiatorPlayer;
 
            if (player == null) return;
            if (player.IsNpc) return; 
            if (entity == null) return;

            if (entity.name.Contains("corpse"))
                return; 

            if (entity.name.Contains("npc")) GiveExperiencePlayer(player, _config.gainingExperience["KillNPC"]);
            else if (entity.name.Contains("barrel"))
            {
                GiveExperiencePlayer(player, _config.gainingExperience["DestroyBarrel"]);
            }
            else if (entity.name.Contains("agents/"))
            {
                GiveExperiencePlayer(player, _config.gainingExperience["KillAnimal"]);
            }
            else if (entity is BradleyAPC)
            {
                GiveExperiencePlayer(player, _config.gainingExperience["DestroyBradley"]);
            }
            else if (entity is BaseHelicopter)
            {
                GiveExperiencePlayer(player, _config.gainingExperience["DestroyHeli"]); 
            }
        }
		
        private void GiveExperiencePlayer(BasePlayer player, double amount)
        {
            if (player.IsNpc) return;
            var playerInfo = GetPlayerInfo(player.userID);
            var multi = _config.permission?.Max(p => permission.UserHasPermission(player.UserIDString, p.Key) ? p.Value : 0f);
            if (multi > 0f) amount *= (float)multi;
            if (amount > 0.5)
            {
                player?.SendConsoleCommand("gametip.hidegametip");
            }

            var currentlyXP = playerInfo.XP;
            if (currentlyXP + amount >= 150)
            {
                amount = (currentlyXP + amount) - 150;
                playerInfo.balance += 5;
                playerInfo.XP = 0;
                player.Command("chat.add2", 0, 0, $"<color=#afafaf>Вам было выдано 5$ на /capsule</color>", "Система", "#af5", 1);
            }            
            playerInfo.XP += amount;
            if (amount > 0.5)
            {
                player?.SendConsoleCommand("gametip.showgametip", $"Получено {amount} XP для капсул");
                timer.Once(2f, () => player?.SendConsoleCommand("gametip.hidegametip"));
            }
        }

		#endregion

        object OnItemAction(Item item, string action, BasePlayer player)
        {
			if (!action.Contains("unwrap")) return null;
            if (player == null) return null;
            if (player.IsNpc) return null;
            if (item == null) return null;
            if (item.name == null) return null;
			CapsuleInfo capsule = _config.items.FirstOrDefault(x => x.name.Equals(item.name)) as CapsuleInfo;			
			if (capsule == null) return null;
			if (item.skin!=capsule.skinID) return null;
			var rnd = UnityEngine.Random.RandomRange(0, 100);
			DropInfo randomItem = null;
			foreach (var capsItem in capsule.drop)
			{
				if (rnd >= capsItem.minChance && rnd <= capsItem.maxChance) randomItem = capsItem;
			}
			if (randomItem == null) return null;
			if (randomItem.isCommand)
			{
				var cmdFormatted = randomItem.command.Replace("{steamid}", player.UserIDString);
				Server.Command(cmdFormatted);
				item.Remove();
				if (!string.IsNullOrEmpty(randomItem.chatText))					
					player.Command("chat.add2", 0, 0, randomItem.chatText, "Система", "#af5", 1);
				return false;
			}
			else
			{
				var NewItem = ItemManager.CreateByName(randomItem.shortname, randomItem.amount, randomItem.skinID);
				if (NewItem==null) return null;
				item.UseItem(1);
				NewItem.name = randomItem.name;
				player.GiveItem(NewItem);
				return false;
			}
            return null;
        }
        void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);
        }
        
        void Loaded()
        {
            LoadConfig();

            foreach (var capsule in _config.items)
            {
				AddImage(capsule.png,Translit("CAPS_"+capsule.name));
				//Puts($">>> {capsule.name} = {}");
                foreach (var item in capsule.drop.Where(x => !string.IsNullOrEmpty(x.image)))
                {
                    AddImage(item.image,Translit("CAPS_ITEM_"+item.name));
                }
            }
			AddImage("https://i.ibb.co/1ZZHfnL/xp-status.png","CAPS_XP_STATUS");
			AddImage("https://i.ibb.co/g6bwMTn/exit.png","CAPS_EXIT_IMG");
			AddImage("https://i.ibb.co/xgMg8CL/bonus.png","CAPS_BONUS_IMG");
			AddImage("https://i.ibb.co/nQG6p33/lines.png","CAPS_LINE_IMG");
            foreach (var p in _config.permission) permission.RegisterPermission(p.Key, this);
        }

        void OnServerInitialized()
        { 
            timer.Every(305f, SaveData);
            
            LoadData();
            
            var minChance = 0;
            var maxChance = 100;
            var id = 0;
    
            foreach (var capsule in _config.items)
            {   
                var commonItems = capsule.drop.Where(x => x.rare.Equals("Common"));
                var unCommonItems = capsule.drop.Where(x => x.rare.Equals("Uncommon"));
                var rareItems = capsule.drop.Where(x => x.rare.Equals("Rare"));
                var mythicalItems = capsule.drop.Where(x => x.rare.Equals("Mythical"));
                
                id = 0;
                foreach (var item in commonItems)
                {
                    item.id = id;
                    id += 1;
                    item.minChance = minChance;
                    item.maxChance = minChance + (Convert.ToInt32(100 / capsule.drop.Count) + 3);
                    minChance = item.maxChance;
                    minChance += 1; 
                }
                
                foreach (var item in unCommonItems)
                {
                    item.id = id;
                    id += 1;
                    item.minChance = minChance;
                    item.maxChance = minChance + (Convert.ToInt32(100 / capsule.drop.Count) - 2);
                    minChance = item.maxChance;
                    minChance += 1;
                }
                 
                foreach (var item in rareItems) 
                {
                    item.id = id;
                    id += 1;
                    item.minChance = minChance;
                    item.maxChance = minChance + (Convert.ToInt32(100 / capsule.drop.Count) - 2);
                    minChance = item.maxChance;
                    minChance += 1;
                }

                foreach (var item in mythicalItems)
                {
                    item.id = id;
                    id += 1;
                    item.minChance = minChance;
                    item.maxChance = minChance + (Convert.ToInt32(100 / capsule.drop.Count) - 4);
                    minChance = item.maxChance;
                    minChance += 1;
                }
                
                SaveConfig();
                minChance = 0;
            }
        }

        #endregion
        
        #region Commands

        [ChatCommand("adddropcapsule")]
        void adddrop(BasePlayer player, string command, string[] args)
        {
			if (player==null) return;
            if (!player.IsAdmin) return;
       
            var activeItem = player.GetActiveItem();
            var capsuleInfo = _config.items.First(x => x.name.Equals(activeItem.name));
            capsuleInfo.drop.Clear();
            var id = 0; 
            
            foreach (var item in player.inventory.containerMain.itemList)
            {
                id++;
                capsuleInfo.drop.Add(new DropInfo()
                {
                    amount = item.amount,
                    command = "",
                    id = id,
                    image = "",
                    shortname = item.info.shortname,
                    skinID = item.skin,
                    isCommand = false,
                    name = item.info.displayName.english,
                });
            } 
            SaveConfig();
        }
        
        [ChatCommand("capsule")]
        void chatCmdCapsule(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, Layer); 
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    FadeIn = 0.2f,
                    Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                    Color = "0 0 0 1"
                }
            }, "Overlay", Layer);
            container.Add(new CuiPanel
            {
                Image =
                {
                    FadeIn = 0.2f,
                    Color = "0.2 0.2 0.16 0.7",
                    Material = "assets/content/ui/uibackgroundblur.mat"
                }
            }, Layer);

            var playerInfo = GetPlayerInfo(player.userID);

            container.Add(new CuiLabel
            {
                Text = { Text = "КАПСУЛЫ", Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -135", OffsetMax = "0 -71.6" }
            }, Layer);
            container.Add(new CuiLabel
            {
                Text = { Text = "Испытай свою удачу открыв одну из капсул.", Align = TextAnchor.UpperCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -135", OffsetMax = "0 -113" }
            }, Layer);

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "30 66", OffsetMax = "209 90" },
                Text =
                {
                    Align = TextAnchor.LowerCenter,
                    FontSize = 20,
                    Text = $"Ваш баланс: {playerInfo.balance}$",
                    Font = "RobotoCondensed-Bold.ttf"
                }
            }, Layer, Layer + ".Balance");

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "30 18.6", OffsetMax = $"{30 + 179.3 * (storedData.players[player.userID].XP / 150f)} 56.6" },
                Image = { Color = "0.33 0.87 0.59 0.6" }
            }, Layer, Layer + "BackgroundBarProgress");

            container.Add(new CuiElement
            {
                Name = Layer + "BackgroundBar",
                Parent = Layer,
                Components =
                {
					GetImageComponent("https://i.ibb.co/1ZZHfnL/xp-status.png","CAPS_XP_STATUS"),
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "30 18.6", OffsetMax = "209.3 56.6"},
                }
            });
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text =
                {
                    Text = $"<b>{($"{Math.Round(storedData.players[player.userID].XP, 3)} / 150 XP")}</b>",
                    Font = "RobotoCondensed-Bold.ttf",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                }
            }, Layer + "BackgroundBar");

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    GetImageComponent("https://i.ibb.co/g6bwMTn/exit.png","CAPS_EXIT_IMG"),
                    new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                    new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                }
            });
            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Close = Layer
                },
                Text = { Text = "Покинуть страницу", Align = TextAnchor.UpperCenter, FontSize = 18 },
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
            }, Layer);
            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "UI_Capsule close",
                    Close = Layer
                },
                Text = { Text = "" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
            }, Layer);
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    GetImageComponent("https://i.ibb.co/xgMg8CL/bonus.png","CAPS_BONUS_IMG"),
                    new CuiRectTransformComponent {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "60 26.6", OffsetMax = "134.6 94.6"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                    new CuiRectTransformComponent {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-66 22.6", OffsetMax = "66 25.2"}
                }
            });
            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "getdailymoney",
                },
                Text = { Text = "Получить бонус", Align = TextAnchor.UpperCenter, FontSize = 18 },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-66 22.6", OffsetMax = "66 49.2" },
            }, Layer);

            var capSizeX = 115.3f;
            var capSepX = 29.3f;
            var capSizeY = 166f;

            var num = _config.items.Count;
            var posX = -(capSizeX * num + capSepX * (num - 1)) / 2f;
            var posY = -(capSizeY / 2f);

            for (var i = 0; i < _config.items.Count; i++)
            {
                var item = _config.items[i];
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + capSizeX} {posY + capSizeY}"
                    },
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"UI_Capsule showfullcapsule {item.name}"
                    },
                    Text = { Text = "" }
                }, Layer, Layer + $".Image.{i}");

                container.Add(new CuiElement
                {
                    Name = Layer + $".Image.{i}.Icon",
                    Parent = Layer + $".Image.{i}",
                    Components =
                    {
                        GetImageComponent(item.png,Translit("CAPS_"+item.name)),
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-45 -28", OffsetMax = "-2 -4" },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Text = $"<b>{item.price}$</b>",
                        Font = "RobotoCondensed-Bold.ttf"
                    }
                }, Layer + $".Image.{i}");
                posX += capSizeX + capSepX;
            }

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("capsule_givebalance")]
        void consoleGivebalance(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon || arg.IsAdmin)
            {
                var userid = arg.GetULong(0);
                var amount = arg.GetInt(1);

                GetPlayerInfo(userid).balance += amount;
                PrintError($"Выдали валюту в капсулы игроку {userid} в размере {amount}"); 
            }
        }

        [ConsoleCommand("getdailymoney")]
        void consoleGetDailyMoney(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var playerInfo = GetPlayerInfo(player.userID);
            var dayofyear = DateTime.Now.DayOfYear;
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer + "GetDailyMoney");
            if (playerInfo.lastday == dayofyear)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "Вы уже получили бонус сегодня", Align = TextAnchor.LowerCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-150 104", OffsetMax = "150 130" }
                }, Layer, Layer + "GetDailyMoney");
                CuiHelper.AddUi(player, container);
                return;
            }
            playerInfo.lastday = dayofyear;
            playerInfo.balance += 5;

            container.Add(new CuiLabel
            {
                Text = { Text = "На ваш баланс было зачислено 5$", Align = TextAnchor.LowerCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-150 104", OffsetMax = "150 130" }
            }, Layer, Layer + "GetDailyMoney");

            CuiHelper.DestroyUi(player, Layer + ".Balance");
            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "30 68", OffsetMax = "209 90" },
                Text =
                {
                    Align = TextAnchor.LowerCenter,
                    FontSize = 18,
                    Text = $"Ваш баланс: {playerInfo.balance}$",
                    Font = "RobotoCondensed-Bold.ttf"
                }
            }, Layer, Layer + ".Balance");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("UI_Capsule")]
        void consoleCmdCapsule(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player == null) return;

            switch (arg.GetString(0))
            {
                case "prev":
                {
                    chatCmdCapsule(player, "", new string[0]);
                    break;
                }
                case "buycapsule":
                {
                    var capsule = _config.items.First(x => x.name.Equals(arg.GetString(1) + " капсула"));
                    var playerInfo = GetPlayerInfo(player.userID);
                    var container = new CuiElementContainer();
                    CuiHelper.DestroyUi(player, Layer + "Status");
                    if (playerInfo.balance < capsule.price)
                    {
                        container.Add(new CuiLabel
                        {
                            Text = { Text = "У вас недостаточно $ для покупки капсулы", Align = TextAnchor.LowerCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" },
                            RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-200 104", OffsetMax = "200 130" }
                        }, Layer, Layer + "Status");
                        CuiHelper.AddUi(player, container);
                        return;
                    }

                    var buyItem = ItemManager.CreateByName(capsule.shortname, 1, capsule.skinID);
                    buyItem.name = capsule.name;

                    playerInfo.balance -= capsule.price;
                    player.GiveItem(buyItem);
                        
                    CuiHelper.DestroyUi(player, Layer + "Status");
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Капсула успешно куплена!", Align = TextAnchor.LowerCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-200 104", OffsetMax = "200 130" }
                    }, Layer, Layer + "Status");
                    CuiHelper.AddUi(player, container);
                    break;
                }
                case "showfullcapsule":
                {
                    var capsule = _config.items.First(x => x.name.Equals(arg.GetString(1) + " капсула"));
                    CuiHelper.DestroyUi(player, Layer);

                    var container = new CuiElementContainer();

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            FadeIn = 0.2f,
                            Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                            Color = "0 0 0 1"
                        }
                    }, "Overlay", Layer);
                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            FadeIn = 0.2f,
                            Color = "0.2 0.2 0.16 0.7",
                            Material = "assets/content/ui/uibackgroundblur.mat"
                        }
                    }, Layer);
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{capsule.name.ToUpper()}", Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -135", OffsetMax = "0 -71.6" }
                    }, Layer);
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            GetImageComponent("https://i.ibb.co/g6bwMTn/exit.png","CAPS_EXIT_IMG"),
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                    {
                        new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                        new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                    }
                    });
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_Capsule prev"
                        },
                        Text = { Text = "Вернуться назад", Align = TextAnchor.UpperCenter, FontSize = 18 },
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
                    }, Layer);
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_Capsule prev",
                            Close = Layer
                        },
                        Text = { Text = "" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }, Layer);

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            GetImageComponent("https://i.ibb.co/nQG6p33/lines.png","CAPS_LINE_IMG"),
                            new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-127.6 -155.6", OffsetMax = "127.6 155.6"},
                        }
                    });

                    container.Add(new CuiElement()
                    {
                        Parent = Layer,
                        Name = Layer + ".Image",
                        Components =
                        {
                            GetImageComponent(capsule.png,Translit("CAPS_"+capsule.name)),
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-49.3 -56", OffsetMax = "66 110" },
                        }
                    });
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-45 -28", OffsetMax = "-2 -4" },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"<b>{capsule.price}$</b>",
                            Font = "RobotoCondensed-Bold.ttf"
                        }
                    }, Layer + ".Image");
                    
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-51.6 -106", OffsetMax = "51.6 -70" },
                        Button =
                        {
                            Color = "0.33 0.87 0.59 0.6",
                            Command = $"UI_Capsule buycapsule {capsule.name}",
                        },
                        Text = { Text = "КУПИТЬ", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                    }, Layer);

                    var itemSize = 89.3f;
                    var itemSep = 11.3f;
                    var itemSepBig = 252f;
                    var posStartX = -((itemSize * 4 + itemSep * 2 + itemSepBig) / 2f);
                    var posX = posStartX;
                    var posY = (itemSize * 4 + itemSep * 3) / 2f;
                    var itemCount = capsule.drop.Count;

                    for (var i = 0; i < 16; i++)
                    {
                        container.Add(new CuiPanel
                        {
                            RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posX} {posY - itemSize}", OffsetMax = $"{posX + itemSize} {posY}"},
                            Image = { Color = "0 0 0 0.6" }
                        }, Layer, Layer + $".Item{i}");
                        if (i < itemCount)
                        {
                            var dropItem = capsule.drop[i];
                            container.Add(new CuiElement()
                            {
                                Name = Layer + $".Item{i}.IMG",
                                Parent = Layer + $".Item{i}",
                                Components =
                                {
                                    string.IsNullOrEmpty(dropItem.image) ? GetItemImageComponent(dropItem.shortname) : GetImageComponent(dropItem.image,Translit("CAPS_ITEM_"+dropItem.name)),
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2"},
                                }
                            });
                            if (dropItem.name.Contains("Набор") || dropItem.name.Contains("Привилегия"))
                            {
                                container.Add(new CuiLabel()
                                {
                                    RectTransform = { AnchorMin = "0 0.029199", AnchorMax = "0.946665 1" },
                                    Text =
                                    {
                                        Align = TextAnchor.LowerRight,
                                        FontSize = 12,
                                        Text = $"{(dropItem.name.Contains("Набор") ? "<b>НАБОР</b>" : "<b>ПРИВИЛЕГИЯ</b>")}",
                                        Font = "RobotoCondensed-Bold.ttf"
                                    }
                                }, Layer + $".Item{i}.IMG");

                                container.Add(new CuiLabel()
                                {
                                    RectTransform = { AnchorMin = "0 0.029199", AnchorMax = "0.946665 1", OffsetMax = "0 -2" },
                                    Text =
                                    {
                                        Align = TextAnchor.UpperRight,
                                        FontSize = 10,
                                        Text = dropItem.name.Contains("Набор") ? $"<b>x{dropItem.amount}</b>" : $"<b>{dropItem.amount} DAY</b>",
                                        Font = "RobotoCondensed-Bold.ttf"
                                    }
                                }, Layer + $".Item{i}.IMG");
                            }
                            else
                            {
                                container.Add(new CuiLabel()
                                {
                                    RectTransform = { AnchorMin = "0 0.029199", AnchorMax = "0.946665 1", OffsetMax = "0 -2" },
                                    Text =
                                    {
                                        Align = TextAnchor.UpperRight,
                                        FontSize = 10,
                                        Text = $"<b>x{dropItem.amount}</b>",
                                        Font = "RobotoCondensed-Bold.ttf"
                                    }
                                }, Layer + $".Item{i}.IMG");
                            }
                        }
                        var num = i + 1;
                        if (num % 4 == 0)
                        {
                            posX = posStartX;
                            posY -= itemSize + itemSep;
                        }
                        else if (num % 2 == 0) posX += itemSize + itemSepBig;
                        else posX += itemSize + itemSep;
                    }

                    CuiHelper.AddUi(player, container);

                    break;
                }
            }
        }

        #endregion

        #region Data

        class StoredData
        {
           public Dictionary<ulong, PlayerInfo> players = new Dictionary<ulong, PlayerInfo>();    
        }
        
        class PlayerInfo
        {
            [JsonProperty("Баланс")] public int balance = 0;
            [JsonProperty("Опыта")] public double XP = 0d;
            [JsonProperty("d")] public int lastday = 0;
        }

        void SaveData()
        {
            CapsuleData.WriteObject(storedData);
        }

        void LoadData()
        {
            CapsuleData = Interface.Oxide.DataFileSystem.GetFile("CapsuleSystem/capsuleData");
            try
            {
                storedData = CapsuleData.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        StoredData storedData;
        private DynamicConfigFile CapsuleData;
        
        #endregion

        #region Config

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
			if (_config==null )	LoadDefaultConfig();
        } 

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration()
            {
                startBalance = 0,
                gainingExperience = new Dictionary<string, double>()
                {
                    ["Gather"] = 0.1d,
                    ["KillNPC"] = 1d,
                    ["KillAnimal"] = 0.5d,
                    ["DestroyBradley"] = 20d,
                    ["DestroyHeli"] = 30d,
                    ["DestroyBarrel"] = 0.1d,
                    ["OpenCrate"] = 0.1d,
                    ["OpenMilitaryCrate"] = 1d,
                    ["OpenEliteCrate"] = 5d,
                    ["GatherDetail"] = 1d,
                    ["Crop"] = 0.1d,
                },
                items = new List<CapsuleInfo>()
                {
                    new CapsuleInfo()
                    { 
                        name = "Lite капсула",
                        amount = 1,
                        price = 10,
						shortname = "xmas.present.large",
						skinID = 1592814551,
						png = "https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/capsulesystem/capsule_1.png",
                        drop = new List<DropInfo>()
                        {
                            new DropInfo()
                            {
                                name = "Sulfur",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "sulfur.ore", 
                                skinID = 0,
                                amount = 5000,
								minChance = 0,
								maxChance = 6
                            },
                            new DropInfo()
                            {
                                name = "Satchel Charge",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "explosive.satchel", 
                                skinID = 0,
                                amount = 1,
								minChance = 6,
								maxChance = 10
                            },
                            new DropInfo()
                            {
                                name = "Огненая кирка",
                                isCommand = true,
                                rare = "",
                                command = "customitem.give {steamid} pickaxe.fire",
                                shortname = "", 
                                skinID = 0,
                                amount = 1,
								minChance = 11,
								maxChance = 13,
								image = "https://i.ibb.co/TPHXcpg/753379c6288723283809ec46b330fa9d.png",
								chatText = "Вы получили <color=green>Огненную кирку</color>"
                            },
                            new DropInfo()
                            {
                                name = "Python Revolver",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "pistol.python", 
                                skinID = 0,
                                amount = 1,
								minChance = 14,
								maxChance = 19
                            },
                            new DropInfo()
                            {
                                name = "Garage Door",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "wall.frame.garagedoor", 
                                skinID = 0,
                                amount = 1,
								minChance = 20,
								maxChance = 27
                            },
                            new DropInfo()
                            {
                                name = "Double Barrel Shotgun",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "shotgun.double", 
                                skinID = 0,
                                amount = 1,
								minChance = 28,
								maxChance = 33
                            },
                            new DropInfo()
                            {
                                name = "Revolver",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "pistol.revolver", 
                                skinID = 0,
                                amount = 1,
								minChance = 34,
								maxChance = 43
                            },
                            new DropInfo()
                            {
                                name = "Salvaged Icepick",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "icepick.salvaged", 
                                skinID = 0,
                                amount = 1,
								minChance = 44,
								maxChance = 52
                            },
                            new DropInfo()
                            {
                                name = "hazmat suit",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "hazmatsuit", 
                                skinID = 0,
                                amount = 1,
								minChance = 53,
								maxChance = 60
                            },
                            new DropInfo()
                            {
                                name = "blueberries",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "blueberries", 
                                skinID = 0,
                                amount = 25,
								minChance = 61,
								maxChance = 67
                            },
                            new DropInfo()
                            {
                                name = "tactical gloves",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "tactical.gloves", 
                                skinID = 0,
                                amount = 1,
								minChance = 68,
								maxChance = 75
                            },
                            new DropInfo()
                            {
                                name = "Scrap",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "scrap", 
                                skinID = 0,
                                amount = 500,
								minChance = 76,
								maxChance = 82
                            },
                            new DropInfo()
                            {
                                name = "Дождь на сервере",
                                isCommand = true,
                                rare = "",
                                command = "rain 0.8",
                                shortname = "", 
                                skinID = 0,
                                amount = 1,
								minChance = 83,
								maxChance = 85,
								image = "https://i.ibb.co/5TdNWBK/cloud-rain.png"
                            },
                            new DropInfo()
                            {
                                name = "Golden Eggs",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "easter.goldegg", 
                                skinID = 0,
                                amount = 1,
								minChance = 86,
								maxChance = 90
                            },
                            new DropInfo()
                            {
                                name = "Semi-Automatic Rifle",
                                isCommand = false,
                                rare = "",
                                command = "",
                                shortname = "rifle.semiauto", 
                                skinID = 0,
                                amount = 1,
								minChance = 91,
								maxChance = 95
                            },
                            new DropInfo()
                            {
                                name = "SAY В ЧАТ",
                                isCommand = true,
                                rare = "",
                                command = "chat.say ТЕСТ",
                                shortname = "TEST COMMAND",
                                skinID = 97,
                                amount = 100,
                                image = "https://www.iconfinder.com/data/icons/kameleon-free-pack-rounded/110/Chat-2-512.png"
                            },
                        },
                    },
                    
                }
               
            };
        }

        public Configuration _config;

        public class Configuration
        {
            [JsonProperty("Стартовый баланс")] public int startBalance = 0;

            [JsonProperty("Получение XP")] public Dictionary<string, double> gainingExperience = new Dictionary<string, double>();
            [JsonProperty("Капсулы на продажу")] public List<CapsuleInfo> items = new List<CapsuleInfo>();
            [JsonProperty("Рейт для капсул")] public Dictionary<string, float> permission = new Dictionary<string, float>
            {
                { "capsulesystem.xp2", 2f },
                { "capsulesystem.xp3", 3f }
            };
        }

        public class CapsuleInfo
        {
            [JsonProperty("Название предмета")] public string name = "";
            [JsonProperty("Цена за 1шт")] public int price = 0;
            [JsonProperty("Шортнейм")] public string shortname = "";
            [JsonProperty("Количество")] public int amount = 1;
            [JsonProperty("СкинИД")] public ulong skinID = 0U;
            [JsonProperty("Дроп с капсулы")] public List<DropInfo> drop = new List<DropInfo>();
            [JsonProperty("Изображение")] public string png = "";
        }

        public class DropInfo
        {
            [JsonProperty("Это команда?")] public bool isCommand = false;
            [JsonProperty("Айди")] public int id = 0;
            [JsonProperty("Название")] public string name = "";
            [JsonProperty("Редкость")] public string rare = "Common";
            [JsonProperty("Шортнейм")] public string shortname;
            [JsonProperty("Команда")] public string command = "";
            [JsonProperty("Количество")] public int amount = 0;
            [JsonProperty("СкинИД")] public ulong skinID = 0;
            [JsonProperty("Ссылка на изображение")] public string image = "";
            [JsonProperty("Минимальный шанс")] public int minChance = 0;
            [JsonProperty("Максимальный шанс")] public int maxChance = 0;
            [JsonProperty("Сообщение в чат")] public string chatText;
        }
        #endregion
		
        #region Helpers

        private PlayerInfo GetPlayerInfo(ulong userid)
        {
            PlayerInfo result;
            if (!storedData.players.TryGetValue(userid, out result))
            {
                result = storedData.players[userid] = new PlayerInfo();
            }            
            return result;
        }

        private static string HexToRGB(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6)   str += "FF";
            if (str.Length != 8) throw new Exception(hex);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

		public CuiRawImageComponent GetAvatarImageComponent(ulong user_id, string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildAvatarImageComponent",user_id) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", user_id.ToString()), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga" };
			}
			return new CuiRawImageComponent {Url = "https://image.flaticon.com/icons/png/512/37/37943.png", Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetImageComponent(string url, string shortName="", string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildImageComponent",url) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				if (!string.IsNullOrEmpty(shortName)) url = shortName;
				//Puts($"{url}: "+ (string)plugins.Find("ImageLibrary").Call("GetImage", url));
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", url), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
			}
			return new CuiRawImageComponent {Url = url, Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetItemImageComponent(string shortName){
			string itemUrl = shortName;
			if (plugins.Find("ImageLoader")) {itemUrl = $"https://static.moscow.ovh/images/games/rust/icons/{shortName}.png";}
            return GetImageComponent(itemUrl, shortName);
		}
		public bool AddImage(string url,string shortName=""){
			if (plugins.Find("ImageLoader")){				
				plugins.Find("ImageLoader").Call("CheckCachedOrCache", url);
				return true;
			}else
			if (plugins.Find("ImageLibrary")){
				if (string.IsNullOrEmpty(shortName)) shortName=url;
				plugins.Find("ImageLibrary").Call("AddImage", url, shortName);
				//Puts($"Add Image {shortName}");
				return true;
			}	
			return false;		
		}
		
		public static string Translit(string str)
       {

           str = str.Replace("б", "b");
           str = str.Replace("Б", "B");

           str = str.Replace("в", "v");
           str = str.Replace("В", "V");

           str = str.Replace("г", "h");
           str = str.Replace("Г", "H");

           str = str.Replace("ґ", "g");
           str = str.Replace("Ґ", "G");

           str = str.Replace("д", "d");
           str = str.Replace("Д", "D");

           str = str.Replace("є", "ye");
           str = str.Replace("Э", "Ye");

           str = str.Replace("ж", "zh");
           str = str.Replace("Ж", "Zh");

           str = str.Replace("з", "z");
           str = str.Replace("З", "Z");

           str = str.Replace("и", "y");
           str = str.Replace("И", "Y");

           str = str.Replace("ї", "yi");
           str = str.Replace("Ї", "YI");

           str = str.Replace("й", "j");
           str = str.Replace("Й", "J");

           str = str.Replace("к", "k");
           str = str.Replace("К", "K");

           str = str.Replace("л", "l");
           str = str.Replace("Л", "L");

           str = str.Replace("м", "m");
           str = str.Replace("М", "M");

           str = str.Replace("н", "n");
           str = str.Replace("Н", "N");

           str = str.Replace("п", "p");
           str = str.Replace("П", "P");

           str = str.Replace("р", "r");
           str = str.Replace("Р", "R");

           str = str.Replace("с", "s");
           str = str.Replace("С", "S");

           str = str.Replace("ч", "ch");
           str = str.Replace("Ч", "CH");

           str = str.Replace("ш", "sh");
           str = str.Replace("Щ", "SHH");

           str = str.Replace("ю", "yu");
           str = str.Replace("Ю", "YU");

           str = str.Replace("Я", "YA");
           str = str.Replace("я", "ya");

           str = str.Replace('ь', '"');
           str = str.Replace("Ь", "");

           str = str.Replace('т', 't');
           str = str.Replace("Т", "T");

           str = str.Replace('ц', 'c');
           str = str.Replace("Ц", "C");

           str = str.Replace('о', 'o');
           str = str.Replace("О", "O");

           str = str.Replace('е', 'e');
           str = str.Replace("Е", "E");

           str = str.Replace('а', 'a');
           str = str.Replace("А", "A");

           str = str.Replace('ф', 'f');
           str = str.Replace("Ф", "F");

           str = str.Replace('і', 'i');
           str = str.Replace("І", "I");

           str = str.Replace('У', 'U');
           str = str.Replace("у", "u");

           str = str.Replace('х', 'x');
           str = str.Replace("Х", "X");
           str = str.Replace(" ", "_");
           str = str.Replace("~", "_");
           str = str.Replace(":", "_");
           str = str.Replace("'", "_");
           str = str.Replace(">", "_");
           str = str.Replace("<", "_");
           return str;
       }
        #endregion
    }
}