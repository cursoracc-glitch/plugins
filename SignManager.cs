using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System;
using System.Drawing;
using System.IO;
 
namespace Oxide.Plugins
{
    [Info("SignManager", "Sempai#3239", "1.0.2", ResourceId = 0)]  
    [Description("User/Admin image management plugin.")]

    class SignManager : RustPlugin  
    {
        const string Font = "robotocondensed-regular.ttf";
        string sprite = "assets/content/textures/generic/fulltransparent.tga";
        
        List<ulong> Buffer = new List<ulong>(); 
        public List<BaseEntity> signs = new List<BaseEntity>();
        Dictionary<ulong, BaseEntity> Back = new Dictionary<ulong, BaseEntity>();
        bool exists(uint ID, uint texture) => storedData.Signs[ID].Where(x => x.InsCRC == texture).Any();

        bool loaded = false;
        void OnServerInitialized() 
        {
            loaded = true;
            cmd.AddChatCommand($"{configData.CommandAlias}", this, "SM");
            cmd.AddChatCommand($"{configData.AdminCommandAlias}", this, "SMA");

            foreach (var sign in BaseNetworkable.serverEntities.OfType<Signage>())
                AddSign(sign);

            foreach (var sign in BaseNetworkable.serverEntities.OfType<PhotoFrame>())
                AddSign(sign);

            foreach (var player in BasePlayer.allPlayerList)  
                OnPlayerConnected(player);

            if (storedData.Export.Count > 0)
                Import(); 

            SaveData();
        }

        private void OnServerSave() => timer.Once(UnityEngine.Random.Range(0f, 10f), SaveData);
         
        void OnPlayerConnected(BasePlayer player)
        {
            if (!storedData.UserID.ContainsKey(player.userID))
                storedData.UserID.Add(player.userID, new List<SignData>());
        }

        void OnSignUpdated(Signage sign, BasePlayer player) => DoSign(sign, player);
        void OnSignUpdated(PhotoFrame frame, BasePlayer player) => DoSign(frame, player);
        void OnEntitySpawned(Signage sign) => AddSign(sign);
        void OnEntitySpawned(PhotoFrame sign) => AddSign(sign);
        void OnEntityDeath(Signage sign) => RemoveSign(sign);
        void OnEntityDeath(PhotoFrame sign) => RemoveSign(sign);
        void OnEntityKill(Signage sign) => RemoveSign(sign);
        void OnEntityKill(PhotoFrame sign) => RemoveSign(sign);

        void DoSign(BaseEntity e, BasePlayer player)
        {
            var id = player.userID;
            Buffer.Add(id);
            Buffer = Buffer.Distinct().ToList();

            timer.Once(0.1f, () =>
            {
                if (player == null || e == null)
                {
                    Buffer.Remove(id);
                    return;
                }
                if (Buffer.Contains(player.userID))
                {
                    bool forplayer = permission.UserHasPermission(player.UserIDString, permAuto) && storedData.UserID[player.userID].Count() < 25;
                    SavePlayerSign(player, e, true, -1, forplayer);
                    Buffer.Remove(id);
                }
            });
        }

        void AddSign(BaseEntity e)  
        {
            if (!loaded)
                return;

            NextTick(() => 
            {
                if (e == null) 
                    return;

                signs.Add(e);
          
                Signage sign = e as Signage; 
                PhotoFrame frame = e as PhotoFrame;
                 
                if (!storedData.Signs.ContainsKey(e.net.ID))
                    storedData.Signs.Add(e.net.ID, new List<SignData>());

                if (sign != null)
                {
                    for (int i = 0; i < sign.textureIDs.Count(); i++)
                    {
                        if (sign.textureIDs[i] != 0)
                        {
                            if (exists(e.net.ID, sign.textureIDs[i]))
                                continue; 

                            var Crcs = ReplaceSign(sign.textureIDs[i], e.net.ID, CommunityEntity.ServerInstance.net.ID, Convert.ToUInt32(i));
                            if (Crcs == null)
                                continue;
                            sign.textureIDs[i] = Crcs[0];
                            storedData.Signs[e.net.ID].Add(new SignData()
                            {
                                InsCRC = Crcs[0],
                                PermaCRC = Crcs[1],
                                layer = Convert.ToUInt32(i),
                                time = DateTime.Now,
                                creator = 0
                            });
                        }
                    }
                }

                if (frame != null && frame._overlayTextureCrc != 0)
                {
                    if (exists(e.net.ID, frame._overlayTextureCrc))
                        return;

                    var Crcs = ReplaceSign(frame._overlayTextureCrc, e.net.ID, CommunityEntity.ServerInstance.net.ID, 0);
                    if (Crcs == null)
                        return;

                    frame._overlayTextureCrc = Crcs[0];
                    storedData.Signs[e.net.ID].Add(new SignData()
                    {
                        InsCRC = Crcs[0],
                        PermaCRC = Crcs[1],
                        layer = 0,
                        time = DateTime.Now,
                        creator = 0
                    });
                }
                sign?.SendNetworkUpdate();
                frame?.SendNetworkUpdate();
           });
        }

        void RemoveSign(BaseEntity sign)
        {
            if (signs.Contains(sign))
                signs.Remove(sign);
        }
        
        bool SavePlayerSign(BasePlayer player, BaseEntity e, bool drawn, int layer, bool p) 
        {
            e = e == null ? GetClosestSign(player) : e;
            if (e == null) 
            {
                MsgUI(player, lang.GetMessage("MoveCloser", this));
                return false;
            }

            if (player.IsBuildingBlocked() && player.userID != e.OwnerID)
            {
                MsgUI(player, lang.GetMessage("NotOwner", this));
                return false;
            }

            var frame = e as PhotoFrame; 
            var sign = e as Signage;
            if (frame == null && sign == null)
            {
                MsgUI(player, lang.GetMessage("MoveCloser", this));
                return false;
            }

            uint[] ids = {0};

            ids = sign == null ? new uint[]{ frame._overlayTextureCrc } : sign.textureIDs;
            for (int i = 0; i < ids.Count(); i++)
            {
                if (layer != -1 && layer != i)
                    continue;

                bool update = storedData.Signs[e.net.ID].Where(x => x.PermaCRC == ids[i]).Any();
                var sds = storedData.Signs[e.net.ID].Where(x => x.InsCRC == ids[i]).ToList();

                if (sds.Any())
                {
                    foreach (var entry in storedData.UserID[player.userID].Where(x => x.PermaCRC == sds[0].PermaCRC))
                        return true;

                    storedData.UserID[player.userID].Add(new SignData()
                    {
                        PermaCRC = sds[0].PermaCRC,
                        time = DateTime.Now,
                        creator = sds[0].creator
                    });
                    return true;
                }

                var Crcs = ReplaceSign(ids[i], e.net.ID, CommunityEntity.ServerInstance.net.ID, Convert.ToUInt32(i)); 
                if (Crcs == null) 
                    continue;

                if (sign == null)
                    frame._overlayTextureCrc = Crcs[0];
                else
                    sign.textureIDs[i] = Crcs[0];

                if (!update)
                {
                    storedData.Signs[e.net.ID].Add(new SignData() 
                    {
                        InsCRC = Crcs[0],
                        PermaCRC = Crcs[1],
                        layer = Convert.ToUInt32(i),
                        time = DateTime.Now,
                        creator = player.userID
                    });

                    if (p) 
                    {
                        MsgUI(player, lang.GetMessage("AutoSaved", this));
                        storedData.UserID[player.userID].Add(new SignData()
                        {
                            PermaCRC = Crcs[1],
                            time = DateTime.Now,
                            creator = player.userID
                        });
                    }
                }
                 
                e.SendNetworkUpdate(); 
            }  
            return true; 
        }

        uint[] ReplaceSign(uint ID, uint entity, uint server, uint layer) 
        {
            var image = FileStorage.server.Get(ID, FileStorage.Type.png, entity, layer); 
            if (image == null)
                return null;

            using (var ms = new MemoryStream(image)) 
            {
                var bmp = new Bitmap(ms);
                var SignSave = FileStorage.server.Store(image.Concat(BitConverter.GetBytes(entity)).ToArray<byte>(), FileStorage.Type.png, entity, layer);
                var ServerSave = FileStorage.server.Store(image, FileStorage.Type.png, server, 0);
                return new uint[] { SignSave, ServerSave };
            }
        }

        bool RestoreSign(BasePlayer player, BaseEntity e, uint ID, uint layer)
        {
            e = e == null ? GetClosestSign(player) : e; 
            if (e == null)
            {
                MsgUI(player, lang.GetMessage("MoveCloser", this));
                return false;
            }

            if (player.IsBuildingBlocked() && player.userID != e.OwnerID)
            {
                MsgUI(player, lang.GetMessage("NotOwner", this)); 
                return false;
            }

            var frame = e as PhotoFrame; 
            var sign = e as Signage;
            
            if (frame == null && sign == null)
            {
                MsgUI(player, lang.GetMessage("MoveCloser", this));
                return false;
            }

            var Crcs = ReplaceSign(ID, e.net.ID, CommunityEntity.ServerInstance.net.ID, layer);
            if (Crcs == null)
                return false;

            if (sign != null)
                sign.textureIDs[layer] = Crcs[0];
            else
                frame._overlayTextureCrc = Crcs[0];
            e.SendNetworkUpdate();

            var record = storedData.Signs[e.net.ID].Where(x => x.PermaCRC == ID && x.layer == layer).ToList();
            if (record.Any())
            {
                record[0].time = DateTime.Now;
                record[0].creator = player.userID;
                return true;
            }

            storedData.Signs[e.net.ID].Add(new SignData()
            {
                InsCRC = Crcs[0],
                PermaCRC = Crcs[1],
                layer = layer,
                time = DateTime.Now,
                creator = player.userID  
            });

            return true;
        }

        BaseEntity GetClosestSign(BasePlayer player)
        {
            List<BaseEntity> signs = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(player.transform.position, 2f, signs);
            BaseEntity s = null;
            var distance = 2.1f;
            foreach (var sign in signs.Where(x=> x is Signage || x is PhotoFrame))
            {
                var d = Vector3.Distance(sign.transform.position, player.transform.position);
                if (d < distance)
                {
                    distance = d;
                    s = sign;
                }
            }
            return s; 
        }

        void Import() 
        {
            storedData.Signs.Clear();
            int counter = 0;
            foreach (var image in storedData.Export)
            {
                foreach (var entry in image.Value)
                {
                    if (entry.data == null)
                        continue;
                    var ID = FileStorage.server.Store(entry.data, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, entry.layer);
                    counter++;
                }
            }
            if (counter > 0)
            {
                storedData.Export.Clear();
                PrintWarning($"{counter} images have been restored.");
            }
        }

        void Export()  
        {
            List<uint> PermasInUse = new List<uint>();

            foreach (var sign in storedData.Signs)
            {
                foreach (var image in sign.Value)
                    if (FileStorage.server.Get(image.InsCRC, FileStorage.Type.png, sign.Key, 0) != null)
                        PermasInUse.Add(image.PermaCRC);

                foreach (var record in storedData.UserID)
                    foreach (var image in record.Value)
                        PermasInUse.Add(image.PermaCRC);
            }

            PermasInUse = PermasInUse.Distinct().ToList();

            int counter = 0;
            if (PermasInUse.Count > 0)
            {
                storedData.Export.Clear();
                foreach (var image in PermasInUse)
                {
                    if (!storedData.Export.ContainsKey(image))
                        storedData.Export.Add(image, new List<ExportInfo>());

                    for (int i = 0; i < 5; i++)
                    {
                        var file = FileStorage.server.Get(image, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, Convert.ToUInt32(i));
                        if (file != null)
                        {
                            storedData.Export[image].Add(new ExportInfo() { data = FileStorage.server.Get(image, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, Convert.ToUInt32(i)), layer = Convert.ToUInt32(i) });
                            counter++;
                        }
                    }
                }
            }
            if (counter > 0)
            {
                PrintWarning($"{counter} images will be restored the next time the server starts.");
                SaveData();
            }
            else
                PrintWarning($"There were no images to save.");
        }

        #region CUI
        void MsgUI(BasePlayer player, string message)
        {
            CuiHelper.DestroyUi(player, "MsgUI");
            timer.Once(2f, () =>
            {
                if (player != null)
                    CuiHelper.DestroyUi(player, "MsgUI");
            });
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { FadeIn = 0.3f, Color = $"0.1 0.1 0.1 0.8" }, RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = "0.95 0.9" }, CursorEnabled = false, FadeOut = 0.3f }, "Overlay", "MsgUI");
            elements.Add(new CuiLabel { FadeOut = 0.5f, Text = { FadeIn = 0.5f, Text = message, Color = "1 1 1 1", FontSize = 28, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyMenu(player);

            if (storedData != null)
                SaveData();
        }

        void OnPlayerDisconnected(BasePlayer player) => DestroyMenu(player);

        void DestroyMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SMUI");
            CuiHelper.DestroyUi(player, "SMAUI");
        } 

        void SMUI(BasePlayer player)
        {
            var sign = GetClosestSign(player);

            if (sign == null)
            {
                MsgUI(player, lang.GetMessage("MoveCloser", this));
                return;
            }

            DestroyMenu(player); 
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = $"0.1 0.1 0.1 0.94" }, RectTransform = { AnchorMin = "0.05 0.15", AnchorMax = "0.95 0.9" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "SMUI");
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 1" }, Text = { Text = String.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.999 0.05" }, Text = { Text = String.Empty } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = $"{player.displayName} : Sign Manager", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            double l1 = 0.11, l2 = 0.25,  h1 = 0.78, h2 = 0.92;
            int column = 0, counter = 0;
            Signage s = sign as Signage;
            PhotoFrame p = sign as PhotoFrame;
            var neon = sign == null ? null : sign as NeonSign;
            var first = s != null ? s.textureIDs[0] : p != null ? p._overlayTextureCrc : 0;

            foreach (var entry in storedData.UserID[player.userID].ToList())
            {
                elements.Add(new CuiButton { Button = { Color = "1 1 1 0.3" }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h1}", AnchorMax = $"{l2 + column * 0.16} {h2}" }, Text = { Text = String.Empty } }, mainName);
                elements.Add(new CuiElement { Parent = mainName, Components = { new CuiRawImageComponent { Png = entry.PermaCRC.ToString(), Sprite = sprite }, new CuiRectTransformComponent { AnchorMin = $"{l1 + column * 0.16} {h1}", AnchorMax = $"{l2 + column * 0.16} {h2}" }, }, });

                if (sign != null)
                {
                    if (neon != null && neon.paintableSources.Count() > 1)
                    {
                        elements.Add(new CuiButton { Button = { Command = $"SM Restore {entry.PermaCRC} 0", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.013} {h2}" }, Text = { Text = "1", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"SM Restore {entry.PermaCRC} 1", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.015} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.027} {h2}" }, Text = { Text = "2", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"SM Restore {entry.PermaCRC} 2", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.030} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.042} {h2}" }, Text = { Text = "3", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.paintableSources.Count() > 3)
                        {
                            elements.Add(new CuiButton { Button = { Command = $"SM Restore {entry.PermaCRC} 3", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.045} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.057} {h2}" }, Text = { Text = "4", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                            elements.Add(new CuiButton { Button = { Command = $"SM Restore {entry.PermaCRC} 4", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.060} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.072} {h2}" }, Text = { Text = "5", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        }
                    }
                    else
                        elements.Add(new CuiButton { Button = { Command = $"SM Restore {entry.PermaCRC} 0", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.016} {h2}" }, Text = { Text = "Set" , Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter} }, mainName);
                }  

                elements.Add(new CuiButton { Button = { Command = $"SM Delete {entry.PermaCRC}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l2 + column * 0.16 - 0.014} {h2 - 0.020}", AnchorMax = $"{l2 + column * 0.16} {h2}" }, Text = { Text = "X", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h1 - 0.02}", AnchorMax = $"{l2 + column * 0.16} {h1}" }, Text = { Text = $"{entry.time}", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

                h1 -= 0.17; h2 -= 0.17;
                counter++;

                if (counter % 5 == 0)
                {
                    column++;
                    h1 = 0.78; h2 = 0.92;
                }
            }

            if (HasPermission(player.UserIDString, permManual)) 
            {
                if (storedData.UserID[player.userID].Count() < 25)
                { 
                    if (neon != null && neon.paintableSources.Count() == 3)
                    {
                        if (neon.textureIDs[0] != 0 && !GotSign(player.userID, neon.textureIDs[0]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 0", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.3 0", AnchorMax = $"0.4 0.05" }, Text = { Text = "Save 1", Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.textureIDs[1] != 0 && !GotSign(player.userID, neon.textureIDs[1]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 1", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.45 0", AnchorMax = $"0.55 0.05" }, Text = { Text = "Save 2", Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.textureIDs[2] != 0 && !GotSign(player.userID, neon.textureIDs[2]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 2", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.6 0", AnchorMax = $"0.7 0.05" }, Text = { Text = "Save 3", Align = TextAnchor.MiddleCenter } }, mainName);
                    }
                    else if (neon != null && neon.paintableSources.Count() == 5)
                    {
                        if (neon.textureIDs[0] != 0 && !GotSign(player.userID, neon.textureIDs[0]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 0", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.15 0", AnchorMax = $"0.25 0.05" }, Text = { Text = "Save 1", Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.textureIDs[1] != 0 && !GotSign(player.userID, neon.textureIDs[1]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 1", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.3 0", AnchorMax = $"0.4 0.05" }, Text = { Text = "Save 2", Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.textureIDs[2] != 0 && !GotSign(player.userID, neon.textureIDs[2]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 2", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.45 0", AnchorMax = $"0.55 0.05" }, Text = { Text = "Save 3", Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.textureIDs[3] != 0 && !GotSign(player.userID, neon.textureIDs[3]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 3", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.6 0", AnchorMax = $"0.7 0.05" }, Text = { Text = "Save 4", Align = TextAnchor.MiddleCenter } }, mainName);
                        if (neon.textureIDs[4] != 0 && !GotSign(player.userID, neon.textureIDs[4]))
                            elements.Add(new CuiButton { Button = { Command = "SM Save 4", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.75 0", AnchorMax = $"0.85 0.05" }, Text = { Text = "Save 5", Align = TextAnchor.MiddleCenter } }, mainName);
                    }
                    else if (first != 0 && !GotSign(player.userID, first))
                        elements.Add(new CuiButton { Button = { Command = "SM Save -1", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.4 0", AnchorMax = $"0.6 0.05" }, Text = { Text = "Save Image", Align = TextAnchor.MiddleCenter } }, mainName);
                }

            }
            elements.Add(new CuiButton { Button = { Command = "SM Close", Color = configData.ButtonColour }, RectTransform = { AnchorMin = "0.97 0.95", AnchorMax = "1 1" }, Text = { Text = "X", FontSize = 12, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        bool GotSign(ulong playerid, uint id)
        {
            foreach (var sign in storedData.Signs)
                foreach (var entry in sign.Value.Where(x => x.InsCRC == id))
                    if (storedData.UserID[playerid].Where(x => x.PermaCRC == entry.PermaCRC).Any())
                        return true;
            return false;
        }

        [ConsoleCommand("SM")]
        private void SMCmd(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;

            DestroyMenu(arg.Player());

            switch (arg.Args[0])
            {
                case "Close":
                    break;
                case "Save":
                    if (SavePlayerSign(arg.Player(), null, false, Convert.ToInt16(arg.Args[1]), true))
                        SMUI(arg.Player());
                    break;
                case "Restore":
                    if (RestoreSign(arg.Player(), null, Convert.ToUInt32(arg.Args[1]), Convert.ToUInt32(arg.Args[2])))
                        SMUI(arg.Player());
                    break;
                case "Delete":
                    foreach (var record in storedData.UserID[arg.Player().userID])
                    {
                        if (record.PermaCRC == Convert.ToUInt32(arg.Args[1]))
                        {
                            storedData.UserID[arg.Player().userID].Remove(record);
                            break;
                        }
                    }
                    SMUI(arg.Player());
                    break;
            }
        }

        //// ADMIN
        void SMAUI(BasePlayer player, BaseEntity sign, ulong userID) 
        {
            if (sign == null)
                sign = GetClosestSign(player);

            if (sign == null && userID == 0)
            {
                MsgUI(player, lang.GetMessage("MoveCloserOrSpecify", this));
                return;
            }

            DestroyMenu(player);
            BasePlayer target = BasePlayer.FindAwakeOrSleeping(userID.ToString());
            string name = userID == 0 ? sign.net.ID.ToString() : target == null ? userID.ToString() : target.displayName;
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = $"0.1 0.1 0.1 0.94" }, RectTransform = { AnchorMin = "0.05 0.15", AnchorMax = "0.95 0.9" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "SMAUI");
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 1" }, Text = { Text = String.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.999 0.05" }, Text = { Text = String.Empty } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = $"{name} : Sign Manager (ADMIN)", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            double l1 = 0.11, l2 = 0.25, h1 = 0.78, h2 = 0.92;
            int column = 0, counter = 0;

            List<SignData> record = new List<SignData>();
            if (userID != 0)
                record = storedData.UserID[userID];
            else
            {
                Back[player.userID] = sign;
                record = storedData.Signs[sign.net.ID];
            }

            if (record.Count == 0)
            {
                if (userID != 0)
                    MsgUI(player, lang.GetMessage("NoPlayerHistory", this));
                else
                    MsgUI(player, lang.GetMessage("NoHistory", this));
                return;
            }

            var neon = sign as NeonSign; 
            for (int i = 0; i < Mathf.Min(25, record.Count); i++)
            {
                elements.Add(new CuiButton { Button = { Color = "1 1 1 0.3" }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h1}", AnchorMax = $"{l2 + column * 0.16} {h2}" }, Text = { Text = String.Empty } }, mainName);
                elements.Add(new CuiElement { Parent = mainName, Components = { new CuiRawImageComponent { Png = record[i].PermaCRC.ToString(), Sprite = sprite }, new CuiRectTransformComponent { AnchorMin = $"{l1 + column * 0.16} {h1}", AnchorMax = $"{l2 + column * 0.16} {h2}" }, }, });

                if (neon != null && neon.paintableSources.Count() > 1)
                {  
                    elements.Add(new CuiButton { Button = { Command = $"SMA Restore {record[i].PermaCRC} 0 {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h2 - 0.025}", AnchorMax = $"{l1 + column * 0.16 + 0.015} {h2}" }, Text = { Text = "1", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"SMA Restore {record[i].PermaCRC} 1 {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.016} {h2 - 0.025}", AnchorMax = $"{l1 + column * 0.16 + 0.030} {h2}" }, Text = { Text = "2", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"SMA Restore {record[i].PermaCRC} 2 {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.031} {h2 - 0.025}", AnchorMax = $"{l1 + column * 0.16 + 0.045} {h2}" }, Text = { Text = "3", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                    if (neon.paintableSources.Count() > 3)
                    {
                        elements.Add(new CuiButton { Button = { Command = $"SMA Restore {record[i].PermaCRC} 3 {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.046} {h2 - 0.025}", AnchorMax = $"{l1 + column * 0.16 + 0.060} {h2}" }, Text = { Text = "4", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        elements.Add(new CuiButton { Button = { Command = $"SMA Restore {record[i].PermaCRC} 4 {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16 + 0.061} {h2 - 0.025}", AnchorMax = $"{l1 + column * 0.16 + 0.075} {h2}" }, Text = { Text = "5", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                    }
                }
                else
                    elements.Add(new CuiButton { Button = { Command = $"SMA Restore {record[i].PermaCRC} 0 {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h2 - 0.02}", AnchorMax = $"{l1 + column * 0.16 + 0.016} {h2}" }, Text = { Text = "Set", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
   
                elements.Add(new CuiButton { Button = { Command = $"SMA Delete {record[i].PermaCRC} {userID}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l2 + column * 0.16 - 0.015} {h2 - 0.025}", AnchorMax = $"{l2 + column * 0.16} {h2}" }, Text = { Text = "X", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"SMA Player {record[i].creator}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{l1 + column * 0.16} {h1 - 0.02}", AnchorMax = $"{l2 + column * 0.16} {h1}" }, Text = { Text = $"{record[i].creator}", Font = Font, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

                h1 -= 0.17; h2 -= 0.17;
                counter++;
                 
                if (counter % 5 == 0)
                {
                    column++;
                    h1 = 0.78; h2 = 0.92;
                }
            }

            if (userID != 0 && Back.ContainsKey(player.userID) && Back[player.userID] != null)
                elements.Add(new CuiButton { Button = { Command = "SMA Back", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.2 0", AnchorMax = $"0.3 0.05" }, Text = { Text = "<-", Align = TextAnchor.MiddleCenter } }, mainName);

            elements.Add(new CuiButton { Button = { Command = "SMA Close", Color = configData.ButtonColour }, RectTransform = { AnchorMin = "0.97 0.95", AnchorMax = "1 1" }, Text = { Text = "X", FontSize = 12, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("SMA")]
        private void SMACmd(ConsoleSystem.Arg arg)  
        {
            if (arg.Player() == null)
            {
                if (arg.Args[0] == "export")
                    Export();
                return;
            }

            DestroyMenu(arg.Player());

            switch (arg.Args[0])
            {
                case "Close":
                    if (Back.ContainsKey(arg.Player().userID)) 
                        Back.Remove(arg.Player().userID);
                    break;
                case "Back":
                    SMAUI(arg.Player(), Back[arg.Player().userID], 0);
                    break;
                case "Player":
                    SMAUI(arg.Player(), null, Convert.ToUInt64(arg.Args[1]));
                    break;
                case "Restore": 
                    if (RestoreSign(arg.Player(), null, Convert.ToUInt32(arg.Args[1]), Convert.ToUInt32(arg.Args[2])))
                        SMAUI(arg.Player(), GetClosestSign(arg.Player()), Convert.ToUInt64(arg.Args[3]));
                    break;
                case "Delete": ////Does the whole sign?
                    var id = Convert.ToUInt32(arg.Args[1]);

                    foreach (var record in storedData.UserID.ToList())
                        foreach (var entry in record.Value.ToList())
                            if (entry.PermaCRC == id)
                                storedData.UserID[record.Key].Remove(entry);

                    foreach (var record in storedData.Signs.ToList()) 
                        foreach (var s in record.Value.ToList())
                            if (s.PermaCRC == id)
                            {
                                FileStorage.server.Remove(s.InsCRC, FileStorage.Type.png, record.Key);
                                storedData.Signs[record.Key].Remove(s);
                            }

                    FileStorage.server.Remove(id, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);

                    Signage sign; 
                    PhotoFrame frame;
                    foreach (var s in signs) 
                    {
                        sign = s as Signage;
                        frame = s as PhotoFrame;
                        if (sign != null)
                            for (int i = 0; i < sign.textureIDs.Count(); i++)
                                if (FileStorage.server.Get(sign.textureIDs[i], FileStorage.Type.png, s.net.ID, Convert.ToUInt32(i)) == null)
                                    sign.textureIDs[i] = 0;

                        if (frame != null)
                            if (FileStorage.server.Get(frame._overlayTextureCrc, FileStorage.Type.png, s.net.ID, 0) == null)
                                frame._overlayTextureCrc = 0;

                        s.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }
                    SMAUI(arg.Player(), GetClosestSign(arg.Player()), Convert.ToUInt64(arg.Args[2]));
                    break;
            }
        }
        #endregion

        #region Permissions and Lang
        const string permAdmin = "SignManager.admin";
        const string permAuto = "SignManager.autosave";  
        const string permManual = "SignManager.manualsave";

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        readonly Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"Title", "SignManager : " },
            {"MoveCloser", "Move closer to a sign or frame." },
            {"MoveCloserOrSpecify", "Move closer to a sign or frame, or specify a SteamID." },
            {"Blank", "This sign or frame is blank." },
            {"NoHistory", "This sign has no image history." },
            {"NoPlayerHistory", "This player has no image history." },
            {"NotOwner", "You need to own this sign, or have building priv." },
            {"AutoSaved", "Your image has been autosaved. Use /sm to view or manage." },
        };

        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }

            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permAuto, this);
            permission.RegisterPermission(permManual, this);
        }
        #endregion

        #region Commands
        [ChatCommand("sm")]
        void SM(BasePlayer player, string command, string[] args) 
        {
            if (!HasPermission(player.UserIDString, permAuto) && !HasPermission(player.UserIDString, permManual))
                return;
            SMUI(player);
        }

        [ChatCommand("sma")]
        void SMA(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player.UserIDString, permAdmin) && !player.IsAdmin)
                return;

            ulong id = 0;
            if (args.Length == 1)
            {
                if (args[0] == "export") 
                {
                    Export();
                    return;
                }
                ulong.TryParse(args[0], out id); 
            }
              
            SMAUI(player, GetClosestSign(player), id);
        }
        #endregion

        #region Data
        StoredData storedData;
        class StoredData
        {
            public Dictionary<uint, List<SignData>> Signs = new Dictionary<uint, List<SignData>>();
            public Dictionary<ulong, List<SignData>> UserID = new Dictionary<ulong, List<SignData>>();
            public Dictionary<uint, List<ExportInfo>> Export = new Dictionary<uint, List<ExportInfo>>();
        }

        public class ExportInfo
        {
            public byte[] data;
            public UInt32 layer;
        }

        public class SignData
        {
            public uint PermaCRC;
            public uint InsCRC;
            public UInt32 layer;
            public DateTime time;
            public ulong creator = 0;
        }

        void Loaded() => storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("SignManager");
        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("SignManager", storedData);
        #endregion

        #region Config
        //LoadConfigVariables - See Init()
        private ConfigData configData;

        class ConfigData
        {
            public string CommandAlias = "SignManager";
            public string AdminCommandAlias = "SignManagerAdmin";
            public string ButtonColour = "0.7 0.32 0.17 1";
        }

        private bool LoadConfigVariables()
        {
            try { configData = Config.ReadObject<ConfigData>(); }
            catch { return false; }
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion
    }
}