using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;


namespace Oxide.Plugins
{
    [Info("Death Messages", "Skrip|Tal", "2.1.55")]
    class DeathMessages : RustPlugin
    {
        private static PluginConfig _config;
        private string version = "2.1.55";
        private List<DeathMessage> _notes = new List<DeathMessage>();
        private Dictionary<ulong, HitInfo> _lastHits = new Dictionary<ulong, HitInfo>();

        #region Classes / Enums

        class PluginConfig
        {

            [JsonProperty("A. Время показа сообщения (сек)")]
            public int Cooldown { get; set; }
            [JsonProperty("B. Размер текста")]
            public int FontSize { get; set; }
            [JsonProperty("C. Показывать убиства животных")]
            public bool ShowDeathAnimals { get; set; }
            [JsonProperty("D. Показывать убийства спящих")]
            public bool ShowDeathSleepers { get; set; }
            [JsonProperty("E. Хранение логов")]
            public bool Log { get; set; }
            [JsonProperty("F. Цвет атакующего")]
            public string ColorAttacker { get; set; }
            [JsonProperty("G. Цвет убитого")]
            public string ColorVictim { get; set; }
            [JsonProperty("H. Цвет оружия")]
            public string ColorWeapon { get; set; }
            [JsonProperty("I. Цвет дистанции")]
            public string ColorDistance { get; set; }
            [JsonProperty("J. Цвет части тела")]
            public string ColorBodyPart { get; set; }
            [JsonProperty("K. Дистанция")]
            public double Distance { get; set; }
            [JsonProperty("L. Название вертолета")]
            public string HelicopterName { get; set; }
            [JsonProperty("M. Название Bradlay (Танк)")]
            public string BradleyAPCName { get; set; }
            [JsonProperty("N. Имя NPC")]
            public string NPCName { get; set; }
            [JsonProperty("O. Имя Zombie")]
            public string ZombieName { get; set; }

            [JsonProperty("Оружие")]
            public Dictionary<string, string> Weapons { get; set; }
            [JsonProperty("Конструкции")]
            public Dictionary<string, string> Structures { get; set; }
            [JsonProperty("Ловушки")]
            public Dictionary<string, string> Traps { get; set; }
            [JsonProperty("Турели")]
            public Dictionary<string, string> Turrets { get; set; }
            [JsonProperty("Животные")]
            public Dictionary<string, string> Animals { get; set; }
            [JsonProperty("Сообщения")]
            public Dictionary<string, string> Messages { get; set; }
            [JsonProperty("Части тела")]
            public Dictionary<string, string> BodyParts { get; set; }
        }

        enum AttackerType
        {
            Player,
            Helicopter,
            Animal,
            Turret,
            Guntrap,
            Structure,
            Trap,
            Invalid,
            NPC,
            BradleyAPC,
            Zombie,
            ZombieDeath
        }

        enum VictimType
        {
            Player,
            Helicopter,
            Animal,
            Invalid,
            NPC,
            BradleyAPC,
            Zombie,
            ZombieDeath
        }

        enum DeathReason
        {
            Turret,
            Guntrap,
            Helicopter,
            HelicopterDeath,
            BradleyAPC,
            BradleyAPCDeath,
            Structure,
            Trap,
            Animal,
            AnimalDeath,
            Generic,
            Zombie,
            ZombieDeath,
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

        class Attacker
        {
            public Attacker(BaseEntity entity)
            {
                Entity = entity;
                Type = InitializeType();
                Name = InitializeName();
            }

            public BaseEntity Entity { get; }

            public string Name { get; }

            public AttackerType Type { get; }

            private AttackerType InitializeType()
            {
                if (Entity == null)
                    return AttackerType.Invalid;

                if (Entity.name.Contains("machete.weapon"))
                    return AttackerType.Zombie;

                if (Entity is NPCMurderer)
                    return AttackerType.ZombieDeath;

                if (Entity is NPCPlayer)
                    return AttackerType.NPC;            

                if (Entity is BasePlayer)
                    return AttackerType.Player;

                if (Entity is BaseHelicopter)   
                    return AttackerType.Helicopter;				

                if (Entity is BradleyAPC)                
                    return AttackerType.BradleyAPC;

                if (Entity.name.Contains("agents/"))
                    return AttackerType.Animal;

                if (Entity.name.Contains("barricades/") || Entity.name.Contains("wall.external.high"))
                    return AttackerType.Structure;

                if (Entity.name.Contains("beartrap.prefab") || Entity.name.Contains("landmine.prefab") || Entity.name.Contains("spikes.floor.prefab"))
                    return AttackerType.Trap;

                if (Entity.name.Contains("autoturret_deployed.prefab") || Entity.name.Contains("flameturret.deployed.prefab"))
                    return AttackerType.Turret;
                if (Entity.name.Contains("guntrap_deployed.prefab") || Entity.name.Contains("guntrap.deployed.prefab"))
                    return AttackerType.Guntrap;

                return AttackerType.Invalid;
            }

            private string InitializeName()
            {

                if (Entity == null)
                    return null;

                switch (Type)
                {


                    case AttackerType.Player:
                        return Entity.ToPlayer().displayName;

                    case AttackerType.NPC:
                        return string.IsNullOrEmpty(Entity.ToPlayer()?.displayName) ? _config.NPCName : Entity.ToPlayer()?.displayName;

                    case AttackerType.Helicopter:
                        return "Patrol Helicopter";
						
                    case AttackerType.BradleyAPC:  
                    case AttackerType.Turret: 
                    case AttackerType.Guntrap:
                    case AttackerType.Trap:
                    case AttackerType.Animal:
                    case AttackerType.Structure:
                        return FormatName(Entity.name);
                }

                return string.Empty;
            }
        }

        class Victim
        {
            public Victim(BaseCombatEntity entity)
            {
                Entity = entity;
                Type = InitializeType();
                Name = InitializeName();
            }

            public BaseCombatEntity Entity { get; }

            public string Name { get; }

            public VictimType Type { get; }

            private VictimType InitializeType()
            {
                if (Entity is NPCMurderer)
                    return VictimType.Zombie;

                if (Entity.name.Contains("machete.weapon"))
                    return VictimType.Zombie;

                if (Entity == null)
                    return VictimType.Invalid;


                if (Entity is NPCPlayer)
                    return VictimType.NPC;

                if (Entity is BasePlayer)
                    return VictimType.Player;

                if (Entity is BaseHelicopter)
                    return VictimType.Helicopter;				

                if (Entity is BradleyAPC)
                    return VictimType.BradleyAPC;

                if (Entity.name.Contains("agents/"))
                    return VictimType.Animal;

                return VictimType.Invalid;
            }

            private string InitializeName()
            {
                switch (Type)
                {
                    case VictimType.Zombie:
                        return "ZombieName";

                    case VictimType.Player:
                        return Entity.ToPlayer().displayName;


                    case VictimType.NPC:
                        return string.IsNullOrEmpty(Entity.ToPlayer()?.displayName) ? _config.NPCName : Entity.ToPlayer()?.displayName;

                    case VictimType.Helicopter:
                        return "Patrol Helicopter";						

                    case VictimType.BradleyAPC:
                        return "BradleyAPCName";   

                    case VictimType.Animal:
                        return FormatName(Entity.name);
                }

                return string.Empty;
            }
        }

        class DeathMessage
        {
            public DeathMessage(Attacker attacker, Victim victim, string weapon, string damageType, string bodyPart, double distance)
            {
                Attacker = attacker;
                Victim = victim;
                Weapon = weapon;
                DamageType = damageType;
                BodyPart = bodyPart;
                Distance = distance;

                Reason = InitializeReason();
                Message = InitializeDeathMessage();

                if (_config.Distance <= 0)
                {
                    Players = BasePlayer.activePlayerList;
                }
                else
                {
                    var position = attacker?.Entity?.transform?.position;
                    if (position == null)
                        position = victim?.Entity?.transform?.position;

                    if (position != null)
                        Players = BasePlayer.activePlayerList.Where(x => x.Distance((UnityEngine.Vector3)position) <= _config.Distance).ToList();
                    else
                        Players = new List<BasePlayer>();
                }

                if (victim.Type == VictimType.Player && !Players.Contains(victim.Entity.ToPlayer()))
                    Players.Add(victim.Entity.ToPlayer());

                if (attacker.Type == AttackerType.Player && !Players.Contains(attacker.Entity.ToPlayer()))
                    Players.Add(attacker.Entity.ToPlayer());
            }

            public List<BasePlayer> Players { get; }

            public Attacker Attacker { get; }

            public Victim Victim { get; }

            public string Weapon { get; }

            public string BodyPart { get; }

            public string DamageType { get; }

            public double Distance { get; }

            public DeathReason Reason { get; }

            public string Message { get; }

            private DeathReason InitializeReason()
            {

                if (Attacker.Type == AttackerType.Turret)
                    return DeathReason.Turret;

                if (Attacker.Type == AttackerType.Guntrap)
                    return DeathReason.Guntrap;

                if (Attacker.Type == AttackerType.Zombie)
                    return DeathReason.Zombie;

                else if (Attacker.Type == AttackerType.Helicopter)
                    return DeathReason.Helicopter;

                else if (Attacker.Type == AttackerType.BradleyAPC)  
                    return DeathReason.BradleyAPC;

                else if (Victim.Type == VictimType.Helicopter)
                    return DeathReason.HelicopterDeath;

                else if (Victim.Type == VictimType.BradleyAPC)
                    return DeathReason.BradleyAPCDeath;

                else if (Attacker.Type == AttackerType.Structure)
                    return DeathReason.Structure;

                else if (Attacker.Type == AttackerType.Trap)
                    return DeathReason.Trap;

                else if (Attacker.Type == AttackerType.Animal)
                    return DeathReason.Animal;

                else if (Victim.Type == VictimType.Animal)
                    return DeathReason.AnimalDeath;

                else if (Weapon == "F1 Grenade" || Weapon == "Survey Charge" || Weapon == "Timed Explosive Charge" || Weapon == "Satchel Charge" || Weapon == "Beancan Grenade")
                    return DeathReason.Explosion;


                else if (Weapon == "Flamethrower")
                    return DeathReason.Flamethrower;

                else if (Victim.Type == VictimType.Player || Victim.Type == VictimType.NPC)
                    return GetDeathReason(DamageType);

                if (Victim.Type == VictimType.Zombie)
                    return DeathReason.ZombieDeath;

                return DeathReason.Unknown;


            }

            private DeathReason GetDeathReason(string damage)
            {
                var reasons = (Enum.GetValues(typeof(DeathReason)) as DeathReason[]).Where(x => x.ToString().Contains(damage));

                if (reasons.Count() == 0)
                    return DeathReason.Unknown;

                return reasons.First();
            }

            private string InitializeDeathMessage()
            {
                string message = string.Empty;
                string reason = string.Empty;

                if (Victim.Type == VictimType.Player && Victim.Entity.ToPlayer().IsSleeping() && _config.Messages.ContainsKey(Reason + " Sleeping"))
                    reason = Reason + " Sleeping";
                else
                    reason = Reason.ToString();

                message = GetMessage(reason, _config.Messages);

                var attackerName = Attacker.Name;
                if (string.IsNullOrEmpty(attackerName) && Attacker.Entity == null && Weapon.Contains("Heli"))
                    attackerName = _config.HelicopterName;				

                if (string.IsNullOrEmpty(attackerName) && Attacker.Entity == null && Weapon.Contains("Bradl"))
                    attackerName = _config.BradleyAPCName;   

                switch (Attacker.Type)
                {
                    case AttackerType.ZombieDeath:
                        attackerName = _config.ZombieName;
                        break;

                    case AttackerType.Zombie:
                        attackerName = _config.ZombieName;
                        break;

                    case AttackerType.Helicopter:
                        attackerName = _config.HelicopterName;
                        break;

                    case AttackerType.BradleyAPC:
                        attackerName = _config.BradleyAPCName; 
                        break;

                    case AttackerType.NPC:
                        attackerName = _config.NPCName;
                        break;

                    case AttackerType.Turret:
                        attackerName = GetMessage(attackerName, _config.Turrets);
                        break;
                    case AttackerType.Guntrap:
                        attackerName = GetMessage(attackerName, _config.Turrets);
                        break;

                    case AttackerType.Trap:
                        attackerName = GetMessage(attackerName, _config.Traps);
                        break;

                    case AttackerType.Animal:
                        attackerName = GetMessage(attackerName, _config.Animals);
                        break;

                    case AttackerType.Structure:
                        attackerName = GetMessage(attackerName, _config.Structures);
                        break;
                }

                var victimName = Victim.Name;

                switch (Victim.Type)
                {
                    case VictimType.Helicopter:
                        victimName = _config.HelicopterName;
                        break;						

                    case VictimType.BradleyAPC:
                        victimName = _config.BradleyAPCName;  
                        break;

                    case VictimType.NPC:
                        victimName = _config.NPCName;



                        break;

                    case VictimType.Zombie:
                        victimName = _config.ZombieName;
                        break;

                    case VictimType.Animal:
                        victimName = GetMessage(victimName, _config.Animals);
                        break;
                }

                message = message.Replace("{attacker}", $"<color={_config.ColorAttacker}>{attackerName}</color>");
                message = message.Replace("{victim}", $"<color={_config.ColorVictim}>{victimName}</color>");
                message = message.Replace("{distance}", $"<color={_config.ColorDistance}>{Math.Round(Distance, 0)}</color>");
                message = message.Replace("{weapon}", $"<color={_config.ColorWeapon}>{GetMessage(Weapon, _config.Weapons)}</color>");
                message = message.Replace("{bodypart}", $"<color={_config.ColorBodyPart}>{GetMessage(BodyPart, _config.BodyParts)}</color>");

                return message;
            }
        }

        #endregion

        #region Oxide Hooks

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(new PluginConfig
            {
                Cooldown = 7,
                FontSize = 15,
                Distance = -1,
                Log = true,
                ShowDeathAnimals = true,
                ShowDeathSleepers = true,

                ColorAttacker = "#f0f223",
                ColorVictim = "#f0f223",
                ColorDistance = "#006ca9",
                ColorWeapon = "#006ca9",
                ColorBodyPart = "#006ca9",

                HelicopterName = "Вертолет",
                BradleyAPCName = "Танк",  
                NPCName = "НПЦ",
                ZombieName = "Зомби",

                Weapons = new Dictionary<string, string>
                {
                    { "Assault Rifle", "AKA-47" },
                    { "Beancan Grenade", "Бобовая граната" },
                    { "Nailgun", "Гвоздострел" },
                    { "Bolt Action Rifle", "Снайперская винтовка" },
                    { "Bone Club", "Костяная дубина" },
                    { "Bone Knife", "Костяной нож" },
                    { "Crossbow", "Арбалет" },
					{ "Chainsaw", "Бензопила" },
					{ "Compound Bow", "Блочный лук" },
					{ "Flamethrower", "Огнемёт" },
   					{ "Explosivesatchel", "Сумка с зарядом" },
                    { "Custom SMG", "SMG" },
                    { "Double Barrel Shotgun", "Двухстволка" },
                    { "Eoka Pistol", "Самодельный пистолет" },
                    { "F1 Grenade", "F1-граната" },
                    { "Hunting Bow", "Охотничий лук" },
                    { "Longsword", "Длинный меч" },
                    { "LR-300 Assault Rifle", "LR-300" },
                    { "M249", "Пулемёт М249" },
                    { "M92 Pistol", "Беретта M92" },
                    { "Mace", "Булава" },
                    { "Machete", "Мачете" },
                    { "MP5A4", "MP5A4" },
					{ "Jackhammer", "Отбойник" },
                    { "Pump Shotgun", "Помповый дробовик" },
                    { "Python Revolver", "Питон револьвер" },
                    { "Revolver", "Револьвер" },
                    { "Salvaged Cleaver", "Самодельный тесак" },
                    { "Salvaged Sword", "Самодельный меч" },
                    { "Semi-Automatic Pistol", "Полуавтоматический пистолет" },
                    { "Semi-Automatic Rifle", "Полуавтоматическая винтовка" },
                    { "Stone Spear", "Каменное копьё" },
					{ "Spas-12 Shotgun", "Дробовик Spas-12" },
                    { "Thompson", "Томпсон" },
                    { "Waterpipe Shotgun", "Самодельный дробовик" },
                    { "Wooden Spear", "Деревянное копьё" },
                    { "Hatchet", "Топор" },
                    { "Pick Axe", "Кирка" },
                    { "Salvaged Axe", "Самодельный топор" },
                    { "Salvaged Hammer", "Самодельный молот" },
                    { "Salvaged Icepick", "Самодельный ледоруб" },
                    { "Satchel Charge", "Сумка с зарядом" },
                    { "Stone Hatchet", "Каменный топор" },
                    { "Stone Pick Axe", "Каменная кирка" },
                    { "Survey Charge", "Геологический заряд" },
                    { "Timed Explosive Charge", "С4" },
                    { "Torch", "Факел" },
                    { "RocketSpeed", "Скоростная ракета" },
                    { "Incendiary Rocket", "Зажигательная ракета" },
                    { "Rocket", "Обычная ракета" },
                    { "RocketHeli", "Напалм вертолёта" },
                    { "RocketBradley", "Напалм танка" }

                },

                Structures = new Dictionary<string, string>
                {
                    { "Wooden Barricade", "Деревянная баррикада" },
                    { "Barbed Wooden Barricade", "Колючая деревянная баррикада" },
                    { "Metal Barricade", "Металлическая баррикада" },
                    { "High External Wooden Wall", "Высокая внешняя деревянная стена" },
                    { "High External Stone Wall", "Высокая внешняя каменная стена" },
                    { "High External Wooden Gate", "Высокие внешние деревянные ворота" },
                    { "High External Stone Gate", "Высокие внешние каменные ворота" }
                },

                Traps = new Dictionary<string, string>
                {
                    { "Snap Trap", "Капкан" },
                    { "Land Mine", "Мина" },
                    { "Wooden Floor Spikes", "Деревянные колья" }
                },

                Turrets = new Dictionary<string, string>
                {
                    { "Flame Turret", "Огнеметная турель" },
                    { "Auto Turret", "Автотурель" },
                    { "Guntrap", "Автодробовик" }
                },

                Animals = new Dictionary<string, string>
                {
                    { "Boar", "Кабан" },
                    { "Horse", "Лошадь" },
                    { "Wolf", "Волк" },
                    { "Stag", "Олень" },
                    { "Chicken", "Курица" },
                    { "Bear", "Медведь" }
                },

                BodyParts = new Dictionary<string, string>
                {
                    { "body", "Тело" },
                    { "pelvis", "Таз" },
                    { "hip", "Бедро" },
                    { "left knee", "Левое колено" },
                    { "right knee", "Правое колено" },
                    { "left foot", "Левая стопа" },
                    { "right foot", "Правая стопа" },
                    { "left toe", "Левый палец" },
                    { "right toe", "Правый палец" },
                    { "groin", "Пах" },
                    { "lower spine", "Нижний позвоночник" },
                    { "stomach", "Желудок" },
                    { "chest", "Грудь" },
                    { "neck", "Шея" },
                    { "left shoulder", "Левое плечо" },
                    { "right shoulder", "Правое плечо" },
                    { "left arm", "Левая рука" },
                    { "right arm", "Правая рука" },
                    { "left forearm", "Левое предплечье" },
                    { "right forearm", "Правое предплечье" },
                    { "left hand", "Левая ладонь" },
                    { "right hand", "Правая ладонь" },
                    { "left ring finger", "Левый безымянный палец" },
                    { "right ring finger", "Правый безымянный палец" },
                    { "left thumb", "Левый большой палец" },
                    { "right thumb", "Правый большой палец" },
                    { "left wrist", "Левое запястье" },
                    { "right wrist", "Правое запястье" },
                    { "head", "Голова" },
                    { "jaw", "Челюсть" },
                    { "left eye", "Левый глаз" },
                    { "right eye", "Правый глаз" }
                },

                Messages = new Dictionary<string, string>
                {
                    { "Arrow", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Blunt",  "{attacker} убил {victim} ({weapon})" },
                    { "Bullet", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Flamethrower", "{attacker} сжег заживо игрока {victim} ({weapon})" },
                    { "Drowned", "{victim} утонул." },
                    { "Explosion", "{attacker} взорвал игрока {victim} ({weapon})" },
                    { "Fall", "{victim} разбился." },
                    { "Generic", "Смерть забрала {victim} с собой." },
                    { "Heat", "{victim} сгорел заживо." },
                    { "Helicopter", "{attacker} прямым попаданием убил {victim}." },
                    { "BradleyAPC", "{attacker} прямым попаданием убил {victim}." },
                    { "BradleyAPCDeath", "{victim} был уничтожен игроком {attacker} ({weapon})" },
                    { "HelicopterDeath", "{victim} был сбит игроком {attacker} ({weapon})" },
                    { "Animal", "{attacker} добрался до {victim}" },
                    { "ZombieDeath", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Zombie", "{attacker} приследовал {victim}." },
                    { "AnimalDeath", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Hunger", "{victim} умер от голода." },
                    { "Poison", "{victim} умер от отравления." },
                    { "Radiation", "{victim} умер от радиационного отравления" },
                    { "Slash", "{attacker} убил {victim} ({weapon})" },
                    { "Stab", "{attacker} убил {victim} ({weapon})" },
                    { "Structure", "{victim} умер от сближения с {attacker}" },
                    { "Suicide", "{victim} совершил самоубийство." },
                    { "Thirst", "{victim} умер от обезвоживания" },
                    { "Trap", "{victim} попался на ловушку {attacker}" },
                    { "Cold", "{victim} умер от холода" },
                    { "Turret", "{victim} был убит автоматической турелью" },
                    { "Guntrap", "{victim} был убит ловушкой-дробовиком" },
                    { "Unknown", "У {victim} что-то пошло не так." },
                    { "Bleeding", "{victim} умер от кровотечения" },

                    //  Sleeping
                    { "Blunt Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Bullet Sleeping", "{attacker} убил {victim} с ({weapon},  с {distance} метров)" },
                    { "Flamethrower Sleeping", "{attacker} сжег игрока {victim} ({weapon})" },
                    { "Explosion Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Generic Sleeping", "Смерть забрала {victim} с собой пока он спал." },
                    { "Helicopter Sleeping", "{victim} был убит {attacker} пока он спал." },
                    { "BradleyAPC Sleeping", "{victim} был убит {attacker} пока он спал." },
                    { "Animal Sleeping", "{victim} убил {attacker} пока он спал." },
                    { "Slash Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Stab Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Unknown Sleeping", "У игрока {victim} что-то пошло не так." },
                    { "Turret Sleeping", "{attacker} был убит автоматической турелью." }
                }
            }, true);

            //PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
        }

        private void OnServerInitialized()
        {
            _config = Config.ReadObject<PluginConfig>();
        }

        private Dictionary<uint, BasePlayer> LastHeli = new Dictionary<uint, BasePlayer>();

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
                _lastHits[entity.ToPlayer().userID] = info;
            if (entity is BaseHelicopter && info.InitiatorPlayer != null)
                LastHeli[entity.net.ID] = info.InitiatorPlayer;
        }

        private void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            var _weapon = FirstUpper(info?.Weapon?.GetItem()?.info?.displayName?.english) ?? FormatName(info?.WeaponPrefab?.name);
            var _damageType = FirstUpper(victim.lastDamage.ToString());
            if (info == null)
                if (!(victim is BasePlayer) || !victim.ToPlayer().IsWounded() || !_lastHits.TryGetValue(victim.ToPlayer().userID, out info))
            return;
            if (victim as BaseCorpse != null) return;
            var _victim = new Victim(victim);
            var _attacker = new Attacker(info.Initiator);
            if (_victim == null)
                return;
            if (_attacker == null)
                return;
            if (_victim.Type == VictimType.Invalid)
                return;
            if (_attacker.Type == AttackerType.Invalid)
                return;
            if (_victim.Type == VictimType.Helicopter)
            {
                if (LastHeli.ContainsKey(victim.net.ID))
                {
                    _attacker = new Attacker(LastHeli[victim.net.ID]);
                }
            }
            if ((_victim.Type == VictimType.Zombie && _attacker.Type == AttackerType.NPC))
                return;
            if (!_config.ShowDeathAnimals && _victim.Type == VictimType.Animal)
            {
                return;
            }
            if (!_config.ShowDeathAnimals && _attacker.Type == AttackerType.Animal)
            {
                return;
            }
            if (_victim.Type == VictimType.Player && _victim.Entity.ToPlayer().IsSleeping() && !_config.ShowDeathSleepers)
                return;
            var _bodyPart = victim?.skeletonProperties?.FindBone(info.HitBone)?.name?.english ?? "";
            var _distance = info.ProjectileDistance;
            if (_config.Log && _victim.Type == VictimType.Player && _attacker.Type == AttackerType.Player)
            {

                LogToFile("log", $"[{DateTime.Now.ToShortTimeString()}] {info.Initiator} убил {victim} ({_weapon} [{_bodyPart}] с дистанции {_distance})", this, true);
            }
            AddNote(new DeathMessage(_attacker, _victim, _weapon, _damageType, _bodyPart, _distance));
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
        }

        #endregion

        #region Core

        private void AddNote(DeathMessage note)
        {
            _notes.Insert(0, note);
            if (_notes.Count > 8)
                _notes.RemoveRange(7, _notes.Count - 8);

            RefreshUI(note);
            timer.Once(_config.Cooldown, () =>
            {
                _notes.Remove(note);
                RefreshUI(note);
            });
        }

        #endregion

        #region UI

        private void RefreshUI(DeathMessage note)
        {
            foreach (var player in note.Players)
            {
                DestroyUI(player);
                InitilizeUI(player);
            }
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ui.deathmessages");
        }

        private void InitilizeUI(BasePlayer player)
        {
            var notes = _notes.Where(x => x.Players.Contains(player)).Take(8);

            if (notes.Count() == 0)
                return;

            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.8", AnchorMax = "0.99 0.995" }
            }, name: "ui.deathmessages");

            double index = 1;
            foreach (var note in notes)
            {
                InitilizeLabel(container, note.Message, $"0 {index - 0.2}", $"0.99 {index}");
                index -= 0.14;
            }

            CuiHelper.AddUi(player, container);
        }

        private string InitilizeLabel(CuiElementContainer container, string text, string anchorMin, string anchorMax)
        {
            string Name = CuiHelper.GetGuid();
            container.Add(new CuiElement
            {
                Name = Name,
                Parent = "ui.deathmessages",
                Components =
                {
                    new CuiTextComponent { Align = UnityEngine.TextAnchor.MiddleRight, FontSize = _config.FontSize, Text = text },
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1.0 -0.5" }
                }
            });
            return Name;
        }

        #endregion

        #region Helpers

        private static string FirstUpper(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return string.Join(" ", str.Split(' ').Select(x => x.Substring(0, 1).ToUpper() + x.Substring(1, x.Length - 1)).ToArray());
        }

        private static string FormatName(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return string.Empty;

            var formatedPrefab = FirstUpper(prefab.Split('/').Last().Replace(".prefab", "").Replace(".entity", "").Replace(".weapon", "").Replace(".deployed", "").Replace("_", "."));

            switch (formatedPrefab)
            {
                case "Autoturret.deployed": return "Auto Turret";
                case "Flameturret": return "Flame Turret";
                case "Guntrap.deployed": return "Guntrap";
                case "Beartrap": return "Snap Trap";
                case "Landmine": return "Land Mine";
                case "Spikes.floor": return "Wooden Floor Spikes";

                case "Barricade.wood": return "Wooden Barricade";
                case "Barricade.woodwire": return "Barbed Wooden Barricade";
                case "Barricade.metal": return "Metal Barricade";
                case "Wall.external.high.wood": return "High External Wooden Wall";
                case "Wall.external.high.stone": return "High External Stone Wall";
                case "Gates.external.high.stone": return "High External Wooden Gate";
                case "Gates.external.high.wood": return "High External Stone Gate";

                case "Stone.hatchet": return "Stone Hatchet";
				case "Stone.pickaxe": return "Stone Pickaxe";
                case "Survey.charge": return "Survey Charge";
                case "Explosive.satchel": return "Satchel Charge";
                case "Explosive.timed": return "Timed Explosive Charge";
                case "Grenade.beancan": return "Beancan Grenade";
                case "Grenade.f1": return "F1 Grenade";
                case "Hammer.salvaged": return "Salvaged Hammer";
                case "Axe.salvaged": return "Salvaged Axe";
                case "Icepick.salvaged": return "Salvaged Icepick";
                case "Spear.stone": return "Stone Spear";
                case "Spear.wooden": return "Wooden Spear";
                case "Knife.bone": return "Bone Knife";
                case "Rocket.basic": return "Rocket";    
                case "Flamethrower": return "Flamethrower";
                case "Rocket.hv": return "RocketSpeed";
                case "Rocket.heli": return "RocketHeli";
                case "Rocket.bradley": return "RocketBradley";


                default: return formatedPrefab;
            }
        }

        private static string GetMessage(string name, Dictionary<string, string> source)
        {
            if (source.ContainsKey(name))
                return source[name];

            return name;
        }

        #endregion
    }
}
                                                                                                     