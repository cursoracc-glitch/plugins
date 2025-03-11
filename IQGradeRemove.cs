using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQGradeRemove", "Mercury", "0.1.5")]
    [Description("Умный Grade Remove")]
    class IQGradeRemove : RustPlugin
    {
        /// <summary> 
        /// Обновление 0.1.3
        /// - Изменен метод проверки на объекты при постройке
        /// Обновление 0.1.4
        /// - Поправлена проверка на ресурсы при улучшении всех объектов
        /// - Исправлен интерфейс
        /// - Добавлена проверка на null в хуке OnHammerHit
        /// </summary>
        /// Обновление 0.1.5
        /// - Исправлено возврат лестницы/сетки при удалении
        /// - Исправлено возврат стен и ворот при удалениие
        /// - Исправлен возврат карьера при удалении
        /// - Исправлена возможность забиндить команд на улучшение и удаление
        /// - Исправлено удаление объектов на РТ
        /// - Исправлен метод с запретом на удаление
        /// - Исправлен метод с запретом на возврат предмета после удаления
        /// - Добавлена возможность скрывать меню после истечения времени улучшения/удаления (Настраивается в конфигурации)
        /// - Добавлена возможность заменить символ на кнопке закрыть
        /// - Добавлена возможность скрыть кнопку закрыть
        /// - Добавлена возможность скрыть уровни улучшения в интерфейсе

        #region Vars
        public static String PermissionGRMenu = "iqgraderemove.gruse";
        public static String PermissionGRNoResource = "iqgraderemove.grusenorecource";
        public readonly Dictionary<Int32, String> StatusLevels = new Dictionary<Int32, String>
        {
            [0] = "ОТКЛЮЧЕНО",
            [1] = "ДЕРЕВА",
            [2] = "КАМНЯ",
            [3] = "МЕТАЛЛА",
            [4] = "МВК",
            [5] = "УДАЛЕНИЕ",
            [6] = "УДАЛЕНИЕ ВСЕГО",
            [7] = "УЛУЧШЕНИЕ ВСЕГО",
        };

        public readonly Dictionary<Int32, String> StatusLevelsAllGrades = new Dictionary<Int32, String>
        {
            [0] = "В СОЛОМУ",
            [1] = "В ДЕРЕВО",
            [2] = "В КАМЕНЬ",
            [3] = "В МЕТАЛЛ",
            [4] = "В МВК",
        };
        public readonly Dictionary<Int32, String> SoundLevelsGrade = new Dictionary<Int32, String>
        {
            [0] = "ОТКЛЮЧЕНО",
            [1] = "assets/bundled/prefabs/fx/build/frame_place.prefab",
            [2] = "assets/bundled/prefabs/fx/build/promote_stone.prefab",
            [3] = "assets/bundled/prefabs/fx/build/promote_metal.prefab",
            [4] = "assets/bundled/prefabs/fx/build/promote_toptier.prefab",
            [5] = "УДАЛЕНИЕ"
        };
        public static readonly Dictionary<Int32, String> PermissionsLevel = new Dictionary<Int32, String>
        {
            [0] = "",
            [1] = "iqgraderemove.upwood",
            [2] = "iqgraderemove.upstones",
            [3] = "iqgraderemove.upmetal",
            [4] = "iqgraderemove.uphmetal",
            [5] = "iqgraderemove.removeuse",
        };



        #region Format Time
        static Int32 CurrentTime() => Facepunch.Math.Epoch.Current;
        public static String FormatTime(TimeSpan time)
        {
            String result = String.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "д", "д", "д")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "ч", "ч", "ч")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "м", "м", "м")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "с", "с", "с")} ";

            return result;
        }
        private static String Format(Int32 units, String form1, String form2, String form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        #endregion

        #endregion

        #region Reference
        [PluginReference] Plugin Friends;
        public Boolean IsRaidBlocked(BasePlayer player)
        {
            var ret = Interface.Call("CanTeleport", player) as String;
            if (ret != null)
                return true;
            else return false;
        }
        public Boolean IsFriends(UInt64 userID, UInt64 targetID)
        {
            if (Friends)
                return (Boolean)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }

        void RegisteredPermissions()
        {
            var RemoveTimed = config.RemoveSetting.TimedSetting;

            foreach (var PermsBlockTimed in RemoveTimed.ItemsTimesAllPermissions)
                if (!permission.PermissionExists(PermsBlockTimed.Key, this))
                    permission.RegisterPermission(PermsBlockTimed.Key, this);

            foreach (var PermsBlockTimed in RemoveTimed.ItemsTimesPermissions)
                if (!permission.PermissionExists(PermsBlockTimed.Key, this))
                    permission.RegisterPermission(PermsBlockTimed.Key, this);

            if (!permission.PermissionExists(PermissionGRMenu, this))
                permission.RegisterPermission(PermissionGRMenu, this);

            if (!permission.PermissionExists(PermissionGRNoResource, this))
                permission.RegisterPermission(PermissionGRNoResource, this);

            foreach (string Permissions in PermissionsLevel.Values)
                if (!permission.PermissionExists(Permissions, this))
                    permission.RegisterPermission(Permissions, this);
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка удаления объектов")]
            public RemoveSettings RemoveSetting = new RemoveSettings();
            [JsonProperty("Настройка улучшения объектов")]
            public GradeSettings GradeSetting = new GradeSettings();
            [JsonProperty("Настройка интерфейса")]
            public InterfaceSettings InterfaceSetting = new InterfaceSettings();
            [JsonProperty("Использовать пермишенсы для включения определенного UP или ремува(смотрите в описании плагина, там указаны права)")]
            public bool UsePermission;

            #region Remove Configuration
            internal class RemoveSettings
            {
                [JsonProperty("Настройка возврата предметов после удаления")]
                public ReturnedSettings ReturnedSetting = new ReturnedSettings();
                [JsonProperty("Настройка удаления через время")]
                public TimedSettings TimedSetting = new TimedSettings();
                [JsonProperty("Настройка запретов")]
                public SettingsBlocks SettingsBlock = new SettingsBlocks();
                [JsonProperty("Время действия удаления")]
                public int RemoveTime;

                internal class ReturnedSettings
                {
                    [JsonProperty("Возвращать ресурсы за удаление строений?(true- да/false - нет)")]
                    public bool UseReturnedResource;
                    [JsonProperty("Процент возврата ресурсов за удаление строений?(true- да/false - нет)")]
                    public int PercentReturnRecource;
                    [JsonProperty("Возвращать все предметы при удалении?(true- да/false - нет)")]
                    public bool UseAllowedReturned;
                    [JsonProperty("Снижать состояние предмета при возврате?Эффект будто он поднял его через RUST систему")]
                    public bool UseDamageReturned;
                    [JsonProperty("Предметы,которые не возвращаются при удалении(Shortname)")]
                    public List<string> ShortnameNoteReturned = new List<string>();
                }
                internal class TimedSettings
                {
                    [JsonProperty("Включить полный запрет на удаление объекта через N время(Пример : Через 3 часа после постройки,его нельзя будет удалить вообще)")]
                    public bool UseAllBlock;
                    [JsonProperty("Через сколько нельзя будет удалять постройку вообще")]
                    public int TimeAllBlock;
                    [JsonProperty("Кастомный список предметов,которые нельзя будет удалить через время по правам. [[IQGradeRemove.NAME]] - Время(в сек)")]
                    public Dictionary<string, int> ItemsTimesAllPermissions = new Dictionary<string, int>();
                    [JsonProperty("Использовать запрет на удаление постройки на время(После постройки объекта,его N количество времени нельзя будет удалить)")]
                    public bool UseTimesBlock;
                    [JsonProperty("Через сколько можно будет удалять постройку(если включено)")]
                    public int TimesBlock;
                    [JsonProperty("Кастомный список предметов,которые можно будет удалить через время по правам. [[IQGradeRemove.NAME]] - Время(в сек)")]
                    public Dictionary<string, int> ItemsTimesPermissions = new Dictionary<string, int>();
                }
                internal class SettingsBlocks
                {
                    [JsonProperty("[NoEscape] Запретить удаление во время рейдблока(true - да/false - нет)")]
                    public bool NoEscape;
                    [JsonProperty("[Friends] Удалять постройки могут только друзья(Иначе все,кто есть в шкафу)(true - да/false - нет)")]
                    public bool Friends;
                    [JsonProperty("Предметы,которые нельзя удалить(Shortname)")]
                    public List<string> ShortnameNoteReturned = new List<string>();
                }
            }
            #endregion

            #region Grade Configuration
            internal class GradeSettings
            {
                [JsonProperty("Время действия улучшения")]
                public int GradeTime;
                [JsonProperty("Разрешить обратное улучшение?(Пример : МВК стенку откатить в деревянную)(true - да/false - нет)")]
                public bool UseBackUp;
                [JsonProperty("Настройка запретов")]
                public SettingsBlocks SettingsBlock = new SettingsBlocks();
                internal class SettingsBlocks
                {
                    [JsonProperty("[NoEscape] Запретить улучшение во время рейдблока(true - да/false - нет)")]
                    public bool NoEscape;
                }
            }
            #endregion 

            #region Interface
            internal class InterfaceSettings
            {
                [JsonProperty("Настройка интерфейса")]
                public MainInterface MainInterfaces = new MainInterface();

                internal class MainInterface
                {
                    [JsonProperty("Скрывать интерфейс по истечению таймера")]
                    public Boolean HideMenuTimer;
                    [JsonProperty("Отображать уровни улучшений в интерфейсе")]
                    public Boolean ShowLevelUp;
                    [JsonProperty("Отображать кнопку закрыть в меню")]
                    public Boolean ShowCloseButton;
                    [JsonProperty("Символ для кнопки закрыть")]
                    public String SymbolCloseButton;
                    [JsonProperty("Цвет панели(HEX)")]
                    public string ColorPanel;
                    [JsonProperty("Цвет текста(HEX)")]
                    public string ColorText;
                }
            }
            #endregion

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UsePermission = false,

                    #region Remove Configuration
                    RemoveSetting = new RemoveSettings
                    {
                        RemoveTime = 30,
                        ReturnedSetting = new RemoveSettings.ReturnedSettings
                        {
                            UseReturnedResource = true,
                            PercentReturnRecource = 50,
                            UseDamageReturned = true,
                            UseAllowedReturned = true,
                            ShortnameNoteReturned = new List<string>
                            {
                                "campfire",
                            }
                        },
                        TimedSetting = new RemoveSettings.TimedSettings
                        {
                            UseAllBlock = false,
                            TimeAllBlock = 60,
                            ItemsTimesAllPermissions = new Dictionary<string, int>
                            {
                                ["iqgraderemove.vip"] = 200,
                                ["iqgraderemove.prem"] = 250,
                                ["iqgraderemove.gold"] = 300,
                            },
                            UseTimesBlock = false,
                            TimesBlock = 100,
                            ItemsTimesPermissions = new Dictionary<string, int>
                            {
                                ["iqgraderemove.vip"] = 150,
                                ["iqgraderemove.prem"] = 200,
                                ["iqgraderemove.gold"] = 300,
                            },
                        },
                        SettingsBlock = new RemoveSettings.SettingsBlocks
                        {
                            Friends = true,
                            NoEscape = true,
                            ShortnameNoteReturned = new List<string>
                            {
                                "campfire",
                            }
                        }
                    },
                    #endregion

                    #region Grade Configuration
                    GradeSetting = new GradeSettings
                    {
                        GradeTime = 30,
                        UseBackUp = false,
                        SettingsBlock = new GradeSettings.SettingsBlocks
                        {
                            NoEscape = true,
                        }
                    },
                    #endregion

                    #region Interface
                    InterfaceSetting = new InterfaceSettings
                    {
                        MainInterfaces = new InterfaceSettings.MainInterface
                        {
                            ColorPanel = "#525252",
                            ColorText = "#C9C0B9FF",
                            SymbolCloseButton = "<",
                            ShowCloseButton = true,
                            ShowLevelUp = true,
                            HideMenuTimer = true,
                        },
                    },
                    #endregion
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #132" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        public Dictionary<ulong, GradeRemove> DataPlayer = new Dictionary<ulong, GradeRemove>();
        public Dictionary<uint, int> BuildingRemoveTimers = new Dictionary<uint, int>();
        public Dictionary<uint, int> BuildingRemoveBlock = new Dictionary<uint, int>();
        void ReadData() {
            BuildingRemoveTimers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, int>>("IQGradeRemove/BlockBuilding");
            BuildingRemoveBlock = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, int>>("IQGradeRemove/BuildingRemoveBlock");
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQGradeRemove/BlockBuilding", BuildingRemoveTimers);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQGradeRemove/BuildingRemoveBlock", BuildingRemoveBlock);
        }
        ///0 - солома
        ///1 - дерево
        ///2 - камень
        ///3 - металл
        ///4 - мвк
        public class GradeRemove
        {
            public int ActiveTime;
            public int GradeLevel;
            public Boolean GradeAllObject;
            public Timer TimerEvent = null;
            public void GradeUP(BasePlayer player, bool UseMyGrade = false, int CustomGrade = 0)
            {
                if (config.UsePermission)
                    foreach (var Perm in PermissionsLevel.Where(p => inst.permission.UserHasPermission(player.UserIDString, p.Value)))
                    {
                        if (!UseMyGrade)
                        {
                            if (GradeLevel == 5) GradeLevel = 0;
                            if (GradeLevel >= Perm.Key) continue;
                            GradeLevel = Perm.Key;
                        }
                        else if (String.IsNullOrWhiteSpace(PermissionsLevel[CustomGrade]) || inst.permission.UserHasPermission(player.UserIDString, PermissionsLevel[CustomGrade]))
                        {
                            GradeLevel = CustomGrade;
                            return;
                        }
                        else inst.Interface_Error(player, inst.GetLang("NO_PERM_GRADE_REMOVE", player.UserIDString));
                        break;
                    }
                else
                {
                    if (!UseMyGrade)
                    {
                        if (GradeLevel > 4)
                            GradeLevel = 0;
                        else GradeLevel++;
                    }
                    else GradeLevel = CustomGrade;
                }

                RebootTimer();
                int TimeActive = CustomGrade != 0 & CustomGrade != 5 ? config.GradeSetting.GradeTime : config.RemoveSetting.RemoveTime;
                ActiveTime = Convert.ToInt32(TimeActive + CurrentTime());
            }
            public void RebootTimer()
            {
                if (TimerEvent != null)
                    TimerEvent.Destroy();
            }
        }
        void RegisteredUser(BasePlayer player)
        {
            if (!DataPlayer.ContainsKey(player.userID))
                DataPlayer.Add(player.userID, new GradeRemove { ActiveTime = 0, GradeLevel = 0 });

            if (player.HasFlag(BaseEntity.Flags.Reserved10))
                player.SetFlag(BaseEntity.Flags.Reserved10, false);
        }
        #endregion

        #region Grade Core
        public String GradeErrorParse(BasePlayer player, BuildingBlock buildingBlock, BuildingGrade.Enum grade = BuildingGrade.Enum.None)
        {
            var Data = DataPlayer[player.userID];
            var Block = config.GradeSetting.SettingsBlock;

            if (DeployVolume.Check(buildingBlock.transform.position, buildingBlock.transform.rotation, PrefabAttribute.server.FindAll<DeployVolume>(buildingBlock.prefabID), ~(5 << buildingBlock.gameObject.layer)))
                return GetLang("GRADE_NO_THIS_USER", player.UserIDString);

            if (Block.NoEscape && IsRaidBlocked(player))
                return GetLang("GRADE_NO_ESCAPE", player.UserIDString);

            if (buildingBlock.SecondsSinceAttacked < 30)
                return GetLang("GRADE_ATTACKED_BLOCK", player.UserIDString, FormatTime(TimeSpan.FromSeconds(30 - (int)buildingBlock.SecondsSinceAttacked)));

            if (!player.CanBuild())
                return GetLang("GRADE_NO_AUTH", player.UserIDString);

            if (!permission.UserHasPermission(player.UserIDString, PermissionGRNoResource))
            {
                Int32 Grade = grade == BuildingGrade.Enum.None ? Data.GradeLevel : (Int32)grade;
                if (!buildingBlock.CanAffordUpgrade((BuildingGrade.Enum)Grade, player))
                    return GetLang("GRADE_NO_RESOURCE", player.UserIDString);
            }

            return "";
        }

        public void GradeBuilding(BasePlayer player, BuildingBlock buildingBlock)
        {
            var Data = DataPlayer[player.userID];
            String AlertGrrade = GradeErrorParse(player, buildingBlock);
            if (!String.IsNullOrWhiteSpace(AlertGrrade))
            {
                Interface_Error(player, AlertGrrade);
                return;
            }
            if (!config.GradeSetting.UseBackUp)
                if (buildingBlock.grade > (BuildingGrade.Enum)Data.GradeLevel) return;

            if (!permission.UserHasPermission(player.UserIDString, PermissionGRNoResource))
                buildingBlock.PayForUpgrade(buildingBlock.GetGrade((BuildingGrade.Enum)Data.GradeLevel), player);

            buildingBlock.SetGrade((BuildingGrade.Enum)Data.GradeLevel);
            buildingBlock.SetHealthToMax();
            buildingBlock.UpdateSkin();

            Effect.server.Run(SoundLevelsGrade[Data.GradeLevel], player.GetNetworkPosition());
            DataPlayer[player.userID].ActiveTime = (Int32)(config.GradeSetting.GradeTime + CurrentTime());
        }


        public void GradeAll(BasePlayer player, BuildingBlock buildingBlock)
        {
            if (buildingBlock.GetBuildingPrivilege() == null)
            {
                Interface_Error(player, GetLang("GRADE_ALL_NO_AUTH",player.UserIDString));
                return;
            }
            if (!player.IsBuildingAuthed())
            {
                Interface_Error(player, GetLang("GRADE_NO_AUTH", player.UserIDString));
                return;
            }

            foreach (var Block in buildingBlock.GetBuildingPrivilege().GetBuilding().buildingBlocks.Where(x => x.grade != (BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel))
            {
                if (!permission.UserHasPermission(player.UserIDString, PermissionGRNoResource))
                {
                    if (!Block.CanAffordUpgrade(((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel), player))
                    {
                        Interface_Error(player, GetLang("GRADE_NO_RESOURCE", player.UserIDString));
                        return;
                    }
                    else
                    {
                        Block.PayForUpgrade(buildingBlock.GetGrade((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel), player);
                        Block.SetGrade((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel);
                        Block.SetHealthToMax();
                        Block.UpdateSkin();
                    }
                }
                else
                {
                    Block.SetGrade((BuildingGrade.Enum)DataPlayer[player.userID].GradeLevel);
                    Block.SetHealthToMax();
                    Block.UpdateSkin();
                }
            }
        }
        #endregion

        #region Remove Core

        #region Permanent Block
        private void AddRemoveBlockBuild(UInt32 netID, UInt64 userID)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            if (!RemoveTime.UseAllBlock) return;
            if (IsBlockAvailablePermanent(netID)) return;
            Int32 Time = GetTimeBlockPermanent(userID);
            BuildingRemoveBlock.Add(netID, Time);
        }
        private Boolean IsBlockAvailablePermanent(UInt32 netID)
        {
            if (BuildingRemoveBlock.ContainsKey(netID))
                return true;
            else return false;
        }
        private Boolean IsBlockBuildPemanent(UInt32 netID)
        {
            if (IsBlockAvailablePermanent(netID))
                if (BuildingRemoveBlock[netID] <= CurrentTime())
                    return true;
                else return false;
            else return false;
        }
        private Int32 GetTimeBlockPermanent(UInt64 userID)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            Int32 Time = Convert.ToInt32(CurrentTime() + RemoveTime.TimeAllBlock);

            foreach (var Perms in RemoveTime.ItemsTimesAllPermissions)
                if (permission.UserHasPermission(userID.ToString(), Perms.Key))
                {
                    Time = Convert.ToInt32(CurrentTime() + Perms.Value);
                    return Time;
                }

            return Time;
        }
        #endregion

        #region Temporally Block
        private void AddBlockBuild(UInt32 netID, UInt64 userID)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            if (!RemoveTime.UseTimesBlock) return;
            if (IsBlockAvailable(netID)) return;
            Int32 Time = GetTimeBlock(userID);

            BuildingRemoveTimers.Add(netID, Time);
        }
        private Boolean IsBlockAvailable(UInt32 netID)
        {
            if (BuildingRemoveTimers.ContainsKey(netID))
                return true;
            else return false;
        }
        private Boolean IsBlockBuild(UInt32 netID)
        {
            if (IsBlockAvailable(netID))
                if (BuildingRemoveTimers[netID] > CurrentTime())
                    return true;
                else return false;
            else return false;
        }
        private Int32 GetTimeBlock(UInt64 userID = 0)
        {
            var RemoveTime = config.RemoveSetting.TimedSetting;
            Int32 Time = Convert.ToInt32(CurrentTime() + RemoveTime.TimesBlock);
            if (!userID.IsSteamId()) return Time;

            foreach (var Perms in RemoveTime.ItemsTimesPermissions)
                if (permission.UserHasPermission(userID.ToString(), Perms.Key))
                {
                    Time = Convert.ToInt32(CurrentTime() + Perms.Value);
                    return Time;
                }

            return Time;
        }
        private Int32 GetTimerBlock(UInt32 netID)
        {
            Int32 Time = 0;
            if (IsBlockAvailable(netID))
                Time = Convert.ToInt32(BuildingRemoveTimers[netID] - CurrentTime());
            return Time;
        }
        #endregion

        void ReturnedRemoveItems(BasePlayer player, BaseEntity buildingBlock)
        {
            var Remove = config.RemoveSetting.ReturnedSetting;
            if(Remove.ShortnameNoteReturned.Contains(Regex.Replace(buildingBlock.ShortPrefabName.Replace("mining_quarry", "mining.quarry"), "\\.deployed|_deployed", ""))) return;

            if (Remove.UseAllowedReturned)
            {
                Item ItemReturned = buildingBlock is BaseOven || buildingBlock is MiningQuarry || buildingBlock is BaseLadder || buildingBlock.GetComponent<BaseCombatEntity>().pickup.itemTarget == null ?
                                    ItemManager.CreateByName(Regex.Replace(buildingBlock.ShortPrefabName.Replace("mining_quarry", "mining.quarry"), "\\.deployed|_deployed", ""), 1) : ItemManager.Create(buildingBlock.GetComponent<BaseCombatEntity>().pickup.itemTarget, 1);

                if (ItemReturned != null)
                {
                    if (Remove.UseDamageReturned)
                    {
                        Single healthFraction = buildingBlock.GetComponent<BaseCombatEntity>().Health() / buildingBlock.GetComponent<BaseCombatEntity>().MaxHealth();
                        ItemReturned.conditionNormalized = Mathf.Clamp01(healthFraction - buildingBlock.GetComponent<BaseCombatEntity>().pickup.subtractCondition);
                    }
                    player.GiveItem(ItemReturned);
                    return;
                }
            }
            if (Remove.UseReturnedResource)
                if (buildingBlock is StabilityEntity)
                {
                    Single PercentResource = (Single)((Single)(Remove.PercentReturnRecource) / (Single)(100));
                    foreach (var CostReturned in (buildingBlock as StabilityEntity).BuildCost())
                        player.GiveItem(ItemManager.Create(CostReturned.itemDef, Mathf.FloorToInt((Single)CostReturned.amount * PercentResource)));
                    return;
                }
        }

        public void RemoveAll(BasePlayer player, BaseEntity buildingBlock)
        {
            if (buildingBlock.GetBuildingPrivilege() == null)
            {
                Interface_Error(player, GetLang("REMOVE_ALL_NO_AUTH", player.UserIDString));
                return;
            }
            ListHashSet<BuildingBlock> BlocksRemoveAll = new ListHashSet<BuildingBlock>();
            foreach (var Block in buildingBlock.GetBuildingPrivilege().GetBuilding().buildingBlocks)
                if (!BlocksRemoveAll.Contains(Block))
                     BlocksRemoveAll.Add(Block);


            NextTick(() =>
            {
                foreach (var Block in BlocksRemoveAll)
                    Block.Kill();
            });
        }

        public string RemoveErrorParseBuilding(BasePlayer player, BaseEntity buildingBlock)
        {
            var Remove = config.RemoveSetting;
            var Block = Remove.SettingsBlock;
            var RemoveTimed = Remove.TimedSetting;

            if(buildingBlock.OwnerID == 0)
                return GetLang("REMOVE_ONLY_FRIENDS", player.UserIDString);

            if (Block.NoEscape && IsRaidBlocked(player))
                return GetLang("REMOVE_NO_ESCAPE", player.UserIDString);

            BuildingBlock buildingBlocks = buildingBlock.GetComponent<BuildingBlock>();
            if (buildingBlocks != null)
            {
                if (buildingBlocks.SecondsSinceAttacked < 30)
                    return GetLang("REMOVE_ATTACKED_BLOCK", player.UserIDString, FormatTime(TimeSpan.FromSeconds(30 - (int)buildingBlocks.SecondsSinceAttacked)));
            }

            if (Block.Friends && player.userID != buildingBlock.OwnerID)
            {
                if (!IsFriends(player.userID, buildingBlock.OwnerID))
                    return GetLang("REMOVE_ONLY_FRIENDS", player.UserIDString);
                else if (player.IsBuildingBlocked())
                    return GetLang("REMOVE_NO_AUTH", player.UserIDString);
            }
            else if (player.IsBuildingBlocked())
                return GetLang("REMOVE_NO_AUTH", player.UserIDString);

            if (RemoveTimed.UseTimesBlock && IsBlockBuild(buildingBlock.net.ID))
                return GetLang("REMOVE_TIME_EXECUTE", player.UserIDString, FormatTime(TimeSpan.FromSeconds(GetTimerBlock(buildingBlock.net.ID))));

            if (RemoveTimed.UseAllBlock && IsBlockBuildPemanent(buildingBlock.net.ID))
                return GetLang("REMOVE_TIME_EXECUTE_UNREMOVE", player.UserIDString);

            if (Block.ShortnameNoteReturned.Contains(Regex.Replace(buildingBlock.ShortPrefabName.Replace("mining_quarry", "mining.quarry"), "\\.deployed|_deployed", "")))
                return GetLang("REMOVE_UNREMOVE", player.UserIDString);

            return "";
        }

        public void RemoveBuilding(BasePlayer player, BaseEntity buildingBlock)
        {
            String Alert = RemoveErrorParseBuilding(player, buildingBlock);
            if (!String.IsNullOrWhiteSpace(Alert))
            {
                Interface_Error(player, Alert);
                return;
            }

            ReturnedRemoveItems(player, buildingBlock);

            NextTick(() => {
                {
                    StorageContainer container = buildingBlock.GetComponent<StorageContainer>();
                    if (container != null) 
                        container.DropItems();

                    buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            });
            DataPlayer[player.userID].ActiveTime = (int)(config.RemoveSetting.RemoveTime + CurrentTime());
        }
        #endregion

        #region Commands

        [ConsoleCommand("up")]
        void UPCommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var Data = DataPlayer[player.userID];

            Int32 GradeLevel;
            if (arg?.Args != null && Int32.TryParse(arg.Args[0], out GradeLevel))
            {
                if (GradeLevel < 0 || GradeLevel > 4)
                    return;

                Data.GradeUP(player, true, GradeLevel);
            }
            else Data.GradeUP(player);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }

        [ConsoleCommand("building.upgrade")]
        void BUpgradeCommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var Data = DataPlayer[player.userID];

            Int32 GradeLevel;
            if (arg?.Args != null && Int32.TryParse(arg.Args[0], out GradeLevel))
            {
                if (GradeLevel < 0 || GradeLevel > 4)
                    return;

                Data.GradeUP(player, true, GradeLevel);
            }
            else Data.GradeUP(player);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }

        [ConsoleCommand("remove")]
        void RemoveCommandConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var Data = DataPlayer[player.userID];
            if (Data.GradeLevel == 5)
                Data.GradeUP(player, true, 0);
            else Data.GradeUP(player, true, 5);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);
        }

        [ChatCommand("up")]
        void UPCommand(BasePlayer player, string cmd, string[] arg)
        {
            var Data = DataPlayer[player.userID];
            switch (arg.Length)
            {
                case 0:
                    {
                        Data.GradeUP(player);
                        break;
                    }
                case 1:
                    {
                        Int32 GradeLevel;
                        if (!int.TryParse(arg[0], out GradeLevel) || GradeLevel < 0 || GradeLevel > 4)
                            return;
                        
                        Data.GradeUP(player, true, GradeLevel);
                        break;
                    }
            }
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }
        
        [ChatCommand("bgrade")]
        void BGradeChatCommand(BasePlayer player, string cmd, string[] arg)
        {
            var Data = DataPlayer[player.userID];
            switch (arg.Length)
            {
                case 0:
                    {
                        Data.GradeUP(player);
                        break;
                    }
                case 1:
                    {
                        int GradeLevel;
                        if (!int.TryParse(arg[0], out GradeLevel) || GradeLevel < 0 || GradeLevel > 4)
                            return;
                        
                        Data.GradeUP(player, true, GradeLevel);
                        break;
                    }
            }
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);

            if (Data.GradeLevel != 0)
                UpdateButton_Upgrade(player);
            else CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
        }
        [ChatCommand("remove")]
        void RemoveCommand(BasePlayer player, string cmd, string[] arg)
        {
            var Data = DataPlayer[player.userID];
            if (Data.GradeLevel == 5)
                Data.GradeUP(player, true, 0);
            else Data.GradeUP(player, true, 5);

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                Interface_New_Main(player);

            GradeRemove_Status(player);
        }
   

        [ConsoleCommand("gr.func.turned")] 
        void UI_ConsoleCommandAdminMenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var Data = DataPlayer[player.userID];

            if (Data.GradeAllObject)
                Data.GradeAllObject = false;
            else Data.GradeAllObject = true;

            GradeRemove_AllObject_Turned(player);
        }
        #endregion

        #region Hooks
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || info.HitEntity == null) return;
            var Data = DataPlayer[player.userID];

            if (Data.GradeLevel == 5)
                if (info.HitEntity is BaseEntity)
                    RemoveBuilding(player, info.HitEntity);

            BuildingBlock buildingBlock = info.HitEntity as BuildingBlock;
            if (buildingBlock == null) return;

            if (Data.GradeAllObject && Data.GradeLevel > 0 && Data.GradeLevel != 5)
            {
                GradeAll(player, buildingBlock);
                return;
            }
            if(Data.GradeAllObject && Data.GradeLevel == 5)
            {
                RemoveAll(player, buildingBlock);
                return;
            }

            if (Data.GradeLevel != 0 && Data.GradeLevel != 5)
            {
                if (buildingBlock.grade == (BuildingGrade.Enum)Data.GradeLevel)
                    return;
                
                GradeBuilding(player, buildingBlock);
            }
        }
        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            String Alert = GradeErrorParse(player, entity.GetComponent<BuildingBlock>(), grade);
            if (!String.IsNullOrWhiteSpace(Alert)) // grade
            {
                Interface_Error(player, Alert);
                return false;
            }
            return null;
        }
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null || go == null) return;

            if (go.ToBaseEntity() is BuildingBlock || go.ToBaseEntity() is BaseEntity)
            {
                AddRemoveBlockBuild(go.ToBaseEntity().net.ID, player.userID);
                AddBlockBuild(go.ToBaseEntity().net.ID, player.userID);
            }

            BuildingBlock buildingBlock = go.ToBaseEntity().GetComponent<BuildingBlock>();
            if (buildingBlock == null) return;
            var Data = DataPlayer[player.userID];
            if (Data == null) return;
           // if (Data.GradeLevel == 6 || Data.GradeLevel == 7) return;

            if (Data.GradeLevel != 0 && Data.GradeLevel != 5)
                GradeBuilding(player, buildingBlock);
        }
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || player.userID < 2147483647) return;
            CuiHelper.DestroyUi(player, GradeRemoveOverlay);
        }
        #region Server Hooks
        public static IQGradeRemove inst;
        void Init() => ReadData();
        private void OnServerInitialized()
        {
            inst = this;
            foreach (var p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);
            RegisteredPermissions();
        }
        void OnPlayerConnected(BasePlayer player) => RegisteredUser(player);
        void Unload()
        {
            WriteData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, GradeRemoveOverlay);
                if (player.HasFlag(BaseEntity.Flags.Reserved10))
                    player.SetFlag(BaseEntity.Flags.Reserved10, false);
            }
        }
        void OnNewSave(string filename)
        {
            BuildingRemoveTimers.Clear();
            BuildingRemoveBlock.Clear();
            WriteData();
        }
        #endregion

        #endregion

        #region UI
        public static String GradeRemoveOverlay = "GradeRemoveOverlay";

        [ChatCommand("grade")]
        void Interface_New_Main(BasePlayer player)
        {
            var Data = DataPlayer[player.userID];
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            Data.RebootTimer();

            if (player.HasFlag(BaseEntity.Flags.Reserved10))
            {
                Data.GradeLevel = 0;
                CuiHelper.DestroyUi(player, GradeRemoveOverlay);
                player.SetFlag(BaseEntity.Flags.Reserved10, false);
                return;
            }
            player.SetFlag(BaseEntity.Flags.Reserved10, true);

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = $"0.7075472 0.703154 0.5640352 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "16.19 19.273", OffsetMax = "396.002 78.727" }
            }, "Overlay", GradeRemoveOverlay);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = HexToRustFormat(MainInterface.ColorPanel) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0.092 0.274", OffsetMax = "190.02047 30" }  
            }, GradeRemoveOverlay, "InformationPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = HexToRustFormat(MainInterface.ColorText) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "159.871 1.976", OffsetMax = "-29.36 -1.9762047" }  
            }, "InformationPanel", "LinePanel");

            if (Interface.MainInterfaces.ShowCloseButton)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "chat.say /grade" },
                    Text = { Text = Interface.MainInterfaces.SymbolCloseButton, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "191.404 -29.453", OffsetMax = "210.07 0.273" }
                }, GradeRemoveOverlay, "CloseUI");
            }

            CuiHelper.DestroyUi(player, "GradeRemoveOverlay");
            CuiHelper.AddUi(player, container);

            GradeRemove_Status(player);
            GradeRemove_AllObject_Turned(player);
        }
        void Update_Take_Button_UP(BasePlayer player)
        {
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                return;

            CuiHelper.DestroyUi(player, "ButtonUpgrade");
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var container = new CuiElementContainer();
            var Data = DataPlayer[player.userID];

            String ColorButton = Data.GradeLevel != 0 && Data.GradeLevel <= 4 ? "#373737" : MainInterface.ColorPanel;

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(ColorButton), Command = "chat.say /up" },
                Text = { Text = GetLang("TITLE_TAKE_UP", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "193.415 0.273", OffsetMax = "284.785 29.999" }
            }, "GradeRemoveOverlay", "ButtonUpgrade");

            CuiHelper.AddUi(player, container);
        }
        void Update_Take_Button_REMOVE(BasePlayer player)
        {
            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                return;
            CuiHelper.DestroyUi(player, "ButtonRemove");
            CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");

            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var container = new CuiElementContainer();
            var Data = DataPlayer[player.userID];

            String ColorButton = Data.GradeLevel > 4 ? "#373737" : MainInterface.ColorPanel;

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(ColorButton), Command = "chat.say /remove" },
                Text = { Text = GetLang("TITLE_TAKE_REMOVE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "288.535 0.273", OffsetMax = "379.905 29.999" }
            }, "GradeRemoveOverlay", "ButtonRemove");

            CuiHelper.AddUi(player, container);
        }

        void GradeRemove_Status(BasePlayer player)
        {
            UpdateLabelStatus(player);
            Update_Take_Button_UP(player);
            Update_Take_Button_REMOVE(player);
        }

        void GradeRemove_AllObject_Turned(BasePlayer player)
        {
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var Data = DataPlayer[player.userID];

            CuiHelper.DestroyUi(player, $"AdminButtonDeleteAll");

            CuiElementContainer container = new CuiElementContainer();

            if (permission.UserHasPermission(player.UserIDString, PermissionGRMenu))
            {
                String ColorButton = Data.GradeAllObject ? "#373737" : MainInterface.ColorPanel;

                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(ColorButton), Command = "gr.func.turned" },
                    Text = { Text = GetLang("TITLE_GR_ADMIN_ALL_OBJ", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "193.415 31.363", OffsetMax = "379.905 59.727" }
                }, "GradeRemoveOverlay", "AdminButtonDeleteAll");
            }

            CuiHelper.AddUi(player, container);
        }
        void UpdateButton_Upgrade(BasePlayer player)
        {
            if (!config.InterfaceSetting.MainInterfaces.ShowLevelUp) return;

            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                return;

            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var Data = DataPlayer[player.userID];
            
            CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2196079 0.2196079 0.2196079 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0.091 31.363", OffsetMax = "190.001 59.727" }
            }, GradeRemoveOverlay, "UpgradeButtonStatus");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 1" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-145.386 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLevelOne");

            String HexGradeWood = Data.GradeLevel == 1 ? "#F3F3F3" : "#373737";
            String SpriteCheckWood = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[1]) ? "assets/icons/level_wood.png" : "assets/icons/occupied.png" : "assets/icons/level_wood.png";
            String AnchorMinWood = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[1]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxWood = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[1]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9"; 

            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeWood), Sprite = SpriteCheckWood },
                RectTransform = { AnchorMin = AnchorMinWood, AnchorMax = AnchorMaxWood }
            }, "ButtonUpgradeLevelOne", "SpriteLevelOne");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 2" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "48.993 0", OffsetMax = "-96.393 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLeveTwo");

            String HexGradeStone = Data.GradeLevel == 2 ? "#F3F3F3" : "#373737";
            String SpriteCheckStone = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[2]) ? "assets/icons/level_stone.png" : "assets/icons/occupied.png" : "assets/icons/level_stone.png";
            String AnchorMinStone = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[2]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxStone = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[2]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9";
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeStone), Sprite = SpriteCheckStone },
                RectTransform = { AnchorMin = AnchorMinStone, AnchorMax = AnchorMaxStone }
            }, "ButtonUpgradeLeveTwo", "SpriteLevelTwo");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 3" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "96.893 0", OffsetMax = "-48.493 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLevelThree");

            String HexGradeMetal = Data.GradeLevel == 3 ? "#F3F3F3" : "#373737";
            String SpriteCheckMetal = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[3]) ? "assets/icons/level_metal.png" : "assets/icons/occupied.png" : "assets/icons/level_metal.png";
            String AnchorMinMetal = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[3]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxMetal = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[3]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9";
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeMetal), Sprite = SpriteCheckMetal },
                RectTransform = { AnchorMin = AnchorMinMetal, AnchorMax = AnchorMaxMetal }
            }, "ButtonUpgradeLevelThree", "SpriteLevelThree");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(MainInterface.ColorPanel), Command = "up 4" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "145.386 0", OffsetMax = "0 0.364" }
            }, "UpgradeButtonStatus", "ButtonUpgradeLevelFo");

            String HexGradeTop = Data.GradeLevel == 4 ? "#F3F3F3" : "#373737";
            String SpriteCheckTop = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[4]) ? "assets/icons/level_top.png" : "assets /icons/occupied.png" : "assets/icons/level_top.png";
            String AnchorMinTop = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[4]) ? "0.2 0.1" : "0.25 0.15" : "0.2 0.1";
            String AnchorMaxTop = config.UsePermission ? permission.UserHasPermission(player.UserIDString, PermissionsLevel[4]) ? "0.8 0.9" : "0.75 0.85" : "0.8 0.9";
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(HexGradeTop), Sprite = SpriteCheckTop },
                RectTransform = { AnchorMin = AnchorMinTop, AnchorMax = AnchorMaxTop }
            }, "ButtonUpgradeLevelFo", "SpriteLevelFo");

            CuiHelper.AddUi(player, container);
        }

        void UpdateLabelStatus(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SpriteStatus");
            CuiHelper.DestroyUi(player, "LabelStatus");
            var Interface = config.InterfaceSetting;
            var MainInterface = Interface.MainInterfaces;
            var Data = DataPlayer[player.userID];

            if (Data.ActiveTime - CurrentTime() <= 1 && Data.GradeLevel != 0)
            {
                Data.GradeLevel = 0;
                if (Interface.MainInterfaces.HideMenuTimer)
                {
                    Data.RebootTimer();
                    CuiHelper.DestroyUi(player, GradeRemoveOverlay);
                    player.SetFlag(BaseEntity.Flags.Reserved10, false);
                }
                else
                {
                    CuiHelper.DestroyUi(player, $"UpgradeButtonStatus");
                    GradeRemove_Status(player);
                }
                return;
            }

            CuiElementContainer container = new CuiElementContainer();
            String SpriteStatus = Data.GradeLevel == 0 ? "assets/icons/loading.png" : Data.GradeLevel != 0 && Data.GradeLevel <= 4 ? "assets/icons/upgrade.png" : "assets/icons/level_stone.png";

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = HexToRustFormat(MainInterface.ColorText), Sprite = SpriteStatus },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "162.344 3.414", OffsetMax = "-4.744 -3.414" }
            }, "InformationPanel", "SpriteStatus");

            String LangStatus = Data.GradeLevel == 0 ? GetLang("TITLE_GR_ADMIN", player.UserIDString) : Data.GradeLevel != 0 && Data.GradeLevel <= 4 ?
            GetLang("GRADE_TITLE", player.UserIDString, StatusLevels[Data.GradeLevel], FormatTime(TimeSpan.FromSeconds(Data.ActiveTime - CurrentTime()))) :
            GetLang("REMOVE_TITLE", player.UserIDString, FormatTime(TimeSpan.FromSeconds(Data.ActiveTime - CurrentTime())));

            container.Add(new CuiElement
            {
                Name = "LabelStatus",
                Parent = "InformationPanel",
                Components = {
                    new CuiTextComponent { Text = LangStatus, Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(MainInterface.ColorText) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0.044 0.137", OffsetMax = "-32.266 -0.136" }
                }
            });

            CuiHelper.AddUi(player, container);

            Data.RebootTimer();
            if (Data.GradeLevel == 0)
                return;
            Data.TimerEvent = timer.Once(1f, () => UpdateLabelStatus(player));
        }

        void Interface_Error(BasePlayer player, String Message)
        {
            player.SendConsoleCommand("gametip.showtoast", new object[] 
            {
                "1",
                Message
            });
            Effect.server.Run("assets/bundled/prefabs/fx/invite_notice.prefab", player.GetNetworkPosition());
        }

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion

        #region Lang

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_GR_ADMIN"] = "<b><size=12>CHOOSE AVAILABLE MODE</size></b>",
                ["TITLE_TAKE_UP"] = "<b><size=12>UPGRADE</size></b>",
                ["TITLE_GR_ADMIN_ALL_OBJ"] = "<b><size=12>ВСЕ ПРИВЯЗАННЫЕ ОБЪЕКТЫ</size></b>",
                ["TITLE_TAKE_REMOVE"] = "<b><size=12>REMOVE</size></b>",
                ["GR_REMOVE_ALL_USE"] = "<b><size=10>REMOVE ALL</size></b>",
                ["GR_UP_ALL_USE"] = "<b><size=10>UP ALL</size></b>",

                ["REMOVE_TITLE"] = "<b><size=10>REMOVE ITEMS : {0}</size></b>",
                ["REMOVE_NO_ESCAPE"] = "<b><size=10>DO NOT REMOVE BUILDINGS DURING THE RAID</size></b>",
                ["REMOVE_NO_AUTH"] = "<b><size=10>DO NOT REMOVE OTHER BUILDINGS</size></b>",
                ["REMOVE_ATTACKED_BLOCK"] = "<b><size=11>CAN BE DELETED THROUG {0}</size></b>",
                ["REMOVE_TIME_EXECUTE"] = "<b><size=10>YOU CAN REMOVE AN OBJECT THROUGH : {0}</size></b>",
                ["REMOVE_TIME_EXECUTE_UNREMOVE"] = "<b><size=10>YOU CAN'T REMOVE IT MORE</size></b>",
                ["REMOVE_UNREMOVE"] = "<b><size=12>YOU CAN'T REMOVE</size></b>",
                ["REMOVE_ALL_UNDO"] = "<b><size=10>UNDO</size></b>",
                ["REMOVE_ONLY_FRIENDS"] = "<b><size=10>YOU CAN REMOVE FRIENDS ONLY</size></b>",

                ["GRADE_TITLE"] = "<b><size=10>UPDATE {0}</size></b>",
                ["GRADE_NO_ESCAPE"] = "<b><size=10>CANNOT IMPROVE BUILDINGS DURING THE RAID</size></b>",
                ["GRADE_NO_AUTH"] = "<b><size=10>DO NOT IMPROVE DEVELOPMENTS IN ANOTHER'S TERRITORY</size></b>",
                ["GRADE_ATTACKED_BLOCK"] = "<b><size=11>YOU CAN IMPROVE THROUGH {0}</size></b>",
                ["GRADE_NO_RESOURCE"] = "<b><size=11>NOT ENOUGH RESOURCES FOR IMPROVEMENT</size></b>",
                ["GRADE_NO_THIS_USER"] = "<b><size=11>A PLAYER IS IN THE CONSTRUCTION</size></b>",
                ["GRADE_ALL_NO_AUTH"] = "<b><size=10>IT IS IMPOSSIBLE TO IMPROVE EVERYTHING WITHOUT A CABINET</size></b>",
                ["REMOVE_ALL_NO_AUTH"] = "<b><size=10>DO NOT REMOVE ALL WITHOUT CABINET</size></b>",

                ["NO_PERM_GRADE_REMOVE"] = "<b><size=10>YOU HAVE NO RIGHT TO DO THIS</size></b>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_GR_ADMIN"] = "<b><size=12>ВЫБЕРИТЕ ДОСТУПНЫЙ РЕЖИМ</size></b>",
                ["TITLE_GR_ADMIN_ALL_OBJ"] = "<b><size=12>ВСЕ ПРИВЯЗАННЫЕ ОБЪЕКТЫ</size></b>",
                ["TITLE_TAKE_UP"] = "<b><size=12>УЛУЧШЕНИЕ</size></b>",
                ["TITLE_TAKE_REMOVE"] = "<b><size=12>УДАЛЕНИЕ</size></b>",
                ["GR_UP_ALL_USE"] = "<b><size=10>УЛУЧШЕНИЯ ВСЕХ ОБЪЕКТОВ</size></b>",

                ["REMOVE_TITLE"] = "<b><size=10>УДАЛЕНИЕ ПОСТРОЕК : {0}</size></b>", 
                ["REMOVE_NO_ESCAPE"] = "<b><size=10>НЕЛЬЗЯ УДАЛЯТЬ ПОСТРОЙКИ ВО ВРЕМЯ РЕЙДА</size></b>",
                ["REMOVE_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УДАЛЯТЬ ЧУЖИЕ ПОСТРОЙКИ</size></b>",
                ["REMOVE_ATTACKED_BLOCK"] = "<b><size=11>УДАЛИТЬ МОЖНО БУДЕТ ЧЕРЕЗ {0}</size></b>",
                ["REMOVE_TIME_EXECUTE"] = "<b><size=10>ВЫ СМОЖЕТЕ УДАЛИТЬ ОБЪЕКТ ЧЕРЕЗ : {0}</size></b>",
                ["REMOVE_TIME_EXECUTE_UNREMOVE"] = "<b><size=10>ВЫ БОЛЬШЕ НЕ МОЖЕТЕ УДАЛИТЬ ЭТОТ ОБЪЕКТ</size></b>",
                ["REMOVE_UNREMOVE"] = "<b><size=12>ВЫ НЕ МОЖЕТЕ УДАЛИТЬ ЭТОТ ОБЪЕКТ</size></b>",
                ["REMOVE_ALL"] = "<b><size=10>ВКЛЮЧЕНО УДАЛЕНИЯ ВСЕХ ОБЪЕКТОВ</size></b>",
                ["REMOVE_ALL_UNDO"] = "<b><size=10>ВЕРНУТЬ</size></b>",
                ["REMOVE_ALL_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УДАЛИТЬ ВСЕ БЕЗ ШКАФА</size></b>",
                ["REMOVE_ONLY_FRIENDS"] = "<b><size=10>ВЫ МОЖЕТЕ УДАЛЯТЬ ТОЛЬКО ПОСТРОЙКИ ДРУЗЕЙ</size></b>",

                ["GRADE_TITLE"] = "<b><size=10>УЛУЧШЕНИЕ ДО {0} : {1}</size></b>",
                ["GRADE_NO_ESCAPE"] = "<b><size=10>НЕЛЬЗЯ УЛУЧШАТЬ ПОСТРОЙКИ ВО ВРЕМЯ РЕЙДА</size></b>",
                ["GRADE_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УЛУЧШАТЬ ПОСТРОЙКИ НА ЧУЖОЙ ТЕРРИТОРИИ</size></b>",
                ["GRADE_ATTACKED_BLOCK"] = "<b><size=11>УЛУЧШИТЬ МОЖНО БУДЕТ ЧЕРЕЗ {0}</size></b>",
                ["GRADE_NO_RESOURCE"] = "<b><size=11>НЕДОСТАТОЧНО РЕСУРСОВ ДЛЯ УЛУЧШЕНИЯ</size></b>",
                ["GRADE_NO_THIS_USER"] = "<b><size=11>В ПОСТРОЙКЕ НАХОДИТСЯ ПРЕДМЕТ</size></b>",
                ["GRADE_ALL_NO_AUTH"] = "<b><size=10>НЕЛЬЗЯ УЛУЧШИТЬ ВСЕ БЕЗ ШКАФА</size></b>",

                ["NO_PERM_GRADE_REMOVE"] = "<b><size=10>У ВАС НЕТ ПРАВ ДЛЯ ЭТОГО</size></b>",

            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion
    }
}
