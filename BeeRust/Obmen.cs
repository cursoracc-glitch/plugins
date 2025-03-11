using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Obmen", "kurushimu", "1.1")]
    class Obmen : RustPlugin
    {
        private bool loaded = false;
        
        private string Layer = "https://gspics.org/images/2022/02/02/04Kz73.jpg";
        
        string[] FirstWord = { "100К КАМНЯ НА 200К ДЕРЕВА", "100К КАМНЯ НА 2 ТУРЕЛИ", "500К КАМНЯ НА 2К МВК", "125 СКРАПА НА БУР"};
        string[] FirsttWord = { "50К СКРАПА НА 20К МЕТАЛЛА", "50К СКРАПА НА 100К ТНК", "500К ЖЕЛЕЗА НА 5К МВК", "200К ЖЕЛЕЗА НА 50К СЕРЫ"};
        string[] Command = { "obmen 0", "obmen 1", "obmen 2", "obmen 3"};
        string[] Commandd = { "obmen 4", "obmen 5", "obmen 6", "obmen 7"};

        void OnServerInitialized()
        {
            InitFileManager();
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }
        
        private Dictionary<string, string> images = new Dictionary<string, string>()
        {
            ["Img0"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120008767187198073/8eTRs6p.png",
            ["Img1"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120008767187198073/8eTRs6p.png",
            ["Img2"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120008767187198073/8eTRs6p.png",
            ["Img3"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120019074156601384/c0fa952d07bbf02f55ed344b3260763b.png",
            ["Imgg0"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120019074156601384/c0fa952d07bbf02f55ed344b3260763b.png",
            ["Imgg1"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120019074156601384/c0fa952d07bbf02f55ed344b3260763b.png",
            ["Imgg2"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120008826100404285/b993ZWx.png",
            ["Imgg3"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120008826100404285/b993ZWx.png",
            
            ["Image0"] = "https://cdn.discordapp.com/attachments/1092142749035278414/1107663194513747968/Z4vaStO.png",
            ["Image1"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120021705339981834/autoturret.png",
            ["Image2"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120023014612947014/metal.png",
            ["Image3"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120023133819252858/jackhammer.png",
            ["Imagge0"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120008826100404285/b993ZWx.png",
            ["Imagge1"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120019173070884904/b71e9693f9f878566e75d324240fb41e.png",
            ["Imagge2"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120023014612947014/metal.png",
            ["Imagge3"] = "https://cdn.discordapp.com/attachments/1120005237642637352/1120009100424659004/gwRK5qo.png",
            ["bgr"] = "https://cdn.discordapp.com/attachments/1108899003933933610/1119372699567931502/sdfgdsf.png",
            ["but"] = "https://cdn.discordapp.com/attachments/1108899003933933610/1119376261559623720/sd2fgdsf.png",
            
        };

        IEnumerator LoadImages()
        {
            foreach (var name in images.Keys.ToList())
            {
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(name, images[name]));
                images[name] = m_FileManager.GetPng(name);
            }
            loaded = true;
        }
        
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }

        private void DrawInterface(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.8", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.3f }
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
            }, Layer);


container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent() {Png = images[$"bgr"], Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = $"0.3784375 0.7475927", AnchorMax = $"0.6191041 0.8401851"}
                    }
                });

            container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent() { Png = images[$"bgr"], Color = "1 1 1 1" },
                        new CuiRectTransformComponent(){  AnchorMin = "0.1358333 0.2010741", AnchorMax = "0.8641041 0.859926", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent {Color = "1 1 1 1", Text = $"BeeRust", FontSize = 25, Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent {AnchorMin = $"0.3784375 0.7475927", AnchorMax = $"0.6191041 0.8401851"},
                        new CuiOutlineComponent {Distance = "0.4 0.4", Color = "0 0 0 1"}
                    }
                });


            
     
            double anchor1 = 0.64;
            double anchor2 = 0.74;
            double anchor3 = 0.645;
            double anchor4 = 0.735;
            double anchor1_1 = 0.645;
            double anchor1_2 = 0.735;


            for (int i = 0; i < 4; i++)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent() {Png = images[$"but"], Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = $"0.26 {anchor1}", AnchorMax = $"0.48 {anchor2}"}
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent() {Png = images[$"but"], Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = $"0.52 {anchor1}", AnchorMax = $"0.74 {anchor2}"}
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $"Imgg{i}",
                    Components =
                    {
                        new CuiRawImageComponent() { Png = images[$"Imgg{i}"], Color = "1 1 1 1" },
                        new CuiRectTransformComponent(){  AnchorMin = $"0.535 {anchor1_1}", AnchorMax = $"0.585 {anchor1_2}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $"Imagge{i}",
                    Components =
                    {
                        new CuiRawImageComponent() { Png = images[$"Imagge{i}"], Color = "1 1 1 1"},
                        new CuiRectTransformComponent(){  AnchorMin = $"0.68 {anchor1_1}", AnchorMax = $"0.73 {anchor1_2}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.52 {anchor3}", AnchorMax = $"0.74 {anchor4}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0", Command = $"{Commandd[i]}"},
                    Text = { Text = $"{FirsttWord[i]}", Align = TextAnchor.MiddleCenter, FontSize = 10 }
                }, Layer);



                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $"Img{i}",
                    Components =
                    {
                        new CuiRawImageComponent() { Png = images[$"Img{i}"], Color = "1 1 1 1" },
                        new CuiRectTransformComponent(){  AnchorMin = $"0.275 {anchor1_1}", AnchorMax = $"0.325 {anchor1_2}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $"Image{i}",
                    Components =
                    {
                        new CuiRawImageComponent() { Png = images[$"Image{i}"], Color = "1 1 1 1"},
                        new CuiRectTransformComponent(){  AnchorMin = $"0.42 {anchor1_1}", AnchorMax = $"0.47 {anchor1_2}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.26 {anchor3}", AnchorMax = $"0.48 {anchor4}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0", Command = $"{Command[i]}"},
                    Text = { Text = $"{FirstWord[i]}", Align = TextAnchor.MiddleCenter, FontSize = 10 }
                }, Layer);
                
                anchor1 -= 0.105;
                anchor2 -= 0.107;
                anchor1_1 -= 0.105;
                anchor1_2 -= 0.105;
                anchor3 -= 0.11;
                anchor4 -= 0.11;
                
                CuiHelper.DestroyUi(player, Layer + $"Img{i}");
                CuiHelper.DestroyUi(player, Layer + $"Image{i}");
            } 
            
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private Dictionary<string, int> _itemIds = new Dictionary<string, int>();
		private int FindItemID(string shortName)
		{
			int val;
			if (_itemIds.TryGetValue(shortName, out val))
				return val;

			var definition = ItemManager.FindItemDefinition(shortName);
			if (definition == null) return 0;

			val = definition.itemid;
			_itemIds[shortName] = val;
			return val;
		}

        [ChatCommand("obmen")]
        void Gui(BasePlayer player)
        {
            DrawInterface(player);
        }


        [ConsoleCommand("obmen")]
        void Obmen2(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var cislo = args.Args[0];
            string newitem = "";
            string check = "";
            int z1 = 0;
            int z2 = 0;
            

            if (cislo == "0")
            {
                check = "stones";
                newitem = "wood";
                z1 = 100000;
                z2 = 200000;
            }
            if (cislo == "1")
            {
                check = "stones";
                newitem = "autoturret";
                z1 = 100000;
                z2 = 2;
            }
            if (cislo == "2")
            {
                check = "stones";
                newitem = "metal.refined";
                z1 = 500000;
                z2 = 2000;
            }
            if (cislo == "3")
            {
                check = "scrap";
                newitem = "jackhammer";
                z1 = 125;
                z2 = 1;
            }
            if (cislo == "4")
            {
                check = "scrap";
                newitem = "metal.fragments";
                z1 = 50000;
                z2 = 20000;
            }
            if (cislo == "5")
            {
                check = "scrap";
                newitem = "lowgradefuel";
                z1 = 50000;
                z2 = 100000;
            }
            if (cislo == "6")
            {
                check = "metal.fragments";
                newitem = "metal.refined";
                z1 = 500000;
                z2 = 5000;
            }
            if (cislo == "7")
            {
                check = "metal.fragments";
                newitem = "sulfur";
                z1 = 200000;
                z2 = 50000;
            }
            
            var count = player.inventory.GetAmount(ItemManager.FindItemDefinition(check).itemid);
            if(count>=z1)
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(check).itemid, z1);
                Item item;
                item = ItemManager.CreateByName(newitem, z2);
                player.GiveItem(item);
                SendReply(player, "Вы <color=#97be62>успешно</color> обменяли ресурсы");
            }
            else
            {
                SendReply(player, "У вас <color=red>недостаточно</color> ресурсов");
            }
        }
        
        		private GameObject FileManagerObject;
        private FileManager m_FileManager;

        void InitFileManager()
        {
            FileManagerObject = new GameObject("MAP_FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("Images");

            private class FileInfo
            {
                public string Url;
                public string Png;
            }

            public void SaveData()
            {
                dataFile.WriteObject(files);
            }

            public string GetPng(string name) => files[name].Png;

            private void Awake()
            {
                files = dataFile.ReadObject<Dictionary<string, FileInfo>>() ?? new Dictionary<string, FileInfo>();
            }

            public IEnumerator LoadFile(string name, string url)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url));
            }

            IEnumerator LoadImageCoroutine( string name, string url)
            {
                using (WWW www = new WWW( url ))
                {
                    yield return www;
                    using (MemoryStream stream = new MemoryStream())
                    {
                        if (string.IsNullOrEmpty( www.error ))
                        {
                            stream.Position = 0;
                            stream.SetLength( 0 );

                            var bytes = www.bytes;

                            stream.Write( bytes, 0, bytes.Length );

                            var entityId = CommunityEntity.ServerInstance.net.ID;
                            var crc32 = FileStorage.server.Store(stream.ToArray(), FileStorage.Type.png, entityId).ToString();
                            files[ name ].Png = crc32;
                        }
                    }
                }
                loaded++;
            }
        }
    }
}