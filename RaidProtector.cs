using System;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Game.Rust.Libraries;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("RaidProtector", "Vlad-00003", "1.0.0")]
    [Description("Decrese damage based on permissions, time and online state of the owner.")]
	 /*
	 * Author info:
	 *   E-mail: Vlad-00003@mail.ru
	 *   Vk: vk.com/vlad_00003
	 */
    class RaidProtector : RustPlugin
    {
        #region Vars
        private PluginConfig config;
        private Dictionary<BasePlayer, Timer> Informed = new Dictionary<BasePlayer, Timer>();
        #endregion

        #region Config 
        private class TimeConfig
        {
            [JsonProperty("Использовать защиту в указанный промежуток времени")]
            public bool UseTime;
            [JsonProperty("Час начала защиты")]
            public int Start;
            [JsonProperty("Час снятия защиты")]
            public int End;
        }
        private class PermisssionConfig
        {
            [JsonProperty("Множитель урона по постройкам")]
            public float modifier;
            [JsonProperty("Настройка времени")]
            public TimeConfig timeConfig;
            [JsonProperty("Защищать постройки когда игрок вне сети")]
            public bool Offline;
        }
        private class ProtectionSetup
        {
            [JsonProperty("Чат-команда для получения короткого имени префаба предмета, на который вы смотрите")]
            public string ChatCommand;
            [JsonProperty("Защищать строительные блоки(Стены, фундаменты, каркасы...)")]
            public bool BuildingBlock;
            [JsonProperty("Защищать двери(обычные, двойные, высокие)")]
            public bool Door;
            [JsonProperty("Защищать простые строительные блоки(высокие стены)")]
            public bool SimpleBuildingBlock;
            [JsonProperty("Список префабов, которые необходимо защищать(короткое или полное имя префаба)")]
            public List<string> Prefabs;
        }
        private class PluginConfig
        {
            [JsonProperty("Настройка привилегий")]
            public Dictionary<string, PermisssionConfig> Custom;
            [JsonProperty("Стандартные настройки для всех игроков")]
            public PermisssionConfig Default;
            [JsonProperty("Настройки защиты")]
            public ProtectionSetup Protection;
            [JsonProperty("Формат сообщений в чате")]
            public string ChatFormat;
            [JsonProperty("Задержка между сообщениями в чат о блокировке")]
            public float MessageCooldown;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    ChatFormat = "<color=#f4c842>[RaidProtector]</color> <color=#969696>{0}</color>",
                    MessageCooldown = 15f,
                    Default = new PermisssionConfig()
                    {
                        modifier = 0.7f,
                        Offline = false,
                        timeConfig = new TimeConfig()
                        {
                            UseTime = true,
                            Start = 23,
                            End = 10
                        }
                    },
                    Custom = new Dictionary<string, PermisssionConfig>()
                    {
                        ["raidprotector.pro"] = new PermisssionConfig()
                        {
                            modifier = 0.5f,
                            Offline = false,
                            timeConfig = new TimeConfig()
                            {
                                UseTime = true,
                                Start = 20,
                                End = 12
                            }
                        },
                        ["raidprotector.vip"] = new PermisssionConfig()
                        {
                            modifier = 0.3f,
                            Offline = true,
                            timeConfig = new TimeConfig()
                            {
                                UseTime = false,
                                Start = 23,
                                End = 10
                            }
                        }
                    },
                    Protection = new ProtectionSetup()
                    {
                        ChatCommand = "/shortname",
                        BuildingBlock = true,
                        Door = true,
                        SimpleBuildingBlock = true,
                        Prefabs = new List<string>()
                        {
                            "mining_quarry",
                            "vendingmachine.deployed",
                            "furnace.large",
                            "cupboard.tool.deployed",
                            "refinery_small_deployed"
                        }
                    }
                };
            }
        }
        #endregion

        #region Config initialization
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            foreach (var priv in config.Custom.Keys)
            {
                permission.RegisterPermission(priv, this);
            }
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Localization
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Protected"] = "This building <color=#770000>is protected</color>  and will recive only <color=#777700>{0}%</color> of damage",
                ["Protected anti"] = "Damage for this building <color=#007700>is increased</color> to <color=#777700>{0}%</color>",
                ["Name found"] = "Shortname for entity \"{0}\"",
                ["No item found"] = "No entity can be found in front of you!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Protected"] = "Эта постройка <color=#770000>защищена</color> и будет получать только <color=#777700>{0}%</color> от урона",
                ["Protected anti"] = "Урон по данной постройке <color=#007700>увеличен</color> на <color=#777700>{0}%</color>",
                ["Name found"] = "Короткое имя предмета \"{0}\"",
                ["No item found"] = "Перед вам не обнаружено предмета!"
            }, this, "ru");
        }
        private string GetMsg(string key, BasePlayer player) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        #endregion

        #region Chat commands
        private void GetShortName(BasePlayer player, string command, string[] args)
        {
            RaycastHit RayHit;
            bool flag1 = Physics.Raycast(player.eyes.HeadRay(), out RayHit, 100);
            var TargetEntity = flag1 ? RayHit.GetEntity() : null;
            if (TargetEntity)
            {
                string message = string.Format(GetMsg("Name found", player), TargetEntity.ShortPrefabName);
                player.ChatMessage(message);
                player.ConsoleMessage(message);
                return;
            }
            player.ChatMessage(GetMsg("No item found", player));
        }
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            LoadMessages();
            var command = Interface.Oxide.GetLibrary<Command>();
            command.AddChatCommand(config.Protection.ChatCommand.Replace("/", string.Empty), this, GetShortName);
        }
        void Unloaded()
        {
            var timers = Informed.Select(p => p.Value).ToArray();
            Informed.Clear();
            foreach(var t in timers)
            {
                t?.Destroy(); 
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo.damageTypes.Has(Rust.DamageType.Decay)) return;
            if (CheckEntity(entity, hitInfo))
            {
                var perm = GetPerm(entity.OwnerID);
                if (perm.modifier == 1f) return;
                if(CheckTime(perm) || CheckOffline(entity.OwnerID, perm))
                {
                    var dec = perm.modifier < 1 ? true : false;
                    if (hitInfo.InitiatorPlayer)
                    {
                        hitInfo.damageTypes.ScaleAll(perm.modifier);
                        if (Informed.ContainsKey(hitInfo.InitiatorPlayer)) return;
                        if (dec)
                        {
                            Reply(hitInfo.InitiatorPlayer, "Protected", perm.modifier * 100f);
                        }
                        else
                        {
                            Reply(hitInfo.InitiatorPlayer, "Protected anti", (perm.modifier - 1f) * 100f);
                        }
                        Informed.Add(hitInfo.InitiatorPlayer, timer.Once(config.MessageCooldown, () =>
                        {
                            Informed.Remove(hitInfo.InitiatorPlayer);
                        }));
                        return;
                    }
                    if(hitInfo.Initiator?.name == "assets/bundled/prefabs/fireball_small.prefab")
                    {
                        hitInfo.damageTypes.ScaleAll(perm.modifier);
                    }
                }
            }
        }
        #endregion

        #region Helpers
        private void Reply(BasePlayer player, string langkey, params object[] args)
        {
            SendReply(player, string.Format(config.ChatFormat, GetMsg(langkey, player)), args);
        }
        private bool CheckEntity(BaseCombatEntity ent, HitInfo info)
        {
            if (ent.OwnerID == 0)
                return false;
            if (ent.OwnerID == info.InitiatorPlayer?.userID)
                return false;
            if (ent is BuildingBlock && config.Protection.BuildingBlock)
                return true;
            if (ent is Door && config.Protection.Door)
                return true;
            if (ent is SimpleBuildingBlock && config.Protection.SimpleBuildingBlock)
                return true;
            if (config.Protection.Prefabs.Contains(ent.ShortPrefabName))
                return true;
            return false;
        }
        private bool CheckTime(PermisssionConfig perm)
        {
            TimeConfig time = perm.timeConfig;
            if (!time.UseTime) return false;
            var Now = DateTime.Now.TimeOfDay;
            var Start = new TimeSpan(time.Start, 0, 0);
            var End = new TimeSpan(time.End, 0, 0);
            if (Start < End)
                return Start <= Now && Now <= End;
            return !(End < Now && Now < Start);
        }
        private bool CheckOffline(ulong UserID, PermisssionConfig perm)
        {
            if (!perm.Offline) return false;
            if (!BasePlayer.FindByID(UserID)) return true;
            return false;
        }
        private PermisssionConfig GetPerm(ulong userID)
        {
            var perms = config.Custom.Where(p => permission.UserHasPermission(userID.ToString(), p.Key)).Select(p => p.Value);
            return perms.Count() > 0 ? perms.Aggregate((i1, i2) => i1.modifier < i2.modifier ? i1 : i2) : config.Default;
        }
        #endregion
    }
}