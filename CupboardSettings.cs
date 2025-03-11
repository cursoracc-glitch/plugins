using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Cupboard Settings", "Sempai#3239", "1.0.21")]
    class CupboardSettings : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        public Dictionary<ulong, PlayerSetting> PlayerListed = new Dictionary<ulong, PlayerSetting>();
        public class PlayerSetting
        {
            public Dictionary<uint, CupboardSetting> CupboardsList = new Dictionary<uint, CupboardSetting>();
        }
        void Unload()
        {
            SaveData();
            BasePlayer.activePlayerList.ToList().ForEach(player =>
            {
                CuiHelper.DestroyUi(player, "CupboardSettings_button");
                CuiHelper.DestroyUi(player, "CupboardSettings_main");
            }
            );
        }
        public class CupboardSetting
        {
            public int MaxLimit;
            public bool Announcement;
            public bool AuthOther;
            public string CupboardName;
        }
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError("ImageLibrary not found! Plugin Unloaded");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }
            if (AddImage("https://i.imgur.com/4waRRlv.png", "https://i.imgur.com/4waRRlv.png")) Puts("Image library found! Images loaded");
            else PrintError("Что то пошло не так, ImageLibrary не загрузил изображение");
            permission.RegisterPermission(config.DefaultPermission, this);
            config.PrivilageList.ToList().ForEach(p => permission.RegisterPermission(p.Key, this));
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (!PlayerListed.ContainsKey(player.userID)) PlayerListed.Add(player.userID, new PlayerSetting());
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CupboardSetting_Players", PlayerListed);
        }
        void LoadData()
        {
            try
            {
                PlayerListed = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerSetting>>("CupboardSetting_Players");
            }
            catch
            {
                PlayerListed = new Dictionary<ulong, PlayerSetting>();
            }
        }
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string shortname, string name) => (bool)ImageLibrary.Call("AddImage", shortname, name);
        private static PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за заказ плагина на сайте TopPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Стандартный лимит авторизаций в шкафу")] public int StandartAuthLimit = 5;
            [JsonProperty(PropertyName = "Лимит авторизаций в шкафу с привилегией [Привилегия:Максимальный лимит]")] public Dictionary<string, int> PrivilageList = new Dictionary<string, int>();
            [JsonProperty(PropertyName = "Общая привилегия на использование UI настроек шкафа")] public string DefaultPermission = "cupboardsettings.allowed";
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    StandartAuthLimit = 5,
                    PrivilageList = new Dictionary<string, int>()
                    {
                        ["CupboardSettings.vip"] = 7,
                        ["CupboardSettings.elite"] = 10,
                    }
                }
                ;
            }
        }
        void Loaded()
        {
            LoadData();
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            timer.Repeat(300, 0, () => SaveData());
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            var cupboard = entity.gameObject.GetComponent<BuildingPrivlidge>();
            if (cupboard != null && cupboard.OwnerID == player.userID && permission.UserHasPermission(player.UserIDString, config.DefaultPermission))
            {
                CuiHelper.DestroyUi(player, "CupboardSettings_button");
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiButton
                {
                    RectTransform = {
                        AnchorMin="0.5 0", AnchorMax="0.5 0", OffsetMin="192 491", OffsetMax="573 556"
                    }
                    ,
                    Button = {
                        Color="0.57 0.74 0.27 1.00", Command=$"CupboardSettings_main {entity.net.ID}", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    ,
                    Text = {
                        Text="", Align=TextAnchor.MiddleCenter, Font="robotocondensed-regular.ttf", FontSize=24
                    }
                    ,
                }
                , "Overlay", "CupboardSettings_button");
                container.Add(new CuiElement
                {
                    Parent = "CupboardSettings_button",
                    Components = {
                        new CuiTextComponent {
                            Color="0.84 0.93 0.65 1.00", Text=Messages["MainButton"], FontSize=24, Font="robotocondensed-regular.ttf", Align=TextAnchor.MiddleCenter,
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0", AnchorMax=$"1 1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 0.5", Distance="1 1"
                        }
                    }
                    ,
                }
                );
                CuiHelper.AddUi(player, container);
            }
        }
        [ConsoleCommand("CupboardSettings_main")]
        void cmdCupboardSettings(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            DrawSettingMenu(player, uint.Parse(args.Args[0]));
        }
        public Dictionary<string, ulong> PlayerAuth(BaseEntity BuildingID)
        {
            var cup = BuildingID.GetBuildingPrivilege();
            return cup.authorizedPlayers.ToDictionary(p => p.username, y => y.userid);
        }
        public int GetPrivilage(string steamid)
        {
            var standart = config.StandartAuthLimit;
            foreach (var privilage in config.PrivilageList)
            {
                if (permission.UserHasPermission(steamid, privilage.Key))
                {
                    if (standart < privilage.Value) standart = privilage.Value;
                }
            }
            return standart;
        }
        void DrawSettingMenu(BasePlayer player, uint cupboardId)
        {
            CuiHelper.DestroyUi(player, "CupboardSettings_main");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0 0", AnchorMax="1 1"
                }
                ,
                Button = {
                    Color="0 0 0 0.95", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text="", Align=TextAnchor.MiddleCenter, Font="robotocondensed-regular.ttf", FontSize=24
                }
                ,
            }
            , "Overlay", "CupboardSettings_main");
            container.Add(new CuiElement()
            {
                Parent = "CupboardSettings_main",
                Components = {
                    new CuiTextComponent {
                        Text=Messages["MainText"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=35, FadeIn=0.5f
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0.9", AnchorMax="1 1"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 1", Distance="0.5 -0.5"
                    }
                }
            }
            );
            container.Add(new CuiElement()
            {
                Name = "CupboardSettings_mainClose",
                Parent = $"CupboardSettings_main",
                Components = {
                    new CuiRawImageComponent {
                        Png=GetImage("https://i.imgur.com/4waRRlv.png"), FadeIn=0.5f
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin=$"0.925 0.88", AnchorMax="0.98 0.98"
                    }
                    ,
                }
            }
            );
            container.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0 0", AnchorMax="1 1"
                }
                ,
                Button = {
                    Color="1 1 1 0", Close="CupboardSettings_main", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text="", Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=24
                }
                ,
            }
            , "CupboardSettings_mainClose");
            var entity = BaseEntity.serverEntities.Find(cupboardId);
            var cup = entity.GetComponent<BuildingPrivlidge>();
            if (cup != null)
            {
                if (!PlayerListed.ContainsKey(player.userID))
                    OnPlayerConnected(player);

                if (!PlayerListed[player.userID].CupboardsList.ContainsKey(cup.net.ID) && cup.OwnerID == player.userID)
                {
                    PlayerListed[player.userID].CupboardsList.Add(cup.net.ID, new CupboardSetting()
                    {
                        Announcement = false,
                        MaxLimit = GetPrivilage(cup.OwnerID.ToString()),
                        AuthOther = false,
                        CupboardName = "Без названия",
                    }
                    );
                }
                container.Add(new CuiElement()
                {
                    Name = $"CupboardSettings.ownerAvatar",
                    Parent = "CupboardSettings_main",
                    Components = {
                        new CuiRawImageComponent {
                            Png=GetImage(cup.OwnerID.ToString()), FadeIn=0.5f
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0.25 0.65", AnchorMax="0.39 0.9"
                        }
                        ,
                    }
                }
                );
                container.Add(new CuiElement
                {
                    Name = $"CupboardSettings.owner",
                    Parent = "CupboardSettings_main",
                    Components = {
                        new CuiTextComponent {
                            Text=$"{Messages["OwnerName"]}: {covalence.Players.FindPlayerById(cup.OwnerID.ToString()).Name}", Color="0.97 0.97 0.97 1.00", Font="robotocondensed-bold.ttf", Align=TextAnchor.UpperLeft, FontSize=20
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0.41 0.85", AnchorMax="0.75 0.9"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 1", Distance="0.5 -0.5"
                        }
                    }
                }
                );
                container.Add(new CuiElement
                {
                    Name = $"CupboardSettings.owner",
                    Parent = "CupboardSettings_main",
                    Components = {
                        new CuiTextComponent {
                            Text=$"{Messages["CupNameText"]}: {PlayerListed[player.userID].CupboardsList[cup.net.ID].CupboardName}", Color="0.97 0.97 0.97 1.00", Font="robotocondensed-bold.ttf", Align=TextAnchor.UpperLeft, FontSize=20
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0.41 0.8", AnchorMax="0.75 0.85"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 1", Distance="0.5 -0.5"
                        }
                    }
                }
                );
                container.Add(new CuiButton
                {
                    RectTransform = {
                        AnchorMin="0.65 0.82", AnchorMax="0.75 0.85"
                    }
                    ,
                    Button = {
                        Color="1 1 1 0.5", Command=cup.OwnerID==player.userID ? $"cupboardSetting_setname {cup.net.ID}": "", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    ,
                    Text = {
                        Text="Изменить", Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=12
                    }
                    ,
                }
                , $"CupboardSettings_main", $"CupboardSettings_main_setname");
                if (cup.OwnerID != player.userID) container.Add(new CuiElement
                {
                    Parent = "CupboardSettings_main_setname",
                    Components = {
                        new CuiImageComponent {
                            Color="0 0 0 0.1", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0 0", AnchorMax="1 1"
                        }
                    }
                }
                 );
                container.Add(new CuiElement
                {
                    Name = $"CupboardSettings.owner",
                    Parent = "CupboardSettings_main",
                    Components = {
                        new CuiTextComponent {
                            Text=$"{Messages["AuthOtherText"]}:", Color="0.97 0.97 0.97 1.00", Font="robotocondensed-bold.ttf", Align=TextAnchor.UpperLeft, FontSize=20
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0.41 0.65", AnchorMax="0.75 0.8"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 1", Distance="0.5 -0.5"
                        }
                    }
                }
                );
                container.Add(new CuiElement
                {
                    Name = "CupboardSettings_AuthList",
                    Parent = "CupboardSettings_main",
                    Components = {
                        new CuiRawImageComponent {
                            Color="1 1 1 0.1", Sprite="assets/content/ui/ui.background.tile.psd"
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0.25 0.2", AnchorMax="0.75 0.6"
                        }
                    }
                }
                );
                container.Add(new CuiElement
                {
                    Parent = "CupboardSettings_AuthList",
                    Components = {
                        new CuiTextComponent {
                            Text=Messages["AuthList"], Color="0.97 0.97 0.97 1.00", Font="robotocondensed-bold.ttf", Align=TextAnchor.MiddleCenter, FontSize=20
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0 1", AnchorMax="1 1.1"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 1", Distance="0.5 -0.5"
                        }
                    }
                }
                );
                var pos = GetPositions(5, 2, 0.03f, 0.03f, false);
                int i = 0;
                foreach (var target in PlayerAuth(cup).Take(10))
                {
                    container.Add(new CuiElement()
                    {
                        Name = $"CupboardSettings.{target.Key}",
                        Parent = "CupboardSettings_AuthList",
                        Components = {
                            new CuiImageComponent {
                                Color="0 0 0 0.1"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"{pos[i].AnchorMin}", AnchorMax=$"{pos[i].AnchorMax}"
                            }
                            ,
                        }
                    }
                    );
                    container.Add(new CuiElement()
                    {
                        Parent = $"CupboardSettings.{target.Key}",
                        Components = {
                            new CuiRawImageComponent {
                                Png=GetImage(target.Value.ToString()), FadeIn=0.5f
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"0 0.2", AnchorMax="1 1"
                            }
                            ,
                        }
                    }
                    );
                    container.Add(new CuiElement
                    {
                        Parent = $"CupboardSettings.{target.Key}",
                        Components = {
                            new CuiImageComponent {
                                Color="0 0 0 0.2", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin="0 0", AnchorMax="1 1"
                            }
                            ,
                        }
                    }
                    );
                    container.Add(new CuiElement()
                    {
                        Parent = $"CupboardSettings.{target.Key}",
                        Components = {
                            new CuiTextComponent {
                                Text=target.Key, Align=TextAnchor.LowerCenter, Font="robotocondensed-bold.ttf", FontSize=15, FadeIn=0.5f
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin="0 0.2", AnchorMax="1 1"
                            }
                            , new CuiOutlineComponent {
                                Color="0 0 0 1", Distance="1 -1"
                            }
                        }
                    }
                    );
                    container.Add(new CuiButton
                    {
                        RectTransform = {
                            AnchorMin="0.01 0.01", AnchorMax="0.99 0.18"
                        }
                        ,
                        Button = {
                            Color="0.71 0.10 0.07 1.00", Command=$"cupsetting.deauth {cup.net.ID} {target.Value}", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                        }
                        ,
                        Text = {
                            Text=Messages["DeAuth"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=12
                        }
                        ,
                    }
                    , $"CupboardSettings.{target.Key}");
                    i++;
                }
                container.Add(new CuiButton
                {
                    RectTransform = {
                        AnchorMin="0.3 0.12", AnchorMax="0.7 0.18"
                    }
                    ,
                    Button = {
                        Color="1 1 1 0.3", Command=GetPrivilage(cup.OwnerID.ToString()) > PlayerAuth(cup).Count && cup.OwnerID==player.userID ? $"cupboardsetting_playersList {cup.net.ID} 0": "", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    ,
                    Text = {
                        Text=GetPrivilage(cup.OwnerID.ToString()) > PlayerAuth(cup).Count ? Messages["AddUsers"]: Messages["AddUsersLimit"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=18
                    }
                    ,
                }
                , "CupboardSettings_main");
                CuiHelper.AddUi(player, container);
                if (cup.OwnerID == player.userID) CuiAddButtonOther(player, cup.net.ID, cup.OwnerID);
                LimitSetting(player, cup.net.ID, cup.OwnerID);
            }
        }
        void CuiAddButtonOther(BasePlayer player, uint netID, ulong playerid = 0)
        {
            CuiHelper.DestroyUi(player, "CupboardSettings_main_other");
            var elements = new CuiElementContainer();
            elements.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.65 0.77", AnchorMax="0.75 0.8"
                }
                ,
                Button = {
                    Color=!PlayerListed[player.userID].CupboardsList[netID].AuthOther ? "0.73 0.02 0.00 0.7": "0.04 0.69 0.19 0.7", Command=playerid==player.userID ? $"cupboardsetting_changeauth {netID}": "", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text=PlayerListed[player.userID].CupboardsList[netID].AuthOther ? Messages["AuthOtherEnabled"]: Messages["AuthOtherDisabled"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=18
                }
                ,
            }
            , "CupboardSettings_main", "CupboardSettings_main_other");
            CuiHelper.AddUi(player, elements);
        }
        [ConsoleCommand("cupboardSetting_setname")]
        void cmdSetAvatarOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, "cupboardSetting_setname");
            var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "cupboardSetting_setname",
                Parent = "CupboardSettings_main",
                Components = {
                    new CuiRawImageComponent {
                        Color="0 0 0 0.85", Sprite="assets/content/ui/ui.background.tile.psd", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                }
            }
            );
            elements.Add(new CuiElement
            {
                Name = "clans_setAvatar_input",
                Parent = "cupboardSetting_setname",
                Components = {
                    new CuiRawImageComponent {
                        Color="0.3294118 0.3294118 0.3294118 0.7", Sprite="assets/content/ui/ui.background.tile.psd"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.25 0.45", AnchorMax="0.67 0.55"
                    }
                }
            }
            );
            elements.Add(new CuiElement
            {
                Parent = "clans_setAvatar_input",
                Components = {
                    new CuiTextComponent {
                        Text="<size=18 >НАПИШИТЕ СЮДА НАЗВАНИЕ ШКАФА ИЛИ ВСТАВЬТЕ СКОПИРОВАННОЕ</size>", Color="1 0.9294118 0.8666667 0.1", Font="robotocondensed-bold.ttf", Align=TextAnchor.MiddleCenter
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                    ,
                }
            }
            );
            elements.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin=$"0.4 0.38", AnchorMax=$"0.6 0.44"
                }
                ,
                Button = {
                    Color="1 1 1 0.5", Close=$"cupboardSetting_setname", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text=Messages["ButtonClose"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=18
                }
                ,
            }
            , $"cupboardSetting_setname");
            elements.Add(new CuiElement()
            {
                Parent = "clans_setAvatar_input",
                Components = {
                    new CuiInputFieldComponent {
                        Align=TextAnchor.MiddleCenter, CharsLimit=20, FontSize=26, Command=$"cupboardsetting_changeName {args.Args[0]} ", Font="robotocondensed-bold.ttf", Text="",
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                }
            }
            );
            elements.Add(new CuiButton
            {
                Button = {
                    Color="0.25 0.25 0.23 0.9", Command=""
                }
                ,
                Text = {
                    Text="СОХРАНИТЬ", Align=TextAnchor.MiddleCenter, FontSize=14, Font="robotocondensed-bold.ttf", Color="1 0.9294118 0.8666667 1"
                }
                ,
                RectTransform = {
                    AnchorMin=$"1.01 0", AnchorMax=$"1.2 0.99"
                }
                ,
            }
            , "clans_setAvatar_input");
            CuiHelper.AddUi(player, elements);
        }
        [ConsoleCommand("cupboardsetting_playersList")]
        void cmdShowAllPlayers(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, "cupboardsetting_playersList");
            var cup = uint.Parse(args.Args[0]);
            var page = int.Parse(args.Args[1]);
            var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "cupboardsetting_playersList",
                Parent = "CupboardSettings_main",
                Components = {
                    new CuiRawImageComponent {
                        Color="0 0 0 0.85", Sprite="assets/content/ui/ui.background.tile.psd", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                }
            }
            );
            elements.Add(new CuiElement()
            {
                Parent = "cupboardsetting_playersList",
                Components = {
                    new CuiTextComponent {
                        Text=Messages["PlayersListTitle"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=30, FadeIn=0.5f
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0.9", AnchorMax="1 1"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 1", Distance="0.5 -0.5"
                    }
                }
            }
            );
            elements.Add(new CuiElement
            {
                Name = "cupboardsetting_playersList_AuthList",
                Parent = "cupboardsetting_playersList",
                Components = {
                    new CuiRawImageComponent {
                        Color="1 1 1 0.1", Sprite="assets/content/ui/ui.background.tile.psd"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.25 0.2", AnchorMax="0.75 0.9"
                    }
                }
            }
            );
            int i = 0;
            var pos = GetPositions(5, 3, 0.03f, 0.03f, false);
            var reply = 0;
            if (reply == 0) { }
            foreach (var target in covalence.Players.All.Skip(15 * page).Take(15))
            {
                elements.Add(new CuiElement()
                {
                    Name = $"CupboardSettings.{target.Id}",
                    Parent = "cupboardsetting_playersList_AuthList",
                    Components = {
                        new CuiImageComponent {
                            Color="0 0 0 0.1"
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"{pos[i].AnchorMin}", AnchorMax=$"{pos[i].AnchorMax}"
                        }
                        ,
                    }
                }
                );
                elements.Add(new CuiElement()
                {
                    Parent = $"CupboardSettings.{target.Id}",
                    Components = {
                        new CuiRawImageComponent {
                            Png=GetImage(target.Id.ToString()), FadeIn=0.5f
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin=$"0 0.2", AnchorMax="1 1"
                        }
                        ,
                    }
                }
                );
                elements.Add(new CuiElement()
                {
                    Parent = $"CupboardSettings.{target.Id}",
                    Components = {
                        new CuiTextComponent {
                            Text=target.Name, Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=15, FadeIn=0.5f
                        }
                        , new CuiRectTransformComponent {
                            AnchorMin="0 0", AnchorMax="1 0.2"
                        }
                        , new CuiOutlineComponent {
                            Color="0 0 0 1", Distance="1 -1"
                        }
                    }
                }
                );
                elements.Add(new CuiButton
                {
                    RectTransform = {
                        AnchorMin="0 0.2", AnchorMax="1 1"
                    }
                    ,
                    Button = {
                        Color="0.71 0.10 0.07 0", Command=$"cupboardsetting_authuser {cup} {target.Id}", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    }
                    ,
                    Text = {
                        Text=Messages["Auth"], Align=TextAnchor.LowerCenter, Font="robotocondensed-bold.ttf", FontSize=12
                    }
                    ,
                }
                , $"CupboardSettings.{target.Id}");
                i++;
            }
            if (page > 0) elements.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.25 0.15", AnchorMax="0.3 0.19"
                }
                ,
                Button = {
                    Color="1 1 1 0.5", Command=$"cupboardsetting_playersList {cup} {page - 1}", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text="<", Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=25
                }
                ,
            }
            , $"cupboardsetting_playersList");
            if (covalence.Players.All.Skip(15 * page).ToList().Count > 15) elements.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.7 0.15", AnchorMax="0.75 0.19"
                }
                ,
                Button = {
                    Color="1 1 1 0.5", Command=$"cupboardsetting_playersList {cup} {page + 1}", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text=">", Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=25
                }
                ,
            }
            , $"cupboardsetting_playersList");
            elements.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin=$"0.4 0.05", AnchorMax=$"0.6 0.13"
                }
                ,
                Button = {
                    Color="1 1 1 0.5", Close=$"cupboardsetting_playersList", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                Text = {
                    Text=Messages["ButtonClose"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=18
                }
                ,
            }
            , $"cupboardsetting_playersList");
            CuiHelper.AddUi(player, elements);
        }
        [ConsoleCommand("cupboardsetting_changeauth")]
        void cmdChangeAuthOther(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var cup = PlayerListed[player.userID].CupboardsList[uint.Parse(args.Args[0])];
            if (cup.AuthOther) cup.AuthOther = false;
            else cup.AuthOther = true;
            CuiAddButtonOther(player, uint.Parse(args.Args[0]), player.userID);
        }
        [ConsoleCommand("cupboardsetting_authuser")]
        void cmdAuthNewUSer(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var target = covalence.Players.FindPlayerById(args.Args[1]);
            var entity = BaseEntity.serverEntities.Find(uint.Parse(args.Args[0])) as BuildingPrivlidge;
            if (entity != null)
            {
                if (entity.authorizedPlayers.FirstOrDefault(p => p.userid == ulong.Parse(target.Id)) != null) return;
                if (entity.authorizedPlayers.Count >= PlayerListed[player.userID].CupboardsList[entity.net.ID].MaxLimit)
                {
                    SendReply(player, Messages["AuthLimit"]);
                    return;
                }
                entity.authorizedPlayers.Add(new ProtoBuf.PlayerNameID()
                {
                    username = target.Name,
                    userid = ulong.Parse(target.Id)
                }
                );
                DrawSettingMenu(player, uint.Parse(args.Args[0]));
            }
        }
        [ConsoleCommand("cupboardsetting_changeName")]
        void cmdChangeAvatarOfClan(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (args.GetString(1) == "") return;
            var cup = PlayerListed[player.userID].CupboardsList[uint.Parse(args.Args[0])];
            var name = args.FullString.Skip(args.Args[0].Length + 1);
            cup.CupboardName = string.Join("", name);
            DrawSettingMenu(player, uint.Parse(args.Args[0]));
        }
        [ConsoleCommand("cupsetting.deauth")]
        void cmdDeAutorization(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var entity = BaseEntity.serverEntities.Find(uint.Parse(arg.Args[0]));
            var target = ulong.Parse(arg.Args[1]);
            var cup = entity.GetComponent<BuildingPrivlidge>();
            if (cup != null) cup.authorizedPlayers.RemoveAll(a => a.userid == target);
            if (player.userID == target) player.EndLooting();
            else DrawSettingMenu(player, entity.net.ID);
        }
        [ConsoleCommand("cupsetting.changelimit")]
        void cmdChangeCupLimit(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            uint cup = uint.Parse(arg.Args[0]);
            switch (arg.Args[1])
            {
                case "+":
                    if (PlayerListed[player.userID].CupboardsList[cup].MaxLimit < GetPrivilage(player.UserIDString)) PlayerListed[player.userID].CupboardsList[cup].MaxLimit++;
                    break;
                case "-":
                    if (PlayerListed[player.userID].CupboardsList[cup].MaxLimit >= 2) PlayerListed[player.userID].CupboardsList[cup].MaxLimit--;
                    break;
            }
            LimitSetting(player, cup, player.userID);
        }
        [ConsoleCommand("cupsetting.enabledanno")]
        void cmdEnabledAnnouncement(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            uint cup = uint.Parse(arg.Args[0]);
            if (PlayerListed[player.userID].CupboardsList[cup].Announcement) PlayerListed[player.userID].CupboardsList[cup].Announcement = false;
            else PlayerListed[player.userID].CupboardsList[cup].Announcement = true;
            LimitSetting(player, cup, player.userID);
        }
        void LimitSetting(BasePlayer player, uint cup, ulong ownerID)
        {
            CuiHelper.DestroyUi(player, "CupboardSettings_limitsetting");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "CupboardSettings_limitsetting",
                Parent = "CupboardSettings_main",
                Components = {
                    new CuiRawImageComponent {
                        Color="1 1 1 0.05", Sprite="assets/content/ui/ui.background.tile.psd"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.41 0.65", AnchorMax="0.75 0.73"
                    }
                }
            }
            );
            container.Add(new CuiElement()
            {
                Parent = "CupboardSettings_limitsetting",
                Components = {
                    new CuiTextComponent {
                        Text=Messages["LimitTextUI"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=12
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.01 0.7", AnchorMax="0.4 1"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 1", Distance="1 -1"
                    }
                }
            }
            );
            container.Add(new CuiElement
            {
                Name = "CupboardSettings_limit",
                Parent = "CupboardSettings_limitsetting",
                Components = {
                    new CuiRawImageComponent {
                        Color="1 1 1 0.05", Sprite="assets/content/ui/ui.background.tile.psd"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.01 0.05", AnchorMax="0.4 0.7"
                    }
                }
            }
            );
            container.Add(new CuiElement()
            {
                Parent = "CupboardSettings_limit",
                Components = {
                    new CuiTextComponent {
                        Text=PlayerListed[player.userID].CupboardsList[cup].MaxLimit.ToString(), Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=20
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0 0", AnchorMax="1 1"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 1", Distance="1 -1"
                    }
                }
            }
            );
            container.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.01 0.1", AnchorMax="0.3 0.9"
                }
                ,
                Button = {
                    Color="1 1 1 0.05", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat", Command=player.userID==ownerID ? $"cupsetting.changelimit {cup} -": ""
                }
                ,
                Text = {
                    Text="-", Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=20
                }
                ,
            }
            , $"CupboardSettings_limit");
            container.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.7 0.1", AnchorMax="0.98 0.9"
                }
                ,
                Button = {
                    Color="1 1 1 0.05", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat", Command=player.userID==ownerID ? $"cupsetting.changelimit {cup} +": ""
                }
                ,
                Text = {
                    Text="+", Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=20
                }
                ,
            }
            , $"CupboardSettings_limit");
            container.Add(new CuiElement()
            {
                Parent = "CupboardSettings_limitsetting",
                Components = {
                    new CuiTextComponent {
                        Text=Messages["LimitbuttonTitle"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=12
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.5 0.7", AnchorMax="1 1"
                    }
                    , new CuiOutlineComponent {
                        Color="0 0 0 1", Distance="1 -1"
                    }
                }
            }
            );
            container.Add(new CuiButton
            {
                RectTransform = {
                    AnchorMin="0.5 0.05", AnchorMax="0.98 0.7"
                }
                ,
                Button = {
                    Color=PlayerListed[player.userID].CupboardsList[cup].Announcement ? "0.73 0.02 0.00 0.7": "0.04 0.69 0.19 0.7", Material="assets/content/ui/uibackgroundblur-ingamemenu.mat", Command=player.userID==ownerID ? $"cupsetting.enabledanno {cup}": ""
                }
                ,
                Text = {
                    Text=PlayerListed[player.userID].CupboardsList[cup].Announcement ? Messages["LimitbuttonDisable"]: Messages["LimitbuttonEnabled"], Align=TextAnchor.MiddleCenter, Font="robotocondensed-bold.ttf", FontSize=16
                }
                ,
            }
            , $"CupboardSettings_limitsetting");
            CuiHelper.AddUi(player, container);
        }
        class Position
        {
            public float Xmin;
            public float Xmax;
            public float Ymin;
            public float Ymax;
            public string AnchorMin => $"{Math.Round(Xmin, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymin, 4).ToString(CultureInfo.InvariantCulture)}";
            public string AnchorMax => $"{Math.Round(Xmax, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymax, 4).ToString(CultureInfo.InvariantCulture)}";
            public override string ToString()
            {
                return "----------\nAmin:{AnchorMin}\nAmax:{AnchorMax}\n----------‌​​​‍";
            }
        }
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static List<Position> GetPositions(int colums, int rows, float colPadding = 0, float rowPadding = 0, bool columsFirst = false)
        {
            if (colums == 0) throw new ArgumentException("Can't create positions for gui!‌​​​‍", nameof(colums));
            if (rows == 0) throw new ArgumentException("Can't create positions for gui!", nameof(rows));
            List<Position> result = new List<Position>();
            result.Clear();
            var colsDiv = 1f / colums;
            var rowsDiv = 1f / rows;
            if (colPadding == 0) colPadding = colsDiv / 2;
            if (rowPadding == 0) rowPadding = rowsDiv / 2;
            if (!columsFirst) for (int j = rows;
            j >= 1;
            j--)
                {
                    for (int i = 1;
                    i <= colums;
                    i++)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        }
                        ;
                        result.Add(pos);
                    }
                }
            else for (int i = 1;
            i <= colums;
            i++)
                {
                    for (int j = rows;
                    j >= 1;
                    j--)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        }
                        ;
                        result.Add(pos);
                    }
                }
            return result;
        }
        Dictionary<string, string> Messages = new Dictionary<string, string>() {
                {
                "MainButton", "<b>НАСТРОЙКИ ШКАФА</b>"
            }
            , {
                "DeAuth", "<b>ДЕАВТОРИЗОВАТЬ</b>"
            }
            , {
                "Auth", "<b>АВТОРИЗОВАТЬ</b>"
            }
            , {
                "AuthList", "<b>СПИСОК АВТОРИЗОВАННЫХ В ШКАФУ</b>"
            }
            , {
                "OwnerName", "<b>СОЗДАТЕЛЬ ШКАФА</b>"
            }
            , {
                "MainText", "<b>НАСТРОЙКИ ВЫШЕГО ШКАФА</b>"
            }
            , {
                "LimitText", "<b>АВТОРИЗОВАНЫХ ИГРОКОВ / ЛИМИТ</b>"
            }
            , {
                "CupNameText", "<b>ИМЯ ШКАФА</b>"
            }
            , {
                "LimitTextUI", "МАКС. ИГРОКОВ"
            }
            , {
                "LimitbuttonTitle", "ОПОВЕЩЕНИЕ ОБ АВТОРИЗАЦИИ"
            }
            , {
                "LimitbuttonEnabled", "ВКЛЮЧИТЬ"
            }
            , {
                "LimitbuttonDisable", "ОТКЛЮЧИТЬ"
            }
            , {
                "CupboardLimit", "ИЗВИНИТЕ! Но создатель шкафа установил ограничение на количество авторизованых игроков."
            }
            , {
                "AuthLimit", "ИЗВИНИТЕ! Вы превышаете лимит авторизованых игроков."
            }
            , {
                "PlayerAuth", "<color=red>Внимание</color>, игрок под именем <color=green>{0}</color> авторизовался в вашем шкафу: {1}.\n<size=10>Это автоматическое сообщение, так как вы включили оповещения об авторизациях</size>"
            }
            , {
                "AddUsers", "<b>АВТОРИЗОВАТЬ НОВОГО ИГРОКА</b>"
            }
            , {
                "AddUsersLimit", "<b>МАКСИМАЛЬНО ИГРОКОВ</b>"
            }
            , {
                "PlayersListTitle", "<b>АВТОРИЗАЦИЯ НОВОГО ИГРОКА\n<size=14>Выберите нужного со списка</size></b>"
            }
            , {
                "ButtonClose", "<b>ЗАКРЫТЬ</b>"
            }
            , {
                "AuthOtherText", "<b>ОТКЛЮЧИТЬ АВТОРИЗАЦИЮ</b>"
            }
            , {
                "AuthOtherEnabled", "<b>ВКЛЮЧЕНО</b>"
            }
            , {
                "AuthOtherDisabled", "<b>ОТКЛЮЧЕНО</b>"
            }
            ,
        }
        ;
        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null || privilege?.net.ID == null) return null;
            var entity = PlayerListed.FirstOrDefault(p => p.Value.CupboardsList.ContainsKey(privilege.net.ID));
            if (entity.Key != 0 && entity.Key >= 76560000000000000L)
            {
                if (privilege.authorizedPlayers.Count >= entity.Value.CupboardsList[privilege.net.ID].MaxLimit && entity.Key != privilege.OwnerID)
                {
                    SendReply(player, Messages["CupboardLimit"]);
                    return false;
                }
                if (entity.Value.CupboardsList[privilege.net.ID].AuthOther && entity.Key != privilege.OwnerID)
                {
                    SendReply(player, Messages["CupboardLimit"]);
                    return false;
                }
                if (entity.Value.CupboardsList[privilege.net.ID].Announcement && entity.Key != privilege.OwnerID)
                {
                    var owner = BasePlayer.FindByID(entity.Key);
                    if (owner != null) SendReply(owner, Messages["PlayerAuth"], player.displayName, entity.Value.CupboardsList[privilege.net.ID].CupboardName);
                }
                return null;
            }
            return null;
        }
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (player == null || entity == null || entity?.net.ID == null) return;
            CuiHelper.DestroyUi(player, "CupboardSettings_button");
            CuiHelper.DestroyUi(player, "CupboardSettings_main");
        }
    }
}