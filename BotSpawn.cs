using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using Facepunch;
using Rust;

namespace Oxide.Plugins

{
    [Info("BotSpawn", "Steenamaroo", "1.6.8", ResourceId = 2580)]

    [Description("Spawn tailored AI with kits at monuments, custom locations, or randomly.")]

    class BotSpawn : RustPlugin
    {
        [PluginReference]
        Plugin Vanish, Kits;

        const string permAllowed = "botspawn.allowed";
        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);
        int no_of_AI = 0;
        static System.Random random = new System.Random();

        public Dictionary<string, List<Vector3>> spawnLists = new Dictionary<string, List<Vector3>>();

        public Timer aridTimer;
        public Timer temperateTimer;
        public Timer tundraTimer;
        public Timer arcticTimer;

        bool isBiome(string name)
        {
            if (name == "BiomeArid" || name == "BiomeTemperate" || name == "BiomeTundra" || name == "BiomeArctic")
                return true;
            return false;
        }

        #region Data 
        class StoredData
        {
            public Dictionary<string, DataProfile> DataProfiles = new Dictionary<string, DataProfile>();
            public Dictionary<string, ProfileRelocation> MigrationDataDoNotEdit = new Dictionary<string, ProfileRelocation>();
            public StoredData()
            {
            }
        }

        ///////////////////////////////////////////////////////////////////MIGRATION INFO TO REMOVE in 1.6.7
        class StoredDataOld
        {
            public Dictionary<string, MonumentSettings> CustomProfiles = new Dictionary<string, MonumentSettings>();
            public StoredDataOld()
            {
            }
        }
        public class MonumentSettings
        {
            public bool AutoSpawn = false;
            public bool Murderer = false;
            public int Bots = 5;
            public int BotHealth = 100;
            public int Radius = 100;
            public List<string> Kit = new List<string>();
            public string BotNamePrefix = "";
            public List<string> BotNames = new List<string>();
            public int Bot_Accuracy = 4;
            public float Bot_Damage = 0.4f;
            public int Respawn_Timer = 60;
            public bool Disable_Radio = true;
            public float LocationX;
            public float LocationY;
            public float LocationZ;
            public int Roam_Range = 40;
            public bool Peace_Keeper = true;
            public int Peace_Keeper_Cool_Down = 5;
            public bool Weapon_Drop = true;
            public bool Keep_Default_Loadout = false;
            public bool Wipe_Belt = true;
            public bool Wipe_Clothing = true;
            public bool Allow_Rust_Loot = true;
            public int Suicide_Timer = 300;
            public bool Chute = false;
            public int Long_Attack_Distance = 120;
        }
        StoredDataOld storedDataOld;
        ///////////////////////////////////////////////////////////////////////////

        StoredData storedData;
        #endregion

        void Init()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            };
            var filter = RustExtension.Filter.ToList();                                                                                                     //Thanks Fuji. :)
            filter.Add("cover points");
            filter.Add("resulted in a conflict");
            RustExtension.Filter = filter.ToArray();
            no_of_AI = 0;
            LoadConfigVariables();
        }

        void OnServerInitialized()
        {
            FindMonuments();
        }

        void Loaded()
        {
            spawnLists.Add("AridSpawns", new List<Vector3>());
            spawnLists.Add("TemperateSpawns", new List<Vector3>());
            spawnLists.Add("TundraSpawns", new List<Vector3>());
            spawnLists.Add("ArcticSpawns", new List<Vector3>()); 

            lang.RegisterMessages(messages, this);
            permission.RegisterPermission(permAllowed, this);

            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("BotSpawn");

            ///////////////////////////////////////////////////////////////////////////MIGRATION INFO TO REMOVE in 1.6.7
            storedDataOld = Interface.Oxide.DataFileSystem.ReadObject<StoredDataOld>("BotSpawn");
            var oldDeser = JsonConvert.SerializeObject(storedDataOld);
            if (oldDeser.ToString().Length != 28 && oldDeser.ToString().Length != 26)
            {
                oldDeser = oldDeser.Replace(("\"CustomProfiles\":"), ("\"DataProfiles\":"));
                int lastPos = oldDeser.LastIndexOf('}');
                oldDeser = oldDeser.Insert(lastPos, ",\"MigrationDataDoNotEdit\":{}");
                var migratedData = JsonConvert.DeserializeObject<StoredData>(oldDeser);
                Interface.Oxide.DataFileSystem.WriteObject("BotSpawn", migratedData); 
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("BotSpawn");
                foreach (var profile in storedData.DataProfiles)
                {
                    storedData.MigrationDataDoNotEdit.Add(profile.Key, new ProfileRelocation());
                };
            }
            ///////////////////////////////////////////////////////////////////////////  
            SaveData();
        }
        void Unload()
        {
            var filter = RustExtension.Filter.ToList();
            filter.Remove("cover points");
            filter.Remove("resulted in a conflict");
            RustExtension.Filter = filter.ToArray();
            Wipe();
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 2)
                    return false;
            return true;
        }

        void UpdateRecords(NPCPlayerApex player)
        {
            if (TempRecord.NPCPlayers.Contains(player))
                TempRecord.NPCPlayers.Remove(player);
        }

        void Wipe()
        {
            foreach (var bot in TempRecord.NPCPlayers)
            {
                if (bot == null)
                    continue;
                else
                    bot.Kill();
            }
            TempRecord.NPCPlayers.Clear();
        }

        // Facepunch.RandomUsernames
        public static string Get(ulong v)                                                                                                                      //credit Fujikura.
        {
            return Facepunch.RandomUsernames.Get((int)(v % 2147483647uL));
        }

        #region BiomeSpawnsSetup
        void GenerateSpawnPoints(List<Vector3> spawnlist, string name, int number, Timer myTimer, int biomeNo)
        {
            int getBiomeAttempts = 0;
            myTimer = timer.Repeat(0.1f, 0, () =>
            {
                int halfish = Convert.ToInt16((ConVar.Server.worldsize / 2) / 1.1f);

                int x = x = random.Next(-halfish, halfish);
                int z = random.Next(-halfish, halfish);
                Vector3 randomSpot = new Vector3(x, 0, z);
                bool finished = true;

                if (spawnlist.Count < number)
                {
                    getBiomeAttempts++;
                    if (getBiomeAttempts > 50 && spawnlist.Count == 0)  
                    {
                        Puts($"Failed to find spawnpoints in {name}."); 
                        myTimer.Destroy();
                        return;
                    }

                    finished = false;
                    x = random.Next(-halfish, halfish);
                    z = random.Next(-halfish, halfish);
                    if (TerrainMeta.BiomeMap.GetBiome(randomSpot, biomeNo) > 0.5f
                        && CalculateGroundPos(new Vector3(randomSpot.x, 200, randomSpot.z), true) != new Vector3())
                        spawnlist.Add(CalculateGroundPos(new Vector3(randomSpot.x, 200, randomSpot.z), true));
                }
                if (finished)
                {
                    int i = 0;
                    timer.Repeat(2, number, () =>
                    {
                        SpawnBots(name, TempRecord.AllProfiles[name], "biome", null, spawnlist[i]);
                        i++;
                    });
                    myTimer.Destroy();
                }
            });
        }

        public static Vector3 CalculateGroundPos(Vector3 sourcePos, bool Biome)                                                                                                      //credit Wulf & Nogrod 
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, 1000f, LayerMask.GetMask("Terrain", "World", "Construction", "Water"), QueryTriggerInteraction.Ignore))
            {
                if (hitInfo.collider.tag == "Main Terrain" && Biome)
                {
                    sourcePos.y = hitInfo.point.y;
                    sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
                    return sourcePos;
                }
                if (!Biome)
                {
                    sourcePos.y = hitInfo.point.y;
                    sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
                    return sourcePos;
                }
            }
            return new Vector3();
        }

        Vector3 TryGetSpawn(Vector3 centerPoint, int radius)
        {
            int attempts = 0;
            var spawnPoint = new Vector3();

            while (attempts < 50 && spawnPoint == new Vector3())
            {
                attempts++;
                RaycastHit hitInfo;

                int X = random.Next((-radius), (radius));
                int Z = random.Next((-radius), (radius));
                if (CalculateGroundPos(new Vector3((centerPoint.x + X), 200, (centerPoint.z + Z)), false) != new Vector3())
                {
                    spawnPoint = CalculateGroundPos(new Vector3(centerPoint.x + X, 200, centerPoint.z + Z), false);
                }
            }
            return spawnPoint;
        }
        #endregion


        #region BotSetup
        void AttackPlayer(Vector3 location, string name, DataProfile profile, string group)
        {
            timer.Repeat(1f, profile.Bots, () => SpawnBots(name, profile, "Attack", group, location));
        }

        void SpawnBots(string name, DataProfile zone, string type, string group, Vector3 location)
        {
            var pos = new Vector3(zone.LocationX, zone.LocationY, zone.LocationZ);
            var finalPoint = new Vector3();
            if (location != new Vector3())
                pos = location;

            var randomTerrainPoint = TryGetSpawn(pos, zone.Radius);
            if (randomTerrainPoint == new Vector3())
            {
                Puts($"Failed to find a suitable spawnpoint : Moving on to next. {name}");
                return;
            }
            else
            {
                finalPoint = randomTerrainPoint;
            }

            if (zone.Chute)
            {
                if (type == "AirDrop")
                    finalPoint = (pos - new Vector3(0, -40, 0));
                else
                    finalPoint = new Vector3(randomTerrainPoint.x, 200, (randomTerrainPoint.z));
            }

            NPCPlayer entity = (NPCPlayer)InstantiateSci(finalPoint, Quaternion.Euler(0, 0, 0), zone.Murderer);
            var botapex = entity.GetComponent<NPCPlayerApex>();

            TempRecord.NPCPlayers.Add(botapex);

            botapex.Spawn();
            botapex.SendNetworkUpdateImmediate();

            no_of_AI++;

            var bData = botapex.gameObject.AddComponent<botData>();
            if (group != null)
                bData.group = group;
            else
                bData.group = null;

            bData.spawnPoint = CalculateGroundPos(randomTerrainPoint, false);
            bData.accuracy = zone.Bot_Accuracy;
            bData.damage = zone.Bot_Damage;
            bData.health = zone.BotHealth;
            bData.monumentName = name;
            bData.respawn = true;
            bData.roamRange = zone.Roam_Range;
            bData.dropweapon = zone.Weapon_Drop;
            bData.keepAttire = zone.Keep_Default_Loadout;
            bData.peaceKeeper = zone.Peace_Keeper;
            bData.chute = zone.Chute;
            bData.peaceKeeper_CoolDown = zone.Peace_Keeper_Cool_Down;
            if (type == "biome")
                bData.biome = true;
            if (zone.Long_Attack_Distance < 55) zone.Long_Attack_Distance = 55;
            bData.LongRangeAttack = zone.Long_Attack_Distance;

            if (zone.Chute)
                addChute(botapex, finalPoint);

            int kitRnd;
            kitRnd = random.Next(zone.Kit.Count);

            if (zone.BotNames.Count == zone.Kit.Count && zone.Kit.Count != 0)
                setName(zone, botapex, kitRnd);
            else
                setName(zone, botapex, random.Next(zone.BotNames.Count));

            giveKit(botapex, zone, kitRnd);

            sortWeapons(botapex);

            int suicInt = random.Next((zone.Suicide_Timer), (zone.Suicide_Timer + 10));                                        //slightly randomise suicide de-spawn time
            if (type == "AirDrop" || type == "Attack")
            {
                bData.respawn = false;
                runSuicide(botapex, suicInt);
            }

            if (zone.Disable_Radio)
                botapex.RadioEffect = new GameObjectRef();

            botapex.Stats.VisionRange = zone.Long_Attack_Distance + 20f; // to allow out of range attack over ride  

            if (!zone.Murderer)
            {
                timer.Once(4, () =>
                {
                    if (botapex != null)
                    {
                        botapex.Stats.IsMobile = true;
                        botapex.SetFact(NPCPlayerApex.Facts.IsRoamReady, 1, true, true);
                        botapex.SetFact(NPCPlayerApex.Facts.IsMoving, 1, true, true);
                    }
                });
            }
        }

        BaseEntity InstantiateSci(Vector3 position, Quaternion rotation, bool murd)                                                                            //Spawn population spam fix - credit Fujikura
        {
            string prefabname = "assets/prefabs/npc/scientist/scientist.prefab";
            if (murd)
                prefabname = "assets/prefabs/npc/murderer/murderer.prefab";

            var prefab = GameManager.server.FindPrefab(prefabname);
            GameObject gameObject = Instantiate.GameObject(prefab, position, rotation);
            gameObject.name = prefabname;
            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);
            if (gameObject.GetComponent<Spawnable>())
                UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }

        void addChute(NPCPlayerApex botapex, Vector3 newPos)
        {
            float wind = random.Next(0, 50);
            float fall = random.Next(40, 80);
            var rb = botapex.gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.drag = 0f;
            botapex.gameObject.layer = 3;

            rb.velocity = new Vector3((botapex.transform.forward.x * (wind / 20f)), 0, (botapex.transform.forward * (wind / 20f)).z) + new Vector3(0, -(fall / 10f), 0);

            var col = botapex.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1, 0.5f, 1);  //feet above ground

            var Chute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", newPos, Quaternion.Euler(0, 0, 0));
            Chute.gameObject.Identity();
            Chute.SetParent(botapex, "parachute");
            Chute.Spawn();
        }

        void setName(DataProfile zone, NPCPlayerApex botapex, int number)
        {
            if (zone.BotNames.Count == 0 || zone.BotNames.Count <= number)
            {
                botapex.displayName = Get(botapex.userID);
                botapex.displayName = char.ToUpper(botapex.displayName[0]) + botapex.displayName.Substring(1);
            }
            else if (zone.BotNames[number] == "")
            {
                botapex.displayName = Get(botapex.userID);
                botapex.displayName = char.ToUpper(botapex.displayName[0]) + botapex.displayName.Substring(1);
            }
            else
                botapex.displayName = zone.BotNames[number];

            if (zone.BotNamePrefix != "")
                botapex.displayName = zone.BotNamePrefix + " " + botapex.displayName;
        } 

        void giveKit(NPCPlayerApex botapex, DataProfile zone, int kitRnd)
        {
            var bData = botapex.GetComponent<botData>();

            if (zone.Kit.Count != 0)
            {
                if (zone.Kit[kitRnd] != null)  
                {
                    object checkKit = (Kits.CallHook("GetKitInfo", zone.Kit[kitRnd], true));
                    if (checkKit == null) 
                    {
                        if (zone.Murderer)
                            PrintWarning($"Kit {zone.Kit[kitRnd]} does not exist - Spawning default Murderer.");
                        else
                            PrintWarning($"Kit {zone.Kit[kitRnd]} does not exist - Spawning default Scientist.");
                    }
                    else
                    {
                        bool weaponInBelt = false;
                        if (checkKit != null && checkKit is JObject)
                        {
                            JObject kitContents = checkKit as JObject;

                            JArray items = kitContents["items"] as JArray;
                            foreach (var weap in items)
                            {
                                JObject item = weap as JObject;
                                if (item["container"].ToString() == "belt")
                                    weaponInBelt = true;                                                                                                    //doesn't actually check for weapons - just any item.
                            }
                        }
                        if (!weaponInBelt)
                        {
                            if (zone.Murderer)
                                PrintWarning($"Kit {zone.Kit[kitRnd]} has no items in belt - Spawning default Murderer.");
                            else
                                PrintWarning($"Kit {zone.Kit[kitRnd]} does not exist - Spawning default Scientist.");
                        }
                        else
                        {
                            if (bData.keepAttire == false)
                                botapex.inventory.Strip();
                            Kits.Call($"GiveKit", botapex, zone.Kit[kitRnd], true);
                            if (!(kitList.ContainsKey(botapex.userID)))
                            {
                                kitList.Add(botapex.userID, new kitData
                                {
                                    Kit = zone.Kit[kitRnd],
                                    Wipe_Belt = zone.Wipe_Belt,
                                    Wipe_Clothing = zone.Wipe_Clothing,
                                    Allow_Rust_Loot = zone.Allow_Rust_Loot,
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                if (!(kitList.ContainsKey(botapex.userID)))
                {
                    kitList.Add(botapex.userID, new kitData
                    {
                        Kit = "",
                        Wipe_Belt = zone.Wipe_Belt,
                        Wipe_Clothing = zone.Wipe_Clothing,
                        Allow_Rust_Loot = zone.Allow_Rust_Loot,
                    });
                }
            }
        }

        void sortWeapons(NPCPlayerApex botapex)
        {
            var bData = botapex.GetComponent<botData>();
            foreach (Item item in botapex.inventory.containerBelt.itemList)                                                                                 //store organised weapons lists
            {
                var held = item.GetHeldEntity();
                if (held as HeldEntity != null)
                {
                    if (held.name.Contains("bow") || held.name.Contains("launcher"))
                        continue;
                    if (held as BaseMelee != null || held as TorchWeapon != null)
                        bData.MeleeWeapons.Add(item);
                    else
                    {
                        if (held as BaseProjectile != null)
                        {
                            bData.AllProjectiles.Add(item);
                            if (held.name.Contains("m92") || held.name.Contains("pistol") || held.name.Contains("python") || held.name.Contains("waterpipe"))
                                bData.CloseRangeWeapons.Add(item);
                            else if (held.name.Contains("bolt"))
                                bData.LongRangeWeapons.Add(item);
                            else
                                bData.MediumRangeWeapons.Add(item);
                        }
                    }
                }
            }
        }

        void runSuicide(NPCPlayerApex botapex, int suicInt)
        {
            if (TempRecord.NPCPlayers.Contains(botapex))
            {
                timer.Once(suicInt, () =>
                {
                    if (botapex != null)
                    {
                        if (botapex.AttackTarget != null && Vector3.Distance(botapex.transform.position, botapex.AttackTarget.transform.position) < 10 && botapex.GetNavAgent.isOnNavMesh)
                        {
                            if (botapex.AttackTarget != null)
                            {
                                var position = botapex.AttackTarget.transform.position;
                                botapex.svActiveItemID = 0;
                                botapex.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                botapex.inventory.UpdatedVisibleHolsteredItems();
                                timer.Repeat(0.05f, 100, () =>
                                {
                                    if (botapex == null) return;
                                    botapex.SetDestination(position);
                                });
                            }
                        }
                        timer.Once(4, () =>
                        {
                            if (botapex == null) return;
                            Effect.server.Run("assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion.prefab", botapex.transform.position);
                            HitInfo nullHit = new HitInfo();
                            nullHit.damageTypes.Add(Rust.DamageType.Explosion, 10000);
                            botapex.IsInvinsible = false;
                            botapex.Die(nullHit);
                        }
                        );
                    }
                });
            }
        }
        #endregion

        static BasePlayer FindPlayerByName(string name)
        {
            BasePlayer result = null;
            foreach (BasePlayer current in BasePlayer.activePlayerList)
            {
                if (current.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    BasePlayer result2 = current;
                    return result2;
                }
                if (current.UserIDString.Contains(name, CompareOptions.OrdinalIgnoreCase))
                {
                    BasePlayer result2 = current;
                    return result2;
                }
                if (current.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
                {
                    result = current;
                }
            }
            return result;
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            NPCPlayerApex npc = entity.GetComponent<NPCPlayerApex>();
            if (npc != null && npc is NPCMurderer) 
                NPCSensesLoadBalancer.NpcSensesLoadBalancer.Remove(npc);
        }

        #region Hooks
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var botMelee = info.Initiator as BaseMelee;
            bool melee = false;
            if (botMelee != null)
            {
                melee = true;
                info.Initiator = botMelee.GetOwnerPlayer();
            }
            NPCPlayerApex bot = null;

            if (entity is NPCPlayerApex)
            {
                bot = entity as NPCPlayerApex;

                if (!TempRecord.NPCPlayers.Contains(bot))
                    return null;
                var bData = bot.GetComponent<botData>();
                if (info.Initiator?.ToString() == null && configData.Global.Pve_Safe)
                    info.damageTypes.ScaleAll(0);
                if (info.Initiator is BasePlayer && !(info.Initiator is NPCPlayer))
                {
                    var canNetwork = Vanish?.Call("IsInvisible", info.Initiator);                                                                       //bots wont retaliate to vanished players
                    if ((canNetwork is bool))
                        if ((bool)canNetwork)
                            info.Initiator = null;

                    if (bData.peaceKeeper)                                                                                                               //prevent melee farming with peacekeeper on
                    {
                        var heldMelee = info.Weapon as BaseMelee;
                        var heldTorchWeapon = info.Weapon as TorchWeapon;
                        if (heldMelee != null || heldTorchWeapon != null)
                            info.damageTypes.ScaleAll(0);
                    }
                }
                float multiplier = 100f / bData.health;
                info.damageTypes.ScaleAll(multiplier);
            }

            if (info?.Initiator is NPCPlayer && entity is BasePlayer)                                                                                       //add in bot accuracy
            {
                var attacker = info.Initiator as NPCPlayerApex;

                if (TempRecord.NPCPlayers.Contains(attacker))
                {
                    var bData = attacker.GetComponent<botData>();
                    int rand = random.Next(1, 100);
                    float distance = (Vector3.Distance(info.Initiator.transform.position, entity.transform.position));

                    var newAccuracy = (bData.accuracy * 10f);
                    var newDamage = (bData.damage);
                    if (distance > 100f)
                    {
                        newAccuracy = ((bData.accuracy * 10f) / (distance / 100f));
                        newDamage = bData.damage / (distance / 100f);
                    }
                    if (!melee && newAccuracy < rand)                                                                                                          //scale bot attack damage
                    {
                        return true;
                    }
                    else
                    {
                        info.damageTypes.ScaleAll(newDamage);
                        return null;
                    }
                }
            }
            return null;
        }

        void OnPlayerDie(BasePlayer player)
        {
            string respawnLocationName = "";
            NPCPlayerApex Scientist = null;
            if (player is NPCPlayerApex)
            {
                Scientist = player as NPCPlayerApex;
                if (!TempRecord.NPCPlayers.Contains(Scientist))
                    return;

                if (TempRecord.NPCPlayers.Contains(Scientist))
                {
                    var bData = Scientist.GetComponent<botData>();
                    Item activeItem = player.GetActiveItem();
                    if (bData.dropweapon == true && activeItem != null)
                    {
                        using (TimeWarning timeWarning = TimeWarning.New("PlayerBelt.DropActive", 0.1f))
                        {
                            activeItem.Drop(player.eyes.position, new Vector3(), new Quaternion());
                            player.svActiveItemID = 0;
                            player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            kitRemoveList.Add(player.userID, activeItem.info.name);
                        }
                    }
                    no_of_AI--;
                    respawnLocationName = bData.monumentName;
                    DeadNPCPlayerIds.Add(Scientist.userID);
                    if (TempRecord.AllProfiles[respawnLocationName].Disable_Radio == true)
                        Scientist.DeathEffect = new GameObjectRef();                                                                                               //kill radio effects

                    if (bData.respawn == false)
                    {
                        UnityEngine.Object.Destroy(Scientist.GetComponent<botData>());
                        UpdateRecords(Scientist);
                        return;
                    }
                    if (bData.biome)
                    {
                        List<Vector3> spawnList = new List<Vector3>();

                        if (spawnLists.ContainsKey(bData.monumentName))
                        {
                            spawnList = spawnLists[bData.monumentName];
                            int spawnPos = random.Next(spawnList.Count);
                            SpawnBots(bData.monumentName, TempRecord.AllProfiles[bData.monumentName], "biome", null, spawnList[spawnPos]);
                            return;
                        }
                        else return;
                    }
                    foreach (var profile in TempRecord.AllProfiles)
                    {
                        timer.Once(profile.Value.Respawn_Timer, () => {
                            if (profile.Key == respawnLocationName)
                            {
                                SpawnBots(profile.Key, profile.Value, null, null, new Vector3());
                            }
                        });
                        UnityEngine.Object.Destroy(Scientist.GetComponent<botData>());
                        UpdateRecords(Scientist);
                    }
                }
            }
        }

        void OnEntitySpawned(BaseEntity entity) // handles smoke signals, backpacks, corpses(applying kit)
        {
            NPCPlayerApex npc = entity.GetComponent<NPCPlayerApex>();
            if (npc != null && npc is NPCMurderer)
                npc.NewAI = false;
            
            var KitDetails = new kitData();
            if (entity != null)
            {
                if (entity is NPCPlayerCorpse)
                {
                    var corpse = entity as NPCPlayerCorpse;
                    corpse.ResetRemovalTime(configData.Global.Corpse_Duration);

                    if (kitList.ContainsKey(corpse.playerSteamID))
                    {
                        KitDetails = kitList[corpse.playerSteamID];
                        NextTick(() =>
                        {
                            if (corpse == null)
                                return;
                            if (!KitDetails.Allow_Rust_Loot)
                            {
                                corpse.containers[0].Clear();
                                corpse.containers[1].Clear();
                                corpse.containers[2].Clear();
                            }
                            if (KitDetails.Kit != "")
                            {
                                string[] checkKit = (Kits.CallHook("GetKitContents", KitDetails.Kit)) as string[];

                                var tempbody = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", (corpse.transform.position - new Vector3(0, -100, 0)), corpse.transform.rotation).ToPlayer();
                                tempbody.Spawn();

                                Kits?.Call($"GiveKit", tempbody, KitDetails.Kit, true);

                                var source = new ItemContainer[] { tempbody.inventory.containerMain, tempbody.inventory.containerWear, tempbody.inventory.containerBelt };

                                for (int i = 0; i < (int)source.Length; i++)
                                {
                                    Item[] array = source[i].itemList.ToArray();
                                    for (int j = 0; j < (int)array.Length; j++)
                                    {
                                        Item item = array[j];
                                        if (!item.MoveToContainer(corpse.containers[i], -1, true))
                                        {
                                            item.Remove(0f);
                                        }
                                    }
                                }
                                tempbody.Kill();
                            }
                            if (kitList[corpse.playerSteamID].Wipe_Belt)
                                corpse.containers[2].Clear();
                            else
                            if (kitRemoveList.ContainsKey(corpse.playerSteamID))
                            {
                                foreach (var thing in corpse.containers[2].itemList)                                                                            //If weapon drop is enabled, this removes the weapon from the corpse's inventory.
                                {
                                    if (kitRemoveList[corpse.playerSteamID] == thing.info.name)
                                    {
                                        thing.Remove();
                                        kitRemoveList.Remove(corpse.playerSteamID);
                                        break;
                                    }
                                }
                            }

                            if (kitList[corpse.playerSteamID].Wipe_Clothing)
                            {
                                corpse.containers[1].Clear();
                            }

                            kitList.Remove(corpse.playerSteamID);
                        });
                    }
                }

                if (entity is DroppedItemContainer)
                {
                    NextTick(() =>
                    {
                        if (entity == null || entity.IsDestroyed) return;
                        var container = entity as DroppedItemContainer;

                        ulong ownerID = container.playerSteamID;
                        if (ownerID == 0) return;
                        if (configData.Global.Remove_BackPacks)
                        {
                            if (DeadNPCPlayerIds.Contains(ownerID))
                            {
                                entity.Kill();
                                DeadNPCPlayerIds.Remove(ownerID);
                                return;
                            }
                        }

                    });
                }

                if (entity.name.Contains("grenade.smoke.deployed"))
                    timer.Once(2.3f, () =>
                    {
                        if (entity != null)
                            smokeGrenades.Add(new Vector3(entity.transform.position.x, 0, entity.transform.position.z));
                    });

                if (!(entity.name.Contains("supply_drop")))
                    return;

                Vector3 dropLocation = new Vector3(entity.transform.position.x, 0, entity.transform.position.z);

                if (!(configData.Global.Supply_Enabled))
                {
                    foreach (var location in smokeGrenades)
                    {
                        if (Vector3.Distance(location, dropLocation) < 35f)
                        {
                            smokeGrenades.Remove(location);
                            return;
                        }
                    }
                }
                if (TempRecord.AllProfiles.ContainsKey("AirDrop"))
                {
                    var profile = TempRecord.AllProfiles["AirDrop"];
                    if (profile.AutoSpawn == true)
                    {
                        timer.Repeat(0.1f, profile.Bots, () =>
                        {
                            profile.LocationX = entity.transform.position.x;
                            profile.LocationY = entity.transform.position.y;
                            profile.LocationZ = entity.transform.position.z;
                            SpawnBots("AirDrop", profile, "AirDrop", null, new Vector3());
                        }
                        );
                    }
                }
            }
        }
        #endregion

        #region WeaponSwitching
        void SelectWeapon(NPCPlayerApex npcPlayer, BasePlayer victim, bool hasAttacker)
        {
            if (npcPlayer == null)
                return;

            if (npcPlayer.svActiveItemID == 0)
                return;

            var active = npcPlayer.GetActiveItem();
            HeldEntity heldEntity1 = null;

            if (active != null)
                heldEntity1 = active.GetHeldEntity() as HeldEntity;

            var bData = npcPlayer.GetComponent<botData>();

            if (hasAttacker == false)
            {
                List<int> weapons = new List<int>();                                                                                                    //check all their weapons
                foreach (Item item in npcPlayer.inventory.containerBelt.itemList)
                {
                    var held = item.GetHeldEntity();
                    if (held is BaseProjectile || held is BaseMelee || held is TorchWeapon)
                        weapons.Add(Convert.ToInt16(item.position));
                }

                if (weapons.Count == 0)
                {
                    PrintWarning(lang.GetMessage("noWeapon", this), bData.monumentName);
                    return;
                }
                int index = random.Next(weapons.Count);
                var currentTime = TOD_Sky.Instance.Cycle.Hour;

                if (currentTime > 20 || currentTime < 8)
                {
                    foreach (Item item in npcPlayer.inventory.containerBelt.itemList)
                    {
                        HeldEntity held = item.GetHeldEntity() as HeldEntity;

                        if (item.ToString().Contains("flashlight"))
                        {
                            if (heldEntity1 != null)
                                heldEntity1.SetHeld(false);
                            var UID = item.uid;

                            ChangeWeapon(npcPlayer, held, UID);
                            return;
                        }
                    }
                }
                else
                {
                    foreach (Item item in npcPlayer.inventory.containerBelt.itemList)                                                                   //pick one at random to start with
                    {
                        HeldEntity held = item.GetHeldEntity() as HeldEntity;

                        if (item.position == weapons[index])
                        {
                            if (heldEntity1 != null)
                                heldEntity1.SetHeld(false);
                            var UID = npcPlayer.inventory.containerBelt.GetSlot(weapons[index]).uid;

                            ChangeWeapon(npcPlayer, held, UID);
                            return;
                        }
                    }
                }
            }

            if (hasAttacker == true)
            {
                bData.canChangeWeapon++;

                if (bData.canChangeWeapon > 3)
                {
                    bData.canChangeWeapon = 0;
                    if (npcPlayer == null)
                        return;

                    if (heldEntity1 == null)
                        bData.currentWeaponRange = 0;

                    float distance = Vector3.Distance(npcPlayer.transform.position, victim.transform.position);
                    int noOfAvailableWeapons = 0;
                    int selectedWeapon;
                    Item chosenWeapon = null;
                    HeldEntity held = null;
                    int newCurrentRange = 0;
                    var currentTime = TOD_Sky.Instance.Cycle.Hour;
                    bool night = false;

                    if (currentTime > 20 || currentTime < 8)
                        night = true;

                    if (npcPlayer.AttackTarget == null && night)
                    {
                        foreach (var weap in bData.MeleeWeapons)
                        {
                            if (weap.ToString().Contains("flashlight"))
                            {
                                chosenWeapon = weap;
                                newCurrentRange = 1;
                            }
                        }
                    }
                    else
                    {
                        if (distance < 2f && bData.MeleeWeapons.Count != 0)
                        {
                            bData.enemyDistance = 1;
                            foreach (var weap in bData.MeleeWeapons)
                            {
                                noOfAvailableWeapons++;
                            }
                            if (noOfAvailableWeapons > 0)
                            {
                                selectedWeapon = random.Next(bData.MeleeWeapons.Count);
                                chosenWeapon = bData.MeleeWeapons[selectedWeapon];
                                newCurrentRange = 1;
                            }
                        }
                        else if (distance > 1f && distance < 20f && bData.CloseRangeWeapons != null)
                        {
                            bData.enemyDistance = 2;
                            foreach (var weap in bData.CloseRangeWeapons)
                            {
                                noOfAvailableWeapons++;
                            }
                            if (noOfAvailableWeapons > 0)
                            {
                                selectedWeapon = random.Next(bData.CloseRangeWeapons.Count);
                                chosenWeapon = bData.CloseRangeWeapons[selectedWeapon];
                                newCurrentRange = 2;
                            }
                            else
                            {
                                foreach (var weap in bData.MediumRangeWeapons)                                                                          //if no close weapon, prioritise medium
                                {
                                    noOfAvailableWeapons++;
                                }
                                if (noOfAvailableWeapons > 0)
                                {
                                    selectedWeapon = random.Next(bData.MediumRangeWeapons.Count);
                                    chosenWeapon = bData.MediumRangeWeapons[selectedWeapon];
                                    newCurrentRange = 3;
                                }
                            }
                        }
                        else if (distance > 19f && distance < 40f && bData.MediumRangeWeapons != null)
                        {
                            bData.enemyDistance = 3;
                            foreach (var weap in bData.MediumRangeWeapons)
                            {
                                noOfAvailableWeapons++;
                            }
                            if (noOfAvailableWeapons > 0)
                            {
                                selectedWeapon = random.Next(bData.MediumRangeWeapons.Count);
                                chosenWeapon = bData.MediumRangeWeapons[selectedWeapon];
                                newCurrentRange = 3;
                            }
                        }
                        else if (distance > 39 && bData.LongRangeWeapons != null)
                        {
                            bData.enemyDistance = 4;
                            foreach (var weap in bData.LongRangeWeapons)
                            {
                                noOfAvailableWeapons++;
                            }
                            if (noOfAvailableWeapons > 0)
                            {
                                selectedWeapon = random.Next(bData.LongRangeWeapons.Count);
                                chosenWeapon = bData.LongRangeWeapons[selectedWeapon];
                                newCurrentRange = 4;
                            }
                            else
                            {
                                foreach (var weap in bData.MediumRangeWeapons)                                                                          //if no long weapon, prioritise medium
                                {
                                    noOfAvailableWeapons++;
                                }
                                if (noOfAvailableWeapons > 0)
                                {
                                    selectedWeapon = random.Next(bData.MediumRangeWeapons.Count);
                                    chosenWeapon = bData.MediumRangeWeapons[selectedWeapon];
                                    newCurrentRange = 3;
                                }
                            }
                        }
                        if (chosenWeapon == null)                                                                                                       //if no weapon suited to range, pick any random bullet weapon
                        {                                                                                                                               //prevents sticking with melee @>2m when no pistol is available
                            bData.enemyDistance = 5;
                            if (heldEntity1 != null && bData.AllProjectiles.Contains(active))                                                           //prevents choosing a random weapon if the existing one is fine
                                return;
                            foreach (var weap in bData.AllProjectiles)
                            {
                                noOfAvailableWeapons++;
                            }
                            if (noOfAvailableWeapons > 0)
                            {
                                selectedWeapon = random.Next(bData.AllProjectiles.Count);
                                chosenWeapon = bData.AllProjectiles[selectedWeapon];
                                newCurrentRange = 5;
                            }
                        }
                    }
                    if (chosenWeapon == null) return;
                    if (newCurrentRange == bData.currentWeaponRange)
                    {
                        return;
                    }
                    else
                    {
                        bData.currentWeaponRange = newCurrentRange;
                        held = chosenWeapon.GetHeldEntity() as HeldEntity;

                        if (heldEntity1 != null && heldEntity1.name == held.name)
                            return;

                        if (heldEntity1 != null && heldEntity1.name != held.name)
                            heldEntity1.SetHeld(false);

                        var UID = chosenWeapon.uid;
                        ChangeWeapon(npcPlayer, held, UID);
                    }
                }
            }
            else
            {
                timer.Once(1, () => SelectWeapon(npcPlayer, victim, false));
            }
        }

        void ChangeWeapon(NPCPlayer npcPlayer, HeldEntity held, uint UID)
        {
            if (npcPlayer == null) return;
            npcPlayer.svActiveItemID = 0;
            npcPlayer.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            npcPlayer.inventory.UpdatedVisibleHolsteredItems();

            npcPlayer.svActiveItemID = UID;
            npcPlayer.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            held.SetHeld(true);
            npcPlayer.svActiveItemID = UID;
            npcPlayer.inventory.UpdatedVisibleHolsteredItems();
        }
        #endregion

        #region behaviour hooks
        object OnNpcPlayerResume(NPCPlayerApex player)
        {
            if (TempRecord.NPCPlayers.Contains(player))
            {
                var bData = player.GetComponent<botData>();
                if (bData.inAir) return false;
            }
            return null;
        }

        void OnNpcDestinationSet(NPCPlayerApex player)
        {
            if (TempRecord.NPCPlayers.Contains(player))
            {
                var bData = player.GetComponent<botData>();
                if (bData.goingHome)
                    return;
            }
        }
        #endregion

        #region targetingHooks
        object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity entity)
        {
            var path = TempRecord.NPCPlayers;
            var conf = configData.Global;

            if (npcPlayer == null || entity == null)
                return null;
             
            if (entity is NPCPlayer)
            {
                NPCPlayerApex botVictim = entity as NPCPlayerApex;

                if (path.Contains(botVictim) && !(path.Contains(npcPlayer)) && !(conf.NPCs_Attack_BotSpawn))    //stop oustideNPCs attacking BotSpawn bots
                    return true;

                if (!TempRecord.NPCPlayers.Contains(npcPlayer))
                    return null;

                if (!path.Contains(botVictim) && !(conf.BotSpawn_Attacks_NPCs))                                  //stop BotSpawn bots attacking outsideNPCs                                                                                  
                    return true;

                if (path.Contains(botVictim) && !(conf.BotSpawn_Attacks_BotSpawn))                              //stop BotSpawn murd+sci fighting each other 
                    return true;
            }

            if (!TempRecord.NPCPlayers.Contains(npcPlayer))
                return null;

            BasePlayer victim = entity as BasePlayer;
            if (victim != null)
            {
                var active = npcPlayer.GetActiveItem();
                var bData = npcPlayer.GetComponent<botData>();

                npcPlayer.AiContext.LastAttacker = entity;

                var currentTime = TOD_Sky.Instance.Cycle.Hour;

                HeldEntity heldEntity1 = null;
                if (active != null)
                    heldEntity1 = active.GetHeldEntity() as HeldEntity;

                if (heldEntity1 == null)                                                                                                                            //freshspawn catch, pre weapon draw.
                    return null;
                if (heldEntity1 != null)
                {
                    if (currentTime > 20 || currentTime < 8)
                        heldEntity1.SetLightsOn(true);
                    else
                        heldEntity1.SetLightsOn(false);
                }

                var heldWeapon = victim.GetHeldEntity() as BaseProjectile;
                var heldFlame = victim.GetHeldEntity() as FlameThrower;

                if (bData.peaceKeeper && !(victim is NPCPlayer))
                {
                    if (heldWeapon != null || heldFlame != null)
                    {
                        aggroPlayers.Add(victim.userID);
                        timer.Once(bData.peaceKeeper_CoolDown, () => {
                            if (aggroPlayers.Contains(victim.userID))
                                aggroPlayers.Remove(victim.userID);
                        });
                    }
                    if (!(aggroPlayers.Contains(victim.userID)))
                    {
                        if (victim.svActiveItemID == 0u)
                            return true;
                        if (heldWeapon == null && heldFlame == null)
                            return true;
                    }

                }
                SelectWeapon(npcPlayer, victim, true);

                if (Vector3.Distance(npcPlayer.transform.position, entity.transform.position) > 55
                    && Vector3.Distance(npcPlayer.transform.position, entity.transform.position) < bData.LongRangeAttack  
                    && npcPlayer.GetFact(NPCPlayerApex.Facts.HasLineOfSightStanding) == 1
                    && npcPlayer.GetHeldEntity() as BaseProjectile != null)
                {
                    npcPlayer.AttackTarget = victim;
                    npcPlayer.SetAimDirection((npcPlayer.AttackTarget.transform.position - npcPlayer.GetPosition()).normalized);
                    npcPlayer.ShotTest();
                }

                if (!victim.userID.IsSteamId() && !(victim is NPCPlayer) && conf.Ignore_HumanNPC)                                                                           //stops bots targeting humannpc
                    return true;
            }
            if (entity.name.Contains("agents/") && conf.Ignore_Animals)                                                                           //stops bots targeting animals
                return true;

            return null;
        }

        private object RaycastAll<T>(Ray ray, float distance) //credit S0N_0F_BISCUIT
        {
            var hits = Physics.RaycastAll(ray, Layers.Solid);
            GamePhysics.Sort(hits);
            object target = false;
            foreach (var hit in hits)
            {
                var ent = hit.GetEntity();
                if (ent is T)
                {
                    target = ent;
                    break;
                }
            }
            return target;
        }

        object CanBradleyApcTarget(BradleyAPC bradley, BaseEntity target)                                                                                       //stops bradley targeting bots
        {
            if (target is NPCPlayer && configData.Global.APC_Safe)
                return false;
            return null;
        }

        object OnNpcTarget(BaseNpc npc, BaseEntity entity)                                                                                                      //stops animals targeting bots
        {
            if (entity is NPCPlayer && configData.Global.Animal_Safe)
                return true;
            return null;
        }

        object CanBeTargeted(BaseCombatEntity player, MonoBehaviour turret)                                                                                     //stops autoturrets targetting bots
        {
            if (player is NPCPlayer && configData.Global.Turret_Safe)
                return false;
            return null;
        }
        #endregion

        #region SetUpLocations
        private void FindMonuments()                                                                                                                            //credit K1lly0u 
        {
            TempRecord.AllProfiles.Clear();
            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            int warehouse = 0;
            int lighthouse = 0;
            int gasstation = 0;
            int spermket = 0;
            int compound = 0; 

            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("autospawn/monument") && gobject.transform.position != new Vector3(0, 0, 0))
                {
                    var pos = gobject.transform.position;
                    var rot = gobject.transform.eulerAngles.y;

                    if (gobject.name.Contains("airfield_1"))
                    {
                        AddProfile("Airfield", configData.Monuments.Airfield, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("compound") && compound == 0)
                    {
                        AddProfile("Compound", configData.Monuments.Compound, pos, rot);
                        compound++;
                        continue;
                    }
                    if (gobject.name.Contains("compound") && compound == 1)
                    {
                        AddProfile("Compound1", configData.Monuments.Compound1, pos, rot);
                        compound++;
                        continue;
                    }
                    if (gobject.name.Contains("compound") && compound == 2)
                    {
                        AddProfile("Compound2", configData.Monuments.Compound2, pos, rot);
                        compound++;
                        continue;
                    }
                    if (gobject.name.Contains("sphere_tank"))
                    {
                        AddProfile("Dome", configData.Monuments.Dome, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("gas_station_1") && gasstation == 0)
                    {
                        AddProfile("GasStation", configData.Monuments.GasStation, pos, rot);
                        gasstation++;
                        continue;
                    }
                    if (gobject.name.Contains("gas_station_1") && gasstation == 1)
                    {
                        AddProfile("GasStation1", configData.Monuments.GasStation1, pos, rot);
                        gasstation++;
                        continue;
                    }
                    if (gobject.name.Contains("harbor_1"))
                    {
                        AddProfile("Harbor1", configData.Monuments.Harbor1, pos, rot);
                        continue;
                    }

                    if (gobject.name.Contains("harbor_2"))
                    {
                        AddProfile("Harbor2", configData.Monuments.Harbor2, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("junkyard"))
                    {
                        AddProfile("Junkyard", configData.Monuments.Junkyard, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("launch_site"))
                    {
                        AddProfile("Launchsite", configData.Monuments.Launchsite, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("lighthouse") && lighthouse == 0)
                    {
                        AddProfile("Lighthouse", configData.Monuments.Lighthouse, pos, rot);
                        lighthouse++;
                        continue;
                    }

                    if (gobject.name.Contains("lighthouse") && lighthouse == 1)
                    {
                        AddProfile("Lighthouse1", configData.Monuments.Lighthouse1, pos, rot);
                        lighthouse++;
                        continue;
                    }

                    if (gobject.name.Contains("lighthouse") && lighthouse == 2)
                    {
                        AddProfile("Lighthouse2", configData.Monuments.Lighthouse2, pos, rot);
                        lighthouse++;
                        continue;
                    }

                    if (gobject.name.Contains("military_tunnel_1"))
                    {
                        AddProfile("MilitaryTunnel", configData.Monuments.MilitaryTunnel, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("powerplant_1"))
                    {
                        AddProfile("PowerPlant", configData.Monuments.PowerPlant, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("mining_quarry_c"))
                    {
                        AddProfile("QuarryHQM", configData.Monuments.QuarryHQM, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("mining_quarry_b"))
                    {
                        AddProfile("QuarryStone", configData.Monuments.QuarryStone, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("mining_quarry_a"))
                    {
                        AddProfile("QuarrySulphur", configData.Monuments.QuarrySulphur, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("radtown_small_3"))
                    {
                        AddProfile("Radtown", configData.Monuments.Radtown, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("satellite_dish"))
                    {
                        AddProfile("Satellite", configData.Monuments.Satellite, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("supermarket_1") && spermket == 0)
                    {
                        AddProfile("SuperMarket", configData.Monuments.SuperMarket, pos, rot);
                        spermket++;
                        continue;
                    }

                    if (gobject.name.Contains("supermarket_1") && spermket == 1)
                    {
                        AddProfile("SuperMarket1", configData.Monuments.SuperMarket1, pos, rot);
                        spermket++;
                        continue;
                    }
                    if (gobject.name.Contains("trainyard_1"))
                    {
                        AddProfile("Trainyard", configData.Monuments.Trainyard, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("warehouse") && warehouse == 0)
                    {
                        AddProfile("Warehouse", configData.Monuments.Warehouse, pos, rot);
                        warehouse++;
                        continue;
                    }

                    if (gobject.name.Contains("warehouse") && warehouse == 1)
                    {
                        AddProfile("Warehouse1", configData.Monuments.Warehouse1, pos, rot);
                        warehouse++;
                        continue;
                    }

                    if (gobject.name.Contains("warehouse") && warehouse == 2)
                    {
                        AddProfile("Warehouse2", configData.Monuments.Warehouse2, pos, rot);
                        warehouse++;
                        continue;
                    }
                    if (gobject.name.Contains("water_treatment_plant_1"))
                    {
                        AddProfile("Watertreatment", configData.Monuments.Watertreatment, pos, rot);
                        continue;
                    }
                    if (gobject.name.Contains("compound") && compound > 2)
                        continue;
                    if (gobject.name.Contains("gas_station_1") && gasstation > 1)
                        continue;
                    if (gobject.name.Contains("lighthouse") && lighthouse > 2)
                        continue;
                    if (gobject.name.Contains("supermarket_1") && spermket > 1)
                        continue;
                    if (gobject.name.Contains("warehouse") && warehouse > 2)
                        continue;
                }
            }

            if (configData.Biomes.BiomeArid.AutoSpawn == true)
            {
                AddProfile("BiomeArid", configData.Biomes.BiomeArid, new Vector3(), 0f);
                GenerateSpawnPoints(spawnLists["AridSpawns"], "BiomeArid", configData.Biomes.BiomeArid.Bots, aridTimer, 1);
            }
            if (configData.Biomes.BiomeTemperate.AutoSpawn == true)
            {
                AddProfile("BiomeTemperate", configData.Biomes.BiomeTemperate, new Vector3(), 0f);
                GenerateSpawnPoints(spawnLists["TemperateSpawns"], "BiomeTemperate", configData.Biomes.BiomeTemperate.Bots, temperateTimer, 2);
            }
            if (configData.Biomes.BiomeTundra.AutoSpawn == true)
            {
                AddProfile("BiomeTundra", configData.Biomes.BiomeTundra, new Vector3(), 0f);
                GenerateSpawnPoints(spawnLists["TundraSpawns"], "BiomeTundra", configData.Biomes.BiomeTundra.Bots, tundraTimer, 4);
            }
            if (configData.Biomes.BiomeArctic.AutoSpawn == true)
            {
                AddProfile("BiomeArctic", configData.Biomes.BiomeArctic, new Vector3(), 0f);
                GenerateSpawnPoints(spawnLists["ArcticSpawns"], "BiomeArctic", configData.Biomes.BiomeArctic.Bots, arcticTimer, 8);
            }

            var drop = JsonConvert.SerializeObject(configData.Monuments.AirDrop);
            DataProfile Airdrop = JsonConvert.DeserializeObject<DataProfile>(drop);
            TempRecord.AllProfiles.Add("AirDrop", Airdrop);

            foreach (var profile in storedData.DataProfiles)
            {

                if (!(storedData.MigrationDataDoNotEdit.ContainsKey(profile.Key)))
                    storedData.MigrationDataDoNotEdit.Add(profile.Key, new ProfileRelocation());

                if (profile.Value.Parent_Monument != "")
                {
                    var path = storedData.MigrationDataDoNotEdit[profile.Key];

                    if (TempRecord.AllProfiles.ContainsKey(profile.Value.Parent_Monument) && !isBiome(profile.Value.Parent_Monument))
                    {
                        var configPath = TempRecord.AllProfiles[profile.Value.Parent_Monument];

                        path.ParentMonumentX = configPath.LocationX; //Incase user changed Parent after load
                        path.ParentMonumentY = configPath.LocationY;
                        path.ParentMonumentZ = configPath.LocationZ;

                        if (Mathf.Approximately(path.OldParentMonumentX, 0.0f)) //If it's a new entry, save current monument location info
                        {
                            Puts($"Saved migration data for {profile.Key}");

                            path.OldParentMonumentX = configPath.LocationX;
                            path.OldParentMonumentY = configPath.LocationY;
                            path.OldParentMonumentZ = configPath.LocationZ;
                            path.oldRotation = path.worldRotation;
                        }

                        if (!(Mathf.Approximately(path.ParentMonumentX, path.OldParentMonumentX))) //if old and new aren't equal
                        {
                            bool userChanged = false;
                            foreach (var monument in TempRecord.AllProfiles)
                                if (Mathf.Approximately(monument.Value.LocationX, path.OldParentMonumentX)) //but old matches some other monument, then the user must have switched Parent
                                {
                                    userChanged = true;
                                    break;
                                }

                            if (userChanged)
                            {
                                Puts($"Parent_Monument change detected - Saving {profile.Key} location relative to {profile.Value.Parent_Monument}");
                                path.OldParentMonumentX = path.ParentMonumentX;
                                path.OldParentMonumentY = path.ParentMonumentY;
                                path.OldParentMonumentZ = path.ParentMonumentZ;
                                path.oldRotation = path.worldRotation;
                            }
                            else
                            {
                                Puts($"Map seed change detected - Updating {profile.Key} location relative to new {profile.Value.Parent_Monument}");
                                Vector3 oldloc = new Vector3(profile.Value.LocationX, profile.Value.LocationY, profile.Value.LocationZ);
                                Vector3 oldMonument = new Vector3(path.OldParentMonumentX, path.OldParentMonumentY, path.OldParentMonumentZ);
                                Vector3 newMonument = new Vector3(path.ParentMonumentX, path.ParentMonumentY, path.ParentMonumentZ);
                                //Map Seed Changed  

                                var newTrans = new GameObject().transform;
                                newTrans.transform.position = oldloc;
                                newTrans.transform.RotateAround(oldMonument, Vector3.down, path.oldRotation);                   //spin old loc around old monument until mon-rotation is 0
                                Vector3 oldLocRotated = newTrans.transform.position;

                                Vector3 difference = oldLocRotated - oldMonument;                                               //get relationship betwee old location(rotated) minus monument
                                Vector3 newLocPreRot = newMonument + difference;                                                //add that difference to the new monument location

                                newTrans.transform.position = newLocPreRot;
                                newTrans.transform.RotateAround(newMonument, Vector3.down, -path.worldRotation);
                                Vector3 newLocation = newTrans.transform.position;                                              //rotate that number around the monument by new mon Rotation

                                profile.Value.LocationX = newLocation.x;
                                profile.Value.LocationY = newLocation.y;
                                profile.Value.LocationZ = newLocation.z;

                                path.oldRotation = path.worldRotation;
                                path.OldParentMonumentX = configPath.LocationX;
                                path.OldParentMonumentY = configPath.LocationY;
                                path.OldParentMonumentZ = configPath.LocationZ;
                                path.ParentMonumentX = configPath.LocationX;
                                path.ParentMonumentY = configPath.LocationY;  
                                path.ParentMonumentZ = configPath.LocationZ;
                            }
                        }
                    }
                    else
                    {
                        Puts($"Parent monument {profile.Value.Parent_Monument} does not exist for custom profile {profile.Key}");
                        profile.Value.AutoSpawn = false;
                        SaveData();
                    }


                }
                SaveData();
                TempRecord.AllProfiles.Add(profile.Key, profile.Value);
            }

            foreach (var profile in TempRecord.AllProfiles)
            {
                if (isBiome(profile.Key)) continue;
                if (profile.Value.Kit.Count > 0 && Kits == null)
                {
                    PrintWarning(lang.GetMessage("nokits", this), profile.Key);
                    continue;
                }

                if (profile.Value.AutoSpawn == true && profile.Value.Bots > 0 && !profile.Key.Contains("AirDrop"))
                {
                    timer.Repeat(2, profile.Value.Bots, () =>
                    {
                        if (TempRecord.AllProfiles.Contains(profile))
                            SpawnBots(profile.Key, profile.Value, null, null, new Vector3());
                    });
                }
            }
        }

        void AddProfile(string name, ConfigProfile monument, Vector3 pos, float rotation)                                                                                   //bring config data into live data
        {
            var toAdd = JsonConvert.SerializeObject(monument);
            DataProfile toAddDone = JsonConvert.DeserializeObject<DataProfile>(toAdd);

            TempRecord.AllProfiles.Add(name, toAddDone);
            TempRecord.AllProfiles[name].LocationX = pos.x;
            TempRecord.AllProfiles[name].LocationY = pos.y;
            TempRecord.AllProfiles[name].LocationZ = pos.z;
            foreach (var custom in storedData.DataProfiles)
            {
                if (custom.Value.Parent_Monument == name && storedData.MigrationDataDoNotEdit.ContainsKey(custom.Key))
                {
                    var path = storedData.MigrationDataDoNotEdit[custom.Key];
                    if (Mathf.Approximately(path.oldRotation, 0))
                    {
                        path.oldRotation = rotation;
                    }

                    path.worldRotation = rotation;
                    path.ParentMonumentX = pos.x;
                    path.ParentMonumentY = pos.y;
                    path.ParentMonumentZ = pos.z;
                }
            }
            SaveData();
        }

        private Vector3 RotateVector2D(Vector3 oldDirection, float angle)
        {
            float newX = Mathf.Cos(angle * Mathf.Deg2Rad) * (oldDirection.x) - Mathf.Sin(angle * Mathf.Deg2Rad) * (oldDirection.z);
            float newZ = Mathf.Sin(angle * Mathf.Deg2Rad) * (oldDirection.x) + Mathf.Cos(angle * Mathf.Deg2Rad) * (oldDirection.z);
            return new Vector3(newX, 0f, newZ);
        }

        #endregion

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BotSpawn", storedData);
        }
        #region Commands
        [ConsoleCommand("bot.respawn")]
        void cmdBotRespawn()
        {
            Unload();
            Init();
            OnServerInitialized();
        }

        [ConsoleCommand("bot.count")]
        void cmdBotCount()
        {
            int total = 0;
            foreach (var pair in TempRecord.NPCPlayers)
            {
                total++;
            }
            if (total == 1)
                PrintWarning(lang.GetMessage("numberOfBot", this), total);
            else
                PrintWarning(lang.GetMessage("numberOfBots", this), total);
        }

        [ChatCommand("botspawn")]
        void botspawn(BasePlayer player, string command, string[] args)
        {
            if (HasPermission(player.UserIDString, permAllowed) || isAuth(player))
                if (args != null && args.Length == 1)
                {
                    if (args[0] == "list")
                    {
                        var outMsg = lang.GetMessage("ListTitle", this);

                        foreach (var profile in storedData.DataProfiles)
                        {
                            outMsg += $"\n{profile.Key}";
                        }
                        PrintToChat(player, outMsg);
                    }
                    else
                        SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("error", this));
                }
                else if (args != null && args.Length == 2)
                {
                    if (args[0] == "add")
                    {
                        var name = args[1];
                        if (TempRecord.AllProfiles.ContainsKey(name))
                        {
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("alreadyexists", this), name);
                            return;
                        }
                        Vector3 pos = player.transform.position;

                        var customSettings = new DataProfile()
                        {
                            AutoSpawn = false,
                            BotNames = new List<string> { "" },
                            LocationX = pos.x,
                            LocationY = pos.y,
                            LocationZ = pos.z,
                        };

                        storedData.DataProfiles.Add(name, customSettings);
                        SaveData();
                        SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("customsaved", this), player.transform.position);
                    }

                    else if (args[0] == "move")
                    {
                        var name = args[1];
                        if (storedData.DataProfiles.ContainsKey(name))
                        {
                            storedData.DataProfiles[name].LocationX = player.transform.position.x;
                            storedData.DataProfiles[name].LocationY = player.transform.position.y;
                            storedData.DataProfiles[name].LocationZ = player.transform.position.z;
                            SaveData();
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("custommoved", this), name);
                        }
                        else
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("noprofile", this));
                    }

                    else if (args[0] == "remove")
                    {
                        var name = args[1];
                        if (storedData.DataProfiles.ContainsKey(name))
                        {
                            foreach (var bot in TempRecord.NPCPlayers)
                            {
                                if (bot == null)
                                    continue;

                                var bData = bot.GetComponent<botData>();
                                if (bData.monumentName == name)
                                    bot.Kill();
                            }
                            TempRecord.AllProfiles.Remove(name);
                            storedData.DataProfiles.Remove(name);
                            SaveData();
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("customremoved", this), name);
                        }
                        else
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("noprofile", this));
                    }
                    else
                        SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("error", this));
                }
                else if (args != null && args.Length == 3)
                {
                    if (args[0] == "toplayer")
                    {
                        var name = args[1];
                        var profile = args[2].ToLower();
                        BasePlayer target = FindPlayerByName(name);
                        Vector3 location = (CalculateGroundPos(player.transform.position, false));
                        var found = false;
                        if (target == null)
                        {
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("namenotfound", this), name);
                            return;
                        }
                        foreach (var entry in TempRecord.AllProfiles)
                        {
                            if (entry.Key.ToLower() == profile)
                            {
                                AttackPlayer(location, entry.Key, entry.Value, null);
                                SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("deployed", this), entry.Key, target.displayName);
                                found = true;
                                return;
                            }
                        }
                        if (!found)
                        {
                            SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("noprofile", this));
                            return;
                        }

                    }
                    else
                        SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("error", this));
                }
                else
                    SendReply(player, "<color=orange>" + lang.GetMessage("Title", this) + "</color>" + lang.GetMessage("error", this));
        }
        #endregion

        public static List<ulong> DeadNPCPlayerIds = new List<ulong>(); //to tracebackpacks
        public static Dictionary<ulong, kitData> kitList = new Dictionary<ulong, kitData>();
        public static Dictionary<ulong, string> kitRemoveList = new Dictionary<ulong, string>();
        public static List<Vector3> smokeGrenades = new List<Vector3>();
        public static List<ulong> aggroPlayers = new List<ulong>();

        #region BotMono
        public class kitData
        {
            public string Kit;
            public bool Wipe_Belt;
            public bool Wipe_Clothing;
            public bool Allow_Rust_Loot;
        }

        public class botData : MonoBehaviour
        {
            public Vector3 spawnPoint;
            public int canChangeWeapon;
            public float enemyDistance;
            public int currentWeaponRange;
            public List<Item> AllProjectiles = new List<Item>();
            public List<Item> MeleeWeapons = new List<Item>();
            public List<Item> CloseRangeWeapons = new List<Item>();
            public List<Item> MediumRangeWeapons = new List<Item>();
            public List<Item> LongRangeWeapons = new List<Item>();
            public int accuracy;
            public float damage;
            public float range;
            public int health;
            public string monumentName;
            public bool dropweapon;
            public bool respawn;
            public bool biome = false;
            public int roamRange;
            public bool goingHome;
            public bool keepAttire;
            public bool peaceKeeper;
            public string group; //external hook identifier 
            public bool chute;
            public bool inAir = false;
            public int LongRangeAttack = 120;
            public int peaceKeeper_CoolDown = 5;

            int landingAttempts = 0;
            Vector3 landingDirection = new Vector3(0, 0, 0);
            int updateCounter = 0;

            NPCPlayerApex botapex;
            void Start()
            {
                botapex = this.GetComponent<NPCPlayerApex>();
                if (chute) inAir = true;
                botapex.GetNavAgent.enabled = true;
                botapex.StoppingDistance = 1f;
                botapex.utilityAiComponent.enabled = true;
                //foreach (BasePlayer player in BasePlayer.activePlayerList)
                //player.SendConsoleCommand("ddraw.line", 10f, Color.blue, botapex.transform.position, new Vector3(botapex.transform.position.x, 300, botapex.transform.position.z)); //Biome Check Debug
            }

            private void OnCollisionEnter(Collision collision)
            {
                var rb = botapex.gameObject.GetComponent<Rigidbody>();
                if (landingAttempts == 0)
                    landingDirection = botapex.transform.forward;
                if (inAir)
                {
                    if (landingAttempts > 5)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        botapex.gameObject.layer = 17;

                        foreach (var child in botapex.children)
                        {
                            if (child.name.Contains("parachute"))
                            {
                                child.SetParent(null);
                                break;
                            }
                        }
                        inAir = false;
                        botapex.Resume();
                        return;
                    }
                    if (collision.collider.name.Contains("Terrain"))
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        botapex.gameObject.layer = 17;

                        foreach (var child in botapex.children)
                        {
                            if (child.name.Contains("parachute"))
                            {
                                child.SetParent(null);
                                break;
                            }
                        }
                        inAir = false;
                        botapex.Resume();

                        botapex.Stats.IsMobile = true;
                        botapex.SetFact(NPCPlayerApex.Facts.IsRoamReady, 1, true, true);
                        botapex.SetFact(NPCPlayerApex.Facts.IsMoving, 1, true, true);
                    }
                    else if (Vector3.Distance(botapex.transform.position, CalculateGroundPos(botapex.transform.position, false)) < 2f)
                    {
                        landingAttempts++;
                        rb.useGravity = true;
                        rb.velocity = new Vector3(landingDirection.x * 15, 11, landingDirection.z * 15);
                        rb.drag = 1f;
                    }
                }
            }

            void Update() 
            {
                updateCounter++;
                if (updateCounter == 1000)
                {
                    updateCounter = 0;
                    Vector3 newAim;
                    if (inAir)
                    {
                        if (botapex.AttackTarget is BasePlayer && !(botapex.AttackTarget is NPCPlayer))
                            botapex.SetAimDirection((botapex.AttackTarget.transform.position - botapex.GetPosition()).normalized);

                        goingHome = false;
                    }
                    else
                    {
                        if (!goingHome && botapex.AttackTarget == null && Vector3.Distance(botapex.transform.position, spawnPoint) > roamRange && botapex.GetNavAgent.isOnNavMesh)
                            goingHome = true;

                        if (goingHome && Vector3.Distance(botapex.transform.position, spawnPoint) > 5 && botapex.GetNavAgent.isOnNavMesh)
                        {
                            botapex.GetNavAgent.SetDestination(spawnPoint);
                        }
                        else
                            goingHome = false;
                    }
                }
            }
        }
        #endregion

        #region Config
        private ConfigData configData;

        class TempRecord
        {
            public static List<NPCPlayerApex> NPCPlayers = new List<NPCPlayerApex>();
            public static Dictionary<string, DataProfile> AllProfiles = new Dictionary<string, DataProfile>();
        }

        class Global
        {
            public bool NPCs_Attack_BotSpawn = true;
            public bool BotSpawn_Attacks_NPCs = true;
            public bool BotSpawn_Attacks_BotSpawn = false;
            public bool Ignore_Animals = true;
            public bool APC_Safe = true;
            public bool Turret_Safe = true;
            public bool Animal_Safe = true;
            public bool Supply_Enabled = false;
            public bool Remove_BackPacks = true;
            public bool Ignore_HumanNPC = true;
            public bool Pve_Safe = true;
            public int Corpse_Duration = 60;
        }
        public class Monuments
        {
            public AirDropProfile AirDrop = new AirDropProfile { };
            public ConfigProfile Airfield = new ConfigProfile { };
            public ConfigProfile Dome = new ConfigProfile { };
            public ConfigProfile Compound = new ConfigProfile { };
            public ConfigProfile Compound1 = new ConfigProfile { };
            public ConfigProfile Compound2 = new ConfigProfile { };
            public ConfigProfile GasStation = new ConfigProfile { };
            public ConfigProfile GasStation1 = new ConfigProfile { };
            public ConfigProfile Harbor1 = new ConfigProfile { };
            public ConfigProfile Harbor2 = new ConfigProfile { };
            public ConfigProfile Junkyard = new ConfigProfile { };
            public ConfigProfile Launchsite = new ConfigProfile { };
            public ConfigProfile Lighthouse = new ConfigProfile { };
            public ConfigProfile Lighthouse1 = new ConfigProfile { };
            public ConfigProfile Lighthouse2 = new ConfigProfile { };
            public ConfigProfile MilitaryTunnel = new ConfigProfile { };
            public ConfigProfile PowerPlant = new ConfigProfile { };
            public ConfigProfile QuarrySulphur = new ConfigProfile { };
            public ConfigProfile QuarryStone = new ConfigProfile { };
            public ConfigProfile QuarryHQM = new ConfigProfile { };
            public ConfigProfile SuperMarket = new ConfigProfile { };
            public ConfigProfile SuperMarket1 = new ConfigProfile { };
            public ConfigProfile Radtown = new ConfigProfile { };
            public ConfigProfile Satellite = new ConfigProfile { };
            public ConfigProfile Trainyard = new ConfigProfile { };
            public ConfigProfile Warehouse = new ConfigProfile { };
            public ConfigProfile Warehouse1 = new ConfigProfile { };
            public ConfigProfile Warehouse2 = new ConfigProfile { };
            public ConfigProfile Watertreatment = new ConfigProfile { };
        }

        public class Biomes
        {
            public ConfigProfile BiomeArid = new ConfigProfile { };
            public ConfigProfile BiomeTemperate = new ConfigProfile { };
            public ConfigProfile BiomeTundra = new ConfigProfile { };
            public ConfigProfile BiomeArctic = new ConfigProfile { };
        }
        public class AirDropProfile
        {
            public bool AutoSpawn = false;
            public bool Murderer = false;
            public int Bots = 5;
            public int BotHealth = 100;
            public int Radius = 100;
            public List<string> Kit = new List<string>();
            public string BotNamePrefix = "";
            public List<string> BotNames = new List<string>();
            public int Bot_Accuracy = 4;
            public float Bot_Damage = 0.4f;
            public bool Disable_Radio = true;
            public int Roam_Range = 40;
            public bool Peace_Keeper = true;
            public int Peace_Keeper_Cool_Down = 5;
            public bool Weapon_Drop = true;
            public bool Keep_Default_Loadout = false;
            public bool Wipe_Belt = true;
            public bool Wipe_Clothing = true;
            public bool Allow_Rust_Loot = true;
            public int Suicide_Timer = 300;
            public bool Chute = false;
            public int Long_Attack_Distance = 120;
        }

        public class ConfigProfile
        {
            public bool AutoSpawn = false;
            public bool Murderer = false;
            public int Bots = 5;
            public int BotHealth = 100;
            public int Radius = 100;
            public List<string> Kit = new List<string>();
            public string BotNamePrefix = "";
            public List<string> BotNames = new List<string>();
            public int Bot_Accuracy = 4;
            public float Bot_Damage = 0.4f;
            public bool Disable_Radio = true;
            public int Roam_Range = 40;
            public bool Peace_Keeper = true;
            public int Peace_Keeper_Cool_Down = 5;
            public bool Weapon_Drop = true;
            public bool Keep_Default_Loadout = false;
            public bool Wipe_Belt = true;
            public bool Wipe_Clothing = true;
            public bool Allow_Rust_Loot = true;
            public int Suicide_Timer = 300;
            public bool Chute = false;
            public int Long_Attack_Distance = 120;
            public int Respawn_Timer = 60;
        }

        public class DataProfile
        {
            public bool AutoSpawn = false;
            public bool Murderer = false;
            public int Bots = 5;
            public int BotHealth = 100;
            public int Radius = 100;
            public List<string> Kit = new List<string>();
            public string BotNamePrefix = "";
            public List<string> BotNames = new List<string>();
            public int Bot_Accuracy = 4;
            public float Bot_Damage = 0.4f;
            public bool Disable_Radio = true;
            public int Roam_Range = 40;
            public bool Peace_Keeper = true;
            public int Peace_Keeper_Cool_Down = 5;
            public bool Weapon_Drop = true;
            public bool Keep_Default_Loadout = false;
            public bool Wipe_Belt = true;
            public bool Wipe_Clothing = true;
            public bool Allow_Rust_Loot = true;
            public int Suicide_Timer = 300;
            public bool Chute = false;
            public int Long_Attack_Distance = 120;
            public int Respawn_Timer = 60;
            public float LocationX;
            public float LocationY;
            public float LocationZ;
            public string Parent_Monument = "";
        }

        public class ProfileRelocation
        {
            public float OldParentMonumentX = 0;
            public float OldParentMonumentY = 0;
            public float OldParentMonumentZ = 0;
            public float ParentMonumentX = 0;
            public float ParentMonumentY = 0;
            public float ParentMonumentZ = 0;
            public float oldRotation = 0.0f;
            public float worldRotation = 0.0f;
        }

        class ConfigData
        {
            public Global Global = new Global();
            public Monuments Monuments = new Monuments();
            public Biomes Biomes = new Biomes();
        }
                        class OldConfigData //Remove in 1.6.7
                        {
                            public Options Options = new Options();
                            public Monuments Zones = new Monuments();
                        }
                                class Options
                        {
                            public bool NPCs_Attack_BotSpawn = true;
                            public bool BotSpawn_Attacks_NPCs = true;
                            public bool BotSpawn_Attacks_BotSpawn = false;
                            public bool Ignore_Animals = true;
                            public bool APC_Safe = true;
                            public bool Turret_Safe = true;
                            public bool Animal_Safe = true;
                            public bool Supply_Enabled = false;
                            public bool Remove_BackPacks = true;
                            public bool Ignore_HumanNPC = true;
                            public bool Pve_Safe = true;
                            public int Corpse_Duration = 60;
                        }
        
        private void LoadConfigVariables()
        {
            ///////////////////////////////////////////////////////////////////////////MIGRATION INFO TO REMOVE in 1.6.7
            var oldconfigData = Config.ReadObject<OldConfigData>();
            var oldDeser = JsonConvert.SerializeObject(oldconfigData);
            if (oldDeser.Length != 19414 && oldDeser.Length != 18673)
            {  
                oldDeser = oldDeser.Replace(("\"Zones\":"), ("\"Monuments\":")); 
                oldDeser = oldDeser.Replace(("\"Options\":"), ("\"Global\":"));
                int lastPos = oldDeser.LastIndexOf('}');
                oldDeser = oldDeser.Insert(lastPos, ",\"Biomes\":{}");  
                var migratedData = JsonConvert.DeserializeObject<ConfigData>(oldDeser);
                SaveConfig(migratedData);
            }
            ///////////////////////////////////////////////////////////////////////////  

            configData = Config.ReadObject<ConfigData>();
            SaveConfig(configData);
        }
        protected override void LoadDefaultConfig()
        {  
            Puts("Creating new config file.");

            var config = new ConfigData();
            SaveConfig(config);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"Title", "BotSpawn : " },
            {"error", "/botspawn commands are - list - add - remove - move - toplayer" },
            {"customsaved", "Custom Location Saved @ {0}" },
            {"custommoved", "Custom Location {0} has been moved to your current position." },
            {"alreadyexists", "Custom Location already exists with the name {0}." },
            {"customremoved", "Custom Location {0} Removed." },
            {"deployed", "'{0}' bots deployed to {1}." },
            {"ListTitle", "Custom Locations" },
            {"noprofile", "There is no profile by that name in config or data BotSpawn.json files." },
            {"namenotfound", "Player '{0}' was not found" },
            {"nokits", "Kits is not installed but you have declared custom kits at {0}." },
            {"noWeapon", "A bot at {0} has no weapon. Check your kits." },
            {"numberOfBot", "There is {0} spawned bot alive." },
            {"numberOfBots", "There are {0} spawned bots alive." },

        };
        #endregion

        #region ExternalHooks
        [HookMethod("AddGroupSpawn")]
        public string[] AddGroupSpawn(Vector3 location, string profileName, string group)
        {
            if (location == new Vector3() || profileName == null || group == null)
                return new string[] { "error", "Null parameter" };
            string lowerProfile = profileName.ToLower();

            foreach (var entry in TempRecord.AllProfiles)
            {
                if (entry.Key.ToLower() == lowerProfile)
                {
                    var profile = entry.Value;
                    Vector3 targetLocation = (CalculateGroundPos(location, false));
                    AttackPlayer(targetLocation, entry.Key, profile, group.ToLower());
                    return new string[] { "true", "Group Successfully Added" };
                }
            }
            return new string[] { "false", "Group add failed - Check profile name and try again" };
        }

        [HookMethod("RemoveGroupSpawn")]
        public string[] RemoveGroupSpawn(string group)
        {
            if (group == null)
                return new string[] { "error", "No Group Specified." };

            List<NPCPlayerApex> toDestroy = new List<NPCPlayerApex>();
            foreach (var bot in TempRecord.NPCPlayers)
            {
                if (bot == null)
                    continue;
                var bData = bot.GetComponent<botData>();
                if (bData.group == group.ToLower())
                    toDestroy.Add(bot);
            }
            if (toDestroy.Count == 0)
                return new string[] { "true", $"There are no bots belonging to {group}" };
            foreach (var killBot in toDestroy)
            {
                UpdateRecords(killBot);
                killBot.Kill();
            }
            return new string[] { "true", $"Group {group} was destroyed." };

        }

        [HookMethod("CreateNewProfile")]
        public string[] CreateNewProfile(string name, string profile)
        {
            if (name == null)
                return new string[] { "error", "No Name Specified." };
            if (profile == null)
                return new string[] { "error", "No Profile Settings Specified." };

            DataProfile newProfile = JsonConvert.DeserializeObject<DataProfile>(profile);

            if (storedData.DataProfiles.ContainsKey(name))
            {
                storedData.DataProfiles[name] = newProfile;
                TempRecord.AllProfiles[name] = newProfile;
                return new string[] { "true", $"Profile {name} Was Updated" };
            }

            storedData.DataProfiles.Add(name, newProfile);
            SaveData();
            TempRecord.AllProfiles.Add(name, newProfile);
            return new string[] { "true", $"New Profile {name} Was Created." };
        }

        [HookMethod("ProfileExists")]
        public string[] ProfileExists(string name)
        {
            if (name == null)
                return new string[] { "error", "No Name Specified." };

            if (TempRecord.AllProfiles.ContainsKey(name))
                return new string[] { "true", $"{name} Exists." };

            return new string[] { "false", $"{name} Does Not Exist." };
        }

        [HookMethod("RemoveProfile")]
        public string[] RemoveProfile(string name)
        {
            if (name == null)
                return new string[] { "error", "No Name Specified." };

            if (storedData.DataProfiles.ContainsKey(name))
            {
                foreach (var bot in TempRecord.NPCPlayers)
                {
                    if (bot == null)
                        continue;

                    var bData = bot.GetComponent<botData>();
                    if (bData.monumentName == name)
                        bot.Kill();
                }
                TempRecord.AllProfiles.Remove(name);
                storedData.DataProfiles.Remove(name);
                SaveData();
                return new string[] { "true", $"Profile {name} Was Removed." };
            }
            else
            {
                return new string[] { "false", $"Profile {name} Does Not Exist." };
            }
        }
        #endregion
    }
}