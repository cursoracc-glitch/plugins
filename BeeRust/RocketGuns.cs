using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System;
using System.Collections.Generic;
using Network;
using ProtoBuf;

namespace Oxide.Plugins
{
    [Info("RocketGuns", "Koks", "1.0.0")]
    [Description("RocketGuns")]
    public class RocketGuns : RustPlugin
    {
        private readonly List<string> Guns = new List<string>()
        {
            "rifle.ak",
            "rifle.ak.ice",
            "rifle.bolt",
            "hmlmg",
            "rifle.l96",
            "rifle.lr300",
            "lmg.m249",
            "rifle.m39",
            "rifle.semiauto"
        };
        private const string PermUse = "rocketguns.use";

        #region[Commands]
        [ChatCommand("t")]
        private void ToggleRocketCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), PermUse))
            {
                SendReply(player, lang.GetMessage("NoPerm", this, player.userID.ToString()));
                return;
            }
            if (ActiveGUNS.Contains(player.userID))
            {
                SendReply(player, lang.GetMessage("Off", this, player.userID.ToString()));
                ActiveGUNS.Remove(player.userID);
                DestroyCUI(player);
                return;
            }
            else
            {
                SendReply(player, lang.GetMessage("On", this, player.userID.ToString()));
                ActiveGUNS.Add(player.userID);
                CreateUi(player);
                return;
            }

        }
        #endregion[Commands]

        #region[Rocket prefabs]
        public string Rocket = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
        public string Hv = "assets/prefabs/ammo/rocket/rocket_hv.prefab";
        public string Fire = "assets/content/vehicles/mlrs/rocket_mlrs.prefab";
        #endregion[Rocket prefabs]

        #region GUI
        List<ulong> ActiveGUNS = new List<ulong>();
        void DestroyCUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "GunUI");
        }

        public void CreateUi(BasePlayer player)
        {
            DestroyCUI(player);
            CuiElementContainer elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.5 0.5 0.5 0.0" },
                RectTransform = { AnchorMin = config.ImageAnchorMin, AnchorMax = config.ImageAnchorMax }
            }, "Hud.Menu", "GunUI");
            elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiRawImageComponent {Color = config.ImageColor, Url = config.ImageUrlIcon},
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            CuiHelper.AddUi(player, elements);
        }

        #endregion GUI

        #region[Hooks]
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyCUI(player);
            }
            ActiveGUNS = null;
        }
        private void Init()
        {
            permission.RegisterPermission(PermUse, this);
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (ActiveGUNS.Contains(player.userID))
            {
                DestroyCUI(player);
            }
            else return;
        }
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectileShoot)
        {
            if (player == null) return;
            if (ActiveGUNS.Contains(player.userID) && Guns.Contains(projectile.GetItem()?.info?.shortname))
            {
                string ammo = string.Empty;
                float speed = 0;
                if (mod.name.Contains("hv"))
                {
                    ammo = Hv;
                    speed = config.HvSpeed;
                }
                if (config.UseFire && mod.name.Contains("fire"))
                {
                    ammo = Fire;
                    speed = config.FireSpeed;
                }
                if (mod.name.Contains("explosive"))
                {
                    ammo = Rocket;
                    speed = config.NormalSpeed;
                }
                var heldEntity = projectile.GetItem();
                projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
                projectile.SendNetworkUpdateImmediate();
                int projectileID = projectileShoot.projectiles[0].projectileID;
                if (string.IsNullOrEmpty(ammo)) { return; }
                FireRockets(player, speed, ammo);
            }
            else return;
        }
        void OnLoseCondition(Item item, ref float amount)
        {               
            if (item == null) return;                                     
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null) return;
            if(ActiveGUNS.Contains(player.userID))amount = 0;
        }
        #endregion[Hooks]

        #region[Core]
        public void FireRockets(BasePlayer player, float speed, string ammo)
        {
            if (player == null) return;
            var rocket = GameManager.server.CreateEntity(ammo, player.eyes.position, new Quaternion());
            if (rocket != null)
            {
                rocket.creatorEntity = player;
                rocket.SendMessage("InitializeVelocity", player.eyes.HeadForward() * speed);
                rocket.OwnerID = player.userID;
                rocket.Spawn();
                rocket.ClientRPC(null, "RPCFire");
                return;
            }
        }
        #endregion[Core]

        #region Config
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Allow fire rockets (may cause server lag if too many fired)")]
            public bool UseFire = false;

            [JsonProperty("Hv rocket speed ")]
            public float HvSpeed = 200;

            [JsonProperty("Normal rocket speed")]
            public float NormalSpeed = 100;

            [JsonProperty("Fire rocket speed")]
            public float FireSpeed = 100;

            [JsonProperty("Icon URL (.png or .jpg)")]
            public string ImageUrlIcon = "https://www.citypng.com/public/uploads/preview/fire-explosion-mushroom-cartoon-illustration-hd-png-11665511403qjc2bf8lnu.png";

            [JsonProperty("Image Color")]
            public string ImageColor = "1 1 1 1";

            [JsonProperty("Image AnchorMin")]
            public string ImageAnchorMin = "0.645 0.023";

            [JsonProperty("Image AnchorMax")]
            public string ImageAnchorMax = "0.688 0.095";

        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }
        protected override void LoadDefaultConfig() => config = new Configuration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region[Localization]
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPerm"] = "You dont have permission to use this command",
                ["Off"] = "Raid gun disengaged",
                ["On"] = "Raid gun engaged"
            }, this);
        }
        #endregion[Localization]
    }
}
