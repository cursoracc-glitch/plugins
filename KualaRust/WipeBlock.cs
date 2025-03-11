using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Apex;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Color = UnityEngine.Color;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("WipeBlock", "Hougan", "2.0.9")]
    [Description("Блокировка предметов для вашего сервера!")]
    public class WipeBlock : RustPlugin
    {
        #region Classes

        private class Configuration
        {
            public class Interface
            {
                [JsonProperty("Сдвиг панели по вертикале (если некорректно отображается при текущих настройках)")]
                public int Margin = 0;
                [JsonProperty("Текст на первой строке")]
                public string FirstString = "БЛОКИРОВКА ПРЕДМЕТОВ";
                [JsonProperty("Текст на второй строке")]
                public string SecondString = "НАЖМИТЕ ЧТОБЫ УЗНАТЬ БОЛЬШЕ";
                [JsonProperty("Название сервера")]
                public string ServerName = "%CONFIG%";
            }

            public class Block
            {
                [JsonProperty("Сдвиг блокировки в секундах ('328' - на 328 секунд вперёд, '-328' на 328 секунд назад)")]
                public int TimeMove = 0;
                [JsonProperty("Настройки блокировки предметов")]
                public Dictionary<int, List<string>> BlockItems;
                [JsonProperty("Названия категорий в интерфейсе")]
                public Dictionary<string, string> CategoriesName;
            }

            [JsonProperty("Настройки интерфейса плагина")]
            public Interface SInterface;
            [JsonProperty("Настройки текущей блокировки")]
            public Block SBlock;

            public static Configuration GetDefaultConfiguration()
            {
                var newConfiguration = new Configuration();
                newConfiguration.SInterface = new Interface();
                newConfiguration.SBlock = new Block();
                newConfiguration.SBlock.CategoriesName = new Dictionary<string, string>
                {
                    ["Weapon"] = "ОРУЖИЯ",
                    ["Ammunition"] = "БОЕПРИПАСОВ",
                    ["Medical"] = "МЕДИЦИНЫ",
                    ["Food"] = "ЕДЫ",
                    ["Traps"] = "ЛОВУШЕК",
                    ["Tool"] = "ИНСТРУМЕНТОВ",
                    ["Construction"] = "КОНСТРУКЦИЙ",
                    ["Resources"] = "РЕСУРСОВ",
                    ["Items"] = "ПРЕДМЕТОВ",
                    ["Component"] = "КОМПОНЕНТОВ",
                    ["Misc"] = "ПРОЧЕГО",
                    ["Attire"] = "ОДЕЖДЫ"
                };
                newConfiguration.SBlock.BlockItems = new Dictionary<int, List<string>>
                {
                    [1800] = new List<string>
                    {
                        "pistol.revolver",
                        "shotgun.double",
                    },
                    [3600] = new List<string>
                    {
                        "flamethrower",
                        "bucket.helmet",
                        "riot.helmet",
                        "pants",
                        "hoodie",
                    },
                    [7200] = new List<string>
                    {
                        "pistol.python",
                        "pistol.semiauto",
                        "coffeecan.helmet",
                        "roadsign.jacket",
                        "roadsign.kilt",
                        "icepick.salvaged",
                        "axe.salvaged",
                        "hammer.salvaged",
                    },
                    [14400] = new List<string>
                    {
                        "shotgun.pump",
                        "shotgun.spas12",
                        "pistol.m92",
                        "smg.mp5",
                        "jackhammer",
                        "chainsaw",
                    },
                    [28800] = new List<string>
                    {
                        "smg.2",
                        "smg.thompson",
                        "rifle.semiauto",
                        "explosive.satchel",
                        "grenade.f1",
                        "grenade.beancan",
                        "surveycharge"
                    },
                    [43200] = new List<string>
                    {
                        "rifle.bolt",
                        "rifle.ak",
                        "rifle.lr300",
                        "metal.facemask",
                        "metal.plate.torso",
                        "rifle.l96",
                        "rifle.m39"
                    },
                    [64800] = new List<string>
                    {
                        "ammo.rifle.explosive",
                        "ammo.rocket.basic",
                        "ammo.rocket.fire",
                        "ammo.rocket.hv",
                        "rocket.launcher",
                        "explosive.timed"
                    },
                    [86400] = new List<string>
                    {
                        "lmg.m249",
                        "heavy.plate.helmet",
                        "heavy.plate.jacket",
                        "heavy.plate.pants",
                    }
                };

                return newConfiguration;
            }
        }

        #endregion

        #region Variables

        [PluginReference]
        private Plugin ImageLibrary, Duels, OneVSOne, Battles;

        private bool IsBattles(ulong userid)
        {
            return Battles != null && Battles.Call<bool>("IsPlayerOnBattle", userid);
        }

        private Configuration settings = null;

        [JsonProperty("Список градиентов")]
        private List<string> Gradients = new List<string> { "518eef", "5CAD4F", "5DAC4E", "5EAB4E", "5FAA4E", "60A94E", "61A84E", "62A74E", "63A64E", "64A54E", "65A44E", "66A34E", "67A24E", "68A14E", "69A04E", "6A9F4E", "6B9E4E", "6C9D4E", "6D9C4E", "6E9B4E", "6F9A4E", "71994E", "72984E", "73974E", "74964E", "75954E", "76944D", "77934D", "78924D", "79914D", "7A904D", "7B8F4D", "7C8E4D", "7D8D4D", "7E8C4D", "7F8B4D", "808A4D", "81894D", "82884D", "83874D", "84864D", "86854D", "87844D", "88834D", "89824D", "8A814D", "8B804D", "8C7F4D", "8D7E4D", "8E7D4D", "8F7C4D", "907B4C", "917A4C", "92794C", "93784C", "94774C", "95764C", "96754C", "97744C", "98734C", "99724C", "9B714C", "9C704C", "9D6F4C", "9E6E4C", "9F6D4C", "A06C4C", "A16B4C", "A26A4C", "A3694C", "A4684C", "A5674C", "A6664C", "A7654C", "A8644C", "A9634C", "AA624B", "AB614B", "AC604B", "AD5F4B", "AE5E4B", "B05D4B", "B15C4B", "B25B4B", "B35A4B", "B4594B", "B5584B", "B6574B", "B7564B", "B8554B", "B9544B", "BA534B", "BB524B", "BC514B", "BD504B", "BE4F4B", "BF4E4B", "C04D4B", "C14C4B", "C24B4B", "C44B4B" };

        private string Layer = "UI_328InstanceBlock";
        private string LayerBlock = "UI_328Block";
        private string LayerInfoBlock = "UI_328InfoBlock";

        private string IgnorePermission = "wipeblock.ignore";

        #endregion

        #region Initialization

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                settings = Config.ReadObject<Configuration>();
                if (settings?.SBlock == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => settings = Configuration.GetDefaultConfiguration();
        protected override void SaveConfig() => Config.WriteObject(settings);
        private long SaveCreatedTime = 0;
        private long ToEpoch(DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError("ImageLibrary not found, plugin will not work!");
                return;
            }
            foreach (var check in settings.SBlock.BlockItems.SelectMany(p => p.Value))
            {
                ImageLibrary.Call("AddImage", $"http://db.maxigames.su/images/rust/items/128/{check}.png", check);
            }

            SaveCreatedTime = ToEpoch(SaveRestore.SaveCreatedTime);
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand("block", this, "cmdChatDrawBlock");
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand("wipeblock", this, "cmdChatDrawBlock");
            permission.RegisterPermission(IgnorePermission, this);
            foreach (BasePlayer player in BasePlayer.activePlayerList) OnPlayerConnected(player);
            GUIMain = GUIMain.Replace("{min}", (-318 + settings.SInterface.Margin).ToString()).Replace("{max}", (278 + settings.SInterface.Margin).ToString()).Replace("{name}", settings.SInterface.ServerName);
            createcache();
        }


        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (active.ContainsKey(player.userID) && active[player.userID].active) CmdChatStopBlock(player);
            }
        }
        #endregion

        #region Hooks

        private object CanWearItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (IsBlocked(item.info) > 0)
            {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc || OneVSOne != null && (bool)OneVSOne.Call("IsEventPlayer", player))
                    return null;

                if (permission.UserHasPermission(player.UserIDString, IgnorePermission))
                    return null;

                DrawInstanceBlock(player, item);
                timer.Once(3f, () =>
                {

                    CuiHelper.DestroyUi(player, Layer + ".Destroy1");
                    CuiHelper.DestroyUi(player, Layer + ".Destroy2");
                    CuiHelper.DestroyUi(player, Layer + ".Destroy3");
                    CuiHelper.DestroyUi(player, Layer + ".Destroy5");
                    timer.Once(1, () => CuiHelper.DestroyUi(player, Layer));
                });
                return false;
            }
            return null;
        }

        private object CanEquipItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null) return null;

            if (IsBlocked(item.info) > 0)
            {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc || OneVSOne != null && (bool)OneVSOne.Call("IsEventPlayer", player))
                    return null;

                if (permission.UserHasPermission(player.UserIDString, IgnorePermission))
                    return null;

                DrawInstanceBlock(player, item);
                timer.Once(3f, () =>
                {

                    CuiHelper.DestroyUi(player, Layer + ".Destroy1");
                    CuiHelper.DestroyUi(player, Layer + ".Destroy2");
                    CuiHelper.DestroyUi(player, Layer + ".Destroy3");
                    CuiHelper.DestroyUi(player, Layer + ".Destroy5");
                    timer.Once(1, () => CuiHelper.DestroyUi(player, Layer));
                });
                return false;
            }
            return null;
        }

        private object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            if (player is NPCPlayer)
                return null;

            if (permission.UserHasPermission(player.UserIDString, IgnorePermission))
                return null;

            if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc || OneVSOne != null && (bool)OneVSOne.Call("IsEventPlayer", player))
                return null;

            if (IsBlocked(projectile.primaryMagazine.ammoType) > 0 && IsBattles(player.userID))
            {
                SendReply(player, $"Вы <color=#81B67A>не можете</color> использовать этот тип боеприпасов!");
                return false;
            }
            return null;
        }

        private object OnReloadMagazine(BasePlayer player, BaseProjectile projectile)
        {
            if (player is NPCPlayer)
                return null;

            if (permission.UserHasPermission(player.UserIDString, IgnorePermission) || OneVSOne != null && (bool)OneVSOne.Call("IsEventPlayer", player))
                return null;

            NextTick(() =>
            {
                if (IsBlocked(projectile.primaryMagazine.ammoType) > 0)
                {
                    player.GiveItem(ItemManager.CreateByItemID(projectile.primaryMagazine.ammoType.itemid, projectile.primaryMagazine.contents, 0UL), BaseEntity.GiveItemReason.Generic);
                    projectile.primaryMagazine.contents = 0;
                    //projectile.GetItem().LoseCondition(projectile.GetItem().maxCondition);
                    projectile.SendNetworkUpdate();
                    player.SendNetworkUpdate();

                    PrintError($"[{DateTime.Now.ToShortTimeString()}] {player} пытался взломать систему блокировки!");
                    SendReply(player, $"<color=#81B67A>Неа</color>!");
                }
            });

            return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            DrawBlockInfo(player);
        }

        #endregion

        #region GUI

        private void DrawBlockInfo(BasePlayer player)
        {
            if (!IsAnyBlocked()) return;

            CuiHelper.DestroyUi(player, LayerInfoBlock);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-180 -35", OffsetMax = "-10 -15" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", LayerInfoBlock);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-3 0", AnchorMax = "1 1.5", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "chat.say /block" },
                Text = { Text = settings.SInterface.FirstString, Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF5A"), Align = TextAnchor.UpperRight, FontSize = 20 },
            }, LayerInfoBlock);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-3 -0.2", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "chat.say /block" },
                Text = { Text = settings.SInterface.SecondString, Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF5A"), Align = TextAnchor.LowerRight, FontSize = 12 },
            }, LayerInfoBlock);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("block")]
        private void cmdConsoleDrawBlock(ConsoleSystem.Arg args)
        {
            cmdChatDrawBlock(args.Player());
        }

        [ConsoleCommand("blockmove")]
        private void cmdConsoleMoveblock(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
                return;
            if (!args.HasArgs(1))
            {
                PrintWarning($"Введите количество секунд для перемещения!");
                return;
            }

            int newTime;
            if (!int.TryParse(args.Args[0], out newTime))
            {
                PrintWarning("Вы ввели не число!");
                return;
            }

            settings.SBlock.TimeMove += newTime;
            SaveConfig();
            PrintWarning("Время блокировки успешно изменено!");
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        private void cmdChatDrawBlock(BasePlayer player)
        {
            if (active.ContainsKey(player.userID) && active[player.userID].active)
            {
                CmdChatStopBlock(player);
            }
            else
            {
                DrawBlockGUI(player);
            }
        }

        string lastid;
        void createcache()
        {
            Dictionary<string, Dictionary<Item, string>> cache = new Dictionary<string, Dictionary<Item, string>>();
            FillBlockedItems(cache);
            cacheitems.Clear();
            var blockedItemsNew = cache.OrderByDescending(p => p.Value.Count);
            string category = "";
            int newString = 0;
            bool refresh = false;
            for (int t = 0; t < blockedItemsNew.Count(); t++)
            {
                var blockedCategory = blockedItemsNew.ElementAt(t).Value.OrderBy(p => IsBlocked(p.Value));
                category += GUICategory.Replace("{min}", (0.889 - (t) * 0.17 - newString * 0.123).ToString()).Replace("{max}", (0.925 - (t) * 0.17 - newString * 0.123).ToString()).Replace("{name}", blockedItemsNew.ElementAt(t).Key);
                for (int i = 0; i < blockedCategory.Count(); i++)
                {
                    if (i == 12)
                    {
                        newString++;
                    }
                    float margin = Mathf.CeilToInt(blockedCategory.Count() - Mathf.CeilToInt((float)(i + 1) / 12) * 12);
                    if (margin < 0)
                    {
                        margin *= -1;
                    }
                    else
                    {
                        margin = 0;
                    }

                    var blockedItem = blockedCategory.ElementAt(i);
                    string ID = (string)ImageLibrary?.Call("GetImage", blockedItem.Key.info.shortname);
                    if (string.IsNullOrEmpty(ID) || ID == lastid)
                    {
                        refresh = true;
                    }
                    lastid = ID;
                    string text = IsBlocked(blockedItem.Key.info) > 0
                        ? $"<size=10>ОСТАЛОСЬ</size>\n<size=14>{TimeSpan.FromSeconds((int)IsBlocked(blockedItem.Key.info)).ToShortString()}</size>"
                        : "<size=11>ДОСТУПНО</size>";
                    cacheitems.Add(blockedItem.Key.info.shortname, GUIItem.Replace("{min}", $"{0.008608246 + i * 0.0837714 + ((float)margin / 2) * 0.0837714 - (Math.Floor((double)i / 12) * 12 * 0.0837714)}" + $" {0.7618223 - (t) * 0.17 - newString * 0.12}").Replace("{max}", $"{0.08415613 + i * 0.0837714 + ((float)margin / 2) * 0.0837714 - (Math.Floor((double)i / 12) * 12 * 0.0837714)}" + $" {0.8736619 - (t) * 0.17 - newString * 0.12}").Replace("{png}", ID).Replace("{name}", blockedItem.Key.info.shortname));
                }
            }
            GUIMain = GUIMain.Replace("{category}", category);
            if (refresh)
            {
                Debug.Log("Не получилось загрузить все картинки, повторим попытку через 5 секунд!");
                timer.Once(5f, () => createcache());
            }
            else Debug.Log("Картинки загружены, плагин готов к работе!");
        }

        [ChatCommand("stopBlock")]
        private void CmdChatStopBlock(BasePlayer player)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", LayerBlock);
            if (active.ContainsKey(player.userID)) active[player.userID].active = false;
        }
        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (container.entityOwner != null && container.entityOwner is AutoTurret)
            {
                if (IsBlocked(item.info) > 0f)
                {
                    BasePlayer player = item.GetOwnerPlayer();
                    if (player != null)
                    {
                        DrawInstanceBlock(player, item);
                        timer.Once(3f, () =>
                        {

                            CuiHelper.DestroyUi(player, Layer + ".Destroy1");
                            CuiHelper.DestroyUi(player, Layer + ".Destroy2");
                            CuiHelper.DestroyUi(player, Layer + ".Destroy3");
                            CuiHelper.DestroyUi(player, Layer + ".Destroy5");
                            timer.Once(1, () => CuiHelper.DestroyUi(player, Layer));
                        });
                    }
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            return null;
        }
        string GUIMain = "[{\"name\":\"UI_328Block\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-441.5 {min}\",\"offsetmax\":\"441.5 {max}\"},{\"type\":\"NeedsCursor\"}]},{\"name\":\"3ff2c7eaa31441c987abad215d5eb7c4\",\"parent\":\"UI_328Block\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"chat.say /stopBlock\",\"close\":\"UI_328Block\",\"material\":\"assets/content/ui/uibackgroundblur.mat\",\"color\":\"0 0 0 0.9\"},{\"type\":\"RectTransform\",\"anchormin\":\"-100 -100\",\"anchormax\":\"100 100\",\"offsetmax\":\"0 0\"}]},{\"name\":\"UI_328Block.Header\",\"parent\":\"UI_328Block\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.92860328154\",\"anchormax\":\"1.015 0.9998464\",\"offsetmax\":\"0 0\"}]},{\"parent\":\"UI_328Block.Header\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"БЛОКИРОВКА ПРЕДМЕТОВ НА {name}\",\"fontSize\":30,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}{category}{main}]";
        string GUICategory = ",{\"name\":\"UI_328Block.Category\",\"parent\":\"UI_328Block\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 {min}\",\"anchormax\":\"1.015 {max}\",\"offsetmax\":\"0 0\"}]},{\"parent\":\"UI_328Block.Category\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"БЛОКИРОВКА {name}\",\"fontSize\":16,\"font\":\"robotocondensed-regular.ttf\",\"align\":\"MiddleCenter\",\"color\":\"1 1 1 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}";
        string GUIItem = ",{\"name\":\"UI_328Block.{name}\",\"parent\":\"UI_328Block\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{color}\",\"fadeIn\":0.5},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\",\"offsetmax\":\"0 0\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.1\",\"distance\":\"1 1\"}]},{\"parent\":\"UI_328Block.{name}\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{png}\",\"fadeIn\":0.5},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"2 2\",\"offsetmax\":\"-2 -2\"}]},{\"name\":\"Time.{name}\",\"parent\":\"UI_328Block.{name}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Time.{name}.Update\",\"parent\":\"Time.{name}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"parent\":\"Time.{name}.Update\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{info}\",\"fontSize\":10,\"font\":\"robotocondensed-regular.ttf\",\"align\":\"MiddleCenter\",\"fadeIn\":0.5},{\"type\":\"RectTransform\"}]}";

        Dictionary<string, string> cacheitems = new Dictionary<string, string>();

        private void DrawBlockGUI(BasePlayer player)
        {
            if (!active.ContainsKey(player.userID)) active.Add(player.userID, new perforator { active = true, lastuse = DateTime.Now.AddSeconds(3) });
            else
            {
                if (active[player.userID].lastuse > DateTime.Now)
                {
                    if (active[player.userID].lastmsg < DateTime.Now) player.ChatMessage("НЕ ТАК ЧАСТО!");
                    active[player.userID].lastmsg = DateTime.Now.AddSeconds(3);
                    return;
                }
                active[player.userID].lastuse = DateTime.Now.AddSeconds(3);
                active[player.userID].active = true;
            }
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", LayerBlock);
            string main = "";
            foreach (var blockedItem in settings.SBlock.BlockItems)
            {
                TimeSpan times = SaveRestore.SaveCreatedTime.AddSeconds(blockedItem.Key) - DateTime.UtcNow;
                double val = times.TotalSeconds;
                string text = val > 0 ? $"<size=10>ОСТАЛОСЬ</size>\n<size=14>{times.ToShortString()}</size>" : "<size=11>ДОСТУПНО</size>";
                foreach (var x in blockedItem.Value)
                {
                    main += cacheitems[x].Replace("{color}", HexToRustFormat(Gradients[Math.Min(99, Math.Max(Convert.ToInt32((float)val / blockedItem.Key * 100), 0))] + "96")).Replace("{info}", text);
                }
            }
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIMain.Replace("{main}", main));
            //timeGUI[player.userID] = timer.Every(1f, () => StartUpdate(player));
        }
        Dictionary<ulong, perforator> active = new Dictionary<ulong, perforator>();
        class perforator
        {
            public DateTime lastuse;
            public DateTime lastmsg;
            public bool active;
        }

        /*Dictionary<ulong, Timer> timeGUI = new Dictionary<ulong, Timer>();
        string GUIupdate = "[{\"name\":\"Time.{name}\",\"parent\":\"UI_328Block.{name}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Time.{name}.Update\",\"parent\":\"Time.{name}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"parent\":\"Time.{name}.Update\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{info}\",\"fontSize\":10,\"font\":\"robotocondensed-regular.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]}]";

        private void StartUpdate(BasePlayer player)
        {
            foreach (var blockedItem in settings.SBlock.BlockItems)
            {
                TimeSpan times = SaveRestore.SaveCreatedTime.AddSeconds(blockedItem.Key) - DateTime.Now;
                double val = times.TotalSeconds;
                if (val < -2) continue;
                foreach (var x in blockedItem.Value)
                {
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", $"Time.{x}.Update");
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", $"Time.{x}");
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIupdate.Replace("{name}", x).Replace("{info}", val > 0 ? $"<size=10>ОСТАЛОСЬ</size>\n<size=14>{times.ToShortString()}</size>" : "<size=11>ДОСТУПНО</size>"));
                }
            }
        }*/

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        string GUIBlock = "[{\"name\":\"UI_328InstanceBlock\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1 0.1 0.1 0\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0.35 0.75\",\"anchormax\":\"0.62 0.95\"}],\"fadeOut\":1.0},{\"name\":\"UI_328InstanceBlock.Hide\",\"parent\":\"UI_328InstanceBlock\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}],\"fadeOut\":1.0},{\"name\":\"UI_328InstanceBlock.Destroy1\",\"parent\":\"UI_328InstanceBlock.Hide\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4 0.4 0.4 0.7\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.62\",\"anchormax\":\"1.1 0.85\"}],\"fadeOut\":1.0},{\"name\":\"UI_328InstanceBlock.Destroy5\",\"parent\":\"UI_328InstanceBlock.Destroy1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ПРЕДМЕТ ЗАБЛОКИРОВАН\",\"fontSize\":22,\"font\":\"robotocondensed-bold.ttf\",\"align\":\"MiddleCenter\",\"color\":\"0.9 0.9 0.9 1\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}],\"fadeOut\":1.0},{\"name\":\"UI_328InstanceBlock.Destroy2\",\"parent\":\"UI_328InstanceBlock.Hide\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0.3 0.3 0.3 0.5\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.29\",\"anchormax\":\"1.1 0.61\"}],\"fadeOut\":1.0},{\"name\":\"UI_328InstanceBlock.Destroy3\",\"parent\":\"UI_328InstanceBlock.Hide\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16,\"font\":\"robotocondensed-regular.ttf\",\"align\":\"MiddleLeft\",\"color\":\"0.85 0.85 0.85 1\",\"fadeIn\":1.0},{\"type\":\"RectTransform\",\"anchormin\":\"0.04 0\",\"anchormax\":\"10 0.9\"}],\"fadeOut\":1.0}]";

        private void DrawInstanceBlock(BasePlayer player, Item item)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", Layer);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUIBlock.Replace("{text}", "Предмет {name} временно заблокирован,\nподождите {1}".Replace("{name}", item.info.displayName.english).Replace("{1}", $"{Convert.ToInt32(Math.Floor(TimeSpan.FromSeconds(IsBlocked(item.info)).TotalHours))} час {TimeSpan.FromSeconds(IsBlocked(item.info)).Minutes} минут.")));
        }

        #endregion

        #region Functions

        private string GetGradient(int t)
        {
            var LeftTime = UnBlockTime(t) - CurrentTime();
            return Gradients[Math.Min(99, Math.Max(Convert.ToInt32((float)LeftTime / t * 100), 0))];
        }

        private double IsBlockedCategory(int t) => IsBlocked(settings.SBlock.BlockItems.ElementAt(t).Value.First());
        private bool IsAnyBlocked() => UnBlockTime(settings.SBlock.BlockItems.Last().Key) + settings.SBlock.TimeMove > CurrentTime();
        private double IsBlocked(string shortname)
        {
            if (!settings.SBlock.BlockItems.SelectMany(p => p.Value).Contains(shortname))
                return 0;

            var blockTime = settings.SBlock.BlockItems.FirstOrDefault(p => p.Value.Contains(shortname)).Key;
            var lefTime = (UnBlockTime(blockTime)) - CurrentTime();

            return lefTime > 0 ? lefTime : 0;
        }

        private double UnBlockTime(int amount) => SaveCreatedTime + amount + settings.SBlock.TimeMove;

        private double IsBlocked(ItemDefinition itemDefinition) => IsBlocked(itemDefinition.shortname);

        private void FillBlockedItems(Dictionary<string, Dictionary<Item, string>> fillDictionary)
        {
            foreach (var category in settings.SBlock.BlockItems)
            {
                string categoryColor = GetGradient(category.Key);
                foreach (var item in category.Value)
                {
                    Item createItem = ItemManager.CreateByPartialName(item);
                    string catName = settings.SBlock.CategoriesName[createItem.info.category.ToString()];

                    if (!fillDictionary.ContainsKey(catName))
                        fillDictionary.Add(catName, new Dictionary<Item, string>());

                    if (!fillDictionary[catName].ContainsKey(createItem))
                        fillDictionary[catName].Add(createItem, categoryColor);
                }
            }
        }

        #endregion

        #region Utils

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        public static string ToShortString(TimeSpan timeSpan)
        {
            int i = 0;
            string resultText = "";
            if (timeSpan.Days > 0)
            {
                resultText += timeSpan.Days + " День";
                i++;
            }
            if (timeSpan.Hours > 0 && i < 2)
            {
                if (resultText.Length != 0)
                    resultText += " ";
                resultText += timeSpan.Days + " Час";
                i++;
            }
            if (timeSpan.Minutes > 0 && i < 2)
            {
                if (resultText.Length != 0)
                    resultText += " ";
                resultText += timeSpan.Days + " Мин.";
                i++;
            }
            if (timeSpan.Seconds > 0 && i < 2)
            {
                if (resultText.Length != 0)
                    resultText += " ";
                resultText += timeSpan.Days + " Сек.";
                i++;
            }

            return resultText;
        }

        private void GetConfig<T>(string menu, string key, ref T varObject)
        {
            if (Config[menu, key] != null)
            {
                varObject = Config.ConvertValue<T>(Config[menu, key]);
            }
            else
            {
                Config[menu, key] = varObject;
            }
        }

        #endregion
    }
}