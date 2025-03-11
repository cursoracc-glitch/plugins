using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using Network;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("NeonSkins", "Colon Blow", "1.0.11")]
    class NeonSkins : RustPlugin
    {

        // Added option to change sign refresh rate (tickrate)
        // added fixes to prevent entities not removing from saved sign data list when entity dies

        #region Load and Data Save

        BaseEntity newNeon;
        private bool initialized;

        void Loaded()
        {
            LoadVariables();
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("neonskins.add", this);
            permission.RegisterPermission("neonskins.skin", this);
            permission.RegisterPermission("neonskins.wings", this);
            permission.RegisterPermission("neonskins.attire", this);
            permission.RegisterPermission("neonskins.nolimit", this);
            permission.RegisterPermission("neonskins.viplimit", this);
            timer.In(5, RestoreNeonSkins);
        }

        private void OnServerInitialized()
        {
            LoadDataFile();
            initialized = true;
            timer.In(3, RestoreNeonSkins);
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        private void RestoreNeonSkins()
        {
            if (storedData.saveSkinData.Count > 0)
            {
                BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseEntity).ToArray();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        if (storedData.saveSkinData.ContainsKey(obj.net.ID))
                        {
                            var getent = obj.GetComponent<BaseEntity>();
                            if (getent) getent.Invoke("KillMessage", 0.1f);

                            var signownerid = storedData.saveSkinData[obj.net.ID].ownerid;
                            var signpos = StringToVector3(storedData.saveSkinData[obj.net.ID].pos);
                            var signrot = StringToQuaternion(storedData.saveSkinData[obj.net.ID].rot);
                            var signangle = StringToVector3(storedData.saveSkinData[obj.net.ID].eangles);
                            var signskin1 = storedData.saveSkinData[obj.net.ID].skin1;
                            var signskin2 = storedData.saveSkinData[obj.net.ID].skin2;
                            var signskin3 = storedData.saveSkinData[obj.net.ID].skin3;
                            var signtickrate = storedData.saveSkinData[obj.net.ID].tickrate;
                            SpawnSign(signownerid, signpos, signrot, signangle, signskin1, signskin2, signskin3, signtickrate);

                            RemovePlayerID(signownerid);
                            storedData.saveSkinData.Remove(obj.net.ID);
                            SaveData();

                            AddPlayerID(signownerid);
                        }
                    }
                }
            }
        }

        #endregion

        #region Configuration

        bool Changed;
        bool BlockSignDamage = true;

        bool UseMaxSignChecks = true;
        public int maxsigns = 2;
        public int maxvipsigns = 10;

        static ulong sign1skin1 = 1315739526;
        static ulong sign1skin2 = 1315738576;
        static ulong sign1skin3 = 1315736914;

        static ulong sign2skin1 = 1315783052;
        static ulong sign2skin2 = 1315783411;
        static ulong sign2skin3 = 1315783777;

        static ulong sign3skin1 = 1316693925;
        static ulong sign3skin2 = 1316694367;
        static ulong sign3skin3 = 1316694805;

        static ulong sign4skin1 = 1321558935;
        static ulong sign4skin2 = 1321560877;
        static ulong sign4skin3 = 1318568592;

        static ulong sign5skin1 = 1327047573;
        static ulong sign5skin2 = 1327050402;
        static ulong sign5skin3 = 1327051288;

        //neon skin
        static ulong sign6skin1 = 1328477513;
        static ulong sign6skin2 = 1328482753;
        static ulong sign6skin3 = 1328482753;

        //Coffee
        static ulong sign7skin1 = 1328494655;
        static ulong sign7skin2 = 1328496689;
        static ulong sign7skin3 = 1328496689;

        //IceCream
        static ulong sign8skin1 = 1328562541;
        static ulong sign8skin2 = 1328563646;
        static ulong sign8skin3 = 1328563646;

        //Live Music
        static ulong sign9skin1 = 1328565602;
        static ulong sign9skin2 = 1328566647;
        static ulong sign9skin3 = 1328566647;

        //24HR Open
        static ulong sign10skin1 = 1328523019;
        static ulong sign10skin2 = 1328525057;
        static ulong sign10skin3 = 1328526730;

        //Wifi
        static ulong sign11skin1 = 1328530331;
        static ulong sign11skin2 = 1328532600;
        static ulong sign11skin3 = 1328532600;

        //24HR
        static ulong sign12skin1 = 1328657360;
        static ulong sign12skin2 = 1328658142;
        static ulong sign12skin3 = 1328658903;

        //Bar
        static ulong sign13skin1 = 1328660615;
        static ulong sign13skin2 = 1328661436;
        static ulong sign13skin3 = 1328662136;

        //Coffee Bar
        static ulong sign14skin1 = 1328663741;
        static ulong sign14skin2 = 1328664598;
        static ulong sign14skin3 = 1328665457;

        //Food
        static ulong sign15skin1 = 1328666723;
        static ulong sign15skin2 = 1328667715;
        static ulong sign15skin3 = 1328668631;

        //Motel
        static ulong sign16skin1 = 1328669415;
        static ulong sign16skin2 = 1328670130;
        static ulong sign16skin3 = 1328670791;

        //Open Arrow
        static ulong sign17skin1 = 1328671976;
        static ulong sign17skin2 = 1328671976;
        static ulong sign17skin3 = 1328672694;

        static ulong leftwingangel = 1322833102;
        static ulong leftwingfairy = 1322813799;
        static ulong leftwingblack = 1314069609;

        static ulong rightwingangel = 1322833810;
        static ulong rightwingfairy = 1322814164;
        static ulong rightwingblack = 1314070395;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        private void LoadConfigVariables()
        {

            CheckCfg("Max Neon Signs : Normal Authenicated User : ", ref maxsigns);
            CheckCfg("Max Neon Signs : VIP Authenticated User : ", ref maxvipsigns);

            CheckCfg("Block Damage to Signs : ", ref BlockSignDamage);

            CheckCfg("Sign 1 - SkinID for frame 1 : ", ref sign1skin1);
            CheckCfg("Sign 1 - SkinID for frame 2 : ", ref sign1skin2);
            CheckCfg("Sign 1 - SkinID for frame 3 : ", ref sign1skin3);

            CheckCfg("Sign 2 - SkinID for frame 1 : ", ref sign2skin1);
            CheckCfg("Sign 2 - SkinID for frame 2 : ", ref sign2skin2);
            CheckCfg("Sign 2 - SkinID for frame 3 : ", ref sign2skin3);

            CheckCfg("Sign 3 - SkinID for frame 1 : ", ref sign3skin1);
            CheckCfg("Sign 3 - SkinID for frame 2 : ", ref sign3skin2);
            CheckCfg("Sign 3 - SkinID for frame 3 : ", ref sign3skin3);

            CheckCfg("Sign 4 - SkinID for frame 1 : ", ref sign4skin1);
            CheckCfg("Sign 4 - SkinID for frame 2 : ", ref sign4skin2);
            CheckCfg("Sign 4 - SkinID for frame 3 : ", ref sign4skin3);

            CheckCfg("Sign 5 - SkinID for frame 1 : ", ref sign5skin1);
            CheckCfg("Sign 5 - SkinID for frame 2 : ", ref sign5skin2);
            CheckCfg("Sign 5 - SkinID for frame 3 : ", ref sign5skin3);

            CheckCfg("Wings - Angel Wings Skin Left : ", ref leftwingangel);
            CheckCfg("Wings - Angel Wings Skin Right : ", ref rightwingangel);

            CheckCfg("Wings - Fairy Wings Skin Left : ", ref leftwingfairy);
            CheckCfg("Wings - Fairy Wings Skin Right : ", ref rightwingfairy);

            CheckCfg("Wings - Black Wings Skin Left : ", ref leftwingblack);
            CheckCfg("Wings - Black Wings Skin Right : ", ref rightwingblack);
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        #endregion

        #region Localization

        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["notasign"] = "You need to be looking at a sign to use that !! ",
            ["notflying"] = "You need to be flying to use Wings !! ",
            ["tryagain"] = "Something went wrong, please try again !! ",
            ["togglesign"] = "You have toggled the sign !! ",
            ["maxsigns"] = "You have reached your Maximum Neon Sign build limit !!",
            ["notowner"] = "You need to be owner of sign to use that !! ",
            ["notallowed"] = "You are not authorized to use that command !!"
        };

        #endregion

        #region Data 

        static Dictionary<ulong, PlayerSignData> loadplayer = new Dictionary<ulong, PlayerSignData>();

        public class PlayerSignData
        {
            public BasePlayer player;
            public int signcount;
        }

        static StoredData storedData = new StoredData();
        DynamicConfigFile dataFile;

        public class StoredData
        {
            public Dictionary<uint, StoredSkinsData> saveSkinData = new Dictionary<uint, StoredSkinsData>();
            public StoredData() { }
        }

        public class StoredSkinsData
        {
            public ulong ownerid;
            public string pos;
            public string eangles;
            public string rot;
            public ulong skin1;
            public ulong skin2;
            public ulong skin3;
            public int tickrate;
        }

        void LoadDataFile()
        {
            dataFile = Interface.Oxide.DataFileSystem.GetFile(Title);

            try
            {
                storedData = dataFile.ReadObject<StoredData>();
            }
            catch { }

            if (storedData == null)
                storedData = new StoredData();
        }

        void AddData(uint entnetid, ulong sownerid, string spos, string seangles, string srot, ulong skinid1, ulong skinid2, ulong skinid3, int signtickrate)
        {
            if (storedData.saveSkinData.ContainsKey(entnetid)) storedData.saveSkinData.Remove(entnetid);

            storedData.saveSkinData.Add(entnetid, new StoredSkinsData
            {
                ownerid = sownerid,
                pos = spos,
                eangles = seangles,
                rot = srot,
                skin1 = skinid1,
                skin2 = skinid2,
                skin3 = skinid3,
                tickrate = signtickrate,
            });
            SaveData();
        }

        void RemoveData(uint entnetid)
        {
            if (storedData.saveSkinData.ContainsKey(entnetid)) storedData.saveSkinData.Remove(entnetid);
        }

        void SaveData()
        {
            if (dataFile != null && storedData != null)
            {
                dataFile.WriteObject(storedData);
            }
        }

        public static Vector3 StringToVector3(string sVector)
        {
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }
            string[] sArray = sVector.Split(',');
            Vector3 result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
             float.Parse(sArray[2]));
            return result;
        }

        public static Quaternion StringToQuaternion(string sVector)
        {
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }
            string[] sArray = sVector.Split(',');
            Quaternion result = new Quaternion(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]),
             float.Parse(sArray[3]));
            return result;
        }


        #endregion

        #region Commands

        [ChatCommand("neon")]
        void cmdChatNeonGUI(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.skin") || isAllowed(player, "neonskins.add"))
            {
                var hasgui = player.GetComponent<SignGUI>();
                if (hasgui) { hasgui.OnDestroy(); return; }
                player.gameObject.AddComponent<SignGUI>();
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.add")]
        void cmdChatNeonAdd(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.add"))
            {
                if (SignLimitReached(player)) { SendReply(player, msg("maxsigns", player.UserIDString)); return; }
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f)) newNeon = hit.GetTransform().gameObject.ToBaseEntity();
                if (newNeon == null) return;
                if (newNeon.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                if (newNeon is Signage)
                {
                    if (args != null && args.Length > 0)
                    {
                        var getstring = args[0].ToLower();
                        Vector3 wide = newNeon.transform.eulerAngles;
                        Vector3 tall = newNeon.transform.eulerAngles + new Vector3(90, 0, 0);
                        Vector3 tall2 = newNeon.transform.eulerAngles + new Vector3(270, 0, 0);
                        Vector3 pos = newNeon.transform.position;
                        Quaternion rot = newNeon.transform.rotation;

                        BaseEntity.saveList.Remove(newNeon);
                        newNeon.Kill(BaseNetworkable.DestroyMode.None);

                        if (getstring == "1") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign1skin1, sign1skin2, sign1skin3, 10));
                        if (getstring == "2") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign2skin1, sign2skin2, sign2skin3, 10));
                        if (getstring == "3") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign3skin1, sign3skin2, sign3skin3, 10));
                        if (getstring == "4") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign4skin1, sign4skin2, sign4skin3, 10));
                        if (getstring == "5") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign5skin1, sign5skin2, sign5skin3, 10));
                        if (getstring == "6") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign6skin1, sign6skin2, sign6skin3, 10));
                        if (getstring == "7") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign7skin1, sign7skin2, sign7skin3, 10));
                        if (getstring == "8") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, tall2, sign8skin1, sign8skin2, sign8skin3, 10));
                        if (getstring == "9") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, tall2, sign9skin1, sign9skin2, sign9skin3, 10));
                        if (getstring == "10") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign10skin1, sign10skin2, sign10skin3, 10));
                        if (getstring == "11") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign11skin1, sign11skin2, sign11skin3, 10));
                        if (getstring == "12") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign12skin1, sign12skin2, sign12skin3, 10));
                        if (getstring == "13") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign13skin1, sign13skin2, sign13skin3, 10));
                        if (getstring == "14") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, tall, sign14skin1, sign14skin2, sign14skin3, 10));
                        if (getstring == "15") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, tall, sign15skin1, sign15skin2, sign15skin3, 10));
                        if (getstring == "16") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, tall, sign16skin1, sign16skin2, sign16skin3, 10));
                        if (getstring == "17") timer.Once(1f, () => SpawnSign(player.userID, pos, rot, wide, sign17skin1, sign17skin2, sign17skin3, 10));
                        return;
                    }
                    else SendReply(player, msg("tryagain", player.UserIDString));
                }
                else SendReply(player, msg("tryagain", player.UserIDString));
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.skinwide")]
        void cmdChatNeonSkinWide(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.skin"))
            {
                if (SignLimitReached(player)) { SendReply(player, msg("maxsigns", player.UserIDString)); return; }
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f)) newNeon = hit.GetTransform().gameObject.ToBaseEntity();
                if (newNeon == null) return;
                if (newNeon.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                if (newNeon is Signage)
                {
                    PrepNeon(player, newNeon, args, true, false);
                }
                else SendReply(player, msg("tryagain", player.UserIDString));
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.skintall")]
        void cmdChatNeonSkinTall(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.skin"))
            {
                if (SignLimitReached(player)) { SendReply(player, msg("maxsigns", player.UserIDString)); return; }
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f)) newNeon = hit.GetTransform().gameObject.ToBaseEntity();
                if (newNeon == null) return;
                if (newNeon.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                if (newNeon is Signage)
                {
                    PrepNeon(player, newNeon, args, false, false);
                }
                else SendReply(player, msg("tryagain", player.UserIDString));
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.skinattire")]
        void cmdNeonSkinAttire(BasePlayer player, string command, string[] args)
        {
            if (!isAllowed(player, "neonskins.attire")) { SendReply(player, msg("notallowed", player.UserIDString)); return; }
            int iAmount = 1;

            string str1 = "";
            ulong skinid1 = 0;
            ulong skinid2 = 0;
            ulong skinid3 = 0;
            if (args != null && args.Length > 0)
            {
                str1 = args[0].ToLower();
            }
            if (args != null && args.Length > 1)
            {
                var getstring1 = args[1]?.ToLower();
                if (UInt64.TryParse(getstring1, out skinid1)) ;
            }
            if (args != null && args.Length > 2)
            {
                var getstring2 = args[2]?.ToLower();
                if (UInt64.TryParse(getstring2, out skinid2)) ;
            }
            if (args != null && args.Length > 3)
            {
                var getstring3 = args[3]?.ToLower();
                if (UInt64.TryParse(getstring3, out skinid3)) ;
            }

            Item num = ItemManager.CreateByPartialName(str1, 1);

            if (num == null)

            {
                SendReply(player, "Invalid Item!");
                return;
            }

            int itemID = num.info.itemid;

            var getitem = player.inventory.containerWear.FindItemByItemID(itemID);
            if (getitem != null)
            {
                getitem.skin = skinid1;
                getitem.MarkDirty();
                var addchanger = player.gameObject.AddComponent<AttireChanger>();
                addchanger.skin1 = skinid1;
                addchanger.skin2 = skinid2;
                addchanger.skin3 = skinid3;
                addchanger.GetItem(itemID);
            }
            return;

        }

        [ChatCommand("neon.skinrug")]
        void cmdChatNeonSkinRug(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.skin"))
            {
                if (SignLimitReached(player)) { SendReply(player, msg("maxsigns", player.UserIDString)); return; }
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f)) newNeon = hit.GetTransform().gameObject.ToBaseEntity();
                if (newNeon == null) return;
                if (newNeon.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                if (newNeon.name.Contains("rug/rug"))
                {
                    PrepNeon(player, newNeon, args, true, true);
                }
                else SendReply(player, msg("tryagain", player.UserIDString));
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.toggle")]
        void cmdChatPimpToggle(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.skin") || isAllowed(player, "neonskins.add"))
            {
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f)) newNeon = hit.GetTransform().gameObject.ToBaseEntity();
                if (newNeon == null) return;
                if (newNeon.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                if (IsPimpedOut(newNeon)) { ToggleSignState(newNeon); SendReply(player, msg("togglesign", player.UserIDString)); }
                else SendReply(player, msg("tryagain", player.UserIDString));
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.cleardatabase")]
        void cmdChatNeonClearDataBase(BasePlayer player, string command, string[] args)
        {
            if (player.net?.connection?.authLevel > 1)
            {
                storedData.saveSkinData.Clear();
                SaveData();
            }
            return;
        }

        [ChatCommand("neon.tickrate")]
        void cmdChatNeonTickRate(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player, "neonskins.skin") || isAllowed(player, "neonskins.add"))
            {
                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f)) newNeon = hit.GetTransform().gameObject.ToBaseEntity();
                if (newNeon == null) return;
                if (newNeon.OwnerID != player.userID) { SendReply(player, msg("notowner", player.UserIDString)); return; }
                if (IsPimpedOut(newNeon))
                {
                    int tickrate = 10;
                    if (args != null && args.Length > 0)
                    {
                        var getstring1 = args[0]?.ToLower();
                        if (int.TryParse(getstring1, out tickrate)) ;
                    }
                    if (tickrate < 5) tickrate = 5;
                    ToggleSignTickRate(newNeon, tickrate);
                    SendReply(player, msg("togglesign", player.UserIDString));
                }
                else SendReply(player, msg("tryagain", player.UserIDString));
            }
            else SendReply(player, msg("notallowed", player.UserIDString));
        }

        [ChatCommand("neon.wings")]
        void cmdChatToggleWings(BasePlayer player, string command, string[] args)
        {
            var argstring = "angel";
            if (isAllowed(player, "neonskins.wings"))
            {
                var haswings = player.GetComponent<WingEntity>();
                if (!haswings)
                {
                    if (!player.IsFlying) { SendReply(player, msg("notflying", player.UserIDString)); return; }
                    haswings = player.gameObject.AddComponent<WingEntity>();
                    if (args != null && args.Length > 0)
                    {
                        var getstring = args[0].ToLower();
                        if (getstring == "black") { haswings.isblack = true; haswings.isangel = false; haswings.isfairy = false; haswings.AddWings(); return; }
                        if (getstring == "fairy") { haswings.isblack = false; haswings.isangel = false; haswings.isfairy = true; haswings.AddWings(); return; }
                        if (getstring == "angel") { haswings.isblack = false; haswings.isangel = true; haswings.isfairy = false; haswings.AddWings(); return; }
                    }
                    haswings.isblack = false; haswings.isangel = true; haswings.isfairy = false; haswings.AddWings(); return;
                    return;
                }
                if (haswings) { GameObject.Destroy(haswings); return; }
            }
            SendReply(player, "You are not authorized to use that command");
            return;
        }

        [ChatCommand("neon.count")]
        void cmdChatNeonCount(BasePlayer player, string command, string[] args)
        {
            if (!loadplayer.ContainsKey(player.userID))
            {
                SendReply(player, "You have no Neon Signs !!");
                return;
            }
            SendReply(player, "Current Neon Signs : " + (loadplayer[player.userID].signcount));
        }

        #endregion

        #region Hooks

        bool IsPimpedOut(BaseEntity entity)
        {
            if (entity.GetComponent<SkinChanger>()) return true;
            return false;
        }

        void ToggleSignState(BaseEntity entity)
        {
            var isSign = entity.GetComponent<SkinChanger>();
            if (isSign && isSign.isanimated == true) { isSign.isanimated = false; return; }
            if (isSign && isSign.isanimated == false) { isSign.isanimated = true; return; }
            if (!isSign) entity.gameObject.AddComponent<SkinChanger>();
        }

        void ToggleSignTickRate(BaseEntity entity, int signtickrate)
        {
            var isSign = entity.GetComponent<SkinChanger>() ?? null;
            if (isSign != null)
            {
                entity.Invoke("KillMessage", 0.1f);

                var signownerid = storedData.saveSkinData[entity.net.ID].ownerid;
                var signpos = StringToVector3(storedData.saveSkinData[entity.net.ID].pos);
                var signrot = StringToQuaternion(storedData.saveSkinData[entity.net.ID].rot);
                var signangle = StringToVector3(storedData.saveSkinData[entity.net.ID].eangles);
                var signskin1 = storedData.saveSkinData[entity.net.ID].skin1;
                var signskin2 = storedData.saveSkinData[entity.net.ID].skin2;
                var signskin3 = storedData.saveSkinData[entity.net.ID].skin3;
                SpawnSign(signownerid, signpos, signrot, signangle, signskin1, signskin2, signskin3, signtickrate);

                RemovePlayerID(signownerid);
                storedData.saveSkinData.Remove(entity.net.ID);
                SaveData();

                AddPlayerID(signownerid);
            }
            return;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
	    if (entity == null) return;
            if (storedData.saveSkinData.ContainsKey(entity.net.ID))
            {
                storedData.saveSkinData.Remove(entity.net.ID);
                SaveData();
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (!BlockSignDamage) return;
            if (entity == null || hitInfo == null) return;
            var isneonsign = entity.GetComponentInParent<SkinChanger>() ?? null;
            if (isneonsign != null) hitInfo.damageTypes.ScaleAll(0);
            return;
        }

        private object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            if (entity == null || player == null) return null;
            if (entity.GetComponent<SkinChanger>() && entity.OwnerID == player.userID) return null;
            if (entity.GetComponent<SkinChanger>()) return false;
            return null;
        }

        bool SignLimitReached(BasePlayer player)
        {
            if (UseMaxSignChecks)
            {
                if (isAllowed(player, "neonskins.nolimit")) return false;
                if (loadplayer.ContainsKey(player.userID))
                {
                    var currentcount = loadplayer[player.userID].signcount;
                    var maxallowed = maxsigns;
                    if (isAllowed(player, "neonskins.viplimit")) maxallowed = maxvipsigns;
                    if (currentcount >= maxallowed) return true;
                }
            }
            return false;
        }

        void AddPlayerID(ulong ownerid)
        {
            if (!loadplayer.ContainsKey(ownerid))
            {
                loadplayer.Add(ownerid, new PlayerSignData
                {
                    signcount = 1
                });
                return;
            }
            loadplayer[ownerid].signcount = loadplayer[ownerid].signcount + 1;
        }

        void RemovePlayerID(ulong ownerid)
        {
            if (loadplayer.ContainsKey(ownerid)) loadplayer[ownerid].signcount = loadplayer[ownerid].signcount - 1;
            return;
        }

        void PrepNeon(BasePlayer player, BaseEntity newNeon, string[] args, bool iswide, bool isrug)
        {
            var modifier = new Vector3();
            if (iswide) modifier = new Vector3(0, 0, 0);
            if (!iswide) modifier = new Vector3(90, 0, 0);
            Vector3 pos = newNeon.transform.position;
            Vector3 ang = newNeon.transform.eulerAngles + modifier;
            Quaternion rot = newNeon.transform.rotation;

            ulong skinid1 = 0;
            ulong skinid2 = 0;
            ulong skinid3 = 0;

            if (args != null && args.Length > 0)
            {
                var getstring1 = args[0]?.ToLower();
                if (UInt64.TryParse(getstring1, out skinid1)) ;
            }
            if (args != null && args.Length > 1)
            {
                var getstring2 = args[1]?.ToLower();
                if (UInt64.TryParse(getstring2, out skinid2)) ;
            }
            if (args != null && args.Length > 2)
            {
                var getstring3 = args[2]?.ToLower();
                if (UInt64.TryParse(getstring3, out skinid3)) ;
            }
            if (isrug) { SkinRug(newNeon, skinid1, skinid2, skinid3); return; }
            BaseEntity.saveList.Remove(newNeon);
            newNeon.Kill(BaseNetworkable.DestroyMode.None);

            timer.Once(1f, () => SpawnSign(player.userID, pos, rot, ang, skinid1, skinid2, skinid3, 10));
        }

        void SpawnSign(ulong ownerid, Vector3 pos, Quaternion rot, Vector3 angle, ulong skinid1, ulong skinid2, ulong skinid3, int signtickrate)
        {
            string prefabnewNeon = "assets/prefabs/deployable/rug/rug.deployed.prefab";
            var sign = GameManager.server.CreateEntity(prefabnewNeon, pos, rot, true);

            sign.transform.eulerAngles = angle + new Vector3(0, 90, 90);
            sign.transform.position = sign.transform.position + new Vector3(0f, 0.5f, 0f);
            sign.skinID = skinid1;
            sign?.Spawn();
            sign.OwnerID = ownerid;

            var pimpedout = sign.gameObject.AddComponent<SkinChanger>();
            pimpedout.skin1 = skinid1;
            pimpedout.skin2 = skinid2;
            pimpedout.skin3 = skinid3;
            pimpedout.tickrate = signtickrate;

            AddData(sign.net.ID, ownerid, pos.ToString(), angle.ToString(), rot.ToString(), skinid1, skinid2, skinid3, signtickrate);
            AddPlayerID(ownerid);
        }

        void SkinRug(BaseEntity entity, ulong skinid1, ulong skinid2, ulong skinid3)
        {
            var pimpedout = entity.gameObject.AddComponent<SkinChanger>();
            pimpedout.skin1 = skinid1;
            pimpedout.skin2 = skinid2;
            pimpedout.skin3 = skinid3;
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var haswings = player.GetComponent<WingEntity>();
            if (haswings != null) { GameObject.Destroy(haswings); }
            var hasgui = player.GetComponent<SignGUI>();
            if (hasgui) hasgui.OnDestroy();
            return;
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            var haswings = player.GetComponent<WingEntity>();
            if (haswings != null) { GameObject.Destroy(haswings); }
            var hasgui = player.GetComponent<SignGUI>();
            if (hasgui) hasgui.OnDestroy();
            return;
        }

        void Unload()
        {
            SaveData();
            DestroyAll<WingEntity>();
            DestroyAll<SignGUI>();
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        #endregion

        #region Attire Changer

        class AttireChanger : MonoBehaviour
        {
            BasePlayer player;
            Item item;
            int counter;
            public bool isanimated;
            public ulong skin1;
            public ulong skin2;
            public ulong skin3;
            public int framecounter;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                counter = 0;
                isanimated = true;
                framecounter = 4;
            }

            public void GetItem(int itemID)
            {
                item = player.inventory.containerWear.FindItemByItemID(itemID);
            }

            bool SingleSkin()
            {
                if (skin1 == 0 || skin2 == 0 || skin3 == 0) return true;
                return false;
            }

            void FixedUpdate()
            {
                if (SingleSkin()) return;
                if (counter == 10) item.skin = skin2;
                if (counter == 20) item.skin = skin3;
                RefreshAll();
                counter = counter + 1;
                if (counter == 30) { item.skin = skin1; counter = 0; RefreshAll(); }
            }

            public void RefreshAll()
            {
                item.MarkDirty();
            }
        }

        #endregion

        #region Skin Changer

        class SkinChanger : MonoBehaviour
        {
            BaseEntity entity;
            Vector3 entitypos;
            Quaternion entityrot;
            int counter;
            public bool isanimated;
            int skinnum;
            public ulong skin1;
            public ulong skin2;
            public ulong skin3;
            public int tickrate;
            ulong ownerid;
            uint entityid;
            NeonSkins instance;

            void Awake()
            {
                instance = new NeonSkins();
                entity = GetComponentInParent<BaseEntity>();
                entityid = entity.net.ID;
                entitypos = entity.transform.position;
                entityrot = Quaternion.identity;
                ownerid = entity.OwnerID;
                counter = 0;
                isanimated = true;
                tickrate = 10;
            }

            void ClntDstry(BaseNetworkable entity, bool recursive = true)
            {
                if (Net.sv.write.Start())
                {
                    Net.sv.write.PacketID(Message.Type.EntityDestroy);
                    Net.sv.write.UInt32(entity.net.ID);
                    Net.sv.write.UInt8(0);
                    Net.sv.write.Send(new SendInfo(entity.net.group.subscribers));
                }
                if (recursive && entity.children != null) for (int i = 0; i < entity.children.Count; i++) ClntDstry(entity.children[i], false);
            }

            void EnttSnpsht(BaseNetworkable entity, bool recursive = true)
            {
                entity.InvalidateNetworkCache(); List<Connection> subscribers = entity.net.group == null ? Net.sv.connections : entity.net.group.subscribers; if (subscribers != null && subscribers.Count > 0) { for (int i = 0; i < subscribers.Count; i++) { Connection connection = subscribers[i]; BasePlayer basePlayer = connection.player as BasePlayer; if (!(basePlayer == null)) { if (Net.sv.write.Start()) { connection.validate.entityUpdates = connection.validate.entityUpdates + 1u; BaseNetworkable.SaveInfo saveInfo = new BaseNetworkable.SaveInfo { forConnection = connection, forDisk = false }; Net.sv.write.PacketID(Message.Type.Entities); Net.sv.write.UInt32(connection.validate.entityUpdates); entity.ToStreamForNetwork(Net.sv.write, saveInfo); Net.sv.write.Send(new SendInfo(connection)); } } } }
                if (recursive && entity.children != null) for (int i = 0; i < entity.children.Count; i++) EnttSnpsht(entity.children[i], false);
            }

            bool SingleSkin()
            {
                if (skin1 == 0 || skin2 == 0 || skin3 == 0) return true;
                return false;
            }

            void FixedUpdate()
            {
                if (!isanimated || SingleSkin()) return;
                if (counter == tickrate) entity.skinID = skin2;
                if (counter == (tickrate * 2)) entity.skinID = skin3;
                RefreshAll();
                counter = counter + 1;
                if (counter == (tickrate * 3)) { entity.skinID = skin1; counter = 0; RefreshAll(); }
            }

            public void RefreshAll()
            {
                entity.transform.hasChanged = true;
                ClntDstry(entity, true); EnttSnpsht(entity, true);
                entity.SendNetworkUpdateImmediate();
                entity.UpdateNetworkGroup();
            }

            public void OnDestroy()
            {
                if (loadplayer.ContainsKey(ownerid)) loadplayer[ownerid].signcount = loadplayer[ownerid].signcount - 1;
                if (entity != null && !entity.IsDestroyed) { entity.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

        #region Wings

        class WingEntity : BaseEntity
        {
            BaseEntity entity;
            Vector3 entitypos;
            Quaternion entityrot;
            BaseEntity wing1;
            BaseEntity wing2;
            bool isback;
            int counter;
            public bool isfairy;
            public bool isangel;
            public bool isblack;

            void Awake()
            {
                entity = GetComponentInParent<BaseEntity>();
                entitypos = entity.transform.position;
                entityrot = Quaternion.identity;
                isback = false;
                isfairy = true;
                isangel = false;
                isblack = false;
                counter = 0;
            }

            public void AddWings()
            {
                string prefabwing = "assets/prefabs/deployable/rug/rug.deployed.prefab";

                wing1 = GameManager.server.CreateEntity(prefabwing, entitypos, entityrot, false);
                wing1.transform.localEulerAngles = new Vector3(0, 270, 90);
                wing1.enableSaving = false;

                if (isangel) wing1.transform.localPosition = new Vector3(-1.6f, 1.3f, -0.02f);
                if (isangel) wing1.skinID = leftwingangel;

                if (isfairy) wing1.transform.localPosition = new Vector3(-1.5f, 1.5f, -0.02f);
                if (isfairy) wing1.skinID = leftwingfairy;

                if (isblack) wing1.transform.localPosition = new Vector3(-1.5f, 1.6f, -0.02f);
                if (isblack) wing1.skinID = leftwingblack;
                wing1?.Spawn();
                wing1.SetParent(entity);


                wing2 = GameManager.server.CreateEntity(prefabwing, entitypos, entityrot, false);
                wing2.transform.localEulerAngles = new Vector3(0, 270, 90);
                wing2.enableSaving = false;

                if (isangel) wing2.transform.localPosition = new Vector3(1.6f, 1.3f, -0.02f);
                if (isangel) wing2.skinID = rightwingangel;

                if (isfairy) wing2.transform.localPosition = new Vector3(1.5f, 1.5f, -0.02f);
                if (isfairy) wing2.skinID = rightwingfairy;

                if (isblack) wing2.transform.localPosition = new Vector3(1.5f, 1.6f, -0.02f);
                if (isblack) wing2.skinID = rightwingblack;
                wing2?.Spawn();
                wing2.SetParent(entity);
            }

            void FixedUpdate()
            {
                if (!isblack) return;
                if (counter == 5)
                {
                    if (isblack) Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v2.prefab", wing1.GetComponent<BaseEntity>(), 0, new Vector3(), new Vector3(), null, true);
                    if (isblack) Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v2.prefab", wing2.GetComponent<BaseEntity>(), 0, new Vector3(), new Vector3(), null, true);
                }
                if (counter == 130) { counter = 0; return; }
                counter = counter + 1;
            }

            void RefreshEntities()
            {
                if (wing1 != null) wing1.transform.hasChanged = true;
                if (wing1 != null) wing1.SendNetworkUpdateImmediate();
                if (wing1 != null) wing1.UpdateNetworkGroup();

                if (wing2 != null) wing2.transform.hasChanged = true;
                if (wing2 != null) wing2.SendNetworkUpdateImmediate();
                if (wing2 != null) wing2.UpdateNetworkGroup();
            }

            public void OnDestroy()
            {
                if (wing1 != null) { wing1.Invoke("KillMessage", 0.1f); }
                if (wing2 != null) { wing2.Invoke("KillMessage", 0.1f); }
                GameObject.Destroy(this);
            }
        }

        #endregion

        #region Sign GUI

        class SignGUI : MonoBehaviour
        {
            BasePlayer player;
            NeonSkins neonskins;
            string b1;

            void Awake()
            {
                neonskins = new NeonSkins();
                player = base.GetComponentInParent<BasePlayer>();
                AddSignGUI(player);
            }

            public void AddSignGUI(BasePlayer player)
            {
                DestroyCui(player);

                var elements = new CuiElementContainer();

                elements.Add(new CuiElement
                {
                    Name = "Neongui",
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 1", Url = "http://i.imgur.com/Bgc99p2.png", Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent { AnchorMin = "0.0 0.2",  AnchorMax = "0.28 0.9" }
                    }
                });

                b1 = elements.Add(new CuiButton
                {
                    Button = { Command = $"chat.say /neon", Color = "0.0 0.0 0.0 0.7" },
                    RectTransform = { AnchorMin = "0.24 0.2", AnchorMax = "0.28 0.23" },
                    Text = { Text = "Exit", FontSize = 12, Color = "1.0 1.0 1.0 1.0", Align = TextAnchor.MiddleCenter }
                }, "Overlay", "b1command");

                CuiHelper.AddUi(player, elements);
            }

            void DestroyCui(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "Neongui");
                CuiHelper.DestroyUi(player, b1);
            }

            public void OnDestroy()
            {
                DestroyCui(player);
                Destroy(this);
            }
        }

        #endregion

    }
}