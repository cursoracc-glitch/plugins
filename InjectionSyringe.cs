using UnityEngine;
using Random = UnityEngine.Random;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("InjectionSyringe", "Empty", "2.0.0")]
    public class InjectionSyringe : RustPlugin
    {
        #region НАСТРОЙКИ
        string pic1 = "https://i.imgur.com/vhclJVT.png";//ссылка на картинку шприца
        string pic2 = "https://imgur.com/BbycTX3.png";//ссылка на картинку колбы 1
        string pic3 = "https://imgur.com/e6L6X83.png";//ссылка на картинку колбы 2
        string pic4 = "https://imgur.com/undefined.png";//ссылка на картинку колбы 3
        string pic5 = "https://pic.moscow.ovh/images/2019/10/17/17e60544bac5a6e229887d9a6ac3fedc.png";//ссылка на картинку галочки
        string pic6 = "https://pic.moscow.ovh/images/2019/10/17/b3a73a1d6022495a8206fedf646157eb.png";//ссылка на картинку крестика
        ulong skinid1 = 1720697246;//скин айди шприца
        ulong skinid2 = 1767124720;//скин айди колбы 1
        ulong skinid3 = 1767132350;//скин айди колбы 2
        ulong skinid4 = 1767208385;//скин айди колбы 3

        #endregion
        [PluginReference] private Plugin ImageLibrary;
        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", pic1, "shprits");
            ImageLibrary.Call("AddImage", pic2, "kolba1");
            ImageLibrary.Call("AddImage", pic3, "kolba2");
            ImageLibrary.Call("AddImage", pic4, "kolba3");
            ImageLibrary.Call("AddImage", pic5, "krest");
            ImageLibrary.Call("AddImage", pic6, "galka");
            PrintWarning("★★★★★★★ Autor - vk.com/zaharkotov ★★★★★★★");
        }

        #region Core

        object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (tool.skinID == 1720697246)
            {
                SendReply(player, "<color=red>Вы использовали инъекционный шприц</color>");
                player.health = 100;
            }
            return null;
        }
        void GiveMedical(BasePlayer player)
        {
            Item medical = ItemManager.CreateByItemID(1079279582, 1, skinid1);
            medical.name = "Инъекционный шприц";
            player.GiveItem(medical, BaseEntity.GiveItemReason.PickedUp);
        }

        [ConsoleCommand("syringe.add")]
        private void CmdHandler(ConsoleSystem.Arg args)
        {

            BasePlayer player = args.Player();
            var check1 = player.inventory.GetAmount(ItemManager.FindItemDefinition("xmas.decoration.gingerbreadmen").itemid);//красный
            var check2 = player.inventory.GetAmount(ItemManager.FindItemDefinition("xmas.decoration.pinecone").itemid);//зеленый
            var check3 = player.inventory.GetAmount(ItemManager.FindItemDefinition("xmas.decoration.baubels").itemid);//желтый

            if (check1 >= 1 && check2 >= 1 && check3 >= 1)
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition("xmas.decoration.gingerbreadmen").itemid, 1);
                player.inventory.Take(null, ItemManager.FindItemDefinition("xmas.decoration.pinecone").itemid, 1);
                player.inventory.Take(null, ItemManager.FindItemDefinition("xmas.decoration.baubels").itemid, 1);

                GiveMedical(player);
            }
            else
            {
                SendReply(player, "Вам не хватает колб");
            }
        }

        
        #endregion

        #region ChatCommand
        [ChatCommand("medical")]
        void DrawUI(BasePlayer player)
        {
            var check1 = player.inventory.GetAmount(ItemManager.FindItemDefinition("xmas.decoration.gingerbreadmen").itemid);//красный
            var check2 = player.inventory.GetAmount(ItemManager.FindItemDefinition("xmas.decoration.pinecone").itemid);//зеленый
            var check3 = player.inventory.GetAmount(ItemManager.FindItemDefinition("xmas.decoration.baubels").itemid);//желтый
            string Layer = "page";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.4", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.20f },
                FadeOut = 0.20f
            }, "Overlay", Layer);// фон    
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "krest"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.9781 0.9611", AnchorMax = "0.9947 0.9907", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });//картинка крест на закрытие
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "shprits"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.33 0.50", AnchorMax = "0.4862 0.776854", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });//картинка шприца
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.9781 0.9611", AnchorMax = "0.9947 0.9907", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 1 }
            }, Layer);//кнопка крест на закрытие
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.25 0.89", AnchorMax = "0.75 0.99", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "КРАФТ ИНЬЕКЦИОННОГО ШПРИЦА", Align = TextAnchor.MiddleCenter, FontSize = 40 }
            }, Layer);//надпись КРАФТ ИНЬЕКЦИОННОГО ШПРИЦА
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.39 0.0731", AnchorMax = "0.61 0.1509", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "1 0 0 1", Command = "syringe.add", Close = Layer },
                Text = { Text = "СКРАФТИТЬ", Align = TextAnchor.MiddleCenter, FontSize = 40 }
            }, Layer);//кнопка скрафтить
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.8046", AnchorMax = "0.7 0.8787", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "ДЛЯ КРАФТА ИНЬЕКЦИОННОГО ШПРИЦА ТРЕБУЕТСЯ:", Align = TextAnchor.MiddleCenter, FontSize = 20 }
            }, Layer);//надпись ДЛЯ КРАФТА ИНЬЕКЦИОННОГО ШПРИЦА ТРЕБУЕТСЯ:
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.25 0.3509", AnchorMax = "0.75 0.4824", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "Чтобы сделать инъекционный шприц, нужно найти 3 колбы (красную, зеленую и желтую).\nНайти их можно в ящиках с компонентам.\nПолезное свойство шприца: полное восстановление здоровья", Align = TextAnchor.MiddleCenter, FontSize = 15 }
            }, Layer);//текст инфо
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.39 0.2", AnchorMax = "0.61 0.3314808", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.4", Close = Layer },
                Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 1 }
            }, Layer);//фон под компоненты
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "kolba1"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.3947 0.2055", AnchorMax = "0.4624 0.3259", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });//картинка Колбы 1
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "kolba2"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.4661 0.2055", AnchorMax = "0.5338 0.3259", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });//картинка Колбы 2
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "kolba3"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5374 0.2055", AnchorMax = "0.6052 0.3259", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });//картинка Колбы 3
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5269 0.6668", AnchorMax = "0.8132 0.6968", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "1X КРАСНАЯ КОЛБА", Align = TextAnchor.MiddleLeft, FontSize = 19 }
            }, Layer);//Компонент 1
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5269 0.6268", AnchorMax = "0.8132 0.6568", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "1X ЗЕЛЕНАЯ КОЛБА", Align = TextAnchor.MiddleLeft, FontSize = 19 }
            }, Layer);//Компонент 2
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5269 0.5868", AnchorMax = "0.8132 0.6168", OffsetMin = "0 0", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "1X ЖЕЛТАЯ КОЛБА", Align = TextAnchor.MiddleLeft, FontSize = 19 }
            }, Layer);//Компонент 3
            #region Проверка на наличие в инвентаре 1
            if (check1 >= 1)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "galka"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5069 0.6668", AnchorMax = "0.5235 0.6968", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
                });
            }
            else //0.0166
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "krest"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5069 0.6668", AnchorMax = "0.5235 0.6968", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
                });
            }
            #endregion
            #region Проверка на наличие в инвентаре 2
            if (check2 >= 1)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "galka"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5069 0.6268", AnchorMax = "0.5235 0.6568", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "krest"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5069 0.6268", AnchorMax = "0.5235 0.6568", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
                });
            }
            #endregion
            #region Проверка на наличие в инвентаре 3
            if (check3 >= 1)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "galka"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5069 0.5868", AnchorMax = "0.5235 0.6168", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                {
                    new CuiRawImageComponent() {  Png = (string) ImageLibrary.Call("GetImage", "krest"), Color = "1 1 1 1", },
                    new CuiRectTransformComponent(){  AnchorMin = "0.5069 0.5868", AnchorMax = "0.5235 0.6168", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
                });
            }
            #endregion
            CuiHelper.AddUi(player, container);
        }
        [ChatCommand("medicaladmin")]
        void AdminGive(BasePlayer player)
        {
            if (!player.IsAdmin)
            {
                return;
            }
            Item colb1 = ItemManager.CreateByItemID(-1667224349, 1, skinid2);
            colb1.name = "Красная колба";
            player.GiveItem(colb1, BaseEntity.GiveItemReason.PickedUp);
            Item colb2 = ItemManager.CreateByItemID(1686524871, 1, skinid3);
            colb2.name = "Зеленая колба";
            player.GiveItem(colb2, BaseEntity.GiveItemReason.PickedUp);
            Item colb3 = ItemManager.CreateByItemID(-129230242, 1, skinid4);
            colb3.name = "Желтая колба";
            player.GiveItem(colb3, BaseEntity.GiveItemReason.PickedUp);

        }
        #endregion
    }
        

}