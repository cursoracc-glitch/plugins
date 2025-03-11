using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("Teleport", "unknown", "1.1.40")]
    class Teleport : RustPlugin
    {
        #region References
        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin Friends;
        [PluginReference]
        Plugin NoEscape;
        [PluginReference]
        Plugin Duel;
        #endregion

        #region Variables
        private TeleportConfig m_Config;
        private Dictionary<int, TeleportObject> m_TeleportToPlayerWaiters;
        private Dictionary<string, PlayerPluginData> m_ActivePlayers;
        private Dictionary<int, HomeTeleportObject> m_TeleportToHomeWaiters;
        #endregion

        #region Configuration
        public enum Settings
        {
            SaveHomeLimit,
            TeleportToHomeLimit,
            TeleportToPlayerLimit,
            WaitToTeleportTime,
            WaitToHomeTeleportTime,
            WaitAfterTeleport,
            WaitAfterTeleportHome
        }
        private class TeleportConfig
        {
            [JsonProperty("Префикс плагина для отображения в чате")]
            public string PluginPrefix { get; set; }

            [JsonProperty("Включить префикс плагина ?")]
            public bool EnablePluginPrefix { get; set; }

            [JsonProperty("Включить вывод информации о привилегиях в консоль ?")]
            public bool EnablePrintPermissionsInfo { get; set; }

            [JsonProperty("Включить GUI в плагине ?")]
            public bool EnableGUI { get; set; }
            
            [JsonProperty("Включить логирование использования команд администраторами ?")]
            public bool EnableAdminCommandsLogging { get; set; }

            [JsonProperty("Файл логирования команд администраторами")]
            public string AdminCommandsLoggingFile { get; set; }

            [JsonProperty("Включить логирование использования команд игроками ?")]
            public bool EnableUsersCommandsLogging { get; set; }

            [JsonProperty("Файл логирования команд игроками")]
            public string UserCommandsLoggingFile { get; set; }

            [JsonProperty("Включить систему домов ?")]
            public bool AllowHomeSystem { get; set; }

            [JsonProperty("Включить систему телепортации ?")]
            public bool AllowTeleportSystem { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время блока NoEscape ?")]
            public bool AllowTeleportInEscapeBlock { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время рейда ?")]
            public bool AllowTeleportInRaidBlock { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время боя ?")]
            public bool AllowTeleportInCombatBlock { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта в воде ?")]
            public bool AllowTeleportInWater { get; set; }

            [JsonProperty("Уровень воды для отмены телепорта")]
            public double CancelTeleportWaterFactor { get; set; }

            [JsonProperty("Разрешить телепортироваться в ванише ?")]
            public bool AllowTeleportInVanish { get; set; }

            [JsonProperty("Разрешить телепорт из РТ ?")]
            public bool AllowTeleportInRT { get; set; }

            [JsonProperty("Дальность от РТ для проверки")]
            public float ToRTDistance { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время крафта ?")]
            public bool AllowTeleportInCraft { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта при ваунде ?")]
            public bool AllowTeleportInWounded { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта после смерти ?")]
            public bool AllowTeleportAfterDeath { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время действия радиации ?")]
            public bool AllowTeleportInRadiation { get; set; }

            [JsonProperty("Уровень радиации для отмены телепорта")]
            public int CancelTeleportRadiationFactor { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время действия кровотечения ?")]
            public bool AllowTeleportInBleeding { get; set; }

            [JsonProperty("Уровень кровотечения для отмены телепорта")]
            public int CancelTeleportBleedingFactor { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта при получении урона ?")]
            public bool AllowTeleportAfterDamage { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта во время охлаждения ?")]
            public bool AllowTeleportInFreezing { get; set; }

            [JsonProperty("Уровень холода для отмены телепорта")]
            public int CancelTeleportFreezeFactor { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта в зоне действия чужого шкафа ?")]
            public bool AllowTeleportInBBZone { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта только друзьям ?")]
            public bool AllowTeleportOnlyFriends { get; set; }

            [JsonProperty("Разрешить использовать систему телепорта только соклановцам ?")]
            public bool AllowTeleportOnlyClan { get; set; }

            [JsonProperty("Разрешить телепорт при получении урона ?")]
            public bool AllowTeleportAfterRCVDamage { get; set; }

            [JsonProperty("Разрешить сохранять дом только на фундаменте ?")]
            public bool AllowSaveHomeOnlyFundament { get; set; }

            [JsonProperty("Создавать спальный мешок после сохранения дома ?")]
            public bool CreateBagAfterHomeSave { get; set; }

            [JsonProperty("Разрешить сохранять дом в зоне действия чужого шкафа ?")]
            public bool AllowSaveHomeOnBB { get; set; }

            [JsonProperty("Разрешить сохранять дом только на фундаменте соклановцев ?")]
            public bool AllowSaveHomeOnClanFundament { get; set; }

            [JsonProperty("Разрешить сохранять дом только на фундаменте друзей ?")]
            public bool AllowSaveHomeOnFriendFundament { get; set; }

            [JsonProperty("Время ожидания телепорта по умолчанию")]
            public int DefaultTimeToTeleport { get; set; }

            [JsonProperty("Время ожидания телепорта домой по умолчанию")]
            public int DefaultTimeToTeleportHome { get; set; }

            [JsonProperty("Время восстановление телепорта по умолчанию")]
            public int DefaultRemainingAfterTeleport { get; set; }

            [JsonProperty("Время восстановления телепорта домой по умолчанию")]
            public int DefaultRemainingAfterHomeTeleport { get; set; }

            [JsonProperty("Количество сохраняемых домов по умолчанию")]
            public int DefaultSaveHomeLimit { get; set; }

            [JsonProperty("Количество телепортов к игроку в сутки ?")]
            public int DefaultTTPOneDay { get; set; }

            [JsonProperty("Количество телепортов домой в сутки ?")]
            public int DefaultTTHOneDay { get; set; }

            [JsonProperty("Время, через которое запрос будет отменен автоматически, если игрок на него не ответил")]
            public int DefaultRequestCancelledTime { get; set; }

            [JsonProperty("Время, через которое лимиты будут обновлены автоматически (По умолчанию = 24 часа)")]
            public int DefaultLimitsUpdated { get; set; }

            [JsonProperty("Название уведомления о телепортации. (По умолчанию: ЗАПРОС:)")]
            public string DefaultHeaderTeleportNotify { get; set; }

            [JsonProperty("Сообщение о запросе на телепорт. (По умолчанию {0} хочет телепортироваться к Вам)")]
            public string DefaultFromTeleportNotify { get; set; }

            [JsonProperty("Включить звуковое уведомление")]
            public bool EnableSoundEffects { get; set; }

            [JsonProperty("Кастомные пути к звуковому файлу, для воспроизведения. Если парааметр: 'Включить звуковое уведомление' включен")]
            public Dictionary<string, string> SoundsFotNotify { get; set; }

            [JsonProperty("Динамическая система привилегий. Указывайте настройки для каждой привилегии. Пример указан.")]
            public Dictionary<string, Dictionary<string, Dictionary<string, int>>> PermissionSettings { get; set; }

            public static TeleportConfig Prototype()
            {
                return new TeleportConfig()
                {
                    PluginPrefix = "[TP]",
                    EnableGUI = true,
                    AllowTeleportAfterRCVDamage = false,
                    DefaultHeaderTeleportNotify = "ЗАПРОС:",
                    DefaultFromTeleportNotify = "{0} хочет телепортироваться к Вам",
                    EnablePrintPermissionsInfo = true,
                    EnablePluginPrefix = true,
                    EnableAdminCommandsLogging = true,
                    AdminCommandsLoggingFile = "Admin_Teleports_log",
                    EnableUsersCommandsLogging = true,
                    UserCommandsLoggingFile = "User_Teleports_log",
                    AllowHomeSystem = true,
                    AllowTeleportInCombatBlock = false,
                    AllowTeleportInEscapeBlock = false,
                    AllowTeleportInRaidBlock = false,
                    AllowTeleportInVanish = true,
                    AllowTeleportInRT = false,
                    ToRTDistance = 150f,
                    CancelTeleportBleedingFactor = 5,
                    CancelTeleportFreezeFactor = 5,
                    CancelTeleportRadiationFactor = 5,
                    CancelTeleportWaterFactor = 0.2,
                    AllowTeleportSystem = true,
                    AllowTeleportInWounded = false,
                    AllowTeleportInWater = false,
                    AllowTeleportInCraft = false,
                    AllowTeleportAfterDeath = false,
                    AllowTeleportAfterDamage = false,
                    AllowTeleportInRadiation = false,
                    AllowTeleportInBleeding = false,
                    AllowTeleportInFreezing = false,
                    AllowTeleportInBBZone = false,
                    AllowTeleportOnlyFriends = false,
                    AllowTeleportOnlyClan = false,
                    AllowSaveHomeOnlyFundament = true,
                    CreateBagAfterHomeSave = true,
                    AllowSaveHomeOnBB = false,
                    AllowSaveHomeOnClanFundament = false,
                    AllowSaveHomeOnFriendFundament = false,
                    DefaultSaveHomeLimit = 3,
                    DefaultTimeToTeleport = 15,
                    DefaultTimeToTeleportHome = 15,
                    DefaultRemainingAfterTeleport = 120,
                    DefaultRemainingAfterHomeTeleport = 120,
                    DefaultTTPOneDay = 30,
                    DefaultTTHOneDay = 15,
                    DefaultRequestCancelledTime = 15,
                    DefaultLimitsUpdated = 86400,
                    EnableSoundEffects = true,
                    SoundsFotNotify = new Dictionary<string, string>()
                    {
                        ["tp_request"] = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                        ["tp_receive"] = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                        ["home_teleport"] = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                    },
                    PermissionSettings = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>()
                    {
                        ["teleport.vip"] = new Dictionary<string, Dictionary<string, int>>()
                        {
                            ["Settings"] = new Dictionary<string, int>()
                            {
                                ["SaveHomeLimit"]          = 3,
                                ["TeleportToHomeLimit"]    = 20,
                                ["TeleportToPlayerLimit"]  = 40,
                                ["WaitToTeleportTime"]     = 10,
                                ["WaitToHomeTeleportTime"] = 10,
                                ["WaitAfterTeleport"]      = 60,
                                ["WaitAfterTeleportHome"]  = 60
                            }
                        },
                        ["teleport.admin"] = new Dictionary<string, Dictionary<string, int>>()
                        {
                            ["Settings"] = new Dictionary<string, int>()
                            {
                                ["SaveHomeLimit"]          = 999,
                                ["TeleportToHomeLimit"]    = 999,
                                ["TeleportToPlayerLimit"]  = 999,
                                ["WaitToTeleportTime"]     = 1,
                                ["WaitToHomeTeleportTime"] = 1,
                                ["WaitAfterTeleport"]      = 1,
                                ["WaitAfterTeleportHome"]  = 1
                            }
                        }
                    }
                };
            }
        }

        protected override void LoadDefaultConfig()
        {
            m_Config = TeleportConfig.Prototype();

            PrintWarning("Creating default a configuration file ...");
            RegisterPermissions();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            m_Config = Config.ReadObject<TeleportConfig>();

            RegisterPermissions();
        }
        protected override void SaveConfig() => Config.WriteObject(m_Config);
        #endregion

        #region Localize
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["error_player_in_bleeding"] = "<color=#ba2c2c>Нельзя использовать телепорт если у вас кровотечение</color>",
                ["error_target_in_bleeding"] = "<color=#ba2c2c>У игрока, которому вы отправляете запрос, кровотечение</color>",
                ["error_player_in_freezing"] = "<color=#4acce2>Нельзя использовать телепорт, когда вы замерзаете</color>",
                ["error_target_in_freezing"] = "<color=#4acce2>Игрок, которому вы отправляете запрос, замерзает</color>",
                ["error_player_in_wounded"] = "<color=#d8e82e>Нельзя использовать телепорт в ваунде</color>",
                ["error_target_in_wounded"] = "<color=#d8e82e>Игрок, которому вы отправляете запрос в ваунде</color>",
                ["error_player_in_water"] = "<color=#0571ba>Нельзя использовать телепорт находясь в воде</color>",
                ["error_target_in_water"] = "<color=#0571ba>Игрок, которому вы отправляете запрос находится в воде</color>",
                ["error_player_in_radiation"] = "<color=#ddf21f>Нельзя использовать телепорт во время действия радиации</color>",
                ["error_target_in_radiation"] = "<color=#ddf21f>Игрок, которому вы отправляете запрос находится под воздействием радиации</color>",
                ["error_player_in_already_teleported"] = "<color=#f11f1f>Вы уже телепортируетесь</color>",
                ["error_target_in_already_teleported"] = "<color=#f11f1f>Игрок, которому вы отправляете запрос уже телепортируется</color>",
                ["error_player_in_bb"] = "<color=#f11f1f>Нельзя использовать телепорт в зоне действия чужого шкафа</color>",
                ["error_target_in_bb"] = "<color=#f11f1f>Игрок, которому вы отправляете запрос находится в зоне действия чужого шкафа</color>",
                ["error_ttp_cooldown"] = "<color=#f0761e>Невозможно отправить запрос. Попробуйте через {0} секунд</color>",
                ["info_ttp_cooldown_reseted"] = "<color=#49b50a>Теперь вы снова можете телепортироватся к другим</color>",
                ["error_tth_cooldown"] = "<color=#f0761e>Невозможно отправить запрос. Попробуйте через {0} секунд</color>",
                ["info_tth_cooldown_reseted"] = "<color=#49b50a>Теперь вы снова можете телепортироватся домой</color>",
                ["error_ttp_only_friend"] = "<color=#ba2c2c>Телепортироваться можно только к друзьям</color>",
                ["error_ttp_only_clan"] = "<color=#ba2c2c>Телепортироваться можно только к члену вашего клана</color>",
                ["error_ttp_incorrect_target"] = "<color=#ba2c2c>Запрашиваемый игрок не найден</color>",
                ["error_ttp_self"] = "<color=#ba2c2c>Серьезно? К самому себе?</color>",
                ["error_ttp_player_already_wait"] = "<color=#ba2c2c>Вы уже ожидаете телепортации</color>",
                ["error_ttp_target_already_wait"] = "<color=#ba2c2c>Ваша цель уже находится в очереди на телепортацию</color>",
                ["info_ttp_request_cancelled"] = "<color=#49b50a>Запрос на телепортацию был отменен</color>",
                ["info_ttp_request_notfound"] = "<color=#ba2c2c>Отсутствуют доступные запросы для совершения этого действия</color>",
                ["info_ttp_request_player_appended"] = "<color=#ffa500ff>Игрок '{0}' принял ваш запрос. Вы будете телепортированы через {1} секунд</color>",
                ["info_ttp_request_target_appended"] = "<color=#ffa500ff>Вы приняли запрос от игрока '{0}'. Он появится возле вас черех {1} секунд</color>",
                ["info_ttp_request_player_finished"] = "<color=#49b50a>Игрок '{0}' успешно телепортировался к вам</color>",
                ["info_ttp_request_target_finished"] = "<color=#49b50a>Вы успешно телепортированы к игроку '{0}'. Осталось телепортов: {1}</color>",
                ["info_ttp_request_player_created"] = "<color=#49b50a>Запрос на телепортацию к игроку '{0}' создан успешно. Ожидайте ответа</color>",
                ["info_ttp_request_target_created"] = "<color=#49b50a>Игрок '{0}' хочет к вам телепортироваться. Чтобы принять введите /tpa и /tpc чтобы отклонить. Он будет автоматически отклонен, если вы не ответите в течении 15 секунд</color>",
                ["whose_from_waiter_already_wait"] = "<color=#ba2c2c>Ктото из участников уже ожидает телепортации. Если это вы, то введите /tpc для отмены запроса</color>",
                ["error_command_sethome_incorrect"] = "<color=#ba2c2c>Неверный синтакисис. Используйте команду следующим образом /sethome [name]</color>",
                ["error_home_already_exists"] = "<color=#ba2c2c>Дом с таким именем уже существует</color>",
                ["error_max_home_limits"] = "<color=#ba2c2c>Вы установили максимальное число домов для себя</color>",
                ["info_ttp_limit_reseted"] = "<color=#49b50a>Лимит использования телепортов к игрокам для Вас обновлен !</color>",
                ["info_tth_limit_reseted"] = "<color=#49b50a>Лимит использования телепортов домой для Вас обновлен !</color>",
                ["error_ttp_unlimited"] = "<color=#ba2c2c>Лимит использований исчерпан. Попробуйте через: {0}ч. {1}м. {2}с.</color>",
                ["error_teleport_system_disabled"] = "<color=#ba2c2c>Система телепортации временно отключена</color>",
                ["error_player_received_damage"] = "<color=#ba2c2c>Вы получили урон</color>",
                ["error_target_received_damage"] = "<color=#ba2c2c>Ваша цель получила урон</color>",
                ["error_player_start_damage"] = "<color=#ba2c2c>Вы вступили в бой</color>",
                ["error_target_start_damage"] = "<color=#ba2c2c>Ваша цель вступила в бой</color>",
                ["error_player_is_death"] = "<color=#ba2c2c>Вы умерли</color>",
                ["error_target_is_death"] = "<color=#ba2c2c>Ваша цель умерла</color>",
                ["error_command_tpr_incorrect"] = "<color=#ba2c2c>Неправильное использование команды. Пример /tpr [nickname]</color>",
                ["error_tth_received_damage"] = "<color=#ba2c2c>Вы получили повреждения</color>",
                ["error_tth_start_damage"] = "<color=#ba2c2c>Вы вступили в бой</color>",
                ["error_tth_player_die"] = "<color=#ba2c2c>Вас убили</color>",
                ["info_home_deleted_by_demolish"] = "<color=#ba2c2c>Блок на котором стоял Ваш дом уничтожен. Дом был удален</color>",
                ["error_rename_other_bags"] = "<color=#ba2c2c>Нельзя переименовывать чужие дома</color>",
                ["error_ttp_cancelled_player_after_wounded"] = "<color=#ba2c2c>Вы получили критические ранения</color>",
                ["error_ttp_cancelled_target_after_wounded"] = "<color=#ba2c2c>Игрок, который к Вам телепортируется получил критические ранения</color>",
                ["error_tth_cancelled_after_wounded"] = "<color=#ba2c2c>Вы получили критические повреждения</color>",
                ["info_home_created"] = "<color=#49b50a>Дом: '{0}' успешно сохранен</color>",
                ["error_sethome_save_only_clan_foundation"] = "<color=#ba2c2c>Сохранять дом разрешено только на фундаменте членов клана</color>",
                ["error_sethome_save_only_friend_foundation"] = "<color=#ba2c2c>Сохранять дом разрешено только на фундаменте друзей</color>",
                ["error_home_system_disabled"] = "<color=#ba2c2c>Система домов временно недоступна. Попробуйте позже</color>",
                ["error_command_sethome_incorrect"] = "<color=#ba2c2c>Неверный синтаксис для команды. Пример: /sethome [homename]</color>",
                ["error_max_home_limits"] = "<color=#ba2c2c>Вы уже сохранили максимальное количество домов</color>",
                ["error_home_already_exists"] = "<color=#ba2c2c>Дом с таким названием уже существует</color>",
                ["error_sethome_only_foundation"] = "<color=#ba2c2c>Сохранять дома разрешено только на фундаменте !</color>",
                ["error_sethome_save_on_bb"] = "<color=#ba2c2c>Нельзя сохранять дом в зоне действия чужого шкафа</color>",
                ["error_command_removehome_incorrect"] = "<color=#ba2c2c>Некорректный синтаксис для команды. Пример: /removehome 1</color>",
                ["error_saved_home_notfound"] = "<color=#ba2c2c>Дом: '{0}' не найден. Проверьте корректность ввода</color>",
                ["error_home_killed_automatic_deleted"] = "<color=#ba2c2c>Этот дом был уничтожен и поэтому он был удален</color>",
                ["error_home_deleted_cancelled_teleport"] = "<color=#ba2c2c>Дом был удален</color>",
                ["info_home_deleted"] = "<color=#49b50a>Дом: '{0}' был успешно удален</color>",
                ["info_command_homelist"] = "<color=#f11f1f>Дом: '{0}'(ID:{1}). Позиция: {2}</color>",
                ["info_command_homelist_home_dexists"] = "<color=#49b50a>У вас нет сохраненных домов. Используйте /sethome [homename]</color>",
                ["error_coomand_tphome_incorrect"] = "<color=#ba2c2c>Неверный синтаксис. Пример: /home [homename]</color>",
                ["error_home_does_not_exists"] = "<color=#ba2c2c>Дома с именем: '{0}' не существует.</color>",
                ["info_home_deleted_other_bb"] = "<color=#ba2c2c>Этот дом Вам больше не принадлежит. Дом удален</color>",
                ["info_tth_request_created"] = "<color=#f11f1f>Запрос принят. Вы будете телепортированы домой через {0} секунд</color>",
                ["info_tth_already_wait"] = "<color=#ba2c2c>В данный момент вы уже ожидаете телепортации домой</color>",
                ["info_tth_request_finished"] = "<color=#49b50a>Вы были возвращены домой. Осталось возвращений: {0}</color>",
                ["info_tth_request_cancelled"] = "<color=#ba2c2c>Телепорт домой был отменен</color>",
                ["info_tth_request_notfound"] = "<color=#ba2c2c>Доступные запросы отсутствуют</color>",
                ["error_tth_unlimited"] = "<color=#ba2c2c>Лимит использований исчерпан. Попробуйте через: {0}ч. {1}м. {2}с.</color>",
                ["error_pickup_registered_home"] = "<color=#ba2c2c>Нельзя подобрать спальный мешок, привязанный к дому. Используйте /removehome [homename]</color>",
                ["error_pickup_other_bag"] = "<color=#ba2c2c>Нельзя подбирать чужие спальные мешки</color>",
                ["error_decline_assign_bed"] = "<color=#ba2c2c>Нельзя дарить спальные мешки привязанные к дому</color>",
                ["info_home_destroyed"] = "<color=#ba2c2c>Один из ваших домов был уничтожен</color>",
                ["error_player_in_rt"] = "<color=#ba2c2c>Нельзя телепортироваться в зоне РТ</color>",
                ["error_target_in_rt"] = "<color=#ba2c2c>Ваша цель находится в зоне РТ</color>",
                ["error_player_in_noescape"] = "<color=#ba2c2c>Вам запрещено сейчас телепортироваться</color>",
                ["error_target_in_noescape"] = "<color=#ba2c2c>Вашей цели запрещено сейчас телепортироваться</color>",
                ["error_player_in_raidblock"] = "<color=#ba2c2c>Запрещено использовать телепорт во время рейда</color>",
                ["error_target_in_raidblocked"] = "<color=#ba2c2c>Ваша цель учавствует в рейде, попробуйте позже</color>",
                ["error_player_in_combatblock"] = "<color=#ba2c2c>В бою запрещено использовать телепорт</color>",
                ["error_target_in_combatblock"] = "<color=#ba2c2c>Ваша цель в бою, попробуйте позже</color>",
                ["info_create_tth_remaining"] = "<color=#ba2c2c>Лимит на использование телепортов домой был полностью израсходован</color>",
                ["info_create_ttp_remaining"] = "<color=#ba2c2c>Лимит телепортов к игрокам был полностью израсходован</color>",
                ["incorrect_command_tp"] = "<color=#ba2c2c>Некорректное использование команды /tp.",
                ["error_player_isnt_admin"] = "<color=#ba2c2c>Для использования этой команды Вы должны обладать правами администратора</color>",
                ["error_parse_coords_failed_self"] = "<color=#ba2c2c>Не удалось распознать введенные координаты. Проверьте корректность ввода</color>",
                ["info_gui_enabled"] = "<color=#49b50a>Вы включили GUI уведомления</color>",
                ["info_gui_disabled"] = "<color=#ba2c2c>Вы отключили GUI уведомления</color>",
                ["info_all_enabled"] = "<color=#49b50a>Теперь все игроки могут отправить Вам запрос на телепортацию</color>",
                ["info_all_disabled"] = "<color=#ba2c2c>Вы недоступны для запроса на телепортацию для всех</color>",
                ["info_clan_enabled"] = "<color=#49b50a>Теперь соклановцы могут отправлять Вам запрос на телепортацию</color>",
                ["info_clan_disabled"] = "<color=#ba2c2c>Вы не доступны для запросов на телепортацию от соклановцев</color>",
                ["info_friend_enabled"] = "<color=#49b50a>Теперь друзья могут отправлять Вам запросы на телепортацию</color>",
                ["info_friend_disabled"] = "<color=#ba2c2c>Вы недоступны для запроса на телепортацию для друзей</color>",
                ["player_disabled_all_teleport"] = "<color=#ba2c2c>Игрок запретил отправлять ему запросы на телепортацию</color>",
                ["player_disabled_clan_teleport"] = "<color=#ba2c2c>Игрок запретил соклановцам телепортироваться к нему</color>",
                ["player_disabled_friend_teleport"] = "<color=#ba2c2c>Игрок запретил друзья телепортироваться к нему</color>",
                ["error_target_in_duel"] = "<color=#ba2c2c>В данный момент ваша цель принимает участие в дуэли. Попробуйте позже</color>",
                ["error_player_in_duel"] = "<color=#ba2c2c>Вы не можете использовать систему телепорта во время дуэли</color>",
            }, this);
        }
        #endregion

        #region Custom Structs
        public enum Remainings
        {
            TeleportToHome,
            TeleportToPlayer,
            NextTTPLimit,
            NextTTHLimit
        }
        private class PlayerPluginData
        {
            public string ID           { get; set; }
            public string Name         { get; set; }
            public int TTPLimit        { get; set; }
            public int TTHLimit        { get; set; }
            public bool GuiEnabled     { get; set; }
            public bool FriendsEnabled { get; set; }
            public bool ClanEnabled    { get; set; }
            public bool AllEnabled     { get; set; }

            public Dictionary<string, HomeObject> Homes   { get; set; }
            public TimerBase NextTeleportRemaining     { get; set; }
            public TimerBase NextHomeTeleportRemaining { get; set; }
            public TimerBase NextTTPLimitRemaining     { get; set; }
            public TimerBase NextTTHLimitRemaining     { get; set; }

            public PlayerPluginData() : this("-1", "-1", -1, -1, new Dictionary<string, HomeObject>(), new TimerBase(), new TimerBase(), new TimerBase(), new TimerBase()) { }
            public PlayerPluginData(string id, string name, int ttpl, int tthl, Dictionary<string, HomeObject> homes, TimerBase nextTR, TimerBase nextHTR, TimerBase nttp, TimerBase ntth)
            {
                ID = id;
                Name = name;
                Homes = homes;
                TTPLimit = ttpl;
                TTHLimit = tthl;
                NextTeleportRemaining = nextTR;
                NextHomeTeleportRemaining = nextHTR;
                NextTTPLimitRemaining = nttp;
                NextTTHLimitRemaining = ntth;
                GuiEnabled = true;
                FriendsEnabled = true;
                ClanEnabled = true;
                AllEnabled = true;
            }

            public void CreateRemaining(Remainings type, PluginTimers timer, int seconds, Action callback)
            {
                if(type == Remainings.TeleportToHome)
                {
                    NextHomeTeleportRemaining.Instantiate(timer, seconds, callback);
                }
                else if (type == Remainings.TeleportToPlayer)
                {
                    NextTeleportRemaining.Instantiate(timer, seconds, callback);
                }
                else if(type == Remainings.NextTTPLimit)
                {
                    NextTTPLimitRemaining.Instantiate(timer, seconds, callback);
                }
                else if(type == Remainings.NextTTHLimit)
                {
                    NextTTHLimitRemaining.Instantiate(timer, seconds, callback);
                }
                else
                {
                    return;
                }
            }
            public void CreateHome(int id, string name, BasePlayer player, int limit, bool createBag)
            {
                if(Homes.Count >= limit)
                {
                    return;
                }

                if(Homes.ContainsKey(name))
                {
                    return;
                }

                Homes.Add(name, new HomeObject(id, name, player.transform.position, player, createBag));
            }
            public void RestoreHome(string name, SleepingBag bag)
            {
                if(Homes.ContainsKey(name))
                {
                    Homes[name].Bag = bag;
                }
            }
            public void RemoveHome(string name)
            {
                if(!Homes.ContainsKey(name))
                {
                    return;
                }

                if (Homes[name].Bag != null)
                {
                    Homes[name].Bag.enabled = false;
                    Homes[name].Bag.Kill();
                    Homes[name].Bag = null;

                    Homes.Remove(name);
                }
            }

            public void PrepareToSave()
            {
                NextHomeTeleportRemaining.Destroy();
                NextTeleportRemaining.Destroy();
                NextTTPLimitRemaining.Destroy();
                NextTTHLimitRemaining.Destroy();

                foreach(var home in Homes)
                {
                    home.Value.Bag = null;
                }
            }
        }

        private class TeleportObject
        {
            public int ID                     { get; set; }
            public int Seconds                { get; set; }
            public bool Accepted              { get; set; }

            public PlayerPluginData Initiator { get; private set; }
            public PlayerPluginData Target    { get; private set; }
            public TimerBase        Remaining { get; private set; }

            public TeleportObject(PlayerPluginData init, PlayerPluginData target, int secs)
            {
                Initiator = init;
                Target    = target;
                Seconds   = secs;
                Remaining = new TimerBase();
            }

            public void Instantiate(PluginTimers timer, Action callback, Action onTick = null)
            {
                Remaining.Instantiate(timer, Seconds, callback, onTick);
            }
        } 
        private class HomeObject
        {
            public int     ID       { get; set; }
            public string  Name     { get; set; }
            public Vector3 Position { get; set; }
            public SleepingBag Bag  { get; set; }

            public HomeObject()
            {
                ID = -1;
                Name = "-1";
                Position = new Vector3();
                Bag = null;
            }
            public HomeObject(int id, string name, Vector3 pos, BasePlayer player, bool createBag)
            {
                ID = id;
                Name = name;
                Position = pos;

                if (createBag) Bag = CreateSleepingBag(player);
            }

            private SleepingBag CreateSleepingBag(BasePlayer player)
            {
                SleepingBag sleepingBag =
                GameManager.server.CreateEntity("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab", player.transform.position,
                    player.transform.localRotation) as SleepingBag;
                if (sleepingBag == null) return null;
                sleepingBag.transform.Rotate(player.transform.localEulerAngles);
                sleepingBag.deployerUserID = player.userID;
                sleepingBag.niceName = Name;
                sleepingBag.OwnerID = player.userID;
                sleepingBag.Spawn();

                return sleepingBag;
            }
        }
        private class HomeTeleportObject
        {
            public int ID { get; set; }
            public int Seconds { get; set; }
            public TimerBase Remaining { get; set; }
            public PlayerPluginData Owner { get; set; }
            public Vector3 Position { get; set; }

            public HomeTeleportObject(PlayerPluginData owner, int secs, Vector3 pos)
            {
                Owner = owner;
                Seconds = secs;
                Remaining = new TimerBase();
                Position = pos;
            }

            public void Instantiate(PluginTimers timer, Action callback, Action onTick = null)
            {
                Remaining.Instantiate(timer, Seconds, callback, onTick);
            }
        }

        private class TimerBase
        {
            public Timer  Object    { get; set; }
            public int    Remaining { get; set; }
            public bool   IsEnabled { get; set; }

            public TimerBase()
            {
                Remaining = 0;
                IsEnabled = false;
            }

            public void Instantiate(PluginTimers @object, int secs, Action callBackAction, Action onTick = null)
            {
                Object = @object.Repeat(1, secs, () =>
                {
                    secs--;
                    Remaining = secs;

                    if (onTick != null) onTick();

                    if (secs == 0)
                    {
                        IsEnabled = false;
                        callBackAction();
                    }
                    else IsEnabled = true;
                });
            }

            public void Destroy()
            {
                if (Object != null)
                {
                    Object.Destroy();
                    Object = null;
                }

                IsEnabled = false;
            }
        }
        #endregion

        #region Initialization
        public Teleport()
        {
            m_ActivePlayers           = new Dictionary<string, PlayerPluginData>();
            m_TeleportToPlayerWaiters = new Dictionary<int, TeleportObject>();
            m_TeleportToHomeWaiters   = new Dictionary<int, HomeTeleportObject>();
        }
        private void RegisterPermissions()
        {
            foreach(var perm in m_Config.PermissionSettings)
            {
                if(!permission.PermissionExists(perm.Key, this))
                {
                    permission.RegisterPermission(perm.Key, this);

                    if (m_Config.EnablePrintPermissionsInfo)
                    {
                        PrintWarning($"Create new permission: '{perm.Key}' with settings:");

                        foreach (var prefix in perm.Value)
                        {
                            foreach (var settings in prefix.Value)
                            {
                                PrintWarning($"[{settings.Key}] => {settings.Value}");
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Hooks
        void Loaded()
        {
            LoadAllPlayers();
        }

        /// <summary>
        /// Происходит при выгрузке плагина
        /// </summary>
        void Unload()
        {
            SaveAllPlayers();
        }

        /// <summary>
        /// Происходит при инициализации игрока
        /// </summary>
        /// <param name="player"></param>
        void OnPlayerInit(BasePlayer player)
        {
            LoadPlayer(player);
        }

        /// <summary>
        /// Происходит при отключении игрока от сервера
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(IsAlreadyWait(player, true) || IsAlreadyWait(player, false))
            {
                CancelTTPRequest(player.UserIDString);
            }

            if(IsWaitTeleportToHome(player))
            {
                CancelTTHRequest(player.UserIDString);
            }

            SavePlayer(player);
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null && info == null)
            {
                return;
            }

            BasePlayer victim = entity?.ToPlayer();
            BasePlayer attacker = info?.InitiatorPlayer;

            if (victim == null)
            {
                return;
            }

            if (attacker == null)
            {
                return;
            }

            if (m_Config.AllowTeleportAfterDamage)
            {
                return;
            }

            if (IsAlreadyWait(victim, true) || IsAlreadyWait(victim, false))
            {
                TeleportObject data = GetTeleportData(GetPlayerFromActive(victim.UserIDString));
                if (data == null)
                {
                    data = GetTeleportData(GetPlayerFromActive(victim.UserIDString), false);
                }

                if(victim.UserIDString == data.Initiator.ID)
                {
                    SendReply(FindPlayer(data.Initiator.ID), GetMessage("error_player_received_damage", this));
                    SendReply(FindPlayer(data.Target.ID), GetMessage("error_target_received_damage", this));
                }
                else
                {
                    SendReply(FindPlayer(data.Initiator.ID), GetMessage("error_target_received_damage", this));
                    SendReply(FindPlayer(data.Target.ID), GetMessage("error_player_received_damage", this));
                }

                CancelTTPRequest(victim.UserIDString);
            }

            if (IsAlreadyWait(attacker, false) || IsAlreadyWait(attacker, true))
            {
                try
                {
                    TeleportObject data = GetTeleportData(GetPlayerFromActive(attacker.UserIDString));
                    if (data == null)
                    {
                        data = GetTeleportData(GetPlayerFromActive(attacker.UserIDString), false);
                    }

                    if(attacker.UserIDString == data.Initiator.ID)
                    {
                        SendReply(FindPlayer(data.Initiator.ID), GetMessage("error_player_start_damage", this));
                        SendReply(FindPlayer(data.Target.ID), GetMessage("error_target_start_damage", this));
                    }
                    else
                    {
                        SendReply(FindPlayer(data.Initiator.ID), GetMessage("error_target_start_damage", this));
                        SendReply(FindPlayer(data.Target.ID), GetMessage("error_player_start_damage", this));
                    }

                    CancelTTPRequest(attacker.UserIDString);
                }
                catch(Exception ex)
                {
                    PrintError(ex.Message);
                }
            }

            if(IsWaitTeleportToHome(victim))
            {
                HomeTeleportObject obj = GetHomeObjectFromWaiters(victim.UserIDString);

                SendReply(victim, GetMessage("error_tth_received_damage", this));

                CancelTTHRequest(victim.UserIDString);
            }

            if(IsWaitTeleportToHome(attacker))
            {
                HomeTeleportObject obj = GetHomeObjectFromWaiters(attacker.UserIDString);

                SendReply(attacker, GetMessage("error_tth_start_damage", this));

                CancelTTHRequest(attacker.UserIDString);
            }
        }
        object OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (player == null)
            {
                return null;
            }

            if (m_Config.AllowTeleportAfterDeath)
            {
                return null;
            }

            if (IsAlreadyWait(player, false) || IsAlreadyWait(player, true))
            {
                TeleportObject data = GetTeleportData(GetPlayerFromActive(player.UserIDString));
                if(data == null)
                {
                    data = GetTeleportData(GetPlayerFromActive(player.UserIDString), false);
                }

                SendReply(FindPlayer(data.Initiator.ID), GetMessage("error_player_is_death", this));
                SendReply(FindPlayer(data.Target.ID), GetMessage("error_target_is_death", this));

                CancelTTPRequest(player.UserIDString);
            }

            if (IsWaitTeleportToHome(player))
            {
                HomeTeleportObject obj = GetHomeObjectFromWaiters(player.UserIDString);

                SendReply(player, GetMessage("error_tth_player_die", this));

                CancelTTHRequest(player.UserIDString);
            }

            return null;
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            if(entity.ShortPrefabName.Contains("foundation"))
            {
                RaycastHit hit;
                if (Physics.Raycast(entity.transform.position, Vector3.up, out hit))
                {
                    BaseEntity bagEntity = hit.GetEntity();

                    if (bagEntity != null)
                    {
                        if(bagEntity as SleepingBag)
                        {
                            string name = string.Empty;

                            foreach(var player in m_ActivePlayers)
                            {
                                if(bagEntity.OwnerID == FindPlayer(player.Value.ID).userID)
                                {
                                    bagEntity.Kill();

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return;
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is SleepingBag)
            {
                SleepingBag bagEntity = (SleepingBag)entity;

                string name = string.Empty;

                foreach (var player in m_ActivePlayers)
                {
                    if (bagEntity.OwnerID == FindPlayer(player.Value.ID).userID)
                    {
                        if (player.Value.Homes.Any((x) => x.Value.Position == bagEntity.transform.position))
                        {
                            if (IsWaitTeleportToHome(FindPlayer(player.Value.ID)))
                            {
                                CancelTTHRequest(player.Key);
                            }

                            var finded = player.Value.Homes.Where((x) => x.Value.Position == bagEntity.transform.position)?.First();
                            if (finded == null)
                            {
                                return;
                            }

                            name = finded.Value.Value?.Name;
                            if (name == null)
                            {
                                return;
                            }

                            bagEntity.enabled = false;
                            bagEntity.enableSaving = false;
                            bagEntity.Kill();

                            player.Value.RemoveHome(name);

                            SendReply(FindPlayer(player.Value.ID), GetMessage("info_home_destroyed", this));

                            break;
                        }
                    }
                }
            }
        }
        object CanRenameBed(BasePlayer player, SleepingBag bed, string bedName)
        {
            if(bed.OwnerID != player.userID)
            {
                SendReply(player, GetMessage("error_rename_other_bags", this));

                return player;
            }

            return null;
        }
        bool CanBeWounded(BasePlayer player, HitInfo info)
        {
            if (player == null) return false;
            if (m_Config.AllowTeleportInWounded) return false;

            if(IsAlreadyWait(player, true) || IsAlreadyWait(player, false))
            {
                var data = GetTeleportData(GetPlayerFromActive(player.UserIDString));
                if(data == null)
                {
                    data = GetTeleportData(GetPlayerFromActive(player.UserIDString), false);
                }

                SendReply(player, GetMessage("error_ttp_cancelled_player_after_wounded", this));
                SendReply(FindPlayer(data.Target.ID), GetMessage("error_ttp_cancelled_target_after_wounded", this));

                CancelTTPRequest(player.UserIDString);
            }

            if(IsWaitTeleportToHome(player))
            {
                SendReply(player, GetMessage("error_tth_cancelled_after_wounded", this));

                CancelTTHRequest(player.UserIDString);
            }

            return false;
        }
        bool CanPickupEntity(BasePlayer basePlayer, BaseCombatEntity entity)
        {
            if (basePlayer == null) return true;
            if (entity as SleepingBag)
            {
                if(entity.OwnerID != basePlayer.userID)
                {
                    SendReply(basePlayer, GetMessage("error_pickup_other_bag", this));

                    return false;
                }

                string name = string.Empty;

                foreach (var player in m_ActivePlayers)
                {
                    if (entity.OwnerID == FindPlayer(player.Value.ID).userID)
                    {
                        if (player.Value.Homes.Any((x) => x.Value.Position == entity.transform.position))
                        {
                            var temp = player.Value.Homes.Where((x) => x.Value.Position == entity.transform.position)?.First();
                            if (temp == null)
                            {
                                return false;
                            }
                            name = temp.Value.Value.Name;

                            SendReply(basePlayer, GetMessage("error_pickup_registered_home", this));

                            return false;
                        }
                    }
                }
            }

            return true;
        }
        object CanAssignBed(BasePlayer assigner, SleepingBag bagEntity, ulong targetPlayerId)
        {
            string name = string.Empty;

            foreach (var player in m_ActivePlayers)
            {
                if (bagEntity.OwnerID == FindPlayer(player.Value.ID).userID)
                {
                    if (player.Value.Homes != null)
                    {
                        name = string.Empty;
                        if(player.Value.Homes.Any((x) => x.Value.Position == bagEntity.transform.position))
                        {
                            name = player.Value.Homes.Where((x) => x.Value.Position == bagEntity.transform.position).First().Value.Name;
                        }

                        if (name != string.Empty)
                        {
                            SendReply(assigner, GetMessage("error_decline_assign_bed", this));

                            break;
                        }
                    }
                }
            }

            return null;
        }
        #endregion

        #region Commands
        [ChatCommand("tpr")]
        void CommandChatTPR(BasePlayer player, string command, string[] args)
        {
            if (player == null)
            {
                return;
            }

            if (args.Length != 1)
            {
                SendReply(player, GetMessage("error_command_tpr_incorrect", this));

                return;
            }

            player.SendConsoleCommand($"tp.request {args[0]}", this);
        }

        [ChatCommand("tpa")]
        void CommandChatTPA(BasePlayer player, string command, string[] args)
        {
            if (player == null)
            {
                return;
            }

            player.SendConsoleCommand($"tp.accept", this);
        }

        [ChatCommand("tpc")]
        void CommandChatTPC(BasePlayer player, string command, string[] args)
        {
            if (player == null)
            {
                return;
            }

            player.SendConsoleCommand($"tp.cancel", this);
        }

        [ChatCommand("home")]
        void CommandChatHomeObjective(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (args.Length < 1) return;

            switch(args[0])
            {
                case "set":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, lang.GetMessage("error_command_sethome_incorrect", this));

                            return;
                        }
                        else
                        {
                            SetHome(player, args[1]);
                        }

                        break;
                    }

                case "remove":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, GetMessage("error_command_removehome_incorrect", this));

                            return;
                        }
                        else
                        {
                            RemoveHome(player, args[1]);
                        }

                        break;
                    }

                case "list":
                    {
                        ShowHomelist(player);

                        break;
                    }

                default:
                    {
                        TPToHome(player, args[0]);

                        break;
                    }
            }
        }

        void SetHome(BasePlayer player, string homename)
        {
            if (player == null)
            {
                return;
            }
            if (!m_Config.AllowHomeSystem)
            {
                SendReply(player, GetMessage("error_home_system_disabled", this));

                return;
            }

            var data = GetPlayerFromActive(player.UserIDString);
            if (data.Homes.Count >= GetSHLimit(player.UserIDString))
            {
                SendReply(player, lang.GetMessage("error_max_home_limits", this));

                return;
            }
            if (data.Homes.ContainsKey(homename))
            {
                SendReply(player, lang.GetMessage("error_home_already_exists", this));

                return;
            }

            BaseEntity foundation = GetFoundation(player.transform.position);
            if(foundation == null)
            {
                if (m_Config.AllowSaveHomeOnlyFundament)
                {
                    SendReply(player, GetMessage("error_sethome_only_foundation", this));

                    return;
                }
            }
            else
            {
                if(m_Config.AllowSaveHomeOnClanFundament)
                {
                    if (!IsClanMember(player.userID, foundation.OwnerID))
                    {
                        SendReply(player, GetMessage("error_sethome_save_only_clan_foundation", this));

                        return;
                    }
                }

                if (m_Config.AllowSaveHomeOnFriendFundament)
                {
                    if (!IsFriends(player.userID, foundation.OwnerID))
                    {
                        SendReply(player, GetMessage("error_sethome_save_only_friend_foundation", this));

                        return;
                    }
                }

                if (player.userID != foundation.OwnerID)
                {
                    if(!m_Config.AllowSaveHomeOnlyFundament)
                    {
                        SendReply(player, GetMessage("error_sethome_save_only_clan_foundation", this));

                        return;
                    }
                }
                else { }
            }

            if(IsPlayerInBB(player) && !m_Config.AllowSaveHomeOnBB)
            {
                SendReply(player, GetMessage("error_sethome_save_on_bb", this));

                return;
            }

            data.CreateHome(GenerateID(), homename, player, GetSHLimit(player.UserIDString), m_Config.CreateBagAfterHomeSave);
            SendReply(player, string.Format(GetMessage("info_home_created", this), homename));
        }
        void RemoveHome(BasePlayer player, string homename)
        {
            if (player == null) return;
            var data = GetPlayerFromActive(player.UserIDString);
            if(!data.Homes.ContainsKey(homename))
            {
                SendReply(player, string.Format(GetMessage("error_saved_home_notfound", this), homename));

                return;
            }
            if(data.Homes[homename].Bag == null)
            {
                SendReply(player, GetMessage("error_home_killed_automatic_deleted", this));

                data.Homes.Remove(homename);

                return;
            }

            if(IsWaitTeleportToHome(player))
            {
                SendReply(player, GetMessage("error_home_deleted_cancelled_teleport", this));

                CancelTTHRequest(player.UserIDString);
            }

            foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
            {
                if (Vector3.Distance(sleepingBag.transform.position, data.Homes[homename].Position) < 1)
                {
                    sleepingBag.Kill();
                    break;
                }
            }

            data.RemoveHome(homename);
            SendReply(player, string.Format(GetMessage("info_home_deleted", this), homename));
        }
        void ShowHomelist(BasePlayer player)
        {
            if(player == null)
            {
                return;
            }

            if (!m_Config.AllowHomeSystem)
            {
                SendReply(player, GetMessage("error_home_system_disabled", this));

                return;
            }

            var data = GetPlayerFromActive(player.UserIDString);
            if (data == null) return;

            if (data.Homes.Count > 0)
            {
                foreach (var home in data.Homes)
                {
                    SendReply(player, string.Format(GetMessage("info_command_homelist", this), home.Value.Name, home.Value.ID, home.Value.Position));
                }
            }
            else
            {
                SendReply(player, GetMessage("info_command_homelist_home_dexists", this));
            }
        }
        void TPToHome(BasePlayer player, string homename)
        {
            if (player == null) return;
            if (!m_Config.AllowHomeSystem)
            {
                SendReply(player, GetMessage("error_home_system_disabled", this));

                return;
            }

            string checkResult = CheckPlayer(player, true, false, false);
            if (checkResult != "-1")
            {
                SendReply(player, checkResult);

                return;
            }

            if (IsUnlimited(player, true))
            {
                var finded = GetPlayerFromActive(player.UserIDString);
                if (finded != null)
                {
                    SendReply(player, string.Format(GetReadeableSeconds(finded.NextTTHLimitRemaining.Remaining, true)));

                    return;
                }
            }

            if (IsRemaining(player, true))
            {
                SendReply(player, string.Format(GetMessage("error_tth_cooldown", this), GetPlayerFromActive(player.UserIDString).NextHomeTeleportRemaining.Remaining));

                return;
            }

            var data = GetPlayerFromActive(player.UserIDString);
            if(!data.Homes.ContainsKey(homename))
            {
                SendReply(player, string.Format(GetMessage("error_home_does_not_exists", this), homename));

                return;
            }

            if (data.Homes[homename].Bag == null)
            {
                if (m_Config.CreateBagAfterHomeSave)
                {
                    SendReply(player, GetMessage("error_home_killed_automatic_deleted", this));

                    data.Homes.Remove(homename);

                    return;
                }
            }

            if (m_Config.CreateBagAfterHomeSave)
            {
                if (IsBagInBB(data.Homes[homename].Bag, player))
                {
                    if (IsWaitTeleportToHome(player))
                    {
                        SendReply(player, GetMessage("error_home_deleted_cancelled_teleport", this));

                        CancelTTHRequest(player.UserIDString);
                    }

                    foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
                    {
                        if (Vector3.Distance(sleepingBag.transform.position, data.Homes[homename].Position) < 1)
                        {
                            sleepingBag.enabled = false;
                            sleepingBag.Kill(BaseNetworkable.DestroyMode.None);
                            sleepingBag.DestroyShared();

                            UnityEngine.Object.Destroy(sleepingBag);

                            break;
                        }
                    }

                    data.RemoveHome(homename);
                    SendReply(player, GetMessage("info_home_deleted_other_bb", this));

                    return;
                }
            }

            CreateTTHRequest(GetPlayerFromActive(player.UserIDString), homename);
        }

        [ChatCommand("homes")]
        void CommandChatAdminHomelist(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!player.IsAdmin)
            {
                SendReply(player, GetMessage("error_player_isnt_admin", this));

                return;
            }
            BasePlayer target = FindPlayer(args[0]);
            if (target == null)
            {
                SendReply(player, GetMessage("error_ttp_incorrect_target", this));

                return;
            }
            var data = GetPlayerFromActive(target.UserIDString);
            if (data == null) return;

            if (data.Homes.Count > 0)
            {
                foreach (var home in data.Homes)
                {
                    SendReply(player, string.Format(GetMessage("info_command_homelist", this), home.Value.Name, home.Value.ID, home.Value.Position));
                }
            }
            else
            {
                SendReply(player, $"У игрока: '{target.displayName}' нет сохраненных домов");
            }
        }

        [ChatCommand("tp")]
        void CommandChatTP_Admin(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if(args.Length < 1)
            {
                SendReply(player, GetMessage("incorrect_command_tp", this));

                return;
            }
            if(!player.IsAdmin)
            {
                SendReply(player, GetMessage("error_player_isnt_admin", this));

                return;
            }
            if(args.Length == 1)
            {
                BasePlayer target = FindPlayer(args[0]);
                if(target == null)
                {
                    SendReply(player, GetMessage("error_ttp_incorrect_target", this));

                    return;
                }
                ClearTeleport(player, target.transform.position);
            }
            if(args.Length == 2)
            {
                BasePlayer victim = FindPlayer(args[0]);
                BasePlayer target = FindPlayer(args[1]);
                if (victim == null || target == null)
                {
                    SendReply(player, GetMessage("error_ttp_incorrect_target", this));

                    return;
                }
                ClearTeleport(victim, target.transform.position);
            }
            if(args.Length == 3)
            {
                float[] coords = new float[3];
                if(!float.TryParse(args[0], out coords[0]) || 
                    !float.TryParse(args[1], out coords[1]) ||
                    !float.TryParse(args[2], out coords[2]))
                {
                    SendReply(player, GetMessage("error_parse_coords_failed_self", this));

                    return;
                }

                ClearTeleport(player, new Vector3(coords[0], coords[1], coords[2]));
            }
            if(args.Length == 4)
            {
                BasePlayer target = FindPlayer(args[0]);
                if(target == null)
                {
                    SendReply(player, GetMessage("error_ttp_incorrect_target", this));

                    return;
                }

                float[] coords = new float[3];
                if (!float.TryParse(args[1], out coords[0]) ||
                    !float.TryParse(args[2], out coords[1]) ||
                    !float.TryParse(args[3], out coords[2]))
                {
                    SendReply(player, GetMessage("error_parse_coords_failed_self", this));

                    return;
                }

                ClearTeleport(target, new Vector3(coords[0], coords[1], coords[2]));
            }

            if (m_Config.EnableAdminCommandsLogging)
            {
                string argsWithSeparator = "/tp ";
                foreach(var arg in args)
                {
                    argsWithSeparator += $"{arg} ";
                }

                LogToFile(m_Config.AdminCommandsLoggingFile, $"Admin: '{player.displayName}(ID: {player.UserIDString})' used a command: '{argsWithSeparator}' in [{DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year}]>>[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}]", this);
            }
        }

        [ChatCommand("tpswitch")]
        void CommandChatGuiSwitch(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            PlayerPluginData data = GetPlayerFromActive(player.UserIDString);
            if (data == null) return;
            if (args.Count() < 1) return;

            switch(args[0].ToLower())
            {
                case "gui":
                    {
                        data.GuiEnabled = !data.GuiEnabled;
                        if (data.GuiEnabled)
                        {
                            SendReply(player, GetMessage("info_gui_enabled", this));
                        }
                        else
                        {
                            SendReply(player, GetMessage("info_gui_disabled", this));
                        }

                        break;
                    }

                case "friends":
                    {
                        data.FriendsEnabled = !data.FriendsEnabled;
                        if(data.FriendsEnabled)
                        {
                            SendReply(player, GetMessage("info_friend_enabled", this));
                        }
                        else
                        {
                            SendReply(player, GetMessage("info_friend_disabled", this));
                        }

                        break;
                    }

                case "clan":
                    {
                        data.ClanEnabled = !data.ClanEnabled;
                        if (data.ClanEnabled)
                        {
                            SendReply(player, GetMessage("info_clan_enabled", this));
                        }
                        else
                        {
                            SendReply(player, GetMessage("info_clan_disabled", this));
                        }

                        break;
                    }

                case "all":
                    {
                        data.AllEnabled = !data.AllEnabled;
                        if (data.AllEnabled)
                        {
                            SendReply(player, GetMessage("info_all_enabled", this));
                        }
                        else
                        {
                            SendReply(player, GetMessage("info_all_disabled", this));
                        }

                        break;
                    }
            }
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("tp.request")]
        private void CmdConsoleTPRequest(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Player();

            if (player == null) return;

            if (!m_Config.AllowTeleportSystem)
            {
                SendReply(player, GetMessage("error_teleport_system_disabled", this));

                return;
            }

            string checkResult = CheckPlayer(player, true, false, false);
            if (checkResult != "-1")
            {
                SendReply(player, checkResult);

                return;
            }

            BasePlayer target = FindPlayer(arg.Args[0]);
            if (target == null)
            {
                SendReply(player, GetMessage("error_ttp_incorrect_target", this));

                return;
            }

            if (target == player)
            {
                SendReply(player, GetMessage("error_ttp_self", this));

                return;
            }

            string checkResultTarget = CheckPlayer(target, false, true);
            if (checkResultTarget != "-1")
            {
                SendReply(player, checkResultTarget);

                return;
            }
            PlayerPluginData targetData = GetPlayerFromActive(target.UserIDString);

            if (!targetData.AllEnabled)
            {
                SendReply(player, GetMessage("player_disabled_all_teleport", this));

                return;
            }
            if (m_Config.AllowTeleportOnlyClan)
            {
                if (!IsClanMember(player.userID, target.userID))
                {
                    SendReply(player, GetMessage("error_ttp_only_clan", this));

                    return;
                }
                else
                {
                    if (!targetData.ClanEnabled)
                    {
                        SendReply(player, GetMessage("player_disabled_clan_teleport", this));

                        return;
                    }
                }
            }
            if(m_Config.AllowTeleportOnlyFriends)
            {
                if(!IsFriends(player.userID, target.userID))
                {
                    SendReply(player, GetMessage("error_ttp_only_friend", this));

                    return;
                }
                else
                {
                    if(!targetData.FriendsEnabled)
                    {
                        SendReply(player, GetMessage("player_disabled_friends_teleport", this));

                        return;
                    }
                }
            }

            CreateTTPRequest(GetPlayerFromActive(player.UserIDString), GetPlayerFromActive(target.UserIDString));
        }
        [ConsoleCommand("tp.cancel")]
        private void CmdConsoleTPCancel(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Player();

            if (player == null)
            {
                return;
            }

            DestroyTeleportRequest(player);

            if(IsAlreadyWait(player, true) || IsAlreadyWait(player, false))
                CancelTTPRequest(player.UserIDString);

            if (IsWaitTeleportToHome(player))
                CancelTTHRequest(player.UserIDString);
        }
        [ConsoleCommand("tp.accept")]
        private void CmdConsoleTPAccept(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Player();

            if (player == null)
            {
                return;
            }

            DestroyTeleportRequest(player);

            string checkResult = CheckPlayer(player, true, true);
            if (checkResult != "-1")
            {
                SendReply(player, checkResult);

                return;
            }

            AppendTTPRequest(GetTeleportData(player.UserIDString));
        }
        #endregion

        #region Data
        private void LoadAllPlayers()
        {
            PrintWarning("Load all players from load plugin ...");

            int count = 0;
            foreach(var player in BasePlayer.activePlayerList)
            {
                LoadPlayer(player);
                count++;
            }

            PrintWarning($"Loaded {count} players !");
        }
        private void SaveAllPlayers()
        {
            int count = 0;

            foreach(var player in BasePlayer.activePlayerList)
            {
                if(m_ActivePlayers.ContainsKey(player.UserIDString))
                {
                    SavePlayer(player, true);
                    count++;
                }
                else
                {
                    PrintError($"Player: '{player.displayName}({player.UserIDString})' exists in base active list, but not exists in local active list !");
                }
            }

            PrintWarning($"Save {count} players !");
        }
        private void LoadPlayer(BasePlayer player)
        {
            DynamicConfigFile plDataFile = Interface.Oxide.DataFileSystem.GetFile($"{Title}\\{player.UserIDString}");
            plDataFile.Settings.Converters.Add(Converter);

            PlayerPluginData data = plDataFile.ReadObject<PlayerPluginData>();

            if (data.ID == "-1")
            {
                data = null;
                data = new PlayerPluginData(player.UserIDString, player.displayName, GetTTPLimit(player.UserIDString), GetTTHLimit(player.UserIDString), new Dictionary<string, HomeObject>(), new TimerBase(), new TimerBase(), new TimerBase(), new TimerBase());

                PrintWarning($"Creating new data for player: '{player.displayName}({player.UserIDString})'");
            }

            if (data.Homes.Count > 0)
            {
                foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
                {
                    data.RestoreHome(sleepingBag.niceName, sleepingBag);

                    if (!data.Homes.ContainsKey(sleepingBag.niceName))
                    {
                        if(m_Config.CreateBagAfterHomeSave)
                            PrintWarning($"Duplicate sleeping bag for player: {player.displayName}({player.UserIDString})");
                    }
                    else { }
                }
            }
            if (m_ActivePlayers.ContainsKey(player.UserIDString))
            {
                m_ActivePlayers.Remove(player.UserIDString);

                PrintError($"Conflict players in active structure. Old object of player: {player.displayName} has been deleted");
            }
            if (data.NextTeleportRemaining.Remaining > 0)
            {
                data.CreateRemaining(Remainings.TeleportToPlayer, timer, data.NextTeleportRemaining.Remaining, () =>
                {
                    SendReply(player, GetMessage("info_ttp_cooldown_reseted", this));
                });
            }
            if (data.NextHomeTeleportRemaining.Remaining > 0)
            {
                data.CreateRemaining(Remainings.TeleportToHome, timer, data.NextHomeTeleportRemaining.Remaining, () =>
                {
                    SendReply(player, GetMessage("info_tth_cooldown_reseted", this));
                });
            }
            if (data.NextTTPLimitRemaining.Remaining > 0)
            {
                data.CreateRemaining(Remainings.NextTTPLimit, timer, data.NextTTPLimitRemaining.Remaining, () =>
                {
                    data.TTPLimit = GetTTPLimit(data.ID);

                    SendReply(player, GetMessage("info_ttp_limit_reseted", this));
                });
            }
            if (data.NextTTHLimitRemaining.Remaining > 0)
            {
                data.CreateRemaining(Remainings.NextTTHLimit, timer, data.NextTTHLimitRemaining.Remaining, () =>
                {
                    data.TTHLimit = GetTTHLimit(data.ID);

                    SendReply(player, GetMessage("info_tth_limit_reseted", this));
                });
            }
            m_ActivePlayers.Add(player.UserIDString, data);
        }
        private void SavePlayer(BasePlayer player, bool all = false)
        {
            if(m_ActivePlayers.ContainsKey(player.UserIDString))
            {
                m_ActivePlayers[player.UserIDString].PrepareToSave();

                Interface.Oxide.DataFileSystem.WriteObject($"{Title}\\{player.UserIDString}", m_ActivePlayers[player.UserIDString]);

                if(!all)
                {
                    m_ActivePlayers.Remove(player.UserIDString);
                }
            }
        }
        #endregion

        #region Home System Instruments
        private void ShowHomes(BasePlayer target, bool isAdminCheck)
        { 

        }
        private void CreateTTHRequest(PlayerPluginData initiator, string name)
        {
            if(!IsWaitTeleportToHome(FindPlayer(initiator.ID)))
            {
                HomeTeleportObject obj = new HomeTeleportObject(initiator, GetWaitTTHSeconds(initiator.ID), initiator.Homes[name].Position);
                obj.ID = GenerateID();
                m_TeleportToHomeWaiters.Add(obj.ID, obj);

                SendReply(FindPlayer(initiator.ID), string.Format(GetMessage("info_tth_request_created", this), obj.Seconds));

                PlaySound(FindPlayer(initiator.ID), "home_teleport");

                obj.Instantiate(timer, () =>
                {
                    FindPlayer(initiator.ID).StartSleeping();
                    timer.Once(0.1f, () =>
                    {
                        FinishedTTHRequest(obj);
                    });
                }, () =>
                {
                    if (initiator.GuiEnabled && m_Config.EnableGUI)
                        RefreshTeleportTimer(FindPlayer(initiator.ID), obj.Remaining.Remaining, "ДОМОЙ");
                });
            }
            else
            {
                SendReply(FindPlayer(initiator.ID), GetMessage("info_tth_already_wait", this));

                return;
            }
        }
        private void FinishedTTHRequest(HomeTeleportObject obj)
        {
            BasePlayer owner = FindPlayer(obj.Owner.ID);

            CargoShip ship = owner.GetComponentInParent<CargoShip>();
            if(ship != null)
            {
                ship.RemoveChild(owner);
                ship.UpdateNetworkGroup();
                ship.SendNetworkUpdateImmediate();
                owner.SetParent(null);
            }

            var data = GetPlayerFromActive(obj.Owner.ID);
            HomeObject home = data.Homes.Where((x) => x.Value.Position == obj.Position)?.First().Value;

            if (home.Bag == null && m_Config.CreateBagAfterHomeSave)
            {
                SendReply(owner, GetMessage("error_home_killed_automatic_deleted", this));

                data.Homes.Remove(home.Name);

                return;
            }
            
            string result = CheckPlayer(owner, true, false);
            if (result != "-1")
            {
                SendReply(owner, result);

                CancelTTHRequest(owner.UserIDString);

                return;
            }

            DestroyTeleportTimer(owner);
            ClearTeleport(owner, obj.Position);

            GetPlayerFromActive(obj.Owner.ID).CreateRemaining(Remainings.TeleportToHome, timer, GetTTHRemaining(obj.Owner.ID), () =>
            {
                SendReply(owner, GetMessage("info_tth_cooldown_reseted", this));
            });

            if(GetPlayerFromActive(obj.Owner.ID).TTHLimit > 0)
                GetPlayerFromActive(obj.Owner.ID).TTHLimit--;

            if (GetPlayerFromActive(obj.Owner.ID).TTHLimit <= 0)
            {
                SendReply(owner, GetMessage("info_create_tth_remaining", this));

                GetPlayerFromActive(obj.Owner.ID).CreateRemaining(Remainings.NextTTHLimit, timer, m_Config.DefaultLimitsUpdated, () =>
                {
                    GetPlayerFromActive(obj.Owner.ID).TTHLimit = GetTTHLimit(obj.Owner.ID);

                    SendReply(FindPlayer(obj.Owner.ID), GetMessage("info_ttp_limit_reseted", this));
                });
            }

            PlaySound(FindPlayer(obj.Owner.ID), "home_teleport");

            SendReply(owner, string.Format(GetMessage("info_tth_request_finished", this), GetPlayerFromActive(owner.UserIDString).TTHLimit));

            obj.Remaining.Destroy();
            m_TeleportToHomeWaiters.Remove(obj.ID);
        }
        private void CancelTTHRequest(string id)
        {
            if (IsWaitTeleportToHome(FindPlayer(id)))
            {
                var data = GetHomeObjectFromWaiters(id);

                DestroyTeleportTimer(FindPlayer(id));

                data.Remaining.Destroy();
                m_TeleportToHomeWaiters.Remove(data.ID);

                SendReply(FindPlayer(id), GetMessage("info_tth_request_cancelled", this));
            }
            else
            {
                SendReply(FindPlayer(id), GetMessage("info_tth_request_notfound", this));
            }
        }
        private HomeTeleportObject GetHomeObjectFromWaiters(string id)
        {
            if(m_TeleportToHomeWaiters.Any((x) => x.Value.Owner.ID == id))
            {
                return m_TeleportToHomeWaiters.Where((x) => x.Value.Owner.ID == id).First().Value;
            }
            else
            {
                return null;
            }
        }
        private bool IsWaitTeleportToHome(BasePlayer player)
        {
            if(GetHomeObjectFromWaiters(player.UserIDString) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Plugin Instruments
        private static void ClearTeleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
            {
                player.ClientRPCPlayer(null, player, "StartLoading");
            }
            player.StartSleeping();
            player.MovePosition(position);

            if (player.net?.connection != null)
            {
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            }

            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null)
            {
                return;
            }
            player.SendFullSnapshot();
        }
        private string CheckPlayer(BasePlayer player, bool isInitiator, bool target, bool accept = false, bool finished = false)
        {
            if(IsBleeding(player) && !m_Config.AllowTeleportInBleeding)
            {
                if(isInitiator) return GetMessage("error_player_in_bleeding", this);
                else return GetMessage("error_target_in_bleeding", this);
            }
            if(IsFreezing(player) && !m_Config.AllowTeleportInFreezing)
            {
                if(isInitiator) return GetMessage("error_player_in_freezing", this);
                else return GetMessage("error_target_in_freezing", this);
            }
            if(IsWounded(player) && !m_Config.AllowTeleportInWounded)
            {
                if (isInitiator) return GetMessage("error_player_in_wounded", this);
                else return GetMessage("error_target_in_wounded", this);
            }
            if(InWater(player) && !m_Config.AllowTeleportInWater)
            {
                if (isInitiator) return GetMessage("error_player_in_water", this);
                else return GetMessage("error_target_in_water", this);
            }
            if(IsRadiation(player) && !m_Config.AllowTeleportInRadiation)
            {
                if (isInitiator) return GetMessage("error_player_in_radiation", this);
                else return GetMessage("error_target_in_radiation", this);
            }
            if(IsCanTeleported(player) && !m_Config.AllowTeleportInVanish && !finished)
            {
                if (isInitiator) return GetMessage("error_player_is_already_teleported", this);
                else return GetMessage("error_target_is_already_teleported", this);
            }
            if(IsAlreadyWait(player, isInitiator) && !accept && !finished)
            {
                if(isInitiator)
                {
                    return GetMessage("error_ttp_player_already_wait", this);
                }
                else
                {
                    return GetMessage("error_ttp_target_already_wait", this);
                }
            }
            if(IsDuelPlayer(player))
            {
                if (isInitiator) return GetMessage("error_target_in_duel", this);
                else return GetMessage("error_player_in_duel", this);
            }
            if(IsPlayerInBB(player) && !m_Config.AllowTeleportInBBZone)
            {
                if (isInitiator) return GetMessage("error_player_in_bb", this);
                else return GetMessage("error_target_in_bb", this);
            }
            if (IsRT(player) && !m_Config.AllowTeleportInRT)
            {
                if (isInitiator) return GetMessage("error_player_in_rt", this);
                else return GetMessage("error_target_in_rt", this);
            }
            if (IsUnlimited(player))
            {
                var finded = GetPlayerFromActive(player.UserIDString);
                if (finded != null)
                {
                    return string.Format(GetReadeableSeconds(finded.NextTTPLimitRemaining.Remaining, false));
                }
            }
            if (IsRemaining(player) && !target)
            {
                var finded = GetPlayerFromActive(player.UserIDString);
                if (finded != null)
                {
                    return string.Format(GetMessage("error_ttp_cooldown", this), finded.NextTeleportRemaining.Remaining);
                }
            }
            if(IsEscapeBlocked(player) && !m_Config.AllowTeleportInEscapeBlock)
            {
                if (isInitiator) return GetMessage("error_player_in_noescape", this);
                else return GetMessage("error_target_in_noescape", this);
            }
            if((IsRaidBlockedOM(player) || IsRaidBlockedRP(player)) && !m_Config.AllowTeleportInRaidBlock)
            {
                if (isInitiator) return GetMessage("error_player_in_raidblock", this);
                else return GetMessage("error_target_in_raidblocked", this);
            }
            if(IsCombatBlocked(player) && !m_Config.AllowTeleportInCombatBlock)
            {
                if (isInitiator) return GetMessage("error_player_in_combatblock", this);
                else return GetMessage("error_target_in_combatblock", this);
            }

            return "-1";
        }

        private bool IsDuelPlayer(BasePlayer target)
        {
            if (Duel == null) return false;
            return (bool)(Duel?.Call("IsPlayerOnActiveDuel", target));
        }
        private bool IsRT(BasePlayer player)
        {
            MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();

            bool inRT = false;
            foreach(MonumentInfo monument in monuments)
            {
                float distance = Vector3.Distance(player.transform.position, monument.transform.position);
                if(monument.Type == MonumentType.Radtown)
                {
                    if(distance <= m_Config.ToRTDistance)
                    {
                        inRT = true;

                        break;
                    }
                }
            }

            return inRT;
        }
        private bool IsBleeding(BasePlayer player)
        {
            if(player.metabolism.bleeding.value > m_Config.CancelTeleportBleedingFactor)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool IsFreezing(BasePlayer player)
        {
            if(player.metabolism.temperature.value < m_Config.CancelTeleportFreezeFactor)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool IsWounded(BasePlayer player)
        {
            return player.IsWounded();
        }
        private bool InWater(BasePlayer player)
        {
            BaseEntity entity = player.GetEntity();

            if (entity.WaterFactor() > m_Config.CancelTeleportWaterFactor) return true;
            else return false;
        }
        private bool IsRadiation(BasePlayer player)
        {
            if (player.metabolism.radiation_poison.value > m_Config.CancelTeleportRadiationFactor) return true;
            else return false;
        }
        private bool IsCanTeleported(BasePlayer player)
        {
            string ret = (string)Interface.Call("CanTeleport", player);
            return !String.IsNullOrEmpty(ret);
        }
        private bool IsPlayerInBB(BasePlayer player)
        {
            RaycastHit hit;
            if(Physics.Raycast(player.transform.position, Vector3.down, out hit))
            {
                if (hit.GetEntity() != null)
                {
                    if(player.IsBuildingBlocked() && !player.IsBuildingAuthed())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private bool IsPlayerInOtherFoundation(BasePlayer player)
        {
            BaseEntity foundation = GetFoundation(player.transform.position);
            if(foundation != null)
            {
                if(foundation.GetComponent<BuildingBlock>() != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private bool IsRemaining(BasePlayer player, bool home = false)
        {
            if (m_ActivePlayers.ContainsKey(player.UserIDString))
            {
                if (!home)
                {
                    if (m_ActivePlayers[player.UserIDString].NextTeleportRemaining.IsEnabled && m_ActivePlayers[player.UserIDString].NextTeleportRemaining.Remaining > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (m_ActivePlayers[player.UserIDString].NextHomeTeleportRemaining.IsEnabled && m_ActivePlayers[player.UserIDString].NextHomeTeleportRemaining.Remaining > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }
        private bool IsUnlimited(BasePlayer player, bool home = false)
        {
            if(m_ActivePlayers.ContainsKey(player.UserIDString))
            {
                if(!home)
                {
                    if(m_ActivePlayers[player.UserIDString].NextTTPLimitRemaining.IsEnabled && m_ActivePlayers[player.UserIDString].NextTTPLimitRemaining.Remaining > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (m_ActivePlayers[player.UserIDString].NextTTHLimitRemaining.IsEnabled && m_ActivePlayers[player.UserIDString].NextTTHLimitRemaining.Remaining > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }
        private bool IsClanMember(ulong playerID, ulong targetID)
        {
            string playerTag = (string)Clans?.Call("GetClanOf", playerID);
            string targetTag = (string)Clans?.Call("GetClanOf", targetID);
            if (playerTag == null || targetTag == null) return false;

            if (playerTag == targetTag) return true;
            else return false;
        }
        private bool IsFriends(ulong playerID, ulong friendId)
        {
            return (bool)(Friends?.Call("AreFriends", playerID, friendId) ?? false);
        }
        private bool IsRaidBlockedRP(BasePlayer player)
        {
            try
            {
                if (((double)(NoEscape?.Call("ApiGetTime", player.userID) ?? 0)) > 0)
                {
                    return true;
                }
                else return false;
            }
            catch(Exception)
            {
                return false;
            }
        }
        private bool IsRaidBlockedOM(BasePlayer player)
        {
            return (bool)(NoEscape?.Call("IsRaidBlocked", player) ?? false);
        }
        private bool IsEscapeBlocked(BasePlayer player)
        {
            return (bool)(NoEscape?.Call("IsEscapeBlocked", player) ?? false);
        }
        private bool IsCombatBlocked(BasePlayer player)
        {
            return (bool)(NoEscape?.Call("IsCombatBlocked", player) ?? false);
        }
        private bool IsAlreadyWait(BasePlayer player, bool isInitiator)
        {
            if(GetPlayerDataFromWaiters(player.UserIDString, isInitiator) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool IsBagInBB(SleepingBag bag, BasePlayer owner)
        {
            if (bag == null) return true;

            RaycastHit hit;
            if (Physics.Raycast(bag.transform.position, Vector3.down, out hit))
            {
                if (hit.GetEntity() != null)
                {
                    if (
                        owner.IsBuildingBlocked(bag.transform.position, bag.transform.rotation, bag.transform.GetBounds()) 
                        && !owner.IsBuildingAuthed(bag.transform.position, bag.transform.rotation, bag.transform.GetBounds()))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private BasePlayer FindPlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            return default(BasePlayer);
        }
        private string GetMessage(string key, Plugin caller)
        {
            if (m_Config.EnablePluginPrefix)
            {
                string prefix = $"<color=#ffa500ff>{m_Config.PluginPrefix}</color>: ";

                prefix += lang.GetMessage(key, caller);

                return prefix;
            }
            else
            {
                return lang.GetMessage(key, caller);
            }
        }
        private string GetReadeableSeconds(int seconds, bool isTTH)
        {
            var ts = TimeSpan.FromSeconds(seconds);

            if (!isTTH)
                return string.Format(GetMessage("error_ttp_unlimited", this), ts.Hours, ts.Minutes, ts.Seconds);
            else
                return string.Format(GetMessage("error_tth_unlimited", this), ts.Hours, ts.Minutes, ts.Seconds);
        }
        private BaseEntity GetFoundation(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 0.1f))
            {
                var entity = hit.GetEntity();
                if (entity != null && entity.ShortPrefabName.Contains("foundation"))
                    return entity;
            }
            return null;
        }

        private void CreateTTPRequest(PlayerPluginData initiator, PlayerPluginData target)
        {
            if (!IsAlreadyWait(FindPlayer(initiator.ID), true) || !IsAlreadyWait(FindPlayer(target.ID), false))
            {
                TeleportObject obj = new TeleportObject(initiator, target, -1);
                obj.ID = GenerateID();
                m_TeleportToPlayerWaiters.Add(obj.ID, obj);

                SendReply(FindPlayer(initiator.ID), string.Format(GetMessage("info_ttp_request_player_created", this), obj.Target.Name));
                SendReply(FindPlayer(target.ID), string.Format(GetMessage("info_ttp_request_target_created", this), obj.Initiator.Name));

                if(obj.Target.GuiEnabled && m_Config.EnableGUI)
                    ShowTeleportRequest(FindPlayer(target.ID), initiator.Name);

                if(!FindPlayer(initiator.ID).IsAdmin && m_Config.EnableUsersCommandsLogging)
                {
                    LogToFile(m_Config.UserCommandsLoggingFile, $"[LOG] -> {initiator.Name}({initiator.ID}) using the command '/tpr' to {target.Name}({target.ID}).", this);
                }

                if(FindPlayer(initiator.ID).IsAdmin && m_Config.EnableAdminCommandsLogging)
                {
                    LogToFile(m_Config.AdminCommandsLoggingFile, $"[LOG] -> Admin: {initiator.Name}({initiator.ID}) using the command '/tpr' to {target.Name}({target.ID}).", this);
                }

                PlaySound(FindPlayer(target.ID), "tp_receive");
                PlaySound(FindPlayer(initiator.ID), "tp_request");

                timer.Once(15, () =>
                {
                    if(!obj.Accepted)
                    {
                        CancelTTPRequest(obj.Initiator.ID);
                    }
                });
            }
            else
            {
                SendReply(FindPlayer(initiator.ID), GetMessage("whose_from_waiter_already_wait", this));
                SendReply(FindPlayer(target.ID), GetMessage("whose_from_waiter_already_wait", this));
            }
        }
        private void CancelTTPRequest(string id)
        {
            if(IsAlreadyWait(FindPlayer(id), true) || IsAlreadyWait(FindPlayer(id), false))
            {
                var data = GetTeleportData(id);

                BasePlayer initiator = FindPlayer(data.Initiator.ID);
                BasePlayer target = FindPlayer(data.Target.ID);

                DestroyTeleportTimer(initiator);
                DestroyTeleportRequest(FindPlayer(data.Target.ID));

                data.Remaining.Destroy();
                data.Accepted = true;
                m_TeleportToPlayerWaiters.Remove(data.ID);

                SendReply(initiator, GetMessage("info_ttp_request_cancelled", this));
                SendReply(target, GetMessage("info_ttp_request_cancelled", this));
            }
            else
            {
                SendReply(FindPlayer(id), GetMessage("info_ttp_request_notfound", this));
            }
        }
        private void AppendTTPRequest(TeleportObject obj)
        {
            if (obj == null)
            {
                return;
            }

            obj.Seconds = GetWaitTTPSeconds(obj.Initiator.ID);
            obj.Accepted = true;

            BasePlayer initiator = FindPlayer(obj.Initiator.ID);
            BasePlayer target = FindPlayer(obj.Target.ID);
            if (initiator == null || target == null)
            {
                Puts("Initiator or target is null");

                return;
            }

            obj.Instantiate(timer, () =>
            {
                initiator.StartSleeping();
                timer.Once(0.1f, () =>
                {
                    FinishedTTPRequest(obj);
                });
            }, () =>
            {
                if (obj.Initiator.GuiEnabled && m_Config.EnableGUI)
                    RefreshTeleportTimer(FindPlayer(obj.Initiator.ID), obj.Remaining.Remaining);
            });

            PlaySound(initiator, "tp_receive");

            SendReply(initiator, string.Format(GetMessage("info_ttp_request_player_appended", this), obj.Target.Name, obj.Seconds));
            SendReply(target, string.Format(GetMessage("info_ttp_request_target_appended", this), obj.Initiator.Name, obj.Seconds));
        }
        private void FinishedTTPRequest(TeleportObject obj)
        {
            BasePlayer initiator = FindPlayer(obj.Initiator.ID);
            BasePlayer target = FindPlayer(obj.Target.ID);

            string result = CheckPlayer(initiator, true, false, true);
            if(result != "-1")
            {
                SendReply(initiator, result);
                CancelTTPRequest(obj.Initiator.ID);
                return;
            }
            result = CheckPlayer(target, false, true, true);
            if(result != "-1")
            {
                SendReply(initiator, result);
                CancelTTPRequest(obj.Initiator.ID);
                return;
            }
            CargoShip ship = initiator.GetComponentInParent<CargoShip>();
            if (ship != null)
            {
                ship.RemoveChild(initiator);
                ship.UpdateNetworkGroup();
                ship.SendNetworkUpdateImmediate();
                initiator.SetParent(null);
            }

            DestroyTeleportTimer(initiator);
            obj.Remaining.Destroy();

            ClearTeleport(initiator, target.transform.position);

            GetPlayerFromActive(obj.Initiator.ID).CreateRemaining(Remainings.TeleportToPlayer, timer, GetTTPRemaining(obj.Initiator.ID), () =>
            {
                SendReply(initiator, lang.GetMessage("info_ttp_cooldown_reseted", this));
            });

            GetPlayerFromActive(obj.Initiator.ID).TTPLimit--;

            if (GetPlayerFromActive(obj.Initiator.ID).TTPLimit <= 0)
            {
                SendReply(initiator, GetMessage("info_create_ttp_remaining", this));

                GetPlayerFromActive(obj.Initiator.ID).CreateRemaining(Remainings.NextTTPLimit, timer, m_Config.DefaultLimitsUpdated, () =>
                {
                    GetPlayerFromActive(obj.Initiator.ID).TTPLimit = GetTTPLimit(obj.Initiator.ID);

                    SendReply(FindPlayer(obj.Initiator.ID), GetMessage("info_ttp_limit_reseted", this));
                });
            }

            PlaySound(FindPlayer(obj.Target.ID), "tp_request");
            PlaySound(FindPlayer(obj.Initiator.ID), "tp_request");

            SendReply(initiator, string.Format(GetMessage("info_ttp_request_target_finished", this), obj.Target.Name, GetPlayerFromActive(obj.Initiator.ID).TTPLimit));
            SendReply(target, string.Format(GetMessage("info_ttp_request_player_finished", this), obj.Initiator.Name));

            m_TeleportToPlayerWaiters.Remove(obj.ID);
        }
        #endregion

        #region Server 
        private void PlaySound(BasePlayer player, string key)
        {
            if(m_Config.EnableSoundEffects)
            {
                if(m_Config.SoundsFotNotify.ContainsKey(key))
                {
                    Effect x = new Effect(m_Config.SoundsFotNotify[key], player, 0, new Vector3(), new Vector3());
                    EffectNetwork.Send(x, player.Connection);
                }
                else
                {
                    PrintError($"Sound '{key}' not found. Check your config file");
                }
            }
        }
        #endregion

        #region UI
        public class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return NewElement;
            }

            public static void LoadImage(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    FadeOut = 0.15f,
                    Components =
                    {
                        new CuiRawImageComponent { Url = url, FadeIn = 0.3f },
                        new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static void CreateInput(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, bool password, int charLimit, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent { Text = text, FontSize = size, Align = align, Color = color, Command = command, IsPassword = password, CharsLimit = charLimit},
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }

            public static void CreateText(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.TrimStart('#');
                }

                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        private static string TeleportRequestHud = "Teleport_RequstHud";
        private static string TeleportTimerHud = "Teleport_TimerHud";

        public void ShowTeleportRequest(BasePlayer player, string from)
        {
            CuiElementContainer container = new CuiElementContainer()
            {
                {
                    new CuiPanel()
                    {
                        CursorEnabled = true,
                        RectTransform =
                        {
                            AnchorMin = "0.3661458 0.6888888", AnchorMax = "0.6083333 0.9009258", OffsetMax = "0 0"
                        },
                        Image =
                        {
                            Color = UI.Color("#1D1D1DC2", 0.9f), Material = "assets/content/ui/uibackgroundblur.mat"
                        }
                    }, "Overlay", TeleportRequestHud
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "9.592623E-08 0.6550218", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = m_Config.DefaultHeaderTeleportNotify, Align = TextAnchor.UpperCenter, Font = "robotocondensed-bold.ttf", FontSize = 25 },
                        Button = { Color = UI.Color("#6767679E", 1f), Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
                    },
                    TeleportRequestHud
                },
                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.0322578 0.6200887", AnchorMax = "0.9892477 0.8558964", OffsetMax = "0 0" },
                        Text = { Text = string.Format(m_Config.DefaultFromTeleportNotify, from), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14 }
                    },
                    TeleportRequestHud
                },
                {
                    new CuiPanel()
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.004301132 0.6593894", AnchorMax = "0.9956988 0.6637558", OffsetMax = "0 0"
                        },
                        Image =
                        {
                            Color = "0 0 0 0",
                        }
                    },
                    TeleportRequestHud
                },
                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.1591392 0.3537124", AnchorMax = "0.8516133 0.624455", OffsetMax = "0 0" },
                        Text = { Text = $"ПРИНЯТЬ ?", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 22 }
                    },
                    TeleportRequestHud
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.01935504 0.03056763", AnchorMax = "0.4688173 0.2663756", OffsetMax = "0 0" },
                        Button = { Command = "tp.accept", Color = UI.Color("#588B61FF", 1f), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        Text = { Text = "ДА", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                    }, TeleportRequestHud
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5311831 0.03493431", AnchorMax = "0.9827962 0.2663756", OffsetMax = "0 0" },
                        Button = { Command = "tp.cancel", Color = UI.Color("#714949FF", 1f), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        Text = { Text = "НЕТ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                    }, TeleportRequestHud
                }
            };

            CuiHelper.AddUi(player, container);
        }
        public void DestroyTeleportRequest(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, TeleportRequestHud);
        }
        public void RefreshTeleportTimer(BasePlayer player, int secs, string to = "К ИГРОКУ")
        {
            CuiHelper.DestroyUi(player, TeleportTimerHud);
            CuiElementContainer container = new CuiElementContainer()
            {
                {
                    new CuiPanel()
                    {
                        CursorEnabled = false,
                        RectTransform =
                        {
                            AnchorMin = "0.3276041 0.8398149", AnchorMax = "0.659375 0.9296296", OffsetMax = "0 0"
                        },
                        Image =
                        {
                            Color = "0.1944085 0.1944085 0.1944085 0.4464463", Material = "assets/content/ui/uibackgroundblur.mat"
                        }
                    }, "Overlay", TeleportTimerHud
                },
            };
            if(secs < 10)
            {
                container.Add(new CuiLabel()
                {
                    RectTransform = { AnchorMin = "0.0204581 0.08247322", AnchorMax = "0.973006 0.9484544", OffsetMax = "0 0" },
                    Text = { Color = "0.6480626 0.1103811 0.1855408 1", Text = $"<size=30>☢</size> ТЕЛЕПОРТ {to}: 00:0{secs} <size=30>☢</size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 22 },
                }, TeleportTimerHud);
            }
            else
            {
                container.Add(new CuiLabel()
                {
                    RectTransform = { AnchorMin = "0.0204581 0.08247322", AnchorMax = "0.973006 0.9484544", OffsetMax = "0 0" },
                    Text = { Color = "0.6480626 0.1103811 0.1855408 1", Text = $"<size=30>☢</size> ТЕЛЕПОРТ {to}: 00:{secs} <size=30>☢</size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 22 },
                }, TeleportTimerHud);
            }
            CuiHelper.AddUi(player, container);
        }
        public void DestroyTeleportTimer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, TeleportTimerHud);
        }
        #endregion

        #region Collection Instruments
        private PlayerPluginData GetPlayerDataFromWaiters(string id, bool isInitiator)
        {
            if (isInitiator)
            {
                if (m_TeleportToPlayerWaiters.Any((x) => x.Value.Initiator.ID == id))
                {
                    return m_TeleportToPlayerWaiters.Where((x) => x.Value.Initiator.ID == id).First().Value.Initiator;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (m_TeleportToPlayerWaiters.Any((x) => x.Value.Target.ID == id))
                {
                    return m_TeleportToPlayerWaiters.Where((x) => x.Value.Target.ID == id).First().Value.Target;
                }
                else
                {
                    return null;
                }
            }
        }
        private PlayerPluginData GetPlayerFromActive(string id)
        {
            if(m_ActivePlayers.ContainsKey(id))
            {
                return m_ActivePlayers[id];
            }
            else
            {
                return null;
            }
        }
        private TeleportObject GetTeleportData(int id)
        {
            if(m_TeleportToPlayerWaiters.ContainsKey(id))
            {
                return m_TeleportToPlayerWaiters[id];
            }
            else
            {
                return null;
            }
        }
        private TeleportObject GetTeleportData(PlayerPluginData obj, bool isInitiator = true)
        {
            if (isInitiator)
            {
                var data = m_TeleportToPlayerWaiters.Any((x) => x.Value.Initiator.ID == obj.ID);
                if (data)
                {
                    return m_TeleportToPlayerWaiters.Where((x) => x.Value.Initiator.ID == obj.ID).First().Value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var data = m_TeleportToPlayerWaiters.Any((x) => x.Value.Target.ID == obj.ID);
                if (data)
                {
                    return m_TeleportToPlayerWaiters.Where((x) => x.Value.Target.ID == obj.ID).First().Value;
                }
                else
                {
                    return null;
                }
            }
        }
        private TeleportObject GetTeleportData(string id)
        {
            TeleportObject @object;
            if (GetTeleportData(GetPlayerFromActive(id), true) != null)
            {
                @object = GetTeleportData(GetPlayerFromActive(id), true);
            }
            else
            {
                @object = GetTeleportData(GetPlayerFromActive(id), false);
            }

            return @object;
        }
        private int GenerateID()
        {
            int result = Core.Random.Range(0, 65535);
            if(!m_TeleportToPlayerWaiters.ContainsKey(result))
            {
                return result;
            }
            else
            {
                return GenerateID();
            }
        }
        #endregion

        #region Dynamic Permission Instruments
        public int GetPermissionProperty(string permission, Settings setting)
        {
            if(m_Config.PermissionSettings.ContainsKey(permission))
            {
                if(m_Config.PermissionSettings[permission].ContainsKey("Settings"))
                {
                    if(m_Config.PermissionSettings[permission]["Settings"].ContainsKey(setting.ToString()))
                    {
                        return m_Config.PermissionSettings[permission]["Settings"][setting.ToString()];
                    }
                    else
                    {
                        return -3;
                    }
                }
                else
                {
                    return -2;
                }
            }
            else
            {
                return -1;
            }
        }
        public int GetWaitTTPSeconds(string id)
        {
            int secs = m_Config.DefaultTimeToTeleport;

            foreach (var perm in m_Config.PermissionSettings)
            {
                if (permission.UserHasPermission(id, perm.Key))
                {
                    if(secs > GetPermissionProperty(perm.Key, Settings.WaitToTeleportTime))
                    {
                        secs = GetPermissionProperty(perm.Key, Settings.WaitToTeleportTime);
                    }
                }
            }

            return secs;
        }
        public int GetWaitTTHSeconds(string id)
        {
            int secs = m_Config.DefaultTimeToTeleportHome;

            foreach (var perm in m_Config.PermissionSettings)
            {
                if (permission.UserHasPermission(id, perm.Key))
                {
                    if(secs > GetPermissionProperty(perm.Key, Settings.WaitToHomeTeleportTime))
                    {
                        secs = GetPermissionProperty(perm.Key, Settings.WaitToHomeTeleportTime);
                    }

                    break;
                }
            }

            return secs;
        }
        public int GetTTPRemaining(string id)
        {
            int secs = m_Config.DefaultRemainingAfterTeleport;

            foreach (var perm in m_Config.PermissionSettings)
            {
                if (permission.UserHasPermission(id, perm.Key))
                {
                    if (secs > GetPermissionProperty(perm.Key, Settings.WaitAfterTeleport))
                    {
                        secs = GetPermissionProperty(perm.Key, Settings.WaitAfterTeleport);
                    }

                    break;
                }
            }

            return secs;
        }
        public int GetTTHRemaining(string id)
        {
            int secs = m_Config.DefaultRemainingAfterHomeTeleport;

            foreach(var perm in m_Config.PermissionSettings)
            {
                if(permission.UserHasPermission(id, perm.Key))
                {
                    if (secs > GetPermissionProperty(perm.Key, Settings.WaitAfterTeleportHome))
                    {
                        secs = GetPermissionProperty(perm.Key, Settings.WaitAfterTeleportHome);
                    }

                    break;
                }
            }

            return secs;
        }
        public int GetSHLimit(string id)
        {
            int limit = m_Config.DefaultSaveHomeLimit;

            foreach(var perm in m_Config.PermissionSettings)
            {
                if(permission.UserHasPermission(id, perm.Key))
                {
                    if (limit < GetPermissionProperty(perm.Key, Settings.SaveHomeLimit))
                    {
                        limit = GetPermissionProperty(perm.Key, Settings.SaveHomeLimit);
                    }

                    break;
                }
            }

            return limit;
        }
        public int GetTTHLimit(string id)
        {
            int limit = m_Config.DefaultTTHOneDay;

            foreach (var perm in m_Config.PermissionSettings)
            {
                if (permission.UserHasPermission(id, perm.Key))
                {
                    if (limit < GetPermissionProperty(perm.Key, Settings.TeleportToHomeLimit))
                    {
                        limit = GetPermissionProperty(perm.Key, Settings.TeleportToHomeLimit);
                    }

                    if (limit < 0)
                    {
                        limit = m_Config.DefaultTTHOneDay;
                    }

                    break;
                }
            }

            return limit;
        }
        public int GetTTPLimit(string id)
        {
            int limit = m_Config.DefaultTTPOneDay;

            foreach (var perm in m_Config.PermissionSettings)
            {
                if (permission.UserHasPermission(id, perm.Key))
                {
                    if (limit < GetPermissionProperty(perm.Key, Settings.TeleportToPlayerLimit))
                    {
                        limit = GetPermissionProperty(perm.Key, Settings.TeleportToPlayerLimit);
                    }
                    if (limit < 0)
                    {
                        limit = m_Config.DefaultTTPOneDay;
                    }

                    break;
                }
            }

            return limit;
        }
        #endregion

        #region Converter
        static UnityVector3Converter Converter = new UnityVector3Converter();
        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }
        #endregion
    }
}
