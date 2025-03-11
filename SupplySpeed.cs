using UnityEngine;
using Newtonsoft.Json;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Supply Speed", "rustmods.ru.", "1.0.1")]

    public class SupplySpeed : RustPlugin
    {
        #region Конфиг

        private Configuration _config;
        
        public class Configuration 
        {
            [JsonProperty("Во сколько раз ускорять аирдроп?")]
            public float speed = 10f;

            [JsonProperty("Через сколько секунд будет удаляться шашка с дымом? (обычно 210 сек.)")]
            public float grenadeDespawn = 60f;
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadConfig()
        {
            base.LoadConfig(); 

            try 
            {
                _config = Config.ReadObject<Configuration>();
            } 
            catch 
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        private void OnEntitySpawned(SupplyDrop supplyDrop) 
        {
            var setting = supplyDrop.gameObject.AddComponent<DropSettings>();
            setting.windSpeed = _config.speed;
        }

        private void OnEntitySpawned(SupplySignal signal)
        {
            signal.CancelInvoke(signal.FinishUp);
            signal.gameObject.AddComponent<SignalSettings>().ActivateDestroyer(_config.grenadeDespawn);
        }

        private class SignalSettings : MonoBehaviour
        {
            public void ActivateDestroyer(float duration) => StartCoroutine(KillCoroutine(duration));

            private IEnumerator KillCoroutine(float duration)
            {
                for(;;)
                {
                    yield return new WaitForSeconds(duration);
                    GetComponent<SupplySignal>().Kill();
                }
            }
        }

        private class DropSettings : MonoBehaviour
        {
            private SupplyDrop supplyDrop;
            private BaseEntity chute;

            private Vector3 windDir, newDir;

            public float windSpeed = 1f;

            private void Awake()
            {
                supplyDrop = GetComponent<SupplyDrop>();
                if (supplyDrop == null) Destroy(this);

                chute = supplyDrop.parachute;
                windDir = GetDirection();
            }

            private Vector3 GetDirection()
            {
                var direction = Random.insideUnitSphere * 0f;
                if (direction.y > -windSpeed) direction.y = -windSpeed;

                return direction;
            }

            private void FixedUpdate()
            {
                if (chute == null || supplyDrop == null) Destroy(this);

                newDir = Vector3.RotateTowards(transform.forward, windDir, 0.5f * Time.deltaTime, 0.0F);
                newDir.y = 0f;

                supplyDrop.transform.position = Vector3.MoveTowards(transform.position, transform.position + windDir, windSpeed * Time.deltaTime);
                supplyDrop.transform.rotation = Quaternion.LookRotation(newDir);
            }
        }
    }
}