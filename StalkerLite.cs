using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("Stalker Emission", "Unknown", "1.0.17")]
    [Description("На всей карте появляется радиация, что сопровождается ужудшением погоды, звуками и визуальными эфектами")]
    public class StalkerLite : RustPlugin
    { 
		#region Config

        private bool active;

        private double last = Now();

        private const int
            shockNum = 30, explosionsNum = 15, fireNum = 3, // Количество эффектов
            shockMin = -100, shockMax = 100, explosionMin = -100, explosionMax = 100, fireMin = -30, fireMax = 60;

        private float
            cooldown = 3600f, duration = 180f, delay = 15f, // Общие задержи: Интервал, Длительность, Задержка перед выбросом
            radDelay = 2.5f, shockDelay = 1.5f, explosionDelay = 2.5f, fireDelay = 4.5f, // Задежка между повторным появлением эффекта
            radAmount = 10f; // Интенсивность радиации

        private const string
            firePrefab = "assets/bundled/prefabs/fireball.prefab",
            electricPrefab = "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab",
            explosionPrefab = "assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion_incendiary.prefab",
            preMessage = "<size=15>Через <color=#BEF781>{0} сек.</color> будет выброс радиации!\nНайдите костюм или укрытие!</size>",
            emission = "<size=15><color=#FFA500>!!! НАЧАЛСЯ ВЫБРОС РАДИАЦИИ !!!</color>\nНайдите костюм или укрытие!</size>",
			endemission = "<size=15><color=#FFA500>ВЫБРОС РАДИАЦИИ ЗАВЕРШЕН!</color>\nВы в безопасности!</size>",
            safe = "<size=15><color=#32CD32>Вы в безопасности!</color></size>",
            nonsafe = "<size=15><color=#ce422b>Вы в опасности!</color>\nНайдите костюм или укрытие!</size>",
            permRad = "stalkerlite.use",
			prefix = "<color=#66ff66>[Stalker]</color>";

		protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации...");
        }

		private void LoadConfigValues()
        {
            GetConfig("Интервал между выбросами (в секундах)", ref cooldown);
			GetConfig("Длительность выброса (в секундах)", ref duration);
			GetConfig("Задержка перед выбросом (в секундах)", ref delay);
			GetConfig("Интенсивность радиации", ref radAmount);
			GetConfig("Частота появления эфекта радиации", ref radDelay);
			GetConfig("Частота появления эфекта молнии", ref shockDelay);
			GetConfig("Частота появления эфекта взрыва", ref explosionDelay);
			GetConfig("Частота появления эфекта огня", ref fireDelay);

			SaveConfig();
		}

        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }

        #endregion

        #region Commands

        [ChatCommand("rad")]
        private void CmdRad(BasePlayer player)
        {
            if (active)
            {
                player.ChatMessage($"<size=15>Выброс завершится через <color=#BEF781>{Math.Round(duration-(Now() - last))} сек.</color></size>");
                return;
            }
            
            player.ChatMessage($"<size=15>До следующего выброса осталось <color=#BEF781>{Math.Round(cooldown - (Now() - last))} сек.</color></size>");
        }

        [ChatCommand("stalker")]
        private void ChatCmd(BasePlayer player)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, permRad))
            {
                player.ChatMessage("[Управление] У вас нет прав на использование данной команды!");
                return;
            }

            if (active)
            {
                player.ChatMessage($"{prefix} Выброс прекращён!");
                StopEmission();
                return;
            }

            player.ChatMessage($"{prefix} Выброс запущен!");
            StartEmission();
        }

        #endregion

        #region Oxide Hooks

		private void Loaded()
        {
            LoadConfigValues();
        }	

        private void OnServerInitialized()
        {
            timer.Once(cooldown, () => { StartEmission(); });
            permission.RegisterPermission(permRad, this);
        }

        private void Unload()
        {
            StopEmission();
        }

        #endregion

        #region Core

        private List<ulong> Protected = new List<ulong>();

        public Timer Radiation, Explosion, Fire, Shock, CheckProtection;

        private void DestroyTimers()
        {
            if (Radiation != null) timer.Destroy(ref Radiation);
            if (Explosion != null) timer.Destroy(ref Explosion);
            if (Fire != null) timer.Destroy(ref Fire);
            if (Shock != null) timer.Destroy(ref Shock);
            if (CheckProtection != null) timer.Destroy(ref CheckProtection);
        }

        private Random rand = new Random();

        private void StartEmission()
        {
            if (active)
            {
                StopEmission();
                return;
            }

			Server.Command("weather.fog 1");
			Server.Command("weather.wind 1");
			Server.Command("weather.rain 1");
			Server.Command("weather.clouds 1");
            Server.Broadcast(string.Format(preMessage, delay));
            active = true;
            timer.Once(delay, () =>{Emission();});
        }

        private void StopEmission()
        {
            if (!active) return;

            DestroyTimers();
            active = false;
            Server.Command("weather.fog 0");
			Server.Command("weather.wind 0");
			Server.Command("weather.rain 0");
			Server.Command("weather.clouds 0");			
            foreach (var player in BasePlayer.activePlayerList) player.metabolism.radiation_level.value = 0;
			Server.Broadcast(endemission);
            timer.Once(cooldown, () => { StartEmission(); });
        }

        private void Emission()
        {
            if(!active) return;

            last = Now();
            timer.Once(duration, () =>{ StopEmission(); });

            Server.Broadcast(emission);

            CheckProtection = timer.Every(1f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    player.metabolism.radiation_level.value = 100; // TODO: Radiation Zone?
                    
                    if (bProtected(player))
                    {
                        if (!Protected.Contains(player.userID))
                        {
                            player.ChatMessage(safe);
                            Protected.Add(player.userID);
                        }
                    }
                    else
                    {
                        if (Protected.Contains(player.userID))
                        {
                            player.ChatMessage(nonsafe);
                            Protected.Remove(player.userID);
                        }
                    }
                }
            });

            Radiation = timer.Every(radDelay, () =>
            {
                foreach (var player in BasePlayer.activePlayerList) 
                {
                    if(Protected.Contains(player.userID)) continue;

                    player.metabolism.radiation_poison.value += radAmount;
                }
            });

            Explosion = timer.Every(explosionDelay, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    for (int i = 1; i < explosionsNum + 1; i++)
                    {
                        Effect.server.Run(explosionPrefab, 
                            new Vector3(player.transform.position.x + rand.Next(explosionMin, explosionMin), 
                                player.transform.position.y - 150, player.transform.position.z + rand.Next(explosionMin, explosionMax)));
                    }
                }
            });

            Shock =  timer.Every(shockDelay, () => 
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    for (var i = 1; i < shockNum + 1; i++)
                    {
                        Effect.server.Run(electricPrefab, 
                            new Vector3(player.transform.position.x + rand.Next(shockMin, shockMax), 
                                player.transform.position.y + rand.Next(0, 10), player.transform.position.z + rand.Next(shockMin, shockMax)));
                    }
                }
            });

            Fire = timer.Every(fireDelay, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    for (var i = 1; i < fireNum + 1; i++)
                    {
                        GameManager.server.CreateEntity(firePrefab, 
                            new Vector3(
                                player.transform.position.x + rand.Next(fireMin, fireMax), 
                                player.transform.position.y + rand.Next(0, 10), 
                                player.transform.position.z + rand.Next(fireMin, fireMax)))
                            .Spawn();
                    }
                }
            });
        }

        private bool bProtected(BasePlayer player)
        {
            foreach (var item in player.inventory.containerWear.itemList) if (item.info.shortname.Contains("hazmat")) return true;

            RaycastHit rHit;
            if (Physics.Raycast(player.transform.position , Vector3.up, out rHit, 4f,LayerMask.GetMask("Construction")) && rHit.GetEntity() != null)
            {
                var item = rHit.GetEntity() as BuildingBlock;
                if (item == null) return false;

                return item.grade != BuildingGrade.Enum.Wood && item.grade != BuildingGrade.Enum.Twigs;
            }

            return false;
        }

        private static double Now() {return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;}

        #endregion
    }
}
