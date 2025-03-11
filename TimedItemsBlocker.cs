using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TimedItemsBlocker", "Vlad-00003", "1.0.0")]
    [Description("Prevents some items from being used for a limited period of time.")]
    //Author info:
    //E-mail: Vlad-00003@mail.ru
    //Vk: vk.com/vlad_00003

    class TimedItemsBlocker : RustPlugin
    {

        #region Config setup
        //private Dictionary<BasePlayer, string> Panels = new Dictionary<BasePlayer, string>();
        private string PanelName = "BlockerUI";
        private Dictionary<string, int> BlockedItems = new Dictionary<string, int>();
        private Dictionary<string, int> BlockedClothes = new Dictionary<string, int>();
        private DateTime DateOfWipe;
        private bool UseChat = false;
        private bool Wipe = false;
        private string Prefix = "[Items Blocker]";
        private string PrefixColor = "#f44253";
        private string BypassPermission = "timeditemsblocker.bypass";

        #endregion

        #region Init

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created, Block Start now and will remain for 30 hours. You can change it into the config.");
        }

        private void LoadConfigValues()
        {
            Dictionary<string, object> blockedItems = new Dictionary<string, object>()
            {
                ["Satchel Charge"] = 30,
                ["Timed Explosive Charge"] = 30,
                ["Eoka Pistol"] = 30,
                ["Custom SMG"] = 30,
                ["Assault Rifle"] = 30,
                ["Bolt Action Rifle"] = 30,
                ["Waterpipe Shotgun"] = 30,
                ["Revolver"] = 30,
                ["Thompson"] = 30,
                ["Semi-Automatic Rifle"] = 30,
                ["Semi-Automatic Pistol"] = 30,
                ["Pump Shotgun"] = 30,
                ["M249"] = 30,
                ["Rocket Launcher"] = 30,
                ["Flame Thrower"] = 30,
                ["Double Barrel Shotgun"] = 30,
                ["Beancan Grenade"] = 30,
                ["F1 Grenade"] = 30,
                ["MP5A4"] = 30,
                ["LR-300 Assault Rifle"] = 30,
                ["M92 Pistol"] = 30,
                ["Python Revolver"] = 30
            };
            Dictionary<string, object> blockedClothes = new Dictionary<string, object>()
            {
                ["Metal Facemask"] = 30,
                ["Road Sign Kilt"] = 30,
                ["Road Sign Jacket"] = 30,
                ["Metal Chest Plate"] = 30,
                ["Heavy Plate Pants"] = 30,
                ["Heavy Plate Jacket"] = 30,
                ["Heavy Plate Helmet"] = 30,
                ["Riot Helmet"] = 30,
                ["Bucket Helmet"] = 30,
                ["Coffee Can Helmet"] = 30
            };
            DateOfWipe = DateTime.Now;
            string DateOfWipeStr = DateOfWipe.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            GetConfig("Wipe date", ref DateOfWipeStr);
            GetConfig("Bypass permission", ref BypassPermission);
            GetConfig("Wipe?(If set to true all timers would be automaticly set to current time + Hours of block", ref Wipe);
            GetConfig("Chat prefix", ref Prefix);
            GetConfig("Chat prefix color", ref PrefixColor);
            GetConfig("Use chat insted of GUI", ref UseChat);
            GetConfig("List of blocked items", ref blockedItems);
            GetConfig("List of blocked clothes", ref blockedClothes);
            SaveConfig();

            foreach (var item in blockedItems)
            {
                int hour;
                if(!int.TryParse(item.Value.ToString(), out hour))
                {
                    PrintWarning($"Item {item} has incorrect time format. Shoud be int");
                    continue;
                }
                BlockedItems.Add(item.Key, hour);
            }
            foreach(var item in blockedClothes)
            {
                int hour;
                if(!int.TryParse(item.Value.ToString(), out hour))
                {
                    PrintWarning($"Item {item} has incorrect time format. Shoud be int");
                    continue;
                }
                BlockedClothes.Add(item.Key, hour);
            }
            if (!DateTime.TryParseExact(DateOfWipeStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOfWipe))
            {
                DateOfWipe = DateTime.Now;
                PrintWarning($"Unable to parse Wipe date format, wipe date set to {DateOfWipe.ToString("dd.MM.yyyy HH:mm:ss")}");
            }
        }

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"ItemBlocked", "Using this item is blocked!"},
                {"BlockTimeLeft","{0}d {1:00}:{2:00}:{3:00} until unblock." },
                {"Weapon line 2", "You can only use Hunting bow and Crossbow" },
                {"Cloth line 2","You can only use wood and bone armor!" }
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"ItemBlocked", "Использование данного предмета заблокировано!"},
                {"BlockTimeLeft", "До окончания блокировки осталось {0}д. {1:00}:{2:00}:{3:00}" },
                {"Weapon line 2", "Вы можете использовать только Лук и Арбалет" },
                {"Cloth line 2","Используйте только деревянную и костяную броню!" }
            }, this, "ru");
        }

        void Loaded()
        {
            LoadConfigValues();
            LoadMessages();
            if (Wipe)
            {
                DateOfWipe = DateTime.Now;
                string DateOfWipeStr = DateOfWipe.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                Config["Wipe date"] = DateOfWipeStr;
                Config["Wipe?(If set to true all timers would be automaticly set to current time + Hours of block"] = false;
                SaveConfig();
            }
            permission.RegisterPermission(BypassPermission, this);
        }

        #endregion

        #region Equipcontrol

        object CanEquipItem(PlayerInventory inventory, Item item)
        {
            string name = null;
            if (BlockedItems.ContainsKey(item.info.displayName.english))
            {
                name = item.info.displayName.english;
            }
            if(BlockedItems.ContainsKey(item.info.shortname))
            {
                name = item.info.shortname;
            }
            if(name != null)
            {
                int BlockEnd = BlockedItems[name];
                if (InBlock(BlockEnd))
                {
                    TimeSpan timeleft = TimeLeft(BlockEnd);
                    var player = inventory.GetComponent<BasePlayer>();
                    if (permission.UserHasPermission(player.UserIDString, BypassPermission))
                        return null;
                    string reply = GetMsg("ItemBlocked", player.UserIDString) + "\n";
                    reply += string.Format(GetMsg("BlockTimeLeft", player.UserIDString), timeleft.Days, timeleft.Hours, timeleft.Minutes, timeleft.Seconds);
                    reply += "\n" + GetMsg("Weapon line 2", player.UserIDString);
                    if (UseChat)
                    {
                        SendToChat(player, reply);
                    }
                    else
                    {
                        BlockerUi(player, reply);
                    }                    
                    return false;
                }
            }
            return null;
        }

        object CanWearItem(PlayerInventory inventory, Item item)
        {
            string name = null;
            if (BlockedClothes.ContainsKey(item.info.displayName.english))
            {
                name = item.info.displayName.english;
            }
            if (BlockedClothes.ContainsKey(item.info.shortname))
            {
                name = item.info.shortname;
            }
            if (name != null)
            {
                int BlockEnd = BlockedClothes[name];
                if (InBlock(BlockEnd))
                {
                    TimeSpan timeleft = TimeLeft(BlockEnd);
                    var player = inventory.GetComponent<BasePlayer>();
                    if (permission.UserHasPermission(player.UserIDString, BypassPermission))
                        return null;
                    string reply = GetMsg("ItemBlocked", player.UserIDString) + "\n";
                    reply += string.Format(GetMsg("BlockTimeLeft", player.UserIDString), timeleft.Days, timeleft.Hours, timeleft.Minutes, timeleft.Seconds);
                    reply += "\n" + GetMsg("Cloth line 2", player.UserIDString);
                    if (UseChat)
                    {
                        SendToChat(player, reply);
                    }else
                    {
                        BlockerUi(player, reply);
                    }                    
                    return false;
                }
            }
            return null;
        }

        #endregion

        #region Blocker UI

        private void BlockerUi(BasePlayer player, string inputText)
        {
            CuiHelper.DestroyUi(player, PanelName);
            var elements = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = "0.1 0.1 0.1 0.5"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        CursorEnabled = true
                    },
                    new CuiElement().Parent = "Overlay", PanelName
                }
            };
            elements.Add(new CuiButton
            {
                Button =
                {
                    Close = PanelName,
                    Color = "0.8 0.8 0.8 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                Text =
                {
                    Text = "",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            }, PanelName);
            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = inputText,
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.443 0.867 0.941 1.0"
                },
                RectTransform =
                {
                    AnchorMin = "0.35 0.47",
                    AnchorMax = "0.65 0.57"
                }
            }, PanelName);
            CuiHelper.AddUi(player, elements);
            //timer.Once(1f, () =>
            //{
            //    BlockerUi(player, inputText);
            //});
        }

        #endregion

        #region Helpers
        private bool InBlock(int EndTime)
        {
            if (TimeLeft(EndTime).TotalSeconds >= 0)
            {
                return true;
            }
            return false;
        }
        TimeSpan TimeLeft(int hours) => DateOfWipe.AddHours(hours).Subtract(DateTime.Now);
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        string TimeToString (DateTime time) => time.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        //Функция, отправляющая сообщение в чат конкретному пользователю, добавляет префикс
        private void SendToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, "<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Перезгрузка функции отправки собщения в чат - отправляет сообщение всем пользователям
        private void SendToChat(string Message)
        {
            PrintToChat("<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
        //private void GetConfig<T>(ref T value, params object[] args)
        //{
        //    List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
        //    if (Config.Get(stringArgs.ToArray()) != null)
        //    {
        //        value = (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        //    }
        //    Config.Set(args, value);
        //}
        #endregion
    }
}
