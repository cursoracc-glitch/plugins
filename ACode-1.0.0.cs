using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ACode", "A0001", "1.0.0")]
	[Description("Установка последнего введенного кода")]
    class ACode : RustPlugin
    {
		#region FIELDS/DATA

        private readonly FieldInfo hasCodeField = typeof(CodeLock).GetField("hasCode", BindingFlags.Instance | BindingFlags.NonPublic);
		DynamicConfigFile saveFile = Interface.Oxide.DataFileSystem.GetFile("ACode/UserCode");
        Dictionary<ulong, string> userCode;

        void LoadData()
        {
            userCode = saveFile.ReadObject<Dictionary<ulong, string>>() ?? new Dictionary<ulong, string>();
        }

        void OnServerSave() => SaveData();

        void SaveData()
        {
            saveFile.WriteObject(userCode);
        }

		#endregion

		#region HOOKS

        void OnServerInitialized()
        {
            lang.RegisterMessages(Messages, this, "ru");
            Messages = lang.GetMessages("ru", this);
            LoadData();
        }

		void Unload() => SaveData();

		object CanChangeCode(BasePlayer player, CodeLock codeLock, string newCode, bool isGuestCode)
        {
			if (!isGuestCode)
			{
                userCode[player.userID] = newCode;
				SendReply(player, Messages["CodelockCodeSave"]);
                SaveData();
			}
			return null;
        }

		void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            var player = deployer.GetOwnerPlayer();
            if (player == null) return;
			if (entity.HasSlot(BaseEntity.Slot.Lock))	
            {
                BaseEntity slotEntity = entity.GetSlot(BaseEntity.Slot.Lock);
                if (slotEntity == null) return;
                if (slotEntity is CodeLock)
                {
                    CodeLock codeLock = slotEntity as CodeLock;
                    if (codeLock == null) return;
                    if (userCode.ContainsKey(player.userID))
                    {
						codeLock.code = userCode[player.userID];
                        codeLock.whitelistPlayers.Add(player.userID);
                        Effect.server.Run(codeLock.effectLocked.resourcePath, codeLock, 0, Vector3.zero, Vector3.forward, null, false);
                        codeLock.SetFlag(BaseEntity.Flags.Locked, true);
						codeLock.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
						SendReply(player, Messages["CodelockLastCode"]);
                    }
                    else
                    {
                        SendReply(player, Messages["CodelockNoCode"]);
                    }
                }
            }
        }

		#endregion

        #region LOCALIZATION

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            { "CodelockLastCode", "<size=16>Для замка установлен <color=#FAC73A>последний введеный код</color>!</size>"},
            { "CodelockNoCode", "<size=16>Код не установлен. Введите код в замок!</size>"},
            { "CodelockCodeSave", "<size=16>Ваш код сохранен. Он будет использоваться для всех следующих замков!</size>"}
        };

        #endregion
    }
}
