using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Building Upgrade", "OxideBro", "1.1.22")]
    class BuildingUpgrade : RustPlugin
    {
        [PluginReference] private Plugin NoEscape;
        [PluginReference] Plugin Remove;
        private void PayForUpgrade(ConstructionGrade g, BasePlayer player)
        {
            List<Item> items = new List<Item>();
            foreach (ItemAmount itemAmount in g.costToBuild)
            {
                player.inventory.Take(items, itemAmount.itemid, (int)itemAmount.amount);
                player.Command(string.Concat(new object[] {
                    "note.inv ", itemAmount.itemid, " ", itemAmount.amount * -1f
                }
                ), new object[0]);
            }
            foreach (Item item in items)
            {
                item.Remove(0f);
            }
        }
        private ConstructionGrade GetGrade(BuildingBlock block, BuildingGrade.Enum iGrade)
        {
            if ((int)block.grade < (int)block.blockDefinition.grades.Length) return block.blockDefinition.grades[(int)iGrade];
            return block.blockDefinition.defaultGrade;
        }
        private bool CanAffordUpgrade(BuildingBlock block, BuildingGrade.Enum iGrade, BasePlayer player)
        {
            bool flag;
            object[] objArray = new object[] {
                player, block, iGrade
            }
            ;
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
        }
        Dictionary<BuildingGrade.Enum, string> gradesString = new Dictionary<BuildingGrade.Enum, string>() {
                {
                BuildingGrade.Enum.Wood, "<color=#EC402C>Дерева</color>"
            }
            , {
                BuildingGrade.Enum.Stone, "<color=#EC402C>Камня</color>"
            }
            , {
                BuildingGrade.Enum.Metal, "<color=#EC402C>Метала</color>"
            }
            , {
                BuildingGrade.Enum.TopTier, "<color=#EC402C>Армора</color>"
            }
        }
        ;
        Dictionary<BasePlayer, BuildingGrade.Enum> grades = new Dictionary<BasePlayer, BuildingGrade.Enum>();
        Dictionary<BasePlayer, int> timers = new Dictionary<BasePlayer, int>();
        public Timer mytimer;
        private int resetTime = 40;
        private string permissionAutoGrade = "buildingupgrade.build";
        private string permissionAutoGradeFree = "buildingupgrade.free";
        private string permissionAutoGradeHammer = "buildingupgrade.hammer";
        private bool permissionAutoGradeAdmin = true;
        private bool getBuild = true;
        private bool permissionOn = true;
        private bool useNoEscape = true;
        private bool InfoNotice = true;
        private int InfoNoticeSize = 18;
        private string InfoNoticeText = "Используйте <color=#EC402C>/upgrade</color> (Или нажмите <color=#EC402C>USE - Клавиша E</color>) для быстрого улучшения при постройке.";
        private int InfoNoticeTextTime = 5;
        private bool CanUpgradeDamaged = false;
        private string PanelAnchorMin = "0.0 0.908";
        private string PanelAnchorMax = "1 0.958";
        private string PanelColor = "0 0 0 0.50";
        private int TextFontSize = 16;
        private string TextСolor = "0 0 0 1";
        private string TextAnchorMin = "0.0 0.870";
        private string TextAnchorMax = "1 1";
        private string MessageAutoGradePremHammer = "У вас нету доступа к улучшению киянкой!";
        private string MessageAutoGradePrem = "У вас нету доступа к данной команде!";
        private string MessageAutoGradeNo = "<color=ffcc00><size=16>Для улучшения нехватает ресурсов!!!</size></color>";
        private string MessageAutoGradeOn = "<size=14><color=#EC402C>Upgrade включен!</color> \nДля быстрого переключения используйте: <color=#EC402C>/upgrade 0-4</color></size>";
        private string MessageAutoGradeOff = "<color=ffcc00><size=14>Вы отключили <color=#EC402C>Upgrade!</color></size></color>";
        private string ChatCMD = "upgrade";
        private string ConsoleCMD = "building.upgrade";
        private bool EnabledRemove = false;
        private void LoadDefaultConfig()
        {
            GetConfig("Основные настройки", "Через сколько секунд автоматически выключать улучшение строений", ref resetTime);
            GetConfig("Основные настройки", "Привилегия что бы позволить улучшать объекты при строительстве", ref permissionAutoGrade);
            GetConfig("Основные настройки", "Включить доступ только по привилегиям?", ref permissionOn);
            GetConfig("Основные настройки", "Включить поддержку NoEscape (Запретить Upgrade в Raid Block)?", ref useNoEscape);
            GetConfig("Основные настройки", "Включить бесплатный Upgrade для администраторов?", ref permissionAutoGradeAdmin);
            GetConfig("Основные настройки", "Привилегия для улучшения при строительстве и ударе киянкой без траты ресурсов", ref permissionAutoGradeFree);
            GetConfig("Основные настройки", "Привилегия что бы позволить улучшать объекты ударом киянки", ref permissionAutoGradeHammer);
            GetConfig("Основные настройки", "Запретить Upgrade в Building Block?", ref getBuild);
            GetConfig("Основные настройки", "Включить выключение удаления построек при включении авто-улучшения (Поддержка плагина Remove с сайта RustPlugin.ru)", ref EnabledRemove);
            GetConfig("Основные настройки", "Разрешить улучшать повреждённые постройки?", ref CanUpgradeDamaged);
            GetConfig("Команды", "Чатовая команда включения авто-улучшения при постройки", ref ChatCMD);
            GetConfig("Команды", "Консольная команда включения авто-улучшения при постройки", ref ConsoleCMD);
            GetConfig("Настройки GUI Panel", "Минимальный отступ:", ref PanelAnchorMin);
            GetConfig("Настройки GUI Оповещения", "Включить GUI оповещение при использование плана постройки", ref InfoNotice);
            GetConfig("Настройки GUI Оповещения", "Размер текста GUI оповещения", ref InfoNoticeSize);
            GetConfig("Настройки GUI Оповещения", "Сообщение GUI", ref InfoNoticeText);
            GetConfig("Настройки GUI Оповещения", "Время показа оповещения", ref InfoNoticeTextTime);
            GetConfig("Настройки GUI Panel", "Максимальный отступ:", ref PanelAnchorMax);
            GetConfig("Настройки GUI Panel", "Цвет фона:", ref PanelColor);
            GetConfig("Настройки GUI Text", "Размер текста в gui панели:", ref TextFontSize);
            GetConfig("Настройки GUI Text", "Цвет текста в gui панели:", ref TextСolor);
            GetConfig("Настройки GUI Text", "Минимальный отступ в gui панели:", ref TextAnchorMin);
            GetConfig("Настройки GUI Text", "Максимальный отступ в gui панели:", ref TextAnchorMax);
            GetConfig("Сообщения", "No Permissions Hammer:", ref MessageAutoGradePremHammer);
            GetConfig("Сообщения", "No Permissions:", ref MessageAutoGradePrem);
            GetConfig("Сообщения", "No Resources:", ref MessageAutoGradeNo);
            GetConfig("Сообщения", "Сообщение при включение Upgrade:", ref MessageAutoGradeOn);
            GetConfig("Сообщения", "Сообщение при выключение Upgrade:", ref MessageAutoGradeOff);
            SaveConfig();
        }
        private void GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
            }
            Config[MainMenu, Key] = var;
        }
        void cmdAutoGrade(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGrade))
            {
                SendReply(player, MessageAutoGradePrem);
                return;
            }
            int grade;
            timers[player] = resetTime;
            if (EnabledRemove)
            {
                var removeEnabled = (bool)Remove.Call("OnRemoveActivate", player.userID);
                if (removeEnabled)
                {
                    Remove.Call("RemoveDeativate", player.userID);
                }
            }
            if (args == null || args.Length <= 0 || args[0] != "1" && args[0] != "2" && args[0] != "3" && args[0] != "4" && args[0] != "0")
            {
                if (!grades.ContainsKey(player))
                {
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    SendReply(player, MessageAutoGradeOn);
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
                    SendReply(player, MessageAutoGradeOff);
                    return;
                }
                timers[player] = resetTime;
                DrawUI(player, (BuildingGrade.Enum)grade, resetTime);
                return;
            }
            switch (args[0])
            {
                case "1":
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.Wood, resetTime);
                    return;
                case "2":
                    grade = (int)(grades[player] = BuildingGrade.Enum.Stone);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.Stone, resetTime);
                    return;
                case "3":
                    grade = (int)(grades[player] = BuildingGrade.Enum.Metal);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.Metal, resetTime);
                    return;
                case "4":
                    grade = (int)(grades[player] = BuildingGrade.Enum.TopTier);
                    timers[player] = resetTime;
                    DrawUI(player, BuildingGrade.Enum.TopTier, resetTime);
                    return;
                case "0":
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    SendReply(player, MessageAutoGradeOff);
                    return;
            }
        }
        void consoleAutoGrade(ConsoleSystem.Arg arg, string[] args)
        {
            var player = arg.Player();
            if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGrade))
            {
                SendReply(player, MessageAutoGradePrem);
                return;
            }
            int grade;
            if (EnabledRemove)
            {
                var removeEnabled = (bool)Remove.Call("OnRemoveActivate", player.userID);
                if (removeEnabled)
                {
                    Remove.Call("RemoveDeativate", player.userID);
                }
            }
            timers[player] = resetTime;
            if (player == null) return;
            if (args == null || args.Length <= 0)
            {
                if (!grades.ContainsKey(player))
                {
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    SendReply(player, MessageAutoGradeOn);
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
                    SendReply(player, MessageAutoGradeOff);
                    return;
                }
                timers[player] = resetTime;
                DrawUI(player, (BuildingGrade.Enum)grade, resetTime);
            }
        }
        private void Init()
        {
            permission.RegisterPermission(permissionAutoGrade, this);
            permission.RegisterPermission(permissionAutoGradeFree, this);
            permission.RegisterPermission(permissionAutoGradeHammer, this);
        }
        void OnServerInitialized()
        {
            LoadConfig();
            LoadDefaultConfig();
            timer.Every(1f, GradeTimerHandler);
            cmd.AddChatCommand(ChatCMD, this, cmdAutoGrade);
            cmd.AddConsoleCommand(ConsoleCMD, this, "consoleAutoGrade");
        }
        private void OnActiveItemChanged(BasePlayer player, Item newItem)
        {

            
            Item activeItem = player.GetActiveItem();
            if (activeItem == null || activeItem.info.shortname != "building.planner") return;
            if (activeItem.info.shortname == "building.planner")
            {
                if (!grades.ContainsKey(player))
                {
                    CuiHelper.DestroyUi(player, "InfoNotice");
                    ShowUIInfo(player);
                }
            }
        }
        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            Item activeItem = player.GetActiveItem();
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (activeItem == null || activeItem.info.shortname != "building.planner") return;
                if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGrade))
                {
                    SendReply(player, MessageAutoGradePrem);
                    return;
                }
                int grade;
                timers[player] = resetTime;
                if (!grades.ContainsKey(player))
                {
                    grade = (int)(grades[player] = BuildingGrade.Enum.Wood);
                    SendReply(player, MessageAutoGradeOn);
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
                    SendReply(player, MessageAutoGradeOff);
                    return;
                }
                timers[player] = resetTime;
                DrawUI(player, (BuildingGrade.Enum)grade, resetTime);
                return;
            }
        }
        void Unload()
        {
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                DestroyUI(plobj);
            }
        }
        void ShowUIInfo(BasePlayer player)
        {
            if (!InfoNotice) return;
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "InfoNotice",
                Parent = "Hud",
                FadeOut = 1f,
                Components = {
                    new CuiTextComponent {
                        FadeIn=1f, Text=$"{InfoNoticeText}", FontSize=InfoNoticeSize, Align=TextAnchor.MiddleCenter, Font="robotocondensed-regular.ttf"
                    }
                    , new CuiOutlineComponent {
                        Color="0.0 0.0 0.0 1.0"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.1 0.2", AnchorMax="0.9 0.25"
                    }
                }
            }
            );
            CuiHelper.AddUi(player, container);
            mytimer = timer.Once(InfoNoticeTextTime, () => {
                CuiHelper.DestroyUi(player, "InfoNotice");
            }
            );
        }
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            var buildingBlock = info.HitEntity as BuildingBlock;
            if (buildingBlock == null || player == null) return;
            if (permissionOn && !permission.UserHasPermission(player.UserIDString, permissionAutoGradeHammer))
            {
                SendReply(player, MessageAutoGradePremHammer);
                return;
            }
            Grade(buildingBlock, player);
        }
        /*void OnEntityBuilt(Planner planner, GameObject gameObject)         {             if (planner == null || gameObject == null) return;             var player = planner.GetOwnerPlayer();             BuildingBlock entity = gameObject.ToBaseEntity() as BuildingBlock;             if (entity == null || entity.IsDestroyed) return;             if (player == null) return;             Grade(entity, player);         }*/
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null || entity?.net?.ID == null) return;
            var ent = entity as BaseEntity;
            if (ent == null || ent.IsDestroyed) return;
            var player = BasePlayer.FindByID(ent.OwnerID);
            if (player != null)
            {
                BuildingBlock block = ent as BuildingBlock;
                if (block != null) Grade(block, player);
            }
        }
        void Grade(BuildingBlock block, BasePlayer player)
        {
            BuildingGrade.Enum grade;
            if (useNoEscape)
            {
                object can = NoEscape?.Call("IsRaidBlocked", player);
                if (can != null) if ((bool)can == true)
                    {
                        SendReply(player, "Вы не можете использовать Upgrade во время рейд-блока");
                        return;
                    }
            }
            if (!grades.TryGetValue(player, out grade) || grade == BuildingGrade.Enum.Count) return;
            if (block == null) return;
            if (!((int)grade >= 1 && (int)grade <= 4)) return;
            var targetLocation = player.transform.position + (player.eyes.BodyForward() * 4f);
            var reply = 1959;
            if (reply == 0) { }
            if (getBuild && player.IsBuildingBlocked(targetLocation, new Quaternion(0, 0, 0, 0), new Bounds(Vector3.zero, Vector3.zero)))
            {
                player.ChatMessage("<color=ffcc00><size=16><color=#EC402C>Upgrade</color> запрещен в билдинг блоке!!!</size></color>");
                return;
            }
            if (block.blockDefinition.checkVolumeOnUpgrade)
            {
                if (DeployVolume.Check(block.transform.position, block.transform.rotation, PrefabAttribute.server.FindAll<DeployVolume>(block.prefabID), ~(1 << block.gameObject.layer)))
                {
                    player.ChatMessage("Вы не можете улучшить постройку находясь в ней");
                    return;
                }
            }
            var ret = Interface.Call("CanUpgrade", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            if (permissionAutoGradeAdmin)
            {
                if (player.IsAdmin)
                {
                    if (block.grade > grade)
                    {
                        SendReply(player, "Нельзя понижать уровень строения!");
                        return;
                    }
                    if (block.grade == grade)
                    {
                        SendReply(player, "Уровень строения соответствует выбранному.");
                        return;
                    }
                    if (block.Health() != block.MaxHealth() && !CanUpgradeDamaged)
                    {
                        SendReply(player, "Нельзя улучшать повреждённые постройки!");
                        return;
                    }
                    block.SetGrade(grade);
                    block.SetHealthToMax();
                    block.UpdateSkin(false);
                    Effect.server.Run(string.Concat("assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab"), block, 0, Vector3.zero, Vector3.zero, null, false);
                    timers[player] = resetTime;
                    DrawUI(player, grade, resetTime);
                    return;
                }
            }
            if (permissionOn && permission.UserHasPermission(player.UserIDString, permissionAutoGradeFree))
            {
                if (block.grade > grade)
                {
                    SendReply(player, "Нельзя понижать уровень строения!");
                    return;
                }
                if (block.grade == grade)
                {
                    SendReply(player, "Уровень строения соответствует выбранному.");
                    return;
                }
                if (block.Health() != block.MaxHealth() && !CanUpgradeDamaged)
                {
                    SendReply(player, "Нельзя улучшать повреждённые постройки!");
                    return;
                }
                block.SetGrade(grade);
                block.SetHealthToMax();
                block.UpdateSkin(false);
                Effect.server.Run(string.Concat("assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab"), block, 0, Vector3.zero, Vector3.zero, null, false);
                timers[player] = resetTime;
                DrawUI(player, grade, resetTime);
                return;
            }
            if (CanAffordUpgrade(block, grade, player))
            {
                if (block.grade > grade)
                {
                    SendReply(player, "Нельзя понижать уровень строения!");
                    return;
                }
                if (block.grade == grade)
                {
                    SendReply(player, "Уровень строения соответствует выбранному.");
                    return;
                }
                if (block.Health() != block.MaxHealth() && !CanUpgradeDamaged)
                {
                    SendReply(player, "Нельзя улучшать повреждённые постройки!");
                    return;
                }
                PayForUpgrade(GetGrade(block, grade), player);
                block.SetGrade(grade);
                block.SetHealthToMax();
                block.UpdateSkin(false);
                Effect.server.Run(string.Concat("assets/bundled/prefabs/fx/build/promote_", grade.ToString().ToLower(), ".prefab"), block, 0, Vector3.zero, Vector3.zero, null, false);
                timers[player] = resetTime;
                DrawUI(player, grade, resetTime);
            }
            else
            {
                SendReply(player, MessageAutoGradeNo);
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
                    grades.Remove(player);
                    timers.Remove(player);
                    DestroyUI(player);
                    continue;
                }
                DrawUI(player, grades[player], seconds);
            }
        }
        void DrawUI(BasePlayer player, BuildingGrade.Enum grade, int seconds)
        {
            DestroyUI(player);
            CuiHelper.AddUi(player, GUI.Replace("{0}", gradesString[grade]).Replace("{1}", seconds.ToString()).Replace("{PanelColor}", PanelColor.ToString()).Replace("{PanelAnchorMin}", PanelAnchorMin.ToString()).Replace("{PanelAnchorMax}", PanelAnchorMax.ToString()).Replace("{TextFontSize}", TextFontSize.ToString()).Replace("{TextСolor}", TextСolor.ToString()).Replace("{TextAnchorMin}", TextAnchorMin.ToString()).Replace("{TextAnchorMax}", TextAnchorMax.ToString()));
        }
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "autograde.panel");
            CuiHelper.DestroyUi(player, "autogradetext");
        }
        private string GUI = @"[{""name"": ""autograde.panel"",""parent"": ""Hud"",""components"": [{""type"": ""UnityEngine.UI.Image"",""color"": ""{PanelColor}""},{""type"": ""RectTransform"",""anchormin"": ""{PanelAnchorMin}"",""anchormax"": ""{PanelAnchorMax}""}]}, {""name"": ""autogradetext"",""parent"": ""Hud"",""components"": [{""type"": ""UnityEngine.UI.Text"",""text"": ""Режим улучшения строения до {0} выключится через " + @"{1} секунд."",""fontSize"": ""{TextFontSize}"",""align"": ""MiddleCenter""}, {""type"": ""UnityEngine.UI.Outline"",""color"": ""{TextСolor}"",""distance"": ""0.1 -0.1""}, {""type"": ""RectTransform"",""anchormin"": ""{TextAnchorMin}"",""anchormax"": ""{TextAnchorMax}""}]}]";
        void UpdateTimer(BasePlayer player, ulong playerid = 2006016)
        {
            timers[player] = resetTime;
            DrawUI(player, grades[player], timers[player]);
        }
        object BuildingUpgradeActivate(ulong id)
        {
            var player = BasePlayer.FindByID(id);
            if (player != null) if (grades.ContainsKey(player)) return true;
            return false;
        }
        void BuildingUpgradeDeactivate(ulong id)
        {
            var player = BasePlayer.FindByID(id);
            if (player != null)
            {
                grades.Remove(player);
                timers.Remove(player);
                DestroyUI(player);
            }
        }
    }
}                                                        