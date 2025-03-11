using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Auto Takeoff", "0x89A", "1.1.1")]
    [Description("Allows smooth takeoff with helicopters")]
    class AutoTakeoff : RustPlugin
    {
        #region -Fields-

        private const string _canUse = "autotakeoff.use";

        private readonly Dictionary<Minicopter, bool> _isTakingOff = new Dictionary<Minicopter, bool>();

        #endregion

        void Init()
        {
            permission.RegisterPermission(_canUse, this);
        }

        #region -Chat Command-

        [ChatCommand("takeoff")]
        private void TakeOffCommand(BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, _canUse))
            {
                player.ChatMessage(GetMessage("NoPermission", player.UserIDString));
                return;
            }

            Minicopter helicopter = player.GetMountedVehicle() as Minicopter;
            if (helicopter == null)
            {
                player.ChatMessage(GetMessage("NotMounted", player.UserIDString));
                return;
            }

            bool isTakingOff;
            if (_isTakingOff.TryGetValue(helicopter, out isTakingOff) && isTakingOff)
            {
                player.ChatMessage(GetMessage("AlreadyTakingOff", player.UserIDString));
                return;
            }

            string vehiclePrefab = helicopter.ShortPrefabName;

            if (!_config.scrapheliCanTakeoff && vehiclePrefab == "scraptransporthelicopter" || !_config.minicopterCanTakeoff && vehiclePrefab == "minicopter.entity")
            {
                player.ChatMessage(GetMessage("NotAllowed", player.UserIDString));
                return;
            }

            DoTakeOff(player, helicopter);
        }

        private void DoTakeOff(BasePlayer player, Minicopter helicopter)
        {
            if (helicopter.IsEngineOn() && helicopter.isMobile)
            {
                //raycast to check if on ground
                Ray ray = new Ray(helicopter.transform.position, -Vector2.up);

                if (Physics.Raycast(ray, 0.5f))
                {
                    if (_config.takeOffMethodType)
                    {
                        helicopter.StartCoroutine(LerpMethod(helicopter));
                        return;
                    }

                    PushMethod(helicopter);
                }
                else
                {
                    player.ChatMessage(GetMessage("NotOnGround", player.UserIDString));
                }

                return;
            }
            
            player.ChatMessage(GetMessage("NotFlying", player.UserIDString));
        }

        #endregion

        #region -Methods-

        private IEnumerator LerpMethod(Minicopter helicopter)
        {
            _isTakingOff[helicopter] = true;
            
            Vector3 helicopterPosition = helicopter.transform.position;
            Vector3 endPos = helicopterPosition + Vector3.up * _config.distanceMoved;

            float distance = Vector3.Distance(helicopterPosition, endPos);
				
            float speed = helicopter.ShortPrefabName == "minicopter.entity" ? _config.minicopterSpeed : _config.scrapHelicopterSpeed;

            float startTime = Time.time;

            while (helicopter.AnyMounted() && helicopter.IsEngineOn())
            {
                float distCovered = (Time.time - startTime) * speed;

                float fractionOfJourney = distCovered / distance;

                helicopter.transform.position = Vector3.Lerp(helicopter.transform.position, endPos, fractionOfJourney);

                if (helicopter.CenterPoint().y + 1 >= endPos.y - 2)
                {
                    _isTakingOff[helicopter] = false;
                    yield break;
                }

                yield return CoroutineEx.waitForFixedUpdate;
            }
        }

        private void PushMethod(Minicopter helicopter)
        {
            Rigidbody rb;
            if (!helicopter.TryGetComponent(out rb))
            {
                return;
            }

            float force = helicopter.ShortPrefabName == "minicopter.entity" ? _config.minicopterPushForce : _config.scrapHelicopterPushForce;
            rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
        }
        
        #endregion
        
        #region -Localization-

        private string GetMessage(string key, string userid) => lang.GetMessage(key, this, userid);
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["NotMounted"] = "You are not in a helicopter",
                ["NotOnGround"] = "You are too far from the ground",
                ["NotFlying"] = "The helicopter is not flying",
                ["AlreadyTakingOff"] = "This helicopter is already taking off",
                ["DefaultConfig"] = "Generating new config"
            }
            , this);
        }

        #endregion

        #region -Configuration-

        private Configuration _config;
        class Configuration
        {
            [JsonProperty(PropertyName = "Take off method type")]
            public bool takeOffMethodType = true;

            [JsonProperty(PropertyName = "Helicopter move distance")]
            public float distanceMoved = 10f;

            [JsonProperty(PropertyName = "Minicopter can auto takeoff")]
            public bool minicopterCanTakeoff = true;

            [JsonProperty(PropertyName = "Minicopter move speed")]
            public float minicopterSpeed = 0.025f;

            [JsonProperty(PropertyName = "Minicopter push force")]
            public float minicopterPushForce = 50;

            [JsonProperty(PropertyName = "Scrap Helicopter can auto takeoff")]
            public bool scrapheliCanTakeoff = true;

            [JsonProperty(PropertyName = "Scrap helicopter move speed")]
            public float scrapHelicopterSpeed = 0.0075f;

            [JsonProperty(PropertyName = "Scrap helicopter push force")]
            public float scrapHelicopterPushForce = 100;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new System.Exception();
                SaveConfig();
            }
            catch
            {
                PrintWarning("Error with config, using default values");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion
    }
}
