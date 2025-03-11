using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Rust;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("Building Upgrade", "Ryamkk", "1.1.6")]

    class BuildingUpgrade : RustPlugin
    {
        [PluginReference]
        Plugin NoEscape;

        private void PayForUpgrade(ConstructionGrade g, BasePlayer player)
        {
            List<Item> items = new List<Item>();

            foreach (ItemAmount itemAmount in g.costToBuild)
            {
                player.inventory.Take(items, itemAmount.itemid, (int)itemAmount.amount);
                player.Command(string.Concat(new object[] { "note.inv ", itemAmount.itemid, " ", itemAmount.amount * -1f }), new object[0]);
            }
            foreach (Item item in items)
            {
                item.Remove(0f);
            }
        }
        

        private ConstructionGrade GetGrade(BuildingBlock block, BuildingGrade.Enum iGrade)
        {
            if ((int)block.grade < (int)block.blockDefinition.grades.Length)
                return block.blockDefinition.grades[(int)iGrade];


            return block.blockDefinition.defaultGrade;
        }
        
        private bool CanAffordUpgrade(BuildingBlock block, BuildingGrade.Enum iGrade, BasePlayer player)
        {
            bool flag;
            object[] objArray = new object[] { player, block, iGrade };
            object obj = Interface.CallHook("CanAffordUpgrade", objArray);

            if (obj is bool)
            {
                return (bool)obj;
            }

            List<ItemAmount>.Enumerator enumerator = GetGrade(block, iGrade).costToBuild.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    ItemAmount current = enumerator.Current;
                    if ((float)player.inventory.GetAmount(current.itemid) >= current.amount)
                    {
                        continue;
                    }
                    flag = false;
                    return flag;
                }
                return true;
            }
            finally
            {
                ((IDisposable)enumerator).Dispose();
            }
            return flag;
        }

        private void RegisterCommands()
        {
            foreach (var command in ChatCommands)
            {
                cmd.AddChatCommand(command, this, cmdAutoGrade);
            }

            foreach (var command in ConsoleCommands)
            {
                cmd.AddConsoleCommand(command, this, nameof(consoleAutoGrade));
            }
        }

        Dictionary<BuildingGrade.Enum, string> gradesString = new Dictionary<BuildingGrade.Enum, string>()
        {
            {BuildingGrade.Enum.Wood, "<color=#EC402C>Дерева</color>"},
            {BuildingGrade.Enum.Stone, "<color=#EC402C>Камня</color>"},
            {BuildingGrade.Enum.Metal, "<color=#EC402C>Метала</color>"},
            {BuildingGrade.Enum.TopTier, "<color=#EC402C>Армора</color>"}
        };

        Dictionary<BasePlayer, BuildingGrade.Enum> grades = new Dictionary<BasePlayer, BuildingGrade.Enum>();
        Dictionary<BasePlayer, int> timers = new Dictionary<BasePlayer, int>();

        public Timer mytimer;
		private bool ConfigChanged;
        private int resetTime = 40;
        private string permissionAutoGrade = "buildingupgrade.build";
        private string permissionAutoGradeFree = "buildingupgrade.free";
        private string permissionAutoGradeHammer = "buildingupgrade.hammer";
        private bool permissionAutoGradeAdmin = true;
        private bool getBuild = true;
        private bool permissionOn = false;
        private bool useNoEscape = true;

        private bool InfoNotice = true;
        private int NoticeSize = 18;
        private int NoticeTime = 5;
		private string NoticeFont = "robotocondensed-regular.ttf";

        private bool CanUpgradeDamaged = false;
        private string PanelAnchorMin = "0.0 0.908";
        private string PanelAnchorMax = "1 0.958";
        private string PanelColor = "0 0 0 0.50";

        private int TextFontSize = 16;
        private string TextСolor = "0 0 0 1";
        private string TextAnchorMin = "0.0 0.870";
        private string TextAnchorMax = "1 1";
		private string FontName = "robotocondensed-regular.ttf";
		
		List<string> ChatCommands;
        List<string> ConsoleCommands;
		
		protected override void LoadDefaultConfig() => PrintWarning("Создания стандартной конфигурации...");
	    private void UpgradeConfig()
        {
            resetTime = GetConfig(40, "Основные настройки", "Через сколько секунд автоматически выключать улучшение строений");
			permissionAutoGrade = GetConfig("buildingupgrade.build", "Основные настройки", "Привилегия что бы позволить улучшать объекты при строительстве");
			permissionOn = GetConfig(false, "Основные настройки", "Включить доступ только по привилегиям?");
			useNoEscape = GetConfig(true, "Основные настройки", "Включить поддержку NoEscape (Запретить Upgrade в Raid Block)?");
			permissionAutoGradeAdmin = GetConfig(true, "Основные настройки", "Включить бесплатный Upgrade для администраторов?");
			permissionAutoGradeFree = GetConfig("buildingupgrade.free", "Основные настройки", "Привилегия для улучшения при строительстве и ударе киянкой без траты ресурсов");
			permissionAutoGradeHammer = GetConfig("buildingupgrade.hammer", "Основные настройки", "Привилегия что бы позволить улучшать объекты ударом киянки");
			getBuild = GetConfig(true, "Основные настройки", "Запретить Upgrade в Building Block?");
			CanUpgradeDamaged = GetConfig(true, "Основные настройки", "Разрешить улучшать повреждённые постройки?");
			
			InfoNotice = GetConfig(true, "Настройки GUI Оповещения", "Включить GUI оповещение при использование плана постройки");
			NoticeSize = GetConfig(18, "Настройки GUI Оповещения", "Размер текста GUI оповещения");
			NoticeTime = GetConfig(5, "Настройки GUI Оповещения", "Время показа оповещения");
			NoticeFont = GetConfig("robotocondensed-regular.ttf", "Настройки GUI Оповещения", "Названия шрифта");
			
			PanelAnchorMin = GetConfig("0.0 0.908", "Настройки GUI Panel", "Минимальный отступ");
			PanelAnchorMax = GetConfig("1 0.958", "Настройки GUI Panel", "Максимальный отступ");
			PanelColor = GetConfig("0 0 0 0.50", "Настройки GUI Panel", "Цвет фона");
			 
			TextFontSize = GetConfig(16, "Настройки GUI Text", "Размер текста в gui панели");
			TextСolor = GetConfig("0 0 0 1", "Настройки GUI Text", "Цвет текста в gui панели");
			TextAnchorMin = GetConfig("0.0 0.870", "Настройки GUI Text", "Минимальный отступ в gui панели");
			TextAnchorMax = GetConfig("1 1", "Настройки GUI Text", "Максимальный отступ в gui панели");
			FontName = GetConfig("robotocondensed-regular.ttf", "Настройки GUI Text", "Названия шрифта");
			
			ChatCommands = GetConfig(new List<string>
            {
                "upgrade",
                "up",
				"grade",
				"autograde",
				"agrade",
				"bgrade"
            }, "Команды", "Чат команды включения авто-улучшения при постройки");
			
            ConsoleCommands = GetConfig(new List<string>
            {
                "bgrade.upgrade",
				"building.upgrade",
				"up.grade",
				"auto.upgrade",
				"bgrade.up"
            }, "Команды", "Консольные команды включения авто-улучшения при постройки");

            if (ConfigChanged)
            {
                PrintWarning("Конфигурационный файл изменён. Новые значения занесены в файл!");
                SaveConfig();
            }
        }
		
        void cmdAutoGrade(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGrade))
            {
				SendReply(player, Messages["UpgradePrem"]);
                return;
            }
            int grade;
            timers[player] = resetTime;

            if (args == null || args.Length <= 0 || args[0] != "1" && args[0] != "2" && args[0] != "3" && args[0] != "4" && args[0] != "0")
            {
                if (!grades.ContainsKey(player))
                {
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    SendReply(player, Messages["UpgradeON"]);

                }
                else
                {
                    grade = (int)grades[player];
                    grade++;
                    grades[player] = (BuildingGrade.Enum)Mathf.Clamp(grade, 1, 5);
                }
                if (grade > 4)
                {
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    SendReply(player, Messages["UpgradeOFF"]);
                    return;
                }
                timers[player] = resetTime;
                DrawUI(player, (BuildingGrade.Enum)grade, resetTime, "Upgrade");
                return;
            }
			
            switch (args[0])
            {
                case "1":
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.Wood, resetTime, "Upgrade");
                    return;
                case "2":
                    grade = (int)(grades[player] = BuildingGrade.Enum.Stone);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.Stone, resetTime, "Upgrade");
                    return;
                case "3":
                    grade = (int)(grades[player] = BuildingGrade.Enum.Metal);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.Metal, resetTime, "Upgrade");
                    return;
                case "4":
                    grade = (int)(grades[player] = BuildingGrade.Enum.TopTier);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.TopTier, resetTime, "Upgrade");
                    return;
                case "0":
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    SendReply(player, Messages["UpgradeOFF"]);
                    return;
            }

        }

        void consoleAutoGrade(ConsoleSystem.Arg arg, string[] args)
        {
            var player = arg.Player();
            if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGrade))
            {
                SendReply(player, Messages["UpgradePrem"]);
                return;
            }
            int grade;
            timers[player] = resetTime;

            if (player == null) return;
            if (args == null || args.Length <= 0)
            {
                if (!grades.ContainsKey(player))
                {
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    SendReply(player, Messages["UpgradeON"]);

                }
                else
                {
                    grade = (int)grades[player];
                    grade++;
                    grades[player] = (BuildingGrade.Enum)Mathf.Clamp(grade, 1, 5);
                }

                if (grade > 4)
                {
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    SendReply(player, Messages["UpgradeOFF"]);
                    return;
                }
                timers[player] = resetTime;
                DrawUI(player, (BuildingGrade.Enum)grade, resetTime, "Upgrade");
            }
        }

        private void Init()
        {
			UpgradeConfig();
			RegisterCommands();
            permission.RegisterPermission(permissionAutoGrade, this);
            permission.RegisterPermission(permissionAutoGradeFree, this);
            permission.RegisterPermission(permissionAutoGradeHammer, this);
        }
        void OnServerInitialized()
        {
            timer.Every(1f, GradeTimerHandler);
			lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
        }

        private void OnPlayerActiveItemChanged(BasePlayer player, Item newItem)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem == null || activeItem.info.shortname != "building.planner")
                return;
            if (activeItem.info.shortname == "building.planner")
            {
                if (!grades.ContainsKey(player))
                {
                    CuiHelper.DestroyUi(player, "InfoNotice");
                    ShowRepairInfo(player);
                }
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            Item activeItem = player.GetActiveItem();
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (activeItem == null || activeItem.info.shortname != "building.planner")
                    return;
                if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGrade))
                {
                    SendReply(player, Messages["UpgradePrem"]);
                    return;
                }
                int grade;
                timers[player] = resetTime;
                if (!grades.ContainsKey(player))
                {
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    SendReply(player, Messages["UpgradeON"]);
                }
                else
                {
                    grade = (int)grades[player];
                    grade++;
                    grades[player] = (BuildingGrade.Enum)Mathf.Clamp(grade, 1, 5);
                }

                if (grade > 4)
                {
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    SendReply(player, Messages["UpgradeOFF"]);
                    return;
                }
                timers[player] = resetTime;
                DrawUI(player, (BuildingGrade.Enum)grade, resetTime, "Upgrade");
                return;
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (grades.ContainsKey(player))
                {
                    DestroyUI(player);
                }
            }
        }

        void ShowRepairInfo(BasePlayer player)
        {
            if (!InfoNotice) return;
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "InfoNotice",
                Parent = "Hud",
                FadeOut = 1f,
                Components =
                {
                    new CuiTextComponent
                    {
                        FadeIn = 1f,
                        Text = Messages["UpgradeNotice"],
                        FontSize = NoticeSize,
                        Align = TextAnchor.MiddleCenter,
                        Font = NoticeFont
                    },
                    new CuiOutlineComponent
                    {
                        Color = "0.0 0.0 0.0 1.0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1 0.2",
                        AnchorMax = "0.9 0.25"
                    }
                }
            });

            CuiHelper.AddUi(player, container);

            mytimer = timer.Once(NoticeTime, () => { CuiHelper.DestroyUi(player, "InfoNotice"); });
        }

        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            var buildingBlock = info.HitEntity as BuildingBlock;
            if (buildingBlock == null || player == null) return;

            if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGradeHammer))
            {
				SendReply(player, Messages["UpgradePremHammer"]);
                return;
            }
            Grade(buildingBlock, player);
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null) return;
            var player = planner.GetOwnerPlayer();
            BuildingGrade.Enum grade;
            BuildingBlock entity = gameObject.ToBaseEntity() as BuildingBlock;
            if (entity == null || entity.IsDestroyed) return;
            if (player == null) return;
            var buildingBlock = gameObject.GetComponent<BuildingBlock>();
            var buildingGrade = (int)buildingBlock.grade;
            Grade(entity, player);
        }

        void Grade(BuildingBlock block, BasePlayer player)
        {
            BuildingGrade.Enum grade;
			
			if (useNoEscape)
            {
                object can = NoEscape?.Call("IsRaidBlocked", player);
                if (can != null)
                    if ((bool)can == true)
                    {
						SendReply(player, Messages["UpgradeRaid"]);
                        return;
                    }
            }
			
            if (!grades.TryGetValue(player, out grade) || grade == BuildingGrade.Enum.Count) return;
            if (block == null) return;
            if (!((int)grade >= 1 && (int)grade <= 4)) return;
			
            var targetLocation = player.transform.position + (player.eyes.BodyForward() * 4f);
            var reply = 309;
			if (reply == 0) { }
			
            if (getBuild && player.IsBuildingBlocked(targetLocation, new Quaternion(0, 0, 0, 0), new Bounds(Vector3.zero, Vector3.zero)))
            {
				SendReply(player, Messages["UpgradeBuildingBlocked"]);
                return;
            }
			
            if (block.blockDefinition.checkVolumeOnUpgrade)
            {
                if (DeployVolume.Check(block.transform.position, block.transform.rotation, PrefabAttribute.server.FindAll<DeployVolume>(block.prefabID), ~(1 << block.gameObject.layer)))
                {
					SendReply(player, Messages["UpgradeBlock"]);
                    return;
                }
            }
			
            if (permissionAutoGradeAdmin)
            {
                if (player.IsAdmin)
                {
                    var ret = Interface.Call("CanUpgrade", player) as string;
                    if (ret != null)
                    {
                        SendReply(player, ret);
                        return;
                    }
                    if (block.grade > grade)
                    {
                        SendReply(player, Messages["UpgradeDownLevel"]);
                        return;
                    }
                    if (block.grade == grade)
                    {
                        SendReply(player, Messages["UpgradeLevel"]);
                        return;
                    }
                    if (block.Health() != block.MaxHealth() && !CanUpgradeDamaged)
                    {
                        SendReply(player, Messages["UpgradeDamaged"]);
                        return;
                    }
                    block.SetGrade(grade);
                    block.SetHealthToMax();
                    block.UpdateSkin(false);
                    Effect.server.Run(string.Concat("assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab"), block, 0, Vector3.zero, Vector3.zero, null, false);
                    timers[player] = resetTime;
                    DrawUI(player, grade, resetTime, "Upgrade");
                    return;
                }
            }
			
            if (permissionOn)
            {
                if (permission.UserHasPermission(player.UserIDString, permissionAutoGradeFree))
                {
                    var ret = Interface.Call("CanUpgrade", player) as string;
                    if (ret != null)
                    {
                        SendReply(player, ret);
                        return;
                    }
                    if (block.grade > grade)
                    {
                        SendReply(player, Messages["UpgradeDownLevel"]);
                        return;
                    }
                    if (block.grade == grade)
                    {
                        SendReply(player, Messages["UpgradeLevel"]);
                        return;
                    }
                    if (block.Health() != block.MaxHealth() && !CanUpgradeDamaged)
                    {
                        SendReply(player, Messages["UpgradeDamaged"]);
                        return;
                    }
                    block.SetGrade(grade);
                    block.SetHealthToMax();
                    block.UpdateSkin(false);
                    Effect.server.Run(string.Concat("assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab"), block, 0, Vector3.zero, Vector3.zero, null, false);
                    timers[player] = resetTime;
                    DrawUI(player, grade, resetTime, "Upgrade");
                    return;
                }
            }
			
            if (CanAffordUpgrade(block, grade, player))
            {
                var ret = Interface.Call("CanUpgrade", player) as string;
                if (ret != null)
                {
                    SendReply(player, ret);
                    return;
                }
                if (block.grade > grade)
                {
					SendReply(player, Messages["UpgradeDownLevel"]);
                    return;
                }
                if (block.grade == grade)
                {
					SendReply(player, Messages["UpgradeLevel"]);
                    return;
                }
                if (block.Health() != block.MaxHealth() && !CanUpgradeDamaged)
                {
					SendReply(player, Messages["UpgradeDamaged"]);
                    return;
                }
                PayForUpgrade(GetGrade(block, grade), player);
                block.SetGrade(grade);
                block.SetHealthToMax();
                block.UpdateSkin(false);
                Effect.server.Run(string.Concat("assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab"), block, 0, Vector3.zero, Vector3.zero, null, false);
                timers[player] = resetTime;
                DrawUI(player, grade, resetTime, "Upgrade");
            }
            else
            {
				SendReply(player, Messages["UpgradeNoResources"]);
            }
        }

        int NextGrade(int grade) => ++grade;
        void GradeTimerHandler()
        {
            foreach (var player in timers.Keys.ToList())
            {
                var seconds = --timers[player];
                if (seconds <= 0)
                {
                    BuildingGrade.Enum mode;
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    continue;
                }
                DrawUI(player, grades[player], seconds, "Upgrade");
            }
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

        void DrawUI(BasePlayer player, BuildingGrade.Enum grade, int seconds, string type)
        {
			DestroyUI(player);
            var msg = "";
            if (type == "Upgrade")
            {
                msg = Messages["UpgradeMSG"];
            }
			
            CuiHelper.AddUi(player,
                GUI.Replace("{PanelColor}", PanelColor.ToString())
                   .Replace("{PanelAnchorMin}", PanelAnchorMin.ToString())
                   .Replace("{PanelAnchorMax}", PanelAnchorMax.ToString())
				   .Replace("{FontName}", FontName.ToString())
                   .Replace("{TextFontSize}", TextFontSize.ToString())
                   .Replace("{TextСolor}", TextСolor.ToString())
                   .Replace("{TextAnchorMin}", TextAnchorMin.ToString())
                   .Replace("{TextAnchorMax}", TextAnchorMax.ToString())
				   .Replace("{msg}", msg)
				   .Replace("{0}", gradesString[grade])
				   .Replace("{1}", FormatTime(TimeSpan.FromSeconds(seconds))));
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "autograde.panel");
            CuiHelper.DestroyUi(player, "autogradetext");
        }

        private string GUI = @"[{""name"": ""autograde.panel"",""parent"": ""Hud"",""components"": 
		                       [{""type"": ""UnityEngine.UI.Image"",""color"": ""{PanelColor}""}, 
							    {""type"": ""RectTransform"",""anchormin"": ""{PanelAnchorMin}"",""anchormax"": ""{PanelAnchorMax}""}]}, 
								{""name"": ""autogradetext"",""parent"": ""Hud"",""components"": 
							   [{""type"": ""UnityEngine.UI.Text"",""text"": ""{msg}"",""fontSize"": ""{TextFontSize}"",""font"":""{FontName}"",""align"": ""MiddleCenter""}, 
							    {""type"": ""UnityEngine.UI.Outline"",""color"": ""{TextСolor}"",""distance"": ""0.1 -0.1""}, 
								{""type"": ""RectTransform"",""anchormin"": ""{TextAnchorMin}"",""anchormax"": ""{TextAnchorMax}""}]}]";
								
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"UpgradeDamaged", "Нельзя улучшать повреждённые постройки!"},
            {"UpgradeLevel", "Уровень строения соответствует выбранному!"},
			{"UpgradeDownLevel", "Нельзя понижать уровень строения!"},
            {"UpgradeBlock", "Вы не можете улучшить постройку находясь в ней!"},
			{"UpgradeBuildingBlocked", "<color=ffcc00><size=16><color=#EC402C>Upgrade</color> запрещен в билдинг блоке!!!</size></color>"},
            {"UpgradeRaid", "Вы не можете использовать Upgrade во время рейд-блока! Ремув во время рейда запрещён!\nОсталось<color=#ffd479> {0}</color>."},
			{"UpgradePremHammer", "У вас нету доступа к улучшению киянкой!"},
			{"UpgradePrem", "У вас нет доступа к данной команде!"},
			{"UpgradeNoResources", "<color=ffcc00><size=16>Для улучшения нехватает ресурсов!!!</size></color>"},
			{"UpgradeON", "<size=14><color=#EC402C>Upgrade включен!</color> \nДля быстрого переключения используйте: <color=#EC402C>/upgrade 0-4</color></size>"},
			{"UpgradeOFF", "<color=ffcc00><size=14>Вы отключили <color=#EC402C>Upgrade!</color></size></color>"},
			{"UpgradeNotice", "Используйте <color=#EC402C>/upgrade</color> (Или нажмите <color=#EC402C>USE - Клавиша E</color>) для быстрого улучшения при постройке."},
		    {"UpgradeMSG", "Режим улучшения строения до {0} выключится через {1}"},
        };
		
        void UpdateTimer(BasePlayer player, string type)
        {
            timers[player] = resetTime;
            DrawUI(player, grades[player], timers[player], type);
        }
		
        private T GetConfig<T>(T defaultVal, params string[] path)
        {
            var data = Config.Get(path);
            if (data != null)
            {
                return Config.ConvertValue<T>(data);
            }

            Config.Set(path.Concat(new object[] { defaultVal }).ToArray());
            ConfigChanged = true;
            return defaultVal;
        }
    }
}