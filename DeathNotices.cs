using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core;
using System;
using Oxide.Game.Rust.Cui;


namespace Oxide.Plugins
{
    [Info("DeathNotices", "Stifler", "1.0.0")]
    [Description("Broadcast deaths with many details")]
    class DeathNotices : RustPlugin
    {

     #region Global Declaration

        bool debug = false;
     //   bool killReproducing = false;

        Dictionary<ulong, HitInfo> LastWounded = new Dictionary<ulong, HitInfo>();		
        Dictionary<string, string> reproduceableKills = new Dictionary<string, string>();		
        Dictionary<BasePlayer, Timer> timers = new Dictionary<BasePlayer, Timer>();		
        Dictionary<ulong, PlayerSettings> playerSettings = new Dictionary<ulong, PlayerSettings>();		

        static DeathNotices dn;

     #region Cached Variables

        //DeathHUD
        float AnchorMaxY = 0.97f;

        List<string> killsCache = new List<string>();
        List<Timer> killsTimer = new List<Timer>();
        /////////

        UIColor deathNoticeShadowColor = new UIColor(0.1, 0.1, 0.1, 0.8);
        UIColor deathNoticeColor = new UIColor(0.85, 0.85, 0.85, 0.1);

        List<string> selfInflictedDeaths = new List<string> 
		{ 
            "Cold","Drowned", "Heat", 
			"Suicide", "Generic",
			"Posion", "Radiation", 
			"Thirst", "Hunger", "Fall"			
        };

        List<DeathReason> SleepingDeaths = new List<DeathReason>
        {
            DeathReason.Animal,
            DeathReason.Blunt,
            DeathReason.Bullet,
            DeathReason.Explosion,
            DeathReason.Generic,
            DeathReason.Helicopter,
            DeathReason.Slash,
            DeathReason.Stab,
            DeathReason.Unknown
        };

        List<Regex> regexTags = new List<Regex>
        {
            new Regex(@"<color=.+?>", RegexOptions.Compiled),
            new Regex(@"<size=.+?>", RegexOptions.Compiled)
        };

        List<string> tags = new List<string>
        {
            "</color>",
            "</size>",
            "<i>",
            "</i>",
            "<b>",
            "</b>"
        };
		
//=====================Конфиг=====================//

		// Радиус сообщения
		bool MessageRadiusEnabled;
		float MessageRadius;

		// Где должно появиться сообщение?
		bool LogToFile;
		bool WriteToConsole;
		bool UseSimpleUI = true;

		// Прикрепленные обвесы
		string AttachmentSplit;
		string AttachmentFormatting;

		// Остальные
		string ChatTitle;
		string ChatFormatting;
		string ConsoleFormatting;

		// Цвета            
		string TitleColor;
		string VictimColor;
		string AttackerColor;
		string WeaponColor;
		string AttachmentColor;
		string DistanceColor;
		string BodypartColor;
		string MessageColor;

		// Локализация
		Dictionary<string, object> Names;
		Dictionary<string, object> Bodyparts;
		Dictionary<string, object> Weapons;
		Dictionary<string, object> Attachments;

		// Сообщения
		Dictionary<string, List<string>> Messages;                  

		// Остальные
		bool SimpleUI_StripColors;

		// Масштабирование & Позиционирование
		int SimpleUI_FontSize;

        // Интерфейс UI
		float SimpleUI_Top;
		float SimpleUI_Left;
		float SimpleUI_MaxWidth;
		float SimpleUI_MaxHeight;

		// Таймер
		float SimpleUI_HideTimer;
					
        #endregion
        #endregion        
        #region Classes

        class UIColor
        {
            string color;
			
            public UIColor(double red, double green, double blue, double alpha)
            {
                color = $"{red} {green} {blue} {alpha}";
            }
			
            public override string ToString() => color;
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();
			
            public UIObject()
            {
            }

            string RandomString()
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();
				
                string random = "";
				
                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];
				
                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));
            }

            public void Destroy(BasePlayer player)
            {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(uiName));
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 20, string parent = "HUD/Overlay", int alignmode = 0, float fadeIn = 0f, float fadeOut = 0f  )
            {
                // name = name + RandomString();
                text = text.Replace("\n", "{NEWLINE}");
                string align = "";

                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"fadeOut", fadeOut.ToString()},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align},
                                {"fadeIn", fadeIn.ToString()}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left} {((1 - top) - height)}"},
                                {"anchormax", $"{(left + width)} {(1 - top)}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }

        class PlayerSettings
        {
            public bool ui = true;
            //public bool chat = true;

            public PlayerSettings() { }
			
            internal PlayerSettings(DeathNotices deathnotices)
            {
                ui = dn.UseSimpleUI;
                //chat = dn.WriteToChat;
            }
        }

        class Attacker
        {
            public string name = string.Empty;
            [JsonIgnore]
            public BaseCombatEntity entity;
            public AttackerType type = AttackerType.Invalid;
			
			
            public string TryGetName()
            {
                if (entity == null)
                    return "Зомби";

                if (type == AttackerType.Player)
                    return entity.ToPlayer().displayName;
                if (type == AttackerType.Helicopter)
                    return "Патруль";
                if (type == AttackerType.Turret)
                    return "Турель";
                if (type == AttackerType.Self)
                    return "Сам себя";
                if (type == AttackerType.Animal)
                {
                    if (entity.name.Contains("boar"))
                        return "Кабан";
                    if (entity.name.Contains("horse"))
                        return "Лошадь";
                    if (entity.name.Contains("wolf"))
                        return "Волк";
                    if (entity.name.Contains("stag"))
                        return "Олень";
                    if (entity.name.Contains("chicken"))
                        return "Курица";
                    if (entity.name.Contains("bear"))
                        return "Медведь";
                    if (entity.name.Contains("zombie"))
                        return "Зомби";
                }
                else if (type == AttackerType.Structure)
                {
                    if (entity.name.Contains("barricade.wood.prefab"))
                        return "Деревянной баррикадой";
                    if (entity.name.Contains("barricade.woodwire.prefab"))
                        return "Деревянной баррикадой";
                    if (entity.name.Contains("barricade.metal.prefab"))
                        return "Металлической баррикадой";
                    if (entity.name.Contains("wall.external.high.wood.prefab"))
                        return "Высоким деревянным забором";
                    if (entity.name.Contains("wall.external.high.stone.prefab"))
                        return "Высокой каменной стеной";
                    if (entity.name.Contains("gates.external.high.wood.prefab"))
                        return "Ворота";
                    if (entity.name.Contains("gates.external.high.wood.prefab"))
                        return "Ворота";
                }
                else if (type == AttackerType.Trap)
                {
                    if (entity.name.Contains("beartrap.prefab"))
                        return "Капкан";
                    if (entity.name.Contains("landmine.prefab"))
                        return "Мина";
                    if (entity.name.Contains("spikes.floor.prefab"))
                        return "Деревянные колья";
                }
				
                return "Зомби";
			}

            public AttackerType TryGetType()
            {
                if (entity == null)
                    return AttackerType.Invalid;
                if (entity.ToPlayer() != null)
                    return AttackerType.Player;
                if (entity is BaseHelicopter)// entity.name.Contains("patrolhelicopter.prefab") && !entity.name.Contains("gibs"))
                    return AttackerType.Helicopter;
                if (entity.name.Contains("rust.ai/agents"))
                    return AttackerType.Animal;
                if (entity.name.Contains("barricades/") || entity.name.Contains("wall.external.high"))
                    return AttackerType.Structure;
                if (entity.name.Contains("beartrap.prefab") || entity.name.Contains("landmine.prefab") || entity.name.Contains("spikes.floor.prefab"))
                    return AttackerType.Trap;
                if (entity.name.Contains("autoturret_deployed.prefab"))
                    return AttackerType.Turret;

                return AttackerType.Invalid;
            }
        }

        class Victim
        {
            public string name = string.Empty;
			[JsonIgnore]
            public BaseCombatEntity entity;
            public VictimType type = VictimType.Invalid;

            public string TryGetName()
            {
                if (type == VictimType.Player)
                    return entity.ToPlayer().displayName;
                if (type == VictimType.Helicopter)
                    return "Патруль";
                if (type == VictimType.Animal)
                {
                    if (entity.name.Contains("boar"))
                        return "Кабана";
                    if (entity.name.Contains("horse"))
                        return "Лошадь";
                    if (entity.name.Contains("wolf"))
                        return "Волка";
                    if (entity.name.Contains("stag"))
                        return "Оленя";
                    if (entity.name.Contains("chicken"))
                        return "Курицу";
                    if (entity.name.Contains("bear"))
                        return "Медведя";
                    if (entity.name.Contains("zombie"))
                        return "Зомби";
                }

                return "Зомби";
            }

            public VictimType TryGetType()
            {
                if (entity == null)
                    return VictimType.Invalid;
                if (entity.ToPlayer() != null)
                    return VictimType.Player;
                if (entity.name.Contains("patrolhelicopter.prefab") && entity.name.Contains("gibs"))
                    return VictimType.Helicopter;
                if ((bool)entity?.name?.Contains("rust.ai/agents"))
                    return VictimType.Animal;

                return VictimType.Invalid;
            }
        }

        class DeathData
        {
            public Victim victim = new Victim();
            public Attacker attacker = new Attacker();
            public DeathReason reason = DeathReason.Unknown;
            public string damageType = string.Empty;
            public string weapon = string.Empty;
            public List<string> attachments = new List<string>();
            public string bodypart = string.Empty;
            internal float _distance = -1f;

            public float distance
            {
                get
                {
                    try
                    {
                        if (_distance != -1)
                            return _distance;

                        foreach (string death in dn.selfInflictedDeaths)
                        {
                            if (reason == GetDeathReason(death))
                                attacker.entity = victim.entity;
                        }

                        return victim.entity.Distance(attacker.entity.transform.position);
                    }
                    catch(Exception)
                    {
                        return 0f;
                    }
                }
            }

            public DeathReason TryGetReason()
            {
                if (victim.type == VictimType.Helicopter)
                    return DeathReason.HelicopterDeath;
                else if (attacker.type == AttackerType.Helicopter)
                    return DeathReason.Helicopter;
                else if (attacker.type == AttackerType.Turret)
                    return DeathReason.Turret;
                else if (attacker.type == AttackerType.Trap)
                    return DeathReason.Trap;
                else if (attacker.type == AttackerType.Structure)
                    return DeathReason.Structure;
                else if (attacker.type == AttackerType.Animal)
                    return DeathReason.Animal;
                else if (victim.type == VictimType.Animal)
                    return DeathReason.AnimalDeath;
                else if (weapon == "F1 Grenade" || weapon == "Survey Charge")
                    return DeathReason.Explosion;
                else if (weapon == "Flamethrower")
                    return DeathReason.Flamethrower;
				else if (weapon == "Hunting Bow")
                    return DeathReason.Arrow;
				else if (weapon == "Semi-Automatic Pistol")
                    return DeathReason.Bullet;
				else if (weapon == "Crossbow")
                    return DeathReason.Arrow;
                else if (victim.type == VictimType.Player)
                    return GetDeathReason(damageType);

                return DeathReason.Unknown;
            }

            public DeathReason GetDeathReason(string damage)
            {
                List<DeathReason> Reason = (from DeathReason current in Enum.GetValues(typeof(DeathReason)) where current.ToString() == damage select current).ToList();

                if (Reason.Count == 0)
                    return DeathReason.Unknown;

                return Reason[0];
            }

            [JsonIgnore]
            internal string JSON
            {
                get
                {
                    return JsonConvert.SerializeObject(this, Formatting.Indented);
                }
            }
            
            internal static DeathData Get(object obj)
            {
                JObject jobj = (JObject) obj;
                DeathData data = new DeathData();

                data.bodypart = jobj["bodypart"].ToString();
                data.weapon = jobj["weapon"].ToString();
                data.attachments = (from attachment in jobj["attachments"] select attachment.ToString()).ToList();
                data._distance = Convert.ToSingle(jobj["distance"]);

                /// Victim
                data.victim.name = jobj["victim"]["name"].ToString();

                List<VictimType> victypes = (from VictimType current in Enum.GetValues(typeof(VictimType)) where current.GetHashCode().ToString() == jobj["victim"]["type"].ToString() select current).ToList();

                if (victypes.Count != 0)
                    data.victim.type = victypes[0];

                /// Attacker
                data.attacker.name = jobj["attacker"]["name"].ToString();

                List<AttackerType> attackertypes = (from AttackerType current in Enum.GetValues(typeof(AttackerType)) where current.GetHashCode().ToString() == jobj["attacker"]["type"].ToString() select current).ToList();

                if (attackertypes.Count != 0)
                    data.attacker.type = attackertypes[0];
                
                /// Reason
                List<DeathReason> reasons = (from DeathReason current in Enum.GetValues(typeof(DeathReason)) where current.GetHashCode().ToString() == jobj["reason"].ToString() select current).ToList();
                if (reasons.Count != 0)
                    data.reason = reasons[0];

                return data;
            }
        }

        #endregion
        
        #region Enums / Types

        enum VictimType
        {
            Player,
            Helicopter,
            Animal,
            Invalid
        }

        enum AttackerType
        {
            Player,
            Helicopter,
            Animal,
            Turret,
            Structure,
            Trap,
            Self,
            Invalid
        }

        enum DeathReason
        {
            Turret,
            Helicopter,
            HelicopterDeath,
            Structure,
            Trap,
            Animal,
		    AnimalDeath,
            Generic,
            Hunger,
            Thirst,
            Cold,
            Drowned,
            Heat,
            Bleeding,
            Poison,
            Suicide,
            Bullet,
            Arrow,
            Flamethrower,
            Slash,
            Blunt,
            Fall,
            Radiation,
            Stab,
            Explosion,
            Unknown
        }

        #endregion

        #region Player Settings

        List<string> playerSettingFields
        {
            get
            {
                return (from field in typeof(PlayerSettings).GetFields() select field.Name).ToList();
            }
        }

        List<string> GetSettingValues(BasePlayer player) => (from field in typeof(PlayerSettings).GetFields() select $"{field.Name} : {field.GetValue(playerSettings[player.userID]).ToString().ToLower()}").ToList();

        void SetSettingField<T>(BasePlayer player, string field, T value)
        {
            foreach(var curr in typeof(PlayerSettings).GetFields())
            {
                if (curr.Name == field)
                    curr.SetValue(playerSettings[player.userID], value);
            }
        }

        #endregion

        #region General Plugin Hooks

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("Этот плагин или версия этого плагина не поддерживается данной игрой!");
#endif

            dn = this;

            RegisterPerm("customize");

            LoadConfig();
            LoadData();
            LoadMessages();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (!playerSettings.ContainsKey(player.userID))
                {
                    playerSettings.Add(player.userID, new PlayerSettings(this));

                    SaveData();
                }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создан новый файл конфигурации...");
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!playerSettings.ContainsKey(player.userID))
            {
                playerSettings.Add(player.userID, new PlayerSettings(this));
                SaveData();
            }
        }

        #endregion

        #region Loading

        void LoadData()
        {
            //canRead = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("DeathNotices");

            playerSettings = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerSettings>>("DeathNotices/PlayerSettings");

        }

        void SaveData()
        {
            //Interface.Oxide.DataFileSystem.WriteObject("DeathNotices", canRead);
			
            Interface.Oxide.DataFileSystem.WriteObject("DeathNotices/PlayerSettings", playerSettings);

        }

        void LoadConfig()
        {
			//  Переменные конфигурации
			SetConfig("Общие настройки", "Логирование файлов", false);
            SetConfig("Общие настройки", "Оповещения в консоле", true);
			SetConfig("Общие настройки", "Формат", "[{Title}]: {Message}");
            SetConfig("Общие настройки", "Формат консоли", "{Message}");
            SetConfig("Общие настройки", "Формат для отображения обвесов", " ({attachments})");
			SetConfig("Общие настройки", "Включить радиус сообщений", false);
            SetConfig("Общие настройки", "Радиус сообщений", 300f);           
            SetConfig("Общие настройки", "Цвет полосы UI интерфейса", false);
            SetConfig("Общие настройки", "Интерфейс UI - Размер фона", 16);
            SetConfig("Общие настройки", "Интерфейс UI - Положение вверх", 0.1f);
            SetConfig("Общие настройки", "Интерфейс UI - Положение влево", 0.1f);
            SetConfig("Общие настройки", "Интерфейс UI - Максимальная ширина", 0.8f);
            SetConfig("Общие настройки", "Интерфейс UI - Максимальная высота", 0.05f);
            SetConfig("Общие настройки", "Время UI оповещений (в секундах)", 5f);
            SetConfig("Общие настройки", "Титул", "DeathNotices");            
            SetConfig("Общие настройки", "Разделитель", " | ");
			
			SetConfig("Настройка цвета", "Цвет титула", "#96FFFC");
            SetConfig("Настройка цвета", "Цвет жертвы", "#FFC040");
            SetConfig("Настройка цвета", "Цвет атакующего", "#FFC040");
            SetConfig("Настройка цвета", "Цвет оружия", "#FFFFFF");
            SetConfig("Настройка цвета", "Цвет обвесов", "#FFC040");
            SetConfig("Настройка цвета", "Цвет дистанции", "#FFFFFF");
            SetConfig("Настройка цвета", "Цвет частей тела", "#FFC040");
            SetConfig("Настройка цвета", "Цвет сообщений", "#FFFFFF");            
			
			SetConfig("|Модули", new Dictionary<string, object> { });
			SetConfig("|Названия", new Dictionary<string, object> { });
			SetConfig("Оружие", new Dictionary<string, object> { });
            SetConfig("Части тела", new Dictionary<string, object> { });
            
            // Сообщения о смерти
			SetConfig("|Сообщения", "Arrow", new List<object> { "<size=14>{attacker} застрелил {victim} ({weapon}, {bodypart}, {distance}м.)</size>" });			
            SetConfig("|Сообщения", "Bleeding", new List<object> { "<size=14>{victim} умер от кровотечения</size>" });
			SetConfig("|Сообщения", "Blunt", new List<object> { "<size=14>{attacker} убил {victim} ({weapon})</size>" });
            SetConfig("|Сообщения", "Bullet", new List<object> { "<size=14>{attacker} убил {victim} ({weapon}, {bodypart}, {distance}м.)</size>" });
            SetConfig("|Сообщения", "Flamethrower", new List<object> { "<size=14>{attacker} сжег заживо игрока {victim}</size>" });
            SetConfig("|Сообщения", "Cold", new List<object> { "<size=14>{victim} умер от обморожения</size>" });
            SetConfig("|Сообщения", "Drowned", new List<object> { "<size=14>{victim} утонул</size>" });
            SetConfig("|Сообщения", "Explosion", new List<object> { "<size=14>{attacker} взорвал игрока {victim} орудуя ({weapon})</size>" });
            SetConfig("|Сообщения", "Fall", new List<object> { "<size=14>{victim} разбился</size>" });
            SetConfig("|Сообщения", "Heat", new List<object> { "<size=14>{victim} сгорел</size>" });
            SetConfig("|Сообщения", "Helicopter", new List<object> { "<size=14>{victim} был убит патрульным вертолётом</size>" });
            SetConfig("|Сообщения", "Animal", new List<object> { "<size=14>{attacker} убил {victim}</size>" });
            SetConfig("|Сообщения", "Hunger", new List<object> { "<size=14>{victim} умер от голода</size>" });
            SetConfig("|Сообщения", "Poison", new List<object> { "<size=14>{victim} умер от отравления</size>" });
            SetConfig("|Сообщения", "Radiation", new List<object> { "<size=14>{victim} умер от радиационного отравления</size>" });
            SetConfig("|Сообщения", "Slash", new List<object> { "<size=14>{attacker} зарубил {victim} ({weapon})</size>" });
			SetConfig("|Сообщения", "Stab", new List<object> { "<size=14>{attacker} убил {victim} ({weapon}, {bodypart})</size>" });
            SetConfig("|Сообщения", "Structure", new List<object> { "<size=14>{victim} умер от сближения с {attacker}</size>" });
            SetConfig("|Сообщения", "Suicide", new List<object> { "<size=14>{victim} совершил самоубийство</size>" });
            SetConfig("|Сообщения", "Thirst", new List<object> { "<size=14>{victim} умер от обезвоживания</size>" });
            SetConfig("|Сообщения", "Trap", new List<object> { "<size=14>{victim} попался на ловушку</size>" });
            SetConfig("|Сообщения", "Turret", new List<object> { "<size=14>{victim} был убит автоматической турелью</size>" });
			SetConfig("|Сообщения", "Unknown", new List<object> { "<size=14>У {victim} что-то пошло не так</size>" });
			SetConfig("|Сообщения", "AnimalDeath", new List<object> { "<size=14>{attacker} убил {victim} ({weapon}, {distance}м.)</size>" });
			
			// Сообщения о смерти спящих
			SetConfig("|Сообщения", "Blunt Sleeping", new List<object> { "<size=14>{attacker} убил спящего {victim} ({weapon})</size>" });
			SetConfig("|Сообщения", "Bullet Sleeping", new List<object> { "<size=14>{attacker} убил спящего {victim} ({weapon}, {bodypart}, {distance}м.)</size>" });
			SetConfig("|Сообщения", "Flamethrower Sleeping", new List<object> { "<size=14>{attacker} сжег спящего {victim}</size>" });
			SetConfig("|Сообщения", "Slash Sleeping", new List<object> { "<size=14>{attacker} зарубил спяцего {victim} ({weapon})</size>" });
			SetConfig("|Сообщения", "Stab Sleeping", new List<object> { "<size=14>{attacker} убил спящего {victim} ({weapon})</size>" });
			SetConfig("|Сообщения", "Explosion Sleeping", new List<object> { "<size=14>{attacker} взорвал спяцего {victim} ({weapon})</size>" });

            SaveConfig();
			
            // Переменные конфигурации
			LogToFile = GetConfig(false, "Общие настройки", "Логирование файлов");
            WriteToConsole = GetConfig(true, "Общие настройки", "Оповещения в консоле");
			ChatFormatting = GetConfig("[{Title}]: {Message}", "Общие настройки", "Формат");
            ConsoleFormatting = GetConfig("{Message}", "Общие настройки", "Формат консоли");
            AttachmentFormatting = GetConfig(" ({attachments})", "Общие настройки", "Формат для отображения обвесов");			
            MessageRadiusEnabled = GetConfig(false, "Общие настройки", "Включить радиус сообщений");
            MessageRadius = GetConfig(300f, "Общие настройки", "Радиус сообщений");            
            SimpleUI_StripColors = GetConfig(false, "Общие настройки", "Цвет полосы UI интерфейса");
            SimpleUI_FontSize = GetConfig(16, "Общие настройки", "Интерфейс UI - Размер фона");
            SimpleUI_Top = GetConfig(0.1f, "Общие настройки", "Интерфейс UI - Положение вверх");
            SimpleUI_Left = GetConfig(0.1f, "Общие настройки", "Интерфейс UI - Положение влево");
            SimpleUI_MaxWidth = GetConfig(0.8f, "Общие настройки", "Интерфейс UI - Максимальная ширина");
            SimpleUI_MaxHeight = GetConfig(0.05f, "Общие настройки", "Интерфейс UI - Максимальная высота");
            SimpleUI_HideTimer = GetConfig(5f, "Общие настройки", "Время UI оповещений (в секундах)");
            ChatTitle = GetConfig("DeathNotices", "Общие настройки", "Титул");            
            AttachmentSplit = GetConfig(" | ", "Общие настройки", "Разделитель");
			
			TitleColor = GetConfig("#96FFFC", "Настройка цвета", "Цвет титула");
            VictimColor = GetConfig("#FFC040", "Настройка цвета", "Цвет жертвы");
            AttackerColor = GetConfig("#FFC040", "Настройка цвета", "Цвет атакующего");
            WeaponColor = GetConfig("#FFFFFF", "Настройка цвета", "Цвет оружия");
            AttachmentColor = GetConfig("#FFC040", "Настройка цвета", "Цвет обвесов");
            DistanceColor = GetConfig("#FFFFFF", "Настройка цвета", "Цвет дистанции");
            BodypartColor = GetConfig("#FFC040", "Настройка цвета", "Цвет частей тела");
            MessageColor = GetConfig("#FFFFFF", "Настройка цвета", "Цвет сообщений");
			
			Attachments = GetConfig(new Dictionary<string, object> { }, "|Модули");
			Names = GetConfig(new Dictionary<string, object> { }, "|Названия");
			Weapons = GetConfig(new Dictionary<string, object> { }, "Оружие");
            Bodyparts = GetConfig(new Dictionary<string, object> { }, "Части тела");

            Messages = GetConfig(new Dictionary<string, object>
            {
                // Сообщения о смерти
				{ "Arrow", new List<object> { "<size=14>{attacker} застрелил {victim} ({weapon}, {bodypart}, {distance}м.)</size>" }},
                { "Bleeding", new List<object> { "<size=14>{victim} умер от кровотечения</size>" }},
				{ "Blunt", new List<object> { "<size=14>{attacker} убил {victim} ({weapon})</size>" }},
                { "Bullet", new List<object> { "<size=14>{attacker} убил {victim} ({weapon}, {bodypart}, {distance}м.)</size>" }},
                { "Flamethrower", new List<object> { "<size=14>{attacker} сжег заживо игрока {victim}</size>" }},
                { "Cold", new List<object> { "<size=14>{victim} умер от обморожения</size>" }},
                { "Drowned", new List<object> { "<size=14>{victim} утонул</size>" }},
                { "Explosion", new List<object> { "<size=14>{attacker} взорвал игрока {victim} орудуя ({weapon})</size>" }},
                { "Fall", new List<object> { "<size=14>{victim} разбился</size>" }},
                { "Heat", new List<object> { "<size=14>{victim} сгорел</size>" }},
                { "Helicopter", new List<object> { "<size=14>{victim} был убит патрульным вертолётом</size>" }},
                { "Animal", new List<object> { "<size=14>{attacker} убил {victim}</size>" }},
                { "Hunger", new List<object> { "<size=14>{victim} умер от голода</size>" }},
                { "Poison", new List<object> { "<size=14>{victim} умер от отравления</size>" }},
                { "Radiation", new List<object> { "<size=14>{victim} умер от радиационного отравления</size>" }},
                { "Slash", new List<object> { "<size=14>{attacker} зарубил {victim} ({weapon})</size>" }},
				{ "Stab", new List<object> { "<size=14>{attacker} убил {victim} ({weapon}, {bodypart})</size>" }},
                { "Structure", new List<object> { "<size=14>{victim} умер от сближения с {attacker}</size>" }},
                { "Suicide", new List<object> { "<size=14>{victim} совершил самоубийство</size>" }},
                { "Thirst", new List<object> { "<size=14>{victim} умер от обезвоживания</size>" }},
                { "Trap", new List<object> { "<size=14>{victim} попался на ловушку</size>" }},
                { "Turret", new List<object> { "<size=14>{victim} был убит автоматической турелью</size>" }},
				{ "Unknown", new List<object> { "<size=14>У {victim} что-то пошло не так</size>" }},
				{ "AnimalDeath", new List<object> { "<size=14>{attacker} убил {victim} ({weapon}, {distance}м.)</size>" }},				
				
				// Сообщения о смерти спящих
				{ "Blunt Sleeping", new List<object> { "<size=14>{attacker} убил спящего {victim} ({weapon})</size>" }},
				{ "Bullet Sleeping", new List<object> { "<size=14>{attacker} убил спящего {victim} ({weapon}, {bodypart}, {distance}м.)</size>" }},
				{ "Flamethrower Sleeping", new List<object> { "<size=14>{attacker} сжег спящего {victim}</size>" }},
				{ "Slash Sleeping", new List<object> { "<size=14>{attacker} зарубил спяцего {victim} ({weapon})</size>" }},
				{ "Stab Sleeping", new List<object> { "<size=14>{attacker} убил спящего {victim} ({weapon})</size>" }},
				{ "Explosion Sleeping", new List<object> { "<size=14>{attacker} взорвал спяцего {victim} ({weapon})</size>" }},
				
            }, "|Сообщения").ToDictionary(l => l.Key, l => ((List<object>)l.Value).ConvertAll(m => m.ToString()));
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "У вас нет разрешения на использование этой команды."},
                {"Hidden", "Сообщения о смерти отключены."},
                {"Unhidden", "Сообщения о смерти включены."},
                {"Field Not Found", "Поле не может быть найдено!"},
                {"True Or False", "{arg} должно быть 'true' или 'false'!"},
                {"Field Set", "Поле '{field}' установлен в '{value}'"}
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("deaths")]
        void cmdDeaths(BasePlayer player, string cmd, string[] args)
        {
            if(!HasPerm(player.userID, "customize"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendChatMessage(player, "/deaths set <field> <value> - set a value");
                SendChatMessage(player, "Fields", Environment.NewLine + ListToString(GetSettingValues(player), 0, Environment.NewLine));

                return;
            }

            switch(args[0].ToLower())
            {
                case "set":
                    if(args.Length != 3)
                    {
                        SendChatMessage(player, "Syntax: /deaths set <field> <value>");
                        return;
                    }

                    if(!playerSettingFields.Contains(args[1].ToLower()))
                    {
                        SendChatMessage(player, GetMsg("Field Not Found", player.userID));
                        return;
                    }
                    
                    bool value = false;

                    try
                    {
                        value = Convert.ToBoolean(args[2]);
                    }
                    catch(FormatException)
                    {
                        SendChatMessage(player, GetMsg("True Or False", player.userID).Replace("{arg}", "<value>"));
                        return;
                    }

                    SetSettingField(player, args[1].ToLower(), value);

                    SendChatMessage(player, GetMsg("Field Set", player.userID).Replace("{value}", value.ToString().ToLower()).Replace("{field}", args[1].ToLower()));

                    SaveData();

                    break;

                default:
                    SendChatMessage(player, "/deaths set <field> <value> - set a value");
                    SendChatMessage(player, "Fields", Environment.NewLine + ListToString(GetSettingValues(player), 0, Environment.NewLine));
                    break;
            }
        }

        [ConsoleCommand("reproducekill")]
        void ccmdReproduceKill(ConsoleSystem.Arg arg)
        {
            bool hasPerm = false;

            if (arg?.Connection == null)
                hasPerm = true;
            else
            {
                if((BasePlayer)arg.Connection.player != null)
                {
                    if (HasPerm(arg.Connection.userid, "reproduce"))
                        hasPerm = true;
                }
            }
            
            if (hasPerm)
            {
                if (arg.Args == null || arg.Args.Length != 1)
                {
                    arg.ReplyWith("Syntax: reproducekill <datetime>");
                    return;
                }
                
                if(reproduceableKills.ContainsKey(arg.Args[0]))
                {
                    DeathData data = DeathData.Get(JsonConvert.DeserializeObject(reproduceableKills[arg.Args[0]]));
                    PrintWarning("Reproduced Kill: " + Environment.NewLine + data.JSON);

                    if (data == null)
                        return;

                    NoticeDeath(data, true);
                    arg.ReplyWith("Death reproduced!");
                }
                else
                    arg.ReplyWith("No saved kill at that time found!");
            }
        }

        #endregion

        #region Death Related

        //DeathHUD
        // Внимание! 10 строк макс вмешается в 16x9 а мож в другие соотн
        // не зн поч так, раньше отображалось и 12 строк
        // надо выбрать правильные размеры, убрать все кроме головы и тд. щас не вмешается все иногда + проверить с макс никами
        void AddDeath(string text)
        {
            killsCache.Add(text);
            killsTimer.Add(timer.Once(25f, () => removeLine()));

            if (killsCache.Count > 10)
            {
                killsTimer[0].Destroy();
                removeLine();
            }
            else
            {
                ShowDeathHud();
            }          
        }

        void ShowDeathHud()
        {            
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement()
            {
                Name = "DeathHUD",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = string.Join("\n", killsCache.ToArray()),
                        FontSize = 12,
                        //Font = "robotocondensed-regular.ttf",
                        Align = UnityEngine.TextAnchor.UpperRight
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.4 " + (AnchorMaxY - (0.03f * killsCache.Count)),
                        AnchorMax = "0.99 " + AnchorMaxY
                    },
                    new CuiOutlineComponent()
                    {
                        Color = "0 0 0 1",
                        Distance = "0.5 -0.5"
                    }
                }
            });

            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "DeathHUD");
                CuiHelper.AddUi(player, container);
            }
        }

        void removeLine()
        {
            killsCache.RemoveAt(0);
            killsTimer.RemoveAt(0);

            if (killsCache.Count < 1)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "DeathHUD");
                }

                return;
            }

            ShowDeathHud();
        }
        //конец DeathHUD

        HitInfo TryGetLastWounded(ulong uid, HitInfo info)
        {
            if (LastWounded.ContainsKey(uid))
            {
                HitInfo output = LastWounded[uid];
                LastWounded.Remove(uid);
                return output;
            }

            return info;
        }

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if(victim?.ToPlayer() != null && info?.Initiator?.ToPlayer() != null)
            {
                NextTick(() => 
                {
                    if (victim.ToPlayer().IsWounded())
                        LastWounded[victim.ToPlayer().userID] = info;
                });
            }
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null)
                return;

            if(victim.ToPlayer() != null)
            {
                if (victim.ToPlayer().IsWounded())
                    info = TryGetLastWounded(victim.ToPlayer().userID, info);
            }
            if (victim as BaseNpc != null && info?.Initiator as BaseNpc != null) return;
            if (victim as BaseCorpse != null) return;
			if (info?.Initiator?.ToPlayer() == null && (victim?.name?.Contains("autospawn") ?? false))
                return;

            DeathData data = new DeathData();
            data.victim.entity = victim;
            data.victim.type = data.victim.TryGetType();

            if (data.victim.type == VictimType.Invalid)
                return;

            data.victim.name = data.victim.TryGetName();
            
			if (info?.Initiator != null)
				
                data.attacker.entity = info?.Initiator as BaseCombatEntity;
				
            
            else
                data.attacker.entity = victim.lastAttacker as BaseCombatEntity;

            data.attacker.type = data.attacker.TryGetType();
            data.attacker.name = StripTags(data.attacker.TryGetName());
            data.weapon = info?.Weapon?.GetItem()?.info?.displayName?.english ?? FormatThrownWeapon(info?.WeaponPrefab?.name ?? "No Weapon");
            data.attachments = GetAttachments(info);
            data.damageType = FirstUpper(victim.lastDamage.ToString());

            if(data.weapon == "Heli Rocket")
            {
                data.attacker.name = "Patrol Helicopter";
                data.reason = DeathReason.Helicopter;
            }

            if (info?.HitBone != null)
                data.bodypart = FirstUpper(GetBoneName(victim, info.HitBone) ?? string.Empty);
            else
                data.bodypart = FirstUpper("Body") ?? string.Empty;

            data.reason = data.TryGetReason();

            if (!(bool)(plugins.CallHook("OnDeathNotice", JObject.FromObject(data)) ?? true))
                return;

            NoticeDeath(data);
        }

        void NoticeDeath(DeathData data, bool reproduced = false)
        {
            DeathData newData = UpdateData(data);

            if (string.IsNullOrEmpty(GetDeathMessage(newData, false)))
                return;

            AddDeath(GetDeathMessage(newData, true));
           // foreach (BasePlayer player in BasePlayer.activePlayerList)
           // {                
              //  if (InRadius(player, data.attacker.entity))
               // {
                  //  if (CanSee(player, "ui"))
                        //Новый элемент для DeathHUD
                    //    AddDeath(GetDeathMessage(newData, true));
               // }
          //  }

            if (WriteToConsole)
                Puts(StripTags(GetDeathMessage(newData, true)));

            if (LogToFile)
                //ConVar.Server.Log("oxide/logs/Kills.txt", StripTags(GetDeathMessage(newData, true)));
                LogToFile("kills", StripTags(GetDeathMessage(newData, true)), this);

            if (debug)
            {
                PrintWarning("DATA: " + Environment.NewLine + data.JSON);
                PrintWarning("UPDATED DATA: " + Environment.NewLine + newData.JSON);
            }
        }

        #endregion

        #region Formatting

        string FormatThrownWeapon(string unformatted)
        {
            if (unformatted == string.Empty)
                return string.Empty;

            string formatted = FirstUpper(unformatted.Split('/').Last().Replace(".prefab", "").Replace(".entity", "").Replace(".weapon", "").Replace(".deployed", "").Replace("_", " ").Replace(".", ""));

            if (formatted == "Stonehatchet")
                formatted = "Stone Hatchet";
            else if (formatted == "Knife Bone")
                formatted = "Bone Knife";
            else if (formatted == "Spear Wooden")
                formatted = "Wooden Spear";
            else if (formatted == "Spear Stone")
                formatted = "Stone Spear";
            else if (formatted == "Icepick Salvaged")
                formatted = "Salvaged Icepick";
            else if (formatted == "Axe Salvaged")
                formatted = "Salvaged Axe";
            else if (formatted == "Hammer Salvaged")
                formatted = "Salvaged Hammer";
            else if (formatted == "Grenadef1")
                formatted = "F1 Grenade";
            else if (formatted == "Grenadebeancan")
                formatted = "Beancan Grenade";
            else if (formatted == "Explosivetimed")
                formatted = "Timed Explosive";

            return formatted;
        }

        string StripTags(string original)
        {
            foreach (string tag in tags)
                original = original.Replace(tag, "");

            foreach (Regex regexTag in regexTags)
                original = regexTag.Replace(original, "");

            return original;
        }

        string FirstUpper(string original)
        {
            if (original == string.Empty)
                return string.Empty;

            List<string> output = new List<string>();
            foreach (string word in original.Split(' '))
                output.Add(word.Substring(0, 1).ToUpper() + word.Substring(1, word.Length - 1));

            return ListToString(output, 0, " ");
        }

        #endregion

        #region Death Variables Methods

        List<string> GetMessages(string reason) => Messages.ContainsKey(reason) ? Messages[reason] : new List<string>();
        
        List<string> GetAttachments(HitInfo info)
        {
            List<string> attachments = new List<string>();

            if (info?.Weapon?.GetItem()?.contents?.itemList != null)
            {
                foreach (var content in info.Weapon.GetItem().contents.itemList)
                {
                    attachments.Add(content?.info?.displayName?.english);
                }
            }

            return attachments;
        }

        string GetBoneName(BaseCombatEntity entity, uint boneId) => entity?.skeletonProperties?.FindBone(boneId)?.name?.english ?? "Body";

        bool InRadius(BasePlayer player, BaseCombatEntity attacker)
        {
            if (MessageRadiusEnabled)
            {
                try
                {
                    if (player.Distance(attacker) <= MessageRadius)
                        return true;
                    else
                        return false;
                }
                catch(Exception)
                {
                    return false;
                }
            }

            return true;
        }

        string GetDeathMessage(DeathData data, bool console)
        {
            string message = string.Empty;
            string reason = string.Empty;
            List<string> messages = new List<string>();

            if (data.victim.type == VictimType.Player && data.victim.entity?.ToPlayer() != null && data.victim.entity.ToPlayer().IsSleeping())
            {
                if(SleepingDeaths.Contains(data.reason))
                {
                    reason = data.reason + " Sleeping";
                }
                else
                    reason = data.reason.ToString();
            }
            else
                reason = data.reason.ToString();

            try
            {
                messages = GetMessages(reason);
            }
            catch (InvalidCastException)
            {
            }

            if (messages.Count == 0)
                return message;

            string attachmentsString = data.attachments.Count == 0 ? string.Empty : AttachmentFormatting.Replace("{attachments}", ListToString(data.attachments, 0, AttachmentSplit));

            if (console)
                message = ConsoleFormatting.Replace("{Title}", $"<color={TitleColor}>{ChatTitle}</color>").Replace("{Message}", $"<color={MessageColor}>{messages.GetRandom()}</color>");
            else
               message = ChatFormatting.Replace("{Title}", $"<color={TitleColor}>{ChatTitle}</color>").Replace("{Message}", $"<color={MessageColor}>{messages.GetRandom()}</color>");
            
			message = message.Replace("{attacker}", $"<color={AttackerColor}>{data.attacker.name}</color>");
            message = message.Replace("{victim}", $"<color={VictimColor}>{data.victim.name}</color>");
            message = message.Replace("{distance}", $"<color={DistanceColor}>{Math.Round(data.distance, 2)}</color>");
            message = message.Replace("{weapon}", $"<color={WeaponColor}>{data.weapon}</color>");
            message = message.Replace("{bodypart}", $"<color={BodypartColor}>{data.bodypart}</color>");
            message = message.Replace("{attachments}", $"<color={AttachmentColor}>{attachmentsString}</color>");

            return message;
        }

        DeathData UpdateData(DeathData data)
        {
            bool configUpdated = false;

            if (data.victim.type != VictimType.Player)
            {
                if (Config.Get("|Названия", data.victim.name) == null)
                {
                    SetConfig("|Названия", data.victim.name, data.victim.name);
                    configUpdated = true;
                }
                else
                    data.victim.name = GetConfig(data.victim.name, "|Названия", data.victim.name);
            }

            if (data.attacker.type != AttackerType.Player)
            {
                if (Config.Get("|Названия", data.attacker.name) == null)
                {
                    SetConfig("|Названия", data.attacker.name, data.attacker.name);
                    configUpdated = true;
                }
                else
                    data.attacker.name = GetConfig(data.attacker.name, "|Названия", data.attacker.name);
            }

            if (Config.Get("Части тела", data.bodypart) == null)
            {
                SetConfig("Части тела", data.bodypart, data.bodypart);
                configUpdated = true;
            }
            else
                data.bodypart = GetConfig(data.bodypart, "Части тела", data.bodypart);

            if (Config.Get("Оружие", data.weapon) == null)
            {
                SetConfig("Оружие", data.weapon, data.weapon);
                configUpdated = true;
            }
            else
                data.weapon = GetConfig(data.weapon, "Оружие", data.weapon);

            string[] attachmentsCopy = new string[data.attachments.Count];
            data.attachments.CopyTo(attachmentsCopy);

            foreach (string attachment in attachmentsCopy)
            {
                if (Config.Get("|Модули", attachment) == null)
                {
                    SetConfig("|Модули", attachment, attachment);
                    configUpdated = true;
                }
                else
                {
                    data.attachments.Remove(attachment);
                    data.attachments.Add(GetConfig(attachment, "|Модули", attachment));
                }
            }

            if (configUpdated)
                SaveConfig();

            return data;
        }

        bool CanSee(BasePlayer player, string type)
        {
            if (type == "ui")
            {
                if (HasPerm(player.userID, "customize"))
                    return playerSettings.ContainsKey(player.userID) ? playerSettings[player.userID].ui : true;
                else
                    return UseSimpleUI;
            }

            return false;
        }

        #endregion

        #region Converting

        string ListToString(List<string> list, int first, string seperator) => string.Join(seperator, list.Skip(first).ToArray());

        #endregion

        #region Config and Message Handling

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

        #endregion

        #region Permission Handling

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            uid = uid.ToString();
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        #endregion

        #region Messages

       void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

       void SendChatMessage(BasePlayer player, string prefix, string msg = null, object uid = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg, null, uid?.ToString() ?? "0");

        void UIMessage(BasePlayer player, string message)
        {
            bool replaced = false;
            float fadeIn = 0.2f;

            Timer playerTimer;

            timers.TryGetValue(player, out playerTimer);

            if (playerTimer != null && !playerTimer.Destroyed)
            {
                playerTimer.Destroy();
                fadeIn = 0.1f;

                replaced = true;
            }

            UIObject ui = new UIObject();
			
            //Изменен hud HUD/Overlay для DeathHUD
            ui.AddText("DeathNotice_DropShadow", SimpleUI_Left + 0.001, SimpleUI_Top + 0.001, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeShadowColor, StripTags(message), SimpleUI_FontSize, "HUD/Overlay", 3, fadeIn, 0.2f);
            ui.AddText("DeathNotice", SimpleUI_Left, SimpleUI_Top, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeColor, message, SimpleUI_FontSize, "HUD/Overlay", 3, fadeIn, 0.2f);

            ui.Destroy(player);

            if(replaced)
            {
                timer.Once(0.1f, () =>
                {
                    ui.Draw(player);

                    timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
                });
            }
            else
            {
                ui.Draw(player);

                timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
            }
        }

        #endregion
    }
}
