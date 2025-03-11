using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Invalid Distance Detector", "HiTTA", "1.3.0")]
    [Description("Bans suspicious players based on weapon distances")]
    class InvalidDistanceDetector : CovalencePlugin
    {
        #region Configuration

        private Configuration _config;

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_config, true);
        }

        private class Configuration
        {
            [JsonProperty("Compound Bow Distance")]
            public float CompoundBow = 100;

            [JsonProperty("Bow Distance")]
            public float Bow = 100;

            [JsonProperty("Crossbow Distance")]
            public float Crossbow = 100;

            [JsonProperty("M249 Distance")]
            public float M249 = 350;

            [JsonProperty("Eoka Pistol Distance")]
            public float EokaPistol = 50;

            [JsonProperty("M92 Pistol Distance")]
            public float M92Pistol = 150;

            [JsonProperty("Nailgun Distance")]
            public float Nailgun = 75;

            [JsonProperty("Python Revolver Distance")]
            public float PythonRevolver = 150;

            [JsonProperty("Revolver Distance")]
            public float Revolver = 125;

            [JsonProperty("Semi-Automatic Pistol Distance")]
            public float SemiAutoPistol = 125;

            [JsonProperty("Assault Rifle Distance")]
            public float AssaultRifle = 300;

            [JsonProperty("Bolt Action Rifle Distance")]
            public float BoltActionRifle = 450;

            [JsonProperty("L96 Distance")]
            public float L96Rifle = 450;

            [JsonProperty("LR-300 Assault Rifle distance")]
            public float LR300AssaultRifle = 300;

            [JsonProperty("M39 Rifle distance")]
            public float M39Rifle = 350;

            [JsonProperty("Semi-Automatic Rifle distance")]
            public float SemiAutomaticRifle = 250;

            [JsonProperty("MP5A4 distance")]
            public float MP5A4 = 300;

            [JsonProperty("Thompson distance")]
            public float Thompson = 300;
        }

        #endregion Configuration

        private void OnEntityTakeDamage(BasePlayer victim, HitInfo hitInfo)
        {
            if (hitInfo.IsProjectile() && victim.userID.IsSteamId())
            {
                BasePlayer attacker = hitInfo.Initiator as BasePlayer;
                HeldEntity heldEntity = attacker.GetHeldEntity();
                Item heldItem = heldEntity.GetItem();
                float distance = hitInfo.ProjectileDistance;
                bool bannableDistance = false;

                switch (heldEntity.ShortPrefabName)
                {
                    case "bow.compound":
                        if (distance > _config.CompoundBow) bannableDistance = true;
                        break;

                    case "bow.hunting":
                        if (distance > _config.Bow) bannableDistance = true;
                        break;

                    case "crossbow":
                        if (distance > _config.Crossbow) bannableDistance = true;
                        break;

                    case "lmg.m249":
                        if (distance > _config.M249) bannableDistance = true;
                        break;

                    case "pistol.eoka":
                        if (distance > _config.EokaPistol) bannableDistance = true;
                        break;

                    case "pistol.m92":
                        if (distance > _config.M92Pistol) bannableDistance = true;
                        break;

                    case "pistol.nailgun":
                        if (distance > _config.Nailgun) bannableDistance = true;
                        break;

                    case "pistol.python":
                        if (distance > _config.PythonRevolver) bannableDistance = true;
                        break;

                    case "pistol.revolver":
                        if (distance > _config.Revolver) bannableDistance = true;
                        break;

                    case "pistol.semiauto":
                        if (distance > _config.SemiAutoPistol) bannableDistance = true;
                        break;

                    case "rifle.ak":
                        if (distance > _config.AssaultRifle) bannableDistance = true;
                        break;

                    case "rifle.bolt":
                        if (distance > _config.BoltActionRifle) bannableDistance = true;
                        break;

                    case "rifle.l96":
                        if (distance > _config.L96Rifle) bannableDistance = true;
                        break;

                    case "rifle.lr300":
                        if (distance > _config.LR300AssaultRifle) bannableDistance = true;
                        break;

                    case "rifle.m39":
                        if (distance > _config.M39Rifle) bannableDistance = true;
                        break;

                    case "rifle.semiauto":
                        if (distance > _config.SemiAutomaticRifle) bannableDistance = true;
                        break;

                    case "smg.mp5":
                        if (distance > _config.MP5A4) bannableDistance = true;
                        break;

                    case "smg.thompson":
                        if (distance > _config.Thompson) bannableDistance = true;
                        break;
                }

                if (bannableDistance)
                {
                    server.Ban(attacker.UserIDString, $"Anti Cheat: (Invalid Distance! - {heldItem.info.displayName.english})");
                    Log($"Attacker: {attacker.displayName} ({attacker.userID}) | Hit distance: {distance} | Weapon: {heldItem} | Victim: {victim.displayName} ({victim.userID})");
                }
            }
        }
    }
}