using Newtonsoft.Json; 
using Newtonsoft.Json.Converters; 
using Newtonsoft.Json.Linq; 
using Oxide.Core; 
using Oxide.Core.Configuration; 
using Oxide.Core.Plugins; 
using Oxide.Game.Rust.Cui; 
using System; using System.Collections.Generic; 
using System.Globalization; 
using System.Linq; 
using UnityEngine; 

namespace Oxide.Plugins 
{ 
    [Info("TPBotbonus", "Sempai#3239", "4.0.1")] 
    class TPBotbonus : RustPlugin 
    { 
        [PluginReference] Plugin Duel, ImageLibrary; 
        
        static string apiver = "v=5.92"; 
        
        private bool OxideUpdateSended = false; 
        private System.Random random = new System.Random(); 
        private bool NewWipe = false; 
        JsonSerializerSettings jsonsettings; 

        private List<string> allowedentity = new List<string>() 
        { 
            "door", 
            "wall.window.bars.metal", 
            "wall.window.bars.toptier", 
            "wall.external", 
            "gates.external.high", 
            "floor.ladder", 
            "embrasure", 
            "floor.grill", 
            "wall.frame.fence", 
            "wall.frame.cell", 
            "foundation", 
            "floor.frame", 
            "floor.triangle", 
            "floor", 
            "foundation.steps", 
            "foundation.triangle", 
            "roof", 
            "stairs.l", 
            "stairs.u", 
            "wall.doorway", 
            "wall.frame", 
            "wall.half", 
            "wall.low", 
            "wall.window", 
            "wall", 
            "wall.external.high.stone" 
        }; 
        
        List<string> ExplosiveList = new List<string>() 
        { 
            "explosive.satchel.deployed", 
            "grenade.f1.deployed", 
            "grenade.beancan.deployed", 
            "explosive.timed.deployed" 
        }; 
        
        private List<ulong> BDayPlayers = new List<ulong>(); 
        
        class GiftItem 
        { 
            public string shortname; 
            public ulong skinid; 
            public int count; 
        } 
        
        class ServerInfo 
        { 
            public string name; 
            public string online; 
            public string slots; 
            public string sleepers; 
            public string map; 
        } 
        
        private Dictionary<BasePlayer, DateTime> GiftsList = new Dictionary<BasePlayer, DateTime>(); 
        
        private ConfigData config; 
        private class ConfigData 
        { 
            [JsonProperty(PropertyName = "Ключи VK API, ID группы")] public VKAPITokens VKAPIT;
            [JsonProperty(PropertyName = "Награда за вступление в группу")] public GroupGifts GrGifts;
            [JsonProperty(PropertyName = "Оповещения при вайпе")] public WipeSettings WipeStg;
            
            public class VKAPITokens 
            { 
                [JsonProperty(PropertyName = "VK Token группы (для сообщений)")] public string VKToken = "vk1.a.OdN7gCS7swWRnK4ypRBdPObwJOyUpa-fOSJQFOupnOnYVTi0vLlrMFAFz3lOVbDmhfi6puF3amFAGE43KHh-JCaexjteVScr8SzW0UFTaxjLYkAu423ssvVUwQqsdeqEKcSSo4u69f2RORMjTEey7tkxVG8ZfiuvrPHq_KtqIhELYRu-JP12jJQ-ZOMx0X1weBlqOrOcWPWmb6IX7lMHEg"; 
                [JsonProperty(PropertyName = "VK Token приложения (для записей на стене и статуса)")] public string VKTokenApp = ""; 
                [JsonProperty(PropertyName = "VKID группы")] public string GroupID = "223960721"; 
            } 
            public class GroupGifts 
            { 
                [JsonProperty(PropertyName = "Выдавать подарок игроку за вступление в группу ВК?")] public bool VKGroupGifts = true; 
                [JsonProperty(PropertyName = "Подарок за вступление в группу (команда, если стоит none выдаются предметы из файла data/TPBotbonus.json). Пример: grantperm {steamid} vkraidalert.allow 7d")] public string VKGroupGiftCMD = "none"; 
                [JsonProperty(PropertyName = "Описание команды")] public string GiftCMDdesc = "Оповещения о рейде на 7 дней"; 
                [JsonProperty(PropertyName = "Ссылка на группу ВК")] public string VKGroupUrl = "https://vk.com/rustage_su"; 
                [JsonProperty(PropertyName = "Оповещения в общий чат о получении награды")] public bool GiftsBool = true; 
                [JsonProperty(PropertyName = "Включить оповещения для игроков не получивших награду за вступление в группу?")] public bool VKGGNotify = true; 
                [JsonProperty(PropertyName = "Интервал оповещений для игроков не получивших награду за вступление в группу (в минутах)")] public int VKGGTimer = 30; 
                [JsonProperty(PropertyName = "Выдавать награду каждый вайп?")] public bool GiftsWipe = true; 
            } 
            public class WipeSettings 
            { 
                [JsonProperty(PropertyName = "Отправлять пост в группу после вайпа?")] public bool WPostB = true; 
                [JsonProperty(PropertyName = "Текст поста о вайпе")] public string WPostMsg = "RUSTAGE | WIPE {wipedate}"; 
                [JsonProperty(PropertyName = "Добавить изображение к посту о вайпе?")] public bool WPostAttB = false; 
                [JsonProperty(PropertyName = "Отправлять сообщение администратору о вайпе?")] public bool WPostMsgAdmin = true; 
                [JsonProperty(PropertyName = "Ссылка на изображение к посту о вайпе вида 'photo-1_265827614' (изображение должно быть в альбоме группы)")] public string WPostAtt = "photo-223960721_456239064"; 
                [JsonProperty(PropertyName = "Отправлять игрокам сообщение о вайпе автоматически?")] public bool WMsgPlayers = true; 
                [JsonProperty(PropertyName = "Текст сообщения игрокам о вайпе (сообщение отправляется только тем кто подтвердил профиль)")] public string WMsgText = "Сервер вайпнут! Залетай скорее!";
                [JsonProperty(PropertyName = "Игнорировать тех кто подтвердил профиль? (если включено, сообщение о вайпе будет отправляться всем)")] public bool WCMDIgnore = false; [JsonProperty(PropertyName = "Смена названия группы после вайпа")] public bool GrNameChange = false; 
                [JsonProperty(PropertyName = "Название группы (переменная {wipedate} отображает дату последнего вайпа)")] public string GrName = "ServerName | WIPE {wipedate}"; 
            } 
        } 
        
        private void LoadVariables() 
        { 
            bool changed = false; 
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate; 
            config = Config.ReadObject<ConfigData>(); 
            if (config.GrGifts == null) 
            { 
                config.GrGifts = new ConfigData.GroupGifts(); 
                changed = true; 
            } 
            if (config.WipeStg == null) 
            { 
                config.WipeStg = new ConfigData.WipeSettings(); 
                changed = true; 
            } 
            if (changed) PrintWarning("Конфигурационный файл обновлен"); 
        } 
        
        protected override void LoadDefaultConfig() 
        { 
            var configData = new ConfigData 
            {
                VKAPIT = new ConfigData.VKAPITokens(), 
                GrGifts = new ConfigData.GroupGifts(),
                WipeStg = new ConfigData.WipeSettings()
            }; 
            Config.WriteObject(configData, true); 
        } 
        
        class DataStorageStats 
        { 
            public int WoodGath; 
            public int SulfureGath; 
            public int Rockets; 
            public int Blueprints; 
            public int Explosive; 
            public int Reports; 
            public List<GiftItem> Gifts; 
            public DataStorageStats() { } 
        } 
        class DataStorageUsers 
        { 
            public Dictionary<ulong, VKUDATA> VKUsersData = new Dictionary<ulong, VKUDATA>(); 
            public DataStorageUsers() { } 
        } 
        class VKUDATA 
        { 
            public ulong UserID; 
            public string Name; 
            public string VkID; 
            public string VkOwnerID; 
            public int ConfirmCode; 
            public bool Confirmed; 
            public bool GiftRecived; 
            public string LastRaidNotice; 
            public bool WipeMsg; 
            public string Bdate; 
            public int Raids; 
            public int Kills; 
            public int Farm; 
            public string LastSeen; 
        } 
        class DataStorageReports 
        { 
            public Dictionary<int, REPORT> VKReportsData = new Dictionary<int, REPORT>(); 
            public DataStorageReports() { } 
        } 
        class REPORT 
        { 
            public ulong UserID; 
            public string Name;
            public string Text; 
        } 

        DataStorageStats statdata; 
        DataStorageUsers usersdata; 
        DataStorageReports reportsdata; 
        private DynamicConfigFile VKBData; 
        private DynamicConfigFile StatData; 
        private DynamicConfigFile ReportsData; 
        
        void LoadData() 
        { 
            try 
            { 
                statdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageStats>("TPBotbonus"); 
                usersdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageUsers>("TPBotbonusUsers"); 
                reportsdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageReports>("TPBotbonusReports"); 
            } 
            catch 
            { 
                statdata = new DataStorageStats(); usersdata = new DataStorageUsers(); 
                reportsdata = new DataStorageReports(); 
            } 
        } 
        
        private void OnServerInitialized() 
        {			
            PrintWarning("\n-----------------------------\n " +" Author - Sempai#3239\n " +" VK - https://vk.com/rustnastroika\n " +" Forum - https://topplugin.ru\n " +" Discord - https://discord.gg/5DPTsRmd3G\n" +"-----------------------------");
            
            LoadVariables(); 
            
            VKBData = Interface.Oxide.DataFileSystem.GetFile("TPBotbonusUsers");
            StatData = Interface.Oxide.DataFileSystem.GetFile("TPBotbonus");
            ReportsData = Interface.Oxide.DataFileSystem.GetFile("TPBotbonusReports");
            ImageLibrary.Call("AddImage", VkICO, ".VkICO"); 
            ImageLibrary.Call("AddImage", GiftICO, ".GiftICO"); 
            ImageLibrary.Call("AddImage", AlertICO, ".AlertICO"); 
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_bg.png", "BackgroundImage"); 
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_btn_block.png", "ButtonBlock");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_modal_connect.png", "alerts");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_modal_connect.png", "alerts1");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_modal_field.png", "alertvkback");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_modal_delete.png", "vkdeleteback");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/bonus_modal_getbonus.png", "giftrewardback");
            
            permission.RegisterPermission(AlertPermission, this); 
            
            string msg2 = null; 
            msg2 = $"[TPBotbonus] Сервер успешно загружен."; 
            
            LoadData(); 

            if (NewWipe)		
        	WipeFunctions();
            
            if (statdata.Gifts == null) 
            { 
                statdata.Gifts = new List<GiftItem>() 
                { 
                    new GiftItem 
                    { 
                        shortname = "supply.signal", 
                        count = 1, 
                        skinid = 0 
                    }, 
                    new GiftItem 
                    { 
                        shortname = "pookie.bear", 
                        count = 2, 
                        skinid = 0 
                    } 
                }; 
                
                StatData.WriteObject(statdata); 
            } 
            if (config.GrGifts.VKGGNotify) 
                timer.Repeat(config.GrGifts.VKGGTimer * 60, 0, GiftNotifier); 
        } 
        
        private void Init() 
        { 
            cmd.AddChatCommand("regvk", this, "VKcommand"); 
            cmd.AddConsoleCommand("updatestatus", this, "UStatus"); 
            cmd.AddConsoleCommand("updatewidget", this, "UWidget"); 
            cmd.AddConsoleCommand("updatelabel", this, "ULabel"); 
            cmd.AddConsoleCommand("sendmsgadmin", this, "MsgAdmin"); 
            cmd.AddConsoleCommand("wipealerts", this, "WipeAlerts"); 
            cmd.AddConsoleCommand("userinfo", this, "GetUserInfo"); 
            cmd.AddConsoleCommand("report.answer", this, "ReportAnswer"); 
            cmd.AddConsoleCommand("reports.list", this, "ReportList"); 
            cmd.AddConsoleCommand("report.wipe", this, "ReportClear"); 
            cmd.AddConsoleCommand("usersdata.update", this, "UpdateUsersData"); 
            
            jsonsettings = new JsonSerializerSettings(); 
            jsonsettings.Converters.Add(new KeyValuePairConverter()); 
        } 
        
        private void Loaded() => LoadMessages(); 
        
        private void Unload() 
        { 
            UnloadAllGUI(); 
        } 
        
        private void OnNewSave(string filename) => NewWipe = true; 
        
        private void OnPlayerInit(BasePlayer player) 
        { 
            if (usersdata.VKUsersData.ContainsKey(player.userID) && usersdata.VKUsersData[player.userID].Name != player.displayName) 
            { 
                usersdata.VKUsersData[player.userID].Name = player.displayName; VKBData.WriteObject(usersdata); 
            }  
        } 
            
        private void OnPlayerDisconnected(BasePlayer player, string reason) 
        {  
            if (usersdata.VKUsersData.ContainsKey(player.userID)) 
            { 
                usersdata.VKUsersData[player.userID].LastSeen = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); 
                VKBData.WriteObject(usersdata); 
            } 
        }   
            private void UpdateUsersData(ConsoleSystem.Arg arg) 
            { 
                if (arg.IsAdmin != true) return; 
                DeleteOldUsers(arg.Args?[0]); 
            } 
            private void CheckVkUser(BasePlayer player, string url) 
            { 
                string Userid = null; 
                string[] arr1 = url.Split('/'); 
                string vkname = arr1[arr1.Length - 1]; 
                webrequest.Enqueue("https://api.vk.com/method/users.get?user_ids=" + vkname + "&" + apiver + "&fields=bdate&access_token=" + config.VKAPIT.VKToken, null, (code, response) => { 
                    if (!response.Contains("error")) 
                    { 
                        var json = JObject.Parse(response); 
                        Userid = (string)json["response"][0]["id"]; 
                        string bdate = (string)json["response"][0]["bdate"] ?? "noinfo"; 
                        if (Userid != null) 
                            AddVKUser(player, Userid, bdate); 
                        else 
                            PrintToChat(player, "Ошибка обработки вашей ссылки ВК, обратитесь к администратору."); 
                    } 
                    else 
                    { 
                        PrintWarning($"Ошибка проверки ВК профиля игрока {player.displayName} ({player.userID}). URL - {url}"); 
                        Log("checkresponce", $"Ошибка проверки ВК профиля игрока {player.displayName} ({player.userID}). URL - {url}. Ответ сервера ВК: {response}"); 
                    } 
                }, this); 
            } 
            
            private void AddVKUser(BasePlayer player, string Userid, string bdate) 
            { 
                if (!usersdata.VKUsersData.ContainsKey(player.userID)) 
                { 
                    usersdata.VKUsersData.Add(player.userID, new VKUDATA() 
                    { 
                        UserID = player.userID, 
                        Name = player.displayName, 
                        VkID = Userid, 
                        ConfirmCode = random.Next(1, 9999999), 
                        Confirmed = false, 
                        GiftRecived = false, 
                        Bdate = bdate, 
                        Farm = 0, 
                        Kills = 0, 
                        Raids = 0, 
                        LastSeen = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") 
                    }); 
                    VKBData.WriteObject(usersdata); 
                    SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /regvk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player); 
                } 
                else 
                { 
                    if (Userid == usersdata.VKUsersData[player.userID].VkID && usersdata.VKUsersData[player.userID].Confirmed) 
                    { 
                        PrintToChat(player, string.Format(GetMsg("ПрофильДобавленИПодтвержден"))); 
                        return; 
                    } 
                    if (Userid == usersdata.VKUsersData[player.userID].VkID && !usersdata.VKUsersData[player.userID].Confirmed) 
                    { 
                        PrintToChat(player, string.Format(GetMsg("ПрофильДобавлен"))); 
                        return; 
                    } 
                    usersdata.VKUsersData[player.userID].Name = player.displayName; 
                    usersdata.VKUsersData[player.userID].VkID = Userid; 
                    usersdata.VKUsersData[player.userID].Confirmed = false; 
                    usersdata.VKUsersData[player.userID].ConfirmCode = random.Next(1, 9999999); 
                    usersdata.VKUsersData[player.userID].Bdate = bdate; 
                    VKBData.WriteObject(usersdata); 
                    SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /regvk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player); 
                } 
            } 
            
            private void VKcommand(BasePlayer player, string cmd, string[] args) 
            { 
                Effect Confirmed = new Effect("assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab", player, 0, new Vector3(), new Vector3()); 
                if (args.Length > 0) 
                { 
                    if (args[0] == "add") 
                    { 
                        if (args.Length == 1) 
                        { 
                            PrintToChat(player, string.Format(GetMsg("Подсказка"))); 
                            return; 
                        } 
                        if (!args[1].Contains("vk.com/")) 
                        { 
                            PrintToChat(player, string.Format(GetMsg("НеправильнаяСсылка"))); 
                            return; 
                        } 
                        CheckVkUser(player, args[1]); 
                    } 
                    if (args[0] == "confirm") 
                    { 
                        if (args.Length >= 2) 
                        { 
                            if (usersdata.VKUsersData.ContainsKey(player.userID)) 
                            { 
                                if (usersdata.VKUsersData[player.userID].Confirmed) 
                                { 
                                    PrintToChat(player, string.Format(GetMsg("ПрофильДобавленИПодтвержден"))); 
                                    return; 
                                } 
                                if (args[1] == usersdata.VKUsersData[player.userID].ConfirmCode.ToString()) 
                                { 
                                    usersdata.VKUsersData[player.userID].Confirmed = true; 
                                    VKBData.WriteObject(usersdata); 
                                    PrintToChat(player, string.Format(GetMsg("ПрофильПодтвержден"))); 
                                    EffectNetwork.Send(Confirmed, player.Connection); 
                                    if (config.GrGifts.VKGroupGifts) 
                                        PrintToChat(player, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl)); 
                                } 
                                else 
                                    PrintToChat(player, string.Format(GetMsg("НеверныйКод"))); 
                            } 
                            else 
                                PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); 
                        } 
                        else 
                        { 
                            if (!usersdata.VKUsersData.ContainsKey(player.userID)) 
                            { 
                                PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); 
                                return; 
                            } 
                            if (usersdata.VKUsersData[player.userID].Confirmed) 
                            { 
                                PrintToChat(player, string.Format(GetMsg("ПрофильДобавленИПодтвержден"))); 
                                return; 
                            } 
                            SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /regvk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player); 
                        } 
                    } 
                    if (args[0] == "gift") FixedGifts(player); 
                    if (args[0] == "wipealerts") 
                        WAlert(player); 
                    if (args[0] != "add" && args[0] != "gift" && args[0] != "confirm") 
                    { 
                        PrintToChat(player, string.Format(GetMsg("ДоступныеКоманды"))); 
                        if (config.GrGifts.VKGroupGifts) 
                            PrintToChat(player, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl)); 
                    } 
                } 
                else 
                    PrintToChat(player, string.Format(GetMsg("ДоступныеКоманды"))); 
            } 
                
            private void WAlert(BasePlayer player) 
            { 
                if (!usersdata.VKUsersData.ContainsKey(player.userID)) 
                { 
                    PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); 
                    return; 
                } 
                if (!usersdata.VKUsersData[player.userID].Confirmed) 
                { 
                    PrintToChat(player, string.Format(GetMsg("ПрофильНеПодтвержден"))); 
                    return; 
                } 
                if (usersdata.VKUsersData[player.userID].WipeMsg) 
                { 
                    usersdata.VKUsersData[player.userID].WipeMsg = false; 
                    VKBData.WriteObject(usersdata); 
                    PrintToChat(player, string.Format(GetMsg("ПодпискаОтключена"))); 
                } 
                else 
                { 
                    usersdata.VKUsersData[player.userID].WipeMsg = true; 
                    VKBData.WriteObject(usersdata); 
                    PrintToChat(player, string.Format(GetMsg("ПодпискаВключена"))); 
                } 
            } 
            
            private void VKGift(BasePlayer player) 
            { 
                if (config.GrGifts.VKGroupGifts) 
                { 
                    if (!usersdata.VKUsersData.ContainsKey(player.userID)) 
                    { 
                        PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); 
                        return; 
                    } 
                    if (!usersdata.VKUsersData[player.userID].Confirmed) 
                    { 
                        PrintToChat(player, string.Format(GetMsg("ПрофильНеПодтвержден"))); 
                        return; 
                    } 
                    if (usersdata.VKUsersData[player.userID].GiftRecived) 
                    { 
                        PrintToChat(player, string.Format(GetMsg("НаградаУжеПолучена"))); 
                        return; 
                    } 
                    webrequest.Enqueue($"https://api.vk.com/method/groups.isMember?group_id={config.VKAPIT.GroupID}&user_id={usersdata.VKUsersData[player.userID].VkID}&" + apiver + $"&access_token={config.VKAPIT.VKToken}", null, (code, response) => { 
                        if (response == null || !response.Contains("response")) return; 
                            var json = JObject.Parse(response); 
                            
                            if (json == null) return; 
                                string Result = (string)json["response"]; 
                                
                                if (Result == null) return; 
                                GetGift(code, Result, player); 
                            }, this); 
                    } 
                    else 
                        PrintToChat(player, string.Format(GetMsg("ФункцияОтключена"))); 
                } 
                    
                private void GetGift(int code, string Result, BasePlayer player) 
                { 
                    string msg2 = null; 
                    msg2 = $"[TPBotbonus] Игрок {player.displayName} ({player.userID}) получил награду за подписку!"; 
                    Effect Gift = new Effect("assets/prefabs/misc/xmas/presents/effects/unwrap.prefab", player, 0, new Vector3(), new Vector3()); 
                    if (Result == "1") 
                    { 
                        if (config.GrGifts.VKGroupGiftCMD == "none") 
                        { 
                            if ((24 - player.inventory.containerMain.itemList.Count) >= statdata.Gifts.Count) 
                            { 
                                usersdata.VKUsersData[player.userID].GiftRecived = true; 
                                VKBData.WriteObject(usersdata); 
                                PrintToChat(player, string.Format(GetMsg("НаградаПолучена"))); 
                                
                                if (config.GrGifts.GiftsBool) 
                                    Server.Broadcast(string.Format(GetMsg("ПолучилНаграду"), player.displayName, config.GrGifts.VKGroupUrl)); 
                                
                                foreach (GiftItem gf in statdata.Gifts) 
                                { 
                                    Item gift = ItemManager.CreateByName(gf.shortname, gf.count, gf.skinid); 
                                    gift.MoveToContainer(player.inventory.containerMain, -1, false); 
                                } 
                                EffectNetwork.Send(Gift, player.Connection); 
                            } 
                            else 
                                PrintToChat(player, string.Format(GetMsg("НетМеста"))); 
                        } 
                        else 
                        { 
                            string cmd = config.GrGifts.VKGroupGiftCMD.Replace("{steamid}", player.userID.ToString()); 
                            rust.RunServerCommand(cmd); 
                            usersdata.VKUsersData[player.userID].GiftRecived = true; 
                            VKBData.WriteObject(usersdata); 
                            PrintToChat(player, string.Format(GetMsg("НаградаПолученаКоманда"), config.GrGifts.GiftCMDdesc)); 
                            
                            if (config.GrGifts.GiftsBool) 
                                Server.Broadcast(string.Format(GetMsg("ПолучилНаграду"), player.displayName, config.GrGifts.VKGroupUrl)); 
                            
                            EffectNetwork.Send(Gift, player.Connection); 
                        } 
                    } 
                    else 
                        PrintToChat(player, string.Format(GetMsg("НеВступилВГруппу"), config.GrGifts.VKGroupUrl)); 
                } 
                    
                private void GiftNotifier() 
                { 
                    if (config.GrGifts.VKGroupGifts) 
                    { 
                        foreach (var pl in BasePlayer.activePlayerList) 
                        { 
                            if (!usersdata.VKUsersData.ContainsKey(pl.userID)) 
                                PrintToChat(pl, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl)); 
                            else 
                            { 
                                if (!usersdata.VKUsersData[pl.userID].GiftRecived) PrintToChat(pl, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl)); 
                            } 
                        } 
                    } 
                } 
                void WipeFunctions()
                {
                    if (config.WipeStg.WPostMsgAdmin)
                    {
                        if (config.WipeStg.WPostB)
                    {
                    if (config.WipeStg.WPostAttB)
                    SendVkWall($"{config.WipeStg.WPostMsg}&attachments={config.WipeStg.WPostAtt}");
                    else 

                    SendVkWall($"{config.WipeStg.WPostMsg}"); 
                } 
                if (config.GrGifts.GiftsWipe) 
                { 
                    if (usersdata.VKUsersData.Count != 0) 
                    { 
                        for (int i = 0; i < usersdata.VKUsersData.Count; i++) 
                        { 
                            usersdata.VKUsersData.ElementAt(i).Value.GiftRecived = false; 
                        } 
                        VKBData.WriteObject(usersdata); 
                    } 
                } 
                if (config.WipeStg.WMsgPlayers) WipeAlertsSend(); 
                if (config.WipeStg.GrNameChange) 
                { 
                    string wipedate = WipeDate(); 
                    string text = config.WipeStg.GrName.Replace("{wipedate}", wipedate); 
                    webrequest.Enqueue("https://api.vk.com/method/groups.edit?group_id=" + config.VKAPIT.GroupID + "&title=" + text + "&" + apiver + "&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => { 
                        var json = JObject.Parse(response); 
                        string Result = (string)json["response"]; 
                        if (Result == "1") 
                            PrintWarning($"Новое имя группы - {text}"); 
                        else 
                        { 
                            PrintWarning("Ошибка смены имени группы. Логи - /oxide/logs/TPBotbonus/"); 
                            Log("Errors", $"group title not changed. Error: {response}"); 
                        } 
                    }, this); 
                } 
            }
        } 

        private void WipeAlertsSend() 
        { 
                List<string> UserList = new List<string>(); 
                string userlist = ""; 
                int usercount = 0; 
                if (usersdata.VKUsersData.Count != 0) 
                { 
                    for (int i = 0; i < usersdata.VKUsersData.Count; i++) 
                    { 
                        if (config.WipeStg.WCMDIgnore || usersdata.VKUsersData.ElementAt(i).Value.WipeMsg) 
                        { 
                            if (!ServerUsers.BanListString().Contains(usersdata.VKUsersData.ElementAt(i).Value.UserID.ToString())) 
                            { 
                                if (usercount == 100) 
                                { 
                                    UserList.Add(userlist); userlist = ""; 
                                    usercount = 0; 
                                } 
                                if (usercount > 0) 
                                    userlist = userlist + ", "; 
                                userlist = userlist + usersdata.VKUsersData.ElementAt(i).Value.VkID; 
                                usercount++;
                            } 
                        }
                    } 
                } 
                if (userlist == "" && UserList.Count == 0) 
                { 
                    PrintWarning($"Список адресатов рассылки о вайпе пуст."); 
                    return; 
                } 
                if (UserList.Count > 0) 
                { 
                    foreach (var list in UserList) 
                        SendVkMessage(list, config.WipeStg.WMsgText); 
                } 

                SendVkMessage(userlist, config.WipeStg.WMsgText); 
            } 

                private void SendConfCode(string reciverID, string msg, BasePlayer player) => webrequest.Enqueue("https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + msg + "&" + apiver + "&random_id=" + RandomId() + "&access_token=" + config.VKAPIT.VKToken, null, (code, response) => GetCallback(code, response, "Код подтверждения", player), this); 
                
                private void SendVkMessage(string reciverID, string msg) => webrequest.Enqueue("https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + URLEncode(msg) + "&"+apiver + "&random_id=" + RandomId() + "&access_token=" + config.VKAPIT.VKToken, null, (code, response) => GetCallback(code, response, "Сообщение"), this); 
                
                private void SendVkWall(string msg) => webrequest.Enqueue("https://api.vk.com/method/wall.post?owner_id=-" + config.VKAPIT.GroupID + "&message=" + URLEncode(msg) + "&from_group=1&"+apiver+"&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => GetCallback(code, response, "Пост"), this); 
                
                private void SendVkStatus(string msg) => webrequest.Enqueue("https://api.vk.com/method/status.set?group_id=" + config.VKAPIT.GroupID + "&text=" + URLEncode(msg) + "&" + apiver + "&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => GetCallback(code, response, "Статус"), this); 
                
                private void AddComentToBoard(string topicid, string msg) => webrequest.Enqueue("https://api.vk.com/method/board.createComment?group_id=" + config.VKAPIT.GroupID + "&topic_id=" + URLEncode(topicid) + "&from_group=1&message=" + msg + "&"+apiver+"&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => GetCallback(code, response, "Комментарий в обсуждения"), this); 
                
                private string RandomId() => random.Next(Int32.MinValue, Int32.MaxValue).ToString(); 
                string GetUserVKId(ulong userid) 
                { 
                    if (!usersdata.VKUsersData.ContainsKey(userid) || !usersdata.VKUsersData[userid].Confirmed) return null; 
                    if (BannedUsers.Contains(userid.ToString())) return null; 
                    return usersdata.VKUsersData[userid].VkID; 
                } 
                string GetUserLastNotice(ulong userid) 
                { 
                    if (!usersdata.VKUsersData.ContainsKey(userid) || !usersdata.VKUsersData[userid].Confirmed) return null; 
                    return usersdata.VKUsersData[userid].LastRaidNotice; 
                } 

                private void VKAPISaveLastNotice(ulong userid, string lasttime) 
                { 
                    if (usersdata.VKUsersData.ContainsKey(userid)) 
                    { 
                        usersdata.VKUsersData[userid].LastRaidNotice = lasttime; 
                        VKBData.WriteObject(usersdata); 
                    } 
                } 
                
                private void VKAPIWall(string text, string attachments, bool atimg) 
                { 
                    if (atimg) 
                    { 
                        SendVkWall($"{text}&attachments={attachments}"); 
                        Log("tpbotbonusapi", $"Отправлен новый пост на стену: ({text}&attachments={attachments})"); 
                    } 
                    else 
                    { 
                        SendVkWall($"{text}"); Log("tpbotbonusapi", $"Отправлен новый пост на стену: ({text})"); 
                    } 
                } 
                
                private void VKAPIMsg(string text, string attachments, string reciverID, bool atimg) 
                { 
                    if (atimg) 
                    { 
                        SendVkMessage(reciverID, $"{text}&attachment={attachments}"); 
                        Log("tpbotbonusapi", $"Отправлено новое сообщение пользователю {reciverID}: ({text}&attachments={attachments})"); 
                    } 
                    else 
                    { 
                        SendVkMessage(reciverID, $"{text}"); Log("tpbotbonusapi", $"Отправлено новое сообщение пользователю {reciverID}: ({text})"); 
                    } 
                } 
                
                private void VKAPIStatus(string msg) 
                { 
                    StatusCheck(msg); 
                    SendVkStatus(msg); 
                    Log("tpbotbonusapi", $"Отправлен новый статус: {msg}"); 
                } 
                
                void Log(string filename, string text) => LogToFile(filename, $"[{DateTime.Now}] {text}", this); 
                
                void GetCallback(int code, string response, string type, BasePlayer player = null) 
                { 
                    if (!response.Contains("error")) 
                    { 
                        Puts($"{type} отправлен(о): {response}"); 
                        if (type == "Код подтверждения" && player != null) 
                            StartCodeSendedGUI(player); 
                        PrintToChat(player, string.Format(GetMsg("СообщениеОтправлено"))); 
                    } 
                    else 
                    { 
                        if (type == "Код подтверждения") 
                        { 
                            if (response.Contains("Can't send messages for users without permission") && player != null) 
                            { 
                                StartTPBotbonusHelpVKGUI(player); 
                                PrintToChat(player, string.Format(GetMsg("СообщениеНеОтправлено"))); 
                            } 
                            else 
                                Log("errorconfcode", $"Ошибка отправки кода подтверждения. Ответ сервера ВК: {response}"); 
                        } 
                        else 
                        { 
                            PrintWarning($"{type} не отправлен(о). Файлы лога: /oxide/logs/TPBotbonus/"); 
                            Log("Errors", $"{type} не отправлен(о). Ошибка: " + response); 
                        } 
                    } 
                } 
                
                private string EmojiCounters(string counter) 
                { 
                    var chars = counter.ToCharArray(); 
                    List<object> digits = new List<object>() 
                    { 
                        "0", 
                        "1", 
                        "2", 
                        "3", 
                        "4", 
                        "5", 
                        "6", 
                        "7", 
                        "8", 
                        "9" 
                    }; 

                    string emoji = ""; 
                    
                    for (int ctr = 0; ctr < chars.Length; ctr++) 
                    { 
                        if (digits.Contains(chars[ctr].ToString())) 
                        { 
                            string replace = chars[ctr] + "⃣"; 
                            emoji = emoji + replace; 
                        } 
                        else 
                            emoji = emoji + chars[ctr]; 
                    } 

                    return emoji; 
                } 
                
                private string WipeDate() => SaveRestore.SaveCreatedTime.ToLocalTime().ToString("dd.MM"); 
                
                private string URLEncode(string input) 
                { 
                    if (input.Contains("#")) 
                        input = input.Replace("#", "%23"); 
                    if (input.Contains("$")) 
                        input = input.Replace("$", "%24"); 
                    if (input.Contains("+")) 
                        input = input.Replace("+", "%2B"); 
                    if (input.Contains("/")) 
                        input = input.Replace("/", "%2F"); 
                    if (input.Contains(":")) 
                        input = input.Replace(":", "%3A"); 
                    if (input.Contains(";")) 
                        input = input.Replace(";", "%3B"); 
                    if (input.Contains("?")) 
                        input = input.Replace("?", "%3F"); 
                    if (input.Contains("@")) 
                        input = input.Replace("@", "%40"); 
                    return input; 
                } 
                
                private void StatusCheck(string msg) 
                { 
                    if (msg.Length > 140) 
                        PrintWarning($"Текст статуса слишком длинный. Измените формат статуса чтобы текст отобразился полностью. Лимит символов в статусе - 140. Длина текста - {msg.Length.ToString()}"); 
                } 
                
                private bool IsNPC(BasePlayer player) 
                { 
                    if (player is NPCPlayer) return true; 
                    if (!(player.userID >= 76560000000000000L || player.userID <= 0L)) return true; 
                    return false; 
                } 
                
                private static string GetColor(string hex) 
                { 
                    if (string.IsNullOrEmpty(hex)) 
                        hex = "#FFFFFFFF"; var str = hex.Trim('#'); 
                    if (str.Length == 6) 
                        str += "FF"; 
                    if (str.Length != 8) 
                    { 
                        throw new Exception(hex); 
                        throw new InvalidOperationException("Cannot convert a wrong format."); 
                    } 
                    var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber); 
                    var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber); 
                    var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber); 
                    var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber); 
                    Color color = new Color32(r, g, b, a); 
                    return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}"; 
                } 
                
                private void DeleteOldUsers(string days = null) 
                { 
                    int ddays = 30; 
                    int t; 
                    if (days != null && Int32.TryParse(days, out t)) 
                        ddays = t; 
                    int deleted = 0; 
                    List<ulong> ForDelete = new List<ulong>(); 
                        
                    foreach (var user in usersdata.VKUsersData) 
                    { 
                        if (user.Value.LastSeen == null) 
                            ForDelete.Add(user.Key); 
                        else 
                        { 
                            DateTime LNT; 
                            if (DateTime.TryParseExact(user.Value.LastSeen, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LNT) && DateTime.Now.Subtract(LNT).Days >= ddays)        ForDelete.Add(user.Key); 
                        } 
                    } 
                    foreach (var d in ForDelete) 
                    { 
                        usersdata.VKUsersData.Remove(d); deleted++;
                    }
                    if (deleted > 0) 
                    { 
                        PrintWarning($"Удалено устаревших профилей игроков из базы TPBotbonus: {deleted}"); 
                        VKBData.WriteObject(usersdata); 
                    } 
                    else 
                        PrintWarning($"Нет профилей для удаления."); 
                } 
                    
                private void CheckOxideUpdate() 
                { 
                    string currentver = Manager.GetPlugin("RustCore").Version.ToString(); 
                    webrequest.Enqueue("https://umod.org/games/rust.json", null, (code, response) => 
                    { 
                        if (code == 200 || response != null) 
                        { 
                            var json = JObject.Parse(response); 
                            if (json == null) return; 
                            string latestver = (string)json["latest_release_version"]; 
                            if (latestver == null) return; 
                            if (latestver != currentver && !OxideUpdateSended) 
                            { 
                                OxideUpdateSended = true; 
                            } 
                        } 
                    }, this); 
                    timer.Once(3600f, () => { 
                        CheckOxideUpdate(); 
                    }); 
                } 
                
                private string BannedUsers = ServerUsers.BanListString(); 
                
                private CuiElement BPanel(string name, string color, string anMin, string anMax, string parent = "Hud", bool cursor = false, float fade = 1f) 
                { 
                    var Element = new CuiElement() 
                    { 
                        Name = name, 
                        Parent = parent, 
                        Components = { 
                            new CuiImageComponent { Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = fade, Color = color }, 
                            new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                        }
                    }; 
                
                    if (cursor) 
                        Element.Components.Add(new CuiNeedsCursorComponent()); 
                    
                    return Element; 
                } 
                    
                private CuiElement Panel(string name, string color, string anMin, string anMax, string parent = "Hud", bool cursor = false, float fade = 1f) 
                { 
                    var Element = new CuiElement() 
                    { 
                        Name = name, 
                        Parent = parent, 
                        Components = { 
                            new CuiImageComponent { FadeIn = fade, Color = color }, 
                            new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax } 
                        } 
                    }; 
                    
                    if (cursor) 
                        Element.Components.Add(new CuiNeedsCursorComponent()); 
                    
                    return Element; 
                } 
                
                private CuiElement Text(string parent, string color, string text, TextAnchor pos, int fsize, string anMin = "0 0", string anMax = "1 1", string fname = "robotocondensed-bold.ttf", float fade = 3f) 
                { 
                    var Element = new CuiElement() 
                    { 
                        Parent = parent, 
                        Components = { 
                            new CuiTextComponent() { Color = color, Text = text, Align = pos, Font = fname, FontSize = fsize, FadeIn = fade }, 
                            new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax } 
                        } 
                    }; 
                    
                    return Element; 
                } 
                
                private CuiElement Button(string name, string parent, string command, string color, string anMin, string anMax, float fade = 3f) 
                { 
                    var Element = new CuiElement() 
                    { 
                        Name = name, 
                        Parent = parent, 
                        Components = { 
                            new CuiButtonComponent { Command = command, Color = color, FadeIn = fade}, 
                            new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax } 
                        } 
                    }; 
                    
                    return Element; 
                } 
                
                private CuiElement Image(string parent, string url, string anMin, string anMax, float fade = 3f, string color = "1 1 1 1") 
                { 
                    var Element = new CuiElement 
                    { 
                        Parent = parent, 
                        Components = { 
                            new CuiRawImageComponent { Color = color, Url = url, FadeIn = fade}, 
                            new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax } 
                        } 
                    }; 
                    
                    return Element; 
                } 
                
                private CuiElement Input(string name, string parent, int fsize, string command, string anMin = "0 0", string anMax = "1 1", TextAnchor pos = TextAnchor.MiddleCenter, int chlimit = 300, bool psvd = false, float fade = 3f) 
                { 
                    string text = ""; 
                    var Element = new CuiElement 
                    { 
                        Name = name, 
                        Parent = parent, 
                        Components = { 
                            new CuiInputFieldComponent { Align = pos, CharsLimit = chlimit, FontSize = fsize, Command = command + text, IsPassword = psvd, Text = text }, 
                            new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax } 
                        } 
                    }; 
                    
                    return Element; 
                } 
                
                private void UnloadAllGUI() 
                { 
                    foreach (var player in BasePlayer.activePlayerList) 
                    { 
                        CuiHelper.DestroyUi(player, VkWait); 
                        CuiHelper.DestroyUi(player, VkHelp); 
                        CuiHelper.DestroyUi(player, VkConnect); 
                        CuiHelper.DestroyUi(player, MainLayer); 
                        CuiHelper.DestroyUi(player, VkReward); 
                        CuiHelper.DestroyUi(player, VkAlert); 
                    } 
                } 
                
                private string UserName(string name) 
                { 
                    if (name.Length > 15) 
                        name = name.Remove(12) + "..."; 
                    
                    return name; 
                } 
                
                private void StartTPBotbonusAddVKGUI(BasePlayer player) 
                { 
                    CuiHelper.DestroyUi(player, VkConnect);
                    var container = new CuiElementContainer(); 
                    
                    container.Add(new CuiPanel() 
                    { 
                        CursorEnabled = true, 
                        RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.65 0.62", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" }
                    }, MainLayer, VkConnect); 

                    container.Add(new CuiElement
                    {
                        Parent = VkConnect,
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "alertvkback") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.93 0.81", AnchorMax = "1 1" },
                        Button = { Close = VkConnect, Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, VkConnect);

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.93 1" }, 
                        Text = { Text = "     Привязка страницы вк", FontSize = 12, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft } 
                    }, VkConnect); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.1 0.57", AnchorMax = "0.9 0.9", OffsetMin = "0 0", OffsetMax = "1 1" }, 
                        Text = { Text = "Укажите ссылку на страницу ВК\nв поле ниже и нажмите ENTER", Color = "1 1 1 0.6", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkConnect); 
                    
                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.53", AnchorMax = "1 0.63", OffsetMin = "0 0", OffsetMax = "1 1" }, 
                        Text = { Text = "пример vk.com/nickname", Color = "1 1 1 0.3", FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkConnect); 

                    container.Add(new CuiElement 
                    { 
                        Parent = VkConnect, 
                        Name = "Input", 
                        Components = { 
                            new CuiImageComponent { Color = "0 0 0 0" }, 
                            new CuiRectTransformComponent{ AnchorMin = "0.21 0.35", AnchorMax = "0.79 0.48", OffsetMin = "0 0", OffsetMax = "1 1" }, 
                            new CuiOutlineComponent { Color = "1 1 1 0.4", UseGraphicAlpha = true } 
                        } 
                    }); 
                    
                    container.Add(new CuiElement 
                    { 
                        Parent = "Input", 
                        Components = { 
                            new CuiInputFieldComponent { Text = "", FontSize = 12, Command = "vk.menugui46570981 addvkgui.addvk", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.4", CharsLimit = 34}, 
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "1 1" }, 
                            new CuiOutlineComponent { Color = "0.4 0.4 0.4 0.8", Distance = "0.4 -0.4", UseGraphicAlpha = true } 
                        } 
                    }); 

                    CuiHelper.AddUi(player, container); 
                } 
                
                public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin); 
                public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin); 
                public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName); 
                public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId); 
                void OnPlayerConnected(BasePlayer player) 
                { 
                    SteamAvatarAdd(player.UserIDString); 
                } 
                void SteamAvatarAdd(string userid) 
                { 
                    if (ImageLibrary == null) return; 
                    if (HasImage(userid)) return; 
                    string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=AE4C104E68AB334B06F065DCD2B03014&" + "steamids=" + userid; 
                    webrequest.Enqueue(url, null, (code, response) => { 
                        if (code == 200) 
                        { 
                            string Avatar = (string)JObject.Parse(response)["response"]?["players"]?[0]?["avatarfull"]; 
                            AddImage(Avatar, userid); 
                        } 
                    }, this); 
                } 

                public string VkICO = "https://i.imgur.com/etwraYj.png"; 
                public string GiftICO = "https://i.imgur.com/GS6VKn4.png"; 
                public string AlertICO = "https://i.imgur.com/Cyh2d7y.png"; 
                public string VkConnect = "VkConnect_UI"; 
                public string VkHelp = "VkHelp_UI"; 
                public string VkWait = "VkWait_UI"; 
                public string MainLayer = "lay" + ".Main"; 
                public string VkReward = "VkReward_UI"; 
                public string VkAlert = "VkAlert_UI"; 
                public string AlertPermission = "TPBotbonus.Alert"; 
                
                [ChatCommand("vk")]
                void StartTPBotbonusMainGUI(BasePlayer player, ulong target = 0) 
                { 
                    string addvkbuttoncommand = "vk.menugui46570981 maingui.addvk"; 
                    string addvkbuttongift = "vk.menugui46570981 giftopen"; 
                    string addvkbuttonalert = "vk.menugui46570981 alert"; 
                    string addvkbuutontext = "ОТКРЫТЬ"; 
                    string addvkbuutontextgift = "ОТКРЫТЬ"; 
                    string addvkbuutontextalert = "ОТКРЫТЬ"; 
                    string giftvkbuutontext = "Получить награду за\nвступление в группу ВК"; 
                    string giftvkbuttoncommand = "vk.menugui46570981 maingui.gift"; 
                    string addvkbuttonanmax = "0.99 0.5"; 
                    string imagevk = "";
                    string imagegift = "";
                    string imagealert = "";
                    ulong Person = player.userID;
                    string ImageAvatar = GetImage(Person.ToString()); 
                    var container = new CuiElementContainer(); 
                    
                    if (usersdata.VKUsersData.ContainsKey(player.userID)) 
                    { 
                        if (!usersdata.VKUsersData[player.userID].Confirmed) 
                        { 
                            addvkbuttoncommand = "vk.menugui46570981 maingui.confirm"; 
                        } 
                        else 
                        { 
                            addvkbuutontext = "ДОБАВЛЕНО"; 
                            addvkbuttoncommand = ""; 
                            imagevk = "ButtonBlock";
                            addvkbuttongift = "vk.menugui46570981 giftcomplete"; 
                        } 
                    } 
                    if (usersdata.VKUsersData.ContainsKey(player.userID)) 
                    { 
                        if (usersdata.VKUsersData[player.userID].GiftRecived) 
                        { 
                            addvkbuttongift = ""; 
                            addvkbuutontextgift = "ДОБАВЛЕНО"; 
                            imagegift = "ButtonBlock";
                        } 
                    } 
                    if (usersdata.VKUsersData.ContainsKey(player.userID)) 
                    { 
                        if (permission.UserHasPermission(player.UserIDString, AlertPermission) && usersdata.VKUsersData[player.userID].Confirmed) 
                        { 
                            addvkbuttonalert = ""; 
                            addvkbuutontextalert = "ДОБАВЛЕНО"; 
                            imagealert = "ButtonBlock";
                        } 
                    } 
                    
                                if (usersdata.VKUsersData.ContainsKey(player.userID)) 
                    { 
                        if (permission.UserHasPermission(player.UserIDString, AlertPermission) && usersdata.VKUsersData[player.userID].Confirmed) 
                        { 
                            addvkbuttonalert = ""; 
                            addvkbuutontextalert = "ДОБАВЛЕНО"; 
                            imagealert = "ButtonBlock";
                        } 
                    } 
                    
                    container.Add(new CuiElement
                    {
                        Name = MainLayer,
                        Parent = ".Mains",
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "BackgroundImage") },
                            new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                        Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, MainLayer);





                    container.Add(new CuiPanel() 
                    { 
                        RectTransform = {AnchorMin = "0.34 0.517", AnchorMax = "0.418 0.656", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" } 
                    }, MainLayer, "AVATAR"); 

                    container.Add(new CuiElement 
                    { 
                        Parent = "AVATAR", 
                        Components = { 
                            new CuiRawImageComponent { Png = ImageAvatar,Color = "1 1 1 1" }, 
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0"}, 
                        } 
                    });

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.33 0.48", AnchorMax = "0.428 0.515", OffsetMax = "0 0"}, 
                        Text = { Text = player.displayName, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.6"} 
                    }, MainLayer); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = {AnchorMin = "0.45 0.6", AnchorMax = "0.73 0.675", OffsetMax = "0 0"}, 
                        Text = { Text = "   Ссылка на нашу группу вк\n         https://vk.com/rustage_su", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = "1 1 1 0.6"} 
                    }, MainLayer); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = {AnchorMin = "0.448 0.555", AnchorMax = "0.67 0.59", OffsetMax = "0 0"}, 
                        Text = { Text = "            Для привязки используйте ваш вк", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.6"} 
                    }, MainLayer); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = {AnchorMin = "0.448 0.51", AnchorMax = "0.67 0.545", OffsetMax = "0 0"}, 
                        Text = { Text = "            Вступите в группу вк и получайте награды", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.6"} 
                    }, MainLayer); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = {AnchorMin = "0.448 0.465", AnchorMax = "0.67 0.5", OffsetMax = "0 0"}, 
                        Text = { Text = "            Подключить оповещение о рейде", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.6"} 
                    }, MainLayer); 

                    container.Add(new CuiPanel() 
                    { 
                        RectTransform = {AnchorMin = "0.675 0.553", AnchorMax = "0.73 0.59", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" } 
                    }, MainLayer, "Vk"); 

                    if (imagevk != "")
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "Vk",
                            Components = 
                            {
                                new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", imagevk) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            }
                        });
                    }

                    container.Add(new CuiButton 
                    { 
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }, 
                        Button = { Command = addvkbuttoncommand, Color = "0 0 0 0" }, 
                        Text = { Text = addvkbuutontext, FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } 
                    }, "Vk"); 

                    container.Add(new CuiPanel() 
                    { 
                        RectTransform = {AnchorMin = "0.675 0.51", AnchorMax = "0.73 0.547", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" } 
                    }, MainLayer, "gift"); 

                    if (imagegift != "")
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "gift",
                            Components = 
                            {
                                new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", imagegift) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            }
                        });
                    }

                    container.Add(new CuiButton 
                    { 
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }, 
                        Button = { Color = "0 0 0 0", Command = addvkbuttongift }, 
                        Text = { Text = addvkbuutontextgift, FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } 
                    }, "gift"); 

                    container.Add(new CuiPanel() 
                    { 
                        RectTransform = {AnchorMin = "0.675 0.465", AnchorMax = "0.73 0.503", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" } 
                    }, MainLayer, "alert"); 

                    if (imagealert != "")
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "alert",
                            Components = 
                            {
                                new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", imagealert) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            }
                        });
                    }

                    container.Add(new CuiButton 
                    { 
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }, 
                        Button = { Color = "0 0 0 0", Command = addvkbuttonalert }, 
                        Text = { Text = addvkbuutontextalert, FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } 
                    }, "alert"); 

                    string helpText = "Если у вас возникают проблемы с меню, вы можете использовать чатовые команды:\n<b>/regvk add</b> - привязка вашего профиля ВК\n<b>/regvk confirm</b> - подтверждение вашего профиля ВК\n<b>/regvk gift</b> - получение подарка за подписку ВК"; 
                    
                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.45 0.35", AnchorMax = "0.73 0.46", OffsetMax = "0 0" }, 
                        Text = { Text = helpText, Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "0.67 0.63 0.596"} 
                    }, MainLayer); 

                    CuiHelper.DestroyUi(player, MainLayer); 
                    CuiHelper.AddUi(player, container); 
                } 
                
                private void StartTPBotbonusHelpVKGUI(BasePlayer player) 
                {
                    CuiHelper.DestroyUi(player, VkConnect);
                    CuiHelper.DestroyUi(player, VkHelp); 
                    CuiElementContainer container = new CuiElementContainer(); 
                    container.Add(new CuiPanel() 
                    { 
                        CursorEnabled = true, 
                        RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.65 0.62", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" } 
                    }, MainLayer, VkHelp); 
                    
                    container.Add(new CuiElement
                    {
                        Parent = VkHelp,
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "alerts1") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.93 0.81", AnchorMax = "1 1" },
                        Button = { Close = VkHelp, Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, VkHelp);

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.93 1" }, 
                        Text = { Text = "     Привязка страницы вк", FontSize = 12, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft } 
                    }, VkHelp); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.9" }, 
                        Text = { Text = "Бот не может отправить вам сообщение :(\nОтправьте в сообщения группы любое слово и попробуйте снова", Color = "1 1 1 0.6", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkHelp); 
                    
                    CuiHelper.AddUi(player, container); 
                } 
                
                private void StartCodeSendedGUI(BasePlayer player) 
                { 
                    if (player == null || player.Connection == null) return; 
                    CuiHelper.DestroyUi(player, VkConnect); 
                    CuiHelper.DestroyUi(player, VkReward); 
                    CuiHelper.DestroyUi(player, VkHelp); 
                    CuiElementContainer container = new CuiElementContainer(); 

                    container.Add(new CuiPanel() 
                    { 
                        CursorEnabled = true, 
                        RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.65 0.62", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" }
                    }, MainLayer, VkWait); 

                    container.Add(new CuiElement
                    {
                        Parent = VkWait,
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "alerts") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.93 0.81", AnchorMax = "1 1" },
                        Button = { Command = "vk.refresh help", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, VkWait);

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.93 1" }, 
                        Text = { Text = "     Привязка страницы вк", FontSize = 12, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft } 
                    }, VkWait); 

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.1 0.48", AnchorMax = "0.9 0.9" }, 
                        Text = { Text = "На вашу страницу ВК отправлено сообщение с дальнейшими инструкциями", Color = "1 1 1 0.6", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkWait); 

                    container.Add(new CuiButton 
                    { 
                        RectTransform = { AnchorMin = "0.315 0.35", AnchorMax = "0.68 0.49", }, 
                        Button = { Command = "vk.menugui46570981 maingui.removevk8974321765", Color = "0 0 0 0" }, 
                        Text = { Text = "     Удалить профиль", FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkWait); 

                    CuiHelper.AddUi(player, container); 
                } 
                
                private void RewardTPBotbonusGUI(BasePlayer player) 
                { 
                    var container = new CuiElementContainer(); 

                    container.Add(new CuiPanel() 
                    { 
                        CursorEnabled = true, 
                        RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.65 0.62", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" }
                    }, MainLayer, VkReward); 

                    container.Add(new CuiElement
                    {
                        Parent = VkReward,
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "alerts") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.93 0.81", AnchorMax = "1 1" },
                        Button = { Command = "vk.refresh gift", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, VkReward);

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.93 1" }, 
                        Text = { Text = "     Получение подарка", FontSize = 12, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft } 
                    }, VkReward); 
                    
                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.9" }, 
                        Text = { Text = "\nДля подключения оповещения о рейде зайдите в магазин и купите бесплатно", Color = "1 1 1 0.6", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkReward); 

                    CuiHelper.AddUi(player, container); 
                } 
                
                private void CompleteRewardTPBotbonusGUI(BasePlayer player) 
                { 
                    var container = new CuiElementContainer(); 

                    container.Add(new CuiPanel() 
                    { 
                        CursorEnabled = true, 
                        RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.65 0.62", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" }
                    }, MainLayer, VkReward); 

                    container.Add(new CuiElement
                    {
                        Parent = VkReward,
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "giftrewardback") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.93 0.81", AnchorMax = "1 1" },
                        Button = { Command = "vk.refresh gift", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, VkReward);

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.93 1" }, 
                        Text = { Text = "     Получение подарка", FontSize = 12, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft } 
                    }, VkReward); 
                    
                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.1 0.5", AnchorMax = "0.9 0.9" }, 
                        Text = { Text = "Для получения подарка, нажмите кнопку получить. Убедитесь что подписались на нашу группу", Color = "1 1 1 0.6", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkReward); 

                    container.Add(new CuiButton 
                    { 
                        RectTransform = { AnchorMin = "0.315 0.35", AnchorMax = "0.68 0.49", }, 
                        Button = { Command = "vk.menugui46570981 maingui.gift", Color = "0 0 0 0" }, 
                        Text = { Text = "     Получить", FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkReward); 
 
                    CuiHelper.AddUi(player, container); 
                } 
                
                private void AlertTPBotbonusGUI(BasePlayer player) 
                { 
                    var container = new CuiElementContainer(); 
                    
                    container.Add(new CuiPanel() 
                    { 
                        CursorEnabled = true, 
                        RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.65 0.62", OffsetMin = "0 0", OffsetMax = "0 0"}, 
                        Image = {Color = "0 0 0 0" }
                    }, MainLayer, VkAlert); 

                    container.Add(new CuiElement
                    {
                        Parent = VkAlert,
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "alerts") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.93 0.81", AnchorMax = "1 1" },
                        Button = { Command = "vk.refresh alert", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, VkAlert);

                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.93 1" }, 
                        Text = { Text = "     Уведомление о рейде", FontSize = 12, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft } 
                    }, VkAlert); 
                    
                    container.Add(new CuiLabel 
                    { 
                        RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.9" }, 
                        Text = { Text = "\nДля подключения оповещения о рейде зайдите в магазин и купите бесплатно", Color = "1 1 1 0.6", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter } 
                    }, VkAlert); 
                        
                    CuiHelper.AddUi(player, container); 
                } 
                    
                    [ConsoleCommand("vk.menugui46570981")] 
                    private void CmdChoose(ConsoleSystem.Arg arg) 
                    { 
                        BasePlayer player = arg.Player(); 
                        if (player == null) return; 
                        if (arg.Args == null) return; 
                        
                        switch (arg.Args[0]) 
                        { 
                            case "maingui.close": 
                                CuiHelper.DestroyUi(player, "MainUI"); 
                                break; 
                            case "maingui.addvk": 
                                CuiHelper.DestroyUi(player, "MainUI"); 
                                StartTPBotbonusAddVKGUI(player); 
                                break; 
                            case "maingui.removevk8974321765": 
                                CuiHelper.DestroyUi(player, VkWait); 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                CuiHelper.DestroyUi(player, VkConnect); 
                                CuiHelper.DestroyUi(player, MainLayer); 
                                if (usersdata.VKUsersData.ContainsKey(player.userID)) 
                                { 
                                    usersdata.VKUsersData.Remove(player.userID); VKBData.WriteObject(usersdata); 
                                } 
                                StartTPBotbonusMainGUI(player); 
                                break; 
                            case "maingui.walert": 
                                WAlert(player); 
                                break; 
                            case "maingui.gift": 
                                CuiHelper.DestroyUi(player, VkWait); 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                CuiHelper.DestroyUi(player, VkConnect); 
                                CuiHelper.DestroyUi(player, MainLayer); 
                                CuiHelper.DestroyUi(player, VkReward); 
                                CuiHelper.DestroyUi(player, "Menu_UI"); 
                                FixedGifts(player); 
                                break; 
                            case "maingui.confirm": 
                                CuiHelper.DestroyUi(player, "MainUI"); 
                                SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /regvk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player); 
                                break; 
                            case "maingui.wait": 
                                CuiHelper.DestroyUi(player, "MainUI"); 
                                StartCodeSendedGUI(player); 
                                break;
                            case "addvkgui.close": 
                                CuiHelper.DestroyUi(player, "AddVKUI"); 
                                break; 
                            case "addvkgui.addvk": 
                                string url = string.Join(" ", arg.Args.Skip(1).ToArray()); 
                                if (!url.Contains("vk.com/")) 
                                { 
                                    PrintToChat(player, string.Format(GetMsg("НеправильнаяСсылка"))); 
                                    return; 
                                } 
                                CuiHelper.DestroyUi(player, "AddVKUI"); 
                                CheckVkUser(player, url); 
                                break; 
                            case "helpgui.close": 
                                CuiHelper.DestroyUi(player, "HelpUI"); 
                                break; 
                            case "helpgui.confirm": 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /regvk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player); 
                                break; 
                            case "csendui.close": 
                                CuiHelper.DestroyUi(player, "CodeSendedUI"); 
                                break; 
                            case "giftopen": 
                                RewardTPBotbonusGUI(player); 
                                break; 
                            case "giftcomplete": 
                                CompleteRewardTPBotbonusGUI(player); 
                                break; 
                            case "alert": 
                                AlertTPBotbonusGUI(player); 
                                break; 
                        } 
                    } 
                    
                    [ConsoleCommand("vk.refresh")] 
                    private void CmdRefresh(ConsoleSystem.Arg arg) 
                    { 
                        BasePlayer player = arg.Player(); 
                        if (player == null) return; 
                        if (arg.Args == null) return; 
                        
                        switch (arg.Args[0]) 
                        { 
                            case "connect": 
                                CuiHelper.DestroyUi(player, VkWait); 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                CuiHelper.DestroyUi(player, VkReward); 
                                CuiHelper.DestroyUi(player, VkAlert); 
                                StartTPBotbonusMainGUI(player); 
                                break; 
                            case "help": 
                                CuiHelper.DestroyUi(player, VkWait); 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                CuiHelper.DestroyUi(player, VkConnect); 
                                CuiHelper.DestroyUi(player, MainLayer); 
                                CuiHelper.DestroyUi(player, VkReward); 
                                CuiHelper.DestroyUi(player, VkAlert); 
                                StartTPBotbonusMainGUI(player); 
                                break; 
                            case "gift": 
                                CuiHelper.DestroyUi(player, VkWait); 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                CuiHelper.DestroyUi(player, VkConnect); 
                                CuiHelper.DestroyUi(player, MainLayer); 
                                CuiHelper.DestroyUi(player, VkReward); 
                                CuiHelper.DestroyUi(player, VkAlert); 
                                StartTPBotbonusMainGUI(player); 
                                break; 
                            case "alert": 
                                CuiHelper.DestroyUi(player, VkWait); 
                                CuiHelper.DestroyUi(player, VkHelp); 
                                CuiHelper.DestroyUi(player, VkConnect); 
                                CuiHelper.DestroyUi(player, MainLayer); 
                                CuiHelper.DestroyUi(player, VkReward); 
                                CuiHelper.DestroyUi(player, VkAlert); 
                                StartTPBotbonusMainGUI(player); 
                                break; 
                        } 
                    } 
                    
                    private void FixedGifts(BasePlayer player) 
                    { 
                        if (GiftsList.ContainsKey(player)) 
                        { 
                            TimeSpan interval = DateTime.Now - GiftsList[player]; 
                            if (interval.TotalSeconds < 15) 
                            { 
                                PrintToChat(player, "Вы отправляете запрос слишком часто, попробуйте немного позже"); 
                                return; 
                            } 
                            else 
                            { 
                                GiftsList[player] = DateTime.Now; VKGift(player); 
                            } 
                        } 
                        else 
                        { 
                            GiftsList.Add(player, DateTime.Now); VKGift(player); 
                        } 
                    }
               
        private void LoadMessages() 
        { 
            lang.RegisterMessages(new Dictionary<string, string> 
            { 
                {"ПоздравлениеИгрока", "Администрация сервера поздравляет вас с Днем Рождения!"}, 
                {"ДеньРожденияИгрока", "Администрация сервера поздравляет игрока <color=#81BEF7>{0}</color> с Днем Рождения!"}, 
                {"РепортОтправлен", "Ваше сообщение было отправлено администратору"}, 
                {"КомандаРепорт", "Используйте:\n<color=#81BEF7>/report</color> сообщение"}, 
                {"ФункцияОтключена", "Данная функция отключена администратором"}, 
                {"ПрофильДобавленИПодтвержден", "Вы уже добавили и подтвердили свой профиль"}, 
                {"ПрофильДобавлен", "Вы уже добавили свой профиль. Если вам не пришел код подтверждения, введите команду <color=#81BEF7>/regvk confirm</color>"}, 
                {"ДоступныеКоманды", "<color=#F5DA81>ДОСТУПНЫЕ КОМАНДЫ:</color>\n/vk - открыть меню функций\n/regvk add - привязка вашего профиля ВК\n/regvk confirm - подтверждение вашего профиля ВК\n/regvk gift - получение подарка за подписку ВК"}, 
                {"НеправильнаяСсылка", "Ссылка на страницу должна быть вида \"vk.com/nickname\""}, 
                {"Подсказка", "Используйте:\n<color=#cfc580>/regvk add</color> ваша_ссылка\nСсылка на страницу должна быть вида \"vk.com/nickname\""}, 
                {"ПрофильПодтвержден", "Вы подтвердили свой профиль!"}, 
                {"ОповещениеОПодарках", "Вы можете получить награду, если вступили в нашу группу <color=#81BEF7>{0}</color>"}, 
                {"НеверныйКод", "Неверный код подтверждения"}, 
                {"ПрофильНеДобавлен", "Сначала добавьте и подтвердите свой профиль"}, 
                {"КодОтправлен", "Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color=#81BEF7>{0}</color> и напишите любое сообщение"}, {"ПрофильНеПодтвержден", "Сначала подтвердите свой профиль ВК"}, 
                {"НаградаУжеПолучена", "Вы уже получили свою награду!"}, 
                {"ПодпискаОтключена", "Вы <color=#ffd700>отключили</color> подписку на сообщения о вайпах сервера"}, 
                {"ПодпискаВключена", "Вы <color=#ffd700>включили</color> подписку на сообщения о вайпах сервера"}, 
                {"НаградаПолучена", "Вы получили свою награду!"}, 
                {"ПолучилНаграду", "Игрок <color=#81BEF7>{0}</color> получил награду за вступление в группу сервера!\n<size=12>Подробнее: <color=#ffd700>/menu</color></size>"}, {"НетМеста", "Недостаточно места для получения награды"}, 
                {"НаградаПолученаКоманда", "За вступление в группу нашего сервера вы получили {0}"}, 
                {"НеВступилВГруппу", "Вы не являетесь участником нашей группы!"},
                {"ОтветНаРепортЧат", "<color=#81BEF7>Администратор</color> ответил на ваше сообщение:\n"},
                {"ОтветНаРепортВК", "Администратор ответил на ваше сообщение:\n"},
                {"ИгрокНеНайден", "Игрок не найден"}, 
                {"СообщениеИгрокуТопПромо", "Поздравляем! Вы Топ {0} по результатам этого вайпа, в качестве награды, вы получаете промокод {1} на баланс в нашем магазине. {2}"}, 
                {"АвтоОповещенияОвайпе", "Сервер рассылает оповещения о вайпе всем, подписка не требуется"},
                {"СообщениеОтправлено", "На вашу страницу ВК отправлено сообщение с дальнейшими инструкциями"}, 
                {"СообщениеНеОтправлено", "Бот не может отправить вам сообщение :(\nОтправьте в сообщения группы любое слово и попробуйте снова"} 
            }, this); 
        } 
        string GetMsg(string key) => lang.GetMessage(key, this); 
    } 
}