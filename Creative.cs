/*
*  EULA
*  
*  From version 2.1.0 onward, you are allowed to modify the source code for **personal use only**.
*  You may **not** copy, merge, publish, distribute, sublicense, sell, or share copies or modifications
*  of this software without the Developer's explicit consent.
*
*  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  Copyright Rediz © 2023 - 2025
*  Contact: eric@legacystudio.com.ar
*  Developers: Rediz (former Ryuk_)
*/

// unmodified
// Reference: Mono.Data.Sqlite

using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Libraries.Covalence;
using ProtoBuf;
using Graphics = System.Drawing.Graphics;
using UnityEngine.UI;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Rust;
using System.Globalization;
using System.Text;
using System.Collections;
using Random = UnityEngine.Random;
using Oxide.Core.Configuration;
using Facepunch.Extend;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.Reflection;
using System.Threading.Tasks;
using Network;
using System.Data;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Collections.Specialized;
using System.Threading;
using System.Collections.Concurrent;

namespace Oxide.Plugins
{
    [Info("Creative", "Ryuk_", "2.1.0")]
    [Description("Build any bases and let your players do it too. Everyone has their own private PLOT.")]

    public class Creative : RustPlugin
    {
        #region Variables & Classes

        [PluginReference] Plugin Symmetry, NoEscape, ImageLibrary;

        public class PlayerSettings
        {
            public ulong ID { get; set; }

            public string CurrentMenu { get; set; }
            public bool IsMenuOpened { get; set; }

            public bool AutoEntity { get; set; }
            public bool FillBatteries { get; set; }
            public bool EntityStability { get; set; }
            public bool Noclip { get; set; }
            public bool GodMode { get; set; }
            public bool ChangeBGrade { get; set; }
            public bool InfiniteAmmo { get; set; }
            public bool BGradeHud { get; set; }

            public bool GTFO { get; set; }
            public bool Raid { get; set; }
            public bool PlayerOwnZone { get; set; }

            public bool AutoDoors { get; set; }
            public bool DoorOpenClose { get; set; }
            public bool CodeLockDoor { get; set; }
            public string DoorPrefab { get; set; }
            public string DoubleDoorPrefab { get; set; }

            public bool AutoWindows { get; set; }
            public string WindowPrefab { get; set; }
            public string EmbrasurePrefab { get; set; }

            public bool AutoElectricity { get; set; }
            public List<string> ElectricityList { get; set; } = new List<string>();

            public bool BuildingGrade { get; set; }
            public int CurrentGrade { get; set; }
            public int CurrentSkin { get; set; }
            public int CurrentContainerSkin { get; set; }
            public bool PlayerInArea { get; set; }
            public bool BuildUpgradeInProgress { get; set; }
            public bool BGradeBackground { get; set; }
            public string CurrentBaseName { get; set; }

            public bool NetworkingFoundations { get; set; }
            public bool NetworkingFloor { get; set; }
            public bool NetworkingWalls { get; set; }
            public bool NetworkingOthers { get; set; }
            public bool NetworkingDeployables { get; set; }

            public bool LoadingBase { get; set; }
            public int CurrentPage { get; set; }
            public string CommunityCurrentMenu { get; set; }
            public Vector3 StoredClaimedPlotLocation { get; set; }
            public bool ClearingPlot { get; set; }
            public bool BCostHud { get; set; } = true;

            public string PendingReviewBase { get; set; }
            public float PendingReviewTime { get; set; }
            public ulong PendingReviewSteamId { get; set; }
            public string PendingReviewImageUrl { get; set; }
            
            // public float PlotRadius { get; set; }
            private float _plotRadius;
            private bool _plotRadiusCached;
            public bool doUpdateHud;

            public PlayerSettings(ulong id)
            {
                ID = id;
                ResetSettings(id);
            }

            public void ResetSettings(ulong id)
            {
                IsMenuOpened = false;
                AutoEntity = false;
                FillBatteries = false;
                EntityStability = false;
                Noclip = false;
                GodMode = false;
                ChangeBGrade = false;
                InfiniteAmmo = false;
                BGradeHud = false;
                GTFO = false;
                Raid = false;
                PlayerOwnZone = false;
                AutoDoors = false;
                DoorOpenClose = false;
                CodeLockDoor = false;
                DoorPrefab = string.Empty;
                DoubleDoorPrefab = string.Empty;
                AutoWindows = false;
                WindowPrefab = string.Empty;
                EmbrasurePrefab = string.Empty;
                AutoElectricity = false;
                ElectricityList.Clear();
                BuildingGrade = false;
                CurrentGrade = 0;
                CurrentSkin = 0;
                CurrentContainerSkin = 0;
                PlayerInArea = false;
                BuildUpgradeInProgress = false;
                BGradeBackground = false;
                CurrentBaseName = string.Empty;
                NetworkingFoundations = false;
                NetworkingFloor = false;
                NetworkingWalls = false;
                NetworkingOthers = false;
                NetworkingDeployables = false;
                LoadingBase = false;
                CurrentPage = 0;
                CommunityCurrentMenu = string.Empty;
                StoredClaimedPlotLocation = new Vector3();
                ClearingPlot = false;
                PendingReviewBase = null;
                PendingReviewTime = 0;
                PendingReviewSteamId = 0;
                PendingReviewImageUrl = null;
                _plotRadiusCached = false;
                doUpdateHud = false;
                //PlotRadius = Instance.permission.UserHasPermission(id.ToString(), "creative.vip") ? Instance._config.plot_radius_vip : Instance._config.plot_radius_default;
            }
        }

        public class PlayerWeather
        {
            public float wind { get; set; }
            public float rain { get; set; }
            public float thunder { get; set; }
            public float rainbow { get; set; }
            public float atmosphere_rayleigh { get; set; }
            public float atmosphere_mie { get; set; }
            public float atmosphere_brightness { get; set; }
            public float atmosphere_contrast { get; set; }
            public float atmosphere_directionality { get; set; }
            public float fog { get; set; }
            public float cloud_size { get; set; }
            public float cloud_opacity { get; set; }
            public float cloud_coverage { get; set; }
            public float cloud_sharpness { get; set; }
            public float cloud_coloring { get; set; }
            public float cloud_attenuation { get; set; }
            public float cloud_saturation { get; set; }
            public float cloud_scattering { get; set; }
            public float current_time { get; set; }
            public float value { get; set; }       

            public PlayerWeather(float a1, float a2, float a3, float a4, float a5, float a6, float a7, float a8, float a9, float b1, float b2, float b3, float b4, float b5, float b6, float b7, float b8, float b9, float c1, float c2)
            {
                this.wind = a1;
                this.rain = a2;
                this.thunder = a3;
                this.rainbow = a4;
                this.atmosphere_rayleigh = a5;
                this.atmosphere_mie = a6;
                this.atmosphere_brightness = a7;
                this.atmosphere_contrast = a8;
                this.atmosphere_directionality = a9;
                this.fog = b1;
                this.cloud_size = b2;
                this.cloud_opacity = b3;
                this.cloud_coverage = b4;
                this.cloud_sharpness = b5;
                this.cloud_coloring = b6;
                this.cloud_attenuation = b7;
                this.cloud_saturation = b8;
                this.cloud_scattering = b9;
                this.current_time = c1;
                this.value = c2;
            }
        }

        private class ImageData {
            [JsonProperty(PropertyName = "ImageName")]
            public string name;
            
            [JsonProperty(PropertyName = "ImgUrl")]
            public string img;
        }

        public enum ItemContainerType
        {
            Main,
            Belt,
            Wear
        }

        private class KitData {
            [JsonProperty(PropertyName = "Item ID")]
            public int id;
            
            [JsonProperty(PropertyName = "Item Skin ID")]
            public ulong skinid;

            [JsonProperty(PropertyName = "Player Container Type (Main, Belt, Wear)")]
            public ItemContainerType itemcontainer;
        }

        private class MySqlData {
            [JsonProperty(PropertyName = "Server")]
            public string server;

            [JsonProperty(PropertyName = "Username")]
            public string username;

            [JsonProperty(PropertyName = "Password")]
            public string password;

            [JsonProperty(PropertyName = "Database")]
            public string database;

            [JsonProperty(PropertyName = "Port")]
            public int port;
        }

        public class SpawnedEntityInfo
        {
            public NetworkableId entityID;
            public string entityName;

            public SpawnedEntityInfo(NetworkableId id, string name)
            {
                entityID = id;
                entityName = name;
            }
        }

        [Serializable]
        public class PlotManagerData
        {
            public ulong zoneID;
            public Vector3 CenterZone;
            public bool GTFO;
        }

        public class MarkerData
        {
            public string MarkerID { get; set; }
            public string Text { get; set; }
            public Vector3 Position { get; set; }
            public float Radius { get; set; }
            public ulong OwnerID { get; set; }
        }

        private class BaseShareInfo
        {
            public string CreatorName { get; set; }
            public string SteamId { get; set; }
            public string BaseName { get; set; }
            public string ShareCode { get; set; }
            public int Likes { get; set; }
            public int Dislikes { get; set; }
            public int Downloads { get; set; }
            public string ImageUrl { get; set; }
            public string baseImageUrl {get; set; }
        }

        public class PasteData
        {
            public string Id { get; set; }
            public string Description { get; set; }
            public int Views { get; set; }
            public string CreatedAt { get; set; }
        }

        public class PasteResponse2
        {
            public int CurrentPage { get; set; }
            public List<PasteData> Data { get; set; }
            public bool Success { get; set; }
        }

        public static Creative Instance;
        public Dictionary<ulong, PlayerSettings> playerSettings = new Dictionary<ulong, PlayerSettings>();
        public Dictionary<ulong, PlayerWeather> playerWeather = new Dictionary<ulong, PlayerWeather>();
        public Dictionary<ulong, PlotManager> activeZones = new Dictionary<ulong, PlotManager>();
        private Dictionary<string, VendingMachineMapMarker> machineMarkers = new Dictionary<string, VendingMachineMapMarker>();
        private Dictionary<string, MapMarkerGenericRadius> sphereMarker = new Dictionary<string, MapMarkerGenericRadius>();
        private List<MarkerData> markerDataList = new List<MarkerData>();
        private Dictionary<ulong, float> lastCommandTime = new Dictionary<ulong, float>();
        public Dictionary<ulong, List<SpawnedEntityInfo>> playerEntityCount = new Dictionary<ulong, List<SpawnedEntityInfo>>();
        private Dictionary<ulong, float> commandCooldowns = new Dictionary<ulong, float>();
        private Dictionary<ulong, float> menuCooldown = new Dictionary<ulong, float>();
        private Dictionary<ulong, float> baseCooldown = new Dictionary<ulong, float>();
        private Dictionary<uint, IOEntity> oldToNewEntityIdMap = new Dictionary<uint, IOEntity>();
        public List<StabilityEntity> EntStable = Facepunch.Pool.GetList<StabilityEntity>();
        private Dictionary<ulong, Dictionary<string, List<BaseEntity>>> playerToggledEntities = new Dictionary<ulong, Dictionary<string, List<BaseEntity>>>();
        private readonly Dictionary<BaseEntity, Collider[]> entityColliders = new Dictionary<BaseEntity, Collider[]>();

        private static ConcurrentQueue<string> saveQueue = new ConcurrentQueue<string>();
        private static Dictionary<string, QueuedSave> pendingSaves = new Dictionary<string, QueuedSave>();
        private static Timer saveTimer;
        private const int MaxBatchSaves = 5;

        private class QueuedSave {
            public string Key;
            public string FilePath;
            public JObject SaveData;
            public string SaveName;
            public ulong UserId;
            public BasePlayer Player;
        }

        #endregion

        #region Hooks

        private void RegisterPermissions()
        {            
            permission.RegisterPermission("creative.admin", this);
            permission.RegisterPermission("creative.reviewer", this);

            permission.RegisterPermission("creative.use", this);
            permission.RegisterPermission("creative.fly", this);
            permission.RegisterPermission("creative.godmode", this);
            permission.RegisterPermission("creative.infammo", this);
            permission.RegisterPermission("creative.f1_give", this);
            permission.RegisterPermission("creative.vehicle", this);
            permission.RegisterPermission("creative.vip", this);
        }

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AutoSave"] = "Autosaving bases. There will be some lag.",
                ["InvalidEntity"] = "Invalid entity. Please look at your base before save.",
                ["ErrorSaveBase"] = "There was a problem trying to save your base.",
                ["SuccesfullSaveBase"] = "You've successfully saved.",
                ["NoClaimed"] = "You don't have any claimed area.",
                ["Cooldown"] = "You are in cooldown. Please wait before using this command again.",
                ["NoPerms"] = "You do not have permission to use this command.",
                ["OwnArea"] = "You already own a claimed area.",
                ["ClaimTerrain"] = "You should be in the terrain before claim a zone.",
                ["AreaOverlap"] = "You cannot claim a zone here; It overlaps with an existing zone.",
                ["NotClaimed"] = "You should claim a zone before using this.",
                ["ExitZone"] = "You left the plot owned by: {0}",
                ["EnterZone"] = "You entered the plot owned by: {0}",
                ["Outside"] = "You can not take photos outside your claimed area!",
                ["PhotoCaptured"] = "Succesfully captured base '{0}' photo!",
                ["BaseUpdated"] = "Base '{0}' was updated!",
                ["TeamLeader"] = "Only the team leader can do this action.",
                ["SaveLimitReached"] = "You have reached the maximum base saving limit.",
                ["BuildOutsideArea"] = "You can't build outside your area",
                ["BuildingInProccess"] = "Building upgrade is already in progress. Please wait until it finishes",
                ["GTFOmsg"] = "You're not allowed to be in this zone!",


                ["Menu_Top_Claim"] = "CLAIM PLOT",
                ["Menu_Top_UnClaim"] = "UNCLAIM PLOT",
                ["Menu_Top_Weather"] = "WEATHER SETTINGS",
                ["Menu_Top_Build"] = "BUILD SETTINGS",
                ["Menu_Top_Community"] = "COMMUNITY",
                ["Menu_Top_RequestResources"] = "REQUEST RESOURCES",
                ["Menu_VehicleManager"] = "VEHICLE MANAGER",
                ["Menu_AutoDoors"] = "AUTO DOORS",
                ["Menu_AutoDoors_CloseOpen"] = "Close/Open Doors",
                ["Menu_AutoDoors_Locks"] = "Add Codelock to Doors",
                ["Menu_AutoWindows"] = "AUTO WINDOWS",
                ["Menu_AutoElectricity"] = "AUTO ELECTRICITY",
                ["Menu_BuildingGrade"] = "BUILDING GRADE",
                ["Menu_BaseGrade"] = "BASE GRADE",
                ["Menu_EntitySettings"] = "ENTITY SETTINGS",
                ["Menu_EntitySettings_Furnaces"] = "Enable Furnaces",
                ["Menu_EntitySettings_Batteries"] = "Fill Batteries",
                ["Menu_EntitySettings_Stability"] = "Entity Stability",
                ["Menu_PersonalSettings"] = "PERSONAL SETTINGS",
                ["Menu_PersonalSettings_Noclip"] = "Noclip",
                ["Menu_PersonalSettings_GodMode"] = "God Mode",
                ["Menu_PersonalSettings_CBG"] = "Change Build Grade",
                ["Menu_PersonalSettings_InfAmmo"] = "Infinite Ammo",
                ["Menu_PersonalSettings_UiHud"] = "UI/MENU Hud",
                ["Menu_PlotSettings"] = "PLOT SETTINGS",
                ["Menu_PlotSettings_GTFO"] = "GTFO Mode",
                ["Menu_PlotSettings_RAID"] = "RAID Mode",
                ["Menu_PlotSettings_WTF"] = "Wipe to Foundations",
                ["Menu_PlotSettings_WD"] = "Wipe Deployables",
                ["Menu_PlotSettings_CP"] = "Clear Plot",
                ["Menu_Networking"] = "NETWORKING",
                ["Menu_Networking_Foundations"] = "FOUNDATIONS",
                ["Menu_Networking_Floor"] = "FLOOR",
                ["Menu_Networking_Walls"] = "WALLS",
                ["Menu_Networking_OBB"] = "OTHER BUILDING BLOCKS",
                ["Menu_Networking_Deployables"] = "DEPLOYABLES",
                
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AutoSave"] = "Autosaving bases. There will be some lag.",
                ["InvalidEntity"] = "Entidad invalida. Por favor mira a una construccion antes de guardar la base.",
                ["ErrorSaveBase"] = "Ocurrio un error guardando tu base.",
                ["SuccesfullSaveBase"] = "Base guardada!",
                ["NoClaimed"] = "No tienes un area reclamada!",
                ["Cooldown"] = "Por favor, espera para utilizar este comando nuevamente.",
                ["NoPerms"] = "No tienes permisos para utilizar este comando.",
                ["OwnArea"] = "Ya tienes un area reclamada!",
                ["ClaimTerrain"] = "Debes estar sobre el terreno para reclamar esta area.",
                ["AreaOverlap"] = "No puedes reclamar una zona aqui, esta colisiona con otra que ya existe!",
                ["NotClaimed"] = "Debes reclamar un area antes de usar esto.",
                ["ExitZone"] = "Has salido de la parcela de: {0}",
                ["EnterZone"] = "Has entrado en la parcela de: {0}",
                ["Outside"] = "No puedes tomar fotos fuera de tu parcela!",
                ["PhotoCaptured"] = "Se capturo exitosamente la foto de la base '{0}'!",
                ["BaseUpdated"] = "La Base '{0}' se ha actualizado!",
                ["TeamLeader"] = "Solo el lider del equipo puede hacer esta accion.",
                ["SaveLimitReached"] = "Haz llegado al limite maximo de guardado de bases.",
                ["BuildOutsideArea"] = "No puedes construir fuera de tu parcela!",
                ["BuildingInProccess"] = "La mejora de la construccion esta en proceso. Por favor espera a que termine.",
                ["GTFOmsg"] = "No tienes permisos para estar en esta parcela!",

                ["Menu_Top_Claim"] = "RECLAMAR PARCELA",
                ["Menu_Top_UnClaim"] = "UNCLAIM PLOT",
                ["Menu_Top_Weather"] = "AJUSTES DEL TIEMPO",
                ["Menu_Top_Build"] = "AJUSTES DE CONSTRUCCION",
                ["Menu_Top_Community"] = "COMUNIDAD",
                ["Menu_Top_RequestResources"] = "SOLICITAR RECURSOS",
                ["Menu_VehicleManager"] = "GESTOR DE VEHICULOS",
                ["Menu_AutoDoors"] = "PUERTAS AUTOMATICAS",
                ["Menu_AutoDoors_CloseOpen"] = "Cerrar/Abrir Puertas",
                ["Menu_AutoDoors_Locks"] = "Agregar candados",
                ["Menu_AutoWindows"] = "VENTANAS AUTOMATICAS",
                ["Menu_AutoElectricity"] = "ELECTRICIDAD AUTOMATICA",
                ["Menu_BuildingGrade"] = "GRADO DE CONSTRUCCION",
                ["Menu_BaseGrade"] = "GRADO DE LA BASE",
                ["Menu_EntitySettings"] = "AJUSTES DE ENTIDADES",
                ["Menu_EntitySettings_Furnaces"] = "Encender Hornos",
                ["Menu_EntitySettings_Batteries"] = "Rellenar Baterias",
                ["Menu_EntitySettings_Stability"] = "Estabilidad de Entidades",
                ["Menu_PersonalSettings"] = "AJUSTES PERSONALES",
                ["Menu_PersonalSettings_Noclip"] = "Noclip",
                ["Menu_PersonalSettings_GodMode"] = "Modo Dios",
                ["Menu_PersonalSettings_CBG"] = "Cambiar Grado de Construccion",
                ["Menu_PersonalSettings_InfAmmo"] = "Municion Infinita",
                ["Menu_PersonalSettings_UiHud"] = "UI/MENU Hud",
                ["Menu_PlotSettings"] = "AJUSTES DE PARCELA",
                ["Menu_PlotSettings_GTFO"] = "Modo GTFO",
                ["Menu_PlotSettings_RAID"] = "Modo RAID",
                ["Menu_PlotSettings_WTF"] = "Borrar hasta cimientos",
                ["Menu_PlotSettings_WD"] = "Borrar deployables",
                ["Menu_PlotSettings_CP"] = "Limpiar parcela",
                ["Menu_Networking"] = "NETWORKING",
                ["Menu_Networking_Foundations"] = "CIMIENTOS",
                ["Menu_Networking_Floor"] = "SUELO",
                ["Menu_Networking_Walls"] = "PAREDES",
                ["Menu_Networking_OBB"] = "OTRAS ENTIDADES DE CONSTRUCCION",
                ["Menu_Networking_Deployables"] = "DEPLOYABLES",
            }, this, "es");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AutoSave"] = "Автосохранение баз. Будет наблюдаться некоторое отставание.",
                ["InvalidEntity"] = "Неверный объект. Пожалуйста, посмотрите конструкцию перед сохранением базы.",
                ["ErrorSaveBase"] = "Произошла ошибка при сохранении вашей базы.",
                ["SuccesfullSaveBase"] = "Хранимая база!",
                ["NoClaimed"] = "У вас нет заявленного участка!",
                ["Cooldown"] = "Пожалуйста, подождите, чтобы использовать эту команду снова.",
                ["NoPerms"] = "У вас нет прав на использование этой команды.",
                ["OwnArea"] = "У вас уже есть заявленный участок!",
                ["ClaimTerrain"] = "Вы должны находиться на земле, чтобы претендовать на эту территорию.",
                ["AreaOverlap"] = "Вы не можете объявить здесь зону, она сталкивается с существующей зоной!",
                ["NotClaimed"] = "Прежде чем использовать это, вы должны заявить о своем участке..",
                ["ExitZone"] = "Вы покинули домен: eric: {0}",
                ["EnterZone"] = "Этот участок принадлежит компании: {0}",
                ["Outside"] = "Вы не можете фотографировать за пределами своего участка!",
                ["PhotoCaptured"] = "Фотография базы была успешно сделана '{0}'!",
                ["BaseUpdated"] = "База '{0}' была обновлена!",
                ["TeamLeader"] = "Это действие может предпринять только руководитель группы..",
                ["SaveLimitReached"] = "Вы достигли максимального предела хранения базы..",
                ["BuildOutsideArea"] = "Вы не можете строить за пределами своего участка!",
                ["BuildingInProccess"] = "Работы находятся в процессе. Дождитесь их завершения..",
                ["GTFOmsg"] = "У вас нет разрешения находиться на этом участке!",

                ["Menu_Top_Claim"] = "ЗАПРОСИТЬ УЧАСТОК",
                ["Menu_Top_UnClaim"] = "UNCLAIM PLOT",
                ["Menu_Top_Weather"] = "НАСТРОЙКИ ПОГОДЫ",
                ["Menu_Top_Build"] = "ПАРАМЕТРЫ СТРОИТЕЛЬСТВА",
                ["Menu_Top_Community"] = "СООБЩЕСТВО",
                ["Menu_Top_RequestResources"] = "ЗАПРОСИТЬ РЕСУРСЫ",
                ["Menu_VehicleManager"] = "АВТОМОБИЛЬНЫЙ МЕНЕДЖЕР",
                ["Menu_AutoDoors"] = "АВТОМАТИЧЕСКИЕ ДВЕРИ",
                ["Menu_AutoDoors_CloseOpen"] = "Закрыть/открыть двери",
                ["Menu_AutoDoors_Locks"] = "Добавить замки",
                ["Menu_AutoWindows"] = "АВТОМАТИЧЕСКИЕ ОКНА",
                ["Menu_AutoElectricity"] = "АВТОМАТИЧЕСКОЕ ЭЛЕКТРИЧЕСТВО",
                ["Menu_BuildingGrade"] = "СТРОИТЕЛЬНЫЙ УРОВЕНЬ",
                ["Menu_BaseGrade"] = "ОСНОВНОЙ Сорт",
                ["Menu_EntitySettings"] = "НАСТРОЙКИ ОБЪЕКТОВ",
                ["Menu_EntitySettings_Furnaces"] = "Включите духовки",
                ["Menu_EntitySettings_Batteries"] = "Зарядите батареи",
                ["Menu_EntitySettings_Stability"] = "Стабильность сущности",
                ["Menu_PersonalSettings"] = "ПЕРСОНАЛЬНЫЕ НАСТРОЙКИ",
                ["Menu_PersonalSettings_Noclip"] = "Noclip",
                ["Menu_PersonalSettings_GodMode"] = "Режим Бога",
                ["Menu_PersonalSettings_CBG"] = "Изменить строительную категорию",
                ["Menu_PersonalSettings_InfAmmo"] = "Бесконечные боеприпасы",
                ["Menu_PersonalSettings_UiHud"] = "UI/MENU Hud",
                ["Menu_PlotSettings"] = "КОРРЕКТИРОВКИ СЮЖЕТА",
                ["Menu_PlotSettings_GTFO"] = "Режим GTFO",
                ["Menu_PlotSettings_RAID"] = "Режим RAID",
                ["Menu_PlotSettings_WTF"] = "Стереть до основания",
                ["Menu_PlotSettings_WD"] = "Очистить развертываемые объекты",
                ["Menu_PlotSettings_CP"] = "Четкий сюжет",
                ["Menu_Networking"] = "СЕТЬ",
                ["Menu_Networking_Foundations"] = "ФУНДАМЕНТЫ",
                ["Menu_Networking_Floor"] = "ПОЛ",
                ["Menu_Networking_Walls"] = "СТЕНЫ",
                ["Menu_Networking_OBB"] = "ПРОЧИЕ СТРОИТЕЛЬНЫЕ ПРЕДПРИЯТИЯ",
                ["Menu_Networking_Deployables"] = "РАЗВЕРТЫВАЕМЫЕ ОБЪЕКТЫ",
            }, this, "ru");
        }

        private void SendMessage(BasePlayer player, string key, params object[] args) {
            if (player == null) return;

            string fullLanguage = lang.GetLanguage(player.UserIDString);
            string languageCode = fullLanguage.Split('-')[0];

            lang.SetLanguage(languageCode, player.UserIDString);

            player.ChatMessage(string.Format(GetTranslation(key, player), args));
        }

        private string GetTranslation(string key, BasePlayer player = null) {
            return lang.GetMessage(key, this, player?.UserIDString);
        }
    
        private void LoadCommands()
        {
            foreach (var command in _config.CommandList_Claim)
            {
                cmd.AddChatCommand(command, this, "ClaimZone");
            }

            foreach (var command in _config.CommandList_UnClaim)
            {
                cmd.AddChatCommand(command, this, "UnclaimZone");
            }

            foreach (var command in _config.CommandList_Fly)
            {
                cmd.AddChatCommand(command, this, "NoClip");
            }

            foreach (var command in _config.CommandList_God)
            {
                cmd.AddChatCommand(command, this, "GodModeCmd");
            }

            foreach (var command in _config.CommandList_BGrade)
            {
                cmd.AddChatCommand(command, this, "bgradecmd");
            }

            foreach (var command in _config.CommandList_Menu)
            {
                cmd.AddChatCommand(command, this, "OpenMenuCmd");
            }

            if (_config.weather_menu)
            {
                foreach (var command in _config.CommandList_WeatherMenu)
                {
                    cmd.AddConsoleCommand("creative.weather", this, "CreativeWeatherCmd");
                    cmd.AddChatCommand(command, this, "CreativeWeatherCmdChat");
                }
            }

            foreach (var command in _config.CommandList_SaveBase)
            {
                cmd.AddChatCommand(command, this, "Save");
            }

            foreach (var command in _config.CommandList_LoadBase)
            {
                cmd.AddChatCommand(command, this, "LoadCommand");
            }

            if (_config.info_menu)
            {
                foreach (var command in _config.CommandList_InfoPanel)
                {
                    cmd.AddChatCommand(command, this, "InfoPanelCmd");
                }
            }

            cmd.AddConsoleCommand("load", this, "LoadConsoleCommand");
            cmd.AddChatCommand("invite", this, "team_invite");
            if (_config.buildcost_hud)
                cmd.AddChatCommand("cost", this, "CmdCost");
            cmd.AddChatCommand("setlobby", this, "LobbyCmd");
        }

        void OnServerInitialized(bool initial)
        {
            if (!InitializeBaseInfo())
                return;
            
            Instance = this;
            RegisterPermissions();
            LoadDefaultMessages();

            foreach (var player in BasePlayer.activePlayerList)
            {
                UpdatePlayerSettingsClass(player);
                uiBgradeHud(player);
            }

            NextTick(() =>{ LoadCommands(); });
            ImportImages();

            Task.Run(() => InitializeDatabaseAsync()).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Puts("Error initializing database: " + task.Exception?.Message);
                }
            });

            foreach (ItemBlueprint bp in ItemManager.GetBlueprints())            
                bp.workbenchLevelRequired = 0;
            
            foreach (ItemBlueprint bp in ItemManager.bpList)
            {
                BPs.Add(bp.targetItem.itemid);
            }
            
            if (_config.cvar_disable_terrain_violation_kick)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "antihack.terrain_protection 0");
                 Puts("Terrain Violation Kick Disabled.");
            }
            
            if (_config.cvar_disable_decay)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "decay.scale 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "decay.upkeep 0");
                 Puts("Decay & Upkeep Disabled.");
            }

            if (_config.cvar_always_day){
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "env.progresstime false");
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "env.time 12");
                Puts("Always Day Enabled.");
            }

            ConsoleSystem.Run(ConsoleSystem.Option.Server, $"creative.allUsers {_config.cvar_creative_allusers}");
            ConsoleSystem.Run(ConsoleSystem.Option.Server, $"creative.freePlacement {_config.cvar_creative_freeplacement}");
            ConsoleSystem.Run(ConsoleSystem.Option.Server, $"creative.freeBuild {_config.cvar_creative_freebuild}");
            ConsoleSystem.Run(ConsoleSystem.Option.Server, $"creative.freeRepair {_config.cvar_creative_freerepair}");


            if (saveTimer == null)
                saveTimer = timer.Every(5f, ProcessSaveQueue);
        }
        
        private bool UnloadCalled = false;
        void CallUnload()
        {
            if (UnloadCalled)
                return;
            
            UnloadCalled = true;

            var dataToSave = new Dictionary<ulong, PlotManagerData>();

            foreach (var entry in activeZones)
            {
                ulong zoneID = entry.Key;
                PlotManager plotManager = entry.Value;

                if (plotManager != null)
                {
                    var data = new PlotManagerData
                    {
                        zoneID = plotManager.zoneID,
                        CenterZone = plotManager.CenterZone,
                        GTFO = plotManager.GTFO
                    };

                    dataToSave[zoneID] = data;
                }
            }

            RemoveAllMarkers();

            Interface.Oxide.DataFileSystem.WriteObject("Creative/activeZones", dataToSave);

            foreach (var kvp in activeZones.ToList())
            {
                UnityEngine.Object.DestroyImmediate(kvp.Value.gameObject);
            }

            activeZones.Clear();
            KillAllSpawnedEntities();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "BGradeHudPanel");
                CuiHelper.DestroyUi(player, "BGradeHudPanel2");
                CuiHelper.DestroyUi(player, "CostPanel");
                CuiHelper.DestroyUi(player, "LoadingText");
                CuiHelper.DestroyUi(player, "Info_Panel");
                CuiHelper.DestroyUi(player, "MainMenu_Panel");
                CuiHelper.DestroyUi(player, "Build_Panel");
                CuiHelper.DestroyUi(player, "Networking");
                CuiHelper.DestroyUi(player, "Plot_Panel");
                CuiHelper.DestroyUi(player, "Personal_Panel");
                CuiHelper.DestroyUi(player, "Entity_Panel");
                CuiHelper.DestroyUi(player, "AutoDoors_Panel");
                CuiHelper.DestroyUi(player, "AutoWindows_Panel");
                CuiHelper.DestroyUi(player, "AutoElectricity_Panel");
                CuiHelper.DestroyUi(player, "BuildingUpgrade_Panel");
                CuiHelper.DestroyUi(player, "BaseGradeUpdate_Panel");
                CuiHelper.DestroyUi(player, "VehicleManager_Panel");
                CuiHelper.DestroyUi(player, "Community_Panel");
                CuiHelper.DestroyUi(player, "WeatherPanel");
                CuiHelper.DestroyUi(player, "LoadingText");
                CuiHelper.DestroyUi(player, "Info_Panel");

                ClearAll(player);
            }
        }

        void OnServerShutdown()
        {
            CallUnload();
            if (BPs != null)
                Facepunch.Pool.FreeList(ref BPs);

            foreach (var player in BasePlayer.activePlayerList)
                if (playerSettings.ContainsKey(player.userID))
                    playerSettings.Remove(player.userID);
        }

        void Unload()
        {
            CallUnload();
            if (BPs != null)
                Facepunch.Pool.FreeList(ref BPs);

            foreach (var player in BasePlayer.activePlayerList)
                if (playerSettings.ContainsKey(player.userID))
                    playerSettings.Remove(player.userID);
        }

        void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "Creative")
            {
                if (BPs != null)
                    Facepunch.Pool.FreeList(ref BPs);

                CallUnload();

                foreach (var player in BasePlayer.activePlayerList)
                    if (playerSettings.ContainsKey(player.userID))
                        playerSettings.Remove(player.userID);
            }
        }

        void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            try
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;

                AntiExplosives(player, entity, item);
            }
            catch (Exception e) { LogErrors(e.Message, "OnExplosiveDropped"); }
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            try
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;

                AntiExplosives(player, entity, item);
            }
            catch (Exception e) { LogErrors(e.Message, "OnExplosiveThrown"); }
        }

        void AntiExplosives(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            try
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;

                entity.OwnerID = player.userID;
                ulong closestZoneID = FindClosestZone(player);

                if (closestZoneID != player.userID)
                {
                    entity.Kill();
                    return;
                }

                if (activeZones != null && activeZones.Count > 0)
                {
                    float closestDistance = float.MaxValue;
                    PlotManager closestZone = null;

                    foreach (var kvp in activeZones)
                    {
                        var existingZone = kvp.Value;
                        Vector3 existingZoneCenterPos = existingZone.GetZoneCenter();

                        if (existingZoneCenterPos == Vector3.zero)
                        {
                            entity.Kill();
                            return;
                        }

                        float distanceToZone = Vector3.Distance(existingZoneCenterPos, player.transform.position);

                        if (distanceToZone < closestDistance)
                        {
                            closestDistance = distanceToZone;
                            closestZone = existingZone;
                        }
                    }

                    if (closestZone != null)
                    {
                        if (closestZone.IsPlayerAllowed(player))
                        {
                            float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                            if (Vector3.Distance(closestZone.GetZoneCenter(), player.transform.position) > plot_radius)
                            {
                                entity.Kill();
                                return;
                            }
                        }

                        if (!PlayerOwnedZone(player, closestZone.GetOwner()))
                        {
                            entity.Kill();
                            return;
                        }
                    }
                }
            }
            catch (Exception e) { LogErrors(e.Message, "AntiExplosives"); }
        }

        private bool PlayerOwnedZone(BasePlayer player, ulong ownerUID)
		{
            if (!player || ownerUID == null)
                return false;

            if (player.Team != null)
            {
                if (player.Team.members.Contains(ownerUID))
                {
                    return true;
                }
            }
            else
            {
                if (player.userID == ownerUID)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            var heldEntity = projectile.GetItem();

            if (!playerSettings.ContainsKey(player.userID) || !playerSettings[player.userID].InfiniteAmmo || heldEntity == null || !permission.UserHasPermission(player.UserIDString, "creative.infammo"))
                return;

            heldEntity.condition = heldEntity.info.condition.max;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            try
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;

                ulong closestZoneID = FindClosestZone(player);

                if (closestZoneID != player.userID)
                {
                    entity.Kill();
                    return;
                }

                if (activeZones != null && activeZones.Count > 0)
                {
                    float closestDistance = float.MaxValue;
                    PlotManager closestZone = null;

                    foreach (var kvp in activeZones)
                    {
                        var existingZone = kvp.Value;
                        Vector3 existingZoneCenterPos = existingZone.GetZoneCenter();

                        if (existingZoneCenterPos == Vector3.zero)
                        {
                            entity.Kill();
                            return;
                        }

                        float distanceToZone = Vector3.Distance(existingZoneCenterPos, player.transform.position);

                        if (distanceToZone < closestDistance)
                        {
                            closestDistance = distanceToZone;
                            closestZone = existingZone;
                        }
                    }

                    if (closestZone != null)
                    {
                        if (closestZone.IsPlayerAllowed(player))
                        {
                            float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                            if (Vector3.Distance(closestZone.GetZoneCenter(), player.transform.position) > plot_radius)
                            {
                                entity.Kill();
                                return;
                            }
                        }

                        if (!PlayerOwnedZone(player, closestZone.GetOwner()))
                        {
                            entity.Kill();
                            return;
                        }
                    }
                }

                if (entity is TimedExplosive)
                {
                    TimedExplosive rocket = (TimedExplosive)entity;

                    Rigidbody rigidbody = rocket.GetComponent<Rigidbody>();
                    if (rigidbody == null)
                    {
                        rigidbody = rocket.gameObject.AddComponent<Rigidbody>();
                        rigidbody.isKinematic = true;
                    }

                    Collider col = rocket.GetComponent<Collider>();
                    if (col == null)
                    {
                        BoxCollider boxCollider = rocket.gameObject.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                    }
                }

                var heldEntity = player.GetActiveItem();
                var explosive = entity as TimedExplosive;

                entity.OwnerID = player.userID;

                if (heldEntity == null)
                    return;

                var weapon = heldEntity.GetHeldEntity() as BaseProjectile;

                if (weapon == null || !playerSettings.ContainsKey(player.userID) || !playerSettings[player.userID].InfiniteAmmo || !permission.UserHasPermission(player.UserIDString, "creative.infammo"))
                    return;

                heldEntity.condition = heldEntity.info.condition.max;
                weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                weapon.SendNetworkUpdateImmediate();
            }
            catch (Exception e) { LogErrors(e.Message, "OnRocketLaunched"); }
        }
        
        void OnItemDropped(Item item, BaseEntity entity)
        {
            NextTick(() =>
            {
                if (item != null)
                    item.Remove();

                if (entity != null && !entity.IsDestroyed)
                    entity.Kill();
            });
        }

        void OnPlayerCorpseSpawned(BasePlayer player, PlayerCorpse corpse)
        {
            NextTick(() =>
            {
                if (corpse != null && !corpse.IsDestroyed)
                    corpse.Kill();
            });
        }

        void ClearCorpseInventory(BaseEntity entity)
        {
            var corpse = entity as LootableCorpse;
            if (corpse == null) return;
            
            foreach (var container in corpse.containers)
                container.itemList.Clear();
        }

        private void RocketsCollider(BaseEntity entity)
        {
            if (entity == null) return;
            
            try
            {
                if (entity is TimedExplosive)
                {
                    TimedExplosive rocket = (TimedExplosive)entity;
                    Collider col = rocket.GetComponent<Collider>();
                    if (col == null)
                    {
                        BoxCollider boxCollider = rocket.gameObject.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                    }
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "RocketsCollider"); }
        }

        private void PreventItemSpawn(BaseEntity entity)
        {
            if (entity == null) return;

            try
            {
                foreach (string bloked_items in _config.blocked_items)
                {
                    if (entity is DroppedItemContainer || entity.PrefabName.Contains(bloked_items) || entity.PrefabName.Contains("fireworks") || entity.PrefabName.Contains("igniter.deployed") || entity.PrefabName.Contains("drone") || entity.PrefabName.Contains("smoke"))
                        if (!entity.IsDestroyed)
                            entity.Kill();
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "PreventItemSpawn"); }
        }

        private void VehicleFuel(BaseEntity entity)
        {
            if (entity == null) return;

            try
            {
                if (entity is BaseVehicle vehicle && vehicle.GetFuelSystem() is EntityFuelSystem fuelSystem)
                {
                    NextTick(() =>
                    {
                        var fuelContainer = fuelSystem.GetFuelContainer();
                        if (fuelContainer != null)
                        {
                            var fuelItem = fuelContainer.inventory.FindItemByItemID(fuelContainer.allowedItem.itemid);

                            if (fuelItem == null)
                            {
                                fuelContainer.inventory.AddItem(fuelContainer.allowedItem, 500);
                            }
                            else if (fuelItem.amount != 500)
                            {
                                fuelItem.amount = 500;
                                fuelItem.MarkDirty();
                            }
                        }
                    });
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "VehicleFuel"); }
        }

        private void TCFiller(BaseEntity entity)
        {
            if (entity == null) return;

            try
            {
                var toolCupboard = entity as BuildingPrivlidge;
                if (toolCupboard != null)
                {
                    NextTick(() =>
                    {
                        if (!toolCupboard.IsDestroyed)
                        {
                            var woodItem = ItemManager.CreateByItemID(-151838493, 999999); 
                            var metalFragmentsItem = ItemManager.CreateByItemID(69511070, 999999); 
                            var stoneItem = ItemManager.CreateByItemID(-2099697608, 999999); 
                            var highQualityMetalItem = ItemManager.CreateByItemID(317398316, 999999); 

                            if (woodItem != null)
                            {
                                woodItem.MoveToContainer(toolCupboard.inventory);
                            }
                            if (metalFragmentsItem != null)
                            {
                                metalFragmentsItem.MoveToContainer(toolCupboard.inventory);
                            }
                            if (stoneItem != null)
                            {
                                stoneItem.MoveToContainer(toolCupboard.inventory);
                            }
                            if (highQualityMetalItem != null)
                            {
                                highQualityMetalItem.MoveToContainer(toolCupboard.inventory);
                            }
                        }
                    });
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "TCFiller"); }
        }

        private const string WindowBarsPrefab = "assets/prefabs/building/door.hinged/door.hinged.toptier.prefab";
        List<string> entity_sockets = new List<string>{ "wall.window", "wall.frame", "wall.doorway" };

        private string GenerateRandomCode()
        {
            System.Random random = new System.Random();
            int code = random.Next(1000, 10000);
            return code.ToString("0000");
        }

        private void HandleEntityBuiltOrSpawned(BaseEntity entity, BasePlayer player)
        {
            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (playerSettings[player.userID].CodeLockDoor && (entity.ShortPrefabName.Contains("door.double") || entity.ShortPrefabName.Contains("door.hinged")))
                PlaceCodelockOnDoor(entity, player);

            if (!entity_sockets.Contains(entity.ShortPrefabName))
                return;

            PlaceDeployableOnSocketAsync(entity, player);
        }

        private void PlaceCodelockOnDoor(BaseEntity doorEntity, BasePlayer player)
        {
            BaseEntity codelockEntity = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab", Vector3.zero, Quaternion.identity, true);

            if (codelockEntity == null)
                return;

            var availableSockets = PrefabAttribute.server.FindAll<Socket_Base>(doorEntity.prefabID);

            codelockEntity.SetParent(doorEntity, doorEntity.GetSlotAnchorName(BaseEntity.Slot.Lock));
            codelockEntity.OwnerID = doorEntity.OwnerID;
            codelockEntity.OnDeployed(doorEntity, player, null);
            codelockEntity.Spawn();


            CodeLock codelock = codelockEntity.GetComponent<CodeLock>();
            if (codelock == null)
                return;

            string randomCode = GenerateRandomCode();
            codelock.code = randomCode;
            codelock.SetFlag(CodeLock.Flags.Locked, true);
            codelock.SendNetworkUpdate();

            codelock.whitelistPlayers.Add(player.userID);
            var playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            if (playerTeam != null)
            {
                foreach (var memberId in playerTeam.members)
                {
                    codelock.whitelistPlayers.Add(memberId);
                }
            }
        }

        private void PlaceDeployableOnSocketAsync(BaseEntity parentEntity, BasePlayer player)
        {
            List<string> deployableNames = new List<string>();
            string socket_name = "window-female";

            switch (parentEntity.ShortPrefabName)
            {
                case "wall.window":
                    socket_name = "window-female";
                    deployableNames.Add(playerSettings[player.userID].WindowPrefab);
                    deployableNames.Add(playerSettings[player.userID].EmbrasurePrefab);
                break;

            case "wall.doorway":
                    socket_name = "wall-female";
                    deployableNames.Add(playerSettings[player.userID].DoorPrefab);
                break;

            case "wall.frame":
                    socket_name = "wall-female";
                    deployableNames.Add(playerSettings[player.userID].DoubleDoorPrefab);
                break;
            }

            foreach (var deployableName in deployableNames)
            {
                if (deployableName.Contains("none"))
                    continue;

                if ((parentEntity.ShortPrefabName.Contains("wall.doorway") || parentEntity.ShortPrefabName.Contains("wall.frame")) && !playerSettings[player.userID].AutoDoors)
                    continue;

                if (parentEntity.ShortPrefabName.Contains("wall.window") && !playerSettings[player.userID].AutoWindows)
                    continue;

                BaseEntity deployablePrefab = GameManager.server.CreateEntity(deployableName, Vector3.zero, Quaternion.identity, true);

                if (deployablePrefab == null)
                    continue;

                var availableSockets = PrefabAttribute.server.FindAll<Socket_Base>(parentEntity.prefabID);

                Socket_Base targetSocket = FindSocket(availableSockets, socket_name);

                if (targetSocket == null)
                    continue;

                deployablePrefab.SetParent(parentEntity, targetSocket.socketName);

                if (socket_name == "window-female")
                {
                    deployablePrefab.transform.localPosition = targetSocket.position;
                }

                if (deployablePrefab is BuildingBlock || deployablePrefab.ShortPrefabName.Contains("door") || deployablePrefab.ShortPrefabName.Contains("wall.frame") || deployablePrefab.ShortPrefabName.Contains("wall.window"))
                {
                    var decayEntity = deployablePrefab as DecayEntity;
                    if (decayEntity != null)
                    {
                        uint buildingId = BuildingManager.server.NewBuildingID();
                        decayEntity.AttachToBuilding(buildingId);
                    }
                }

                deployablePrefab.OwnerID = parentEntity.OwnerID;
                deployablePrefab.Spawn();

                if (playerSettings[player.userID].CodeLockDoor && (deployablePrefab.ShortPrefabName.Contains("door.double") || deployablePrefab.ShortPrefabName.Contains("door.hinged")))
                    PlaceCodelockOnDoor(deployablePrefab, player);
            }
        }

        private Socket_Base FindSocket(Socket_Base[] sockets, string socketName)
        {
            foreach (var socket in sockets)
            {
                if (!socket.male && socket.socketName.Contains(socketName))
                {
                    return socket;
                }
            }
            return null;
        }

        private object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null || !player.userID.IsSteamId() || !permission.UserHasPermission(player.UserIDString, "creative.use") || !playerSettings.ContainsKey(player.userID))
                return null;

            if (playerSettings[player.userID].GodMode && permission.UserHasPermission(player.UserIDString, "creative.godmode"))
            {
                info.damageTypes = new DamageTypeList();
                return true;
            }

            return null;
        }

        void OnFireBallDamage(FireBall fire, BaseCombatEntity entity, HitInfo info)
        {
            info.damageTypes.ScaleAll(0f);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null)
                return;

            try
            {
                BasePlayer attacker = hitInfo.Initiator as BasePlayer;
                if (attacker == null)
                    return;

                bool isProtectedEntity = entity is BuildingBlock ||
                                        entity is IOEntity ||
                                        entity is StorageContainer ||
                                        entity is Door ||
                                        entity is BuildingPrivlidge ||
                                        entity is CodeLock ||
                                        entity is BaseLock ||
                                        entity is ContainerIOEntity ||
                                        entity is Signage ||
                                        entity is SleepingBag ||
                                        entity is AutoTurret ||
                                        entity is CCTV_RC ||
                                        entity is Deployable ||
                                        entity is SimpleBuildingBlock ||
                                        entity is VendingMachine;

                if (isProtectedEntity)
                {
                    ulong ownerID = entity.OwnerID;

                    if (!playerSettings.ContainsKey(ownerID))
                        return;

                    if (ownerID != attacker.userID && playerSettings.ContainsKey(ownerID) && !playerSettings[ownerID].Raid)
                    {
                        if (attacker.Team == null || !attacker.Team.members.Contains(ownerID))
                        {
                            hitInfo.damageTypes.ScaleAll(0f);
                        }
                    }
                }
                else if (entity is BaseVehicle vehicle)
                {
                    BasePlayer vehicleOwner = null;
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (player.userID == vehicle.OwnerID)
                        {
                            vehicleOwner = player;
                            break;
                        }
                    }

                    if (vehicleOwner == null || attacker.Team == null || vehicleOwner.Team == null)
                    {
                        hitInfo.damageTypes.ScaleAll(0f);
                        return;
                    }

                    if (!vehicleOwner.Team.members.Contains(attacker.userID))
                    {
                        hitInfo.damageTypes.ScaleAll(0f);
                    }
                }
            }
            catch (Exception e) { LogErrors(e.Message, "OnEntityTakeDamage"); }
        }

    
        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            try
            {
                var player = planner.GetOwnerPlayer();
                if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use")) return;

                NextTick(() =>
                {
                    if (gameObject == null) return;

                    var buildingBlock = gameObject.GetComponent<BuildingBlock>();
                    var deployable = gameObject.GetComponent<Deployable>();

                    if (!deployable && buildingBlock != null && playerSettings.ContainsKey(player.userID) && playerSettings[player.userID].BuildingGrade)
                    {
                        if (buildingBlock != null)
                        {
                            if (playerSettings[player.userID] != null)
                            {
                                var settings = playerSettings[player.userID];

                                if (settings.CurrentGrade >= 0)
                                {
                                    buildingBlock.ChangeGrade((BuildingGrade.Enum)settings.CurrentGrade);
                                    buildingBlock.SetGrade((BuildingGrade.Enum)settings.CurrentGrade);
                                    buildingBlock.SetHealthToMax();
                                }

                                if (settings.CurrentSkin != 0)
                                {
                                    if (settings.CurrentContainerSkin != 0)
                                        buildingBlock.playerCustomColourToApply = (uint)settings.CurrentContainerSkin;

                                    buildingBlock.ChangeGradeAndSkin(buildingBlock.grade, (ulong)settings.CurrentSkin, true, true);
                                }

                                if (!settings.EntityStability && !buildingBlock.grounded)
                                {
                                    buildingBlock.grounded = true;
                                }

                                buildingBlock.UpdateSkin();
                                buildingBlock.SendNetworkUpdateImmediate();
                            }
                        }
                    }
                });
            }
            catch (Exception ex) { LogErrors(ex.Message, "OnEntityBuilt"); }
        }

        void OnItemDeployed(Deployer deployer, BaseEntity entity, BaseEntity slotEntity)
        {
            var player = deployer.GetOwnerPlayer();
            if (player == null) return;

            foreach (string blocked_item in _config.blocked_items)
            {
                if (entity.PrefabName.Contains(blocked_item) || entity.PrefabName.Contains("fireworks"))
                {
                    if (!entity.IsDestroyed)
                    {
                        entity.Kill();
                    }
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable a1)
        {
            BaseEntity entity = a1 as BaseEntity;
            if (entity == null)
                return;

            NextTick(() =>
            {
                if (entity == null || entity.IsDestroyed) return;

                if (entity.ShortPrefabName.Contains("fireball") && !entity.IsDestroyed)
                    entity.Kill();
                    
                ClearCorpseInventory(entity);
                PreventItemSpawn(entity);
                RocketsCollider(entity);
                VehicleFuel(entity);
                TCFiller(entity);
                ElectricityManager(entity);


                var playerBuild = BasePlayer.FindByID(entity.OwnerID);

                if (playerBuild == null || !permission.UserHasPermission(playerBuild.UserIDString, "creative.use"))
                    return;

                if (!playerSettings.ContainsKey(playerBuild.userID))
                    return;

                if (playerBuild != null && !playerSettings[playerBuild.userID].LoadingBase)
                {
                    if (playerSettings.ContainsKey(playerBuild.userID) && !playerSettings[playerBuild.userID].LoadingBase)
                        HandleEntityBuiltOrSpawned(entity, playerBuild);
                    
                    if (playerSettings[playerBuild.userID].FillBatteries && entity.ShortPrefabName.Contains("battery"))
                        FillBatteries(playerBuild);

                    if (playerSettings[playerBuild.userID].AutoEntity && entity.ShortPrefabName.Contains("furnace"))
                        ToggleFurnaces(playerBuild, playerSettings[playerBuild.userID].AutoEntity);
                }
            });
        }

        public void ApplyKit(BasePlayer player)
        {
            if (player?.inventory == null) return;

            foreach (KitData kitData in _config.kitdata)
            {
                ItemContainer container = GetPlayerContainer(player, kitData.itemcontainer);
                if (container == null || container.itemList == null)
                    continue;

                if (HasItemWithSkin(container, kitData.id, kitData.skinid))
                    continue;

                Item item = ItemManager.CreateByItemID(kitData.id, 1, kitData.skinid);
                if (item == null)
                    continue;

                if (!item.MoveToContainer(container))
                    item.Remove();
            }
        }

        private ItemContainer GetPlayerContainer(BasePlayer player, ItemContainerType containerType)
        {
            switch (containerType)
            {
                case ItemContainerType.Main:
                    return player.inventory.containerMain;
                case ItemContainerType.Belt:
                    return player.inventory.containerBelt;
                case ItemContainerType.Wear:
                    return player.inventory.containerWear;
                default:
                    return null;
            }
        }

        private bool HasItemWithSkin(ItemContainer container, int itemId, ulong skinId)
        {
            foreach (Item item in container.itemList)
            {
                if (item.info.itemid == itemId && item.skin == skinId)
                {
                    return true;
                }
            }
            return false;
        }

        private void CmdCost(BasePlayer player, string command, string[] args)
        {
            if (!_config.buildcost_hud) return;
            
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
            {
                SendMessage(player, "NoPerms");
                return;
            }

            if (!playerSettings.ContainsKey(player.userID))
                return;

            playerSettings[player.userID].BCostHud = !playerSettings[player.userID].BCostHud;
            
            if (!playerSettings[player.userID].BCostHud)
                CuiHelper.DestroyUi(player, "CostPanel");

            UpdateCost(player, true);
        }

        private void InfoPanelCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            Open_InfoMenu(player);
        }

        private void DisplayCostsToPlayer(BasePlayer player, Dictionary<string, int> buildingBlockCosts, Dictionary<string, int> deployableCosts)
        {
            var totalCostMessage = new StringBuilder();

            totalCostMessage.AppendLine("Building Block Costs:");
            foreach (var cost in buildingBlockCosts)
            {
                totalCostMessage.AppendLine($"{cost.Key}: {cost.Value}");
            }

            totalCostMessage.AppendLine("\nDeployable Costs:");
            foreach (var cost in deployableCosts)
            {
                totalCostMessage.AppendLine($"{cost.Key}: {cost.Value}");
            }

            SendReply(player, totalCostMessage.ToString());
        }

       private Dictionary<ulong, int> deployableLookup = new Dictionary<ulong, int>();

        private async Task PeriodicUpdateCost(BasePlayer player)
        {
            while (player != null && player.IsConnected)
            {
                await UpdateCost(player);
                await Task.Delay(10000);
            }
        }

        private async Task UpdateCost(BasePlayer player, bool is_cmd = false)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use") || 
                !playerSettings.TryGetValue(player.userID, out var settings) || 
                !settings.BCostHud || !_config.buildcost_hud)
                return;

            Vector3 zoneCenterPos = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (zoneCenterPos == Vector3.zero)
                return;

            float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;

            var entities = BaseNetworkable.serverEntities
                .OfType<BaseEntity>()
                .Where(e => Vector3.Distance(e.transform.position, zoneCenterPos) <= plot_radius)
                .ToList();

            var buildingBlockCosts = new Dictionary<string, int>();
            var deployableCosts = new Dictionary<string, int>();

            foreach (var entity in entities)
            {
                if (entity is BuildingBlock buildingBlock)
                {
                    AddCosts(buildingBlockCosts, GetBuildingBlockCosts(buildingBlock));
                }
                else if (entity is BaseEntity deployable && deployable.PrefabName.Contains("/"))
                {
                    AddDeployableCostsToBuildingCosts(buildingBlockCosts, deployableCosts, GetDeployableCosts(deployable));
                }
            }

            if (!settings.LoadingBase)
                uiBuildCost(player, buildingBlockCosts, deployableCosts);

            if (is_cmd)
                DisplayCostsToPlayer(player, buildingBlockCosts, deployableCosts);
        }

        private Dictionary<string, int> GetBuildingBlockCosts(BuildingBlock block)
        {
            var costs = new Dictionary<string, int>();

            if (block?.blockDefinition?.grades == null)
                return costs;

            var grade = block.blockDefinition.grades.FirstOrDefault(g => g.gradeBase.type == block.grade);
            if (grade == null)
                return costs;

            AddCostsToDictionary(costs, grade.CostToBuild());
            return costs;
        }

        private void AddCostsToDictionary(Dictionary<string, int> costs, List<ItemAmount> itemAmounts)
        {
            foreach (var itemAmount in itemAmounts)
            {
                var ingredientName = itemAmount.itemDef.displayName.english.ToLower();
                if (costs.ContainsKey(ingredientName))
                {
                    costs[ingredientName] += (int)itemAmount.amount;
                }
                else
                {
                    costs[ingredientName] = (int)itemAmount.amount;
                }
            }
        }

        private void BuildDeployableLookups()
        {
            deployableLookup.Clear();

            foreach (ItemDefinition itemDef in ItemManager.GetItemDefinitions())
            {
                if (itemDef == null) continue;

                ItemModDeployable modDeployable = itemDef.GetComponent<ItemModDeployable>();
                if (modDeployable != null)
                {
                    deployableLookup[modDeployable.entityPrefab.resourceID] = itemDef.itemid;
                }
            }
        }

        private Dictionary<string, int> GetDeployableCosts(BaseEntity deployable)
        {
            if (deployableLookup.Count == 0)
                BuildDeployableLookups();

            if (!deployableLookup.TryGetValue(deployable.prefabID, out var itemId))
                return new Dictionary<string, int>();

            var itemDef = ItemManager.FindItemDefinition(itemId);
            if (itemDef?.Blueprint?.ingredients == null)
                return new Dictionary<string, int>();

            return itemDef.Blueprint.ingredients
                .GroupBy(ingredient => ingredient.itemDef.displayName.english.ToLower())
                .ToDictionary(group => group.Key, group => group.Sum(ingredient => (int)ingredient.amount));
        }

        private void AddDeployableCostsToBuildingCosts(Dictionary<string, int> buildingBlockCosts, Dictionary<string, int> deployableCosts, Dictionary<string, int> deployableCostsvar)
        {
            foreach (var cost in deployableCostsvar)
            {
                if (IsBuildingResource(cost.Key))
                {
                    buildingBlockCosts[cost.Key] = buildingBlockCosts.GetValueOrDefault(cost.Key) + cost.Value;
                }
                else
                {
                    deployableCosts[cost.Key] = deployableCosts.GetValueOrDefault(cost.Key) + cost.Value;
                }
            }
        }

        private bool IsBuildingResource(string resourceName)
        {
            return resourceName.Contains("wood") ||
                resourceName.Contains("stone") ||
                resourceName.Contains("metal fragments") ||
                resourceName.Contains("high quality metal");
        }

        private void AddCosts(Dictionary<string, int> totalCosts, Dictionary<string, int> costsToAdd)
        {
            foreach (var cost in costsToAdd)
            {
                if (totalCosts.ContainsKey(cost.Key))
                {
                    totalCosts[cost.Key] += cost.Value;
                }
                else
                {
                    totalCosts[cost.Key] = cost.Value;
                }
            }
        }

        private void UpdatePlayerSettingsClass(BasePlayer player)
        {
            try
            {
                string DoorPrefab = "assets/prefabs/building/door.hinged/door.hinged.toptier.prefab";
                string DoubleDoorPrefab = "assets/prefabs/building/door.double.hinged/door.double.hinged.metal.prefab";
                string WindowPrefab = "assets/prefabs/building/wall.window.bars/wall.window.bars.metal.prefab";
                string EmbrasurePrefab = "assets/prefabs/building/wall.window.shutter/shutter.wood.a.prefab";

                List<string> ElectricalList = new List<string>
                {
                    "ceilinglight.deployed",
                    "autoturret_deployed",
                    "sam_site_turret_deployed",
                    "electrical.heater",
                    "electricfurnace.deployed",
                    "industrial.wall.lamp.red.deployed",
                    "industrial.wall.lamp.green.deployed",
                    "industrial.wall.lamp.deployed",
                    "sign.neon.125x125",
                    "sign.neon.125x215",
                    "sign.neon.xl",
                    "sign.neon.125x215.animated",
                    "sign.neon.xl.animated",
                    "searchlight.deployed"
                };

                if (!playerSettings.ContainsKey(player.userID))
                {
                    playerSettings[player.userID] = new PlayerSettings(player.userID)
                    {
                        IsMenuOpened = false,
                        AutoEntity = true,
                        FillBatteries = false,
                        EntityStability = true,
                        Noclip = false,
                        GodMode = true,
                        ChangeBGrade = true,
                        InfiniteAmmo = true,
                        BGradeHud = true,
                        GTFO = true,
                        PlayerOwnZone = false,
                        AutoDoors = _config.menu_autodoors,
                        DoorOpenClose = true,
                        CodeLockDoor = false,
                        DoorPrefab = DoorPrefab,
                        DoubleDoorPrefab = DoubleDoorPrefab,
                        AutoWindows = _config.menu_autowindows,
                        WindowPrefab = WindowPrefab,
                        EmbrasurePrefab = EmbrasurePrefab,
                        AutoElectricity = _config.menu_autoelectricity,
                        ElectricityList = ElectricalList,
                        BuildingGrade = _config.menu_buildingrade,
                        CurrentGrade = 0,
                        CurrentMenu = "build",
                        PlayerInArea = false,
                        BuildUpgradeInProgress = false,
                        CurrentSkin = 0,
                        CurrentContainerSkin = 0,
                        BGradeBackground = false,
                        CurrentBaseName = null,
                        NetworkingFoundations = true,
                        NetworkingFloor = true,
                        NetworkingWalls = true,
                        NetworkingOthers = true,
                        NetworkingDeployables = true,
                        LoadingBase = false,
                        CurrentPage = 0,
                        CommunityCurrentMenu = "saved",
                        StoredClaimedPlotLocation = Vector3.zero,
                        Raid = false
                    };
                }

                playerEntityCount[player.userID] = new List<SpawnedEntityInfo>();

                if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "creative.admin"))
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, true);

                ServerMgr.Instance.StartCoroutine(UnlockBlueprints(player));
                player.ClientRPCPlayer(null, player, "craftMode", 1);
                player.PauseFlyHackDetection(86400f);
                player.PauseSpeedHackDetection(86400f);
                player.SendNetworkUpdateImmediate();

                foreach (ItemBlueprint bp in ItemManager.GetBlueprints())            
                    bp.workbenchLevelRequired = 0;

                playerWeather[player.userID] = new PlayerWeather(0f, 0f, 0f, 0f, 2f, 4f, 0.9f, 1.25f, 0.75f, 0.3f, 2f, 0.25f, 0f, 0f, 1f, 0.25f, 1f, 1f, 12f, 0f);
                
            }
            catch (Exception e) { LogErrors(e.Message, "UpdatePlayerSettingsClass"); }
        }

        private List<int> BPs = Facepunch.Pool.GetList<int>();

        private IEnumerator UnlockBlueprints(BasePlayer player)
        {
            var currentPlayerBps = player.PersistantPlayerInfo.unlockedItems;

            foreach (var bp in BPs)
            {
                if (!currentPlayerBps.Contains(bp))
                    currentPlayerBps.Add(bp);
            }

            var persistantPlayerInfo = player.PersistantPlayerInfo;
            persistantPlayerInfo.unlockedItems = currentPlayerBps;
            player.PersistantPlayerInfo = persistantPlayerInfo;
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);

            yield return new WaitForSeconds(0.05f);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            UpdatePlayerSettingsClass(player);
            ApplyKit(player);
            uiBgradeHud(player);
            _ = UpdatePlayerBaseImagesAsync(player);

            if (_config.buildcost_hud)
                _ = PeriodicUpdateCost(player);
        }

        private async Task UpdatePlayerBaseImagesAsync(BasePlayer player)
        {
            if (player == null) return;

            string playerId = player.UserIDString;
            string targetImageUrl = "https://i.imgur.com/0QtCHOh.png";

            List<string> baseNames = new List<string>();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT BaseName
                    FROM SavedBases
                    WHERE PlayerId = @PlayerId AND baseImageUrl = @TargetImageUrl";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerId", playerId);
                    command.Parameters.AddWithValue("@TargetImageUrl", targetImageUrl);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string baseName = reader["BaseName"].ToString();
                            if (!string.IsNullOrEmpty(baseName))
                            {
                                baseNames.Add(baseName);
                            }
                        }
                    }
                }
            }

            foreach (string baseName in baseNames)
            {
                string baseImagePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerId}/{baseName}.png");

                if (File.Exists(baseImagePath))
                {
                    LogErrors($"Uploading '{baseName}' from playerid '{playerId}' to Imgur/Imgbb", "UpdatePlayerBaseImagesAsync");
                    await UpdateBaseUrlForImageAsync(player, baseImagePath, baseName);
                    await Task.Delay(50);
                }
                else
                {
                    LogErrors($"Image file not found for base '{baseName}' of playerid '{playerId}'", "UpdatePlayerBaseImagesAsync");
                }
            }
        }

        private async Task UpdateBaseUrlForImageAsync(BasePlayer player, string baseImagePath, string baseName)
        {
            string imageUrl = await Task.Run(() => UploadImageToHostingService(baseImagePath, player));

            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "https://i.imgur.com/0QtCHOh.png") return;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string updateQuery = @"
                    UPDATE SavedBases
                    SET baseImageUrl = @BaseImageUrl
                    WHERE PlayerId = @PlayerId AND BaseName = @BaseName";

                using (var command = new SqliteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@BaseImageUrl", imageUrl);
                    command.Parameters.AddWithValue("@PlayerId", player.UserIDString);
                    command.Parameters.AddWithValue("@BaseName", baseName);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        LogErrors($"Successfully updated image URL for base '{baseName}' of playerid '{player.UserIDString}'", "UpdateBaseUrlForImageAsync");
                    }
                    else
                    {
                        LogErrors($"Failed to update image URL for base '{baseName}' of playerid '{player.UserIDString}'", "UpdateBaseUrlForImageAsync");
                    }
                }
            }
        }


        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;
                
            if (_config.player_clearinventory)
                player.inventory.Strip();
            
            if (_config.plot_despawn && activeZones != null)
                RemoveZones(player);

            KillPlayerSpawnedEntities(player.userID);

            if (playerSettings.ContainsKey(player.userID))
            {
                playerSettings.Remove(player.userID);
            }
        }

        private async void RemoveZones(BasePlayer player)
        {
            try
            {
                List<ulong> zonesToRemove = Facepunch.Pool.GetList<ulong>();

                zonesToRemove.AddRange(activeZones
                    .Where(pair => pair.Value.GetOwner() == player.userID)
                    .Select(pair => pair.Key));

                foreach (ulong zoneID in zonesToRemove)
                {
                    if (activeZones.TryGetValue(zoneID, out var zoneTrigger))
                    {
                        if (zoneTrigger != null)
                        {
                            _ = ClearEntitiesForPlayer(player, playerSettings[player.userID].StoredClaimedPlotLocation);
                            RemoveMarker(zoneID.ToString());
                            zoneTrigger.DeleteCircle();

                            if (activeZones.ContainsKey(zoneID))
                            {
                                activeZones.Remove(zoneID);
                            }

                            if (zoneTrigger.gameObject != null)
                            {
                                UnityEngine.Object.DestroyImmediate(zoneTrigger.gameObject);
                            }
                        }
                    }
                }
                Facepunch.Pool.FreeList(ref zonesToRemove);

                if (playerSettings.ContainsKey(player.userID))
                {
                    playerSettings.Remove(player.userID);
                }
            }
            catch (Exception e) { LogErrors(e.Message, "RemoveZones"); }
        }

        private object OnItemCraft(ItemCraftTask task, BasePlayer owner)
        {
            if (!permission.UserHasPermission(owner.UserIDString, "creative.use"))
                return null;

            ulong skin = ItemDefinition.FindSkin(task.blueprint.targetItem.itemid, task.skinID);
            Item item = null;
            try
            {
                item = ItemManager.CreateByItemID(task.blueprint.targetItem.itemid, task.amount * task.blueprint.amountToCreate, skin);
            }
            catch (Exception e) { LogErrors(e.Message, "OnItemCraft"); }

            if (item == null)
                return false;

            ItemContainer itemContainer = owner.inventory.crafting.containers.First<ItemContainer>();
            owner.inventory.GiveItem(item);
            owner.Command("note.inv", new object[]{item.info.itemid, task.amount * task.blueprint.amountToCreate});

            return true;
        }

        private void GrantResources(BasePlayer player)
        {
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            ulong playerId = player.userID;
            player.inventory.containerMain.Clear();
            player.inventory.containerMain.capacity = 25 + (_config.resourceItems.Count + 1);

            foreach (string resourceName in _config.resourceItems)
            {
                int itemId = ItemManager.itemDictionary
                .Where(kvp => kvp.Value.shortname.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

                if (itemId != 0)
                {
                    if (!PlayerHasItem(player, itemId))
                    {
                        Item resourceItem = ItemManager.CreateByItemID(itemId, 99999999);
                        if (resourceItem != null)
                        {
                            player.GiveItem(resourceItem);
                            for (var i = 0; i < _config.resourceItems.Count; i++)
                            {
                                resourceItem.MoveToContainer(player.inventory.containerMain, 25 + i, false);
                            }
                        }
                    }
                }
            }
        }

        private bool PlayerHasItem(BasePlayer player, int itemId)
        {
            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info.itemid == itemId)
                {
                    return true;
                }
            }
            return false;
        }

        private void RemoveEffectsFromPlayer(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (player == null || !player.IsConnected)
                return;

            if (player.metabolism.calories.value < 500)
                player.metabolism.calories.value = 500;

            if (player.metabolism.hydration.value < 250)
                player.metabolism.hydration.value = 250;

            player.health = 100;
            player.metabolism.temperature.value = 30;
            player.metabolism.wetness.max = 0;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;

            GrantResources(player);
            RemoveEffectsFromPlayer(player);
            uiBgradeHud(player);
            ApplyKit(player);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null) return;

            GrantResources(player);
            RemoveEffectsFromPlayer(player);
            uiBgradeHud(player);
            ApplyKit(player);

            var markersToRecreate = markerDataList.Where(markerData => markerData.OwnerID == player.userID).ToList();

            foreach (var markerData in markersToRecreate)
            {
                CreateMapMarker(markerData.MarkerID, markerData.Text, markerData.Position, markerData.Radius, player, true);
            }
        }

        private bool IsBlocked(BasePlayer player)
        {
            if (NoEscape == null)
                return false;

            if (NoEscape.Call<bool>("HasPerm", player.UserIDString, "raid.buildblock") && NoEscape.Call<bool>("IsRaidBlocked", player))
                SendReply(player, "You can't use this command due to raid block!");

            if (NoEscape.Call<bool>("HasPerm", player.UserIDString, "combat.buildblock") && NoEscape.Call<bool>("IsCombatBlocked", player))
                SendReply(player, "You can't use this command due to combat block!");


            return (NoEscape.Call<bool>("HasPerm", player.UserIDString, "raid.buildblock") && NoEscape.Call<bool>("IsRaidBlocked", player)) ||
                NoEscape.Call<bool>("HasPerm", player.UserIDString, "combat.buildblock") && NoEscape.Call<bool>("IsCombatBlocked", player);

            return false;
        }

        [ConsoleCommand("inventory.giveid")]
        void GiveIdCommand(BasePlayer player, string command, string[] args)
        {
        }

        object OnClientCommand(Network.Connection connection, string arg)
        {
            BasePlayer player = BasePlayer.Find(connection.userid.ToString());

            if (player == null) return null;
            try
            {
                if (arg.Contains("giveid") || arg.Contains("givearm"))
                {
                    if (permission.UserHasPermission(player.UserIDString, "creative.use") && permission.UserHasPermission(player.UserIDString, "creative.f1_give"))
                    {
                        string[] args = arg.Split(' ');

                        if (args.Length < 3)
                        {
                           // SendMessage(player, "Invalid command syntax. Usage: giveid <itemid> <amount>");
                            return null;
                        }

                        string NoQuoteFirstArg = args[1].Trim('"');

                        int itemid;
                        int.TryParse(NoQuoteFirstArg, out itemid);

                        int valueamm;
                        int.TryParse(args[2].Trim('"'), out valueamm);

                        if (valueamm <= 0)
                        {
                            SendMessage(player, "Invalid item ID or amount.");
                            return null;
                        }

                        Item item = ItemManager.CreateByItemID(itemid, valueamm, 0);
                        if (item == null)
                            return false;

                        item.amount = valueamm;
                        if (!player.inventory.GiveItem(item, null))
                        {
                            item.Remove(0f);
                            return false;
                        }

                        player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                        return false;
                    }
                    else
                    {
                        SendMessage(player, "NoPerms");
                    }
                }else{
                    
                }
            }
            catch (Exception e) { LogErrors(e.Message, "OnClientCommand"); }

            return null;
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            string command = arg.cmd.Name;

            BasePlayer player = arg.Player();
            if (!player || !playerSettings.ContainsKey(player.userID)) return null;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return null;

            if (IsBlocked(player))
                return null;

            if (arg.cmd.Name.ToLower() == _config.keybind_noclip && permission.UserHasPermission(player.UserIDString, "creative.fly"))
            {
                player.SendNetworkUpdateImmediate();
                player.SendConsoleCommand("noclip", new object[] { });
                playerSettings[player.userID].Noclip = !playerSettings[player.userID].Noclip;
            }

            return null;
        }

        private bool fast_return_check(BasePlayer player)
        {
            if (FindClosestZone(player) != player.userID && player.Team == null)
                return true;

            if (player.Team != null && !player.Team.members.Contains(FindClosestZone(player)))
                return true;

            return false;
        }

        object CanBuild(Planner plan, Construction prefab)
        {
            try
            {
                BasePlayer player = plan.GetOwnerPlayer();

                if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use") || !playerSettings.ContainsKey(player.userID))
                    return false;

                if (fast_return_check(player))
                {
                    SendMessage(player, "NotClaimed");
                    return false;
                }

                if (activeZones != null)
                {
                    foreach (var kvp in activeZones)
                    {
                        var existingZone = kvp.Value;
                        Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

                        if (player.Team != null && player.Team.members.Contains(existingZone.GetOwner()) && player.userID != existingZone.GetOwner())
                            Zone_Center_Pos = playerSettings[existingZone.GetOwner()].StoredClaimedPlotLocation;

                        if (Zone_Center_Pos == Vector3.zero)
                            return false;

                        float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;

                        if (Vector3.Distance(Zone_Center_Pos, plan.transform.position) > plot_radius - 3f)
                        {
                            SendMessage(player, "BuildOutsideArea");
                            LogErrors($"Allowed: {existingZone.IsPlayerAllowed(player)} | Center: {Zone_Center_Pos} \nBuild Pos: {plan.transform.position} | Distance: {Vector3.Distance(Zone_Center_Pos, plan.transform.position)} | Plot Radius: {plot_radius - 3f} | Cant build?: {Vector3.Distance(Zone_Center_Pos, plan.transform.position) > plot_radius}", "CanBuild");
                            return false;
                        }
                        else
                            return null;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "CanBuild"); 
                return false;
            }

            return false;
        }

        private BuildingBlock GetBuildingBlockInView(BasePlayer player)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(player.eyes.HeadRay(), 5f))
            {
                var entity = hit.collider.GetComponentInParent<BuildingBlock>();
                if (entity != null && entity.GetType() == typeof(BuildingBlock))
                {
                    return entity;
                }
            }
            return null;
        }

        void DoBuilding(BasePlayer player, string curr)
        {
            var layers =  LayerMask.GetMask("Construction", "Default", "Deployed");

            RaycastHit hit = new RaycastHit();
                
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue, layers))
            {
                var entity = hit.GetEntity();
                if (entity != null)
                {
                    var buildingBlock = GetBuildingBlockInView(player);

                    if (buildingBlock == null)
                        return;

                    if (buildingBlock.OwnerID != player.userID)
                    {
                        if (player.Team == null)
                            return;

                        if (!player.Team.members.Contains(buildingBlock.OwnerID))
                            return;
                    }

                    var currentGrade = buildingBlock.grade;
                    var nextGrade = currentGrade + 1;
                    var previousGrade = currentGrade - 1;

                    if (!Enum.IsDefined(typeof(BuildingGrade.Enum), nextGrade))
                        return;

                    if (curr == "upgrade" && currentGrade.ToString() != "TopTier")
                    {
                        buildingBlock.ChangeGradeAndSkin(nextGrade, 0, true, true);
                        buildingBlock.SetHealthToMax();
                        buildingBlock.SendNetworkUpdateImmediate();
                    }else if (curr == "downgrade" && currentGrade.ToString() != "Twigs"){
                        buildingBlock.ChangeGradeAndSkin(previousGrade, 0, true, true);
                        buildingBlock.SetHealthToMax();
                        buildingBlock.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

        private bool IsEntityInClaimedArea(BasePlayer player, Vector3 entityPosition)
        {
            Vector3 zoneCenter = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (zoneCenter == Vector3.zero)
                return false;

            float radius = permission.UserHasPermission(player.UserIDString, "creative.vip") 
                ? _config.plot_radius_vip 
                : _config.plot_radius_default;

            return Vector3.Distance(zoneCenter, entityPosition) <= radius;
        }

        private async Task HandleRaycastAsync(BasePlayer player)
        {
            await Task.Yield();

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 20f))
            {
                var entity = hit.GetEntity();
                if (entity != null)
                {
                    if (!IsEntityInClaimedArea(player, entity.transform.position))
                        return;

                    if (entity.OwnerID == player.userID || (player.Team?.members.Contains(entity.OwnerID) ?? false))
                    {
                        entity.Kill();
                    }
                }
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
                return;

            if (!playerSettings.TryGetValue(player.userID, out var settings) || 
                !permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (input.WasJustPressed(BUTTON.FIRE_THIRD) && !IsOnCooldown(player.userID))
            {
                settings.IsMenuOpened = !settings.IsMenuOpened;
                if (settings.IsMenuOpened)
                    uiMenuMain(player, settings.CurrentMenu);
                else
                    uiCloseAll(player);
            }

            if (input.WasJustPressed(_config.keybind_bgrade) && input.IsDown(BUTTON.SPRINT) && settings.ChangeBGrade)
            {
                settings.CurrentGrade = (settings.CurrentGrade < 4) ? settings.CurrentGrade + 1 : 0;
                settings.CurrentSkin = 0;
                uiBgradeHud(player, true);
            }
            
            if (input.WasJustPressed(_config.keybind_removaltool) && player.GetActiveItem()?.info.shortname == "hammer")
            {
                bool sym_status = Symmetry != null && Symmetry.Call<bool>("GetSymmetryStatus", player);
                if (!sym_status)
                    _ = HandleRaycastAsync(player);
            }

            if ((input.WasJustPressed(_config.keybind_upgrade) || input.WasJustPressed(_config.keybind_downgrade)) && input.IsDown(BUTTON.SPRINT))
            {
                if (player.GetActiveItem()?.info.shortname == "hammer")
                {
                    var action = input.WasJustPressed(_config.keybind_upgrade) ? "upgrade" : "downgrade";
                    DoBuilding(player, action);
                }
            }
        }

        private bool IsOnCooldown(ulong userId)
        {
            if (menuCooldown.TryGetValue(userId, out float lastCommandTime) && UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 0.5f)
                return true;

            menuCooldown[userId] = UnityEngine.Time.realtimeSinceStartup;
            return false;
        }

        object OnPayForPlacement(BasePlayer player, Planner planner, Construction construction)
        {            
            return false;
        }

        Vector3 GetZoneCenter(BasePlayer player)
        {
            Vector3 return_data = Vector3.zero;

            var GetZones = Facepunch.Pool.GetList<ulong>();

            GetZones.AddRange(activeZones
                .Where(pair => pair.Value.GetOwner() == player.userID)
                .Select(pair => pair.Key));

            foreach (ulong zoneID in GetZones)
            {
                if (activeZones.TryGetValue(zoneID, out var zoneTrigger))
                {
                    return_data = zoneTrigger.GetZoneCenter();
                }
            }

            Facepunch.Pool.FreeList(ref GetZones);

            return return_data;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player == null || item == null || action == null) return null;
            if (action.Contains("unwrap") || action.Contains("upgrade_item")) return true;

            if (action.Contains("drop"))
            {
                item.Remove();
                return true;
            }
            return null;
        }

        private void ToggleFurnaces(BasePlayer player, bool turnOn)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (Zone_Center_Pos == Vector3.zero)
                return;

            NextTick(() =>
            {
                float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                var furnaces = Physics.OverlapSphere(Zone_Center_Pos, plot_radius)
                    .Select(collider => collider.GetComponentInParent<BaseEntity>())
                    .Where(entity => entity != null && entity.ShortPrefabName.Contains("furnace"))
                    .ToList();

                foreach (var furnace in furnaces)
                {
                    var oven = furnace as BaseOven;
                    if (oven != null)
                    {
                        if (turnOn)
                        {
                            oven.SetFlag(BaseEntity.Flags.On, true);
                        }
                        else
                        {
                            oven.StopCooking();
                        }
                        oven.SendNetworkUpdate();
                    }
                }
            });
        }

        private void FillBatteries(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (Zone_Center_Pos == Vector3.zero)
                return;

            NextTick(() =>
            {
                float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                var batteries = Physics.OverlapSphere(Zone_Center_Pos, plot_radius)
                    .Select(collider => collider.GetComponentInParent<ElectricBattery>())
                    .Where(battery => battery != null)
                    .ToList();

                foreach (var battery in batteries)
                {
                    battery.rustWattSeconds = battery.maxCapactiySeconds;
                    battery.SendNetworkUpdate();
                }
            });
        }


        private void SetBuildingStability(BasePlayer player, bool stable)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (Zone_Center_Pos == Vector3.zero)
                return;

            float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
            var entities = Physics.OverlapSphere(Zone_Center_Pos, plot_radius)
                .Select(collider => collider.GetComponentInParent<StabilityEntity>())
                .Where(entity => entity != null)
                .ToList();

            foreach (var entity in entities)
            {
                if (entity.ShortPrefabName.Contains("foundation"))
                    continue;

                entity.grounded = !stable;
                entity.InitializeSupports();
                entity.UpdateStability();
                entity.SendNetworkUpdate();
            }
        }

        private bool isPerformanceCheckInProgress = false;

        private const float MIN_FRAME_RATE = 15f;
        private const float MAX_FRAME_TIME = 50f;
        private const float MAX_MEMORY_USAGE = 80f;
        private const int MAX_PENDING_TASKS = 100;

        private bool PerformanceCheck()
        {
            if (isPerformanceCheckInProgress)
                return false;

            isPerformanceCheckInProgress = true;

            if (Performance.report.frameRate < MIN_FRAME_RATE)
            {
                isPerformanceCheckInProgress = false;
                return false;
            }

            if (Performance.report.frameTime > MAX_FRAME_TIME)
            {
                isPerformanceCheckInProgress = false;
                return false;
            }

            float memoryUsagePercentage = (Performance.report.memoryUsageSystem / SystemInfo.systemMemorySize) * 100;
            if (memoryUsagePercentage > MAX_MEMORY_USAGE)
            {
                isPerformanceCheckInProgress = false;
                return false;
            }

            if (Performance.report.loadBalancerTasks > MAX_PENDING_TASKS)
            {
                isPerformanceCheckInProgress = false;
                return false;
            }

            if (Performance.report.gcTriggered)
            {
                isPerformanceCheckInProgress = false;
                return false;
            }

            isPerformanceCheckInProgress = false;
            return true;
        }

        private async Task ClearEntitiesForPlayer(BasePlayer player, Vector3 storedZone = default, string type = null)
        {
            try
            {
                if (!playerSettings.ContainsKey(player.userID))
                {
                    Puts($"Player settings not found for user: {player.userID}");
                    return;
                }

                if (storedZone == Vector3.zero)
                    storedZone = playerSettings[player.userID].StoredClaimedPlotLocation;

                Vector3 zoneCenterPos = storedZone;

                if (zoneCenterPos == Vector3.zero)
                {
                    NextTick(() => SendMessage(player, "NoClaimed"));
                    return;
                }

                const int batchSize = 15;
                const int delayBetweenBatches = 100;
                var playerRadius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;

                var entitiesToRemove = new HashSet<BaseEntity>();
                var foundEntities = new List<BaseEntity>();

                Vis.Entities(zoneCenterPos, playerRadius, foundEntities);

                foreach (var entity in foundEntities)
                {
                    try
                    {
                        if (type == null)
                        {
                            if (entity.ShortPrefabName.Contains("ballista") || 
                                entity.ShortPrefabName.Contains("battering") || 
                                entity.ShortPrefabName.Contains("catapul") || 
                                entity.ShortPrefabName.Contains("siege"))
                                entitiesToRemove.Add(entity);
                            
                            if (entity is BasePlayer || entity.OwnerID == 0 || entity is MapMarkerGenericRadius)
                                continue;

                            if (entity is BuildingBlock buildingBlock)
                            {
                                if (buildingBlock.PrefabName.Contains("foundation"))
                                {
                                    entitiesToRemove.Add(entity);
                                }else{
                                    entitiesToRemove.Add(entity);
                                }
                            }
                            else
                            {
                                entitiesToRemove.Add(entity);
                            }
                        }
                        else if (type == "deployable")
                        {
                            if (entity is BasePlayer || entity.OwnerID == 0 || entity is MapMarkerGenericRadius || entity is BuildingBlock)
                                continue;

                            entitiesToRemove.Add(entity);
                        }
                        else if (type == "foundation")
                        {
                            if (entity is BasePlayer || entity.OwnerID == 0 && entity is not BuildingBlock  || entity is MapMarkerGenericRadius)
                                continue;
                                
                            if (entity is BuildingBlock buildingBlock && buildingBlock.PrefabName.Contains("foundation"))
                                continue;

                            entitiesToRemove.Add(entity);
                        }


                        if (entitiesToRemove.Count >= batchSize)
                        {
                            await ProcessEntityBatchAsync(entitiesToRemove);
                            await Task.Delay(delayBetweenBatches);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        LogErrors(innerEx.Message, "ClearEntitiesForPlayer - Entity Loop");
                    }
                }

                if (entitiesToRemove.Count > 0)
                {
                    await ProcessEntityBatchAsync(entitiesToRemove);
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "ClearEntitiesForPlayer - Main");
            }
        }


        private async Task ProcessEntityBatchAsync(HashSet<BaseEntity> entitiesToRemove)
        {
            var batch = new List<BaseEntity>(entitiesToRemove);
            entitiesToRemove.Clear();

            try
            {
                foreach (var entityToRemove in batch)
                {
                    if (entityToRemove != null && !entityToRemove.IsDestroyed)
                    {
                        if (entityToRemove is MapMarkerGenericRadius || entityToRemove == null)
                            continue;

                        var stabilityEntity = entityToRemove.GetComponent<StabilityEntity>();
                        if (stabilityEntity != null && stabilityEntity.grounded)
                        {
                            stabilityEntity.grounded = false;
                            stabilityEntity.SendNetworkUpdateImmediate();
                        }

                        entityToRemove.SendNetworkUpdateImmediate();
                        entityToRemove.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "ProcessEntityBatchAsync");
            }

            await Task.CompletedTask;
        }

        private async void ClearAll(BasePlayer player)
        {
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
            {
                playerSettings[player.userID].LoadingBase = false;
                return;
            }

            if (!playerSettings.ContainsKey(player.userID))
            {
                playerSettings[player.userID].LoadingBase = false;
                return;
            }
            
            playerSettings[player.userID].ClearingPlot = true;
            _ = ClearEntitiesForPlayer(player, playerSettings[player.userID].StoredClaimedPlotLocation);
            await Task.Delay(100);
            playerSettings[player.userID].LoadingBase = false;
            playerSettings[player.userID].ClearingPlot = false;
        }

        [HookMethod("CheckZone")]
        private object CanEntitySpawn(BasePlayer player, Vector3 position)
        {
            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (Zone_Center_Pos == Vector3.zero)
                return false;

            var playerRadius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
            if (Vector3.Distance(Zone_Center_Pos, position) > playerRadius - 3f)
            {
                return false;
            }
            
            return true;
        }

        private void OpenClosePlayerDoors(BasePlayer player, bool close_open)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;
            if (Zone_Center_Pos == Vector3.zero)
                return;

            var entList = BaseEntity.saveList.Where(x => (x as Door) != null && x.OwnerID == player.userID).ToList();
            if (entList.Count == 0) return;

            foreach (var item in entList)
            {
                if (item == null) continue;

                var door = item as Door;
                if (door == null) continue;

                if (!playerSettings.ContainsKey(player.userID))
                    continue;

                if (playerSettings[player.userID].DoorOpenClose)
                    door.SetOpen(close_open);
                else
                    door.SetOpen(close_open);
            }
        }

        void ElectricityManager(BaseNetworkable entity)
        {
            if (entity == null)
                return;

            var autoturret = entity as AutoTurret;
            var samsite = entity as SamSite;
            var heater = entity as ElectricalHeater;
            var flashlight = entity as FlasherLight;
            var ceilinglight = entity as CeilingLight;
            var electricfurnace = entity as ElectricOven;
            var industriallight = entity as SimpleLight;
            var neonsign = entity as NeonSign;
            var searchlight = entity as SearchLight;

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!playerSettings.ContainsKey(player.userID))
                    continue;

                if (!playerSettings.TryGetValue(player.userID, out var settings))
                    continue;

                if (!settings.ElectricityList.Contains(entity.ShortPrefabName))
                    continue;

                bool isOn = settings.AutoElectricity;

                if (autoturret != null)
                {
                    autoturret.SetIsOnline(isOn);
                }

                if (samsite != null)
                {
                    samsite.UpdateHasPower(isOn ? 25 : 0, 0);
                    samsite.SetFlag(BaseEntity.Flags.Reserved8, isOn);
                    samsite.SendNetworkUpdate();
                }

                if (heater != null)
                {
                    heater.UpdateHasPower(isOn ? 3 : 0, 0);
                    heater.SetFlag(BaseEntity.Flags.Reserved8, isOn);
                    heater.SendNetworkUpdate();
                }

                if (flashlight != null)
                {
                    flashlight.UpdateHasPower(isOn ? 1 : 0, 0);
                    flashlight.SetFlag(BaseEntity.Flags.Reserved8, isOn);
                    flashlight.SendNetworkUpdate();
                }

                if (ceilinglight != null)
                {
                    ceilinglight.UpdateHasPower(isOn ? 3 : 0, 0);
                    ceilinglight.SetFlag(BaseEntity.Flags.On, isOn);
                    ceilinglight.SendNetworkUpdate();
                }

                if (electricfurnace != null)
                {
                    if (isOn)
                        electricfurnace.StartCooking();
                    else
                        electricfurnace.StopCooking();

                    electricfurnace.SetFlag(BaseEntity.Flags.On, isOn);
                    electricfurnace.SendNetworkUpdate();
                }

                if (industriallight != null)
                {
                    industriallight.UpdateHasPower(isOn ? 1 : 0, 0);
                    industriallight.SetFlag(BaseEntity.Flags.On, isOn);
                    industriallight.SendNetworkUpdate();
                }

                if (neonsign != null)
                {
                    neonsign.UpdateHasPower(isOn ? 10 : 0, 0);
                    neonsign.SetFlag(BaseEntity.Flags.On, isOn);
                    neonsign.SendNetworkUpdate();
                }

                if (searchlight != null)
                {
                    searchlight.UpdateHasPower(isOn ? 15 : 0, 0);
                    searchlight.SetFlag(BaseEntity.Flags.On, isOn);
                    searchlight.SendNetworkUpdate();
                }
            }
        }

        object OnStructureRepair(BaseCombatEntity self, BasePlayer player)
        {
            if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use"))
                return null;

            self.lastAttackedTime = float.MinValue;
            return null;
        }

        object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (player == null)
            {
                return null;
            }

            BasePlayer teamLeaderPlayer = BasePlayer.FindByID(team.teamLeader);

            if (teamLeaderPlayer == null)
            {
                return null;
            }

            if (!playerSettings.ContainsKey(player.userID) || !playerSettings.ContainsKey(teamLeaderPlayer.userID))
                return null;

            bool playerOwnsZone = playerSettings[player.userID].PlayerOwnZone;
            bool teamLeaderOwnsZone = playerSettings[teamLeaderPlayer.userID].PlayerOwnZone;

            if (playerOwnsZone && teamLeaderOwnsZone)
            {
                SendReply(player, "You both own zones, you cannot join the team. \nOne must un-claim their plot.");
                SendReply(teamLeaderPlayer, "You both own zones, you cannot join the team. \nOne must un-claim their plot.");

                return false;
            }

            if (team.teamLeader != player.userID)
            {
                Vector3 teleportPosition = teamLeaderPlayer.transform.position;
                player.Teleport(teleportPosition);
            }

            return null;
        }

        private async Task SyncBases(BasePlayer player)
        {
            if (_config.pastee_auth != "PASTE.EE TOKEN HERE")
                await Task.Run(() => CheckAndAddNewBasesFromPasteAPI(player.userID.ToString()));
        }

        private void LogErrors(string err, string function)
        {
            if (!_config.log_errors) return;
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string reviewLog = $"[{timestamp}] | [{function}] Error: {err}";
            string folderPath = Path.Combine(Interface.GetMod().DataDirectory, "Creative/Logs");
            string filePath = Path.Combine(folderPath, "logs.json");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var errlogs = File.Exists(filePath) ? JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath)) : new List<string>();
            errlogs.Add(reviewLog);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(errlogs, Formatting.Indented));
        }

        async void OnPhotoCapture(PhotoEntity photoEntity, Item item, BasePlayer player, byte[] imageData)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            ulong closestZoneID = FindClosestZone(player);
            if (closestZoneID != player.userID) return;

            Vector3 zoneCenterPos = playerSettings[player.userID].StoredClaimedPlotLocation;
            Vector3 playerPos = player.transform.position;
            var playerRadius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
            float distanceToZone = Vector3.Distance(playerPos, zoneCenterPos);

            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (string.IsNullOrEmpty(playerSettings[player.userID].CurrentBaseName))
                return;

            if (distanceToZone > playerRadius)
            {
                SendMessage(player, "Outside");
                return;
            }

            ulong playerID = player.userID;
            string baseImagePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerID}/{playerSettings[player.userID].CurrentBaseName}.png");

            File.WriteAllBytes(baseImagePath, imageData);

            ProccessImageZ(baseImagePath, player);

            SendMessage(player, "PhotoCaptured", playerSettings[player.userID].CurrentBaseName);
        }

        private async void ProccessImageZ(string baseImagePath, BasePlayer player)
        {
            string imageUrl;

            if (File.Exists(baseImagePath))
            {
                imageUrl = await Task.Run(() => UploadImageToHostingService(baseImagePath, player));
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    imageUrl = "https://i.imgur.com/0QtCHOh.png";
                }

                UpdateBaseUrl(player, imageUrl);
            }
        }

        private void UpdateBaseUrl(BasePlayer player, string imageUrl)
        {
            if (playerSettings.ContainsKey(player.userID) && !string.IsNullOrEmpty(playerSettings[player.userID].CurrentBaseName))
            {
                string baseName = playerSettings[player.userID].CurrentBaseName;

                string updateBaseUrlQuery = @"
                    UPDATE SavedBases
                    SET baseImageUrl = @baseImageUrl
                    WHERE BaseName = @BaseName AND PlayerId = @PlayerId
                ";

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqliteCommand(updateBaseUrlQuery, connection))
                    {
                        command.Parameters.AddWithValue("@baseImageUrl", imageUrl);
                        command.Parameters.AddWithValue("@BaseName", baseName);
                        command.Parameters.AddWithValue("@PlayerId", player.userID.ToString());

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            SendReply(player, $"Updated Image for the base '{baseName}'");
                        }
                        else
                        {
                            SendReply(player, $"Base '{baseName}' not found.");
                        }
                    }
                }
            }
        }

        #endregion

        #region DB

        private string connectionString;
        
        private bool InitializeBaseInfo()
        {
           string dbPath = Path.Combine(Interface.GetMod().DataDirectory, "CreativeDB.db");
            connectionString = $"Data Source={dbPath};Version=3;";
            return CreateDatabaseTables();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                if (!await CanConnectToDatabaseAsync())
                {
                    PrintError("Database connection failed. Skipping database initialization.");
                    return;
                }

                string checkDbQuery = $"SHOW DATABASES LIKE '{_config.sqldata[0].database}'";
                string createDbQuery = $"CREATE DATABASE `{_config.sqldata[0].database}`";

                string checkTableQuery = $@"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = '{_config.sqldata[0].database}' 
                    AND table_name = 'bases'";

                string createTableQuery = $@"
                    CREATE TABLE `{_config.sqldata[0].database}`.`bases` (
                        `playerid` VARCHAR(255) NOT NULL,
                        `basename` VARCHAR(255) NOT NULL,
                        `basedata` JSON NOT NULL,
                        `creation_date` DATETIME NOT NULL,
                        PRIMARY KEY (`playerid`, `basename`)
                    )";

                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    bool dbExists = false;
                    using (MySqlCommand cmd = new MySqlCommand(checkDbQuery, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        dbExists = reader.HasRows;
                    }

                    if (!dbExists)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(createDbQuery, conn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    bool tableExists = false;
                    using (MySqlCommand cmd = new MySqlCommand(checkTableQuery, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            tableExists = reader.GetInt32(0) > 0;
                        }
                    }

                    if (!tableExists)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(createTableQuery, conn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "InitializeDatabaseAsync"); }
        }
    
        private async Task<bool> CanConnectToDatabaseAsync()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "CanConnectToDatabaseAsync"); 
                return false;
            }
        }

        private bool CreateDatabaseTables()
        {
            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    string createBaseShareTableQuery = @"
                        CREATE TABLE IF NOT EXISTS BaseShare (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CreatorName TEXT NOT NULL,
                            SteamId TEXT NOT NULL,
                            BaseName TEXT NOT NULL,
                            ShareCode TEXT NOT NULL UNIQUE,
                            Likes INTEGER DEFAULT 0,
                            Dislikes INTEGER DEFAULT 0,
                            Downloads INTEGER DEFAULT 0,
                            CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            ImageUrl TEXT DEFAULT NULL,
                            BaseUrl TEXT DEFAULT NULL
                        )";

                    using (var command = new SqliteCommand(createBaseShareTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string createVotesTableQuery = @"
                        CREATE TABLE IF NOT EXISTS votes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            BaseCode TEXT NOT NULL,
                            PlayerId TEXT NOT NULL,
                            VoteType TEXT CHECK(VoteType IN ('Upvote', 'Downvote')) NOT NULL,
                            UNIQUE(BaseCode, PlayerId)
                        )";

                    using (var command = new SqliteCommand(createVotesTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string createLocalBasesTableQuery = @"
                        CREATE TABLE IF NOT EXISTS SavedBases (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            BaseName TEXT NOT NULL,
                            PlayerId TEXT NOT NULL,
                            CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            BaseUrl TEXT DEFAULT NULL,
                            baseImageUrl TEXT DEFAULT NULL
                        )";

                    using (var command = new SqliteCommand(createLocalBasesTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    AddBaseUrlColumnIfNotExists(connection);

                    return true;
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "CreateDatabaseTables"); 
                return false;
            }
        }

        private void AddBaseUrlColumnIfNotExists(SqliteConnection connection)
        {
            string checkSavedBasesColumnQuery = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('SavedBases') 
                WHERE name = 'BaseUrl'";

            using (var checkCommand = new SqliteCommand(checkSavedBasesColumnQuery, connection))
            {
                var columnExists = (long)checkCommand.ExecuteScalar() > 0;

                if (!columnExists)
                {
                    string alterTableQuery = "ALTER TABLE SavedBases ADD COLUMN BaseUrl TEXT DEFAULT NULL";
                    using (var alterCommand = new SqliteCommand(alterTableQuery, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }
                }
            }

            string checkBaseShareColumnQuery = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('BaseShare') 
                WHERE name = 'BaseUrl'";

            using (var checkCommand = new SqliteCommand(checkBaseShareColumnQuery, connection))
            {
                var columnExists = (long)checkCommand.ExecuteScalar() > 0;

                if (!columnExists)
                {
                    string alterTableQuery = "ALTER TABLE BaseShare ADD COLUMN BaseUrl TEXT DEFAULT NULL";
                    using (var alterCommand = new SqliteCommand(alterTableQuery, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }
                }
            }

            string checkSavedBasesColumnQuery2 = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('SavedBases') 
            WHERE name = 'baseImageUrl'";

            using (var checkCommand = new SqliteCommand(checkSavedBasesColumnQuery2, connection))
            {
                var columnExists = (long)checkCommand.ExecuteScalar() > 0;

                if (!columnExists)
                {
                    string alterTableQuery = "ALTER TABLE SavedBases ADD COLUMN baseImageUrl TEXT DEFAULT NULL";
                    using (var alterCommand = new SqliteCommand(alterTableQuery, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private async Task<int> GetSavedBasesCount(string playerId)
        {
            int count = 0;
            string query = "SELECT COUNT(*) FROM SavedBases WHERE PlayerID = @PlayerID";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var checkCommand = new SqliteCommand(query, connection))
                {
                    checkCommand.Parameters.AddWithValue("@PlayerID", playerId);

                    count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                }
            }

            return count;
        }

        private async Task<bool> IsExistingBase(string saveName, string playerId)
        {
            bool exists = false;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM SavedBases WHERE BaseName = @BaseName AND PlayerId = @PlayerId";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BaseName", saveName);
                    command.Parameters.AddWithValue("@PlayerId", playerId);

                    long count = (long)await command.ExecuteScalarAsync();
                    exists = count > 0;
                }
            }

            return exists;
        }

        private string GetConnectionString()
        {
            if (_config.sqldata != null && _config.sqldata.Count > 0)
            {
                var mysqlData = _config.sqldata[0];
                return $"Server={mysqlData.server};User ID={mysqlData.username};Password={mysqlData.password};Port={mysqlData.port};Database={mysqlData.database};Pooling=true;MinimumPoolSize=1;MaximumPoolSize=10;Connection Timeout=3;";
            }
            else
            {
                throw new InvalidOperationException("MySQL configuration is missing or not properly set up.");
            }
        }

        public class PasteResponse
        {
            public string link { get; set; }
        }

        private async Task<string> UploadToPasteEeAsync(JObject data, string basename, BasePlayer player)
        {
            string pasteUrl = "https://api.paste.ee/v1/pastes";
            string authToken = _config.pastee_auth;

            var request = (HttpWebRequest)WebRequest.Create(pasteUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers["X-Auth-Token"] = authToken;

            var jsonData = new
            {
                title = $"{basename} - {player.userID}",
                description = $"{basename} - {player.userID}",
                sections = new[]
                {
                    new {
                        name = $"{basename} - {player.userID}",
                        syntax = "json",
                        contents = data.ToString()
                    }
                },
                expiration = "never"
            };

            using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                string jsonPayload = JsonConvert.SerializeObject(jsonData);
                await streamWriter.WriteAsync(jsonPayload);
                await streamWriter.FlushAsync();
            }

            try
            {
                var response = (HttpWebResponse)await request.GetResponseAsync();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = await streamReader.ReadToEndAsync();
                    var pasteResponse = JsonConvert.DeserializeObject<PasteResponse>(result);
                    return pasteResponse.link;
                }
            }
            catch (WebException ex)
            {
                using (var errorResponse = (HttpWebResponse)ex.Response)
                {
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorText = await reader.ReadToEndAsync();
                        LogErrors( errorText, "UploadToPasteEeAsync"); 
                        throw new Exception($"Failed to upload to Paste.ee: {errorText}");
                    }
                }
            }
        }

        private async Task SaveBaseToMysql(string playerId, string baseName, string baseDataJson)
        {
            string checkQuery = "SELECT COUNT(*) FROM `bases` WHERE playerid = @playerid AND basename = @basename";
            string insertQuery = "INSERT INTO bases (playerid, basename, basedata, creation_date) VALUES (@playerid, @basename, @basedata, @creation_date)";
            string updateQuery = "UPDATE bases SET basedata = @basedata, creation_date = @creation_date WHERE playerid = @playerid AND basename = @basename";

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString() + $"Database={_config.sqldata[0].database};"))
            {
                try
                {
                    conn.Open();

                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@playerid", playerId);
                        checkCmd.Parameters.AddWithValue("@basename", baseName);

                        int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (exists > 0)
                        {
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@basedata", baseDataJson);
                                updateCmd.Parameters.AddWithValue("@creation_date", DateTime.Now);
                                updateCmd.Parameters.AddWithValue("@playerid", playerId);
                                updateCmd.Parameters.AddWithValue("@basename", baseName);

                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@playerid", playerId);
                                insertCmd.Parameters.AddWithValue("@basename", baseName);
                                insertCmd.Parameters.AddWithValue("@basedata", baseDataJson);
                                insertCmd.Parameters.AddWithValue("@creation_date", DateTime.Now);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (MySqlException ex) { LogErrors(ex.Message, "SaveBaseToMysql Mysql"); }
                catch (Exception ex) { LogErrors(ex.Message, "SaveBaseToMysql Exception"); }
            }
        }

        private async Task SaveBaseToDb(string baseName, string playerId, BasePlayer player, JObject saveData)
        {
            try
            {
                string checkQuery = @"
                    SELECT COUNT(*) FROM SavedBases 
                    WHERE BaseName = @baseName AND PlayerId = @playerId";

                string insertQuery = @"
                    INSERT INTO SavedBases (BaseName, PlayerId, BaseUrl) 
                    VALUES (@baseName, @playerId, @baseUrl)";

                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var checkCommand = new SqliteCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@baseName", baseName);
                        checkCommand.Parameters.AddWithValue("@playerId", playerId);

                        var count = (long)await checkCommand.ExecuteScalarAsync();

                        string baseUrlSave = string.Empty;

                        if (count == 0)
                        {
                            if (_config.pastee_auth != "PASTE.EE TOKEN HERE")
                                baseUrlSave = await UploadToPasteEeAsync(saveData, baseName, player);

                            using (var insertCommand = new SqliteCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@baseName", baseName);
                                insertCommand.Parameters.AddWithValue("@playerId", playerId);
                                insertCommand.Parameters.AddWithValue("@baseUrl", baseUrlSave);

                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            SendMessage(player, "BaseUpdated", baseName);
                        }
                    }
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "SaveBaseToDb"); }
        }

        private async Task<bool> CheckBaseExistsInDb(string baseName, string playerId)
        {
            string query = @"
                SELECT COUNT(*) FROM baseshare 
                WHERE BaseName = @baseName AND SteamId = @playerId";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);

                    var count = (long)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        private async Task<string> GetShareCodeFromDb(string baseName, string playerId)
        {
            string query = @"
                SELECT ShareCode FROM baseshare 
                WHERE BaseName = @baseName AND SteamId = @playerId";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);

                    return (string)await command.ExecuteScalarAsync();
                }
            }
        }

        private async Task CheckAndAddNewBasesFromPasteAPI(string playerId)
        {
            try
            {
                var apiUrl = "https://api.paste.ee/v1/pastes";
                var request = WebRequest.Create(apiUrl);
                request.Method = "GET";
                request.Headers.Add("X-Auth-Token", $"{_config.pastee_auth}");

                using (var response = await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var jsonResponse = await reader.ReadToEndAsync();
                    var pasteData = JsonConvert.DeserializeObject<PasteResponse2>(jsonResponse);

                    foreach (var paste in pasteData.Data)
                    {
                        var pasteDetailUrl = $"https://api.paste.ee/v1/pastes/{paste.Id}";
                        var pasteDetailRequest = WebRequest.Create(pasteDetailUrl);
                        pasteDetailRequest.Method = "GET";
                        pasteDetailRequest.Headers.Add("X-Auth-Token", $"{_config.pastee_auth}");

                        using (var pasteDetailResponse = await pasteDetailRequest.GetResponseAsync())
                        using (var pasteDetailStream = pasteDetailResponse.GetResponseStream())
                        using (var pasteDetailReader = new StreamReader(pasteDetailStream))
                        {
                            var pasteDetailJson = await pasteDetailReader.ReadToEndAsync();
                            var pasteDetail = JsonConvert.DeserializeObject<PasteData>(pasteDetailJson);
                            
                            var baseDescription = paste.Description;
                            if (string.IsNullOrWhiteSpace(baseDescription) || !baseDescription.Contains(" - "))
                            {
                                // Puts($"Skipping paste with ID {paste.Id} due to invalid or missing description.");
                                continue;
                            }

                            var descriptionParts = baseDescription.Split(new[] { " - " }, StringSplitOptions.None);
                            var baseName = descriptionParts[0];
                            var steamId = descriptionParts[1];

                            if (string.IsNullOrWhiteSpace(baseName) || baseName == "Unnamed Base")
                            {
                                // Puts($"Skipping base with ID {paste.Id} due to 'Unnamed Base'.");
                                continue;
                            }

                            bool existsInDb = await CheckBaseExistsInDbSaved(baseName, playerId);

                            if (!existsInDb)
                            {
                                await DownloadAndSavePasteJson(paste.Id, playerId, baseName);

                                await AddBaseToDb(baseName, playerId, pasteDetailUrl);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "CheckAndAddNewBasesFromPasteAPI"); }
        }

        private async Task<bool> CheckBaseExistsInDbSaved(string baseName, string playerId)
        {
            string query = @"
                SELECT COUNT(*) FROM SavedBases 
                WHERE BaseName = @baseName AND PlayerId = @playerId";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);

                    var count = (long)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        private async Task DownloadAndSavePasteJson(string pasteId, string playerId, string baseName)
        {
            try
            {
                var sanitizedBaseName = Path.GetFileName(baseName);

                var folderPath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerId}");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                var pasteRawUrl = $"https://paste.ee/d/{pasteId}";
                var request = WebRequest.Create(pasteRawUrl);
                request.Method = "GET";

                using (var response = await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var rawContent = await reader.ReadToEndAsync();

                    var filePath = Path.Combine(folderPath, $"{sanitizedBaseName}.json");
                    await File.WriteAllTextAsync(filePath, rawContent);
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "DownloadAndSavePasteJson"); }
        }

        private async Task AddBaseToDb(string baseName, string steamId, string baseUrl)
        {
            string query = @"
                INSERT INTO SavedBases (BaseName, PlayerId, BaseUrl) 
                VALUES (@baseName, @playerId, @baseUrl)";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", steamId);
                    command.Parameters.AddWithValue("@baseUrl", baseUrl);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private BaseShareInfo GetBaseInfoForPlayer(string steamId, string baseName)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM BaseShare WHERE SteamId = @SteamId AND BaseName = @BaseName";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SteamId", steamId);
                    command.Parameters.AddWithValue("@BaseName", baseName);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new BaseShareInfo
                            {
                                CreatorName = reader["CreatorName"].ToString(),
                                SteamId = reader["SteamId"].ToString(),
                                BaseName = reader["BaseName"].ToString(),
                                ShareCode = reader["ShareCode"].ToString(),
                                Likes = Convert.ToInt32(reader["Likes"]),
                                Dislikes = Convert.ToInt32(reader["Dislikes"]),
                                Downloads = Convert.ToInt32(reader["Downloads"]),
                                ImageUrl = reader["ImageUrl"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        private string UploadImageToHostingService(string imagePath, BasePlayer player = null)
        {
            string clientId = _config.imgur_client_id;
            string uploadUrl = "https://api.imgur.com/3/image";

            if (!File.Exists(imagePath))
            {
                return "https://i.imgur.com/0QtCHOh.png";
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(uploadUrl);
                request.Method = "POST";
                request.ContentType = "multipart/form-data";
                request.Headers.Add("Authorization", $"Client-ID {clientId}");

                using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                using (var requestStream = request.GetRequestStream())
                {
                    var boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
                    var boundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
                    var endBoundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

                    request.ContentType = "multipart/form-data; boundary=" + boundary;

                    requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                    var header = $"Content-Disposition: form-data; name=\"image\"; filename=\"{Path.GetFileName(imagePath)}\"\r\nContent-Type: image/jpeg\r\n\r\n";
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    requestStream.Write(headerBytes, 0, headerBytes.Length);
                    fileStream.CopyTo(requestStream);
                    requestStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var responseData = reader.ReadToEnd();
                    var json = JObject.Parse(responseData);

                    if (json["success"]?.ToObject<bool>() == true)
                    {
                        return json["data"]["link"].ToString();
                    }
                    else
                    {
                        return "https://i.imgur.com/0QtCHOh.png";
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "UploadImageToHostingService");
                return imgbbstore(imagePath, player);
            }
        }

        private string imgbbstore(string imagePath, BasePlayer player)
        {
            string uploadUrl = $"https://api.imgbb.com/1/upload?key={_config.imgbb_api}";

            if (!File.Exists(imagePath))
            {
                return "https://i.imgur.com/0QtCHOh.png";
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(uploadUrl);
                request.Method = "POST";

                string boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
                request.ContentType = $"multipart/form-data; boundary={boundary}";

                using (var requestStream = request.GetRequestStream())
                {
                    var boundaryBytes = Encoding.UTF8.GetBytes($"--{boundary}\r\n");
                    var endBoundaryBytes = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

                    var header = $"Content-Disposition: form-data; name=\"image\"; filename=\"{Path.GetFileName(imagePath)}\"\r\nContent-Type: image/jpeg\r\n\r\n";
                    var headerBytes = Encoding.UTF8.GetBytes(header);

                    requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                    requestStream.Write(headerBytes, 0, headerBytes.Length);

                    using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(requestStream);
                    }

                    requestStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var responseJson = reader.ReadToEnd();
                    var json = JObject.Parse(responseJson);

                    if (json["status"]?.ToObject<int>() == 200)
                    {
                        string imageUrl = json["data"]["url"].ToString();
                        return imageUrl;
                    }
                    else
                    {
                        return "https://i.imgur.com/0QtCHOh.png";
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "imgbbstore");
                return "https://i.imgur.com/0QtCHOh.png";
            }
        }

        private string GetBaseUrlForSavedBase(string steamId, string baseName)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT BaseUrl 
                    FROM SavedBases 
                    WHERE PlayerId = @PlayerId AND BaseName = @BaseName 
                    LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerId", steamId);
                    command.Parameters.AddWithValue("@BaseName", baseName);

                    var result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        private async Task<bool> ShareCodeExists(string shareCode)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM BaseShare WHERE ShareCode = @ShareCode";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShareCode", shareCode);
                    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                }
            }
        }

        private async Task UpdateBaseVotesAsync(string shareCode, string voteType, int increment = 1)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                int currentCount = 0;
                string countQuery = voteType == "Upvote" ?
                    "SELECT Likes FROM BaseShare WHERE ShareCode = @ShareCode" :
                    "SELECT Dislikes FROM BaseShare WHERE ShareCode = @ShareCode";

                using (var countCommand = new SqliteCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue("@ShareCode", shareCode);
                    currentCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                if (voteType == "Upvote" && (currentCount + increment) >= 0)
                {
                    string updateQuery = "UPDATE BaseShare SET Likes = Likes + @Increment WHERE ShareCode = @ShareCode";
                    using (var updateCommand = new SqliteCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@ShareCode", shareCode);
                        updateCommand.Parameters.AddWithValue("@Increment", increment);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
                else if (voteType == "Downvote" && (currentCount + increment) >= 0)
                {
                    string updateQuery = "UPDATE BaseShare SET Dislikes = Dislikes + @Increment WHERE ShareCode = @ShareCode";
                    using (var updateCommand = new SqliteCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@ShareCode", shareCode);
                        updateCommand.Parameters.AddWithValue("@Increment", increment);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private async Task<string?> CheckExistingVoteAsync(string shareCode, string steamId)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT VoteType FROM votes WHERE BaseCode = @BaseCode AND PlayerId = @PlayerId";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BaseCode", shareCode);
                    command.Parameters.AddWithValue("@PlayerId", steamId);
                    var result = await command.ExecuteScalarAsync();
                    return result as string;
                }
            }
        }

        private async Task UpdateVoteInDatabaseAsync(string shareCode, string steamId, string voteType)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "UPDATE votes SET VoteType = @VoteType WHERE BaseCode = @BaseCode AND PlayerId = @PlayerId";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BaseCode", shareCode);
                    command.Parameters.AddWithValue("@PlayerId", steamId);
                    command.Parameters.AddWithValue("@VoteType", voteType);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SaveVoteToDatabaseAsync(string shareCode, string steamId, string voteType)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO votes (BaseCode, PlayerId, VoteType) VALUES (@BaseCode, @PlayerId, @VoteType)";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BaseCode", shareCode);
                    command.Parameters.AddWithValue("@PlayerId", steamId);
                    command.Parameters.AddWithValue("@VoteType", voteType);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private BaseShareInfo GetBaseInfo(string shareCode)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT * FROM BaseShare WHERE ShareCode = @ShareCode";

                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@ShareCode", shareCode);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new BaseShareInfo
                            {
                                CreatorName = reader["CreatorName"].ToString(),
                                SteamId = reader["SteamId"].ToString(),
                                BaseName = reader["BaseName"].ToString(),
                                ShareCode = reader["ShareCode"].ToString(),
                                Likes = reader.GetInt32(reader.GetOrdinal("Likes")),
                                Dislikes = reader.GetInt32(reader.GetOrdinal("Dislikes")),
                                Downloads = reader.GetInt32(reader.GetOrdinal("Downloads")),
                                ImageUrl = reader["ImageUrl"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        private void IncrementDownloads(string shareCode)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string updateQuery = "UPDATE BaseShare SET Downloads = Downloads + 1 WHERE ShareCode = @ShareCode";

                using (var command = new SqliteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@ShareCode", shareCode);
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Save & Load

        private void QueueBaseSave(string filePath, string saveName, ulong userId, BasePlayer player, JObject saveData) {
            string key = $"{userId}/{saveName}";
            lock (pendingSaves) {
                pendingSaves[key] = new QueuedSave {
                    Key = key,
                    FilePath = filePath,
                    SaveData = saveData,
                    SaveName = saveName,
                    UserId = userId,
                    Player = player
                };
                if (!saveQueue.Contains(key))
                    saveQueue.Enqueue(key);
            }
        }

        private void ProcessSaveQueue() {
            int processed = 0;
            while (processed < MaxBatchSaves && saveQueue.TryDequeue(out string key)) {
                QueuedSave save;
                lock (pendingSaves) {
                    if (!pendingSaves.TryGetValue(key, out save))
                        continue;
                    pendingSaves.Remove(key);
                }
                try {
                    Task.Run(async () => {
                        Interface.Oxide.DataFileSystem.WriteObject(save.FilePath, save.SaveData);
                        await SaveBaseToDb(save.SaveName, save.UserId.ToString(), save.Player, save.SaveData);
                        await SaveBaseToMysql(save.UserId.ToString(), save.SaveName, JsonConvert.SerializeObject(save.SaveData));
                        save.Player?.ChatMessage($"[Creative] Save '{save.SaveName}' completed.");
                    });

                } catch (Exception ex) {
                    LogErrors(ex.Message, "SaveBase Queue");
                    save.Player?.ChatMessage($"[Creative] Failed to save '{save.SaveName}'.");
                }
                processed++;
            }
        }

        private async Task Save(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (args.Length != 1)
            {
                player.ChatMessage("Usage: /save <base name>");
                return;
            }
            string saveName = args[0];
            SaveBase(player, saveName);
        }

        private JArray GetLineAnchors(IOEntity.LineAnchor[] lineAnchors, IOEntity ioEntity)
        {
            var anchors = new JArray();
            foreach (var anchor in lineAnchors)
            {
                var anchorData = new JObject
                {
                    ["position"] = new JObject
                    {
                        ["x"] = anchor.position.x.ToString(),
                        ["y"] = anchor.position.y.ToString(),
                        ["z"] = anchor.position.z.ToString()
                    },
                    ["index"] = anchor.index,
                    ["boneName"] = anchor.boneName,
                    ["entityRefID"] = anchor.entityRef.uid.Value
                };

                anchors.Add(anchorData);
            }

            return anchors;
        }

        private JArray SerializeLinePoints(Vector3[] linePoints)
        {
            if (linePoints == null)
            {
                return new JArray();
            }

            var jArray = new JArray();
            foreach (var point in linePoints)
            {
                var jObject = new JObject
                {
                    ["x"] = point.x,
                    ["y"] = point.y,
                    ["z"] = point.z
                };
                jArray.Add(jObject);
            }
            return jArray;
        }

        private float DegreeToRadian(float angle)
        {
            return (float)(Math.PI * angle / 180.0f);
        }

        private Vector3 NormalizePosition(Vector3 initialPos, Vector3 currentPos, float diffRot)
        {
            var dir = currentPos - initialPos;
            var rot = Quaternion.Euler(0f, -diffRot * Mathf.Rad2Deg, 0f);
            return rot * dir;
        }

        private Dictionary<string, object> EntityData(BaseEntity entity, Vector3 entPos, Vector3 entRot, Vector3 sourcePos, float rotationdiff, bool isChild = false)
        {
            var normalizedPos = NormalizePosition(sourcePos, entPos, rotationdiff);
            if (!isChild) entRot.y -= rotationdiff;
            var data = new Dictionary<string, object>
            {
                { "prefabname", entity.PrefabName },
                { "skinid", entity.skinID },
                { "flags", TryCopyFlags(entity) },
                {
                    "pos", new Dictionary<string, object>
                    {
                        { "x", isChild ? entity.transform.localPosition.x.ToString() : normalizedPos.x.ToString() },
                        { "y", isChild ? entity.transform.localPosition.y.ToString() : normalizedPos.y.ToString() },
                        { "z", isChild ? entity.transform.localPosition.z.ToString() : normalizedPos.z.ToString() }
                    }
                },
                {
                    "rot", new Dictionary<string, object>
                    {
                        { "x", isChild ? entity.transform.localRotation.eulerAngles.x.ToString() : entRot.x.ToString() },
                        { "y", isChild ? entity.transform.localRotation.eulerAngles.y.ToString() : entRot.y.ToString() },
                        { "z", isChild ? entity.transform.localRotation.eulerAngles.z.ToString() : entRot.z.ToString() }
                    }
                },
                { "ownerid", entity.OwnerID }
            };

            if (entity.HasParent())
            {
                if (entity.parentBone != 0)
                {
                    data.Add("parentbone", StringPool.Get(entity.parentBone));
                }
                if (GetSlot(entity.GetParentEntity(), entity, out BaseEntity.Slot? theslot) && theslot != null)
                {
                    data.Add("slot", (int)theslot);
                }
            }

            if (entity.children != null && entity.children.Count > 0)
            {
                var children = new List<object>();
                foreach (var child in entity.children)
                {
                    if (!child.IsValid())
                        continue;
                    children.Add(EntityData(child, child.transform.position, child.transform.rotation.eulerAngles, sourcePos, rotationdiff, true));
                }
                if (children.Count > 0)
                    data.Add("children", children);
            }

                    if (entity is BuildingBlock buildingBlock)
                    {
                data.Add("grade", (int)buildingBlock.grade);
                        if (buildingBlock.customColour != 0)
                    data.Add("customColour", buildingBlock.customColour);
                        if (buildingBlock.HasWallpaper())
                        {
                    data.Add("wallpaperID", (int)buildingBlock.wallpaperID);
                    data.Add("wallpaperHealth", (int)buildingBlock.wallpaperHealth);
                        }
                    }

                    if (entity is StorageContainer storageContainer)
                    {
                        var itemsArray = new JArray();
                        foreach (var item in storageContainer.inventory.itemList)
                        {
                            var itemData = new JObject
                            {
                                ["id"] = item.info.itemid,
                                ["amount"] = item.amount,
                                ["condition"] = item.condition,
                                ["skinid"] = item.skin,
                                ["position"] = item.position
                            };
                            itemsArray.Add(itemData);
                        }
                data.Add("items", itemsArray);
                    }

                    if (entity is IOEntity ioEntity && ioEntity.IsValid() && !ioEntity.IsDestroyed)
                    {
                        var ioData = new JObject();

                        var inputs = new JArray(ioEntity.inputs.Select(input => new JObject
                        {
                            ["connectedID"] = input.connectedTo.entityRef.uid.Value,
                            ["connectedToSlot"] = input.connectedToSlot,
                            ["niceName"] = input.niceName,
                            ["wireColour"] = (int)input.wireColour,
                            ["type"] = (int)input.type
                        }));

                        ioData["inputs"] = inputs;

                        var outputs = new JArray(ioEntity.outputs.Select(output => new JObject
                        {
                            ["connectedID"] = output.connectedTo.entityRef.uid.Value,
                            ["connectedToSlot"] = output.connectedToSlot,
                            ["niceName"] = output.niceName,
                            ["wireColour"] = (int)output.wireColour,
                            ["type"] = (int)output.type,
                            ["linePoints"] = SerializeLinePoints(output.linePoints),
                            ["lineAnchors"] = output.lineAnchors != null ? GetLineAnchors(output.lineAnchors, ioEntity) : null
                        }));

                        ioData["outputs"] = outputs;
                        ioData["oldID"] = ioEntity.net.ID.Value;

                        var electricalBranch = ioEntity as ElectricalBranch;
                        if (electricalBranch != null)
                        {
                            ioData.Add("branchAmount", electricalBranch.branchAmount);
                        }

                        var counter = ioEntity as PowerCounter;
                        if (counter != null)
                        {
                            ioData.Add("targetNumber", counter.GetTarget());
                            ioData.Add("counterNumber", counter.counterNumber);
                        }

                        var timerSwitch = ioEntity as TimerSwitch;
                        if (timerSwitch != null)
                        {
                            ioData.Add("timerLength", timerSwitch.timerLength);
                        }

                        var rfBroadcaster = ioEntity as IRFObject;
                        if (rfBroadcaster != null)
                        {
                            ioData.Add("frequency", rfBroadcaster.GetFrequency());
                        }

                        var seismicSensor = ioEntity as SeismicSensor;
                        if (seismicSensor != null)
                        {
                            ioData.Add("range", seismicSensor.range);
                        }

                        var digitalClock = ioEntity as DigitalClock;
                        if (digitalClock != null)
                        {
                            var alarms = new JArray();
                            foreach (var alarm in digitalClock.alarms)
                            {
                                alarms.Add(new JObject
                                {
                                    { "time", alarm.time },
                                    { "active", alarm.active },
                                });
                            }

                            ioData.Add("muted", digitalClock.muted);
                            ioData.Add("alarms", alarms);
                        }

                data["IOEntity"] = ioData;
                    }

                    var sign = entity as Signage;
                    if (sign != null && sign.textureIDs != null)
                    {
                        var signData = new JObject
                        {
                            ["locked"] = sign.IsLocked()
                        };

                        for (var num = 0; num < sign.textureIDs.Length; num++)
                        {
                            var textureId = sign.textureIDs[num];
                            if (textureId == 0)
                                continue;

                            var imageByte = FileStorage.server.Get(textureId, FileStorage.Type.png, sign.net.ID);
                            if (imageByte != null)
                            {
                                signData[$"texture{num}"] = Convert.ToBase64String(imageByte);
                            }
                        }

                data["sign"] = signData;
                        signData["amount"] = sign.textureIDs.Length;
                    }

                    if (entity is SleepingBag sleepingBag)
                    {
                        var sleepingBagData = new JObject
                        {
                            ["niceName"] = sleepingBag.niceName,
                            ["deployerUserID"] = sleepingBag.deployerUserID,
                            ["isPublic"] = sleepingBag.IsPublic()
                        };
                data["sleepingbag"] = sleepingBagData;
                    }

                    if (entity is AutoTurret autoTurret)
                    {
                        var autoTurretData = new JObject
                        {
                            ["authorizedPlayers"] = new JArray(autoTurret.authorizedPlayers.Select(p => p.userid))
                        };
                data["autoturret"] = autoTurretData;
                    }

                    if (entity is ContainerIOEntity ContainerIO)
                    {
                        var itemlist = new JArray();

                        foreach (var item in ContainerIO.inventory.itemList)
                        {
                            var itemdata = new JObject
                            {
                                { "condition", item.condition.ToString() },
                                { "id", item.info.itemid },
                                { "amount", item.amount },
                                { "skinid", item.skin },
                                { "position", item.position },
                                { "blueprintTarget", item.blueprintTarget },
                                { "dataInt", item.instanceData?.dataInt ?? 0 }
                            };

                            if (!string.IsNullOrEmpty(item.text))
                                itemdata["text"] = item.text;

                            var heldEnt = item.GetHeldEntity();

                            if (heldEnt != null)
                            {
                                var projectiles = heldEnt.GetComponent<BaseProjectile>();

                                if (projectiles != null)
                                {
                                    var magazine = projectiles.primaryMagazine;

                                    if (magazine != null)
                                    {
                                        itemdata.Add("magazine", new JObject
                                        {
                                            { magazine.ammoType.itemid.ToString(), magazine.contents }
                                        });
                                    }
                                }
                            }

                            if (item?.contents?.itemList != null)
                            {
                                var contents = new JArray();

                                foreach (var itemContains in item.contents.itemList)
                                {
                                    contents.Add(new JObject
                                    {
                                        { "id", itemContains.info.itemid },
                                        { "amount", itemContains.amount }
                                    });
                                }

                                itemdata["items"] = contents;
                            }

                            itemlist.Add(itemdata);
                        }

                data["items"] = itemlist;
                    }

                    if (entity is CCTV_RC cctvRc)
                    {
                        var cctvData = new JObject
                        {
                            ["yaw"] = cctvRc.yawAmount,
                            ["pitch"] = cctvRc.pitchAmount,
                            ["rcIdentifier"] = cctvRc.rcIdentifier
                        };
                data["cctv"] = cctvData;
                    }

                    if (entity is BuildingPrivlidge cupboard)
                    {
                        var cupboardData = new JObject
                        {
                            ["userid"] = new JArray(cupboard.authorizedPlayers.Select(y => y.userid)),
                            ["username"] = new JArray(cupboard.authorizedPlayers.Select(y => y.username))
                        };

                data["authedPlayers"] = cupboardData;
                    }

                    if (entity is VendingMachine vendingMachine)
                    {
                        var sellOrders = new JArray();
                        foreach (var vendItem in vendingMachine.sellOrders.sellOrders)
                        {
                            sellOrders.Add(new JObject
                            {
                                ["itemToSellID"] = vendItem.itemToSellID,
                                ["itemToSellAmount"] = vendItem.itemToSellAmount,
                                ["currencyID"] = vendItem.currencyID,
                                ["currencyAmountPerItem"] = vendItem.currencyAmountPerItem,
                                ["inStock"] = vendItem.inStock,
                                ["currencyIsBP"] = vendItem.currencyIsBP,
                                ["itemToSellIsBP"] = vendItem.itemToSellIsBP
                            });
                        }

                data["vendingmachine"] = new JObject
                        {
                            ["shopName"] = vendingMachine.shopName,
                            ["isBroadcasting"] = vendingMachine.IsBroadcasting(),
                            ["sellOrders"] = sellOrders
                        };
                    }

            return data;
        }

        private async Task SaveBase(BasePlayer player, string saveName)
        {
            try
            {
                if (!playerSettings.ContainsKey(player.userID) || !permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;

                if (baseCooldown.TryGetValue(player.userID, out float lastCommandTime))
                {
                    if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 1f)
                    {
                        SendMessage(player, "Cooldown");
                        return;
                    }
                }

                baseCooldown[player.userID] = UnityEngine.Time.realtimeSinceStartup;
                float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                var entities = new List<BaseEntity>();
                playerSettings[player.userID].CurrentBaseName = saveName;

                foreach (var networkable in BaseEntity.serverEntities)
                {
                    if (networkable is BaseEntity entity)
                    {
                        if (entity != null && Vector3.Distance(playerSettings[player.userID].StoredClaimedPlotLocation, entity.transform.position) <= plot_radius)
                        {
                            if (entity.OwnerID != 0)
                                entities.Add(entity);
                        }
                    }
                }

                var foundations = entities.Where(e => e is BuildingBlock && e.ShortPrefabName.Contains("foundation")).ToList();
                Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;
                if (foundations.Count > 0)
                {
                    float sumX = 0f, sumY = 0f, sumZ = 0f;
                    foreach (var f in foundations)
                    {
                        sumX += f.transform.position.x;
                        sumY += f.transform.position.y;
                        sumZ += f.transform.position.z;
                    }
                    Zone_Center_Pos = new Vector3(sumX / foundations.Count, sumY / foundations.Count, sumZ / foundations.Count);
                }

                int savedBasesCount = await GetSavedBasesCount(player.userID.ToString());
                bool isExistingBase = await IsExistingBase(saveName, player.userID.ToString());

                int maxSlots = permission.UserHasPermission(player.UserIDString, "creative.vip") 
                    ? _config.saved_bases_slots_vip 
                    : _config.saved_bases_slots_default;

                if (!isExistingBase && savedBasesCount >= maxSlots)
                {
                    SendMessage(player, "SaveLimitReached");
                    return;
                }

                var excludedPrefabs = new HashSet<string>
                {
                    "assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
                    "assets/bundled/prefabs/radtown/oil_barrel.prefab",
                    "assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
                    "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                    "assets/bundled/prefabs/radtown/crate_basic.prefab",
                    "assets/bundled/prefabs/radtown/vehicle_parts.prefab",
                    "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",
                    "assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab",
                    "assets/bundled/prefabs/radtown/crate_normal.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab",
                    "assets/bundled/prefabs/radtown/crate_tools.prefab",
                    "assets/bundled/prefabs/radtown/minecart.prefab",
                    "assets/bundled/prefabs/radtown/crate_normal_2_food.prefab",
                    "assets/bundled/prefabs/radtown/crate_mine.prefab",
                    "assets/bundled/prefabs/radtown/foodbox.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/vehicle_parts.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_tools.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
                    "assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab",
                    "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab",
                    "assets/content/vehicles/trains/workcart/subents/workcart_fuel_storage.prefab",
                    "assets/content/props/roadsigns/roadsign4.prefab",
                    "assets/content/props/roadsigns/roadsign2.prefab",
                    "assets/content/props/roadsigns/roadsign3.prefab",
                    "assets/content/props/roadsigns/roadsign6.prefab",
                    "assets/content/props/roadsigns/roadsign9.prefab",
                    "assets/content/props/roadsigns/roadsign5.prefab",
                    "assets/content/props/roadsigns/roadsign8.prefab",
                    "assets/content/props/roadsigns/roadsign7.prefab",
                    "assets/content/props/roadsigns/roadsign1.prefab",
                    "assets/prefabs/deployable/quarry/fuelstorage.prefab",
                    "assets/prefabs/deployable/quarry/hopperoutput.prefab",
                    "assets/content/vehicles/snowmobiles/subents/snowmobileitemstorage.prefab",
                    "assets/content/vehicles/snowmobiles/subents/snowmobilefuelstorage.prefab",
                    "assets/content/vehicles/mlrs/subents/mlrs_rocket_storage.prefab",
                    "assets/content/vehicles/mlrs/subents/mlrs_dashboard_storage.prefab",
                    "assets/prefabs/deployable/hot air balloon/subents/hab_storage.prefab",
                    "assets/content/vehicles/boats/rhib/subents/fuel_storage.prefab",
                    "assets/content/vehicles/modularcar/subents/modular_car_fuel_storage.prefab",
                    "assets/content/vehicles/modularcar/subents/modular_car_camper_storage.prefab",
                    "assets/prefabs/deployable/bbq/bbq.campermodule.prefab",
                    "assets/prefabs/deployable/locker/locker.campermodule.prefab",
                    "assets/content/vehicles/modularcar/subents/modular_car_i4_engine_storage.prefab",
                    "assets/content/vehicles/modularcar/subents/modular_car_v8_engine_storage.prefab",
                    "assets/content/vehicles/modularcar/subents/modular_car_1mod_trade.prefab",
                    "assets/content/vehicles/modularcar/subents/modular_car_1mod_storage.prefab",
                    "assets/content/vehicles/boats/rowboat/subents/fuel_storage.prefab",
                    "assets/content/vehicles/boats/rowboat/subents/rowboat_storage.prefab",
                    "assets/content/vehicles/bikes/subents/motorbikefuelstorage.prefab",
                    "assets/content/vehicles/boats/tugboat/subents/tugboat fuel_storage.prefab",
                    "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-2.prefab",
                    "assets/bundled/prefabs/autospawn/resource/loot/trash-pile-1.prefab",
                    "assets/bundled/prefabs/static/recycler_static.prefab",
                    "assets/bundled/prefabs/static/repairbench_static.prefab",
                    "assets/bundled/prefabs/static/hobobarrel_static.prefab",
                    "assets/bundled/prefabs/static/researchtable_static.prefab",
                    "assets/bundled/prefabs/static/small_refinery_static.prefab",
                    "assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab",
                    "assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_fishexchange.prefab",
                    "assets/bundled/prefabs/static/wall.frame.shopfront.metal.static.prefab",
                    "assets/prefabs/deployable/card table/subents/cardgameplayerstorage.prefab",
                    "assets/prefabs/deployable/card table/subents/cardgamepotstorage.prefab",
                    "assets/bundled/prefabs/static/workbench1.static.prefab",
                    "assets/prefabs/misc/marketplace/marketterminal.prefab",
                    "assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab",
                    "assets/prefabs/misc/casino/slotmachine/slotmachinestorage.prefab",
                    "assets/bundled/prefabs/static/bbq.static_hidden.prefab",
                    "assets/bundled/prefabs/static/workbench2.static.prefab",
                    "assets/bundled/prefabs/static/bbq.static.prefab"             
                };

                var excludedTrainPrefabs = new List<string>
                {
                    "assets/prefabs/deployable/cctvcamera/cctv.static.prefab",
                    "assets/bundled/prefabs/static/door.hinged.elevator_door.prefab",
                    "assets/prefabs/io/electric/switches/doormanipulator.invisible.prefab",
                    "assets/prefabs/deployable/elevator/static/elevator.static.prefab",
                    "assets/prefabs/io/electric/switches/pressbutton/pressbutton_trainstairwell.prefab",
                    "assets/prefabs/deployable/elevator/static/elevator.static.top.prefab",
                    "assets/bundled/prefabs/static/door.hinged.industrial_a_e.prefab",
                    "assets/bundled/prefabs/static/door.hinged.bunker.door.prefab",
                    "assets/prefabs/io/electric/switches/doormanipulator.prefab",
                    "assets/bundled/prefabs/static/door.hinged.vent.prefab",
                    "assets/bundled/prefabs/static/door.hinged.industrial_a_a.prefab",
                    "assets/bundled/prefabs/static/door.hinged.bunker_hatch.prefab" 
                };

                var filteredEntities = entities.FindAll(entity =>
                    !excludedTrainPrefabs.Contains(entity.PrefabName) &&
                    (
                        entity is BuildingBlock || 
                        entity is IOEntity || 
                        (entity is StorageContainer && !(entity is BasePlayer)) || 
                        entity is Door || 
                        entity is BuildingPrivlidge ||
                        entity is ContainerIOEntity ||
                        entity is Signage || 
                        entity is SleepingBag || 
                        entity is AutoTurret || 
                        entity is CCTV_RC ||
                        entity is Deployable ||
                        entity is SimpleBuildingBlock ||
                        entity is VendingMachine ||
                        entity is BaseOven ||
                        entity is ElectricOven ||
                        entity is Barricade ||
                        entity is BaseLadder
                    ) && 
                    !(entity is StorageContainer && excludedPrefabs.Contains(entity.PrefabName))
                );

                filteredEntities.Sort((x, y) =>
                {
                    bool xIsFoundation = x.ShortPrefabName.Contains("foundation");
                    bool yIsFoundation = y.ShortPrefabName.Contains("foundation");
                    if (xIsFoundation && !yIsFoundation) return -1;
                    if (!xIsFoundation && yIsFoundation) return 1;
                    return 0;
                });

                float rotationdiff = DegreeToRadian(player.GetNetworkRotation().eulerAngles.y);
                float rotationy = 0f;
                if (filteredEntities.Count > 0)
                {
                    rotationy = filteredEntities[0].GetNetworkRotation().eulerAngles.y;
                }

                var saveData = new Dictionary<string, object>();
                saveData["default"] = new Dictionary<string, object>
                {
                    { "position", new Dictionary<string, object>
                        {
                            { "x", Zone_Center_Pos.x.ToString() },
                            { "y", Zone_Center_Pos.y.ToString() },
                            { "z", Zone_Center_Pos.z.ToString() }
                        }
                    },
                    { "rotationdiff", rotationdiff.ToString() },
                    { "rotationy", rotationy.ToString() }
                };

                var entitiesArray = new List<object>();
                foreach (var entity in filteredEntities)
                {
                    entitiesArray.Add(EntityData(entity, entity.transform.position, entity.transform.rotation.eulerAngles / Mathf.Rad2Deg, Zone_Center_Pos, rotationdiff, false));
                }
                saveData["entities"] = entitiesArray;

                saveData["protocol"] = new Dictionary<string, object>
                {
                    { "items", 2 },
                    { "version", new Dictionary<string, object>
                        {
                            { "Major", 1 },
                            { "Minor", 0 },
                            { "Patch", 0 }
                        }
                    }
                };

                try
                {
                    var saveDataJObject = JObject.FromObject(saveData);
                    QueueBaseSave($"Creative/{player.userID}/{saveName}", saveName, player.userID, player, saveDataJObject);
                    player.ChatMessage($"[Creative] Save for {saveName} queued. It will be saved soon.");
                }
                catch (Exception ex)
                {
                    LogErrors(ex.Message, "SaveBase 1");
                    player.ChatMessage($"Failed to save entities");
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "SaveBase 2");
                Puts($"Exception on 'SaveBase': {ex}");
            }
        }

        private Dictionary<string, object> TryCopyFlags(BaseEntity entity)
        {
            var flags = new Dictionary<string, object>();

            foreach (BaseEntity.Flags flag in Enum.GetValues(typeof(BaseEntity.Flags)))
            {
                if (entity.HasFlag(flag))
                    flags.Add(flag.ToString(), entity.HasFlag(flag));
            }

            return flags;
        }

        private bool GetSlot(BaseEntity parent, BaseEntity child, out BaseEntity.Slot? slot)
        {
            slot = null;
            
            for (int s = 0; s < (int)BaseEntity.Slot.Count; s++)
            {
                var slotEnum = (BaseEntity.Slot)s;
                
                if (parent.HasSlot( slotEnum ) && parent.GetSlot( slotEnum ) == child)
                {
                    slot = slotEnum;
                    return true;
                }
            }
            
            return false;
        }

        private async void LoadCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (!player)
                return;

            if (!playerSettings.ContainsKey(player.userID))
                return;
            
            if (baseCooldown.TryGetValue(player.userID, out float lastCommandTime))
            {
                if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 5f)
                {
                    SendMessage(player, "Cooldown");
                    return;
                }
            }

            baseCooldown[player.userID] = UnityEngine.Time.realtimeSinceStartup;

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

            if (Zone_Center_Pos == Vector3.zero)
            {   
                SendMessage(player, "NoClaimed");
                return;
            }

            if (args.Length != 1)
            {
                player.ChatMessage("Usage: /load <base name>");
                return;
            }

            string saveName = args[0];
            var saveData = Interface.Oxide.DataFileSystem.ReadObject<JObject>($"Creative/{player.userID}/{saveName}");

            if (saveData == null)
            {
                player.ChatMessage($"No save file found with name {saveName}");
                return;
            }

            Quaternion playerRotation = player.transform.rotation;

            playerSettings[player.userID].CurrentBaseName = saveName;
            
            if (!playerSettings[player.userID].LoadingBase)
            {
                _ = ClearEntitiesForPlayer(player, playerSettings[player.userID].StoredClaimedPlotLocation);
                await Task.Delay(100);
                await LoadBase(saveData, player, Zone_Center_Pos, playerRotation, saveName);
            }else{
                player.ChatMessage($"Already loading your base. Please wait...");
            }
        }

        private async void LoadConsoleCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            var args = arg.Args;

            if (player == null)
                return;

            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;
            
            if (baseCooldown.TryGetValue(player.userID, out float lastCommandTime))
            {
                if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 5f)
                {
                    SendMessage(player, "Cooldown");
                    return;
                }
            }

            baseCooldown[player.userID] = UnityEngine.Time.realtimeSinceStartup;

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

            if (Zone_Center_Pos == Vector3.zero)
            {   
                SendMessage(player, "NoClaimed");
                return;
            }

            if (args.Length != 1)
            {
                player.ChatMessage("Usage: /load <base name>");
                return;
            }

            string saveName = args[0];
            var saveData = Interface.Oxide.DataFileSystem.ReadObject<JObject>($"Creative/{player.userID}/{saveName}");

            if (saveData == null)
            {
                player.ChatMessage($"No save file found with name {saveName}");
                return;
            }

            Quaternion playerRotation = player.transform.rotation;

            playerSettings[player.userID].CurrentBaseName = saveName;

            ClearAll(player);
            
            if (!playerSettings[player.userID].LoadingBase)
            {
                await LoadBase(saveData, player, Zone_Center_Pos, playerRotation, saveName);
            }else{
                player.ChatMessage($"Already loading your base. Please wait...");
            }
        }

        private async Task LoadBaseWithProtocol(JObject saveData, BasePlayer player, Vector3 zoneCenterPos, Quaternion rotation, string saveName)
        {
            try
            {
                if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;
                if (!playerSettings.ContainsKey(player.userID))
                    return;
                if (!saveData.TryGetValue("default", out var defaultToken) || !(defaultToken is JObject defaultObj))
                    return;
                if (!saveData.TryGetValue("entities", out var entitiesToken) || !(entitiesToken is JArray entitiesArray))
                    return;
                var posObj = defaultObj["position"] as JObject;
                float fileCenterX = float.Parse(posObj["x"].ToString());
                float fileCenterY = float.Parse(posObj["y"].ToString());
                float fileCenterZ = float.Parse(posObj["z"].ToString());
                float fileRotDiff = float.Parse(defaultObj["rotationdiff"].ToString());
                float fileRotY = float.Parse(defaultObj["rotationy"].ToString());
                float maxRaycastDistance = 1000.0f;
                int layers = LayerMask.GetMask("Terrain", "World", "Construction", "Default", "Deployed");
                RaycastHit hit;
                Vector3 raycastOrigin = zoneCenterPos + Vector3.up * 15f;
                if (!Physics.Raycast(raycastOrigin, Vector3.down, out hit, maxRaycastDistance, layers))
                    return;
                float terrainHeight = hit.point.y;
                float terrainYOffset = terrainHeight - (fileCenterY + zoneCenterPos.y);
                oldToNewEntityIdMap.Clear();
                player.ChatMessage($"<color=#aeed6f>Loading base '{saveName}'.</color>");
                await Task.Delay(1000);
                playerSettings[player.userID].LoadingBase = true;
                await SpawnEntitiesCopyPasteStyle(player, entitiesArray, zoneCenterPos, fileCenterX, fileCenterY, fileCenterZ, fileRotDiff, fileRotY, rotation, saveName, terrainYOffset);
            }
            catch (Exception ex) { LogErrors(ex.Message, "LoadBaseWithProtocol"); }
        }

        private async Task SpawnEntitiesCopyPasteStyle(BasePlayer player, JArray entitiesArray, Vector3 zoneCenterPos, float fileCenterX, float fileCenterY, float fileCenterZ, float fileRotDiff, float fileRotY, Quaternion rotation, string saveName, float terrainYOffset)
        {
            try
            {
                int totalEntities = entitiesArray.Count;
                int loadedEntities = 0;
                int lastReportedProgress = 0;
                int batchSize = CalculateDynamicBatchSize();
                List<(BaseEntity entity, JObject data)> spawnedEntities = new List<(BaseEntity entity, JObject data)>();
                List<StabilityEntity> stabilityEntities = new List<StabilityEntity>();

                JObject lowestFoundation = null;
                float lowestY = float.MaxValue;
                Vector3 lowestFoundationPos = Vector3.zero;
                foreach (var entityToken in entitiesArray)
                {
                    if (entityToken is JObject entityObj && entityObj["prefabname"]?.ToString().Contains("foundation") == true)
                    {
                        var posObj = entityObj["pos"] as JObject;
                        if (posObj != null)
                        {
                            float py = float.Parse(posObj["y"].ToString());
                            if (py < lowestY)
                            {
                                lowestY = py;
                                lowestFoundation = entityObj;
                                float px = float.Parse(posObj["x"].ToString());
                                float pz = float.Parse(posObj["z"].ToString());
                                lowestFoundationPos = new Vector3(px, py, pz);
                            }
                        }
                    }
                }

                float terrainBaseY = 0f;
                if (lowestFoundation != null)
                {
                    Vector3 rayOrigin = zoneCenterPos + (Quaternion.AngleAxis(player.transform.rotation.eulerAngles.y, Vector3.up) * lowestFoundationPos) + new Vector3(0, 100f, 0);
                    RaycastHit hit;
                    int layers = LayerMask.GetMask("Terrain", "World", "Default");
                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 200f, layers))
                    {
                        terrainBaseY = hit.point.y + 0.5f;
                    }
                    else
                    {
                        terrainBaseY = zoneCenterPos.y;
                    }
                }
                else
                {
                    terrainBaseY = zoneCenterPos.y;
                }

                float zoneRotationDeg = player.transform.rotation.eulerAngles.y;
                float zoneRotationRad = DegreeToRadian(zoneRotationDeg);
                Vector3 fileCenter = new Vector3(fileCenterX, fileCenterY, fileCenterZ);

                float yOffset = 0f;
                if (lowestFoundation != null)
                {
                    Quaternion zoneRotQ = Quaternion.AngleAxis(zoneRotationDeg, Vector3.up);
                    Vector3 rotatedFoundation = zoneRotQ * lowestFoundationPos;
                    float foundationWorldY = rotatedFoundation.y + zoneCenterPos.y;
                    yOffset = terrainBaseY - (foundationWorldY);
                }

                foreach (var entityToken in entitiesArray)
                {
                    if (entityToken is JObject entityObj)
                    {
                        var posObj = entityObj["pos"] as JObject;
                        var rotObj = entityObj["rot"] as JObject;
                        if (posObj == null || rotObj == null) continue;
                        float px = float.Parse(posObj["x"].ToString());
                        float py = float.Parse(posObj["y"].ToString());
                        float pz = float.Parse(posObj["z"].ToString());
                        float rx = float.Parse(rotObj["x"].ToString());
                        float ry = float.Parse(rotObj["y"].ToString());
                        float rz = float.Parse(rotObj["z"].ToString());
                        Vector3 localPos = new Vector3(px, py, pz);
                        Vector3 localRot = new Vector3(rx, ry, rz);

                        Quaternion zoneRotQ = Quaternion.AngleAxis(zoneRotationDeg, Vector3.up);
                        Vector3 rotatedPos = zoneRotQ * localPos;
                        Vector3 entityPos = rotatedPos + zoneCenterPos;
                        entityPos.y += yOffset;
                        Quaternion entityRot = Quaternion.Euler(localRot * Mathf.Rad2Deg) * zoneRotQ;

                        string prefabName = entityObj["prefabname"]?.ToString();
                        if (string.IsNullOrEmpty(prefabName)) continue;
                        var spawn_entity = GameManager.server.CreateEntity(prefabName, entityPos, entityRot);
                        if (spawn_entity != null)
                        {
                            var stabilityEntity = spawn_entity as StabilityEntity;
                            if (stabilityEntity != null && !stabilityEntity.grounded)
                            {
                                stabilityEntity.grounded = true;
                                stabilityEntities.Add(stabilityEntity);
                            }
                            var buildingBlock = spawn_entity as BuildingBlock;
                            if (buildingBlock != null)
                            {
                                NextTick(() =>
                                {
                                    if (entityObj["grade"] != null)
                                        buildingBlock.SetGrade((BuildingGrade.Enum)Convert.ToInt32(entityObj["grade"].ToString()));
                                    buildingBlock.SetHealthToMax();
                                    buildingBlock.StartBeingRotatable();
                                    buildingBlock.SendNetworkUpdate();
                                    if (entityObj["skinid"] != null)
                                        buildingBlock.skinID = Convert.ToUInt64(entityObj["skinid"]);
                                    buildingBlock.OwnerID = player.userID;
                                    buildingBlock.UpdateSkin();
                                    buildingBlock.ResetUpkeepTime();
                                    buildingBlock.UpdateSurroundingEntities();
                                    buildingBlock.SendNetworkUpdateImmediate();
                                    if (entityObj["wallpaperID"] != null && entityObj["wallpaperHealth"] != null)
                                    {
                                        buildingBlock.SetWallpaper(Convert.ToUInt64(entityObj["wallpaperID"]));
                                        buildingBlock.wallpaperHealth = Convert.ToUInt64(entityObj["wallpaperHealth"]);
                                    }
                                    if (entityObj["customColour"] != null)
                                        buildingBlock.SetCustomColour(Convert.ToUInt32(entityObj["customColour"]));
                                });
                            }
                            var decayEntity = spawn_entity as DecayEntity;
                            var nearestBuilding = FindNearestBuildingBlock(spawn_entity.transform.position);
                            if (decayEntity != null && nearestBuilding != null)
                            {
                                if (nearestBuilding.buildingID == 0)
                                    nearestBuilding.buildingID = BuildingManager.server.NewBuildingID();
                                decayEntity.AttachToBuilding(nearestBuilding.buildingID);
                                decayEntity.ResetUpkeepTime();
                            }
                            else if (decayEntity != null && nearestBuilding == null)
                            {
                                var newBuildingID = BuildingManager.server.NewBuildingID();
                                decayEntity.AttachToBuilding(newBuildingID);
                                decayEntity.ResetUpkeepTime();
                            }
                            spawn_entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
                            if (entityObj["skinid"] != null)
                                spawn_entity.skinID = Convert.ToUInt64(entityObj["skinid"]);
                            spawn_entity.SendNetworkUpdate();
                            spawn_entity.Spawn();
                            spawnedEntities.Add((spawn_entity, entityObj));

                            if (spawn_entity is IOEntity ioEntity)
                            {
                                if (entityObj["IOEntity"] is JObject ioData && ioData["oldID"] != null)
                                {
                                    uint oldID = Convert.ToUInt32(ioData["oldID"].ToString());
                                    oldToNewEntityIdMap[oldID] = ioEntity;
                                }
                            }
                        }
                    }
                    loadedEntities++;
                    int progress = Mathf.FloorToInt(((float)loadedEntities / totalEntities) * 100);
                    if (progress >= (lastReportedProgress + 100 / 6))
                    {
                        player.ChatMessage($"Loading base... {progress}%");
                        lastReportedProgress = progress;
                    }
                    if ((loadedEntities % batchSize) == 0)
                    {
                        await Task.Delay(_config.base_load_ms);
                    }
                }
                await Task.Delay(1000);
                NextTick(() =>
                {
                    foreach (var stabilityEntity in stabilityEntities)
                    {
                        stabilityEntity.grounded = false;
                        stabilityEntity.InitializeSupports();
                        stabilityEntity.UpdateStability();
                    }
                    foreach (var (spawnedEntity, entityData) in spawnedEntities)
                    {
                        if (spawnedEntity is IOEntity ioEntity)
                        {
                            MapConnectedIDs(ioEntity, entityData, true);
                        }

                        if (entityData["children"] is JArray childrenArray)
                        {
                            foreach (var childToken in childrenArray)
                            {
                                if (childToken is JObject childObj && childObj["IOEntity"] is JObject childIOData)
                                {
                                    if (childIOData["oldID"] != null)
                                    {
                                        uint childOldID = Convert.ToUInt32(childIOData["oldID"].ToString());
                                        if (oldToNewEntityIdMap.TryGetValue(childOldID, out IOEntity childIOEntity))
                                        {
                                            MapConnectedIDs(childIOEntity, childObj, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    foreach (var (spawnedEntity, entityData) in spawnedEntities)
                    {
                        RestoreIOEntityData(spawnedEntity, entityData);
                        RestoreAdditionalEntityData(spawnedEntity, entityData);
                        spawnedEntity.OwnerID = player.userID;
                        spawnedEntity.SendNetworkUpdateImmediate();
                    }
                    player.ChatMessage($"'{saveName}' Base successfully loaded!");
                    timer.Once(2f, () =>
                    {
                        playerSettings[player.userID].LoadingBase = false;
                    });
                });
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "SpawnEntitiesCopyPasteStyle");
            }
        }

        private async Task LoadBase(JObject saveData, BasePlayer player, Vector3 position, Quaternion rotation, string saveName)
        {
            try
            {
                if (saveData["protocol"] != null)
                {
                    await LoadBaseWithProtocol(saveData, player, position, rotation, saveName);
                    return;
                }

                await Task.Delay(1500);

                if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use"))
                    return;

                if (!playerSettings.ContainsKey(player.userID))
                return;

                Vector3 zoneCenterPos = playerSettings[player.userID].StoredClaimedPlotLocation;

                if (zoneCenterPos == Vector3.zero)
                    return;

                zoneCenterPos.y += 15f;

                float maxRaycastDistance = 1000.0f;
                int layers = LayerMask.GetMask("Terrain", "World", "Construction", "Default", "Deployed");

                RaycastHit hit;
                if (!Physics.Raycast(zoneCenterPos, Vector3.down, out hit, maxRaycastDistance, layers))
                    return;

                float terrainHeight = hit.point.y;

                if (saveData.TryGetValue("entities", out var entitiesToken) && entitiesToken is JArray entitiesArray)
                {
                    JObject lowestFoundation = null;
                    float lowestY = float.MaxValue;

                    foreach (var entityToken in entitiesArray)
                    {
                        if (entityToken is JObject entityObj && entityObj["prefabname"]?.ToString().Contains("foundation") == true)
                        {
                            var posData = entityObj["pos"]?.ToString();
                            if (posData != null)
                            {
                                var posSplit = posData.Split(' ');
                                if (posSplit.Length == 3)
                                {
                                    float foundationY = float.Parse(posSplit[1]);
                                    if (foundationY < lowestY)
                                    {
                                        lowestY = foundationY;
                                        lowestFoundation = entityObj;
                                    }
                                }
                            }
                        }
                    }

                    if (lowestFoundation == null)
                    {
                        player.ChatMessage("No suitable foundation found in the base data.");
                        return;
                    }

                    var lowestFoundationPos = lowestFoundation["pos"]?.ToString().Split(' ');
                    float lowestFoundationY = float.Parse(lowestFoundationPos[1]);
                    float terrainYOffset = terrainHeight - (lowestFoundationY + zoneCenterPos.y);

                    oldToNewEntityIdMap.Clear();
                    player.ChatMessage($"<color=#aeed6f>Loading base '{saveName}'.</color>");
                    
                    await Task.Delay(1000);

                    playerSettings[player.userID].LoadingBase = true;
                    await SpawnEntitiesWithOffset(player, entitiesArray, zoneCenterPos, rotation, saveName, terrainYOffset);
                }
                else
                {
                    DeleteBase(player, saveName);
                    SendReply(player, "Base not found. \nSorry but this base was deleted!");
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "LoadBase"); }
        }

        private int CalculateDynamicBatchSize()
        {
            float memoryUsagePercentage = (Performance.report.memoryUsageSystem / SystemInfo.systemMemorySize) * 100;
            float frameRate = Performance.report.frameRate;

            if (memoryUsagePercentage > 80f || frameRate < 15f)
                return 5;
            else if (memoryUsagePercentage > 50f || frameRate < 30f)
                return 10;

            return 20;
        }

        private async Task SpawnEntitiesWithOffset(BasePlayer player, JArray entitiesArray, Vector3 zoneCenterPos, Quaternion rotation, string saveName, float terrainYOffset)
        {
            try
            {
                int attempts = 0;
                while (!PerformanceCheck())
                {
                    attempts++;
                    
                    if (attempts >= 10)
                    {
                        player.ChatMessage("Server performance is too low. Cancelling base load.");
                        playerSettings[player.userID].LoadingBase = false;
                        return;
                    }

                    player.ChatMessage($"Bad server performance. \nAttempt {attempts} / 10. Please wait...");
                    await Task.Delay(3000);
                }

                Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

                int totalEntitiespBatch = entitiesArray.Count;
                int baseSizeBatch = totalEntitiespBatch <= 50 ? 50 : (totalEntitiespBatch <= 200 ? 35 : 20);
                int serverLoadBatch = CalculateDynamicBatchSize();

                int batchSize = Math.Min(baseSizeBatch, serverLoadBatch);

                List<(BaseEntity entity, JObject data)> spawnedEntities = new List<(BaseEntity entity, JObject data)>();
                List<StabilityEntity> stabilityEntities = new List<StabilityEntity>();
                int lastReportedProgress = 0;
                var buildingBlocks = new List<JObject>();
                var otherEntities = new List<JObject>();

                foreach (var entityToken in entitiesArray)
                {
                    if (entityToken is JObject entityObj)
                    {
                        string prefabName = entityObj["prefabname"]?.ToString();
                        string prefabType = entityObj["entityType"]?.ToString();
                        if (prefabName != null && (prefabName.Contains("building core") || prefabName.Contains("foundation") || prefabName.Contains("wall") || prefabName.Contains("buildingblock")))
                        {
                            buildingBlocks.Add(entityObj);
                        }
                        else
                        {
                            otherEntities.Add(entityObj);
                        }
                    }
                }

                int totalEntities = buildingBlocks.Count + otherEntities.Count;
                int loadedEntities = 0;

                foreach (var entityObj in buildingBlocks)
                {
                    var prefabName = entityObj["prefabname"]?.ToString();
                    var posData = entityObj["pos"]?.ToString();
                    var rotData = entityObj["rot"]?.ToString();

                    if (!string.IsNullOrEmpty(prefabName) && !string.IsNullOrEmpty(posData) && !string.IsNullOrEmpty(rotData))
                    {
                        var posSplit = posData.Split(' ');
                        var rotSplit = rotData.Split(' ');

                        if (posSplit.Length == 3 && rotSplit.Length == 3)
                        {
                            Vector3 entityPos = new Vector3(
                                float.Parse(posSplit[0]) + zoneCenterPos.x,
                                float.Parse(posSplit[1]) + zoneCenterPos.y + terrainYOffset,
                                float.Parse(posSplit[2]) + zoneCenterPos.z
                            );

                            entityPos.y += 1f;

                            Quaternion entityRot = Quaternion.Euler(
                                float.Parse(rotSplit[0]),
                                float.Parse(rotSplit[1]),
                                float.Parse(rotSplit[2])
                            ) * rotation;

                            var spawn_entity = GameManager.server.CreateEntity(prefabName, entityPos, entityRot);
                            if (spawn_entity != null)
                            {
                                var stabilityEntity = spawn_entity as StabilityEntity;
                                if (stabilityEntity != null && !stabilityEntity.grounded)
                                {
                                    stabilityEntity.grounded = true;
                                    stabilityEntities.Add(stabilityEntity);
                                }
                                
                                var buildingBlock = spawn_entity as BuildingBlock;
                                if (buildingBlock != null)
                                {
                                    NextTick(() =>
                                    {
                                        buildingBlock.SetGrade((BuildingGrade.Enum)(int)entityObj["grade"]);
                                        buildingBlock.SetHealthToMax();
                                        buildingBlock.StartBeingRotatable();
                                        buildingBlock.SendNetworkUpdate();
                                        buildingBlock.skinID = Convert.ToUInt64(entityObj["skinid"]);
                                        buildingBlock.OwnerID = player.userID;
                                        buildingBlock.flags = (BaseEntity.Flags)(int)entityObj["flags"];
                                        buildingBlock.UpdateSkin();
                                        buildingBlock.ResetUpkeepTime();
                                        buildingBlock.UpdateSurroundingEntities();
                                        buildingBlock.SendNetworkUpdateImmediate();

                                        if (entityObj["wallpaperID"] != null && entityObj["wallpaperHealth"] != null)
                                        {
                                            buildingBlock.SetWallpaper(Convert.ToUInt64(entityObj["wallpaperID"]));
                                            buildingBlock.wallpaperHealth = Convert.ToUInt64(entityObj["wallpaperHealth"]);
                                        }

                                        if (entityObj["customColour"] != null)
                                            buildingBlock.SetCustomColour(Convert.ToUInt32(entityObj["customColour"]));
                                    });
                                }

                                var decayEntity = spawn_entity as DecayEntity;
                                var nearestBuilding = FindNearestBuildingBlock(spawn_entity.transform.position);

                                if (decayEntity != null && nearestBuilding != null)
                                {
                                    if (nearestBuilding.buildingID == 0)
                                        nearestBuilding.buildingID = BuildingManager.server.NewBuildingID();

                                    decayEntity.AttachToBuilding(nearestBuilding.buildingID);
                                    decayEntity.ResetUpkeepTime();
                                }
                                else if (decayEntity != null && nearestBuilding == null)
                                {
                                    var newBuildingID = BuildingManager.server.NewBuildingID();
                                    decayEntity.AttachToBuilding(newBuildingID);
                                    decayEntity.ResetUpkeepTime();
                                }

                                spawn_entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
                                spawn_entity.skinID = Convert.ToUInt64(entityObj["skinid"]);
                                spawn_entity.SendNetworkUpdate();
                                spawn_entity.Spawn();
                                spawnedEntities.Add((spawn_entity, entityObj));   
                            }
                        }
                    }

                    loadedEntities++;
                    int progress = Mathf.FloorToInt(((float)loadedEntities / totalEntities) * 100);
                    
                    if (progress >= (lastReportedProgress + 100 / 6))
                    {
                        player.ChatMessage($"Loading base... {progress}%");
                        lastReportedProgress = progress;
                    }

                    if ((loadedEntities % batchSize) == 0)
                    {
                        await Task.Delay(_config.base_load_ms);
                    }
                }

                foreach (var entityObj in otherEntities)
                {
                    var prefabName = entityObj["prefabname"]?.ToString();
                    var posData = entityObj["pos"]?.ToString();
                    var rotData = entityObj["rot"]?.ToString();

                    if (!string.IsNullOrEmpty(prefabName) && !string.IsNullOrEmpty(posData) && !string.IsNullOrEmpty(rotData))
                    {
                        var posSplit = posData.Split(' ');
                        var rotSplit = rotData.Split(' ');

                        if (posSplit.Length == 3 && rotSplit.Length == 3)
                        {
                            Vector3 entityPos = new Vector3(
                                float.Parse(posSplit[0]) + zoneCenterPos.x,
                                float.Parse(posSplit[1]) + zoneCenterPos.y + terrainYOffset,
                                float.Parse(posSplit[2]) + zoneCenterPos.z
                            );

                            entityPos.y += 1f;

                            Quaternion entityRot = Quaternion.Euler(
                                float.Parse(rotSplit[0]),
                                float.Parse(rotSplit[1]),
                                float.Parse(rotSplit[2])
                            ) * rotation;

                            var spawn_entity = GameManager.server.CreateEntity(prefabName, entityPos, entityRot);
                            if (spawn_entity != null)
                            {
                                if (spawn_entity is BuildingBlock)
                                {
                                    var stabilityEntity = spawn_entity as StabilityEntity;
                                    if (stabilityEntity != null && !stabilityEntity.grounded)
                                    {
                                        stabilityEntity.grounded = true;
                                        stabilityEntities.Add(stabilityEntity);
                                    }
                                }

                                spawn_entity.OwnerID = player.userID;
                                spawn_entity.flags = (BaseEntity.Flags)(int)entityObj["flags"];

                                var decayEntity = spawn_entity as DecayEntity;
                                var nearestBuilding = FindNearestBuildingBlock(spawn_entity.transform.position);

                                if (decayEntity != null && nearestBuilding != null)
                                {
                                    if (nearestBuilding.buildingID == 0)
                                        nearestBuilding.buildingID = BuildingManager.server.NewBuildingID();

                                    var cupboard = spawn_entity as BuildingPrivlidge;
                                    if (cupboard != null)
                                    {
                                        RestoreBuildingPrivlidge(cupboard, entityObj, player);
                                    }
                                    decayEntity.AttachToBuilding(nearestBuilding.buildingID);
                                    decayEntity.ResetUpkeepTime();
                                }
                                else if (decayEntity != null && nearestBuilding == null)
                                {
                                    var cupboard = spawn_entity as BuildingPrivlidge;
                                    if (cupboard != null)
                                    {
                                        RestoreBuildingPrivlidge(cupboard, entityObj, player);
                                    }
                                    var newBuildingID = BuildingManager.server.NewBuildingID();
                                    decayEntity.AttachToBuilding(newBuildingID);
                                    decayEntity.ResetUpkeepTime();
                                }

                                spawn_entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
                                spawn_entity.skinID = Convert.ToUInt64(entityObj["skinid"]);
                                spawn_entity.SendNetworkUpdate();
                                spawn_entity.Spawn();
                                spawnedEntities.Add((spawn_entity, entityObj));
                            }
                        }
                    }

                    loadedEntities++;
                    int progress = Mathf.FloorToInt(((float)loadedEntities / totalEntities) * 100);

                    if ((loadedEntities % batchSize) == 0)
                    {
                        await Task.Delay(_config.deployable_load_ms);
                    }
                }

                await Task.Delay(1000);

                NextTick(() =>
                {
                    foreach (var stabilityEntity in stabilityEntities)
                    {
                        stabilityEntity.grounded = false;
                        stabilityEntity.InitializeSupports();
                        stabilityEntity.UpdateStability();
                    }

                    foreach (var (spawnedEntity, entityData) in spawnedEntities)
                    {
                        if (spawnedEntity is IOEntity ioEntity)
                        {
                            // Aquí puedes usar entityData para buscar el ID, etc.
                            if (entityData.TryGetValue("entityID", out var entityIdToken) && uint.TryParse(entityIdToken.ToString(), out uint entityID))
                            {
                                oldToNewEntityIdMap[entityID] = ioEntity;
                            }
                            MapConnectedIDs(ioEntity, entityData);
                        }

                        RestoreIOEntityData(spawnedEntity, entityData);
                        RestoreAdditionalEntityData(spawnedEntity, entityData);
                        spawnedEntity.OwnerID = player.userID;
                        spawnedEntity.SendNetworkUpdateImmediate();
                    }

                    player.ChatMessage($"'{saveName}' Base successfully loaded!");
                    timer.Once(2f, () =>
                    {
                        playerSettings[player.userID].LoadingBase = false;
                    });
                });
            }
            catch (Exception ex)
            {
                LogErrors(ex.Message, "SpawnEntitiesWithOffset");
            }
        }

        private async void DeleteBase(BasePlayer player, string baseName)
        {
            string playerId = player.userID.ToString();
            string basePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerId}/{baseName}.json");

            string baseUrl = null;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT BaseUrl FROM SavedBases WHERE BaseName = @baseName AND PlayerId = @playerId";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            baseUrl = reader["BaseUrl"]?.ToString();
                        }
                    }
                }

                string deleteQuery = "DELETE FROM SavedBases WHERE BaseName = @baseName AND PlayerId = @playerId";
                using (var command = new SqliteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.ExecuteNonQuery();
                }

                string deleteBaseShareQuery = "DELETE FROM BaseShare WHERE BaseName = @baseName AND SteamId = @playerId";
                using (var command = new SqliteCommand(deleteBaseShareQuery, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.ExecuteNonQuery();
                }
            }

            if (File.Exists(basePath))
            {
                File.Delete(basePath);
            }

            if (!string.IsNullOrEmpty(baseUrl))
            {
                await DeleteBaseFromPasteAPI(baseUrl);
            }
            
             Puts($"Base '{baseName}' deleted locally and from paste.ee (if applicable).");

        }

        private async Task DeleteBaseFromPasteAPI(string baseUrl)
        {
            try
            {
                var pasteId = baseUrl.Replace("https://paste.ee/p/", "");

                var apiUrl = $"https://api.paste.ee/v1/pastes/{pasteId}";
                var request = WebRequest.Create(apiUrl);
                request.Method = "DELETE";
                request.Headers.Add("X-Auth-Token", _config.pastee_auth);

                using (var response = await request.GetResponseAsync())
                {
                     Puts($"Paste with ID '{pasteId}' successfully deleted from paste.ee.");
                }
            }

            catch (WebException webEx)
            {
                using (var stream = webEx.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var responseMessage = await reader.ReadToEndAsync();
                    PrintError($"Failed to delete paste: {responseMessage}");
                    LogErrors( responseMessage, "DeleteBaseFromPasteAPI | WebException");
                }
            }
            catch (Exception ex) { LogErrors(ex.Message, "DeleteBaseFromPasteAPI | Exception"); }
        }

        private BuildingBlock FindNearestBuildingBlock(Vector3 position)
        {
            Collider[] colliders = Physics.OverlapSphere(position, 20f, LayerMask.GetMask("Construction"));
            float minDistance = float.MaxValue;
            BuildingBlock nearestEntity = null;

            foreach (Collider collider in colliders)
            {
                BuildingBlock entity = collider.GetComponentInParent<BuildingBlock>();
                if (entity != null)
                {
                    float distance = Vector3.Distance(position, entity.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestEntity = entity;
                    }
                }
            }

            return nearestEntity;
        }

        private void RestoreBuildingPrivlidge(BuildingPrivlidge cupboard, JObject data, BasePlayer player)
        {
            if (cupboard == null)
                return;

            if (data.TryGetValue("authedPlayers", out var authedPlayersToken))
            {
                var authedPlayers = authedPlayersToken as JObject;
                if (authedPlayers == null)
                    return;

                var userIds = authedPlayers["userid"] as JArray;
                var usernames = authedPlayers["username"] as JArray;

                if (userIds != null && usernames != null && userIds.Count == usernames.Count)
                {
                    for (int i = 0; i < userIds.Count; i++)
                    {
                        var userId = (ulong)userIds[i];
                        var username = usernames[i].ToString();
                        cupboard.authorizedPlayers.Add(new PlayerNameID { userid = userId, username = username });
                    }
                }
            }

            if (player != null)
            {
                cupboard.authorizedPlayers.Add(new PlayerNameID { userid = player.userID, username = player.displayName });

                if (player.currentTeam != 0)
                {
                    var team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    if (team != null)
                    {
                        foreach (var memberId in team.members)
                        {
                            BasePlayer member = BasePlayer.FindByID(memberId);
                            if (member != null)
                            {
                                cupboard.authorizedPlayers.Add(new PlayerNameID { userid = member.userID, username = member.displayName });
                            }
                        }
                    }
                }
            }

            cupboard.SendNetworkUpdate();
        }

        private void MapConnectedIDs(IOEntity ioEntity, JObject entityData, bool cp = false)
        {
            var ioData = entityData["IOEntity"] as JObject;
            if (ioData != null)
            {
                if (cp)
                {
                    MapEntityID(ioEntity, ioData, "inputs");
                    MapEntityID(ioEntity, ioData, "outputs");
                }else{
                    MapEntityIDCP(ioEntity, ioData, "inputs");
                    MapEntityIDCP(ioEntity, ioData, "outputs");
                }
            }
        }

        private void MapEntityIDCP(IOEntity ioEntity, JObject ioData, string key)
        {
            var slotArray = ioData[key] as JArray;
            if (slotArray != null)
            {
                foreach (var slotToken in slotArray)
                {
                    var slotData = slotToken as JObject;
                    if (slotData != null)
                    {
                        uint oldConnectedID = Convert.ToUInt32(slotData["connectedID"].ToString());
                        if (oldToNewEntityIdMap.TryGetValue(oldConnectedID, out IOEntity connectedEntity))
                        {
                            int slotIndex = Convert.ToInt32(slotData["connectedToSlot"].ToString());
                            if (key == "inputs" && slotIndex >= 0 && slotIndex < ioEntity.inputs.Length)
                            {
                                ioEntity.inputs[slotIndex].connectedTo.entityRef.uid = new NetworkableId(Convert.ToUInt64(connectedEntity.net.ID.Value));
                                ioEntity.inputs[slotIndex].connectedToSlot = slotIndex;
                            }
                            else if (key == "outputs" && slotIndex >= 0 && slotIndex < ioEntity.outputs.Length)
                            {
                                ioEntity.outputs[slotIndex].connectedTo.entityRef.uid = new NetworkableId(Convert.ToUInt64(connectedEntity.net.ID.Value));
                                ioEntity.outputs[slotIndex].connectedToSlot = slotIndex;
                            }
                        }
                    }
                }
            }
        }

        private void MapEntityID(IOEntity ioEntity, JObject ioData, string key)
        {
            var slotArray = ioData[key] as JArray;
            if (slotArray != null)
            {
                foreach (var slotToken in slotArray)
                {
                    var slotData = slotToken as JObject;
                    if (slotData != null)
                    {
                        uint oldConnectedID = (uint)slotData["connectedID"];
                        if (oldToNewEntityIdMap.TryGetValue(oldConnectedID, out IOEntity connectedEntity))
                        {
                            int slotIndex = (int)slotData["connectedToSlot"];
                            if (key == "inputs" && slotIndex >= 0 && slotIndex < ioEntity.inputs.Length)
                            {
                                ioEntity.inputs[slotIndex].connectedTo.entityRef.uid = new NetworkableId(Convert.ToUInt64(connectedEntity.net.ID.Value));
                                ioEntity.inputs[slotIndex].connectedToSlot = slotIndex;
                            }
                            else if (key == "outputs" && slotIndex >= 0 && slotIndex < ioEntity.outputs.Length)
                            {
                                ioEntity.outputs[slotIndex].connectedTo.entityRef.uid = new NetworkableId(Convert.ToUInt64(connectedEntity.net.ID.Value));
                                ioEntity.outputs[slotIndex].connectedToSlot = slotIndex;
                            }
                        }
                    }
                }
            }
        }

        private void RestoreIOEntityData(BaseEntity entity, JObject entityData)
        {
            if (entity is IOEntity ioEntity && entityData.TryGetValue("IOEntity", out var ioDataToken) && ioDataToken is JObject ioData)
            {
                RestoreConnections(ioEntity, ioData, "inputs");
                RestoreConnections(ioEntity, ioData, "outputs");

                if (ioEntity is ElectricalBranch electricalBranch && ioData.TryGetValue("branchAmount", out var branchAmountToken))
                {
                    electricalBranch.branchAmount = branchAmountToken.ToObject<int>();
                }

                if (ioEntity is TimerSwitch timerSwitch && ioData.TryGetValue("timerLength", out var timerLengthToken))
                {
                    timerSwitch.timerLength = timerLengthToken.ToObject<float>();
                }

                if (ioEntity is PowerCounter powerCounter)
                {
                    if (ioData.TryGetValue("targetNumber", out var targetNumberToken))
                    {
                        powerCounter.targetCounterNumber = targetNumberToken.ToObject<int>();
                    }
                    if (ioData.TryGetValue("counterNumber", out var counterNumberToken))
                    {
                        powerCounter.counterNumber = counterNumberToken.ToObject<int>();
                    }
                }

                if (ioEntity is SeismicSensor seismicSensor && ioData.TryGetValue("range", out var rangeToken))
                {
                    seismicSensor.range = rangeToken.ToObject<int>();
                }

                if (ioEntity is DigitalClock digitalClock)
                {
                    if (ioData.TryGetValue("muted", out var mutedToken))
                    {
                        digitalClock.muted = mutedToken.ToObject<bool>();
                    }

                    if (ioData.TryGetValue("alarms", out var alarmsToken) && alarmsToken is JArray alarmsArray)
                    {
                        var restoredAlarms = new List<DigitalClock.Alarm>();

                        foreach (var alarmToken in alarmsArray)
                        {
                            if (alarmToken is JObject alarmData &&
                                alarmData.TryGetValue("time", out var timeToken) &&
                                alarmData.TryGetValue("active", out var activeToken))
                            {
                                var time = TimeSpan.Parse(timeToken.ToObject<string>());
                                var active = activeToken.ToObject<bool>();
                                restoredAlarms.Add(new DigitalClock.Alarm { time = time, active = active });
                            }
                        }

                        digitalClock.alarms = restoredAlarms;
                    }

                    digitalClock.MarkDirty();
                    digitalClock.SendNetworkUpdate();
                }

                ioEntity.MarkDirty();
            }
        }

        private void RestoreConnections(IOEntity ioEntity, JObject ioData, string key)
        {
            var slotArray = ioData[key] as JArray;
            if (slotArray != null)
            {
                foreach (var slotToken in slotArray)
                {
                    var slotData = slotToken as JObject;
                    if (slotData != null)
                    {
                        uint oldConnectedID = Convert.ToUInt32(slotData["connectedID"].ToString());
                        int connectedToSlot = Convert.ToInt32(slotData["connectedToSlot"].ToString());
                        IOEntity.IOType type = (IOEntity.IOType)Convert.ToInt32(slotData["type"].ToString());

                        if (oldToNewEntityIdMap.TryGetValue(oldConnectedID, out IOEntity connectedEntity))
                        {
                            if (key == "outputs" && ioEntity.outputs.Length > connectedToSlot)
                            {
                                ioEntity.outputs[connectedToSlot].connectedTo.entityRef.uid =
                                    new NetworkableId(Convert.ToUInt64(connectedEntity.net.ID.Value));

                                ioEntity.outputs[connectedToSlot].connectedToSlot = connectedToSlot;
                                ioEntity.outputs[connectedToSlot].type = type;

                                var linePointsArray = slotData["linePoints"] as JArray;
                                if (linePointsArray != null)
                                {
                                    ioEntity.outputs[connectedToSlot].linePoints = DeserializeLinePoints(linePointsArray);
                                }

                                ioEntity.outputs[connectedToSlot].connectedTo.Init();
                            }
                            else if (key == "inputs" && ioEntity.inputs.Length > connectedToSlot)
                            {
                                ioEntity.inputs[connectedToSlot].connectedTo.entityRef.uid =
                                    new NetworkableId(Convert.ToUInt64(connectedEntity.net.ID.Value));

                                ioEntity.inputs[connectedToSlot].connectedToSlot = connectedToSlot;
                                ioEntity.inputs[connectedToSlot].type = type;

                                ioEntity.inputs[connectedToSlot].connectedTo.Init();
                            }
                        }
                    }
                }
            }
        }


























        private class SignSize
        {
            public int Width;
            public int Height;

            public SignSize(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        private Dictionary<string, SignSize> _signSizes = new Dictionary<string, SignSize>
        {
            { "sign.pictureframe.landscape", new SignSize(256, 128) },
            { "sign.pictureframe.tall", new SignSize(128, 512) },
            { "sign.pictureframe.portrait", new SignSize(128, 256) },
            { "sign.pictureframe.xxl", new SignSize(1024, 512) },
            { "sign.pictureframe.xl", new SignSize(512, 512) },
            { "sign.small.wood", new SignSize(128, 64) },
            { "sign.medium.wood", new SignSize(256, 128) },
            { "sign.large.wood", new SignSize(256, 128) },
            { "sign.huge.wood", new SignSize(512, 128) },
            { "sign.hanging.banner.large", new SignSize(64, 256) },
            { "sign.pole.banner.large", new SignSize(64, 256) },
            { "sign.post.single", new SignSize(128, 64) },
            { "sign.post.double", new SignSize(256, 256) },
            { "sign.post.town", new SignSize(256, 128) },
            { "sign.post.town.roof", new SignSize(256, 128) },
            { "sign.hanging", new SignSize(128, 256) },
            { "sign.hanging.ornate", new SignSize(256, 128) },
            { "sign.neon.xl.animated", new SignSize(250, 250) },
            { "sign.neon.xl", new SignSize(250, 250) },
            { "sign.neon.125x215.animated", new SignSize(215, 125) },
            { "sign.neon.125x215", new SignSize(215, 125) },
            { "sign.neon.125x125", new SignSize(125, 125) },
        };

        private void FixSignage(Signage sign, byte[] imageBytes, int index)
        {
            if (!_signSizes.ContainsKey(sign.ShortPrefabName))
                return;

            var size = Math.Max(sign.paintableSources.Length, 1);
            if (sign.textureIDs == null || sign.textureIDs.Length != size)
            {
                Array.Resize(ref sign.textureIDs, size);
            }

            var resizedImage = ImageResize(imageBytes, _signSizes[sign.ShortPrefabName].Width,
                _signSizes[sign.ShortPrefabName].Height);

            sign.textureIDs[index] = FileStorage.server.Store(resizedImage, FileStorage.Type.png, sign.net.ID);
        }

        private void RestoreAdditionalEntityData(BaseEntity entity, JObject entityData)
        {
            var data = entityData.ToObject<Dictionary<string, object>>();

            var autoTurret = entity as AutoTurret;
            if (autoTurret != null)
            {
                var authorizedPlayers = new List<ulong>();

                if (data["autoturret"] is JObject autoTurretData)
                {                    
                    if (autoTurretData["authorizedPlayers"] is JArray playersList)
                    {
                        authorizedPlayers = playersList.Select(item => Convert.ToUInt64(item.ToString())).ToList();
                    }
                }

                foreach (var userId in authorizedPlayers)
                {
                    autoTurret.authorizedPlayers.Add(new PlayerNameID { userid = userId, username = "Player" });
                }
                autoTurret.SendNetworkUpdate();
            }

            var containerIo = entity as ContainerIOEntity;
            if (containerIo != null)
            {
                JObject jObjectData = JObject.FromObject(data);
                RestoreContainerIoEntity(containerIo, jObjectData, autoTurret);
            }

            var lights = entity as AdvancedChristmasLights;
            if (lights != null)
            {
                var jObjectData = JObject.FromObject(data);
                RestoreAdvancedChristmasLights(lights, jObjectData);
            }

            var sleepingBag = entity as SleepingBag;
            if (sleepingBag != null && data.ContainsKey("sleepingbag"))
            {
                var jObjectData = JObject.FromObject(data);
                RestoreSleepingBag(sleepingBag, jObjectData);
            }

            var cctvRc = entity as CCTV_RC;
            if (cctvRc != null && data.ContainsKey("cctv"))
            {
                var jObjectData = JObject.FromObject(data);
                RestoreCctvRc(cctvRc, jObjectData);
            }

            var vendingMachine = entity as VendingMachine;
            if (vendingMachine != null && data.ContainsKey("vendingmachine"))
            {
                var jObjectData = JObject.FromObject(data);
                RestoreVendingMachine(vendingMachine, jObjectData);
            }

            var sign = entity as Signage;
            if (sign != null)
            {
                JObject jObjectData = JObject.FromObject(data);
                RestoreSignage(sign, jObjectData);
            }
        }

        private Vector3 DeserializeVector3(JToken token)
        {
            return new Vector3((float)token["x"], (float)token["y"], (float)token["z"]);
        }

        private Vector3[] DeserializeLinePoints(JArray array)
        {
            return array.Select(point => DeserializeVector3(point)).ToArray();
        }

        private void RestoreSignage(Signage sign, JObject data)
        {
            if (data.TryGetValue("sign", out JToken signToken))
            {
                var signData = signToken as JObject;

                if (signData != null)
                {
                    if (signData.TryGetValue("amount", out JToken amountToken))
                    {
                        if (int.TryParse(amountToken.ToString(), out int amount))
                        {
                            for (var num = 0; num < amount; num++)
                            {
                                if (signData.TryGetValue($"texture{num}", out JToken textureToken))
                                {
                                    var imageBytes = Convert.FromBase64String(textureToken.ToString());
                                    FixSignage(sign, imageBytes, num);
                                }
                            }
                        }
                    }
                    else if (signData.TryGetValue("texture", out JToken textureToken))
                    {
                        var imageBytes = Convert.FromBase64String(textureToken.ToString());
                        FixSignage(sign, imageBytes, 0);
                    }

                    if (signData.TryGetValue("locked", out JToken lockedToken))
                    {
                        bool isLocked = Convert.ToBoolean(lockedToken);
                        if (isLocked)
                        {
                            sign.SetFlag(BaseEntity.Flags.Locked, true);
                        }
                    }

                    sign.SendNetworkUpdate();
                }
            }
        }

        private void RestoreContainerIoEntity(ContainerIOEntity containerIo, JObject data, AutoTurret turret = null)
        {
            if (containerIo.inventory == null)
            {
                containerIo.CreateInventory(true);
                containerIo.OnInventoryFirstCreated(containerIo.inventory);
            }
            else
            {
                containerIo.inventory.Clear();
            }

            if (data["items"] is JArray itemsArray)
            {
                foreach (var itemDef in itemsArray)
                {
                    if (itemDef is JObject itemJson)
                    {
                        var itemid = itemJson["id"]?.ToObject<int>() ?? 0;
                        var itemamount = itemJson["amount"]?.ToObject<int>() ?? 0;
                        var itemskin = itemJson["skinid"]?.ToObject<ulong>() ?? 0;
                        var itemcondition = itemJson["condition"]?.ToObject<float>() ?? 0;
                        var dataInt = itemJson["dataInt"]?.ToObject<int>() ?? 0;

                        var item = ItemManager.CreateByItemID(itemid, itemamount, itemskin);
                        if (item != null)
                        {
                            item.condition = itemcondition;

                            if (itemJson["text"] != null)
                                item.text = itemJson["text"].ToString();

                            if (itemJson["name"] != null)
                                item.name = itemJson["name"].ToString();

                            if (itemJson["blueprintTarget"] != null)
                                item.blueprintTarget = itemJson["blueprintTarget"].ToObject<int>();

                            if (dataInt > 0)
                                item.instanceData = new ProtoBuf.Item.InstanceData { ShouldPool = false, dataInt = dataInt };

                            if (itemJson["magazine"] is JObject magazine)
                            {
                                var ammoType = magazine.Properties().First().Name;
                                var ammoAmount = magazine[ammoType]?.ToObject<int>() ?? 0;

                                var heldEntity = item.GetHeldEntity();
                                if (heldEntity != null)
                                {
                                    var projectiles = heldEntity.GetComponent<BaseProjectile>();
                                    if (projectiles != null)
                                    {
                                        projectiles.primaryMagazine.ammoType = ItemManager.FindItemDefinition(int.Parse(ammoType));
                                        projectiles.primaryMagazine.contents = ammoAmount;

                                        if (itemJson["items"] is JArray itemContainsList)
                                        {
                                            foreach (var itemContains in itemContainsList)
                                            {
                                                if (itemContains is JObject contents)
                                                {
                                                    var contentsItemId = contents["id"]?.ToObject<int>() ?? 0;
                                                    var contentsAmount = contents["amount"]?.ToObject<int>() ?? 0;
                                                    item.contents.AddItem(ItemManager.FindItemDefinition(contentsItemId), contentsAmount);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            item.position = itemJson["position"]?.ToObject<int>() ?? -1;
                            containerIo.inventory.Insert(item);
                        }
                    }
                }
            }

            if (turret != null)
            {
                turret.Invoke(turret.UpdateAttachedWeapon, 0.5f);
            }

            containerIo.SendNetworkUpdate();
        }

        private void RestoreAdvancedChristmasLights(AdvancedChristmasLights lights, JObject data)
        {
            if (data["points"] is JArray pointsArray)
            {
                foreach (var pointData in pointsArray)
                {
                    if (pointData is JObject point)
                    {
                        var normal = point["normal"]?.ToObject<Vector3>() ?? default(Vector3);
                        var pointPos = point["point"]?.ToObject<Vector3>() ?? default(Vector3);
                        lights.points.Add(new AdvancedChristmasLights.pointEntry { normal = normal, point = pointPos });
                    }
                }
            }

            if (data["animationStyle"] != null)
            {
                lights.animationStyle = (AdvancedChristmasLights.AnimationType)data["animationStyle"].ToObject<int>();
            }

            lights.SendNetworkUpdate();
        }

        private void RestoreSleepingBag(SleepingBag sleepingBag, JObject data)
        {
            if (data["sleepingbag"] is JObject sleepingBagData)
            {
                sleepingBag.deployerUserID = sleepingBagData["deployerUserID"]?.ToObject<ulong>() ?? 0;
                sleepingBag.niceName = sleepingBagData["niceName"]?.ToString() ?? "";
                sleepingBag.SetPublic(sleepingBagData["isPublic"]?.ToObject<bool>() ?? false);
                sleepingBag.SendNetworkUpdate();
            }
        }

        private void RestoreCctvRc(CCTV_RC cctvRc, JObject data)
        {
            if (data["cctv"] is JObject cctvData)
            {
                cctvRc.rcIdentifier = cctvData["rcIdentifier"]?.ToString() ?? "";
                cctvRc.yawAmount = cctvData["yaw"]?.ToObject<float>() ?? 0;
                cctvRc.pitchAmount = cctvData["pitch"]?.ToObject<float>() ?? 0;
            }

            cctvRc.SendNetworkUpdate();
        }

        private void RestoreVendingMachine(VendingMachine vendingMachine, JObject data)
        {
            if (data.TryGetValue("vendingmachine", out var vendingDataToken) && vendingDataToken is JObject vendingData)
            {
                if (vendingData.TryGetValue("shopName", out var shopNameToken))
                {
                    vendingMachine.shopName = shopNameToken.ToObject<string>();
                }

                if (vendingData.TryGetValue("isBroadcasting", out var isBroadcastingToken))
                {
                    vendingMachine.SetFlag(BaseEntity.Flags.Reserved1, isBroadcastingToken.ToObject<bool>());
                }

                if (vendingData.TryGetValue("sellOrders", out var sellOrdersToken) && sellOrdersToken is JArray sellOrdersArray)
                {
                    var sellOrderContainer = new ProtoBuf.VendingMachine.SellOrderContainer
                    {
                        ShouldPool = false,
                        sellOrders = new List<ProtoBuf.VendingMachine.SellOrder>()
                    };

                    foreach (var sellOrderToken in sellOrdersArray)
                    {
                        if (sellOrderToken is JObject sellOrderData)
                        {
                            var sellOrder = new ProtoBuf.VendingMachine.SellOrder
                            {
                                ShouldPool = false,
                                itemToSellID = sellOrderData["itemToSellID"]?.ToObject<int>() ?? 0,
                                itemToSellAmount = sellOrderData["itemToSellAmount"]?.ToObject<int>() ?? 0,
                                currencyID = sellOrderData["currencyID"]?.ToObject<int>() ?? 0,
                                currencyAmountPerItem = sellOrderData["currencyAmountPerItem"]?.ToObject<int>() ?? 0,
                                currencyIsBP = sellOrderData["currencyIsBP"]?.ToObject<bool>() ?? false,
                                itemToSellIsBP = sellOrderData["itemToSellIsBP"]?.ToObject<bool>() ?? false
                            };

                            sellOrderContainer.sellOrders.Add(sellOrder);
                        }
                    }

                    vendingMachine.sellOrders = sellOrderContainer;
                }

                vendingMachine.FullUpdate();
            }
        }

        private byte[] ImageResize(byte[] imageBytes, int width, int height)
        {
            Bitmap resizedImage = new Bitmap(width, height),
                sourceImage = new Bitmap(new MemoryStream(imageBytes));

            Graphics.FromImage(resizedImage).DrawImage(sourceImage, new Rectangle(0, 0, width, height),
                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), GraphicsUnit.Pixel);

            var ms = new MemoryStream();
            resizedImage.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }

        #endregion

        #region Claim Zones & Markers

        private void CreateMapMarker(string markerID, string text, Vector3 position, float radius, BasePlayer player, bool exist = false)
        {
            float wrldSize = ConVar.Server.worldsize;
            wrldSize = wrldSize / 25;

            if (!_config.plot_mapmarker)
                return;

            MarkerData markerData = new MarkerData
            {
                MarkerID = markerID,
                Text = text,
                Position = position,
                Radius = radius,
                OwnerID = player.userID
            };
            markerDataList.Add(markerData);

            MapMarkerGenericRadius mapMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;

            if (mapMarker != null)
            {
                mapMarker.alpha = 0.6f;
                mapMarker.color1 = UnityEngine.Color.green;
                mapMarker.color2 = UnityEngine.Color.white;
                mapMarker.name = markerID;
                mapMarker.radius = radius / wrldSize;
                mapMarker.OwnerID = player.userID;
                mapMarker.Spawn();
                mapMarker.SendUpdate();
                sphereMarker[markerID] = mapMarker;
            }
            
            if (!exist)
            {
                var markerPrefab = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", position) as VendingMachineMapMarker;
                if (markerPrefab != null)
                {
                    markerPrefab.markerShopName = text;
                    markerPrefab.SetFlag(BaseEntity.Flags.Busy, true, false, true);
                    markerPrefab.Spawn();
                    markerPrefab.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    machineMarkers[markerID] = markerPrefab;
                }
            }
        }

        private void RemoveMarker(string markerID)
        {
            if (sphereMarker.ContainsKey(markerID))
            {
                MapMarkerGenericRadius marker = sphereMarker[markerID];
                if (marker != null && !marker.IsDestroyed)
                {
                    marker.Kill();
                }
                sphereMarker.Remove(markerID);
            }

            if (machineMarkers.ContainsKey(markerID))
            {
                VendingMachineMapMarker marker = machineMarkers[markerID];
                if (marker != null)
                {
                    marker.Kill();
                }
                machineMarkers.Remove(markerID);
            }

            markerDataList.RemoveAll(m => m.MarkerID == markerID);
        }

        private void RemoveAllMarkers()
        {
            foreach (var Smarker in sphereMarker.Values)
            {
                if (Smarker != null)
                {
                    Smarker.Kill();
                }
            }
            sphereMarker.Clear();

            foreach (var marker in machineMarkers.Values)
            {
                if (marker != null)
                {

                    marker.Kill();
                }
            }
            machineMarkers.Clear();
            markerDataList.Clear();
        }

        private bool DoesZoneCollide(Vector3 position, float radius)
        {
            if (_config.lobby_pos != Vector3.zero)
            {
                float distance_lobby = Vector3.Distance(position, _config.lobby_pos);

                if (distance_lobby < radius + 100f)
                    return true;
            }

            foreach (var kvp in activeZones)
            {
                var existingZone = kvp.Value;

                float distance = Vector3.Distance(position, existingZone.transform.position);

                if (distance < radius + existingZone.GetColliderRadius())
                {
                    return true;
                }
            }

            return false;
        }

        private ulong FindClosestZone(BasePlayer player)
        {
            ulong closestZoneID = 0;
            float closestDistance = float.MaxValue;
            Vector3 playerPosition = player.transform.position;

            foreach (var kvp in activeZones)
            {
                var existingZone = kvp.Value;
                float distance = Vector3.Distance(playerPosition, existingZone.transform.position);

                if (distance < closestDistance)
                {
                    if (existingZone.GetOwner() == player.userID || player.Team != null && player.Team.members.Contains(existingZone.GetOwner()))
                    {
                        closestDistance = distance;
                        closestZoneID = kvp.Key;
                    }
                }
            }

            return closestZoneID;
        }

        private float GetPlayerToGroundDistance(BasePlayer player)
        {
            RaycastHit hit;
            float maxDistance = 30f;
            Vector3 pos = player.transform.position;
            pos.y += 0.5f;

            if (Physics.Raycast(pos, Vector3.down, out hit, maxDistance, LayerMask.GetMask("Water", "Terrain", "World", "Default")))
            {
                return hit.distance;
            }

            return maxDistance;
        }

        private bool IsPlayerNearTerrainOrWater(Vector3 position, float minimumDistance, BasePlayer player)
        {
            return GetPlayerToGroundDistance(player) < 10f;
        }

        private void ClaimZone(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (FindClosestZone(player) != player.userID)
            {
                Vector3 playerPosition = player.transform.position;

                if (!IsPlayerNearTerrainOrWater(playerPosition, 10f, player))
                {
                    SendMessage(player, "ClaimTerrain");
                    return;
                }

                float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                if (DoesZoneCollide(playerPosition, plot_radius))
                {
                    SendMessage(player, "AreaOverlap");
                    return;
                }

                var zoneTrigger = new GameObject().AddComponent<PlotManager>();
                zoneTrigger.CreateBubble(playerPosition, plot_radius, player);
                zoneTrigger.CenterZone = playerPosition;
                activeZones.Add(player.userID, zoneTrigger);

                playerSettings[player.userID].PlayerOwnZone = true;
                playerSettings[player.userID].StoredClaimedPlotLocation = playerPosition;
                
                CreateMapMarker(player.userID.ToString(), player.displayName, playerPosition, plot_radius, player);

                SendReply(player, $"Plot successfully claimed!");
            }
            else{
                SendMessage(player, "OwnArea");
            }
        }

        private void UnclaimZone(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;
                
            ulong closestZoneID = FindClosestZone(player);
            if (closestZoneID != player.userID)
            {
                SendReply(player, "You do not have any zones to unclaim.");
                return;
            }

            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (playerSettings[player.userID].LoadingBase)
            {
                SendReply(player, "You can't unclaim this plot while you're loading a base!");
                return;
            }

            if (activeZones.TryGetValue(closestZoneID, out var zoneTrigger))
            {
                if (zoneTrigger.GetOwner() == player.userID || player.IsAdmin && permission.UserHasPermission(player.UserIDString, "creative.admin"))
                {
                    ClearAll(player);
                    zoneTrigger.DeleteCircle();
                    activeZones.Remove(closestZoneID);
                    UnityEngine.Object.DestroyImmediate(zoneTrigger.gameObject);

                    RemoveMarker(player.userID.ToString());
                    playerSettings[player.userID].PlayerOwnZone = false;
                    playerSettings[player.userID].StoredClaimedPlotLocation = Vector3.zero;

                    SendReply(player, $"You've unclaimed your plot!");
                }
                else
                {
                    SendReply(player, "You do not have permission to unclaim this plot.");
                }
            }
        }

        #endregion

        
        #region UI

        private void Add_CuiPanel(CuiElementContainer container, bool cursor, string img_color, string anchorMin, string anchorMax, string offsetMin, string offsetMax, string layer, string panel_name)
        {
            container.Add(new CuiPanel
            {
                CursorEnabled = cursor,
                Image = { Color = img_color, FadeIn = 0f, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform ={ AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax },
            }, layer, panel_name);
        }

        void Add_CuiScrollView(CuiElementContainer container, string name, string parent, bool vertical, bool horizontal, string anchorMin, string anchorMax, string offsetMin, string offsetMax, string contentAnchorMin, string contentAnchorMax, string contentOffsetMin, string contentOffsetMax, bool inv = true)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components = {
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax },
                    new CuiScrollViewComponent
                    {
                        Vertical = vertical,
                        Horizontal = horizontal,
                        ContentTransform = new CuiRectTransform
                        {
                            AnchorMin = contentAnchorMin,
                            AnchorMax = contentAnchorMax,
                            OffsetMin = contentOffsetMin,
                            OffsetMax = contentOffsetMax
                        },
                        HorizontalScrollbar = new CuiScrollbar
                        {
                            Invert = inv
                        }
                    }
                }
            });
        }

        private void Add_CuiButton(CuiElementContainer container, string command, string button_color, string text, string font, int fontsize, TextAnchor anchor, string text_color, string anchorMin, string anchorMax, string offsetMin, string offsetMax, string panel, string btn_name)
        {
            container.Add(new CuiButton
            {
                Button = { Command = command, Color = button_color, FadeIn = 0f, Material = "assets/icons/iconmaterial.mat" },
                Text = { Text = text, Font = font, FontSize = fontsize, Align = anchor, Color = text_color },
                RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax },
            }, panel, btn_name);
        }

        private void Add_CuiElement(CuiElementContainer container, string name, string parent, string text, string font, int fontsize, TextAnchor anchor, string text_color, string outline_color, string outline_distance, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components = 
                {
                    new CuiTextComponent { Text = text, Font = font, FontSize = fontsize, Align = anchor, Color = text_color, FadeIn = 0f },
                    new CuiOutlineComponent { Color = outline_color, Distance = outline_distance },
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }
                }
            });
        }

        private void Add_CuiElementImage(CuiElementContainer container, string name, string parent, string  outline_color, string outline_distance, string url, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components = {
                    new CuiRawImageComponent{ Png = url, FadeIn = 0f },
                    new CuiOutlineComponent { Color = outline_distance, Distance = outline_distance },
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }
                }
            });
        }

        private void Add_CuiElementImage_Url(CuiElementContainer container, string name, string parent, string  outline_color, string outline_distance, string url, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components = {
                    new CuiRawImageComponent{ Url = url, FadeIn = 0f },
                    new CuiOutlineComponent { Color = outline_distance, Distance = outline_distance },
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }
                }
            });
        }

        private void Add_CuiElementImageColor(CuiElementContainer container, string name, string parent, string outline_color, string outline_distance, string color, string anchorMin, string anchorMax, string offsetMin, string offsetMax)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components = {
                    new CuiImageComponent { Color = color },
                    new CuiOutlineComponent { Color = outline_color, Distance = outline_distance },
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }
                }
            });
        }

        private string GetListButtonColor(string item, BasePlayer player = null)
        {
            foreach (var settings in playerSettings.Values)
            {
                if (settings.DoorPrefab.Contains(item) ||
                    settings.DoubleDoorPrefab.Contains(item) ||
                    settings.WindowPrefab.Contains(item) ||
                    settings.EmbrasurePrefab.Contains(item))
                {
                    return "0.25 0.8 0.15 0.2";
                }

                foreach (var electricityItem in settings.ElectricityList)
                {
                    if (electricityItem.Contains(item))
                    {
                        return "0.25 0.8 0.15 0.2";
                    }
                }
            }
            return "0.7 0.3 0.2 0.2";
        }

        private string GetSelectedGrade(BasePlayer player, int id)
        {
            if (playerSettings[player.userID].CurrentGrade == id)
                return "0.8 0.9 0.2 0.25";

            return "0.8 0.9 0.2 0.08";
        }

        private string GetButtonColor(bool a1, bool a2)
        {
            return a1 == a2 ? "0.8 0.9 0.2 1" : "0.74 0.19 0.13 0.8";
        }

        private string GetButtonTextColor(BasePlayer player, string current)
        {
            if (current == playerSettings[player.userID].CurrentMenu)
                return "0.8 0.9 0.2 1";

            return "0.4 0.6 0.2 1";
        }

        void ImportImages()
        {
            Dictionary<string, string> imageList = new Dictionary<string, string>();
            foreach (var imgData in _config.imgdata)
            {
                imageList.Add(imgData.name, imgData.img);
            }

            ImageLibrary?.Call("ImportImageList", Title, imageList, 0UL, true, null);
        }

        private string GetImage(string name){ 
            return ImageLibrary?.Call<string>("GetImage", name); 
        }

        private string GetTxt(bool dt)
        {
            return dt ? "Visible" : "Invisible";
        }

        private List<BaseShareInfo> GetSavedBasesForPlayer(BasePlayer player)
        {
            var playerId = player.userID;
            List<BaseShareInfo> savedBases = new List<BaseShareInfo>();

            string defaultImageUrl = "https://i.imgur.com/0QtCHOh.png";

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT BaseName, baseImageUrl FROM SavedBases WHERE PlayerId = @PlayerId";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlayerId", playerId.ToString());

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string baseName = reader.GetString(reader.GetOrdinal("BaseName"));
                                
                                string baseImgUrl = !reader.IsDBNull(reader.GetOrdinal("baseImageUrl")) 
                                                    ? reader.GetString(reader.GetOrdinal("baseImageUrl")) 
                                                    : defaultImageUrl;
                                
                                string localImgUrl = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerId}/{baseName}.png");

                                savedBases.Add(new BaseShareInfo
                                {
                                    BaseName = baseName,
                                    ImageUrl = localImgUrl,
                                    baseImageUrl = baseImgUrl
                                });
                            }
                        }
                    }
                }

            return savedBases;
        }

        private List<BaseShareInfo> GetAllBaseInfoForPlayer(string steamId)
        {
            var baseShareInfos = new List<BaseShareInfo>();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT * FROM BaseShare";

                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@SteamId", steamId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            baseShareInfos.Add(new BaseShareInfo
                            {
                                CreatorName = reader["CreatorName"].ToString(),
                                SteamId = reader["SteamId"].ToString(),
                                BaseName = reader["BaseName"].ToString(),
                                ShareCode = reader["ShareCode"].ToString(),
                                Likes = reader.GetInt32(reader.GetOrdinal("Likes")),
                                Dislikes = reader.GetInt32(reader.GetOrdinal("Dislikes")),
                                Downloads = reader.GetInt32(reader.GetOrdinal("Downloads")),
                                ImageUrl = reader["ImageUrl"].ToString()
                            });
                        }
                    }
                }
            }

            return baseShareInfos;
        }

        private string GetButtonTextColor2(BasePlayer player, string current)
        {
            if (current == playerSettings[player.userID].CommunityCurrentMenu)
                return "0.8 0.9 0.2 1";

            return "0.4 0.6 0.2 1";
        }

        private void uiCloseAll(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "MainMenu_Panel");
            CuiHelper.DestroyUi(player, "Build_Panel");
            CuiHelper.DestroyUi(player, "Networking");
            CuiHelper.DestroyUi(player, "Plot_Panel");
            CuiHelper.DestroyUi(player, "Personal_Panel");
            CuiHelper.DestroyUi(player, "Entity_Panel");
            CuiHelper.DestroyUi(player, "AutoDoors_Panel");
            CuiHelper.DestroyUi(player, "AutoWindows_Panel");
            CuiHelper.DestroyUi(player, "AutoElectricity_Panel");
            CuiHelper.DestroyUi(player, "BuildingUpgrade_Panel");
            CuiHelper.DestroyUi(player, "BaseGradeUpdate_Panel");
            CuiHelper.DestroyUi(player, "VehicleManager_Panel");
            CuiHelper.DestroyUi(player, "Community_Panel");
            CuiHelper.DestroyUi(player, "WeatherPanel");
            CuiHelper.DestroyUi(player, "LoadingText");
            CuiHelper.DestroyUi(player, "Info_Panel");
        }

        bool IsTeamLeader(BasePlayer player)
        {
            var team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            bool isTeamLeader = team != null && team.teamLeader == player.userID;

            return team != null && !isTeamLeader;
        }

        private void uiMenuMain(BasePlayer player, string curr_menu = "build")
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            var container = new CuiElementContainer();
            string plot_status = "";

            if (IsTeamLeader(player))
                plot_status = "DISABLED BUTTON";
            else
                plot_status = playerSettings[player.userID].PlayerOwnZone ? GetTranslation("Menu_Top_UnClaim", player) : GetTranslation("Menu_Top_Claim", player);

            switch(curr_menu)
            {
                case "build":
                    Add_CuiPanel(container, true, "0.2 0.2 0.2 0.90", "0 0", "1 1", "-0.005 0", "-0.005 -32.476", "Overall", "Build_Panel");
                    Add_CuiPanel(container, true, "0.2 0.2 0.2 1", "0 1", "1 1", "0 -44.84", "0 32.476", "Build_Panel", "MainMenu_Panel");
                    Add_CuiElement(container, "ServerName", "MainMenu_Panel", _config.menu_title, "robotocondensed-bold.ttf", 24, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-459.244 0", "459.236 38.42");
                    Add_CuiButton(container, "creative.menu build", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_Build", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, GetButtonTextColor(player, "build"), "0.5 0.5", "0.5 0.5", "-82.647 -38.416", "82.639 -5.944", "MainMenu_Panel", "Build Options");
                    if (_config.weather_menu) Add_CuiButton(container, "creative.menu weather", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_Weather", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, GetButtonTextColor(player, "weather"), "0.5 0.5", "0.5 0.5", "-270.943 -38.416", "-105.657 -5.944", "MainMenu_Panel", "Weather Options");
                    Add_CuiButton(container, "creative.menu community", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_Community", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, GetButtonTextColor(player, "community"), "0.5 0.5", "0.5 0.5", "105.647 -38.416", "270.933 -5.944", "MainMenu_Panel", "Community Options");
                    Add_CuiButton(container, $"creative.menu CLAIM", "0.3 0.4 0.1 1", $"{plot_status}", "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0.5 0.5", "0.5 0.5", "-459.243 -38.412", "-293.957 -5.939", "MainMenu_Panel", "Claim Zone");
                    Add_CuiButton(container, "creative.menu request_resources", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_RequestResources", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0.5 0.5", "0.5 0.5", "293.947 -38.412", "459.233 -5.939", "MainMenu_Panel", "Request Resources");
                    Add_CuiButton(container, "creative.menu close_menu", "0.7 0.3 0.2 1", "X", "permanentmarker.ttf", 20, TextAnchor.MiddleCenter, "0.6 0.50 0.3 1", "0.5 0.5", "0.5 0.5", "593.4 -3.6", "628.4 31.4", "MainMenu_Panel", "CloseMenu");


                    CuiHelper.DestroyUi(player, "MainMenu_Panel");
                    CuiHelper.DestroyUi(player, "Build_Panel");
                    CuiHelper.DestroyUi(player, "Networking");
                    CuiHelper.DestroyUi(player, "Plot_Panel");
                    CuiHelper.DestroyUi(player, "Personal_Panel");
                    CuiHelper.DestroyUi(player, "Entity_Panel");
                    CuiHelper.DestroyUi(player, "AutoDoors_Panel");
                    CuiHelper.DestroyUi(player, "AutoWindows_Panel");
                    CuiHelper.DestroyUi(player, "AutoElectricity_Panel");
                    CuiHelper.DestroyUi(player, "BuildingUpgrade_Panel");
                    CuiHelper.DestroyUi(player, "BaseGradeUpdate_Panel");
                    CuiHelper.DestroyUi(player, "VehicleManager_Panel");
                    CuiHelper.DestroyUi(player, "Community_Panel");
                    CuiHelper.DestroyUi(player, "LoadingText");
                    CuiHelper.AddUi(player, container);

                    uiNetworkingPanel(player);
                    uiPlotPanel(player);
                    uiPersonalPanel(player);
                    uiEntityPanel(player);
                    uiAutoDoorsPanel(player);
                    uiAutoWindowsPanel(player);
                    uiAutoElectricityPanel(player);
                    uiBuildingUpgradePanel(player);
                    uiBaseGradeUpdate(player);
                    if (permission.UserHasPermission(player.UserIDString, "creative.vehicle"))
                        uiVehicleManagerPanel(player);
                break;

                case "community":
                    Add_CuiPanel(container, true, "0.2 0.2 0.2 0.90", "0 0", "1 1", "-0.005 0", "-0.005 -32.476", "Overall", "Build_Panel");
                    Add_CuiPanel(container, true, "0.2 0.2 0.2 1", "0 1", "1 1", "0 -44.84", "0 32.476", "Build_Panel", "MainMenu_Panel");
                    Add_CuiElement(container, "ServerName", "MainMenu_Panel", _config.menu_title, "robotocondensed-bold.ttf", 24, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-459.244 0", "459.236 38.42");
                    Add_CuiButton(container, "creative.menu build", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_Build", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, GetButtonTextColor(player, "build"), "0.5 0.5", "0.5 0.5", "-82.647 -38.416", "82.639 -5.944", "MainMenu_Panel", "Build Options");
                    if (_config.weather_menu) Add_CuiButton(container, "creative.menu weather", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_Weather", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, GetButtonTextColor(player, "weather"), "0.5 0.5", "0.5 0.5", "-270.943 -38.416", "-105.657 -5.944", "MainMenu_Panel", "Weather Options");
                    Add_CuiButton(container, "creative.menu community", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_Community", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, GetButtonTextColor(player, "community"), "0.5 0.5", "0.5 0.5", "105.647 -38.416", "270.933 -5.944", "MainMenu_Panel", "Community Options");
                    Add_CuiButton(container, $"creative.menu {plot_status}", "0.3 0.4 0.1 1", $"{plot_status}", "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0.5 0.5", "0.5 0.5", "-459.243 -38.412", "-293.957 -5.939", "MainMenu_Panel", "Claim Zone");
                    Add_CuiButton(container, "creative.menu request_resources", "0.3 0.4 0.1 1", GetTranslation("Menu_Top_RequestResources", player), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0.5 0.5", "0.5 0.5", "293.947 -38.412", "459.233 -5.939", "MainMenu_Panel", "Request Resources");
                    Add_CuiButton(container, "creative.menu close_menu", "0.7 0.3 0.2 1", "X", "permanentmarker.ttf", 20, TextAnchor.MiddleCenter, "0.6 0.50 0.3 1", "0.5 0.5", "0.5 0.5", "593.4 -3.6", "628.4 31.4", "MainMenu_Panel", "CloseMenu");
                    
                    CuiHelper.DestroyUi(player, "MainMenu_Panel");
                    CuiHelper.DestroyUi(player, "Build_Panel");
                    CuiHelper.DestroyUi(player, "Networking");
                    CuiHelper.DestroyUi(player, "Plot_Panel");
                    CuiHelper.DestroyUi(player, "Personal_Panel");
                    CuiHelper.DestroyUi(player, "Entity_Panel");
                    CuiHelper.DestroyUi(player, "AutoDoors_Panel");
                    CuiHelper.DestroyUi(player, "AutoWindows_Panel");
                    CuiHelper.DestroyUi(player, "AutoElectricity_Panel");
                    CuiHelper.DestroyUi(player, "BuildingUpgrade_Panel");
                    CuiHelper.DestroyUi(player, "BaseGradeUpdate_Panel");
                    CuiHelper.DestroyUi(player, "VehicleManager_Panel");
                    CuiHelper.DestroyUi(player, "Community_Panel");
                    CuiHelper.DestroyUi(player, "Community_Panel");
                    CuiHelper.DestroyUi(player, "LoadingText");
                    CuiHelper.AddUi(player, container);

                    uiCommunityMenu(player, playerSettings[player.userID].CurrentPage);
                break;
            }
        }

        private void uiWeatherMenu(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use") || !_config.weather_menu)
                return;

            var container = new CuiElementContainer();
        
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0.15 0.15 0.1 1" },
                RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175.535 -340.376", OffsetMax = "185.355 307.9" }
            },"Overall","WeatherPanel");

            container.Add(new CuiElement
            {
                Name = "WeatherTitleBackground",
                Parent = "WeatherPanel",
                Components = {
                                new CuiRawImageComponent { Color = "0.3905749 0.6320754 0.6155467 0.8352941" },
                                new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-169.565 281.221", OffsetMax = "169.565 314.059" }
                            }
            });

            Add_CuiElement(container,"WeatherTitle", "WeatherPanel", "WEATHER SETTINGS", "robotocondensed-bold.ttf", 24, TextAnchor.MiddleCenter, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.565 281.222", "169.565 314.058");

            Add_CuiButton(container, "creative.menu close_weather_menu", "0.7 0.3 0.2 1", "X", "permanentmarker.ttf", 20, TextAnchor.MiddleCenter, "0.6 0.50 0.3 1", "0.5 0.5", "0.5 0.5", "137.598 281.221", "169.562 314.059", "WeatherPanel", "CloseWeatherMenu");
            Add_CuiButton(container, "creative.menu reset_weather_menu", "0.7 0.3 0.2 1", "↻", "permanentmarker.ttf", 20, TextAnchor.MiddleCenter, "0.6 0.50 0.3 1", "0.5 0.5", "0.5 0.5", "102.598 281.221", "134.562 314.059", "WeatherPanel", "ResetWeatherMenu");

            Add_CuiElement(container, "Wind", "WeatherPanel", "Wind", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.562 233.975", "-0.002 266.625");
            Add_CuiButton(container, "creative.weather wind b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 240.384", "72.39 260.216", "WeatherPanel", "Wind-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].wind.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 240.382", "108.49 260.218", "WeatherPanel", "WindCurrent" );
            Add_CuiButton(container, "creative.weather wind a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 240.38", "144.49 260.22", "WeatherPanel", "Wind+" );

            Add_CuiElement(container,"Rain", "WeatherPanel", "Rain", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 201.325", "0 233.975");
            Add_CuiButton(container, "creative.weather rain b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 207.734", "72.39 227.566", "WeatherPanel", "Rain-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].rain.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 207.732", "108.49 227.568", "WeatherPanel", "RainCurrent" );
            Add_CuiButton(container, "creative.weather rain a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 207.729", "144.49 227.57", "WeatherPanel", "Rain+" );

            Add_CuiElement(container,"Thunder", "WeatherPanel", "Thunder", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 168.675", "0 201.325");
            Add_CuiButton(container, "creative.weather thunder b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 175.084", "72.39 194.916", "WeatherPanel", "Thunder-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].thunder.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 175.082", "108.49 194.918", "WeatherPanel", "ThunderCurrent" );
            Add_CuiButton(container, "creative.weather thunder a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 175.079", "144.49 194.921", "WeatherPanel", "Thunder+" );

            Add_CuiElement(container,"Athmosphere_Brightness", "WeatherPanel", "Atmosphere Brightness", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 136.025", "0 168.675");
            Add_CuiButton(container, "creative.weather atmosphere_brightness b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 142.434", "72.39 162.266", "WeatherPanel", "Brightness-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].atmosphere_brightness.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 142.432", "108.49 162.268", "WeatherPanel", "BrightnessCurrent" );
            Add_CuiButton(container, "creative.weather atmosphere_brightness a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 142.43", "144.49 162.271", "WeatherPanel", "Brightness+" );

            Add_CuiElement(container,"Athmosphere_Rayleigh", "WeatherPanel", "Atmosphere Rayleigh", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 103.375", "0 136.025");
            Add_CuiButton(container, "creative.weather atmosphere_rayleigh b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 109.784", "72.39 129.616", "WeatherPanel", "Athmosphere_Rayleigh-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].atmosphere_rayleigh.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 109.782", "108.49 129.618", "WeatherPanel", "Athmosphere_RayleighCurrent" );
            Add_CuiButton(container, "creative.weather atmosphere_rayleigh a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 109.779", "144.49 129.62", "WeatherPanel", "Athmosphere_Rayleigh+" );

            Add_CuiElement(container,"MieMultiplier", "WeatherPanel", "Atmosphere Mie", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.565 70.726", "-0.005 103.376");
            Add_CuiButton(container, "creative.weather atmosphere_mie b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 77.135", "72.39 96.966", "WeatherPanel", "MieMultiplier-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].atmosphere_mie.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 77.135", "108.49 96.971", "WeatherPanel", "MieMultiplierCurrent" );
            Add_CuiButton(container, "creative.weather atmosphere_mie a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 77.135", "144.49 96.977", "WeatherPanel", "MieMultiplier+" );

            Add_CuiElement(container,"Atmosphere_Contrast", "WeatherPanel", "Atmosphere Contrast", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 38.077", "0 70.727");
            Add_CuiButton(container, "creative.weather atmosphere_contrast b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 44.486", "72.39 64.318", "WeatherPanel", "Atmosphere_Contrast-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].atmosphere_contrast.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 44.482", "108.49 64.318", "WeatherPanel", "Atmosphere_ContrastCurrent" );
            Add_CuiButton(container, "creative.weather atmosphere_contrast a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 44.477", "144.49 64.318", "WeatherPanel", "Atmosphere_Contrast+" );

            Add_CuiElement(container,"Atmosphere_Directionality", "WeatherPanel", "Atmosphere Directionality", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 5.428", "0 38.078");
            Add_CuiButton(container, "creative.weather atmosphere_directionality b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 11.837", "72.39 31.669", "WeatherPanel", "Atmosphere_Directionality-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].atmosphere_directionality.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 11.833", "108.49 31.669", "WeatherPanel", "Atmosphere_DirectionalityCurrent" );
            Add_CuiButton(container, "creative.weather atmosphere_directionality a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 11.827", "144.49 31.668", "WeatherPanel", "Atmosphere_Directionality+" );

            Add_CuiElement(container,"Fog", "WeatherPanel", "Fog", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.565 -27.221", "-0.005 5.429");
            Add_CuiButton(container, "creative.weather fog b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -20.812", "72.39 -0.98", "WeatherPanel", "Fog-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].fog.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -20.816", "108.49 -0.98", "WeatherPanel", "FogCurrent" );
            Add_CuiButton(container, "creative.weather fog a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -20.812", "144.49 -0.97", "WeatherPanel", "Fog+" );

            Add_CuiElement(container,"CloudSize", "WeatherPanel", "Cloud Size", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 -59.87", "0 -27.22");
            Add_CuiButton(container, "creative.weather cloud_size b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -53.461", "72.39 -33.629", "WeatherPanel", "CloudSize-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_size.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -53.465", "108.49 -33.629", "WeatherPanel", "CloudSizeCurrent" );
            Add_CuiButton(container, "creative.weather cloud_size a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -53.461", "144.49 -33.62", "WeatherPanel", "CloudSize+" );

            Add_CuiElement(container,"CloudOpacity", "WeatherPanel", "Cloud Opacity", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 -92.519", "0 -59.869");
            Add_CuiButton(container, "creative.weather cloud_opacity b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -86.11", "72.39 -66.278", "WeatherPanel", "CloudOpacity-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_opacity.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -86.11", "108.49 -66.274", "WeatherPanel", "CloudOpacityCurrent" );
            Add_CuiButton(container, "creative.weather cloud_opacity a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -86.119", "144.49 -66.278", "WeatherPanel", "CloudOpacity+" );

            Add_CuiElement(container,"CloudCoverage", "WeatherPanel", "Cloud Coverage", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.565 -125.165", "-0.005 -92.515");
            Add_CuiButton(container, "creative.weather cloud_coverage b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -118.756", "72.39 -98.924", "WeatherPanel", "CloudCoverage-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_coverage.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -118.758", "108.49 -98.922", "WeatherPanel", "CloudCoverageCurrent" );
            Add_CuiButton(container, "creative.weather cloud_coverage a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -118.76", "144.49 -98.919", "WeatherPanel", "CloudCoverage+" );

            Add_CuiElement(container,"CloudSharpness", "WeatherPanel", "Cloud Sharpness", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.565 -157.815", "-0.005 -125.165");
            Add_CuiButton(container, "creative.weather cloud_sharpness b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -151.406", "72.39 -131.574", "WeatherPanel", "CloudSharpness-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_sharpness.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -151.408", "108.49 -131.572", "WeatherPanel", "CloudSharpnessCurrent" );
            Add_CuiButton(container, "creative.weather cloud_sharpness a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -151.411", "144.49 -131.57", "WeatherPanel", "CloudSharpness+" );

            Add_CuiElement(container,"CloudColoring", "WeatherPanel", "Cloud Color", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 -190.465", "0 -157.815");
            Add_CuiButton(container, "creative.weather cloud_coloring b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -184.056", "72.39 -164.224", "WeatherPanel", "CloudColoring-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_coloring.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -184.058", "108.49 -164.222", "WeatherPanel", "CloudColoringCurrent" );
            Add_CuiButton(container, "creative.weather cloud_coloring a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -184.061", "144.49 -164.219", "WeatherPanel", "CloudColoring+" );

            Add_CuiElement(container,"CloudAttenuation", "WeatherPanel", "Cloud Attenuation", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.565 -223.115", "-0.005 -190.465");
            Add_CuiButton(container, "creative.weather cloud_attenuation b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -216.706", "72.39 -196.874", "WeatherPanel", "CloudAttenuation-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_attenuation.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -216.708", "108.49 -196.872", "WeatherPanel", "CloudAttenuationCurrentt" );
            Add_CuiButton(container, "creative.weather cloud_attenuation a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -216.71", "144.49 -196.869", "WeatherPanel", "CloudAttenuation+" );

            Add_CuiElement(container,"CloudSaturation", "WeatherPanel", "Cloud Satuarion", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 -255.765", "0 -223.115");
            Add_CuiButton(container, "creative.weather cloud_saturation b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -249.356", "72.39 -229.524", "WeatherPanel", "CloudSaturation-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_saturation.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -249.358", "108.49 -229.522", "WeatherPanel", "CloudSaturationCurrent" );
            Add_CuiButton(container, "creative.weather cloud_saturation a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -249.361", "144.49 -229.52", "WeatherPanel", "CloudSaturation+" );

            Add_CuiElement(container,"CloudScattering", "WeatherPanel", "Cloud Scattering", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 -288.415", "0 -255.765");
            Add_CuiButton(container, "creative.weather cloud_scattering b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.61 -282.006", "72.39 -262.174", "WeatherPanel", "CloudScattering-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].cloud_scattering.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.71 -282.008", "108.49 -262.172", "WeatherPanel", "CloudScatteringCurrent" );
            Add_CuiButton(container, "creative.weather cloud_scattering a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -282.01", "144.49 -262.169", "WeatherPanel", "CloudScattering+" );

            Add_CuiElement(container,"CurrentTime", "WeatherPanel", "Current Time", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "1 1 1 1", "0 0 0 0", "0 0", "0.5 0.5", "0.5 0.5", "-169.56 -321.065", "0 -288.415");
            Add_CuiButton(container, "creative.weather time b", "0.7 0.3 0.2 1", "-", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "38.131 -314.656", "71.911 -294.824", "WeatherPanel", "CloudScattering-" );
            Add_CuiButton(container, "", "0.7 0.3 0.2 1", playerWeather[player.userID].current_time.ToString(), "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "74.132 -314.658", "107.911 -294.822", "WeatherPanel", "CloudScatteringCurrent" );
            Add_CuiButton(container, "creative.weather time a", "0.7 0.3 0.2 1", "+", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "110.71 -314.66", "144.49 -294.819", "WeatherPanel", "CloudScattering+" );

            CuiHelper.DestroyUi(player, "WeatherPanel");
            CuiHelper.AddUi(player, container);
        }

        private async Task uiCommunityMenu(BasePlayer player, int page = 0)
        {
            if (!playerSettings.ContainsKey(player.userID))
                return;

            try
            {
                var loadingContainer = new CuiElementContainer();
                loadingContainer.Add(new CuiLabel
                {
                    Text = { Text = "Loading...", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -25", OffsetMax = "50 25" }
                }, "Overall", "LoadingText");

                CuiHelper.AddUi(player, loadingContainer);

                await Task.Run(async () =>
                {
                    List<BaseShareInfo> baseList;
                    int basesPerPage = 21;
                    int totalPages;

                    if (playerSettings[player.userID].CommunityCurrentMenu == "saved")
                    {
                        baseList = GetSavedBasesForPlayer(player);
                        totalPages = (int)Math.Ceiling((double)baseList.Count / basesPerPage);
                    }
                    else
                    {
                        baseList = GetAllBaseInfoForPlayer(player.userID.ToString());
                        totalPages = (int)Math.Ceiling((double)baseList.Count / basesPerPage);
                    }

                    var paginatedBaseList = baseList.Skip(page * basesPerPage).Take(basesPerPage).ToList();

                    var container = new CuiElementContainer();

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.2 0.2 0.2 0.90" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.005 0", OffsetMax = "-0.005 -76.84" }
                    }, "Overall", "Community_Panel");

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0 0 0 0.3254902" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 271.671", OffsetMax = "640 320.569" }
                    }, "Community_Panel", "TopCommunityPanel");

                    Add_CuiButton(container, "creative.changecommmenu community", "0.3 0.4 0.1 1", "COMMUNITY BASES", "robotocondensed-bold.ttf", 11, (TextAnchor)4, GetButtonTextColor2(player, "community"), "0.5 0.5", "0.5 0.5", "12.142 -13.456", "183.058 13.456", "TopCommunityPanel", "Community Bases");
                    Add_CuiButton(container, "creative.changecommmenu saved", "0.3 0.4 0.1 1", "SAVED BUILDINGS", "robotocondensed-bold.ttf", 11, (TextAnchor)4, GetButtonTextColor2(player, "saved"), "0.5 0.5", "0.5 0.5", "-182.158 -13.456", "-11.242 13.456", "TopCommunityPanel", "MyBases");

                    if (playerSettings[player.userID].CommunityCurrentMenu == "saved")
                    {
                        container.Add(new CuiPanel
                        {
                            CursorEnabled = false,
                            Image = { Color = "0 0 0 0.3254902" },
                            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -321.58", OffsetMax = "640 271.67" }
                        }, "Community_Panel", "BaseList");

                        foreach (var baseInfo in paginatedBaseList.Select((value, index) => new { value, index }))
                        {
                            int i = baseInfo.index;
                            var info = baseInfo.value;

                            int row = i / 7;
                            int column = i % 7;

                            float xOffsetMin = -611.2f + (column * 175f);
                            float yOffsetMin = 261.544f - (row * 175f);


                            Add_CuiButton(container, "", "0.3 0.4 0.1 1", info.BaseName, "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin}", $"{xOffsetMin + 137.8f} {yOffsetMin + 26f}", "BaseList", $"Base{i + 1}_Name");
                            Add_CuiElementImage_Url(container, $"Base{i + 1}_Image", "BaseList", "0 0 0 0.5", "1 -1", info.baseImageUrl, "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin - 93f}", $"{xOffsetMin + 137.8f} {yOffsetMin - 19f}");
                            Add_CuiButton(container, $"load {info.BaseName}", "0.3 0.4 0.1 1", "Load", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin - 128f}", $"{xOffsetMin + 58.948f} {yOffsetMin - 106f}", "BaseList", $"Base{i + 1}_Download");

                            float trashXOffsetMin = xOffsetMin + 137.8f - 26f;
                            float trashYOffsetMin = yOffsetMin;

                            float imageSize = 30f;

                            Add_CuiElementImage(container, $"Base{i + 1}_Trash_Image", "BaseList", "0 0 0 0.5", "1 -1", GetImage("trashcan"), "0.5 0.5", "0.5 0.5", $"{trashXOffsetMin - 5f} {trashYOffsetMin - 5f}", $"{trashXOffsetMin + imageSize} {trashYOffsetMin + imageSize}");
                            Add_CuiButton(container, $"creative.deletebase {info.BaseName}", "0.4 0.1 0.1 0.4", "", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{trashXOffsetMin} {trashYOffsetMin}", $"{trashXOffsetMin + 26f} {trashYOffsetMin + 26f}", "BaseList", $"Base{i + 1}_Trash");
                            
                            bool baseExists = await CheckBaseExistsInDb(info.BaseName, player.UserIDString);

                            if (baseExists)
                            {
                                Add_CuiButton(container, $"getcode {info.BaseName}", "0.3 0.4 0.1 1", "Get Code", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin + 88f} {yOffsetMin - 128f}", $"{xOffsetMin + 138f} {yOffsetMin - 106f}", "BaseList", $"Base{i + 1}_GetCode");
                            }
                            else
                            {
                                Add_CuiButton(container, $"share {info.BaseName}", "0.3 0.4 0.1 1", "Share", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin + 88f} {yOffsetMin - 128f}", $"{xOffsetMin + 138f} {yOffsetMin - 106f}", "BaseList", $"Base{i + 1}_ShareBase");
                            }
                        }
                    }
                    else
                    {
                        container.Add(new CuiPanel
                        {
                            CursorEnabled = false,
                            Image = { Color = "0 0 0 0.3254902" },
                            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -321.58", OffsetMax = "640 271.67" }
                        }, "Community_Panel", "BaseList");

                        for (int i = 0; i < paginatedBaseList.Count; i++)
                        {
                            var baseInfo = paginatedBaseList[i];
                            int row = i / 7;
                            int column = i % 7;

                            float xOffsetMin = -611.2f + (column * 175f);
                            float yOffsetMin = 261.544f - (row * 175f);

                            float trashXOffsetMin = xOffsetMin + 137.8f - 26f;
                            float trashYOffsetMin = yOffsetMin;
                            
                            Add_CuiButton(container, "", "0.3 0.4 0.1 1", baseInfo.BaseName, "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin}", $"{xOffsetMin + 137.8f} {yOffsetMin + 26f}", "BaseList", $"Base{i + 1}_Name");
                            Add_CuiElementImage_Url(container, $"Base{i + 1}_Image", "BaseList", "0 0 0 0.5", "1 -1", baseInfo.ImageUrl, "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin - 93f}", $"{xOffsetMin + 137.8f} {yOffsetMin - 19f}");
                            Add_CuiElement(container, $"Base{i + 1}_Information", "BaseList", $"Creator: {baseInfo.CreatorName}\nUpvotes: {baseInfo.Likes}\nDownvotes: {baseInfo.Dislikes}\nDownloads: {baseInfo.Downloads}", "robotocondensed-regular.ttf", 14, (TextAnchor)0, "0 0 0 1", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin - 93f}", $"{xOffsetMin + 137.8f} {yOffsetMin - 19f}");

                            Add_CuiElementImage(container, $"Base{i + 1}_Upvote_Image", "BaseList", "0 0 0 0.5", "1 -1", GetImage("UpVote"), "0.5 0.5", "0.5 0.5", $"{xOffsetMin + 73.691f} {yOffsetMin - 123f}", $"{xOffsetMin + 90.543f} {yOffsetMin - 105f}");
                            Add_CuiElementImage(container, $"Base{i + 1}_Downvote_Image", "BaseList", "0 0 0 0.5", "1 -1", GetImage("DownVote"), "0.5 0.5", "0.5 0.5", $"{xOffsetMin + 111.671f} {yOffsetMin - 123f}", $"{xOffsetMin + 128.523f} {yOffsetMin - 105f}");

                            Add_CuiButton(container, $"load_code {baseInfo.ShareCode}", "0.3 0.4 0.1 1", "Load", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin} {yOffsetMin - 128f}", $"{xOffsetMin + 58.948f} {yOffsetMin - 106f}", "BaseList", $"Base{i + 1}_Download");
                            Add_CuiButton(container, $"vote {baseInfo.ShareCode} upvote", "0.3 0.4 0.1 0.1", "", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin + 64.384f} {yOffsetMin - 128f}", $"{xOffsetMin + 99.824f} {yOffsetMin - 106f}", "BaseList", $"Base{i + 1}_UpVote");
                            Add_CuiButton(container, $"vote {baseInfo.ShareCode} downvote", "0.3 0.4 0.1 0.1", "", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{xOffsetMin + 102.365f} {yOffsetMin - 128f}", $"{xOffsetMin + 137.804f} {yOffsetMin - 106f}", "BaseList", $"Base{i + 1}_DownVote");
                        
                            if (permission.UserHasPermission(player.UserIDString, "creative.reviewer"))
                            {
                                Add_CuiElementImage(container, $"Base{i + 1}_Trash_Image", "BaseList", "0 0 0 0.5", "1 -1", GetImage("trashcan"), "0.5 0.5", "0.5 0.5", $"{trashXOffsetMin - 5f} {trashYOffsetMin - 5f}", $"{trashXOffsetMin + 30f} {trashYOffsetMin + 30f}");
                                Add_CuiButton(container, $"creative.initiatereview {baseInfo.BaseName} {baseInfo.SteamId} {baseInfo.ImageUrl}", "0.4 0.1 0.1 0.4", "review", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{trashXOffsetMin} {trashYOffsetMin}", $"{trashXOffsetMin + 26f} {trashYOffsetMin + 26f}", "BaseList", $"Base{i + 1}_Trash");
                            }
                            else if (player.userID.ToString() == baseInfo.SteamId)
                            {
                                Add_CuiElementImage(container, $"Base{i + 1}_Trash_Image", "BaseList", "0 0 0 0.5", "1 -1", GetImage("trashcan"), "0.5 0.5", "0.5 0.5", $"{trashXOffsetMin - 5f} {trashYOffsetMin - 5f}", $"{trashXOffsetMin + 30f} {trashYOffsetMin + 30f}");
                                Add_CuiButton(container, $"creative.unshare {baseInfo.BaseName} {baseInfo.SteamId}", "0.4 0.1 0.1 0.4", "unshare", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", $"{trashXOffsetMin} {trashYOffsetMin}", $"{trashXOffsetMin + 26f} {trashYOffsetMin + 26f}", "BaseList", $"Base{i + 1}_Trash");
                            }
                        }
                    }

                    Add_CuiButton(container, "", "0.3 0.4 0.1 1",$"{page} / {totalPages - 1}", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "-29.474 -310.563", "29.474 -288.147", "Community_Panel", "List_Current");

                    if (page > 0)
                    {
                        Add_CuiButton(container, $"creative.communitymenu {page - 1}", "0.3 0.4 0.1 1", "<<", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "-73.02 -310.563", "-37.58 -288.147", "Community_Panel", "List_Previous");
                    
                    }

                    if (page < totalPages - 1)
                    {
                        Add_CuiButton(container, $"creative.communitymenu {page + 1}", "0.3 0.4 0.1 1", ">>", "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "39.481 -310.566", "74.919 -288.144", "Community_Panel", "List_Next");
                    }

                    CuiHelper.DestroyUi(player, "Community_Panel");
                    CuiHelper.AddUi(player, container);
                });

                CuiHelper.DestroyUi(player, "LoadingText");
            }
            catch (Exception ex) { LogErrors(ex.Message, "uiCommunityMenu"); }
        }

        private void Open_InfoMenu(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.2 0.2 0.2 0.90", "0 0", "1 1", "-0.005 0", "-0.005 -32.476", "Overall", "Info_Panel");
            Add_CuiPanel(container, true, "0.2 0.2 0.2 1", "0 1", "1 1", "0 -44.84", "0 32.476", "Info_Panel", "MainMenu_Panel");
            Add_CuiElement(container, "ServerName", "MainMenu_Panel", _config.menu_title, "robotocondensed-bold.ttf", 24, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-459.244 0", "459.236 38.42");
                    
            Add_CuiButton(container, "creative.menu close_menu", "1 1 1 1", "X", "permanentmarker.ttf", 14, (TextAnchor)4, "0 0 0 1", "0.5 0.5", "0.5 0.5", "593.4 -3.6", "628.4 31.4", "MainMenu_Panel", "CloseMenu");
            Add_CuiElement(container, "Left_Top_Label", "Info_Panel", _config.infopanel_left_top, "robotocondensed-regular.ttf", 14, (TextAnchor)0, "0 0 0 0.5", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", "-608 23.691", "-25.262 298.92");
            Add_CuiElement(container, "Right_Top_Label", "Info_Panel", _config.infopanel_right_top, "robotocondensed-regular.ttf", 14, (TextAnchor)0, "0 0 0 0.5", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", "25.261 23.696", "607.999 298.924");
            Add_CuiElement(container, "Left_Down_Label", "Info_Panel", _config.infopanel_left_down, "robotocondensed-regular.ttf", 14, (TextAnchor)0, "0 0 0 0.5", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", "-607.999 -298.924", "-25.261 -23.696");
            Add_CuiElement(container, "Right_Down_Label", "Info_Panel", _config.infopanel_right_down, "robotocondensed-regular.ttf", 14, (TextAnchor)0, "0 0 0 0.5", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", "25.261 -298.924", "607.999 -23.696");

            CuiHelper.DestroyUi(player, "Info_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void CreateMainPanel(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            var container = new CuiElementContainer();

            Add_CuiPanel(container, false, "0.15 0.15 0.1 1", "1 0", "1 0", "-454.646 18.561", "-248.455 77.836", "Hud", "BGradeHudPanel");

            CuiHelper.DestroyUi(player, "BGradeHudPanel");
            CuiHelper.AddUi(player, container);
        }

        private void uiBgradeHud(BasePlayer player, bool update = false)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use") || !playerSettings.ContainsKey(player.userID))
                return;

            var container = new CuiElementContainer();

            if (!playerSettings[player.userID].BGradeHud)
            {
                playerSettings[player.userID].BGradeBackground = true;
                CuiHelper.DestroyUi(player, "BGradeHudPanel");
                CuiHelper.DestroyUi(player, "BGradeHudPanel2");
                return;
            }

            if (playerSettings[player.userID].BGradeBackground)
            {
                CreateMainPanel(player);
                playerSettings[player.userID].BGradeBackground = false;
            }
            
            Add_CuiPanel(container, false, "0.15 0.15 0.1 0", "1 0", "1 0", "-454.646 18.561", "-248.455 77.836", "Hud", "BGradeHudPanel2");
            Add_CuiElementImage(container, "BuildGrade_Twigs", "BGradeHudPanel2", "0 0 0 1", "1 -1", GetImage("twigs"), "0.5 0.5", "0.5 0.5", "-97.485 -18.128", "-63.715 18.132");
            Add_CuiElementImage(container, "BuildGrade_Wood", "BGradeHudPanel2", "0 0 0 1", "1 -1", GetImage("wood"), "0.5 0.5", "0.5 0.5", "-58.185 -18.128", "-24.415 18.132");
            Add_CuiElementImage(container, "BuildGrade_Stone", "BGradeHudPanel2", "0 0 0 1", "1 -1", GetImage("stones"), "0.5 0.5", "0.5 0.5", "-16.885 -18.13", "16.885 18.13");
            Add_CuiElementImage(container, "BuildGrade_Metal", "BGradeHudPanel2", "0 0 0 1", "1 -1", GetImage("metal fragments"), "0.5 0.5", "0.5 0.5", "24.215 -18.13", "57.985 18.13");
            Add_CuiElementImage(container, "BuildGrade_Hqm", "BGradeHudPanel2", "0 0 0 1", "1 -1", GetImage("high quality metal"), "0.5 0.5", "0.5 0.5", "63.215 -18.13", "96.985 18.13");
            Add_CuiButton(container, "creative.bgrade twigs", GetSelectedGrade(player, 0), "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-97.485 -18.128", "-63.715 18.132", "BGradeHudPanel2", "Twigs_Button" );
            Add_CuiButton(container, "creative.bgrade wood", GetSelectedGrade(player, 1), "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-58.185 -18.128","-24.415 18.132", "BGradeHudPanel2", "Wood_Button" );
            Add_CuiButton(container, "creative.bgrade stone", GetSelectedGrade(player, 2), "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-16.885 -18.13","16.885 18.13", "BGradeHudPanel2", "Stone_Button" );
            Add_CuiButton(container, "creative.bgrade metal", GetSelectedGrade(player, 3), "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "24.215 -18.13","57.985 18.13", "BGradeHudPanel2", "Metal_Button" );
            Add_CuiButton(container, "creative.bgrade hqm", GetSelectedGrade(player, 4), "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "63.215 -18.13","96.985 18.13", "BGradeHudPanel2", "HQM_Button" );

            CuiHelper.DestroyUi(player, "BGradeHudPanel2");
            CuiHelper.AddUi(player, container);
        }

        private void uiNetworkingPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            Add_CuiPanel(container, false, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "138.81 -343.761", "370.81 -132.019", "Build_Panel", "Networking");
            Add_CuiPanel(container, false, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-116.01 -100.776", "115.99 -77.84", "Networking", "Networking_Background_Title");
            Add_CuiElement(container, "Networking_Label", "Networking", GetTranslation("Menu_Networking", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116.01 -105.633", "115.99 -72.983");

            Add_CuiPanel(container, false, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.986 78.092", "116.004 101.028", "Networking", "Networking_Foundations_Background");
            Add_CuiPanel(container, false, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.985 45.429", "116.005 68.365", "Networking", "Networking_Floors_Background");
            Add_CuiPanel(container, false, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.985 12.78", "116.005 35.716", "Networking", "Networking_Walls_Background");
            Add_CuiPanel(container, false, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.985 -19.869", "116.005 3.067", "Networking", "Networking_Extra_Background");
            Add_CuiPanel(container, false, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.985 -52.518", "116.005 -29.582", "Networking", "Networking_Walls_Background");

            Add_CuiElement(container, "Networking_Foundations", "Networking", GetTranslation("Menu_Networking_Foundations", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.986 73.221", "116.005 105.871");
            Add_CuiElement(container, "Networking_Floors", "Networking", GetTranslation("Menu_Networking_Floor", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.986 40.572", "116.005 73.222");
            Add_CuiElement(container, "Networking_Walls", "Networking", GetTranslation("Menu_Networking_Walls", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.986 7.923", "116.005 40.573");
            Add_CuiElement(container, "Networking_Extra", "Networking", GetTranslation("Menu_Networking_OBB", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.986 -24.726", "116.005 7.923");
            Add_CuiElement(container, "Networking_Deployables", "Networking", GetTranslation("Menu_Networking_Deployables", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.986 -57.375", "116.005 -24.725");

            Add_CuiButton(container, "creative.networking foundations", GetButtonColor(playerSettings[player.userID].NetworkingFoundations, true), GetTxt(playerSettings[player.userID].NetworkingFoundations), "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "57.021 81.617", "112.179 97.503", "Networking", "Networking_Foundation_Btn");
            Add_CuiButton(container, "creative.networking floor", GetButtonColor(playerSettings[player.userID].NetworkingFloor, true), GetTxt(playerSettings[player.userID].NetworkingFloor), "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "57.021 48.954", "112.179 64.84", "Networking", "Networking_Floor_Btn");
            Add_CuiButton(container, "creative.networking walls", GetButtonColor(playerSettings[player.userID].NetworkingWalls, true), GetTxt(playerSettings[player.userID].NetworkingWalls), "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "57.021 16.305", "112.179 32.191", "Networking", "Networking_Walls_Btn");
            Add_CuiButton(container, "creative.networking others", GetButtonColor(playerSettings[player.userID].NetworkingOthers, true), GetTxt(playerSettings[player.userID].NetworkingOthers), "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "57.021 -16.344", "112.179 -0.458", "Networking", "Networking_Other_Btn");
            Add_CuiButton(container, "creative.networking deployables", GetButtonColor(playerSettings[player.userID].NetworkingDeployables, true), GetTxt(playerSettings[player.userID].NetworkingDeployables), "robotocondensed-bold.ttf", 11, (TextAnchor)4, "1 1 1 1", "0.5 0.5", "0.5 0.5", "57.021 -48.993", "112.179 -33.107", "Networking", "Networking_Deployables_Btn");

            CuiHelper.DestroyUi(player, "Networking");
            CuiHelper.AddUi(player, container);
        }

        private void uiPlotPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-116.004 -343.761", "115.996 -132.019", "Build_Panel", "Plot_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -101.018", "116 -78.082", "Plot_Panel", "Background_1");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 78.078", "116 101.014", "Plot_Panel", "Background_2");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 45.429", "116 68.365", "Plot_Panel", "Background_3");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-116 12.78", "115.99 35.716", "Plot_Panel", "Background_4");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -19.869", "116 3.067", "Plot_Panel", "Background_5");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -53.518", "116 -30.582", "Plot_Panel", "Background_6");

            Add_CuiElement(container, "PlotSettings_Label", "Plot_Panel", GetTranslation("Menu_PlotSettings", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.991 -105.875", "116 -73.225");
            Add_CuiElement(container, "GTFO Mode", "Plot_Panel", GetTranslation("Menu_PlotSettings_GTFO", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 73.221", "115.991 105.871");
            Add_CuiButton(container, "creative.menu gtfo_off", GetButtonColor(playerSettings[player.userID].GTFO, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 81.603", "87.111 97.489", "Plot_Panel", "GTFO_Button_Off");
            Add_CuiButton(container, "creative.menu gtfo_on", GetButtonColor(playerSettings[player.userID].GTFO, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 81.603", "114.69 97.489", "Plot_Panel", "GTFO_Button_On");

            Add_CuiElement(container, "RAID Mode", "Plot_Panel", GetTranslation("Menu_PlotSettings_RAID", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 40.572", "115.991 73.222");
            Add_CuiButton(container, "creative.menu raid_off", GetButtonColor(playerSettings[player.userID].Raid, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 48.954", "87.111 64.84", "Plot_Panel", "RAID_Button_Off");
            Add_CuiButton(container, "creative.menu raid_on", GetButtonColor(playerSettings[player.userID].Raid, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 48.954", "114.689 64.84", "Plot_Panel", "RAID_Button_On");

           // Add_CuiElement(container, "Wipe To Foundations", "Plot_Panel", GetTranslation("Menu_PlotSettings_WTF", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 7.923", "115.991 40.573");
           // Add_CuiButton(container, "creative.menu wipe_to_foundations", "0.7 0.3 0.2 1", "WIPE!", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 16.305", "114.689 32.191", "Plot_Panel", "WipeToFoundations_Button");

           // Add_CuiElement(container, "Wipe Deployables", "Plot_Panel", GetTranslation("Menu_PlotSettings_WD", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 -24.726", "115.991 7.923");
           // Add_CuiButton(container, "creative.menu wipe_deployables", "0.7 0.3 0.2 1", "WIPE!", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -16.344", "114.689 -0.458", "Plot_Panel", "WipeDeployables_Button");

            Add_CuiElement(container, "Clear Plot", "Plot_Panel", GetTranslation("Menu_PlotSettings_CP", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 -53.518", "115.991 -30.582");
            Add_CuiButton(container, "creative.menu clear_plot", "0.7 0.3 0.2 1", "CLEAR!", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -53.458", "114.689 -37.344", "Plot_Panel", "ClearPlot_Button");

            CuiHelper.DestroyUi(player, "Plot_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiPersonalPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-367.28 -343.761", "-135.28 -132.019", "Build_Panel", "Personal_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -101.018", "116 -78.082", "Personal_Panel", "Background_1");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 78.078", "116 101.014", "Personal_Panel", "Background_2");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 45.429", "116 68.365", "Personal_Panel", "Background_3");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-116 12.78", "115.99 35.716", "Personal_Panel", "Background_4");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -19.869", "116 3.067", "Personal_Panel", "Background_5");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -52.518", "116 -29.582", "Personal_Panel", "Background_6");
            string bgrade_trans = GetTranslation("Menu_PersonalSettings_CBG", player);

            Add_CuiElement(container, "PersonalSettings_Label", "Personal_Panel", GetTranslation("Menu_PersonalSettings", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.995 -105.875", "115.995 -73.225");
            Add_CuiElement(container, "Noclip (F Key)", "Personal_Panel", GetTranslation("Menu_PersonalSettings_Noclip", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 73.221", "115.991 105.871");
            Add_CuiElement(container, "God Mode", "Personal_Panel", GetTranslation("Menu_PersonalSettings_GodMode", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 40.572", "115.991 73.222");
            Add_CuiElement(container, $"{bgrade_trans} ({_config.keybind_bgrade} Key)", "Personal_Panel", $"Change Build Grade ({_config.keybind_bgrade} Key)", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 7.923", "115.991 40.573");
            Add_CuiElement(container, "Infinite Ammo", "Personal_Panel", GetTranslation("Menu_PersonalSettings_InfAmmo", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 -24.726", "115.991 7.923");
            Add_CuiElement(container, "BuildGrade Hud", "Personal_Panel", GetTranslation("Menu_PersonalSettings_UiHud", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 -57.375", "115.991 -24.725");
            
            Add_CuiButton(container, "creative.menu fly_off", GetButtonColor(playerSettings[player.userID].Noclip, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 81.603", "87.111 97.489", "Personal_Panel", "Noclip_Button_Off");
            Add_CuiButton(container, "creative.menu fly_on", GetButtonColor(playerSettings[player.userID].Noclip, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 81.603", "114.69 97.489", "Personal_Panel", "Noclip_Button_On");

            Add_CuiButton(container, "creative.menu godmode_off", GetButtonColor(playerSettings[player.userID].GodMode, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 48.954", "87.111 64.84", "Personal_Panel", "GodMode_Button_Off");
            Add_CuiButton(container, "creative.menu godmode_on", GetButtonColor(playerSettings[player.userID].GodMode, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 48.954", "114.69 64.84", "Personal_Panel", "GodMode_Button_On");

            Add_CuiButton(container, "creative.menu changebgrade_off", GetButtonColor(playerSettings[player.userID].ChangeBGrade, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 16.305", "87.111 32.191", "Personal_Panel", "ChangeBuildGrade_Button_Off");
            Add_CuiButton(container, "creative.menu changebgrade_on", GetButtonColor(playerSettings[player.userID].ChangeBGrade, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 16.305", "114.69 32.191", "Personal_Panel", "ChangeBuildGrade_Button_On");
            
            Add_CuiButton(container, "creative.menu infammo_off", GetButtonColor(playerSettings[player.userID].InfiniteAmmo, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -15.886", "87.111 0", "Personal_Panel", "InfiniteAmmo_Button_Off");
            Add_CuiButton(container, "creative.menu infammo_on", GetButtonColor(playerSettings[player.userID].InfiniteAmmo, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 -16.344", "114.69 -0.458", "Personal_Panel", "InfiniteAmmo_Button_On");

            Add_CuiButton(container, "creative.menu bgradehud_off", GetButtonColor(playerSettings[player.userID].BGradeHud, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -48.993", "87.111 -33.107", "Personal_Panel", "BuildGradeHud_Button_Off");
            Add_CuiButton(container, "creative.menu bgradehud_on", GetButtonColor(playerSettings[player.userID].BGradeHud, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 -48.993", "114.69 -33.107", "Personal_Panel", "BuildGradeHud_Button_On");

            CuiHelper.DestroyUi(player, "Personal_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiEntityPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-618.56 -343.751", "-386.56 -132.009", "Build_Panel", "Entity_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -101.019", "116 -78.083", "Entity_Panel", "EntityPanel_Settings");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-116 78.078", "115.99 101.014", "Entity_Panel", "EntityPanel_EntTurnOn");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-116 45.429", "115.99 68.365", "Entity_Panel", "EntityPanel_FillBatteries");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 12.78", "116 35.716", "Entity_Panel", "EntityPanel_Stability");
            
            Add_CuiElement(container, "EntitySettings_Label", "Entity_Panel", GetTranslation("Menu_EntitySettings", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.995 -105.876", "115.995 -73.226");
            Add_CuiElement(container, "Entity_Automatic_Ent_Enable", "Entity_Panel", GetTranslation("Menu_EntitySettings_Furnaces", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 73.221", "115.991 105.871");
            Add_CuiElement(container, "Fill_Batteries", "Entity_Panel", GetTranslation("Menu_EntitySettings_Batteries", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 40.572", "115.991 73.222");
            Add_CuiElement(container, "Entity_Stability", "Entity_Panel", GetTranslation("Menu_EntitySettings_Stability", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.991 7.923", "116 40.573");
            Add_CuiButton(container, "creative.menu entity_turn_off", GetButtonColor(playerSettings[player.userID].AutoEntity, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 81.603", "87.111 97.489", "Entity_Panel", "Automatic_Ent_Enable_Button_Off");
            Add_CuiButton(container, "creative.menu entity_turn_on", GetButtonColor(playerSettings[player.userID].AutoEntity, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 81.603", "114.69 97.489", "Entity_Panel", "Automatic_Ent_Enable_Button_On");
            Add_CuiButton(container, "creative.menu fill_batteries_on", GetButtonColor(playerSettings[player.userID].FillBatteries, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 48.954", "114.69 64.84", "Entity_Panel", "Fill_Batteries_Button_On");
            Add_CuiButton(container, "creative.menu fill_batteries_off", GetButtonColor(playerSettings[player.userID].FillBatteries, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 48.954", "87.111 64.84", "Entity_Panel", "Fill_Batteries_Button_Off");
            Add_CuiButton(container, "creative.menu entity_stability_100", GetButtonColor(playerSettings[player.userID].EntityStability, false), "100%", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 16.305", "87.111 32.191", "Entity_Panel", "EntityStability_Button_Off");
            Add_CuiButton(container, "creative.menu entity_stability_default", GetButtonColor(playerSettings[player.userID].EntityStability, true), "Default", "robotocondensed-bold.ttf", 9, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 16.305", "114.69 32.191", "Entity_Panel", "EntityStability_Button_On");

            CuiHelper.DestroyUi(player, "Entity_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiAutoDoorsPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-618.56 -105.863", "-386.56 105.879", "Build_Panel", "AutoDoors_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 -101.019", "116 -78.083", "AutoDoors_Panel", "AutoDoors_Background_1");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 78.078", "116 101.014", "AutoDoors_Panel", "AutoDoors_Background_2");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.99 45.429", "116 68.365", "AutoDoors_Panel", "AutoDoors_Background_3");
            
            Add_CuiElement(container, "AutoDoors_Settings", "AutoDoors_Panel", GetTranslation("Menu_AutoDoors", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.995 -105.876", "115.995 -73.226");
            Add_CuiElement(container, "OpenDoors", "AutoDoors_Panel", GetTranslation("Menu_AutoDoors_CloseOpen", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 73.221", "115.991 105.871");
            Add_CuiElement(container, "Codelock", "AutoDoors_Panel", GetTranslation("Menu_AutoDoors_Locks", player), "robotocondensed-bold.ttf", 15, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-116 40.572", "115.991 73.222");
            Add_CuiButton(container, "creative.menu autodoors_on", GetButtonColor(playerSettings[player.userID].AutoDoors, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 -97.494", "114.69 -81.608", "AutoDoors_Panel", "AutoDoors_Button_On");
            Add_CuiButton(container, "creative.menu autodoors_off", GetButtonColor(playerSettings[player.userID].AutoDoors, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -97.494", "87.111 -81.608", "AutoDoors_Panel", "AutoDoors_Button_Off");
            Add_CuiButton(container, "creative.menu codelock_on_doors_true", GetButtonColor(playerSettings[player.userID].CodeLockDoor, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 48.954", "114.69 64.84", "AutoDoors_Panel", "Codelock_Button_On");
            Add_CuiButton(container, "creative.menu codelock_on_doors_false", GetButtonColor(playerSettings[player.userID].CodeLockDoor, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 48.954", "87.111 64.84", "AutoDoors_Panel", "Codelock_Button_Off");
            Add_CuiButton(container, "creative.menu doors_open", GetButtonColor(playerSettings[player.userID].DoorOpenClose, true), "OPEN", "robotocondensed-bold.ttf", 10, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 81.603", "114.69 97.489", "AutoDoors_Panel", "OpenDoors_Button_On");
            Add_CuiButton(container, "creative.menu doors_close", GetButtonColor(playerSettings[player.userID].DoorOpenClose, false), "CLOSE", "robotocondensed-bold.ttf", 10, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 81.603", "87.111 97.489", "AutoDoors_Panel", "OpenDoors_Button_Off");

            Add_CuiElement(container, "Image_Door_None", "AutoDoors_Panel", "X", "permanentmarker.ttf", 30, TextAnchor.MiddleCenter, "0.8 0.4 0.4 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-107.035 -12.794", "-66.565 31.394");
            Add_CuiElementImage(container, "Image_Door_Wooden", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("simple_wooden_door"), "0.5 0.5", "0.5 0.5", "-51.935 -12.794", "-11.465 31.394");
            Add_CuiElementImage(container, "Image_Door_SheetMetal", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("simple_metal_door"), "0.5 0.5", "0.5 0.5", "-107.035 -71.022", "-66.565 -26.834");
            Add_CuiElementImage(container, "Image_Door_Hqm", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("simple_hqm_door"), "0.5 0.5", "0.5 0.5", "-51.935 -71.022", "-11.465 -26.834");

            Add_CuiElementImage(container, "Image_Garage_Door", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("garage_door"), "0.5 0.5", "0.5 0.5", "11.465 -12.794", "51.935 31.394");
            Add_CuiElementImage(container, "Image_Double_Door_Wooden", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("double_wooden_door"), "0.5 0.5", "0.5 0.5", "66.565 -12.794", "107.035 31.394");
            Add_CuiElementImage(container, "Image_Double_Door_SheetMetal", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("double_metal_door"), "0.5 0.5", "0.5 0.5", "11.465 -71.022", "51.935 -26.834");
            Add_CuiElementImage(container, "Image_Double_Door_Hqm", "AutoDoors_Panel", "0 0 0 1", "1 -1", GetImage("double_hqm_door"), "0.5 0.5", "0.5 0.5", "66.565 -71.022", "107.035 -26.834");

            Add_CuiButton(container, "creative.deployable door_menu_none", GetListButtonColor("door_none"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-109.083 -14.999", "-64.517 33.599", "AutoDoors_Panel", "No_Door_Button");
            Add_CuiButton(container, "creative.deployable door_menu_wooden", GetListButtonColor("assets/prefabs/building/door.hinged/door.hinged.wood.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-53.983 -14.999", "-9.417 33.599", "AutoDoors_Panel", "Wooden_Door_Button");
            Add_CuiButton(container, "creative.deployable door_menu_sheetmetal", GetListButtonColor("assets/prefabs/building/door.hinged/door.hinged.metal.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-109.083 -73.227", "-64.517 -24.629", "AutoDoors_Panel", "SheetMetal_Door_Button");
            Add_CuiButton(container, "creative.deployable door_menu_hqm", GetListButtonColor("assets/prefabs/building/door.hinged/door.hinged.toptier.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-53.983 -73.227", "-9.417 -24.629", "AutoDoors_Panel", "HQM_Door_Button");
            Add_CuiButton(container, "creative.deployable double_door_menu_garage", GetListButtonColor("assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "9.417 -14.999", "53.983 33.599", "AutoDoors_Panel", "No_DoubleDoor_Button");
            Add_CuiButton(container, "creative.deployable double_door_menu_wooden", GetListButtonColor("assets/prefabs/building/door.double.hinged/door.double.hinged.wood.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "64.517 -14.999", "109.083 33.599", "AutoDoors_Panel", "Wooden_DoubleDoor_Button");
            Add_CuiButton(container, "creative.deployable double_door_menu_sheetmetal", GetListButtonColor("assets/prefabs/building/door.double.hinged/door.double.hinged.metal.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "9.417 -73.227", "53.983 -24.629", "AutoDoors_Panel", "SheetMetal_DoubleDoor_Button");
            Add_CuiButton(container, "creative.deployable double_door_menu_hqm", GetListButtonColor("assets/prefabs/building/door.double.hinged/door.double.hinged.toptier.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "64.517 -73.227", "109.083 -24.629", "AutoDoors_Panel", "HQM_DoubleDoor_Button");

            CuiHelper.DestroyUi(player, "AutoDoors_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiAutoWindowsPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-367.28 -105.863", "-135.28 105.879", "Build_Panel", "AutoWindows_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-115.989 -101.019", "116.001 -78.083", "AutoWindows_Panel", "AutoWindows_Background_1");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-104.814 78.078", "-11.182 101.014", "AutoWindows_Panel", "AutoWindows_Background_2");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "11.185 78.078", "104.817 101.014", "AutoWindows_Panel", "AutoWindows_Background_3");

            Add_CuiElement(container, "No_Windows_Button", "AutoWindows_Panel", "X", "permanentmarker.ttf", 25, TextAnchor.MiddleCenter, "0.8 0.4 0.4 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-104.813 27.7", "-67.944 64.199");
            Add_CuiElementImage(container, "Window_Bars_Wooden", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Window_Wooden"), "0.5 0.5", "0.5 0.5", "-48.772 29.483", "-14.733 62.52");
            Add_CuiElementImage(container, "Window_Bars_Metal", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Window_Metal"), "0.5 0.5", "0.5 0.5", "-103.819 -14.519", "-69.781 18.519");
            Add_CuiElementImage(container, "Window_Bars_Hqm", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Window_Hqm"), "0.5 0.5", "0.5 0.5", "-48.771 -14.519", "-14.733 18.519");
            Add_CuiElementImage(container, "Window_Window_Glass_Reinforced", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Window_Glass_Reinforced"), "0.5 0.5", "0.5 0.5", "-103.819 -58.467", "-69.781 -25.429");
            Add_CuiElement(container, "No_Embrasure_Button", "AutoWindows_Panel", "X", "permanentmarker.ttf", 30, TextAnchor.MiddleCenter, "0.8 0.4 0.4 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "9.417 15.601", "53.983 64.199");
            Add_CuiElementImage(container, "Embrasure_Shutters", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Embrasure_Shutters"), "0.5 0.5", "0.5 0.5", "66.565 17.806", "107.035 61.994");
            Add_CuiElementImage(container, "Embrasure_Metal_Vertical", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Embrasure_Metal_Vertical"), "0.5 0.5", "0.5 0.5", "11.465 -40.422", "51.935 3.766");
            Add_CuiElementImage(container, "Embrasure_Metal_Horizontal", "AutoWindows_Panel", "0 0 0 1", "1 -1", GetImage("Embrasure_Metal_Horizontal"), "0.5 0.5", "0.5 0.5", "66.565 -40.422", "107.035 3.766");

            Add_CuiElement(container, "AutoWindows_Settings", "AutoWindows_Panel", GetTranslation("Menu_AutoWindows", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.995 -105.876", "115.995 -73.226");
            Add_CuiElement(container, "Window", "AutoWindows_Panel", "WINDOWS", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.998 73.221", "0.002 105.871");
            Add_CuiElement(container, "Embrasure", "AutoWindows_Panel", "EMBRASURE", "robotocondensed-bold.ttf", 15, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "0.001 73.221", "116.001 105.871");
            Add_CuiButton(container, "creative.menu auto_windows_on", GetButtonColor(playerSettings[player.userID].AutoWindows, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 -97.494", "114.69 -81.608", "AutoWindows_Panel", "AutoWindows_Button_On");
            Add_CuiButton(container, "creative.menu auto_windows_off", GetButtonColor(playerSettings[player.userID].AutoWindows, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -97.494", "87.111 -81.608", "AutoWindows_Panel", "AutoWindows_Button_Off");
            Add_CuiButton(container, "creative.deployable window_none", GetListButtonColor("window_none"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-105.655 27.699", "-67.945 64.199", "AutoWindows_Panel", "No_Window");
            Add_CuiButton(container, "creative.deployable window_bars_wood", GetListButtonColor("wall.window.bars.wood"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-50.555 27.7", "-12.845 64.199", "AutoWindows_Panel", "WoodenWindow_Bar_Button");
            Add_CuiButton(container, "creative.deployable window_bars_metal", GetListButtonColor("wall.window.bars.metal"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-105.655 -16.25", "-67.945 20.25", "AutoWindows_Panel", "MetalWindow_Bar_Button");
            Add_CuiButton(container, "creative.deployable window_bars_toptier", GetListButtonColor("wall.window.bars.toptier"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-50.555 -16.25", "-12.845 20.25", "AutoWindows_Panel", "ReinforcedGlass_Window_Button");
            Add_CuiButton(container, "creative.deployable window_glass_reinforced", GetListButtonColor("wall.window.glass.reinforced"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-105.655 -60.198", "-67.945 -23.699", "AutoWindows_Panel", "StrengthenedGlass_Window_Button");
            Add_CuiButton(container, "creative.deployable embrasure_none", GetListButtonColor("embrasure_none"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "9.417 15.601", "53.983 64.199", "AutoWindows_Panel", "No_Embrasure");
            Add_CuiButton(container, "creative.deployable embrasure_wood", GetListButtonColor("assets/prefabs/building/wall.window.shutter/shutter.wood.a.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "64.517 15.601", "109.083 64.199", "AutoWindows_Panel", "WoodShutters_Button");
            Add_CuiButton(container, "creative.deployable embrasure_vertical", GetListButtonColor("assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.b.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "9.417 -42.627", "53.983 5.971", "AutoWindows_Panel", "MetalVertical_Embrasure_Button");
            Add_CuiButton(container, "creative.deployable embrasure_horizontal", GetListButtonColor("assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "64.517 -42.627", "109.083 5.971", "AutoWindows_Panel", "MetalHorizontal_Embrasure_Button");

            CuiHelper.DestroyUi(player, "AutoWindows_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiAutoElectricityPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-116.004 -105.871", "115.996 105.871", "Build_Panel", "AutoElectricity_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-116 -101.019", "115.99 -78.083", "AutoElectricity_Panel", "AutoWindows_Background_1");
            
            Add_CuiElementImage(container, "Electricity_Turrets", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("autoturret"), "0.5 0.5", "0.5 0.5", "-107.035 52.006", "-66.565 96.194");
            Add_CuiElementImage(container, "Electricity_SamSite", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("samsite"), "0.5 0.5", "0.5 0.5", "-51.935 52.006", "-11.465 96.194");
            Add_CuiElementImage(container, "Electricity_ElectricHeater", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("heater"), "0.5 0.5", "0.5 0.5", "11.465 52.006", "51.935 96.194");
            Add_CuiElementImage(container, "Electricity_FlasherLight", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("flasherlight"), "0.5 0.5", "0.5 0.5", "66.565 52.006", "107.035 96.194");
            Add_CuiElementImage(container, "Electricity_CeilingLight", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("ceilinglight"), "0.5 0.5", "0.5 0.5", "-107.035 -5.594", "-66.565 38.594");
            Add_CuiElementImage(container, "Electricity_ElectricFurnace", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("electricfurnace"), "0.5 0.5", "0.5 0.5", "-51.935 -5.594", "-11.465 38.594");
            Add_CuiElementImage(container, "Electricity_IndustrialWallLight", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("industrial"), "0.5 0.5", "0.5 0.5", "11.465 -5.594", "51.935 38.594");
            Add_CuiElementImage(container, "Electricity_NeonSign", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("animatedneon"), "0.5 0.5", "0.5 0.5", "66.565 -5.594", "107.035 38.594");
            Add_CuiElementImage(container, "Electricity_SearchLight", "AutoElectricity_Panel", "0 0 0 1", "1 -1", GetImage("searchlight"), "0.5 0.5", "0.5 0.5", "-107.035 -63.194", "-66.565 -19.006");

            Add_CuiElement(container, "AutoElectricity_Settings", "AutoElectricity_Panel", GetTranslation("Menu_AutoElectricity", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-115.995 -105.876", "115.995 -73.226");
            Add_CuiButton(container, "creative.menu autoelectricity_on", GetButtonColor(playerSettings[player.userID].AutoElectricity, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "87.11 -97.494", "114.69 -81.608", "AutoElectricity_Panel", "AutoElectricity_On");
            Add_CuiButton(container, "creative.menu autoelectricity_off", GetButtonColor(playerSettings[player.userID].AutoElectricity, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "59.531 -97.494", "87.111 -81.608", "AutoElectricity_Panel", "AutoElectricity_Off");
            Add_CuiButton(container, "creative.electricity autoturret", GetListButtonColor("autoturret_deployed", player), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-109.083 49.801", "-64.517 98.399", "AutoElectricity_Panel", "Turret_Button");
            Add_CuiButton(container, "creative.electricity samsite", GetListButtonColor("sam_site_turret_deployed"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-53.983 49.801", "-9.417 98.399", "AutoElectricity_Panel", "SamSite_Button");
            Add_CuiButton(container, "creative.electricity ceilinglight", GetListButtonColor("ceilinglight.deployed"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-109.083 -7.799", "-64.517 40.799", "AutoElectricity_Panel", "CeilingLight_Button");
            Add_CuiButton(container, "creative.electricity electric.furnace", GetListButtonColor("electricfurnace.deployed"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-53.983 -7.799", "-9.417 40.799", "AutoElectricity_Panel", "ElectricFurnace_Button");
            Add_CuiButton(container, "creative.electricity electric.heater", GetListButtonColor("electrical.heater"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "9.417 49.801", "53.983 98.399", "AutoElectricity_Panel", "ElectricHeater_Button");
            Add_CuiButton(container, "creative.electricity electric.flasherlight", GetListButtonColor("electric.flasherlight.deployed"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "64.517 49.801", "109.083 98.399", "AutoElectricity_Panel", "FlasherLight_Button");
            Add_CuiButton(container, "creative.electricity industrial.wall.light", GetListButtonColor("industrial.wall.lamp."), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "9.417 -7.799", "53.983 40.799", "AutoElectricity_Panel", "IndustrialWallLight_Button");
            Add_CuiButton(container, "creative.electricity sign.neon.", GetListButtonColor("sign.neon."), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "64.517 -7.799", "109.083 40.799", "AutoElectricity_Panel", "NeonSign_Button");
            Add_CuiButton(container, "creative.electricity searchlight", GetListButtonColor("searchlight.deployed"), "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-109.083 -65.399", "-64.517 -16.801", "AutoElectricity_Panel", "SearchLight_Button");


            CuiHelper.DestroyUi(player, "AutoElectricity_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiBuildingUpgradePanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "135.27 -105.871", "374.35 132.019", "Build_Panel", "BuildingUpgrade_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-119.525 -115.738", "114.651 -92.802", "BuildingUpgrade_Panel", "BuildingUpgrade_Background");
            Add_CuiElement(container, "BuildingGrade_Settings", "BuildingUpgrade_Panel", GetTranslation("Menu_BuildingGrade", player), "robotocondensed-bold.ttf", 20, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-119.525 -120.594", "112.465 -87.944");
            Add_CuiScrollView(container, "Scroll_View", "BuildingUpgrade_Panel", false, true, "0 0", "1 1", "0 0", "0 0", "-1.12 0", "2.5 1", "-100 0", "250 0");

            Add_CuiElementImage(container, "BuildGrade_Twigs", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("twigs"), "0.5 0.5", "0.5 0.5", "-108.715 -82.93", "-74.945 -46.67");
            Add_CuiElementImage(container, "BuildGrade_Wood", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("wood"), "0.5 0.5", "0.5 0.5", "-108.715 -42.13", "-74.945 -5.87");
            Add_CuiElementImage(container, "BuildGrade_Stone", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("stones"), "0.5 0.5", "0.5 0.5", "-108.715 -2.23", "-74.945 34.03");
            Add_CuiElementImage(container, "BuildGrade_Metal", "Scroll_View", "0 0 0 1", "1 -1", GetImage("metal fragments"), "0.5 0.5", "0.5 0.5", "-230.825 37.57", "-197.055 73.83");
            Add_CuiElementImage(container, "BuildGrade_Hqm", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("high quality metal"), "0.5 0.5", "0.5 0.5", "-108.715 77.87", "-74.945 114.13");

            Add_CuiElementImage(container, "BuildGrade_Wood_Dlc", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("wood_dlc"), "0.5 0.5", "0.5 0.5", "-67.275 -42.13", "-33.505 -5.87");
            Add_CuiElementImage(container, "BuildGrade_Stone_Dlc_Adobe", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("adobe"), "0.5 0.5", "0.5 0.5", "-67.275 -2.23", "-33.505 34.03");
            Add_CuiElementImage(container, "BuildGrade_Stone_Dlc_Bricks", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("bricks"), "0.5 0.5", "0.5 0.5", "-25.835 -2.23", "7.935 34.03");
            Add_CuiElementImage(container, "BuildGrade_Stone_Dlc_Brutalist", "BuildingUpgrade_Panel", "0 0 0 1", "1 -1", GetImage("brutalist"), "0.5 0.5", "0.5 0.5", "15.605 -2.23", "49.375 34.03");
            
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_1", "Scroll_View", "0 0 0 1", "1 -1", "0.3725 0.5607 0.7490 0.8", "0.5 0.5", "0.5 0.5", "-189.385 37.57", "-155.615 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_2", "Scroll_View", "0 0 0 1", "1 -1", "0.4549 0.7098 0.3490 0.8", "0.5 0.5", "0.5 0.5", "-147.945 37.57", "-114.175 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_3", "Scroll_View", "0 0 0 1", "1 -1", "0.5725 0.2901 0.8274 0.8", "0.5 0.5", "0.5 0.5", "-106.505 37.57", "-72.735 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_4", "Scroll_View", "0 0 0 1", "1 -1", "0.4235 0.1647 0.1058 0.8", "0.5 0.5", "0.5 0.5", "-65.065 37.57", "-31.295 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_5", "Scroll_View", "0 0 0 1", "1 -1", "0.8117 0.4588 0.1294 0.8", "0.5 0.5", "0.5 0.5", "-23.625 37.57", "10.145 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_6", "Scroll_View", "0 0 0 1", "1 -1", "0.8666 0.8666 0.8666 0.8", "0.5 0.5", "0.5 0.5", "17.815 37.57", "51.585 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_7", "Scroll_View", "0 0 0 1", "1 -1", "0.1882 0.1882 0.1882 0.8", "0.5 0.5", "0.5 0.5", "59.255 37.57", "93.025 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_8", "Scroll_View", "0 0 0 1", "1 -1", "0.3686 0.3098 0.2549 0.8", "0.5 0.5", "0.5 0.5", "100.695 37.57", "134.465 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_9", "Scroll_View", "0 0 0 1", "1 -1", "0.1725 0.2039 0.3215 0.8", "0.5 0.5", "0.5 0.5", "142.135 37.57", "175.905 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_10", "Scroll_View", "0 0 0 1", "1 -1", "0.2196 0.3254 0.1803 0.8", "0.5 0.5", "0.5 0.5", "432.215 37.57", "465.985 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_11", "Scroll_View", "0 0 0 1", "1 -1", "0.7098 0.2941 0.1843 0.8", "0.5 0.5", "0.5 0.5", "183.575 37.57", "217.345 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_12", "Scroll_View", "0 0 0 1", "1 -1", "0.7803 0.5333 0.4 0.8", "0.5 0.5", "0.5 0.5", "225.015 37.57", "258.785 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_13", "Scroll_View", "0 0 0 1", "1 -1", "0.8431 0.6627 0.2235 0.8", "0.5 0.5", "0.5 0.5", "266.455 37.57", "300.225 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_14", "Scroll_View", "0 0 0 1", "1 -1", "0.3333 0.3333 0.3333 0.8", "0.5 0.5", "0.5 0.5", "307.895 37.57", "341.665 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_15", "Scroll_View", "0 0 0 1", "1 -1", "0.2039 0.3372 0.3686 0.8", "0.5 0.5", "0.5 0.5", "349.335 37.57", "383.105 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_16", "Scroll_View", "0 0 0 1", "1 -1", "0.6627 0.6 0.5686 0.8", "0.5 0.5", "0.5 0.5", "390.775 37.57", "424.545 73.83");
            Add_CuiButton(container, "creative.menu buildgrade_on", GetButtonColor(playerSettings[player.userID].BuildingGrade, true), "ON", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "83.58 -112.212", "111.16 -96.326", "BuildingUpgrade_Panel", "BuildingGrade_On");
            Add_CuiButton(container, "creative.menu buildgrade_off", GetButtonColor(playerSettings[player.userID].BuildingGrade, false), "OFF", "robotocondensed-bold.ttf", 11, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "56.001 -112.212", "83.581 -96.326", "BuildingUpgrade_Panel", "BuildingGrade_Off");
            Add_CuiButton(container, "creative.bgrade twigs", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 -82.93", "-74.945 -46.67", "BuildingUpgrade_Panel", "Twigs_Button");
            Add_CuiButton(container, "creative.bgrade wood", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 -42.13", "-74.945 -5.87", "BuildingUpgrade_Panel", "Wood_Button");
            Add_CuiButton(container, "creative.bgrade stone", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 -2.23", "-74.945 34.03", "BuildingUpgrade_Panel", "Stone_Button");
            Add_CuiButton(container, "creative.bgrade metal", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-230.825 37.57", "-197.055 73.83", "Scroll_View", "Metal_Button");
            Add_CuiButton(container, "creative.bgrade hqm", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 77.87", "-74.945 114.13", "BuildingUpgrade_Panel", "HQM_Button");
            Add_CuiButton(container, "creative.bgrade wood_dlc", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-67.275 -42.13", "-33.505 -5.87", "BuildingUpgrade_Panel", "Wood_Dlc_Button");
            Add_CuiButton(container, "creative.bgrade stone_adobe", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-67.275 -2.23", "-33.505 34.03", "BuildingUpgrade_Panel", "Stone_Dlc_Adobe_Button");
            Add_CuiButton(container, "creative.bgrade stone_bricks", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-25.835 -2.23", "7.935 34.03", "BuildingUpgrade_Panel", "Stone_Dlc_Bricks_Button");
            Add_CuiButton(container, "creative.bgrade stone_brutalist", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "15.605 -2.23", "49.375 34.03", "BuildingUpgrade_Panel", "Stone_Dlc_Brutalist_Button");
            Add_CuiButton(container, "creative.bgrade metal_dlc 1", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-189.385 37.57", "-155.615 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_1");
            Add_CuiButton(container, "creative.bgrade metal_dlc 2", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-147.945 37.57", "-114.175 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_2");
            Add_CuiButton(container, "creative.bgrade metal_dlc 3", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-106.505 37.57", "-72.735 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_3");
            Add_CuiButton(container, "creative.bgrade metal_dlc 4", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-65.065 37.57", "-31.295 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_4");
            Add_CuiButton(container, "creative.bgrade metal_dlc 5", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-23.625 37.57", "10.145 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_5");
            Add_CuiButton(container, "creative.bgrade metal_dlc 6", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "17.815 37.57", "51.585 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_6");
            Add_CuiButton(container, "creative.bgrade metal_dlc 7", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "59.255 37.57", "93.025 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_7");
            Add_CuiButton(container, "creative.bgrade metal_dlc 8", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "100.695 37.57", "134.465 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_8");
            Add_CuiButton(container, "creative.bgrade metal_dlc 9", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "142.135 37.57", "175.905 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_9");
            Add_CuiButton(container, "creative.bgrade metal_dlc 10", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "183.575 37.57", "217.345 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_10");
            Add_CuiButton(container, "creative.bgrade metal_dlc 11", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "225.015 37.57", "258.785 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_11");
            Add_CuiButton(container, "creative.bgrade metal_dlc 12", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "266.455 37.57", "300.225 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_12");
            Add_CuiButton(container, "creative.bgrade metal_dlc 13", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "307.895 37.57", "341.665 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_13");
            Add_CuiButton(container, "creative.bgrade metal_dlc 14", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "349.335 37.57", "383.105 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_14");
            Add_CuiButton(container, "creative.bgrade metal_dlc 15", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "390.775 37.57", "424.545 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_15");
            Add_CuiButton(container, "creative.bgrade metal_dlc 16", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "432.215 37.57", "465.985 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_16");

            CuiHelper.DestroyUi(player, "BuildingUpgrade_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiBaseGradeUpdate(BasePlayer player)
        {
            var container = new CuiElementContainer();

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "379.49 -105.863", "618.57 132.027", "Build_Panel", "BaseGradeUpdate_Panel");
            Add_CuiPanel(container, true, "0.3 0.4 0.1 1", "0.5 0.5", "0.5 0.5", "-119.525 -115.738", "114.651 -92.802", "BaseGradeUpdate_Panel", "BuildingUpgrade_Background");
            string val = playerSettings[player.userID].BuildUpgradeInProgress ? "UPGRADING" : "WAITING";
            string val_lang = GetTranslation("Menu_BaseGrade", player);
            Add_CuiElement(container, "BuildingGrade_Settings", "BaseGradeUpdate_Panel", $"{val_lang} | {val}", "robotocondensed-bold.ttf", 20, TextAnchor.MiddleLeft, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-119.525 -120.594", "112.465 -87.944");
            Add_CuiScrollView(container, "Scroll_View", "BaseGradeUpdate_Panel", false, true, "0 0", "1 1", "0 0", "0 0", "-1.12 0", "2.5 1", "-100 0", "250 0");

            Add_CuiElementImage(container, "BuildGrade_Twigs", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("twigs"), "0.5 0.5", "0.5 0.5", "-108.715 -82.93", "-74.945 -46.67");
            Add_CuiElementImage(container, "BuildGrade_Wood", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("wood"), "0.5 0.5", "0.5 0.5", "-108.715 -42.13", "-74.945 -5.87");
            Add_CuiElementImage(container, "BuildGrade_Stone", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("stones"), "0.5 0.5", "0.5 0.5", "-108.715 -2.23", "-74.945 34.03");
            Add_CuiElementImage(container, "BuildGrade_Metal", "Scroll_View", "0 0 0 1", "1 -1", GetImage("metal fragments"), "0.5 0.5", "0.5 0.5", "-230.825 37.57", "-197.055 73.83");
            Add_CuiElementImage(container, "BuildGrade_Hqm", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("high quality metal"), "0.5 0.5", "0.5 0.5", "-108.715 77.87", "-74.945 114.13");

            Add_CuiElementImage(container, "BuildGrade_Wood_Dlc", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("wood_dlc"), "0.5 0.5", "0.5 0.5", "-67.275 -42.13", "-33.505 -5.87");
            Add_CuiElementImage(container, "BuildGrade_Stone_Dlc_Adobe", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("adobe"), "0.5 0.5", "0.5 0.5", "-67.275 -2.23", "-33.505 34.03");
            Add_CuiElementImage(container, "BuildGrade_Stone_Dlc_Bricks", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("bricks"), "0.5 0.5", "0.5 0.5", "-25.835 -2.23", "7.935 34.03");
            Add_CuiElementImage(container, "BuildGrade_Stone_Dlc_Brutalist", "BaseGradeUpdate_Panel", "0 0 0 1", "1 -1", GetImage("brutalist"), "0.5 0.5", "0.5 0.5", "15.605 -2.23", "49.375 34.03");
            
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_1", "Scroll_View", "0 0 0 1", "1 -1", "0.3725 0.5607 0.7490 0.8", "0.5 0.5", "0.5 0.5", "-189.385 37.57", "-155.615 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_2", "Scroll_View", "0 0 0 1", "1 -1", "0.4549 0.7098 0.3490 0.8", "0.5 0.5", "0.5 0.5", "-147.945 37.57", "-114.175 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_3", "Scroll_View", "0 0 0 1", "1 -1", "0.5725 0.2901 0.8274 0.8", "0.5 0.5", "0.5 0.5", "-106.505 37.57", "-72.735 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_4", "Scroll_View", "0 0 0 1", "1 -1", "0.4235 0.1647 0.1058 0.8", "0.5 0.5", "0.5 0.5", "-65.065 37.57", "-31.295 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_5", "Scroll_View", "0 0 0 1", "1 -1", "0.8117 0.4588 0.1294 0.8", "0.5 0.5", "0.5 0.5", "-23.625 37.57", "10.145 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_6", "Scroll_View", "0 0 0 1", "1 -1", "0.8666 0.8666 0.8666 0.8", "0.5 0.5", "0.5 0.5", "17.815 37.57", "51.585 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_7", "Scroll_View", "0 0 0 1", "1 -1", "0.1882 0.1882 0.1882 0.8", "0.5 0.5", "0.5 0.5", "59.255 37.57", "93.025 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_8", "Scroll_View", "0 0 0 1", "1 -1", "0.3686 0.3098 0.2549 0.8", "0.5 0.5", "0.5 0.5", "100.695 37.57", "134.465 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_9", "Scroll_View", "0 0 0 1", "1 -1", "0.1725 0.2039 0.3215 0.8", "0.5 0.5", "0.5 0.5", "142.135 37.57", "175.905 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_10", "Scroll_View", "0 0 0 1", "1 -1", "0.2196 0.3254 0.1803 0.8", "0.5 0.5", "0.5 0.5", "432.215 37.57", "465.985 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_11", "Scroll_View", "0 0 0 1", "1 -1", "0.7098 0.2941 0.1843 0.8", "0.5 0.5", "0.5 0.5", "183.575 37.57", "217.345 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_12", "Scroll_View", "0 0 0 1", "1 -1", "0.7803 0.5333 0.4 0.8", "0.5 0.5", "0.5 0.5", "225.015 37.57", "258.785 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_13", "Scroll_View", "0 0 0 1", "1 -1", "0.8431 0.6627 0.2235 0.8", "0.5 0.5", "0.5 0.5", "266.455 37.57", "300.225 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_14", "Scroll_View", "0 0 0 1", "1 -1", "0.3333 0.3333 0.3333 0.8", "0.5 0.5", "0.5 0.5", "307.895 37.57", "341.665 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_15", "Scroll_View", "0 0 0 1", "1 -1", "0.2039 0.3372 0.3686 0.8", "0.5 0.5", "0.5 0.5", "349.335 37.57", "383.105 73.83");
            Add_CuiElementImageColor(container, "BuildGrade_Metal_Dlc_Ship_16", "Scroll_View", "0 0 0 1", "1 -1", "0.6627 0.6 0.5686 0.8", "0.5 0.5", "0.5 0.5", "390.775 37.57", "424.545 73.83");
            Add_CuiButton(container, "creative.upbase twigs default", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 -82.93", "-74.945 -46.67", "BaseGradeUpdate_Panel", "Twigs_Button");
            Add_CuiButton(container, "creative.upbase wood default", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 -42.13", "-74.945 -5.87", "BaseGradeUpdate_Panel", "Wood_Button");
            Add_CuiButton(container, "creative.upbase stone default", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 -2.23", "-74.945 34.03", "BaseGradeUpdate_Panel", "Stone_Button");
            Add_CuiButton(container, "creative.upbase metal default", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-230.825 37.57", "-197.055 73.83", "Scroll_View", "Metal_Button");
            Add_CuiButton(container, "creative.upbase toptier default", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-108.715 77.87", "-74.945 114.13", "BaseGradeUpdate_Panel", "HQM_Button");
            Add_CuiButton(container, "creative.upbase wood legacy", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-67.275 -42.13", "-33.505 -5.87", "BaseGradeUpdate_Panel", "Wood_Dlc_Button");
            Add_CuiButton(container, "creative.upbase stone adobe", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-67.275 -2.23", "-33.505 34.03", "BaseGradeUpdate_Panel", "Stone_Dlc_Adobe_Button");
            Add_CuiButton(container, "creative.upbase stone bricks", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-25.835 -2.23", "7.935 34.03", "BaseGradeUpdate_Panel", "Stone_Dlc_Bricks_Button");
            Add_CuiButton(container, "creative.upbase stone brutalist", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "15.605 -2.23", "49.375 34.03", "BaseGradeUpdate_Panel", "Stone_Dlc_Brutalist_Button");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 1", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-189.385 37.57", "-155.615 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_1");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 2", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-147.945 37.57", "-114.175 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_2");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 3", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-106.505 37.57", "-72.735 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_3");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 4", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-65.065 37.57", "-31.295 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_4");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 5", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "-23.625 37.57", "10.145 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_5");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 6", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "17.815 37.57", "51.585 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_6");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 7", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "59.255 37.57", "93.025 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_7");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 8", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "100.695 37.57", "134.465 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_8");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 9", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "142.135 37.57", "175.905 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_9");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 10", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "183.575 37.57", "217.345 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_10");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 11", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "225.015 37.57", "258.785 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_11");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 12", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "266.455 37.57", "300.225 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_12");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 13", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "307.895 37.57", "341.665 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_13");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 14", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "349.335 37.57", "383.105 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_14");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 15", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "390.775 37.57", "424.545 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_15");
            Add_CuiButton(container, "creative.upbase metal shipcontainer 16", "0 0 0 0", "", "robotocondensed-regular.ttf", 10, TextAnchor.MiddleCenter, "0 0 0 1", "0.5 0.5", "0.5 0.5", "432.215 37.57", "465.985 73.83", "Scroll_View", "Metal_ShipContainter_Dlc_16");

            CuiHelper.DestroyUi(player, "BaseGradeUpdate_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiVehicleManagerPanel(BasePlayer player)
        {
            if (player == null || _config == null || !permission.UserHasPermission(player.UserIDString, "creative.vehicle"))
                return;
                
            var container = new CuiElementContainer();

            playerEntityCount.TryGetValue(player.userID, out List<SpawnedEntityInfo> spawnedEntities);

            Add_CuiPanel(container, true, "0.15 0.15 0.1 1", "0.5 0.5", "0.5 0.5", "-618.535 147.175", "618.565 264.825", "Build_Panel", "VehicleManager_Panel");
            string val_lang = GetTranslation("Menu_VehicleManager", player);
            Add_CuiElement(container, "VehicleManager_Settings", "VehicleManager_Panel", $"{val_lang} | {spawnedEntities.Count}/{_config.player_vehicle_limit}", "robotocondensed-bold.ttf", 20, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-141.395 -58.825", "90.595 -26.175");
            
            Add_CuiScrollView(container, "Scroll_View_VehicleManager", "VehicleManager_Panel", false, true, "0 0", "1 1", "0 0", "0 0", "0 0", "1 1", "-100 0", "100 0");

            Add_CuiElementImage(container, "VehicleManager_MagnetCrane", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("MagnetCrane"), "0.5 0.5", "0.5 0.5", "-564.306 -2.065", "-509.294 52.665");
            Add_CuiElementImage(container, "VehicleManager_Tugboat", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Tugboat"), "0.5 0.5", "0.5 0.5", "-497.706 -2.065", "-442.694 52.665");
            Add_CuiElementImage(container, "VehicleManager_Chinook", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Chinook"), "0.5 0.5", "0.5 0.5", "-429.906 -2.065", "-374.894 52.665");
            Add_CuiElementImage(container, "VehicleManager_AttackHelicopter", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("AttackHelicopter"), "0.5 0.5", "0.5 0.5", "-296.706 -2.065", "-241.694 52.665");
            Add_CuiElementImage(container, "VehicleManager_Sedan", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Sedan"), "0.5 0.5", "0.5 0.5", "-162.406 -2.06", "-107.394 52.665");
            Add_CuiElementImage(container, "VehicleManager_HotAirBallon", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("HotAirBallon"), "0.5 0.5", "0.5 0.5", "-230.106 -2.065", "-175.094 52.665");
            Add_CuiElementImage(container, "VehicleManager_Horse", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("RidableHorse"), "0.5 0.5", "0.5 0.5", "-95.606 -2.0655", "-40.594 52.665");
            Add_CuiElementImage(container, "VehicleManager_Minicopter", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Minicopter"), "0.5 0.5", "0.5 0.5", "-363.906 -2.065", "-308.894 52.665");
            Add_CuiElementImage(container, "VehicleManager_RowBoat", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("RowBoat"), "0.5 0.5", "0.5 0.5", "241.694 -2.065", "296.706 52.665");
            Add_CuiElementImage(container, "VehicleManager_DuoSubmarine", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("DuoSubmarine"), "0.5 0.5", "0.5 0.5", "504.694 -2.065", "559.706 52.665");
            Add_CuiElementImage(container, "VehicleManager_SoloSubmarine", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("SoloSubmarine"), "0.5 0.5", "0.5 0.5", "372.294 -2.065", "427.306 52.665");
            Add_CuiElementImage(container, "VehicleManager_ScrapCopter", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("ScrapCopter"), "0.5 0.5", "0.5 0.5", "438.994 -2.065", "494.006 52.665");        
            Add_CuiElementImage(container, "VehicleManager_2ModuleCar", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("2ModuleCar"), "0.5 0.5", "0.5 0.5", "-27.50 -2.065", "27.506 52.665");
            Add_CuiElementImage(container, "VehicleManager_3ModuleCar", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("3ModuleCar"), "0.5 0.5", "0.5 0.5", "40.894 -2.065", "95.906 52.665");
            Add_CuiElementImage(container, "VehicleManager_4ModuleCar", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("4ModuleCar"), "0.5 0.5", "0.5 0.5", "108.594 -2.065", "163.606 52.665");
            Add_CuiElementImage(container, "VehicleManager_Snowmobile", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Snowmobile"), "0.5 0.5", "0.5 0.5", "175.594 -2.065", "230.606 52.665");
            Add_CuiElementImage(container, "VehicleManager_RHIB", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Rhib"), "0.5 0.5", "0.5 0.5", "307.394 -2.065", "362.406 52.665");
            Add_CuiElementImage(container, "VehicleManager_Bike", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("Bike"), "0.5 0.5", "0.5 0.5", "570.594 -2.065", "625.606 52.665");
            Add_CuiElementImage(container, "VehicleManager_MotorBike", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("MotorBike"), "0.5 0.5", "0.5 0.5", "637.694 -2.065", "692.706 52.665");
            Add_CuiElementImage(container, "VehicleManager_MotorBikeSideCar", "Scroll_View_VehicleManager", "0 0 0 1", "1 -1", GetImage("MotorBikeSideCar"), "0.5 0.5", "0.5 0.5", "-629.406 -2.065", "-574.394 52.665");

            Add_CuiButton(container, "creative.spawn_manager ClearVehicles", "0.7 0.3 0.2 1", "WIPE VEHICLES", "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "1 1 1 1", "0.5 0.5", "0.5 0.5", "98.398 -53.464", "195.173 -31.536", "VehicleManager_Panel", "ClearVehicle_Button");
            Add_CuiButton(container, "creative.spawn_manager ScrapCopter", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "436.5 -4.7", "496.5 55.3", "Scroll_View_VehicleManager", "Minicopter_Button");
            Add_CuiButton(container, "creative.spawn_manager MagnetCrane", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-566.8 -4.7", "-506.8 55.3", "Scroll_View_VehicleManager", "ScrapCopter_Button");
            Add_CuiButton(container, "creative.spawn_manager Chinook", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-432.4 -4.7", "-372.4 55.3", "Scroll_View_VehicleManager", "Chinook_Button");
            Add_CuiButton(container, "creative.spawn_manager AttackHelicopter", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-299.2 -4.7", "-239.2 55.3", "Scroll_View_VehicleManager", "Sedan_Button");
            Add_CuiButton(container, "creative.spawn_manager Sedan", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-164.9 -4.7", "-104.9 55.3", "Scroll_View_VehicleManager", "2_Module_Car_Button");
            Add_CuiButton(container, "creative.spawn_manager RidableHorse", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-98.1 -4.7", "-38.1 55.3", "Scroll_View_VehicleManager", "3_Module_Car_Button");
            Add_CuiButton(container, "creative.spawn_manager 2ModuleCar", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-30 -4.7", "30 55.3", "Scroll_View_VehicleManager", "4_Module_Car_Button");
            Add_CuiButton(container, "creative.spawn_manager 3ModuleCar", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "38.4 -4.7", "98.4 55.3", "Scroll_View_VehicleManager", "Snowmobile_Button");
            Add_CuiButton(container, "creative.spawn_manager 4ModuleCar", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "106.1 -4.7", "166.1 55.3", "Scroll_View_VehicleManager", "RowBoat_Button");
            Add_CuiButton(container, "creative.spawn_manager Snowmobile", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "173.1 -4.7", "233.1 55.3", "Scroll_View_VehicleManager", "RHIB_Button");
            Add_CuiButton(container, "creative.spawn_manager RowBoat", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "239.2 -4.7", "299.2 55.3", "Scroll_View_VehicleManager", "Solo_Submarine_Button");
            Add_CuiButton(container, "creative.spawn_manager Rhib", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "304.9 -4.7", "364.9 55.3", "Scroll_View_VehicleManager", "Duo_Submarine_Button");
            Add_CuiButton(container, "creative.spawn_manager SoloSubmarine", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "369.8 -4.7", "429.8 55.3", "Scroll_View_VehicleManager", "Tugboat_Button");
            Add_CuiButton(container, "creative.spawn_manager DuoSubmarine", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "502.2 -4.7", "562.2 55.3", "Scroll_View_VehicleManager", "Hot_Air_Ballon_Button");
            Add_CuiButton(container, "creative.spawn_manager Tugboat", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-500.2 -4.7", "-440.2 55.3", "Scroll_View_VehicleManager", "Magnet_Crane_Button");
            Add_CuiButton(container, "creative.spawn_manager Minicopter", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-366.4 -4.7", "-306.4 55.3", "Scroll_View_VehicleManager", "Attack_Helicopter_Button");
            Add_CuiButton(container, "creative.spawn_manager HotAirBallon", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-232.6 -4.7", "-172.6 55.3", "Scroll_View_VehicleManager", "Horse_Button");
            Add_CuiButton(container, "creative.spawn_manager Bike", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "568.1 -4.7", "628.1 55.3", "Scroll_View_VehicleManager", "Bike");
            Add_CuiButton(container, "creative.spawn_manager MotorBike", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "635.2 -4.7", "695.2 55.3", "Scroll_View_VehicleManager", "MotorBike");
            Add_CuiButton(container, "creative.spawn_manager MotorBikeSideCar", "1 1 1 0.1", "", "robotocondensed-regular.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.1", "0.5 0.5", "0.5 0.5", "-631.9 -4.7", "-571.9 55.3", "Scroll_View_VehicleManager", "MotorBikeSideCar");

            CuiHelper.DestroyUi(player, "VehicleManager_Panel");
            CuiHelper.AddUi(player, container);
        }

        private void uiBuildCost(BasePlayer player, Dictionary<string, int> buildingBlockCosts, Dictionary<string, int> deployableCosts)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use") || !_config.buildcost_hud)
                return;
            
            var container = new CuiElementContainer();

            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (!playerSettings[player.userID].BCostHud)
            {
                CuiHelper.DestroyUi(player, "CostPanel");
                return;
            }

            Add_CuiPanel(container, false, "0.2 0.2 0.2 0.90", "1 0.5", "1 0.5", "-127.506 -253.445", "-0.001 220.969", "Hud", "CostPanel");
            
            // Titles
            Add_CuiElement(container, "BuildTitle", "CostPanel", "BUILD COST", "robotocondensed-bold.ttf", 20, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-63.754 204.372", "63.756 237.208");
            Add_CuiElement(container, "DeployableTitle", "CostPanel", "DEPLOYABLE COST", "robotocondensed-bold.ttf", 20, TextAnchor.MiddleCenter, "0.4 0.6 0.2 1", "0 0 0 0.5", "1 -1", "0.5 0.5", "0.5 0.5", "-63.755 18.323", "63.755 51.159");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0.3" },
                RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-63.756 51.159", OffsetMax = "63.754 204.365" }
            }, "CostPanel", "BuildCostPanel");

            int yOffset = 69;
            foreach (var cost in buildingBlockCosts)
            {
                Add_CuiElementImage(container, $"{cost.Key}_img", "BuildCostPanel", "0 0 0 0.5", "1 -1", GetImage(cost.Key), "0.5 0.5", "0.5 0.5", $"-59.91 {yOffset - 30}", $"-29.91 {yOffset}");
                Add_CuiElement(container, $"{cost.Key}_txt", "BuildCostPanel", cost.Value.ToString(), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.5", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", $"-26.149 {yOffset - 30}", $"21.149 {yOffset}");
                yOffset -= 36;
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0.3" },
                RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-63.755 -237.205", OffsetMax = "63.755 18.325" }
            }, "CostPanel", "DeployableCostPanel");

            yOffset = 121;
            foreach (var cost in deployableCosts)
            {
                Add_CuiElementImage(container, $"{cost.Key}_img", "DeployableCostPanel", "0 0 0 0.5", "1 -1", GetImage(cost.Key), "0.5 0.5", "0.5 0.5", $"-62 {yOffset - 30}", $"-32 {yOffset}");
                Add_CuiElement(container, $"{cost.Key}_txt", "DeployableCostPanel", cost.Value.ToString(), "robotocondensed-bold.ttf", 14, TextAnchor.MiddleCenter, "0 0 0 0.5", "0 0 0 0", "1 -1", "0.5 0.5", "0.5 0.5", $"-31.849 {yOffset - 30}", $"3.261 {yOffset}");
                yOffset -= 34;
            }
            
            CuiHelper.DestroyUi(player, "CostPanel");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Commands

        private void GodModeCmd(BasePlayer player, string command, string[] args)
        {
            if (!playerSettings.ContainsKey(player.userID) || !permission.UserHasPermission(player.UserIDString, "creative.use") || !permission.UserHasPermission(player.UserIDString, "creative.godmode"))
            {
                SendMessage(player, "NoPerms");
                return;
            }

            playerSettings[player.userID].GodMode = !playerSettings[player.userID].GodMode;
            player.SendConsoleCommand($"god {playerSettings[player.userID].GodMode}");
            string tx = playerSettings[player.userID].GodMode ? "ENABLED": "DISABLED";
            SendReply(player, $"Godmode {tx}");
        }

        private void NoClip(BasePlayer player, string saveName)
        {
            if (!playerSettings.ContainsKey(player.userID) || !permission.UserHasPermission(player.UserIDString, "creative.use") || !permission.UserHasPermission(player.UserIDString, "creative.fly"))
                return;

            playerSettings[player.userID].Noclip = !playerSettings[player.userID].Noclip;
            player.SendConsoleCommand($"noclip {playerSettings[player.userID].Noclip}");
        }

        private void bgradecmd(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, $"Usage /{command} <0 to 4>");
                return;
            }

            if (!playerSettings.ContainsKey(player.userID))
                return;

            int grade;
            if (int.TryParse(args[0], out grade) && grade >= 0 && grade <= 4)
            {
                playerSettings[player.userID].CurrentGrade = grade;
                uiBgradeHud(player);
                SendReply(player, "Changed building grade!");
            }
            else
            {
                SendReply(player, "Invalid grade value. Please enter a number between 0 and 4.");
            }
        }

        private void OpenMenuCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.use") || !playerSettings.ContainsKey(player.userID))
                return;

            if (menuCooldown.TryGetValue(player.userID, out float lastCommandTime))
            {
                if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 0.5f)
                    return;
            }

            menuCooldown[player.userID] = UnityEngine.Time.realtimeSinceStartup;

            uiMenuMain(player, playerSettings[player.userID].CurrentMenu);
        }

        private void CreativeWeatherCmdChat(BasePlayer player, string command, string[] args)
        {
            if (player == null || !_config.weather_menu)
                return;

            ulong playerId = player.userID;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (args.Length < 2)
            {
                player.ChatMessage($"Usage: /{command} <type> <value>");
                player.ChatMessage("Accepted types: time, wind, rain, thunder, rainbow, atmosphere_rayleigh, \natmosphere_mie, atmosphere_brightness, atmosphere_contrast, atmosphere_directionality, fog, cloud_size, \ncloud_opacity, cloud_coverage, cloud_sharpness, cloud_coloring, cloud_saturation, cloud_saturation, cloud_scattering");
                return;
            }

            float value = 0.0f;
            string type = args[0].ToLower();

            if (!float.TryParse(args[1], out value))
            {
                player.ChatMessage("Invalid value. Please provide a number.");
                return;
            }

            if (type.Contains("time"))
            {
                value = Mathf.Clamp(value, 0f, 24f);
                value = Mathf.Round(value * 100f) / 100f;
            }
            else
            {
                value = Mathf.Clamp(value, -2f, 2f);
                value = Mathf.Round(value * 100f) / 100f;
            }

            switch (type)
            {
                case "wind":
                    playerWeather[player.userID].wind = value;
                    break;
                case "rain":
                    playerWeather[player.userID].rain = value;
                    break;
                case "thunder":
                    playerWeather[player.userID].thunder = value;
                    break;
                case "rainbow":
                    playerWeather[player.userID].rainbow = value;
                    break;
                case "atmosphere_rayleigh":
                    playerWeather[player.userID].atmosphere_rayleigh = value;
                    break;
                case "atmosphere_mie":
                    playerWeather[player.userID].atmosphere_mie = value;
                    break;
                case "atmosphere_brightness":
                    playerWeather[player.userID].atmosphere_brightness = value;
                    break;
                case "atmosphere_contrast":
                    playerWeather[player.userID].atmosphere_contrast = value;
                    break;
                case "atmosphere_directionality":
                    playerWeather[player.userID].atmosphere_directionality = value;
                    break;
                case "fog":
                    playerWeather[player.userID].fog = value;
                    break;
                case "cloud_size":
                    playerWeather[player.userID].cloud_size = value;
                    break;
                case "cloud_opacity":
                    playerWeather[player.userID].cloud_opacity = value;
                    break;
                case "cloud_coverage":
                    playerWeather[player.userID].cloud_coverage = value;
                    break;
                case "cloud_sharpness":
                    playerWeather[player.userID].cloud_sharpness = value;
                    break;
                case "cloud_coloring":
                    playerWeather[player.userID].cloud_coloring = value;
                    break;
                case "cloud_attenuation":
                    playerWeather[player.userID].cloud_attenuation = value;
                    break;
                case "cloud_saturation":
                    playerWeather[player.userID].cloud_saturation = value;
                    break;
                case "cloud_scattering":
                    playerWeather[player.userID].cloud_scattering = value;
                    break;
                case "time":
                    playerWeather[player.userID].current_time = value;
                    SetPlayerTime(player, value);
                    break;
                default:
                    player.ChatMessage("Invalid weather type.");
                    return;
            }

            if (type != "time")
            {
                var weatherVars = new Dictionary<string, string>
                {
                    { $"weather.{type}", value.ToString() }
                };

                UpdatePlayerWeather(player.Connection, weatherVars);
                player.ChatMessage($"Weather {type} set to {value}");
            }
        }

        private void CreativeWeatherCmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong playerId = player.userID;

            if (player == null || !_config.weather_menu)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            var args = arg.Args;

            if (args.Length < 2)
            {
                player.ChatMessage("Usage: /weather-ttime <type> <value>");
                player.ChatMessage("Accepted types: time, wind, rain, thunder, rainbow, rayleighmultiplier, \nmiemultiplier, brightness, contrast, directionality, fogginess, size, \nopacity, coverage, sharpness, color, attenuation, saturation, scattering");
                uiWeatherMenu(player);
                return;
            }

            float value = 0.0f;
            string type = args[0].ToLower();
            bool isIncrement = args[1] == "a";

            if (args[1] == "a" || args[1] == "b")
            {
                switch (type)
                {
                    case "wind":
                        value = playerWeather[player.userID].wind;
                        break;
                    case "rain":
                        value = playerWeather[player.userID].rain;
                        break;
                    case "thunder":
                        value = playerWeather[player.userID].thunder;
                        break;
                    case "rainbow":
                        value = playerWeather[player.userID].rainbow;
                        break;
                    case "atmosphere_rayleigh":
                        value = playerWeather[player.userID].atmosphere_rayleigh;
                        break;
                    case "atmosphere_mie":
                        value = playerWeather[player.userID].atmosphere_mie;
                        break;
                    case "atmosphere_brightness":
                        value = playerWeather[player.userID].atmosphere_brightness;
                        break;
                    case "atmosphere_contrast":
                        value = playerWeather[player.userID].atmosphere_contrast;
                        break;
                    case "atmosphere_directionality":
                        value = playerWeather[player.userID].atmosphere_directionality;
                        break;
                    case "fog":
                        value = playerWeather[player.userID].fog;
                        break;
                    case "cloud_size":
                        value = playerWeather[player.userID].cloud_size;
                        break;
                    case "cloud_opacity":
                        value = playerWeather[player.userID].cloud_opacity;
                        break;
                    case "cloud_coverage":
                        value = playerWeather[player.userID].cloud_coverage;
                        break;
                    case "cloud_sharpness":
                        value = playerWeather[player.userID].cloud_sharpness;
                        break;
                    case "cloud_coloring":
                        value = playerWeather[player.userID].cloud_coloring;
                        break;
                    case "cloud_attenuation":
                        value = playerWeather[player.userID].cloud_attenuation;
                        break;
                    case "cloud_saturation":
                        value = playerWeather[player.userID].cloud_saturation;
                        break;
                    case "cloud_scattering":
                        value = playerWeather[player.userID].cloud_scattering;
                        break;
                    case "time":
                        value = playerWeather[player.userID].current_time;
                        break;

                    default:
                        player.ChatMessage("Invalid weather type.");
                        return;
                }

                if (type.Contains("time"))
                {
                    if (isIncrement)
                        value = Mathf.Clamp(value + 0.5f, 0f, 24f);
                    else
                        value = Mathf.Clamp(value - 0.5f, 0f, 24f);

                    value = Mathf.Round(value * 100f) / 100f;
                }else{
                    if (isIncrement)
                        value = Mathf.Clamp(value + 0.5f, -2f, 2f);
                    else
                        value = Mathf.Clamp(value - 0.5f, -2f, 2f);

                    value = Mathf.Round(value * 100f) / 100f;
                }

                switch (type)
                {
                    case "wind":
                        playerWeather[player.userID].wind = value;
                        break;
                    case "rain":
                        playerWeather[player.userID].rain = value;
                        break;
                    case "thunder":
                        playerWeather[player.userID].thunder = value;
                        break;
                    case "rainbow":
                        playerWeather[player.userID].rainbow = value;
                        break;
                    case "atmosphere_rayleigh":
                        playerWeather[player.userID].atmosphere_rayleigh = value;
                        break;
                    case "atmosphere_mie":
                        playerWeather[player.userID].atmosphere_mie = value;
                        break;
                    case "atmosphere_brightness":
                        playerWeather[player.userID].atmosphere_brightness = value;
                        break;
                    case "atmosphere_contrast":
                        playerWeather[player.userID].atmosphere_contrast = value;
                        break;
                    case "atmosphere_directionality":
                        playerWeather[player.userID].atmosphere_directionality = value;
                        break;
                    case "fog":
                        playerWeather[player.userID].fog = value;
                        break;
                    case "cloud_size":
                        playerWeather[player.userID].cloud_size = value;
                        break;
                    case "cloud_opacity":
                        playerWeather[player.userID].cloud_opacity = value;
                        break;
                    case "cloud_coverage":
                        playerWeather[player.userID].cloud_coverage = value;
                        break;
                    case "cloud_sharpness":
                        playerWeather[player.userID].cloud_sharpness = value;
                        break;
                    case "cloud_coloring":
                        playerWeather[player.userID].cloud_coloring = value;
                        break;
                    case "cloud_attenuation":
                        playerWeather[player.userID].cloud_attenuation = value;
                        break;
                    case "cloud_saturation":
                        playerWeather[player.userID].cloud_saturation = value;
                        break;
                    case "cloud_scattering":
                        playerWeather[player.userID].cloud_scattering = value;
                        break;
                    case "time":
                        playerWeather[player.userID].current_time = value;
                        break;
                }

                if (type.Contains("time"))
                {
                    SetPlayerTime(player, value);
                }else{
                    var weatherVars = new Dictionary<string, string>
                    {
                        { $"weather.{type}", value.ToString() }
                    };

                    UpdatePlayerWeather(player.Connection, weatherVars);
                    player.ChatMessage($"Weather {type} set to {value}");
                }
                uiWeatherMenu(player);
            }
            else if (!float.TryParse(args[1], out value))
            {
                player.ChatMessage("Invalid value. Please provide a number.");
            }
        }

        private void SetPlayerTime(BasePlayer player, float hours, bool message_h = true)
        {
            if (player == null || hours < 0 || hours > 24)
            {
                player.ChatMessage("Invalid time value. Hours must be between 0 and 24.");
                return;
            }

            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "creative.admin"))
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, false);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            }
            player.SendNetworkUpdateImmediate();

            player.SendConsoleCommand($"admintime {hours}", new object[] { });
            player.SendNetworkUpdateImmediate();

            NextTick(() =>
            {
                if (!permission.UserHasPermission(player.UserIDString, "creative.admin"))
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, true);
                    player.SendNetworkUpdateImmediate();
                }
            });
        }

        private void ResetWeatherCmd(BasePlayer player, bool is_init = true)
        {
            ulong playerId = player.userID;

            playerWeather[playerId] = new PlayerWeather(
                0f,  // wind
                0f,  // rain
                0f,  // thunder
                0f,  // rainbow
                2f,  // atmosphere_rayleigh
                4f,  // atmosphere_mie
                0.9f,  // atmosphere_brightness
                1.25f,  // atmosphere_contrast
                0.75f,  // atmosphere_directionality
                0.3f,  // fog
                2f,  // cloud_size
                0.25f,  // cloud_opacity
                0f,  // cloud_coverage
                0f,  // cloud_sharpness
                1f,  // cloud_coloring
                0.25f,  // cloud_attenuation
                1f,  // cloud_saturation
                1f,   // cloud_scattering
                12f,
                0f   // value
            );

            var weatherVars = new Dictionary<string, string>
            {
                {"weather.wind", "0"},
                {"weather.rain", "0"},
                {"weather.thunder", "0"},
                {"weather.rainbow", "0"},
                {"weather.atmosphere_rayleigh", "2"},
                {"weather.atmosphere_mie", "4"},
                {"weather.atmosphere_brightness", "0.9"},
                {"weather.atmosphere_contrast", "1.25"},
                {"weather.atmosphere_directionality", "0.75"},
                {"weather.fog", "0.3"},
                {"weather.cloud_size", "2"},
                {"weather.cloud_opacity", "0.25"},
                {"weather.cloud_coverage", "0"},
                {"weather.cloud_sharpness", "0"},
                {"weather.cloud_coloring", "1"},
                {"weather.cloud_attenuation", "0.25"},
                {"weather.cloud_saturation", "1"},
                {"weather.cloud_scattering", "1"}
            };

            UpdatePlayerWeather(player.Connection, weatherVars);
            SetPlayerTime(player, 12f);
            if (!is_init)
                uiWeatherMenu(player);
            player.ChatMessage("Weather settings have been reset to default.");
        }

        private void UpdatePlayerWeather(Connection connection, Dictionary<string, string> weatherVars)
        {
            NetWrite netWrite = Net.sv.StartWrite();
            List<Connection> connections = new List<Connection> { connection };

            List<KeyValuePair<string, string>> list2 = new List<KeyValuePair<string, string>>(weatherVars);
            netWrite.PacketID(Message.Type.ConsoleReplicatedVars);
            netWrite.Int32(list2.Count);

            foreach (var item in list2)
            {
                netWrite.String(item.Key);
                netWrite.String(item.Value);
            }

            netWrite.Send(new SendInfo(connections));
        }

        private static List<BasePlayer> FindPlayersOnline(string name)
        {
            List<BasePlayer> playersList = Facepunch.Pool.GetList<BasePlayer>();

            if (string.IsNullOrEmpty(name))
            {
                return playersList;
            }

            foreach (var activePlayer in BasePlayer.activePlayerList.ToList())
            {
                if (activePlayer.UserIDString.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    playersList.Add(activePlayer);
                }
                else if (!string.IsNullOrEmpty(activePlayer.displayName) &&
                        activePlayer.displayName.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    playersList.Add(activePlayer);
                }
                else if (activePlayer.net?.connection != null &&
                        activePlayer.net.connection.ipaddress.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    playersList.Add(activePlayer);
                }
            }

            return playersList;
        }

        private void team_invite(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Usage /invite <playername>");
                return;
            }

            string playerName = args[0];

            if (player.currentTeam == 0UL)
            {
                SendReply(player, "You should create a team before invite a player!");
                return;
            }

            RelationshipManager.PlayerTeam Team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            var Target = FindPlayersOnline(playerName);

            if (Target.Count <= 0)
            {
                SendReply(player, $"Player {playerName} not found!");
                return;
            }
            else if (Target.Count > 1)
            {
                SendReply(player, $"There are multiple players with the name {playerName}!");
                return;
            }
    
            var pTarget = Target[0];

            if (!pTarget || pTarget == null){
                SendReply(player, $"Player {playerName} not found!");
                return;
            }

            if (pTarget.currentTeam != 0UL){
                SendReply(player, $"Player {playerName} is already in another team!");
                return;
            }

            if (pTarget == player){
                SendReply(player, $"You can't invite yourself!");
                return;
            }

            Team.SendInvite(pTarget);
            SendReply(player, $"Invited {pTarget.displayName}");
            SendReply(pTarget, $"You have been invited to {player.displayName} team! \nPlease accept or reject it!");
        }

        private void LobbyCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.admin"))
            {
                SendMessage(player, "NoPerms");
                return;
            }

            _config.lobby_pos = player.transform.position;

            SendReply(player, "Lobby position saved!");

            SaveConfig();
        }

        [ConsoleCommand("creative.communitymenu")]
        private void CmdCommunityMenuConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            int page = 0;
            if (arg.HasArgs() && int.TryParse(arg.GetString(0), out int parsedPage))
            {
                page = Math.Max(0, parsedPage);
            }

            uiCommunityMenu(player, page);
        }

        [ConsoleCommand("creative.menu")]
        private async void MenuCommands(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;
            
            var player = arg?.Player();

            if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use") || !playerSettings.ContainsKey(player.userID))
                return;
                
            string command = args[0].ToString();

            switch (command)
            {
                #region Main Menu Commands

                case "CLAIM":
                case "UNCLAIM":
                    if (IsTeamLeader(player))
                    {
                        SendMessage(player, "TeamLeader");
                        return;
                    }

                     if (baseCooldown.TryGetValue(player.userID, out float lastCommandTime))
                    {
                        if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 2f)
                        {
                            SendMessage(player, "Cooldown");
                            return;
                        }
                    }

                    baseCooldown[player.userID] = UnityEngine.Time.realtimeSinceStartup;

                    if (!playerSettings[player.userID].PlayerOwnZone)
                        ClaimZone(player);
                    else
                        UnclaimZone(player);
                        
                    uiMenuMain(player);
                break;

                case "weather":
                    uiWeatherMenu(player);
                break;

                case "build":
                    playerSettings[player.userID].CurrentMenu = "build";
                    uiMenuMain(player, playerSettings[player.userID].CurrentMenu);
                break;

                case "community":
                    playerSettings[player.userID].CurrentMenu = "community";
                    uiMenuMain(player, playerSettings[player.userID].CurrentMenu);
                break;

                case "request_resources":
                    GrantResources(player);
                break;

                case "close_menu":
                    uiCloseAll(player);
                break;

                case "close_weather_menu":
                    CuiHelper.DestroyUi(player, "WeatherPanel");
                break;

                case "reset_weather_menu":
                    //ResetWeatherCmd(player, false);
                break;
                #endregion

                #region BuildMenu Commands

                case "entity_turn_on":
                case "entity_turn_off":
                    playerSettings[player.userID].AutoEntity = !playerSettings[player.userID].AutoEntity;
                    ToggleFurnaces(player, playerSettings[player.userID].AutoEntity);
                    uiEntityPanel(player);
                break;

                case "fill_batteries_on":
                case "fill_batteries_off":
                    playerSettings[player.userID].FillBatteries = !playerSettings[player.userID].FillBatteries;
                    FillBatteries(player);
                    uiEntityPanel(player);
                break;

                case "entity_stability_100":
                case "entity_stability_default":
                    playerSettings[player.userID].EntityStability = !playerSettings[player.userID].EntityStability;
                    SetBuildingStability(player, playerSettings[player.userID].EntityStability);
                    uiEntityPanel(player);
                break;

                #endregion

                #region PersonalSettings Commands

                case "fly_off":
                case "fly_on":
                    if (permission.UserHasPermission(player.UserIDString, "creative.fly"))
                    {
                        playerSettings[player.userID].Noclip = !playerSettings[player.userID].Noclip;
                        player.SendConsoleCommand($"noclip {playerSettings[player.userID].Noclip}");
                        uiPersonalPanel(player);
                    }
                break;

                case "godmode_off":
                case "godmode_on":
                    if (permission.UserHasPermission(player.UserIDString, "creative.godmode"))
                    {
                        playerSettings[player.userID].GodMode = !playerSettings[player.userID].GodMode;
                        player.SendConsoleCommand($"god {playerSettings[player.userID].GodMode}");
                        uiPersonalPanel(player);
                    }else{
                        SendMessage(player, "NoPerms");
                    }
                break;

                case "changebgrade_off":
                case "changebgrade_on":
                    playerSettings[player.userID].ChangeBGrade = !playerSettings[player.userID].ChangeBGrade;
                    uiPersonalPanel(player);
                break;

                case "infammo_off":
                case "infammo_on":
                    playerSettings[player.userID].InfiniteAmmo = !playerSettings[player.userID].InfiniteAmmo;
                    uiPersonalPanel(player);
                break;

                case "bgradehud_off":
                case "bgradehud_on":
                    playerSettings[player.userID].BGradeHud = !playerSettings[player.userID].BGradeHud;
                    uiPersonalPanel(player);
                    uiBgradeHud(player);
                break;

                #endregion

                #region PlotSettings Commands

                case "gtfo_off":
                case "gtfo_on":
                    playerSettings[player.userID].GTFO = !playerSettings[player.userID].GTFO;
                    uiPlotPanel(player);
                break;

                case "raid_on":
                case "raid_off":
                    playerSettings[player.userID].Raid = !playerSettings[player.userID].Raid;
                    uiPlotPanel(player);
                break;

                case "wipe_to_foundations":
                    /*playerSettings[player.userID].clearing_plot = true;
                    await ClearToFoundations(player);
                    playerSettings[player.userID].clearing_plot = false;
                    NextTick(() =>
                    {
                        uiPlotPanel(player);
                    });*/
                break;

                case "wipe_deployables":
                    /*playerSettings[player.userID].clearing_plot = true;
                    await ClearDeployables(player);
                    playerSettings[player.userID].clearing_plot = false;
                    NextTick(() =>
                    {
                        uiPlotPanel(player);
                    });*/
                break;

                case "clear_plot":
                    playerSettings[player.userID].LoadingBase = true;
                    ClearAll(player);
                    uiPlotPanel(player);
                break;

                #endregion

                #region AutoDoors Commands

                case "autodoors_off":
                case "autodoors_on":
                    playerSettings[player.userID].AutoDoors = !playerSettings[player.userID].AutoDoors;
                    uiAutoDoorsPanel(player);
                break;

                case "doors_open":
                case "doors_close":
                    playerSettings[player.userID].DoorOpenClose = !playerSettings[player.userID].DoorOpenClose;
                    OpenClosePlayerDoors(player, playerSettings[player.userID].DoorOpenClose);
                    uiAutoDoorsPanel(player);
                break;

                case "codelock_on_doors_true":
                case "codelock_on_doors_false":
                    playerSettings[player.userID].CodeLockDoor = !playerSettings[player.userID].CodeLockDoor;
                    uiAutoDoorsPanel(player);
                break;

                #endregion

                #region AutoWindows Commands

                case "auto_windows_on":
                case "auto_windows_off":
                    playerSettings[player.userID].AutoWindows = !playerSettings[player.userID].AutoWindows;
                    uiAutoWindowsPanel(player);
                break;

                #endregion

                #region AutoElectricity Commands
                
                case "autoelectricity_off":
                case "autoelectricity_on":
                    playerSettings[player.userID].AutoElectricity = !playerSettings[player.userID].AutoElectricity;

                    foreach (var entity in BaseEntity.serverEntities)
                    {
                        if (entity != null && playerSettings[player.userID].ElectricityList.Contains(entity.ShortPrefabName))
                        {
                            ElectricityManager(entity);
                        }
                    }
                    
                    uiAutoElectricityPanel(player);
                break;

                #endregion

                #region BuildingGrade Commands

                case "buildgrade_off":
                case "buildgrade_on":
                    playerSettings[player.userID].BuildingGrade = !playerSettings[player.userID].BuildingGrade;
                    uiBuildingUpgradePanel(player);
                break;

                #endregion

                #region VehicleManager Commands

                case "vehicle_manager_1":
                    if (permission.UserHasPermission(player.UserIDString, "creative.vehicle"))
                        uiVehicleManagerPanel(player);
                break;

                #endregion
            }
        }

        [ConsoleCommand("creative.changecommmenu")]
        private async void ChangeCommMenu(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (args.Length < 1)
                return;
            
            if (playerSettings[player.userID].CommunityCurrentMenu == args[0])
                return;

            playerSettings[player.userID].CommunityCurrentMenu = args[0];
            playerSettings[player.userID].CurrentPage = 0;

            string steamId = player.UserIDString;
            uiCommunityMenu(player, playerSettings[player.userID].CurrentPage);
        }

        [ConsoleCommand("creative.changepage")]
        private async void ChangePage(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (args.Length < 1)
                return;

            if (int.TryParse(args[0], out int newPage))
            {
                playerSettings[player.userID].CurrentPage = newPage;

                NextTick(() => 
                {
                    string steamId = player.UserIDString;
                    uiCommunityMenu(player, playerSettings[player.userID].CurrentPage);
                });
            }
            else
            {
                player.ChatMessage("Invalid page number. Please enter a valid number.");
            }
        }

        [ConsoleCommand("creative.initiatereview")]
        private void InitiateReviewConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args.Length < 3)
            {
                Puts("Usage: creative.initiatereview <baseName> <steamId> <imageUrl>");
                return;
            }

            string baseName = arg.Args[0];
            ulong steamId = ulong.Parse(arg.Args[1]);
            string baseImageUrl = arg.Args[2];

            BasePlayer player = arg.Player();

            if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.reviewer"))
            {
                player.ChatMessage("You do not have permission to review bases.");
                return;
            }

            if (!playerSettings.ContainsKey(player.userID))
                return;

            player.ChatMessage($"\nYou are reviewing the base '<color=#aeed6f>{baseName}</color>' owned by '<color=#aeed6f>{steamId}</color>'. \nPlease type '<color=#aeed6f>review delete <reason></color>' to delete it, or '<color=#aeed6f>review cancel</color>' to cancel the review.");

            playerSettings[player.userID].PendingReviewBase = baseName;
            playerSettings[player.userID].PendingReviewSteamId = steamId;
            playerSettings[player.userID].PendingReviewImageUrl = baseImageUrl;
            playerSettings[player.userID].PendingReviewTime = Time.realtimeSinceStartup;

            uiCloseAll(player);

            timer.Once(30f, () =>
            {
                if (playerSettings[player.userID].PendingReviewBase == baseName)
                {
                    player.ChatMessage("Review process timed out. The base was not deleted.");
                    playerSettings[player.userID].PendingReviewBase = null;
                }
            });
        }

        [ConsoleCommand("share")]
        private async void ShareBaseCmd(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null)
                return;
                
            if (!permission.UserHasPermission(player.userID.ToString(), "creative.use"))
                return;

            if (args.Length < 1)
            {
                SendReply(player, "Usage: /share <base name>");
                return;
            }

            string baseName = args[0];
            string creatorName = player.displayName;
            string steamId = player.userID.ToString();
            string basePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{steamId}/{baseName}.json");
            string baseImagePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{steamId}/{baseName}.png");
            string imageUrl = "https://i.imgur.com/0QtCHOh.png";

            if (!File.Exists(basePath))
            {
                SendReply(player, $"Base '{baseName}' does not exist.");
                return;
            }

            BaseShareInfo existingBaseInfo = await Task.Run(() => GetBaseInfoForPlayer(steamId, baseName));
            
            if (existingBaseInfo != null)
            {
                SendReply(player, $"Base '{baseName}' already exists with code {existingBaseInfo.ShareCode}.");
                SendReply(player, "If you want to update this base, use the command /share_update <base name>. To cancel, use /share_cancel.");
                return;
            }

            SendReply(player, $"Sharing base '{baseName}', please wait...");

            if (File.Exists(baseImagePath))
            {
                imageUrl = await Task.Run(() => UploadImageToHostingService(baseImagePath));
            }

            string baseUrl = "";
            
            if (_config.pastee_auth != "PASTE.EE TOKEN HERE")
                baseUrl = await Task.Run(() => GetBaseUrlForSavedBase(steamId, baseName));

            string shareCode;
            do
            {
                System.Random random = new System.Random();
                if (permission.UserHasPermission(player.UserIDString, "creative.vip"))
                    shareCode = $"[VIP]{random.Next(10000, 99999)}";
                else
                    shareCode = random.Next(10000, 99999).ToString();
            } while (await Task.Run(() => ShareCodeExists(shareCode)));


            await Task.Run(() =>
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    string insertQuery = @"
                        INSERT INTO BaseShare (CreatorName, SteamId, BaseName, ShareCode, ImageUrl, BaseUrl) 
                        VALUES (@CreatorName, @SteamId, @BaseName, @ShareCode, @ImageUrl, @BaseUrl)";

                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@CreatorName", creatorName);
                        command.Parameters.AddWithValue("@SteamId", steamId);
                        command.Parameters.AddWithValue("@BaseName", baseName);
                        command.Parameters.AddWithValue("@ShareCode", shareCode);
                        command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                        command.Parameters.AddWithValue("@BaseUrl", baseUrl);
                        command.ExecuteNonQuery();
                    }
                }
            });

            SendReply(player, $"Base '{baseName}' shared with code {shareCode}.");
        }

        [ConsoleCommand("load_code")]
        private async void LoadCodeCmd(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null)
                return;

            try
            {
                if (!playerSettings.ContainsKey(player.userID))
                    return;
                    
                if (!permission.UserHasPermission(player.userID.ToString(), "creative.use"))
                    return;
                    
                if (baseCooldown.TryGetValue(player.userID, out float lastCommandTime))
                {
                    if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 5f)
                    {
                        SendMessage(player, "Cooldown");
                        return;
                    }
                }

                baseCooldown[player.userID] = UnityEngine.Time.realtimeSinceStartup;

                Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

                if (Zone_Center_Pos == Vector3.zero)
                {   
                    SendMessage(player, "NoClaimed");
                    return;
                }

                if (args.Length < 1)
                {
                    SendReply(player, "Usage: F1 CONSOLE -> load_code <base code>");
                    return;
                }

                string shareCode = args[0];
                BaseShareInfo baseInfo = GetBaseInfo(shareCode);

                if (baseInfo != null)
                {
                    if (shareCode.StartsWith("[VIP]"))
                    {
                        SendReply(player, "<color=red>Note: This base was created by a VIP user. It may not load correctly.</color>");
                    }
                    
                    var saveData = Interface.Oxide.DataFileSystem.ReadObject<JObject>($"Creative/{baseInfo.SteamId}/{baseInfo.BaseName}");
                    Quaternion playerRotation = player.transform.rotation;
                    
                    if (!playerSettings[player.userID].LoadingBase)
                    {
                        _ = ClearEntitiesForPlayer(player, playerSettings[player.userID].StoredClaimedPlotLocation);
                        await Task.Delay(100);
                        await LoadBase(saveData, player, Zone_Center_Pos, playerRotation, baseInfo.BaseName);

                        playerSettings[player.userID].LoadingBase = true;
                    }
                    else{
                        player.ChatMessage($"Already loading your base. Please wait...");
                    }

                    SendReply(player, $"Loaded base with code {shareCode} created by {baseInfo.CreatorName}.");
                    IncrementDownloads(shareCode);
                }
                else
                {
                    SendReply(player, "Base with the provided code does not exist.");
                }
            }
            catch (Exception e)
            {
                LogErrors(e.Message, "LoadCodeCmd");
            }
        }

        [ConsoleCommand("vote")]
        private async void VoteCmd(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg.Player();

            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (args.Length < 2)
            {
                player.ChatMessage("Usage: /vote <share code> <upvote/downvote>");
                return;
            }

            string shareCode = args[0];
            string voteType = args[1].ToLower();
            string steamId = player.UserIDString;

            if (voteType != "upvote" && voteType != "downvote")
            {
                player.ChatMessage("Invalid vote type. Use 'upvote' or 'downvote'.");
                return;
            }

            string normalizedVoteType = voteType == "upvote" ? "Upvote" : "Downvote";

            var existingVoteType = await CheckExistingVoteAsync(shareCode, steamId);
            
            if (!string.IsNullOrEmpty(existingVoteType))
            {
                if (existingVoteType == normalizedVoteType)
                {
                    player.ChatMessage("You have already voted for this base with the same vote.");
                    return;
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        await UpdateVoteInDatabaseAsync(shareCode, steamId, normalizedVoteType);

                        if (existingVoteType == "Upvote")
                        {
                            await UpdateBaseVotesAsync(shareCode, "Downvote");
                            await UpdateBaseVotesAsync(shareCode, "Upvote", -1);
                        }
                        else
                        {
                            await UpdateBaseVotesAsync(shareCode, "Upvote");
                            await UpdateBaseVotesAsync(shareCode, "Downvote", -1); 
                        }
                    });

                    player.ChatMessage($"Successfully changed your vote to {normalizedVoteType.ToLower()} for base {shareCode}.");
                }
            }
            else
            {

                await Task.Run(async () =>
                {
                    await SaveVoteToDatabaseAsync(shareCode, steamId, normalizedVoteType);
                    await UpdateBaseVotesAsync(shareCode, normalizedVoteType);
                });

                player.ChatMessage($"Successfully {normalizedVoteType.ToLower()}d base {shareCode}.");
            }

            uiCommunityMenu(player, playerSettings[player.userID].CurrentPage);
        }

        [ConsoleCommand("share_update")]
        private async void ShareUpdateCmd(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null)
                return;

            if (!permission.UserHasPermission(player.userID.ToString(), "creative.use"))
                return;

            if (args.Length < 1)
            {
                SendReply(player, "Usage: /share_update <base name>");
                return;
            }

            string baseName = args[0];
            string steamId = player.userID.ToString();
            string baseImagePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{steamId}/{baseName}.png");
            string imageUrl = "https://i.imgur.com/0QtCHOh.png";

            BaseShareInfo existingBaseInfo = await Task.Run(() => GetBaseInfoForPlayer(steamId, baseName));
            
            if (existingBaseInfo == null)
            {
                SendReply(player,$"Base '{baseName}' does not exist or has not been shared yet.");
                return;
            }

            SendReply(player,$"Updating base '{baseName}', please wait...");

            if (File.Exists(baseImagePath))
            {
                imageUrl = await Task.Run(() => UploadImageToHostingService(baseImagePath));
            }

            await Task.Run(() =>
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE BaseShare SET ImageUrl = @ImageUrl WHERE SteamId = @SteamId AND BaseName = @BaseName";

                    using (var command = new SqliteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                        command.Parameters.AddWithValue("@SteamId", steamId);
                        command.Parameters.AddWithValue("@BaseName", baseName);
                        command.ExecuteNonQuery();
                    }
                }
            });

            SendReply(player, $"Base '{baseName}' has been updated.");
        }

        [ConsoleCommand("share_cancel")]
        private void ShareCancelCmd(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null)
                return;

            SendReply(player, "Base sharing process has been canceled.");
        }

        [ConsoleCommand("creative.unshare")]
        private void InitiateUnshare(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args.Length < 2)
            {
                Puts("Usage: creative.unshare <baseName> <steamId>");
                return;
            }

            string baseName = arg.Args[0];
            ulong steamId = ulong.Parse(arg.Args[1]);

            BasePlayer player = arg.Player();

            if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use") || player.userID != steamId)
            {
                player.ChatMessage("You do not have permission to unshare thid base.");
                return;
            }

            player.ChatMessage($"\nUnshared '<color=#aeed6f>{baseName}</color>'!");
            DeleteBaseReview(player, baseName);
        }

        [ChatCommand("review")]
        void HandleReviewCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "creative.reviewer"))
            {
                player.ChatMessage("You do not have permission to review bases.");
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage("Usage: review delete <reason> | review cancel");
                return;
            }

            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (playerSettings[player.userID].PendingReviewBase == null)
            {
                player.ChatMessage("No review is currently in progress.");
                return;
            }

            string baseName = playerSettings[player.userID].PendingReviewBase;
            ulong steamId = playerSettings[player.userID].PendingReviewSteamId;
            string baseImageUrl = playerSettings[player.userID].PendingReviewImageUrl;

            if (args[0] == "delete" && args.Length > 1)
            {
                string reason = string.Join(" ", args.Skip(1));

                LogReview(player, reason, baseName, steamId, baseImageUrl);
                DeleteBaseReview(player, baseName);

                player.ChatMessage($"base '<color=#aeed6f>{baseName}</color>' was deleted. \nReason: <color=#aeed6f>{reason}</color>");

                playerSettings[player.userID].PendingReviewBase = null;
            }
            else if (args[0] == "cancel")
            {
                player.ChatMessage("Review process canceled.");
                playerSettings[player.userID].PendingReviewBase = null;
            }
            else
            {
                player.ChatMessage("Invalid command. Use 'review delete <reason>' or 'review cancel'.");
            }
        }

        private async void DeleteBaseReview(BasePlayer player, string baseName)
        {
            string playerId = player.userID.ToString();
            string basePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerId}/{baseName}.json");

            string baseUrl = null;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT BaseUrl FROM SavedBases WHERE BaseName = @baseName AND PlayerId = @playerId";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            baseUrl = reader["BaseUrl"]?.ToString();
                        }
                    }
                }

                string deleteBaseShareQuery = "DELETE FROM BaseShare WHERE BaseName = @baseName AND SteamId = @playerId";
                using (var command = new SqliteCommand(deleteBaseShareQuery, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.ExecuteNonQuery();
                }
            }

            if (!string.IsNullOrEmpty(baseUrl))
            {
                await DeleteBaseFromPasteAPI(baseUrl);
            }
            
            Puts($"Base '{baseName}' deleted from paste.ee.");
        }

        private void LogReview(BasePlayer player, string reason, string baseName, ulong baseOwnerID, string baseImageUrl)
        {
            string reviewLog = $"'[{player.userID}] {player.displayName}' deleted base '{baseName}', '{baseOwnerID}', '{baseImageUrl}' with the reason: '{reason}'";

            string path = Path.Combine(Interface.GetMod().DataDirectory, "Creative/reviews.json");
            var reviews = File.Exists(path) ? JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path)) : new List<string>();
            reviews.Add(reviewLog);
            File.WriteAllText(path, JsonConvert.SerializeObject(reviews, Formatting.Indented));
        }

        [ConsoleCommand("creative.deletebase")]
        private void cmdDeleteBase(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon) return;

            BasePlayer player = arg.Player();

            if (player == null || arg.Args.Length < 1)
            {
                player.ChatMessage("Usage: /deletebase <basename>");
                return;
            }

            string baseName = arg.Args[0];
            string playerId = player.userID.ToString();
            string basePath = Path.Combine(Interface.GetMod().DataDirectory, $"Creative/{playerId}/{baseName}.json");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM SavedBases WHERE BaseName = @baseName AND PlayerId = @playerId";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@baseName", baseName);
                    command.Parameters.AddWithValue("@playerId", playerId);

                    long count = (long)command.ExecuteScalar();
                    if (count == 0)
                    {
                        player.ChatMessage($"Base '{baseName}' not found.");
                        return;
                    }
                }
            }

            player.ChatMessage($"<color=#aeed6f>Are you sure you want to delete the base</color> '{baseName}'? \n<color=#aeed6f>Type </color>/delete yes <color=#aeed6f>to confirm or </color>/delete no <color=#aeed6f>to cancel.</color>");

            timer.Once(15f, () => 
            {
                player.ChatMessage("Deletion cancelled due to timeout.");
            });

            _pendingDeletions[playerId] = baseName;
        }

        private readonly Dictionary<string, string> _pendingDeletions = new Dictionary<string, string>();

        [ChatCommand("delete")]
        private void cmdConfirmDelete(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1 || !_pendingDeletions.ContainsKey(player.userID.ToString()))
            {
                player.ChatMessage("You have no pending deletion requests.");
                return;
            }

            string baseName = _pendingDeletions[player.userID.ToString()];

            if (args[0].ToLower() == "yes")
            {
                DeleteBase(player, baseName);
                player.ChatMessage($"Base '{baseName}' has been deleted.");
            }
            else if (args[0].ToLower() == "no")
            {
                player.ChatMessage("Base deletion cancelled.");
            }
            else
            {
                player.ChatMessage("Invalid response. Type /delete yes to confirm or /delete no to cancel.");
                return;
            }

            _pendingDeletions.Remove(player.userID.ToString());
        }

        [ConsoleCommand("creative.upbase")]
        private void UpBaseCmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong playerId = player.userID;

            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            if (arg.Args == null || arg.Args.Length < 2 || arg.Args.Length > 4)
                return;

            if (commandCooldowns.TryGetValue(player.userID, out float lastCommandTime))
            {
                if (UnityEngine.Time.realtimeSinceStartup - lastCommandTime < 2f)
                {
                    SendMessage(player, "Cooldown");
                    return;
                }
            }
            commandCooldowns[player.userID] = UnityEngine.Time.realtimeSinceStartup;
                

            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

            if (Zone_Center_Pos == Vector3.zero)
            {   
                SendMessage(player, "NoClaimed");
                return;
            }

            var args = arg.Args;
            string target = args[0];
            string skinTarget = args[1];
            BuildingGrade.Enum currTarget = BuildingGrade.Enum.Twigs;
            ulong currSkinTarget = 0;
            ulong currSkinId = 0;

            switch (target)
            {
                case "twigs":
                    currTarget = BuildingGrade.Enum.Twigs;
                    break;
                case "wood":
                    currTarget = BuildingGrade.Enum.Wood;
                    if (skinTarget == "default")
                        currSkinTarget = 0;
                    else if (skinTarget == "legacy")
                        currSkinTarget = 10232;
                    break;
                case "stone":
                    currTarget = BuildingGrade.Enum.Stone;
                    if (skinTarget == "default")
                        currSkinTarget = 0;
                    else if (skinTarget == "adobe")
                        currSkinTarget = 10220;
                    else if (skinTarget == "bricks")
                        currSkinTarget = 10223;
                    else if (skinTarget == "brutalist")
                        currSkinTarget = 10225;
                    break;
                case "metal":
                    currTarget = BuildingGrade.Enum.Metal;
                    if (skinTarget == "default")
                        currSkinTarget = 0;
                    else if (skinTarget == "shipcontainer")
                        currSkinTarget = 10221;
                    break;
                case "toptier":
                    currTarget = BuildingGrade.Enum.TopTier;
                    break;
            }

            if (currTarget == BuildingGrade.Enum.Metal && currSkinTarget == 10221)
            {
                currSkinId = (ulong)int.Parse(args[2]);
            }

            List<BuildingBlock> blocks = new List<BuildingBlock>();

            foreach (var networkable in BaseNetworkable.serverEntities)
            {
                if (networkable is BuildingBlock buildingBlock)
                {
                    float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                    if (Vector3.Distance(buildingBlock.transform.position, Zone_Center_Pos) <= plot_radius)
                    {
                        blocks.Add(buildingBlock);
                    }
                }
            }

            if (blocks.Count == 0)
            {
                player.ChatMessage("No building blocks found within a 10m radius.");
                return;
            }

            if (!playerSettings.ContainsKey(playerId))
            {
                SendMessage(player, "Player settings not found.");
                return;
            }

            if (playerSettings[player.userID].BuildUpgradeInProgress)
            {
                SendMessage(player, "BuildingInProccess");
                return;
            }

            playerSettings[player.userID].BuildUpgradeInProgress = true;

            int currentIndex = 0;
            Action upgradeAction = null;

            upgradeAction = () =>
            {
                if (currentIndex < blocks.Count)
                {
                    BuildingBlock block = blocks[currentIndex];

                    if (block != null && !block.IsDestroyed)
                    {
                        block.skinID = (uint)currSkinTarget;
                        block.SetGrade(currTarget);
                        block.ChangeGrade(currTarget);
                        block.UpdateSkin();

                        if (currSkinId != 0)
                            block.SetCustomColour((uint)currSkinId);

                        block.SetHealthToMax();
                        block.SendNetworkUpdateImmediate();

                        switch (currTarget)
                        {
                            case BuildingGrade.Enum.Twigs:
                            case BuildingGrade.Enum.Wood:
                                Effect.server.Run("assets/bundled/prefabs/fx/build/frame_place.prefab", block.transform.position);
                                break;
                            case BuildingGrade.Enum.Stone:
                                Effect.server.Run("assets/bundled/prefabs/fx/build/promote_stone.prefab", block.transform.position);
                                break;
                            case BuildingGrade.Enum.Metal:
                                Effect.server.Run("assets/bundled/prefabs/fx/build/promote_metal.prefab", block.transform.position);
                                break;
                            case BuildingGrade.Enum.TopTier:
                                Effect.server.Run("assets/bundled/prefabs/fx/build/promote_toptier.prefab", block.transform.position);
                                break;
                        }
                        currentIndex++;
                    }
                    else
                    {
                        playerSettings[player.userID].BuildUpgradeInProgress = false;
                        timer.Once(0.01f, () =>
                        {
                            upgradeAction = null;
                        });
                    }
                    timer.Once(0.03f, upgradeAction);
                }
                else
                {
                    playerSettings[player.userID].BuildUpgradeInProgress = false;
                    blocks.Clear();
                    // LOG HERE
                }
            };

            upgradeAction();
        }

        [ConsoleCommand("creative.spawn_manager")]
        private void CreativeSpawnManager(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong playerId = player.userID;

            if (player == null || !permission.UserHasPermission(player.UserIDString, "creative.use") || !permission.UserHasPermission(player.UserIDString, "creative.vehicle"))
                return;

            var args = arg.Args;

            switch (args[0])
            {
                case "ClearVehicles":
                    if (permission.UserHasPermission(player.UserIDString, "creative.vehicle"))
                    {
                        KillPlayerSpawnedEntities(player.userID, false);
                        uiVehicleManagerPanel(player);
                    }
                break;

                case "RowBoat":
                    SpawnCar(player, "assets/content/vehicles/boats/rowboat/rowboat.prefab", true);
                break;

                case "Minicopter":
                    SpawnCar(player, "assets/content/vehicles/minicopter/minicopter.entity.prefab");
                break;

                case "ScrapCopter":
                    SpawnCar(player, "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab");
                break;

                case "MagnetCrane":
                    SpawnCar(player, "assets/content/vehicles/crane_magnet/magnetcrane.entity.prefab");
                break;

                case "Chinook":
                    SpawnCar(player, "assets/prefabs/npc/ch47/ch47.entity.prefab");
                break;

                case "AttackHelicopter":
                    SpawnCar(player, "assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab");
                break;

                case "Sedan":
                    SpawnCar(player, "assets/content/vehicles/sedan_a/sedantest.entity.prefab");
                break;

                case "RidableHorse":
                    SpawnCar(player, "assets/rust.ai/nextai/testridablehorse.prefab");
                break;

                case "2ModuleCar":
                    SpawnCar(player, $"{CarReturn(1)}");
                break;

                case "3ModuleCar":
                    SpawnCar(player, $"{CarReturn(2)}");
                break;

                case "4ModuleCar":
                    SpawnCar(player, $"{CarReturn(3)}");
                break;

                case "Snowmobile":
                    SpawnCar(player, "assets/content/vehicles/snowmobiles/snowmobile.prefab");
                break;

                case "Rhib":
                    SpawnCar(player, "assets/content/vehicles/boats/rhib/rhib.prefab", true);
                break;

                case "SoloSubmarine":
                    SpawnCar(player, "assets/content/vehicles/submarine/submarinesolo.entity.prefab", true);
                break;

                case "DuoSubmarine":
                    SpawnCar(player, "assets/content/vehicles/submarine/submarineduo.entity.prefab", true);
                break;

                case "Tugboat":
                    SpawnCar(player, "assets/content/vehicles/boats/tugboat/tugboat.prefab", true);
                break;

                case "HotAirBallon":
                    SpawnCar(player, "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab");
                break;

                case "Bike":
                    SpawnCar(player, "assets/content/vehicles/bikes/pedalbike.prefab");
                break;

                case "MotorBike":
                    SpawnCar(player, "assets/content/vehicles/bikes/motorbike.prefab");
                break;

                case "MotorBikeSideCar":
                    SpawnCar(player, "assets/content/vehicles/bikes/motorbike_sidecar.prefab");
                break;
            }
        }

        private void KillAllSpawnedEntities()
        {
            var playerIds = new List<ulong>(playerEntityCount.Keys);

            foreach (var playerId in playerIds)
            {
                KillPlayerSpawnedEntities(playerId);
            }
        }

        private void KillPlayerSpawnedEntities(ulong playerID, bool remove = true)
        {
            if (playerEntityCount.TryGetValue(playerID, out List<SpawnedEntityInfo> spawnedEntities))
            {
                List<SpawnedEntityInfo> entitiesToRemove = new List<SpawnedEntityInfo>();

                foreach (var entityInfo in spawnedEntities)
                {
                    BaseEntity entity = BaseNetworkable.serverEntities.Find(entityInfo.entityID) as BaseEntity;
                    entity?.Kill();

                    entitiesToRemove.Add(entityInfo);
                }

                foreach (var entityInfo in entitiesToRemove)
                {
                    spawnedEntities.Remove(entityInfo);
                }

                if (remove)
                {
                    playerEntityCount.Remove(playerID);
                }
            }
        }

        private void SpawnCar(BasePlayer player, string Prefab, bool require_water = false)
        {
            if (playerEntityCount.TryGetValue(player.userID, out List<SpawnedEntityInfo> spawnedEntities))
            {
                if (spawnedEntities.Count >= _config.player_vehicle_limit)
                {
                    SendReply(player, $"You have reached the maximum limit of {_config.player_vehicle_limit} spawned entities.");
                    return;
                }
            }
            Vector3 water_position = FindNearestWaterPoint(player.transform.position);
            if (require_water && water_position == null)
            {
                SendReply(player, "No water found nearby to spawn this type of vehicle.");
                return;
            }

            SendReply(player, $"Spawned {spawnedEntities.Count+1}/{_config.player_vehicle_limit} total entities allowed.");

            Vector3 playerPosition = player.transform.position;
            Quaternion playerRotation = player.transform.rotation;
            Vector3 forward = player.eyes.HeadRay().direction;

            float spawnDistance = 5f;
            Vector3 spawnPosition = require_water ? water_position : playerPosition + forward * spawnDistance;

            spawnPosition.y = FindBestHeight(spawnPosition);

            BaseEntity entity = GameManager.server.CreateEntity(Prefab, spawnPosition);
            if (entity != null)
            {
                entity.OwnerID = player.userID;
                entity.Spawn();
                SendReply(player, $"Entity '{entity.ShortPrefabName}' spawned at {spawnPosition}");

                StoreSpawnedEntityInfo(player.userID, entity.net.ID, entity.ShortPrefabName);
            }

            uiVehicleManagerPanel(player);
        }

        private Vector3 FindNearestWaterPoint(Vector3 position)
        {
            float searchRadius = 100f;
            float stepSize = 5f;

            for (float radius = stepSize; radius <= searchRadius; radius += stepSize)
            {
                for (float angle = 0; angle < 360; angle += 10)
                {
                    float radian = angle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(radian), 0, Mathf.Sin(radian)) * radius;
                    Vector3 checkPosition = position + offset;

                    if (IsWater(checkPosition))
                    {
                        return checkPosition;
                    }
                }
            }

            return Vector3.zero;
        }

        private bool IsWater(Vector3 position)
        {
            TerrainTopology.Enum waterMask = TerrainTopology.Enum.Beach;

            return TopologyCheck(waterMask, position, 10f) && !TopologyCheck(TerrainTopology.Enum.Road | TerrainTopology.Enum.Roadside, position, 10f);
        }

        public bool TopologyCheck(TerrainTopology.Enum mask, Vector3 position, float radius)
        {
            int topology = TerrainMeta.TopologyMap.GetTopology(position, radius);
            return (topology & (int)mask) != 0;
        }

        void StoreSpawnedEntityInfo(ulong playerID, NetworkableId entityID, string entityName)
        {
            if (!playerEntityCount.ContainsKey(playerID))
            {
                playerEntityCount[playerID] = new List<SpawnedEntityInfo>();
            }

            playerEntityCount[playerID].Add(new SpawnedEntityInfo(entityID, entityName));
        }

        private System.Random random = new System.Random();
        private string GenerateRandomNumber(int max)
        {
            int randomNumber = random.Next(1, max);
            return randomNumber.ToString("D2");
        }

        private string CarReturn(int type)
        {
            switch(type)
            {
                case 1:
                    return $"assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_{GenerateRandomNumber(8)}.prefab";
                break;

                case 2:
                    return $"assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_{GenerateRandomNumber(9)}.prefab";
                break;

                case 3:
                    return $"assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_{GenerateRandomNumber(11)}.prefab";
                break;
            }

            return "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";
        }

        private object GetGround(Vector3 pos)
        {
            RaycastHit hitInfo;
            pos += new Vector3(0, 100, 0);

            if (Physics.Raycast(pos, Vector3.down, out hitInfo, 200, LayerMask.GetMask("Terrain", "Default")))
                return hitInfo.point;

            return null;
        }

        float FindBestHeight(Vector3 startPos)
        {
            var maxHeight = 0f;

            var foundHeight = GetGround(startPos);

            if (foundHeight != null)
            {
                var height = (Vector3)foundHeight;

                if (height.y > maxHeight)
                    maxHeight = height.y;
            }

            maxHeight += 1f;

            return maxHeight;
        }

        [ConsoleCommand("creative.bgrade")]
        private void BuildGradeCommand(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;
            
            var player = arg?.Player();
            if (!permission.UserHasPermission(player.UserIDString, "creative.use") || args.Length < 1 || !playerSettings.ContainsKey(player.userID))
                return;

            string command = args[0].ToString();
            string command2 = args.Length > 1 ? args[1].ToString() : null;

            if (command == "metal_dlc" && command2 != null && args.Length > 1)
            {
                if (int.TryParse(command2, out int cmd2int))
                {
                    playerSettings[player.userID].CurrentContainerSkin = cmd2int;
                    playerSettings[player.userID].CurrentGrade = 3;
                    playerSettings[player.userID].CurrentSkin = 10221;
                }
            }

            switch(command)
            {
                case "twigs":
                    playerSettings[player.userID].CurrentGrade = 0;
                    playerSettings[player.userID].CurrentContainerSkin = 0;
                    playerSettings[player.userID].CurrentSkin = 0;
                break;

                case "wood":
                    playerSettings[player.userID].CurrentGrade = 1;
                    playerSettings[player.userID].CurrentContainerSkin = 0;
                    playerSettings[player.userID].CurrentSkin = 0;
                break;

                case "stone":
                    playerSettings[player.userID].CurrentGrade = 2;
                    playerSettings[player.userID].CurrentContainerSkin = 0;
                    playerSettings[player.userID].CurrentSkin = 0;
                break;

                case "metal":
                    playerSettings[player.userID].CurrentGrade = 3;
                    playerSettings[player.userID].CurrentContainerSkin = 0;
                    playerSettings[player.userID].CurrentSkin = 0;
                break;

                case "hqm":
                    playerSettings[player.userID].CurrentGrade = 4;
                    playerSettings[player.userID].CurrentContainerSkin = 0;
                    playerSettings[player.userID].CurrentSkin = 0;
                break;

                case "wood_dlc":
                    playerSettings[player.userID].CurrentGrade = 1;
                    playerSettings[player.userID].CurrentSkin = 10232;
                break;

                case "stone_adobe":
                    playerSettings[player.userID].CurrentGrade = 2;
                    playerSettings[player.userID].CurrentSkin = 10220;
                break;

                case "stone_bricks":
                    playerSettings[player.userID].CurrentGrade = 2;
                    playerSettings[player.userID].CurrentSkin = 10223;
                break;

                case "stone_brutalist":
                    playerSettings[player.userID].CurrentGrade = 2;
                    playerSettings[player.userID].CurrentSkin = 10225;
                break;
            }

            uiBgradeHud(player);
        }

        private void UpdateElectricalList(BasePlayer player, string item)
        {
            if (!playerSettings.ContainsKey(player.userID))
                return;

            if (playerSettings[player.userID].ElectricityList.Contains(item))
                playerSettings[player.userID].ElectricityList.Remove(item);
            else
                playerSettings[player.userID].ElectricityList.Add(item);
        }

        [ConsoleCommand("creative.electricity")]
        private void ElectricityList(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;
            
            var player = arg?.Player();
            string command = args[0].ToString();
            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;
            switch(command)
            {
                case "autoturret":
                    UpdateElectricalList(player, "autoturret_deployed");
                break;

                case "samsite":
                    UpdateElectricalList(player, "sam_site_turret_deployed");
                break;

                case "electric.heater":
                    UpdateElectricalList(player, "electrical.heater");
                break;

                case "electric.flasherlight":
                    UpdateElectricalList(player, "electric.flasherlight.deployed");
                break;

                case "ceilinglight":
                    UpdateElectricalList(player, "ceilinglight.deployed");
                break;

                case "electric.furnace":
                    UpdateElectricalList(player, "electricfurnace.deployed");
                break;

                case "industrial.wall.light":
                    UpdateElectricalList(player, "industrial.wall.lamp.red.deployed");
                    UpdateElectricalList(player, "industrial.wall.lamp.green.deployed");
                    UpdateElectricalList(player, "industrial.wall.lamp.deployed");
                break;

                case "sign.neon.":
                    UpdateElectricalList(player, "sign.neon.125x125");
                    UpdateElectricalList(player, "sign.neon.125x215");
                    UpdateElectricalList(player, "sign.neon.xl");
                    UpdateElectricalList(player, "sign.neon.125x215.animated");
                    UpdateElectricalList(player, "sign.neon.xl.animated");
                break;

                case "searchlight":
                    UpdateElectricalList(player, "searchlight.deployed");
                break;
            }

            uiAutoElectricityPanel(player);
        }

        [ConsoleCommand("creative.deployable")]
        private void DeployablesCommands(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;
            
            var player = arg?.Player();
            string command = args[0].ToString();
            if (player == null || !playerSettings.ContainsKey(player.userID) || !permission.UserHasPermission(player.UserIDString, "creative.use"))
                return;

            switch (command)
            {
                case "embrasure_none":
                    playerSettings[player.userID].EmbrasurePrefab = "embrasure_none";
                break;

                case "embrasure_wood":
                    playerSettings[player.userID].EmbrasurePrefab = "assets/prefabs/building/wall.window.shutter/shutter.wood.a.prefab";
                break;

                case "embrasure_vertical":
                    playerSettings[player.userID].EmbrasurePrefab = "assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.b.prefab";
                break;

                case "embrasure_horizontal":
                    playerSettings[player.userID].EmbrasurePrefab = "assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab";
                break;

                case "window_none":
                    playerSettings[player.userID].WindowPrefab = "window_none";
                break;

                case "window_bars_wood":
                    playerSettings[player.userID].WindowPrefab = "assets/prefabs/building/wall.window.bars/wall.window.bars.wood.prefab";
                break;

                case "window_bars_metal":
                    playerSettings[player.userID].WindowPrefab = "assets/prefabs/building/wall.window.bars/wall.window.bars.metal.prefab";
                break;

                case "window_bars_toptier":
                    playerSettings[player.userID].WindowPrefab = "assets/prefabs/building/wall.window.bars/wall.window.bars.toptier.prefab";
                break;

                case "window_glass_reinforced":
                    playerSettings[player.userID].WindowPrefab = "assets/prefabs/building/wall.window.reinforcedglass/wall.window.glass.reinforced.prefab";
                break;

                case "door_menu_none":
                    playerSettings[player.userID].DoorPrefab = "door_none";
                break;

                case "door_menu_wooden":
                    playerSettings[player.userID].DoorPrefab = "assets/prefabs/building/door.hinged/door.hinged.wood.prefab";
                break;

                case "door_menu_sheetmetal":
                    playerSettings[player.userID].DoorPrefab = "assets/prefabs/building/door.hinged/door.hinged.metal.prefab";
                break;

                case "door_menu_hqm":
                    playerSettings[player.userID].DoorPrefab = "assets/prefabs/building/door.hinged/door.hinged.toptier.prefab";
                break;

                case "double_door_menu_garage":
                    playerSettings[player.userID].DoubleDoorPrefab = "assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab";
                break;

                case "double_door_menu_wooden":
                    playerSettings[player.userID].DoubleDoorPrefab = "assets/prefabs/building/door.double.hinged/door.double.hinged.wood.prefab";
                break;

                case "double_door_menu_sheetmetal":
                    playerSettings[player.userID].DoubleDoorPrefab = "assets/prefabs/building/door.double.hinged/door.double.hinged.metal.prefab";
                break;

                case "double_door_menu_hqm":
                    playerSettings[player.userID].DoubleDoorPrefab = "assets/prefabs/building/door.double.hinged/door.double.hinged.toptier.prefab";
                break;
            }

            if (command.Contains("door")) uiAutoDoorsPanel(player); else uiAutoWindowsPanel(player);
        }

        private void NetworkingSwitch(BasePlayer player, List<string> types, string typeName)
        {
            Vector3 Zone_Center_Pos = playerSettings[player.userID].StoredClaimedPlotLocation;

            if (Zone_Center_Pos == Vector3.zero)
                return;

            if (!playerToggledEntities.ContainsKey(player.userID))
            {
                playerToggledEntities[player.userID] = new Dictionary<string, List<BaseEntity>>();
            }

            if (playerToggledEntities[player.userID].ContainsKey(typeName))
            {
                foreach (BaseEntity entity in playerToggledEntities[player.userID][typeName])
                {
                    if (entity == null || entity.IsDestroyed) continue;

                    entity.EnableSaving(true);
                    entity.SetFlag(BaseEntity.Flags.Disabled, false);
                    entity.InvokeRepeating(entity.NetworkPositionTick, 0f, 0.1f);
                    entity.SendNetworkUpdateImmediate();
                    entity.OwnerID = player.userID;

                    if (entityColliders.TryGetValue(entity, out Collider[] colliders))
                    {
                        foreach (var collider in colliders)
                        {
                            collider.enabled = true;
                        }
                        entityColliders.Remove(entity);
                    }
                }

                int reenabledCount = playerToggledEntities[player.userID][typeName].Count;
                player.ChatMessage($"Networking and hitboxes re-enabled for {reenabledCount} entities of type {typeName}.");

                playerToggledEntities[player.userID].Remove(typeName);
            }
            else
            {
                List<BaseEntity> entities = new List<BaseEntity>();
                float plot_radius = permission.UserHasPermission(player.UserIDString, "creative.vip") ? _config.plot_radius_vip : _config.plot_radius_default;
                Vis.Entities(Zone_Center_Pos, plot_radius, entities, Rust.Layers.Solid);

                List<BaseEntity> filteredEntities = new List<BaseEntity>();
                foreach (BaseEntity entity in entities)
                {
                    if (entity == null || entity.IsDestroyed) continue;

                    if (!types.Any(t => entity.PrefabName.Contains(t, StringComparison.OrdinalIgnoreCase))) continue;

                    entity.EnableSaving(false);
                    entity.SetFlag(BaseEntity.Flags.Disabled, true);
                    entity.SendNetworkUpdateImmediate();
                    entity.CancelInvoke(entity.NetworkPositionTick);

                    Collider[] colliders = entity.GetComponentsInChildren<Collider>();
                    if (colliders != null && colliders.Length > 0)
                    {
                        entityColliders[entity] = colliders;
                        foreach (var collider in colliders)
                        {
                            collider.enabled = false;
                        }
                    }

                    filteredEntities.Add(entity);
                   
                }

                playerToggledEntities[player.userID][typeName] = filteredEntities;
                player.ChatMessage($"Networking and hitboxes disabled for {filteredEntities.Count} entities of type {typeName}.");
            }
        }

        [ConsoleCommand("creative.networking")]
        private void NetworkingCommands(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;

            if (arg.Player() == null) 
                return;

            var player = arg?.Player();

            if (player == null || !playerSettings.ContainsKey(player.userID))
                return;

            if (!permission.UserHasPermission(player.UserIDString, "creative.use"))
            {
                SendMessage(player, "NoPerms");
                return;
            }

            string command = args[0].ToString();
            ulong closestZoneID = FindClosestZone(player);

            if (closestZoneID != player.userID)
            {
                SendMessage(player, "NotClaimed");
                return;
            }

            switch (command)
            {
                case "foundations":
                    List<string> foundationList = new List<string> { 
                        "assets/prefabs/building core/foundation/foundation.prefab", 
                        "assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab"
                    };
                    playerSettings[player.userID].NetworkingFoundations = !playerSettings[player.userID].NetworkingFoundations;
                    NetworkingSwitch(player, foundationList, "foundations");
                break;

                case "floor":
                    List<string> floorList = new List<string> { 
                        "assets/prefabs/building core/floor/floor.prefab", 
                        "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                        "assets/prefabs/building core/floor.triangle.frame/floor.triangle.frame.prefab",
                        "assets/prefabs/building core/floor.frame/floor.frame.prefab"
                    };
                    playerSettings[player.userID].NetworkingFloor = !playerSettings[player.userID].NetworkingFloor;
                    NetworkingSwitch(player, floorList, "floor");
                break;

                case "walls":
                    List<string> wallsList = new List<string> { 
                        "assets/prefabs/building core/wall/wall.prefab", 
                        "assets/prefabs/building core/wall.half/wall.half.prefab",
                        "assets/prefabs/building core/wall.low/wall.low.prefab",
                        "assets/prefabs/building core/wall.window/wall.window.prefab",
                        "assets/prefabs/building core/wall.frame/wall.frame.prefab",
                        "assets/prefabs/building core/wall.window/wall.window.prefab"
                    };
                    playerSettings[player.userID].NetworkingWalls = !playerSettings[player.userID].NetworkingWalls;
                    NetworkingSwitch(player, wallsList, "walls");
                break;

                case "others":
                    List<string> otherssList = new List<string> { 
                            "assets/prefabs/building core/ramp/ramp.prefab", 
                            "assets/prefabs/building core/roof.triangle/roof.triangle.prefab",
                            "assets/prefabs/building core/roof.triangle/roof.triangle.prefab",
                            "assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
                            "assets/prefabs/building core/stairs.spiral/block.stair.spiral.prefab",
                            "assets/prefabs/building core/stairs.spiral.triangle/block.stair.spiral.triangle.prefab",
                            "assets/prefabs/building core/stairs.spiral/stairs_spiral.prefab",
                            "assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"
                        };
                    playerSettings[player.userID].NetworkingOthers = !playerSettings[player.userID].NetworkingOthers;
                    NetworkingSwitch(player, otherssList, "others");
                break;

                case "deployables":
                    List<string> deployablelist = new List<string> { 
                            "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", 
                            "assets/prefabs/deployable/bear trap/beartrap.prefab",
                            "assets/prefabs/deployable/landmine/landmine.prefab",
                            "assets/prefabs/deployable/sam_site/sam_site_turret.prefab",
                            "assets/prefabs/npc/flame turret/flameturret.deployed.prefab",
                            "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab",
                            "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab",
                            "assets/prefabs/deployable/furnace/furnace.prefab",
                            "assets/prefabs/deployable/campfire/campfire.prefab",
                            "assets/prefabs/deployable/fridge/fridge.deployed.prefab",
                            "assets/prefabs/deployable/repair bench/repairbench_deployed.prefab",
                            "assets/prefabs/deployable/research table/researchtable_deployed.prefab",
                            "assets/prefabs/deployable/tier 1 workbench/workbench1.deployed.prefab",
                            "assets/prefabs/deployable/tier 2 workbench/workbench2.deployed.prefab",
                            "assets/prefabs/deployable/tier 3 workbench/workbench3.deployed.prefab",
                            "assets/prefabs/building/door.hinged/door.hinged.wood.prefab",
                            "assets/prefabs/building/door.hinged/door.hinged.metal.prefab",
                            "assets/prefabs/building/door.hinged/door.hinged.toptier.prefab",
                            "assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab",
                            "assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab",
                            "assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab",
                            "assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab"
                        };
                    playerSettings[player.userID].NetworkingDeployables = !playerSettings[player.userID].NetworkingDeployables;
                    NetworkingSwitch(player, deployablelist, "deployables");
                break;
            }
        }

        #endregion

        #region MonoBehaviour

        public class PlotManager : MonoBehaviour
        {
            private SphereCollider innerCollider;
            private List<SphereEntity> innerSpheres = Facepunch.Pool.GetList<SphereEntity>();
            public ulong zoneID;
            public Vector3 CenterZone;
            public bool GTFO = true;
            public Dictionary<ulong, List<ulong>> AllowedUsers = new Dictionary<ulong, List<ulong>>();
            private Timer zoneCheckTimer;

            void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                enabled = false;
            }

            void OnDestroy() => DeleteCircle();

            bool KillEntity(BaseEntity entity, bool onexit = false)
            {
                BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
                BasePlayer area_owner = BasePlayer.FindByID(GetOwner());

                if (entity != null && player != null && !entity.ToPlayer())
                {
                    if (onexit && (entity.ShortPrefabName.Contains("rocket") || entity is TimedExplosive))
                        return true;

                    if (entity.OwnerID == GetOwner())
                        return false;
                    
                    if (area_owner != null && area_owner.Team != null &&
                        area_owner.Team.members.Contains(entity.OwnerID))
                        return false;

                    if (Instance.playerSettings[area_owner.userID].Raid)
                        return false;

                    return true;
                }

                return false;
            }

            void OnTriggerEnter(Collider col)
            {
                if (zoneID == 0 || zoneID == 1)
                    return;

                BaseEntity entity = col?.GetComponentInParent<BaseEntity>();
                var player = col?.GetComponentInParent<BasePlayer>();

                if (player != null)
                {
                    if (ShouldTeleportPlayer(player))
                    {
                        if (player.isMounted)
                        {
                            BaseMountable mount = player.GetMounted();

                            if (mount != null)
                            {
                                mount.DismountPlayer(player);
                            }
                        }
                        TeleportPlayerOutsideZone(player);
                    }

                    if (IsPlayerAllowed(player) && Instance.playerSettings.ContainsKey(player.userID))
                        Instance.playerSettings[player.userID].PlayerInArea = true;

                    BasePlayer playerz = BasePlayer.FindByID(zoneID) ?? BasePlayer.FindSleeping(zoneID);
                    Instance.SendMessage(player, "EnterZone", playerz.displayName);
                }

                if (KillEntity(entity))
                    entity.Kill();
            }

            void OnTriggerExit(Collider col)
            {
                if (zoneID == 1 || zoneID == 0 || col == null)
                    return;

                BaseEntity entity = col?.GetComponentInParent<BaseEntity>();
                var player = col?.GetComponentInParent<BasePlayer>();

                if (player != null)
                {
                    if (IsPlayerAllowed(player) && Instance.playerSettings.ContainsKey(player.userID))
                        Instance.playerSettings[player.userID].PlayerInArea = false;

                    BasePlayer playerz = BasePlayer.FindByID(zoneID) ?? BasePlayer.FindSleeping(zoneID);
                    Instance.SendMessage(player, "ExitZone", playerz.displayName);
                }

                if (KillEntity(entity, true))
                    entity.Kill();
            }

            public bool ShouldTeleportPlayer(BasePlayer player)
            {
                if (zoneID == null || player == null)
                    return false;

                if (player.IsAdmin)
                    return false;

                bool isNotOwner = player.userID != zoneID;
                bool isNotInOwnerTeam = player.Team == null || !player.Team.members.Contains(zoneID);
                bool isGtfoEnabled = Instance.playerSettings.ContainsKey(zoneID) && Instance.playerSettings[zoneID].GTFO;

                return isNotOwner && isNotInOwnerTeam && isGtfoEnabled;
            }

            public bool IsPlayerAllowed(BasePlayer player)
            {
                if (zoneID == 0)
                    return false;

                if (player.userID == zoneID || player.IsAdmin || player.Team != null && player.Team.members.Contains(zoneID))
                    return true;

                if (Instance.playerSettings.ContainsKey(zoneID) && !Instance.playerSettings[zoneID].GTFO)
                    return true;

                return false;
            }

            private void TeleportPlayerOutsideZone(BasePlayer player)
            {
                if (Instance.permission.UserHasPermission(player.UserIDString, "creative.admin") && player.IsAdmin)
                    return;

                Vector3 teleportPosition = player.transform.position + (player.transform.forward * 100f);
                player.Teleport(teleportPosition);
                Instance.SendMessage(player, "GTFOmsg");
            }

            public BuildingPrivlidge GetClosestToolCupboard(Vector3 position)
            {
                float closestDistance = float.MaxValue;
                BuildingPrivlidge closestTC = null;

                foreach (var kvp in Instance.activeZones)
                {
                    var existingZone = kvp.Value;
                    float distance = Vector3.Distance(position, existingZone.transform.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTC = existingZone.GetClosestToolCupboard();
                    }
                }

                return closestTC;
            }

            private BuildingPrivlidge GetClosestToolCupboard()
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, GetColliderRadius());

                foreach (var collider in colliders)
                {
                    BuildingPrivlidge tc = collider.GetComponentInParent<BuildingPrivlidge>();
                    if (tc != null)
                    {
                        return tc;
                    }
                }

                return null;
            }

            public void ManageAllowedUsers(BasePlayer player, ulong zoneId)
            {
                if (!AllowedUsers.ContainsKey(zoneId))
                {
                    AllowedUsers[zoneId] = new List<ulong>();
                }

                if (!AllowedUsers[zoneId].Contains(player.userID))
                {
                    AllowedUsers[zoneId].Add(player.userID);
                }

                var team = RelationshipManager.ServerInstance?.FindTeam(player.currentTeam);
                if (team != null)
                {
                    foreach (var memberId in team.members)
                    {
                        if (!AllowedUsers[zoneId].Contains(memberId))
                        {
                            AllowedUsers[zoneId].Add(memberId);
                        }
                    }
                }
            }

            public void CreateBubble(Vector3 position, float initialRadius, BasePlayer player)
            {
                zoneID = player.userID;
                ManageAllowedUsers(player, player.userID);

                transform.position = position;
                transform.rotation = new Quaternion();

                for (int i = 0; i < 7; i++)
                {
                    var sphere = (SphereEntity)GameManager.server.CreateEntity(Instance._config.plot_sphere_prefab, position, new Quaternion(), true);
                    sphere.currentRadius = initialRadius * 2;
                    sphere.lerpSpeed = 0;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    innerSpheres.Add(sphere);
                }

                var innerRB = innerSpheres[0].gameObject.AddComponent<Rigidbody>();
                innerRB.useGravity = false;
                innerRB.isKinematic = true;

                innerCollider = gameObject.AddComponent<SphereCollider>();
                innerCollider.transform.position = innerSpheres[0].transform.position;
                innerCollider.isTrigger = true;
                innerCollider.radius = initialRadius;

                gameObject.SetActive(true);
                enabled = true;
                StartZoneCheck();
            }

            public void DeleteCircle()
            {
                if (zoneID != 0 && Instance.activeZones.ContainsKey(zoneID))
                {
                    
                    foreach (SphereEntity sphere in innerSpheres)
                        sphere.Kill();

                    innerSpheres.Clear();
                    Instance.activeZones.Remove(zoneID);
                    Facepunch.Pool.FreeList(ref innerSpheres);
                }
                StopZoneCheck();
            }

            public float GetColliderRadius()
            {
                return innerCollider != null ? innerCollider.radius : 0f;
            }

            public Vector3 GetZoneCenter()
            {
                return CenterZone;
            }

            public ulong GetOwner()
            {
                return zoneID;
            }

            public void StartZoneCheck()
            {
                zoneCheckTimer?.Destroy();

                zoneCheckTimer = Instance.timer.Every(1f, () =>
                {
                    if (zoneID == 0 || innerCollider == null) return;

                    var colliders = Physics.OverlapSphere(transform.position, innerCollider.radius, LayerMask.GetMask("Player (Server)"));
                    foreach (var col in colliders)
                    {
                        var player = col.GetComponentInParent<BasePlayer>();
                        if (player == null || !player.IsConnected) continue;

                        if (ShouldTeleportPlayer(player))
                        {
                            TeleportPlayerOutsideZone(player);
                        }
                    }
                });
            }

            public void StopZoneCheck()
            {
                zoneCheckTimer?.Destroy();
                zoneCheckTimer = null;
            }
        }

        #endregion


        #region Config

        private Configuration _config;
        private class Configuration
        {
            [JsonProperty(PropertyName = "svar: Disable Terrain Violation Kick")]
            public bool cvar_disable_terrain_violation_kick = true;

            [JsonProperty(PropertyName = "svar: Disable Structure Decay")]
            public bool cvar_disable_decay = true;

            [JsonProperty(PropertyName = "svar: Always Day")]
            public bool cvar_always_day = true;

            [JsonProperty(PropertyName = "svar: Creative - All Users")]
            public bool cvar_creative_allusers = false;

            [JsonProperty(PropertyName = "svar: Creative - FreePlacement")]
            public bool cvar_creative_freeplacement = false;

            [JsonProperty(PropertyName = "svar: Creative - FreeBuild")]
            public bool cvar_creative_freebuild = false;

            [JsonProperty(PropertyName = "svar: Creative - FreeRepair")]
            public bool cvar_creative_freerepair = false;

            [JsonProperty(PropertyName = "Menu Title")]
            public string menu_title = "CREATIVE | SANDBOX | FREE BUILDING";

            [JsonProperty(PropertyName = "Menu Autodoors (true = on | false = off)")]
            public bool menu_autodoors = false;

            [JsonProperty(PropertyName = "Menu Autowindows (true = on | false = off)")]
            public bool menu_autowindows = false;

            [JsonProperty(PropertyName = "Menu Autoelectricity (true = on | false = off)")]
            public bool menu_autoelectricity = false;

            [JsonProperty(PropertyName = "Menu Buildingrade (true = on | false = off)")]
            public bool menu_buildingrade = false;

            [JsonProperty(PropertyName = "Info Menu")]
            public bool info_menu = false;

            [JsonProperty(PropertyName = "Weather Panel")]
            public bool weather_menu = true;

            [JsonProperty(PropertyName = "BuildCost Panel")]
            public bool buildcost_hud = true;

            [JsonProperty(PropertyName = "Lobby Area Position")]
            public Vector3 lobby_pos = Vector3.zero;

            [JsonProperty(PropertyName = "Plot Radius DEFAULT (default: 50f)")]
            public float plot_radius_default = 50f;

            [JsonProperty(PropertyName = "Plot Radius VIP(default: 100f)")]
            public float plot_radius_vip = 100f;

            [JsonProperty(PropertyName = "Saved Bases Slots DEFAULT (default: 8)")]
            public int saved_bases_slots_default = 8;

            [JsonProperty(PropertyName = "Saved Bases Slots VIP (default: 30)")]
            public int saved_bases_slots_vip = 30;

            [JsonProperty(PropertyName = "Plot Clear/Despawn")]
            public bool plot_despawn = true;

            [JsonProperty(PropertyName = "Plot Despawn Time (default: 300f)")]
            public float plot_despawn_time = 300f;

            [JsonProperty(PropertyName = "Plot MapMarker")]
            public bool plot_mapmarker = true;

            [JsonProperty(PropertyName = "Plot Building Upgrade Effect")]
            public bool plot_rpc_effect = true;

            [JsonProperty(PropertyName = "Plot Sphere Prefab")]
            public string plot_sphere_prefab = "assets/prefabs/visualization/sphere.prefab";

            [JsonProperty(PropertyName = "player: Clear Inventory on Disconnect")]
            public bool player_clearinventory = false;

            [JsonProperty(PropertyName = "player: Vehicle Manager Spawn Limit (def: 5)")]
            public int player_vehicle_limit = 5;

            [JsonProperty(PropertyName = "Fly Keybind")]
            public string keybind_noclip = "lighttoggle";

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Keybind: Building Upgrade")]
            public BUTTON keybind_upgrade = BUTTON.FIRE_PRIMARY;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Keybind: Building Downgrade")]
            public BUTTON keybind_downgrade = BUTTON.FIRE_SECONDARY;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Keybind: Display/Close Menu")]
            public BUTTON keybind_menudisplay = BUTTON.FIRE_THIRD;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Keybind: Removal Tool (require hammer in hand)")]
            public BUTTON keybind_removaltool = BUTTON.RELOAD;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Keybind: Change Building Grade")]
            public BUTTON keybind_bgrade = BUTTON.USE;

            [JsonProperty(PropertyName = "Claim Plot Command List")]
            public List<string> CommandList_Claim = new List<string> { "claim", "c" };

            [JsonProperty(PropertyName = "Unclaim Plot Command List")]
            public List<string> CommandList_UnClaim = new List<string> { "unclaim", "u" };

            [JsonProperty(PropertyName = "Fly Command List")]
            public List<string> CommandList_Fly = new List<string> { "noclip", "fly", "f" };

            [JsonProperty(PropertyName = "God Command List")]
            public List<string> CommandList_God = new List<string> { "god", "g" };

            [JsonProperty(PropertyName = "Building Grade Command List")]
            public List<string> CommandList_BGrade = new List<string> { "bgrade", "b" };

            [JsonProperty(PropertyName = "Open Main Menu Command List")]
            public List<string> CommandList_Menu = new List<string> { "menu", "m" };

            [JsonProperty(PropertyName = "Open Weather Menu Command List")]
            public List<string> CommandList_WeatherMenu = new List<string> { "weather", "time" };

            [JsonProperty(PropertyName = "Save Base Command List")]
            public List<string> CommandList_SaveBase = new List<string> { "save", "guardar" };

            [JsonProperty(PropertyName = "Load Base Command List")]
            public List<string> CommandList_LoadBase = new List<string> { "load", "cargar" };

            [JsonProperty(PropertyName = "Info Panel Command List")]
            public List<string> CommandList_InfoPanel = new List<string> { "help", "info" };

            [JsonProperty(PropertyName = "IMGUR CLIENT ID")]
            public string imgur_client_id = "PUT YOUR CLIENT ID HERE!";

            [JsonProperty(PropertyName = "Paste.ee Auth Token (Required for multiple servers)")]
            public string pastee_auth = "PASTE.EE TOKEN HERE";

            [JsonProperty(PropertyName = "ImgBB Api Key (Needed for Base image saving system if imgur does not work.)")]
            public string imgbb_api = "IMGBB API KEY";

            [JsonProperty(PropertyName = "Loading base (building block) delay time (default: 100 milliseconds)")]
            public int base_load_ms = 100;

            [JsonProperty(PropertyName = "Loading base (deployables) delay time (default: 200 milliseconds)")]
            public int deployable_load_ms = 200;

            [JsonProperty(PropertyName = "Log Errors (false by default)")]
            public bool log_errors = false;

            [JsonProperty(PropertyName = "Info Panel - Left Top Panel Text")]
            public string infopanel_left_top = "<b><size=30><color=white>COMMAND LIST </color> </size> </b>   \n <b><color=yellow>/claim - /c</color></b> <color=white>Claim a plot</color>  \n <b><color=yellow>/unclaim - /u</color></b> <color=white>Unclaim current plot</color>  \n <b><color=yellow>/noclip - /fly - /f</color></b> <color=white>Enable/Disable Noclip (FLY)</color>  \n <b><color=yellow>/god - /g</color></b> <color=white>Enable/Disable GodMode</color>  \n <b><color=yellow>/bgrade 0 to 4 - /b 0 to 4</color></b> <color=white>Change the current building grade</color>  \n <b><color=yellow>/menu - /m</color></b> <color=white>Open Main Menu</color>  \n <b><color=yellow>/weather - /time</color></b> <color=white>Allow the user to change the local weather</color>  \n <b><color=yellow>/save base_name - /guardar base_name</color></b> <color=white>Save the current building in the claimed plot.</color>  \n <b><color=yellow>/load base_name - /cargar base_name</color></b> <color=white>Load a saved base in the claimed plot.</color>  \n <b><color=yellow>/help - /info</color></b> <color=white>Open the info menu</color> \n <b><color=yellow>/invite player_name</color></b> <color=white>Invite a player to your team</color>\n <b><color=yellow>/cost</color></b> <color=white>Enable/Disable COST UI</color>";

            [JsonProperty(PropertyName = "Info Panel - Left Down Panel Text")]
            public string infopanel_left_down = "<b><size=30><color=white>KEYBIND LIST </color> </size> </b>  \n <b><color=yellow>Middle Mouse Button</color></b> <color=white>Open/Close Main Menu</color>  \n <b><color=yellow>F Key</color></b> <color=white>Enable/Disable Noclip</color>  \n <b><color=yellow>R Key + Hammer in Hand</color></b> <color=white>Removal Tool</color>  \n <b><color=yellow>SHIFT + Hammmer in Hande + Left Mouse Button</color></b> <color=white>Upgrade Building</color>  \n <b><color=yellow>SHIFT + Hammmer in Hande + Right Mouse Button</color></b> <color=white>Downgrade Building</color>  \n <b><color=yellow>SHIFT + E Key</color></b> <color=white>Change Building Grade</color>";

            [JsonProperty(PropertyName = "Info Panel - Right Top Panel Text")]
            public string infopanel_right_top = "<b><size=30><color=white>HOW TO ADD MORE INFO? </color> </size> </b> \n<b><color=yellow>«b» BOLD TEXT «/b»</color></b> \n<i><color=yellow>«i» ITALIC TEXT «/i»</color></i> \n<b><color=yellow>«color=color here (red/white...)» COLORED TEXT «/color»</color></b> \n<b><color=yellow>«size=size here (1,5,20,34...)» BIGGER/SMALLER TEXT «/size»</color></b> \n";

            [JsonProperty(PropertyName = "Info Panel - Right Down Panel Text")]
            public string infopanel_right_down = "Add more text here if you want... (Modify the config file!)";

            [JsonProperty("MySQL Data")]
            public List<MySqlData> sqldata = new List<MySqlData>();

            [JsonProperty(PropertyName = "Resources: Give Player Resources")]
            public List<string> resourceItems;

            [JsonProperty("ImageData")]
            public List<ImageData> imgdata = new List<ImageData>();

            [JsonProperty("KitData")]
            public List<KitData> kitdata = new List<KitData>();

            [JsonProperty(PropertyName = "Blocked Items")]
            public List<string> blocked_items;

            public static Configuration DefaultConfig()
            {
                return new Configuration()
                {
                    sqldata = new List<MySqlData> 
                    {
                        new MySqlData {
                            server = "SERVER",
                            username = "USERNAME",
                            password = "PASSWORD",
                            database = "DATABASE NAME",
                            port = 3306
                        }
                    },
                    resourceItems = new List<string>() { "wood", "stones", "sulfur", "cloth", "leather", "lowgradefuel", "metal.refined", "gunpowder", "hqm", "metal.fragments", "fat.animal", "skull.human", "gears", "pipes", "stash.small", "grenade.beancan", "propanetank", "roadsigns", "rope", "semibody", "sheetmetal", "tarp", "techparts", "targeting.computer", "cctv.camera", "riflebody", "scrap", "smgbody", "explosives", "metalspring", "electricfuse", "metalblade", "metalpipe", "ladder.wooden.wall", "skull.wolf", "sewingkit" },
                    kitdata = new List<KitData>() { new KitData { id = 200773292, skinid = 3326537103, itemcontainer = ItemContainerType.Belt }, new KitData { id = 237239288, skinid = 2483395996, itemcontainer = ItemContainerType.Wear }, new KitData { id = 1751045826, skinid = 2483392049, itemcontainer = ItemContainerType.Wear } },
                    imgdata = new List<ImageData> { new ImageData { name = "twigs", img = "https://cdn.legacystudio.com.ar/Materials/twigs.png" }, new ImageData { name = "wood", img = "https://cdn.legacystudio.com.ar/Materials/wood.png" }, new ImageData { name = "stones", img = "https://cdn.legacystudio.com.ar/Materials/stones.png" }, new ImageData { name = "metal fragments", img = "https://cdn.legacystudio.com.ar/Materials/metal.fragments.png" }, new ImageData { name = "high quality metal", img = "https://cdn.legacystudio.com.ar/Materials/metal.refined.png" }, new ImageData { name = "wood_dlc", img = "https://cdn.legacystudio.com.ar/Materials/10226.png" }, new ImageData { name = "adobe", img = "https://cdn.legacystudio.com.ar/Materials/10220.png" }, new ImageData { name = "bricks", img = "https://cdn.legacystudio.com.ar/Materials/10223.png" }, new ImageData { name = "brutalist", img = "https://cdn.legacystudio.com.ar/Materials/10225.png" }, new ImageData { name = "ScrapCopter", img = "https://cdn.legacystudio.com.ar/Vehicles/scrap-heli.png" }, new ImageData { name = "MagnetCrane", img = "https://cdn.legacystudio.com.ar/Vehicles/magnet-crane.png" }, new ImageData { name = "Chinook", img = "https://cdn.legacystudio.com.ar/Vehicles/JjRociu.png" }, new ImageData { name = "AttackHelicopter", img = "https://cdn.legacystudio.com.ar/Vehicles/attack-helicopter.png" }, new ImageData { name = "Sedan", img = "https://cdn.legacystudio.com.ar/Vehicles/psn6fvX.png" }, new ImageData { name = "RidableHorse", img = "https://cdn.legacystudio.com.ar/Vehicles/ridable-horse.png" }, new ImageData { name = "2ModuleCar", img = "https://cdn.legacystudio.com.ar/Vehicles/modular-vehicle-2.png" }, new ImageData { name = "3ModuleCar", img = "https://cdn.legacystudio.com.ar/Vehicles/modular-vehicle-3.png" }, new ImageData { name = "4ModuleCar", img = "https://cdn.legacystudio.com.ar/Vehicles/modular-vehicle-4.png" }, new ImageData { name = "Snowmobile", img = "https://cdn.legacystudio.com.ar/Vehicles/snowmobile.png" }, new ImageData { name = "RowBoat", img = "https://cdn.legacystudio.com.ar/Vehicles/rowboat.png" }, new ImageData { name = "Rhib", img = "https://cdn.legacystudio.com.ar/Vehicles/rhib.png" }, new ImageData { name = "SoloSubmarine", img = "https://cdn.legacystudio.com.ar/Vehicles/submarine-solo.png" }, new ImageData { name = "DuoSubmarine", img = "https://cdn.legacystudio.com.ar/Vehicles/submarine-duo.png" }, new ImageData { name = "Tugboat", img = "https://cdn.legacystudio.com.ar/Vehicles/tugboat.png" }, new ImageData { name = "Minicopter", img = "https://cdn.legacystudio.com.ar/Vehicles/minicopter.png" }, new ImageData { name = "HotAirBallon", img = "https://cdn.legacystudio.com.ar/Vehicles/balloon.png" }, new ImageData { name = "Bike", img = "https://cdn.legacystudio.com.ar/Vehicles/bicycle.png" }, new ImageData { name = "MotorBike", img = "https://cdn.legacystudio.com.ar/Vehicles/motorbike.png" }, new ImageData { name = "MotorBikeSideCar", img = "https://cdn.legacystudio.com.ar/Vehicles/motorbike-sidecar.png" }, new ImageData { name = "UpVote", img = "https://cdn.legacystudio.com.ar/Vehicles/rCaZzhq.png" }, new ImageData { name = "DownVote", img = "https://cdn.legacystudio.com.ar/Vehicles/F9I5jQc.png" }, new ImageData { name = "garage_door", img = "https://cdn.legacystudio.com.ar/Misc/wall.frame.garagedoor.png" },new ImageData { name = "heater", img = "https://cdn.legacystudio.com.ar/Misc/heather.png" },new ImageData { name = "flasherlight", img = "https://cdn.legacystudio.com.ar/Misc/flasherlight.png" },new ImageData { name = "electricfurnace", img = "https://cdn.legacystudio.com.ar/Misc/electricfurnace.png" },new ImageData { name = "industrial", img = "https://cdn.legacystudio.com.ar/Misc/industrialwalllight.png" },new ImageData { name = "animatedneon", img = "https://cdn.legacystudio.com.ar/Misc/animatedneon.png" },new ImageData { name = "double_wooden_door", img = "https://cdn.legacystudio.com.ar/Misc/door.double.hinged.wood.png" },new ImageData { name = "double_metal_door", img = "https://cdn.legacystudio.com.ar/Misc/door.double.hinged.metal.png" }, new ImageData { name = "double_hqm_door", img = "https://cdn.legacystudio.com.ar/Misc/door.double.hinged.toptier.png" }, new ImageData { name = "simple_wooden_door", img = "https://cdn.legacystudio.com.ar/Misc/door.hinged.wood.png" }, new ImageData { name = "simple_metal_door", img = "https://cdn.legacystudio.com.ar/Misc/door.hinged.metal.png" }, new ImageData { name = "simple_hqm_door", img = "https://cdn.legacystudio.com.ar/Misc/door.hinged.toptier.png" }, new ImageData { name = "Window_Wooden", img = "https://cdn.legacystudio.com.ar/Misc/wall.window.bars.wood.png" }, new ImageData { name = "Window_Metal", img = "https://cdn.legacystudio.com.ar/Misc/wall.window.bars.metal.png" }, new ImageData { name = "Window_Hqm", img = "https://cdn.legacystudio.com.ar/Misc/wall.window.bars.toptier.png" }, new ImageData { name = "Window_Glass_Reinforced", img = "https://cdn.legacystudio.com.ar/Misc/wall.window.glass.reinforced.png" }, new ImageData { name = "Embrasure_Shutters", img = "https://cdn.legacystudio.com.ar/Misc/shutter.wood.a.png" }, new ImageData { name = "Embrasure_Metal_Vertical", img = "https://cdn.legacystudio.com.ar/Misc/shutter.metal.embrasure.b.png" }, new ImageData { name = "Embrasure_Metal_Horizontal", img = "https://cdn.legacystudio.com.ar/Misc/shutter.metal.embrasure.a.png" }, new ImageData { name = "autoturret", img = "https://carbonmod.gg/assets/media/items/autoturret.png" } },
                    blocked_items = new List<string>() { "firework.boomer.blue", "firework.romancandle.blue", "firework.boomer.champagne", "firework.boomer.green", "firework.romancandle.green", "firework.boomer.orange", "firework.boomer.pattern", "firework.boomer.red", "firework.romancandle.red", "firework.volcano.red", "firework.boomer.violet", "firework.romancandle.violet", "firework.volcano.violet", "firework.volcano", "electric.igniter" }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch (Exception e)
            {
                LogErrors(e.Message, "LoadConfig");
                PrintError("Error reading config, please check!");
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = Configuration.DefaultConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion
    }
}   