using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Random = Oxide.Core.Random;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("DoorManager", "fermens", "0.0.41")]
    [Description("^^")]
    public class DoorManager : RustPlugin
    {
        #region КЕШ
        static Dictionary<BasePlayer, CodeLock> codelocks = new Dictionary<BasePlayer, CodeLock>();
        static Dictionary<ulong, set> settings = new Dictionary<ulong, set>();
        const int codelockid = 1159991980;
        const string lockprefab = "assets/prefabs/locks/keypad/lock.code.prefab";

        class set
        {
            public string code;
            public bool autolock;
            public bool closer;
            public float delay;
        }

        Dictionary<string, string> MessagesRU = new Dictionary<string, string>
        {
            {"notallow", "<color=#ff8000>У ВАС НЕТ ДОСТУПА К ЭТОЙ КОМАНДЕ!</color>"},
            {"codeinit", "Кодовый замок установлен с кодом <color=#ff8000>{0}</color>."},
            {"autlockoff", "Автолок выключен."},
            {"autlockon", "Автолок включен."},
            {"codeupdate", "Ваш новый код - <color=#ff8000>{0}</color>."},
            {"closeroff", "Установка автодоводчика выключена."},
            {"closeron", "Установка автодоводчика включена."},
            {"closertime", "<color=#ff8000>МОЖНО УКАЗАТЬ НЕ МЕНЬШЕ 2 И НЕ БОЛЬШЕ 10!</color>"},
            {"closerdelay", "<color=#ff8000>Вы успешно указали новое время - {0} сек!</color>"},
            {"main", "<color=#ff8000>KualaCodeLock™</color> \n\n<color=#ff8000>/{0} c</color> - установить новый код\n<color=#ff8000>/{0} t</color> - включить/выключить автолок\n<color=#ff8000>/{0} cl</color> - включить/выключить установку автодоводчика\n<color=#ff8000>/{0} cl </color><color=#FF0077>2-10</color> - установить время закрытия дверей <color=#FF1400>(новых!)</color> <color=#ff8000>(сейчас:</color> <color=#D0007F>{delay}</color><color=#ff8000>)</color>"}
        };

        Dictionary<string, string> MessagesEN = new Dictionary<string, string>
        {
            {"notallow", "<color=#ff8000>YOU DO NOT HAVE ACCESS!</color>"},
            {"codeinit", "Combination lock installed with code <color=#ff8000>{0}</color>."},
            {"autlockoff", "Autolock off."},
            {"autlockon", "Autolock on."},
            {"codeupdate", "Your new code - <color=#ff8000>{0}</color>."},
            {"closeroff", "Automatic closer installation disabled."},
            {"closeron", "Automatic closer installation enabled."},
            {"closertime", "<color=#ff8000>IT IS POSSIBLE TO SPECIFY AT LEAST 2 AND NO MORE THAN 10!</color>"},
            {"closerdelay", "<color=#ff8000>You have successfully entered a new time - {0} сек!</color>"},
            {"main", "<color=#ff8000>/{0} code</color> - install new code\n<color=#ff8000>/{0} toggle</color> - enable/disabled autolock\n<color=#ff8000>/{0} closer</color> - enable/disabled installation of an automatic closer\n<color=#ff8000>/{0} closer 2-10</color> - set door closing time <color=#ff8000>[new!][current: {delay}]</color>"}
        };

        Dictionary<string, string> MessagesES = new Dictionary<string, string>
        {
            {"notallow", "<color=#ff8000>¡NO TIENES ACCESO!</color>"},
            {"codeinit", "Cerradura de combinación instalada con código <color=#ff8000>{0}</color>."},
            {"autlockoff", "Bloqueo automático desactivado."},
            {"autlockon", "Bloqueo automático incluido."},
            {"codeupdate", "Tu nuevo código - <color=#ff8000>{0}</color>."},
            {"closeroff", "Instalación automática más cercana desactivada."},
            {"closeron", "Instalación automática más cercana activada."},
            {"closertime", "<color=#ff8000>¡ES POSIBLE ESPECIFICAR AL MENOS 2 Y NO MÁS DE 10!</color>"},
            {"closerdelay", "<color=#ff8000>Ha ingresado exitosamente una nueva hora - {0} сек!</color>"},
            {"main", "<color=#ff8000>/{0} code</color> - instalar nuevo código\n<color=#ff8000>/{0} toggle</color> - activada/desactivada bloqueo automático\n<color=#ff8000>/{0} closer</color> - activada/desactivada instalación de un cerrador automático\n<color=#ff8000>/{0} closer 2-10</color> - establecer la hora de cierre de la puerta <color=#ff8000>[nuevo!][ahora: {delay}]</color>"}
        };
        #endregion

        #region ОКСИДХУКИ
        private void OnServerInitialized()
        {
            lang.RegisterMessages(MessagesEN, this, "en");
            lang.RegisterMessages(MessagesRU, this, "ru");
            lang.RegisterMessages(MessagesRU, this, "uk");
            lang.RegisterMessages(MessagesES, this, "es");

            permission.RegisterPermission("doormanager.noitem", this);
            permission.RegisterPermission("doormanager.auto", this);

            settings = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, set>>("doormanager");

            var command = Interface.Oxide.GetLibrary<ru.Libraries.Command>(null);
            command.AddChatCommand("autolock", this, "cmdcommand");
            command.AddChatCommand("al", this, "cmdcommand");
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            if (player == null) return;

            if (!(go.ToBaseEntity() is DecayEntity)) return;

            DecayEntity decayEntity = go.ToBaseEntity().GetComponent<DecayEntity>();
            if (decayEntity == null) return;

            set set;
            if (!settings.TryGetValue(player.userID, out set))
            {
                ADD(player.userID);
                set = settings[player.userID];
            }

            if (!set.autolock || decayEntity.IsLocked()) return;

            if (decayEntity is StorageContainer)
            {
                StorageContainer storageContainer = decayEntity as StorageContainer;
                if (!storageContainer.isLockable) return;
            }
            else if (decayEntity is AnimatedBuildingBlock)
            {
                if (HasCloserPermission(player.UserIDString))
                {
                    Door door = decayEntity.GetComponent<Door>();
                    if (door != null /*&& door.canTakeCloser*/ && (decayEntity.HasSlot(BaseEntity.Slot.UpperModifier) || decayEntity.HasSlot(BaseEntity.Slot.LowerCenterDecoration)))
                    {
                        CREATECLOSER(decayEntity, set.delay, player);
                    }
                }
            }
            else return;

            if (!HASLOCKER(player)) return;

            CodeLock codeLock = GameManager.server.CreateEntity(lockprefab) as CodeLock;
            if (codeLock == null) return;

            codeLock.OwnerID = player.userID;
            codeLock.Spawn();
            codeLock.code = set.code;
            codeLock.SetParent(decayEntity, decayEntity.GetSlotAnchorName(BaseEntity.Slot.Lock));
            decayEntity.SetSlot(BaseEntity.Slot.Lock, codeLock);
            codeLock.SetFlag(BaseEntity.Flags.Locked, true);
            Effect.server.Run(codeLock.effectLocked.resourcePath, codeLock.transform.position);
            codeLock.whitelistPlayers.Add(player.userID);

            player.ChatMessage(GetMessageLanguage("codeinit", player.UserIDString).Replace("{0}", player.net.connection.info.GetBool("global.streamermode") ? "****" : set.code));
        }
        private void Unload()
        {
            Save();
            foreach (var z in codelocks) z.Value?.Kill();
        }
        #endregion
        
        #region ПОМОШНИКИ
        private string GetRandomCode() => Random.Range(1, 9999).ToString().PadLeft(4, '0');
        private bool HasCloserPermission(string id) => permission.UserHasPermission(id, "doormanager.auto");
        private void Save()
        {
            if (settings != null && settings.Count > 0) Interface.Oxide.DataFileSystem.WriteObject("doormanager", settings);
        }

        private string GetMessageLanguage(string key, string userId)
        {
            return lang.GetMessage(key, this, userId);
        }

        private void ADD(ulong id) => settings.Add(id, new set { code = GetRandomCode(), closer = true, autolock = true, delay = 3f });

        private void CREATECLOSER(BaseEntity entity, float delay, BasePlayer player)
        {
            DoorCloser doorcloser = GameManager.server.CreateEntity(StringPool.Get(1831641807), new Vector3(), new Quaternion(), true) as DoorCloser;
            if (doorcloser == null) return;
            doorcloser.gameObject.Identity();
            doorcloser.delay = delay;

            if (entity.ShortPrefabName.StartsWith("door.double.hinged") || entity.ShortPrefabName == "wall.frame.garagedoor")
            {
                doorcloser.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.LowerCenterDecoration));
            }
            else
            {
                doorcloser.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.UpperModifier));
            }

            doorcloser.OnDeployed(entity, player);
            if (entity.ShortPrefabName == "floor.ladder.hatch") doorcloser.transform.localPosition = new Vector3(0.7f, 0f, 0f);
            doorcloser.Spawn();
            entity.SetSlot(BaseEntity.Slot.UpperModifier, doorcloser);
        }

        private bool HASLOCKER(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "doormanager.noitem")) return true;
            Item item = player.inventory.FindItemID(codelockid);
            if (item == null) return false;
            player.inventory.Take(null, codelockid, 1);
            return true;
        }

        private void OpenCodeLockUI(BasePlayer player)
        {
            CodeLock codeLock = GameManager.server.CreateEntity(lockprefab, player.eyes.position + new Vector3(0, -3, 0)) as CodeLock;
            if (codeLock != null)
            {
                codeLock.Spawn();
                codeLock.SetFlag(BaseEntity.Flags.Locked, true);
                codeLock.ClientRPCPlayer(null, player, "EnterUnlockCode");
                codelocks[player] = codeLock;
                Subscribe("OnCodeEntered");

                timer.Once(20f, () => codeLock?.Kill());
            }
        }

        private void OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
        {
            if (player == null) return;

            CodeLock cl;
            if (!codelocks.TryGetValue(player, out cl)) return;

            if (cl != codeLock)
            {
                cl?.Kill();
                codelocks.Remove(player);
                return;
            }

            set set;
            if (!settings.TryGetValue(player.userID, out set)) return;

            set.code = code;
            player.ChatMessage(string.Format(GetMessageLanguage("codeupdate", player.UserIDString), player.net.connection.info.GetBool("global.streamermode") ? "****" : code));
            Effect.server.Run(cl.effectCodeChanged.resourcePath, player.transform.position);
            cl?.Kill();
            codelocks.Remove(player);
            if (codelocks.Count == 0) Unsubscribe("OnCodeEntered");
        }
        #endregion

        #region ЧАТ_КОМАНДА
        private void cmdcommand(BasePlayer player, string command, string[] args)
        {
            set set;
            if (!settings.TryGetValue(player.userID, out set))
            {
                ADD(player.userID);
                set = settings[player.userID];
            }

            if (args == null || args.Length < 1)
            {
                player.ChatMessage(GetMessageLanguage("main", player.UserIDString).Replace("{0}", command).Replace("{delay}", set.delay.ToString()));
                return;
            }

            if(args[0] == "c")
            {
                OpenCodeLockUI(player);
            }
            else if (args[0] == "t")
            {
                set.autolock = !set.autolock;
                player.ChatMessage(GetMessageLanguage(set.autolock ? "autlockon" : "autlockoff", player.UserIDString));
            }
            else if (args[0] == "cl")
            {
                if (!HasCloserPermission(player.UserIDString))
                {
                    player.ChatMessage(GetMessageLanguage("notallow", player.UserIDString));
                    return;
                }
                float time;
                if (args.Length == 1 || !float.TryParse(args[1], out time))
                {
                    set.closer = !set.closer;
                    player.ChatMessage(GetMessageLanguage(set.closer ? "closeron" : "closeroff", player.UserIDString));
                }
                else
                {
                    if(time < 2 || time > 10)
                    {
                        player.ChatMessage(GetMessageLanguage("closertime", player.UserIDString));
                        return;
                    }
                    set.delay = time;
                    player.ChatMessage(GetMessageLanguage("closerdelay", player.UserIDString).Replace("{0}", time.ToString()));
                }
            }
        }
        #endregion

    }
}
