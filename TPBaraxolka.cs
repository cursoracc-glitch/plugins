using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Network;

namespace Oxide.Plugins
{
    [Info("TPBaraxolka", "Sempai#3239", "1.0.3")]
    internal class TPBaraxolka : RustPlugin
    {
        public List<BaseEntity> BaseEntityList = new List<BaseEntity>();
        [PluginReference] Plugin ImageLibrary, TPEconomic, TPMenuSystem;

        void Loaded()
        {
            ins = this;
            LoadSounds();

        }
        void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n " +" Author - Sempai#3239\n " +" VK - https://vk.com/rustnastroika\n " +" Forum - https://topplugin.ru\n " +" Discord - https://discord.gg/5DPTsRmd3G\n" +"-----------------------------");
            if (ImageLibrary)
            {
                var images = config.bottleSetting.CustomItemsShop.Where(p => !string.IsNullOrEmpty(p.ImageURL));
                foreach (var check in images)
                    ImageLibrary.Call("AddImage", check.ImageURL, check.ImageURL);


                foreach (var check in config.bottleSetting.CustomItemsShop.Where(p => string.IsNullOrEmpty(p.ImageURL)))
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check.defaultItem.ShortName}.png", check.defaultItem.ShortName + 128);
                ImageLibrary.Call("AddImage", "https://media.discordapp.net/attachments/845901958820790285/1193980037347684403/2.png", "MainUI");
                ImageLibrary.Call("AddImage", "https://media.discordapp.net/attachments/845901958820790285/1193978187386990752/2.png", "DropUI");
                ImageLibrary.Call("AddImage", "https://media.discordapp.net/attachments/845901958820790285/1193978457428869130/2.png", "BuyerUI");
            }
            ServerMgr.Instance.StartCoroutine(DownloadImage("https://i.imgur.com/7fIELSt.png"));
            //FindPositions();

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        public string External = "UI_TPBaraxolka_External";
        public string Internal = "UI_TPBaraxolka_Internal";


        public string Layer95="MainUI_TPBaraxolka";
        private void DrawNPCUI(BasePlayer player, string img = "MainUI", int a=0)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, External);
            CuiHelper.DestroyUi(player, Internal);
            CuiHelper.DestroyUi(player, Layer95);


            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
            }, "Overlay", Layer95);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.233 0.2", AnchorMax = $"0.80 0.8", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, Layer95, ".Mains_Tr");

            container.Add(new CuiElement
            {
                Name = External,
                Parent = ".Mains_Tr",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", img), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                }
            });

            CuiHelper.AddUi(player, container);
            InitializeLayers(player);
            if(img == "MainUI" && a==0)
                player.SendConsoleCommand("UI_TPBaraxolka info");
        }


        void CreateInfoPlayer(BasePlayer player, bool opened)
        {
            CuiHelper.DestroyUi(player, External + ".Mood1" + ".panel");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = External + ".Mood1" + ".panel",
                Parent = "Overlay",
                Components =
                        {
                            new CuiImageComponent { FadeIn = 1f, Color = opened?  "0.235 0.227 0.180 0.6" :"0.235 0.227 0.180 0" , Sprite = "assets/content/ui/ui.background.transparent.linear.psd"},
                            new CuiRectTransformComponent {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = opened ?  "-240 -81" : "-240 -141", OffsetMax = opened ? "-60 -30" : "-60 -90"}
                        }
            });



            container.Add(new CuiElement
            {
                Parent = External + ".Mood1" + ".panel",
                Name = External + ".Mood1",
                Components =
                        {
                            new CuiRawImageComponent {FadeIn = 1f,Png = (string)ImageLibrary.Call("GetImage", "https://i.imgur.com/ghyCq0Q.png"), Color = "0.929 0.882 0.847 1"},
                            new CuiRectTransformComponent {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-30 -60", OffsetMax = "50 20"}
                        }
            });


            if (opened)
            {
                container.Add(new CuiElement
                {
                    Parent = External + ".Mood1" + ".panel",
                    Components =
                        {
                            new CuiTextComponent { FadeIn = 1f,Color = "0.929 0.882 0.847 0.5", Text = $"<color=#EDE1D8>ВАМ ПОСЫЛКА</color>", Align = TextAnchor.UpperLeft /*,Font="robotocondensed-regular.ttf"*/},
                            new CuiRectTransformComponent {AnchorMin = "0.03 0", AnchorMax = "1 0.9"}
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = External + ".Mood1" + ".panel",
                    Components =
                        {
                            new CuiTextComponent {FadeIn = 1f, Color = "0.929 0.882 0.847 0.4", Text = $"НАЖМИТЕ ЧТОБЫ ЗАБРАТЬ", Align = TextAnchor.MiddleLeft ,Font="robotocondensed-regular.ttf", FontSize = 12},
                            new CuiRectTransformComponent {AnchorMin = "0.03 0", AnchorMax = "1 0.8"}
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = External + ".Mood1" + ".panel",
                    Components =
                        {
                            new CuiButtonComponent { Color = "0.929 0.882 0.847 0", Command = "UI_TPBaraxolka givepackage"},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                        }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = External + ".Mood1",
                    Components =
                        {
                            new CuiTextComponent { FadeIn = 1f,Color = "0.929 0.882 0.847 0.4   ", Text = $"ЗАБРАТЬ", Align = TextAnchor.UpperCenter ,Font="robotocondensed-regular.ttf"},
                            new CuiRectTransformComponent {AnchorMin = "0 -0.3", AnchorMax = "1 0.05"}
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = External + ".Mood1",
                    Components =
                        {
                            new CuiButtonComponent { Color = "0.929 0.882 0.847 0", Command = "UI_TPBaraxolka givepackage"},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                        }
                });



            }

            CuiHelper.AddUi(player, container);
        }

        string GetMoodTranslate(FatherComponent.Mood mood)
        {
            switch (mood)
            {
                case FatherComponent.Mood.Neutral:
                    return "НЕЙТРАЛЬНЫЙ";

                case FatherComponent.Mood.Kind:
                    return "ВЕСЁЛЫЙ";

                case FatherComponent.Mood.Evil:
                    return "ЗЛОЙ";
            }
            return "НЕЙТРАЛЬНЫЙ";
        }


        string GetMoodInfoRmation(FatherComponent.Mood mood)
        {
            switch (mood)
            {
                case FatherComponent.Mood.Neutral:
                    return "От его настроения зависит дальнейший диалог с ним. Предметы какие обменяет батя будут стандартные. Чем лучше настроение, тем больше предметов предложит батя";

                case FatherComponent.Mood.Kind:
                    return "От его настроения зависит дальнейший диалог с ним. Предметы какие обменяет батя будут увеличенные. Чем лучше настроение тем, больше предметов предложит батя";


                case FatherComponent.Mood.Evil:
                    return "Предметы какие обменяет батя будут уменьшены. Смотри осторожно, есть возможность получить леща от него. Батя не особо дружелюбный, бонусов не накинет.";
            }

            return "От его настроения зависит дальнейший диалог с ним. Предметы какие обменяет батя будут стандартные. Чем лучше настроение, тем больше предметов предложит батя";

        }



        string InitialLayer = "UI_TPBaraxolka_InitialLayer";
        private void InitializeLayers(BasePlayer player, string SelectMenu = "")
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, External, InitialLayer);

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0.7 0", AnchorMax = "0.9 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                Image = { Color = "0.24 0.45 0.90 0", Material = "" }
            }, InitialLayer, InitialLayer + ".C");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.804", AnchorMax = "0.57 0.83" },
                Button = { Close = "MainUI_TPBaraxolka", Color = "1 1 1 0" },
            }, InitialLayer + ".C");


            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0.25 0.2", AnchorMax = "0.754 0.64", OffsetMin = "0 0", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0" }
            }, InitialLayer, InitialLayer + ".R");
            CuiHelper.DestroyUi(player, InitialLayer);
            CuiHelper.AddUi(player, container);
            DrawMenuPoints(player);
        }

        void CreateInfoJson(BasePlayer player, int page)
        {
            CuiHelper.DestroyUi(player, InitialLayer + ".Expedition");
            DrawNPCUI(player, "DropUI");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "-0.08 0.3", AnchorMax = "0.48 0.95" },
                Image = { Color = "0.24 0.45 0.90 0" }
            }, InitialLayer + ".R", InitialLayer + ".Expedition");

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "345 -95", OffsetMax="355 3" },
                Image = { Color = "0.24 0.45 0.90 0" }
            }, InitialLayer + ".Expedition", InitialLayer + ".ItemsList");

            var pos = GetPositions(7, 7, 0.01f, 0.02f);
            int count = 0;
            var itemsList = config.expeditionSettings.ItemsAdded.OrderBy(p => p.Value.amount).Skip(page * 49).Take(49);

            foreach (var item in itemsList)
            {
                container.Add(new CuiPanel()
                {
                    RectTransform = { AnchorMin = pos[count].AnchorMin, AnchorMax = pos[count].AnchorMax },
                    Image = { Color = "0.77 0.74 0.71 0"}
                }, InitialLayer + ".ItemsList", InitialLayer + item.Key);

                container.Add(new CuiElement
                {
                    Parent = InitialLayer + item.Key,
                    Components =
                            {
                                new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", item.Key), Color = "1 1 1 0.6"},
                                new CuiRectTransformComponent { AnchorMin = "0.1 0.05", AnchorMax = "0.9 0.95" }
                            }
                });


                container.Add(new CuiElement
                {
                    Parent = InitialLayer + item.Key,
                    Components =
                        {
                            new CuiTextComponent { Color = "0.929 0.882 0.847 0.2", Text = $"<b>{item.Value.amount}</b>", Align = TextAnchor.MiddleCenter , FontSize = 23,Font="robotocondensed-regular.ttf"},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 5", OffsetMax="0 0" }
                        }
                });


                container.Add(new CuiElement
                {
                    Parent = InitialLayer + item.Key,
                    Components =
                        {
                            new CuiTextComponent { Color = "0.929 0.882 0.847 0.3", Text = $"MAX {item.Value.maxCount}", Align = TextAnchor.LowerCenter , FontSize = 8,Font="robotocondensed-regular.ttf"},
                            new CuiRectTransformComponent {AnchorMin = "0 0.05", AnchorMax = "1 1", OffsetMin = "0 -3", OffsetMax="0 0" }
                        }
                });


                count++;
            }

            container.Add(new CuiElement
            {
                Parent = InitialLayer + ".Expedition",
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 0.6", Text = $"ОСНОВНАЯ ИНФОРМАЦИЯ", Align = TextAnchor.UpperCenter, FontSize = 13},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax="0 20" }
                        }
            });

            container.Add(new CuiElement
            {
                Parent = InitialLayer + ".Expedition",
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 0.6", Text = $"СПИСОК ПРЕДМЕТОВ", Align = TextAnchor.UpperCenter, FontSize = 13},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "310 0", OffsetMax="400 20" }
                        }
            });

            container.Add(new CuiElement
            {
                Parent = InitialLayer + ".Expedition",
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 0.6", Text = $"Здесь вы можете увидеть список предметов которые вы можете продать на барахолке, и количество TopCoin которые за них получите. Предметы и цена может меняться время от времени.\n\n\nНа что влияет цена:\n        Хлам из первого верстака (на него цена ниже).\n        Хлам второго верстака (На него цена повыше).\n        Хлам третьего верстака (На него цена высокая).\n\nНо имейте ввиду что вы вы не сможете продать на барахолку много одинакого хлама\nНа каждый товар действует лимит в зависимости от редкости товара", Align = TextAnchor.UpperLeft, FontSize = 11},
                            new CuiRectTransformComponent {AnchorMin = "0.05 0", AnchorMax = "1 0.95"}
                        }
            });

            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0" , Command = page > 0 ? $"UI_TPBaraxolka expedition {page -1}" : ""
                },
                Text =
                {
                    Text = "◀", FontSize = 55, Align = TextAnchor.LowerRight, Color = page > 0 ? "0.929 0.882 0.847 0.7" : "0.929 0.882 0.847 0.1"
                },
                RectTransform =
                {
                    AnchorMin = $"0 0.2", AnchorMax = $"0.1 0.4"
                }
            }, InitialLayer + ".Expedition");

            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0", Command =  page < 2 ? $"UI_TPBaraxolka expedition {page +1}" : ""
                },
                Text =
                {
                    Text = "▶", FontSize = 55, Align = TextAnchor.LowerLeft, Color = page < 2  ? "0.929 0.882 0.847 0.7" :  "0.929 0.882 0.847 0.1"
                },
                RectTransform =
                {
                  AnchorMin = $"0.9 0.2", AnchorMax = $"1 0.4"
                }
            }, InitialLayer + ".Expedition");


            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0.5 0.6", AnchorMax = "0.89 0.85" },
                Image = { Color = "0.77 0.74 0.71 0" }
            }, InitialLayer + ".Expedition", InitialLayer + ".buttonAccept");

            container.Add(new CuiElement
            {
                Parent = InitialLayer + ".buttonAccept",
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 0.6", Text = $"*Нажми, чтобы передать бате предметы на обмен", Align = TextAnchor.UpperCenter , FontSize = 8},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin="-340 -250", OffsetMax="220 -225"}
                        }
            });

            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0" , Command = $"UI_TPBaraxolka startLoot"
                },
                Text =
                {
                    Text = "Начать обмен", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.6"
                },
                RectTransform =
                {
                    AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMin="-47 -174", OffsetMax="-70 -198"
                }
            }, InitialLayer + ".buttonAccept");
            CuiHelper.AddUi(player, container);
        }

        void CreateInfo(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            DrawNPCUI(player, "MainUI", 1);

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0.24 0.45 0.90 0" }
            }, InitialLayer + ".R", InitialLayer + ".Info");

            container.Add(new CuiElement
            {
                Parent = InitialLayer + ".Info",
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 0.6", Text = $"<b>Барахолка являются одним из самых популярных и полезных мест для покупки и продажи вещей.\nОна обладают рядом преимуществ, делая её незаменимой для многих игроков.\n\n<size=13>Во-первых:</size>\nБарахолки предлагают широкий ассортимент товаров, которые можно приобрести по выгодным ценам.\nБлагодаря большому количеству предложений, каждый пользователь может найти то, что ему нужно, и при этом выгодно купить товар.\n\n<size=13>Во-вторых:</size>\nБарахолки являются отличной площадкой для продажи ненужного хлама.\nВы можете продать свои ненужные предметы за <size=13>TopCoin</size> и купить что то полезное, либо испытать удачу в лотерее.</b>", Align = TextAnchor.UpperLeft , FontSize = 11},
                            new CuiRectTransformComponent {AnchorMin = "0.15 0", AnchorMax = "0.85 0.85"}
                        }
            });

            container.Add(new CuiElement
            {
                Parent = InitialLayer + ".Info",
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 0.6", Text = "ОСНОВНАЯ ИНФОРМАЦИЯ", Align = TextAnchor.LowerCenter , FontSize = 20},
                            new CuiRectTransformComponent {AnchorMin = "0 0.9", AnchorMax = "1 1"}
                        }
            });

            container.Add(new CuiButton
	    {
	    Button = {
	    Command = $"playerVideoBatia {config.urlvido}",
	    Color = "0 0 0 0.5" ,
	    },
	    Text = {
	    Text = $"ВИДЕО ИНСТРУКЦИЯ", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "0.929 0.882 0.847 0.7"
	    },
	    RectTransform = {
	    AnchorMin = "0.2 0", AnchorMax = "0.8 0.12"
	    }}, InitialLayer + ".Info");

            CuiHelper.DestroyUi(player, InitialLayer + ".Info");
            CuiHelper.AddUi(player, container);
            }
            [ConsoleCommand("playerVideoBatia")]
	    void paltdsag(ConsoleSystem.Arg arg)
	    {
	    BasePlayer baseplayer = arg.Player();
	    if(baseplayer == null) return;
	    string @string = arg.GetString(0, "");
	    baseplayer.Command("client.playvideo", new object[]
	    {
	    @string
	    });
	    }
            void CreateBottleExchange(BasePlayer player, int page = 0)
           {
            DrawNPCUI(player, "BuyerUI", 1);
            CuiElementContainer container = new CuiElementContainer();
            float amount = (float)TPEconomic.Call("API_GET_BALANCE", player.userID);

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-5 5", OffsetMax = "5 5" },
                Image = { Color = "1 1 1 0"}
            }, InitialLayer + ".R", InitialLayer + ".Bottle");

            var itemList = config.bottleSetting.CustomItemsShop.Skip(page * 12);
            if (itemList.Count() > 12)
            {
                container.Add(new CuiButton
                {
                    Button =
                {
                    Command = $"UI_Gold page {page+1} ",
                    Color = "0 0 0 0" ,
                },
                    Text =
                {
                    Text = $"▶", FontSize = 60, Align = TextAnchor.MiddleRight, Color = "0.929 0.882 0.847 0.7"
                },
                    RectTransform =
                {
                    AnchorMin = $"0.89 -0.15",
                    AnchorMax = $"0.99 0"
                }
                }, InitialLayer + ".Bottle");
            }

            int i = 0;
            int u = 0;
            //var pos = GetPositions(3, 4, 0.02f, 0.02f);
            int aa=0;

            foreach (var item in itemList.Skip(page * 12).Take(12))
            {
                if(i==3)
                {
                    i=0;
                    u++;
                }
                container.Add(new CuiPanel()
                {//AnchorMin = $"0.340 0.77", AnchorMax = $"0.658 1"
                    //+22
                    RectTransform = { AnchorMin = $"0 0.77", AnchorMax = $"0.32 1", OffsetMin = $"{205*i} {-75*u}", OffsetMax = $"{205*i} {-75*u}" },
                    Image = { Color = "1 1 1 0"}
                }, InitialLayer + ".Bottle", InitialLayer + $".Shop.{i}");

                container.Add(new CuiPanel()
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.36 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                    Image = { Color = "0.815 0.776 0.741 0", Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
                }, InitialLayer + $".Shop.{i}");


                var image = !string.IsNullOrEmpty(item.ImageURL) ? item.ImageURL : item.defaultItem.ShortName + 128;

                container.Add(new CuiElement
                {
                    Parent = InitialLayer + $".Shop.{i}",
                    Components =
                    {
                        new CuiRawImageComponent {Png            = (string) ImageLibrary?.Call("GetImage",image) },
                        new CuiRectTransformComponent {AnchorMin = "0 0.08", AnchorMax = "0.34 0.92", OffsetMin = "10 5", OffsetMax = "-10 -5"}
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.36 0.7", AnchorMax = "1 1", OffsetMin = "-3 0", OffsetMax = "0 0" },
                    Text = { Text = item.Title, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.6" }
                }, InitialLayer + $".Shop.{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "80 11", OffsetMax = "0 0" },
                    Text = { Text = $"{item.NeedGold} шт.", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "0.929 0.882 0.847 0.7" }
                }, InitialLayer + $".Shop.{i}");

                var color = amount >= item.NeedGold ? "0.8 0.7 0.741 0" : "0.815 0.776 0.741 0";

                var Tcolor = amount >= item.NeedGold ? "1 1 1 0.8" : "1 1 1 0.3";

                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = $"UI_TPBaraxolka buy {aa}", Material = "" },
                    Text = { Text = "Купить", Color = Tcolor, Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.72 0.1", AnchorMax = $"0.96 0.44" },
                }, InitialLayer + $".Shop.{i}");
                i++;
                aa++;
            }

            CuiHelper.DestroyUi(player, InitialLayer + ".Bottle");

            CuiHelper.AddUi(player, container);
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int pos)
        {
            var newItem = container.parent as Item;
            if (newItem == null) return null;

            if (newItem.info.shortname == "wrappedgift")
                return ItemContainer.CanAcceptResult.CannotAccept;
            return null;
        }
        [ChatCommand("baraxolka")]
        void asf(BasePlayer player)
        {
            DrawNPCUI(player);
        }

        public void DrawMenuPoints(BasePlayer player, MenuPoints choosed = null)
        {
            CuiHelper.DestroyUi(player, InitialLayer + ".Bottle");
            CuiHelper.DestroyUi(player, InitialLayer + ".Info");
            CuiHelper.DestroyUi(player, InitialLayer + ".Expedition");

            CuiElementContainer container = new CuiElementContainer();
/*
            float marginTop = -211;
            float originalHeight = 35;
            float freeHeight = 20;
            float padding = 5;*/

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0.24 0.45 0.90 0", Material = "" }
            }, InitialLayer + ".R", InitialLayer + ".MenuInfo");

            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"-355 -228", OffsetMax = $"-265 -205" },
                    Button = { Command = "UI_TPBaraxolka info", Color = "1 1 1 0" },
                    Text = { Text = "ИНФОРМАЦИЯ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 11, Color = "1 1 1 0.6" }
                }, InitialLayer + ".C", InitialLayer + "inf");

            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"-245 -226", OffsetMax = $"-198 -205" },
                    Button = { Command = "UI_TPBaraxolka expedition 0", Color = "1 1 1 0" },
                    Text = { Text = "ДРОП", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 11, Color = "1 1 1 0.6" }
                }, InitialLayer + ".C", InitialLayer + "drop");

            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"-180 -228", OffsetMax = $"-107 -205" },
                    Button = { Command = "UI_TPBaraxolka bottle", Color = "1 1 1 0" },
                    Text = { Text = "ПОКУПКА", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 11, Color = "1 1 1 0.6" }
                }, InitialLayer + ".C", InitialLayer + "sukablyatbottlevodkanaxuirusskiyvodkaPonyalDaAAAA");

 

            updateBalance(player);


            CuiHelper.DestroyUi(player, InitialLayer + ".MenuInfo");

            CuiHelper.AddUi(player, container);
        }
        void updateBalance(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, InitialLayer + "BalanceTbanyaShval");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"-100 -228", OffsetMax = $"-10 -203" },
                Text = { Text = $"{(float) TPEconomic.Call("API_GET_BALANCE", player.userID)}xp", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 0.8" }
            }, InitialLayer + $".C", InitialLayer + "BalanceTbanyaShval");

            CuiHelper.AddUi(player, container);
        }
        [ConsoleCommand("UI_TPBaraxolka")]
        void cmdMenuTPBaraxolka(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;

            if (!args.HasArgs(1)) return;
            switch (args.Args[0])
            {
                case "menu":
                    int chooseIndex = -1;
                    if (!int.TryParse(args.Args[1], out chooseIndex)) return;

                    DrawMenuPoints(player);

                    break;

                case "bottle":
                    CuiHelper.DestroyUi(player, InitialLayer + ".Expedition");
                    CuiHelper.DestroyUi(player, InitialLayer + ".Info");
                    CreateBottleExchange(player);
                    break;
                case "info":
                    CuiHelper.DestroyUi(player, InitialLayer + ".Expedition");
                    CuiHelper.DestroyUi(player, InitialLayer + ".art");
                    CreateInfo(player);
                    break;
               

                

                case "expedition":
                    int page;
                    if (!int.TryParse(args.Args[1], out page)) page = 0;
                    CuiHelper.DestroyUi(player, InitialLayer + ".Info");
                    CuiHelper.DestroyUi(player, InitialLayer + ".art");
                    CreateInfoJson(player, page);
                    break;

                case "startLoot":
                    CuiHelper.DestroyUi(player, "MainUI_TPBaraxolka");
                    ExpeditionExceptionBox box = ExpeditionExceptionBox.Spawn(player);
                    box.StartLoot();
                    break;
                case "sendLoot":
                    var entityLoot = player.inventory.loot;
                    if (entityLoot == null || entityLoot.entitySource.GetComponent<ExpeditionExceptionBox>() == null) return;
                    if (entityLoot.entitySource.GetComponent<ExpeditionExceptionBox>().PoitsInvectoryCount() < 1) return;

                    Dictionary<string, int> obmenList = DataObmen[player.userID];
                    foreach(var a in entityLoot.entitySource.GetComponent<ExpeditionExceptionBox>().storage.inventory.itemList)
                    {
                        if (obmenList.ContainsKey($"{a.info.shortname}"))
                        {
                            if(obmenList[a.info.shortname]+a.amount > config.expeditionSettings.ItemsAdded[a.info.shortname].maxCount)
                            {
                                player.ChatMessage($"Вы певышаете лимит {a.info.displayName.english}!\n{obmenList[a.info.shortname]} + {a.amount}/{config.expeditionSettings.ItemsAdded[a.info.shortname].maxCount}");
                                return;
                            }
                            obmenList[a.info.shortname] += a.amount;
                        }
                        else
                        {
                            if(a.amount > config.expeditionSettings.ItemsAdded[a.info.shortname].maxCount)
                            {
                                player.ChatMessage($"Вы певышаете лимит {a.info.displayName.english}!\n{a.amount}/{config.expeditionSettings.ItemsAdded[a.info.shortname].maxCount}");
                                return;
                            }
                            obmenList.Add(a.info.shortname, a.amount);
                        }
                    }
                      
                    entityLoot.entitySource.GetComponent<ExpeditionExceptionBox>().Close();
                    player.SendConsoleCommand("menu tpbaraxolka");
                    break;

                case "buy":
                    chooseIndex = -1;
                    if (!int.TryParse(args.Args[1], out chooseIndex)) return;
                    float amount = (float) TPEconomic.Call("API_GET_BALANCE", player.userID);
                    var buyItem = config.bottleSetting.CustomItemsShop.ElementAtOrDefault(chooseIndex);
                    if (buyItem == null) return;
                    if (amount < buyItem.NeedGold) 
                    {
                        CuiHelper.DestroyUi(player, "fdsfsdfasgdsag");
                        CuiElementContainer container = new CuiElementContainer();
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.2 -0.5", AnchorMax = "0.78 0", OffsetMax = "0 0" },
                            Button = { Color = "1 1 1 0.03" },
                            Text = { Text = "НЕТ ДЕНЕГ НА БАЛАНСЕ", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "0.929 0.882 0.847 1" }
                        }, InitialLayer + ".Bottle", "fdsfsdfasgdsag");
                        CuiHelper.AddUi(player, container);
                        timer.Once(2.0f, () =>
                        {
                            CuiHelper.DestroyUi(player, "fdsfsdfasgdsag");
                        });
                        return;
                    }
                    if (buyItem.Command.Count > 0)
                    {
                        foreach (var command in buyItem.Command)
                        {
                            Server.Command(command.Replace("%STEAMID%", player.UserIDString));
                        }
                        TPEconomic.Call("API_PUT_BALANCE_MINUS", player.userID, (float)buyItem.NeedGold);               
                        updateBalance(player);
                        break;
                    }
                    if (!string.IsNullOrEmpty(buyItem.defaultItem.ShortName))
                    {
                        var giveItem = ItemManager.CreateByName(buyItem.defaultItem.ShortName, buyItem.defaultItem.MinAmount, buyItem.defaultItem.SkinID);
                        player.GiveItem(giveItem, BaseEntity.GiveItemReason.Generic);
                        TPEconomic.Call("API_PUT_BALANCE_MINUS", player.userID, (float)buyItem.NeedGold);                    
                        updateBalance(player);
                        break;
                    }
                    SendReply(player, $"Ошибка");
                    break;

                case "close":
                    if (com != null && com.OpenInterface.Contains(player))
                        com.OpenInterface.Remove(player);
                    CuiHelper.DestroyUi(player, External);
                    CuiHelper.DestroyUi(player, "UI_TPBaraxolka_External" + ".Mood1" + ".panel");
                    break;
            }
        }

        public ulong ContainerID = 9876778;

        object CanLootEntity(BasePlayer player, BaseEntity container)
        {
            if (player == null || container == null) return null;
            if (container.OwnerID == ContainerID)
            {
                DrawNPCUI(player);
                com.OpenInterface.Add(player);
                return false;
            }
            return null;
        }

        void FindPositions()
        {
            var bandit = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().FirstOrDefault(p => p.name.Contains("bandit"));
            if (bandit != null)
            {
                string chairprefab = "assets/bundled/prefabs/static/chair.invisible.static.prefab";
                var pos1 = bandit.transform.position + bandit.transform.rotation * new Vector3(-24.2f, 2f, 37.2f);

                string lump = "assets/prefabs/deployable/lantern/lantern.deployed.prefab";
                var sp1 = GameManager.server.CreateEntity(lump, bandit.transform.position + bandit.transform.rotation * new Vector3(-24.1f, 2.7f, 36.2f), bandit.transform.rotation * new Quaternion(0f, 2f, 0f, -0.2f), true);
                sp1.enableSaving = false;
                UnityEngine.Object.Destroy(sp1.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(sp1.GetComponent<GroundWatch>());
                sp1.Spawn();
                sp1.SetFlag(BaseEntity.Flags.On, true);
                sp1.SetFlag(BaseEntity.Flags.Busy, true);
                sp1.SendNetworkUpdate();

                string cump = "assets/prefabs/deployable/fireplace/fireplace.deployed.prefab";
                var sp2 = GameManager.server.CreateEntity(cump, bandit.transform.position + bandit.transform.rotation * new Vector3(-21f, 2f, 37f), bandit.transform.rotation * new Quaternion(0f, 0.2f, 0f, -0.2f), true);
                sp2.enableSaving = false;
                UnityEngine.Object.Destroy(sp2.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(sp2.GetComponent<GroundWatch>());
                sp2.Spawn();
                sp2.SetFlag(BaseEntity.Flags.On, true);
                sp2.SetFlag(BaseEntity.Flags.Busy, true);
                sp2.SendNetworkUpdate();
                string photou = "assets/prefabs/deployable/signs/sign.pictureframe.landscape.prefab";
                var photo = GameManager.server.CreateEntity(photou, bandit.transform.position + bandit.transform.rotation * new Vector3(-24.1f, 3.6f, 35.7f), bandit.transform.rotation * new Quaternion(0f, 0f, 0f, -0.2f), true);
                photo.enableSaving = false;
                photo.Spawn();
                photo.SetFlag(BaseEntity.Flags.Busy, true);
                var sp3 = GameManager.server.CreateEntity("assets/prefabs/deployable/rug/rug.deployed.prefab", bandit.transform.position + bandit.transform.rotation * new Vector3(-24.6f, 1.9f, 37.5f), bandit.transform.rotation * new Quaternion(0f, 0.2f, 0f, -0.2f), true);
                sp3.Spawn();
                sp3.SetFlag(BaseEntity.Flags.Busy, true);
                UnityEngine.Object.Destroy(sp3.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(sp3.GetComponent<GroundWatch>());
                sp3.skinID = 871072156;
                sp3.SendNetworkUpdate();
                var chutemount = GameManager.server.CreateEntity(chairprefab, pos1, bandit.transform.rotation * new Quaternion(0f, 0.9f, 0f, -0.2f), true);
                chutemount.Spawn();
                SpawnNPC(pos1);
                var hasmount = chutemount.GetComponent<BaseMountable>();
                hasmount.MountPlayer(newPlayer);
                var fuel = GameManager.server.CreateEntity("assets/prefabs/deployable/quarry/fuelstorage.prefab", pos1 + new Vector3(0.3f, 0, 0), new Quaternion(0, 0, 90, 90), true);
                fuel.Spawn();
                fuel.OwnerID = ContainerID;
                fuel.SendNetworkUpdateImmediate();
                Item x = ItemManager.CreateByPartialName("targeting.computer", 1);

                BaseEntity dropped = x.Drop(photo.transform.position, Vector3.down, bandit.transform.rotation);
                dropped.SetParent(photo);
                UnityEngine.Object.Destroy(dropped.gameObject.GetComponent<Rigidbody>());
                dropped.transform.localPosition = new Vector3(-0.5f, -0.9f, 0.7f);

                dropped.transform.rotation = photo.transform.rotation;

                dropped.transform.eulerAngles = new Vector3(
                    dropped.transform.eulerAngles.x,
                    dropped.transform.eulerAngles.y + 190,
                    dropped.transform.eulerAngles.z
                );
                WorldItem worldItem = dropped as WorldItem;
                worldItem.allowPickup = false;
                worldItem.SetFlag(BaseEntity.Flags.Busy, true);
                dropped.GetComponent<DroppedItem>().CancelInvoke(new Action(dropped.GetComponent<DroppedItem>().IdleDestroy));
                dropped.SendNetworkUpdate();
                Item x1 = ItemManager.CreateByPartialName("shotgun.waterpipe", 1);
                BaseEntity dropped1 = x1.Drop(photo.transform.position, Vector3.down, bandit.transform.rotation);
                dropped1.SetParent(photo);
                UnityEngine.Object.Destroy(dropped1.gameObject.GetComponent<Rigidbody>());
                dropped.GetComponent<DroppedItem>().CancelInvoke(new Action(dropped1.GetComponent<DroppedItem>().IdleDestroy));

                dropped1.transform.localPosition = new Vector3(-0.9f, -0.88f, 1f);
                dropped1.transform.rotation = photo.transform.rotation;

                dropped1.transform.eulerAngles = new Vector3(
                     dropped1.transform.eulerAngles.x,
                     dropped1.transform.eulerAngles.y + 240,
                     dropped1.transform.eulerAngles.z + 90
                 );
                WorldItem worldItem1 = dropped1 as WorldItem;
                worldItem1.allowPickup = false;
                worldItem1.SetFlag(BaseEntity.Flags.Busy, true);

                BaseEntityList.Add(sp1);
                BaseEntityList.Add(sp2);
                BaseEntityList.Add(sp3);
                BaseEntityList.Add(fuel);
                BaseEntityList.Add(chutemount);
                BaseEntityList.Add(photo);

                if (photo.GetComponent<Signage>() != null)
                    FixSignage(photo.GetComponent<Signage>());
            }
        }

        private void FixSignage(Signage sign)
        {
            timer.Once(5f, () =>
            {
                sign.textureIDs[0] = FileStorage.server.Store(ImageBytes, FileStorage.Type.png, sign.net.ID);
                sign.SendNetworkUpdateImmediate();
            });
        }

        private static byte[] ImageBytes;

        private IEnumerator DownloadImage(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);

            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                www.Dispose();
                yield break;
            }

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(www.downloadHandler.data);
            if (texture != null)
            {
                ImageBytes = texture.EncodeToPNG();
            }

            www.Dispose();
        }

        public static TPBaraxolka ins;

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за покупку плагина на сайте TopPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < new VersionNumber(1, 0, 1))
            {
                PrintWarning("Config update detected! Updating config values...");
        

                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class MenuPoints
        {
            [JsonProperty("Название пункта меню в UI")]
            public string DisplayName;

            [JsonProperty("Выполняемая команда")]
            public string DrawMethod;

            [JsonProperty("Титл страницы")]
            public string Title;


            [JsonProperty("Диалог при нажатии (Пустое ничего не будет)")]
            public string Sound = "";

        }

        class PluginConfig
        {
            [JsonProperty("Видео обзор")]
	    public string urlvido;

            [JsonProperty("Настройка продажи предметов")]
            public BottleSetting bottleSetting;

            [JsonProperty("Настройка покупки")]
            public ExpeditionSettings expeditionSettings;


          

            [JsonProperty("Configuration Version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    urlvido = "https://cdn.discordapp.com/attachments/1169468253920301177/1195917678783778956/RUST_2024.01.14_-_04.45.13.06_2.mp4",
                    bottleSetting = new BottleSetting(),
                    expeditionSettings = new ExpeditionSettings(),
                    PluginVersion = new VersionNumber(),
                   
                };
            }
        }


        public class CustomShopItems
        {
            [JsonProperty("Название предмета в UI")]
            public string Title;

            [JsonProperty("Выполняемая команда (Если это предмет оставь ПУСТЫМ! %STEAMID% - индификатор игрока)")]
            public List<string> Command = new List<string>();

            [JsonProperty("Кастомное изображение предмета (Если у игровой предмет можно не указывать)")]
            public string ImageURL;

            [JsonProperty("Нужное количество золота на покупку данного предмета")]
            public int NeedGold;

            [JsonProperty("Настройка предмета (Если не привилегия трогать не нужно)")]
            public DefaultItem defaultItem;
        }

        public class DefaultItem
        {
            [JsonProperty("Shortname предмета")]
            public string ShortName;
            [JsonProperty("Количество")]
            public int MinAmount;
            [JsonProperty("SkinID предмета")]
            public ulong SkinID;
            [JsonProperty("Имя предмета при создании (Оставте поле постым чтобы использовать стандартное название итема)")]
            public string Name;
            [JsonProperty("Это чертеж")]
            public bool IsBlueprnt;
        }

    

        internal class ItemsAddSetting
        {
            [JsonProperty("Количество очков за предмет")]
            public float amount;

            [JsonProperty("Максимальное количество предмета")]
            public int maxCount;
        }

        internal class ArtefactsItems
        {
            [JsonProperty("ShortName предмета")] public string ShortName;
            [JsonProperty("SkinID предмета")] public ulong SkinID;
            [JsonProperty("Ссылка на изображение")] public string ImageURL;
        }

        public class BottleSetting
        {
            [JsonProperty("Список предметов на обмен")]
            public List<CustomShopItems> CustomItemsShop = new List<CustomShopItems>();
        }
  
        public class ExpeditionSettings
        {
            [JsonProperty("Список предметов, которые игрок может положить")]
            public Dictionary<string, ItemsAddSetting> ItemsAdded;
            
        }
    
        [ChatCommand("say")]
        void cmdBotSay(BasePlayer player, string com, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args.Length < 1) return;

            int count;
            if (!int.TryParse(args[1], out count)) return;

            var messages = Sounds[args[0]].Sounds[count];
            foreach (var f in messages)
            {
                SendToPlayer(player, newPlayer.net.ID.Value, f);
            }
        }

        #region TPBaraxolka
    
        private void GiveItem(BasePlayer player, Item item, BaseEntity.GiveItemReason reason = 0)
        {
            if (reason == BaseEntity.GiveItemReason.ResourceHarvested)
                //player.stats.Add(string.Format("harvest.{0}", item.info.shortname), item.amount, Stats.Server | Stats.Life);
                player.stats.Add(string.Format("harvest.{0}", item.info.shortname), item.amount, Stats.Server);

            int num = item.amount;
            if (!GiveItem(player.inventory, item, null))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return;
            }

            if (string.IsNullOrEmpty(item.name))
            {
                player.Command("note.inv", new object[] { item.info.itemid, num, string.Empty, (int)reason });
                return;
            }

            player.Command("note.inv", new object[] { item.info.itemid, num, item.name, (int)reason });
        }

        private bool GiveItem(PlayerInventory inv, Item item, ItemContainer container = null)
        {
            if (item == null)
                return false;

            int num = -1;
            GetIdealPickupContainer(inv, item, ref container, ref num);
            if (container != null && MoveToContainer(item, container, num, true))
                return true;

            if (MoveToContainer(item, inv.containerMain, -1, true))
                return true;

            if (MoveToContainer(item, inv.containerBelt, -1, true))
                return true;

            return false;
        }

        private static bool MoveToContainer(Item itemBase, ItemContainer newcontainer, int iTargetPos = -1, bool allowStack = true)
        {
            bool container;
            Quaternion quaternion;
            using (TimeWarning timeWarning = TimeWarning.New("MoveToContainer", 0))
            {
                var itemContainer = itemBase.parent;
                if (!itemBase.CanMoveTo(newcontainer, iTargetPos))
                    container = false;
                else
                    if (iTargetPos >= 0 && newcontainer.SlotTaken(itemBase, iTargetPos))
                {
                    Item slot = newcontainer.GetSlot(iTargetPos);

                    if (allowStack)
                    {
                        int num = slot.MaxStackable();
                        if (slot.CanStack(itemBase))
                        {
                            if (slot.amount < num)
                            {
                                slot.amount += itemBase.amount;
                                slot.MarkDirty();
                                itemBase.RemoveFromWorld();
                                itemBase.RemoveFromContainer();
                                itemBase.Remove(0f);
                                int num1 = slot.amount - num;
                                if (num1 > 0)
                                {
                                    Item item = slot.SplitItem(num1);
                                    if (item != null && !MoveToContainer(item, newcontainer, -1, false) && (itemContainer == null || !MoveToContainer(item, itemContainer, -1, true)))
                                    {
                                        Vector3 vector3 = newcontainer.dropPosition;
                                        Vector3 vector31 = newcontainer.dropVelocity;
                                        quaternion = new Quaternion();
                                        item.Drop(vector3, vector31, quaternion);
                                    }
                                    slot.amount = num;
                                }
                                container = true;
                                return container;
                            }
                            else
                            {
                                container = false;
                                return container;
                            }
                        }
                    }

                    if (itemBase.parent == null)
                        container = false;
                    else
                    {
                        ItemContainer itemContainer1 = itemBase.parent;
                        int num2 = itemBase.position;
                        if (slot.CanMoveTo(itemContainer1, num2))
                        {
                            itemBase.RemoveFromContainer();
                            slot.RemoveFromContainer();
                            MoveToContainer(slot, itemContainer1, num2, true);
                            container = MoveToContainer(itemBase, newcontainer, iTargetPos, true);
                        }
                        else
                            container = false;
                    }
                }
                else
                        if (itemBase.parent != newcontainer)
                {
                    if (iTargetPos == -1 & allowStack && itemBase.info.stackable > 1)
                    {
                        var item1 = newcontainer.itemList.Where(x => x != null && x.info.itemid == itemBase.info.itemid && x.skin == itemBase.skin).OrderBy(x => x.amount).FirstOrDefault();
                        if (item1 != null && item1.CanStack(itemBase))
                        {
                            int num3 = item1.MaxStackable();
                            if (item1.amount < num3)
                            {
                                var total = item1.amount + itemBase.amount;
                                if (total <= num3)
                                {
                                    item1.amount += itemBase.amount;
                                    item1.MarkDirty();
                                    itemBase.RemoveFromWorld();
                                    itemBase.RemoveFromContainer();
                                    itemBase.Remove(0f);
                                    container = true;
                                    return container;
                                }
                                else
                                {
                                    item1.amount = item1.MaxStackable();
                                    item1.MarkDirty();
                                    itemBase.amount = total - item1.MaxStackable();
                                    itemBase.MarkDirty();
                                    container = MoveToContainer(itemBase, newcontainer, iTargetPos, allowStack);
                                    return container;
                                }
                            }
                        }
                    }

                    if (newcontainer.maxStackSize > 0 && newcontainer.maxStackSize < itemBase.amount)
                    {
                        Item item2 = itemBase.SplitItem(newcontainer.maxStackSize);
                        if (item2 != null && !MoveToContainer(item2, newcontainer, iTargetPos, false) && (itemContainer == null || !MoveToContainer(item2, itemContainer, -1, true)))
                        {
                            Vector3 vector32 = newcontainer.dropPosition;
                            Vector3 vector33 = newcontainer.dropVelocity;
                            quaternion = new Quaternion();
                            item2.Drop(vector32, vector33, quaternion);
                        }
                        container = true;
                    }
                    else
                        if (newcontainer.CanAccept(itemBase))
                    {
                        itemBase.RemoveFromContainer();
                        itemBase.RemoveFromWorld();
                        itemBase.position = iTargetPos;
                        itemBase.SetParent(newcontainer);
                        container = true;
                    }
                    else
                        container = false;
                }
                else
                            if (iTargetPos < 0 || iTargetPos == itemBase.position || itemBase.parent.SlotTaken(itemBase, iTargetPos))
                    container = false;
                else
                {
                    itemBase.position = iTargetPos;
                    itemBase.MarkDirty();
                    container = true;
                }
            }

            return container;
        }

        private void GetIdealPickupContainer(PlayerInventory inv, Item item, ref ItemContainer container, ref int position)
        {
            if (item.info.stackable > 1)
            {
                if (inv.containerBelt != null && inv.containerBelt.FindItemByItemID(item.info.itemid) != null)
                {
                    container = inv.containerBelt;
                    return;
                }

                if (inv.containerMain != null && inv.containerMain.FindItemByItemID(item.info.itemid) != null)
                {
                    container = inv.containerMain;
                    return;
                }
            }

            if (!item.info.isUsable || item.info.HasFlag(ItemDefinition.Flag.NotStraightToBelt))
                return;

            container = inv.containerBelt;
        }

        public BasePlayer newPlayer = null;
        FatherComponent com = null;

        [PluginReference] Plugin IQKits, RustStore;

        private void SpawnNPC(Vector3 positon)
        {
            newPlayer = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", positon, new Quaternion()).ToPlayer();
            newPlayer.Spawn();
            com = newPlayer.gameObject.AddComponent<FatherComponent>();
            newPlayer.displayName = "БАТЯ";
            ItemManager.Create(ItemManager.FindItemDefinition("pants"), 1, 960252273).MoveToContainer(newPlayer.inventory.containerWear);
            ItemManager.Create(ItemManager.FindItemDefinition("hoodie"), 1, 959641236).MoveToContainer(newPlayer.inventory.containerWear);
            ItemManager.Create(ItemManager.FindItemDefinition("shoes.boots"), 1, 962503020).MoveToContainer(newPlayer.inventory.containerWear);
            newPlayer.SendNetworkUpdateImmediate();
            IQKits?.Call("ParseAndGive", newPlayer, "tpbaraxolka");
        }

        void Unload()
        {
            foreach (var ent in BaseEntityList)
            {
                ent.Kill();
            }
            BaseEntityList.Clear();
            if (newPlayer != null)
            {
                if (newPlayer.GetComponent<FatherComponent>() != null)
                {
                    newPlayer.GetComponent<FatherComponent>().SaveData();
                    UnityEngine.Component.Destroy(newPlayer.GetComponent<FatherComponent>());
                }
                newPlayer.AdminKill();
            }

            var objects = UnityEngine.Object.FindObjectsOfType<ExpeditionExceptionBox>();
            if (objects != null)
                foreach (var component in objects)
                    UnityEngine.Object.Destroy(component);

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, External);
                CuiHelper.DestroyUi(player, "ExpeditionExceptionBox_UI");
                CuiHelper.DestroyUi(player, External + ".Mood1" + ".panel");
                CuiHelper.DestroyUi(player, Layer95);

            }
            WriteData();
        }

        string GetImageInMood()
        {
            switch (com.mood)
            {
                case FatherComponent.Mood.Neutral:
                    return "https://i.imgur.com/yLsGJrM.png";
                case FatherComponent.Mood.Kind:
                    return "https://i.imgur.com/CidcicJ.png";
                case FatherComponent.Mood.Evil:
                    return "https://i.imgur.com/ZS57k7Y.png";
            }
            return "https://i.imgur.com/yLsGJrM.png";
        }

        void RegisteredDataUser(UInt64 player)
        {
            if (!player.IsSteamId()) return;
            if (!DataObmen.ContainsKey(player))
                DataObmen.Add(player, new Dictionary<string, int>());
        }
       

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            RegisteredDataUser(player.userID);

            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }
            CuiHelper.DestroyUi(player, External + ".Mood1" + ".panel");

            if (com != null && com.EndExpeditions.ContainsKey(player.userID))
                CreateInfoPlayer(player, false);
        }
        public List<ulong> ArtefactsList = new List<ulong>();


        private void OnServerShutdown() => Unload();
        private void Init() => ReadData();

        [JsonProperty("Список обмена")] public Dictionary<ulong, Dictionary<string, int>> DataObmen = new Dictionary<ulong, Dictionary<string, int>>();
        void ReadData()
        {
            DataObmen = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, int>>>("TPBaraxolka/Obmen");
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("TPBaraxolka/Obmen", DataObmen);
        }


        void sendmoney(BasePlayer player, float money)
        {
            TPEconomic.Call("API_PUT_BALANCE_PLUS", player.userID, money);
        }

        class FatherComponent : FacepunchBehaviour
        {
            public BasePlayer player;
            SphereCollider sphereCollider;
            public BasePlayer target = null;
            public Dictionary<ulong, ExpeditionPlayer> CurrentExpeditions = new Dictionary<ulong, ExpeditionPlayer>();
            public Dictionary<ulong, float> EndExpeditions = new Dictionary<ulong, float>();


            public List<BasePlayer> OpenInterface = new List<BasePlayer>();


            public void SaveData()
            {
                Interface.Oxide.DataFileSystem.WriteObject("TPBaraxolka/CurrentExpeditions", CurrentExpeditions);
                Interface.Oxide.DataFileSystem.WriteObject("TPBaraxolka/EndExpeditions", EndExpeditions);
                Interface.Oxide.DataFileSystem.WriteObject("TPBaraxolka/Artefacts", ins.ArtefactsList);

            }

            public void LoadData()
            {
                try
                {
                    CurrentExpeditions = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ExpeditionPlayer>>("TPBaraxolka/CurrentExpeditions");
                    EndExpeditions = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, float>>("TPBaraxolka/EndExpeditions");
                    ins.ArtefactsList = Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>("TPBaraxolka/Artefacts");

                }
                catch
                {
                    CurrentExpeditions = new Dictionary<ulong, ExpeditionPlayer>();
                    EndExpeditions = new Dictionary<ulong, float>();
                    ins.ArtefactsList = new List<ulong>();
                }
            }

            public Mood mood;

            public class ExpeditionPlayer
            {
                public double EndTime;
                public float Points;
                public bool Ending;
            }

          
            void SendMessages(ulong Player)
            {
                var bsPlayer = BasePlayer.FindByID(Player);
                if (bsPlayer == null || !bsPlayer.IsConnected) return;
            }


            void ExpeditionHandler()
            {
                foreach (var player in CurrentExpeditions)
                {
                    player.Value.EndTime--;
                    if (player.Value.EndTime <= 0)
                    {
                        EndExpeditions.Add(player.Key, player.Value.Points);
                        ins.NextTick(() => CurrentExpeditions.Remove(player.Key));

                        if (BasePlayer.FindByID(player.Key) != null)
                            ins.CreateInfoPlayer(BasePlayer.FindByID(player.Key), OpenInterface.Contains(BasePlayer.FindByID(player.Key)));
                        continue;
                    }

                }

                foreach (var bsPLayer in OpenInterface)
                {
                    CuiHelper.DestroyUi(player, "UI_TPBaraxolka_External" + ".Mood1" + ".panel");
                    if (CurrentExpeditions.ContainsKey(bsPLayer.userID))
                    {

                        if (CurrentExpeditions[bsPLayer.userID].EndTime <= 0)
                        {
                            ins.CreateInfoPlayer(player, true);
                        }
                        else
                            CreateInfoPlayer(bsPLayer, CurrentExpeditions[bsPLayer.userID].EndTime);


                    }


                }
            }

            void CreateInfoPlayer(BasePlayer player, double time)
            {
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Name = "UI_TPBaraxolka_External" + ".Mood1" + ".panel",
                    Parent = "Overlay",
                    Components =
                        {
                            new CuiImageComponent { Color =  "0.235 0.227 0.180 0.6" , Sprite = "assets/content/ui/ui.background.transparent.linear.psd"},
                            new CuiRectTransformComponent {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-240 -81", OffsetMax = "-60 -30"}
                        }
                });



                container.Add(new CuiElement
                {
                    Parent = "UI_TPBaraxolka_External" + ".Mood1" + ".panel",
                    Name = "UI_TPBaraxolka_External" + ".Mood1",
                    Components =
                        {
                            new CuiRawImageComponent {Png = (string)ins.ImageLibrary?.Call("GetImage", "https://i.imgur.com/ghyCq0Q.png"), Color = "0.929 0.882 0.847 1"},
                            new CuiRectTransformComponent {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-30 -60", OffsetMax = "50 20"}
                        }
                });



                container.Add(new CuiElement
                {
                    Parent = "UI_TPBaraxolka_External" + ".Mood1" + ".panel",
                    Components =
                        {
                            new CuiTextComponent { Color = "0.929 0.882 0.847 0.5", Text = $"<color=#EDE1D8>ОЖИДАНИЕ ПОСЫЛКИ</color>", Align = TextAnchor.UpperLeft /*,Font="robotocondensed-regular.ttf"*/},
                            new CuiRectTransformComponent {AnchorMin = "0.03 0", AnchorMax = "1 0.9"}
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = "UI_TPBaraxolka_External" + ".Mood1" + ".panel",
                    Components =
                        {
                            new CuiTextComponent {Color = "0.929 0.882 0.847 0.4", Text = $"ОСТАЛОСЬ ВРЕМЕНИ: {FormatShortTime(TimeSpan.FromSeconds(time))}", Align = TextAnchor.MiddleLeft ,Font="robotocondensed-regular.ttf", FontSize = 12},
                            new CuiRectTransformComponent {AnchorMin = "0.03 0", AnchorMax = "1 0.8"}
                        }
                });


                CuiHelper.DestroyUi(player, "UI_TPBaraxolka_External" + ".Mood1" + ".panel");

                CuiHelper.AddUi(player, container);
            }


            public static string FormatShortTime(TimeSpan time)
            {
                if (time.Hours != 0)
                    return time.ToString(@"hh\:mm\:ss");
                if (time.Minutes != 0)
                    return time.ToString(@"mm\:ss");
                if (time.Seconds != 0)
                    return time.ToString(@"mm\:ss");
                return time.ToString(@"hh\:mm\:ss");
            }



            public enum Mood
            {
                Neutral,
                Kind,
                Evil
            }

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                LoadData();
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 2.5f;
                sphereCollider.gameObject.layer = (int)Layer.Reserved1;
                ChangeInMood();
                InvokeRepeating("ChangeInMood", 7200, 7200);
                InvokeRepeating("ExpeditionHandler", 1, 1);
            }

            void ChangeInMood()
            {
                var random = UnityEngine.Random.Range(-1, 3);
                switch (random)
                {
                    case 0:
                        mood = Mood.Neutral;
                        break;
                    case 1:
                        mood = Mood.Kind;
                        break;
                    case 2:
                        mood = Mood.Evil;
                        break;
                }
            }

            private void OnTriggerEnter(Collider other)
            {
                var otherPlayer = other.gameObject.GetComponent<BasePlayer>();
                if (otherPlayer == null || target != null)
                    return;

                if (player.IsVisible(otherPlayer.transform.position))
                    ins.GetSoundToPlayer(otherPlayer, player.net.ID.Value, "hi");

            }

            private void OnTriggerExit(Collider other)
            {
                var otherPlayer = other.gameObject.GetComponent<BasePlayer>();
                if (otherPlayer == null)
                    return;
                if (player.IsVisible(otherPlayer.transform.position))
                    ins.GetSoundToPlayer(otherPlayer, player.net.ID.Value, "buy");
            }

            void Puts(string messages) => ins.Puts(messages);
            public void Destroy() => Destroy(this);
        }
        #endregion

        #region Sound
        public Dictionary<string, SoundData> Sounds = new Dictionary<string, SoundData>();

        public class SoundData
        {
            public List<List<byte[]>> Sounds = new List<List<byte[]>>();

            public List<byte[]> RandomSound()
              => Sounds.GetRandom();
        }

        Dictionary<ulong, bool> status = new Dictionary<ulong, bool>();
        public List<byte[]> timed = new List<byte[]>();

        private void GetSoundToPlayer(BasePlayer player, ulong netid, string name)
        {
            if (!Sounds.ContainsKey(name))
            {
                PrintError($"Не могу найти звук с именем {name}");
            }
            else
            {
                if (player != null)
                {
                    foreach (var f in Sounds[name].RandomSound())
                    {
                        SendToPlayer(player, netid, f);
                    }
                }
            }
        }

        public void SendToPlayer(BasePlayer player, ulong netid, byte[] data)
        {
            if (!Net.sv.IsConnected())
                return;

            NetWrite netWrite = Net.sv.StartWrite();
            

            netWrite.PacketID(Network.Message.Type.VoiceData);
            netWrite.UInt32(Convert.ToUInt32(netid));
            netWrite.BytesWithSize(data);
            netWrite.Send(new Network.SendInfo(player.Connection) { priority = Network.Priority.Immediate });
        }

        void LoadSounds()
        {
            try
            {
                Sounds = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, SoundData>>("TPBaraxolka/Sounds");
            }
            catch
            {
                Sounds = new Dictionary<string, SoundData>();
            }
        }
        private void SaveSoundData() => Interface.Oxide.DataFileSystem.WriteObject("TPBaraxolka/Sounds", Sounds);

        #endregion

        object OnPlayerVoice(BasePlayer player, byte[] data)
        {
            if (player != null && status.ContainsKey(player.userID))
            {
                timed.Add(data);
            }
            return null;
        }


        [ChatCommand("sound")]
        void startcmd(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            switch (args[0].ToLower())
            {
                case "start":
                    {
                        if (!status.ContainsKey(player.userID))
                        {
                            status.Add(player.userID, true);
                            SendReply(player, "Запись включена говорите в микрофон");
                            return;
                        }

                        if (status.ContainsKey(player.userID))
                        {
                            SendReply(player, "Запись уже идёт");
                            return;
                        }

                        if (!status[player.userID])
                        {
                            SendReply(player, "Запись уже сделана сохраните её");
                        }

                        break;
                    }
                case "clear":
                    {
                        if (status.ContainsKey(player.userID))
                        {
                            timed.Clear();
                            SendReply(player, "Запись стёрта попробуйте ещё");
                        }
                        else
                        {
                            SendReply(player, "Вы не начали запись");
                        }
                        break;
                    }
                case "stop":
                    {
                        if (status.ContainsKey(player.userID))
                        {
                            status[player.userID] = false;
                            SendReply(player, "Запись остановлена вы можете сохранить её");
                        }
                        else
                        {
                            SendReply(player, "Вы не начали запись");
                        }

                        break;
                    }
                case "save":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, "Введите название записи");
                            return;
                        }

                        if (!status.ContainsKey(player.userID))
                        {
                            SendReply(player, "Вы ничего не записали");
                            return;
                        }

                        if (!status[player.userID])
                        {
                            status.Remove(player.userID);
                            if (Sounds.ContainsKey(args[1]))
                            {
                                var sounds = Sounds[args[1]];
                                sounds.Sounds.Add(timed);
                            }
                            else
                            {
                                Sounds.Add(args[1], new SoundData());
                                Sounds[args[1]].Sounds.Add(timed);
                            }
                            SaveSoundData();
                            timed.Clear();
                            SendReply(player, "Запись успешно сохранена");
                        }
                        break;
                    }
                case "delete":
                    {
                        if (!Sounds.ContainsKey(args[1]))
                        {
                            SendReply(player, "Нету закой записи");
                            return;
                        }

                        Sounds.Remove(args[1]);
                        SendReply(player, $"Запись с названием {args[1]} была удалена");
                        break;
                    }
                case "play":
                    {
                        if (!Sounds.ContainsKey(args[1]))
                        {
                            SendReply(player, "Нету такой сохраненной записи");
                            return;
                        }
                        GetSoundToPlayer(player, player.net.ID.Value, args[1]);
                        break;
                    }
            }
        }


        class Position
        {
            public float Xmin;
            public float Xmax;
            public float Ymin;
            public float Ymax;

            public string AnchorMin =>
                $"{Math.Round(Xmin, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymin, 4).ToString(CultureInfo.InvariantCulture)}";
            public string AnchorMax =>
                $"{Math.Round(Xmax, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymax, 4).ToString(CultureInfo.InvariantCulture)}";

            public override string ToString()
            {
                return $"----------\nAmin:{AnchorMin}\nAmax:{AnchorMax}\n----------";
            }
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static List<Position> GetPositions(int colums, int rows, float colPadding = 0, float rowPadding = 0, bool columsFirst = false)
        {
            if (colums == 0)
                throw new ArgumentException("Can't create positions for gui!", nameof(colums));
            if (rows == 0)
                throw new ArgumentException("Can't create positions for gui!", nameof(rows));
            List<Position> result = new List<Position>();
            result.Clear();
            var colsDiv = 1f / colums;
            var rowsDiv = 1f / rows;
            if (colPadding == 0) colPadding = colsDiv / 2;
            if (rowPadding == 0) rowPadding = rowsDiv / 2;
            if (!columsFirst)
                for (int j = rows; j >= 1; j--)
                {
                    for (int i = 1; i <= colums; i++)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        };
                        result.Add(pos);
                    }
                }
            else
                for (int i = 1; i <= colums; i++)
                {
                    for (int j = rows; j >= 1; j--)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        };
                        result.Add(pos);
                    }
                }
            return result;
        }


        #region Expedition

        private static string _(string i)
        {
            return !string.IsNullOrEmpty(i)
                ? new string(i.Select(x =>
                    x >= 'a' && x <= 'z' ? (char)((x - 'a' + 13) % 26 + 'a') :
                    x >= 'A' && x <= 'Z' ? (char)((x - 'A' + 13) % 26 + 'A') : x).ToArray())
                : i;
        }


        public class ExpeditionExceptionBox : MonoBehaviour
        {
            public StorageContainer storage;
            public BasePlayer player;
            public string UIPanel = "ExpeditionExceptionBox_UI";

            public void Init(StorageContainer storage, BasePlayer owner)
            {
                this.storage = storage;
                this.player = owner;
            }
            public static ExpeditionExceptionBox Spawn(BasePlayer player)
            {
                player.EndLooting();
                var storage = SpawnContainer(player);
                var box = storage.gameObject.AddComponent<ExpeditionExceptionBox>();
                box.Init(storage, player);
                return box;
            }

            public static StorageContainer SpawnContainer(BasePlayer player)
            {
                var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", player.transform.position - new Vector3(0, 250f + UnityEngine.Random.Range(-25f, 25f), 0)) as StorageContainer;
                if (storage == null) return null;
                if (!storage) return null;
                storage.panelName = "mailboxcontents";
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
                UnityEngine.Object.Destroy(storage.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(storage.GetComponent<GroundWatch>());
                storage.Spawn();
                storage.inventory.capacity = 12;
                return storage;
            }

            private void PlayerStoppedLooting(BasePlayer player)
            {
                if (ins.com != null && ins.com.OpenInterface.Contains(player))
                    ins.com.OpenInterface.Remove(player);

                CuiHelper.DestroyUi(player, "UI_TPBaraxolka_External" + ".Mood1" + ".panel");
                CuiHelper.DestroyUi(player, UIPanel);
                ReturnPlayerItems();
                Destroy(this);
            }

            public void Close()
            {
                SendItems();
            }

            public void StartLoot()
            {
                player.EndLooting();
                storage.SetFlag(BaseEntity.Flags.Open, true, false);
                player.inventory.loot.StartLootingEntity(storage, false);
                player.inventory.loot.AddContainer(storage.inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", storage.panelName);
                storage.DecayTouch();
                storage.SendNetworkUpdate();
                CreateUI();
                InvokeRepeating("UpdatePanels", 1f, 1f);
                InvokeRepeating("UpdateInfo", 1f, 1f);
            }

            bool disabledButton = false;

            void CreateUI()
            {
                CuiHelper.DestroyUi(player, UIPanel);
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "199 35", OffsetMax = "425 97" },
                    Image = { Color = "1 1 1 0" }
                }, "Overlay", UIPanel);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image = { Color = "0.77 0.74 0.71 0.05", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, UIPanel, UIPanel + ".main");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.03 0.15", AnchorMax = "0.97 0.85" },
                    Button = { Command = disabledButton ? "UI_TPBaraxolka sendLoot" : "", Color = disabledButton ? "0.41 0.47 0.26 1.00" : "0.41 0.47 0.26 0.2" },
                    Text = { Text = $"", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 18, Color = disabledButton ? "0.59 0.69 0.42 0.7" : "0.59 0.69 0.42 0.2" }
                }, UIPanel + ".main", UIPanel + ".button");



             
                container.Add(new CuiElement
                {
                    Parent = UIPanel + ".button",

                    Components =
                    {
                        new CuiTextComponent { Text = $"ОТПРАВИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20, Color = disabledButton ? "0.59 0.69 0.42 0.7" : "0.59 0.69 0.42 0.2"},
                        new CuiRectTransformComponent {AnchorMin = "0 0.1", AnchorMax = "1 1"}
                    }
                });

                CuiHelper.AddUi(player, container);
                UpdateInfo();
            }

            public void ReturnPlayerItems()
            {
                global::ItemContainer itemContainer = storage.inventory;
                if (itemContainer != null)
                {
                    for (int i = itemContainer.itemList.Count - 1; i >= 0; i--)
                    {
                        global::Item item = itemContainer.itemList[i];
                        player.GiveItem(item, global::BaseEntity.GiveItemReason.Generic);
                    }
                }
            }

            void UpdatePanels()
            {
                if (storage.inventory.itemList.Count > 0 && !disabledButton && PoitsInvectoryCount() >= 1)
                {
                    disabledButton = true;
                    CreateUI();
                    return;
                }
                if (storage.inventory.itemList.Count == 0 && disabledButton || PoitsInvectoryCount() < 1)
                {
                    disabledButton = false;
                    CreateUI();
                }
            }

            public float PoitsInvectoryCount()
            {
                float amount = 0;

                for (int i = 0; i < storage.inventory.itemList.Count; i++)
                {
                    var item = storage.inventory.itemList[i];
                    if (!ins.config.expeditionSettings.ItemsAdded.ContainsKey(item.info.shortname))
                        return amount;
                    var configItem = ins.config.expeditionSettings.ItemsAdded[item.info.shortname];
                    amount += configItem.amount * item.amount;
                }

                return amount;
            }

            void UpdateInfo()
            {
                CuiHelper.DestroyUi(player, UIPanel + ".text");

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Parent = UIPanel + ".button",
                    Name = UIPanel + ".text",

                    Components =
                        {
                            new CuiTextComponent { Color = disabledButton? "0.59 0.69 0.42 1" : "0.59 0.69 0.42 0.2", Text = PoitsInvectoryCount() > 0 ?  $"Текущие очки: {PoitsInvectoryCount()}" : "Нету предметов" , Align = TextAnchor.LowerCenter , FontSize = 11},
                            new CuiRectTransformComponent {AnchorMin = "0 0.05", AnchorMax = "1 1"}
                        }
                });
                CuiHelper.AddUi(player, container);
            }


            public void SendItems()
            {
                ins.sendmoney(player, PoitsInvectoryCount());
                storage.inventory.itemList.Clear();
                player.EndLooting();
                Destroy(this);
            }

            public List<Item> GetItems => storage.inventory.itemList.Where(i => i != null).ToList();
            void OnDestroy()
            {
                ReturnPlayerItems();
                storage.Kill();
            }
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, ItemContainerId targetContainer, int targetSlot, int amount)
        {
            var player = playerLoot.GetComponent<BasePlayer>();
            if (player == null || playerLoot == null || targetContainer.Value == 0) return null;
            if(!config.expeditionSettings.ItemsAdded.ContainsKey(item.info.shortname)) return null;
            if (item.GetRootContainer() != null && item.GetRootContainer().entityOwner != null && item.GetRootContainer().entityOwner.GetComponent<ExpeditionExceptionBox>() != null)
            {
                var newContainer = playerLoot.FindContainer(targetContainer);
                if (newContainer == null) return null;
                var slot = newContainer.GetSlot(targetSlot);
                if (slot == null) return null;
                return false;
            }
            var container = playerLoot.FindContainer(targetContainer);
            if (container == null || container.entityOwner == null || container.entityOwner.GetComponent<ExpeditionExceptionBox>() == null) return null;
            if (!config.expeditionSettings.ItemsAdded.ContainsKey(item.info.shortname))
                return false;
            else
            {
                var configItem = config.expeditionSettings.ItemsAdded[item.info.shortname];
                var containsItem = container.itemList.Find(p => p.info.shortname == item.info.shortname);
                if (containsItem != null)
                {
                    if (configItem.maxCount == 1) return null;
                    if (containsItem.amount == configItem.maxCount) return false;
                    if ((containsItem.amount + amount) > configItem.maxCount)
                    {
                        var needAmount = configItem.maxCount - containsItem.amount;
                        item.UseItem(needAmount);

                        var newItem = ItemManager.CreateByItemID(item.info.itemid, needAmount);
                        newItem.MoveToContainer(container);
                        return false;
                    }
                }
                if (amount > configItem.maxCount)
                {
                    item.UseItem(configItem.maxCount);
                    var newItem = ItemManager.CreateByItemID(item.info.itemid, configItem.maxCount);
                    newItem.MoveToContainer(container);
                    return false;
                }
            }
            return null;
        }

        object OnEntityTakeDamage(BasePlayer entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (entity == newPlayer) return false;
            return null;
        }

        #endregion
    }
}