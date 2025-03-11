using System;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using Object = System.Object;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("IQTurret", "alone_sempai", "1.9.20")]
    [Description("Турели без электричества с лимитами на игрока/шкаф")]
    public class IQTurret : RustPlugin
    {
        void OnServerShutdown() => UnloadPlugin();

        private Boolean IsTurretFlagsTurned(BaseEntity entityTurret)
        {
            if (entityTurret == null) return false;

            return entityTurret switch
            {
                AutoTurret turret => turret.HasFlag(BaseEntity.Flags.On),
                SamSite site => site.HasFlag(BaseEntity.Flags.Reserved8),
                _ => false
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                if (config.weaponControll == null)
                {
                    config.weaponControll = new Configuration.WeaponControll()
                    {
                        UseWeaponController = false,
                        BlockSlotsAutoWeapon = false,
                        privilageWeapon = new Dictionary<String, Configuration.WeaponControll.WeaponListAutoTurret>()
                        {
                            ["iqturret.ultra"] = new Configuration.WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "rifle.ak",
                                ammoList = new List<Configuration.WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle.explosive",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle.explosive",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle.explosive",
                                        amount = 128,
                                    },
                                },
                            },
                            ["iqturret.king"] = new Configuration.WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "rifle.ak",
                                ammoList = new List<Configuration.WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                }
                            },
                            ["iqturret.premium"] = new Configuration.WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "rifle.semiauto",
                                ammoList = new List<Configuration.WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                }
                            },
                            ["iqturret.vip"] = new Configuration.WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "smg.thompson",
                                ammoList = new List<Configuration.WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.pistol",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.pistol",
                                        amount = 128,
                                    },
                                    new Configuration.WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.pistol",
                                        amount = 128,
                                    },
                                }
                            },
                        }
                    };
                }
            }
            catch
            {
                PrintWarning(LanguageEn
                    ? $"Error #8344445 configuration readings 'oxide/config/{Name}', creating a new configuration!"
                    : $"Ошибка #8344445 чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!"); 
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void SaveConfig() => Config.WriteObject(config);

        private enum TypeLimiter
        {
            Player,
            Building
        }

        void OnEntitySpawned(SamSite samSite)
        {
            if (!config.UseSamSite) return;
            NextTick(() => { SetupTurret(samSite); });
        }

        void OnEntityKill(SamSite samSite)
        {
            if (!config.UseSamSite) return;
            RemoveSwitch(samSite);
        }
        /// <summary>
        /// Обновление :
        /// - Дополнительная корректировка к API для IQGuardianDrone
        /// - Прочие корректировки
        /// </summary>
        ///
        
                private static IQTurret _;


        
        
        private static StringBuilder sb = new StringBuilder();

        void Unload() => UnloadPlugin();

        public void SendChat(String Message, BasePlayer player,
            ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                if (config.ReferencesPlugin.IQChatSetting.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else
                    IQChat?.Call("API_ALERT_PLAYER", player, Message,
                        config.ReferencesPlugin.IQChatSetting.CustomPrefix,
                        config.ReferencesPlugin.IQChatSetting.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        private BaseEntity GetTurret(UInt64 userID, ElectricSwitch electricSwitch)
        {
            if (!localRepositories.ContainsKey(userID)) return null;

            foreach (LocalRepository repository in localRepositories[userID])
            {
                if (repository.electricSwitch == null) continue;
                if (repository.electricSwitch != electricSwitch) continue;
                return repository.turret;
            }

            return null;
        }
        private readonly String PermissionTurnAllTurretsOn = "iqturret.turnonall";

        [ChatCommand("t")]
        void TurretControllChatCommand(BasePlayer player, String cmd, String[] arg)
        {
            if (player == null || arg == null || arg.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_ERROR", player.UserIDString), player);
                return;
            }

            String action = arg[0];
            TurretAction(player, action);
        }
		   		 		  						  	   		  	   		  	  			  	   		  		  
        private Int32 GetAmountActiveTurretPlayer(UInt64 userID, UInt64 buildingID)
        {
            if (!localRepositories.ContainsKey(userID)) return 0;
            List<LocalRepository> localTurrets = localRepositories[userID];

            Int32 countTurret = 0;

            foreach (LocalRepository localTurret in localTurrets)
            {
                if (localTurret.turret == null) continue;
                if (IsTurretElectricalTurned(localTurret.turret)) continue;
                if (!IsTurretFlagsTurned(localTurret.turret)) continue;

                UInt64 turretBuildingID = GetBuildingID(userID, localTurret.turret);
                switch (config.LimitController.typeLimiter)
                {
                    case TypeLimiter.Building when turretBuildingID != buildingID:
                    case TypeLimiter.Player when localTurret.turret.OwnerID != userID:
                        continue;
                    default:
                        countTurret++;
                        break;
                }
            }

            return countTurret;
        }

        
        
        private void TurretToggle(BasePlayer player, ElectricSwitch electricSwitch, Boolean useInCommand = false, Boolean primaryState = false)
        {
            if (electricSwitch == null) return;

            BaseEntity turret = GetTurret(electricSwitch.OwnerID, electricSwitch);
            if (turret == null) return;

            Boolean IsFlag = false;

            BaseEntity.Flags flags = turret switch
            {
                AutoTurret => BaseEntity.Flags.On,
                SamSite => BaseEntity.Flags.Reserved8,
                _ => BaseEntity.Flags.On
            };

            Boolean state = useInCommand ? primaryState : !turret.HasFlag(flags);
            turret.SetFlag(flags, state);
            IsFlag = turret.HasFlag(flags);
            
            if (useInCommand)
            {
                electricSwitch.SetFlag(BaseEntity.Flags.On, state);
                return;
            }
            
            if (!config.LimitController.UseLimitControll) return;

            UInt64 buildingID = GetBuildingID(player, turret);
            Int32 LimitCount = (GetLimitPlayer(electricSwitch.OwnerID) -
                                GetAmountActiveTurretPlayer(electricSwitch.OwnerID, buildingID));
            SendChat(
                GetLang(
                    IsFlag
                        ? (electricSwitch.OwnerID != player.userID
                            ? "INFORMATION_USER_ON_OTHER"
                            : "INFORMATION_USER_ON")
                        : (electricSwitch.OwnerID != player.userID
                            ? "INFORMATION_USER_OFF_OTHER"
                            : "INFORMATION_USER_OFF"), player.UserIDString, LimitCount), player);
        }

        
        
        
        private static Configuration config = new Configuration();

        
        
        private Boolean IsLimitPlayer(UInt64 userID, UInt64 buildingID) =>
            GetAmountActiveTurretPlayer(userID, buildingID) >= GetLimitPlayer(userID);

        
        
        
        private void AddWeaponTurret(AutoTurret turret, UInt64 userID)
        {
            if (!config.weaponControll.UseWeaponController) return;
            Configuration.WeaponControll.WeaponListAutoTurret weaponInfo = config.weaponControll.GetWeaponTurretDefault(userID);
            if (weaponInfo == null) return;
            
            Item weapon = ItemManager.CreateByName(weaponInfo.shortnameWeapon, 1);
            
            if (!weapon.MoveToContainer(turret.inventory, 0))
                weapon.Remove();
            else
            {
                turret.CancelInvoke(turret.UpdateAttachedWeapon);
                turret.UpdateAttachedWeapon();
        
                turret.Invoke(() => {                
                    AddReserveAmmo(turret, weaponInfo);
                    if (config.weaponControll.BlockSlotsAutoWeapon)
                    {
                        turret.dropsLoot = false;
                        turret.inventory.capacity = 0;
                    }
                },0.3f);
            }
        }

        private void UnloadPlugin()
        {
            if (_ == null) return;
       
            foreach (KeyValuePair<UInt64, List<LocalRepository>> localRepositories in localRepositories)
            {
                foreach (LocalRepository localRepository in localRepositories.Value)
                {
                    if (localRepository.electricSwitch != null && !localRepository.electricSwitch.IsDestroyed)
                        localRepository.electricSwitch.Kill();
                }
            }
            
            _ = null;
        }

        
        public Boolean IsRaidBlocked(BasePlayer player)
        {
            if (!config.ReferencesPlugin.BlockedTumblerRaidblock) return false;
            String ret = Interface.Call("CanTeleport", player) as String;
            return ret != null;
        }
        
        private void AddSamAmmo(SamSite samSite)
        {
            Item ammoItem = ItemManager.CreateByName("ammo.rocket.sam", config.samSiteControll.amount);
            if (ammoItem == null) return;
            if (!ammoItem.MoveToContainer(samSite.inventory))
                ammoItem.Remove();

            ammoItem.MarkDirty();
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

        
        
        
        Boolean IsValidTurret(BaseEntity turret, String action)
        {
            if (action == "off")
            {
                return (turret is AutoTurret && turret.HasFlag(BaseEntity.Flags.On))
                       || (turret is SamSite && turret.HasFlag(BaseEntity.Flags.Reserved8));
            }

            return (turret is AutoTurret && !turret.HasFlag(BaseEntity.Flags.On))
                   || (turret is SamSite && !turret.HasFlag(BaseEntity.Flags.Reserved8));
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["IS_LIMIT_TRUE"] =
                    "At you <color=#dd6363>exceeded</color> limit of active turrets <color=#dd6363>WITHOUT ELECTRICITY</color>",
                ["IS_TURRET_ELECTRIC_TRUE"] =
                    "This turret is connected <color=#dd6363>to electricity</color>, you can't use the switch!",
                ["IS_BUILDING_BLOCK_TOGGLE"] =
                    "You cannot use the switch in <color=#dd6363>someone else's house</color>",
                ["INFORMATION_USER_ON"] =
                    "You have successfully <color=#66e28b>enabled</color> the turret, you can still enable <color=#dd6363>{0}</color> turret",
                ["INFORMATION_USER_OFF"] =
                    "You have successfully <color=#dd6363>disabled</color> the turret, you can still enable <color=#dd6363>{0}</color> turret",
                ["INFORMATION_MY_LIMIT"] =
                    "<color=#dd6363> is available to you</color> to enable <color=#dd6363>{0}</color> turrets",
                ["SYNTAX_COMMAND_ERROR"] =
                    "<color=#dd6363>Syntax error : </color>\nUse the commands :\n1. t on - enables all disabled turrets\n2. t off - turns off all enabled turrets\n3. t limit - shows how many turrets are still available to you without electricity",
                ["PERMISSION_COMMAND_ERROR"] =
                    "<color=#dd6363>Access error : </color>\nYou don't have enough rights to use this command!",

                ["IS_LIMIT_TRUE_OTHER"] =
                    "The owner of the turret <color=#dd6363>exceeded</color> limit of active turrets <color=#dd6363>WITHOUT ELECTRICITY</color>",
                ["INFORMATION_USER_ON_OTHER"] =
                    "You have successfully <color=#66e28b>enabled</color> the player's turret, the player can still turn on <color=#dd6363>{0}</color> turret",
                ["INFORMATION_USER_OFF_OTHER"] =
                    "You have successfully <color=#dd6363>disabled</color> тthe player's turret, the player is still available for inclusion <color=#dd6363>{0}</color> turret",
                ["INFORMATION_USER_ACTION_COMMAND_BUILDING"] = "To use this command, you must be within the range of a cupboard",
                ["INFORMATION_USER_ACTION_COMMAND_COOLDOWN"] = "Cooldown for using the command, please wait another <color=#dd6363>{0}</color> seconds",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["IS_LIMIT_TRUE"] =
                    "У вас <color=#dd6363>превышен</color> лимит активных турелей <color=#dd6363>БЕЗ ЭЛЕКТРИЧЕСТВА</color>",
                ["IS_TURRET_ELECTRIC_TRUE"] =
                    "Данная турель подключена <color=#dd6363>к электричеству</color>, вы не можете использовать рубильник!",
                ["IS_BUILDING_BLOCK_TOGGLE"] =
                    "Вы не можете использовать рубильник в <color=#dd6363>чужом доме</color>",
                ["INFORMATION_USER_ON"] =
                    "Вы успешно <color=#66e28b>включили</color> турель, вам доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_USER_OFF"] =
                    "Вы успешно <color=#dd6363>выключили</color> турель, вам доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_MY_LIMIT"] =
                    "Вам <color=#dd6363>доступно</color> для включения <color=#dd6363>{0}</color> турелей",
                ["SYNTAX_COMMAND_ERROR"] =
                    "<color=#dd6363>Ошибка синтаксиса : </color>\nИспользуйте команды :\n1. t on - включает все выключенные\n2. t off - выключает все включенные турели\n3. t limit - показывает сколько вам еще доступно турелей без электричества",
                ["PERMISSION_COMMAND_ERROR"] =
                    "<color=#dd6363>Ошибка доступа : </color>\nУ вас недостаточно прав для использования данной команды!",

                ["IS_LIMIT_TRUE_OTHER"] =
                    "У владельца турели <color=#dd6363>превышен</color> лимит активных турелей <color=#dd6363>БЕЗ ЭЛЕКТРИЧЕСТВА</color>",
                ["INFORMATION_USER_ON_OTHER"] =
                    "Вы успешно <color=#66e28b>включили</color> турель игрока, игроку доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_USER_OFF_OTHER"] = "Вы успешно <color=#dd6363>выключили</color> турель игрока, игрока доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_USER_ACTION_COMMAND_BUILDING"] = "Для использования этой команды вы должны находиться в зоне действия шкафа",
                ["INFORMATION_USER_ACTION_COMMAND_COOLDOWN"] = "Перезарядка на использование команды, подождите еще <color=#dd6363>{0}</color> секунд",

            }, this, "ru");
            PrintWarning(LanguageEn ? "Logs: #832545 | Language file loaded successfully" : "Logs : #832545 | Языковой файл загружен успешно");
        }
        
        internal class LocalRepository
        {
            public BaseEntity turret;
            public ElectricSwitch electricSwitch;
        }
        
        
        
        
        private void LoadPlugin()
        {
            List<BaseNetworkable> turretServerList = BaseNetworkable.serverEntities
                .Where(b => b != null && b.net != null && IDsPrefabs.Contains(b.prefabID)).ToList();

            foreach (BaseNetworkable turretList in turretServerList)
            {
                BaseEntity turret = turretList as BaseEntity;
                if (turret == null) continue;
                
                UInt64 userID = turret.OwnerID;
                if (!userID.IsSteamId() || userID == 0) continue;

                if (IQGuardianDrone && turret is AutoTurret)
                {
                    Boolean IsValidTurret = IQGuardianDrone.Call<Boolean>("IsValidTurret", turret);
                    if (IsValidTurret)
                        continue;
                }
                
                NextTick(() =>
                {
                    if (turret == null) return;
                    SetupTurret(turret);
                });
            }

            timer.Once(2f, () => Subscribe(nameof(OnEntitySpawned)));
        }
		   		 		  						  	   		  	   		  	  			  	   		  		  
        [ConsoleCommand("t")]
        void TurretControllConsoleCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || arg == null || !arg.HasArgs(1))
            {
                SendChat(GetLang("SYNTAX_COMMAND_ERROR", player.UserIDString), player);
                return;
            }

            String action = arg.Args[0];
            TurretAction(player, action);
        }

        private Boolean IsTurretElectricalTurned(BaseEntity entityTurret)
        {
            if (entityTurret == null) return false;
            return entityTurret switch
            {
                AutoTurret turret => turret.currentEnergy > 0,
                SamSite site => site.currentEnergy > 0,
                _ => false
            };
        }

        
                
        private Dictionary<UInt64, List<LocalRepository>> localRepositories = new();
        
        void OnEntityKill(AutoTurret turret) => RemoveSwitch(turret);
        
        private void TurretAction(BasePlayer player, String action)
        {
            UInt64 playerBuildingID = GetBuildingID(player, null);
            if(playerBuildingID == 0)
            {
                SendChat(GetLang("INFORMATION_USER_ACTION_COMMAND_BUILDING", player.UserIDString), player);
                return;
            }

            if (config.cooldownController.useCooldown && !action.Equals("limit"))
            {
                if (!CooldownCommanndTOnOff.TryGetValue(player, out Double cooldown) || cooldown - CurrentTime <= 0)
                    CooldownCommanndTOnOff[player] = CurrentTime + config.cooldownController.timeCooldown;
                else
                {
                    SendChat(GetLang("INFORMATION_USER_ACTION_COMMAND_COOLDOWN", player.UserIDString, cooldown - CurrentTime), player);
                    return;
                }
            }

            Int32 limitPlayer = GetLimitPlayer(player.userID);

            if (localRepositories.TryGetValue(player.userID, out List<LocalRepository> localRepository))
            {
                List<LocalRepository> filteredList = new List<LocalRepository>();
		   		 		  						  	   		  	   		  	  			  	   		  		  
                foreach (LocalRepository item in localRepository)
                {
                    UInt64 turretBuildingID = GetBuildingID(null, item.turret);
                    if (item.turret != null && turretBuildingID != 0 && playerBuildingID == turretBuildingID)
                        filteredList.Add(item);
                }

                filteredList.Sort((x, y) =>
                {
                    if (x.turret == null || y.turret == null || x.turret.IsDestroyed || y.turret.IsDestroyed) return 0;
                    
                    Boolean xFlag = IsValidTurret(x.turret, action) && HasValidSwitch(x.electricSwitch, action) && x.turret.OwnerID == player.userID && GetBuildingID(null, x.turret) == playerBuildingID;
                    Boolean yFlag = IsValidTurret(y.turret, action) && HasValidSwitch(y.electricSwitch, action) && y.turret.OwnerID == player.userID && GetBuildingID(null, y.turret) == playerBuildingID;

                    if (xFlag != yFlag) 
                        return action == "off" ? yFlag.CompareTo(xFlag) : xFlag.CompareTo(yFlag);

                    if (action == "off") return 0;

                    return xFlag.CompareTo(yFlag);
                });
		   		 		  						  	   		  	   		  	  			  	   		  		  
                foreach (LocalRepository item in filteredList)
                {
                    if (limitPlayer < 0) break;
                    if (item.turret.IsDestroyed || item.electricSwitch == null ||
                        item.electricSwitch.IsDestroyed) continue;

                    UInt64 buildingID = GetBuildingID(player, item.turret);
                    
                    switch (action)
                    {
                        case "off" when permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOff):
                            TurretToggle(player, item.electricSwitch, true, false);
                            limitPlayer--;
                            break;
                        case "off":
                            SendChat(GetLang("PERMISSION_COMMAND_ERROR", player.UserIDString), player);
                            return;
                        case "on" when permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOn) &&
                                       IsLimitPlayer(player.userID, buildingID):
                            return;
                        case "on" when permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOn):
                            TurretToggle(player, item.electricSwitch, true, true);
                            limitPlayer--;
                            break;
                        case "on":
                            SendChat(GetLang("PERMISSION_COMMAND_ERROR", player.UserIDString), player);
                            return;
                    }
                }
            }

            if (action != "limit") return;
            String lang = GetLang("INFORMATION_MY_LIMIT", player.UserIDString, (limitPlayer - GetAmountActiveTurretPlayer(player.userID, playerBuildingID)));
            SendChat(lang, player);

        }

        object OnWireConnect(BasePlayer player, IOEntity entity1, int inputs, IOEntity entity2, int outputs)
        {
            if ((entity1 is AutoTurret or SamSite))
            {
                ElectricSwitch Switch = GetSwtich(entity1.OwnerID, entity1) as ElectricSwitch;
                if (Switch == null) return null;

                if (Switch.HasFlag(BaseEntity.Flags.On))
                {
                    Switch.SetFlag(BaseEntity.Flags.On, false);
                    Switch.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    Switch.MarkDirty();
                }
            }
            return null;
        }

        private ProtectionProperties ImmortalProtection;
        
        Boolean CanPickupEntity(BasePlayer player, AutoTurret turret)
        {
            Boolean canPickUp = true;
            Object canRemove = Interface.Call("canRemove", player, turret);

            if (IQGuardianDrone && canRemove != null)
                canPickUp = canRemove is not String; 
            
            if (turret == null || turret.OwnerID == 0 || !localRepositories.ContainsKey(player.userID)) return canPickUp;
     
            RemoveSwitch(turret);
            return canPickUp;
        }

        
        
        [PluginReference] private Plugin IQChat, IQGuardianDrone, IQDronePatrol;

        
        
        private Boolean API_IS_TURRETLIST(ElectricSwitch electricSwitch)
        {
            BaseEntity turret = GetTurret(electricSwitch.OwnerID, electricSwitch);
            return turret != null;
        }

        private void AddReserveAmmo(AutoTurret turret, Configuration.WeaponControll.WeaponListAutoTurret weaponInfo)
        {
            Int32 slot = 1;
            Int32 maxSlot = turret.inventory.capacity - 1;
		   		 		  						  	   		  	   		  	  			  	   		  		  
            foreach (Configuration.WeaponControll.WeaponListAutoTurret.AmmoList ammoList in weaponInfo.ammoList)
            {
                if (slot > maxSlot)
                    break;
                
                Item item = ItemManager.CreateByName(ammoList.shortname, ammoList.amount);
                if (!item.MoveToContainer(turret.inventory, slot))
                    item.Remove();

                item.MarkDirty();
                slot++;
            }
        }
        

        
        private void SetupTurret(BaseEntity entityTurret, Boolean IsInit = false)
        {
            Boolean isValidTurretDrone = false;
            if (IQGuardianDrone && entityTurret as AutoTurret)
                isValidTurretDrone = IQGuardianDrone.Call<Boolean>("IsValidTurret", entityTurret);

            if (isValidTurretDrone) return;
             
            if (IQDronePatrol && entityTurret as AutoTurret)
                isValidTurretDrone = IQDronePatrol.Call<Boolean>("IsValidTurret", entityTurret.OwnerID);
            
            if (isValidTurretDrone) return;
            
            if (entityTurret == null || entityTurret is NPCAutoTurret ||
                !IDsPrefabs.Contains(entityTurret.prefabID) && entityTurret.skinID == 1587601905 ||
                entityTurret.skinID == 763013205) return;

            Vector3 PositionSwitch = entityTurret is AutoTurret
                ? new Vector3(0f, 0.35f, 0.32f)
                : new Vector3(0f, 0.35f, 0.95f);

            ElectricSwitch smartSwitch = GameManager.server.CreateEntity(SwitchPrefab,
                entityTurret.transform.TransformPoint(PositionSwitch),
                Quaternion.Euler(entityTurret.transform.rotation.eulerAngles.x,
                    entityTurret.transform.rotation.eulerAngles.y, 0f), true) as ElectricSwitch;

            if (smartSwitch == null) return;

            smartSwitch.OwnerID = entityTurret.OwnerID;
            smartSwitch.pickup.enabled = false;
            smartSwitch.SetFlag(IOEntity.Flag_HasPower, true);
            smartSwitch.baseProtection = ImmortalProtection;

            
            foreach (var meshCollider in smartSwitch.GetComponentsInChildren<MeshCollider>())
                UnityEngine.Object.DestroyImmediate(meshCollider);

            UnityEngine.Object.DestroyImmediate(smartSwitch.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(smartSwitch.GetComponent<GroundWatch>());

            
            
            foreach (IOEntity.IOSlot input in smartSwitch.inputs)
                input.type = IOEntity.IOType.Generic;

            foreach (IOEntity.IOSlot output in smartSwitch.outputs)
                output.type = IOEntity.IOType.Generic;
		   		 		  						  	   		  	   		  	  			  	   		  		  
            
            smartSwitch.Spawn();
            smartSwitch.SetFlag(BaseEntity.Flags.Reserved8, true);
            smartSwitch.SetFlag(BaseEntity.Flags.On, false);

            if (!IsInit)
            {
                if (entityTurret as AutoTurret)
                {
                    AutoTurret autoTurret = (AutoTurret)entityTurret;
                    AddWeaponTurret(autoTurret, entityTurret.OwnerID);
                    autoTurret.SendNetworkUpdate();
                }
		   		 		  						  	   		  	   		  	  			  	   		  		  
                if(config.samSiteControll.useSamAmmo)
                    if (entityTurret as SamSite)
                    {
                        SamSite samSite = (SamSite)entityTurret;
                        samSite.Invoke(() =>
                        {
                            AddSamAmmo(samSite);
                            if (config.samSiteControll.BlockSlotsAutoWeapon)
                            {
                                samSite.dropsLoot = false;
                                samSite.inventory.capacity = 0;
                            }
                        }, 0.3f);

                        samSite.SendNetworkUpdate();
                    }
            }

            RegisteredTurret(entityTurret.OwnerID, smartSwitch, entityTurret);
        }

        void OnServerInitialized()
        {
            ImmortalProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
            ImmortalProtection.name = "TurretsSwitchProtection";
            ImmortalProtection.Add(1);
            
            foreach (String Permissions in config.LimitController.PermissionsLimits.Keys)
                if (!permission.PermissionExists(Permissions))
                    permission.RegisterPermission(Permissions, this);
            
            foreach (String Permissions in config.weaponControll.privilageWeapon.Keys)
                if (!permission.PermissionExists(Permissions))
                    permission.RegisterPermission(Permissions, this);

            permission.RegisterPermission(PermissionTurnAllTurretsOn, this);
            permission.RegisterPermission(PermissionTurnAllTurretsOff, this);

            LoadPlugin();
        }

        private Int32 GetLimitPlayer(UInt64 userID)
        {
            foreach (KeyValuePair<String, Int32> LimitPrivilage in config.LimitController.PermissionsLimits)
                if (permission.UserHasPermission(userID.ToString(), LimitPrivilage.Key))
                    return LimitPrivilage.Value;

            return config.LimitController.LimitAmount;
        }

        
        
        private UInt64 GetBuildingID(BasePlayer player, BaseEntity turret) => turret != null && turret.GetBuildingPrivilege() != null ? turret.GetBuildingPrivilege().buildingID : player != null && player.GetBuildingPrivilege() != null ?  player.GetBuildingPrivilege().buildingID : 0;
        
        object OnWireClear(BasePlayer player, IOEntity entity1, int connected, IOEntity entity2, bool flag)
        {
            ElectricSwitch switchConnected = entity1 as ElectricSwitch;
            if (switchConnected == null) return null;
            BaseEntity turret = GetTurret(switchConnected.OwnerID, switchConnected);
            if (turret != null)
                return false;

            return null;
        }
        
        private BaseEntity GetSwtich(UInt64 userID, BaseEntity turret)
        {
            if (!localRepositories.ContainsKey(userID)) return null;

            foreach (LocalRepository repository in localRepositories[userID])
            {
                if (repository.turret == null) continue;
                if (repository.turret != turret) continue;
                return repository.electricSwitch;
            }
		   		 		  						  	   		  	   		  	  			  	   		  		  
            return null;
        }

        
        
        private void RemoveSwitch(BaseEntity entity)
        {
            if (entity == null) return;
            UInt64 userID = entity.OwnerID;
            if (!localRepositories.ContainsKey(userID)) return;
            BaseEntity smartSwtich = GetSwtich(userID, entity);
            if (smartSwtich != null && !smartSwtich.IsDestroyed)
                smartSwtich.Kill();

            localRepositories[userID].RemoveAll(item => item.turret == entity);
        }
        
        Object OnSwitchToggle(IOEntity entity, BasePlayer player)
        {
            if (entity == null || player == null) return null;
            if (entity.OwnerID == 0) return null;
            if (IsRaidBlocked(player))
                return false;

            ElectricSwitch electricSwitch = entity as ElectricSwitch;
            if (electricSwitch == null) return null;
		   		 		  						  	   		  	   		  	  			  	   		  		  
            if(GetTurret(player.userID, electricSwitch) != null)
                if (!player.IsBuildingAuthed())
                {
                    SendChat(GetLang("IS_BUILDING_BLOCK_TOGGLE", player.UserIDString), player);
                    return false;
                }

            BaseEntity turret = GetTurret(electricSwitch.OwnerID, electricSwitch);
            if (turret == null) return null;

            if (IsTurretElectricalTurned(turret))
            {
                if (electricSwitch.HasFlag(BaseEntity.Flags.On))
                    electricSwitch.SetFlag(BaseEntity.Flags.On, false);
                
                return false;
            }
            
            if (electricSwitch.HasFlag(BaseEntity.Flags.On))
            {
                TurretToggle(player, electricSwitch);
                return null;
            }
		   		 		  						  	   		  	   		  	  			  	   		  		  
            if (config.LimitController.UseLimitControll)
            {
                UInt64 buildingID = turret.GetBuildingPrivilege() != null ? turret.GetBuildingPrivilege().buildingID : player.GetBuildingPrivilege() != null ?  player.GetBuildingPrivilege().buildingID : 0;
                
                if (IsLimitPlayer(electricSwitch.OwnerID, buildingID))
                {
                    SendChat(GetLang(electricSwitch.OwnerID != player.userID ? "IS_LIMIT_TRUE_OTHER" : "IS_LIMIT_TRUE", player.UserIDString), player);
                    return false;
                }
            }

            TurretToggle(player, electricSwitch);
            return null;
        }
        
        private Boolean API_IS_TURRETLIST(AutoTurret turret)
        {
            BaseEntity switchTurret = GetSwtich(turret.OwnerID, turret);
            return switchTurret != null;
        }

        public string GetLang(String LangKey, String userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
		   		 		  						  	   		  	   		  	  			  	   		  		  
            return lang.GetMessage(LangKey, this, userID);
        }

        private class Configuration
        {
            [JsonProperty(LanguageEn ? "To add a toggle switch for SamSite" : "Добавлять тумблер для SamSite")]
            public Boolean UseSamSite;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UseSamSite = true,
                    samSiteControll = new SamSiteControll
                    {
                        useSamAmmo = false,
                        amount = 30,
                        BlockSlotsAutoWeapon = true
                    },
                    cooldownController = new CooldownUseCommand()
                    {
                        useCooldown = true,
                        timeCooldown = 60f,
                    },
                    LimitController = new LimitControll
                    {
                        typeLimiter = TypeLimiter.Building,
                        UseLimitControll = true,
                        LimitAmount = 3,
                        PermissionsLimits = new Dictionary<String, Int32>()
                        {
                            ["iqturret.ultra"] = 150,
                            ["iqturret.king"] = 15,
                            ["iqturret.premium"] = 10,
                            ["iqturret.vip"] = 6,
                        }
                    },
                    weaponControll = new WeaponControll()
                    {
                        UseWeaponController = false,
                        BlockSlotsAutoWeapon = false,
                        privilageWeapon = new Dictionary<String, WeaponControll.WeaponListAutoTurret>()
                        {
                            ["iqturret.ultra"] = new WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "rifle.ak",
                                ammoList = new List<WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle.explosive",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle.explosive",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle.explosive",
                                        amount = 128,
                                    },
                                },
                            },
                            ["iqturret.king"] = new WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "rifle.ak",
                                ammoList = new List<WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                }
                            },
                            ["iqturret.premium"] = new WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "rifle.semiauto",
                                ammoList = new List<WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.rifle",
                                        amount = 128,
                                    },
                                }
                            },
                            ["iqturret.vip"] = new WeaponControll.WeaponListAutoTurret()
                            {
                                shortnameWeapon = "smg.thompson",
                                ammoList = new List<WeaponControll.WeaponListAutoTurret.AmmoList>()
                                {
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.pistol",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.pistol",
                                        amount = 128,
                                    },
                                    new WeaponControll.WeaponListAutoTurret.AmmoList()
                                    {
                                        shortname = "ammo.pistol",
                                        amount = 128,
                                    },
                                }
                            },
                        }
                    },
                    ReferencesPlugin = new ReferenceSettings
                    {
                        IQChatSetting = new ReferenceSettings.IQChatPlugin
                        {
                            CustomPrefix = "[<color=#ffff40>IQTurret</color>] ",
                            CustomAvatar = "0",
                            UIAlertUse = false,
                        },
                        BlockedTumblerRaidblock = true,
                    }
                };
            }

            [JsonProperty(LanguageEn ? "Configuring plugins for Collaboration" : "Настройка плагинов для совместной работы")]
            public ReferenceSettings ReferencesPlugin = new ReferenceSettings();
            [JsonProperty(LanguageEn ? "Setting the cooldown for the commands t on | t off" : "Настройка перезарядки для команд t on | t off")]
            public CooldownUseCommand cooldownController = new CooldownUseCommand();
            internal class CooldownUseCommand
            {
                [JsonProperty(LanguageEn ? "Use cooldown for commands t on | t off" : "Использовать перезарядку на команды t on | t off")]
                public Boolean useCooldown;
                [JsonProperty(LanguageEn ? "Time in seconds" : "Время в секундах")]
                public Single timeCooldown;
            }
            internal class LimitControll
            {
                [JsonProperty(LanguageEn ? "Limit Type: 0 - Player, 1 - Building" : "Тип лимита : 0 - На игрока, 1 - На шкаф")]
                public TypeLimiter typeLimiter;
                [JsonProperty(LanguageEn ? "Use the limit on turrets WITHOUT electricity? (true - yes/false - no)" : "Использовать лимит на туррели БЕЗ электричества? (true - да/false - нет)")]
                public Boolean UseLimitControll;
                [JsonProperty(LanguageEn ? "Limit turrets WITHOUT electricity (If the player does not have privileges)" : "Лимит турелей БЕЗ электричества (Если у игрока нет привилегий)")]
                public Int32 LimitAmount;
                [JsonProperty(LanguageEn ? "The limit of turrets WITHOUT electricity by privileges [Permission] = Limit (Make a list from more to less)" : "Лимит турелей БЕЗ электричества по привилегиям [Права] = Лимит (Составляйте список от большего - к меньшему)")]
                public Dictionary<String, Int32> PermissionsLimits = new Dictionary<String, Int32>();
            }

            internal class ReferenceSettings
            {
                [JsonProperty(LanguageEn ? "Setting up collaboration with IQChat" : "Настройка совместной работы с IQChat")]
                public IQChatPlugin IQChatSetting = new IQChatPlugin();

                internal class IQChatPlugin
                {
                    [JsonProperty(LanguageEn ? "IQChat :Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix;
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat(If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar;
                    [JsonProperty(LanguageEn ? "IQChat : Use UI notifications" : "IQChat : Использовать UI-уведомления")]
                    public Boolean UIAlertUse = false;
                }

                [JsonProperty(LanguageEn ? "Prohibit the use of a switch during a raidBlock?" : "Запретить использовать рубильник во время рейдблока?")]
                public Boolean BlockedTumblerRaidblock;
            }
            [JsonProperty(LanguageEn ? "Setting up the automatic addition of cartridges in SamSite" : "Настройка автоматического добавления патронов в SamSite")]
            public SamSiteControll samSiteControll = new SamSiteControll();

            internal class SamSiteControll
            {
                [JsonProperty(LanguageEn ? "Add ammo to SamSite" : "Добавлять патроны в SamSite")]
                public Boolean useSamAmmo;
                [JsonProperty(LanguageEn ? "Amount" : "Количество")]
                public Int32 amount;
                [JsonProperty(LanguageEn ? "Lock slots in SamSite after automatically adding a cartridge" : "Блокировать слоты в SamSite после автоматического добавления патрон")]
                public Boolean BlockSlotsAutoWeapon;
            }
            [JsonProperty(LanguageEn ? "Setting limits on turrets WITHOUT electricity" : "Настройка лимитов на турели БЕЗ электричества")]
            public LimitControll LimitController = new LimitControll();
            internal class WeaponControll
            {
                [JsonProperty(LanguageEn ? "Use the weapon embedding function after installing the auto turret" : "Использовать функцию встраивания оружия после установки автоматической турели")]
                public Boolean UseWeaponController;
                [JsonProperty(LanguageEn ? "Block slots in turrets - if weapons were added automatically" : "Блокировать слоты в автоматической турели - если были добавлены оружия автоматически")]
                public Boolean BlockSlotsAutoWeapon;
                
                [JsonProperty(LanguageEn ? "[Permissions] - Weapon settings" : "[Права] - Настройка оружия")]
                public Dictionary<String, WeaponListAutoTurret> privilageWeapon;

                public WeaponListAutoTurret GetWeaponTurretDefault(UInt64 userID)
                {
                    foreach (KeyValuePair<String, WeaponListAutoTurret> weaponList in privilageWeapon)
                    {
                        if (String.IsNullOrWhiteSpace(weaponList.Value.shortnameWeapon)) continue;
                        if (_.permission.UserHasPermission(userID.ToString(), weaponList.Key))
                            return weaponList.Value;
                    }

                    return null;
                }
                internal class WeaponListAutoTurret
                {
                    [JsonProperty(LanguageEn ? "Weapon shortname" : "Shortname оружия")]
                    public String shortnameWeapon;
                    [JsonProperty(LanguageEn ? "Ammo for turret" : "Патроны для турели")]
                    public List<AmmoList> ammoList = new List<AmmoList>();
                    internal class AmmoList
                    {
                        [JsonProperty(LanguageEn ? "Ammo shortname" : "Shortname патрона")]
                        public String shortname;
                        [JsonProperty(LanguageEn ? "Ammo amount" : "Количество патрон")]
                        public Int32 amount;
                    }
                }
            }
            [JsonProperty(LanguageEn ? "Setting up automatic weapon addition to the turret upon installation" : "Настройка автоматического добавления оружия в турель при установке")]
            public WeaponControll weaponControll = new WeaponControll();
        }
        private static Double CurrentTime => Facepunch.Math.Epoch.Current;
        private const Boolean LanguageEn = false;

        void OnEntitySpawned(AutoTurret turret) => NextTick(() => { SetupTurret(turret); });

        private void RegisteredTurret(UInt64 userID, ElectricSwitch smartSwitch, BaseEntity entityTurret)
        {
            BasePlayer player = BasePlayer.FindByID(userID);
            UInt64 buildingID = 0;

            if (player != null)
                buildingID = player.GetBuildingPrivilege()?.buildingID ?? 0;
            else buildingID = entityTurret.GetBuildingPrivilege()?.buildingID ?? 0;
		   		 		  						  	   		  	   		  	  			  	   		  		  
            AutoTurret autoTurret = entityTurret as AutoTurret;
            if (autoTurret != null && autoTurret.currentEnergy <= 0 && autoTurret.HasFlag(BaseEntity.Flags.On))
            {
                autoTurret.SetFlag(BaseEntity.Flags.On, true);
                smartSwitch.SetFlag(BaseEntity.Flags.On, true);
            }
            
            SamSite samSite = entityTurret as SamSite;
            if (samSite != null && samSite.currentEnergy <= 0 && samSite.HasFlag(BaseEntity.Flags.Reserved8))
            {
                samSite.SetFlag(BaseEntity.Flags.Reserved8, true);
                smartSwitch.SetFlag(BaseEntity.Flags.On, true);
            }

            if (!localRepositories.ContainsKey(userID))
                localRepositories.Add(userID, new List<LocalRepository> { });

            localRepositories[userID].Add(new LocalRepository
            {
                turret = entityTurret,
                electricSwitch = smartSwitch
            });
        }

        private Dictionary<BasePlayer, Double> CooldownCommanndTOnOff = new();
        private readonly List<UInt64> IDsPrefabs = new List<UInt64> { 3312510084, 2059775839 };

        Boolean HasValidSwitch(BaseEntity electricSwitch, String action)
        {
            if (action == "off")
                return electricSwitch != null && !electricSwitch.IsDestroyed &&
                       electricSwitch.HasFlag(BaseEntity.Flags.On);

            return electricSwitch != null && !electricSwitch.IsDestroyed &&
                   !electricSwitch.HasFlag(BaseEntity.Flags.On);
        }

        
        
        void Init()
        {
            _ = this;
            
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private UInt64 GetBuildingID(UInt64 userID, BaseEntity turret)
        {
            if (turret != null && turret.GetBuildingPrivilege() != null)
                return turret.GetBuildingPrivilege().buildingID;
            
            BasePlayer player = BasePlayer.FindByID(userID);
            if (player != null && player.GetBuildingPrivilege() != null)
                return player.GetBuildingPrivilege().buildingID;

            return 0;
        }
        private const String SwitchPrefab = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        private readonly String PermissionTurnAllTurretsOff = "iqturret.turnoffall";

            }
}