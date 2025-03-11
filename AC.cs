using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Libraries;
using System.Collections.Generic;
using Time = UnityEngine.Time;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("AntiCheat", "playermodel", "1.0.0")]
    class AC : RustPlugin
    {

        #region Helpers
        
        public class PlayerDetect
        {
            public string SteamId { get; set; }
            public int Detects { get; set; }
            
            public string DetectType { get; set; }
        }
        
        class DetectSystem {
            static List<PlayerDetect> repository = new List<PlayerDetect>();

            public static int Count(string SteamId, string DetectType)
            {
                IEnumerable<PlayerDetect> detects = repository.Where(item => item.SteamId == SteamId && item.DetectType == DetectType);

                if(detects.Count() < 1) {
                    return 0;
                }

                PlayerDetect detect = detects.First();

                if(detect != null)
                    return detect.Detects;
                
                return 0;
            }

            public static bool Increase(string SteamId, string DetectType)
            {
                IEnumerable<PlayerDetect> detects = repository.Where(item => item.SteamId == SteamId && item.DetectType == DetectType);

                if(detects.Count() < 1) {
                    repository.Add(new PlayerDetect() {
                        SteamId = SteamId,
                        DetectType = DetectType,
                        Detects = 1
                    });

                    return true;
                }

                PlayerDetect detect = detects.First();

                if(detect == null) return false;

                if(!repository.Contains(detect)) return false;

                repository[repository.IndexOf(detect)].Detects++;

                return true;
            }

            public static void Drop(string SteamId, string DetectType)
            {
                repository.RemoveAll(item => item.SteamId == SteamId && item.DetectType == DetectType);
            }
        }

        class AcConfig
        {
            public string DiscordWebHook;
            public string BanReason;
            public int AntiFakeAdminInterval;
            public bool BanForBulletTeleport;
            public bool BanForInvalidDistance;
            public bool BanForFastKill;
            public bool BanForAntiAimDetectType1;
            public bool BanForWalkOnWater;
            public bool BanForIvalidHitMaterial;
            public bool BanForAutoFarm;
            public bool ShootWhileMounted;
        }
        AcConfig config;
        
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new AcConfig() {
                DiscordWebHook = "https://discord.com/api/webhooks/",
                BanReason = "Использование читов",
                AntiFakeAdminInterval = 30,
                BanForBulletTeleport = true,
                BanForInvalidDistance = true,
                BanForFastKill = true,
                BanForAntiAimDetectType1 = false,
                BanForWalkOnWater = false,
                BanForIvalidHitMaterial = true,
                BanForAutoFarm = true,
                ShootWhileMounted = true,
            }, true);
        }
        void SendDetectDiscord(string content, string detectType)
        {
            Dictionary<string, object> embed = new Dictionary<string, object>
            {
                ["embeds"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = $"Игрок - {content}",
                        ["description"] = $"Возможное использование {detectType}, проследите за игроком!",
                        ["color"] = 16711680
                    }
                }
            };
            
            string json = JsonConvert.SerializeObject(embed);
            
            webrequest.Enqueue(config.DiscordWebHook, json, (code, response) =>
            {
                if (code != 204)
                {
                    Puts($"Failed to send Discord webhook. Code: {code}, Response: {response}");
                }
                else
                {
                    Puts("Discord webhook sent successfully!");
                }
            }, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
        }
        
        void SendProjectileLog(string content, string detectType, HitInfo info, BasePlayer attacker)
        {
            Dictionary<string, object> embed = new Dictionary<string, object>
            {
                ["embeds"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = $"Игрок - {content}",
                        ["description"] = $"Возможное использование {detectType}, проследите за игроком!",
                        ["color"] = 16711680,
                        ["fields"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["name"] = "Shoot information:",
                                ["value"] = $"\n\n" +
                                            $"Hit position world: {info.HitPositionWorld}\n" +
                                            $"Hit position local: {info.HitPositionLocal}\n" +
                                            $"Hit normal world: {info.HitNormalWorld}\n" +
                                            $"Hit normal local: {info.HitNormalLocal}\n" +
                                            $"Attacker see point end? {attacker.IsVisible(info.HitPositionOnRay())}\n" +
                                            $"\nDebug info:\n" +
                                            $"Projectile distance: {info.ProjectileDistance}\n" +
                                            $"Distance to target: {Vector3.Distance(attacker.transform.position, info.HitPositionWorld)}\n" +
                                            $"Point start: {info.PointStart}\n" +
                                            $"Point end: {info.PointEnd}\n" +
                                            $"Weapon used: {info.WeaponPrefab.name}"
                            }
                        }
                    }
                }
            };
            
            string json = JsonConvert.SerializeObject(embed);
            
            webrequest.Enqueue(config.DiscordWebHook, json, (code, response) =>
            {
                if (code != 204)
                {
                    Puts($"Failed to send Discord webhook. Code: {code}, Response: {response}");
                }
                else
                {
                    Puts("Discord webhook sent successfully!");
                }
            }, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
        }
        bool IsInteger(double number)
        {
            return number == (int)number;
        }
        
        #endregion

        void Init()
        {
            config = Config.ReadObject<AcConfig>();
        }

        void OnServerInitialized()
        {
            timer.Every(config.AntiFakeAdminInterval, () => {
                foreach(BasePlayer player in BasePlayer.activePlayerList) {
                    if(player != null && !player.IsAdmin) {
                        player.SendConsoleCommand("noclip");
                        player.SendConsoleCommand("debugcamera");
                        player.SendConsoleCommand("drawcolliders true");
                    }
                }
            });
        }
        
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if(player == null || player.IsAdmin) return;
            
            if (player.timeSinceLastTick > 0.3f && Performance.report.frameRate > 60)
            {

                if(!DetectSystem.Increase(player.UserIDString, "manipulator"))
                    return;
                
                if (DetectSystem.Count(player.UserIDString, "manipulator") > 6)
                {
                    SendDetectDiscord($"{player.UserIDString} | {player.displayName} | {player.net.connection.ipaddress.Split(':')[0]}", "manipulator");

                    DetectSystem.Drop(player.UserIDString, "manipulator");

                    return;
                }
            }
            
            var veh = player.GetMountedVehicle();
            if(veh != null) {
                if((veh.PrefabName.Contains("minicopter") || veh.PrefabName.Contains("scraptransporthelicopter")) && veh.GetPlayerSeat(player) == 0) {
                    SendDetectDiscord($"{player.UserIDString} | {player.displayName} | {player.net.connection.ipaddress.Split(':')[0]}", "ShootWhileMounted");
                    veh.Hurt(float.MaxValue);
                    
                    if (config.ShootWhileMounted)
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", player.UserIDString, config.BanReason);
                    }
                }
            }
        }
        
        object OnPlayerLand(BasePlayer player, float num)
        {
            if (player == null || player.IsAdmin) return null;
            if (num == 1)
            {
                if(!DetectSystem.Increase(player.UserIDString, "fastkill"))
                    return null;

                if (DetectSystem.Count(player.UserIDString, "fastkill") > 3)
                {
                    SendDetectDiscord($"{player.UserIDString} | {player.displayName} | {player.net.connection.ipaddress.Split(':')[0]}", "fastkill");

                    DetectSystem.Drop(player.UserIDString, "fastkill");

                    if (config.BanForFastKill)
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", player.UserIDString, config.BanReason);
                    }
                    return null;
                }
            }
            return null;
        }
        
        // object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        // {
        //     if (player != null && type == AntiHackType.FlyHack)
        //     {
        //         
        //         // if (!player.IsAdmin)
        //         // {
        //         //     if (player.Connection != null && !player.Connection.IsRecording)
        //         //     {
        //         //         player.StartDemoRecording();
        //         //     }
        //         //     
        //         //     SendDetectDiscord($"{player.UserIDString} | {player.displayName} | {player.net.connection.ipaddress.Split(':')[0]}", "flyhack_violation");
        //         //     
        //         //     timer.Once(5f, () =>
        //         //     {
        //         //         if (player.Connection != null && player.Connection.IsRecording)
        //         //         {
        //         //             player.StopDemoRecording();
        //         //         }
        //         //     });                    
        //         // }
        //         
        //     }
        //     return null;
        // }
        
        object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
        {
            if(player == null || player.IsAdmin || !player.IsConnected || player.IsWounded() || player.IsSleeping() || player.IsDead() || Performance.report.frameRate < 60) return null;
            
            if (msg.inputState.aimAngles.x > 360 ||
                msg.inputState.aimAngles.x < -360 ||
                msg.inputState.aimAngles.y > 360 ||
                msg.inputState.aimAngles.y < -360)
            {
                if(!DetectSystem.Increase(player.UserIDString, "antiaim_type1"))
                    return null;

                if (DetectSystem.Count(player.UserIDString, "antiaim_type1") > 128)
                {
                    SendDetectDiscord($"{player.UserIDString} | {player.displayName} | {player.net.connection.ipaddress.Split(':')[0]}", "antiaim_type1");

                    DetectSystem.Drop(player.UserIDString, "antiaim_type1");
                    
                    if (config.BanForAntiAimDetectType1)
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", player.UserIDString, config.BanReason);
                    }
                    return null;
                }
            }

            return null;
        }
        
        /*void OnPlayerInput(BasePlayer player, InputState input)
        {
        	if(player == null || player.IsAdmin) return;
            
            float radius = player.GetRadius();
            float height = player.GetHeight(false);
            Vector3 currentPosition = player.transform.position;
            Vector3 lastPosition = player.lastReceivedTick.position;
            Vector3 vector = (lastPosition + currentPosition) * 0.5f;
            Vector3 vector2 = vector + new Vector3(0.0f, radius - 2.0f, 0.0f);
            Vector3 vector3 = vector + new Vector3(0.0f, height - radius, 0.0f);
            bool isBuildingNear = GamePhysics.CheckCapsule(vector2, vector3, 0.5f, 2097152, QueryTriggerInteraction.Ignore);
            if (isBuildingNear)
            {
                if (input.IsDown(BUTTON.JUMP) && !player.modelState.onLadder)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.transform.position, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain", "World", "Construction")))
                    {
                        if (hit.distance > 0.1f)
                        {
                            var ray = player.eyes.HeadRay();
                            RaycastHit lookray;

                            if (Physics.Raycast(ray, out lookray, 1f))
                            {
                                BaseEntity entity = lookray.GetEntity();
                                if (entity.ShortPrefabName == "wall" || entity.ShortPrefabName == "wall.half")
                                {
                                    if (hit.distance > 1.7)
                                    {
                                        if(!DetectSystem.Increase(player.UserIDString, "spider"))
                                            return;
                                        
                                        if (DetectSystem.Count(player.UserIDString, "spider") > 20)
                                        {
                                            SendDetectDiscord($"{player.UserIDString}\n" +
                                                              $"IP:{player.net.connection.ipaddress.Split(':')[0]}", "spider");
                        
                                            DetectSystem.Drop(player.UserIDString, "spider");
                                            
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            bool isOverWater = WaterLevel.Test(player.transform.position, 0.01f, player);
            float waterDepth = WaterLevel.GetOverallWaterDepth(player.transform.position, false, player);
            
            if (waterDepth > 1.2f && !player.isMounted)
            {
                if (isOverWater)
                {
                    if(!DetectSystem.Increase(player.UserIDString, "walk_on_water"))
                        return;
                
                    if (DetectSystem.Count(player.UserIDString, "walk_on_water") > 20)
                    {
                        SendDetectDiscord($"{player.UserIDString}\n" + 
                                          $"IP:{player.net.connection.ipaddress.Split(':')[0]}", "walk_water");
                        
                        DetectSystem.Drop(player.UserIDString, "walk_on_water");
                        
                        if (config.BanForWalkOnWater)
                        {
                            ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", player.UserIDString, config.BanReason);
                        }
                        return;
                    }
                }
            }
        }*/
        
        object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if(info.HitEntity == null || attacker.IsAdmin) return null;
            
            if (info.HitEntity is BasePlayer)
            {
                if(Vector3.Distance(attacker.transform.position, info.HitPositionWorld) < info.ProjectileDistance 
                   && Mathf.Abs(Vector3.Distance(attacker.transform.position, info.HitPositionWorld) - info.ProjectileDistance) >= 50)
                {
                    SendProjectileLog($"{attacker.UserIDString} | {attacker.displayName} | {attacker.net.connection.ipaddress.Split(':')[0]}", "invalid_hit_distance", info, attacker);
                    
                    if (config.BanForInvalidDistance)
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", attacker.UserIDString, config.BanReason);
                    }
                    return false;
                }
                
                // if(IsInteger(info.ProjectileDistance))
                // {
                //     SendProjectileLog($"{attacker.UserIDString} | {attacker.displayName} | {attacker.net.connection.ipaddress.Split(':')[0]}", "invalid_hit_distance", info, attacker);
                //     
                //     if (config.BanForInvalidDistance)
                //     {
                //         ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", attacker.UserIDString, config.BanReason);
                //     }
                //     return false;
                // }
                
                if (info.HitNormalLocal == Vector3.zero && info.HitNormalWorld == Vector3.zero) {
                    SendProjectileLog($"{attacker.UserIDString} | {attacker.displayName} | {attacker.net.connection.ipaddress.Split(':')[0]}", "bullet_teleport", info, attacker);
                    
                    if (config.BanForBulletTeleport)
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", attacker.UserIDString, config.BanReason);
                    }
                    return false;
                }
            
                if (info.HitMaterial != 1395914656) {
                    if(!DetectSystem.Increase(attacker.UserIDString, "invalid_hitmaterial")) return false;
                
                    if (DetectSystem.Count(attacker.UserIDString, "invalid_hitmaterial") > 3)
                    {
                        SendProjectileLog($"{attacker.UserIDString} | {attacker.displayName} | {attacker.net.connection.ipaddress.Split(':')[0]}", "invalid_hitmaterial", info, attacker);

                        DetectSystem.Drop(attacker.UserIDString, "invalid_hitmaterial");
                        
                        if (config.BanForIvalidHitMaterial)
                        {
                            ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", attacker.UserIDString, config.BanReason);
                        }
                        return false;
                    }
                }
                
            } else {
                if(info.HitEntity.name == "assets/prefabs/misc/orebonus/orebonus_generic.prefab") {
                    SendDetectDiscord($"{attacker.UserIDString} | {attacker.displayName} | {attacker.net.connection.ipaddress.Split(':')[0]})", "autofarm");

                    ConsoleSystem.Run(ConsoleSystem.Option.Server, "ban", attacker.UserIDString, config.BanReason);

                    return false;
                }
            }

            return null;
        }
    }
}