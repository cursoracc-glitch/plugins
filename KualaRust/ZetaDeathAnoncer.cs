using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;
using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZetaDeathAnoncer", "fermens", "0.1.71")]
    [Description("ОТОБРАЖЕНИЕ УБИЙСТВ СПРАВА СВЕРХУ, КОПИЯ С МАГИК РАСТ")]
    public class ZetaDeathAnoncer : RustPlugin
    {
        #region Grid
        Dictionary<string, Vector3> Grids = new Dictionary<string, Vector3>();
        const float calgon = 0.0066666666666667f;
        void CreateSpawnGrid()
        {
            var worldSize = (ConVar.Server.worldsize);
            float offset = worldSize / 2;
            var gridWidth = (calgon * worldSize);
            float step = worldSize / gridWidth;

            string start = "";

            char letter = 'A';
            int number = 0;

            for (float zz = offset; zz > -offset; zz -= step)
            {
                for (float xx = -offset; xx < offset; xx += step)
                {
                    Grids.Add($"{start}{letter}{number}", new Vector3(xx - 55f, 0, zz));
                    if (letter.ToString().ToUpper() == "Z")
                    {
                        start = "A";
                        letter = 'A';
                    }
                    else
                    {
                        letter = (char)(((int)letter) + 1);
                    }


                }
                number++;
                start = "";
                letter = 'A';
            }
        }

        private string GetNameGrid(Vector3 pos)
        {
            return Grids.Where(x => x.Value.x < pos.x && x.Value.x + 150f > pos.x && x.Value.z > pos.z && x.Value.z - 150f < pos.z).FirstOrDefault().Key;
        }
        #endregion

        #region Oxide Hooks

        private void Unload()
        {
            foreach (var z in killsList) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", z.guid);
        }

        void OnServerInitialized()
        {
            if (!config.truename) SaveConfig();
            if (config.animal == null || config.animal.translatekill == null)
            {
                config.mes[TA.Animal] = "убил";
                config.mes[TA.AnimalKill] = "убил";
                config.animal = new animal
                {
                    animalkillplayers = true,
                    killanimals = false,
                    translatekill = new Dictionary<string, string>
                        {
                            { "wolf", "Волк" },
                            { "bear", "Медведь" },
                            { "boar", "Кабан" },
                            { "chicken", "Курица" },
                            { "horse", "Лошадь" },
                            { "stag", "Олень" },
                            { "testridablehorse", "Лошадь" }
                        },
                    translatedeath = new Dictionary<string, string>
                        {
                            { "wolf", "Волка" },
                            { "bear", "Медведя" },
                            { "boar", "Кабана" },
                            { "chicken", "Курицу" },
                            { "horse", "Лошадь" },
                            { "stag", "Оленя" },
                            { "testridablehorse", "Лошадь" }
                        }
                };
                SaveConfig();
            }
            if (config.wound)
            {
                Unsubscribe("OnPlayerRecover");
                Unsubscribe("OnPlayerRecovered");
                Unsubscribe("CanBeWounded");
            }
            if (config.names == null)
            {
                config.names = new Dictionary<TA, string>
                    {
                        { TA.Death, "Ученый [NPC]" },
                        { TA.HeliKill, "Вертолет [NPC]" },
                        { TA.TankKill, "Танк [NPC]" },
                        { TA.Heli, "Вертолет [NPC]" },
                        { TA.Tank, "Танк [NPC]" }
                    };
                config.mes.Add(TA.Heli, "подбил");
                config.mes.Add(TA.Tank, "взорвал");
                config.mes.Add(TA.HeliKill, "убил");
                config.mes.Add(TA.TankKill, "поджарил");
                config.format4 = "<color=COL2>{0}</color> {1} <color=COL1>{2}</color> (квадрат {3})";
                config.format5 = "<color=COL2>{0}</color> {1} <color=COL1>{2}</color>";
                SaveConfig();
            }
            CreateSpawnGrid();
            Guijson = "[{\"name\":\"{guid}\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":\"{size}\",\"fadeIn\":\"{fade}\",\"align\":\"UpperRight\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.7\",\"distance\":\"0.6 0.6\"},{\"type\":\"RectTransform\",\"anchormin\":\"1 1\",\"anchormax\":\"1 1\",\"offsetmin\":\"-400 {min}\",\"offsetmax\":\"-5 {max}\"}],\"fadeOut\":\"0.1\"}]".Replace("{size}", config.textSize.ToString());
        }
        string Guijson = "";
        [PluginReference] Plugin Battles, HaxBot;
        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L)) return true;
            return false;
        }

        Dictionary<uint, ulong> helilast = new Dictionary<uint, ulong>();
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
            {
                BasePlayer player = info.InitiatorPlayer;
                if (player == null) return;
                helilast[entity.net.ID] = player.userID;
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (entity is BaseHelicopter)
            {
                uint id = entity.net.ID;
                if (!helilast.ContainsKey(id)) return;
                BasePlayer player = BasePlayer.FindByID(helilast[id]);
                if (player == null) return;
                add(entity, TA.Heli, player, GetNameGrid(entity.transform.position));
                helilast.Remove(id);
            }
            else if (info.Initiator is BasePlayer)
            {
                if (entity is BradleyAPC) add(entity, TA.Tank, info.InitiatorPlayer, GetNameGrid(entity.transform.position));
                else if (config.animal.killanimals && (entity is BaseAnimalNPC || entity is RidableHorse))
                {
                    add(entity, TA.Animal, info.InitiatorPlayer, info.WeaponPrefab?.ShortPrefabName ?? info.damageTypes.GetMajorityDamageType().ToString(), (int)Vector3.Distance(info.InitiatorPlayer.transform.position, entity.transform.position));
                }
            }
        }

        private void OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null || info == null || Battles != null && (bool)Battles.Call("IsPlayerOnBattle", victim.userID)) return;
            if (info.Initiator is BasePlayer)
            {
                var attacker = info.InitiatorPlayer;
                if (attacker == null) return;
                if (!config.npc && (IsNPC(victim) || IsNPC(attacker))) return;
                if (attacker == victim && info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Suicide)
                {
                    if (config.suicide) add(victim, TA.Suicide);
                    return;
                }
                add(victim, victim.IsSleeping() ? TA.Sleep : TA.Death, attacker, info.WeaponPrefab?.ShortPrefabName ?? info.damageTypes.GetMajorityDamageType().ToString(), (int)Vector3.Distance(attacker.transform.position, victim.transform.position));
            }
            else if (info.Initiator is BaseHelicopter)
            {
                add(victim, TA.HeliKill, (BaseHelicopter)info.Initiator);
            }
            else if (info.Initiator is BradleyAPC)
            {
                add(victim, TA.TankKill, (BradleyAPC)info.Initiator);
            }
            else if (config.animal.animalkillplayers && (info.Initiator is BaseAnimalNPC || info.Initiator is RidableHorse))
            {
                add(victim, TA.AnimalKill, (BaseCombatEntity)info.Initiator);
            }
        }

        object OnPlayerRecover(BasePlayer player)
        {
            if (IsNPC(player) || Battles != null && (bool)Battles.Call("IsPlayerOnBattle", player.userID)) return null;
            add(player, TA.Up);
            return null;
        }

        void OnPlayerRecovered(BasePlayer player)
        {
            if (IsNPC(player) || Battles != null && (bool)Battles.Call("IsPlayerOnBattle", player.userID)) return;
            add(player, TA.Up);
        }

        void CanBeWounded(BasePlayer victim, HitInfo info)
        {
            if (victim == null) return;
            var attacker = info?.InitiatorPlayer;
            if (attacker == null) return;
            if (!config.npc && (IsNPC(victim) || IsNPC(attacker))) return;
            NextTick(() =>
            {
                if (!victim.IsWounded() || Battles != null && (bool)Battles.Call("IsPlayerOnBattle", victim.userID)) return;
                add(victim, TA.Wound, attacker, info.WeaponPrefab?.ShortPrefabName ?? info.damageTypes.GetMajorityDamageType().ToString(), (int)Vector3.Distance(attacker.transform.position, victim.transform.position));
            });
        }

        #endregion

        #region Config
        private PluginConfig config;
        protected override void LoadDefaultConfig()
        {
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

        class animal
        {
            [JsonProperty(PropertyName = "Отображать убийства животных?")]
            public bool killanimals;

            [JsonProperty(PropertyName = "Отображать смерти игроков от животных?")]
            public bool animalkillplayers;

            [JsonProperty(PropertyName = "Перевод - для смерти")]
            public Dictionary<string, string> translatedeath;

            [JsonProperty(PropertyName = "Перевод - для убийства")]
            public Dictionary<string, string> translatekill;
        }

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Животные")]
            public animal animal;

            [JsonProperty(PropertyName = "Длительность показа убийства")]
            public float timeout;

            [JsonProperty(PropertyName = "Максимальное отображаемое кол-во убийств")]
            public int kills;

            [JsonProperty(PropertyName = "Отступ")]
            public int nim;

            [JsonProperty(PropertyName = "Формат - с оружием и расстоянием")]
            public string format1;

            [JsonProperty(PropertyName = "Формат - с оружием, когда расстояние меньше 1 м.")]
            public string format2;

            [JsonProperty(PropertyName = "Формат - для суицидов и др.")]
            public string format3;

            [JsonProperty(PropertyName = "Формат - для сбитого вертолета и танка.")]
            public string format4;

            [JsonProperty(PropertyName = "Формат - для вертолета и танка.")]
            public string format5;

            [JsonProperty(PropertyName = "Размер текста")]
            public int textSize;

            [JsonProperty(PropertyName = "Не отображать когда нокнули и когда поднялся")]
            public bool wound;

            [JsonProperty(PropertyName = "Отображать суициды")]
            public bool suicide;

            [JsonProperty(PropertyName = "Максимальная длина ника")]
            public int maxNameChars;

            [JsonProperty(PropertyName = "Верхняя точка по оси Y (от 0 до минус бесконечность)")]
            public int start;

            [JsonProperty(PropertyName = "Отображать убийства/смерти от NPC")]
            public bool npc;

            [JsonProperty(PropertyName = "Цвет себя")]
            public string color1;

            [JsonProperty(PropertyName = "Цвет друга")]
            public string color2;

            [JsonProperty(PropertyName = "Отображать настоящие ники ботов")]
            public bool truename;

            [JsonProperty(PropertyName = "Цвет других")]
            public string color3;

            [JsonProperty(PropertyName = "Вырезки")]
            public Dictionary<TA, string> mes;

            [JsonProperty(PropertyName = "Названия")]
            public Dictionary<TA, string> names;

            [JsonProperty(PropertyName = "Перевод")]
            public Dictionary<string, string> custom;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    animal = new animal
                    {
                        animalkillplayers = true,
                        killanimals = false,
                        translatekill = new Dictionary<string, string>
                        {
                            { "wolf", "Волк" },
                            { "bear", "Медведь" },
                            { "boar", "Кабан" },
                            { "chicken", "Курица" },
                            { "horse", "Лошадь" },
                            { "stag", "Олень" },
                            { "testridablehorse", "Лошадь" }
                        },
                        translatedeath = new Dictionary<string, string>
                        {
                            { "wolf", "Волка" },
                            { "bear", "Медведя" },
                            { "boar", "Кабана" },
                            { "chicken", "Курицу" },
                            { "horse", "Лошадь" },
                            { "stag", "Оленя" },
                            { "testridablehorse", "Лошадь" }
                        }
                    },
                    timeout = 7f,
                    kills = 7,
                    npc = true,
                    textSize = 14,
                    maxNameChars = 14,
                    start = -10,
                    nim = 20,
                    format1 = "<color=COL2>{0}</color> {1} <color=COL1>{2}</color> ({3}, {4} м.)",
                    format2 = "<color=COL2>{0}</color> {1} <color=COL1>{2}</color> ({3})",
                    format3 = "<color=COL1>{0}</color> {1}",
                    format4 = "<color=COL2>{0}</color> {1} <color=COL1>{2}</color> (квадрат {3})",
                    format5 = "<color=COL2>{0}</color> {1} <color=COL1>{2}</color>",
                    color1 = "#ffd479",
                    color3 = "#3399ff",
                    color2 = "#ccff99",
                    custom = new Dictionary<string, string>
                    {
                        { "40mm_grenade_he", "Гранатомёт" },
                        {"mp5.entity", "MP5A4" },
                        { "crossbow.entity", "Арбалет" },
                        { "bolt_rifle.entity", "Болт" },
                        { "compound_bow.entity", "Блочный лук" },
                        { "nailgun.entity", "Гвоздомёт" },
                        { "pitchfork.entity", "Вилы" },
                        { "shotgun_pump.entity", "Помповый дробовик" },
                        { "pistol_semiauto.entity", "P250" },
                        { "semi_auto_rifle.entity", "Берданка" },
                        { "m39.entity", "Винтовка M39" },
                        { "l96.entity", "Винтовка L96" },
                        { "thompson.entity", "Томсон" },
                        { "knife.combat.entity", "Боевой нож" },
                        { "grenade.beancan.deployed", "Бобовка" },
                        { "spas12.entity", "Дробовик Spas-12" },
                        { "grenade.f1.deployed", "Граната F1" },
                        { "longsword.entity", "Длинный меч" },
                        { "double_shotgun.entity", "Двухствольный дробовик" },
                        { "mace.entity", "Булава" },
                        { "spear_wooden.entity", "Деревянное копье" },
                        { "rock.entity", "Камень" },
                        { "torch.entity", "Факел" },
                        { "knife_bone.entity", "Костяная дубинка" },
                        { "bone_club.entity", "Костяной нож" },
                        { "candy_cane.entity", "Леденец-дубинка" },
                        { "flamethrower.entity", "Огнемёт" },
                        { "m249.entity", "Пулемёт M249" },
                        { "bow_hunting.entity", "Охотничий лук" },
                        { "ak47u.entity", "AK-47" },
                        { "salvaged_cleaver.entity", "Самодельный тесак" },
                        { "snowball.entity", "Снежок" },
                        { "pistol_revolver.entity", "Револьвер" },
                        { "python.entity", "Питон" },
                        { "rocket_basic", "Ракетница" },
                        { "pistol_eoka.entity", "Самодельный пистолет" },
                        { "salvaged_sword.entity", "Самодельный меч" },
                        { "shotgun_waterpipe.entity", "Самодельный дробовик" },
                        { "spear_stone.entity", "Каменное копье" },
                        { "lr300.entity", "LR-300" },
                        { "butcherknife.entity", "Нож мясника" },
                        { "machete.weapon", "Мачете" },
                        { "explosive.timed.deployed", "С4" },
                        { "stone_pickaxe.entity", "Каменная кирка" },
                        { "stonehatchet.entity", "Каменный топор" },
                        { "pickaxe.entity", "Кирка" },
                        { "m92.entity", "Беретта" },
                        { "survey_charge.deployed", "Геологический заряд" },
                        { "chainsaw.entity", "Бензопила" },
                        { "jackhammer.entity", "Отбойный молоток" },
                        { "icepick_salvaged.entity", "Самодельный ледоруб" },
                        { "hammer_salvaged.entity", "Самодельный молот" },
                        { "hatchet.entity", "Топор" },
                        { "axe_salvaged.entity", "Самодельный топор" },
                        { "flashlight.entity", "Фонарик" },
                        { "cake.entity", "Торт" },
                        { "explosive.satchel.deployed", "Сумка с зарядом" },
                        { "smg.entity", "SMG" }
                    },
                    mes = new Dictionary<TA, string>
                    {
                        { TA.Death, "убил" },
                        { TA.Animal, "убил" },
                        { TA.AnimalKill, "убил" },
                        { TA.Wound, "нокнул" },
                        { TA.Up, "каким то чудом сам поднялся" },
                        { TA.Suicide, "совершил самоубийство" },
                        { TA.Sleep, "убил спящего" },
                        { TA.Heli, "подбил" },
                        { TA.Tank, "взорвал" },
                        { TA.HeliKill, "убил" },
                        { TA.TankKill, "поджарил" },
                    },
                    names = new Dictionary<TA, string>
                    {
                        { TA.Death, "Ученый [NPC]" },
                        { TA.HeliKill, "Вертолет [NPC]" },
                        { TA.TankKill, "Танк [NPC]" },
                        { TA.Heli, "Вертолет [NPC]" },
                        { TA.Tank, "Танк [NPC]" }
                    },
                    suicide = true
                };
            }
        }
        #endregion

        Dictionary<ulong, Anoncer> ttt = new Dictionary<ulong, Anoncer>();
        class Anoncer
        {
            public bool wound;
            public Timer ttimer;
            public float pos;
        }

        enum TA
        {
            Death, Wound, Sleep, Suicide, Up, Heli, Tank, HeliKill, TankKill, Animal, AnimalKill
        }
        class infoA
        {
            public bool npcA;
            public string guid;
            public TA what;
            public string message;
            public ulong victim;
            public ulong attacker;
            public List<ulong> friendsV = new List<ulong>();
            public List<ulong> friendsA = new List<ulong>();
        }
        private List<infoA> killsList = new List<infoA>();
        #region Core
        private void add(BaseCombatEntity victim, TA what, BaseCombatEntity attacker = null, string weapon = null, int distance = 0)
        {
            string startfade = "0.5", victimname = "";
            infoA newinfoA = new infoA();
            newinfoA.guid = Random.Range(0, 99999).ToString();
            newinfoA.what = what;

            if (victim is BasePlayer)
            {
                BasePlayer VICTIM = (BasePlayer)victim;
                if (IsNPC(VICTIM))
                {
                    if (!config.truename) victimname = config.names[TA.Death];
                    else victimname = VICTIM.displayName /*+ " (бот)"*/;
                }
                else
                {
                    if (VICTIM.Team != null && VICTIM.Team.members.Count > 1) newinfoA.friendsV.AddRange(VICTIM.Team.members.Where(x => x != VICTIM.userID));
                    victimname = VICTIM.displayName;
                    if (victimname.Length > config.maxNameChars) victimname = victimname.Substring(0, config.maxNameChars);
                }
                newinfoA.victim = VICTIM.userID;
            }
            else if (what == TA.Tank)
            {
                victimname = config.names[TA.Tank];
                newinfoA.victim = victim.net.ID;
            }
            else if (what == TA.Heli)
            {
                victimname = config.names[TA.Heli];
                newinfoA.victim = victim.net.ID;
            }
            else if (what == TA.Animal)
            {
                string animal;
                if (!config.animal.translatedeath.TryGetValue(victim.ShortPrefabName, out animal))
                {
                    Debug.Log($"Добавьте перевод для животного - {victim.ShortPrefabName}");
                    animal = victim.ShortPrefabName;
                }
                victimname = animal;
                newinfoA.victim = victim.net.ID;
            }
            else return;

            if (attacker != null)
            {
                string attackername = "";
                if (attacker is BasePlayer)
                {
                    BasePlayer ATTACKER = (BasePlayer)attacker;
                    if (IsNPC(ATTACKER))
                    {
                        if (!config.truename) attackername = config.names[TA.Death];
                        else attackername = ATTACKER.displayName/* + " (бот)"*/;
                    }
                    else
                    {
                        if (ATTACKER.Team != null && ATTACKER.Team.members.Count > 1) newinfoA.friendsA.AddRange(ATTACKER.Team.members.Where(x => x != ATTACKER.userID));
                        attackername = ATTACKER.displayName;
                        if (attackername.Length > config.maxNameChars) attackername = attackername.Substring(0, config.maxNameChars);
                    }
                    newinfoA.attacker = ATTACKER.userID;
                }
                else if (what == TA.TankKill)
                {
                    attackername = config.names[TA.TankKill];
                    newinfoA.attacker = attacker.net.ID;
                }
                else if (what == TA.HeliKill)
                {
                    attackername = config.names[TA.HeliKill];
                    newinfoA.attacker = attacker.net.ID;
                }
                else if (what == TA.AnimalKill)
                {
                    string animal;
                    if (!config.animal.translatekill.TryGetValue(attacker.ShortPrefabName, out animal))
                    {
                        Debug.Log($"Добавьте перевод для животного - {attacker.ShortPrefabName}");
                        animal = attacker.ShortPrefabName;
                    }
                    attackername = animal;
                    newinfoA.attacker = attacker.net.ID;
                }
                else return;
                //what.Equals(TA.Animal)
                if (what.Equals(TA.Tank) || what.Equals(TA.Heli)) newinfoA.message = string.Format(config.format4, attackername, config.mes[what], victimname, weapon);
                else if (what.Equals(TA.TankKill) || what.Equals(TA.HeliKill) || what.Equals(TA.AnimalKill)) newinfoA.message = string.Format(config.format5, attackername, config.mes[what], victimname);
                else if (distance > 1) newinfoA.message = string.Format(config.format1, attackername, config.mes[what], victimname, GetName(weapon), distance);
                else newinfoA.message = string.Format(config.format2, attackername, config.mes[what], victimname, GetName(weapon));
            }
            else newinfoA.message = string.Format(config.format3, victimname, config.mes[what]);
            HaxBot?.Call("SENDTODISCORD", $"[{DateTime.Now.ToShortTimeString()}] {STRIP(newinfoA.message)}", 1);
            killsList.Add(newinfoA);
            if (killsList.Count >= config.kills) destroying(killsList[0]);
            if (what.Equals(TA.Death))
            {
                infoA woun = killsList.Where(x => x.victim.Equals(newinfoA.victim) && x.what.Equals(TA.Wound)).FirstOrDefault();
                if (woun != null)
                {
                    destroying(woun);
                    startfade = "0";
                }
            }

            string falde = startfade;
            int start = config.start;
            foreach (var log in killsList.AsEnumerable().Reverse())
            {
                string send = Guijson.Replace("{guid}", log.guid).Replace("{fade}", falde).Replace("{max}", start.ToString()).Replace("{min}", (start - config.nim).ToString());
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", log.guid);
                List<Network.Connection> sendto = new List<Network.Connection>(Network.Net.sv.connections);
                foreach (var z in Network.Net.sv.connections)
                {
                    if (log.victim.Equals(z.userid))
                    {
                        CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = z }, null, "AddUI", send.Replace("{text}", log.message.Replace("COL1", config.color1).Replace("COL2", log.friendsV.Contains(z.userid) ? config.color2 : config.color3)));
                        sendto.Remove(z);
                    }
                    else if (log.attacker.Equals(z.userid))
                    {
                        CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = z }, null, "AddUI", send.Replace("{text}", log.message.Replace("COL1", log.friendsA.Contains(z.userid) ? config.color2 : config.color3).Replace("COL2", config.color1)));
                        sendto.Remove(z);
                    }
                    else if (log.friendsA.Contains(z.userid))
                    {
                        CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = z }, null, "AddUI", send.Replace("{text}", log.message.Replace("COL1", log.friendsA.Contains(log.victim) ? config.color2 : config.color3).Replace("COL2", config.color2)));
                        sendto.Remove(z);
                    }
                    else if (log.friendsV.Contains(z.userid))
                    {
                        CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = z }, null, "AddUI", send.Replace("{text}", log.message.Replace("COL1", config.color2).Replace("COL2", log.friendsV.Contains(log.attacker) ? config.color2 : config.color3)));
                        sendto.Remove(z);
                    }
                }
                if (sendto.Count > 0)
                {
                    send = send.Replace("{text}", log.message.Replace("COL1", config.color3).Replace("COL2", config.color3));
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", send);
                }
                falde = "0";
                start -= config.nim;
            }
            timerYA.Add(newinfoA.guid, timer.Once(config.timeout, () => destroying(newinfoA)));
        }

        void destroying(infoA newinfoA)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", newinfoA.guid);
            killsList.Remove(newinfoA);
            timerYA[newinfoA.guid]?.Destroy();
            timerYA.Remove(newinfoA.guid);
        }
        Dictionary<string, Timer> timerYA = new Dictionary<string, Timer>();
        private string GetName(string name)
        {
            if (config.custom.ContainsKey(name)) return config.custom[name];
            config.custom.Add(name, name);
            SaveConfig();
            return name;
        }

        #endregion

        private readonly List<string> _tags = new List<string>
        {
            "</color>",
            "`",
            "</size>",
            "<i>",
            "</i>",
            "<b>",
            "</b>"
        };

        private readonly List<Regex> _regexTags = new List<Regex>
        {
            new Regex("<color=.+?>", RegexOptions.Compiled),
            new Regex("<size=.+?>", RegexOptions.Compiled)
        };

        private string STRIP(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return string.Empty;
            }

            foreach (string tag in _tags)
            {
                original = original.Replace(tag, "");
            }

            foreach (Regex regexTag in _regexTags)
            {
                original = regexTag.Replace(original, "");
            }

            return original;
        }
    }
}