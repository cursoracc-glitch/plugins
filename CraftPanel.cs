using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins{

    [Info("CraftPanel", "TopPlugin.ru", "1.3.1")]
    [Description("Simple Custom Crafting Interface")]
    public class CraftPanel : RustPlugin {

        #region Fields
        [PluginReference] Plugin ImageLibrary;
        private const string elemq0 = "craftpanel.base";
        private const string elemq1 = "craftpanel.main";
        private const string elemq2 = "craftpanel.error";
        private const string elemq7 = "craftpanel.subdetails";
        private const string elemq6 = "craftpanel.list";
        private const string elemqitemvip = "craftpanel.itemvip";
        private bool _isCraftReady;
        private List<CraftInfo> craftList;
        #endregion

        private void Init(){
            cmd.AddConsoleCommand("craftpanel.craft", this, nameof(cmdCraft));
            cmd.AddConsoleCommand("craftpanel.close", this, nameof(cmdCloseConsole));
            cmd.AddConsoleCommand("craftpanel.item", this, nameof(cmdInfoVipConsole));
            cmd.AddConsoleCommand("craftpanel.close2", this, nameof(cmdClose2Console));
            cmd.AddConsoleCommand("craftpanel.page", this, nameof(cmdItemsPage));
        }

        private void OnServerInitialized(){
            LoadImages();
            Permissions();
        }

        private void Unload(){
            foreach (var player in BasePlayer.activePlayerList){
                ClosePanelNoSound(player, false);
            }        
        }

        #region Comandos
        [ChatCommand("craft")]
        void chatopenvip(BasePlayer player){
            if (HasPermission(player.UserIDString, config.Custom.permissionuse)){
                if (!_isCraftReady){
                    PrintToChat(player, Lang("MessageResponse", player.UserIDString));
                    return;
                }
                OpenPanelCraft(player);
            } else {
                PrintToChat(player, Lang("NotAllowed", player.UserIDString));
            }
        }
        #endregion

        #region Functions
        private void cmdCloseConsole(ConsoleSystem.Arg arg){
            ClosePanel(arg.Player(), true);
        }
        private void cmdClose2Console(ConsoleSystem.Arg arg){
            Efecto(arg.Player(), config.Custom.sound1);
            CuiHelper.DestroyUi(arg.Player(), elemq2);
        }

        private void cmdInfoVipConsole(ConsoleSystem.Arg arg){
            cmdInfoVip(arg.Player(), string.Empty, arg.Args);
        }
        private void cmdInfoVip(BasePlayer player, string command, string[] args){
            var page = int.Parse(args[1]);
            GetPanelItem(player, args[0], page);
            Efecto(player, config.Custom.sound1);
        }

        private void cmdCraft(ConsoleSystem.Arg arg){
            cmdCrafting(arg.Player(), string.Empty, arg.Args);
        }

        private void cmdCrafting(BasePlayer player, string command, string[] args){
            int craftitem = int.Parse(args[0]);
            if (CanCraftItem(player,craftitem)){
                var craft = config.Craft[craftitem].result;
                var comando = craft.command;
                if (string.IsNullOrEmpty(comando)) {
                    var item = ItemManager.CreateByName(craft.shortname, craft.amount, craft.skinID);
                    item.name = config.Craft[craftitem].name;
                    player.GiveItem(item);
                } else {
                    comando = comando.Replace("{steamID}", player.UserIDString);
                    Server.Command(comando);
                }
                Efecto(player, config.Custom.sound2);
            }
        }

        private void cmdItemsPage(ConsoleSystem.Arg arg){
            var player = arg.Player();
            var page = int.Parse(arg.Args[1]);
            GetPanelItem(player, arg.Args[0], page);
            Efecto(player, config.Custom.sound1);
        }
  
        private void Permissions(){
            permission.RegisterPermission(config.Custom.permissionuse, this);
            foreach (var recipe in config.Craft){
                var permCraft = recipe.permission;
                var permCraftVIP = recipe.permissionVIP;
                var permCraftNoCost = recipe.permissionNoCost;
                permission.RegisterPermission(permCraft, this);
                permission.RegisterPermission(permCraftVIP, this);
                permission.RegisterPermission(permCraftNoCost, this);
            }
        }

        private bool HasPermission(string userID, string perm){
            return string.IsNullOrEmpty(perm) || permission.UserHasPermission(userID, perm);
        }

        private void LoadImages(){
            Dictionary<string, string> imageListCraft = new Dictionary<string, string>();
            List<KeyValuePair<string, ulong>> itemIcons = new List<KeyValuePair<string, ulong>>();
            imageListCraft.Add("block", config.Custom.imgblock);

            foreach (var recipe in config.Craft){
                if (recipe.img != "" && !imageListCraft.ContainsKey(recipe.img)) {
                    imageListCraft.Add(recipe.img, recipe.img);
                }

                if (recipe.imgicon != "" && !imageListCraft.ContainsKey(recipe.imgicon)) {
                    imageListCraft.Add(recipe.imgicon, recipe.imgicon);
                }

                 foreach (var shopItem in recipe.items){
                     if (shopItem.item.Contains("hazmatsuit.spacesuit") || shopItem.item.Contains("attire.ninja.suit") || shopItem.item.Contains("workcart")) continue;

                    itemIcons.Add(new KeyValuePair<string, ulong>(shopItem.item, shopItem.skinID));
                 }
            }

            if (itemIcons.Count > 0){
                ImageLibrary?.Call("LoadImageList", Title, itemIcons, null);
            }

            ImageLibrary?.Call("ImportImageList", Title, imageListCraft, 0UL, true, new Action(CraftReady));
        }

        private void CraftReady(){
            _isCraftReady = true;
        }

        /*private void AddImage(string name, string url){
                if (ImageLibrary == null || !ImageLibrary.IsLoaded){
                    timer.Once(1f, () => {
                        AddImage(name, url);
                    });
                    return;
                }
                ImageLibrary.CallHook("AddImage", url, name, (ulong) 0);
        }*/

        private string GetImageLibrary(string name, ulong skinid = 0){ 
            return ImageLibrary?.Call<string>("GetImage", name, skinid); 
        }

        void Efecto(BasePlayer player, String prefab){ 
            if(config.Custom.soundeffects){
                Effect.server.Run(prefab, player.transform.position, Vector3.up, null, true); 
            }
        }

        private List<CraftInfo> GetCraftItemsForPlayer(BasePlayer player){
            //config.Custom.showWithoutPerm
            return craftList.Where(kit => (string.IsNullOrEmpty(kit.permission) || 1 == 1 || permission.UserHasPermission(player.UserIDString, kit.permission)) ).ToList();
        }
        #endregion

        #region GUI
        private void OpenPanelCraft(BasePlayer player){
            CuiHelper.DestroyUi(player, elemq0);
            var container = new CuiElementContainer();
            container.Add(new CuiElement {
                Name = elemq0,
                Parent = "Overlay",
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = "0 0 0 0.8",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-1000 -800",
                        OffsetMax = "1000 800"
                    },
                    new CuiNeedsCursorComponent()
                }
            });

            UI.Label(ref container, elemq0, config.Custom.title, 26, "0.202 0.67", "0.3 0.69", config.Custom.colortitle, TextAnchor.MiddleLeft);

            CuiHelper.AddUi(player, container);
            OpenPanelItem(player, "0", 0);
        }

        private void OpenPanelItem(BasePlayer player, string item, int page){
            GetPanelItem(player, item, page);
            Efecto(player, config.Custom.sound1);
        }

        private void OpenPanelError(BasePlayer player, string textshow){
            Efecto(player, "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab");
            var container = new CuiElementContainer();
            container.AddRange(GetPanel2(player, textshow));
            CuiHelper.DestroyUi(player, elemq2);
            CuiHelper.AddUi(player, container);
            timer.Once(8, () => CuiHelper.DestroyUi(player, elemq2));
        }

        private void ClosePanel(BasePlayer player, bool sonido){
            Efecto(player, config.Custom.sound1);
            CuiHelper.DestroyUi(player, elemq0);
        }

        private void ClosePanelNoSound(BasePlayer player, bool sonido){
            CuiHelper.DestroyUi(player, elemq0);
        }
      
        //---Panel Info Item
        private void GetPanelItem(BasePlayer player, string i, int page = 0){
            CuiHelper.DestroyUi(player, elemq1);
            var CraftItems = GetCraftItemsForPlayer(player).Skip(16 * page).Take(16).ToList();
            var container = new CuiElementContainer();

            container.Add(new CuiPanel{
                Image = {
                    Color="1 1 1 0"
                },
                RectTransform = {
                    AnchorMin=$"0 0", AnchorMax=$"1 1"
                }
            }, elemq0, elemq1);

            UI.Button(ref container, elemq1, config.Custom.colorbtnclose, Lang("Close", player.UserIDString), 15, "0.7 0.67", "0.79 0.69", $"craftpanel.close");

            //Lista de Items
            var list_sizeX = 80;
            var list_sizeY = 80;
            var list_startX = -600;
            var list_startY = 250;
            var list_x = list_startX;
            var list_y = list_startY;

            var po = 0;
            var e = 0;

            if (CraftItems.Count > 0) foreach (var list_entry in CraftItems){
                //var list_entry = config.Craft[e];
                var perm = list_entry.permission;
                    
                if (po != 0 && po % 4 == 0){
                    list_x = list_startX;
                    list_y -= list_sizeY + 40;
                }
                
                po++;
                
                string list_name = list_entry.name;
                string list_imgicon = list_entry.imgicon;

                container.Add(new CuiElement {
                    Name = elemq6, Parent = elemq1, Components = {
                        new CuiRawImageComponent{
                            Png = GetImageLibrary(list_imgicon)
                        },
                        new CuiRectTransformComponent{
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{list_x} {list_y-list_sizeY}",
                            OffsetMax = $"{list_x + list_sizeX} {list_y}"
                        }
                    }
                });

                if (!HasPermission(player.UserIDString, perm)){
                    UI.Image(ref container, elemq6, GetImageLibrary("block"), "0.1 0.1", "0.9 0.9");
                }
                container.Add(new CuiButton {
                    Button = {
                        Command = page > 0 ? $"craftpanel.item {e + (page * 16)} {page}" : $"craftpanel.item {e} {page}",
                        Color = config.Custom.colorbtnlist,
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    Text = {
                        Text = list_name,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Font = "robotocondensed-regular.ttf"
                    },
                    RectTransform = {
                        AnchorMin = "0 -0.32",
                        AnchorMax = "1 -0.05"
                    }
                }, elemq6);
                
                list_x += list_sizeX + 15;
                
                e++;
            }

            if (config.Craft.Count > 16 || page != 0){
                UI.Button(ref container, elemq1, page > 0 ? config.Custom.colorbtnback : "0.5 0.5 0.5 0.1", Lang("Back", player.UserIDString), 15, "0.2 0.32", "0.29 0.34", page > 0 ? $"craftpanel.page {i} {page - 1}": "");
                UI.Button(ref container, elemq1, GetCraftItemsForPlayer(player).Skip(16 * (page + 1)).Count() > 0 ? config.Custom.colorbtnnext : "0.5 0.5 0.5 0.1"  , Lang("Next", player.UserIDString), 15, "0.295 0.32", "0.385 0.34", GetCraftItemsForPlayer(player).Skip(16 * (page + 1)).Count() > 0 ? $"craftpanel.page {i} {page + 1}": $"");
            }

            var eID = Convert.ToInt32(i);
            var entry = config.Craft[eID];
            string name = entry.name;
            string complete = entry.namecomplete;
            string img = entry.img;
            string description = entry.description;
            var discount = entry.VIPdiscount;
            string workbench = entry.workbench.ToString();
            var perms = entry.permission;
            var permNoCost = entry.permissionNoCost;
            var permVIP = entry.permissionVIP;

            //Panel Central
            container.Add(new CuiElement {
                Name = elemq7,
                Parent = elemq1,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 0.2f,
                        Color = config.Custom.colorbgpanel,
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 -250",
                        OffsetMax = "200 250"
                    }
                }
            });

            UI.Image(ref container, elemq1, GetImageLibrary(img), "0.4 0.561", "0.5995 0.6562");

            /*container.Add(new CuiElement {
                Parent = elemq1,
                Components = {
                    new CuiRawImageComponent{
                        FadeIn = 0.2f,
                        Png = GetImageLibrary(img)
                    },
                    new CuiRectTransformComponent{
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 100",
                        OffsetMax = "200 250"
                    }
                }
            });*/

            if(workbench != "0"){
                UI.Panel(ref container, elemq1, "0.31 0.50 1.00 0.60", "0.40 0.643", "0.463 0.656");
                UI.Label(ref container, elemq1, Lang("Workbench", player.UserIDString, workbench), 13, "0.40 0.643", "0.463 0.656", "", TextAnchor.MiddleCenter);
            }

            container.Add(new CuiElement {
                Parent = elemq1,
                Components = {
                    new CuiTextComponent {
                        Text = complete,
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-190 45",
                        OffsetMax = "190 90"
                    }
                }
            });

            container.Add(new CuiElement {
                Parent = elemq1,
                Components = {
                    new CuiTextComponent {
                        Text = description,
                        Color = "1.00 1.00 1.00 0.82",
                        FontSize = 13,
                        Font = "robotocondensed-regular.ttf",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-190 -80",
                        OffsetMax = "190 100"
                    }
                }
            });

            if (HasPermission(player.UserIDString, permVIP)){
                UI.Label(ref container, elemq1, Lang("VIP", player.UserIDString, discount.ToString()), 17, "0.4 0.37", "0.6 0.43", config.Custom.colortxtvip, TextAnchor.MiddleCenter);
            }
            
            //Panel de Ingredientes
            var sizeX = 60;
            var sizeY = 60;
            var startX = 250;
            var startY = 250;
            var x = startX;
            var y = startY;

            for (var o = 0; o < config.Craft[eID].items.Count;o++){
                var entry2 = config.Craft[eID].items[o];
                if (o != 0 && o % 4 == 0){
                    x = startX;
                    y -= sizeY + 25;
                }

                container.Add(new CuiElement {
                    Name = elemqitemvip, Parent = elemq1, Components = {
                        new CuiRawImageComponent{
                            Png = GetImageLibrary(entry2.item, entry2.skinID)
                        },
                        new CuiRectTransformComponent{
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{x} {y-sizeY}",
                            OffsetMax = $"{x + sizeX} {y}"
                        }
                    }
                });

                var amount = entry2.amount;

                UI.Label(ref container, elemqitemvip, $"x{amount}", 13, "0 -0.3", "1 0.2", "", TextAnchor.MiddleCenter);

                if (permission.UserHasPermission(player.UserIDString, permVIP) == true){
                    var promo = amount - ((amount*discount)/100);
                    UI.Label(ref container, elemqitemvip, $"({promo})", 10, "0 -0.4", "1 -0.2", config.Custom.coloramountvip, TextAnchor.MiddleCenter);
                }
                x += sizeX + 12;
            }

            if (HasPermission(player.UserIDString, perms)){
                UI.Button(ref container, elemq7, config.Custom.colorbtncraft, Lang("Craft", player.UserIDString), 15, "0 0", "0.998 0.08", $"craftpanel.craft {i}");
            } else {
                UI.Button(ref container, elemq7, config.Custom.colorbtnclose, Lang("WithoutPermission", player.UserIDString), 15, "0 0", "0.998 0.08", $"craftpanel");
            }

            CuiHelper.AddUi(player, container);
        }

        //---Panel Error
        private CuiElementContainer GetPanel2(BasePlayer player, string textshow){
            var container = new CuiElementContainer();
            container.Add(new CuiElement {
                Name = elemq2,
                Parent = "Overlay",
                Components = {
                    new CuiImageComponent {
                        Color = "1.0 0 0.0 0.4",
                        Material = "assets/content/ui/namefontmaterial.mat"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 260",
                        OffsetMax = "200 300"
                    }
                }
            });

            container.Add(new CuiElement {
                Parent = elemq2,
                Components = {
                    new CuiTextComponent {
                        Text = textshow,
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-200 -10",
                        OffsetMax = "200 10"
                    }
                }
            });

            container.Add(new CuiButton{
				Button =
				{
					Command = "craftpanel.close2",
					Color = "0 0 0 0.2",
                    Material = "assets/content/ui/namefontmaterial.mat"
				},
				RectTransform =
				{
					AnchorMin = "0.88 0.20",
					AnchorMax = "0.97 0.80"
				},
				Text =
				{
					Text = "x",
					FontSize = 14,
					Align = TextAnchor.MiddleCenter
				}
			}, elemq2);

            return container;
        }
        #endregion

        #region CUI Helper
        public class UI {
            static public void Panel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false, string material = "assets/content/ui/namefontmaterial.mat"){
                container.Add(new CuiPanel{
                    Image = { Color = color, Material = material},
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax},
                    CursorEnabled = cursor
                },
                panel);
            }

            static public void Label(ref CuiElementContainer container, string panel, string text, int size, string aMin, string aMax, string color = "1 1 1 0.6", TextAnchor align = TextAnchor.MiddleCenter, bool font = false){
                container.Add(new CuiLabel{
                    Text = { FontSize = size, Font = font? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf", Color = color, Align = align, Text = text},
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax}
                },
                panel);
            }

            static public void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter){
                container.Add(new CuiButton{
                    Button = { Color = color, Material = "assets/content/ui/namefontmaterial.mat", Command = command, FadeIn = 0f},
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax},
                    Text = { Text = text, FontSize = size, Align = align}
                },
                panel);
            }

            static public void Input(ref CuiElementContainer container, string panel, string color, string text, int size, string command, string aMin, string aMax){
                container.Add(new CuiElement{
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components = {
                        new CuiInputFieldComponent {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 50,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax}
                    }
                });
            }

            static public void Image(ref CuiElementContainer container, string panel, string png, string aMin, string aMax){
                container.Add(new CuiElement{
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components = {
                        new CuiRawImageComponent {Png = png},
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax}
                    }
                });
            }
        }
        #endregion

        #region Config
        private static ConfigData config;

        private class ConfigData {
            [JsonProperty(PropertyName = "Custom")]
            public CraftCustom Custom;

            [JsonProperty(PropertyName = "Craft")]
            public List<CraftInfo> Craft;
        }
        
        private class CraftInfo {
            [JsonProperty(PropertyName = "Short Name")]
            public string name;
            
            [JsonProperty(PropertyName = "Full Name")]
            public string namecomplete;

            [JsonProperty(PropertyName = "Img Full")]
            public string img;

            [JsonProperty(PropertyName = "Img Icon")]
            public string imgicon;

            [JsonProperty(PropertyName = "Description")]
            public string description;

            [JsonProperty(PropertyName = "Craft Result")]
            public CraftResult result;

            [JsonProperty(PropertyName = "Permission Use")]
            public string permission;

            [JsonProperty(PropertyName = "Permission VIP")]
            public string permissionVIP;

            [JsonProperty(PropertyName = "Permission No Cost")]
            public string permissionNoCost;

            [JsonProperty(PropertyName = "VIP discount: 10 = 10%")]
            public int VIPdiscount;

            [JsonProperty(PropertyName = "Require Workbench? 0 = NOT, 1 = Level 1,...")]
            public int workbench;

            [JsonProperty(PropertyName = "Items")]
            public List<CraftInfoItem> items;
        }

        private class CraftInfoItem {
            [JsonProperty(PropertyName = "Item")]
            public string item;

            [JsonProperty(PropertyName = "Amount")]
            public int amount;

            [JsonProperty(PropertyName = "Skin ID")]
            public ulong skinID;
        }

        private class CraftResult {
            [JsonProperty(PropertyName = "Command (keep empty to create item)")]
            public string command;
            
            [JsonProperty(PropertyName = "Shortname")]
            public string shortname;
            
            [JsonProperty(PropertyName = "Amount")]
            public int amount;
            
            [JsonProperty(PropertyName = "Skin ID")]
            public ulong skinID;
        }

        private class CraftCustom {
            [JsonProperty(PropertyName = "Title")]
            public string title;

            [JsonProperty(PropertyName = "Show even if you don't have permissions (you won't be able to craft)")]
            public bool showWithoutPerm;

            [JsonProperty(PropertyName = "Sound Effects")]
            public bool soundeffects;

            [JsonProperty(PropertyName = "Sound Prefab 1")]
            public string sound1;

            [JsonProperty(PropertyName = "Sound Prefab 2")]
            public string sound2;

            [JsonProperty(PropertyName = "Sound Prefab 3")]
            public string sound3;

            [JsonProperty(PropertyName = "Permission Use /craft")]
            public string permissionuse;

            [JsonProperty(PropertyName = "Color Title")]
            public string colortitle;

            [JsonProperty(PropertyName = "Color Button List")]
            public string colorbtnlist;
            
            [JsonProperty(PropertyName = "Color Button Close")]
            public string colorbtnclose;

            [JsonProperty(PropertyName = "Color Button Craft")]
            public string colorbtncraft;

            [JsonProperty(PropertyName = "Color Button Back")]
            public string colorbtnback;

            [JsonProperty(PropertyName = "Color Button Next")]
            public string colorbtnnext;
            
            [JsonProperty(PropertyName = "Color Background Panel")]
            public string colorbgpanel;

            [JsonProperty(PropertyName = "Color Text VIP")]
            public string colortxtvip;

            [JsonProperty(PropertyName = "Color Text Amount")]
            public string coloramount;

            [JsonProperty(PropertyName = "Color Text Amount VIP")]
            public string coloramountvip;

            [JsonProperty(PropertyName = "Img Block Item")]
            public string imgblock;
        }

        private ConfigData GetDefaultConfig() {
            return new ConfigData {
                Custom = new CraftCustom {
                    title = "Craft Panel",
                    soundeffects = true,
                    sound1 = "assets/prefabs/tools/keycard/effects/swipe.prefab",
                    sound2 = "assets/bundled/prefabs/fx/build/promote_toptier.prefab",
                    sound3 = "assets/prefabs/misc/xmas/presents/effects/wrap.prefab",
                    permissionuse  = "craftpanel.use",
                    colortitle = "1.00 1.00 1.00 0.43",
                    colorbtnlist = "1.00 1.00 1.00 0.10",
                    colorbtnclose = "0.52 0.00 0.00 1.00",
                    colorbtncraft = "0.00 0.50 0.40 1.00",
                    colorbtnback = "0.30 0.30 0.80 0.90",
                    colorbtnnext = "0.30 0.30 0.80 0.90",
                    colorbgpanel = "0.2 0.2 0.2 0.95",
                    colortxtvip = "0.90 0.80 0.04 1.00",
                    coloramount = "1.00 1.00 1.00 1.00",
                    coloramountvip = "0.90 0.80 0.04 1.00",
                    imgblock = "https://i.imgur.com/KzE9uZR.png"
                },
                Craft = new List<CraftInfo> {
                    new CraftInfo {
                        name = "Recycler",
                        namecomplete = "Recycler at Home",
                        img = "https://i.imgur.com/wdVWxDB.jpg",
                        imgicon = "https://i.imgur.com/vK1dRNM.png",
                        description  = "Make your own recycler to install it on your base. You can collect it by hitting with the hammer.",
                        result = new CraftResult {
                            amount = 0,
                            command = "recycler.give {steamID}",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.recycler.use",
                        permissionVIP = "craftpanel.recycler.vip",
                        permissionNoCost = "craftpanel.recycler.nocost",
                        VIPdiscount = 10,
                        workbench = 2,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 50000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 300,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 50,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "fuse",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "techparts",
                                amount = 15,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Mini Helicopter",
                        namecomplete = "Mini Helicopter",
                        img = "https://i.imgur.com/vnZND1c.jpg",
                        imgicon = "https://i.imgur.com/nzCwmo8.png",
                        description  = "Because having your own Helicopters whenever you want is cool too.\nCraft your Helis, put them away and take them out when you need them.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} minicopter",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.miniheli.use",
                        permissionVIP = "craftpanel.miniheli.vip",
                        permissionNoCost = "craftpanel.miniheli.nocost",
                        VIPdiscount = 25,
                        workbench = 0,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 5000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "propanetank",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "roadsigns",
                                amount = 5,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 10,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Scrap Helicopter",
                        namecomplete = "Scrap Helicopter",
                        img = "https://i.imgur.com/L1XrkSv.jpg",
                        imgicon = "https://i.imgur.com/qW9mNti.png",
                        description  = "Scrap Helicopter is the father of the Minicopter, it has two seats in the front and plenty of room in the rear for gamers to huddle.\n\nIt has 2500 horsepower and consumes more low-grade fuel. I wouldn't recommend putting yourself under one ...",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} scrap",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.scrapheli.use",
                        permissionVIP = "craftpanel.scrapheli.vip",
                        permissionNoCost = "craftpanel.scrapheli.nocost",
                        VIPdiscount = 15,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 10000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 50,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 15,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "propanetank",
                                amount = 15,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "roadsigns",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 20,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "fuse",
                                amount = 1,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Sentry Turret",
                        namecomplete = "Sentry Turret",
                        img = "https://i.imgur.com/yMmmvuO.jpg",
                        imgicon = "https://i.imgur.com/en1TJ0X.png",
                        description  = "I know, you've always dreamed of having a turret like the one in the Outpost at your base. Now you can protect your rooftop with this electronic marvel that can hold up to 2 C4 or 6 Missiles.",
                        result = new CraftResult {
                            amount = 0,
                            command = "sentryturrets.give {steamID}",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.sentryturret.use",
                        permissionVIP = "craftpanel.sentryturret.vip",
                        permissionNoCost = "craftpanel.sentryturret.nocost",
                        VIPdiscount = 10,
                        workbench = 3,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 30000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 20,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "techparts",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "cctv.camera",
                                amount = 5,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "targeting.computer",
                                amount = 5,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "weapon.mod.lasersight",
                                amount = 1,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "lmg.m249",
                                amount = 1,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Boat",
                        namecomplete = "Boat",
                        img = "https://i.imgur.com/47tq7JF.jpg",
                        imgicon = "https://i.imgur.com/kk5zMmT.png",
                        description  = "A small portable wooden boat so that you can set sail the seas in search of the oil company and then collect it.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} boat",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.boat.use",
                        permissionVIP = "craftpanel.boat.vip",
                        permissionNoCost = "craftpanel.boat.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 125,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Lancha RHIB",
                        namecomplete = "Lancha RHIB",
                        img = "https://i.imgur.com/223ufKc.jpg",
                        imgicon = "https://i.imgur.com/NTeqZiM.png",
                        description  = "Because going to the Ship or the Oil is sometimes a bit screwed up.\nWhy don't you leave the house with your own boat?",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} rhib",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.rhib.use",
                        permissionVIP = "craftpanel.rhib.vip",
                        permissionNoCost = "craftpanel.rhib.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 1500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 100,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 5,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Submarine Solo",
                        namecomplete = "Submarine Solo",
                        img = "https://i.imgur.com/F5UFLKB.jpg",
                        imgicon = "https://i.imgur.com/UO9tKWD.png",
                        description  = "A portable 1-seater submarine for you to place on your favorite beach shore. You can pick it up later by hitting it with a wooden hammer.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} submarinesolo",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.submarinesolo.use",
                        permissionVIP = "craftpanel.submarinesolo.vip",
                        permissionNoCost = "craftpanel.submarinesolo.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 2500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 300,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Submarine Duo",
                        namecomplete = "Submarine Duo",
                        img = "https://i.imgur.com/8nuFwuy.jpg",
                        imgicon = "https://i.imgur.com/cJGnI2L.png",
                        description  = "A portable 2-seater submarine for you to place on your favorite beach shore. You can pick it up later by hitting it with a wooden hammer.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} submarineduo",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.submarineduo.use",
                        permissionVIP = "craftpanel.submarineduo.vip",
                        permissionNoCost = "craftpanel.submarineduo.nocost",
                        VIPdiscount = 5,
                        workbench = 2,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 3500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 400,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 5,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Quarry",
                        namecomplete = "Mining Quarry",
                        img = "https://i.imgur.com/CaHgjk4.jpg",
                        imgicon = "https://i.imgur.com/n6EJtNj.png",
                        description  = "We know that the giant excavator is the key to collecting resources, but with these quarries if you are lucky with the prospecting charges you can collect resources near your base.",
                        result = new CraftResult {
                            amount = 1,
                            command = "",
                            shortname = "mining.quarry",
                            skinID = 0
                        },
                        permission  = "craftpanel.quarry.use",
                        permissionVIP = "craftpanel.quarry.vip",
                        permissionNoCost = "craftpanel.quarry.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 3000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 25,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "wood",
                                amount = 5000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 10,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Super Card",
                        namecomplete = "Super Card",
                        img = "https://i.imgur.com/EGkxhc0.jpg",
                        imgicon = "https://i.imgur.com/9QzPQsR.png",
                        description  = "The Super card makes it possible to replace all cards (green, blue, red) with one universal card and open absolutely any door (fuse is still needed).",
                        result = new CraftResult {
                            amount = 0,
                            command = "supercard.give {steamID}",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.supercard.use",
                        permissionVIP = "craftpanel.supercard.vip",
                        permissionNoCost = "craftpanel.supercard.nocost",
                        VIPdiscount = 5,
                        workbench = 2,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 350,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "metal.refined",
                                amount = 10,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Car 2",
                        namecomplete = "2 Module Car",
                        img = "https://i.imgur.com/pWfG1jf.jpg",
                        imgicon = "https://i.imgur.com/KuZIQAR.png",
                        description  = "Make your own random car with 2 modules.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} car2",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.car2.use",
                        permissionVIP = "craftpanel.car2.vip",
                        permissionNoCost = "craftpanel.car2.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 1500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 100,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 10,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 2,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Car 3",
                        namecomplete = "3 Module Car",
                        img = "https://i.imgur.com/Oynpu1r.jpg",
                        imgicon = "https://i.imgur.com/rSQnsSc.png",
                        description  = "Make your own random car with 3 modules.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} car3",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.car3.use",
                        permissionVIP = "craftpanel.car3.vip",
                        permissionNoCost = "craftpanel.car3.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 3000,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 200,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 15,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 3,
                                skinID = 0
                            }
                        }
                    },
                    new CraftInfo {
                        name = "Car 4",
                        namecomplete = "4 Module Car",
                        img = "https://i.imgur.com/7PaVLkk.jpg",
                        imgicon = "https://i.imgur.com/hyW79Pc.png",
                        description  = "Make your own random car with 4 modules.",
                        result = new CraftResult {
                            amount = 0,
                            command = "portablevehicles.give {steamID} car4",
                            shortname = "",
                            skinID = 0
                        },
                        permission  = "craftpanel.car4.use",
                        permissionVIP = "craftpanel.car4.vip",
                        permissionNoCost = "craftpanel.car4.nocost",
                        VIPdiscount = 5,
                        workbench = 1,
                        items = new List<CraftInfoItem> {
                            new CraftInfoItem {
                                item = "metal.fragments",
                                amount = 3500,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "scrap",
                                amount = 250,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "sheetmetal",
                                amount = 20,
                                skinID = 0
                            },
                            new CraftInfoItem {
                                item = "gears",
                                amount = 4,
                                skinID = 0
                            }
                        }
                    },
                }
            };
        }

        protected override void LoadConfig(){
            base.LoadConfig();
            try {
                config = Config.ReadObject<ConfigData>();
                if (config == null){
                    LoadDefaultConfig();
                }
                craftList = config.Craft;
            } catch {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig(){
            config = GetDefaultConfig();
        }

        protected override void SaveConfig(){
            Config.WriteObject(config);
        }
        #endregion

        #region Resources
        private class IngredientData{
            public int amount;
            public List<Item> items = new List<Item>();
            public ulong skin;
        }

        private bool CanCraftItem(BasePlayer player, int craftitem) {
            var craft = config.Craft[craftitem];
            int discount = craft.VIPdiscount;

            if(craft.workbench <= player.currentCraftLevel){
                //Puts("Nivel necesario y lo tiene." + player.currentCraftLevel);
            } else {
                OpenPanelError(player, Lang("NoWorkbench", player.UserIDString, craft.workbench));
                return false;
            }

            if (permission.UserHasPermission(player.UserIDString, craft.permissionNoCost) == true){
                return true;
            }

            var costes = craft.items;
            var existing = GetExistingIngredients(player.inventory.AllItems());
            if (HasIngredients(existing, costes, permission.UserHasPermission(player.UserIDString, craft.permissionVIP), discount)){
                TakeIngredients(existing, costes, permission.UserHasPermission(player.UserIDString, craft.permissionVIP), discount);
                return true;
            } else {
                OpenPanelError(player, Lang("NoResources", player.UserIDString));
                return false;
            }
        }

        private Dictionary<string, IngredientData> GetExistingIngredients(Item[] items){
            var existing = new Dictionary<string, IngredientData>();
            foreach (var item in items){
                var name = item.info.shortname;
                existing.TryAdd(name, new IngredientData());
                existing[name].amount += item.amount;
                existing[name].skin = item.skin;
                existing[name].items.Add(item);
                //Puts("Inventario: " + name + " Name: " + item.skin.ToString());
            }
            return existing;
        }

        private bool HasIngredients(Dictionary<string, IngredientData> existing, List<CraftInfoItem> cost, bool vip, int discount){
            foreach (var ingredient in cost){
                var NameCost = ingredient.item;
                //Puts("Nombre de Costo: " + NameCost + " Skin que te pido: " + ingredient.skinID.ToString());
                if (!existing.ContainsKey(NameCost)){
                    return false;
                }
                if (ingredient.skinID != 0){
                    //Puts("Se pide una Skin especifica");
                    if (existing[NameCost].skin != ingredient.skinID){
                        //Puts("Las skins no coinciden");
                        return false;
                    }
                }
                var TotalAmount = 0;
                if(vip){
                    TotalAmount = ingredient.amount - ((ingredient.amount*discount)/100);
                } else {
                    TotalAmount = ingredient.amount;
                }
                if (existing[NameCost].amount < TotalAmount){
                    return false;
                }
            }
            return true;
        }

        private void TakeIngredients(Dictionary<string, IngredientData> existing, List<CraftInfoItem> cost, bool vip, int discount){
            foreach (var ingredient in cost){
                var NameCost = ingredient.item;
                var TotalAmount = 0;
                if(vip){
                    TotalAmount = ingredient.amount - ((ingredient.amount*discount)/100);
                } else {
                    TotalAmount = ingredient.amount;
                }
                
                var items = existing[NameCost].items;
                foreach (var item in items){
                    if (TotalAmount == 0){
                        break;
                    }
                    if (ingredient.skinID != 0){
                        //Puts("Se quita una Skin especifica");
                        if (existing[NameCost].skin != item.skin){
                            //Puts("Las skins para quitar no coinciden");
                            continue;
                        }
                    }
                    if (item.amount > TotalAmount){
                        item.amount -= TotalAmount;
                        break;
                    } else {
                        TotalAmount -= item.amount;
                        item.GetHeldEntity()?.Kill();
                        item.DoRemove();
                    }
                }
            }
        }

        #endregion

        #region Language
        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                ["Close"] = "CLOSE",
                ["Craft"] = "CRAFT ITEM",
                ["VIP"] = "For being VIP you have a Discount of {0}%",
                ["Workbench"] = "Workbench Level {0}",
                ["NoWorkbench"] = "You need the Level {0} Workbench",
                ["NoResources"] = "You do not have the necessary materials.",
                ["NotAllowed"] = "You do not have permission to use this command.",
                ["MessageResponse"] = "CraftPanel is waiting for ImageLibrary downloads to finish please wait.",
                ["WithoutPermission"] = "No permissions.",
                ["Back"] = "BACK",
                ["Next"] = "NEXT",
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void PrintToChat(BasePlayer player, string message) => Player.Message(player, "<color=#f74d31>CraftPanel:</color> " + message);
        #endregion
    }
}