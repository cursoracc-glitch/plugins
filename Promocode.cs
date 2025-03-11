using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Promocode", "rustmods.ru", "1.0.1")]
    public class Promocode : RustPlugin
    {
        public class PSettings
        {
            [JsonProperty("Промокод, который нужно ввести")]
            public string Promocode;

            [JsonProperty("Сколько игрок получит рублей за данный промокод?")]
            public int GRub;

        }


        private ConfigData _config;
        public class ConfigData
        {
            [JsonProperty("Настройка промокодов")]
            public List<PSettings> PList = new List<PSettings>();
            
            
            [JsonProperty("Номер магазина!")]
            public string ShopID = "";
            
            [JsonProperty("Секретный ключ")]
            public string APIKey = "";

            public static ConfigData GetNewCong()
            {
                ConfigData newConfig = new ConfigData();
                newConfig.ShopID = "";
                newConfig.APIKey = "";
                newConfig.PList = new List<PSettings>()
                {
                    new PSettings()
                    {
                        Promocode = "y1",
                        GRub = 10,
                    },
                    new PSettings()
                    {
                        Promocode = "y2",
                        GRub = 20,
                    },
                    new PSettings()
                    {
                        Promocode = "y3",
                        GRub = 30,
                    }
                };
                return newConfig;
            }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
                
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => _config = ConfigData.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(_config);


        public class PlayerPromo
        {
            [JsonProperty("Введеный промокод")] 
            public string PromoCode;

            [JsonProperty("Во сколько он ввел промокод")]
            public string Date;
            
        }

        public class PlayerPromoSettings
        {
            public List<PlayerPromo> _promoList = new List<PlayerPromo>();
        }
        
        public Dictionary<ulong, PlayerPromoSettings> _playerPromo = new Dictionary<ulong,PlayerPromoSettings>();


        void OnServerInitialized()
        {
            try
            {
                _playerPromo = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerPromoSettings>>(Name);
            }
            catch
            {
                _playerPromo = new Dictionary<ulong, PlayerPromoSettings>();
            }
            
            ImageLibrary?.Call("AddImage", ImageButton, ImageButton);
        }


        [PluginReference] private Plugin ImageLibrary;
        public string Layer = "UI_YPromoLayer";
        public string ImageButton = "https://i.imgur.com/Q0ZBOad.png";


        [ConsoleCommand("destory.menusss")]
        void DestroyMenu(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            CuiHelper.DestroyUi(player, Layer);
        }
        
        
        [ChatCommand("promo")]
        void PieMenu(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            
            var panel = container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.7", Command = "destory.menusss"},
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent
                        {Png = (string) ImageLibrary?.Call("GetImage", ImageButton)},
                    new CuiRectTransformComponent {AnchorMin = "0.3864583 0.3916667", AnchorMax = "0.615625 0.5796296"},
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.5911459 0.5361111", AnchorMax = "0.6072916 0.5620371"},
                Button = {Color = "0 0 0 0", Command = "destory.menusss"},
                Text = {Text = ""}
            }, Layer);
            
            
            container.Add(new CuiButton
            {
                RectTransform =
                    {AnchorMin = "0.4125 0.4453703", AnchorMax = "0.5890625 0.4861111"},
                Button =
                {
                    FadeIn = 0f, Color = "0 0 0 0", Command = $"",
                },
                Text =
                {
                    Text = "", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = "0.4745098 0.4862745 0.5607843 1",
                    FontSize = 11
                }
            }, Layer, Layer + ".Transfer2");
            container.Add(new CuiElement 
            {
                Parent = Layer + $".Transfer2",
                Components = 
                { 
                    new CuiInputFieldComponent { FontSize = 14, Command = $"UI_HandlerPromo", Text = "Введи промокод", Align = TextAnchor.MiddleCenter, CharsLimit = 100, IsPassword = false},
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                } 
            });
            container.Add(new CuiButton
            {
                RectTransform =
                    {AnchorMin = "0.4125 0.4166667", AnchorMax = "0.5890625 0.4388889"},
                Button =
                {
                    FadeIn = 0f, Color = "0 0 0 0", Command = $"",
                },
                Text =
                {
                    Text = "Введи промокод и нажми Enter", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = "0.4745098 0.4862745 0.5607843 1",
                    FontSize = 11
                }
            }, Layer);
            CuiHelper.AddUi(player, container);
        }
        

        [ConsoleCommand("UI_HandlerPromo")]
        void GetPromo(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            try
            {
                string promo = args.Args[0];
                var find = _config.PList.Where(p => p.Promocode == promo).FirstOrDefault();
                if (find != null)
                {
                    if (_playerPromo[player.userID]._promoList.Any(p => p.PromoCode.ToLower() == promo.ToLower()))
                    {
                        player.ChatMessage("Вы уже ввели данный промокод!");
                        CuiHelper.DestroyUi(player, Layer);
                        return;
                    }
                    _playerPromo[player.userID]._promoList.Add(new PlayerPromo
                    {
                        PromoCode = promo,
                        Date = DateTime.Now.ToString("g")
                    });
                    player.ChatMessage($"Вы успешно ввели промокод и получили {find.GRub} руб. на баланс магазина!");
                    MoneyPlus(player.userID, find.GRub);
                    CuiHelper.DestroyUi(player, Layer);
                }
                else     
                {
                    player.ChatMessage("Промокод не найден, попробуйте еще раз!");
                }
            }
            catch
            {
            }
        }

        
        void MoneyPlus(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() }
            });
        }
        void ExecuteApiRequest(Dictionary<string, string> args)
        {
            string url = $"http://gamestores.ru/api?shop_id={_config.ShopID}&secret={_config.APIKey}" +
                         $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    LogToFile("Promocode", $"Код ошибки: {i}, подробности:\n{s}", this);
                }
                else
                {
                    if (s.Contains("fail"))
                    {
                        return;
                    }
                }
            }, this);
        }
        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _playerPromo);
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _playerPromo);
        }
    }
}